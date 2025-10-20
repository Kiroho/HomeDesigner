using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using System;

public class ConfirmDialog : StandardWindow
{

    public event Action<bool> confirmed;

    private readonly ContentsManager _contents;

    public ConfirmDialog(ContentsManager contents)
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
        this.ZIndex = 10;

        //Text
        var infoText = new Label()
        {
            Parent = this,
            Text = "Do you really want to overwrite this file?",
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
        confirmed?.Invoke(false);  // Event feuern
        this.Hide();
    }

    private void OnConfirmClicked(object sender, MouseEventArgs e)
    {
        confirmed?.Invoke(true);   // Event feuern
        this.Hide();
    }
}
