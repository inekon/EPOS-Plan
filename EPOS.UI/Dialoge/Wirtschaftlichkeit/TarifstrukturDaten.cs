using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Ä18 (Nutzerauftrag 26.08.2026): Komponentensicht des Tarifdialogs. Es gilt
/// weiterhin EIN Tarifsatz je Stamm (<c>Tab_ProjektTarif</c>) — die Sicht
/// bestimmt nur, welche Blöcke der Dialog baut. Geteilte Felder (Kopf,
/// Einspeisepreise, Bezugs-Referenzrolle) erscheinen in mehreren Sichten und
/// meinen dieselben Werte; nicht gebaute Felder behält der Speichervorgang
/// unverändert bei.
///
/// <para>Wortgleich aus der gelöschten WinForms-Fassung
/// <c>Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs</c> übernommen (iU9-W2.3);
/// sie stand dort im selben Quelltext wie die Maske und musste mit ihr
/// umziehen — <c>BhkwWirtschaftlichkeitHuelle</c> und
/// <c>UcWirtschaftlichkeit</c> benutzen sie weiter.</para>
/// </summary>
public enum TarifSicht
{
    /// <summary>Alle Blöcke (Bestandsverhalten, Rückfall).</summary>
    Komplett,

    /// <summary>Strom-EINKAUF: Zonen-Bezugspreise, Leistungspreis-Staffel und
    /// die Bezugsrolle — die Tarifseite der Wärmepumpe und aller Verbraucher.</summary>
    Strombezug,

    /// <summary>BHKW: das Rollenmodell (Differenzmethode) samt Referenzbezug
    /// und Einspeisung sowie die Zonen-Einspeisepreise (KWK-Anteil).</summary>
    Bhkw,

    /// <summary>Photovoltaik: die Einspeisepreise beider Modelle
    /// (Zonenpreise bzw. Rollen-Einspeisung).</summary>
    Photovoltaik
}

/// <summary>
/// Die Beschriftungen des Tarifdialogs — einmal aufgelöst, nicht bei jedem
/// Zeichnen. Gleiche Bauweise und gleicher Grund wie
/// <see cref="BhkwWirtschaftlichkeitTexte"/>: Razor kann einen Ausdruck mit
/// Zeichenketten nicht bequem in einem Attributwert tragen.
///
/// <para>Schlüssel <c>TARIF_*</c> (Sammelnachtrag iU9-W2.6), deutscher Rückfall
/// wortgleich aus <c>Form_Tarifstruktur.InitializeComponent</c>.</para>
/// </summary>
public sealed class TarifstrukturTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    // ------------------------------------------------------------ Rahmen
    public string TitelKomplett { get; } = T("TARIF_TITEL", "Tarifstruktur Strom");
    public string TitelStrombezug { get; } = T("TARIF_TITEL_BEZUG",
        "Tarifstruktur Strombezug (Wärmepumpe & Verbraucher)");
    public string TitelBhkw { get; } = T("TARIF_TITEL_BHKW", "Tarifstruktur BHKW (Strom)");
    public string TitelPv { get; } = T("TARIF_TITEL_PV", "Tarifstruktur PV-Einspeisung");
    public string BtnSpeichern { get; } = T("TARIF_BTN_SPEICHERN", "Speichern");
    public string BtnAbbrechen { get; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    // -------------------------------------------------------------- Kopf
    public string Aktiv { get; } = T("TARIF_AKTIV",
        "Tarifstruktur aktiv (ersetzt die Flat-Strompreise der Kostenmaske)");
    public string Modell { get; } = T("TARIF_MODELL", "Tarifmodell:");
    public string ModellZonen { get; } = T("TARIF_MODELL_ZONEN",
        "Zonenmodell (Winter/Sommer × HT/NT)");
    public string ModellRollen { get; } = T("TARIF_MODELL_ROLLEN",
        "Rollenmodell (Bezug / Reststrom / Einspeisung)");
    public string GueltigAb { get; } = T("TARIF_GUELTIG_AB", "Preisstand (gültig ab):");
    public string SichtHinweis { get; } = T("TARIF_SICHT_HINWEIS",
        "Komponentensicht: Es gilt EIN Tarifsatz je Stamm. Kopfdaten und geteilte " +
        "Preisfelder erscheinen in mehreren Sichten und meinen dieselben Werte.");

    // ---------------------------------------------------------- Zeitzonen
    public string GZeitzonen { get; } = T("TARIF_G_ZEITZONEN",
        "Zeitzonen (HT gilt Mo–Fr; Referenzjahr 2026)");
    public string WinterVon { get; } = T("TARIF_WINTER_VON", "Winter von Monat:");
    public string WinterBis { get; } = T("TARIF_WINTER_BIS", "Winter bis Monat:");
    public string HtVon { get; } = T("TARIF_HT_VON", "HT von Stunde (nur Zonenmodell):");
    public string HtBis { get; } = T("TARIF_HT_BIS", "HT bis Stunde (exklusiv):");

    // -------------------------------------------------------- Zonenmodell
    public string GZonen { get; } = T("TARIF_G_ZONEN",
        "Zonenmodell (Stufe W3) — vier Zonenpreise, zweistufige Staffel");
    public string GZonenNurEinspeisung { get; } = T("TARIF_G_ZONEN_EINSP",
        "Zonenmodell (Stufe W3) — Einspeisepreise");
    public string GBezugspreise { get; } = T("TARIF_G_BEZUGSPREISE", "Bezugspreise [€/kWh]");
    public string GEinspeisepreise { get; } = T("TARIF_G_EINSPEISEPREISE",
        "Einspeisepreise [€/kWh] (PV- und KWK-Einspeisung — geteiltes Feld)");
    public string WinterHt { get; } = T("TARIF_WINTER_HT", "Winter HT:");
    public string WinterNt { get; } = T("TARIF_WINTER_NT", "Winter NT:");
    public string SommerHt { get; } = T("TARIF_SOMMER_HT", "Sommer HT:");
    public string SommerNt { get; } = T("TARIF_SOMMER_NT", "Sommer NT:");
    public string GStaffel { get; } = T("TARIF_G_STAFFEL",
        "Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)");
    public string StaffelGrenze { get; } = T("TARIF_STAFFEL_GRENZE", "Staffelgrenze [kW]:");
    public string StaffelPreis1 { get; } = T("TARIF_STAFFEL_PREIS1", "Preis bis Grenze [€/kW·a]:");
    public string StaffelPreis2 { get; } = T("TARIF_STAFFEL_PREIS2", "Preis über Grenze [€/kW·a]:");

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
    public string SpStufe { get; } = T("TARIF_SP_STUFE", "Staffelstufe");
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
    public string MsgHtLeer { get; } = T("TARIF_MSG_HT_LEER",
        "Das HT-Fenster ist leer (von ≥ bis).");
    public string MsgOhneBezugspreis { get; } = T("TARIF_MSG_OHNE_BEZUG",
        "Die Tarifstruktur ist aktiv, aber es ist kein Bezugspreis gepflegt — " +
        "die Berechnung fällt dann auf die Flat-Preise der Kostenmaske zurück.");
    public string MsgOhneArbeitspreis { get; } = T("TARIF_MSG_OHNE_ARBEIT",
        "Das Rollenmodell ist aktiv, aber weder für den Bezug noch für den " +
        "Reststrom ist ein Arbeitspreis gepflegt — die Berechnung fällt dann auf die " +
        "Flat-Preise der Kostenmaske zurück.");
    public string MsgSpeicherfehler { get; } = T("TARIF_MSG_SPEICHERFEHLER",
        "Die Tarifstruktur konnte nicht gespeichert werden.");

    /// <summary>Der Fenstertitel zur Sicht (Ä18).</summary>
    public string Titel(TarifSicht sicht) => sicht switch
    {
        TarifSicht.Strombezug => TitelStrombezug,
        TarifSicht.Bhkw => TitelBhkw,
        TarifSicht.Photovoltaik => TitelPv,
        _ => TitelKomplett
    };
}

/// <summary>
/// Die beiden Auswahllisten des Tarifdialogs — Steuerwerte aus
/// <c>DbWerte</c>, Anzeigetexte aus <see cref="TarifstrukturTexte"/>
/// (Drei-Schichten-Regel).
/// </summary>
public static class TarifWahlen
{
    /// <summary>Tarifmodell: Zonen- oder Rollenmodell.</summary>
    public static IReadOnlyList<Steuerwahl> Modus(TarifstrukturTexte t) => new[]
    {
        new Steuerwahl(0, DbWerte.TARIF_MODUS_ZONEN,  t.ModellZonen),
        new Steuerwahl(1, DbWerte.TARIF_MODUS_ROLLEN, t.ModellRollen)
    };

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
