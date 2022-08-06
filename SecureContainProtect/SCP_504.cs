using System;
using RogueLibsCore;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.Food,
                    RogueCategories.Weapons, RogueCategories.NonStandardWeapons, RogueCategories.NotRealWeapons)]
    public class SCP_504 : CustomItem, IItemUsable
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_504>()
                     .WithName(new CustomNameInfo("SCP-504"))
                     .WithDescription(new CustomNameInfo
                     {
                         English = "Critical tomatoes that violently self-destruct at people making bad jokes.",
                     })
                     .WithSprite(Properties.Resources.SCP_504, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 5,
                         CharacterCreationCost = 5,
                         LoadoutCost = 5,
                     });

        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Food;
            Item.healthChange = 15;
            Item.initCount = 3;
            Item.rewardCount = 6;
            Item.itemValue = 40;
            Item.stackable = true;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;
            Item.goesInToolbar = true;
            Item.throwDamage = 5;
        }

        public bool UseItem()
        {
            int heal = new ItemFunctions().DetermineHealthChange(Item, Owner);
            Owner.statusEffects.ChangeHealth(heal);

            if (Owner.HasTrait(VanillaTraits.ShareTheHealth) || Owner.HasTrait(VanillaTraits.ShareTheHealth2))
                new ItemFunctions().GiveFollowersHealth(Owner, heal);

            gc.audioHandler.Play(Owner, VanillaAudio.UseFood);
            Count--;
            Owner.AddHook<SCP_504_ConsumedTomato>();
            return true;
        }

        [RLSetup]
        public static void SetupPatches()
        {
            RoguePatcher patcher = ScpPlugin.GetPatcher<SCP_504>();

#pragma warning disable CS0618
            patcher.Postfix(typeof(Agent), nameof(Agent.SayDialogue),
                            new Type[4] { typeof(bool), typeof(string), typeof(bool), typeof(NetworkInstanceId) });
#pragma warning restore CS0618

            patcher.Postfix(typeof(Item), "OnCollisionEnter2D", new Type[1] { typeof(Collision2D) });

        }

        public static void Agent_SayDialogue(Agent __instance, string type)
        {
            if (type.StartsWith("Joke_", StringComparison.Ordinal))
            {
                if (__instance.GetHook<SCP_504_ConsumedTomato>() is not null && DeterminePower() > Power.None)
                {
                    __instance.deathKiller =  __instance.deathMethod = __instance.deathMethodItem
                        = gc.nameDB.GetName(nameof(SCP_504), NameTypes.Item);
                    __instance.ChangeHealth(-200f);
                }

                Vector2 target = __instance.curPosition;

                foreach (Item item in gc.itemList.FindAll(static i => i.invItem?.invItemName is nameof(SCP_504)))
                {
                    TriggerTomato(item, target, DeterminePower());
                }

                foreach (Agent agent in gc.agentList)
                {
                    if (Vector2.Distance(agent.tr.position, target) > 16 * 0.64f) return;
                    if (agent.isPlayer is 0)
                    {
                        InvItem? tomato = agent.inventory.FindItem(nameof(SCP_504));
                        if (tomato is not null) TriggerTomato(tomato, target, DeterminePower());
                    }
                    else
                    {
                        InvSlot? slot = agent.mainGUI?.AllSlots.Find(static s => s.slotType is "Toolbar" && s.item?.invItemName is nameof(SCP_504));
                        if (slot is not null) TriggerTomato(slot.item, target, DeterminePower());
                    }
                }

            }
        }

        public enum Power { None, Twitch, Hurl, Launch, Blast }

        private static readonly Random rnd = new Random();
        public static Power DeterminePower() => rnd.Next(1000) switch
        {
            < 5 => Power.Blast,    //  0.5% - Huge explosion
            < 50 => Power.Launch,  //  2.5% - Big explosion
            < 100 => Power.Twitch, //  7.0% - small movement
            < 400 => Power.None,   // 30.0% - nothing
            _ => Power.Hurl,       // 60.0% - Normal explosion
        };

        public static Item? TriggerTomato(Item item, Vector2 target, Power power)
        {
            if (power is Power.None) return null;

            InvItem itemPart = new InvItem
            {
                invItemName = item.invItem.invItemName,
                invItemCount = 1,
            };
            itemPart.SetupDetails(true);

            Item tomato = gc.spawnerMain.SpawnItemWeapon(item.curPosition, itemPart, null, item.owner);
            if (--item.invItem.invItemCount is 0) item.DestroyMe();
            LaunchTomato(tomato, target, power);
            return tomato;
        }
        public static Item? TriggerTomato(InvItem item, Vector2 target, Power power)
        {
            if (power is Power.None || item.database is null || item.agent is null) return null;

            InvItem itemPart = new InvItem
            {
                invItemName = item.invItemName,
                invItemCount = 1,
            };
            itemPart.SetupDetails(true);

            Item tomato = gc.spawnerMain.SpawnItemWeapon(item.agent.curPosition, itemPart, null, item.agent);
            item.database.SubtractFromItemCount(item, 1);
            LaunchTomato(tomato, target, power);
            return tomato;
        }
        private static void LaunchTomato(Item item, Vector2 target, Power power)
        {
            item.thrower = item.owner;
            item.beingThrown = true;
            item.throwerReal = item.owner;
            item.canHitOwner = true;
            item.go.layer = 19;
            item.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            SCP_504_Tomato tomato = item.AddHook<SCP_504_Tomato>();
            tomato.Power = power;

            int otherDamage = 0;
            Movement movement = item.GetComponent<Movement>();
            movement.RotateToPositionOffset(target);
            movement.RotateToPositionOffsetTr(target);
            switch (power)
            {
                case Power.Twitch:
                    otherDamage = 1;
                    movement.Throw(2f); // TODO: play different sound clips
                    break;
                case Power.Hurl:
                    otherDamage = 20;
                    movement.Throw(16f);
                    break;
                case Power.Launch:
                    otherDamage = 40;
                    movement.Throw(24f);
                    break;
                case Power.Blast:
                    otherDamage = 100;
                    movement.Throw(32f);
                    break;
            }
            item.otherDamageMode = true;
            item.invItem.otherDamage = otherDamage;

            Physics2D.IgnoreCollision(item.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(),
                                      item.thrower.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(), true);
            Physics2D.IgnoreCollision(item.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(),
                                      item.thrower.tr.Find("AgentItemCollider").GetComponent<BoxCollider2D>().GetComponent<Collider2D>(), true);

        }

        public static void Item_OnCollisionEnter2D(Item __instance)
        {
            SCP_504_Tomato? tomato = __instance.GetHook<SCP_504_Tomato>();
            if (tomato is not null && tomato.Power >= Power.Hurl)
            {
                if (__instance.rb.velocity.magnitude < 4f)
                {
                    ScpPlugin.Logger.LogWarning($"The tomato's too slow! {__instance.rb.velocity.magnitude:F3} u/s");
                    return;
                }
                gc.spawnerMain.SpawnNoise(__instance.tr.position, 1f, null, null, __instance.thrower);
                gc.audioHandler.Play(__instance, "ItemHitItem");

                gc.spawnerMain.SpawnExplosion(__instance, __instance.tr.position, tomato.Power switch
                {
                    Power.Hurl => "Normal",
                    Power.Launch => "Big",
                    Power.Blast => "Huge",
					_ => throw new InvalidOperationException($"Invalid tomato power: {tomato.Power}"),
                });
                __instance.DestroyMe();
            }
        }

    }
    public class SCP_504_Tomato : HookBase<PlayfieldObject>
    {
        protected override void Initialize() { }
        public SCP_504.Power Power;

    }
    public class SCP_504_ConsumedTomato : HookBase<PlayfieldObject>, IDoUpdate
    {
        private float timeUntilDigestion;

        protected override void Initialize()
        {
            timeUntilDigestion = 120f;
            // TODO: traits affecting digestion
        }

        public void Update()
        {
            timeUntilDigestion -= Time.deltaTime;
            if (timeUntilDigestion <= 0f) Instance.RemoveHook(this);
        }

    }
}
