// DIE DATEN DER VERWALTUNG „KÄLTEMITTEL" (KM4-E3-c, Fachkonzept Kältemittel als Stammdatum 7.6).
//
// Keine Fachklasse des Kerns: Die Hülle (EPOS.UI.Daten, KaeltemittelHuelle) übersetzt die Sätze von
// KaeltemittelCtrl in diese Zeilen, samt Anzeigetexten der Wertemengen und der Verwendung, und nimmt den
// Arbeitsstand über ihre Delegaten zurück. Persistenzwerte (DbWerte.KAELTEMITTEL_*) bleiben Text; leer heißt
// „unbekannt", nie eine stille Vorgabe.

using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>Eine Zeile der Kältemitteltabelle — Liste und Arbeitsstand des Satzbereichs.</summary>
public sealed class KaeltemittelZeile
{
    /// <summary>Die ID; 0 = neue, noch nicht gespeicherte Zeile.</summary>
    public int Id { get; set; }

    /// <summary>Kurzzeichen nach ISO 817 mit Isomerangabe.</summary>
    public string Code { get; set; } = "";

    /// <summary>Stoff- oder Gemischname, neutral.</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Stoffgruppe als Persistenzwert; leer = unbekannt.</summary>
    public string Gruppe { get; set; } = "";

    /// <summary>GWP₁₀₀; <c>null</c> = unbekannt.</summary>
    public double? Gwp { get; set; }

    /// <summary>Fundstelle des GWP-Werts.</summary>
    public string GwpQuelle { get; set; } = "";

    /// <summary>Fluoriertes Treibhausgas; <c>null</c> = unbekannt.</summary>
    public bool? FGas { get; set; }

    /// <summary>Natürliches Kältemittel; <c>null</c> = unbekannt.</summary>
    public bool? Natuerlich { get; set; }

    /// <summary>Sicherheitsgruppe nach ISO 817 / EN 378-1; leer = unbekannt.</summary>
    public string Sicherheitsgruppe { get; set; } = "";

    /// <summary>PFAS-Status als Persistenzwert; leer = unbekannt.</summary>
    public string PfasStatus { get; set; } = "";

    /// <summary>Belegstand als Persistenzwert; leer wird beim Speichern „vorläufig".</summary>
    public string Status { get; set; } = "";

    /// <summary>Fundstelle der übrigen Kennzeichen.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Reihenfolge der Klappliste; <c>null</c> = nach dem Code.</summary>
    public int? Sortierung { get; set; }

    /// <summary>Auslieferungszeile (<c>ReadOnly = 1</c>) — nur lesbar, bis das Schloss aufgehoben ist.</summary>
    public bool Gesperrt { get; set; }

    /// <summary>Wie viele Kältemaschinen (Katalog und Projekte) die Zeile verwenden.</summary>
    public int Verwendung { get; set; }

    /// <summary>Anzeigetext der Gruppe (Hülle).</summary>
    public string GruppeText { get; set; } = "";

    /// <summary>Anzeigetext des PFAS-Status (Hülle).</summary>
    public string PfasText { get; set; } = "";

    /// <summary>Anzeigetext des Status (Hülle).</summary>
    public string StatusText { get; set; } = "";

    /// <summary>Eine flache Kopie — der Arbeitsstand lässt die Zeile der Liste unangetastet.</summary>
    public KaeltemittelZeile Kopie() => (KaeltemittelZeile)MemberwiseClone();
}

/// <summary>Ein Eintrag einer Wertemenge: Persistenzwert und Anzeigetext.</summary>
public sealed record KaeltemittelWahl(string Wert, string Text);

/// <summary>Der Ausgang eines Schreibwegs der Hülle: bei Erfolg die ID, sonst der Grund.</summary>
public sealed record KaeltemittelAntwort(bool Ok, string Meldung, int Id);
