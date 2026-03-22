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
using System.Threading;
using HomeDesigner.Loader;

namespace HomeDesigner
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        private CornerIcon cornerIcon;
        private DesignerWindow designerWindow;
        public GraphicsDevice gd;
        public BlueprintRenderer _blueprintRenderer;
        public RendererControl _rendererControl;
        private FileManager _fileManager;
        //private int selectedObjectCount = 0;
        public SettingEntry<int> renderDistance;
        public SettingEntry<int> gizmoSize;
        public SettingEntry<int> selectionSesitivity;
        public SettingEntry<bool> lazyLoading;
        private SettingEntry<bool> checkModelVersionOnStart;
        private SettingEntry<bool> autoUpdateOnStart;
        public SettingEntry<int> modelVersion;
        public String objModelPath = "";
        public String iconPath = "";


        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) 
        {
            gd = GameService.Graphics.GraphicsDeviceManager.GraphicsDevice;
            objModelPath = DirectoriesManager.GetFullDirectoryPath("HomeDesignerModels");
            iconPath = DirectoriesManager.GetFullDirectoryPath("HomeDesigner");

            _blueprintRenderer = new BlueprintRenderer(this);
            _rendererControl = new RendererControl(this);
            _fileManager = new FileManager(objModelPath, iconPath);


        }

        protected override void DefineSettings(SettingCollection settings)
        {
            renderDistance = settings.DefineSetting(
                "Render Distance",
                1000,
                () => "Render Distance",
                () => "Sets the distance for visible Blueprints");
            renderDistance.SetRange(0, 1000);

            gizmoSize = settings.DefineSetting(
                "Gizmo Size",
                5,
                () => "Gizmo Size",
                () => "Sets the size of your editing tools");
            gizmoSize.SetRange(1, 10);

            selectionSesitivity = settings.DefineSetting(
                "Selection Tool Mouse Sensitivity",
                5,
                () => "Selection Tool Mouse Sensitivity",
                () => "Sets the mouse  sensitivity for height selection");
            selectionSesitivity.SetRange(1, 10);

            lazyLoading = settings.DefineSetting(
                "Loads Deco Models only when used.",
                true,
                () => "Model Lazy Loading",
                () => "Deactivate to load all Deco Models on module start. This leads to a longer start time.\nRequires Restart.");

            // Für Settings, die nicht in der UI auftauchen
            var hiddenCollection = settings.AddSubCollection("internal", false);

            modelVersion = hiddenCollection.DefineSetting(
                "Model Version",
                0
                );


            checkModelVersionOnStart = settings.DefineSetting(
                "Check for new Models on Start",
                true,
                () => "Check for new Models on Start",
                () => "On start the module will automatically check if new deco models are available");

            autoUpdateOnStart = settings.DefineSetting(
                "Download new Models on Start",
                false,
                () => "Download new Models on Start",
                () => "On Start automatically download new Deco Models if available.\nDownloaded file can reach 150mb and more.");

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

            // Load Decorations from local file. If missing, download from API
            _blueprintRenderer.decorationList = _fileManager.LoadDecorationsFromFile();
            if (_blueprintRenderer.decorationList.Count == 0)
            {
                // Wenn Liste leer -> lade von API
                ScreenNotification.ShowNotification("Loading Decoration from API...");
                _blueprintRenderer.decorationList = await _fileManager.DownloadDecorationsAsync();
                _fileManager.SaveDecorationsToFile(_blueprintRenderer.decorationList);
            }
            else
                ScreenNotification.ShowNotification("Loading Decoration from Local File...");

            // Load Decoration Icons
            await _fileManager.LoadIconsAsync(gd, _blueprintRenderer.decoIconDict, _blueprintRenderer.decorationList);


            // Load Categories
            _blueprintRenderer.decoCategories = await _fileManager.LoadDecoCategories();





            //_blueprintRenderer.decorationLut = await "https://bhm.blishhud.com/gw2stacks_blish/item_storage/decorationLUT.json".WithHeader("User-Agent", "Blish-HUD").GetJsonAsync<DecorationLUT>();

            //if (Directory.EnumerateFiles(DirectoriesManager.GetFullDirectoryPath("HomeDesigner"), "*", SearchOption.AllDirectories).ToList().Count <
            //    _blueprintRenderer.decorationLut.decorations.Count)
            //{
            //    foreach (var deco in _blueprintRenderer.decorationLut.decorations)
            //    {
            //        var texture = AsyncTexture2D.FromAssetId(deco.Value.icon);

            //        if (texture == null)
            //        {
            //            texture = ContentsManager.GetTexture("Icons/placeholder.png");
            //        }
            //        _blueprintRenderer.decoIconDict[deco.Key] = texture;

            //        //Debug.WriteLine($"________Deko {deco.Key} geladen von Web____");
            //    }
            //    _ = saveDecoIcons();
            //}
            //else
            //{
            //    //Debug.WriteLine("________Bilder bereits geladen");
            //    foreach (var deco in _blueprintRenderer.decorationLut.decorations)
            //    {
            //        var texture = loadDecoIcon(deco.Key);
            //        _blueprintRenderer.decoIconDict[deco.Key] = texture;

            //        //Debug.WriteLine($"________Deko {deco.Key} geladen von Ordner____");
            //    }
            //}






            // Load Models only if lazy loading setting is off
            if (!lazyLoading.Value)
            {
                ScreenNotification.ShowNotification("Loading Models...");
                await Task.Delay(100);
                //List<string> keyList = _blueprintRenderer.decorationLut.decorations.Keys.Select(k => k.ToString()).ToList();
                List<string> keyList = _blueprintRenderer.decorationList.Select(d => d.id.ToString()).ToList();
                int total = keyList.Count;
                int completed = 0;
                int lastPercentReported = 0;

                SemaphoreSlim semaphore = new SemaphoreSlim(50);
                var tasks = keyList.Select(async key =>
                {
                    try
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            await Task.Run(() =>
                            {
                                _blueprintRenderer.LoadModel(key, objModelPath, Vector3.Zero);
                            });
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                        int done = Interlocked.Increment(ref completed);
                        int percent = (done * 100) / total;

                        int oldPercent;
                        do
                        {
                            oldPercent = lastPercentReported;

                            if (percent < oldPercent + 10)
                                return;

                        } while (Interlocked.CompareExchange(
                                    ref lastPercentReported,
                                    percent,
                                    oldPercent) != oldPercent);

                        ScreenNotification.ShowNotification($"{percent}% Models Loaded");
                    }
                    catch (Exception)
                    {
                        ScreenNotification.ShowNotification($"Model load failed: {key}");
                    }
                });

                await Task.WhenAll(tasks);

                ScreenNotification.ShowNotification("All Models Loaded");
            }

            _ = checkNewModels();

            await Task.Delay(75);
            cornerIcon.Visible = true;

            
        }


        //private Task asyncLoadModel(String key)
        //{
        //    return Task.Run(() =>
        //    {
        //        _blueprintRenderer.LoadModel(key, objModelPath, Vector3.Zero);
        //    });
        //}

        protected override void OnModuleLoaded(EventArgs e)
        {

            GameService.Graphics.QueueMainThreadRender(_ => {

                designerWindow = new DesignerWindow(this);
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


        private async Task checkNewModels()
        {

            if (checkModelVersionOnStart.Value || autoUpdateOnStart.Value)
            {
                int newVersion = await _fileManager.GetModelVersionAsync();
                if (newVersion > modelVersion.Value)
                {
                    ScreenNotification.ShowNotification("New Model Version Available");
                }

                if (autoUpdateOnStart.Value)
                {
                    if (newVersion > modelVersion.Value)
                    {   
                        // Teste Install Model mit vorhandener Zip
                        //string filePath = Path.Combine(objModelPath, "models.zip");
                        //bool test = await _fileManager.installModels(filePath);
                        bool downloadSuccess = await _fileManager.DownloadModelsFromDriveAsync();
                        if (downloadSuccess)
                        {
                            ScreenNotification.ShowNotification("Models Updated");
                            modelVersion.Value = newVersion;
                        }
                        else
                        {
                            ScreenNotification.ShowNotification("Due to an Error models could not be updated");
                        }

                    }
                }
            }
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
                                    //Debug.WriteLine($"________Deko {deco.Key} gespeichert");
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
                    return Texture2D.FromFile(gd,filePath); //.FromStream(gd, stream);
                }
            }
            else
            {
                return ContentsManager.GetTexture("Icons/placeholder.png");
            }
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