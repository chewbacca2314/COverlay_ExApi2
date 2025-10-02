using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using ExileCore2;
using ExileCore2.PoEMemory.Elements.AtlasElements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using GameOffsets2.Native;
using Microsoft.VisualBasic.Logging;
using RectangleF = ExileCore2.Shared.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace cOverlay
{
    public partial class cOverlay : BaseSettingsPlugin<cOverlaySettings>
    {
        public State state = new State();
        private AtlasPanel atlasPanel;

        private AtlasNodeDescription[] atlasNodes = [];
        private AtlasNodeDescription[] emptyTowersList = [];
        private HashSet<AtlasNodeDescription> towerNodes = new HashSet<AtlasNodeDescription>();
        private HashSet<Node> processingNodes = new HashSet<Node>();
        private (Vector2i, Vector2i, Vector2i, Vector2i, Vector2i)[] atlasPoints = [];

        public Stopwatch atlasRefreshSw = Stopwatch.StartNew();
        public Stopwatch contentSw = Stopwatch.StartNew();
        public Stopwatch swRun = Stopwatch.StartNew();

        public override bool Initialise()
        {
            state.Init();
            state.Load();
            atlasPanel = GameController.IngameState.IngameUi.WorldMap.AtlasPanel;

            return true;
        }
        private Color GenerateAreaColor(string areaName)
        {
            //Base color: #EEDC31 (238, 220, 49) - golden yellow
            //#794D14 (121, 77, 20) - dark brown

            var baseR = 121;
            var baseG = 77;
            var baseB = 20;
            var baseA = 238; //Alpha

            //Generate hash for area name
            var hash = areaName.GetHashCode();
            var variation = Math.Abs(hash % 60) - 30;

            //Apply variations to RGB based on hash
            var r = Math.Max(0, Math.Min(255, baseR + variation));
            var g = Math.Max(0, Math.Min(255, baseG + variation));
            var b = Math.Max(0, Math.Min(255, baseB + variation));

            return Color.FromArgb(baseA, r, g, b);
        }

        public override void AreaChange(AreaInstance area)
        {
            atlasNodes = [];
            atlasPoints = [];
            processingNodes = [];
            emptyTowersList = [];
            towerNodes.Clear();

            atlasRefreshSw.Restart();
            swRun.Restart();
            contentSw.Restart();
        }

        public bool IsTower(AtlasPanelNode node)
        {
            return node.Area.Name == "Bluff"
                                || node.Area.Name == "Lost Towers"
                                || node.Area.Name == "Sinking Spire"
                                || node.Area.Name == "Mesa"
                                || node.Area.Name == "Alpine Ridge";
        }

        public override void Tick()
        {
            var counter = 0;
            atlasPanel = GameController.IngameState.IngameUi.WorldMap.AtlasPanel;
            if (atlasPanel.IsVisible)
            {
                if (atlasRefreshSw.ElapsedMilliseconds > state.atlasRefreshRate || counter == 0)
                {
                    var atlasNodeCount = atlasNodes.Count();
                    var atlasDescCount = atlasPanel.Descriptions.Count;
                    if (atlasNodeCount < atlasDescCount)
                    {
                        atlasNodes = atlasPanel.Descriptions.ToArray();
                        atlasPoints = atlasPanel.Points.ToArray();

                        counter++;
                    }

                    atlasRefreshSw.Restart();
                }

                if (swRun.ElapsedMilliseconds > state.screnRefreshRate)
                {
                    foreach (var node in atlasNodes)
                    {
                        if (!Utility.IsInScreen(node, state))
                        {
                            processingNodes.RemoveWhere(x => x.NodeCoords == node.Coordinate);
                            towerNodes.RemoveWhere(x => x.Coordinate == node.Coordinate);
                            continue;
                        }

                        if (!processingNodes.Any(x => x.NodeCoords == node.Coordinate))
                        {
                            if (IsTower(node.Element) && node.Element.IsCompleted)
                            {
                                towerNodes.Add(node);
                            }
                            if (!node.Element.IsCompleted)
                            {
                                List<AtlasNodeDescription> nNodes = new List<AtlasNodeDescription>();

                                if (state.drawConnections)
                                {
                                    var point = atlasPoints.FirstOrDefault(x => x.Item1 == node.Coordinate);
                                    foreach (var nd in atlasNodes)
                                    {
                                        if (nd.Coordinate == point.Item2 ||
                                            nd.Coordinate == point.Item3 ||
                                            nd.Coordinate == point.Item4 ||
                                            nd.Coordinate == point.Item5)
                                        {
                                            if (!nd.Element.IsCompleted)
                                                nNodes.Add(nd);
                                        }
                                    }
                                }

                                if (!state.NodeColors.ContainsKey(node.Element.Area.Name))
                                {
                                    state.NodeColors.Add(node.Element.Area.Name, GenerateAreaColor(node.Element.Area.Name));
                                    state.Save();
                                    state.Load();
                                }

                                if (!state.NameColors.ContainsKey(node.Element.Area.Name))
                                {
                                    state.NameColors.Add(node.Element.Area.Name, Color.White);
                                    state.Save();
                                    state.Load();
                                }

                                var towers = atlasNodes.Where(x => IsTower(x.Element));
                                var nearbyTowers = towers.Where(x => Vector2.Distance(x.Coordinate, node.Coordinate) <= state.towersRange).ToArray();
                                var affectedTowers = nearbyTowers.Where(x => atlasPanel.EffectSources.Any(y => x.Coordinate == y.Coordinate));
                                emptyTowersList = towerNodes.Where(x => !atlasPanel.EffectSources.Any(y => x.Coordinate == y.Coordinate)).ToArray();

                                //LogMessage($"Node at {node.Coordinate} added to processing");
                                processingNodes.Add(new Node(node.Coordinate, node, nNodes.ToList(), nearbyTowers.Count(), affectedTowers.Count()));
                                nNodes.Clear();
                            }
                        }
                    }

                    swRun.Restart();
                }
            }
            else
            {
                counter = 0;
            }

            ListenHotkeys();
        }

        public void ListenHotkeys()
        {
            if (atlasPanel.IsVisible)
            {
                if (Utility.IsKeyPressedOnce(state.KeybindCreateWaypoint))
                {
                    CreateWaypoint();
                }

                if (Utility.IsKeyPressedOnce(state.KeybindRefreshNodes))
                {
                    LogMessage($"Nodes refreshed");
                    processingNodes = [];
                    atlasNodes = [];
                }
            }

            if (Utility.IsKeyPressedOnce(state.KeybindShowWaypointPanel))
            {
                state.ShowWaypointMenu = !state.ShowWaypointMenu;
            }

            if (Utility.IsKeyPressedOnce(state.KeybindSaveSettings))
            {
                LogMessage($"Settings saved");
                state.Save();
            }
        }

        public override void Render()
        {
            if (state.ShowWaypointMenu)
            {
                DrawWaypointPanel();
            }

            if (atlasPanel.IsVisible)
            {
                if (state.drawConnections)
                {
                    foreach (var nodeObject in processingNodes)
                    {
                        DrawConnections(nodeObject);
                    }
                }
                if (state.DebugDrawPerfomance)
                {
                    Graphics.DrawText($"atlas nodes {atlasNodes.Count()} " +
                       $"\ndraw nodes {processingNodes.Count} " +
                       $"\nrender ms {PluginManager.Plugins.First(x => x.Name == "cOverlay").RenderDebugInformation.TickAverage}" +
                       $"\ntick m,s {PluginManager.Plugins.First(x => x.Name == "cOverlay").TickDebugInformation.TickAverage}" +
                       $"\natlasDesc {atlasPanel.Descriptions.Count}" +
                       $"\n{atlasPanel.Camera.Snapshot.Matrix.Translation}",
                       new Vector2(200, 200),
                       Color.LightGreen);
                }
                foreach (var nodeObject in processingNodes)
                {
                    var node = nodeObject.NodeObject.Element;
                    if (!IsTower(node))
                    {
                        int textOffset = -30; //Draws text above the node to avoid overlapping
                        
                        if (IsCorruptedNexus(nodeObject.NodeObject.Element) && state.ToggleCorruptedNexus)
                        {
                            Graphics.DrawTextWithBackground("Corrupted Nexus", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.Red, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.Black);
                        }

                        if (IsSealedVault(node))
                        {
                            Graphics.DrawTextWithBackground("** Sealed Vault **", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.Gold, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.Black);
                        }

                        if (IsJadeIsles(node))
                        {
                            Graphics.DrawTextWithBackground("** Jade Isles **", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.Green, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.Black);
                        }

                        if (IsCastaway(node))
                        {
                            Graphics.DrawTextWithBackground("** Castaway **", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.Cyan, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.Black);
                        }

                        if (IsCitadel(node))
                        {
                            Graphics.DrawTextWithBackground("** Citadel **", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.Purple, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.Black);
                        }

                        if (IsSilentCave(node))
                        {
                            Graphics.DrawTextWithBackground("** Silent Cave **", 
                                new Vector2(node.Center.X, node.Center.Y + textOffset), 
                                Color.DarkSlateGray, 
                                ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, 
                                Color.White);
                        }
                        
                        if ((nodeObject.TowersCount >= state.HighTowerAmountThreshold ||
                            (IsCleansed(nodeObject.NodeObject.Element) || 
                            nodeObject.NodeObject.Element.IsCorrupted)
                            && nodeObject.TowersCount >= state.HighTowerAmountThreshold - 1
                            && state.ToggleCorruptedMaps)
                            && !nodeObject.NodeObject.Element.IsVisited)
                        {
                            DrawContent(node);
                            DrawNode(node);
                            DrawNodeMain(nodeObject);
                        }
                        else
                        {
                            if (state.ShowAllMapContent)
                            {
                                DrawContent(node);
                            }
                            DrawNodeTrash(node);
                        }
                    }
                    else
                    {
                        //Graphics.DrawCircle(new Vector2(node.Center.X, node.Center.Y), state.towersRange * 50, Color.Red, 10, 90);
                        Graphics.DrawTextWithBackground("I AM TOWER", new Vector2(node.Center.X, node.Center.Y), Color.Yellow, ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, Color.Black);
                    }

                    if (state.drawDebug)
                    {
                        DrawDebug(nodeObject);
                    }
                }

                foreach (var _tower in emptyTowersList)
                {
                    var tower = _tower;

                    Graphics.DrawTextWithBackground("MISSING TABLET", new Vector2(tower.Element.Center.X, tower.Element.Center.Y), Color.Red, ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter, Color.Black);

                    if (tower.Element.IsCompleted)
                    {
                    }
                }
            }
        }

        public void DrawNode(AtlasPanelNode node)
        {
            var nodeRadius = state.NodeRadius;
            Graphics.DrawCircleFilled(new Vector2(node.Center.X, node.Center.Y), nodeRadius, state.NodeColors[node.Area.Name], 20);
        }

        public void DrawNodeTrash(AtlasPanelNode node)
        {
            var nodeRadius = state.NodeRadius;
            var _traversalColor = state.traversalColor;
            var _untraversalColor = state.untraversalColor;
            Graphics.DrawCircleFilled(new Vector2(node.Center.X, node.Center.Y), node.CanTraverse ? state.nodeRadiusTrash + 5 : state.nodeRadiusTrash, node.CanTraverse ? _traversalColor : _untraversalColor, 20);
        }

        public void DrawNodeMain(Node _node)
        {
            var node = _node.NodeObject.Element;
            var resultText = state.showTowersAtName ? $"{node.Area.Name} [{_node.AffectedTowersCount}/{_node.TowersCount}]" : node.Area.Name;
            var textSize = Graphics.MeasureText(resultText);
            var padding = state.paddingName;
            var rounding = state.nodeTextRounding;
            var backgroundRect = new RectangleF(
                node.Center.X - textSize.X / 2 - padding.X,
                node.Center.Y - textSize.Y / 2 - padding.Y,
                textSize.X + padding.X * 2,
                textSize.Y + padding.Y * 2);

            Graphics.DrawBox(
                backgroundRect,
                _node.TowersCount == _node.AffectedTowersCount && !IsTower(node) && state.RecolorHighTowersAmount ? state.HighTowerAmountColorBg : state.NodeColors[node.Area.Name]
                /*node.Content.Any(x => x.Name == "Irradiated" || x.Name == "Map Boss" && node.IsCorrupted) ? Color.White : state.NodeColors[node.Area.Name]*/,
                rounding);

            var baseTextColor = _node.AffectedTowersCount == _node.TowersCount && state.RecolorHighTowersAmount ? state.HighTowerAmountColorTxt : state.NameColors[node.Area.Name];
            Color finalTextColor = node.CanTraverse ? baseTextColor : Color.FromArgb(state.traversalTransparency, baseTextColor);

            var areaColor = _node.AffectedTowersCount == _node.TowersCount ? state.HighTowerAmountColorTxt : state.NameColors[node.Area.Name];
            Color transparentColor = Color.FromArgb(state.traversalTransparency, areaColor);

            Graphics.DrawText(
                resultText,
                backgroundRect.Center,
                finalTextColor,
                //node.Content.Any(x => x.Name == "Irradiated" || x.Name == "Map Boss" && node.IsCorrupted) ? Color.Black : state.DrawTextSameColor ? state.AreaTextColor : state.NameColors[node.Area.Name],
                ExileCore2.Shared.Enums.FontAlign.VerticalCenter | ExileCore2.Shared.Enums.FontAlign.Center);

            //DrawTraversal(_node, backgroundRect);
        }

        public void DrawContent(AtlasPanelNode node)
        {
            var nodeContent = node.Content;
            if (node.Content != null)
            {
                var textSize = Graphics.MeasureText(node.Area.Name.ToLower());
                var padding = state.paddingName;
                var rounding = state.nodeTextRounding;

                int counter = 1;

                if (state.ToggleGemling && node.IsCorrupted)
                {
                    Graphics.DrawCircle(
                        new Vector2(node.Center.X, node.Center.Y),
                        state.NodeRadius + state.GapRadius + (float)(counter * state.ContentCircleThickness),
                        state.GemlingColor,
                        state.ContentCircleThickness, 20);
                    counter++;
                }

                if ((state.ToggleGemling && IsCleansed(node)))
                {
                    Graphics.DrawCircle(
                        new Vector2(node.Center.X, node.Center.Y),
                        state.NodeRadius + state.GapRadius + (float)(counter * state.ContentCircleThickness),
                        Color.White,
                        state.ContentCircleThickness, 20);
                    counter++;
                }

                if (state.ToggleGemling && IsCorruptedNexus(node))
                {
                    Graphics.DrawCircle(
                        new Vector2(node.Center.X, node.Center.Y),
                        state.NodeRadius + state.GapRadius + (float)(counter * state.ContentCircleThickness),
                        Color.Red,
                        state.ContentCircleThickness, 20);
                    counter++;
                }

                foreach (var content in nodeContent)
                {
                    // Ensure content keys exist in dictionaries before accessing them
                    if (!state.ContentCircleColor.ContainsKey(content.Name))
                    {
                        state.ContentCircleColor.Add(content.Name, Color.White);
                    }
                    if (!state.ContentToggle.ContainsKey(content.Name))
                    {
                        state.ContentToggle.Add(content.Name, true);
                    }

                    if (contentSw.ElapsedMilliseconds > 10000)
                    {
                        contentSw.Restart();
                    }

                    if (state.ContentToggle[content.Name])
                    {
                        Graphics.DrawCircle(
                            new Vector2(node.Center.X, node.Center.Y),
                            state.NodeRadius + state.GapRadius + (float)(counter * state.ContentCircleThickness),
                            state.ContentCircleColor[content.Name],
                            state.ContentCircleThickness, 20);
                        counter++;
                        //Graphics.DrawBox(
                        //    new Vector2(node.Center.X - textSize.X / 2 - padding.X / 2 - counter * state.ContentCircleThickness[content.Name], node.Center.Y - textSize.Y / 2 - padding.Y / 2 - counter * state.ContentCircleThickness[content.Name]),
                        //    new Vector2(node.Center.X + textSize.X / 2 + padding.X / 2 + counter * state.ContentCircleThickness[content.Name], node.Center.Y + textSize.Y / 2 + padding.Y / 2 + counter * state.ContentCircleThickness[content.Name]),
                        //    state.ContentCircleColor[content.Name],
                        //    rounding);
                    }
                }
            }
        }

        public void DrawDebug(Node node)
        {
            var counter = 0;
            var nodeCoords = node.NodeObject.Coordinate;
            var nodeElement = node.NodeObject.Element;
            var nodeCenter = nodeElement.Center;
            var resultText = "";

            if (state.DebugDrawCoordinates)
            {
                if (counter > 0)
                {
                    resultText += "\n";
                }
                resultText += $"x{nodeCoords.X} y{nodeCoords.Y}";
                resultText += $"\n{node.NodeObject.Element.Area.Name}";
                resultText += $"\n{node.NodeObject.Element.IsTower}";
                counter++;
            }
            if (state.DebugDrawNodePosition)
            {
                if (counter > 0)
                {
                    resultText += "\n";
                }
                resultText += $"X{nodeElement.X:N0} nY{nodeElement.Y:N0}";
                counter++;
            }
            if (state.DebugDrawNodeCenterPosition)
            {
                if (counter > 0)
                {
                    resultText += "\n";
                }
                resultText += $"cX{nodeCenter.X:N0} cY{nodeCenter.Y:N0}";
                counter++;
            }
            if (state.DebugDrawAttempted)
            {
                if (counter > 0)
                {
                    resultText += "\n";
                }
                resultText += $"attempted {node.NodeObject.Element.IsVisited}";
                counter++;
            }
            if (state.DebugDrawContentAmount)
            {
                if (counter > 0)
                {
                    resultText += "\n";
                }
                resultText += $"nodeContent {node.NodeObject.Element.Content.ToList().Count()}";
                counter++;
            }
            if (state.DebugDrawContentNames)
            {
                foreach (var content in nodeElement.Content)
                {
                    if (counter > 0)
                    {
                        resultText += "\n";
                    }
                    resultText += $"content: {content.Name}";
                    counter++;
                }
                resultText += $"\ncleansed: {IsCleansed(node.NodeObject.Element)}\nnexus: {IsCorruptedNexus(node.NodeObject.Element)}";
            }
            Graphics.DrawTextWithBackground(
                    resultText,
                    new Vector2(nodeCenter.X, nodeCenter.Y + 30),
                    Color.Black,
                    ExileCore2.Shared.Enums.FontAlign.Center | ExileCore2.Shared.Enums.FontAlign.VerticalCenter,
                    Color.Green);
        }
        public bool IsCleansed (AtlasPanelNode node)
        {
            bool result = false;
            if (node.Children.Any(x => x.Children.Any(y => !string.IsNullOrEmpty(y.Tooltip.Text) && y.Tooltip.Text.Contains("Cleansed"))))
            {
                result = true;
            }
            return result;
        }
        public bool IsCorruptedNexus(AtlasPanelNode node)
        {
            if (state.ToggleCorruptedNexus == false) return false;
            try
            {
                //Used for debugging, will log all found texture names
                /*
                foreach (var child in node.GetChildAtIndex(0).Children)
                {
                    if (!string.IsNullOrEmpty(child.TextureName))
                    {
                        LogMessage($"Found texture: {child.TextureName}");
                        if (child.TextureName.Contains("CorruptionNexus"))
                            return true;
                    }
                } */
                return node.GetChildAtIndex(0).Children.Any(x => x.Children.Any(y => y.TextureName.Contains("CorruptionNexus") == true));
            }
            catch
            {
                LogError($"Error checking for Corrupted Nexus");
            }
            return false;
        }
        public bool IsSealedVault(AtlasPanelNode node)
        {
            return state.ToggleSealedVault && node.Area.Name.Contains("Sealed Vault");
        }

        public bool IsJadeIsles(AtlasPanelNode node)
        {
            return state.ToggleJadeIsles && node.Area.Name.Contains("Jade Isles");
        }

        public bool IsCastaway(AtlasPanelNode node)
        {
            return state.ToggleCastaway && node.Area.Name.Contains("Castaway");
        }

        public bool IsCitadel(AtlasPanelNode node)
        {
            return state.ToggleCitadel && node.Area.Name.Contains("Citadel");
        }

        public bool IsSilentCave(AtlasPanelNode node)
        {
            return state.ToggleSilentCave && node.Area.Name.Contains("The Silent Cave");
        }
        public void DrawConnections(Node node)
        {
            foreach (var ne in node.neighbourNodes)
            {
                Graphics.DrawLine(new Vector2(node.NodeObject.Element.Center.X, node.NodeObject.Element.Center.Y), new Vector2(ne.Element.Center.X, ne.Element.Center.Y), state.ConnectionsThickness, state.ConnectionsColor);
            }
        }

        public void DrawTraversal(Node node, RectangleF rect)
        {
            if (node.NodeObject.Element.CanTraverse)
            {
                Graphics.DrawLine(rect.TopLeft, rect.TopRight, 5, Color.Aqua);
            }
        }

        public void CreateWaypoint()
        {
            var node = GetClosestToCursorNode();
            LogMessage(node.NodeObject.Coordinate.ToString());
            var PositionX = atlasPanel.Camera.Snapshot.Matrix.Translation.X;
            var PositionY = atlasPanel.Camera.Snapshot.Matrix.Translation.Y;
            var Name = node.NodeObject.Element.Area.Name;
            var TowersCount = node.TowersCount;

            Waypoint waypoint = new Waypoint(PositionX, PositionY, Name, TowersCount, node.NodeCoords);

            state.WaypointList.Add(waypoint);
        }

        public void DeleteWaypoint(Vector2 position)
        {
            state.WaypointList.RemoveAll(x => new Vector2(x.PositionX, x.PositionY) == position);
        }

        public Node GetClosestToCursorNode()
        {
            var cursorPos = new Vector2(GameController.IngameState.MousePosX, GameController.IngameState.MousePosY);
            var closestNode = processingNodes.OrderBy(x => Vector2.Distance(new Vector2(x.NodeObject.Element.Center.X, x.NodeObject.Element.Center.Y), cursorPos)).FirstOrDefault();

            return closestNode;
        }

        public override void EntityAdded(Entity entity)
        {
            //If you have a reason to process every entity only once,
            //this is a good place to do so.
            //You may want to use a queue and run the actual
            //processing (if any) inside the Tick method.
        }
    }
}