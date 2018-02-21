using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using YoloDetector.Models;

namespace YoloDetector.ViewModels
{
    public class ShellViewModel : Screen
    {
        public string Lorem { get; set; } = Helper.Lorem;

        private Mat _imageMat = new Mat();
        private WriteableBitmap _image;
        private string _filePath;

        public Mat ImageMat
        {
            get => _imageMat;
            set
            {
                _imageMat = value;
                Image = ImageMat.ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        public WriteableBitmap Image
        {
            get => _image;
            set
            {
                _image = value;
                NotifyOfPropertyChange(() => Image);
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                NotifyOfPropertyChange(() => FilePath);
            }
        }

        public void OpenImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images (*.bmp;*.jpg;*.png;*.gif)|*.bmp;*.jpg;*.png;*.gif|All files (*.*)|*.*"
            };
            var result = dlg.ShowDialog();

            if (!(result ?? false)) return;

            try
            {
                FilePath = dlg.FileName;
                ImageMat = new Mat(FilePath, ImreadModes.AnyDepth | ImreadModes.AnyColor);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void FindObjects()
        {
            var cfg = "yolo/coco/coco.cfg";
            var model = "yolo/coco/coco.weights"; //YOLOv2 544x544
            string[] labels = File.ReadAllLines("yolo/coco/coco.names");
            var threshold = 0.3;

            var w = ImageMat.Width;
            var h = ImageMat.Height;
            //setting blob, parameter are important
            var blob = CvDnn.BlobFromImage(ImageMat, 1 / 255.0, new OpenCvSharp.Size(608, 608), new Scalar(), true, false);
            var net = CvDnn.ReadNetFromDarknet(cfg, model);
            net.SetInput(blob, "data");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //forward model
            var prob = net.Forward();
            sw.Stop();
            //Console.WriteLine($"Runtime:{sw.ElapsedMilliseconds} ms");

            /* YOLO2 VOC output
             0 1 : center                    2 3 : w/h
             4 : confidence                  5 ~24 : class probability */
            const int prefix = 5; //skip 0~4

            for (int i = 0; i < prob.Rows; i++)
            {
                var confidence = prob.At<float>(i, 4);
                if (confidence > threshold)
                {
                    //get classes probability
                    Cv2.MinMaxLoc(prob.Row[i].ColRange(prefix, prob.Cols), out _, out OpenCvSharp.Point max);
                    var classes = max.X;
                    var probability = prob.At<float>(i, classes + prefix);

                    if (probability > threshold) //more accuracy
                    {
                        //get center and width/height
                        var centerX = prob.At<float>(i, 0) * w;
                        var centerY = prob.At<float>(i, 1) * h;
                        var width = prob.At<float>(i, 2) * w;
                        var height = prob.At<float>(i, 3) * h;
                        //label formating
                        var label = $"{labels[classes]} {probability * 100:0.00}%";
                        //Console.WriteLine($"confidence {confidence * 100:0.00}% {label}");
                        var x1 = (centerX - width / 2) < 0 ? 0 : centerX - width / 2; //avoid left side over edge
                        //draw result
                        ImageMat.Rectangle(new OpenCvSharp.Point(x1, centerY - height / 2),
                            new OpenCvSharp.Point(centerX + width / 2, centerY + height / 2), Helper.Colors[classes], 2);
                        var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheyTriplex, 0.5, 1, out var baseline);
                        Cv2.Rectangle(ImageMat, new OpenCvSharp.Rect(new OpenCvSharp.Point(x1, centerY - height / 2 - textSize.Height - baseline),
                            new OpenCvSharp.Size(textSize.Width, textSize.Height + baseline)), Helper.Colors[classes], Cv2.FILLED);
                        Cv2.PutText(ImageMat, label, new OpenCvSharp.Point(x1, centerY - height / 2 - baseline),
                            HersheyFonts.Italic, 0.5, Scalar.Black);
                        ImageMat = ImageMat;
                    }
                }
            }
        }
    }
}