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
        public static string Folder = String.Empty;
        public static string Offset = String.Empty;
        public static string OutputRoot = String.Empty;

        public static string OutputFace = $"{OutputRoot}/CG顏";
        public static string OutputCG = $"{OutputRoot}/CG";
        public static string OutputAnm = $"{OutputCG}/anm";
        public static string OutputUsed = $"{OutputRoot}/Used";
        public static string AnmBackup = $"{OutputRoot}/Usedanm(backup)";

        // 用於檔案名稱排序
        [System.Runtime.InteropServices.DllImport("Shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);

        // 每次指定Folder後都應該呼叫這個function來刷新全域變數
        public static void UpdateVar()
        {
            OutputFace = $"{OutputRoot}/CG顏";
            OutputCG = $"{OutputRoot}/CG";
            OutputAnm = $"{OutputCG}/anm";
            OutputUsed = $"{OutputRoot}/Used";
            AnmBackup = $"{OutputRoot}/Usedanm(backup)";
        }
    }
}
