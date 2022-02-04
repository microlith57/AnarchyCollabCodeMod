using System;
using System.Reflection;
using System.Collections;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using Monocle;

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
    [CustomEntity("AnarchyCollab2022/FragmentsNPCTheoVent")]
    public class FragmentsNPCTheoVent : NPC {
        private const string AppearedFlag = "microlith57FragmentsTheoVentsAppeared";
        private const string TalkedFlag = "microlith57FragmentsTheoVentsTalked";
        private const int SpriteAppearY = -8;

        private float particleDelay;
        private bool appeared;
        private Entity grate;

        private Type grateType;
        private ConstructorInfo grateConstructorInfo;
        private MethodInfo grateShakeInfo;
        private MethodInfo grateFallInfo;

        public FragmentsNPCTheoVent(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset) {
            grateType = typeof(NPC03_Theo_Vents).GetNestedType("Grate", BindingFlags.NonPublic);
            grateConstructorInfo = grateType.GetConstructor(new Type[] { typeof(Vector2) });
            grateShakeInfo = grateType.GetMethod("Shake");
            grateFallInfo = grateType.GetMethod("Fall");

            base.Tag = Tags.TransitionUpdate;
            Add(Sprite = GFX.SpriteBank.Create("theo"));
            Sprite.Scale.Y = -1f;
            Sprite.Scale.X = -1f;
            Visible = false;
            Maxspeed = 48f;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (base.Session.GetFlag(TalkedFlag)) {
                RemoveSelf();
            } else {
                Add(new Coroutine(AppearRoutine()));
            }
        }

        public override void Update() {
            base.Update();
            if (appeared) {
                return;
            }
            particleDelay -= Engine.DeltaTime;
            if (particleDelay <= 0f) {
                Level.ParticlesFG.Emit(ParticleTypes.VentDust, 8, Position, new Vector2(6f, 0f));
                if (grate != null) {
                    grateShakeInfo.Invoke(grate, new object[] { });
                }
                particleDelay = Calc.Random.Choose(1f, 2f, 3f);
            }
        }

        private IEnumerator AppearRoutine() {
            if (!Session.GetFlag(AppearedFlag)) {
                Scene.Add(grate = (Entity)grateConstructorInfo.Invoke(new object[] { Position }));

                Player player;
                do {
                    yield return null;
                }
                while ((player = Scene.Tracker.GetEntity<Player>()) == null || !(player.X > X - 32f));

                Audio.Play("event:/char/theo/resort_ceilingvent_hey", Position);
                Level.ParticlesFG.Emit(ParticleTypes.VentDust, 24, Position, new Vector2(6f, 0f));
                grateFallInfo.Invoke(grate, new object[] { });

                float from = -24f;
                for (float p = 0f; p < 1f; p += Engine.DeltaTime * 2f) {
                    yield return null;
                    Visible = true;
                    Sprite.Y = from - (from + 8f) * Ease.CubeOut(p);
                }

                Session.SetFlag(AppearedFlag);
            }

            appeared = true;
            Sprite.Y = -8f;
            Visible = true;
            Add(Talker = new TalkComponent(new Rectangle(-16, 0, 32, 100), new Vector2(0f, -8f), OnTalk));
        }

        private void OnTalk(Player player) {
            Level.StartCutscene(OnTalkEnd);
            Add(new Coroutine(TalkRoutine(player)));
        }

        private IEnumerator TalkRoutine(Player player) {
            yield return PlayerApproach(player, turnToFace: true, 10f, -1);
            player.DummyAutoAnimate = false;
            player.Sprite.Play("lookUp");
            Vector2 zoomPosition = Vector2.Lerp(player.Position, Position, 0.5f) - 16f * Vector2.UnitY;
            yield return Level.ZoomTo(Level.Camera.CameraToScreen(zoomPosition), 2f, 0.5f);
            yield return Textbox.Say("MICROLITH57_FRAGMENTS_THEO_VENTS");
            yield return DisappearRoutine();
            yield return 0.25f;
            yield return Level.ZoomBack(0.5f);
            Level.EndCutscene();
            OnTalkEnd(Level);
        }

        private void OnTalkEnd(Level level) {
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (entity != null) {
                entity.DummyAutoAnimate = true;
                entity.StateMachine.Locked = false;
                entity.StateMachine.State = 0;
            }
            base.Session.SetFlag(TalkedFlag);
            RemoveSelf();
        }

        private IEnumerator DisappearRoutine() {
            Audio.Play("event:/char/theo/resort_ceilingvent_seeya", Position);
            int to = -24;
            float from = Sprite.Y;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 2f) {
                yield return null;
                Level.ParticlesFG.Emit(ParticleTypes.VentDust, 1, Position, new Vector2(6f, 0f));
                Sprite.Y = from + ((float)to - from) * Ease.BackIn(p);
            }
        }
    }
}