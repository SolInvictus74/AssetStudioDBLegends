using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using DrawingColor = System.Drawing.Color;
using DrawingSystemColors = System.Drawing.SystemColors;

namespace AssetStudio.GUI
{
    internal enum AppTheme
    {
        Light,
        Dark
    }

    internal static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        private static readonly Dictionary<TabControl, TabHeaderPainter> TabPainters = new();

        // ============================================================
        // ASSETSTUDIO DARK PALETTE
        // Visual Studio / DarkUI inspired
        // ============================================================

        public static readonly DrawingColor DarkBackground =
            DrawingColor.FromArgb(30, 30, 30);       // #1E1E1E

        public static readonly DrawingColor DarkPanel =
            DrawingColor.FromArgb(37, 37, 38);       // #252526

        public static readonly DrawingColor DarkControl =
            DrawingColor.FromArgb(45, 45, 48);       // #2D2D30

        public static readonly DrawingColor DarkInput =
            DrawingColor.FromArgb(37, 37, 38);       // #252526

        public static readonly DrawingColor DarkBorder =
            DrawingColor.FromArgb(63, 63, 70);       // #3F3F46

        public static readonly DrawingColor DarkText =
            DrawingColor.FromArgb(241, 241, 241);

        public static readonly DrawingColor DarkTextSecondary =
            DrawingColor.FromArgb(190, 190, 190);

        public static readonly DrawingColor DarkSelected =
            DrawingColor.FromArgb(9, 71, 113);       // #094771

        public static readonly DrawingColor DarkHover =
            DrawingColor.FromArgb(62, 62, 64);

        public static readonly DrawingColor Accent =
            DrawingColor.FromArgb(0, 122, 204);      // #007ACC

        public static readonly DrawingColor AccentText =
            DrawingColor.FromArgb(111, 208, 140);    // #6FD08C

        public static readonly DrawingColor ProgressBackground =
            DrawingColor.FromArgb(51, 51, 55);


        // ============================================================
        // APPLY THEME
        // ============================================================

        public static void Apply(Form form, AppTheme theme)
        {
            CurrentTheme = theme;

            form.SuspendLayout();

            ApplyControl(form, theme);

            form.ResumeLayout(true);
            form.Refresh();
        }
        public static void ApplyToOpenForms(AppTheme theme)
        {
            CurrentTheme = theme;

            foreach (Form form in Application.OpenForms)
            {
                Apply(form, theme);
            }
        }

        // ============================================================
        // CONTROLS
        // ============================================================

        private static void ApplyControl(Control control, AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            control.BackColor = dark
                ? DarkBackground
                : DrawingSystemColors.Control;

            control.ForeColor = dark
                ? DarkText
                : DrawingSystemColors.ControlText;

            // --------------------------------------------------------
            // LinkLabel
            // --------------------------------------------------------

            if (control is LinkLabel linkLabel)
            {
                linkLabel.BackColor = dark
                    ? DarkBackground
                    : DrawingSystemColors.Control;

                linkLabel.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.ControlText;

                linkLabel.LinkColor = dark
                    ? DrawingColor.FromArgb(111, 208, 140)   // #6FD08C
                    : DrawingColor.Blue;

                linkLabel.ActiveLinkColor = dark
                    ? DrawingColor.FromArgb(140, 230, 165)
                    : DrawingColor.Red;

                linkLabel.VisitedLinkColor = dark
                    ? DrawingColor.FromArgb(111, 208, 140)
                    : DrawingColor.Purple;
            }
            // --------------------------------------------------------
            // TextBox
            // --------------------------------------------------------

            else if (control is TextBox textBox)
            {
                textBox.BackColor = dark
                    ? DarkInput
                    : DrawingSystemColors.Window;

                textBox.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;
            }

            // --------------------------------------------------------
            // RichTextBox
            // --------------------------------------------------------

            else if (control is RichTextBox richTextBox)
            {
                richTextBox.BackColor = dark
                    ? DarkInput
                    : DrawingSystemColors.Window;

                richTextBox.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;
            }

            // --------------------------------------------------------
            // TreeView
            // --------------------------------------------------------

            else if (control is TreeView treeView)
            {
                treeView.BackColor = dark
                    ? DarkPanel
                    : DrawingSystemColors.Window;

                treeView.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;

                treeView.LineColor = dark
                    ? DarkBorder
                    : DrawingSystemColors.ControlDark;
            }

            // --------------------------------------------------------
            // ListView
            // --------------------------------------------------------

            else if (control is ListView listView)
            {
                listView.BackColor = dark
                    ? DarkPanel
                    : DrawingSystemColors.Window;

                listView.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;

                listView.GridLines = !dark;

                ConfigureListView(listView, theme);
            }

            // --------------------------------------------------------
            // ProgressBar
            // --------------------------------------------------------
            else if (control is ThemedProgressBar progressBar)
            {
                progressBar.DarkMode = dark;
                progressBar.Invalidate();
            }
            // --------------------------------------------------------
            // DataGridView
            // --------------------------------------------------------

            else if (control is DataGridView dataGridView)
            {
                if (dark)
                {
                    dataGridView.BackgroundColor = DarkBackground;
                    dataGridView.GridColor = DarkBorder;

                    dataGridView.EnableHeadersVisualStyles = false;

                    dataGridView.DefaultCellStyle.BackColor = DarkPanel;
                    dataGridView.DefaultCellStyle.ForeColor = DarkText;
                    dataGridView.DefaultCellStyle.SelectionBackColor = DarkSelected;
                    dataGridView.DefaultCellStyle.SelectionForeColor = DarkText;

                    dataGridView.AlternatingRowsDefaultCellStyle.BackColor =
                        DarkBackground;

                    dataGridView.ColumnHeadersDefaultCellStyle.BackColor =
                        DarkControl;

                    dataGridView.ColumnHeadersDefaultCellStyle.ForeColor =
                        DarkText;

                    dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor =
                        DarkControl;

                    dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor =
                        DarkText;

                    dataGridView.RowHeadersDefaultCellStyle.BackColor =
                        DarkControl;

                    dataGridView.RowHeadersDefaultCellStyle.ForeColor =
                        DarkText;

                    dataGridView.RowHeadersDefaultCellStyle.SelectionBackColor =
                        DarkSelected;

                    dataGridView.RowHeadersDefaultCellStyle.SelectionForeColor =
                        DarkText;
                }
                else
                {
                    dataGridView.BackgroundColor =
                        DrawingSystemColors.AppWorkspace;

                    dataGridView.GridColor =
                        DrawingSystemColors.ControlDark;

                    dataGridView.EnableHeadersVisualStyles = true;

                    dataGridView.DefaultCellStyle.BackColor =
                        DrawingSystemColors.Window;

                    dataGridView.DefaultCellStyle.ForeColor =
                        DrawingSystemColors.ControlText;

                    dataGridView.DefaultCellStyle.SelectionBackColor =
                        DrawingSystemColors.Highlight;

                    dataGridView.DefaultCellStyle.SelectionForeColor =
                        DrawingSystemColors.HighlightText;

                    dataGridView.AlternatingRowsDefaultCellStyle.BackColor =
                        DrawingSystemColors.Window;

                    dataGridView.ColumnHeadersDefaultCellStyle.BackColor =
                        DrawingSystemColors.Control;

                    dataGridView.ColumnHeadersDefaultCellStyle.ForeColor =
                        DrawingSystemColors.ControlText;

                    dataGridView.RowHeadersDefaultCellStyle.BackColor =
                        DrawingSystemColors.Control;

                    dataGridView.RowHeadersDefaultCellStyle.ForeColor =
                        DrawingSystemColors.ControlText;
                }
            }
            // --------------------------------------------------------
            // TabControl
            // --------------------------------------------------------

            else if (control is TabControl tabControl)
            {
                ConfigureTabControl(tabControl, theme);
            }

            // --------------------------------------------------------
            // TabPage
            // --------------------------------------------------------

            else if (control is TabPage tabPage)
            {
                tabPage.BackColor = dark
                    ? DarkBackground
                    : DrawingSystemColors.Control;

                tabPage.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.ControlText;
            }

            // --------------------------------------------------------
            // Panel
            // --------------------------------------------------------

            else if (control is Panel panel)
            {
                panel.BackColor = dark
                    ? DarkBackground
                    : DrawingSystemColors.Control;

                panel.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.ControlText;
            }

            // --------------------------------------------------------
            // Button
            // --------------------------------------------------------

            else if (control is Button button)
            {
                button.BackColor = dark
                    ? DarkControl
                    : DrawingSystemColors.Control;

                button.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.ControlText;

                button.FlatStyle = dark
                    ? FlatStyle.Flat
                    : FlatStyle.Standard;

                if (dark)
                {
                    button.FlatAppearance.BorderColor = DarkBorder;
                    button.FlatAppearance.MouseOverBackColor = DarkHover;
                    button.FlatAppearance.MouseDownBackColor = DarkSelected;
                }

                button.UseVisualStyleBackColor = !dark;
            }

            // --------------------------------------------------------
            // CheckBox
            // --------------------------------------------------------

            else if (control is CheckBox checkBox)
            {
                checkBox.BackColor = dark
                    ? DarkBackground
                    : DrawingSystemColors.Control;

                checkBox.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.ControlText;

                checkBox.UseVisualStyleBackColor = !dark;
            }

            // --------------------------------------------------------
            // ComboBox
            // --------------------------------------------------------

            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = dark
                    ? DarkInput
                    : DrawingSystemColors.Window;

                comboBox.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;
            }

            // --------------------------------------------------------
            // NumericUpDown
            // --------------------------------------------------------

            else if (control is NumericUpDown numeric)
            {
                numeric.BackColor = dark
                    ? DarkInput
                    : DrawingSystemColors.Window;

                numeric.ForeColor = dark
                    ? DarkText
                    : DrawingSystemColors.WindowText;
            }

            // --------------------------------------------------------
            // Menu / Status / ToolStrip
            // --------------------------------------------------------

            else if (control is ToolStrip toolStrip)
            {
                ApplyToolStrip(toolStrip, theme);
            }


            // Context menu attached to control
            if (control.ContextMenuStrip != null)
            {
                ApplyToolStrip(control.ContextMenuStrip, theme);
            }


            // Process children
            foreach (Control child in control.Controls)
            {
                ApplyControl(child, theme);
            }
        }
        // ============================================================
        // LIST VIEW
        // ============================================================

        private static void ConfigureListView(
            ListView listView,
            AppTheme theme)
        {
            // Evita di registrare gli stessi eventi più volte
            listView.DrawColumnHeader -= ListView_DrawColumnHeader;
            listView.DrawItem -= ListView_DrawItem;
            listView.DrawSubItem -= ListView_DrawSubItem;

            if (theme == AppTheme.Dark &&
                listView.View == View.Details)
            {
                listView.OwnerDraw = true;

                listView.DrawColumnHeader += ListView_DrawColumnHeader;
                listView.DrawItem += ListView_DrawItem;
                listView.DrawSubItem += ListView_DrawSubItem;
            }
            else
            {
                listView.OwnerDraw = false;
            }

            listView.Invalidate();
        }
        private static void ListView_DrawColumnHeader(
            object sender,
            DrawListViewColumnHeaderEventArgs e)
        {
            using SolidBrush backgroundBrush =
                new SolidBrush(DarkControl);

            e.Graphics.FillRectangle(
                backgroundBrush,
                e.Bounds);

            using Pen borderPen =
                new Pen(DarkBorder);

            e.Graphics.DrawRectangle(
                borderPen,
                e.Bounds.X,
                e.Bounds.Y,
                e.Bounds.Width - 1,
                e.Bounds.Height - 1);

            Rectangle textBounds = new Rectangle(
                e.Bounds.X + 6,
                e.Bounds.Y,
                e.Bounds.Width - 8,
                e.Bounds.Height);

            TextFormatFlags flags =
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis;

            if (e.Header.TextAlign == HorizontalAlignment.Center)
                flags |= TextFormatFlags.HorizontalCenter;
            else if (e.Header.TextAlign == HorizontalAlignment.Right)
                flags |= TextFormatFlags.Right;
            else
                flags |= TextFormatFlags.Left;

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                e.Font,
                textBounds,
                DarkText,
                flags);
        }
        private static void ListView_DrawItem(
            object sender,
            DrawListViewItemEventArgs e)
        {
            // In Details i singoli SubItem vengono disegnati
            // da ListView_DrawSubItem.
            if (((ListView)sender).View == View.Details)
                return;

            bool selected = e.Item.Selected;

            DrawingColor background =
                selected ? DarkSelected : DarkPanel;

            using SolidBrush brush =
                new SolidBrush(background);

            e.Graphics.FillRectangle(
                brush,
                e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                e.Item.Text,
                e.Item.Font,
                e.Bounds,
                DarkText,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }

        private static void ListView_DrawSubItem(
            object sender,
            DrawListViewSubItemEventArgs e)
        {
            ListView listView = (ListView)sender;

            bool selected = e.Item.Selected;

            DrawingColor background =
                selected ? DarkSelected : DarkPanel;

            DrawingColor foreground =
               e.Item.ForeColor.ToArgb() != DrawingSystemColors.WindowText.ToArgb() &&
                e.Item.ForeColor.ToArgb() != DarkText.ToArgb()
                    ? AccentText
                    : DarkText;

            using SolidBrush backgroundBrush =
                new SolidBrush(background);

            e.Graphics.FillRectangle(
                backgroundBrush,
                e.Bounds);

            Rectangle textBounds = new Rectangle(
                e.Bounds.X + 4,
                e.Bounds.Y,
                e.Bounds.Width - 6,
                e.Bounds.Height);

            TextFormatFlags flags =
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis;

            if (e.Header != null)
            {
                if (e.Header.TextAlign == HorizontalAlignment.Center)
                    flags |= TextFormatFlags.HorizontalCenter;
                else if (e.Header.TextAlign == HorizontalAlignment.Right)
                    flags |= TextFormatFlags.Right;
                else
                    flags |= TextFormatFlags.Left;
            }
            else
            {
                flags |= TextFormatFlags.Left;
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem.Text,
                listView.Font,
                textBounds,
                foreground,
                flags);
        }
        // ============================================================
        // TAB CONTROL
        // ============================================================

        private static void ConfigureTabControl(
            TabControl tabControl,
            AppTheme theme)
        {
            if (theme == AppTheme.Dark)
            {
                tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;

                tabControl.DrawItem -= TabControl_DrawItem;
                tabControl.DrawItem += TabControl_DrawItem;

                if (!TabPainters.ContainsKey(tabControl))
                {
                    TabPainters[tabControl] =
                        new TabHeaderPainter(tabControl);
                }
            }
            else
            {
                tabControl.DrawItem -= TabControl_DrawItem;
                tabControl.DrawMode = TabDrawMode.Normal;

                if (TabPainters.TryGetValue(tabControl, out var painter))
                {
                    painter.ReleaseHandle();
                    TabPainters.Remove(tabControl);
                }
            }

            tabControl.Invalidate();
        }


        private static void TabControl_DrawItem(
            object sender,
            DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl)
                return;

            if (e.Index < 0 || e.Index >= tabControl.TabPages.Count)
                return;

            TabPage page = tabControl.TabPages[e.Index];
            Rectangle bounds = e.Bounds;

            bool selected =
                e.Index == tabControl.SelectedIndex;

            DrawingColor background =
                selected
                    ? DarkSelected
                    : DarkControl;

            using (SolidBrush brush =
                new SolidBrush(background))
            {
                e.Graphics.FillRectangle(
                    brush,
                    bounds);
            }


            TextRenderer.DrawText(
                e.Graphics,
                page.Text,
                tabControl.Font,
                bounds,
                DarkText,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);


            using (Pen borderPen =
                new Pen(DarkBorder))
            {
                e.Graphics.DrawRectangle(
                    borderPen,
                    bounds.X,
                    bounds.Y,
                    bounds.Width - 1,
                    bounds.Height - 1);
            }


            // Accent line on active tab
            if (selected)
            {
                using Pen accentPen =
                    new Pen(Accent, 2);

                e.Graphics.DrawLine(
                    accentPen,
                    bounds.Left + 1,
                    bounds.Bottom - 2,
                    bounds.Right - 2,
                    bounds.Bottom - 2);
            }
        }


        // ============================================================
        // TOOLSTRIP
        // ============================================================

        private static void ApplyToolStrip(
            ToolStrip strip,
            AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            strip.BackColor = dark
                ? DarkControl
                : DrawingSystemColors.Control;

            strip.ForeColor = dark
                ? DarkText
                : DrawingSystemColors.ControlText;


            if (dark)
            {
                strip.Renderer =
                    new DarkToolStripRenderer();
            }
            else
            {
                strip.RenderMode =
                    ToolStripRenderMode.System;
            }


            foreach (ToolStripItem item in strip.Items)
            {
                ApplyToolStripItem(
                    item,
                    theme);
            }

            strip.Invalidate();
        }


        private static void ApplyToolStripItem(
            ToolStripItem item,
            AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            item.BackColor = dark
                ? DarkControl
                : DrawingSystemColors.Control;

            item.ForeColor = dark
                ? DarkText
                : DrawingSystemColors.ControlText;


            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.BackColor =
                    dark
                        ? DarkControl
                        : DrawingSystemColors.Control;

                dropDownItem.DropDown.ForeColor =
                    dark
                        ? DarkText
                        : DrawingSystemColors.ControlText;


                if (dark)
                {
                    dropDownItem.DropDown.Renderer =
                        new DarkToolStripRenderer();
                }
                else
                {
                    dropDownItem.DropDown.RenderMode =
                        ToolStripRenderMode.System;
                }


                foreach (
                    ToolStripItem child
                    in dropDownItem.DropDownItems)
                {
                    ApplyToolStripItem(
                        child,
                        theme);
                }
            }
        }


        // ============================================================
        // DARK TOOLSTRIP RENDERER
        // ============================================================

        private sealed class DarkToolStripRenderer
            : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer()
                : base(new DarkColorTable())
            {
            }


            protected override void OnRenderItemText(
                ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = DarkText;

                base.OnRenderItemText(e);
            }


            protected override void OnRenderSeparator(
                ToolStripSeparatorRenderEventArgs e)
            {
                int y =
                    e.Item.Height / 2;

                using Pen pen =
                    new Pen(DarkBorder);

                e.Graphics.DrawLine(
                    pen,
                    4,
                    y,
                    e.Item.Width - 4,
                    y);
            }
        }


        // ============================================================
        // DARK COLOR TABLE
        // ============================================================

        private sealed class DarkColorTable
            : ProfessionalColorTable
        {
            public override DrawingColor ToolStripDropDownBackground
                => DarkControl;

            public override DrawingColor MenuBorder
                => DarkBorder;

            public override DrawingColor MenuItemBorder
                => DarkBorder;

            public override DrawingColor MenuItemSelected
                => DarkHover;

            public override DrawingColor MenuItemSelectedGradientBegin
                => DarkHover;

            public override DrawingColor MenuItemSelectedGradientEnd
                => DarkHover;

            public override DrawingColor MenuItemPressedGradientBegin
                => DarkSelected;

            public override DrawingColor MenuItemPressedGradientMiddle
                => DarkSelected;

            public override DrawingColor MenuItemPressedGradientEnd
                => DarkSelected;

            public override DrawingColor ImageMarginGradientBegin
                => DarkControl;

            public override DrawingColor ImageMarginGradientMiddle
                => DarkControl;

            public override DrawingColor ImageMarginGradientEnd
                => DarkControl;

            public override DrawingColor SeparatorDark
                => DarkBorder;

            public override DrawingColor SeparatorLight
                => DarkBorder;

            public override DrawingColor ToolStripBorder
                => DarkBorder;

            public override DrawingColor ToolStripGradientBegin
                => DarkControl;

            public override DrawingColor ToolStripGradientMiddle
                => DarkControl;

            public override DrawingColor ToolStripGradientEnd
                => DarkControl;
        }

        private sealed class TabHeaderPainter : NativeWindow
        {
            private readonly TabControl _tabControl;
            public TabHeaderPainter(TabControl tabControl)
            {
                _tabControl = tabControl;

                if (tabControl.IsHandleCreated)
                {
                    AssignHandle(tabControl.Handle);
                }
                else
                {
                    tabControl.HandleCreated += TabControl_HandleCreated;
                }

                tabControl.HandleDestroyed += TabControl_HandleDestroyed;
                tabControl.Disposed += TabControl_Disposed;
            }

            private void TabControl_HandleCreated(object sender, EventArgs e)
            {
                if (Handle == IntPtr.Zero)
                {
                    AssignHandle(_tabControl.Handle);
                }
            }

            private void TabControl_HandleDestroyed(object sender, EventArgs e)
            {
                if (Handle != IntPtr.Zero)
                {
                    ReleaseHandle();
                }
            }

            private void TabControl_Disposed(object sender, EventArgs e)
            {
                if (Handle != IntPtr.Zero)
                {
                    ReleaseHandle();
                }

                _tabControl.HandleCreated -= TabControl_HandleCreated;
                _tabControl.HandleDestroyed -= TabControl_HandleDestroyed;
                _tabControl.Disposed -= TabControl_Disposed;

                TabPainters.Remove(_tabControl);
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_PAINT = 0x000F;

                base.WndProc(ref m);

                if (m.Msg == WM_PAINT &&
                    CurrentTheme == AppTheme.Dark &&
                    !_tabControl.IsDisposed)
                {
                    PaintHeaderBackground();
                }
            }

            private void PaintHeaderBackground()
            {
                if (_tabControl.TabCount == 0)
                    return;

                using Graphics graphics =
                    Graphics.FromHwnd(_tabControl.Handle);

                Rectangle displayArea =
                    _tabControl.DisplayRectangle;

                int headerHeight = displayArea.Top;

                if (headerHeight <= 0)
                    return;

                Rectangle headerArea =
                    new Rectangle(
                        0,
                        0,
                        _tabControl.ClientSize.Width,
                        headerHeight);

                using SolidBrush backgroundBrush =
                    new SolidBrush(DarkBackground);

                graphics.FillRectangle(
                    backgroundBrush,
                    headerArea);

                // Ridisegna le linguette sopra il nuovo background.
                for (int i = 0; i < _tabControl.TabCount; i++)
                {
                    Rectangle bounds =
                        _tabControl.GetTabRect(i);

                    bool selected =
                        i == _tabControl.SelectedIndex;

                    DrawingColor background =
                        selected
                            ? DarkSelected
                            : DarkControl;

                    using SolidBrush tabBrush =
                        new SolidBrush(background);

                    graphics.FillRectangle(
                        tabBrush,
                        bounds);

                    TextRenderer.DrawText(
                        graphics,
                        _tabControl.TabPages[i].Text,
                        _tabControl.Font,
                        bounds,
                        DarkText,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine);

                    using Pen borderPen =
                        new Pen(DarkBorder);

                    graphics.DrawRectangle(
                        borderPen,
                        bounds.X,
                        bounds.Y,
                        bounds.Width - 1,
                        bounds.Height - 1);

                    if (selected)
                    {
                        using Pen accentPen =
                            new Pen(Accent, 2);

                        graphics.DrawLine(
                            accentPen,
                            bounds.Left + 1,
                            bounds.Bottom - 2,
                            bounds.Right - 2,
                            bounds.Bottom - 2);
                    }
                }
            }
        }


    }
}