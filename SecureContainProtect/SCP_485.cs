using System;
using System.Collections.Generic;
using RogueLibsCore;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SecureContainProtect
{
    public class SCP_485 : CustomItem, IItemUsable, IDoUpdate
    {
        [RLSetup]
        public static void Setup()
        {
            RogueLibs.CreateCustomItem<SCP_485>()
                     .WithName(new CustomNameInfo
                     {
                         English = "SCP-485",
                     })
                     .WithDescription(new CustomNameInfo
                     {
                         English = "A pen with a surprisingly smooth button mechanism.",
                     })
                     .WithSprite(Properties.Resources.SCP_485)
                     .WithUnlock(new ItemUnlock(false)
                     {
                         SortingOrder = ScpPlugin.SortingOrder,
                         UnlockCost = 5,
                         CharacterCreationCost = 5,
                         LoadoutCost = 5,
                     });

            RogueLibs.CreateCustomAudio("SCP_485_Click_1", Properties.Resources.SCP_485_Click_1, AudioType.MPEG);
            RogueLibs.CreateCustomAudio("SCP_485_Click_2", Properties.Resources.SCP_485_Click_2, AudioType.MPEG);

        }

        private static readonly RelationChecker Checker = new RelationChecker();
        static SCP_485()
        {
            // players
            Checker.Know(static (a, b) => a.isPlayer > 0 && b.isPlayer > 0);
            // employees and employers
            Checker.Know(static a => a.employer).Know(static a => a.formerEmployer).Know(static a => a.previousEmployer);
            // slaves and their owners
            Checker.Know(static a => a.slaveOwners).Know(static a => a.slavesOwned);
            // former slaves and their former owners
            Checker.Know(static a => a.formerSlaveOwner).Know(static a => a.formerSlaveOwners);
            // gangsters/mobsters know their gang members (zero is checked for)
            Checker.Know(static a => a.gang).Know(static a => a.gangMembers);
            // former gang members
            Checker.Know(static (a, b) =>
            {
                if (a.gang != 0 && b.formerGang != 0) return a.gang == b.formerGang;
                if (b.gang != 0 && a.formerGang != 0) return b.gang == a.formerGang;
                return a.formerGang != 0 && a.formerGang == b.formerGang;
            });
            // followed and followers
            Checker.Know(static a => a.following).Know(static a => a.previousFollowing);
            // bodyguards and protected
            Checker.Know(static a => a.protectingAgent);
            // muggers and their victims
            Checker.Know(static a => a.shookDownAgent);
            // zombies and their makers
            Checker.Know(static a => a.zombifiedByAgent);
            // commanders and their troops
            Checker.Know(static a => a.commander);
            // people from the same chunk
            Checker.Know(static a => a.startingChunk);
        }

        private int clickCount;

        public override void SetupDetails()
        {
            Item.stackable = false;
            Item.goesInToolbar = true;
        }

        public override string GetSprite()
        {
            return clickCount % 2 == 0 ? "SCP_485_1" : "SCP_485_2";
        }

        public bool UseItem()
        {
            if (clickCount % 2 == 0)
            {
                Agent[] victims = Checker.Find(Owner!);
                RelationHook hook = Owner!.GetOrAddHook<RelationHook>();
                int total = victims.Length + hook.KnowsExtraPeople;
                if (total > 0)
                {
                    int rnd = Random.Range(0, total);
                    if (rnd < victims.Length)
                    {
                        Agent victim = victims[rnd];
                        ScpPlugin.Logger.LogWarning($"Selected {victim}");
                        victim.statusEffects.ChangeHealth(-200f);
                    }
                    else if (hook.KnowsExtraPeople > 0)
                        hook.KnowsExtraPeople--;
                }
            }

            gc.audioHandler.Play(Owner, clickCount % 2 == 0 ? "SCP_485_Click_1" : "SCP_485_Click_2");
            clickCount++;
            return true;
        }

        private float timeIdle;
        private float threshold;
        public void Update()
        {
            if (Owner is null || Owner.isPlayer > 0 || Owner.rb.velocity.magnitude > 0.1)
            {
                timeIdle = 0f;
                return;
            }
            if (timeIdle == 0f) threshold = Random.Range(5f, 60f);
            timeIdle += Time.deltaTime;
            if (timeIdle > threshold)
            {
                UseItem();
                threshold = Random.Range(0.5f, 1f);
                timeIdle = 0.01f;
            }
        }

        private class RelationChecker
        {
            private readonly List<Func<Agent, Agent, bool>> checkers = new List<Func<Agent, Agent, bool>>();

            public RelationChecker Know(Func<Agent, Agent, bool> know)
            {
                checkers.Add((a, b) => know(a, b) || know(b, a));
                return this;
            }

            public RelationChecker Know(Func<Agent, Agent> know)
                => Know((a, b) => know(a) == b || know(b) == a);
            public RelationChecker Know(Func<Agent, ICollection<Agent>> know)
                => Know((a, b) => know(a).Contains(b) || know(b).Contains(a));
            public RelationChecker Know(Func<Agent, int> know)
                => Know((a, b) =>
                {
                    int gang = know(a);
                    return gang > 0 && gang == know(b);
                });

            public Agent[] Find(Agent user)
            {
                List<Agent> found = new List<Agent>();
                found.AddRange(user.relationships.alignedLoyalList);

                foreach (Agent other in gc.agentList)
                {
                    if (other.dead || other.electronic || other.inhuman || other.disappeared) continue;
                    if (found.Contains(other)) continue;
                    if (user.relationships.GetRelCode(other) is relStatus.Aligned or relStatus.Loyal or relStatus.Submissive
                        || checkers.Exists(check => check(user, other)))
                        found.Add(other);
                }
                found.Remove(user);
                return found.ToArray();
            }

        }
        private class RelationHook : HookBase<PlayfieldObject>
        {
            public int KnowsExtraPeople { get; set; }
            protected override void Initialize()
            {
                Agent agent = (Agent)Instance;
                KnowsExtraPeople = agent.HasTrait("NoFollowers") ? 10
                    : agent.HasTrait("MoreFollowers") ? 100
                    : agent.HasTrait("ZombieArmy") ? 200
                    : 50;
            }
        }

    }
}
