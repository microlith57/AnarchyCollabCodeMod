namespace Celeste.Mod.AnarchyCollab2022 {
    public class AnarchyCollabModule : EverestModule {

        public static AnarchyCollabModule Instance { get; private set; }
        public static string Name => Instance?.Metadata?.Name;

        public AnarchyCollabModule() {
            Instance = this;
        }

        public override void Load() {
            Content.CrouchBlocker.Load();
            Content.DemoDashButtonBlocker.Load();
            JankTempleCustomEvent.Load();
            ElevatorCutsceneUtils.Load();
            FragmentsStarJumpBlock.Load();
        }

        public override void Unload() {
            Content.CrouchBlocker.Unload();
            Content.DemoDashButtonBlocker.Unload();
            JankTempleCustomEvent.Unload();
            ElevatorCutsceneUtils.Unload();
            FragmentsStarJumpBlock.Unload();
        }
    }
}
