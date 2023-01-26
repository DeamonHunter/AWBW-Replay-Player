// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class MultiColourPath
    {
        private class MultiColourPathDrawNode : DrawNode
        {
            public const int MAX_RES = 24;

            protected new MultiColourPath Source => (MultiColourPath)base.Source;

            private readonly List<(Line, SRGBColour, SRGBColour)> segments = new List<(Line, SRGBColour, SRGBColour)>();

            private Texture texture;
            private Vector2 drawSize;
            private float radius;
            private IShader pathShader;

            private IVertexBatch<TexturedVertex3D> halfCircleBatch;
            private IVertexBatch<TexturedVertex3D> quadBatch;

            public MultiColourPathDrawNode(MultiColourPath source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                segments.Clear();
                segments.AddRange(Source.segments);

                texture = Source.Texture;
                drawSize = Source.DrawSize;
                radius = Source.PathRadius;
                pathShader = Source.pathShader;
            }

            private Vector2 pointOnCircle(float angle) => new Vector2(MathF.Sin(angle), -MathF.Cos(angle));

            private void addLineCap(Vector2 origin, float theta, float thetaDiff, RectangleF texRect, Color4 colour)
            {
                const float step = MathF.PI / MAX_RES;

                float dir = Math.Sign(thetaDiff);
                thetaDiff = dir * thetaDiff;

                int amountPoints = (int)Math.Ceiling(thetaDiff / step);

                if (dir < 0)
                    theta += MathF.PI;

                Vector2 current = origin + pointOnCircle(theta) * radius;

                for (int i = 1; i <= amountPoints; i++)
                {
                    // Center point
                    halfCircleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(origin.X, origin.Y, 1),
                        TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                        Colour = colour
                    });

                    // First outer point
                    halfCircleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(current.X, current.Y, 0),
                        TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                        Colour = colour
                    });

                    float angularOffset = Math.Min(i * step, thetaDiff);
                    current = origin + pointOnCircle(theta + dir * angularOffset) * radius;

                    // Second outer point
                    halfCircleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(current.X, current.Y, 0),
                        TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                        Colour = colour
                    });
                }
            }

            private void addLineQuads(Line line, RectangleF texRect, SRGBColour startColour, SRGBColour endColour)
            {
                Vector2 ortho = line.OrthogonalDirection;
                Line lineLeft = new Line(line.StartPoint + ortho * radius, line.EndPoint + ortho * radius);
                Line lineRight = new Line(line.StartPoint - ortho * radius, line.EndPoint - ortho * radius);

                quadBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineRight.EndPoint.X, lineRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = endColour
                });
                quadBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineRight.StartPoint.X, lineRight.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = startColour
                });

                // Each "quad" of the slider is actually rendered as 2 quads, being split in half along the approximating line.
                // On this line the depth is 1 instead of 0, which is done properly handle self-overlap using the depth buffer.
                // Thus the middle vertices need to be added twice (once for each quad).
                Vector3 firstMiddlePoint = new Vector3(line.StartPoint.X, line.StartPoint.Y, 1);
                Vector3 secondMiddlePoint = new Vector3(line.EndPoint.X, line.EndPoint.Y, 1);
                Color4 firstMiddleColour = startColour;
                Color4 secondMiddleColour = endColour;

                for (int i = 0; i < 2; ++i)
                {
                    quadBatch.Add(new TexturedVertex3D
                    {
                        Position = firstMiddlePoint,
                        TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                        Colour = firstMiddleColour
                    });
                    quadBatch.Add(new TexturedVertex3D
                    {
                        Position = secondMiddlePoint,
                        TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                        Colour = secondMiddleColour
                    });
                }

                quadBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineLeft.EndPoint.X, lineLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = endColour
                });
                quadBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineLeft.StartPoint.X, lineLeft.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = startColour
                });
            }

            private void updateVertexBuffer()
            {
                (Line line, SRGBColour capColour, _) = segments[0];
                float theta = line.Theta;

                // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
                RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));
                addLineCap(line.StartPoint, theta + MathF.PI, MathF.PI, texRect, capColour);

                for (int i = 1; i < segments.Count; ++i)
                {
                    (Line nextLine, SRGBColour cap, SRGBColour nextCap) = segments[i];
                    float nextTheta = nextLine.Theta;
                    addLineCap(line.EndPoint, theta, nextTheta - theta, texRect, cap);

                    line = nextLine;
                    theta = nextTheta;
                    capColour = nextCap;
                }

                addLineCap(line.EndPoint, theta, MathF.PI, texRect, capColour);

                foreach ((Line segment, SRGBColour startColour, SRGBColour endColour) in segments)
                    addLineQuads(segment, texRect, startColour, endColour);
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (texture?.Available != true || segments.Count == 0)
                    return;

                // We multiply the size param by 3 such that the amount of vertices is a multiple of the amount of vertices
                // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
                // grouping of vertices into primitives.
                halfCircleBatch ??= renderer.CreateLinearBatch<TexturedVertex3D>(MAX_RES * 100 * 3, 10, PrimitiveTopology.Triangles);
                quadBatch ??= renderer.CreateQuadBatch<TexturedVertex3D>(200, 10);

                renderer.PushLocalMatrix(DrawInfo.Matrix);
                renderer.PushDepthInfo(DepthInfo.Default);

                // Blending is removed to allow for correct blending between the wedges of the path.
                renderer.SetBlend(BlendingParameters.None);

                pathShader.Bind();

                texture.Bind();

                updateVertexBuffer();

                pathShader.Unbind();

                renderer.PopDepthInfo();
                renderer.PopLocalMatrix();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                halfCircleBatch?.Dispose();
                quadBatch?.Dispose();
            }
        }
    }
}
