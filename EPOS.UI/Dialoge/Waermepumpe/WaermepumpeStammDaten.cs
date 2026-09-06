namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Eine Zeile der Wärmepumpen-Liste im Stammdialog (iU9-W7.3).
/// </summary>
/// <param name="Id">Primärschlüssel aus <c>Tab_WP_STAMM</c>.</param>
/// <param name="Bezeichner">Anzeigename.</param>
/// <param name="NurLesen">
/// Auslieferungssatz (<c>ReadOnly</c>). Der Vorläufer zeichnete solche Zeilen GRAU
/// (<c>listBox_WP_DrawItem</c>:187); hier tragen sie <c>aria-disabled</c> und dieselbe
/// gedämpfte Farbe — sie bleiben wählbar, nur nicht änderbar.
/// </param>
public sealed record WaermepumpeStammZeile(int Id, string Bezeichner, bool NurLesen);

/// <summary>
/// Die beiden Kennlinienbilder als fertige PNG (iU9-W7.3) — gezeichnet von
/// <c>ChartRenderer.Kennlinien</c> im Kern, angezeigt von <c>ChartBild</c>.
/// </summary>
/// <param name="Cop">Blatt „COP".</param>
/// <param name="Leistung">Blatt „Leistung".</param>
public sealed record KennlinienBilder(byte[]? Cop, byte[]? Leistung)
{
    /// <summary>Kein Bild — die Blätter zeigen dann ihren Platzhalter.</summary>
    public static readonly KennlinienBilder Leer = new(null, null);
}

/// <summary>
/// Der Feldsatz des Wärmepumpen-Stammdialogs — das plattformfreie Abbild von
/// <c>WPModel</c>, soweit die Maske es zeigt (iU9-W7.3).
///
/// <para><b>Warum ein eigener Typ.</b> Wie bei den Katalogeditoren der Welle 6: Eine
/// Razor-Komponente kennt die Fachklassen des Kerns nicht, und <c>WPModel</c> trägt
/// daneben Maße, Gewicht und Raum, die diese Maske nie anfasst.</para>
///
/// <para><b>Zwei Werte laufen VERBORGEN mit</b> — <see cref="MaxPtherm"/> und
/// <see cref="Id"/>. Die Maske zeigt sie nicht, der Speicherweg schreibt sie aber mit:
/// Der Vorläufer las dafür vor dem <c>Update</c> den Satz mit <c>ReadSingle</c> nach,
/// sonst hätte er <c>maxPtherm</c> genullt. <see cref="Modulkosten"/> lief bis zum
/// 06.09.2026 ebenso mit; seit dem Anwenderentscheid W14a‑O‑1 ZEIGT die Maske ihn —
/// nur lesend, geschrieben wird er weiterhin nur durchgereicht.</para>
/// </summary>
public sealed class WaermepumpeStammDaten
{
    /// <summary>Primärschlüssel; 0 bei einer Neuanlage.</summary>
    public int Id { get; set; }

    /// <summary>Bezeichner (<c>Tab_WP_STAMM.Bezeichner</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Beschreibung (mehrzeilig).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Wärmepumpentyp (Sole-Wasser, Luft-Wasser …).</summary>
    public string Typ { get; set; } = "";

    /// <summary>Baujahr.</summary>
    public int? Baujahr { get; set; }

    /// <summary>Aufstellungsart.</summary>
    public string Aufstellung { get; set; } = "";

    /// <summary>Nennleistung [kW].</summary>
    public int? Nennleistung { get; set; }

    /// <summary>Leistung des elektrischen Heizstabs [kW] (Feld „Heizstab").</summary>
    public int? Heizstab { get; set; }

    /// <summary>Leistungsstufen (einstufig, zweistufig, stetig).</summary>
    public string Regelung { get; set; } = "";

    /// <summary>
    /// Kühlleistung [kW] — im Vorläufer ein Textfeld, das die Maske FÜLLT, aber nie
    /// zurückschreibt (<c>btn_Speichern_Click</c> fasst es nicht an). Deshalb hier nur
    /// lesbar.
    /// </summary>
    public double Kuehlleistung { get; set; }

    /// <summary>
    /// Modulkosten [€] — ein Betrag JE GERÄT, nicht ein spezifischer Satz
    /// (<c>TechnikPlanwertCtrl.BasenFuellen</c>, Fall <c>ERZEUGER_WAERMEPUMPE</c>: Basis
    /// „Modulpreis", der Wert unverändert).
    ///
    /// <para><b>Anwenderentscheid W14a‑O‑1 (06.09.2026):</b> Die Maske ZEIGT den Wert
    /// wieder — als Lesewert mit Herleitungszeile. Ä19 bleibt gewahrt: Es gibt kein
    /// Eingabefeld und keinen Schreibweg aus dem Dialog; gefüllt wird die Spalte allein
    /// vom VDI‑3805‑Import, gepflegt werden Gerätekosten in der Kostenverwaltung. Der
    /// Wert läuft im Speicherweg unverändert mit; bei einer Neuanlage ist er 0, und 0
    /// heißt „kein Planwert" (<c>TechnikPlanwertCtrl.Basis</c> verwirft Beträge ≤ 0).</para>
    /// </summary>
    public int Modulkosten { get; set; }

    /// <summary>
    /// Maximale thermische Leistung. Die Maske zeigt sie nicht; sie muss aber
    /// mitgeschrieben werden, sonst nullt das <c>UPDATE</c> sie.
    /// </summary>
    public int MaxPtherm { get; set; }

    /// <summary>Bauart — die Maske zeigt sie nicht, das <c>INSERT</c> schreibt sie.</summary>
    public string Bauart { get; set; } = "";

    /// <summary>Auslieferungssatz? Dann sind Speichern und Löschen gesperrt.</summary>
    public bool NurLesen { get; set; }

    /// <summary>Eine wortgleiche Kopie — der Dialog bearbeitet nie das Original der Hülle.</summary>
    public WaermepumpeStammDaten Kopie() => new()
    {
        Id = Id,
        Name = Name,
        Firma = Firma,
        Beschreibung = Beschreibung,
        Typ = Typ,
        Baujahr = Baujahr,
        Aufstellung = Aufstellung,
        Nennleistung = Nennleistung,
        Heizstab = Heizstab,
        Regelung = Regelung,
        Kuehlleistung = Kuehlleistung,
        Modulkosten = Modulkosten,
        MaxPtherm = MaxPtherm,
        Bauart = Bauart,
        NurLesen = NurLesen
    };
}
