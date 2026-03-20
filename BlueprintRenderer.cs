using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HomeDesigner
{
    public class BlueprintRenderer : IDisposable
    {
        private Module module;
        public GraphicsDevice graphicsDevice { get; }

        public readonly ContentsManager contentManager;

        public ConcurrentDictionary<string, ObjLoader> _models = new ConcurrentDictionary<string, ObjLoader>();
        private ConcurrentDictionary<string, ObjLoader> _gizmoModels = new ConcurrentDictionary<string, ObjLoader>();
        public ConcurrentDictionary<string, Vector3> _modelPivots = new ConcurrentDictionary<string, Vector3>();
        public DecorationLUT decorationLut = new DecorationLUT();
        public Dictionary<int, AsyncTexture2D> decoIconDict = new Dictionary<int, AsyncTexture2D>();
        public Dictionary<int, string> decoCategories = new Dictionary<int, string>();
        private BasicEffect _effect;
        


        public BlueprintRenderer(Module module)
        {
            this.module = module;
            graphicsDevice = module.gd;
            this.contentManager = module.ContentsManager;

            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = false,
                LightingEnabled = true
            };
            // Licht 1 – Hauptlicht
            _effect.DirectionalLight0.Enabled = true;
            _effect.DirectionalLight0.DiffuseColor = Vector3.One;
            _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0.3f, 0.5f, 0.1f));

            // Licht 2 – Fülllicht
            _effect.DirectionalLight1.Enabled = true;
            _effect.DirectionalLight1.DiffuseColor = new Vector3(0.7f, 0.7f, 0.7f); // etwas schwächer
            _effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-0.3f, -0.4f, -0.1f));

            // Licht 3 – Fülllicht
            _effect.DirectionalLight2.Enabled = true;
            _effect.DirectionalLight2.DiffuseColor = new Vector3(0.6f, 0.6f, 0.6f); 
            _effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0.0f, -0.5f, -0.4f));

            _effect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f);

        }


        // Load von Dokumente
        public void LoadModel(string key, string path, Vector3 pivot)
        {
            var dataPath = Path.Combine(path, key + ".obj");
            if (!File.Exists(dataPath))
            {
                using (var stream = contentManager.GetFileStream("models/placeholder.obj"))
                {
                    var loader = new ObjLoader(graphicsDevice);
                    loader.LoadFromStream(stream);
                    _models[key] = loader;
                    _modelPivots[key] = pivot;
                }
            }
            else
            {
                var loader = new ObjLoader(graphicsDevice);
                loader.Load(dataPath);

                _models[key] = loader;
                _modelPivots[key] = pivot;
            }

        }


        public void LoadGizmoModel(string key, string path)
        {
            using (var stream = contentManager.GetFileStream(path))
            {
                var loader = new ObjLoader(graphicsDevice);
                loader.LoadFromStream(stream);
                _gizmoModels[key] = loader;
            }



        }


        public IEnumerable<string> GetModelKeys()
        {
            return _models.Keys;
        }


        /// <summary>
        /// Berechnet für jedes Objekt die Weltmatrix und gruppiert sie nach Modellkey
        /// </summary>
        public void PrecomputeWorlds(List<BlueprintObject> objects)
        {
            // Rotations-Korrektur: -90° um X, um Blender Z-Up -> MonoGame Y-Up anzupassen
            //var blenderCorrection = Quaternion.CreateFromAxisAngle(Vector3.Right, MathHelper.ToRadians(90));
            //float adjustScale = 0.0255f; // Scale factor to make obj files fit ingame size
            float adjustScale = 1f; // Scale factor to make obj files fit ingame size

            foreach (var obj in objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader))
                {
                    //Debug.WriteLine("------------ Model nicht gefunden!! -----------");
                    continue;
                }

                var pivot = _modelPivots[obj.ModelKey];

                // 🔸 Rotation über Quaternion
                // 🔸 Blender-Korrektur und Objektrotation kombinieren
                //var finalRotation = blenderCorrection * obj.RotationQuaternion;
                //var rotationMatrix = Matrix.CreateFromQuaternion(finalRotation);
                var rotationMatrix = Matrix.CreateFromQuaternion(obj.RotationQuaternion);

                var world =
                    Matrix.CreateScale(obj.Scale*adjustScale) *
                    Matrix.CreateTranslation(-pivot) *
                    rotationMatrix *
                    Matrix.CreateTranslation(pivot + obj.Position);

                obj.CachedWorld = world;

                obj.BoundingBox = TransformBoundingBox(loader.ModelBoundingBox, world);
            }
        }

        public void PrecomputeGizmoWorlds(List<BlueprintObject> gizmos)
        {
            foreach (var gizmo in gizmos)
            {
                if (!_gizmoModels.TryGetValue(gizmo.ModelKey, out var loader))
                    continue;

                Vector3 camPos = GameService.Gw2Mumble.PlayerCamera.Position;
                float distance = Vector3.Distance(camPos, gizmo.Position);
                float baseScale = module.gizmoSize.Value*0.001f;
                float scale = distance * baseScale;
                if (scale < 0.001f) scale = 0.001f;
                else if (scale > 100f) scale = 100f;

                var pivot = Vector3.Zero; // Gizmos meist ohne Pivot-Korrektur

                var rotationMatrix = Matrix.CreateFromQuaternion(gizmo.RotationQuaternion);

                var world =
                    Matrix.CreateScale(scale) *
                    Matrix.CreateTranslation(-pivot) *
                    rotationMatrix *
                    Matrix.CreateTranslation(pivot + gizmo.Position);

                gizmo.CachedWorld = world;

                gizmo.BoundingBox = TransformBoundingBox(loader.ModelBoundingBox, world);
            }
        }



        // Hilfsfunktion zum Transformieren
        private BoundingBox TransformBoundingBox(BoundingBox box, Matrix transform)
        {
            var corners = box.GetCorners();
            Vector3.Transform(corners, ref transform, corners);

            return BoundingBox.CreateFromPoints(corners);
        }


        // BlueprintRenderer
        public void Draw(Matrix view, Matrix projection, List<BlueprintObject> _objects)
        {

            // 💡 Transparenz aktivieren
            graphicsDevice.BlendState = BlendState.AlphaBlend;

            // DepthBuffer beschreibbar deaktivieren (verhindert Flackerprobleme)
            graphicsDevice.DepthStencilState = new DepthStencilState()
            {
                DepthBufferWriteEnable = false,
                DepthBufferFunction = CompareFunction.LessEqual
            };

            var playerPos = GameService.Gw2Mumble.PlayerCharacter.Position;

            foreach (var obj in _objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader))
                {
                    //Debug.WriteLine("------------ Model nicht gefunden!! -----------");
                    continue;
                }

                float dist = Vector3.Distance(playerPos, obj.Position);
                if (dist > module.renderDistance.Value)
                {
                    continue;
                }

                _effect.World = obj.CachedWorld;
                _effect.View = view;
                _effect.Projection = projection;

                // 🔹 Farbe je nach Auswahl setzen
                if (obj.Selected)
                {
                    _effect.DiffuseColor = new Vector3(0.6f, 0.6f, 0.1f); // Gelb
                    _effect.Alpha = 1f;   // Deckkraft
                }
                else if (!obj.IsOriginal)
                {
                    _effect.DiffuseColor = new Vector3(0.6f, 0.6f, 0.1f); // Gelb
                    _effect.Alpha = 0.5f;   // Deckkraft
                }
                else
                {
                    _effect.DiffuseColor = new Vector3(0.15f, 0.55f, 1f); // Blau
                    _effect.Alpha = 1f;   // Deckkraft
                }


                graphicsDevice.SetVertexBuffer(loader.VertexBuffer);
                graphicsDevice.Indices = loader.IndexBuffer;

                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                graphicsDevice.RasterizerState = RasterizerState.CullNone;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, loader.PrimitiveCount);
                }
            }

        }


        public void DrawGizmo(Matrix view, Matrix projection, List<BlueprintObject> gizmoObjects, BlueprintObject activeGizmo)
        {
            if (gizmoObjects == null || gizmoObjects.Count == 0)
                return;

            var gd = graphicsDevice;

            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.AlphaBlend;

            // 🔸 Depth-Buffer komplett ausschalten (alles im Vordergrund)
            gd.DepthStencilState = DepthStencilState.None;

            foreach (var gizmo in gizmoObjects)
            {
                if (!_gizmoModels.TryGetValue(gizmo.ModelKey, out var loader))
                    continue;

                _effect.World = gizmo.CachedWorld;
                _effect.View = view;
                _effect.Projection = projection;

                // 🔹 Farbwahl nach Achse
                bool isActive = activeGizmo != null && gizmo.ModelKey == activeGizmo.ModelKey;

                // 🔹 Basisfarbe nach Achse
                if (gizmo.ModelKey.Contains("X"))
                    _effect.DiffuseColor = new Vector3(1f, 0f, 0f);
                else if (gizmo.ModelKey.Contains("Y"))
                    _effect.DiffuseColor = new Vector3(0f, 1f, 0f);
                else if (gizmo.ModelKey.Contains("Z"))
                    _effect.DiffuseColor = new Vector3(0.0f, 0.2f, 1f);
                else
                    _effect.DiffuseColor = new Vector3(1f, 1f, 1f);

                if (isActive)
                {
                    _effect.Alpha = 1f;
                    float pulse = 0.8f + 0.2f * (float)Math.Sin(GameService.Overlay.CurrentGameTime.TotalGameTime.TotalSeconds * 6);
                    _effect.DiffuseColor *= pulse;
                }
                else
                {
                    // Transparenter, wenn ein anderes Gizmo aktiv ist
                    _effect.Alpha = activeGizmo != null ? 0.15f : 0.4f;
                    _effect.DiffuseColor *= 0.8f;
                }



                gd.SetVertexBuffer(loader.VertexBuffer);
                gd.Indices = loader.IndexBuffer;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, loader.PrimitiveCount);
                }
            }

            gd.DepthStencilState = DepthStencilState.Default;
        }








        public void Dispose()
        {
            _effect?.Dispose();
            foreach (var loader in _models.Values)
            {
                loader.VertexBuffer?.Dispose();
                loader.IndexBuffer?.Dispose();
            }
            _models.Clear();
            decorationLut = null;
            foreach(var icon in decoIconDict)
            {
                icon.Value?.Dispose();
            }
            decoIconDict.Clear();
            _models.Clear();
            _gizmoModels.Clear();

        }
    }
}
