namespace WindowsFormsApplication1
{
    partial class Form10
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
            this.cogToolBlockEdit1 = new Cognex.VisionPro.ToolBlock.CogToolBlockEditV2();
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).BeginInit();
            this.SuspendLayout();
            // 
            // cogToolBlockEdit1
            // 
            this.cogToolBlockEdit1.AllowDrop = true;
            this.cogToolBlockEdit1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cogToolBlockEdit1.ContextMenuCustomizer = null;
            this.cogToolBlockEdit1.Location = new System.Drawing.Point(2, 2);
            this.cogToolBlockEdit1.Margin = new System.Windows.Forms.Padding(2);
            this.cogToolBlockEdit1.MinimumSize = new System.Drawing.Size(367, 0);
            this.cogToolBlockEdit1.Name = "cogToolBlockEdit1";
            this.cogToolBlockEdit1.ShowNodeToolTips = true;
            this.cogToolBlockEdit1.Size = new System.Drawing.Size(787, 461);
            this.cogToolBlockEdit1.SuspendElectricRuns = false;
            this.cogToolBlockEdit1.TabIndex = 9;
            // 
            // Form10
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 464);
            this.Controls.Add(this.cogToolBlockEdit1);
            this.Name = "Form10";
            this.Text = "Form10";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form10_FormClosing);
            this.Load += new System.EventHandler(this.Form10_Load);
            ((System.ComponentModel.ISupportInitialize)(this.cogToolBlockEdit1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Cognex.VisionPro.ToolBlock.CogToolBlockEditV2 cogToolBlockEdit1;
    }
}