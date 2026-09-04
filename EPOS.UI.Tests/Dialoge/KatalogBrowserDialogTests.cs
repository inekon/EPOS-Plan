using System.Globalization;
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
public class KatalogBrowserDialogTests : BunitContext
{
    public KatalogBrowserDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =================================================================================
    // Prüfstand: ein kleiner Katalog je Ausprägung
    // =================================================================================

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static KatalogBrowserProfil Profil(KatalogBrowserArt art) =>
        KatalogBrowserProfil.Finde(art, s => WindowsFormsApplication1.MyResource.Resource
                                                 .ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<BrowserZeile> Zeilen(KatalogBrowserArt art) => art switch
    {
        KatalogBrowserArt.Bhkw => new[]
        {
            new BrowserZeile(1, "BHKW A", "2-G\nBrennstoff: Erdgas\nPtherm: 250 kW\nPel: 250 kW", true),
            new BrowserZeile(2, "BHKW B", "Vaillant\nBrennstoff: Erdgas\nPtherm: 20 kW\nPel: 10 kW", false)
        },
        KatalogBrowserArt.Solarkollektoren => new[]
        {
            new BrowserZeile(1, "Kollektor A", "Junkers\nKollektortyp: Flachkollektor\nAperturfläche: 1,94 m²"),
            new BrowserZeile(2, "Kollektor B", "Vaillant\nKollektortyp: Röhrenkollektor\nAperturfläche: 1 m²")
        },
        _ => new[]
        {
            new BrowserZeile(1, "Eintrag A"),
            new BrowserZeile(2, "Eintrag B")
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
            Liste = (_, __) => Zeilen(art),
            Detail = name => Felder(art, name),
            Existiert = _ => false,
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n),
            Speichern = (n, _, __) => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n),
            IstGeschuetzt = n => n == "BHKW A"
        };

        return Render<KatalogBrowserDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, Profil(art))
            .Add(x => x.NurLesen, nurLesen)
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.FilterEins, new[] { (0, "Alle"), (1, "Gas") })
            .Add(x => x.FilterZwei, new[] { (0, "Alle"), (1, "bis 50 kW") })
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    // =================================================================================
    // Feldbestand je Ausprägung (R-W14-4)
    // =================================================================================

    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, "Administration Heizkessel", 8)]
    [InlineData(KatalogBrowserArt.Bhkw, "BHKW Verwaltung", 8)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, "Administration Solarkollektoren", 8)]
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
    /// Die Filterleiste steht nur, wo der Vorläufer sie hatte: zwei Klapplisten bei
    /// Heizkessel, BHKW und Pufferspeicher, KEINE bei den Solarkollektoren.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, 2)]
    [InlineData(KatalogBrowserArt.Bhkw, 2)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, 0)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, 2)]
    public void Die_Filterleiste_steht_nur_wo_der_Vorlaeufer_sie_hatte(
        KatalogBrowserArt art, int klapplisten)
    {
        var cut = Aufbauen(art);
        Assert.Equal(klapplisten, cut.FindAll(".epos-katalogbrowser-filter select").Count);
    }

    /// <summary>
    /// Die zweite Rasterspalte steht nur bei den beiden Ausprägungen, die im Vorläufer
    /// ein <c>DataGridView</c> hatten (BHKW und Solarkollektoren); Heizkessel und
    /// Pufferspeicher hatten eine <c>ListBox</c> mit nur dem Namen.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, false)]
    [InlineData(KatalogBrowserArt.Bhkw, true)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, true)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, false)]
    public void Die_zweite_Rasterspalte_steht_nur_bei_den_zweispaltigen(
        KatalogBrowserArt art, bool zweispaltig)
    {
        var cut = Aufbauen(art);
        Assert.Equal(zweispaltig, cut.FindAll(".epos-katalogbrowser-eigenschaften").Count > 0);
    }

    /// <summary>
    /// Der Speichern-Knopf steht nur bei Heizkessel und BHKW — die beiden Browser mit
    /// dem Speicherweg vom 18.08.2026.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, 5)]
    [InlineData(KatalogBrowserArt.Bhkw, 5)]
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

        cut.FindAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal("Eintrag B", cut.Instance.Gewaehlt);
        Assert.Equal("Eintrag B", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
    }

    [Fact]
    public void Ein_Filterwechsel_baut_die_Liste_neu()
    {
        int gruppe = -1, stufe = -1;
        var wege = new KatalogBrowserWege
        {
            Liste = (g, s) => { gruppe = g; stufe = s; return Zeilen(KatalogBrowserArt.Heizkessel); },
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name)
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-katalogbrowser-filter select")[0].Change("1");
        Assert.Equal(1, gruppe);

        cut.FindAll(".epos-katalogbrowser-filter select")[1].Change("1");
        Assert.Equal(1, stufe);
    }

    // =================================================================================
    // NurLesen (eigener Abnahmepunkt, R-W14-4)
    // =================================================================================

    /// <summary>
    /// <c>NurLesen</c>: „Neu…", „Bearbeiten…" und „Löschen" sind gesperrt, Liste und
    /// Detailblock bleiben sichtbar — wortgleich
    /// <c>Form_PufferSp_Admin.Form_PufferSp_Admin_Load</c> (Z. 39-44).
    /// </summary>
    [Fact]
    public void NurLesen_sperrt_drei_Knoepfe_und_laesst_Liste_und_Detail_stehen()
    {
        var cut = Aufbauen(KatalogBrowserArt.Pufferspeicher, nurLesen: true);

        var knoepfe = cut.FindAll(".epos-leiste .epos-knopf");
        Assert.Equal(4, knoepfe.Count);
        Assert.True(knoepfe[0].HasAttribute("disabled"));    // Neu...
        Assert.True(knoepfe[1].HasAttribute("disabled"));    // Bearbeiten...
        Assert.True(knoepfe[2].HasAttribute("disabled"));    // Löschen
        Assert.False(knoepfe[3].HasAttribute("disabled"));   // OK

        // Liste und Detailblock stehen unveraendert.
        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal(6, cut.FindAll(".epos-feld input").Count
                      + cut.FindAll(".epos-feld textarea").Count
                      + cut.FindAll(".epos-schalter input").Count);
    }

    [Fact]
    public void Ohne_NurLesen_sind_die_drei_Knoepfe_frei()
    {
        var cut = Aufbauen(KatalogBrowserArt.Pufferspeicher);

        var knoepfe = cut.FindAll(".epos-leiste .epos-knopf");
        Assert.All(knoepfe, k => Assert.False(k.HasAttribute("disabled")));
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
            Liste = (_, __) => Zeilen(art),
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
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
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
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            Loeschen = _ => new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", "")
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        Loeschknopf(cut, KatalogBrowserArt.Bhkw).Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
    }

    // =================================================================================
    // Neu und Bearbeiten
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
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
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
    /// „Bearbeiten…" ohne Auswahl meldet dort, wo der Vorläufer meldete (BHKW und
    /// Solarkollektoren), und schweigt dort, wo er schwieg (Heizkessel,
    /// Pufferspeicher) — bitgleich.
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
            Liste = (_, __) => Array.Empty<BrowserZeile>(),
            Detail = _ => null
        };
        var cut = Aufbauen(art, wege: wege);

        Assert.Equal("", cut.Instance.Gewaehlt);
        Bearbeitenknopf(cut, art).Click();

        Assert.Equal(meldung, cut.Instance.Meldung);
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
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
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

    /// <summary>
    /// Der BHKW-Browser fragt vor dem Überschreiben eines geschützten Satzes — in der
    /// Auslieferungsdatenbank der Regelfall (<c>Form_BHKWAdmin.cs:413-430</c>).
    /// </summary>
    [Fact]
    public void Das_BHKW_fragt_vor_dem_Ueberschreiben_eines_geschuetzten_Satzes()
    {
        bool? uebergangen = null;
        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            IstGeschuetzt = _ => true,
            Speichern = (n, _, u) => { uebergangen = u; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.True(cut.Instance.Schutzfrage);
        Assert.Null(uebergangen);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.True(uebergangen);
    }

    [Fact]
    public void Nein_auf_die_Schutzfrage_schreibt_nicht()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Bhkw),
            Detail = name => Felder(KatalogBrowserArt.Bhkw, name),
            IstGeschuetzt = _ => true,
            Speichern = (n, _, __) => { gerufen = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(KatalogBrowserArt.Bhkw, wege: wege);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(gerufen);
        Assert.False(cut.Instance.Schutzfrage);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_haelt_den_Speicherweg_auf()
    {
        bool gerufen = false;
        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
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
    // OK und Esc
    // =================================================================================

    /// <summary>
    /// <b>Angleichung E-1 (Befund W14-B4).</b> Drei der vier Vorläufer setzten kein
    /// <c>DialogResult</c> und lieferten IMMER <c>false</c>; „OK" heißt jetzt OK.
    /// </summary>
    [Fact]
    public void OK_meldet_bestaetigt_und_den_gewaehlten_Eintrag()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
        Assert.Equal("Eintrag A", ergebnis.Bezeichner);
    }

    /// <summary>
    /// „OK" schreibt vorher offene Änderungen zurück — genau wie
    /// <c>btn_OK_Click</c> beim Heizkessel und beim BHKW (Speicherpaket 18.08.2026).
    /// </summary>
    [Fact]
    public void OK_schreibt_offene_Aenderungen_zurueck()
    {
        bool geschrieben = false;
        BrowserErgebnis? ergebnis = null;
        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (n, _, __) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", n); }
        };
        var cut = Aufbauen(wege: wege, geschlossen: e => ergebnis = e);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();

        Assert.True(geschrieben);
        Assert.NotNull(ergebnis);
    }

    /// <summary>Scheitert das Schreiben, bleibt der Dialog offen — Bestandsverhalten.</summary>
    [Fact]
    public void OK_laesst_den_Dialog_bei_einem_Fehlschlag_offen()
    {
        BrowserErgebnis? ergebnis = null;
        var wege = new KatalogBrowserWege
        {
            Liste = (_, __) => Zeilen(KatalogBrowserArt.Heizkessel),
            Detail = name => Felder(KatalogBrowserArt.Heizkessel, name),
            Speichern = (_, __, ___) => new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", "")
        };
        var cut = Aufbauen(wege: wege, geschlossen: e => ergebnis = e);

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();

        Assert.Null(ergebnis);
        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
    }

    [Fact]
    public void Esc_schliesst_ohne_Bestaetigung()
    {
        BrowserErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Bestaetigt);
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

    // =================================================================================
    // Helfer: die Knopfstellen je Ausprägung
    // =================================================================================

    /// <summary>Ohne Speicherweg fehlt der erste Knopf; alles rückt um eins.</summary>
    private static int Versatz(KatalogBrowserArt art) =>
        Profil(art).HatSpeicherweg ? 1 : 0;

    private static AngleSharp.Dom.IElement Neuknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art)];

    private static AngleSharp.Dom.IElement Bearbeitenknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art) + 1];

    private static AngleSharp.Dom.IElement Loeschknopf(
        IRenderedComponent<KatalogBrowserDialog> cut, KatalogBrowserArt art) =>
        cut.FindAll(".epos-leiste .epos-knopf")[Versatz(art) + 2];
}
