using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using System.Diagnostics;
using HomeDesigner.Loader;
using System;

namespace HomeDesigner.Views
{

    public class ModelSettingsView : View
    {
        private Module module;
        private FileManager fileManager;
        private bool downloadRunning = false;

        public ModelSettingsView(Module module)
        {
            this.module = module;
            fileManager = new FileManager(module.objModelPath, module.iconPath);
        }

        protected override void Build(Container buildPanel)
        {
            // Title
            new Label()
            {
                Parent = buildPanel,
                Text = "Original Template",
                Font = GameService.Content.DefaultFont18,
                Location = new Point(40, 10),
                AutoSizeWidth = true
            };


            var checkModelVersion = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Check for new Models",
                Width = 250,
                BasicTooltipText = "Checks if new 3D Models are available",
                Location = new Point(50, 130)
            };
            checkModelVersion.Click += async (s, e) =>
            {
                ScreenNotification.ShowNotification("Checking...");
                int newModelVersion = await fileManager.GetModelVersionAsync();
                if (newModelVersion > module.modelVersion.Value)
                {
                    ScreenNotification.ShowNotification("New Models are available");
                }
                else
                    ScreenNotification.ShowNotification("Model are Up to Date");
            };


            // Download Models Button
            var DownloadModels = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Download Current Deco Models",
                Width = 250,
                Location = new Point(50, 180)
            };
            if (downloadRunning)
            {
                DownloadModels.Enabled = false;
            }

            DownloadModels.Click += async (s, e) =>
            {
                DownloadModels.Enabled = false;

                try
                {
                    downloadRunning = true;
                    bool downloadSuccess = await fileManager.DownloadModelsFromDriveAsync();
                    if (downloadSuccess)
                    {
                        int newVersion = await fileManager.GetModelVersionAsync();
                        if (newVersion != 0)
                        {
                            module.modelVersion.Value = newVersion;
                        }
                        else
                            ScreenNotification.ShowNotification("An Error occoured when setting the new Model Version");
                    }
                }
                catch (Exception ex)
                {
                    ScreenNotification.ShowNotification(ex.Message);
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    DownloadModels.Enabled = true;
                    downloadRunning = true;
                }
            };



            var checkDecoVersion = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Check for new Decorations",
                Width = 250,
                BasicTooltipText = "Checks if new Decorations are available",
                Location = new Point(350, 130)
            };

            checkDecoVersion.Click += async (s, e) =>
            {
                ScreenNotification.ShowNotification("Checking...");
                bool newDecos = await fileManager.CheckForNewDecos(module._blueprintRenderer.decorationList);
                if (newDecos)
                {
                    ScreenNotification.ShowNotification("New Decorations are available");
                }
                else
                    ScreenNotification.ShowNotification("Your Decorations are Up to Date");
            };




        }


        private void resize(object sender, ResizedEventArgs e)
        {
        }

    }
}
