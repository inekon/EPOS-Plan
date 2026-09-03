namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Welchen der drei Speicherwege der Katalogeditor gerade anbietet (iU9-W6.1).
///
/// <para>Vorbild <c>Form_Heizkessel_Bearbeiten</c>, Konstruktorparameter <c>mode</c>:
/// <c>MODE_EDIT</c> lädt einen vorhandenen Satz und schaltet „Überschreiben" und
/// „Speichern unter" frei, <c>MODE_NEU</c> nur „Speichern".</para>
/// </summary>
public enum KatalogModus
{
    /// <summary>Ein geladener Katalogsatz wird bearbeitet.</summary>
    Bearbeiten,

    /// <summary>Ein neuer Katalogsatz entsteht.</summary>
    Neu
}

/// <summary>
/// Der Feldsatz des Heizkessel-Katalogeditors — das plattformfreie Abbild von
/// <c>HeizkesselModel</c> (iU9-W6.1).
///
/// <para><b>Warum ein eigener Typ.</b> Eine Razor-Komponente kennt die Fachklassen des
/// Kerns nicht (<c>EPOS.UI/CLAUDE.md</c>); die Hülle bildet zwischen beiden ab. Der Typ
/// trägt genau die 21 Felder, die die Maske zeigt, in derselben Reihenfolge wie die
/// Feldkarte — und drei davon als <c>double?</c>/<c>int?</c>, weil ein leeres Feld etwas
/// anderes ist als eine 0. Beim Speichern wird daraus 0 (Bestandsregel
/// „leerErlaubt: true").</para>
///
/// <para><b>Veränderlich, nicht als <c>record</c>.</b> Der Dialog schreibt beim Tippen
/// hinein; ein Ergebnis-Record entstünde erst beim OK. Hier ist die Klasse selbst der
/// Arbeitsstand, und die Hülle nimmt sie beim Speichern entgegen.</para>
/// </summary>
public sealed class HeizkesselKatalogDaten
{
    /// <summary>Primärschlüssel des geladenen Katalogsatzes; 0 im Modus „Neu".</summary>
    public int KatalogId { get; set; }

    // --- Gruppe 1: Bezeichnung -------------------------------------------------

    /// <summary>Kesselbezeichnung. Im Modus „Bearbeiten" nur lesbar (Designer: gesperrt).</summary>
    public string Name { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Beschreibung (mehrzeilig).</summary>
    public string Beschreibung { get; set; } = "";

    // --- Gruppe 2: Technische Daten --------------------------------------------

    /// <summary>Thermische Leistung [kW].</summary>
    public double? Ptherm { get; set; }

    /// <summary>Wirkungsgrad Gas, Biogas, Holz und Sonstiges.</summary>
    public double? Wirkungsgrad_Gas { get; set; }

    /// <summary>Wirkungsgrad Öl.</summary>
    public double? Wirkungsgrad_Oel { get; set; }

    /// <summary>Betriebsbereitschaftsverluste [%].</summary>
    public double? Betriebsbereitschaftverlust { get; set; }

    /// <summary>
    /// Energieträger als <b>1-basierte</b> Nummer aus <c>Tab_Brennstoff_Stamm</c>.
    /// <c>null</c> = keiner gewählt; beim Speichern wird daraus 1, wie im Vorläufer
    /// (<c>SelectedIndex &gt;= 0 ? SelectedIndex + 1 : 1</c>).
    /// </summary>
    public int? Brennstoff { get; set; }

    /// <summary>Brennwertkessel?</summary>
    public bool Brennwert { get; set; }

    /// <summary>Vorlauftemperatur [°C], ganzzahlig.</summary>
    public int? Vorlauf { get; set; }

    /// <summary>Rücklauftemperatur [°C], ganzzahlig.</summary>
    public int? Ruecklauf { get; set; }

    // --- Gruppe 3: Kosten ------------------------------------------------------

    /// <summary>Investitionskosten [€].</summary>
    public double? Investitionskosten { get; set; }

    /// <summary>Raumbedarf [m³].</summary>
    public double? Raumbedarf { get; set; }

    /// <summary>Nutzungsdauer [Jahre].</summary>
    public double? Nutzungsdauer { get; set; }

    /// <summary>Wartungskosten — Betrag zur gewählten Einheit.</summary>
    public double? Wartungskosten { get; set; }

    /// <summary>
    /// Index der Wartungseinheit in der Liste, die die Hülle mitliefert. Der
    /// Persistenzwert (<c>EUR_JAHR</c> …) bleibt in der Hülle: Er ist Datenbankinhalt
    /// und gehört nicht in die Oberfläche (Drei-Schichten-Regel).
    /// </summary>
    public int WartungEinheit { get; set; }

    // --- Gruppe 4: Emissionsfaktoren -------------------------------------------

    /// <summary>CO2 [g/MWh].</summary>
    public double? CO2 { get; set; }

    /// <summary>SO2 [g/MWh].</summary>
    public double? SO2 { get; set; }

    /// <summary>NOx [g/MWh].</summary>
    public double? NOx { get; set; }

    /// <summary>CO [g/MWh].</summary>
    public double? CO { get; set; }

    /// <summary>Staub [g/MWh].</summary>
    public double? Staub { get; set; }
}

/// <summary>
/// Was ein Speicherversuch ergeben hat — das Abbild von
/// <c>HeizkesselStammCtrl.SpeicherErgebnis</c> auf der Oberflächenseite.
/// </summary>
/// <param name="Ok">Wurde geschrieben?</param>
/// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
/// <param name="Name">Der Bezeichner, unter dem der Satz jetzt steht.</param>
public sealed record KatalogSpeicherErgebnis(bool Ok, string Meldung, string Name);
