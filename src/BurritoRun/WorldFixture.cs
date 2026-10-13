using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // Test-only native writes live here, never in WorldAwareness/WorldNative.
    // Own exactly one spawned actor; never reset saves, teleport residents or alter the player.
    internal sealed class WorldFixture
    {
        internal Agent Actor { get; private set; }
        internal bool OwnedActive => Actor != null && Actor.GetInstanceID() == ownedInstance && Actor.UID == ownedUid && WorldNative.Active(Actor);
        private int ownedInstance;
        private int ownedUid;
        private float nextMenuAction;
        private bool selectedCharacter;
        private bool confirmedStart;
        private bool acceptedCharacter;

        internal static void ConfigureCollection() => Application.runInBackground = true;

        internal static bool Ready(GameController gc) => gc != null && gc.loadComplete && gc.loadCompleteReally && !gc.levelTransitioning &&
            !gc.returningToMainMenu && gc.playerAgent != null && !gc.playerAgent.dead &&
            gc.levelType != "HomeBase" && gc.levelType != "Attract";

        internal string AdvanceMenu(GameController gc)
        {
            if (gc?.menuGUI == null || !gc.didWaitForRealStart || Time.realtimeSinceStartup < nextMenuAction) return null;
            var menu = gc.menuGUI;
            nextMenuAction = Time.realtimeSinceStartup + 3;
            if ((menu.titleScreen || gc.levelType == "HomeBase" && gc.loadCompleteReally) && !selectedCharacter &&
                WorldNative.Read(menu, "curSelected") != null)
            {
                if (!menu.titleScreen) menu.OpenMainMenu();
                menu.PressedGameStartMenu();
                menu.PressedQuickStart();
                menu.PressedCharSelected("Soldier");
                selectedCharacter = true;
                return "native-quick-start-character-Soldier";
            }
            if (selectedCharacter && !confirmedStart && menu.characterSelect.activeInHierarchy)
            {
                confirmedStart = true;
                menu.PressedStartGame(1);
                return "native-quick-start-confirm";
            }
            var selection = gc.mainGUI?.characterSelectScript;
            if (confirmedStart && !acceptedCharacter && gc.mainGUI?.characterSelect?.activeInHierarchy == true &&
                selection?.selectedInvSlot?[0] != null && !selection.isChoiceAccepted())
            {
                acceptedCharacter = true;
                selection.AcceptChoice(0);
                return "native-character-choice-accepted";
            }
            return null;
        }

        internal bool Spawn(Agent player)
        {
            if (Actor != null) throw new InvalidOperationException("Fixture already owns an actor");
            for (int ring = 2; ring <= 5; ring++)
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI / 4;
                    var spot = player.tr.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * ring;
                    if (!player.gc.tileInfo.PossibleToWalk(player, spot) || !player.gc.tileInfo.SafeToWalk(player, spot) ||
                        !player.movement.HasLOSPosition360(spot) || Crowded(player.gc, spot)) continue;
                    Actor = player.gc.spawnerMain.SpawnAgent(spot, (PlayfieldObject)null, "Courier");
                    if (Actor == null) return false;
                    ownedInstance = Actor.GetInstanceID(); ownedUid = Actor.UID;
                    Actor.SetDefaultGoal("DoNothing");
                    return true;
                }
            return false;
        }

        private static bool Crowded(GameController gc, Vector3 spot)
        {
            foreach (var actor in gc.agentList)
                if (actor != null && WorldNative.Active(actor) && Vector2.Distance(actor.tr.position, spot) < 1.5f) return true;
            return false;
        }

        internal void EmitPressure(Agent player, Action<Noise, bool> observe, int count = 64)
        {
            if (count < 1 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count; i++)
            {
                var noise = player.gc.spawnerMain.SpawnNoise(player.tr.position, 0, player, "Normal", player, true);
                if (noise == null) throw new InvalidOperationException("Native pressure noise not spawned");
                try { observe(noise, true); }
                finally { noise.DestroyMe(); }
                observe(noise, false);
            }
        }

        internal static Action SlowWriter(CaptureWorker writer)
        {
            // Release on the first observed real trigger; the timeout also bounds interrupted tests.
            var release = new System.Threading.ManualResetEventSlim(false);
            if (!writer.Enqueue(() => release.Wait(2000), 64, true))
                throw new InvalidOperationException("Slow-writer fixture not enqueued");
            return () => release.Set();
        }

        internal void Move(Agent player, Vector3 target)
        {
            if (!OwnedActive) throw new InvalidOperationException("Owned movement fixture was retired or reused");
            Actor.agentInteractions.GoHere(Actor, player, target);
        }

        internal static Bullet Projectile(Agent source)
        {
            var bullet = source.gc.spawnerMain.SpawnBullet(source.tr.position, bulletStatus.Normal, source);
            if (bullet == null) throw new InvalidOperationException("Native projectile fixture not spawned");
            bullet.DestroyMe();
            return bullet;
        }

        internal static Vector3 ObjectSpot(Agent player, List<PlayfieldObject> existing)
        {
            for (int ring = 2; ring <= 5; ring++)
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI / 4;
                    var spot = player.tr.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * ring;
                    if (!player.gc.tileInfo.PossibleToWalk(player, spot) || !player.gc.tileInfo.SafeToWalk(player, spot) ||
                        !player.movement.HasLOSPosition360(spot) || Crowded(player.gc, spot)) continue;
                    bool nearby = false;
                    foreach (var entity in existing) if (Vector2.Distance(entity.tr.position, spot) < 1.5f) nearby = true;
                    if (!nearby) return spot;
                }
            throw new InvalidOperationException("No safe object fixture spot");
        }

        internal static PlayfieldObject Object(Agent player, string name, Vector3 position) =>
            name == Names.Food ? (PlayfieldObject)player.gc.spawnerMain.SpawnItem(position, Names.Food) :
            player.gc.spawnerMain.spawnObjectReal(position, (PlayfieldObject)null, name);

        internal static void RemoveObject(PlayfieldObject entity, int uid)
        {
            if (entity != null && entity.UID == uid && WorldNative.Active(entity) && !(entity is ObjectReal obj && obj.destroyed))
                entity.DestroyMe();
        }

        internal static void SetDoor(Door door, bool open, bool locked)
        { door.open = open; door.locked = locked; }

        internal static void Reload(GameController gc) => gc.levelTransition.ChangeLevel();
        internal static void AcceptReload(GameController gc)
        {
            var selection = gc?.mainGUI?.characterSelectScript;
            if (gc?.mainGUI?.characterSelect?.activeInHierarchy == true && selection?.selectedInvSlot?[0] != null && !selection.isChoiceAccepted())
                selection.AcceptChoice(0);
        }

        internal void Remove()
        {
            var actor = Actor;
            // Pool reuse must never turn cleanup into removal of an unrelated actor.
            if (actor != null && actor.GetInstanceID() == ownedInstance && actor.UID == ownedUid && WorldNative.Active(actor))
                actor.DestroyMe();
            Actor = null;
        }
    }
}
