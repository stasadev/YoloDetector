using System.IO;
using System.Linq;
using Caliburn.Micro;
using OpenCvSharp;

namespace YoloDetector.Models
{
    public class YoloModel
    {
        public string Name { get; set; }
        public string Cfg { get; set; }
        public string Model { get; set; }
        public string[] Labels { get; set; }
        public Scalar[] Colors { get; set; }

        public static BindableCollection<YoloModel> GetYoloModels()
        {
            var coco = File.ReadAllLines("yolo/coco/coco.names");
            var voc = File.ReadAllLines("yolo/voc/voc.names");
            var cocoColors = Enumerable.Repeat(false, coco.Length).Select(x => Scalar.RandomColor()).ToArray();
            var vocColors = Enumerable.Repeat(false, voc.Length).Select(x => Scalar.RandomColor()).ToArray();

            var yoloModels = new BindableCollection<YoloModel>
            {
                new YoloModel
                {
                    Name = "Coco",
                    Cfg = "yolo/coco/coco.cfg",
                    Model = "yolo/coco/coco.weights",
                    Labels = coco,
                    Colors = cocoColors,
                },
                new YoloModel
                {
                    Name = "Tiny Coco",
                    Cfg = "yolo/coco/tiny-coco.cfg",
                    Model = "yolo/coco/tiny-coco.weights",
                    Labels = coco,
                    Colors = cocoColors,
                },
                new YoloModel
                {
                    Name = "Voc",
                    Cfg = "yolo/voc/voc.cfg",
                    Model = "yolo/voc/voc.weights",
                    Labels = voc,
                    Colors = vocColors,
                },
                new YoloModel
                {
                    Name = "Tiny Voc",
                    Cfg = "yolo/voc/tiny-voc.cfg",
                    Model = "yolo/voc/tiny-voc.weights",
                    Labels = voc,
                    Colors = vocColors,
                }
            };
            return yoloModels;
        }
    }
}