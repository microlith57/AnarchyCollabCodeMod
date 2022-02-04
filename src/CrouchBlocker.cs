using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

using Celeste;
using Celeste.Mod.Entities;
using Monocle;

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AnarchyCollab2022.Content {
    [Tracked]
    [CustomEntity("AnarchyCollab2022/CrouchBlocker")]
    public class CrouchBlocker : Trigger {
        protected static int GlobalBlockerCount = 0;
        private static ILHook updateHook;
        private static Hook duckingSetterHook;
        private static On.Celeste.Player.hook_DashEnd dashEndHook;

        private bool blockGlobal;

        public CrouchBlocker(EntityData data, Vector2 offset) : base(data, offset) {
            blockGlobal = data.Bool("blockGlobal");
        }

        public override void Added(Scene scene) {
            if (blockGlobal) { GlobalBlockerCount++; }
            base.Added(scene);
        }

        public override void Removed(Scene scene) {
            if (blockGlobal) {
                GlobalBlockerCount--; 
                blockGlobal = false;
            }
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            if (blockGlobal) {
                GlobalBlockerCount--; 
                blockGlobal = false;
            }
            base.SceneEnd(scene);
        }

        internal static void Load() {
            // Add hooks
            updateHook = new ILHook(typeof(Player).GetMethod("NormalUpdate", BindingFlags.NonPublic | BindingFlags.Instance), ctx => {
                // Find "Ducking = true;"
                ILCursor cursor = new ILCursor(ctx);
                while (cursor.TryGotoNext(i => i.MatchLdarg(0), i => i.MatchLdcI4(1), i => i.MatchCallvirt(typeof(Player).GetProperty(nameof(Player.Ducking)).GetSetMethod()))) {
                    // Find if condition
                    if (!cursor.TryGotoPrev(i => i.OpCode.FlowControl == FlowControl.Cond_Branch)) { return; }
                    ILLabel dontDuckLabel = (ILLabel)cursor.Instrs[cursor.Index].Operand;
                    cursor.Index++;

                    // Emit check if we should crouch, and if we shouldn't, jump to end of if clause
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<Player, bool>>(player => GlobalBlockerCount <= 0 && !player.CollideCheck<CrouchBlocker>());
                    cursor.Emit(OpCodes.Brfalse, dontDuckLabel);

                    // Return back to end of if clause
                    cursor.TryGotoNext(i => i.Offset >= dontDuckLabel.Target.Offset);
                }
            });

            duckingSetterHook = new Hook(typeof(Player).GetProperty(nameof(Player.Ducking)).GetSetMethod(), (Action<Action<Player, bool>, Player, bool>)((orig, self, ducking) => {
                if (ducking && !self.StartedDashing && (GlobalBlockerCount > 0 || self.CollideCheck<CrouchBlocker>())) {
                    return;
                }
                orig(self, ducking);
            }));

            On.Celeste.Player.DashEnd += dashEndHook = (orig, self) => {
                orig(self);
                if (GlobalBlockerCount > 0 || self.CollideCheck<CrouchBlocker>()) { self.Ducking = false; }
            };
        }

        internal static void Unload() {
            // Remove hooks
            updateHook.Dispose();
            duckingSetterHook.Dispose();
            On.Celeste.Player.DashEnd -= dashEndHook;
        }
    }
}
