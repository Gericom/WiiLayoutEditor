using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiiLayoutEditor.GUI
{
	public class MainMenu : System.Windows.Forms.MainMenu {
		private System.ComponentModel.IContainer iContainer;
		public MainMenu(System.ComponentModel.IContainer iContainer)
		{
			// TODO: Complete member initialization
			this.iContainer = iContainer;
		}
	}
}
