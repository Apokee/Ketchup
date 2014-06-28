using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ketchup.IO
{
    internal static class IoUtility
    {
        public static IEnumerable<FileInfo> GetFirmwareFiles()
        {
            return FilterImageFiles(GetFiles(GetSubDirectory("Firmware")));
        }

        public static IEnumerable<FileInfo> GetFloppyFiles()
        {
            return FilterImageFiles(GetFiles(GetSubDirectory("FloppyDisks")));
        }

        private static IEnumerable<FileInfo> FilterImageFiles(IEnumerable<FileInfo> files)
        {
            return files.Where(i => i.Name.ToLowerInvariant().EndsWith(".bin") || i.Name.ToLowerInvariant().EndsWith(".img"));
        }

        private static string GetBaseDirectory()
        {
            var savesDirectory = Path.Combine(KSPUtil.ApplicationRootPath, "saves");
            var profileDirectory = Path.Combine(savesDirectory, HighLogic.SaveFolder);
            var ketchupDirectory = Path.Combine(profileDirectory, "Ketchup");

            return ketchupDirectory;
        }

        private static string GetSubDirectory(string name)
        {
            return Path.Combine(GetBaseDirectory(), name);
        }

        private static IEnumerable<FileInfo> GetFiles(string directory)
        {
            if (Directory.Exists(directory))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    yield return new FileInfo(file);
                }
            }
        }
    }
}
