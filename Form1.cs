using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiceConverter
{
    public partial class Form1 : Form
    {
        private Bitmap[] dice = new Bitmap[7];
        public Form1()
        {
            InitializeComponent();
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
               });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
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

        public double getD(int x)
        {
            return Math.Round((double) x/6, 2);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int maxDice = 0;
            if(tbMaxDice.Text != "")
            {
                try
                {
                    maxDice = int.Parse(tbMaxDice.Text);
                    if (maxDice < 1) maxDice = 0;
                }
                catch(Exception)
                {
                    MessageBox.Show("Invalid Max Dice Value", "Invalid MaxDice Value :c");
                    return;
                }
            }
            string topText = this.Text;

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.RestoreDirectory = true;
            string fileNameNoExt = "";
            string filePath = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var path = openFileDialog1.FileName;
                fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(path);
                filePath = System.IO.Path.GetDirectoryName(path);
                tbPath.Text = path;
            }
            else
            {
                return;
            }
            await Task.Run(() =>
            {
                try
                {
                    this.Text = topText + " - Converting image to grayscale...";
                    Bitmap bm = (Bitmap)Bitmap.FromFile(tbPath.Text);
                    if(maxDice != 0)
                    {
                        Size original = new Size(bm.Width, bm.Height);
                        int maxSize = (int) Math.Floor(Math.Sqrt(maxDice));
                        float percent = (new List<float> { (float)maxSize / (float)original.Width, (float)maxSize / (float)original.Height }).Min();
                        Size resultSize = new Size((int)Math.Floor(original.Width * percent), (int)Math.Floor(original.Height * percent));
                        if(resultSize.Width <= bm.Width && resultSize.Height <= bm.Height)
                            bm = ResizeImage((Image) bm, resultSize.Width, resultSize.Height);
                    }
                    Bitmap d = MakeGrayscale3(bm);
                    pictureBox1.Image = d;
                    this.Text = topText + " - Generating DiceMap...";
                    string diceMap = "";
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath + "\\" + fileNameNoExt + ".txt"))
                    {
                        for (int x = 0; x < d.Height; x++)
                        {
                            for (int y = 0; y < d.Width; y++)
                            {
                                double brightness = Math.Round(d.GetPixel(y, x).GetBrightness(), 2);
                                if (brightness <= getD(1))
                                    diceMap += "6";
                                else if (brightness > getD(1) && brightness <= getD(2))
                                    diceMap += "5";
                                else if (brightness > getD(2) && brightness <= getD(3))
                                    diceMap += "4";
                                else if (brightness > getD(3) && brightness <= getD(4))
                                    diceMap += "3";
                                else if (brightness > getD(4) && brightness <= getD(5))
                                    diceMap += "2";
                                else if (brightness > getD(5))
                                    diceMap += "1";
                                else
                                    diceMap += "0"; //shouldn't ever happen.*/
                            }
                            this.Text = topText + " - Generating DiceMap... [" + x + "/" + d.Height + "]";
                            file.WriteLine(diceMap);
                            diceMap = "";
                        }
                        this.Text = topText;
                    }
                    this.Text = topText + " - Generating Preview...";
                    var lines = System.IO.File.ReadAllLines(filePath + "\\" + fileNameNoExt + ".txt");
                    Bitmap preview = new Bitmap(d.Width*40, d.Height*40);
                    int dieWidth = 40;
                    int curLine = 0;
                    using (Graphics g = Graphics.FromImage(preview))
                    {
                        foreach (var line in lines)
                        {
                            for (int x = 0; x < line.Length; x++)
                            {
                                g.DrawImage(dice[int.Parse(line[x].ToString())], dieWidth * x, curLine);
                            }
                            curLine += dieWidth;
                        }
                    }
                    preview.Save(filePath + "\\" + fileNameNoExt + "#DiceImage#.png", ImageFormat.Png);
                    pictureBox1.Image = preview;
                    this.Text = topText;
                    MessageBox.Show("Complete! Dice map written to " + filePath + "\\" + fileNameNoExt + ".txt" + Environment.NewLine + Environment.NewLine + "Dice Required: " + d.Width * d.Height, "Dice Map Complete!");
                }
                catch (ArgumentException err)
                {
                    this.Text = topText;
                    MessageBox.Show("Cannot generate a dice preview for this image because it is too big!" + Environment.NewLine + Environment.NewLine + "Try scaling it down!", "Resize Image for Dice Preview! (Make it smaller!)");
                }
                catch (Exception err)
                {
                    this.Text = topText;
                    MessageBox.Show(err.ToString());
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dice[1] = Properties.Resources.dice1;
            dice[2] = Properties.Resources.dice2;
            dice[3] = Properties.Resources.dice3;
            dice[4] = Properties.Resources.dice4;
            dice[5] = Properties.Resources.dice5;
            dice[6] = Properties.Resources.dice6;
        }
    }
}
