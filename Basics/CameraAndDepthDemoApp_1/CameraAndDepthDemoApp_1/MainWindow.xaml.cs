using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace CameraAndDepthDemoApp_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        KinectSensor connectedKinectSensor;
        private double minDepthDistanceKinectDepthFrame = 1400;
        private double maxDepthDistanceKinectDepthFrame = 1500;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectKinectSensor();
        }

        void ConnectKinectSensor()
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                connectedKinectSensor = KinectSensor.KinectSensors[0];
                if (connectedKinectSensor.Status == KinectStatus.Connected)
                {
                    connectedKinectSensor.ColorStream.Enable();
                    connectedKinectSensor.DepthStream.Enable();
                    connectedKinectSensor.SkeletonStream.Enable();
                    connectedKinectSensor.Start();

                    Console.WriteLine("Kinect setup completed ID = {0}", connectedKinectSensor.UniqueKinectId);


                    connectedKinectSensor.AllFramesReady += ConnectedKinectSensor_AllFramesReady;

                }
            }
        }

        private void ConnectedKinectSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e) //This function is called whenever the event fires
        {
            //throw new NotImplementedException();
            using (ColorImageFrame kinectImageFrame = e.OpenColorImageFrame())  //Get color frame data
            {
                if (kinectImageFrame == null)   //To handle the NullReferenceException that occurs due to the frame rates and refreshing pixel data
                {
                    return;
                }
                byte[] colorImageFramePixelData = new byte[kinectImageFrame.PixelDataLength];   //Byte array of the pixel data
                kinectImageFrame.CopyPixelDataTo(colorImageFramePixelData); //Copy pixel data to this array

                int stride = kinectImageFrame.Width * 4;

                colorImage_ConnectedKinectSensor.Source = BitmapSource.Create(
                    pixelWidth: kinectImageFrame.Width,
                    pixelHeight: kinectImageFrame.Height,
                    dpiX: 94,
                    dpiY: 94,
                    pixelFormat: PixelFormats.Bgr32,
                    palette: null,
                    pixels: colorImageFramePixelData,
                    stride: stride
                    );  //This creates the Bitmap source of image from the pixel data in the array
                //Now the source has been contained
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) //To prevent null pointer exception
                {
                    return;
                }

                byte[] depthImageFramePixels = GenerateDepthImageFramePixels(depthFrame);

                int stride = depthFrame.Width * 4;  //BGRN format
                depthImage_ConnectedKinectSensor.Source = BitmapSource.Create(
                    pixelWidth: depthFrame.Width,
                    pixelHeight: depthFrame.Height,
                    dpiX: 96,
                    dpiY: 96,
                    pixelFormat: PixelFormats.Bgr32,
                    palette: null,
                    pixels: depthImageFramePixels,
                    stride: stride
                    );
            }
        }

        byte[] GenerateDepthImageFramePixels(DepthImageFrame depthFrame)
        {
            //Get the depth points in a short[]
            short[] rawDepthValues = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthValues);

            byte[] imageFramePixels = new byte[depthFrame.Width * depthFrame.Height * 4];   //Every point has 4 properties (bytes) in BGR32 format

            const int RedComponentIndex = 2, GreenComponentIndex = 1, BlueComponentIndex = 0;  //BGR format, don't care about N

            for (int depthIndex = 0, colorPixelIndex = 0; depthIndex < rawDepthValues.Length; depthIndex += 1, colorPixelIndex += 4)
            {
                //Get the depth from the raw values using the formula
                int depth = rawDepthValues[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth > maxDepthDistanceKinectDepthFrame || depth < minDepthDistanceKinectDepthFrame)   //Pont outside of range
                {   //Make it black
                    imageFramePixels[colorPixelIndex + BlueComponentIndex] = imageFramePixels[colorPixelIndex + GreenComponentIndex] = imageFramePixels[colorPixelIndex + RedComponentIndex] = 0;
                }
                else   //Point inside the range
                {   //Make it white 
                    imageFramePixels[colorPixelIndex + BlueComponentIndex] = imageFramePixels[colorPixelIndex + GreenComponentIndex] = imageFramePixels[colorPixelIndex + RedComponentIndex] = 255;
                }
            }

            return imageFramePixels;
        }

        void DisconnectKinect(KinectSensor sensor)
        {
            sensor.Stop();
            sensor.AudioSource.Stop();
            Console.WriteLine("Kinect sensor with ID = {0} has been disconnected", sensor.UniqueKinectId);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectKinect(connectedKinectSensor);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectedKinectSensor.ElevationAngle = Convert.ToInt32(adjustTiltTextBox.Text);
            }
            catch (Exception excep)
            {
                //Do nothing...
            }
        }

        private void adjustDistancesButton_Click(object sender, RoutedEventArgs e)
        {
            minDepthDistanceKinectDepthFrame = Convert.ToDouble(minDistanceTextBox.Text);
            minDistanceLabel.Content = "The minimum distance is " + minDepthDistanceKinectDepthFrame.ToString();
            maxDepthDistanceKinectDepthFrame = Convert.ToDouble(maxDistanceTextBox.Text);
            maxDistanceLabel.Content = "The maximum distance is " + maxDepthDistanceKinectDepthFrame.ToString();
        }

        private void minDistanceLabel_Loaded(object sender, RoutedEventArgs e)
        {
            minDistanceLabel.Content = "The minimum distance is " + minDepthDistanceKinectDepthFrame.ToString();
        }

        private void maxDistanceLabel_Loaded(object sender, RoutedEventArgs e)
        {
            maxDistanceLabel.Content = "The maximum distance is " + maxDepthDistanceKinectDepthFrame.ToString();
        }
    }
}
