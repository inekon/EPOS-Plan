using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Die Beschriftungen des Dialogs „Wirtschaftlichkeits-Parameter" — einmal
/// aufgelöst, nicht bei jedem Zeichnen (Bauweise wie
/// <see cref="BhkwWirtschaftlichkeitTexte"/>).
///
/// <para>Die Maske trug ihre Bestandszeilen noch als deutsche Literale (offener
/// Punkt 11 des Umsetzungsstands); sie bekommen mit dem Port das Präfix
/// <c>WPAR_*</c> (Sammelnachtrag iU9-W2.6). Wo bereits Schlüssel bestanden —
/// <c>WIRT_DLG_CO2*</c>, <c>BILANZ_DLG_*</c>, <c>BHW_PARAM_*</c> — bleiben sie
/// unverändert.</para>
/// </summary>
public sealed class WirtschaftlichkeitParameterTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    // ------------------------------------------------------------ Rahmen
    public string Titel { get; } = T("WPAR_TITEL", "Wirtschaftlichkeits-Parameter");
    public string BtnSpeichern { get; } = T("WPAR_BTN_SPEICHERN", "Speichern");
    public string BtnAbbrechen { get; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    // --------------------------------------------------------- Allgemein
    public string GAllgemein { get; } = T("WPAR_G_ALLGEMEIN", "Allgemein");
    public string Zins { get; } = T("WPAR_ZINS", "Kalkulationszinssatz i [%]:");
    public string Jahre { get; } = T("WPAR_JAHRE", "Betrachtungszeitraum T [a]:");
    public string PreisEnergie { get; } = T("WPAR_PREIS_E", "Preissteigerung Energie [%/a]:");
    public string PreisBetrieb { get; } = T("WPAR_PREIS_B", "Preissteigerung Betrieb [%/a]:");

    // -------------------------------------------------------------- Strom
    public string GStrom { get; } = T("WPAR_G_STROM", "Strom — Einspeisung und Bezug");
    public string EinspeisungPv { get; } = T("WPAR_EINSP_PV", "Einspeisevergütung PV [€/kWh]:");
    public string EinspeisungKwk { get; } = T("WPAR_EINSP_KWK",
        "Einspeisevergütung KWK-Strom [€/kWh]:");
    public string Aufschlaege { get; } = T("WPAR_AUFSCHLAEGE",
        "Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession, Vertrieb) " +
        "berücksichtigen — Pflege im Energieträgerdialog (Strom)");

    // --------------------------------------------------------------- BHKW
    public string GBhkw { get; } = T("BHW_PARAM_GRUPPE", "BHKW — KWKG, Energie- und Stromsteuer");
    public string BhkwVerweis { get; } = T("BHW_PARAM_VERWEIS",
        "Diese Angaben stehen seit Etappe B5 im eigenen Dialog „BHKW-Wirtschaftlichkeit“ — " +
        "dort zusammen mit den Werten je BHKW-Modul, den Herleitungen und der Vorschau.");
    public string BhkwKnopf { get; } = T("BHW_PARAM_KNOPF",
        "⚙ BHKW-Wirtschaftlichkeit (KWKG, Steuern, Module)…");
    public string SprungHinweis { get; } = T("WPAR_SPRUNG_HINWEIS",
        "Der Sprung schließt diesen Dialog und öffnet ihn danach wieder — " +
        "bitte vorher speichern.");

    // -------------------------------------------------------- Brennstoff
    public string GBrennstoff { get; } = T("WPAR_G_BRENNSTOFF",
        "Brennstoff — BEHG und Emissionsbilanz (BHKW/Kessel)");
    public string Co2 { get; } = T("WIRT_DLG_CO2", "CO₂-Preis [€/t] (0 = Pfad aus dem Katalog):");
    public string Co2Konstant { get; } = T("WIRT_DLG_CO2_KONSTANT_ZEILE",
        "Konstanter Preis {0} €/t — der Katalogpfad wird nicht angewendet.");
    public string Co2Pfad { get; } = T("WIRT_DLG_CO2_PFAD_ZEILE",
        "Pfad aus dem Gesetzeskatalog; Prognose ab {0}.");
    public string Co2Katalog { get; } = T("WIRT_DLG_CO2_KATALOG",
        "⚙ Gesetzliche Parameter (CO₂-Preispfad)…");
    public string Park { get; } = T("WPAR_PARK", "Referenz-Kraftwerkspark:");

    // ---------------------------------------------------- Bilanzierung
    public string GBilanz { get; } = T("BILANZ_DLG_GRUPPE", "Bilanzierung");
    public string BilanzJahr { get; } = T("BILANZ_DLG_JAHR", "Bilanzjahr (0 = nicht gepflegt):");
    public string Methode { get; } = T("BILANZ_DLG_METHODE", "Emissionsmethode:");
    public string MethodeKatalog { get; } = T("BILANZ_DLG_METHODE_KATALOG", "Katalog");
    public string MethodeGutschrift { get; } = T("BILANZ_DLG_METHODE_GUTSCHRIFT", "Stromgutschrift");
    public string MethodeOhne { get; } = T("BILANZ_DLG_METHODE_OHNE", "ohne Gutschrift");
    public string MethodeSubstitution { get; } = T("BILANZ_DLG_METHODE_SUBSTITUTION", "Substitution");
    public string Biomasse { get; } = T("BILANZ_DLG_BIOMASSE", "Biomasse-Konvention:");
    public string BiomasseNull { get; } = T("BILANZ_DLG_BIOMASSE_NULL", "Nullansatz");
    public string BiomasseVerbrennung { get; } = T("BILANZ_DLG_BIOMASSE_VERBRENNUNG", "Verbrennung");
    public string Nachweis { get; } = T("BILANZ_DLG_NACHWEIS", "Nachhaltigkeitsnachweis vorhanden");

    // ------------------------------------------------------------ Hinweis
    public string Hinweis { get; } = T("WPAR_HINWEIS",
        "Die Parameter gelten für Stamm und alle Varianten der Vergleichsgruppe; " +
        "Erzeuger-Gruppen erscheinen nur, wenn der Erzeugertyp in der Gruppe " +
        "vorkommt (ausgeblendete Werte bleiben erhalten). Energie- und Strompreise " +
        "kommen aus der Kostenmaske." +
        " Aufschläge: Vorgabe AUS — eingeschaltet steigen die Energiekosten " +
        "typischerweise um rund ein Drittel (Vorschlagswerte in Summe " +
        "11,746 ct/kWh). Gepflegt werden sie je Energieträger in der Kostenmaske; " +
        "OB die Wirtschaftlichkeit sie ansetzt, wird im Energieträgerdialog " +
        "(Strom) entschieden — der Haken hier zeigt die Wahl nur an.");
    public string HinweisKwkg { get; } = T("WPAR_HINWEIS_KWKG",
        "KWKG: Deckel-Override 0 = degressive Vbh-Staffel 2025 ab dem " +
        "Inbetriebnahmejahr; förderfähig nur mit Stichtag bis 31.12.2026 " +
        "+ Realisierung bis Ablauf des 4. Folgejahres." +
        " Steuern: Ohne ausdrückliche Angabe entsteht KEINE Gutschrift — " +
        "§ 53 und § 53a schließen einander aus, die Sätze und Grenzwerte " +
        "kommen aus dem Katalog „Gesetzliche Parameter“. Der Jahresnutzungsgrad " +
        "wird nur für § 53a gebraucht (Schwelle 70 %).");
    public string HinweisKwkgKatalog { get; } = T("WIRT_DLG_KWKG_HINWEIS", "");
    public string HinweisSteuerformulare { get; } = T("WIRT_DLG_STEUER_FORMULARE", "");
    public string HinweisBilanz { get; } = T("BILANZ_DLG_HINWEIS", "");

    // ---------------------------------------------------------- Meldungen
    public string MsgSpeicherfehler { get; } = T("WPAR_MSG_SPEICHERFEHLER",
        "Die Parameter konnten nicht gespeichert werden.");
}

/// <summary>
/// Die drei Auswahllisten der Emissionsgruppe — Steuerwerte aus <c>DbWerte</c>,
/// Anzeigetexte aus <see cref="WirtschaftlichkeitParameterTexte"/>. Reihenfolge
/// und Rückfall („unbekannter Bestandswert fällt auf den ersten Eintrag")
/// wortgleich aus <c>Form_WirtschaftlichkeitParameter.AuswahlZeile</c>.
/// </summary>
public static class WirtParameterWahlen
{
    /// <summary>Emissionsmethode (L12).</summary>
    public static IReadOnlyList<Steuerwahl> Methode(WirtschaftlichkeitParameterTexte t) => new[]
    {
        new Steuerwahl(0, DbWerte.EMISSIONSMETHODE_KATALOG,         t.MethodeKatalog),
        new Steuerwahl(1, DbWerte.EMISSIONSMETHODE_STROMGUTSCHRIFT, t.MethodeGutschrift),
        new Steuerwahl(2, DbWerte.EMISSIONSMETHODE_OHNE_GUTSCHRIFT, t.MethodeOhne),
        new Steuerwahl(3, DbWerte.EMISSIONSMETHODE_SUBSTITUTION,    t.MethodeSubstitution)
    };

    /// <summary>Biomasse-Konvention (L13).</summary>
    public static IReadOnlyList<Steuerwahl> Biomasse(WirtschaftlichkeitParameterTexte t) => new[]
    {
        new Steuerwahl(0, DbWerte.BIOMASSE_KONVENTION_NULL,        t.BiomasseNull),
        new Steuerwahl(1, DbWerte.BIOMASSE_KONVENTION_VERBRENNUNG, t.BiomasseVerbrennung)
    };
}

/// <summary>
/// Wohin der Anwender aus dem Parameterdialog springen wollte.
///
/// <para>Zwei Ziele, zwei Wege: Der Gesetzeskatalog ist eine WinForms-Maske und
/// wird über die <c>Sprungbruecke</c> (iU9-W2.2) MODAL über dem Dialog gezeigt —
/// dieser Sprung erscheint deshalb nicht in dieser Aufzählung. Der Dialog
/// „BHKW-Wirtschaftlichkeit" dagegen ist selbst eine Blazor-Hülle; er bleibt
/// nachgelagert (Risiko R2), und dafür ist der Wunsch hier.</para>
/// </summary>
public enum WirtParameterSprung
{
    /// <summary>Kein Sprung — der Dialog wurde einfach geschlossen.</summary>
    Keiner,

    /// <summary>Der Sammeldialog „BHKW-Wirtschaftlichkeit" (Etappe B5).</summary>
    BhkwWirtschaftlichkeit
}

/// <summary>Was der Parameterdialog beim Schließen meldet.</summary>
/// <param name="Gespeichert">true, wenn geschrieben wurde — dann rechnet die
/// Wirtschaftlichkeitsseite neu (Bestandsverhalten von
/// <c>Form_WirtschaftlichkeitParameter.Gespeichert</c>).</param>
/// <param name="Sprung">Das gewünschte Folgefenster.</param>
public sealed record WirtParameterErgebnis(bool Gespeichert, WirtParameterSprung Sprung);
