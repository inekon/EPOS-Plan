using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Gebäudetypen-Verwaltung (iU9-W8.4) — seit Stufe 5 der Neuordnung der
/// Administrationsdialoge (V16, Bestand A10) im Gerüst der Verwaltungen: Typliste als
/// Katalogliste (Zeile ist Wahl, Schloss), Stammblatt mit der Gruppe „Tagesprofil"
/// (Klappliste „Kurve", Tagesbild, „Stundenwerte…" als Überlagerung) und den Kenndaten,
/// Auswahlleiste mit Vergleichen, Duplizieren… und Löschen, Fußleiste Speichern ·
/// Verwerfen · Status · Neu… · Beenden.
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class GebaeudetypDialogTests : EposBunitContext
{
    private static readonly string[] TYPEN = { "Buerogebaeude", "Hotel", "Wohngebaeude VDI 2067" };

    private static readonly string[] KURZ =
    { "Winter-heiter", "Winter-trübe", "Übergang-heiter", "Übergang-trübe", "Sommertag" };

    private static readonly string[] LANG =
    { "Winter-Wochentag", "Winter-Wochenende", "Übergang1-Wochentag", "Übergang1-Wochenende",
      "Sommer-Wochentag", "Sommer-Wochenende", "Übergang2-Wochentag", "Übergang2-Wochenende" };

    /// <summary>Das Zeichenmodell des Tagesprofils — EINE Instanz (der Baustein vergleicht die Referenz).</summary>
    private static readonly Zeichenmodell MODELL = Stundenprofil();

    private static Zeichenmodell Stundenprofil()
    {
        var werte = new double[24];
        for (int s = 0; s < 24; s++) werte[s] = s + 1;
        return ChartRenderer.StundenprofilModell("Stundenverteilung", werte, 1, "Stunde", "Anteil [%]");
    }

    public GebaeudetypDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Wie viele Kurven ein Testtyp führt: der Wohntyp fünf, die übrigen acht.</summary>
    private static int Kurven(string name) => name.StartsWith("Wohn") ? 5 : 8;

    /// <summary>Ein Auslieferungstyp: der Wohntyp (nicht veränderbar).</summary>
    private static bool Aenderbar(string name) => !name.StartsWith("Wohn");

    /// <summary>Ein Typ, dessen Werte die laufende Nummer 1…n × 24 SIND.</summary>
    private static GebaeudetypDaten Typ(string name, int kurven, bool aenderbar = true, int id = 7)
    {
        var v = new double[kurven, 24];
        for (int n = 0; n < kurven; n++)
            for (int s = 0; s < 24; s++) v[n, s] = n * 24 + s + 1;

        return new GebaeudetypDaten
        {
            Id = id,
            Name = name,
            Beschreibung = "Beschreibung " + name,
            Aenderbar = aenderbar,
            Verteilung = v,
            Kurvennamen = kurven <= 5 ? KURZ.Take(kurven).ToList() : LANG.Take(kurven).ToList()
        };
    }

    private static IReadOnlyList<Katalogfilterzeile> Zeilen(IEnumerable<string> typen)
        => typen.Select((t, i) => new Katalogfilterzeile(i + 1, t) { Geschuetzt = !Aenderbar(t) }
                .MitText(Katalogfilterprofil.SpBezeichner, t)
                .MitZahl(Katalogfilterprofil.SpKurven, Kurven(t), 0)
                .MitText(Katalogfilterprofil.SpBeschreibung, "Beschreibung " + t))
            .ToList();

    /// <summary>Was die Wege gerufen haben.</summary>
    private sealed class Protokoll
    {
        internal List<string> Typen = TYPEN.ToList();
        internal readonly List<(int Id, double[,] Verteilung)> Verteilungen = new();
        internal readonly List<(int Id, string Text)> Beschreibungen = new();
        internal readonly List<(string Name, string Beschreibung, int Kurven)> Angelegt = new();
        internal readonly List<int> Geloescht = new();
        internal readonly List<(int Id, string Name)> Dupliziert = new();
        internal readonly List<double[]> Bilder = new();
        internal bool? Geschlossen;
    }

    private IRenderedComponent<GebaeudetypDialog> Aufbauen(
        Protokoll? p = null,
        string titel = "Gebäudetypen Verwaltung",
        IReadOnlyDictionary<string, int>? verwendung = null,
        bool mitBild = true,
        Schlosspruefung? schloss = null)
    {
        Protokoll pr = p ?? new Protokoll();
        return Render<GebaeudetypDialog>(b => b
            .Add(x => x.TitelText, titel)
            .Add(x => x.Katalogzeilen, () => schloss is null ? Zeilen(pr.Typen) : schloss.Markieren(Zeilen(pr.Typen)))
            .Add(x => x.Schloss, schloss?.Weg())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeudetyp(
                s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, n => pr.Typen.Contains(n)
                ? Typ(n, Kurven(n),
                      schloss is null ? Aenderbar(n) : !schloss.Gesperrt.Contains(pr.Typen.IndexOf(n) + 1),
                      pr.Typen.IndexOf(n) + 1)
                : null)
            .Add(x => x.Speichern, (id, v) => { pr.Verteilungen.Add((id, v)); return true; })
            .Add(x => x.BeschreibungSpeichern, (id, t) => { pr.Beschreibungen.Add((id, t)); return true; })
            .Add(x => x.Anlegen, (n, t, k) => { pr.Angelegt.Add((n, t, k)); pr.Typen.Add(n); return 42; })
            .Add(x => x.Loeschen, id => { pr.Geloescht.Add(id); pr.Typen.RemoveAt(id - 1); return true; })
            .Add(x => x.Duplizieren, (id, n) =>
            {
                pr.Dupliziert.Add((id, n));
                pr.Typen.Add(n);
                return new KatalogSpeicherErgebnis(true, "", n);
            })
            .Add(x => x.Verwendung, () => verwendung ?? new Dictionary<string, int>())
            .Add(x => x.Bild, mitBild ? w => { pr.Bilder.Add(w); return MODELL; } : null)
            .Add(x => x.Geschlossen, e => pr.Geschlossen = e));
    }

    private static IElement Knopf(IRenderedComponent<GebaeudetypDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<GebaeudetypDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == text);

    private static IElement Fussleiste(IRenderedComponent<GebaeudetypDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    private static IElement Kurvenwahl(IRenderedComponent<GebaeudetypDialog> cut)
        => cut.Find(".epos-stammblatt select");

    // =================================================================================
    // Gerüst und Feldbestand
    // =================================================================================

    /// <summary>
    /// <b>Das Gerüst</b>: Katalogliste mit drei Spalten statt der Typliste mit Wahlknopf,
    /// Stammblatt mit Tagesprofil und Kenndaten, Fußleiste Speichern · Verwerfen · Neu… ·
    /// Beenden (einziger primärer Knopf zuletzt).
    /// </summary>
    [Fact]
    public void Das_Geruest_steht_wie_bei_den_Verwaltungen()
    {
        var cut = Aufbauen();

        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Equal(new[] { "Name", "Tageskurven", "Beschreibung" },
                     cut.FindAll(".epos-katalogliste thead .epos-spaltenkopf-text").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-zeilenwahl"));

        Assert.Equal(new[] { "Tagesprofil", "Kenndaten" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu…", "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(Fussleiste(cut).QuerySelectorAll("button.epos-knopf--primaer"));
        Assert.Same(Fussleiste(cut).QuerySelectorAll("button").Last(),
                    Fussleiste(cut).QuerySelectorAll("button.epos-knopf--primaer")[0]);
    }

    /// <summary>Ohne Gaben zeichnet der Dialog — leere Liste, Platzhalter, nur „Beenden".</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<GebaeudetypDialog>();

        Assert.Equal("Gebäudetypen Verwaltung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(new[] { "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Ohne Katalogzeilen baut der Dialog die Liste aus der blossen Namensliste (<c>Typen</c>)
    /// — der Weg eines Wirts, der das Profil noch nicht liefert.
    /// </summary>
    [Fact]
    public void Ohne_Katalogzeilen_traegt_die_Namensliste()
    {
        var cut = Render<GebaeudetypDialog>(b => b
            .Add(x => x.Typen, () => TYPEN)
            .Add(x => x.Lies, n => Typ(n, 8)));

        Assert.Equal(TYPEN, cut.Instance.Typliste);
        Assert.Equal(TYPEN[0], cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Fünf und acht Kurven tragen verschiedene Namen</b> — entschieden über die
    /// KURVENZAHL; die Klappliste „Kurve" zeigt sie.
    /// </summary>
    [Fact]
    public void Fuenf_und_acht_Kurven_stehen_in_der_Klappliste()
    {
        var cut = Aufbauen();

        Assert.Equal(LANG, Kurvenwahl(cut).QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());

        Zeilenklick.Zeile(cut, 2);                               // Wohngebaeude, fuenf Kurven

        Assert.Equal(KURZ, Kurvenwahl(cut).QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());
        Assert.Equal("5 Tageskurven · Auslieferungssatz", cut.Find(".epos-stammblatt-unter").TextContent);
    }

    /// <summary>Die Kurvenwahl zeichnet das Bild der gewählten Kurve (24 Werte).</summary>
    [Fact]
    public void Die_Kurvenwahl_zeichnet_das_Bild_der_Kurve()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Kurvenwahl(cut).Change("2");

        Assert.Equal(2, cut.Instance.Kurvenwahl);
        Assert.Equal(24, p.Bilder.Last().Length);
        Assert.Equal(49, p.Bilder.Last()[0]);                    // 2 * 24 + 1
        Assert.NotEmpty(cut.FindAll(".epos-stammblatt .epos-diagramm-svg"));
    }

    /// <summary>Ohne Bilddelegat steht der Platzhalter.</summary>
    [Fact]
    public void Ohne_Bilddelegat_steht_der_Platzhalter()
    {
        var cut = Aufbauen(mitBild: false);

        Assert.Contains("Kein Diagramm vorhanden", cut.Find(".epos-stammblatt").TextContent);
    }

    // =================================================================================
    // Die Stundenwerte als Überlagerung
    // =================================================================================

    /// <summary>
    /// <b>„Stundenwerte…"</b> öffnet die 24 Felder der gewählten Kurve als Überlagerung mit
    /// Titel und GENAU einem Kreuz; „Übernehmen" legt sie in den Arbeitsstand — geschrieben
    /// wird erst mit „Speichern".
    /// </summary>
    [Fact]
    public void Stundenwerte_oeffnen_als_Ueberlagerung_und_gehen_in_den_Arbeitsstand()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Knopf(cut, "Stundenwerte…").Click();

        Assert.True(cut.Instance.StundenwerteOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-zu"));
        Assert.Equal("Stundenwerte – Winter-Wochentag", cut.Find(".epos-ueberlagerung-titel").TextContent);
        var felder = cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]");
        Assert.Equal(24, felder.Count);
        Assert.Equal(1.0, cut.Instance.Felder[0]);

        felder[6].Input("99");
        Knopf(cut, "Übernehmen").Click();

        Assert.False(cut.Instance.StundenwerteOffen);
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal(99, cut.Instance.Kurvenwerte[6]);
        Assert.Empty(p.Verteilungen);
        Assert.Equal("1 Feld geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);
    }

    /// <summary>Ein leeres Stundenfeld hält „Übernehmen" an und nennt die Stunde.</summary>
    [Fact]
    public void Ein_leeres_Stundenfeld_nennt_die_Stunde()
    {
        var cut = Aufbauen();
        Knopf(cut, "Stundenwerte…").Click();

        cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]")[4].Input("");
        Knopf(cut, "Übernehmen").Click();

        Assert.True(cut.Instance.StundenwerteOffen);
        Assert.Contains("Stunde 5", cut.Find(".epos-ueberlagerung").TextContent);
        Assert.False(cut.Instance.Geaendert);
    }

    /// <summary>Das Kreuz der Überlagerung verwirft — nichts geht in den Arbeitsstand.</summary>
    [Fact]
    public void Das_Kreuz_der_Stundenwerte_verwirft()
    {
        var cut = Aufbauen();
        Knopf(cut, "Stundenwerte…").Click();
        cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]")[0].Input("77");

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.StundenwerteOffen);
        Assert.False(cut.Instance.Geaendert);
        Assert.Equal(1, cut.Instance.Kurvenwerte[0]);
    }

    /// <summary>
    /// <b>Ein Kurvenwechsel mit geänderten Werten hält an</b> wie ein Zeilenwechsel (Konzept
    /// 3.6 Punkt 3) — der stille Übertrag des Vorläufers entfällt.
    /// </summary>
    [Fact]
    public void Ein_Kurvenwechsel_mit_Aenderungen_haelt_an()
    {
        var cut = Aufbauen();
        Knopf(cut, "Stundenwerte…").Click();
        cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]")[0].Input("5");
        Knopf(cut, "Übernehmen").Click();

        Kurvenwahl(cut).Change("3");
        Assert.Equal(0, cut.Instance.Kurvenwahl);

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Buerogebaeude", cut.Instance.Gewaehlt);

        Assert.Contains("ungespeicherte", cut.Instance.Meldung);
    }

    // =================================================================================
    // Speichern, Verwerfen, Auslieferung
    // =================================================================================

    /// <summary>Speichern schreibt Verteilung und Beschreibung über ihre Wege; die Statuszeile meldet es.</summary>
    [Fact]
    public void Speichern_schreibt_Verteilung_und_Beschreibung()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Knopf(cut, "Stundenwerte…").Click();
        cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]")[23].Input("0,5");
        Knopf(cut, "Übernehmen").Click();
        cut.Find(".epos-stammblatt textarea").Input("neu");

        Knopf(cut, "Speichern").Click();

        var (id, v) = Assert.Single(p.Verteilungen);
        Assert.Equal(1, id);
        Assert.Equal(0.5, v[0, 23]);
        Assert.Equal((1, "neu"), Assert.Single(p.Beschreibungen));
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    /// <summary>Verwerfen nimmt Stundenwerte und Beschreibung zurück.</summary>
    [Fact]
    public void Verwerfen_nimmt_den_Arbeitsstand_zurueck()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        cut.Find(".epos-stammblatt textarea").Input("anders");
        Assert.True(cut.Instance.Geaendert);

        Knopf(cut, "Verwerfen").Click();

        Assert.False(cut.Instance.Geaendert);
        Assert.Empty(p.Beschreibungen);
    }

    /// <summary>
    /// <b>Ein Auslieferungstyp</b> (V13): das Schloss an der Zeile und im Kopf statt der
    /// Herleitungszeile, die Kenndaten als Text, „Speichern" und „Löschen" weich gesperrt;
    /// die Stundenwerte stehen nur lesend.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungstyp_ist_nur_lesbar()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 2);                               // Wohngebaeude, nicht veraenderbar

        Assert.NotEmpty(cut.FindAll(".epos-katalogliste .epos-schloss"));
        Assert.NotNull(cut.Find(".epos-stammblatt-kopf .epos-schloss"));
        Assert.Empty(cut.FindAll(".epos-stammblatt textarea"));
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
        Assert.Equal("true", Handlung(cut, "Löschen").GetAttribute("aria-disabled"));

        // Der Versuch nennt den Grund im Band UND rot in der Statuszeile neben dem Knopf.
        Knopf(cut, "Speichern").Click();
        var status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Equal(cut.Instance.Meldung, status.TextContent);
        Assert.NotEqual("", status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);

        Knopf(cut, "Stundenwerte…").Click();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung input[inputmode=decimal]"));
        Assert.Contains("nicht geändert werden", cut.Find(".epos-ueberlagerung").TextContent);
    }

    // =================================================================================
    // Neu, Duplizieren, Löschen, Vergleichen
    // =================================================================================

    /// <summary>
    /// <b>„Neu…"</b> fragt Name, Beschreibung und die Kurvenzahl (fünf oder acht); der neue
    /// Typ ist danach gewählt, die Statuszeile nennt ihn.
    /// </summary>
    [Fact]
    public void Neu_fragt_Name_Beschreibung_und_Kurvenzahl()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Knopf(cut, "Neu…").Click();
        Assert.True(cut.Instance.Namensfrage);
        Assert.Equal(2, cut.FindAll(".epos-ueberlagerung input[type=radio]").Count);

        cut.Find(".epos-ueberlagerung input[type=text]").Input("Kita");
        cut.Find(".epos-ueberlagerung textarea").Input("Tagesstaette");
        cut.FindAll(".epos-ueberlagerung input[type=radio]")[1].Change("5");   // fuenf
        Knopf(cut, "OK").Click();

        Assert.Equal(("Kita", "Tagesstaette", 5), Assert.Single(p.Angelegt));
        Assert.Equal("Kita", cut.Instance.Gewaehlt);
        Assert.Contains("Kita", cut.Instance.Status);
    }

    /// <summary>Ein belegter Name meldet in der Abfrage und legt nichts an.</summary>
    [Fact]
    public void Ein_belegter_Name_meldet_und_legt_nichts_an()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Knopf(cut, "Neu…").Click();

        cut.Find(".epos-ueberlagerung input[type=text]").Input("Hotel");
        Knopf(cut, "OK").Click();

        Assert.Empty(p.Angelegt);
        Assert.True(cut.Instance.Namensfrage);
        Assert.Contains("Name existiert bereits!", cut.Find(".epos-ueberlagerung").TextContent);
    }

    /// <summary>
    /// <b>Duplizieren…</b> (AD-Q11) — auch eines Auslieferungstyps: Namensabfrage mit
    /// „… (Kopie)", danach ist die Kopie gewählt.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_den_eigenen_Typ_an_und_waehlt_ihn()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Zeilenklick.Zeile(cut, 2);

        Handlung(cut, "Duplizieren...").Click();
        Assert.True(cut.Instance.Duplizierfrage);
        Knopf(cut, "OK").Click();

        Assert.Equal((3, "Wohngebaeude VDI 2067 (Kopie)"), Assert.Single(p.Dupliziert));
        Assert.Equal("Wohngebaeude VDI 2067 (Kopie)", cut.Instance.Gewaehlt);
        Assert.Contains("dupliziert", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Löschen fragt zurück</b> (der Vorläufer löschte ohne, A‑8) und schreibt nach dem
    /// „Ja"; die Statuszeile nennt den Typ.
    /// </summary>
    [Fact]
    public void Loeschen_fragt_zurueck_und_meldet()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);

        Handlung(cut, "Löschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("Buerogebaeude", cut.Find(".epos-rueckfrage").TextContent);
        Assert.Empty(p.Geloescht);

        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { 1 }, p.Geloescht);
        Assert.Contains("Buerogebaeude", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Die Verwendungssperre</b>: Ein Typ, den Gebäude des Katalogs führen, ist weich
    /// gegen Löschen gesperrt; der Kurztext nennt die Zahl.
    /// </summary>
    [Fact]
    public void Loeschen_ist_gesperrt_wenn_Gebaeude_den_Typ_fuehren()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p, verwendung: new Dictionary<string, int> { ["Buerogebaeude"] = 21 });

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("21", loeschen.GetAttribute("title"));

        loeschen.Click();
        Assert.False(cut.Instance.Loeschfrage);
        Assert.Empty(p.Geloescht);
    }

    /// <summary>Zwei Kästchen und „Vergleichen" legen dieselbe Kurve der Typen nebeneinander.</summary>
    [Fact]
    public void Vergleichen_legt_dieselbe_Kurve_nebeneinander()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[0].Change(true);
        cut.FindAll(".epos-katalogliste tbody td.epos-spalte-kaestchen input")[1].Change(true);

        Handlung(cut, "Vergleichen").Click();

        Assert.True(cut.Instance.Vergleicht);
        Assert.Contains(cut.Instance.Vergleichszeilen, z => z.Name == "Stunde 1");
        Assert.NotNull(cut.Find(".epos-stammblatt .epos-vergleichstabelle"));
    }

    // =================================================================================
    // Schluss
    // =================================================================================

    /// <summary>Beenden, Esc und das Kreuz schließen mit <c>true</c>; Enter tut nichts.</summary>
    [Fact]
    public void Beenden_Esc_und_Kreuz_schliessen_Enter_nicht()
    {
        var p1 = new Protokoll();
        Knopf(Aufbauen(p1), "Beenden").Click();
        Assert.True(p1.Geschlossen);

        var p2 = new Protokoll();
        var cut2 = Aufbauen(p2);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Null(p2.Geschlossen);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(p2.Geschlossen);

        var p3 = new Protokoll();
        Aufbauen(p3).Find(".epos-dialog-zu").Click();
        Assert.True(p3.Geschlossen);
    }

    /// <summary>Esc schließt nicht, solange die Stundenwerte offen stehen.</summary>
    [Fact]
    public void Esc_schliesst_nicht_bei_offenen_Stundenwerten()
    {
        var p = new Protokoll();
        var cut = Aufbauen(p);
        Knopf(cut, "Stundenwerte…").Click();

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(p.Geschlossen);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel (eingebettet) weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titel: "");

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der Zeuge dieser Maske an der Maskenbrücke</b>: Der Typ ist eine WAHL, ein Setzen
    /// wählt seine Zeile — derselbe Weg wie ein Klick in die Liste; die Kurve ist die zweite
    /// Wahl.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_waehlt_den_Typ()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.GEBAEUDETYP));

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDETYP, "typ");
        Assert.NotNull(zugang);
        Assert.Equal(TYPEN[0], zugang.Lesen());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, TYPEN[1]);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(TYPEN[1], zugang.Lesen());
        Assert.Equal(TYPEN[1], cut.Instance.Gewaehlt);

        WindowsFormsApplication1.KiFeldzugang kurve =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDETYP, "kurve");
        Assert.NotNull(kurve);
        Assert.True(kurve.Setzbar);
        Assert.Equal(0, kurve.Lesen());
    }

    /// <summary>
    /// <b>Die Beschreibung ist setzbar</b> (Welle #458, Nachzug der Feldkarte): Sie geht
    /// denselben Weg wie eine Eingabe im Stammblatt — in den Arbeitsstand, gespeichert
    /// wird mit „Speichern". Ein Auslieferungstyp meldet den Schreibschutz und nimmt
    /// nichts an.
    /// </summary>
    [Fact]
    public void Die_Beschreibung_ist_setzbar_und_ein_Auslieferungstyp_geschuetzt()
    {
        var cut = Aufbauen();

        WindowsFormsApplication1.KiFeldzugang feld =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDETYP, "beschreibung");
        Assert.NotNull(feld);
        Assert.True(feld.Setzbar);

        // Der Auslieferungstyp: geschuetzt, und die Maske nimmt die Eingabe nicht an.
        Zeilenklick.Zeile(cut, 2);
        Assert.True(KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDETYP).IstSchreibgeschuetzt());
        string vorher = (string)feld.Lesen()!;
        cut.InvokeAsync(() => feld.Setzen("Gegenprobe"));
        Assert.Equal(vorher, feld.Lesen());

        // Ein eigener Typ: Die Beschreibung geht in den Arbeitsstand und steht im Stammblatt.
        Zeilenklick.Zeile(cut, 1);
        Assert.False(KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDETYP).IstSchreibgeschuetzt());

        cut.InvokeAsync(() => feld.Setzen("Neu beschrieben"));
        cut.Render();

        Assert.Equal("Neu beschrieben", feld.Lesen());
        Assert.Equal("Neu beschrieben", cut.Find(".epos-stammblatt textarea").GetAttribute("value")
                                        ?? cut.Find(".epos-stammblatt textarea").TextContent);
    }

    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss eines Gebäudetyps aufheben</b> (AD-Q15): Ein Auslieferungstyp (nicht
    /// veränderbar) wird nach dem „Ja" ein eigener — Stundenwerte und Speichern sind frei, das
    /// Stammblatt trägt das Band; wieder gesetzt ist er gesperrt wie zuvor.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_und_wieder_setzen()
    {
        var pr = new Protokoll();
        int wohn = pr.Typen.FindIndex(t => !Aenderbar(t)) + 1;
        var schloss = new Schlosspruefung(wohn);
        var cut = Aufbauen(pr, schloss: schloss);

        Zeilenklick.Zeile(cut, wohn - 1);
        Assert.Equal("Schloss aufheben...", Schlosspruefung.Beschriftung(cut));

        Schlosspruefung.Knopf(cut).Click();
        Schlosspruefung.Ja(cut);

        Assert.False(schloss.Aufrufe.Single().Gesperrt);
        Assert.True(Schlosspruefung.Band(cut));
        Assert.Empty(cut.FindAll(".epos-stammblatt-name .epos-schloss"));
        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));

        Schlosspruefung.Knopf(cut).Click();
        Schlosspruefung.Ja(cut);

        Assert.True(schloss.Aufrufe[^1].Gesperrt);
        Assert.False(Schlosspruefung.Band(cut));
        Assert.NotEmpty(cut.FindAll(".epos-stammblatt-name .epos-schloss"));
    }

    // =================================================================================
    // Die Zahlenreihe „stundenwerte" (Welle #458 Stufe 3b)
    // =================================================================================

    /// <summary>
    /// <b>Die 24 Stundenwerte der GEWÄHLTEN Kurve sind EINE Zahlenreihe</b> im
    /// Arbeitsstand: gelesen, ganz und je Stunde gesetzt — und danach hält die Maske den
    /// Kurvenwechsel an, bis gespeichert oder verworfen ist, wie nach einer Eingabe von
    /// Hand.
    /// </summary>
    [Fact]
    public void Die_Stundenwerte_der_gewaehlten_Kurve_liest_und_setzt_der_Assistent_als_Reihe()
    {
        var cut = Aufbauen();
        const string maske = KiMaskennamen.GEBAEUDETYP;

        Assert.Equal(Hilfe.KiReihenhilfe.Folge(24).Select(w => (double?)w),
                     Hilfe.KiReihenhilfe.Werte(maske, "stundenwerte"));

        cut.InvokeAsync(() => Hilfe.KiReihenhilfe.Setze(maske, "stundenwerte", Hilfe.KiReihenhilfe.Gleich(24, 3.0)));
        cut.InvokeAsync(() => Hilfe.KiReihenhilfe.Setze(maske, "stundenwerte", new[] { 9.0 }, ab: 24));
        cut.Render();

        Assert.Equal(3.0, cut.Instance.Kurvenwerte[0]);
        Assert.Equal(9.0, cut.Instance.Kurvenwerte[23]);
        Assert.NotNull(Hilfe.KiReihenhilfe.Grund(maske, "stundenwerte", new double[25]));

        // Ungespeichert hält die Maske den Kurvenwechsel an.
        WindowsFormsApplication1.KiFeldzugang kurve = KiMaskenbruecke.Feldzugang(maske, "kurve")!;
        cut.InvokeAsync(() => kurve.Setzen(1));
        Assert.Equal(0, cut.Instance.Kurvenwahl);
    }

    /// <summary>
    /// <b>„Speichern" schreibt die gesetzte Kurve</b> — über den Weg der Maske; die übrigen
    /// Kurven gehen unverändert mit.
    /// </summary>
    [Fact]
    public async Task Speichern_schreibt_die_gesetzten_Stundenwerte()
    {
        var pr = new Protokoll();
        var cut = Aufbauen(pr);

        await cut.InvokeAsync(() => Hilfe.KiReihenhilfe.Setze(KiMaskennamen.GEBAEUDETYP, "stundenwerte",
                                                              Hilfe.KiReihenhilfe.Folge(24, 0.5)));
        KiKern.KiErgebnis ergebnis =
            await cut.InvokeAsync(() => KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDETYP).Speichern());

        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        double[,] verteilung = pr.Verteilungen.Single().Verteilung;
        Assert.Equal(0.5, verteilung[0, 0]);
        Assert.Equal(12.0, verteilung[0, 23]);
        Assert.Equal(25.0, verteilung[1, 0]);
    }

    /// <summary>
    /// <b>Ein Auslieferungstyp nimmt keine Reihe an</b> — der Haken meldet den Schutz, und
    /// der Setzweg selbst lehnt mit dem Grund der Maske ab.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungstyp_nimmt_keine_Stundenwerte_an()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 2);

        Assert.True(KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDETYP).IstSchreibgeschuetzt());
        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.GEBAEUDETYP, "stundenwerte")!;
        double?[] vorher = Hilfe.KiReihenhilfe.Werte(KiMaskennamen.GEBAEUDETYP, "stundenwerte");

        Assert.Throws<InvalidOperationException>(() => zugang.Setzen(new double?[24]));
        Assert.Equal(vorher, Hilfe.KiReihenhilfe.Werte(KiMaskennamen.GEBAEUDETYP, "stundenwerte"));
    }
}
