using System;
using System.Drawing;
using System.Windows.Forms;

using DrawingColor = System.Drawing.Color;

namespace AssetStudio.GUI
{
    internal class ThemedProgressBar : ProgressBar
    {
        public bool DarkMode { get; set; }

        public ThemedProgressBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!DarkMode)
            {
                base.OnPaint(e);
                return;
            }

            Rectangle bounds = ClientRectangle;

            using (SolidBrush backgroundBrush =
                   new SolidBrush(DrawingColor.FromArgb(51, 51, 55)))
            {
                e.Graphics.FillRectangle(
                    backgroundBrush,
                    bounds);
            }

            if (Maximum <= Minimum)
                return;

            double percentage =
                (double)(Value - Minimum) /
                (Maximum - Minimum);

            int fillWidth =
                (int)Math.Round(
                    bounds.Width * percentage);

            if (fillWidth <= 0)
                return;

            Rectangle fillBounds = new Rectangle(
                0,
                0,
                fillWidth,
                bounds.Height);

            using (SolidBrush progressBrush =
                   new SolidBrush(DrawingColor.FromArgb(0, 122, 204)))
            {
                e.Graphics.FillRectangle(
                    progressBrush,
                    fillBounds);
            }
        }
    }
}