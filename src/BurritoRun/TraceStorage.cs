using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace BurritoRun
{
    // No game objects enter the serializer. Callers supply detached value snapshots.
    internal sealed class TraceStorage
    {
        internal const long DefaultLimit = 8 * 1024 * 1024;
        private const long TerminalReserve = 64 * 1024;
        private readonly string path;
        private readonly long limit;
        private long bytes;
        private long dropped;
        private readonly CaptureWorker worker;
        internal long Dropped => Interlocked.Read(ref dropped);
        internal bool Failed { get; private set; }
        internal string Failure { get; private set; }
        internal bool Limited { get; private set; }

        internal TraceStorage(string path, long limit = DefaultLimit, CaptureWorker worker = null)
        { this.path = path; this.limit = limit; this.worker = worker; }

        internal bool Write(Dictionary<string, object> record, bool terminal = false)
        {
            if (Failed || path == null) { Interlocked.Increment(ref dropped); return false; }
            if (worker == null) return WriteCore(record, terminal);
            if (worker.Enqueue(() => WriteCore(record, terminal), CaptureWorker.Measure(record), terminal)) return true;
            Interlocked.Increment(ref dropped); return false;
        }

        private bool WriteCore(Dictionary<string, object> record, bool terminal)
        {
            try
            {
                // Earlier queued writes have completed here; terminal evidence includes their losses.
                if (terminal && record.TryGetValue("event", out var kind) && (string)kind == "terminal" &&
                    record.TryGetValue("data", out var dataValue) && dataValue is Dictionary<string, object> data)
                {
                    data["droppedRecords"] = Dropped;
                    data["fileLimited"] = Limited;
                    data["writerFailed"] = Failed;
                    if (Dropped > 0 && data.TryGetValue("status", out var status) && (string)status == "PASS")
                        data["status"] = "INCONCLUSIVE";
                }
                string line = JsonConvert.SerializeObject(record, Formatting.None) + "\n";
                int length = Encoding.UTF8.GetByteCount(line);
                long allowed = terminal ? limit : Math.Max(0, limit - Math.Min(TerminalReserve, limit / 4));
                if (bytes + length > allowed)
                { Limited = true; Interlocked.Increment(ref dropped); return false; }
                if (worker == null) File.AppendAllText(path, line, new UTF8Encoding(false));
                else worker.AppendBuffered(path, line, terminal);
                bytes += length;
                return true;
            }
            catch (Exception error)
            {
                Failed = true;
                Failure = error.GetType().Name + ": " + error.Message;
                Interlocked.Increment(ref dropped);
                return false;
            }
        }
    }

    internal sealed class TraceHistory<T>
    {
        private readonly Queue<T> values = new Queue<T>();
        private readonly Queue<long> weights = new Queue<long>();
        private readonly int capacity;
        private readonly long byteCapacity;
        internal long RetainedBytes { get; private set; }
        internal long Evicted { get; private set; }
        internal TraceHistory(int capacity, long byteCapacity = long.MaxValue)
        { this.capacity = capacity; this.byteCapacity = byteCapacity; }
        internal void Add(T value, long bytes = 1)
        {
            if (bytes < 0 || bytes > byteCapacity || capacity < 1) { Evicted++; return; }
            while (values.Count >= capacity || RetainedBytes > byteCapacity - bytes) Evict();
            values.Enqueue(value); weights.Enqueue(bytes); RetainedBytes += bytes;
        }
        private void Evict() { values.Dequeue(); RetainedBytes -= weights.Dequeue(); Evicted++; }
        internal T[] Snapshot() => values.ToArray();
        internal void Prune(Func<T, bool> expired)
        { while (values.Count > 0 && expired(values.Peek())) Evict(); }
        internal void Clear() { values.Clear(); weights.Clear(); RetainedBytes = 0; Evicted = 0; }
    }
}
