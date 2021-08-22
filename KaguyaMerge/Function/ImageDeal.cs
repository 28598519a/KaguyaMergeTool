using System.Drawing;
using System.Drawing.Drawing2D;

public class ImageDeal
{
    public static void DealImage(Bitmap SrcBitmap1, Bitmap SrcBitmap2, int Xoffset, int Yoffset, string path)
    {
        int ImgWidth = SrcBitmap1.Width;
        int ImgHeight = SrcBitmap1.Height;

        using (var bmp = new Bitmap(ImgWidth, ImgHeight))
        {
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.CompositingQuality = CompositingQuality.HighQuality;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.DrawImage(SrcBitmap1, new Rectangle(0, 0, ImgWidth, ImgHeight), 0, 0, ImgWidth, ImgHeight, GraphicsUnit.Pixel);
                gr.DrawImage(SrcBitmap2, Xoffset, Yoffset, SrcBitmap2.Width, SrcBitmap2.Height);
            }

            bmp.Save(path);
        }
    }
}