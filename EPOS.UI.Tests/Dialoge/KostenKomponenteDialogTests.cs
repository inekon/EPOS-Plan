using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
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
    public KostenKomponenteDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
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
    // Ertrag/Bonus
    // =====================================================================

    [Fact]
    public void Der_Abschnitt_Ertrag_Bonus_erscheint_nur_wenn_die_Huelle_ihn_meldet()
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

        Assert.Empty(ohne.FindAll(".epos-ertragbonus"));
        Assert.Single(cut.FindAll(".epos-ertragbonus"));
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
}
