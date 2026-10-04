using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BurritoRun
{
    [HarmonyPatch]
    internal static class WorldEffectSpawns
    {
#pragma warning disable IDE0051 // Harmony discovers these callbacks and target enumerator.
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in typeof(SpawnerMain).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (method.Name == "SpawnAgent" || method.Name == "SpawnNoise" || method.Name == "SpawnBullet" || method.Name == "SpawnFire" || method.Name == "SpawnGas" ||
                    method.Name == "SpawnWater" || method.Name == "SpawnDanger" || method.Name == "SpawnExplosion")
                    if (typeof(Component).IsAssignableFrom(method.ReturnType)) yield return method;
        }
        private static void Postfix(object __result, MethodBase __originalMethod)
        { if (__result is Component component) Plugin.Instance?.Awareness?.Lifecycle(component, true, __originalMethod.Name); }
#pragma warning restore IDE0051
    }
    // This exact overload is the native pooling boundary, including agents recycled without FakeSetInactive.
    [HarmonyPatch(typeof(ObjectPool), "Recycle", new[] { typeof(GameObject), typeof(GameObject) })]
    internal static class WorldPoolRetirement
    {
#pragma warning disable IDE0051 // Harmony discovers this observational callback.
        private static void Postfix(GameObject __0)
        {
            var awareness = Plugin.Instance?.Awareness;
            if (__0 == null || awareness == null || !awareness.LifecycleEnabled) return;
            long started = Stopwatch.GetTimestamp();
            var entity = __0.GetComponent<PlayfieldObject>();
            double lookupMs = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
            if (entity != null) awareness.Lifecycle(entity, false, "PoolRecycle", lookupMs);
            else awareness.AddHookWork(lookupMs);
        }
#pragma warning restore IDE0051
    }
    [HarmonyPatch]
    internal static class WorldEffectRetirements
    {
#pragma warning disable IDE0051 // Harmony discovers these read-only observers.
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in new[] { typeof(PlayfieldObject), typeof(Agent), typeof(Noise), typeof(Bullet), typeof(Fire), typeof(Gas), typeof(Water), typeof(Danger), typeof(Explosion) })
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    if (method.Name == "DestroyMe" || method.Name == "FakeSetInactive" || method.Name == "OnDestroy") yield return method;
        }
        private static void Postfix(object __instance, MethodBase __originalMethod)
        { if (__instance is Component component) Plugin.Instance?.Awareness?.Lifecycle(component, false, __originalMethod.Name); }
#pragma warning restore IDE0051
    }
}
