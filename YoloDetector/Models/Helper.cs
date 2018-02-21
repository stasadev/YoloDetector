using System.Linq;
using OpenCvSharp;

namespace YoloDetector.Models
{
    public static class Helper
    {
        public static readonly Scalar[] Colors = Enumerable.Repeat(false, 20).Select(x => Scalar.RandomColor())
            .ToArray();
    }
}
