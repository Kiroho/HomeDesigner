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

        // Liste speichert nun direkt XDocuments
        private List<XDocument> _loadedTemplates = new List<XDocument>();
        private XDocument mergedTemplate;

        public TemplateManagerView(ContentsManager contents)
        {
            this.contents = contents;
        }

        protected override void Build(Container buildPanel)
        {
            // Titel
            new Label()
            {
                Parent = buildPanel,
                Text = "📂 Geladene Templates",
                Font = GameService.Content.DefaultFont18,
                Location = new Point(20, 10),
                AutoSizeWidth = true
            };

            // Panel für geladene Templates
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

            // 🔸 Template laden Button
            var loadButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Template laden",
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

            
            // 🔸 Templates Mergen
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

                mergedTemplate = XmlLoader.MergeTemplates(_loadedTemplates);
                ClearLoadedTemplates();
                AddTemplate(mergedTemplate, "Merged Template");
                ScreenNotification.ShowNotification("🔀 Templates merged!");
            };

            // 🔸 Template speichern Button
            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Template speichern",
                Width = 180,
                Location = new Point(380, 220)
            };

            saveButton.Click += (s, e) =>
            {
                if (_loadedTemplates.Count != 1)
                {
                    ScreenNotification.ShowNotification("⚠ Bitte genau ein Template auswählen, um es zu speichern.");
                    return;
                }

                // Das erste (und einzige) Template an den SaveDialog übergeben
                var templateToSave = _loadedTemplates.First();
                var saveDialog = new SaveDialog(contents, templateToSave);

                saveDialog.TemplateSaved += (path) =>
                {
                    ScreenNotification.ShowNotification($"✅ Template saved");
                };

                saveDialog.Show();
            };


            // 🔸 Template speichern Button
            var testButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "rotation Test",
                Width = 180,
                Location = new Point(380, 280)
            };

            testButton.Click += (s, e) =>
            {
                TestQuaternionConversion();

            };

        }

        public static void TestQuaternionConversion()
        {
            float pitch = -1.0f;
            float yaw = 0.5f;
            float roll = 0.25f;

            Quaternion q1 = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

            XmlLoader.QuaternionToYawPitchRoll(q1, out float yaw2, out float pitch2, out float roll2);

            Debug.WriteLine($"Original: P={pitch} Y={yaw} R={roll}");
            Debug.WriteLine($"Converted: P={pitch2} Y={yaw2} R={roll2}");

            ScreenNotification.ShowNotification($"Original: P={pitch} Y={yaw} R={roll}");
            ScreenNotification.ShowNotification($"Converted: P={pitch2} Y={yaw2} R={roll2}");

            Quaternion q2 = Quaternion.CreateFromYawPitchRoll(yaw2, pitch2, roll2);

            float dot = Quaternion.Dot(q1, q2);
            Debug.WriteLine($"Dot(q1, q2) = {dot}");
            ScreenNotification.ShowNotification($"Dot(q1, q2) = {dot}");

        }

        private void AddTemplate(XDocument template, string displayName)
        {
            if (template == null) return;
            if (_loadedTemplates.Contains(template)) return; // doppelte verhindern

            _loadedTemplates.Add(template);

            var row = new Panel()
            {
                Parent = _loadedTemplatesPanel,
                Width = _loadedTemplatesPanel.ContentRegion.Width - 25,
                Height = 40,
                ShowBorder = true
            };

            // 📄 Name anzeigen
            new Label()
            {
                Parent = row,
                Text = displayName,
                Location = new Point(5, 5),
                AutoSizeWidth = true
            };

            // 🗺 Mapname aus template auslesen
            string mapName = "Unbekannte Karte";
            var decorations = template.Element("Decorations");
            if (decorations != null && decorations.Attribute("mapName") != null)
            {
                mapName = decorations.Attribute("mapName").Value;
            }

            new Label()
            {
                Parent = row,
                Text = $"🗺 {mapName}",
                Location = new Point(180, 5),
                AutoSizeWidth = true
            };

            // ❌ Entfernen-Button
            var removeButton = new StandardButton()
            {
                Parent = row,
                Text = "X",
                Size = new Point(25, 25),
                Location = new Point(row.Width - 30, 2),
                BasicTooltipText = "Template entfernen"
            };

            removeButton.Click += (s, e) =>
            {
                _loadedTemplates.Remove(template);
                row.Dispose();
                ScreenNotification.ShowNotification($"🗑 Template entfernt: {displayName}");
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
