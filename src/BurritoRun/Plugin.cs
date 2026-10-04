using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BurritoRun
{
    [BepInPlugin("local.streetsofrogue.burritorun", "Burrito Run", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string Version = "0.6.0";
        internal static readonly string BuildLabel = typeof(Plugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? Version;
        internal static Plugin Instance;
        internal DeliveryService Delivery;
        internal ServicesPhone Phone;
        internal DoorwayInspector Inspector;
        internal WorldInspector World;
        internal TestScenarios Scenarios;
        internal CaptureWorker CaptureWriter;
        internal WorldAwareness Awareness;
        internal WorldHarness Harness;
        internal TestDiagnostics Diagnostics;
        internal BarkTrace Trace;
        internal AquariumService Aquarium;
        internal PetService Pets;
        internal bool TestMode;
        private Vector2 testPanelScroll;
        private float testPanelInputBlockedUntil;
        internal bool BlocksWorldInput
        {
            get
            {
                if (Phone?.BlocksInput == true) return true;
                if (Time.realtimeSinceStartup < testPanelInputBlockedUntil) return true;
                if (!TestMode || !Delivery.CanOrder(GameController.gameController?.playerAgent)) return false;
                var mouse = Input.mousePosition;
                bool pressed = Input.GetMouseButton(0) || Input.GetMouseButton(1) ||
                    Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                    Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
                return TestPanelInput.BlocksWorldClick(mouse.x, mouse.y,
                    Screen.width, Screen.height, pressed);
            }
        }
        private string notice = "Burrito Run loaded. Quick Start a single-player run.";
        private float noticeUntil = 999999;

        // Unity discovers these lifecycle callbacks by name.
#pragma warning disable IDE0051
        private void Awake()
        {
            Instance = this;
            TestMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--burrito-test") >= 0;
            Delivery = new DeliveryService(this);
            Phone = new ServicesPhone(this);
            Inspector = new DoorwayInspector(this);
            World = new WorldInspector(this);
            Scenarios = new TestScenarios(this);
            CaptureWriter = new CaptureWorker();
            Diagnostics = new TestDiagnostics(this);
            Aquarium = new AquariumService(this);
            Pets = new PetService(this);
            Trace = new BarkTrace(this, Diagnostics.LogDirectory, Diagnostics.Session);
            Awareness = new WorldAwareness(this);
            new Harmony("local.streetsofrogue.burritorun").PatchAll(typeof(Plugin).Assembly);
            Harness = new WorldHarness(this);
            Log("BURRITO_BOOT plugin=" + Version + " unity=" + Application.unityVersion);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--burrito-smoke") >= 0)
                StartCoroutine(Smoke());
        }

#pragma warning restore IDE0051

        internal void Log(string message)
        {
            var line = message + " build=" + BuildLabel + Diagnostics.Context;
            Logger.LogInfo(line);
            Diagnostics.Write(line);
        }
        internal void Error(Exception error)
        {
            Logger.LogError(error);
            Diagnostics.Write("BURRITO_ERROR " + error + " build=" + BuildLabel + Diagnostics.Context);
            Trace?.Interrupt("plugin-exception");
            Trace?.Incident("plugin-exception");
        }
        internal void Warn(string message) => Logger.LogWarning(message);
        internal void Notice(string message)
        {
            notice = message;
            noticeUntil = Time.realtimeSinceStartup + 12;
            Log("BURRITO_NOTICE " + message);
        }

#pragma warning disable IDE0051 // Unity lifecycle callbacks, invoked by the engine.
        private void Update()
        {
            Awareness?.BeginFrame();
            try { Phone.Tick(); Delivery.Tick(); if (TestMode) Scenarios.Tick(); }
            catch (Exception error) { Error(error); Delivery.AbortAfterError(); }
            try { Aquarium.Tick(); }
            catch (Exception error) { Error(error); }
            try { Pets.Tick(); }
            catch (Exception error) { Error(error); }
            var diagnosticTimer = System.Diagnostics.Stopwatch.StartNew();
            try { if (Awareness.Mode != AwarenessMode.Off) Trace.Tick(); }
            catch (Exception error) { Logger.LogError(error); Trace.Interrupt("capture-exception"); }
            Awareness.AddWork(diagnosticTimer.Elapsed.TotalMilliseconds);
            Awareness.Tick();
            var harnessTimer = Harness.Performance ? System.Diagnostics.Stopwatch.StartNew() : null;
            Harness.Tick();
            if (harnessTimer != null) Awareness.AddWork(harnessTimer.Elapsed.TotalMilliseconds);
            if (TestMode && Input.GetKeyDown(KeyCode.F11)) Awareness.StartBenchmarkSuite("ordinary");
            if (TestMode && Input.GetKeyDown(KeyCode.F10)) Awareness.ToggleDetail();
            if (TestMode && Input.GetKeyDown(KeyCode.F3)) Inspector.Capture(GameController.gameController?.playerAgent);
            if (TestMode && Input.GetKeyDown(KeyCode.F4)) World.Capture(GameController.gameController?.playerAgent);
            if (TestMode && Input.GetKeyDown(KeyCode.F5)) Diagnostics.MarkIncident();
            if (TestMode && Input.GetKeyDown(KeyCode.F6)) Pets.SpawnTestPet(GameController.gameController?.playerAgent);
            if (TestMode && Input.GetKeyDown(KeyCode.F7)) Pets.ReleaseTestPet();
            if (TestMode && Input.GetKeyDown(KeyCode.F8)) Pets.BarkTestPet();
            if (TestMode && Input.GetKeyDown(KeyCode.F9)) Pets.DistractGuard();
        }

        private void OnGUI()
        {
            Phone.Draw();
            if (Phone.IsOpen) return;
            Awareness.DrawOverlay();
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            if (Time.realtimeSinceStartup < noticeUntil || Delivery.Active)
                GUI.Box(new Rect(15, Screen.height - 105, Math.Min(610, Screen.width - 30), 85),
                    "BURRITO RUN " + BuildLabel + "\n" + (Delivery.Active ? Delivery.Description : notice), style);
            GUI.Label(new Rect(15, Screen.height - 25, 500, 25), "Burrito Run " + BuildLabel);
            if (!TestMode || !Delivery.CanOrder(GameController.gameController?.playerAgent)) return;
            var panelRect = new Rect(Screen.width - 275, 15, 260,
                Math.Min(720, Math.Max(200, Screen.height - 30)));
            var guiEvent = Event.current;
            if ((guiEvent.type == EventType.MouseDown || guiEvent.type == EventType.MouseUp) &&
                panelRect.Contains(guiEvent.mousePosition))
                testPanelInputBlockedUntil = Time.realtimeSinceStartup + 0.25f;
            GUILayout.BeginArea(panelRect, "Burrito Run quick test", GUI.skin.window);
            testPanelScroll = GUILayout.BeginScrollView(testPanelScroll);
            GUILayout.Label(BuildLabel);
            GUILayout.Label("Orders arrive in 3 seconds.\nYou can also use any computer.\n" + Delivery.ServiceStatus);
            if (GUILayout.Button("Services phone (F2)")) Phone.Open(GameController.gameController.playerAgent);
            if (GUILayout.Button("Inspect nearest door (F3)")) Inspector.Capture(GameController.gameController.playerAgent);
            if (GUILayout.Button("Inspect nearest object/NPC (F4)")) World.Capture(GameController.gameController.playerAgent);
            if (GUILayout.Button("World awareness inspector")) Awareness.ToggleDetail();
            if (GUILayout.Button("Mark incident (F5)")) Diagnostics.MarkIncident();
            GUILayout.Label("AQUARIUM  |  " + Aquarium.Status);
            if (GUILayout.Button("Spawn test aquarium")) Aquarium.SpawnTestTank(GameController.gameController.playerAgent);
            if (GUILayout.Button("Break test aquarium (diagnostic)")) Aquarium.ForceBreak(GameController.gameController.playerAgent);
            GUILayout.Label("TEST PET  |  " + Pets.Status);
            if (GUILayout.Button("Spawn test dog (F6)")) Pets.SpawnTestPet(GameController.gameController.playerAgent);
            if (GUILayout.Button("Drop one test snack")) Pets.DropTestSnack(GameController.gameController.playerAgent);
            if (GUILayout.Button("Release test dog (F7)")) Pets.ReleaseTestPet();
            GUILayout.Label(Pets.Barks.Status);
            Pets.Barks.AutomaticBarks = GUILayout.Toggle(Pets.Barks.AutomaticBarks, "Automatic stranger barks");
            if (GUILayout.Button("Bark near stranger (F8)")) Pets.BarkTestPet();
            if (GUILayout.Button("Distract guard (F9)")) Pets.DistractGuard();
            GUILayout.Label("SCENARIOS  |  " + Scenarios.Status);
            if (GUILayout.Button("Start: delivery handoff")) Scenarios.StartHandoff(GameController.gameController.playerAgent);
            if (GUILayout.Button("Start: drop and recover")) Scenarios.StartDropRecovery(GameController.gameController.playerAgent);
            if (GUILayout.Button("Start: safety recovery ($20)")) Scenarios.StartSafetyRecovery(GameController.gameController.playerAgent);
            if (GUILayout.Button("Continue safety recovery ($10)")) Scenarios.ContinueSafetyRecovery(GameController.gameController.playerAgent);
            if (GUILayout.Button("Clear scenario (world unchanged)")) Scenarios.Clear();
            if (GUILayout.Button("Order here ($10)")) Delivery.Order(GameController.gameController.playerAgent);
            if (GUILayout.Button("Courier drops package")) Delivery.ForceDrop();
            if (GUILayout.Button("Add $30 test money")) GameController.gameController.playerAgent.inventory.AddItem("Money", 30);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

#pragma warning restore IDE0051

        private IEnumerator Smoke()
        {
            yield return new WaitForSecondsRealtime(25);
            Log("BURRITO_SMOKE_PASS loader and plugin initialized");
            Application.Quit(0);
        }

#pragma warning disable IDE0051 // Unity invokes OnDestroy by name.
        private void OnDestroy() { Harness?.Close(); Pets?.Barks.Reset("plugin-unload"); Trace?.Interrupt("plugin-unload"); Awareness?.Close(); CaptureWriter?.Dispose(); }
#pragma warning restore IDE0051
    }
}
