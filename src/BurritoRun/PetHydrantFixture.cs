using System;
using System.Collections.Generic;

namespace BurritoRun
{
    // Owned test mutations only; discovery and actual response run through production services.
    internal sealed class PetHydrantFixture
    {
        private readonly PetFoodFixture dog;
        private readonly Agent player;
        private FireHydrant hydrant;
        private int uid;
        private int instance;
        internal FireHydrant Hydrant => hydrant;
        internal PetHydrantFixture(Plugin plugin, Agent player)
        { this.player = player; dog = new PetFoodFixture(plugin, player); }
        internal void Spawn(Agent pet)
        {
            Remove();
            hydrant = Place(player, pet);
            if (hydrant == null) throw new InvalidOperationException("No native hydrant fixture");
            uid = hydrant.UID; instance = hydrant.GetInstanceID();
        }
        internal static FireHydrant Place(Agent player, Agent pet) =>
            WorldFixture.Object(player, "FireHydrant", WorldFixture.ObjectSpot(pet, new List<PlayfieldObject>())) as FireHydrant;
        internal static void StopDog(Agent pet)
        {
            pet.startingPosition = pet.tr.position;
            pet.job = pet.jobBig = ""; pet.jobCode = pet.jobBigCode = jobType.None;
            pet.SetFollowing(null); pet.SetDefaultGoal("DoNothing");
        }
        internal void Remove()
        {
            if (hydrant != null && hydrant.GetInstanceID() == instance) WorldFixture.RemoveObject(hydrant, uid);
            hydrant = null;
        }
        internal void Close() { Remove(); dog.Close(); }
    }
}
