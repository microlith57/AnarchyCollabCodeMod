using System;
using System.Linq;
using System.Collections;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.AnarchyCollab2022.Content {
    [CustomEntity("AnarchyCollab2022/PlayerDialogNPC")]
    public class PlayerDialogNPC : NPC {
        private EntityID id;
        private bool flipX, flipY;
        private string idleAnimation;
        private int approachStartDistance;
        private bool loopDialog;
        private string[] dialogIds;
        private string[] dialogAnimations;

        private PlayerHair hair;
        private float windHairTimer = 0;
        private Coroutine talkRoutine = null;
        private string dialogAnimation = null;

        public PlayerDialogNPC(EntityData data, Vector2 off, EntityID id) : base(data.Position + off) {
            this.id = id;

            // Create sprite & hair
            Sprite = new PlayerSprite((PlayerSpriteMode)data.Int("spriteMode", (int)PlayerSpriteMode.Madeline));
            Add(hair = new PlayerHair((PlayerSprite)Sprite));
            Add(Sprite);
            hair.Color = data.HexColor("hairColor", Player.NormalHairColor);
            hair.Start();

            // Parse data
            idleAnimation = data.Attr("idleAnimation", "idle");
            Sprite.Scale.X = (flipX = data.Bool("flipX")) ? -1 : 1;
            Sprite.Scale.Y = (flipY = data.Bool("flipY")) ? -1 : 1;
            approachStartDistance = data.Int("approachStartDistance");
            loopDialog = data.Bool("loopDialog");

            // Get dialog IDs and animations
            dialogIds = data.Attr("dialogIds").Length > 0 ? data.Attr("dialogIds").Split(new char[] { ';' }) : null;
            dialogAnimations = data.Attr("dialogAnimations").Length > 0 ? data.Attr("dialogAnimations").Split(new char[] { ';' }) : null;
            if (dialogAnimations != null && dialogIds != null && dialogAnimations.Length == 1) {
                dialogAnimations = Enumerable.Repeat(dialogAnimations[0], dialogIds.Length).ToArray();
            }

            // Validate animations
            if (!Sprite.Has(idleAnimation)) {
                Logger.Log(AnarchyCollabModule.Name, $"Invalid idle animation for Player NPC '{idleAnimation}'");
                idleAnimation = null;
            }

            for (int i = 0; i < dialogAnimations.Length; i++) {
                if (!Sprite.Has(dialogAnimations[i])) {
                    Logger.Log(AnarchyCollabModule.Name, $"Invalid dialog animation {i} for Player NPC '{dialogAnimations[i]}'");
                    dialogAnimations[i] = null;
                }
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            if (dialogIds != null && Level.Session.GetCounter($"{id}TalkCounter") < dialogIds.Length) {
                // Create talker
                Add(Talker = new TalkComponent(new Rectangle(-Sprite.Texture.Width / 2 - 48, -Sprite.Texture.Height - 16, Sprite.Texture.Width + 96, Sprite.Texture.Height + 32), new Vector2(-0.5f, -Sprite.Texture.Height / 2), OnTalkStart));
            }
        }

        public override void Update() {
            base.Update();

            // If talking will start when approached, handle that
            Player player = Level.Tracker.GetEntity<Player>();
            if (player != null && Talker != null && !Level.Session.GetFlag($"{id}Approached") && Math.Abs(player.X - X) <= approachStartDistance && Math.Abs(player.Y - Y) <= 64) {
                Level.Session.SetFlag($"{id}Approached");
                OnTalkStart(player);
            }

            // Update hair
            hair.Facing = (Sprite.Scale.X < 0) ? Facings.Left : Facings.Right;
            if (player != null) {
                DynamicData dynPlayer = DynamicData.For(player);

                Vector2 windDir = dynPlayer.Get<Vector2>("windDirection");
                if (player.ForceStrongWindHair.Length() > 0) { windDir = player.ForceStrongWindHair; }
                if (dynPlayer.Get<float>("windTimeout") > 0 && windDir.X != 0) {
                    windHairTimer += Engine.DeltaTime * 8;
                    hair.StepPerSegment = new Vector2(windDir.X * 5f, (float)Math.Sin(windHairTimer));
                    hair.StepInFacingPerSegment = 0f;
                    hair.StepApproach = 128f;
                    hair.StepYSinePerSegment = 0f;
                } else {
                    hair.StepPerSegment = new Vector2(0f, 2f + windDir.Y * 0.5f);
                    hair.StepInFacingPerSegment = 0.5f;
                    hair.StepApproach = 64f;
                    hair.StepYSinePerSegment = 0f;
                }
            }

            // Play animations
            if ((dialogAnimation ?? idleAnimation) != null) {
                Sprite.Play(dialogAnimation ?? idleAnimation, false);
            }
        }

        private void OnTalkStart(Player player) {
            // Put player into dummy state
            player.StateMachine.State = Player.StDummy;
            player.StateMachine.Locked = true;

            // Start talk cutscene
            Level.StartCutscene(OnTalkEnd);
            Add(talkRoutine = new Coroutine(Talk(player, Level.Session.GetCounter($"{id}TalkCounter"))));
        }

        private void OnTalkEnd(Level level) {
            // Stop coroutine
            talkRoutine.Cancel();
            talkRoutine.RemoveSelf();

            // Increment talk counter
            Level.Session.IncrementCounter($"{id}TalkCounter");
            if (Level.Session.GetCounter($"{id}TalkCounter") >= dialogIds.Length) {
                // Reset talk counter or remove abillity to talk
                if (loopDialog) {
                    Level.Session.SetCounter($"{id}TalkCounter", 0);
                } else {
                    Talker.RemoveSelf();
                    Talker = null;
                }
            }

            // Reset sprite scale
            Sprite.Scale.X = flipX ? -1 : 1;
            Sprite.Scale.Y = flipY ? -1 : 1;

            // Revert player state
            Player player = level.Tracker.GetEntity<Player>();
            player.StateMachine.Locked = false;
            player.StateMachine.State = Player.StNormal;

            // Play idle animation
            dialogAnimation = null;
        }

        private IEnumerator Talk(Player player, int dialogNum) {
            // Make player approach NPC
            yield return PlayerApproach(player, true, 12);

            // Play dialog animation
            dialogAnimation = dialogAnimations[dialogNum];

            // Start dialog
            yield return Textbox.Say(dialogIds[dialogNum]);

            // End cutscene
            Level.EndCutscene();
            OnTalkEnd(Level);
        }
    }
}
