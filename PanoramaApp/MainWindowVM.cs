using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;

namespace PanoramaApp;

public partial class MainWindowVM : ObservableObject
{
    [ObservableProperty]
    private List<string> _files = new();
    [ObservableProperty]
    private bool _isPanorama = true;
    [ObservableProperty]
    private int _selectMode = 0;
    [ObservableProperty]
    private string _saveFileName = "pano";


    [RelayCommand]
    private void CreatePanorama()
    {
        try
        {
            var images = Files.Select(x => new Mat(x, ImreadModes.Unchanged));
            var stitcher = Stitcher.Create(SelectMode == 0 ? Stitcher.Mode.Panorama : Stitcher.Mode.Scans);
            using var pano = new Mat();
            stitcher.Stitch(images, pano);
            foreach (var image in images)
            {
                image.Dispose();
            }
            Cv2.ImWrite(Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(SaveFileName) + ".png"), pano);
            MessageBox.Show("成功", "合成できました", MessageBoxButton.OK, MessageBoxImage.Information);
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(SaveFileName) + ".png"),
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
        catch
        {
            MessageBox.Show("失敗", "合成できませんでした", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SelectFiles()
    {
        // Configure open file dialog box
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "画像ファイル|*.jepg;*.jpg;*.jpe;*.png;*.bmp;*.pbm,;*.pgm;*.ppm;*.sr;*.ras;*.jp2;*.tiff;*.tif|" +
            "Windowsビットマップ|*.bmp|ポータブル画像形式|*.pbm;*.pgm;*.ppm|Sunラスター|*.sr,*.ras|" +
            "JPEG|*.jepg;*.jpg;*.jpe|JPEG 2000|*.jp2|TIFF|*.tiff;*.tif|PNG|*.png"
        };

        // Show open file dialog box
        var result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result == true)
        {
            Files = Files.Concat(dialog.FileNames).ToList();
        }
    }
}
