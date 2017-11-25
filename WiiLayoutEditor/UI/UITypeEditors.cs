using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Drawing;
using System.ComponentModel;

namespace WiiLayoutEditor.UI
{
	public class UITypeEditors
	{
		// This UITypeEditor can be associated with Int32, Double and Single 
		// properties to provide a design-mode angle selection interface.
		[System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
		public class PSColorPicker : System.Drawing.Design.UITypeEditor
		{
			public PSColorPicker()
			{
			}

			// Indicates whether the UITypeEditor provides a form-based (modal) dialog,  
			// drop down dialog, or no UI outside of the properties window. 
			public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
			{
				return UITypeEditorEditStyle.Modal;
			}

			// Displays the UI for value selection. 
			public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
			{
				// Return the value if the value is not of type Int32, Double and Single. 
				if (value.GetType() != typeof(Color))
					return value;

				// Uses the IWindowsFormsEditorService to display a  
				// drop-down UI in the Properties window.
				IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (edSvc != null)
				{
					// Display an angle selection control and retrieve the value.
					ColorPicker colorpicker = new ColorPicker((Color)value);
					edSvc.ShowDialog(colorpicker);

					// Return the value in the appropraite data format. 
					if (colorpicker.DialogResult == System.Windows.Forms.DialogResult.OK)
					{
						value = colorpicker.GetColor();
					}
				}
				return value;
			}

			// Draws a representation of the property's value. 
			public override void PaintValue(System.Drawing.Design.PaintValueEventArgs e)
			{
				int normalX = (e.Bounds.Width / 2);
				int normalY = (e.Bounds.Height / 2);

				// Fill background and ellipse and center point.
				e.Graphics.FillRectangle(new SolidBrush((Color)e.Value), e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);

				// Draw line along the current angle. 
				//double radians = ((double)e.Value * Math.PI) / (double)180;
				//e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red), 1), normalX + e.Bounds.X, normalY + e.Bounds.Y,
				//	e.Bounds.X + (normalX + (int)((double)normalX * Math.Cos(radians))),
				//	e.Bounds.Y + (normalY + (int)((double)normalY * Math.Sin(radians))));
			}

			// Indicates whether the UITypeEditor supports painting a  
			// representation of a property's value. 
			public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
			{
				return true;
			}
		}
		public class MyColorConverter : ColorConverter
		{ // reference: System.Drawing.Design.dll 
			public override bool GetStandardValuesSupported(
					ITypeDescriptorContext context)
			{
				return false;
			}
		} 

	}
}
