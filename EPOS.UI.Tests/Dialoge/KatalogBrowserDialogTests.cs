using AngleSharp.Dom;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
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
            Speichern = (n, _, __) => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n)
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

        // Stufe 4 (V13): Ein Auslieferungssatz zeigt seine Felder als TEXT (Name und
        // Wert, ohne Doppelpunkt) - gezaehlt wird beides.
        var texte = cut.FindAll(".epos-feld-text, .epos-stammblattwert dt")
                       .Select(e => e.TextContent.Trim().TrimEnd(':').Trim()).ToList();
        foreach (var feld in Profil(art).Detailfelder)
            Assert.Contains(feld.Bezeichnung.Trim().TrimEnd(':').Trim(), texte);

        // Ein Feld je Profilzeile — Textfelder, Zahlenfelder, Ganzzahlfelder und der
        // eine Schalter zusammengezählt.
        // Ein Feld je Profilzeile - Textfelder, Zahlenfelder, Ganzzahlfelder und der
        // eine Schalter zusammengezaehlt. Der Schalter liegt in einem eigenen <label>
        // (epos-schalter), die uebrigen in epos-feld.
        int gezeichnet = cut.FindAll(".epos-feld input").Count
                       + cut.FindAll(".epos-feld textarea").Count
                       + cut.FindAll(".epos-schalter input").Count
                       + cut.FindAll(".epos-stammblattwert").Count;
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
        // selbst ist die Wahl. Seit Stufe 3 (V6) steht davor die KAESTCHENspalte der
        // Mehrfachwahl - sonst traegt der Kopf nur die Spalten des Profils.
        var koepfe = cut.FindAll("thead th");
        Assert.Equal(spalten + 1, koepfe.Count);
        Assert.Contains("epos-spalte-kaestchen", koepfe[0].ClassName ?? "");
        Assert.Empty(cut.FindAll("th.epos-spalte-wahl"));
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
    [InlineData(KatalogBrowserArt.Heizkessel, 4)]
    [InlineData(KatalogBrowserArt.Bhkw, 4)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, 4)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, 4)]
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
        Assert.Equal(2 + v, knoepfe.Count);
        if (v == 2)
        {
            Assert.True(knoepfe[0].HasAttribute("disabled"));           // Speichern
            Assert.True(knoepfe[1].HasAttribute("disabled"));           // Verwerfen
        }
        Assert.True(knoepfe[v].HasAttribute("disabled"));               // Neu...
        Assert.False(knoepfe[v + 1].HasAttribute("disabled"));          // Beenden

        // Loeschen steht seit Stufe 3 in der Auswahlleiste (V8) - im Lesemodus HART
        // gesperrt wie Neu...
        Assert.True(Loeschknopf(cut, art).HasAttribute("disabled"));

        // Liste und Detailblock stehen - seit Stufe 4 (V13) im Lesemodus als TEXT, nicht
        // als gesperrte Felder.
        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal(6, cut.FindAll(".epos-stammblattwert").Count);
        Assert.Empty(cut.FindAll(".epos-stammblatt .epos-feld input"));
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

        // Die erste Zeile des BHKW ist ein Auslieferungssatz - Loeschen ist dort weich
        // gesperrt (V13); geloescht wird der eigene Satz daneben.
        int zeile = art == KatalogBrowserArt.Bhkw ? 1 : 0;
        if (zeile > 0) Zeilenklick.Zeile(cut, zeile);
        string name = cut.Instance.Zeilen[zeile].Bezeichner;

        Loeschknopf(cut, art).Click();

        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains(name, cut.Find(".epos-rueckfrage").TextContent);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal(name, geloescht);
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

        Zeilenklick.Zeile(cut, 1);                       // der eigene Satz "BHKW B"
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
    /// <b>Ohne Zeile gibt es nichts zu löschen — und keinen Knopf dafür</b> (Stufe 3,
    /// V8): Am Platz der Auswahlleiste steht eine leise Zeile, damit die Liste nicht
    /// springt; das Stammblatt sagt, dass keine Zeile gewählt ist. Bis Stufe 2 stand
    /// „Löschen" in der Fußleiste und meldete bei BHKW und Solarkollektoren
    /// „Bitte … auswählen!" — eine Meldung zu einem Knopf, der nichts tun konnte.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher)]
    public void Ohne_Zeile_steht_die_leise_Zeile_statt_der_Handlungen(KatalogBrowserArt art)
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = Array.Empty<Katalogfilterzeile>,
            Detail = _ => null
        };
        var cut = Aufbauen(art, wege: wege);

        Assert.Equal("", cut.Instance.Gewaehlt);
        Assert.Empty(cut.FindAll(".epos-auswahlleiste button"));
        Assert.Equal("Keine Zeile gewählt", cut.Find(".epos-auswahlleiste-leise").TextContent.Trim());
        Assert.Equal("Keine Zeile gewählt.", cut.Find(".epos-stammblatt-leer").TextContent.Trim());
        Assert.Equal("", cut.Instance.Meldung);
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

        // Der Grund steht auch ROT in der Statuszeile neben dem Knopf.
        var status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Contains("Duplizieren", status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);
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
        Assert.Equal("Auslieferungssatz – nur lesen, Duplizieren oder Schloss aufheben erlaubt", schloss!.GetAttribute("title"));
        Assert.Equal(schloss.GetAttribute("title"), schloss.GetAttribute("aria-label"));
        Assert.Equal("", schloss.TextContent.Trim());
        Assert.Null(zeilen[1].QuerySelector(".epos-schloss"));

        // Keine Spalte „Auslieferung" oder „Schreibschutz".
        Assert.DoesNotContain(cut.FindAll(".epos-katalogliste th"),
                              th => th.TextContent.Contains("Auslieferung") || th.TextContent.Contains("Schreibschutz"));
    }

    /// <summary>
    /// <b>Die Fußleiste der Verwaltung</b> (V15, Stufe 3): Speichern · Verwerfen · Füller ·
    /// Neu… · Beenden — der Füller ist die Statuszeile. Duplizieren… und Löschen stehen
    /// in der Auswahlleiste (V8: keine Handlung an zwei Orten).
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
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu...", "Beenden" }, texte);

        var handlungen = cut.FindAll(".epos-auswahlleiste .epos-auswahlleiste-knopf:not(.epos-nur-schmal)")
                            .Select(k => k.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Vergleichen", "Duplizieren...", "Löschen" }, handlungen);

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

        Handlung(cut, "Duplizieren...").Click();

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

        Handlung(cut, "Duplizieren...").Click();
        cut.Find(".epos-ueberlagerung input[type=text]").Input("BHKW B");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(gerufen);
        Assert.True(cut.Instance.Duplizierfrage);
    }

    // =================================================================================
    // Stufe 3 (Konzept Administrationsdialoge): Stammblatt, Auswahlleiste, Kästchen,
    // Vergleich und die Löschsperre der Auslieferung (V3 V6 V8 V9 V12 V13) — Pilot
    // =================================================================================

    /// <summary>
    /// <b>Das Stammblatt statt des Eingabeblocks</b> (V3, V9): Es steht im Rahmen neben
    /// der Liste; sein Kopf nennt Name, Herkunft (die Textspalten der Zeile und „eigener
    /// Satz") und drei Kennzahlen; die bisherigen Katalogfelder stehen geteilt in
    /// „Kenndaten" und „Kosten" — die Investition unter Kosten, die Leistung unter
    /// Kenndaten.
    /// </summary>
    [Fact]
    public void Das_Stammblatt_steht_neben_der_Liste_mit_Kenndaten_und_Kosten()
    {
        var cut = Aufbauen();

        Assert.NotNull(cut.Find(".epos-katalograhmen .epos-katalog-stammblatt .epos-stammblatt"));
        Assert.Equal("Eintrag A", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal("Vaillant · Erdgas E · eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-stammblatt-kennzahl").Count);
        Assert.Empty(cut.FindAll(".epos-stammblatt-schutz"));

        var gruppen = cut.FindAll(".epos-stammblattgruppe");
        Assert.Equal(new[] { "Kenndaten", "Kosten" },
                     gruppen.Select(g => g.QuerySelector(".epos-stammblattgruppe-titel")!.TextContent));

        var profil = Profil(KatalogBrowserArt.Heizkessel);
        string invest = profil.Detailfelder.Single(f => f.Schluessel == KatalogBrowserProfil.FeldInvestitionskosten).Bezeichnung;
        string leistung = profil.Detailfelder.Single(f => f.Schluessel == KatalogBrowserProfil.FeldPtherm).Bezeichnung;
        Assert.Contains(invest, gruppen[1].TextContent);
        Assert.DoesNotContain(invest, gruppen[0].TextContent);
        Assert.Contains(leistung, gruppen[0].TextContent);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz: Löschen weich gesperrt, der Grund im Kopf</b> (V13):
    /// Das Stammblatt trägt das Schloss hinter dem Namen, nennt die Herkunft und sagt in
    /// Worten, dass der Satz nur lesbar ist. „Löschen" ist <c>aria-disabled</c> mit dem
    /// Grund im Kurztext; ein Klick nennt ihn im Warnband — keine Rückfrage, kein
    /// Schreibweg.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_sperrt_Loeschen_weich_und_sagt_es_im_Stammblattkopf()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Loeschen = n => { gerufen = true; return new KatalogSpeicherErgebnis(true, "", n); },
            Duplizieren = (id, n) => new KatalogSpeicherErgebnis(true, "", n)
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Assert.Equal("BHKW A", cut.Instance.Gewaehlt);
        Assert.NotNull(cut.Find(".epos-stammblatt-name .epos-schloss"));
        Assert.Contains("nur lesen", cut.Find(".epos-stammblatt-schutz").TextContent);
        Assert.Contains("Auslieferungssatz", cut.Find(".epos-stammblatt-unter").TextContent);

        var loeschen = Loeschknopf(cut, KatalogBrowserArt.Bhkw);
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.False(loeschen.HasAttribute("disabled"));
        Assert.Contains("Duplizieren", loeschen.GetAttribute("title") ?? "");

        loeschen.Click();

        Assert.False(cut.Instance.Loeschfrage);
        Assert.False(gerufen);
        Assert.Contains("Löschen gesperrt", cut.Instance.Meldung);

        // Der eigene Satz desselben Katalogs bleibt löschbar.
        Zeilenklick.Zeile(cut, 1);
        Assert.Null(Loeschknopf(cut, KatalogBrowserArt.Bhkw).GetAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Die Leertaste setzt das Kästchen der Fokuszeile</b> (V6) — und ein zweites Mal
    /// nimmt sie es wieder weg; die Fokuszeile bleibt dabei stehen.
    /// </summary>
    [Fact]
    public void Die_Leertaste_setzt_das_Kaestchen_der_Fokuszeile()
    {
        var cut = Aufbauen();

        Zeilenklick.Taste(cut, " ");
        Assert.Equal(new[] { "Eintrag A" }, cut.Instance.Kaestchen);
        Assert.Equal("Eintrag A", cut.Instance.Gewaehlt);
        Assert.Equal("1 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());

        Zeilenklick.Taste(cut, " ");
        Assert.Empty(cut.Instance.Kaestchen);
        Assert.Equal("Eintrag A", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());
    }

    /// <summary>
    /// <b>Eine Handlung außerhalb ihrer Zeilenzahl ist weich gesperrt</b> (V8): Ohne
    /// Kästchen wirkt alles auf die Fokuszeile — „Vergleichen" braucht zwei und nennt
    /// den Weg; mit zwei Kästchen ist „Duplizieren…" gesperrt, denn es gilt genau einer
    /// Zeile. Ein Klick meldet den Grund und tut nichts.
    /// </summary>
    [Fact]
    public void Eine_Handlung_ausserhalb_ihrer_Zeilenzahl_ist_weich_gesperrt_und_nennt_den_Grund()
    {
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Duplizieren = (id, n) => new KatalogSpeicherErgebnis(true, "", n)
        };
        var cut = Aufbauen(wege: wege);

        var vergleichen = Handlung(cut, "Vergleichen");
        Assert.Equal("true", vergleichen.GetAttribute("aria-disabled"));
        Assert.Contains("zwei", vergleichen.GetAttribute("title") ?? "");
        vergleichen.Click();
        Assert.False(cut.Instance.Vergleicht);
        Assert.Contains("zwei", cut.Instance.Meldung);
        Assert.Null(Handlung(cut, "Duplizieren...").GetAttribute("aria-disabled"));

        Kaestchen(cut, 0);
        Kaestchen(cut, 1);

        var duplizieren = Handlung(cut, "Duplizieren...");
        Assert.Equal("true", duplizieren.GetAttribute("aria-disabled"));
        Assert.Contains("genau eine", duplizieren.GetAttribute("title") ?? "");
        duplizieren.Click();
        Assert.False(cut.Instance.Duplizierfrage);
        Assert.Null(Handlung(cut, "Vergleichen").GetAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Vergleichen zeigt die gewählten Sätze im Stammblatt</b> (V12): eine Spalte je
    /// Satz, eine Zeile je Katalogfeld (ohne den Bezeichner — er steht im Kopf) und die
    /// Herkunft; „Vergleichen" steht gedrückt. „‹ Stammblatt von …" führt zur Fokuszeile
    /// zurück, die Kästchen bleiben.
    /// </summary>
    [Fact]
    public void Zwei_Kaestchen_vergleichen_im_Stammblatt()
    {
        var cut = Aufbauen();

        Kaestchen(cut, 0);
        Kaestchen(cut, 1);
        Assert.Equal(new[] { "Eintrag A", "Eintrag B" }, cut.Instance.Kaestchen);
        Assert.Equal("2 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());

        Handlung(cut, "Vergleichen").Click();

        Assert.True(cut.Instance.Vergleicht);
        Assert.Equal("true", Handlung(cut, "Vergleichen").GetAttribute("aria-pressed"));
        Assert.Equal(new[] { "Parameter", "Eintrag A", "Eintrag B" },
                     cut.FindAll(".epos-stammblatt .epos-vergleich thead th").Select(th => th.TextContent.Trim()));
        Assert.Empty(cut.FindAll(".epos-stammblattgruppe"));
        Assert.DoesNotContain(cut.Instance.Vergleichszeilen, z => z.Name.StartsWith("Name", StringComparison.Ordinal));
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Herkunft" && !z.Abweichend);
        Assert.Contains("nur lesbar", cut.Find(".epos-stammblatt-hinweis").TextContent);

        cut.Find(".epos-stammblatt-zurueck").Click();

        Assert.False(cut.Instance.Vergleicht);
        Assert.Equal("Eintrag A", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal(2, cut.Instance.Kaestchen.Count);
    }

    /// <summary>
    /// <b>Löschen wirkt auf die gewählten Zeilen und lässt Auslieferungssätze stehen</b>
    /// (AD-Q9, V13): Die Rückfrage nennt, was gelöscht wird und was stehen bleibt;
    /// geschrieben wird je Zeile, nur die eigenen; die Statuszeile nennt beide Zahlen,
    /// und das Kästchen der gelöschten Zeile fällt.
    /// </summary>
    [Fact]
    public void Loeschen_mehrerer_Zeilen_laesst_Auslieferungssaetze_stehen()
    {
        var katalog = Zeilen(KatalogBrowserArt.Bhkw).ToList();
        var geloescht = new List<string>();
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => katalog,
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Loeschen = n =>
            {
                geloescht.Add(n);
                katalog = katalog.Where(z => z.Bezeichner != n).ToList();
                return new KatalogSpeicherErgebnis(true, "", n);
            }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Kaestchen(cut, 0);                                     // BHKW A, Auslieferung
        Kaestchen(cut, 1);                                     // BHKW B, eigener Satz
        Assert.Null(Loeschknopf(cut, KatalogBrowserArt.Bhkw).GetAttribute("aria-disabled"));

        Loeschknopf(cut, KatalogBrowserArt.Bhkw).Click();

        string frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("BHKW B", frage);
        Assert.Contains("Stehen bleiben (Auslieferungssatz): BHKW A", frage);

        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal(new[] { "BHKW B" }, geloescht);
        Assert.Equal("1 gelöscht, 1 stehen geblieben", cut.Instance.Status);
        Assert.Equal(new[] { "BHKW A" }, cut.Instance.Kaestchen);
        Assert.Single(cut.Instance.Zeilen);
    }

    /// <summary>
    /// <b>„Duplizieren…" kopiert die EINE gewählte Zeile</b> — auch wenn sie nicht die
    /// Fokuszeile ist (V8: Sind Kästchen gesetzt, wirkt die Handlung auf sie). Danach ist
    /// die Kopie Fokuszeile und die Kästchen sind leer.
    /// </summary>
    [Fact]
    public void Duplizieren_kopiert_die_gewaehlte_Zeile_auch_neben_der_Fokuszeile()
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

        Assert.Equal("BHKW A", cut.Instance.Gewaehlt);
        Kaestchen(cut, 1);                                     // BHKW B
        Handlung(cut, "Duplizieren...").Click();

        Assert.Equal("BHKW B (Kopie)", cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value"));
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((2, "BHKW B (Kopie)"), gerufen);
        Assert.Equal("BHKW B (Kopie)", cut.Instance.Gewaehlt);
        Assert.Empty(cut.Instance.Kaestchen);
    }

    /// <summary>
    /// <b>„Auswahl aufheben" steht nur bei gesetzten Kästchen</b> und nimmt sie alle weg
    /// (V8); ohne Kästchen steht dort der leise Hinweis, dass es sie gibt. Esc hebt nicht
    /// auf — es bleibt Beenden.
    /// </summary>
    [Fact]
    public void Auswahl_aufheben_steht_nur_bei_gesetzten_Kaestchen()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        Assert.Empty(cut.FindAll(".epos-auswahlleiste-aufheben"));
        Assert.Equal("Kästchen: mehrere wählen", cut.Find(".epos-auswahlleiste-leise").TextContent.Trim());

        Kaestchen(cut, 1);
        cut.Find(".epos-auswahlleiste-aufheben").Click();

        Assert.Empty(cut.Instance.Kaestchen);
        Assert.Empty(cut.FindAll(".epos-auswahlleiste-aufheben"));

        Kaestchen(cut, 0);
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.NotNull(ergebnis);
    }

    /// <summary>
    /// <b>Im schmalen Fenster</b> (V3): „Stammblatt ›" schiebt das Blatt über die Liste,
    /// „‹ Liste" führt zurück; Esc schließt erst das Blatt, dann den Dialog (Konzept 3.3).
    /// Die Knöpfe stehen immer im Markup — ob sie SICHTBAR sind, entscheidet die
    /// Containerabfrage des Rahmens (Katalogprobe, Fälle N01b bis N04b).
    /// </summary>
    [Fact]
    public void Schmal_schiebt_Stammblatt_das_Blatt_und_Esc_schliesst_erst_das_Blatt()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-auswahlleiste .epos-nur-schmal").Click();
        Assert.True(cut.Instance.BlattOffen);
        Assert.Contains("epos-katalog-paar--blatt", cut.Find(".epos-katalog-paar").ClassName ?? "");
        Assert.Empty(cut.FindAll(".epos-auswahlleiste .epos-nur-schmal"));

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.BlattOffen);
        Assert.Null(ergebnis);

        cut.Find(".epos-auswahlleiste .epos-nur-schmal").Click();
        cut.Find(".epos-stammblatt-zurliste").Click();
        Assert.False(cut.Instance.BlattOffen);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.NotNull(ergebnis);
    }

    /// <summary>
    /// <b>Der Fuß des Stammblatts zählt die geänderten Felder</b> (V9) — gegen die Werte
    /// beim Laden; ohne Änderung steht kein Fuß.
    /// </summary>
    [Fact]
    public void Der_Fuss_des_Stammblatts_zaehlt_die_geaenderten_Felder()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-stammblatt-fuss"));

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        Assert.Equal("1 Feld geändert", cut.Find(".epos-stammblatt-hinweis").TextContent.Trim());

        cut.FindAll("input[inputmode=decimal]")[1].Input("7");
        Assert.Equal("2 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent.Trim());
    }

    // =================================================================================
    // Helfer: die Knopfstellen je Ausprägung
    // =================================================================================

    /// <summary>Setzt das Kästchen der Zeile <paramref name="index"/> um (V6).</summary>
    private static void Kaestchen(IRenderedComponent<KatalogBrowserDialog> cut, int index)
        => cut.FindAll("td.epos-spalte-kaestchen input")[index].Change(true);

    /// <summary>
    /// Mit Speicherweg stehen „Speichern" und „Verwerfen" (AD-Q6) vor dem Füller; ohne
    /// ihn fehlen beide, und alles rückt um zwei.
    /// </summary>
    private static int Versatz(KatalogBrowserArt art) =>
        Profil(art).HatSpeicherweg ? 2 : 0;

    private static AngleSharp.Dom.IElement Neuknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art)];

    /// <summary>„Löschen" steht seit Stufe 3 in der Auswahlleiste (V8).</summary>
    private static AngleSharp.Dom.IElement Loeschknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        Handlung(cut, "Löschen");

    /// <summary>Ein Knopf der Auswahlleiste nach seiner Beschriftung.</summary>
    private static AngleSharp.Dom.IElement Handlung(
        IRenderedComponent<KatalogBrowserDialog> cut, string text) =>
        cut.FindAll(".epos-auswahlleiste button").First(k => k.TextContent.Trim() == text);

    // =================================================================================
    // „Import…" als Zweitweg (Konzept Administrationsdialoge 7.1 d)
    // =================================================================================

    /// <summary>Der kleinste Parametersatz des <c>KatalogImportDialog</c>: Art, Profil, Texte.</summary>
    private static IReadOnlyDictionary<string, object> ImportGaben(KatalogImportArt art)
        => new Dictionary<string, object>
        {
            ["Art"] = art,
            ["ProfilVorgabe"] = KatalogImportProfil.Finde(art, EPOS.UI.Dialoge.Import.Texte.Zu),
            ["Meldungstext"] = new Func<SpeicherEngine.PruefMeldung, string>(EPOS.UI.Dialoge.Import.Texte.Zu),
            ["Fortschrittstext"] = new Func<ImportFortschritt, string>(EPOS.UI.Dialoge.Import.Texte.Zu)
        };

    /// <summary>Wege mit „Import…" über einer veränderlichen Zeilenliste.</summary>
    private static KatalogBrowserWege ImportWege(List<Katalogfilterzeile> zeilen, Action? gelesen = null)
        => new KatalogBrowserWege
        {
            Katalogzeilen = () => { gelesen?.Invoke(); return zeilen.ToArray(); },
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            ImportGaben = () => ImportGaben(KatalogImportArt.Heizkessel)
        };

    [Fact]
    public void Ohne_Importweg_steht_kein_Importknopf()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(".epos-importknopf"));
    }

    [Fact]
    public void Import_oeffnet_den_Herstellerimport_als_Ueberlagerung_mit_dessen_Titel_und_Kreuz()
    {
        var cut = Aufbauen(wege: ImportWege(Zeilen(KatalogBrowserArt.Heizkessel).ToList()));

        var knopf = cut.Find(".epos-importknopf");
        Assert.Equal("Import…", knopf.TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        knopf.Click();

        Assert.True(cut.Instance.ImportOffen);
        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        // DAS KREUZ STEHT BEIM TITEL: Titel und Kreuz traegt der Importdialog, die
        // Ueberlagerung hat keinen Kopf - genau ein Kreuz im Bereich.
        Assert.Null(ueberlagerung.QuerySelector(".epos-ueberlagerung-kopf"));
        Assert.NotNull(ueberlagerung.QuerySelector(".epos-katalogimport .epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-zu"));
    }

    /// <summary>
    /// <b>Nach einer Übernahme sind die neuen Sätze die Auswahl</b> (V14, Konzept 7.1 d):
    /// Die Liste liest neu, die Kästchen der neuen Sätze stehen — und nur ihre, ein Kästchen
    /// von vorher fällt —, die Auswahlleiste sagt „2 gewählt", der erste neue Satz ist
    /// Fokuszeile und steht im Stammblatt, die Statuszeile nennt die Zahl.
    /// </summary>
    [Fact]
    public async Task Nach_einem_Import_sind_alle_neuen_Saetze_gewaehlt_und_der_erste_im_Fokus()
    {
        var zeilen = Zeilen(KatalogBrowserArt.Heizkessel).ToList();
        var cut = Aufbauen(wege: ImportWege(zeilen));
        Kaestchen(cut, 0);                                  // ein Kästchen von vorher
        cut.Find(".epos-importknopf").Click();

        // Der Import schreibt zwei Saetze und schliesst mit "geschrieben".
        zeilen.Add(new Katalogfilterzeile(7, "Kessel Neu 1")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel Neu 1"));
        zeilen.Add(new Katalogfilterzeile(8, "Kessel Neu 2")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel Neu 2"));
        var import = cut.FindComponent<EPOS.UI.Dialoge.Import.KatalogImportDialog>();
        await cut.InvokeAsync(() => import.Instance.Geschlossen.InvokeAsync(true));

        Assert.False(cut.Instance.ImportOffen);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(4, cut.Instance.Zeilen.Count);
        Assert.Equal(new[] { "Kessel Neu 1", "Kessel Neu 2" }, cut.Instance.Kaestchen);
        Assert.Equal("Kessel Neu 1", cut.Instance.Gewaehlt);
        Assert.Equal("2 gewählt", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());
        Assert.Equal("Kessel Neu 1", cut.Find(".epos-stammblatt-nametext").TextContent);
        Assert.Equal(new[] { false, false, true, true },
                     cut.FindAll("tbody td.epos-spalte-kaestchen input").Select(k => k.HasAttribute("checked")));
        Assert.Equal("2 Sätze übernommen und gewählt.", cut.Instance.Status);

        // Die Handlungen der Auswahlleiste wirken auf die zwei: Vergleichen ist frei.
        Assert.Null(Handlung(cut, "Vergleichen").GetAttribute("aria-disabled"));
    }

    /// <summary>
    /// <b>Ein einzelner neuer Satz ist als Fokuszeile allein die Wahl</b> („Zeile ist Wahl",
    /// wie nach dem Einlesen einer Klimaregion): kein Kästchen, die Auswahlleiste nennt
    /// ihn beim Namen, Kästchen von vorher fallen.
    /// </summary>
    [Fact]
    public async Task Nach_einem_Import_mit_einem_Satz_ist_er_Fokuszeile_ohne_Kaestchen()
    {
        var zeilen = Zeilen(KatalogBrowserArt.Heizkessel).ToList();
        var cut = Aufbauen(wege: ImportWege(zeilen));
        Kaestchen(cut, 0);
        Kaestchen(cut, 1);
        cut.Find(".epos-importknopf").Click();

        zeilen.Add(new Katalogfilterzeile(7, "Kessel Neu 1")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel Neu 1"));
        var import = cut.FindComponent<EPOS.UI.Dialoge.Import.KatalogImportDialog>();
        await cut.InvokeAsync(() => import.Instance.Geschlossen.InvokeAsync(true));

        Assert.Empty(cut.Instance.Kaestchen);
        Assert.Equal("Kessel Neu 1", cut.Instance.Gewaehlt);
        Assert.Equal("Kessel Neu 1", cut.Find(".epos-auswahlleiste-was").TextContent.Trim());
        Assert.All(cut.FindAll("tbody td.epos-spalte-kaestchen input"), k => Assert.False(k.HasAttribute("checked")));
        Assert.Equal("„Kessel Neu 1“ eingelesen.", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Nach dem Import rollt die Liste zur neuen Fokuszeile</b> — über denselben Aufruf
    /// wie ein Tastenschritt (<c>zeileZeigen</c> mit ihrer Stelle und dem Zeilenmaß 46).
    /// Das Öffnen rollt nicht; in einer Liste von Tausenden stünde der neue Satz sonst
    /// gewählt, aber außer Sicht.
    /// </summary>
    [Fact]
    public async Task Nach_einem_Import_rollt_die_Liste_zur_neuen_Fokuszeile()
    {
        var modul = JSInterop.SetupModule(Katalogliste.MODUL);
        modul.SetupVoid("anmelden", _ => true);
        modul.SetupVoid("zeileZeigen", _ => true);

        var zeilen = Zeilen(KatalogBrowserArt.Heizkessel).ToList();
        var cut = Aufbauen(wege: ImportWege(zeilen));
        cut.WaitForAssertion(() => Assert.Single(modul.Invocations, i => i.Identifier == "anmelden"));
        cut.Find(".epos-importknopf").Click();

        zeilen.Add(new Katalogfilterzeile(7, "Kessel Neu 1")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel Neu 1"));
        zeilen.Add(new Katalogfilterzeile(8, "Kessel Neu 2")
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel Neu 2"));
        Assert.DoesNotContain(modul.Invocations, i => i.Identifier == "zeileZeigen");

        var import = cut.FindComponent<EPOS.UI.Dialoge.Import.KatalogImportDialog>();
        await cut.InvokeAsync(() => import.Instance.Geschlossen.InvokeAsync(true));

        // Die Stelle der Fokuszeile in der gezeichneten Liste.
        int stelle = cut.FindAll("tbody tr").ToList()
                        .FindIndex(z => z.TextContent.Contains("Kessel Neu 1", StringComparison.Ordinal));
        Assert.True(stelle >= 0);

        cut.WaitForAssertion(() =>
        {
            var zeigen = modul.Invocations.Where(i => i.Identifier == "zeileZeigen").ToList();
            Assert.Single(zeigen);
            Assert.Equal(stelle, zeigen[0].Arguments[1]);
            Assert.Equal(46f, zeigen[0].Arguments[2]);
        });
    }

    [Fact]
    public void Kreuz_und_Esc_des_Imports_schliessen_nur_die_Ueberlagerung()
    {
        int gelesen = 0;
        bool zu = false;
        var cut = Aufbauen(wege: ImportWege(Zeilen(KatalogBrowserArt.Heizkessel).ToList(), () => gelesen++),
                           geschlossen: _ => zu = true);

        cut.Find(".epos-importknopf").Click();
        int vorher = gelesen;
        cut.Find(".epos-ueberlagerung .epos-dialog-zu").Click();

        Assert.False(cut.Instance.ImportOffen);
        Assert.False(zu);
        Assert.Equal(vorher, gelesen);          // nichts geschrieben: kein Neulesen

        // Esc im Import schliesst den Import - die Verwaltung bleibt offen.
        cut.Find(".epos-importknopf").Click();
        cut.Find(".epos-katalogimport").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(cut.Instance.ImportOffen);
        Assert.False(zu);
    }

    // =================================================================================
    // Der Hilfe-Assistent (Welle #456, KI-D-Q11)
    // =================================================================================

    /// <summary>
    /// <b>Jede Ausprägung meldet sich unter ihrem Navigationsschlüssel an</b> — mit genau
    /// den Feldern ihres Profils und dem Satz — und beim Schließen wieder ab.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, KiMaskennamen.HEIZKESSEL_ADMIN)]
    [InlineData(KatalogBrowserArt.Bhkw, KiMaskennamen.BHKW_ADMIN)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, KiMaskennamen.SOLARKOLLEKTOREN_ADMIN)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, KiMaskennamen.PUFFERSPEICHER_ADMIN)]
    public void Jede_Auspraegung_meldet_sich_unter_ihrem_Navigationsschluessel_an_und_ab(
        KatalogBrowserArt art, string maske)
    {
        var cut = Aufbauen(art);

        Assert.True(KiMaskenbruecke.IstAngemeldet(maske));
        Assert.Equal(maske, KiMaskenbruecke.AktiveMaske());
        Assert.Equal(Profil(art).Detailfelder.Count + 1, KiMaskenbruecke.Lesen(maske).Count);

        // Der Infoknopf gibt dem Assistenten den Namen der Verwaltung mit.
        Assert.Contains(cut.FindComponents<InfoKnopf>(), k => k.Instance.Dialogname == Profil(art).Titel);

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(maske));
    }

    /// <summary>
    /// <b>Der Assistent liest und setzt einen Wert des gewählten Satzes</b> — beim
    /// Heizkessel, BHKW und Kollektor den Vorlauf, beim Pufferspeicher (der keinen führt)
    /// das Volumen. Der Wert landet im Feldsatz des Stammblatts, „Speichern" wird frei.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, KiMaskennamen.HEIZKESSEL_ADMIN, "vorlauf", 55)]
    [InlineData(KatalogBrowserArt.Bhkw, KiMaskennamen.BHKW_ADMIN, "vorlauf", 85)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, KiMaskennamen.SOLARKOLLEKTOREN_ADMIN, "vorlauf", 60)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, KiMaskennamen.PUFFERSPEICHER_ADMIN, "volumen", 1500)]
    public void Der_Assistent_liest_und_setzt_einen_Wert_und_Speichern_wird_frei(
        KatalogBrowserArt art, string maske, string feld, int wert)
    {
        var cut = Aufbauen(art);

        // Das BHKW beginnt auf einem Auslieferungssatz - der Assistent waehlt den eigenen.
        if (cut.Instance.Auslieferungssatz)
            KiSatzSetzen(cut, maske, "BHKW B");
        Assert.False(cut.Instance.Auslieferungssatz);

        WindowsFormsApplication1.KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(maske, feld)!;
        Assert.NotNull(zugang);
        Assert.True(zugang.Setzbar);
        Assert.Equal(70, zugang.Lesen());

        KiFeldumsetzung u = KiFeldwandler.Wandle(zugang, wert.ToString(CultureInfo.InvariantCulture));
        Assert.True(u.Ok, u.Grund);
        zugang.Setzen(u.Wert);
        KiMaskenbruecke.Haken(maske).Auffrischung();
        cut.Render();

        Assert.Equal(wert, zugang.Lesen());
        Assert.False(cut.FindAll(".epos-leiste .epos-knopf")[0].HasAttribute("disabled"));

        // Beim BHKW laeuft die Summe der Wirkungsgrade mit (BeiFeldAenderung) - dort
        // zaehlt der Fuss zwei Felder, sonst eins.
        Assert.EndsWith("geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);
    }

    /// <summary>
    /// <b>Der Speicherhaken schreibt über den Weg des Knopfes</b> — dieselben Felder, und
    /// die Statuszeile meldet es; die Prüfung ist die des Knopfes.
    /// </summary>
    [Fact]
    public async Task Der_Speicherhaken_schreibt_ueber_den_Weg_des_Knopfes()
    {
        IReadOnlyList<BrowserFeldwert>? geschrieben = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, f, _) => { geschrieben = f; return new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n); }
        };
        var cut = Aufbauen(wege: wege);
        const string maske = KiMaskennamen.HEIZKESSEL_ADMIN;

        KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);

        // Ohne Aenderung gibt es nichts zu speichern - benannt, nicht still.
        Assert.Equal(KiKern.KiStatus.Abgelehnt, (await haken.Speichern()).Status);

        WindowsFormsApplication1.KiFeldzugang vorlauf = KiMaskenbruecke.Feldzugang(maske, "vorlauf")!;
        vorlauf.Setzen(55);
        cut.Render();
        Assert.Equal("", haken.Befund());

        KiKern.KiErgebnis ok = await haken.Speichern();

        Assert.Equal(KiKern.KiStatus.Ausgefuehrt, ok.Status);
        Assert.NotNull(geschrieben);
        Assert.Equal("55", geschrieben!.First(f => f.Schluessel == KatalogBrowserProfil.FeldVorlauf).Wert);
        Assert.Equal("Datensatz gespeichert", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz ist für den Assistenten geschützt</b> (AD-Q11): Schon
    /// <c>feld_setzen</c> lehnt ab und nennt „Duplizieren…", ebenso der Speicherweg. Die
    /// WAHL des Satzes bleibt frei — der eigene Satz lässt sich danach setzen.
    /// </summary>
    [Fact]
    public async Task Ein_Auslieferungssatz_lehnt_ab_und_nennt_den_Weg_die_Satzwahl_bleibt_frei()
    {
        Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
        Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
        try
        {
            var cut = Aufbauen(KatalogBrowserArt.Bhkw);
            const string maske = KiMaskennamen.BHKW_ADMIN;
            Assert.True(cut.Instance.Auslieferungssatz);

            KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
            Assert.True(haken.IstSchreibgeschuetzt());
            Assert.Contains("Duplizieren", haken.Schutzgrund());

            string? grund = Vorbedingung("feld_setzen", maske, ("feld", "vorlauf"), ("wert", "85"));
            Assert.NotNull(grund);
            Assert.Contains("Duplizieren", grund);

            KiKern.KiErgebnis gespeichert = await haken.Speichern();
            Assert.Equal(KiKern.KiStatus.Abgelehnt, gespeichert.Status);

            // Die Satzwahl geht durch, und der eigene Satz ist setzbar.
            Assert.Null(Vorbedingung("feld_setzen", maske, ("feld", "satz"), ("wert", "BHKW B")));
            KiSatzSetzen(cut, maske, "BHKW B");
            Assert.Equal("BHKW B", cut.Instance.Gewaehlt);
            Assert.False(haken.IstSchreibgeschuetzt());
            Assert.Null(Vorbedingung("feld_setzen", maske, ("feld", "vorlauf"), ("wert", "85")));
        }
        finally
        {
            Schreibnaht.Schreibrecht = schreibrechtVorher;
        }
    }

    /// <summary>
    /// <b>Im Lesemodus des Wirts</b> ist die Verwaltung für den Assistenten geschützt —
    /// mit dem eigenen Grund, nicht dem des Auslieferungssatzes.
    /// </summary>
    [Fact]
    public async Task Im_Lesemodus_ist_die_Verwaltung_geschuetzt()
    {
        var cut = Aufbauen(KatalogBrowserArt.Pufferspeicher, nurLesen: true);
        const string maske = KiMaskennamen.PUFFERSPEICHER_ADMIN;

        KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
        Assert.True(haken.IstSchreibgeschuetzt());
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KI_DLG_KBROW_NURLESEN, haken.Schutzgrund());
        Assert.Equal(KiKern.KiStatus.Abgelehnt, (await haken.Speichern()).Status);
        Assert.NotNull(cut.Instance);
    }

    /// <summary>
    /// <b>„satz" wechselt die Zeile</b> wie ein Klick — und ungespeicherte Änderungen
    /// halten den Wechsel an: BENANNT, mit dem Text des Warnbands.
    /// </summary>
    [Fact]
    public void Der_Satz_wechselt_die_Zeile_und_Aenderungen_halten_den_Wechsel_an()
    {
        var cut = Aufbauen();
        const string maske = KiMaskennamen.HEIZKESSEL_ADMIN;

        WindowsFormsApplication1.KiFeldzugang satz = KiMaskenbruecke.Feldzugang(maske, "satz")!;
        Assert.True(satz.IstWahl);
        Assert.Equal(2, satz.Wahleintraege().Count);
        Assert.Equal("Eintrag A", satz.Lesen());

        KiSatzSetzen(cut, maske, "Eintrag B");
        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);

        KiMaskenbruecke.Feldzugang(maske, "vorlauf")!.Setzen(55);
        cut.Render();

        var fehler = Assert.Throws<InvalidOperationException>(() => satz.Setzen("Eintrag A"));
        Assert.Contains("Speichern", fehler.Message);
        cut.Render();
        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);
        Assert.Contains("Speichern", cut.Instance.Meldung);
    }

    /// <summary>Setzt den Satz über die Brücke — der Weg des Assistenten.</summary>
    private static void KiSatzSetzen(IRenderedComponent<KatalogBrowserDialog> cut, string maske, string name)
    {
        WindowsFormsApplication1.KiFeldzugang satz = KiMaskenbruecke.Feldzugang(maske, "satz")!;
        KiFeldumsetzung u = KiFeldwandler.Wandle(satz, name);
        Assert.True(u.Ok, u.Grund);
        satz.Setzen(u.Wert);
        cut.Render();
    }

    /// <summary>Die Vorbedingung einer Formularaktion — die Stelle, die ablehnt oder durchlässt.</summary>
    private static string? Vorbedingung(string aktion, string maske, params (string Name, string Wert)[] werte)
    {
        var parameter = new Dictionary<string, object?> { ["maske"] = maske };
        foreach (var (name, wert) in werte) parameter[name] = wert;

        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiKern.KiPruefErgebnis p = KiKern.KiPruefung.Pruefe(schicht.Register, aktion, parameter);
        Assert.True(p.Gueltig, p.FehlerText());

        return schicht.Register.Finde(aktion)!.Vorbedingung!(p.Aufruf!);
    }

    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss aufheben</b> (AD-Q15): Die Handlung steht zwischen „Duplizieren…" und
    /// „Löschen", heißt beim Auslieferungssatz „Schloss aufheben…", fragt mit „Nein" als
    /// Vorgabe und schaltet erst nach dem „Ja". Danach liest der Dialog neu: kein Schloss
    /// mehr, das Band „Schloss aufgehoben" im Stammblatt, „Speichern" frei.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_nach_Rueckfrage_gibt_Speichern_frei()
    {
        var schloss = new Schlosspruefung(1);
        string? gespeichert = null;
        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => schloss.Markieren(Zeilen(KatalogBrowserArt.Bhkw)),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Speichern = (n, _, __) => { gespeichert = n; return new KatalogSpeicherErgebnis(true, "gespeichert", n); },
            Duplizieren = (id, name) => new KatalogSpeicherErgebnis(true, "", name),
            Schloss = schloss.Weg()
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Assert.True(cut.Instance.Auslieferungssatz);
        Assert.Equal(new[] { "Vergleichen", "Duplizieren...", "Schloss aufheben...", "Löschen" },
                     Schlosspruefung.Handlungen(cut));

        Schlosspruefung.Knopf(cut).Click();
        Assert.True(cut.Instance.Schlossfrage);
        Assert.StartsWith("Schloss von „BHKW A“ aufheben?", Schlosspruefung.Frage(cut));
        Assert.Empty(schloss.Aufrufe);                    // erst das „Ja" schreibt

        Schlosspruefung.Ja(cut);

        Assert.False(cut.Instance.Schlossfrage);
        Assert.Equal(new[] { 1 }, schloss.Aufrufe.Single().Ids);
        Assert.False(schloss.Aufrufe.Single().Gesperrt);
        Assert.False(cut.Instance.Auslieferungssatz);
        Assert.Equal("Schloss von „BHKW A“ aufgehoben.", cut.Instance.Status);
        Assert.True(Schlosspruefung.Band(cut));
        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.Null(speichern.GetAttribute("aria-disabled"));
        speichern.Click();
        Assert.Equal("BHKW A", gespeichert);
    }

    /// <summary>
    /// <b>Das Schloss setzen</b> und die harte Sperre: Ein eigener Satz wird nach Rückfrage
    /// zum Auslieferungssatz; im Lesemodus des Dialogs oder der Lizenz ist die Handlung
    /// <c>disabled</c>.
    /// </summary>
    [Fact]
    public void Schloss_setzen_und_harte_Sperre_im_Lesemodus()
    {
        var schloss = new Schlosspruefung(1);
        KatalogBrowserWege Wege(bool lizenzLesemodus) => new()
        {
            Katalogzeilen = () => schloss.Markieren(Zeilen(KatalogBrowserArt.Bhkw)),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Schloss = schloss.Weg(lizenzLesemodus)
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: Wege(false));

        Zeilenklick.Zeile(cut, 1);                       // "BHKW B", eigener Satz
        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));
        Schlosspruefung.Knopf(cut).Click();
        Assert.StartsWith("„BHKW B“ als Auslieferungssatz sperren?", Schlosspruefung.Frage(cut));
        Schlosspruefung.Ja(cut);

        Assert.True(schloss.Aufrufe.Single().Gesperrt);
        Assert.True(cut.Instance.Auslieferungssatz);
        Assert.False(Schlosspruefung.Band(cut));
        Assert.Equal("Schloss von „BHKW B“ gesetzt.", cut.Instance.Status);

        Assert.True(Schlosspruefung.Knopf(Aufbauen(KatalogBrowserArt.Bhkw, nurLesen: true, wege: Wege(false)))
                                   .HasAttribute("disabled"));
        Assert.True(Schlosspruefung.Knopf(Aufbauen(KatalogBrowserArt.Bhkw, wege: Wege(true)))
                                   .HasAttribute("disabled"));
    }
}
