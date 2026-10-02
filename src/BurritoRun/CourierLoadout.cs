using System;

namespace BurritoRun
{
    internal static class CourierLoadout
    {
        internal static void TryEquipSidearm(Plugin plugin, Agent courier, int orderId)
        {
            try
            {
                var weapon = courier.inventory.AddItem("Pistol", 12);
                if (weapon == null) { plugin.Log("BURRITO_ARM_FAILED id=" + orderId); return; }
                courier.inventory.EquipWeapon(weapon);
                plugin.Log($"BURRITO_ARMED id={orderId} courier={courier.UID} weapon={weapon.invItemName} ammo={weapon.invItemCount} equipped={courier.inventory.equippedWeapon == weapon}");
                plugin.Notice("Your driver has requested hazard pay. They brought protection. Delivery still costs $10.");
            }
            catch (Exception error)
            {
                // An optional loadout failure must not lose the customer's food/order.
                plugin.Error(error);
                plugin.Log("BURRITO_ARM_FAILED id=" + orderId);
            }
        }
    }
}
