using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace HomeDesigner
{
    public class ObjLoader
    {
        private readonly GraphicsDevice _gd;

        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }
        public int PrimitiveCount { get; private set; }
        public BoundingBox ModelBoundingBox { get; set; }

        // 🔹 NEU: Alle VertexPositionen im Array für Raycasting speichern
        public Vector3[] Vertices { get; private set; }
        public int[] Indices { get; private set; }

        public ObjLoader(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        }

        /// <summary>
        /// Lädt ein OBJ aus einem Stream (z. B. ContentsManager.GetFile())
        /// und erzeugt VertexPositionNormalTexture + IndexBuffer.
        /// </summary>
        public void Load(Stream objStream)
        {
            var positions = new List<Vector3>();
            var texcoords = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertexList = new List<VertexPositionNormalTexture>();
            var indexList = new List<int>();
            var vertexCache = new Dictionary<string, int>();

            using (var reader = new StreamReader(objStream))
            {
                string line;
                var inv = CultureInfo.InvariantCulture;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;

                    var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    switch (parts[0])
                    {
                        case "v":
                            if (parts.Length >= 4)
                                positions.Add(new Vector3(
                                    float.Parse(parts[1], inv),
                                    float.Parse(parts[2], inv),
                                    float.Parse(parts[3], inv)));
                            break;
                        case "vt":
                            if (parts.Length >= 3)
                                texcoords.Add(new Vector2(
                                    float.Parse(parts[1], inv),
                                    float.Parse(parts[2], inv)));
                            break;
                        case "vn":
                            if (parts.Length >= 4)
                                normals.Add(new Vector3(
                                    float.Parse(parts[1], inv),
                                    float.Parse(parts[2], inv),
                                    float.Parse(parts[3], inv)));
                            break;
                        case "f":
                            if (parts.Length < 4) break;
                            for (int i = 1; i < parts.Length - 2; i++)
                            {
                                string[] tri = { parts[1], parts[i + 1], parts[i + 2] };
                                Vector3 faceNormal = Vector3.Zero;
                                bool needFaceNormal = (normals.Count == 0);

                                if (needFaceNormal)
                                {
                                    int a = ParseVertexIndex(tri[0]) - 1;
                                    int b = ParseVertexIndex(tri[1]) - 1;
                                    int c = ParseVertexIndex(tri[2]) - 1;
                                    if (a >= 0 && b >= 0 && c >= 0 && a < positions.Count && b < positions.Count && c < positions.Count)
                                    {
                                        var pA = positions[a];
                                        var pB = positions[b];
                                        var pC = positions[c];
                                        faceNormal = Vector3.Cross(pB - pA, pC - pA);
                                        if (faceNormal != Vector3.Zero) faceNormal.Normalize();
                                    }
                                    else
                                    {
                                        faceNormal = Vector3.Up;
                                    }
                                }

                                foreach (var token in tri)
                                {
                                    var fields = token.Split('/');
                                    int vIdx = (fields.Length >= 1 && fields[0].Length > 0) ? int.Parse(fields[0]) - 1 : -1;
                                    int vtIdx = (fields.Length >= 2 && fields[1].Length > 0) ? int.Parse(fields[1]) - 1 : -1;
                                    int vnIdx = (fields.Length >= 3 && fields[2].Length > 0) ? int.Parse(fields[2]) - 1 : -1;

                                    string key = $"{vIdx}/{vtIdx}/{vnIdx}";
                                    if (!vertexCache.TryGetValue(key, out int vertIndex))
                                    {
                                        Vector3 pos = (vIdx >= 0 && vIdx < positions.Count) ? positions[vIdx] : Vector3.Zero;
                                        Vector2 tex = (vtIdx >= 0 && vtIdx < texcoords.Count) ? texcoords[vtIdx] : Vector2.Zero;
                                        Vector3 norm = (vnIdx >= 0 && vnIdx < normals.Count) ? normals[vnIdx] : faceNormal;
                                        if (norm != Vector3.Zero) norm.Normalize(); else norm = Vector3.Up;

                                        var v = new VertexPositionNormalTexture(pos, norm, tex);
                                        vertIndex = vertexList.Count;
                                        vertexList.Add(v);
                                        vertexCache.Add(key, vertIndex);
                                    }
                                    indexList.Add(vertIndex);
                                }
                            }
                            break;
                    }
                }
            }

            // Vertex/IndexBuffer erstellen
            VertexBuffer = new VertexBuffer(_gd, typeof(VertexPositionNormalTexture), vertexList.Count, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexList.ToArray());

            // Nach Vertex/IndexBuffer Erstellung:
            Vertices = vertexList.Select(v => v.Position).ToArray();

            if (vertexList.Count < 65536)
            {
                Indices = indexList.Select(i => (int)i).ToArray();
            }
            else
            {
                Indices = indexList.ToArray();
            }

            if (vertexList.Count < 65536)
            {
                var idx16 = new ushort[indexList.Count];
                for (int i = 0; i < indexList.Count; i++) idx16[i] = (ushort)indexList[i];
                IndexBuffer = new IndexBuffer(_gd, IndexElementSize.SixteenBits, idx16.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(idx16);
            }
            else
            {
                var idx32 = indexList.ToArray();
                IndexBuffer = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, idx32.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(idx32);
            }

            PrimitiveCount = indexList.Count / 3;

            if (Vertices != null && Vertices.Length > 0)
            {
                ModelBoundingBox = BoundingBox.CreateFromPoints(Vertices);
            }
        }

        private int ParseVertexIndex(string token)
        {
            var parts = token.Split('/');
            if (parts.Length == 0) return -1;
            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return v;
            return -1;
        }


    }
}
