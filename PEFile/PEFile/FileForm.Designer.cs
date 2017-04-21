namespace PEFile
{
    partial class FileForm
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
            this.TxtSummary = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // TxtSummary
            // 
            this.TxtSummary.Enabled = false;
            this.TxtSummary.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TxtSummary.Location = new System.Drawing.Point(15, 15);
            this.TxtSummary.Multiline = true;
            this.TxtSummary.Name = "TxtSummary";
            this.TxtSummary.Size = new System.Drawing.Size(732, 590);
            this.TxtSummary.TabIndex = 0;
            // 
            // FileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(773, 620);
            this.Controls.Add(this.TxtSummary);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.Name = "FileForm";
            this.Text = "MainForm";
            this.Load += new System.EventHandler(this.FileForm_Load);
            this.Resize += new System.EventHandler(this.FileForm_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TxtSummary;
    }
}