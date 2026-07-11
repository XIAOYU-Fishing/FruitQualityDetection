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

                HObject eraserRegion, connected, candidates, eraser;

                // 第一步：颜色提取（H:15-90 覆盖黄到青绿）
                HOperatorSet.Threshold(h, out eraserRegion, 15, 90);

                HOperatorSet.Connection(eraserRegion, out connected);

                HOperatorSet.SelectShape(connected, out candidates,
                    new HTuple("area").TupleConcat("rectangularity"),
                    "and",
                    new HTuple(300).TupleConcat(0.5),
                    new HTuple(50000).TupleConcat(1.0));

                HTuple count;
                HOperatorSet.CountObj(candidates, out count);

                // 第二步：回退到灰度+面积
                if (count.I == 0)
                {
                    HObject gray;
                    HOperatorSet.Rgb1ToGray(image, out gray);
                    HOperatorSet.BinaryThreshold(gray, out eraserRegion, "max_separability", "dark", out _);
                    HOperatorSet.Connection(eraserRegion, out connected);
                    HOperatorSet.SelectShape(connected, out candidates,
                        new HTuple("area").TupleConcat("rectangularity"),
                        "and",
                        new HTuple(300).TupleConcat(0.5),
                        new HTuple(50000).TupleConcat(1.0));
                    HOperatorSet.CountObj(candidates, out count);
                }

                if (count.I == 0)
                {
                    r.Dispose(); g.Dispose(); b.Dispose();
                    h.Dispose(); s.Dispose(); v.Dispose();
                    return false;
                }

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