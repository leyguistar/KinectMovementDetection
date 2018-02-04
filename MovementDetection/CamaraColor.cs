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
using System.Media;
using System.Diagnostics;

namespace MovementDetection
{
    public class CamaraColor
    {
        private KinectSensor miKinect;

        public CamaraColor(KinectSensor elKinect)
        {
            miKinect = elKinect;
        }
        public void TomarFoto()
        {
            try
            {
                miKinect.ColorStream.Enable();

            }catch { }
            miKinect.ColorFrameReady += MiKinect_ColorFrameReady;
        }
        byte[] datosColor;
        BitmapSource bitmap;
        private void MiKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using(ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null) return;

                if (datosColor == null)
                    datosColor = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(datosColor);
                

            }
        }
    }
}
