using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WiiLayoutEditor.UI
{
	public partial class ProgressDialog : Form
	{
		public ProgressDialog()
		{
			InitializeComponent();
		}

		private void ProgressDialog_Load(object sender, EventArgs e)
		{

		}

		public void SetProgress(int Percent, String Message)
		{
			label1.Text = Message + " - " + Percent + "%";
			progressBar1.Value = Percent;
		}
	}
}
