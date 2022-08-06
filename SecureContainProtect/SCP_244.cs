using RogueLibsCore;
using UnityEngine;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.Usable, RogueCategories.Defense)]
    public class SCP_244 : CustomItem, IItemUsable
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_244>()
                     .WithName(new CustomNameInfo
                     {
                         English = "SCP-244",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = "", // TODO
                     })
                     .WithSprite(Properties.Resources.SCP_244, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 5,
                         CharacterCreationCost = 5,
                         LoadoutCost = 5,
                     });

            RoguePatcher patcher = ScpPlugin.GetPatcher<SCP_244>();
            patcher.Postfix(typeof(Item), nameof(global::Item.ActionsAfterDrop));
        }

        public static void Item_ActionsAfterDrop(Item __instance)
        {
            if (__instance.itemName is nameof(SCP_244))
            {
                __instance.interactable = true;
                __instance.SetCantPickUp(false);
                __instance.objectSprite.dangerous = true;
                __instance.makeObjectsHaveColliders = true;
                gc.objectModifyEnvironmentList.Add(__instance);
            }
        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Tool;
            Item.initCount = 1;
            Item.itemValue = 200;
            Item.stackable = false;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;

            Item.goesInToolbar = true;
        }

        public bool UseItem()
        {
            if (!gc.tileInfo.IsIndoors(Owner.tr.position))
            {
                Owner.SayDialogue("CantAttack");
                if (Owner.isPlayer != 0 && Owner.localPlayer)
                    gc.audioHandler.Play(Owner, "CantDo");
                return false;
            }
            if (Owner.statusEffects.hasTrait("CantAttack") || Owner.agentName == "Doctor" || Owner.statusEffects.hasTrait("CantUseWeapons"))
            {
                Owner.SayDialogue("CantAttack");
                if (Owner.isPlayer != 0 && Owner.localPlayer)
                    gc.audioHandler.Play(Owner, "CantDo");
                return false;
            }

            Inventory!.DropItemAmount(Item, 1, true);
            return true;
        }

    }
}
