using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Anmutung der Startseite und des Hauptfensters</b> — Anwenderwunsch
/// <b>W16b‑E‑5</b> / <b>W16c‑E‑5</b> der Windows-Abnahme vom 05.09.2026
/// („Design und Farbgebung kann verbessert werden, angelehnt an winforms
/// Version vor‑W16").
///
/// <para>Die Angleichung ist FARBE und SCHRIFT, also ausschließlich Stilblatt.
/// Eine bunit-Probe sieht davon nichts — bunit rechnet keine Stilblätter aus,
/// und das Markup war die ganze Zeit richtig. Deshalb liest dieser Fall das
/// Stilblatt selbst, denselben Weg wie die Wachen zu W5‑B‑1
/// (<c>KostenSeiteTests</c>) und zu W16b‑E‑3 (<c>KachelbilderTests</c>).</para>
///
/// <para>Drei Gruppen: (a) die zwölf Token stehen als WERT in <c>:root</c> und
/// nicht mehr nur als Rückfall in der Regel, (b) die Regeln, die das Bild
/// tragen, nennen genau diese Token, (c) jede neu gesetzte
/// Schrift-auf-Fläche-Paarung hält den Hauskontrast von 4,5:1. Die letzte
/// Gruppe rechnet die Werte aus dem Stilblatt nach — sie ist der Grund, warum
/// drei Farben des Vorläufers bewusst NICHT übernommen wurden.</para>
///
/// <para>Die Sprache ist auf de-DE gepinnt (Hausregel seit iU9‑W8): Die Klasse
/// rendert zwei Reiterkomponenten, und der Windows-Läufer läuft mit englischer
/// Oberfläche.</para>
/// </summary>
public class StartseiteAnmutungTests : BunitContext
{
    public StartseiteAnmutungTests()
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

    // =====================================================================
    //  (a) Die Token stehen in :root
    // =====================================================================

    /// <summary>
    /// Die sieben Token der Startseite. Sie tragen die Handschrift von
    /// <c>Form_Start</c> und dürfen deshalb NUR dort gelten — wer sie in den
    /// gemeinsamen Farbsatz zöge, färbte sechzig Dialoge mit um.
    /// </summary>
    [Theory]
    [InlineData("--epos-start-kasten-rahmen", "#b4becd")]      // Pen(180,190,205)
    [InlineData("--epos-start-leiste-flaeche", "#f0f0f0")]     // SystemColors.Control
    [InlineData("--epos-start-reiter-aktiv", "#005aa0")]
    [InlineData("--epos-start-reiter-aktiv-text", "#ffffff")]  // Form_Start :136
    [InlineData("--epos-start-text-leise", "#696969")]         // Color.DimGray
    [InlineData("--epos-start-knopf-flaeche", "#d3d3d3")]      // Color.LightGray
    [InlineData("--epos-start-zusammenfassung", "#f9fafc")]    // Designer :903
    public void Die_sieben_Startseitentoken_stehen_in_root(string name, string wert)
    {
        Assert.Equal(wert, Token(name));
    }

    /// <summary>
    /// Die fünf Werte des Hauptfensters standen bis W16c NUR als Rückfall in
    /// der Regel (<c>var(--epos-marke, #005aa0)</c>). Ein Rückfall ist keine
    /// Farbwahrheit: Er steht so oft da, wie er benutzt wird.
    /// </summary>
    [Theory]
    [InlineData("--epos-marke", "#005aa0")]              // InitMarke :227/236
    [InlineData("--epos-marke-untertitel", "#70777e")]   // InitMarke :245
    [InlineData("--epos-marke-trennlinie", "#dee3e8")]   // InitMarke :224
    [InlineData("--epos-menue-flaeche", "#f0f8ff")]      // menuToolbar = AliceBlue
    [InlineData("--epos-flaeche-hell", "#ffffff")]       // Form_Start.BackColor
    public void Die_Werte_des_Hauptfensters_stehen_in_root(string name, string wert)
    {
        Assert.Equal(wert, Token(name));
    }

    /// <summary>
    /// Die zwölf stehen jetzt in <c>:root</c> — also darf keine Regel sie mehr
    /// mit einem Rückfall aufrufen (<c>var(--epos-marke, #005aa0)</c>). Ein
    /// Rückfall neben einer Festlegung ist eine zweite Wahrheit, die niemand
    /// pflegt: Sie steht so oft da, wie das Token benutzt wird.
    ///
    /// <para>Der Fall prüft NUR diese zwölf. Ältere Regeln des Hauses tragen
    /// weiter Rückfälle auf längst festgelegte Token
    /// (<c>var(--epos-flaeche, #ffffff)</c> und drei weitere); die
    /// aufzuräumen ist Kosmetik in Dialogen der Wellen 1–15 und gehört nicht
    /// in diesen Anwenderwunsch.</para>
    /// </summary>
    [Fact]
    public void Keines_der_zwoelf_Token_traegt_noch_einen_Rueckfall()
    {
        // Kommentare erst heraus: Der Text ueber dem :root-Block ZITIERT die
        // alte Schreibweise, und ein Zitat ist keine Regel.
        string css = Regex.Replace(Stilblatt(), @"/\*.*?\*/", "", RegexOptions.Singleline);

        string[] zwoelf =
        {
            "--epos-start-kasten-rahmen", "--epos-start-leiste-flaeche",
            "--epos-start-reiter-aktiv", "--epos-start-reiter-aktiv-text",
            "--epos-start-text-leise", "--epos-start-knopf-flaeche",
            "--epos-start-zusammenfassung",
            "--epos-marke", "--epos-marke-untertitel", "--epos-marke-trennlinie",
            "--epos-menue-flaeche", "--epos-flaeche-hell"
        };

        string[] rueckfaelle = Regex.Matches(css, @"var\(\s*(--[a-z0-9-]+)\s*,")
                                    .Select(m => m.Groups[1].Value)
                                    .Distinct()
                                    .Where(zwoelf.Contains)
                                    .ToArray();

        Assert.True(rueckfaelle.Length == 0,
                    "Rueckfall trotz Festlegung: " + string.Join(", ", rueckfaelle));
    }

    // =====================================================================
    //  (b) Die Regeln, die das Bild tragen
    // =====================================================================

    [Fact]
    public void Die_aktive_Reiterzunge_der_Startseite_ist_gefuellt()
    {
        // tabControl_Wizard_DrawItem zeichnete den Text der gewaehlten Seite in
        // 0xffffff (Form_Start :136); die Flaeche darunter kam von
        // e.DrawBackground() und war SystemColors.Highlight. Ohne Fuellung
        // stuende weisser Text auf weissem Grund.
        string aktiv = Stilblock(
            ".epos-startseite > .epos-reiter > .epos-reiter-leiste > .epos-reiter-knopf--aktiv {");

        Assert.Contains("background: var(--epos-start-reiter-aktiv)", aktiv);
        Assert.Contains("color: var(--epos-start-reiter-aktiv-text)", aktiv);

        // Der 3-px-Fuss der Hausleiste bleibt: Im erzwungenen Farbmodus faellt
        // jede Flaeche weg, und dann ist der Rahmen das Einzige, was die
        // gewaehlte Zunge noch traegt.
        Assert.Contains("border-bottom-color: var(--epos-start-reiter-aktiv)", aktiv);

        // Ein gesperrter Reiter bleibt sichtbar gesperrt - die Regel oben
        // traegt vier Klassen und schluege die zweistufige Hausregel sonst.
        string gesperrt = Stilblock(
            ".epos-startseite > .epos-reiter > .epos-reiter-leiste > .epos-reiter-knopf:disabled {");
        Assert.Contains("color: var(--epos-text-sehr-leise)", gesperrt);
    }

    [Fact]
    public void Die_Reiterleiste_steht_auf_ihrer_eigenen_Flaeche()
    {
        string leiste = Stilblock(".epos-startseite > .epos-reiter > .epos-reiter-leiste {");
        Assert.Contains("background: var(--epos-start-leiste-flaeche)", leiste);
        Assert.Contains("margin-bottom: 0", leiste);

        // Das Blatt ist der Koerper des Reiterwerks und haengt ohne Fuge daran.
        string blatt = Stilblock(".epos-startseite > .epos-reiter > .epos-reiter-blatt {");
        Assert.Contains("background: var(--epos-flaeche-hell)", blatt);
        Assert.Contains("border-top: 0", blatt);
    }

    [Fact]
    public void Die_zwei_Kopfkaesten_tragen_den_kuehlen_Rahmen_des_Vorlaeufers()
    {
        // panelKlima_Paint/panelVariante_Paint: Pen(180,190,205) auf ein
        // Rundeck von 8 px (Form_Start :2273-2292).
        string kaesten = Stilblock(".epos-startseite-projekt,\n.epos-startseite-klima {");
        Assert.Contains("border: 1px solid var(--epos-start-kasten-rahmen)", kaesten);
        Assert.Contains("border-radius: 8px", kaesten);
    }

    [Fact]
    public void Die_Knoepfe_der_Startseite_sind_hellgrau()
    {
        // Form_Start_Load :85-88 - btn_Zurueck, btn_Weiter und btn_SimKonfig
        // bekamen Color.LightGray zur Laufzeit.
        string knopf = Stilblock(
            ".epos-startseite-fuss .epos-knopf,\n.epos-startreiter-leiste .epos-knopf {");
        Assert.Contains("background: var(--epos-start-knopf-flaeche)", knopf);

        // Ein gesperrter Knopf darf nicht wie ein bedienbarer aussehen.
        string gesperrt = Stilblock(".epos-startseite-fuss .epos-knopf:disabled {");
        Assert.Contains("background: var(--epos-flaeche)", gesperrt);
    }

    [Fact]
    public void Die_Erlaeuterungen_der_Startseite_stehen_in_DimGray()
    {
        // label24/26/28/30 (Reiter) und label2_pBox_* (Kacheln) trugen Segoe UI
        // Semibold 12 pt FETT in Color.DimGray - beides derselbe Ton.
        string reiter = Stilblock(".epos-startreiter-text {");
        Assert.Contains("color: var(--epos-start-text-leise)", reiter);
        Assert.Contains("font-weight: 600", reiter);

        string kachel = Stilblock(".epos-startreiter .epos-kachel-beschreibung {");
        Assert.Contains("color: var(--epos-start-text-leise)", kachel);
        Assert.Contains("font-weight: 600", kachel);
    }

    [Fact]
    public void Das_Kopfband_des_Hauptfensters_traegt_die_Masse_von_InitMarke()
    {
        // Name Segoe UI Semibold 14 pt (= rund 19 px), Untertitel und Version
        // 8,25 pt (= 11 px), Trennlinie 222,227,232 (InitMarke :215-268).
        string name = Stilblock(".epos-hauptfenster-name {");
        Assert.Contains("font-size: 19px", name);
        Assert.Contains("color: var(--epos-marke)", name);

        string untertitel = Stilblock(".epos-hauptfenster-untertitel {");
        Assert.Contains("font-size: 11px", untertitel);
        Assert.Contains("color: var(--epos-marke-untertitel)", untertitel);

        string band = Stilblock(".epos-hauptfenster-marke {");
        Assert.Contains("border-bottom: 1px solid var(--epos-marke-trennlinie)", band);
    }

    [Fact]
    public void Das_Menueband_ist_AliceBlue_und_traegt_die_Kopfschrift_des_Bestands()
    {
        string band = Stilblock(".epos-menueband {");
        Assert.Contains("background: var(--epos-menue-flaeche)", band);

        string koepfe = Stilblock(
            ".epos-menueband > .epos-menueband-punkt > .epos-menueband-knopf,\n"
            + ".epos-menueband > .epos-menueband-knopf {");
        Assert.Contains("font-size: var(--epos-schriftgroesse-kartentitel)", koepfe);
    }

    // =====================================================================
    //  (c) Der Kontrast - warum drei Farben des Vorlaeufers NICHT gelten
    // =====================================================================

    /// <summary>
    /// Jede Paarung, die diese Welle neu gesetzt hat, hält 4,5:1. Die Werte
    /// kommen aus dem Stilblatt, nicht aus dieser Datei — wer ein Token
    /// aufhellt, bekommt hier den roten Fall und nicht erst der Anwender.
    /// </summary>
    [Theory]
    // Weisser Text auf der gefuellten Reiterzunge (statt SystemColors.Highlight
    // #0078d7, das mit 4,50:1 genau auf der Schwelle liegt).
    [InlineData("--epos-start-reiter-aktiv-text", "--epos-start-reiter-aktiv")]
    // Erlaeuterungen in DimGray auf dem weissen Blatt.
    [InlineData("--epos-start-text-leise", "--epos-flaeche-hell")]
    // Dieselben Erlaeuterungen im Zusammenfassungskasten.
    [InlineData("--epos-start-text-leise", "--epos-start-zusammenfassung")]
    // Die Werte der Zusammenfassung - statt Color.FromArgb(128,128,255), das
    // dort nur 3,12:1 traegt.
    [InlineData("--epos-marke", "--epos-start-zusammenfassung")]
    // Die Reiterbeschriftung auf dem grauen Grund der Leiste.
    [InlineData("--epos-text", "--epos-start-leiste-flaeche")]
    // Gattung und Claim im Kopfband - statt Color.FromArgb(150,156,162) fuer
    // die Version, das auf Weiss nur 2,77:1 traegt.
    [InlineData("--epos-marke-untertitel", "--epos-flaeche-hell")]
    [InlineData("--epos-text-leise", "--epos-flaeche-hell")]
    public void Jede_neue_Paarung_haelt_den_Hauskontrast(string vorne, string hinten)
    {
        double v = Kontrast(Token(vorne), Token(hinten));

        Assert.True(v >= 4.5,
                    $"{vorne} auf {hinten}: {v.ToString("F2", CultureInfo.InvariantCulture)}:1");
    }

    /// <summary>
    /// Die Gattungszeile (<c>label20</c>, 26 px fett) steht weiß auf
    /// <c>--epos-marke-flaeche</c> und trägt damit 3,76:1. Das ist für GROSSE
    /// Schrift zulässig (3:1) und der Grund, warum die Marke NUR dort steht und
    /// nicht als Fläche einer Beschriftung in 16 px (<c>label11</c>, Designer
    /// :1078).
    /// </summary>
    [Fact]
    public void Die_Markenflaeche_traegt_nur_grosse_Schrift()
    {
        double v = Kontrast(Token("--epos-marke-text"), Token("--epos-marke-flaeche"));

        Assert.InRange(v, 3.0, 4.5);
    }

    // =====================================================================
    //  Was die Reiter zeichnen
    // =====================================================================

    [Fact]
    public void Der_Waermebedarfsreiter_traegt_die_gestylten_Klassen()
    {
        var cut = Render<WaermebedarfReiter>(p => p
            .Add(x => x.Kacheln, new[]
            {
                new StartKachel
                {
                    Schluessel = Kachelschluessel.Gebaeude,
                    Reiter = Reiterschluessel.Waermebedarf,
                    Titel = "Gebäudedaten eingeben",
                    Beschreibung = "Erfassen Sie Fläche, Dämmung und andere Parameter"
                }
            }));

        // Die Erlaeuterung des Reiters und die der Kachel - beide bekommen
        // DimGray, und beide muessen dafuer unter .epos-startreiter stehen.
        Assert.NotNull(cut.Find(".epos-startreiter > .epos-startreiter-kopf .epos-startreiter-text"));

        IElement beschreibung = cut.Find(".epos-startreiter .epos-kachel-beschreibung");
        Assert.Equal("Erfassen Sie Fläche, Dämmung und andere Parameter", beschreibung.TextContent);
    }

    [Fact]
    public void Der_Simulationsreiter_traegt_seinen_Knopf_in_der_Leiste()
    {
        // .epos-startreiter-leiste .epos-knopf ist die Stelle, an der
        // btn_SimKonfig sein LightGray bekommt.
        var cut = Render<SimulationReiter>(p => p
            .Add(x => x.Kacheln, Array.Empty<StartKachel>()));

        Assert.NotNull(cut.Find(".epos-startreiter-leiste .epos-knopf"));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Der Wert eines Tokens aus dem <c>:root</c>-Block.</summary>
    private static string Token(string name)
    {
        string css = Stilblatt();
        Match m = Regex.Match(css, Regex.Escape(name) + @"\s*:\s*([^;]+);");

        Assert.True(m.Success, $"Token {name} steht nicht im Stilblatt");
        return m.Groups[1].Value.Trim();
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        string css = Stilblatt().Replace("\r\n", "\n");
        string gesucht = selektor.Replace("\r\n", "\n");

        int a = css.IndexOf(gesucht, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");

        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + gesucht.Length, e - a - gesucht.Length);
    }

    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null &&
               !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    // ---------------------------------------------------------------------
    //  Kontrast nach WCAG 2.1 (1.4.3) - dieselbe Rechnung, die die
    //  Hausschwelle von 4,5:1 meint.
    // ---------------------------------------------------------------------

    private static double Kontrast(string vorne, string hinten)
    {
        double a = Helligkeit(vorne);
        double b = Helligkeit(hinten);
        double hell = Math.Max(a, b);
        double dunkel = Math.Min(a, b);
        return (hell + 0.05) / (dunkel + 0.05);
    }

    /// <summary>Relative Helligkeit einer Farbe der Schreibweise <c>#rrggbb</c>.</summary>
    private static double Helligkeit(string farbe)
    {
        Match m = Regex.Match(farbe.Trim(), @"^#([0-9a-fA-F]{6})$");
        Assert.True(m.Success, $"Keine Farbe der Form #rrggbb: {farbe}");

        string hex = m.Groups[1].Value;
        double R = Kanal(Convert.ToInt32(hex.Substring(0, 2), 16));
        double G = Kanal(Convert.ToInt32(hex.Substring(2, 2), 16));
        double B = Kanal(Convert.ToInt32(hex.Substring(4, 2), 16));

        return 0.2126 * R + 0.7152 * G + 0.0722 * B;
    }

    private static double Kanal(int wert)
    {
        double c = wert / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
