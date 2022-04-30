using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.AnarchyCollab2022 {
    [CustomEntity("AnarchyCollab2022/NoOutlineSwapBlock")]
    public class NoOutlineSwapBlock : SwapBlock {
        private DynamicData baseData;

        public NoOutlineSwapBlock(EntityData data, Vector2 offset)
            : base(data, offset) {
            baseData = new DynamicData(typeof(SwapBlock), this);
            float baseMaxForwardSpeed = baseData.Get<float>("maxForwardSpeed");
            float speed = data.Float("speed", 360f);
            baseData.Set("maxForwardSpeed", baseMaxForwardSpeed * (speed / 360f));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Entity path = baseData.Get<Entity>("path");
            path.Active = path.Visible = false;
        }
    }
}
