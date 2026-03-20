using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using JeremyAnsel.Media.WavefrontObj;

namespace HomeDesigner
{
    public class ObjLoader
    {
        private readonly GraphicsDevice _gd;

        public VertexBuffer VertexBuffer { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }
        public int PrimitiveCount { get; private set; }
        public BoundingBox ModelBoundingBox { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public int[] Indices { get; private set; }

        public ObjLoader(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        }

        public void Load(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                LoadFromStream(stream);
            }
        }

        public void LoadFromStream(Stream stream)
        {
            var obj = ObjFile.FromStream(stream);
            Build(obj);
        }

        private void Build(ObjFile obj)
        {
            var vertices = new List<VertexPositionNormalTexture>();


            IEnumerable<ObjGroup> groups;

            if (obj.Groups.Count > 0)
                groups = obj.Groups;
            else
                groups = new List<ObjGroup> { obj.DefaultGroup };

            foreach (var group in groups)
            {
                foreach (var face in group.Faces)
                {
                    if (face.Vertices.Count < 3)
                        continue;

                    // Triangulation
                    for (int i = 1; i < face.Vertices.Count - 1; i++)
                    {
                        AddVertex(face.Vertices[0], obj, vertices);
                        AddVertex(face.Vertices[i], obj, vertices);
                        AddVertex(face.Vertices[i + 1], obj, vertices);
                    }
                }
            }

            if (vertices.Count == 0)
                throw new Exception("OBJ enthält keine gültigen Faces (f).");

            // 🔹 Positions speichern
            Vertices = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                Vertices[i] = vertices[i].Position;

            // 🔹 Indices (sequentiell)
            Indices = new int[vertices.Count];
            for (int i = 0; i < Indices.Length; i++)
                Indices[i] = i;

            // 🔹 VertexBuffer
            VertexBuffer = new VertexBuffer(
                _gd,
                typeof(VertexPositionNormalTexture),
                vertices.Count,
                BufferUsage.WriteOnly);

            VertexBuffer.SetData(vertices.ToArray());

            // 🔹 IndexBuffer
            IndexBuffer = new IndexBuffer(
                _gd,
                IndexElementSize.ThirtyTwoBits,
                Indices.Length,
                BufferUsage.WriteOnly);

            IndexBuffer.SetData(Indices);

            PrimitiveCount = vertices.Count / 3;

            ModelBoundingBox = BoundingBox.CreateFromPoints(Vertices);
        }

        private void AddVertex(
            ObjTriplet triplet,
            ObjFile obj,
            List<VertexPositionNormalTexture> vertices)
        {
            // ❗ Safety Check
            if (triplet.Vertex <= 0 || triplet.Vertex > obj.Vertices.Count)
                return;

            // 🔹 Position
            var v = obj.Vertices[triplet.Vertex - 1].Position;
            Vector3 position = new Vector3((float)v.X, (float)v.Y, (float)v.Z);

            // 🔹 Normal
            Vector3 normal = Vector3.Up;
            if (triplet.Normal > 0 && triplet.Normal <= obj.VertexNormals.Count)
            {
                var n = obj.VertexNormals[triplet.Normal - 1];
                normal = new Vector3((float)n.X, (float)n.Y, (float)n.Z);
            }

            // 🔹 UV (optional → dein Fall!)
            Vector2 uv = Vector2.Zero;
            if (triplet.Texture > 0 && triplet.Texture <= obj.TextureVertices.Count)
            {
                var t = obj.TextureVertices[triplet.Texture - 1];
                uv = new Vector2((float)t.X, 1f - (float)t.Y);
            }

            vertices.Add(new VertexPositionNormalTexture(position, normal, uv));
        }
    }
}