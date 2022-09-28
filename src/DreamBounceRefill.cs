using System.Reflection;

using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.AnarchyCollab2022 {
    [CustomEntity("AnarchyCollab2022/DreamBounceRefill")]
    public class DreamBounceRefill : ShardRefill {
        private static readonly FieldInfo DREAMDASH_END_TIMER = typeof(Player).GetField("dreamDashCanEndTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        private static int bounceHookCounter = 0;
        private Sprite sprite;

        public DreamBounceRefill(EntityData data, Vector2 offset) : base(data.Position + offset, data.Bool("doubleRefill") ? Color.DeepSkyBlue : Color.DarkMagenta, Color.DarkMagenta, data.Bool("doubleRefill"), data.Float("respawnDelay", 2.5f)) {}

        public override void Added(Scene scene) {
            if (bounceHookCounter++ <= 0) { On.Celeste.Player.DreamDashedIntoSolid += BounceHook; }
            base.Added(scene);
            sprite = Components.Get<Sprite>();
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (--bounceHookCounter <= 0) { On.Celeste.Player.DreamDashedIntoSolid -= BounceHook; }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            sprite = Components.Get<Sprite>();
        }

        public override void Render() {
            if(sprite == null) return;
            if ((Scene as Level).Session.Inventory.DreamDash) {
                sprite.Color = Color.White;
                sprite.Scale = Vector2.One;
                sprite.RenderPosition = Position;
                Active = true;
                Depth = -50;
            } else {
                sprite.Color = new Color(20, 20, 20);
                sprite.Scale = Vector2.One * 0.65f;
                sprite.RenderPosition = Position.Floor();
                Active = false;
                Depth = 50;
            }
            base.Render();
        }

        protected override bool OnTouch(Player player) {
            if (!(Scene as Level).Session.Inventory.DreamDash) { return false; }
            return base.OnTouch(player);
        }

        private bool BounceHook(On.Celeste.Player.orig_DreamDashedIntoSolid orig, Player player) {
            if (!orig(player)) { return false; }

            // Try to consume a shard
            if (!ConsumeShard<DreamBounceRefill>(player)) { return true; }

            // Reflect the player's dream dash
            Vector2 oldPos = player.Position;
            player.Position -= player.Speed * (Engine.DeltaTime + 0.01f);
            DREAMDASH_END_TIMER.SetValue(player, float.Epsilon);

            DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();
            if ((oldPos.X < dreamBlock.Left || dreamBlock.Right < oldPos.X) && dreamBlock.Top <= oldPos.Y && oldPos.Y <= dreamBlock.Bottom) { player.Speed.X *= -1; }
            if ((oldPos.Y < dreamBlock.Top || dreamBlock.Bottom < oldPos.Y) && dreamBlock.Left <= oldPos.X && oldPos.X <= dreamBlock.Right) { player.Speed.Y *= -1; }

            return false;
        }
    }
}
