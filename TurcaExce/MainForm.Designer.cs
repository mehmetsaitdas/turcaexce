namespace TurcaExce
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnCompany = new Button();
            btnSupport = new Button();
            btnManualOrder = new Button();
            btnPrint = new Button();
            lblSource = new Label();
            txtSourcePath = new TextBox();
            btnBrowseSource = new Button();
            lblTarget = new Label();
            txtTargetPath = new TextBox();
            btnBrowseTarget = new Button();
            btnConvert = new Button();
            lblStatus = new Label();
            lblEmail = new Label();
            txtEmail = new TextBox();
            btnSendEmail = new Button();
            btnOutlookLogin = new Button();
            lblMailStatus = new Label();
            gridPreview = new DataGridView();
            lblWarnings = new Label();
            txtWarnings = new TextBox();
            lblSettingsFile = new Label();
            btnEmaHistory = new Button();
            ((System.ComponentModel.ISupportInitialize)gridPreview).BeginInit();
            SuspendLayout();
            // 
            // btnCompany
            // 
            btnCompany.Location = new Point(12, 11);
            btnCompany.Name = "btnCompany";
            btnCompany.Size = new Size(300, 25);
            btnCompany.TabIndex = 17;
            btnCompany.Text = "Firma: ...";
            btnCompany.TextAlign = ContentAlignment.MiddleLeft;
            btnCompany.UseVisualStyleBackColor = true;
            btnCompany.Click += btnCompany_Click;
            // 
            // btnSupport
            // 
            btnSupport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSupport.Location = new Point(858, 11);
            btnSupport.Name = "btnSupport";
            btnSupport.Size = new Size(114, 25);
            btnSupport.TabIndex = 18;
            btnSupport.Text = "Destek";
            btnSupport.UseVisualStyleBackColor = true;
            btnSupport.Click += btnSupport_Click;
            // 
            // btnManualOrder
            // 
            btnManualOrder.Location = new Point(320, 114);
            btnManualOrder.Name = "btnManualOrder";
            btnManualOrder.Size = new Size(205, 32);
            btnManualOrder.TabIndex = 19;
            btnManualOrder.Text = "Elle Üretim Emri Gir";
            btnManualOrder.UseVisualStyleBackColor = true;
            btnManualOrder.Click += btnManualOrder_Click;
            // 
            // btnPrint
            // 
            btnPrint.Location = new Point(535, 114);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(134, 32);
            btnPrint.TabIndex = 20;
            btnPrint.Text = "Yazdır";
            btnPrint.UseVisualStyleBackColor = true;
            btnPrint.Click += btnPrint_Click;
            // 
            // lblSource
            // 
            lblSource.AutoSize = true;
            lblSource.Location = new Point(12, 52);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(101, 15);
            lblSource.TabIndex = 0;
            lblSource.Text = "Üretim emri (.xls):";
            // 
            // txtSourcePath
            // 
            txtSourcePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSourcePath.Location = new Point(130, 48);
            txtSourcePath.Name = "txtSourcePath";
            txtSourcePath.Size = new Size(716, 23);
            txtSourcePath.TabIndex = 1;
            // 
            // btnBrowseSource
            // 
            btnBrowseSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseSource.Location = new Point(858, 47);
            btnBrowseSource.Name = "btnBrowseSource";
            btnBrowseSource.Size = new Size(114, 25);
            btnBrowseSource.TabIndex = 2;
            btnBrowseSource.Text = "Gözat...";
            btnBrowseSource.UseVisualStyleBackColor = true;
            btnBrowseSource.Click += btnBrowseSource_Click;
            // 
            // lblTarget
            // 
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(12, 84);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(68, 15);
            lblTarget.TabIndex = 3;
            lblTarget.Text = "Çıktı (.xlsx):";
            // 
            // txtTargetPath
            // 
            txtTargetPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtTargetPath.Location = new Point(130, 80);
            txtTargetPath.Name = "txtTargetPath";
            txtTargetPath.Size = new Size(716, 23);
            txtTargetPath.TabIndex = 4;
            // 
            // btnBrowseTarget
            // 
            btnBrowseTarget.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseTarget.Location = new Point(858, 79);
            btnBrowseTarget.Name = "btnBrowseTarget";
            btnBrowseTarget.Size = new Size(114, 25);
            btnBrowseTarget.TabIndex = 5;
            btnBrowseTarget.Text = "Gözat...";
            btnBrowseTarget.UseVisualStyleBackColor = true;
            btnBrowseTarget.Click += btnBrowseTarget_Click;
            // 
            // btnConvert
            // 
            btnConvert.Location = new Point(130, 114);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(180, 32);
            btnConvert.TabIndex = 6;
            btnConvert.Text = "Dönüştür";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Location = new Point(645, 114);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(327, 32);
            lblStatus.TabIndex = 7;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(12, 158);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(50, 15);
            lblEmail.TabIndex = 12;
            lblEmail.Text = "E-posta:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(130, 154);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "alici@ornek.com";
            txtEmail.Size = new Size(241, 23);
            txtEmail.TabIndex = 13;
            // 
            // btnSendEmail
            // 
            btnSendEmail.Location = new Point(391, 153);
            btnSendEmail.Name = "btnSendEmail";
            btnSendEmail.Size = new Size(134, 25);
            btnSendEmail.TabIndex = 14;
            btnSendEmail.Text = "E-posta Gönder";
            btnSendEmail.UseVisualStyleBackColor = true;
            btnSendEmail.Click += btnSendEmail_Click;
            // 
            // btnOutlookLogin
            // 
            btnOutlookLogin.Location = new Point(685, 153);
            btnOutlookLogin.Name = "btnOutlookLogin";
            btnOutlookLogin.Size = new Size(140, 25);
            btnOutlookLogin.TabIndex = 15;
            btnOutlookLogin.Text = "Outlook ile Giriş Yap";
            btnOutlookLogin.UseVisualStyleBackColor = true;
            btnOutlookLogin.Click += btnOutlookLogin_Click;
            // 
            // lblMailStatus
            // 
            lblMailStatus.AutoSize = true;
            lblMailStatus.ForeColor = SystemColors.GrayText;
            lblMailStatus.Location = new Point(831, 157);
            lblMailStatus.Name = "lblMailStatus";
            lblMailStatus.Size = new Size(141, 15);
            lblMailStatus.TabIndex = 16;
            lblMailStatus.Text = "(Outlook girişi yapılmadı)";
            // 
            // gridPreview
            // 
            gridPreview.AllowUserToAddRows = false;
            gridPreview.AllowUserToDeleteRows = false;
            gridPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            gridPreview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridPreview.Location = new Point(12, 188);
            gridPreview.Name = "gridPreview";
            gridPreview.ReadOnly = true;
            gridPreview.RowHeadersVisible = false;
            gridPreview.Size = new Size(960, 330);
            gridPreview.TabIndex = 8;
            // 
            // lblWarnings
            // 
            lblWarnings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblWarnings.AutoSize = true;
            lblWarnings.Location = new Point(12, 528);
            lblWarnings.Name = "lblWarnings";
            lblWarnings.Size = new Size(50, 15);
            lblWarnings.TabIndex = 9;
            lblWarnings.Text = "Uyarılar:";
            // 
            // txtWarnings
            // 
            txtWarnings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtWarnings.Location = new Point(12, 546);
            txtWarnings.Multiline = true;
            txtWarnings.Name = "txtWarnings";
            txtWarnings.ReadOnly = true;
            txtWarnings.ScrollBars = ScrollBars.Vertical;
            txtWarnings.Size = new Size(960, 90);
            txtWarnings.TabIndex = 10;
            // 
            // lblSettingsFile
            // 
            lblSettingsFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSettingsFile.AutoEllipsis = true;
            lblSettingsFile.ForeColor = SystemColors.GrayText;
            lblSettingsFile.Location = new Point(12, 642);
            lblSettingsFile.Name = "lblSettingsFile";
            lblSettingsFile.Size = new Size(960, 15);
            lblSettingsFile.TabIndex = 11;
            // 
            // btnEmaHistory
            // 
            btnEmaHistory.Location = new Point(535, 153);
            btnEmaHistory.Name = "btnEmaHistory";
            btnEmaHistory.Size = new Size(134, 25);
            btnEmaHistory.TabIndex = 21;
            btnEmaHistory.Text = "Gönderim Geçmişi";
            btnEmaHistory.UseVisualStyleBackColor = true;
            btnEmaHistory.Click += btnEmaHistory_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 667);
            Controls.Add(btnEmaHistory);
            Controls.Add(btnCompany);
            Controls.Add(btnSupport);
            Controls.Add(lblSource);
            Controls.Add(txtSourcePath);
            Controls.Add(btnBrowseSource);
            Controls.Add(lblTarget);
            Controls.Add(txtTargetPath);
            Controls.Add(btnBrowseTarget);
            Controls.Add(btnConvert);
            Controls.Add(btnManualOrder);
            Controls.Add(btnPrint);
            Controls.Add(lblStatus);
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);
            Controls.Add(btnSendEmail);
            Controls.Add(btnOutlookLogin);
            Controls.Add(lblMailStatus);
            Controls.Add(gridPreview);
            Controls.Add(lblWarnings);
            Controls.Add(txtWarnings);
            Controls.Add(lblSettingsFile);
            MinimumSize = new Size(800, 536);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TurcaExce — Üretim Emri → Ürün Girişi Dönüştürücü";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnCompany;
        private Button btnSupport;
        private Button btnManualOrder;
        private Button btnPrint;
        private Label lblSource;
        private TextBox txtSourcePath;
        private Button btnBrowseSource;
        private Label lblTarget;
        private TextBox txtTargetPath;
        private Button btnBrowseTarget;
        private Button btnConvert;
        private Label lblStatus;
        private Label lblEmail;
        private TextBox txtEmail;
        private Button btnSendEmail;
        private Button btnOutlookLogin;
        private Label lblMailStatus;
        private DataGridView gridPreview;
        private Label lblWarnings;
        private TextBox txtWarnings;
        private Label lblSettingsFile;
        private Button btnEmaHistory;
    }
}
