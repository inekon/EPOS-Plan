using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Katalogimport der Brauchwasser-Nutzungsarten</b>
    /// (<c>EPOS.Kern/Controller/TwwNutzungsartCtrl.Import.cs</c>; Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, Kapitel 6 (b), N2, N12 (m)/(p); Stufe Z4, Gruppe 3): ein Paket im
    /// Format N2 mit Tagesgangsatz, Tagesgängen, Nutzungsarten und Zapfkategorien wird als
    /// Anwenderzeilen (Status <c>IMPORT</c>, Herkunftsart <c>IMPORT</c>, <c>FREI</c>/<c>FIKTIV</c>
    /// bleiben) angelegt; gleicher Inhalt wird übersprungen, abweichender kommt als „(Import n)",
    /// eine Auslieferung gleichen Namens bleibt stehen, die Katalogsperre bleibt unberührt; ein
    /// Formfehler lehnt das Paket als Ganzes ab, ein Regelverstoß nur die Nutzungsart.
    ///
    /// <para>Das Paket liegt erfunden unter <c>Proben/Zapfprofil/Katalogpaket/</c> — runde Zahlen,
    /// keine Normzahl. Jeder Fall legt eine leere Tww-Datenbank an (<see cref="TwwTestdatenbank"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogimportTests
    {
        private const string A = "Probenutzung A (erfunden)";
        private const string B = "Probenutzung B (erfunden)";
        private const string VERSION = "PROBE-1";

        private static string Paketordner() => Path.Combine(ZapfZufallTests.Probenordner(), "Katalogpaket");

        /// <summary>Das Probepaket als Dateien — über den Leseweg des Kerns (Ordner).</summary>
        /// <remarks>Die Probendateien liegen im Arbeitsbaum mit dem Zeilenende des Auscheckens
        /// (CRLF unter Windows mit <c>core.autocrlf</c>, LF auf einem Linux-Läufer ohne — beide
        /// über <c>text=auto</c> gültig); die Mutationen der Fälle unten gehen von CRLF aus, also
        /// wird hier einmal auf CRLF vereinheitlicht, unabhängig vom Auscheck-Zeilenende.</remarks>
        private static List<TwwPaketdatei> Paket()
        {
            IReadOnlyList<TwwPaketdatei> d = TwwNutzungsartCtrl.PaketLesen(Paketordner(), out ZapfSatz fehler);
            Assert.Null(fehler);
            return d.Select(x => x with { Inhalt = AufCrLf(x.Inhalt) }).ToList();
        }

        private static string AufCrLf(string text) => text == null ? text : Regex.Replace(text, "\r\n|\r|\n", "\r\n");

        /// <summary>Das Paket mit einer Datei, deren Text <paramref name="aendern"/> umschreibt.</summary>
        private static List<TwwPaketdatei> PaketMit(string tabelle, Func<string, string> aendern)
            => Paket().Select(d => string.Equals(d.Name, tabelle + ".csv", StringComparison.OrdinalIgnoreCase)
                                   ? d with { Inhalt = aendern(d.Inhalt) } : d).ToList();

        /// <summary>
        /// Das Paket mit dem Zeilenende des Falls in jeder Datei: <c>LF</c> (Linux, macOS, iOS),
        /// <c>CRLF</c> (Windows), <c>CR</c> (älteres Excel für Mac) oder <c>gemischt</c> (reihum CRLF,
        /// LF, CR). Auch ein Umbruch in einem Feld in Anführungszeichen bekommt es — so schreibt ihn ein
        /// Editor, so checkt ihn Git aus.
        /// </summary>
        private static List<TwwPaketdatei> MitZeilenende(IEnumerable<TwwPaketdatei> paket, string zeilenende)
        {
            string[] enden;
            switch (zeilenende)
            {
                case "LF": enden = new[] { "\n" }; break;
                case "CRLF": enden = new[] { "\r\n" }; break;
                case "CR": enden = new[] { "\r" }; break;
                default: enden = new[] { "\r\n", "\n", "\r" }; break;
            }
            return paket.Select(d =>
            {
                string[] z = Regex.Split(d.Inhalt, "\r\n|\r|\n");
                var s = new StringBuilder(z[0]);
                for (int i = 1; i < z.Length; i++) s.Append(enden[(i - 1) % enden.Length]).Append(z[i]);
                return d with { Inhalt = s.ToString() };
            }).ToList();
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        private static DataRow Zeile(string tabelle, int id)
            => DataRepository.GetDataTable("SELECT * FROM " + tabelle + " WHERE ID = ?", new DbParam("@id", id)).Rows[0];

        // =================================================================================
        // Anlegen
        // =================================================================================

        [Fact]
        public void Das_Paket_legt_Nutzungsarten_samt_Satz_Tagesgaengen_und_Kategorien_als_Import_an()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket());

            Assert.Null(b.Abbruch);
            Assert.Empty(b.Hinweise);
            Assert.Equal(2, b.Angelegt);
            Assert.Equal(new[] { A, B }, b.Zeilen.Select(z => z.Katalogname).ToArray());
            Assert.All(b.Zeilen, z => Assert.Null(z.Grund));

            foreach (int id in b.NeueIds)
            {
                DataRow n = Zeile(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, id);
                // Anwenderzeile: Status und ReadOnly des Pakets zählen nicht, kein Beleg, keine Vorlage.
                Assert.Equal(TwwSchema.STATUS_IMPORT, n["Status"]);
                Assert.False(Convert.ToBoolean(n["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(DBNull.Value, n["Beleg"]);
                Assert.Equal(DBNull.Value, n["ID_Vorlage"]);
                Assert.Equal(VERSION, n["Katalogversion"]);
                // Herkunftsart IMPORT - FREI und FIKTIV bleiben, Quelle und Version des Pakets bleiben.
                Assert.Equal(TwwSchema.HERKUNFT_IMPORT, n["Bedarf_Herkunftsart"]);
                Assert.Equal(TwwSchema.HERKUNFT_FREI, n["Jahresgang_Herkunftsart"]);
                Assert.Equal(TwwSchema.HERKUNFT_FIKTIV, n["Wochengang_Herkunftsart"]);
                Assert.Equal("Probepaket (erfunden)", n["Bedarf_Quelle"]);
            }

            // EIN Satz für beide Nutzungsarten - mit vier Tagesgängen, Status IMPORT.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE Status = 'IMPORT' AND ReadOnly = 0"));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " WHERE Herkunftsart = 'IMPORT'"));

            Nutzungsart na = TwwNutzungsartCtrl.Lies(b.NeueIds[0]);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, na.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(0.5, na.Tagesgaenge.Anteile[0, 7]);
            Assert.True(na.Tagesgaenge.Vollstaendig);

            // A trägt den Vorgabesatz (ohne ID_Nutzungsart) - FREI bleibt FREI; B seine eigene Kategorie.
            TwwKategorienStand ka = TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[0]);
            Assert.Equal(new[] { "Kurz (erfunden)", "Lang (erfunden)" }, ka.Kategorien.Select(k => k.Name).ToArray());
            Assert.All(ka.Kategorien, k => Assert.Equal(Herkunftsart.Frei, k.Herkunft.Art));
            TwwKategorienStand kb = TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[1]);
            Assert.Equal("Eigen (erfunden)", Assert.Single(kb.Kategorien).Name);
            Assert.Equal(Herkunftsart.Import, kb.Kategorien[0].Herkunft.Art);
            Assert.Equal(20.0, kb.Kategorien[0].KappungLJeMin);
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE Status = 'IMPORT' AND ReadOnly = 0"));

            // Eine importierte Zeile ist eine freie Anwenderzeile: änderbar und löschbar.
            Assert.False(TwwNutzungsartCtrl.IstReadOnly(b.NeueIds[0]));
            Assert.True(TwwNutzungsartCtrl.Loeschen(b.NeueIds[1]).Ok);
        }

        [Fact]
        public void Ein_zweiter_Import_desselben_Pakets_ueberspringt_alles()
        {
            using var db = new TwwTestdatenbank();
            Assert.Equal(2, TwwNutzungsartCtrl.Importieren(Paket()).Angelegt);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket());
            Assert.Null(b.Abbruch);
            Assert.Equal(2, b.Uebersprungen);
            Assert.All(b.Zeilen, z => Assert.Equal("KATALOGIMPORT_GLEICH_VORHANDEN", z.Grund.Kennung));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
        }

        [Fact]
        public void Abweichender_Inhalt_kommt_als_Import_n_und_die_Zeile_gleichen_Namens_bleibt()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht erst = TwwNutzungsartCtrl.Importieren(Paket());
            int alt = erst.NeueIds[0];

            // Bedarf mittel der Nutzungsart A von 2 auf 2.5 - erfunden.
            List<TwwPaketdatei> geaendert = PaketMit(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
                t => t.Replace("10;" + A + ";PROBE-1;1;1;2;3;", "10;" + A + ";PROBE-1;1;1;2.5;3;"));
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(geaendert);

            TwwImportzeile za = b.Zeilen.Single(z => z.Nutzungsart == A);
            Assert.Equal(TwwImportausgang.Angelegt, za.Ausgang);
            Assert.Equal(A + " (Import 1)", za.Katalogname);
            Assert.Equal("KATALOGIMPORT_NEUE_VERSION", za.Grund.Kennung);
            Assert.Equal(TwwImportausgang.Uebersprungen, b.Zeilen.Single(z => z.Nutzungsart == B).Ausgang);
            Assert.Equal(2.0, TwwNutzungsartCtrl.Lies(alt).BedarfJeNiveauKwhJeEinheitTag[1]);
            Assert.Equal(2.5, TwwNutzungsartCtrl.Lies(za.IdNeu).BedarfJeNiveauKwhJeEinheitTag[1]);
            // Der Satz ist gleich geblieben - er wird wiederverwendet, nicht verdoppelt.
            Assert.Equal(TwwNutzungsartCtrl.Lies(alt).Tagesgaenge.Id, TwwNutzungsartCtrl.Lies(za.IdNeu).Tagesgaenge.Id);

            // Ein drittes Mal: die frühere Version „(Import 1)" trägt schon den Inhalt.
            TwwKatalogimportBericht dritt = TwwNutzungsartCtrl.Importieren(geaendert);
            TwwImportzeile z3 = dritt.Zeilen.Single(z => z.Nutzungsart == A);
            Assert.Equal(TwwImportausgang.Uebersprungen, z3.Ausgang);
            Assert.Equal("KATALOGIMPORT_GLEICH_ALS", z3.Grund.Kennung);
            Assert.Equal(A + " (Import 1)", z3.Katalogname);
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
        }

        [Fact]
        public void Ein_abweichender_Tagesgang_bringt_einen_eigenen_Satz_als_Import_n()
        {
            using var db = new TwwTestdatenbank();
            TwwNutzungsartCtrl.Importieren(Paket());

            // Tagtyp 1: die Hälften auf die Stunden 9 und 20 statt 8 und 20.
            List<TwwPaketdatei> geaendert = PaketMit(TwwSchema.TAB_TWW_TAGESGANG_STAMM, t =>
            {
                string[] z = t.Split(new[] { "\r\n" }, StringSplitOptions.None);
                string[] f = z[1].Split(';');
                f[2 + 7] = "0";
                f[2 + 8] = "0.5";
                z[1] = string.Join(";", f);
                return string.Join("\r\n", z);
            });
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(geaendert);
            Assert.Null(b.Abbruch);
            Assert.Equal(2, b.Angelegt);
            Assert.All(b.Zeilen, z => Assert.EndsWith(" (Import 1)", z.Katalogname));
            DataTable s = DataRepository.GetDataTable("SELECT Bezeichner FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " ORDER BY ID");
            Assert.Equal(new[] { "Probesatz (erfunden)", "Probesatz (erfunden) (Import 1)" },
                         s.Rows.Cast<DataRow>().Select(r => (string)r[0]).ToArray());
            Assert.Equal(0.5, TwwNutzungsartCtrl.Lies(b.NeueIds[0]).Tagesgaenge.Anteile[0, 8]);
        }

        [Fact]
        public void Eine_Auslieferung_gleichen_Namens_bleibt_stehen_und_die_Katalogsperre_unberuehrt()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz G", VERSION);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen(A, VERSION, satz, status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen(B, VERSION, satz);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone 1", 10.0);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket());
            TwwImportzeile za = b.Zeilen.Single(z => z.Nutzungsart == A);
            Assert.Equal(TwwImportausgang.Uebersprungen, za.Ausgang);
            Assert.Equal("KATALOGIMPORT_AUSLIEFERUNG", za.Grund.Kennung);

            // Die benutzte Zeile gleichen Namens weicht ab: Sie bleibt, wie sie ist, der Import kommt als „(Import 1)".
            TwwImportzeile zb = b.Zeilen.Single(z => z.Nutzungsart == B);
            Assert.Equal(TwwImportausgang.Angelegt, zb.Ausgang);
            Assert.Equal(B + " (Import 1)", zb.Katalogname);
            Assert.True(TwwNutzungsartCtrl.IstReadOnly(geliefert));
            Assert.True(TwwNutzungsartCtrl.IstBenutzt(benutzt));
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, TwwNutzungsartCtrl.Lies(benutzt).BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(TwwSchema.STATUS_AUSLIEFERUNG, Zeile(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, geliefert)["Status"]);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Nutzungsart = ?", new DbParam("@n", benutzt)));
        }

        // =================================================================================
        // Ablehnung des Pakets (nichts geändert)
        // =================================================================================

        [Theory]
        [InlineData("Unbekannt", "KATALOGIMPORT_SPALTE_UNBEKANNT")]
        [InlineData("Zahl", "KATALOGIMPORT_ZAHL")]
        [InlineData("Feldzahl", "KATALOGIMPORT_FELDZAHL")]
        [InlineData("Spalte", "KATALOGIMPORT_SPALTE_FEHLT")]
        [InlineData("Id", "KATALOGIMPORT_ID_DOPPELT")]
        public void Ein_Formfehler_lehnt_das_Paket_ab_und_nennt_Datei_und_Zeile(string fall, string kennung)
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, t =>
            {
                switch (fall)
                {
                    case "Unbekannt": return t.Replace(";ReadOnly\r\n", ";Farbe\r\n");
                    case "Zahl": return t.Replace(";PROBE-1;1;1;2;3;", ";PROBE-1;1;1;2,5;3;");
                    case "Feldzahl": return t.Replace(";AUSLIEFERUNG;1\r\n11;", ";AUSLIEFERUNG;1;X\r\n11;");
                    case "Spalte": return t.Replace(";Bezug_Kaltwasser;", ";Bezug_Kalt;").Replace(";Bezug_Kalt;", ";Beleg;");
                    default: return t.Replace("\r\n11;", "\r\n10;");
                }
            });

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.NotNull(b.Abbruch);
            Assert.Equal(kennung, b.Abbruch.Kennung);
            Assert.Contains(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv", b.Abbruch.Klartext);
            Assert.Contains("nichts importiert", b.Abbruch.Klartext);
            Assert.Empty(b.Zeilen);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
        }

        [Fact]
        public void Ohne_Datei_der_Nutzungsarten_ist_das_Paket_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = Paket().Where(d => !d.Name.StartsWith(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, StringComparison.Ordinal)).ToList();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Equal("KATALOGIMPORT_KEINE_DATEI", b.Abbruch.Kennung);

            // Der freie Paketteil taugt nicht als Katalogpaket: Seine Zeilen treten der Katalogversion des
            // Katalogs bei, den sie erreichen, und führen deshalb keine (Regel 2 seiner LIESMICH.md) —
            // der Katalogimport verlangt sie. Benannt abgelehnt, nichts geändert; die Dateien, die kein
            // Katalogimport kennt (Parameter, Bedarfstag), sind benannt übergangen.
            string frei = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ZapfZufallTests.Probenordner()))),
                                       "Referenzlaeufe", "Katalogpaket_frei");
            IReadOnlyList<TwwPaketdatei> teil = TwwNutzungsartCtrl.PaketLesen(frei, out ZapfSatz fehler);
            Assert.Null(fehler);
            TwwKatalogimportBericht bf = TwwNutzungsartCtrl.Importieren(teil);
            Assert.Equal("KATALOGIMPORT_SPALTE_FEHLT", bf.Abbruch.Kennung);
            Assert.Equal(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv", bf.Abbruch.Werte[0]);
            Assert.Equal("Katalogversion", bf.Abbruch.Werte[1]);
            Assert.Contains(bf.Hinweise, h => h.Kennung == "KATALOGIMPORT_DATEI_UEBERGANGEN"
                                               && h.Werte[0].Equals(TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
        }

        [Fact]
        public void Ohne_Tabellen_und_bei_unlesbarer_Datei_ist_der_Import_benannt_abgelehnt()
        {
            using (var leer = new TwwTestdatenbank(mitTwwSchema: false))
                Assert.Equal("KATALOGIMPORT_TABELLEN_FEHLEN", TwwNutzungsartCtrl.Importieren(Paket()).Abbruch.Kennung);

            IReadOnlyList<TwwPaketdatei> d = TwwNutzungsartCtrl.PaketLesen(
                Path.Combine(Path.GetTempPath(), "epos-gibt-es-nicht-" + Guid.NewGuid().ToString("N") + ".zip"), out ZapfSatz fehler);
            Assert.Empty(d);
            Assert.Equal("KATALOGIMPORT_DATEI_UNLESBAR", fehler.Kennung);
        }

        // =================================================================================
        // Ablehnung einer Nutzungsart (die übrigen kommen)
        // =================================================================================

        [Theory]
        [InlineData("Satz", "KATALOGIMPORT_SATZ_FEHLT")]
        [InlineData("Raster", "KATALOGIMPORT_RASTER")]
        [InlineData("Herkunft", "KATALOGIMPORT_WERT_UNGUELTIG")]
        [InlineData("Bezugsart", "KATALOGIMPORT_WERT_UNGUELTIG")]
        [InlineData("Pflicht", "KATALOGIMPORT_PFLICHT_FEHLT")]
        [InlineData("Kategorie", "KATEGORIE_DAUER")]
        public void Ein_Regelverstoss_lehnt_nur_die_Nutzungsart_ab(string fall, string kennung)
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = fall == "Kategorie"
                ? PaketMit(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, t => t.Replace("11;Eigen (erfunden);1;5;5;", "11;Eigen (erfunden);1;5;0;"))
                : PaketMit(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, t =>
                {
                    string[] z = t.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string zeileB = z[2];
                    switch (fall)
                    {
                        case "Satz": zeileB = zeileB.Replace(";FIKTIV;1;AUSLIEFERUNG;1", ";FIKTIV;7;AUSLIEFERUNG;1"); break;
                        case "Raster": zeileB = zeileB.Replace(";0.2;0.2;0.2;0.2;0.2;0;0;", ";0.2;0.2;0.2;0.2;0.2;0.2;0;"); break;
                        case "Herkunft": zeileB = zeileB.Replace(";PROBE-1;VERFAHREN;", ";PROBE-1;NORMWERT;"); break;
                        case "Bezugsart": zeileB = zeileB.Replace(";PROBE-1;6;", ";PROBE-1;9;"); break;
                        default: zeileB = zeileB.Replace(";60;10;1;2;", ";60;;1;2;"); break;
                    }
                    z[2] = zeileB;
                    return string.Join("\r\n", z);
                });

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            TwwImportzeile zb = b.Zeilen.Single(z => z.Nutzungsart == B);
            Assert.Equal(TwwImportausgang.Abgelehnt, zb.Ausgang);
            Assert.Equal(kennung, zb.Grund.Kennung);
            Assert.Equal(0, zb.IdNeu);
            Assert.Equal(3, zb.Zeile);
            Assert.Equal(TwwImportausgang.Angelegt, b.Zeilen.Single(z => z.Nutzungsart == A).Ausgang);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
        }

        [Fact]
        public void Ein_unvollstaendiger_Satz_und_ein_falscher_Tagesgang_lehnen_ihre_Nutzungsarten_ab()
        {
            using var db = new TwwTestdatenbank();
            // Tagtyp 4 fehlt.
            List<TwwPaketdatei> ohne4 = PaketMit(TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                t => string.Join("\r\n", t.Split(new[] { "\r\n" }, StringSplitOptions.None).Where(z => !z.StartsWith("1;4;", StringComparison.Ordinal))));
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(ohne4);
            Assert.All(b.Zeilen, z => Assert.Equal("KATALOGIMPORT_SATZ_UNVOLLSTAENDIG", z.Grund.Kennung));
            Assert.Equal(0, b.Angelegt);

            // Tagtyp 2 summiert zu 1,5.
            List<TwwPaketdatei> falsch = PaketMit(TwwSchema.TAB_TWW_TAGESGANG_STAMM, t =>
            {
                string[] z = t.Split(new[] { "\r\n" }, StringSplitOptions.None);
                string[] f = z[2].Split(';');
                f[2] = "0.5";
                z[2] = string.Join(";", f);
                return string.Join("\r\n", z);
            });
            TwwKatalogimportBericht bf = TwwNutzungsartCtrl.Importieren(falsch);
            Assert.All(bf.Zeilen, z => Assert.Equal("KATALOGIMPORT_TAGESGANG", z.Grund.Kennung));
            Assert.Equal(2, bf.Zeilen[0].Grund.Werte[1]);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
        }

        [Fact]
        public void Doppelte_Nutzungsart_im_Paket_kommt_einmal()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, t => t.Replace("11;" + B, "11;" + A));
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Equal(TwwImportausgang.Angelegt, b.Zeilen[0].Ausgang);
            Assert.Equal(TwwImportausgang.Abgelehnt, b.Zeilen[1].Ausgang);
            Assert.Equal("KATALOGIMPORT_DOPPELT_IM_PAKET", b.Zeilen[1].Grund.Kennung);
        }

        // =================================================================================
        // Hinweise, Lesewege, Schemastand
        // =================================================================================

        [Fact]
        public void Fremde_Dateien_und_Kategorien_ohne_Nutzungsart_sind_benannt_uebergangen()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
                t => t + "99;Waise (erfunden);1;1;1;1;0;;Probepaket (erfunden);;PROBE-1;FREI;EIGEN;0\r\n");
            p.Add(new TwwPaketdatei(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ".csv", "ID;Bezeichner\r\n1;x\r\n"));
            p.Add(new TwwPaketdatei("notizen.csv", "a;b\r\n"));

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.Equal(2, b.Angelegt);
            Assert.Equal(new[] { "KATALOGIMPORT_DATEI_UEBERGANGEN", "KATALOGIMPORT_DATEI_UEBERGANGEN", "KATALOGIMPORT_KATEGORIEN_UEBERGANGEN" },
                         b.Hinweise.Select(h => h.Kennung).OrderBy(k => k, StringComparer.Ordinal).ToArray());
            Assert.Equal(1, b.Hinweise.Single(h => h.Kennung == "KATALOGIMPORT_KATEGORIEN_UEBERGANGEN").Werte[0]);
        }

        /// <summary>
        /// <b>Der freie Paketteil als Katalogpaket</b> (Nachbesserung Gruppe 2): Seine Kategoriedatei
        /// führt ZWEI Vorgabesätze, getrennt allein durch die Steuerspalte <c>Gruppe</c>. Gebunden wird
        /// je Nutzungsart der Satz IHRER Gruppe (<see cref="TwwSchema.Kategoriengruppe"/> aus der
        /// Kalenderart): Die Wohn-Nutzungsart des Probepakets bekommt die vier Wohnkategorien, die
        /// Nichtwohn-Nutzungsart die zwei Nichtwohnkategorien. Würden beide Sätze zusammen gebunden,
        /// stände „Kurzzapfung" zweimal da und jede Zeile wäre mit
        /// <c>KATEGORIE_NAME_DOPPELT</c> abgelehnt.
        /// </summary>
        [Fact]
        public void Der_Paketteil_als_Katalogpaket_bindet_den_Vorgabesatz_je_Gruppe()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, _ => FreieKategorien());

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.All(b.Zeilen, z => Assert.Null(z.Grund));
            Assert.Equal(2, b.Angelegt);

            // A ist Kalenderart 1 (Wohnen), B Kalenderart 2 (Nichtwohnen).
            Assert.Equal(new[] { "Kurzzapfung", "Mittlere Zapfung", "Wannenbad", "Dusche" },
                         TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[0]).Kategorien.Select(k => k.Name).ToArray());
            Assert.Equal(new[] { "Kurzzapfung", "Duschzapfung" },
                         TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[1]).Kategorien.Select(k => k.Name).ToArray());
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
        }

        /// <summary>
        /// Führt der Paketteil den Vorgabesatz einer Gruppe NICHT, ist das eine benannte Ablehnung der
        /// Nutzungsart dieser Gruppe — nicht der stille Satz der anderen Gruppe.
        /// </summary>
        [Fact]
        public void Ohne_Vorgabesatz_seiner_Gruppe_ist_die_Nutzungsart_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            string[] zeilen = FreieKategorien().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string nurWohnen = string.Join("\r\n",
                zeilen.Where(z => z == zeilen[0] || z.StartsWith(TwwSchema.KATEGORIENGRUPPE_WOHNEN + ";", StringComparison.Ordinal))) + "\r\n";
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, _ => nurWohnen);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.Equal(1, b.Angelegt);
            Assert.Equal(1, b.Abgelehnt);
            TwwImportzeile abgelehnt = b.Zeilen.Single(z => z.Ausgang == TwwImportausgang.Abgelehnt);
            Assert.Equal(B, abgelehnt.Nutzungsart);
            Assert.Equal("KATALOGIMPORT_VORGABESATZ_GRUPPE", abgelehnt.Grund.Kennung);
            Assert.Equal(TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN, abgelehnt.Grund.Werte[0]);
        }

        /// <summary>
        /// Führt das Paket ÜBERHAUPT keinen Vorgabesatz (die Kategoriedatei trägt nur ihre Kopfzeile),
        /// bleiben die Nutzungsarten ohne Zapfkategorien — sie rechnen dann ohne Streuung. Das wird
        /// benannt, nicht still übergangen.
        /// </summary>
        [Fact]
        public void Ein_Paket_ohne_Vorgabesatz_nennt_die_Nutzungsarten_ohne_Kategorien()
        {
            using var db = new TwwTestdatenbank();
            string kopf = FreieKategorien().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0] + "\r\n";
            List<TwwPaketdatei> p = PaketMit(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, _ => kopf);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.All(b.Zeilen, z => Assert.Null(z.Grund));
            Assert.Equal(2, b.Angelegt);
            ZapfSatz hinweis = b.Hinweise.Single(h => h.Kennung == "KATALOGIMPORT_OHNE_VORGABESATZ");
            Assert.Equal(2, hinweis.Werte[0]);
            Assert.Empty(TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[0]).Kategorien);
        }

        /// <summary>Die Kategoriedatei des freien Paketteils im Arbeitsbaum, auf CRLF vereinheitlicht.</summary>
        private static string FreieKategorien()
        {
            string frei = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ZapfZufallTests.Probenordner()))),
                                       "Referenzlaeufe", "Katalogpaket_frei");
            IReadOnlyList<TwwPaketdatei> teil = TwwNutzungsartCtrl.PaketLesen(frei, out ZapfSatz fehler);
            Assert.Null(fehler);
            return AufCrLf(teil.Single(d => string.Equals(d.Name, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ".csv",
                                                          StringComparison.OrdinalIgnoreCase)).Inhalt);
        }

        [Fact]
        public void Ohne_Tabelle_der_Zapfkategorien_kommen_die_Nutzungsarten_ohne_sie()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            TwwTestdatenbank.SchemaAnlegen(mitT2: false);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket());
            Assert.Null(b.Abbruch);
            Assert.Equal(2, b.Angelegt);
            Assert.Equal("KATALOGIMPORT_KATEGORIEN_OHNE_TABELLE", Assert.Single(b.Hinweise).Kennung);
        }

        [Fact]
        public void Zip_Ordner_und_eine_Datei_des_Ordners_lesen_dasselbe_Paket()
        {
            List<TwwPaketdatei> ordner = Paket();
            Assert.Equal(4, ordner.Count);

            IReadOnlyList<TwwPaketdatei> eine = TwwNutzungsartCtrl.PaketLesen(
                Path.Combine(Paketordner(), TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"), out ZapfSatz f1);
            Assert.Null(f1);
            Assert.Equal(ordner.Select(d => d.Name).OrderBy(n => n), eine.Select(d => d.Name).OrderBy(n => n));

            string zip = Path.Combine(Path.GetTempPath(), "epos-twwpaket-" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                    foreach (TwwPaketdatei d in ordner)
                    {
                        ZipArchiveEntry e = a.CreateEntry("paket/" + d.Name);
                        using var w = new StreamWriter(e.Open(), new UTF8Encoding(true));
                        w.Write(d.Inhalt);
                    }
                IReadOnlyList<TwwPaketdatei> ausZip = TwwNutzungsartCtrl.PaketLesen(zip, out ZapfSatz f2);
                Assert.Null(f2);
                Assert.Equal(ordner.Select(d => d.Name).OrderBy(n => n), ausZip.Select(d => d.Name).OrderBy(n => n));

                using var db = new TwwTestdatenbank();
                Assert.Equal(2, TwwNutzungsartCtrl.Importieren(ausZip).Angelegt);
            }
            finally { try { File.Delete(zip); } catch { } }
        }

        /// <summary>
        /// Komma als Trenner, Felder in Anführungszeichen mit Trenner, Anführungszeichen und
        /// Zeilenumbruch — mit jedem Zeilenende. Der Umbruch im Feld kommt als LF an, gleich woher das
        /// Paket stammt; bei CR allein stünde sonst das ganze Paket in der Kopfzeile, und das „;" im
        /// Bezeichner wählte den falschen Trenner.
        /// </summary>
        [Theory]
        [InlineData("LF")]
        [InlineData("CRLF")]
        [InlineData("CR")]
        public void Komma_als_Trenner_und_Felder_in_Anfuehrungszeichen_nach_RFC_4180(string zeilenende)
        {
            using var db = new TwwTestdatenbank();
            const string NAME = "Probe; \"zitiert\" (erfunden)";
            const string QUELLE = "Probepaket\n(erfunden)";
            List<TwwPaketdatei> p = MitZeilenende(Paket().Select(d =>
            {
                // Aus „;" wird „,"; der Bezeichner mit Trenner und Anführungszeichen steht in Anführungszeichen,
                // die Quelle des Bedarfs mit dem Zeilenumbruch auch.
                string t = d.Inhalt.Replace(";", ",");
                if (d.Name.StartsWith(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, StringComparison.Ordinal))
                    t = t.Replace("10," + A + ",PROBE-1,1,1,2,3,1.5,2.5,Probepaket (erfunden),",
                                  "10,\"" + NAME.Replace("\"", "\"\"") + "\",PROBE-1,1,1,2,3,1.5,2.5,\"" + QUELLE + "\",");
                return d with { Inhalt = t };
            }), zeilenende);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.Equal(new[] { NAME, B }, b.Zeilen.Select(z => z.Katalogname).ToArray());
            Assert.Equal(NAME, TwwNutzungsartCtrl.Lies(b.NeueIds[0]).Name);
            Assert.Equal(QUELLE, Zeile(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, b.NeueIds[0])["Bedarf_Quelle"]);
        }

        /// <summary>
        /// Ein Paket aus Windows (CRLF), aus Linux, macOS oder iOS (LF), aus einem älteren Excel für Mac
        /// (CR) oder mit gemischten Zeilenenden liest sich gleich: dieselben Nutzungsarten mit denselben
        /// Werten, dieselben Zeilen im Bericht und beim Formfehler; ein zweiter Import mit anderem
        /// Zeilenende findet alles gleich vorhanden.
        /// </summary>
        [Theory]
        [InlineData("LF")]
        [InlineData("CRLF")]
        [InlineData("CR")]
        [InlineData("gemischt")]
        public void Jedes_Zeilenende_liest_dasselbe_Paket(string zeilenende)
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(MitZeilenende(Paket(), zeilenende));
            Assert.Null(b.Abbruch);
            Assert.Empty(b.Hinweise);
            Assert.Equal(new[] { A, B }, b.Zeilen.Select(z => z.Katalogname).ToArray());
            Assert.Equal(new[] { 2, 3 }, b.Zeilen.Select(z => z.Zeile).ToArray());
            Nutzungsart na = TwwNutzungsartCtrl.Lies(b.NeueIds[0]);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, na.BedarfJeNiveauKwhJeEinheitTag);
            Assert.True(na.Tagesgaenge.Vollstaendig);
            Assert.Equal(0.5, na.Tagesgaenge.Anteile[3, 19]);
            Assert.Equal(new[] { "Kurz (erfunden)", "Lang (erfunden)" },
                         TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[0]).Kategorien.Select(k => k.Name).ToArray());
            Assert.Equal(20.0, Assert.Single(TwwNutzungsartCtrl.KategorienLesen(b.NeueIds[1]).Kategorien).KappungLJeMin);

            TwwKatalogimportBericht zweit = TwwNutzungsartCtrl.Importieren(MitZeilenende(Paket(), zeilenende == "CRLF" ? "LF" : "CRLF"));
            Assert.All(zweit.Zeilen, z => Assert.Equal("KATALOGIMPORT_GLEICH_VORHANDEN", z.Grund?.Kennung));

            ZapfSatz abbruch = TwwNutzungsartCtrl.Importieren(MitZeilenende(
                PaketMit(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, t => t.Replace("\r\n11;", "\r\n10;")), zeilenende)).Abbruch;
            Assert.Equal("KATALOGIMPORT_ID_DOPPELT", abbruch?.Kennung);
            Assert.Equal(3, abbruch.Werte[1]);
        }

        // =================================================================================
        // Die Paketvorlage der A100-Typen (ZU24)
        // =================================================================================

        /// <summary>Der Ordner der Paketvorlage <c>Referenzlaeufe/Katalogpaket_Vorlage_A100</c>.</summary>
        private static string VorlageA100()
            => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ZapfZufallTests.Probenordner()))),
                            "Referenzlaeufe", "Katalogpaket_Vorlage_A100");

        /// <summary>
        /// <b>Die Paketvorlage der A100-Typen liest sich ohne Ablehnung ein</b> (Anwenderentscheid
        /// ZU24): Die vier Dateien des Importformats mit ihrer Beispielzeile legen eine Nutzungsart
        /// samt Tagesgangsatz, vier Tagesgängen und dem Vorgabesatz ihrer Gruppe an — Herkunftsart
        /// <c>IMPORT</c>, Gruppe <c>Nichtwohnen</c> gebunden (Kalenderart 5, Auslastungsgang). So ist
        /// die Vorlage nachweislich einspielbar, bevor der Anwender seine Normwerte einträgt.
        /// </summary>
        [Fact]
        public void Die_Paketvorlage_A100_spielt_ohne_Ablehnung_ein()
        {
            using var db = new TwwTestdatenbank();
            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(VorlageA100(), out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(4, dateien.Count);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien.ToList());
            Assert.Null(b.Abbruch);
            Assert.All(b.Zeilen, z => Assert.Null(z.Grund));
            Assert.Equal(1, b.Angelegt);
            Assert.Equal(0, b.Abgelehnt);
            Assert.Empty(b.Hinweise);

            int id = b.NeueIds[0];
            DataRow z = Zeile(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, id);
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(z["Status"]));
            foreach (string g in new[] { "Bedarf", "Jahresgang", "Wochengang" })
                Assert.Equal(TwwSchema.HERKUNFT_IMPORT, Convert.ToString(z[g + "_Herkunftsart"]));
            Assert.Equal(5L, Convert.ToInt64(z["Kalenderart"], CultureInfo.InvariantCulture));
            Assert.Equal(TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN,
                         TwwSchema.Kategoriengruppe(Convert.ToInt64(z["Kalenderart"], CultureInfo.InvariantCulture)));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM));
            Assert.Equal(new[] { "Kurzzapfung (Vorlage)", "Duschzapfung (Vorlage)" },
                         TwwNutzungsartCtrl.KategorienLesen(id).Kategorien.Select(k => k.Name).ToArray());
        }

        /// <summary>Die Zahlen, die die Paketvorlage A100 bewusst trägt — und nur sie.</summary>
        private static readonly double[] PLATZHALTERZAHLEN =
        {
            0.0, 0.5, 1.0, 2.0, 3.0, 4.0, 5.0, 10.0, 20.0, 30.0, 60.0,
            1.0 / 24.0,                                      // Tagesgang gleichverteilt
            1.0 / 7.0                                        // Woche gleichverteilt
        };

        /// <summary>
        /// Freiliste: die Textfelder der Paketvorlage, die bewusst Ziffern führen. Jeder andere Text
        /// mit einer Ziffer ist ein Fund — so fällt eine in einen Quellentext getippte Tabellen-,
        /// Bild- oder Zahlenangabe auf.
        /// </summary>
        private static readonly string[] PLATZHALTERTEXTE =
        {
            "A100-1",                                        // Katalogversion und Provenienz-Version
            "DIN EN 12831-3 Beiblatt A100, Tabelle …"        // Quelle je Wertgruppe, ohne Tabellennummer
        };

        /// <summary>
        /// Hält einen Ordner im Format der Paketvorlage gegen die zwei Freilisten und gibt je Fund
        /// eine Zeile. Der Trenner kommt wie beim Leser (<c>TwwNutzungsartCtrl.PaketLesen</c>) aus der
        /// Kopfzeile: Semikolon, wenn sie eines führt, sonst Komma. Liefert eine Datei **kein**
        /// Zahlenfeld, ist das selbst ein Fund — sonst wäre ein falsch gewählter Trenner als grüner
        /// Lauf zu lesen.
        /// </summary>
        private static List<string> PlatzhalterFunde(string ordner)
        {
            var funde = new List<string>();
            string[] dateien = Directory.GetFiles(ordner, "*.csv").OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (dateien.Length == 0) { funde.Add(ordner + ": keine CSV-Datei"); return funde; }
            foreach (string datei in dateien)
            {
                string name = Path.GetFileName(datei);
                string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8).Where(x => x.Trim().Length > 0).ToArray();
                if (zeilen.Length < 2) { funde.Add(name + ": keine Beispielzeile"); continue; }
                char trenner = zeilen[0].IndexOf(';') >= 0 ? ';' : ',';
                int zahlenfelder = 0;
                for (int i = 1; i < zeilen.Length; i++)
                    foreach (string feld in zeilen[i].Split(trenner))
                    {
                        string s = feld.Trim();
                        if (s.Length == 0) continue;
                        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                        {
                            zahlenfelder++;
                            if (!PLATZHALTERZAHLEN.Any(e => Math.Abs(w - e) <= 1e-9 * Math.Max(1.0, Math.Abs(e))))
                                funde.Add(name + " Zeile " + (i + 1) + ": Zahl " + s);
                            continue;
                        }
                        if (s.Any(char.IsDigit) && !PLATZHALTERTEXTE.Contains(s))
                            funde.Add(name + " Zeile " + (i + 1) + ": Text \"" + s + "\"");
                    }
                if (zahlenfelder == 0)
                    funde.Add(name + ": kein einziges Zahlenfeld gelesen (Trenner '" + trenner + "')");
            }
            return funde;
        }

        /// <summary>
        /// <b>Keine Normzahl in der Vorlage</b> (ZU24, Kapitel 6 (a)): Jedes Zahlenfeld der vier
        /// Dateien steht in der Liste der Platzhalter, und jedes Textfeld mit einer Ziffer steht in der
        /// Freiliste. Wer einen Normwert einträgt — als Zahl oder als Angabe im Quellentext —, fällt
        /// hier auf; die gefüllte Datei gehört außerhalb des Repositoriums.
        /// </summary>
        [Fact]
        public void Die_Paketvorlage_A100_traegt_nur_Platzhalterzahlen()
        {
            List<string> funde = PlatzhalterFunde(VorlageA100());
            Assert.True(funde.Count == 0, "Die Paketvorlage A100 traegt Zahlen oder Texte, die keine " +
                                         "Platzhalter sind (Kapitel 6 (a): keine Normzahl im Repositorium):" +
                                         Environment.NewLine + string.Join(Environment.NewLine, funde));
        }

        /// <summary>
        /// <b>Die Platzhalterwache greift auch</b> (Gegenprobe zu ZU24): Eine Kopie der Vorlage, in der
        /// eine fremde Zahl (37) bzw. eine Tabellennummer im Quellentext steht, ergibt einen Fund. Ohne
        /// diesen Fall wäre nicht belegt, dass der grüne Lauf des Nachbarfalls etwas prüft.
        /// </summary>
        [Fact]
        public void Die_Platzhalterwache_meldet_eine_fremde_Zahl_und_eine_Zahl_im_Quellentext()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "EPOS_A100_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(ordner);
                foreach (string datei in Directory.GetFiles(VorlageA100(), "*.csv"))
                    File.Copy(datei, Path.Combine(ordner, Path.GetFileName(datei)));
                string ziel = Path.Combine(ordner, "Tab_TwwNutzungsart_STAMM.csv");
                string[] zeilen = File.ReadAllLines(ziel, Encoding.UTF8);

                Assert.Empty(PlatzhalterFunde(ordner));                       // die Kopie selbst ist sauber

                // (1) eine fremde Zahl: Bedarf_Niedrig 10 -> 37
                string[] felder = zeilen[1].Split(';');
                int spalte = Array.IndexOf(zeilen[0].Split(';'), "Bedarf_Niedrig");
                Assert.True(spalte > 0, "Spalte Bedarf_Niedrig fehlt");
                felder[spalte] = "37";
                File.WriteAllLines(ziel, new[] { zeilen[0], string.Join(";", felder) }, Encoding.UTF8);
                Assert.Contains(PlatzhalterFunde(ordner), f => f.Contains("Zahl 37"));

                // (2) eine Zahl im Quellentext: Tabellennummer eingetragen
                felder = zeilen[1].Split(';');
                spalte = Array.IndexOf(zeilen[0].Split(';'), "Bedarf_Quelle");
                Assert.True(spalte > 0, "Spalte Bedarf_Quelle fehlt");
                felder[spalte] = "DIN EN 12831-3 Beiblatt A100, Tabelle NA.7";
                File.WriteAllLines(ziel, new[] { zeilen[0], string.Join(";", felder) }, Encoding.UTF8);
                Assert.Contains(PlatzhalterFunde(ordner), f => f.Contains("Tabelle NA.7"));
            }
            finally
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
        }

        [Fact]
        public void Die_Herkunftsart_eines_Imports_ist_IMPORT_ausser_FREI_und_FIKTIV()
        {
            foreach (Herkunftsart art in Enum.GetValues(typeof(Herkunftsart)))
            {
                Provenienz p = TwwNutzungsartCtrl.ImportHerkunft(new Provenienz("Q", "A", "V", art));
                Herkunftsart soll = art == Herkunftsart.Frei || art == Herkunftsart.Fiktiv ? art : Herkunftsart.Import;
                Assert.Equal(soll, p.Art);
                Assert.Equal("Q", p.Quelle);
                Assert.Equal("V", p.Version);
            }
        }
    }
}
