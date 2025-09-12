using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using AutoMapper;

namespace cOverlay
{
    public class State
    {
        public State()
        {
        }

        private static string GetProjectConfigPath()
        {
            // Fetch plugin path
            string pluginPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            DirectoryInfo currentDir = new DirectoryInfo(Path.GetDirectoryName(pluginPath));
            
            // Fetch project root path
            DirectoryInfo projectRoot = currentDir.Parent?.Parent?.Parent;
            
            if (projectRoot != null && projectRoot.Exists)
            {
                string configPath = Path.Combine(projectRoot.FullName, "config", "cOverlay");
                
                // Ensure the config directory exists
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }
                
                return configPath;
            }
            
            // Fallback to relative config dir
            string fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "config", "cOverlay");
            if (!Directory.Exists(fallbackPath))
            {
                Directory.CreateDirectory(fallbackPath);
            }
            return fallbackPath;
        }

        public static string settingsPath = GetProjectConfigPath();

        // Keybinds

        public Keys KeybindCreateWaypoint = Keys.F1;
        public Keys KeybindShowWaypointPanel = Keys.F2;
        public Keys KeybindSaveSettings = Keys.F3;
        public Keys KeybindRefreshNodes = Keys.F4;

        // Draw toggles

        public bool drawDebug = false;
        public bool drawConnections = false;
        public bool RecolorHighTowersAmount = false;
        public bool DrawTowerRange = false;
        public bool ShowAllMapContent = false;

        // Colors

        public Color AreaTextColor = Color.White;
        public Color ConnectionsColor = Color.AliceBlue;
        public Color BackgroundAreaColor = Color.Black;
        public Color TowerTextColor = Color.White;
        public Color HighTowerAmountColorBg = Color.White;
        public Color HighTowerAmountColorTxt = Color.Black;
        public Color traversalColor = Color.FromArgb(255, 108, 180, 235);
        public Color untraversalColor = Color.Gray;

        // Style settings

        public int HighTowerAmountThreshold = 2;
        public Vector2 paddingName = new Vector2(3, 2);
        public int ConnectionsThickness = 2;
        public int nodeTextRounding = 3;
        public int NodeRadius = 20;
        public int traversalTransparency = 90;
        public int nodeRadiusTrash = 15;
        public bool showTowersAtName = true;
        public float towersRange = 12;

        // Node settings

        public Dictionary<string, Color> NodeColors = new Dictionary<string, Color>();
        public Dictionary<string, Color> NameColors = new Dictionary<string, Color>();

        // General Settings

        public int borderX = 1920;
        public int borderY = 1080;
        public int atlasRefreshRate = 10000;
        public int screnRefreshRate = 1000;

        // Content

        public bool ToggleGemling = true;
        public bool ToggleCorruptedMaps = true;
        public bool ToggleCorruptedNexus = true;
        public bool ToggleSealedVault = true;
        public bool ToggleJadeIsles = true;
        public bool ToggleCastaway = true;
        public bool ToggleCitadel = true;
        public bool ToggleSilentCave = true;
        public Color GemlingColor = Color.Orange;
        public int ContentCircleThickness = 2;
        public int GapRadius = 5;
        public Dictionary<string, bool> ContentToggle = new Dictionary<string, bool>();
        public Dictionary<string, Color> ContentCircleColor = new Dictionary<string, Color>();
        public List<Content> ContentSettings { get; set; } = new List<Content>();

        // Waypoints

        public bool ShowWaypointMenu = false;
        public Vector2 WaypointWindowPos = new Vector2(100, 100);
        public List<Waypoint> WaypointList { get; set; } = new List<Waypoint>();

        // Debug

        public bool DebugDrawPerfomance = false;
        public bool DebugDrawCoordinates = false;
        public bool DebugDrawContentAmount = false;
        public bool DebugDrawContentNames = false;
        public bool DebugDrawAttempted = false;
        public bool DebugDrawNodePosition = false;
        public bool DebugDrawNodeCenterPosition = false;

        // ---------------

        public void Load()
        {
            string settingsFilePath = Path.Combine(settingsPath, "StateSettings.json");

            if (File.Exists(settingsFilePath))
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap<State, State>());
                var mapper = config.CreateMapper();

                var obj = SerializationApi.Deserialize<State>(settingsFilePath);
                if (obj != null)
                {
                    mapper.Map(obj, this);
                    NodeColors = obj.NodeColors.OrderBy(x => x.Key).ToDictionary<string, Color>();
                    NameColors = obj.NameColors.OrderBy(x => x.Key).ToDictionary<string, Color>();

                    ContentToggle = obj.ContentToggle ?? new Dictionary<string, bool>();
                    ContentCircleColor = obj.ContentCircleColor ?? new Dictionary<string, Color>();

                    foreach (var contentType in Content.ContentType)
                    {
                        if (!ContentToggle.ContainsKey(contentType))
                        {
                            ContentToggle.Add(contentType, true);
                        }

                        if (!ContentCircleColor.ContainsKey(contentType))
                        {
                            ContentCircleColor.Add(contentType, Color.White);
                        }
                    }
                }
            }
            else
            {
                using (var fs = File.Create(settingsFilePath))
                {
                    //Created StateSettings.json, disposing stream
                }
            }
        }

        public void Save()
        {
            string settingsFilePath = Path.Combine(settingsPath, "StateSettings.json");

            if (!File.Exists(settingsFilePath))
            {
                using (var fs = File.Create(settingsFilePath))
                {
                    //Created StateSettings.json, disposing stream
                }
            }

            SerializationApi.Serialize<State>(this, settingsFilePath);
        }

        public void Init()
        {
            string settingsFilePath = Path.Combine(settingsPath, "StateSettings.json");

            if (!File.Exists(settingsFilePath))
            {
                using (var fs = File.Create(settingsFilePath))
                {
                    //Created StateSettings.json, disposing stream
                }
                SerializationApi.Serialize<State>(this, settingsFilePath);
            }

            var obj = SerializationApi.Deserialize<State>(settingsFilePath);
            if (obj != null)
            {
                if (obj.ContentSettings.Count < (Content.ContentType.Count()))
                {
                    foreach (var content in Content.ContentType)
                    {
                        if (!obj.ContentSettings.Any(x => x.Type == content))
                        {
                            obj.ContentSettings.Add(new Content(content));
                        }
                    }
                }

                foreach (var contentType in Content.ContentType)
                {
                    if (!obj.ContentToggle.ContainsKey(contentType))
                    {
                        obj.ContentToggle.Add(contentType, true);
                    }

                    if (!obj.ContentCircleColor.ContainsKey(contentType))
                    {
                        obj.ContentCircleColor.Add(contentType, Color.White);
                    }
                }

                SerializationApi.Serialize<State>(obj, settingsFilePath);
            }
            else
            {
                foreach (var contentType in Content.ContentType)
                {
                    ContentToggle.Add(contentType, true);
                    ContentCircleColor.Add(contentType, Color.White);
                }
                SerializationApi.Serialize<State>(this, settingsFilePath);
            }
        }
    }
}
