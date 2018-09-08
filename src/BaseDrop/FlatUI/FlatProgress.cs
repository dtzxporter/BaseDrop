using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaseDrop.FlatUI
{
    public class FlatProgress : Control
    {
        private int progress = 0;
        public int Progress { get { return progress; } set { progress = value; this.Invalidate(); } }
        public Color ProgressBorder { get; set; }
        public Color ProgressFill { get; set; }

        public FlatProgress()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Paint it
            //Draw background
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), this.ClientRectangle);
            //Draw border
            e.Graphics.DrawRectangle(new Pen(new SolidBrush(this.ProgressBorder), 1), new Rectangle(new Point(0, 0), new Size(this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1)));
            //Calc width
            float WidthCalc = ((this.ClientRectangle.Width - 4f) * (this.progress / 100f));
            //Draw progress fill
            e.Graphics.FillRectangle(new SolidBrush(this.ProgressFill), new RectangleF(new Point(2, 2), new SizeF(WidthCalc, this.ClientRectangle.Height - 4)));
        }
    }
}
