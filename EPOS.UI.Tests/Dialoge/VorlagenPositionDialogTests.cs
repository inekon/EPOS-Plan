using Bunit;
using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Feldkarten-Abgleich und Verhalten des Zeileneditors (iU9-W1.1).
/// Soll ist die Feldkarte von <c>Form_VorlagenPosition</c>: 5 Felder
/// (Bezeichnung, Kostenart, Erlös-Schalter, Empfehlung von, bis) plus Kopftitel
/// und OK/Abbrechen.
/// </summary>
public class VorlagenPositionDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Kostenarten =
    {
        (0, "kapitalgebunden"),
        (1, "bedarfsgebunden"),
        (2, "betriebsgebunden"),
        (3, "sonstige"),
        (4, "Zuschuss")
    };

    public VorlagenPositionDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VorlagenPositionDialog> Aufbauen(
        Action<VorlagenPositionErgebnis?> beimSchliessen,
        string bezeichnung = "Montage",
        int? kostenart = 1,
        bool istErloes = false,
        double? von = null,
        double? bis = null)
    {
        return Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Bezeichnung, bezeichnung)
            .Add(x => x.KostenartId, kostenart)
            .Add(x => x.IstErloes, istErloes)
            .Add(x => x.EmpfehlungVon, von)
            .Add(x => x.EmpfehlungBis, bis)
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen(_ => { });

        // 1 Textfeld + 2 Zahlenfelder = 3 <input type=text>, 1 Schalter, 1 Auswahl.
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        Assert.Single(cut.FindAll("input[type=checkbox]"));
        Assert.Single(cut.FindAll("select"));
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal("Position bearbeiten", cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Bezeichnung:", texte[0].TextContent);
        Assert.Equal("Kostenart:", texte[1].TextContent);
        Assert.Equal("Erlös/Zuschuss (negativer Ausweis)", texte[2].TextContent);
        Assert.Equal("Empfehlung von/bis:", texte[3].TextContent);
        Assert.Equal("bis", texte[4].TextContent);
    }

    [Fact]
    public void Die_Kostenartenliste_kommt_von_aussen()
    {
        var cut = Aufbauen(_ => { });

        var optionen = cut.FindAll("option");
        Assert.Equal(5, optionen.Count);
        Assert.Equal("kapitalgebunden", optionen[0].TextContent);
        Assert.Equal("Zuschuss", optionen[4].TextContent);
    }

    [Fact]
    public void Die_Vorbelegung_steht_in_den_Feldern()
    {
        var cut = Aufbauen(_ => { }, bezeichnung: "Montage", kostenart: 2,
                           istErloes: true, von: 1.5, bis: 3);

        var felder = cut.FindAll("input[type=text]");
        Assert.Equal("Montage", felder[0].GetAttribute("value"));
        Assert.Equal("1,5", felder[1].GetAttribute("value"));
        Assert.Equal("3", felder[2].GetAttribute("value"));
        Assert.Equal(2, cut.Instance.Kostenart);
        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void Eine_unbekannte_Kostenart_faellt_auf_sonstige_zurueck()
    {
        // SetControls: index >= 0 ? index : 3.
        var cut = Aufbauen(_ => { }, kostenart: null);

        Assert.Equal(3, cut.Instance.Kostenart);
    }

    [Fact]
    public void OK_liefert_die_eingegebenen_Werte()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, kostenart: 0);

        cut.FindAll("input[type=text]")[0].Input("  Wartung  ");
        cut.Find("select").Change("4");
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("input[type=text]")[1].Input("2,5");
        cut.FindAll("input[type=text]")[2].Input("7.5");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal("Wartung", ergebnis!.Bezeichnung);
        Assert.Equal(4, ergebnis.KostenartId);
        Assert.True(ergebnis.IstErloes);
        Assert.Equal(2.5, ergebnis.EmpfehlungVon);
        Assert.Equal(7.5, ergebnis.EmpfehlungBis);
    }

    [Fact]
    public void Eine_leere_Empfehlung_bleibt_leer()
    {
        // Program.ZahlPruefen(..., leerErlaubt: true) - NULL heisst "nicht gepflegt".
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, von: 4, bis: 9);

        cut.FindAll("input[type=text]")[1].Input("");
        cut.FindAll("input[type=text]")[2].Input("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.EmpfehlungVon);
        Assert.Null(ergebnis.EmpfehlungBis);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_faerbt_das_Feld_und_meldet_sie_nicht()
    {
        // A-8 aus B5b: Hausregel statt MessageBox von Program.ZahlPruefen.
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e, von: 4);

        cut.FindAll("input[type=text]")[1].Input("vier");

        Assert.Contains("epos-fehleingabe", cut.FindAll("input[type=text]")[1].ClassName);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(4, ergebnis!.EmpfehlungVon);
    }

    [Fact]
    public void OK_ohne_Bezeichnung_meldet_und_haelt_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(_ => geschlossen = true);

        cut.FindAll("input[type=text]")[0].Input("   ");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.False(geschlossen);
        Assert.Equal("Bitte eine Bezeichnung eingeben.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        VorlagenPositionErgebnis? ergebnis = new("x", 0, false, null, null);
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; });

        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_bestaetigt_und_Esc_bricht_ab()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        int gemeldet = 0;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(1, gemeldet);
        Assert.NotNull(ergebnis);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);
        Assert.Null(ergebnis);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Esc.</summary>
    [Fact]
    public void Das_Kreuz_schliesst_wie_Esc()
    {
        VorlagenPositionErgebnis? ergebnis = new("x", 1, false, null, null);
        int gemeldet = 0;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet++; });

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_kein_Kreuz()
    {
        var cut = Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen(_ => { });
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_VorlagenPosition.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Der Feldlauf steht im
    /// <c>Formularraster</c> — Beschriftung neben dem Feld, die beiden
    /// Empfehlungsfelder kurz.
    /// </summary>
    [Fact]
    public void Der_Feldlauf_steht_im_Formularraster()
    {
        var cut = Aufbauen(_ => { });

        Assert.Single(cut.FindAll(".epos-formularraster"));
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count >= 4);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count >= 2);
    }

    // =====================================================================
    // U8 (Stufe S2) — die Positionsart
    // =====================================================================

    private static readonly (int Id, string Text)[] Positionsarten =
    {
        (6, "Modul"), (7, "Abgasanlage"), (9, "MSR"), (27, "Montage")
    };

    /// <summary>
    /// U8: Ohne Nutzungsdauertabelle gibt die Hülle eine leere Liste — dann steht
    /// das Feld gar nicht erst da, und der Dialog ist der von vorher.
    /// </summary>
    [Fact]
    public void Ohne_Positionsarten_bleibt_das_Feld_weg()
    {
        var cut = Aufbauen(_ => { });

        Assert.Single(cut.FindAll("select"));
        Assert.Null(cut.Instance.Positionsart);
    }

    /// <summary>Mit Liste steht eine zweite Klappliste da, samt leerer Zeile.</summary>
    [Fact]
    public void Mit_Positionsarten_steht_eine_zweite_Klappliste_da()
    {
        var cut = Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Positionsarten, Positionsarten)
            .Add(x => x.PositionsartId, 9)
            .Add(x => x.Geschlossen, (VorlagenPositionErgebnis? _) => { }));

        Assert.Equal(2, cut.FindAll("select").Count);
        Assert.Equal(9, cut.Instance.Positionsart);
        // Die leere Zeile plus die vier Arten.
        Assert.Equal(5, cut.FindAll("select")[1].QuerySelectorAll("option").Length);
    }

    /// <summary>
    /// U8: Eine Positionsart, die es in dieser Technik nicht (mehr) gibt, fällt auf
    /// „keine" zurück — dieselbe Vorsicht wie bei der Kostenart.
    /// </summary>
    [Fact]
    public void Eine_unbekannte_Positionsart_faellt_auf_keine_zurueck()
    {
        var cut = Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Positionsarten, Positionsarten)
            .Add(x => x.PositionsartId, 999)
            .Add(x => x.Geschlossen, (VorlagenPositionErgebnis? _) => { }));

        Assert.Null(cut.Instance.Positionsart);
    }

    /// <summary>OK meldet die gewählte Positionsart; die leere Zeile meldet <c>null</c>.</summary>
    [Fact]
    public void OK_meldet_die_gewaehlte_Positionsart()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Bezeichnung, "Abgasanlage")
            .Add(x => x.KostenartId, 0)
            .Add(x => x.Positionsarten, Positionsarten)
            .Add(x => x.PositionsartId, null)
            .Add(x => x.Geschlossen, (VorlagenPositionErgebnis? e) => ergebnis = e));

        cut.FindAll("select")[1].Change("7");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(7, ergebnis!.PositionsartId);

        ergebnis = null;
        cut.FindAll("select")[1].Change("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.PositionsartId);
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Die KOSTENART ist ein
    /// WAHLFELD über ihren Anzeigetext; der Schlüssel ist der Listenplatz der
    /// VDI‑2067-Liste, die der Wirt hereinreicht.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Kostenart()
    {
        var cut = Aufbauen(_ => { });

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.VORLAGENPOSITION));

        KiFeldzugang bezeichnung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.VORLAGENPOSITION, "bezeichnung");
        Assert.Equal("Montage", bezeichnung.Lesen());

        KiFeldzugang art =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.VORLAGENPOSITION, "kostenart");
        Assert.NotNull(art);
        Assert.True(art.Setzbar);

        string zweite = art.Wahleintraege()[1].Text;
        KiFeldumsetzung wahl = KiFeldwandler.Wandle(art, zweite);
        Assert.True(wahl.Ok, wahl.Grund);
        art.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(art.Wahleintraege()[1].Schluessel,
                     Convert.ToString(cut.Instance.Kostenart, CultureInfo.InvariantCulture));
    }

    // =====================================================================
    //  ETAPPE E7c (Schritt E, Entscheid A6): Ersatz und Restwert je Position
    // =====================================================================

    private static readonly (int Id, string Text)[] Kennzeichen = { (1, "ja"), (0, "nein") };

    private IRenderedComponent<VorlagenPositionDialog> MitKennzeichen(
        Action<VorlagenPositionErgebnis?> beimSchliessen, int? ersatz, int? restwert, bool zeigen = true)
    {
        return Render<VorlagenPositionDialog>(p => p
            .Add(x => x.Kostenarten, Kostenarten)
            .Add(x => x.Bezeichnung, "Planung")
            .Add(x => x.KostenartId, 0)
            .Add(x => x.MitKennzeichen, zeigen)
            .Add(x => x.Kennzeichen, Kennzeichen)
            .Add(x => x.KennzeichenLeer, "(leer — wie bisher)")
            .Add(x => x.ErsatzAuswahl, ersatz)
            .Add(x => x.RestwertAuswahl, restwert)
            .Add(x => x.InfoKennzeichen, "Herleitung")
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    /// <summary>Ohne Kennzeichen (Betriebsseite, Datenbank ohne Schritt 107) bleibt der
    /// Dialog der von vorher — eine Klappliste.</summary>
    [Fact]
    public void Ohne_Kennzeichen_bleiben_die_zwei_Klapplisten_weg()
    {
        var cut = MitKennzeichen(_ => { }, 0, 1, zeigen: false);

        Assert.Single(cut.FindAll("select"));
    }

    /// <summary>Mit Kennzeichen stehen zwei weitere Klapplisten da — je der Platzhalter
    /// (leer = wie bisher) und „ja"/„nein"; die Vorbelegung steht in den Feldern.</summary>
    [Fact]
    public void Mit_Kennzeichen_stehen_zwei_Dreiwerte_Klapplisten_da()
    {
        var cut = MitKennzeichen(_ => { }, 0, null);

        Assert.Equal(3, cut.FindAll("select").Count);
        Assert.Equal(3, cut.FindAll("select")[1].QuerySelectorAll("option").Length);
        Assert.Equal(0, cut.Instance.Ersatz);
        Assert.Null(cut.Instance.Restwert);
        Assert.Contains("Herleitung", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>OK meldet beide Wahlen; der Platzhalter meldet <c>null</c> (wie bisher).</summary>
    [Fact]
    public void OK_meldet_die_zwei_Kennzeichen()
    {
        VorlagenPositionErgebnis? ergebnis = null;
        var cut = MitKennzeichen(e => ergebnis = e, null, null);

        cut.FindAll("select")[1].Change("0");
        cut.FindAll("select")[2].Change("1");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(0, ergebnis!.ErsatzAuswahl);
        Assert.Equal(1, ergebnis.RestwertAuswahl);

        ergebnis = null;
        cut.FindAll("select")[1].Change("");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.ErsatzAuswahl);
        Assert.Equal(1, ergebnis.RestwertAuswahl);
    }

    /// <summary>Eine unbekannte Vorbelegung fällt auf leer zurück.</summary>
    [Fact]
    public void Eine_unbekannte_Kennzeichenwahl_faellt_auf_leer_zurueck()
    {
        var cut = MitKennzeichen(_ => { }, 7, 1);

        Assert.Null(cut.Instance.Ersatz);
        Assert.Equal(1, cut.Instance.Restwert);
    }

    /// <summary>Der Assistent sieht beide Kennzeichen als Wahlfelder und setzt sie über
    /// ihren Anzeigetext.</summary>
    [Fact]
    public void Der_Assistent_setzt_das_Ersatzkennzeichen_ueber_seinen_Text()
    {
        var cut = MitKennzeichen(_ => { }, null, null);

        KiFeldzugang ersatz =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.VORLAGENPOSITION, "ersatz_fuehren");
        Assert.NotNull(ersatz);
        Assert.Equal(2, ersatz.Wahleintraege().Count);

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(ersatz, "nein");
        Assert.True(wahl.Ok, wahl.Grund);
        ersatz.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(0, cut.Instance.Ersatz);
    }
}
