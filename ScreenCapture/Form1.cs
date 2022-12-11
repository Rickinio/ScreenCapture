using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenCapture
{
    class NativeUtilities
    {
        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>This is the primary display.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x16,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        public const int ENUM_CURRENT_SETTINGS = -1;
        const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("User32.dll")]
        public static extern int EnumDisplayDevices(string lpDevice, int iDevNum, ref DisplayDevice lpDisplayDevice, int dwFlags);
    }

    public partial class Form1 : Form
    {
        private Size captureSize;
        private Bitmap screenCapture;
        private Image bgImage;
        private bool startCapture;
        private int startMouseX;
        private int startMouseY;
        private int mouseX;
        private int mouseY;
        private Rectangle capturedRectangle;
        public Form1()
        {
            InitializeComponent();
            this.Hide();

            screenCapture = ScreenCapture();
            captureSize = screenCapture.Size;


            this.StartPosition = FormStartPosition.Manual;
            this.Location = GetOverallDesktopRectangle().Location;
            this.Size = captureSize;

            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;


            using (MemoryStream s = new MemoryStream())
            {
                //save graphic variable into memory
                screenCapture.Save(s, ImageFormat.Bmp);
                bgImage = Image.FromStream(s);
                pictureBox1.Size = captureSize;
                //set the picture box with temporary stream
                pictureBox1.Image = Image.FromStream(s);
            }
            //Show Form
            this.Show();
            //Cross Cursor
            Cursor = Cursors.Cross;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {


            pictureBox1.Invalidate();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            startMouseX = e.X;
            startMouseY = e.Y;
            startCapture = true;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            startCapture = false;


            try
            {
                var newImage = new Bitmap(capturedRectangle.Width, capturedRectangle.Height);
                var a = Graphics.FromImage(newImage);
                a.DrawImage(pictureBox1.Image, 0, 0, capturedRectangle, GraphicsUnit.Pixel);

                newImage.Save("C:\\Ricky\\selected.bmp");
                a.Dispose();
                newImage.Dispose();
            }
            catch (Exception ex)
            {
                var a = ex;
            }
            finally
            {
                this.Close();
            }


        }

        private void PictureBox1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var canvas = e.Graphics;

            if (startCapture)
            {
                capturedRectangle = new Rectangle(new Point(startMouseX, startMouseY),
                    new Size(mouseX - startMouseX, mouseY - startMouseY));
                e.Graphics.DrawRectangle(new Pen(Color.Red, 2), capturedRectangle);
            }
        }

        public Bitmap ScreenCapture()
        {
            // Initialize the virtual screen to dummy values
            int screenLeft = int.MaxValue;
            int screenTop = int.MaxValue;
            int screenRight = int.MinValue;
            int screenBottom = int.MinValue;

            // Enumerate system display devices
            int deviceIndex = 0;
            while (true)
            {
                NativeUtilities.DisplayDevice deviceData = new NativeUtilities.DisplayDevice { cb = Marshal.SizeOf(typeof(NativeUtilities.DisplayDevice)) };
                if (NativeUtilities.EnumDisplayDevices(null, deviceIndex, ref deviceData, 0) != 0)
                {
                    // Get the position and size of this particular display device
                    NativeUtilities.DEVMODE devMode = new NativeUtilities.DEVMODE();
                    if (NativeUtilities.EnumDisplaySettings(deviceData.DeviceName, NativeUtilities.ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        // Update the virtual screen dimensions
                        screenLeft = Math.Min(screenLeft, devMode.dmPositionX);
                        screenTop = Math.Min(screenTop, devMode.dmPositionY);
                        screenRight = Math.Max(screenRight, devMode.dmPositionX + devMode.dmPelsWidth);
                        screenBottom = Math.Max(screenBottom, devMode.dmPositionY + devMode.dmPelsHeight);
                    }
                    deviceIndex++;
                }
                else
                    break;
            }

            // Create a bitmap of the appropriate size to receive the screen-shot.
            Bitmap bmp = new Bitmap(screenRight - screenLeft, screenBottom - screenTop);

            // Draw the screen-shot into our bitmap.
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);

            // Stuff the bitmap into a file
            bmp.Save("C:\\Ricky\\allscreens.bmp", System.Drawing.Imaging.ImageFormat.Png);

            return bmp;

        }

        private static Rectangle GetOverallDesktopRectangle()
        {
            var rect = new Rectangle(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
            return Screen.AllScreens.Aggregate(rect, (current, screen) => Rectangle.Union(current, screen.Bounds));
        }
    }
}
