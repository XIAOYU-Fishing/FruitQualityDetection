namespace FruitQualityDetection.Models
{
    public class FruitSpec
    {
        public string Name { get; set; }
        public double MinLenMm { get; set; }
        public double MaxLenMm { get; set; }
        public double MinWidMm { get; set; }
        public double MaxWidMm { get; set; }
        public bool UseCircleMeasure { get; set; }

        public static FruitSpec[] Standards => new[]
        {
            new FruitSpec { Name = "苹果", MinLenMm = 65, MaxLenMm = 90, MinWidMm = 65, MaxWidMm = 90, UseCircleMeasure = true },
            new FruitSpec { Name = "香蕉", MinLenMm = 150, MaxLenMm = 220, MinWidMm = 25, MaxWidMm = 40, UseCircleMeasure = false },
            new FruitSpec { Name = "橘子", MinLenMm = 55, MaxLenMm = 80, MinWidMm = 55, MaxWidMm = 80, UseCircleMeasure = true }
        };
    }
}