using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Blish_HUD.Settings;
using System.Threading.Tasks;



namespace HomeDesigner
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private CornerIcon cornerIcon;
        private DesignerWindow designerWindow;

        private BlueprintRenderer _renderer;
        private RendererControl _rendererControl;
        private int _objectCount = 0;

        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        { 
        
        }

        // blish Load Async
        protected override async Task LoadAsync()
        {
            // Add a corner icon in the top left next to the other icons in guild wars 2 (e.g. inventory icon, Mail icon)
            cornerIcon = new CornerIcon()
            {
                Icon = ContentsManager.GetTexture("CornerIcon.png"),
                Priority = 61747774,
                Parent = GameService.Graphics.SpriteScreen,
                Visible = false
            };

            // On click listener for corner icon
            cornerIcon.Click += delegate
            {

                //ScreenNotification.ShowNotification("Icon gedrückt");
                designerWindow.ToggleWindow();
            };

            designerWindow = new DesignerWindow(this.ContentsManager, _rendererControl, _renderer);

            await Task.Delay(75);
            cornerIcon.Visible = true;
        }

        protected override void Initialize()
        {
            var gd = GameService.Graphics.GraphicsDeviceManager.GraphicsDevice; // Im Auge behalten!!!

            _renderer = new BlueprintRenderer(gd, ContentsManager);

            // Beispielmodell laden (Cube)
            _renderer.LoadModel("cube", "models/cube.obj", Vector3.Zero);
            _renderer.LoadModel("cube2", "models/cube.obj", Vector3.Zero);

            // Overlay-Control hinzufügen
            _rendererControl = new RendererControl(_renderer);
            GameService.Graphics.SpriteScreen.AddChild(_rendererControl);

            // Weltmatrizen einmal vorberechnen (jetzt mit List - kein Casting-Fehler mehr)
            _renderer.PrecomputeWorlds(_rendererControl.Objects);

            //Debug.WriteLine($"[Init] Player Pos = {GameService.Gw2Mumble.PlayerCharacter?.Position}");

            // Maus-Handler registrieren
            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
        }




        private void OnLeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
        {

            //ScreenNotification.ShowNotification($"[Init] Player Pos = {GameService.Gw2Mumble.PlayerCharacter?.Position}");
            
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
                // STRG + Klick → Objekt setzen
                //var newObj = new BlueprintObject
                //{
                //    ModelKey = "cube",
                //    Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                //    Rotation = new Vector3(MathHelper.PiOver2, 0f, 0f),
                //    Scale = 0.001f
                //};

                //_rendererControl.AddObject(newObj);
                //_objectCount++;


                // Notification anzeigen
                //ScreenNotification.ShowNotification($"Objekte gesetzt: {_objectCount}");
            }
            else if (altDown)
            {
                // Nur Klick → später Mehrfachauswahl Auswahl / andere Funktionen
                //ScreenNotification.ShowNotification($"Alt + Linksklick");
                RaycastSelect(true);
            }
            else
            {
                // Nur Klick → später Auswahl / andere Funktionen
                //ScreenNotification.ShowNotification($"Normaler Linksklick");

                RaycastSelect(false);
            }
        }




        protected override void Unload()
        {
            // Maus-Handler abmelden
            GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;

            _renderer?.Dispose();
            cornerIcon?.Dispose();
            designerWindow?.Dispose();
        }



        /// <summary>
        /// Führt einen Raycast aus und gibt alle getroffenen Objekte zurück,
        /// sortiert nach Distanz (nächstes zuerst).
        /// </summary>
        private void RaycastSelect(bool multi = false)
        {
            var ray = CreateRayFromMouse();
            //_renderer.SetDebugRay(ray, 200f);

            var closest = GetClosestObject(ray);

            if (closest != null)
            {
                if (!multi)
                {
                    // 🔹 Einzelauswahl → Toggle für dieses Objekt
                    bool willSelect = !closest.Selected;

                    foreach (var obj in _rendererControl.Objects)
                        obj.Selected = false;

                    closest.Selected = willSelect;

                    ScreenNotification.ShowNotification(
                        closest.Selected
                            ? $"Objekt ausgewählt: {closest.ModelKey}"
                            : $"Objekt abgewählt: {closest.ModelKey}"
                    );
                }
                else
                {
                    // 🔹 Mehrfachauswahl → unabhängiges Toggle
                    closest.Selected = !closest.Selected;

                    ScreenNotification.ShowNotification(
                        closest.Selected
                            ? $"Objekt hinzugefügt: {closest.ModelKey}"
                            : $"Objekt abgewählt: {closest.ModelKey}"
                    );
                }
            }
            else
            {
                // 🔹 Kein Treffer → nichts machen
                ScreenNotification.ShowNotification("Kein Objekt getroffen");
            }

            UpdateSelectedObjects();
        }




        private void UpdateSelectedObjects()
        {
            _rendererControl.SelectedObjects.Clear();
            _rendererControl.SelectedObjects.AddRange(_rendererControl.Objects.Where(o => o.Selected));
        }


        private BlueprintObject GetClosestObject(Ray ray)
        {
            BlueprintObject closest = null;
            float minDist = float.MaxValue;

            foreach (var obj in _rendererControl.Objects)
            {
                var dist = ray.Intersects(obj.BoundingBox);
                if (dist.HasValue && dist.Value < minDist)
                {
                    closest = obj;
                    minDist = dist.Value;
                }
            }

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



    }

}
