/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Windows
{
    public class RealWorldTerrainWaterMaskConverter : EditorWindow
    {
        private string inputFile;
        private int width;
        private int height;
        private Depth depth = Depth.eight;
        private RealWorldTerrainByteOrder byteOrder = RealWorldTerrainByteOrder.Windows;
        private int threshold = 128;

        private void Convert()
        {
            if (!Validate()) return;

            string outputFile = EditorUtility.SaveFilePanel("Save Water Mask", "", "WaterMask.bytes", "bytes");
            if (string.IsNullOrEmpty(outputFile)) return;

            FileStream stream = File.OpenRead(inputFile);
            BinaryReader reader = new BinaryReader(stream);
            int countBytes = depth == Depth.eight ? 1 : 2;
            int bufferSize = 8192 * countBytes;
            byte[] buffer = new byte[bufferSize];
            byte[] output = new byte[Mathf.CeilToInt(width * height / 8f) + 8];
            
            MemoryStream ms = new MemoryStream(output);
            BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(width);
            writer.Write(height);
            writer.Close();
            
            long outputIndex = 64;
            int bufferIndex = 0;
            int bufferCount = 0;
            int value = 0;

            int delimiter = depth == Depth.eight ? 1 : 256;

            while ((bufferCount = reader.Read(buffer, bufferIndex, bufferSize)) > 0)
            {
                for (int i = 0; i < bufferCount; i++)
                {
                    if (depth == Depth.sixteen)
                    {
                        if (i % 2 == 0)
                        {
                            value = buffer[i];
                            continue;
                        }

                        if (byteOrder == RealWorldTerrainByteOrder.Windows)
                        {
                            value += buffer[i] * 256;
                        }
                        else
                        {
                            value = buffer[i] + value * 256;
                        }
                    }
                    else
                    {
                        value = buffer[i];
                    }

                    value /= delimiter;
                    
                    int bitIndex = (int)(outputIndex % 8);
                    int bit = value > threshold ? 1 : 0;
                    byte o = (byte)(output[outputIndex / 8] | (bit << bitIndex));
                    output[outputIndex / 8] = o;
                    outputIndex++;
                }
            }
            
            reader.Close();
            stream.Close();
            
            File.WriteAllBytes(outputFile, output);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", "Water mask successfully saved.", "OK");
        }

        private void OnGUI()
        {
            ProcessDragAndDrop();
            
            EditorGUILayout.HelpBox("Tool to convert RAW files to bitmask for water generation.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            inputFile = EditorGUILayout.TextField("Input File", inputFile);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                inputFile = EditorUtility.OpenFilePanel("Select Input File", inputFile, "raw");
            }

            EditorGUILayout.EndHorizontal();

            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            depth = (Depth)EditorGUILayout.EnumPopup("Depth", depth);
            if (depth == Depth.sixteen)
            {
                byteOrder = (RealWorldTerrainByteOrder)EditorGUILayout.EnumPopup("Byte Order", byteOrder);
            }
            threshold = EditorGUILayout.IntField("Threshold", threshold);

            if (GUILayout.Button("Convert"))
            {
                Convert();
            }
        }

        private void ProcessDragAndDrop()
        {
            Event e = Event.current;
            Rect rect = new Rect(0, 0, position.width, position.height);
            
            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(e.mousePosition)) return;
                    if (DragAndDrop.paths.Length != 1) return;
                    if (!string.IsNullOrEmpty(inputFile)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (e.type != EventType.DragPerform) return;
                    
                    DragAndDrop.AcceptDrag();
                    
                    inputFile = DragAndDrop.paths[0];

                    break;
            }
        }

        public static void OpenWindow()
        {
            GetWindow<RealWorldTerrainWaterMaskConverter>(true, "Raw to Water Mask Converter", true);
        }

        private bool Validate()
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                EditorUtility.DisplayDialog("Error", "Input file is empty.", "OK");
                return false;
            }

            if (!File.Exists(inputFile))
            {
                EditorUtility.DisplayDialog("Error", "Input file does not exist.", "OK");
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Width and height must be greater than zero.", "OK");
                return false;
            }
            
            FileInfo info = new FileInfo(inputFile);
            int countBytes = depth == Depth.eight ? 1 : 2;
            if (info.Length != width * height * countBytes)
            {
                EditorUtility.DisplayDialog("Error", "File size does not match the specified width, height and depth.", "OK");
                return false;
            }

            return true;
        }

        public enum Depth
        {
            eight,
            sixteen,
        }
    }
}