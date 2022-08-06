using RogueLibsCore;
using UnityEngine;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.Technology)]
    public class Me : CustomItem
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<Me>()
                     .WithName(new CustomNameInfo
                     {
                         English = "Me",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = "I look like a normal toaster. I wonder what I do.",
                     })
                     .WithSprite(Properties.Resources.SCP_426, new Rect(0, 0, 64, 64))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 3,
                         CharacterCreationCost = 3,
                         LoadoutCost = 3,
                     });

        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Tool;
            Item.initCount = 1;
            Item.itemValue = 40;
            Item.goesInToolbar = true;
            Item.stackable = true;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;

            Item.canBeUsedOnDoor = true;
            Item.canBeUsedOnSafe = true;
        }

    }
}
