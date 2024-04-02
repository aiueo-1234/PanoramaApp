using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCvSharp;

namespace PanoramaApp;

public partial class MainWindowVM : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _files = new();
    [ObservableProperty]
    private int _selectMode = 0;
    [ObservableProperty]
    private Visibility _isOverRapperEnabled = Visibility.Hidden;

    [RelayCommand]
    private async Task CreatePanorama()
    {
        IsOverRapperEnabled = Visibility.Visible;
        IEnumerable<Mat> images = new List<Mat>();
        try
        {
            images = Files.Select(x => Cv2.ImRead(x));
            using var stitcher = Stitcher.Create(SelectMode == 0 ? Stitcher.Mode.Panorama : Stitcher.Mode.Scans);
            using var pano = new Mat();
            var code = await Task.Run(()=>stitcher.Stitch(images, pano));
            if (code == Stitcher.Status.OK)
            {
                var dlg = new SaveFileDialog
                {
                    FileName = "Panorama",
                    DefaultExt = ".png",
                    Filter = "画像ファイル|*.bmp;*.dib;*.jepg;*.jpg;*.jpe;*.png;*.sr;*.ras;*.pbm;*.pgm;*.ppm;*.pxm;*.pnm;*.tiff;*.tif;*.hdr;*.pic|" +
                    "Windowsビットマップ|*.bmp;*.dib|JPEG|*.jepg;*.jpg;*.jpe|PNG|*.png|Sun rasters|*.sr;*.ras|ポータブル画像形式|*.pbm;*.pgm;*.ppm;*.pxm;*.pnm|" +
                    "TIFF|*.tiff;*.tif|ラディアンスHDR|*.hdr;*.pic"
                };
                var result = dlg.ShowDialog();
                if (result == true)
                {
                    var isFileSaved = Cv2.ImWrite(dlg.FileName, pano);
                    if (isFileSaved)
                    {
                        MessageBox.Show("合成できました", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        var processStartInfo = new ProcessStartInfo()
                        {
                            FileName = dlg.FileName,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                    }
                    else
                    {
                        MessageBox.Show("画像を保存できませんでした", "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (code == Stitcher.Status.ErrorNeedMoreImgs)
            {
                MessageBox.Show("画像が足りませんでした", "失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (code == Stitcher.Status.ErrorHomographyEstFail)
            {
                MessageBox.Show("ホモグラフィーの推定に失敗しました\n特徴点を多く含む画像を選択してください", "失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (code == Stitcher.Status.ErrorCameraParamsAdjustFail)
            {
                MessageBox.Show("合成できませんでした", "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (OpenCvSharpException ex)
        {
            if (ex.Message == "imread failed.")
            {
                MessageBox.Show("画像を読み込めませんでした。", "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#if DEBUG
            throw;
#else
            MessageBox.Show("合成できませんでした", "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
#endif  
        }
        catch
        {
#if DEBUG
            throw;
#else
            MessageBox.Show("合成できませんでした", "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
#endif  
        }
        finally
        {
            foreach (var image in images)
            {
                image?.Dispose();
            }
            IsOverRapperEnabled = Visibility.Hidden;
        }
    }

    [RelayCommand]
    private void SelectFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "画像ファイル|*.bmp;*.dib;*.jepg;*.jpg;*.jpe;*.png;*.sr;*.ras;*.pbm;*.pgm;*.ppm;*.pxm;*.pnm;*.tiff;*.tif;*.hdr;*.pic|" +
            "Windowsビットマップ|*.bmp;*.dib|JPEG|*.jepg;*.jpg;*.jpe|PNG|*.png|Sun rasters|*.sr;*.ras|ポータブル画像形式|*.pbm;*.pgm;*.ppm;*.pxm;*.pnm|" +
            "TIFF|*.tiff;*.tif|ラディアンスHDR|*.hdr;*.pic"
        };
        var result = dialog.ShowDialog();
        if (result == true)
        {
            Files = new(Files.Concat(dialog.FileNames).Distinct());
        }
    }

    [RelayCommand]
    private void RemoveFile(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        Files.Remove(fileName);
    }
}
