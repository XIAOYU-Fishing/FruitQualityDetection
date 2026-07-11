using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using FruitQualityDetection.Core;
using FruitQualityDetection.Models;
using HalconDotNet;
using OpenCvSharp;

namespace FruitQualityDetection.UI
{
    public partial class MainForm : Form
    {
        private HalconEngine _engine;
        private YoloDetector _yolo;
        private Calibrator _calibrator;
        private FruitAnalyzer _analyzer;
        private GradeClassifier _classifier;
        private HWindowControl _hWindow;
        private string _currentImagePath;

        private List<string> _batchImagePaths = new List<string>();
        private bool _isBatchMode = false;

        // 分页显示相关
        private int _currentPage = 0;
        private const int THUMBNAIL_COUNT = 6;
        private List<PictureBox> _thumbnails = new List<PictureBox>();
        private Button _btnPrevPage, _btnNextPage;
        private Label _lblPageInfo;
        private Button _btnBackToGrid;
        private List<string> _annotatedPaths = new List<string>();

        // 视频相关成员
        private VideoCapture _videoCapture;
        private Thread _videoThread;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _realtimeDetect = false;
        private double _videoFps = 30;
        private int _frameIntervalMs = 33;
        private readonly object _frameLock = new object();
        private Mat _currentFrame;

        public MainForm()
        {
            InitializeComponent();
            InitHalconUI();
            InitCore();
        }

        private void InitHalconUI()
        {
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

            // 创建返回网格按钮（右上角）
            _btnBackToGrid = new Button
            {
                Text = "← 返回网格",
                Width = 100,
                Height = 32,
                Top = 8,
                Left = panelDisplay.Width - 115,
                Visible = false,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F)
            };
            _btnBackToGrid.Click += (s, e) =>
            {
                ShowThumbnailGrid();
                AppendLog("已返回网格视图");
            };
            panelDisplay.Controls.Add(_btnBackToGrid);
            _btnBackToGrid.BringToFront();

            AppendLog("系统初始化完成");
            AppendLog("【图片模式】导入图片 → 标定 → 加载模型 → 开始检测");
            AppendLog("【视频模式】导入视频 → 标定 → 加载模型 → 勾选实时检测 → 播放");
            AppendLog("提示：多选图片可分页显示（每页6张），点击缩略图放大，点击右上角返回");
        }

        // ========== 标定（修复：批量模式自动加载当前图） ==========
        private void btnCalibrate_Click(object sender, EventArgs e)
        {
            // 批量模式下，如果引擎没有图像，自动加载当前页第一张
            if (_isBatchMode && _batchImagePaths.Count > 0 && _engine.CurrentImage == null)
            {
                int idx = _currentPage * THUMBNAIL_COUNT;
                if (idx < _batchImagePaths.Count)
                {
                    _engine.ReadImage(_batchImagePaths[idx]);
                    _currentImagePath = _batchImagePaths[idx];
                }
            }

            if (_engine.CurrentImage == null) { MessageBox.Show("请先导入图像或视频"); return; }
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
                MessageBox.Show("未检测到橡皮。请确保：\n1. 橡皮在图像内\n2. 橡皮颜色与背景差异明显\n3. 图像清晰");
            }
        }

        // ========== 图片导入（支持单张/多选，修复：批量自动加载第一张） ==========
        private void btnImportImage_Click(object sender, EventArgs e)
        {
            StopVideo();

            using var dlg = new OpenFileDialog
            {
                Filter = "图像|*.jpg;*.jpeg;*.png;*.bmp;*.tif",
                Multiselect = true
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            _batchImagePaths.Clear();
            _annotatedPaths.Clear();

            if (dlg.FileNames.Length == 1)
            {
                _isBatchMode = false;
                _currentPage = 0;
                ShowSingleWindow();
                _currentImagePath = dlg.FileName;
                _engine.ReadImage(dlg.FileName);
                AppendLog($"已加载图片: {Path.GetFileName(dlg.FileName)}");
            }
            else
            {
                _isBatchMode = true;
                _batchImagePaths.AddRange(dlg.FileNames);
                _currentPage = 0;

                // 关键修复：加载第一张图到引擎，用于标定
                _engine.ReadImage(_batchImagePaths[0]);
                _currentImagePath = _batchImagePaths[0];

                ShowThumbnailGrid();
                AppendLog($"已导入 {_batchImagePaths.Count} 张图片，点击【开始检测】批量处理");
            }
        }

        // ========== 显示模式切换 ==========
        private void ShowSingleWindow()
        {
            foreach (var pb in _thumbnails) pb.Visible = false;
            if (_btnPrevPage != null) _btnPrevPage.Visible = false;
            if (_btnNextPage != null) _btnNextPage.Visible = false;
            if (_lblPageInfo != null) _lblPageInfo.Visible = false;
            _hWindow.Visible = true;
            _hWindow.Dock = DockStyle.Fill;
            _btnBackToGrid.Visible = true;
            _btnBackToGrid.BringToFront();
        }

        private void ShowThumbnailGrid()
        {
            _hWindow.Visible = false;
            _btnBackToGrid.Visible = false;
            if (_thumbnails.Count == 0) CreateThumbnailGrid();
            _btnPrevPage.Visible = true;
            _btnNextPage.Visible = true;
            _lblPageInfo.Visible = true;
            LoadPage(_currentPage);
        }

        private void CreateThumbnailGrid()
        {
            int cols = 3;
            int rows = 2;
            int margin = 5;

            int cellW = (panelDisplay.Width - margin * (cols + 1)) / cols;
            int cellH = (panelDisplay.Height - margin * (rows + 1) - 35) / rows;

            for (int i = 0; i < THUMBNAIL_COUNT; i++)
            {
                var pb = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black,
                    BorderStyle = BorderStyle.FixedSingle,
                    Visible = false
                };

                int col = i % cols;
                int row = i / cols;
                pb.Left = margin + col * (cellW + margin);
                pb.Top = margin + row * (cellH + margin);
                pb.Width = cellW;
                pb.Height = cellH;

                pb.Click += (s, e) =>
                {
                    int idx = _thumbnails.IndexOf((PictureBox)s);
                    int globalIdx = _currentPage * THUMBNAIL_COUNT + idx;
                    if (globalIdx < _batchImagePaths.Count)
                    {
                        _currentImagePath = _batchImagePaths[globalIdx];
                        _engine.ReadImage(_currentImagePath);
                        ShowSingleWindow();
                        AppendLog($"已放大查看: {Path.GetFileName(_currentImagePath)}");
                    }
                };

                panelDisplay.Controls.Add(pb);
                _thumbnails.Add(pb);
            }

            _btnPrevPage = new Button
            {
                Text = "◀ 上一页",
                Width = 80,
                Height = 28,
                Top = panelDisplay.Height - 32,
                Left = panelDisplay.Width / 2 - 90,
                Visible = false,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnPrevPage.Click += (s, e) => { if (_currentPage > 0) { _currentPage--; LoadPage(_currentPage); } };

            _btnNextPage = new Button
            {
                Text = "下一页 ▶",
                Width = 80,
                Height = 28,
                Top = panelDisplay.Height - 32,
                Left = panelDisplay.Width / 2 + 10,
                Visible = false,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnNextPage.Click += (s, e) =>
            {
                int maxPage = (_batchImagePaths.Count - 1) / THUMBNAIL_COUNT;
                if (_currentPage < maxPage) { _currentPage++; LoadPage(_currentPage); }
            };

            _lblPageInfo = new Label
            {
                Text = "第 1 页",
                ForeColor = Color.White,
                Top = panelDisplay.Height - 28,
                Left = panelDisplay.Width / 2 - 20,
                AutoSize = true,
                Visible = false
            };

            panelDisplay.Controls.Add(_btnPrevPage);
            panelDisplay.Controls.Add(_btnNextPage);
            panelDisplay.Controls.Add(_lblPageInfo);
        }

        private void LoadPage(int page)
        {
            int startIdx = page * THUMBNAIL_COUNT;
            int endIdx = Math.Min(startIdx + THUMBNAIL_COUNT, _batchImagePaths.Count);

            for (int i = 0; i < THUMBNAIL_COUNT; i++)
            {
                int globalIdx = startIdx + i;
                if (globalIdx < _batchImagePaths.Count)
                {
                    _thumbnails[i].Visible = true;
                    string displayPath = (globalIdx < _annotatedPaths.Count && !string.IsNullOrEmpty(_annotatedPaths[globalIdx]))
                        ? _annotatedPaths[globalIdx]
                        : _batchImagePaths[globalIdx];
                    _thumbnails[i].Image = Image.FromFile(displayPath);
                }
                else
                {
                    _thumbnails[i].Visible = false;
                    _thumbnails[i].Image = null;
                }
            }

            int maxPage = Math.Max(0, (_batchImagePaths.Count - 1) / THUMBNAIL_COUNT);
            _lblPageInfo.Text = $"第 {page + 1} / {maxPage + 1} 页";
            _btnPrevPage.Enabled = page > 0;
            _btnNextPage.Enabled = page < maxPage;
        }

        // ========== 模型加载 ==========
        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "ONNX模型|*.onnx" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _yolo.LoadModel(dlg.FileName);
                AppendLog("YOLO11 ONNX 模型加载成功！");
            }
        }

        // ========== 检测（修复：批量模式不检查 CurrentImage） ==========
        private void btnDetect_Click(object sender, EventArgs e)
        {
            if (!_calibrator.IsCalibrated) { MessageBox.Show("请先完成标定"); return; }
            if (_yolo == null || !_yolo.IsLoaded)
            {
                MessageBox.Show("请先加载模型！\n\n点击【加载模型】选择 best.onnx", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 批量模式：直接检测，不检查 CurrentImage
            if (_isBatchMode && _batchImagePaths.Count > 0)
            {
                BatchDetect();
                return;
            }

            // 单张模式
            if (_engine.CurrentImage == null) { MessageBox.Show("请先导入图像"); return; }
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

        // ========== 批量检测（修复：DumpWindow 2参数） ==========
        private void BatchDetect()
        {
            AppendLog($"批量检测开始，共 {_batchImagePaths.Count} 张...");
            listViewResults.Items.Clear();
            int totalDetected = 0;
            _annotatedPaths.Clear();

            ShowThumbnailGrid();

            for (int i = 0; i < _batchImagePaths.Count; i++)
            {
                var imgPath = _batchImagePaths[i];

                if (!_engine.ReadImage(imgPath))
                {
                    AppendLog($"跳过: {Path.GetFileName(imgPath)}");
                    _annotatedPaths.Add(null);
                    continue;
                }

                var results = _yolo.Detect(imgPath, _engine.ImageWidth, _engine.ImageHeight);

                _engine.ClearWindow();
                _engine.FitImageToWindow();

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
                    totalDetected++;
                }

                // 保存 Halcon 窗口标注截图（2参数版本）
                string annotatedPath = Path.GetTempPath() + $"annotated_{i}_{Guid.NewGuid():N}.jpg";
                _hWindow.HalconWindow.DumpWindow("jpeg", annotatedPath);
                _annotatedPaths.Add(annotatedPath);

                // 如果当前页正在显示，实时更新缩略图
                if (i >= _currentPage * THUMBNAIL_COUNT && i < (_currentPage + 1) * THUMBNAIL_COUNT)
                {
                    int pageIdx = i % THUMBNAIL_COUNT;
                    if (_thumbnails[pageIdx].Image != null) _thumbnails[pageIdx].Image.Dispose();
                    _thumbnails[pageIdx].Image = Image.FromFile(annotatedPath);
                }

                AppendLog($"[{i + 1}/{_batchImagePaths.Count}] {Path.GetFileName(imgPath)}: {results.Count} 个目标");
                Application.DoEvents();
            }

            lblCount.Text = $"检测数量: {totalDetected}";
            AppendLog($"批量检测完成，总计 {totalDetected} 个目标");
            MessageBox.Show($"批量检测完成！\n共 {_batchImagePaths.Count} 张图片\n检测到 {totalDetected} 个目标", "完成");
        }

        // ========== 视频导入 ==========
        private void btnImportVideo_Click(object sender, EventArgs e)
        {
            StopVideo();
            using var dlg = new OpenFileDialog { Filter = "视频|*.mp4;*.avi;*.mov;*.mkv;*.wmv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            _videoCapture = new VideoCapture(dlg.FileName);
            if (!_videoCapture.IsOpened())
            {
                MessageBox.Show("无法打开视频文件");
                return;
            }

            _videoFps = _videoCapture.Fps;
            if (_videoFps <= 0) _videoFps = 30;
            _frameIntervalMs = (int)(1000.0 / _videoFps);

            AppendLog($"视频已加载: {Path.GetFileName(dlg.FileName)}");
            AppendLog($"分辨率: {_videoCapture.FrameWidth}x{_videoCapture.FrameHeight}, FPS: {_videoFps:F1}");

            using var firstFrame = new Mat();
            if (_videoCapture.Read(firstFrame) && !firstFrame.Empty())
            {
                DisplayMatToHalcon(firstFrame);
                string tempPath = Path.GetTempPath() + "video_first_frame.jpg";
                firstFrame.SaveImage(tempPath);
                _currentImagePath = tempPath;
                AppendLog("已显示视频首帧，请进行标定");
            }
        }

        // ========== 播放/暂停 ==========
        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            if (_videoCapture == null || !_videoCapture.IsOpened())
            {
                MessageBox.Show("请先导入视频");
                return;
            }

            if (_isPlaying && !_isPaused)
            {
                _isPaused = true;
                btnPlayPause.Text = "▶ 继续播放";
                AppendLog("视频已暂停");
            }
            else if (_isPlaying && _isPaused)
            {
                _isPaused = false;
                btnPlayPause.Text = "⏸ 暂停";
                AppendLog("视频继续播放");
            }
            else
            {
                if (!_calibrator.IsCalibrated)
                {
                    MessageBox.Show("请先完成标定");
                    return;
                }
                if (_yolo == null || !_yolo.IsLoaded)
                {
                    MessageBox.Show("请先加载模型");
                    return;
                }

                _isPlaying = true;
                _isPaused = false;
                btnPlayPause.Text = "⏸ 暂停";
                btnStopVideo.Enabled = true;

                _videoThread = new Thread(VideoLoop);
                _videoThread.IsBackground = true;
                _videoThread.Start();
                AppendLog("视频开始播放...");
            }
        }

        // ========== 停止视频 ==========
        private void btnStopVideo_Click(object sender, EventArgs e)
        {
            StopVideo();
            AppendLog("视频已停止");
        }

        private void StopVideo()
        {
            _isPlaying = false;
            _isPaused = false;
            _videoThread?.Join(500);
            _videoCapture?.Release();
            _videoCapture = null;

            if (btnPlayPause != null) btnPlayPause.Text = "▶ 播放";
            if (btnStopVideo != null) btnStopVideo.Enabled = false;
        }

        // ========== 实时检测开关 ==========
        private void chkRealtimeDetect_CheckedChanged(object sender, EventArgs e)
        {
            _realtimeDetect = chkRealtimeDetect.Checked;
            AppendLog(_realtimeDetect ? "实时检测已开启" : "实时检测已关闭");
        }

        // ========== 视频播放线程 ==========
        private void VideoLoop()
        {
            int frameCount = 0;
            while (_isPlaying && _videoCapture != null && _videoCapture.IsOpened())
            {
                if (_isPaused)
                {
                    Thread.Sleep(50);
                    continue;
                }

                using var frame = new Mat();
                if (!_videoCapture.Read(frame) || frame.Empty())
                {
                    _isPlaying = false;
                    break;
                }

                frameCount++;
                lock (_frameLock)
                {
                    _currentFrame = frame.Clone();
                }

                if (_realtimeDetect)
                {
                    string tempPath = Path.GetTempPath() + $"video_frame_{frameCount}.jpg";
                    frame.SaveImage(tempPath);

                    this.Invoke(new Action(() =>
                    {
                        try
                        {
                            _engine.ClearWindow();
                            DisplayMatToHalcon(frame);

                            var results = _yolo.Detect(tempPath, _engine.ImageWidth, _engine.ImageHeight);

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
                            AppendLog($"[帧{frameCount}] 检测到 {results.Count} 个目标");
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[帧{frameCount}] 检测异常: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        DisplayMatToHalcon(frame);
                    }));
                }

                Thread.Sleep(_frameIntervalMs);
            }

            this.Invoke(new Action(() =>
            {
                AppendLog("视频播放结束");
                btnPlayPause.Text = "▶ 播放";
                btnStopVideo.Enabled = false;
                _isPlaying = false;
            }));
        }

        // ========== Mat → Halcon HObject ==========
        private void DisplayMatToHalcon(Mat frame)
        {
            Mat[] channels = Cv2.Split(frame);

            byte[] rData = new byte[frame.Rows * frame.Cols];
            byte[] gData = new byte[frame.Rows * frame.Cols];
            byte[] bData = new byte[frame.Rows * frame.Cols];

            channels[2].GetArray(out rData);
            channels[1].GetArray(out gData);
            channels[0].GetArray(out bData);

            foreach (var ch in channels) ch.Dispose();

            GCHandle handleR = GCHandle.Alloc(rData, GCHandleType.Pinned);
            GCHandle handleG = GCHandle.Alloc(gData, GCHandleType.Pinned);
            GCHandle handleB = GCHandle.Alloc(bData, GCHandleType.Pinned);

            try
            {
                HObject halconImage;
                HOperatorSet.GenImage3(out halconImage, "byte", frame.Cols, frame.Rows,
                    handleR.AddrOfPinnedObject(),
                    handleG.AddrOfPinnedObject(),
                    handleB.AddrOfPinnedObject());

                _engine.CurrentImage?.Dispose();
                _engine.CurrentImage = halconImage;
                _engine.ImageWidth = frame.Cols;
                _engine.ImageHeight = frame.Rows;
                _engine.FitImageToWindow();
            }
            finally
            {
                handleR.Free();
                handleG.Free();
                handleB.Free();
            }
        }

        // ========== 导出结果 ==========
        private void btnExport_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"检测结果_{DateTime.Now:yyyyMMdd_HHmmss}" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var lines = new List<string>
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
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(msg)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopVideo();
            _yolo?.Dispose();
            _engine?.Dispose();
            base.OnFormClosing(e);
        }
    }
}