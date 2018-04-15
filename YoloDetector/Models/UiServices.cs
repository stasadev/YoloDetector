using System;
using System.Windows;
using OpenCvSharp;

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

        public static Scalar GetReadableForeColor(Scalar c)
        {
            return (int) Math.Sqrt(
                       c.Val0 * c.Val0 * .299 +
                       c.Val1 * c.Val1 * .587 +
                       c.Val2 * c.Val2 * .114) > 130
                ? Scalar.Black
                : Scalar.White;
        }
    }
}