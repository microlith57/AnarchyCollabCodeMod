using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

/*
Copyright (c) 2022 microlith57

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

/*
Some code in this file is modified from the CelesteTAS mod source code, which
is released under the MIT license.

See https://github.com/EverestAPI/CelesteTAS-EverestInterop/blob/master/LICENSE
for the full license.
*/

namespace Celeste.Mod.AnarchyCollab2022 {
    [Tracked]
    [CustomEntity("AnarchyCollab2022/SimplifiedGraphicsController")]
    public class SimplifiedGraphicsController : Entity {
        public static SimplifiedGraphicsController Current = null;
        private static bool DrawingHitboxes = false;
        private static bool ShouldApplyChanges => (Current != null && DrawingHitboxes);

        public Color ColorSolid;
        public Color ColorDanger;
        public Color ColorDangerInactive;
        public Color ColorInteractive;
        public bool RenderVirtually;
        public bool ForceOpaque;
        // FIX: distort is still applied for some reason
        public bool SuppressDistort;

        public static VirtualRenderTarget Target;

        public SimplifiedGraphicsController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Tag = (int)Tags.TransitionUpdate;

            ColorSolid = data.HexColor("solids_color", Color.Coral);
            ColorDanger = data.HexColor("danger_color", Color.Red);
            ColorDangerInactive = data.HexColor("danger_inactive_color", Color.DarkRed);
            ColorInteractive = data.HexColor("interactive_color", Color.YellowGreen);

            RenderVirtually = data.Bool("render_virtually", false);
            ForceOpaque = data.Bool("force_opaque", false);
            SuppressDistort = data.Bool("suppress_distort", false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            if (Current == null) {
                Current = this;

                if (RenderVirtually && (Target == null || Target.IsDisposed)) {
                    Target = VirtualContent.CreateRenderTarget("anarchycollab2022-simplified-graphics-overlay", 320, 180);
                }
            }
            var transitionListener = new TransitionListener();
            transitionListener.OnInBegin = () => {
                if (Current.Scene != Scene) {
                    Current = this;

                    if (RenderVirtually && (Target == null || Target.IsDisposed)) {
                        Target = VirtualContent.CreateRenderTarget("anarchycollab2022-simplified-graphics-overlay", 320, 180);
                    }
                }
            };
            Add(transitionListener);
        }

        public override void Removed(Scene scene) {
            Dispose();
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            Dispose();
            base.SceneEnd(scene);
        }

        private void Dispose() {
            if (Current == this) {
                SimplifiedGraphicsController successor = null;
                if (Scene is Level level) {
                    foreach (var entity in level.Tracker.GetEntities<SimplifiedGraphicsController>()) {
                        var controller = entity as SimplifiedGraphicsController;
                        if (controller != null && controller != this) {
                            successor = controller;
                            break;
                        }
                    }
                }
                if ((successor == null || !successor.RenderVirtually) && Target != null && !Target.IsDisposed) { Target.Dispose(); }
                if (successor != null && (Target == null || Target.IsDisposed) && successor.RenderVirtually) {
                    Target = VirtualContent.CreateRenderTarget("anarchycollab2022-simplified-graphics-overlay", 320, 180);
                }
                Current = successor;
            }
        }

        internal static void Load() {
            On.Celeste.GameplayRenderer.Render += GameplayRendererOnRender;
            On.Celeste.Distort.Render += DistortOnRender;
            On.Monocle.Draw.HollowRect_float_float_float_float_Color += ModDrawHollowRect;
            On.Monocle.Draw.Circle_Vector2_float_Color_int += ModDrawCircle;
            On.Monocle.Grid.Render += CombineGridHitbox;
        }

        internal static void Unload() {
            On.Celeste.GameplayRenderer.Render -= GameplayRendererOnRender;
            On.Celeste.Distort.Render -= DistortOnRender;
            On.Monocle.Draw.HollowRect_float_float_float_float_Color -= ModDrawHollowRect;
            On.Monocle.Draw.Circle_Vector2_float_Color_int -= ModDrawCircle;
            On.Monocle.Grid.Render -= CombineGridHitbox;
        }

        private static void GameplayRendererOnRender(On.Celeste.GameplayRenderer.orig_Render orig, GameplayRenderer self, Scene scene) {
            if (Current == null) { orig(self, scene); return; }

            if (Current.RenderVirtually) {
                if (Target == null || Target.IsDisposed) {
                    Target = VirtualContent.CreateRenderTarget("anarchycollab2022-simplified-graphics-overlay", 320, 180);
                }
                Engine.Graphics.GraphicsDevice.SetRenderTarget(Target);
                Engine.Graphics.GraphicsDevice.Clear(Color.Black);
            }

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, Current.ForceOpaque ? BlendState.Opaque : BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, self.Camera.Matrix);
            DrawingHitboxes = true;
            foreach (var entity in scene.Entities) {
                if (entity.Collider == null) { continue; }

                if (entity.GetType().FullName is "Celeste.Mod.CollabUtils2.Entities.AbstractMiniHeart"
                                              or "Celeste.Mod.CollabUtils2.Entities.FakeMiniHeart"
                                              or "Celeste.Mod.CollabUtils2.Entities.MiniHeart") {
                    entity.Collider.Render(self.Camera, entity.Collidable ? Current.ColorInteractive : Current.ColorInteractive * 0.6f);
                }

                switch (entity) {
                    case Player:
                        var collider = entity.Collider;
                        entity.Collider.Render(self.Camera, Color.Red);
                        entity.Collider = DynamicData.For(entity).Get<Hitbox>("hurtbox");
                        entity.Collider.Render(self.Camera, Color.Lime);
                        entity.Collider = collider;
                        break;

                    case Spikes:
                        entity.Collider.Render(self.Camera, entity.Collidable ? Current.ColorDanger : Current.ColorDangerInactive); break;

                    case Booster:
                        var booster = entity as Booster;
                        var dynbooster = DynamicData.For(booster);
                        bool usable = dynbooster.Get<float>("respawnTimer") <= 0f
                                      && dynbooster.Get<float>("cannotUseTimer") <= 0f
                                      && !booster.BoostingPlayer;
                        entity.Collider.Render(self.Camera, (entity.Collidable && usable) ? Current.ColorInteractive : Current.ColorInteractive * 0.6f); break;

                    case Solid:
                    case Platform:
                        entity.Collider.Render(self.Camera, entity.Collidable ? Current.ColorSolid : Current.ColorSolid * 0.6f); break;
                }
            }
            DrawingHitboxes = false;
            Draw.SpriteBatch.End();

            if (Current.RenderVirtually) {
                Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
                orig(self, scene);
            }
        }

        private static void DistortOnRender(On.Celeste.Distort.orig_Render orig, Texture2D source, Texture2D map, bool hasDistortion) {
            if (ShouldApplyChanges && Current.SuppressDistort) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
                Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);
                Draw.SpriteBatch.End();
            } else { orig(source, map, hasDistortion); }
        }

        private static void ModDrawHollowRect(On.Monocle.Draw.orig_HollowRect_float_float_float_float_Color orig,
                float x, float y,
                float width, float height,
                Color color) {
            if (!ShouldApplyChanges) {
                orig(x, y, width, height, color);
                return;
            }

            float fx = (float)Math.Floor(x);
            float fy = (float)Math.Floor(y);
            float cw = (float)Math.Ceiling(width + x - fx);
            float cy = (float)Math.Ceiling(height + y - fy);
            orig(fx, fy, cw, cy, color);
        }

        private static void ModDrawCircle(On.Monocle.Draw.orig_Circle_Vector2_float_Color_int orig,
                Vector2 center, float radius,
                Color color, int resolution) {
            if (!ShouldApplyChanges) {
                orig(center, radius, color, resolution);
                return;
            }

            SimplifiedGraphicsUtils.DrawCircle(center, radius, color);
        }

        private static void CombineGridHitbox(On.Monocle.Grid.orig_Render orig, Grid self, Camera camera, Color color) {
            if (!ShouldApplyChanges) {
                orig(self, camera, color);
                return;
            }

            if (camera == null) {
                for (int x = 0; x < self.CellsX; ++x) {
                    for (int y = 0; y < self.CellsY; ++y) {
                        SimplifiedGraphicsUtils.DrawCombineHollowRect(self, color, x, y, 0, self.CellsX - 1, 0, self.CellsY - 1);
                    }
                }
            } else {
                int left = (int)Math.Max(0.0f, (camera.Left - self.AbsoluteLeft) / self.CellWidth);
                int right = (int)Math.Min(self.CellsX - 1, Math.Ceiling((camera.Right - (double)self.AbsoluteLeft) / self.CellWidth));
                int top = (int)Math.Max(0.0f, (camera.Top - self.AbsoluteTop) / self.CellHeight);
                int bottom = (int)Math.Min(self.CellsY - 1,
                    Math.Ceiling((camera.Bottom - (double)self.AbsoluteTop) / self.CellHeight));

                for (int x = left; x <= right; ++x) {
                    for (int y = top; y <= bottom; ++y) {
                        SimplifiedGraphicsUtils.DrawCombineHollowRect(self, color, x, y, left, right, top, bottom);
                    }
                }
            }
        }
    }
}