using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
        private Dictionary<string, Vector3> _modelPivots = new Dictionary<string, Vector3>();
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

            foreach (var obj in objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader)) continue;

                var pivot = _modelPivots[obj.ModelKey];

                var rotationMatrix =
                    Matrix.CreateRotationX(obj.Rotation.X) *
                    Matrix.CreateRotationY(obj.Rotation.Y) *
                    Matrix.CreateRotationZ(obj.Rotation.Z);

                var world =
                    Matrix.CreateScale(obj.Scale) *
                    Matrix.CreateTranslation(-pivot) *
                    rotationMatrix *
                    Matrix.CreateTranslation(pivot + obj.Position);

                obj.CachedWorld =
                    Matrix.CreateScale(obj.Scale) *
                    Matrix.CreateTranslation(-pivot) *
                    rotationMatrix *
                    Matrix.CreateTranslation(pivot + obj.Position);

                // 🔹 BoundingBox transformieren
                obj.BoundingBox = TransformBoundingBox(loader.ModelBoundingBox, world);
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
            foreach (var obj in _objects)
            {
                if (!_models.TryGetValue(obj.ModelKey, out var loader)) continue;

                _effect.World = obj.CachedWorld;
                _effect.View = view;
                _effect.Projection = projection;

                // 🔹 Farbe je nach Auswahl setzen
                if (obj.Selected)
                {
                    _effect.DiffuseColor = new Vector3(1f, 1f, 0f); // Gelb
                }
                else
                {
                    _effect.DiffuseColor = new Vector3(1f, 1f, 1f); // Weiß
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

        

        public void DrawGizmo(List<BlueprintObject> selectedObjects, Matrix view, Matrix projection)
        {
            if (selectedObjects == null || selectedObjects.Count == 0) return;

            Vector3 pivotWorld;

            if (selectedObjects.Count == 1)
            {
                pivotWorld = GetWorldPivot(selectedObjects[0]);
            }
            else
            {
                // Mittlerer Pivot für Multiselektion
                pivotWorld = GetMultiPivot(selectedObjects);
            }

            float axisLength = 2f;
            float pivotMarkerSize = axisLength * 0.2f;

            var verts = new List<VertexPositionColor>();

            // --- Achsen (immer Weltachsen bei Multiselektion) ---
            verts.Add(new VertexPositionColor(pivotWorld, Color.Red));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Right * axisLength, Color.Red));

            verts.Add(new VertexPositionColor(pivotWorld, Color.Green));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Up * axisLength, Color.Green));

            verts.Add(new VertexPositionColor(pivotWorld, Color.Blue));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Forward * axisLength, Color.Blue));

            // --- Pivotkreuz ---
            verts.Add(new VertexPositionColor(pivotWorld - Vector3.Right * pivotMarkerSize, Color.White));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Right * pivotMarkerSize, Color.White));
            verts.Add(new VertexPositionColor(pivotWorld - Vector3.Up * pivotMarkerSize, Color.White));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Up * pivotMarkerSize, Color.White));
            verts.Add(new VertexPositionColor(pivotWorld - Vector3.Forward * pivotMarkerSize, Color.White));
            verts.Add(new VertexPositionColor(pivotWorld + Vector3.Forward * pivotMarkerSize, Color.White));

            // --- Rendering ---
            var gd = GraphicsDevice;
            using (var fx = new BasicEffect(gd))
            {
                fx.VertexColorEnabled = true;
                fx.LightingEnabled = false;
                fx.World = Matrix.Identity;
                fx.View = view;
                fx.Projection = projection;

                gd.DepthStencilState = DepthStencilState.None;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.BlendState = BlendState.Opaque;

                foreach (var pass in fx.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawUserPrimitives(PrimitiveType.LineList, verts.ToArray(), 0, verts.Count / 2);
                }
            }
        }

        private Vector3 GetWorldPivot(BlueprintObject obj)
        {
            if (!_modelPivots.TryGetValue(obj.ModelKey, out var pivotLocal))
                pivotLocal = Vector3.Zero;

            // Pivot in die Welt transformieren
            return Vector3.Transform(pivotLocal, obj.CachedWorld);
        }


        private Vector3 GetMultiPivot(List<BlueprintObject> selectedObjects)
        {
            if (selectedObjects == null || selectedObjects.Count == 0)
                return Vector3.Zero;
            if (selectedObjects.Count == 1)
                return GetWorldPivot(selectedObjects[0]);

            // Start mit der BoundingBox des ersten Objekts
            BoundingBox totalBB = selectedObjects[0].BoundingBox;

            // Alle BoundingBoxen zusammenführen
            for (int i = 1; i < selectedObjects.Count; i++)
            {
                totalBB = BoundingBox.CreateMerged(totalBB, selectedObjects[i].BoundingBox);
            }

            // Pivot = Mittelpunkt der Gesamt-BoundingBox
            return (totalBB.Min + totalBB.Max) / 2f;
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
