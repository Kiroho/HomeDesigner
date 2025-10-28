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
using HomeDesigner.Views;
using Microsoft.Xna.Framework.Graphics;

namespace HomeDesigner
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        private SettingEntry<bool> activDesignerTool;

        private CornerIcon cornerIcon;
        private DesignerWindow designerWindow;
        private GraphicsDevice gd;
        private BlueprintRenderer _renderer;
        private RendererControl _rendererControl;

        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            //activDesignerTool = settings.DefineSetting(
            //    "Activate Designer Tool",
            //    true,
            //    () => "Activate Designer Tool",
            //    () => "Activates the 3D Designer with a new tab in the Home Designer Window");
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

            //activDesignerTool.SettingChanged += DesignerToolSetting;

            await Task.Delay(75);
            cornerIcon.Visible = true;
        }


        protected override void Initialize()
        {
             gd = GameService.Graphics.GraphicsDeviceManager.GraphicsDevice; // Im Auge behalten!!!


            _renderer = new BlueprintRenderer(gd, ContentsManager);
            _rendererControl = new RendererControl(_renderer);

        }

        protected override void Update(GameTime gameTime)
        {

        }

        protected override void Unload()
        {
            designerWindow?.Dispose();
            _rendererControl?.Dispose();
            _renderer?.Dispose();
            cornerIcon?.Dispose();
            designerWindow?.Dispose();
        }




        private void DesignerToolSetting(object sender, ValueChangedEventArgs<bool> e)
        {
            ScreenNotification.ShowNotification($"Setting: {activDesignerTool.Value}");
            if (e.NewValue)
            {
                initializeDesignerTool();
            }
            else
            {
                designerWindow.removeDesignerTab();
            }
        }

        private void initializeDesignerTool()
        {

            // Modelle laden
            _renderer.LoadModel("Piano", "models/klavier.obj", Vector3.Zero);
            _renderer.LoadModel("Fancy Table", "models/eleganter_tisch.obj", Vector3.Zero);
            _renderer.LoadModel("Kodan Fence", "models/kodan_zaun.obj", Vector3.Zero);
            _renderer.LoadModel("Kodan Oven", "models/kodan_ofen.obj", Vector3.Zero);

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


            designerWindow.setBlueprintRenderer(_renderer);
            designerWindow.setRendererControl(_rendererControl);

            designerWindow.addDesignerTab();

        }

    }

}
