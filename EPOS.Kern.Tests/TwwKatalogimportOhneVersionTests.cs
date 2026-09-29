using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Katalogversion eines Katalogpakets ist wahlfrei</b>
    /// (<c>EPOS.Kern/Controller/TwwNutzungsartCtrl.Import.cs</c>,
    /// <c>ZapfprofilCtrl.Zielkatalogversion</c>): Führt ein Paket in KEINER seiner Kopfdateien die
    /// Spalte <c>Katalogversion</c>, treten alle seine Zeilen der Katalogversion des Zielkatalogs
    /// bei — der, die der Parametersatz liest. Führt es sie in ALLEN, gilt sie je Zeile wie
    /// bisher; führt es sie nur in einem TEIL, ist das Paket benannt abgelehnt.
    ///
    /// <para>Die Fälle messen am erfundenen Probepaket unter
    /// <c>Proben/Zapfprofil/Katalogpaket/</c> (runde Zahlen, keine Normzahl) und am ECHTEN freien
    /// Paketteil <c>Referenzlaeufe/Katalogpaket_frei/</c>, der keine Katalogversion führt — er ist
    /// der Anlass dieser Regel und wird gegen eine Arbeitskopie der Testdatenbank importiert.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogimportOhneVersionTests
    {
        private const string RUECKFALL = ZapfprofilCtrl.KATALOGVERSION_RUECKFALL;

        // =================================================================================
        // Regel 1: die Katalogversion des Zielkatalogs
        // =================================================================================

        /// <summary>Ohne die Parametertabelle gibt es keine Version, der beizutreten wäre — der Rückfall.</summary>
        [Fact]
        public void Zielkatalogversion_ohne_Parametertabelle_ist_der_Rueckfall()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal(RUECKFALL, ZapfprofilCtrl.Zielkatalogversion());
        }

        /// <summary>Ein leerer Katalog bekommt mit den ersten Zeilen seine erste Katalogversion.</summary>
        [Fact]
        public void Zielkatalogversion_im_leeren_Katalog_ist_der_Rueckfall()
        {
            using var db = new TwwTestdatenbank();
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal(RUECKFALL, ZapfprofilCtrl.Zielkatalogversion());
        }

        /// <summary>
        /// Führt der Katalog MEHRERE Versionen, gilt die der zuletzt angelegten Parameterzeile —
        /// genau die, die <see cref="ZapfprofilCtrl.Parameter()"/> liest. Die Regel ist an den
        /// Lesepfad gebunden, nicht an die Textform der Version (kein „V10" gegen „V9").
        /// </summary>
        [Fact]
        public void Zielkatalogversion_bei_mehreren_Versionen_ist_die_des_Parametersatzes()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Anzeigetemperatur", 45, "ALT-9", "°C");
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Anzeigetemperatur", 40, "NEU-1", "°C");

            Assert.Equal("NEU-1", ZapfprofilCtrl.Zielkatalogversion());
            Assert.Equal("NEU-1", ZapfprofilCtrl.Parameter().Katalogversion);
        }

        /// <summary>Im laufenden Schreibvorgang gilt derselbe Stand wie außerhalb.</summary>
        [Fact]
        public void Zielkatalogversion_im_Vorgang_liest_denselben_Stand()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Anzeigetemperatur", 45, "IM-VORGANG-1", "°C");

            using DbVorgang v = DataRepository.Vorgang();
            Assert.Equal("IM-VORGANG-1", ZapfprofilCtrl.Zielkatalogversion(v));
        }

        // =================================================================================
        // Der Katalogimport: Paket ohne, mit und mit gemischter Katalogversion
        // =================================================================================

        /// <summary>
        /// Ein Paket OHNE die Spalte in allen vier Kopfdateien: Jede Zeile trägt danach die
        /// Katalogversion des Zielkatalogs, und ein Hinweis nennt sie. Der Parametersatz liest die
        /// eingespielten Parameter — das ist der Sinn der Regel.
        /// </summary>
        [Fact]
        public void Ein_Paket_ohne_Katalogversion_tritt_der_Version_des_Katalogs_bei()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Stundenschwelle", 0.1, "KAT-7", "kW");

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(OhneVersionsspalte(Paket()));

            Assert.Null(b.Abbruch);
            Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION" && h.Nennt("KAT-7"));
            Assert.All(b.Zeilen, z => Assert.Equal("KAT-7", z.Katalogversion));

            foreach (string t in new[] { TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
                                         TwwSchema.TAB_TWW_BEDARFSTAG_STAMM })
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + t + " WHERE Katalogversion <> ?", new DbParam("@k", "KAT-7")));

            // Der Nachweis am Lesepfad: Der Parametersatz der Stochastik sieht die neuen Werte.
            Parametersatz p = ZapfprofilCtrl.Parameter();
            Assert.Equal("KAT-7", p.Katalogversion);
            Assert.Equal(40.0, p.Wert("Zapfprofil.Anzeigetemperatur"));
        }

        /// <summary>Am leeren Katalog bekommt das Paket ohne Version die erste Katalogversion.</summary>
        [Fact]
        public void Ein_Paket_ohne_Katalogversion_am_leeren_Katalog_bekommt_den_Rueckfall()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(OhneVersionsspalte(Paket()));

            Assert.Null(b.Abbruch);
            Assert.All(b.Zeilen, z => Assert.Equal(RUECKFALL, z.Katalogversion));
            Assert.Equal(RUECKFALL, ZapfprofilCtrl.Parameter().Katalogversion);
        }

        /// <summary>Ein Paket MIT der Spalte bleibt, wie es war: seine Version je Zeile, kein Hinweis.</summary>
        [Fact]
        public void Ein_Paket_mit_Katalogversion_behaelt_seine_Version()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Stundenschwelle", 0.1, "KAT-7", "kW");

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket());

            Assert.Null(b.Abbruch);
            Assert.DoesNotContain(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION");
            Assert.All(b.Zeilen, z => Assert.Equal("PROBE-1", z.Katalogversion));
            // Der Katalog führt danach zwei Versionen — die des Katalogs bleibt, wie sie war.
            Assert.Equal(2L, Zahl("SELECT COUNT(DISTINCT Katalogversion) FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM));
        }

        /// <summary>
        /// Nur EIN TEIL der Kopfdateien führt die Spalte: benannt abgelehnt, mit beiden
        /// Dateilisten im Grund — und NICHTS geschrieben.
        /// </summary>
        [Fact]
        public void Ein_Paket_mit_gemischter_Katalogversion_ist_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> paket = Paket()
                .Select(d => string.Equals(d.Name, TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv", StringComparison.OrdinalIgnoreCase)
                             ? d with { Inhalt = SpalteEntfernen(d.Inhalt, "Katalogversion") } : d)
                .ToList();

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(paket);

            Assert.NotNull(b.Abbruch);
            Assert.Equal("KATALOGIMPORT_VERSION_GEMISCHT", b.Abbruch.Kennung);
            Assert.True(b.Abbruch.Nennt(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"),
                        "Der Grund nennt die Dateien MIT der Spalte: " + b.Abbruch.Klartext);
            Assert.True(b.Abbruch.Nennt(TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv"),
                        "Der Grund nennt die Dateien OHNE die Spalte: " + b.Abbruch.Klartext);
            Assert.Empty(b.Zeilen);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM));
        }

        /// <summary>Der Prüflauf eines Pakets ohne Version nennt die Zielversion und schreibt nichts.</summary>
        [Fact]
        public void Der_Pruefmodus_nennt_die_Zielversion_und_schreibt_nichts()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Stundenschwelle", 0.1, "KAT-7", "kW");

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(OhneVersionsspalte(Paket()), pruefen: true);

            Assert.Null(b.Abbruch);
            Assert.True(b.Pruefmodus);
            Assert.All(b.Zeilen, z => Assert.Equal("KAT-7", z.Katalogversion));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM));
        }

        // =================================================================================
        // Der echte freie Paketteil gegen die Testdatenbank
        // =================================================================================

        /// <summary>
        /// <b>Der Anlass der Regel:</b> Der Ordner <c>Referenzlaeufe/Katalogpaket_frei/</c> —
        /// unverändert, als Dateiwahl im Ordner — geht durch den Katalogimport. Die Testdatenbank
        /// trägt seine Zeilen schon (das Saatskript spielt sie in der Version <c>TEST-1</c> ein),
        /// also ist NICHTS angelegt und NICHTS ersetzt: jede Zeile übersprungen.
        /// </summary>
        [Fact]
        public void Der_freie_Paketteil_geht_gegen_die_Testdatenbank_ohne_Fehler_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = Pfad("Referenzlaeufe", "Katalogpaket_frei");
            Assert.True(Directory.Exists(ordner), "Der freie Paketteil fehlt: " + ordner);

            // Gewählt wird EINE Datei des Ordners - so wählt der Anwender im Dialog.
            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(
                Path.Combine(ordner, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"), out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(7, dateien.Count);

            string ziel = ZapfprofilCtrl.Zielkatalogversion();
            string vorher = Abbild();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);

            Assert.Null(b.Abbruch);
            Assert.Equal(0, b.Abgelehnt);
            Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION" && h.Nennt(ziel));
            Assert.All(b.Zeilen, z => Assert.Equal(ziel, z.Katalogversion));
            // Jede Zeile steht schon so im Katalog - nichts angelegt, nichts ersetzt.
            Assert.Equal(6, b.ZeilenVon(TwwImportbereich.Nutzungsart).Count);
            Assert.Equal(9, b.ZeilenVon(TwwImportbereich.Bedarfstag).Count);
            Assert.Equal(42, b.ZeilenVon(TwwImportbereich.Parameter).Count);
            Assert.Equal(0, b.Angelegt);
            Assert.Equal(0, b.Ersetzt);
            Assert.Equal(b.Zeilen.Count, b.Uebersprungen);

            // Der Zellvergleich: JEDE Zelle jeder Tww-Katalogtabelle steht danach, wie sie stand.
            Assert.Equal(vorher, Abbild());
        }

        /// <summary>
        /// Derselbe Ordner als ZIP-Archiv — derselbe Bericht. Das Archiv entsteht im Temp-Ordner
        /// und wird danach entfernt; das Repositorium bleibt unberührt.
        /// </summary>
        [Fact]
        public void Der_freie_Paketteil_geht_auch_als_ZIP_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = Pfad("Referenzlaeufe", "Katalogpaket_frei");
            string archiv = Path.Combine(Path.GetTempPath(),
                                         "epos-paketteil-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip");
            try
            {
                using (var zip = ZipFile.Open(archiv, ZipArchiveMode.Create))
                    foreach (string d in Directory.GetFiles(ordner, "*.csv").OrderBy(x => x, StringComparer.Ordinal))
                        zip.CreateEntryFromFile(d, Path.GetFileName(d));

                IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(archiv, out ZapfSatz fehler);
                Assert.Null(fehler);
                TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);

                Assert.Null(b.Abbruch);
                Assert.Equal(0, b.Abgelehnt);
                Assert.Equal(b.Zeilen.Count, b.Uebersprungen);
            }
            finally
            {
                try { File.Delete(archiv); } catch (IOException) { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>
        /// Derselbe Ordner an einem Katalog OHNE die Paketzeilen: Jede Zeile wird angelegt, in der
        /// Katalogversion des Zielkatalogs, und der Parametersatz liest danach die eingespielten
        /// Parameter der Stochastik. Der Katalog entsteht leer (<see cref="TwwTestdatenbank"/>) —
        /// die Testdatenbank im Repositorium wird nie verändert.
        /// </summary>
        [Fact]
        public void Der_freie_Paketteil_legt_an_einem_Katalog_ohne_seine_Zeilen_alles_an()
        {
            using var db = new TwwTestdatenbank();
            string ordner = Pfad("Referenzlaeufe", "Katalogpaket_frei");
            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(ordner, out ZapfSatz fehler);
            Assert.Null(fehler);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);

            Assert.Null(b.Abbruch);
            Assert.Equal(0, b.Abgelehnt);
            Assert.Equal(0, b.Uebersprungen);
            Assert.Equal(b.Zeilen.Count, b.Angelegt);
            Assert.All(b.Zeilen, z => Assert.Equal(RUECKFALL, z.Katalogversion));

            // Die sechs Nutzungsarten, die neun Ecodesign-Bedarfstage und die 42 Parameter.
            Assert.Equal(6, b.ZeilenVon(TwwImportbereich.Nutzungsart).Count);
            Assert.Equal(9, b.ZeilenVon(TwwImportbereich.Bedarfstag).Count);
            Assert.Equal(42, b.ZeilenVon(TwwImportbereich.Parameter).Count);
            Assert.Equal(5L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
            Assert.Equal(20L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM));
            Assert.Equal(161L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM));

            // Jede Nutzungsart bekommt den Vorgabesatz IHRER Gruppe (Wohnen 4, Nichtwohnen 2).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                  " WHERE ID NOT IN (SELECT ID_Nutzungsart FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ")"));

            // Der Nachweis am Lesepfad: Stochastik und Speicherauslegung lesen die neuen Werte.
            Parametersatz p = ZapfprofilCtrl.Parameter();
            Assert.Equal(RUECKFALL, p.Katalogversion);
            Assert.Equal(14.0, p.Wert("Zapfprofil.Stochastik.Urlaubsversatz"));
            Assert.Equal(60.0, p.Wert("Speicherauslegung.Speichertemperatur_Vorgabe"));
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static List<TwwPaketdatei> Paket()
        {
            IReadOnlyList<TwwPaketdatei> d = TwwNutzungsartCtrl.PaketLesen(
                Pfad("EPOS.Kern.Tests", "Proben", "Zapfprofil", "Katalogpaket"), out ZapfSatz fehler);
            Assert.Null(fehler);
            return d.ToList();
        }

        /// <summary>Das Paket ohne die Spalte <c>Katalogversion</c> in jeder seiner Dateien.</summary>
        private static List<TwwPaketdatei> OhneVersionsspalte(IEnumerable<TwwPaketdatei> paket)
            => paket.Select(d => d with { Inhalt = SpalteEntfernen(d.Inhalt, "Katalogversion") }).ToList();

        /// <summary>
        /// Eine Spalte samt ihrer Felder aus einer CSV-Datei nehmen (Trenner <c>;</c>, kein Feld
        /// des Probepakets trägt Anführungszeichen). Führt die Datei sie nicht, bleibt sie, wie sie ist.
        /// </summary>
        private static string SpalteEntfernen(string inhalt, string spalte)
        {
            string[] zeilen = System.Text.RegularExpressions.Regex.Split(inhalt ?? "", "\r\n|\r|\n");
            int i = Array.IndexOf(zeilen[0].Split(';'), spalte);
            if (i < 0) return inhalt;
            for (int z = 0; z < zeilen.Length; z++)
            {
                if (zeilen[z].Length == 0) continue;
                List<string> felder = zeilen[z].Split(';').ToList();
                if (i < felder.Count) felder.RemoveAt(i);
                zeilen[z] = string.Join(";", felder);
            }
            return string.Join("\r\n", zeilen);
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        /// <summary>
        /// Jede Zelle der sieben Tww-Katalogtabellen als Text, nach <c>ID</c> geordnet — der
        /// Zellvergleich vorher/nachher (Muster <c>TwwKatalogWacheTests.Abbild</c>).
        /// </summary>
        private static string Abbild()
        {
            var sb = new System.Text.StringBuilder();
            foreach (string t in TwwNutzungsartCtrl.IMPORT_TABELLEN)
            {
                System.Data.DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + t + " ORDER BY ID");
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    sb.Append(t);
                    foreach (System.Data.DataColumn s in dt.Columns)
                        sb.Append('|').Append(r[s] == DBNull.Value
                            ? "NULL" : Convert.ToString(r[s], CultureInfo.InvariantCulture));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
