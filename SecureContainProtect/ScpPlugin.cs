using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using RogueLibsCore;
using UnityEngine;

namespace SecureContainProtect
{
    [BepInPlugin(GUID, Name, Version)]
    public class ScpPlugin : BaseUnityPlugin
    {
        public const string GUID = "abbysssal.streetsofrogue.securecontainprotect";
        public const string Name = "SCP";
        public const string Version = "0.1.0";

        public const string Category = "SCP";
        public const int SortingOrder = -539;

        public new static ManualLogSource Logger = null!; // set in Awake
        private static RoguePatcher Patcher = null!;      // set in Awake

        public void Awake()
        {
            Logger = base.Logger;
            Patcher = new RoguePatcher(this);
            RogueLibs.LoadFromAssembly();
        }
        public static RoguePatcher GetPatcher<T>()
        {
            Patcher.TypeWithPatches = typeof(T);
            return Patcher;
        }

        public static RogueSprite[] CreateOctoSprite(string name, SpriteScope scope, byte[] rawData, float rectSize, float ppu = 64f)
        {
            Rect Area(int x, int y) => new Rect(x * rectSize, y * rectSize, rectSize, rectSize);

            return new RogueSprite[]
            {
                RogueLibs.CreateCustomSprite(name + "N", scope, rawData, Area(1, 0), ppu),
                RogueLibs.CreateCustomSprite(name + "NE", scope, rawData, Area(2, 0), ppu),
                RogueLibs.CreateCustomSprite(name + "E", scope, rawData, Area(2, 1), ppu),
                RogueLibs.CreateCustomSprite(name + "SE", scope, rawData, Area(2, 2), ppu),
                RogueLibs.CreateCustomSprite(name + "S", scope, rawData, Area(1, 2), ppu),
                RogueLibs.CreateCustomSprite(name + "SW", scope, rawData, Area(0, 2), ppu),
                RogueLibs.CreateCustomSprite(name + "W", scope, rawData, Area(0, 1), ppu),
                RogueLibs.CreateCustomSprite(name + "NW", scope, rawData, Area(0, 0), ppu),
            };
        }
        public static Sprite[] ConvertQuadraSprite(byte[] rawData, float rectSize, float ppu = 64f)
        {
            Rect Area(int x, int y) => new Rect(x * rectSize, y * rectSize, rectSize, rectSize);

            return new Sprite[]
            {
                RogueUtilities.ConvertToSprite(rawData, Area(0, 0), ppu),
                RogueUtilities.ConvertToSprite(rawData, Area(1, 0), ppu),
                RogueUtilities.ConvertToSprite(rawData, Area(0, 1), ppu),
                RogueUtilities.ConvertToSprite(rawData, Area(1, 1), ppu),
            };
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                GameController gc = GameController.gameController;
                Vector2 pos = gc.playerAgent.curPosition + new Vector2(0, 3f);
                gc.spawnerMain.SpawnAgent(pos, null, "SCP_096");
            }
            if (Input.GetKeyDown(KeyCode.F7))
            {
                Agent scp = GameController.gameController.agentList.Find(static a => a.GetHook<SCP_096>() is not null);
                List<Agent> targets = scp.GetHook<SCP_096>()!.SeenBy;
                Logger.LogWarning($"Current target: {(targets.Count > 0 ? targets[0] : null)} (total: {targets.Count})");
            }
            if (Input.GetKeyDown(KeyCode.F8))
            {
                GameController gc = GameController.gameController;
                gc.playerAgent.interactionHelper.interactionObject
                    = gc.agentList.Find(static a => a.GetHook<SCP_096>() is not null).gameObject;
            }
        }


    }
}
