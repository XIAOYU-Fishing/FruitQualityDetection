namespace FruitQualityDetection.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelTop, panelLeft, panelDisplay, panelRight, panelBottom;
        private System.Windows.Forms.Button btnImportImage, btnLoadModel, btnDetect, btnExport, btnCalibrate;
        private System.Windows.Forms.Label lblEraserLength, lblEraserWidth, lblScale;
        private System.Windows.Forms.TextBox txtEraserLength, txtEraserWidth;
        private System.Windows.Forms.ListView listViewResults;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.panelDisplay = new System.Windows.Forms.Panel();
            this.panelRight = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();

            this.btnImportImage = new System.Windows.Forms.Button();
            this.btnLoadModel = new System.Windows.Forms.Button();
            this.btnDetect = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCalibrate = new System.Windows.Forms.Button();

            this.lblEraserLength = new System.Windows.Forms.Label();
            this.lblEraserWidth = new System.Windows.Forms.Label();
            this.txtEraserLength = new System.Windows.Forms.TextBox();
            this.txtEraserWidth = new System.Windows.Forms.TextBox();
            this.lblScale = new System.Windows.Forms.Label();

            this.listViewResults = new System.Windows.Forms.ListView();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblCount = new System.Windows.Forms.Label();

            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Height = 50;
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(45, 62, 80);

            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Width = 200;
            this.panelLeft.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);

            System.Windows.Forms.Button[] buttons = { btnImportImage, btnLoadModel, btnDetect, btnExport, btnCalibrate };
            string[] texts = { "📷 导入图片", "📦 加载模型", "🔍 开始检测", "💾 导出结果", "📐 开始标定" };
            int y = 20;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Text = texts[i];
                buttons[i].Top = y; buttons[i].Left = 10;
                buttons[i].Width = 180; buttons[i].Height = 40;
                buttons[i].FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                buttons[i].ForeColor = System.Drawing.Color.White;
                buttons[i].BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
                buttons[i].Font = new System.Drawing.Font("微软雅黑", 10F);
                this.panelLeft.Controls.Add(buttons[i]);
                y += 50;
            }

            this.lblEraserLength.Text = "橡皮长(mm):"; this.lblEraserLength.ForeColor = System.Drawing.Color.White;
            this.lblEraserLength.Top = y; this.lblEraserLength.Left = 10; this.lblEraserLength.Width = 80;
            this.txtEraserLength.Text = "40"; this.txtEraserLength.Top = y; this.txtEraserLength.Left = 95; this.txtEraserLength.Width = 90;
            y += 30;
            this.lblEraserWidth.Text = "橡皮宽(mm):"; this.lblEraserWidth.ForeColor = System.Drawing.Color.White;
            this.lblEraserWidth.Top = y; this.lblEraserWidth.Left = 10; this.lblEraserWidth.Width = 80;
            this.txtEraserWidth.Text = "20"; this.txtEraserWidth.Top = y; this.txtEraserWidth.Left = 95; this.txtEraserWidth.Width = 90;
            y += 35;
            this.lblScale.Text = "比例尺: 未标定"; this.lblScale.ForeColor = System.Drawing.Color.Yellow;
            this.lblScale.Top = y; this.lblScale.Left = 10; this.lblScale.Width = 180;

            this.panelLeft.Controls.Add(lblEraserLength);
            this.panelLeft.Controls.Add(txtEraserLength);
            this.panelLeft.Controls.Add(lblEraserWidth);
            this.panelLeft.Controls.Add(txtEraserWidth);
            this.panelLeft.Controls.Add(lblScale);

            this.panelDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDisplay.BackColor = System.Drawing.Color.Black;

            this.panelRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelRight.Width = 400;
            this.panelRight.BackColor = System.Drawing.Color.FromArgb(236, 240, 241);

            this.listViewResults.View = System.Windows.Forms.View.Details;
            this.listViewResults.Dock = System.Windows.Forms.DockStyle.Top;
            this.listViewResults.Height = 350;
            this.listViewResults.Columns.Add("水果", 60);
            this.listViewResults.Columns.Add("等级", 60);
            this.listViewResults.Columns.Add("缺陷", 70);
            this.listViewResults.Columns.Add("尺寸(mm)", 90);
            this.listViewResults.Columns.Add("缺陷%", 60);
            this.listViewResults.Columns.Add("置信度", 70);
            this.listViewResults.FullRowSelect = true;
            this.listViewResults.GridLines = true;

            this.lblCount.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCount.Height = 30;
            this.lblCount.Text = "检测数量: 0";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.panelRight.Controls.Add(this.listViewResults);
            this.panelRight.Controls.Add(this.lblCount);

            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Height = 150;
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);

            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.ForeColor = System.Drawing.Color.LightGreen;
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.txtLog.ReadOnly = true;

            this.panelBottom.Controls.Add(this.txtLog);

            this.Controls.Add(this.panelDisplay);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.ClientSize = new System.Drawing.Size(1500, 900);
            this.Text = "水果质量检测系统 v1.0 | Halcon 25.11 + YOLO11";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.btnImportImage.Click += new System.EventHandler(this.btnImportImage_Click);
            this.btnLoadModel.Click += new System.EventHandler(this.btnLoadModel_Click);
            this.btnDetect.Click += new System.EventHandler(this.btnDetect_Click);
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            this.btnCalibrate.Click += new System.EventHandler(this.btnCalibrate_Click);

            this.ResumeLayout(false);
        }
    }
}