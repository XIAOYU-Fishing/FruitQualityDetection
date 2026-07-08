using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FruitQualityDetection.Core;
using FruitQualityDetection.Models;
using HalconDotNet;

namespace FruitQualityDetection.UI
{
    public partial class MainForm : Form
    {
        private HalconEngine _engine;
        private YoloDetector _yolo;
        private Calibrator _calibrator;
        private FruitAnalyzer _analyzer;
        private GradeClassifier _classifier;
        
        // 【关键修改】从 HSmartWindowControl 改为 HWindowControl
        private HWindowControl _hWindow;

        // 保存当前图片路径，供 OnnxRuntime 读取
        private string _currentImagePath;

        public MainForm()
        {
            InitializeComponent();
            InitHalconUI();
            InitCore();
        }

        private void InitHalconUI()
        {
            // 【关键修改】使用 HWindowControl 替代 HSmartWindowControl
            _hWindow = new HWindowControl { Dock = DockStyle.Fill };
            panelDisplay.Controls.Add(_hWindow);
            _hWindow.HalconWindow.SetWindowParam("background_color", "black");
        }

        private void InitCore()
        {
            _engine = new HalconEngine();
            _engine.Initialize(_hWindow.HalconWindow);

            _yolo = new YoloDetector();
            _calibrator = new Calibrator();
            _analyzer = new FruitAnalyzer(_calibrator);
            _classifier = new GradeClassifier();

            AppendLog("系统初始化完成");
            AppendLog("步骤1: 在图片中放入蓝色橡皮，输入尺寸后点击【开始标定】");
            AppendLog("步骤2: 点击【加载模型】选择 best.onnx");
            AppendLog("步骤3: 导入水果图片，点击【开始检测】");
        }

        // ========== 标定 ==========
        private void btnCalibrate_Click(object sender, EventArgs e)
        {
            if (_engine.CurrentImage == null) { MessageBox.Show("请先导入图像"); return; }
            if (!double.TryParse(txtEraserLength.Text, out double len) ||
                !double.TryParse(txtEraserWidth.Text, out double wid))
            { MessageBox.Show("请输入有效的橡皮实际长宽（毫米）"); return; }

            bool ok = _calibrator.CalibrateFromImage(_engine.CurrentImage, len, wid);
            if (ok)
            {
                lblScale.Text = $"比例尺: {_calibrator.ScaleMmPerPixel:F4} mm/pixel";
                AppendLog($"标定成功！比例尺: {_calibrator.ScaleMmPerPixel:F4} mm/pixel");
            }
            else
            {
                MessageBox.Show("未检测到橡皮。请确保：\n1. 橡皮在图像内\n2. 橡皮颜色与背景差异明显（推荐蓝色）\n3. 图像清晰");
            }
        }

        // ========== 导入图片 ==========
        private void btnImportImage_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "图像|*.jpg;*.png;*.bmp;*.tif" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _currentImagePath = dlg.FileName;
                _engine.ReadImage(dlg.FileName);
                AppendLog($"已加载: {Path.GetFileName(dlg.FileName)}");
            }
        }

        // ========== 加载模型 ==========
        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "ONNX模型|*.onnx" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _yolo.LoadModel(dlg.FileName);
                AppendLog("YOLO11 ONNX 模型加载成功！");
            }
        }

        // ========== 开始检测 ==========
        private void btnDetect_Click(object sender, EventArgs e)
        {
            if (_engine.CurrentImage == null) { MessageBox.Show("请先导入图像"); return; }
            if (!_calibrator.IsCalibrated) { MessageBox.Show("请先完成标定（放置橡皮并点击开始标定）"); return; }
            if (string.IsNullOrEmpty(_currentImagePath)) { MessageBox.Show("图像路径无效"); return; }

            _engine.ClearWindow();
            _engine.FitImageToWindow();

            var results = _yolo.Detect(_currentImagePath, _engine.ImageWidth, _engine.ImageHeight);

            listViewResults.Items.Clear();
            foreach (var r in results)
            {
                _analyzer.Analyze(r, _engine.CurrentImage);
                _classifier.Classify(r);
                _engine.DrawResult(r);

                var item = new ListViewItem(new[]
                {
                    r.FruitType, r.QualityGrade, r.DefectType,
                    $"{r.ActualLengthMm:F1}x{r.ActualWidthMm:F1}",
                    $"{r.DefectAreaRatio:F2}%", r.Confidence.ToString("P2")
                });
                listViewResults.Items.Add(item);
            }

            lblCount.Text = $"检测数量: {results.Count}";
            AppendLog($"检测完成，共 {results.Count} 个目标");
        }

        // ========== 导出结果 ==========
        private void btnExport_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"检测结果_{DateTime.Now:yyyyMMdd_HHmmss}" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var lines = new System.Collections.Generic.List<string>
                {
                    "水果类型,品质等级,缺陷类型,实际长度mm,实际宽度mm,缺陷占比%,置信度"
                };
                foreach (ListViewItem item in listViewResults.Items)
                {
                    lines.Add(string.Join(",", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
                }
                File.WriteAllLines(dlg.FileName, lines);
                AppendLog("结果已导出到: " + dlg.FileName);
            }
        }

        private void AppendLog(string msg)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        // ========== 关闭释放 ==========
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _yolo?.Dispose();
            _engine?.Dispose();
            base.OnFormClosing(e);
        }
    }
}