using System;
using System.IO;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using DelvUI.Config;
using DelvUI.Interface;
using DelvUI.Helpers;
using DelvUI.Interface.GeneralElements;
using FFXIVClientStructs;
using ImGuiNET;
using SigScanner = Dalamud.Game.SigScanner;

namespace DelvUI
{
    public class Plugin : IDalamudPlugin
    {
        public static ClientState ClientState { get; private set; }
        public static CommandManager CommandManager { get; private set; }
        public static Condition Condition { get; private set; }
        public static DalamudPluginInterface PluginInterface { get; private set; }
        public static DataManager DataManager { get; private set; }
        public static Framework Framework { get; private set; }
        public static GameGui GameGui { get; private set; }
        public static JobGauges JobGauges { get; private set; }
        public static ObjectTable ObjectTable { get; private set; }
        public static SigScanner SigScanner { get; private set; }
        public static TargetManager TargetManager { get; private set; }
        public static UiBuilder UiBuilder { get; private set; }

        public static ImGuiScene.TextureWrap BannerTexture;

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public string AssemblyLocation { get; set; } = Assembly.GetExecutingAssembly().Location;
        public string Name => "DelvUI";
        public static string Version { get; private set; } = "";
        
        private bool _fontBuilt;
        private bool _fontLoadFailed;
        private HudManager _hudManager;
        private SystemMenuHook _menuHook;

        public Plugin(
            ClientState clientState,
            CommandManager commandManager,
            Condition condition,
            DalamudPluginInterface pluginInterface,
            DataManager dataManager,
            Framework framework,
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable,
            SigScanner sigScanner,
            TargetManager targetManager,
            UiBuilder uiBuilder
        )
        {
            ClientState = clientState;
            CommandManager = commandManager;
            Condition = condition;
            PluginInterface = pluginInterface;
            DataManager = dataManager;
            Framework = framework;
            GameGui = gameGui;
            JobGauges = jobGauges;
            ObjectTable = objectTable;
            SigScanner = sigScanner;
            TargetManager = targetManager;
            UiBuilder = uiBuilder;

            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";

            LoadBanner();

            // initialize a not-necessarily-defaults configuration
            ConfigurationManager.Initialize(false);
            FontsManager.Initialize();

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.BuildFonts += BuildFont;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;

            if (!_fontBuilt && !_fontLoadFailed)
            {
                PluginInterface.UiBuilder.RebuildFonts();
            }

            CommandManager.AddHandler(
                "/delvui",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage = "Opens the DelvUI configuration window.\n",
                    ShowInHelp = true
                }
            );

            _menuHook = new SystemMenuHook(PluginInterface);

            TexturesCache.Initialize();
            GlobalColors.Initialize();
            Resolver.Initialize();

            _hudManager = new HudManager();
        }

        public void Dispose()
        {
            _menuHook.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void BuildFont()
        {
            string fontFile = Path.Combine(Path.GetDirectoryName(AssemblyLocation) ?? "", "Media", "Fonts", "big-noodle-too.ttf");
            _fontBuilt = false;

            if (File.Exists(fontFile))
            {
                try
                {
                    FontsManager.Instance.BigNoodleTooFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontFile, 24);
                    _fontBuilt = true;
                }
                catch (Exception ex)
                {
                    PluginLog.Log($"Font failed to load. {fontFile}");
                    PluginLog.Log(ex.ToString());
                    _fontLoadFailed = true;
                }
            }
            else
            {
                PluginLog.Log($"Font doesn't exist. {fontFile}");
                _fontLoadFailed = true;
            }
        }

        private void LoadBanner()
        {
            string bannerImage = Path.Combine(Path.GetDirectoryName(AssemblyLocation) ?? "", "Media", "Images", "banner_short_x150.png");

            if (File.Exists(bannerImage))
            {
                try
                {
                    BannerTexture = PluginInterface.UiBuilder.LoadImage(bannerImage);
                }
                catch (Exception ex)
                {
                    PluginLog.Log($"Image failed to load. {bannerImage}");
                    PluginLog.Log(ex.ToString());
                }
            }
            else
            {
                PluginLog.Log($"Image doesn't exist. {bannerImage}");
            }
        }

        private void PluginCommand(string command, string arguments)
        {
            ConfigurationManager.GetInstance().DrawConfigWindow = !ConfigurationManager.GetInstance().DrawConfigWindow;
        }

        private void ReloadConfigCommand(string command, string arguments) { ConfigurationManager.GetInstance().LoadConfigurations(); }

        private void Draw()
        {
            bool hudState = Condition[ConditionFlag.WatchingCutscene]
                         || Condition[ConditionFlag.WatchingCutscene78]
                         || Condition[ConditionFlag.OccupiedInCutSceneEvent]
                         || Condition[ConditionFlag.CreatingCharacter]
                         || Condition[ConditionFlag.BetweenAreas]
                         || Condition[ConditionFlag.BetweenAreas51];

            PluginInterface.UiBuilder.OverrideGameCursor = false;

            ConfigurationManager.GetInstance().Draw();

            if (_fontBuilt)
            {
                ImGui.PushFont(FontsManager.Instance.BigNoodleTooFont);
            }

            if (!hudState)
            {
                _hudManager?.Draw();
            }

            if (_fontBuilt)
            {
                ImGui.PopFont();
            }
        }

        private void OpenConfigUi()
        {
            ConfigurationManager.GetInstance().DrawConfigWindow = !ConfigurationManager.GetInstance().DrawConfigWindow;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            ConfigurationManager.GetInstance().DrawConfigWindow = false;

            CommandManager.RemoveHandler("/delvui");
            PluginInterface.UiBuilder.Draw -= Draw;
            PluginInterface.UiBuilder.BuildFonts -= BuildFont;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            PluginInterface.UiBuilder.RebuildFonts();
        }
    }
}
