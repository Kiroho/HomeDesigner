using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using HomeDesigner.Views;

namespace HomeDesigner
{

    public class DesignerWindow : TabbedWindow2
    {

        private RendererControl rendererControl;
        private BlueprintRenderer blueprintRenderer;
        private ContentsManager contents;

        public DesignerWindow(ContentsManager contents, RendererControl rendererControl, BlueprintRenderer blueprintRenderer)
            : base(
                contents.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 750),
                new Rectangle(70, 71, 839, 644)
            )
        {
            this.rendererControl = rendererControl;
            this.blueprintRenderer = blueprintRenderer;
            this.contents = contents;

            this.Title = "Home Designer";
            this.Parent = GameService.Graphics.SpriteScreen;
            this.Emblem = contents.GetTexture("CornerIcon.png");
            this.SavesPosition = true;
            this.SavesSize = true;
            this.CanResize = true;
            this.Id = "HomeDesigner.MainWindow";
            this.ZIndex = 0;

            BuildTabs();
        }

        private void BuildTabs()
        {

            this.Tabs.Add(new Tab(
                Content.GetTexture("155052"),
                () => new TemplateManagerView(contents),
                "Template Manager"
            ));


            this.Tabs.Add(new Tab(
                Content.GetTexture("155052"),
                () => new DesignerView(rendererControl, blueprintRenderer, contents),
                "Designer"
            ));

        }
    }
}
