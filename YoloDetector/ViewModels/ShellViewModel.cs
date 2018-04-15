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
            Finish = SelectedYoloModel.FindObjects(Start.Clone(), 0.3, out var infoText);
            Info = infoText;
        }
    }
}