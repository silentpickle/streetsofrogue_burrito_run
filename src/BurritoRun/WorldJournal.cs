using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace BurritoRun
{
    internal sealed class WorldJournal
    {
        private readonly CaptureWorker worker;
        private readonly string directory;
        private readonly string session;
        private readonly string build;
        private readonly long segmentLimit;
        private readonly int retain;
        private readonly Queue<string> files = new Queue<string>();
        private StreamWriter stream;
        private long segmentBytes;
        private int segment;
        private long sequence;
        private long dropped;
        private long bytes;
        private long records;
        private long retiredSegments;
        private long lastFlush;
        private string failure;
        private double serializationMs;
        private double writeMs;
        internal long Dropped => Interlocked.Read(ref dropped);
        internal long Bytes => Interlocked.Read(ref bytes);
        internal string Failure => failure;
        internal double SerializationMs => serializationMs;
        internal double WriteMs => writeMs;
        internal WorldJournal(CaptureWorker worker, string directory, string session, string build,
            long segmentLimit = 8 * 1024 * 1024, int retain = 16)
        { this.worker = worker; this.directory = directory; this.session = session; this.build = build; this.segmentLimit = segmentLimit; this.retain = retain; }
        /// <summary>
        /// Wrap detached data in a sequenced session record and queue it for background writing.
        /// Sequence gaps and rejected records remain visible so a reader can detect incomplete
        /// evidence.
        /// </summary>
        internal bool Write(string kind, int level, float gameTime, double wallTime, object data, bool essential = false)
        {
            long seq = Interlocked.Increment(ref sequence);
            if (directory == null || failure != null) { Interlocked.Increment(ref dropped); return false; }
            var record = new Dictionary<string, object> { { "schema", 1 }, { "stream", "world" }, { "seq", seq },
                { "session", session }, { "build", build }, { "event", kind }, { "level", level },
                { "gameTime", gameTime }, { "wallSeconds", wallTime }, { "utc", DateTime.UtcNow.ToString("O") }, { "data", data } };
            long size = CaptureWorker.Measure(record);
            if (worker.Enqueue(() => WriteCore(record), size, essential)) return true;
            Interlocked.Increment(ref dropped); return false;
        }
        /// <summary>
        /// Serialize one record, rotate bounded disk segments and track write failures on the
        /// worker thread.
        /// Disk retention is finite; segment retirement and dropped records must be reported rather
        /// than hidden.
        /// </summary>
        private void WriteCore(Dictionary<string, object> record)
        {
            if (failure != null) { Interlocked.Increment(ref dropped); return; }
            try
            {
                record["streamDropped"] = Dropped;
                var timer = Stopwatch.StartNew();
                string line = JsonConvert.SerializeObject(record, Formatting.None);
                int length = Encoding.UTF8.GetByteCount(line) + 1;
                serializationMs += timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                if (length > segmentLimit) { Interlocked.Increment(ref dropped); return; }
                if (stream == null || segmentBytes + length > segmentLimit)
                {
                    stream?.Dispose(); Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, "world-" + session + "-" + (segment++).ToString("D4") + ".jsonl");
                    stream = new StreamWriter(path, false, new UTF8Encoding(false), 65536); segmentBytes = 0;
                    files.Enqueue(path);
                    while (files.Count > retain) { File.Delete(files.Dequeue()); retiredSegments++; }
                    Manifest(false);
                }
                stream.WriteLine(line); segmentBytes += length; Interlocked.Add(ref bytes, length); records++;
                if ((Stopwatch.GetTimestamp() - lastFlush) / (double)Stopwatch.Frequency > 0.5)
                { stream.Flush(); lastFlush = Stopwatch.GetTimestamp(); }
                writeMs += timer.Elapsed.TotalMilliseconds;
            }
            catch (Exception error) { failure = error.GetType().Name + ": " + error.Message; Interlocked.Increment(ref dropped); }
        }
        /// <summary>
        /// Write the stream's retained files, losses, limits and closure state to its manifest.
        /// Readers need the recording's coverage and terminal state before trusting it as complete.
        /// </summary>
        private void Manifest(bool closed)
        {
            File.WriteAllText(Path.Combine(directory, "world-" + session + "-manifest.json"),
                JsonConvert.SerializeObject(new
                {
                    schema = 1,
                    session,
                    build,
                    closed,
                    records,
                    bytes = Bytes,
                    dropped = Dropped,
                    retiredSegments,
                    retainedFiles = files.ToArray(),
                    segmentLimit,
                    retain,
                    coverage = retiredSegments > 0 ? "old-segments-rotated" : "since-stream-start",
                    failure
                }, Formatting.Indented));
        }
        /// <summary>
        /// Queue final stream disposal and a closed manifest after previously queued writes.
        /// Closure shares the ordered writer; merely requesting it is not proof that it has reached
        /// disk.
        /// </summary>
        internal void Close()
        {
            if (!worker.Enqueue(() =>
            {
                try { stream?.Dispose(); stream = null; if (directory != null) { Directory.CreateDirectory(directory); Manifest(true); } }
                catch (Exception error) { failure = error.Message; }
            }, 256, true)) Interlocked.Increment(ref dropped);
        }
    }
}
