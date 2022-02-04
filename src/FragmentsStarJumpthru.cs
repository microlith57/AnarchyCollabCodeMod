using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste;
using Celeste.Mod.Entities;

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
                // Draw.SpriteBatch.Draw((RenderTarget2D)blockFill, Position + new Vector2(0f, 3f), new Rectangle(
                //     (int)(X - camera_pos.X),
                //     (int)(Y - camera_pos.Y) + 3,
                //     5, 2), Color.White);
                // Draw.SpriteBatch.Draw((RenderTarget2D)blockFill, Position + new Vector2(0f, 3f), new Rectangle(
                //     (int)(X - camera_pos.X),
                //     (int)(Y - camera_pos.Y) + 3,
                //     2, 5), Color.White);

                // Right Support
                DrawFill(blockFill, camera_pos, new Rectangle((int)Width - 5, 3, 5, 2));
                DrawFill(blockFill, camera_pos, new Rectangle((int)Width - 2, 3, 2, 5));
                // Draw.SpriteBatch.Draw((RenderTarget2D)blockFill, Position + new Vector2(Width - 5f, 3f), new Rectangle(
                //     (int)(X - camera_pos.X + Width) - 5,
                //     (int)(Y - camera_pos.Y) + 3,
                //     5, 2), Color.White);
                // Draw.SpriteBatch.Draw((RenderTarget2D)blockFill, Position + new Vector2(Width - 2f, 3f), new Rectangle(
                //     (int)(X - camera_pos.X + Width) - 2,
                //     (int)(Y - camera_pos.Y) + 3,
                //     2, 5), Color.White);
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