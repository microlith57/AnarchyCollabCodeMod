using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.AnarchyCollab2022 {
    public static class JankTempleCustomEvent {
        internal static void Load() {
            Everest.Events.EventTrigger.OnEventTrigger += JankTempleTilesetEvent;
        }

        internal static void Unload() {
            Everest.Events.EventTrigger.OnEventTrigger -= JankTempleTilesetEvent;
        }

        private static bool JankTempleTilesetEvent(EventTrigger trigger, Player player, string eventID) {
            if (eventID == "ac2022_jank_tileset") {
                Level level = player.SceneAs<Level>();
                if (!level.Session.GetFlag("ac2022_jank_tileset")) {
                    trigger.Add(new Coroutine(TilesetGlitchRoutine(level)));
                    level.Session.SetFlag("ac2022_jank_tileset");
                }
                return true;
            }
            return false;
        }

        private static IEnumerator TilesetGlitchRoutine(Level level) {
            Audio.Play(SFX.game_10_glitch_medium);
            Glitch.Value = 0.3f;
            yield return 0.2f;
            ReplaceTiles(level, 'T', 'J');
            yield return 0.2f;
            Glitch.Value = 0f;
        }

        private static void ReplaceTiles(Level level, char origTile, char newTile) {
            VirtualMap<char> solidsData = level.SolidsData.Clone();
            for (int x = 0; x < solidsData.Columns; x++) {
                for (int y = 0; y < solidsData.Rows; y++) {
                    if (solidsData[x, y] == origTile) {
                        solidsData[x, y] = newTile;
                    }
                }
            }
            level.Remove(level.SolidTiles);
            level.Add(level.SolidTiles = new SolidTiles(level.SolidTiles.Position, solidsData));
            level.SolidsData = solidsData;
        }
    }
}
