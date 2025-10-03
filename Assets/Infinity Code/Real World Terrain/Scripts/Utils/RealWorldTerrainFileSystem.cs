/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace InfinityCode.RealWorldTerrain
{
    /// <summary>
    /// Provides utility methods for file system operations.
    /// </summary>
    public static class RealWorldTerrainFileSystem
    {
        /// <summary>
        /// The number of bytes in megabyte.
        /// </summary>
        public const int MB = 1048576;
        
        /// <summary>
        /// Calculates the total size of a directory in bytes.
        /// </summary>
        /// <param name="folder">The directory for which to calculate the size.</param>
        /// <returns>The total size of the directory in bytes.</returns>
        public static long GetDirectorySize(DirectoryInfo folder)
        {
            return folder.GetFiles().Sum(fi => fi.Length) + folder.GetDirectories().Sum(dir => GetDirectorySize(dir));
        }

        /// <summary>
        /// Calculates the total size of a directory in bytes.
        /// </summary>
        /// <param name="folderPath">The path of the directory for which to calculate the size.</param>
        /// <returns>The total size of the directory in bytes.</returns>
        public static long GetDirectorySize(string folderPath)
        {
            return GetDirectorySize(new DirectoryInfo(folderPath));
        }

        /// <summary>
        /// Calculates the total size of a directory in megabytes.
        /// </summary>
        /// <param name="folderPath">The path of the directory for which to calculate the size.</param>
        /// <returns>The total size of the directory in megabytes.</returns>
        public static long GetDirectorySizeMB(string folderPath)
        {
            return GetDirectorySize(folderPath) / MB;
        }
        
        /// <summary>
        /// Safely deletes a directory.
        /// </summary>
        /// <param name="directoryName">The name of the directory to delete.</param>
        public static void SafeDeleteDirectory(string directoryName)
        {
            try
            {
                Directory.Delete(directoryName, true);
            }
            catch
            { }
        }

        /// <summary>
        /// Safely deletes a file.
        /// </summary>
        /// <param name="filename">The path of the file to delete.</param>
        /// <param name="tryCount">The number of attempts to delete the file. Default is 10.</param>
        public static void SafeDeleteFile(string filename, int tryCount = 10)
        {
            while (tryCount-- > 0)
            {
                try
                {
                    File.Delete(filename);
                    break;
                }
                catch (Exception)
                {
#if !NETFX_CORE
                    Thread.Sleep(10);
#endif
                }
            }
        }
    }
}