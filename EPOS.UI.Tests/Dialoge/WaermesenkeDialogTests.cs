using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// WaermesenkeDialog (iU9-W10a.7) - der Ersatz fuer Form_Waermesenke.
///
/// <para>Die Maske hatte KEINEN Designer; der Feldbestand steht in BaueOberflaeche
/// (Vermessung §6 f, 35 Steuerelemente in vier Gruppen).</para>
///
/// <para>Die Datenseite ist ein Pruefstand: Er schreibt mit, statt zu schreiben, und
/// laesst sich auf jeden Pruefausgang stellen.</para>
/// </summary>
public class WaermesenkeDialogTests : BunitContext
{
    private const string HEIZKREIS = "Heizkreis";
    private const string PROZESS = "Prozesswaerme";
    private const string P_HEIZUNG = "PufferHeizung";
    private const string P_BRAUCHWASSER = "PufferBrauchwasser";
    private const string P_PROZESS = "PufferProzess";
    private const string P_KOMBI = "PufferKombi";

    private const string BEIDES = "Beides";
    private const string WARMWASSER = "Warmwasser";
    private const string HEIZUNG = "Heizung";

    public WaermesenkeDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Delegatensatz, der mitschreibt statt zu schreiben.</summary>
    private sealed class Pruefstand
    {
        internal List<SenkenzeileDaten> Bestand { get; } = new();
        internal List<SenkenPuffer> Puffer { get; } = new();
        internal List<SenkenPuffer> Kandidaten { get; } = new();
        internal string? Harter { get; set; }
        internal SenkenPruefung Pruefung { get; set; } = new(true, "", false, "");
        internal bool SchreibErgebnis { get; set; } = true;
        internal List<string> Weiche { get; } = new();

        internal IReadOnlyList<SenkenzeileDaten>? Geschrieben;
        internal IReadOnlyList<int>? VerbundGeschrieben;

        internal WaermesenkeDienste Dienste() => new(
            Zeilen: () => Bestand,
            Puffer: () => Puffer,
            VerbundKandidaten: _ => Kandidaten,
            VerbundKapazitaet: (_, m) => 100.0 * (m.Count + 1),
            Position: (z, zweit) => "Lädt als " + (zweit ? "2" : "1") + ". von 2",
            PufferName: id => Puffer.FirstOrDefault(p => p.Id == id)?.Anzeige ?? "",
            ZielAnzeige: z => z,
            HarterBefund: _ => Harter,
            Pruefen: (_, _) => Pruefung,
            Schreiben: (z, v) => { Geschrieben = z; VerbundGeschrieben = v; return SchreibErgebnis; },
            WeicheBefunde: _ => Weiche);
    }

    private static Pruefstand MitPuffern()
    {
        var s = new Pruefstand();
        s.Puffer.Add(new SenkenPuffer(11, "Heizungsspeicher", 1, "Heizung"));
        s.Puffer.Add(new SenkenPuffer(12, "Kombispeicher", 3, "Heizung + Brauchwasser"));
        s.Kandidaten.Add(new SenkenPuffer(11, "Heizungsspeicher", 1, "Heizung"));
        s.Kandidaten.Add(new SenkenPuffer(13, "Zweiter Heizungsspeicher", 1, "Heizung"));
        return s;
    }

    private IRenderedComponent<WaermesenkeDialog> Zeige(
        Pruefstand stand,
        Action<WaermesenkeErgebnis?>? geschlossen = null,
        bool pvModus = false,
        IReadOnlyList<int>? verbund = null,
        Func<string, IReadOnlyDictionary<string, object>>? verwaltung = null)
    {
        return Render<WaermesenkeDialog>(p =>
        {
            p.Add(x => x.Daten, new WaermesenkeDaten
            {
                IdProjekt = 1030,
                IdAnlage = 77,
                IdType = 1,
                AnlagenName = "WP Erdgeschoss",
                PvModus = pvModus,
                VerbundMitglieder = verbund ?? Array.Empty<int>()
            });
            p.Add(x => x.Dienste, stand.Dienste());
            p.Add(x => x.Ziele, new[] { HEIZKREIS, PROZESS, P_HEIZUNG, P_BRAUCHWASSER, P_PROZESS, P_KOMBI });
            p.Add(x => x.Zieltexte, new[] { "Heizkreis", "Prozesswärme", "Puffer Heizung",
                                            "Puffer Brauchwasser", "Puffer Prozess", "Puffer Kombi" });
            p.Add(x => x.Pufferziele, new[] { P_HEIZUNG, P_BRAUCHWASSER, P_PROZESS, P_KOMBI });
            p.Add(x => x.ZielHeizkreis, HEIZKREIS);
            p.Add(x => x.ZielPufferHeizung, P_HEIZUNG);
            p.Add(x => x.Bedarfsarten, new[] { BEIDES, WARMWASSER, HEIZUNG });
            p.Add(x => x.Bedarfsarttexte, new[] { "beides", "nur Warmwasser", "nur Heizwärme" });
            if (verwaltung is not null) p.Add(x => x.VerwaltungGaben, verwaltung);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    // ============================================================ Feldbestand

    /// <summary>
    /// Eine leere Anlage bekommt die RANG-1-INVARIANTE: eine Heizkreis-Zeile
    /// (ZeilenLaden:900). Ohne sie rechnete die Engine mit Protokollwarnung dasselbe.
    /// </summary>
    [Fact]
    public void Eine_leere_Anlage_bekommt_eine_Heizkreiszeile()
    {
        var cut = Zeige(MitPuffern());

        Assert.Single(cut.Instance.Zeilen);
        Assert.Equal(HEIZKREIS, cut.Instance.Zeilen[0].Ziel);
        Assert.Equal("Wärmesenken — WP Erdgeschoss", cut.Find("h1.epos-dialog-titel").TextContent);
    }

    /// <summary>Das Ziel steuert, welche Felder ueberhaupt erscheinen.</summary>
    [Fact]
    public void Das_Ziel_steuert_die_Sichtbarkeit()
    {
        var cut = Zeige(MitPuffern());

        // Heizkreis: Bedarfsart ja, Speicher nein, Ladeverhalten nein.
        Assert.Equal(2, cut.FindAll("select").Count);           // Ziel + Bedarfsart
        Assert.DoesNotContain("Ladeverhalten", cut.Markup);

        // Auf ein Ladeziel umschalten: Speicher ja, Bedarfsart nein, Ladeverhalten ja.
        Ziel(cut, 2);
        Assert.Contains("Ladeverhalten", cut.Markup);
        Assert.Contains("Speicher", cut.Markup);
    }

    // ============================================================ Liste bearbeiten

    /// <summary>
    /// btnHinzu_Click:1186-1203 - Vorbelegung "Rest in den Heizungspuffer": der erste
    /// FREIE Heizungspuffer, sonst der Heizkreis.
    /// </summary>
    [Fact]
    public void Hinzufuegen_waehlt_den_ersten_freien_Heizungspuffer()
    {
        var cut = Zeige(MitPuffern());

        Hinzu(cut);

        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal(P_HEIZUNG, cut.Instance.Zeilen[1].Ziel);
        Assert.Equal(11, cut.Instance.Zeilen[1].IdPuffer);

        // Der naechste bekommt den zweiten - 11 ist jetzt belegt.
        Hinzu(cut);
        Assert.Equal(13, cut.Instance.Zeilen[2].IdPuffer);

        // Danach ist keiner mehr frei: Heizkreis.
        Hinzu(cut);
        Assert.Equal(HEIZKREIS, cut.Instance.Zeilen[3].Ziel);
    }

    /// <summary>Die LETZTE Zeile laesst sich nicht entfernen (Konzept 5.1).</summary>
    [Fact]
    public void Die_letzte_Zeile_bleibt_stehen()
    {
        var cut = Zeige(MitPuffern());

        Entfernen(cut);

        Assert.Single(cut.Instance.Zeilen);
        Assert.Contains("mindestens eine Wärmesenke", cut.Instance.Meldung);
    }

    [Fact]
    public void Entfernen_nimmt_die_gewaehlte_Zeile()
    {
        var cut = Zeige(MitPuffern());
        Hinzu(cut);
        Assert.Equal(2, cut.Instance.Zeilen.Count);

        Entfernen(cut);
        Assert.Single(cut.Instance.Zeilen);
        Assert.Equal(HEIZKREIS, cut.Instance.Zeilen[0].Ziel);
    }

    /// <summary>
    /// Tauschen:1258-1276 - die PV-Sonderprioritaet wandert NICHT mit: Sie gibt es nur
    /// auf Rang 1, und stehen zu lassen, was nicht mehr gilt, waere der Anfang einer
    /// stillen Falschrechnung.
    /// </summary>
    [Fact]
    public void Beim_Tauschen_faellt_die_PV_Prioritaet_ab_Rang_2_weg()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, LadeprioPv = 3 });
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = HEIZKREIS, Bedarfsart = BEIDES });

        var cut = Zeige(stand, pvModus: true);
        Assert.Equal(3, cut.Instance.Zeilen[0].LadeprioPv);

        cut.FindAll("button").First(b => b.TextContent.Contains("nach unten")).Click();

        Assert.Equal(HEIZKREIS, cut.Instance.Zeilen[0].Ziel);
        Assert.Equal(0, cut.Instance.Zeilen[1].LadeprioPv);       // sie ist weg
    }

    // ============================================================ Felder löschen

    /// <summary>
    /// ZeileAusOberflaeche:1082-1107 - Felder, die nicht zum Ziel passen, werden
    /// GELOESCHT. Eine Ladeprioritaet an einer Direktsenke stuende sonst in der
    /// Ladeordnung und wuerde beim naechsten Zielwechsel unbemerkt wieder wirksam.
    /// </summary>
    [Fact]
    public void Der_Zielwechsel_loescht_was_nicht_passt()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten
        {
            Ziel = P_HEIZUNG, IdPuffer = 11, Ladeprio = 4, Ladegrenze = 80,
            Anschlusshoehe = 0.5, Bedarfsart = BEIDES
        });

        var cut = Zeige(stand);
        Assert.Equal(11, cut.Instance.Zeilen[0].IdPuffer);

        Ziel(cut, 0);        // Heizkreis

        SenkenzeileDaten z = cut.Instance.Zeilen[0];
        Assert.Equal(HEIZKREIS, z.Ziel);
        Assert.Equal(0, z.IdPuffer);
        Assert.Equal(0, z.Ladeprio);
        Assert.Null(z.Ladegrenze);
        Assert.Null(z.Anschlusshoehe);
    }

    // ============================================================ Ladeverhalten

    /// <summary>
    /// Die beiden Haken tragen ihre Vorbelegung "70" bzw. "1" (BaueOberflaeche:620/664),
    /// und ohne Haken steht in der Zeile null - "keine eigene" bzw. "nicht gesetzt".
    /// </summary>
    [Fact]
    public void Die_Haken_tragen_ihre_Vorbelegung()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Assert.Null(cut.Instance.Zeilen[0].Ladegrenze);
        Assert.Null(cut.Instance.Zeilen[0].Anschlusshoehe);

        Schalter(cut, "Ladeobergrenze").Change(true);
        Assert.Equal(70.0, cut.Instance.Zeilen[0].Ladegrenze);

        Schalter(cut, "Einspeisehöhe").Change(true);
        Assert.Equal(1.0, cut.Instance.Zeilen[0].Anschlusshoehe);
    }

    /// <summary>Die PV-Sonderprioritaet erscheint nur bei PV-Modus UND auf Rang 1.</summary>
    [Fact]
    public void Die_PV_Prioritaet_erscheint_nur_auf_Rang_eins_im_PV_Modus()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        var ohne = Zeige(stand);
        Assert.DoesNotContain("PV", ohne.Markup);

        var mit = Zeige(stand, pvModus: true);
        Assert.Contains("PV", mit.Markup);
    }

    /// <summary>Die Positionszeile kommt aus dem Kern, nicht aus dem Dialog.</summary>
    [Fact]
    public void Die_Positionszeile_kommt_vom_Dienst()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        Assert.Equal("Lädt als 1. von 2", Zeige(stand).Instance.Position);
    }

    // ============================================================ Parallelverbund

    /// <summary>
    /// Der Verbund haengt konstruktiv an RANG 1 (Befund W10-B25, woertlich): Er
    /// erscheint nur, wenn dort ein Ladeziel MIT Speicher steht.
    /// </summary>
    [Fact]
    public void Der_Verbund_haengt_an_Rang_eins()
    {
        var ohne = Zeige(MitPuffern());
        Assert.False(ohne.Instance.VerbundMoeglich);

        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });
        var mit = Zeige(stand);
        Assert.True(mit.Instance.VerbundMoeglich);
    }

    /// <summary>
    /// Der Leitspeicher selbst ist kein Kandidat, und die Summe zaehlt ihn mit
    /// (VerbundSummeAnzeigen:1391-1405).
    /// </summary>
    [Fact]
    public void Die_Verbundsumme_zaehlt_den_Leitspeicher_mit()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Assert.Contains("Kein Verbund", cut.Instance.Verbundsumme);

        // Der Leitspeicher 11 steht NICHT in der Kandidatenliste.
        Assert.Contains("Zweiter Heizungsspeicher", cut.Markup);

        cut.FindAll("input.epos-mehrfach-kasten, .epos-mehrfachauswahl input[type=checkbox]")[0]
           .Change(true);
        Assert.Single(cut.Instance.Verbund);
        Assert.Contains("2 Speicher", cut.Instance.Verbundsumme);
    }

    // ============================================================ OK-Regeln

    [Fact]
    public void Ein_Ladeziel_ohne_Speicher_meldet_mit_Rang_und_Ziel()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 0, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Ok(cut);

        Assert.Contains("Rang 1", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    [Fact]
    public void Derselbe_Speicher_zweimal_meldet()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_KOMBI, IdPuffer = 11, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Ok(cut);

        Assert.Contains("mehr als einmal", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    /// <summary>
    /// ABWEICHUNG A-9 (Befund W10-B24): Der Feldfehler steht im Formularzustand, nicht
    /// als -2 im Modell - gemeldet wird er mit demselben Wortlaut und der Nennung des
    /// Rangs.
    /// </summary>
    [Fact]
    public void Eine_Ladegrenze_ausserhalb_meldet_mit_dem_Rang()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Schalter(cut, "Ladeobergrenze").Change(true);
        cut.FindAll("input.epos-eingabe")[0].Input("150");
        Ok(cut);

        Assert.Contains("Ladeobergrenze", cut.Instance.Meldung);
        Assert.Contains("zwischen 0 und 100", cut.Instance.Meldung);
        Assert.Contains("Rang 1", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    [Fact]
    public void Eine_Einspeisehoehe_ausserhalb_meldet_mit_dem_Rang()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        var cut = Zeige(stand);
        Schalter(cut, "Einspeisehöhe").Change(true);
        cut.FindAll("input.epos-eingabe").Last().Input("2");
        Ok(cut);

        Assert.Contains("Einspeisehöhe", cut.Instance.Meldung);
        Assert.Contains("zwischen 0 und 1", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    /// <summary>Der HARTE Warnbefund kommt aus dem Kern und blockiert ebenfalls.</summary>
    [Fact]
    public void Ein_harter_Warnbefund_blockiert()
    {
        var stand = MitPuffern();
        stand.Harter = "Der Speicher ist zugleich Quelle und Senke.";

        var cut = Zeige(stand);
        Ok(cut);

        Assert.Equal("Der Speicher ist zugleich Quelle und Senke.", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    // ============================================================ Speichern

    [Fact]
    public void OK_speichert_Liste_und_Verbund_und_liefert_beides()
    {
        var stand = MitPuffern();
        stand.Bestand.Add(new SenkenzeileDaten { Ziel = P_HEIZUNG, IdPuffer = 11, Bedarfsart = BEIDES });

        WaermesenkeErgebnis? ergebnis = null;
        var cut = Zeige(stand, e => ergebnis = e);

        Ok(cut);

        Assert.NotNull(stand.Geschrieben);
        Assert.NotNull(stand.VerbundGeschrieben);
        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.SpeichernOk);
        Assert.Single(ergebnis.Zeilen);
    }

    /// <summary>Ein gescheitertes Schreiben meldet der Aufrufer, nicht der Dialog.</summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_kommt_als_SpeichernOk_false()
    {
        var stand = MitPuffern();
        stand.SchreibErgebnis = false;

        WaermesenkeErgebnis? ergebnis = null;
        var cut = Zeige(stand, e => ergebnis = e);
        Ok(cut);

        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.SpeichernOk);
    }

    /// <summary>
    /// Die WEICHEN Befunde kommen NACH dem Speichern und in EINEM Banner mit dem Kopf
    /// des Warnkriterienkatalogs.
    /// </summary>
    [Fact]
    public void Die_weichen_Befunde_kommen_nach_dem_Speichern_in_einem_Banner()
    {
        var stand = MitPuffern();
        stand.Weiche.Add("Der Speicher führt den Kanal nicht.");
        stand.Weiche.Add("Der Vorlauf liegt unter dem Sollwert.");

        var cut = Zeige(stand);
        Ok(cut);

        Assert.NotNull(stand.Geschrieben);                    // gespeichert wurde
        Assert.Contains("unplausibel", cut.Instance.Meldung);
        Assert.Contains("führt den Kanal nicht", cut.Instance.Meldung);
        Assert.Contains("unter dem Sollwert", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der Absprung (Konzept 4.6): Ein Fehler, der sich durch das Anlegen eines Puffers
    /// beheben laesst, fragt zurueck statt nur zu melden.
    /// </summary>
    [Fact]
    public void Ein_fehlender_Puffer_fragt_nach_der_Verwaltung()
    {
        var stand = MitPuffern();
        stand.Pruefung = new SenkenPruefung(false, "Kein Pufferspeicher im Projekt.", true, "");

        var gaben = new Dictionary<string, object> { ["IdProjekt"] = 1030 };
        var cut = Zeige(stand, verwaltung: _ => gaben);
        Ok(cut);

        Assert.True(cut.Instance.FrageSteht);
        Assert.Contains("Kein Pufferspeicher im Projekt.", cut.Markup);
        Assert.Null(stand.Geschrieben);

        cut.FindAll(".epos-rueckfrage button")[0].Click();     // Ja
        Assert.True(cut.Instance.VerwaltungOffen);
    }

    /// <summary>Ohne Absprungkennzeichen bleibt es bei der Meldung.</summary>
    [Fact]
    public void Ein_sonstiger_Pruefungsfehler_meldet_nur()
    {
        var stand = MitPuffern();
        stand.Pruefung = new SenkenPruefung(false, "Kurzschluss Quelle = Senke.", false, "");

        var cut = Zeige(stand);
        Ok(cut);

        Assert.False(cut.Instance.FrageSteht);
        Assert.Equal("Kurzschluss Quelle = Senke.", cut.Instance.Meldung);
        Assert.Null(stand.Geschrieben);
    }

    [Fact]
    public void Abbrechen_und_Esc_liefern_null_ohne_zu_schreiben()
    {
        var stand = MitPuffern();
        WaermesenkeErgebnis? ergebnis = new(true, Array.Empty<SenkenzeileDaten>(), Array.Empty<int>());
        var cut = Zeige(stand, e => ergebnis = e);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent == "Abbrechen").Click();
        Assert.Null(ergebnis);
        Assert.Null(stand.Geschrieben);

        ergebnis = new(true, Array.Empty<SenkenzeileDaten>(), Array.Empty<int>());
        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
        Assert.Null(stand.Geschrieben);
    }

    // ============================================================ Hilfsgriffe

    private static void Ok(IRenderedComponent<WaermesenkeDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").Last().Click();

    private static void Hinzu(IRenderedComponent<WaermesenkeDialog> cut)
        => cut.FindAll("button").First(b => b.TextContent.Contains("hinzufügen")).Click();

    private static void Entfernen(IRenderedComponent<WaermesenkeDialog> cut)
        => cut.FindAll("button").First(b => b.TextContent == "Entfernen").Click();

    private static void Ziel(IRenderedComponent<WaermesenkeDialog> cut, int index)
        => cut.FindAll("select")[0].Change(index.ToString());

    /// <summary>
    /// Ein SCHALTER ueber seine BESCHRIFTUNG. Nicht ueber die Reihenfolge zaehlen: Die
    /// Mehrfachauswahl des Verbunds baut ihre Haken aus demselben Baustein und steht
    /// davor.
    /// </summary>
    private static AngleSharp.Dom.IElement Schalter(
        IRenderedComponent<WaermesenkeDialog> cut, string beschriftung)
    {
        foreach (var l in cut.FindAll("label.epos-schalter"))
            if (l.TextContent.Contains(beschriftung))
                return l.QuerySelector("input")!;
        throw new InvalidOperationException("Kein Schalter mit der Beschriftung " + beschriftung);
    }
}
