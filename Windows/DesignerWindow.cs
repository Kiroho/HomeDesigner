using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using HomeDesigner.Views;
using System;

namespace HomeDesigner
{

    public class DesignerWindow : TabbedWindow2
    {

        private RendererControl rendererControl;
        private BlueprintRenderer blueprintRenderer;
        private ContentsManager contents;

        private Tab mergerTab;
        private Tab differTab;
        private Tab designerTab;

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

            mergerTab = new Tab(
                Content.GetTexture("155052"),
                () => new TemplateMergerView(contents),
                "Template Manager"
            );

            differTab = new Tab(
                Content.GetTexture("155052"),
                () => new TemplateDifferenceView(contents),
                "Template Manager"
            );

            this.Tabs.Add(mergerTab);
            this.Tabs.Add(differTab);

            this.Resized += resized;
        }

        private void resized(object sender, ResizedEventArgs e)
        {
            this.ContentRegion.WithSetDimension(this.WindowRegion.X+30, this.WindowRegion.Y+40, this.WindowRegion.Width-60, this.WindowRegion.Height-80);
        }

        public void addDesignerTab()
        {
            designerTab = new Tab(
                Content.GetTexture("155052"),
                () => new DesignerView(rendererControl, blueprintRenderer, contents),
                "Designer"
            );
            this.Tabs.Add(designerTab);
        }

        public void removeDesignerTab()
        {
            this.Tabs.Remove(designerTab);
        }

        public void setRendererControl(RendererControl control)
        {
            rendererControl = control;
        }

        public void setBlueprintRenderer(BlueprintRenderer renderer)
        {
            blueprintRenderer = renderer;
        }

    }
}
