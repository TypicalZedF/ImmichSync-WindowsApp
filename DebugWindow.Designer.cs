namespace ImmichSyncApp
{
    partial class DebugWindow
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtDebug;

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
            this.txtDebug = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtDebug
            // 
            this.txtDebug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDebug.Multiline = true;
            this.txtDebug.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDebug.ReadOnly = true;
            this.txtDebug.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtDebug.Location = new System.Drawing.Point(0, 0);
            this.txtDebug.Name = "txtDebug";
            this.txtDebug.Size = new System.Drawing.Size(600, 400);
            this.txtDebug.TabIndex = 0;
            // 
            // DebugWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.txtDebug);
            this.Name = "DebugWindow";
            this.Text = "Connection Debug Output";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
