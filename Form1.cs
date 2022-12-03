using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using static GKProj3.ErrorDiffusionColorReducer;

namespace GKProj3
{
    public partial class Form1 : Form
    {
        Control errorControl, popularityControl, kmeansControl;
        public Form1()
        {
            InitializeComponent();
            errorControl = propagationPictureBox;
            popularityControl = popularityPictureBox;
            kmeansControl = kmeansPictureBox;


            comboBox1.SelectedIndex = 0;

            //propagationPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
            //popularityPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
            //kmeansPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
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

        private async void clusterImageBtn_Click(object sender, EventArgs e)
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

            ErrorDiffusionColorReducer ecr = new ErrorDiffusionColorReducer((Bitmap)mainPictureBox.Image, mode);
            PopularityColorReducer pcr = new PopularityColorReducer((Bitmap)mainPictureBox.Image);
            KMeansColorReducer kcr = new KMeansColorReducer((Bitmap)mainPictureBox.Image, epsilonTrackBar.Value);

            List<Task<Bitmap>> reductionTasks = new List<Task<Bitmap>>();
            reductionTasks.Add(ecr.ReduceAsync((int)rNumeric.Value, (int)gNumeric.Value, (int)bNumeric.Value));
            reductionTasks.Add(pcr.ReduceAsync(colorsTrackBar.Value));
            reductionTasks.Add(kcr.ReduceAsync(colorsTrackBar.Value));

            propagationPictureBox.Image = await reductionTasks[0];
            popularityPictureBox.Image = await reductionTasks[1];
            kmeansPictureBox.Image = await reductionTasks[2];

        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (colorsTrackBar.Value == 1)
                clusterImageBtn.Text = "Cluster image to 1 color";
            else
                clusterImageBtn.Text = $"Cluster image to {colorsTrackBar.Value} colors";


            int singleChannelN = (int)Math.Max(1,Math.Pow(colorsTrackBar.Value, (double)1 / 3));
            
            rNumeric.Value = singleChannelN;
            gNumeric.Value = singleChannelN;
            bNumeric.Value = singleChannelN;
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