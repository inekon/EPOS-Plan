using System;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;


public partial class ucKostenZeile : UserControl
{
    public KostenPosition Daten { get; private set; }

    public ucKostenZeile(KostenPosition pos)
    {
        InitializeComponent();

        this.Margin = new Padding(5, 5, 5, 5);
        this.Daten = pos;
        lblName.Text = pos.Name;
        lblEinheit.Text = pos.Einheit;

        // ETAPPE K5: Ein Zuschuss wird POSITIV erfasst, wirkt aber mindernd. Damit
        // niemand die Zahl für zusätzliche Kosten hält, trägt die Einheitenspalte das
        // Minuszeichen — die Zeile liest sich dann als „12.000  − €". Der Wert selbst
        // bleibt unangetastet; ein negatives Drehfeld hier wäre die zweite Wahrheit
        // über dasselbe Vorzeichen (die erste steht im Rechenkern).
        if (pos.IstZuschuss)
        {
            lblEinheit.Text = "− " + pos.Einheit;
            lblName.ForeColor = Color.FromArgb(0x1B, 0x5E, 0x20);
        }

        numBetrag.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
        numBetrag.Increment = 1M;  // Schritte beim Klicken

        // Obergrenze: der Designer stand auf 99.999 €. Das reicht für ein Modul, nicht
        // für eine Anlage — das Beispiel-BHKW „2G 250kw.el Gas" kommt über den
        // spezifischen Preis auf 163.400 € (653,60 €/kWel × 250 kWel), und jedes Setzen
        // eines größeren Wertes hätte eine ArgumentOutOfRangeException geworfen.
        // Programmatisch statt im Designer, damit die generierte Datei unberührt bleibt.
        numBetrag.Maximum = 100000000M;

        // Untergrenze: Der Designer lässt sie auf 0, und damit machte Klemme() aus jedem
        // negativen Betrag stillschweigend eine 0 — Erlöse waren so weder eingebbar noch
        // darstellbar (Etappe E3, Leitentscheidung L5). Für ERLÖSpositionen wird die
        // Grenze gespiegelt: Sie dürfen nur ≤ 0 sein, Kostenpositionen weiterhin nur ≥ 0.
        // Damit kann ein Erlös nirgends als Kosten in eine Summe geraten — dieselbe Regel,
        // die BetriebskostenCtrl.Betrag rechnerisch erzwingt.
        if (pos.IstErloes)
        {
            numBetrag.Minimum = -100000000M;
            numBetrag.Maximum = 0M;
        }

        numDauer.DecimalPlaces = 2; // Erlaubt zwei Nachkommastellen
        numDauer.Increment = 0.5M;  // Schritte beim Klicken
        numBetrag.Value = Klemme(pos.Betrag, numBetrag.Minimum, numBetrag.Maximum);

        // Abgeleitete Positionen: Der Betrag entsteht aus Menge × Einheitpreis und darf
        // hier nicht überschrieben werden — sonst liefen der gespeicherte Betrag und die
        // gespeicherte Herleitung auseinander. Gesperrt und SICHTBAR gekennzeichnet, nicht
        // still geleert (Konzept 4.1; die Altanwendung leerte die Absolutfelder
        // kommentarlos, Befund 6).
        if (pos.Abgeleitet)
        {
            numBetrag.ReadOnly = true;
            numBetrag.Increment = 0M;
            numBetrag.BackColor = SystemColors.Control;
            numBetrag.Cursor = Cursors.No;
            if (!string.IsNullOrEmpty(pos.Herleitung))
                toolTip1.SetToolTip(numBetrag, pos.Herleitung);
        }
        numDauer.Value = Klemme(pos.Nutzungsdauer, numDauer.Minimum, numDauer.Maximum);

        // Events abfangen, um Änderungen zurück ins Objekt zu schreiben
        numBetrag.ValueChanged += (s, e) => { pos.Betrag = (decimal)numBetrag.Value; OnValueChanged(); };
        numDauer.ValueChanged += (s, e) => { pos.Nutzungsdauer = (decimal)numDauer.Value; OnValueChanged(); };

        // Abstand: Links=0, Oben=2, Rechts=0, Unten=2
        this.Margin = new Padding(0, 2, 0, 0);

        btn_Delete.Visible = false;
        if (!pos.IsMainComponent)
        {
            btn_Delete.Font = new Font("Segoe MDL2 Assets", 12, FontStyle.Bold);
            btn_Delete.Text = "\u2796";
            // Optional: Rand entfernen für flachen Look
            btn_Delete.FlatStyle = FlatStyle.Flat;
            btn_Delete.FlatAppearance.BorderSize = 0;
            btn_Delete.ForeColor = Color.DarkGray;
            btn_Delete.Size = new Size(25, 25);
            btn_Delete.Visible = true;  
        }

        btnOpenCases.Height = 18;
        btnOpenCases.Top = numDauer.Top; //(this.ClientSize.Height - btnOpenCases.Height) / 2;
        btnOpenCases.MaximumSize = new Size(33, 18); // Breite, Höhe
        btnOpenCases.MinimumSize = new Size(0, 18);   // Verhindert das Schrumpfen
        btnOpenCases.FlatStyle = FlatStyle.Flat;
        btnOpenCases.UseVisualStyleBackColor = false; // Verhindert, dass das System die Kontrolle übernimmt
        btnOpenCases.FlatAppearance.BorderSize = 1; // Ein Rahmen von 1 Pixel
        btnOpenCases.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215); // Das Windows-Blau
        btnOpenCases.ForeColor = Color.Black;
        btnOpenCases.TabStop = false;
        btnOpenCases.UseCompatibleTextRendering = true; // Hilft manchmal bei Farb-Glitching

        toolTip1.ToolTipTitle = "Preis-Szenarien";
        toolTip1.ToolTipIcon = ToolTipIcon.Info;
        toolTip1.IsBalloon = true; // Macht den Tooltip zu einer Sprechblase (optional)

        UpdateTooltip();
    }

    // Neues Event für das Löschen
    public event EventHandler DeleteRequested;

    // Im Konstruktor oder via Designer das Click-Event des Buttons abonnieren
    private void btnDelete_Click(object sender, EventArgs e)
    {
        // Wir feuern das Event, damit das FlowLayoutPanel weiß: "Ich muss weg!"
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetBerechnetenWert(decimal wert)
    {
        decimal w = Klemme(wert, numBetrag.Minimum, numBetrag.Maximum);

        // Control anpassen, das den Betrag anzeigt.
        if (this.numBetrag.InvokeRequired)
        {
            this.Invoke(new Action(() => this.numBetrag.Value = w));
        }
        else
        {
            this.numBetrag.Value = w;
        }
    }

    /// <summary>
    /// Hält einen Wert in den Grenzen des Drehfeldes. Ein Betrag aus der Datenbank kann
    /// außerhalb liegen (Altbestand, importierte Daten); <c>NumericUpDown.Value</c> wirft
    /// dann eine Ausnahme und riss bislang den Aufbau der ganzen Positionsliste ab.
    /// </summary>
    private static decimal Klemme(decimal wert, decimal min, decimal max)
    {
        if (wert < min) return min;
        if (wert > max) return max;
        return wert;
    }

    /// <summary>
    /// Worst/Best Case dieser Zeile — seit iU9-W1.3 die Razor-Komponente
    /// <c>CaseEingabeDialog</c>; die WinForms-Maske <c>Form_CaseEingabe</c> ist im
    /// selben Schritt gelöscht (Regel M1).
    ///
    /// <para><b>Zum Stand dieser Datei.</b> <c>ucKostenZeile</c> hängt an
    /// <c>Form_Kosten</c> und ist seit KD6a über die Oberfläche nicht mehr
    /// erreichbar (K6-Erreichbarkeitsbefund vom 03.09.2026); nach dem
    /// Anwenderentscheid iF29 wird die Maske stillgelegt. Bis dahin bleibt dieser
    /// Aufruf funktionsgleich: Er ist der EINZIGE, der das Zuschuss-Kennzeichen
    /// wirklich zurückliest, und schreibt wie bisher in <c>this.Daten</c>.</para>
    /// </summary>
    private void btnOpenCases_Click(object sender, EventArgs e)
    {
        KostenPosition daten = this.Daten;

        // ZuschussSchalterAnlegen: keine Hauptkomponente UND Kostenart leer,
        // kapitalgebunden oder bereits Zuschuss. Eine LEERE Kostenart zählt mit —
        // sonst bliebe der Schalter in einer nie migrierten Datenbank verborgen.
        bool zuschussMoeglich = !daten.IsMainComponent &&
            (daten.IstZuschuss ||
             string.IsNullOrEmpty(daten.Kostenart) ||
             string.Equals(daten.Kostenart, DbWerte.KOSTENART_KAPITALGEBUNDEN,
                           StringComparison.OrdinalIgnoreCase));

        CaseEingabeErgebnis ergebnis = null;
        BlazorDialogForm<CaseEingabeDialog> dlg = null;

        var werte = new System.Collections.Generic.Dictionary<string, object>
        {
            ["Betrag"] = (double)daten.Betrag,
            ["BestCase"] = (double)daten.BestCase,
            ["WorstCase"] = (double)daten.WorstCase,
            ["BestNutzungsdauer"] = (double)daten.BestCase_Nutzungsdauer,
            ["WorstNutzungsdauer"] = (double)daten.WorstCase_Nutzungsdauer,
            ["StartJahr"] = daten.StartJahr,
            ["IstZuschuss"] = daten.IstZuschuss,
            ["ZuschussMoeglich"] = zuschussMoeglich,
            ["IstErloes"] = daten.IstErloes,

            ["TitelText"] = Text_("KCASE_TITEL", "Eingabe Worst/Best Case"),
            ["LabelAbsolut"] = Text_("KOSTEN_CASE_ABSOLUT", "Eingabe absolut [€]"),
            ["LabelProzent"] = Text_("KOSTEN_CASE_PROZENT", "Eingabe in % vom Erwartungswert"),
            ["VorlageUmrechnung"] = Text_("KOSTEN_CASE_UMRECHNUNG", "ergibt: Best {0:N2} € · Worst {1:N2} €"),
            ["LabelKosten"] = Text_("KCASE_G_KOSTEN", "Kosten:"),
            ["LabelNutzungsdauer"] = Text_("KCASE_G_NUTZUNGSDAUER", "Nutzungsdauer:"),
            ["LabelBestKosten"] = Text_("KCASE_BEST_EUR", "Best Case [€]:"),
            ["LabelWorstKosten"] = Text_("KCASE_WORST_EUR", "Worst Case [€]:"),
            ["LabelBestNutzung"] = Text_("KCASE_BEST_A", "Best Case [a]:"),
            ["LabelWorstNutzung"] = Text_("KCASE_WORST_A", "Worst Case [a]:"),
            ["LabelStartJahr"] = Text_("KOSTEN_CASE_STARTJAHR",
                "Startjahr (0 = sofort; Jahr X: Zahlung/Betrieb ab X):"),
            ["LabelZuschuss"] = WindowsFormsApplication1.MyResource.Resource.KOSTEN_CHK_ZUSCHUSS,
            ["HinweisZuschuss"] = WindowsFormsApplication1.MyResource.Resource.KOSTEN_CHK_ZUSCHUSS_HINT,
            ["HinweisErloes"] = Text_("KCASE_ERLOES_HINWEIS",
                "Erlösposition: Die Werte werden als Betrag eingegeben; das negative Vorzeichen setzt die Rechnung."),
            ["OkText"] = WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_OK,
            ["AbbrechenText"] = WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_ABBRECHEN,

            ["Geschlossen"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                .Create<CaseEingabeErgebnis>(this, erg =>
                {
                    ergebnis = erg;
                    if (dlg != null) dlg.Schliessen(erg != null);
                })
        };

        dlg = new BlazorDialogForm<CaseEingabeDialog>(
            Text_("KCASE_TITEL", "Eingabe Worst/Best Case"), new Size(560, 620), werte);

        using (dlg)
        {
            if (dlg.ShowDialog() != DialogResult.OK || ergebnis == null) return;

            // Wortgleich zu btn_OK_Click der gelöschten Maske.
            daten.BestCase = (decimal)ergebnis.BestCase;
            daten.WorstCase = (decimal)ergebnis.WorstCase;
            daten.BestCase_Nutzungsdauer = (decimal)ergebnis.BestNutzungsdauer;
            daten.WorstCase_Nutzungsdauer = (decimal)ergebnis.WorstNutzungsdauer;
            daten.IstZuschuss = ergebnis.IstZuschuss;
            daten.StartJahr = ergebnis.StartJahr;

            // Den Tooltip sofort aktualisieren
            UpdateTooltip();
            // WICHTIG: Event feuern, damit das Hauptformular weiß: "Hier hat sich was geändert!"
            OnValueChanged();
        }
    }

    private static string Text_(string schluessel, string rueckfall)
    {
        string t = null;
        try { t = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    public void UpdateTooltip()
    {
        // mehrzeiligen String
        string info = $"📊 Kalkulations-Varianten:\n" +
                      $"--------------------------\n" +
                      $"Best Case Kosten:  {Daten.BestCase:N2} €\n" +
                      $"Worst Case Kosten: {Daten.WorstCase:N2} €\n" +
                      $"Best Case Nutzungsdauer:  {Daten.BestCase_Nutzungsdauer:N2} €\n" +
                      $"Worst Case Nutzungsdauer: {Daten.WorstCase_Nutzungsdauer:N2} €";

        // "btnOpenCases" ist +/- Button
        toolTip1.SetToolTip(btnOpenCases, info);

        // Optional: Färbe den Button ein, wenn Werte hinterlegt sind
        if (Daten.BestCase != 0 || Daten.WorstCase != 0)
        {
            // Schrift bleibt schwarz, aber der Rahmen wird dicker oder die Fläche dezent farbig
            btnOpenCases.FlatAppearance.BorderColor = Color.DeepSkyBlue;
            btnOpenCases.FlatAppearance.BorderSize = 2; // Deutlicher Hinweis
            btnOpenCases.ForeColor = Color.Black; // Sicherstellen, dass sie schwarz bleibt
        }
        else
        {
            btnOpenCases.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            btnOpenCases.FlatAppearance.BorderSize = 1;
            btnOpenCases.ForeColor = Color.Black;
        }
    }

    public event EventHandler ValueChanged;
    protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
}

public class KostenPosition
{
    public int ID { get; set; } // Der Primärschlüssel (Autowert) aus Tab_ProjektWerte
    public string Name { get; set; }
    public string Gruppe { get; set; } // z.B. "Infrastruktur"
    public decimal Betrag { get; set; }
    public decimal WorstCase { get; set; }
    public decimal BestCase { get; set; }
    public decimal WorstCase_Nutzungsdauer { get; set; }
    public decimal BestCase_Nutzungsdauer { get; set; }
    public string Einheit { get; set; }
    public decimal Nutzungsdauer { get; set; }
    public string Gruppenname { get; set; }
    public bool IsMainComponent { get; set; }
    public int StammID { get; set; } // Optional: Verweis auf die Stammdaten, falls benötigt
    public string Komponente { get; set; }

    // ------------------------------------------------- Etappe E3 (Migrationsschritt 19)

    /// <summary>
    /// Erlösposition (<c>Tab_ProjektWerte.IstErloes</c>) — nur dann darf der Betrag
    /// negativ sein. Vorbelegung false, also das Verhalten des gesamten Bestands.
    /// </summary>
    public bool IstErloes { get; set; }

    /// <summary>
    /// Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>). Vorbelegung
    /// <see cref="DbWerte.BEMESSUNG_BETRAG"/> — der fest eingegebene Jahresbetrag und
    /// damit das Verhalten jeder Bestandszeile.
    /// </summary>
    public string Bemessung { get; set; } = DbWerte.BEMESSUNG_BETRAG;

    /// <summary>
    /// true, wenn der Betrag aus Menge × Einheitpreis entsteht und deshalb nicht von
    /// Hand geändert werden darf.
    /// </summary>
    public bool Abgeleitet
    {
        get
        {
            return !string.IsNullOrEmpty(Bemessung) &&
                   !string.Equals(Bemessung, DbWerte.BEMESSUNG_BETRAG, System.StringComparison.Ordinal);
        }
    }

    /// <summary>Klartext der Herleitung für den Hinweis am gesperrten Feld.</summary>
    public string Herleitung { get; set; } = "";

    // ------------------------------------------------- Etappe KD6 (Konzept § 11, FK10)

    /// <summary>
    /// Startjahr der Position (<c>Tab_ProjektWerte.StartJahr</c>): 0 = t0
    /// (NULL in der Datenbank, Bestand); X ≥ 2 = Investition erst im Jahr X,
    /// Betrieb ab X — Rechenwirkung im <c>KapitalwertRechner</c>.
    /// </summary>
    public int StartJahr { get; set; }

    // ------------------------------------------------- Etappe K5 (Konzept § 7.4)

    /// <summary>
    /// Kostenart der Position (<c>Tab_ProjektWerte.Kostenart</c>, Steuerwert
    /// <c>DbWerte.KOSTENART_*</c>). Leer = wie bisher, also die Vorbelegung aus
    /// Migrationsschritt 19b.
    /// </summary>
    public string Kostenart { get; set; } = "";

    /// <summary>
    /// true, wenn die Position ein <b>Investitionszuschuss</b> ist: Der erfasste
    /// (positive) Betrag mindert dann die Anfangsauszahlung, statt sie zu erhöhen.
    /// Setzen und Löschen geht über dieselbe Eigenschaft — sie ist der einzige Weg,
    /// die Kostenart aus der Oberfläche zu ändern, und hält damit den Steuerwert
    /// beisammen.
    /// </summary>
    public bool IstZuschuss
    {
        get
        {
            return string.Equals(Kostenart, DbWerte.KOSTENART_ZUSCHUSS,
                                 System.StringComparison.OrdinalIgnoreCase);
        }
        set
        {
            if (value) Kostenart = DbWerte.KOSTENART_ZUSCHUSS;
            else if (IstZuschuss) Kostenart = DbWerte.KOSTENART_KAPITALGEBUNDEN;
        }
    }
}
