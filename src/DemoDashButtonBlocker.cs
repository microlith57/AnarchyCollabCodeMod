using System.Reflection;

using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod.Entities;
using Monocle;

using MonoMod.Utils;

namespace Celeste.Mod.AnarchyCollab2022.Content {
    [Tracked]
    [CustomEntity("AnarchyCollab2022/DemoDashButtonBlocker")]
    public class DemoDashButtonBlocker : Trigger {
        protected static int GlobalBlockerCount = 0;
        private static readonly FieldInfo DEMODASHED_FIELD = typeof(Player).GetField("demoDashed", BindingFlags.NonPublic | BindingFlags.Instance);
        private static On.Celeste.Player.hook_DashBegin dashBeginHook;

        private bool blockGlobal;

        public DemoDashButtonBlocker(EntityData data, Vector2 offset) : base(data, offset) {
            blockGlobal = data.Bool("blockGlobal");
        }

        public override void Added(Scene scene) {
            if (blockGlobal) { GlobalBlockerCount++; }
            base.Added(scene);
        }

        public override void Removed(Scene scene) {
            if (blockGlobal) { GlobalBlockerCount--; }
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            if (blockGlobal) { GlobalBlockerCount--; }
            base.SceneEnd(scene);
        }

        internal static void Load() {
            // Add dash begin hook
            On.Celeste.Player.DashBegin += dashBeginHook = (On.Celeste.Player.orig_DashBegin orig, Player self) => {
                if (GlobalBlockerCount > 0 || self.CollideCheck<DemoDashButtonBlocker>()) {
                    // Set "demo dashed" flag to false
                    DEMODASHED_FIELD.SetValue(self, false);
                }

                orig(self);
            };
        }

        internal static void Unload() {
            // Remove hook
            On.Celeste.Player.DashBegin -= dashBeginHook;
        }
    }
}
