using DnDBattle.Models.Tiles;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    public class BattleGridTileMapManager
    {
        #region Fields

        private TileMap _loadedTileMap;

        #endregion

        #region Properties

        public TileMap LoadedTileMap => _loadedTileMap;

        #endregion

        #region Constructor

        public BattleGridTileMapManager()
        {
            Debug.WriteLine("[TileMapManager] Initialized");
        }

        #endregion

        #region Public Methods

        public async Task LoadTileMapAsync(TileMap tileMap, double cellSize)
        {
            await Task.Run(() =>
            {
                _loadedTileMap = tileMap;
                Debug.WriteLine($"[TileMapManager] Tile map loaded: {tileMap.Name}");
            });
        }

        public void ClearTileMap()
        {
            _loadedTileMap = null;
            Debug.WriteLine("[TileMapManager] Tile map cleared");
        }

        #endregion
    }
}