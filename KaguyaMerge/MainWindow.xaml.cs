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
            App.OutputRoot = selectPath;
            App.UpdateVar();
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

        public enum C2B
        {
            No = 0,
            Base = 1,
            Part = 2
        }

        private async void btn_deal_pic_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(lb_folder.Content.ToString()) || String.IsNullOrEmpty(lb_offset.Content.ToString()))
            {
                System.Windows.MessageBox.Show("需要選擇資料夾、Offset表才能合成！");
                return;
            }

            // 有指定CG資料夾，但沒呼叫UpdateVar()時會出現的情況
            if (String.IsNullOrEmpty(App.OutputRoot))
            {
                System.Windows.MessageBox.Show("輸出資料夾指定異常，請檢查UpdateVar()是否有正確呼叫！");
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
                    // 計算每一組CG底的合成次數
                    int BaseCounter = 0;
                    string filename = Path.GetFileNameWithoutExtension(file);

                    // 放棄無法自動處理的CG
                    if (CGfileList_M.FindAll(find => new Regex($"{filename}_[0-9]+.png").IsMatch(find)).Count != 0)
                        continue;
                    if (CGfileList_P.FindAll(find => new Regex($"{filename}[B-Z]部").IsMatch(find)).Count != 0)
                        continue;

                    // 取得CG差分部件 (甲)
                    List<string> SpiltfileList = CGfileList.FindAll(find => new Regex($"{filename}[B-Z]モ[0-9]+甲").IsMatch(find));

                    Dictionary<string, int> spiltmax = new Dictionary<string, int>();
                    Dictionary<string, int> spiltmin = new Dictionary<string, int>();
                    int SpiltTotal = 0;
                    int SpiltCount = 0;
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

                        int nownum = Convert.ToInt32(Regex.Match(Path.GetFileNameWithoutExtension(spilt), "モ[0-9]+").Value.Replace("モ", String.Empty));
                        if (!spiltmax.ContainsKey(spiltcharMax))
                        {
                            // 初始化
                            spiltmax.Add(spiltcharMax, nownum);
                            spiltmin.Add(spiltcharMax, nownum);
                        }
                        else
                        {
                            // 由於一組anm的モ[0-9]是相同的，所以只會被記為1次
                            spiltmax[spiltcharMax] = Math.Max(spiltmax[spiltcharMax], nownum);
                            spiltmin[spiltcharMax] = Math.Min(spiltmin[spiltcharMax], nownum);
                        }
                    }
                    foreach (KeyValuePair<string, int> pair in spiltmax)
                    {
                        SpiltTotal += pair.Value;
                    }
                    foreach (KeyValuePair<string, int> pair in spiltmin)
                    {
                        // 多算的要扣掉，多減的要補回來
                        SpiltTotal = (SpiltTotal - pair.Value) + 1;
                    }

                    // 多表情+多部件組的要單獨按組算
                    if (spiltcharMin != "B" && spiltcharMin != spiltcharMax && spiltmax.Count != 0)
                    {
                        SpiltTotal = spiltmax[spiltcharMin] - spiltmin[spiltcharMin] + 1;
                    }
                    SpiltCount = SpiltTotal;

                    // 取得CG差分部件 (顏)
                    List<string> CGfaceList = new List<string>();
                    List<string> FacefileList = new List<string>();
                    Dictionary<char, int> facemax = new Dictionary<char, int>();
                    FacefileList = CGfileList.FindAll(find => new Regex($"{filename}[A-Z]顔[0-9]+").IsMatch(find));

                    // 無表情表示該CG為唯一 or 只有 (部)需進行手動合成
                    if (FacefileList.Count == 0)
                        continue;

                    if (SpiltfileList.Count != 0)
                    {
                        // 多表情 (依序且非同步) + 多部件組
                        if (spiltcharMin != "B" && spiltcharMin != spiltcharMax)
                        {
                            List<string> tmplist = new List<string>();

                            // 如果有多表情+多部件組，則從第一個差分組開始檢查是否有 (?1甲)，必須跟CG底合併後才能合顏
                            foreach (string spilt in SpiltfileList)
                            {
                                string spiltname = Path.GetFileNameWithoutExtension(spilt);
                                string Eng = Regex.Match(spiltname, "[C-Z]モ").Value.Replace("モ", String.Empty);

                                // 根據ASCII表，英文字母是連續的
                                for (char c = Convert.ToChar(spiltcharMin); c <= Convert.ToChar(spiltcharMax); c++)
                                {
                                    if (spiltname.Contains(c.ToString() + "モ1甲"))
                                    {
                                        Color pixelColor = new Color();

                                        // 先檢查CG底確實有缺失的空白處，再合併上去
                                        using (Bitmap bmp = new Bitmap(file))
                                        {
                                            int count = 0;
                                            using (Bitmap bmpB = new Bitmap(spilt))
                                            {
                                                // 因為有些位置會有液體導致判斷錯誤，因此做3次採樣 (只要成功一次就算有)
                                                do
                                                {
                                                    int w = 0;
                                                    int h = 0;

                                                    // 先找1甲計算出有顏色的中心點，再拿回CG底做檢查
                                                    switch (count)
                                                    {
                                                        case 0:
                                                            // 從中間
                                                            h = (bmpB.Height - 1) / 2;
                                                            break;
                                                        case 1:
                                                            // 從上面
                                                            h = (bmpB.Height - 1) / 4;
                                                            break;
                                                        case 2:
                                                            // 從下面
                                                            h = (bmpB.Height - 1) - ((bmpB.Height - 1) / 4);
                                                            break;
                                                    }

                                                    int x01 = 0;
                                                    int x02 = 0;
                                                    for (int x = 0; x < bmpB.Width; x++)
                                                    {
                                                        if (bmpB.GetPixel(x, h).A != 0)
                                                        {
                                                            x01 = x;
                                                            break;
                                                        }
                                                    }
                                                    for (int x = bmpB.Width - 1; x >= 0; x--)
                                                    {
                                                        if (bmpB.GetPixel(x, h).A != 0)
                                                        {
                                                            x02 = x;
                                                            break;
                                                        }
                                                    }
                                                    w = (x01 + x02) / 2;

                                                    pixelColor = bmp.GetPixel(Offset[spiltname].Item1 + w, Offset[spiltname].Item2 + h);
                                                    count++;
                                                }
                                                while (!pixelColor.ToArgb().Equals(Color.White.ToArgb()) && count < 3);
                                            }

                                            // 先把要合上的所有1甲差分部件 + CG底
                                            if (pixelColor.ToArgb().Equals(Color.White.ToArgb()))
                                            {
                                                string outpath = $"{App.OutputFace}/{filename}.png";
                                                if (!File.Exists(outpath))
                                                {
                                                    Merge(file, spilt, Offset[spiltname].Item1, Offset[spiltname].Item2, outpath);
                                                }
                                                else
                                                {
                                                    Merge(outpath, spilt, Offset[spiltname].Item1, Offset[spiltname].Item2, outpath);
                                                }
                                            }
                                        }

                                        // 先存著，等跑完再把這些用掉的差分部件做處理
                                        tmplist.Add(spilt);
                                    }
                                }
                            }

                            foreach (string s in tmplist)
                            {
                                // 該差分部件為B底的一部分，所以要從List拿掉另外處理
                                SpiltfileList.Remove(s);
                                SpiltTotal -= 1;

                                // 拿來合過的差分部件放到Used，與之後需要靠手動合成處理的部件分開
                                File.Move(s, $"{App.OutputUsed}/{Path.GetFileName(s)}");
                            }

                            FacefileList = CGfileList.FindAll(find => new Regex($"{filename}[A-Z]顔[0-9]+").IsMatch(find));

                            // 合上A顏 (後續的顏等合差分部件處在處理)
                            foreach (string face in FacefileList.FindAll(find => new Regex($"{filename}A顔[0-9]+").IsMatch(find)))
                            {
                                string facename = Path.GetFileNameWithoutExtension(face);
                                string outpath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                                string tmpfile = $"{App.OutputFace}/{filename}.png";
                                if (Offset.ContainsKey(facename))
                                {
                                    Merge(tmpfile, face, Offset[facename].Item1, Offset[facename].Item2, outpath);
                                    CGfaceList.Add(outpath);

                                    if (!facemax.ContainsKey('A'))
                                        facemax.Add('A', 0);

                                    facemax['A'] = facemax['A']++;
                                }

                                // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                File.Move(face, $"{App.OutputUsed}/{facename}.png");
                            }
                        }
                        else
                        {
                            foreach (string face in FacefileList)
                            {
                                string facename = Path.GetFileNameWithoutExtension(face);
                                string outpath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                                if (Offset.ContainsKey(facename))
                                {
                                    // 先做常規CG底+A顔
                                    if (facename.Contains("A顔"))
                                    {
                                        Merge(file, face, Offset[facename].Item1, Offset[facename].Item2, outpath);
                                        CGfaceList.Add(outpath);

                                        if (!facemax.ContainsKey('A'))
                                            facemax.Add('A', 0);

                                        facemax['A'] = facemax['A']++;
                                    }
                                    else if (spiltcharMin == spiltcharMax)
                                    {
                                        // 多角色表情同步變化 (後續的CG甲應只有1組)
                                        // 直接取代A顔
                                        Merge(outpath, face, Offset[facename].Item1, Offset[facename].Item2, outpath);
                                    }

                                    // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                    File.Move(face, $"{App.OutputUsed}/{facename}.png");
                                }
                            }
                        }
                    }
                    else
                    {
                        // 只有表情，沒差分部件
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
                                BaseCounter++;
                                lb_done.Content = counter++;
                                await Task.Delay(1);

                                // 拿來合過的CG顏放到Used，與之後需要靠手動合成處理的部件分開
                                File.Move(face, $"{App.OutputUsed}/{facename}.png");
                            }

                            spiltnum++;
                        }

                        // CG顏複製至CG；CG顏放到Used，與之後需要靠手動合成處理的部件分開
                        File.Copy(file, $"{App.OutputCG}/{filename}.png");
                        File.Move(file, $"{App.OutputUsed}/{filename}.png");

                        // 刷新合成進度
                        BaseCounter++;
                        lb_done.Content = counter++;
                        await Task.Delay(1);

                        // 沒有差分部件，所以後續不用執行
                        continue;
                    }

                    char nowFaceEng = 'A';
                    string ImgBase = null;
                    string ImgBaseB = String.Empty;
                    string preEng = Regex.Match(Path.GetFileNameWithoutExtension(SpiltfileList[0]), "[B-Z]モ").Value.Replace("モ", String.Empty);
                    string prespilt = String.Empty;
                    string prespiltKey = String.Empty;
                    string preout = String.Empty;
                    int appendnum = 0;
                    C2B MergeC2B = C2B.No;

                    if (spiltcharMin == "B")
                    {
                        // 如果只有單表情，檢查是否有差分部件 (C1甲)，此等同於B的底圖
                        foreach (string spilt in SpiltfileList)
                        {
                            string spiltname = Path.GetFileNameWithoutExtension(spilt);
                            string Eng = Regex.Match(spiltname, "[B-Z]モ").Value.Replace("モ", String.Empty);

                            if (spiltname.Contains("Cモ1甲"))
                            {
                                ImgBaseB = spilt;
                                string spiltnameB = Path.GetFileNameWithoutExtension(ImgBaseB);
                                Color pixelColor = new Color();

                                using (Bitmap bmp = new Bitmap(file))
                                {
                                    int count = 0;
                                    using (Bitmap bmpB = new Bitmap(ImgBaseB))
                                    {
                                        // 因為有些位置會有液體導致判斷錯誤，因此做3次採樣
                                        do
                                        {
                                            int w = 0;
                                            int h = 0;

                                            // ImgBaseB會大於空白區，所以要計算出有顏色的中心點，不能直接用邊緣
                                            switch (count)
                                            {
                                                case 0:
                                                    // 從中間
                                                    h = (bmpB.Height - 1) / 2;
                                                    break;
                                                case 1:
                                                    // 從上面
                                                    h = (bmpB.Height - 1) / 4;
                                                    break;
                                                case 2:
                                                    // 從下面
                                                    h = (bmpB.Height - 1) - ((bmpB.Height - 1) / 4);
                                                    break;
                                            }


                                            int x01 = 0;
                                            int x02 = 0;
                                            for (int x = 0; x < bmpB.Width; x++)
                                            {
                                                if (bmpB.GetPixel(x, h).A != 0)
                                                {
                                                    x01 = x;
                                                    break;
                                                }
                                            }
                                            for (int x = bmpB.Width - 1; x >= 0; x--)
                                            {
                                                if (bmpB.GetPixel(x, h).A != 0)
                                                {
                                                    x02 = x;
                                                    break;
                                                }
                                            }
                                            w = (x01 + x02) / 2;

                                            pixelColor = bmp.GetPixel(Offset[spiltnameB].Item1 + w, Offset[spiltnameB].Item2 + h);
                                            count++;
                                        }
                                        while (!pixelColor.ToArgb().Equals(Color.White.ToArgb()) && count < 3);
                                    }

                                    // 看看CG底的那個位置是不是空白的，如果是就是底的一部分，如果不是就是部件
                                    // https://stackoverflow.com/questions/28920332/result-of-color-getpixel-equalscolor-blue-comes-out-false
                                    if (pixelColor.ToArgb().Equals(Color.White.ToArgb()))
                                        MergeC2B = C2B.Base;
                                    else
                                        MergeC2B = C2B.Part;
                                }

                                // 該差分部件為B底的一部分，所以要從List拿掉另外處理
                                SpiltfileList.Remove(spilt);
                                SpiltTotal -= 1;

                                // 修改List後，不可再繼續執行此迴圈
                                break;
                            }
                        }
                    }

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

                            // 只有單角色表情，則更新CG顏
                            if (spiltcharMin == "B")
                            {
                                // 直接更新取代CG顏，用CG顏 + 已經移到Used的最後一次用的差分部件 (針對會存取到Used資料夾的操作都必須小心)
                                foreach (string CGface in CGfaceList)
                                {
                                    Merge(CGface, prespilt, Offset[prespiltKey].Item1, Offset[prespiltKey].Item2, CGface);
                                }
                            }
                            else
                            {
                                // 有多角色表情且異步變化，則應由最後合的CG甲，合下一個角色的表情後直接更新取代A顏 (顏組數 & 差分部件甲組數 應相同)
                                CGfaceList.Clear();
                                nowFaceEng++;
                                SpiltTotal = spiltmax[nowEng] - spiltmin[nowEng] + 1;

                                // 從A-Z顏中，只取出現在要用的一組顔來處理
                                foreach (string face in FacefileList.FindAll(find => new Regex($"{filename}{nowFaceEng}顔[0-9]+").IsMatch(find)))
                                {
                                    string facename = Path.GetFileNameWithoutExtension(face);
                                    char FaceEng = Convert.ToChar(Regex.Match(facename, "[B-Z]顔").Value.Replace("顔", String.Empty));
                                    string facepath = $"{App.OutputFace}/{filename}_{Regex.Match(facename, "顔[0-9]+")}.png";
                                    if (Offset.ContainsKey(facename))
                                    {
                                        // !!!如果會有差分部件跟顏可能有交疊的狀況，就要考慮按上面單表情的方式處理!!!
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

                        // 間隔多少換下一張顏的參考值
                        double facebase = (double)SpiltCount / (double)facemax[nowFaceEng];
                        int facenum = Convert.ToInt32(Math.Round((double)spiltnum / facebase));
                        if (facenum < 1) facenum = 1;

                        // 決定該差分部件要用哪個顏為底來合成
                        ImgBase = CGfaceList[facenum - 1];

                        string anm = Regex.Match(spiltname, "#[0-9]+").Value;
                        int outnum = spiltnum + appendnum;
                        string outpath = $"{App.OutputCG}/{filename}_{outnum}.png";

                        // 紀錄最後一個前一次部件的資訊，方便取用
                        prespilt = $"{App.OutputUsed}/{spiltname}.png";
                        preEng = nowEng;
                        preout = outpath;
                        if (anm == String.Empty)
                            prespiltKey = spiltname;
                        else
                            prespiltKey = spiltname.Replace(anm, String.Empty).Replace("甲", "乙");


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

                        // 針對有Cモ1甲的情況合成進B的圖
                        if (nowEng == "B")
                        {
                            string spiltnameB = Path.GetFileNameWithoutExtension(ImgBaseB);

                            // 針對是 整個B的CG底一部分 or B1甲的物件 分別處理
                            if (MergeC2B == C2B.Base)
                            {
                                // 剛才合的 + Cモ1甲取代掉剛才合的
                                Merge(outpath, ImgBaseB, Offset[spiltnameB].Item1, Offset[spiltnameB].Item2, outpath);
                            }
                            else if (spiltnum == 1 && MergeC2B == C2B.Part)
                            {
                                Merge(outpath, ImgBaseB, Offset[spiltnameB].Item1, Offset[spiltnameB].Item2, outpath);
                            }
                        }

                        // 刷新合成進度
                        BaseCounter++;
                        lb_done.Content = counter++;
                        await Task.Delay(1);

                        // CG底的表情與顏1並不同，所以要合成一張CG底與第一張差分的結果 (不能用outnum，因為第一個差分不一定是1甲)
                        if (BaseCounter == 1)
                        {
                            string tmpfile = $"{App.OutputFace}/{filename}.png";
                            outpath = $"{App.OutputCG}/{filename}.png";

                            if (!File.Exists(tmpfile))
                            {
                                Merge(file, spilt, Offset[spiltname].Item1, Offset[spiltname].Item2, outpath);

                                // 針對有Cモ1甲的情況合成進B的圖
                                if (nowEng == "B")
                                {
                                    // 針對是CG底本身有空白的情況來處理
                                    if (MergeC2B == C2B.Base)
                                    {
                                        string spiltnameB = Path.GetFileNameWithoutExtension(ImgBaseB);

                                        // 剛才合的 + Cモ1甲取代掉剛才合的
                                        Merge(outpath, ImgBaseB, Offset[spiltnameB].Item1, Offset[spiltnameB].Item2, outpath);
                                    }
                                }
                            }
                            else
                            {
                                // 多表情 (依序且非同步) + 多部件組的情況的底是有處理好的存在CG顏資料夾裡
                                File.Move(tmpfile, outpath);
                            }

                            // 刷新合成進度
                            BaseCounter++;
                            lb_done.Content = counter++;
                            await Task.Delay(1);
                        }

                        // 拿來合過的CG甲放到Used，與之後需要靠手動合成處理的部件分開 (這裡不能用spiltname，因為中間要查座標的時候動過名稱)
                        File.Move(spilt, $"{App.OutputUsed}/{Path.GetFileName(spilt)}");
                    }

                    // 拿來合過的C1甲放到Used，與之後需要靠手動合成處理的部件分開
                    if (ImgBaseB != String.Empty)
                        File.Move(ImgBaseB, $"{App.OutputUsed}/{Path.GetFileName(ImgBaseB)}");

                    // 拿來合過的CG底放到Used，與之後需要靠手動合成處理的部件分開
                    File.Move(file, $"{App.OutputUsed}/{filename}.png");
                }

                // 所有合成動作完成後，刪掉CG顏資料夾
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

                        bool folder_chg = false;
                        foreach (string anm in AnmfileList1)
                        {
                            // 無需合成的anm不應該有出現在新一組差分部件後的情況 (ex: 有B甲、C甲，則該種anm應只出現在B的範圍內)
                            string anmname = Path.GetFileNameWithoutExtension(anm);
                            string filename = Regex.Match(anmname, "cg[0-9]+").Value;
                            int spiltnum = Convert.ToInt32(Regex.Match(anmname, "モ[0-9]+").Value.Replace("モ", String.Empty));
                            string anmnum = Regex.Match(anmname, "#[0-9]+").Value;
                            string outputpath = $"{App.OutputAnm}/{filename}_{spiltnum}";
                            string append = "";

                            // 換一組anm時要先重置append設定
                            if (anmnum == "#00")
                                folder_chg = false;

                            if (Directory.Exists(outputpath) || folder_chg == true)
                            {
                                // 只能對第一個anm判斷是要換資料夾，後續須延續第一個的append設定
                                if (anmnum == "#00")
                                    folder_chg = true;

                                if (folder_chg == true)
                                {
                                    append = "B";
                                    outputpath += append;
                                }
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
                // Debug模式下出現錯誤要顯示出包含錯誤行數的資訊，才能在不動try-catch的情況下直接去下斷點偵錯
                #if DEBUG
                    System.Windows.MessageBox.Show($"失敗：{ex.ToString()}");
                #else
                    System.Windows.MessageBox.Show($"失敗：{ex.Message}");
                #endif
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
            // 另外要考慮到 _1 跟 部 的組是被放棄合成的，這樣分離anm的時候命名上可能會有問題 (考慮之後加一個判斷CG資料夾內其餘差分部件檔名後在做分離)
            System.Windows.MessageBox.Show("勾選此功能請確保CG資料夾中 : \n" +
                "1. 在按下\"開始合成\"後，結束合成時裡面會有全遊戲CG自動合成後的檔案，否則可能會命名錯誤!\n" +
                "2. 裡面沒有與欲分離的檔案重複的內容，否則可能會以B的資料夾分類重複出現 (也就是說分離操作應是第一次執行)", "注意事項");
        }
    }
}