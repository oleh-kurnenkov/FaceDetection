﻿using System;
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
        private static int ind;
        private static int j;
        DispatcherTimer timer;
        private Image<Bgr, Byte> currentFrame;
        private CascadeClassifier haarCascade;
        
        public MainWindow()
        {
            InitializeComponent();
            
        }
      
    
        private void Wnd_loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                j = 0;
                ind = -1;
                capture = new Capture();
                haarCascade = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
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

                dtc = haarCascade.DetectMultiScale(grayFrame, 1.4, 4, new System.Drawing.Size(150, 150), new System.Drawing.Size(250, 250));
                
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
            dtc = haarCascade.DetectMultiScale(grayFrame, 1.4,4, new System.Drawing.Size(150, 150), new System.Drawing.Size(250, 250));

            currentFrame.ROI = dtc[0];
            Image<Bgr, byte> fullIMG = currentFrame.Clone();
            BitmapSource flimg = ToBitmapSource(fullIMG);
            Bitmap flBitmap = BitmapFromSource(flimg);
            MemoryStream m = new MemoryStream();
            System.Drawing.Image full = (System.Drawing.Image)flBitmap;
            var imgfull = new Bitmap(full);
            imgfull.Save(m, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] fimg = m.ToArray();
           Image<Gray,Byte> imgToDB = currentFrame.Resize(8,8, INTER.CV_INTER_CUBIC).Convert<Gray,Byte>();
            BitmapSource src = ToBitmapSource(imgToDB);
            Bitmap btm = BitmapFromSource(src);
            System.Drawing.Image img = (System.Drawing.Image)btm;
            MemoryStream ms = new MemoryStream();
            var img2 = new Bitmap(img);
            img2.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

            byte[] imgtoDB = ms.ToArray(); 
            String connectionString = @"Data Source=(localdb)\v11.0;Initial Catalog=aa;Integrated Security=True;Pooling=False";

            List<byte[]> imgs = getFromDB();
            int count = 0;
            List<byte[]> images = getFullImage();
            for (int i = 0; i < imgs.Count;i++)
            {
                MemoryStream imgMS = new MemoryStream(imgs[i]);
                Bitmap DBimg = new Bitmap(imgMS);
                if (imageCompare(btm, DBimg) < 5)
                {
                    ind = i;    
                    count++;
                }
               
            }
            if (count == 0)
                using (SqlConnection connection = new SqlConnection(
           connectionString))
                {
                    SqlCommand command = new SqlCommand(
            @"INSERT INTO face1 (face,id,fullface) VALUES(@face,@id,@fullface)", connection);

                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@face";
                    param.Value = imgtoDB;
                    param.SqlDbType = System.Data.SqlDbType.Image;
                    param.Size = imgtoDB.Length;
                    command.Parameters.Add(param);
                    Random random = new Random();
                    param = new SqlParameter();
                    param.ParameterName = "@id";
                    param.Value = random.Next(1, 100000);
                    param.SqlDbType = System.Data.SqlDbType.Int;
                    command.Parameters.Add(param);
                    connection.Open();
                  
                    param = new SqlParameter();
                    param.ParameterName = "@fullface";
                    param.Value = m.ToArray();
                    param.SqlDbType = System.Data.SqlDbType.Image;
                    param.Size = m.ToArray().Length;
                    command.Parameters.Add(param);
                    command.ExecuteNonQuery();
                    MessageBox.Show("added");
                }
            else
            {
                MessageBox.Show("already in DB");
                var mm1 = new MemoryStream(images[ind]);
                Bitmap face = new Bitmap(mm1);
                Image<Bgr,byte> imgf = new Image<Bgr,byte>(face);
                imgFromDB.Source = ToBitmapSource(imgf);
            };


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
           String connectionString = @"Data Source=(localdb)\v11.0;Initial Catalog=aa;Integrated Security=True;Pooling=False";
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

        List<byte[]> getFullImage()
        {
            var images = new List<byte[]>();
            String connectionString = @"Data Source=(localdb)\v11.0;Initial Catalog=aa;Integrated Security=True;Pooling=False";
            using (SqlConnection connection = new SqlConnection(
       connectionString))
            {
                SqlCommand command = new SqlCommand(@"Select * From face1", connection);
                connection.Open();
                using (SqlDataReader imgReader = command.ExecuteReader())
                {
                    do
                    {
                        while (imgReader.Read())
                        {
                            images.Add((byte[])imgReader.GetValue(2));
                        }
                    } while (imgReader.NextResult());
                }
            }
            return images;
        }
        public byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }


private int imageCompare(Bitmap first, Bitmap second)
        {
            int DiferentPixels = 0;
            float avr1=0, avr2 = 0;
           int[] clrs1= new int[64];
           int[] clrs2 = new int[64];
           int[] clrs3 = new int[64];
           int[] clrs4 = new int[64];
            Bitmap container = new Bitmap(first.Width, first.Height);
            for (int i = 0; i < first.Width; ++i)
            {
                for (int j = 0; j < first.Height; ++j)
                {
                    Color secondColor = second.GetPixel(i, j);
                    Color firstColor = first.GetPixel(i, j);
                    clrs1[i * j] = firstColor.R + firstColor.G + firstColor.B;
                    clrs2[i * j] = secondColor.R + secondColor.G + secondColor.B;
                    avr1 += clrs1[i * j];
                    avr2 += clrs2[i * j];
                }
            }
            avr1 = (float)(avr1 / 64);
            avr2 = (float)(avr2 / 64);
            for (int i = 0; i < first.Width; ++i)
            {
                for (int j = 0; j < first.Height; ++j)
                {
                    if (clrs1[i * j] >= avr1)
                        clrs3[i * j] = 1;
                    else
                        clrs3[i * j] = 0;

                    if (clrs2[i * j] >= avr2)
                        clrs4[i * j] = 1;
                    else
                        clrs4[i * j] = 0;
                }
            }
            for (int i = 0; i < first.Width; ++i)
            {
                for (int j = 0; j < first.Height; ++j)
                {
                    if (clrs3[i * j] != clrs4[i * j])
                        DiferentPixels++;
                   
                }
            }
            return DiferentPixels;
        }


    }
}
