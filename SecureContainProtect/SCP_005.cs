using RogueLibsCore;
using UnityEngine;

namespace SecureContainProtect
{
    [ItemCategories(ScpPlugin.Category, RogueCategories.NonUsableTool, RogueCategories.Stealth)]
    public class SCP_005 : CustomItem
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_005>()
                     .WithName(new CustomNameInfo("SCP-005"))
                     .WithDescription(new CustomNameInfo
                     {
                         English = "A key that can unlock any recognizable locking mechanism, be it mechanical, digital, fictional or meta-existential.",
                     })
                     .WithSprite(Properties.Resources.SCP_005, new Rect(0f, 0f, 64f, 64f))
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 5,
                         CharacterCreationCost = 5,
                         LoadoutCost = 5,
                     });

            RogueLibs.CreateCustomName("UseSCP_005", NameTypes.Interface, new CustomNameInfo
            {
                English = "Use SCP-005",
                Russian = @"Использовать SCP-005",
            });

            RogueInteractions.CreateProvider<Door>(static h =>
            {
                if (h.Helper.interactingFar || h.Object.open) return;

                if (h.Agent.inventory.HasItem<SCP_005>())
                {
                    if (h.Object.placedDetonatorInitial is 1 && !h.Agent.isHoisting)
                    {
                        h.AddButton("UseSCP_005", static m =>
                        {
                            m.Object.Disarm();
                            m.Agent.skillPoints.AddPoints("DisarmDetonatorPoints");
                        });
                    }
                    else if (h.Object.locked && !h.HasButton("UseKey"))
                    {
                        h.AddButton("UseSCP_005", static m =>
                        {
                            m.Object.Unlock();
                            m.Object.OpenDoor(m.Agent);
                        });
                    }
                }
            });

            RogueInteractions.CreateProvider<AlarmButton>(static h =>
            {
                if (h.Helper.interactingFar || h.Object.hacked) return;

                if (h.Agent.inventory.HasItem<SCP_005>())
                {
                    h.AddButton("UseSCP_005", static m =>
                    {
                        m.Object.hacked = true;
                        if (!m.gc.serverPlayer) m.gc.playerAgent.objectMult.ObjectAction(m.Object.objectNetID, "AllAccess");
                    });
                }
            });

            RogueInteractions.CreateProvider<Agent>(static h =>
            {
                if (h.Helper.interactingFar) return;

                if (h.Agent.inventory.HasItem<SCP_005>())
                {
                    if (h.Object.agentName is "Slave" && !h.Object.slaveOwners.Contains(h.Agent) && h.Object.slaveOwners.Count > 0
                        && h.Object.inventory.equippedArmorHead.invItemName == "SlaveHelmet")
                    {
                        h.AddButton("UseSCP_005", static m => m.Object.agentInteractions.RemoveSlaveHelmetHack(m.Object, m.Agent));
                    }
                    else if (h.Object.mechEmpty && !h.Agent.transforming)
                    {
                        if (h.Object.health <= 1f)
                            h.SetSideEffect(static m => m.Agent.SayDialogue("MechNeedsOil"));
                        else
                        {
                            h.AddImplicitButton("EnterMech", static m =>
                            {
                                if (!m.Agent.statusEffects.recentlyDepossessed)
                                    m.Agent.statusEffects.MechTransformStart(m.Object);
                            });
                        }

                        if (h.Object.health < h.Object.healthMax)
                        {
                            InvItem? invItem = h.Agent.inventory.FindItem("OilContainer");

                            if (invItem is null || invItem.invItemCount == 0)
                                h.SetStopCallback(static m => m.gc.audioHandler.Play(m.Agent, "CantDo"));
                            else h.AddButton("GiveMechOil", static m => m.Object.agentInteractions.GiveMechOil(m.Object, m.Agent));
                        }
                    }
                }
            });

            RogueInteractions.CreateProvider<Safe>(static h =>
            {
                if (h.Helper.interactingFar || !h.Object.locked) return;

                if (h.Agent.inventory.HasItem<SCP_005>() && !h.HasButton("UseSafeCombination"))
                {
                    h.AddButton("UseSCP_005", static m =>
                    {
                        m.Object.UnlockSafe();
                        m.Object.ShowChest();
                        m.Object.TreasureBonus(m.Agent);
                    });
                }
            });

            RogueInteractions.CreateProvider<TrapDoor>(static h =>
            {
                if (h.Helper.interactingFar || h.Object.opened) return;

                if (h.Agent.inventory.HasItem<SCP_005>())
                {
                    h.AddImplicitButton("UseSCP_005", static m =>
                    {
                        m.Object.OpenTrapDoor(false, false, true);
                        m.StopInteraction();
                    });
                }
            });

            RogueInteractions.CreateProvider<FireHydrant>(static h =>
            {
                if (h.Helper.interactingFar || h.Object.state > 0) return;

                h.AddButton("UseSCP_005", static m =>
                {
                    m.Object.lastHitByAgent = m.Agent;
                    m.Object.direction = m.Object.FindSprayDir(m.Agent);
                    m.Object.BreakHydrant();
                });
            });

            // TODO: A key to your heart?

            // Rejected Ideas:
            // - Window: cannot be opened, only broken;
            // - Robot: can't take out batteries, since, like, that would be weird;
            // - Elevator: the player doesn't want to go up because they haven't finished the missions;
            // - Home Base Door: cannot be acquired at Home Base;
            // - Vending Machines: that would be weird;
            // - Vendor Cart: that would be weird;

        }

        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Tool;
            Item.initCount = 1;
            Item.itemValue = 200;
            Item.stackable = false;
            Item.cantBeCloned = true;
            Item.notInLoadoutMachine = true;

            Item.canBeUsedOnDoor = true;
            Item.canBeUsedOnSafe = true;
        }

    }
}
