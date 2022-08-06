using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using RogueLibsCore;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace SecureContainProtect
{
    public class SCP_096 : HookBase<PlayfieldObject>, IDoUpdate, IDisposable
    {
        [RLSetup]
        public static void Setup()
        {
            ScpPlugin.CreateOctoSprite("SCP_096_Head", SpriteScope.Hair, Properties.Resources.SCP_096_Head, 64f, 64f);
            ScpPlugin.CreateOctoSprite("SCP_096", SpriteScope.Bodies, Properties.Resources.SCP_096_Body, 64f, 64f);

            SCP_096_VisualEffect.ScrambleFrames = ScpPlugin.ConvertQuadraSprite(Properties.Resources.SCP_096_ScrambleFrame, 64f);
            SCP_096_VisualEffect.ScrambleCrosshair = RogueUtilities.ConvertToSprite(Properties.Resources.SCP_096_ScrambleCrosshair);
            SCP_096_VisualEffect.BlackSprite = RogueUtilities.ConvertToSprite(Properties.Resources.SCP_096_ScrambleBox);

            RogueLibs.CreateCustomTrait<SCP_096_Trait>();

            RoguePatcher patcher = ScpPlugin.GetPatcher<SCP_096>();
            patcher.Prefix(typeof(StatusEffects), nameof(StatusEffects.SetupDeath),
                           new Type[3] { typeof(PlayfieldObject), typeof(bool), typeof(bool) });

#pragma warning disable CS0618 // Type or member is obsolete
            patcher.Prefix(typeof(AudioHandler), nameof(AudioHandler.Play),
                           new Type[4] { typeof(PlayfieldObject), typeof(string), typeof(NetworkInstanceId), typeof(bool) });
            patcher.Prefix(typeof(SpawnerMain), nameof(SpawnerMain.SpawnStatusText),
                           new Type[] { typeof(PlayfieldObject), typeof(string), typeof(string), typeof(string),
                               typeof(NetworkInstanceId), typeof(string), typeof(string) });
#pragma warning restore CS0618 // Type or member is obsolete

            patcher.Prefix(typeof(WallDestroyDetector), nameof(WallDestroyDetector.collided));
            patcher.Prefix(typeof(AgentHitbox), nameof(AgentHitbox.HitAgent));

            patcher.Postfix(typeof(Agent), nameof(Agent.SetupAgentStats));

            RogueLibs.CreateCustomName("SCP_096", NameTypes.Agent, new CustomNameInfo
            {
                English = "SCP-096",
            });

            RogueLibs.CreateCustomName("SCP_096_Cry1", NameTypes.Dialogue, new CustomNameInfo
            {
                English = @"WAAAAA!!  WAAAAAA!!!",
            });
            RogueLibs.CreateCustomName("SCP_096_Cry2", NameTypes.Dialogue, new CustomNameInfo
            {
                English = @"WAAAAA!!!",
            });
            RogueLibs.CreateCustomName("SCP_096_Cry3", NameTypes.Dialogue, new CustomNameInfo
            {
                English = @"WAAAAAAAAAAAAA!!",
            });
            RogueLibs.CreateCustomName("SCP_096_Cry4", NameTypes.Dialogue, new CustomNameInfo
            {
                English = @"WAA!!  WAAAAAAAAAAAAAAA!!!",
            });

        }

        public static void StatusEffects_SetupDeath(StatusEffects __instance)
        {
            if (__instance.agent.GetHook<SCP_096>() != null)
            {
                __instance.AddStatusEffect("Resurrection", false, true, 1);
                __instance.agent.resurrect = true;
                __instance.agent.fullHealthAfterResurrect = true;
            }

            foreach (Agent agent in GameController.gameController.agentList)
            {
                SCP_096? scp = agent.GetHook<SCP_096>();
                if (scp?.SeenBy.Contains(__instance.agent) == true)
                    scp.OnKilled(__instance.agent);
            }

        }
        public static bool AudioHandler_Play(PlayfieldObject playfieldObject, string clipName)
        {
            if (clipName is "AgentRevive" or "AgentOK" && playfieldObject is Agent a && a.GetHook<SCP_096>() != null)
                return false;
            return true;
        }
        public static bool SpawnerMain_SpawnStatusText(PlayfieldObject myPlayfieldObject, string textType)
        {
            if (textType == "HealthUp" && myPlayfieldObject.GetHook<SCP_096>() != null)
                return false;
            return true;
        }

        public static bool WallDestroyDetector_collided(WallDestroyDetector __instance, Collider2D other)
        {
            if (__instance.agent.GetHook<SCP_096>() is null)
                return true;

            if (__instance.agent.isPlayer != 0 && !__instance.agent.localPlayer)
                return false;
            if (!__instance.gc.loadComplete || __instance.agent.ghost)
                return false;

            if (other.CompareTag("ExtraCollider"))
            {
                other = other.gameObject.transform.parent.GetComponent<Collider2D>();
                if (other == null) return false;
            }
            if (other.CompareTag("Agent")) return false;
            if (other.CompareTag("Wall"))
            {
                GameObject gameObject = other.gameObject;
                Vector3 position = other.transform.position;
                TileData tileData = __instance.gc.tileInfo.GetTileData(position);

                Door.freerAgent = __instance.agent.chargingForward ? __instance.agent : __instance.agent.lastHitByAgent;

                __instance.gc.tileInfo.DestroyWallTileAtPosition(position.x, position.y, Vector3.zero, true, __instance.agent.lastHitByAgent, false, true, false, __instance.agent, false);
                __instance.tilesDestroyed = true;
                __instance.gc.audioHandler.Play(__instance.agent, "WallDestroy");
                if (tileData.wallMaterial == wallMaterialType.Glass)
                {
                    __instance.gc.audioHandler.Play(__instance.agent, "WallDestroyGlass");
                }
                gameObject.layer = 1;
                if ((__instance.gc.serverPlayer || __instance.agent.objectMultPlayfield.clientHasControl || !__instance.gc.serverPlayer && __instance.agent.isPlayer != 0 && __instance.agent.localPlayer) && !__instance.agent.statusEffects.hasTrait("HitObjectsNoNoise"))
                {
                    __instance.gc.spawnerMain.SpawnNoise(position, 1f, null, null, __instance.agent);
                }
                if (tileData.wallMaterial != wallMaterialType.Border)
                    __instance.gc.stats.AddDestructionQuestPoints();
                if (__instance.agent.isPlayer > 0)
                    __instance.gc.stats.AddToStat(__instance.agent, "Destruction", 1);

                __instance.gc.OwnCheck(__instance.agent, gameObject, "Normal", 0);
                __instance.gc.ScreenShake(0.25f, 80f, __instance.agent.tr.position, __instance.agent);
            }
            else if (other.CompareTag("ObjectReal") || other.CompareTag("ObjectRealSprite"))
            {
                ObjectReal objectReal = other.GetComponent<ObjectReal>();
                if (other.CompareTag("ObjectRealSprite"))
                {
                    objectReal = other.name.Contains("ExtraSprite")
                        ? other.transform.parent.transform.parent.GetComponent<ObjectReal>()
                        : other.GetComponent<ObjectSprite>().objectReal;
                }

                if (__instance.destroyedObjects.Contains(objectReal)) return false;
                __instance.destroyedObjects.Add(objectReal);

                if (objectReal.OnFloor && __instance.agent.go.layer != 20)
                    objectReal.ObjectCollide(__instance.agent, true);

                objectReal.Damage(__instance.agent);
                if (__instance.agent.oma.mindControlled && (objectReal.destroyed || objectReal.destroying) && __instance.gc.multiplayerMode && !__instance.gc.serverPlayer)
                {
                    __instance.gc.playerAgent.objectMult.CallCmdDestroyObjectSimple(objectReal.tr.position, false);
                }
                if (objectReal.destroying || objectReal.justDamaged)
                {
                    if (!objectReal.noDestroyEffects)
                        __instance.gc.ScreenShake(0.25f, 80f, __instance.agent.tr.position, __instance.agent);
                }
                if (__instance.gc.serverPlayer && !objectReal.noDamageNoise && !__instance.agent.statusEffects.hasTrait("HitObjectsNoNoise"))
                {
                    __instance.gc.spawnerMain.SpawnNoise(objectReal.FindDamageNoisePos(objectReal.tr.position),
                        objectReal.noiseHitVol, null, null, __instance.agent);
                }
                __instance.gc.OwnCheck(__instance.agent, other.gameObject, "AgentBody", 1);

            }

            return false;
        }
        public static bool AgentHitbox_HitAgent(AgentHitbox __instance, GameObject hitObject)
        {
            if (__instance.agent.GetHook<SCP_096>() is null) return true;
            return !hitObject.CompareTag("AgentSprite");
        }

        public static void Agent_SetupAgentStats(Agent __instance)
        {
            if (__instance.agentName == "SCP_096")
            {
                __instance.AddHook<SCP_096>();

                __instance.AddTrait<SCP_096_Trait>();
                //__instance.statusEffects.AddTrait("CantSpeakEnglish");
                __instance.statusEffects.AddTrait("HearingBlocked");

                __instance.statusEffects.AddTrait("KnockbackLess2");
                __instance.statusEffects.AddTrait("MoreDamageWhenHealthLow2");
                __instance.statusEffects.AddTrait("CantUseWeapons2");
                __instance.statusEffects.AddTrait("KnockWeapons");
                __instance.statusEffects.AddTrait("FastMelee");
                __instance.statusEffects.AddTrait("LongLunge2");
                __instance.modMeleeSkill = 5;
                __instance.modToughness = 4;

                __instance.agentHitboxScript.hasSetup = true;
                __instance.agentHitboxScript.hairType = "SCP_096_Head";
                __instance.agentHitboxScript.facialHairType = "None";
                __instance.agentHitboxScript.bodyColor = new Color32(255, 255, 255, 255);
                __instance.agentHitboxScript.legsColor = new Color32(160, 160, 160, 255);
                __instance.agentHitboxScript.skinColor = new Color32(160, 160, 160, 255);
                __instance.agentHitboxScript.head.SetSprite("Clear");

                __instance.agentCategories.Clear();
                __instance.agentCategories.Add(ScpPlugin.Category);
                __instance.agentCategories.Add("Melee");
                __instance.agentCategories.Add("Movement");
                __instance.setInitialCategories = true;

                __instance.preventStatusEffects = true;
                __instance.preventsMindControl = true;
                __instance.inhuman = true;
                __instance.dontStopForDanger = true;
                __instance.cantChallengeToFight = true;
                __instance.cantCannibalize = true;
                __instance.wontFlee = true;

                __instance.SetEndurance(100);
                __instance.health = __instance.healthMax;
                __instance.SetStrength(2);
                __instance.SetSpeed(-4);
                __instance.SetAccuracy(0);
            }
            __instance.agentHitboxScript.SetUsesNewHead();
            __instance.objectSprite.RefreshRenderer();
        }

        public Agent Me => (Agent)Instance;
        public readonly List<Agent> SeenBy = new List<Agent>();

        private SCP_096_VisualEffect visual = null!;
        private SCP_096_VisualEffect visualWb = null!;
        protected override void Initialize()
        {
            GameObject vfx = new GameObject("SCP-096 VFX");
            vfx.transform.SetParent(Me.agentHitboxScript.head.transform.parent);
            vfx.transform.localPosition = new Vector3(0f, 0f, 1e-3f);
            visual = vfx.AddComponent<SCP_096_VisualEffect>();
            visual.SCP = this;

            GameObject vfxWb = new GameObject("SCP-096 VFX (WB)");
            vfxWb.transform.SetParent(Me.agentHitboxScript.headWB.transform.parent);
            vfxWb.transform.localPosition = new Vector3(0f, 0f, 1e-3f);
            visualWb = vfxWb.AddComponent<SCP_096_VisualEffect>();
            visualWb.SCP = this;
        }
        public void Dispose()
        {
            Object.Destroy(visual.gameObject);
            Object.Destroy(visualWb.gameObject);
        }

        public void OnSeen(Agent agent)
        {
            if (SeenBy.Contains(agent)) return;
            if (agent == Me || agent.agentName is "Ghost" or "ObjectAgent" || agent.dead || agent.electronic) return;
            ScpPlugin.Logger.LogWarning($"Triggered by {agent.agentName}.");

            agent.relationships.SetRel(Me, "Hostile");
            Me.relationships.SetRel(agent, "Hostile");
            SeenBy.Add(agent);
            if (SeenBy.Count is 1)
            {
                if (rampaging != null) agent.StopCoroutine(rampaging);
                rampaging = agent.StartCoroutine(RampageCoroutine());
            }
        }
        public void OnKilled(Agent agent) => SeenBy.Remove(agent);

        public float TimeRampaging;

        private Coroutine? rampaging;
        public IEnumerator RampageCoroutine()
        {
            TimeRampaging = float.Epsilon;

            // wind up
            yield return null;
            Me.gc.audioHandler.Play(Me, "AgentAnnoyed");
            Me.Say(Me.gc.nameDB.GetName("SCP_096_Cry1", "Dialogue"));

            yield return new WaitForSeconds(1f);

            Me.gc.audioHandler.Play(Me, "AgentAnnoyed");
            Me.Say(Me.gc.nameDB.GetName("SCP_096_Cry2", "Dialogue"));

            yield return new WaitForSeconds(0.8f);

            Me.gc.audioHandler.Play(Me, "AgentAnnoyed");
            Me.Say(Me.gc.nameDB.GetName("SCP_096_Cry3", "Dialogue"));

            yield return new WaitForSeconds(0.6f);

            Me.gc.audioHandler.Play(Me, "AgentAnnoyed");
            Me.Say(Me.gc.nameDB.GetName("SCP_096_Cry4", "Dialogue"));

            if (SeenBy.Count is 0) yield break;

            // start rampaging
            Me.loud = true;
            Me.SetSpeed(-4);
            Me.SetStrength(4);

            while (SeenBy.Count > 0)
            {
                // rampage
                float before = TimeRampaging;
                TimeRampaging += Time.deltaTime;

                if (!Me.dead)
                {
                    Agent target = SeenBy.MinBy(a => Vector2.Distance(Me.tr.position, a.tr.position));
                    Me.SetOpponent(target);
                    Me.agentInteractions.Attack(Me, Me, target, true);
                    float dist = Vector2.Distance(target.tr.position, Me.tr.position);
                    float anger = TimeRampaging / 60f;
                    float acc = Mathf.Clamp(dist * (1f + anger), 4f, 30f);
                    Me.rb.AddForce((target.tr.position - Me.tr.position).normalized * 5f * acc);
                    //Me.chargingForward = true;
                    //Me.chargeDirection = (target.curPosition - Me.curPosition).normalized;

                    if (Vector2.Distance(target.tr.position, Me.tr.position) < 1.92f && !Me.melee.attackAnimPlaying)
                    {
                        Me.melee.attackObject = target;
                        Me.melee.Attack(false);
                    }
                }

                if (before < 10f && TimeRampaging >= 10f)
                {
                    Me.SetStrength(6);
                }
                if (before < 20f && TimeRampaging >= 20f)
                {
                    Me.SetStrength(8);
                    Me.agentCollider.radius = 0.48f;
                    Me.wallDestroyDetector.circleCollider.radius = 0.64f;
                    Me.wallDestroyDetector.EnableIfPossible();
                    Me.depthMask.transform.localPosition = Me.depthMask.transform.localPosition.WithY(-1.44f);
                }
                if (before < 40f && TimeRampaging >= 40f)
                {
                    Me.SetStrength(10);
                }
                if (before < 60f && TimeRampaging >= 60f)
                {
                    Me.SetStrength(12);
                }

                yield return null;
            }

            Me.chargingForward = false;
            Me.chargeDirection = Vector2.zero;

            // stop rampaging
            Me.loud = false;
            Me.SetSpeed(-4);
            Me.SetStrength(8);
            Me.agentCollider.radius = 0.24f;
            Me.wallDestroyDetector.circleCollider.radius = 0.24f;
            Me.wallDestroyDetector.DisableIfPossible();
            Me.depthMask.transform.localPosition = Me.depthMask.transform.localPosition.WithY(-0.82f);
            TimeRampaging = 0f;
            rampaging = null;

            // calm down
        }

        public static bool CanSee(Agent a, Vector2 pos, float angle)
            => Vector2.Angle(pos - a.curPosition, a.curRightAngle) < angle
               && !Physics2D.Linecast(a.curPosition, pos, a.movement.myLayerMaskEfficient);

        private float cooldown;
        public void Update()
        {
            if (TimeRampaging is 0f or > 10f)
            {
                cooldown -= Time.deltaTime;
                if (cooldown <= 0f)
                {
                    cooldown = UnityEngine.Random.Range(3f, 10f);
                    Me.SayDialogue($"SCP_096_Cry{UnityEngine.Random.Range(1, 5)}");
                }
            }

            if (Me.agentName != "SCP_096")
            {
                Object.Destroy(visual);
                Me.RemoveHook(this);
                return;
            }

            Me.agentHitboxScript.head.SetSprite("Clear");
            Me.agentHitboxScript.headH.SetSprite("Clear");
            Me.agentHitboxScript.headWB.SetSprite("Clear");
            Me.agentHitboxScript.headWBH.SetSprite("Clear");

            foreach (Agent agent in GameController.gameController.agentList)
            {
                if (Vector2.Distance(Me.curPosition, agent.curPosition) < 16f
                    && CanSee(Me, agent.curPosition, 120f) && CanSee(agent, Me.curPosition, 60f))
                {
                    OnSeen(agent);
                }
            }

            if (SeenBy.Count > 0)
            {
                Me.agentActive = true;

                Me.wontFlee = true;
                Me.mustFlee = false;
                Me.inFleeCombat = false;
                Me.scary = true;
                Me.scary2 = true;
                Me.combat.moveAwayFromAgent = null;
                Me.combat.moveAwayFromInCombatAgent = null;
                Me.combat.hasMoveAwayFromAgent = false;
                Me.combat.hasMoveAwayFromInCombatAgent = false;
                Me.stunLocked = 0;

                Me.attackCooldown = 0f;
                Me.weaponCooldown = 0f;
                Me.combat.AIHold = 0f;
                Me.combat.meleeJustBlockedCooldown = 0f;
                Me.combat.meleeJustHitCloseCooldown = 0f;
                Me.combat.meleeJustHitCooldown = 0f;
                Me.combat.personalCooldown = 0f;
            }
        }

    }
    public class SCP_096_Trait : CustomTrait, ITraitUpdateable
    {
        public float Interval { get; set; } = 0.2f;
        public float Health { get; set; } = 2f;

        public override void OnAdded()
        {
            Owner.agentSpriteTransform.localScale = new Vector3(1.5f, 2.5f, 1f);
        }
        public override void OnRemoved()
        {
            Owner.agentSpriteTransform.localScale = new Vector3(1f, 1f, 1f);
        }
        public void OnUpdated(TraitUpdatedArgs e)
        {
            e.UpdateDelay = Interval;
            Owner.ChangeHealth(Health);
        }
    }
    public class SCP_096_VisualEffect : MonoBehaviour
    {
        public static Sprite[] ScrambleFrames = null!;  // set in Start
        public static Sprite ScrambleCrosshair = null!; // set in Start
        public static Sprite BlackSprite = null!;       // set in Start
        private readonly Random rnd = new Random();

        public SCP_096? SCP;

        public Vector2 Center => transform.localPosition;

        private Canvas canvas = null!;   // set in Start
        private Image insurance = null!; // set in Start
        private readonly Queue<Image> boxes = new Queue<Image>();
        private readonly List<Image> frames = new List<Image>();
        private readonly List<Image> crosshairs = new List<Image>();

        private static Image CreatePart(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            RectTransform rect = child.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1f, 1f);
            return child.AddComponent<Image>();
        }
        public void Start()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            transform.localPosition = new Vector3(0f, 0.16f, -5f);
            transform.localScale = new Vector3(0.15f, 0.25f, 1f);

            insurance = CreatePart(transform, "VFX Insurance");
            insurance.sprite = BlackSprite;
            insurance.rectTransform.sizeDelta = new Vector2(0.6f, 0.6f);

            SCP!.Me.agentHitboxScript.head.enabled = false;
            SCP.Me.agentHitboxScript.facialHair.enabled = false;

            for (int i = 0; i < 5; i++)
            {
                Image box = CreatePart(transform, $"VFX Box {i}");
                box.sprite = BlackSprite;
                boxes.Enqueue(box);
            }
            for (int i = 0; i < boxes.Count; i++)
                UpdateBoxes();

            for (int i = 0; i < 4; i++)
            {
                Image frame = CreatePart(transform, $"VFX Frame {i}");
                frame.sprite = ScrambleFrames[rnd.Next(ScrambleFrames.Length)];
                frames.Add(frame);
            }
            UpdateFrames();
            for (int i = 0; i < 3; i++)
            {
                Image crosshair = CreatePart(transform, $"VFX Crosshair {i}");
                crosshair.sprite = ScrambleCrosshair;
                crosshairs.Add(crosshair);
            }
            UpdateCrosshairs();
        }

        public void UpdateBoxes()
        {
            // size of the insurance box is 1.2
            const float maxOffset = 0.6f;
            const float minSize = 0.2f;
            const float maxSize = 0.8f;

            Image box = boxes.Dequeue();
            boxes.Enqueue(box);

            float posX = Center.x + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
            float posY = Center.y + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
            float sizeX = minSize + (float)rnd.NextDouble() * (maxSize - minSize);
            float sizeY = minSize + (float)rnd.NextDouble() * (maxSize - minSize);

            box.rectTransform.localPosition = new Vector2(posX, posY);
            box.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);

        }
        public void UpdateFrames()
        {
            // size of the insurance box is 1.2
            const float maxOffset = 0.8f;
            const float minSize = 0.2f;
            const float maxSize = 0.7f;

            foreach (Image frame in frames)
            {
                frame.sprite = ScrambleFrames[rnd.Next(ScrambleFrames.Length)];

                float posX = Center.x + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
                float posY = Center.y + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
                float sizeX = minSize + (float)rnd.NextDouble() * (maxSize - minSize);
                float sizeY = minSize + (float)rnd.NextDouble() * (maxSize - minSize);

                frame.rectTransform.localPosition = new Vector2(posX, posY);
                frame.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            }
        }
        public void UpdateCrosshairs()
        {
            // size of the insurance box is 1.2
            const float maxOffset = 1f;
            const float minSize = 0.2f;
            const float maxSize = 0.3f;

            foreach (Image crosshair in crosshairs)
            {
                float posX = Center.x + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
                float posY = Center.y + ((float)rnd.NextDouble() * 2 - 1) * maxOffset;
                float sizeX = minSize + (float)rnd.NextDouble() * (maxSize - minSize);
                float sizeY = minSize + (float)rnd.NextDouble() * (maxSize - minSize);

                crosshair.rectTransform.localPosition = new Vector2(posX, posY);
                crosshair.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            }
        }

        private float boxCooldown;
        private float frameCooldown;
        public void Update()
        {
            boxCooldown -= Time.deltaTime;
            if (boxCooldown <= 0f)
            {
                UpdateBoxes();
                boxCooldown = 0.03f;
            }
            frameCooldown -= Time.deltaTime;
            if (frameCooldown <= 0f)
            {
                UpdateFrames();
                UpdateCrosshairs();
                frameCooldown = 0.25f;
            }
        }


    }
}
