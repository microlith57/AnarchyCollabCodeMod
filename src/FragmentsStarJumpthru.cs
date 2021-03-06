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
    [CustomEntity("AnarchyCollab2022/FragmentsStarJumpthru")]
    public class FragmentsStarJumpthru : JumpthruPlatform {
        public FragmentsStarJumpthru(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, "dream", -1) {
        }

        public override void Render() {
            if (Scene.Tracker.GetEntity<FragmentsStarJumpController>() != null && FragmentsStarJumpController.BlockFill != null) {
                var blockFill = (RenderTarget2D)FragmentsStarJumpController.BlockFill;

                Vector2 camera_pos = SceneAs<Level>().Camera.Position.Floor();

                // Main Jumpthru Part
                DrawFill(blockFill, camera_pos, new Rectangle(0, 0, (int)Width, 3));

                // Left Support
                DrawFill(blockFill, camera_pos, new Rectangle(0, 3, 5, 2));
                DrawFill(blockFill, camera_pos, new Rectangle(0, 3, 2, 5));

                // Right Support
                DrawFill(blockFill, camera_pos, new Rectangle((int)Width - 5, 3, 5, 2));
                DrawFill(blockFill, camera_pos, new Rectangle((int)Width - 2, 3, 2, 5));
            }

            base.Render();
        }

        private void DrawFill(RenderTarget2D fill, Vector2 camera_pos, Rectangle rect) {
            Vector2 world_pos = new Vector2(X + (float)rect.Location.X,
                                            Y + (float)rect.Location.Y);
            Point screen_pos = new Point((int)X - (int)camera_pos.X + rect.Location.X,
                                         (int)Y - (int)camera_pos.Y + rect.Location.Y);
            rect.Location = screen_pos;
            Draw.SpriteBatch.Draw(fill, world_pos, rect, Color.White);
        }
    }
}