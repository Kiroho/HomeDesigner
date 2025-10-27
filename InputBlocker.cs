using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeDesigner
{
    public class InputBlocker : Control
    {
        public InputBlocker()
        {
            Parent = GameService.Graphics.SpriteScreen;
            Location = new Point(0, 0);
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
            ZIndex = 0; // ganz hinten, hinter allen UI-Elementen
            Visible = false; 
            BackgroundColor = Color.White * 0f; // muss nicht gezeichnet werden

            GameService.Graphics.SpriteScreen.Resized += onResized;
        }

        protected override CaptureType CapturesInput() => CaptureType.Mouse;


        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            
        }

        public void onResized(object sender, ResizedEventArgs e)
        {
            Width = GameService.Graphics.SpriteScreen.Width;
            Height = GameService.Graphics.SpriteScreen.Height;
        }
        
    }

}
