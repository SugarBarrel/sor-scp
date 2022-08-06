using RogueLibsCore;
using UnityEngine;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.Usable, RogueCategories.NotRealWeapons)]
    public class SCP_096_PhotoFolder : CustomItem, IItemUsable
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_096_PhotoFolder>()
                     .WithName(new CustomNameInfo
                     {
                         English = "Photo of SCP-096",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = "A folder with a photograph of SCP-096's face. Be careful not to look at it, when you open the folder.",
                     })
                     .WithSprite(Properties.Resources.SCP_096_PhotoFolder, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 5,
                         CharacterCreationCost = 5,
                         LoadoutCost = 5,
                     });
        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Tool;
            Item.initCount = 1;
            Item.rewardCount = 1;
            Item.itemValue = 200;
            Item.stackable = false;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;
            Item.goesInToolbar = false;
        }
        public bool UseItem()
        {
            InvDatabase? inv = Inventory;
            if (inv is not null)
            {
                inv.DestroyItem(Item);
                inv.AddItem<SCP_096_Photo>(Count);
                return true;
            }
            return false;
        }

    }
}
