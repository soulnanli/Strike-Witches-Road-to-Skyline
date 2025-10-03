/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.IO;
using InfinityCode.RealWorldTerrain.Generators;
using InfinityCode.RealWorldTerrain.Windows;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain
{
    public static class RealWorldTerrainWaterMask
    {
        private static bool hasMercatorBounds;
        private static double mx1, my1, mx2, my2;
        private static byte[] waterMask;
        private static int waterMaskWidth;
        private static int waterMaskHeight;

        private static RealWorldTerrainPrefs prefs
        {
            get { return RealWorldTerrainWindow.prefs; }
        }

        public static void Dispose()
        {
            hasMercatorBounds = false;
            waterMask = null;
        }

        public static bool Get(double x, double y, out double value)
        {
            try
            {
                if (!hasMercatorBounds)
                {
                    RealWorldTerrainGeo.LatLongToMercat(prefs.leftLongitude, prefs.topLatitude, out mx1, out my1);
                    RealWorldTerrainGeo.LatLongToMercat(prefs.rightLongitude, prefs.bottomLatitude, out mx2, out my2);
                    hasMercatorBounds = true;
                }

                if (IsWater(x, y))
                {
                    if (RealWorldTerrainElevationGenerator.OnElevationRetrieved != null)
                    {
                        double? nv = RealWorldTerrainElevationGenerator.OnElevationRetrieved(x, y, double.MinValue);
                        if (nv.HasValue)
                        {
                            value = nv.Value;
                            return true;
                        }
                    }

                    value = double.MinValue;
                    return true;
                }
            }
            catch
            {
                    
            }
            
            value = 0;
            return false;
        }
        
        private static bool IsWater(double x, double y)
        {
            double rx = (x - mx1) / (mx2 - mx1);
            if (prefs.waterDetectionSource == RealWorldTerrainWaterDetectionSource.texture)
            {
                double ry = (my2 - y) / (my2 - my1);
                Color c = prefs.waterDetectionTexture.GetPixelBilinear((float)rx, (float)ry);
                if (c.grayscale > 0.5f) return true;
            }
            else if (prefs.waterDetectionSource == RealWorldTerrainWaterDetectionSource.bitMask)
            {
                double ry = (y - my1) / (my2 - my1);
                if (waterMask == null) LoadWaterMask();
                
                int ix = (int)Math.Round(waterMaskWidth * rx);
                int iy = (int)Math.Round(waterMaskHeight * ry);
                
                if (ix < 0) ix = 0;
                else if (ix >= waterMaskWidth) ix = waterMaskWidth - 1;
                
                if (iy < 0) iy = 0;
                else if (iy >= waterMaskHeight) iy = waterMaskHeight - 1;
                
                int index = iy * waterMaskWidth + ix;
                int bitIndex = index % 8;
                byte b = waterMask[index / 8];
                
                return (b & (1 << bitIndex)) != 0;
            }
            
            return false;
        }

        public static bool IsUsed(bool allowWaterTexture = true)
        {
            if (!allowWaterTexture || !prefs.generateUnderWater) return false;

            if (prefs.waterDetectionSource == RealWorldTerrainWaterDetectionSource.texture)
            {
                return prefs.waterDetectionTexture != null;
            }

            if (prefs.waterDetectionSource == RealWorldTerrainWaterDetectionSource.bitMask)
            {
                return prefs.waterDetectionBitMask != null;
            }

            return true;
        }

        private static void LoadWaterMask()
        {
            byte[] bytes = prefs.waterDetectionBitMask.bytes;
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);
            waterMaskWidth = reader.ReadInt32();
            waterMaskHeight = reader.ReadInt32();
            waterMask = reader.ReadBytes(bytes.Length - 8);
        }
    }
}