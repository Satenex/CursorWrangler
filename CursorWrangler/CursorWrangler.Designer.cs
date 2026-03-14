using System.Windows.Forms;

namespace CursorWrangler
{
    partial class CursorWrangler
    {
        private System.ComponentModel.IContainer components = null;

        private Button ToggleButton;
        private Label StatusLabel;
		private CheckBox AlwaysOnTopCheckBox;
        private CheckBox LaunchOnStartupCheckBox;
        private CheckBox StartMinimizedCheckBox;
        private CheckBox MinimizeOnCloseCheckBox;
		private Panel BottomSeparator;
		private CheckBox ForceEnglishCheckBox;
		private CheckBox debugCheckbox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.ToggleButton = new Button();
            this.StatusLabel = new Label();
            this.LaunchOnStartupCheckBox = new CheckBox();
            this.StartMinimizedCheckBox = new CheckBox();
            this.MinimizeOnCloseCheckBox = new CheckBox();
			this.debugCheckbox = new System.Windows.Forms.CheckBox();

            this.SuspendLayout();

            //
            // ToggleButton
            //
            this.ToggleButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.ToggleButton.Location = new System.Drawing.Point(30, 15);
            this.ToggleButton.Name = "ToggleButton";
            this.ToggleButton.Size = new System.Drawing.Size(200, 45);
            this.ToggleButton.TabIndex = 0;
            this.ToggleButton.Text = "Toggle";
            this.ToggleButton.UseVisualStyleBackColor = true;
            this.ToggleButton.BackColor = System.Drawing.Color.LightGray;
            this.ToggleButton.Click += new System.EventHandler(this.ToggleButton_Click);

            //
            // StatusLabel
            //
            this.StatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.StatusLabel.Location = new System.Drawing.Point(10, 65);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(240, 28);
            this.StatusLabel.TabIndex = 1;
            this.StatusLabel.Text = "Waiting for focus";
            this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.StatusLabel.ForeColor = System.Drawing.Color.DarkOrange;
					
			// 
			// AlwaysOnTopCheckBox
			// 
			this.AlwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
			this.AlwaysOnTopCheckBox.AutoSize = true;
			this.AlwaysOnTopCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.AlwaysOnTopCheckBox.Location = new System.Drawing.Point(30, 105);
			this.AlwaysOnTopCheckBox.Name = "AlwaysOnTopCheckBox";
			this.AlwaysOnTopCheckBox.Size = new System.Drawing.Size(187, 17);
			this.AlwaysOnTopCheckBox.TabIndex = 5;
			this.AlwaysOnTopCheckBox.Text = "Always on top";
			this.AlwaysOnTopCheckBox.UseVisualStyleBackColor = true;
			this.AlwaysOnTopCheckBox.CheckedChanged += new System.EventHandler(this.AlwaysOnTopCheckBox_CheckedChanged);

            //
            // LaunchOnStartupCheckBox
            //
            this.LaunchOnStartupCheckBox.AutoSize = true;
            this.LaunchOnStartupCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.LaunchOnStartupCheckBox.Location = new System.Drawing.Point(30, 132);
            this.LaunchOnStartupCheckBox.Name = "LaunchOnStartupCheckBox";
            this.LaunchOnStartupCheckBox.Size = new System.Drawing.Size(145, 21);
            this.LaunchOnStartupCheckBox.TabIndex = 2;
            this.LaunchOnStartupCheckBox.Text = "Launch at startup";
            this.LaunchOnStartupCheckBox.UseVisualStyleBackColor = true;

            //
            // StartMinimizedCheckBox
            //
            this.StartMinimizedCheckBox.AutoSize = true;
            this.StartMinimizedCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.StartMinimizedCheckBox.Location = new System.Drawing.Point(30, 159);
            this.StartMinimizedCheckBox.Name = "StartMinimizedCheckBox";
            this.StartMinimizedCheckBox.Size = new System.Drawing.Size(130, 21);
            this.StartMinimizedCheckBox.TabIndex = 3;
            this.StartMinimizedCheckBox.Text = "Start minimized";
            this.StartMinimizedCheckBox.UseVisualStyleBackColor = true;

            //
            // MinimizeOnCloseCheckBox
            //
            this.MinimizeOnCloseCheckBox.AutoSize = true;
            this.MinimizeOnCloseCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.MinimizeOnCloseCheckBox.Location = new System.Drawing.Point(30, 186);
            this.MinimizeOnCloseCheckBox.Name = "MinimizeOnCloseCheckBox";
            this.MinimizeOnCloseCheckBox.Size = new System.Drawing.Size(176, 21);
            this.MinimizeOnCloseCheckBox.TabIndex = 4;
            this.MinimizeOnCloseCheckBox.Text = "Minimize to tray on close";
            this.MinimizeOnCloseCheckBox.UseVisualStyleBackColor = true;

			// 
			// BottomSeparator
			// 
			this.BottomSeparator = new System.Windows.Forms.Panel();
			this.BottomSeparator.BackColor = System.Drawing.Color.Gray;
			this.BottomSeparator.Location = new System.Drawing.Point(25, 215);
			this.BottomSeparator.Size = new System.Drawing.Size(210, 1);

			// 
			// ForceEnglishCheckBox
			// 
			this.ForceEnglishCheckBox = new System.Windows.Forms.CheckBox();
			this.ForceEnglishCheckBox.AutoSize = true;
			this.ForceEnglishCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
			this.ForceEnglishCheckBox.Location = new System.Drawing.Point(30, 226);
			this.ForceEnglishCheckBox.Name = "ForceEnglishCheckBox";
			this.ForceEnglishCheckBox.Size = new System.Drawing.Size(200, 21);
			this.ForceEnglishCheckBox.Text = "Force English in fullscreen";
			this.ForceEnglishCheckBox.UseVisualStyleBackColor = true;
			this.ForceEnglishCheckBox.CheckedChanged += new System.EventHandler(this.ForceEnglishCheckBox_CheckedChanged);

			//
			// debugCheckbox
			//
			this.debugCheckbox.AutoSize = true;
			this.debugCheckbox.Location = new System.Drawing.Point(242, 5);
			this.debugCheckbox.Size = new System.Drawing.Size(15, 14);
			this.debugCheckbox.UseVisualStyleBackColor = true;

            //
            // CursorWrangler
            //
            this.ClientSize = new System.Drawing.Size(260, 256);
            this.Controls.Add(this.ToggleButton);
            this.Controls.Add(this.StatusLabel);
			this.Controls.Add(this.AlwaysOnTopCheckBox);
            this.Controls.Add(this.LaunchOnStartupCheckBox);
            this.Controls.Add(this.StartMinimizedCheckBox);
            this.Controls.Add(this.MinimizeOnCloseCheckBox);
			this.Controls.Add(this.BottomSeparator);
			this.Controls.Add(this.ForceEnglishCheckBox);
			this.Controls.Add(this.debugCheckbox);
			
            this.Icon = Properties.Resources.CursorWranglerIcon;

            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            this.ShowInTaskbar = false;
            this.ShowIcon = true;

            this.Name = "CursorWrangler";
            this.Text = "CursorWrangler";

            this.FormClosing += new FormClosingEventHandler(this.CursorWrangler_FormClosing);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}