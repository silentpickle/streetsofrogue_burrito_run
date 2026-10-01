using UnityEngine;

namespace BurritoRun
{
    internal sealed class ServicesPhone
    {
        private readonly Plugin plugin;
        private Agent customer;
        private float inputBlockedUntil;
        private float nextProvision;
        internal bool IsOpen => customer != null;
        internal bool BlocksInput => IsOpen || Time.realtimeSinceStartup < inputBlockedUntil;

        internal ServicesPhone(Plugin plugin) { this.plugin = plugin; }

        internal void Tick()
        {
            var player = GameController.gameController?.playerAgent;
            if (IsOpen && (customer != player || !plugin.Delivery.CanOrder(customer))) Close();
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (IsOpen) Close();
                else if (plugin.Delivery.CanOrder(player)) Open(player);
            }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
            if (Time.unscaledTime < nextProvision || !plugin.Delivery.CanOrder(player)) return;
            nextProvision = Time.unscaledTime + 2;
            if (player.inventory.FindItem(Names.Phone) != null) return;
            var phone = new InvItem { invItemName = Names.Phone };
            phone.SetupDetails(false);
            if (!player.inventory.hasEmptySlotForItem(phone)) return;
            if (player.inventory.AddItem(Names.Phone, 1) != null)
                plugin.Log("BURRITO_PHONE provisioned player=" + player.UID);
        }

        internal void Open(Agent player)
        {
            if (!plugin.Delivery.CanOrder(player)) return;
            player.mainGUI.HideEverything();
            customer = player;
            player.gc.SetTimeScale();
            plugin.Log("BURRITO_PHONE opened player=" + player.UID);
        }

        internal void Close()
        {
            var gc = customer?.gc;
            customer = null;
            plugin.Inspector.Hide();
            inputBlockedUntil = Time.realtimeSinceStartup + 0.2f;
            if (gc != null) gc.SetTimeScale();
            plugin.Log("BURRITO_PHONE closed");
        }

        internal void Draw()
        {
            if (!IsOpen) return;
            if (plugin.Inspector.Visible) { plugin.Inspector.Draw(); return; }
            var width = Mathf.Min(360, Screen.width - 30);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 30, width, Mathf.Min(510, Screen.height - 60)), "SERVICES", GUI.skin.window);
            GUILayout.Label("Burrito Run " + Plugin.BuildLabel);
            GUILayout.Label("Your city. Questionable convenience.");
            GUILayout.Space(15);
            GUILayout.Label("FOOD DELIVERY - $10");
            GUILayout.Label("One physical burrito. One actual courier.\nDelivered to the spot where you order.");
            GUILayout.Space(10);
            GUILayout.Label(plugin.Delivery.ServiceStatus);
            GUILayout.Label(plugin.Delivery.Description);
            GUILayout.Label("Last order: " + plugin.Delivery.LastOutcome);
            var money = customer.inventory.FindItem("Money");
            GUILayout.Label("Wallet: $" + (money?.invItemCount ?? 0));
            GUILayout.Space(10);
            GUI.enabled = !plugin.Delivery.Active && plugin.Delivery.ServiceAvailable && (money?.invItemCount ?? 0) >= 10;
            if (GUILayout.Button("Order burrito here ($10)", GUILayout.Height(40))) plugin.Delivery.Order(customer);
            GUI.enabled = true;
            GUILayout.Space(15);
            GUILayout.Label("More services will live here as they become playable.\nGame paused - close phone to let the courier travel.");
            if (GUILayout.Button("Close phone (F2 / Esc)", GUILayout.Height(35))) Close();
            GUILayout.EndArea();
        }
    }
}
