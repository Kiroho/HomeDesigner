using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using System.Linq;
using Blish_HUD.Input;
using System.Collections.Generic;

namespace HomeDesigner
{

    public class DesignerWindow : StandardWindow
    {
        private TrackBar _sliderX;
        private TrackBar _sliderY;
        private TrackBar _sliderZ;

        private Dictionary<BlueprintObject, Vector3> _originalPositions = new Dictionary<BlueprintObject, Vector3>();
        private bool _isResettingSliders = false;
        private bool _isDraggingSlider = false;



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


            // Slider X
            _sliderX = new TrackBar()
            {
                Parent = this,
                Location = new Point(20, 200),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderX.ValueChanged += (s, e) => ApplyOffset(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            // Slider Y
            _sliderY = new TrackBar()
            {
                Parent = this,
                Location = new Point(20, 260),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderY.ValueChanged += (s, e) => ApplyOffset(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            // Slider Z
            _sliderZ = new TrackBar()
            {
                Parent = this,
                Location = new Point(20, 300),
                Width = 500,
                MinValue = -50,
                MaxValue = 50,
                Value = 0,
                SmallStep = true
            };
            _sliderZ.ValueChanged += (s, e) => ApplyOffset(new Vector3(_sliderX.Value, _sliderY.Value, _sliderZ.Value));

            _sliderX.LeftMouseButtonPressed += (s, e) => _isDraggingSlider = true;
            _sliderY.LeftMouseButtonPressed += (s, e) => _isDraggingSlider = true;
            _sliderZ.LeftMouseButtonPressed += (s, e) => _isDraggingSlider = true;

            GameService.Input.Mouse.LeftMouseButtonReleased += resetSliders;

        }

        void resetSliders(object sender, MouseEventArgs e)
        {
            if (_isDraggingSlider)
            {
                _isResettingSliders = true;

                _sliderX.Value = 0;
                _sliderY.Value = 0;
                _sliderZ.Value = 0;

                _isResettingSliders = false;
                _originalPositions.Clear();

                _isDraggingSlider = false;
            }
        }


        private void ApplyOffset(Vector3 offset)
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

            blueprintRenderer.PrecomputeWorlds(rendererControl.SelectedObjects);
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
                    ScreenNotification.ShowNotification($"Modell ausgewählt: {selectedModelKey}");
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
            selectedBtn.BasicTooltipText = "Aktuell ausgewählt";
            selectedBtn.BackgroundColor = Color.LightGreen;
        }

        private void PlaceSelectedModel()
        {
            if (string.IsNullOrEmpty(selectedModelKey))
            {
                ScreenNotification.ShowNotification("⚠ Kein Modell ausgewählt!");
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

            ScreenNotification.ShowNotification($" {selectedModelKey} platziert");
        }


    }
}
