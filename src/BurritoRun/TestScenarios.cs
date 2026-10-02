using UnityEngine;

namespace BurritoRun
{
    // Quick-test cases run through the ordinary paid delivery path. They only
    // arrange the outcome and observe the real inventory/cash state afterward.
    internal sealed class TestScenarios
    {
        private enum Case { None, Handoff, RecoverDrop, SafetyRecovery }
        private readonly Plugin plugin;
        private Case selected;
        private GameController controller;
        private Agent player;
        private int orderId;
        private int foodBefore;
        private int moneyBefore;
        private bool running;
        private bool waitingForPickup;
        private bool awaitingSecondHandoff;
        private int peacefulHandoffs;
        private float pickupDeadline;
        private string status = "No scenario selected.";

        internal string Name => selected == Case.Handoff ? "delivery-handoff" :
            selected == Case.RecoverDrop ? "delivery-drop-recovery" :
            selected == Case.SafetyRecovery ? "safety-recovery" : "none";
        internal int RunNumber { get; private set; }
        internal string Status => status;

        internal TestScenarios(Plugin plugin) { this.plugin = plugin; }

        internal void StartHandoff(Agent customer) => Start(Case.Handoff, customer);
        internal void StartDropRecovery(Agent customer) => Start(Case.RecoverDrop, customer);
        internal void StartSafetyRecovery(Agent customer) => Start(Case.SafetyRecovery, customer);

        private void Start(Case testCase, Agent customer)
        {
            if (running) { plugin.Notice("Finish the current scenario or clear it first."); return; }
            if (!plugin.Delivery.CanOrder(customer) || plugin.Delivery.Active || !plugin.Delivery.ServiceAvailable)
            { plugin.Notice("Start on a playable level with no active order or cooldown."); return; }
            if (customer.gc.tileInfo.IsIndoors(customer.tr.position))
            { plugin.Notice("For this scenario, stand outdoors in an open delivery spot."); return; }
            var money = customer.inventory.FindItem("Money");
            int requiredMoney = testCase == Case.SafetyRecovery ? 20 : 10;
            if (money == null || money.invItemCount < requiredMoney)
            { plugin.Notice("Add test money first; this scenario needs $" + requiredMoney + " for real orders."); return; }
            if (CountFood(customer) != 0)
            { plugin.Notice("Eat or remove earlier Delivery Burritos before starting this scenario."); return; }
            if (testCase == Case.SafetyRecovery && plugin.Delivery.SafetyReports != 0)
            { plugin.Notice("Start safety recovery in a fresh run with a clear driver record."); return; }

            selected = testCase;
            RunNumber++;
            controller = customer.gc;
            player = customer;
            moneyBefore = money.invItemCount;
            foodBefore = CountFood(customer);
            waitingForPickup = false;
            awaitingSecondHandoff = false;
            peacefulHandoffs = 0;
            running = true;
            status = testCase == Case.SafetyRecovery
                ? "Test flag seeded. Stay near the order spot for two peaceful handoffs."
                : "Running " + Name + ".";
            plugin.Log("BURRITO_TEST_BEGIN money=" + moneyBefore + " food=" + foodBefore + " player=" + customer.UID + " at=" + customer.tr.position);
            if (testCase == Case.SafetyRecovery && !plugin.Delivery.SeedSafetyFlagForTest())
            { Complete("INCONCLUSIVE", "Could not seed the test-only safety flag."); return; }
            plugin.Delivery.Order(customer);
            orderId = plugin.Delivery.ActiveOrderId;
            if (orderId == 0) { Complete("INCONCLUSIVE", "Order was not accepted."); return; }
            if (Money(customer) != moneyBefore - 10)
                Complete("FAIL", "Expected a $10 charge; check money and order logs.");
        }

        internal void ContinueSafetyRecovery(Agent customer)
        {
            if (!running || selected != Case.SafetyRecovery || !awaitingSecondHandoff)
            { plugin.Notice("Start safety recovery and finish its first handoff before continuing."); return; }
            if (customer != player || customer.gc != controller || !plugin.Delivery.CanOrder(customer) ||
                plugin.Delivery.Active || !plugin.Delivery.ServiceAvailable)
            { plugin.Notice("Wait for a playable level and available courier service."); return; }
            if (controller.tileInfo.IsIndoors(customer.tr.position))
            { plugin.Notice("Stand outdoors in an open delivery spot for the second handoff."); return; }
            if (Money(customer) < 10)
            { plugin.Notice("Add $10 test money for the second real order."); return; }

            moneyBefore = Money(customer);
            foodBefore = CountFood(customer);
            awaitingSecondHandoff = false;
            status = "Second paid delivery underway. Stay near this order spot.";
            plugin.Log("BURRITO_TEST_ACTION second-handoff money=" + moneyBefore + " food=" + foodBefore);
            plugin.Delivery.Order(customer);
            orderId = plugin.Delivery.ActiveOrderId;
            if (orderId == 0) { Complete("INCONCLUSIVE", "Second order was not accepted."); return; }
            if (Money(customer) != moneyBefore - 10)
                Complete("FAIL", "Expected a $10 charge for the second order.");
        }

        internal void Tick()
        {
            if (!running) return;
            if (GameController.gameController != controller || !controller.loadComplete || player == null || player.dead)
            { Complete("INCONCLUSIVE", "Level or player changed. Start a fresh run for a clean fixture."); return; }
            if (waitingForPickup)
            {
                if (CountFood(player) > foodBefore)
                    Complete("PASS", "Dropped food entered inventory after pickup.");
                else if (Time.time >= pickupDeadline)
                    Complete("FAIL", "Food was not recovered within 45 game seconds.");
                return;
            }
            if (selected != Case.RecoverDrop || plugin.Delivery.ActiveOrderId != orderId || !plugin.Delivery.CourierHasPacket) return;
            plugin.Log("BURRITO_TEST_ACTION force-drop id=" + orderId);
            plugin.Delivery.ForceDrop();
        }

        internal void ObserveResult(int completedOrderId, string outcome)
        {
            if (!running || completedOrderId != orderId) return;
            if (GameController.gameController != controller || !controller.loadComplete)
            { Complete("INCONCLUSIVE", "Level changed before the scenario completed."); return; }
            if (selected == Case.Handoff)
            {
                if ((outcome == "delivered" || outcome == "met-driver") && CountFood(player) > foodBefore)
                    Complete("PASS", "Paid handoff put one burrito in inventory.");
                else Complete("FAIL", "Expected inventory handoff; outcome=" + outcome + " food=" + CountFood(player));
            }
            else if (selected == Case.SafetyRecovery)
            {
                if ((outcome != "delivered" && outcome != "met-driver") || CountFood(player) <= foodBefore)
                { Complete("FAIL", "Expected a real inventory handoff; outcome=" + outcome); return; }
                peacefulHandoffs++;
                int expectedReports = peacefulHandoffs == 1 ? 1 : 0;
                int expectedRecovery = peacefulHandoffs == 1 ? 1 : 0;
                if (plugin.Delivery.SafetyReports != expectedReports ||
                    plugin.Delivery.PeacefulHandoffs != expectedRecovery)
                {
                    Complete("FAIL", "Safety record mismatch after handoff " + peacefulHandoffs +
                        ": reports=" + plugin.Delivery.SafetyReports +
                        " recovery=" + plugin.Delivery.PeacefulHandoffs);
                    return;
                }
                plugin.Log("BURRITO_TEST_CHECK handoffs=" + peacefulHandoffs +
                    " reports=" + expectedReports + " recovery=" + expectedRecovery);
                if (peacefulHandoffs == 2)
                    Complete("PASS", "Two paid handoffs cleared the test-only safety flag.");
                else
                {
                    orderId = 0;
                    awaitingSecondHandoff = true;
                    status = "1/2 peaceful handoffs. Click Continue safety recovery ($10).";
                    plugin.Notice(status);
                }
            }
            else if (outcome == "test-drop")
            {
                waitingForPickup = true;
                pickupDeadline = Time.time + 45f;
                status = "Drop made. Press E on the FUD can within 45 game seconds.";
                plugin.Log("BURRITO_TEST_WAIT pickupDeadline=" + pickupDeadline);
                plugin.Notice(status);
            }
            else Complete("FAIL", "Expected forced drop; outcome=" + outcome);
        }

        internal void Clear()
        {
            if (running) Complete("INCONCLUSIVE", "Scenario cleared; existing world and order are unchanged.");
            selected = Case.None;
            status = "Tracking cleared; orders, food and any test safety flag remain. New run = clean world.";
            plugin.Log("BURRITO_TEST_CLEAR worldUnchanged=True");
            plugin.Notice(status);
        }

        private void Complete(string result, string reason)
        {
            status = result + ": " + reason;
            running = false;
            plugin.Log("BURRITO_TEST_RESULT result=" + result + " order=" + orderId +
                " money=" + Money(player) + " food=" + CountFood(player) +
                " reports=" + plugin.Delivery.SafetyReports +
                " recovery=" + plugin.Delivery.PeacefulHandoffs + " reason=" + reason);
            plugin.Notice(status);
        }

        private static int Money(Agent agent) => agent?.inventory?.FindItem("Money")?.invItemCount ?? 0;
        private static int CountFood(Agent agent) => agent?.inventory?.FindItem(Names.Food)?.invItemCount ?? 0;
    }
}
