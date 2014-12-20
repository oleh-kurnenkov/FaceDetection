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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Windows.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using System.Drawing.Imaging;
using System.Data.Common;
using System.IO;
using System.Data.SqlClient;

namespace face_detection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        private Capture capture;
        private static int i;
        DispatcherTimer timer;
        private Image<Bgr, Byte> currentFrame;
        private CascadeClassifier haarCascade;
        
        public MainWindow()
        {
            InitializeComponent();
            
        }
      
        private void snpClick(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            currentFrame = capture.QueryFrame();
            Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();

            Rectangle[] dtc = null;
                dtc = haarCascade.DetectMultiScale(grayFrame, 1.1, 2, new System.Drawing.Size(100, 100), new System.Drawing.Size(800, 800));

            //var detectedFaces = grayFrame.DetectHaarCascade(haarCascade)[0];

            //foreach (var face in detectedFaces)
            //    currentFrame.Draw(face.rect, new Bgr(0, double.MaxValue, 0), 3);
               currentFrame.ROI = dtc[0];
               Image<Bgr,Byte> saveFrame = currentFrame.Resize(200,200,INTER.CV_INTER_LANCZOS4,true);
                //currentFrame.Draw(dtc[0], new Bgr(0, double.MaxValue, 0), 3);
            StringBuilder name = new StringBuilder("face");
            name.AppendFormat("{0}.jpg", i);
            String n = name.ToString();
            saveFrame.Save(n);
            i++;
            timer.Start();
        }

        private void Wnd_loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                i = 0;
                capture = new Capture();
                haarCascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
                timer = new DispatcherTimer();
                timer.Tick += new EventHandler(timer_Tick);
                timer.Interval = new TimeSpan(0, 0, 0, 0, 40);
                timer.Start();
            
            }
            catch (TypeInitializationException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
          
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            currentFrame = null;
            currentFrame = capture.QueryFrame();
            if (currentFrame != null)
            {
                
                Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();
                Rectangle[] dtc = null;
                dtc = haarCascade.DetectMultiScale(grayFrame, 1.1, 5, new System.Drawing.Size(150, 150), new System.Drawing.Size(400, 400));
                
                //var detectedFaces = currentFrame.DetectHaarCascade(haarCascade)[0];

                foreach (Rectangle face in dtc)
                try
                {
                    if (face != null)
                        currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                WebCamImage.Source = ToBitmapSource(currentFrame);

                currentFrame.Dispose();
                GC.Collect();
            }
            
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        public  BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(ptr);
                source.Dispose();
                return bs;
            }
        }

        private void add(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            currentFrame = capture.QueryFrame();
            Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();

            Rectangle[] dtc = null;
            dtc = haarCascade.DetectMultiScale(grayFrame, 1.1, 5, new System.Drawing.Size(100, 100), new System.Drawing.Size(800, 800));

            currentFrame.ROI = dtc[0];
           Image<Bgr,Byte> imgToDB = currentFrame.Resize(200, 200, INTER.CV_INTER_LANCZOS4);
          
            BitmapSource src = ToBitmapSource(imgToDB);
            Bitmap btm = BitmapFromSource(src);
            System.Drawing.Image img = (System.Drawing.Image)btm;
            MemoryStream ms = new MemoryStream();
            var img2 = new Bitmap(img, new System.Drawing.Size(200,200));
            img2.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);

            byte[] imgtoDB = ms.ToArray(); ;
            String connectionString = @"Data Source=(local)\SQLEXPRESS;Initial Catalog=faces1;Integrated Security=True";

            using(SqlConnection connection = new SqlConnection(
       connectionString))
            {
                SqlCommand command = new SqlCommand(
        @"INSERT INTO face1 (face,id) VALUES(@face,@id)", connection);

                SqlParameter param = new SqlParameter();
                param.ParameterName = "@face";
                param.Value = imgtoDB;
                param.SqlDbType = System.Data.SqlDbType.Image;
                param.Size = imgtoDB.Length;
                command.Parameters.Add(param);
                Random random = new Random();
                param = new SqlParameter();
                param.ParameterName = "@id";
                param.Value = random.Next(1,100000);
                param.SqlDbType = System.Data.SqlDbType.Int;
                command.Parameters.Add(param); 


                
                connection.Open();
                command.ExecuteNonQuery();
            }



            timer.Start();
        }


        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }

        List<byte[]> getFromDB()
        {
           var images = new List<byte[]>();
            String connectionString = @"Data Source=(local)\SQLEXPRESS;Initial Catalog=faces1;Integrated Security=True";
            using(SqlConnection connection = new SqlConnection(
       connectionString))
            {
                SqlCommand command = new SqlCommand(@"Select * From face1",connection);
                connection.Open();
                using(SqlDataReader imgReader = command.ExecuteReader())
                {
                    do
                    {
                    while(imgReader.Read())
                    {
                        images.Add((byte[])imgReader.GetValue(0));
                    }
                    }while(imgReader.NextResult());
                }
            }
            return images;
        }
        public byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }

        private void getImagesCount(object sender, RoutedEventArgs e)
        {
            var imgs = getFromDB();
            MessageBox.Show(imgs.Count.ToString());
        }

private bool imageCompare()
        {

        }
    }
}
