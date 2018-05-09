using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private Mat _start;
        private Mat _finish;
        private WriteableBitmap _image;
        private string _filePath;
        private YoloModel _selectedYoloModel;
        private double _threshold = 30;
        private string _runtime = string.Format(UiServices.Locale("Runtime"), 0);
        private string _status = UiServices.Locale("Ready");
        private Visibility _waitAnimation = Visibility.Collapsed;
        private BindableCollection<string> _info = new BindableCollection<string>();
        private BindableCollection<Image> _loadedImages = new BindableCollection<Image>();
        private BindableCollection<YoloModel> _yoloModels = YoloModel.GetYoloModels();

        public Mat Start
        {
            get => _start;
            set
            {
                _start = value;
                Image = Start.ToWriteableBitmap(PixelFormats.Bgr24);
                ClearResult();
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
        public BindableCollection<YoloModel> YoloModels
        {
            get => _yoloModels;
            set => _yoloModels = value;
        }

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
                ClearResult();
            }
        }

        /// <summary>
        /// TextBox
        /// </summary>
        public BindableCollection<string> Info
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

        /// <summary>
        /// Visibility of BusyControl
        /// </summary>
        public Visibility WaitAnimation
        {
            get => _waitAnimation;
            set
            {
                _waitAnimation = value;
                NotifyOfPropertyChange(() => WaitAnimation);
            }
        }

        /// <summary>
        /// TextBlock
        /// </summary>
        public string Runtime
        {
            get => _runtime;
            set
            {
                _runtime = value;
                NotifyOfPropertyChange(() => Runtime);
            }
        }

        /// <summary>
        /// Images ListBox
        /// </summary>
        public BindableCollection<Image> LoadedImages
        {
            get => _loadedImages;
            set => _loadedImages = value;
        }

        /// <summary>
        /// Button
        /// </summary>
        public void OpenImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = UiServices.Locale("ImageFilter"),
                Title = UiServices.Locale("MenuOpen"),
                Multiselect = true,
            };
            var result = dlg.ShowDialog();

            if (!(result ?? false)) return;

            LoadedImages.Clear();

            foreach (var fileName in dlg.FileNames)
            {
                try
                {
                    Image img = new Image();
                    BitmapImage src = new BitmapImage();
                    src.BeginInit();
                    src.UriSource = new Uri(fileName, UriKind.Relative);
                    src.CacheOption = BitmapCacheOption.OnLoad;
                    src.EndInit();
                    img.Source = src;
                    img.Stretch = Stretch.UniformToFill;
                    img.MouseDown += Img_MouseDown;
                    img.Tag = fileName;
                    img.Cursor = Cursors.Hand;
                    LoadedImages.Add(img);
                }
                catch (Exception ex)
                {
                    UiServices.ShowError(ex);
                }
            }

            // load first image
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

        private void Img_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                FilePath = (string) (sender as Image)?.Tag;
                Start = new Mat(FilePath, ImreadModes.AnyDepth | ImreadModes.AnyColor);
            }
            catch (Exception ex)
            {
                UiServices.ShowError(ex);
            }
        }

        private void ClearResult()
        {
            if (Start != null)
            {
                Image = Start.ToWriteableBitmap(PixelFormats.Bgr24);
            }

            Info.Clear();
            Runtime = string.Format(UiServices.Locale("Runtime"), 0);
        }

        /// <summary>
        /// Enable|Disable FindObjects button
        /// </summary>
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

                ClearResult();

                var tuple = await SelectedYoloModel.FindObjects(Start.Clone(), threshold);

                // result image
                Finish = tuple.Item1;

                // show probabilities
                Info = new BindableCollection<string>(tuple.Item2);

                // show runtime
                Runtime = string.Format(UiServices.Locale("Runtime"), tuple.Item3);

                Status = UiServices.Locale("Ready");
                WaitAnimation = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UiServices.ShowError(ex);
            }
        }

        /// <summary>
        /// Button
        /// </summary>
        public void CloseApp()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Button
        /// </summary>
        public void About()
        {
            MessageBox.Show(UiServices.Locale("Author"), UiServices.Locale("MenuAbout"));
        }
    }
}