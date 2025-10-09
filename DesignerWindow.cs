using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using System.Linq;
using Blish_HUD.Input;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace HomeDesigner
{

    public class DesignerWindow : StandardWindow
    {
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

        public DesignerWindow(ContentsManager contents, RendererControl rendererControl, BlueprintRenderer blueprintRenderer)
            : base(
                contents.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 691),   // Außenrahmen (Fenstergröße)
                new Rectangle(70, 71, 839, 605))  // Innenbereich (wo Controls rein kommen)
        {
            this.rendererControl = rendererControl;
            this.blueprintRenderer = blueprintRenderer;

            this.Title = "Home Designer";
            this.Parent = GameService.Graphics.SpriteScreen;

            this.SavesPosition = true;
            this.SavesSize = true;
            this.CanResize = true;
            this.Id = "HomeDesigner.MainWindow";

            //Mainpanel
            var mainPanel = new FlowPanel()
            {
                Size = new Point(this.ContentRegion.Width, this.ContentRegion.Height),
                Location = new Point(0, 50),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Parent = this,
                CanScroll = true,
                Padding = new Thickness(20)
            };

            //Objekt platzieren
            // Button zum Platzieren des gewählten Modells
            var placeButton = new StandardButton()
            {
                Parent = this,
                Text = "Ausgewähltes Modell platzieren",
                Width = this.ContentRegion.Width - 10,
                Height = 45,
                Location = new Point(5, 150)
            };

            placeButton.Click += (s, e) => PlaceSelectedModel();

            //Objekt platzieren
            //  Panel für Modellliste
            modelListPanel = new FlowPanel()
            {
                Parent = this,
                Size = new Point(this.ContentRegion.Width, 150),
                Location = new Point(0, 0),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                ShowBorder = true
            };

            RefreshModelList();

            //Objekt transformieren
            //Modus Buttons
            _translateButton = new StandardButton() 
            { 
                Parent = this,
                Location = new Point(20, 220),
                Text = "Move", 
                Width = 100 
            };

            _rotateButton = new StandardButton() 
            { 
                Parent = this,
                Location = new Point(220, 220),
                Text = "Rotate", 
                Width = 100 
            };

            _scaleButton = new StandardButton() 
            { 
                Parent = this,
                Location = new Point(420, 220),
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
                Parent = this,
                Text = "Weltachse",
                Checked = true,
                Location = new Point(220, 260)
            };

            _localAxisCheckbox = new Checkbox()
            {
                Parent = this,
                Text = "Lokale Achse",
                Checked = false,
                Location = new Point(220, 280)
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
                Parent = this,
                Location = new Point(20, 320),
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
                Parent = this,
                Location = new Point(20, 360),
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
                Parent = this,
                Location = new Point(20, 400),
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
                    ApplyTranslation(values);
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
        private void ApplyTranslation(Vector3 offset)
        {
            if (_isResettingSliders) return;

            if (rendererControl.SelectedObjects.Count == 0)
                return;

            foreach (var obj in rendererControl.SelectedObjects)
            {
                // Originalposition merken, falls noch nicht gespeichert
                if (!_originalPositions.ContainsKey(obj))
                    _originalPositions[obj] = obj.Position;

                // Neue Position = Original + Offset
                obj.Position = _originalPositions[obj] + offset;
            }

        }


        private void StartTransform()
        {
            if (_isDraggingSlider) return;
            _isDraggingSlider = true;

            _rotationPivot = blueprintRenderer.getPivotObject();

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

            var pivot = _rotationPivot ?? blueprintRenderer.getPivotObject();

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

            const float rotationSensitivity = 2.0f; // wie bei Weltrotation

            // Sliderwerte → Radiant (mit Empfindlichkeit)
            float rx = MathHelper.ToRadians(sliderDegrees.X * rotationSensitivity);
            float ry = MathHelper.ToRadians(sliderDegrees.Y * rotationSensitivity);
            float rz = MathHelper.ToRadians(sliderDegrees.Z * rotationSensitivity);

            // 🔸 Lokales Delta-Quaternion
            Quaternion deltaQuat = Quaternion.CreateFromYawPitchRoll(ry, rx, rz);

            foreach (var obj in rendererControl.SelectedObjects)
            {
                var baseRot = _startRotations[obj];

                // 🔸 Lokale Rotation: Basis * Delta
                obj.RotationQuaternion = baseRot * deltaQuat;

                // Position bleibt bei lokaler Rotation gleich
                // Wenn du willst, dass der Pivot im Objekt bleibt, brauchst du hier nichts anzupassen.
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
                Rotation = new Vector3(MathHelper.PiOver2, 0f, 0f),
                Scale = 0.001f
            };

            rendererControl.AddObject(newObj);

            //ScreenNotification.ShowNotification($" {selectedModelKey} platziert");
        }


    }
}
