using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Brauchwasser-Zapfprofil", <b>Vergleich mit einer Messreihe und Kalibrierung</b>
/// (Umsetzungskonzept Zapfprofilgenerator 4.8, 5.7; Stufe Z5, Gruppe 3): der Knopf „Messdaten…"
/// samt Überlagerung, die Wahl der Messreihe im Reiter Kennzahlen, der nebenläufige Lauf mit
/// Fortschritt und Abbruch, die Kennzahlen als Zeilen (ein STRICH, wo nichts entschieden ist), die
/// Veraltet-Markierung nach einer Eingabe, die Hinweise in der Warnliste, „Aus Messreihe
/// kalibrieren" mit Rückfrage und „Vorschlag übernehmen…" mit Vorschau, Rückfrage und Umstellung
/// der Zone.
///
/// <para>Alle Delegaten sind Prüfstände; der Dialog rechnet und schreibt nie selbst. Die
/// Entprellung steht auf 0. Kultur de-DE, alle Werte erfunden.</para>
/// </summary>
public class ZapfprofilVergleichDialogTests : EposBunitContext
{
    public ZapfprofilVergleichDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Frist jedes Wartens auf einen gezeichneten Zustand nach einem nebenläufigen Lauf.</summary>
    private static readonly TimeSpan Frist = TimeSpan.FromSeconds(10);

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    private const string REIHE_A = "Zaehler A (erfunden)";
    private const string REIHE_B = "Zaehler B (erfunden)";

    private static ZapfprofilNutzungsartDaten Art(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Bezugsart = 2,
        Bezugsgroesse = "Wohneinheiten",
        Einheit = "WE",
        BedarfJeNiveauKwhJeEinheitTag = new[] { 4.0, 5.0, 6.0 },
        Herkunft = "Eigenkonstruktion · Testquelle",
        Status = "Auslieferung",
        Katalogversion = "TEST-1",
        Waehlbar = true
    };

    private static ZapfprofilEingabeDaten Eingabe() => new()
    {
        Weg = ZapfprofilWeg.Bestand,
        Zonen = { new ZapfprofilZoneDaten { Id = 11, Name = "Zone 1", IdNutzungsart = 1, Bezugsmenge = 20 } }
    };

    private static ZapfprofilVorschauDaten Vorschau(ZapfprofilEingabeDaten e)
    {
        var v = new ZapfprofilVorschauDaten { Zustand = ZapfprofilVorschauZustand.Gerechnet, Status = "Vorschau aktuell" };
        for (int i = 0; i < e.Zonen.Count; i++)
            v.Zonen.Add(new ZapfprofilZonenwertDaten
            {
                IdZone = e.Zonen[i].Id, Position = i, Zone = e.Zonen[i].Name,
                JahresbedarfZapfungKwh = (e.Zonen[i].Bezugsmenge ?? 0) * 1000
            });
        v.Ansichten.Add(Ansicht("Summe aller Zonen"));
        for (int i = 0; i < e.Zonen.Count; i++) v.Ansichten.Add(Ansicht(e.Zonen[i].Name));
        return v;
    }

    private static ZapfprofilAnsichtDaten Ansicht(string titel) => new()
    {
        Titel = titel,
        Monat = 1,
        WerktagKw = new double[24],
        Kennzahlen = new ZapfprofilKennzahlenDaten
        {
            JahresbedarfZapfungKwh = 20000,
            JahresverlustZirkulationKwh = 1000,
            JahresbedarfGesamtKwh = 21000
        }
    };

    private static ZapfprofilDaten Daten(ZapfprofilEingabeDaten e) => new()
    {
        IdProjekt = 7,
        Kontext = new ZapfprofilKontextDaten { Projekt = "Beispielprojekt", Klimaregion = "Klimaregion Mitte" },
        Katalog = { Art(1, "Wohnen A"), Art(2, "Büro B") },
        Eingabe = e,
        Vorschau = Vorschau(e),
        Verfuegbar = true,
        SeedVorgabe = 1,
        RealisierungenVorgabe = 10,
        RealisierungenMindestens = 1,
        RealisierungenHoechstens = 1000
    };

    /// <summary>Ein Stand mit zwei eingespielten Reihen.</summary>
    private static TwwMessreihenstandDaten Reihen(params string[] namen)
    {
        var s = new TwwMessreihenstandDaten { TabelleDa = true };
        foreach (string n in namen)
            s.Reihen.Add(new TwwMessreiheDaten
            {
                Bezeichnung = n, Groesse = "Energie (kWh)", GroesseId = TwwMessreihenwahl.GroesseEnergie,
                AufloesungMin = 60, Beginn = "2025-01-01T00:00", Schritte = 8760, Tage = 365.0,
                Quelle = "Probe (erfunden)", DatumImport = "2026-09-25"
            });
        return s;
    }

    /// <summary>Ein Vergleich, den der Kern entschieden hat — ohne Ensemble bleibt die Streuung leer.</summary>
    private static ZapfprofilMessvergleichDaten Vergleich(string reihe = REIHE_A, bool imRahmen = true)
    {
        var v = new ZapfprofilMessvergleichDaten
        {
            Ok = true,
            Reihe = reihe,
            EnergieVerhaeltnis = 1.08,
            EnergieAbweichung = 0.08,
            Spitzenverhaeltnis = 0.9,
            BandUnten = 0.85,
            BandOben = 0.95,
            PerzentilUnten = 0.85,
            PerzentilOben = 0.95,
            Dauerlinienwerte = 8760,
            Lage = ZapfprofilSpitzenlage.ImBand,
            Einheiten = 20,
            WurzelNVerhaeltnis = 0.2236,
            Skalierungsmass = 4.025,
            Formmass = imRahmen ? 0.004 : 0.02,
            Formschwelle = 0.01,
            FormImRahmen = imRahmen,
            MonateGroessteAbweichung = 0.012,
            MonateGroessterMonat = 7
        };
        v.Form.Add(new ZapfprofilFormabgleichDaten("Werktag", 250, 250, imRahmen ? 0.004 : 0.02, 0.05, imRahmen));
        v.Form.Add(new ZapfprofilFormabgleichDaten("Samstag", 52, 52, 0.003, 0.04, true));
        v.Hinweise.Add(new ZapfprofilWarnDaten("ZPG_WARN_MESSVERGLEICH_OHNE_ENSEMBLE", "Ohne Ensemble",
                                              "Die Streuung der Realisierungsspitzen braucht ein Ensemble.",
                                              ZapfprofilWarnstufe.Hinweis));
        return v;
    }

    /// <summary>Ein benannter Hinweis des Kalibriervorschlags — ein Tagtyp ohne Messtag.</summary>
    private static ZapfprofilWarnDaten Vorschlagshinweis()
        => new("ZPG_WARN_MESSKALIBRIERUNG_TAGTYP_FEHLT", "Tagtyp ohne Messtag",
               "Für den Tagtyp Ruhetag trägt die Messung keinen vollständigen Tag; der Tagesgang der Vorlage bleibt.",
               ZapfprofilWarnstufe.Hinweis);

    /// <summary>Der Prüfstand der fünf Delegaten der Stufe Z5.</summary>
    private sealed class Pruefstand
    {
        internal TwwMessreihenstandDaten Stand = Reihen(REIHE_A, REIHE_B);
        internal ZapfprofilMessvergleichDaten? Ergebnis;
        internal ZapfprofilMesskalibrierungDaten? Kalibrierung;
        internal ZapfprofilVorschlagDaten? Vorschlag;
        internal ZapfprofilVorschlagErgebnisDaten? Uebernahme;

        internal readonly List<string> Verglichen = new();
        internal readonly List<(string Reihe, int Zone)> Kalibriert = new();
        internal readonly List<(string Reihe, int Zone)> Vorgeschlagen = new();
        internal readonly List<(string Reihe, int Zone)> Uebernommen = new();

        /// <summary>Solange gesetzt, hält der Lauf an — der Fall prüft Fortschritt und Abbruch.</summary>
        internal TaskCompletionSource? Sperre;

        internal async Task<ZapfprofilMessvergleichDaten> Vergleichen(ZapfprofilEingabeDaten e, string reihe,
                                                                     CancellationToken abbruch)
        {
            Verglichen.Add(reihe);
            if (Sperre is not null)
            {
                using (abbruch.Register(() => Sperre.TrySetResult()))
                    await Sperre.Task;
                abbruch.ThrowIfCancellationRequested();
            }
            return Ergebnis ?? Vergleich(reihe);
        }

        /// <summary>Solange gesetzt, hält die Kalibrierung an — der Fall prüft Fortschritt und Abbruch.</summary>
        internal TaskCompletionSource? Kalibriersperre;

        internal async Task<ZapfprofilMesskalibrierungDaten> Kalibrieren(ZapfprofilEingabeDaten e, string reihe,
                                                                        int zone, CancellationToken abbruch)
        {
            Kalibriert.Add((reihe, zone));
            if (Kalibriersperre is not null)
            {
                using (abbruch.Register(() => Kalibriersperre.TrySetResult()))
                    await Kalibriersperre.Task;
                abbruch.ThrowIfCancellationRequested();
            }
            return Kalibrierung ?? new ZapfprofilMesskalibrierungDaten
            {
                Ok = true,
                Reihe = reihe,
                Wert = 4380.0,
                EinheitId = (int)ZapfprofilMesswerteinheit.KwhJeJahr,
                BilanzgrenzeId = (int)ZapfprofilBilanzgrenze.Zapfstelle,
                Quelle = "Messreihe " + reihe,
                Zeitraum = "2025-01-01 – 2026-01-01"
            };
        }

        internal ZapfprofilVorschlagDaten Vorschlagen(ZapfprofilEingabeDaten e, string reihe, int zone)
        {
            Vorgeschlagen.Add((reihe, zone));
            if (Vorschlag is not null) return Vorschlag;
            var v = new ZapfprofilVorschlagDaten
            {
                Ok = true,
                Reihe = reihe,
                Vorlage = "Wohnen A · TEST-1",
                Kopie = "Wohnen A · TEST-1-E1",
                TagesbedarfKwh = 12.0,
                TagesbedarfJeEinheitKwh = 0.6,
                Bezugsmenge = 20,
                VolleTage = 365
            };
            v.Wochenfaktoren.AddRange(Enumerable.Repeat(1.0 / 7.0, 7));
            v.Tagesgaenge.Add(new ZapfprofilVorschlagsgangDaten("Werktag", 250,
                Enumerable.Repeat(1.0 / 24.0, 24).ToList()));
            v.Hinweise.Add(Vorschlagshinweis());
            return v;
        }

        internal ZapfprofilVorschlagErgebnisDaten Uebernehmen(ZapfprofilEingabeDaten e, string reihe, int zone)
        {
            Uebernommen.Add((reihe, zone));
            if (Uebernahme is not null) return Uebernahme;
            var e2 = new ZapfprofilVorschlagErgebnisDaten
            {
                Ok = true,
                IdNutzungsart = 2,
                Kopie = "Wohnen A · TEST-1-E1",
                Meldung = "Kalibrierte Kopie angelegt: Wohnen A · TEST-1-E1"
            };
            e2.Hinweise.Add(Vorschlagshinweis());
            return e2;
        }
    }

    private IRenderedComponent<ZapfprofilDialog> Aufbauen(Pruefstand p, bool mitMessdaten = true,
                                                         Func<IReadOnlyDictionary<string, object>>? messdaten = null)
    {
        ZapfprofilEingabeDaten e = Eingabe();
        return Render<ZapfprofilDialog>(b =>
        {
            b.Add(x => x.Daten, Daten(e))
             .Add(x => x.Texte, new ZapfprofilTexte())
             .Add(x => x.Vorschau, Vorschau)
             .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
             .Add(x => x.EntprellungMs, 0)
             .Add(x => x.Messreihen, () => p.Stand)
             .Add(x => x.Messvergleich, p.Vergleichen)
             .Add(x => x.Messkalibrierung, p.Kalibrieren)
             .Add(x => x.Kalibriervorschlag, p.Vorschlagen)
             .Add(x => x.VorschlagUebernehmen, p.Uebernehmen);
            if (mitMessdaten)
                b.Add(x => x.MessreihenGaben, messdaten ?? (() => new Dictionary<string, object>()));
        });
    }

    private static IElement Knopf(IRenderedComponent<ZapfprofilDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Option(IRenderedComponent<ZapfprofilDialog> cut, string text)
        => cut.FindAll("label.epos-option").First(l => l.TextContent.Trim() == text).QuerySelector("input")!;

    /// <summary>Stellt den Dialog auf Erweitert und öffnet den Reiter Kennzahlen.</summary>
    private static void Erweitert(IRenderedComponent<ZapfprofilDialog> cut)
    {
        Option(cut, "Erweitert").Change("1");
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
    }

    // =================================================================================
    // Der Knopf „Messdaten…" und seine Überlagerung
    // =================================================================================

    [Fact]
    public void Ohne_Delegat_kein_Knopf_Messdaten()
    {
        var cut = Aufbauen(new Pruefstand(), mitMessdaten: false);
        Assert.Empty(cut.FindAll("button.epos-zapfprofil-messdaten"));
    }

    [Fact]
    public void Der_Knopf_Messdaten_oeffnet_die_Ueberlagerung_und_Esc_schliesst_nur_sie()
    {
        int gerufen = 0;
        var cut = Aufbauen(new Pruefstand(), messdaten: () => { gerufen++; return new Dictionary<string, object>(); });

        Assert.Single(cut.FindAll("button.epos-zapfprofil-messdaten"));
        Knopf(cut, "Messdaten…").Click();
        Assert.Equal(1, gerufen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-tww-messreihen"));

        // Esc des WIRTS schliesst ihn nicht, solange die Ueberlagerung offen ist.
        cut.Find(".epos-zapfprofil").KeyDown("Escape");
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-tww-messreihen"));
    }

    /// <summary>
    /// Das Kreuz der Überlagerung geht denselben Weg wie „Beenden" des eingebetteten Dialogs: Die
    /// Reihen werden neu gelesen, und ein Vergleich gegen eine Reihe, die es nicht mehr gibt, fällt
    /// weg — der Dialog schreibt sofort, also ist nach dem Kreuz alles möglich geschehen.
    /// </summary>
    [Fact]
    public void Das_Kreuz_der_Ueberlagerung_liest_neu_und_raeumt_einen_Vergleich_ohne_Reihe()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p, messdaten: () => new Dictionary<string, object>());
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);

        // Waehrend die Ueberlagerung offen steht, verschwindet die verglichene Reihe.
        Knopf(cut, "Messdaten…").Click();
        p.Stand = Reihen(REIHE_B);
        cut.Find("button.epos-ueberlagerung-zu").Click();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung .epos-tww-messreihen"));
        Assert.Null(cut.Instance.Vergleichsergebnis);
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        Assert.Contains("Wählen Sie die Messreihe", cut.Find(".epos-zapfprofil-vergleich-leer").TextContent);
    }

    // =================================================================================
    // Die Wahl der Messreihe
    // =================================================================================

    [Fact]
    public void In_der_Stufe_Einfach_steht_der_Grund_statt_einer_Zahl()
    {
        var cut = Aufbauen(new Pruefstand());
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();

        Assert.Contains("steht ab der Stufe Erweitert", cut.Find(".epos-zapfprofil-vergleich-leer").TextContent);
        IElement rechnen = cut.Find("button.epos-zapfprofil-vergleich-rechnen");
        Assert.Equal("true", rechnen.GetAttribute("aria-disabled"));
        Assert.Contains("steht ab der Stufe Erweitert", rechnen.GetAttribute("title") ?? "");
    }

    [Fact]
    public void Ohne_eingespielte_Reihe_nennt_der_Reiter_den_Grund()
    {
        var p = new Pruefstand { Stand = new TwwMessreihenstandDaten { TabelleDa = true, Grund = "Es ist keine Messreihe eingespielt." } };
        var cut = Aufbauen(p);
        Erweitert(cut);

        Assert.Contains("keine Messreihe eingespielt", cut.Find(".epos-zapfprofil-vergleich-leer").TextContent);
        Assert.Equal("true", cut.Find("button.epos-zapfprofil-vergleich-rechnen").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Ohne_gewaehlte_Reihe_bleibt_Rechnen_gesperrt_und_nennt_den_Grund()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);

        IElement rechnen = cut.Find("button.epos-zapfprofil-vergleich-rechnen");
        Assert.Equal("true", rechnen.GetAttribute("aria-disabled"));
        rechnen.Click();
        Assert.Empty(p.Verglichen);
        Assert.Contains("Wählen Sie die Messreihe", cut.Find(".epos-zapfprofil-vergleichleiste .epos-status").TextContent);
    }

    // =================================================================================
    // Der Lauf und die Kennzahlen
    // =================================================================================

    [Fact]
    public void Der_Vergleich_zeigt_die_Kennzahlen_als_Verhaeltnisse_und_je_Tagtyp_eine_Zeile()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);

        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleichsergebnis?.Ok), Frist);
        Assert.Equal(new[] { REIHE_A }, p.Verglichen.ToArray());

        string[] zeilen = cut.FindAll(".epos-zapfprofil-vergleichzeile").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(8, zeilen.Length);
        Assert.Contains(zeilen, z => z.StartsWith("Energie gemessen/gerechnet") && z.Contains("1,080"));
        Assert.Contains(zeilen, z => z.StartsWith("Abweichung der Energie") && z.Contains("8,00 %"));
        Assert.Contains(zeilen, z => z.StartsWith("Messspitze") && z.Contains("0,900") && z.Contains("im Band"));
        Assert.Contains(zeilen, z => z.StartsWith("Band der Dauerlinie (P85–P95)") && z.Contains("0,850 … 0,950")
                                     && z.Contains("8.760 Stundenwerte"));
        Assert.Contains(zeilen, z => z.StartsWith("√N-Skalierungsmaß") && z.Contains("4,025") && z.Contains("N = 20"));
        Assert.Contains(zeilen, z => z.StartsWith("Formabgleich") && z.Contains("im Rahmen"));
        Assert.Contains(zeilen, z => z.StartsWith("Größte Abweichung der Monatsanteile") && z.Contains("Monat 7"));

        // Ohne Ensemble steht ein STRICH, nicht eine Null - und der Grund daneben.
        string streuung = Assert.Single(zeilen, z => z.StartsWith("Streuung der Realisierungsspitzen"));
        Assert.Contains("–", streuung);
        Assert.Contains("ohne Ensemble", streuung);

        // Je Tagtyp eine Formzeile mit Tagen beider Seiten und dem Urteil.
        IElement[] form = cut.FindAll(".epos-zapfprofil-formzeile").ToArray();
        Assert.Equal(2, form.Length);
        Assert.Equal("Werktag", form[0].GetAttribute("data-tagtyp"));
        Assert.Contains("250 Messtage / 250 Rechentage", form[0].TextContent);
        Assert.Contains("im Rahmen", form[0].TextContent);

        // Die Statuszeile nennt die Reihe.
        Assert.Contains(REIHE_A, cut.Find(".epos-zapfprofil-vergleichleiste .epos-status").TextContent);
    }

    /// <summary>
    /// <b>Mit Ensemble steht die Spitzenstreuung als Zahl</b> (N15 Gruppe 3, Folge): Sobald die
    /// Hülle die Stundenspitzen der Realisierungen führt, zeigt der Vergleichsbericht die beiden
    /// Grenzen und im Vermerk Realisierungszahl und Streubreite — kein STRICH, und der Hinweis
    /// „ohne Ensemble" fällt weg. Die übrigen sieben Zeilen bleiben, wie sie sind.
    /// </summary>
    [Fact]
    public void Mit_Ensemble_steht_die_Spitzenstreuung_als_Zahl()
    {
        ZapfprofilMessvergleichDaten v = Vergleich();
        v.Hinweise.Clear();
        v.StreuungUnten = 0.82;
        v.StreuungOben = 1.04;
        v.Streubreite = 1.268;
        v.Realisierungen = 25;

        var p = new Pruefstand { Ergebnis = v };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);

        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleichsergebnis?.Ok), Frist);

        string[] zeilen = cut.FindAll(".epos-zapfprofil-vergleichzeile").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(8, zeilen.Length);
        string streuung = Assert.Single(zeilen, z => z.StartsWith("Streuung der Realisierungsspitzen"));
        Assert.Contains("0,820 … 1,040", streuung);
        Assert.Contains("25 Realisierungen", streuung);
        Assert.Contains("1,268", streuung);
        Assert.DoesNotContain("ohne Ensemble", streuung);
        // Kein STRICH mehr in dieser Zeile — sie trägt jetzt zwei Zahlen.
        Assert.DoesNotContain("–", streuung);
    }

    /// <summary>
    /// <b>Mehrere stochastische Zonen tragen ihren eigenen Strichgrund</b> (N18, Gegenprüfung):
    /// Tragen mehrere Zonen ein Ensemble, ist die Stichprobe der Realisierungsspitzen nicht zu
    /// bilden — jede Zone zieht für sich. Die Zeile zeigt dann einen Strich, und im Vermerk steht
    /// genau dieser Grund und nicht „ohne Ensemble": Die Rechnung IST stochastisch, nur die
    /// Stichprobe nicht bildbar. Das ist ein anderer Satz als der leere Ensemblefall darunter.
    /// </summary>
    [Fact]
    public void Mehrere_stochastische_Zonen_zeigen_einen_Strich_mit_ihrem_eigenen_Grund()
    {
        ZapfprofilMessvergleichDaten v = Vergleich();
        v.Hinweise.Clear();
        v.Stochastisch = true;
        v.EnsembleZonen = 2;

        var p = new Pruefstand { Ergebnis = v };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);

        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleichsergebnis?.Ok), Frist);

        string[] zeilen = cut.FindAll(".epos-zapfprofil-vergleichzeile").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(8, zeilen.Length);
        string streuung = Assert.Single(zeilen, z => z.StartsWith("Streuung der Realisierungsspitzen"));
        Assert.Contains("mehrere stochastische Zonen", streuung);
        Assert.DoesNotContain("ohne Ensemble", streuung);
        // Ein Strich statt Zahlen — geschaetzt wird nichts.
        Assert.Contains("–", streuung);

        // Ohne die Zonenzahl bleibt es beim leeren Ensemblefall.
        ZapfprofilMessvergleichDaten ohne = Vergleich();
        ohne.Hinweise.Clear();
        var p2 = new Pruefstand { Ergebnis = ohne };
        var cut2 = Aufbauen(p2);
        Erweitert(cut2);
        Wahl(cut2, 1);
        Knopf(cut2, "Vergleich rechnen").Click();
        cut2.WaitForAssertion(() => Assert.True(cut2.Instance.Vergleichsergebnis?.Ok), Frist);
        string leer = Assert.Single(cut2.FindAll(".epos-zapfprofil-vergleichzeile").Select(z => z.TextContent.Trim()),
                                    z => z.StartsWith("Streuung der Realisierungsspitzen"));
        Assert.Contains("ohne Ensemble", leer);
        Assert.DoesNotContain("mehrere stochastische Zonen", leer);
    }

    [Fact]
    public void Eine_Form_ueber_der_Schwelle_steht_als_abweichend_da()
    {
        var p = new Pruefstand { Ergebnis = Vergleich(imRahmen: false) };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleichsergebnis?.Ok), Frist);

        IElement werktag = cut.FindAll(".epos-zapfprofil-formzeile")[0];
        Assert.Contains("über der Schwelle 0,0100", werktag.TextContent);
        Assert.Contains("epos-kohaerenz--abweichend", werktag.QuerySelector(".epos-kohaerenz")?.ClassName ?? "");
    }

    /// <summary>
    /// <b>Anwenderentscheid ZU35 — „nicht bewertbar" ist gelb</b>: Unter der Mindestzahl der
    /// Einheiten trägt die Bandzeile „nicht bewertbar" mit dem Grund (Einheiten, Mindestzahl) und
    /// der gelben Ampel, die Messspitze nennt „nicht bewertbar", und der Kopf der Bandzeile zeigt
    /// P95–P99,9 (eine Nachkommastelle, keine P100). Eine bewertete Zeile trägt keine Ampel.
    /// </summary>
    [Fact]
    public void Ein_nicht_bewertbares_Band_steht_gelb_mit_seinem_Grund()
    {
        ZapfprofilMessvergleichDaten v = Vergleich();
        v.PerzentilUnten = 0.95;
        v.PerzentilOben = 0.999;
        v.Lage = ZapfprofilSpitzenlage.NichtBewertbar;
        v.BandEinheiten = 3;
        v.BandMindestEinheiten = 10;
        v.Einheiten = 3;
        v.Gesamtampel = ZapfprofilVergleichsampel.Gelb;

        var p = new Pruefstand { Ergebnis = v };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Vergleichsergebnis?.Ok), Frist);

        IElement[] zeilen = cut.FindAll(".epos-zapfprofil-vergleichzeile").ToArray();
        Assert.Equal(8, zeilen.Length);
        IElement band = Assert.Single(zeilen, z => z.TextContent.Trim().StartsWith("Band der Dauerlinie (P95–P99,9)"));
        IElement ampel = band.QuerySelector(".epos-zapfprofil-band-nicht-bewertbar")
                         ?? throw new Xunit.Sdk.XunitException("Die Bandzeile trägt keine Ampel.");
        Assert.Contains("epos-ampel--gelb", ampel.ClassName);
        Assert.Contains("nicht bewertbar — 3 Einheiten, bewertet wird ab 10", ampel.TextContent);
        Assert.DoesNotContain("Stundenwerte", band.TextContent);
        Assert.Contains(zeilen, z => z.TextContent.Trim().StartsWith("Messspitze") && z.TextContent.Contains("nicht bewertbar"));
        // Nur die Bandzeile ist gekennzeichnet; die Grenzen stehen zur Anschauung.
        Assert.Single(cut.FindAll(".epos-zapfprofil-vergleichzeile .epos-ampel"));
        Assert.Contains("0,850 … 0,950", band.TextContent);

        // Gegenprobe: ein bewertetes Band trägt keine Ampel.
        var p2 = new Pruefstand();
        var cut2 = Aufbauen(p2);
        Erweitert(cut2);
        Wahl(cut2, 1);
        Knopf(cut2, "Vergleich rechnen").Click();
        cut2.WaitForAssertion(() => Assert.True(cut2.Instance.Vergleichsergebnis?.Ok), Frist);
        Assert.Empty(cut2.FindAll(".epos-zapfprofil-vergleichzeile .epos-ampel"));
    }

    [Fact]
    public void Ein_abgelehnter_Vergleich_nennt_den_Grund_mit_seiner_Kennung()
    {
        var p = new Pruefstand
        {
            Ergebnis = new ZapfprofilMessvergleichDaten
            {
                Reihe = REIHE_A,
                Kennung = "ZPG_SATZ_MESSVERGLEICH_OHNE_STUNDENWERTE",
                Abbruch = "Die Reihe trägt keine vollständigen Stunden (Raster 1440 min)."
            }
        };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);

        IElement zeile = cut.Find(".epos-zapfprofil-vergleich-abbruch");
        Assert.Equal("ZPG_SATZ_MESSVERGLEICH_OHNE_STUNDENWERTE", zeile.GetAttribute("data-kennung"));
        Assert.Contains("keine vollständigen Stunden", zeile.TextContent);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vergleichzeile"));
    }

    [Fact]
    public void Der_Lauf_zeigt_den_Fortschritt_und_laesst_sich_abbrechen()
    {
        var p = new Pruefstand { Sperre = new TaskCompletionSource() };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);

        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VergleichLaeuft), Frist);
        Assert.Contains(REIHE_A, cut.Find(".epos-fortschritt").TextContent);

        cut.Find(".epos-fortschritt button").Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.VergleichLaeuft), Frist);
        Assert.Null(cut.Instance.Vergleichsergebnis);
        Assert.Contains("abgebrochen", cut.Find(".epos-zapfprofil-vergleichleiste .epos-status").TextContent);
    }

    [Fact]
    public void Ein_Wechsel_der_Reihe_verwirft_das_Ergebnis_der_alten()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);

        Wahl(cut, 2);
        Assert.Null(cut.Instance.Vergleichsergebnis);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vergleichzeile"));
        Assert.Contains("Noch kein Vergleich gerechnet", cut.Find(".epos-zapfprofil-vergleich-leer").TextContent);
    }

    [Fact]
    public void Eine_Eingabe_markiert_den_Vergleich_als_veraltet_und_laesst_die_Zahlen_stehen()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);
        Assert.False(cut.Instance.VergleichUeberholt);

        // Eine Aenderung des Arbeitsstands: die Bezugsmenge der Zone.
        cut.FindAll("input").First(i => i.GetAttribute("value") == "20").Input("30");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VergleichUeberholt), Frist);

        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        Assert.Contains("früheren Arbeitsstand", cut.Find(".epos-zapfprofil-vergleich-veraltet").TextContent);
        Assert.Equal(8, cut.FindAll(".epos-zapfprofil-vergleichzeile").Count);
    }

    [Fact]
    public void Die_Hinweise_des_Vergleichs_stehen_in_der_Warnliste()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);

        IElement zeile = Assert.Single(cut.FindAll(".epos-zapfausl-warnliste li"),
                                      l => l.GetAttribute("data-kennung") == "ZPG_WARN_MESSVERGLEICH_OHNE_ENSEMBLE");
        Assert.Contains("Ohne Ensemble", zeile.TextContent);
        Assert.Equal("hinweis", zeile.GetAttribute("data-stufe"));
    }

    // =================================================================================
    // „Aus Messreihe kalibrieren"
    // =================================================================================

    [Fact]
    public void Ohne_gewaehlte_Reihe_ist_Kalibrieren_weich_gesperrt()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Option(cut, "Erweitert").Change("1");

        IElement knopf = cut.Find("button.epos-zapfprofil-kalibrieren");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Empty(p.Kalibriert);
        Assert.False(cut.Instance.FrageOffen);
        Assert.Contains("Wählen Sie die Messreihe", cut.Instance.Hinweis);
    }

    [Fact]
    public void Kalibrieren_fragt_zurueck_und_setzt_die_Felder_erst_danach()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Contains("Zone 1", cut.Instance.Fragetext);
        Assert.Contains(REIHE_A, cut.Instance.Fragetext);
        Assert.Empty(p.Kalibriert);

        // Nein: nichts gesetzt.
        Knopf(cut, "Nein").Click();
        Assert.Empty(p.Kalibriert);

        // Ja: der Jahresmesswert der Zone steht.
        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal(new[] { (REIHE_A, 0) }, p.Kalibriert.ToArray());
        cut.WaitForAssertion(() => Assert.Contains("Jahresmesswert aus", cut.Instance.Kalibrierstatus), Frist);
        Assert.Contains(REIHE_A, cut.Find(".epos-zapfprofil-kalibrierstatus").TextContent);
    }

    /// <summary>
    /// Eine Reihe über ein ganzes Jahr braucht keine Hochrechnung — die Hülle rechnet dann keine
    /// Jahresreihe, und der Dialog zeigt keinen Fortschritt: Der Wert steht sofort da.
    /// </summary>
    [Fact]
    public void Eine_Volljahresreihe_kalibriert_ohne_sichtbaren_Lauf()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Knopf(cut, "Ja").Click();
        cut.WaitForAssertion(() => Assert.Contains("Jahresmesswert aus", cut.Instance.Kalibrierstatus), Frist);

        Assert.False(cut.Instance.KalibrierungLaeuft);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-kalibrierfortschritt .epos-fortschritt"));
    }

    /// <summary>
    /// Ein Teiljahr wird über den Jahresgang hochgerechnet; dieser Lauf läuft nebenläufig, zeigt
    /// den Fortschritt an der Kalibrierleiste und lässt sich abbrechen. Ein abgebrochener Lauf
    /// setzt kein Feld und nennt den Abbruch.
    /// </summary>
    [Fact]
    public void Ein_Teiljahr_zeigt_den_Fortschritt_und_laesst_sich_abbrechen()
    {
        var p = new Pruefstand { Kalibriersperre = new TaskCompletionSource() };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Knopf(cut, "Ja").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KalibrierungLaeuft), Frist);
        IElement fortschritt = cut.Find(".epos-zapfprofil-kalibrierfortschritt .epos-fortschritt");
        Assert.Contains(REIHE_A, fortschritt.TextContent);

        // Solange er laeuft, ist der Knopf weich gesperrt und nennt den Lauf als Grund.
        IElement knopf = cut.Find("button.epos-zapfprofil-kalibrieren");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains(REIHE_A, knopf.GetAttribute("title") ?? "");

        cut.Find(".epos-zapfprofil-kalibrierfortschritt button").Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.KalibrierungLaeuft), Frist);
        Assert.Contains("abgebrochen", cut.Instance.Hinweis);
        Assert.Equal("", cut.Instance.Kalibrierstatus);
    }

    [Fact]
    public void Eine_abgelehnte_Kalibrierung_meldet_den_Grund()
    {
        var p = new Pruefstand
        {
            Kalibrierung = new ZapfprofilMesskalibrierungDaten
            {
                Kennung = "ZPG_SATZ_MESSKALIBRIERUNG_REIHE_ZU_KURZ",
                Abbruch = "Die Reihe trägt 12 vollständige Tage; für einen Vorschlag sind mindestens 30 nötig."
            }
        };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Knopf(cut, "Ja").Click();
        cut.WaitForAssertion(() => Assert.Contains("mindestens 30", cut.Instance.Hinweis), Frist);
    }

    // =================================================================================
    // „Vorschlag übernehmen…"
    // =================================================================================

    [Fact]
    public void Der_Vorschlag_zeigt_die_Vorschau_und_uebernimmt_erst_nach_der_Rueckfrage()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-vorschlag").Click();
        Assert.Equal(new[] { (REIHE_A, 0) }, p.Vorgeschlagen.ToArray());

        IElement vorschau = cut.Find(".epos-zapfprofil-vorschlagvorschau");
        Assert.Contains("Wohnen A · TEST-1-E1", cut.Find(".epos-zapfprofil-vorschlagkopie").TextContent);
        Assert.Contains("12,00", vorschau.TextContent);
        Assert.Equal(7, cut.FindAll(".epos-zapfprofil-vorschlagwoche td").Count);
        Assert.Single(cut.FindAll(".epos-zapfprofil-vorschlaggaenge tbody tr"));
        Assert.Equal(26, cut.FindAll(".epos-zapfprofil-vorschlaggaenge thead th").Count);

        // „Uebernehmen" fragt zurueck; Nein schreibt nichts.
        Knopf(cut, "Übernehmen").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Empty(p.Uebernommen);
        Knopf(cut, "Nein").Click();
        Assert.Empty(p.Uebernommen);

        // Ja: die Kopie entsteht, die Zone rechnet mit ihr, die Vorschau ist zu.
        Knopf(cut, "Übernehmen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal(new[] { (REIHE_A, 0) }, p.Uebernommen.ToArray());
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vorschlagvorschau"));
        Assert.Contains("Kalibrierte Kopie angelegt", cut.Instance.Kalibrierstatus);
    }

    [Fact]
    public void Ein_Vorschlag_ohne_Ergebnis_steht_als_Banner_ohne_Uebernahmeknopf()
    {
        var p = new Pruefstand
        {
            Vorschlag = new ZapfprofilVorschlagDaten
            {
                Kennung = "ZPG_SATZ_MESSKALIBRIERUNG_OHNE_STUNDENWERTE",
                Abbruch = "Die Reihe trägt keine Stundenwerte (Raster 1440 min)."
            }
        };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-vorschlag").Click();
        Assert.Contains("keinen Vorschlag", cut.Find(".epos-zapfprofil-vorschlagvorschau").TextContent);
        Assert.Contains("keine Stundenwerte", cut.Find(".epos-zapfprofil-vorschlagvorschau").TextContent);
        Assert.Empty(cut.FindAll("button.epos-zapfprofil-vorschlag-uebernehmen"));

        // Abbrechen schliesst die Vorschau, ohne etwas zu schreiben.
        cut.Find("button.epos-zapfprofil-vorschlag-abbrechen").Click();
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vorschlagvorschau"));
        Assert.Empty(p.Uebernommen);
    }

    /// <summary>
    /// Die Hinweise des Kalibriervorschlags stehen in DERSELBEN Warnliste wie die der Bilanz und des
    /// Vergleichs — und sie bleiben dort, wenn die Vorschau mit der Übernahme zugeht: Sie gehören
    /// den Werten der Kopie, nicht der Überlagerung.
    /// </summary>
    [Fact]
    public void Die_Hinweise_des_Vorschlags_stehen_in_der_Warnliste_und_bleiben_nach_der_Uebernahme()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-vorschlag").Click();
        IElement zeile = Assert.Single(cut.FindAll(".epos-zapfausl-warnliste li"),
                                      l => l.GetAttribute("data-kennung") == "ZPG_WARN_MESSKALIBRIERUNG_TAGTYP_FEHLT");
        Assert.Contains("Tagtyp ohne Messtag", zeile.TextContent);
        Assert.Equal("hinweis", zeile.GetAttribute("data-stufe"));

        // Nach der Uebernahme ist die Vorschau zu - die Hinweise des Ergebnisses bleiben.
        Knopf(cut, "Übernehmen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vorschlagvorschau"));
        Assert.Single(cut.FindAll(".epos-zapfausl-warnliste li"),
                      l => l.GetAttribute("data-kennung") == "ZPG_WARN_MESSKALIBRIERUNG_TAGTYP_FEHLT");
    }

    // =================================================================================
    // Kleinkram
    // =================================================================================

    /// <summary>
    /// <b>Ein überholter Lauf hinterlässt nichts</b>: Ändert der Anwender den Arbeitsstand, während
    /// der Vergleich läuft, gehört das Ergebnis einem Stand, der nicht mehr gilt. Es kommt nicht in
    /// die Anzeige — und weil der Lauf nicht der Anwender abgebrochen hat, steht auch keine leise
    /// Zeile „abgebrochen" da.
    /// </summary>
    [Fact]
    public void Ein_ueberholter_Lauf_hinterlaesst_nichts()
    {
        var p = new Pruefstand { Sperre = new TaskCompletionSource() };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);

        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VergleichLaeuft), Frist);

        // Eine Eingabe waehrend des Laufs: die Bezugsmenge der Zone.
        cut.FindAll("input").First(i => i.GetAttribute("value") == "20").Input("30");
        cut.WaitForAssertion(() => Assert.False(cut.Instance.VergleichLaeuft), Frist);

        Assert.Single(p.Verglichen);
        Assert.Null(cut.Instance.Vergleichsergebnis);
        Assert.False(cut.Instance.VergleichUeberholt);
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        Assert.DoesNotContain("abgebrochen", cut.Find(".epos-zapfprofil-vergleichleiste .epos-status").TextContent);
    }

    /// <summary>
    /// <b>Eine eigene Eingabe des Messwerts räumt die Kalibrierhinweise</b>: Sie gehörten dem Wert,
    /// der jetzt nicht mehr da steht — ein Hinweis zu einem überschriebenen Wert wäre falsch.
    /// </summary>
    [Fact]
    public void Eine_eigene_Eingabe_des_Messwerts_raeumt_die_Kalibrierhinweise()
    {
        var kalibrierung = new ZapfprofilMesskalibrierungDaten
        {
            Ok = true,
            Reihe = REIHE_A,
            Wert = 4380.0,
            EinheitId = (int)ZapfprofilMesswerteinheit.KwhJeJahr,
            BilanzgrenzeId = (int)ZapfprofilBilanzgrenze.Zapfstelle,
            Quelle = "Messreihe " + REIHE_A,
            Zeitraum = "2025-01-01 – 2026-01-01",
            Hochgerechnet = true
        };
        kalibrierung.Hinweise.Add(new ZapfprofilWarnDaten("ZPG_WARN_MESSKALIBRIERUNG_HOCHGERECHNET", "Hochgerechnet",
                                                         "Die Reihe deckt 40 von 365 Tagen; der Jahreswert ist hochgerechnet.",
                                                         ZapfprofilWarnstufe.Hinweis));
        var p = new Pruefstand { Kalibrierung = kalibrierung };
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Option(cut, "Erweitert").Change("1");

        cut.Find("button.epos-zapfprofil-kalibrieren").Click();
        Knopf(cut, "Ja").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-zapfausl-warnliste li"),
                                                l => l.GetAttribute("data-kennung") == "ZPG_WARN_MESSKALIBRIERUNG_HOCHGERECHNET"),
                             Frist);

        // Die eigene Eingabe des Jahresmesswerts: der Hinweis faellt weg.
        cut.FindAll("label.epos-feld").First(l => l.TextContent.Contains("Jahresmesswert"))
           .QuerySelector("input")!.Input("5000");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-zapfausl-warnliste li")
                                                  .Where(l => l.GetAttribute("data-kennung") == "ZPG_WARN_MESSKALIBRIERUNG_HOCHGERECHNET")),
                             Frist);
    }

    /// <summary>
    /// <b>Englische Kultur</b>: Jede Zahl des Vergleichsberichts folgt der Kultur der Oberfläche —
    /// im Deutschen „1,080", im Englischen „1.080". Ein festes Format hätte in einer der beiden
    /// Sprachen eine falsche Zahl gezeigt.
    /// </summary>
    [Fact]
    public void Der_Vergleichsbericht_zeichnet_seine_Zahlen_in_der_Kultur_der_Oberflaeche()
    {
        using var _ = new Kulturvorrichtung("en-US");
        var p = new Pruefstand();
        var cut = Aufbauen(p);
        Erweitert(cut);
        Wahl(cut, 1);
        Knopf(cut, "Vergleich rechnen").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Vergleichsergebnis), Frist);

        string bericht = cut.Find("table.epos-zapfprofil-kennzahlen").TextContent;
        Assert.Contains("1.080", bericht);      // Energieverhaeltnis 1,08 in en-US
        Assert.Contains("8.00 %", bericht);     // Abweichung +8 %
        Assert.DoesNotContain("1,080", bericht);
    }

    /// <summary>Wählt die Messreihe mit der Nummer <paramref name="nummer"/> (ab 1) im Reiter Kennzahlen.</summary>
    private static void Wahl(IRenderedComponent<ZapfprofilDialog> cut, int nummer)
        => cut.Find(".epos-zapfprofil-vergleichwahl select")
              .Change(nummer.ToString(CultureInfo.InvariantCulture));
}
