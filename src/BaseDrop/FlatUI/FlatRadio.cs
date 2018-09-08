using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaseDrop.FlatUI
{
    internal class FlatRadio : Control
    {
        private bool ischecked = false;
        public bool isChecked { get { return ischecked; } set { ischecked = value; this.Invalidate(); } }

        private Color borderColor;
        public Color BorderColor { get { return borderColor; } set { borderColor = value; this.Invalidate(); } }

        public FlatRadio()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            // Check state
            if (!this.isChecked)
            {
                this.isChecked = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Set Anti-Alias
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Check
            if (this.Enabled)
            {
                // Draw the initial circle
                e.Graphics.DrawEllipse(new Pen(new SolidBrush(this.BorderColor)), new Rectangle(new Point(0, 0), new Size(this.ClientRectangle.Height - 1, this.ClientRectangle.Height - 1)));
            }
            else
            {
                // Draw the initial circle
                e.Graphics.DrawEllipse(new Pen(new SolidBrush(Color.DarkGray)), new Rectangle(new Point(0, 0), new Size(this.ClientRectangle.Height - 1, this.ClientRectangle.Height - 1)));
            }
            // Draw circle check if necessary
            if (this.isChecked)
            {
                e.Graphics.FillEllipse(new SolidBrush(this.BorderColor), new Rectangle(new Point(3, 3), new Size(this.ClientRectangle.Height - 7, this.ClientRectangle.Height - 7)));
            }
            // Draw text if any
            if (this.Text.Length > 0)
            {
                SizeF size = e.Graphics.MeasureString(this.Text, this.Font);
                e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), (this.ClientRectangle.Height - 1) + 4, (this.ClientSize.Height - size.Height) / 2);
            }
        }
    }
}
