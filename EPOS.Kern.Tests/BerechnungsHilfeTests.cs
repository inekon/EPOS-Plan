using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Hilferubrik „Berechnung" (H13, Anwenderwunsch vom 06.09.2026).
    ///
    /// <para><b>Was hier geprüft wird — und warum.</b> Die Rechenwegseiten sind Texte, und
    /// Texte altern still. Der Prüfstand hält deshalb genau die Zusagen fest, die die
    /// Bauform des Pakets macht: Jede Seite trägt einen vollständigen KOPFBLOCK mit
    /// Seitenname, Stand und den Kerndateien, gegen die sie belegt ist, und jede Seite hat
    /// dieselben SECHS Abschnitte. Wer eine Seite ergänzt und den Kopf vergisst, sieht es
    /// hier und nicht erst im Wiki.</para>
    ///
    /// <para><b>Fassung 2 (06.09.2026).</b> Der Anwender wünschte die Definition der
    /// Parameter und Variablen und die Formeln „in mathematischer Schreibweise". Daraus
    /// kommen SIEBEN Abschnitte je Seite — der neue steht zwischen den Eingangsgrößen und
    /// dem Rechenweg — und drei weitere Zusagen: keine <c>math</c>-Auszeichnung und kein
    /// LaTeX-Befehl (diese Wikiinstallation hat KEINE Math-Erweiterung; beides erschiene
    /// dem Leser als Klartext), mindestens eine nummerierte Anzeige-Formel und die zwei
    /// Symboltabellen. Geprüft werden diese vier Zusagen für die Seiten des Pakets
    /// <b>Teil A</b>; die sieben Erzeugerseiten aus Teil B ziehen im eigenen Zweig nach,
    /// und erst nach der Zusammenführung gilt die Prüfung für alle dreizehn.</para>
    ///
    /// <para><b>Fassung 3 (06.09.2026).</b> Der Anwender wünschte die Formeln „mit
    /// mathematischen Zeichen … wie LaTeX" und die Definition jeder Variablen „unter der
    /// verwendeten Formel"; dazu lässt er die <b>Math-Erweiterung</b> im Wiki
    /// installieren. Damit dreht sich der Riegel der Fassung 2 um: <c>&lt;math&gt;</c>
    /// ist nicht mehr verboten, sondern die Bauform — jede Anzeige-Gleichung steht als
    /// <c>: &lt;math&gt;\displaystyle …&lt;/math&gt;  (n)</c>, und unmittelbar darunter
    /// steht je Zeichen eine Legendezeile <c>:: &lt;math&gt;…&lt;/math&gt; – Bedeutung
    /// [Einheit]</c>. Geblieben ist der Gedanke des Riegels: Ein Befehl außerhalb der
    /// vereinbarten LaTeX-Teilmenge fällt hier auf, denn WikiTexVC weist ihn ab und der
    /// Leser sähe eine rote Fehlermeldung statt einer Formel.</para>
    ///
    /// <para>Dazu die zwei Anschlüsse: Dateien mit führendem Unterstrich sind KEINE
    /// Wikiseiten (<c>_Index.wiki</c> ist die Rubrik-Startseite, <c>_Bezuege.wiki</c> die
    /// Arbeitsvorlage für den Anwender), und jede echte Seite erreicht das Wissen des
    /// Assistenten als eigener Abschnitt.</para>
    ///
    /// <para>Ohne Datenbank, ohne Oberfläche, ohne <c>Dienste.*</c>-Tausch — deshalb keine
    /// Sammlungsangabe.</para>
    /// </summary>
    public class BerechnungsHilfeTests
    {
        /// <summary>
        /// Die sechs Abschnitte, die JEDE Seite der Rubrik führt (Bauform des Pakets).
        /// Geprüft wird die Überschriftszeile im MediaWiki-Markup.
        /// </summary>
        private static readonly string[] Abschnitte =
        {
            "== Was berechnet wird ==",
            "== Eingangsgrößen ==",
            "== Rechenweg ==",
            "== Grenzen und Annahmen ==",
            "== Ergebnisse und wo sie stehen ==",
            "== Bezüge =="
        };

        /// <summary>
        /// Die SIEBEN Abschnitte der Fassung 2, in ihrer Reihenfolge. Der neue steht
        /// zwischen den Eingangsgrößen und dem Rechenweg — der Leser soll die Zeichen
        /// kennen, BEVOR er die erste Formel sieht.
        /// </summary>
        private static readonly string[] AbschnitteFassung2 =
        {
            "== Was berechnet wird ==",
            "== Eingangsgrößen ==",
            "== Formelzeichen und Parameter ==",
            "== Rechenweg ==",
            "== Grenzen und Annahmen ==",
            "== Ergebnisse und wo sie stehen ==",
            "== Bezüge =="
        };

        /// <summary>
        /// Eine Anzeige-Gleichung: eingerückte Zeile mit der laufenden Nummer am
        /// Zeilenende, gesetzt in <c>&lt;math&gt;</c> (Fassung 3) ODER in
        /// <c>&lt;big&gt;</c> (Fassung 2).
        /// </summary>
        /// <remarks>
        /// Das Oder ist ein Übergangszustand: Teil A stellt seine sechs Seiten um,
        /// Teil B seine sieben im eigenen Zweig. Bis beide zusammengeführt sind, liegen
        /// beide Formen im selben Ordner, und ein Wächter, der nur eine kennte, fiele
        /// rot aus, ohne dass jemand einen Fehler gemacht hätte.
        /// </remarks>
        // TODO Zusammenführung Fassung 3: auf "<math>" allein verengen.
        private static readonly Regex Anzeigeformel =
            new(@"^:\s*(?:<math>.+</math>|<big>.+</big>)(?:\s|&nbsp;)*\(\d+\)\s*$",
                RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>Eine Anzeige-Gleichung der Fassung 3 — LaTeX in <c>&lt;math&gt;</c>.</summary>
        private static readonly Regex Anzeigegleichung =
            new(@"^:\s*<math>.+</math>(?:\s|&nbsp;)*\(\d+\)\s*$",
                RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Eine Legendezeile der Fassung 3: doppelt eingerückt, beginnt mit dem
        /// Zeichen, das sie erklärt.
        /// </summary>
        private static readonly Regex Legendezeile =
            new(@"^::\s*<math>", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>Ein LaTeX-Befehl — <c>\frac</c>, <c>\sum</c>, <c>\cdot</c> …</summary>
        private static readonly Regex LatexBefehl =
            new(@"\\([A-Za-z]+)", RegexOptions.Compiled);

        /// <summary>
        /// Die LaTeX-Teilmenge der Rubrik (Fassung 3). Sie ist bewusst klein: Nur was
        /// WikiTexVC — der Prüfer der Math-Erweiterung — sicher kennt, darf in eine
        /// Seite. Ein Befehl außerhalb dieser Liste erschiene dem Leser als rote
        /// Fehlerzeile mitten im Rechenweg.
        /// </summary>
        /// <remarks>
        /// Die ersten fünf Zeilen sind die Liste des Auftrags. Die sechste hält die
        /// sechs Zeichen fest, die die Rechenwege dieses Teils führen und für die es
        /// kein Ersatzzeichen gibt: <c>\pi</c> (Kreisfrequenz des Jahresgangs, Kusuda),
        /// <c>\omega</c> (dieselbe, als Symbol der Parametertabelle), <c>\kappa</c>
        /// (Kaltwasserfaktor der Brauchwasser-Monatswerte), <c>\Psi</c>
        /// (Wärmebrückenverlustkoeffizient), <c>\ell</c> (Sondenmeter und
        /// Anschlusslängen) und <c>\dot</c> (der Massenstrom der Zeichentabelle auf
        /// der Rubrikstartseite). Alle sechs sind texvc-Kernbefehle.
        /// </remarks>
        private static readonly HashSet<string> ErlaubteBefehle = new(StringComparer.Ordinal)
        {
            "frac", "sqrt", "sum", "int", "prod", "min", "max", "cdot",
            "left", "right", "lvert", "rvert",
            "mathrm", "text", "operatorname", "displaystyle", "begin", "end", "quad",
            "le", "ge", "ne", "approx", "pm", "to", "infty", "in", "dots",
            "eta", "vartheta", "rho", "lambda", "alpha", "beta", "gamma",
            "varepsilon", "tau", "varphi", "Delta", "Sigma",
            "pi", "omega", "kappa", "Psi", "ell", "dot"
        };

        /// <summary>Die Kopfzeile der Parametertabelle.</summary>
        private const string KOPF_PARAMETER = "! Symbol !! Bedeutung !! Einheit !! Herkunft";

        /// <summary>Die Kopfzeile der Variablentabelle.</summary>
        private const string KOPF_VARIABLEN = "! Symbol !! Bedeutung !! Einheit !! berechnet in";

        // =====================================================================
        //  Bestand
        // =====================================================================

        /// <summary>
        /// Die DREIZEHN Seiten der Rubrik H13 — namentlich (seit der Zusammenführung von
        /// Teil A und Teil B am 06.09.2026 beide Teile). Eine gelöschte oder
        /// umbenannte Datei fiele sonst nur dadurch auf, dass niemand sie mehr findet;
        /// der Infoknopf des zugehörigen Dialogs zeigte weiter auf eine Wikiseite, die
        /// es im Quellbaum nicht mehr gibt.
        /// </summary>
        /// <remarks>
        /// Geprüft wird „mindestens", nicht „genau" — jede weitere Seite ist willkommen
        /// und läuft durch dieselben Fälle.
        /// </remarks>
        public static readonly string[] SeitenDerRubrik =
        {
            "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
            "Strombedarf", "Wärmequelle Erdreich", "Heizkessel", "BHKW", "Wärmepumpe",
            "Pufferspeicher", "Solarthermie", "Photovoltaik", "Stromspeicher"
        };

        /// <summary>
        /// Die SECHS Seiten, die dieses Paket (Teil A der Fassung 3) auf LaTeX
        /// umgestellt hat. Nur für sie gelten die Fassung-3-Fälle — die sieben
        /// Erzeugerseiten aus Teil B liegen im selben Ordner noch in der Fassung 2 und
        /// fielen sonst rot aus, ohne dass jemand einen Fehler gemacht hätte.
        /// </summary>
        // TODO Zusammenführung Fassung 3: auf SeitenDerRubrik umstellen
        public static readonly string[] SeitenDiesesTeils =
        {
            "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
            "Strombedarf", "Wärmequelle Erdreich"
        };

        /// <summary>
        /// Die Rubrik ist eingebettet und lesbar, und sie führt die dreizehn Seiten des
        /// Pakets. Ein leerer Bestand liefe sonst durch jeden folgenden Fall grün
        /// hindurch, ohne je etwas geprüft zu haben — die <c>.wiki</c>-Dateien hängen an
        /// einem <c>EmbeddedResource</c>-Muster in <c>EPOS.Kern.csproj</c>, und ein
        /// Tippfehler im <c>LogicalName</c> fällt genau hier auf.
        /// </summary>
        [Fact]
        public void Die_Rubrik_ist_eingebettet_und_traegt_ihre_Seiten()
        {
            IReadOnlyList<BerechnungsSeite> seiten = BerechnungsHilfe.Seiten;

            Assert.NotNull(seiten);
            Assert.True(seiten.Count >= SeitenDerRubrik.Length,
                "Die Rubrik 'Berechnung' führt nur " + seiten.Count + " Seite(n), erwartet sind " +
                "mindestens " + SeitenDerRubrik.Length + ". Ist das EmbeddedResource-Muster " +
                "'Allgemein\\Hilfe\\Berechnung\\*.wiki' mit LogicalName '" +
                BerechnungsHilfe.RESSOURCE_VORSATZ + "%(Filename)%(Extension)' noch in EPOS.Kern.csproj?");

            var fehlend = SeitenDerRubrik
                .Where(n => BerechnungsHilfe.Seite(n) == null)
                .ToList();

            Assert.True(fehlend.Count == 0,
                "Diese Seiten des Pakets H13 fehlen: " + string.Join(", ", fehlend));
        }

        /// <summary>
        /// Kein Seitenname doppelt — zwei Seiten gleichen Namens wären im Wiki eine.
        /// </summary>
        [Fact]
        public void Jeder_Seitenname_kommt_genau_einmal_vor()
        {
            var doppelt = BerechnungsHilfe.Seiten
                .GroupBy(s => s.Seitenname, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.True(doppelt.Count == 0, "Doppelte Seitennamen: " + string.Join(", ", doppelt));
        }

        // =====================================================================
        //  Kopfblock
        // =====================================================================

        /// <summary>
        /// Der Kopfblock ist die Beleglage: Er nennt die Seite, den Stand und die Dateien
        /// des Rechenkerns, gegen die der Text geschrieben wurde. Fehlt eine der drei
        /// Angaben, ist der Text nicht mehr nachprüfbar.
        /// </summary>
        [Fact]
        public void Jede_Seite_traegt_einen_vollstaendigen_Kopfblock()
        {
            var funde = new List<string>();

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                if (string.IsNullOrWhiteSpace(seite.Seitenname)) funde.Add("(ohne Namen): Feld 'Seite' fehlt");
                if (string.IsNullOrWhiteSpace(seite.Stand)) funde.Add(seite.Seitenname + ": Feld 'Stand' fehlt");
                if (string.IsNullOrWhiteSpace(seite.Rechenkern))
                    funde.Add(seite.Seitenname + ": Feld 'Rechenkern' fehlt");
            }

            Assert.True(funde.Count == 0,
                "Diese Seiten der Rubrik 'Berechnung' haben keinen vollständigen Kopfblock " +
                "(Muster: <!-- EPOS-Plan Hilferubrik Berechnung | Seite: … | Stand: JJJJ-MM-TT | " +
                "Rechenkern: … -->):\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Der Stand BEGINNT mit einem Datum der Form <c>JJJJ-MM-TT</c> — sonst ist er
        /// nicht sortierbar.
        ///
        /// <para>Seit der Fassung 2 darf dahinter ein Zusatz in runden Klammern stehen
        /// (<c>Stand: 2026-09-06 (Fassung 2: Formelzeichen und Notation)</c>). Er sagt dem
        /// Leser der Wikiseite, WELCHE Überarbeitung er vor sich hat; das Datum bleibt die
        /// sortierbare Angabe und steht deshalb vorn.</para>
        /// </summary>
        [Fact]
        public void Der_Stand_beginnt_mit_einem_Datum()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Match datum = Regex.Match(seite.Stand ?? "", @"^(\d{4}-\d{2}-\d{2})(\s*\(.+\))?$");

                Assert.True(datum.Success,
                    seite.Seitenname + ": '" + seite.Stand + "' ist kein Stand der Form " +
                    "'JJJJ-MM-TT' oder 'JJJJ-MM-TT (Zusatz)'.");

                Assert.True(DateTime.TryParseExact(datum.Groups[1].Value, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _),
                    seite.Seitenname + ": '" + datum.Groups[1].Value + "' ist kein gültiges Datum.");
            }
        }

        /// <summary>
        /// Der Kopfblock nennt Dateien des Rechenkerns, nicht irgendetwas. Geprüft wird
        /// die Form: mindestens ein Pfad, der mit <c>EPOS.Kern/</c> beginnt.
        /// </summary>
        [Fact]
        public void Der_Kopfblock_nennt_Dateien_des_Rechenkerns()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.True(seite.Rechenkern.IndexOf("EPOS.Kern/", StringComparison.Ordinal) >= 0,
                    seite.Seitenname + ": Der Kopfblock nennt keine Datei unter EPOS.Kern/ — " +
                    "gefunden: '" + seite.Rechenkern + "'.");
            }
        }

        // =====================================================================
        //  Gliederung
        // =====================================================================

        /// <summary>
        /// Dieselbe Gliederung auf jeder Seite — das ist die Zusage an den Leser: Er weiß
        /// immer, wo die Vorgabewerte stehen und wo die Grenzen.
        /// </summary>
        [Fact]
        public void Jede_Seite_hat_die_sechs_Abschnitte()
        {
            var funde = new List<string>();

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
                foreach (string abschnitt in Abschnitte)
                    if (seite.Markup.IndexOf(abschnitt, StringComparison.Ordinal) < 0)
                        funde.Add(seite.Seitenname + ": '" + abschnitt + "' fehlt");

            Assert.True(funde.Count == 0,
                "Diese Abschnitte fehlen (Bauform H13: sechs Abschnitte je Seite):\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Fassung 2:</b> Die Seiten dieses Pakets führen SIEBEN Abschnitte, und zwar in
        /// der vorgegebenen Reihenfolge. Der neue Abschnitt steht zwischen den
        /// Eingangsgrößen und dem Rechenweg — stünde er hinter dem Rechenweg, läse der
        /// Anwender die Formeln, bevor er die Zeichen kennt.
        /// </summary>
        /// <remarks>
        /// Seit der Zusammenführung beider Teile (06.09.2026) gilt die Fassung 2 für alle
        /// dreizehn Seiten der Rubrik (H13‑O‑5 geschlossen).
        /// </remarks>
        [Theory]
        [MemberData(nameof(AlleSeitenDerRubrik))]
        public void Jede_Seite_hat_die_sieben_Abschnitte_in_Reihenfolge(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            int gelesen = -1;

            foreach (string abschnitt in AbschnitteFassung2)
            {
                int stelle = seite!.Markup.IndexOf(abschnitt, StringComparison.Ordinal);

                Assert.True(stelle >= 0, seitenname + ": '" + abschnitt + "' fehlt.");
                Assert.True(stelle > gelesen,
                    seitenname + ": '" + abschnitt + "' steht vor dem vorangehenden Abschnitt — " +
                    "die Reihenfolge der Bauform ist: " + string.Join(", ", AbschnitteFassung2));

                gelesen = stelle;
            }
        }

        /// <summary>
        /// <b>Fassung 3:</b> Jeder LaTeX-Befehl einer Seite steht in der vereinbarten
        /// Teilmenge. Die Fassung 2 verbot Backslash-Befehle ganz, weil das Wiki keine
        /// Math-Erweiterung hatte; seit der Anwender sie installieren lässt, ist nicht
        /// mehr der Befehl das Problem, sondern der UNBEKANNTE Befehl: WikiTexVC weist
        /// ihn ab, und an der Stelle der Formel steht eine rote Fehlerzeile.
        /// </summary>
        /// <remarks>
        /// Der Fall gilt für alle dreizehn Seiten und ist für die sieben Seiten aus
        /// Teil B trivial erfüllt — sie führen (noch) gar keinen Befehl.
        /// </remarks>
        [Theory]
        [MemberData(nameof(AlleSeitenDerRubrik))]
        public void Jede_Seite_haelt_sich_an_die_LaTeX_Teilmenge(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            var fremd = LatexBefehl.Matches(seite!.Markup)
                .Select(m => m.Groups[1].Value)
                .Where(b => !ErlaubteBefehle.Contains(b))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.True(fremd.Count == 0,
                seitenname + ": Befehl(e) außerhalb der LaTeX-Teilmenge der Rubrik: \\" +
                string.Join(", \\", fremd) + ". Erlaubt ist nur, was WikiTexVC sicher " +
                "kennt — die Liste steht in " + nameof(ErlaubteBefehle) + ".");
        }

        /// <summary>
        /// <b>Fassung 3:</b> Die Seiten DIESES Teils setzen jede Anzeige-Gleichung als
        /// LaTeX in <c>&lt;math&gt;</c> und stellen ihr die Legende unmittelbar darunter.
        /// Genau das war der Anwenderwunsch: „Die Definitionen der Parameter/Variablen …
        /// sollte unter der verwendeten Formel beschrieben werden."
        /// </summary>
        /// <remarks>
        /// Eine Gleichung ohne Legende ist der Rückfall, gegen den dieser Fall steht:
        /// Sie sieht gesetzt aus und lässt den Leser trotzdem mit unerklärten Zeichen
        /// zurück — und dann springt er zur Symboltabelle zurück, statt zu lesen.
        /// </remarks>
        [Theory]
        [MemberData(nameof(AlleSeitenDiesesTeils))]
        public void Jede_Seite_dieses_Teils_setzt_die_Gleichungen_in_LaTeX(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            string[] zeilen = seite!.Markup.Replace("\r\n", "\n").Split('\n');

            Assert.DoesNotContain("<big>", seite.Markup, StringComparison.Ordinal);

            var nummern = new List<int>();

            for (int i = 0; i < zeilen.Length; i++)
            {
                Match gleichung = Anzeigegleichung.Match(zeilen[i]);
                if (!gleichung.Success) continue;

                nummern.Add(int.Parse(Regex.Match(zeilen[i], @"\((\d+)\)\s*$").Groups[1].Value,
                                      System.Globalization.CultureInfo.InvariantCulture));

                string darunter = i + 1 < zeilen.Length ? zeilen[i + 1] : "";
                Assert.True(Legendezeile.IsMatch(darunter),
                    seitenname + ": auf die Zeile '" + zeilen[i].Trim() + "' folgt keine " +
                    "Legendezeile. Muster: ':: <math>P_{\\mathrm{el}}</math> – elektrische " +
                    "Nennleistung [kW]'.");
            }

            Assert.True(nummern.Count >= 1,
                seitenname + ": keine Anzeige-Gleichung in <math> gefunden.");
            Assert.Equal(Enumerable.Range(1, nummern.Count).ToList(), nummern);
        }

        /// <summary>
        /// <b>Fassung 3:</b> Der Kopfblock der Seiten dieses Teils nennt die Fassung.
        /// Ohne diesen Zusatz stünde im Wiki eine Seite mit LaTeX-Formeln und einem
        /// Stand, der genauso gut die Unicode-Fassung meinen könnte.
        /// </summary>
        [Theory]
        [MemberData(nameof(AlleSeitenDiesesTeils))]
        public void Der_Stand_dieses_Teils_nennt_die_Fassung_3(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            Assert.Equal("2026-09-06 (Fassung 3: LaTeX-Formeln und Legenden)", seite!.Stand);
        }

        /// <summary>
        /// <b>Fassung 2:</b> Jede Seite trägt mindestens EINE nummerierte Anzeige-Formel.
        /// Ohne sie wäre der Abschnitt „Formelzeichen und Parameter" eine Zeichenliste ohne
        /// Formel, auf die er sich beziehen könnte.
        /// </summary>
        /// <remarks>
        /// Der Fall nimmt beide Formen an — <c>&lt;math&gt;</c> der Fassung 3 wie
        /// <c>&lt;big&gt;</c> der Fassung 2 —, weil bis zur Zusammenführung beide im
        /// selben Ordner liegen. Dass die sechs Seiten dieses Teils WIRKLICH
        /// <c>&lt;math&gt;</c> führen, hält
        /// <see cref="Jede_Seite_dieses_Teils_setzt_die_Gleichungen_in_LaTeX"/> fest.
        /// </remarks>
        // TODO Zusammenführung Fassung 3: das Oder in Anzeigeformel auflösen
        [Theory]
        [MemberData(nameof(AlleSeitenDerRubrik))]
        public void Jede_Seite_traegt_nummerierte_Anzeigeformeln(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            MatchCollection formeln = Anzeigeformel.Matches(seite!.Markup);

            Assert.True(formeln.Count >= 1,
                seitenname + ": keine Anzeige-Formel gefunden. Muster einer solchen Zeile: " +
                "': <math>\\displaystyle P_{\\mathrm{AC}}(t) = \\min( … )</math>  (3)' " +
                "(Fassung 3) oder ': <big>P<sub>AC</sub>(t) = min( … )</big>  (3)' (Fassung 2).");

            // Die Nummern laufen je Seite von 1 an und ohne Lücke - sonst verweist der
            // Text auf eine Gleichung, die es nicht gibt.
            var nummern = formeln
                .Select(m => int.Parse(Regex.Match(m.Value, @"\((\d+)\)\s*$").Groups[1].Value,
                                       System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            Assert.Equal(Enumerable.Range(1, nummern.Count).ToList(), nummern);
        }

        /// <summary>
        /// <b>Fassung 2:</b> Der neue Abschnitt trägt BEIDE Tabellen — Parameter (was
        /// hereinkommt) und Variablen (was die Seite rechnet). Eine Seite mit nur einer
        /// von beiden ließe offen, welche Zahl der Anwender eingibt und welche das
        /// Programm bildet; genau diese Trennung war der Anwenderwunsch.
        /// </summary>
        [Theory]
        [MemberData(nameof(AlleSeitenDerRubrik))]
        public void Jede_Seite_traegt_beide_Symboltabellen(string seitenname)
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seite(seitenname);
            Assert.True(seite != null, "Seite '" + seitenname + "' nicht gefunden.");

            Assert.True(seite!.Markup.IndexOf(KOPF_PARAMETER, StringComparison.Ordinal) >= 0,
                seitenname + ": die Parametertabelle fehlt (Kopfzeile '" + KOPF_PARAMETER + "').");
            Assert.True(seite.Markup.IndexOf(KOPF_VARIABLEN, StringComparison.Ordinal) >= 0,
                seitenname + ": die Variablentabelle fehlt (Kopfzeile '" + KOPF_VARIABLEN + "').");
        }

        /// <summary>Alle dreizehn Seiten der Rubrik als Theoriedaten.</summary>
        public static TheoryData<string> AlleSeitenDerRubrik()
        {
            var daten = new TheoryData<string>();
            foreach (string name in SeitenDerRubrik) daten.Add(name);
            return daten;
        }

        /// <summary>Die sechs Seiten dieses Teils als Theoriedaten (Fassung 3).</summary>
        // TODO Zusammenführung Fassung 3: auf AlleSeitenDerRubrik umstellen
        public static TheoryData<string> AlleSeitenDiesesTeils()
        {
            var daten = new TheoryData<string>();
            foreach (string name in SeitenDiesesTeils) daten.Add(name);
            return daten;
        }

        /// <summary>
        /// Der Abschnitt „Bezüge" verweist wirklich — mindestens ein Wikilink. Ohne ihn
        /// wäre die Rubrik eine Sackgasse, und genau das wollte der Anwenderwunsch nicht
        /// („aufrufbar aus den allgemeinen Erklärungen mit Bezügen").
        /// </summary>
        [Fact]
        public void Jede_Seite_verweist_auf_die_allgemeine_Dokumentation()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.True(seite.Markup.IndexOf("[[Programm Dokumentation", StringComparison.Ordinal) >= 0,
                    seite.Seitenname + ": kein Verweis auf die Rubrik 'Programm Dokumentation'.");
            }
        }

        // =====================================================================
        //  Beiwerk
        // =====================================================================

        /// <summary>
        /// Dateien mit führendem Unterstrich sind KEINE Wikiseiten: <c>_Index.wiki</c> ist
        /// die Rubrik-Startseite, <c>_Bezuege.wiki</c> die Vorlage der Abschnitte, die der
        /// Anwender in die allgemeinen Seiten einfügt. Kämen sie als Seiten durch, stünden
        /// sie im Wissen des Assistenten und in jeder Seitenliste.
        /// </summary>
        [Fact]
        public void Dateien_mit_Unterstrich_sind_keine_Seiten()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
                Assert.False(seite.Seitenname.StartsWith("_", StringComparison.Ordinal),
                    "'" + seite.Seitenname + "' beginnt mit einem Unterstrich und ist keine Wikiseite.");

            // Gegenprobe: Das Beiwerk ist wirklich vorhanden - sonst prüfte der Fall nichts.
            string[] ressourcen = typeof(BerechnungsHilfe).Assembly.GetManifestResourceNames();
            Assert.Contains(ressourcen,
                r => r.StartsWith(BerechnungsHilfe.RESSOURCE_VORSATZ + "_", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Klartext
        // =====================================================================

        /// <summary>
        /// Der Klartext ist das, was der Assistent liest. Der Kopfblock gehört nicht in
        /// den Prompt (er nennt Quelltextpfade), die Auszeichnung auch nicht — jede Zahl
        /// dagegen schon.
        /// </summary>
        [Fact]
        public void Der_Klartext_traegt_keinen_Kopfblock_und_keine_Auszeichnung()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.False(string.IsNullOrWhiteSpace(seite.Klartext), seite.Seitenname + ": Klartext leer.");
                Assert.DoesNotContain("<!--", seite.Klartext);
                Assert.DoesNotContain("EPOS.Kern/", seite.Klartext);
                Assert.DoesNotContain("== ", seite.Klartext);
                Assert.DoesNotContain("'''", seite.Klartext);
                Assert.DoesNotContain("[[", seite.Klartext);

                // Fassung 2: die HTML-Klammern der Formelschreibweise sind umgesetzt,
                // nicht mitgeschleppt.
                Assert.DoesNotContain("<sub>", seite.Klartext);
                Assert.DoesNotContain("<sup>", seite.Klartext);
                Assert.DoesNotContain("<big>", seite.Klartext);

                // Fassung 3: dasselbe für die LaTeX-Formeln. Ein durchgereichtes
                // "\frac{a}{b}" wäre für den Assistenten kein Bruch, sondern Text.
                Assert.DoesNotContain("<math>", seite.Klartext);
                Assert.DoesNotContain("\\frac", seite.Klartext);
                Assert.DoesNotContain("\\displaystyle", seite.Klartext);
                Assert.DoesNotContain("\\mathrm", seite.Klartext);
            }
        }

        // =====================================================================
        //  Klartext — die LaTeX-Umsetzung der Fassung 3
        // =====================================================================

        /// <summary>
        /// <b>Fassung 3 — der Bruch.</b> Der Assistent hat keinen Formelsetzer; ein
        /// <c>\frac</c> muss deshalb in eine Zeile übergehen, die sich vorlesen lässt.
        /// Der Beispielfall ist der aus dem Anwenderwunsch (Stromkennzahl des BHKW).
        /// </summary>
        [Fact]
        public void Der_Klartext_macht_aus_einem_Bruch_eine_Zeile()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <math>\\displaystyle \\mathrm{SKZ} = \\frac{P_{\\mathrm{el}}}{P_{\\mathrm{th}}}</math> &nbsp;&nbsp;(2)\n");

            Assert.Contains("SKZ = (P_el)/(P_th)", klartext);
            Assert.Contains("(2)", klartext);
            Assert.DoesNotContain("\\", klartext);
            Assert.DoesNotContain("{", klartext);
        }

        /// <summary>
        /// <b>Fassung 3 — die Summe mit ihren Grenzen.</b> Ohne Umsetzung stünde im
        /// Prompt <c>\sum_{t=1}^{8\,760}</c>; der Anwender fragte danach mit „Σ".
        /// </summary>
        [Fact]
        public void Der_Klartext_macht_aus_einer_Summe_ihr_Zeichen()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <math>\\displaystyle Q_{\\mathrm{a}} = \\frac{\\sum_{t=1}^{8\\,760} Q(t)}{1\\,000}</math> &nbsp;&nbsp;(23)\n");

            Assert.Contains("Q_a = (Σ_t=1^8760 Q(t))/(1000)", klartext);
            Assert.DoesNotContain("sum", klartext);
        }

        /// <summary>
        /// <b>Fassung 3 — Index und Hochzahl.</b> Die Klammern von <c>_{…}</c> und
        /// <c>^{…}</c> fallen weg, der mehrteilige Index bleibt zusammen. Ohne diesen
        /// Schritt läse der Assistent „PAC,nenn" statt „P_AC,nenn".
        /// </summary>
        [Fact]
        public void Der_Klartext_loest_Index_und_Hochzahl_auf()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <math>\\displaystyle P_{\\mathrm{AC,nenn}} = A \\cdot 10^{6} \\cdot \\sqrt{2}</math> &nbsp;&nbsp;(1)\n" +
                ":: <math>P_{\\mathrm{AC,nenn}}</math> – Nennleistung des Wechselrichters [kW]\n");

            Assert.Contains("P_AC,nenn = A · 10^6 · √(2)", klartext);
            Assert.Contains("P_AC,nenn – Nennleistung des Wechselrichters [kW]", klartext);
        }

        /// <summary>
        /// <b>Fassung 3 — die Fallunterscheidung.</b> Eine <c>cases</c>-Umgebung ist im
        /// Wiki eine geschweifte Klammer über zwei Zeilen; im Prompt wird daraus ein
        /// Satz, den der Assistent wiedergeben kann.
        /// </summary>
        [Fact]
        public void Der_Klartext_macht_aus_einer_Fallunterscheidung_einen_Satz()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <math>\\displaystyle f = \\begin{cases} 1 + 0{,}025 \\cdot \\vartheta_{\\mathrm{a}}(d) & " +
                "\\vartheta_{\\mathrm{a}}(d) < 0 \\\\ 1 & \\vartheta_{\\mathrm{a}}(d) \\ge 0 \\end{cases}</math> &nbsp;&nbsp;(5)\n");

            Assert.Contains("f = { 1 + 0,025 · ϑ_a(d) wenn ϑ_a(d) < 0; 1 wenn ϑ_a(d) ≥ 0 }", klartext);
            Assert.DoesNotContain("cases", klartext);
        }

        /// <summary>
        /// <b>Fassung 3 — die griechischen Befehle.</b> Sie werden zu ihrem Zeichen;
        /// der Anwender findet in der Antwort dasselbe η wieder, das auf der Wikiseite
        /// steht. Geprüft ist auch, dass <c>\in</c> nicht in <c>\infty</c> hineingreift.
        /// </summary>
        [Fact]
        public void Der_Klartext_setzt_griechische_Befehle_in_Zeichen_um()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <math>\\displaystyle \\eta_{\\mathrm{a}} \\le 1, \\quad \\Delta\\vartheta = 50, \\quad " +
                "\\lambda \\approx \\rho \\cdot \\kappa \\cdot \\omega \\cdot \\pi, \\quad \\ell \\ne \\infty, \\quad " +
                "\\tau \\in \\{1, \\dots, n\\}, \\quad \\Psi \\pm \\varepsilon \\to \\varphi</math> &nbsp;&nbsp;(1)\n");

            Assert.Contains("η_a ≤ 1", klartext);
            Assert.Contains("Δϑ = 50", klartext);
            Assert.Contains("λ ≈ ρ · κ · ω · π", klartext);
            Assert.Contains("ℓ ≠ ∞", klartext);
            Assert.Contains("τ ∈ 1, …, n", klartext);
            Assert.Contains("Ψ ± ε → φ", klartext);
        }

        /// <summary>
        /// <b>Gegenprobe zur LaTeX-Umsetzung:</b> Sie greift die Auszeichnung und lässt
        /// den Satz in Ruhe. Ein deutscher Text ohne Formel geht unverändert durch, und
        /// eine Legendezeile behält ihre Bedeutung samt Einheit.
        /// </summary>
        [Fact]
        public void Die_LaTeX_Umsetzung_laesst_den_Satz_in_Ruhe()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                "Der Faktor 0,83 gilt fuer Wand und Waermebruecken.\n" +
                ":: <math>\\eta</math> – Jahresnutzungsgrad der Bestandsanlage [–]\n");

            Assert.Contains("Der Faktor 0,83 gilt fuer Wand und Waermebruecken.", klartext);
            Assert.Contains("η – Jahresnutzungsgrad der Bestandsanlage [–]", klartext);
        }

        /// <summary>
        /// <b>Fassung 2 — die Formelschreibweise im Prompt.</b> Der Assistent bekommt keinen
        /// HTML-Fähigen Anzeigebereich; ein tiefgestellter Index muss deshalb in Zeichen
        /// übergehen, die er lesen und wiedergeben kann. Ohne diese Umsetzung fräße die
        /// Tag-Entfernung die Auszeichnung SAMT Trennzeichen, und aus
        /// <c>P&lt;sub&gt;AC,nenn&lt;/sub&gt;</c> würde das stumme <c>PAC,nenn</c> — der
        /// Anwender fände „P_AC,nenn" der Wikiseite in keiner Antwort wieder.
        /// </summary>
        [Fact]
        public void Der_Klartext_setzt_Indizes_und_Hochzahlen_in_lesbare_Zeichen_um()
        {
            const string markup =
                "== Rechenweg ==\n" +
                ": <big>P<sub>AC</sub>(t) = min( P<sub>DC</sub>(t) · η<sub>WR</sub>(x(t)) , " +
                "P<sub>AC,nenn</sub> )</big>  (3)\n" +
                "Die Flaeche geht mit A<sup>2</sup> ein.\n";

            string klartext = BerechnungsHilfe.AlsKlartext(markup);

            Assert.Contains("P_AC(t) = min( P_DC(t) · η_WR(x(t)) , P_AC,nenn )", klartext);
            Assert.Contains("(3)", klartext);
            Assert.Contains("A^2", klartext);

            Assert.DoesNotContain("<sub>", klartext);
            Assert.DoesNotContain("</sub>", klartext);
            Assert.DoesNotContain("<sup>", klartext);
            Assert.DoesNotContain("<big>", klartext);
            Assert.DoesNotContain("</big>", klartext);
        }

        /// <summary>
        /// <b>Gegenprobe zur Umsetzung:</b> Sie greift nur die zwei Klammern und lässt
        /// alles andere in Ruhe — die Unicode-Zeichen der Notation kommen unverändert an.
        /// </summary>
        [Fact]
        public void Der_Klartext_behaelt_die_Unicode_Zeichen_der_Notation()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                ": <big>Q<sub>a</sub> = ( Σ<sub>t=1…8 760</sub> Q(t) ) / 1 000</big>  (4)\n" +
                "Grenzen: ϑ ≥ 0 °C, η ≤ 1, Δϑ = 50 K, √2, ρ · c<sub>p</sub>, ṁ ≠ 0\n");

            Assert.Contains("Q_a = ( Σ_t=1…8 760 Q(t) ) / 1 000", klartext);
            Assert.Contains("ϑ ≥ 0 °C, η ≤ 1, Δϑ = 50 K, √2, ρ · c_p, ṁ ≠ 0", klartext);
        }

        /// <summary>
        /// Geschützte Leerzeichen (H13 Fassung 2: „&lt;/big&gt; &amp;nbsp;&amp;nbsp;(3)", „8&amp;nbsp;760")
        /// werden zu gewöhnlichen — der Assistent liest „8 760" und „(3)", nicht die Entität.
        /// </summary>
        [Fact]
        public void Der_Klartext_loest_geschuetzte_Leerzeichen_auf()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(
                "== Rechenweg ==\n: <big>P<sub>AC</sub>(t) = 8&nbsp;760 · x</big> &nbsp;&nbsp;(3)\n");

            Assert.DoesNotContain("&nbsp;", klartext, StringComparison.Ordinal);
            Assert.Contains("8 760", klartext, StringComparison.Ordinal);
            Assert.Contains("P_AC(t)", klartext, StringComparison.Ordinal);
            Assert.Contains("(3)", klartext, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Gegenprobe zum Klartext:</b> Die Umwandlung nimmt die Auszeichnung, nicht den
        /// Inhalt. Geprüft an einem Ausschnitt, der jede Form enthält, die in den Seiten
        /// vorkommt — Überschrift, Liste, Tabelle, Verweis, Fettsatz, Formelzeile.
        /// </summary>
        [Fact]
        public void Der_Klartext_behaelt_Woerter_Zahlen_und_Formeln()
        {
            const string markup =
                "<!-- EPOS-Plan Hilferubrik Berechnung | Seite: Probe | Stand: 2026-09-06 | Rechenkern: EPOS.Kern/X.cs -->\n" +
                "== Rechenweg ==\n" +
                "Der '''Vorgabewert''' ist 0,95.\n" +
                "* Erster Punkt\n" +
                "{| class=\"wikitable\"\n" +
                "! Größe !! Einheit\n" +
                "|-\n" +
                "| Leistung || kW\n" +
                "|}\n" +
                " P_AC = min(P_DC, P_nenn)\n" +
                "Siehe [[Programm Dokumentation/Photovoltaik|Photovoltaik]].\n";

            string klartext = BerechnungsHilfe.AlsKlartext(markup);

            Assert.Contains("Rechenweg", klartext);
            Assert.Contains("Vorgabewert", klartext);
            Assert.Contains("0,95", klartext);
            Assert.Contains("Erster Punkt", klartext);
            Assert.Contains("Größe | Einheit", klartext);
            Assert.Contains("Leistung | kW", klartext);
            Assert.Contains("P_AC = min(P_DC, P_nenn)", klartext);
            Assert.Contains("Photovoltaik", klartext);

            Assert.DoesNotContain("<!--", klartext);
            Assert.DoesNotContain("'''", klartext);
            Assert.DoesNotContain("[[", klartext);
            Assert.DoesNotContain("{|", klartext);
        }

        /// <summary>
        /// <b>Gegenprobe zur Tag-Entfernung:</b> Ein Vergleichszeichen in einer Formelzeile
        /// ist kein HTML. Ein weit gefasstes Muster („alles zwischen &lt; und &gt;") machte
        /// aus „P &lt; 0 und Q &gt; 1" das sinnlose „P 1" — genau das darf nicht passieren.
        /// </summary>
        [Fact]
        public void Vergleichszeichen_in_einer_Formel_bleiben_stehen()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(" wenn P < 0 und Q > 1 dann Rest = 0\n");

            Assert.Contains("P < 0 und Q > 1", klartext);
        }

        // =====================================================================
        //  Kopfblockleser
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Kopfblockleser:</b> Er liest die drei Felder auch aus einem
        /// über zwei Zeilen umbrochenen Kommentar — so stehen sie in den Dateien.
        /// </summary>
        [Fact]
        public void Der_Kopfblockleser_versteht_den_umbrochenen_Kommentar()
        {
            const string markup =
                "<!-- EPOS-Plan Hilferubrik Berechnung | Seite: Wärmequelle Erdreich | Stand: 2026-09-06 | Rechenkern:\n" +
                "     EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs, EPOS.Kern/Allgemein/Simulation/VDI4640Pruefung.cs -->\n" +
                "== Was berechnet wird ==\n";

            BerechnungsHilfe.Kopfblock(markup, out string seite, out string stand, out string kern);

            Assert.Equal("Wärmequelle Erdreich", seite);
            Assert.Equal("2026-09-06", stand);
            Assert.Contains("ErdreichTemperatur.cs", kern);
            Assert.Contains("VDI4640Pruefung.cs", kern);
        }

        /// <summary>Ohne Kopfblock bleiben alle drei Felder leer — kein Wurf, kein Raten.</summary>
        [Fact]
        public void Ohne_Kopfblock_bleiben_die_Felder_leer()
        {
            BerechnungsHilfe.Kopfblock("== Rechenweg ==\n", out string seite, out string stand, out string kern);

            Assert.Equal("", seite);
            Assert.Equal("", stand);
            Assert.Equal("", kern);
        }

        // =====================================================================
        //  Nachschlagen
        // =====================================================================

        /// <summary>
        /// Eine Seite ist über ihren Namen UND über das Mapping-Ziel erreichbar —
        /// <c>help_mapping.txt</c> trägt „Berechnung/&lt;Seite&gt;".
        /// </summary>
        [Fact]
        public void Eine_Seite_ist_ueber_Namen_und_Ziel_erreichbar()
        {
            BerechnungsSeite erste = BerechnungsHilfe.Seiten[0];

            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Seitenname));
            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Seitenname.ToLowerInvariant()));
            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Ziel));
            Assert.Null(BerechnungsHilfe.Seite("gibt es nicht"));
            Assert.Null(BerechnungsHilfe.Seite(""));
            Assert.Null(BerechnungsHilfe.Seite(null));
        }

        /// <summary>Titel, Ziel und Wikititel folgen der Rubrik — sie sind die Adressen der Seite.</summary>
        [Fact]
        public void Titel_Ziel_und_Wikititel_folgen_der_Rubrik()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.Equal("Berechnung: " + seite.Seitenname, seite.Titel);
                Assert.Equal("Berechnung/" + seite.Seitenname, seite.Ziel);
                Assert.Equal("Programm Dokumentation/Berechnung/" + seite.Seitenname, seite.WikiTitel);
            }
        }

        // =====================================================================
        //  Anschluss an das Wissen des Assistenten
        // =====================================================================

        /// <summary>
        /// Der Assistent kann „Wie wird die Photovoltaik berechnet?" beantworten, weil je
        /// Seite ein Wissensabschnitt im eingebauten Wissen steht — auch ohne Netz und
        /// bevor die Seiten im Wiki angelegt sind.
        /// </summary>
        [Fact]
        public void Jede_Seite_steht_als_Wissensabschnitt_im_Assistenten()
        {
            List<WissensAbschnitt> abschnitte = HilfeWissen.Abschnitte;

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                WissensAbschnitt treffer = abschnitte.FirstOrDefault(
                    a => string.Equals(a.Titel, seite.Titel, StringComparison.Ordinal));

                Assert.True(treffer != null,
                    "Zur Seite '" + seite.Seitenname + "' fehlt der Wissensabschnitt '" + seite.Titel + "'.");
                Assert.Equal(BerechnungsHilfe.BEREICH, treffer.Bereich);
                Assert.Equal(seite.Klartext, treffer.Inhalt);
            }
        }

        /// <summary>
        /// Die Suche findet den Rechenweg zu einer Frage nach der Berechnung. Geprüft mit
        /// dem Seitennamen selbst — er zählt im Titel dreifach, der Bereich „Berechnung"
        /// doppelt. Gewichtung und Suche selbst sind von H13 unberührt.
        /// </summary>
        [Fact]
        public void Die_Suche_findet_den_Rechenweg()
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seiten[0];

            List<WissensAbschnitt> treffer =
                HilfeWissen.Suchen("Berechnung " + seite.Seitenname, "", 4);

            Assert.Contains(treffer, a => string.Equals(a.Titel, seite.Titel, StringComparison.Ordinal));
        }
    }
}
