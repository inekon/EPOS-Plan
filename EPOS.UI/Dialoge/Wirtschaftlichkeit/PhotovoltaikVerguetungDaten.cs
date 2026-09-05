using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Die Beschriftungen des PV-Vergütungsdialogs — einmal aufgelöst, nicht bei
/// jedem Zeichnen (gleiche Bauweise wie <see cref="BhkwWirtschaftlichkeitTexte"/>).
///
/// <para>Die 63 Schlüssel <c>PVW_*</c> stehen seit Etappe P5/P6 zweisprachig im
/// Katalog und werden hier unverändert weiterbenutzt; nur die Beschriftungen,
/// die es in WinForms nicht gab (die beiden Gruppentitel der Optionsgruppen und
/// der Sprunghinweis), sind neu und tragen das Präfix <c>PVV_*</c>
/// (Sammelnachtrag iU9-W2.6).</para>
/// </summary>
public sealed class PhotovoltaikVerguetungTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    // ------------------------------------------------------------ Rahmen
    public string Titel { get; } = T("PVW_TITEL", "PV-Vergütung (EEG)");
    public string Aktiv { get; } = T("PVW_AKTIV", "Vergütung anwenden");
    public string BtnUebernehmen { get; } = T("PVW_UEBERNEHMEN", "Übernehmen");
    public string BtnAbbrechen { get; } = T("PVW_ABBRECHEN", "Abbrechen");
    public string BtnMarktwerte { get; } = T("PVW_BTN_MARKTWERTE", "Marktwerte importieren…");
    public string BtnTarif { get; } = T("PVW_BTN_TARIF", "Einspeise-Tarif…");

    // ------------------------------------------------------------ Anlage
    public string GAnlage { get; } = T("PVW_G_ANLAGE", "Anlage");
    public string Kwp { get; } = T("PVW_KWP", "Installierte Leistung:");
    public string KwpOverride { get; } = T("PVW_KWP_OVR", "Override [kWp] (0 = keiner):");
    public string Ibn { get; } = T("PVW_IBN", "Inbetriebnahme:");
    public string GEinspeiseart { get; } = T("PVV_G_EINSPEISEART", "Einspeiseart");
    public string Ueberschuss { get; } = T("PVW_UEBERSCHUSS", "Überschusseinspeisung");
    public string Voll { get; } = T("PVW_VOLL", "Volleinspeisung");
    public string KwpWert { get; } = T("PVW_KWP_WERT", "rechnerisch {0:N1} kWp");
    public string KwpOverrideZusatz { get; } = T("PVW_KWP_OVERRIDE", " — Override {0:N1}");
    public string WarnAusschreibung { get; } = T("PVW_WARN_AUSSCHREIBUNG",
        "über 1 MW: Ausschreibung — AW-Override nötig.");
    public string WarnStromsteuer { get; } = T("PVW_WARN_STROMSTEUER",
        "über 2 MW: Stromsteuer auf Eigenverbrauch prüfen.");

    // ------------------------------------------------- Anzulegender Wert
    public string GAw { get; } = T("PVW_G_AW", "Anzulegender Wert");
    public string AwOverride { get; } = T("PVW_AW_OVR", "AW-Override [ct/kWh] (0 = Katalog):");
    public string AwOverrideZusatz { get; } = T("PVW_AW_OVERRIDE", "(Override)");
    public string EvSatz { get; } = T("PVW_EV_SATZ", "Feste EV (AW − {0:0.00}): {1:0.00} ct/kWh");

    // ------------------------------------------------------- Vermarktung
    public string GVermarktung { get; } = T("PVW_G_VERMARKTUNG", "Vermarktung");
    public string Ev { get; } = T("PVW_EV", "Feste Einspeisevergütung");
    public string Marktpraemie { get; } = T("PVW_MP", "Direktvermarktung mit Marktprämie");
    public string Ppa { get; } = T("PVW_PPA", "Sonstige Direktvermarktung / PPA");
    public string Keine { get; } = T("PVW_KEINE", "Keine Vergütung (unentgeltlich)");
    public string Dv { get; } = T("PVW_DV", "DV-Entgelt [ct/kWh]:");
    public string PpaPreis { get; } = T("PVW_PPA_PREIS", "PPA-Festpreis [ct/kWh] (0 = keiner):");
    public string PpaAufschlag { get; } = T("PVW_PPA_AUFSCHLAG", "PPA-Aufschlag auf Spot [ct/kWh]:");
    public string Hinweis21c { get; } = T("PVW_HINWEIS_21C",
        "Ohne aktive Zuordnung beim Netzbetreiber gilt < 200 kW die unentgeltliche Abnahme (§ 21c).");
    public string EvGesperrt { get; } = T("PVW_EV_GESPERRT",
        "Feste EV nur bis 100 kW (§ 21 Abs. 1 Nr. 1).");

    // ------------------------------------------------ § 51 / § 51a
    public string G51 { get; } = T("PVW_G_51", "Vergütungsausfall (§ 51 / § 51a)");
    public string Anwenden { get; } = T("PVW_ANWENDEN", "Anwenden:");
    public string IMSys { get; } = T("PVW_IMSYS", "iMSys-Einbaujahr (0 = keins):");
    public string Ausfall { get; } = T("PVW_AUSFALL", "Ausfallanteil der Einspeisearbeit [%]:");
    public string Par51a { get; } = T("PVW_51A", "§ 51a-Kompensation (Laufzeitverlängerung)");
    public string Altanlage { get; } = T("PVW_51_ALTANLAGE", "greift nicht: IBN vor 25.02.2025.");
    public string Greift { get; } = T("PVW_51_GREIFT", "greift ab der ersten negativen Viertelstunde.");
    public string GreiftImSys { get; } = T("PVW_51_IMSYS", "greift ab {0} (Folgejahr des iMSys-Einbaus).");
    public string Verschont { get; } = T("PVW_51_VERSCHONT", "greift nicht: Anlage < 100 kW ohne iMSys.");

    // ------------------------------------------------------------ Bezug
    public string GBezug { get; } = T("PVW_G_BEZUG", "Strompreis / Bezugsbewertung");
    public string BezugReihe { get; } = T("PVW_BEZUG_REIHE",
        "Netzbezug stundenscharf aus Preiszeitreihe bewerten");
    public string Stromsteuer { get; } = T("PVW_STROMSTEUER",
        "Eigenverbrauch aus Anlagen ≤ 2 MW im räumlichen Zusammenhang ist stromsteuerfrei " +
        "(§ 9 StromStG); bei Lieferung an Dritte gelten andere Regeln.");

    // ---------------------------------------------------------- Kappung
    public string GKappung { get; } = T("PVW_G_KAPPUNG", "60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2)");
    public string KappungAus { get; } = T("PVW_KAP_AUS", "abgeschaltet.");
    public string KappungAn { get; } = T("PVW_KAP_AN",
        "aktiv: Einspeisung auf 60 % der kWp begrenzt (ohne iMSys).");
    public string KappungInaktiv { get; } = T("PVW_KAP_INAKTIV",
        "greift nicht (Direktvermarktung oder iMSys vorhanden).");

    // ---------------------------------------------------------- Vorschau
    public string GVorschau { get; } = T("PVW_G_VORSCHAU", "Vorschau");
    public string Vorschau { get; } = T("PVW_VORSCHAU",
        "Einspeisung {0:N1} MWh/a · Satz Jahr 1: {1:0.00} ct/kWh · Erlös Jahr 1: {2:N0} €/a · " +
        "Vergütungsausfall {3:N0} kWh ({4:N0} €) · § 51a-Gutschrift {5:N0} € (Jahr {6})");
    public string VorschauOhneErgebnis { get; } = T("PVW_VORSCHAU_OHNE_ERGEBNIS",
        "Noch kein Simulationsergebnis — die Vorschau zeigt erst nach einem Lauf Mengen und " +
        "Erlöse; die Sätze oben gelten bereits.");

    // -------------------------------------------------- Auswahl und Meldungen
    public string Auto { get; } = T("PVW_AUTO", "Automatisch");
    public string Ja { get; } = T("PVW_JA", "Ja");
    public string Nein { get; } = T("PVW_NEIN", "Nein");
    public string IbnPflicht { get; } = T("PVW_IBN_PFLICHT", "Bitte das Inbetriebnahmedatum angeben.");
    public string ImportOk { get; } = T("PVW_IMPORT_OK", "Marktwerte übernommen: ");
    public string ImportFehler { get; } = T("PVW_IMPORT_FEHLER", "Import nicht möglich: ");
    public string MsgSpeicherfehler { get; } = T("PVV_MSG_SPEICHERFEHLER",
        "Die PV-Vergütung konnte nicht gespeichert werden.");
    public string SprungHinweis { get; } = T("PVV_SPRUNG_HINWEIS",
        "Der Sprung in den Tarifdialog schließt dieses Fenster und öffnet es danach " +
        "wieder — bitte vorher übernehmen.");
}

/// <summary>
/// Die beiden Ja/Nein/Automatisch-Listen des Dialogs (<c>cmbPar51</c>,
/// <c>cmbKappung</c>). Reihenfolge und Steuerwerte wortgleich aus
/// <c>Form_PhotovoltaikVerguetung.SchalterIndex</c>/<c>SchalterWert</c>:
/// 0 = AUTO, 1 = JA, 2 = NEIN.
/// </summary>
public static class PvSchalterwahlen
{
    /// <summary>Die drei Einträge in Anzeigereihenfolge.</summary>
    public static IReadOnlyList<Steuerwahl> Liste(PhotovoltaikVerguetungTexte t) => new[]
    {
        new Steuerwahl(0, DbWerte.PV_SCHALTER_AUTO, t.Auto),
        new Steuerwahl(1, DbWerte.PV_SCHALTER_JA,   t.Ja),
        new Steuerwahl(2, DbWerte.PV_SCHALTER_NEIN, t.Nein)
    };

    /// <summary>Steuerwert → Stellung (unbekannt = AUTO).</summary>
    public static int Index(string? wert) =>
        string.Equals(wert, DbWerte.PV_SCHALTER_JA, StringComparison.Ordinal) ? 1
        : string.Equals(wert, DbWerte.PV_SCHALTER_NEIN, StringComparison.Ordinal) ? 2
        : 0;

    /// <summary>Stellung → Steuerwert.</summary>
    public static string Wert(int? index) =>
        index == 1 ? DbWerte.PV_SCHALTER_JA
        : index == 2 ? DbWerte.PV_SCHALTER_NEIN
        : DbWerte.PV_SCHALTER_AUTO;
}

/// <summary>Das Ergebnis eines Marktwert-Imports (netztransparenz-CSV, P6).</summary>
/// <param name="Ok">true = übernommen.</param>
/// <param name="Bericht">Der Klartextbericht des Controllers.</param>
public sealed record MarktwertImport(bool Ok, string Bericht);

/// <summary>
/// Wohin der Anwender aus dem PV-Dialog springen wollte.
///
/// <para>Dasselbe Muster wie <see cref="BhkwSprung"/> und aus demselben Grund:
/// Das Ziel — der Tarifdialog in der PV-Sicht — ist seit iU9-W2.3 selbst eine
/// Blazor-Hülle, und zwei WebViews übereinander sind Risiko R2 des Wellenplans.
/// Die <c>Sprungbruecke</c> (iU9-W2.2) führt ausdrücklich nur WinForms-Masken;
/// bis Welle 4 den Baustein <c>Ueberlagerung</c> bringt, bleibt der Sprung
/// nachgelagert.</para>
/// </summary>
public enum PvSprung
{
    /// <summary>Kein Sprung — der Dialog wurde einfach geschlossen.</summary>
    Keiner,

    /// <summary>PV-Sicht der Tarifstruktur (<c>TarifSicht.Photovoltaik</c>).</summary>
    Tarif
}

/// <summary>Was der PV-Vergütungsdialog beim Schließen meldet.</summary>
/// <param name="Gespeichert">true, wenn „Übernehmen" geschrieben hat — dann
/// rechnet der Aufrufer neu (Bestandsverhalten von
/// <c>Form_PhotovoltaikVerguetung.Gespeichert</c>).</param>
/// <param name="Sprung">Das gewünschte Folgefenster.</param>
public sealed record PvVerguetungErgebnis(bool Gespeichert, PvSprung Sprung);
