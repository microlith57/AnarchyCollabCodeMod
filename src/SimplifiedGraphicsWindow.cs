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
    [CustomEntity("AnarchyCollab2022/SimplifiedGraphicsWindow")]
    public class SimplifiedGraphicsWindow : Entity {
        public new float Width;
        public new float Height;

        public SimplifiedGraphicsWindow(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Width = data.Width;
            Height = data.Height;

            // big negative number. chosen by a complex, highly involved process
            Depth = -57830273;
        }

        public override void Render() {
            base.Render();

            if ((Scene is Level level) && (SimplifiedGraphicsController.Current?.RenderVirtually ?? false) && SimplifiedGraphicsController.Target != null && !SimplifiedGraphicsController.Target.IsDisposed) {
                Vector2 camera_pos = level.Camera.Position.Floor();
                Vector2 source_pos = Position - camera_pos;
                Rectangle sourceRectangle = new Rectangle(
                    (int)source_pos.X, (int)source_pos.Y,
                    (int)Width, (int)Height);

                Draw.SpriteBatch.Draw((RenderTarget2D)SimplifiedGraphicsController.Target, Position, sourceRectangle, Color.White);
            }
        }
    }
}