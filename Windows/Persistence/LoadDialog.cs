using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class LoadDialog : StandardWindow
{
    public event Action<string> TemplateSelected;

    private FlowPanel _filePanel;
    private StandardButton _loadButton;

    private string _selectedFile;
    private StandardButton _selectedButton;

    private readonly ContentsManager _contents;

    public LoadDialog(ContentsManager contents)
        : base(
            contents.GetTexture("WindowBackground.png"),
            new Rectangle(40, 26, 913, 750),
            new Rectangle(70, 71, 839, 644)
        )
    {
        _contents = contents;

        this.Title = "Template laden";
        this.Parent = GameService.Graphics.SpriteScreen;
        this.Size = new Point(400, 500);
        this.Location = new Point(600, 300);
        this.SavesPosition = true;
        this.SavesSize = true;
        this.CanResize = true;
        this.ZIndex = 6;

        BuildLayout();
        RefreshList();
    }

    private void BuildLayout()
    {
        // 🔸 Liste der Templates
        _filePanel = new FlowPanel()
        {
            Parent = this,
            Width = this.ContentRegion.Width,
            Height = this.ContentRegion.Height - 50,
            Location = new Point(this.ContentRegion.X, 10),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ControlPadding = new Vector2(4, 4)
        };

        // 🔸 Laden-Button
        _loadButton = new StandardButton()
        {
            Parent = this,
            Text = "Laden",
            Width = this.ContentRegion.Width - 20,
            Location = new Point(this.ContentRegion.X + 10, this.ContentRegion.Height - 40),
            Enabled = false // erst aktiv, wenn eine Datei gewählt wurde
        };
        _loadButton.Click += OnLoadClicked;
    }

    private void RefreshList()
    {
        _filePanel.ClearChildren();
        _selectedFile = null;
        _selectedButton = null;
        _loadButton.Enabled = false;

        var folder = GetHomesteadFolder();
        var files = Directory.GetFiles(folder, "*.xml").OrderBy(f => f);

        if (!files.Any())
        {
            new Label()
            {
                Parent = _filePanel,
                Text = "Keine Templates gefunden.",
                AutoSizeWidth = true
            };
            return;
        }

        foreach (var file in files)
        {
            string name = Path.GetFileName(file);
            string mapName = LoadMapName(file) ?? "(unbekannte Karte)";

            var row = new Panel()
            {
                Parent = _filePanel,
                Width = _filePanel.ContentRegion.Width - 20,
                Height = 30,
                ShowBorder = false
            };

            var btn = new StandardButton()
            {
                Parent = row,
                Text = name,
                Width = row.Width / 2 - 10,
                Height = 30,
                Location = new Point(0, 0)
            };

            var lbl = new Label()
            {
                Parent = row,
                Text = mapName,
                AutoSizeWidth = false,
                Width = row.Width / 2 - 10,
                Height = 30,
                Location = new Point(btn.Width + 10, 0),
                VerticalAlignment = VerticalAlignment.Middle
            };

            btn.Click += (s, e) => SelectFile(file, btn);
        }
    }

    private void SelectFile(string filePath, StandardButton button)
    {
        // Alte Auswahl zurücksetzen
        if (_selectedButton != null)
        {
            _selectedButton.BackgroundColor = Color.Transparent;
        }

        _selectedButton = button;
        _selectedFile = filePath;

        // ✅ Nur der Button selbst wird grün markiert
        button.BackgroundColor = Color.LightGreen;
        _loadButton.Enabled = true;
    }

    private void OnLoadClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_selectedFile))
        {
            TemplateSelected?.Invoke(_selectedFile);
            this.Hide();
        }
    }

    private string LoadMapName(string filePath)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var decorations = doc.Element("Decorations");
            if (decorations != null)
            {
                var attr = decorations.Attribute("mapName");
                return attr?.Value;
            }
        }
        catch
        {
            // Ignoriere Parsingfehler, gib einfach null zurück
        }
        return null;
    }

    private string GetHomesteadFolder()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var path = Path.Combine(docs, "Guild Wars 2", "Homesteads");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
}
