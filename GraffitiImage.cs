using AFS;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Jet_Set_Radio_Graffiti_Tool
{
    internal class GraffitiImage
    {
        enum FileTypes { ExtraLarge = 4, Large = 2, Small = 1 };

        // Copied from https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp
        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static void ImportImage(PictureBox pictureBox, AFSArchive archive, ListBox listBox)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select file to import!";
            ofd.Filter = "Image Files|*.png;*.jpg;*.bmp";
            ofd.ShowDialog();
            if (ofd.FileName != "")
            {
                if (File.Exists(ofd.FileName))
                {
                    Image image = Bitmap.FromFile(ofd.FileName);
                    Bitmap bmp = new Bitmap(image);

                    if (pictureBox.Image == null)
                    {
                        MessageBox.Show("No image selected.", "JSR Graffiti Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    //if (bmp.Height != 128 || (bmp.Width != 128 && bmp.Width != 256 & bmp.Width != 512))
                    if (pictureBox.Image.Height != bmp.Height || pictureBox.Image.Width != bmp.Width)
                    {
                        DialogResult result = MessageBox.Show("Source image is " + bmp.Width + "x" + bmp.Height + ", but needs to be 128x128 (for small), 256x128 (for large), or 512x128 (for extra large).\n\nShould Jet Set Radio Graffiti Tool try to resize it?", "JSR Graffiti Tool", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            bmp = ResizeImage(bmp,pictureBox.Image.Width, pictureBox.Image.Height);
                        }
                        else
                        {
                            return;
                        }
                    }

                    Color[,] pixelColors = new Color[bmp.Width, bmp.Height];

                    for (int y = 0; y < bmp.Height; ++y)
                    {
                        for (int x = 0; x < bmp.Width; ++x)
                        {
                            pixelColors[x, y] = bmp.GetPixel(x, y);
                        }
                    }

                    try
                    {
                        byte[] data = null;

                        if (bmp.Width == 128)
                            data = new byte[0x8000];
                        else if (bmp.Width == 256)
                            data = new byte[0x10000];
                        else if (bmp.Width == 512)
                            data = new byte[0x20000];

                        int pos = 0;

                        for (int y = 0; y < bmp.Height; ++y)
                        {
                            for (int x = 0; x < bmp.Width; ++x)
                            {
                                int gb = ((pixelColors[x, y].G / 16) << 4) + (pixelColors[x, y].B / 16);
                                int ar = ((pixelColors[x, y].A / 16) << 4) + (pixelColors[x, y].R / 16);

                                data[pos] = (byte)gb;
                                data[pos + 1] = (byte)ar;
                                pos += 2;
                            }
                        }

                        if (bmp.Width == 128)
                        {
                            while (pos < 0x8000)
                            {
                                data[pos] = 0;
                                pos++;
                            }
                        }
                        else if (bmp.Width == 256)
                        {
                            while (pos < 0x10000)
                            {
                                data[pos] = 0;
                                pos++;
                            }
                        }
                        else if (bmp.Width == 512)
                        {
                            while (pos < 0x20000)
                            {
                                data[pos] = 0;
                                pos++;
                            }
                        }



                        AFSFile file = new AFSFile();
                        file.data = data;
                        FileInfo finfo = new FileInfo(ofd.FileName);
                        file.entry.size = (uint)data.Length;
                        file.entry.filename = listBox.SelectedItem.ToString();
                        file.entry.year = (UInt16)finfo.LastWriteTime.Year;
                        file.entry.month = (UInt16)finfo.LastWriteTime.Month;
                        file.entry.day = (UInt16)finfo.LastWriteTime.Day;
                        file.entry.hour = (UInt16)finfo.LastWriteTime.Hour;
                        file.entry.minute = (UInt16)finfo.LastWriteTime.Minute;
                        file.entry.second = (UInt16)finfo.LastWriteTime.Second;

                        file.toc.size = (uint)data.Length;

                        archive.ReplaceFile(listBox.SelectedItem.ToString(), file);
                        MessageBox.Show("File imported!", "JSR Graffiti Tool");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: \n" + ex.ToString(), "JSR Graffiti Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Invalid file.", "JSR Graffiti Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void ExportImage(PictureBox pictureBox)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select location for image.";
            sfd.Filter = "Image Files|*.png;*.jpg;*.bmp";
            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Save(sfd.FileName);
                    MessageBox.Show("Exported Image!", "JSR Graffiti Tool");
                }
                else
                {
                    MessageBox.Show("No Graffiti selected!", "JSR Graffiti Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}