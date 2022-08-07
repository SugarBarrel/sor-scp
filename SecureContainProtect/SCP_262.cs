using System;
using System.Collections.Generic;
using RogueLibsCore;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.Defense, RogueCategories.Melee,
                    RogueCategories.MeleeAccessory, RogueCategories.Weird)]
    public class SCP_262 : CustomItem
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_262>()
                     .WithName(new CustomNameInfo
                     {
                         English = "SCP-262",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = "A military coat that can manifest multiple additional arms from the dark inner lining.",
                     })
                     .WithSprite(Properties.Resources.SCP_262, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 15,
                         CharacterCreationCost = 15,
                         LoadoutCost = 15,
                     });

            RoguePatcher patcher = ScpPlugin.GetPatcher<SCP_262>();
            patcher.Postfix(typeof(StatusEffects), nameof(StatusEffects.hasStatusEffect));
            patcher.Postfix(typeof(StatusEffects), nameof(StatusEffects.hasTrait));

            patcher.Prefix(typeof(Melee), nameof(Melee.Attack), "Melee_Attack_Prefix", new Type[1] { typeof(bool) });
            patcher.Postfix(typeof(Melee), nameof(Melee.Attack), "Melee_Attack_Postfix", new Type[1] { typeof(bool) });
            patcher.Prefix(typeof(Melee), nameof(Melee.CheckAttack), "Melee_CheckAttack_Prefix");
            patcher.Postfix(typeof(Melee), nameof(Melee.CheckAttack), "Melee_CheckAttack_Postfix");
            patcher.Postfix(typeof(MeleeHitbox), "Awake");

            patcher.Prefix(typeof(MeleeColliderBox), "OnTriggerEnter2D");
            patcher.Prefix(typeof(MeleeHitbox), "OnTriggerEnter2D");

            patcher.Postfix(typeof(ItemFunctions), nameof(ItemFunctions.EquipArmor));
            patcher.Postfix(typeof(ItemFunctions), nameof(ItemFunctions.UnequipArmor));

            patcher.Prefix(typeof(PlayfieldObject), nameof(PlayfieldObject.FindDamage),
                           new Type[4] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) });
        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Wearable;
            Item.isArmor = true;
            Item.initCount = 1;
            Item.noCountText = true;
            Item.itemValue = 300;
            Item.stackable = false;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;
        }

        public static void StatusEffects_hasStatusEffect(StatusEffects __instance, string statusEffectName, ref bool __result)
        {
            SCP_262_Manager? man = __instance.agent?.GetHook<SCP_262_Manager>();
            if (man is not null && man.UsingExtraHand)
                __result = man.ProcessHandTraits(statusEffectName);
        }
        public static void StatusEffects_hasTrait(StatusEffects __instance, string traitName, ref bool __result)
        {
            SCP_262_Manager? man = __instance.agent?.GetHook<SCP_262_Manager>();
            if (man is not null && man.UsingExtraHand)
                __result = man.ProcessHandTraits(traitName);
        }

        public static bool Melee_Attack_Prefix(Melee __instance)
        {
            SCP_262_Manager? man = __instance.agent.GetHook<SCP_262_Manager>();
            InvItem weapon = __instance.agent.inventory.equippedWeapon ?? __instance.agent.inventory.fist;
            if (man is not null && weapon.invItemName is "Fist")
            {
                if (man.UsingExtraHand && !man.IsExtraHand(__instance))
                {
                    man.CurrentMelee.Attack(false);
                    return false;
                }
            }
            return true;
        }
        public static void Melee_Attack_Postfix(Melee __instance, ref Coroutine? ___disableFailSafeCoroutine)
        {
            SCP_262_Manager? man = __instance.agent.GetHook<SCP_262_Manager>();
            InvItem weapon = __instance.agent.inventory.equippedWeapon ?? __instance.agent.inventory.fist;
            if (man is not null && (weapon.invItemName is "Fist" || man.IsExtraHand(__instance)))
            {
                __instance.meleeContainerAnim.speed = 2.5f;
                __instance.canAttackAgain = true;
                __instance.agent.weaponCooldown = 0f;
                if (___disableFailSafeCoroutine is not null)
                {
                    __instance.StopCoroutine(___disableFailSafeCoroutine);
                    ___disableFailSafeCoroutine = null;
                }
            }
        }
        public static bool Melee_CheckAttack_Prefix(Melee __instance, out bool __state)
        {
            SCP_262_Manager? man = __instance.agent.GetHook<SCP_262_Manager>();
            InvItem weapon = __instance.agent.inventory.equippedWeapon ?? __instance.agent.inventory.fist;
            if (man is not null && __instance.attackAnimPlaying && !man.IsExtraHand(__instance) && weapon.invItemName is "Fist")
            {
                for (int i = 0; i < 2; i++)
                {
                    __instance.canAttackAgain = true;
                    __instance.agent.weaponCooldown = 0f;
                    Melee extraMelee = man.SelectRandomHand();
                    try
                    {
                        extraMelee.attackAnimPlaying = extraMelee.meleeContainerAnim.isActiveAndEnabled;
                        extraMelee.CheckAttack(false);
                    }
                    finally
                    {
                        man.DeselectHand();
                    }
                }
                __state = true;
                return false;
            }
            __state = false;
            return true;
        }
        public static void Melee_CheckAttack_Postfix(Melee __instance, bool __state)
        {
            if (__state) __instance.didAttackAgain = false;
        }
        public static void MeleeHitbox_Awake(MeleeHitbox __instance) => __instance.objectList = new List<GameObject>();

        public static bool MeleeColliderBox_OnTriggerEnter2D(MeleeColliderBox __instance, Collider2D other)
        {
            SCP_262_Manager? man = __instance.meleeHitbox.myMelee.agent.GetHook<SCP_262_Manager>();

            if (man?.IsExtraHand(__instance.meleeHitbox.myMelee) is true)
            {
                using (man.UsingHand(__instance.meleeHitbox.myMelee))
                {
                    GameObject hitObject = other.gameObject;
                    if (other.CompareTag("AgentSprite"))
                        hitObject = other.GetComponent<AgentColliderBox>().objectSprite.go;
                    __instance.meleeHitbox.HitObject(hitObject, false);
                }
                return false;
            }
            return true;
        }
        public static bool MeleeHitbox_OnTriggerEnter2D(MeleeHitbox __instance, Collider2D other)
        {
            SCP_262_Manager? man = __instance.myMelee.agent.GetHook<SCP_262_Manager>();

            if (man?.IsExtraHand(__instance.myMelee) is true)
            {
                using (man.UsingHand(__instance.myMelee))
                {
                    GameObject hitObject = other.gameObject;
                    if (other.CompareTag("AgentSprite"))
                        hitObject = other.GetComponent<AgentColliderBox>().objectSprite.go;
                    __instance.HitObject(hitObject, false);
                }
                return false;
            }
            return true;
        }

        public static void ItemFunctions_EquipArmor(InvItem item, Agent agent)
        {
            if (item.invItemName is nameof(SCP_262) && agent.GetHook<SCP_262_Manager>() is null)
                agent.AddHook<SCP_262_Manager>();
        }
        public static void ItemFunctions_UnequipArmor(InvItem item, Agent agent)
        {
            if (item.invItemName is nameof(SCP_262))
                agent.RemoveHook<SCP_262_Manager>();
        }

        public static bool PlayfieldObject_FindDamage(PlayfieldObject __instance, PlayfieldObject damagerObject, out int __result)
        {
            SCP_262_Manager? man;
            if (damagerObject is not Melee melee || (man = melee.agent.GetHook<SCP_262_Manager>()) is null)
            {
                __result = default;
                return true;
            }

            if (man.UsingExtraHand)
            {
                float damage = 5f * (1 + man.GetHandStrength() / 3f);
                if (__instance is Agent agent)
                {
                    agent.deathMethodItem = nameof(SCP_262);
                    agent.deathMethodObject = nameof(SCP_262);
                    agent.deathMethod = nameof(SCP_262);
                    agent.deathKiller = melee.agent.agentName;
                }
                __result = Mathf.CeilToInt(damage);
                return false;
            }
            __result = default;
            return true;
        }


    }
    public class SCP_262_Manager : HookBase<PlayfieldObject>, IDisposable
    {
        public Agent Owner => (Agent)Instance;
        private readonly Random rnd = new Random();
        private readonly Melee?[] meleeList = new Melee?[100];
        public int GetActiveMeleeCount()
        {
            int count = 0;
            for (int i = 0, length = meleeList.Length; i < length; i++)
                if (meleeList[i]?.attackAnimPlaying is true)
                    count++;
            return count;
        }

        public bool IsExtraHand(Melee melee) => Array.IndexOf(meleeList, melee) is not -1;
        public Melee CurrentMelee => meleeList[ExtraHandIndex]!;

        public HandSelector UsingHand(int index) => new HandSelector(this, index);
        public HandSelector UsingHand(Melee melee) => new HandSelector(this, IndexOf(melee));
        public Melee SelectRandomHand()
        {
            int index;
            int count = 0;
            do
            {
                index = rnd.Next(10); // TODO: replace with 100
                count++;
            }
            while (meleeList[index]?.attackAnimPlaying is true && count < 100);

            return SelectHand(index);
        }
        public Melee SelectHand(int index)
        {
            if (index is -1)
            {
                ExtraHandIndex = -1;
                return null!;
            }
            if (meleeList[index] is null) CreateHand(index);
            ExtraHandIndex = index;
            return meleeList[index]!;
        }
        public void SelectHand(Melee melee) => SelectHand(IndexOf(melee));
        public void DeselectHand() => ExtraHandIndex = -1;
        private void CreateHand(int index)
        {
            GameObject newMeleeContainer;
            try
            {
                Debug.unityLogger.logEnabled = false;
                newMeleeContainer = Object.Instantiate(Owner.melee.meleeContainer.gameObject);
            }
            finally
            {
                Debug.unityLogger.logEnabled =  true;
            }
            newMeleeContainer.transform.SetParent(Owner.melee.meleeContainer.gameObject.transform.parent, false);
            newMeleeContainer.name = $"ExtraHand_{index}";
            newMeleeContainer.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            newMeleeContainer.transform.Translate(UnityEngine.Random.insideUnitCircle.normalized * 0.32f);

            Melee newMelee = newMeleeContainer.GetComponent<MeleeContainer>().melee;
            meleeList[index] = newMelee;

            newMelee.enabled = true;
            newMelee.RevertAllVars();
            newMelee.attackAnimPlaying = newMelee.meleeContainerAnim.isActiveAndEnabled;
            newMelee.HideMelee();
            tk2dSprite realArm1HSprite = newMelee.realArm1.transform.Find("Arm1H").GetComponent<tk2dSprite>();
            realArm1HSprite.enabled = false;
            newMelee.realArm2HSprite.enabled = false;
        }
        public int IndexOf(Melee melee) => Array.IndexOf(meleeList, melee);

        public void ShowHand(int index)
        {
            Melee melee = meleeList[index]!;
            melee.ShowMelee(false);
        }
        public void HideHand(int index)
        {
            Melee melee = meleeList[index]!;
            melee.HideMelee();
        }

        public int ExtraHandIndex { get; private set; } = -1;
        public bool UsingExtraHand => ExtraHandIndex is not -1;

        // |-----------------------------------|----------------------------------------------------------------------------------------------------|
        // |                                   |     Hand Index                                                                                     |
        // | Trait Name                        |0         1         2         3         4         5         6         7         8         9         |
        // |                                   |0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789|
        // |-----------------------------------|----------------------------------------------------------------------------------------------------|
        // | Strength stat                     |12        6         8         6         10        6         8         6         10        6         |
        // | default: hand % 4 + 1             | 234123412 412341234 234123412 412341234 234123412 412341234 234123412 412341234 234123412 412341234|
        // |-----------------------------------|----------------------------------------------------------------------------------------------------|
        // | CauseBiggerKnockback              |x     x     x     x     x     x     x     x     x     x     x     x     x     x     x     x     x   | % 6
        // | HitObjectsNoNoise                 |x      x      x      x      x      x      x      x      x      x      x      x      x      x      x | % 7
        // | MeleeDestroysWalls (1|2)          |2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   1   2   | % 4
        // | AttacksDamageAttacker (1|2)       |2     1     2     1     2     1     2     1     2     1     2     1     2     1     2     1     2   | % 6
        // | BlocksSometimesHit (1|2)          |2      1      2      1      2      1      2      1      2      1      2      1      2      1      2 | % 7
        // | FleshFeast (1|2)                  |2        1        2        1        2        1        2        1        2        1        2        1| % 9
        // | ChanceToKnockWeapons|KnockWeapons |2         1         2         1         2         1         2         1         2         1         | % 10
        // |                                   |                                                                                                    |
        // |-----------------------------------|----------------------------------------------------------------------------------------------------|

        public int GetHandStrength()
        {
            int hand = ExtraHandIndex;
            if (hand is 0) return 12;
            if (hand % 40 is 0) return 10;
            if (hand % 20 is 0) return 8;
            if (hand % 10 is 0) return 6;
            return hand % 4 + 1;
        }
        public bool ProcessHandTraits(string traitOrEffectName)
        {
            int active = GetActiveMeleeCount();
            int hand = ExtraHandIndex;
            switch (traitOrEffectName)
            {
                case "CauseBiggerKnockback": return hand % 6 is 0;
                case "HitObjectsNoNoise": return hand % 7 is 0;
                case "MeleeDestroysWalls": return active <= 7 && hand % 4 is 0 && hand % 8 is not 0;
                case "MeleeDestroysWalls2": return active > 7 || hand % 8 is 0;
                case "AttacksDamageAttacker": return hand % 6 is 0 && hand % 12 is not 0;
                case "AttacksDamageAttacker2": return hand % 12 is 0;
                case "BlocksSometimesHit": return hand % 7 is 0 && hand % 14 is not 0;
                case "BlocksSometimesHit2": return hand % 14 is 0;
                case "FleshFeast": return hand % 9 is 0 && hand % 18 is not 0;
                case "FleshFeast2": return hand % 18 is 0;
                case "ChanceToKnockWeapons": return hand % 10 is 0 && hand % 20 is not 0;
                case "KnockWeapons": return hand % 20 is 0;
                default: return false;
            }
        }

        protected override void Initialize()
        {

        }
        public void Dispose()
        {

        }
    }
    public class HandSelector : IDisposable
    {
        public HandSelector(SCP_262_Manager man, int index)
        {
            Manager = man;
            man.SelectHand(index);
        }
        private readonly SCP_262_Manager Manager;
        public void Dispose() => Manager.SelectHand(-1);
    }
}
