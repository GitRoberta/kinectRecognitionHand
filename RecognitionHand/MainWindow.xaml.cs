using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.Drawing;
using System.ComponentModel;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
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

        private AdaptiveSkinDetector detector;
        private Hsv hsv_min;
        private Hsv hsv_max;
        private Ycc YCrCb_max;
        private Ycc YCrCb_min;
        private MCvBox2D box;
        private Emgu.CV.Structure.Ellipse ellip;
        private YCrCbSkinDetector skinDetector;
        private Seq<System.Drawing.Point> hull;
        private Seq<System.Drawing.Point> filteredHull;
        private Seq<MCvConvexityDefect> defects;
        private MCvConvexityDefect[] defectArray;
        private Image<Bgr, byte> _my_image;
        private Image<Gray, Byte> skin;
        private POINT handPosition = new POINT(0, 0);
        private int heightHand =0, widhtHand = 0;
        private int fattoreDiCorrezione = 0;
        private POINT result = new POINT(0, 0);
       // PlanarImage planarImage;
       // Bitmap bitmapPlanarImage;

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
            heightHand =(int) imageConvexHull.Height;
            widhtHand = (int) imageConvexHull.Width;
            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepth | RuntimeOptions.UseSkeletalTracking);
            nui.VideoFrameReady += Nui_VideoFrameReady;
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution1280x1024, ImageType.Color);
            #region SmoothTransform
            nui.SkeletonEngine.TransformSmooth = true;
            var parameters = new TransformSmoothParameters { Smoothing = 0.75f, Correction = 0.0f,
                Prediction = 0.0f, JitterRadius = 0.05f, MaxDeviationRadius = 0.04f };
            nui.SkeletonEngine.SmoothParameters = parameters;
            #endregion

            nui.SkeletonFrameReady += Nui_skeleton_SkeletonFrameReady;

            #region parteCodiceHandRecognition
            detector = new AdaptiveSkinDetector(1, AdaptiveSkinDetector.MorphingMethod.NONE);
            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
            box = new MCvBox2D();
            ellip = new Emgu.CV.Structure.Ellipse();

            #endregion
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
            var scaledJoint = joint.ScaleTo((int)ScreenWidth, (int)ScreenHeight, 1,  1);
            int correctX = (int) scaledJoint.Position.X * 4;
            int correctY = (int) scaledJoint.Position.Y * 3;
            /* Map point and interpolate for a smoothing transition */
            POINT p;
            GetCursorPos(out p);
            int x = (int)((correctX + p.X) / 2);
            int y = (int)((correctY + p.Y) / 2);
            handPosition = new POINT((int) scaledJoint.Position.X, (int) scaledJoint.Position.Y);
            Console.WriteLine("scaledJoinsz is:" + scaledJoint.Position.Z);
            fattoreDiCorrezione = calculateCorretiveFactor(scaledJoint.Position.Z);
            /* Applying a little threshold... */
            //if (Math.Abs(scaledJoint.Position.X - p.X) > 2.0f || Math.Abs(scaledJoint.Position.Y - p.Y) > 2.0f)
            //{
            //  SetPositon(x, y);
            //}
        }

        private int calculateCorretiveFactor(float depth) {
            int correctiveFactor = (int)(((heightHand/2)*depth)/1.0f);
            return correctiveFactor;
        }

        private POINT calculateRelativeHandPosition(int resolutionX=640, int resolutionY= 480) {
            result.X = (int)((handPosition.X * resolutionX) / ScreenWidth);
            result.Y = (int)((handPosition.Y * resolutionY) / ScreenHeight);
            result.X = ((result.X - fattoreDiCorrezione > 0) && (result.X - fattoreDiCorrezione < resolutionX)) ? result.X - fattoreDiCorrezione : result.X;
            result.Y = ((result.Y - fattoreDiCorrezione > 0) && (result.Y - fattoreDiCorrezione < resolutionY)) ? result.Y - fattoreDiCorrezione : result.Y;
            return result;
           
        }

        #region ConvertPlanarImageToBitmap
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
        #endregion

        private void Nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
          
            imageBGR.Source = e.ImageFrame.ToBitmapSource(); //--> currentFrame
            
            #region MetodWithPlanarImage
            // planarImage  = e.ImageFrame.Image;
            // bitmapPlanarImage = PImageToBitmap(e.ImageFrame.Image);
            if (e.ImageFrame.Image.Equals(null)) return;
            Image<Bgr, byte> _my_image = new Image<Bgr, byte>(PImageToBitmap(e.ImageFrame.Image));
            #endregion
            Image<Bgr, byte> subImg = new Image<Bgr, byte>(widhtHand, heightHand);
            calculateRelativeHandPosition(1280,1024);
            _my_image.ROI = new Rectangle(result.X, result.Y, widhtHand, heightHand);


            CvInvoke.cvCopy(_my_image, subImg, IntPtr.Zero);
            //imageConvexHull.Source = toBitmapSourceFromImage(subImg);
            takeSkin(subImg);
            ExtractContourAndHull(skin, subImg);
            //DrawAndComputeFingersNum(_my_image);
            DeleteObject(subImg);
            DeleteObject(_my_image);
        }

        private void takeSkin(Image<Bgr, byte> my_image) {
            skinDetector = new YCrCbSkinDetector();
            skin = skinDetector.DetectSkin(my_image, YCrCb_min, YCrCb_max);
            imageConvexHull.Source = toBitmapSourceFromImage(skin);
        }

        private void DrawAndComputeFingersNum(Image<Bgr, byte> currentFrame)
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
            //for (int i = 0; i < defects.Total; i++)
            //{
            //    PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
            //                                    (float)defectArray[i].StartPoint.Y);

            //    PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
            //                                    (float)defectArray[i].DepthPoint.Y);

            //    PointF endPoint = new PointF((float)defectArray[i].EndPoint.X,
            //                                    (float)defectArray[i].EndPoint.Y);

            //    LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);

            //    LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);

            //    CircleF startCircle = new CircleF(startPoint, 5f);

            //    CircleF depthCircle = new CircleF(depthPoint, 5f);

            //    CircleF endCircle = new CircleF(endPoint, 5f);

            //    //Custom heuristic based on some experiment, double check it before use
            //    if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
            //    {
            //        fingerNum++;
            //        currentFrame.Draw(startDepthLine, new Bgr(System.Drawing.Color.Green), 2);
            //        //currentFrame.Draw(depthEndLine, new Bgr(Color.Magenta), 2);
            //    }


            //    currentFrame.Draw(startCircle, new Bgr(System.Drawing.Color.Red), 2);
            //    currentFrame.Draw(depthCircle, new Bgr(System.Drawing.Color.Yellow), 5);
            //    //currentFrame.Draw(endCircle, new Bgr(Color.DarkBlue), 4);
            //}
            #endregion

            //MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
            //currentFrame.Draw(fingerNum.ToString(), ref font, new System.Drawing.Point(50, 150), new Bgr(System.Drawing.Color.White));
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
                    for (int i = 0; i < points.Length; i++)
                        ps[i] = new System.Drawing.Point((int)points[i].X, (int)points[i].Y);

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
                }
                else
                    Console.WriteLine("Biggest contourn is null");

                imageConvexHull.Source = toBitmapSourceFromImage(currentFrame);
            }
        }
        
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
        }
        
        #region toBitmapSourceFromImage
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
        #endregion

    }
}
