using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaCLI
{
    public static class DirectoryUtilities
    {
        public static void TryCopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists) return;

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string targetSubDir = Path.Combine(destinationDir, subdir.Name);
                    TryCopyDirectory(subdir.FullName, targetSubDir, true);
                }
            }
        }
    }
}
