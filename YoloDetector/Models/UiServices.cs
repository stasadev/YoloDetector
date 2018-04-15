using System;
using System.Windows;

namespace YoloDetector.Models
{
    public static class UiServices
    {
        public static void ShowError(Exception ex)
        {
            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            MessageBox.Show(
                ex.Message,
                Locale("Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public static string Locale(string param)
        {
            return (string) Application.Current.FindResource(param);
        }
    }
}