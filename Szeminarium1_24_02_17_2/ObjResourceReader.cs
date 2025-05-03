using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<float[]> objNormals;
            List<int[]> objFaces;
            List<int[]> objNormalIndices;

            ReadObjDataForTeapot(out objVertices, out objNormals, out objFaces, out objNormalIndices);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objNormals, objFaces, objNormalIndices, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        public static unsafe GlObject CreateModelFromObjFile(GL Gl, string filePath, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<float[]> objNormals;
            List<int[]> objFaces;
            List<int[]> objNormalIndices;

            ReadObjDataFromFile(filePath, out objVertices, out objNormals, out objFaces, out objNormalIndices);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objNormals, objFaces, objNormalIndices, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<float[]> objNormals, List<int[]> objFaces, List<int[]> objNormalIndices, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            // Ellenorizem hogy van-e normal vektor
            bool hasNormals = objNormals.Count > 0;

            for (int faceIndex = 0; faceIndex < objFaces.Count; faceIndex++)
            {
                var objFace = objFaces[faceIndex];
                Vector3D<float> calculatedNormal = new Vector3D<float>(0, 0, 0);

                // Kiszamolom a lap normalvektorat
                if (objFace.Length >= 3)
                {
                    var aObjVertex = objVertices[objFace[0] - 1];
                    var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                    var bObjVertex = objVertices[objFace[1] - 1];
                    var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                    var cObjVertex = objVertices[objFace[2] - 1];
                    var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                    calculatedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                // Process all vertices of this face
                for (int i = 0; i < objFace.Length; ++i)
                {
                    var objVertex = objVertices[objFace[i] - 1];
                    Vector3D<float> vertexNormal;

                    // Normalvektor index ellenorzese
                    bool hasNormalIndex = faceIndex < objNormalIndices.Count &&
                                          i < objNormalIndices[faceIndex].Length &&
                                          objNormalIndices[faceIndex][i] > 0;

                    if (hasNormals && hasNormalIndex)
                    {
                        int normalIndex = objNormalIndices[faceIndex][i] - 1;
                        if (normalIndex >= 0 && normalIndex < objNormals.Count)
                        {
                            var objNormal = objNormals[normalIndex];
                            vertexNormal = new Vector3D<float>(objNormal[0], objNormal[1], objNormal[2]);
                            vertexNormal = Vector3D.Normalize(vertexNormal);
                        }
                        else
                        {
                            // ha hibas az index a normalvektort hasznalom
                            vertexNormal = calculatedNormal;
                        }
                    }
                    else
                    { 
                        vertexNormal = calculatedNormal;
                    }

                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    glVertex.Add(vertexNormal.X);
                    glVertex.Add(vertexNormal.Y);
                    glVertex.Add(vertexNormal.Z);
                    // add texture, color

                    // check if vertex exists
                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
            }
        }

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<float[]> objNormals, out List<int[]> objFaces, out List<int[]> objNormalIndices)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objFaces = new List<int[]>();
            objNormalIndices = new List<int[]>();

            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.cube.obj"))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                ReadObjData(objReader, objVertices, objNormals, objFaces, objNormalIndices);
            }
        }

        private static unsafe void ReadObjDataFromFile(string filePath, out List<float[]> objVertices, out List<float[]> objNormals, out List<int[]> objFaces, out List<int[]> objNormalIndices)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objFaces = new List<int[]>();
            objNormalIndices = new List<int[]>();

            using (StreamReader objReader = new StreamReader(filePath))
            {
                ReadObjData(objReader, objVertices, objNormals, objFaces, objNormalIndices);
            }
        }

        private static void ReadObjData(StreamReader objReader, List<float[]> objVertices, List<float[]> objNormals, List<int[]> objFaces, List<int[]> objNormalIndices)
        {
            while (!objReader.EndOfStream)
            {
                var line = objReader.ReadLine();

                if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                    continue;

                var splitIndex = line.IndexOf(' ');
                if (splitIndex == -1)
                    continue;

                var lineClassifier = line.Substring(0, splitIndex);
                var lineData = line.Substring(splitIndex).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                switch (lineClassifier)
                {
                    case "v":
                        float[] vertex = new float[3];
                        for (int i = 0; i < Math.Min(lineData.Length, vertex.Length); ++i)
                            vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                        objVertices.Add(vertex);
                        break;
                    case "vn":
                        float[] normal = new float[3];
                        for (int i = 0; i < Math.Min(lineData.Length, normal.Length); ++i)
                            normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                        objNormals.Add(normal);
                        break;
                    case "f":
                        int[] face = new int[lineData.Length];
                        int[] normalIndices = new int[lineData.Length];
                        bool hasNormalIndices = false;

                        for (int i = 0; i < lineData.Length; ++i)
                        {
                            string[] parts = lineData[i].Split('/');
                            face[i] = int.Parse(parts[0]);

                            if (parts.Length == 3) // v/vt/vn formatum
                            {
                                if (!string.IsNullOrEmpty(parts[2]))
                                {
                                    normalIndices[i] = int.Parse(parts[2]);
                                    hasNormalIndices = true;
                                }
                            }
                            else if (parts.Length == 2) // v/vt vagy v//vn formatum
                            {
                                if (lineData[i].Contains("//"))
                                {
                                    normalIndices[i] = int.Parse(parts[1]);
                                    hasNormalIndices = true;
                                }
                            }
                        }

                        objFaces.Add(face);

                        // Csak akkor adom hozza a normalindex-tombot, ha tenyleg talaltam normalvektorokat
                        if (hasNormalIndices)
                        {
                            objNormalIndices.Add(normalIndices);
                        }
                        else
                        {
                            // Ureseket adunk hozza
                            objNormalIndices.Add(new int[lineData.Length]);
                        }
                        break;
                }
            }
        }
    }
}