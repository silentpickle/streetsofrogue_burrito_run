using System;
using System.Collections;
using HarmonyLib;
using BepInEx;
using UnityEngine;

namespace BurritoRun
{
    [BepInPlugin("local.streetsofrogue.burritorun", "Burrito Run", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance;
        internal DeliveryService Delivery;
        internal bool TestMode;
        private string notice = "Burrito Run loaded. Quick Start a single-player run.";
        private float noticeUntil = 999999;

        private void Awake()
        {
            Instance = this;
            TestMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--burrito-test") >= 0;
            Delivery = new DeliveryService(this);
            new Harmony("local.streetsofrogue.burritorun").PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("BURRITO_BOOT plugin=0.1.0 unity=" + Application.unityVersion);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--burrito-smoke") >= 0)
                StartCoroutine(Smoke());
        }

        internal void Log(string message) => Logger.LogInfo(message);
        internal void Error(Exception error) => Logger.LogError(error);
        internal void Notice(string message)
        {
            notice = message;
            noticeUntil = Time.realtimeSinceStartup + 12;
            Log("BURRITO_NOTICE " + message);
        }

        private void Update()
        {
            try { Delivery.Tick(); }
            catch (Exception error) { Error(error); Delivery.AbortAfterError(); }
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            if (Time.realtimeSinceStartup < noticeUntil || Delivery.Active)
                GUI.Box(new Rect(15, Screen.height - 105, Math.Min(610, Screen.width - 30), 85),
                    "BURRITO RUN\n" + (Delivery.Active ? Delivery.Description : notice), style);
            if (!TestMode || !Delivery.CanOrder(GameController.gameController?.playerAgent)) return;
            GUILayout.BeginArea(new Rect(Screen.width - 235, 15, 220, 205), "Burrito Run quick test", GUI.skin.window);
            GUILayout.Label("Orders arrive in 3 seconds.\nYou can also use any computer.\n" + Delivery.ServiceStatus);
            if (GUILayout.Button("Order here ($10)")) Delivery.Order(GameController.gameController.playerAgent);
            if (GUILayout.Button("Courier drops package")) Delivery.ForceDrop();
            if (GUILayout.Button("Add $30 test money")) GameController.gameController.playerAgent.inventory.AddItem("Money", 30);
            GUILayout.EndArea();
        }

        private IEnumerator Smoke()
        {
            yield return new WaitForSecondsRealtime(25);
            Logger.LogInfo("BURRITO_SMOKE_PASS loader and plugin initialized");
            Application.Quit(0);
        }
    }
}
