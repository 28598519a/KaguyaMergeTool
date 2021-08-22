using System;
using System.Windows;

namespace KaguyaMerge
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Folder = "";
        public static string Offset = "";
        public static string OutputRoot = "D:/[test]";
        public static string OutputFace = $"{OutputRoot}/CG顏";
        public static string OutputCG = $"{OutputRoot}/CG";
        public static string OutputAnm = $"{OutputCG}/anm";
        public static string OutputUsed = $"{OutputRoot}/Used";
        public static string AnmBackup = $"{OutputRoot}/Usedanm(backup)";

        // 用於檔案名稱排序
        [System.Runtime.InteropServices.DllImport("Shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }
}
