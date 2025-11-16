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
using System.Diagnostics;
using System.Linq;

namespace HomeDesigner.Views
{

    public class DesignerView : View
    {

        private readonly ContentsManager contents;
        private Dictionary<BlueprintObject, Quaternion> _startRotations = new Dictionary<BlueprintObject, Quaternion>();
        private Dictionary<BlueprintObject, Vector3> _startPositions = new Dictionary<BlueprintObject, Vector3>();
        
        private Checkbox _worldAxisCheckbox;
        private Checkbox _localAxisCheckbox;


        private StandardButton _translateButton;
        private StandardButton _rotateButton;
        private StandardButton _scaleButton;


        private FlowPanel modelListPanel = new FlowPanel();
        private RendererControl rendererControl;
        private BlueprintRenderer blueprintRenderer;
        private int selectedModelKey=-1;
        private FlowPanel selectedObjectsPanel = new FlowPanel();
        public Vector3 CopiedPivot { get; private set; }

        private StandardButton _btnRectSelect;
        private StandardButton _btnPolySelect;
        private StandardButton redoButton;
        private StandardButton undoButton;

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

            //  Panel for Model list
            modelListPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width - 30, 150),
                Location = new Point(30, 0),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true
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



            //Achse ändern
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


            // 🔹 Rechteck-Auswahl starten
            _btnRectSelect = new StandardButton
            {
                Parent = buildPanel,
                Text = "Rectangle Selection",
                Location = new Point(30, 320),
                Size = new Point(140, 30)
            };
            _btnRectSelect.Click += (s, e) =>
            {
                rendererControl.StartRectangleSelection();
                SubscribeSelectionEvents();
            };

            // 🔹 Polygon-Auswahl starten/beenden
            _btnPolySelect = new StandardButton
            {
                Parent = buildPanel,
                Text = "Polygon Selection",
                Location = new Point(200, 320),
                Size = new Point(140, 30)
            };
            _btnPolySelect.Click += (s, e) =>
            {
                rendererControl.StartPolygonSelection();
                if (rendererControl.IsSelectingPolygon)
                {
                    SubscribeSelectionEvents();
                    _btnPolySelect.Text = "Confirm Polygon";
                }
                else
                {
                    UnsubscribeSelectionEvents();
                    _btnPolySelect.Text = "Polygon Selection";
                }
                    
            };


            //  Panel for Selected Object list
            selectedObjectsPanel = new FlowPanel()
            {
                Parent = buildPanel,
                Size = new Point(350, 150),
                Location = new Point(450, 270),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true
            };

            //  Panel for Selected Object list
            var instruction = new Label()
            {
                Parent = buildPanel,
                Size = new Point(550, 300),
                Location = new Point(380, 430),
                Text="Tipps:" +
                "\n- Select a decoration of the list and press 'place model' \n   to create a blueprint on players position" +
                "\n- Blueprints resemble ingame decorations"+
                "\n- For editing blueprints, select them and click on \n   the axis once, move the mouse and clicke again to confirm" +
                "\n- Press T to cancel your edit" +
                "\n- Press 7 to create a copy at the new position" +
                "\n- Hold Shift when rotating to rotate on 45° steps" +
                "\n- For Rectangle selection: click once, \n   span your rectangle, click to confirm" +
                "\n- For Polygon selection: click to set points \n   of your polygon, click button to confirm" +
                "\n- Both selections are set at the base ground, \n   so check perspective when being above/below" +
                "\n- This is a very early version, so backup your \n   templates before overwriting. Just in case."
            };





            RefreshSelectedList();

            undoButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Undo",
                Location = new Point(30, 380),
                Size = new Point(140, 30)
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

            redoButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Redo",
                Location = new Point(200, 380),
                Size = new Point(140, 30)
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



            var removeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Remove Objects",
                Location = new Point(30, 450),
                Size = new Point(140, 30)
            };
            removeButton.Click += (s, e) => RemoveSelectedObject();


            var removeAllButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Remove All Objects",
                Location = new Point(200, 450),
                Size = new Point(140, 30)
            };
            removeAllButton.Click += (s, e) => RemoveAllObjects();


            var copyButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Copy Object",
                Size = new Point(140, 30),
                Location = new Point(30, 500)
            };
            copyButton.Click += (s, e) => CopySelectedObject();


            var pasteButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Paste Object",
                Size = new Point(140, 30),
                Location = new Point(200, 500)
            };
            pasteButton.Click += (s, e) => PasteObject();


            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Template",
                Size = new Point(140, 30),
                Location = new Point(30, 550)
            };
            saveButton.Click += (s, e) => SaveTemplate();


            var loadButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Load Template",
                Size = new Point(140, 30),
                Location = new Point(200, 550)
            };
            loadButton.Click += (s, e) => LoadTemplate();

            var saveSelectedButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Save Selection",
                Size = new Point(140, 30),
                Location = new Point(30, 600)
            };
            saveSelectedButton.Click += (s, e) => SaveSelectionTemplate();

            var addButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Add Template",
                Size = new Point(140, 30),
                Location = new Point(200, 600)
            };
            addButton.Click += (s, e) => AddTemplate();


            

        }


        private void SubscribeSelectionEvents()
        {
            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseSelection;
        }

        public void UnsubscribeSelectionEvents()
        {
            GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseSelection;
        }

        private void OnLeftMouseSelection(object sender, MouseEventArgs e)
        {
            rendererControl.OnSelectionClick(this);
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
                StandardButton btn;
                if (int.TryParse(key, out int intKey))
                {
                     btn = new StandardButton()
                    {
                        Parent = modelListPanel,
                        Text = blueprintRenderer.decorationLut.decorations[intKey].name,
                        Width = modelListPanel.ContentRegion.Width - 20,
                        Height = 30
                    };

                    btn.Click += (s, e) => {
                        selectedModelKey = intKey;
                        //ScreenNotification.ShowNotification($"Modell ausgewählt: {selectedModelKey}");
                        HighlightSelectedButton(btn);
                    };

                }
                else
                {
                    Debug.WriteLine($"\n[Fehler] Model Key {key} konnte nicht in int übersetzt werden.\n");
                }

            }
        }


        public void RefreshSelectedList()
        {
            selectedObjectsPanel.ClearChildren();


            if (!rendererControl.SelectedObjects.Any())
            {
                new Label()
                {
                    Parent = selectedObjectsPanel,
                    Text = "No Objects Selected.",
                    AutoSizeWidth = true
                };
                return;
            }

            foreach (var obj in rendererControl.SelectedObjects)
            {

                var row = new Panel()
                {
                    Parent = selectedObjectsPanel,
                    Width = selectedObjectsPanel.ContentRegion.Width - 10,
                    Height = 25
                };

                // File name
                new Label()
                {
                    Parent = row,
                    Text = obj.Name,
                    Location = new Point(5, 5),
                    AutoSizeWidth = true
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
            if (selectedModelKey==-1)
            {
                //ScreenNotification.ShowNotification("⚠ Kein Modell ausgewählt!");
                return;
            }

            var newObj = new BlueprintObject()
            {
                ModelKey = selectedModelKey.ToString(),
                Name = blueprintRenderer.decorationLut.decorations[selectedModelKey].name,
                Position = GameService.Gw2Mumble.PlayerCharacter.Position,
                Rotation = new Vector3(0f, 0f, 0f),
                Id = selectedModelKey,
                Scale = 1.0f,
                InternalId = rendererControl.internalObjectId,
                IsOriginal = true
            };
            rendererControl.internalObjectId++;

            rendererControl.AddObject(newObj);

            rendererControl.updateHistoryList();

            //ScreenNotification.ShowNotification($" {selectedModelKey} platziert");
        }

        private void RemoveSelectedObject()
        {
            rendererControl.Objects.RemoveAll(o => rendererControl.SelectedObjects.Contains(o));
            rendererControl.SelectedObjects.Clear();
            rendererControl.updateHistoryList();
            ScreenNotification.ShowNotification("Selection Removed");
        }

        private void RemoveAllObjects()
        {
            var confirmDialog = new ConfirmDialog(contents, "Do you really want to remove all blueprints?");

            confirmDialog.confirmed += result =>
            {
                if (result)
                {
                    rendererControl.SelectedObjects.Clear();
                    rendererControl.Objects.Clear();
                    rendererControl.updateHistoryList();
                    ScreenNotification.ShowNotification("All Blueprints Removed");
                }
            };
            confirmDialog.Show();
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
                    Id = obj.Id,
                    Name = obj.Name,
                    Position = obj.Position,
                    Rotation = obj.Rotation,
                    RotationQuaternion = obj.RotationQuaternion,
                    Scale = obj.Scale,
                    CachedWorld = obj.CachedWorld,
                    InternalId = obj.InternalId,
                    IsOriginal = false,
                    Selected = false,
                };
                rendererControl.internalObjectId++;

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

            //rendererControl.ClearSelection();

            foreach (var obj in rendererControl.CopiedObjects)
            {
                var offset = obj.Position - oldPivot;
                var newPosition = GameService.Gw2Mumble.PlayerCharacter.Position + offset;

                var copy = new BlueprintObject
                {
                    ModelKey = obj.ModelKey,
                    Id = obj.Id,
                    Name = obj.Name,
                    Position = newPosition,
                    Rotation = obj.Rotation,
                    RotationQuaternion = obj.RotationQuaternion,
                    Scale = obj.Scale,
                    InternalId = rendererControl.internalObjectId,
                    IsOriginal = true,
                    Selected = false
                };

                rendererControl.AddObject(copy);
                //rendererControl.SelectObject(obj, true);
            }

            rendererControl.updateHistoryList();

            ScreenNotification.ShowNotification("Copied Objects Pasted");
        }

        private void SaveTemplate()
        {
            var mapId = GameService.Gw2Mumble.CurrentMap.Id.ToString();
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.Objects, mapId);
            if (xmlDoc == null)
            {
                ScreenNotification.ShowNotification($"An error occoured. There seem to be no decorations.");
                return;
            }
            var saveDialog = new SaveDialog(contents, xmlDoc);

            saveDialog.TemplateSaved += (path) =>
            {
                ScreenNotification.ShowNotification($"Template Saved");
            };
            saveDialog.Show();
        }

        private void SaveSelectionTemplate()
        {
            var mapId = GameService.Gw2Mumble.CurrentMap.Id.ToString();
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.SelectedObjects, mapId);
            if (xmlDoc == null)
            {
                ScreenNotification.ShowNotification($"An error occoured. Seems like no decorations were selected.");
                return;
            }

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
                        obj.InternalId = rendererControl.internalObjectId;
                        rendererControl.internalObjectId++;
                        obj.IsOriginal = true;
                        rendererControl.Objects.Add(obj);
                    }
                    rendererControl.updateWorld();

                    //ScreenNotification.ShowNotification("Objekte: "+rendererControl.Objects.Count());
                    ScreenNotification.ShowNotification("Template Loaded");
                }
                catch (Exception ex)
                {
                    ScreenNotification.ShowNotification($"Loading Failed\n{ex.Message}");
                }
                //ScreenNotification.ShowNotification($"📂 Template hinzugefügt: {Path.GetFileName(path)}");


                rendererControl.resetHistory();
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
                        obj.InternalId = rendererControl.internalObjectId;
                        rendererControl.internalObjectId++;
                        obj.IsOriginal = true;
                        rendererControl.Objects.Add(obj);
                    }
                    rendererControl.updateWorld();
                    rendererControl.updateHistoryList();

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
