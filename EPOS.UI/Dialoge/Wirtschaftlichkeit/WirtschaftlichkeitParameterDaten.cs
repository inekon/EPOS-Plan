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
    // W-E2 (Mockup-Prüfung 04): Speichern ist ein Hausknopf, derselbe Schlüssel wie
    // bei Abbrechen darunter — kein eigener, gleichlautender Schlüssel je Dialog.
    public string BtnSpeichern { get; } = T("ADM_BTN_SPEICHERN", "Speichern");
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
    // AUFTRAG #325 (Anwenderwunsch 17.09.2026): Die vier Schluessel
    // WPAR_G_BEWERTUNG, WPAR_NICHT_MONETAER, WPAR_NICHT_MONETAER_PLATZ und
    // WPAR_NICHT_MONETAER_HINWEIS sind MITGEZOGEN auf die Seite
    // "Wirtschaftlichkeit" (Seiten/Berichte/WirtschaftlichkeitSeite.razor) - das
    // Freitextfeld steht jetzt dort, unter der Kennzahltabelle. Unbenannt und
    // unveraendert; hier fuehrt der Dialog sie nicht mehr, weil er das Feld nicht
    // mehr zeigt.

    // -------------------------------------------------------------- Strom
    public string GStrom { get; } = T("WPAR_G_STROM", "Strom — Einspeisung und Bezug");
    public string EinspeisungPv { get; } = T("WPAR_EINSP_PV", "Einspeisevergütung PV [€/kWh]:");

    // AUFTRAG #325: WPAR_EINSP_KWK ist MITGEZOGEN in den Dialog
    // "BHKW-Wirtschaftlichkeit" (BhkwWirtschaftlichkeitTexte.PEinspKwk) - der Satz
    // steht bei der Anlage, deren Strom er verguetet.

    /// <summary>
    /// SP-E-5 (a), 17.09.2026: Der PV-Satz ist die EINE Quelle der
    /// Einspeisevergütung für PV — er bewertet den eingespeisten Strom in der
    /// Wirtschaftlichkeit und stellt zugleich <c>v_pv</c> der Speicherwelt. Die
    /// Trägerkarte trägt ihn nicht mehr.
    ///
    /// <para>AUFTRAG #325: Er ist zugleich der Rückfall für <c>v_bhkw</c>, wenn kein
    /// KWK-Satz gepflegt ist, und der Verkaufserlös der Arbitrage außerhalb des
    /// Spotmarkts — deshalb bleibt er in DIESEM Dialog und wandert nicht in den
    /// PV-Vergütungsdialog, der ohne PV-Anlage im Projekt nicht erreichbar wäre.</para>
    /// </summary>
    public string EinspeisungHinweis { get; } = T("WPAR_EINSP_HINWEIS",
        "Dieser Satz gilt für die ganze Anwendung: Er bewertet den eingespeisten " +
        "PV-Strom in der Wirtschaftlichkeit UND stellt den Verkaufspreis, mit dem " +
        "Stromspeicher und Speicherflotte rechnen (v_pv) — und außerhalb des " +
        "Spotmarkts auch den Verkaufserlös der Arbitrage. Ohne gepflegten KWK-Satz " +
        "im Dialog „BHKW-Wirtschaftlichkeit“ gilt er auch für BHKW-Strom; ist gar " +
        "nichts gepflegt, rechnet die Speicherwelt mit 0 und weist das im Protokoll " +
        "aus. Führt der PV-Vergütungsdialog die Vergütung, hat er für v_pv Vorrang.");

    // --------------------------------------------------------------- BHKW
    /// <summary>
    /// Die BHKW-Gruppe dieses Dialogs ist reiner VERWEIS: Sie sagt, wo die
    /// Angaben stehen, und führt selbst nicht dorthin. Der Einstieg in den
    /// Dialog „BHKW-Wirtschaftlichkeit" ist die Fußleiste der
    /// Wirtschaftlichkeitsseite.
    /// </summary>
    public string GBhkw { get; } = T("BHW_PARAM_GRUPPE", "BHKW — KWKG, Energie- und Stromsteuer");
    public string BhkwVerweis { get; } = T("BHW_PARAM_VERWEIS",
        "Diese Angaben stehen seit Etappe B5 im eigenen Dialog „BHKW-Wirtschaftlichkeit“ — " +
        "dort zusammen mit den Werten je BHKW-Modul, den Herleitungen und der Vorschau.");

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
/// Was der Parameterdialog beim Schließen meldet.
///
/// <para>Der Dialog führt genau EINEN Unterdialog, den Gesetzeskatalog, und der
/// steht als <c>Ueberlagerung</c> in ihm. Ein Folgefenster meldet er nicht: Er
/// schließt oder er bleibt stehen.</para>
/// </summary>
/// <param name="Gespeichert">true, wenn geschrieben wurde — dann rechnet die
/// Wirtschaftlichkeitsseite neu (Bestandsverhalten von
/// <c>Form_WirtschaftlichkeitParameter.Gespeichert</c>).</param>
public sealed record WirtParameterErgebnis(bool Gespeichert);
