using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die Beispieldaten der Wiki-Bilder zur Kostenbasis (Komponente <c>WikibildKosten</c>):
/// Energieträger, Brennstoffe, Kostenverwaltung, Wirtschaftlichkeit, Tarifstruktur,
/// Emissionen und Gesetzesparameter.
/// </summary>
/// <remarks>
/// Regel des Wikis (Konzept Hilfesystem, Abschnitt 13 und 14.3): neutrale Namen und runde,
/// erkennbar beispielhafte Werte. Keine Emissionsfaktoren aus Normen, keine Sätze aus
/// Gesetzen - die Zahlen hier sind Beispiele, keine Fachwerte.
/// </remarks>
public static class WikibeispieleKosten
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    // ------------------------------------------------------------ Energieträger

    /// <summary>Die Trägerliste: drei Gruppen mit je einem Träger.</summary>
    public static readonly EnergietraegerDialog.EnergietraegerListe[] Traegerliste =
    {
        new(null, "Gas"),
        new(1, "Erdgas"),
        new(null, "Strom"),
        new(2, "Strom"),
        new(null, "Wasser"),
        new(3, "Wasser")
    };

    /// <summary>Der gewählte Träger: Wasser mit Arbeitspreis je m³, ohne Grundpreis und Heizwert.</summary>
    public static EnergietraegerAnsicht Wasser()
    {
        var stand = new EnergietraegerStand
        {
            TraegerZeile = "Wasser",
            GruppeZeile = "Gruppe: Wasser",
            TraegerName = "Wasser",
            Arbeitspreis = 3,
            Grundpreis = 0,
            MitHeizwert = false,
            MitBrennwert = false,
            MitLeistungspreis = false,
            MitFormel = false,
            MitGrundpreis = false,
            MitEmissionen = false,
            MitUmrechnung = false,
            MitSzenario = true,
            SzenarioArbeitBest = 2.5,
            SzenarioArbeitWorst = 4,
            EinheitArbeitspreis = "€/m³",
            EinheitGrundpreis = "€/a",
            Basiseinheit = "m³",
            Preisbasen = new[] { (0, "€/m³") },
            PreisbasisId = 0,
            GueltigAb = new DateOnly(2026, 1, 1)
        };
        return new EnergietraegerAnsicht { Stand = stand, StammName = "Wasser", StammGruppe = 3 };
    }

    // ------------------------------------------------------------ Brennstoffe

    private static Dictionary<string, double?> Werte(double hi, double hs, double co2, double nox, double pe) => new()
    {
        [ProjektBrennstoffFelder.Hi] = hi,
        [ProjektBrennstoffFelder.Hs] = hs,
        [ProjektBrennstoffFelder.Co2] = co2,
        [ProjektBrennstoffFelder.So2] = 0,
        [ProjektBrennstoffFelder.Nox] = nox,
        [ProjektBrennstoffFelder.Staub] = 0,
        [ProjektBrennstoffFelder.Pe] = pe,
    };

    /// <summary>Zwei Brennstoffe des Projekts; der zweite weicht beim CO₂ vom Katalog ab.</summary>
    public static ProjektBrennstoffeStand Brennstoffe() => new(
        "Beispielprojekt",
        new[]
        {
            new ProjektBrennstoffZeile(1, "Brennstoff A", "m³", Werte(10, 11, 200, 100, 1.2),
                                       Werte(10, 11, 200, 100, 1.2), true, Array.Empty<string>()),
            new ProjektBrennstoffZeile(2, "Brennstoff B", "L", Werte(10, 11, 250, 150, 1.2),
                                       Werte(10, 11, 300, 150, 1.2), true, new[] { ProjektBrennstoffFelder.Co2 }),
        },
        new[] { new ProjektBrennstoffKatalogsatz(3, "Brennstoff C") });

    // ------------------------------------------------------------ Kostenverwaltung

    /// <summary>Die Komponenten der Klappliste.</summary>
    public static readonly (int Id, string Text)[] Komponenten =
    {
        (0, "Wärmepumpe"), (1, "Heizkessel"), (2, "Pufferspeicher")
    };

    private static KostenPositionZeile Position(int id, string name, double betrag, double? dauer) => new()
    {
        Id = id,
        Bezeichnung = name,
        BemessungId = 0,
        Satz = betrag,
        Nutzungsdauer = dauer,
        BetragText = betrag.ToString("#,##0.##", De),
        Kette = true,
        Einheit = "€",
        Schreibbar = true
    };

    /// <summary>Die Investitionskosten der Wärmepumpe: eine Variante, zwei Positionen.</summary>
    public static KostenKomponenteStand Kosten() => new()
    {
        Titel = "Kostenverwaltung Wärmepumpe",
        Untertitel = "Investitionskosten",
        Varianten = new[] { (5, "Standard"), (6, "Variante 1") },
        VarianteId = 6,
        VariantePflegbar = true,
        Zeilen = new[] { Position(11, "Gerät", 10000, 20), Position(12, "Montage", 2000, 20) },
        Bemessungen = new[] { (0, "fester Betrag"), (1, "% der Investition"), (2, "je kW Leistung") },
        SpalteBetrag = "Betrag netto [€]",
        MitNutzungsdauer = true,
        Summen = new[] { ("Summe Investitionskosten netto: 12.000,00 €", true) },
        PositionNeuMoeglich = true,
        VarianteLoeschbar = true
    };

    // ------------------------------------------------------------ Wirtschaftlichkeit

    /// <summary>Die Parameter der Wirtschaftlichkeit mit runden Werten.</summary>
    public static WirtschaftlichkeitParameter Wirtschaftlichkeit() => new()
    {
        IdStamm = 1,
        Zinssatz = 3,
        Betrachtungszeitraum = 20,
        PreissteigerungEnergie = 2,
        PreissteigerungBetrieb = 1.5,
        Einspeiseverguetung = 0.08,
        EinspeiseverguetungKWK = null,
        CO2Preis = 0,
        IdKraftwerkspark = 0,
        BilanzJahr = 0,
        EmissionsMethode = DbWerte.EMISSIONSMETHODE_KATALOG,
        BiomasseKonvention = DbWerte.BIOMASSE_KONVENTION_NULL,
        NachhaltigkeitsnachweisBiomasse = true
    };

    /// <summary>Eine einfache Tarifstruktur im Rollenmodell mit monatlichem Leistungspreis.</summary>
    public static TarifParameter Tarif()
    {
        var p = new TarifParameter
        {
            IdStamm = 1,
            Aktiv = true,
            Modus = DbWerte.TARIF_MODUS_ROLLEN,
            WinterVonMonat = 10,
            WinterBisMonat = 3,
            GueltigAb = new DateTime(2026, 1, 1)
        };
        p.Bezug.ArbeitspreisEurKWh = 0.25;
        p.Bezug.GrundpreisEurJahr = 100;
        p.Bezug.MonatspreisEurKWMonat = 10;
        p.Bezug.Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH;
        p.Reststrom.ArbeitspreisEurKWh = 0.30;
        p.Reststrom.GrundpreisEurJahr = 100;
        p.Reststrom.MonatspreisEurKWMonat = 10;
        p.Reststrom.Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH;
        p.Einspeisung.ArbeitspreisEurKWh = 0.08;
        return p;
    }

    // ------------------------------------------------------------ Emissionen

    /// <summary>Zwei Emissionsarten: die Pflichtart CO₂ und eine zweite Art ohne Treibhauswirkung.</summary>
    public static readonly IReadOnlyList<EmissionsartZeile> Emissionsarten = new[]
    {
        new EmissionsartZeile(1, "CO2", "Kohlendioxid", "g/kWh", 1, "1", "", true, true, false),
        new EmissionsartZeile(2, "NOx", "Stickoxide", "mg/kWh", 0, "0", "", true, false, false),
    };

    /// <summary>Die Werte einer Art: ein Wert des Trägers und eine Vorlage, beide eigene Beispielwerte.</summary>
    public static IReadOnlyList<EmissionswertZeile> Emissionswerte(int artId) => artId == 1
        ? new[]
        {
            new EmissionswertZeile(11, "Träger", "Beispielwert A", 200, "200", false, true, true, true),
            new EmissionswertZeile(12, "Vorlage", "Beispielwert B", 250, "250", false, false, false, true),
        }
        : new[]
        {
            new EmissionswertZeile(21, "Träger", "Beispielwert A", 100, "100", false, true, true, true),
        };

    // ------------------------------------------------------------ Gesetzesparameter

    /// <summary>Die Klassen der Klappliste.</summary>
    public static readonly IReadOnlyList<(string Wert, string Anzeige)> Gesetzesklassen = new[]
    {
        ("CO2_PREIS", "CO₂-Preis"), ("UMLAGE", "Umlagen")
    };

    /// <summary>Zwei Zeilen einer Klasse mit runden Beispielwerten.</summary>
    public static IReadOnlyList<Katalogfilterzeile> Gesetzeszeilen(string klasse)
    {
        (int Id, string Schluessel, int Jahr, double Wert, string Einheit, string Status)[] saetze = klasse == "CO2_PREIS"
            ? new[] { (1, "CO2_PREIS", 2027, 100.0, "€/t", "PROGNOSE"), (2, "CO2_PREIS", 2030, 150.0, "€/t", "PROGNOSE") }
            : new[] { (3, "UMLAGE_A", 2026, 1.0, "ct/kWh", "GESICHERT"), (4, "UMLAGE_B", 2026, 0.5, "ct/kWh", "GESICHERT") };
        return saetze.Select(s => (Katalogfilterzeile)new Katalogfilterzeile(s.Id, s.Schluessel)
            {
                Schluessel = s.Id.ToString(CultureInfo.InvariantCulture)
            }
            .MitText(GesetzKatalog.SpSchluessel, s.Schluessel)
            .Mit(GesetzKatalog.SpJahrVon, new Katalogwert(s.Jahr.ToString(CultureInfo.InvariantCulture), s.Jahr))
            .Mit(GesetzKatalog.SpWert, new Katalogwert(s.Wert.ToString("0.##", De), s.Wert))
            .MitText(GesetzKatalog.SpEinheit, s.Einheit)
            .MitText(GesetzKatalog.SpStatus, s.Status)
            .MitText(GesetzKatalog.SpQuelle, "Beispielwert")).ToList();
    }
}
