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
        private Image rectangleIcon;
        private Image lassoIcon;

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
            Size = new Point(730, 190);
            Location = new Point(680, 950);
            SavesPosition = true;
            SavesSize = true;
            //CanResize = true;
            Title = "";


            var undoButton = new StandardButton()
            {
                Parent = this,
                Text = "Undo",
                Location = new Point(15, 0),
                Size = new Point(50, 30)
            };
            undoButton.Click += (s, e) =>
            {
                if (rendererControl.historyPosition > 1)
                {
                    rendererControl.ClearSelection();
                    rendererControl.historyPosition--;
                    rendererControl.loadHistory();
                    rendererControl.ClearSelection();
                }
            };

            var redoButton = new StandardButton()
            {
                Parent = this,
                Text = "Redo",
                Location = new Point(75, 0),
                Size = new Point(50, 30)
            };
            redoButton.Click += (s, e) =>
            {
                if (rendererControl.historyPosition < rendererControl.HistoryList.Count)
                {
                    rendererControl.ClearSelection();
                    rendererControl.historyPosition++;
                    rendererControl.loadHistory();
                    rendererControl.ClearSelection();
                }
            };

            var multiSelect = new Checkbox()
            {
                Parent = this,
                Text = "Set Multi",
                BasicTooltipText = "Check to enable selection multiple rectanlges/lassos.\nUncheck to make a new selection each rectangle/lasso.",
                Location = new Point(570, 0),
                Size = new Point(100, 30)
            };
            multiSelect.CheckedChanged += (s, e) =>
            {
                if (multiSelect.Checked)
                {
                    rendererControl.multiLassoSelect = true;
                }
                else
                {
                    rendererControl.multiLassoSelect = false;
                }
            };

            var selectionBase = new Checkbox()
            {
                Parent = this,
                Text = "Set Base",
                BasicTooltipText = "Check to set Rectangle/Lasso's base to player's height.\nUncheck to set base to ground level.",
                Location = new Point(490, 0),
                Size = new Point(100, 30)
            };
            selectionBase.CheckedChanged += (s, e) =>
            {
                if (selectionBase.Checked)
                {
                    rendererControl.planeZ = GameService.Gw2Mumble.PlayerCharacter.Position.Z;
                }
                else
                {
                    if (GameService.Gw2Mumble.CurrentMap.Id == 1596) //Heartglow
                    {
                        rendererControl.planeZ = 1f;
                    }
                    else if (GameService.Gw2Mumble.CurrentMap.Id == 1558) //Comosus
                    {
                        rendererControl.planeZ = 15f;
                    }
                }
            };



            mainPanel = new FlowPanel()
            {
                ShowBorder = false,
                Size = new Point(this.ContentRegion.Width, this.ContentRegion.Height),
                Location = new Point(0, 35),
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                Parent = this,
                ControlPadding = new Vector2(15, 5),
                OuterControlPadding = new Vector2(15, 5)

            };

            moveIcon = new Image(contents.GetTexture("Icons/Move.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Move",
                ZIndex = 1,
                Parent = mainPanel,
                Tint = new Color(250, 250, 80, 128)
            };
            moveIcon.Click += (s, e) =>
            {
                designerView.SetTransformMode(RendererControl.TransformMode.Translate);
                highlightIcon(moveIcon);
            };

            rotateIcon = new Image(contents.GetTexture("Icons/Rotate.png"))
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

            scaleIcon = new Image(contents.GetTexture("Icons/Scale.png"))
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

            axisIcon = new Image(contents.GetTexture("Icons/Axis_World.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Axis",
                ZIndex = 1,
                Parent = mainPanel
            };
            axisIcon.Click += (s, e) =>
            {
                // Set World Axis
                if (rendererControl._rotationSpace == RendererControl.RotationSpace.Local)
                {
                    rendererControl._rotationSpace = RendererControl.RotationSpace.World;
                    rendererControl.setPivotRotation(Quaternion.Identity);
                    rendererControl.updateGizmos();
                    axisIcon.Texture = contents.GetTexture("Icons/Axis_world.png");
                    axisIcon.BasicTooltipText = "Axis: World";

                } // Set Local Axis
                else if (rendererControl._rotationSpace == RendererControl.RotationSpace.World)
                {
                    rendererControl._rotationSpace = RendererControl.RotationSpace.Local;
                    if (rendererControl.SelectedObjects.Count > 0)
                    {
                        rendererControl.setPivotRotation(rendererControl.SelectedObjects[0].RotationQuaternion);
                    }
                    rendererControl.updateGizmos();
                    axisIcon.BasicTooltipText = "Axis: Local";
                    axisIcon.Texture = contents.GetTexture("Icons/Axis_Local.png");
                }
            };

            copyIcon = new Image(contents.GetTexture("Icons/Copy.png"))
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

            pasteIcon = new Image(contents.GetTexture("Icons/Paste.png"))
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

            rectangleIcon = new Image(contents.GetTexture("Icons/Rectangle.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Rectangle Selection",
                ZIndex = 1,
                Parent = mainPanel
            };
            rectangleIcon.Click += (s, e) =>
            {
                designerView.UnsubscribeSelectionEvents();
                lassoIcon.Tint = Color.White;
                rendererControl.StartRectangleSelection();
                designerView.SubscribeSelectionEvents();
            };

            lassoIcon = new Image(contents.GetTexture("Icons/Lasso.png"))
            {
                Size = new Point(64, 64),
                BasicTooltipText = "Lasso Selection",
                ZIndex = 1,
                Parent = mainPanel
            };
            lassoIcon.Click += (s, e) =>
            {
                designerView.UnsubscribeSelectionEvents();
                designerView.SubscribeSelectionEvents();
                if (rendererControl._selectionMode == RendererControl.SelectionMode.None)
                {
                    lassoIcon.Tint = new Color(250, 250, 80, 128);
                }
                else if (rendererControl._selectionMode == RendererControl.SelectionMode.PolygonPoints)
                {
                    //designerView.UnsubscribeSelectionEvents();
                    lassoIcon.Tint = Color.White;
                }
                rendererControl.PolygonSelection();
            };


            removeIcon = new Image(contents.GetTexture("Icons/Remove.png"))
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




            //this.Resized += (_, e) =>
            //{
            //    mainPanel.Width = this.ContentRegion.Width;
            //    mainPanel.Height = this.ContentRegion.Height;

            //};
        }


        private void highlightIcon(Image image)
        {
            moveIcon.Tint = Color.White;
            rotateIcon.Tint = Color.White;
            scaleIcon.Tint = Color.White;
            image.Tint = new Color(250, 250, 80, 128);
        }

        public void unload()
        {
            moveIcon.Texture?.Dispose();
            rotateIcon.Texture?.Dispose();
            scaleIcon.Texture?.Dispose();
            axisIcon.Texture?.Dispose();
            copyIcon.Texture?.Dispose();
            pasteIcon.Texture?.Dispose();
            removeIcon.Texture?.Dispose();
            rectangleIcon.Texture?.Dispose();
            lassoIcon.Texture?.Dispose();
            moveIcon?.Dispose();
            rotateIcon?.Dispose();
            scaleIcon?.Dispose();
            axisIcon?.Dispose();
            copyIcon?.Dispose();
            pasteIcon?.Dispose();
            removeIcon?.Dispose();
            rectangleIcon?.Dispose();
            lassoIcon?.Dispose();
        }

    }
}
