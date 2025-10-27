using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HomeDesigner.Loader;
using HomeDesigner;

public class SaveDialog : StandardWindow
{
    public event Action<string> TemplateSaved;

    private FlowPanel _filePanel;
    private TextBox _fileNameBox;
    private StandardButton _saveButton;

    private readonly ContentsManager _contents;
    private readonly XDocument _template; // das zu speichernde Template
    private InputBlocker inputBlocker = new InputBlocker();

    public SaveDialog(ContentsManager contents, XDocument template)
        : base(
            contents.GetTexture("WindowBackground.png"),
            new Rectangle(40, 26, 913, 750),
            new Rectangle(70, 71, 839, 644)
        )
    {
        _contents = contents;
        _template = template;

        this.Title = "Template speichern";
        this.Parent = GameService.Graphics.SpriteScreen;
        this.Size = new Point(450, 550);
        this.Location = new Point(600, 300);
        this.SavesPosition = true;
        this.SavesSize = true;
        this.CanResize = true;
        this.ZIndex = 100;

        BuildLayout();
        RefreshList();

        inputBlocker.ZIndex = 99;
        inputBlocker.Visible = true;
        this.Hidden += (s, e) => {
            inputBlocker.Visible = false;
        };

    }

    private void BuildLayout()
    {
        // 🔸 Panel mit Dateiübersicht
        _filePanel = new FlowPanel()
        {
            Parent = this,
            Width = this.ContentRegion.Width,
            Height = this.ContentRegion.Height - 120,
            Location = new Point(this.ContentRegion.X, 0),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ControlPadding = new Vector2(4, 4),
            ShowBorder = true
        };

        // 🔸 Eingabefeld für Dateinamen
        _fileNameBox = new TextBox()
        {
            Parent = this,
            PlaceholderText = "Dateiname eingeben...",
            Width = this.ContentRegion.Width - 20,
            Location = new Point(this.ContentRegion.X + 10, _filePanel.Bottom + 10),
        };

        // 🔸 Speichern-Button
        _saveButton = new StandardButton()
        {
            Parent = this,
            Text = "Speichern",
            Width = this.ContentRegion.Width - 20,
            Location = new Point(this.ContentRegion.X + 10, _fileNameBox.Bottom + 10)
        };
        _saveButton.Click += OnSaveClicked;
    }

    private void RefreshList()
    {
        _filePanel.ClearChildren();

        var folder = GetHomesteadFolder();
        var files = Directory.GetFiles(folder, "*.xml").OrderBy(f => f);

        if (!files.Any())
        {
            new Label()
            {
                Parent = _filePanel,
                Text = "Keine Templates vorhanden.",
                AutoSizeWidth = true
            };
            return;
        }

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            string mapName = XmlLoader.GetMapNameFromPath(file);

            var row = new Panel()
            {
                Parent = _filePanel,
                Width = _filePanel.ContentRegion.Width - 10,
                Height = 30
            };

            // 📄 Dateiname
            new Label()
            {
                Parent = row,
                Text = fileName,
                Location = new Point(5, 5),
                AutoSizeWidth = true
            };

            // 🗺 Mapname
            new Label()
            {
                Parent = row,
                Text = $"{mapName}",
                Location = new Point(180, 5),
                AutoSizeWidth = true
            };
        }
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        string fileName = _fileNameBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            ScreenNotification.ShowNotification("⚠ Bitte Dateinamen eingeben");
            return;
        }

        if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            fileName += ".xml";

        string folder = GetHomesteadFolder();
        string filePath = Path.Combine(folder, fileName);

        if (File.Exists(filePath))
        {
            var confirmDialog = new ConfirmDialog(_contents);

            confirmDialog.confirmed += result =>
            {
                if (!result)
                {
                    ScreenNotification.ShowNotification("Template NOT saved");
                    return; // Speichern abbrechen
                }
                else
                {
                    try
                    {
                        _template.Save(filePath); // XDocument speichern
                        TemplateSaved?.Invoke(filePath);
                        inputBlocker.Visible = false;
                        this.Hide();
                    }
                    catch (Exception ex)
                    {
                        ScreenNotification.ShowNotification($"❌ Fehler beim Speichern: {ex.Message}");
                    }
                }

                
            };
            confirmDialog.Show();
        }
        else
        {
            try
            {
                _template.Save(filePath); // XDocument speichern
                TemplateSaved?.Invoke(filePath);
                inputBlocker.Visible = false;
                this.Hide();
            }
            catch (Exception ex)
            {
                ScreenNotification.ShowNotification($"❌ Fehler beim Speichern: {ex.Message}");
            }
        }

    }

    private string GetHomesteadFolder()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var path = Path.Combine(docs, "Guild Wars 2", "Homesteads");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }



}
