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
using Flurl.Http;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Content;
using System.IO;
using Flurl;
using Newtonsoft.Json.Linq;

namespace HomeDesigner
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        private CornerIcon cornerIcon;
        private DesignerWindow designerWindow;
        private GraphicsDevice gd;
        private BlueprintRenderer _blueprintRenderer;
        private RendererControl _rendererControl;
        //private int selectedObjectCount = 0;
        private SettingEntry<int> renderDistance;
        private SettingEntry<int> gizmoSize;


        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) 
        {
            gd = GameService.Graphics.GraphicsDeviceManager.GraphicsDevice; // Im Auge behalten!!!
            _blueprintRenderer = new BlueprintRenderer(gd, ContentsManager);
            _rendererControl = new RendererControl(_blueprintRenderer);


        }

        protected override void DefineSettings(SettingCollection settings)
        {
            renderDistance = settings.DefineSetting(
                "Render Distance",
                1000,
                () => "Render Distance",
                () => "Sets the distance for visible Blueprints");
            renderDistance.SetRange(0, 1000);
            renderDistance.SettingChanged += (s, e) =>
            {
                _blueprintRenderer.renderDistance = renderDistance.Value;
            };

            gizmoSize = settings.DefineSetting(
                "Gizmo Size",
                5,
                () => "Gizmo Size",
                () => "Sets the size of your editing tools");
            gizmoSize.SetRange(1, 10);
            gizmoSize.SettingChanged += (s, e) =>
            {
                _blueprintRenderer.gizmoSize = gizmoSize.Value;
            };

        }

        protected override void Initialize()
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


            _blueprintRenderer.decoCategories = await LoadDecoCategories();

            _blueprintRenderer.decorationLut = await "https://bhm.blishhud.com/gw2stacks_blish/item_storage/decorationLUT.json".WithHeader("User-Agent", "Blish-HUD").GetJsonAsync<DecorationLUT>();

            //Debug.WriteLine($"______________________________Files in Ordner: {Directory.EnumerateFiles(DirectoriesManager.GetFullDirectoryPath("HomeDesigner"), "*", SearchOption.AllDirectories).ToList().Count}");
            //Debug.WriteLine($"______________________________Dekos: {_blueprintRenderer.decorationLut.decorations.Count}");

            if (Directory.EnumerateFiles(DirectoriesManager.GetFullDirectoryPath("HomeDesigner"), "*", SearchOption.AllDirectories).ToList().Count <
                _blueprintRenderer.decorationLut.decorations.Count)
            {
                foreach (var deco in _blueprintRenderer.decorationLut.decorations)
                {
                    var texture = AsyncTexture2D.FromAssetId(deco.Value.icon);

                    if (texture == null)
                    {
                        texture = ContentsManager.GetTexture("Icons/placeholder.png");
                    }
                    _blueprintRenderer.decoIconDict[deco.Key] = texture;

                    //Debug.WriteLine($"________Deko {deco.Key} geladen von Web____");
                }
                saveDecoIcons();
            }
            else
            {
                //Debug.WriteLine("________Bilder bereits geladen");
                foreach (var deco in _blueprintRenderer.decorationLut.decorations)
                {
                    var texture = loadDecoIcon(deco.Key);
                    _blueprintRenderer.decoIconDict[deco.Key] = texture;

                    //Debug.WriteLine($"________Deko {deco.Key} geladen von Ordner____");
                }
            }


            await Task.Delay(75);
            cornerIcon.Visible = true;

            
        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            GameService.Graphics.QueueMainThreadRender(_ => {


                foreach (var key in _blueprintRenderer.decorationLut.decorations)
                {
                    _blueprintRenderer.LoadModel(key.Value.id.ToString(), $"models/{key.Value.id}.obj", Vector3.Zero);
                }


                designerWindow = new DesignerWindow(this.ContentsManager, _rendererControl, _blueprintRenderer);
                initializeDesignerTool();

                // On click listener for corner icon
                cornerIcon.Click += delegate
                {
                    //ScreenNotification.ShowNotification("Icon gedrückt");
                    designerWindow.ToggleWindow();
                };

            });


            base.OnModuleLoaded(e);
        }


        protected override void Update(GameTime gameTime)
        {
            if (_rendererControl != null && designerWindow != null)
            {
                designerWindow.designerView.RefreshSelectedList();;
            }
            
        }

        protected override void Unload()
        {
            designerWindow?.unload();
            designerWindow?.Dispose();
            _rendererControl.unload();
            _rendererControl?.Dispose();
            _blueprintRenderer?.Dispose();
            cornerIcon?.Dispose();
        }


        private Task saveDecoIcons()
        {
            var folder = DirectoriesManager.GetFullDirectoryPath("HomeDesigner");
            return Task.Run(() =>
            {
                foreach (var deco in _blueprintRenderer.decoIconDict)
                {
                    string filePath = Path.Combine(folder, deco.Key + ".png");
                        try
                        {
                            if (!File.Exists(filePath))
                            {
                                using (var stream = File.Create(filePath))
                                {
                                    deco.Value.Texture.SaveAsPng(stream, deco.Value.Width, deco.Value.Height);
                                    Debug.WriteLine($"________Deko {deco.Key} gespeichert");
                                }
                            }
                            else
                                Debug.WriteLine($"________Deko {deco.Key} existiert bereits");

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                }
            });

        }

        private Texture2D loadDecoIcon(int decoKey)
        {
            var folder = DirectoriesManager.GetFullDirectoryPath("HomeDesigner");
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"{decoKey}.png");

            if (File.Exists(filePath))
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return Texture2D.FromStream(gd, stream);
                }
            }
            else
            {
                return ContentsManager.GetTexture("Icons/placeholder.png");
            }
        }


        private async Task<Dictionary<int, string>> LoadDecoCategories()
        {
            const string endpoint = "https://api.guildwars2.com/v2/homestead/decorations/categories";

            // 1) Alle IDs holen
            var ids = await endpoint
                .GetJsonAsync<List<int>>();

            // 2) Detaildaten laden
            var json = await endpoint
                .SetQueryParam("ids", string.Join(",", ids))
                .GetJsonAsync<JArray>();

            // 3) Dictionary erstellen (ID → Name)
            var dict = new Dictionary<int, string>();

            foreach (var item in json)
            {
                int id = item.Value<int>("id");
                string name = item.Value<string>("name");

                dict[id] = name;
            }

            return dict;
        }


        private void initializeDesignerTool()
        {

            // Modelle laden
            //_renderer.LoadModel("Kerze", "models/kerze.obj", Vector3.Zero);
            //_renderer.LoadModel("Piano", "models/klavier.obj", Vector3.Zero);
            //_renderer.LoadModel("Fancy Table", "models/eleganter_tisch.obj", Vector3.Zero);
            //_renderer.LoadModel("Kodan Fence", "models/kodan_zaun.obj", Vector3.Zero);
            //_renderer.LoadModel("Kodan Oven", "models/kodan_ofen.obj", Vector3.Zero);

            // Gizmomodelle laden
            _blueprintRenderer.LoadGizmoModel("translate_X", "gizmos/Gizmo_Translate_X.obj");
            _blueprintRenderer.LoadGizmoModel("translate_Y", "gizmos/Gizmo_Translate_Y.obj");
            _blueprintRenderer.LoadGizmoModel("translate_Z", "gizmos/Gizmo_Translate_Z.obj");
            _blueprintRenderer.LoadGizmoModel("rotate_X", "gizmos/Gizmo_Rotate_X.obj");
            _blueprintRenderer.LoadGizmoModel("rotate_Y", "gizmos/Gizmo_Rotate_Y.obj");
            _blueprintRenderer.LoadGizmoModel("rotate_Z", "gizmos/Gizmo_Rotate_Z.obj");
            _blueprintRenderer.LoadGizmoModel("scale_X", "gizmos/Gizmo_Scale_X.obj");
            _blueprintRenderer.LoadGizmoModel("scale_Y", "gizmos/Gizmo_Scale_Y.obj");
            _blueprintRenderer.LoadGizmoModel("scale_Z", "gizmos/Gizmo_Scale_Z.obj");

            


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

        }

    }

}