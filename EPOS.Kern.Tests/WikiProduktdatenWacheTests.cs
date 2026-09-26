using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die INHALTE der Wiki-Quellen: <b>keine Hersteller- und keine
    /// Produktdaten</b> (Konzept Hilfesystem, Abschnitt 13.2; Wurzel-<c>CLAUDE.md</c>,
    /// Abschnitt „Dokumentation").
    ///
    /// <para><b>Die Regel.</b> Keine Wiki-Seite nennt einen Hersteller, ein Produkt oder
    /// eine Typbezeichnung — weder im Fließtext noch in Tabellen, Beispielen,
    /// Bildunterschriften, Beispielausgaben des Programms oder HTML-Kommentaren. Die
    /// Kataloge von EPOS-Plan tragen Herstellerdaten, die die Hersteller unter eigenen
    /// Bedingungen bereitstellen; das Wiki ist öffentlich und beschreibt das PROGRAMM,
    /// nicht die Produkte. Ein Produktname im Beispiel wirkt wie eine Empfehlung, altert
    /// mit dem Katalog und kann Marken- und Nutzungsrechte berühren. Beispiele tragen
    /// deshalb neutrale Namen mit runden Werten („Speicher A 1", „Modul B, 400 W").</para>
    ///
    /// <para><b>Was der Wächter liest.</b> Die beiden Ablagen der Wiki-Quellen im
    /// Repository: <c>EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki</c> (die Rechenwegseiten,
    /// in den Kern eingebettet) und <c>Projekte/Wiki/*.wiki</c> (die Bedienungsseiten).
    /// Seiten OHNE Repo-Quelle erreicht er nicht; die prüft die Orchestrierung vor jedem
    /// Upload mit derselben Liste.</para>
    ///
    /// <para><b>Drei Quellen der verbotenen Namen.</b>
    /// (1) Die <b>Katalognamen der Testdatenbank</b> — Hersteller (<c>Firma</c>,
    /// <c>Hersteller</c>) und Gerätenamen (<c>Bezeichner</c>) der acht Gerätekataloge und
    /// ihrer Projekttabellen. Sie sind die einzige Quelle, die MITWÄCHST: Kommt ein
    /// Hersteller in den Katalog, hält der Wächter ihn ab dem nächsten Lauf von den
    /// Wiki-Seiten fern, ohne dass jemand eine Liste pflegt.
    /// (2) Eine <b>feste Liste bekannter Hersteller und Produktlinien</b> für alles, was
    /// die Testdatenbank (noch) nicht führt — der Fund, der diesen Wächter ausgelöst hat,
    /// stand genau dort: ein Speicherhersteller, den kein Katalog dieser Datenbank kennt.
    /// (3) Ein <b>Typcode-Muster</b> für die Form „ein bis drei Großbuchstaben, unmittelbar
    /// gefolgt von mindestens drei Ziffern" — so sehen Typbezeichnungen aus, und so sieht
    /// sonst nichts aus, was auf einer Hilfeseite zu suchen hätte.</para>
    ///
    /// <para><b>Was ausdrücklich ERLAUBT bleibt</b> und deshalb nicht trifft: Datenquellen,
    /// Normen und Formate (VDI 3805, VDI 4640, VDI 6002, DIN, EN, ISO, die CEC-Listen,
    /// PVsyst, GEMIS, DWD, TRY, BDEW), Gattungsbegriffe und Technikklassen
    /// (Luft-Wasser-Wärmepumpe, Flachkollektor), Richtwerte ohne Produktbezug sowie
    /// Software- und Plattformnamen (Windows, iOS, WebView2, MediaWiki). Die
    /// Norm-Schreibweisen mit Leerzeichen („VDI 3805", „TRY 2015", „ISO 8601") trifft das
    /// Typcode-Muster ohnehin nicht; die GEKLEBTE Form (<c>VDI4640Pruefung</c> — ein
    /// Dateiname im Kopfblock der Rechenwegseiten) wird über die Normkürzel ausgenommen.
    /// </para>
    ///
    /// <para><b>Wortgrenzen mit Umlaut.</b> Die Grenze ist nicht <c>\b</c>, sondern
    /// <c>(?&lt;![\p{L}\p{N}_])…(?![\p{L}\p{N}_])</c> — sonst träfe ein Begriff mitten in
    /// einem deutschen Wort. Die feste Liste führt deshalb „sonnenBatterie" und nicht
    /// „sonnen"; „Sonnenschein" bleibt unbehelligt.</para>
    ///
    /// <para><b>Fehlt die Testdatenbank, schweigt Quelle (1)</b> — die feste Liste und das
    /// Typcode-Muster laufen trotzdem. Ein Lauf in einer Umgebung ohne die 68-MB-Datei soll
    /// nicht rot werden, aber auch nicht still alles durchlassen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class WikiProduktdatenWacheTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new();

        public WikiProduktdatenWacheTests(TestDatenbank db) { _db = db; }

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Die Quellen
        // =====================================================================

        /// <summary>Die beiden Ablagen der Wiki-Quellen, repo-relativ.</summary>
        private static readonly string[] Quellordner =
        {
            "EPOS.Kern/Allgemein/Hilfe/Berechnung",
            "Projekte/Wiki",
        };

        /// <summary>
        /// Die acht Gerätekataloge — je Eintrag der Rumpfname. Gelesen werden die
        /// Katalogtabelle (<c>…_STAMM</c>) UND die gleichnamige Projekttabelle: Ein Name,
        /// der nur in einem Projekt steht, ist auf einer Wikiseite genauso wenig zu suchen.
        /// </summary>
        private static readonly string[] Katalogtabellen =
        {
            "Tab_BHKW", "Tab_Heizkessel", "Tab_PV", "Tab_Pufferspeicher",
            "Tab_Solarkollektoren", "Tab_Stromspeicher", "Tab_WP", "Tab_Wechselrichter",
        };

        /// <summary>Die Spalten, die einen HERSTELLER tragen — je Tabelle höchstens eine.</summary>
        private static readonly string[] Herstellerspalten = { "Firma", "Hersteller" };

        /// <summary>
        /// Die Spalten, die einen GERÄTENAMEN tragen könnten, in dieser Reihenfolge; je
        /// Tabelle zählt die ERSTE vorhandene. Der Bestand führt durchweg
        /// <c>Bezeichner</c>; die übrigen stehen für einen künftigen Katalog. Die
        /// Reihenfolge ist wesentlich: <c>Tab_Stromspeicher.Typ</c> trägt die
        /// TECHNIKKLASSE („Li-Ion"), nicht den Gerätenamen — <c>Bezeichner</c> muss
        /// deshalb davor stehen.
        /// </summary>
        private static readonly string[] Namensspalten =
        {
            "Bezeichner", "Name", "Bezeichnung", "Modell", "Modul", "Typ",
        };

        /// <summary>
        /// Platzhalter der Testdatenbank — Zeilen, die niemand als Produkt gemeint hat
        /// („Muster 2500TL", „test3", „EPOS-Plan Referenz"). Verglichen wird das erste
        /// Wort ohne angehängte Ziffern, ohne Rücksicht auf Groß- und Kleinschreibung.
        /// </summary>
        private static readonly string[] Platzhalter = { "Muster", "test", "xxx", "meins" };

        /// <summary>Der Platzhalter, der aus MEHREREN Wörtern besteht und darum ganz verglichen wird.</summary>
        private const string PlatzhalterGanz = "EPOS-Plan Referenz";

        /// <summary>
        /// Firmenzusätze, die vor dem Vergleich abfallen — damit auch der Kurzname trifft
        /// („STIEBEL ELTRON GmbH &amp; Co. KG" → „STIEBEL ELTRON"). Abgeschnitten wird
        /// wiederholt und von hinten, deshalb stehen die längsten Formen zuerst.
        /// </summary>
        private static readonly string[] Firmenzusaetze =
        {
            " GmbH & Co. KG", " GmbH & Co", " & Co. KG", " & Co", " GmbH", " mbH",
            " A/S", " AG", " SE", " KG", " e.K.",
        };

        /// <summary>
        /// Die feste Liste bekannter Hersteller und Produktlinien — <b>case-sensitive</b>.
        /// Sie fängt, was die Testdatenbank nicht führt, und ist bewusst in der
        /// Schreibweise der Marke gehalten: „sonnenBatterie" statt „sonnen", „Wolf GmbH"
        /// statt „Wolf" — sonst träfe der Wächter das deutsche Wort und nicht die Marke.
        /// </summary>
        private static readonly string[] FesteHersteller =
        {
            "Viessmann", "Vaillant", "Bosch", "Buderus", "Junkers", "Stiebel Eltron",
            "STIEBEL ELTRON", "Wolf GmbH", "Weishaupt", "SenerTec", "2G Energy", "EC Power",
            "Jinkosolar", "Jinko", "LG Electronics", "Growatt", "SMA Solar", "Fronius",
            "Huawei", "BYD", "Tesla", "sonnenBatterie", "Senec", "E3/DC", "Kostal",
            "SolarEdge", "Daikin", "Mitsubishi", "Panasonic", "NIBE", "Nibe", "Dimplex",
            "Trina", "JA Solar", "LONGi", "Longi", "Q CELLS", "Meyer Burger", "Pylontech",
            "VARTA", "Varta", "Dachs", "ecoPOWER", "Logano", "Logamax", "Vitocal",
            "Vitodens", "Vitobloc", "Vitovalor", "ecoTEC", "aroTHERM", "geoTHERM",
            "Ablytek", "Philadelphia Solar", "Shenzhen",
        };

        /// <summary>
        /// Das Typcode-Muster: ein bis drei Großbuchstaben, UNMITTELBAR gefolgt von
        /// mindestens drei Ziffern und beliebigem Typanhang („CS6800iAW", „JKM400M",
        /// „PS-M144" greift über den Anhang). „VDI 3805" und „TRY 2015" haben ein
        /// Leerzeichen und treffen darum gar nicht; „Windows-1252" hat einen Bindestrich
        /// vor der Zahl.
        /// </summary>
        private static readonly Regex Typcode =
            new Regex(@"(?<![\p{L}\p{N}_])[A-Z]{1,3}\d{3,}[A-Za-z0-9+\-]*(?![\p{L}\p{N}_])",
                      RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Die Kürzel, die vor einer Zahl eine NORM und kein Produkt anzeigen — auch in der
        /// geklebten Form, wie sie in Dateinamen vorkommt (<c>VDI4640Pruefung.cs</c> steht
        /// im Kopfblock der Rechenwegseite „Wärmequelle Erdreich").
        /// </summary>
        private static readonly string[] Normkuerzel = { "DIN", "EN", "ISO", "IEC", "VDI", "TRY" };

        /// <summary>
        /// Zwei Normbezeichnungen der Brauchwasserauslegung, die das Typcode-Muster als Ganzes trifft:
        /// <c>A100</c> (Beiblatt zu DIN EN 12831-3) und <c>W551</c> (DVGW-Arbeitsblatt W 551). Sie
        /// stehen in Oberflächentexten des Zapfprofilgenerators und — geklebt — in den Schlüsseln
        /// seines Parameterkatalogs (<c>A100.Ladungsfaktor</c>, <c>W551.Mindesttemperatur</c>); ein
        /// Produkt sind sie nicht.
        /// </summary>
        private static readonly string[] Normbezeichnungen = { "A100", "W551" };

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// <b>Der Wächter selbst:</b> Keine Wiki-Quelle des Bestands nennt einen
        /// Hersteller, ein Produkt oder eine Typbezeichnung.
        /// </summary>
        [Fact]
        public void Keine_Wikiquelle_nennt_Hersteller_oder_Produktdaten()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();
            var funde = new List<string>();

            foreach ((string datei, string text) in Wikiquellen())
                funde.AddRange(Fundstellen(datei, text, begriffe));

            Assert.True(funde.Count == 0,
                "Diese Wiki-Quellen nennen einen Hersteller, ein Produkt oder eine " +
                "Typbezeichnung (Konzept Hilfesystem 13.2). Das Wiki beschreibt das " +
                "Programm, nicht die Produkte - Beispiele tragen neutrale Namen mit runden " +
                "Werten (\"Speicher A 1\", \"Modul B, 400 W\"):\n" + string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Ein synthetischer Text, der alle DREI Arten trägt,
        /// wird in allen drei Arten gefunden. Ohne diesen Fall bliebe offen, ob der
        /// Wächter überhaupt etwas findet oder nur zufällig auf einen sauberen Bestand
        /// passt.
        /// </summary>
        [Fact]
        public void Der_Leser_findet_alle_drei_Arten()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();

            string[] zeilen =
            {
                "Die Wärmepumpe Vitocal 200-S steht im Katalog.",       // feste Liste
                "ihre Bezeichner lauten dann ''Growatt WIT 1''.",       // feste Liste
                "Ein Modul JKM400M liefert 400 W.",                     // Typcode
                "Der Speicher allSTOR exclusiv VPS 300/3-7 fasst 300 l.", // Katalog
                "Die Wand trägt Platten von Sto auf Foamglas.",          // Katalog: Baustoffhersteller
            };

            List<string> funde = Fundstellen("probe.wiki", string.Join("\n", zeilen), begriffe);

            Assert.Contains(funde, f => f.Contains("[Liste] Vitocal", StringComparison.Ordinal));
            Assert.Contains(funde, f => f.Contains("[Liste] Growatt", StringComparison.Ordinal));
            Assert.Contains(funde, f => f.Contains("[Typcode] JKM400M", StringComparison.Ordinal));

            // Quelle (1) kann nur nachgewiesen werden, wenn die Testdatenbank vorliegt.
            if (_db.Vorhanden)
            {
                Assert.Contains(funde, f => f.Contains("[Katalog] allSTOR", StringComparison.Ordinal));
                Assert.Contains(funde, f => f.Contains("[Katalog] Sto ", StringComparison.Ordinal));
                Assert.Contains(funde, f => f.Contains("[Katalog] Foamglas ", StringComparison.Ordinal));
            }

            // Datei und Zeile stehen in jeder Meldung.
            Assert.Contains(funde, f => f.StartsWith("probe.wiki:3", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Gegenprobe zur anderen Seite:</b> Was erlaubt ist, trifft NICHT — Normen und
        /// Datenquellen, Gattungsbegriffe, Plattformnamen, neutrale Beispielnamen und die
        /// geklebte Normform aus dem Kopfblock. Ein Wächter, der hier anschlüge, wäre
        /// nicht zu gebrauchen: Er würde genau die Sätze verbieten, die eine Hilfeseite
        /// braucht.
        /// </summary>
        [Fact]
        public void Erlaubte_Begriffe_treffen_nicht()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();

            string[] zeilen =
            {
                "Der Import liest VDI 3805 und die Erdreichprüfung folgt VDI 4640.",
                "Die CEC Energy Storage System List und die PVsyst-Formate .PAN und .OND.",
                "Ein Flachkollektor bei Sonnenschein; die Luft-Wasser-Wärmepumpe läuft.",
                "Die Bezeichner lauten ''Speicher A 1'', ''Speicher A 2'', …",
                "Ältere Dateien sind Windows-1252 ohne BOM; der Wetterdatensatz ist TRY 2015.",
                "Zeitstempel nach ISO 8601, Emissionen aus GEMIS und vom UBA.",
                "EPOS.Kern/Allgemein/Simulation/VDI4640Pruefung.cs trägt die Prüfung.",
                "Ein Modul B mit 400 W und ein Speicher 1 mit 100 kWh.",
            };

            List<string> funde = Fundstellen("probe.wiki", string.Join("\n", zeilen), begriffe);

            Assert.True(funde.Count == 0,
                "Der Waechter schlaegt bei erlaubten Begriffen an - Normen, Datenquellen, " +
                "Gattungsbegriffe und neutrale Beispielnamen muessen durchgehen:\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Gegenprobe zur Quelle (1):</b> Die Testdatenbank liefert wirklich eine
        /// nennenswerte Zahl von Katalognamen, und die Platzhalter sind heraus. Eine leere
        /// oder halbe Liste liefe grün durch, ohne je etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Die_Katalognamen_kommen_vollzaehlig_und_ohne_Platzhalter()
        {
            if (!_db.Vorhanden) return;

            List<string> namen = Katalognamen();

            Assert.True(namen.Count > 150,
                "Nur " + namen.Count + " Katalognamen aus der Testdatenbank - erwartet " +
                "werden mehrere hundert. Steht die Datenbank? Heissen die Spalten noch so?");

            // Hersteller in voller UND gekuerzter Form.
            Assert.Contains("Viessmann", namen);
            Assert.Contains("STIEBEL ELTRON", namen);
            Assert.Contains("STIEBEL ELTRON GmbH & Co. KG", namen);
            Assert.Contains("SenerTec", namen);

            // Geraetenamen.
            Assert.Contains(namen, n => n.StartsWith("allSTOR exclusiv VPS", StringComparison.Ordinal));

            // Gebaeudesimulation G3: die Herstellerzeilen des Baustoffkatalogs - Hersteller und
            // Produkt -, die herstellerneutralen Normzeilen NICHT (eine Hilfeseite darf
            // "Stahlbeton" sagen).
            Assert.Contains("Wienerberger", namen);
            Assert.Contains("Ytong ThermUltra PP2-0,30", namen);
            Assert.DoesNotContain("Stahlbeton", namen);
            Assert.DoesNotContain("Kalksandstein 1800", namen);
            // ... auch die kurzen Herstellernamen und jeder Teil eines zusammengesetzten.
            Assert.Contains("H+H", namen);
            Assert.Contains("Sto", namen);
            Assert.Contains("Foamglas (Owens Corning)", namen);
            Assert.Contains("Foamglas", namen);
            Assert.Contains("Owens Corning", namen);
            Assert.Contains("Bachl", namen);
            Assert.Contains("Styrodur", namen);

            // Platzhalter NICHT.
            Assert.DoesNotContain("Muster", namen);
            Assert.DoesNotContain("meins", namen);
            Assert.DoesNotContain("test", namen);
            Assert.DoesNotContain("Test", namen);
            Assert.DoesNotContain(PlatzhalterGanz, namen);
            Assert.DoesNotContain(namen, n => n.StartsWith("Muster ", StringComparison.Ordinal));
            Assert.DoesNotContain(namen, n => Regex.IsMatch(n, @"^[Tt]est\d*$"));
            // Unter vier Zeichen steht nur ein Herstellername des Baustoffkatalogs, nichts unter drei.
            Assert.DoesNotContain(namen, n => n.Length < MINDESTLAENGE_HERSTELLER);
            var baustoffhersteller = new HashSet<string>(
                BaustoffSaattabelle.Hersteller.SelectMany(s => Herstellernamen(s.Hersteller)), StringComparer.Ordinal);
            Assert.All(namen.Where(n => n.Length < 4), n => Assert.Contains(n, baustoffhersteller));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über einen wirklich
        /// vorhandenen Bestand — beide Ablagen, mit Inhalt. Läge er auf einem leeren
        /// Ordner, wäre sein grünes Ergebnis wertlos.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_beide_Ablagen_der_Wikiquellen()
        {
            List<(string Datei, string Text)> quellen = Wikiquellen();

            Assert.True(quellen.Count >= 20, "Nur " + quellen.Count + " Wiki-Quellen gefunden.");
            Assert.Contains(quellen, q => q.Datei.StartsWith("EPOS.Kern/Allgemein/Hilfe/Berechnung/", StringComparison.Ordinal));
            Assert.Contains(quellen, q => q.Datei.StartsWith("Projekte/Wiki/", StringComparison.Ordinal));
            Assert.All(quellen, q => Assert.True(q.Text.Length > 0, q.Datei + " ist leer."));
        }

        // =====================================================================
        //  Zapfprofilgenerator (Konzept Kapitel 6 (e), ZU-Folge (c)): die Katalogtexte
        //  der Tab_Tww*_STAMM und die Ressourcen ZPG_/ZPGK_
        // =====================================================================

        /// <summary>
        /// <b>Kein Katalogtext der Tww-Kataloge nennt einen Hersteller, ein Produkt oder eine
        /// Typbezeichnung</b> (Umsetzungskonzept Zapfprofilgenerator, Kapitel 6 (e)): jede Textspalte
        /// jeder <c>Tab_Tww*_STAMM</c> der Testdatenbank — Bezeichner, Quellen, Ausgaben, Versionen,
        /// Parameterschlüssel und Kategorien — AUSSER der internen Spalte <c>Beleg</c>, die eine
        /// Sekundärquelle tragen darf und die weder Oberfläche, Bericht noch KiSicht zeigen. Dieselben
        /// drei Quellen der verbotenen Namen wie für die Wiki-Seiten.
        /// </summary>
        [Fact]
        public void Kein_Katalogtext_der_Tww_Kataloge_nennt_Hersteller_oder_Produktdaten()
        {
            if (!_db.Vorhanden) return;
            List<Suchbegriff> begriffe = AlleBegriffe();
            var funde = new List<string>();
            int texte = 0;
            foreach ((string tabelle, string spalte) in TwwTextspalten())
                foreach (string wert in Werte(tabelle, spalte))
                {
                    texte++;
                    funde.AddRange(Fundstellen(tabelle + "." + spalte, wert, begriffe));
                }

            Assert.True(texte > 100, "Nur " + texte + " Katalogtexte der Tww-Kataloge gelesen.");
            Assert.True(funde.Count == 0,
                "Diese Katalogtexte der Tww-Kataloge nennen einen Hersteller, ein Produkt oder eine " +
                "Typbezeichnung (Konzept Zapfprofilgenerator 6 (e)) - eine Sekundärquelle gehört in " +
                "die interne Spalte Beleg:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Keine Ressource des Zapfprofilgenerators</b> (<c>ZPG_…</c> samt der Satzmuster
        /// <c>ZPG_SATZ_…</c>, <c>ZPGK_…</c> des Katalogdialogs) nennt in einer der beiden Sprachen einen
        /// Hersteller, ein Produkt oder eine Typbezeichnung.
        /// </summary>
        [Fact]
        public void Keine_Ressource_des_Zapfprofilgenerators_nennt_Hersteller_oder_Produktdaten()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();
            var funde = new List<string>();
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (string datei in new[] { "Resource.resx", "Resource.en-US.resx" })
                foreach ((string k, string wert) in Zapfressourcen(datei))
                {
                    schluessel.Add(k);
                    funde.AddRange(Fundstellen(datei + ":" + k, wert, begriffe));
                }

            Assert.True(schluessel.Count > 1000, "Nur " + schluessel.Count + " Schlüssel ZPG_/ZPGK_ gelesen.");
            Assert.Contains("ZPGK_TITEL", schluessel);
            Assert.Contains("ZPG_SATZ_KATALOGIMPORT_NEUE_VERSION", schluessel);
            Assert.True(funde.Count == 0,
                "Diese Ressourcen des Zapfprofilgenerators nennen einen Hersteller, ein Produkt oder " +
                "eine Typbezeichnung:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Gegenprobe zu den Tww-Fällen:</b> ein Herstellername und ein Typcode in einem Katalogtext
        /// schlagen an; die Normbezeichnungen der Parameterschlüssel (<c>A100.…</c>, <c>W551.…</c>,
        /// <c>DIN4708.…</c>) und die freien Quellen des Paketteils nicht.
        /// </summary>
        [Fact]
        public void Gegenprobe_Katalogtexte_mit_Hersteller_schlagen_an_Normschluessel_nicht()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();
            Assert.NotEmpty(Fundstellen("Tab_TwwNutzungsart_STAMM.Bezeichner", "Wohnen mit Vitocal", begriffe));
            Assert.NotEmpty(Fundstellen("Tab_TwwNutzungsart_STAMM.Bedarf_Quelle", "Datenblatt JKM400M", begriffe));
            Assert.NotEmpty(Fundstellen("Tab_TwwParameter_STAMM.Schluessel", "A1000.Wert", begriffe));

            foreach (string erlaubt in new[]
                     {
                         "A100.Ladungsfaktor", "A100-Referenzprofil aus dem Katalog", "W551.Mindesttemperatur",
                         "DIN4708.Profil.Block.1.Anteil", "DIN18599.Wohnen.a", "Jordan/Vajen (IEA SHC Task 26)",
                         "ABl. L 239 vom 6.9.2013, Tabelle 1, Lastprofil L", "abgeleitet aus VDI 6002 Blatt 2"
                     })
                Assert.True(Fundstellen("probe", erlaubt, begriffe).Count == 0, erlaubt);
        }

        // =====================================================================
        //  Berichtsvorlagen (Konzept Berichtsvorlagen 6.3 Nr. 2, 12, Zeile „Produktdaten“)
        // =====================================================================

        /// <summary>
        /// <b>Der Kurzbericht je Sprache nennt keinen Hersteller, kein Produkt und keine Typbezeichnung</b> — weder im
        /// Rumpf noch in Kopf- und Fußzeile, Kommentaren oder Dokumenteigenschaften. Die Beispiele der Lehrvorlage
        /// tragen neutrale Namen mit runden Werten („Variante 1“, „10.000 €“). Die Platzhalter selbst sind Schlüssel,
        /// keine Namen; sie fallen vor der Prüfung heraus. Dieselben drei Quellen verbotener Namen wie für die
        /// Wiki-Seiten.
        /// </summary>
        [Fact]
        public void Der_Kurzbericht_nennt_keine_Hersteller_oder_Produktdaten()
        {
            List<Suchbegriff> begriffe = AlleBegriffe();
            var funde = new List<string>();
            int texte = 0;
            foreach (string datei in new[] { BerichtsvorlageDateiWacheTests.KURZBERICHT, BerichtsvorlageDateiWacheTests.KURZBERICHT_EN })
            {
                using DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc = BerichtsvorlageDateiWacheTests.Oeffnen(datei);
                if (doc == null) return;
                foreach ((string teil, string text) in Vorlagentexte(doc))
                {
                    texte++;
                    string ohneMarken = Regex.Replace(text, @"\{\{[^}]*\}\}", " ");
                    funde.AddRange(Fundstellen(datei + ":" + teil, ohneMarken, begriffe));
                }
            }

            Assert.True(texte >= 2 * 4, "Nur " + texte + " Textteile der Kurzberichte gelesen.");
            Assert.True(funde.Count == 0,
                "Der Kurzbericht nennt einen Hersteller, ein Produkt oder eine Typbezeichnung (Konzept Berichtsvorlagen 12) — " +
                "Beispiele tragen neutrale Namen mit runden Werten:\n" + string.Join("\n", funde));
        }

        /// <summary>Die Texte einer Word-Vorlage je Teil: Rumpf absatzweise, Kopf- und Fußzeilen, Kommentare, Eigenschaften.</summary>
        private static IEnumerable<(string Teil, string Text)> Vorlagentexte(DocumentFormat.OpenXml.Packaging.WordprocessingDocument doc)
        {
            static string Absaetze(DocumentFormat.OpenXml.OpenXmlElement wurzel)
                => string.Join("\n", wurzel.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText));
            var main = doc.MainDocumentPart;
            yield return ("Rumpf", Absaetze(main.Document.Body));
            foreach (var h in main.HeaderParts) yield return ("Kopfzeile", Absaetze(h.Header));
            foreach (var f in main.FooterParts) yield return ("Fußzeile", Absaetze(f.Footer));
            if (main.WordprocessingCommentsPart?.Comments != null)
                yield return ("Kommentare", Absaetze(main.WordprocessingCommentsPart.Comments));
            yield return ("Eigenschaften", string.Join("\n", doc.PackageProperties.Title, doc.PackageProperties.Description,
                                                       doc.PackageProperties.Creator, doc.PackageProperties.LastModifiedBy));
        }

        /// <summary>Die Textspalten aller <c>Tab_Tww*_STAMM</c> der Testdatenbank — ohne <c>Beleg</c>.</summary>
        private static IEnumerable<(string Tabelle, string Spalte)> TwwTextspalten()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE 'Tab_Tww%' ORDER BY name");
            if (t == null) yield break;
            foreach (DataRow r in t.Rows)
            {
                string tabelle = Convert.ToString(r[0]);
                if (!tabelle.EndsWith("_STAMM", StringComparison.Ordinal)) continue;
                DataTable s = DataRepository.GetDataTable("SELECT name, type FROM pragma_table_info(?)", new DbParam("@t", tabelle));
                foreach (DataRow z in s.Rows)
                {
                    string spalte = Convert.ToString(z[0]);
                    string typ = Convert.ToString(z[1]) ?? "";
                    if (spalte == "Beleg" || !typ.StartsWith("TEXT", StringComparison.OrdinalIgnoreCase)) continue;
                    yield return (tabelle, spalte);
                }
            }
        }

        /// <summary>Die Einträge <c>ZPG_…</c> und <c>ZPGK_…</c> einer Ressourcendatei, dekodiert.</summary>
        private static IEnumerable<(string Schluessel, string Wert)> Zapfressourcen(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Arbeitsbaum(), "EPOS.Kern", "MyResource", datei));
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>ZPGK?_[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                yield return (m.Groups["k"].Value, System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value));
        }

        // =====================================================================
        //  Die Regel als Funktion — dieselbe für den Bestand und die Gegenproben
        // =====================================================================

        /// <summary>Ein verbotener Name samt der Art seiner Herkunft.</summary>
        private sealed class Suchbegriff
        {
            public string Art;        // "Katalog" oder "Liste"
            public string Wort;
            public Regex Muster;
            public StringComparison Vergleich;
        }

        /// <summary>Alle Suchbegriffe: Katalognamen (mitwachsend) und feste Liste.</summary>
        private List<Suchbegriff> AlleBegriffe()
        {
            var begriffe = new List<Suchbegriff>();

            // (1) Katalognamen - ohne Ruecksicht auf Gross- und Kleinschreibung, weil ein
            //     Wikitext denselben Namen anders schreiben kann als der Katalog.
            foreach (string n in Katalognamen())
                begriffe.Add(Neu("Katalog", n, RegexOptions.IgnoreCase, StringComparison.OrdinalIgnoreCase));

            // (2) Feste Liste - case-sensitive, denn sie fuehrt Marken in ihrer eigenen
            //     Schreibweise ("NIBE" neben "Nibe", "LONGi" neben "Longi"), und ein
            //     unbekuemmerter Vergleich traefe gewoehnliche Woerter.
            foreach (string n in FesteHersteller)
                begriffe.Add(Neu("Liste", n, RegexOptions.None, StringComparison.Ordinal));

            return begriffe;
        }

        private static Suchbegriff Neu(string art, string wort, RegexOptions zusatz, StringComparison vergleich)
            => new Suchbegriff
            {
                Art = art,
                Wort = wort,
                Vergleich = vergleich,
                Muster = new Regex(@"(?<![\p{L}\p{N}_])" + Regex.Escape(wort) + @"(?![\p{L}\p{N}_])",
                                   RegexOptions.Compiled | RegexOptions.CultureInvariant | zusatz),
            };

        /// <summary>
        /// Alle Fundstellen eines Textes, je Fund eine Zeile „Datei:Zeile [Art] Treffer → Auszug".
        ///
        /// <para>Der billige <c>IndexOf</c> über den GANZEN Text steht vor der teuren
        /// Zeilenschleife: Von den rund dreihundert Begriffen kommt fast keiner vor, und
        /// ohne diesen Vorfilter liefe der Wächter dreihundert Regexe über jede Zeile
        /// jeder Datei.</para>
        /// </summary>
        private static List<string> Fundstellen(string datei, string text, List<Suchbegriff> begriffe)
        {
            var funde = new List<string>();

            List<Suchbegriff> kandidaten =
                begriffe.Where(b => text.IndexOf(b.Wort, b.Vergleich) >= 0).ToList();

            string[] zeilen = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];

                foreach (Suchbegriff b in kandidaten)
                {
                    Match m = b.Muster.Match(zeile);
                    if (m.Success) funde.Add(Meldung(datei, i + 1, b.Art, m.Value, zeile));
                }

                foreach (Match m in Typcode.Matches(zeile))
                {
                    if (IstNormkuerzel(m.Value)) continue;
                    funde.Add(Meldung(datei, i + 1, "Typcode", m.Value, zeile));
                }
            }

            return funde;
        }

        private static string Meldung(string datei, int zeile, string art, string treffer, string text)
            => datei + ":" + zeile + "  [" + art + "] " + treffer + "  -> " + Auszug(text);

        private static string Auszug(string zeile)
        {
            string k = zeile.Trim();
            return k.Length <= 110 ? k : k.Substring(0, 110) + " …";
        }

        /// <summary>
        /// Beginnt der Typcode-Treffer mit einem NORMKÜRZEL? Dann ist er eine Norm und kein
        /// Produkt (<c>VDI4640Pruefung</c>).
        /// </summary>
        private static bool IstNormkuerzel(string treffer)
        {
            foreach (string k in Normkuerzel)
                if (treffer.Length > k.Length
                    && treffer.StartsWith(k, StringComparison.Ordinal)
                    && char.IsDigit(treffer[k.Length]))
                    return true;
            // A100 und W551 als ganze Bezeichnung („A100", „A100-Referenzprofil"), nicht „A1000".
            foreach (string n in Normbezeichnungen)
                if (treffer.StartsWith(n, StringComparison.Ordinal)
                    && (treffer.Length == n.Length || !char.IsDigit(treffer[n.Length])))
                    return true;
            return false;
        }

        // =====================================================================
        //  Quelle (1): die Katalognamen der Testdatenbank — NUR LESEND
        // =====================================================================

        /// <summary>
        /// Hersteller- und Gerätenamen aller acht Kataloge und ihrer Projekttabellen, ohne
        /// Platzhalter, ohne Dubletten. Hersteller stehen doppelt drin: in voller Form und
        /// ohne Firmenzusatz.
        /// </summary>
        private List<string> Katalognamen()
        {
            var namen = new SortedSet<string>(StringComparer.Ordinal);
            if (!_db.Vorhanden) return namen.ToList();

            foreach (string rumpf in Katalogtabellen)
                foreach (string tabelle in new[] { rumpf + "_STAMM", rumpf })
                {
                    if (!DataRepository.TabelleVorhanden(tabelle)) continue;
                    List<string> spalten = DataRepository.SpaltenVonTabelle(tabelle);

                    foreach (string spalte in Herstellerspalten)
                    {
                        if (!spalten.Contains(spalte, StringComparer.OrdinalIgnoreCase)) continue;
                        foreach (string wert in Werte(tabelle, spalte))
                        {
                            Aufnehmen(namen, wert);
                            Aufnehmen(namen, OhneFirmenzusatz(wert));
                        }
                    }

                    // Die ERSTE vorhandene Namensspalte traegt den Geraetenamen.
                    string namensspalte = Namensspalten
                        .FirstOrDefault(s => spalten.Contains(s, StringComparer.OrdinalIgnoreCase));
                    if (namensspalte == null) continue;
                    foreach (string wert in Werte(tabelle, namensspalte))
                        Aufnehmen(namen, wert);
                }

            // Gebaeudesimulation G3: der Baustoffkatalog (Schritt S-A) und seine Projektkopie
            // tragen BEIDES - herstellerneutrale Normzeilen ("Stahlbeton", "Kalksandstein 1800"),
            // die eine Hilfeseite nennen darf, und Herstellerzeilen, die sie nicht nennen darf.
            // Aufgenommen werden deshalb nur die Zeilen MIT Hersteller: der Hersteller (voll und
            // ohne Firmenzusatz) und der Produktname. Die Spalte Hersteller fuehrt gepflegte Namen,
            // keine Platzhalter: Ein Hersteller zaehlt hier schon ab drei Zeichen ("H+H", "Sto"),
            // und ein zusammengesetzter Name zaehlt auch mit jedem seiner Teile
            // ("Foamglas (Owens Corning)" -> "Foamglas", "Owens Corning").
            foreach (string tabelle in new[] { SchemaKatalog.TAB_BAUSTOFF_STAMM, SchemaKatalog.TAB_BAUSTOFF })
            {
                if (!DataRepository.TabelleVorhanden(tabelle)) continue;
                DataTable t = DataRepository.GetDataTable(
                    "SELECT DISTINCT \"Hersteller\", \"Bezeichner\" FROM \"" + tabelle + "\" WHERE \"Hersteller\" IS NOT NULL");
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                {
                    foreach (string hersteller in Herstellernamen(Convert.ToString(r["Hersteller"])))
                    {
                        Aufnehmen(namen, hersteller, MINDESTLAENGE_HERSTELLER);
                        Aufnehmen(namen, OhneFirmenzusatz(hersteller), MINDESTLAENGE_HERSTELLER);
                    }
                    Aufnehmen(namen, Convert.ToString(r["Bezeichner"])?.Trim());
                }
            }

            return namen.ToList();
        }

        /// <summary>Mindestlänge eines Herstellernamens aus dem Baustoffkatalog (gepflegte Spalte, keine Platzhalter).</summary>
        private const int MINDESTLAENGE_HERSTELLER = 3;

        /// <summary>
        /// Die Namen eines Herstellereintrags: der ganze Eintrag und — bei der Form „Name (Zusatz)" —
        /// Name und Zusatz einzeln („Bachl (Styrodur)" → „Bachl", „Styrodur").
        /// </summary>
        private static IEnumerable<string> Herstellernamen(string eintrag)
        {
            string ganz = eintrag?.Trim();
            if (string.IsNullOrEmpty(ganz)) yield break;
            yield return ganz;

            int auf = ganz.IndexOf('(');
            int zu = ganz.LastIndexOf(')');
            if (auf <= 0 || zu <= auf) yield break;
            yield return ganz.Substring(0, auf).Trim();
            yield return ganz.Substring(auf + 1, zu - auf - 1).Trim();
        }

        /// <summary>Alle verschiedenen Werte einer Textspalte, leere ausgelassen.</summary>
        private static IEnumerable<string> Werte(string tabelle, string spalte)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT DISTINCT \"" + spalte + "\" FROM \"" + tabelle + "\"");
            if (t == null) yield break;

            foreach (DataRow r in t.Rows)
            {
                if (r[0] == null || r[0] == DBNull.Value) continue;
                string wert = Convert.ToString(r[0])?.Trim();
                if (!string.IsNullOrEmpty(wert)) yield return wert;
            }
        }

        /// <summary>Nimmt einen Namen auf, wenn er kein Platzhalter und lang genug ist (Vorgabe: vier Zeichen).</summary>
        private static void Aufnehmen(SortedSet<string> namen, string wert, int mindestlaenge = 4)
        {
            if (string.IsNullOrWhiteSpace(wert)) return;
            string k = wert.Trim();
            if (k.Length < mindestlaenge) return;
            if (IstPlatzhalter(k)) return;
            namen.Add(k);
        }

        /// <summary>
        /// Ist der Name ein Platzhalter der Testdatenbank? Verglichen wird das erste Wort
        /// ohne angehängte Ziffern — so fallen „Muster", „Muster 2500TL", „test", „test3"
        /// und „Test" gemeinsam heraus —, dazu der eine mehrteilige Platzhalter im Ganzen.
        /// </summary>
        private static bool IstPlatzhalter(string wert)
        {
            if (string.Equals(wert, PlatzhalterGanz, StringComparison.OrdinalIgnoreCase)) return true;

            int leer = wert.IndexOf(' ');
            string erstes = leer < 0 ? wert : wert.Substring(0, leer);
            erstes = erstes.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

            return Platzhalter.Contains(erstes, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Schneidet die Firmenzusätze von hinten ab, wiederholt, bis keiner mehr passt —
        /// „Viessmann GmbH &amp; Co" wird über „Viessmann GmbH" zu „Viessmann".
        /// </summary>
        private static string OhneFirmenzusatz(string firma)
        {
            string k = firma.Trim();
            bool weiter = true;
            while (weiter)
            {
                weiter = false;
                foreach (string zusatz in Firmenzusaetze)
                {
                    if (k.Length <= zusatz.Length) continue;
                    if (!k.EndsWith(zusatz, StringComparison.OrdinalIgnoreCase)) continue;
                    k = k.Substring(0, k.Length - zusatz.Length).TrimEnd(' ', '.', ',');
                    weiter = true;
                }
            }
            return k;
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>Alle Wiki-Quellen beider Ablagen: repo-relativer Pfad und Inhalt.</summary>
        private static List<(string Datei, string Text)> Wikiquellen()
        {
            string wurzel = Arbeitsbaum();
            var quellen = new List<(string, string)>();

            foreach (string ordner in Quellordner)
            {
                string voll = Path.Combine(wurzel, ordner.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(voll)) continue;

                foreach (string datei in Directory.EnumerateFiles(voll, "*.wiki").OrderBy(d => d, StringComparer.Ordinal))
                    quellen.Add((ordner + "/" + Path.GetFileName(datei), File.ReadAllText(datei)));
            }

            return quellen;
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>RepositoryOrdnungWacheTests</c>.</summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
