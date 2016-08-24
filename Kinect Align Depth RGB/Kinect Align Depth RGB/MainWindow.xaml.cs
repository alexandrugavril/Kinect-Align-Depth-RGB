using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;


namespace Kinect_Align_Depth_RGB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _sensor;
        private WriteableBitmap _bitmap;
        private byte[] _bitmapBits;
        private ColorImagePoint[] _mappedDepthLocations;
        private byte[] _colorPixels = new byte[0];
        private short[] _depthPixels = new short[0];

        private void SetSensor(KinectSensor newSensor)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
            }

            _sensor = newSensor;

            if (_sensor != null)
            {
                Debug.Assert(_sensor.Status == KinectStatus.Connected, "This should only be called with Connected sensors.");
                _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                _sensor.AllFramesReady += _sensor_AllFramesReady;
                _sensor.Start();
            }
        }

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            bool gotColor = false;
            bool gotDepth = false;

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    Debug.Assert(colorFrame.Width == 640 && colorFrame.Height == 480, "This app only uses 640x480.");

                    if (_colorPixels.Length != colorFrame.PixelDataLength)
                    {
                        _colorPixels = new byte[colorFrame.PixelDataLength];
                        _bitmap = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
                        _bitmapBits = new byte[640 * 480 * 4];
                        this.Image.Source = _bitmap;
                    }

                    colorFrame.CopyPixelDataTo(_colorPixels);
                    gotColor = true;
                }
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    Debug.Assert(depthFrame.Width == 640 && depthFrame.Height == 480, "This app only uses 640x480.");

                    if (_depthPixels.Length != depthFrame.PixelDataLength)
                    {
                        _depthPixels = new short[depthFrame.PixelDataLength];
                        _mappedDepthLocations = new ColorImagePoint[depthFrame.PixelDataLength];
                    }

                    depthFrame.CopyPixelDataTo(_depthPixels);
                    gotDepth = true;
                }
            }

            // Put the color image into _bitmapBits
            for (int i = 0; i < _colorPixels.Length; i += 4)
            {
                _bitmapBits[i + 3] = 255;
                _bitmapBits[i + 2] = _colorPixels[i + 2];
                _bitmapBits[i + 1] = _colorPixels[i + 1];
                _bitmapBits[i] = _colorPixels[i];
            }

            this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mappedDepthLocations);

            for (int i = 0; i < _depthPixels.Length; i++)
            {
                int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                ColorImagePoint point = _mappedDepthLocations[i];

            }

            _bitmap.WritePixels(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight), _bitmapBits, _bitmap.PixelWidth * sizeof(int), 0);
        }

        public MainWindow()
        {
            InitializeComponent();

            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == _sensor)
                {
                    if (e.Status != KinectStatus.Connected)
                    {
                        SetSensor(null);
                    }
                }
                else if ((_sensor == null) && (e.Status == KinectStatus.Connected))
                {
                    SetSensor(e.Sensor);
                }
            };

            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    SetSensor(sensor);
                }
            }
        }
    }
}