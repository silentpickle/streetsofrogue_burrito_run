using System;
using System.Collections.Generic;

namespace BurritoRun
{
    // Disposable test mutations only. PetService owns the real response; awareness only reads.
    internal sealed class PetFoodFixture
    {
        private readonly Agent player;
        private Agent actor;
        private int actorUid;
        private int actorInstance;
        internal Item Food { get; private set; }
        private int foodUid;
        private int foodInstance;
        internal bool OwnedActor => actor != null && actor.UID == actorUid && actor.GetInstanceID() == actorInstance && WorldNative.Active(actor);
        internal PetFoodFixture(Plugin plugin, Agent player)
        {
            this.player = player;
            plugin.Pets.SpawnTestPet(player);
            actor = plugin.Pets.Actor;
            if (actor == null) throw new InvalidOperationException("No dog fixture spot");
            actorUid = actor.UID; actorInstance = actor.GetInstanceID();
        }
        internal void SpawnFood(bool stack = false)
        {
            if (!OwnedActor) throw new InvalidOperationException("Dog fixture activation lost");
            RemoveFood();
            var spot = WorldFixture.ObjectSpot(actor, new List<PlayfieldObject>());
            Food = (Item)WorldFixture.Object(player, Names.Food, spot);
            if (Food == null) throw new InvalidOperationException("No food fixture");
            foodUid = Food.UID; foodInstance = Food.GetInstanceID();
            if (stack) { Food.invItem.invItemCount = 2; Food.itemCount = 2; }
        }
        internal void Injure()
        {
            if (!OwnedActor) throw new InvalidOperationException("Dog fixture activation lost");
            actor.statusEffects.ChangeHealth(-Math.Min(15, actor.health / 2));
        }
        internal void RemoveFood()
        {
            if (Food != null && Food.GetInstanceID() == foodInstance) WorldFixture.RemoveObject(Food, foodUid);
            Food = null;
        }
        internal bool RejectSecondSpawn(Plugin plugin)
        {
            plugin.Pets.SpawnTestPet(player);
            if (plugin.Pets.Actor == null) return true;
            // A failed limit check still owns this test-created actor; do not leak it.
            actor = plugin.Pets.Actor; actorUid = actor.UID; actorInstance = actor.GetInstanceID();
            return false;
        }
        internal void Close()
        {
            RemoveFood();
            if (OwnedActor) actor.DestroyMe();
        }
    }
}
