using UnityEngine;

namespace BurritoRun
{
    internal sealed class ServicesPhone
    {
        private readonly Plugin plugin;
        private Agent customer;
        private float inputBlockedUntil;
        private float nextProvision;
        private enum PhonePage { Home, Food, Account }
        private PhonePage page;
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
            page = PhonePage.Home;
            player.gc.SetTimeScale();
            plugin.Log("BURRITO_PHONE opened player=" + player.UID);
        }

        internal void Close()
        {
            var gc = customer?.gc;
            customer = null;
            plugin.Inspector.Hide();
            plugin.World.Hide();
            inputBlockedUntil = Time.realtimeSinceStartup + 0.2f;
            if (gc != null) gc.SetTimeScale();
            plugin.Log("BURRITO_PHONE closed");
        }

        internal void Draw()
        {
            if (!IsOpen) return;
            if (plugin.Inspector.Visible) { plugin.Inspector.Draw(); return; }
            if (plugin.World.Visible) { plugin.World.Draw(); return; }
            var width = Mathf.Min(360, Screen.width - 30);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 30, width, Mathf.Min(510, Screen.height - 60)), "SERVICES", GUI.skin.window);
            GUILayout.Label("Burrito Run " + Plugin.BuildLabel);
            GUILayout.Label("Your city. Questionable convenience.");
            var money = customer.inventory.FindItem("Money");
            GUILayout.Label("Wallet: $" + (money?.invItemCount ?? 0));
            GUILayout.Space(15);
            switch (page)
            {
                case PhonePage.Home: DrawHome(); break;
                case PhonePage.Food: DrawFood(money?.invItemCount ?? 0); break;
                case PhonePage.Account: DrawAccount(); break;
            }
            GUILayout.Space(15);
            GUILayout.Label("Game paused - close phone to let the city move.");
            if (GUILayout.Button("Close phone (F2 / Esc)", GUILayout.Height(35))) Close();
            GUILayout.EndArea();
        }

        private void DrawHome()
        {
            GUILayout.Label("APPS");
            if (GUILayout.Button("Food Delivery\nActual drivers. Actual consequences.", GUILayout.Height(60))) page = PhonePage.Food;
            if (GUILayout.Button("My Account\nWhat the drivers are saying about you.", GUILayout.Height(60))) page = PhonePage.Account;
            GUILayout.Space(10);
            GUILayout.Label(plugin.Delivery.Active ? "Delivery in progress. Open Food Delivery for status." : plugin.Delivery.ServiceStatus);
            GUILayout.Label(plugin.Delivery.AccountStatus);
        }

        private void DrawFood(int money)
        {
            GUILayout.Label("FOOD DELIVERY - $10");
            GUILayout.Label("One physical burrito. One actual courier.\nIndoor orders prefer an outside entrance handoff.");
            GUILayout.Space(10);
            GUILayout.Label(plugin.Delivery.ServiceStatus);
            GUILayout.Label(plugin.Delivery.Description);
            GUILayout.Label("Last order: " + plugin.Delivery.LastOutcome);
            GUILayout.Label(plugin.Delivery.AccountStatus);
            GUILayout.Space(10);
            GUI.enabled = !plugin.Delivery.Active && plugin.Delivery.ServiceAvailable && money >= 10;
            if (GUILayout.Button("Order burrito here ($10)", GUILayout.Height(40))) plugin.Delivery.Order(customer);
            GUI.enabled = true;
            GUILayout.Space(15);
            if (GUILayout.Button("Home", GUILayout.Height(30))) page = PhonePage.Home;
        }

        private void DrawAccount()
        {
            GUILayout.Label("MY ACCOUNT");
            GUILayout.Label(plugin.Delivery.AccountStatus);
            GUILayout.Space(10);
            GUILayout.Label("Assault reports increase the chance of armed drivers.\nNo automatic hostility. No additional delivery fee.\nPeaceful handoffs gradually rebuild trust.\nThis account resets when the run ends.");
            if (plugin.TestMode) GUILayout.Label("Quick test: a safety flag guarantees an armed replacement.");
            if (GUILayout.Button("Home", GUILayout.Height(30))) page = PhonePage.Home;
        }
    }
}
