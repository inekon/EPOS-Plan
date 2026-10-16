using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Klimadaten;
using EPOS.UI.Dialoge.Strom;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using SpeicherEngine;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die Beispieldaten der weiteren Wiki-Bilder (Klimadaten, Kältebedarf, Betriebskalender, Wärmebedarf extern,
/// Stromlastgang, Zapfprofil, Baustoffe): neutrale Namen, runde Werte, keine Datenbank, keine Hersteller.
/// Die Reihen sind synthetisch; sie zeigen die Form, keine Messung.
/// </summary>
public partial class Wikibild
{
    // ---------------------------------------------------------------- Klimadaten
    private static readonly CultureInfo Deutsch = CultureInfo.GetCultureInfo("de-DE");

    private static readonly (string Name, double Lon, double Lat, bool Auslieferung)[] Klimaregionen =
    {
        ("Region Küste, Testreferenzjahr", 9.0, 54.0, true),
        ("Region Flachland, Testreferenzjahr", 11.0, 52.0, true),
        ("Region Mitte, Testreferenzjahr", 9.0, 51.0, true),
        ("Region Mittelgebirge, Testreferenzjahr", 11.0, 50.5, false),
        ("Region Süd, Testreferenzjahr", 10.0, 48.5, false),
        ("Region Süd, Zukunftsszenario 2045", 10.0, 48.5, false),
    };

    private static Task<IReadOnlyList<Katalogfilterzeile>> KlimaRegionen()
        => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(Klimaregionen.Select((r, i) =>
            new Katalogfilterzeile(i + 1, r.Name) { Geschuetzt = r.Auslieferung }
                .MitText(Katalogfilterprofil.SpBezeichner, r.Name)
                .MitText(Katalogfilterprofil.SpQuelle, i == 5 ? "Testreferenzjahr, Szenario" : "Testreferenzjahr")
                .MitText(Katalogfilterprofil.SpStandort, r.Lat.ToString("0.0", Deutsch) + "° N, "
                                                       + r.Lon.ToString("0.0", Deutsch) + "° O")
                .MitZahl(Katalogfilterprofil.SpLongitude, r.Lon, 4)
                .MitZahl(Katalogfilterprofil.SpLatitude, r.Lat, 4)
                .MitText(Katalogfilterprofil.SpImportdatum, "2026-01-15")
                .MitKennzeichen(Katalogfilterprofil.SpSchreibschutz, r.Auslieferung)).ToList());

    private static readonly Zeichenmodell KlimaTemperatur = KlimaModell(false);
    private static readonly Zeichenmodell KlimaSonne = KlimaModell(true);

    private static Zeichenmodell KlimaModell(bool sonne)
    {
        var werte = new double[8760];
        for (int i = 0; i < werte.Length; i++)
        {
            double tag = 2 * Math.PI * i / 8760.0, stunde = 2 * Math.PI * ((i % 24) - 6) / 24.0;
            werte[i] = sonne
                ? Math.Max(0.0, (40.0 - 23.0 * Math.Cos(tag)) * Math.Sin(stunde))
                : 9.0 - 10.0 * Math.Cos(tag - 0.35) + 4.0 * Math.Sin(stunde);
        }
        return sonne
            ? ChartRenderer.JahresgangModell("Sonnenhöhe Verlauf",
                new[] { new ChartRenderer.Reihe("Sonnenhöhe", werte, ChartRenderer.C_AUSSENTEMPERATUR) },
                "Stunde des Jahres", "Sonnenhöhe [°]")
            : ChartRenderer.JahresgangModell("Jahrestemperatur Verlauf",
                new[] { new ChartRenderer.Reihe("Temperatur", werte, ChartRenderer.C_AUSSENTEMPERATUR) },
                "Stunde des Jahres", "Temperatur [°C]");
    }

    private static Task<KlimadatenDialog.Regionsansicht> KlimaAnsicht(string name)
    {
        var r = Klimaregionen.FirstOrDefault(x => x.Name == name);
        return Task.FromResult(new KlimadatenDialog.Regionsansicht(
            "Testreferenzjahr, Beispielregion", r.Lon, r.Lat, KlimaTemperatur, KlimaSonne, "")
        {
            Jahresmittel = 9.0, Tiefstwert = -12.0, Hoechstwert = 32.0
        });
    }

    // ---------------------------------------------------------------- Kältebedarf
    public static readonly (string Name, string Typ, double Mwh, string Beschreibung)[] Kaeltesaetze =
    {
        ("Kühlung Bürogeschoss", "Raumkühlung", 40, "Bürofläche, Montag bis Freitag 7–19 Uhr"),
        ("Kühlung Verkaufsfläche", "Raumkühlung", 80, "Ladenfläche, Montag bis Samstag"),
        ("Serverraum", "Prozesskälte", 120, "durchgehende Grundlast"),
        ("Kühlzelle Lager", "Prozesskälte", 30, "Lagerkühlung, durchgehend"),
        ("Produktion Zweischicht", "Prozesskälte", 200, "Maschinenkühlung, zwei Schichten"),
    };

    private Katalogfilterprofil? _kaelteBedarfProfil;
    private Katalogfilterprofil KaelteBedarfProfil
        => _kaelteBedarfProfil ??= Katalogfilterprofil.FuerBedarf(BedarfsArt.Kaelte, Uebersetzt).MitVerwendungsspalte(Uebersetzt);

    private List<BedarfsProfilZeile>? _kaelteZeilen;
    private List<BedarfsProfilZeile> KaelteZeilen => _kaelteZeilen ??= new List<BedarfsProfilZeile>
    {
        KaelteZeile(1, "Kühlung Bürogeschoss", 40, 1),
        KaelteZeile(2, "Serverraum", 120, 0)
    };

    private static BedarfsProfilZeile KaelteZeile(int id, string name, double mwh, int kalender) => new()
    {
        IdZ = id, IdStamm = id, Name = name, Summe = mwh, Vorlauf = 16, Ruecklauf = 19, KalenderId = kalender,
        Eingabeart = DbWerte.KAELTE_EINGABE_JAHR,
        Monatsanteile = new double[] { 1, 2, 4, 7, 11, 15, 18, 17, 12, 7, 4, 2 },
        Wochenprofil = Kaelteprofileingabe.WocheAusTagen(
            Enumerable.Range(0, 24).Select(h => h >= 7 && h < 19 ? 1.0 : 0.2).ToArray(),
            Enumerable.Range(0, 24).Select(h => h >= 8 && h < 14 ? 0.5 : 0.2).ToArray(),
            Enumerable.Repeat(0.2, 24).ToArray())
    };

    private static BedarfsProfilInfo? KaelteInfo(string name)
    {
        var s = Kaeltesaetze.FirstOrDefault(x => x.Name == name);
        return s.Name is null ? null : new BedarfsProfilInfo(s.Name, s.Beschreibung, s.Typ, 16, 19);
    }

    private static double KaelteSumme(string name) => Kaeltesaetze.FirstOrDefault(x => x.Name == name).Mwh;

    private static readonly (string Wert, string Text)[] KaelteArtenListe =
    {
        (DbWerte.KAELTE_ART_RAUM, "Raumkühlung"), (DbWerte.KAELTE_ART_PROZESS, "Prozesskälte")
    };

    /// <summary>Die Grafik der markierten Kältezeile aus einer synthetischen Reihe (werktags 7–19 Uhr, Sommeranstieg).</summary>
    private static Kaelteprofilbilder? KaelteGrafik(BedarfsProfilZeile markiert, IReadOnlyList<BedarfsProfilZeile> alle)
    {
        static double[] Reihe(double summeMwh, bool dauer)
        {
            var r = new double[8760];
            for (int d = 0; d < 365; d++)
            {
                double saison = 0.4 + 0.6 * Math.Max(0.0, Math.Sin(Math.PI * (d - 60) / 245.0));
                for (int h = 0; h < 24; h++)
                    r[d * 24 + h] = dauer ? 0.8 + 0.2 * saison : saison * ((d + 3) % 7 < 5 && h >= 7 && h < 19 ? 1.0 : 0.2);
            }
            double s = r.Sum();
            for (int i = 0; i < r.Length; i++) r[i] *= summeMwh * 1000.0 / s;
            return r;
        }
        bool Dauer(BedarfsProfilZeile z) => z.Name == "Serverraum";
        return Kaelteprofilgrafik.Bilder(Reihe(markiert.Summe, Dauer(markiert)),
            alle.Select(z => (z.Name, Reihe(z.Summe, Dauer(z)))).ToList(), 3, null);
    }

    // ---------------------------------------------------------------- Betriebskalender
    private readonly List<BetriebskalenderDaten> _kalender = new()
    {
        new BetriebskalenderDaten { Id = 1, Bezeichner = "Bürobetrieb", Bundesland = "HE",
            Von = new int?[] { 1, 356, null, null }, Bis = new int?[] { 2, 365, null, null } },
        new BetriebskalenderDaten { Id = 2, Bezeichner = "Werksferien Sommer", Bundesland = "BY",
            Von = new int?[] { 213, null, null, null }, Bis = new int?[] { 233, null, null, null } }
    };

    private IReadOnlyList<(int Id, string Text)> Kalenderliste()
        => _kalender.OrderBy(k => k.Bezeichner).Select(k => (k.Id, k.Bezeichner)).ToList();

    private IReadOnlyDictionary<string, object> KalenderGaben() => new Dictionary<string, object>
    {
        ["Laden"] = new Func<IReadOnlyList<BetriebskalenderDaten>>(() => _kalender.OrderBy(k => k.Bezeichner).Select(k => k.Kopie()).ToList()),
        ["Laender"] = (IReadOnlyList<Bundeslandeintrag>)new[]
        {
            new Bundeslandeintrag("BY", "Bayern"), new Bundeslandeintrag("HE", "Hessen"), new Bundeslandeintrag("NW", "Nordrhein-Westfalen")
        },
        ["Pruefen"] = new Func<BetriebskalenderDaten, string?>(_ => null),
        ["Speichern"] = new Func<BetriebskalenderDaten, string?>(_ => null),
        ["Loeschen"] = new Func<int, string?>(_ => null),
        ["Verwendungen"] = new Func<int, int>(_ => 1),
        ["Herleitung"] = new Func<BetriebskalenderDaten, string>(_ => "Feiertage des gewählten Bundeslands und die Ferienzeiträume im Gemeinjahr."),
        ["Gebaeude"] = new Func<IReadOnlyList<BetriebskalenderGebaeude>>(() => new[]
        {
            new BetriebskalenderGebaeude("Bürogebäude", "HE", new[] { (1, 2), (356, 365) })
        })
    };

    // ---------------------------------------------------------------- Wärmebedarf extern und Stromlastgang
    private static readonly (string Name, double Mwh, double Kw)[] Waermelastgaenge =
    {
        ("Lastgang Wohnquartier", 1200, 600),
        ("Lastgang Schule", 400, 250),
        ("Lastgang Hallenbad", 900, 200),
        ("Lastgang Gewerbehalle", 300, 180),
    };

    private static readonly (string Name, double Mwh, double Kw)[] Stromlastgaenge =
    {
        ("Messung Bürogebäude", 250, 90),
        ("Messung Werkstatt", 120, 60),
        ("Messung Kühlhaus", 500, 80),
        ("Messung Supermarkt", 400, 75),
    };

    private Katalogfilterprofil? _waermeGangProfil, _stromGangProfil;
    private Katalogfilterprofil WaermeGangProfil
        => _waermeGangProfil ??= Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Waermebedarf, Uebersetzt).MitVerwendungsspalte(Uebersetzt);
    private Katalogfilterprofil StromGangProfil
        => _stromGangProfil ??= Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Stromganglinie, Uebersetzt).MitVerwendungsspalte(Uebersetzt);

    private static IReadOnlyList<Katalogfilterzeile> Gangkatalog((string Name, double Mwh, double Kw)[] saetze, bool strom)
        => saetze.Select((s, i) =>
            {
                var z = new Katalogfilterzeile(i + 1, s.Name) { Geschuetzt = i == 0 }
                    .MitText(Katalogfilterprofil.SpBezeichner, s.Name)
                    .MitZahl(Katalogfilterprofil.SpJahresarbeitMwh, s.Mwh, Energieeinheit.JAHRESWERT_MWH_NACHKOMMASTELLEN)
                    .MitZahl(Katalogfilterprofil.SpSpitzeKw, s.Kw, 1);
                return strom ? z.MitZahl(Katalogfilterprofil.SpZeitintervall, 4, 0) : z;
            }).ToList();

    private readonly List<WaermebedarfExternZeile> _waermeExtern = new()
    {
        new WaermebedarfExternZeile { IdZ = 1, IdGanglinie = 1, Bezeichner = "Lastgang Wohnquartier", Kanal = "HEIZUNG" }
    };

    private readonly List<GanglinienProjektZeile> _stromGang = new()
    {
        new GanglinienProjektZeile(1, 1, "Messung Bürogebäude")
    };

    private static readonly (string Wert, string Text)[] Kanaele =
    {
        ("HEIZUNG", "Heizung"), ("BRAUCHWASSER", "Brauchwasser"), ("PROZESS", "Prozesswärme")
    };

    private static Zeichenmodell GangModell(bool strom, bool sortiert, int breite, int hoehe)
    {
        var werte = new double[8760];
        for (int i = 0; i < werte.Length; i++)
        {
            int d = i / 24, h = i % 24;
            werte[i] = strom
                ? 30.0 + ((d + 3) % 7 < 5 && h >= 7 && h < 18 ? 50.0 : 0.0) + 5.0 * Math.Sin(2 * Math.PI * i / 8760.0)
                : Math.Max(40.0, 330.0 + 250.0 * Math.Cos(2 * Math.PI * (i - 400) / 8760.0) + 40.0 * Math.Sin(2 * Math.PI * (h - 8) / 24.0));
        }
        if (sortiert) Array.Sort(werte, (a, b) => b.CompareTo(a));
        return ChartRenderer.GanglinieNormiertModell(strom ? "Stromlastgang" : "Wärmelastgang",
            new[] { new ChartRenderer.Reihe("Leistung", werte, strom ? ChartRenderer.C_BEDARF : ChartRenderer.C_BEDARF) },
            "kW", sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate, sortiert,
            breite: breite, hoehe: hoehe);
    }

    private Zeichenmodell? GangBild(GanglinienWahl w, bool sortiert) => GangModell(_bild == "strombedarf", sortiert, 0, 0);
    private Zeichenmodell? GangBildMass(GanglinienWahl w, bool sortiert, int breite, int hoehe)
        => breite <= 0 || hoehe <= 0 ? GangBild(w, sortiert) : GangModell(_bild == "strombedarf", sortiert, breite, hoehe);
    private Task<GanglinienKennzahlen?> GangKennzahlen(GanglinienWahl w)
        => Task.FromResult<GanglinienKennzahlen?>(_bild == "strombedarf"
            ? new GanglinienKennzahlen(250.0, 90.0, 2780)
            : new GanglinienKennzahlen(1200.0, 600.0, 2000));

    private static readonly Func<string, Task<string?>> KeineDatei = _ => Task.FromResult<string?>(null);
    private static readonly Func<string, GanglinienRaster, GanglinienImportRueckrufe, Task<GanglinienImportErgebnis>> KeinImport
        = (_, _, _) => Task.FromResult(new GanglinienImportErgebnis());
    private static readonly Func<string, string, Task<bool>> KeineKopie = (_, _) => Task.FromResult(false);
    private static readonly Schlossweg KeinSchloss = new((_, _) => new SchlossErgebnis(false, ""));

    // ---------------------------------------------------------------- Zapfprofil
    private static ZapfprofilKatalogeintragDaten Eintrag(int id, string name) => new() { Id = id, Name = name };

    private ZapfprofilDaten? _zapf;
    private ZapfprofilDaten Zapf => _zapf ??= ZapfBauen();

    private static ZapfprofilDaten ZapfBauen()
    {
        var d = new ZapfprofilDaten
        {
            IdProjekt = 1,
            Kontext = new ZapfprofilKontextDaten { Projekt = "Beispielprojekt", Klimaregion = "Region Mitte", Kalender = "Gemeinjahr", Bilanzgrenze = "Zapfstelle" },
            Eingabe = new ZapfprofilEingabeDaten
            {
                Weg = ZapfprofilWeg.Generator,
                Zonen =
                {
                    new ZapfprofilZoneDaten { Id = 11, Name = "Wohnen", IdNutzungsart = 1, Bezugsmenge = 40, Angaben = new ZapfprofilZonenangabenDaten() },
                    new ZapfprofilZoneDaten { Id = 12, Name = "Büro", IdNutzungsart = 2, Bezugsmenge = 50, Angaben = new ZapfprofilZonenangabenDaten() }
                },
                Gebaeude = new ZapfprofilGebaeudeDaten()
            },
            Verfuegbar = true,
            SeedVorgabe = 1, RealisierungenVorgabe = 10, RealisierungenMindestens = 1, RealisierungenHoechstens = 1000,
            GebaeudeVorgabe = new ZapfprofilGebaeudeDaten(),
            Vorgaben = new ZapfprofilVorgabenDaten
            {
                KaltwasserMittelC = 10, KaltwasserAmplitudeK = 3, WohnflaecheJeWeM2 = 80, ZirkLaufzeitH = 24, ZirkAnteil = 0.2,
                ZirkVerlustWJeM = 10, ZirkLage = 1, ZirkKennwertLage1 = 5, ZirkKennwertLage2 = 10, KaltwasserAuslegungC = 10,
                SpeicherC = 60, LadefensterH = 10, LadefensterBeginnH = 22, AnzeigetemperaturC = 45, SchwelleKw = 0.1
            },
            Tagesgangsaetze = { Eintrag(1, "Tagesgang Wohnen") },
            Ausstattungen = { Eintrag(3, "Ausstattung Standard") },
            Monatsnamen = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" }
        };
        d.Katalog.Add(ZapfArt(1, "Wohnen", "Personen"));
        d.Katalog.Add(ZapfArt(2, "Büro", "Beschäftigte"));
        d.Vorschau = ZapfVorschau(d.Eingabe);   // die Vorschau steht beim Öffnen, wie nach dem ersten Rechnen
        return d;
    }

    private static ZapfprofilNutzungsartDaten ZapfArt(int id, string name, string einheit) => new()
    {
        Id = id, Name = name, Bezugsart = einheit == "Personen" ? 1 : 6,
        Bezugsgroesse = einheit, Einheit = einheit == "Personen" ? "P" : "B",
        BedarfJeNiveauKwhJeEinheitTag = new[] { 1.0, 2.0, 3.0 }, Herkunft = "Beispielwerte",
        Status = "eigen", Katalogversion = "1", Kalenderart = einheit == "Personen" ? 1 : 2,
        Kalender = einheit == "Personen" ? "Wohnen" : "Arbeitstage", Wohnen = einheit == "Personen", Bilanzgrenze = 1,
        ZapftemperaturC = 50, Monatsfaktoren = Enumerable.Repeat(1.0, 12).ToArray(), IdTagesgangsatz = 1
    };

    /// <summary>Eine synthetische Vorschau: Morgen- und Abendspitze, Zirkulation gleichmäßig, Winter etwas höher.</summary>
    private static ZapfprofilVorschauDaten ZapfVorschau(ZapfprofilEingabeDaten e)
    {
        var texte = new ZapfprofilBildtexte();
        double[] Tag(double f) => Enumerable.Range(0, 24).Select(h =>
            f * (0.3 + 2.4 * Math.Exp(-Math.Pow(h - 7, 2) / 2.0) + 1.8 * Math.Exp(-Math.Pow(h - 19, 2) / 3.0)
                 + (h >= 1 && h <= 4 ? -0.25 : 0.0))).ToArray();
        double[] werktag = Tag(1.0), samstag = Tag(1.1), sonntag = Tag(1.15);
        double[] zirk = Enumerable.Repeat(1.2, 24).ToArray();
        double[] woche = Enumerable.Range(0, 168).Select(i => (i / 24 < 5 ? werktag : i / 24 == 5 ? samstag : sonntag)[i % 24]).ToArray();
        double[] wocheZirk = Enumerable.Repeat(1.2, 168).ToArray();
        double[] monateZapf = Enumerable.Range(0, 12).Select(m => 3300.0 + 450.0 * Math.Cos(2 * Math.PI * m / 12.0)).ToArray();
        double[] monateZirk = Enumerable.Repeat(880.0, 12).ToArray();
        double zapfung = monateZapf.Sum(), verlust = monateZirk.Sum();
        var a = new ZapfprofilAnsichtDaten
        {
            Titel = "Summe aller Zonen", Monat = 1,
            WerktagKw = werktag, SamstagKw = samstag, SonnFeiertagKw = sonntag, TageJeTagtyp = new[] { 21, 5, 5 },
            ZirkulationTagKw = zirk, WochenStarttag = 5, WocheZapfungKw = woche, WocheZirkulationKw = wocheZirk,
            MonateZapfungKwh = monateZapf, MonateZirkulationKwh = monateZirk,
            Kennzahlen = new ZapfprofilKennzahlenDaten
            {
                JahresbedarfZapfungKwh = zapfung, JahresverlustZirkulationKwh = verlust, JahresbedarfGesamtKwh = zapfung + verlust,
                Zirkulationsanteil = verlust / (zapfung + verlust), TagesmittelZapfungKwh = zapfung / 365.0,
                ZapfungLiterJeTag = 2000, GroessterStundenwertKw = 9.0
            },
            UnterschriftTagesgang = "Zapfung in kW (kWh je Stunde), Summe aller Zonen, Januar; Mittel der Tage je Tagtyp."
        };
        a.TagesgangModell = ZapfprofilBilder.TagesgangModell("Januar", werktag, samstag, sonntag, zirk, texte);
        a.WochenprofilModell = ZapfprofilBilder.WochenprofilModell(woche, wocheZirk, texte);
        a.JahresgangModell = ZapfprofilBilder.JahresgangModell(monateZapf.Select(x => x / 1000.0).ToArray(),
            monateZirk.Select(x => x / 1000.0).ToArray(), Energieeinheit.MWh.Text, texte);
        return new ZapfprofilVorschauDaten
        {
            Zustand = ZapfprofilVorschauZustand.Gerechnet,
            Ansichten = { a },
            Zonen =
            {
                new ZapfprofilZonenwertDaten { IdZone = 11, Position = 0, Zone = "Wohnen", JahresbedarfZapfungKwh = zapfung * 0.85 },
                new ZapfprofilZonenwertDaten { IdZone = 12, Position = 1, Zone = "Büro", JahresbedarfZapfungKwh = zapfung * 0.15 }
            }
        };
    }

    // ---------------------------------------------------------------- Baustoffe
    private static readonly (string Name, string Gruppe, double Lambda, double Rho, double Cp, bool Auslieferung)[] Baustoffsaetze =
    {
        ("Beton, bewehrt", "Beton", 2.300, 2400, 1000, true),
        ("Kalksandstein, Rohdichte 1800", "Mauerwerk", 0.990, 1800, 1000, true),
        ("Mauerziegel, Rohdichte 800", "Mauerwerk", 0.210, 800, 1000, true),
        ("Mineralwolle 035", "Dämmstoffe", 0.035, 30, 1030, true),
        ("Polystyrol-Hartschaum 032", "Dämmstoffe", 0.032, 20, 1450, true),
        ("Holzfaserdämmplatte 040", "Dämmstoffe", 0.040, 160, 2100, false),
        ("Kalkzementputz", "Putze und Mörtel", 1.000, 1800, 1000, true),
        ("Gipsputz", "Putze und Mörtel", 0.570, 1300, 1000, true),
        ("Zementestrich", "Estriche", 1.400, 2000, 1000, true),
        ("Nadelholz", "Holz und Holzwerkstoffe", 0.130, 500, 1600, true),
        ("Gipskartonplatte", "Platten", 0.250, 900, 1000, false),
    };

    private Katalogfilterprofil? _baustoffProfil;
    private Katalogfilterprofil BaustoffProfil => _baustoffProfil ??= Katalogfilterprofil.FuerBaustoff(Uebersetzt);

    private static IReadOnlyList<Katalogfilterzeile> BaustoffZeilen()
        => Baustoffsaetze.Select((s, i) => new Katalogfilterzeile(i + 1, s.Name)
                { Schluessel = (i + 1).ToString(CultureInfo.InvariantCulture), Geschuetzt = s.Auslieferung }
            .MitText(Katalogfilterprofil.SpBezeichner, s.Name)
            .MitText(Katalogfilterprofil.SpGruppe, s.Gruppe)
            .MitZahl(Katalogfilterprofil.SpLambda, s.Lambda, 3)
            .MitZahl(Katalogfilterprofil.SpRho, s.Rho, 0)
            .MitZahl(Katalogfilterprofil.SpCp, s.Cp, 0)
            .MitText(Katalogfilterprofil.SpQuelle, "Bemessungswerte nach Norm")
            .MitText(Katalogfilterprofil.SpHerkunft, "Vorgabe")).ToList();

    private static BaustoffDaten? BaustoffLesen(int id)
    {
        if (id < 1 || id > Baustoffsaetze.Length) return null;
        var s = Baustoffsaetze[id - 1];
        return new BaustoffDaten
        {
            Id = id, Bezeichner = s.Name, Gruppe = s.Gruppe, Hersteller = "", Lambda = s.Lambda, Rho = s.Rho, Cp = s.Cp,
            Quelle = "Bemessungswerte nach Norm", Herkunft = "Vorgabe", Auslieferung = s.Auslieferung
        };
    }
}
