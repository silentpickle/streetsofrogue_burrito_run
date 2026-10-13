using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    internal sealed class QuestService
    {
        internal const string AcceptButton = "BurritoRun_FavorAccept";
        internal const string CashButton = "BurritoRun_FavorCash";
        internal const string KitButton = "BurritoRun_FavorKit";
        internal const string AbandonButton = "BurritoRun_FavorAbandon";
        private readonly Plugin plugin;
        private readonly QuestStartWindow startWindow = new QuestStartWindow();
        private bool usedThisRun;
        private bool released;
        private float nextObserve;
        internal BananaEncounter Encounter { get; private set; }
        internal QuestPolicy Policy { get; private set; }
        internal event Action<string, string> Completed;
        internal QuestService(Plugin plugin) { this.plugin = plugin; }
        /// <summary>
        /// Spend this run's encounter attempt and place the native recipients and banana.
        /// A failed placement remains a bounded attempt rather than an unlimited item-spawning
        /// retry loop.
        /// </summary>
        internal bool Start(Agent player)
        {
            if (usedThisRun || !Ready(player)) return false;
            usedThisRun = true;
            var encounter = new BananaEncounter(player);
            try
            {
                if (!encounter.Place()) { plugin.Notice("No clear place for a banana favor on this run."); return false; }
                Encounter = encounter; Policy = QuestPolicy.Banana(); released = false;
                Record("offered");
                plugin.Notice("Two neighbors want a banana. Talk to either: $15 or a First Aid Kit. Your choice.");
                return true;
            }
            catch (Exception error) { plugin.Error(error); encounter.RemoveOwned(true); return false; }
        }
        /// <summary>
        /// Check that the local player is in a fully loaded, supported single-player run.
        /// Character selection and outgoing menu rooms can otherwise look ready enough to consume
        /// the encounter too early.
        /// </summary>
        internal static bool Ready(Agent player)
        {
            var gc = GameController.gameController;
            return gc != null && gc.loadComplete && gc.loadCompleteReally && !gc.levelTransitioning && !gc.returningToMainMenu &&
                !SelectingCharacter(gc) &&
                gc.menuGUI?.titleScreen != true &&
                gc.serverPlayer && !gc.multiplayerMode && !gc.coopMode && !gc.fourPlayerMode && gc.agentsNest != null &&
                gc.levelType != "HomeBase" && gc.levelType != "Attract" && gc.levelType != "Tutorial" &&
                player != null && gc.playerAgent == player && player.isPlayer > 0 && player.localPlayer && player.didStartReal &&
                !player.dead && !player.ghost && player.inventory != null;
        }
        internal static bool SelectingCharacter(GameController gc) => gc?.mainGUI?.openedCharacterSelect == true ||
            gc?.mainGUI?.doingInitialCharacterSelect == true || gc?.menuGUI?.onMenu == true &&
            gc.menuGUI.characterSelect?.activeInHierarchy == true;
        /// <summary>
        /// Manage automatic startup, recipient/source loss and release across the encounter
        /// lifetime.
        /// The optional favor must expire with its own level and leave native quest progression
        /// alone.
        /// </summary>
        internal void Tick()
        {
            var gc = GameController.gameController;
            if (Encounter != null && !Encounter.SameLevel)
            {
                Policy.End(QuestState.Expired, "level-ended");
                Record("scope-ended"); Encounter = null;
            }
            if (gc != null && (gc.returningToMainMenu || gc.levelType == "HomeBase" || gc.levelType == "Attract"))
            {
                if (Encounter != null) Encounter.Release();
                Encounter = null; Policy = null; usedThisRun = false; released = false; startWindow.Reset(); return;
            }
            bool autoStart = startWindow.Observe(plugin.BananaMode && !usedThisRun && Encounter == null &&
                (plugin.Harness?.Enabled != true || plugin.Harness.Extension == "banana-quest") && Ready(gc?.playerAgent),
                Time.time, gc?.timeSinceLoad ?? 0);
            if (Time.timeScale <= 0 || Time.time < nextObserve) return;
            nextObserve = Time.time + .25f;
            if (Encounter == null)
            {
                if (autoStart) Start(gc.playerAgent);
                return;
            }
            Encounter.ObserveClock();
            if (Policy.Open)
            {
                if (!Ready(Encounter.Player)) Policy.End(QuestState.Unavailable, "player-unavailable");
                for (int i = 0; i < Policy.OfferCount; i++) if (!Encounter.RecipientLive(i)) Policy.Withdraw(i);
                if (Policy.Open && !Encounter.SourceLive && Count(Encounter.Player, QuestPolicy.RequiredItem) == 0)
                    Policy.End(QuestState.Unavailable, "banana-unavailable");
                if (!Policy.Open) Record("unavailable");
            }
            if (!Policy.Open && !released) { Encounter.Release(); released = true; }
        }
        /// <summary>
        /// Match an actor to one of this encounter's exact living recipients.
        /// Role names or reused native IDs are insufficient when deciding whose native menus we may
        /// extend.
        /// </summary>
        internal int RecipientIndex(Agent actor)
        {
            if (Encounter == null || !Encounter.SameLevel) return -1;
            for (int i = 0; i < 2; i++) if (Encounter.RecipientLive(i) && actor == Encounter.Recipients[i]) return i;
            return -1;
        }
        private bool CanTalk(Agent recipient, Agent player, out int index)
        {
            index = RecipientIndex(recipient);
            return index >= 0 && Ready(player) && player == Encounter.Player && !recipient.knockedOut &&
                !NpcPickupProbe.Emergency(recipient) && !NpcPickupProbe.Emergency(player) &&
                Vector2.Distance(player.tr.position, recipient.tr.position) <= 2 && player.movement.HasLOSPosition360(recipient.tr.position);
        }
        /// <summary>
        /// Append the favor's namespaced choices to the currently valid recipient menu.
        /// Other NPCs and unrelated native choices must retain their normal interaction paths.
        /// </summary>
        internal void AddButtons(Agent recipient)
        {
            if (!CanTalk(recipient, recipient.interactingAgent, out int index) || !Policy.Open || !Policy.Available(index)) return;
            AddButton(recipient, Policy.State == QuestState.Offered ? AcceptButton : GiveButton(index));
            if (Policy.State == QuestState.Accepted) AddButton(recipient, AbandonButton);
        }
        private static void AddButton(Agent actor, string button)
        {
            if (actor.buttons.Contains(button)) return;
            actor.buttons.Add(button); actor.buttonsExtra.Add(""); actor.buttonPrices.Add(0);
        }
        internal static string GiveButton(int index) => index == 0 ? CashButton : KitButton;
        internal static bool OwnButton(string button) => button == AcceptButton || button == CashButton || button == KitButton || button == AbandonButton;
        /// <summary>
        /// Handle our favor buttons and report whether the native handler should defer to us.
        /// Recognizing only namespaced choices prevents accidental takeover of unrelated menu
        /// actions.
        /// </summary>
        internal bool Handle(Agent recipient, Agent player, string button)
        {
            if (!OwnButton(button)) return false;
            if (!CanTalk(recipient, player, out int index) || !Policy.Open || !Policy.Available(index))
            { plugin.Notice("That favor is no longer available here."); return true; }
            if (button == AcceptButton)
            {
                if (Policy.Accept())
                {
                    recipient.Say("One banana. Buyer pays $15; Neighbor offers a first aid kit. Choose either of us.");
                    Record("accepted"); plugin.Notice("Banana favor accepted. Either recipient can complete it; only one reward.");
                }
            }
            else if (button == AbandonButton)
            { Policy.End(QuestState.Abandoned, "player-abandoned"); Encounter.Release(); released = true; Record("abandoned"); }
            else if (button == GiveButton(index)) Submit(index);
            return true;
        }
        /// <summary>
        /// Attempt one banana exchange, verify item custody and reward, and close or reopen the
        /// offer accordingly.
        /// Native gifting can consume food; explicit count checks and bounded compensation make
        /// failure visible.
        /// </summary>
        internal bool Submit(int index)
        {
            if (Encounter == null || index < 0 || index >= Policy.OfferCount || !CanTalk(Encounter.Recipients[index], Encounter.Player, out int actual) || actual != index ||
                !Policy.Begin(index)) return false;
            var player = Encounter.Player; var recipient = Encounter.Recipients[index]; var offer = Policy.Offer(index);
            var before = Snapshot(index);
            try
            {
                var banana = player.inventory.FindItem(QuestPolicy.RequiredItem);
                var reward = new InvItem { invItemName = offer.RewardItem, invItemCount = offer.RewardCount };
                reward.SetupDetails(false);
                string refusal = banana == null || banana.invItemCount < 1 ? "Bring one banana first." :
                    banana.questItem ? "That banana belongs to another quest." :
                    !HasRoom(recipient, banana) ? "Their inventory is full. Try the other recipient." :
                    !HasRoom(player, reward) ? "Make room for your reward first." : null;
                if (refusal != null)
                {
                    Policy.Refuse(refusal); Record("refused", before, Snapshot(index)); plugin.Notice(refusal); return false;
                }
                // Native GiveItem auto-uses food on an NPC. Add one then subtract one to verify custody before consumption.
                int playerBefore = Count(player, QuestPolicy.RequiredItem), recipientBefore = Count(recipient, QuestPolicy.RequiredItem);
                int rewardBefore = Count(player, offer.RewardItem);
                var received = recipient.inventory.AddItem(QuestPolicy.RequiredItem, 1);
                if (received == null || Count(recipient, QuestPolicy.RequiredItem) - (long)recipientBefore != 1)
                { Policy.End(QuestState.Faulted, "native-receipt-refused"); Record("fault", before, Snapshot(index)); return false; }
                player.inventory.SubtractFromItemCount(banana, 1);
                if (Count(player, QuestPolicy.RequiredItem) != playerBefore - 1)
                {
                    recipient.inventory.SubtractFromItemCount(received, 1);
                    Policy.End(QuestState.Faulted, "native-debit-mismatch"); Record("fault", before, Snapshot(index)); return false;
                }
                var paid = player.inventory.AddItem(offer.RewardItem, offer.RewardCount);
                if (paid == null && Count(player, offer.RewardItem) == rewardBefore)
                {
                    // Known refusal: restore exactly the transferred unit, then verify the compensation.
                    var restored = player.inventory.AddItem(QuestPolicy.RequiredItem, 1);
                    if (restored != null) recipient.inventory.SubtractFromItemCount(received, 1);
                    if (Count(player, QuestPolicy.RequiredItem) == playerBefore && Count(recipient, QuestPolicy.RequiredItem) == recipientBefore)
                        Policy.Refuse("reward-refused-restored");
                    else Policy.End(QuestState.Faulted, "reward-refusal-compensation-failed");
                    Record("refused", before, Snapshot(index)); plugin.Notice("The exchange could not finish. Check your banana before trying again."); return false;
                }
                bool complete = Policy.Complete(playerBefore, Count(player, QuestPolicy.RequiredItem), recipientBefore,
                    Count(recipient, QuestPolicy.RequiredItem), rewardBefore, Count(player, offer.RewardItem));
                Record(complete ? "completed" : "fault", before, Snapshot(index));
                if (!complete) { plugin.Notice("The exchange was interrupted. This favor has been closed."); return false; }
                Encounter.Release(); released = true;
                recipient.Say(offer.Thanks); plugin.Notice("Favor complete! Received " + offer.RewardLabel + ". The other offer is closed.");
                try { Completed?.Invoke("banana-favor", offer.Id); }
                catch (Exception error) { plugin.Error(error); }
                return true;
            }
            catch (Exception error)
            {
                Policy.End(QuestState.Faulted, "native-exchange-error"); Record("fault", before, Snapshot(index));
                plugin.Error(error); plugin.Notice("The exchange was interrupted. This favor has been closed."); return false;
            }
        }
        internal static int Count(Agent actor, string item) => actor?.inventory?.FindItem(item)?.invItemCount ?? 0;
        // Native hasEmptySlotForItem updates money-found statistics. These three simple items need only empty/stack slots.
        /// <summary>
        /// Check empty or compatible stack slots without invoking the native capacity helper.
        /// That helper can update money-found statistics; checking reward capacity should not
        /// change gameplay accounting.
        /// </summary>
        internal static bool HasRoom(Agent actor, InvItem item)
        {
            if (actor?.inventory?.InvItemList == null || item == null) return false;
            foreach (var slot in actor.inventory.InvItemList)
                if (string.IsNullOrEmpty(slot.invItemName) || item.stackable && slot.invItemName == item.invItemName) return true;
            return false;
        }
        /// <summary>
        /// Copy the favor state and relevant inventory counts into a detached evidence record.
        /// Before/after observations let report readers verify the exchange independently of the
        /// service's outcome label.
        /// </summary>
        internal Dictionary<string, object> Snapshot(int index = 0) => TraceWorld.Values(
            "state", Policy?.State.ToString(), "winner", Policy?.Winner, "reason", Policy?.Reason,
            "playerBananas", Count(Encounter?.Player, QuestPolicy.RequiredItem),
            "recipientBananas", Encounter != null ? Count(Encounter.Recipients[index], QuestPolicy.RequiredItem) : 0,
            "money", Count(Encounter?.Player, "Money"), "kits", Count(Encounter?.Player, "FirstAidKit"),
            "sourceActive", Encounter?.SourceLive == true, "recipient", index,
            "buyerActive", Encounter?.RecipientLive(0) == true, "neighborActive", Encounter?.RecipientLive(1) == true,
            "playerHealth", Encounter?.Player?.health, "timeScale", Time.timeScale);
        private void Record(string phase, Dictionary<string, object> before = null, Dictionary<string, object> after = null)
        {
            var data = TraceWorld.Values("phase", phase, "state", Policy?.State.ToString(), "winner", Policy?.Winner,
                "reason", Policy?.Reason, "before", before, "after", after ?? Snapshot());
            plugin.Log("BURRITO_QUEST " + JsonConvert.SerializeObject(data));
            if (plugin.Harness?.Enabled == true) plugin.Awareness.HarnessRecord("quest", data);
        }
        /// <summary>
        /// Expire unfinished work and release recipients when the plugin shuts down.
        /// Encounter-owned behavior must not remain active after its service stops ticking.
        /// </summary>
        internal void Close()
        {
            if (Policy != null && (Policy.Open || Policy.State == QuestState.Exchanging)) Policy.End(QuestState.Expired, "plugin-unload");
            if (Encounter?.SameLevel == true) Encounter.Release();
        }
        // Test-only reset: the command harness is already restricted to its own disposable profile.
        /// <summary>
        /// Remove the owned disposable encounter and reset its once-per-run state for the harness.
        /// This mutating reset is restricted to the dedicated harness, not offered as ordinary
        /// player-world cleanup.
        /// </summary>
        internal void ResetFixture()
        {
            if (plugin.Harness?.Enabled != true) throw new InvalidOperationException("Dedicated harness required");
            Encounter?.RemoveOwned(true); Encounter = null; Policy = null; usedThisRun = false; released = false; nextObserve = 0; startWindow.Reset();
        }
        internal void Draw()
        {
            if (Policy == null || Encounter == null || !Encounter.SameLevel || plugin.Phone.IsOpen) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = 15 };
            string text = "BANANA FAVOR — " + Policy.State + "\n";
            if (Policy.Open)
                text += "Buyer: $15   |   Neighbor: First Aid Kit\n" +
                    (Policy.State == QuestState.Offered ? "Talk to either neighbor to hear the offers." :
                    "Give one banana to either neighbor. Only one reward.") + "\n" +
                    (Count(Encounter.Player, QuestPolicy.RequiredItem) > 0 ? "You have a banana. Return to a neighbor." : "Pick up the marked banana with E.");
            else text += Policy.State == QuestState.Completed ? "Received " +
                (Policy.Winner == "buyer" ? "$15" : "First Aid Kit") + ". Both offers are closed." : "Optional favor ended. Continue your run.";
            GUI.Box(new Rect(12, Math.Max(196, Screen.height - 240), Math.Min(390, Screen.width - 24), 116), text, style);
            if (!Policy.Open) return;
            var camera = Encounter.Player.agentCamera?.originalCamera;
            if (camera == null) return;
            for (int i = 0; i < 2; i++)
                if (Encounter.RecipientLive(i) && Policy.Available(i))
                    Label(camera, Encounter.Recipients[i].tr.position, Policy.Offer(i).Recipient + " — " + Policy.Offer(i).RewardLabel, Color.cyan);
            if (Encounter.SourceLive) Label(camera, Encounter.Source.tr.position, "Banana — pick up with E", Color.yellow);
        }
        private static void Label(Camera camera, Vector3 position, string text, Color color)
        {
            var point = camera.WorldToViewportPoint(position);
            if (!WorldGeometry.InViewport(point.x, point.y, point.z)) return;
            var screen = camera.WorldToScreenPoint(position);
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = color;
            GUI.Box(new Rect(Mathf.Clamp(screen.x - 115, 0, Screen.width - 230), Screen.height - screen.y - 52, 230, 28), text, style);
        }
    }
}
