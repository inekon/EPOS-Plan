using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Ä18 (Nutzerauftrag 26.08.2026): Komponentensicht des Tarifdialogs. Es gilt
/// weiterhin EIN Tarifsatz je Stamm (<c>Tab_ProjektTarif</c>) — die Sicht
/// bestimmt nur, welche Blöcke der Dialog baut. Geteilte Felder (Kopf,
/// Einspeisung, Bezugs-Referenzrolle) erscheinen in beiden Sichten und meinen
/// dieselben Werte; nicht gebaute Felder behält der Speichervorgang unverändert bei.
///
/// <para><b>Zwei Sichten</b> (Entscheid Q11, Anwender 22.09.2026: „kein HT/NT"; der
/// Rest nach Empfehlung). Die Sicht <c>Strombezug</c> — Zonen-Bezugspreise,
/// Leistungspreis-Staffel und Bezugsrolle, geöffnet über „Strombezug…" — und die
/// Sicht <c>Komplett</c> ohne Wirt sind entfallen: Den Zeitzonentarif gibt es nicht
/// mehr, die Staffel pflegt der Stromträger in der Kostenverwaltung, und die
/// Bezugsrolle steht in der BHKW-Sicht. Was bleibt, ist das Rollenmodell.</para>
/// </summary>
public enum TarifSicht
{
    /// <summary>BHKW: das Rollenmodell (Differenzmethode) — Bezugstarif ohne Anlage
    /// als Referenz, Reststromtarif mit Anlage und Einspeisung. Die Vorgabe.</summary>
    Bhkw,

    /// <summary>Photovoltaik: der Einspeisetarif des Rollenmodells.</summary>
    Photovoltaik
}

/// <summary>
/// Die Beschriftungen des Tarifdialogs — einmal aufgelöst, nicht bei jedem
/// Zeichnen. Gleiche Bauweise und gleicher Grund wie
/// <see cref="BhkwWirtschaftlichkeitTexte"/>: Razor kann einen Ausdruck mit
/// Zeichenketten nicht bequem in einem Attributwert tragen.
///
/// <para>Schlüssel <c>TARIF_*</c> (Sammelnachtrag iU9-W2.6), deutscher Rückfall
/// wortgleich aus <c>Form_Tarifstruktur.InitializeComponent</c>. Die Texte des
/// Zonenmodells, der Staffel, der Modellwahl und der entfallenen Sichten sind mit
/// Q11 gestrichen.</para>
/// </summary>
public sealed class TarifstrukturTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    // ------------------------------------------------------------ Rahmen
    public string TitelBhkw { get; } = T("TARIF_TITEL_BHKW", "Tarifstruktur BHKW (Strom)");
    public string TitelPv { get; } = T("TARIF_TITEL_PV", "Tarifstruktur PV-Einspeisung");
    // W-E2 (Mockup-Prüfung 04): Speichern ist ein Hausknopf, derselbe Schlüssel wie
    // bei Abbrechen darunter — kein eigener, gleichlautender Schlüssel je Dialog.
    public string BtnSpeichern { get; } = T("ADM_BTN_SPEICHERN", "Speichern");
    public string BtnAbbrechen { get; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    // -------------------------------------------------------------- Kopf
    public string Aktiv { get; } = T("TARIF_AKTIV",
        "Tarifstruktur aktiv (ersetzt die Flat-Strompreise der Kostenmaske)");
    public string GueltigAb { get; } = T("TARIF_GUELTIG_AB", "Preisstand (gültig ab):");
    public string SichtHinweis { get; } = T("TARIF_SICHT_HINWEIS",
        "Komponentensicht: Es gilt EIN Tarifsatz je Stamm. Kopfdaten und geteilte " +
        "Preisfelder erscheinen in mehreren Sichten und meinen dieselben Werte.");

    // ------------------------------------------------------- Winterspanne
    public string GZeitzonen { get; } = T("TARIF_G_ZEITZONEN",
        "Winterspanne (Sommer- und Wintermaximum des Leistungspreismodells „Staffel“)");
    public string WinterVon { get; } = T("TARIF_WINTER_VON", "Winter von Monat:");
    public string WinterBis { get; } = T("TARIF_WINTER_BIS", "Winter bis Monat:");

    // ------------------------------------------------------- Rollenmodell
    public string GRollen { get; } = T("TARIF_G_ROLLEN",
        "Rollenmodell (Etappe E5) — Differenzmethode „vermiedene Kosten“");
    public string RolleBezug { get; } = T("TARIF_ROLLE_BEZUG", "Bezugstarif OHNE BHKW (Referenz)");
    public string RolleReststrom { get; } = T("TARIF_ROLLE_REST",
        "Reststromtarif MIT BHKW (kleinere Abnahme, meist teurer)");
    public string Arbeitspreis { get; } = T("TARIF_ARBEITSPREIS",
        "Arbeitspreis (Durchschnitt) [€/kWh]:");
    public string Grundpreis { get; } = T("TARIF_GRUNDPREIS", "Grundpreis [€/a]:");
    public string Leistungsmodell { get; } = T("TARIF_LEISTUNGSMODELL", "Leistungspreismodell:");
    public string ModellMonatlich { get; } = T("TARIF_LM_MONATLICH",
        "monatlich (Σ zwölf Monatsmaxima × €/kW·Monat)");
    public string ModellStaffel { get; } = T("TARIF_LM_STAFFEL",
        "Staffel (Sommer- und Wintermaximum getrennt)");
    public string ModellJahr { get; } = T("TARIF_LM_JAHR",
        "Jahreshöchstlast (Staffel mit Winterpreisen)");
    public string Monatspreis { get; } = T("TARIF_MONATSPREIS",
        "Monatlicher Leistungspreis [€/kW·Monat]:");
    public string SpObergrenze { get; } = T("TARIF_SP_OBERGRENZE", "Obergrenze [kW]");
    public string SpSommer { get; } = T("TARIF_SP_SOMMER", "Sommer [€/kW·a]");
    public string SpWinter { get; } = T("TARIF_SP_WINTER", "Winter [€/kW·a]");
    public string Stufe { get; } = T("TARIF_STUFE", "Stufe {0}");
    public string StufeRest { get; } = T("TARIF_STUFE_REST", "Stufe {0} (Rest)");
    public string GEinspeisung { get; } = T("TARIF_G_EINSPEISUNG",
        "Einspeisung (kein Leistungspreis — Befund 11 der Altanwendung; " +
        "geteiltes Feld für PV- und KWK-Einspeisung)");
    public string Einspeisepreis { get; } = T("TARIF_EINSPEISEPREIS", "Einspeisepreis [€/kWh]:");
    public string StaffelHinweis { get; } = T("TARIF_STAFFEL_HINWEIS",
        "Die Staffelgrenzen sind KUMULIERTE Obergrenzen: „500 / 2.000 / 8.000 kW“ heißt " +
        "bis 500 kW Stufe 1, 500–2.000 kW Stufe 2, 2.000–8.000 kW Stufe 3, darüber Stufe 4. " +
        "Eine Obergrenze von 0 bedeutet „nach oben offen“ und beendet die Staffel. " +
        "Der Altkatalog speichert an dieser Stelle Stufen-BREITEN — alte Zahlenreihen sind " +
        "vor der Übernahme umzurechnen.");

    // ---------------------------------------------------------- Meldungen
    public string MsgOhneArbeitspreis { get; } = T("TARIF_MSG_OHNE_ARBEIT",
        "Das Rollenmodell ist aktiv, aber weder für den Bezug noch für den " +
        "Reststrom ist ein Arbeitspreis gepflegt — die Berechnung fällt dann auf die " +
        "Flat-Preise der Kostenmaske zurück.");
    public string MsgSpeicherfehler { get; } = T("TARIF_MSG_SPEICHERFEHLER",
        "Die Tarifstruktur konnte nicht gespeichert werden.");

    /// <summary>Der Fenstertitel zur Sicht (Ä18).</summary>
    public string Titel(TarifSicht sicht) => sicht switch
    {
        TarifSicht.Photovoltaik => TitelPv,
        _ => TitelBhkw
    };
}

/// <summary>
/// Die Auswahlliste des Tarifdialogs — Steuerwerte aus <c>DbWerte</c>,
/// Anzeigetexte aus <see cref="TarifstrukturTexte"/> (Drei-Schichten-Regel) — und
/// die Umrechnung Nummer ↔ Steuerwert, die auch der PV- und der Parameterdialog
/// nehmen. Die Wahl des Tarifmodells ist mit Q11 entfallen: Es gibt nur noch das
/// Rollenmodell.
/// </summary>
public static class TarifWahlen
{
    /// <summary>Leistungspreismodell einer Bezugsrolle.</summary>
    public static IReadOnlyList<Steuerwahl> Leistungsmodell(TarifstrukturTexte t) => new[]
    {
        new Steuerwahl(0, DbWerte.LEISTUNGSMODELL_MONATLICH,         t.ModellMonatlich),
        new Steuerwahl(1, DbWerte.LEISTUNGSMODELL_STAFFEL,           t.ModellStaffel),
        new Steuerwahl(2, DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST, t.ModellJahr)
    };

    /// <summary>Die Nummer zum Steuerwert; unbekannt = 0 (erster Eintrag),
    /// dieselbe Rückfallregel wie <c>Form_Tarifstruktur.AuswahlZeile</c>.</summary>
    public static int NummerZu(IReadOnlyList<Steuerwahl> liste, string? wert)
    {
        foreach (Steuerwahl w in liste)
            if (string.Equals(w.Wert, wert ?? "", StringComparison.Ordinal)) return w.Nummer;
        return 0;
    }

    /// <summary>Der Steuerwert zu einer Nummer; unbekannt = der erste Eintrag.</summary>
    public static string WertZu(IReadOnlyList<Steuerwahl> liste, int? nummer)
    {
        if (nummer.HasValue)
            foreach (Steuerwahl w in liste)
                if (w.Nummer == nummer.Value) return w.Wert;
        return liste.Count > 0 ? liste[0].Wert : "";
    }

    /// <summary>Die Einträge eines <see cref="EPOS.UI.Standards.Auswahlfeld"/>.</summary>
    public static IReadOnlyList<(int Id, string Text)> Eintraege(IReadOnlyList<Steuerwahl> liste)
    {
        var l = new List<(int, string)>(liste.Count);
        foreach (Steuerwahl w in liste) l.Add((w.Nummer, w.Text));
        return l;
    }
}
