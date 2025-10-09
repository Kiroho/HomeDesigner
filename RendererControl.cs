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


        public RendererControl(BlueprintRenderer renderer)
        {
            _renderer = renderer;
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
            Visible = true;
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

        public void ClearSelection()
        {
            foreach (var obj in Objects)
                obj.Selected = false;
            _renderer.clearPivotObject();
        }

        public void SelectObject(BlueprintObject obj, bool multiSelect = false)
        {
            if (!multiSelect) ClearSelection(); // Wenn kein Multiselect, vorherige Auswahl löschen
            obj.Selected = true;
        }


        protected override void Paint(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Rectangle bounds)
        {
            var gd = _renderer.GraphicsDevice;

            gd.DepthStencilState = Microsoft.Xna.Framework.Graphics.DepthStencilState.Default;
            gd.RasterizerState = Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone;

            // Kamera aus Mumble
            var view = GameService.Gw2Mumble.PlayerCamera.View;
            var projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            // Alle Objekte zeichnen 
            _renderer.Draw(view, projection, Objects);

            // 🔹 Gizmo nur anzeigen, wenn genau ein Objekt selektiert ist
            if (SelectedObjects.Count > 0)
            {
                //System.Diagnostics.Debug.WriteLine(">>> Gizmo wird gezeichnet!");
                _renderer.DrawGizmo(SelectedObjects, view, projection);
            }
        }
    }
}
