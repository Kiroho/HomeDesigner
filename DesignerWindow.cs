using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;

namespace HomeDesigner
{

    public class DesignerWindow : StandardWindow
    {

        public DesignerWindow(ContentsManager contents)
            : base(
                contents.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 691),   // Außenrahmen (Fenstergröße)
                new Rectangle(70, 71, 839, 605))  // Innenbereich (wo Controls rein kommen)
        {
            this.Title = "Home Designer";
            this.Parent = GameService.Graphics.SpriteScreen;

            this.SavesPosition = true;
            this.SavesSize = true;
            this.CanResize = true;
            this.Id = "HomeDesigner.MainWindow";
        }
    }
}
