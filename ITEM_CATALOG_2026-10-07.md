# Streets of Rogue — installed item and carrying catalog

Inspected October 7, 2026. Read-only inspection of the installed game; no game launch, inventory changes, rebuild or installation of the mod.

The installed `InvItem.SetupDetails` switch has **236 named definition cases**. This is not 236 ordinary carryable objects: the game also represents abilities, cosmetic equipment, debris, special effects and unused content through this class. `Unlocks.LoadInitialUnlocks` registers **116 Item entries** and **20 Ability entries**. Quest, endgame and special inventory items exist outside the Item unlock list.

English names were read from the installed `level0` localization data, matching all 235 ItemNameDB row IDs from the assembly and validating the serialized row count. Untranslated entries retain their native IDs. Names and numeric metadata are independently extracted from the installed game. No descriptions or translated dialogue are reproduced.

Values below are literal `InvItem.itemValue` assignments in each definition block, not guaranteed sale prices. They do not evaluate ammo, durability, quantity, modifiers, character perks or mutators. Multiple assignments are shown rather than silently picking one; unassigned does not mean worthless. The ordinary Rocket Launcher assigns 140, or 20 under the RocketLaunchers mutator. A common post-definition path can halve projectile-weapon value for applicable infinite-ammo mutators.

Registration proves a defined inventory entry, not that it spawns in every run or is currently unlocked. "Reported unused" uses the community wiki as a secondary availability source; this audit did not prove a spawn path for those entries. Ability-linked tools can have real item-like behavior but are not generally ordinary tradeable loot.

## Especially valuable entries

| Item | Literal base value | Availability / significance |
|---|---:|---|
| Mayor's Hat | 1,200 | Quest/endgame item; not ordinary recurring treasure |
| Resurrection Shampoo | 200 | Registered inventory consumable; native Resurrection status effect |
| Safe-Cracking Tool | 200 | Registered inventory tool; reusable safe access |
| Skeleton Key | 150 | Registered inventory tool; reusable door access |
| Rocket Launcher | 140 / 20 | Registered weapon; mutator-dependent definition |
| Flamethrower, Ghost Gibber, Tranquilizer Gun | 120 each | Registered weapons; condition affects practical valuation |
| Machinegun | 110 | Registered weapon |
| Laptop | 250 | Registered as an Ability, not an ordinary treasure item |
| BFG, Six-Leaf Clover, Warp Zoner | 200 each | Unused definitions; do not count as normal loot |

No named gold-bar, diamond or jewel inventory case appears in this installed definition switch. Valuable tools, weapons, money, mission objects and the endgame hat are the better starting points for a theft or negotiation encounter.

## All 116 registered inventory entries

**Combine (13):** Accuracy Mod, Ammo Processor, Ammo Stock, Armor Durability Spray, Bomb Processor, Drink Mixer, Fud Processor, Identify Wand, Melee Durability Spray, Portable Sell-O-Matic, Rate of Fire Mod, Silencer, Water Pistol.

**Consumable (16):** Antidote, Blood Bag, Cigarettes, Cologne, Critter Upper, Cyanide Pill, Electro Pill, First Aid Kit, Giantizer, Killer Thrower, Muscly Pill, Rage Poison, Resurrection Shampoo, Shrinker, Sugar, Syringe.

**Food (6):** Bacon Cheeseburger, Banana, Beer, Fud, Ham Sandwich, Whiskey.

**Money (1):** Money.

**Tool (41):** Ammo Stealer, Blindenizer, Body Swapper, Boo-Urn, Boombox, Cardboard Box, Cigarette Lighter, Cube of Lampey, Door Detonator, EarWarp Whistle, Explodevice, Fireworks, Four-Leaf Clover, Free Item Voucher, Friend Phone, Hacking Tool, Haterator, Hiring Voucher, Hologram Bigfoot, Hypnotizer Mark I, Hypnotizer Mark II, Kill Ammunizer, Kill Healthenizer, Kill Profiter, Lockpick, Memory Mutilator, Mini Fridge, Monkey Barrel, Necronomicon, Quick Escape Teleporter, Remote Bomb, Safe Buster, Safe-Cracking Tool, Skeleton Key, Slave Helmet Remote, Slave Helmet Remover, Time Bomb, Translator, Walkie Talkie, Wall Bypasser, Window Cutter.

**WeaponMelee (8):** Axe, Baseball Bat, Crowbar, Knife, Police Baton, Sledgehammer, Sword, Wrench.

**WeaponProjectile (14):** Fire Extinguisher, Flamethrower, Freeze Ray, Ghost Gibber, Leafblower, Machinegun, Oil Container, Pistol, Revolver, Rocket Launcher, Shotgun, Shrink Ray, Taser, Tranquilizer Gun.

**WeaponThrown (10):** Bear Trap, Dizzy Grenade, EMP Grenade, Grenade, Land Mine, Molotov Cocktail, Paralyzer Trap, Rock, Shuriken, Warp Grenade.

**Wearable (7):** Bracelet of Strength, Bulletproof Vest, Codpiece, Combat Helmet, Fireproof Suit, Gas Mask, Hard Hat.

## Quest, mission and endgame entries

Blueprints, Briefcase, Circuit Board, Courier Package, Courier Package (Shook Up), Evidence, Giant Bomb, Hard Drive, Incriminating Photo, Macguffin Muffin, Mayor's Hat, Record of Evidence, Signed Baseball, Tooth, Will.

## Physical props that can be lifted with Toss

These **42 object classes** explicitly assign `ObjectReal.pickUppable = true` in `SetVars`. `StatusEffects.FindSpecialAbilityObject` uses this flag for Toss and also checks current state, including fire, destruction, temporary interaction suppression and other targeting conditions. This is inspected eligibility code, not a runtime pass for every prop. These objects are held for throwing; they are not ordinary backpack items.

AirConditioner, Altar, AmmoDispenser, ArcadeGame, ATMMachine, AugmentationBooth, Barbecue, BarStool, Bathtub, Bed, Bench, BoulderSmall, CapsuleMachine, Chair, CloneMachine, Computer, CornStalk, Desk, ExplodingBarrel, FlamingBarrel, Gravestone, Jukebox, Lamp, LoadoutMachine, PawnShopMachine, Plant, PoolTable, Refrigerator, SecurityBot, Shelf, Sign, SlimeBarrel, SlotMachine, Speaker, Stove, Table, Television, Toilet, TrashCan, Turntables, WasteBasket, WaterPump.

Explicit false assignments in the inspected SetVars methods: AlarmButton, Billboard, Boulder, Bush, Crate, FireHydrant, Fireplace, Fountain, KillerPlant, PoliceBox, PowerBox, Statue, SwitchBasic, Tree, VendorCart, Well.

## Complete installed definition catalog

This table includes every named definition case, with ordinary inventory entries distinguished from abilities and unavailable or uncertain content. It includes game definitions only. Our mod separately adds Delivery Burrito and Services Phone.

| English name / native fallback | Native ID | Type | Literal base value(s) | Classification |
|---|---|---|---:|---|
| Accuracy Mod | `AccuracyMod` | Combine | 50 | Registered inventory item |
| Ammo Processor | `AmmoProcessor` | Combine | 4 | Registered inventory item |
| Ammo Stealer | `AmmoStealer` | Tool | 35 | Registered inventory item |
| Ammo Stock | `AmmoCapacityMod` | Combine | 50 | Registered inventory item |
| Antidote | `Antidote` | Consumable | 50 | Registered inventory item |
| Armor Durability Spray | `ArmorDurabilityDoubler` | Combine | 100 | Registered inventory item |
| Augmentation Canister | `AugmentationCanister` | Tool | 150 | Reported unused; definition exists |
| Axe | `Axe` | WeaponMelee | 70 | Registered inventory item |
| Bacon Cheeseburger | `BaconCheeseburger` | Food | 25 | Registered inventory item |
| Ballet Shoes | `BalletShoes` | Wearable | 5 | Reported unused; definition exists |
| Banana | `Banana` | Food | 15 | Registered inventory item |
| Banana Peel | `BananaPeel` | WeaponThrown | 5 | Special / availability not established by this audit |
| Baseball Bat | `BaseballBat` | WeaponMelee | 50 | Registered inventory item |
| Bear Trap | `BearTrap` | WeaponThrown | 20 | Registered inventory item |
| Bear Trap | `BearTrapPark` | WeaponThrown | 30 | Internal / variant / effect; not ordinary loot |
| Beer | `Beer` | Food | 10 | Registered inventory item |
| BFG | `BFG` | WeaponProjectile | 200 | Reported unused; definition exists |
| Bite | `Bite` | unassigned | unassigned | Ability / ability-linked tool |
| Blindenizer | `Blindenizer` | Tool | 40 | Registered inventory item |
| Blood Bag | `BloodBag` | Consumable | 20 | Registered inventory item |
| Blood Transfusion Kit | `BloodTransfusionKit` | Tool | 0 | Special / availability not established by this audit |
| Blowtorch | `Blowtorch` | WeaponMelee | 5 | Special / availability not established by this audit |
| Blueprints | `Blueprints` | unassigned | unassigned | Quest / mission / endgame item |
| Body Swapper | `BodySwapper` | Tool | 40 | Registered inventory item |
| Body Vanishing Salt | `BodyVanisher` | Tool | 20 | Reported unused; definition exists |
| Bomb Processor | `BombMaker` | Combine | 4 | Registered inventory item |
| Boo-Urn | `BooUrn` | Tool | 40 | Registered inventory item |
| Boombox | `Boombox` | Tool | 30 | Registered inventory item |
| Bracelet of Strength | `BraceletStrength` | Wearable | 70 | Registered inventory item |
| Briefcase | `Briefcase` | unassigned | unassigned | Quest / mission / endgame item |
| Bulletproof Vest | `BulletproofVest` | Wearable | 100 | Registered inventory item |
| Camouflage | `InvisibleLimitedItem` | unassigned | unassigned | Ability / ability-linked tool |
| Cannibalize | `Cannibalize` | unassigned | unassigned | Ability / ability-linked tool |
| Cardboard Box | `CardboardBox` | Tool | 50 | Registered inventory item |
| Chaaarge | `Charge` | unassigned | unassigned | Ability / ability-linked tool |
| Chainsaw | `Chainsaw` | WeaponMelee | 100 | Reported unused; definition exists |
| Chicken Nugget | `Nugget` | Nugget | 1 | Special / availability not established by this audit |
| Chloroform Hankie | `ChloroformHankie` | WeaponMelee | 100 | Ability / ability-linked tool |
| Cigarette Lighter | `CigaretteLighter` | Tool | 10 | Registered inventory item |
| Cigarettes | `Cigarettes` | Consumable | 15 | Registered inventory item |
| Circuit Board | `CircuitBoard` | unassigned | unassigned | Quest / mission / endgame item |
| Cocktail | `Cocktail` | Consumable | 40 | Special / availability not established by this audit |
| Codpiece | `CodPiece` | Wearable | 40 | Registered inventory item |
| Cologne | `Cologne` | Consumable | 100 | Registered inventory item |
| Combat Helmet | `SoldierHelmet` | Wearable | 80 | Registered inventory item |
| Condom | `Condom` | Wearable | 5 | Special / availability not established by this audit |
| Cop2Hat | `Cop2Hat` | Wearable | unassigned | Special / availability not established by this audit |
| CopHat | `CopHat` | Wearable | unassigned | Special / availability not established by this audit |
| Courier Package | `CourierPackage` | unassigned | unassigned | Quest / mission / endgame item |
| Courier Package (Shook Up) | `CourierPackageBroken` | unassigned | unassigned | Quest / mission / endgame item |
| Critter Upper | `CritterUpper` | Consumable | 50 | Registered inventory item |
| Crowbar | `Crowbar` | WeaponMelee | 40 | Registered inventory item |
| Cry Profusely | `TutorialAbility` | unassigned | unassigned | Internal / variant / effect; not ordinary loot |
| Cube of Lampey | `CubeOfLampey` | Tool | 4 | Registered inventory item |
| Cyanide Pill | `CyanidePill` | Consumable | 150 | Registered inventory item |
| Dart (Poison) | `DartPoison` | WeaponThrown | 20 | Internal / variant / effect; not ordinary loot |
| Delivery App | `DeliveryApp` | Tool | 0 | Special / availability not established by this audit |
| Dizzy Grenade | `GrenadeDizzy` | WeaponThrown | 15 | Registered inventory item |
| DoctorHeadLamp | `DoctorHeadLamp` | Wearable | unassigned | Special / availability not established by this audit |
| Door Detonator | `DoorDetonator` | Tool | 50 | Registered inventory item |
| Drink Mixer | `DrinkMixer` | Combine | 100 | Registered inventory item |
| Duster | `Duster` | WeaponMelee | 80 | Special / availability not established by this audit |
| EarWarp Whistle | `HearingBlocker` | Tool | 40 | Registered inventory item |
| Electro Pill | `ElectroPill` | Consumable | 60 | Registered inventory item |
| Electro Tether Vest | `BodyguardTether` | Tool | 0 | Special / availability not established by this audit |
| EMP Grenade | `GrenadeEMP` | WeaponThrown | 15 | Registered inventory item |
| Enslave | `Enslave` | unassigned | unassigned | Ability / ability-linked tool |
| Evidence | `Evidence` | unassigned | unassigned | Quest / mission / endgame item |
| Explodevice | `ExplosiveStimulator` | Tool | 50 | Registered inventory item |
| FactoryObject | `FactoryObject` | NonItem | unassigned | Internal / variant / effect; not ordinary loot |
| Fedora | `Fedora` | Wearable | unassigned | Special / availability not established by this audit |
| Fire Extinguisher | `FireExtinguisher` | WeaponProjectile | 50 | Registered inventory item |
| FireHelmet | `FireHelmet` | Wearable | unassigned | Special / availability not established by this audit |
| Fireproof Suit | `FireproofSuit` | Wearable | 70 | Registered inventory item |
| Fireworks | `Fireworks` | Tool | 70 | Registered inventory item |
| First Aid Kit | `FirstAidKit` | Consumable | 50 | Registered inventory item |
| Fist | `Fist` | WeaponMelee | 0 | Internal / variant / effect; not ordinary loot |
| Five-Leaf Clover | `FiveLeafClover` | Tool | 140 | Reported unused; definition exists |
| Flamethrower | `Flamethrower` | WeaponProjectile | 120 | Registered inventory item |
| Flask | `Flask` | Consumable | 5 | Special / availability not established by this audit |
| Force Field | `ForceField` | Tool | 50 | Special / availability not established by this audit |
| Four-Leaf Clover | `FourLeafClover` | Tool | 80 | Registered inventory item |
| Free Item Voucher | `FreeItemVoucher` | Tool | 50 | Registered inventory item |
| Freeze Ray | `FreezeRay` | WeaponProjectile | 100 | Registered inventory item |
| Friend Phone | `FriendPhone` | Tool | 50 | Registered inventory item |
| Fud | `Fud` | Food | 7 | Registered inventory item |
| Fud Processor | `FoodProcessor` | Combine | 4 | Registered inventory item |
| Gas Mask | `GasMask` | Wearable | 70 | Registered inventory item |
| Ghost Gibber | `GhostBlaster` | WeaponProjectile | 120 | Registered inventory item |
| Giant Bomb | `BigBomb` | unassigned | unassigned | Quest / mission / endgame item |
| Giantizer | `Giantizer` | Consumable | 100 | Registered inventory item |
| Giblet | `Giblet` | NonItem | unassigned | Internal / variant / effect; not ordinary loot |
| Grappling Hook | `GrapplingHook` | WeaponProjectile | 5 | Reported unused; definition exists |
| Grenade | `Grenade` | WeaponThrown | 20 | Registered inventory item |
| Guided Missile Launcher | `GuidedMissileLauncher` | WeaponProjectile | 180 | Reported unused; definition exists |
| HackerGlasses | `HackerGlasses` | Wearable | unassigned | Special / availability not established by this audit |
| Hacking Tool | `HackingTool` | Tool | 30 | Registered inventory item |
| Ham Sandwich | `HamSandwich` | Food | 15 | Registered inventory item |
| Handcuffs | `Handcuffs` | unassigned | unassigned | Ability / ability-linked tool |
| Hard Drive | `HardDrive` | unassigned | unassigned | Quest / mission / endgame item |
| Hard Hat | `HardHat` | Wearable | 60 | Registered inventory item |
| HatBlue | `HatBlue` | Wearable | unassigned | Special / availability not established by this audit |
| Haterator | `Haterator` | Tool | 40 | Registered inventory item |
| HatRed | `HatRed` | Wearable | unassigned | Special / availability not established by this audit |
| Headphones | `Headphones` | Wearable | unassigned | Special / availability not established by this audit |
| Hiring Voucher | `HiringVoucher` | Tool | 30 | Registered inventory item |
| Hologram Bigfoot | `HologramItem` | Tool | 30 | Registered inventory item |
| Hot Fud | `HotFud` | Food | 15 | Special / availability not established by this audit |
| Hypnotizer Mark I | `Hypnotizer` | Tool | 30 | Registered inventory item |
| Hypnotizer Mark II | `Hypnotizer2` | Tool | 50 | Registered inventory item |
| Identify Wand | `IdentifyWand` | Combine | 20 | Registered inventory item |
| Incriminating Photo | `IncriminatingPhoto` | unassigned | unassigned | Quest / mission / endgame item |
| Item Teleporter | `ItemTeleporter` | Tool | 0 | Special / availability not established by this audit |
| Jackhammer | `Jackhammer` | WeaponMelee | 5 | Reported unused; definition exists |
| Joke | `Joke` | unassigned | unassigned | Ability / ability-linked tool |
| Key | `Key` | Tool | 0 | Special / availability not established by this audit |
| Keycard | `KeyCard` | Tool | 0 | Reported unused; definition exists |
| Kill Ammunizer | `KillProfiterAmmo` | Tool | 100 | Registered inventory item |
| Kill Healthenizer | `KillProfiterHealth` | Tool | 70 | Registered inventory item |
| Kill Profiter | `KillProfiter` | Tool | 100 | Registered inventory item |
| Killer Thrower | `KillerThrower` | Consumable | 50 | Registered inventory item |
| Knife | `Knife` | WeaponMelee | 30 | Registered inventory item |
| Knocker Grenade | `GrenadeKnocker` | WeaponThrown | 20 | Internal / variant / effect; not ordinary loot |
| Knocker Melee | `KnockerMelee` | WeaponMelee | 5 | Internal / variant / effect; not ordinary loot |
| Land Mine | `LandMine` | WeaponThrown | 30 | Registered inventory item |
| Laptop | `Laptop` | Tool | 250 | Ability / ability-linked tool |
| Laser Blazer | `LaserBlazer` | Wearable | 70 | Special / availability not established by this audit |
| Laser Cannon | `LaserGun` | WeaponProjectile | 110 | Special / availability not established by this audit |
| Leafblower | `LeafBlower` | WeaponProjectile | 70 | Registered inventory item |
| Lockpick | `Lockpick` | Tool | 20 | Registered inventory item |
| Macguffin Muffin | `MacguffinMuffin` | unassigned | unassigned | Quest / mission / endgame item |
| Machinegun | `MachineGun` | WeaponProjectile | 110 | Registered inventory item |
| Magic Lamp | `MagicLamp` | Tool | 100 | Reported unused; definition exists |
| Map | `Map` | Readable | 50 | Reported unused; definition exists |
| Matches | `Matches` | Tool | 10 | Reported unused; definition exists |
| Mayor Visitor's Badge | `MayorBadge` | Wearable | 0 | Special / availability not established by this audit |
| Mayor's Hat | `MayorHat` | Wearable | 1200 | Quest / mission / endgame item |
| Mech Key | `MechTransformItem` | Tool | 0 | Special / availability not established by this audit |
| Melee Durability Spray | `MeleeDurabilityDoubler` | Combine | 50 | Registered inventory item |
| Memory Mutilator | `MemoryEraser` | Tool | 40 | Registered inventory item |
| Mind Control | `MindControl` | unassigned | unassigned | Ability / ability-linked tool |
| Mind Reader Device | `MindReaderDevice` | Tool | 100 | Reported unused; definition exists |
| Mini Fridge | `MiniFridge` | Tool | 50 | Registered inventory item |
| Molotov Cocktail | `MolotovCocktail` | WeaponThrown | 20 | Registered inventory item |
| Money | `Money` | Money | 1 | Registered inventory item |
| Monkey Barrel | `MonkeyBarrel` | Tool | 40 | Registered inventory item |
| Mood Ring | `MoodRing` | Wearable | 10 | Reported unused; definition exists |
| Muscly Pill | `Steroids` | Consumable | 40 | Registered inventory item |
| Mystery | `TwitchMystery` | unassigned | unassigned | Internal / variant / effect; not ordinary loot |
| Necronomicon | `Necronomicon` | Tool | 40 | Registered inventory item |
| Newspaper | `Newspaper` | Readable | 10 | Reported unused; definition exists |
| Note | `Note` | Readable | 5 | Reported unused; definition exists |
| Oil Container | `OilContainer` | WeaponProjectile | 70 | Registered inventory item |
| Paralyzer Trap | `ParalyzerTrap` | WeaponThrown | 20 | Registered inventory item |
| Pistol | `Pistol` | WeaponProjectile | 70 | Registered inventory item |
| Plasma Sword | `PlasmaSword` | WeaponMelee | 60 | Reported unused; definition exists |
| Police Baton | `PoliceBaton` | WeaponMelee | 30 | Registered inventory item |
| Portable Sell-O-Matic | `PortableSellOMatic` | Combine | 100 | Registered inventory item |
| Possess | `Possess` | unassigned | unassigned | Ability / ability-linked tool |
| Possession Stone | `Depossessor` | Tool | 0 | Special / availability not established by this audit |
| Power Drill | `PowerDrill` | WeaponMelee | 100 | Special / availability not established by this audit |
| Power Sap | `PowerSap` | unassigned | unassigned | Ability / ability-linked tool |
| Primal Lunge | `Lunge` | unassigned | unassigned | Ability / ability-linked tool |
| Property Deed | `PropertyDeed` | Tool | 150 | Reported unused; definition exists |
| ProtectiveShell | `ProtectiveShell` | unassigned | unassigned | Internal / variant / effect; not ordinary loot |
| Quick Escape Teleporter | `QuickEscapeTeleporter` | Tool | 30 | Registered inventory item |
| Rag | `Rag` | Junk | 5 | Reported unused; definition exists |
| Rage Poison | `RagePoison` | Consumable | 75 | Registered inventory item |
| Rate of Fire Mod | `RateOfFireMod` | Combine | 50 | Registered inventory item |
| Record of Evidence | `MayorEvidence` | unassigned | unassigned | Quest / mission / endgame item |
| Remote Bomb | `RemoteBomb` | Tool | 20 | Registered inventory item |
| Remote Bomb Trigger | `BombTrigger` | Tool | 0 | Special / availability not established by this audit |
| Remote Control | `RemoteControl` | Tool | 150 | Reported unused; definition exists |
| Research Gun | `ResearchGun` | WeaponProjectile | 0 | Special / availability not established by this audit |
| Resurrection Shampoo | `ResurrectionShampoo` | Consumable | 200 | Registered inventory item |
| Revolver | `Revolver` | WeaponProjectile | 90 | Registered inventory item |
| Ride Mech | `MechTransform` | unassigned | unassigned | Ability / ability-linked tool |
| Rock | `Rock` | WeaponThrown | 5 | Registered inventory item |
| Rocket Launcher | `RocketLauncher` | WeaponProjectile | 20, 140 | Registered inventory item |
| RocketLauncherInf | `RocketLauncherInf` | WeaponProjectile | 120 | Internal / variant / effect; not ordinary loot |
| Rope | `Rope` | Tool | 5 | Reported unused; definition exists |
| Rubber Bullets | `RubberBulletsMod` | Combine | 50 | Special / availability not established by this audit |
| Safe Buster | `SafeBuster` | Tool | 30 | Registered inventory item |
| Safe Combination | `SafeCombination` | Tool | 0 | Special / availability not established by this audit |
| Safe-Cracking Tool | `SafeCrackingTool` | Tool | 200 | Registered inventory item |
| Saw | `Saw` | Tool | 40 | Reported unused; definition exists |
| Sharp Lunge | `WerewolfLunge` | unassigned | unassigned | Internal / variant / effect; not ordinary loot |
| Shotgun | `Shotgun` | WeaponProjectile | 70 | Registered inventory item |
| Shovel | `Shovel` | WeaponMelee | 40 | Reported unused; definition exists |
| Shrink Ray | `ShrinkRay` | WeaponProjectile | 100 | Registered inventory item |
| Shrinker | `Shrinker` | Consumable | 50 | Registered inventory item |
| Shuriken | `Shuriken` | WeaponThrown | 5 | Registered inventory item |
| Signed Baseball | `SignedBaseball` | unassigned | unassigned | Quest / mission / endgame item |
| Silencer | `Silencer` | Combine | 50 | Registered inventory item |
| Six-Leaf Clover | `SixLeafClover` | Tool | 200 | Reported unused; definition exists |
| Skeleton Key | `SkeletonKey` | Tool | 150 | Registered inventory item |
| Slave Helmet | `SlaveHelmet` | Wearable | 0 | Special / availability not established by this audit |
| Slave Helmet Remote | `SlaveHelmetRemote` | Tool | 0 | Registered inventory item |
| Slave Helmet Remover | `SlaveHelmetRemover` | Tool | 50 | Registered inventory item |
| Sledgehammer | `Sledgehammer` | WeaponMelee | 80 | Registered inventory item |
| Sniper Rifle | `SniperRifle` | WeaponProjectile | 80 | Reported unused; definition exists |
| Sticky Glove | `StealingGlove` | WeaponMelee | 100 | Ability / ability-linked tool |
| Sticky Mine | `StickyMine` | WeaponThrown | 50 | Special / availability not established by this audit |
| Stomp | `Stomp` | unassigned | unassigned | Ability / ability-linked tool |
| Sugar | `Cocaine` | Consumable | 50 | Registered inventory item |
| Sunglasses | `Sunglasses` | Wearable | unassigned | Special / availability not established by this audit |
| Sword | `Sword` | WeaponMelee | 60 | Registered inventory item |
| Sword (Flaming) | `FlamingSword` | WeaponMelee | 100 | Reported unused; definition exists |
| Syringe | `Syringe` | Consumable | 50 | Registered inventory item |
| Taser | `Taser` | WeaponProjectile | 70 | Registered inventory item |
| Teleporter | `Teleporter` | Tool | 15 | Reported unused; definition exists |
| ThiefHat | `ThiefHat` | Wearable | unassigned | Special / availability not established by this audit |
| Time Bomb | `TimeBomb` | Tool | 100 | Registered inventory item |
| Tooth | `Tooth` | unassigned | unassigned | Quest / mission / endgame item |
| Toss | `Toss` | unassigned | unassigned | Ability / ability-linked tool |
| TossItem | `TossItem` | WeaponThrown | unassigned | Internal / variant / effect; not ordinary loot |
| Tranquilizer Gun | `TranquilizerGun` | WeaponProjectile | 120 | Registered inventory item |
| Translator | `Translator` | Tool | 50 | Registered inventory item |
| Tripmine | `TripMine` | WeaponThrown | 50 | Reported unused; definition exists |
| Vision Detector | `VisionDetector` | Tool | 5 | Special / availability not established by this audit |
| Voodoo Doll | `VoodooDoll` | Tool | 5 | Reported unused; definition exists |
| Walkie Talkie | `WalkieTalkie` | Tool | 40 | Registered inventory item |
| Wall Bypasser | `WallBypasser` | Tool | 40 | Registered inventory item |
| Warp Grenade | `GrenadeWarp` | WeaponThrown | 25 | Registered inventory item |
| Warp Zoner | `WarpZoner` | Tool | 200 | Reported unused; definition exists |
| Water Cannon | `WaterCannon` | WeaponProjectile | 50 | Ability / ability-linked tool |
| Water Pistol | `WaterPistol` | Combine | 100 | Registered inventory item |
| WeaponProjectile | `WeaponProjectile` | unassigned | unassigned | Internal / variant / effect; not ordinary loot |
| Werewolf Transformation | `WerewolfTransform` | unassigned | unassigned | Ability / ability-linked tool |
| Whiskey | `Whiskey` | Food | 20 | Registered inventory item |
| Will | `Will` | unassigned | unassigned | Quest / mission / endgame item |
| Window Cutter | `WindowCutter` | Tool | 30 | Registered inventory item |
| Wreckage | `Wreckage` | NonItem | unassigned | Internal / variant / effect; not ordinary loot |
| Wrench | `Wrench` | WeaponMelee | 40 | Registered inventory item |
| Zombie Phlegm | `ZombieSpit` | unassigned | unassigned | Ability / ability-linked tool |
| Zombie Reanimator | `ZombieReanimator` | Tool | 5 | Special / availability not established by this audit |

## Evidence and limitations

- Source assembly SHA256: `4d82cb026f3830f4771a855efe8b4d8dc1f3b2bd4115c371485687bb43a91025`.
- Localization asset SHA256: `45f81d5d1b7c67dad825e15a7faf67fcbe0f59dc23124f7718862106ab4a0c29`.
- Extraction evidence and the independent read-only C# reader are retained in [.runtime/item-audit-20261007](.runtime/item-audit-20261007/).
- `definitions.json` retains literal fields and category assignments for all 236 cases; `native-methods-with-carrying.json` retains inspected IL, including unlock and Toss checks; `english-names.json` retains native English labels; `carry-props.json` retains liftability classifications; `receipt.json` identifies the source files.
- Community reference for availability: [Items](https://streetsofrogue.fandom.com/wiki/Items), [Unused Content](https://streetsofrogue.fandom.com/wiki/Unused_Content). Physical carrying reference: [Wrestler](https://streetsofrogue.fandom.com/wiki/Wrestler).
- Native base values are established by code inspection. This report does not claim sale payouts, a complete runtime loot-spawn matrix, NPC willingness to trade, or that every character can use every item.
