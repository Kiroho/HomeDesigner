using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HomeDesigner
{
    public class BlueprintRenderer : IDisposable
    {
        private Ray? _debugRay;
        private float _debugRayLength = 200f;

        public GraphicsDevice GraphicsDevice { get; }

        private readonly ContentsManager contentManager;

        private Dictionary<string, ObjLoader> _models = new Dictionary<string, ObjLoader>();
        private Dictionary<string, ObjLoader> _gizmoModels = new Dictionary<string, ObjLoader>();
        public Dictionary<string, Vector3> _modelPivots = new Dictionary<string, Vector3>();
        private BasicEffect _effect;
        



        // Weltmatrizen pro Modell key vorberechnen
        private Dictionary<string, List<Matrix>> _precomputedWorlds = new Dictionary<string, List<Matrix>>();

        public BlueprintRenderer(GraphicsDevice graphicsDevice, ContentsManager contentManager)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));

            _effect = new BasicEffect(GraphicsDevice)
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
            _effect.DirectionalLight1.DiffuseColor = new Vector3(0.4f, 0.4f, 0.4f); // etwas schwächer
            _effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-0.3f, 0.3f, -0.1f));

            // Licht 3 – Fülllicht
            _effect.DirectionalLight2.Enabled = true;
            _effect.DirectionalLight2.DiffuseColor = new Vector3(0.55f, 0.55f, 0.55f); // etwas schwächer
            _effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0.0f, -0.5f, -0.4f));

            _effect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f);

        }

        public void LoadModel(string key, string path, Vector3 pivot)
        {
            using (var stream = contentManager.GetFileStream(path))
            {
                var loader = new ObjLoader(GraphicsDevice);
                loader.Load(stream);
                _models[key] = loader;
                _modelPivots[key] = pivot;

                // 🔹 BoundingBox aus den geladenen Vertices berechnen
                var verts = loader.Vertices; // Annahme: dein ObjLoader hat die Vertex-Positionen
                var bb = BoundingBox.CreateFromPoints(verts);
                loader.ModelBoundingBox = bb; // Eigenschaft im ObjLoader ergänzen

                //var gg = loader.ModelBoundingBox;
                //System.Diagnostics.Debug.WriteLine($"BoundingBox: Min={gg.Min}, Max={gg.Max}");

            }
        }

        public void LoadGizmoModel(string key, string path)
        {
            using (var stream = contentManager.GetFileStream(path))
            {
                var loader = new ObjLoader(GraphicsDevice);
                loader.Load(stream);
                _gizmoModels[key] = loader;

                // 🔹 BoundingBox aus den geladenen Vertices berechnen
                var verts = loader.Vertices; 
                var bb = BoundingBox.CreateFromPoints(verts);
                loader.ModelBoundingBox = bb; // Eigenschaft im ObjLoader ergänzen

                //var gg = loader.ModelBoundingBox;
                //System.Diagnostics.Debug.WriteLine($"BoundingBox: Min={gg.Min}, Max={gg.Max}");

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

            foreach (var obj in objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader)) continue;

                var pivot = _modelPivots[obj.ModelKey];

                // 🔸 Rotation über Quaternion
                // 🔸 Blender-Korrektur und Objektrotation kombinieren
                //var finalRotation = blenderCorrection * obj.RotationQuaternion;
                //var rotationMatrix = Matrix.CreateFromQuaternion(finalRotation);
                var rotationMatrix = Matrix.CreateFromQuaternion(obj.RotationQuaternion);

                var world =
                    Matrix.CreateScale(obj.Scale) *
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

                var pivot = Vector3.Zero; // Gizmos meist ohne Pivot-Korrektur

                var rotationMatrix = Matrix.CreateFromQuaternion(gizmo.RotationQuaternion);

                var world =
                    Matrix.CreateScale(gizmo.Scale) *
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
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            // DepthBuffer beschreibbar deaktivieren (verhindert Flackerprobleme)
            GraphicsDevice.DepthStencilState = new DepthStencilState()
            {
                DepthBufferWriteEnable = false,
                DepthBufferFunction = CompareFunction.LessEqual
            };



            foreach (var obj in _objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader)) continue;

                _effect.World = obj.CachedWorld;
                _effect.View = view;
                _effect.Projection = projection;

                // 🔹 Farbe je nach Auswahl setzen
                if (obj.Selected)
                {
                    _effect.DiffuseColor = new Vector3(0.6f, 0.6f, 0.1f); // Gelb
                    _effect.Alpha = 1f;   // Deckkraft
                }
                else
                {
                    _effect.DiffuseColor = new Vector3(0.15f, 0.55f, 1f); // Blau
                    _effect.Alpha = 1f;   // Deckkraft
                }

                GraphicsDevice.SetVertexBuffer(loader.VertexBuffer);
                GraphicsDevice.Indices = loader.IndexBuffer;

                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, loader.PrimitiveCount);
                }
            }

            // --- Debug-Ray am Ende zeichnen ---
            if (_debugRay.HasValue)
            {
                DrawDebugRay(_debugRay.Value, view, projection, _debugRayLength);
                // Optional: nur ein Frame anzeigen:
                // _debugRay = null;
            }
        }


        public void SetDebugRay(Ray ray, float length = 200f)
        {
            _debugRay = ray;
            _debugRayLength = length;
        }
        private void DrawDebugRay(Ray ray, Matrix view, Matrix projection, float length)
        {
            var gd = this.GraphicsDevice;

            // Schutz
            if (ray.Direction == Vector3.Zero) return;
            Vector3 dir = Vector3.Normalize(ray.Direction);

            Vector3 start = ray.Position;
            Vector3 end = start + dir * length;

            // --- Hauptlinie (rot) ---
            var lineVerts = new[]
            {
                new VertexPositionColor(start, Color.Red),
                new VertexPositionColor(end,   Color.Red)
            };

            // --- Pfeilkopf (gelb) ---
            // Bestimme zwei orthogonale Achsen zur Richtung:
            Vector3 up = Vector3.Up;
            if (Math.Abs(Vector3.Dot(dir, up)) > 0.99f) up = Vector3.Right; // Falls parallel zu Up
            Vector3 axis1 = Vector3.Cross(dir, up);
            if (axis1 != Vector3.Zero) axis1.Normalize();
            Vector3 axis2 = Vector3.Cross(dir, axis1);
            if (axis2 != Vector3.Zero) axis2.Normalize();

            float headLength = MathHelper.Min(length * 0.05f, 2f); // max 2 world-units
            float headWidth = headLength * 0.8f;

            Vector3 p1 = end - dir * headLength + axis1 * headWidth;
            Vector3 p2 = end - dir * headLength - axis1 * headWidth;
            Vector3 p3 = end - dir * headLength + axis2 * headWidth;
            Vector3 p4 = end - dir * headLength - axis2 * headWidth;

            var arrowVerts = new[]
            {
                // 4 Linien vom tip zum Punkten
                new VertexPositionColor(end, Color.Yellow), new VertexPositionColor(p1, Color.Yellow),
                new VertexPositionColor(end, Color.Yellow), new VertexPositionColor(p2, Color.Yellow),
                new VertexPositionColor(end, Color.Yellow), new VertexPositionColor(p3, Color.Yellow),
                new VertexPositionColor(end, Color.Yellow), new VertexPositionColor(p4, Color.Yellow),
            };

            // --- kleine Kreuze an Start und Ende (grün/blau) ---
            float crossSize = MathHelper.Min(length * 0.01f, 0.5f); // klein, relativ zur Länge
            var crossVerts = new List<VertexPositionColor>();

            // Start-Kreuz (grün)
            crossVerts.Add(new VertexPositionColor(start - Vector3.Right * crossSize, Color.Lime));
            crossVerts.Add(new VertexPositionColor(start + Vector3.Right * crossSize, Color.Lime));
            crossVerts.Add(new VertexPositionColor(start - Vector3.Up * crossSize, Color.Lime));
            crossVerts.Add(new VertexPositionColor(start + Vector3.Up * crossSize, Color.Lime));
            crossVerts.Add(new VertexPositionColor(start - Vector3.Forward * crossSize, Color.Lime));
            crossVerts.Add(new VertexPositionColor(start + Vector3.Forward * crossSize, Color.Lime));

            // End-Kreuz (cyan)
            crossVerts.Add(new VertexPositionColor(end - Vector3.Right * crossSize, Color.Cyan));
            crossVerts.Add(new VertexPositionColor(end + Vector3.Right * crossSize, Color.Cyan));
            crossVerts.Add(new VertexPositionColor(end - Vector3.Up * crossSize, Color.Cyan));
            crossVerts.Add(new VertexPositionColor(end + Vector3.Up * crossSize, Color.Cyan));
            crossVerts.Add(new VertexPositionColor(end - Vector3.Forward * crossSize, Color.Cyan));
            crossVerts.Add(new VertexPositionColor(end + Vector3.Forward * crossSize, Color.Cyan));

            // --- Effekte setzen und zeichnen ---
            using (var fx = new BasicEffect(gd) { VertexColorEnabled = true, World = Matrix.Identity, View = view, Projection = projection })
            {
                foreach (var pass in fx.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    // Linie
                    gd.DrawUserPrimitives(PrimitiveType.LineList, lineVerts, 0, 1);
                    // Pfeil
                    gd.DrawUserPrimitives(PrimitiveType.LineList, arrowVerts, 0, arrowVerts.Length / 2);
                    // Kreuze
                    gd.DrawUserPrimitives(PrimitiveType.LineList, crossVerts.ToArray(), 0, crossVerts.Count / 2);
                }
            }
        }


        public void DrawGizmo(Matrix view, Matrix projection, List<BlueprintObject> gizmoObjects, BlueprintObject activeGizmo)
        {
            if (gizmoObjects == null || gizmoObjects.Count == 0)
                return;

            var gd = GraphicsDevice;

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
                    _effect.Alpha = activeGizmo != null ? 0.15f : 0.7f;
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
        }
    }
}
