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


            // Download Models Button
            var DownloadModels = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Download Current Deco Models",
                Width = 250,
                Location = new Point(20, 130)
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
                    bool downloadSuccess = await fileManager.DownloadFromDriveAsync();
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


            var checkVersion = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Check for new Models",
                Width = 250,
                BasicTooltipText = "Checks if new Deco Modules are available",
                Location = new Point(20, 180)
            };
            checkVersion.Click += async (s, e) =>
            {
                int newModelVersion = await fileManager.GetModelVersionAsync();
                if (newModelVersion > module.modelVersion.Value)
                {
                    ScreenNotification.ShowNotification("New Models are available");
                }
                else
                    ScreenNotification.ShowNotification("Model are Up to Date");
            };


        }


        private void resize(object sender, ResizedEventArgs e)
        {
        }

    }
}
