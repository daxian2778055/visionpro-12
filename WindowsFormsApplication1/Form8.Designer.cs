namespace WindowsFormsApplication1
{
    partial class Form8
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
            this.Result_label = new System.Windows.Forms.Label();
            this.cogToolBlockEdit1 = new Cognex.VisionPro.ToolBlock.CogToolBlockEditV2();
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).BeginInit();
            this.SuspendLayout();
            // 
            // Result_label
            // 
            this.Result_label.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Result_label.AutoSize = true;
            this.Result_label.Location = new System.Drawing.Point(841, 585);
            this.Result_label.Name = "Result_label";
            this.Result_label.Size = new System.Drawing.Size(55, 15);
            this.Result_label.TabIndex = 9;
            this.Result_label.Text = "label1";
            // 
            // cogToolBlockEdit1
            // 
            this.cogToolBlockEdit1.AllowDrop = true;
            this.cogToolBlockEdit1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cogToolBlockEdit1.ContextMenuCustomizer = null;
            this.cogToolBlockEdit1.Location = new System.Drawing.Point(13, 10);
            this.cogToolBlockEdit1.MinimumSize = new System.Drawing.Size(489, 0);
            this.cogToolBlockEdit1.Name = "cogToolBlockEdit1";
            this.cogToolBlockEdit1.ShowNodeToolTips = true;
            this.cogToolBlockEdit1.Size = new System.Drawing.Size(1007, 586);
            this.cogToolBlockEdit1.SuspendElectricRuns = false;
            this.cogToolBlockEdit1.TabIndex = 8;
            // 
            // Form8
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1032, 610);
            this.Controls.Add(this.Result_label);
            this.Controls.Add(this.cogToolBlockEdit1);
            this.Name = "Form8";
            this.Text = "Form8";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form8_FormClosing);
            this.Load += new System.EventHandler(this.Form8_Load);
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Result_label;
        private Cognex.VisionPro.ToolBlock.CogToolBlockEditV2 cogToolBlockEdit1;
    }
}