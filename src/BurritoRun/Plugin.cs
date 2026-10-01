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
        internal const string Version = "0.1.1";
        internal static readonly string BuildLabel = typeof(Plugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? Version;
        internal static Plugin Instance;
        internal DeliveryService Delivery;
        internal ServicesPhone Phone;
        internal DoorwayInspector Inspector;
        internal bool TestMode;
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
            new Harmony("local.streetsofrogue.burritorun").PatchAll(typeof(Plugin).Assembly);
            Log("BURRITO_BOOT plugin=" + Version + " unity=" + Application.unityVersion);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--burrito-smoke") >= 0)
                StartCoroutine(Smoke());
        }

#pragma warning restore IDE0051

        internal void Log(string message) => Logger.LogInfo(message + " build=" + BuildLabel);
        internal void Error(Exception error) => Logger.LogError(error);
        internal void Notice(string message)
        {
            notice = message;
            noticeUntil = Time.realtimeSinceStartup + 12;
            Log("BURRITO_NOTICE " + message);
        }

#pragma warning disable IDE0051 // Unity lifecycle callbacks, invoked by the engine.
        private void Update()
        {
            try { Phone.Tick(); Delivery.Tick(); }
            catch (Exception error) { Error(error); Delivery.AbortAfterError(); }
            if (TestMode && Input.GetKeyDown(KeyCode.F3)) Inspector.Capture(GameController.gameController?.playerAgent);
        }

        private void OnGUI()
        {
            Phone.Draw();
            if (Phone.IsOpen) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            if (Time.realtimeSinceStartup < noticeUntil || Delivery.Active)
                GUI.Box(new Rect(15, Screen.height - 105, Math.Min(610, Screen.width - 30), 85),
                    "BURRITO RUN " + BuildLabel + "\n" + (Delivery.Active ? Delivery.Description : notice), style);
            GUI.Label(new Rect(15, Screen.height - 25, 500, 25), "Burrito Run " + BuildLabel);
            if (!TestMode || !Delivery.CanOrder(GameController.gameController?.playerAgent)) return;
            GUILayout.BeginArea(new Rect(Screen.width - 235, 15, 220, 270), "Burrito Run quick test", GUI.skin.window);
            GUILayout.Label(BuildLabel);
            GUILayout.Label("Orders arrive in 3 seconds.\nYou can also use any computer.\n" + Delivery.ServiceStatus);
            if (GUILayout.Button("Services phone (F2)")) Phone.Open(GameController.gameController.playerAgent);
            if (GUILayout.Button("Inspect nearest door (F3)")) Inspector.Capture(GameController.gameController.playerAgent);
            if (GUILayout.Button("Order here ($10)")) Delivery.Order(GameController.gameController.playerAgent);
            if (GUILayout.Button("Courier drops package")) Delivery.ForceDrop();
            if (GUILayout.Button("Add $30 test money")) GameController.gameController.playerAgent.inventory.AddItem("Money", 30);
            GUILayout.EndArea();
        }

#pragma warning restore IDE0051

        private IEnumerator Smoke()
        {
            yield return new WaitForSecondsRealtime(25);
            Log("BURRITO_SMOKE_PASS loader and plugin initialized");
            Application.Quit(0);
        }
    }
}
