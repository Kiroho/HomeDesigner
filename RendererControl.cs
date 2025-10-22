using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Controls;
using System.Collections.Generic;

namespace HomeDesigner
{
    public class RendererControl : Control
    {
        private readonly BlueprintRenderer _renderer;
        public List<BlueprintObject> Objects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> SelectedObjects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> GizmoObjects { get; } = new List<BlueprintObject>();
        public List<BlueprintObject> CopiedObjects { get; } = new List<BlueprintObject>();

        private Vector3 pivotObject = Vector3.Zero;
        private Quaternion pivotRotation = Quaternion.Identity;
        public enum RotationSpace { World, Local }
        public RotationSpace _rotationSpace = RotationSpace.World;


        public RendererControl(BlueprintRenderer renderer)
        {
            _renderer = renderer;
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
            Visible = true;
        }


        public void updateWorld()
        {
            _renderer.PrecomputeWorlds(Objects); // Weltmatrizen einmal vorberechnen
        }

        public void updateGizmo()
        {
            _renderer.PrecomputeGizmoWorlds(GizmoObjects);
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


        public void AddGizmoObject(BlueprintObject obj)
        {
            GizmoObjects.Add(obj);
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
            pivotRotation = SelectedObjects[0].RotationQuaternion;
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

                foreach (var ob in GizmoObjects)
                {
                    ob.Position = pivotObject;
                    ob.RotationQuaternion = pivotRotation;
                }
                _renderer.PrecomputeGizmoWorlds(GizmoObjects);
                _renderer.DrawGizmo(view, projection, GizmoObjects);
            }
        }
    }
}
