﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace YoloDetector.Models
{
    public class YoloModel
    {
        public string Name { get; set; }
        private string _cfg;
        private string[] _labels;
        private Scalar[] _colors;
        private Net _net;

        public static BindableCollection<YoloModel> GetYoloModels()
        {
            const string cocoPath = "yolo/coco/coco";
            const string vocPath = "yolo/voc/voc";
            const string tiny = "tiny";

            var cocoNames = File.ReadAllLines($"{cocoPath}.names");
            var vocNames = File.ReadAllLines($"{vocPath}.names");
            var cocoColors = Enumerable.Repeat(false, cocoNames.Length).Select(x => Scalar.RandomColor()).ToArray();
            var vocColors = Enumerable.Repeat(false, vocNames.Length).Select(x => Scalar.RandomColor()).ToArray();

            var yoloModels = new BindableCollection<YoloModel>
            {
                new YoloModel
                {
                    Name = "Coco",
                    _cfg = $"{cocoPath}.cfg",
                    _labels = cocoNames,
                    _colors = cocoColors,
                    _net = CvDnn.ReadNetFromDarknet($"{cocoPath}.cfg", $"{cocoPath}.weights")
                },
                new YoloModel
                {
                    Name = "Tiny Coco",
                    _cfg = $"{cocoPath}{tiny}.cfg",
                    _labels = cocoNames,
                    _colors = cocoColors,
                    _net = CvDnn.ReadNetFromDarknet($"{cocoPath}{tiny}.cfg", $"{cocoPath}{tiny}.weights")
                },
                new YoloModel
                {
                    Name = "Voc",
                    _cfg = $"{vocPath}.cfg",
                    _labels = vocNames,
                    _colors = vocColors,
                    _net = CvDnn.ReadNetFromDarknet($"{vocPath}.cfg", $"{vocPath}.weights")
                },
                new YoloModel
                {
                    Name = "Tiny Voc",
                    _cfg = $"{vocPath}{tiny}.cfg",
                    _labels = vocNames,
                    _colors = vocColors,
                    _net = CvDnn.ReadNetFromDarknet($"{vocPath}{tiny}.cfg", $"{vocPath}{tiny}.weights")
                }
            };
            return yoloModels;
        }

        /// <summary>
        /// Read size from cfg file
        /// </summary>
        /// <returns>size of image</returns>
        private int GetSize()
        {
            return int.Parse(
                new string(File.ReadAllLines(_cfg).First(x => x.StartsWith("width"))
                    .Where(char.IsDigit).ToArray())
            );
        }

        /// <summary>
        /// Find object from darknet yolo
        /// </summary>
        /// <param name="image">input image</param>
        /// <param name="threshold">minimum threshold</param>
        /// <param name="text">result percents in string format</param>
        /// <returns>Image with labels</returns>
        public Mat FindObjects(Mat image, double threshold, out string text)
        {
            // setting blob, parameter are important
            Mat blob = CvDnn.BlobFromImage(image, 1 / 255.0, new Size(GetSize(), GetSize()),
                new Scalar(), true, false);

            _net.SetInput(blob, "data");

            var sw = new Stopwatch();

            sw.Start();

            // forward model
            Mat prob = _net.Forward();

            sw.Stop();

            var result = new StringBuilder();
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
                    // get classes probability
                    Cv2.MinMaxLoc(prob.Row[i].ColRange(prefix, prob.Cols), out _, out Point max);
                    int classes = max.X;
                    var probability = prob.At<float>(i, classes + prefix);

                    // more accuracy
                    if (probability > threshold)
                    {
                        // get center and width/height

                        var centerX = prob.At<float>(i, 0) * image.Width;
                        var centerY = prob.At<float>(i, 1) * image.Height;
                        var width = prob.At<float>(i, 2) * image.Width;
                        var height = prob.At<float>(i, 3) * image.Height;

                        // label formatting
                        var label = $"{_labels[classes]} {probability * 100:0.00}%";
                        result.AppendLine($"confidence {confidence * 100:0.00}% {label}");

                        // avoid left side over edge
                        var x1 = (centerX - width / 2) < 0 ? 0 : centerX - width / 2;

                        // draw result
                        image.Rectangle(
                            new Point(x1, centerY - height / 2),
                            new Point(centerX + width / 2, centerY + height / 2), _colors[classes],
                            2
                        );
                        var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheyTriplex, 0.5, 1, out var baseline);

                        Cv2.Rectangle(
                            image,
                            new Rect(
                                new Point(x1, centerY - height / 2 - textSize.Height - baseline),
                                new Size(textSize.Width, textSize.Height + baseline)
                            ),
                            _colors[classes],
                            Cv2.FILLED
                        );

                        Cv2.PutText(
                            image,
                            label,
                            new Point(x1, centerY - height / 2 - baseline),
                            HersheyFonts.Italic,
                            0.5,
                            Scalar.Black
                        );
                    }

                }

            }

            text = result.ToString();
            return image;
        }
    }
}