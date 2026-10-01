using System;
using HarmonyLib;

namespace BurritoRun
{
    internal static class Names
    {
        internal const string Order = "BurritoRun_Order";
        internal const string Food = "BurritoRun_Food";
    }

    [HarmonyPatch(typeof(NameDB), nameof(NameDB.GetName))]
    internal static class NamePatch
    {
        private static bool Prefix(string myName, string type, ref string __result)
        {
            if (myName == Names.Order && type == "Interface") __result = "Order Burrito here ($10)";
            else if (myName == Names.Food && type == "Item") __result = "Delivery Burrito";
            else if (myName == Names.Food && type == "Description") __result = "Still warm. Probably. Restores 15 health. Delivery fees do not cover gunfire.";
            else return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Computer), nameof(Computer.DetermineButtons))]
    internal static class TerminalButtons
    {
        private static void Postfix(Computer __instance)
        {
            if (__instance.functional && !__instance.destroyed &&
                Plugin.Instance.Delivery.CanOrder(__instance.interactingAgent) && !__instance.buttons.Contains(Names.Order))
                __instance.buttons.Add(Names.Order);
        }
    }

    [HarmonyPatch(typeof(Computer), nameof(Computer.PressedButton))]
    internal static class TerminalOrder
    {
        private static bool Prefix(Computer __instance, string buttonText)
        {
            if (buttonText != Names.Order) return true;
            try { Plugin.Instance.Delivery.Order(__instance.interactingAgent); }
            catch (Exception error) { Plugin.Instance.Error(error); Plugin.Instance.Delivery.AbortAfterError(); }
            __instance.StopInteraction();
            return false;
        }
    }

    // Reuse native food setup and consumption, including character dietary restrictions.
    // Restore the namespaced ID even if native code throws.
    [HarmonyPatch(typeof(InvItem), nameof(InvItem.SetupDetails))]
    internal static class BurritoDetails
    {
        private static void Prefix(InvItem __instance, out bool __state)
        {
            __state = __instance.invItemName == Names.Food;
            if (__state) __instance.invItemName = "HotFud";
        }
        private static void Finalizer(InvItem __instance, bool __state)
        {
            if (!__state) return;
            __instance.invItemName = Names.Food;
            __instance.invItemRealName = "Delivery Burrito";
            __instance.invItemDescription = "Still warm. Probably. Restores 15 health.";
        }
    }

    [HarmonyPatch(typeof(ItemFunctions), nameof(ItemFunctions.UseItem))]
    internal static class BurritoUse
    {
        private static void Prefix(InvItem item, out bool __state)
        {
            __state = item.invItemName == Names.Food;
            if (__state) item.invItemName = "HotFud";
        }
        private static void Finalizer(InvItem item, bool __state)
        {
            if (__state && item.invItemCount > 0) item.invItemName = Names.Food;
        }
    }

    [HarmonyPatch(typeof(Agent), nameof(Agent.SetJustHitByAgent))]
    internal static class CourierHit
    {
        private static void Postfix(Agent __instance, Agent myJustHitByAgent)
        {
            try { Plugin.Instance?.Delivery?.OnCourierHit(__instance, myJustHitByAgent); }
            catch (Exception error) { Plugin.Instance?.Error(error); }
        }
    }

    [HarmonyPatch(typeof(SteamGog), nameof(SteamGog.CloudSave))]
    internal static class LocalProfileCloud
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(SteamGog), nameof(SteamGog.SetAchievement))]
    internal static class LocalProfileAchievements
    {
        private static bool Prefix() => false;
    }
}
