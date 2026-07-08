using HalconDotNet;
using System;

namespace FruitQualityDetection.Core
{
    public class Calibrator
    {
        public double ScaleMmPerPixel { get; private set; } = 0;
        public bool IsCalibrated => ScaleMmPerPixel > 0;

        public bool CalibrateFromImage(HObject image, double physicalLength, double physicalWidth)
        {
            try
            {
                HObject r, g, b, h, s, v;
                HOperatorSet.Decompose3(image, out r, out g, out b);
                HOperatorSet.TransFromRgb(r, g, b, out h, out s, out v, "hsv");

                // 蓝色橡皮 H: 100-140（Halcon 0-255范围）
                // 红色橡皮改为 threshold(h, 0, 20) 或 (220, 255)
                // 绿色橡皮改为 threshold(h, 60, 90)
                HObject eraserRegion;
                HOperatorSet.Threshold(h, out eraserRegion, 100, 140);

                HObject connected;
                HOperatorSet.Connection(eraserRegion, out connected);

                HObject candidates;
                HOperatorSet.SelectShape(connected, out candidates,
                    new HTuple("area").TupleConcat("rectangularity"),
                    "and",
                    new HTuple(1000).TupleConcat(0.7),
                    new HTuple(50000).TupleConcat(1.0));

                HObject eraser;
                HOperatorSet.SelectShapeStd(candidates, out eraser, "max_area", 70);

                HTuple row, col, phi, length1, length2;
                HOperatorSet.SmallestRectangle2(eraser, out row, out col, out phi, out length1, out length2);

                double pixelLength = length1.D * 2.0;
                double pixelWidth = length2.D * 2.0;

                double scaleX = physicalLength / pixelLength;
                double scaleY = physicalWidth / pixelWidth;
                ScaleMmPerPixel = (scaleX + scaleY) / 2.0;

                r.Dispose(); g.Dispose(); b.Dispose();
                h.Dispose(); s.Dispose(); v.Dispose();
                eraserRegion.Dispose(); connected.Dispose();
                candidates.Dispose(); eraser.Dispose();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}