using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using AFS;

namespace Jet_Set_Radio_Graffiti_Tool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string dir = "";

        AFSArchive archive;
        enum FileTypes { ExtraLarge = 4, Large = 2, Small = 1 };

        void UpdateImage()
        {
            foreach (AFSFile file in archive.Files)
            {
                if (file.entry.filename != graffitiFiles.SelectedItem.ToString())
                    continue;

                var bytes = file.data;
                FileTypes graffitiSize;

                // Extra Large (512x128)
                if (bytes.Length == 0x20000)
                {
                    graffitiSize = FileTypes.ExtraLarge;
                    label1.Text = "Graffiti Size: Extra Large";
                } // large (256x128)
                else if (bytes.Length == 0x10000)
                {
                    graffitiSize = FileTypes.Large;
                    label1.Text = "Graffiti Size: Large";
                } // Small (128x128)
                else if (bytes.Length == 0x8000)
                {
                    graffitiSize = FileTypes.Small;
                    label1.Text = "Graffiti Size: Small";
                }
                else
                {
                    MessageBox.Show("Invalid file size! (Not a correct graffiti file?)", "JSR Graffiti Tool");
                    return;
                }

                int imageWidth = 128 * (int)graffitiSize;

                Bitmap bmp = new Bitmap(imageWidth, 128);
                int i = 0;
                int pos = 0;
                while (pos < bytes.Length && i < imageWidth * 128)
                {
                    Color color;

                    int byte1 = bytes[pos];
                    int byte2 = bytes[pos + 1];
                    pos += 2;

                    color = Color.FromArgb((byte2 >> 4) * 16,
                        ((byte)(byte2 << 4) >> 4) * 16,
                        (byte1 >> 4) * 16,
                        ((byte)(byte1 << 4) >> 4) * 16);

                    bmp.SetPixel(i % imageWidth, i / imageWidth, color);

                    i++;
                }

                pictureBox1.Image = bmp;
            }
        }

        private void AFSFileChanged(object sender, EventArgs e)
        {
            archive = new AFSArchive(dir + "/" + afsFiles.SelectedItem.ToString());

            graffitiFiles.Items.Clear();

            foreach (AFSFile file in archive.Files)
            {
                graffitiFiles.Items.Add(file.entry.filename);
            }

            groupBox2.Text = "Graffiti Files (" + graffitiFiles.Items.Count + ")";

            this.Text = "Jet Set Radio Graffiti Tool [" + dir + "\\" + afsFiles.SelectedItem.ToString() + "]";
        }

        private void GraffitiChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void ExportImage(object sender, EventArgs e)
        {
            GraffitiImage.ExportImage(pictureBox1);            
        }

        private void ImportFile(object sender, EventArgs e)
        {
            GraffitiImage.ImportImage(pictureBox1, archive, graffitiFiles);
            UpdateImage();
        }

        private void OpenFolderClick(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                Description = "Select JETRADIO Folder."
            };
            fbd.ShowDialog();

            if (Directory.Exists(fbd.SelectedPath))
            {
                string[] files = Directory.GetFiles(fbd.SelectedPath, "*GRF*.AFS");
                dir = fbd.SelectedPath;

                afsFiles.Items.Clear();

                foreach (string file in files)
                {
                    afsFiles.Items.Add(Path.GetFileName(file));
                }

                groupBox1.Text = "AFS Files (" + afsFiles.Items.Count + ")";

                this.Text = "Jet Set Radio Graffiti Tool [" + dir + "]";
            }
        }
        private void SaveFileClick(object sender, EventArgs e)
        {
            if (archive != null)
            {
                archive.Save(dir + "/" + afsFiles.SelectedItem.ToString());
                MessageBox.Show("Saved file: " + afsFiles.SelectedItem.ToString() + "!", "JSR Graffiti Tool");
            }
            else
            {
                MessageBox.Show("No Archive Selected.", "JSR Graffiti Tool");
            }
        }
        private void CloseClick(object sender, EventArgs e)
        {
            this.Close();
        }
        private void AboutClick(object sender, EventArgs e)
        {
            MessageBox.Show("Jet Set Radio Graffiti Tool\nBy ChrisDerWahre / CDW_Dev Copyright 2019\nThis Tool is licensed under the MIT license.\n\nCredits:\nimage2graffiti (licensed under the MIT license).", "JSR Graffiti Tool");
        }
        private void TwitterClick(object sender, EventArgs e)
        {
            Process.Start("https://twitter.com/cdw_dev");
        }
        private void ReportAnIssueClick(object sender, EventArgs e)
        {
            Process.Start("https://github.com/chrisderwahre/JSRGraffitiTool/issues");
        }
    }
}
