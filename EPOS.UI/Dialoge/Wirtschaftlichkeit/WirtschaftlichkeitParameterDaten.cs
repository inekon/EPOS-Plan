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

    // ETAPPE W5-B-12 (Anwenderentscheid 09.09.2026): p_I steht NEBEN p_B, weil es
    // sein Rueckfall ist - ein leeres Feld heisst "wie Betrieb", nicht "0 %/a".
    // Die Herleitungszeile darunter nennt deshalb immer den WIRKSAMEN Wert und
    // seine Herkunft; sonst saehe niemand, womit gerechnet wird.
    public string PreisInvest { get; } = T("WPAR_PREIS_I",
        "Preissteigerung Investition/Ersatz p_I [%/a] (leer = wie Betrieb):");
    public string PreisInvestZeile { get; } = T("WPAR_PREIS_I_ZEILE",
        "p_I wirksam: {0} %/a ({1}). Der Satz indiziert die Ersatzbeschaffungen und die " +
        "Preisbasis des Restwerts (VDI 2067 Blatt 1). Ein leeres Feld heißt „wie Betrieb“, " +
        "nicht „0 %/a“.");
    public string PreisInvestWieB { get; } = T("WPAR_PREIS_I_WIE_B", "wie Betrieb");
    public string PreisInvestGepflegt { get; } = T("WPAR_PREIS_I_GEPFLEGT", "gepflegt");

    // ---------------------------------------------------------- Szenarien
    // ETAPPE W5-B-9 (Anwenderentscheid 09.09.2026): der Abschnitt "Szenarien" -
    // drei Spalten Erwartet | Best | Worst ueber sechs Groessen. Die
    // Erwartet-Spalte ist ANZEIGE: Sie wiederholt, was oben unter "Allgemein"
    // gepflegt wird ("Kein Delegat ist kein Knopf").
    public string GSzenarien { get; } = T("WPAR_G_SZENARIEN",
        "Szenarien — Best und Worst gegen den Erwartungsfall");
    public string SzGroesse { get; } = T("WPAR_SZ_SPALTE_GROESSE", "Größe");
    public string SzErwartet { get; } = T("WPAR_SZ_SPALTE_ERWARTET", "Erwartet");
    public string SzBest { get; } = T("WPAR_SZ_SPALTE_BEST", "Best");
    public string SzWorst { get; } = T("WPAR_SZ_SPALTE_WORST", "Worst");
    public string SzZins { get; } = T("WPAR_SZ_ZINS", "Kalkulationszins");
    public string SzPreisE { get; } = T("WPAR_SZ_PREIS_E", "Preissteigerung Energie");
    public string SzPreisB { get; } = T("WPAR_SZ_PREIS_B", "Preissteigerung Betrieb");
    public string SzInvest { get; } = T("WPAR_SZ_INVEST", "Investition");
    public string SzErtrag { get; } = T("WPAR_SZ_ERTRAG", "Erträge");
    public string SzDauer { get; } = T("WPAR_SZ_DAUER", "Nutzungsdauer");
    /// <summary>ETAPPE W5-B-12: die siebte Zeile der Szenariotabelle.</summary>
    public string SzPreisI { get; } = T("WPAR_SZ_PREIS_I", "Preissteigerung Investition");
    public string SzVorgaben { get; } = T("WPAR_SZ_VORGABEN", "Vorgaben");
    public string SzHinweis { get; } = T("WPAR_SZ_HINWEIS", "");
    public string SzHerkunftVorgabe { get; } = T("WPAR_SZ_HERKUNFT_VORGABE",
        "{0}: Vorgaben — {1}");
    public string SzHerkunftGepflegt { get; } = T("WPAR_SZ_HERKUNFT_GEPFLEGT",
        "{0}: gepflegte Werte — {1}");

    // -------------------------------------------- Bewertung (DIN EN 17463)
    // ETAPPE W5-B-12 (VALERI-Luecke G6): das Freitextfeld "Nicht monetaere
    // Wirkungen". Es steht in einem EIGENEN Abschnitt und nicht unter
    // "Allgemein": Dort stehen Rechengroessen, hier steht eine Beschreibung -
    // und die Norm verlangt sie ausdruecklich neben der Zahl.
    public string GBewertung { get; } = T("WPAR_G_BEWERTUNG", "Bewertung nach DIN EN 17463");
    public string NichtMonetaer { get; } = T("WPAR_NICHT_MONETAER", "Nicht monetäre Wirkungen:");
    public string NichtMonetaerPlatz { get; } = T("WPAR_NICHT_MONETAER_PLATZ",
        "z. B. Versorgungssicherheit, Arbeitssicherheit, Komfort, Außenwirkung, " +
        "Erfüllung einer Auflage");
    public string NichtMonetaerHinweis { get; } = T("WPAR_NICHT_MONETAER_HINWEIS",
        "DIN EN 17463 verlangt zu jeder Bewertung eine Beschreibung dessen, was sich " +
        "nicht in Euro fassen lässt. Der Text gehört zur Maßnahme als Ganzes und steht " +
        "deshalb am Projekt, nicht je Variante; er erscheint im Bericht und auf der Seite " +
        "unter „Nicht monetäre Wirkungen“. Bleibt er leer, entfällt die Zeile — eine " +
        "leere Überschrift wäre keine Aussage.");

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
