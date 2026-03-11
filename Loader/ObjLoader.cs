using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Assimp;
using System;
using System.Collections.Generic;
using System.IO;

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

        #region Load from File Path
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var importer = new AssimpContext();
            var scene = importer.ImportFile(
                filePath,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.JoinIdenticalVertices |
                PostProcessSteps.ImproveCacheLocality |
                PostProcessSteps.PreTransformVertices |
                PostProcessSteps.OptimizeMeshes |
                PostProcessSteps.OptimizeGraph
            );

            ProcessScene(scene);
        }
        #endregion

        #region Load from Stream
        /// <summary>
        /// Importiert ein Modell aus einem Stream. Das Format muss angegeben werden, z.B. "obj".
        /// </summary>
        public void LoadFromStream(Stream objStream, string format)
        {
            if (objStream == null)
                throw new ArgumentNullException(nameof(objStream));
            if (string.IsNullOrEmpty(format))
                throw new ArgumentException("Format muss angegeben werden (z.B. 'obj').", nameof(format));

            var importer = new AssimpContext();
            var scene = importer.ImportFileFromStream(
                objStream,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.JoinIdenticalVertices |
                PostProcessSteps.ImproveCacheLocality |
                PostProcessSteps.PreTransformVertices |
                PostProcessSteps.OptimizeMeshes |
                PostProcessSteps.OptimizeGraph,
                format
            );

            ProcessScene(scene);
        }
        #endregion

        #region Scene Processing (gemeinsamer Code)
        private void ProcessScene(Scene scene)
        {
            if (scene == null || scene.MeshCount == 0)
                throw new InvalidDataException("Keine gültigen Meshes gefunden.");

            var vertices = new List<VertexPositionNormalTexture>(scene.MeshCount * 1024);
            var indices = new List<int>(scene.MeshCount * 2048);

            foreach (var mesh in scene.Meshes)
            {
                int baseVertex = vertices.Count;

                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var v = mesh.Vertices[i];
                    var n = mesh.HasNormals ? mesh.Normals[i] : new Assimp.Vector3D(0, 1, 0);

                    Vector2 uv = Vector2.Zero;
                    if (mesh.HasTextureCoords(0))
                    {
                        var tex = mesh.TextureCoordinateChannels[0][i];
                        uv = new Vector2(tex.X, 1f - tex.Y); // XNA UV-Flip
                    }

                    vertices.Add(new VertexPositionNormalTexture(
                        new Vector3(v.X, v.Y, v.Z),
                        new Vector3(n.X, n.Y, n.Z),
                        uv
                    ));
                }

                foreach (var face in mesh.Faces)
                {
                    if (face.IndexCount != 3)
                        continue;

                    indices.Add(baseVertex + face.Indices[0]);
                    indices.Add(baseVertex + face.Indices[1]);
                    indices.Add(baseVertex + face.Indices[2]);
                }
            }

            if (vertices.Count == 0 || indices.Count == 0)
                throw new InvalidDataException("Keine Vertices oder Faces gefunden.");

            Vertices = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                Vertices[i] = vertices[i].Position;

            Indices = indices.ToArray();
            PrimitiveCount = Indices.Length / 3;

            // VertexBuffer
            VertexBuffer = new VertexBuffer(
                _gd,
                typeof(VertexPositionNormalTexture),
                vertices.Count,
                BufferUsage.WriteOnly
            );
            VertexBuffer.SetData(vertices.ToArray());

            // IndexBuffer
            if (vertices.Count < 65536)
            {
                ushort[] idx16 = new ushort[Indices.Length];
                for (int i = 0; i < Indices.Length; i++)
                    idx16[i] = (ushort)Indices[i];

                IndexBuffer = new IndexBuffer(
                    _gd,
                    IndexElementSize.SixteenBits,
                    idx16.Length,
                    BufferUsage.WriteOnly
                );
                IndexBuffer.SetData(idx16);
            }
            else
            {
                IndexBuffer = new IndexBuffer(
                    _gd,
                    IndexElementSize.ThirtyTwoBits,
                    Indices.Length,
                    BufferUsage.WriteOnly
                );
                IndexBuffer.SetData(Indices);
            }

            ModelBoundingBox = BoundingBox.CreateFromPoints(Vertices);
        }
        #endregion
    }
}