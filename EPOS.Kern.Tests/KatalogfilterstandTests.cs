using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Filterregister und die Spalte „im Projekt verwendet"</b> —
    /// Anwenderentscheid <b>W14a-E-10</b> vom 07.09.2026, Stufen <b>S2.5</b>
    /// (<see cref="Katalogfilterregister"/>, Frage Q2) und <b>S2.3</b>
    /// (<see cref="Katalogverwendung"/>, Frage Q12).
    ///
    /// <para><b>Warum das hier steht und nicht in bunit.</b> Beides ist Rechnung
    /// ohne Oberflaeche: „welcher Katalog steht wie gefiltert" und „welche
    /// Katalogzeile steht im Projekt". Haette der Filterstand als <c>static</c> in
    /// einer Razor-Komponente gelegen, liesse er sich weder hier zuruecksetzen noch
    /// zwischen zwei Dialogen nachweisen.</para>
    ///
    /// <para><b>Diese Klasse laeuft NICHT nebenlaeufig zu anderen</b> — sie raeumt
    /// ein prozessweites Register auf. xunit faehrt Testklassen desselben
    /// Sammelnamens nacheinander, deshalb steht sie in derselben Sammlung wie die
    /// Datenbankproben.</para>
    ///
    /// <para>Kultur gepinnt (Hausregel seit iU9-W8): Der Ausdruck
    /// <c>&gt;=0,95</c> und die Anzeigetexte „Ja"/„Nein" haengen daran.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogfilterstandTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.DefaultThreadCurrentCulture;

        public KatalogfilterstandTests()
        {
            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;

            Katalogfilterregister.Leeren();
        }

        public void Dispose()
        {
            Katalogfilterregister.Leeren();
            CultureInfo.DefaultThreadCurrentCulture = _vorher;
            CultureInfo.DefaultThreadCurrentUICulture = _vorher;
        }

        // =================================================================
        //  1 - Das Register (S2.5, Frage Q2)
        // =================================================================

        /// <summary>
        /// <b>Der Kern von Q2:</b> Verwaltung und Projektdialog teilen sich EINE
        /// Instanz. Der Nachweis ist die Objektgleichheit — nicht ein gleicher
        /// Inhalt, denn zwei Staende mit demselben Inhalt liefen beim naechsten
        /// Tastendruck auseinander.
        /// </summary>
        [Fact]
        public void Verwaltung_und_Projektdialog_teilen_denselben_Stand()
        {
            Katalogfilterstand ausDerVerwaltung = Katalogfilterregister.Stand(Anlagenart.Heizkessel);
            Katalogfilterstand ausDemProjekt = Katalogfilterregister.Stand(Anlagenart.Heizkessel);

            Assert.Same(ausDerVerwaltung, ausDemProjekt);
        }

        /// <summary>
        /// <b>Der Filter ueberlebt Schliessen und Oeffnen.</b> Ein Dialog holt seinen
        /// Stand beim Aufbau, setzt einen Ausdruck und geht; der naechste Aufbau —
        /// derselbe Dialog oder der andere Wirt — findet ihn wieder. Genau das
        /// beschreibt Q2: „Wer im Projektdialog nach Gas filtert, die Verwaltung
        /// oeffnet und zurueckkommt, will seinen Filter wiederfinden."
        /// </summary>
        [Fact]
        public void Der_Filter_ueberlebt_das_Schliessen_des_Dialogs()
        {
            // Aufbau 1 - der Projektdialog filtert.
            Katalogfilterregister.Stand(Anlagenart.Heizkessel)
                                 .Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");
            Katalogfilterregister.Stand(Anlagenart.Heizkessel)
                                 .Sortieren(Katalogfilterprofil.SpPtherm);

            // Aufbau 2 - die Verwaltung geht auf.
            Katalogfilterstand zweiterAufbau = Katalogfilterregister.Stand(Anlagenart.Heizkessel);

            Assert.Equal("Gas", zweiterAufbau.Ausdruck(Katalogfilterprofil.SpBrennstoff));
            Assert.True(zweiterAufbau.Gefiltert(Katalogfilterprofil.SpBrennstoff));

            // S2.5: der Stand traegt seit dieser Stufe auch SPALTE UND RICHTUNG.
            Assert.Equal(Katalogfilterprofil.SpPtherm, zweiterAufbau.Sortierspalte);
            Assert.True(zweiterAufbau.Aufsteigend);
        }

        /// <summary>
        /// <b>Je Katalog EINER</b> — der Kesselfilter darf im Pufferspeicher nicht
        /// auftauchen. Acht Auspraegungen, acht Staende.
        /// </summary>
        [Fact]
        public void Jeder_Katalog_fuehrt_seinen_eigenen_Stand()
        {
            Katalogfilterregister.Stand(Anlagenart.Heizkessel)
                                 .Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");

            Assert.NotSame(Katalogfilterregister.Stand(Anlagenart.Heizkessel),
                           Katalogfilterregister.Stand(Anlagenart.Pufferspeicher));
            Assert.Equal("", Katalogfilterregister.Stand(Anlagenart.Pufferspeicher)
                                                  .Ausdruck(Katalogfilterprofil.SpBrennstoff));

            var alle = Katalogfilterprofil.AlleArten.ToList();
            foreach (Anlagenart art in alle) Katalogfilterregister.Stand(art);
            Assert.Equal(8, alle.Count);
            Assert.All(alle, art => Assert.True(Katalogfilterregister.Bekannt(art)));
        }

        /// <summary>
        /// <c>Bekannt</c> legt NICHTS an — sonst waere die Pruefhilfe selbst der
        /// Grund, warum es einen Stand gibt.
        /// </summary>
        [Fact]
        public void Bekannt_legt_keinen_Stand_an()
        {
            Assert.False(Katalogfilterregister.Bekannt(Anlagenart.Bhkw));
            Assert.False(Katalogfilterregister.Bekannt(Anlagenart.Bhkw));

            Katalogfilterregister.Stand(Anlagenart.Bhkw);
            Assert.True(Katalogfilterregister.Bekannt(Anlagenart.Bhkw));
        }

        /// <summary>
        /// <b>Sitzung, nicht dauerhaft.</b> <c>Leeren</c> ist der einzige Weg
        /// hinaus; es wird nichts geschrieben und nichts gelesen. Das ist die
        /// Entscheidung zu Q2 („Sitzung") — dauerhaft wird der Filter erst, wenn
        /// der Anwender es nach der Abnahme vermisst.
        /// </summary>
        [Fact]
        public void Leeren_wirft_alles_weg()
        {
            Katalogfilterregister.Stand(Anlagenart.Heizkessel)
                                 .Setzen(Katalogfilterprofil.SpBrennstoff, "Gas");
            Katalogfilterstand vorher = Katalogfilterregister.Stand(Anlagenart.Heizkessel);

            Katalogfilterregister.Leeren();

            Assert.False(Katalogfilterregister.Bekannt(Anlagenart.Heizkessel));
            Katalogfilterstand nachher = Katalogfilterregister.Stand(Anlagenart.Heizkessel);
            Assert.NotSame(vorher, nachher);
            Assert.Equal("", nachher.Ausdruck(Katalogfilterprofil.SpBrennstoff));
        }

        /// <summary>
        /// Zwei Faeden duerfen nicht zwei Staende bekommen — der Zugriff steht unter
        /// einem Schloss. Ohne es entstuende beim ersten gleichzeitigen Oeffnen zweier
        /// Wirte ein zweiter Stand, und einer der beiden filterte ins Leere.
        /// </summary>
        [Fact]
        public void Zwei_Faeden_bekommen_denselben_Stand()
        {
            var gefunden = new Katalogfilterstand[32];

            Parallel.For(0, gefunden.Length,
                         i => gefunden[i] = Katalogfilterregister.Stand(Anlagenart.Photovoltaik));

            Assert.All(gefunden, s => Assert.Same(gefunden[0], s));
        }

        // =================================================================
        //  2 - Die Spalte „im Projekt verwendet" (S2.3, Frage Q12)
        // =================================================================

        private static List<Katalogfilterzeile> Katalog()
            => new List<Katalogfilterzeile>
            {
                new Katalogfilterzeile(1, "Vitocal 200"),
                new Katalogfilterzeile(2, "Vitocal 300"),
                new Katalogfilterzeile(3, "Logatherm WLW"),
            };

        private static string JA => WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA;
        private static string NEIN => WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_NEIN;

        /// <summary>
        /// <b>Der Kern von Q12:</b> „ja"/„nein" je Zeile, EINMAL fuer die ganze Liste
        /// gestempelt — aus der Projektliste, die der Dialog ohnehin fuehrt.
        /// </summary>
        [Fact]
        public void Die_Zeilen_des_Projekts_tragen_Ja_die_anderen_Nein()
        {
            var katalog = Katalog();

            int treffer = Katalogverwendung.Stempeln(katalog, new[] { "Vitocal 300" });

            Assert.Equal(1, treffer);
            Assert.Equal(NEIN, katalog[0].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(JA, katalog[1].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(NEIN, katalog[2].Text(Katalogfilterprofil.SpVerwendet));
        }

        /// <summary>
        /// <b>Gross/klein und Randleerzeichen entscheiden nicht</b> — dieselbe Wahl
        /// wie im uebrigen Katalogfilter (<c>CurrentCultureIgnoreCase</c>). Die
        /// Projektkopie traegt denselben Bezeichner wie der Katalogsatz, kann aber
        /// von Hand umbenannt worden sein.
        /// </summary>
        [Fact]
        public void Gross_klein_und_Leerzeichen_entscheiden_nicht()
        {
            var katalog = Katalog();

            Assert.Equal(2, Katalogverwendung.Stempeln(
                katalog, new[] { "  vitocal 200  ", "LOGATHERM WLW" }));

            Assert.Equal(JA, katalog[0].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(NEIN, katalog[1].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(JA, katalog[2].Text(Katalogfilterprofil.SpVerwendet));
        }

        /// <summary>
        /// <b>Ohne Projektliste traegt jede Zeile „Nein"</b> — nicht „leer". Eine leere
        /// Zelle liesse offen, ob nichts verwendet wird oder ob niemand nachgesehen
        /// hat; die Spalte laesst sich dann auch sortieren.
        /// </summary>
        [Fact]
        public void Ohne_Projektliste_traegt_jede_Zeile_Nein()
        {
            var katalog = Katalog();

            Assert.Equal(0, Katalogverwendung.Stempeln(katalog, null));
            Assert.All(katalog, z => Assert.Equal(NEIN, z.Text(Katalogfilterprofil.SpVerwendet)));

            Assert.Equal(0, Katalogverwendung.Stempeln(katalog, Array.Empty<string>()));
            Assert.All(katalog, z => Assert.Equal(NEIN, z.Text(Katalogfilterprofil.SpVerwendet)));
        }

        /// <summary>
        /// <b>Der Stempel ist FRISCH, nicht kumulativ.</b> Wer eine Zeile aus dem
        /// Projekt entfernt, sieht das beim naechsten Zeichnen — die alte Marke
        /// bleibt nicht stehen. Genau deshalb steht der Aufruf im
        /// <c>OnParametersSet</c> der Dialoge und nicht im <c>OnInitialized</c>.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Stempel_loescht_den_ersten()
        {
            var katalog = Katalog();

            Katalogverwendung.Stempeln(katalog, new[] { "Vitocal 200", "Vitocal 300" });
            Assert.Equal(JA, katalog[0].Text(Katalogfilterprofil.SpVerwendet));

            // Der Anwender nimmt "Vitocal 200" aus dem Projekt heraus.
            Assert.Equal(1, Katalogverwendung.Stempeln(katalog, new[] { "Vitocal 300" }));
            Assert.Equal(NEIN, katalog[0].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(JA, katalog[1].Text(Katalogfilterprofil.SpVerwendet));
        }

        /// <summary>
        /// Leere Namen und <c>null</c>-Zeilen kippen den Stempel nicht — eine
        /// Projektliste im Aufbau darf beides enthalten.
        /// </summary>
        [Fact]
        public void Leere_Namen_und_leere_Zeilen_stoeren_nicht()
        {
            var katalog = Katalog();
            katalog.Insert(1, null);

            Assert.Equal(1, Katalogverwendung.Stempeln(
                katalog, new string[] { null, "", "   ", "Vitocal 300" }));

            Assert.Equal(JA, katalog[2].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(0, Katalogverwendung.Stempeln(null, new[] { "Vitocal 300" }));
        }

        /// <summary>
        /// <b>Die Spalte ist SORTIERBAR und traegt keinen Trichter</b> (Q12:
        /// „sortierbar, Sortierpfeil"). Ein Trichter mit einem Freitextfeld auf
        /// zwei Werten waere ein Bedienelement fuer nichts.
        /// </summary>
        [Fact]
        public void Die_Spalte_ist_sortierbar_und_ohne_Trichter()
        {
            Katalogfilterprofil mit = Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel);
            Katalogfilterprofil ohne = Katalogfilterprofil.Finde(Anlagenart.Heizkessel);

            Assert.Equal(ohne.Spalten.Count + 1, mit.Spalten.Count);

            Katalogspalte spalte = mit.Spalte(Katalogfilterprofil.SpVerwendet);
            Assert.NotNull(spalte);
            Assert.True(spalte.Sortierbar);
            Assert.False(spalte.Filterbar);
            Assert.Equal(Katalogspaltenart.JaNein, spalte.Art);

            // Sie steht am ENDE - der Katalog bleibt vorn, die Projektauskunft hinten.
            Assert.Equal(Katalogfilterprofil.SpVerwendet,
                         mit.Spalten[mit.Spalten.Count - 1].Schluessel);

            // Und ohne sie gibt es sie nicht.
            Assert.Null(ohne.Spalte(Katalogfilterprofil.SpVerwendet));
        }

        /// <summary>
        /// <b>Sortieren nach der Spalte holt die verwendeten Geraete zusammen</b> —
        /// das ist der Zweck, den Q12 ihr gibt. Ein Kennzeichen sortiert wie eine
        /// ZAHL (0/1) und nicht nach dem Anzeigetext, damit „Ja" in beiden Sprachen
        /// an derselben Stelle steht: aufsteigend „Nein" zuerst, ein zweiter Klick
        /// dreht um und stellt die verwendeten nach oben.
        /// </summary>
        [Fact]
        public void Sortieren_nach_der_Spalte_holt_die_verwendeten_zusammen()
        {
            Katalogfilterprofil profil = Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel);
            var katalog = Katalog();
            Katalogverwendung.Stempeln(katalog, new[] { "Logatherm WLW" });

            var stand = new Katalogfilterstand();

            // Erster Klick: aufsteigend - die nicht verwendeten zuerst.
            stand.Sortieren(Katalogfilterprofil.SpVerwendet);
            var auf = Katalogfilter.Anwenden(profil, katalog, stand);
            Assert.Equal(NEIN, auf[0].Text(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(JA, auf[auf.Count - 1].Text(Katalogfilterprofil.SpVerwendet));

            // Zweiter Klick: absteigend - die verwendeten nach oben.
            stand.Sortieren(Katalogfilterprofil.SpVerwendet);
            Assert.False(stand.Aufsteigend);
            var ab = Katalogfilter.Anwenden(profil, katalog, stand);
            Assert.Equal("Logatherm WLW", ab[0].Bezeichner);
            Assert.Equal(JA, ab[0].Text(Katalogfilterprofil.SpVerwendet));
            Assert.All(ab.Skip(1),
                       z => Assert.Equal(NEIN, z.Text(Katalogfilterprofil.SpVerwendet)));

            // Dritter Klick: aus (Sortierzyklus auf -> ab -> aus, 5.6.2).
            stand.Sortieren(Katalogfilterprofil.SpVerwendet);
            Assert.Equal("", stand.Sortierspalte);
        }

        // =================================================================
        //  3 - Die Kultur des Zahlenfilters (Befund W13-B-6, dritte Frage)
        // =================================================================

        /// <summary>
        /// <b>Was dasteht, ist auch das, was man tippen kann.</b> Zum Befund W13-B-6
        /// gehoerte die Frage, ob der Zahlenfilter „9,6" ueberhaupt versteht — die
        /// Oberflaeche laeuft in einer <c>BlazorWebView</c>, und
        /// <c>StandardSprache.KulturUebernehmen</c> setzt ausdruecklich NUR die
        /// Anzeigesprache (<c>CurrentUICulture</c>); die RECHENkultur bleibt die des
        /// Betriebssystems (Drei-Schichten-Regel, Konzept 13.6).
        ///
        /// <para><b>Die Antwort ist: immer.</b> Beide Seiten haengen an DERSELBEN
        /// Groesse. <see cref="Katalogwert.AusZahl"/> formatiert mit
        /// <c>CultureInfo.CurrentCulture</c>, und <c>Katalogfilter.PasstSpalte</c>
        /// liest ueber <see cref="Zahlenausdruck.Lesen(string, CultureInfo)"/> ohne
        /// ausdrueckliche Kultur, also ebenfalls mit <c>CurrentCulture</c>. Eine
        /// englische Oberflaeche auf einem deutschen Windows aendert daran nichts —
        /// dann steht „9,6" da, und „9,6" trifft.</para>
        ///
        /// <para><b>Und die fremde Schreibweise ist kein stiller Fehltreffer:</b> Sie
        /// ist UNVERSTANDEN, und ein unverstandener Ausdruck ist kein Filter (dieselbe
        /// Regel wie beim halb getippten Bereich). Die Liste bleibt also vollstaendig
        /// stehen, statt leer zu werden.</para>
        ///
        /// <para>Der Faden wird hier bewusst nur ueber
        /// <c>Thread.CurrentThread.CurrentCulture</c> umgestellt und nicht ueber
        /// <c>DefaultThreadCurrentCulture</c>: Letzteres gilt prozessweit, und xunit
        /// faehrt Testsammlungen nebenlaeufig.</para>
        /// </summary>
        [Theory]
        [InlineData("de-DE", "9,6", "9.6")]
        [InlineData("en-US", "9.6", "9,6")]
        public void Anzeige_und_Zahlenfilter_teilen_sich_EINE_Kultur(
            string kuerzel, string eigene, string fremde)
        {
            CultureInfo vorher = Thread.CurrentThread.CurrentCulture;
            try
            {
                var kultur = new CultureInfo(kuerzel);
                Thread.CurrentThread.CurrentCulture = kultur;

                // Die Zeile entsteht UNTER dieser Kultur - so wie im Programm auch.
                var zeile = new Katalogfilterzeile(1, "Speicher A")
                    .MitZahl(Katalogfilterprofil.SpPtherm, 9.6);

                // 1. So SCHREIBT die Spalte.
                Assert.Equal(eigene, zeile.Text(Katalogfilterprofil.SpPtherm));

                Katalogfilterprofil profil = Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);
                Katalogspalte spalte = profil.Spalte(Katalogfilterprofil.SpPtherm);
                Katalogwert wert = zeile.Wert(Katalogfilterprofil.SpPtherm);

                // 2. Und genau so LIEST der Filter - in allen Schreibweisen des Popovers.
                Assert.True(Katalogfilter.PasstSpalte(spalte, wert, "=" + eigene));
                Assert.True(Katalogfilter.PasstSpalte(spalte, wert, eigene));
                Assert.True(Katalogfilter.PasstSpalte(spalte, wert, ">" + eigene.Replace("6", "5")));
                Assert.False(Katalogfilter.PasstSpalte(spalte, wert, "<" + eigene.Replace("6", "5")));

                // 3. Die fremde Schreibweise ist unverstanden - also KEIN Filter.
                Assert.Null(Zahlenausdruck.Lesen("=" + fremde, kultur));
                Assert.True(Katalogfilter.PasstSpalte(spalte, wert, "=" + fremde));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = vorher;
            }
        }
    }
}
