using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Один замер системной нагрузки.
    /// </summary>
    public class PerfSample
    {
        public DateTime Time;
        public double Cpu;   // % системы (0..100)
        public double Ram;   // % физической памяти
        public double Gpu;   // % GPU (-1 если нет данных)
        public double Disk;  // МБ/с суммарно
    }

    /// <summary>
    /// Живой график нагрузки в стиле системного монитора Windows.
    /// Кольцевой буфер + отрисовка через Paint.
    /// </summary>
    public class PerformanceGraph : Panel
    {
        private const int Capacity = 120; // 120 сканов = ~6 минут при 3с интервале

        private readonly List<PerfSample> _samples = new List<PerfSample>();

        public bool ShowCpu { get; set; } = true;
        public bool ShowRam { get; set; } = true;
        public bool ShowGpu { get; set; } = true;
        public bool ShowDisk { get; set; } = true;

        public PerformanceGraph()
        {
            BackColor = Color.FromArgb(20, 20, 24);
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void Add(PerfSample s)
        {
            _samples.Add(s);
            if (_samples.Count > Capacity) _samples.RemoveAt(0);
            Invalidate();
        }

        public void Clear()
        {
            _samples.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Draw(e.Graphics);
        }

        private void Draw(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle area = ClientRectangle;
            if (area.Width < 50 || area.Height < 50) return;

            // Фон
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 20, 24)))
                g.FillRectangle(bg, area);

            // Отступы: слева — для оси Y, сверху — для легенды, снизу — для оси X
            const int padLeft = 48;
            const int padTop = 28;
            const int padRight = 12;
            const int padBottom = 22;
            Rectangle plot = new Rectangle(padLeft, padTop,
                area.Width - padLeft - padRight,
                area.Height - padTop - padBottom);

            if (plot.Width < 20 || plot.Height < 20) return;

            // Сетка: 5 горизонтальных линий (0, 25, 50, 75, 100)
            using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 60), 1))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Top + plot.Height * i / 4;
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                }
            }

            // Подписи оси Y (0..100)
            using (Font f = new Font("Segoe UI", 7.5f))
            using (SolidBrush txt = new SolidBrush(Color.FromArgb(160, 160, 180)))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int val = 100 - i * 25;
                    int y = plot.Top + plot.Height * i / 4 - 6;
                    g.DrawString(val.ToString() + "%", f, txt, 4, y);
                }
            }

            // Легенда сверху
            DrawLegend(g, area.Width);

            // Рисуем линии
            if (_samples.Count >= 2)
            {
                double yMax = 100.0; // для CPU/RAM/Gpu
                double diskMax = GetDiskMax();
                if (diskMax < 50) diskMax = 50;

                if (ShowCpu) DrawLine(g, plot, _samples, s => s.Cpu, 100.0,
                    Color.FromArgb(80, 220, 120), 2);
                if (ShowRam) DrawLine(g, plot, _samples, s => s.Ram, 100.0,
                    Color.FromArgb(100, 160, 255), 2);
                if (ShowGpu) DrawLine(g, plot, _samples, s => s.Gpu, 100.0,
                    Color.FromArgb(255, 180, 60), 2);
                if (ShowDisk) DrawLine(g, plot, _samples, s => s.Disk, diskMax,
                    Color.FromArgb(200, 120, 255), 2);
            }
            else
            {
                using (Font f = new Font("Segoe UI", 9f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 120, 140)))
                {
                    g.DrawString("Ожидание данных сканирования...", f, b,
                        plot.Left + 10, plot.Top + plot.Height / 2 - 8);
                }
            }
        }

        private void DrawLine(Graphics g, Rectangle plot, List<PerfSample> samples,
            Func<PerfSample, double> value, double max, Color color, float width)
        {
            using (Pen pen = new Pen(color, width))
            using (Pen glow = new Pen(Color.FromArgb(60, color.R, color.G, color.B), width + 3))
            {
                // Свечение
                DrawPoly(g, glow, plot, samples, value, max);
                // Основная линия
                DrawPoly(g, pen, plot, samples, value, max);
            }
        }

        private void DrawPoly(Graphics g, Pen pen, Rectangle plot, List<PerfSample> samples,
            Func<PerfSample, double> value, double max)
        {
            if (max <= 0) return;
            Point[] pts = new Point[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                double v = value(samples[i]);
                if (v < 0) v = 0;
                if (v > max) v = max;
                int x = plot.Left + (plot.Width - 1) * i / Math.Max(1, samples.Count - 1);
                int y = plot.Bottom - 1 - (int)((plot.Height - 1) * v / max);
                pts[i] = new Point(x, y);
            }
            g.DrawLines(pen, pts);
        }

        private void DrawLegend(Graphics g, int width)
        {
            using (Font f = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush txt = new SolidBrush(Color.FromArgb(220, 220, 230)))
            {
                int x = 12;
                int y = 6;
                int spacing = 130;

                PerfSample last = _samples.Count > 0 ? _samples[_samples.Count - 1] : null;

                DrawLegendItem(g, f, txt, ref x, y, "CPU", Color.FromArgb(80, 220, 120),
                    last != null ? last.Cpu.ToString("0") + "%" : "—");
                x += spacing;
                DrawLegendItem(g, f, txt, ref x, y, "RAM", Color.FromArgb(100, 160, 255),
                    last != null ? last.Ram.ToString("0") + "%" : "—");
                x += spacing;
                DrawLegendItem(g, f, txt, ref x, y, "GPU", Color.FromArgb(255, 180, 60),
                    last != null && last.Gpu >= 0 ? last.Gpu.ToString("0") + "%" : "—");
                x += spacing;
                DrawLegendItem(g, f, txt, ref x, y, "DISK", Color.FromArgb(200, 120, 255),
                    last != null ? last.Disk.ToString("0") + " МБ/с" : "—");
            }
        }

        private void DrawLegendItem(Graphics g, Font f, SolidBrush txt, ref int x, int y,
            string label, Color color, string value)
        {
            Rectangle swatch = new Rectangle(x, y, 12, 12);
            using (SolidBrush b = new SolidBrush(color))
                g.FillRectangle(b, swatch);
            g.DrawString(label + ": " + value, f, txt, x + 16, y - 1);
        }

        private double GetDiskMax()
        {
            double m = 0;
            foreach (PerfSample s in _samples)
                if (s.Disk > m) m = s.Disk;
            return m;
        }
    }
}