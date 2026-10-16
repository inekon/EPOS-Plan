using System.Globalization;
using EPOS.UI.Seiten.Pufferspeicher;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die Beispieldaten des Wiki-Bilds „Pufferspeicher auslegen“: ein Kombipuffer (Heizung und
/// Brauchwasser über eine Frischwasserstation) an einer leistungsgeregelten Wärmepumpe mit 40 kW,
/// Vorlauf 55 °C, Heizlast 30 kW, ohne Sperrzeit. Startstand und Vorlagen stehen hier, das Ergebnis
/// rechnet der Kern selbst (<see cref="PufferAuslegungCtrl.Rechnen"/>, ohne Datenbank) aus
/// synthetischen Jahresreihen — Kriterien, Rechenwege und Vorschlag sind seine, nicht erfundene.
/// Neutrale Namen, runde Werte, ein Katalog aus „Pufferspeicher 1“ bis „Pufferspeicher 8“.
/// </summary>
public partial class Wikibild
{
    // Einmal gerechnet: Ein neuer Startstand je Zeichnen setzte die Ansicht auf Schritt 1 zurück.
    private IDictionary<string, object>? _pufferauslegung;

    private static IDictionary<string, object> PufferauslegungGaben()
    {
        PufferAuslegungEingang basis = PufferauslegungEingang();
        PufferAuslegungEingabeDaten eingabe = PufferAuslegungHuelle.EingabeAus(basis);
        var dienste = new PufferAuslegungDienste(
            d => PufferAuslegungHuelle.Abbilden(PufferAuslegungCtrl.Rechnen(PufferauslegungMit(basis, d))),
            Speichern: _ => null,
            Uebernehmen: (_, u) => new PufferUebernahmeErgebnis(true, "", 1),
            Nachbarstufen: d =>
            {
                PufferAuslegungEingang e = PufferauslegungMit(basis, d);
                return PufferAuslegungHuelle.Abbilden(PufferAuslegung.Nachbarstufen(e, PufferAuslegungCtrl.Rechnen(e)));
            });

        string erzeuger = string.Format(CultureInfo.CurrentCulture, Resource.PAUS_HERK_ERZEUGER,
            40.0, "", Resource.PAUS_HERK_ERZEUGER_GEREGELT, 0.0, "");
        var start = new PufferAuslegungStartDaten
        {
            IdProjekt = 1,
            IdPuffer = 1,
            Projektname = "Beispielprojekt Wohnquartier",
            PufferBezeichner = "Puffer 1",
            Einstieg = Resource.PAUS_EINSTIEG_KONFIG,
            Eingabe = eingabe,
            Herkunft = new[]
            {
                new PufferHerkunftDaten("Klassen", "Puffer", "Puffer", "Nutzung des Puffers: Heizung und Brauchwasser"),
                new PufferHerkunftDaten("Vorlage", "Kaskade", "Kaskade", "Wärmepumpe 1 an Rang 1, ohne Zweiterzeuger"),
                new PufferHerkunftDaten("HeizgrenzeC", "Projekt", "Projekt", "Heizgrenze des Projekts"),
                new PufferHerkunftDaten("Erzeuger", "Kaskade", "Kaskade", erzeuger)
            },
            Nutzungsprofil = PufferAuslegungTexte.Nach("PAUS_NP_", "WOHNEN", "WOHNEN"),
            NutzungsprofilHerkunft = "Nutzungsart des Zapfprofils „Mehrfamilienhaus“",
            Vorlagen = PufferAuslegungCtrl.Vorlagen().Select(PufferauslegungVorlage).ToList(),
            Uebergabearten = new[]
            {
                new PufferWahlDaten("", Resource.PAUS_UEBERGABE_IDEAL),
                new PufferWahlDaten("RADIATOR", Resource.PAUS_UEBERGABE_RADIATOR),
                new PufferWahlDaten("FLAECHE", Resource.PAUS_UEBERGABE_FLAECHE),
                new PufferWahlDaten("KONVEKTOR", Resource.PAUS_UEBERGABE_KONVEKTOR),
                new PufferWahlDaten("LUEFTER", Resource.PAUS_UEBERGABE_LUEFTER)
            },
            Zirkulationswege = new[]
            {
                new PufferWahlDaten("", Resource.PAUS_ZIRK_AUTOMATIK),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.ZAPFPROFIL), Resource.PAUS_ZIRK_ZAPFPROFIL),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.PROJEKT), Resource.PAUS_ZIRK_PROJEKT),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.ANTEIL), Resource.PAUS_ZIRK_ANTEIL),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.JE_WE), Resource.PAUS_ZIRK_JE_WE)
            },
            Reihen = new[]
            {
                PufferauslegungReihe(Resource.KANAL_HEIZUNG_ANZEIGE, basis.ReiheHeizung),
                PufferauslegungReihe(Resource.KANAL_BRAUCHWASSER_ANZEIGE, basis.ReiheBrauchwasser),
                PufferauslegungReihe(Resource.KANAL_PROZESS_ANZEIGE, basis.ReiheProzess)
            },
            Erzeuger = new[] { erzeuger },
            IstWaermepumpe = true,
            VorlaufC = basis.VorlaufC,
            RuecklaufC = basis.RuecklaufC,
            SchwelleEin = basis.SchwelleEin ?? 0.1,
            SchwelleAus = basis.SchwelleAus ?? 0.9,
            ZapfVorhanden = true,
            ZapfTopologie = basis.Zapfprofil.Topologie.ToString(),
            ZapfDmaxKwh = basis.Zapfprofil.DmaxKwh,
            ZapfPersonen = basis.Zapfprofil.Personen,
            ZapfTagesbedarfL = basis.Zapfprofil.TagesbedarfL,
            BezeichnerMuster = Resource.PAUS_BEZEICHNER_MUSTER
        };
        start.Ergebnis = dienste.Rechnen(eingabe);
        var uhr = System.Diagnostics.Stopwatch.StartNew();
        start.Ergebnis.Nachbarstufen = dienste.Nachbarstufen!(eingabe);
        start.Ergebnis.Nachbarstufen.DauerMs = uhr.Elapsed.TotalMilliseconds;
        start.Ergebnis.Nachbarstufen.Automatisch = true;

        return new Dictionary<string, object>
        {
            ["Daten"] = start,
            ["Dienste"] = dienste,
            ["Texte"] = new PufferAuslegungTexte()
        };
    }

    /// <summary>Der Eingang des Kerns: Kombipuffer an einer Wärmepumpe, synthetische Jahresreihen.</summary>
    private static PufferAuslegungEingang PufferauslegungEingang()
    {
        var heizung = new double[8760];
        var brauchwasser = new double[8760];
        for (int i = 0; i < 8760; i++)
        {
            int tag = i / 24, stunde = i % 24;
            double aussen = 9 - 11 * Math.Cos(2 * Math.PI * (tag - 15) / 365.0) + 3 * Math.Sin(2 * Math.PI * (stunde - 9) / 24.0);
            heizung[i] = Math.Max(0, (15 - aussen) / 20.0 * 30.0);
            // Wohnüblicher Tagesgang: kurze Morgenspitze um 7 Uhr, kleinere Abendspitze um 19 Uhr, sonst wenig;
            // im Mittel rund 2,7 kW (≈ 64 kWh je Tag). Die Spitzen über der Ladeleistung tragen die Kurve.
            double gang = stunde switch { 7 => 30.0, 6 or 8 => 6.0, 19 => 12.0, 20 => 6.0, _ => 0.25 };
            brauchwasser[i] = gang;
        }
        double mittel = brauchwasser.Average();
        for (int i = 0; i < 8760; i++) brauchwasser[i] *= 2.7 / mittel;

        int[] volumen = { 500, 800, 1000, 1500, 2000, 3000, 4000, 5000 };
        return new PufferAuslegungEingang
        {
            KlasseHeizung = true,
            KlasseBrauchwasser = true,
            Vorlage = PufferVorlage.WP_MONO,
            Nutzungsprofil = PufferNutzungsprofil.WOHNEN,
            Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = true, MindestleistungKw = 12 },
            VorlaufC = 55,
            RuecklaufC = 45,
            SchwelleEin = 0.1,
            SchwelleAus = 0.9,
            ReiheHeizung = heizung,
            ReiheBrauchwasser = brauchwasser,
            ReiheProzess = new double[8760],
            Uebergabeart = "FLAECHE",
            HeizgrenzeC = 15,
            AuslegungsheizlastKw = 30,
            Sperrfenster = PufferSperrprofil.Fenster(PufferSperrprofilWert.Keine),
            Zapfprofil = new PufferZapfprofil
            {
                Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 20, LadeleistungKw = 20, Personen = 40, TagesbedarfL = 1600
            },
            Katalog = volumen.Select((v, i) => new PufferKatalogsatz(i + 1, $"Pufferspeicher {i + 1}", v, null, true)).ToList(),
            Parameter = PufferAuslegungParameter.Vorgabe()
        };
    }

    /// <summary>Der Arbeitsstand der Ansicht über den Eingang gelegt — wie die Hülle es tut, ohne Datenbank.</summary>
    private static PufferAuslegungEingang PufferauslegungMit(PufferAuslegungEingang b, PufferAuslegungEingabeDaten d)
    {
        PufferVorlage vorlage = Enum.TryParse(d.Vorlage, out PufferVorlage v) ? v : b.Vorlage;
        PufferZirkulationWeg? zw = !string.IsNullOrEmpty(d.ZirkulationWeg) && Enum.TryParse(d.ZirkulationWeg, out PufferZirkulationWeg z)
            ? z : null;
        return b with
        {
            KlasseHeizung = d.KlasseHeizung,
            KlasseBrauchwasser = d.KlasseBrauchwasser,
            KlasseProzess = d.KlasseProzess,
            Vorlage = vorlage,
            Erzeuger = b.Erzeuger with { Geregelt = d.WpGeregelt, ZweiterzeugerFrei = d.ZweiterzeugerFrei, MindestleistungKw = d.MindestleistungKw },
            Uebergabeart = string.IsNullOrEmpty(d.Uebergabeart) ? null : d.Uebergabeart,
            HeizgrenzeC = d.HeizgrenzeC,
            AuslegungsheizlastKw = d.AuslegungsheizlastKw,
            AnlagenvolumenL = d.AnlagenvolumenL,
            Sperrfenster = PufferSperrprofil.Fenster(string.IsNullOrEmpty(d.Sperrprofil) ? PufferSperrprofilWert.Keine : d.Sperrprofil,
                                                     d.SperrbeginnH, d.SperrdauerH),
            SperrzeitExpertenweg = d.SperrzeitExpertenweg,
            MindestlaufzeitMin = d.MindestlaufzeitMin,
            StartzielJeTag = d.StartzielJeTag,
            Deckungsziel = d.DeckungszielProzent / 100.0,
            DeltaTBK = d.DeltaTBK,
            TPufferObenC = d.TPufferObenC,
            ZirkulationWeg = zw,
            Wohneinheiten = d.Wohneinheiten,
            Parameter = PufferAuslegungCtrl.ParameterMitKriterien(PufferAuslegungParameter.Vorgabe(), vorlage, d.Kriterien)
        };
    }

    private static PufferReiheDaten PufferauslegungReihe(string kanal, IReadOnlyList<double> werte)
        => werte.Count == 0 ? new PufferReiheDaten(kanal, 0, 0) : new PufferReiheDaten(kanal, werte.Max(), werte.Sum());

    /// <summary>Eine Vorlagenkarte wie in der Hülle: Titel, Untertitel, Text und Kriterienschalter aus dem Kern.</summary>
    private static PufferVorlageDaten PufferauslegungVorlage(PufferVorlagenbeschreibung v)
    {
        string typ = v.Vorlage.ToString();
        var kriterien = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (string k in PufferAuslegungVorgaben.VORLAGE_SCHALTER)
            kriterien[k] = v.Kriterien.TryGetValue(k, out bool an) && an;
        string faust = v.Faustwert > 0
            ? string.Format(CultureInfo.CurrentCulture, Resource.PAUS_FAUSTWERT, v.Faustwert.ToString("0.##", CultureInfo.CurrentCulture), v.FaustwertEinheit)
            : "";
        return new PufferVorlageDaten(typ,
            PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ, typ),
            PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ + "_UNTER", ""),
            PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ + "_TEXT", ""),
            kriterien, faust);
    }
}
