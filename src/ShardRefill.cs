using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Procedurline;

namespace Celeste.Mod.AnarchyCollab2022 {
    public abstract class ShardRefill : CustomRefill {
        public sealed class Shard : Entity {
            public const float SHARD_SCALE = 0.8f;

            private Follower follower;
            private Sprite sprite;

            internal Shard(ShardRefill refill) {
                Refill = refill;
                Collider = new Hitbox(refill.Collider.Size.X * SHARD_SCALE, refill.Collider.Size.Y * SHARD_SCALE, refill.Collider.Position.X * SHARD_SCALE, refill.Collider.Position.Y * SHARD_SCALE);
                Active = false;
                Depth = -1000000;

                //Create sprite
                Add(sprite = Refill.Sprite.Clone());
                sprite.Play("idle", randomizeFrame: true);
                sprite.CenterOrigin();
                sprite.Scale = new Vector2(SHARD_SCALE, SHARD_SCALE);
                sprite.Visible = false;

                //Add other components
                Add(follower = new Follower());
                Add(new MirrorReflection());
                Add(new BloomPoint(0.8f, 16f * SHARD_SCALE * 0.65f));
                Add(new VertexLight(Color.White, 1f, (int)(16 * SHARD_SCALE), (int)(48 * SHARD_SCALE)));
            }

            public override void Render() {
                sprite.RenderPosition = Position.Floor();
                base.Render();
            }

            internal void AttachToPlayer(Player player) {
                if (Player == null) {
                    //Follow player
                    Player = player;
                    Position = Refill.Position;
                    player.Leader.GainFollower(follower);
                    Active = true;
                    sprite.Visible = true;
                }
            }

            internal void Consume() {
                if (Player != null) {
                    //Feedback
                    Audio.Play("event:/game/general/diamond_touch", Position).setVolume(SHARD_SCALE * SHARD_SCALE);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    (Scene as Level)?.ParticlesFG?.Emit(Refill.ShatterParticleType, 3, Position, Vector2.One * 4f * SHARD_SCALE, (float)Math.PI / 2f);
                    (Scene as Level)?.ParticlesFG?.Emit(Refill.ShatterParticleType, 3, Position, Vector2.One * 4f * SHARD_SCALE, (float)Math.PI / 2f);

                    //Don't follow player anymore
                    Player.Leader.LoseFollower(follower);
                    Player = null;
                    Active = false;
                    sprite.Visible = false;
                    if (Refill.Broken) { Refill.Respawn(); }
                }
            }

            internal void Replace(Shard other) {
                //Swap shard properties
                Player = other.Player;
                Position = other.Position;
                other.Player = null;

                Active = true;
                sprite.Visible = true;
                other.Active = false;
                other.sprite.Visible = false;

                //Swap followers
                Player.Leader.Followers[other.follower.FollowIndex] = follower;
                follower.OnGainLeaderUtil(other.follower.Leader);
                other.follower.OnLoseLeaderUtil();
                if (other.Refill.Broken) { other.Refill.Respawn(); }
            }

            public ShardRefill Refill { get; }
            public Player Player { get; private set; }
        }

        public static IEnumerable<Shard> GetShards<T>(Player player) where T : ShardRefill => GetShards(player, typeof(T));
        public static IEnumerable<Shard> GetShards(Player player, Type refillType) => player.Leader.Followers.Where(f => f.Entity is Shard s && refillType.IsAssignableFrom(s.Refill.GetType())).Select(f => f.Entity as Shard);

        public static bool ConsumeShard<T>(Player player) where T : ShardRefill => ConsumeShard(player, typeof(T));
        public static bool ConsumeShard(Player player, Type refillType) {
            Shard s = GetShards(player, refillType).FirstOrDefault();
            s?.Consume();
            return s != null;
        }

        public readonly Color ShardColor;
        public readonly float RespawnDelay;
        private Shard[] shards;

        protected ShardRefill(Vector2 position, Color color, Color shardColor, bool doubleRefill, float respawnDelay = 2.5f) : base(position, color, doubleRefill, false) {
            ShardColor = shardColor;
            RespawnDelay = respawnDelay;
        }

        public override void Added(Scene scene) {
            scene.Add(shards = Enumerable.Range(0, ShardRefillLimit).Select(_ => new Shard(this)).ToArray());
            base.Added(scene);
        }

        protected override bool OnTouch(Player player) {
            if (GetShards(player, GetType()).Count() >= ShardRefillLimit) { return false; }
            foreach (Shard shard in shards) {
                Shard exShard = GetShards(player, GetType()).Where(s => s.Refill != this).FirstOrDefault();
                if (exShard != null) {
                    shard.Replace(exShard);
                } else {
                    shard.AttachToPlayer(player);
                }
            }
            Break(player, RespawnDelay);
            return false;
        }

        public virtual int ShardRefillLimit => DoubleRefill ? 2 : 1;
    }
}
