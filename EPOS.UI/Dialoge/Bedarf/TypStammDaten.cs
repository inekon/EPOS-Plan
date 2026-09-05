using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Der Feldsatz eines Bedarfs-Stammkopfes (iU9-W8.1) — das plattformfreie Abbild eines
/// Satzes aus <c>Tab_Stromverbraucher_STAMM</c>, <c>Tab_Prozesswaerme_STAMM</c> bzw.
/// <c>Tab_Brauchwasser_STAMM</c>.
///
/// <para><b>Die zwölf Monatswerte sind <c>double?</c></b>, weil ein leeres Feld etwas
/// anderes ist als eine 0: Alle drei Vorläufer prüfen sie mit
/// <c>Program.ZahlPruefen(…, leerErlaubt: false)</c> — leer ist unzulässig und meldet den
/// Monatsnamen. Erst wenn alle zwölf stehen, wird geschrieben.</para>
///
/// <para><b>Veränderlich, nicht als <c>record</c></b> — der Dialog schreibt beim Tippen
/// hinein, wie in Welle 6 und 7 (<c>HeizkesselKatalogDaten</c>).</para>
/// </summary>
public sealed class TypStammDaten
{
    /// <summary>Welches der drei Blätter. Sie bestimmt Beschriftungen UND Zieltabelle.</summary>
    public BedarfsArt Art { get; set; } = BedarfsArt.Stromverbraucher;

    /// <summary>Der Bezeichner des Satzes; im Dialog nur lesbar (Feld <c>gesperrt</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Typ aus dem Typkatalog (Spalte <c>Typ</c> des Kopfsatzes).</summary>
    public string Typ { get; set; } = "";

    /// <summary>Freitext (Spalte <c>Beschreibung</c>).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Die zwölf Monatswerte; <c>null</c> = das Feld ist leer.</summary>
    public double?[] Monat { get; set; } = new double?[12];

    /// <summary>Die zwölf Werte als <c>double[]</c> für den Speicherweg (leer zählt als 0).</summary>
    public double[] MonatWerte()
    {
        var w = new double[12];
        for (int m = 0; m < 12 && m < Monat.Length; m++) w[m] = Monat[m] ?? 0.0;
        return w;
    }
}
