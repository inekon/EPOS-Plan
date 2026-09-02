using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Wechselrichterangaben EINER PV-Anlage (Stufe E2.1/E2.2 des
    /// <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>): AC-Nennleistung und die
    /// drei Punkte der Teillastkennlinie, dazu die Live-Kennzahl DC/AC.
    ///
    /// <para><b>Warum ein eigener Dialog.</b> Das Panel „PV Anlage Eigenschaften" in
    /// <see cref="Form_PV"/> ist 420 x 128 Pixel gross und damit voll; rechts daneben
    /// beginnt bei x = 449 die Modulliste, unten bei y = 403 der Modulblock. Vier
    /// weitere Felder passen dort nicht hinein, ohne die ganze Maske umzubauen. Der
    /// Knopf „Wechselrichter…" fuehrt deshalb hierher.</para>
    ///
    /// <para><b>PROGRAMMATISCH, ohne Designer- und .resx-Datei.</b> Hausregel des
    /// Hauptprojekts (CLAUDE.md): Designer- und <c>.resx</c>-Dateien werden nicht von
    /// Hand editiert. Die Texte kommen aus <c>MyResource</c> (Praefix <c>PVM_*</c>,
    /// deutsch und englisch).</para>
    ///
    /// <para><b>0 heisst NULL</b> — in jedem der vier Felder. Ein
    /// <see cref="NumericUpDown"/> kann nicht leer sein; die Unterscheidung „nie
    /// gepflegt" gegen „ausdruecklich gesetzt" laeuft deshalb wie im
    /// PV-Verguetungsdialog ueber die 0. Ohne AC-Nennleistung rechnet die Simulation
    /// OHNE Clipping, ohne Kennlinienpunkte mit den Vorbelegungen 0,94 / 0,975 / 0,97.</para>
    ///
    /// <para><b>Im Modell EINFACH sind alle vier Felder gesperrt</b> (Enabled, nicht
    /// Visible — Vorgabe des Konzepts N2.4): Sie sind dort wirkungslos, aber der
    /// Anwender soll sehen, dass es sie gibt und was in ihnen steht.</para>
    /// </summary>
    public class Form_PVModell : Form
    {
        private readonly double _kwpAnlage;
        private readonly bool _erweitert;

        private NumericUpDown _numNennleistung, _numEta10, _numEta50, _numEta100;
        private Label _lblDcAc;
        private ToolTip _tip;

        /// <summary>AC-Nennleistung [kW]; null = kein Clipping.</summary>
        public double? Nennleistung { get; private set; }

        /// <summary>Wirkungsgrad bei 10 % Auslastung; null = Vorbelegung.</summary>
        public double? Eta10 { get; private set; }

        /// <summary>Wirkungsgrad bei 50 % Auslastung; null = Vorbelegung.</summary>
        public double? Eta50 { get; private set; }

        /// <summary>Wirkungsgrad bei 100 % Auslastung; null = Vorbelegung.</summary>
        public double? Eta100 { get; private set; }

        /// <param name="anlage">Bezeichner der Anlage — steht im Titel.</param>
        /// <param name="kwpAnlage">Nennleistung der Anlage [kWp] fuer die DC/AC-Kennzahl.</param>
        /// <param name="erweitert">true = Modell ERWEITERT; sonst sind die Felder gesperrt.</param>
        public Form_PVModell(string anlage, double kwpAnlage, bool erweitert,
                             double? nennleistung, double? eta10, double? eta50, double? eta100)
        {
            _kwpAnlage = kwpAnlage;
            _erweitert = erweitert;

            Nennleistung = nennleistung;
            Eta10 = eta10;
            Eta50 = eta50;
            Eta100 = eta100;

            Aufbauen(anlage);
            DcAcAktualisieren();
        }

        // =====================================================================
        // Aufbau
        // =====================================================================

        private const int RAND = 16;
        private const int LABEL_BREITE = 250;
        private const int FELD_LINKS = 274;
        private const int FELD_BREITE = 90;
        private const int ZEILE = 30;

        private void Aufbauen(string anlage)
        {
            _tip = new ToolTip();

            Text = string.Format(CultureInfo.CurrentCulture,
                                 MyResource.Resource.PVM_DLG_TITEL, anlage ?? "");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(400, 260);

            int y = RAND;

            Label kopf = new Label();
            kopf.Text = _erweitert
                ? MyResource.Resource.PVM_DLG_KOPF_ERWEITERT
                : MyResource.Resource.PVM_DLG_KOPF_EINFACH;
            kopf.AutoSize = false;
            kopf.Size = new Size(ClientSize.Width - 2 * RAND, 34);
            kopf.Location = new Point(RAND, y);
            kopf.ForeColor = _erweitert ? SystemColors.ControlText : Color.Firebrick;
            Controls.Add(kopf);
            y += 42;

            _numNennleistung = FeldAnlegen(MyResource.Resource.PVM_DLG_NENNLEISTUNG,
                                           MyResource.Resource.PVM_DLG_NENNLEISTUNG_TIP,
                                           ref y, 2, 0M, 100000M, 0.5M, Nennleistung);
            _numEta10 = FeldAnlegen(MyResource.Resource.PVM_DLG_ETA10,
                                    MyResource.Resource.PVM_DLG_ETA_TIP,
                                    ref y, 3, 0M, 1M, 0.005M, Eta10);
            _numEta50 = FeldAnlegen(MyResource.Resource.PVM_DLG_ETA50,
                                    MyResource.Resource.PVM_DLG_ETA_TIP,
                                    ref y, 3, 0M, 1M, 0.005M, Eta50);
            _numEta100 = FeldAnlegen(MyResource.Resource.PVM_DLG_ETA100,
                                     MyResource.Resource.PVM_DLG_ETA_TIP,
                                     ref y, 3, 0M, 1M, 0.005M, Eta100);

            _lblDcAc = new Label();
            _lblDcAc.AutoSize = false;
            _lblDcAc.Size = new Size(ClientSize.Width - 2 * RAND, 19);
            _lblDcAc.Location = new Point(RAND, y + 6);
            _lblDcAc.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Controls.Add(_lblDcAc);

            Button ok = new Button();
            ok.Text = MyResource.Resource.PVM_DLG_OK;
            ok.Size = new Size(100, 28);
            ok.Location = new Point(ClientSize.Width - RAND - 210, ClientSize.Height - RAND - 28);
            ok.DialogResult = DialogResult.OK;
            ok.Click += (s, e) => Uebernehmen();
            Controls.Add(ok);

            Button abbruch = new Button();
            abbruch.Text = MyResource.Resource.PVM_DLG_ABBRECHEN;
            abbruch.Size = new Size(100, 28);
            abbruch.Location = new Point(ClientSize.Width - RAND - 100, ClientSize.Height - RAND - 28);
            abbruch.DialogResult = DialogResult.Cancel;
            Controls.Add(abbruch);

            AcceptButton = ok;
            CancelButton = abbruch;
        }

        /// <summary>Eine Zeile „Beschriftung + Zahlenfeld"; 0 zeigt „nicht gepflegt".</summary>
        private NumericUpDown FeldAnlegen(string beschriftung, string hilfe, ref int y,
                                          int stellen, decimal min, decimal max, decimal schritt,
                                          double? wert)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.AutoSize = false;
            lbl.Size = new Size(LABEL_BREITE, 19);
            lbl.Location = new Point(RAND, y + 3);
            Controls.Add(lbl);

            NumericUpDown num = new NumericUpDown();
            num.DecimalPlaces = stellen;
            num.Minimum = min;
            num.Maximum = max;
            num.Increment = schritt;
            num.Size = new Size(FELD_BREITE, 23);
            num.Location = new Point(FELD_LINKS, y);
            num.TextAlign = HorizontalAlignment.Right;
            num.Enabled = _erweitert;
            num.Value = wert.HasValue
                ? Math.Max(min, Math.Min(max, (decimal)wert.Value))
                : 0M;
            num.ValueChanged += (s, e) => DcAcAktualisieren();
            Controls.Add(num);

            _tip.SetToolTip(lbl, hilfe);
            _tip.SetToolTip(num, hilfe);

            y += ZEILE;
            return num;
        }

        // =====================================================================
        // Live-Kennzahl
        // =====================================================================

        /// <summary>
        /// DC/AC = kWp der Anlage / AC-Nennleistung. Ohne Nennleistung gibt es kein
        /// Verhaeltnis — dann sagt die Zeile, dass ohne Clipping gerechnet wird.
        /// </summary>
        private void DcAcAktualisieren()
        {
            if (_lblDcAc == null || _numNennleistung == null) return;

            double nenn = (double)_numNennleistung.Value;
            if (nenn <= 0.0)
            {
                _lblDcAc.Text = MyResource.Resource.PVM_DLG_DCAC_OHNE;
                return;
            }

            _lblDcAc.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.PVM_DLG_DCAC, _kwpAnlage, nenn, _kwpAnlage / nenn);
        }

        // =====================================================================
        // Uebernehmen
        // =====================================================================

        /// <summary>0 wird zu <c>null</c> — „nicht gepflegt, es gilt der Vorgabewert".</summary>
        private void Uebernehmen()
        {
            Nennleistung = Wert(_numNennleistung);
            Eta10 = Wert(_numEta10);
            Eta50 = Wert(_numEta50);
            Eta100 = Wert(_numEta100);
        }

        private static double? Wert(NumericUpDown num)
        {
            if (num == null) return null;
            return num.Value > 0M ? (double?)num.Value : null;
        }
    }
}
