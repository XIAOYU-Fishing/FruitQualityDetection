using HalconDotNet;
using FruitQualityDetection.Models;
using System;

namespace FruitQualityDetection.Core
{
    public class HalconEngine : IDisposable
    {
        public HWindow HalconWindow { get; private set; }
        public HObject CurrentImage { get; private set; }
        public HTuple ImageWidth, ImageHeight;

        public void Initialize(HWindow window)
        {
            HalconWindow = window;
            HalconWindow.SetWindowParam("background_color", "black");
            HalconWindow.SetColor("green");
            HalconWindow.SetLineWidth(2);
            HalconWindow.SetFont("-Microsoft YaHei-16-*-0-*-*-1-");
        }

        public bool ReadImage(string path)
        {
            try
            {
                CurrentImage?.Dispose();
                HOperatorSet.ReadImage(out HObject img, path);
                CurrentImage = img;
                HOperatorSet.GetImageSize(CurrentImage, out ImageWidth, out ImageHeight);
                FitImageToWindow();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"图像读取失败: {ex.Message}");
            }
        }

        public HObject PreprocessImage(HObject image)
        {
            HObject r, g, b, h, s, v;
            HObject median, gauss, emphasize, edge;

            HOperatorSet.Decompose3(image, out r, out g, out b);
            HOperatorSet.TransFromRgb(r, g, b, out h, out s, out v, "hsv");

            HOperatorSet.MedianImage(image, out median, "circle", 3, "mirrored");
            HOperatorSet.GaussImage(median, out gauss, 5);
            HOperatorSet.Emphasize(gauss, out emphasize, 7, 7, 1.0);
            HOperatorSet.SobelAmp(emphasize, out edge, "sum_abs", 3);

            r.Dispose(); g.Dispose(); b.Dispose();
            h.Dispose(); s.Dispose(); v.Dispose();
            median.Dispose(); gauss.Dispose(); edge.Dispose();

            return emphasize;
        }

        public void FitImageToWindow()
        {
            if (CurrentImage == null || HalconWindow == null) return;
            // 【修复】强制转 int，避免 HTuple 重载歧义
            HalconWindow.SetPart(0, 0, (int)ImageHeight - 1, (int)ImageWidth - 1);
            HalconWindow.DispObj(CurrentImage);
        }

        public void ClearWindow()
        {
            HalconWindow?.ClearWindow();
        }

        public void DrawResult(DetectionResult r)
        {
            string color = r.QualityGrade switch
            {
                "优等品" => "green",
                "一等品" => "cyan",
                "次品" => "orange",
                _ => "red"
            };

            int row1 = (int)r.BoundingBox.Top;
            int col1 = (int)r.BoundingBox.Left;
            int row2 = (int)r.BoundingBox.Bottom;
            int col2 = (int)r.BoundingBox.Right;

            HalconWindow.SetColor(color);
            HalconWindow.SetLineWidth(3);
            // 【修复】强制转 double，避免重载歧义
            HalconWindow.DispRectangle1((double)row1, (double)col1, (double)row2, (double)col2);

            HalconWindow.SetTposition(row1 - 60, col1);
            HalconWindow.WriteString($"{r.FruitType} | {r.QualityGrade}");
            HalconWindow.SetTposition(row1 - 40, col1);
            HalconWindow.WriteString($"尺寸:{r.ActualLengthMm:F1}x{r.ActualWidthMm:F1}mm");
            HalconWindow.SetTposition(row1 - 20, col1);
            HalconWindow.WriteString($"缺陷:{r.DefectAreaRatio:F1}% 置信:{r.Confidence:P0}");
        }

        public void Dispose()
        {
            CurrentImage?.Dispose();
        }
    }
}