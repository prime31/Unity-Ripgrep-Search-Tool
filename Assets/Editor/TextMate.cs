using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    public static class TextMate
    {
        public static bool IsInstalled() => File.Exists("/usr/local/bin/mate");
        
        /// <summary>opens the file in TextMate or if it is not installed the default text editor</summary>
        public static void Open(string path, int lineNumber = 1)
        {
            var command = IsInstalled() ? $"/usr/local/bin/mate {path} -l {lineNumber}" : $"open -t {path}";
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/zsh";
            process.StartInfo.Arguments = "-c \" " + command + " \"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var txt = process.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(txt))
                Debug.LogError($"Output: {txt}");

            txt = process.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(txt))
                Debug.LogError($"Error: {txt}");
            
            process.WaitForExit();
        }

        public static void OpenSelectedAsset()
        {
            if (Selection.assetGUIDs.Length > 0) Open(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
        }
    }
}
