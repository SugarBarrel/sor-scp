using System;
using System.Collections;
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
            Item.healthChange = 5;
            Item.initCount = 6;
            Item.rewardCount = 12;
            Item.itemValue = 20;
            Item.stackable = true;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;
            Item.goesInToolbar = true;
            Item.throwDamage = 5;
        }

        public bool UseItem()
        {
            int heal = new ItemFunctions().DetermineHealthChange(Item, Owner);
            Owner!.statusEffects.ChangeHealth(heal);

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
                Vector2 target = __instance.tr.position;

                bool ignoreSelf = false;
                SCP_504_ConsumedTomato? consumedTomato = __instance.GetHook<SCP_504_ConsumedTomato>();
                if (consumedTomato is not null)
                { // explode the consumed tomato
                    Power power = DeterminePower();
                    if (power > Power.Twitch)
                    {
                        __instance.RemoveHook(consumedTomato);
                        __instance.deathKiller =  __instance.deathMethod = __instance.deathMethodItem = nameof(SCP_504);
                        __instance.ChangeHealth(-200f);
                        HitTomato(target, power, __instance);
                        ignoreSelf = true;
                    }
                }

                const float distanceThreshold = 16 * 0.64f;
                const float thresholdSqr = distanceThreshold * distanceThreshold;

                foreach (Item item in gc.itemList.FindAll(static i => i.invItem?.invItemName is nameof(SCP_504)))
                {
                    if (((Vector2)item.tr.position - target).sqrMagnitude > thresholdSqr) continue;

                    Power power = DeterminePower();
                    if (power > Power.None)
                        TriggerTomatoOnGround(item, target, power);
                }

                foreach (Agent agent in gc.agentList.ToArray())
                {
                    if (ignoreSelf && agent == __instance) continue;
                    if (((Vector2)agent.tr.position - target).sqrMagnitude > thresholdSqr) continue;

                    if (agent.isPlayer is 0)
                    { // just check their inventory, since they don't have many items
                        InvItem? tomato = agent.inventory?.FindItem(nameof(SCP_504));
                        Power power = DeterminePower();
                        if (tomato is not null && power > Power.None)
                            TriggerTomatoFromInventory(tomato, target, power, __instance);
                    }
                    else
                    { // check just the toolbar, to allow the player to somehow keep their tomatoes under control
                        InvSlot? slot = agent.mainGUI?.AllSlots.Find(
                            static s => s.slotType is "Toolbar" && s.item?.invItemName is nameof(SCP_504));
                        Power power = DeterminePower();
                        if (slot is not null && power > Power.None)
                            TriggerTomatoFromInventory(slot.item, target, DeterminePower(), __instance);
                    }
                }
            }
        }

        public static void Item_OnCollisionEnter2D(Item __instance) => HitTomato(__instance);

        public enum Power { None, Twitch, Hurl, Launch, Blast, Triggered }

        private static readonly Random rnd = new Random();

        public static Power DeterminePower() => rnd.Next(1000) switch
        {
            < 1 => Power.Triggered, //  0.1% - Huge explosion
            < 25 => Power.Blast,    //  2.4% - Big explosion
            < 75 => Power.Twitch,   //  5.0% - small movement
            < 200 => Power.Launch,  // 12.5% - Normal explosion
            < 500 => Power.None,    // 30.0% - nothing
            _ => Power.Hurl,        // 50.0% - powerful throw
        };
        private static int GetDamage(Power power) => power switch
        {
            Power.None => 0,
            Power.Twitch => 1,
            Power.Hurl => 20,
            Power.Launch => 20,
            Power.Blast => 50,
            Power.Triggered => 200,
            _ => throw new ArgumentException(nameof(power)),
        };

        public static void ThrowTomato(Item item, Vector2 target, Power power, bool canHitThrower)
        {
            item.thrower = item.owner;
            item.beingThrown = true;
            item.throwerReal = item.owner;
            item.canHitOwner = true;
            item.go.layer = 19;
            item.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            SCP_504_Tomato tomato = item.AddHook<SCP_504_Tomato>();
            tomato.Power = power;

            int otherDamage = GetDamage(power);
            Movement movement = item.GetComponent<Movement>();
            movement.RotateToPositionOffset(target);
            movement.RotateToPositionOffsetTr(target);

            switch (power)
            {
                // TODO: play different sound clips
                case Power.Twitch:
                    movement.Throw(1f);
                    break;
                case Power.Hurl:
                    movement.Throw(16f);
                    break;
                case Power.Launch:
                    movement.Throw(24f);
                    break;
                case Power.Blast:
                    movement.Throw(32f);
                    break;
                case Power.Triggered:
                    movement.Throw(32f);
                    break;
            }
            item.otherDamageMode = true;
            item.invItem.otherDamage = otherDamage;

            if (power > Power.Twitch)
            {
                Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
                float prevDrag = rb.drag;
                rb.drag = 0.1f;
                IEnumerator Enumerator()
                {
                    yield return new WaitForSeconds(0.5f);
                    rb.drag = prevDrag;
                }
                movement.StartCoroutine(Enumerator());
            }

            if (!canHitThrower)
            {
                Physics2D.IgnoreCollision(item.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(),
                                          item.thrower.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(), true);
                Physics2D.IgnoreCollision(item.GetComponent<CircleCollider2D>().GetComponent<Collider2D>(),
                                          item.thrower.tr.Find("AgentItemCollider").GetComponent<BoxCollider2D>().GetComponent<Collider2D>(), true);
            }
        }
        public static void HitTomato(Item __instance)
        {
            SCP_504_Tomato? tomato = __instance.GetHook<SCP_504_Tomato>();
            if (tomato is null) return;
            if (__instance.rb.velocity.magnitude < 4f)
            {
                ScpPlugin.Logger.LogWarning($"The tomato's too slow! {__instance.rb.velocity.magnitude:F3} u/s");
                return;
            }
            HitTomato(__instance.tr.position, tomato.Power, __instance.thrower);

            if (tomato.Power > Power.Twitch)
                __instance.DestroyMeFromClient();
        }
        public static void HitTomato(Vector2 hit, Power power, Agent thrower)
        {
            gc.spawnerMain.SpawnNoise(hit, 1f, null, null, thrower);
            gc.audioHandler.Play(thrower, "ItemHitItem");

            if (power >= Power.Launch)
            {
                gc.spawnerMain.SpawnExplosion(thrower, hit, power switch
                {
                    Power.Launch => "Normal",
                    Power.Blast => "Big",
                    Power.Triggered => "Huge",
                    _ => null,
                });
                ParticleSystem ps = gc.particleEffectsList[gc.particleEffectsList.Count - 1].ps;
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color32(221, 48, 17, 255));
            }
        }

        public static void TriggerTomatoFromInventory(InvItem item, Vector2 target, Power power, Agent targetAgent)
        {
            if (item.database is null || item.agent is null) return;

            if (item.agent == targetAgent)
            {
                targetAgent.deathKiller =  targetAgent.deathMethod = targetAgent.deathMethodItem = nameof(SCP_504);
                item.database.SubtractFromItemCount(item, 1);
                targetAgent.statusEffects.ChangeHealth(-GetDamage(power));
                HitTomato(target, power, targetAgent);
                return;
            }

            InvItem itemPart = new InvItem
            {
                invItemName = item.invItemName,
                invItemCount = 1,
            };
            itemPart.SetupDetails(true);

            Item tomato = gc.spawnerMain.SpawnItemWeapon(item.agent.curPosition, itemPart, null, item.agent);
            item.database.SubtractFromItemCount(item, 1);
            ThrowTomato(tomato, target, power, false);
        }
        public static void TriggerTomatoOnGround(Item item, Vector2 target, Power power)
        {
            InvItem itemPart = new InvItem
            {
                invItemName = item.invItem.invItemName,
                invItemCount = 1,
            };
            itemPart.SetupDetails(true);

            Item tomato = gc.spawnerMain.SpawnItemWeapon(item.curPosition, itemPart, null, item.owner);
            if (--item.invItem.invItemCount is 0) item.DestroyMe();
            ThrowTomato(tomato, target, power, true);
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
            timeUntilDigestion = 60f;
            // TODO: traits affecting digestion
        }

        public void Update()
        {
            timeUntilDigestion -= Time.deltaTime;
            if (timeUntilDigestion <= 0f) Instance.RemoveHook(this);
        }

    }
}
