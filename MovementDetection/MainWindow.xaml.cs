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
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Diagnostics;

namespace MovementDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor miKinect;
        SpeechSynthesizer voz;
        List<VoiceInfo> voice;
        SoundPlayer sonido = new SoundPlayer(@"C:\Users\david\Videos\audio de videos\Alerta roja   Efecto de sonido.wav");
        public MainWindow()
        {
            InitializeComponent();
        }

        private void image_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(KinectSensor.KinectSensors.Count <= 0)
            {
                MessageBox.Show("no se encontro un kinect");
                Application.Current.Shutdown();
                return;
            }
            try {
                miKinect = KinectSensor.KinectSensors.FirstOrDefault();
                miKinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                miKinect.Start();
            }
            catch (Exception ex){
                MessageBox.Show("error\n" + ex.Message);
            }
            miKinect.DepthFrameReady += MiKinect_DepthFrameReady;
            Application.Current.Exit += Current_Exit;
            voz = new SpeechSynthesizer();
            voice = new List<VoiceInfo>();
            foreach (InstalledVoice Voice in voz.GetInstalledVoices())
            {
                comboBox.Items.Add(Voice.VoiceInfo.Name);
                voice.Add(Voice.VoiceInfo);
            }

        }
        private void Hablar(string mensaje)
        {
            string name = voice.ElementAt(comboBox.SelectedIndex).Name;
            voz.SelectVoice(name);
            voz.Volume = 100;
            voz.Speak(mensaje);
        }
        private void Current_Exit(object sender, ExitEventArgs e)
        {
            if(miKinect != null)
            {
                miKinect.Stop();
            }
        }

        byte[] datosColor;
        short[] datosProfundidad;
        WriteableBitmap bitmap;
        Stopwatch tiempo = new Stopwatch();
        Stopwatch Contador = new Stopwatch();
        bool DetectarMovimiento = false;
        int tolerancia = 10000;
        int movimiento = 0;
        bool MovimientoDetectado = true;
        bool unaVez = true;
        int Desde = 10, Hasta = 100;
        int posColor = 0;
        int[] lastFrame;
        private void MiKinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using(DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame == null) return;

                if (datosColor == null)
                    datosColor = new byte[frame.PixelDataLength * 4];
                if (datosProfundidad == null)
                    datosProfundidad = new short[frame.PixelDataLength];
                if (lastFrame == null)
                    lastFrame = new int[frame.PixelDataLength];
                frame.CopyPixelDataTo(datosProfundidad);
                posColor = 0;
                movimiento = 0;
                for (int i = 0; i < frame.PixelDataLength; i++)
                {
                    int profundidad = datosProfundidad[i] >> 3;

                    if(profundidad == miKinect.DepthStream.UnknownDepth)
                    {
                        datosColor[posColor++] = 255;
                        datosColor[posColor++] = 0;
                        datosColor[posColor++] = 0;
                    }
                    else if(profundidad == miKinect.DepthStream.TooNearDepth)
                    {
                        datosColor[posColor++] = 0;
                        datosColor[posColor++] = 0;
                        datosColor[posColor++] = 255;
                    }
                    else if(profundidad == miKinect.DepthStream.TooFarDepth)
                    {
                        datosColor[posColor++] = 0;
                        datosColor[posColor++] = 255;
                        datosColor[posColor++] = 0;
                    }
                    else if(profundidad >> 5 >= Desde && profundidad >> 5 <= Hasta)
                    {
                        datosColor[posColor++] = 29;
                        datosColor[posColor++] = 32;
                        datosColor[posColor++] = 54;
                    }
                    else
                    {
                        byte color = (byte)(255-(profundidad >> 5));
                        datosColor[posColor++] = color;
                        datosColor[posColor++] = color;
                        datosColor[posColor++] = color;
                    }
                    posColor++;
                    if (DetectarMovimiento)
                    {
                        if (profundidad >> 5 <= Hasta && profundidad >> 5 >= Desde)
                        {
                            if (lastFrame[i] != profundidad)
                            {
                                movimiento++;
                            }
                        }
                    }
                    lastFrame[i] = profundidad;
                }
                if (DetectarMovimiento)
                {
                    if(movimiento > tolerancia)
                    {
                        MovimientoDetectado = true;
                        if (unaVez)
                        {
                            if(cbSonido.IsChecked == true)
                                sonido.Play();
                            lbMovimiento.Content = "Movimiento detectado";
                            lbMovimiento.Foreground = Brushes.Red;
                            unaVez = false;
                            tiempo.Start();
                        }

                        Console.WriteLine("Movimiento detectado " + movimiento);
                    }
                }
                if(Contador.ElapsedMilliseconds > int.Parse(tbTiempo.Text) * 1000)
                {
                    Contador.Stop();
                    Contador.Reset();
                    ActivarDeteccion();
                }
                else if (Contador.IsRunning)
                {
                    lbTiempo.Content = string.Format("{0}", Contador.ElapsedMilliseconds / 1000);
                }
                if(tiempo.ElapsedMilliseconds > 5000)
                {
                    lbMovimiento.Content = "No movimiento";
                    lbMovimiento.Foreground = Brushes.Black;
                    unaVez = true;
                    tiempo.Stop();
                    tiempo.Reset();
                }
                if (bitmap == null)
                    bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                        PixelFormats.Bgr32, null);

                bitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), datosColor,
                    frame.Width * 4, 0);
                image.Source = bitmap;

            }
        }
        private void TomarFoto()
        {
            miKinect.ColorStream.Enable();

        }
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(textBox.Text != string.Empty && textBox.Text != null)
            tolerancia = int.Parse(textBox.Text);
        }

        private void tbDesde_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tbDesde.Text != "" && tbDesde.Text != null)
            {
                Desde = int.Parse(tbDesde.Text);

            }
            else
            {
                tbDesde.Text = "10";
            }
        }

        private void tbHasta_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tbHasta.Text != "" && tbHasta.Text != null)
            {
                Hasta = int.Parse(tbHasta.Text);

            }
            else
            {
                tbHasta.Text = "10";
            }
        }
        private void btActivar_Click(object sender, RoutedEventArgs e)
        {
            if (!DetectarMovimiento)
            {
                if (cbTiempo.IsChecked == true)
                {
                    Contador.Start();
                }
                else
                {
                    ActivarDeteccion();
                }
            }
            else
            {

                DetectarMovimiento = false;
                btActivar.Content = "Activar";
                lbEstado.Content = "Inactivo";
                lbEstado.Foreground = Brushes.Red;
            }
        }

        private void tbTiempo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tbTiempo.Text == "")
            {
                tbTiempo.Text = "0";
            }
        }

        private void tbEntrada_TextChanged(object sender, TextChangedEventArgs e)
        {
            string Entrada = tbEntrada.Text;
            if (Entrada.ToLower().Contains("activar"))
            {
                ActivarDeteccion();
                Hablar("Deteccion activada");
                tbEntrada.Text = "";
            }
            else if (Entrada.ToLower().Contains("apagar"))
            {
                Hablar("apagando deteccion de movimiento");
                DetectarMovimiento = false;
                btActivar.Content = "Activar";
                lbEstado.Content = "Inactivo";
                lbEstado.Foreground = Brushes.Red;
                tbEntrada.Text = "";
            }
        }

        private void ActivarDeteccion()
        {
            
                DetectarMovimiento = true;
                btActivar.Content = "Desactivar";
                lbEstado.Content = "Activo";
                lbEstado.Foreground = Brushes.Green;

        }
    }
}
