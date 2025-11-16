using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using HomeDesigner;
using Microsoft.Xna.Framework;
using System;

public class ConfirmDialog : StandardWindow
{

    public event Action<bool> confirmed;

    private readonly ContentsManager _contents;
    private InputBlocker inputBlocker = new InputBlocker();

    public ConfirmDialog(ContentsManager contents, String text)
        : base(
            contents.GetTexture("WindowBackground.png"),
            new Rectangle(40, 26, 913, 750),
            new Rectangle(70, 71, 839, 644)
        )
    {
        _contents = contents;

        this.Title = "Confirm";
        this.Parent = GameService.Graphics.SpriteScreen;
        this.Size = new Point(400, 230);
        this.Location = new Point(700, 450);
        this.CanResize = false;
        this.ZIndex = 999;

        inputBlocker.ZIndex = 998;
        inputBlocker.Visible = true;
        this.Hidden += (s, e) =>
        {
            inputBlocker.Visible = false;
        };

        //Text
        var infoText = new Label()
        {
            Parent = this,
            Text = text,
            Width = this.ContentRegion.Width -20,
            Location = new Point(10, 0),
            WrapText = true,
            AutoSizeHeight = true
        };


        // confirm-Button
        var confirmButton = new StandardButton()
        {
            Parent = this,
            Text = "Confirm",
            Width = this.ContentRegion.Width / 2,
            Location = new Point(10, 35)
        };
        confirmButton.Click += OnConfirmClicked;


        // cancel-Button
        var cancelButton = new StandardButton()
        {
            Parent = this,
            Text = "Cancel",
            Width = this.ContentRegion.Width /2,
            Location = new Point(confirmButton.Width + 10, 35)
        };
        cancelButton.Click += OnCancelClicked;
    }

    private void OnCancelClicked(object sender, MouseEventArgs e)
    {
        inputBlocker.Visible = false;
        confirmed?.Invoke(false);
        this.Hide();
    }

    private void OnConfirmClicked(object sender, MouseEventArgs e)
    {
        inputBlocker.Visible = false;
        confirmed?.Invoke(true);
        this.Hide();
    }
}
