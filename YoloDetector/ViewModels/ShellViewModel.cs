using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private Mat _start = new Mat();
        private Mat _finish = new Mat();
        private WriteableBitmap _image;
        private string _filePath;
        private YoloModel _selectedYoloModel;
        private string _info;

        public Mat Start
        {
            get => _start;
            set
            {
                _start = value;
                Image = Start.ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        public Mat Finish
        {
            get => _finish;
            set
            {
                _finish = value;
                Image = Finish.ToWriteableBitmap(PixelFormats.Bgr24);
            }
        }

        public WriteableBitmap Image
        {
            get => _image;
            set
            {
                _image = value;
                NotifyOfPropertyChange(() => Image);
                NotifyOfPropertyChange(() => CanFindObjects);
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

        public BindableCollection<YoloModel> YoloModels { get; set; } = YoloModel.GetYoloModels();

        public YoloModel SelectedYoloModel
        {
            get => _selectedYoloModel;
            set
            {
                _selectedYoloModel = value;
                NotifyOfPropertyChange(() => SelectedYoloModel);
            }
        }

        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                NotifyOfPropertyChange(() => Info);
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
                Start = new Mat(FilePath, ImreadModes.AnyDepth | ImreadModes.AnyColor);
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

        public bool CanFindObjects => Image != null;

        public void FindObjects()
        {
            var cfg = SelectedYoloModel.Cfg;
            var size = int.Parse(
                new string(File.ReadAllLines(cfg).First(x => x.StartsWith("width"))
                    .Where(char.IsDigit).ToArray())
            );

            var model = SelectedYoloModel.Model;
            var labels = SelectedYoloModel.Labels;
            var colors = SelectedYoloModel.Colors;
            var threshold = 0.3;

            StringBuilder result = new StringBuilder();

            Finish = Start.Clone();
            var w = Finish.Width;
            var h = Finish.Height;

            //setting blob, parameter are important
            var blob = CvDnn.BlobFromImage(Finish, 1 / 255.0, new OpenCvSharp.Size(size, size),
                new Scalar(), true, false);
            var net = CvDnn.ReadNetFromDarknet(cfg, model);
            net.SetInput(blob, "data");

            var sw = new Stopwatch();
            sw.Start();
            //forward model
            var prob = net.Forward();
            sw.Stop();
            result.AppendLine($"Runtime: {sw.ElapsedMilliseconds} ms");

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
                        result.AppendLine($"confidence {confidence * 100:0.00}% {label}");
                        var x1 = (centerX - width / 2) < 0 ? 0 : centerX - width / 2; //avoid left side over edge
                        //draw result
                        Finish.Rectangle(new OpenCvSharp.Point(x1, centerY - height / 2),
                            new OpenCvSharp.Point(centerX + width / 2, centerY + height / 2), colors[classes],
                            2);
                        var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheyTriplex, 0.5, 1, out var baseline);
                        Cv2.Rectangle(Finish, new OpenCvSharp.Rect(
                                new OpenCvSharp.Point(x1, centerY - height / 2 - textSize.Height - baseline),
                                new OpenCvSharp.Size(textSize.Width, textSize.Height + baseline)),
                            colors[classes],
                            Cv2.FILLED);
                        Cv2.PutText(Finish, label, new OpenCvSharp.Point(x1, centerY - height / 2 - baseline),
                            HersheyFonts.Italic, 0.5, Scalar.Black);

                    }
                }
            }
            Finish = Finish;
            Info = result.ToString();
        }
    }
}