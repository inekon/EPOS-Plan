using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die Beispieldaten der Bildschirmfotos fürs Wiki (Seite <c>/wikibild</c>).
/// </summary>
/// <remarks>
/// Regel des Wikis (Konzept Hilfesystem, Abschnitt 13 und 14.3): Im Bild stehen keine
/// Hersteller- und Produktdaten. Deshalb tragen alle Sätze neutrale Namen („Kessel 1“,
/// „Hersteller A“, „Speicher 1, 100 kWh“) und runde Werte. Die Zeilen der Messproben
/// (<see cref="Zeilenbau"/>) taugen dafür nicht - sie tragen echte Herstellernamen.
/// </remarks>
public static class Wikibeispiele
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Eine Katalogzeile: Name und die Werte je Spaltenschlüssel.</summary>
    public sealed record Satz(string Name, params (string Spalte, object Wert)[] Werte);

    /// <summary>
    /// Baut die Katalogzeilen zu einem Profil. Jede Spalte des Profils bekommt den Wert
    /// des Satzes, sonst bleibt sie leer; der Bezeichner ist immer der Name.
    /// </summary>
    public static IReadOnlyList<Katalogfilterzeile> Zeilen(Katalogfilterprofil profil, IEnumerable<Satz> saetze,
                                                          bool schluesselIstId = false)
    {
        var liste = new List<Katalogfilterzeile>();
        var alle = saetze.ToList();
        // Eine Spalte mit einem Bruch zeigt alle ihre Zahlen mit zwei Nachkommastellen.
        var nachkomma = alle.SelectMany(x => x.Werte).Where(w => w.Wert is double)
            .GroupBy(w => w.Spalte)
            .ToDictionary(g => g.Key, g => g.Any(w => Math.Abs((double)w.Wert - Math.Round((double)w.Wert)) > 1e-9) ? 2 : 0);
        int id = 1;
        foreach (Satz s in alle)
        {
            var zeile = new Katalogfilterzeile(id, s.Name);
            if (schluesselIstId) zeile.Schluessel = id.ToString(CultureInfo.InvariantCulture);
            var werte = s.Werte.ToDictionary(w => w.Spalte, w => w.Wert);
            foreach (Katalogspalte spalte in profil.Spalten)
            {
                if (spalte.Schluessel == Katalogfilterprofil.SpBezeichner) { zeile.MitText(spalte.Schluessel, s.Name); continue; }
                if (!werte.TryGetValue(spalte.Schluessel, out object? wert))
                {
                    if (spalte.Art == Katalogspaltenart.JaNein) zeile.MitKennzeichen(spalte.Schluessel, false);
                    continue;
                }
                switch (wert)
                {
                    case bool b: zeile.MitKennzeichen(spalte.Schluessel, b); break;
                    case int i: zeile.MitZahl(spalte.Schluessel, i, 0); break;
                    case double d: zeile.MitZahl(spalte.Schluessel, d, nachkomma[spalte.Schluessel]); break;
                    default: zeile.MitText(spalte.Schluessel, wert.ToString() ?? ""); break;
                }
            }
            liste.Add(zeile);
            id++;
        }
        return liste;
    }

    public static string Zahl(double d, int nachkomma = 0) => d.ToString("N" + nachkomma, De);

    // ------------------------------------------------------------------ Heizkessel
    public static readonly Satz[] Heizkessel =
    {
        new("Kessel 1", (Katalogfilterprofil.SpHersteller, "Hersteller A"), (Katalogfilterprofil.SpBrennstoff, "Erdgas H"),
            (Katalogfilterprofil.SpPtherm, 20.0), (Katalogfilterprofil.SpEta, 0.98), (Katalogfilterprofil.SpBrennwert, true)),
        new("Kessel 2", (Katalogfilterprofil.SpHersteller, "Hersteller A"), (Katalogfilterprofil.SpBrennstoff, "Erdgas H"),
            (Katalogfilterprofil.SpPtherm, 50.0), (Katalogfilterprofil.SpEta, 0.97), (Katalogfilterprofil.SpBrennwert, true)),
        new("Kessel 3", (Katalogfilterprofil.SpHersteller, "Hersteller B"), (Katalogfilterprofil.SpBrennstoff, "Heizöl EL"),
            (Katalogfilterprofil.SpPtherm, 100.0), (Katalogfilterprofil.SpEta, 0.92), (Katalogfilterprofil.SpBrennwert, false)),
        new("Kessel 4", (Katalogfilterprofil.SpHersteller, "Hersteller B"), (Katalogfilterprofil.SpBrennstoff, "Holzpellets"),
            (Katalogfilterprofil.SpPtherm, 30.0), (Katalogfilterprofil.SpEta, 0.9), (Katalogfilterprofil.SpBrennwert, false)),
        new("Kessel 5", (Katalogfilterprofil.SpHersteller, "Hersteller C"), (Katalogfilterprofil.SpBrennstoff, "Erdgas H"),
            (Katalogfilterprofil.SpPtherm, 150.0), (Katalogfilterprofil.SpEta, 0.97), (Katalogfilterprofil.SpBrennwert, true)),
        new("Kessel 6", (Katalogfilterprofil.SpHersteller, "Hersteller C"), (Katalogfilterprofil.SpBrennstoff, "Flüssiggas"),
            (Katalogfilterprofil.SpPtherm, 60.0), (Katalogfilterprofil.SpEta, 0.96), (Katalogfilterprofil.SpBrennwert, true)),
    };

    public static ErzeugerDetail HeizkesselDetail(string name) => new(
        name, "Brennwertkessel, modulierend",
        new[] { ("Brennstoff Typ:", "Erdgas H"), ("Leistung [kW]:", "50,00") },
        ("Brennwertkessel", true));

    // ------------------------------------------------------------------ BHKW
    public static readonly Satz[] Bhkw =
    {
        Bhkwsatz("BHKW 1", "Hersteller A", 20, 40),
        Bhkwsatz("BHKW 2", "Hersteller A", 50, 80),
        Bhkwsatz("BHKW 3", "Hersteller B", 100, 150),
        Bhkwsatz("BHKW 4", "Hersteller B", 200, 250),
        Bhkwsatz("BHKW 5", "Hersteller C", 10, 25),
    };

    private static Satz Bhkwsatz(string name, string firma, double pel, double pth) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpBrennstoff, "Erdgas H"),
        (Katalogfilterprofil.SpPel, pel), (Katalogfilterprofil.SpPtherm, pth),
        (Katalogfilterprofil.SpSigma, Math.Round(pel / pth, 2)), (Katalogfilterprofil.SpEta, 0.9),
        (Katalogfilterprofil.SpMotortyp, "Gas-Otto-Motor"));

    public static ErzeugerDetail BhkwDetail(string name) => new(
        name, "Modul mit Abgaswärmetauscher",
        new[] { ("Brennstoff Typ:", "Erdgas H"), ("Hersteller:", "Hersteller A"),
                ("thermische Leistung [kWth]:", "80"), ("elektrische Leistung [kWel]:", "50") });

    // ------------------------------------------------------------------ Pufferspeicher
    public static readonly Satz[] Pufferspeicher =
    {
        Puffersatz("Speicher 1, 500 l", "Hersteller A", 500, 1.5),
        Puffersatz("Speicher 2, 800 l", "Hersteller A", 800, 1.8),
        Puffersatz("Speicher 3, 1000 l", "Hersteller B", 1000, 2.0),
        Puffersatz("Speicher 4, 1500 l", "Hersteller B", 1500, 2.5),
        Puffersatz("Speicher 5, 2000 l", "Hersteller C", 2000, 3.0),
    };

    private static Satz Puffersatz(string name, string firma, double liter, double verlust) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpSpeichertyp, "Pufferspeicher"),
        (Katalogfilterprofil.SpVolumen, liter), (Katalogfilterprofil.SpVerluste, verlust));

    public static ErzeugerDetail PufferDetail(string name) => new(
        name, "",
        new[] { ("Hersteller:", "Hersteller B"), ("Speichertyp:", "stehend"),
                ("Bereitschaftsverluste:", "2,0"), ("Gesamtvolumen [l]:", "1.000,0") });

    // ------------------------------------------------------------------ Stromspeicher
    public static readonly Satz[] Stromspeicher =
    {
        Batteriesatz("Speicher 1, 100 kWh", "Hersteller A", "LFP", 100, 50),
        Batteriesatz("Speicher 2, 10 kWh", "Hersteller A", "LFP", 10, 5),
        Batteriesatz("Speicher 3, 20 kWh", "Hersteller B", "NMC", 20, 10),
        Batteriesatz("Speicher 4, 50 kWh", "Hersteller B", "LFP", 50, 25),
        Batteriesatz("Speicher 5, 200 kWh", "Hersteller C", "LFP", 200, 100),
    };

    private static Satz Batteriesatz(string name, string firma, string chemie, double kwh, double kw) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpChemie, chemie),
        (Katalogfilterprofil.SpEnergie, kwh), (Katalogfilterprofil.SpLeistung, kw),
        (Katalogfilterprofil.SpHerkunft, "Handeingabe"), (Katalogfilterprofil.SpQuelle, "Handeingabe"),
        (Katalogfilterprofil.SpCrate, 0.5), (Katalogfilterprofil.SpEtaRt, 0.9), (Katalogfilterprofil.SpZyklen, 6000));

    public static ErzeugerDetail StromspeicherDetail(string name) => new(
        name, "",
        new[] { ("Typ:", "LFP"), ("Leistung [kW]:", "50"),
                ("Energie (Kapazität) [kWh]:", "100"), ("Degradation [%/a]:", "1,0"),
                ("Ladezustand [%]:", "50"), ("Modulkosten [€/kWh]:", "500") });

    // ------------------------------------------------------------------ Photovoltaik
    public static readonly Satz[] Photovoltaik =
    {
        Modulsatz("Modul A", "Hersteller A", 400, 20.0, 2.0),
        Modulsatz("Modul B", "Hersteller A", 450, 21.0, 2.1),
        Modulsatz("Modul C", "Hersteller B", 350, 19.0, 1.8),
        Modulsatz("Modul D", "Hersteller B", 500, 22.0, 2.3),
        Modulsatz("Modul E", "Hersteller C", 300, 18.0, 1.7),
    };

    private static Satz Modulsatz(string name, string firma, double watt, double eta, double flaeche) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpPstc, watt), (Katalogfilterprofil.SpEta, eta),
        (Katalogfilterprofil.SpTechnologie, "Mono-c-Si"), (Katalogfilterprofil.SpModulflaeche, flaeche),
        (Katalogfilterprofil.SpTnoct, 45.0));

    public static ErzeugerDetail PhotovoltaikDetail(string name) => new(
        name, "monokristallines Modul",
        new[] { ("Hersteller:", "Hersteller A"), ("Modul Leistung [W]:", "400,00") });

    // ------------------------------------------------------------------ Solarkollektoren
    public static readonly Satz[] Solarkollektoren =
    {
        Kollektorsatz("Kollektor A", "Hersteller A", "Flachkollektor", 2.5, 0.8, 3.5),
        Kollektorsatz("Kollektor B", "Hersteller A", "Flachkollektor", 2.0, 0.78, 3.8),
        Kollektorsatz("Kollektor C", "Hersteller B", "Vakuumröhrenkollektor", 3.0, 0.7, 1.2),
        Kollektorsatz("Kollektor D", "Hersteller B", "Flachkollektor", 2.3, 0.8, 3.6),
        Kollektorsatz("Kollektor E", "Hersteller C", "Vakuumröhrenkollektor", 2.0, 0.65, 1.0),
    };

    private static Satz Kollektorsatz(string name, string firma, string typ, double apertur, double eta0, double k1) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpKollektortyp, typ),
        (Katalogfilterprofil.SpApertur, apertur), (Katalogfilterprofil.SpEtaNull, eta0), (Katalogfilterprofil.SpK1, k1));

    public static ErzeugerDetail KollektorDetail(string name) => new(
        name, "",
        new[] { ("Kollektor:", "Flach"), ("Hersteller :", "Hersteller A"),
                ("Beschreibung :", "Flachkollektor"), ("Aperturfläche:", "2,50") });

    // ------------------------------------------------------------------ Wärmepumpe
    public static readonly Satz[] Waermepumpen =
    {
        Wpsatz("Wärmepumpe A", "Hersteller A", "Sole-Wasser", 10, 4.5, 0),
        Wpsatz("Wärmepumpe B", "Hersteller A", "Luft-Wasser", 12, 3.5, 10),
        Wpsatz("Wärmepumpe C", "Hersteller B", "Sole-Wasser", 20, 4.6, 15),
        Wpsatz("Wärmepumpe D", "Hersteller B", "Luft-Wasser", 30, 3.4, 25),
        Wpsatz("Wärmepumpe E", "Hersteller C", "Wasser-Wasser", 40, 5.5, 0),
    };

    private static Satz Wpsatz(string name, string firma, string quelle, double kw, double cop, double kuehl) => new(name,
        (Katalogfilterprofil.SpHersteller, firma), (Katalogfilterprofil.SpQuelle, quelle),
        (Katalogfilterprofil.SpNennleistung, kw), (Katalogfilterprofil.SpVlMin, 25), (Katalogfilterprofil.SpVlMax, 60),
        (Katalogfilterprofil.SpZuheizung, 6.0), (Katalogfilterprofil.SpKuehlleistung, kuehl), (Katalogfilterprofil.SpCop, cop));

    public static WaermepumpeAnlageDaten WaermepumpeAnlage() => new()
    {
        Bezeichner = "Wärmepumpe A",
        Firma = "Hersteller A",
        IdWp = 1,
        Vorlauf = 35,
        Ruecklauf = 28,
        Nennleistung = 10,
        Betriebsart = DbWerte.WP_BETRIEBSART_PARALLEL,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        HeizstabLeistung = 6
    };

    public static WaermepumpeStammDaten WaermepumpeStamm(string name, string firma, int kw) => new()
    {
        Id = 1, Name = name, Firma = firma, Nennleistung = kw, Heizstab = 6, Modulkosten = 10000
    };

    // ------------------------------------------------------------------ Kältemaschine
    public static readonly Satz[] Kaeltemaschinen =
    {
        Kmsatz("Kältemaschine A", 50, 3.0, WindowsFormsApplication1.MyResource.Resource.KM_RUECKKUEHLART_LUFT),
        Kmsatz("Kältemaschine B", 100, 3.5, WindowsFormsApplication1.MyResource.Resource.KM_RUECKKUEHLART_LUFT),
        Kmsatz("Kältemaschine C", 200, 4.0, WindowsFormsApplication1.MyResource.Resource.KM_RUECKKUEHLART_WASSER),
        Kmsatz("Kältemaschine D", 400, 4.5, WindowsFormsApplication1.MyResource.Resource.KM_RUECKKUEHLART_WASSER),
    };

    private static Satz Kmsatz(string name, double kw, double eer, string rueck) => new(name,
        (Katalogfilterprofil.SpHersteller, "Hersteller A"), (Katalogfilterprofil.SpNennkaelteleistung, kw),
        (Katalogfilterprofil.SpEer, eer), (Katalogfilterprofil.SpRueckkuehlart, rueck));

    // ------------------------------------------------------------------ Brauchwasser und Prozesswärme
    public static readonly (string Name, string Typ, double Mwh, string Beschreibung)[] Brauchwasser =
    {
        ("Wohnhaus 10 Wohnungen", "Mehrfamilienhaus", 30, "zehn Wohnungen, Bestandsprofil"),
        ("Wohnhaus 20 Wohnungen", "Mehrfamilienhaus", 60, "zwanzig Wohnungen, Bestandsprofil"),
        ("Einfamilienhaus", "Einfamilienhaus", 4, "vier Personen"),
        ("Hotel 50 Zimmer", "Hotel", 120, "Hotel mit Frühstück"),
        ("Büro 100 Arbeitsplätze", "Büro", 10, "Handwaschbecken und Teeküchen"),
    };

    public static readonly (string Name, string Typ, double Mwh, string Beschreibung)[] Prozesswaerme =
    {
        ("Reinigung Einschicht", "Einschichtbetrieb", 50, "Teilereinigung, Montag bis Freitag"),
        ("Trocknung Zweischicht", "Zweischichtbetrieb", 200, "Trockner, zwei Schichten"),
        ("Galvanik Dreischicht", "Dreischichtbetrieb", 400, "Bäder, durchgehend"),
        ("Waschanlage", "Einschichtbetrieb", 30, "Fahrzeugwäsche"),
    };

    public static IReadOnlyList<Katalogfilterzeile> BedarfZeilen(
        IEnumerable<(string Name, string Typ, double Mwh, string Beschreibung)> saetze)
        => saetze.Select((s, i) => new Katalogfilterzeile(i + 1, s.Name)
                .MitText(Katalogfilterprofil.SpBezeichner, s.Name)
                .MitText(Katalogfilterprofil.SpTyp, s.Typ)
                .MitZahl(Katalogfilterprofil.SpJahressummeMwh, s.Mwh, 3)
                .MitText(Katalogfilterprofil.SpBeschreibung, s.Beschreibung)
                .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, i < 2))
            .ToList();

    // ------------------------------------------------------------------ Gebäude
    public static readonly (string Name, string Art, string Verwendung, string Klasse, double Flaeche)[] Gebaeude =
    {
        ("Wohnhaus Musterstraße", "großes Mehrfamilienhaus", "Wohngebäude", "2002 bis 2009", 1200),
        ("Wohnhaus Beispielweg", "Einfamilienhaus", "Wohngebäude", "2016 bis 2020", 150),
        ("Bürogebäude Am Markt", "Verwaltungsgebäude", "Gewerbe+Sonstige", "2016 bis 2020", 2500),
        ("Schule Lindenallee", "Schule", "Gewerbe+Sonstige", "1919 bis 1948", 4000),
        ("Hotel Am See", "Hotel", "Gewerbe+Sonstige", "2002 bis 2009", 3000),
    };

    public static IReadOnlyList<Katalogfilterzeile> GebaeudeZeilen(Katalogfilterprofil profil)
        => Zeilen(profil, Gebaeude.Select(g => new Satz(g.Name,
               (Katalogfilterprofil.SpGebaeudeart, g.Art), (Katalogfilterprofil.SpVerwendung, g.Verwendung),
               (Katalogfilterprofil.SpBaualtersklasse, g.Klasse), (Katalogfilterprofil.SpFlaecheM2, g.Flaeche),
               (Katalogfilterprofil.SpBeschreibung, "Beispielgebäude"))));
}
