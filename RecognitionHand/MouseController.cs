using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecognitionHand
{
    class MouseController
    {

        //Control mouse
        private int leftClick = 1;
        private int rightClick = 2;
        private bool LMB_PRESSED = false;
        private bool RMB_PRESSED = false;
        private const int DELTA_TIME_PRESSED_SHORT = 15; //Number of frame for single click
        private const int DELTA_TIME_PRESSED_LONG = 30;  //Number of frame for long click
        private int countFrame = 0;
        private int actualFingerNumber = 0;
        private int[] frameWindow;
        private int index = 0;

        public MouseController(int a) {
            frameWindow = new int[a];
        }

        private void LeftClick()
        {
            Console.WriteLine("LeftClick");
        }

        private void RightClick()
        {
            Console.WriteLine("RightClick");
        }

        public void Click(int fingerNumber)
        {

        }


        private int AverageFrameWindow() {
            int averageNumber = 0;
            int count=0, previousCount=0;
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < frameWindow.Length; j++) {
                    if (frameWindow[j] == i) {
                        count++;
                    }
                  }
                if (count > previousCount) {
                    previousCount = count;
                    averageNumber = i;
                }
                count = 0;
            }
            return averageNumber;
        }

        public void AddNumber(int number) {
            int fingerNumber = 0;
            if (index < frameWindow.Length) {
                frameWindow[index] = number;
                index++;
            }
            else {
                fingerNumber = AverageFrameWindow();
                index = 0;
                Click(fingerNumber);
            }
        }
        

    }
}
