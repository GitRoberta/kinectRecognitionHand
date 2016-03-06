using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using Emgu.CV;

namespace RecognitionHand
{
    public class HsvSkinDetector : IColorSkinDetector
    {

        public override Image<Gray, byte> DetectSkin(Image<Bgr, byte> Img, IColor min, IColor max)
        {
            Image<Hsv, Byte> currentHsvFrame = Img.Convert<Hsv, Byte>();
            Image<Gray, byte> skin = new Image<Gray, byte>(Img.Width, Img.Height);
            skin = currentHsvFrame.InRange((Hsv)min, (Hsv)max);

            StructuringElementEx rect_6 = new StructuringElementEx(3, 3,1, 1, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            CvInvoke.cvDilate(skin, skin, rect_6, 2);


            return skin;
        }
    }
}
