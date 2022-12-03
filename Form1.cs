using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using static GKProj3.ErrorDiffusionColorReducer;

namespace GKProj3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }

        private void changeImageBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Image";
                dlg.Filter = "Image files |*.bmp;*.png;*.jpg";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if(mainPictureBox.Image is not null) mainPictureBox.Image.Dispose();
                    if (propagationPictureBox.Image is not null) propagationPictureBox.Image.Dispose();
                    if (popularityPictureBox.Image is not null) popularityPictureBox.Image.Dispose();
                    if (kmeansPictureBox.Image is not null) kmeansPictureBox.Image.Dispose();
                    
                    mainPictureBox.Image = Image.FromFile(dlg.FileName);
                }
            }
        }

        private void clusterImageBtn_Click(object sender, EventArgs e)
        {
            ErrorDiffusionColorReducer.Modes mode;
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    mode = ErrorDiffusionColorReducer.Modes.FloydSteinberg;
                    break;
                case 1:
                    mode = ErrorDiffusionColorReducer.Modes.Burkes;
                    break;
                case 2:
                    mode = ErrorDiffusionColorReducer.Modes.Stucky;
                    break;
                default:
                    throw new Exception("wrong filter mode selected");
            }


            int colorN = colorsTrackBar.Value;
            int eps = epsilonTrackBar.Value;
            Bitmap image1 = (Bitmap)mainPictureBox.Image.Clone();
            Bitmap image2 = (Bitmap)mainPictureBox.Image.Clone();
            Bitmap image3 = (Bitmap)mainPictureBox.Image.Clone();
            Task task1 = Task.Run(() => PerformErrorReduction(image1, mode, (int)rNumeric.Value, (int)gNumeric.Value, (int)bNumeric.Value));
            Task task2 = Task.Run(() => PerformPopularityReduction(image2, colorN));
            Task task3 = Task.Run(() => PerformKmeansReduction(image3, colorN, eps));
        }

        private void PerformErrorReduction(Bitmap image, Modes mode, int rK, int gK, int bK)
        {
            ErrorDiffusionColorReducer cr = new ErrorDiffusionColorReducer(image, mode);
            propagationPictureBox.Image = cr.Reduce(rK, gK, bK);
        }

        private void PerformPopularityReduction(Bitmap image,int colorN)
        {
            PopularityColorReducer cr = new PopularityColorReducer(image);
            popularityPictureBox.Image = cr.Reduce(colorN);
        }        
        private void PerformKmeansReduction(Bitmap image,int colorN, int epsilon)
        {
            KMeansColorReducer cr = new KMeansColorReducer(image,epsilon);
            kmeansPictureBox.Image = cr.Reduce(colorN);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (colorsTrackBar.Value == 1)
                clusterImageBtn.Text = "Cluster image to 1 color";
            else
                clusterImageBtn.Text = $"Cluster image to {colorsTrackBar.Value} colors";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            mainPictureBox.Image = (sender as PictureBox)!.Image;
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            for(int i=0;i<comboBox1.Items.Count;i++)
            {
                if(comboBox1.Text == comboBox1.Items[i].ToString())
                {
                    comboBox1.SelectedIndex = i;
                }
            }
            comboBox1.SelectedIndex = 0;
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            epsilonLabel.Text = $"Epsilon value: {epsilonTrackBar.Value}";
        }
    }
}