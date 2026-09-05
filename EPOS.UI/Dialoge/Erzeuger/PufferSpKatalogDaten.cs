namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Feldsatz des Pufferspeicher-Katalogeditors — das plattformfreie Abbild von
/// <c>PufferSpModel</c> (iU9-W14a.2).
///
/// <para><b>Der fehlende vierte.</b> <c>HeizkesselKatalogDialog</c> (W6.1),
/// <c>BhkwKatalogDialog</c> (W6.2) und <c>SolarkollektorKatalogDialog</c> (W7.6) stehen
/// seit den Wellen 6 und 7; der Pufferspeicher war der letzte Katalogeditor in WinForms.
/// Er teilt sich mit ihnen <see cref="KatalogModus"/> und
/// <see cref="KatalogSpeicherErgebnis"/> aus <c>HeizkesselKatalogDaten.cs</c> — dieselben
/// zwei Zustände, dieselben drei Speicherwege.</para>
///
/// <para><b>Warum ein eigener Typ.</b> Eine Razor-Komponente kennt die Fachklassen des
/// Kerns nicht (<c>EPOS.UI/CLAUDE.md</c>); die Hülle bildet zwischen beiden ab. Drei
/// Felder sind <c>double?</c>/<c>int?</c>, weil ein leeres Feld etwas anderes ist als
/// eine 0 — beim Schreiben wird daraus 0, wie im Vorläufer
/// (<c>leerErlaubt: true</c> beim Volumen, <c>double.TryParse</c>-Rückfall 0.0 bei den
/// beiden anderen).</para>
/// </summary>
public sealed class PufferSpKatalogDaten
{
    /// <summary>Bezeichner. Im Modus „Bearbeiten" nur lesbar (Designer: gesperrt).</summary>
    public string Name { get; set; } = "";

    /// <summary>Hersteller (<c>Tab_Pufferspeicher_STAMM.Hersteller</c>).</summary>
    public string Firma { get; set; } = "";

    /// <summary>
    /// Index des Speichertyps in der Liste, die die Hülle mitliefert; <c>null</c> = keiner
    /// gewählt.
    /// </summary>
    /// <remarks>
    /// <b>Der Index ist der Steuerwert, nicht der Text</b> (Befund L0-1): Bis Paket 9
    /// schrieb der Vorläufer den LOKALISIERTEN Auswahltext in die Datenbank, und auf
    /// englischer Oberfläche landeten „Solar storage", „Buffer storage",
    /// „Combination storage" in der Speichertyp-Spalte. Die Abbildung Index →
    /// Persistenzwert liegt seit iU9-W14a.0d im Kern
    /// (<c>PufferSpStammCtrl.SpeichertypDbWert</c>).
    /// </remarks>
    public int? SpeichertypIndex { get; set; }

    /// <summary>Bereitschaftsverluste [kWh/24 h].</summary>
    public double? Bereitschaftsverluste { get; set; }

    /// <summary>Gesamtvolumen [l], ganzzahlig.</summary>
    public int? Gesamtvolumen { get; set; }

    /// <summary>Investitionskosten [€].</summary>
    public double? Investitionskosten { get; set; }
}
