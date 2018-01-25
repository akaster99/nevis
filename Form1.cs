using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Media;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Luxand;

namespace Nevis
{
    public partial class Form1 : Form
    {
        // program states: whether we recognize faces, or user has clicked a face
        enum ProgramState { psRemember, psRecognize }
        ProgramState programState = ProgramState.psRecognize;

        String cameraName;
        bool needClose = false;
        string userName;
        String TrackerMemoryFile = "tracker.dat";
        int mouseX = 0;
        int mouseY = 0;
        String saved_name = "Nevis";

        // WinAPI procedure to release HBITMAP handles returned by FSDKCam.GrabFrame
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        public Form1()
        {
            InitializeComponent();
            button1.MouseEnter += OnMouseEnterButton1;
            button1.MouseLeave += OnMouseLeaveButton1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary("NfCVICqstH2sPcAn6lwNMiUb8SWIK7tX2ovYRPGNYdx+v55mC6uxKvRTfnCcP8M02cV4AAitelp8pN/MNp9/5JYmgk55CYH2nIf5QbYOfnRH3xHrQ+x9VbTKoSIvYFh4JVeS/gZ8X0/YNPPfEzqnNEBa27M1mEc56V1E85KYTCY="))
            {
                MessageBox.Show("Please run the License Key Wizard (Start - Luxand - FaceSDK - License Key Wizard)", "Error activating FaceSDK", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            FSDK.InitializeLibrary();
            FSDKCam.InitializeCapturing();

            string[] cameraList;
            int count;
            FSDKCam.GetCameraList(out cameraList, out count);

            if (0 == count)
            {
                MessageBox.Show("Please attach a camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            cameraName = cameraList[0];

            FSDKCam.VideoFormatInfo[] formatList;
            FSDKCam.GetVideoFormatList(ref cameraName, out formatList, out count);
            this.pictureBox1.Hide();
            this.webBrowser1.Hide();
            this.panel1.Show();
            this.button1.Show();
            this.panel2.Hide();

            button1.Parent = pictureBox2;
            button1.BackColor = Color.Transparent;
            button1.BringToFront();


        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            needClose = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Hide();
            this.panel2.Show();
            this.pictureBox1.Show();
            this.webBrowser1.Show();
            


            int cameraHandle = 0;

            int r = FSDKCam.OpenVideoCamera(ref cameraName, ref cameraHandle);
            if (r != FSDK.FSDKE_OK)
            {
                MessageBox.Show("Error opening the first camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            int tracker = 0; 	// creating a Tracker
            if (FSDK.FSDKE_OK != FSDK.LoadTrackerMemoryFromFile(ref tracker, TrackerMemoryFile)) // try to load saved tracker state
                FSDK.CreateTracker(ref tracker); // if could not be loaded, create a new tracker

            int err = 0; // set realtime face detection parameters
            FSDK.SetTrackerMultipleParameters(tracker, "HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=100; FaceDetectionThreshold=5;", ref err);

            while (!needClose)
            {
                Int32 imageHandle = 0;
                if (FSDK.FSDKE_OK != FSDKCam.GrabFrame(cameraHandle, ref imageHandle)) // grab the current frame from the camera
                {
                    Application.DoEvents();
                    continue;
                }
                FSDK.CImage image = new FSDK.CImage(imageHandle);

                long[] IDs;
                long faceCount = 0;
                FSDK.FeedFrame(tracker, 0, image.ImageHandle, ref faceCount, out IDs, sizeof(long) * 256); // maximum of 256 faces detected
                Array.Resize(ref IDs, (int)faceCount);

                // make UI controls accessible (to find if the user clicked on a face)
                Application.DoEvents();

                Image frameImage = image.ToCLRImage();
                Graphics gr = Graphics.FromImage(frameImage);

                for (int i = 0; i < IDs.Length; ++i)
                {
                    FSDK.TFacePosition facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(tracker, 0, IDs[i], ref facePosition);

                    int left = facePosition.xc - (int)(facePosition.w * 0.6);
                    int top = facePosition.yc - (int)(facePosition.w * 0.5);
                    int w = (int)(facePosition.w * 1.2);

                    String name = null;
                    String address = "sunrinwiki.layer7.kr/index.php/";
                    String new_address;

                    int res = FSDK.GetAllNames(tracker, IDs[i], out name, 65536); // maximum of 65536 characters

                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    { // draw name
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;

                        gr.DrawString(name, new System.Drawing.Font("Arial", 16),

                            new System.Drawing.SolidBrush(System.Drawing.Color.LightGreen),
                            facePosition.xc, top + w + 5, format);

                        if ((saved_name != name) && (name != null))
                        {
                            new_address = address + name;
                            webBrowser1.Navigate(new_address);
                            saved_name = name;
                        }




                    }

                    Pen pen = Pens.LightGreen;
                    if (mouseX >= left && mouseX <= left + w && mouseY >= top && mouseY <= top + w)
                    {
                        pen = Pens.Blue;
                        if (ProgramState.psRemember == programState)
                        {
                            if (FSDK.FSDKE_OK == FSDK.LockID(tracker, IDs[i]))
                            {
                                // get the user name
                                InputName inputName = new InputName();
                                if (DialogResult.OK == inputName.ShowDialog())
                                {
                                    userName = inputName.userName;
                                    if (userName == null || userName.Length <= 0)
                                    {
                                        String s = "";
                                        FSDK.SetName(tracker, IDs[i], "");
                                        FSDK.PurgeID(tracker, IDs[i]);
                                    }
                                    else
                                    {
                                        FSDK.SetName(tracker, IDs[i], userName);
                                    }
                                    FSDK.UnlockID(tracker, IDs[i]);
                                }
                            }
                        }
                    }
                    gr.DrawRectangle(pen, left, top, w, w);
                }
                programState = ProgramState.psRecognize;

                // display current frame
                pictureBox1.Image = frameImage;
                GC.Collect(); // collect the garbage after the deletion

            }
            FSDK.SaveTrackerMemoryToFile(tracker, TrackerMemoryFile);
            FSDK.FreeTracker(tracker);

            FSDKCam.CloseVideoCamera(cameraHandle);
            FSDKCam.FinalizeCapturing();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            programState = ProgramState.psRemember;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            mouseX = 0;
            mouseY = 0;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void OnMouseEnterButton1(object sender, EventArgs e)
        {
            this.button1.BackgroundImage = buttons.Images[1];
        }
        private void OnMouseLeaveButton1(object sender, EventArgs e)
        {
            this.button1.BackgroundImage = buttons.Images[0];

        }
        private void playbgm()
        {
            SoundPlayer bgm = new SoundPlayer(@"../theme.mp3");
            bgm.Play();
        }

    }
}
