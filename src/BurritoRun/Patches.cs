using System;
using HarmonyLib;

namespace BurritoRun
{
    internal static class Names
    {
        internal const string Order = "BurritoRun_Order";
        internal const string Food = "BurritoRun_Food";
        internal const string Phone = "BurritoRun_Phone";
    }

    [HarmonyPatch(typeof(NameDB), nameof(NameDB.GetName))]
    internal static class NamePatch
    {
        private static bool Prefix(string myName, string type, ref string __result)
        {
            if (myName == Names.Order && type == "Interface") __result = "Order Burrito here ($10)";
            else if (myName == Names.Phone && type == "Item") __result = "Services Phone";
            else if (myName == Names.Phone && type == "Description") __result = "Food delivery and questionable convenience. Use to open Services. F2 also opens it.";
            else if (myName == Names.Food && type == "Item") __result = "Delivery Burrito";
            else if (myName == Names.Food && type == "Description") __result = "Still warm. Probably. Restores 15 health. Delivery fees do not cover gunfire.";
            else return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(InvItem), nameof(InvItem.SetupDetails))]
    internal static class PhoneDetails
    {
        private static void Prefix(InvItem __instance, out bool __state)
        {
            __state = __instance.invItemName == Names.Phone;
            if (__state) __instance.invItemName = "FriendPhone";
        }
        private static void Finalizer(InvItem __instance, bool __state)
        {
            if (!__state) return;
            __instance.invItemName = Names.Phone;
            __instance.invItemRealName = "Services Phone";
            __instance.invItemDescription = "Use to open Services. F2 also opens it. Close phone to resume the city.";
            __instance.itemValue = 0;
            __instance.cantDrop = true;
            __instance.cantBeCloned = true;
            __instance.thiefCantSteal = true;
            __instance.hasCharges = false;
            __instance.stackable = false;
            __instance.noCountText = true;
        }
    }

    [HarmonyPatch(typeof(ItemFunctions), nameof(ItemFunctions.UseItem))]
    internal static class PhoneUse
    {
        private static bool Prefix(InvItem item, Agent agent)
        {
            if (item.invItemName != Names.Phone) return true;
            Plugin.Instance.Phone.Open(agent);
            return false; // Never summon native FriendPhone NPCs or consume the phone.
        }
    }

    [HarmonyPatch(typeof(GameController), nameof(GameController.SetTimeScale))]
    internal static class PhonePause
    {
        private static void Postfix()
        {
            if (Plugin.Instance?.Phone?.IsOpen == true) UnityEngine.Time.timeScale = 0;
        }
    }

    // Suppress gameplay input, not Unity IMGUI input. A short close grace prevents
    // the mouse click or Escape used to dismiss the phone leaking into gameplay.
    // The quick-test panel also captures pointer-down frames before OnGUI runs.
    [HarmonyPatch]
    internal static class PhoneInput
    {
#pragma warning disable IDE0051 // Harmony discovers dynamic patch callbacks by reflection.
        [HarmonyTargetMethods]
        private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (var name in new[] { "keyCheck", "keyCheckHeld", "pressButtonDown", "mouseButtonDown", "mouseButtonUp", "useItemCheck" })
                yield return AccessTools.Method(typeof(PlayerControl), name);
        }
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (Plugin.Instance?.BlocksWorldInput != true) return true;
            __result = false;
            return false;
        }
#pragma warning restore IDE0051
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

    [HarmonyPatch(typeof(Door), nameof(Door.KnockOnDoor))]
    internal static class CourierDoorKnock
    {
        private static void Postfix(Door __instance, Agent __0)
        {
            try { Plugin.Instance?.Delivery?.OnCourierKnocked(__instance, __0); }
            catch (Exception error) { Plugin.Instance?.Error(error); }
        }
    }

    [HarmonyPatch(typeof(ObjectReal), nameof(ObjectReal.DestroyMe), new[] { typeof(PlayfieldObject) })]
    internal static class TestAquariumBreak
    {
        private static void Prefix(ObjectReal __instance, out bool __state)
        {
            __state = Plugin.Instance?.Aquarium?.ShouldBreak(__instance) == true;
        }

        private static void Postfix(ObjectReal __instance, PlayfieldObject __0, bool __state)
        {
            if (!__state) return;
            try { Plugin.Instance?.Aquarium?.OnBroken(__instance, __0); }
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
