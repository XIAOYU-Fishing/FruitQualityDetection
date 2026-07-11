using System;
using System.Drawing;

namespace FruitQualityDetection.Models
{
    public class DetectionResult
    {
        public string FruitType { get; set; }
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
        public double ActualLengthMm { get; set; }
        public double ActualWidthMm { get; set; }
        public double DefectAreaRatio { get; set; }
        public string DefectType { get; set; }
        public string QualityGrade { get; set; }
        public DateTime DetectTime { get; set; }
    }
}