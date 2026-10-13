using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Katalogimport der DIN-4708-Katalogwerte</b> (ZB7,
    /// <c>EPOS.Kern/Controller/TwwNutzungsartCtrl.ImportDin4708.cs</c>): Belegung je Raumzahl und
    /// Ausstattungsklassen kommen über die wahlfreie Datei <c>Tab_TwwDin4708Wert_STAMM.csv</c> in den
    /// Katalog — anlegen, am Platz ersetzen, überspringen, benannt ablehnen —, und die Paketvorlage
    /// <c>Referenzlaeufe/Katalogpaket_Vorlage_DIN4708</c> spielt unverändert nichts ein.
    ///
    /// <para><b>Nur erfundene Werte</b> (Konzept Kapitel 6 (a)): runde Zahlen, Klassennamen „Klasse …“,
    /// keine Normzahl. Die meisten Fälle laufen an einer leeren Datenbank (<see cref="TwwTestdatenbank"/>),
    /// die Fälle der Vorlage zusätzlich an einer Arbeitskopie der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogimportDin4708Tests
    {
        private const string VERSION = "PRUEF-9";
        private const string DATEI = "Tab_TwwDin4708Wert_STAMM.csv";
        private const string KOPF = "Art;Schluessel;Wert;Katalogversion;Quelle;Ausgabe;Version;Herkunftsart";

        // =================================================================================
        // Anlegen, Ersetzen, Überspringen
        // =================================================================================

        /// <summary>
        /// Neue Zeilen beider Arten: angelegt mit Stand und Herkunft <c>IMPORT</c>, die Raumzahl
        /// normalisiert („2.0“ wird „2“), und der Lesepfad des Zapfprofils sieht sie.
        /// </summary>
        [Fact]
        public void Neue_Zeilen_beider_Arten_werden_angelegt_und_vom_Zapfprofil_gelesen()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket(
                Zeile("BELEGUNG", "2.0", "1.5"),
                Zeile("belegung", "2.5", "2"),
                Zeile("AUSSTATTUNG", "Klasse A", "3000")));

            Assert.Null(b.Abbruch);
            Assert.Equal(3, b.Angelegt);
            Assert.All(b.Zeilen, z => Assert.Equal(TwwImportbereich.Din4708, z.Bereich));
            Assert.Equal(new[] { "BELEGUNG 2", "BELEGUNG 2.5", "AUSSTATTUNG Klasse A" }, b.Zeilen.Select(z => z.Nutzungsart).ToArray());
            Assert.Equal(3, b.ZeilenVon(TwwImportbereich.Din4708).Count);

            foreach (DataRow r in Tabelle().Rows)
            {
                Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(r["Status"]));
                Assert.Equal(TwwSchema.HERKUNFT_IMPORT, Convert.ToString(r["Herkunftsart"]));
                Assert.Equal(0L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
            }

            IReadOnlyDictionary<string, double> belegung = ZapfprofilCtrl.Belegung(VERSION);
            Assert.Equal(1.5, belegung["2"]);
            Assert.Equal(2.0, belegung["2.5"]);
            Din4708Ausstattung a = Assert.Single(ZapfprofilCtrl.Ausstattungen(VERSION));
            Assert.Equal("Klasse A", a.Schluessel);
            Assert.Equal(3000.0, a.WertWh);
        }

        /// <summary>
        /// Abweichender Wert: die vorhandene Zeile — auch eine der Auslieferung — trägt den neuen Wert
        /// unter DERSELBEN Id (ein Wohnungstyp, der auf sie zeigt, trifft weiter), Stand <c>IMPORT</c>,
        /// <c>ReadOnly</c> 0; der Bericht nennt alten und neuen Wert, ein Hinweis die Auslieferungszeile.
        /// </summary>
        [Fact]
        public void Ein_abweichender_Wert_ersetzt_die_Zeile_am_Platz_mit_gleicher_Id()
        {
            using var db = new TwwTestdatenbank();
            int id = WertAnlegen("AUSSTATTUNG", "Klasse B", 5000, VERSION, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket(Zeile("AUSSTATTUNG", "Klasse B", "6000")));

            Assert.Null(b.Abbruch);
            TwwImportzeile z = Assert.Single(b.Zeilen);
            Assert.Equal(TwwImportausgang.Ersetzt, z.Ausgang);
            Assert.Equal(id, z.IdErsetzt);
            Assert.Equal("KATALOGIMPORT_DIN4708_ERSETZT", z.Grund.Kennung);
            Assert.Equal(5000.0, Convert.ToDouble(z.Grund.Werte[1], CultureInfo.InvariantCulture));
            Assert.Equal(6000.0, Convert.ToDouble(z.Grund.Werte[2], CultureInfo.InvariantCulture));
            Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_AUSLIEFERUNG_ERSETZT");

            DataRow r = Assert.Single(Tabelle().Rows.Cast<DataRow>());
            Assert.Equal(id, Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            Assert.Equal(6000.0, Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture));
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(r["Status"]));
            Assert.Equal(0L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Beleg"]);
        }

        [Fact]
        public void Eine_gleiche_Zeile_wird_uebersprungen()
        {
            using var db = new TwwTestdatenbank();
            WertAnlegen("BELEGUNG", "3", 2.5, VERSION, "EIGEN");

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket(Zeile("BELEGUNG", "3.0", "2.5")));

            TwwImportzeile z = Assert.Single(b.Zeilen);
            Assert.Equal(TwwImportausgang.Uebersprungen, z.Ausgang);
            Assert.Equal("KATALOGIMPORT_GLEICH_VORHANDEN", z.Grund.Kennung);
            Assert.Equal("EIGEN", Convert.ToString(Assert.Single(Tabelle().Rows.Cast<DataRow>())["Status"]));
        }

        // =================================================================================
        // Formfehler und Inhaltsfehler
        // =================================================================================

        [Fact]
        public void Eine_unbekannte_Spalte_lehnt_das_Paket_ab_und_laesst_den_Katalog_unveraendert()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(new List<TwwPaketdatei>
            {
                new TwwPaketdatei(DATEI, KOPF + ";Farbe\r\nBELEGUNG;2;1.5;" + VERSION + ";Q;;V;FIKTIV;rot\r\n")
            });

            Assert.Equal("KATALOGIMPORT_SPALTE_UNBEKANNT", b.Abbruch?.Kennung);
            Assert.True(b.Abbruch.Nennt(DATEI));
            Assert.True(b.Abbruch.Nennt("Farbe"));
            Assert.Empty(b.Zeilen);
            Assert.Equal(0, Tabelle().Rows.Count);
        }

        /// <summary>Eine Zahl mit Komma ist ein Formfehler: Datei, Zeile und Spalte im Grund, nichts geschrieben.</summary>
        [Fact]
        public void Eine_Zahl_mit_Komma_lehnt_das_Paket_mit_Datei_Zeile_und_Spalte_ab()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket(
                Zeile("BELEGUNG", "2", "1.5"),
                Zeile("BELEGUNG", "3", "2,5")));

            Assert.Equal("KATALOGIMPORT_ZAHL", b.Abbruch?.Kennung);
            Assert.Equal(DATEI, b.Abbruch.Werte[0]);
            Assert.Equal(3, Convert.ToInt32(b.Abbruch.Werte[1], CultureInfo.InvariantCulture));
            Assert.Equal("Wert", b.Abbruch.Werte[2]);
            Assert.Equal(0, Tabelle().Rows.Count);
        }

        /// <summary>
        /// Inhaltsfehler lehnen nur ihre Zeile ab: unbekannte Art, Raumzahl 0, negativ, nicht in halben
        /// Schritten oder keine Zahl, Wert 0 oder negativ, leerer Klassenname — die gültige Zeile kommt.
        /// </summary>
        [Fact]
        public void Inhaltsfehler_lehnen_nur_ihre_Zeile_ab_die_uebrigen_kommen()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Paket(
                Zeile("PERSONEN", "2", "1"),
                Zeile("BELEGUNG", "0", "1"),
                Zeile("BELEGUNG", "-1", "1"),
                Zeile("BELEGUNG", "2.3", "1"),
                Zeile("BELEGUNG", "zwei", "1"),
                Zeile("BELEGUNG", "4", "0"),
                Zeile("AUSSTATTUNG", "Klasse C", "-5"),
                Zeile("AUSSTATTUNG", "", "100"),
                Zeile("AUSSTATTUNG", "Klasse D", "700")));

            Assert.Null(b.Abbruch);
            string[] gruende = b.Zeilen.Select(z => z.Grund?.Kennung).ToArray();
            Assert.Equal(new[]
            {
                "KATALOGIMPORT_DIN4708_ART",
                "KATALOGIMPORT_DIN4708_RAUMZAHL", "KATALOGIMPORT_DIN4708_RAUMZAHL",
                "KATALOGIMPORT_DIN4708_RAUMZAHL", "KATALOGIMPORT_DIN4708_RAUMZAHL",
                "KATALOGIMPORT_DIN4708_WERT", "KATALOGIMPORT_DIN4708_WERT",
                "KATALOGIMPORT_PFLICHT_FEHLT",
                null
            }, gruende);
            Assert.Equal(8, b.Abgelehnt);
            Assert.Equal(1, b.Angelegt);
            Assert.True(b.Zeilen[0].Grund.Nennt("PERSONEN"));
            Assert.Equal("Klasse D", Assert.Single(ZapfprofilCtrl.Ausstattungen(VERSION)).Schluessel);
        }

        [Fact]
        public void Der_Raumzahlschluessel_steht_so_wie_der_Rechenweg_ihn_sucht()
        {
            Assert.Equal("2", TwwNutzungsartCtrl.Din4708Raumzahlschluessel("2"));
            Assert.Equal("2", TwwNutzungsartCtrl.Din4708Raumzahlschluessel(" 2.00 "));
            Assert.Equal("0.5", TwwNutzungsartCtrl.Din4708Raumzahlschluessel("0.5"));
            Assert.Equal(2.5.ToString(CultureInfo.InvariantCulture), TwwNutzungsartCtrl.Din4708Raumzahlschluessel("2.5"));
            Assert.Null(TwwNutzungsartCtrl.Din4708Raumzahlschluessel("0.25"));
            Assert.Null(TwwNutzungsartCtrl.Din4708Raumzahlschluessel("0"));
            Assert.Null(TwwNutzungsartCtrl.Din4708Raumzahlschluessel("NaN"));
            Assert.Null(TwwNutzungsartCtrl.Din4708Raumzahlschluessel(""));
        }

        // =================================================================================
        // Katalogversion, Prüfmodus, Kennwertpaket, fehlende Tabelle
        // =================================================================================

        /// <summary>Ohne Spalte <c>Katalogversion</c> treten die Zeilen der Katalogversion des Katalogs bei.</summary>
        [Fact]
        public void Ohne_Katalogversion_treten_die_Zeilen_der_Version_des_Katalogs_bei()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Zapfprofil.Stundenschwelle", 0.1, "KAT-3", "kW");

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(new List<TwwPaketdatei>
            {
                new TwwPaketdatei(DATEI, "Art;Schluessel;Wert;Quelle;Version;Herkunftsart\r\n" +
                                         "AUSSTATTUNG;Klasse E;800;Q;V;FIKTIV\r\n")
            });

            Assert.Null(b.Abbruch);
            Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION" && h.Nennt("KAT-3"));
            Assert.Equal("KAT-3", Assert.Single(b.Zeilen).Katalogversion);
            Assert.Equal("Klasse E", Assert.Single(ZapfprofilCtrl.Ausstattungen("KAT-3")).Schluessel);
        }

        /// <summary>Führt nur eine der Kopfdateien die Spalte, ist das Paket als Ganzes abgelehnt.</summary>
        [Fact]
        public void Eine_gemischte_Katalogversion_lehnt_das_ganze_Paket_ab()
        {
            using var db = new TwwTestdatenbank();
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(new List<TwwPaketdatei>
            {
                new TwwPaketdatei(DATEI, "Art;Schluessel;Wert;Quelle;Version;Herkunftsart\r\n" +
                                         "AUSSTATTUNG;Klasse F;800;Q;V;FIKTIV\r\n"),
                new TwwPaketdatei("Tab_TwwParameter_STAMM.csv",
                                  "Schluessel;Wert;Einheit;Katalogversion;Quelle;Version;Herkunftsart\r\n" +
                                  "DIN4708.z;9;h;" + VERSION + ";Q;V;FIKTIV\r\n")
            });

            Assert.Equal("KATALOGIMPORT_VERSION_GEMISCHT", b.Abbruch?.Kennung);
            Assert.True(b.Abbruch.Nennt(DATEI));
            Assert.Equal(0, Tabelle().Rows.Count);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM)));
        }

        [Fact]
        public void Nur_pruefen_liefert_denselben_Bericht_und_schreibt_nichts()
        {
            using var db = new TwwTestdatenbank();
            int id = WertAnlegen("BELEGUNG", "2", 1.0, VERSION, "EIGEN");
            IReadOnlyList<TwwPaketdatei> paket = Paket(Zeile("BELEGUNG", "2", "1.5"), Zeile("AUSSTATTUNG", "Klasse G", "900"));

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(paket, pruefen: true);

            Assert.True(b.Pruefmodus);
            Assert.Equal(1, b.Ersetzt);
            Assert.Equal(1, b.Angelegt);
            DataRow r = Assert.Single(Tabelle().Rows.Cast<DataRow>());
            Assert.Equal(id, Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            Assert.Equal(1.0, Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture));
            Assert.Equal("EIGEN", Convert.ToString(r["Status"]));
        }

        /// <summary>
        /// Ein Paket ohne Nutzungsarten ist nur als Kennwertpaket zulässig: Führt es eine Datei der
        /// Nutzungsarten-Kette, bleibt es abgelehnt wie bisher.
        /// </summary>
        [Fact]
        public void Ohne_Nutzungsarten_aber_mit_Tagesgangsatz_bleibt_das_Paket_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            List<TwwPaketdatei> paket = Paket(Zeile("AUSSTATTUNG", "Klasse H", "900")).ToList();
            paket.Add(new TwwPaketdatei(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv",
                                        "ID;Bezeichner;Katalogversion\r\n1;Satz;" + VERSION + "\r\n"));

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(paket);

            Assert.Equal("KATALOGIMPORT_KEINE_DATEI", b.Abbruch?.Kennung);
            Assert.Equal(0, Tabelle().Rows.Count);
        }

        /// <summary>Fehlt der Datenbank die Tabelle, ist die Datei benannt übergangen; die übrigen Dateien kommen.</summary>
        [Fact]
        public void Ohne_die_Tabelle_ist_die_Datei_benannt_uebergangen()
        {
            using var db = new TwwTestdatenbank();
            DataRepository.ExecuteNonQuery("DROP TABLE " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM);

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(new List<TwwPaketdatei>
            {
                new TwwPaketdatei(DATEI, KOPF + "\r\nAUSSTATTUNG;Klasse I;900;" + VERSION + ";Q;;V;FIKTIV\r\n"),
                new TwwPaketdatei("Tab_TwwParameter_STAMM.csv",
                                  "Schluessel;Wert;Einheit;Katalogversion;Quelle;Version;Herkunftsart\r\n" +
                                  "DIN4708.z;9;h;" + VERSION + ";Q;V;FIKTIV\r\n")
            });

            Assert.Null(b.Abbruch);
            Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_DIN4708_OHNE_TABELLE");
            Assert.Equal(TwwImportbereich.Parameter, Assert.Single(b.Zeilen).Bereich);
            Assert.Equal(1, b.Angelegt);
        }

        // =================================================================================
        // Die Paketvorlage
        // =================================================================================

        /// <summary>
        /// <b>Die unveränderte Vorlage spielt nichts ein</b>: Jede Zeile ist benannt abgelehnt (der Wert
        /// fehlt), der Katalog — eine Arbeitskopie der Testdatenbank mit eigenen DIN-4708-Werten und
        /// Parametern — bleibt Zeile für Zeile, wie er war.
        /// </summary>
        [Fact]
        public void Die_unveraenderte_Vorlage_lehnt_jede_Zeile_benannt_ab_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string vorher = Abbild();

            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(Vorlage(), out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(2, dateien.Count);
            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);

            Assert.Null(b.Abbruch);
            Assert.Equal(0, b.Angelegt);
            Assert.Equal(0, b.Ersetzt);
            Assert.Equal(0, b.Uebersprungen);
            Assert.Equal(5, b.ZeilenVon(TwwImportbereich.Din4708).Count);
            Assert.Equal(11, b.ZeilenVon(TwwImportbereich.Parameter).Count);
            Assert.All(b.Zeilen, z =>
            {
                Assert.Equal(TwwImportausgang.Abgelehnt, z.Ausgang);
                Assert.Equal("KATALOGIMPORT_PFLICHT_FEHLT", z.Grund.Kennung);
                Assert.True(z.Grund.Nennt("Wert"));
            });
            Assert.Equal(vorher, Abbild());
        }

        /// <summary>
        /// <b>Die gefüllte Vorlage spielt ein</b>: Eine Kopie mit erfundenen Werten tritt der
        /// Katalogversion der Testdatenbank bei, nichts ist abgelehnt, und danach liefert das
        /// Zapfprofil die Klassen der Vorlage und die Belegung je Raumzahl.
        /// </summary>
        [Fact]
        public void Die_mit_erfundenen_Werten_gefuellte_Vorlage_spielt_ohne_Ablehnung_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string ordner = Path.Combine(Path.GetTempPath(), "EPOS_DIN4708_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(ordner);
                var erfunden = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["1"] = "1.5", ["2"] = "2.5", ["3"] = "3.5", ["Klasse 1"] = "1111", ["Klasse 2"] = "2222",
                    ["DIN4708.a1"] = "7", ["DIN4708.a2"] = "8", ["DIN4708.z"] = "9", ["DIN4708.p_b"] = "4",
                    ["DIN4708.w_b"] = "1234", ["DIN4708.W_b"] = "4321", ["DIN4708.Kappung"] = "6",
                    ["DIN4708.Profil.Bloecke"] = "1", ["DIN4708.Profil.Block.1.Beginn"] = "600",
                    ["DIN4708.Profil.Block.1.Dauer"] = "60", ["DIN4708.Profil.Block.1.Anteil"] = "1"
                };
                Fuellen(DATEI, "Schluessel", erfunden, ordner);
                Fuellen("Tab_TwwParameter_STAMM.csv", "Schluessel", erfunden, ordner);

                string version = ZapfprofilCtrl.Zielkatalogversion();
                IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(ordner, out ZapfSatz fehler);
                Assert.Null(fehler);
                TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);

                Assert.Null(b.Abbruch);
                Assert.True(b.Abgelehnt == 0, string.Join("; ", b.Zeilen.Where(z => z.Grund != null).Select(z => z.Nutzungsart + ": " + z.Grund.Klartext)));
                Assert.Equal(16, b.Angelegt + b.Ersetzt + b.Uebersprungen);

                string[] klassen = ZapfprofilCtrl.Ausstattungen(version).Select(a => a.Schluessel).ToArray();
                Assert.Contains("Klasse 1", klassen);
                Assert.Contains("Klasse 2", klassen);
                IReadOnlyDictionary<string, double> belegung = ZapfprofilCtrl.Belegung(version);
                Assert.Equal(1.5, belegung["1"]);
                Assert.Equal(3.5, belegung["3"]);
                Assert.Equal(9.0, ZapfprofilCtrl.Parameter().Wert(ZapfAuslegungParameter.DIN4708_Z));
            }
            finally
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
        }

        /// <summary>
        /// <b>Keine Normzahl in der Vorlage</b>: Jede Spalte <c>Wert</c> ist leer, Zahlen stehen allein
        /// in Schlüssel und Einheit, und kein Quellen-, Ausgabe- oder
        /// Versionstext trägt eine Ziffer außer der Normnummer.
        /// </summary>
        [Fact]
        public void Die_Vorlage_traegt_nur_Platzhalter()
        {
            var funde = new List<string>();
            foreach (string datei in Directory.GetFiles(Vorlage(), "*.csv"))
            {
                string name = Path.GetFileName(datei);
                string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8).Where(x => x.Trim().Length > 0).ToArray();
                string[] kopf = zeilen[0].Split(';');
                if (zeilen.Length < 2) funde.Add(name + ": keine Beispielzeile");
                for (int i = 1; i < zeilen.Length; i++)
                {
                    string[] felder = zeilen[i].Split(';');
                    if (felder.Length != kopf.Length) { funde.Add(name + " Zeile " + (i + 1) + ": Feldzahl"); continue; }
                    for (int s = 0; s < kopf.Length; s++)
                    {
                        string f = felder[s].Trim();
                        if (kopf[s] == "Wert" && f.Length > 0) funde.Add(name + " Zeile " + (i + 1) + ": Wert " + f);
                        bool schluessel = kopf[s] == "Schluessel" || kopf[s] == "Einheit"; // Block.1, Klasse 1, 1/h
                        string ohneNorm = f.Replace("4708", "");
                        if (!schluessel && ohneNorm.Any(char.IsDigit))
                            funde.Add(name + " Zeile " + (i + 1) + ": Ziffer in " + kopf[s] + " \"" + f + "\"");
                    }
                }
            }
            Assert.True(funde.Count == 0, "Die DIN-4708-Vorlage traegt Werte statt Platzhaltern:\n" + string.Join("\n", funde));
        }

        // =================================================================================
        // Helfer
        // =================================================================================

        private static string Zeile(string art, string schluessel, string wert)
            => art + ";" + schluessel + ";" + wert + ";" + VERSION + ";" + TwwTestdatenbank.QUELLE + ";;V1;VERFAHREN";

        private static IReadOnlyList<TwwPaketdatei> Paket(params string[] zeilen)
            => new List<TwwPaketdatei> { new TwwPaketdatei(DATEI, KOPF + "\r\n" + string.Join("\r\n", zeilen) + "\r\n") };

        private static int WertAnlegen(string art, string schluessel, double wert, string version, string status,
                                       bool readOnly = false)
            => DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM +
                " (Art, Schluessel, Wert, Katalogversion, Quelle, Ausgabe, Version, Herkunftsart, Status, Beleg, ReadOnly) " +
                "VALUES (?, ?, ?, ?, ?, NULL, ?, 'FIKTIV', ?, 'intern', ?)",
                new[]
                {
                    new DbParam("@a", art), new DbParam("@s", schluessel), new DbParam("@w", wert), new DbParam("@k", version),
                    new DbParam("@q", TwwTestdatenbank.QUELLE), new DbParam("@v", version), new DbParam("@st", status),
                    new DbParam("@r", readOnly ? 1 : 0)
                });

        private static DataTable Tabelle()
            => DataRepository.GetDataTable("SELECT * FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM + " ORDER BY ID");

        /// <summary>Jede Zelle der DIN-4708-Werte und der Parameter als Text — der Zellvergleich vorher/nachher.</summary>
        private static string Abbild()
        {
            var sb = new StringBuilder();
            foreach (string t in new[] { TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM })
                foreach (DataRow r in DataRepository.GetDataTable("SELECT * FROM " + t + " ORDER BY ID").Rows)
                    sb.Append(t).Append('|').AppendJoin("|", r.ItemArray.Select(x => x == DBNull.Value
                        ? "NULL" : Convert.ToString(x, CultureInfo.InvariantCulture))).Append('\n');
            return sb.ToString();
        }

        /// <summary>Kopiert eine Datei der Vorlage nach <paramref name="ziel"/> und trägt je Schlüssel den erfundenen Wert ein.</summary>
        private static void Fuellen(string datei, string schluesselspalte, Dictionary<string, string> werte, string ziel)
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(Vorlage(), datei), Encoding.UTF8).Where(x => x.Length > 0).ToArray();
            string[] kopf = zeilen[0].Split(';');
            int s = Array.IndexOf(kopf, schluesselspalte), w = Array.IndexOf(kopf, "Wert");
            for (int i = 1; i < zeilen.Length; i++)
            {
                string[] f = zeilen[i].Split(';');
                f[w] = werte[f[s]];
                zeilen[i] = string.Join(";", f);
            }
            File.WriteAllLines(Path.Combine(ziel, datei), zeilen, new UTF8Encoding(false));
        }

        private static string Vorlage() => Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_Vorlage_DIN4708");

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
