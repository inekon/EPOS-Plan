using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über die Berichtsschreiber ohne Datenbank</b> (Konzept Berichtsvorlagen 5.1;
    /// <c>EPOS.Kern/CLAUDE.md</c>, Abschnitt „Bericht"). Die Schreiber lesen nur <see cref="BerichtsDaten"/>:
    /// Alles, was sie aus der Datenbank brauchen, sammelt <c>BerichtsDatenSammler.SammleFuerBericht</c> EINMAL
    /// in <see cref="BerichtsDaten.Wirtschaft"/> (<see cref="WirtschaftsBerichtswerte"/>) — dieselben
    /// Rechenwege, einmal gerufen; Word und Excel lesen denselben Satz.
    ///
    /// <para><b>Die Schreiber</b> (<see cref="Einzelschreiber"/>): der ganze Bausteinordner
    /// <c>Allgemein/Bericht/Bausteine/</c> — ein neuer Baustein fällt ohne Zutun darunter —, die
    /// Anhang-E-Checkliste, der Tabellenbericht samt Formelmappe und Verlaufsblatt und der Vorlagenfüller.</para>
    ///
    /// <para><b>Was die Wache meldet</b> (<see cref="Zugriffe"/>, je Muster ein Name für die Meldung), in drei
    /// Gruppen: die Zugriffsschicht (<c>DataRepository</c>, <c>StilleDb</c>, <c>RecordSet</c>,
    /// <c>IDatenzugriff</c>, <c>DbParam</c>, SQL-Ausführung, eine eigene SQLite-Verbindung); die Controller
    /// (angelegt, statisch gerufen, als Feld, Parameter oder Variable gehalten); und die Rechenwege mit
    /// Datenbank, die der Wertesatz einmal ruft — Ergebnisse, Parameter, Tarif, Strommatrix, Referenzkessel,
    /// Emissionsbilanz, Wirkungen, Erzeuger der Gruppe, Kapitalwertverlauf, KWKG-Lage, Trägerpreise und
    /// -namen, Emissionsquelle, Bewertung, Gesetzkatalog. Holt ein Schreiber einen davon wieder selbst, rechnet
    /// er an der Sammlung vorbei.</para>
    ///
    /// <para><b>Gelesen wird der Programmtext</b> (<see cref="Programmtext"/>): ohne Kommentare,
    /// Präprozessorzeilen und den Inhalt von Zeichenketten und Zeichen — aber MIT den Löchern einer
    /// interpolierten Zeichenkette und mit Folgezeilen, die mit <c>*</c> beginnen. Die Schreiber setzen Texte
    /// zusammen; ein Aufruf in <c>$"…{…}…"</c> ist ein Aufruf. Deshalb ein kleiner Leser statt der
    /// zeilenweisen Vereinfachung der übrigen Quelltextwachen.</para>
    ///
    /// <para><b>Die Positivliste</b> (<see cref="Ausnahmen"/>) nennt je Datei, Muster und Treffer die GENAUE
    /// Anzahl und den Grund: reine Funktionen eines Controllers, ein geschachtelter Typname und Überladungen für
    /// Aufrufer außerhalb des Berichtslaufs. Stimmt eine Anzahl nicht mehr, ist das ein Befund — so wächst
    /// keine Ausnahme still mit, und keine bleibt als Lücke stehen.</para>
    ///
    /// <para><b>Kein Fund ist <c>WirtschaftsBerichtswerte.Von(daten)</c></b>: Nach dem Sammeln gibt er den
    /// gesammelten Satz zurück. Nur ein Baum ohne Sammler (Proben, Prüfstände) bekommt den Rückfall, der jeden
    /// Teil beim ersten Lesen rechnet — in <c>WirtschaftsBerichtswerte.cs</c>, nicht im Schreiber.</para>
    ///
    /// <para><b>Die Laufzeithälfte steht in <see cref="BerichtWertesatzTests"/></b> und wird hier nicht
    /// gedoppelt: Nach dem Sammeln schreiben Word (bisheriger Weg und Vorlagenweg) und die Mappe für 1030, die
    /// Gruppe 1019 und die Proben der Messlatte unter einem Zugriff, der bei jedem Vorgang wirft, und
    /// <see cref="WirtschaftsBerichtswerte.Nachgeholt"/> bleibt leer. Diese Wache hält den Quelltext.</para>
    /// </summary>
    public sealed class BerichtSchreiberOhneDatenbankWacheTests
    {
        /// <summary>Der Berichtsordner des Kerns, repo-relativ.</summary>
        private const string BERICHTSORDNER = "EPOS.Kern/Allgemein/Bericht";

        /// <summary>Der Bausteinordner, ab dem Berichtsordner — er wird ganz gelesen.</summary>
        private const string BAUSTEINORDNER = "Bausteine";

        /// <summary>Die Schreiber außerhalb des Bausteinordners, ab dem Berichtsordner.</summary>
        private static readonly string[] Einzelschreiber =
        {
            "AnhangECheckliste.cs",
            "ExcelBerichtGenerator.cs",
            "ExcelFormelmappe.cs",
            "VerlaufExcel.cs",
            "Vorlagen/WordVorlagenfueller.cs",
        };

        // Die Namen der Muster, auf die sich die Positivliste bezieht.
        private const string CTRL_GLIED = "…Ctrl.Glied";
        private const string EMISSIONSQUELLE = "Emissionsquelle.";
        private const string TRAEGERPREISSATZ_LIES = "Traegerpreissatz.Lies(";

        /// <summary>Die Zugriffe, die ein Schreiber nicht tut — je Muster ein Name für die Meldung.</summary>
        private static readonly (string Name, Regex Muster)[] Zugriffe =
        {
            // Die Zugriffsschicht.
            ("new DataRepository",                  R(@"\bnew\s+DataRepository\b")),
            ("DataRepository.",                     R(@"\bDataRepository\s*\.\s*\w+")),
            ("StilleDb.",                           R(@"\bStilleDb\s*\.\s*\w+")),
            ("RecordSet",                           R(@"\bRecordSet\b")),
            ("IDatenzugriff",                       R(@"\bIDatenzugriff\b")),
            ("Datenzugriff.",                       R(@"Datenzugriff\s*\.\s*\w+")),      // auch SqliteDatenzugriff.
            ("DbParam",                             R(@"\bDbParam\w*")),
            ("SQL-Ausführung",                      R(@"\b(?:ExecuteScalar|ExecuteNonQuery|ExecuteReader|GetDataTable)\s*\(")),
            ("SQLite direkt",                       R(@"\bSqlite(?:Connection|Command|DataReader|Transaction|Parameter)\b|\bMicrosoft\s*\.\s*Data\s*\.\s*Sqlite\b")),

            // Die Controller.
            ("new …Ctrl(",                          R(@"\bnew\s+[\w.]*Ctrl\s*[({]")),
            (CTRL_GLIED,                            R(@"\b\w+Ctrl\s*\.\s*\w+")),
            ("…Ctrl als Feld, Parameter, Variable", R(@"\b\w+Ctrl\s+@?\w+\s*[=;,)]")),

            // Die Rechenwege mit Datenbank, die der Wertesatz einmal ruft.
            ("LadeErgebnisse(",                     R(@"\bLadeErgebnisse\s*\(")),
            ("LadeParameter(",                      R(@"\bLadeParameter\s*\(")),
            ("LadeTarif(",                          R(@"\bLadeTarif\s*\(")),
            ("LadeStromMatrix(",                    R(@"\bLadeStromMatrix\s*\(")),
            ("LiesReferenzkessel(",                 R(@"\bLiesReferenzkessel\s*\(")),
            ("EmissionsBilanzRechner.Berechne(, Lade…(", R(@"\bEmissionsBilanzRechner\s*\.\s*(?:Berechne|Lade\w*|StelleKatalogSicher)\s*\(")),
            ("ProjektWirkungCtrl.Laden(",           R(@"\bProjektWirkungCtrl\s*(?:\(\s*\)\s*)?\.\s*Laden\s*\(")),
            ("ErzeugerDerGruppe(",                  R(@"\bErzeugerDerGruppe\s*\(")),
            ("BerechneVerlaufSzenarien…(",          R(@"\bBerechneVerlaufSzenarien\w*\s*\(")),
            ("KwkgAktivierung.IstAktiv(",           R(@"\bKwkgAktivierung\s*\.\s*IstAktiv\s*\(")),
            (EMISSIONSQUELLE,                       R(@"\bEmissionsquelle\s*\.\s*\w+")),
            ("KostenEmissionRechner.",              R(@"\bKostenEmissionRechner\s*\.\s*\w+")),
            (TRAEGERPREISSATZ_LIES,                 R(@"\bTraegerpreissatz\s*\.\s*Lies\s*\(")),
            ("TraegerpreisSzenario.Nachweiszeile(", R(@"\bTraegerpreisSzenario\s*\.\s*Nachweiszeile\s*\(")),
            ("WirtschaftlichkeitBewertung.FuerBericht(", R(@"\bWirtschaftlichkeitBewertung\s*\.\s*FuerBericht\s*\(")),
            ("new GesetzKatalog(",                  R(@"\bnew\s+GesetzKatalog\s*\(")),
        };

        /// <summary>
        /// <b>Die Positivliste.</b> Jede Ausnahme gilt für GENAU eine Datei, ein Muster und einen Treffer, und
        /// zwar genau so oft, wie hier steht — der Grund steht daneben.
        /// </summary>
        private static readonly Ausnahme[] Ausnahmen =
        {
            new Ausnahme("ExcelFormelmappe.cs", CTRL_GLIED, "BetriebskostenCtrl.Bemessungsfaktor", 1,
                "Reine Funktion des Betriebskosten-Rechenwegs (kein Datenbankzugriff, kein Zustand): Prozent- oder " +
                "Satzbemessung der Formelzeile — derselbe Rechenweg statt einer zweiten Liste der Bemessungsarten."),
            new Ausnahme("ExcelFormelmappe.cs", CTRL_GLIED, "BetriebskostenCtrl.MengenEinheit", 1,
                "Einheitenzeichen der Bezugsmenge aus dem BemessungKatalog, einer Tafel im Code — keine Datenbank."),
            new Ausnahme("ExcelFormelmappe.cs", CTRL_GLIED, "BetriebskostenCtrl.SatzEinheit", 1,
                "Einheitenzeichen des Satzes aus dem BemessungKatalog, einer Tafel im Code — keine Datenbank."),
            new Ausnahme("ExcelFormelmappe.cs", CTRL_GLIED, "BetriebskostenCtrl.Betrag", 1,
                "Der eine Rechenweg je Bemessungsart als reine Funktion: die Gegenrechnung der Formelzelle."),
            new Ausnahme("Bausteine/BausteineWirtschaftlichkeit.cs", CTRL_GLIED, "WirtschaftlichkeitCtrl.ErzeugerFlags", 1,
                "Ein Typname: ErzeugerFlags ist in WirtschaftlichkeitCtrl geschachtelt. Der Wert kommt aus dem " +
                "Wertesatz (werte.Erzeuger); gerufen wird nichts."),
            new Ausnahme("Bausteine/BausteineProjekt.cs", EMISSIONSQUELLE, "Emissionsquelle.TraegerName", 2,
                "Die Überladung KuehltraegerText(m) ohne Namensquelle und der Rückfall der Überladung mit Quelle, " +
                "wenn keine kommt — für Aufrufer außerhalb des Berichtslaufs (KaeltestromAbrechnungTests). Der " +
                "Bericht übergibt die Namen des Wertesatzes (WirtschaftsBerichtswerte.Traegername)."),
            new Ausnahme("ExcelFormelmappe.cs", TRAEGERPREISSATZ_LIES, "Traegerpreissatz.Lies", 1,
                "Die Überladungen des Parameterblocks ohne Trägerpreisquelle — für Aufrufer außerhalb des " +
                "Berichtslaufs (Formelmappentests). Der Tabellenbericht übergibt die Trägerpreise des Wertesatzes " +
                "(WirtschaftsBerichtswerte.Traegerpreise)."),
        };

        // =====================================================================
        //  Die Wache
        // =====================================================================

        /// <summary>
        /// Kein Berichtsschreiber greift auf die Datenbank oder einen Controller zu — außer den Stellen der
        /// Positivliste, jede genau so oft, wie sie dort steht.
        /// </summary>
        [Fact]
        public void Kein_Berichtsschreiber_greift_auf_Datenbank_oder_Controller_zu()
        {
            var funde = new List<Fund>();
            foreach ((string name, string pfad) in Schreiberdateien())
                funde.AddRange(Funde(name, File.ReadAllText(pfad)));

            List<string> befunde = Befunde(funde);
            foreach (Ausnahme a in Ausnahmen)
                if (!funde.Any(f => Gilt(a, f)))
                    befunde.Add("Ausnahme ohne Fund - aus der Positivliste streichen: " + a.Datei + "  (" + a.Muster +
                                ")  " + a.Treffer);

            Assert.True(befunde.Count == 0,
                "Ein Berichtsschreiber greift auf die Datenbank oder einen Controller zu. Die Schreiber lesen nur " +
                "BerichtsDaten: Was sie aus der Datenbank brauchen, ermittelt BerichtsDatenSammler.SammleFuerBericht " +
                "einmal als Teil von WirtschaftsBerichtswerte (BerichtsDaten.Wirtschaft). Eine bewusste Ausnahme " +
                "steht mit Anzahl und Grund in der Positivliste dieser Wache:\n" + string.Join("\n", befunde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Die Wache erkennt einen eingebauten Verstoß</b> — jedes Muster in der Schreibweise des Bestands, auf
        /// der Zeile, in die er eingebaut wurde, auch im Loch einer interpolierten Zeichenkette, hinter einem
        /// Zeichen <c>'"'</c> und auf einer Folgezeile mit <c>*</c>. Was nicht gemeint ist — Kommentar,
        /// Zeichenkette, Rohzeichenkette, Präprozessorzeile, der Wertesatz —, schlägt nicht an. Und die
        /// Positivliste ist eng: dieselbe Datei, dasselbe Glied, dieselbe Anzahl.
        /// </summary>
        [Fact]
        public void Die_Wache_erkennt_einen_eingebauten_Verstoss()
        {
            const string PROBE = "Probe.cs";
            int marke = Array.IndexOf(Probeschreiber, MARKE) + 1;
            Assert.True(marke > 0);

            // 1. Der saubere Probeschreiber ist fundfrei.
            List<Fund> sauber = Funde(PROBE, Probe(MARKE));
            Assert.True(sauber.Count == 0, "Der saubere Probeschreiber meldet:\n" + string.Join("\n", sauber));

            // 2. Jeder eingebaute Verstoß fällt auf, mit seinem Muster und auf seiner Zeile.
            var fehlt = new List<string>();
            foreach ((string muster, string einbau) in Verstoesse)
            {
                int bis = marke + einbau.Count(c => c == '\n');
                List<Fund> funde = Funde(PROBE, Probe(einbau));
                if (!funde.Any(f => f.Muster == muster && f.Zeile >= marke && f.Zeile <= bis))
                    fehlt.Add(muster + "  <-  " + einbau.Trim() + "  (gefunden: " +
                              string.Join("; ", funde.Select(f => f.Muster + " Z. " + f.Zeile)) + ")");
                if (funde.Any(f => f.Zeile < marke || f.Zeile > bis))
                    fehlt.Add(muster + "  <-  " + einbau.Trim() + "  (Fund außerhalb der Einbauzeilen)");
            }
            Assert.True(fehlt.Count == 0, "Diese Verstöße sieht die Wache nicht:\n" + string.Join("\n", fehlt));

            // Jedes Muster hat mindestens einen Verstoß in der Probe.
            string[] ungeprueft = Zugriffe.Select(z => z.Name)
                .Where(n => !Verstoesse.Any(v => v.Muster == n)).ToArray();
            Assert.True(ungeprueft.Length == 0, "Muster ohne Probe: " + string.Join(", ", ungeprueft));

            // 3. Was nicht gemeint ist, schlägt nicht an.
            var falsch = new List<string>();
            foreach (string einbau in KeineVerstoesse)
            {
                List<Fund> funde = Funde(PROBE, Probe(einbau));
                if (funde.Count > 0) falsch.Add(einbau.Trim() + "  ->  " + string.Join("; ", funde));
            }
            Assert.True(falsch.Count == 0, "Diese Stellen meldet die Wache zu Unrecht:\n" + string.Join("\n", falsch));

            // 4. Die Positivliste ist eng.
            const string BETRAG =
                "            double nach = BetriebskostenCtrl.Betrag(n.Bemessung, 0.0, n.Menge, n.Einheitpreis, n.IstErloes);";
            Assert.Empty(Befunde(Funde("ExcelFormelmappe.cs", Probe(BETRAG))));
            Assert.NotEmpty(Befunde(Funde("ExcelFormelmappe.cs", Probe(BETRAG + "\n" + BETRAG))));
            Assert.NotEmpty(Befunde(Funde("AnhangECheckliste.cs", Probe(BETRAG))));
            Assert.NotEmpty(Befunde(Funde("ExcelFormelmappe.cs",
                Probe("            double? summe = BetriebskostenCtrl.InvestSummeFuer(daten.IdStamm, 0);"))));
        }

        /// <summary>
        /// <b>Die Wache sieht, was sie bewacht:</b> alle benannten Schreiber und den Bausteinordner; jeder
        /// <see cref="IBerichtsBaustein"/> der Assembly ist in einer bewachten Datei deklariert (sonst fiele ein
        /// neuer Baustein außerhalb des Ordners unbemerkt heraus); die Schreiber des Wertesatzes lesen ihn
        /// wirklich über <c>WirtschaftsBerichtswerte.Von</c> — auch ein Beleg, dass der Leser ihren Programmtext
        /// stehen lässt —; und jede Ausnahme zeigt auf einen bewachten Schreiber und ein Muster der Wache.
        /// </summary>
        [Fact]
        public void Die_Wache_sieht_alle_Schreiber_und_jeden_Baustein()
        {
            List<(string Name, string Pfad)> dateien = Schreiberdateien();
            string[] namen = dateien.Select(d => d.Name).ToArray();

            foreach (string datei in Einzelschreiber)
                Assert.True(namen.Contains(datei), "Schreiber nicht gefunden: " + BERICHTSORDNER + "/" + datei);
            Assert.True(namen.Count(n => n.StartsWith(BAUSTEINORDNER + "/", StringComparison.Ordinal)) >= 4,
                        "Der Bausteinordner führt kaum Dateien: " + string.Join(", ", namen));

            var deklariert = new HashSet<string>(StringComparer.Ordinal);
            var klasse = new Regex(@"\bclass\s+(\w+)");
            foreach ((string Name, string Pfad) datei in dateien)
                foreach (Match m in klasse.Matches(Programmtext(File.ReadAllText(datei.Pfad))))
                    deklariert.Add(m.Groups[1].Value);
            string[] bausteine = typeof(IBerichtsBaustein).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IBerichtsBaustein).IsAssignableFrom(t))
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            Assert.Contains("WirtschaftlichkeitBaustein", bausteine);
            string[] draussen = bausteine.Where(n => !deklariert.Contains(n)).ToArray();
            Assert.True(draussen.Length == 0,
                "Diese Bausteine liegen außerhalb der bewachten Schreiber - in den Bausteinordner legen oder die " +
                "Datei in Einzelschreiber aufnehmen: " + string.Join(", ", draussen));

            foreach (string datei in new[] { BAUSTEINORDNER + "/BausteineWirtschaftlichkeit.cs",
                                             "AnhangECheckliste.cs", "ExcelBerichtGenerator.cs" })
            {
                string text = Programmtext(File.ReadAllText(dateien.Single(d => d.Name == datei).Pfad));
                Assert.True(Regex.IsMatch(text, @"\bWirtschaftsBerichtswerte\s*\.\s*Von\s*\("),
                            datei + " liest den Wertesatz nicht (mehr) über WirtschaftsBerichtswerte.Von.");
            }

            foreach (Ausnahme a in Ausnahmen)
            {
                Assert.True(namen.Contains(a.Datei), "Ausnahme für eine unbewachte Datei: " + a.Datei);
                Assert.True(Zugriffe.Any(z => z.Name == a.Muster), "Ausnahme für ein unbekanntes Muster: " + a.Muster);
                Assert.True(a.Anzahl > 0 && !string.IsNullOrWhiteSpace(a.Grund), "Ausnahme ohne Anzahl oder Grund: " + a.Treffer);
            }
        }

        // =====================================================================
        //  Die Probe
        // =====================================================================

        /// <summary>Die Zeile des Probeschreibers, an deren Stelle der Verstoß eingebaut wird.</summary>
        private const string MARKE = "            // <Einbau>";

        /// <summary>
        /// Ein sauberer Probeschreiber in der Schreibweise des Bestands: Er liest den Wertesatz und nennt die
        /// verbotenen Zugriffe nur in Kommentaren, Zeichenketten, Zeichen und einer Präprozessorzeile. Ein
        /// Blockkommentar und eine wörtliche Zeichenkette über zwei Zeilen stehen vor der <see cref="MARKE"/> —
        /// so prüft jeder Einbau auch, dass die Zeilen stimmen.
        /// </summary>
        private static readonly string[] Probeschreiber =
        {
            "using System;",
            "using WindowsFormsApplication1;",
            "",
            "namespace WindowsFormsApplication1",
            "{",
            "    /* Zwei Zeilen Blockkommentar: new WirtschaftlichkeitCtrl().LadeParameter(id);",
            "       DataRepository.GetDataTable(sql) und StilleDb.Scalar(sql) sind hier kein Fund. */",
            "    internal static class Probeschreiber",
            "    {",
            "        /// <summary>Nennt <see cref=\"Emissionsquelle.TraegerName\"/> nur im Kommentar.</summary>",
            "        internal static string Schreibe(BerichtsDaten daten)",
            "        {",
            "            WirtschaftsBerichtswerte w = WirtschaftsBerichtswerte.Von(daten);   // new ProjektWirkungCtrl().Laden(id)",
            "            string sql = @\"SELECT [name] FROM energy_carrier",
            "                           -- DataRepository.GetDataTable(\"\"sql\"\")\";",
            "            char anfuehrung = '\"';",
            "            string text = $\"{w.Traegername(3)}: {{Emissionsquelle.TraegerName}} {anfuehrung}\";",
            "#if DEBUG // KwkgAktivierung.IstAktiv(id, ids)",
            "#endif",
            MARKE,
            "            return text + sql + w.Parameternachweis(null) + (w.Erzeuger == null ? \"\" : \"Erzeuger\");",
            "        }",
            "    }",
            "}",
        };

        /// <summary>Der Probeschreiber mit <paramref name="einbau"/> an der Stelle der Marke.</summary>
        private static string Probe(string einbau)
            => string.Join("\n", Probeschreiber.Select(z => z == MARKE ? einbau : z));

        /// <summary>Je Muster mindestens ein Verstoß — und die Schreibweisen, die der Leser nicht verschlucken darf.</summary>
        private static readonly (string Muster, string Einbau)[] Verstoesse =
        {
            ("new DataRepository",                  "            var r = new DataRepository();"),
            ("DataRepository.",                     "            DataTable t = DataRepository.GetDataTable(sql);"),
            ("StilleDb.",                           "            object o = StilleDb.ScalarStreng(sql);"),
            ("RecordSet",                           "            using (var rs = new RecordSet(sql)) { }"),
            ("IDatenzugriff",                       "            IDatenzugriff zugriff = null;"),
            ("Datenzugriff.",                       "            var v = SqliteDatenzugriff.OeffneVerbindung();"),
            ("DbParam",                             "            var p = new DbParam(\"@p\", 1);"),
            ("SQL-Ausführung",                      "            object o = verbindung.ExecuteScalar(sql);"),
            ("SQLite direkt",                       "            using var v = new SqliteConnection(pfad);"),
            ("new …Ctrl(",                          "            var provider = new WirtschaftlichkeitCtrl();"),
            (CTRL_GLIED,                            "            var satz = EnergietraegerPreisCtrl.SzenarioJeTraeger(stand.IdProjekt);"),
            ("…Ctrl als Feld, Parameter, Variable", "            WirtschaftlichkeitCtrl provider = Quelle();"),
            ("LadeErgebnisse(",                     "            var alle = provider.LadeErgebnisse(ids);"),
            ("LadeParameter(",                      "            var p = provider.LadeParameter(daten.IdStamm);"),
            ("LadeTarif(",                          "            var t = provider.LadeTarif(daten.IdStamm);"),
            ("LadeStromMatrix(",                    "            var m = provider.LadeStromMatrix(ids);"),
            ("LiesReferenzkessel(",                 "            var k = provider.LiesReferenzkessel(daten.IdStamm);"),
            ("EmissionsBilanzRechner.Berechne(, Lade…(", "            var b = EmissionsBilanzRechner.Berechne(stand.IdProjekt, p);"),
            ("ProjektWirkungCtrl.Laden(",           "            var l = new ProjektWirkungCtrl().Laden(daten.IdStamm);"),
            ("ErzeugerDerGruppe(",                  "            var f = provider.ErzeugerDerGruppe(daten.IdStamm);"),
            ("BerechneVerlaufSzenarien…(",          "            var v = provider.BerechneVerlaufSzenarienJeZeitraum(daten, p);"),
            ("KwkgAktivierung.IstAktiv(",           "            bool kwkg = KwkgAktivierung.IstAktiv(daten.IdStamm, ids);"),
            (EMISSIONSQUELLE,                       "            string name = Emissionsquelle.TraegerName(id);"),
            ("KostenEmissionRechner.",              "            KostenEmissionRechner.PreisSatz(id, traeger, sz, out a, out g, out l);"),
            (TRAEGERPREISSATZ_LIES,                 "            var saetze = Traegerpreissatz.Lies(stand.IdProjekt);"),
            ("TraegerpreisSzenario.Nachweiszeile(", "            string z = TraegerpreisSzenario.Nachweiszeile(daten.Varianten, sz, kultur);"),
            ("WirtschaftlichkeitBewertung.FuerBericht(", "            var b = WirtschaftlichkeitBewertung.FuerBericht(daten, alle, p, kultur);"),
            ("new GesetzKatalog(",                  "            var k = BilanzKonvention.Bestimme(p, new GesetzKatalog());"),

            // Der Leser: das Loch einer interpolierten Zeichenkette, auch wörtlich und mit Text im Loch; ein
            // Zeichen '"' und ein maskiertes Anführungszeichen davor; eine Folgezeile, die mit * beginnt.
            (EMISSIONSQUELLE,                       "            string s = $\"Träger {Emissionsquelle.TraegerName(id)}\";"),
            ("DataRepository.",                     "            string s = $@\"{DataRepository.GetDataTable(sql).Rows.Count} Zeilen\";"),
            (CTRL_GLIED,                            "            string s = $\"{(w == null ? \"-\" : EnergietraegerPreisCtrl.SzenarioJeTraeger(id).Count.ToString())}\";"),
            ("StilleDb.",                           "            char q = '\"'; object o = StilleDb.Scalar(sql);"),
            ("DbParam",                             "            string s = \"a\\\"b\"; var p = new DbParam(\"@p\", 1);"),
            ("KostenEmissionRechner.",              "            double x = 2.0\n                * KostenEmissionRechner.AnschlussleistungKW(id, traeger);"),
        };

        /// <summary>Stellen, die keinen Zugriff sind: der Wertesatz, reine Helfer, Kommentar, Text, Präprozessor.</summary>
        private static readonly string[] KeineVerstoesse =
        {
            "            var e = w.Ergebnisse; bool a = w.ErgebnisAktuell(e[0]); var f = w.Erzeuger;",
            "            var t = WirtschaftsBerichtswerte.Von(daten).Traegerpreise(null); string n = w.Traegername(3);",
            "            double? d = ProjektDetails.D(null, \"Nutzflaeche\"); var k = KennzahlenKatalog.Alle();",
            "            string s = \"DataRepository.GetDataTable(sql) und new WirtschaftlichkeitCtrl()\";",
            "            string s = $\"{{DataRepository.GetDataTable}} {w.Traegername(1)}\";",
            "            string s = @\"StilleDb.Scalar(\"\"sql\"\") \\\";",
            "            /* StilleDb.Scalar(sql) */ int n = 0; // new WirtschaftlichkeitCtrl()",
            "            string s = \"\"\"\n                DataRepository.GetDataTable(sql)\n                \"\"\";",
            "#region Emissionsquelle.TraegerName(id)",
            "            // Emissionsquelle.TraegerName(id)",
        };

        // =====================================================================
        //  Der Leser
        // =====================================================================

        /// <summary>Ein Fund: die Datei (ab dem Berichtsordner), die Zeile, das Muster, der Treffer und die Zeile im Wortlaut.</summary>
        private sealed record Fund(string Datei, int Zeile, string Muster, string Treffer, string Text)
        {
            public override string ToString() => Datei + ":" + Zeile + "  (" + Muster + ")  " + Text;
        }

        /// <summary>Eine bewusste Ausnahme: Datei (ab dem Berichtsordner), Muster, Treffer, genaue Anzahl und Grund.</summary>
        private sealed record Ausnahme(string Datei, string Muster, string Treffer, int Anzahl, string Grund);

        /// <summary>Die Funde eines Quelltexts — die Kernschleife ohne Datei, gegenprobenfähig.</summary>
        private static List<Fund> Funde(string datei, string quelltext)
        {
            string[] roh = quelltext.Replace("\r\n", "\n").Split('\n');
            string[] code = Programmtext(quelltext).Split('\n');
            var funde = new List<Fund>();
            for (int i = 0; i < code.Length; i++)
                foreach ((string name, Regex muster) in Zugriffe)
                    foreach (Match m in muster.Matches(code[i]))
                        funde.Add(new Fund(datei, i + 1, name, Treffer(m.Value), roh[i].Trim()));
            return funde;
        }

        /// <summary>
        /// Die Befunde zu den Funden: jeder Fund ohne Ausnahme, und jede Ausnahme, deren Anzahl nicht stimmt.
        /// Eine Ausnahme ohne jeden Fund prüft erst die Wache über den ganzen Bestand.
        /// </summary>
        private static List<string> Befunde(IEnumerable<Fund> funde)
        {
            var befunde = new List<string>();
            foreach (IGrouping<(string, string, string), Fund> gruppe in funde.GroupBy(f => (f.Datei, f.Muster, f.Treffer)))
            {
                Ausnahme a = Ausnahmen.FirstOrDefault(x => Gilt(x, gruppe.First()));
                if (a == null)
                {
                    befunde.AddRange(gruppe.Select(f => f.ToString()));
                    continue;
                }
                int anzahl = gruppe.Count();
                if (anzahl != a.Anzahl)
                    befunde.Add("Die Ausnahme " + a.Treffer + " in " + a.Datei + " gilt für " + a.Anzahl +
                                " Stelle(n), gefunden: " + anzahl + "\n  " + string.Join("\n  ", gruppe.Select(f => f.ToString())));
            }
            return befunde;
        }

        private static bool Gilt(Ausnahme a, Fund f)
            => a.Datei == f.Datei && a.Muster == f.Muster && a.Treffer == f.Treffer;

        /// <summary>Der Treffer ohne Leerraum um Punkt und Klammern, ohne die öffnende Klammer am Ende.</summary>
        private static string Treffer(string wert)
        {
            string t = Regex.Replace(wert.Trim(), @"\s+", " ");
            t = Regex.Replace(t, @"\s*([.(){}=;,])\s*", "$1");
            return t.TrimEnd('(', '{', '=', ';', ',', ')');
        }

        /// <summary>
        /// Der Programmtext eines Quelltexts: Kommentare, Präprozessorzeilen und der Inhalt von Zeichenketten und
        /// Zeichen werden Leerzeichen, jeder Zeilenumbruch bleibt — so zeigt ein Fund auf seine Zeile. Die Löcher
        /// einer interpolierten Zeichenkette bleiben Programmtext; eine Rohzeichenkette (<c>"""</c>) zählt ganz
        /// als Text.
        /// </summary>
        private static string Programmtext(string quelltext)
        {
            char[] z = (quelltext ?? "").Replace("\r\n", "\n").ToCharArray();
            int i = 0;
            Programm(z, ref i, false);
            return new string(z);
        }

        /// <summary>
        /// Liest Programmtext ab <paramref name="i"/>. Im Loch einer interpolierten Zeichenkette endet er an
        /// dessen schließender Klammer (Tiefe 0) und lässt <paramref name="i"/> auf ihr stehen.
        /// </summary>
        private static void Programm(char[] z, ref int i, bool imLoch)
        {
            int tiefe = 0;
            bool zeilenanfang = !imLoch;
            while (i < z.Length)
            {
                char c = z[i];
                char n = i + 1 < z.Length ? z[i + 1] : '\0';
                if (c == '\n') { zeilenanfang = true; i++; continue; }
                if (zeilenanfang && c == '#' && !imLoch) { BisZeilenende(z, ref i); continue; }
                if (!char.IsWhiteSpace(c)) zeilenanfang = false;

                if (c == '/' && n == '/') { BisZeilenende(z, ref i); continue; }
                if (c == '/' && n == '*') { Blockkommentar(z, ref i); continue; }
                if (c == '\'') { Zeichen(z, ref i); continue; }
                if (c == '"' || ((c == '$' || c == '@') && BeginntZeichenkette(z, i))) { Zeichenkette(z, ref i); continue; }
                if (imLoch)
                {
                    if (c == '{') tiefe++;
                    else if (c == '}') { if (tiefe == 0) return; tiefe--; }
                }
                i++;
            }
        }

        private static bool BeginntZeichenkette(char[] z, int i)
        {
            int j = i;
            while (j < z.Length && (z[j] == '$' || z[j] == '@')) j++;
            return j < z.Length && z[j] == '"';
        }

        /// <summary>Eine Zeichenkette ab ihrem Vorsatz (<c>$</c>, <c>@</c>) — regulär, wörtlich, interpoliert oder roh.</summary>
        private static void Zeichenkette(char[] z, ref int i)
        {
            bool interpoliert = false, woertlich = false;
            while (z[i] == '$' || z[i] == '@')
            {
                if (z[i] == '$') interpoliert = true; else woertlich = true;
                Leeren(z, i++);
            }

            int anfuehrung = 0;
            while (i + anfuehrung < z.Length && z[i + anfuehrung] == '"') anfuehrung++;
            if (!woertlich && anfuehrung >= 3)
            {
                // Rohzeichenkette: ganz als Text bis zur nächsten Folge von ebenso vielen Anführungszeichen.
                for (int k = 0; k < anfuehrung; k++) Leeren(z, i++);
                while (i < z.Length)
                {
                    int folge = 0;
                    while (i + folge < z.Length && z[i + folge] == '"') folge++;
                    if (folge >= anfuehrung)
                    {
                        for (int k = 0; k < folge; k++) Leeren(z, i++);
                        return;
                    }
                    Leeren(z, i++);
                }
                return;
            }

            Leeren(z, i++);                                             // das öffnende Anführungszeichen
            while (i < z.Length)
            {
                char c = z[i];
                char n = i + 1 < z.Length ? z[i + 1] : '\0';
                if (woertlich)
                {
                    if (c == '"')
                    {
                        if (n == '"') { Leeren(z, i++); Leeren(z, i++); continue; }
                        Leeren(z, i++);
                        return;
                    }
                }
                else
                {
                    if (c == '\\') { Leeren(z, i++); if (i < z.Length) Leeren(z, i++); continue; }
                    if (c == '"') { Leeren(z, i++); return; }
                    if (c == '\n') return;                              // nicht geschlossen: die Zeile endet
                }
                if (interpoliert && c == '{')
                {
                    if (n == '{') { Leeren(z, i++); Leeren(z, i++); continue; }
                    Leeren(z, i++);
                    Programm(z, ref i, true);                           // das Loch ist Programmtext
                    if (i < z.Length && z[i] == '}') Leeren(z, i++);
                    continue;
                }
                if (interpoliert && c == '}' && n == '}') { Leeren(z, i++); Leeren(z, i++); continue; }
                Leeren(z, i++);
            }
        }

        /// <summary>Ein Zeichen <c>'x'</c>, <c>'\''</c>, <c>'"'</c> oder <c>'"'</c>.</summary>
        private static void Zeichen(char[] z, ref int i)
        {
            Leeren(z, i++);                                             // das öffnende Hochkomma
            if (i < z.Length && z[i] == '\\')
            {
                Leeren(z, i++);
                if (i < z.Length && z[i] != '\n') Leeren(z, i++);
            }
            else if (i < z.Length && z[i] != '\n')
                Leeren(z, i++);
            while (i < z.Length && z[i] != '\'' && z[i] != '\n') Leeren(z, i++);
            if (i < z.Length && z[i] == '\'') Leeren(z, i++);
        }

        private static void Blockkommentar(char[] z, ref int i)
        {
            Leeren(z, i++);
            Leeren(z, i++);
            while (i < z.Length && !(z[i] == '*' && i + 1 < z.Length && z[i + 1] == '/')) Leeren(z, i++);
            if (i < z.Length) { Leeren(z, i++); Leeren(z, i++); }
        }

        private static void BisZeilenende(char[] z, ref int i)
        {
            while (i < z.Length && z[i] != '\n') Leeren(z, i++);
        }

        /// <summary>Ein Zeichen wird Leerzeichen — ein Zeilenumbruch bleibt.</summary>
        private static void Leeren(char[] z, int i)
        {
            if (z[i] != '\n') z[i] = ' ';
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static Regex R(string muster) => new Regex(muster, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Die Schreiberdateien: der Bausteinordner ganz und die <see cref="Einzelschreiber"/>, ohne Bauordner —
        /// je Datei der Name ab dem Berichtsordner (mit '/') und der volle Pfad.
        /// </summary>
        private static List<(string Name, string Pfad)> Schreiberdateien()
        {
            string ordner = Path.Combine(Arbeitsbaum(), BERICHTSORDNER.Replace('/', Path.DirectorySeparatorChar));
            string bausteine = Path.Combine(ordner, BAUSTEINORDNER);
            Assert.True(Directory.Exists(bausteine), "Ordner nicht gefunden: " + bausteine);

            char t = Path.DirectorySeparatorChar;
            IEnumerable<string> pfade = Directory.GetFiles(bausteine, "*.cs", SearchOption.AllDirectories)
                .Where(p => p.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                         && p.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0)
                .Concat(Einzelschreiber
                    .Select(d => Path.Combine(ordner, d.Replace('/', Path.DirectorySeparatorChar)))
                    .Where(File.Exists));

            return pfade
                .Select(p => (Name: p.Substring(ordner.Length).TrimStart(t).Replace(t, '/'), Pfad: p))
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>ParallelitaetWacheTests</c>.</summary>
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
