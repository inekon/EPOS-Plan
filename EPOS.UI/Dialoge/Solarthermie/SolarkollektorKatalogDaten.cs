namespace EPOS.UI.Dialoge.Solarthermie;

/// <summary>
/// Der Feldsatz des Solarkollektor-Katalogeditors — das plattformfreie Abbild von
/// <c>SolarkollektorenModel</c> (iU9-W7.6).
///
/// <para><b>Warum ein eigener Typ.</b> Wie beim Heizkessel-Katalogeditor (W6.1): Eine
/// Razor-Komponente kennt die Fachklassen des Kerns nicht; die Hülle bildet zwischen
/// beiden ab. Der Typ trägt die 14 Felder der Maske in der Reihenfolge der Feldkarte.</para>
///
/// <para><b>Acht Pflichtzahlen, zwei mit erlaubter Leere.</b> Der Vorläufer
/// (<c>InitDatensatzUpdate</c>) prüft Modulfläche, Aperturfläche, h0, k1, k2, Kdir,
/// Kdiff und die Investitionskosten mit <c>leerErlaubt: false</c>, Vorlauf und Rücklauf
/// mit <c>true</c> — dort galt „" schon bisher als 0. Deshalb sind alle zehn
/// <c>double?</c>/<c>int?</c>: Ein leeres Feld ist etwas anderes als eine 0, und erst
/// der Speicherweg entscheidet, ob das reicht.</para>
///
/// <para><b>Veränderlich, nicht als <c>record</c>.</b> Der Dialog schreibt beim Tippen
/// hinein; die Hülle nimmt den Stand beim Speichern entgegen.</para>
/// </summary>
public sealed class SolarkollektorKatalogDaten
{
    /// <summary>Primärschlüssel des geladenen Katalogsatzes; 0 im Modus „Neu".</summary>
    public int KatalogId { get; set; }

    /// <summary>
    /// Kollektorname. Er ist in BEIDEN Modi nur lesbar — im Designer trägt
    /// <c>textBox_Name</c> <c>Enabled = false</c>. Im Modus „Neu" kommt er aus der
    /// Namensabfrage VOR dem Öffnen, im Modus „Bearbeiten" aus dem Katalogsatz;
    /// umbenannt wird über „Speichern unter".
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Beschreibung (mehrzeilig).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Kollektortyp (Flachkollektor, Vakuumröhre …).</summary>
    public string Kollektortyp { get; set; } = "";

    /// <summary>Modulfläche [m²].</summary>
    public double? Modulflaeche { get; set; }

    /// <summary>Aperturfläche [m²].</summary>
    public double? Aperturflaeche { get; set; }

    /// <summary>Optischer Wirkungsgrad h0 (Konversionsfaktor).</summary>
    public double? H0 { get; set; }

    /// <summary>Wärmeverlustbeiwert k1 [W/(m²·K)].</summary>
    public double? K1 { get; set; }

    /// <summary>Temperaturabhängiger Wärmeverlustbeiwert k2 [W/(m²·K²)].</summary>
    public double? K2 { get; set; }

    /// <summary>Einfallswinkelkorrektur direkte Strahlung (Kdir).</summary>
    public double? Kdir { get; set; }

    /// <summary>Einfallswinkelkorrektur diffuse Strahlung bei 50° (Kdiff).</summary>
    public double? Kdiff { get; set; }

    /// <summary>Investitionskosten [€].</summary>
    public double? Kosten { get; set; }

    /// <summary>Vorlauftemperatur [°C]; leer erlaubt und beim Speichern 0.</summary>
    public int? Vorlauf { get; set; }

    /// <summary>Rücklauftemperatur [°C]; leer erlaubt und beim Speichern 0.</summary>
    public int? Ruecklauf { get; set; }
}
