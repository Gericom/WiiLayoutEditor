namespace WiiLayoutEditor.UI
{
	partial class ColorPicker
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorPicker));
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.label1 = new System.Windows.Forms.Label();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.numericUpDown4 = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.colorViewer1 = new WiiLayoutEditor.UI.ColorViewer();
			this.colorSliderControl1 = new WiiLayoutEditor.UI.ColorSliderControl();
			this.colorPickerControl1 = new WiiLayoutEditor.UI.ColorPickerControl();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).BeginInit();
			this.SuspendLayout();
			// 
			// trackBar1
			// 
			this.trackBar1.Location = new System.Drawing.Point(12, 277);
			this.trackBar1.Maximum = 255;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(292, 45);
			this.trackBar1.TabIndex = 2;
			this.trackBar1.TickFrequency = 8;
			this.trackBar1.Value = 255;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(364, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(18, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "R:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.Location = new System.Drawing.Point(388, 12);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(147, 20);
			this.numericUpDown1.TabIndex = 5;
			this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// numericUpDown2
			// 
			this.numericUpDown2.Location = new System.Drawing.Point(388, 40);
			this.numericUpDown2.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new System.Drawing.Size(147, 20);
			this.numericUpDown2.TabIndex = 7;
			this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(364, 42);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(18, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "G:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// numericUpDown3
			// 
			this.numericUpDown3.Location = new System.Drawing.Point(388, 66);
			this.numericUpDown3.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericUpDown3.Name = "numericUpDown3";
			this.numericUpDown3.Size = new System.Drawing.Size(147, 20);
			this.numericUpDown3.TabIndex = 9;
			this.numericUpDown3.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(364, 68);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(17, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "B:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// numericUpDown4
			// 
			this.numericUpDown4.Location = new System.Drawing.Point(388, 92);
			this.numericUpDown4.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericUpDown4.Name = "numericUpDown4";
			this.numericUpDown4.Size = new System.Drawing.Size(147, 20);
			this.numericUpDown4.TabIndex = 11;
			this.numericUpDown4.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericUpDown4.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(364, 94);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 13);
			this.label4.TabIndex = 10;
			this.label4.Text = "A:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// colorViewer1
			// 
			this.colorViewer1.BackColor = System.Drawing.Color.White;
			this.colorViewer1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("colorViewer1.BackgroundImage")));
			this.colorViewer1.Location = new System.Drawing.Point(310, 12);
			this.colorViewer1.Name = "colorViewer1";
			this.colorViewer1.Size = new System.Drawing.Size(48, 48);
			this.colorViewer1.TabIndex = 12;
			this.colorViewer1.ForeColorChanged += new System.EventHandler(this.panel1_BackColorChanged);
			// 
			// colorSliderControl1
			// 
			this.colorSliderControl1.Location = new System.Drawing.Point(277, 12);
			this.colorSliderControl1.Name = "colorSliderControl1";
			this.colorSliderControl1.Size = new System.Drawing.Size(27, 259);
			this.colorSliderControl1.TabIndex = 1;
			this.colorSliderControl1.HueChanged += new WiiLayoutEditor.UI.ColorSliderControl.HueChangedEventhandler(this.colorSliderControl1_HueChanged);
			// 
			// colorPickerControl1
			// 
			this.colorPickerControl1.Cursor = System.Windows.Forms.Cursors.Cross;
			this.colorPickerControl1.Hue = 0D;
			this.colorPickerControl1.Location = new System.Drawing.Point(12, 12);
			this.colorPickerControl1.Name = "colorPickerControl1";
			this.colorPickerControl1.Size = new System.Drawing.Size(259, 259);
			this.colorPickerControl1.TabIndex = 0;
			this.colorPickerControl1.ColorChanged += new WiiLayoutEditor.UI.ColorPickerControl.ColorChangedEventhandler(this.colorPickerControl1_ColorChanged);
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button1.Location = new System.Drawing.Point(460, 291);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 13;
			this.button1.Text = "OK";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button2.Location = new System.Drawing.Point(379, 291);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 14;
			this.button2.Text = "Cancel";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// ColorPicker
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(547, 326);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.colorViewer1);
			this.Controls.Add(this.numericUpDown4);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.numericUpDown3);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.numericUpDown2);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.numericUpDown1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.trackBar1);
			this.Controls.Add(this.colorSliderControl1);
			this.Controls.Add(this.colorPickerControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ColorPicker";
			this.Text = "ColorPicker";
			this.Load += new System.EventHandler(this.ColorPicker_Load);
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ColorPickerControl colorPickerControl1;
		private ColorSliderControl colorSliderControl1;
		private System.Windows.Forms.TrackBar trackBar1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.NumericUpDown numericUpDown2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown numericUpDown3;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown numericUpDown4;
		private System.Windows.Forms.Label label4;
		private ColorViewer colorViewer1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
	}
}