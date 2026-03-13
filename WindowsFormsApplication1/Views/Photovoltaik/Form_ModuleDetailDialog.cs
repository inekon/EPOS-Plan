using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ModuleDetailDialog : Form
    {
        private readonly PVModule _m;

        private static readonly Color C_Blue = Color.FromArgb(30, 87, 153);
        private static readonly Color C_BlueMid = Color.FromArgb(55, 115, 185);
        private static readonly Color C_Card = Color.White;
        private static readonly Color C_CardBrd = Color.FromArgb(210, 220, 235);
        private static readonly Color C_BG = Color.FromArgb(236, 242, 250);
        private static readonly Color C_Lbl = Color.FromArgb(90, 105, 125);
        private static readonly Color C_Val = Color.FromArgb(20, 50, 110);
        private static readonly Color C_Green = Color.FromArgb(25, 135, 84);

        private static readonly Font F_Title = new Font("Segoe UI", 13f, FontStyle.Bold);
        private static readonly Font F_Sub = new Font("Segoe UI", 9f);
        private static readonly Font F_CardHdr = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font F_Lbl = new Font("Segoe UI", 8.5f);
        private static readonly Font F_Val = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font F_KpiNum = new Font("Segoe UI", 15f, FontStyle.Bold);
        private static readonly Font F_KpiLbl = new Font("Segoe UI", 7.5f);

        public Form_ModuleDetailDialog(PVModule module)
        {
            _m = module;
            InitializeComponent();
            SetupCustomUI();
        }

        private void SetupCustomUI()
        {
            this.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            bool bifacial = _m.Bifacial == "1" || _m.Bifacial.Equals("true", StringComparison.OrdinalIgnoreCase);
            double ff = (_m.I_sc_ref > 0 && _m.V_oc_ref > 0)
                ? (_m.I_mp_ref * _m.V_mp_ref) / (_m.I_sc_ref * _m.V_oc_ref) * 100.0 : 0;

            // Header Inhalt
            header.Controls.Add(new Label
            {
                Text = _m.Name,
                Font = F_Title,
                ForeColor = Color.White,
                Location = new Point(16, 10),
                AutoSize = false,
                Width = 860,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            });
            header.Controls.Add(new Label
            {
                Text = $"{_m.Manufacturer}   ·   {_m.Technology}" + (bifacial ? "   ·   Bifazial" : ""),
                Font = F_Sub,
                ForeColor = Color.FromArgb(180, 210, 245),
                Location = new Point(18, 42),
                AutoSize = true
            });

            btnClose.Click += (s, e) => Close();
            header.Resize += (s, e) => btnClose.Location = new Point(header.Width - 40, 10);

            // KPI Zeile
            var kpiRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            kpiRow.Controls.Add(MakeKpi("STC-Leistung", $"{_m.STC:F1} W", C_Blue));
            kpiRow.Controls.Add(MakeKpi("PTC-Leistung", $"{_m.PTC:F1} W", C_BlueMid));
            kpiRow.Controls.Add(MakeKpi("Effizienz", $"{_m.Efficiency:F2} %", C_Green));
            kpiRow.Controls.Add(MakeKpi("Füllfaktor", $"{ff:F1} %", Color.FromArgb(130, 80, 10)));
            kpiRow.Controls.Add(MakeKpi("Modulfläche", $"{_m.A_c:F3} m²", Color.FromArgb(80, 90, 110)));

            // Karten-Rows
            var cardRow1 = MakeCardRow(
                MakeCard("📐  Abmessungen & Aufbau", new[] {
                    ("Länge × Breite", $"{_m.Length:F3} m × {_m.Width:F3} m"),
                    ("Modulfläche", $"{_m.A_c:F4} m²"),
                    ("Zellen (Reihe)", $"{_m.N_s}"),
                    ("Stränge (parallel)", $"{_m.N_p}"),
                    ("Zellen gesamt", $"{_m.cells_in_series}"),
                    ("Bifazial", bifacial ? "Ja ✓" : "Nein"),
                    ("Technologie", _m.Technology),
                    ("Version / Quelle", _m.Version.Length > 0 ? _m.Version : "—"),
                }),
                MakeCard("⚡  Elektrische Parameter (STC)", new[] {
                    ("I_sc  Kurzschlussstrom", $"{_m.I_sc_ref:F4} A"),
                    ("V_oc  Leerlaufspannung", $"{_m.V_oc_ref:F4} V"),
                    ("I_mp  MPP-Strom", $"{_m.I_mp_ref:F4} A"),
                    ("V_mp  MPP-Spannung", $"{_m.V_mp_ref:F4} V"),
                    ("P_mp  I_mp × V_mp", $"{_m.I_mp_ref * _m.V_mp_ref:F2} W"),
                    ("FF    Füllfaktor", $"{ff:F2} %"),
                })
            );

            var cardRow2 = MakeCardRow(
                MakeCard("🌡  Temperaturkoeffizienten", new[] {
                    ("T_NOCT  Betriebstemperatur", $"{_m.T_NOCT:F1} °C"),
                    ("α_sc    TK Kurzschlussstrom", $"{_m.alpha_sc:+0.000000;-0.000000} A/°C"),
                    ("β_oc    TK Leerlaufspannung", $"{_m.beta_oc:+0.000000;-0.000000} V/°C"),
                    ("γ_r     TK Leistung", $"{_m.gamma_r:+0.0000;-0.0000} %/°C"),
                }),
                new Panel()
            );

            inner.Controls.Add(kpiRow);
            inner.Controls.Add(cardRow1);
            inner.Controls.Add(cardRow2);

            scroll.Resize += (s, e) => {
                inner.Width = Math.Max(scroll.ClientSize.Width - 28, 700);
                foreach (Control c in inner.Controls) c.Width = inner.Width;
                foreach (Control row in inner.Controls) if (row is FlowLayoutPanel fp && fp != kpiRow) ResizeCardRow(fp);
            };

            footer.Resize += (s, e) => btnOk.Location = new Point(footer.Width - 120, 7);
            AcceptButton = btnOk;
        }

        private void Header_Paint(object sender, PaintEventArgs e)
        {
          // C# 7.3 konform
            using (LinearGradientBrush br = new LinearGradientBrush(header.ClientRectangle,
                   C_Blue, Color.FromArgb(20, 65, 130), LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(br, header.ClientRectangle);
            }

        }

        private static Panel MakeKpi(string label, string value, Color accent)
        {
            var p = new Panel { Size = new Size(155, 72), BackColor = C_Card, Margin = new Padding(0, 0, 8, 0) };
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Linker Akzent-Streifen
                using (SolidBrush br = new SolidBrush(accent))
                {
                    e.Graphics.FillRectangle(br, 0, 0, 5, p.Height);
                } // Hier wird br automatisch disposed

                // Border
                using (Pen pen = new Pen(C_CardBrd, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                } // Hier wird pen automatisch disposed
            };
            p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = accent, Location = new Point(12, 10), AutoSize = false, Width = 138, Height = 28, TextAlign = ContentAlignment.MiddleLeft });
            p.Controls.Add(new Label { Text = label, Font = F_KpiLbl, ForeColor = C_Lbl, Location = new Point(13, 42), AutoSize = true });
            return p;
        }

        private Panel MakeCard(string title, (string label, string value)[] rows)
        {
            int rowH = 24; int cardH = 36 + rows.Length * rowH + 8;
            var card = new Panel { BackColor = C_Card, Height = cardH, Margin = new Padding(0, 0, 8, 10) };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      

                // 1. Äußerer Rahmen
                using (Pen pen = new Pen(C_CardBrd, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }

                // 2. Kopfzeile Hintergrund (hellblau/grau)
                using (SolidBrush br = new SolidBrush(Color.FromArgb(244, 247, 253)))
                {
                    e.Graphics.FillRectangle(br, 1, 1, card.Width - 2, 30);
                }

                // 3. Trennlinie unter der Kopfzeile
                using (Pen sep = new Pen(C_CardBrd, 1))
                {
                    e.Graphics.DrawLine(sep, 1, 31, card.Width - 2, 31);
                }

            };
            card.Controls.Add(new Label { Text = title, Font = F_CardHdr, ForeColor = C_Blue, Location = new Point(10, 7), AutoSize = true });
            for (int i = 0; i < rows.Length; i++)
            {
                int y = 36 + i * rowH; bool alt = i % 2 == 0;
                var rowBg = new Panel { Location = new Point(1, y), Height = rowH, BackColor = alt ? Color.FromArgb(250, 252, 255) : Color.White };
                card.Controls.Add(rowBg);
                var lblL = new Label { Text = rows[i].label, Font = F_Lbl, ForeColor = C_Lbl, Location = new Point(10, y + 4), AutoSize = false, Height = 18, Width = 200, TextAlign = ContentAlignment.MiddleLeft };
                var lblV = new Label { Text = rows[i].value, Font = F_Val, ForeColor = C_Val, Location = new Point(214, y + 4), AutoSize = false, Height = 18, TextAlign = ContentAlignment.MiddleLeft };
                card.Controls.Add(lblL); card.Controls.Add(lblV);
                rowBg.SendToBack();
            }
            card.Resize += (e, s) => {
                foreach (Control c in card.Controls)
                {
                    if (c is Panel bg && bg != card) bg.Width = card.Width - 2;
                    if (c is Label lv && lv.Left > 200) lv.Width = card.Width - 224;
                }
            };
            return card;
        }

        private static FlowLayoutPanel MakeCardRow(Panel left, Panel right)
        {
            var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false, Height = Math.Max(left.Height, right.Height) + 2, Margin = new Padding(0, 0, 0, 4) };
            left.Width = 430; right.Width = 430; left.Margin = new Padding(0, 0, 8, 0);
            row.Controls.Add(left); row.Controls.Add(right);
            return row;
        }

        private static void ResizeCardRow(FlowLayoutPanel row)
        {
            if (row.Controls.Count < 2) return;
            int half = (row.Width - 8) / 2;
            row.Controls[0].Width = half; row.Controls[1].Width = half;
        }
    }

    internal class ScrollablePanel : Panel
    {
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            AutoScrollPosition = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y - e.Delta / 3);
        }
    }
}
