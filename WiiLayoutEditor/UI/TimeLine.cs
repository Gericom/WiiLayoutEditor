using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace WiiLayoutEditor.UI
{
	public partial class TimeLine : UserControl
	{
		public AdobeToolStripRender ToolstripRender;
		private readonly ObservableCollection<Group> groups = new ObservableCollection<Group>();
		public ObservableCollection<Group> Groups
		{
			get
			{
				return groups;
			}
		}
		public void BeginUpdate()
		{
			update = true;
		}
		public void EndUpdate()
		{
			toolStrip1.Refresh();
			//dataGridView1.SuspendDrawing();
			dataGridView1.BeginUpdate();
			dataGridView1.Columns.Clear();
			for (int i = 0; i < NrFrames; i++)
			{
				dataGridView1.Columns.Add(new DataGridViewFlashFrameColumn());
				dataGridView1.Columns[i].Width = 8;
				dataGridView1.Columns[i].FillWeight = 8;
				dataGridView1.Columns[i].ReadOnly = true;
			}
			int j = 0;
			foreach (Group g in Groups)
			{
				dataGridView1.Rows.Add(g);
				dataGridView1.Rows[j].HeaderCell = new FlashHeaderCell();
				dataGridView1.Rows[j].HeaderCell.Value = g.Name;
				j++;
			}
			dataGridView1.EndUpdate();
			update = false;
		}
		bool update = false;
		private int curFrame = 0;
		[DisplayName("Current Frame")]
		public UInt32 CurFrame
		{
			get { return (uint)curFrame; }
			set
			{
				curFrame = (int)value;
				ToolstripRender.FrameNr = (int)value + 1;
				if (!update)
				{
					toolStrip1.Refresh();
					dataGridView1.Refresh();
				}
			}
		}
		private int nrFrames = 100;
		public UInt32 NrFrames
		{
			get { return (uint)nrFrames; }
			set
			{
				if (value < 1) { NrFrames = 1; return; }
				nrFrames = (int)value;
				ToolstripRender.NrFrames = nrFrames;
				if (!update)
				{
					toolStrip1.Refresh();
					//dataGridView1.SuspendDrawing();
					dataGridView1.Columns.Clear();
					for (int i = 0; i < NrFrames; i++)
					{
						dataGridView1.Columns.Add(new DataGridViewFlashFrameColumn());
						dataGridView1.Columns[i].Width = 8;
						dataGridView1.Columns[i].FillWeight = 8;
						dataGridView1.Columns[i].ReadOnly = true;
					}
					int j = 0;
					foreach (Group g in Groups)
					{
						dataGridView1.Rows.Add(g);
						dataGridView1.Rows[j].HeaderCell = new FlashHeaderCell();
						dataGridView1.Rows[j].HeaderCell.Value = g.Name;
						j++;
					}
				}
				//dataGridView1.ResumeDrawing(true);
			}
		}
		public TimeLine()
		{
			InitializeComponent();
			ToolstripRender = new AdobeToolStripRender(100, 0, 1);
			toolStrip1.Renderer = ToolstripRender;
			groups.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(groups_CollectionChanged);
			for (int i = 0; i < NrFrames; i++)
			{
				dataGridView1.Columns.Add(new DataGridViewFlashFrameColumn());
				dataGridView1.Columns[i].Width = 8;
				dataGridView1.Columns[i].FillWeight = 8;
				dataGridView1.Columns[i].ReadOnly = true;
			}
			int j = 0;
			foreach (Group g in Groups)
			{
				dataGridView1.Rows.Add(g);
				dataGridView1.Rows[j].HeaderCell = new FlashHeaderCell();
				dataGridView1.Rows[j].HeaderCell.Value = g.Name;
				j++;
			}
		}

		private void groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			//dataGridView1.SuspendDrawing();
			if (!update)
			{
				dataGridView1.Columns.Clear();
				for (int i = 0; i < NrFrames; i++)
				{
					dataGridView1.Columns.Add(new DataGridViewFlashFrameColumn());
					dataGridView1.Columns[i].Width = 8;
					dataGridView1.Columns[i].FillWeight = 8;
					dataGridView1.Columns[i].ReadOnly = true;
				}
				int j = 0;
				foreach (Group g in Groups)
				{
					dataGridView1.Rows.Add(g);
					dataGridView1.Rows[j].HeaderCell = new FlashHeaderCell();
					dataGridView1.Rows[j].HeaderCell.Value = g.Name;
					j++;
				}
			}
			//dataGridView1.ResumeDrawing(true);
		}
		public class AdobeToolStripRender : ToolStripSystemRenderer
		{
			public AdobeToolStripRender(Int32 NrFrames, Int32 StartFrame, Int32 FrameNr)
			{
				this.NrFrames = NrFrames;
				this.StartFrame = StartFrame;
				this.FrameNr = FrameNr;
			}
			public Int32 FrameNr { get; set; }
			public Int32 NrFrames { get; set; }
			public Int32 StartFrame { get; set; }
			protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
			{
				base.OnRenderToolStripBackground(e);
				e.Graphics.TranslateTransform(e.ToolStrip.Padding.Left, 0);
				e.Graphics.DrawLine(new Pen(Color.FromArgb(166, 166, 166)), 0, 0, 0, 27);
				e.Graphics.TranslateTransform(1, 0);
				e.Graphics.DrawLine(new Pen(Color.FromArgb(166, 166, 166)), 0, 26, e.ToolStrip.Width, 26);
				e.Graphics.DrawLine(new Pen(Color.FromArgb(94, 94, 94)), 0, 0, e.ToolStrip.Width, 0);
				e.Graphics.TranslateTransform(-StartFrame * 8, 0);
				if (!(StartFrame >= FrameNr))
				{
					e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 153, 153)), (FrameNr - 1) * 8 - 1, 4, 8, 21);
				}
				StringFormat drawFormat = new StringFormat();
				drawFormat.Alignment = StringAlignment.Near;
				drawFormat.LineAlignment = StringAlignment.Center;
				for (int i = StartFrame; i < NrFrames; i++)
				{
					if (i * 8 + 7 - StartFrame * 8 > e.AffectedBounds.Width)
					{
						break;
					}
					e.Graphics.DrawLine(new Pen(Color.FromArgb(128, 128, 128)), i * 8 + 7, 21, i * 8 + 7, 24);
					if ((i + 1) % 5 == 0 || i == 0)
					{
						e.Graphics.DrawString((i + 1).ToString(), DefaultFont, Brushes.Black, new RectangleF(i * 8 - 3, 0, 30, 27), drawFormat);
					}
				}
				if (!(StartFrame >= FrameNr))
				{
					e.Graphics.DrawRectangle(new Pen(Color.FromArgb(204, 0, 0)), (FrameNr - 1) * 8 - 1, 4, 8, 20);
					e.Graphics.DrawLine(new Pen(Color.FromArgb(204, 0, 0)), (FrameNr - 1) * 8 + 3, 25, (FrameNr - 1) * 8 + 3, 27);
				}
			}
			protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
			{
				//base.OnRenderToolStripBorder(e);
				e.Graphics.DrawLine(new Pen(Color.FromArgb(166, 166, 166)), 0, 26, e.ToolStrip.Padding.Left, 26);
				e.Graphics.DrawLine(new Pen(Color.FromArgb(94, 94, 94)), 0, 0, e.ToolStrip.Width, 0);
			}
		}
		public class DataGridViewFlashFrameColumn : DataGridViewColumn
		{
			public DataGridViewFlashFrameColumn()
			{
				this.CellTemplate = new FlashFrame();
			}
		}

		public class FlashFrame : DataGridViewTextBoxCell
		{
			protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
			{
				if (value == null)
				{
					graphics.FillRectangle(new SolidBrush(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : ((ColumnIndex + 1) % 5) == 0 ? Color.FromArgb(237, 237, 237) : Color.White)), cellBounds.X, cellBounds.Y, 8, 18);
					graphics.DrawLine(new Pen(Color.FromArgb(222, 222, 222)), cellBounds.X + 7, cellBounds.Y, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.DrawLine(new Pen(Color.FromArgb(222, 222, 222)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + 7, cellBounds.Y + 17);
				}
				else if (value == "+")
				{
					graphics.FillRectangle(new SolidBrush(
										((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(113, 174, 235) : Color.FromArgb(204, 204, 204))
										), cellBounds.X, cellBounds.Y, 8, 18);
					graphics.DrawLine(Pens.Black, cellBounds.X + 7, cellBounds.Y, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : Color.Black)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 2, cellBounds.Y + 10, 3, 5);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 1, cellBounds.Y + 11, 1, 3);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 5, cellBounds.Y + 11, 1, 3);
				}
				else if (value == "+-")
				{
					graphics.FillRectangle(new SolidBrush(
										((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(113, 174, 235) : Color.FromArgb(204, 204, 204))
										), cellBounds.X, cellBounds.Y, 8, 18);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(120, 181, 242) : Color.FromArgb(222, 222, 222))), cellBounds.X + 7, cellBounds.Y, cellBounds.X + 7, cellBounds.Y + 1);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(120, 181, 242) : Color.FromArgb(222, 222, 222))), cellBounds.X + 7, cellBounds.Y + 16, cellBounds.X + 7, cellBounds.Y + 15);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : Color.Black)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 2, cellBounds.Y + 10, 3, 5);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 1, cellBounds.Y + 11, 1, 3);
					graphics.FillRectangle(new SolidBrush(
						((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)
						), cellBounds.X + 5, cellBounds.Y + 11, 1, 3);
				}
				else if (value == "-")
				{
					graphics.FillRectangle(new SolidBrush(
										((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(113, 174, 235) : Color.FromArgb(204, 204, 204))
										), cellBounds.X, cellBounds.Y, 8, 18);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : Color.Black)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(120, 181, 242) : Color.FromArgb(222, 222, 222))), cellBounds.X + 7, cellBounds.Y, cellBounds.X + 7, cellBounds.Y + 1);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(120, 181, 242) : Color.FromArgb(222, 222, 222))), cellBounds.X + 7, cellBounds.Y + 16, cellBounds.X + 7, cellBounds.Y + 15);
				}
				else if (value == "-+")
				{
					graphics.FillRectangle(new SolidBrush(
										((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(113, 174, 235) : Color.FromArgb(204, 204, 204))
										), cellBounds.X, cellBounds.Y, 8, 18);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : Color.Black)), cellBounds.X + 7, cellBounds.Y, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.DrawLine(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(51, 153, 255) : Color.Black)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + 7, cellBounds.Y + 17);
					graphics.DrawRectangle(new Pen(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(31, 92, 153) : Color.Black)), cellBounds.X + 1, cellBounds.Y + 7, 4, 8);
					graphics.FillRectangle(new SolidBrush(((cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? Color.FromArgb(133, 194, 255) : Color.White)), cellBounds.X + 2, cellBounds.Y + 8, 3, 7);
				}
				if (((TimeLine)DataGridView.Parent).ToolstripRender.FrameNr - 1 == ColumnIndex)
				{
					graphics.DrawLine(new Pen(Color.FromArgb(204, 0, 0)), cellBounds.X + 3, cellBounds.Y, cellBounds.X + 3, cellBounds.Y + 17);
				}
			}
		}

		public class FlashHeaderCell : DataGridViewRowHeaderCell
		{
			protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground);
				DataGridViewCellStyle c = cellStyle.Clone();
				if (Selected || (DataGridView.SelectedCells.Count != 0 && DataGridView.SelectedCells[0].RowIndex == RowIndex))
				{
					graphics.FillRectangle(new SolidBrush(DataGridView.RowHeadersDefaultCellStyle.SelectionBackColor), cellBounds);
					c.ForeColor = Color.White;
				}
				cellBounds.X -= 48 - 15;
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, c, advancedBorderStyle, DataGridViewPaintParts.ContentForeground);
				cellBounds.X += 48 - 15;
				graphics.DrawLine(new Pen(Color.FromArgb(222, 222, 222)), cellBounds.X, cellBounds.Y + 17, cellBounds.X + cellBounds.Width, cellBounds.Y + 17);
			}
		}

		private void dataGridView1_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawLine(new Pen(Color.FromArgb(166, 166, 166)), dataGridView1.RowHeadersWidth - 1, 0, dataGridView1.RowHeadersWidth - 1, dataGridView1.Height);
			if (!(ToolstripRender.StartFrame >= ToolstripRender.FrameNr))
			{
				e.Graphics.DrawLine(new Pen(Color.FromArgb(204, 0, 0)), CurFrame * 8 + 3 + dataGridView1.RowHeadersWidth - dataGridView1.HorizontalScrollingOffset, dataGridView1.RowCount*18, CurFrame * 8 + 3 + dataGridView1.RowHeadersWidth - dataGridView1.HorizontalScrollingOffset, e.ClipRectangle.Height);
			}
		}

		private void dataGridView1_RowHeadersWidthChanged(object sender, EventArgs e)
		{
			toolStrip1.Padding = new Padding(dataGridView1.RowHeadersWidth - 1, 0, 0, 0);
		}
		public class Frame
		{
			public Frame()
			{
				IsKeyFrame = true;
			}
			public Frame(bool IsKeyFrame)
			{
				this.IsKeyFrame = IsKeyFrame;
			}
			public bool IsKeyFrame { get; set; }
		}
		public class Group
		{
			public Group()
			{

			}
			public Group(String Name)
			{
				this.Name = Name;
			}
			public String Name { get; set; }
			private readonly Collection<Frame> frames = new Collection<Frame>();
			public Collection<Frame> Frames
			{
				get { return frames; }
			}
			private readonly Collection<Group> subGroups = new Collection<Group>();
			public Collection<Group> SubGroups
			{
				get { return subGroups; }
			}
			public override string ToString()
			{
				return Name;
			}
			public static implicit operator string[](Group g)
			{
				List<String> s = new List<string>();
				int idx = 0;
				foreach (Frame f in g.Frames)
				{
					if (idx != g.Frames.Count - 1 && f.IsKeyFrame && !g.Frames[idx + 1].IsKeyFrame)
					{
						s.Add("+-");
					}
					else if (f.IsKeyFrame)
					{
						s.Add("+");
					}
					else if (idx != g.Frames.Count - 1 && g.Frames[idx + 1].IsKeyFrame || idx == g.Frames.Count - 1)
					{
						s.Add("-+");
					}
					else
					{
						s.Add("-");
					}
					idx++;
				}
				return s.ToArray();
			}
		}

		private void dataGridView1_Scroll(object sender, ScrollEventArgs e)
		{
			dataGridView1.HorizontalScrollingOffset = e.NewValue / 8 * 8;
			dataGridView1.FirstDisplayedScrollingColumnIndex = e.NewValue / 8;
			ToolstripRender.StartFrame = dataGridView1.FirstDisplayedScrollingColumnIndex;
			toolStrip1.Refresh();
		}

		private void toolStrip1_MouseMove(object sender, MouseEventArgs e)
		{
			if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
			{
				int idx = (e.X - toolStrip1.Padding.Left) / 8;
				idx += ToolstripRender.StartFrame;
				if (idx < 0)
				{
					idx = 0;
				}
				if (idx > NrFrames - 1)
				{
					idx = (int)NrFrames - 1;
				}
				ToolstripRender.FrameNr = idx + 1;
				CurFrame = (uint)idx;
				toolStrip1.Refresh();
				dataGridView1.Refresh();
				if (OnFrameChanged != null)
				{
					OnFrameChanged((int)CurFrame);
				}
			}
		}

		private void toolStrip1_MouseUp(object sender, MouseEventArgs e)
		{
			if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
			{
				int idx = (e.X - toolStrip1.Padding.Left) / 8;
				idx += ToolstripRender.StartFrame;
				if (idx < 0)
				{
					idx = 0;
				}
				if (idx > NrFrames - 1)
				{
					idx = (int)NrFrames - 1;
				}
				ToolstripRender.FrameNr = idx + 1;
				CurFrame = (uint)idx;
				toolStrip1.Refresh();
				dataGridView1.Refresh();
				if (OnFrameChanged != null)
				{
					OnFrameChanged((int)CurFrame);
				}
			}
		}

		private void dataGridView1_SelectionChanged(object sender, EventArgs e)
		{
			//dataGridView1.InvalidateColumn(ToolstripRender.FrameNr-1);
		}

		private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			
		}
		public delegate void OnFrameChangedEventHandler(int FrameNr);
		public event OnFrameChangedEventHandler OnFrameChanged;
	}
	public class dataGridViewNF : DataGridView
	{
		public dataGridViewNF()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer| ControlStyles.Opaque| ControlStyles.UserPaint| ControlStyles.AllPaintingInWmPaint, true);
			//this.VerticalScrollBar.SmallChange = 8;
			//this.VerticalScrollBar.LargeChange = 8;
			this.HorizontalScrollBar.SmallChange = 8;
			this.HorizontalScrollBar.LargeChange = 8;
		}
		public void BeginUpdate()
		{
			this.ScrollBars = System.Windows.Forms.ScrollBars.None;
			update = true;
		}
		public void EndUpdate()
		{
			this.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			update = false;
			this.Invalidate();
		}
		bool update = false;
		protected override void OnPaint(PaintEventArgs e)
		{
			if(!update)	base.OnPaint(e);
		}
	}
}
