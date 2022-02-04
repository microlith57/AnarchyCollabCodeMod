using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;

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

namespace Celeste.Mod.AnarchyCollab2022 {
    [Tracked]
    [CustomEntity("AnarchyCollab2022/FragmentsStarJumpBlock")]
    public class FragmentsStarJumpBlock : Solid {
        private bool sinks;
        private float startY;
        private float yLerp;
        private float sinkTimer;

        private static On.Celeste.Player.hook_RefillDash refillDashHook;

        public FragmentsStarJumpBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, safe: false) {
            Depth = -9000;
            sinks = data.Bool("sinks");

            Add(new LightOcclude());
            Add(new ClimbBlocker(edge: false));
            startY = Y;
            SurfaceSoundIndex = SurfaceIndex.AuroraGlass;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            List<MTexture> tex_edge_h = GFX.Game.GetAtlasSubtextures("objects/starjumpBlock/edgeH");
            List<MTexture> tex_edge_v = GFX.Game.GetAtlasSubtextures("objects/starjumpBlock/edgeV");
            List<MTexture> tex_corner = GFX.Game.GetAtlasSubtextures("objects/starjumpBlock/corner");
            for (int i = 8; (float)i < base.Width - 8f; i += 8) {
                if (Open(i, -8f)) {
                    Image edge_h = new Image(Calc.Random.Choose(tex_edge_h));
                    edge_h.CenterOrigin();
                    edge_h.Position = new Vector2(i + 4, 4f);
                    Add(edge_h);
                }
                if (Open(i, base.Height)) {
                    Image edge_h = new Image(Calc.Random.Choose(tex_edge_h));
                    edge_h.CenterOrigin();
                    edge_h.Scale.Y = -1f;
                    edge_h.Position = new Vector2(i + 4, base.Height - 4f);
                    Add(edge_h);
                }
            }
            for (int j = 8; (float)j < base.Height - 8f; j += 8) {
                if (Open(-8f, j)) {
                    Image edge_v = new Image(Calc.Random.Choose(tex_edge_v));
                    edge_v.CenterOrigin();
                    edge_v.Scale.X = -1f;
                    edge_v.Position = new Vector2(4f, j + 4);
                    Add(edge_v);
                }
                if (Open(base.Width, j)) {
                    Image edge_v = new Image(Calc.Random.Choose(tex_edge_v));
                    edge_v.CenterOrigin();
                    edge_v.Position = new Vector2(base.Width - 4f, j + 4);
                    Add(edge_v);
                }
            }
            Image corner = null;
            if (Open(-8f, 0f) && Open(0f, -8f)) {
                corner = new Image(Calc.Random.Choose(tex_corner));
                corner.Scale.X = -1f;
            } else if (Open(-8f, 0f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_v));
                corner.Scale.X = -1f;
            } else if (Open(0f, -8f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_h));
            }
            if (corner != null) {
                corner.CenterOrigin();
                corner.Position = new Vector2(4f, 4f);
                Add(corner);
            }
            corner = null;
            if (Open(base.Width, 0f) && Open(base.Width - 8f, -8f)) {
                corner = new Image(Calc.Random.Choose(tex_corner));
            } else if (Open(base.Width, 0f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_v));
            } else if (Open(base.Width - 8f, -8f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_h));
            }
            if (corner != null) {
                corner.CenterOrigin();
                corner.Position = new Vector2(base.Width - 4f, 4f);
                Add(corner);
            }
            corner = null;
            if (Open(-8f, base.Height - 8f) && Open(0f, base.Height)) {
                corner = new Image(Calc.Random.Choose(tex_corner));
                corner.Scale.X = -1f;
            } else if (Open(-8f, base.Height - 8f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_v));
                corner.Scale.X = -1f;
            } else if (Open(0f, base.Height)) {
                corner = new Image(Calc.Random.Choose(tex_edge_h));
            }
            if (corner != null) {
                corner.Scale.Y = -1f;
                corner.CenterOrigin();
                corner.Position = new Vector2(4f, base.Height - 4f);
                Add(corner);
            }
            corner = null;
            if (Open(base.Width, base.Height - 8f) && Open(base.Width - 8f, base.Height)) {
                corner = new Image(Calc.Random.Choose(tex_corner));
            } else if (Open(base.Width, base.Height - 8f)) {
                corner = new Image(Calc.Random.Choose(tex_edge_v));
            } else if (Open(base.Width - 8f, base.Height)) {
                corner = new Image(Calc.Random.Choose(tex_edge_h));
            }
            if (corner != null) {
                corner.Scale.Y = -1f;
                corner.CenterOrigin();
                corner.Position = new Vector2(base.Width - 4f, base.Height - 4f);
                Add(corner);
            }
        }

        private bool Open(float x, float y) {
            return !base.Scene.CollideCheck<FragmentsStarJumpBlock>(new Vector2(base.X + x + 4f, base.Y + y + 4f));
        }

        public override void Update() {
            base.Update();

            if (sinks) {
                if (HasPlayerRider()) {
                    sinkTimer = 0.1f;
                } else if (sinkTimer > 0f) {
                    sinkTimer -= Engine.DeltaTime;
                }
                if (sinkTimer > 0f) {
                    yLerp = Calc.Approach(yLerp, 1f, 1f * Engine.DeltaTime);
                } else {
                    yLerp = Calc.Approach(yLerp, 0f, 1f * Engine.DeltaTime);
                }
                float y = MathHelper.Lerp(startY, startY + 12f, Ease.SineInOut(yLerp));
                MoveToY(y);
            }
        }

        public override void Render() {
            if (Scene.Tracker.GetEntity<FragmentsStarJumpController>() != null) {
                VirtualRenderTarget blockFill = FragmentsStarJumpController.BlockFill;

                if (blockFill != null) {
                    Vector2 camera_pos = SceneAs<Level>().Camera.Position.Floor();
                    Rectangle sourceRectangle = new Rectangle(
                        (int)(X - camera_pos.X), (int)(Y - camera_pos.Y),
                        (int)Width, (int)Height);

                    Draw.SpriteBatch.Draw((RenderTarget2D)blockFill, Position, sourceRectangle, Color.White);
                }
            }

            base.Render();
        }

        internal static void Load() {
            On.Celeste.Player.RefillDash += (refillDashHook = (On.Celeste.Player.orig_RefillDash orig, Player self) => {
                if (self.Scene != null && (
                    self.CollideCheck<FragmentsStarJumpBlock>(self.Position + Vector2.UnitY) ||
                    self.CollideCheckOutside<FragmentsStarJumpthru>(self.Position + Vector2.UnitY)
                    ) && self.CollideAll<Solid>(self.Position + Vector2.UnitY)
                             .All((e) => (e is FragmentsStarJumpBlock || e is FragmentsStarJumpthru))) {
                    return false;
                }
                return orig(self);
            });
        }

        internal static void Unload() {
            On.Celeste.Player.RefillDash -= refillDashHook;
        }
    }
}