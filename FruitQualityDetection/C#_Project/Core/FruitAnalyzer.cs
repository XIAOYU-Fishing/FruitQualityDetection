using HalconDotNet;
using System.Linq;
using FruitQualityDetection.Models;

namespace FruitQualityDetection.Core
{
    public class FruitAnalyzer
    {
        private Calibrator _calibrator;

        public FruitAnalyzer(Calibrator calibrator)
        {
            _calibrator = calibrator;
        }

        public void Analyze(DetectionResult result, HObject fullImage)
        {
            int r1 = (int)result.BoundingBox.Top;
            int c1 = (int)result.BoundingBox.Left;
            int r2 = (int)result.BoundingBox.Bottom;
            int c2 = (int)result.BoundingBox.Right;

            HObject roi;
            HOperatorSet.CropRectangle1(fullImage, out roi, r1, c1, r2, c2);

            MeasureSize(result, roi);
            AnalyzeDefect(result, roi);

            roi.Dispose();
        }

        private void MeasureSize(DetectionResult result, HObject roi)
        {
            if (!_calibrator.IsCalibrated) return;

            var spec = System.Array.Find(FruitSpec.Standards, s => s.Name == result.FruitType);
            if (spec == null) return;

            if (spec.UseCircleMeasure)
            {
                HTuple row, col, radius;
                HOperatorSet.SmallestCircle(roi, out row, out col, out radius);
                double diameterPx = radius.D * 2.0;
                result.ActualLengthMm = diameterPx * _calibrator.ScaleMmPerPixel;
                result.ActualWidthMm = result.ActualLengthMm;
            }
            else
            {
                HTuple row, col, phi, length1, length2;
                HOperatorSet.SmallestRectangle2(roi, out row, out col, out phi, out length1, out length2);
                result.ActualLengthMm = length1.D * 2.0 * _calibrator.ScaleMmPerPixel;
                result.ActualWidthMm = length2.D * 2.0 * _calibrator.ScaleMmPerPixel;
            }
        }

        private void AnalyzeDefect(DetectionResult result, HObject roi)
        {
            HObject r, g, b, h, s, v;
            HOperatorSet.Decompose3(roi, out r, out g, out b);
            HOperatorSet.TransFromRgb(r, g, b, out h, out s, out v, "hsv");

            HObject defectH, defectS, defectUnion;
            HOperatorSet.Threshold(h, out defectH, 0, 30);
            HOperatorSet.Threshold(s, out defectS, 0, 40);

            HOperatorSet.Union2(defectH, defectS, out defectUnion);
            HObject connected;
            HOperatorSet.Connection(defectUnion, out connected);

            HObject defectClean;
            HOperatorSet.SelectShape(connected, out defectClean, "area", "and", 15, 999999);

            HObject defectFinal;
            HOperatorSet.OpeningCircle(defectClean, out defectFinal, 2.0);
            HObject defectClosed;
            HOperatorSet.ClosingCircle(defectFinal, out defectClosed, 3.0);

            HTuple defectArea, totalArea;
            HOperatorSet.AreaCenter(defectClosed, out defectArea, out _, out _);
            HOperatorSet.AreaCenter(roi, out totalArea, out _, out _);

            result.DefectAreaRatio = totalArea.I > 0
                ? (double)defectArea.I / (double)totalArea.I * 100.0
                : 0;

            if (result.DefectAreaRatio < 0.5)
                result.DefectType = "无缺陷";
            else if (result.DefectAreaRatio < 3.0)
                result.DefectType = "轻微斑点";
            else if (result.DefectAreaRatio < 8.0)
                result.DefectType = "明显瑕疵";
            else
                result.DefectType = "严重腐烂/损伤";

            r.Dispose(); g.Dispose(); b.Dispose();
            h.Dispose(); s.Dispose(); v.Dispose();
            defectH.Dispose(); defectS.Dispose(); defectUnion.Dispose();
            connected.Dispose(); defectClean.Dispose(); defectFinal.Dispose(); defectClosed.Dispose();
        }
    }
}