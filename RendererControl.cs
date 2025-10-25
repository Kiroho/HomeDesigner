using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Controls;
using System.Collections.Generic;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System;

namespace HomeDesigner
{
    public class RendererControl : Control
    {
        private InputBlocker inputBlocker;

        private readonly BlueprintRenderer _renderer;
        public List<BlueprintObject> Objects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> SelectedObjects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> TranslateGizmos { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> RotateGizmos { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> ScaleGizmos { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> CopiedObjects { get; } = new List<BlueprintObject>();
        public enum GizmoMode { Translate, Rotate, Scale }
        public GizmoMode gizmoMode = GizmoMode.Translate;

        private Vector3 pivotObject = Vector3.Zero;
        private Quaternion pivotRotation = Quaternion.Identity;
        public enum RotationSpace { World, Local }
        public RotationSpace _rotationSpace = RotationSpace.World;

        public enum TransformMode { Translate, Rotate, Scale }
        public TransformMode currentMode = TransformMode.Translate;

        // For Gizmo Dragging
        private bool _gizmoActive = false;
        private Point _startMousePos;
        private BlueprintObject activeGizmo= null;

        private Dictionary<BlueprintObject, Vector3> _startPositions = new Dictionary<BlueprintObject, Vector3>();
        private bool _dragInitialized = false;
        private Ray _startRay;
        private Vector3 _activeAxis;
        private Vector3 _lastProjectedPoint;

        private Dictionary<BlueprintObject, Quaternion> _startRotations = new Dictionary<BlueprintObject, Quaternion>();
        private Vector3 _rotationStartVec;
        private bool _rotationInitialized = false;




        public RendererControl(BlueprintRenderer renderer)
        {
            _renderer = renderer;
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
            Visible = true;
            // Maus-Handler registrieren
            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;


            //inputBlocker = new InputBlocker();

        }



        public BlueprintRenderer getBlueprintrenderer()
        {
            return _renderer;
        }

        public void updateWorld()
        {
            _renderer.PrecomputeWorlds(Objects); // Weltmatrizen einmal vorberechnen
        }

        public void updateGizmos()
        {
            _renderer.PrecomputeGizmoWorlds(TranslateGizmos);
            _renderer.PrecomputeGizmoWorlds(RotateGizmos);
            _renderer.PrecomputeGizmoWorlds(ScaleGizmos);
        }

        public void AddObject(BlueprintObject obj)
        {
            Objects.Add(obj);
            _renderer.PrecomputeWorlds(Objects); // Weltmatrizen einmal vorberechnen
        }

        public void RemoveObject(BlueprintObject obj)
        {
            Objects.Remove(obj);
            _renderer.PrecomputeWorlds(Objects);
        }


        public void AddTranslateGizmos(BlueprintObject obj)
        {
            TranslateGizmos.Add(obj);
        }
        public void AddRotateGizmos(BlueprintObject obj)
        {
            RotateGizmos.Add(obj);
        }
        public void AddScaleGizmos(BlueprintObject obj)
        {
            ScaleGizmos.Add(obj);
        }

        public void ClearSelection()
        {
            foreach (var obj in Objects)
                obj.Selected = false;
            SelectedObjects.Clear();
            clearPivotObject();
            clearPivotRotation();
        }

        public void SelectObject(BlueprintObject obj, bool multiSelect = false)
        {
            if (!multiSelect) ClearSelection(); // Wenn kein Multiselect, vorherige Auswahl löschen
            obj.Selected = true;
            SelectedObjects.Add(obj);
            if (_rotationSpace == RotationSpace.Local)
            {
                // Wenn lokale Achsen genutzt werden, soll das Gizmo die Objektrotation übernehmen
                pivotRotation = obj.RotationQuaternion;
            }
            else
            {
                // Weltmodus -> Gizmo bleibt an Weltachsen ausgerichtet
                pivotRotation = Quaternion.Identity;
            }
            updateGizmos();
        }

        

        public Vector3 getPivotObject()
        {
            return (Vector3)pivotObject;
        }

        public void clearPivotObject()
        {
            pivotObject = Vector3.Zero;
        }

        public void setPivotRotation(Quaternion rot)
        {
            pivotRotation = rot;
        }
        public Quaternion getPivotRotation()
        {
            return pivotRotation;
        }

        public void clearPivotRotation()
        {
            pivotRotation = Quaternion.Identity;
        }

        public void updateWorldPivot()
        {
            if (!_renderer._modelPivots.TryGetValue(SelectedObjects[0].ModelKey, out var pivotLocal))
                pivotLocal = Vector3.Zero;

            // Pivot in die Welt transformieren
            pivotObject =  Vector3.Transform(pivotLocal, SelectedObjects[0].CachedWorld);
        }


        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var gd = _renderer.GraphicsDevice;

            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullNone;

            // Kamera aus Mumble
            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            // Alle Objekte zeichnen 
            _renderer.Draw(view, projection, Objects);

            // 🔹 Gizmo nur anzeigen, wenn genau ein Objekt selektiert ist
            if (SelectedObjects.Count > 0)
            {
                ////System.Diagnostics.Debug.WriteLine(">>> Gizmo wird gezeichnet!");
                updateWorldPivot();

                switch (gizmoMode)
                {
                    case GizmoMode.Translate:
                        paintGizmo(view, projection, TranslateGizmos);
                        break;
                    case GizmoMode.Rotate:
                        paintGizmo(view, projection, RotateGizmos);
                        break;
                    case GizmoMode.Scale:
                        paintGizmo(view, projection, ScaleGizmos);
                        break;
                }

            }
        }

        private void paintGizmo(Matrix view, Matrix projection,List<BlueprintObject> gizmolist)
        {
            foreach (var ob in gizmolist)
            {
                ob.Position = pivotObject;
                ob.RotationQuaternion = pivotRotation;
            }
            _renderer.PrecomputeGizmoWorlds(gizmolist);
            _renderer.DrawGizmo(view, projection, gizmolist, activeGizmo);
        }



















        private void OnLeftMouseButtonPressed(object sender, MouseEventArgs e)
        {
            // 🛑 Wenn Maus gerade über einem UI Control liegt → Klick ignorieren
            if (Control.ActiveControl != null)
            {
                //ScreenNotification.ShowNotification($"Klick auf UI: { Control.ActiveControl}");
                return;
            }

            bool ctrlDown = GameService.Input.Keyboard.KeysDown.Contains(Keys.LeftControl);
            bool altDown = GameService.Input.Keyboard.KeysDown.Contains(Keys.LeftAlt);

            if (ctrlDown)
            {
            }
            else if (altDown)
            {
                RaycastSelect(e, true);
            }
            else
            {
                RaycastSelect(e, false);
            }
        }

        /// <summary>
        /// Führt einen Raycast aus und gibt alle getroffenen Objekte zurück,
        /// sortiert nach Distanz (nächstes zuerst).
        /// </summary>
        private void RaycastSelect(MouseEventArgs e, bool multi = false)
        {
            var ray = CreateRayFromMouse();
            //_renderer.SetDebugRay(ray, 200f);

            bool isGizmo = false;
            BlueprintObject closest = null;
            GetClosestObject(ray,out closest, out isGizmo);

            if(closest!=null)
                ScreenNotification.ShowNotification($"Selected Model = {closest.ModelKey}");

            if (closest != null && isGizmo == false)
            {
                if (multi)
                {
                    if (closest.Selected)
                    {
                        SelectedObjects.Remove(closest);
                        closest.Selected = false;
                    }
                    else
                    {
                        SelectObject(closest, true);
                    }
                }
                else
                {
                    if (closest.Selected)
                    {
                        ClearSelection();
                    }
                    else
                    {
                        SelectObject(closest, false);
                    }
                }
            }
            else if (closest != null && isGizmo)
            {
                // Gizmo Drag
                _gizmoActive = true;
                activeGizmo = closest;
                _startMousePos = e.MousePosition;
                switch (currentMode)
                {
                    case RendererControl.TransformMode.Translate:
                        GameService.Input.Mouse.MouseMoved += GizmoTranslate;
                        break;
                    case RendererControl.TransformMode.Rotate:
                        GameService.Input.Mouse.MouseMoved += GizmoRotate;
                        break;
                    case RendererControl.TransformMode.Scale:
                        GameService.Input.Mouse.MouseMoved += GizmoTranslate;
                        break;
                }
                GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;
                return;
            }
        }


        private BlueprintObject GetClosestObject(Ray ray, out BlueprintObject closest, out bool isGizmo)
        {
            closest = null;
            isGizmo = false;
            float minDist = float.MaxValue;

            if(SelectedObjects.Count > 0)
            {
                switch (gizmoMode)
                {
                    case GizmoMode.Translate:
                        foreach (var obj in TranslateGizmos)
                        {
                            var dist = ray.Intersects(obj.BoundingBox);
                            if (dist.HasValue && dist.Value < minDist)
                            {
                                closest = obj;
                                minDist = dist.Value;
                            }
                        }
                        break;

                    case GizmoMode.Rotate:
                        foreach (var obj in RotateGizmos)
                        {
                            var dist = ray.Intersects(obj.BoundingBox);
                            if (dist.HasValue && dist.Value < minDist)
                            {
                                closest = obj;
                                minDist = dist.Value;
                            }
                        }
                        break;

                    case GizmoMode.Scale:
                        foreach (var obj in ScaleGizmos)
                        {
                            var dist = ray.Intersects(obj.BoundingBox);
                            if (dist.HasValue && dist.Value < minDist)
                            {
                                closest = obj; 
                                 minDist = dist.Value;
                            }
                        }
                        break;
                }

                if (closest != null)
                {
                    isGizmo = true;
                    return closest;
                }

            }
            

            foreach (var obj in Objects)
            {
                var dist = ray.Intersects(obj.BoundingBox);
                if (dist.HasValue && dist.Value < minDist)
                {
                    closest = obj;
                    minDist = dist.Value;
                }
            }

            isGizmo = false;
            return closest;
        }


        private Ray CreateRayFromMouse()
        {
            var mouse = GameService.Input.Mouse.Position; // absolute Mausposition
            var vp = _renderer.GraphicsDevice.Viewport;

            // Maus in Viewport-Koordinaten umrechnen
            float x = (mouse.X / (float)GameService.Graphics.SpriteScreen.Width) * vp.Width;
            float y = (mouse.Y / (float)GameService.Graphics.SpriteScreen.Height) * vp.Height;
            var mousePos = new Vector2(x, y);

            // Near / Far Punkte unprojecten
            Vector3 near = vp.Unproject(new Vector3(mousePos, 0f),
                                         GameService.Gw2Mumble.PlayerCamera.Projection,
                                         GameService.Gw2Mumble.PlayerCamera.View,
                                         Matrix.Identity);

            Vector3 far = vp.Unproject(new Vector3(mousePos, 1f),
                                        GameService.Gw2Mumble.PlayerCamera.Projection,
                                        GameService.Gw2Mumble.PlayerCamera.View,
                                        Matrix.Identity);

            Vector3 dir = Vector3.Normalize(far - near);

            return new Ray(near, dir);
        }





        private void GizmoTranslate(object sender, MouseEventArgs e)
        {
            if (!_gizmoActive)
                return;
            if (activeGizmo==null)
                return;
            // Funktion "bearbeitung beenden" hinzufüge -> setzt alle variablen zurück. Returns berücksichtigen, die müssen auch alles zurücksetzen


            var ray = CreateRayFromMouse();
            Vector3 gizmoOrigin = SelectedObjects.First().Position;

            // Achsenrichtung bestimmen
            Vector3 axisDir = Vector3.UnitX;
            if (activeGizmo.ModelKey.Contains("Y")) axisDir = Vector3.UnitY;
            if (activeGizmo.ModelKey.Contains("Z")) axisDir = Vector3.UnitZ;

            if (_rotationSpace == RendererControl.RotationSpace.Local)
                axisDir = Vector3.Transform(axisDir, getPivotRotation());

            // 🔹 Projektion des Maus-Rays auf die Gizmo-Achse
            Vector3 projectedPoint = ClosestPointBetweenRayAndLine(ray, gizmoOrigin, axisDir);

            if (!_dragInitialized)
            {
                _lastProjectedPoint = projectedPoint;
                _dragInitialized = true;
                return;
            }

            Vector3 delta = projectedPoint - _lastProjectedPoint;
            _lastProjectedPoint = projectedPoint;

            // 🔹 Offset nur entlang der Achse
            float movement = Vector3.Dot(delta, axisDir);
            Vector3 offset = axisDir * movement;

            foreach (var obj in SelectedObjects)
            {
                obj.Position += offset;
            }

            _renderer.PrecomputeWorlds(SelectedObjects);
            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftGizmoConfirm;
        }

        private Vector3 ClosestPointBetweenRayAndLine(Ray ray, Vector3 lineOrigin, Vector3 lineDir)
        {
            lineDir.Normalize();
            Vector3 diff = ray.Position - lineOrigin;

            float a = Vector3.Dot(ray.Direction, ray.Direction);
            float b = Vector3.Dot(ray.Direction, lineDir);
            float c = Vector3.Dot(lineDir, lineDir);
            float d = Vector3.Dot(ray.Direction, diff);
            float e = Vector3.Dot(lineDir, diff);

            float denom = a * c - b * b;
            if (Math.Abs(denom) < 1e-6f)
                return lineOrigin; // Parallel – kein Schnittpunkt

            float t = (b * e - c * d) / denom;
            float s = (a * e - b * d) / denom;

            Vector3 rayPoint = ray.Position + ray.Direction * t;
            Vector3 linePoint = lineOrigin + lineDir * s;

            return linePoint; // oder rayPoint – beide sind nahe beieinander
        }



        private void GizmoRotate(object sender, MouseEventArgs e)
        {
            if (!_gizmoActive)
                return;
            if (activeGizmo == null)
                return;
            // Funktion "bearbeitung beenden" hinzufüge -> setzt alle variablen zurück. Returns berücksichtigen, die müssen auch alles zurücksetzen

            var ray = CreateRayFromMouse();
            Vector3 pivot = pivotObject;

            // 🔸 Achsenrichtung bestimmen
            Vector3 axisDir = Vector3.UnitX;
            if (activeGizmo.ModelKey.Contains("Y")) axisDir = Vector3.UnitY;
            if (activeGizmo.ModelKey.Contains("Z")) axisDir = Vector3.UnitZ;

            if (_rotationSpace == RendererControl.RotationSpace.Local)
                axisDir = Vector3.Transform(axisDir, getPivotRotation());

            // 🔹 Ebene der Rotation (senkrecht zur Achse)
            Plane rotationPlane = new Plane(axisDir, -Vector3.Dot(axisDir, pivot));

            // 🔹 Schnittpunkt zwischen Maus-Ray und Rotations-Ebene
            if (!RayIntersectsPlane(ray, rotationPlane, out Vector3 hitPoint))
                return;

            Vector3 dir = Vector3.Normalize(hitPoint - pivot);

            if (!_rotationInitialized)
            {
                _rotationStartVec = dir;
                _rotationInitialized = true;
                return;
            }

            // 🔹 Winkel zwischen Start- und aktuellem Vektor
            float angle = (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(_rotationStartVec, dir), -1f, 1f));

            // 🔹 Richtung bestimmen (links/rechts)
            Vector3 cross = Vector3.Cross(_rotationStartVec, dir);
            float sign = Math.Sign(Vector3.Dot(cross, axisDir));
            angle *= sign;

            // 🔹 Empfindlichkeit
            const float sensitivity = 1.5f;
            angle *= sensitivity;

            Quaternion deltaRot = Quaternion.CreateFromAxisAngle(axisDir, angle);

            foreach (var obj in SelectedObjects)
            {
                if (!_startPositions.ContainsKey(obj))
                    _startPositions[obj] = obj.Position;
                if (!_startRotations.ContainsKey(obj))
                    _startRotations[obj] = obj.RotationQuaternion;

                var pivotOffset = _startPositions[obj] - pivot;
                var rotatedOffset = Vector3.Transform(pivotOffset, deltaRot);
                obj.Position = pivot + rotatedOffset;

                obj.RotationQuaternion = deltaRot * _startRotations[obj];
            }

            _renderer.PrecomputeWorlds(SelectedObjects);

            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftGizmoConfirm;
        }


        private bool RayIntersectsPlane(Ray ray, Plane plane, out Vector3 intersection)
        {
            float denom = Vector3.Dot(plane.Normal, ray.Direction);
            if (Math.Abs(denom) < 1e-6f)
            {
                intersection = Vector3.Zero;
                return false;
            }

            float t = -(Vector3.Dot(plane.Normal, ray.Position) + plane.D) / denom;
            intersection = ray.Position + ray.Direction * t;
            return t >= 0;
        }




        private void OnLeftGizmoConfirm(object sender, MouseEventArgs e)
        {
            // Confirm Gizmo Drag
            if (_gizmoActive)
            {
                _gizmoActive = false; 
                _dragInitialized = false;
                _rotationInitialized = false;
                activeGizmo = null;
                GameService.Input.Mouse.MouseMoved -= GizmoTranslate;
                GameService.Input.Mouse.MouseMoved -= GizmoRotate;

                _startRotations.Clear();
                _startPositions.Clear();
                foreach (var obj in SelectedObjects)
                {
                    _startRotations[obj] = obj.RotationQuaternion;
                    _startPositions[obj] = obj.Position;
                }

                if(_rotationSpace == RotationSpace.World)
                {
                    setPivotRotation(Quaternion.Identity);
                    updateGizmos();
                }

                GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
                GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftGizmoConfirm;
                return;
            }

        }

        private void OnGizmoCancel(object sender, MouseEventArgs e)
        {
            // Muss noch eingebaut werden
            ScreenNotification.ShowNotification("Gizmo-Aktion Abgebrochen.");
            // Cancel Gizmo Drag
            if (_gizmoActive)
            {
                _gizmoActive = false;
                GameService.Input.Mouse.MouseMoved -= GizmoTranslate;

                GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
                GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftGizmoConfirm;
                return;
            }
        }



    }
}
