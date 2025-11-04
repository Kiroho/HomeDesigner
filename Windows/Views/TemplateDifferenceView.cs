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
    public class TemplateDifferenceView : View
    {
        private FlowPanel _originalTemplatesPanel;
        private FlowPanel _cutOutTemplatesPanel;
        private readonly ContentsManager contents;

        private List<XDocument> _loadedOriginalTemplates = new List<XDocument>();
        private List<XDocument> _loadedCutOutTemplates = new List<XDocument>();
        private XDocument differedTemplate;

        public TemplateDifferenceView(ContentsManager contents)
        {
            this.contents = contents;
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

            // Panel for loaded Templates
            _originalTemplatesPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width - 40, 50),
                Location = new Point(20, 60),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true,
                ControlPadding = new Vector2(4, 4)
            };

            // Load Template Button
            var loadOriginalButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Load Template",
                Width = 180,
                Location = new Point(20, 130)
            };

            loadOriginalButton.Click += (s, e) =>
            {
                var loadDialog = new LoadDialog(contents);

                loadDialog.TemplateSelected += (path) =>
                {
                    var templ = XmlLoader.LoadXml(path);
                    ClearLoadedTemplates(_loadedOriginalTemplates, _originalTemplatesPanel);
                    AddTemplate(_loadedOriginalTemplates, _originalTemplatesPanel, templ, Path.GetFileName(path));
                    //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");
                };

                loadDialog.Show();
            };






            new Label()
            {
                Parent = buildPanel,
                Text = "Cut Out Template",
                Font = GameService.Content.DefaultFont18,
                Location = new Point(40, 190),
                AutoSizeWidth = true
            };

            // Panel for loaded Templates
            _cutOutTemplatesPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width - 40, 50),
                Location = new Point(20, 230),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true,
                ControlPadding = new Vector2(4, 4)
            };

            // Load Template Button
            var loadCutOutButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Load Template",
                Width = 180,
                Location = new Point(20, 300)
            };

            loadCutOutButton.Click += (s, e) =>
            {
                var loadDialog = new LoadDialog(contents);

                loadDialog.TemplateSelected += (path) =>
                {
                    var templ = XmlLoader.LoadXml(path);
                    ClearLoadedTemplates(_loadedCutOutTemplates, _cutOutTemplatesPanel);
                    AddTemplate(_loadedCutOutTemplates, _cutOutTemplatesPanel, templ, Path.GetFileName(path));
                    //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");
                };

                loadDialog.Show();
            };








            // Differ Templates
            var differButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Search Differences",
                Width = 180,
                Location = new Point(20, 350)
            };

            differButton.Click += (s, e) =>
            {
                if(_loadedOriginalTemplates.Count<1 || _loadedCutOutTemplates.Count < 1)
                {
                    ScreenNotification.ShowNotification("To find the differences, two templates must be selected.");
                }
                else
                {
                    ScreenNotification.ShowNotification("Search successfull.");
                    differedTemplate = CompareXDocuments(_loadedOriginalTemplates[0], _loadedCutOutTemplates[0]);
                }

                
            };

            // Save Template Button
            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Template",
                Width = 180,
                Location = new Point(220, 350)
            };

            saveButton.Click += (s, e) =>
            {
                if (differedTemplate == null)
                {
                    ScreenNotification.ShowNotification("Nothing to save yet.");
                    return;
                }

                // Give first (and only) template to save dialog
                var templateToSave = differedTemplate;
                var saveDialog = new SaveDialog(contents, templateToSave);

                saveDialog.TemplateSaved += (path) =>
                {
                    ScreenNotification.ShowNotification($"Template Saved");
                };

                saveDialog.Show();
            };


            var idea = new Label()
            {
                Parent = buildPanel,
                Text = "The idea: \nTo cut out specific construction from your template \nyou can do this:\n\n- Save your original template\n- Remove the decorations you want to keep\n- Save your 'cut out' template\n Now you can compare both files to get the missing decorations.\nThat's where this tool helps you.",
                Width = 400,
                Location = new Point(40, 390),
                AutoSizeHeight = true
            };

            var HowTo = new Label()
            {
                Parent = buildPanel,
                Text = "How to: \n\n-Load your original template in the first slot\n- Load the template with removed decorations in the 2nd slot\n- Click 'Search Differences'\n- Save the new template\nThat's it. You are done. :)",
                Width = 400,
                Location = new Point(500, 390),
                AutoSizeHeight = true
            };


            //var warning = new Label()
            //{
            //    Parent = buildPanel,
            //    Text = "This is an early version. \nMake sure to backup your templates \nbefore using this tool.\nJust in case. :)",
            //    TextColor = Color.Red,
            //    Height = 200,
            //    Location = new Point(40, 390),
            //    AutoSizeWidth = true
            //};



            buildPanel.Resized += resize;
        }

        private void resize(object sender, ResizedEventArgs e)
        {
            if(_originalTemplatesPanel.Parent != null)
            {
                _originalTemplatesPanel.Width = _originalTemplatesPanel.Parent.ContentRegion.Width - 40;
            }
            if(_cutOutTemplatesPanel.Parent != null)
            {
                _cutOutTemplatesPanel.Width = _cutOutTemplatesPanel.Parent.ContentRegion.Width - 40;
            }
        }

        private void AddTemplate(List<XDocument> list, Panel pan, XDocument template, string displayName)
        {
            if (template == null) return;
            if (list.Contains(template)) return;

            list.Add(template);

            var row = new Panel()
            {
                Parent = pan,
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
                list.Remove(template);
                row.Dispose();
                //ScreenNotification.ShowNotification($"Template entfernt: {displayName}");
            };
        }


        private void ClearLoadedTemplates(List<XDocument> list, Panel pan)
        {
            foreach (var control in pan.Children.ToArray())
            {
                control.Dispose();
            }

            list.Clear();
        }



        public static XDocument CompareXDocuments(XDocument docA, XDocument docB)
        {
            if (docA?.Root == null || docB?.Root == null)
                throw new ArgumentException("Beide XDocument-Objekte müssen eine gültige Root enthalten.");

            // Vergleichsschlüssel ohne 'pos'
            string BuildPropKey(XElement prop)
            {
                var attribs = prop.Attributes()
                    .Where(a => a.Name.LocalName != "pos")
                    .OrderBy(a => a.Name.LocalName)
                    .Select(a => $"{a.Name.LocalName}={a.Value}");

                // Payload optional für Vergleich
                var payload = prop.Elements("payload")
                    .Select(p => $"{p.Attribute("pt")?.Value ?? ""}|{p.Attribute("v")?.Value ?? ""}|{p.Value}")
                    .DefaultIfEmpty("")
                    .FirstOrDefault();

                return string.Join("|", attribs) + "|payload:" + payload;
            }

            // Vergleichsdaten aus Datei B
            var propsBKeys = docB.Root.Elements("prop")
                .Select(BuildPropKey)
                .ToHashSet();

            // Fehlende Props in A finden
            var missingProps = docA.Root.Elements("prop")
                .Where(p => !propsBKeys.Contains(BuildPropKey(p)))
                .Select(p => new XElement(p)) // komplette Struktur (inkl. Payload) klonen
                .ToList();

            // Neues XML mit gleichen Root-Attributen wie A
            var decorations = new XElement("Decorations",
                docA.Root.Attributes(),
                missingProps
            );

            return new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                decorations
            );
        }


        //public static XDocument CompareXDocuments(XDocument docA, XDocument docB)
        //{
        //    if (docA?.Root == null || docB?.Root == null)
        //        throw new ArgumentException("Beide XDocument-Objekte müssen eine gültige Root enthalten.");

        //    // Vergleichsschlüssel ohne "pos"
        //    Func<XElement, string> attribKey = prop =>
        //    {
        //        var attributes = prop.Attributes()
        //            .Where(a => a.Name.LocalName != "pos")
        //            .OrderBy(a => a.Name.LocalName)
        //            .Select(a => $"{a.Name.LocalName}={a.Value}");
        //        return string.Join("|", attributes);
        //    };

        //    // Alle <prop>-Einträge sammeln
        //    var propsA = docA.Root.Elements("prop").ToList();
        //    var propsBKeys = docB.Root.Elements("prop")
        //        .Select(attribKey)
        //        .ToHashSet();

        //    // Fehlende Props in A finden
        //    var missingProps = propsA
        //        .Where(p => !propsBKeys.Contains(attribKey(p)))
        //        .ToList();

        //    // Neues Dokument im gleichen Stil wie docA erstellen
        //    var decorations = new XElement("Decorations",
        //        docA.Root.Attributes(),
        //        missingProps.Select(p => new XElement("prop", p.Attributes()))
        //    );

        //    return new XDocument(
        //        new XDeclaration("1.0", "UTF-8", null),
        //        decorations
        //    );
        //}
    }
}
