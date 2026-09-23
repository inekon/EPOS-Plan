using AngleSharp.Dom;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogbrowser der Erzeuger (iU9-W14a.1) — EINE Komponente, VIER Ausprägungen.
/// Soll sind die Feldkarten von <c>Form_Heizkessel_Admin</c> (19 Zeilen),
/// <c>Form_BHKWAdmin</c> (20), <c>Form_SolarKollektorenAdmin</c> (19) und
/// <c>Form_PufferSp_Admin</c> (16).
///
/// <para><b>Der Feldkartenabgleich läuft je AUSPRÄGUNG</b>, nicht je Komponente
/// (Muster W8/W13, Risiko R-W14-4) — dazu ein eigener Fall für den Lesemodus.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Regel seit iU9-W8): Die
/// Erwartungswerte sind deutsche Beschriftungen und deutsche Zahlenschreibweise.</para>
/// </summary>
public class KatalogBrowserDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    public KatalogBrowserDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfstand: ein kleiner Katalog je Ausprägung
    // =================================================================================

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static KatalogBrowserProfil Profil(KatalogBrowserArt art) =>
        KatalogBrowserProfil.Finde(art, s => WindowsFormsApplication1.MyResource.Resource
                                                 .ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Ein kleiner Katalog je Auspraegung. <b>Seit W14a-E-10</b> traegt eine Zeile
    /// ihre PARAMETERSPALTEN statt eines mehrzeiligen Eigenschaftentextes in EINER
    /// Zelle (Konzept Befund 1.2/2).
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Zeilen(KatalogBrowserArt art) => art switch
    {
        KatalogBrowserArt.Bhkw => new[]
        {
            new Katalogfilterzeile(1, "BHKW A") { Geschuetzt = true }
                .MitText(Katalogfilterprofil.SpBezeichner, "BHKW A")
                .MitText(Katalogfilterprofil.SpHersteller, "2-G")
                .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
                .MitZahl(Katalogfilterprofil.SpPel, 250.0)
                .MitZahl(Katalogfilterprofil.SpPtherm, 250.0),
            new Katalogfilterzeile(2, "BHKW B")
                .MitText(Katalogfilterprofil.SpBezeichner, "BHKW B")
                .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
                .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
                .MitZahl(Katalogfilterprofil.SpPel, 10.0)
                .MitZahl(Katalogfilterprofil.SpPtherm, 20.0)
        },
        KatalogBrowserArt.Solarkollektoren => new[]
        {
            new Katalogfilterzeile(1, "Kollektor A")
                .MitText(Katalogfilterprofil.SpBezeichner, "Kollektor A")
                .MitText(Katalogfilterprofil.SpHersteller, "Junkers")
                .MitText(Katalogfilterprofil.SpKollektortyp, "Flachkollektor")
                .MitZahl(Katalogfilterprofil.SpApertur, 1.94, 2),
            new Katalogfilterzeile(2, "Kollektor B")
                .MitText(Katalogfilterprofil.SpBezeichner, "Kollektor B")
                .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
                .MitText(Katalogfilterprofil.SpKollektortyp, "Röhrenkollektor")
                .MitZahl(Katalogfilterprofil.SpApertur, 1.0, 2)
        },
        _ => new[]
        {
            new Katalogfilterzeile(1, "Eintrag A")
                .MitText(Katalogfilterprofil.SpBezeichner, "Eintrag A")
                .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
                .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
                .MitZahl(Katalogfilterprofil.SpPtherm, 15.0),
            new Katalogfilterzeile(2, "Eintrag B")
                .MitText(Katalogfilterprofil.SpBezeichner, "Eintrag B")
                .MitText(Katalogfilterprofil.SpHersteller, "Buderus")
                .MitText(Katalogfilterprofil.SpBrennstoff, "Heizöl EL")
                .MitZahl(Katalogfilterprofil.SpPtherm, 80.0)
        }
    };

    /// <summary>Ein Detailsatz nach dem Profil — jedes Feld beantwortet.</summary>
    private static IReadOnlyList<BrowserFeldwert> Felder(KatalogBrowserArt art, string name)
    {
        var profil = Profil(art);
        var liste = new List<BrowserFeldwert>();
        foreach (var feld in profil.Detailfelder)
        {
            string wert = feld.Schluessel == KatalogBrowserProfil.FeldBezeichner ? name
                        : feld.Art == BrowserFeldArt.Schalter ? "1"
                        : feld.Art == BrowserFeldArt.Zahl ? "12,50"
                        : feld.Art == BrowserFeldArt.Ganzzahl ? "70"
                        : "Wert " + feld.Schluessel;

            liste.Add(new BrowserFeldwert
            {
                Schluessel = feld.Schluessel,
                Bezeichnung = feld.Bezeichnung,
                Einheit = feld.Einheit,
                Art = feld.Art,
                Editierbar = feld.Editierbar,
                Wert = wert
            });
        }
        return liste;
    }

    private IRenderedComponent<KatalogBrowserDialog> Aufbauen(
        KatalogBrowserArt art = KatalogBrowserArt.Heizkessel,
        bool nurLesen = false,
        KatalogBrowserWege? wege = null,
        Action<BrowserErgebnis>? geschlossen = null,
        Func<string, bool, Action<string?>, IReadOnlyDictionary<string, object>>? editorGaben = null)
    {
        var standard = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(art),
            Detail = name => Felder(art, name),
            Existiert = _ => false,
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n),
            Speichern = (n, _, __) => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n),
            IstGeschuetzt = n => n == "BHKW A"
        };

        return Render<KatalogBrowserDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.ProfilVorgabe, Profil(art))
            .Add(x => x.NurLesen, nurLesen)
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    // =================================================================================
    // Feldbestand je Ausprägung (R-W14-4)
    // =================================================================================

    // Die Feldzahlen sind seit dem Anwenderentscheid vom 15.09.2026 die des VOLLEN
    // Katalogsatzes — jede fachliche Spalte der Stammtabelle ausser ID und ReadOnly
    // (21 / 27 / 14 / 6). Bis dahin waren es 8 / 8 / 8 / 6: der Detailblock der vier
    // Vorlaeufer-Masken. Beim BHKW kamen mit dem Entscheid vom 20.09.2026 die zwei
    // Wirkungsgradanteile dazu. Die Quelle ist KatalogBrowserProfil; hier steht nur
    // die Zahl.
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, "Administration Heizkessel", 21)]
    [InlineData(KatalogBrowserArt.Bhkw, "BHKW Verwaltung", 27)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, "Administration Solarkollektoren", 14)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, "Administration Pufferspeicher", 6)]
    public void Jede_Auspraegung_zeigt_ihren_Titel_und_ihre_Detailfelder(
        KatalogBrowserArt art, string titel, int felder)
    {
        var cut = Aufbauen(art);

        Assert.Equal(titel, cut.Find(".epos-dialog-titel").TextContent);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (var feld in Profil(art).Detailfelder)
            Assert.Contains(feld.Bezeichnung, texte);

        // Ein Feld je Profilzeile — Textfelder, Zahlenfelder, Ganzzahlfelder und der
        // eine Schalter zusammengezählt.
        // Ein Feld je Profilzeile - Textfelder, Zahlenfelder, Ganzzahlfelder und der
        // eine Schalter zusammengezaehlt. Der Schalter liegt in einem eigenen <label>
        // (epos-schalter), die uebrigen in epos-feld.
        int gezeichnet = cut.FindAll(".epos-feld input").Count
                       + cut.FindAll(".epos-feld textarea").Count
                       + cut.FindAll(".epos-schalter input").Count;
        Assert.Equal(felder, gezeichnet);
    }

    /// <summary>
    /// <b>Die zwei Klapplisten sind weg — in ALLEN VIER Ausprägungen</b>
    /// (Anwenderentscheid W14a‑E‑10 vom 07.09.2026). Der Filter sitzt seither im
    /// Spaltenkopf; über der Liste steht nur noch die eine Suchzeile.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher)]
    public void Die_zwei_Klapplisten_sind_dem_Spaltenkopf_gewichen(KatalogBrowserArt art)
    {
        var cut = Aufbauen(art);

        Assert.Empty(cut.FindAll(".epos-katalogbrowser-filter"));
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
        Assert.NotEmpty(cut.FindAll(".epos-trichter"));
    }

    /// <summary>
    /// <b>Aus dem mehrzeiligen Eigenschaftentext sind SPALTEN geworden</b> — auch bei
    /// den zwei Ausprägungen, die im Vorläufer ein <c>DataGridView</c> hatten (BHKW,
    /// Solarkollektoren). Ein Text in EINER Zelle war weder sortierbar noch
    /// vergleichbar (Konzept Befund 1.2/2); jetzt trägt jede Ausprägung die Spalten
    /// ihres Profils.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, 6)]
    [InlineData(KatalogBrowserArt.Bhkw, 8)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, 6)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, 5)]
    public void Jede_Auspraegung_zeigt_die_Spalten_ihres_Profils(
        KatalogBrowserArt art, int spalten)
    {
        var cut = Aufbauen(art);

        Assert.Empty(cut.FindAll(".epos-katalogbrowser-eigenschaften"));
        Assert.Equal(spalten, cut.FindAll(".epos-spaltenkopf").Count);

        // Keine Wahlspalte mehr (Konzept Administrationsdialoge, V4): Die Zeile
        // selbst ist die Wahl, der Kopf traegt nur die Spalten des Profils.
        Assert.Equal(spalten, cut.FindAll("thead th").Count);
    }

    /// <summary>
    /// Der Speichern-Knopf steht, wo das Profil einen Speicherweg ausweist
    /// (<c>HatSpeicherweg</c>) — seit dem 15.09.2026 bei ALLEN VIER Ausprägungen:
    /// Solarkollektoren und Pufferspeicher haben ihren Schreibweg im Kern
    /// (<c>…StammCtrl.AnzeigefelderSchreiben</c>) und ihre Hüllen belegen
    /// <c>KatalogBrowserWege.Speichern</c> damit, seit der Aufklapper „Alle Daten
    /// anzeigen" der Projektdialoge ihn braucht.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, 5)]
    [InlineData(KatalogBrowserArt.Bhkw, 5)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, 5)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, 5)]
    public void Der_Speichern_Knopf_steht_nur_wo_es_einen_Speicherweg_gibt(
        KatalogBrowserArt art, int knoepfe)
    {
        var cut = Aufbauen(art);
        Assert.Equal(knoepfe, cut.FindAll(".epos-leiste .epos-knopf").Count);
    }

    // =================================================================================
    // Liste und Detailblock
    // =================================================================================

    [Fact]
    public void Beim_Oeffnen_steht_die_erste_Zeile_und_ihr_Detailblock()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);
        Assert.Equal("Eintrag A", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
    }

    [Fact]
    public void Eine_andere_Zeile_zieht_ihren_Detailblock_nach()
    {
        var cut = Aufbauen();

        Zeilenklick.Zeile(cut, 1);

        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);
        Assert.Equal("Eintrag B", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
    }

    [Fact]
    public void Ein_Spaltenfilter_engt_die_Liste_ein_ohne_sie_neu_zu_lesen()
    {
        int gelesen = 0;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => { gelesen++; return Zeilen(KatalogBrowserArt.Heizkessel); },
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name)
        };
        var cut = Aufbauen(wege: wege);

        // DIE ZWEI KLAPPLISTEN SIND WEG (W14a-E-10) - an ihrer Stelle steht der
        // Trichter im Spaltenkopf.
        Assert.Empty(cut.FindAll(".epos-katalogbrowser-filter"));
        Assert.NotEmpty(cut.FindAll(".epos-trichter"));

        int nachDemAufbau = gelesen;
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        // Der Brennstofffilter (dritte Spalte) laesst nur "Erdgas E" stehen.
        cut.FindAll(".epos-trichter")[2].Click();
        cut.Find(".epos-spaltenfilter input").Change("Erdgas");

        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        // GEFILTERT WIRD VOR DEM RASTER, nicht in der Datenbank: Der Weg zum Katalog
        // wird dabei NICHT noch einmal gerufen (Konzept 5.6.6).
        Assert.Equal(nachDemAufbau, gelesen);
        Assert.Equal(2, cut.Instance.Zeilen.Count);
    }

    // =================================================================================
    // NurLesen (eigener Abnahmepunkt, R-W14-4)
    // =================================================================================

    /// <summary>
    /// <c>NurLesen</c>: „Neu…" und „Löschen" sind gesperrt, Liste und
    /// Detailblock bleiben sichtbar — wortgleich
    /// <c>Form_PufferSp_Admin.Form_PufferSp_Admin_Load</c> (Z. 39-44).
    ///
    /// <para><b>Und „Speichern" ebenfalls.</b> Der Pufferspeicherkatalog hat seit dem
    /// 15.09.2026 einen Speicherweg, die Leiste trägt also fünf Knöpfe. Ein
    /// schreibgesperrter Browser, dessen Speicherknopf frei stünde, wäre die
    /// gefährlichste Lücke von allen — <c>SpeichernErlaubt</c> nimmt <c>NurLesen</c>
    /// deshalb als ERSTE Bedingung.</para>
    /// </summary>
    [Fact]
    public void NurLesen_sperrt_drei_Knoepfe_und_laesst_Liste_und_Detail_stehen()
    {
        const KatalogBrowserArt art = KatalogBrowserArt.Pufferspeicher;
        var cut = Aufbauen(art, nurLesen: true);

        int v = Versatz(art);
        var knoepfe = cut.FindAll(".epos-leiste .epos-knopf");
        Assert.Equal(3 + v, knoepfe.Count);
        if (v == 2)
        {
            Assert.True(knoepfe[0].HasAttribute("disabled"));           // Speichern
            Assert.True(knoepfe[1].HasAttribute("disabled"));           // Verwerfen
        }
        Assert.True(knoepfe[v].HasAttribute("disabled"));               // Neu...
        Assert.True(knoepfe[v + 1].HasAttribute("disabled"));           // Löschen
        Assert.False(knoepfe[v + 2].HasAttribute("disabled"));          // OK

        // Liste und Detailblock stehen unveraendert.
        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal(6, cut.FindAll(".epos-feld input").Count
                      + cut.FindAll(".epos-feld textarea").Count
                      + cut.FindAll(".epos-schalter input").Count);
    }

    /// <summary>
    /// Ohne <c>NurLesen</c> stehen die drei Knöpfe und OK frei. „Speichern" bleibt
    /// gesperrt, solange nichts geändert ist — das ist seine EIGENE Regel
    /// (<c>Der_Speichern_Knopf_ist_ohne_Aenderung_gesperrt</c>) und kein Schreibschutz.
    /// </summary>
    [Fact]
    public void Ohne_NurLesen_sind_die_drei_Knoepfe_frei()
    {
        const KatalogBrowserArt art = KatalogBrowserArt.Pufferspeicher;
        var cut = Aufbauen(art);

        int v = Versatz(art);
        var knoepfe = cut.FindAll(".epos-leiste .epos-knopf");
        Assert.All(knoepfe.Skip(v), k => Assert.False(k.HasAttribute("disabled")));
        if (v == 2)
        {
            Assert.True(knoepfe[0].HasAttribute("disabled"));           // Speichern, noch ohne Änderung
            Assert.True(knoepfe[1].HasAttribute("disabled"));           // Verwerfen, ebenso
        }
    }

    // =================================================================================
    // Löschen
    // =================================================================================

    /// <summary>
    /// „Löschen" fragt zurück — mit EINEM Text für alle vier Ausprägungen
    /// (Angleichung E-4). Der Solarkollektor-Browser hatte bis hierher einen eigenen
    /// Wortlaut OHNE Namen (Befund W14-B16).
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher)]
    public void Loeschen_fragt_zurueck_und_nennt_den_Namen(KatalogBrowserArt art)
    {
        string? geloescht = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(art),
            Detail = name => Felder(art, name),
            Loeschen = n => { geloescht = n; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(art, wege: wege);

        Loeschknopf(cut, art).Click();

        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains(cut.Instance.Zeilen[0].Bezeichner,
                        cut.Find(".epos-rueckfrage").TextContent);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal(cut.Instance.Zeilen[0].Bezeichner, geloescht);
    }

    [Fact]
    public void Nein_in_der_Rueckfrage_loescht_nicht()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Loeschen = n => { gerufen = true; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(wege: wege);

        Loeschknopf(cut, KatalogBrowserArt.Heizkessel).Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(gerufen);
        Assert.False(cut.Instance.Loeschfrage);
    }

    [Fact]
    public void Ein_abgelehntes_Loeschen_nennt_den_Grund()
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Loeschen = _ => new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", "")
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Loeschknopf(cut, KatalogBrowserArt.Bhkw).Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
    }

    // =================================================================================
    // Neu - und „Bearbeiten…" entfällt (AD-Q6)
    // =================================================================================

    [Fact]
    public void Neu_fragt_erst_den_Namen()
    {
        var cut = Aufbauen();

        Neuknopf(cut, KatalogBrowserArt.Heizkessel).Click();

        Assert.True(cut.Instance.Namensfrage);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Der <c>Exists</c>-Vorabtest gab es im Bestand nur bei Heizkessel und
    /// Pufferspeicher; BHKW und Solarkollektoren legten ohne ihn an. Jetzt fragen alle
    /// vier — und die Meldung ist dieselbe.
    /// </summary>
    [Fact]
    public void Neu_lehnt_einen_vergebenen_Namen_ab()
    {
        bool editor = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Existiert = n => n == "Eintrag A"
        };
        var cut = Aufbauen(wege: wege,
                           editorGaben: (_, __, ___) => { editor = true; return new Dictionary<string, object>(); });

        Neuknopf(cut, KatalogBrowserArt.Heizkessel).Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Eintrag A");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        Assert.False(editor);
        Assert.Equal("Name existiert bereits!", cut.Instance.Meldung);
    }

    /// <summary>
    /// „Löschen" ohne Auswahl meldet dort, wo der Vorläufer meldete (BHKW und
    /// Solarkollektoren), und schweigt dort, wo er schwieg (Heizkessel,
    /// Pufferspeicher) — bitgleich. Bis AD-Q6 prüfte der Fall „Bearbeiten…"; die
    /// Regel dahinter (<c>Ausgewaehlt</c>) ist dieselbe.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, "")]
    [InlineData(KatalogBrowserArt.Bhkw, "Bitte ein BHKW auswählen!")]
    [InlineData(KatalogBrowserArt.Solarkollektoren, "Bitte einen Kollektor auswählen!")]
    [InlineData(KatalogBrowserArt.Pufferspeicher, "")]
    public void Ohne_Auswahl_meldet_nur_wer_es_schon_immer_tat(KatalogBrowserArt art, string meldung)
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = Array.Empty<Katalogfilterzeile>,
            Detail = _ => null
        };
        var cut = Aufbauen(art, wege: wege);

        Assert.Equal("", cut.Instance.Gewaehlt);
        Loeschknopf(cut, art).Click();

        Assert.Equal(meldung, cut.Instance.Meldung);
    }

    // =================================================================================
    // Der Gesamtwirkungsgrad des BHKW (Anwenderentscheid 20.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Der Gesamtwirkungsgrad ist im Aufklapper reine ANZEIGE</b> — eingegeben werden
    /// der elektrische und der thermische Anteil, wie im Katalogeditor.
    /// </summary>
    [Fact]
    public void Der_Gesamtwirkungsgrad_des_BHKW_ist_nicht_editierbar()
    {
        var cut = Aufbauen(KatalogBrowserArt.Bhkw);

        // "BHKW A" ist ein Auslieferungssatz und damit ganz nur lesbar (AD-Q11) -
        // die Feldregel zeigt der eigene Satz "BHKW B".
        Zeilenklick.Zeile(cut, 1);

        Assert.True(Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgrad).HasAttribute("readonly"));
        Assert.False(Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgradEl).HasAttribute("readonly"));
        Assert.False(Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgradTh).HasAttribute("readonly"));
    }

    /// <summary>
    /// <b>Die Summe läuft mit:</b> Wer einen der zwei Anteile ändert, sieht den
    /// Gesamtwirkungsgrad sofort — er ist ihre Summe, auf drei Stellen
    /// (<c>BhkwWirkungsgrad.GesamtAnzeige</c>).
    /// </summary>
    [Fact]
    public void Die_Summe_laeuft_mit_wenn_ein_Anteil_sich_aendert()
    {
        var cut = Aufbauen(KatalogBrowserArt.Bhkw);
        Zeilenklick.Zeile(cut, 1);                 // der eigene Satz "BHKW B" (AD-Q11)

        Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgradEl).Input("0,30");
        Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgradTh).Input("0,60");

        Assert.Equal("0,9", Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgrad).GetAttribute("value"));

        // Ein halbes Paar ergibt keine Summe - leer statt einer halben Wahrheit.
        Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgradTh).Input("");
        Assert.Equal("", Eingabe(cut, KatalogBrowserProfil.FeldWirkungsgrad).GetAttribute("value"));
    }

    /// <summary>Das Eingabefeld zu einem Profilschlüssel — über seine Beschriftung.</summary>
    private static IElement Eingabe(IRenderedComponent<KatalogBrowserDialog> cut, string schluessel)
    {
        string bezeichnung = Profil(KatalogBrowserArt.Bhkw).Detailfelder
                             .Single(f => f.Schluessel == schluessel).Bezeichnung;

        return cut.FindAll("label.epos-feld")
                  .Single(l => l.QuerySelector(".epos-feld-text")?.TextContent == bezeichnung)
                  .QuerySelector("input")!;
    }

    // =================================================================================
    // Der Speicherweg (Heizkessel und BHKW)
    // =================================================================================

    [Fact]
    public void Der_Speichern_Knopf_ist_ohne_Aenderung_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(cut.FindAll(".epos-leiste .epos-knopf")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Eine_Aenderung_gibt_den_Speichern_Knopf_frei_und_schreibt_zurueck()
    {
        IReadOnlyList<BrowserFeldwert>? gesehen = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, f, _) => { gesehen = f; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(wege: wege);

        // Die Kesselleistung ist editierbar (Speicherweg vom 18.08.2026).
        cut.FindAll("input[inputmode=decimal]")[0].Input("42");

        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.False(speichern.HasAttribute("disabled"));
        speichern.Click();

        Assert.NotNull(gesehen);
        Assert.Equal("42", gesehen!.First(f => f.Schluessel == KatalogBrowserProfil.FeldPtherm).Wert);
    }

    // =================================================================================
    // AD-Q6 (22.09.2026): „Bearbeiten…" entfällt, die Felder sind direkt bedienbar
    // =================================================================================

    /// <summary>
    /// <b>Kein „Bearbeiten…" mehr</b> (Konzept Administrationsdialoge, Entscheid AD-Q6,
    /// Stufe 1): Die Felder im Eingabeblock sind direkt bedienbar; neben „Speichern"
    /// steht „Verwerfen". „Neu…" öffnet weiter den Editor.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher)]
    public void Bearbeiten_entfaellt_neben_Speichern_steht_Verwerfen(KatalogBrowserArt art)
    {
        var cut = Aufbauen(art);

        var texte = cut.FindAll(".epos-leiste .epos-knopf").Select(k => k.TextContent.Trim()).ToList();
        Assert.DoesNotContain(texte, t => t.StartsWith("Bearbeiten", StringComparison.Ordinal));
        Assert.Equal("Speichern", texte[0]);
        Assert.Equal("Verwerfen", texte[1]);
        Assert.Equal("Neu...", texte[2]);
    }

    /// <summary>
    /// <b>„Verwerfen" nimmt die Änderung zurück und schreibt nichts</b> — gesperrt,
    /// solange nichts geändert ist, danach frei; nach dem Klick steht der Wert aus dem
    /// Katalog wieder da, und „Speichern" ist wieder gesperrt.
    /// </summary>
    [Fact]
    public void Verwerfen_nimmt_die_Aenderung_zurueck_und_schreibt_nicht()
    {
        bool geschrieben = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, _, __) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(wege: wege);

        string vorher = cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value") ?? "";
        Assert.True(cut.FindAll(".epos-leiste .epos-knopf")[1].HasAttribute("disabled"));

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        Assert.False(cut.FindAll(".epos-leiste .epos-knopf")[1].HasAttribute("disabled"));

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();

        Assert.Equal(vorher, cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value") ?? "");
        Assert.True(cut.FindAll(".epos-leiste .epos-knopf")[0].HasAttribute("disabled"));   // Speichern
        Assert.True(cut.FindAll(".epos-leiste .epos-knopf")[1].HasAttribute("disabled"));   // Verwerfen
        Assert.False(geschrieben);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz wird nie überschrieben</b> (Entscheid AD-Q11,
    /// 23.09.2026): „BHKW A" trägt den Schreibschutz. Seine Felder stehen nur zum Lesen
    /// da, „Speichern" ist WEICH gesperrt (<c>aria-disabled</c>, der Grund im Kurztext),
    /// und ein Klick nennt den Weg über „Duplizieren…" im Warnband — geschrieben wird
    /// nichts, und die Rückfrage „Trotzdem überschreiben?" gibt es nicht mehr.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_sperrt_Speichern_weich_und_nennt_den_Weg()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Speichern = (n, _, __) => { gerufen = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Assert.Equal("BHKW A", cut.Instance.Gewaehlt);
        Assert.True(cut.Instance.Auslieferungssatz);

        // Die Felder sind nur lesbar - es gibt kein bedienbares Zahlenfeld.
        Assert.Empty(cut.FindAll("input[inputmode=decimal]:not([readonly])"));

        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        Assert.False(speichern.HasAttribute("disabled"));
        Assert.Contains("Duplizieren", speichern.GetAttribute("title") ?? "");

        speichern.Click();

        Assert.False(gerufen);
        Assert.Contains("Duplizieren", cut.Instance.Meldung);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>
    /// Ein EIGENER Satz desselben Katalogs bleibt bearbeitbar — die Sperre hängt am
    /// Satz, nicht an der Ausprägung. Der Schreibweg bekommt nie die Erlaubnis, den
    /// Schutz zu übergehen.
    /// </summary>
    [Fact]
    public void Ein_eigener_Satz_bleibt_bearbeitbar_und_uebergeht_nie_den_Schutz()
    {
        bool? uebergangen = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Speichern = (n, _, u) => { uebergangen = u; return new KatalogSpeicherErgebnis(true, "gespeichert", n); }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Zeilenklick.Zeile(cut, 1);                       // "BHKW B", kein Auslieferungssatz
        Assert.False(cut.Instance.Auslieferungssatz);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.Null(speichern.GetAttribute("aria-disabled"));
        speichern.Click();

        Assert.False(uebergangen);
        Assert.Equal("gespeichert", cut.Instance.Status);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_haelt_den_Speicherweg_auf()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, _, __) => { gerufen = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll("input[inputmode=decimal]")[0].Input("keine Zahl");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.False(gerufen);
        Assert.Contains("Leistung", cut.Instance.Meldung);
    }

    // =================================================================================
    // Beenden, Esc und das Kreuz (V15) - und was sie aufhält (V11)
    // =================================================================================

    /// <summary>
    /// <b>„Beenden" statt „OK"</b> (Konzept Administrationsdialoge, V15): Der eine primäre
    /// Schlussknopf steht zuletzt und meldet bestätigt samt gewähltem Eintrag.
    /// </summary>
    [Fact]
    public void Beenden_meldet_bestaetigt_und_den_gewaehlten_Eintrag()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        var letzter = cut.FindAll(".epos-leiste .epos-knopf").Last();
        Assert.Equal("Beenden", letzter.TextContent.Trim());
        Assert.Contains("epos-knopf--primaer", letzter.ClassName);
        letzter.Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
        Assert.Equal("Eintrag A", ergebnis.Bezeichner);
    }

    /// <summary>
    /// <b>Geänderte Felder halten „Beenden" auf</b> (Konzept 3.3): Es schreibt nicht
    /// still zurück und verwirft nicht still, sondern sagt „Speichern oder Verwerfen"
    /// und bleibt offen. Nach „Verwerfen" schließt es.
    /// </summary>
    [Fact]
    public void Beenden_haelt_bei_geaenderten_Feldern_an_und_schreibt_nicht()
    {
        bool geschrieben = false;
        BrowserErgebnis? ergebnis = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, _, __) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(wege: wege, geschlossen: e => ergebnis = e);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();

        Assert.Null(ergebnis);
        Assert.False(geschrieben);
        Assert.Contains("Speichern", cut.Instance.Meldung);
        Assert.Contains("Verwerfen", cut.Instance.Meldung);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();      // Verwerfen
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();  // Beenden

        Assert.NotNull(ergebnis);
        Assert.False(geschrieben);
    }

    /// <summary>
    /// <b>Ein Zeilenwechsel verwirft keine Eingabe still</b> (V11): Mit geänderten
    /// Feldern bleibt die Wahl stehen, und das Warnband sagt warum.
    /// </summary>
    [Fact]
    public void Ein_Zeilenwechsel_haelt_bei_geaenderten_Feldern_die_Wahl()
    {
        var cut = Aufbauen();

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        Zeilenklick.Zeile(cut, 1);

        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);
        Assert.Contains("Verwerfen", cut.Instance.Meldung);
        Assert.Equal("42", cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value"));
    }

    /// <summary>Esc wirkt wie das Kreuz und wie „Beenden" — EIN Schlussweg (V15).</summary>
    [Fact]
    public void Esc_schliesst_wie_Beenden()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026) — und es tut,
    /// was „Beenden" tut (V15).
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Beenden()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog-zu").Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
    }

    [Fact]
    public void Esc_bei_offener_Rueckfrage_schliesst_den_Dialog_nicht()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        Loeschknopf(cut, KatalogBrowserArt.Heizkessel).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Enter ist unbelegt (Hausregel: „Speichern", „Duplizieren…" und „Löschen" schreiben
    /// sofort) — weder am Dialog noch an der Liste.
    /// </summary>
    [Fact]
    public void Enter_tut_nichts()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        Zeilenklick.Taste(cut, "Enter");
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Null(ergebnis);
        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);
        Assert.False(cut.Instance.Namensfrage);
    }

    // =================================================================================
    // Die Zeile ist die Wahl (V4) und die Tastatur (V11)
    // =================================================================================

    /// <summary>
    /// <b>Keine Wahlspalte mehr</b>: kein runder Knopf, die Zeile trägt die Wahl; die
    /// gewählte Zeile ist markiert und ihre Zellen tragen <c>aria-current</c>.
    /// </summary>
    [Fact]
    public void Die_Zeile_ist_die_Wahl_ohne_Wahlspalte()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(".epos-anlagenwahl"));
        Assert.Empty(cut.FindAll("th.epos-spalte-wahl"));

        var zeilen = cut.FindAll(".epos-katalogliste tbody tr");
        Assert.Contains("epos-zeile--gewaehlt", zeilen[0].ClassName ?? "");
        Assert.DoesNotContain("epos-zeile--gewaehlt", zeilen[1].ClassName ?? "");
        Assert.Equal("true", zeilen[0].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
    }

    /// <summary>
    /// <b>↑ ↓ Pos1 Ende</b> bewegen die Wahl — die Liste ist EIN Tabulatorhalt, und der
    /// Eingabeblock folgt.
    /// </summary>
    [Fact]
    public void Pfeiltasten_Pos1_und_Ende_bewegen_die_Wahl()
    {
        var cut = Aufbauen();

        var huelle = cut.Find(".epos-katalogliste .epos-raster-huelle");
        Assert.Equal("0", huelle.GetAttribute("tabindex"));

        Zeilenklick.Taste(cut, "ArrowDown");
        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);
        Assert.Equal("Eintrag B", cut.FindAll("input[type=text]")[0].GetAttribute("value"));

        Zeilenklick.Taste(cut, "ArrowDown");                 // am Ende bleibt es stehen
        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);

        Zeilenklick.Taste(cut, "Home");
        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);

        Zeilenklick.Taste(cut, "End");
        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);

        Zeilenklick.Taste(cut, "ArrowUp");
        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);
    }

    // =================================================================================
    // Das Schloss (V10) und „Duplizieren…" (AD-Q11)
    // =================================================================================

    /// <summary>
    /// <b>Nur das Schloss</b> (AD-Q13): Der Auslieferungssatz trägt es hinter dem Namen,
    /// ohne Wort, mit dem Satz im Kurztext — ein eigener Satz trägt keins.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_traegt_das_Schloss_mit_Kurztext()
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Duplizieren = (id, name) => new KatalogSpeicherErgebnis(true, "", name)
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        var zeilen = cut.FindAll(".epos-katalogliste tbody tr");
        var schloss = zeilen[0].QuerySelector(".epos-schloss");
        Assert.NotNull(schloss);
        Assert.Equal("Auslieferungssatz – nur lesen, Duplizieren erlaubt", schloss!.GetAttribute("title"));
        Assert.Equal(schloss.GetAttribute("title"), schloss.GetAttribute("aria-label"));
        Assert.Equal("", schloss.TextContent.Trim());
        Assert.Null(zeilen[1].QuerySelector(".epos-schloss"));

        // Keine Spalte „Auslieferung" oder „Schreibschutz".
        Assert.DoesNotContain(cut.FindAll(".epos-katalogliste th"),
                              th => th.TextContent.Contains("Auslieferung") || th.TextContent.Contains("Schreibschutz"));
    }

    /// <summary>
    /// <b>Die Fußleiste der Verwaltung</b> (V15): Speichern · Verwerfen · Füller ·
    /// Neu… · Duplizieren… · Löschen · Beenden — der Füller ist die Statuszeile.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_steht_in_der_Reihenfolge_der_Verwaltung()
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, _, __) => new KatalogSpeicherErgebnis(true, "", n),
            Duplizieren = (id, name) => new KatalogSpeicherErgebnis(true, "", name)
        };
        var cut = Aufbauen(wege: wege);

        var leiste = cut.FindAll(".epos-leiste").Last();
        var texte = leiste.QuerySelectorAll(".epos-knopf").Select(k => k.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu...", "Duplizieren...", "Löschen", "Beenden" }, texte);

        var kinder = leiste.Children.ToList();
        int fueller = kinder.FindIndex(k => (k.ClassName ?? "").Contains("epos-leiste-fueller"));
        Assert.Equal(2, fueller);
        Assert.Equal("status", kinder[fueller].GetAttribute("role"));
    }

    /// <summary>
    /// <b>„Duplizieren…" legt einen eigenen Satz an und wählt ihn</b> (AD-Q11): Die
    /// Namensabfrage ist mit „Name (Kopie)" vorbelegt; der Weg bekommt die ID der
    /// gewählten Zeile; danach ist die Kopie gewählt, und die Statuszeile nennt beide.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_die_Kopie_an_und_waehlt_sie()
    {
        var katalog = Zeilen(KatalogBrowserArt.Bhkw).ToList();
        (int Id, string Name)? gerufen = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => katalog,
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Duplizieren = (id, name) =>
            {
                gerufen = (id, name);
                katalog = katalog.Append(new Katalogfilterzeile(99, name)
                    .MitText(Katalogfilterprofil.SpBezeichner, name)).ToList();
                return new KatalogSpeicherErgebnis(true, "", name);
            }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf").First(k => k.TextContent.Trim() == "Duplizieren...").Click();

        Assert.True(cut.Instance.Duplizierfrage);
        var feld = cut.Find(".epos-ueberlagerung input[type=text]");
        Assert.Equal("BHKW A (Kopie)", feld.GetAttribute("value"));

        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((1, "BHKW A (Kopie)"), gerufen);
        Assert.False(cut.Instance.Duplizierfrage);
        Assert.Equal("BHKW A (Kopie)", cut.Instance.Gewaehlt);
        Assert.False(cut.Instance.Auslieferungssatz);
        Assert.Contains("BHKW A (Kopie)", cut.Instance.Status);
        Assert.Contains("dupliziert", cut.Instance.Status);
    }

    /// <summary>Ein vergebener Name hält die Namensabfrage offen; geschrieben wird nichts.</summary>
    [Fact]
    public void Duplizieren_lehnt_einen_vergebenen_Namen_ab()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Duplizieren = (id, name) => { gerufen = true; return new KatalogSpeicherErgebnis(true, "", name); }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf").First(k => k.TextContent.Trim() == "Duplizieren...").Click();
        cut.Find(".epos-ueberlagerung input[type=text]").Input("BHKW B");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(gerufen);
        Assert.True(cut.Instance.Duplizierfrage);
    }

    // =================================================================================
    // Helfer: die Knopfstellen je Ausprägung
    // =================================================================================

    /// <summary>
    /// Mit Speicherweg stehen „Speichern" und „Verwerfen" (AD-Q6) vor dem Füller; ohne
    /// ihn fehlen beide, und alles rückt um zwei.
    /// </summary>
    private static int Versatz(KatalogBrowserArt art) =>
        Profil(art).HatSpeicherweg ? 2 : 0;

    private static AngleSharp.Dom.IElement Neuknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art)];

    private static AngleSharp.Dom.IElement Loeschknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art) + 1];
}
