using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using System;
using HomeDesigner;

public class TextDialog : StandardWindow
{
    public event Action<string> TextConfirmed;

    private TextBox _fileNameBox;
    private StandardButton _confirmButton;

    private InputBlocker inputBlocker = new InputBlocker();

    public TextDialog(ContentsManager contents)
        : base(
            contents.GetTexture("WindowBackground.png"),
            new Rectangle(40, 26, 913, 750),
            new Rectangle(50, 41, 863, 709)
        )
    {

        this.Title = "Text of your Decoration";
        this.Parent = GameService.Graphics.SpriteScreen;
        this.Size = new Point(450, 200);
        this.Location = new Point(600, 300);
        this.SavesPosition = true;
        this.SavesSize = true;
        this.CanResize = true;
        this.ZIndex = 100;

        BuildLayout();

        inputBlocker.ZIndex = 99;
        inputBlocker.Visible = true;
        this.Hidden += (s, e) =>
        {
            inputBlocker.Visible = false;
        };

    }

    private void BuildLayout()
    {

        // File name input
        _fileNameBox = new TextBox()
        {
            Parent = this,
            PlaceholderText = "Text...",
            Width = this.ContentRegion.Width - 20,
            Location = new Point(5, 0)
        };

        // Confirm Button
        _confirmButton = new StandardButton()
        {
            Parent = this,
            Text = "Confirm",
            Width = this.ContentRegion.Width - 20,
            Location = new Point(5, _fileNameBox.Bottom + 15)
        };
        _confirmButton.Click += OnConfirmClicked;
    }

    

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        string fileName = _fileNameBox.Text?.Trim();
        TextConfirmed?.Invoke(_fileNameBox.Text);
        inputBlocker.Visible = false;
        this.Hide();


    }



}
