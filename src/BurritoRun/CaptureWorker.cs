using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace BurritoRun
{
    // Exactly one writer for a plugin session. Work owns detached values, never Unity objects.
    internal sealed class CaptureWorker : IDisposable
    {
        private sealed class Work { internal Action Action; internal long Bytes; }
        private readonly Queue<Work> queue = new Queue<Work>();
        private readonly object gate = new object();
        private readonly Thread thread;
        private readonly int capacity;
        private readonly long byteCapacity;
        private bool stopping;
        private bool busy;
        private long queuedBytes;
        private long dropped;
        private double workMs;
        private string failure;
        private readonly Dictionary<string, StreamWriter> streams = new Dictionary<string, StreamWriter>();
        private long lastFlush;
        internal long Dropped => Interlocked.Read(ref dropped);
        internal int Depth { get { lock (gate) return queue.Count; } }
        internal long QueuedBytes { get { lock (gate) return queuedBytes; } }
        internal double WorkMs { get { lock (gate) return workMs; } }
        internal string Failure { get { lock (gate) return failure; } }
        internal bool Saturated => Depth >= capacity * 3 / 4 || QueuedBytes >= byteCapacity * 3 / 4;
        internal CaptureWorker(int capacity = 2048, long byteCapacity = 4 * 1024 * 1024)
        {
            this.capacity = capacity; this.byteCapacity = byteCapacity;
            thread = new Thread(Run) { IsBackground = true, Name = "BurritoRun capture writer" };
            thread.Start();
        }
        /// <summary>
        /// Queue detached work without waiting for disk, returning false when capacity or shutdown
        /// prevents it.
        /// Reserved space favors essential records while explicit drop accounting prevents false
        /// completeness claims.
        /// </summary>
        internal bool Enqueue(Action action, long bytes, bool essential = false)
        {
            lock (gate)
            {
                int reserved = Math.Min(64, capacity / 4);
                long reserveBytes = Math.Min(256 * 1024, byteCapacity / 4);
                if (stopping || bytes < 0 || queue.Count + (busy ? 1 : 0) >= capacity - (essential ? 0 : reserved) ||
                    bytes > byteCapacity - queuedBytes - (essential ? 0 : reserveBytes))
                { Interlocked.Increment(ref dropped); return false; }
                queue.Enqueue(new Work { Action = action, Bytes = bytes }); queuedBytes += bytes;
                Monitor.PulseAll(gate); return true;
            }
        }
        /// <summary>
        /// Execute queued work in order on the single writer thread and periodically flush idle
        /// buffers.
        /// Keeping serialization and disk access off the game thread reduces capture-induced frame
        /// stalls.
        /// </summary>
        private void Run()
        {
            while (true)
            {
                Work work;
                lock (gate)
                {
                    while (queue.Count == 0 && !stopping)
                    {
                        // Release the queue lock before disk access; idle buffers flush within half a second.
                        Monitor.Exit(gate);
                        try { FlushStreams(false); }
                        finally { Monitor.Enter(gate); }
                        if (queue.Count == 0 && !stopping) Monitor.Wait(gate, 500);
                    }
                    if (queue.Count == 0) break;
                    work = queue.Dequeue(); busy = true;
                }
                var timer = Stopwatch.StartNew();
                try { work.Action(); if ((Stopwatch.GetTimestamp() - lastFlush) / (double)Stopwatch.Frequency >= .5) FlushStreams(false); }
                catch (Exception error) { Interlocked.Increment(ref dropped); lock (gate) failure = error.GetType().Name + ": " + error.Message; }
                finally
                {
                    lock (gate)
                    {
                        queuedBytes -= work.Bytes; busy = false; workMs += timer.Elapsed.TotalMilliseconds;
                        Monitor.PulseAll(gate);
                    }
                }
            }
            FlushStreams(true);
        }
        // Worker-thread only. Legacy text remains byte-for-byte ordered while writes are buffered.
        /// <summary>
        /// Append legacy text on the writer thread while preserving its original order.
        /// Buffered I/O improves capture cost without changing the text format consumed by existing
        /// readers.
        /// </summary>
        internal void AppendBuffered(string path, string text, bool flush = false)
        {
            if (Thread.CurrentThread != thread) throw new InvalidOperationException("Writer thread required");
            if (!streams.TryGetValue(path, out var stream))
            {
                if (streams.Count >= 8) throw new InvalidOperationException("Too many capture files");
                streams[path] = stream = new StreamWriter(path, true, new UTF8Encoding(false), 65536);
            }
            stream.Write(text);
            if (flush) stream.Flush();
        }
        private void FlushStreams(bool close)
        {
            foreach (var stream in streams.Values)
                try { if (close) stream.Dispose(); else stream.Flush(); }
                catch (Exception error) { Interlocked.Increment(ref dropped); lock (gate) failure = error.GetType().Name + ": " + error.Message; }
            lastFlush = Stopwatch.GetTimestamp();
        }
        // Tests/shutdown only. Never wait for disk during live gameplay.
        /// <summary>
        /// Wait up to a bounded deadline for queued work to finish during tests or shutdown.
        /// Live gameplay must not block on storage; a timeout must remain distinguishable from a
        /// successful drain.
        /// </summary>
        internal bool Drain(int milliseconds = 5000)
        {
            var timer = Stopwatch.StartNew();
            lock (gate)
            {
                while (busy || queue.Count > 0)
                {
                    int remaining = milliseconds - (int)timer.ElapsedMilliseconds;
                    if (remaining <= 0) return false;
                    Monitor.Wait(gate, remaining);
                }
                return true;
            }
        }
        /// <summary>
        /// Stop accepting work, wake the writer and wait briefly for it to finish.
        /// Shutdown has a time bound, so disposal alone cannot guarantee every queued record
        /// reached disk.
        /// </summary>
        public void Dispose()
        { lock (gate) { stopping = true; Monitor.PulseAll(gate); } thread.Join(1500); }

        // Conservative detached payload accounting. Unknown reference types are rejected.
        /// <summary>
        /// Estimate the retained size of a detached payload and reject unsupported or deeply nested
        /// values.
        /// Queue limits must account for payload memory, and arbitrary reference objects could
        /// carry unsafe live game state.
        /// </summary>
        internal static long Measure(object value, long limit = 4 * 1024 * 1024, int depth = 0)
        {
            if (depth > 32) return limit + 1;
            if (value == null) return 16;
            if (value is string text) return 64L + text.Length * 4L;
            if (value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal) return 32;
            long size = 64;
            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry pair in dictionary)
                {
                    size += 64 + Measure(pair.Key, limit, depth + 1) + Measure(pair.Value, limit, depth + 1);
                    if (size > limit) return size;
                }
                return size;
            }
            if (value is IEnumerable sequence)
            {
                foreach (var item in sequence)
                { size += 16 + Measure(item, limit, depth + 1); if (size > limit) return size; }
                return size;
            }
            return limit + 1;
        }
    }
}
