using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using HearThis.Script;

namespace HearThis.UI
{
	public partial class ChapterButton : UserControl
	{
		private bool _selected;
		private Brush _highlightBoxBrush;
		private int _percentageRecorded;
		private int _percentageTranslated;

		public ChapterButton(ChapterInfo chapterInfo)
		{
			ChapterInfo = chapterInfo;
			InitializeComponent();
			_highlightBoxBrush = new SolidBrush(AppPallette.HilightColor);

			//We'r'e doing ThreadPool instead of the more convenient BackgroundWorker based on experimentation and the advice on the web; we are doing relatively a lot of little threads here,
			//that don't really have to interact much with the UI until they are complete.
			var waitCallback = new WaitCallback(GetStatsInBackground);
			ThreadPool.QueueUserWorkItem(waitCallback, this);
		}

		static void GetStatsInBackground(object stateInfo)
		{

			ChapterButton button = stateInfo as ChapterButton;
			button._percentageRecorded = button.ChapterInfo.CalculatePercentageRecorded();
			button._percentageTranslated =  button.ChapterInfo.CalculatePercentageTranslated();
			lock(button)
			{
				if(button.IsHandleCreated && !button.IsDisposed)
				{
					button.Invoke(new Action(delegate { button.Invalidate(); }));
				}
			}
		}


		public ChapterInfo ChapterInfo { get; private set; }

		public bool Selected
		{
			get { return _selected; }
			set
			{
				if(_selected !=value)
				{
					_selected = value;
					Invalidate();
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			int fillWidth = Width - 4;
			int fillHeight = Height - 5;
			var r = new Rectangle( 2, 3, fillWidth, fillHeight);
			if (Selected)
			{
				e.Graphics.FillRectangle(_highlightBoxBrush, 0, 0, Width, Height);
			}

			DrawBox(e.Graphics, r, Selected, _percentageTranslated, _percentageRecorded);
		}

		public static void DrawBox(Graphics g, Rectangle bounds, bool selected, int percentageTranslated, int percentageRecorded)
		{
			using (Brush _fillBrush = new SolidBrush(percentageTranslated > 0 ? AppPallette.Blue : AppPallette.EmptyBoxColor))
			{
				g.FillRectangle(_fillBrush, bounds);
			}
			if(percentageRecorded >0 && percentageRecorded < 100)
			{
				using(var pen = new Pen(AppPallette.HilightColor,1))
				{
					g.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right-1, bounds.Bottom - 1);
				}
			}
			else if (percentageRecorded ==100)
			{
				int v1 = bounds.Height/2 + 3;
				int v2 = bounds.Height/2 + 7;
				int v3 = bounds.Height/2 - 2;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				Pen progressPen = percentageRecorded == 100 ? AppPallette.CompleteProgressPen : AppPallette.PartialProgressPen;


				if (percentageRecorded == 100)
				{
					//draw the first stroke of a check mark
					g.DrawLine(progressPen, 4, v1, 7, v2);
					//complete the checkmark
					g.DrawLine(progressPen, 7, v2, 10, v3);
				}
			}
		}
	}
}
