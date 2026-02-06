using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Encounters;
﻿using System.Collections.Generic;

namespace DnDBattle.Models.Encounters
{
    /// <summary>
    /// Data Transfer Object for saving/loading encounters
    /// </summary>
    public class EncounterDto
    {
        public string MapImagePath { get; set; } = string.Empty;
        public List<TokenDto> Tokens { get; set; } = new List<TokenDto>();
        public List<WallDto> Walls { get; set; } = new List<WallDto>();
        public List<LightDto> Lights { get; set; } = new List<LightDto>();
    }

    /// <summary>
    /// DTO for serializing wall data
    /// </summary>
    public class WallDto
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public string WallType { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for serializing light source data
    /// </summary>
    public class LightDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double RadiusSquares { get; set; }
        public double Intensity { get; set; }
        public double BrightRadius { get; set; } = 4;
        public double DimRadius { get; set; } = 8;
        public byte ColorR { get; set; } = 255;
        public byte ColorG { get; set; } = 255;
        public byte ColorB { get; set; } = 200;
        public bool IsEnabled { get; set; } = true;
        public string LightType { get; set; } = "Point";
        public double Direction { get; set; }
        public double ConeWidth { get; set; } = 60;
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for serializing point data (if still needed elsewhere)
    /// </summary>
    public class PointDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}