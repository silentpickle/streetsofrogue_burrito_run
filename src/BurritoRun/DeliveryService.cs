using UnityEngine;

namespace BurritoRun
{
    internal sealed class DeliveryService
    {
        private readonly Plugin plugin;
        private DeliveryRequest current;
        private int sequence;
        private float nextTick;
        private float serviceSuspendedUntil;
        private Agent recentCourier;
        private Agent recentCustomer;
        private int recentCourierId;
        private float recentCourierUntil;
        private bool recentCourierHit;
        private int pendingRefund;
        private readonly CustomerReputation reputation = new CustomerReputation();
        internal string AccountStatus => reputation.Reports == 0
            ? "Driver safety record: clear."
            : $"Driver safety flags: {reputation.Reports}. Drivers may carry protection.\nTwo peaceful handoffs clear one flag ({reputation.PeacefulHandoffs}/2).";
        internal string LastOutcome = "none";
        internal Agent Courier => current?.Courier;
        internal Vector3? Destination => current?.Target;
        internal bool Active => current != null;
        internal int ActiveOrderId => current?.Id ?? 0;
        internal bool CourierHasPacket => current?.Courier != null && current.Packet != null &&
            current.Courier.inventory.InvItemList.Contains(current.Packet) && current.Packet.invItemCount > 0;
        internal bool ServiceAvailable => Time.time >= serviceSuspendedUntil;
        internal int SafetyReports => reputation.Reports;
        internal int PeacefulHandoffs => reputation.PeacefulHandoffs;
        internal string ServiceStatus => Time.time < serviceSuspendedUntil
            ? $"Courier service paused for {Mathf.CeilToInt(serviceSuspendedUntil - Time.time)}s after an assault."
            : "Courier service is taking orders.";
        internal string Description => current == null ? "No active order." :
            (current.Panicked ? "Courier is fleeing. Watch for the dropped food." :
             current.Route?.WaitingForCustomer == true ? $"Meet your driver near ({current.MeetingPoint.x:0}, {current.MeetingPoint.y:0}); food drops after 20 game seconds." :
             current.Courier == null ? (current.Entrance != null
                 ? "Kitchen accepted your order. Meet the driver outside the building entrance."
                 : "Kitchen accepted your order. Stay near the order spot.") :
             current.Entrance != null ? "Courier is heading to the building entrance. Come outside to meet them." :
             "Courier is heading to where you placed the order, not following you.") +
            (plugin.TestMode ? "\nQuick test: three-second kitchen delay." : "");

        internal DeliveryService(Plugin plugin) { this.plugin = plugin; }

        /// <summary>
        /// Check whether a live local player is in a supported world for delivery.
        /// The service depends on single-player ownership and an initialized inventory, not menu or
        /// multiplayer actors.
        /// </summary>
        internal bool CanOrder(Agent player)
        {
            var gc = GameController.gameController;
            return gc != null && gc.loadComplete && gc.serverPlayer && !gc.multiplayerMode &&
                !gc.coopMode && !gc.fourPlayerMode &&
                gc.levelType != "HomeBase" && gc.levelType != "Attract" && gc.levelType != "Tutorial" &&
                player != null && player.isPlayer > 0 && player.localPlayer && player.didStartReal &&
                !player.dead && !player.ghost && player.inventory != null;
        }

        /// <summary>
        /// Validate payment and availability, choose a handoff address, and queue one paid
        /// delivery.
        /// An order belongs to the original address and world; later player movement must not
        /// silently retarget it.
        /// </summary>
        internal void Order(Agent player)
        {
            if (!CanOrder(player)) { plugin.Notice("Start a single-player run to place an order."); return; }
            if (current != null) { plugin.Notice("One delivery at a time. Your courier is still working on it."); return; }
            if (Time.time < serviceSuspendedUntil) { plugin.Notice(ServiceStatus); return; }
            var money = player.inventory.FindItem("Money");
            if (money == null || money.invItemCount < 10) { plugin.Notice("You need $10. No money was taken."); return; }
            var gc = player.gc;
            Vector3 target = player.tr.position;
            Door entrance;
            Vector3 meetingPoint;
            bool entranceDelivery = DeliveryDestination.TryFindEntrance(gc, target, out meetingPoint, out entrance);
            // The target is the player's walkable location, never the computer collider.
            int id = ++sequence;
            var random = new System.Random(unchecked(gc.seedNum * 397 ^ id));
            float delay = plugin.TestMode ? 3 : random.Next(45, 91);
            current = new DeliveryRequest
            {
                Id = id,
                Controller = gc,
                Player = player,
                PlayerId = player.UID,
                Target = entranceDelivery ? meetingPoint : target,
                Entrance = entrance,
                Armed = reputation.ShouldArm(plugin.TestMode, random.NextDouble()),
                Due = Time.time + delay,
                Created = Time.time
            };
            player.inventory.SubtractFromItemCount(money, 10);
            current.Paid = true;
            LastOutcome = "queued";
            plugin.Log($"BURRITO_ORDER id={id} seed={gc.seedNum} target={target} delay={delay}");
            plugin.Log($"BURRITO_DRIVER_RISK id={id} reports={reputation.Reports} armed={current.Armed} chance={(plugin.TestMode && reputation.Reports > 0 ? 1 : reputation.ArmedChance)}");
            if (entranceDelivery)
                plugin.Log($"BURRITO_ENTRANCE id={id} door={entrance.UID} address={target} handoff={meetingPoint}");
            plugin.Notice(plugin.TestMode ? "Order accepted: $10. Courier in 3 seconds." : "Order accepted: $10. Courier in 45-90 seconds. Delivery to THIS spot.");
            if (entranceDelivery) plugin.Notice("Order accepted: $10. Indoor order: meet your driver OUTSIDE the building entrance.");
        }

        /// <summary>
        /// Advance courier spawn, routing, handoff, drops, interruption and refund handling.
        /// Game-time deadlines pause with the world, while bounded route recovery avoids stranded
        /// orders.
        /// </summary>
        internal void Tick()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.2f;
            var gc = GameController.gameController;
            if (gc != null && (gc.returningToMainMenu || gc.levelType == "HomeBase" || gc.levelType == "Attract" ||
                gc.levelType == "Tutorial" || gc.playerAgent != null && (gc.playerAgent.dead || gc.playerAgent.ghost)))
            {
                if (reputation.Reports > 0)
                {
                    reputation.Reset();
                    serviceSuspendedUntil = 0;
                    recentCourier = null;
                    plugin.Log("BURRITO_REPUTATION reset=run-ended");
                }
            }
            if (recentCourier != null && (Time.time > recentCourierUntil || recentCourier.dead ||
                recentCourier.UID != recentCourierId || recentCourier.gc != gc))
                recentCourier = null;
            if (current != null && (gc == null || gc != current.Controller || !gc.loadComplete))
            {
                Cancel("Level changed; undelivered order refunded.", true);
                return;
            }
            if (current == null)
            {
                if (pendingRefund > 0 && CanOrder(gc?.playerAgent))
                {
                    gc.playerAgent.inventory.AddItemOrDrop("Money", pendingRefund);
                    pendingRefund = 0;
                }
                return;
            }
            var order = current;
            if (order.Player == null || order.Player.UID != order.PlayerId || order.Player.dead || order.Player.ghost)
            { Cancel("Order ended when the customer died.", false); return; }
            // Time.time pauses with the game, so pause menus do not consume delivery time.
            if (order.Courier == null)
            {
                // A destroyed Unity object compares equal to null. Never spawn a replacement
                // after the original courier (and possibly its recoverable package) existed.
                if (order.SpawnedOnce)
                { Cancel("Courier disappeared. Check the street for the package; refunded if no food existed.", order.Packet == null); return; }
                if (Time.time < order.Due) return;
                Spawn(order);
                return;
            }
            if (!order.Courier.gameObject.activeInHierarchy || order.Courier.UID != order.CourierId)
            { Cancel("Courier disappeared. Check the street for the package.", order.Packet == null); return; }
            if (order.Courier.dead || order.Courier.knockedOut)
            {
                // Native death/knockout spills inventory. Never create a second package.
                if (order.Packet == null) Cancel("Courier down before receiving the food; refunded $10.", true);
                else Finish("interrupted", "Courier down! Recover the burrito from the scene.");
                return;
            }
            if (order.Packet == null)
            {
                if (!order.Courier.didStartReal || Time.time - order.Spawned < 0.75f) return;
                order.Packet = order.Courier.inventory.AddItem(Names.Food, 1);
                if (order.Packet == null) { Cancel("Kitchen lost the package; refunded.", true); return; }
                if (order.Panicked) return;
                if (order.Armed) CourierLoadout.TryEquipSidearm(plugin, order.Courier, order.Id);
                order.Courier.agentInteractions.GoHere(order.Courier, order.Player, order.Target);
                order.Route = new DeliveryRoute(Time.time, Vector2.Distance(order.Courier.tr.position, order.Target),
                    order.Entrance != null ? 0.45f : DeliveryRoute.ArrivalRadius);
                plugin.Log($"BURRITO_ROUTE id={order.Id} courier={order.CourierId} from={order.Courier.tr.position} to={order.Target}");
                return;
            }
            if (!order.Courier.inventory.InvItemList.Contains(order.Packet) || order.Packet.invItemCount < 1)
            { Finish("intercepted", "The courier no longer has the burrito. Someone got there first."); return; }
            if (order.Panicked)
            {
                if (Time.time < order.PanicDropDue) return;
                order.Courier.inventory.DropItem(order.Packet);
                Finish("panicked", "Courier fled and dropped your burrito. Chase down the package!");
                return;
            }
            order.DoorInteraction.TryKnockAtDestination(order.Courier, order.Target);
            float distance = Vector2.Distance(order.Courier.tr.position, order.Target);
            bool customerHere = Vector2.Distance(order.Player.tr.position, order.Courier.tr.position) < 1.5f;
            if (order.Entrance != null && !order.Entrance.open && order.Controller.tileInfo.IsIndoors(order.Player.tr.position))
                customerHere = false; // Do not hand food through a closed entrance door.
            bool reachedEntrance = order.Entrance != null && distance < 0.45f;
            bool beginMeeting = reachedEntrance
                ? order.Route.BeginMeeting(Time.time, "entrance-arrival")
                : order.Route.NeedsMeeting(Time.time, distance, order.Courier.jobCode == jobType.GoHere);
            if (beginMeeting)
            {
                // The courier's current occupied tile is our conservative meeting point.
                // Do not guess an exterior/door tile or teleport through walls.
                order.MeetingPoint = order.Courier.tr.position;
                order.Courier.agentInteractions.GoHere(order.Courier, order.Player, order.MeetingPoint);
                plugin.Log($"BURRITO_ROUTE_FALLBACK id={order.Id} reason={order.Route.FallbackReason} at={order.MeetingPoint} target={order.Target} distance={distance:0.0}");
                plugin.Notice(reachedEntrance
                    ? "Driver: I'm at the entrance! Come meet me. I'll wait 20 seconds, then leave the food here."
                    : "Driver: Can't get there. Come meet me! I'll wait 20 seconds, then leave the food here.");
            }
            if (order.Route.WaitingForCustomer)
            {
                if (customerHere && order.Player.inventory.hasEmptySlotForItem(order.Packet))
                {
                    order.Courier.inventory.GiveItem(order.Packet, order.Player);
                    Finish("met-driver", "You met the driver and received your burrito.");
                }
                else if (order.Route.MeetingExpired(Time.time))
                {
                    order.Courier.inventory.DropItem(order.Packet);
                    plugin.Log($"BURRITO_ROUTE_DROP id={order.Id} at={order.Courier.tr.position}");
                    Finish("stranded", "Driver left the food at the meeting spot. Recover it with E. You can order again.");
                }
                return;
            }
            if (order.Entrance == null && distance < DeliveryRoute.ArrivalRadius)
            {
                if (customerHere && order.Player.inventory.hasEmptySlotForItem(order.Packet))
                {
                    order.Courier.inventory.GiveItem(order.Packet, order.Player);
                    Finish("delivered", "Burrito delivered to your inventory. Enjoy the consequences.");
                }
                else
                {
                    order.Courier.inventory.DropItem(order.Packet);
                    Finish("dropped", "Burrito left at the original order spot. Pick it up before somebody else does.");
                }
            }
        }

        private void Spawn(DeliveryRequest order)
        {
            var gc = order.Controller;
            Vector2 candidate = gc.tileInfo.FindLocationNearLocation(order.Target, null, 4f, 8f, true, true);
            if (Vector2.Distance(candidate, order.Target) < 2f)
            {
                if (++order.SpawnAttempts < 3) { order.Due = Time.time + 1; return; }
                Cancel("No safe courier entrance found. Refunded $10.", true);
                return;
            }
            order.Courier = gc.spawnerMain.SpawnAgent(candidate, (PlayfieldObject)null, "Courier");
            if (order.Courier == null) { Cancel("Courier could not spawn. Refunded $10.", true); return; }
            order.CourierId = order.Courier.UID;
            order.DoorInteraction = new CourierDoorInteraction();
            order.SpawnedOnce = true;
            order.Spawned = Time.time;
            order.Courier.SetDefaultGoal("WanderFar");
            plugin.Notice("Your courier has entered the street. Delivery to the original order spot.");
            plugin.Log($"BURRITO_SPAWN id={order.Id} courier={order.CourierId} at={candidate}");
        }

        /// <summary>
        /// Request an explicit test drop of the courier's existing package.
        /// Exercising the ordinary ground-recovery path is more useful than reporting a synthetic
        /// handoff.
        /// </summary>
        internal void ForceDrop()
        {
            if (current?.Courier == null || current.Packet == null) { plugin.Notice("Wait until the courier has the package."); return; }
            current.Courier.inventory.DropItem(current.Packet);
            Finish("test-drop", "TEST: Courier dropped the package. Go recover it.");
        }

        /// <summary>
        /// Seed one labeled reputation flag only in developer mode with a clear record.
        /// Recovery checks can then exercise paid peaceful handoffs without staging an assault.
        /// </summary>
        internal bool SeedSafetyFlagForTest()
        {
            if (!plugin.TestMode || reputation.Reports != 0) return false;
            reputation.RecordAssault();
            plugin.Log("BURRITO_REPUTATION action=test-seed reports=" + reputation.Reports);
            return true;
        }

        /// <summary>
        /// Recognize the active courier's first observed native knock and report it.
        /// Native knocking may happen before our optional destination knock, so the two paths must
        /// share suppression.
        /// </summary>
        internal void OnCourierKnocked(Door door, Agent courier)
        {
            var order = current;
            if (order == null || courier == null || door == null || courier != order.Courier ||
                courier.UID != order.CourierId || order.Panicked ||
                order.DoorInteraction?.ObserveNativeKnock() != true) return;
            plugin.Log($"BURRITO_DOOR_KNOCK id={order.Id} courier={courier.UID} door={door.UID} at={door.tr.position}");
            plugin.Notice("Driver: Knock knock. Food delivery! Nearby residents may answer.");
        }

        /// <summary>
        /// Apply the service's assault consequences when its customer hits an active or recently
        /// released courier.
        /// The bounded identity/time checks avoid turning unrelated world combat into a
        /// driver-safety report.
        /// </summary>
        internal void OnCourierHit(Agent victim, Agent attacker)
        {
            if (victim == null || attacker == null || !attacker.localPlayer || attacker.isPlayer <= 0) return;
            var order = current;
            bool activeHit = order != null && victim == order.Courier && victim.UID == order.CourierId && attacker == order.Player;
            bool recentHit = !activeHit && victim == recentCourier && victim.UID == recentCourierId &&
                attacker == recentCustomer && Time.time <= recentCourierUntil && !recentCourierHit;
            if (!activeHit && !recentHit) return;
            if (activeHit && order.Panicked) return;
            if (recentHit) recentCourierHit = true;
            reputation.RecordAssault();
            plugin.Log($"BURRITO_REPUTATION action=assault reports={reputation.Reports}");
            if (activeHit)
            {
                order.Panicked = true;
                order.PanicDropDue = Time.time + 5f;
            }
            // Native goal arbitration gives mustFlee priority over fighting this attacker.
            victim.wontFlee = false;
            victim.mustFlee = true;
            serviceSuspendedUntil = Mathf.Max(serviceSuspendedUntil, Time.time + (plugin.TestMode ? 20f : 45f));
            plugin.Log($"BURRITO_COURIER_HIT id={order?.Id} courier={victim.UID} phase={(activeHit ? "carrying" : "after-delivery")} cooldown={serviceSuspendedUntil - Time.time:0}");
            plugin.Notice(activeHit
                ? "You hit the courier! They're fleeing with your food. Service is paused."
                : "You hit the courier! Service is pausing future orders.");
        }

        /// <summary>
        /// Clear the legacy delivery job and return its surviving courier to native wandering.
        /// This older service uses dedicated courier cleanup, not the newer pickup probe's
        /// exact-goal lease contract.
        /// </summary>
        private void ReleaseCourier(DeliveryRequest order)
        {
            if (order.Courier == null || order.Courier.UID != order.CourierId || order.Courier.dead) return;
            order.Courier.job = "";
            order.Courier.jobBig = "";
            order.Courier.jobCode = jobType.None;
            order.Courier.jobBigCode = jobType.None;
            order.Courier.SetDefaultGoal("WanderFar");
        }

        /// <summary>
        /// Close the order, release the courier and record its terminal outcome.
        /// Only inventory handoff outcomes count toward peaceful recovery; a dropped or intercepted
        /// package does not.
        /// </summary>
        private void Finish(string outcome, string message)
        {
            var order = current;
            current = null;
            LastOutcome = outcome;
            if (order != null && (outcome == "delivered" || outcome == "met-driver"))
            {
                reputation.RecordPeacefulHandoff();
                plugin.Log($"BURRITO_REPUTATION action=peaceful-handoff reports={reputation.Reports} recovery={reputation.PeacefulHandoffs}");
                recentCourier = order.Courier;
                recentCustomer = order.Player;
                recentCourierId = order.CourierId;
                recentCourierUntil = Time.time + 30f;
                recentCourierHit = false;
            }
            if (order != null) ReleaseCourier(order);
            plugin.Log($"BURRITO_RESULT id={order?.Id} outcome={outcome}");
            if (plugin.TestMode && order != null) plugin.Scenarios.ObserveResult(order.Id, outcome);
            plugin.Notice(message);
        }

        /// <summary>
        /// End an order and, when allowed, return its payment now or defer the refund.
        /// A refund may outlive the old level or player reference, so it cannot always be paid
        /// immediately.
        /// </summary>
        private void Cancel(string message, bool refund)
        {
            var order = current;
            if (order == null) return;
            // Refund only before a package has entered the world, or when the entire level unloads.
            if (refund && order.Paid)
            {
                if (order.Player != null && order.Player.UID == order.PlayerId && !order.Player.dead && order.Controller == GameController.gameController)
                    order.Player.inventory.AddItemOrDrop("Money", 10);
                else pendingRefund += 10;
            }
            Finish("cancelled", message);
        }

        /// <summary>
        /// Try to close a broken delivery without leaving the service permanently occupied.
        /// Refund eligibility depends on whether food already entered the world; an exception must
        /// not duplicate it.
        /// </summary>
        internal void AbortAfterError()
        {
            if (current == null) return;
            try { Cancel("Delivery stopped after an error; see the session log.", current.Packet == null); }
            catch { current = null; }
        }

        private sealed class DeliveryRequest
        {
            internal int Id, PlayerId, CourierId, SpawnAttempts;
            internal GameController Controller;
            internal Agent Player, Courier;
            internal InvItem Packet;
            internal Vector3 Target;
            internal Vector3 MeetingPoint;
            internal DeliveryRoute Route;
            internal CourierDoorInteraction DoorInteraction;
            internal Door Entrance;
            internal float Due, Created, Spawned;
            internal float PanicDropDue;
            internal bool Paid, SpawnedOnce, Panicked, Armed;
        }
    }
}
