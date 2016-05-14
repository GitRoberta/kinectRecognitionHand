using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;


namespace RecognitionHand
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime nui = new Runtime();

        private double ScreenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        private double ScreenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
        private Hsv hsv_min;
        private Hsv hsv_max;
        private Ycc YCrCb_max;
        private Ycc YCrCb_min;
        private MCvBox2D box;
        private YCrCbSkinDetector skinDetector;
        public Seq<System.Drawing.Point> hull;
        public Seq<System.Drawing.Point> filteredHull;
        private Seq<MCvConvexityDefect> defects;
        private MCvConvexityDefect[] defectArray;
        private Image<Bgr, byte> _my_image;
        private Image<Gray, Byte> skin;
        private POINT handPosition = new POINT(0, 0);
        private int heightHand = 0, widhtHand = 0;
        private POINT result = new POINT(0, 0);
        private float previousFrameX = 0;
        private float fingerHeight = 5.5f;

        private float multiplierMouseX = 1f;
        private float multiplierMouseY = 1f;

        private MouseController mouseController = new MouseController(15,true);
        private int countAverage = 0;

        /* If true use ycbcr, else hsv */
        public bool useYCbCr = false;
        /* If we already get the color, dont do anything */
        private bool handColorTaken = false;
       

        #region ImportRegion
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int DeleteObject(IntPtr o);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        private void SetPositon(int a, int b)
        {
            if (a >= ScreenWidth)
                a = (int)ScreenWidth;
            if (b >= ScreenHeight)
                b = (int)ScreenHeight;
            SetCursorPos(a, b);
        }
        #endregion
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            heightHand = (int)imageConvexHull.Height;
            widhtHand = (int)imageConvexHull.Width;

            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseSkeletalTracking);
            nui.VideoFrameReady += Nui_VideoFrameReady;
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);

            //Parameters of Skeleton Tracking
            #region SmoothTransform
            nui.SkeletonEngine.TransformSmooth = true;
            var parameters = new TransformSmoothParameters { Smoothing = 0.75f, Correction = 0.0f,
                Prediction = 0.0f, JitterRadius = 0.05f, MaxDeviationRadius = 0.04f };
            nui.SkeletonEngine.SmoothParameters = parameters;
            #endregion

            nui.SkeletonFrameReady += Nui_skeleton_SkeletonFrameReady;

            #region HandRecognitionInit
            
            //hsv_min = new Hsv(10, 45, 50);
            //hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
            //Parameter Test
            hsv_min = new Hsv(11, 27, 94);
            hsv_max = new Hsv(14, 255, 197);

            Actual_HSV_H.Content = "Min =" + hsv_min.Hue.ToString() + " Max=" + hsv_max.Hue.ToString();
            ACTUAL_HSV_S.Content= "Min =" + hsv_min.Satuation.ToString() + " Max=" + hsv_max.Satuation.ToString();
            ACTUAL_HSV_V.Content = "Min =" + hsv_min.Value.ToString() + " Max=" + hsv_max.Value.ToString();

            hsv_hue_min_slider.Value = hsv_min.Hue;
            hsv_min_sat_slider.Value = hsv_min.Satuation;
            hsv_min_value_slider.Value = hsv_min.Value;

            hsv_max_hue_slider.Value = hsv_max.Hue;
            hsv_max_saturation_slider.Value = hsv_max.Satuation;
            hsv_max_value_slider.Value = hsv_max.Value;

            sliderSensitivityX.Value = 400;
            sliderSensitivityY.Value = 400;


            mouseController.ReleaseClick();
            box = new MCvBox2D();
            #endregion
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
            mouseController.ReleaseClick();
        }

        private void Nui_skeleton_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame allSkeletons = e.SkeletonFrame;
            SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
            if (skeleton != null)
                calculateJointPosition(skeleton.Joints[JointID.HandRight]);    
        }
        
        private void calculateJointPosition(Joint joint) {
            var scaledJoint = joint.ScaleTo((int)ScreenWidth, (int)ScreenHeight, 1,1);
            handPosition = new POINT((int)scaledJoint.Position.X, (int)scaledJoint.Position.Y);

            int correctX = (int)scaledJoint.Position.X;
            int correctY = (int)scaledJoint.Position.Y;

            /* Map point and interpolate for a smoothing transition */
            POINT p;
            GetCursorPos(out p);
            int x = (int)((correctX + p.X) / 2);
            int y = (int)((correctY + p.Y) / 2);


            if (x < ScreenWidth / 2)
            {
                x = ((x - 200) > 0) ? x - 200 : 0;
            }
            else {
                x = ((x + 200) < ScreenWidth) ? x + 200 : (int) ScreenWidth;
            }

            if (y >= ScreenHeight / 2) {
               y= ((y + 100) > 0) ? y + 100 : (int) ScreenHeight;
            }
            else {
                y = ((y - 100) > 0) ? y - 100 : 0;
            }
            
           
            if(handColorTaken)
                mouseController.MoveMouse(scaledJoint);

        }

        private POINT calculateRelativeHandPosition(int resolutionX = 640, int resolutionY = 480) {

            int x= (int)((handPosition.X * resolutionX) / ScreenWidth);
            int y= (int)((handPosition.Y * resolutionY) / ScreenHeight);

            if (!(x + widhtHand > resolutionX || x - widhtHand < 0))
            {
                /*  was before (int)((handPosition.X * resolutionX) / ScreenWidth)*/
                result.X = x;
                result.X = ((result.X - (widhtHand/2) > 0)) ? result.X - (widhtHand / 2) : 0;

                /* Needs for avoiding frame flikering */
                if (Math.Abs(result.X - previousFrameX) > 30 && previousFrameX != 0)
                    result.X = (int)previousFrameX;
            }

            /* Cannot move if overflow happen */
            if (!(y + heightHand > resolutionY || y - heightHand < 0))
            {
                result.Y = y;
                result.Y = ((result.Y - (heightHand/2) > 0)) ? result.Y - (heightHand / 2) : 0;
            }

            previousFrameX = result.X;
            return result;
        }
        
        private void Nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            Image<Bgr, byte> subImg=null;
            try {
                imageBGR.Source = e.ImageFrame.ToBitmapSource(); //--> currentFrame

                #region MetodWithPlanarImage
                if (e.ImageFrame.Image.Equals(null)) return;
                Image<Bgr, byte> _my_image = new Image<Bgr, byte>(PImageToBitmap(e.ImageFrame.Image));
                #endregion
                
                if (_my_image == null) return;

                POINT actual_hand_position = new POINT(0, 0);
                actual_hand_position.X = (int)((handPosition.X * 640) / ScreenWidth);
                actual_hand_position.Y = (int)((handPosition.Y * 480) / ScreenHeight);

                subImg = new Image<Bgr, byte>(widhtHand, heightHand);
                calculateRelativeHandPosition();

                /* Take the ROI FROM THE SOURCE IMAGE */
                _my_image.ROI = new Rectangle(result.X, result.Y, widhtHand, heightHand);
                CvInvoke.cvCopy(_my_image, subImg, IntPtr.Zero);

                takeSkin(subImg);
                if (handColorTaken)
                    ExtractContourAndHull(skin, subImg);
            }
            catch (Exception eex)
            {
                Console.WriteLine("Exception " + eex.ToString());
                DeleteObject(subImg);
                DeleteObject(_my_image);
            }

            DeleteObject(subImg);
            DeleteObject(_my_image);
        }

        #region PrincipalMethods

        private void takeSkin(Image<Bgr, byte> my_image) {
            HsvSkinDetector skin_detector=null;
            if (useYCbCr)
                skinDetector = new YCrCbSkinDetector();
            else
                skin_detector = new HsvSkinDetector();
            skin = (useYCbCr)? skinDetector.DetectSkin(my_image, YCrCb_min, YCrCb_max):skin_detector.DetectSkin(my_image, hsv_min, hsv_max);
            
            imageConvexHull.Source = toBitmapSourceFromImage(skin);
        }

        private void ExtractContourAndHull(Image<Gray, byte> skin, Image<Bgr,byte> currentFrame)
        {
            using (MemStorage storage = new MemStorage())
            {
                Contour<System.Drawing.Point> contours = skin.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);
                Contour<System.Drawing.Point> biggestContour = null;

                Double Result1 = 0;
                Double Result2 = 0;
                while (contours != null)
                {
                    Result1 = contours.Area;
                    if (Result1 > Result2)
                    {
                        Result2 = Result1;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }

                if (biggestContour != null)
                {
                    currentFrame.Draw(biggestContour, new Bgr(Color.DarkViolet), 2);
                    Contour<System.Drawing.Point> currentContour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                    currentFrame.Draw(currentContour, new Bgr(System.Drawing.Color.LimeGreen), 2);
                    biggestContour = currentContour;                    
                    hull = biggestContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    box = biggestContour.GetMinAreaRect();
                    PointF[] points = box.GetVertices();
                    //handRect = box.MinAreaRect();
                    //currentFrame.Draw(handRect, new Bgr(200, 0, 0), 1);
                    System.Drawing.Point[] ps = new System.Drawing.Point[points.Length];
                    for (int i = 0; i < points.Length; i++) {
                        ps[i] = new System.Drawing.Point((int)points[i].X, (int)points[i].Y);
                    }
                    currentFrame.DrawPolyline(hull.ToArray(), true, new Bgr(200, 125, 75), 2);
                    currentFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Bgr(200, 125, 75), 2);
                    filteredHull = new Seq<System.Drawing.Point>(storage);
                    for (int i = 0; i < hull.Total; i++)
                    {
                        if (Math.Sqrt(Math.Pow(hull[i].X - hull[i + 1].X, 2) + Math.Pow(hull[i].Y - hull[i + 1].Y, 2)) > box.size.Width / 10)
                        {
                            filteredHull.Push(hull[i]);
                        }
                    }
                    defects = biggestContour.GetConvexityDefacts(storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    defectArray = defects.ToArray();
                    DrawAndComputeFingersNum(currentFrame,filteredHull,defects,defectArray);
                }
                imageConvexHull.Source = toBitmapSourceFromImage(currentFrame);
            }
        }

        private void DrawAndComputeFingersNum(Image<Bgr, byte> currentFrame, Seq<System.Drawing.Point> filteredHull, Seq<MCvConvexityDefect> defects, MCvConvexityDefect[] defectArray)
        {
            int fingerNum = 0;

            #region hull drawing
            //for (int i = 0; i < filteredHull.Total; i++)
            //{
            //    PointF hullPoint = new PointF((float)filteredHull[i].X,
            //                                  (float)filteredHull[i].Y);
            //    CircleF hullCircle = new CircleF(hullPoint, 4);
            //    currentFrame.Draw(hullCircle, new Bgr(Color.Aquamarine), 2);
            //}
            #endregion

            #region defects drawing
            for (int i = 0; i < defects.Total; i++)
            {
                PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
                                                (float)defectArray[i].StartPoint.Y);

                PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
                                                (float)defectArray[i].DepthPoint.Y);

                //PointF endPoint = new PointF((float)defectArray[i].EndPoint.X,
                //                                (float)defectArray[i].EndPoint.Y);

                //LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);
                //LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);
                CircleF startCircle = new CircleF(startPoint, 5f);
                CircleF depthCircle = new CircleF(depthPoint, 5f);
                //CircleF endCircle = new CircleF(endPoint, 5f);

                //Custom heuristic based on some experiment, double check it before use ; 
                if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > (box.size.Height / fingerHeight)))
                {
                    if (fingerNum < 5)
                    {
                        fingerNum++;                       
                    }
                }
                
                // currentFrame.Draw(startCircle, new Bgr(System.Drawing.Color.Red), 2);
                // currentFrame.Draw(depthCircle, new Bgr(System.Drawing.Color.Yellow), 5);
                // currentFrame.Draw(endCircle, new Bgr(Color.Black), 4);
            }
            #endregion

            mouseController.AddNumber(fingerNum);
           // Console.WriteLine("Detected Finger Number =" + fingerNum);
        }

        #endregion
        
        /* Change Hue Value */
        #region SliderHSV
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_min.Hue = (int)slider.Value;

            if (hsv_max.Hue <= hsv_min.Hue)
            {
                hsv_max.Hue = hsv_min.Hue + 1;
                hsv_max_hue_slider.Value = hsv_max.Hue;
                MAX_HSV_H.Content = hsv_max.Hue;
            }
            if(MIN_HSV_H!=null)
             MIN_HSV_H.Content = (int)slider.Value;
        }

        private void hsv_min_sat_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_min.Satuation = (int)slider.Value;

            if (hsv_max.Satuation <= hsv_min.Satuation)
            {
                hsv_max.Satuation = hsv_min.Satuation + 1;
                hsv_max_saturation_slider.Value = hsv_max.Satuation;
                MAX_HSV_S.Content = hsv_max.Satuation;
            }

            if (MIN_HSV_S != null)
                MIN_HSV_S.Content = (int)slider.Value;
        }

        private void hsv_min_value_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_min.Value = (int)slider.Value;

            if (hsv_max.Value <= hsv_min.Value)
            {
                hsv_max.Value = hsv_min.Value + 1;
                hsv_max_value_slider.Value = hsv_max.Value;
                MAX_HSV_V.Content = hsv_max.Value;
            }

            if (MIN_HSV_V != null)
                MIN_HSV_V.Content = (int)slider.Value;
        }

        private void hsv_max_hue_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_max.Hue = (int)slider.Value;

            if (hsv_max.Hue <= hsv_min.Hue)
            {
                hsv_max.Hue = hsv_min.Hue + 1;
                slider.Value = hsv_max.Hue;
            }

            if(MAX_HSV_H!=null)
                MAX_HSV_H.Content = (int)slider.Value;
        }

        private void hsv_max_saturation_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_max.Satuation = (int)slider.Value;

            if (hsv_max.Satuation <= hsv_min.Satuation)
            {
                hsv_max.Satuation = hsv_min.Satuation + 1;
                slider.Value = hsv_max.Satuation;
            }

            if(MAX_HSV_S!=null)
                MAX_HSV_S.Content = (int)slider.Value;
        }

        private void hsv_max_value_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            hsv_max.Value = (int)slider.Value;

            if (hsv_max.Value <= hsv_min.Value)
            {
                hsv_max.Value = hsv_min.Value + 1;
                slider.Value = hsv_max.Value;
            }

            if(MAX_HSV_V!=null)
                MAX_HSV_V.Content =(int) slider.Value;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            handColorTaken = !handColorTaken;
            Button b = sender as Button;
            b.Content = (handColorTaken) ? "Stop" : "Start";
            SetPositon((int)ScreenWidth/2,(int)ScreenHeight/2);
        }

        private void sliderSensitivityX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            multiplierMouseX = (float)slider.Value;
            if (mouseController != null)
                mouseController.SensitivityX = multiplierMouseX/100;

            SensitivityX.Content = multiplierMouseX.ToString();
        }

        private void sliderSensitivityY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            multiplierMouseY = (float)slider.Value;
            if (mouseController != null)
                mouseController.SensitivityY = multiplierMouseY/100;
            SensitivityY.Content = multiplierMouseY.ToString();
        }

        #endregion

        #region UsefulMethods
        /* Take a image and return a bitmapsource for building Image<> for Emgucv*/
        public static BitmapSource toBitmapSourceFromImage(IImage img)
        {
            using (System.Drawing.Bitmap source = img.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();
                BitmapSource bitsrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(ptr);
                return bitsrc;
            }
        }

        Bitmap PImageToBitmap(PlanarImage PImage)
        {
            Bitmap bmap = new Bitmap(PImage.Width, PImage.Height, PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(
            new Rectangle(0, 0, PImage.Width, PImage.Height), ImageLockMode.WriteOnly, bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(PImage.Bits,
            0,
            ptr,
            PImage.Width *
            PImage.BytesPerPixel *
            PImage.Height);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }

        
        #endregion UsefulMethods

    }
}
