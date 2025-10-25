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

            // Modelle laden
            _renderer.LoadModel("Klavier", "models/klavier.obj", Vector3.Zero);
            _renderer.LoadModel("Klavier Test", "models/klavier_test.obj", Vector3.Zero);
            _renderer.LoadModel("Eleganter Tisch", "models/eleganter_tisch.obj", Vector3.Zero);
            _renderer.LoadModel("Test Tisch", "models/tisch_test.obj", Vector3.Zero);
            _renderer.LoadModel("Kodan Zaun", "models/kodan_zaun.obj", Vector3.Zero);
            _renderer.LoadModel("Kodan Ofen", "models/kodan_ofen.obj", Vector3.Zero);
            _renderer.LoadModel("cube", "models/cube.obj", Vector3.Zero);
            _renderer.LoadModel("real cube", "models/realCube.obj", Vector3.Zero);

            // Gizmomodelle laden
            _renderer.LoadGizmoModel("translate_X", "gizmos/Gizmo_Translate_X.obj");
            _renderer.LoadGizmoModel("translate_Y", "gizmos/Gizmo_Translate_Y.obj");
            _renderer.LoadGizmoModel("translate_Z", "gizmos/Gizmo_Translate_Z.obj");
            _renderer.LoadGizmoModel("rotate_X", "gizmos/Gizmo_Rotate_X.obj");
            _renderer.LoadGizmoModel("rotate_Y", "gizmos/Gizmo_Rotate_Y.obj");
            _renderer.LoadGizmoModel("rotate_Z", "gizmos/Gizmo_Rotate_Z.obj");
            _renderer.LoadGizmoModel("scale_X", "gizmos/Gizmo_Scale_X.obj");
            _renderer.LoadGizmoModel("scale_Y", "gizmos/Gizmo_Scale_Y.obj");
            _renderer.LoadGizmoModel("scale_Z", "gizmos/Gizmo_Scale_Z.obj");

            // Overlay-Control hinzufügen
            _rendererControl = new RendererControl(_renderer);
            GameService.Graphics.SpriteScreen.AddChild(_rendererControl);


            // Gizmoobjekte erstellen
            // Translate Gizmo
            _rendererControl.AddTranslateGizmos(new BlueprintObject()
            {
                ModelKey = "translate_Z",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddTranslateGizmos(new BlueprintObject()
            {
                ModelKey = "translate_Y",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddTranslateGizmos(new BlueprintObject()
            {
                ModelKey = "translate_X",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });


            // Rotate Gizmo
            _rendererControl.AddRotateGizmos(new BlueprintObject()
            {
                ModelKey = "rotate_Y",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddRotateGizmos(new BlueprintObject()
            {
                ModelKey = "rotate_Z",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddRotateGizmos(new BlueprintObject()
            {
                ModelKey = "rotate_X",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });

            // Scale Gizmo
            _rendererControl.AddScaleGizmos(new BlueprintObject()
            {
                ModelKey = "scale_Z",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddScaleGizmos(new BlueprintObject()
            {
                ModelKey = "scale_Y",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f
            });
            _rendererControl.AddScaleGizmos(new BlueprintObject()
            {
                ModelKey = "scale_X",
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.05f,
                
            });


            // Weltmatrizen einmal vorberechnen
            _rendererControl.updateWorld();
            _rendererControl.updateGizmos();

            //Debug.WriteLine($"[Init] Player Pos = {GameService.Gw2Mumble.PlayerCharacter?.Position}");

        }
        protected override void Update(GameTime gameTime)
        {

        }

        protected override void Unload()
        {
            // Maus-Handler abmelden
            //GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;

            _renderer?.Dispose();
            cornerIcon?.Dispose();
            designerWindow?.Dispose();
        }



        



    }

}
