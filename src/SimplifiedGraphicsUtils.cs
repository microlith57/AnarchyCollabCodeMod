using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

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

/*
Some code in this file is modified from the CelesteTAS mod source code, which
is released under the MIT license.

See https://github.com/EverestAPI/CelesteTAS-EverestInterop/blob/master/LICENSE
for the full license.
*/

namespace Celeste.Mod.AnarchyCollab2022 {
    internal static class SimplifiedGraphicsUtils {
        internal static void DrawCircle(Vector2 center, float radius, Color color) {
            // Adapted from John Kennedy, "A Fast Bresenham Type Algorithm For Drawing Circles"
            // https://web.engr.oregonstate.edu/~sllu/bcircle.pdf
            // Not as fast though because we are forced to use floating point arithmetic anyway
            // since the center and radius aren't necessarily integral.
            // For similar reasons, we can't just assume the circle has 8-fold symmetry.
            // Modified so that instead of minimizing error, we include exactly those pixels which intersect the circle.

            CircleOctant(center, radius, color, +1, +1, false);
            CircleOctant(center, radius, color, +1, -1, false);
            CircleOctant(center, radius, color, -1, +1, false);
            CircleOctant(center, radius, color, -1, -1, false);
            CircleOctant(center, radius, color, +1, +1, true);
            CircleOctant(center, radius, color, +1, -1, true);
            CircleOctant(center, radius, color, -1, +1, true);
            CircleOctant(center, radius, color, -1, -1, true);
        }

        private static void CircleOctant(Vector2 center, float radius, Color color, float flipX, float flipY, bool interchangeXy) {
            // when flipX = flipY = 1 and interchangeXY = false, we are drawing the [0, pi/4] octant.

            float cx, cy;
            if (interchangeXy) {
                cx = center.Y;
                cy = center.X;
            } else {
                cx = center.X;
                cy = center.Y;
            }

            float x, y;
            if (flipX > 0) {
                x = (float)Math.Ceiling(cx + radius - 1);
            } else {
                x = (float)Math.Floor(cx - radius + 1);
            }

            if (flipY > 0) {
                y = (float)Math.Floor(cy);
            } else {
                y = (float)Math.Ceiling(cy);
            }

            float starty = y;
            float e = (x - cx) * (x - cx) + (y - cy) * (y - cy) - radius * radius;
            float yc = flipY * 2 * (y - cy) + 1;
            float xc = flipX * -2 * (x - cx) + 1;
            while (flipY * (y - cy) <= flipX * (x - cx)) {
                // Slower than using DrawLine, but more obviously correct:
                //DrawPoint((int)x + (flipX < 0 ? -1 : 0), (int)y + (flipY < 0 ? -1 : 0), interchangeXY, color);
                e += yc;
                y += flipY;
                yc += 2;
                if (e >= 0) {
                    // We would have a 1px correction for flipY here (as we do for flipX) except for
                    // the fact that our lines always include the top pixel and exclude the bottom one.
                    // Because of this we would have to make two corrections which cancel each other out,
                    // so we just don't do either of them.
                    DrawLine((int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy, color);
                    starty = y;
                    e += xc;
                    x -= flipX;
                    xc += 2;
                }
            }

            DrawLine((int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy, color);
        }

        private static void DrawLine(int x, int y0, int y1, bool interchangeXy, Color color) {
            // x, y0, and y1 must all be integers
            int length = (int)(y1 - y0);
            Rectangle rect;
            if (interchangeXy) {
                rect.X = (int)y0;
                rect.Y = (int)x;
                rect.Width = length;
                rect.Height = 1;
            } else {
                rect.X = (int)x;
                rect.Y = (int)y0;
                rect.Width = 1;
                rect.Height = length;
            }

            Draw.SpriteBatch.Draw(Draw.Pixel.Texture.Texture, rect, Draw.Pixel.ClipRect, color);
        }

        internal static void DrawCombineHollowRect(Grid grid, Color color, int x, int y, int left, int right, int top, int bottom) {
            float topLeftX = grid.AbsoluteLeft + x * grid.CellWidth;
            float topLeftY = grid.AbsoluteTop + y * grid.CellHeight;
            Vector2 width = Vector2.UnitX * grid.CellWidth;
            Vector2 height = Vector2.UnitY * grid.CellHeight;

            Vector2 topLeft = new Vector2(topLeftX, topLeftY);
            Vector2 topRight = topLeft + width;
            Vector2 bottomLeft = topLeft + height;
            Vector2 bottomRight = topRight + height;

            bool drawnLeft = false, drawnRight = false, drawnTop = false, drawnBottom = false;

            VirtualMap<bool> data = grid.Data;

            if (data[x, y]) {
                // left
                if (x != left && !data[x - 1, y]) {
                    Draw.Line(topLeft + Vector2.One, bottomLeft + Vector2.UnitX - Vector2.UnitY, color);
                    drawnLeft = true;
                }

                // right
                if (x == right || x + 1 <= right && !data[x + 1, y]) {
                    Draw.Line(topRight + Vector2.UnitY, bottomRight - Vector2.UnitY, color);
                    drawnRight = true;
                }

                // top
                if (y != top && !data[x, y - 1]) {
                    Draw.Line(topLeft + Vector2.UnitX, topRight - Vector2.UnitX, color);
                    drawnTop = true;
                }

                // bottom
                if (y == bottom || y + 1 <= bottom && !data[x, y + 1]) {
                    Draw.Line(bottomLeft - Vector2.UnitY + Vector2.UnitX, bottomRight - Vector2.One, color);
                    drawnBottom = true;
                }

                // top left point
                if (drawnTop || drawnLeft) {
                    Draw.Point(topLeft, color);
                }

                // top right point
                if (drawnTop || drawnRight) {
                    Draw.Point(topRight - Vector2.UnitX, color);
                }

                // bottom left point
                if (drawnBottom || drawnLeft) {
                    Draw.Point(bottomLeft - Vector2.UnitY, color);
                }

                // bottom right point
                if (drawnBottom || drawnRight) {
                    Draw.Point(bottomRight - Vector2.One, color);
                }
            } else {
                // inner hollow top left point
                if (x - 1 >= left && y - 1 >= top && data[x - 1, y - 1] && data[x - 1, y] && data[x, y - 1]) {
                    Draw.Point(topLeft - Vector2.One, color);
                }

                // inner hollow top right point
                if (x + 1 <= right && y - 1 >= top && data[x + 1, y - 1] && data[x + 1, y] && data[x, y - 1]) {
                    Draw.Point(topRight - Vector2.UnitY, color);
                }

                // inner hollow bottom left point
                if (x - 1 >= left && y + 1 <= bottom && data[x - 1, y + 1] && data[x - 1, y] && data[x, y + 1]) {
                    Draw.Point(bottomLeft - Vector2.UnitX, color);
                }

                // inner hollow bottom right point
                if (x + 1 <= right && y + 1 >= top && data[x + 1, y + 1] && data[x + 1, y] && data[x, y + 1]) {
                    Draw.Point(bottomRight, color);
                }
            }
        }
    }
}