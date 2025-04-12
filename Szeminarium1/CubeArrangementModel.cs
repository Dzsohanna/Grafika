using Silk.NET.OpenGL;
using System.Drawing;

namespace GrafikaSzeminarium
{
    public enum Direction
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    internal static class RubiksCubeFactory
    {
        public static unsafe ModelObjectDescriptor CreateSmallCube(GL Gl, Dictionary<Direction, Color> faceColors)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            var vertexArray = new float[] {
                -0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,

                -0.5f, 0.5f, 0.5f,
                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,

                -0.5f, 0.5f, 0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, 0.5f,

                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, 0.5f,
            };

            float[] colorArray = new float[24 * 4];

            ApplyFaceColor(colorArray, 0, faceColors[Direction.Top]);
            ApplyFaceColor(colorArray, 4, faceColors[Direction.Front]);
            ApplyFaceColor(colorArray, 8, faceColors[Direction.Left]);
            ApplyFaceColor(colorArray, 12, faceColors[Direction.Bottom]);
            ApplyFaceColor(colorArray, 16, faceColors[Direction.Back]);
            ApplyFaceColor(colorArray, 20, faceColors[Direction.Right]);

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            Gl.BindVertexArray(0);

            return new ModelObjectDescriptor()
            {
                Vao = vao,
                Vertices = vertices,
                Colors = colors,
                Indices = indices,
                IndexArrayLength = (uint)indexArray.Length,
                Gl = Gl
            };
        }

        private static void ApplyFaceColor(float[] colorArray, int startVertex, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = (startVertex + i) * 4;
                colorArray[idx] = color.R / 255.0f;
                colorArray[idx + 1] = color.G / 255.0f;
                colorArray[idx + 2] = color.B / 255.0f;
                colorArray[idx + 3] = 1.0f;
            }
        }
    }
}