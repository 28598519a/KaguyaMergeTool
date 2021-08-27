using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/* 加入參考System.Windows.Forms */
namespace KaguyaMerge
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_folder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            string selectPath = fbd.SelectedPath;
            if (!Directory.Exists(selectPath))
            {
                System.Windows.MessageBox.Show("未選擇資料夾！");
                return;
            }

            App.Folder = selectPath;
            lb_folder.Content = Path.GetFileName(selectPath);
        }

        private void btn_offset_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "Offset Files (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
            {
                App.Offset = openFileDialog.FileName;
                lb_offset.Content = Path.GetFileName(openFileDialog.FileName);
            }
        }

        private async void btn_deal_pic_Click(object sender, RoutedEventArgs e)
        {
            if ((lb_folder.Content.ToString() == String.Empty) || (lb_offset.Content.ToString() == String.Empty))
            {
                System.Windows.MessageBox.Show("需要選擇資料夾、Offset表才能合成！");
                return;
            }

            int counter = 0;
            var Offset = new Dictionary<string, Tuple<int, int>>();
            IEnumerable<string> text = File.ReadLines(App.Offset).Cast<string>();

            foreach (string t in text)
            {
                string[] entry = t.Split(',');
                Tuple<int, int> XY = new Tuple<int, int>(Convert.ToInt32(entry[1]), Convert.ToInt32(entry[2]));
                Offset.Add(entry[0], XY);
            }

            if (!Directory.Exists(App.OutputRoot))
                Directory.CreateDirectory(App.OutputRoot);

            if (!Directory.Exists(App.OutputFace))
                Directory.CreateDirectory(App.OutputFace);

            if (!Directory.Exists(App.OutputCG))
                Directory.CreateDirectory(App.OutputCG);

            if (cb_anm.IsChecked == true)
            {
                if (!Directory.Exists(App.OutputAnm))
                    Directory.CreateDirectory(App.OutputAnm);
            }

            if (!Directory.Exists(App.OutputUsed))
                Directory.CreateDirectory(App.OutputUsed);

            try
            {
                // 由指定的資料夾中，取得CG中所有可能會用到的底、面
                List<string> CGfileList = Directory.GetFiles(App.Folder, "*.png", SearchOption.TopDirectoryOnly).Where(path => new Regex("cg[0-9]+").IsMatch(path)).ToList();
                CGfileList.Sort(App.StrCmpLogicalW);

                // 差分部件 (部)
                List<string> CGfileList_P = CGfileList.FindAll(find => new Regex("cg[0-9]+[B-Z]部").IsMatch(find));

                // 取得本來就已經是成品狀態的anm (甲)
                List<string> AnmfileList1 = CGfileList.FindAll(find => new Regex("cg[0-9]+_モ[0-9]+甲#[0-9]+").IsMatch(find));
                List<string> AnmfileList2 = CGfileList.FindAll(find => new Regex("cg[0-9]+_[0-9]+#[0-9]+").IsMatch(find));

                // 取得CG
                List<string> CGfileList_B = CGfileList.FindAll(find => new Regex("cg[0-9]+.png").IsMatch(find));
                List<string> CGfileList_M = CGfileList.FindAll(find => new Regex("cg[0-9]+_[0-9]+.png").IsMatch(find));

                foreach (string file in CGfileList_B)
                {
                    string filename = Path.GetFileNameWithoutExtension(file);

                    // 放棄無法自動處理的CG
                    if (CGfileList_M.FindAll(find => new Regex($"{filename}_[0-9]+.png").IsMatch(find)).Count != 0)
                        continue;
                    if (CGfileList_P.FindAll(find => new Regex($"{filename}[B-Z]部").IsMatch(find)).Count != 0)
                        continue;

                    // 取得CG差分部件 (甲)
                    List<string> SpiltfileList = CGfileList.FindAll(find => new Regex($"{filename}[B-Z]モ[0-9]+甲").IsMatch(find));

                    Dictionary<string, int> spiltmax = new Dictionary<string, int>();
                    string spiltcharMax = "A";
                    string spiltcharMin = "Z";
                    foreach (string spilt in SpiltfileList)
                    {
                        string Eng = Regex.Match(Path.GetFileNameWithoutExtension(spilt), "[B-Z]モ").Value.Replace("モ", String.Empty);
                        if (String.Compare(spiltcharMax, Eng) < 0)
                        {
                            spiltcharMax = Eng;
                        }
                        if (String.Compare(spiltcharMin, Eng) > 0)
                        {
                            spiltcharMin = Eng;
                        }

                        if (!spiltmax.ContainsKey(spiltcharMax))
                            spiltmax.Add(spiltcharMax, 0);

                        spiltmax[spiltcharMax] = Math.Max(spiltmax[spiltcharMax], Convert.ToInt32(Regex.Match(Path.GetFileNameWithoutExtension(spilt), "モ[0-9]+").Value.Replace("モ", String.Empty)));
                    }

                    // 取得CG差分部件 (顏)
                    List<string> CGfaceList = new List<string>();
                    List<string> FacefileList = new List<string>();
                    Dictionary<char, int> facemax = new Dictionary<char, int>();
                    FacefileList = CGfileList.FindAll(find => new Regex($"{filename}A顔[0-9]+").IsMatch(find));

                    // 無表情表示該CG為唯一 or 只有 (部)需進行手動合成
                    if (FacefileList.Count == 0)
                        continue;

                    if (SpiltfileList.Count != 0)
                    {
                        foreach (string face in FacefileList)
                        {
                            string facename = Path.GetFileNameWithoutExtension(face);
                            string outpath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                            if (Offset.ContainsKey(facename))
                            {
                                Merge(file, face, Offset[facename].Item1, Offset[facename].Item2, outpath);
                                CGfaceList.Add(outpath);

                                if (!facemax.ContainsKey('A'))
                                    facemax.Add('A', 0);

                                facemax['A'] = facemax['A']++;

                                // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                File.Move(face, $"{App.OutputUsed}/{facename}.png");
                            }
                        }

                        // 針對一張CG包含多個角色 (顏)的部分進行處理
                        if (spiltcharMin != "B")
                        {
                            FacefileList = CGfileList.FindAll(find => new Regex($"{filename}[B-Z]顔[0-9]+").IsMatch(find));

                            // 多角色表情同步變化 (後續的CG甲應只有1組)
                            if (spiltcharMin == spiltcharMax)
                            {
                                foreach (string face in FacefileList)
                                {
                                    string facename = Path.GetFileNameWithoutExtension(face);
                                    string outpath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                                    if (Offset.ContainsKey(facename))
                                    {
                                        // 直接取代A顏
                                        Merge(outpath, face, Offset[facename].Item1, Offset[facename].Item2, outpath);

                                        // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                        File.Move(face, $"{App.OutputUsed}/{facename}.png");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // 只有表情
                        int spiltnum = 1;
                        foreach (string anm in AnmfileList1.FindAll(find => new Regex(filename).IsMatch(find)))
                        {
                            if (spiltnum == Convert.ToInt32(Regex.Match(anm, "_[0-9]+").Value.Replace("_", String.Empty)))
                                spiltnum++;
                        }

                        foreach (string face in FacefileList)
                        {
                            string facename = Path.GetFileNameWithoutExtension(face);
                            string outpath = $"{App.OutputCG}/{filename}_{spiltnum}.png";
                            if (Offset.ContainsKey(facename))
                            {
                                Merge(file, face, Offset[facename].Item1, Offset[facename].Item2, outpath);

                                // 刷新合成進度
                                lb_done.Content = counter++;
                                await Task.Delay(1);

                                // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                File.Move(face, $"{App.OutputUsed}/{facename}.png");
                            }

                            spiltnum++;
                        }

                        // CG底複製至CG；CG底放到Used，與之後需要靠手動合成處理的部件分開
                        File.Copy(file, $"{App.OutputCG}/{filename}.png");
                        File.Move(file, $"{App.OutputUsed}/{filename}.png");
                        continue;
                    }

                    char nowFaceEng = 'A';
                    string ImgBase = null;
                    bool BaseCalc = true;
                    string preEng = Regex.Match(Path.GetFileNameWithoutExtension(SpiltfileList[0]), "[B-Z]モ").Value.Replace("モ", String.Empty);
                    string prespilt = String.Empty;
                    string preout = String.Empty;
                    int appendnum = 0;

                    // CG、CG顏 + 差分部件 (甲) 
                    foreach (string spilt in SpiltfileList)
                    {
                        string spiltname = Path.GetFileNameWithoutExtension(spilt);
                        int spiltnum = Convert.ToInt32(Regex.Match(spiltname, "モ[0-9]+").Value.Replace("モ", String.Empty));
                        string nowEng = Regex.Match(spiltname, "[B-Z]モ").Value.Replace("モ", String.Empty);

                        // 如果差分部件甲是新的小組 (ex: B→C)
                        if (String.Compare(nowEng, preEng) > 0)
                        {
                            string prespiltname = Path.GetFileNameWithoutExtension(prespilt);

                            // 只有單角色表情，表示C的表情直接沿用B的最後一個合成底圖來合就好
                            if (spiltcharMin == "B")
                            {
                                ImgBase = preout;
                                BaseCalc = false;
                            }
                            else
                            {
                                // 有多角色表情且異步變化，則應由最後合的CG甲，合下一個角色的表情後直接更新取代A顏 (角色數量 & 差分部件甲組數 應相同)
                                CGfaceList.Clear();
                                nowFaceEng++;
                                foreach (string face in FacefileList.FindAll(find => new Regex($"{filename}{nowFaceEng}顔[0-9]+").IsMatch(find)))
                                {
                                    string facename = Path.GetFileNameWithoutExtension(face);
                                    char FaceEng = Convert.ToChar(Regex.Match(facename, "[B-Z]顔").Value.Replace("顔", String.Empty));
                                    string facepath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                                    if (Offset.ContainsKey(facename))
                                    {
                                        Merge(preout, face, Offset[facename].Item1, Offset[facename].Item2, facepath);
                                        CGfaceList.Add(facepath);

                                        if (!facemax.ContainsKey(FaceEng))
                                            facemax.Add(FaceEng, 0);

                                        facemax[FaceEng] = facemax[FaceEng]++;

                                        // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                        File.Move(face, $"{App.OutputUsed}/{facename}.png");
                                    }
                                }
                            }

                            appendnum += Convert.ToInt32(Regex.Match(prespiltname, "モ[0-9]+").Value.Replace("モ", String.Empty));
                        }

                        if (BaseCalc == true)
                        {
                            // 間隔多少換下一張臉的參考值
                            double facebase = (double)spiltmax[nowEng] / (double)facemax[nowFaceEng];
                            int facenum = Convert.ToInt32(Math.Round((double)spiltnum / facebase));
                            if (facenum < 1) facenum = 1;

                            ImgBase = CGfaceList[facenum - 1];
                        }

                        string anm = Regex.Match(spiltname, "#[0-9]+").Value;
                        int outnum = spiltnum + appendnum;
                        string outpath = $"{App.OutputCG}/{filename}_{outnum}.png";

                        // 紀錄最後一個非差分部件的名稱
                        prespilt = $"{App.OutputUsed}/{spiltname}.png";
                        if (anm == String.Empty)
                        {
                            preEng = nowEng;
                            preout = outpath;
                        }

                        // 如果是anm，要用乙的座標去查
                        if (anm != String.Empty)
                        {
                            spiltname = spiltname.Replace(anm, String.Empty).Replace("甲", "乙");
                            if (cb_anm.IsChecked == true)
                            {
                                if (!Directory.Exists($"{App.OutputAnm}/{filename}_{outnum}"))
                                    Directory.CreateDirectory($"{App.OutputAnm}/{filename}_{outnum}");
                                outpath = $"{App.OutputAnm}/{filename}_{outnum}/{filename}_{outnum}{anm}.png";
                            }
                        }

                        Merge(ImgBase, spilt, Offset[spiltname].Item1, Offset[spiltname].Item2, outpath);

                        // 刷新合成進度
                        lb_done.Content = counter++;
                        await Task.Delay(1);

                        if (outnum == 1)
                        {
                            outpath = $"{App.OutputCG}/{filename}.png";
                            Merge(file, spilt, Offset[spiltname].Item1, Offset[spiltname].Item2, outpath);

                            // 刷新合成進度
                            lb_done.Content = counter++;
                            await Task.Delay(1);
                        }

                        // 拿來合過的CG甲放到Used，與之後需要靠手動合成處理的部件分開 (這裡不能用spiltname，因為中間要查座標的時候動過名稱)
                        File.Move(spilt, $"{App.OutputUsed}/{Path.GetFileNameWithoutExtension(spilt)}.png");
                    }

                    // 拿來合過的CG底放到Used，與之後需要靠手動合成處理的部件分開
                    File.Move(file, $"{App.OutputUsed}/{filename}.png");
                }

                if (Directory.Exists(App.OutputFace))
                    Directory.Delete(App.OutputFace, true);

                // 分離無需合成的anm
                if (cb_anm_OK.IsChecked == true)
                {
                    if (AnmfileList1.Count != 0 || AnmfileList2.Count != 0)
                    {
                        lb_done.Content = lb_done.Content.ToString() + "，整理中";

                        if (!Directory.Exists(App.AnmBackup))
                            Directory.CreateDirectory(App.AnmBackup);

                        foreach (string anm in AnmfileList1)
                        {
                            string anmname = Path.GetFileNameWithoutExtension(anm);
                            string filename = Regex.Match(anmname, "cg[0-9]+").Value;
                            int spiltnum = Convert.ToInt32(Regex.Match(anmname, "モ[0-9]+").Value.Replace("モ", String.Empty));
                            string anmnum = Regex.Match(anmname, "#[0-9]+").Value;
                            string outputpath = $"{App.OutputAnm}/{filename}_{spiltnum}";
                            string append = "";

                            if (Directory.Exists(outputpath))
                            {
                                append = "B";
                                outputpath += append;
                            }

                            if (cb_anm.IsChecked == true)
                            {
                                if (!Directory.Exists(outputpath))
                                    Directory.CreateDirectory(outputpath);

                                outputpath = $"{outputpath}/{filename}_{spiltnum}{append}";
                            }

                            File.Copy(anm, $"{outputpath}{anmnum}.png");
                            File.Move(anm, $"{App.AnmBackup}/{anmname}.png");
                        }

                        foreach (string anm in AnmfileList2)
                        {
                            string anmname = Path.GetFileNameWithoutExtension(anm);
                            string filename = Regex.Match(anmname, "cg[0-9]+_[0-9]+").Value;
                            string anmnum = Regex.Match(anmname, "#[0-9]+").Value;

                            if (!Directory.Exists($"{App.OutputAnm}/{filename}"))
                                Directory.CreateDirectory($"{App.OutputAnm}/{filename}");

                            File.Copy(anm, $"{App.OutputAnm}/{filename}/{filename}{anmnum}.png");
                            File.Move(anm, $"{App.AnmBackup}/{anmname}.png");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"失敗：{ex.Message}");
            }

            // 合成完成，跳通知告知結果
            lb_done.Content = $"Done {counter}";
            System.Windows.MessageBox.Show($"Done, 共合成 {counter} 個");
        }

        private void Merge(string file1, string file2, int Xoffset, int Yoffset, string outpath)
        {
            try
            {
                Bitmap img1 = new Bitmap(file1);
                Bitmap img2 = new Bitmap(file2);

                if (file1 == outpath)
                {
                    img1.Dispose();
                    File.Move(file1, $"{file1}_tmp1");
                    img1 = new Bitmap($"{file1}_tmp1");
                }
                else if (file2 == outpath)
                {
                    img2.Dispose();
                    File.Move(file1, $"{file1}_tmp2");
                    img2 = new Bitmap($"{file1}_tmp2");
                }

                ImageDeal.DealImage(img1, img2, Xoffset, Yoffset, outpath);
                img1.Dispose();
                img2.Dispose();

                if (File.Exists($"{file1}_tmp1"))
                    File.Delete($"{file1}_tmp1");

                if (File.Exists($"{file1}_tmp2"))
                    File.Delete($"{file1}_tmp2");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"失敗：{ex.Message}");
            }
        }

        private void cb_anm_OK_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("勾選此功能請確保CG資料夾中 : \n" +
                "1. 在按下\"開始合成\"後，結束合成時裡面會有全遊戲CG自動合成後的檔案，否則可能會命名錯誤!\n" +
                "2. 裡面沒有與欲分離的檔案重複的內容，否則可能會以B的資料夾分類重複出現 (也就是說分離操作應是第一次執行)", "注意事項");
        }
    }
}