using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Controls;
using System.Collections.Generic;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System;
using Blish_HUD.Graphics.UI;
using HomeDesigner.Views;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using System.IO;
using System.Diagnostics;

namespace HomeDesigner
{
    public class RendererControl : Control
    {
        private readonly BlueprintRenderer _blueprintRenderer;
        public List<BlueprintObject> Objects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> SelectedObjects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> BackupObjects { get; } = new List<BlueprintObject>();
        public int internalObjectId = 1;
        public Dictionary<int, List<BlueprintObject>> HistoryList { get; } = new Dictionary<int, List<BlueprintObject>>();
        public int historyPosition = 0;
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
        private BlueprintObject activeGizmo= null;

        private Dictionary<BlueprintObject, Vector3> _startPositions = new Dictionary<BlueprintObject, Vector3>();
        private bool _dragInitialized = false;
        private Vector3 _lastProjectedPoint;
        private Dictionary<BlueprintObject, Quaternion> _startRotations = new Dictionary<BlueprintObject, Quaternion>();
        private Vector3 _rotationStartVec;

        // Snapped rotate
        private float _snapAccum = 0f;   // akkumulierte Mausrotation während Shift
        private const float SnapStepSize = (float)(Math.PI / 4f); // 45° in Radiant
        private const float SnapThreshold = 0.7f;        // Deadzone / Härte
        private bool _prevShift = false;



        #region --- Multi Selection Tools ---
        public enum SelectionMode { None, RectangleStart, RectangleEnd, RectangleHeight, PolygonPoints, PolygonHeight }


        public SelectionMode _selectionMode = SelectionMode.None;
        private Vector3 _rectStart;
        private Vector3 _rectEnd;

        // Höhe -> Area:
        private float _areaHeight;
        private float _dragStartMouseY;


        private List<Vector3> _polygonPoints = new List<Vector3>();
        private bool _isSelectingPolygon = false;
        public bool IsSelectingPolygon => _isSelectingPolygon;

        private BasicEffect _selectionEffect;

        public float planeZ = 0.000000f;
        public bool multiLassoSelect = false;
        #endregion


        public RendererControl(BlueprintRenderer renderer)
        {
            _blueprintRenderer = renderer;
            Parent = GameService.Graphics.SpriteScreen;
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
            Visible = true;
            ZIndex = -1000;

            if (GameService.Gw2Mumble.CurrentMap.Id == 1596) //Heartglow
            {
                planeZ = 1f;
            }
            else if (GameService.Gw2Mumble.CurrentMap.Id == 1558) //Comosus
            {
                planeZ = 15f;
            }
            else
            {
                planeZ = 0.000000f;
            }


            // Maus-Handler registrieren
            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;

            // Resize
            GameService.Graphics.SpriteScreen.Resized += (s, e) =>
            {
                Width = GameService.Graphics.SpriteScreen.Width;
                Height = GameService.Graphics.SpriteScreen.Height;
            };

            // Selection Effect
            _selectionEffect = new BasicEffect(_blueprintRenderer.GraphicsDevice)
            {
                VertexColorEnabled = true
            };
            

        }
        protected override CaptureType CapturesInput() => CaptureType.DoNotBlock;


        public BlueprintRenderer getBlueprintrenderer()
        {
            return _blueprintRenderer;
        }

        public void updateWorld()
        {
            _blueprintRenderer.PrecomputeWorlds(Objects);
        }

        public void updateGizmos()
        {
            _blueprintRenderer.PrecomputeGizmoWorlds(TranslateGizmos);
            _blueprintRenderer.PrecomputeGizmoWorlds(RotateGizmos);
            _blueprintRenderer.PrecomputeGizmoWorlds(ScaleGizmos);
        }

        public void AddObject(BlueprintObject obj)
        {
            Objects.Add(obj);
            _blueprintRenderer.PrecomputeWorlds(Objects);
        }

        public void RemoveObject(BlueprintObject obj)
        {
            Objects.Remove(obj);
            _blueprintRenderer.PrecomputeWorlds(Objects);
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
            updateBackupObjects();
        }

        public void SelectObject(BlueprintObject obj, bool multiSelect = false)
        {
            if (!multiSelect) ClearSelection();
            obj.Selected = true;
            SelectedObjects.Add(obj);
            if (_rotationSpace == RotationSpace.Local)
            {
                pivotRotation = obj.RotationQuaternion;
            }
            else
            {
                pivotRotation = Quaternion.Identity;
            }
            updateGizmos();
            updateBackupObjects();
        }

        private void updateBackupObjects()
        {
            BackupObjects.Clear();
            foreach (var obj in SelectedObjects)
            {
                BackupObjects.Add(new BlueprintObject()
                {
                    ModelKey = obj.ModelKey,
                    Id = obj.Id,
                    Name = obj.Name,
                    Position = obj.Position,
                    Rotation = obj.Rotation,
                    RotationQuaternion = obj.RotationQuaternion,
                    Scale = obj.Scale,
                    CachedWorld = obj.CachedWorld,
                    InternalId = obj.InternalId,
                    IsOriginal = false
                }
                    );
            }
        }


        public void loadHistory()
        {
            Objects.Clear();
            foreach(var obj in HistoryList[historyPosition])
            {
                Objects.Add(
                    new BlueprintObject()
                    {
                        ModelKey = obj.ModelKey,
                        Id = obj.Id,
                        Name = obj.Name,
                        Position = obj.Position,
                        Rotation = obj.Rotation,
                        RotationQuaternion = obj.RotationQuaternion,
                        Scale = obj.Scale,
                        CachedWorld = obj.CachedWorld,
                        InternalId = obj.InternalId,
                        IsOriginal = obj.IsOriginal,
                        Selected = obj.Selected,
                        BoundingBox = obj.BoundingBox
                    }
                );
            }
        }

        public void resetHistory()
        {

            HistoryList.Clear();
            historyPosition = 0;
        }
        public void updateHistoryList()
        {
            var histList = new List<BlueprintObject>();
            foreach ( var obj in Objects)
            {
                histList.Add(
                    new BlueprintObject()
                    {
                        ModelKey = obj.ModelKey,
                        Id = obj.Id,
                        Name = obj.Name,
                        Position = obj.Position,
                        Rotation = obj.Rotation,
                        RotationQuaternion = obj.RotationQuaternion,
                        Scale = obj.Scale,
                        CachedWorld = obj.CachedWorld,
                        InternalId = obj.InternalId,
                        IsOriginal = obj.IsOriginal,
                        Selected = obj.Selected,
                        BoundingBox = obj.BoundingBox
                    }
                );
            }

            foreach (var key in HistoryList.Keys.Where(k => k > historyPosition).ToList())
            {
                HistoryList.Remove(key);
            }


            historyPosition++;
            HistoryList.Add(historyPosition, histList);
        }


        public Vector3 getPivotObject()
        {
            return pivotObject;
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
            if (!_blueprintRenderer._modelPivots.TryGetValue(SelectedObjects[0].ModelKey, out var pivotLocal))
                pivotLocal = Vector3.Zero;

            // Pivot in die Welt transformieren
            pivotObject =  Vector3.Transform(pivotLocal, SelectedObjects[0].CachedWorld);
        }


        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var gd = _blueprintRenderer.GraphicsDevice;

            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullNone;

            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            _blueprintRenderer.Draw(view, projection, Objects);

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

                if(BackupObjects.Count > 0)
                {
                    _blueprintRenderer.Draw(view, projection, BackupObjects);
                }

            }



            // Multi Selection
            if (_selectionEffect == null)
                InitializeSelectionEffect(_blueprintRenderer.GraphicsDevice);

            if (_selectionMode == SelectionMode.RectangleStart || _selectionMode == SelectionMode.RectangleEnd || _selectionMode == SelectionMode.RectangleHeight)
            {
                var mouse = InputService.Input.Mouse.Position;
                spriteBatch.Draw(
                    _blueprintRenderer.contentManager.GetTexture("Icons/Mouse_Rectangle.png"),
                    new Rectangle(mouse.X + 32, mouse.Y + 32, 32, 32),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f //zindex
                );
            }
            else if(_selectionMode == SelectionMode.PolygonPoints || _selectionMode == SelectionMode.PolygonHeight)
            {
                var mouse = GameService.Input.Mouse.Position;
                spriteBatch.Draw(
                    _blueprintRenderer.contentManager.GetTexture("Icons/Mouse_Lasso.png"),
                    new Rectangle(mouse.X + 32, mouse.Y + 32, 32, 32),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f //zindex
                );
            }

            if (_selectionMode == SelectionMode.RectangleEnd)
            {
                var ray = CreateRayFromMouse();
                if (RaycastGround(ray, out Vector3 current))
                    DrawSelectionRectangle(_rectStart, current, Color.LimeGreen);
            }
            else if (_selectionMode == SelectionMode.RectangleHeight)
            {
                setAreaHeight();
                DrawCuboidPreview(_rectStart, _rectEnd, _areaHeight);
            }
            else if (_selectionMode == SelectionMode.PolygonPoints)
            {
                if (_polygonPoints.Count > 0)
                {
                    DrawPolygon(_polygonPoints);
                }
            }
            else if (_selectionMode == SelectionMode.PolygonHeight)
            {
                setAreaHeight();
                DrawPolygonPrismPreview(_polygonPoints, _areaHeight);
            }





        }

        private void paintGizmo(Matrix view, Matrix projection,List<BlueprintObject> gizmolist)
        {
            foreach (var ob in gizmolist)
            {
                ob.Position = pivotObject;
                ob.RotationQuaternion = pivotRotation;
            }
            _blueprintRenderer.PrecomputeGizmoWorlds(gizmolist);
            _blueprintRenderer.DrawGizmo(view, projection, gizmolist, activeGizmo);
        }



















        private void OnLeftMouseButtonPressed(object sender, MouseEventArgs e)
        {
            if (Control.ActiveControl != this)
            {
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

            //if(closest!=null)
            //    ScreenNotification.ShowNotification($"Selected Model = {closest.ModelKey}");

            if (closest != null && isGizmo == false)
            {
                if (multi)
                {
                    if (closest.Selected)
                    {
                        SelectedObjects.Remove(closest);
                        updateBackupObjects();
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
                switch (currentMode)
                {
                    case RendererControl.TransformMode.Translate:
                        GameService.Input.Mouse.MouseMoved += GizmoTranslate;
                        break;
                    case RendererControl.TransformMode.Rotate:
                        GameService.Input.Mouse.MouseMoved += GizmoRotate;
                        break;
                    case RendererControl.TransformMode.Scale:
                        GameService.Input.Mouse.MouseMoved += GizmoScale;
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
            var mouse = GameService.Input.Mouse.Position;
            var vp = _blueprintRenderer.GraphicsDevice.Viewport;

            // Mouse to viewport coordinates
            float x = (mouse.X / (float)GameService.Graphics.SpriteScreen.Width) * vp.Width;
            float y = (mouse.Y / (float)GameService.Graphics.SpriteScreen.Height) * vp.Height;
            var mousePos = new Vector2(x, y);

            // Near / Far points unprojection
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

            Vector3 axisDir = Vector3.UnitX;
            if (activeGizmo.ModelKey.Contains("Y")) axisDir = Vector3.UnitY;
            if (activeGizmo.ModelKey.Contains("Z")) axisDir = Vector3.UnitZ;

            if (_rotationSpace == RendererControl.RotationSpace.Local)
                axisDir = Vector3.Transform(axisDir, getPivotRotation());

            // Projekting Mouse Rays on Gizmo Axis
            Vector3 projectedPoint = ClosestPointBetweenRayAndLine(ray, gizmoOrigin, axisDir);

            if (!_dragInitialized)
            {
                _lastProjectedPoint = projectedPoint;
                _dragInitialized = true;
                return;
            }

            Vector3 delta = projectedPoint - _lastProjectedPoint;
            _lastProjectedPoint = projectedPoint;

            // Offset only for respective Axis
            float movement = Vector3.Dot(delta, axisDir);
            Vector3 offset = axisDir * movement;

            foreach (var obj in SelectedObjects)
            {
                obj.Position += offset;
            }

            _blueprintRenderer.PrecomputeWorlds(SelectedObjects);
            GameService.Input.Mouse.LeftMouseButtonPressed += OnGizmoConfirm;
            GameService.Input.Keyboard.KeyPressed += OnGizmoKeyPress;
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
            if (!_gizmoActive || activeGizmo == null)
                return;

            var ray = CreateRayFromMouse();
            Vector3 pivot = pivotObject;

            Vector3 axisDir = Vector3.UnitX;
            if (activeGizmo.ModelKey.Contains("Y")) axisDir = Vector3.UnitY;
            if (activeGizmo.ModelKey.Contains("Z")) axisDir = Vector3.UnitZ;

            if (_rotationSpace == RendererControl.RotationSpace.Local)
                axisDir = Vector3.Transform(axisDir, getPivotRotation());

            Plane rotationPlane = new Plane(axisDir, -Vector3.Dot(axisDir, pivot));
            if (!RayIntersectsPlane(ray, rotationPlane, out Vector3 hitPoint))
                return;

            Vector3 dir = Vector3.Normalize(hitPoint - pivot);

            bool shift = GameService.Input.Keyboard.State.IsKeyDown(Keys.LeftShift);

            // ---------------- DRAG START ----------------
            if (!_dragInitialized)
            {
                _rotationStartVec = dir;
                _snapAccum = 0f;
                _prevShift = shift;

                _dragInitialized = true;
                return;
            }

            // ---------------- SHIFT -> PRESSED TRANSITION ----------------
            if (shift && !_prevShift)
            {
                // SHIFT gerade neu gedrückt → Basis sauber setzen
                _rotationStartVec = dir;
                _snapAccum = 0f;

                foreach (var obj in SelectedObjects)
                {
                    _startRotations[obj] = obj.RotationQuaternion;
                    _startPositions[obj] = obj.Position;
                }
            }

            // ---------------- SHIFT -> RELEASED TRANSITION ----------------
            if (!shift && _prevShift)
            {
                // SHIFT wurde gerade losgelassen → Basis für freie Rotation setzen
                _rotationStartVec = dir;
                _snapAccum = 0f;

                foreach (var obj in SelectedObjects)
                {
                    _startRotations[obj] = obj.RotationQuaternion;
                    _startPositions[obj] = obj.Position;
                }
            }

            _prevShift = shift;

            // ---------------- WINKELBERECHNUNG ----------------
            float angle = (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(_rotationStartVec, dir), -1f, 1f));

            Vector3 cross = Vector3.Cross(_rotationStartVec, dir);
            float sign = Math.Sign(Vector3.Dot(cross, axisDir));
            angle *= sign;

            const float sensitivity = 1.5f;
            angle *= sensitivity;

            float finalAngle = angle;

            // ---------------- SNAPPING ----------------
            if (shift)
            {
                _snapAccum += angle;

                if (Math.Abs(_snapAccum) >= SnapThreshold)
                {
                    int steps = (int)(_snapAccum / SnapThreshold);
                    int direction = Math.Sign(_snapAccum);

                    finalAngle = direction * SnapStepSize;

                    _snapAccum = 0f;

                    _rotationStartVec = dir;

                    foreach (var obj in SelectedObjects)
                    {
                        _startRotations[obj] = obj.RotationQuaternion;
                        _startPositions[obj] = obj.Position;
                    }
                }
                else
                {
                    // Unter Threshold → keine Rotation, aber Startvektor updaten
                    _rotationStartVec = dir;
                    return;
                }
            }
            else
            {
                // Freie Rotation
                _snapAccum = 0f;
            }

            // ---------------- APPLY ROTATION ----------------
            Quaternion deltaRot = Quaternion.CreateFromAxisAngle(axisDir, finalAngle);

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

            _blueprintRenderer.PrecomputeWorlds(SelectedObjects);

            GameService.Input.Mouse.LeftMouseButtonPressed += OnGizmoConfirm;
            GameService.Input.Keyboard.KeyPressed += OnGizmoKeyPress;
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


        private void GizmoScale(object sender, MouseEventArgs e)
        {
            if (!_gizmoActive)
                return;

            var ray = CreateRayFromMouse();
            Vector3 gizmoOrigin = SelectedObjects.First().Position;

            // 🔹 Achsenrichtung bestimmen
            Vector3 axisDir = Vector3.UnitX;
            if (activeGizmo.ModelKey.Contains("Y")) axisDir = Vector3.UnitY;
            if (activeGizmo.ModelKey.Contains("Z")) axisDir = Vector3.UnitZ;

            // 🔹 Mausbewegung auf Achse projizieren
            Vector3 projectedPoint = ClosestPointBetweenRayAndLine(ray, gizmoOrigin, axisDir);

            if (!_dragInitialized)
            {
                _lastProjectedPoint = projectedPoint;
                _dragInitialized = true;
                return;
            }

            // 🔹 Bewegung entlang der Achse berechnen
            Vector3 delta = projectedPoint - _lastProjectedPoint;
            _lastProjectedPoint = projectedPoint;

            float movement = Vector3.Dot(delta, axisDir);

            // 🔹 Empfindlichkeit der Skalierung
            const float sensitivity = 1.2f; // kleiner Wert = feinfühliger

            // 🔹 Skalierungsfaktor berechnen
            float scaleFactor = 1f + movement * sensitivity;

            // 🔹 Zu starke Sprünge abfangen
            scaleFactor = MathHelper.Clamp(scaleFactor, 0.5f, 1.5f);

            foreach (var obj in SelectedObjects)
            {
                float newScale = obj.Scale * scaleFactor;

                // 🔹 Globales Minimum und Maximum (wie gewünscht)
                newScale = MathHelper.Clamp(newScale, 0.1f, 2f);

                obj.Scale = newScale;
            }

            _blueprintRenderer.PrecomputeWorlds(SelectedObjects);

            GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;
            GameService.Input.Mouse.LeftMouseButtonPressed += OnGizmoConfirm;
            GameService.Input.Keyboard.KeyPressed += OnGizmoKeyPress;
        }



        private void OnGizmoConfirm(object sender, MouseEventArgs e)
        {
            // Confirm Gizmo Drag
            if (_gizmoActive)
            {
                _gizmoActive = false; 
                _dragInitialized = false;
                activeGizmo = null;
                GameService.Input.Mouse.MouseMoved -= GizmoTranslate;
                GameService.Input.Mouse.MouseMoved -= GizmoRotate;
                GameService.Input.Mouse.MouseMoved -= GizmoScale;
                GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
                GameService.Input.Mouse.LeftMouseButtonPressed -= OnGizmoConfirm;
                GameService.Input.Keyboard.KeyPressed -= OnGizmoKeyPress;

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
                }
                else
                {
                    setPivotRotation(SelectedObjects[0].RotationQuaternion);
                }
                updateGizmos();
                updateHistoryList();
                updateBackupObjects();

            }

        }

        private void OnGizmoKeyPress(object sender, KeyboardEventArgs e)
        {
            // Cancel Action
            if(e.Key == Keys.T)
            {
                // Cancel Gizmo Drag
                if (_gizmoActive)
                {
                    _gizmoActive = false;
                    _dragInitialized = false;
                    activeGizmo = null;
                    GameService.Input.Mouse.MouseMoved -= GizmoTranslate;
                    GameService.Input.Mouse.MouseMoved -= GizmoRotate;
                    GameService.Input.Mouse.MouseMoved -= GizmoScale;
                    GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
                    GameService.Input.Mouse.LeftMouseButtonPressed -= OnGizmoConfirm;
                    GameService.Input.Keyboard.KeyPressed -= OnGizmoKeyPress;

                    _startRotations.Clear();
                    _startPositions.Clear();

                    if (_rotationSpace == RotationSpace.World)
                    {
                        setPivotRotation(Quaternion.Identity);
                        updateGizmos();
                    }


                    // Setting back values from current edit
                    var map = SelectedObjects.ToDictionary(o => o.InternalId);

                    foreach (var backup in BackupObjects)
                    {
                        if (map.TryGetValue(backup.InternalId, out var original))
                        {
                            original.ModelKey = backup.ModelKey;
                            original.Id = backup.Id;
                            original.Name = backup.Name;
                            original.Position = backup.Position;
                            original.Rotation = backup.Rotation;
                            original.RotationQuaternion = backup.RotationQuaternion;
                            original.Scale = backup.Scale;
                            original.CachedWorld = backup.CachedWorld;
                            original.InternalId = backup.InternalId;
                            original.IsOriginal = true;
                        }
                    }

                }

            }
            else if (e.Key == Keys.D7)
            {
                // Copy Gizmo Drag
                placeCopy();

            }

        }



        public void placeCopy()
        {
            // Copy Gizmo Drag
            if (_gizmoActive)
            {
                _gizmoActive = false;
                _dragInitialized = false;
                activeGizmo = null;
                GameService.Input.Mouse.MouseMoved -= GizmoTranslate;
                GameService.Input.Mouse.MouseMoved -= GizmoRotate;
                GameService.Input.Mouse.MouseMoved -= GizmoScale;
                GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
                GameService.Input.Mouse.LeftMouseButtonPressed -= OnGizmoConfirm;
                GameService.Input.Keyboard.KeyPressed -= OnGizmoKeyPress;

                _startRotations.Clear();
                _startPositions.Clear();

                if (_rotationSpace == RotationSpace.World)
                {
                    setPivotRotation(Quaternion.Identity);
                }

                foreach (var obj in BackupObjects)
                {
                    Objects.Add(
                        new BlueprintObject()
                        {
                            ModelKey = obj.ModelKey,
                            Id = obj.Id,
                            Name = obj.Name,
                            Position = obj.Position,
                            Rotation = obj.Rotation,
                            RotationQuaternion = obj.RotationQuaternion,
                            Scale = obj.Scale,
                            CachedWorld = obj.CachedWorld,
                            InternalId = internalObjectId,
                            IsOriginal = true,
                            Selected = obj.Selected,
                            BoundingBox = obj.BoundingBox
                        }
                    );
                    internalObjectId++;
                }
                ClearSelection();
                updateGizmos();
                updateHistoryList();
                updateBackupObjects();
            }
        }





        #region --- Multi Selection Tools ---
        public void InitializeSelectionEffect(GraphicsDevice device)
        {
            _selectionEffect = new BasicEffect(device)
            {
                VertexColorEnabled = true
            };
        }

        // Wird von außen (DesignerView) aufgerufen:
        public void StartRectangleSelection()
        {
            ScreenNotification.ShowNotification("Rectangle selection started");
            _selectionMode = SelectionMode.RectangleStart;
            _polygonPoints.Clear();
            _isSelectingPolygon = false;
        }

        public void PolygonSelection()
        {
            if (_selectionMode == SelectionMode.None)
            {
                _polygonPoints.Clear();
                _selectionMode = SelectionMode.PolygonPoints;
                ScreenNotification.ShowNotification("Lasso selection started");
            }
            else if (_selectionMode == SelectionMode.PolygonPoints)
            {
                if (_polygonPoints == null || _polygonPoints.Count < 3)
                {
                    ScreenNotification.ShowNotification("Too few points for a selection");
                    _polygonPoints.Clear();
                    _selectionMode = SelectionMode.None;
                }
                else
                {
                    _dragStartMouseY = GameService.Input.Mouse.Position.Y;
                    _areaHeight = 0;
                    _selectionMode = SelectionMode.PolygonHeight;
                }
            }
        }

        public void CancelSelection()
        {
            _selectionMode = SelectionMode.None;
            _isSelectingPolygon = false;
            _polygonPoints.Clear();
        }

        // Wird vom DesignerView bei Mausclick aufgerufen:
        public void OnSelectionClick( DesignerView view)
        {
            if (_selectionMode == SelectionMode.None)
                return;
            if (GameService.Input.Mouse.ActiveControl != this) 
                return;

            var ray = CreateRayFromMouse();
            if (!RaycastGround(ray, out Vector3 hit))
                return;


            if (_selectionMode == SelectionMode.RectangleStart)
            {
                if (RaycastGround(ray, out _rectStart))
                {
                    _selectionMode = SelectionMode.RectangleEnd;
                    return;
                }
            }
            else if (_selectionMode == SelectionMode.RectangleEnd)
            {
                if (RaycastGround(ray, out _rectEnd))
                {
                    _areaHeight = 0;
                    _dragStartMouseY = GameService.Input.Mouse.Position.Y;

                    _selectionMode = SelectionMode.RectangleHeight;
                    return;
                }
            }
            else if (_selectionMode == SelectionMode.RectangleHeight)
            {
                // Höhe bestätigt → Volumen selektieren
                SelectObjectsInCuboid();
                _selectionMode = SelectionMode.None;
            }
            else if (_selectionMode == SelectionMode.PolygonPoints)
            {
                if (RaycastGround(ray, out Vector3 corner))
                {
                    _polygonPoints.Add(corner);
                    return;
                }
                
            }
            else if (_selectionMode == SelectionMode.PolygonHeight)
            {
                // Höhe bestätigt → Volumen selektieren
                //SelectObjectsInPolygonPrism(_polygonPoints, _polygonHeight);
                SelectObjectsInPolygon();
                _selectionMode = SelectionMode.None;
            }

        }


        /// <summary>
        /// Raycast auf eine horizontale Ebene auf Spielerhöhe (oder Y = playerY).
        /// Liefert den Trefferpunkt in world-space zurück.
        /// </summary>
        private bool RaycastGround(Ray ray, out Vector3 hit)
        {
            // Wenn Ray parallel zur Ebene ist, kein Treffer
            if (Math.Abs(ray.Direction.Y) < 1e-6f)
            {
                hit = Vector3.Zero;
                return false;
            }
            
            
            float t = (planeZ - ray.Position.Z) / ray.Direction.Z;
            if (t <= 0f)
            {
                hit = Vector3.Zero;
                return false;
            }

            hit = ray.Position + ray.Direction * t;
            return true;
        }


        private void setAreaHeight()
        {
            float dy = _dragStartMouseY - GameService.Input.Mouse.Position.Y;
            // Sensitivität einstellen
            float scale = 0.05f;
            _areaHeight = dy * scale;
        }


        private void DrawCuboidPreview(Vector3 start, Vector3 end, float height)
        {
            var gd = _blueprintRenderer.GraphicsDevice;

            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            // sortiere X
            float x1 = Math.Min(start.X, end.X);
            float x2 = Math.Max(start.X, end.X);

            // sortiere Y  (das ist dein Start-Z in der Fläche)
            float y1 = Math.Min(start.Y, end.Y);
            float y2 = Math.Max(start.Y, end.Y);

            float baseZ = start.Z;
            float topZ = baseZ + height;

            Vector3 b1 = new Vector3(x1, y1, baseZ);
            Vector3 b2 = new Vector3(x2, y1, baseZ);
            Vector3 b3 = new Vector3(x2, y2, baseZ);
            Vector3 b4 = new Vector3(x1, y2, baseZ);

            Vector3 t1 = new Vector3(x1, y1, topZ);
            Vector3 t2 = new Vector3(x2, y1, topZ);
            Vector3 t3 = new Vector3(x2, y2, topZ);
            Vector3 t4 = new Vector3(x1, y2, topZ);


            Color fill = new Color(0.2f, 0.6f, 1.0f, 0.25f);

            // Liste für Dreiecke
            List<VertexPositionColor> tris = new List<VertexPositionColor>();

            // Top
            AddQuad(tris, t1, t2, t3, t4, fill);

            // Bottom (optional)
            // AddQuad(tris, b1, b2, b3, b4, fill);

            // Sides
            AddQuad(tris, b1, b2, t2, t1, fill);
            AddQuad(tris, b2, b3, t3, t2, fill);
            AddQuad(tris, b3, b4, t4, t3, fill);
            AddQuad(tris, b4, b1, t1, t4, fill);


            // ---- BORDER VERTICES ----
            Color border = Color.LimeGreen;

            var borderVerts = new VertexPositionColor[]
            {
                // bottom loop
                new VertexPositionColor(b1, border),
                new VertexPositionColor(b2, border),
                new VertexPositionColor(b3, border),
                new VertexPositionColor(b4, border),
                new VertexPositionColor(b1, border),

                // connect top to bottom
                new VertexPositionColor(b1, border),
                new VertexPositionColor(t1, border),
                new VertexPositionColor(b2, border),
                new VertexPositionColor(t2, border),
                new VertexPositionColor(b3, border),
                new VertexPositionColor(t3, border),
                new VertexPositionColor(b4, border),
                new VertexPositionColor(t4, border),

                // top loop
                new VertexPositionColor(t1, border),
                new VertexPositionColor(t2, border),
                new VertexPositionColor(t3, border),
                new VertexPositionColor(t4, border),
                new VertexPositionColor(t1, border),
            };

            if (_selectionEffect == null)
                _selectionEffect = new BasicEffect(gd) { VertexColorEnabled = true };

            _selectionEffect.World = Matrix.Identity;
            _selectionEffect.View = view;
            _selectionEffect.Projection = projection;

            var oldRasterizer = gd.RasterizerState;

            gd.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None
            };

            foreach (var pass in _selectionEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawUserPrimitives(PrimitiveType.TriangleList, tris.ToArray(), 0, tris.Count / 3);
                gd.DrawUserPrimitives(PrimitiveType.LineStrip, borderVerts, 0, borderVerts.Length - 1);
            }
            gd.RasterizerState = oldRasterizer;
        }

        private void AddQuad(
            List<VertexPositionColor> v,
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            Color c)
        {
            v.Add(new VertexPositionColor(v1, c));
            v.Add(new VertexPositionColor(v2, c));
            v.Add(new VertexPositionColor(v3, c));

            v.Add(new VertexPositionColor(v1, c));
            v.Add(new VertexPositionColor(v3, c));
            v.Add(new VertexPositionColor(v4, c));
        }






        /// <summary>
        /// Wählt alle Objekte aus, deren Position (XZ) innerhalb des Kubus liegt.
        /// </summary>
        private void SelectObjectsInCuboid()
        {
            float minX = Math.Min(_rectStart.X, _rectEnd.X);
            float maxX = Math.Max(_rectStart.X, _rectEnd.X);
            float minZ = Math.Min(_rectStart.Y, _rectEnd.Y);
            float maxZ = Math.Max(_rectStart.Y, _rectEnd.Y);

            // Min/Max für Y (Höhe)
            float minY = Math.Min(planeZ, planeZ + _areaHeight);
            float maxY = Math.Max(planeZ, planeZ + _areaHeight);

            // Optional: entferne vorherige Auswahl
            if (!multiLassoSelect)
            {
                ClearSelection();
            }

            foreach (var obj in Objects)
            {
                if (multiLassoSelect && obj.Selected)
                    continue;

                var p = obj.Position;
                if (p.X >= minX && p.X <= maxX && p.Y >= minZ && p.Y <= maxZ && p.Z >= minY && p.Z <= maxY)
                {
                    SelectObject(obj, true); // multi-select
                }
            }

            ScreenNotification.ShowNotification($"{SelectedObjects.Count} Objects selected");
        }

        /// <summary>
        /// Bestätigt die Polygon-Auswahl: führt Point-in-Polygon-Test (XZ) für alle Objekte durch.
        /// </summary>
        private void SelectObjectsInPolygon()
        {
            if (_polygonPoints == null || _polygonPoints.Count < 3)
            {
                return;
            }

            // Projektionsliste (XZ)
            var poly2 = _polygonPoints.Select(p => new Vector2(p.X, p.Y)).ToList();


            float baseZ = _polygonPoints[0].Z;
            float topZ = baseZ + _areaHeight;

            float minZ = Math.Min(baseZ, topZ);
            float maxZ = Math.Max(baseZ, topZ);

            if (!multiLassoSelect)
            {
                ClearSelection();
            }

            foreach (var obj in Objects)
            {
                if (multiLassoSelect && obj.Selected)
                    continue;

                Vector3 p = obj.Position;
                // 1) Höhencheck
                if (p.Z < minZ || p.Z > maxZ)
                    continue;

                // 2) 2D-Polygon-Check (XY)
                var pos2 = new Vector2(obj.Position.X, obj.Position.Y);
                if (PointInPolygon(pos2, poly2))
                {
                    SelectObject(obj, true);
                }
            }

            ScreenNotification.ShowNotification($"{SelectedObjects.Count} Objects selected");

            // Aufräumen
            _isSelectingPolygon = false;
            _selectionMode = SelectionMode.None;
            _polygonPoints.Clear();
            //planeZ = 0.000000f;
        }

        /// <summary>
        /// Punkt-in-Polygon (Ray-casting / winding parity) - 2D
        /// </summary>
        private bool PointInPolygon(Vector2 pt, List<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];

                bool intersect = ((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                                 (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y + float.Epsilon) + pi.X);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// Zeichnet ein Rechteck (Rahmen + halbtransparente Füllung) zwischen two 3D-Punkten (auf derselben Y-Höhe).
        /// MonoGame-kompatible Implementierung (kein TriangleFan).
        /// </summary>
        private void DrawSelectionRectangle(Vector3 start, Vector3 end, Color borderColor)
        {
            var gd = _blueprintRenderer.GraphicsDevice;

            var oldRasterizer = gd.RasterizerState;
            gd.RasterizerState = new RasterizerState() { CullMode = CullMode.None };


            // Kamera (View/Projection) aus Mumble
            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            // Y-Ebene verwenden (wir nehmen die Y-Komponente des Startpunkts an)
            float z = start.Z;

            // 4 Ecken des Rechtecks (in Weltkoordinaten)
            float x1 = Math.Min(start.X, end.X);
            float x2 = Math.Max(start.X, end.X);
            float y1 = Math.Min(start.Y, end.Y);
            float y2 = Math.Max(start.Y, end.Y);

            Vector3 v1 = new Vector3(x1, y1, z);
            Vector3 v2 = new Vector3(x2, y1, z);
            Vector3 v3 = new Vector3(x2, y2, z);
            Vector3 v4 = new Vector3(x1, y2, z);

            // Füllfarbe (halbtransparent) — float RGBA Konstruktor funktioniert zuverlässig
            var fillColor = new Color(0.2f, 0.6f, 1.0f, 0.25f); // hellblau, alpha 0.25

            // ----- Fläche als 2 Dreiecke (TriangleList) -----
            var fillVerts = new VertexPositionColor[]
            {
                new VertexPositionColor(v1, fillColor),
                new VertexPositionColor(v2, fillColor),
                new VertexPositionColor(v3, fillColor),

                new VertexPositionColor(v1, fillColor),
                new VertexPositionColor(v3, fillColor),
                new VertexPositionColor(v4, fillColor)
            };

            // ----- Rahmen (LineStrip) -----
            var borderVerts = new VertexPositionColor[]
            {
                new VertexPositionColor(v1, borderColor),
                new VertexPositionColor(v2, borderColor),
                new VertexPositionColor(v3, borderColor),
                new VertexPositionColor(v4, borderColor),
                new VertexPositionColor(v1, borderColor) // schließen
            };

            // Verwende dein vorhandenes BasicEffect (_selectionEffect). Falls null, initialisieren.
            if (_selectionEffect == null)
            {
                _selectionEffect = new BasicEffect(gd) { VertexColorEnabled = true };
            }

            _selectionEffect.World = Matrix.Identity;
            _selectionEffect.View = view;
            _selectionEffect.Projection = projection;

            // Rendern
            foreach (var pass in _selectionEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                // Fläche
                gd.DrawUserPrimitives(PrimitiveType.TriangleList, fillVerts, 0, 2);

                // Rahmen
                gd.DrawUserPrimitives(PrimitiveType.LineStrip, borderVerts, 0, borderVerts.Length - 1);
            }
            gd.RasterizerState = oldRasterizer;
        }




        private void DrawPolygon(List<Vector3> points)
        {
            if (points.Count == 0) return;
            // Punkte markieren
            foreach (var p in points)
                DrawSelectionMarker(p + Vector3.Up * 0.2f, 0.2f, Color.Yellow);

            if (points.Count < 2) return;

            var gd = _blueprintRenderer.GraphicsDevice;
            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var proj = GameService.Gw2Mumble.PlayerCamera.Projection;

            // Linien zeichnen
            var verts = new List<VertexPositionColor>();
            for (int i = 0; i < points.Count; i++)
                verts.Add(new VertexPositionColor(points[i] + Vector3.Up * 0.05f, Color.Cyan));

            if (points.Count > 1)
                verts.Add(new VertexPositionColor(points[0] + Vector3.Up * 0.05f, Color.Cyan));

            _selectionEffect.World = Matrix.Identity;
            _selectionEffect.View = view;
            _selectionEffect.Projection = proj;

            foreach (var pass in _selectionEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawUserPrimitives(PrimitiveType.LineStrip, verts.ToArray(), 0, verts.Count - 1);
            }

            
        }


        private void DrawSelectionMarker(Vector3 pos, float size, Color color)
        {
            var gd = _blueprintRenderer.GraphicsDevice;

            var verts = new VertexPositionColor[]
            {
                new VertexPositionColor(pos + new Vector3(-size, 0, 0), color),
                new VertexPositionColor(pos + new Vector3(size, 0, 0), color),
                new VertexPositionColor(pos + new Vector3(0, 0, -size), color),
                new VertexPositionColor(pos + new Vector3(0, 0, size), color)
            };

            foreach (var pass in _selectionEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, 2);
            }
        }


        private void DrawPolygonPrismPreview(List<Vector3> points, float height)
        {
            if (points == null || points.Count < 3)
                return;

            var gd = _blueprintRenderer.GraphicsDevice;

            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            float baseZ = points[0].Z;
            float topZ = baseZ + height;

            Color fill = new Color(0.2f, 0.6f, 1.0f, 0.25f);
            Color border = Color.LimeGreen;

            // -------------------------
            // Bottom / Top Punkte
            // -------------------------
            List<Vector3> bottom = new List<Vector3>();
            List<Vector3> top = new List<Vector3>();

            for (int i = 0; i < points.Count; i++)
            {
                bottom.Add(new Vector3(points[i].X, points[i].Y, baseZ));
                top.Add(new Vector3(points[i].X, points[i].Y, topZ));
            }

            // -------------------------
            // TRIANGLES
            // -------------------------
            List<VertexPositionColor> tris = new List<VertexPositionColor>();

            // --- Top (Triangle Fan)
            Vector3 topCenter = Vector3.Zero;
            for (int i = 0; i < top.Count; i++)
                topCenter += top[i];
            topCenter /= top.Count;

            for (int i = 0; i < top.Count; i++)
            {
                int next = (i + 1) % top.Count;

                tris.Add(new VertexPositionColor(topCenter, fill));
                tris.Add(new VertexPositionColor(top[i], fill));
                tris.Add(new VertexPositionColor(top[next], fill));
            }

            // --- Seitenflächen
            for (int i = 0; i < bottom.Count; i++)
            {
                int next = (i + 1) % bottom.Count;

                AddQuad(
                    tris,
                    bottom[i],
                    bottom[next],
                    top[next],
                    top[i],
                    fill
                );
            }

            // -------------------------
            // BORDER LINES
            // -------------------------
            List<VertexPositionColor> borderVerts = new List<VertexPositionColor>();

            // Bottom loop
            for (int i = 0; i < bottom.Count; i++)
                borderVerts.Add(new VertexPositionColor(bottom[i], border));
            borderVerts.Add(new VertexPositionColor(bottom[0], border));

            // Vertical edges
            for (int i = 0; i < bottom.Count; i++)
            {
                borderVerts.Add(new VertexPositionColor(bottom[i], border));
                borderVerts.Add(new VertexPositionColor(top[i], border));
            }

            // Top loop
            for (int i = 0; i < top.Count; i++)
                borderVerts.Add(new VertexPositionColor(top[i], border));
            borderVerts.Add(new VertexPositionColor(top[0], border));

            // -------------------------
            // EFFECT / RENDER
            // -------------------------
            if (_selectionEffect == null)
                _selectionEffect = new BasicEffect(gd) { VertexColorEnabled = true };

            _selectionEffect.World = Matrix.Identity;
            _selectionEffect.View = view;
            _selectionEffect.Projection = projection;

            var oldRasterizer = gd.RasterizerState;
            gd.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None
            };

            foreach (var pass in _selectionEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                gd.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    tris.ToArray(),
                    0,
                    tris.Count / 3
                );

                gd.DrawUserPrimitives(
                    PrimitiveType.LineStrip,
                    borderVerts.ToArray(),
                    0,
                    borderVerts.Count - 1
                );
            }

            gd.RasterizerState = oldRasterizer;
        }








        #endregion


        public void unload()
        {
            GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;
            GameService.Input.Mouse.MouseMoved -= GizmoTranslate;
            GameService.Input.Mouse.MouseMoved -= GizmoRotate;
            GameService.Input.Mouse.MouseMoved -= GizmoScale;
            GameService.Input.Mouse.LeftMouseButtonPressed -= OnGizmoConfirm;
            GameService.Input.Keyboard.KeyPressed -= OnGizmoKeyPress;
        }



    }
}
