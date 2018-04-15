using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Microsoft.Win32;
using OpenCvSharp;
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
        private double _threshold = 30;
        private string _status = UiServices.Locale("Ready");
        private Visibility _waitAnimation = Visibility.Collapsed;

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

        /// <summary>
        /// Image
        /// </summary>
        public WriteableBitmap Image
        {
            get => _image;
            set
            {
                _image = value;
                NotifyOfPropertyChange(() => Image);
                NotifyOfPropertyChange(() => CanFindObjects);
                NotifyOfPropertyChange(() => WaitAnimation);
            }
        }

        /// <summary>
        /// Textbox
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                NotifyOfPropertyChange(() => FilePath);
            }
        }

        /// <summary>
        /// ComboBox
        /// </summary>
        public BindableCollection<YoloModel> YoloModels { get; set; } = YoloModel.GetYoloModels();

        /// <summary>
        /// Selected item of ComboBox
        /// </summary>
        public YoloModel SelectedYoloModel
        {
            get => _selectedYoloModel;
            set
            {
                _selectedYoloModel = value;
                NotifyOfPropertyChange(() => SelectedYoloModel);
            }
        }

        /// <summary>
        /// TextBox
        /// </summary>
        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                NotifyOfPropertyChange(() => Info);
            }
        }

        /// <summary>
        /// Slider
        /// </summary>
        public double Threshold
        {
            get => _threshold;
            set
            {
                _threshold = value;
                NotifyOfPropertyChange(() => Threshold);
            }
        }

        /// <summary>
        /// StatusBarItem
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                NotifyOfPropertyChange(() => Status);
                NotifyOfPropertyChange(() => CanFindObjects);
                NotifyOfPropertyChange(() => WaitAnimation);
            }
        }

        public Visibility WaitAnimation
        {
            get => _waitAnimation;
            set {
                _waitAnimation = value;
                NotifyOfPropertyChange(() => WaitAnimation);
            }
        }

        /// <summary>
        /// Button
        /// </summary>
        public void OpenImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = UiServices.Locale("ImageFilter"),
                Title = UiServices.Locale("BtnOpenImage"),
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
                UiServices.ShowError(ex);
            }
        }

        public bool CanFindObjects => Image != null && Status == UiServices.Locale("Ready");

        /// <summary>
        /// Button
        /// </summary>
        public async void FindObjects()
        {
            try
            {
                Status = UiServices.Locale("Processing");
                WaitAnimation = Visibility.Visible;

                // convert percents
                var threshold = Threshold / 100;

                var tuple = await SelectedYoloModel.FindObjects(Start.Clone(), threshold);

                // result image
                Finish = tuple.Item1;

                // show probabilities
                Info = tuple.Item2;

                Status = UiServices.Locale("Ready");
                WaitAnimation = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UiServices.ShowError(ex);
            }
        }
    }
}