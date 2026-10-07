using System;
using System.Drawing;
using System.Windows.Forms;

using DrawingColor = System.Drawing.Color;

namespace AssetStudio.GUI
{
    internal class ThemedProgressBar : ProgressBar
    {
        private bool darkMode;

        public bool DarkMode
        {
            get => darkMode;

            set
            {
                if (darkMode == value)
                    return;

                darkMode = value;

                SetStyle(
                    ControlStyles.UserPaint,
                    darkMode);

                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer,
                    darkMode);

                UpdateStyles();
                Invalidate();
            }
        }

        public ThemedProgressBar()
        {
            /*
             * Light Mode starts with the native Windows renderer.
             * This preserves the original green ProgressBar.
             */
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            /*
             * In Light Mode UserPaint is disabled, so Windows
             * renders the native ProgressBar automatically.
             */
            if (!DarkMode)
            {
                base.OnPaint(e);
                return;
            }

            Rectangle bounds = ClientRectangle;

            using (SolidBrush backgroundBrush =
                   new SolidBrush(
                       DrawingColor.FromArgb(51, 51, 55)))
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

            Rectangle fillBounds =
                new Rectangle(
                    0,
                    0,
                    fillWidth,
                    bounds.Height);

            using (SolidBrush progressBrush =
                   new SolidBrush(
                       DrawingColor.FromArgb(0, 122, 204)))
            {
                e.Graphics.FillRectangle(
                    progressBrush,
                    fillBounds);
            }
        }
    }
}