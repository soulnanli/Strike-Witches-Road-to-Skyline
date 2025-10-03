/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.RealWorldTerrain
{
    /// <summary>
    /// This class contains utility methods.
    /// </summary>
    public static class RealWorldTerrainUtils
    {
        /// <summary>
        /// The average size of the texture of the tile.
        /// </summary>
        public const int AVERAGE_TEXTURE_SIZE = 20000;

        /// <summary>
        /// Maximum the size of the download for Google Maps.
        /// </summary>
        public const int DOWNLOAD_TEXTURE_LIMIT = 90000000;

        /// <summary>
        /// Size of tile.
        /// </summary>
        public const short TILE_SIZE = 256;

        /// <summary>
        /// Gets Hex value of Color.
        /// </summary>
        /// <param name="color">Color</param>
        /// <returns>Hex value</returns>
        public static string ColorToHex(Color32 color)
        {
            return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        }

        /// <summary>
        /// Creates a new GameObject with the specified name, sets it as a child of the parent GameObject, and positions it at the origin.
        /// </summary>
        /// <param name="parent">The MonoBehaviour whose GameObject should be the parent of the new GameObject.</param>
        /// <param name="name">The name of the new GameObject.</param>
        /// <returns>The new GameObject.</returns>
        public static GameObject CreateGameObject(MonoBehaviour parent, string name)
        {
            return CreateGameObject(parent.gameObject, name, Vector3.zero);
        }

        /// <summary>
        /// Creates a new GameObject with the specified name, sets it as a child of the parent GameObject.
        /// </summary>
        /// <param name="parent">The GameObject that should be the parent of the new GameObject.</param>
        /// <param name="name">The name of the new GameObject.</param>
        /// <returns>The new GameObject.</returns>
        public static GameObject CreateGameObject(GameObject parent, string name)
        {
            return CreateGameObject(parent, name, Vector3.zero);
        }

        /// <summary>
        /// Creates a new GameObject with the specified name, sets it as a child of the parent GameObject, and positions it at the specified position.
        /// </summary>
        /// <param name="parent">The GameObject that should be the parent of the new GameObject.</param>
        /// <param name="name">The name of the new GameObject.</param>
        /// <param name="position">The local position of the new GameObject.</param>
        /// <returns>The new GameObject.</returns>
        public static GameObject CreateGameObject(GameObject parent, string name, Vector3 position)
        {
            GameObject container = new GameObject(name);
            container.transform.parent = parent.transform;
            container.transform.localPosition = position;
            return container;
        }

        /// <summary>
        /// Deletes a GameObject from the scene hierarchy.
        /// </summary>
        /// <param name="current">The parent transform from which to delete the GameObject.</param>
        /// <param name="name">The name of the GameObject to delete.</param>
        public static void DeleteGameObject(Transform current, string name)
        {
            for (int i = current.childCount - 1; i >= 0; i--)
            {
                Transform child = current.GetChild(i);
                if (child.name == name) Object.DestroyImmediate(child.gameObject);
                else DeleteGameObject(child, name);
            }
        }

        /// <summary>
        /// Exports a collection of MeshFilters to a file in the .obj format.
        /// </summary>
        /// <param name="filename">The name of the file to which the meshes will be exported.</param>
        /// <param name="mfs">An array of MeshFilters whose meshes will be exported.</param>
        /// <remarks>
        /// This method exports the meshes of the provided MeshFilters to a file in the .obj format.
        /// Each mesh is exported as a separate group, and the name of the group is the name of the GameObject to which the MeshFilter is attached.
        /// The method also exports the normals and UVs of the meshes.
        /// </remarks>
        public static void ExportMesh(string filename, params MeshFilter[] mfs)
        {
            StringBuilder builder = new StringBuilder();
            int nextNormalIndex = 0;
            foreach (MeshFilter mf in mfs)
            {
                Mesh m = mf.sharedMesh;
                Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

                builder.Append("g ").Append(mf.name).Append("\n");
                for (int i = 0; i < m.vertices.Length; i++)
                {
                    Vector3 v = m.vertices[i];
                    builder.Append("v ").Append(v.x).Append(" ").Append(v.y).Append(" ").Append(v.z).Append("\n");
                }
                builder.Append("\n");

                for (int i = 0; i < m.normals.Length; i++)
                {
                    Vector3 v = m.normals[i];
                    builder.Append("vn ").Append(v.x).Append(" ").Append(v.y).Append(" ").Append(v.z).Append("\n");
                }
                builder.Append("\n");

                for (int i = 0; i < m.uv.Length; i++)
                {
                    Vector2 v = m.uv[i];
                    builder.Append("vt ").Append(v.x).Append(" ").Append(v.y).Append("\n");
                }

                for (int material = 0; material < m.subMeshCount; material++)
                {
                    builder.Append("\nusemtl ").Append(mats[material].name).Append("\n");
                    builder.Append("usemap ").Append(mats[material].name).Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int tni1 = triangles[i] + 1 + nextNormalIndex;
                        int tni2 = triangles[i + 1] + 1 + nextNormalIndex;
                        int tni3 = triangles[i + 2] + 1 + nextNormalIndex;
                        builder.Append("f ").Append(tni1).Append("/").Append(tni1).Append("/").Append(tni1);
                        builder.Append(" ").Append(tni2).Append("/").Append(tni2).Append("/").Append(tni2);
                        builder.Append(" ").Append(tni3).Append("/").Append(tni3).Append("/").Append(tni3).Append("\n");
                    }
                }

                builder.Append("\n");
                nextNormalIndex += m.normals.Length;
            }

#if !NETFX_CORE
            StreamWriter stream = new StreamWriter(filename);
            stream.Write(builder.ToString());
            stream.Close();
#endif
        }

        /// <summary>
        /// Calculates the bounding rectangle from a list of Vector3 points.
        /// </summary>
        /// <param name="points">A list of Vector3 points.</param>
        /// <returns>A Rect structure that contains the smallest axis-aligned rectangle that can be drawn to encompass all the points in the list.</returns>
        public static Rect GetRectFromPoints(List<Vector3> points)
        {
            return new Rect
            {
                x = points.Min(p => p.x),
                y = points.Min(p => p.z),
                xMax = points.Max(p => p.x),
                yMax = points.Max(p => p.z)
            };
        }
        
        /// <summary>
        /// Finds an object of type T in the scene.
        /// </summary>
        /// <typeparam name="T">The type of the object to find. This type parameter should be a subclass of UnityEngine.Object.</typeparam>
        /// <returns>The object of type T found in the scene, or null if no such object exists.</returns>
        public static T FindObjectOfType<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// Finds all objects of type T in the scene.
        /// </summary>
        /// <typeparam name="T">The type of the objects to find. This type parameter should be a subclass of UnityEngine.Object.</typeparam>
        /// <returns>An array of objects of type T found in the scene. If no such objects exist, an empty array is returned.</returns>
        public static T[] FindObjectsOfType<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        /// <summary>
        /// Converts a hexadecimal color string to a Color object.
        /// </summary>
        /// <param name="hex">A string representing a color in hexadecimal format. The string should be 6 characters long and consist of pairs of hexadecimal digits representing the red, green, and blue components of the color, in that order.</param>
        /// <returns>A Color object that represents the color specified by the hexadecimal string. The alpha component of the color is always set to 255.</returns>
        public static Color HexToColor(string hex)
        {
            byte r = Byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = Byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = Byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        /// <summary>
        /// Replaces all occurrences of specified strings within the original string with a new string.
        /// </summary>
        /// <param name="str">The original string.</param>
        /// <param name="oldValues">An array of strings to be replaced.</param>
        /// <param name="newValue">The string to replace all occurrences of the old values.</param>
        /// <returns>A new string that is identical to the original string, except that all occurrences of strings in the oldValues array have been replaced by the newValue string.</returns>
        public static string ReplaceString(string str, string[] oldValues, string newValue)
        {
            foreach (string oldValue in oldValues) str = str.Replace(oldValue, newValue);
            return str;
        }

        /// <summary>
        /// Replaces all occurrences of specified strings within the original string with corresponding new strings.
        /// </summary>
        /// <param name="str">The original string.</param>
        /// <param name="oldValues">An array of strings to be replaced.</param>
        /// <param name="newValues">An array of new strings that correspond to the old values. Each string in oldValues is replaced by the string at the same index in newValues.</param>
        /// <returns>A new string that is identical to the original string, except that all occurrences of strings in the oldValues array have been replaced by the corresponding strings in the newValues array.</returns>
        public static string ReplaceString(string str, string[] oldValues, string[] newValues)
        {
            for (int i = 0; i < oldValues.Length; i++) str = str.Replace(oldValues[i], newValues[i]);
            return str;
        }

        /// <summary>
        /// Removes a specified number of elements from a list at a specified position and returns them.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list from which to remove elements.</param>
        /// <param name="offset">The zero-based index at which to start removing elements.</param>
        /// <param name="count">The number of elements to remove. The default value is 1.</param>
        /// <returns>A new list that contains the removed elements.</returns>
        public static List<T> SpliceList<T>(List<T> list, int offset, int count = 1)
        {
            List<T> newList = list.Skip(offset).Take(count).ToList();
            list.RemoveRange(offset, count);
            return newList;
        }

        /// <summary>
        /// Converts a string representation of a color to a Color object.
        /// </summary>
        /// <param name="str">A string representing a color. This can be a named color (e.g. "red", "blue", "green", etc.) or a hexadecimal color string (e.g. "FF0000" for red).</param>
        /// <returns>A Color object that represents the color specified by the string. If the string does not represent a valid color, Color.white is returned.</returns>
        public static Color StringToColor(string str)
        {
            str = str.ToLower();
            if (str == "black") return Color.black;
            if (str == "blue") return Color.blue;
            if (str == "cyan") return Color.cyan;
            if (str == "gray") return Color.gray;
            if (str == "green") return Color.green;
            if (str == "magenta") return Color.magenta;
            if (str == "red") return Color.red;
            if (str == "white") return Color.white;
            if (str == "yellow") return Color.yellow;

            try
            {
                string hex = (str + "000000").Substring(1, 6);
                byte[] cb =
                    Enumerable.Range(0, hex.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                        .ToArray();
                return new Color32(cb[0], cb[1], cb[2], 255);
            }
            catch
            {
                return Color.white;
            }
        }

        #region Obsolete

        [Obsolete("Use RealWorldTerrainGeo.LatLongToMercat instead.")]
        public static void LatLongToMercat(ref double x, ref double y)
        {
            RealWorldTerrainGeo.LatLongToMercat(ref x, ref y);
        }

        [Obsolete("Use RealWorldTerrainGeo.LatLongToMercat instead.")]
        public static void LatLongToMercat(double x, double y, out double mx, out double my)
        {
            RealWorldTerrainGeo.LatLongToMercat(x, y, out mx, out my);
        }

        [Obsolete("Use RealWorldTerrainGeo.LatLongToTile instead.")]
        public static void LatLongToTile(double dx, double dy, int zoom, out double tx, out double ty)
        {
            RealWorldTerrainGeo.LatLongToTile(dx, dy, zoom, out tx, out ty);
        }

        [Obsolete("Use RealWorldTerrainGeo.MercatToLatLong instead.")]
        public static void MercatToLatLong(double mx, double my, out double x, out double y)
        {
            RealWorldTerrainGeo.MercatToLatLong(mx, my, out x, out y);
        }

        [Obsolete("Use RealWorldTerrainGeo.TileToLatLong instead.")]
        public static void TileToLatLong(double tx, double ty, int zoom, out double lx, out double ly)
        {
            RealWorldTerrainGeo.TileToLatLong(tx, ty, zoom, out lx, out ly);
        }

        [Obsolete("Use RealWorldTerrainGeo.TileToQuadKey instead.")]
        public static string TileToQuadKey(int x, int y, int zoom)
        {
            return RealWorldTerrainGeo.TileToQuadKey(x, y, zoom);
        }

        #endregion
    }
}