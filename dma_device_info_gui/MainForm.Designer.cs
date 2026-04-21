namespace dma_device_info_gui
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTop = new System.Windows.Forms.Label();
            this.cboDevices = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnQuery = new System.Windows.Forms.Button();
            this.btnImportJson = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.lblNote = new System.Windows.Forms.Label();
            this.lblKernelStatus = new System.Windows.Forms.Label();
            this.btnSetupEnv = new System.Windows.Forms.Button();
            this.btnExportReport = new System.Windows.Forms.Button();
            this.btnRestartNow = new System.Windows.Forms.Button();
            this.btnRestartLater = new System.Windows.Forms.Button();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.lblChecklistTitle = new System.Windows.Forms.Label();
            this.gridChecklist = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblOutputTitle = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridChecklist)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTop
            // 
            this.lblTop.AutoSize = true;
            this.lblTop.Location = new System.Drawing.Point(12, 13);
            this.lblTop.Name = "lblTop";
            this.lblTop.Size = new System.Drawing.Size(126, 17);
            this.lblTop.TabIndex = 0;
            this.lblTop.Text = "硬件列表（PCI 设备）";
            // 
            // cboDevices
            // 
            this.cboDevices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cboDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDevices.FormattingEnabled = true;
            this.cboDevices.Location = new System.Drawing.Point(15, 37);
            this.cboDevices.Name = "cboDevices";
            this.cboDevices.Size = new System.Drawing.Size(938, 25);
            this.cboDevices.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.Location = new System.Drawing.Point(961, 36);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(86, 28);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "刷新列表";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // btnQuery
            // 
            this.btnQuery.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnQuery.Location = new System.Drawing.Point(1053, 36);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(86, 28);
            this.btnQuery.TabIndex = 3;
            this.btnQuery.Text = "采集所选";
            this.btnQuery.UseVisualStyleBackColor = true;
            this.btnQuery.Click += new System.EventHandler(this.BtnQuery_Click);
            // 
            // btnImportJson
            // 
            this.btnImportJson.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImportJson.Location = new System.Drawing.Point(1145, 36);
            this.btnImportJson.Name = "btnImportJson";
            this.btnImportJson.Size = new System.Drawing.Size(86, 28);
            this.btnImportJson.TabIndex = 4;
            this.btnImportJson.Text = "导入 JSON";
            this.btnImportJson.UseVisualStyleBackColor = true;
            this.btnImportJson.Click += new System.EventHandler(this.BtnImportJson_Click);
            // 
            // btnCopy
            // 
            this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopy.Location = new System.Drawing.Point(1237, 36);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(86, 28);
            this.btnCopy.TabIndex = 5;
            this.btnCopy.Text = "复制结果";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.BtnCopy_Click);
            // 
            // lblNote
            // 
            this.lblNote.AutoSize = true;
            this.lblNote.Location = new System.Drawing.Point(12, 73);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(596, 17);
            this.lblNote.TabIndex = 6;
            this.lblNote.Text = "本工具支持 WMI 查询与内核 IOCTL 采集，可导出 TXT + JSON，并支持一键环境安装与重启。";
            // 
            // lblKernelStatus
            // 
            this.lblKernelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblKernelStatus.BackColor = System.Drawing.Color.Firebrick;
            this.lblKernelStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblKernelStatus.ForeColor = System.Drawing.Color.White;
            this.lblKernelStatus.Location = new System.Drawing.Point(871, 68);
            this.lblKernelStatus.Name = "lblKernelStatus";
            this.lblKernelStatus.Size = new System.Drawing.Size(180, 27);
            this.lblKernelStatus.TabIndex = 7;
            this.lblKernelStatus.Text = "内核环境：检测中";
            this.lblKernelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnSetupEnv
            // 
            this.btnSetupEnv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetupEnv.Location = new System.Drawing.Point(1057, 68);
            this.btnSetupEnv.Name = "btnSetupEnv";
            this.btnSetupEnv.Size = new System.Drawing.Size(82, 27);
            this.btnSetupEnv.TabIndex = 8;
            this.btnSetupEnv.Text = "环境安装";
            this.btnSetupEnv.UseVisualStyleBackColor = true;
            this.btnSetupEnv.Click += new System.EventHandler(this.BtnSetupEnv_Click);
            // 
            // btnExportReport
            // 
            this.btnExportReport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportReport.Location = new System.Drawing.Point(1145, 68);
            this.btnExportReport.Name = "btnExportReport";
            this.btnExportReport.Size = new System.Drawing.Size(86, 27);
            this.btnExportReport.TabIndex = 9;
            this.btnExportReport.Text = "导出报告";
            this.btnExportReport.UseVisualStyleBackColor = true;
            this.btnExportReport.Click += new System.EventHandler(this.BtnExportReport_Click);
            // 
            // btnRestartNow
            // 
            this.btnRestartNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestartNow.Enabled = false;
            this.btnRestartNow.Location = new System.Drawing.Point(1237, 68);
            this.btnRestartNow.Name = "btnRestartNow";
            this.btnRestartNow.Size = new System.Drawing.Size(86, 27);
            this.btnRestartNow.TabIndex = 10;
            this.btnRestartNow.Text = "立即重启";
            this.btnRestartNow.UseVisualStyleBackColor = true;
            this.btnRestartNow.Click += new System.EventHandler(this.BtnRestartNow_Click);
            // 
            // btnRestartLater
            // 
            this.btnRestartLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestartLater.Enabled = false;
            this.btnRestartLater.Location = new System.Drawing.Point(1237, 101);
            this.btnRestartLater.Name = "btnRestartLater";
            this.btnRestartLater.Size = new System.Drawing.Size(86, 27);
            this.btnRestartLater.TabIndex = 11;
            this.btnRestartLater.Text = "稍后重启";
            this.btnRestartLater.UseVisualStyleBackColor = true;
            this.btnRestartLater.Click += new System.EventHandler(this.BtnRestartLater_Click);
            // 
            // splitMain
            // 
            this.splitMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitMain.Location = new System.Drawing.Point(15, 133);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.lblChecklistTitle);
            this.splitMain.Panel1.Controls.Add(this.gridChecklist);
            this.splitMain.Panel1MinSize = 170;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.lblOutputTitle);
            this.splitMain.Panel2.Controls.Add(this.txtOutput);
            this.splitMain.Panel2MinSize = 180;
            this.splitMain.Size = new System.Drawing.Size(1308, 619);
            this.splitMain.SplitterDistance = 250;
            this.splitMain.TabIndex = 12;
            // 
            // lblChecklistTitle
            // 
            this.lblChecklistTitle.AutoSize = true;
            this.lblChecklistTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblChecklistTitle.Location = new System.Drawing.Point(0, 3);
            this.lblChecklistTitle.Name = "lblChecklistTitle";
            this.lblChecklistTitle.Size = new System.Drawing.Size(198, 19);
            this.lblChecklistTitle.TabIndex = 0;
            this.lblChecklistTitle.Text = "采集清单（仅目标项）";
            // 
            // gridChecklist
            // 
            this.gridChecklist.AllowUserToAddRows = false;
            this.gridChecklist.AllowUserToDeleteRows = false;
            this.gridChecklist.AllowUserToResizeRows = false;
            this.gridChecklist.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridChecklist.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridChecklist.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItem,
            this.colStatus,
            this.colValue,
            this.colReason});
            this.gridChecklist.Location = new System.Drawing.Point(0, 25);
            this.gridChecklist.MultiSelect = false;
            this.gridChecklist.Name = "gridChecklist";
            this.gridChecklist.ReadOnly = true;
            this.gridChecklist.RowHeadersVisible = false;
            this.gridChecklist.RowTemplate.Height = 24;
            this.gridChecklist.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridChecklist.Size = new System.Drawing.Size(1308, 225);
            this.gridChecklist.TabIndex = 1;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "采集项目";
            this.colItem.MinimumWidth = 120;
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colItem.Width = 260;
            // 
            // colStatus
            // 
            this.colStatus.HeaderText = "采集状态";
            this.colStatus.MinimumWidth = 80;
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colStatus.Width = 90;
            // 
            // colValue
            // 
            this.colValue.HeaderText = "实际采集值";
            this.colValue.MinimumWidth = 220;
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            this.colValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colValue.Width = 420;
            // 
            // colReason
            // 
            this.colReason.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colReason.HeaderText = "失败原因";
            this.colReason.MinimumWidth = 200;
            this.colReason.Name = "colReason";
            this.colReason.ReadOnly = true;
            this.colReason.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // lblOutputTitle
            // 
            this.lblOutputTitle.AutoSize = true;
            this.lblOutputTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblOutputTitle.Location = new System.Drawing.Point(0, 3);
            this.lblOutputTitle.Name = "lblOutputTitle";
            this.lblOutputTitle.Size = new System.Drawing.Size(86, 19);
            this.lblOutputTitle.TabIndex = 0;
            this.lblOutputTitle.Text = "详细报告";
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(0, 25);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtOutput.Size = new System.Drawing.Size(1308, 340);
            this.txtOutput.TabIndex = 1;
            this.txtOutput.WordWrap = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1336, 768);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.btnRestartLater);
            this.Controls.Add(this.btnRestartNow);
            this.Controls.Add(this.btnExportReport);
            this.Controls.Add(this.btnSetupEnv);
            this.Controls.Add(this.lblKernelStatus);
            this.Controls.Add(this.lblNote);
            this.Controls.Add(this.btnImportJson);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.btnQuery);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.cboDevices);
            this.Controls.Add(this.lblTop);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设备信息采集演示（课程作业版）";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridChecklist)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblTop;
        private System.Windows.Forms.ComboBox cboDevices;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.Button btnImportJson;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Label lblKernelStatus;
        private System.Windows.Forms.Button btnSetupEnv;
        private System.Windows.Forms.Button btnExportReport;
        private System.Windows.Forms.Button btnRestartNow;
        private System.Windows.Forms.Button btnRestartLater;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Label lblChecklistTitle;
        private System.Windows.Forms.DataGridView gridChecklist;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReason;
        private System.Windows.Forms.Label lblOutputTitle;
        private System.Windows.Forms.TextBox txtOutput;
    }
}
