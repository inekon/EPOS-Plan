using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Herleitungszeile der Aufheizoptimierung</b> (Entwurf KP3, Welle D2; Teilkonzept 7.6; Festlegung 16, B11)
/// — der Hüllenweg <c>AufheizHerleitungszeile</c> hinter der Naht <c>AufheizHerleitung</c> in
/// <c>SimulationErgebnisHuelle.ParameterGaben</c>, ohne Datenbank, in beiden Kulturen: bemessen mit Zielleistung
/// (Faktor 1), mit Grenze und Faktor ≠ 1 (Eingabe und Faktor), mit Verbrauchsangabe (Faktor erst im Lauf),
/// Variante (b) unerreichbar, gekoppelt, Tagesbilanz-Weg, Fehler des Eingangsbauers; ohne Bemessung keine Zeile.
/// Die Zahlen derselben Auskunft prüft <c>EPOS.Kern.Tests/AufheizAuskunftTests</c> gegen den Lauf.
/// </summary>
public class AufheizHerleitungszeileTests
{
    public static IEnumerable<object[]> Faelle()
    {
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, 34.64, 34.64, 1.0, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Hotel: t_auf,max 5 h bei -9,3 °C (kälteste Stunde) · P_auf 34,6 kW Zielleistung",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, 34.64, 34.64, 1.0, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Hotel: longest preheat time 5 h at -9.3 °C (coldest hour) · preheat power 34.6 kW target power",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 3,
                                                 -9.26, 25.3121, 100.0, 0.253121, DbWerte.AUFHEIZ_QUELLE_GRENZE),
            "Hotel: t_auf,max 3 h bei -9,3 °C (kälteste Stunde) · P_auf 25,3 kW Heizleistungsgrenze " +
            "(Eingabe 100,0 kW gilt dem Katalogbau, × Faktor 0,253)",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 3,
                                                 -9.26, 25.3121, 100.0, 0.253121, DbWerte.AUFHEIZ_QUELLE_GRENZE),
            "Hotel: longest preheat time 3 h at -9.3 °C (coldest hour) · preheat power 25.3 kW heating power limit " +
            "(input 100.0 kW applies to the catalogue building, × factor 0.253)",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Halle", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 2,
                                                 -12.0, 40.0, 80.0, 0.5, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Halle: t_auf,max 2 h bei -12,0 °C (kälteste Stunde) · P_auf 40,0 kW Zielleistung (Katalogbau 80,0 kW × Faktor 0,5)",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, null, 136.857, null, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Hotel: t_auf,max 5 h bei -9,3 °C (kälteste Stunde) · P_auf 136,9 kW Zielleistung " +
            "(am Katalogbau; den Faktor der Verbrauchsangabe bestimmt erst der Lauf)",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, null, 136.857, null, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Hotel: longest preheat time 5 h at -9.3 °C (coldest hour) · preheat power 136.9 kW target power " +
            "(catalogue building; the factor of the consumption entry is set by the run)",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG,
                                                 null, -12.26, 31.75, 31.75, 1.0, DbWerte.AUFHEIZ_QUELLE_GEMISCHT),
            "Hotel: Die Aufheizleistung reicht nicht — bei -12,3 °C (kälteste Stunde − ΔT_K) hält keine Rampe bis 48 h · " +
            "P_auf 31,8 kW Grenze und Zielleistung je Zone",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG,
                                                 null, -12.26, 31.75, 31.75, 1.0, DbWerte.AUFHEIZ_QUELLE_GEMISCHT),
            "Hotel: The preheat power is insufficient — at -12.3 °C (coldest hour − ΔT_K) no ramp of up to 48 h holds · " +
            "preheat power 31.8 kW limit and target power per zone",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("GMH", DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, null, null, null, null, null, null, null),
            "GMH: Heizkreis gekoppelt (AK1) — wird nicht optimiert",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("GMH", DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, null, null, null, null, null, null, null),
            "GMH: heating circuit coupled (AK1) — not optimised",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("EFH", null, null, null, null, null, null, null, null, Tagesbilanz: true),
            "EFH: Tagesbilanz — ohne Aufheizoptimierung",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("EFH", null, null, null, null, null, null, null, null, Tagesbilanz: true),
            "EFH: daily balance — no preheat optimisation",
        };
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("EFH", null, null, null, null, null, null, null, null, Befund: "Grund X"),
            "EFH: keine Bemessung — Grund X",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("EFH", null, null, null, null, null, null, null, null, Befund: "Grund X"),
            "EFH: no design — Grund X",
        };
        // KP3 O1b (Festlegung 35): mit Aufschlag nennt die Zeile n' neben n — hinter Reserve und Faktor; t_auf,max
        // bleibt die bemessene Zeit.
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, 40.0, 80.0, 0.5, DbWerte.AUFHEIZ_QUELLE_ZIEL,
                                                 ReserveAnteil: 0.2, ReserveVorgabe: true, RampeN: 6, RampeNAufschlag: 9),
            "Hotel: t_auf,max 5 h bei -9,3 °C (kälteste Stunde) · P_auf 40,0 kW Zielleistung · Reserve 20 % (Vorgabe) " +
            "(Katalogbau 80,0 kW × Faktor 0,5) · Aufschlag: längste Rampe n′ = 9 statt 6 Stufen",
        };
        yield return new object[]
        {
            "en-US", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                                 -9.26, 34.64, 34.64, 1.0, DbWerte.AUFHEIZ_QUELLE_ZIEL, RampeN: 6, RampeNAufschlag: 9),
            "Hotel: longest preheat time 5 h at -9.3 °C (coldest hour) · preheat power 34.6 kW target power " +
            "· surcharge: longest ramp n′ = 9 instead of 6 steps",
        };
        // Ohne Aufschlag oder ohne Wirkung (n' = n fehlt in den Angaben) bleibt die Zeile, wie sie war.
        yield return new object[]
        {
            "de-DE", new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 0,
                                                 -9.26, 34.64, 34.64, 1.0, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            "Hotel: t_auf,max 0 h bei -9,3 °C (kälteste Stunde) · P_auf 34,6 kW Zielleistung",
        };
    }

    /// <summary>Je Fall die Zeile in der Kultur — Texte aus <c>SIMKONF_AUFH_*</c>, Zahlen in der Oberflächenkultur.</summary>
    [Theory]
    [MemberData(nameof(Faelle))]
    public void Die_Zeile_in_beiden_Kulturen(string kultur, object daten, string erwartet)
    {
        // Die Angaben sind ein interner Typ der Hülle - als object übergeben, weil die Theorie öffentlich ist.
        using var k = new Kulturvorrichtung(kultur);
        Assert.Equal(erwartet, AufheizHerleitungszeile.Zeile((AufheizHerleitungsdaten)daten, CultureInfo.CurrentCulture));
    }

    /// <summary>Ohne Bemessung (Schalter aus) und ohne Angaben keine Zeile.</summary>
    [Fact]
    public void Ohne_Bemessung_keine_Zeile()
    {
        using var k = new Kulturvorrichtung("de-DE");
        Assert.Null(AufheizHerleitungszeile.Zeile(null, CultureInfo.CurrentCulture));
        Assert.Null(AufheizHerleitungszeile.Zeile(
            new AufheizHerleitungsdaten("Hotel", null, null, null, null, null, null, null, null), CultureInfo.CurrentCulture));
    }
}
