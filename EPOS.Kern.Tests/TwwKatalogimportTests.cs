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

            // Der freie Paketteil allein führt keine Nutzungsart: abgelehnt, die fremden Dateien benannt übergangen.
            string frei = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ZapfZufallTests.Probenordner()))),
                                       "Referenzlaeufe", "Katalogpaket_frei");
            IReadOnlyList<TwwPaketdatei> teil = TwwNutzungsartCtrl.PaketLesen(frei, out ZapfSatz fehler);
            Assert.Null(fehler);
            TwwKatalogimportBericht bf = TwwNutzungsartCtrl.Importieren(teil);
            Assert.Equal("KATALOGIMPORT_KEINE_DATEI", bf.Abbruch.Kennung);
            Assert.Contains(bf.Hinweise, h => h.Kennung == "KATALOGIMPORT_DATEI_UEBERGANGEN"
                                               && h.Werte[0].Equals(TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM));
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

        [Fact]
        public void Komma_als_Trenner_und_Felder_in_Anfuehrungszeichen_nach_RFC_4180()
        {
            using var db = new TwwTestdatenbank();
            const string NAME = "Probe; \"zitiert\" (erfunden)";
            List<TwwPaketdatei> p = Paket().Select(d =>
            {
                // Aus „;" wird „,"; der Bezeichner mit Trenner und Anführungszeichen steht in Anführungszeichen.
                string t = d.Inhalt.Replace(";", ",");
                if (d.Name.StartsWith(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, StringComparison.Ordinal))
                    t = t.Replace("10," + A + ",", "10,\"" + NAME.Replace("\"", "\"\"") + "\",");
                return d with { Inhalt = t };
            }).ToList();

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(p);
            Assert.Null(b.Abbruch);
            Assert.Equal(new[] { NAME, B }, b.Zeilen.Select(z => z.Katalogname).ToArray());
            Assert.Equal(NAME, TwwNutzungsartCtrl.Lies(b.NeueIds[0]).Name);
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
