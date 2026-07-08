using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FruitQualityDetection.Models;

namespace FruitQualityDetection.Core
{
    public class YoloDetector : IDisposable
    {
        private InferenceSession _session;
        private readonly string[] _classNames = { "苹果", "香蕉", "橘子" };

        public void LoadModel(string onnxPath)
        {
            if (!System.IO.File.Exists(onnxPath))
                throw new System.IO.FileNotFoundException("ONNX模型不存在", onnxPath);

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(onnxPath, options);
        }

        public List<DetectionResult> Detect(string imagePath, double origWidth, double origHeight)
        {
            var results = new List<DetectionResult>();

            // 1. 预处理图像
            var inputTensor = PreprocessImage(imagePath);
            
            // 2. 推理
            var inputName = _session.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };
            
            using var outputs = _session.Run(inputs);
            var outputTensor = outputs.First().AsTensor<float>();
            
            // 3. 解析输出 [1, 7, 8400]
            int numAnchors = outputTensor.Dimensions[2];
            int numClasses = outputTensor.Dimensions[1] - 4;
            
            var candidates = new List<(float x1, float y1, float x2, float y2, float conf, int classId)>();

            for (int i = 0; i < numAnchors; i++)
            {
                float cx = outputTensor[0, 0, i];
                float cy = outputTensor[0, 1, i];
                float w = outputTensor[0, 2, i];
                float h = outputTensor[0, 3, i];

                float maxCls = 0;
                int clsId = 0;
                for (int c = 0; c < numClasses; c++)
                {
                    float p = outputTensor[0, 4 + c, i];
                    if (p > maxCls) { maxCls = p; clsId = c; }
                }

                float confidence = maxCls;
                if (confidence < 0.25f) continue;
                if (clsId >= _classNames.Length) continue;

                float x1_640 = cx - w / 2;
                float y1_640 = cy - h / 2;
                float x2_640 = cx + w / 2;
                float y2_640 = cy + h / 2;

                float scaleX = (float)origWidth / 640f;
                float scaleY = (float)origHeight / 640f;

                candidates.Add((
                    x1_640 * scaleX, y1_640 * scaleY,
                    x2_640 * scaleX, y2_640 * scaleY,
                    confidence, clsId));
            }

            // 4. NMS
            var finalBoxes = Nms(candidates, 0.45f);

            foreach (var b in finalBoxes)
            {
                results.Add(new DetectionResult
                {
                    FruitType = _classNames[b.classId],
                    BoundingBox = new RectangleF(b.x1, b.y1, b.x2 - b.x1, b.y2 - b.y1),
                    Confidence = b.conf,
                    DetectTime = DateTime.Now
                });
            }

            return results;
        }

        private DenseTensor<float> PreprocessImage(string imagePath)
        {
            using var bitmap = new Bitmap(imagePath);
            using var resized = new Bitmap(bitmap, new Size(640, 640));

            var tensor = new DenseTensor<float>(new[] { 1, 3, 640, 640 });

            for (int y = 0; y < 640; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    var pixel = resized.GetPixel(x, y);
                    tensor[0, 0, y, x] = pixel.R / 255.0f;
                    tensor[0, 1, y, x] = pixel.G / 255.0f;
                    tensor[0, 2, y, x] = pixel.B / 255.0f;
                }
            }

            return tensor;
        }

        private List<(float x1, float y1, float x2, float y2, float conf, int classId)> Nms(
            List<(float x1, float y1, float x2, float y2, float conf, int classId)> boxes, float thresh)
        {
            var result = new List<(float x1, float y1, float x2, float y2, float conf, int classId)>();
            var sorted = boxes.OrderByDescending(b => b.conf).ToList();

            while (sorted.Count > 0)
            {
                var current = sorted[0];
                result.Add(current);
                sorted.RemoveAt(0);

                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    if (sorted[i].classId != current.classId) continue;
                    if (IoU(current, sorted[i]) > thresh)
                        sorted.RemoveAt(i);
                }
            }
            return result;
        }

        private float IoU((float x1, float y1, float x2, float y2, float conf, int classId) a,
                         (float x1, float y1, float x2, float y2, float conf, int classId) b)
        {
            float interX1 = Math.Max(a.x1, b.x1);
            float interY1 = Math.Max(a.y1, b.y1);
            float interX2 = Math.Min(a.x2, b.x2);
            float interY2 = Math.Min(a.y2, b.y2);
            float interArea = Math.Max(0, interX2 - interX1) * Math.Max(0, interY2 - interY1);
            float areaA = (a.x2 - a.x1) * (a.y2 - a.y1);
            float areaB = (b.x2 - b.x1) * (b.y2 - b.y1);
            return interArea / (areaA + areaB - interArea + 1e-6f);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}