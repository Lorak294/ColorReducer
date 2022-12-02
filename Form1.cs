using System.Diagnostics;

namespace GKProj3
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

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
                    mainPictureBox.Image.Dispose();
                    propagationPictureBox.Image.Dispose();
                    propagationPictureBox.Image.Dispose();
                    kmeansPictureBox.Image.Dispose();
                    
                    mainPictureBox.Image = Image.FromFile(dlg.FileName);
                    propagationPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
                    propagationPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
                    kmeansPictureBox.Image = (Bitmap)mainPictureBox.Image.Clone();
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

            ErrorDiffusionColorReducer ecr = new ErrorDiffusionColorReducer((Bitmap)mainPictureBox.Image, mode);

            if(propagationPictureBox.Image != null)propagationPictureBox.Image.Dispose();
            propagationPictureBox.Image = ecr.Reduce((int)rNumeric.Value, (int)gNumeric.Value, (int)bNumeric.Value);



            PopularityColorReducer pcr = new PopularityColorReducer((Bitmap)mainPictureBox.Image);
            if (popularityPictureBox.Image != null) popularityPictureBox.Image.Dispose();
            popularityPictureBox.Image = pcr.Reduce(colorsTrackBar.Value);
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