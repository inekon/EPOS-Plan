using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Kostenverwaltung (iU9-W4.2), Vorbild
/// <c>Views/Kosten/Form_KostenKomponente</c>.
///
/// <para>Soll ist die Feldkarte: Kopf mit Titel und Untertitel, der
/// Netto-Hinweis mit Kreuz, die Kontextzeile (Komponente, Kategorie), die
/// Variantenzeile, das Positionsraster mit sieben Spalten, die drei Knöpfe
/// darunter, der Summenfuß und die Schlussleiste. Dazu die fünf Unterdialoge
/// der Wellen 1 bis 3, die jetzt in einer Überlagerung stehen.</para>
/// </summary>
public class KostenKomponenteDialogTests : BunitContext
{
    // Gate sept39 (11.09.2026, Gegenprobe #230b, LANG=en_US.UTF-8):
    // Der_Spaltenkopf_Nutzungsdauer_traegt_keinen_Zeilenumbruch vergleicht gegen
    // Resource.KDLG_SP_NUTZUNG ("Nutzungsdauer [a]") — ohne Pinnung lieferte die
    // Ressource unter en-US "Service life [a]". Hausvorrichtung seit #168,
    // Rückstellung in Dispose.
    private readonly Kulturvorrichtung _kultur = new();

    public KostenKomponenteDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kultur.Dispose();
        }

        base.Dispose(disposing);
    }

    // ---- Probendaten -----------------------------------------------------

    private static readonly (int Id, string Text)[] EINTRAEGE =
    {
        (0, "Wärmepumpe"), (1, "Heizkessel")
    };

    private static readonly (int Id, string Text)[] BEMESSUNGEN =
    {
        (0, "fester Betrag"), (1, "% der Investition"), (2, "je kW Leistung")
    };

    private static KostenPositionZeile Zeile(int id, string name, double satz)
        => new KostenPositionZeile
        {
            Id = id,
            Bezeichnung = name,
            BemessungId = 0,
            Satz = satz,
            BetragText = satz.ToString("0.##"),
            Kette = true,
            Einheit = "€",
            Schreibbar = true
        };

    private KostenKomponenteStand _stand = new();
    private int _geladen;

    private KostenKomponenteStand Standard(bool projekt = false, bool nurLesen = false)
        => new KostenKomponenteStand
        {
            Titel = projekt ? "Kostenverwaltung Wärmepumpe — Musterprojekt"
                            : "Kostenverwaltung Wärmepumpe",
            Untertitel = "Investitionskosten nach VDI 2067",
            Varianten = new[] { (5, "Standard"), (6, "Variante 1") },
            VarianteId = 5,
            VariantePflegbar = !projekt,
            NurLesen = nurLesen,
            Zeilen = new[] { Zeile(11, "Montage", 1200), Zeile(12, "Gerät", 8000) },
            Bemessungen = BEMESSUNGEN,
            SpalteBetrag = "Betrag netto [€]",
            MitNutzungsdauer = true,
            MitWorstBest = projekt,
            Summen = new[] { ("Summe Investitionskosten netto: 9.200,00 €", true) },
            PositionNeuMoeglich = !nurLesen,
            VarianteLoeschbar = !nurLesen
        };

    /// <summary>Der zuletzt gestellte Kontext (Prüfhilfe).</summary>
    private KostenKomponenteKontext? _gefragt;

    private IRenderedComponent<KostenKomponenteDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<KostenKomponenteDialog>>? mehr = null,
        KostenKomponenteStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        _gefragt = null;
        return Render<KostenKomponenteDialog>(p =>
        {
            p.Add(x => x.Eintraege, EINTRAEGE);
            p.Add(x => x.Laden, k => { _geladen++; _gefragt = k; return _stand; });
            p.Add(x => x.Summen, () => _stand.Summen);
            mehr?.Invoke(p);
        });
    }

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Kontext_Raster_und_Schlussleiste()
    {
        var cut = Zeige();

        Assert.Equal("Kostenverwaltung Wärmepumpe", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Investitionskosten nach VDI 2067", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Single(cut.FindAll(".epos-zeilenraster"));
        Assert.Equal(2, cut.FindAll(".epos-optionsgruppe .epos-option").Count);
    }

    [Fact]
    public void Der_Spaltenkopf_traegt_die_sieben_Ueberschriften_der_Feldkarte()
    {
        var cut = Zeige(p => p
            .Add(x => x.SpalteAktionen, "Aktionen")
            .Add(x => x.SpaltePosition, "Position")
            .Add(x => x.SpalteBemessung, "Bemessung")
            .Add(x => x.SpalteSatz, "Satz")
            .Add(x => x.SpalteNutzung, "Nutzung [a]")
            .Add(x => x.SpalteWorstBest, "Worst/Best"));

        var koepfe = cut.FindAll(".epos-zr-kopfzelle");
        Assert.Equal(7, koepfe.Count);
        Assert.Equal("Aktionen", koepfe[0].TextContent);
        Assert.Equal("Betrag netto [€]", koepfe[4].TextContent);
        Assert.Equal("Worst/Best", koepfe[6].TextContent);
    }

    /// <summary>
    /// Befund 1a der Abnahmeliste vom 11.09.2026 (#186): <c>KDLG_SP_NUTZUNG</c>
    /// trug einen echten Zeilenumbruch ("Nutzungs-\ndauer [a]"), der in WinForms
    /// wirkte und in HTML zu einem Leerzeichen kollabierte - dadurch verdeckt
    /// blieb, dass die Kopfzelle des Zeilenrasters keinen Umbruch verkraftete
    /// (<c>white-space: nowrap</c> ohne Overflow) und der Kopftext in die
    /// Nachbarspalte lief. Dieser Fall haelt den RESSOURCENWERT fest, den die
    /// Windows-Huelle (<c>KostenKomponenteHuelle</c>) unveraendert an
    /// <see cref="KostenKomponenteDialog.SpalteNutzung"/> reicht, und dass der
    /// Spaltenkopf ihn ohne Zeilenumbruchzeichen zeigt.
    /// </summary>
    [Fact]
    public void Der_Spaltenkopf_Nutzungsdauer_traegt_keinen_Zeilenumbruch()
    {
        string spalte = Resource.KDLG_SP_NUTZUNG;
        Assert.DoesNotContain("\n", spalte);
        Assert.DoesNotContain("\r", spalte);
        Assert.Equal("Nutzungsdauer [a]", spalte);

        var cut = Zeige(p => p.Add(x => x.SpalteNutzung, spalte));

        string kopf = cut.FindAll(".epos-zr-kopfzelle")[5].TextContent;   // sechste Spalte

        Assert.Equal("Nutzungsdauer [a]", kopf);
        Assert.DoesNotContain("\n", kopf);
    }

    [Fact]
    public void Der_Netto_Hinweis_laesst_sich_ausblenden()
    {
        var cut = Zeige(p => p.Add(x => x.BannerText, "Alle Beträge sind NETTO."));

        Assert.Single(cut.FindAll(".epos-bannerzeile"));
        cut.Find(".epos-bannerzeile button").Click();
        Assert.Empty(cut.FindAll(".epos-bannerzeile"));
    }

    [Fact]
    public void Die_Variantenzeile_steht_nur_im_Stammkontext()
    {
        var stamm = Zeige();
        var projekt = Zeige(stand: Standard(projekt: true));

        Assert.Equal(2, stamm.FindAll(".epos-kontextleiste").Count);   // Kontext + Variante
        Assert.Single(projekt.FindAll(".epos-kontextleiste"));          // nur der Kontext
    }

    [Fact]
    public void Eine_Auslieferungsvorlage_meldet_sich_und_sperrt_das_Anlegen()
    {
        var cut = Zeige(stand: Standard(nurLesen: true));

        Assert.Contains("Auslieferungsvorlage", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zr-neuzeile"));
    }

    [Fact]
    public void Der_Summenfuss_zeigt_die_Nettosumme()
    {
        var cut = Zeige();

        Assert.Contains("Summe Investitionskosten netto: 9.200,00 €",
                        cut.Find(".epos-zr-summenzelle").TextContent);
    }

    // =====================================================================
    // U29 — der dreiteilige Summenfuß der Investitionsseite
    // =====================================================================

    /// <summary>
    /// Führt die Komponente eine Zuschusszeile, nennt der Fuß unter der
    /// Nettosumme die Bruttoinvestition, den Zuschuss und I₀ — drei Zeilen, die
    /// dritte betont. Gebildet wird sie in der Hülle über den Kern
    /// (<c>KostenSummenCtrl.Fuss</c>); der Dialog zeigt, was im Stand steht.
    /// </summary>
    [Fact]
    public void Mit_Zuschusszeile_traegt_der_Fuss_Bruttoinvestition_Zuschuss_und_I0()
    {
        KostenKomponenteStand s = Standard();
        s.Summen = new[]
        {
            ("Summe Investitionskosten netto: 234.772,40 €", true),
            ("Summe brutto: 279.379,16 € (Umsatzsteuer 19 % aus dem Katalog)", false),
            ("Investition brutto 240.772,40 € · Zuschuss 6.000,00 € · I₀ 234.772,40 €", true),
        };

        var cut = Zeige(stand: s);

        var zellen = cut.FindAll(".epos-zr-summenzelle");
        Assert.Equal(3, zellen.Count);
        Assert.Equal("Investition brutto 240.772,40 € · Zuschuss 6.000,00 € · I₀ 234.772,40 €",
                     zellen[2].TextContent);
        Assert.Contains("epos-zr-summenzelle--stark", zellen[2].ClassName);
    }

    /// <summary>Gegenprobe: Ohne Zuschusszeile gibt der Kern keinen dritten Text
    /// heraus — dann bleibt es bei Netto und Brutto, weil netto = I₀ ist.</summary>
    [Fact]
    public void Ohne_Zuschusszeile_bleibt_der_dreiteilige_Fuss_weg()
    {
        KostenKomponenteStand s = Standard();
        s.Summen = new[]
        {
            ("Summe Investitionskosten netto: 240.772,40 €", true),
            ("Summe brutto: 286.519,16 € (Umsatzsteuer 19 % aus dem Katalog)", false),
        };

        var cut = Zeige(stand: s);

        Assert.Equal(2, cut.FindAll(".epos-zr-summenzelle").Count);
        Assert.DoesNotContain("Investition brutto", cut.Markup);
    }

    // =====================================================================
    // U28 — die Herleitungszeile wandert bis in die Rasterzeile durch
    // =====================================================================

    /// <summary>
    /// Der Dialog reicht die Herleitung des Kerns an die Zeile weiter — sie steht
    /// unter dem Betrag, eine je gerechneter Zeile.
    /// </summary>
    [Fact]
    public void Die_Herleitung_des_Standes_steht_unter_dem_Betrag_der_Zeile()
    {
        KostenKomponenteStand s = Standard();
        KostenPositionZeile erste = s.Zeilen[0];
        erste.Herleitung = "× 300,00 kW · P_el der Anlage · Runde 1";

        var cut = Zeige(stand: s);

        var herleitungen = cut.FindAll(".epos-zr-herleitung");
        Assert.Single(herleitungen);
        Assert.Equal("× 300,00 kW · P_el der Anlage · Runde 1", herleitungen[0].TextContent);
    }

    [Fact]
    public void Jede_Position_erscheint_als_eigene_Zeile()
    {
        var cut = Zeige();

        // Zwei gepflegte Zeilen plus die Abschlusszeile.
        Assert.Equal(3, cut.FindAll(".epos-zr-zeile").Count);
        Assert.Contains("Montage", cut.Markup);
        Assert.Contains("Gerät", cut.Markup);
    }

    // =====================================================================
    // Kontextwechsel
    // =====================================================================

    [Fact]
    public void Ein_Komponentenwechsel_fragt_die_Huelle_neu()
    {
        var cut = Zeige();

        cut.Find(".epos-kontextleiste select").Change("1");

        Assert.Equal(1, _gefragt!.EintragId);
        Assert.True(_gefragt.Invest);
        Assert.Null(_gefragt.VarianteId);
    }

    [Fact]
    public void Ein_Kategoriewechsel_fragt_mit_Betrieb()
    {
        var cut = Zeige();

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].Change(true);

        Assert.False(_gefragt!.Invest);
    }

    [Fact]
    public void Ein_Variantenwechsel_fragt_mit_der_Variante()
    {
        var cut = Zeige();

        cut.FindAll(".epos-kontextleiste select")[1].Change("6");

        Assert.Equal(6, _gefragt!.VarianteId);
    }

    [Fact]
    public void Die_Vorwahl_wird_uebernommen()
    {
        KostenKomponenteKontext? gefragt = null;
        Render<KostenKomponenteDialog>(p => p
            .Add(x => x.Eintraege, EINTRAEGE)
            .Add(x => x.EintragVorwahl, 1)
            .Add(x => x.InvestVorwahl, false)
            .Add(x => x.Laden, k => { gefragt = k; return Standard(); }));

        Assert.Equal(1, gefragt!.EintragId);
        Assert.False(gefragt.Invest);
        Assert.False(gefragt.Invest);
    }

    // =====================================================================
    // Feldänderungen (Ä12/Ä19: erst „Speichern" schreibt)
    // =====================================================================

    [Fact]
    public void Eine_Feldaenderung_zieht_Kopplung_und_Summen_nach_ohne_zu_schreiben()
    {
        KostenPositionZeile? nachgezogen = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Nachziehen, (KostenPositionZeile z) => nachgezogen = z)
            .Add(x => x.Speichern, () => { gespeichert++; return true; }));

        cut.FindAll(".epos-zr-zeile input[type=text]")[1].Input("1500");

        Assert.NotNull(nachgezogen);
        Assert.Equal(1500.0, nachgezogen!.Satz);
        Assert.Equal(0, gespeichert);
    }

    [Fact]
    public void Speichern_schreibt_alles_und_bestaetigt_in_der_Statuszeile()
    {
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.VorlageGespeichert, "gespeichert {0} Uhr"));

        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[1].Click();

        Assert.Equal(1, gespeichert);
        Assert.StartsWith("gespeichert ", cut.Instance.Status);
    }

    [Fact]
    public void OK_speichert_und_schliesst_mit_true()
    {
        bool? ergebnis = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[2].Click();

        Assert.Equal(1, gespeichert);
        Assert.True(ergebnis);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_speichern()
    {
        bool? ergebnis = null;
        int gespeichert = 0;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, () => { gespeichert++; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-leiste")[^1].QuerySelectorAll("button")[0].Click();

        Assert.Equal(0, gespeichert);
        Assert.False(ergebnis);
    }

    // =====================================================================
    // Positionen
    // =====================================================================

    [Fact]
    public void Die_Abschlusszeile_legt_mit_ihrem_Namen_an()
    {
        string? name = null;
        var cut = Zeige(p => p.Add(x => x.PositionNeu, (string n) => { name = n; return 42; }));

        var neu = cut.Find(".epos-zr-neuzeile");
        neu.QuerySelectorAll("input[type=text]")[0].Input("Wartung");
        neu.QuerySelector("button")!.Click();

        Assert.Equal("Wartung", name);
    }

    [Fact]
    public void Der_Knopf_Position_hinzufuegen_nimmt_die_Vorgabe()
    {
        string? name = null;
        var cut = Zeige(p => p
            .Add(x => x.PositionNeuVorgabe, "Neue Position")
            .Add(x => x.PositionNeu, (string n) => { name = n; return 42; }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[0].Click();

        Assert.Equal("Neue Position", name);
    }

    [Fact]
    public void Der_Papierkorb_fragt_erst_nach()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; })
            .Add(x => x.VorlagePositionLoeschen, "Position „{0}\" löschen?"));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();

        Assert.Contains("Position „Montage\" löschen?", cut.Markup);
        Assert.Equal(0, geloescht);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        Assert.Equal(1, geloescht);
    }

    [Fact]
    public void Nein_loescht_nicht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[1].Click();

        Assert.Equal(0, geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Eine_Pflichtposition_wird_erklaert_statt_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.IstPflicht, (KostenPositionZeile z) => true)
            .Add(x => x.PositionLoeschen, (KostenPositionZeile z) => { geloescht++; return true; })
            .Add(x => x.VorlagePflichtLoeschen, "„{0}\" ist eine Pflichtposition."));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[1].Click();

        Assert.Equal(0, geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Contains("„Montage\" ist eine Pflichtposition.", cut.Instance.Meldung);
    }

    // =====================================================================
    // Zeilenaktionen: das Neuladen behaelt die ungespeicherten Eingaben
    // =====================================================================

    /// <summary>
    /// Eine „Datenbank", wie die Windows-Hülle sie führt: <c>Laden</c> baut bei
    /// JEDEM Aufruf NEUE <see cref="KostenPositionZeile"/> aus den persistierten
    /// Positionen (<c>VorlagenRasterAufbauen</c>: <c>_bindungen.Clear()</c>,
    /// <c>_zeilen = new List&lt;…&gt;()</c>). Ein Fake, der immer dieselben Objekte
    /// zurückgibt, kann den Anwenderbefund gar nicht zeigen — er verliert die
    /// Eingabe nicht, weil sie im selben Objekt stehen bleibt.
    /// </summary>
    private sealed class Ablage
    {
        public readonly List<(int Id, string Name, double Satz)> Positionen
            = new List<(int, string, double)> { (11, "Montage", 1200), (12, "Gerät", 8000) };

        private int _naechste = 13;

        /// <summary>Wie <c>PositionNeu</c> der Hülle: schreibt SOFORT (Ä12).</summary>
        public int Anlegen(string name)
        {
            int id = _naechste++;
            Positionen.Add((id, name, 0));
            return id;
        }

        public bool Loeschen(int id) => Positionen.RemoveAll(p => p.Id == id) > 0;
    }

    /// <summary>Die Datenzeilen des Rasters ohne die Abschlusszeile.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Datenzeilen(
        IRenderedComponent<KostenKomponenteDialog> cut)
        => cut.FindAll(".epos-zeilenraster > .epos-zr-zeile");

    /// <summary>Das Satzfeld der <paramref name="nr"/>-ten Datenzeile (vierte Spur).</summary>
    private static AngleSharp.Dom.IElement Satzfeld(
        IRenderedComponent<KostenKomponenteDialog> cut, int nr)
        => Datenzeilen(cut)[nr].QuerySelectorAll("input[type=text]")[1];

    private KostenKomponenteStand AusAblage(Ablage ablage)
    {
        KostenKomponenteStand s = Standard();
        var zeilen = new List<KostenPositionZeile>();
        foreach ((int id, string name, double satz) in ablage.Positionen)
        {
            zeilen.Add(Zeile(id, name, satz));
        }

        s.Zeilen = zeilen;
        _stand = s;
        s.Summen = Summenfuss();
        return s;
    }

    /// <summary>Der Summenfuß fällt aus dem STAND, nicht aus der Ablage (Ä12/Ä19).</summary>
    private IReadOnlyList<(string Text, bool Stark)> Summenfuss()
    {
        double summe = 0;
        foreach (KostenPositionZeile z in _stand.Zeilen) summe += z.Satz ?? 0;
        return new[] { ("Summe " + summe.ToString("0.##"), true) };
    }

    private IRenderedComponent<KostenKomponenteDialog> ZeigeMitAblage(
        Ablage ablage,
        Action<Bunit.ComponentParameterCollectionBuilder<KostenKomponenteDialog>>? mehr = null)
    {
        _geladen = 0;
        _gefragt = null;

        return Render<KostenKomponenteDialog>(p =>
        {
            p.Add(x => x.Eintraege, EINTRAEGE);
            p.Add(x => x.Laden, k => { _geladen++; _gefragt = k; return AusAblage(ablage); });
            p.Add(x => x.Summen, Summenfuss);
            p.Add(x => x.Nachziehen, (KostenPositionZeile z) =>
                z.BetragText = (z.Satz ?? 0).ToString("0.##"));
            p.Add(x => x.PositionNeu, (string n) => ablage.Anlegen(n));
            p.Add(x => x.PositionLoeschen, (KostenPositionZeile z) => ablage.Loeschen(z.Id));
            p.Add(x => x.PositionNeuVorgabe, "Neue Position");
            mehr?.Invoke(p);
        });
    }

    /// <summary>
    /// ANWENDERBEFUND 14.09.2026 (Kostenverwaltung, Reiter „Kosten Invest/Betrieb"):
    /// „Bei zufügen von Position werden zuvor eingegebene Werte auf null gesetzt."
    ///
    /// <para>Ursache: „Position hinzufügen" schreibt die NEUE Position sofort und
    /// lässt danach den ganzen Stand neu laden; die Hülle baut dabei jede Zeile neu
    /// aus der Datenbank, in der die noch nicht gespeicherten Eingaben der übrigen
    /// Zeilen nicht stehen. Die Regel Ä12/Ä19 („die Änderung lebt bis Speichern nur
    /// im Objekt") trägt nur, solange niemand die Objekte austauscht — beim
    /// Auffrischen werden die Eingaben deshalb übertragen.</para>
    /// </summary>
    [Fact]
    public void Position_hinzufuegen_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(3, Datenzeilen(cut).Count));
        Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(1500.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("8000", Satzfeld(cut, 1).GetAttribute("value"));
        Assert.Equal("Neue Position", cut.Instance.Stand.Zeilen[2].Bezeichnung);
        Assert.Equal("Summe 9500", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>Derselbe Weg über die Abschlusszeile (Name eintippen, dann ＋).</summary>
    [Fact]
    public void Die_Abschlusszeile_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 1).Input("9000");
        cut.WaitForAssertion(() => Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value")));

        cut.Find(".epos-zr-neuzeile").QuerySelectorAll("input[type=text]")[0].Input("Wartung");
        cut.Find(".epos-zr-neuzeile").QuerySelector("button")!.Click();

        cut.WaitForAssertion(() => Assert.Equal(3, Datenzeilen(cut).Count));
        Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value"));
        Assert.Equal(9000.0, cut.Instance.Stand.Zeilen[1].Satz);
        Assert.Equal("Wartung", cut.Instance.Stand.Zeilen[2].Bezeichnung);
        Assert.Equal("Summe 10200", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>„Position löschen" lädt denselben Stand neu — und behält ebenso.</summary>
    [Fact]
    public void Position_loeschen_behaelt_die_ungespeicherten_Eingaben_der_uebrigen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage, p => p
            .Add(x => x.VorlagePositionLoeschen, "Position „{0}\" löschen?"));

        Satzfeld(cut, 1).Input("9000");
        cut.WaitForAssertion(() => Assert.Equal("9000", Satzfeld(cut, 1).GetAttribute("value")));

        Datenzeilen(cut)[0].QuerySelectorAll("button")[1].Click();      // 🗑️ der ersten Zeile
        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();         // Ja

        cut.WaitForAssertion(() => Assert.Single(Datenzeilen(cut)));
        Assert.Equal("9000", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(9000.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("Summe 9000", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>
    /// „In Projekt übernehmen" lädt nach dem Schließen ebenfalls neu
    /// (<c>btnUebernahme_Click</c>: übernommene Positionen sofort zeigen).
    /// </summary>
    [Fact]
    public void Die_Uebernahme_behaelt_die_ungespeicherten_Eingaben_der_Zeilen()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage, p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                }));

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();   // Übernahme
        cut.FindAll(".epos-ueberlagerung .epos-leiste")[^1]
           .QuerySelectorAll("button")[1].Click();                              // Abbrechen

        cut.WaitForAssertion(() => Assert.False(cut.Instance.UeberlagerungOffen));
        Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value"));
        Assert.Equal(1500.0, cut.Instance.Stand.Zeilen[0].Satz);
        Assert.Equal("Summe 9500", cut.Find(".epos-zr-summenzelle").TextContent);
    }

    /// <summary>
    /// Der GEGENFALL und damit die Grenze der Regel: Ein echter Kontextwechsel
    /// (andere Komponente, andere Kategorie, andere Variante) lädt OHNE Übertrag —
    /// dort ist das Verwerfen gewollt, die Zeilen des neuen Kontexts sind andere.
    /// </summary>
    [Fact]
    public void Ein_Kontextwechsel_laedt_ohne_Uebertrag()
    {
        var ablage = new Ablage();
        var cut = ZeigeMitAblage(ablage);

        Satzfeld(cut, 0).Input("1500");
        cut.WaitForAssertion(() => Assert.Equal("1500", Satzfeld(cut, 0).GetAttribute("value")));

        cut.FindAll(".epos-kontextleiste select")[0].Change("1");

        cut.WaitForAssertion(() => Assert.Equal(1200.0, cut.Instance.Stand.Zeilen[0].Satz));
        Assert.Equal("1200", Satzfeld(cut, 0).GetAttribute("value"));
    }

    // =====================================================================
    // Die fünf Unterdialoge in der Überlagerung
    // =====================================================================

    [Fact]
    public void Der_Stift_oeffnet_den_Zeileneditor_als_Ueberlagerung()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.True(cut.Instance.UeberlagerungOffen);
    }

    [Fact]
    public void Das_Plusminus_steht_nur_im_Projektmodus_und_oeffnet_Worst_Best()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0
                }),
            stand: Standard(projekt: true));

        var knoepfe = cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button");
        Assert.Equal(3, knoepfe.Length);
        knoepfe[2].Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Neu_und_Speichern_unter_oeffnen_die_Namensabfrage()
    {
        bool? kopie = null;
        var cut = Zeige(p => p.Add(x => x.VariantenGaben, (bool k) =>
        {
            kopie = k;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["TitelText"] = k ? "Speichern unter" : "Neue Variante",
                ["FrageText"] = "Name der neuen Variante:"
            };
        }));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();
        Assert.False(kopie);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ein_belegter_Variantenname_meldet_sich()
    {
        var cut = Zeige(p => p
            .Add(x => x.VariantenGaben, (bool k) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["TitelText"] = "Neue Variante", ["FrageText"] = "Name:",
                    ["Vorbelegung"] = "Wärmepumpe — Variante 2"
                })
            .Add(x => x.VarianteNeu, (bool k, string n) => 0)
            .Add(x => x.MeldungNameBelegt, "Der Name ist bereits vergeben oder leer."));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();
        // OK der Namensabfrage: die letzte Leiste in der Überlagerung.
        var leiste = cut.Find(".epos-ueberlagerung .epos-leiste");
        leiste.QuerySelectorAll("button")[^1].Click();

        Assert.Contains("bereits vergeben", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Standardvorlage_laesst_sich_nicht_loeschen()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.VarianteIstStandard, () => true)
            .Add(x => x.VarianteLoeschen, () => { geloescht++; return true; })
            .Add(x => x.MeldungStandardLoeschen, "Die Standardvorlage kann nicht gelöscht werden."));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[2].Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("Standardvorlage", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_Variante_wird_nach_Rueckfrage_geloescht()
    {
        int geloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.VarianteIstStandard, () => false)
            .Add(x => x.VarianteLoeschen, () => { geloescht++; return true; })
            .Add(x => x.VorlageVarianteLoeschen, "Variante „{0}\" wirklich löschen?"));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[2].Click();
        Assert.Contains("Variante „Standard\" wirklich löschen?", cut.Markup);

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        Assert.Equal(1, geloescht);
    }

    [Fact]
    public void Uebernahme_und_Katalog_erscheinen_als_Ueberlagerung()
    {
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    // =====================================================================
    // #187 — "Ein Titel, eine Stelle" (W11b-B-9): der Titel einer
    // Ueberlagerung mit eingebettetem Unterdialog erscheint GENAU EINMAL.
    // Vor #187 trugen CaseEingabeDialog, VorlagenPositionDialog und
    // KostenfaktorKatalogDialog ihren TitelText UNBEDINGT — bei gleichem
    // Ressourcenschluessel wie CaseTitel/EditorTitel/KatalogTitel stand der
    // Text zweimal uebereinander. Die Huelle liefert TitelText seither leer
    // (KostenKomponenteHuelle.CaseGaben/EditorGaben,
    // KostenfaktorKatalogHuelle.Gaben); hier wird das am gerenderten Markup
    // nachgewiesen, nicht nur an der Huelle.
    // =====================================================================

    [Fact]
    public void Der_Titel_des_Zeileneditors_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") },
                    ["TitelText"] = ""            // wie KostenKomponenteHuelle.EditorGaben seit #187
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Position bearbeiten", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    [Fact]
    public void Der_Titel_von_Worst_Best_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0,
                    ["TitelText"] = ""            // wie KostenKomponenteHuelle.CaseGaben seit #187
                }),
            stand: Standard(projekt: true));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[2].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Eingabe Worst/Best Case", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    [Fact]
    public void Der_Titel_des_Kostenfaktorkatalogs_erscheint_genau_einmal()
    {
        var cut = Zeige(p => p
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") },
                    ["TitelText"] = ""            // wie KostenfaktorKatalogHuelle.Gaben seit #187
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Administration Kostenfaktoren", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));
    }

    // =====================================================================
    // Ertrag/Bonus
    // =====================================================================

    [Fact]
    public void Der_Reiter_Ertrag_Bonus_erscheint_nur_wenn_die_Huelle_ihn_meldet()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstPv"] = true,
            ["Projekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
        };

        var ohne = Zeige();
        var cut = Zeige(stand: mit);

        // iU9-W5.0 (Nachzug A-2): Der Vorlaeufer ENTFERNTE die zweite
        // Reiterseite zur Laufzeit (ErtragReiterSteuern); hier fehlt der
        // zweite Reiterknopf.
        Assert.Single(ohne.FindAll(".epos-reiter-knopf"));
        Assert.Equal(2, cut.FindAll(".epos-reiter-knopf").Count);
        Assert.Equal("Ertrag/Bonus", cut.FindAll(".epos-reiter-knopf")[1].TextContent.Trim());

        Assert.Empty(cut.FindAll(".epos-ertragbonus"));   // erst nach dem Wechsel
        cut.FindAll(".epos-reiter-knopf")[1].Click();
        Assert.Single(cut.FindAll(".epos-ertragbonus"));
    }

    /// <summary>
    /// <b>Die SECHSTE Ueberlagerung: der Gesetzeskatalog</b> (iU9-W14c.3). Bis dahin
    /// sprang <c>ErtragBonus</c> mit <c>Sprungziel.Gesetzesparameter</c> ueber die
    /// <c>Sprungbruecke</c> in ein WinForms-Fenster; das Ziel ist jetzt selbst Razor,
    /// und zwei WebViews uebereinander sind Risiko R2. Das Reiterblatt kann keine
    /// Ueberlagerung oeffnen - es meldet den Wunsch nach oben.
    /// </summary>
    [Fact]
    public void Der_Gesetzeskatalog_erscheint_als_sechste_Ueberlagerung()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object>
        {
            ["IstBhkw"] = true
        };

        int gerufen = 0;
        var cut = Zeige(p => p.Add(x => x.GesetzeGaben, () =>
        {
            gerufen++;
            return (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
            };
        }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter Ertrag/Bonus
        Assert.False(cut.Instance.UeberlagerungOffen);

        cut.Find(".epos-ertragbonus button").Click();          // "Gesetzesparameter..."

        Assert.Equal(1, gerufen);
        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Single(cut.FindComponents<EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogDialog>());
    }

    /// <summary>
    /// Ohne Gaben bleibt der Knopf im Reiterblatt weg - "kein Delegat, kein Knopf".
    /// </summary>
    [Fact]
    public void Ohne_Gesetzesgaben_fehlt_der_Katalogknopf()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        var cut = Zeige(stand: mit);
        cut.FindAll(".epos-reiter-knopf")[1].Click();

        Assert.Empty(cut.FindAll(".epos-ertragbonus button"));
    }

    // =====================================================================
    // Tastatur
    // =====================================================================

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog").KeyDown(key: "Escape");

        Assert.False(ergebnis);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Esc/Abbrechen.</summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Abbrechen()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>
    /// <b>Esc schliesst immer nur die OBERSTE Ebene</b> (R-W14c-3): Steht eine
    /// Ueberlagerung, bleibt der Wirt stehen - je Ebene geprueft, auch fuer die neue
    /// sechste.
    /// </summary>
    [Theory]
    [InlineData("editor")]
    [InlineData("uebernahme")]
    [InlineData("katalog")]
    [InlineData("gesetze")]
    public void Esc_laesst_den_Wirt_stehen_solange_eine_Ueberlagerung_offen_ist(string ebene)
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok)
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                })
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                })
            .Add(x => x.GesetzeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
                }),
            stand: mit);

        switch (ebene)
        {
            case "editor":
                cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();
                break;
            case "uebernahme":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
                break;
            case "katalog":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();
                break;
            case "gesetze":
                cut.FindAll(".epos-reiter-knopf")[1].Click();
                cut.Find(".epos-ertragbonus button").Click();
                break;
        }

        Assert.True(cut.Instance.UeberlagerungOffen, "Die Ebene " + ebene + " steht nicht.");

        cut.Find(".epos-dialog").KeyDown(key: "Escape");

        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Die sechs Unterdialog-Überlagerungen waren <c>Schliessbar="false"</c> — seit dem
    /// Anwenderentscheid 15.09.2026 tragen sie ihr eigenes Kreuz (Titel jeweils gesetzt).
    /// Es schliesst NUR die Überlagerung, wie Esc es auch tut (R-W14c-3) — der Wirt
    /// bleibt stehen.
    /// </summary>
    [Theory]
    [InlineData("editor")]
    [InlineData("uebernahme")]
    [InlineData("katalog")]
    [InlineData("gesetze")]
    public void Das_Kreuz_der_Ueberlagerung_schliesst_nur_sie_selbst(string ebene)
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok)
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                })
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                })
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                })
            .Add(x => x.GesetzeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
                }),
            stand: mit);

        switch (ebene)
        {
            case "editor":
                cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();
                break;
            case "uebernahme":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();
                break;
            case "katalog":
                cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();
                break;
            case "gesetze":
                cut.FindAll(".epos-reiter-knopf")[1].Click();
                cut.Find(".epos-ertragbonus button").Click();
                break;
        }

        Assert.True(cut.Instance.UeberlagerungOffen, "Die Ebene " + ebene + " steht nicht.");

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool? ergebnis = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.Find(".epos-dialog").KeyDown(key: "Enter");

        Assert.Null(ergebnis);
    }

    [Fact]
    public void Der_Hilfeschluessel_bleibt_der_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);   // gewinnt gegen KeineHilfe

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_KostenKomponente.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    // Kein stilles 0 (Anwenderbefund 14.09.2026)
    // =====================================================================

    private static KostenKomponenteStand StandOhneBasis()
    {
        var stand = new KostenKomponenteStand
        {
            Titel = "Kostenverwaltung Solarthermie — Musterprojekt",
            Untertitel = "Investitionskosten nach VDI 2067",
            Zeilen = new[]
            {
                Zeile(21, "Montage", 1200),
                new KostenPositionZeile
                {
                    Id = 22,
                    Bezeichnung = "Solarthermie",
                    BemessungId = 2,
                    Satz = 700,
                    Einheit = "€/kW",
                    BetragText = "0,00",
                    OhneBasis = true,
                    BetragKurztext = "Keine Bezugsgröße: kein Gerät mit dieser Baugröße "
                                     + "im Projekt. Es gilt der erfasste Betrag.",
                    Schreibbar = true
                }
            },
            Bemessungen = BEMESSUNGEN,
            SpalteBetrag = "Betrag netto [€]",
            MitNutzungsdauer = true,
            MitWorstBest = true,
            PositionNeuMoeglich = true
        };
        return stand;
    }

    /// <summary>
    /// DER BEFUND: Im Bildschirmfoto stand unter dem Raster nichts. Der Grund lag
    /// allein im Werkzeugtipp des Betragsfeldes. Jetzt sammelt eine LEISE Zeile
    /// unter dem Raster Bezeichnung und Grund jeder Zeile ohne Bezugsgröße.
    /// </summary>
    [Fact]
    public void Zeilen_ohne_Bezugsgroesse_stehen_unter_dem_Raster()
    {
        var cut = Zeige(stand: StandOhneBasis());

        string zeile = cut.Find(".epos-zr-ohnebasis-zeile").TextContent;

        Assert.Contains("Solarthermie", zeile);
        Assert.Contains("kein Gerät mit dieser Baugröße im Projekt", zeile);
        Assert.DoesNotContain("Montage", zeile);
    }

    /// <summary>Hat jede Zeile ihre Bezugsgröße — der Regelfall —, bleibt die
    /// Zeile weg; ein Dauerhinweis wäre Lärm.</summary>
    [Fact]
    public void Ohne_solche_Zeilen_bleibt_der_Hinweis_weg()
    {
        var cut = Zeige(stand: Standard(projekt: true));

        Assert.Empty(cut.FindAll(".epos-zr-ohnebasis-zeile"));
        Assert.Equal("", cut.Instance.OhneBasisText);
    }

    /// <summary>Und die Zeile selbst trägt das Zeichen — der Wirt reicht das
    /// Kennzeichen durch.</summary>
    [Fact]
    public void Die_betroffene_Zeile_traegt_das_Zeichen_im_Raster()
    {
        var cut = Zeige(stand: StandOhneBasis());

        Assert.Single(cut.FindAll(".epos-zr-zeile .epos-zr-ohnebasis"));
    }

    // =====================================================================
    //  „Das Kreuz steht beim Titel" (Anwenderentscheid 15.09.2026): Die
    //  Ueberlagerung traegt Titel UND ✕, das eingebettete Blatt keins von
    //  beidem — je Einbettungsstelle ein Fall.
    // =====================================================================

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    /// <summary>Die Prüfung je Überlagerung: genau EIN ✕, kein zweiter Titel darunter.</summary>
    private static void NurEinKreuz(IRenderedComponent<KostenKomponenteDialog> cut)
    {
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Die_Ueberlagerung_Worst_Best_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.CaseGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Betrag"] = 1200.0
                }),
            stand: Standard(projekt: true));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[2].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Zeileneditor_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.EditorGaben, (KostenPositionZeile z) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Bezeichnung"] = z.Bezeichnung,
                    ["Kostenarten"] = (IReadOnlyList<(int, string)>)new[] { (0, "kapitalgebunden") }
                }));

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("button")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Namensabfrage_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.VariantenGaben, (bool k) =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["TitelText"] = "Neue Variante",
                ["FrageText"] = "Name der neuen Variante:"
            }));

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Uebernahme_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zielprojekte"] = (IReadOnlyList<(int, string)>)new[] { (1, "Projekt") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[1].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Kostenfaktorkatalog_zeigt_nur_ein_Kreuz()
    {
        var cut = Zeige(p => p
            .Add(x => x.KatalogGaben, () =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Zeilen"] = (IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>)
                        new[] { new KostenfaktorKatalogDialog.KostenfaktorZeile(1, "Faktor") }
                }));

        cut.FindAll(".epos-leiste")[0].QuerySelectorAll("button")[2].Click();

        NurEinKreuz(cut);
    }

    [Fact]
    public void Die_Ueberlagerung_Gesetzeskatalog_zeigt_nur_ein_Kreuz()
    {
        KostenKomponenteStand mit = Standard();
        mit.ErtragSichtbar = true;
        mit.ErtragGaben = new Dictionary<string, object> { ["IstBhkw"] = true };

        var cut = Zeige(p => p.Add(x => x.GesetzeGaben, () =>
            (IReadOnlyDictionary<string, object>)new Dictionary<string, object>
            {
                ["Klassenvorrat"] = (IReadOnlyList<(string, string)>)new[] { ("KWKG", "KWK-Gesetz") }
            }), stand: mit);

        cut.FindAll(".epos-reiter-knopf")[1].Click();          // Reiter Ertrag/Bonus
        cut.Find(".epos-ertragbonus button").Click();          // „Gesetzesparameter…"

        NurEinKreuz(cut);
    }
}
