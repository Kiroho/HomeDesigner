using Blish_HUD.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using HomeDesigner.Views;

namespace HomeDesigner.Windows
{
    public class Toolbar : StandardWindow
    {
        ContentsManager contents;
        RendererControl rendererControl;
        private FlowPanel mainPanel;
        private Image moveIcon;
        private Image rotateIcon;
        private Image scaleIcon;
        private Image axisIcon;
        private Image copyIcon;
        private Image pasteIcon;
        private Image removeIcon;

        public Toolbar(ContentsManager contents, RendererControl rendererControl, DesignerView designerView)
            : base(
                contents.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 750),
                new Rectangle(40, 26, 913, 750)
            )
        {
            this.contents = contents;
            this.rendererControl = rendererControl;
            Parent = GameService.Graphics.SpriteScreen;
            Size = new Point(590, 150);
            Location = new Point(800, 950);
            SavesPosition = true;
            SavesSize = true;
            CanResize = true;
            Title = "";
            
            



            mainPanel = new FlowPanel()
            {
                ShowBorder = false,
                Size = new Point(this.ContentRegion.Width, this.ContentRegion.Height),
                Location = new Point(0, 0),
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                Parent = this,
                ControlPadding = new Vector2(15, 5),
                OuterControlPadding = new Vector2(15, 5)

            };

            moveIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Move",
                ZIndex = 1,
                Parent = mainPanel,
                BackgroundColor = Color.LightBlue
        };
            moveIcon.Click += (s, e) =>
            {
                designerView.SetTransformMode(RendererControl.TransformMode.Translate);
                highlightIcon(moveIcon);
            };

            rotateIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Rotate",
                ZIndex = 1,
                Parent = mainPanel
            };
            rotateIcon.Click += (s, e) =>
            {
                designerView.SetTransformMode(RendererControl.TransformMode.Rotate);
                highlightIcon(rotateIcon);
            };

            scaleIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Scale",
                ZIndex = 1,
                Parent = mainPanel
            };
            scaleIcon.Click += (s, e) =>
            {
                designerView.SetTransformMode(RendererControl.TransformMode.Scale);
                highlightIcon(scaleIcon);
            };

            axisIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Axis",
                ZIndex = 1,
                Parent = mainPanel,
                BackgroundColor = Color.Blue
            };
            axisIcon.Click += (s, e) =>
            {
                // Set World Axis
                if (rendererControl._rotationSpace == RendererControl.RotationSpace.Local)
                {
                    rendererControl._rotationSpace = RendererControl.RotationSpace.World;
                    rendererControl.setPivotRotation(Quaternion.Identity);
                    rendererControl.updateGizmos();
                    axisIcon.BackgroundColor = Color.Blue;

                } // Set Local Axis
                else if (rendererControl._rotationSpace == RendererControl.RotationSpace.World)
                {
                    rendererControl._rotationSpace = RendererControl.RotationSpace.Local;
                    if (rendererControl.SelectedObjects.Count > 0)
                    {
                        rendererControl.setPivotRotation(rendererControl.SelectedObjects[0].RotationQuaternion);
                    }
                    rendererControl.updateGizmos();
                    axisIcon.BackgroundColor = Color.Yellow;
                }
            };

            copyIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Copy",
                ZIndex = 1,
                Parent = mainPanel
            };
            copyIcon.Click += (s, e) =>
            {
                designerView.CopySelectedObject();
            };

            pasteIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Paste",
                ZIndex = 1,
                Parent = mainPanel
            };
            pasteIcon.Click += (s, e) =>
            {
                designerView.PasteObject();
            };

            removeIcon = new Image(contents.GetTexture("Icons/placeholder.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Remove Selection",
                ZIndex = 1,
                Parent = mainPanel
            };
            removeIcon.Click += (s, e) =>
            {
                designerView.RemoveSelectedObject();
            };



            this.Resized += (_, e) =>
            {
                mainPanel.Width = this.ContentRegion.Width;
                mainPanel.Height = this.ContentRegion.Height;

            };
        }


        private void highlightIcon(Image image)
        {
            moveIcon.BackgroundColor = Color.Transparent;
            rotateIcon.BackgroundColor = Color.Transparent;
            scaleIcon.BackgroundColor = Color.Transparent;
            image.BackgroundColor = Color.LightBlue;
        }



    }
}
