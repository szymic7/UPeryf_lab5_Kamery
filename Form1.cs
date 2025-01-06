using Accord.Video.FFMPEG;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UPeryf_lab5_kamery_v2
{
    public partial class Form1 : Form
    {
        Bitmap frame;
        VideoCaptureDevice capturedDevice;
        FilterInfoCollection videoDeviceList;
        VideoFileWriter videoFileWriter;

        HueModifier hueFilter = new HueModifier(0);
        SaturationCorrection satFilter = new SaturationCorrection(0f);

        string defaultPhotoPath = @"./screenshots/";
        string defaultFilmPath = @"./videos/";

        int photoCount = 0;
        int filmCount = 0;

        int frameRate = 30;

        bool isRecording = false;

        Bitmap previousFrame = null;
        double movementThreshold = 0.05;


        public Form1()
        {
            try
            {
                if (!Directory.Exists("./screenshots/"))
                    Directory.CreateDirectory("./screenshots");

                if (!Directory.Exists("./videos/"))
                    Directory.CreateDirectory("./videos");

            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }

            InitializeComponent();
            videoDeviceList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDeviceList)
            {
                comboBox1.Items.Add(videoDevice.Name);
            }
            capturedDevice = new VideoCaptureDevice();
            comboBox1.SelectedIndex = 0;
        }


        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap originalFrame = null;
            Bitmap frameToWrite = null;

            try
            {
                // Clone the current frame from the event
                originalFrame = (Bitmap)eventArgs.Frame.Clone();

                frame?.Dispose();
                frame = originalFrame;

                // Apply filters
                frame = hueFilter.Apply(frame);
                frame = satFilter.Apply(frame);

                // Movement detection
                if (previousFrame != null)
                {
                    bool movementDetected = DetectMovement(previousFrame, frame);

                    if (movementDetected)
                    {
                        Console.WriteLine("Movement detected!");
                    }
                }

                // Update the previous frame
                previousFrame?.Dispose();
                previousFrame = (Bitmap)frame.Clone();

                // Update the PictureBox
                pictureBox1.Image?.Dispose();
                pictureBox1.Image = frame;

                if (isRecording)
                {
                    frameToWrite = (Bitmap)frame.Clone();
                    videoFileWriter.WriteVideoFrame(frameToWrite);
                }
            }
            finally
            {
                originalFrame?.Dispose();
                frameToWrite?.Dispose();
            }


        }


        private bool DetectMovement(Bitmap previousFrame, Bitmap currentFrame)
        {
            if (previousFrame == null || currentFrame == null)
                return false;

            // Ensure both frames have the same dimensions
            if (previousFrame.Width != currentFrame.Width || previousFrame.Height != currentFrame.Height)
                return false;

            int width = currentFrame.Width;
            int height = currentFrame.Height;
            int pixelCount = width * height;
            int changedPixels = 0;

            // Compare pixel data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color prevColor = previousFrame.GetPixel(x, y);
                    Color currColor = currentFrame.GetPixel(x, y);

                    // Compute the difference between RGB values
                    int diffR = Math.Abs(prevColor.R - currColor.R);
                    int diffG = Math.Abs(prevColor.G - currColor.G);
                    int diffB = Math.Abs(prevColor.B - currColor.B);

                    // If the difference is significant (> 30), count the pixel as changed
                    if (diffR > 30 || diffG > 30 || diffB > 30)
                    {
                        changedPixels++;
                    }
                }
            }

            // Calculate the percentage of changed pixels
            double movementPercentage = (double)changedPixels / pixelCount;

            // Return true if the movement exceeds the threshold
            return movementPercentage > movementThreshold;
        }


        private void buttonStart_Click(object sender, EventArgs e)
        {
            capturedDevice = new VideoCaptureDevice(videoDeviceList[comboBox1.SelectedIndex].MonikerString);
            capturedDevice.NewFrame += new NewFrameEventHandler(video_NewFrame);
            capturedDevice.Start() ;
        }

        


        private void buttonScreenshot_Click(object sender, EventArgs e)
        {
            var path = defaultPhotoPath + photoCount.ToString() + ".bmp";
            photoCount++;

            
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Bitmap imageToSave = (Bitmap)pictureBox1.Image.Clone();
            imageToSave.Save(path);

        }


        private void buttonRecord_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("No image available to record. Start the camera first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var path = defaultFilmPath + filmCount.ToString() + ".avi";
            filmCount++;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            videoFileWriter = new VideoFileWriter();
            videoFileWriter.Open(path, pictureBox1.Image.Width, pictureBox1.Image.Height, frameRate, VideoCodec.MPEG4);
            isRecording = true;
        }


        private void buttonStop_Click(object sender, EventArgs e)
        {
            videoFileWriter.Close();
            isRecording = false;
        }



        private void trackBarSaturation_Scroll(object sender, EventArgs e)
        {
            satFilter.AdjustValue = (float)trackBarSaturation.Value / 100;
        }


        private void trackBarHue_Scroll(object sender, EventArgs e)
        {
            hueFilter.Hue = trackBarHue.Value;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the video capture device
            if (capturedDevice != null && capturedDevice.IsRunning)
            {
                capturedDevice.SignalToStop();
                capturedDevice.WaitForStop();
                capturedDevice = null;
            }

            // Dispose the VideoFileWriter if recording
            if (isRecording)
            {
                videoFileWriter.Close();
                videoFileWriter.Dispose();
                isRecording = false;
            }

            // Dispose PictureBox image
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

        }


    }

}
