using System;
using Monocle;

namespace Celeste.Mod.AnarchyCollab2022 {
    public class AnarchyCollabModule : EverestModule {

        public static AnarchyCollabModule Instance { get; private set; }
        public static string Name => Instance?.Metadata?.Name;

        private On.Celeste.Level.hook_End levelEndHook;

        public AnarchyCollabModule() {
            Instance = this;
        }

        public override void Load() {
            // Remove entities with "CleanupRemove" attribute when the level ends
            On.Celeste.Level.End += levelEndHook = (On.Celeste.Level.orig_End orig, Level level) => {
                foreach (Entity entity in level.Entities) {
                    if (entity.GetType().GetCustomAttributes(typeof(CleanupRemoveAttribute), true).Length > 0) {
                        entity.Removed(level);
                    }
                }

                orig(level);
            };
            JankTempleCustomEvent.Load();
            ElevatorCutsceneUtils.Load();
            FragmentsStarJumpBlock.Load();
        }

        public override void Unload() {
            On.Celeste.Level.End -= levelEndHook;
            JankTempleCustomEvent.Unload();
            ElevatorCutsceneUtils.Unload();
            FragmentsStarJumpBlock.Unload();
        }
    }
}
