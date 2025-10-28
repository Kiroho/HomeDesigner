using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using HomeDesigner.Loader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeDesigner.Views
{

    public class DesignerView : View
    {

        private readonly ContentsManager contents;
        private Dictionary<BlueprintObject, Quaternion> _startRotations = new Dictionary<BlueprintObject, Quaternion>();
        private Dictionary<BlueprintObject, Vector3> _startPositions = new Dictionary<BlueprintObject, Vector3>();
        private Vector3? _rotationPivot = null;
        private bool _isResettingSliders = false;
        private bool _isDraggingSlider = false;
        
        private Checkbox _worldAxisCheckbox;
        private Checkbox _localAxisCheckbox;


        private StandardButton _translateButton;
        private StandardButton _rotateButton;
        private StandardButton _scaleButton;


        private FlowPanel modelListPanel = new FlowPanel();
        private RendererControl rendererControl;
        private BlueprintRenderer blueprintRenderer;
        private string selectedModelKey;
        public Vector3 CopiedPivot { get; private set; }

        public DesignerView(RendererControl rendererControl, BlueprintRenderer blueprintRenderer, ContentsManager contents)
        {
            this.rendererControl = rendererControl;
            this.blueprintRenderer = blueprintRenderer;
            this.contents = contents;
        }

        protected override void Build(Container buildPanel)
        {
            //Mainpanel
            var mainPanel = new FlowPanel()
            {
                Size = new Point(buildPanel.Width, buildPanel.Height),
                Location = new Point(30, 50),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Parent = buildPanel,
                CanScroll = true,
                Padding = new Thickness(20)
            };


            // Place button
            var placeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Place Model",
                Width = buildPanel.ContentRegion.Width -30,
                Height = 45,
                Location = new Point(30, 150)
            };

            placeButton.Click += (s, e) => PlaceSelectedModel();

            //  Panel for Model list
            modelListPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width-30, 150),
                Location = new Point(30, 0),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true
            };

            RefreshModelList();

            
            //Modus Buttons
            _translateButton = new StandardButton()
            {
                Parent = buildPanel,
                Location = new Point(30, 220),
                Text = "Move",
                Width = 100
            };

            _rotateButton = new StandardButton()
            {
                Parent = buildPanel,
                Location = new Point(230, 220),
                Text = "Rotate",
                Width = 100
            };

            _scaleButton = new StandardButton()
            {
                Parent = buildPanel,
                Location = new Point(430, 220),
                Text = "Scale",
                Width = 100
            };

            _translateButton.Click += (s, e) => SetTransformMode(RendererControl.TransformMode.Translate);
            _rotateButton.Click += (s, e) => SetTransformMode(RendererControl.TransformMode.Rotate);
            _scaleButton.Click += (s, e) => SetTransformMode(RendererControl.TransformMode.Scale);
            _translateButton.BackgroundColor = Color.LightGreen;



            //Rotationsachse ändern
            _worldAxisCheckbox = new Checkbox()
            {
                Parent = buildPanel,
                Text = "World Axis",
                Checked = true,
                Location = new Point(230, 260),
                Enabled = false
            };

            _worldAxisCheckbox.CheckedChanged += (s, e) =>
            {
                if (_worldAxisCheckbox.Checked)
                {
                    _localAxisCheckbox.Checked = false;
                    rendererControl._rotationSpace = RendererControl.RotationSpace.World;

                    rendererControl.setPivotRotation(Quaternion.Identity);
                    rendererControl.updateGizmos();

                    _localAxisCheckbox.Enabled = true;
                    _worldAxisCheckbox.Enabled = false;
                }
            };


            _localAxisCheckbox = new Checkbox()
            {
                Parent = buildPanel,
                Text = "Local Axis",
                Checked = false,
                Location = new Point(230, 280)
            };

            _localAxisCheckbox.CheckedChanged += (s, e) =>
            {
                if (_localAxisCheckbox.Checked)
                {
                    _worldAxisCheckbox.Checked = false;
                    rendererControl._rotationSpace = RendererControl.RotationSpace.Local;

                    if (rendererControl.SelectedObjects.Count > 0)
                    {
                        rendererControl.setPivotRotation(rendererControl.SelectedObjects[0].RotationQuaternion);
                    }
                    rendererControl.updateGizmos();

                    _localAxisCheckbox.Enabled = false;
                    _worldAxisCheckbox.Enabled = true;
                }
            };



            var removeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Remove Objects",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 450)
            };
            removeButton.Click += (s, e) => RemoveSelectedObject();


            var copyButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Copy Object",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 510)
            };
            copyButton.Click += (s, e) => CopySelectedObject();


            var pasteButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Paste Object",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 560)
            };
            pasteButton.Click += (s, e) => PasteObject();


            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Template",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 620)
            };
            saveButton.Click += (s, e) => SaveTemplate();


            var loadButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Load Template",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 680)
            };
            loadButton.Click += (s, e) => LoadTemplate();

            var saveSelectedButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Selection as Template",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 740)
            };
            saveSelectedButton.Click += (s, e) => SaveSelectionTemplate();

            var addButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Add Template",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 800)
            };
            addButton.Click += (s, e) => AddTemplate();

        }



        private void SetTransformMode(RendererControl.TransformMode mode)
        {
            rendererControl.currentMode = mode;

            // Farben definieren
            var activeColor = Color.LightGreen;
            var inactiveColor = Color.Transparent;

            // Alle Buttons zurücksetzen
            _translateButton.BackgroundColor = inactiveColor;
            _rotateButton.BackgroundColor = inactiveColor;
            _scaleButton.BackgroundColor = inactiveColor;

            // Aktiven Button hervorheben
            switch (mode)
            {
                case RendererControl.TransformMode.Translate:
                    _translateButton.BackgroundColor = activeColor;
                    rendererControl.gizmoMode = RendererControl.GizmoMode.Translate;
                    break;
                case RendererControl.TransformMode.Rotate:
                    _rotateButton.BackgroundColor = activeColor;
                    rendererControl.gizmoMode = RendererControl.GizmoMode.Rotate;
                    break;
                case RendererControl.TransformMode.Scale:
                    _scaleButton.BackgroundColor = activeColor;
                    rendererControl.gizmoMode = RendererControl.GizmoMode.Scale;
                    break;
            }
        }



        private void RefreshModelList()
        {
            modelListPanel.ClearChildren();

            foreach (var key in blueprintRenderer.GetModelKeys())
            {
                var btn = new StandardButton()
                {
                    Parent = modelListPanel,
                    Text = key,
                    Width = modelListPanel.ContentRegion.Width - 20,
                    Height = 30
                };

                btn.Click += (s, e) => {
                    selectedModelKey = key;
                    //ScreenNotification.ShowNotification($"Modell ausgewählt: {selectedModelKey}");
                    HighlightSelectedButton(btn);
                };
            }
        }


        private void HighlightSelectedButton(StandardButton selectedBtn)
        {
            // Alle Buttons zurücksetzen
            foreach (var child in modelListPanel.Children.OfType<StandardButton>())
            {
                child.BasicTooltipText = null;
                child.BackgroundColor = Color.Transparent;
            }

            // Gewählten Button hervorheben
            selectedBtn.BasicTooltipText = "Currently Selected";
            selectedBtn.BackgroundColor = Color.LightGreen;
        }


        private void PlaceSelectedModel()
        {
            if (string.IsNullOrEmpty(selectedModelKey))
            {
                //ScreenNotification.ShowNotification("⚠ Kein Modell ausgewählt!");
                return;
            }

            var newObj = new BlueprintObject()
            {
                ModelKey = selectedModelKey,
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Scale = 0.028f
            };

            rendererControl.AddObject(newObj);

            //ScreenNotification.ShowNotification($" {selectedModelKey} platziert");
        }

        private void RemoveSelectedObject()
        {
            rendererControl.Objects.RemoveAll(o => rendererControl.SelectedObjects.Contains(o));
            rendererControl.SelectedObjects.Clear();
            ScreenNotification.ShowNotification("Selection Removed");
        }

        private void CopySelectedObject()
        {
            if (rendererControl.SelectedObjects.Count == 0)
            {
                ScreenNotification.ShowNotification("No Object Selected");
                return;
            }

            CopiedPivot = rendererControl.getPivotObject();
            rendererControl.CopiedObjects.Clear();

            foreach (var obj in rendererControl.SelectedObjects)
            {
                var copy = new BlueprintObject
                {
                    ModelKey = obj.ModelKey,
                    Position = obj.Position,
                    Rotation = obj.Rotation,
                    RotationQuaternion = obj.RotationQuaternion,
                    Scale = obj.Scale,
                    CachedWorld = obj.CachedWorld
                };

                rendererControl.CopiedObjects.Add(copy);
            }
            ScreenNotification.ShowNotification("Selection Copied");
        }



        private void PasteObject()
        {
            if (rendererControl.CopiedObjects.Count == 0)
            {
                ScreenNotification.ShowNotification("No Object To Paste");
                return;
            }

            var oldPivot = CopiedPivot;

            rendererControl.ClearSelection();

            foreach (var obj in rendererControl.CopiedObjects)
            {
                var offset = obj.Position - oldPivot;
                var newPosition = GameService.Gw2Mumble.PlayerCharacter.Position + offset;

                var copy = new BlueprintObject
                {
                    ModelKey = obj.ModelKey,
                    Position = newPosition,
                    Rotation = obj.Rotation,
                    RotationQuaternion = obj.RotationQuaternion,
                    Scale = obj.Scale,
                    CachedWorld = obj.CachedWorld,
                    BoundingBox = obj.BoundingBox
                };

                rendererControl.AddObject(copy);
                //rendererControl.SelectObject(obj, true);
            }

            ScreenNotification.ShowNotification("Copied Objects Pasted");
        }

        private void SaveTemplate()
        {
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.Objects);
            var saveDialog = new SaveDialog(contents, xmlDoc);

            saveDialog.TemplateSaved += (path) =>
            {
                ScreenNotification.ShowNotification($"Template Saved");
            };
            saveDialog.Show();
        }

        private void SaveSelectionTemplate()
        {
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.SelectedObjects);
            var saveDialog = new SaveDialog(contents, xmlDoc);

            saveDialog.TemplateSaved += (path) =>
            {
                ScreenNotification.ShowNotification($"Template Saved");
            };
            saveDialog.Show();
        }




        private void LoadTemplate()
        {
            var loadDialog = new LoadDialog(contents);

            loadDialog.TemplateSelected += (path) =>
            {
                try
                {
                    rendererControl.Objects.Clear();
                    rendererControl.SelectedObjects.Clear();

                    List<BlueprintObject> loadedObjects = XmlLoader.LoadBlueprintObjectsFromXml(path);

                    foreach (var obj in loadedObjects)
                    {
                        rendererControl.Objects.Add(obj);
                    }
                    rendererControl.updateWorld();

                    //ScreenNotification.ShowNotification("Objekte: "+rendererControl.Objects.Count());
                    ScreenNotification.ShowNotification("Template Loaded");
                }
                catch
                {
                    ScreenNotification.ShowNotification("Loading Failed");
                }
                //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");
            };

            loadDialog.Show();
            
        }


        private void AddTemplate()
        {
            var loadDialog = new LoadDialog(contents);

            loadDialog.TemplateSelected += (path) =>
            {
                try
                {
                    rendererControl.SelectedObjects.Clear();

                    List<BlueprintObject> loadedObjects = XmlLoader.LoadBlueprintObjectsFromXml(path);

                    foreach (var obj in loadedObjects)
                    {
                        rendererControl.Objects.Add(obj);
                    }
                    rendererControl.updateWorld();

                    //ScreenNotification.ShowNotification("Objekte: "+rendererControl.Objects.Count());
                    ScreenNotification.ShowNotification("Template Added");
                }
                catch
                {
                    ScreenNotification.ShowNotification("Loading Failed");
                }
                //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");
            };

            loadDialog.Show();

        }


    }




}
