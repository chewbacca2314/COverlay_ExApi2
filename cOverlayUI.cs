using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ImGuiNET;

namespace cOverlay
{
    public partial class cOverlay
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public override void DrawSettings()
        {
            BuildSettings();
        }

        private void BuildSettings()
        {
            if (ImGui.Button("Save settings"))
            {
                state.Save();
            }
            if (ImGui.CollapsingHeader("Perfomance"))
            {
                DrawPerfomanceMenu();
            }

            if (ImGui.CollapsingHeader("Style settings"))
            {
                DrawStyleSettings();
            }

            if (ImGui.CollapsingHeader("Keybinds"))
            {
                DrawKeybindMenu();
            }

            if (ImGui.CollapsingHeader("Map settings"))
            {
                DrawMapSettings();
            }

            if (ImGui.CollapsingHeader("Content settings"))
            {
                DrawContentSettings();
            }

            if (ImGui.CollapsingHeader("Waypoints"))
            {
                DrawWaypointMenu();
            }

            if (ImGui.CollapsingHeader("Debug"))
            {
                DrawDebugMenu();
            }
        }

        private void DrawPerfomanceMenu()
        {
            ImGui.SliderInt("Atlas refresh rate", ref state.atlasRefreshRate, 1000, 20000);
            HelpMarker("How often new segments of atlas are updated. Default: 5000");
            ImGui.SliderInt("Screen refresh rate", ref state.screnRefreshRate, 100, 1000);
            HelpMarker("How often nodes on screen are updated. Default: 500");
            ImGui.SliderInt("BorderX", ref state.borderX, 500, 5000);
            HelpMarker("X coordinate (from screen) bound where nodes are updated. Default: 1920");
            ImGui.SliderInt("BorderY", ref state.borderY, 500, 5000);
            HelpMarker("Y coordinate (from screen) bound where nodes are updated. Default: 1080");
        }

        private void DrawStyleSettings()
        {
            var bgAreaColor = ColorConverter.ToVector4(state.BackgroundAreaColor);
            var textAreaColor = ColorConverter.ToVector4(state.AreaTextColor);
            var textTowerColor = ColorConverter.ToVector4(state.TowerTextColor);
            var highTowerAmountColorBg = ColorConverter.ToVector4(state.HighTowerAmountColorBg);
            var highTowerAmountColorTxt = ColorConverter.ToVector4(state.HighTowerAmountColorTxt);
            var connectionsColor = ColorConverter.ToVector4(state.ConnectionsColor);
            var _traversalColor = ColorConverter.ToVector4(state.traversalColor);
            var _untraversalColor = ColorConverter.ToVector4(state.untraversalColor);
            var bFlags = ImGuiColorEditFlags.NoInputs;

            ImGui.SeparatorText("Node settings");

            ImGui.SliderInt("Node radius", ref state.NodeRadius, 1, 30);
            HelpMarker("Default: 20");
            ImGui.SliderInt("Trash node radius", ref state.nodeRadiusTrash, 1, 30);
            HelpMarker("Default: 15");
            ImGui.SliderFloat("Towers' range", ref state.towersRange, 10, 15);
            ImGui.ColorEdit4("Doable color", ref highTowerAmountColorBg, bFlags);
            ImGui.ColorEdit4("Doable text color", ref highTowerAmountColorTxt, bFlags);
            ImGui.ColorEdit4("Traversable node color", ref _traversalColor, bFlags);
            ImGui.ColorEdit4("Untraversable node color", ref _untraversalColor, bFlags);

            ImGui.SeparatorText("Area text settings");
            ImGui.Checkbox("Enable corrupted maps counter (-1 of high towers amount)", ref state.ToggleCorruptedMaps);
            ImGui.Checkbox("Show Corrupted Nexus", ref state.ToggleCorruptedNexus);
            ImGui.Checkbox("Show all map content (ignore tower treshold)", ref state.ShowAllMapContent);
            ImGui.Checkbox("Recolor high towers amount", ref state.RecolorHighTowersAmount);
            ImGui.Checkbox("Draw towers range", ref state.DrawTowerRange);
            ImGui.Checkbox("Show towers amount after name [1/5]", ref state.showTowersAtName);
            ImGui.SliderInt("High towers amount threshold", ref state.HighTowerAmountThreshold, 2, 10);
            HelpMarker("Number of towers around node for highlight. Default: 5");
            ImGui.SliderInt("Traversal transparency", ref state.traversalTransparency, 0, 255);
            HelpMarker("Default: 90");
            ImGui.SliderFloat("Area text padding X", ref state.paddingName.X, 0, 10);
            HelpMarker("Default: 6");
            ImGui.SliderFloat("Area text padding Y", ref state.paddingName.Y, 0, 10);
            HelpMarker("Default: 2.6");
            ImGui.SliderInt("Area background rounding", ref state.nodeTextRounding, 0, 10);
            HelpMarker("Default: 0");

            ImGui.SeparatorText("Connections settings");
            ImGui.Checkbox("Draw Connections", ref state.drawConnections);
            ImGui.SliderInt("Connections thickness", ref state.ConnectionsThickness, 1, 5);
            ImGui.ColorEdit4("Connections Color", ref connectionsColor, bFlags);

            state.HighTowerAmountColorBg = ColorConverter.FromVector4(highTowerAmountColorBg);
            state.HighTowerAmountColorTxt = ColorConverter.FromVector4(highTowerAmountColorTxt);
            state.BackgroundAreaColor = ColorConverter.FromVector4(bgAreaColor);
            state.AreaTextColor = ColorConverter.FromVector4(textAreaColor);
            state.ConnectionsColor = ColorConverter.FromVector4(connectionsColor);
            state.traversalColor = ColorConverter.FromVector4(_traversalColor);
            state.untraversalColor = ColorConverter.FromVector4(_untraversalColor);
        }

        private void DrawKeybindMenu()
        {
            DrawKeybindRow("Create waypoint", ref state.KeybindCreateWaypoint);
            DrawKeybindRow("Show waypoint panel", ref state.KeybindShowWaypointPanel);
            DrawKeybindRow("Refresh nodes", ref state.KeybindRefreshNodes);
            DrawKeybindRow("Save settings", ref state.KeybindSaveSettings);
        }

        private void DrawMapSettings()
        {
            var tFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.PreciseWidths;
            if (ImGui.BeginTable("MapSettings", 3, tFlags))
            {
                var cFlags = ImGuiTableColumnFlags.NoHide;
                ImGui.TableSetupColumn("Name", cFlags);
                ImGui.TableSetupColumn("Node color", cFlags | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Name color", cFlags | ImGuiTableColumnFlags.NoSort);
                ImGui.TableHeadersRow();

                foreach (var map in state.NodeColors)
                {
                    ImGui.PushID(map.Key);
                    var nodeColor = ColorConverter.ToVector4(map.Value);
                    var nameColor = ColorConverter.ToVector4(state.NameColors[map.Key]);
                    var bFlags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel;

                    ImGui.TableNextColumn();
                    ImGui.Text($"{map.Key}");
                    ImGui.TableNextColumn();
                    ImGui.ColorEdit4("Node color", ref nodeColor, bFlags);
                    ImGui.TableNextColumn();
                    ImGui.ColorEdit4("Name color", ref nameColor, bFlags);
                    ImGui.TableNextRow();

                    state.NodeColors[map.Key] = ColorConverter.FromVector4(nodeColor);
                    state.NameColors[map.Key] = ColorConverter.FromVector4(nameColor);
                    ImGui.PopID();
                }
            }
            ImGui.EndTable();
        }

        private void DrawContentSettings()
        {
            ImGui.SliderInt("Circle thickness", ref state.ContentCircleThickness, 1, 10);
            ImGui.SliderInt("Gap between node / content", ref state.GapRadius, 1, 10);

            ImGui.Checkbox("Gemling map toggle", ref state.ToggleGemling);
            var _gemlingColor = ColorConverter.ToVector4(state.GemlingColor);
            ImGui.ColorEdit4("Gemling map color", ref _gemlingColor, ImGuiColorEditFlags.NoInputs);
            state.GemlingColor = ColorConverter.FromVector4(_gemlingColor);

            var tFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.PreciseWidths;
            if (ImGui.BeginTable("ContentSettings", 3, tFlags))
            {
                var cFlags = ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.DefaultSort;
                ImGui.TableSetupColumn("Enabled", cFlags);
                ImGui.TableSetupColumn("Name", cFlags);
                ImGui.TableSetupColumn("Content color", cFlags);
                ImGui.TableHeadersRow();

                foreach (var content in state.ContentCircleColor)
                {
                    ImGui.PushID(content.Key);
                    var contentColor = ColorConverter.ToVector4(content.Value);
                    var toggle = state.ContentToggle[content.Key];
                    var bFlags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel;

                    ImGui.TableNextColumn();
                    ImGui.Checkbox("", ref toggle);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{content.Key}");
                    ImGui.TableNextColumn();
                    ImGui.ColorEdit4("Circle color", ref contentColor, bFlags);
                    ImGui.TableNextRow();

                    state.ContentCircleColor[content.Key] = ColorConverter.FromVector4(contentColor);
                    state.ContentToggle[content.Key] = toggle;
                    ImGui.PopID();
                }
            }
            ImGui.EndTable();
        }

        private void DrawWaypointMenu()
        {
            ImGui.SliderFloat("Waypoint menu position x", ref state.WaypointWindowPos.X, 0, 1920, "x = %.0f");
            ImGui.SliderFloat("Waypoint menu position y", ref state.WaypointWindowPos.Y, 0, 1080, "y = %.0f");
            ImGui.Checkbox("Show waypoint menu", ref state.ShowWaypointMenu);
        }

        private void DrawWaypointPanel()
        {
            var mFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
            var window = GameController.Window.GetWindowRectangleTimeCache.TopRight;

            if (ImGui.Begin("Waypoint menu", ref state.ShowWaypointMenu, mFlags))
            {
                ImGui.SetWindowPos(state.WaypointWindowPos);
                var tFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg;
                if (ImGui.BeginTable("Waypoints", 6, tFlags))
                {
                    var cFlags = ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed;
                    ImGui.TableSetupColumn("   imbo   ", cFlags);

                    ImGui.TableSetupColumn(" Towers ", cFlags);

                    ImGui.TableSetupColumn("       Name       ", cFlags);

                    ImGui.TableSetupColumn("Dist", cFlags);

                    ImGui.TableSetupColumn("", cFlags);

                    ImGui.TableSetupColumn(" ", cFlags);

                    ImGui.TableHeadersRow();

                    foreach (var waypoint in state.WaypointList)
                    {
                        ImGui.PushID(waypoint.PositionX.ToString());

                        ImGui.TableNextColumn();
                        CenterText("Move to");
                        if (ImGui.Button("Move to"))
                        {
                        }

                        ImGui.TableNextColumn();
                        CenterText(waypoint.TowersCount.ToString());
                        ImGui.Text(waypoint.TowersCount.ToString());

                        ImGui.TableNextColumn();
                        CenterText(waypoint.Name);
                        ImGui.Text(waypoint.Name);

                        ImGui.TableNextColumn();
                        DrawWorldDirectionIndicator(
                            new Vector2(waypoint.PositionX, -waypoint.PositionY),
                            new Vector2(atlasPanel.Camera.Snapshot.Matrix.Translation.X, -atlasPanel.Camera.Snapshot.Matrix.Translation.Y)
                            );
                        ImGui.TableNextColumn();

                        CenterText("[X]");
                        if (ImGui.Button("[X]"))
                        {
                            DeleteWaypoint(new Vector2(waypoint.PositionX, waypoint.PositionY));
                        }

                        ImGui.TableNextRow();

                        ImGui.PopID();
                    }
                }
                ImGui.EndTable();
            }

            ImGui.End();
        }

        private void DrawDebugMenu()
        {
            ImGui.Checkbox("Draw Debug", ref state.drawDebug);
            if (state.drawDebug)
            {
                ImGui.Checkbox("Draw perfomance", ref state.DebugDrawPerfomance);
                ImGui.Checkbox("Draw coordinates", ref state.DebugDrawCoordinates);
                ImGui.Checkbox("Draw position", ref state.DebugDrawNodePosition);
                ImGui.Checkbox("Draw center position", ref state.DebugDrawNodeCenterPosition);
                ImGui.Checkbox("Draw attempted", ref state.DebugDrawAttempted);
                ImGui.Checkbox("Draw content amount", ref state.DebugDrawContentAmount);
                ImGui.Checkbox("Draw content names", ref state.DebugDrawContentNames);
            }
        }

        public void DrawWorldDirectionIndicator(Vector2 worldCoord1, Vector2 worldCoord2)
        {
            // Calculate direction vector
            Vector2 direction = worldCoord2 - worldCoord1;
            float distance = direction.Length();
            direction = Vector2.Normalize(direction);
            CenterText($"{distance / 1000:N0}");
            ImGui.Text($"{distance / 1000:N0}");
            ImGui.TableNextColumn();
            ImGui.TableSetColumnIndex(4);
            // Draw small arrow widget
            Vector2 arrowSize = new Vector2(45, 45); // Size of our indicator
            Vector2 center = ImGui.GetCursorScreenPos() + arrowSize / 2;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            // Main direction line
            Vector2 arrowEnd = center + direction * (arrowSize.X / 2 - 5);
            drawList.AddLine(center, arrowEnd, ImGui.GetColorU32(ImGuiCol.Text), 2f);

            // Arrowhead
            Vector2 perp = new Vector2(-direction.Y, direction.X);
            drawList.AddTriangleFilled(
                arrowEnd,
                arrowEnd - direction * 10 + perp * 5,
                arrowEnd - direction * 10 - perp * 5,
                ImGui.GetColorU32(ImGuiCol.Text)
            );
            ImGui.Dummy(new Vector2(30, 40));
        }

        private void CenterText(string text)
        {
            //ImGui.TableSetColumnIndex(ImGui.GetColumnIndex() + 1);
            var textSize = ImGui.CalcTextSize(text);
            var cellSize = new Vector2(ImGui.GetColumnWidth(), 35);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (cellSize.X - textSize.X) * 0.5f);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (cellSize.Y - textSize.Y) * 0.5f);
        }

        private static void HelpMarker(string desc)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.BeginItemTooltip())
            {
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private static string keyToChange = "";
        private static bool waitingForKey = false;
        private void DrawKeybindRow(string label, ref Keys key)
        {
            ImGui.Text($"{label}");
            ImGui.SameLine();
            ImGui.PushID(label);
            if (ImGui.Button($"{key}"))
            {
                waitingForKey = true;
                keyToChange = label;
            }
            if (waitingForKey && keyToChange == label)
            {
                ImGui.SameLine();
                ImGui.Text("Press any key...");
                CaptureKeyPress(ref key);
            }
            ImGui.PopID();
        }
        private void CaptureKeyPress(ref Keys key)
        {
            for (int i = 0; i < 256; i++)
            {
                if ((GetAsyncKeyState(i) & 0x8000) != 0)
                {
                    key = (Keys)i;
                    waitingForKey = false;
                    break;
                }
            }
        }
    }
}