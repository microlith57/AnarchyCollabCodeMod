using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using LunaticHelper;

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
    static class ElevatorCutsceneUtils {
        private static On.Celeste.Backdrop.hook_Update backdrop_update_hook;

        public static void SetWind(Level level, bool down) {
            WindController.Patterns pattern = down ? WindController.Patterns.Down : WindController.Patterns.None;
            WindController windController = level.Entities.FindFirst<WindController>();

            if (windController == null) {
                level.Add(new WindController(pattern));
            } else {
                windController.SetPattern(pattern);
            }
        }

        public static void FadeOutDust(Level level) {
            foreach (var dust in level.Background.GetEach<CustomDust>()) {
                DynData<CustomDust> dyndata = new DynData<CustomDust>(dust);
                dyndata.Set<bool>("should_patch_update", true);
            }
            foreach (var dust in level.Foreground.GetEach<CustomDust>()) {
                DynData<CustomDust> dyndata = new DynData<CustomDust>(dust);
                dyndata.Set<bool>("should_patch_update", true);
            }
        }

        internal static void Load() {
            On.Celeste.Backdrop.Update += backdrop_update_hook = (On.Celeste.Backdrop.orig_Update orig, Backdrop backdrop, Scene scene) => {
                if (backdrop is CustomDust) {
                    CustomDust dust = backdrop as CustomDust;
                    DynData<CustomDust> dust_data = new DynData<CustomDust>(dust);

                    object flag = dust_data["should_patch_update"];
                    if (flag is bool && (bool)flag) {
                        Array particles = dust_data.Get<Array>("particles");
                        for (int i = 0; i < particles.Length; i++) {
                            object particle = particles.GetValue(i);
                            DynamicData particle_data = new DynamicData(particle);
                            if (particle_data.Get<float>("Percent") >= 1f) {
                                particle_data.Set("Percent", 0f);
                                particle_data.Set("Duration", float.PositiveInfinity);
                                particle_data.Set("Speed", 0f);
                                particle_data.Set("Color", Color.Transparent);
                            }
                            particles.SetValue(particle, i);
                        }
                    }
                }
                orig(backdrop, scene);
            };
        }

        internal static void Unload() {
            On.Celeste.Backdrop.Update -= backdrop_update_hook;
        }
    }
}
