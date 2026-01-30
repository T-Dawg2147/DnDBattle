using DnDBattle.Models.Tiles;
using DnDBattle.Services.Mapping_Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Services
{
    public class TileMapRenderer
    {
        private readonly TileImageCacheService _imageCache;
        private readonly TilePaletteService _paletteService;

        public TileMapRenderer(TilePaletteService paletteService)
        {
            _imageCache = TileImageCacheService.Instance;
            _paletteService = paletteService;
        }

        public BitmapSource RenderToBitmap(TileMap map, double scale = 1.0)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            double cellSize = map.GridCellSize * scale;
            int pixelWidth = (int)(map.WidthInSquares * cellSize);
            int pixelHeight = (int)(map.HeightInSquares * cellSize);

            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                var bgColor = (Color)ColorConverter.ConvertFromString(map.BackgroundColor ?? "#1E1E1E");
                dc.DrawRectangle(new SolidColorBrush(bgColor), null,
                    new Rect(0, 0, pixelWidth, pixelHeight));

                var sortedTiles = map.Tiles
                    .OrderBy(t => t.Layer)
                    .ThenBy(t => t.GridY)
                    .ThenBy(t => t.GridX);

                foreach (var tile in sortedTiles)
                {
                    DrawTile(dc, tile, cellSize);
                }
            }

            var bitmap = new RenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                96 * scale,
                96 * scale,
                PixelFormats.Pbgra32);

            bitmap.Render(visual);
            bitmap.Freeze();

            return bitmap;
        }

        public void SaveToPng(TileMap map, string filePath, double scale = 1.0)
        {
            var bitmap = RenderToBitmap(map, scale);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = System.IO.File.Create(filePath))
            {
                encoder.Save(stream);
            }
        }

        private void DrawTile(DrawingContext dc, Tile tile, double cellSize)
        {
            var definition = _paletteService.GetTileDefinition(tile.TileDefinitionId);
            if (definition?.CachedImage == null) return;

            double x = tile.GridX * cellSize;
            double y = tile.GridY * cellSize;
            double width = cellSize * definition.WidthInSquares;
            double height = cellSize * definition.HeightInSquares;

            if (tile.Rotation != 0)
            {
                var centerX = x + width / 2;
                var centerY = y + height / 2;
                dc.PushTransform(new RotateTransform(tile.Rotation, centerX, centerY));
            }

            dc.DrawImage(definition.CachedImage, new Rect(x, y, width, height));

            if (tile.Rotation != 0)
                dc.Pop();
        }
    }
}
