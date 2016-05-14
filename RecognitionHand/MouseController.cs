using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Kinect.Nui;
using System.Runtime.InteropServices;


namespace RecognitionHand
{
    class MouseController
    {
        //Control mouse
        private int leftClick = 1;
        private int rightClick = 2;
        private bool LMB_PRESSED = false;
        private bool RMB_PRESSED = false;
        private const int DELTA_TIME_PRESSED_SHORT = 60; //Number of frame for single click
        private const int DELTA_TIME_PRESSED_LONG = 90;  //Number of frame for long click
        private int countFrame = 0;
        private int actualFingerNumber = 0;
        private int[] frameWindow;
        private int index = 0;
        private uint mouseX = 0, mouseY = 0;
        private bool DebugMode = false;

        public float SensitivityX = 1f;
        public float SensitivityY = 1f;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

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


        private const int LMB_DOWN_CODE = 0x02;
        private const int LMB_UP_CODE = 0x04;
        private const int RMB_DOWN_CODE = 0x08;
        private const int RMB_UP_CODE = 0x10;


        private double startX = System.Windows.SystemParameters.PrimaryScreenWidth / 2;
        private double startY = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
        private float previusX = 0;
        private float previusY = 0;

        public MouseController(int a, bool debug)
        {
            frameWindow = new int[a];
            DebugMode = debug;
            previusX = (uint)startX;
            previusY = (uint)startY;
        }

        private void LeftClick()
        {

            //Console.WriteLine("LeftClick");
            countFrame += 15;
            if (RMB_PRESSED)
            {
                mouse_event(RMB_UP_CODE, mouseX, mouseY, 0, 0);
                //Console.WriteLine("RMB_UP");
                RMB_PRESSED = false;
            }

            if (!LMB_PRESSED)
            {
                //Console.WriteLine("LMB_DOWN");
                mouse_event(LMB_DOWN_CODE, mouseX, mouseY, 0, 0);
                LMB_PRESSED = true;
            }
            //else
            //    Console.WriteLine("LMB_STILL_DOWN");
        }

        private void RightClick()
        {
            //Console.WriteLine("RightClick");
            countFrame += 15;
            if (LMB_PRESSED)
            {
                mouse_event(LMB_UP_CODE, mouseX, mouseY, 0, 0);
                //Console.WriteLine("LMB_UP");
                LMB_PRESSED = false;
            }

            if (!RMB_PRESSED)
            {
                //Console.WriteLine("RMB_DOWN");
                mouse_event(RMB_DOWN_CODE, mouseX, mouseY, 0, 0);
                RMB_PRESSED = true;
            }
            //else
            //    Console.WriteLine("RMB_STILL_DOWN");
        }

        public void ReleaseClick()
        {
            countFrame = 0;

            if (RMB_PRESSED)
            {
                mouse_event(RMB_UP_CODE, mouseX, mouseY, 0, 0);
                RMB_PRESSED = false;
            }

            if (LMB_PRESSED)
            {
                mouse_event(LMB_UP_CODE, mouseX, mouseY, 0, 0);
                LMB_PRESSED = false;
            }

            //Console.WriteLine("ReleaseClick");
        }

        private void Click(int fingerNumber)
        {
            if (DebugMode)
                return;




            switch (fingerNumber)
            {
                case 0:// Console.WriteLine("0 fingernumber windowed");
                    ReleaseClick();
                    break;
                case 1: // Console.WriteLine("1 fingernumber windowed");
                    ReleaseClick(); break;
                case 2: LeftClick(); break;
                case 3:
                    RightClick();
                    break;
                case 4:// Console.WriteLine("4 fingernumber windowed");
                    ReleaseClick();
                    break;
                case 5:// Console.WriteLine("5 fingernumber windowed");
                    ReleaseClick();
                    break;
                default:
                    ReleaseClick();
                    break;
            }
        }


        private void ButtonClicked(bool left)
        {
            if (left)
            {
                LMB_PRESSED = true;
                RMB_PRESSED = false;
            }
        }

        private int AverageFrameWindow()
        {
            int averageNumber = 0;
            int count = 0, previousCount = 0;

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < frameWindow.Length; j++)
                {
                    if (frameWindow[j] == i)
                    {
                        count++;
                    }
                }
                if (count > previousCount)
                {
                    previousCount = count;
                    averageNumber = i;
                }
                count = 0;
            }

            return averageNumber;
        }

        public void AddNumber(int number, uint dx = 0, uint dy = 0)
        {
            int fingerNumber = 0;
            if (index < frameWindow.Length)
            {
                frameWindow[index] = number;
                index++;
            }
            else {
                fingerNumber = AverageFrameWindow();
                index = 0;
                mouseX = dx;
                mouseY = dy;
                Click(fingerNumber);
            }
        }

        public void MoveMouse(Joint joint)
        {
            float actualX = joint.Position.X;
            float actualY = joint.Position.Y;

            int deltaX = (int)(actualX - previusX);
            int deltaY = (int)(actualY - previusY);

           
            SetMousePosition(deltaX, deltaY);

            previusX = actualX;
            previusY = actualY;

        }

        public void SetMousePosition(int deltaX, int deltaY)
        {
            POINT p;
            GetCursorPos(out p);
            p.X += (int)(deltaX * SensitivityX);
            p.Y += (int)(deltaY * SensitivityY);

            mouseX = (uint) p.X;
            mouseY = (uint) p.Y;
            SetCursorPos(p.X, p.Y);
        }

    }
}
