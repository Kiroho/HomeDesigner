using Blish_HUD;
using static Blish_HUD.GameService;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WindowsInput;
using Color = Microsoft.Xna.Framework.Color;
using Image = Blish_HUD.Controls.Image;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Microsoft.Xna.Framework.Input;
using System.Windows.Input;

namespace EmoteTome
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private CornerIcon tomeCornerIcon;
        private StandardWindow tomeWindow;
        private int language = BadLocalization.ENGLISH;
        private Vector3 currentPositionA;
        private Vector3 currentPositionB;
        private Vector3 currentPositionC;
        private int checkPositionSwitch = 0;
        private List<Emote> coreEmoteList = new List<Emote>();
        private List<Emote> unlockEmoteList = new List<Emote>();
        private List<Emote> rankEmoteList = new List<Emote>();
        private Color activatedColor = new Color(250, 250, 250);
        private Color lockedColor = new Color(30, 30, 30);
        private Color noTargetColor = new Color(130, 130, 130);
        private Color cooldownColor = new Color(50, 50, 50);
        private bool checkedAPIForUnlock = false;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        // Ideally you should keep the constructor as is (empty). Instead use LoadAsync() to handle initializing the module.
        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            
        }

        // Define the settings you would like to use in your module.  Settings are persistent
        // between updates to both Blish HUD and your module.
        protected override void DefineSettings(SettingCollection settings)
        {
        }


        // blish update loop
        protected override async Task LoadAsync()
        {
            // Localization
            switch (Overlay.UserLocale.Value)
            {
                case Gw2Sharp.WebApi.Locale.English:
                    language = BadLocalization.ENGLISH;
                    break;
                case Gw2Sharp.WebApi.Locale.French:
                    language = BadLocalization.FRENCH;
                    break;
                case Gw2Sharp.WebApi.Locale.German:
                    language = BadLocalization.GERMAN;
                    break;
                case Gw2Sharp.WebApi.Locale.Spanish:
                    language = BadLocalization.SPANISH;
                    break;
                default:
                    language = BadLocalization.ENGLISH;
                    break;
            };


            // Add a corner icon in the top left next to the other icons in guild wars 2 (e.g. inventory icon, Mail icon)
            tomeCornerIcon = new CornerIcon()
            {
                Icon = ContentsManager.GetTexture("CornerIcon.png"),
                Parent = GameService.Graphics.SpriteScreen,
                Visible = false
            };

            // On click listener for corner icon
            tomeCornerIcon.Click += delegate
            {
                tomeWindow.ToggleWindow();
                if (tomeWindow.Visible && checkedAPIForUnlock == false)
                {
                    _ = checkUnlockedEmotesByAPI();
                    checkedAPIForUnlock = true;
                }
            };


            //Main Window
            tomeWindow = new StandardWindow(
                ContentsManager.GetTexture("WindowBackground.png"),
                new Rectangle(40, 26, 913, 691),
                new Rectangle(70, 71, 839, 605))
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = BadLocalization.WINDOWTITLE[language],
                SavesPosition = true,
                SavesSize = true,
                Id = "0001",
                CanResize = true
            };

            var targetCheckbox = new Checkbox()
            {
                Text = BadLocalization.TARGETCHECKBOXTEXT[language],
                Location = new Point(0, 0),
                BasicTooltipText = BadLocalization.TARGETCHECKBOXTOOLTIP[language],
                Parent = tomeWindow
            };

            var synchronCheckbox = new Checkbox()
            {
                Text = BadLocalization.SYNCHRONCHECKBOXTEXT[language],
                Location = new Point(0, 20),
                BasicTooltipText = BadLocalization.SYNCHRONCHECKBOXTOOLTIP[language],
                Parent = tomeWindow
            };

            //Mainpanel
            var mainPanel = new FlowPanel()
            {
                Size = new Point(tomeWindow.ContentRegion.Width, tomeWindow.ContentRegion.Height),
                Location = new Point(0, 50),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Parent = tomeWindow,
                CanScroll = true,
                Padding = new Thickness(20)
            };


            //Panel for core emotes
            var corePanel = new FlowPanel()
            {
                ShowBorder = true,
                Title = BadLocalization.COREPANELTITLE[language],
                Size = new Point(mainPanel.ContentRegion.Width, mainPanel.ContentRegion.Height),
                Parent = mainPanel,
                CanCollapse = true,
                FlowDirection = ControlFlowDirection.LeftToRight,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(5, 5),
                ControlPadding = new Vector2(5, 5),
                OuterControlPadding = new Vector2(5, 5)
            };

            EmoteLibrary library = new EmoteLibrary(ContentsManager);
            coreEmoteList = library.loadCoreEmotes();

            int size = 64;
            List<Image> coreEmoteImages = new List<Image>();

            //Create Emote Images
            foreach (Emote emote in coreEmoteList)
            {
                if (emote.getCategory().Equals(EmoteLibrary.CORECODE))
                {
                    var emoteImage = new Image(ContentsManager.GetTexture(emote.getImagePath()))
                    {
                        Size = new Point(size, size),
                        BasicTooltipText = emote.getToolTipp()[language],
                        Parent = corePanel
                    };

                    //OnClick Listener
                    emoteImage.Click += delegate
                    {
                        if (emoteAllowed())
                        {
                            //ScreenNotification.ShowNotification(emote.getToolTipp()[language]);
                            //Activate Emote
                            activateEmote(emote.getChatCode(), targetCheckbox.Checked, synchronCheckbox.Checked);
                        }
                    };
                    coreEmoteImages.Add(emoteImage);
                    emote.setImg(emoteImage);
                }
            }


            //Panel for unlockable Emotes
            var unlockablePanel = new FlowPanel()
            {
                ShowBorder = true,
                Title = BadLocalization.UNLOCKABLEPANELTITLE[language],
                Size = new Point(mainPanel.ContentRegion.Width, mainPanel.ContentRegion.Height),
                Parent = mainPanel,
                CanCollapse = true,
                FlowDirection = ControlFlowDirection.LeftToRight,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(5, 5),
                ControlPadding = new Vector2(5, 5),
                OuterControlPadding = new Vector2(5, 5)
            };

            unlockEmoteList = library.loadUnlockEmotes();
            List<Image> unlockEmoteImages = new List<Image>();

            //Create Emote Images
            foreach (Emote emote in unlockEmoteList)
            {
                if (emote.getCategory().Equals(EmoteLibrary.UNLOCKCODE))
                {
                    var emoteImage = new Image(ContentsManager.GetTexture(emote.getImagePath()))
                    {
                        Size = new Point(size, size),
                        BasicTooltipText = emote.getToolTipp()[language],
                        Parent = unlockablePanel,
                        Tint = lockedColor,
                        Enabled = false
                    };

                    //OnClick Listener
                    emoteImage.Click += delegate
                    {
                        if (emoteAllowed())
                        {
                            //ScreenNotification.ShowNotification(emote.getToolTipp()[language]);
                            //Activate Emote
                            activateEmote(emote.getChatCode(), targetCheckbox.Checked, synchronCheckbox.Checked);
                        }
                    };
                    unlockEmoteImages.Add(emoteImage);
                    emote.setImg(emoteImage);
                    emote.isDeactivatedByLocked(true);
                }
            }



            //Panel for Rank Emotes
            var rankPanel = new FlowPanel()
            {
                ShowBorder = true,
                Title = BadLocalization.RANKPANELTITLE[language],
                Size = new Point(mainPanel.ContentRegion.Width, mainPanel.ContentRegion.Height),
                Parent = mainPanel,
                CanCollapse = true,
                FlowDirection = ControlFlowDirection.LeftToRight,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(5, 5),
                ControlPadding = new Vector2(5, 5),
                OuterControlPadding = new Vector2(5, 5),
            };
            rankEmoteList = library.loadRankEmotes();
            List<Image> cooldownEmoteImages = new List<Image>();

            //Create Emote Images
            foreach (Emote emote in rankEmoteList)
            {
                if (emote.getCategory().Equals(EmoteLibrary.RANKCODE))
                {
                    var emoteImage = new Image(ContentsManager.GetTexture(emote.getImagePath()))
                    {
                        Size = new Point(size, size),
                        BasicTooltipText = emote.getToolTipp()[language],
                        Parent = rankPanel,
                        Tint = lockedColor,
                        Enabled = false
                    };

                    //OnClick Listener
                    emoteImage.Click += delegate
                    {
                        if (emoteAllowed())
                        {
                            //ScreenNotification.ShowNotification(emote.getToolTipp()[language]);
                            //Activate Emote
                            activateEmote(emote.getChatCode(), targetCheckbox.Checked, synchronCheckbox.Checked);
                            activateCooldown();
                        }
                    };
                    cooldownEmoteImages.Add(emoteImage);
                    emote.setImg(emoteImage);
                    emote.isDeactivatedByLocked(true);
                }
            }



            //Spacer for bottom
            var spacePanel = new Panel()
            {
                Size = new Point(mainPanel.ContentRegion.Width, 50),
                Parent = mainPanel
            };

            //Resizing__________________________________________________________________________________________________________________________

            //Window
            tomeWindow.Resized += (_, e) =>
            {
                mainPanel.Width = tomeWindow.ContentRegion.Width;
                mainPanel.Height = tomeWindow.ContentRegion.Height;
                corePanel.Width = mainPanel.ContentRegion.Width;
                unlockablePanel.Width = mainPanel.ContentRegion.Width;
                rankPanel.Width = mainPanel.ContentRegion.Width;
                spacePanel.Width = mainPanel.ContentRegion.Width;

            };

            //When target checkbox is clicked
            targetCheckbox.CheckedChanged += (_, e) =>
            {
                if (targetCheckbox.Checked)
                {
                    foreach (Emote emote in coreEmoteList)
                    {
                        if (!emote.hasTarget())
                        {
                            emote.getImg().Tint = noTargetColor;
                            emote.isDeactivatedByTargeting(true);
                        }
                    }
                    foreach (Emote emote in unlockEmoteList)
                    {
                        if (!emote.hasTarget() && !emote.isDeactivatedByLocked())
                        {
                            emote.getImg().Tint = noTargetColor;
                            emote.isDeactivatedByTargeting(true);
                        }
                    }
                    foreach (Emote emote in rankEmoteList)
                    {
                        if (!emote.hasTarget() && !emote.isDeactivatedByLocked())
                        {
                            if (!emote.isDeactivatedByCooldown())
                            {
                                emote.getImg().Tint = noTargetColor;
                            }
                            emote.isDeactivatedByTargeting(true);
                        }
                    }
                }
                else
                {
                    foreach (Emote emote in coreEmoteList)
                    {
                        emote.getImg().Tint = activatedColor;
                        emote.isDeactivatedByTargeting(false);
                    }
                    foreach (Emote emote in unlockEmoteList)
                    {
                        if (!emote.isDeactivatedByLocked())
                        {
                            emote.getImg().Tint = activatedColor;
                        }
                        emote.isDeactivatedByTargeting(false);
                    }
                    foreach (Emote emote in rankEmoteList)
                    {
                        if (!emote.isDeactivatedByCooldown() && !emote.isDeactivatedByLocked())
                        {
                            emote.getImg().Tint = activatedColor;
                        }
                        emote.isDeactivatedByTargeting(false);

                    }
                }
            };

            //Set Cooldown for Rank Emotes
            void activateCooldown()
            {
                foreach (Emote emote in rankEmoteList)
                {
                    if (!emote.isDeactivatedByLocked())
                    {
                        emote.getImg().Tint = cooldownColor;
                        emote.getImg().Enabled = false;
                        emote.isDeactivatedByCooldown(true);
                    }

                }

                System.Timers.Timer aTimer = new System.Timers.Timer();
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                aTimer.Interval = 60000;
                aTimer.Enabled = true;

                void OnTimedEvent(object source, ElapsedEventArgs e)
                {
                    aTimer.Enabled = false;
                    foreach (Emote emote in rankEmoteList)
                    {
                        if (!emote.isDeactivatedByLocked())
                        {
                            if (emote.isDeactivatedByTargeting())
                            {
                                emote.getImg().Tint = noTargetColor;
                            }
                            else
                            {
                                emote.getImg().Tint = activatedColor;
                            }
                            emote.getImg().Enabled = true;
                            emote.isDeactivatedByCooldown(false);
                        }
                    }
                }
            }

            tomeCornerIcon.Visible = true;
        }

        protected override void Update(GameTime gameTime)
        {
            //Check if player is moving, when emote window is opened
            if (tomeWindow.Visible)
            {
                switch (checkPositionSwitch)
                {
                    case 0:
                        currentPositionA = GameService.Gw2Mumble.PlayerCharacter.Position;
                        break;
                    case 1:
                        currentPositionB = GameService.Gw2Mumble.PlayerCharacter.Position;
                        break;
                    case 2:
                        currentPositionC = GameService.Gw2Mumble.PlayerCharacter.Position;
                        break;
                }
                if (checkPositionSwitch >= 2)
                    checkPositionSwitch = 0;
                else
                    checkPositionSwitch++;
            }
        }

        
        protected override void Unload()
        {
            tomeWindow.Visible = false;
            tomeCornerIcon?.Dispose();
        }



        private bool emoteAllowed()
        {
            if (IsAnyKeyDown())
            {
                ScreenNotification.ShowNotification(BadLocalization.NOEMOTEONKEYPRESSED[language]);
                return false;
            }
            else if (isPlayerMoving())
            {
                ScreenNotification.ShowNotification(BadLocalization.NOEMOTEWHENMOVING[language]);
                return false;
            }
            else if (GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
            {
                ScreenNotification.ShowNotification(BadLocalization.NOEMOTEINCOMBAT[language]);
                return false;
            }
            else if (!GameService.Gw2Mumble.PlayerCharacter.CurrentMount.ToString().Equals("None"))
            {
                ScreenNotification.ShowNotification(BadLocalization.NOEMOTEONMOUNT[language]);
                return false;
            }
            else
            {
                return true;
            }

        }
        private void activateEmote(String emote, bool targetChecked, bool synchronChecked)
        {
            String chatCommand = "/" + emote;
            if (targetChecked)
            {
                chatCommand = chatCommand + " @";
            }
            if (synchronChecked)
            {
                chatCommand = chatCommand + " *";
            }

            if (!GameService.Gw2Mumble.UI.IsTextInputFocused)
            {
                Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN);
                Thread.Sleep(25);
            }
            Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.CONTROL, true);
            Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_A, true);
            Thread.Sleep(25);
            Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.CONTROL, true);
            Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.KEY_A);
            Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.KEY_D);
            InputSimulator sim = new InputSimulator();
            sim.Keyboard.TextEntry(chatCommand);
            Thread.Sleep(50);
            Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN);


        }

        private bool isPlayerMoving()
        {
            if (currentPositionA.Equals(currentPositionB) && currentPositionA.Equals(currentPositionC))
                return false;
            else
                return true;
        }

        public bool IsAnyKeyDown()
        {
            var values = Enum.GetValues(typeof(Key));

            foreach (var v in values)
                if (((Key)v) != Key.None && Keyboard.GetState().IsKeyDown((Keys)(Key)v))
            return true;

            return false;
        }


        private async Task checkUnlockedEmotesByAPI()
        {
            var apiKeyPermissions = new List<TokenPermission>
                        {
                            TokenPermission.Account, // this permission can be used to check if your module got a token at all because every api key has this persmission.
                            TokenPermission.Progression,
                            TokenPermission.Unlocks,
                            TokenPermission.Pvp
                        };
            try
            {
                System.Diagnostics.Debug.WriteLine("_______________________________________________");
                if (Gw2ApiManager.HasPermissions(apiKeyPermissions))
                {
                    System.Diagnostics.Debug.WriteLine("Permission");
                    // load unlocked emotes
                    var finisherList = await Gw2ApiManager.Gw2ApiClient.V2.Account.Finishers.GetAsync();
                    var ranks = await Gw2ApiManager.Gw2ApiClient.V2.Pvp.Stats.GetAsync();

                    foreach (Emote emote in rankEmoteList)
                    {
                        if (emote.getChatCode().Equals("rank"))
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 1"))
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 10") && ranks.PvpRank >= 10)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 20") && ranks.PvpRank >= 20)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 30") && ranks.PvpRank >= 30)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 40") && ranks.PvpRank >= 40)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 50") && ranks.PvpRank >= 50)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 60") && ranks.PvpRank >= 60)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 70") && ranks.PvpRank >= 70)
                        {
                            enableRankEmote(emote);
                        }
                        else if (emote.getChatCode().Equals("rank 80") && ranks.PvpRank >= 80)
                        {
                            enableRankEmote(emote);
                        }
                    }
                    void enableRankEmote(Emote emote)
                    {
                        emote.getImg().Tint = activatedColor;
                        emote.getImg().Enabled = true;
                        emote.isDeactivatedByLocked(false);
                    }



                    var unlockedEmotes = new List<string>(await Gw2ApiManager.Gw2ApiClient.V2.Account.Emotes.GetAsync());
                    unlockedEmotes = unlockedEmotes.ConvertAll(d => d.ToLower());
                    foreach (Emote emote in unlockEmoteList)
                    {
                        //Deactivate all unlockable emotes
                        //Exceptions for Emotes that are not yet included in API
                        if (emote.getChatCode().Equals("bless") ||
                            emote.getChatCode().Equals("heroic") ||
                            emote.getChatCode().Equals("hiss") ||
                            emote.getChatCode().Equals("magicjuggle") ||
                            emote.getChatCode().Equals("paper") ||
                            emote.getChatCode().Equals("possessed") ||
                            emote.getChatCode().Equals("readbook") ||
                            emote.getChatCode().Equals("rock") ||
                            emote.getChatCode().Equals("scissors") ||
                            emote.getChatCode().Equals("serve") ||
                            emote.getChatCode().Equals("sipcoffee"))
                        {
                            emote.getImg().Enabled = true;
                            emote.getImg().Tint = activatedColor;
                            emote.isDeactivatedByLocked(false);
                        }

                        //System.Diagnostics.Debug.WriteLine("All Emotes: " + unlocked);
                        //System.Diagnostics.Debug.WriteLine("Unlocked Emotes: " + unlocked);

                        //Activate unlocked emotes
                        if (unlockedEmotes.Contains(emote.getChatCode()))
                        {
                            System.Diagnostics.Debug.WriteLine("_______________________________________________");
                            System.Diagnostics.Debug.WriteLine("Freigeschalten: " + emote.getChatCode());

                            emote.getImg().Enabled = true;
                            emote.getImg().Tint = activatedColor;
                            emote.isDeactivatedByLocked(false);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No permissions");
                    foreach (Emote emote in unlockEmoteList)
                    {
                        enableLockedEmote(emote);
                    }
                    foreach (Emote emote in rankEmoteList)
                    {
                        enableLockedEmote(emote);
                    }
                    void enableLockedEmote(Emote emote)
                    {
                        emote.getImg().Tint = activatedColor;
                        emote.getImg().Enabled = true;
                        emote.isDeactivatedByLocked(false);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No Permissions via exception");
                foreach (Emote emote in unlockEmoteList)
                {
                    enableLockedEmote(emote);
                }
                foreach (Emote emote in rankEmoteList)
                {
                    enableLockedEmote(emote);
                }
                void enableLockedEmote(Emote emote)
                {
                    emote.getImg().Tint = activatedColor;
                    emote.getImg().Enabled = true;
                    emote.isDeactivatedByLocked(false);
                }
            }
        }


    }
}