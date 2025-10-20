using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules.Managers;
using HomeDesigner.Loader;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeDesigner.Views
{

    public class DesignerView : View
    {

        private readonly ContentsManager contents;
        //Position Ändern
        private TrackBar _sliderX;
        private TrackBar _sliderY;
        private TrackBar _sliderZ;
        private Dictionary<BlueprintObject, Vector3> _originalPositions = new Dictionary<BlueprintObject, Vector3>();
        private Dictionary<BlueprintObject, Quaternion> _startRotations = new Dictionary<BlueprintObject, Quaternion>();
        private Dictionary<BlueprintObject, Vector3> _startPositions = new Dictionary<BlueprintObject, Vector3>();
        private Vector3? _rotationPivot = null;
        private Dictionary<BlueprintObject, float> _originalScales = new Dictionary<BlueprintObject, float>();
        private bool _isResettingSliders = false;
        private bool _isDraggingSlider = false;
        private enum TransformMode { Translate, Rotate, Scale }
        private TransformMode currentMode = TransformMode.Translate;
        private enum RotationSpace { World, Local }
        private RotationSpace _rotationSpace = RotationSpace.World;
        private Checkbox _worldAxisCheckbox;
        private Checkbox _localAxisCheckbox;


        private StandardButton _translateButton;
        private StandardButton _rotateButton;
        private StandardButton _scaleButton;




        //Fenster und Hilfsvariablen
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

            //Objekt platzieren
            // Button zum Platzieren des gewählten Modells
            var placeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Ausgewähltes Modell platzieren",
                Width = buildPanel.ContentRegion.Width -30,
                Height = 45,
                Location = new Point(30, 150)
            };

            placeButton.Click += (s, e) => PlaceSelectedModel();

            //Objekt platzieren
            //  Panel für Modellliste
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

            //Objekt transformieren
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

            _translateButton.Click += (s, e) => SetTransformMode(TransformMode.Translate);
            _rotateButton.Click += (s, e) => SetTransformMode(TransformMode.Rotate);
            _scaleButton.Click += (s, e) => SetTransformMode(TransformMode.Scale);
            _translateButton.BackgroundColor = Color.LightGreen;



            //Objekt transformieren
            //Rotationsachse ändern
            _worldAxisCheckbox = new Checkbox()
            {
                Parent = buildPanel,
                Text = "Weltachse",
                Checked = true,
                Location = new Point(230, 260)
            };

            _localAxisCheckbox = new Checkbox()
            {
                Parent = buildPanel,
                Text = "Lokale Achse",
                Checked = false,
                Location = new Point(230, 280)
            };

            _worldAxisCheckbox.CheckedChanged += (s, e) =>
            {
                if (_worldAxisCheckbox.Checked)
                {
                    _localAxisCheckbox.Checked = false;
                    _rotationSpace = RotationSpace.World;
                }
            };

            _localAxisCheckbox.CheckedChanged += (s, e) =>
            {
                if (_localAxisCheckbox.Checked)
                {
                    _worldAxisCheckbox.Checked = false;
                    _rotationSpace = RotationSpace.Local;
                }
            };


            //Objekt transformieren
            // Slider X
            _sliderX = new TrackBar()
            {
                Parent = buildPanel,
                Location = new Point(30, 320),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderX.ValueChanged += (s, e) => ApplyTransform(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            //Objekt transformieren
            // Slider Y
            _sliderY = new TrackBar()
            {
                Parent = buildPanel,
                Location = new Point(30, 360),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderY.ValueChanged += (s, e) => ApplyTransform(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            //Objekt transformieren
            // Slider Z
            _sliderZ = new TrackBar()
            {
                Parent = buildPanel,
                Location = new Point(30, 400),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderZ.ValueChanged += (s, e) => ApplyTransform(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            _sliderX.LeftMouseButtonPressed += (s, e) => StartTransform();
            _sliderY.LeftMouseButtonPressed += (s, e) => StartTransform();
            _sliderZ.LeftMouseButtonPressed += (s, e) => StartTransform();

            GameService.Input.Mouse.LeftMouseButtonReleased += resetSliders;


            var removeButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Ausgewähltes Objekt entfernen",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 450)
            };
            removeButton.Click += (s, e) => RemoveSelectedObject();


            var copyButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Ausgewähltes Objekt kopieren",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 510)
            };
            copyButton.Click += (s, e) => CopySelectedObject();


            var pasteButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Kopiertes Objekt einfügen",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 560)
            };
            pasteButton.Click += (s, e) => PasteObject();


            var saveButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Template speichern",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 620)
            };
            saveButton.Click += (s, e) => SaveTemplate();


            var loadButton = new StandardButton()
            {
                Parent = buildPanel,
                Text = "Template laden",
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
                Text = "Add to Template",
                Width = buildPanel.ContentRegion.Width - 30,
                Height = 45,
                Location = new Point(30, 800)
            };
            addButton.Click += (s, e) => AddTemplate();
        }

        public static void RotateAroundPivot(
        Vector3 objectPosition,
        Quaternion objectRotation,
        Vector3 pivot,
        Vector3 axis,
        float angleDegrees,
        out Vector3 newPosition,
        out Quaternion newRotation)
        {
            // 1. Achse normalisieren
            Vector3 normAxis = Vector3.Normalize(axis);

            // 2. Winkel in Radiant
            float angleRadians = (float)(Math.PI * angleDegrees / 180.0);


            // 3. Rotations-Quaternion erstellen
            Quaternion rotation = Quaternion.CreateFromAxisAngle(normAxis, angleRadians);

            // 4. Position um Pivot drehen
            newPosition = RotatePointAroundPivot(objectPosition, pivot, rotation);

            // 5. Orientierung aktualisieren (Mitrotation)
            newRotation = rotation * objectRotation;

        }

        /// <summary>
        /// Dreht einen Punkt im Raum mit einem Quaternion um einen Pivot.
        /// </summary>
        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 dir = point - pivot;
            dir = Vector3.Transform(dir, rotation);
            return pivot + dir;
        }




        private void SetTransformMode(TransformMode mode)
        {
            currentMode = mode;

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
                case TransformMode.Translate:
                    _translateButton.BackgroundColor = activeColor;
                    break;
                case TransformMode.Rotate:
                    _rotateButton.BackgroundColor = activeColor;
                    break;
                case TransformMode.Scale:
                    _scaleButton.BackgroundColor = activeColor;
                    break;
            }
        }



        private void ApplyTransform(Vector3 values)
        {
            if (rendererControl.SelectedObjects.Count == 0)
                return;

            switch (currentMode)
            {
                case TransformMode.Translate:
                    if (_rotationSpace == RotationSpace.World)
                        ApplyTranslation(values, false);
                    else
                        ApplyTranslation(values, true);
                    break;
                case TransformMode.Rotate:
                    if (_rotationSpace == RotationSpace.World)
                        ApplyWorldRotation(values);
                    else
                        ApplyLocalRotation(values);
                    break;
                case TransformMode.Scale:
                    ApplyScale(values);
                    break;
            }

            // Weltmatrizen aktualisieren
            blueprintRenderer.PrecomputeWorlds(rendererControl.SelectedObjects);
        }

        //Objekt transformieren
        private void ApplyTranslation(Vector3 offset, bool local = false)
        {
            if (_isResettingSliders) return;

            if (rendererControl.SelectedObjects.Count == 0)
                return;

            //using local axis
            if (local)
            {
                offset = Vector3.Transform(offset, rendererControl.SelectedObjects[0].RotationQuaternion);
            }

            foreach (var obj in rendererControl.SelectedObjects)
            {
                if (!_startPositions.ContainsKey(obj))
                    _startPositions[obj] = obj.Position;

                obj.Position = _startPositions[obj] + offset;
            }

        }


        private void StartTransform()
        {
            if (_isDraggingSlider) return;
            _isDraggingSlider = true;

            _rotationPivot = rendererControl.getPivotObject();

            _startRotations.Clear();
            _startPositions.Clear();

            foreach (var obj in rendererControl.SelectedObjects)
            {
                _startRotations[obj] = obj.RotationQuaternion;
                _startPositions[obj] = obj.Position;
            }
        }




        private void ApplyWorldRotation(Vector3 sliderDegrees)
        {
            if (_isResettingSliders) return;
            if (rendererControl.SelectedObjects.Count == 0) return;
            if (!_isDraggingSlider) StartTransform();

            var pivot = _rotationPivot ?? rendererControl.getPivotObject();

            // 🔸 Empfindlichkeit (z. B. 2.0f → doppelt so schnell)
            const float rotationSensitivity = 2.0f; //Später ggf. über einstellungen änderbar machen

            // 🔸 Skalierte Sliderwerte → Radiant
            float rx = MathHelper.ToRadians(sliderDegrees.X * rotationSensitivity);
            float ry = MathHelper.ToRadians(sliderDegrees.Y * rotationSensitivity);
            float rz = MathHelper.ToRadians(sliderDegrees.Z * rotationSensitivity);

            Quaternion deltaQuat = Quaternion.CreateFromYawPitchRoll(ry, rx, rz);

            foreach (var obj in rendererControl.SelectedObjects)
            {
                var basePos = _startPositions[obj];
                var baseRot = _startRotations[obj];

                Vector3 offset = basePos - pivot;
                Vector3 rotatedOffset = Vector3.Transform(offset, deltaQuat);
                obj.Position = pivot + rotatedOffset;

                obj.RotationQuaternion = deltaQuat * baseRot;
            }
        }




        //Objekt transformieren
        private void ApplyLocalRotation(Vector3 sliderDegrees)
        {
            if (_isResettingSliders) return;
            if (rendererControl.SelectedObjects.Count == 0) return;
            if (!_isDraggingSlider) StartTransform();

            var pivot = _rotationPivot ?? rendererControl.getPivotObject();
            const float rotationSensitivity = 2.0f;

            float rx = MathHelper.ToRadians(sliderDegrees.X * rotationSensitivity);
            float ry = MathHelper.ToRadians(sliderDegrees.Y * rotationSensitivity);
            float rz = MathHelper.ToRadians(sliderDegrees.Z * rotationSensitivity);

            // ΔRotation in lokalen Achsen des ersten Objekts
            var refObj = rendererControl.SelectedObjects[0];
            var deltaQuat = Quaternion.CreateFromYawPitchRoll(ry, rx, rz);
            var refBaseRot = _startRotations[refObj];

            // Delta in Weltkoordinaten umrechnen
            Quaternion deltaWorld = deltaQuat * refBaseRot;

            foreach (var obj in rendererControl.SelectedObjects)
            {
                var basePos = _startPositions[obj];
                var baseRot = _startRotations[obj];

                // Offset vom Pivot
                Vector3 offset = basePos - pivot;

                // Rotieren um die Achse des Referenzobjekts
                Vector3 rotatedOffset = Vector3.Transform(offset, deltaWorld * Quaternion.Inverse(refBaseRot));

                obj.Position = pivot + rotatedOffset;
                obj.RotationQuaternion = deltaQuat * baseRot;
            }
        }





        //Objekt transformieren
        private void ApplyScale(Vector3 scaleValues)
        {
            //ToDo
        }


        //Objekt transformieren
        private void resetSliders(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSlider) return;

            _isResettingSliders = true;

            _sliderX.Value = 0;
            _sliderY.Value = 0;
            _sliderZ.Value = 0;

            _rotationPivot = null;
            _isDraggingSlider = false;

            // Neue Baselines direkt aus aktuellen Objektzuständen übernehmen
            _startRotations.Clear();
            _startPositions.Clear();

            foreach (var obj in rendererControl.SelectedObjects)
            {
                _startRotations[obj] = obj.RotationQuaternion;
                _startPositions[obj] = obj.Position;
            }

            _isResettingSliders = false;
        }



        //Objekt platzieren
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
                    ScreenNotification.ShowNotification($"Modell ausgewählt: {selectedModelKey}");
                    HighlightSelectedButton(btn);
                };
            }
        }

        //Objekt platzieren
        private void HighlightSelectedButton(StandardButton selectedBtn)
        {
            // Alle Buttons zurücksetzen
            foreach (var child in modelListPanel.Children.OfType<StandardButton>())
            {
                child.BasicTooltipText = null;
                child.BackgroundColor = Color.Transparent;
            }

            // Gewählten Button hervorheben
            selectedBtn.BasicTooltipText = "Aktuell ausgewählt";
            selectedBtn.BackgroundColor = Color.LightGreen;
        }

        //Objekt platzieren
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
            //ToDo
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
                rendererControl.SelectObject(obj, true);
                obj.Selected = true;
            }

            ScreenNotification.ShowNotification("Copied Objects Pasted");
        }

        private void SaveTemplate()
        {
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.Objects);
            var saveDialog = new SaveDialog(contents, xmlDoc);

            saveDialog.TemplateSaved += (path) =>
            {
                ScreenNotification.ShowNotification($"Template saved");
            };
            saveDialog.Show();
        }

        private void SaveSelectionTemplate()
        {
            var xmlDoc = XmlLoader.SaveBlueprintObjectsToXml(rendererControl.SelectedObjects);
            var saveDialog = new SaveDialog(contents, xmlDoc);

            saveDialog.TemplateSaved += (path) =>
            {
                ScreenNotification.ShowNotification($"Template saved");
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
