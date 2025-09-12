using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.AtlasElements;
using GameOffsets2.Native;

namespace cOverlay
{
    public class Content
    {
        public Content()
        { }

        public Content(string type, float contentWidth = 1)
        {
            this.Type = type;
            this.ContentColor = Color.Black;
            this.ContentWidth = contentWidth;
        }

        public Content(string type, Color _contentColor, float _contentWidth = 1)
        {
            this.Type = type;
            this.ContentColor = _contentColor;
            this.ContentWidth = _contentWidth;
        }

        public string Type { get; set; } = "";
        public Color ContentColor { get; set; } = Color.White;
        public float ContentWidth { get; set; }

        public static HashSet<string> ContentType = new HashSet<string> {
            "Breach",
            "Ritual",
            "Expedition",
            "Irradiated",
            "Map Boss",
            "Delirium",
            "Corrupted Nexus"
        };
    }

    public class Node
    {
        public Node(Vector2i _coords, AtlasNodeDescription _node, List<AtlasNodeDescription> neighbourNodes, int towersCount, int affectedTowersCount)
        {
            this.NodeCoords = _coords;
            NodeObject = _node;
            this.neighbourNodes = neighbourNodes;
            TowersCount = towersCount;
            AffectedTowersCount = affectedTowersCount;
        }

        public Vector2i NodeCoords { get; set; }
        public AtlasNodeDescription NodeObject { get; set; }
        public List<AtlasNodeDescription> neighbourNodes { get; set; } = new List<AtlasNodeDescription>();
        public int TowersCount { get; set; }
        public int AffectedTowersCount { get; set; }
    }

    public class Waypoint
    {
        public Waypoint(float positionX, float positionY, string name, int towersCount, Vector2i coordinates)
        {
            PositionX = positionX;
            PositionY = positionY;
            Name = name;
            TowersCount = towersCount;
            Coordinates = coordinates;
        }

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string Name { get; set; }
        public int TowersCount { get; set; }
        public Vector2i Coordinates { get; set; }
    }
}