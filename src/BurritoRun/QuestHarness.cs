using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    // Disposable-profile fixture. Placement uses production code; API calls below are scripted, not player input.
    internal sealed class QuestHarness
    {
        private readonly Plugin plugin;
        private readonly Agent player;
        private readonly int initialBananas, initialKits, startingLevel;
        private readonly string[] cases = { "cash", "kit", "missing-banana", "full-reward", "full-recipient", "one-recipient-lost", "both-recipients-lost", "source-lost", "abandon", "pause", "level-change" };
        private int index, stage;
        private float due, deadline, pauseUntil, pauseTime;
        private string pauseSnapshot;
        private bool pickupVerified;
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        private QuestService Service => plugin.Quests;
        internal QuestHarness(Plugin plugin, Agent player)
        {
            this.plugin = plugin; this.player = player;
            initialBananas = QuestService.Count(player, "Banana"); initialKits = QuestService.Count(player, "FirstAidKit");
            startingLevel = plugin.Awareness.HarnessLevel; Setup();
        }
        private void Setup()
        {
            Service.ResetFixture();
            Trim("Banana", initialBananas); Trim("FirstAidKit", initialKits);
            pickupVerified = false;
            if (!Service.Start(player)) { Fail("production-placement-refused"); return; }
            var encounter = Service.Encounter;
            Record("placement", TraceWorld.Values("buyer", encounter.RecipientLive(0), "neighbor", encounter.RecipientLive(1),
                "source", encounter.SourceLive, "ordinaryLevel", !player.gc.customLevel,
                "unprotected", !encounter.Source.invItem.questItem && !encounter.Source.invItem.stealable && encounter.Source.startingOwner == 0,
                "buyerApproach", BananaEncounter.ClearApproach(player, encounter.Recipients[0].tr.position),
                "neighborApproach", BananaEncounter.ClearApproach(player, encounter.Recipients[1].tr.position),
                "bananaApproach", BananaEncounter.ClearApproach(player, encounter.Source.tr.position, .64f)));
            stage = 0; due = Time.time + 2; deadline = Time.realtimeSinceStartup + 30;
        }
        private void Trim(string item, int baseline)
        {
            var value = player.inventory.FindItem(item);
            if (value != null && value.invItemCount > baseline) player.inventory.SubtractFromItemCount(value, value.invItemCount - baseline);
        }
        private Dictionary<string, object> NativeState()
        {
            var quests = new List<object>();
            foreach (var quest in player.gc.quests.mainQuestList)
                quests.Add(TraceWorld.Values("type", quest.questType, "status", quest.questStatus, "code", quest.questStatusCode.ToString()));
            return TraceWorld.Values("quests", quests, "numQuests", player.gc.quests.numQuests,
                "floorComplete", player.gc.quests.floorComplete, "canExit", player.gc.exitPoint?.DetermineIfCanExit());
        }
        private void Record(string phase, Dictionary<string, object> extra)
        {
            plugin.Awareness.HarnessRecord("quest-case", TraceWorld.Values("case", cases[index], "phase", phase,
                "state", Service.Policy?.State.ToString(), "winner", Service.Policy?.Winner,
                "snapshot", Service.Snapshot(), "extra", extra));
        }
        private void Fail(string reason)
        { Status = "FAIL"; Reason = cases[index] + ":" + reason; Record("failed", TraceWorld.Values("reason", reason)); }
        private Agent Near(int recipient)
        {
            var actor = Service.Encounter.Recipients[recipient];
            // Relocate only this disposable encounter's actor along its already validated clear approach.
            var direction = (actor.tr.position - player.tr.position).normalized;
            actor.tr.position = player.tr.position + direction * 1.2f;
            return actor;
        }
        private List<string> Menu(int recipient)
        {
            var actor = Near(recipient); actor.Interact(player);
            return new List<string>(actor.buttons);
        }
        private void Press(int recipient, string button)
        {
            if (!Service.Encounter.RecipientLive(recipient)) { Service.Submit(recipient); return; }
            var actor = Near(recipient); actor.Interact(player); actor.PressedButton(button, 0);
        }
        private bool Pickup()
        {
            var source = Service.Encounter.Source;
            int before = QuestService.Count(player, "Banana");
            source.Interact(player);
            pickupVerified = QuestService.Count(player, "Banana") == before + 1 && !Service.Encounter.SourceLive;
            Record("pickup", TraceWorld.Values("before", before, "after", QuestService.Count(player, "Banana"),
                "groundActive", Service.Encounter.SourceLive, "nativeCall", "Item.Interact"));
            return pickupVerified;
        }
        private bool Thanks(Agent actor)
        {
            foreach (var bubble in UnityEngine.Object.FindObjectsOfType<TalkText>())
                if (bubble.playfieldObject == actor && bubble.myText != null && bubble.myText.text.Contains("Thanks!")) return true;
            return false;
        }
        private void Exchange(int recipient, bool fullReward = false, bool fullRecipient = false)
        {
            var before = Service.Snapshot(recipient); var nativeBefore = NativeState();
            var database = fullReward ? player.inventory : Service.Encounter.Recipients[recipient].inventory;
            var names = new List<string>();
            if (fullReward || fullRecipient)
                foreach (var slot in database.InvItemList)
                { names.Add(slot.invItemName); if (!(fullReward && slot.invItemName == "Banana")) slot.invItemName = "HarnessOccupiedSlot"; }
            try { Press(recipient, QuestService.GiveButton(recipient)); }
            finally
            {
                if (fullReward || fullRecipient)
                    for (int i = 0; i < names.Count; i++) database.InvItemList[i].invItemName = names[i];
            }
            var after = Service.Snapshot(recipient); var nativeAfter = NativeState();
            bool thanks = Thanks(Service.Encounter.Recipients[recipient]);
            var duplicateBefore = Service.Snapshot(recipient);
            // Calls through the patched native handler must remain inert once completed, for either recipient.
            if (Service.Policy.State == QuestState.Completed)
            {
                Press(recipient, QuestService.GiveButton(recipient));
                Press(1 - recipient, QuestService.GiveButton(1 - recipient));
            }
            Record("exchange", TraceWorld.Values("recipient", recipient, "before", before, "after", after,
                "nativeBefore", nativeBefore, "nativeAfter", nativeAfter, "thanksBubble", thanks,
                "pickupVerified", pickupVerified, "duplicateBefore", duplicateBefore, "duplicateAfter", Service.Snapshot(recipient),
                "fullReward", fullReward, "fullRecipient", fullRecipient));
        }
        internal void Tick()
        {
            if (Status != null) return;
            if (stage == 2)
            {
                if (Time.realtimeSinceStartup > deadline) { Fail("native-menu-pause-timeout"); return; }
                if (pauseUntil == 0)
                {
                    if (Time.timeScale != 0) return;
                    var initial = Service.Snapshot(); initial.Remove("timeScale");
                    pauseSnapshot = JsonConvert.SerializeObject(initial); pauseTime = Time.time;
                    pauseUntil = Time.realtimeSinceStartup + .5f; return;
                }
                if (Time.realtimeSinceStartup < pauseUntil) return;
                Service.Tick();
                var snapshot = Service.Snapshot(); snapshot.Remove("timeScale");
                Record("pause", TraceWorld.Values("unchanged", pauseSnapshot == JsonConvert.SerializeObject(snapshot) && Time.time == pauseTime,
                    "timeScale", Time.timeScale));
                player.mainGUI.HideMainMenu(); stage = 1; due = Time.time + .3f; return;
            }
            if (stage == 3)
            {
                WorldFixture.AcceptReload(GameController.gameController);
                if (Time.realtimeSinceStartup > deadline) { Fail("reload-timeout"); return; }
                if (!WorldFixture.Ready(GameController.gameController) || plugin.Awareness.HarnessLevel == startingLevel) return;
                Record("scope-ended", TraceWorld.Values("oldLevel", startingLevel, "newLevel", plugin.Awareness.HarnessLevel,
                    "cleared", Service.Encounter == null, "state", Service.Policy?.State.ToString()));
                Status = Service.Encounter == null && Service.Policy?.State == QuestState.Expired ? "COMPLETE" : "FAIL";
                Reason = "banana-cases-and-native-reload"; return;
            }
            if (Time.realtimeSinceStartup > deadline) { Fail("timeout-stage-" + stage); return; }
            if (Time.timeScale <= 0 || Time.time < due) return;
            string name = cases[index];
            if (stage == 0)
            {
                var nativeBefore = NativeState();
                var buttons = Menu(0);
                if (!buttons.Contains(QuestService.AcceptButton)) { Fail("accept-menu-missing"); return; }
                Press(0, QuestService.AcceptButton);
                var buyer = Menu(0); Service.Encounter.Recipients[0].StopInteraction();
                var neighbor = Menu(1); Service.Encounter.Recipients[1].StopInteraction();
                Record("menus", TraceWorld.Values("before", buttons, "buyer", buyer, "neighbor", neighbor,
                    "nativeBefore", nativeBefore, "nativeAfter", NativeState()));
                if (Service.Policy.State != QuestState.Accepted || !buyer.Contains(QuestService.CashButton) || !neighbor.Contains(QuestService.KitButton))
                { Fail("accepted-offer-menu-missing"); return; }
                if (name == "cash" || name == "kit" || name == "full-reward" || name == "full-recipient" || name == "one-recipient-lost")
                    if (!Pickup()) { Fail("native-pickup-unconfirmed"); return; }
                if (name == "cash") { player.inventory.AddItem("Banana", 2); Exchange(0); }
                else if (name == "kit") Exchange(1);
                else if (name == "full-reward") Exchange(1, fullReward: true);
                else if (name == "full-recipient") Exchange(0, fullRecipient: true);
                else if (name == "missing-banana") Exchange(0);
                else if (name == "one-recipient-lost") Service.Encounter.Recipients[0].DestroyMe();
                else if (name == "both-recipients-lost")
                { Service.Encounter.Recipients[0].DestroyMe(); Service.Encounter.Recipients[1].DestroyMe(); }
                else if (name == "source-lost") Service.Encounter.Source.DestroyMe();
                else if (name == "abandon") Press(0, QuestService.AbandonButton);
                else if (name == "pause")
                {
                    player.mainGUI.ShowMainMenu(); pauseUntil = 0; stage = 2; return;
                }
                else if (name == "level-change")
                { WorldFixture.Reload(player.gc); stage = 3; deadline = Time.realtimeSinceStartup + 120; return; }
                stage = 1; due = Time.time + .6f;
            }
            else if (stage == 1)
            {
                if (name == "one-recipient-lost") Exchange(1);
                Record("complete", TraceWorld.Values("buyerAvailable", Service.Policy.Available(0), "neighborAvailable", Service.Policy.Available(1)));
                index++;
                if (index == cases.Length) { Status = "COMPLETE"; Reason = "banana-cases"; return; }
                Setup();
            }
        }
        internal void Close() { if (stage == 2) player.mainGUI.HideMainMenu(); Service.Close(); }
    }
}
