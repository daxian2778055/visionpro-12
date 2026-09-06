namespace WindowsFormsApplication1
{
    partial class Form6
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.stackStrip1 = new Cognex.VisionPro.PMAlign.Implementation.Internal.StackStrip();
            this.cogToolBlockEdit1 = new Cognex.VisionPro.ToolBlock.CogToolBlockEditV2();
            this.Result_label = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).BeginInit();
            this.SuspendLayout();
            // 
            // stackStrip1
            // 
            this.stackStrip1.AutoSize = false;
            this.stackStrip1.CanOverflow = false;
            this.stackStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stackStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.stackStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.stackStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.stackStrip1.Location = new System.Drawing.Point(0, 0);
            this.stackStrip1.Name = "stackStrip1";
            this.stackStrip1.Size = new System.Drawing.Size(825, 488);
            this.stackStrip1.TabIndex = 0;
            this.stackStrip1.Text = "stackStrip1";
            // 
            // cogToolBlockEdit1
            // 
            this.cogToolBlockEdit1.AllowDrop = true;
            this.cogToolBlockEdit1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cogToolBlockEdit1.ContextMenuCustomizer = null;
            this.cogToolBlockEdit1.Location = new System.Drawing.Point(10, 10);
            this.cogToolBlockEdit1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cogToolBlockEdit1.MinimumSize = new System.Drawing.Size(391, 0);
            this.cogToolBlockEdit1.Name = "cogToolBlockEdit1";
            this.cogToolBlockEdit1.ShowNodeToolTips = true;
            this.cogToolBlockEdit1.Size = new System.Drawing.Size(806, 469);
            this.cogToolBlockEdit1.SuspendElectricRuns = false;
            this.cogToolBlockEdit1.TabIndex = 6;
            // 
            // Result_label
            // 
            this.Result_label.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Result_label.AutoSize = true;
            this.Result_label.Location = new System.Drawing.Point(672, 470);
            this.Result_label.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Result_label.Name = "Result_label";
            this.Result_label.Size = new System.Drawing.Size(41, 12);
            this.Result_label.TabIndex = 7;
            this.Result_label.Text = "label1";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 200;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form6
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(825, 488);
            this.Controls.Add(this.Result_label);
            this.Controls.Add(this.cogToolBlockEdit1);
            this.Controls.Add(this.stackStrip1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form6";
            this.Text = "Form6";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form6_FormClosing);
            this.Load += new System.EventHandler(this.Form6_Load);
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Cognex.VisionPro.PMAlign.Implementation.Internal.StackStrip stackStrip1;
        private Cognex.VisionPro.ToolBlock.CogToolBlockEditV2 cogToolBlockEdit1;
        private System.Windows.Forms.Label Result_label;
        private System.Windows.Forms.Timer timer1;
    }
}