using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BurritoRun
{
    // Ordered background event trail; abrupt exit can lose queued records.
    // BepInEx's full log remains the source for engine and stack-trace details.
    internal sealed class TestDiagnostics
    {
        private readonly Plugin plugin;
        private readonly string session = Guid.NewGuid().ToString("N").Substring(0, 8);
        private string eventPath;
        private bool writeFailed;
        private long eventBytes;
        internal string Session => session;
        internal string LogDirectory { get; private set; }

        internal TestDiagnostics(Plugin plugin)
        {
            this.plugin = plugin;
            var profile = Environment.GetEnvironmentVariable("BURRITO_PROFILE_PATH");
            if (string.IsNullOrEmpty(profile) || !Path.IsPathRooted(profile)) return;
            try
            {
                var logDir = Path.Combine(Path.GetDirectoryName(profile), "logs");
                LogDirectory = logDir;
                Directory.CreateDirectory(logDir);
                eventPath = Path.Combine(logDir, "events-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + session + ".log");
            }
            catch (Exception error)
            {
                writeFailed = true;
                plugin.Warn("Could not prepare event log: " + error.Message);
            }
        }

        internal string Context
        {
            get
            {
                var gc = GameController.gameController;
                return " session=" + session + " scenario=" + (plugin.Scenarios?.Name ?? "none") +
                    " run=" + (plugin.Scenarios?.RunNumber ?? 0) +
                    " level=" + (gc?.levelType ?? "none") + " seed=" + (gc?.seedNum ?? 0) +
                    " gameTime=" + Time.time.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        internal void Write(string line)
        {
            if (writeFailed || eventPath == null) return;
            string text = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + " " + line + Environment.NewLine;
            if (!plugin.CaptureWriter.Enqueue(() =>
            {
                int length = System.Text.Encoding.UTF8.GetByteCount(text);
                if (eventBytes + length > 8 * 1024 * 1024) throw new IOException("Event log 8 MiB cap reached");
                plugin.CaptureWriter.AppendBuffered(eventPath, text); eventBytes += length;
            }, 64L + text.Length * 4L, true))
                writeFailed = true;
        }

        internal void MarkIncident()
        {
            plugin.Trace?.Incident("user-mark");
            plugin.Awareness?.Incident("user-mark");
            var player = GameController.gameController?.playerAgent;
            var courier = plugin.Delivery.Courier;
            plugin.Log("BURRITO_MARK player=" + (player != null ? player.UID.ToString() : "none") +
                " at=" + (player != null ? player.tr.position.ToString() : "none") +
                " health=" + (player != null ? player.health.ToString("0", CultureInfo.InvariantCulture) : "none") +
                " money=" + (player?.inventory?.FindItem("Money")?.invItemCount ?? 0) +
                " courier=" + (courier != null ? courier.UID.ToString() : "none") +
                " courierAt=" + (courier != null ? courier.tr.position.ToString() : "none") +
                " order=" + plugin.Delivery.ActiveOrderId + " outcome=" + plugin.Delivery.LastOutcome);
            plugin.Notice("Incident marked in this session's event log.");
        }
    }
}
