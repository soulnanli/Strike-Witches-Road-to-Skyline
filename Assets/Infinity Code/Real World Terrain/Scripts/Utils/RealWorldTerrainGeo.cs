/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain
{
    /// <summary>
    /// Provides utility methods for geographic and Mercator coordinate conversions.
    /// </summary>
    public static class RealWorldTerrainGeo
    {
        /// <summary>
        /// The radius of the Earth.
        /// </summary>
        public const float EARTH_RADIUS = 6371;

        /// <summary>
        /// The length of the equator.
        /// </summary>
        public const int EQUATOR_LENGTH = 40075;
        
        /// <summary>
        /// The maximum elevation in the world.
        /// </summary>
        public const int MAX_ELEVATION = 15000;
        
        /// <summary>
        /// Converts geographic coordinates to SRTM data index.
        /// </summary>
        /// <param name="pos">Geographic coordinates</param>
        /// <returns>SRTM data index</returns>
        public static Vector2 LanLongToFlat(Vector2 pos)
        {
            return new Vector2(Mathf.FloorToInt(pos.x / 5.0f) * 5 + 180, 90 - Mathf.FloorToInt(pos.y / 5.0f) * 5);
        }

        /// <summary>
        /// Converts geographic coordinates to Mercator coordinates.
        /// </summary>
        /// <param name="x">Longitude</param>
        /// <param name="y">Latitude</param>
        public static void LatLongToMercat(ref double x, ref double y)
        {
            double sy = Math.Sin(y * RealWorldTerrainMath.DEG2RAD);
            x = (x + 180) / 360;
            y = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);
        }

        /// <summary>
        /// Converts geographic coordinates to Mercator coordinates.
        /// </summary>
        /// <param name="x">Longitude</param>
        /// <param name="y">Latitude</param>
        /// <param name="mx">Output Mercator X</param>
        /// <param name="my">Output Mercator Y</param>
        public static void LatLongToMercat(double x, double y, out double mx, out double my)
        {
            double sy = Math.Sin(y * RealWorldTerrainMath.DEG2RAD);
            mx = (x + 180) / 360;
            my = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);
        }

        /// <summary>
        /// Converts geographic coordinates to the index of the tile.
        /// What is the tiles, and how it works, you can read here:
        /// https://developers.google.com/maps/documentation/javascript/v2/overlays?csw=1#Google_Maps_Coordinates
        /// </summary>
        /// <param name="dx">Longitude</param>
        /// <param name="dy">Latitude</param>
        /// <param name="zoom">Zoom</param>
        /// <param name="tx">Output tile X</param>
        /// <param name="ty">Output tile Y</param>
        public static void LatLongToTile(double dx, double dy, int zoom, out double tx, out double ty)
        {
            LatLongToMercat(ref dx, ref dy);
            uint mapSize = (uint)RealWorldTerrainUtils.TILE_SIZE << zoom;
            double px = RealWorldTerrainMath.Clamp(dx * mapSize + 0.5, 0, mapSize - 1);
            double py = RealWorldTerrainMath.Clamp(dy * mapSize + 0.5, 0, mapSize - 1);
            tx = px / RealWorldTerrainUtils.TILE_SIZE;
            ty = py / RealWorldTerrainUtils.TILE_SIZE;
        }

        /// <summary>
        /// Converts Mercator coordinates to geographic coordinates.
        /// </summary>
        /// <param name="mx">Mercator X</param>
        /// <param name="my">Mercator Y</param>
        /// <param name="x">Output longitude</param>
        /// <param name="y">Output latitude</param>
        public static void MercatToLatLong(double mx, double my, out double x, out double y)
        {
            uint mapSize = (uint)RealWorldTerrainUtils.TILE_SIZE << 20;
            double px = RealWorldTerrainMath.Clamp(mx * mapSize + 0.5, 0, mapSize - 1);
            double py = RealWorldTerrainMath.Clamp(my * mapSize + 0.5, 0, mapSize - 1);
            mx = px / RealWorldTerrainUtils.TILE_SIZE;
            my = py / RealWorldTerrainUtils.TILE_SIZE;
            TileToLatLong(mx, my, 20, out x, out y);
        }
        
        /// <summary>
        /// Converts tile coordinates to geographic coordinates.
        /// </summary>
        /// <param name="tx">Tile X</param>
        /// <param name="ty">Tile Y</param>
        /// <param name="zoom">Zoom level</param>
        /// <param name="lx">Output longitude</param>
        /// <param name="ly">Output latitude</param>
        public static void TileToLatLong(double tx, double ty, int zoom, out double lx, out double ly)
        {
            double mapSize = RealWorldTerrainUtils.TILE_SIZE << zoom;
            lx = 360 * (RealWorldTerrainMath.Repeat(tx * RealWorldTerrainUtils.TILE_SIZE, 0, mapSize - 1) / mapSize - 0.5);
            ly = 90 - 360 * Math.Atan(Math.Exp(-(0.5 - RealWorldTerrainMath.Clamp(ty * RealWorldTerrainUtils.TILE_SIZE, 0, mapSize - 1) / mapSize) * 2 * Math.PI)) / Math.PI;
        }

        /// <summary>
        /// Converts tile index to quadkey.
        /// What is the tiles and quadkey, and how it works, you can read here:
        /// http://msdn.microsoft.com/en-us/library/bb259689.aspx
        /// </summary>
        /// <param name="x">Tile X</param>
        /// <param name="y">Tile Y</param>
        /// <param name="zoom">Zoom</param>
        /// <returns>Quadkey</returns>
        public static string TileToQuadKey(int x, int y, int zoom)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = zoom; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((x & mask) != 0) digit++;
                if ((y & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }
    }
}