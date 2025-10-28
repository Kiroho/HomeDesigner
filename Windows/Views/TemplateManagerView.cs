using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Blish_HUD.Modules.Managers;
using HomeDesigner.Loader;
using System.Diagnostics;

namespace HomeDesigner.Views
{
    public class TemplateManagerView : View
    {
        private FlowPanel _loadedTemplatesPanel;
        private readonly ContentsManager contents;

        private List<XDocument> _loadedTemplates = new List<XDocument>();
        private XDocument mergedTemplate;

        public TemplateManagerView(ContentsManager contents)
        {
            this.contents = contents;
        }

        protected override void Build(Container buildPanel)
        {
            // Title
            new Label()
            {
                Parent = buildPanel,
                Text = "Loaded Templates",
                Font = GameService.Content.DefaultFont18,
                Location = new Point(20, 10),
                AutoSizeWidth = true
            };

            // Panel for loaded Templates
            _loadedTemplatesPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width - 40, 150),
                Location = new Point(20, 40),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true,
                ControlPadding = new Vector2(4, 4)
            };

            // Load Template Button
            var loadButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Load Template",
                Width = 180,
                Location = new Point(20, 220)
            };

            loadButton.Click += (s, e) =>
            {
                var loadDialog = new LoadDialog(contents);

                loadDialog.TemplateSelected += (path) =>
                {
                    var templ = XmlLoader.LoadXml(path);
                    AddTemplate(templ, Path.GetFileName(path));
                    //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");
                };

                loadDialog.Show();
            };

            
            // Merge Templates
            var mergeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Merge Templates",
                Width = 180,
                Location = new Point(200, 220)
            };

            mergeButton.Click += (s, e) =>
            {
                if (_loadedTemplates.Count == 0) return;

                List<string> mapIDs = new List<string>();

                foreach(var template in _loadedTemplates)
                {
                    var decorations = template.Element("Decorations");
                    mapIDs.Add(decorations.Attribute("mapId").Value.ToString());
                }

                if(mapIDs.Distinct().Count() == 1)
                {
                    mergedTemplate = XmlLoader.MergeTemplates(_loadedTemplates);
                    ClearLoadedTemplates();
                    AddTemplate(mergedTemplate, "Merged Template");
                    ScreenNotification.ShowNotification("Templates merged!");
                }
                else
                {

                    ScreenNotification.ShowNotification("Can't merge Templates of different Maps!");
                }


                
            };

            // Save Template Button
            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Template",
                Width = 180,
                Location = new Point(380, 220)
            };

            saveButton.Click += (s, e) =>
            {
                if (_loadedTemplates.Count != 1)
                {
                    ScreenNotification.ShowNotification("To save, there must be > one < template in the list.");
                    return;
                }

                // Give first (and only) template to save dialog
                var templateToSave = _loadedTemplates.First();
                var saveDialog = new SaveDialog(contents, templateToSave);

                saveDialog.TemplateSaved += (path) =>
                {
                    ScreenNotification.ShowNotification($"Template Saved");
                };

                saveDialog.Show();
            };

            buildPanel.Resized += resize;
        }

        private void resize(object sender, ResizedEventArgs e)
        {
            _loadedTemplatesPanel.Width = _loadedTemplatesPanel.Parent.ContentRegion.Width - 40;
        }

        private void AddTemplate(XDocument template, string displayName)
        {
            if (template == null) return;
            if (_loadedTemplates.Contains(template)) return;

            _loadedTemplates.Add(template);

            var row = new Panel()
            {
                Parent = _loadedTemplatesPanel,
                Width = 500,
                //Width = _loadedTemplatesPanel.ContentRegion.Width - 80,
                Height = 40,
                ShowBorder = true
            };

            // Show Names
            new Label()
            {
                Parent = row,
                Text = displayName,
                Location = new Point(5, 5),
                AutoSizeWidth = true
            };

            // Read template's map info
            string mapName = "Unknown Map";
            var decorations = template.Element("Decorations");
            if (decorations != null && decorations.Attribute("mapName") != null)
            {
                mapName = decorations.Attribute("mapName").Value;
            }

            new Label()
            {
                Parent = row,
                Text = $"{mapName}",
                Location = new Point(220, 5),
                AutoSizeWidth = true
            };

            // Remove Button
            var removeButton = new StandardButton()
            {
                Parent = row,
                Text = "X",
                Size = new Point(25, 25),
                Location = new Point(450, 2),
                BasicTooltipText = "Remove Template"
            };

            removeButton.Click += (s, e) =>
            {
                _loadedTemplates.Remove(template);
                row.Dispose();
                //ScreenNotification.ShowNotification($"Template entfernt: {displayName}");
            };
        }


        private void ClearLoadedTemplates()
        {
            foreach (var control in _loadedTemplatesPanel.Children.ToArray())
            {
                control.Dispose();
            }

            _loadedTemplates.Clear();
        }
    }
}
