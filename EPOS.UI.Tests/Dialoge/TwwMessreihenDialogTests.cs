using System.Globalization;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Messdaten" (Umsetzungskonzept Zapfprofilgenerator 4.8, 5.7, Kapitel 7 Zeile Z5;
/// Stufe Z5, Gruppe 3): die Liste der Messreihen des Projekts, der Leerzustand samt Grund, die
/// Dateiwahl mit sofortiger Prüfung, der Bericht einer Ablehnung, die Eingaben über der Dateiwahl,
/// das Einspielen mit und ohne Rückfrage, das Löschen mit Rückfrage, die weichen Sperren, Esc wie
/// Beenden, der Fall ohne Gaben und die Sicht des Assistenten — dazu die Wache über das Textbündel.
///
/// <para>Die Datenseite kommt aus Prüfdelegaten; der Dialog schreibt nie selbst. Kultur de-DE,
/// alle Werte erfunden — kein gemessener Wert eines Objekts steht in dieser Datei.</para>
/// </summary>
public class TwwMessreihenDialogTests : EposBunitContext
{
    public TwwMessreihenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Frist jedes Wartens auf einen gezeichneten Zustand nach einem nebenläufigen Lauf.</summary>
    private static readonly TimeSpan Frist = TimeSpan.FromSeconds(10);

    // =================================================================================
    // Prüfstand (erfunden)
    // =================================================================================

    private static TwwMessreiheDaten Reihe(string name, int aufloesung = 60, int nulllaeufe = 12) => new()
    {
        Bezeichnung = name,
        Groesse = "Energie (kWh)",
        GroesseId = TwwMessreihenwahl.GroesseEnergie,
        AufloesungMin = aufloesung,
        Beginn = "2025-01-01T00:00",
        Schritte = 8760,
        Tage = 365.0,
        Nulllaeufe = nulllaeufe,
        Nullanteil = nulllaeufe / 8760.0,
        Quelle = "Probenzaehler (erfunden)",
        DatumImport = "2026-09-25"
    };

    /// <summary>Ein Stand mit zwei eingespielten Reihen.</summary>
    private static TwwMessreihenstandDaten Voll() => new()
    {
        TabelleDa = true,
        Reihen = new List<TwwMessreiheDaten> { Reihe("Zaehler A (erfunden)"), Reihe("Zaehler B (erfunden)", 15, 0) },
        LueckenschwelleVorgabe = 0.05,
        MindesttageVorschlag = 30
    };

    private static TwwMessreihenstandDaten Leer(string grund = "") => new() { TabelleDa = true, Grund = grund };

    /// <summary>Der Prüfstand: er merkt sich, was gerufen wurde, und gibt vorbereitete Antworten.</summary>
    private sealed class Pruefstand
    {
        internal TwwMessreihenstandDaten Stand = Leer();
        internal TwwMessreihenpruefungDaten? Bericht;
        internal TwwMessreihenergebnisDaten? Einspielergebnis;

        internal readonly List<string> Gewaehlt = new();
        internal readonly List<TwwMessreiheneingabeDaten> Geprueft = new();
        internal readonly List<string> Eingespielt = new();
        internal readonly List<string> Geloescht = new();

        internal TwwMessreihenstandDaten StandLesen() => Stand;

        /// <summary>Solange gesetzt, hält die Prüfung an — der Fall prüft Fortschritt und Abbruch.</summary>
        internal TaskCompletionSource? Sperre;

        internal async Task<TwwMessreihenpruefungDaten> Pruefen(string pfad, TwwMessreiheneingabeDaten eingabe,
                                                               CancellationToken abbruch)
        {
            Geprueft.Add(eingabe);
            if (Sperre is not null)
            {
                using (abbruch.Register(() => Sperre.TrySetResult()))
                    await Sperre.Task;
                abbruch.ThrowIfCancellationRequested();
            }
            return Bericht ?? Gut();
        }

        internal TwwMessreihenergebnisDaten Einspielen(string pfad, TwwMessreiheneingabeDaten eingabe)
        {
            Eingespielt.Add(pfad);
            TwwMessreihenergebnisDaten e = Einspielergebnis ?? new TwwMessreihenergebnisDaten
            {
                Ok = true,
                Bezeichnung = "Zaehler A (erfunden)",
                Meldung = "„Zaehler A (erfunden)“ eingespielt: 8.760 Zeile(n), 0 ersetzt.",
                Stand = Voll()
            };
            if (e.Ok) Stand = e.Stand;
            return e;
        }

        internal TwwMessreihenergebnisDaten Loeschen(string bezeichnung)
        {
            Geloescht.Add(bezeichnung);
            var stand = new TwwMessreihenstandDaten { TabelleDa = true };
            stand.Reihen.AddRange(Stand.Reihen.Where(r => r.Bezeichnung != bezeichnung));
            var e = new TwwMessreihenergebnisDaten
            {
                Ok = true,
                Bezeichnung = bezeichnung,
                Meldung = "„" + bezeichnung + "“ entfernt: 8.760 Zeile(n).",
                Stand = stand
            };
            Stand = stand;
            return e;
        }

        /// <summary>Ein Bericht, der die Datei annimmt.</summary>
        internal static TwwMessreihenpruefungDaten Gut(bool ersetzt = false)
        {
            var b = new TwwMessreihenpruefungDaten
            {
                Bezeichnung = "Zaehler A (erfunden)",
                ErsetztVorhandene = ersetzt,
                Zusammenfassung = "Die Datei trägt 8.760 Zeitschritt(e) im Raster 60 min — 365,0 Tag(e)."
            };
            b.Angaben.Add(new TwwMessreihenangabeDaten("Bezeichnung", "Zaehler A (erfunden)"));
            b.Angaben.Add(new TwwMessreihenangabeDaten("Gefüllte Lücken", "3"));
            b.Hinweise.Add("Die Reihe überstreicht einen 29. Februar; er hat im Rechenjahr keinen Gegentag.");
            return b;
        }

        /// <summary>Ein Bericht, der die Datei mit Datei und Zeile ablehnt.</summary>
        internal static TwwMessreihenpruefungDaten Schlecht() => new()
        {
            Abgebrochen = true,
            Abbruch = "Die Datei ist abgelehnt: In probe.csv, Zeile 7, ist „x“ keine Zahl."
        };
    }

    private IRenderedComponent<TwwMessreihenDialog> Aufbauen(Pruefstand p, Action<bool>? geschlossen = null,
                                                            string datei = "C:/proben/messung.csv")
        => Render<TwwMessreihenDialog>(b => b
            .Add(x => x.Stand, p.StandLesen)
            .Add(x => x.DateiWaehlen, f => { p.Gewaehlt.Add(f); return Task.FromResult(datei); })
            .Add(x => x.Pruefen, p.Pruefen)
            .Add(x => x.Einspielen, p.Einspielen)
            .Add(x => x.Loeschen, p.Loeschen)
            .Add(x => x.Geschlossen, g => geschlossen?.Invoke(g)));

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Die Liste
    // =================================================================================

    [Fact]
    public void Die_Liste_zeigt_je_Reihe_eine_Zeile_mit_neun_Spalten()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        Assert.Equal(9, cut.FindAll(".epos-tww-messreihen-liste thead th").Count);
        IElement[] zeilen = cut.FindAll(".epos-tww-messreihen-liste tbody tr").ToArray();
        Assert.Equal(2, zeilen.Length);
        string erste = zeilen[0].TextContent;
        Assert.Contains("Zaehler A (erfunden)", erste);
        Assert.Contains("Energie (kWh)", erste);
        Assert.Contains("365,0", erste);
        Assert.Contains("Probenzaehler (erfunden)", erste);
        Assert.Contains("2026-09-25", erste);

        // Die beiden Herleitungszeilen sagen, dass Messreihen zum Projekt gehoeren und was ein
        // Nulllauf ist - nie eine stille Zahl.
        string ganz = cut.Find(".epos-tww-messreihen").TextContent;
        Assert.Contains("gehören zum Projekt", ganz);
        Assert.Contains("keiner Auslieferungsvorlage", ganz);
        Assert.Contains("gefüllte Lücke oder eine gemessene Stunde ohne Zapfung", ganz);
    }

    [Fact]
    public void Ohne_Messreihe_steht_der_Grund_statt_einer_leeren_Liste()
    {
        var p = new Pruefstand { Stand = Leer("Messreihen brauchen ein gespeichertes Projekt.") };
        var cut = Aufbauen(p);

        Assert.Empty(cut.FindAll(".epos-tww-messreihen-liste"));
        Assert.Contains("gespeichertes Projekt", cut.Find(".epos-tww-messreihen-leer").TextContent);
    }

    [Fact]
    public void Ohne_Grund_steht_der_Leersatz_des_Buendels()
    {
        var cut = Aufbauen(new Pruefstand());
        Assert.Contains("keine Messreihe eingespielt", cut.Find(".epos-tww-messreihen-leer").TextContent);
    }

    [Fact]
    public void Die_Zeilenwahl_markiert_und_ein_zweiter_Klick_hebt_sie_auf()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        cut.FindAll(".epos-tww-messreihen-wahl")[1].Click();
        Assert.Equal("Zaehler B (erfunden)", cut.Instance.Gewaehlte);
        Assert.Single(cut.FindAll(".epos-tww-messreihen-liste tbody tr.epos-raster-zeile--gewaehlt"));

        cut.FindAll(".epos-tww-messreihen-wahl")[1].Click();
        Assert.Equal("", cut.Instance.Gewaehlte);
        Assert.Empty(cut.FindAll(".epos-tww-messreihen-liste tbody tr.epos-raster-zeile--gewaehlt"));
    }

    // =================================================================================
    // Dateiwahl und Prüfung
    // =================================================================================

    [Fact]
    public void Die_Dateiwahl_prueft_sofort_und_zeigt_den_Bericht()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);

        // Ohne Datei ist „Einspielen" weich gesperrt und meldet den Grund.
        IElement einspielen = Knopf(cut, "Einspielen");
        Assert.Equal("true", einspielen.GetAttribute("aria-disabled"));
        Assert.Contains("keine Datei", einspielen.GetAttribute("title") ?? "");
        einspielen.Click();
        Assert.Empty(p.Eingespielt);
        Assert.Contains("keine Datei", cut.Instance.Meldung);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Assert.Single(p.Geprueft);
        Assert.Contains("8.760 Zeitschritt(e)", cut.Find(".epos-tww-messreihen-summe").TextContent);
        Assert.Equal(2, cut.FindAll(".epos-tww-messreihen-pruefung tbody tr").Count);
        Assert.Contains("29. Februar", cut.Find(".epos-tww-messreihen-hinweis").TextContent);
        Assert.Null(Knopf(cut, "Einspielen").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Eine_abgelehnte_Datei_steht_als_Banner_und_sperrt_das_Einspielen()
    {
        var p = new Pruefstand { Bericht = Pruefstand.Schlecht() };
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Bericht?.Abgebrochen));
        Assert.Contains("Zeile 7", cut.Find(".epos-tww-messreihen-pruefung, .epos-warnbanner").TextContent);

        IElement einspielen = Knopf(cut, "Einspielen");
        Assert.Equal("true", einspielen.GetAttribute("aria-disabled"));
        einspielen.Click();
        Assert.Empty(p.Eingespielt);
        Assert.Contains("Zeile 7", cut.Instance.Meldung);
    }

    /// <summary>
    /// Neu geprüft wird nur, was das LESEN der Datei ändert: gemessene Größe, Lückenschwelle und
    /// Zeitrechnung. Bezeichnung und Quelle beschreiben die Reihe — sie schicken den CSV-Leser nicht
    /// je Tastendruck über die ganze Datei, sondern setzen die leise Zeile zum offenen Bericht.
    /// </summary>
    [Fact]
    public void Nur_eine_Angabe_des_Formats_prueft_die_Datei_neu()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.Single(p.Geprueft));

        // Bezeichnung und Quelle: KEINE Neupruefung, aber eine leise Zeile.
        cut.FindAll("input[type=text]").First().Input("Eigene Bezeichnung");
        cut.FindAll("input[type=text]")[1].Input("Eigene Quelle");
        Assert.Single(p.Geprueft);
        Assert.Contains("nach der nächsten Prüfung", cut.Find(".epos-tww-messreihen-berichtoffen").TextContent);

        // Die gemessene Groesse: ungewaehlt bleibt sie leer - leer und die Kennung 0 heissen beide
        // „aus der Kopfzeile lesen" (TwwMessreiheneingabeDaten.GroesseId). Gewaehlt wird Volumen.
        Assert.Null(p.Geprueft[0].GroesseId);
        cut.FindAll("select").First().Change(TwwMessreihenwahl.GroesseVolumen.ToString(CultureInfo.InvariantCulture));
        cut.WaitForAssertion(() => Assert.Equal(2, p.Geprueft.Count));
        Assert.Equal(TwwMessreihenwahl.GroesseVolumen, p.Geprueft[1].GroesseId);
        // Die neue Pruefung traegt Bezeichnung und Quelle mit; die leise Zeile faellt weg.
        Assert.Equal("Eigene Bezeichnung", p.Geprueft[1].Bezeichnung);
        Assert.Equal("Eigene Quelle", p.Geprueft[1].Quelle);
        Assert.Empty(cut.FindAll(".epos-tww-messreihen-berichtoffen"));

        // Die Zeitrechnung: Vorgabe Ortszeit, gewaehlt Normalzeit.
        Assert.Equal(TwwMessreihenwahl.ZeitOrtszeit, p.Geprueft[1].ZeitstempelId);
        cut.FindAll("input[type=radio]").Last().Change(true);
        cut.WaitForAssertion(() => Assert.Equal(3, p.Geprueft.Count));
        Assert.Equal(TwwMessreihenwahl.ZeitNormalzeit, p.Geprueft[2].ZeitstempelId);
    }

    /// <summary>
    /// Die Prüfung läuft nebenläufig: Fortschritt mit Abbrechen, „Einspielen" bleibt bis zum
    /// Ergebnis gesperrt und nennt den Lauf als Grund; ein Abbruch lässt keinen Bericht stehen.
    /// </summary>
    [Fact]
    public void Die_Pruefung_laeuft_nebenlaeufig_mit_Fortschritt_und_Abbruch()
    {
        var p = new Pruefstand { Sperre = new TaskCompletionSource() };
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.PruefungLaeuft), Frist);
        Assert.Contains("geprüft", cut.Find(".epos-tww-messreihen-pruefschritt .epos-fortschritt").TextContent);

        IElement einspielen = Knopf(cut, "Einspielen");
        Assert.Equal("true", einspielen.GetAttribute("aria-disabled"));
        Assert.Contains("geprüft", einspielen.GetAttribute("title") ?? "");

        cut.Find(".epos-tww-messreihen-pruefschritt button").Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.PruefungLaeuft), Frist);
        Assert.Null(cut.Instance.Bericht);
        Assert.Contains("abgebrochen", cut.Instance.Meldung);
    }

    /// <summary>
    /// Eine nach der Prüfung geänderte Bezeichnung verliert die Rückfrage nicht: Gefragt wird gegen
    /// die Liste des Dialogs und den Namen, der jetzt in den Feldern steht.
    /// </summary>
    [Fact]
    public void Eine_geaenderte_Bezeichnung_traegt_die_Rueckfrage_des_Ersetzens()
    {
        var p = new Pruefstand { Stand = Voll(), Bericht = Pruefstand.Gut() };
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Assert.False(cut.Instance.Bericht!.ErsetztVorhandene);

        // Der Name einer vorhandenen Reihe - ohne Neupruefung, aber mit Rueckfrage.
        cut.FindAll("input[type=text]").First().Input("Zaehler B (erfunden)");
        Assert.Single(p.Geprueft);
        Knopf(cut, "Einspielen").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Contains("Zaehler B (erfunden)", cut.Instance.Fragetext);
        Assert.Empty(p.Eingespielt);
    }

    // =================================================================================
    // Einspielen und Löschen
    // =================================================================================

    [Fact]
    public void Eine_neue_Bezeichnung_wird_ohne_Rueckfrage_eingespielt()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();

        Assert.False(cut.Instance.FrageOffen);
        Assert.Single(p.Eingespielt);
        Assert.Contains("eingespielt", cut.Instance.Status);
        Assert.Equal(2, cut.FindAll(".epos-tww-messreihen-liste tbody tr").Count);
        Assert.Equal("Zaehler A (erfunden)", cut.Instance.Gewaehlte);
    }

    [Fact]
    public void Eine_gleichnamige_Reihe_wird_erst_nach_der_Rueckfrage_ersetzt()
    {
        var p = new Pruefstand { Stand = Voll(), Bericht = Pruefstand.Gut(ersetzt: true) };
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Bericht?.ErsetztVorhandene));
        Knopf(cut, "Einspielen").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Empty(p.Eingespielt);

        // Nein: nichts geschrieben.
        Knopf(cut, "Nein").Click();
        Assert.False(cut.Instance.FrageOffen);
        Assert.Empty(p.Eingespielt);

        // Ja: eingespielt.
        Knopf(cut, "Einspielen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Single(p.Eingespielt);
    }

    [Fact]
    public void Ein_fehlgeschlagenes_Einspielen_meldet_den_Grund_und_laesst_den_Stand()
    {
        var p = new Pruefstand
        {
            Stand = Voll(),
            Einspielergebnis = new TwwMessreihenergebnisDaten
            {
                Meldung = "Die Datei ist abgelehnt: Die Tabelle Tab_TwwMessreihe fehlt.",
                Stand = Voll()
            }
        };
        var cut = Aufbauen(p);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();

        Assert.Contains("Tab_TwwMessreihe fehlt", cut.Instance.Meldung);
        Assert.Equal("", cut.Instance.Status);
        Assert.Equal(2, cut.FindAll(".epos-tww-messreihen-liste tbody tr").Count);
    }

    [Fact]
    public void Loeschen_fragt_zurueck_und_nimmt_die_Zeile_erst_danach_weg()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        cut.FindAll(".epos-tww-messreihen-loeschen")[0].Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Empty(p.Geloescht);

        Knopf(cut, "Nein").Click();
        Assert.Empty(p.Geloescht);
        Assert.Equal(2, cut.FindAll(".epos-tww-messreihen-liste tbody tr").Count);

        cut.FindAll(".epos-tww-messreihen-loeschen")[0].Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal(new[] { "Zaehler A (erfunden)" }, p.Geloescht.ToArray());
        Assert.Single(cut.FindAll(".epos-tww-messreihen-liste tbody tr"));
        Assert.Equal("", cut.Instance.Gewaehlte);
        Assert.Contains("entfernt", cut.Instance.Status);
    }

    // =================================================================================
    // Beenden, Esc und der Fall ohne Gaben
    // =================================================================================

    [Fact]
    public void Beenden_meldet_ob_sich_die_Reihen_geaendert_haben()
    {
        var p = new Pruefstand();
        bool? geaendert = null;
        var cut = Aufbauen(p, g => geaendert = g);

        Knopf(cut, "Beenden").Click();
        Assert.False(geaendert);

        geaendert = null;
        var cut2 = Aufbauen(p, g => geaendert = g);
        Knopf(cut2, "Datei wählen…").Click();
        cut2.WaitForAssertion(() => Assert.NotNull(cut2.Instance.Bericht));
        Knopf(cut2, "Einspielen").Click();
        Knopf(cut2, "Beenden").Click();
        Assert.True(geaendert);
    }

    [Fact]
    public void Esc_wirkt_wie_Beenden_und_haelt_an_einer_offenen_Rueckfrage()
    {
        var p = new Pruefstand { Stand = Voll() };
        bool? geaendert = null;
        var cut = Aufbauen(p, g => geaendert = g);

        // Solange die Rueckfrage steht, schliesst Esc nicht.
        cut.FindAll(".epos-tww-messreihen-loeschen")[0].Click();
        cut.Find(".epos-tww-messreihen").KeyDown("Escape");
        Assert.Null(geaendert);

        Knopf(cut, "Nein").Click();
        cut.Find(".epos-tww-messreihen").KeyDown("Escape");
        Assert.False(geaendert);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_traegt_keinen_Schreibknopf()
    {
        var cut = Render<TwwMessreihenDialog>();

        Assert.Contains("keine Messreihe eingespielt", cut.Find(".epos-tww-messreihen-leer").TextContent);
        Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Einspielen"));
        Assert.NotNull(Knopf(cut, "Beenden"));
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    [Fact]
    public void Die_Maske_meldet_zwoelf_Anzeigen_an_und_nimmt_keinen_Wert()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_MESSREIHEN));
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.BRAUCHWASSER_MESSREIHEN, feld)!;

        Assert.Equal(2, Zugang("anzahl").Lesen());
        Assert.Equal("Zaehler A (erfunden), Zaehler B (erfunden)", Zugang("reihen").Lesen());
        Assert.Equal("", Zugang("gewaehlt").Lesen());
        Assert.Equal("", Zugang("grund").Lesen());

        cut.FindAll(".epos-tww-messreihen-wahl")[0].Click();
        Assert.Equal("Zaehler A (erfunden)", Zugang("gewaehlt").Lesen());
        Assert.Equal("Energie (kWh)", Zugang("groesse").Lesen());
        Assert.Equal(60, Zugang("aufloesung").Lesen());
        Assert.Equal("2025-01-01T00:00", Zugang("beginn").Lesen());
        Assert.Equal(365.0, Zugang("tage").Lesen());
        Assert.Equal(12, Zugang("nulllaeufe").Lesen());
        Assert.Equal("Probenzaehler (erfunden)", Zugang("quelle").Lesen());
        Assert.Equal("2026-09-25", Zugang("importdatum").Lesen());

        // JEDES Feld ist nur lesend - es gibt keinen Setzweg (die Maske fuehrt keinen Einstellwert).
        foreach (string feld in new[] { "anzahl", "reihen", "gewaehlt", "groesse", "aufloesung", "beginn",
                                        "tage", "nulllaeufe", "quelle", "importdatum", "grund", "pruefbericht" })
            Assert.Null(Zugang(feld).Setzen);

        Knopf(cut, "Datei wählen…").Click();
        cut.WaitForAssertion(() => Assert.Contains("8.760 Zeitschritt(e)",
                                                  Convert.ToString(Zugang("pruefbericht").Lesen()) ?? ""));

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_MESSREIHEN));
    }

    // =================================================================================
    // Wache über das Textbündel
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \} = ""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    /// <summary>
    /// Jede Beschriftung des Bündels trägt ihren <c>ZPGM_</c>-Schlüssel, der steht in beiden
    /// Sprachen, und der deutsche Wert gleicht dem Rückfall im Quelltext — sonst zeigte der Dialog
    /// je Sprache etwas anderes als der Test.
    /// </summary>
    [Fact]
    public void Jede_Beschriftung_steht_mit_ihrem_Schluessel_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", "TwwMessreihenDaten.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).ToList();
        Assert.True(treffer.Count >= 50, "Nur " + treffer.Count + " Beschriftungen im Bündel.");

        var vorgabe = new TwwMessreihenTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string schluessel = m.Groups["schluessel"].Value;
            string name = m.Groups["name"].Value;
            string deutsch = Resource.ResourceManager.GetString(schluessel, DE) ?? "";
            string englisch = Resource.ResourceManager.GetString(schluessel, EN) ?? "";
            string rueckfall = (string)typeof(TwwMessreihenTexte).GetProperty(name)!.GetValue(vorgabe)!;

            if (deutsch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.resx");
            if (englisch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
            if (!string.Equals(deutsch, rueckfall, StringComparison.Ordinal))
                funde.Add(schluessel + ": Rückfall „" + rueckfall + "“ ≠ Ressource „" + deutsch + "“");
            if (!schluessel.StartsWith("ZPGM_", StringComparison.Ordinal))
                funde.Add(schluessel + ": kein ZPGM_-Schlüssel");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Der Pfad im Arbeitsbaum — von dieser Quelldatei aus, unabhängig vom Arbeitsordner.</summary>
    private static string Pfad(params string[] teile)
    {
        string wurzel = Wurzel();
        return Path.Combine(new[] { wurzel }.Concat(teile).ToArray());
    }

    private static string Wurzel([CallerFilePath] string hier = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(hier)!, "..", ".."));
}
