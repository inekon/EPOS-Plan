using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Kosten;

// =====================================================================================
// DIE ANZEIGEFORM DER „BRENNSTOFFE DES PROJEKTS" (Anwenderentscheid 03.10.2026: Projektkopie
// des Brennstoffkatalogs). Keine Fachklasse des Kerns: Die Hülle (EPOS.UI.Daten,
// ProjektBrennstoffeHuelle) übersetzt ProjektBrennstoffe des Kerns in diese Records.
// =====================================================================================

/// <summary>Die bearbeitbaren Felder einer Projektkopie, in der Folge der Spalten.</summary>
public static class ProjektBrennstoffFelder
{
    /// <summary>Unterer Heizwert.</summary>
    public const string Hi = "Hi";

    /// <summary>Oberer Heizwert.</summary>
    public const string Hs = "Hs";

    /// <summary>CO₂-Faktor.</summary>
    public const string Co2 = "CO2";

    /// <summary>SO₂-Faktor.</summary>
    public const string So2 = "SO2";

    /// <summary>NOₓ-Faktor.</summary>
    public const string Nox = "NOx";

    /// <summary>Staubfaktor.</summary>
    public const string Staub = "Staub";

    /// <summary>Primärenergiefaktor.</summary>
    public const string Pe = "PE_Faktor";

    /// <summary>Grundpreis.</summary>
    public const string Grundpreis = "Standard_Grundpreis";

    /// <summary>Arbeitspreis.</summary>
    public const string Arbeitspreis = "Standard_Arbeitspreis";

    /// <summary>Leistungspreis.</summary>
    public const string Leistungspreis = "Standard_Leistungspreis";

    /// <summary>Alle Felder in der Folge der Spalten.</summary>
    public static IReadOnlyList<string> Alle { get; } = new[]
    {
        Hi, Hs, Co2, So2, Nox, Staub, Pe, Grundpreis, Arbeitspreis, Leistungspreis
    };
}

/// <summary>Eine Zeile der Liste: die Projektkopie eines Brennstoffs.</summary>
/// <param name="IdBrennstoff">Die Brennstoffart (Schlüssel für Bearbeiten und Zurücksetzen).</param>
/// <param name="Bezeichner">Der Name.</param>
/// <param name="Einheit">Die Mengeneinheit.</param>
/// <param name="Werte">Die Werte der Projektkopie je Feld (<see cref="ProjektBrennstoffFelder"/>).</param>
/// <param name="Katalogwerte">Die Werte des Katalogs je Feld; leer ohne Katalogsatz.</param>
/// <param name="KatalogVorhanden">Steht der Brennstoff noch im Katalog?</param>
/// <param name="Abweichungen">Die Felder, in denen die Projektkopie vom Katalog abweicht.</param>
public sealed record ProjektBrennstoffZeile(int IdBrennstoff, string Bezeichner, string Einheit,
                                            IReadOnlyDictionary<string, double?> Werte,
                                            IReadOnlyDictionary<string, double?> Katalogwerte,
                                            bool KatalogVorhanden, IReadOnlyList<string> Abweichungen)
{
    /// <summary>Weicht die Projektkopie vom Katalog ab?</summary>
    public bool WeichtAb => Abweichungen.Count > 0;

    /// <summary>Der Wert eines Felds; <c>null</c> = nicht gepflegt.</summary>
    public double? Wert(string feld) => Werte.TryGetValue(feld, out double? w) ? w : null;

    /// <summary>Der Katalogwert eines Felds; <c>null</c> = nicht gepflegt oder kein Katalogsatz.</summary>
    public double? Katalogwert(string feld) => Katalogwerte.TryGetValue(feld, out double? w) ? w : null;
}

/// <summary>Ein Brennstoff des Katalogs, den das Projekt noch nicht führt.</summary>
/// <param name="IdBrennstoff">Die Brennstoffart.</param>
/// <param name="Bezeichner">Der Name.</param>
public sealed record ProjektBrennstoffKatalogsatz(int IdBrennstoff, string Bezeichner);

/// <summary>Der Stand des Dialogs: Projektname, Kopien und die noch nicht übernommenen Katalogsätze.</summary>
/// <param name="Projekt">Der Name des offenen Projekts; leer = kein Projekt offen.</param>
/// <param name="Zeilen">Die Projektkopien nach Brennstoffart.</param>
/// <param name="Katalog">Die Brennstoffe des Katalogs ohne Projektkopie.</param>
public sealed record ProjektBrennstoffeStand(string Projekt, IReadOnlyList<ProjektBrennstoffZeile> Zeilen,
                                             IReadOnlyList<ProjektBrennstoffKatalogsatz> Katalog)
{
    /// <summary>Der Stand ohne Gaben.</summary>
    public static ProjektBrennstoffeStand Leer { get; } =
        new ProjektBrennstoffeStand("", new List<ProjektBrennstoffZeile>(), new List<ProjektBrennstoffKatalogsatz>());
}

/// <summary>Die Antwort auf Speichern, Zurücksetzen oder Übernehmen.</summary>
/// <param name="Ok">Ist der Schritt gelungen?</param>
/// <param name="Meldung">Was der Anwender lesen soll.</param>
/// <param name="Stand">Der neue Stand; <c>null</c> = unverändert.</param>
public sealed record ProjektBrennstoffeAntwort(bool Ok, string Meldung, ProjektBrennstoffeStand? Stand);

/// <summary>
/// Das TEXTBÜNDEL des Dialogs — Beschriftungen, kein Zustand. Jede Eigenschaft nennt ihren
/// Ressourcenschlüssel; der deutsche Text ist der Rückfall.
/// </summary>
public sealed class ProjektBrennstoffeTexte
{
    /// <summary><c>PBRS_TITEL</c></summary>
    public string Titel { get; init; } = "Brennstoffe des Projekts";

    /// <summary><c>PBRS_EINLEITUNG</c></summary>
    public string Einleitung { get; init; } =
        "Das Projekt rechnet mit seinen eigenen Kopien der Brennstoffe: Heizwerte, Emissionsfaktoren und " +
        "Preisvorgaben. Ein Katalog-Update ändert sie nicht; „Auf Katalog zurücksetzen“ übernimmt den " +
        "heutigen Katalogstand.";

    /// <summary><c>PBRS_KEIN_PROJEKT</c></summary>
    public string KeinProjekt { get; init; } = "Kein Projekt geöffnet.";

    /// <summary><c>PBRS_LEER</c></summary>
    public string Leer { get; init; } = "Das Projekt führt noch keine Brennstoffe.";

    /// <summary><c>PBRS_SP_BRENNSTOFF</c></summary>
    public string SpalteBrennstoff { get; init; } = "Brennstoff";

    /// <summary><c>PBRS_SP_EINHEIT</c></summary>
    public string SpalteEinheit { get; init; } = "Einheit";

    /// <summary>Die Spaltenköpfe der bearbeitbaren Felder (<c>PBRS_SP_HI</c> … <c>PBRS_SP_LEISTUNGSPREIS</c>).</summary>
    public IReadOnlyDictionary<string, string> Felder { get; init; } = new Dictionary<string, string>
    {
        [ProjektBrennstoffFelder.Hi] = "Hi [kWh/Einheit]",
        [ProjektBrennstoffFelder.Hs] = "Hs [kWh/Einheit]",
        [ProjektBrennstoffFelder.Co2] = "CO₂ [g/kWh]",
        [ProjektBrennstoffFelder.So2] = "SO₂ [mg/kWh]",
        [ProjektBrennstoffFelder.Nox] = "NOₓ [mg/kWh]",
        [ProjektBrennstoffFelder.Staub] = "Staub [mg/kWh]",
        [ProjektBrennstoffFelder.Pe] = "Primärenergiefaktor",
        [ProjektBrennstoffFelder.Grundpreis] = "Grundpreis [€/a]",
        [ProjektBrennstoffFelder.Arbeitspreis] = "Arbeitspreis [€/Einheit]",
        [ProjektBrennstoffFelder.Leistungspreis] = "Leistungspreis [€/kW]",
    };

    /// <summary><c>PBRS_SP_STAND</c></summary>
    public string SpalteStand { get; init; } = "Stand";

    /// <summary><c>PBRS_SP_AKTIONEN</c></summary>
    public string SpalteAktionen { get; init; } = "Aktionen";

    /// <summary><c>PBRS_STAND_WIE_KATALOG</c></summary>
    public string WieKatalog { get; init; } = "wie Katalog";

    /// <summary><c>PBRS_STAND_ABWEICHEND</c></summary>
    public string Abweichend { get; init; } = "abweichend";

    /// <summary><c>PBRS_STAND_OHNE_KATALOG</c></summary>
    public string OhneKatalog { get; init; } = "nicht mehr im Katalog";

    /// <summary><c>PBRS_TIP_KATALOGWERT</c> — {0} = der Katalogwert.</summary>
    public string TipKatalogwert { get; init; } = "Katalogwert: {0}";

    /// <summary><c>PBRS_BEARBEITEN</c></summary>
    public string Bearbeiten { get; init; } = "Bearbeiten";

    /// <summary><c>PBRS_BEARBEITEN_TITEL</c> — {0} = der Brennstoff.</summary>
    public string BearbeitenTitel { get; init; } = "Werte von „{0}“";

    /// <summary><c>PBRS_ZURUECKSETZEN</c></summary>
    public string Zuruecksetzen { get; init; } = "Auf Katalog zurücksetzen";

    /// <summary><c>PBRS_FRAGE_ZURUECKSETZEN</c> — {0} = der Brennstoff.</summary>
    public string FrageZuruecksetzen { get; init; } = "Die Werte von „{0}“ auf den heutigen Katalogstand zurücksetzen?";

    /// <summary><c>PBRS_UEBERNEHMEN</c></summary>
    public string Uebernehmen { get; init; } = "Aus Katalog übernehmen";

    /// <summary><c>PBRS_UEBERNEHMEN_WAHL</c></summary>
    public string UebernehmenWahl { get; init; } = "Brennstoff des Katalogs";

    /// <summary><c>PBRS_UEBERNEHMEN_LEER</c></summary>
    public string UebernehmenLeer { get; init; } = "Alle Brennstoffe des Katalogs sind im Projekt.";

    /// <summary><c>ADM_BTN_SPEICHERN</c></summary>
    public string Speichern { get; init; } = "Speichern";

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; init; } = "Abbrechen";

    /// <summary><c>KABG_BTN_SCHLIESSEN</c></summary>
    public string Schliessen { get; init; } = "Schließen";

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; init; } = "Ja";

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; init; } = "Nein";
}
