using System.Linq;
using FruitQualityDetection.Models;

namespace FruitQualityDetection.Core
{
    public class GradeClassifier
    {
        public void Classify(DetectionResult r)
        {
            var spec = FruitSpec.Standards.FirstOrDefault(s => s.Name == r.FruitType);
            bool sizeOk = false;
            if (spec != null)
            {
                sizeOk = r.ActualLengthMm >= spec.MinLenMm && r.ActualLengthMm <= spec.MaxLenMm
                      && r.ActualWidthMm >= spec.MinWidMm && r.ActualWidthMm <= spec.MaxWidMm;
            }

            if (r.DefectAreaRatio < 1.0 && sizeOk && r.Confidence > 0.9f)
                r.QualityGrade = "优等品";
            else if (r.DefectAreaRatio < 3.0 && (sizeOk || r.Confidence > 0.8f))
                r.QualityGrade = "一等品";
            else if (r.DefectAreaRatio < 10.0)
                r.QualityGrade = "次品";
            else
                r.QualityGrade = "不合格品";
        }
    }
}