using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Brauchwasser-Zapfprofil" in der Stufe Einfach (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.7; Stufe Z1): Feldbestand je Zone, Zonenliste, Reiter der
/// Vorschau, benannte Sperren, Hinweise am Feld, der Rückweg mit Ergebnis und mit Abbruch und
/// der Fall ohne Gaben.
///
/// <para>Die Vorschau kommt aus einem Prüfdelegaten — der Dialog rechnet nicht; die
/// Entprellung steht auf 0, damit jede Eingabe sofort rechnet. Kultur de-DE.</para>
/// </summary>
public class ZapfprofilDialogTests : EposBunitContext
{
    public ZapfprofilDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    private static ZapfprofilNutzungsartDaten Art(int id, string name, bool waehlbar = true) => new()
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
        Waehlbar = waehlbar,
        Sperrgrund = waehlbar ? "" : "Der Tagesgangsatz dieser Nutzungsart ist unvollständig."
    };

    private static ZapfprofilEingabeDaten Eingabe() => new()
    {
        Weg = ZapfprofilWeg.Bestand,
        Zonen =
        {
            new ZapfprofilZoneDaten { Id = 11, Name = "Zone 1", IdNutzungsart = 1, Bezugsmenge = 20, Ueberschrieben = 2 },
            new ZapfprofilZoneDaten { Id = 12, Name = "Zone 2", IdNutzungsart = 2, Bezugsmenge = 50, Niveau = ZapfprofilNiveau.Hoch }
        }
    };

    private static ZapfprofilAnsichtDaten Ansicht(string titel, double zapfungKwh, bool abgelehnt = false) => new()
    {
        Titel = titel,
        Abgelehnt = abgelehnt,
        Monat = 1,
        WerktagKw = new double[24],
        Kennzahlen = new ZapfprofilKennzahlenDaten
        {
            JahresbedarfZapfungKwh = zapfungKwh,
            JahresverlustZirkulationKwh = 1000,
            JahresbedarfGesamtKwh = zapfungKwh + 1000,
            Zirkulationsanteil = 1000 / (zapfungKwh + 1000),
            TagesmittelZapfungKwh = zapfungKwh / 365,
            ZapfungLiterJeTag = 100,
            GroessterStundenwertKw = 10,
            VermerkGroessterStundenwert = "Bilanzwert, keine Auslegungsgröße",
            VolllaststundenH = 3000
        },
        UnterschriftTagesgang = "Unterschrift Tagesgang " + titel,
        UnterschriftWoche = "Unterschrift Woche " + titel,
        UnterschriftJahresgang = "Unterschrift Jahresgang " + titel
    };

    /// <summary>Eine gerechnete Vorschau zu einer Eingabe: Summe zuerst, dann je Zone.</summary>
    private static ZapfprofilVorschauDaten Vorschau(ZapfprofilEingabeDaten e, params ZapfprofilMeldung[] meldungen)
    {
        var v = new ZapfprofilVorschauDaten { Zustand = ZapfprofilVorschauZustand.Gerechnet, Status = "Vorschau aktuell" };
        double summe = 0;
        for (int i = 0; i < e.Zonen.Count; i++)
        {
            double kwh = (e.Zonen[i].Bezugsmenge ?? 0) * 1000;
            summe += kwh;
            v.Zonen.Add(new ZapfprofilZonenwertDaten { IdZone = e.Zonen[i].Id, Position = i, Zone = e.Zonen[i].Name, JahresbedarfZapfungKwh = kwh });
        }
        v.Ansichten.Add(Ansicht("Summe aller Zonen", summe));
        for (int i = 0; i < e.Zonen.Count; i++) v.Ansichten.Add(Ansicht(e.Zonen[i].Name, v.Zonen[i].JahresbedarfZapfungKwh));
        v.Meldungen.AddRange(meldungen);
        return v;
    }

    private static ZapfprofilDaten Daten(ZapfprofilEingabeDaten? eingabe = null, ZapfprofilVorschauDaten? vorschau = null)
    {
        ZapfprofilEingabeDaten e = eingabe ?? Eingabe();
        return new ZapfprofilDaten
        {
            IdProjekt = 7,
            Kontext = new ZapfprofilKontextDaten
            {
                Projekt = "Beispielprojekt",
                Klimaregion = "Klimaregion Mitte",
                Kalender = "Kalender: 1. Januar = Donnerstag, 365 Tage",
                Bilanzgrenze = "Bilanzgrenze: Zapfenergie an der Zapfstelle"
            },
            Katalog = { Art(1, "Wohnen A"), Art(2, "Büro B"), Art(3, "Gesperrt C", waehlbar: false) },
            Eingabe = e,
            Vorschau = vorschau ?? Vorschau(e),
            Verfuegbar = true
        };
    }

    private IRenderedComponent<ZapfprofilDialog> Aufbauen(
        ZapfprofilDaten? daten = null,
        Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>? vorschau = null,
        Func<ZapfprofilEingabeDaten, IReadOnlyList<ZapfprofilMeldung>>? pruefen = null,
        Action<ZapfprofilErgebnisDaten?>? geschlossen = null,
        bool titel = true)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, vorschau ?? (e => Vorschau(e)))
            .Add(x => x.Pruefen, pruefen ?? (_ => Array.Empty<ZapfprofilMeldung>()))
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.TitelAnzeigen, titel)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Option(IRenderedComponent<ZapfprofilDialog> cut, string text)
        => cut.FindAll("label.epos-option").First(l => l.TextContent.Trim() == text).QuerySelector("input")!;

    // =================================================================================
    // Feldbestand und Anordnung
    // =================================================================================

    [Fact]
    public void Einfach_zeigt_die_Zonenliste_und_vier_Felder_je_Zone()
    {
        var cut = Aufbauen();

        Assert.Contains("Brauchwasser-Zapfprofil – Beispielprojekt", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("Klimaregion Mitte", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Contains("2 Werte überschrieben", cut.Markup);

        // Die Zonenliste: zwei Zeilen, Summenzeile, eigene Listenleiste.
        IElement tabelle = cut.Find("table.epos-zapfprofil-zonen");
        Assert.Equal(2, tabelle.QuerySelectorAll("tbody tr").Length);
        Assert.Contains("Summe 2 Zonen", tabelle.QuerySelector("tfoot")!.TextContent);
        Assert.Contains("70,0", tabelle.QuerySelector("tfoot")!.TextContent);   // 20 + 50 MWh/a aus der Prüfvorschau
        Assert.Contains("Wohnen A", tabelle.TextContent);
        Assert.Contains("20 Wohneinheiten", tabelle.TextContent);
        Assert.NotNull(Knopf(cut, "Zone hinzufügen…"));
        Assert.NotNull(Knopf(cut, "Duplizieren"));
        Assert.NotNull(Knopf(cut, "Entfernen"));

        // Der Eingabeblock der gewählten Zone: Zonenname, Nutzungsart, Bezugsgröße, Niveau.
        Assert.Contains("Zone 1 · Nutzung", cut.Markup);
        Assert.Single(cut.FindAll("input[inputmode=decimal]"));
        Assert.Contains("Zonenname", cut.Markup);
        Assert.Contains("Nutzungsart", cut.Markup);
        Assert.Contains("Bedarfsniveau", cut.Markup);
        Assert.Equal(new[] { "niedrig", "mittel", "hoch" },
                     cut.FindAll("fieldset[aria-label='Bedarfsniveau'] .epos-feld-text").Select(e => e.TextContent).ToArray());

        // Herkunft und Status der Nutzungsart, die Vorgabe des Niveaus am Feld.
        Assert.Contains("Katalog · Eigenkonstruktion · Testquelle · Auslieferung · TEST-1", cut.Markup);
        Assert.Contains("Vorgabe · mittel = 5,0 kWh/(WE·d) aus dem Katalog", cut.Markup);
        Assert.Contains("Einheit aus dem Katalog der Nutzungsart", cut.Markup);
    }

    [Fact]
    public void Die_Katalogauswahl_sperrt_eine_unvollstaendige_Nutzungsart_mit_Grund()
    {
        var cut = Aufbauen();

        IElement gesperrt = cut.FindAll("option").First(o => o.TextContent.StartsWith("Gesperrt C"));
        Assert.True(gesperrt.HasAttribute("disabled"));
        Assert.Equal("Der Tagesgangsatz dieser Nutzungsart ist unvollständig.", gesperrt.GetAttribute("title"));
    }

    [Fact]
    public void Die_Vorschau_zeigt_vier_Reiter_und_die_Dauerlinie_benannt_gesperrt()
    {
        var cut = Aufbauen();

        string[] reiter = cut.FindAll("[role=tab]").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Tagesgang", "Wochenprofil", "Jahresgang", "Dauerlinie", "Kennzahlen" }, reiter);

        IElement dauer = cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Dauerlinie");
        Assert.Equal("true", dauer.GetAttribute("aria-disabled"));
        Assert.Equal("In dieser Fassung noch nicht verfügbar.", dauer.GetAttribute("title"));

        Assert.Contains("Unterschrift Tagesgang Summe aller Zonen", cut.Markup);
        Assert.Contains("Zapfung 70,0 MWh/a", cut.Find(".epos-zapfprofil-kurzkennzahlen").TextContent);

        // Kennzahlen: der größte Stundenwert trägt seinen Vermerk.
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        IElement kennzahlen = cut.Find("table.epos-zapfprofil-kennzahlen");
        Assert.Contains("Bilanzwert, keine Auslegungsgröße", kennzahlen.TextContent);
        Assert.Contains("Stochastik · noch nicht gerechnet", kennzahlen.TextContent);
        Assert.Contains("≈ 100 l/d", kennzahlen.TextContent);

        // Jahresgang mit Zirkulationsanteil.
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Jahresgang").Click();
        Assert.Contains("Zirkulationsanteil", cut.Find(".epos-zapfprofil-zirkulationsanteil").TextContent);
    }

    /// <summary>
    /// Befund 3, Mockup Abschnitt 3: Der Reiter Kennzahlen zeigt „Stunden über Schwelle", wenn
    /// der Kern sie ausweist (sonst nicht); die Gleichzeitigkeit steht mit „—" in der Bilanz, die
    /// Gruppe Stochastik trägt in der Stufe Einfach ihre Erklärzeile.
    /// </summary>
    [Fact]
    public void Die_Kennzahlen_zeigen_Schwelle_Bilanz_und_Stochastik_nach_dem_Mockup()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        ZapfprofilVorschauDaten v = Vorschau(e);
        v.Ansichten[0].Kennzahlen.StundenUeberSchwelle = 600;
        v.Ansichten[0].Kennzahlen.SchwelleKw = 10;
        var cut = Aufbauen(Daten(e, v));

        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        IElement[] zeilen = cut.FindAll("table.epos-zapfprofil-kennzahlen tbody tr").ToArray();
        string[] texte = zeilen.Select(z => z.TextContent.Trim()).ToArray();

        int bilanz = Array.FindIndex(texte, t => t.StartsWith("Bilanz"));
        int schwelle = Array.FindIndex(texte, t => t.StartsWith("Stunden über Schwelle 10,0 kW"));
        int gleichzeitigkeit = Array.FindIndex(texte, t => t.StartsWith("Gleichzeitigkeit"));
        int stochastik = Array.FindIndex(texte, t => t.StartsWith("Stochastik"));
        Assert.True(bilanz >= 0 && schwelle > bilanz && gleichzeitigkeit > schwelle && stochastik > gleichzeitigkeit,
                    string.Join(" | ", texte));
        Assert.Contains("600 h/a", texte[schwelle]);
        Assert.Contains("—", texte[gleichzeitigkeit]);
        Assert.StartsWith("„Stochastisch rechnen“ in der Fußleiste", texte[stochastik + 1]);
        Assert.Equal(stochastik + 2, texte.Length);

        // Ohne Schwelle keine Zeile.
        cut = Aufbauen();
        cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Kennzahlen").Click();
        Assert.DoesNotContain("Stunden über Schwelle", cut.Find("table.epos-zapfprofil-kennzahlen").TextContent);
    }

    [Fact]
    public void Anzeigen_fuer_waehlt_die_Ansicht_einer_Zone()
    {
        var cut = Aufbauen();

        IElement wahl = cut.FindAll("select").Last();
        wahl.Change("2");

        Assert.Contains("Unterschrift Tagesgang Zone 2", cut.Markup);
        Assert.Contains("Zapfung 50,0 MWh/a", cut.Find(".epos-zapfprofil-kurzkennzahlen").TextContent);
    }

    // =================================================================================
    // Benannte Sperren
    // =================================================================================

    [Fact]
    public void Erweitert_und_Experte_sind_benannt_gesperrt()
    {
        var cut = Aufbauen();

        IElement erweitert = Option(cut, "Erweitert");
        Assert.Equal("true", erweitert.GetAttribute("aria-disabled"));
        Assert.Equal("true", Option(cut, "Experte").GetAttribute("aria-disabled"));
        Assert.True(Option(cut, "Einfach").HasAttribute("checked"));

        erweitert.Change("1");

        Assert.Equal("In dieser Fassung noch nicht verfügbar.", cut.Instance.Hinweis);
        Assert.True(Option(cut, "Einfach").HasAttribute("checked"));
    }

    [Fact]
    public void Stochastik_und_Auslegung_stehen_im_Aktionsschlitz_und_melden_den_Grund()
    {
        var cut = Aufbauen();

        IElement leiste = cut.FindAll(".epos-leiste").Last();
        string[] knoepfe = leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Stochastisch rechnen", "Auslegung…", "Abbrechen", "OK" }, knoepfe);

        IElement stochastik = Knopf(cut, "Stochastisch rechnen");
        Assert.Equal("true", stochastik.GetAttribute("aria-disabled"));
        Assert.False(stochastik.HasAttribute("disabled"));
        stochastik.Click();

        Assert.Equal("In dieser Fassung noch nicht verfügbar.", cut.Instance.Hinweis);
    }

    // =================================================================================
    // Die Überlagerung „Auslegung" (Stufe Z2)
    // =================================================================================

    private static ZapfprofilAuslegungStartDaten AuslegungStart(ZapfprofilAuslegungEingabeDaten? eingabe) => new()
    {
        Kontext = "Summe aller Zonen",
        Eingabe = eingabe ?? new ZapfprofilAuslegungEingabeDaten { SpeicherC = 60 },
        Verfuegbar = true,
        Ergebnis = new ZapfprofilAuslegungDaten
        {
            Zustand = ZapfprofilAuslegungZustand.Gerechnet,
            Gruppen =
            {
                new ZapfprofilAuslegungsgruppeDaten
                {
                    Topologie = "Speicher",
                    Speicher = true,
                    Zonen = { "Zone 1" },
                    Hauptwert = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.Gerechnet },
                    Empfehlung = new ZapfprofilEmpfehlungDaten { Rechenbar = true, Speicher = true, VolumenL = 300, LeistungKw = 25 }
                }
            }
        }
    };

    private IRenderedComponent<ZapfprofilDialog> MitAuslegung(List<ZapfprofilEingabeDaten> geoeffnet,
                                                              Action<ZapfprofilErgebnisDaten?>? geschlossen = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, e => Vorschau(e))
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.AuslegungGaben, e =>
            {
                geoeffnet.Add(e);
                return new Dictionary<string, object>
                {
                    ["Daten"] = AuslegungStart(e.Auslegung),
                    ["Texte"] = new ZapfprofilAuslegungTexte(),
                    ["EntprellungMs"] = 0
                };
            })
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    [Fact]
    public void Ohne_Delegat_steht_Auslegung_weich_gesperrt()
    {
        var cut = Aufbauen();

        IElement auslegung = Knopf(cut, "Auslegung…");
        Assert.Equal("true", auslegung.GetAttribute("aria-disabled"));
        auslegung.Click();
        Assert.False(cut.Instance.AuslegungOffen);
        Assert.Equal("In dieser Fassung noch nicht verfügbar.", cut.Instance.Hinweis);
    }

    [Fact]
    public void Auslegung_oeffnet_zum_Arbeitsstand_und_OK_legt_Eingaben_samt_Punkt_hinein()
    {
        var geoeffnet = new List<ZapfprofilEingabeDaten>();
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = MitAuslegung(geoeffnet, e => ergebnis = e);

        IElement knopf = Knopf(cut, "Auslegung…");
        Assert.False(knopf.HasAttribute("aria-disabled"));
        knopf.Click();

        Assert.True(cut.Instance.AuslegungOffen);
        Assert.Equal(2, Assert.Single(geoeffnet).Zonen.Count);
        Assert.Contains("aria-label=\"Auslegung Brauchwasser\"", cut.Markup);   // der Titel steht an der Überlagerung

        IRenderedComponent<ZapfprofilAuslegungDialog> a = cut.FindComponent<ZapfprofilAuslegungDialog>();
        Assert.Empty(a.FindAll(".epos-dialog-titel"));
        Assert.Empty(a.FindAll(".epos-dialog-zu"));
        Knopf(a, "OK").Click();

        Assert.False(cut.Instance.AuslegungOffen);
        Assert.Equal(300, cut.Instance.Eingabe.Auslegung!.PunktVolumenL);
        Assert.Equal(25, cut.Instance.Eingabe.Auslegung.PunktLeistungKw);
        Assert.Contains("Auslegung übernommen: 300 l · 25,0 kW", cut.Find(".epos-zapfprofil-auslegungsstand").TextContent);

        // Ein zweites Öffnen bringt die übernommene Auslegung mit; Abbrechen verwirft nur die neue Runde.
        Knopf(cut, "Auslegung…").Click();
        Assert.Equal(300, geoeffnet[1].Auslegung!.PunktVolumenL);
        a = cut.FindComponent<ZapfprofilAuslegungDialog>();
        a.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == "Speichertemperatur")
         .QuerySelector("input")!.Input("50");
        Knopf(a, "Abbrechen").Click();
        Assert.False(cut.Instance.AuslegungOffen);
        Assert.Equal(60, cut.Instance.Eingabe.Auslegung!.SpeicherC);

        // Das OK des Zapfprofils trägt die Auslegung zum Wirt.
        Knopf(cut, "OK").Click();
        Assert.Equal(300, ergebnis!.Eingabe.Auslegung!.PunktVolumenL);
    }

    private static IElement Feld(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung)
        => cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung)
              .QuerySelector("input")!;

    [Fact]
    public void Eine_Zonenaenderung_macht_den_uebernommenen_Punkt_ueberholt()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = MitAuslegung(new List<ZapfprofilEingabeDaten>(), e => ergebnis = e);
        Knopf(cut, "Auslegung…").Click();
        Knopf(cut.FindComponent<ZapfprofilAuslegungDialog>(), "OK").Click();
        Assert.Equal(300, cut.Instance.Eingabe.Auslegung!.PunktVolumenL);

        // Der Name einer Zone ändert die Auslegung nicht — der Punkt bleibt.
        Feld(cut, "Zonenname").Input("Zone Nord");
        Assert.Equal(300, cut.Instance.Eingabe.Auslegung!.PunktVolumenL);
        Assert.False(cut.Instance.Eingabe.PunktUeberholt);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-punktueberholt"));

        // Die Bezugsgröße ändert sie: Der Punkt fällt, die leise Zeile sagt es.
        Feld(cut, "Bezugsgröße").Input("35");
        Assert.True(cut.Instance.Eingabe.PunktUeberholt);
        Assert.Null(cut.Instance.Eingabe.Auslegung!.PunktVolumenL);
        Assert.Null(cut.Instance.Eingabe.Auslegung.PunktLeistungKw);
        Assert.Equal(60, cut.Instance.Eingabe.Auslegung.SpeicherC);          // die Eingaben der Auslegung bleiben
        Assert.Contains("überholt", cut.Find(".epos-zapfprofil-punktueberholt").TextContent);
        Assert.Contains("Eingaben ohne Punkt", cut.Find(".epos-zapfprofil-auslegungsstand").TextContent);

        // Ein neues OK der Auslegung setzt den Punkt und hebt die Marke auf.
        Knopf(cut, "Auslegung…").Click();
        Knopf(cut.FindComponent<ZapfprofilAuslegungDialog>(), "OK").Click();
        Assert.False(cut.Instance.Eingabe.PunktUeberholt);
        Assert.Equal(300, cut.Instance.Eingabe.Auslegung!.PunktVolumenL);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-punktueberholt"));

        // Eine neue Zone macht ihn wieder überholt; das OK trägt die Marke zum Wirt.
        Knopf(cut, "Zone hinzufügen…").Click();
        Assert.True(cut.Instance.Eingabe.PunktUeberholt);
        Feld(cut, "Bezugsgröße").Input("10");
        Option(cut, "hoch").Change("3");
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis!.Eingabe.PunktUeberholt);
        Assert.Null(ergebnis.Eingabe.Auslegung!.PunktVolumenL);
    }

    [Fact]
    public void Ein_Punkt_des_Stands_beim_Oeffnen_wird_mit_einer_Zonenaenderung_ueberholt()
    {
        ZapfprofilDaten mitPunkt = Daten();
        mitPunkt.MitPunkt = true;
        var cut = Aufbauen(mitPunkt);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-punktueberholt"));

        Option(cut, "hoch").Change("3");
        Assert.True(cut.Instance.Eingabe.PunktUeberholt);
        Assert.Null(cut.Instance.Eingabe.Auslegung);                        // die Auslegung selbst bleibt unberührt
        Assert.Contains("beim Speichern verworfen", cut.Find(".epos-zapfprofil-punktueberholt").TextContent);

        // Ohne Punkt beim Öffnen ist nichts zu überholen.
        var ohne = Aufbauen();
        Option(ohne, "hoch").Change("3");
        Assert.False(ohne.Instance.Eingabe.PunktUeberholt);
        Assert.Empty(ohne.FindAll(".epos-zapfprofil-punktueberholt"));
    }

    [Fact]
    public void Esc_schliesst_bei_offener_Auslegung_nur_sie()
    {
        int gerufen = 0;
        var cut = MitAuslegung(new List<ZapfprofilEingabeDaten>(), _ => gerufen++);

        Knopf(cut, "Auslegung…").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });   // der Wirt: offen, also nichts
        Assert.True(cut.Instance.AuslegungOffen);
        Assert.Equal(0, gerufen);

        cut.FindComponent<ZapfprofilAuslegungDialog>().Find(".epos-zapfausl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.AuslegungOffen);
        Assert.Null(cut.Instance.Eingabe.Auslegung);
        Assert.Equal(0, gerufen);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });   // jetzt schließt der Wirt
        Assert.Equal(1, gerufen);
    }

    // =================================================================================
    // Hinweise und Meldungen
    // =================================================================================

    [Fact]
    public void Hinweise_stehen_am_Feld_und_blockieren_nicht()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        var hinweis = new ZapfprofilMeldung("ZPG_HINW_BEDARF_AUSSERHALB_BANDBREITE", "Zone 1",
                                            "Zone „Zone 1“: Bedarf außerhalb der Bandbreite.", ZapfprofilMeldungsart.Hinweis);
        var ablehnung = new ZapfprofilMeldung("ZPG_EINGABE_BEZUGSMENGE_FEHLT", "Zone 2",
                                              "Zone „Zone 2“ trägt 0: Bezugsmenge fehlt.", ZapfprofilMeldungsart.Ablehnung);
        var cut = Aufbauen(Daten(e, Vorschau(e, hinweis, ablehnung)));

        IElement feld = cut.Find(".epos-zapfprofil-feldhinweis");
        Assert.Equal("ZPG_HINW_BEDARF_AUSSERHALB_BANDBREITE", feld.GetAttribute("data-kennung"));
        Assert.Contains("Bedarf außerhalb der Bandbreite", feld.TextContent);

        // Die Ablehnung der nicht gewählten Zone steht benannt in der Vorschau.
        Assert.Contains("Zone „Zone 2“ trägt 0: Bezugsmenge fehlt.", cut.Find(".epos-warnbanner").TextContent);

        // Nicht blockierend: OK geht durch.
        ZapfprofilErgebnisDaten? ergebnis = null;
        cut = Aufbauen(Daten(e, Vorschau(e, hinweis)), geschlossen: x => ergebnis = x);
        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
    }

    /// <summary>
    /// Befund 8: Tragen zwei Zonen denselben Namen, ordnet die Position der Hülle die Meldung
    /// genau ihrer Zone zu; ohne Position steht sie an keiner der beiden, sondern bei den
    /// allgemeinen Meldungen — nie an beiden.
    /// </summary>
    [Fact]
    public void Meldungen_gehen_ueber_die_Position_an_ihre_Zone_auch_bei_gleichem_Namen()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        e.Zonen[1].Name = e.Zonen[0].Name;
        string name = e.Zonen[0].Name;
        var zweite = new ZapfprofilMeldung("ZPG_HINW_BEDARF_AUSSERHALB_BANDBREITE", name,
                                           "Hinweis der zweiten Zone.", ZapfprofilMeldungsart.Hinweis, "", 1);
        var ohnePosition = new ZapfprofilMeldung("ZPG_HINW_MESSWERT_ABWEICHUNG", name,
                                                 "Hinweis ohne Position.", ZapfprofilMeldungsart.Hinweis);
        var cut = Aufbauen(Daten(e, Vorschau(e, zweite, ohnePosition)));

        // Gewählt ist die erste Zone: an ihrem Feld keiner der beiden Hinweise.
        IElement[] amFeld = cut.FindAll(".epos-blockspalte:first-child .epos-zapfprofil-feldhinweis").ToArray();
        Assert.Empty(amFeld);
        Assert.Contains("Hinweis der zweiten Zone.", cut.Markup);
        Assert.Contains("Hinweis ohne Position.", cut.Markup);

        // Die zweite Zone gewählt: ihr Hinweis steht an ihrem Feld, der ohne Position nicht.
        cut.FindAll(".epos-zapfprofil-zonen tbody tr")[1].QuerySelector("button, input")!.Click();
        string[] feld = cut.FindAll(".epos-blockspalte:first-child .epos-zapfprofil-feldhinweis")
                           .Select(x => x.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Hinweis der zweiten Zone." }, feld);
        Assert.Single(cut.FindAll(".epos-zapfprofil-feldhinweis"), x => x.TextContent.Contains("Hinweis ohne Position."));
    }

    [Fact]
    public void Ohne_Vorschau_steht_der_Grund_statt_einer_Zahl()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        var v = new ZapfprofilVorschauDaten
        {
            Zustand = ZapfprofilVorschauZustand.Abgebrochen,
            Grund = "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau.",
            Status = "Keine Vorschau — Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."
        };
        var cut = Aufbauen(Daten(e, v), vorschau: _ => v);

        Assert.Contains("Keine Vorschau — Das Projekt hat keine Klimaregion", cut.Find(".epos-zapfprofil-ohnevorschau").TextContent);
        Assert.Empty(cut.FindAll("[role=tab]"));
        Assert.DoesNotContain("70,0", cut.Find("table.epos-zapfprofil-zonen").TextContent);
    }

    [Fact]
    public void Ein_nicht_verfuegbarer_Generator_nennt_seinen_Grund()
    {
        var daten = new ZapfprofilDaten
        {
            Verfuegbar = false,
            Sperrgrund = "Der Zapfprofilgenerator ist in dieser Datenbank nicht verfügbar — es fehlen seine Tabellen."
        };
        var cut = Aufbauen(daten, vorschau: _ => new ZapfprofilVorschauDaten());

        Assert.Contains("es fehlen seine Tabellen", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Bearbeiten und Vorschau
    // =================================================================================

    [Fact]
    public void Eine_Eingabe_rechnet_die_Vorschau_mit_dem_Arbeitsstand()
    {
        var gerechnet = new List<ZapfprofilEingabeDaten>();
        var cut = Aufbauen(vorschau: e => { gerechnet.Add(e); return Vorschau(e); });

        cut.Find("input[inputmode=decimal]").Input("30");

        ZapfprofilEingabeDaten letzte = Assert.Single(gerechnet);
        Assert.Equal(30, letzte.Zonen[0].Bezugsmenge);
        Assert.Contains("80,0", cut.Find("table.epos-zapfprofil-zonen tfoot").TextContent);

        // Das Niveau ist eine Wahl im Arbeitsstand.
        Option(cut, "hoch").Change("3");
        Assert.Equal(ZapfprofilNiveau.Hoch, cut.Instance.Eingabe.Zonen[0].Niveau);
        Assert.Equal(2, gerechnet.Count);
    }

    [Fact]
    public void Zonen_lassen_sich_anlegen_duplizieren_und_entfernen()
    {
        var cut = Aufbauen();

        Knopf(cut, "Zone hinzufügen…").Click();
        Assert.Equal(3, cut.Instance.Eingabe.Zonen.Count);
        Assert.Equal("Zone 3", cut.Instance.Eingabe.Zonen[2].Name);
        Assert.Contains("Zone 3 · Nutzung", cut.Markup);

        cut.FindAll("table.epos-zapfprofil-zonen tbody button")[0].Click();
        Knopf(cut, "Duplizieren").Click();
        ZapfprofilZoneDaten kopie = cut.Instance.Eingabe.Zonen[1];
        Assert.Equal("Zone 1 (Kopie)", kopie.Name);
        Assert.Equal(0, kopie.Id);
        Assert.Equal(11, kopie.IdVorlage);

        Knopf(cut, "Entfernen").Click();
        Assert.Equal(new[] { "Zone 1", "Zone 2", "Zone 3" }, cut.Instance.Eingabe.Zonen.Select(z => z.Name).ToArray());
    }

    // =================================================================================
    // Rueckweg
    // =================================================================================

    [Fact]
    public void Ok_ohne_Bezugsmenge_zeigt_den_Banner_und_bleibt_offen()
    {
        bool gerufen = false;
        var fehler = new ZapfprofilMeldung("ZPG_MSG_ZONE_OHNE_BEZUGSMENGE", "Zone 1",
                                           "Zone „Zone 1“: Bitte eine Bezugsgröße größer 0 eingeben.", ZapfprofilMeldungsart.Fehler);
        var cut = Aufbauen(pruefen: _ => new[] { fehler }, geschlossen: _ => gerufen = true);

        Knopf(cut, "OK").Click();

        Assert.False(gerufen);
        Assert.Equal("Zone „Zone 1“: Bitte eine Bezugsgröße größer 0 eingeben.", cut.Instance.OkMeldung);
        Assert.Contains("Bitte eine Bezugsgröße größer 0 eingeben.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ok_uebergibt_den_Stand_mit_dem_Weg_Generator_und_schreibt_nicht()
    {
        ZapfprofilDaten daten = Daten();
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = Aufbauen(daten, geschlossen: x => ergebnis = x);

        cut.Find("input[inputmode=decimal]").Input("25");
        Knopf(cut, "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(ZapfprofilWeg.Generator, ergebnis!.Weg);
        Assert.Equal(25, ergebnis.Eingabe.Zonen[0].Bezugsmenge);
        Assert.Equal(2, ergebnis.Eingabe.Zonen.Count);

        // Der Stand des Wirtes bleibt unberührt (Arbeitsstand auf Kopien).
        Assert.Equal(20, daten.Eingabe.Zonen[0].Bezugsmenge);
        Assert.Equal(ZapfprofilWeg.Bestand, daten.Eingabe.Weg);
    }

    [Fact]
    public void Abbrechen_Esc_und_Kreuz_verwerfen_mit_null()
    {
        int gerufen = 0;
        ZapfprofilErgebnisDaten? ergebnis = new(new ZapfprofilEingabeDaten());
        var cut = Aufbauen(geschlossen: x => { gerufen++; ergebnis = x; });

        Knopf(cut, "Abbrechen").Click();
        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gerufen);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(3, gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Eingebettet_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titel: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.Equal(2, cut.FindAll(".epos-infoknopf").Count);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_mit_Grund_statt_Zahlen()
    {
        var cut = Render<ZapfprofilDialog>();

        Assert.Contains("Brauchwasser-Zapfprofil", cut.Markup);
        Assert.Empty(cut.FindAll("table.epos-zapfprofil-zonen tbody tr"));
        Assert.NotNull(cut.Find(".epos-zapfprofil-ohnevorschau"));
        Assert.NotNull(Knopf(cut, "Zone hinzufügen…"));
    }

    // =================================================================================
    // Wache des Einstiegsbündels im Bedarfsprofil-Dialog
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c>[^\r\n]*</summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \}\s*=\s*""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    /// <summary>
    /// Jede Beschriftung von <see cref="ZapfprofilEinstiegTexte"/> nennt ihren Schlüssel; er
    /// steht in beiden Sprachen, und der Vorgabewert ist der deutsche Text der neutralen
    /// Ressource.
    /// </summary>
    [Fact]
    public void Das_Einstiegsbuendel_steht_mit_seinen_Schluesseln_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", "BedarfsProfileDaten.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).Cast<Match>()
            .Where(m => typeof(ZapfprofilEinstiegTexte).GetProperty(m.Groups["name"].Value) is not null).ToList();

        string[] eigenschaften = typeof(ZapfprofilEinstiegTexte)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(eigenschaften, treffer.Select(m => m.Groups["name"].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        var vorgabe = new ZapfprofilEinstiegTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string schluessel = m.Groups["schluessel"].Value;
            string deutsch = Resource.ResourceManager.GetString(schluessel, new CultureInfo("de-DE")) ?? "";
            string englisch = Resource.ResourceManager.GetString(schluessel, new CultureInfo("en-US")) ?? "";
            string rueckfall = (string)typeof(ZapfprofilEinstiegTexte).GetProperty(m.Groups["name"].Value)!.GetValue(vorgabe)!;
            if (englisch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
            if (!string.Equals(deutsch, rueckfall, StringComparison.Ordinal))
                funde.Add(schluessel + ": Rückfall „" + rueckfall + "“ ≠ Ressource „" + deutsch + "“");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));

        // Die eine Stelle, die „Trinkwarmwasser" ausschreibt; sonst heißt es Brauchwasser.
        Assert.Contains("Brauchwasser (Trinkwarmwasser)", vorgabe.HinweisRechenweg);
    }

    private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

    private static string Wurzel([CallerFilePath] string eigeneDatei = "")
    {
        string? ordner = Path.GetDirectoryName(eigeneDatei);
        while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
            ordner = Path.GetDirectoryName(ordner);
        Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!;
    }
}
