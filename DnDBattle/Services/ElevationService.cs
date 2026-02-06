using System;
using System.Collections.Generic;

namespace DnDBattle.Services
{
    /// <summary>
    /// Manages 2.5D elevation layers for the battle grid.
    /// Uses a flat array for O(1) terrain elevation lookups.
    /// Token elevations are tracked separately for flying/climbing.
    /// </summary>
    public sealed class ElevationService
    {
        // Terrain elevation stored as flat array for cache-friendly access
        private int[] _terrainElevation; // elevation in feet
        private int _width;
        private int _height;

        // Per-token elevation overrides (flying, levitating, etc.)
        private readonly Dictionary<string, int> _tokenElevations = new();

        /// <summary>Elevation increment in feet (D&amp;D standard: 5 or 10 ft).</summary>
        public int ElevationStepFeet { get; set; } = 10;

        /// <summary>Maximum elevation allowed in feet.</summary>
        public int MaxElevationFeet { get; set; } = 300;

        /// <summary>
        /// Initialize the elevation grid for a given map size.
        /// All cells start at ground level (0 ft).
        /// </summary>
        public void Initialize(int gridWidth, int gridHeight)
        {
            _width = gridWidth;
            _height = gridHeight;
            _terrainElevation = new int[gridWidth * gridHeight];
        }

        /// <summary>
        /// Gets terrain elevation at a grid position. O(1) flat array lookup.
        /// </summary>
        public int GetTerrainElevation(int gridX, int gridY)
        {
            if (!Options.EnableElevationSystem) return 0;
            if (!IsInBounds(gridX, gridY)) return 0;
            return _terrainElevation[gridY * _width + gridX];
        }

        /// <summary>
        /// Sets terrain elevation at a grid position.
        /// </summary>
        public void SetTerrainElevation(int gridX, int gridY, int elevationFeet)
        {
            if (!IsInBounds(gridX, gridY)) return;
            _terrainElevation[gridY * _width + gridX] = Math.Clamp(elevationFeet, 0, MaxElevationFeet);
        }

        /// <summary>
        /// Raises terrain elevation at a position by one step.
        /// </summary>
        public void RaiseTerrain(int gridX, int gridY)
        {
            int current = GetTerrainElevation(gridX, gridY);
            SetTerrainElevation(gridX, gridY, current + ElevationStepFeet);
        }

        /// <summary>
        /// Lowers terrain elevation at a position by one step.
        /// </summary>
        public void LowerTerrain(int gridX, int gridY)
        {
            int current = GetTerrainElevation(gridX, gridY);
            SetTerrainElevation(gridX, gridY, Math.Max(0, current - ElevationStepFeet));
        }

        /// <summary>
        /// Sets a token's personal elevation (for flying, climbing, etc.).
        /// This is added on top of the terrain elevation at their position.
        /// </summary>
        public void SetTokenElevation(string tokenId, int elevationFeet)
        {
            _tokenElevations[tokenId] = Math.Clamp(elevationFeet, 0, MaxElevationFeet);
        }

        /// <summary>
        /// Gets a token's personal elevation offset.
        /// </summary>
        public int GetTokenElevation(string tokenId)
        {
            return _tokenElevations.TryGetValue(tokenId, out int elev) ? elev : 0;
        }

        /// <summary>
        /// Gets total height of a token (terrain + personal elevation).
        /// </summary>
        public int GetTotalHeight(string tokenId, int gridX, int gridY)
        {
            return GetTerrainElevation(gridX, gridY) + GetTokenElevation(tokenId);
        }

        /// <summary>
        /// Calculates 3D distance between two points at different elevations.
        /// Uses Pythagorean theorem: sqrt(dx² + dy² + dz²), where dz is elevation difference.
        /// Result in grid squares (each square = FeetPerSquare feet).
        /// </summary>
        public double Calculate3DDistance(int x1, int y1, int elev1Feet, int x2, int y2, int elev2Feet, int feetPerSquare = 5)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            double dzSquares = (double)(elev1Feet - elev2Feet) / feetPerSquare;
            return Math.Sqrt(dx * dx + dy * dy + dzSquares * dzSquares);
        }

        /// <summary>
        /// Calculates falling damage in D&amp;D 5e rules: 1d6 per 10 feet, max 20d6.
        /// Returns the number of d6 to roll.
        /// </summary>
        public int CalculateFallingDamageDice(int heightFeet)
        {
            if (heightFeet <= 0) return 0;
            int dice = heightFeet / 10;
            return Math.Min(dice, 20); // D&D 5e caps at 20d6
        }

        /// <summary>
        /// Checks if a token at one elevation can see a target at another elevation,
        /// considering simple height-based line of sight.
        /// </summary>
        public bool HasElevationLineOfSight(int sourceElevFeet, int targetElevFeet, int horizontalDistSquares, int wallHeightFeet = 10)
        {
            if (!Options.EnableElevationSystem) return true;
            // Simple check: if source is high enough above intervening wall height, LoS is clear
            int elevDiff = Math.Abs(sourceElevFeet - targetElevFeet);
            // At short range, elevation doesn't block
            if (horizontalDistSquares <= 1) return true;
            // Higher token can see over walls proportionally
            int higherElev = Math.Max(sourceElevFeet, targetElevFeet);
            return higherElev >= wallHeightFeet || elevDiff <= wallHeightFeet;
        }

        /// <summary>
        /// Gets all distinct elevation levels present on the map (for layer rendering).
        /// </summary>
        public IReadOnlyList<int> GetDistinctElevationLevels()
        {
            if (_terrainElevation == null) return Array.Empty<int>();
            var levels = new SortedSet<int>();
            for (int i = 0; i < _terrainElevation.Length; i++)
                levels.Add(_terrainElevation[i]);
            return new List<int>(levels);
        }

        /// <summary>
        /// Removes a token from elevation tracking (e.g., token removed from map).
        /// </summary>
        public void RemoveToken(string tokenId)
        {
            _tokenElevations.Remove(tokenId);
        }

        /// <summary>
        /// Resets all elevation data.
        /// </summary>
        public void Reset()
        {
            if (_terrainElevation != null)
                Array.Clear(_terrainElevation, 0, _terrainElevation.Length);
            _tokenElevations.Clear();
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height && _terrainElevation != null;
        }
    }
}
