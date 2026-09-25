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
    /// <c>alias -&gt; ziel</c>. Die Liste der LAUFENDEN Fassung muss dem Katalog gleichen; aus
    /// JEDER Liste muss jeder Schlüssel noch lebendig oder Alias sein (5.6). <b>Neu einfrieren</b>
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
            Assert.Equal(1, Vorlagenfeldkatalog.KATALOGFASSUNG);
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

        [Fact]
        public void Katalog_v1_zaehlt_27_handgepflegte_und_je_Kennzahl_drei_erzeugte_Eintraege()
        {
            int kennzahlen = KennzahlenKatalog.Alle().Count;
            Assert.Equal(44, kennzahlen);
            Assert.Equal(27, Vorlagenfeldkatalog.Alle.Count(f => f.Handgepflegt));
            Assert.Equal(3 * kennzahlen, Vorlagenfeldkatalog.Alle.Count(f => !f.Handgepflegt));

            var bereiche = Vorlagenfeldkatalog.Alle.GroupBy(f => f.Schluessel.Split('.')[0])
                .ToDictionary(g => g.Key, g => g.Count());
            _ausgabe.WriteLine(string.Join(", ", bereiche.Select(b => b.Key + " " + b.Value)));
            Assert.Equal(9, bereiche["bericht"]);
            Assert.Equal(7, bereiche["text"]);
            Assert.Equal(3, bereiche["ersteller"]);
            Assert.Equal(8, bereiche["projekt"]);
            Assert.Equal(kennzahlen, bereiche["stamm"]);
            Assert.Equal(2 * kennzahlen, bereiche["kennzahl"]);
        }

        [Fact]
        public void Bericht_inhalt_ist_der_Sammelanker_ueber_alle_Bausteine()
        {
            Vorlagenfeld inhalt = Vorlagenfeldkatalog.Finde("bericht.inhalt");
            Assert.Equal(Vorlagenfeldart.Kapitel, inhalt.Art);
            Assert.Equal(Vorlagenausgabe.Word, inhalt.Ausgaben);
            Assert.Equal(BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel), inhalt.Deckt);
            Assert.Single(Vorlagenfeldkatalog.Alle, f => f.Art == Vorlagenfeldart.Kapitel);
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

        [Fact]
        public void Nur_die_Kaeltestunden_brauchen_die_Zeitreihen()
        {
            List<string> mitBedarf = Vorlagenfeldkatalog.Alle.Where(f => f.Bedarf != Vorlagenbedarf.Keiner)
                .Select(f => f.Schluessel).ToList();
            Assert.Equal(new[] { "stamm.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN }, mitBedarf);
            Assert.Equal(Vorlagenbedarf.Zeitreihen, Vorlagenfeldkatalog.Finde(mitBedarf[0]).Bedarf);
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
            Assert.Equal(3, Vorlagenfeldkatalog.Musterschluessel.Count);
            Assert.Equal(15, Vorlagenfeldkatalog.Textschluessel.Count);
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
                if (!f.Handgepflegt)
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

        [Fact]
        public void Die_Liste_der_laufenden_Fassung_gleicht_dem_Katalog()
        {
            int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG;
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
