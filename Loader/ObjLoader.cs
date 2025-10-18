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

        public Vector3[] Vertices { get; private set; }
        public int[] Indices { get; private set; }

        public ObjLoader(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        }

        public void Load(Stream objStream)
        {
            var positions = new List<Vector3>();
            var texcoords = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertexList = new List<VertexPositionNormalTexture>();
            var indexList = new List<int>();
            var vertexCache = new Dictionary<string, int>();

            var inv = CultureInfo.InvariantCulture;

            using (var reader = new StreamReader(objStream))
            {
                string line;
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
                            {
                                positions.Add(new Vector3(
                                    float.Parse(parts[1], inv),
                                    float.Parse(parts[2], inv),
                                    float.Parse(parts[3], inv)
                                ));
                            }
                            break;

                        case "vt":
                            if (parts.Length >= 3)
                            {
                                // Y-Flip für XNA
                                texcoords.Add(new Vector2(
                                    float.Parse(parts[1], inv),
                                    1f - float.Parse(parts[2], inv)
                                ));
                            }
                            break;

                        case "vn":
                            if (parts.Length >= 4)
                            {
                                normals.Add(new Vector3(
                                    float.Parse(parts[1], inv),
                                    float.Parse(parts[2], inv),
                                    float.Parse(parts[3], inv)
                                ));
                            }
                            break;

                        case "f":
                            if (parts.Length < 4) break;

                            // Trianguliere Face falls mehr als 3 Vertices angegeben (Fan)
                            for (int i = 1; i < parts.Length - 2; i++)
                            {
                                string[] tri = { parts[1], parts[i + 1], parts[i + 2] };

                                // Normale für Face berechnen, wenn keine vn vorhanden
                                Vector3 faceNormal = Vector3.Zero;
                                if (normals.Count == 0)
                                {
                                    int a = ResolveIndex(tri[0].Split('/')[0], positions.Count);
                                    int b = ResolveIndex(tri[1].Split('/')[0], positions.Count);
                                    int c = ResolveIndex(tri[2].Split('/')[0], positions.Count);

                                    if (a >= 0 && b >= 0 && c >= 0)
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

                                // Drei Vertexeinträge anlegen
                                foreach (var token in tri)
                                {
                                    var fields = token.Split('/');

                                    int vIdx = (fields.Length >= 1) ? ResolveIndex(fields[0], positions.Count) : -1;
                                    int vtIdx = (fields.Length >= 2) ? ResolveIndex(fields[1], texcoords.Count) : -1;
                                    int vnIdx = (fields.Length >= 3) ? ResolveIndex(fields[2], normals.Count) : -1;

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

                        // o / usemtl ignorieren, aber nicht abbrechen
                        case "o":
                        case "g":
                        case "usemtl":
                        case "mtllib":
                        case "s":
                            // ignorieren, aber wichtig: keine break-Fehler verursachen
                            break;
                    }
                }
            }

            if (vertexList.Count == 0 || indexList.Count == 0)
                throw new InvalidDataException("OBJ enthält keine gültigen Vertices oder Faces.");

            // VertexBuffer
            VertexBuffer = new VertexBuffer(_gd, typeof(VertexPositionNormalTexture), vertexList.Count, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexList.ToArray());
            Vertices = vertexList.Select(v => v.Position).ToArray();

            // IndexBuffer
            Indices = indexList.ToArray();
            if (vertexList.Count < 65536)
            {
                var idx16 = new ushort[indexList.Count];
                for (int i = 0; i < indexList.Count; i++) idx16[i] = (ushort)indexList[i];
                IndexBuffer = new IndexBuffer(_gd, IndexElementSize.SixteenBits, idx16.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(idx16);
            }
            else
            {
                IndexBuffer = new IndexBuffer(_gd, IndexElementSize.ThirtyTwoBits, Indices.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(Indices);
            }

            PrimitiveCount = indexList.Count / 3;

            ModelBoundingBox = BoundingBox.CreateFromPoints(Vertices);
        }

        /// <summary>
        /// Wandelt OBJ-Indizes in 0-basiertes Array um, inkl. relativer Indizes (Blender-kompatibel).
        /// </summary>
        private int ResolveIndex(string token, int count)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)) return -1;

            if (idx < 0)
                return count + idx;  // relativer Index
            else
                return idx - 1;      // 1-basiert zu 0-basiert
        }
    }
}
