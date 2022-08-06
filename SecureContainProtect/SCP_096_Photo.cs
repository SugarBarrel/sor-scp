using System.Text;
using RogueLibsCore;
using UnityEngine;
using Random = System.Random;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.NonUsableTool, RogueCategories.Passive, RogueCategories.NotRealWeapons)]
    public class SCP_096_Photo : CustomItem, IItemUsable, IDoUpdate
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_096_Photo>()
                     .WithName(new CustomNameInfo
                     {
                         English = "Photo of SCP-096",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = $"The photograph of SCP-096's face... {RandomizeStyle("AND YOU LOOKED AT IT")}.",
                     })
                     .WithSprite(Properties.Resources.SCP_096_Photo, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         IsAvailable = false,
                         IsAvailableInCC = false,
                         IsAvailableInItemTeleporter = false,
                     });

            RoguePatcher patcher = ScpPlugin.GetPatcher<SCP_096_Photo>();
            patcher.Postfix(typeof(InvSlot), nameof(InvSlot.UpdateInvSlot));
        }

        private static string RandomizeStyle(string text)
        {
            Random rnd = new Random(488755541);
            StringBuilder sb = new StringBuilder();
            for (int i = 0, length = text.Length; i < length; i++)
            {
                bool bold = rnd.Next(2) is 0;
                bool italic = rnd.Next(2) is 0;

                if (bold) sb.Append("<b>");
                if (italic) sb.Append("<i>");
                sb.Append(text[i]);
                if (italic) sb.Append("</i>");
                if (bold) sb.Append("</b>");
            }
            return sb.ToString();
        }

        public static void InvSlot_UpdateInvSlot(InvSlot __instance)
        {
            if (__instance.item?.invItemName is nameof(SCP_096_Photo) && __instance.overSlot)
            {
                __instance.agent?.GetOrAddHook<SCP_096_PhotoSubject>().SetSeenThisFrame();
            }
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
                inv.AddItem<SCP_096_PhotoFolder>(Count);
                return true;
            }
            return false;
        }

        public void Update()
        {
            Item? item = gc.itemList.Find(i => i.invItem == Item);
            if (item is null) return;

            bool any = false;
            foreach (Agent agent in gc.agentList)
            {
                if (Vector2.Distance(agent.curPosition, item.tr.position) is >= 0.34f and <= 16f
                    && SCP_096.CanSee(agent, item.tr.position, 90f))
                {
                    if (agent.HasHook<SCP_096>()) continue;
                    agent.GetOrAddHook<SCP_096_PhotoSubject>().SetSeenThisFrame();
                    any = true;
                }
            }
            item.flashingRepeatedly = any;
            item.objectSprite.flashingRepeatedly = any;
        }

    }
    public class SCP_096_PhotoSubject : HookBase<PlayfieldObject>, IDoUpdate
    {
        public Agent Agent => (Agent)Instance;
        protected override void Initialize() { }
        public float SeenTime { get; set; }
        public float Threshold { get; set; } = 0.2f;

        private bool seenThisFrame;
        public void SetSeenThisFrame() => seenThisFrame = true;
        public void Update()
        {
            if (seenThisFrame)
            {
                SeenTime += Time.deltaTime;
                if (SeenTime > Threshold)
                {
                    foreach (Agent agent in GameController.gameController.agentList)
                    {
                        SCP_096? scp = agent.GetHook<SCP_096>();
                        scp?.OnSeen(Agent);
                    }
                    SeenTime = 0f;
                }
                seenThisFrame = false;
            }
            else SeenTime -= Time.deltaTime;
        }

    }
}
