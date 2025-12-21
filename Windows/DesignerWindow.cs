using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using HomeDesigner.Views;
using System;
using HomeDesigner.Windows;

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

        public DesignerView designerView;
        public TemplateMergerView templateMergerView;
        public TemplateDifferenceView templateDifferenceView;

        public DesignerWindow(ContentsManager contents, RendererControl rendererControl, BlueprintRenderer blueprintRenderer)
            : base(
                contents.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 700),
                new Rectangle(70, 40, 865, 650)
            )
        {
            this.rendererControl = rendererControl;
            this.blueprintRenderer = blueprintRenderer;
            this.contents = contents;

            this.Title = "Home Designer";
            this.Parent = GameService.Graphics.SpriteScreen;
            this.Size = new Point(700, 750);
            this.SavesPosition = true;
            this.SavesSize = true;
            this.CanResize = true;
            this.Id = "HomeDesigner.MainWindow";
            this.ZIndex = 0;

            designerView = new DesignerView(rendererControl, blueprintRenderer, contents);
            templateMergerView = new TemplateMergerView(contents);
            templateDifferenceView = new TemplateDifferenceView(contents);

            designerTab = new Tab(
                contents.GetTexture("Icons/Designer.png"),
                () => designerView,
                "Designer"
            );

            mergerTab = new Tab(
                contents.GetTexture("Icons/Merge.png"),
                () => templateMergerView,
                "Template Merger"
            );

            differTab = new Tab(
                contents.GetTexture("Icons/Differ.png"),
                () => templateDifferenceView,
                "Template Cutter"
            );

            this.Tabs.Add(designerTab);
            this.Tabs.Add(mergerTab);
            this.Tabs.Add(differTab);


            this.Resized += resized;

        }

        private void resized(object sender, ResizedEventArgs e)
        {
            this.ContentRegion.WithSetDimension(this.WindowRegion.X+30, this.WindowRegion.Y+40, this.WindowRegion.Width-60, this.WindowRegion.Height-80);
        }

        public void unload()
        {
            this.Resized -= resized;
            designerView?.unload();
            designerTab.Icon?.Dispose();
            mergerTab.Icon?.Dispose();
            differTab.Icon?.Dispose();
            designerView?.DoUnload();
            templateMergerView?.DoUnload();
            templateDifferenceView?.DoUnload();
        }

    }
}
