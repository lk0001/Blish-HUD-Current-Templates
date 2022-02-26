using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

namespace lk0001.CurrentTemplates
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        public static readonly Logger Logger = Logger.GetLogger<Module>();

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        public static string[] _fontSizes = new string[] { "8", "11", "12", "14", "16", "18", "20", "22", "24", "32", "34", "36" };
        public static string[] _fontAlign = new string[] { "Left", "Center", "Right" };
        public static SettingEntry<string> _settingFontSize;
        public static SettingEntry<string> _settingAlign;
        public static SettingEntry<Point> _settingLoc;
        public static SettingEntry<bool> _settingDrag;
        public static SettingEntry<bool> _settingBuildPad;
        public static SettingEntry<string> _settingBuildPadPath;
        public static bool _hasBuildPad = false;

        private static readonly double INTERVAL_CHECKTEMPLATES = 30010; // 30 seconds + 10ms
        private static readonly List<string> GW2_PATHS = new List<string>()
        {
            "c:/Program Files/Guild Wars 2",
            "c:/Program Files (x86)/Guild Wars 2",
            "d:/Guild Wars 2",
            ".",
            "..",
        };
        private static readonly string BUILD_PAD_PATH = "addons/arcdps/arcdps.buildpad/config.ini";

        private double _lastTemplateCheck = -1;
        private Controls.DrawTemplates templatesControl;
        private Character character;
        private string characterName = "";
        private Utility.BuildPad buildPad;
        private Views.SettingsView settingsView;

        protected override void DefineSettings(SettingCollection settings)
        {
            _settingFontSize = settings.DefineSetting("CurrentTemplatesFont", "16", "Font Size", "");
            _settingAlign = settings.DefineSetting("CurrentTemplatesAlign", "Left", "Align", "");
            _settingLoc = settings.DefineSetting("CurrentTemplatesLoc", new Point(1, 30), "Location", "");
            _settingDrag = settings.DefineSetting("CurrentTemplatesDrag", false, "Enable Dragging", "");
            _settingBuildPad = settings.DefineSetting("CurrentTemplatesBP", false, "BuildPad integration", "Displays build template name based on your builds saved in BuildPad (requires setting the path below)");
            _settingBuildPadPath = settings.DefineSetting("CurrentTemplatesBPPath", "", "Path to BuildPad config", "For example " + @"C:\Program Files\Guild Wars 2\addons\arcdps\arcdps.buildpad\config.ini".Replace(" ", "\u00A0"));

            _settingFontSize.SettingChanged += UpdateCurrentTemplatesSettings_Font;
            _settingAlign.SettingChanged += UpdateCurrentTemplatesSettings_Font;
            _settingLoc.SettingChanged += UpdateCurrentTemplatesSettings_Location;
            _settingDrag.SettingChanged += UpdateCurrentTemplatesSettings_Show;
            _settingBuildPad.SettingChanged += UpdateCurrentTemplatesSettings_BuildPadShow;
            _settingBuildPadPath.SettingChanged += UpdateCurrentTemplatesSettings_BuildPadConfigPath;
        }

        public override IView GetSettingsView()
        {
            return settingsView;
        }

        protected override void Initialize()
        {
            settingsView = new Views.SettingsView();
        }

        private void Gw2ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            // Reset our check interval so that we check immediately now that we have a new token.
            _lastTemplateCheck = INTERVAL_CHECKTEMPLATES;
        }

        protected override async Task LoadAsync()
        {
            Gw2ApiManager.SubtokenUpdated += Gw2ApiManager_SubtokenUpdated;
            templatesControl = new Controls.DrawTemplates();
            templatesControl.Parent = GameService.Graphics.SpriteScreen;
            UpdateCurrentTemplatesSettings_Font();
            UpdateCurrentTemplatesSettings_Location();
            UpdateCurrentTemplatesSettings_Show();
            UpdateCurrentTemplatesSettings_BuildPadShow();
            UpdateCurrentTemplatesSettings_BuildPadConfigPath();
            DetectBuildPadPath();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {
            // Hide when in character select, loading screens, world map
            if (GameService.GameIntegration.Gw2Instance.IsInGame && !GameService.Gw2Mumble.UI.IsMapOpen)
            {
                templatesControl.Show();
            }
            else
            {
                templatesControl.Hide();
            }

            // Reset check interval when player changes character
            if (characterName != GameService.Gw2Mumble.PlayerCharacter.Name)
            {
                characterName = GameService.Gw2Mumble.PlayerCharacter.Name;
                Logger.Debug("Changing character to '{0}'", characterName);
                ResetTemplates();
                _lastTemplateCheck = INTERVAL_CHECKTEMPLATES;
            }
            Utility.UpdateCadenceUtil.UpdateAsyncWithCadence(CheckTemplates, gameTime, INTERVAL_CHECKTEMPLATES, ref _lastTemplateCheck);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            Gw2ApiManager.SubtokenUpdated -= Gw2ApiManager_SubtokenUpdated;
            _settingFontSize.SettingChanged -= UpdateCurrentTemplatesSettings_Font;
            _settingAlign.SettingChanged -= UpdateCurrentTemplatesSettings_Font;
            _settingLoc.SettingChanged -= UpdateCurrentTemplatesSettings_Location;
            _settingDrag.SettingChanged -= UpdateCurrentTemplatesSettings_Show;
            _settingBuildPad.SettingChanged -= UpdateCurrentTemplatesSettings_BuildPadShow;
            _settingBuildPadPath.SettingChanged -= UpdateCurrentTemplatesSettings_BuildPadConfigPath;
            templatesControl?.Dispose();
        }

        private void InitBuildPad()
        {
            if (Utility.BuildPad.ValidPath(_settingBuildPadPath.Value))
            {
                buildPad = new Utility.BuildPad(_settingBuildPadPath.Value);
                _hasBuildPad = true;
            } else
            {
                buildPad = null;
                _hasBuildPad = false;
            }
            settingsView.ToggleIncorrectPathWarning();
        }

        private void DetectBuildPadPath()
        {
            if (_settingBuildPadPath.Value != "") return;

            GW2_PATHS.ForEach((path) =>
            {
                string fullPath = System.IO.Path.Combine(new string[] { path, BUILD_PAD_PATH });
                if (System.IO.File.Exists(fullPath))
                {
                    _settingBuildPadPath.Value = System.IO.Path.GetFullPath(fullPath);
                    return;
                }
            });
        }

        private async Task CheckTemplates(GameTime gameTime)
        {
            if (_lastTemplateCheck < 0)
            {
                Logger.Debug("No token yet, skipping.");
                return;
            }

            try
            {
                if (Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Characters, TokenPermission.Inventories, TokenPermission.Builds }))
                {
                    Logger.Debug("Getting character '{0}' from the API.", characterName);

                    templatesControl.ShowSpinner();
                    character = await Gw2ApiManager.Gw2ApiClient.V2.Characters.GetAsync(characterName);
                    templatesControl.HideSpinner();

                    UpdateTemplates();

                    Logger.Debug("Loaded '{0}' and '{1}' from the API.", templatesControl.buildName, templatesControl.equipmentName);
                }
                else
                {
                    Logger.Debug("Skipping API call - API key does not give us permissions.");
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to load character '{0}'.", characterName);
            }
        }

        private void ResetTemplates()
        {
            templatesControl.buildName = "";
            templatesControl.equipmentName = "";
        }

        private void UpdateTemplates()
        {
            if (character != null)
            {
                templatesControl.buildName = TemplateName(character.ActiveBuildTab, character.BuildTabs);
                templatesControl.equipmentName = TemplateName(character.ActiveEquipmentTab, character.EquipmentTabs);
            }
        }

        private string TemplateName<T>(int? activeTab, IReadOnlyList<T> tabs)
        {
            if (activeTab == null) return "Missing!";
            T tab = tabs[(int)activeTab - 1];
            string tabName = "";
            string suffix = ""; // TODO extract suffix to instance variable and add color (optional?)
            if (typeof(T) == typeof(CharacterBuildTabSlot))
            {
                string? buildPadName = _settingBuildPad.Value && buildPad != null ?
                    buildPad.GetName(((CharacterBuildTabSlot)(object)tab).Build) :
                    null;

                if (buildPadName != null)
                {
                    tabName = buildPadName;
                    suffix = " [BP]";
                } else
                {
                    tabName = ((CharacterBuildTabSlot)(object)tab).Build.Name;
                    suffix = _settingBuildPad.Value ? " [Not in BP]" : "";
                }
            } else if (typeof(T) == typeof(CharacterEquipmentTabSlot))
            {
                tabName = ((CharacterEquipmentTabSlot)(object)tab).Name;
            }
            if (tabName == "")
            {
                tabName = "Tab " + activeTab.ToString();
            }
            return tabName + suffix;
        }

        private void UpdateCurrentTemplatesSettings_Show(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            templatesControl.Drag = _settingDrag.Value;
        }
        private void UpdateCurrentTemplatesSettings_Font(object sender = null, ValueChangedEventArgs<string> e = null)
        {
            templatesControl.Font_Size = (ContentService.FontSize)Enum.Parse(typeof(ContentService.FontSize), "Size" + _settingFontSize.Value);
            templatesControl.Align = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), _settingAlign.Value);
        }
        private void UpdateCurrentTemplatesSettings_Location(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            templatesControl.Location = _settingLoc.Value;
        }

        private void UpdateCurrentTemplatesSettings_BuildPadShow(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            if (!templatesControl.BuildPad && _settingBuildPad.Value)
            {
                DetectBuildPadPath();
                InitBuildPad();
            }
            templatesControl.BuildPad = _settingBuildPad.Value;
            UpdateTemplates();
        }

        private void UpdateCurrentTemplatesSettings_BuildPadConfigPath(object sender = null, ValueChangedEventArgs<string> e = null)
        {
            if (templatesControl.BuildPadConfigPath != _settingBuildPadPath.Value)
            {
                templatesControl.BuildPadConfigPath = _settingBuildPadPath.Value;
                InitBuildPad();
                UpdateTemplates();
            }
        }
    }

}
