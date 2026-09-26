using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über den Platzhalterkatalog</b> (Konzept Berichtsvorlagen 5.5, 5.6 und 12
    /// „Katalog“; Etappe BV-E1).
    ///
    /// <para><b>Was sie hält.</b> Jeder Schlüssel und Alias folgt dem Schlüsselmuster und ist
    /// eindeutig; Aliasse führen auf ihren Eintrag; jede handgepflegte Beschreibung, jedes Muster
    /// und jeder Text, den die Auflösung in den Bericht schreibt, steht in BEIDEN <c>.resx</c>;
    /// jede Kennzahl des <see cref="KennzahlenKatalog"/> hat ihre drei Einträge; die Abbildungen
    /// auf Ressourcen- (<c>VF_…</c>) und Excel-Namen (<c>EPOS.…</c>) sind kollisionsfrei, und kein
    /// Excel-Name fällt auf einen Namen der Formelmappe.</para>
    ///
    /// <para><b>Die eingefrorene Schlüsselliste</b> liegt je Katalogfassung unter
    /// <c>EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v&lt;n&gt;.txt</c> (UTF-8 ohne BOM, CRLF):
    /// je Zeile ein ausgelieferter Schlüssel, ordinal sortiert, ein Alias als
    /// <c>alias -&gt; ziel</c>. Die Liste JEDER Fassung muss dem Katalog gleichen (Schlüssel mit
    /// <c>Seit</c> ≤ Fassung); aus jeder Liste muss jeder Schlüssel noch lebendig oder Alias sein (5.6).
    /// Die Deckungswache hält, dass die Standardvorlage jeden Schlüssel mit Ausgabe Word zeigt
    /// (Konzept 12). <b>Neu einfrieren</b>
    /// heißt: Bei einer Abweichung schreibt der Fall die aktuelle Liste in den Testausgabeordner
    /// (<c>bin/&lt;Konfiguration&gt;/net10.0/Messlatten/</c>); ist sie gewollt — eine neue Fassung
    /// mit höherer <see cref="Vorlagenfeldkatalog.KATALOGFASSUNG"/> —, wird sie von dort nach
    /// <c>EPOS.Kern.Tests/Messlatten/</c> kopiert. Eine ausgelieferte Liste wird nie geändert.</para>
    /// </summary>
    public class VorlagenfeldkatalogWacheTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public VorlagenfeldkatalogWacheTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        private static readonly Regex Schluesselmuster = new Regex(Platzhaltersyntax.SCHLUESSELMUSTER);

        /// <summary>Alle Schlüssel und Aliasse des Katalogs.</summary>
        private static List<string> SchluesselUndAliasse()
        {
            return Vorlagenfeldkatalog.Alle.SelectMany(f => new[] { f.Schluessel }.Concat(f.Aliasse)).ToList();
        }

        // =====================================================================
        //  Schlüssel
        // =====================================================================

        [Fact]
        public void Jeder_Schluessel_und_Alias_folgt_dem_Schluesselmuster()
        {
            List<string> verstoesse = SchluesselUndAliasse().Where(s => !Schluesselmuster.IsMatch(s)).ToList();
            Assert.True(verstoesse.Count == 0, "Schlüssel gegen das Muster: " + string.Join(", ", verstoesse));
        }

        [Fact]
        public void Schluessel_und_Aliasse_sind_eindeutig()
        {
            List<string> doppelt = SchluesselUndAliasse().GroupBy(s => s, StringComparer.Ordinal)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(doppelt.Count == 0, "Doppelt: " + string.Join(", ", doppelt));
        }

        [Fact]
        public void Jeder_Alias_fuehrt_auf_seinen_Eintrag()
        {
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle)
                foreach (string alias in f.Aliasse)
                {
                    Assert.NotEqual(f.Schluessel, alias);
                    Assert.Same(f, Vorlagenfeldkatalog.Finde(alias));
                }
            Assert.Equal("ersteller.version", Vorlagenfeldkatalog.Finde("bericht.programmversion").Schluessel);
        }

        [Fact]
        public void Finde_normiert_den_Schluessel()
        {
            Vorlagenfeld kunde = Vorlagenfeldkatalog.Finde("projekt.kunde");
            Assert.NotNull(kunde);
            Assert.Same(kunde, Vorlagenfeldkatalog.Finde(" Projekt . Kunde "));
            Assert.Same(Vorlagenfeldkatalog.Finde("projekt.geaendert"), Vorlagenfeldkatalog.Finde("Projekt.Geändert"));
            Assert.Null(Vorlagenfeldkatalog.Finde("projekt.gibtsnicht"));
            Assert.Null(Vorlagenfeldkatalog.Finde(""));
            Assert.Null(Vorlagenfeldkatalog.Finde(null));
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle)
                Assert.Same(f, Vorlagenfeldkatalog.Finde(f.Schluessel));
        }

        [Fact]
        public void Jeder_Eintrag_hat_Quelle_Art_Kontext_und_Fassung()
        {
            Assert.Equal(5, Vorlagenfeldkatalog.KATALOGFASSUNG);
            Assert.Equal(4, Vorlagenfeldkatalog.KatalogfassungWord);
            Assert.Equal(Vorlagenfeldkatalog.KATALOGFASSUNG, Vorlagenfeldkatalog.Katalogfassung);
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle)
            {
                Assert.NotNull(f.Quelle);
                Assert.True(Enum.IsDefined(typeof(Vorlagenfeldart), f.Art), f.Schluessel);
                Assert.True(Enum.IsDefined(typeof(Vorlagenfeldkontext), f.Kontext), f.Schluessel);
                Assert.InRange(f.Seit, 1, Vorlagenfeldkatalog.KATALOGFASSUNG);
                Assert.NotNull(f.Leerwert);
                Assert.True(f.Ausgaben != Vorlagenausgabe.Keine, f.Schluessel);
                Assert.True(f.Leerwert != "0", f.Schluessel + ": ein Leerwert ist nie 0");
                if (f.Art != Vorlagenfeldart.Kapitel) Assert.Empty(f.Deckt);
                if (f.Art == Vorlagenfeldart.Zahl) Assert.Equal(Vorlagenfeld.STRICH, f.Leerwert);
            }
        }

        /// <summary>
        /// Katalog v2: 27 handgepflegte Einträge der Fassung 1, dazu 25 der Fassung 2 — neun Kapitel, acht
        /// Schalter, sieben Kapitelköpfe und das Logo —, je Kennzahl drei erzeugte. Die Zahl der Kennzahlen
        /// pinnt dieser Fall bewusst nicht — eine neue Kennzahl meldet die eingefrorene Liste. Die Einträge der
        /// Fassung 3 (BV-E4) zählt <c>VorlagenfeldStandwerteTests</c>.
        /// </summary>
        [Fact]
        public void Katalog_v2_zaehlt_52_handgepflegte_und_je_Kennzahl_drei_erzeugte_Eintraege()
        {
            int kennzahlen = KennzahlenKatalog.Alle().Count;
            List<Vorlagenfeld> v2 = Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= 2).ToList();
            Assert.Equal(52, v2.Count(f => f.Handgepflegt));
            Assert.Equal(27, v2.Count(f => f.Handgepflegt && f.Seit == 1));
            Assert.Equal(3 * kennzahlen, v2.Count(f => !f.Handgepflegt));

            var bereiche = v2.GroupBy(f => f.Schluessel.Split('.')[0])
                .ToDictionary(g => g.Key, g => g.Count());
            _ausgabe.WriteLine(string.Join(", ", bereiche.Select(b => b.Key + " " + b.Value)));
            Assert.Equal(9, bereiche["bericht"]);
            Assert.Equal(14, bereiche["text"]);
            Assert.Equal(3, bereiche["ersteller"]);
            Assert.Equal(8, bereiche["projekt"]);
            Assert.Equal(9, bereiche["kapitel"]);
            Assert.Equal(8, bereiche["baustein"]);
            Assert.Equal(1, bereiche["bild"]);
            Assert.Equal(kennzahlen, bereiche["stamm"]);
            Assert.Equal(2 * kennzahlen, bereiche["kennzahl"]);
        }

        /// <summary>
        /// Die Einträge der Fassung 2 (Etappe BV-E2): je <see cref="Berichtskapitel"/> ein Kapitel (Word,
        /// Kontext Bericht), je Häkchen ein Schalter, je Kapitelkopf der Standardvorlage ein Festtext, dazu
        /// das Logo als Bild (Word, Kontext Installation) — alle mit <c>Seit</c> 2.
        /// </summary>
        [Fact]
        public void Kapitel_Schalter_Kapitelkoepfe_und_Logo_der_Fassung_2()
        {
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
            {
                Vorlagenfeld kapitel = Vorlagenfeldkatalog.Finde(k.Schluessel);
                Assert.NotNull(kapitel);
                Assert.Equal(Vorlagenfeldart.Kapitel, kapitel.Art);
                Assert.Equal(Vorlagenfeldkontext.Bericht, kapitel.Kontext);
                Assert.Equal(Vorlagenausgabe.Word, kapitel.Ausgaben);
                Assert.Equal(2, kapitel.Seit);

                if (k.Schalter != null)
                {
                    Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde(k.Schalter);
                    Assert.Equal(Vorlagenfeldart.Schalter, schalter.Art);
                    Assert.Equal(2, schalter.Seit);
                }
                if (k.Kopfschluessel != null)
                {
                    Vorlagenfeld kopf = Vorlagenfeldkatalog.Finde(k.Kopfschluessel);
                    Assert.Equal(Vorlagenfeldart.Text, kopf.Art);
                    Assert.Equal(2, kopf.Seit);
                }
            }
            Assert.Equal(new[]
            {
                "baustein.anhang", "baustein.deckblatt", "baustein.ergebnisse", "baustein.inhalt", "baustein.komponenten",
                "baustein.projekt", "baustein.vergleich", "baustein.wirtschaftlichkeit",
            }, Vorlagenfeldkatalog.Alle.Where(f => f.Art == Vorlagenfeldart.Schalter && f.Seit <= 2).Select(f => f.Schluessel)
                                    .OrderBy(s => s, StringComparer.Ordinal));
            Assert.Equal(new[]
            {
                "text.kapitel_anhang", "text.kapitel_anhang_e", "text.kapitel_ergebnisse", "text.kapitel_komponenten",
                "text.kapitel_projekt", "text.kapitel_vergleich", "text.kapitel_wirtschaftlichkeit",
            }, Vorlagenfeldkatalog.Alle.Where(f => f.Schluessel.StartsWith("text.kapitel_", StringComparison.Ordinal))
                                    .Select(f => f.Schluessel).OrderBy(s => s, StringComparer.Ordinal));

            Vorlagenfeld logo = Vorlagenfeldkatalog.Finde(Vorlagenfeldkatalog.LOGO);
            Assert.Equal(Vorlagenfeldart.Bild, logo.Art);
            Assert.Equal(Vorlagenfeldkontext.Installation, logo.Kontext);
            Assert.Equal(Vorlagenausgabe.Word, logo.Ausgaben);
            Assert.Equal(2, logo.Seit);
            Assert.Equal("", logo.Leerwert);
        }

        /// <summary>
        /// Die Titel der Häkchen stehen zweisprachig in <c>MyResource</c> (<c>BK_BER_BAUSTEIN_&lt;SCHLÜSSEL&gt;</c>);
        /// die deutschen gleichen Wort für Wort den festen Titeln (Messlatte — die Häkchenliste liest dieselben
        /// Texte wie zuvor). Der Kapitelkopf <c>text.kapitel_&lt;name&gt;</c> nennt dagegen die Überschrift, die der
        /// Baustein selbst setzt: Sie gleicht dem deutschen Häkchentitel bis auf die Ergebnisse
        /// („Berechnungsergebnisse je Variante“ — so hält die Messlatte des Berichts den Kapitelteil zeilengleich).
        /// </summary>
        [Fact]
        public void Haekchentitel_stehen_zweisprachig_in_MyResource_und_die_deutschen_bleiben()
        {
            string[] deutsch = { "Deckblatt", "Inhaltsverzeichnis", "Projektbeschreibung", "Komponenten & Varianten",
                                 "Ergebnisse je Variante", "Variantenvergleich", "Wirtschaftlichkeit", "Anhang" };
            string[] englisch = { "Title page", "Table of contents", "Project description", "Components & variants",
                                  "Results per variant", "Variant comparison", "Economic viability", "Appendix" };
            Assert.Equal(deutsch, BerichtsKonfiguration.AlleBausteine.Select(b => b.Titel));
            Assert.Equal(deutsch, BerichtsKonfiguration.AlleBausteine.Select(b => b.TitelIn(false)));
            Assert.Equal(englisch, BerichtsKonfiguration.AlleBausteine.Select(b => b.TitelIn(true)));
            Assert.All(BerichtsKonfiguration.AlleBausteine,
                       b => Assert.Equal("BK_BER_BAUSTEIN_" + b.Schluessel.ToUpperInvariant(), b.TitelId));
            Assert.Equal("Economic viability", BerichtsKonfiguration.Titel(BerichtsKonfiguration.B_WIRTSCHAFT, true));
            Assert.Equal("Wirtschaftlichkeit", BerichtsKonfiguration.Titel(BerichtsKonfiguration.B_WIRTSCHAFT, false));
            Assert.Equal("unbekannt", BerichtsKonfiguration.Titel("unbekannt", true));

            foreach (Berichtskapitel k in Berichtskapitel.Alle.Where(k => k.Kopfschluessel != null && k.Schalter != null))
            {
                string erwartet = k.Name == Berichtskapitel.ERGEBNISSE
                    ? "Berechnungsergebnisse je Variante"
                    : BerichtsKonfiguration.Titel(k.Baustein, false);
                Assert.Equal(erwartet, k.Ueberschrift(false));
            }
        }

        /// <summary>
        /// Der Sammelanker deckt jedes Kapitel; jedes Kapitel deckt die Einzelschlüssel, die sein Baustein
        /// schreibt, dazu Kapitelkopf und Schalter — alles Schlüssel des Katalogs. Ohne Deckt ist nur ein
        /// Kapitel, das keinen Einzelschlüssel hat.
        /// </summary>
        [Fact]
        public void Bericht_inhalt_deckt_jedes_Kapitel_und_jedes_Kapitel_seine_Schluessel()
        {
            Vorlagenfeld inhalt = Vorlagenfeldkatalog.Finde("bericht.inhalt");
            Assert.Equal(Vorlagenfeldart.Kapitel, inhalt.Art);
            Assert.Equal(Vorlagenausgabe.Word, inhalt.Ausgaben);
            Assert.Equal(1, inhalt.Seit);
            Assert.Equal(Berichtskapitel.Alle.Select(k => k.Schluessel), inhalt.Deckt);
            Assert.Equal(10, Vorlagenfeldkatalog.Alle.Count(f => f.Art == Vorlagenfeldart.Kapitel));

            foreach (Berichtskapitel k in Berichtskapitel.Alle)
            {
                Vorlagenfeld kapitel = Vorlagenfeldkatalog.Finde(k.Schluessel);
                Assert.All(kapitel.Deckt, d => Assert.True(Vorlagenfeldkatalog.Finde(d)?.Schluessel == d, k.Name + ": " + d));
                Assert.Equal(kapitel.Deckt.Count, kapitel.Deckt.Distinct(StringComparer.Ordinal).Count());
                if (k.Schalter != null) Assert.Contains(k.Schalter, kapitel.Deckt);
                if (k.Kopfschluessel != null) Assert.Contains(k.Kopfschluessel, kapitel.Deckt);
            }

            Assert.Equal(new[]
            {
                "bericht.titel", "bericht.untertitel", "projekt.kunde", "projekt.bearbeiter", "bericht.varianten.liste",
                "bericht.datum", "bericht.gebaeudemodell.ausweis", "ersteller.firma", "ersteller.programm", "ersteller.version",
                "baustein.deckblatt",
            }, Vorlagenfeldkatalog.Finde("kapitel.deckblatt").Deckt);
            Assert.Equal(Vorlagenfeldkatalog.Alle.Where(f => f.Schluessel.StartsWith("projekt.", StringComparison.Ordinal))
                                             .Select(f => f.Schluessel)
                                             .Concat(new[]
                                             {
                                                 "tabelle.kaelteerzeuger", "hat.tabelle.kaelteerzeuger",
                                                 "tabelle.speichertemperaturen", "hat.tabelle.speichertemperaturen",
                                                 "tabelle.gebaeude.ergebnis", "hat.tabelle.gebaeude.ergebnis",
                                                 "stamm.bild.speichertemperaturen", "hat.bild.speichertemperaturen",
                                                 "text.kapitel_projekt", "baustein.projekt",
                                             }),
                         Vorlagenfeldkatalog.Finde("kapitel.projekt").Deckt);
            foreach (string kennzahlen in new[] { "kapitel.ergebnisse", "kapitel.vergleich" })
            {
                IReadOnlyList<string> deckt = Vorlagenfeldkatalog.Finde(kennzahlen).Deckt;
                foreach (Kennzahl k in KennzahlenKatalog.Alle())
                {
                    Assert.Contains("stamm.kennzahl." + k.Schluessel, deckt);
                    Assert.Contains("kennzahl." + k.Schluessel + ".beschriftung", deckt);
                    Assert.Contains("kennzahl." + k.Schluessel + ".einheit", deckt);
                }
            }
            Assert.Contains("bericht.emissionsmodus", Vorlagenfeldkatalog.Finde("kapitel.vergleich").Deckt);
            Assert.Equal(new[]
            {
                "bericht.warnungen", "tabelle.anhang.simulationsstaende", "hat.tabelle.anhang.simulationsstaende",
                "text.kapitel_anhang", "baustein.anhang",
            }, Vorlagenfeldkatalog.Finde("kapitel.anhang").Deckt);
            Assert.Equal(new[] { "tabelle.anhang_e.checkliste", "hat.tabelle.anhang_e.checkliste", "text.kapitel_anhang_e" },
                         Vorlagenfeldkatalog.Finde("kapitel.anhang_e").Deckt);
            Assert.Equal(new[] { "baustein.inhalt" }, Vorlagenfeldkatalog.Finde("kapitel.inhalt").Deckt);
        }

        /// <summary>
        /// <b>Tabellen und Bilder im Deckt ihres Kapitels</b> (Katalog v4, Anwenderentscheid BV-E5-3): Jede Tabelle und
        /// jedes Bild der Fassung 4 steht — samt seinem Schalter <c>hat.tabelle.*</c> bzw. <c>hat.bild.*</c> unmittelbar
        /// dahinter — im <see cref="Vorlagenfeld.Deckt"/> genau des Kapitels, dessen Baustein dieselbe Tafel bzw. dasselbe
        /// Bild schreibt; ausgenommen sind nur die Mustertabelle (Steuerschlüssel der Engine, kein Inhalt) und das Logo
        /// (Kopfzeile, Fassung 2). Deckblatt und Inhaltsverzeichnis decken keine. Die Zuordnung je Kapitel steht hier
        /// ausdrücklich — eine neue Tabelle, ein neues Bild ohne Kapitel fällt auf.
        /// </summary>
        [Fact]
        public void Jede_Tabelle_und_jedes_Bild_steht_im_Deckt_seines_Kapitels()
        {
            List<Vorlagenfeld> v4 = Vorlagenfeldkatalog.Alle
                .Where(f => f.Seit == 4 && (f.Art == Vorlagenfeldart.Tabelle || f.Art == Vorlagenfeldart.Bild))
                .Where(f => f.Schluessel != Vorlagenfeldkatalog.MUSTER_TABELLE)
                .ToList();
            Assert.True(v4.Count > 50, v4.Count + " Tabellen und Bilder der Fassung 4");

            var gefunden = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
            {
                IReadOnlyList<string> deckt = Vorlagenfeldkatalog.Finde(k.Schluessel).Deckt;
                for (int i = 0; i < deckt.Count; i++)
                {
                    Vorlagenfeld f = Vorlagenfeldkatalog.Finde(deckt[i]);
                    if (f.Seit != 4 || (f.Art != Vorlagenfeldart.Tabelle && f.Art != Vorlagenfeldart.Bild)) continue;
                    Assert.True(gefunden.TryAdd(f.Schluessel, k.Name), f.Schluessel + " steht in zwei Kapiteln");
                    string schalter = f.Art == Vorlagenfeldart.Tabelle
                        ? Vorlagenfeldkatalog.SchalterDerTabelle(f.Schluessel)
                        : Vorlagenfeldkatalog.SchalterDesBildes(f.Schluessel);
                    Assert.Equal(Vorlagenfeldart.Schalter, Vorlagenfeldkatalog.Finde(schalter).Art);
                    Assert.True(i + 1 < deckt.Count && deckt[i + 1] == schalter, k.Name + ": " + schalter + " fehlt hinter " + f.Schluessel);
                }
            }

            List<string> ohneKapitel = v4.Select(f => f.Schluessel).Where(s => !gefunden.ContainsKey(s)).ToList();
            Assert.True(ohneKapitel.Count == 0, "Ohne Kapitel: " + string.Join(", ", ohneKapitel));
            Assert.DoesNotContain(Vorlagenfeldkatalog.MUSTER_TABELLE, gefunden.Keys);

            // Die Zuordnung, ausdrücklich — dieselbe Tafel, dasselbe Bild wie der Baustein.
            string[] Von(string kapitel) => gefunden.Where(p => p.Value == kapitel).Select(p => p.Key)
                                                    .Where(s => !s.StartsWith("tabelle.komponenten.kenndaten.", StringComparison.Ordinal)
                                                             && !s.StartsWith("tabelle.vergleich.", StringComparison.Ordinal)
                                                             && !s.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal))
                                                    .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { "stamm.bild.speichertemperaturen", "tabelle.gebaeude.ergebnis", "tabelle.kaelteerzeuger",
                                 "tabelle.speichertemperaturen" }, Von(Berichtskapitel.PROJEKT));
            Assert.Equal(new[] { "stand.tabelle.abweichungen", "tabelle.komponenten.matrix", "tabelle.varianten" },
                         Von(Berichtskapitel.KOMPONENTEN));
            Assert.Equal(new[] { "stand.bild.speicherverlauf", "stand.bild.strombilanz_monate", "stand.bild.waerme_dauerlinie",
                                 "stand.bild.waerme_jahresverlauf", "stand.tabelle.kennzahlen" }, Von(Berichtskapitel.ERGEBNISSE));
            Assert.Equal(new[] { "stand.bild.deckung_strom", "stand.bild.deckung_waerme", "stand.tabelle.brennstoffmengen",
                                 "stand.tabelle.erzeuger", "tabelle.vergleich" }, Von(Berichtskapitel.VERGLEICH));
            Assert.Equal(new[]
            {
                "bild.wirtschaft.barwerte_kumuliert", "bild.wirtschaft.bruecke", "bild.wirtschaft.kapitalwert_szenarien",
                "bild.wirtschaft.spanne", "stand.bild.zahlungsstrom", "stand.tabelle.betriebskosten", "stand.tabelle.emissionsbilanz",
                "stand.tabelle.kwkg_module", "stand.tabelle.mehrjahres", "stand.tabelle.sensitivitaet", "stand.tabelle.strommengen",
                "stand.tabelle.vermiedene_kosten", "tabelle.wirtschaft.kennzahlen", "tabelle.wirtschaft.kennzahlen.guenstig",
                "tabelle.wirtschaft.kennzahlen.unguenstig", "tabelle.wirtschaft.nicht_monetaer", "tabelle.wirtschaft.szenarien",
            }, Von(Berichtskapitel.WIRTSCHAFTLICHKEIT));
            Assert.All(gefunden.Where(p => p.Key.StartsWith("tabelle.komponenten.kenndaten.", StringComparison.Ordinal)),
                       p => Assert.Equal(Berichtskapitel.KOMPONENTEN, p.Value));
            Assert.All(gefunden.Where(p => p.Key.StartsWith("tabelle.vergleich.", StringComparison.Ordinal)
                                         || p.Key.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal)),
                       p => Assert.Equal(Berichtskapitel.VERGLEICH, p.Value));
            Assert.Contains(gefunden, p => p.Key.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal));
            Assert.Contains(gefunden, p => p.Key.StartsWith("tabelle.komponenten.kenndaten.", StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Schalter je Tabelle und Bild zählen wie <c>baustein.*</c> nicht als Inhalt: Ein Kapitel, das die Vorlage
        /// nicht führt, gilt erst als gedeckt, wenn sie jede seiner Tabellen und jedes Bild führt — nicht über deren
        /// Schalter und nicht über einen Teil (<see cref="Vorlagenfeldkatalog.Gedeckt"/>).
        /// </summary>
        [Fact]
        public void Gedeckt_zaehlt_Tabellen_und_Bilder_als_Inhalt_und_ihre_Schalter_nicht()
        {
            Assert.DoesNotContain("kapitel.anhang_e", Vorlagenfeldkatalog.Gedeckt(new[] { "hat.tabelle.anhang_e.checkliste" }));
            HashSet<string> anhangE = Vorlagenfeldkatalog.Gedeckt(new[] { "tabelle.anhang_e.checkliste" });
            Assert.Contains("kapitel.anhang_e", anhangE);
            Assert.Contains("text.kapitel_anhang_e", anhangE);

            Assert.DoesNotContain("kapitel.anhang", Vorlagenfeldkatalog.Gedeckt(new[] { "bericht.warnungen" }));
            Assert.Contains("kapitel.anhang", Vorlagenfeldkatalog.Gedeckt(new[] { "bericht.warnungen", "tabelle.anhang.simulationsstaende" }));

            HashSet<string> wirtschaft = Vorlagenfeldkatalog.Gedeckt(new[] { "kapitel.wirtschaftlichkeit" });
            Assert.Contains("tabelle.wirtschaft.szenarien", wirtschaft);
            Assert.Contains("hat.bild.wirtschaft.spanne", wirtschaft);
            Assert.Contains("stand.bild.zahlungsstrom", wirtschaft);
            Assert.DoesNotContain("stand.bild.deckung_waerme", wirtschaft);
        }

        /// <summary>
        /// <see cref="Vorlagenfeldkatalog.Gedeckt"/>: was die Kapitel einer Vorlage decken, gilt; ein
        /// Kapitel, das sie nicht führt, gilt, wenn sie jeden seiner Einzelschlüssel führt (das Deckblatt aus
        /// Platzhaltern) — nicht aber über Schalter oder Kapitelkopf allein; der Sammelanker gilt, sobald
        /// jedes Kapitel gilt.
        /// </summary>
        [Fact]
        public void Gedeckt_folgt_den_Kapiteln_und_dem_Deckblatt_aus_Platzhaltern()
        {
            HashSet<string> projekt = Vorlagenfeldkatalog.Gedeckt(new[] { "kapitel.projekt" });
            Assert.Contains("projekt.klimaregion", projekt);
            Assert.Contains("baustein.projekt", projekt);
            Assert.DoesNotContain("kapitel.komponenten", projekt);

            HashSet<string> inhalt = Vorlagenfeldkatalog.Gedeckt(new[] { "bericht.inhalt" });
            Assert.All(Berichtskapitel.Alle, k => Assert.Contains(k.Schluessel, inhalt));
            Assert.Contains("stamm.kennzahl.eff.jaz", inhalt);

            HashSet<string> deckblatt = Vorlagenfeldkatalog.Gedeckt(Vorlagenfeldkatalog.Deckblattangaben);
            Assert.Contains("kapitel.deckblatt", deckblatt);
            Assert.Contains("baustein.deckblatt", deckblatt);
            Assert.DoesNotContain("kapitel.deckblatt", Vorlagenfeldkatalog.Gedeckt(new[] { "bericht.titel", "bericht.datum" }));

            Assert.DoesNotContain("kapitel.komponenten", Vorlagenfeldkatalog.Gedeckt(new[] { "text.kapitel_komponenten", "baustein.komponenten" }));
            Assert.Contains("ersteller.version", Vorlagenfeldkatalog.Gedeckt(new[] { "bericht.programmversion" }));   // Alias
            Assert.Empty(Vorlagenfeldkatalog.Gedeckt(new[] { "gibt.es.nicht" }));
        }

        /// <summary>
        /// <b>Die Deckungswache</b> (Konzept 12, „Deckung je Ausgabe“): Jeder Katalogschlüssel mit Ausgabe
        /// Word steht in der Word-Standardvorlage — direkt oder über das <see cref="Vorlagenfeld.Deckt"/> eines
        /// dort geführten Kapitels (<see cref="Vorlagenfeldkatalog.Gedeckt"/>); ausgenommen sind nur
        /// vorgemerkte Einträge (<c>Seit</c> über der Fassung). Das Logo der Kopfzeile ist ein Bild mit dem
        /// Alternativtext <c>{{bild.ersteller.logo}}</c> (Entscheid BV-E2-1). Die Einzelwerte ab Fassung 3 (BV-E4:
        /// Stände, Paarsicht, Gruppe, Gebäude, Datenschalter) gehören in eigene Vorlagen mit Blöcken — die
        /// Standardvorlage bleibt inhaltsgleich und führt von ihnen nur die Kapitel. <b>Die Fassung 4</b> (BV-E5:
        /// Strukturtabellen, Bilder und ihre Schalter) prüft die Wache wieder ganz (Anwenderentscheid BV-E5-3): Jede
        /// Tabelle und jedes Bild deckt das Kapitel, dessen Baustein sie schreibt; ausgenommen ist allein die
        /// Mustertabelle <c>{{muster.tabelle}}</c> — sie steuert die Engine und trägt keinen Inhalt.
        /// </summary>
        /// <summary>Die Fassungen, deren Einträge die Standardvorlage vollständig führt — direkt oder über ein Kapitel.</summary>
        private static readonly int[] FassungenStandardvorlage = { 1, 2, 4 };

        [Fact]
        public void Deckungswache_jeder_Word_Schluessel_steht_in_der_Standardvorlage()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null) return;
            byte[] vorlage = File.ReadAllBytes(pfad);

            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, new Pruefkontext());
            Assert.Empty(befund.UnbekannteSchluessel);
            Assert.Contains(Vorlagenfeldkatalog.LOGO, befund.Schluessel);
            Assert.Equal((int?)Vorlagenfeldkatalog.KatalogfassungWord, befund.Katalogfassung);
            HashSet<string> gedeckt = Vorlagenfeldkatalog.Gedeckt(befund.Schluessel);

            List<Vorlagenfeld> geprueft = Vorlagenfeldkatalog.Alle
                .Where(f => (f.Ausgaben & Vorlagenausgabe.Word) != 0 && f.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG)
                .Where(f => FassungenStandardvorlage.Contains(f.Seit) || f.Art == Vorlagenfeldart.Kapitel)
                .Where(f => f.Schluessel != Vorlagenfeldkatalog.MUSTER_TABELLE)
                .ToList();
            Assert.Contains(geprueft, f => f.Seit == 4 && f.Art == Vorlagenfeldart.Tabelle);
            Assert.Contains(geprueft, f => f.Seit == 4 && f.Art == Vorlagenfeldart.Bild);
            Assert.Contains(geprueft, f => f.Seit == 4 && f.Art == Vorlagenfeldart.Schalter);
            List<string> fehlen = geprueft.Select(f => f.Schluessel).Where(s => !gedeckt.Contains(s)).ToList();
            _ausgabe.WriteLine("Standardvorlage: " + befund.Schluessel.Count + " Schlüssel direkt, " + gedeckt.Count + " gedeckt, "
                               + geprueft.Count(f => f.Seit == 4) + " der Fassung 4 geprüft");
            Assert.True(fehlen.Count == 0, "Nicht in der Standardvorlage und von keinem ihrer Kapitel gedeckt: " + string.Join(", ", fehlen));
        }

        // =====================================================================
        //  Kennzahlen
        // =====================================================================

        [Fact]
        public void Jede_Kennzahl_hat_ihre_drei_Eintraege()
        {
            foreach (Kennzahl k in KennzahlenKatalog.Alle())
            {
                Vorlagenfeld wert = Vorlagenfeldkatalog.Finde("stamm.kennzahl." + k.Schluessel);
                Assert.NotNull(wert);
                Assert.Equal(Vorlagenfeldart.Zahl, wert.Art);
                Assert.Equal(Vorlagenfeldkontext.Stamm, wert.Kontext);
                Assert.Equal(k.Format, wert.Format);
                Assert.Equal(k.Einheit, wert.Einheit);
                Pruefe(wert.Ableitung, Vorlagenfeldkatalog.MUSTER_STAMM_KENNZAHL, nameof(WindowsFormsApplication1.MyResource.Resource.VF_MUSTER_STAMM_KENNZAHL), k);

                Vorlagenfeld beschriftung = Vorlagenfeldkatalog.Finde("kennzahl." + k.Schluessel + ".beschriftung");
                Assert.NotNull(beschriftung);
                Assert.Equal(Vorlagenfeldart.Text, beschriftung.Art);
                Assert.Equal(Vorlagenfeldkontext.Bericht, beschriftung.Kontext);
                Pruefe(beschriftung.Ableitung, Vorlagenfeldkatalog.MUSTER_KENNZAHL_BESCHRIFTUNG, nameof(WindowsFormsApplication1.MyResource.Resource.VF_MUSTER_KENNZAHL_BESCHRIFTUNG), k);

                Vorlagenfeld einheit = Vorlagenfeldkatalog.Finde("kennzahl." + k.Schluessel + ".einheit");
                Assert.NotNull(einheit);
                Assert.Equal(Vorlagenfeldart.Text, einheit.Art);
                Pruefe(einheit.Ableitung, Vorlagenfeldkatalog.MUSTER_KENNZAHL_EINHEIT, nameof(WindowsFormsApplication1.MyResource.Resource.VF_MUSTER_KENNZAHL_EINHEIT), k);
            }
        }

        private static void Pruefe(Vorlagenfeldableitung ableitung, string muster, string musterId, Kennzahl k)
        {
            Assert.NotNull(ableitung);
            Assert.Equal(muster, ableitung.Muster);
            Assert.Equal(musterId, ableitung.MusterId);
            Assert.Equal(k.Schluessel, ableitung.Parameter);
            Assert.Contains(musterId, Vorlagenfeldkatalog.Musterschluessel);
        }

        /// <summary>
        /// Den Bedarf (Konzept 5.1, Etappe BV-E3) tragen die Kältestunden (gezählt an der Kanalreihe des
        /// Laufs) und die Kapitel, deren Baustein mehr als den Regellauf liest: die Ergebnisse je Variante
        /// die Stundenreihen (Ganglinien), die Wirtschaftlichkeit Verlauf und Emissionsbilanz — und der
        /// Sammelanker alle drei. Jedes Kapitel führt genau den Bedarf seines Bausteins.
        /// </summary>
        [Fact]
        public void Den_Bedarf_tragen_die_Kaeltestunden_und_die_Kapitel_der_Bausteine()
        {
            List<string> mitBedarf = Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= 2 && f.Bedarf != Vorlagenbedarf.Keiner)
                .Select(f => f.Schluessel).ToList();
            string stunden = "stamm.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN;
            Assert.Equal(new[] { "bericht.inhalt", "kapitel.ergebnisse", "kapitel.wirtschaftlichkeit", stunden }, mitBedarf);

            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde(stunden).Bedarf);
            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde("kapitel.ergebnisse").Bedarf);
            Assert.Equal(Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz,
                         Vorlagenfeldkatalog.Finde("kapitel.wirtschaftlichkeit").Bedarf);
            Assert.Equal(Vorlagenbedarf.Zeitreihen | Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz,
                         Vorlagenfeldkatalog.Finde("bericht.inhalt").Bedarf);
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
                Assert.Equal(k.Bedarf, Vorlagenfeldkatalog.Finde(k.Schluessel).Bedarf);
        }

        [Fact]
        public void Handgepflegt_heisst_eigene_Beschreibung_erzeugt_heisst_Muster()
        {
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle)
            {
                if (f.Handgepflegt)
                {
                    Assert.Null(f.Ableitung);
                    Assert.Equal(Vorlagenfeldkatalog.RessourcenName(f.Schluessel), f.BeschreibungId);
                }
                else
                {
                    Assert.NotNull(f.Ableitung);
                    Assert.Null(f.BeschreibungId);
                }
                Assert.Null(f.BeispielId);   // Katalog v1 führt keine Beispielwerte
            }
        }

        // =====================================================================
        //  Ressourcen — beide Sprachen
        // =====================================================================

        [Fact]
        public void Jede_handgepflegte_Beschreibung_steht_in_beiden_resx()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            var funde = new List<string>();
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle.Where(f => f.Handgepflegt))
                PruefeZweisprachig(f.BeschreibungId, de, en, funde);
            Assert.True(funde.Count == 0, string.Join(Environment.NewLine, funde));
        }

        [Fact]
        public void Muster_und_Texte_der_Aufloesung_stehen_in_beiden_resx()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            var funde = new List<string>();
            foreach (string muster in Vorlagenfeldkatalog.Musterschluessel)
            {
                PruefeZweisprachig(muster, de, en, funde);
                if (de.TryGetValue(muster, out string d) && !d.Contains("{0}")) funde.Add(muster + ": ohne {0} in Resource.resx");
                if (en.TryGetValue(muster, out string e) && !e.Contains("{0}")) funde.Add(muster + ": ohne {0} in Resource.en-US.resx");
            }
            foreach (string text in Vorlagenfeldkatalog.Textschluessel)
                PruefeZweisprachig(text, de, en, funde);
            Assert.True(funde.Count == 0, string.Join(Environment.NewLine, funde));
            Assert.Equal(3 + 16 + 3 + 2, Vorlagenfeldkatalog.Musterschluessel.Count);   // Fassung 3: 16 Muster, Fassung 4: 3 der Tabellen, 2 der Bilder
            Assert.Equal(16 + 11 + 1 + 6, Vorlagenfeldkatalog.Textschluessel.Count);    // Fassung 3: 11 Gründe, Fassung 4: leere Tabelle, 6 der Bilder
        }

        private static void PruefeZweisprachig(string name, Dictionary<string, string> de, Dictionary<string, string> en,
                                               List<string> funde)
        {
            bool hatDe = de.TryGetValue(name, out string d) && d.Trim().Length > 0;
            bool hatEn = en.TryGetValue(name, out string e) && e.Trim().Length > 0;
            if (!hatDe) funde.Add(name + ": fehlt in Resource.resx");
            if (!hatEn) funde.Add(name + ": fehlt in Resource.en-US.resx");
            if (hatDe && hatEn && string.Equals(d, e, StringComparison.Ordinal))
                funde.Add(name + ": englisch gleich deutsch („" + d + "“) — nicht übersetzt?");
        }

        [Fact]
        public void Jede_Beschreibung_loest_sich_in_beiden_Sprachen_auf()
        {
            Dictionary<string, Kennzahl> kennzahlen = KennzahlenKatalog.Alle().ToDictionary(k => k.Schluessel);
            foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle)
            {
                string de = Vorlagenfeldkatalog.Beschreibung(f, false);
                string en = Vorlagenfeldkatalog.Beschreibung(f, true);
                Assert.False(string.IsNullOrWhiteSpace(de), f.Schluessel);
                Assert.False(string.IsNullOrWhiteSpace(en), f.Schluessel);
                Assert.DoesNotContain("{0}", de);
                Assert.DoesNotContain("{0}", en);
                Assert.DoesNotContain("{1}", de);
                Assert.DoesNotContain("{1}", en);
                // Kennzahlmuster tragen die Beschriftung; die Muster mit eigener Bezeichnung (Zeilen der
                // Wirtschaftlichkeit, Parameter, Szenarien, Paarsicht) prüft VorlagenfeldStandwerteTests.
                if (!f.Handgepflegt && f.Ableitung.Bezeichnung == null)
                {
                    Kennzahl k = kennzahlen[f.Ableitung.Parameter];
                    Assert.Contains(k.LabelDe, de);
                    Assert.Contains(k.LabelEn, en);
                }
            }
            Assert.Equal("", Vorlagenfeldkatalog.Beschreibung(null, false));
        }

        // =====================================================================
        //  Namen: Ressourcen, Excel, Formelmappe
        // =====================================================================

        [Fact]
        public void Ressourcennamen_sind_kollisionsfrei_und_Bezeichner()
        {
            Assert.Equal("VF_PROJEKT__KUNDE", Vorlagenfeldkatalog.RessourcenName("projekt.kunde"));
            Assert.Equal("VF_TEXT__ERSTELLT_MIT", Vorlagenfeldkatalog.RessourcenName("text.erstellt_mit"));

            List<string> namen = SchluesselUndAliasse().Select(Vorlagenfeldkatalog.RessourcenName).ToList();
            Assert.Equal(namen.Count, namen.Distinct(StringComparer.Ordinal).Count());
            foreach (string n in namen)
                Assert.Matches("^[A-Za-z_][A-Za-z0-9_]*$", n);   // designer_neu.py nimmt nur C#-Bezeichner an

            // Kein Katalogschlüssel darf auf ein Muster fallen (VF_MUSTER_… hat nur einfache Unterstriche).
            Assert.Empty(namen.Intersect(Vorlagenfeldkatalog.Musterschluessel, StringComparer.Ordinal));

            // Die Abbildung ist umkehrbar — eindeutig, weil das Muster nur einzelne Unterstriche kennt.
            foreach (string s in SchluesselUndAliasse())
            {
                string zurueck = Vorlagenfeldkatalog.RessourcenName(s).Substring(Vorlagenfeldkatalog.PRAEFIX_RESSOURCE.Length)
                                                    .Replace("__", ".").ToLowerInvariant();
                Assert.Equal(s, zurueck);
            }
        }

        [Fact]
        public void Excel_Namen_sind_kollisionsfrei_und_gueltig()
        {
            Assert.Equal("EPOS.stamm.kennzahl.eff.jaz", Vorlagenfeldkatalog.ExcelName("stamm.kennzahl.eff.jaz"));

            List<string> namen = SchluesselUndAliasse().Select(Vorlagenfeldkatalog.ExcelName).ToList();
            // Excel vergleicht Namen ohne Rücksicht auf Groß- und Kleinschreibung.
            Assert.Equal(namen.Count, namen.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (string n in namen)
            {
                Assert.Matches(@"^[A-Za-z_][A-Za-z0-9_.]*$", n);
                Assert.InRange(n.Length, 1, 255);
                Assert.StartsWith(Vorlagenfeldkatalog.PRAEFIX_EXCEL, n);
            }
        }

        [Fact]
        public void Kein_Excel_Name_faellt_auf_einen_Namen_der_Formelmappe()
        {
            var reserviert = new HashSet<string>(Vorlagenfeldkatalog.ReservierteExcelNamen, StringComparer.OrdinalIgnoreCase);
            List<string> treffer = SchluesselUndAliasse().Select(Vorlagenfeldkatalog.ExcelName).Where(reserviert.Contains).ToList();
            Assert.True(treffer.Count == 0, "Reserviert: " + string.Join(", ", treffer));
            Assert.DoesNotContain(Vorlagenfeldkatalog.ReservierteExcelNamen,
                                  n => n.StartsWith(Vorlagenfeldkatalog.PRAEFIX_EXCEL, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Die reservierten Namen DECKEN die Formelmappe: Jede Namenskonstante von
        /// <c>ExcelFormelmappe</c> (alle <c>const string</c> außer den Anhängen und Formaten) steht
        /// mit beiden Szenarioanhängen in <see cref="Vorlagenfeldkatalog.ReservierteExcelNamen"/>.
        /// Eine neue Namenskonstante der Formelmappe fällt hier auf.
        /// </summary>
        [Fact]
        public void Die_reservierten_Namen_decken_die_Formelmappe()
        {
            List<FieldInfo> konstanten = typeof(ExcelFormelmappe)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Where(f => !f.Name.StartsWith("ANHANG_", StringComparison.Ordinal) &&
                            !f.Name.StartsWith("FORMAT_", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(10, konstanten.Count);

            foreach (FieldInfo f in konstanten)
            {
                string name = (string)f.GetRawConstantValue();
                Assert.Contains(name, Vorlagenfeldkatalog.ReservierteExcelNamen);
                Assert.Contains(name + ExcelFormelmappe.ANHANG_GUENSTIG, Vorlagenfeldkatalog.ReservierteExcelNamen);
                Assert.Contains(name + ExcelFormelmappe.ANHANG_UNGUENSTIG, Vorlagenfeldkatalog.ReservierteExcelNamen);
            }
            Assert.Equal(30, Vorlagenfeldkatalog.ReservierteExcelNamen.Count);
        }

        // =====================================================================
        //  Eingefrorene Schlüssellisten (5.6)
        // =====================================================================

        /// <summary>Dateiname der eingefrorenen Liste einer Fassung.</summary>
        private static string Listendatei(int fassung) { return "Vorlagenfeldkatalog_v" + fassung + ".txt"; }

        /// <summary>Die Liste des Katalogs für eine Fassung: Schlüssel mit <c>Seit</c> ≤ Fassung und
        /// ihre Aliasse, ordinal sortiert.</summary>
        private static List<string> Liste(int fassung)
        {
            return Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= fassung)
                .SelectMany(f => new[] { f.Schluessel }.Concat(f.Aliasse.Select(a => a + " -> " + f.Schluessel)))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }

        private static readonly string[] Kopf =
        {
            "# Vorlagenfeldkatalog — eingefrorene Schlüsselliste (Konzept Berichtsvorlagen 5.6, VorlagenfeldkatalogWacheTests).",
            "# Je Zeile ein ausgelieferter Schlüssel, ordinal sortiert; ein Alias als „alias -> ziel“.",
            "# Nie von Hand ändern: eine neue Fassung entsteht aus dem Testausgabeordner.",
        };

        /// <summary>Jede Katalogfassung von 1 bis zur laufenden.</summary>
        public static IEnumerable<object[]> Fassungen()
        {
            for (int f = 1; f <= Vorlagenfeldkatalog.KATALOGFASSUNG; f++) yield return new object[] { f };
        }

        /// <summary>
        /// JEDE Fassung gleicht ihrer eingefrorenen Liste: die Schlüssel mit <c>Seit</c> ≤ Fassung samt
        /// Aliassen. So fällt auch auf, wenn ein neuer Eintrag die <c>Seit</c> einer ausgelieferten Fassung
        /// trüge — er stünde dann in deren Liste, die sich nie mehr ändert.
        /// </summary>
        [Theory]
        [MemberData(nameof(Fassungen))]
        public void Die_Liste_jeder_Fassung_gleicht_dem_Katalog(int fassung)
        {
            string datei = Listendatei(fassung);
            var aktuell = new List<string>(Kopf) { "# Katalogfassung " + fassung };
            aktuell.AddRange(Liste(fassung));

            string pfad = Path.Combine(Messlattenordner(), datei);
            List<string> erwartet = File.Exists(pfad) ? Zeilen(File.ReadAllText(pfad, Encoding.UTF8)) : null;
            if (erwartet != null && erwartet.SequenceEqual(aktuell, StringComparer.Ordinal))
            {
                _ausgabe.WriteLine(datei + ": " + (aktuell.Count - Kopf.Length - 1) + " Schlüssel, gleich der Liste");
                return;
            }

            string ausgabe = Path.Combine(AppContext.BaseDirectory, "Messlatten", datei);
            Directory.CreateDirectory(Path.GetDirectoryName(ausgabe));
            File.WriteAllText(ausgabe, string.Join("\r\n", aktuell) + "\r\n", new UTF8Encoding(false));

            string fehlt = erwartet == null ? "Die Liste fehlt." :
                "Nur im Katalog: " + string.Join(", ", aktuell.Except(erwartet, StringComparer.Ordinal).Take(20)) +
                " | nur in der Liste: " + string.Join(", ", erwartet.Except(aktuell, StringComparer.Ordinal).Take(20));
            Assert.Fail(datei + " weicht vom Katalog ab. " + fehlt + Environment.NewLine +
                        "Die Liste dieses Laufs steht unter " + ausgabe + ". Eine ausgelieferte Fassung wird nie " +
                        "geändert: neue Schlüssel bekommen eine höhere KATALOGFASSUNG und eine neue Liste.");
        }

        [Fact]
        public void Jeder_eingefrorene_Schluessel_ist_lebendig_oder_Alias()
        {
            string[] listen = Directory.GetFiles(Messlattenordner(), "Vorlagenfeldkatalog_v*.txt");
            Assert.NotEmpty(listen);
            var funde = new List<string>();
            foreach (string liste in listen)
                foreach (string zeile in Zeilen(File.ReadAllText(liste, Encoding.UTF8)).Where(z => z.Length > 0 && z[0] != '#'))
                {
                    string[] teile = zeile.Split(new[] { " -> " }, StringSplitOptions.None);
                    Vorlagenfeld f = Vorlagenfeldkatalog.Finde(teile[0]);
                    if (f == null) funde.Add(Path.GetFileName(liste) + ": " + teile[0] + " ist weder Schlüssel noch Alias");
                    else if (teile.Length > 1 && f.Schluessel != teile[1])
                        funde.Add(Path.GetFileName(liste) + ": Alias " + teile[0] + " führt auf " + f.Schluessel + " statt " + teile[1]);
                }
            Assert.True(funde.Count == 0, string.Join(Environment.NewLine, funde));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static string Messlattenordner()
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            Assert.True(wurzel != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return Path.Combine(wurzel, BerichtVorlagenMesslatteTests.MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>Die Einträge einer <c>.resx</c> des Kerns (Name → entschlüsselter Wert).</summary>
        private static Dictionary<string, string> Resx(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            Assert.True(wurzel != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            string text = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "MyResource", datei), Encoding.UTF8);
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        /// <summary>Die Zeilen eines Textes, gleich welche Zeilenenden; eine Schlusszeile ohne Inhalt entfällt.</summary>
        private static List<string> Zeilen(string text)
        {
            List<string> zeilen = text.Split('\n').Select(z => z.TrimEnd('\r')).ToList();
            if (zeilen.Count > 0 && zeilen[^1].Length == 0) zeilen.RemoveAt(zeilen.Count - 1);
            return zeilen;
        }
    }
}
