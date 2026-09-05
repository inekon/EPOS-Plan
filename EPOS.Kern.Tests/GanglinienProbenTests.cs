using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der bitgleiche Nachweis des Ganglinien-Imports</b> (iU9-W12.0i).
    ///
    /// <para><b>Warum es diese Sammlung gibt.</b> Die AP5-Importkette
    /// (<see cref="GanglinienDatei"/> + <see cref="GanglinienPruefung"/>) stand bis
    /// zur Welle 12 ZWEIMAL woertlich im Bestand — einmal in
    /// <c>Form_Stromganglinie_Admin.btn_Einlesen_Click</c> mit Ablage und einmal in
    /// <c>Form_PeakShaving.Datei_Click</c> ohne. Beide werden in dieser Welle durch
    /// EINEN Kern-Ablauf ersetzt (<see cref="GanglinienImportAblauf"/>). Fuer die
    /// Leseschicht selbst gab es dabei bis hierher KEINEN einzigen Test (Befund
    /// W12-B14) — die Pruefschicht hat 41 in
    /// <c>SpeicherEngine.Tests/GanglinienPruefungTests.cs</c>, die Datei- und
    /// Erkennungsschicht keinen.
    ///
    /// <para><b>Was hier steht.</b> Elf abgelegte Probendateien unter
    /// <c>EPOS.Kern.Tests/Proben/Ganglinien/</c> und eine zwoelfte, die der Test
    /// selbst erzeugt (525 600 Minutenwerte, rund 3,5 MB — die legt man nicht in ein
    /// Verzeichnis, das bei jedem Auschecken mitkommt). Sie decken die Achsen
    /// Trennzeichen (<c>;</c> / <c>,</c> / Tabulator / einspaltig), Dezimaltrenner
    /// (Komma / Punkt), Kopfzeile (mit / ohne), Rasterlaenge
    /// (8 760 / 35 040 / 525 600), Schaltjahr, beide Sommerzeitfaelle, die Einheit
    /// kWh je Intervall und Excel ab.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN</b> — sie stammen aus dem
    /// Bestand vom 04.09.2026, VOR dem Umbau der Kette, und sind hier auf die letzte
    /// Stelle festgehalten. Aendert sich eine Zahl, ist das kein Testfehler, sondern
    /// eine Verhaltensaenderung des Imports.</para>
    ///
    /// <para><b>Ohne Datenbank, ohne Oberflaeche</b> — reines Datei-Lesen und
    /// Rechnen. Die Reihe ist kulturunabhaengig: <see cref="GanglinienDatei"/> parst
    /// ueber den ERKANNTEN Dezimaltrenner, nie ueber <c>CurrentCulture</c>. Deshalb
    /// braucht diese Sammlung — anders als die Texttests — keine festgelegte
    /// Oberflaechensprache.</para>
    /// </summary>
    public class GanglinienProbenTests
    {
        // ==================================================================
        // Zugang zu den Proben
        // ==================================================================

        /// <summary>
        /// Sucht <c>EPOS.Kern.Tests/Proben/Ganglinien</c> aufwaerts vom Laufordner —
        /// dasselbe Vorgehen wie <c>TestDatenbank.Quelle</c>. Die Proben werden
        /// bewusst NICHT in die Ausgabe kopiert: 3,4 MB je Bauart waeren teuer, und
        /// gelesen wird ohnehin nur.
        /// </summary>
        private static string Ordner()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests", "Proben", "Ganglinien");
                if (Directory.Exists(kandidat)) return kandidat;
            }
            return null;
        }

        private static string Probe(string name)
        {
            string ordner = Ordner();
            Assert.True(ordner != null, "Der Probenordner EPOS.Kern.Tests/Proben/Ganglinien wurde nicht gefunden.");
            string pfad = Path.Combine(ordner, name);
            Assert.True(File.Exists(pfad), "Die Probe fehlt: " + pfad);
            return pfad;
        }

        /// <summary>Erkennen, Lesen und Pruefen in einem Zug — genau die Reihenfolge der Maske.</summary>
        private static (GanglinienVorschau v, GanglinienRohdaten r, GanglinienPruefErgebnis e)
            Kette(string name, GanglinienEinheit einheit = GanglinienEinheit.Kilowatt)
        {
            string pfad = Probe(name);
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
            Assert.True(v.Lesbar, "Die Probe " + name + " gilt als nicht lesbar.");

            GanglinienImportOptionen o = v.Vorschlag;
            o.Einheit = einheit;

            GanglinienRohdaten r = GanglinienDatei.Lies(pfad, o);
            GanglinienPruefErgebnis e = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
            {
                Rohwerte = r.Werte,
                Zeitstempel = r.Zeitstempel,
                Einheit = o.Einheit,
                DeklariertesRaster = o.Raster,
                Konvention = o.Konvention
            });
            return (v, r, e);
        }

        private static double Summe(double[] w)
        {
            double s = 0.0;
            for (int i = 0; i < w.Length; i++) s += w[i];
            return s;
        }

        private static double Groesster(double[] w)
        {
            double s = double.MinValue;
            for (int i = 0; i < w.Length; i++) if (w[i] > s) s = w[i];
            return s;
        }

        /// <summary>Die Kurzfassungen aller Protokollzeilen — sprachneutral, damit der Vergleich stimmt.</summary>
        private static List<string> Zeilen(IReadOnlyList<PruefMeldung> protokoll)
        {
            List<string> liste = new List<string>();
            for (int i = 0; i < protokoll.Count; i++) liste.Add(protokoll[i].ToString());
            return liste;
        }

        // ==================================================================
        // 1 — 8 760 Stundenwerte, Semikolon, Dezimalkomma, mit Kopfzeile
        // ==================================================================

        [Fact]
        public void P01_Semikolon_mit_Dezimalkomma_und_Kopfzeile_wird_erkannt()
        {
            GanglinienVorschau v = GanglinienDatei.Erkenne(Probe("p01_stunden_semikolon_komma_kopf.csv"));

            Assert.True(v.Lesbar);
            Assert.False(v.IstExcel);
            Assert.Equal(2, v.Spaltenzahl);
            Assert.Equal(GanglinienDatei.VorschauZeilen, v.Zeilen.Count);
            Assert.Equal(';', v.Vorschlag.Trennzeichen);
            Assert.Equal(',', v.Vorschlag.Dezimaltrenner);
            Assert.True(v.Vorschlag.Kopfzeile);
            Assert.Equal(0, v.Vorschlag.ZeitSpalte);
            Assert.Equal(1, v.Vorschlag.WertSpalte);

            // Die Erkennung protokolliert genau zwei Zeilen — Format und Spaltenwahl.
            Assert.Equal(new[] { "IMPORT_PROT_FORMAT_ERKANNT: ;; ,; 2; 1", "IMPORT_PROT_SPALTENWAHL: 2; 1" },
                         Zeilen(v.Meldungen));
        }

        [Fact]
        public void P01_liefert_die_eingefrorene_Stundenreihe()
        {
            var (_, r, e) = Kette("p01_stunden_semikolon_komma_kopf.csv");

            Assert.True(r.Erfolgreich);
            Assert.Equal(8760, r.Werte.Length);
            Assert.Equal(8760, r.Zeitstempel.Length);
            Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0), r.Zeitstempel[0]);

            Assert.True(e.Erfolgreich);
            Assert.Equal(1, e.Zeitinterval);
            Assert.Equal(8760, e.Werte.Length);
            Assert.False(e.SchaltjahrNormalisiert);
            Assert.False(e.Gemittelt);
            Assert.False(e.SommerzeitBehandelt);
            Assert.False(e.BestaetigungNoetig);

            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(232.23, e.Werte[1]);
            Assert.Equal(280.57, e.Werte[100]);
            Assert.Equal(215.33, e.Werte[4000]);
            Assert.Equal(221.77, e.Werte[8759]);
            Assert.Equal(2005977.0000000068, Summe(e.Werte));
            Assert.Equal(402.06, Groesster(e.Werte));
        }

        // ==================================================================
        // 2 — Komma als Trennzeichen, Dezimalpunkt, OHNE Kopfzeile
        // ==================================================================

        [Fact]
        public void P02_Komma_als_Trennzeichen_und_Dezimalpunkt_ohne_Kopfzeile()
        {
            var (v, r, e) = Kette("p02_stunden_komma_punkt_ohne_kopf.csv");

            Assert.Equal(',', v.Vorschlag.Trennzeichen);
            Assert.Equal('.', v.Vorschlag.Dezimaltrenner);
            Assert.False(v.Vorschlag.Kopfzeile);
            Assert.Equal(0, v.Vorschlag.ZeitSpalte);
            Assert.Equal(1, v.Vorschlag.WertSpalte);

            Assert.Equal(8760, r.Werte.Length);
            Assert.True(e.Erfolgreich);

            // Dieselbe Reihe wie P01 — dieselben Zahlen, nur anders geschrieben.
            // Genau das ist die Zusage der Leseschicht: die Datei einmal mit Punkt
            // und einmal mit Komma ergibt dieselbe Reihe.
            Assert.Equal(1, e.Zeitinterval);
            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(232.23, e.Werte[1]);
            Assert.Equal(280.57, e.Werte[100]);
            Assert.Equal(2005977.0000000068, Summe(e.Werte));
        }

        // ==================================================================
        // 3 — Tabulator, drei Spalten, die dritte ist Text
        // ==================================================================

        [Fact]
        public void P03_Tabulator_mit_dritter_Textspalte_waehlt_die_richtige_Wertspalte()
        {
            var (v, r, e) = Kette("p03_stunden_tab_komma_kopf.csv");

            Assert.Equal('\t', v.Vorschlag.Trennzeichen);
            Assert.Equal(',', v.Vorschlag.Dezimaltrenner);
            Assert.Equal(3, v.Spaltenzahl);
            Assert.Equal(0, v.Vorschlag.ZeitSpalte);
            Assert.Equal(1, v.Vorschlag.WertSpalte);   // NICHT die Textspalte 2

            Assert.Equal(8760, r.Werte.Length);
            Assert.True(e.Erfolgreich);
            Assert.Equal(2005977.0000000068, Summe(e.Werte));
        }

        // ==================================================================
        // 4 — einspaltig (Altweg .txt), ohne Zeitstempel
        // ==================================================================

        [Fact]
        public void P04_Einspaltige_Textdatei_bestimmt_das_Raster_aus_der_Anzahl()
        {
            var (v, r, e) = Kette("p04_stunden_einspaltig_punkt.txt");

            Assert.Equal('\0', v.Vorschlag.Trennzeichen);
            Assert.Equal(1, v.Spaltenzahl);
            Assert.Equal(-1, v.Vorschlag.ZeitSpalte);
            Assert.Equal(0, v.Vorschlag.WertSpalte);

            Assert.Equal(8760, r.Werte.Length);
            Assert.Null(r.Zeitstempel);

            // Ohne Zeitspalte gibt es weder RASTER_AUS_ZEIT noch KONVENTION_* —
            // das Raster kommt allein aus der Wertanzahl.
            Assert.Equal(new[] { "IMPORT_PROT_RASTER_ERKANNT: 8760; 1", "IMPORT_PROT_ERGEBNIS: 8760; 1; 2005977" },
                         Zeilen(e.Protokoll));
            Assert.Equal(1, e.Zeitinterval);
            Assert.Equal(2005977.0000000068, Summe(e.Werte));
        }

        // ==================================================================
        // 5 — 35 040 Viertelstundenwerte mit ISO-Zeitstempel
        // ==================================================================

        [Fact]
        public void P05_Viertelstundenreihe_mit_ISO_Zeitstempel()
        {
            var (v, r, e) = Kette("p05_viertelstunden_semikolon_punkt_kopf.csv");

            Assert.Equal(';', v.Vorschlag.Trennzeichen);
            Assert.Equal('.', v.Vorschlag.Dezimaltrenner);
            Assert.Equal(35040, r.Werte.Length);
            Assert.Equal(new DateTime(2023, 1, 1, 0, 15, 0), r.Zeitstempel[1]);

            Assert.True(e.Erfolgreich);
            Assert.Equal(4, e.Zeitinterval);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(223.46, e.Werte[1]);
            Assert.Equal(246.8, e.Werte[100]);
            Assert.Equal(261.91, e.Werte[4000]);
            Assert.Equal(217.04, e.Werte[35039]);
            Assert.Equal(8024150.999999962, Summe(e.Werte));
        }

        // ==================================================================
        // 6 — einspaltig MIT Dezimalkomma: die Erkennung greift daneben
        // ==================================================================

        /// <summary>
        /// <b>Woertlich eingefroren, obwohl es falsch aussieht.</b> Eine einspaltige
        /// Datei mit Dezimalkomma sieht fuer die Trennzeichenerkennung aus wie eine
        /// zweispaltige Komma-Datei: <c>"223,46"</c> ist ein Komma zwischen zwei
        /// Zahlen. Die Erkennung schlaegt deshalb Trennzeichen <c>,</c> und
        /// Dezimaltrenner <c>.</c> vor, und die Reihe kommt GANZZAHLIG herein
        /// (223 statt 223,46). Genau dafuer gibt es den Optionendialog — der
        /// Anwender uebersteuert, und der naechste Fall zeigt das Ergebnis.
        /// </summary>
        [Fact]
        public void P06_Einspaltig_mit_Dezimalkomma_wird_als_Kommadatei_gelesen()
        {
            var (v, r, e) = Kette("p06_viertelstunden_einspaltig_komma.txt");

            Assert.Equal(',', v.Vorschlag.Trennzeichen);
            Assert.Equal('.', v.Vorschlag.Dezimaltrenner);
            Assert.Equal(0, v.Vorschlag.WertSpalte);
            Assert.Equal(-1, v.Vorschlag.ZeitSpalte);

            Assert.Equal(35040, r.Werte.Length);
            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(223.0, e.Werte[1]);       // der Nachkommateil ist die zweite "Spalte"
            Assert.Equal(8006779.0, Summe(e.Werte));
        }

        /// <summary>
        /// Derselbe Fall mit uebersteuerten Optionen — der Weg, den
        /// <c>Form_GanglinieImportOptionen</c> heute und
        /// <c>GanglinieImportOptionenDialog</c> ab dieser Welle anbietet.
        /// </summary>
        [Fact]
        public void P06_Mit_uebersteuerten_Optionen_kommt_die_volle_Reihe()
        {
            string pfad = Probe("p06_viertelstunden_einspaltig_komma.txt");
            GanglinienImportOptionen o = new GanglinienImportOptionen
            {
                Trennzeichen = '\0',
                Dezimaltrenner = ',',
                Kopfzeile = false,
                WertSpalte = 0,
                ZeitSpalte = -1
            };

            GanglinienVorschau v = GanglinienDatei.Vorschau(pfad, o);
            Assert.True(v.Lesbar);
            Assert.Equal(1, v.Spaltenzahl);
            Assert.Equal(GanglinienDatei.VorschauZeilen, v.Zeilen.Count);

            GanglinienRohdaten r = GanglinienDatei.Lies(pfad, o);
            GanglinienPruefErgebnis e = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
            {
                Rohwerte = r.Werte,
                Zeitstempel = r.Zeitstempel,
                Einheit = o.Einheit,
                DeklariertesRaster = o.Raster,
                Konvention = o.Konvention
            });

            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(4, e.Zeitinterval);
            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(223.46, e.Werte[1]);
            Assert.Equal(8024150.999999962, Summe(e.Werte));   // identisch zu P05
        }

        // ==================================================================
        // 7 — Schaltjahr
        // ==================================================================

        [Fact]
        public void P07_Schaltjahr_wird_auf_8760_normalisiert()
        {
            var (_, r, e) = Kette("p07_schaltjahr_stunden_semikolon_kopf.csv");

            Assert.Equal(8784, r.Werte.Length);

            Assert.True(e.Erfolgreich);
            Assert.True(e.SchaltjahrNormalisiert);
            Assert.True(e.BestaetigungNoetig);          // der Anwender bekommt das Protokoll
            Assert.Equal(8760, e.Werte.Length);
            Assert.Equal(1, e.Zeitinterval);
            Assert.Contains("IMPORT_PROT_SCHALTJAHR: 8784; 8760; 24", Zeilen(e.Protokoll));
            Assert.Equal(2003479.3700000064, Summe(e.Werte));
            Assert.Equal(225.34, e.Werte[4000]);        // hinter dem 29.02. verschoben
        }

        // ==================================================================
        // 8 / 9 — die beiden Sommerzeitfaelle
        // ==================================================================

        [Fact]
        public void P08_Sommerzeitluecke_wird_mit_dem_Vorwert_gefuellt()
        {
            var (_, r, e) = Kette("p08_sommerzeit_luecke_stunden.csv");

            Assert.Equal(8759, r.Werte.Length);         // die Datei hat eine Stunde weniger

            Assert.True(e.Erfolgreich);
            Assert.True(e.SommerzeitBehandelt);
            Assert.True(e.BestaetigungNoetig);
            Assert.Equal(8760, e.Werte.Length);
            Assert.Contains("IMPORT_PROT_SOMMERZEIT_LUECKE: 26.03.2023 02:00; 1", Zeilen(e.Protokoll));
            Assert.Equal(2005965.6300000069, Summe(e.Werte));
        }

        [Fact]
        public void P09_Sommerzeitdublette_wird_gemittelt()
        {
            var (_, r, e) = Kette("p09_sommerzeit_dublette_stunden.csv");

            Assert.Equal(8761, r.Werte.Length);         // die Datei hat eine Stunde mehr

            Assert.True(e.Erfolgreich);
            Assert.True(e.SommerzeitBehandelt);
            Assert.Equal(8760, e.Werte.Length);
            Assert.Contains("IMPORT_PROT_SOMMERZEIT_DUBLETTE: 29.10.2023 02:00; 2", Zeilen(e.Protokoll));
            Assert.Equal(2005992.0000000068, Summe(e.Werte));
        }

        // ==================================================================
        // 10 — Einheit kWh je Intervall
        // ==================================================================

        [Fact]
        public void P10_Arbeit_je_Intervall_wird_in_Leistung_umgerechnet()
        {
            var (_, r, e) = Kette("p10_viertelstunden_kwh_je_intervall.csv",
                                  GanglinienEinheit.KilowattstundeJeIntervall);

            Assert.Equal(35040, r.Werte.Length);
            Assert.Equal(55.0, r.Werte[0]);             // kWh je Viertelstunde

            Assert.True(e.Erfolgreich);
            Assert.Contains("IMPORT_PROT_EINHEIT_UMGERECHNET: 4", Zeilen(e.Protokoll));
            Assert.Equal(220.0, e.Werte[0]);            // = 55 kWh * 4 Intervalle je Stunde
            Assert.Equal(223.48, e.Werte[1]);
            Assert.Equal(8024149.919999944, Summe(e.Werte));
        }

        // ==================================================================
        // 11 — Excel
        // ==================================================================

        /// <summary>
        /// <b>Befund W12-B27 haengt an diesem Fall.</b> Vor dieser Welle brach der
        /// Excel-Zweig mit <c>IMPORT_PROT_LESEFEHLER</c> ab: Das Bulk-Read legt sein
        /// Feld eins groesser an, damit es 1-basiert wie Excel angesprochen werden
        /// kann, die drei Leseschleifen zaehlten aber bis <c>GetLength()</c> statt bis
        /// <c>GetLength() - 1</c> und liefen damit ueber das Feld hinaus. Der
        /// Nachweispunkt <c>Umsetzung_iU0_iU1_Nachweise.md:136</c> („Ganglinien-Import
        /// mit .xlsx") stand deshalb bis hierher offen — mit dieser Probe ist er
        /// belegt.
        /// </summary>
        [Fact]
        public void P11_Excelmappe_wird_gelesen_und_liefert_dieselbe_Reihe_wie_die_CSV()
        {
            var (v, r, e) = Kette("p11_stunden_excel.xlsx");

            Assert.True(v.IstExcel);
            Assert.Equal(new[] { "Lastgang", "Notizen" }, v.Blaetter);
            Assert.Equal("Lastgang", v.Vorschlag.Blattname);   // ohne Angabe das erste Blatt
            Assert.True(v.Vorschlag.Kopfzeile);
            Assert.Equal(0, v.Vorschlag.ZeitSpalte);
            Assert.Equal(1, v.Vorschlag.WertSpalte);

            Assert.Equal(8760, r.Werte.Length);
            Assert.Equal(8760, r.Zeitstempel.Length);
            Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0), r.Zeitstempel[0]);

            Assert.True(e.Erfolgreich);
            Assert.Equal(1, e.Zeitinterval);
            Assert.Equal(8760, e.Werte.Length);
            // Zahlengleich mit P01 — dieselben Werte, nur in einer Mappe.
            Assert.Equal(220.0, e.Werte[0]);
            Assert.Equal(232.23, e.Werte[1]);
            Assert.Equal(280.57, e.Werte[100]);
            Assert.Equal(2005977.0000000068, Summe(e.Werte));
        }

        // ==================================================================
        // 12 — 525 600 Minutenwerte (zur Laufzeit erzeugt)
        // ==================================================================

        /// <summary>
        /// Die Minutenprobe wird ERZEUGT statt abgelegt: 525 600 Zeilen sind rund
        /// 3,5 MB, die bei jedem Auschecken mitkaemen. Die Formel ist Teil des
        /// Tests und damit genauso eingefroren wie eine Datei.
        /// </summary>
        private static string MinutenProbe()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-w12-minutenprobe.txt");
            if (File.Exists(pfad) && new FileInfo(pfad).Length > 3_000_000) return pfad;

            using (StreamWriter w = new StreamWriter(pfad, false))
                for (int i = 0; i < 525600; i++)
                {
                    double v = 220.0
                             + 120.0 * Math.Sin(2.0 * Math.PI * i / 525600.0)
                             + 45.0 * Math.Sin(2.0 * Math.PI * (i % 1440) / 1440.0)
                             + (i % 37) * 0.1;
                    w.Write(Math.Round(v, 2, MidpointRounding.AwayFromZero)
                                .ToString("0.00", CultureInfo.InvariantCulture) + "\r\n");
                }
            return pfad;
        }

        [Fact]
        public void P12_Minutenreihe_wird_auf_Viertelstunden_gemittelt()
        {
            string pfad = MinutenProbe();

            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
            Assert.True(v.Lesbar);
            Assert.Equal('\0', v.Vorschlag.Trennzeichen);
            Assert.Equal('.', v.Vorschlag.Dezimaltrenner);

            GanglinienRohdaten r = GanglinienDatei.Lies(pfad, v.Vorschlag);
            Assert.Equal(525600, r.Werte.Length);
            Assert.Null(r.Zeitstempel);

            GanglinienPruefErgebnis e = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
            {
                Rohwerte = r.Werte,
                Zeitstempel = r.Zeitstempel,
                Einheit = v.Vorschlag.Einheit,
                DeklariertesRaster = v.Vorschlag.Raster,
                Konvention = v.Vorschlag.Konvention
            });

            Assert.True(e.Erfolgreich);
            Assert.True(e.Gemittelt);
            Assert.True(e.BestaetigungNoetig);
            Assert.Equal(35040, e.Werte.Length);
            Assert.Equal(4, e.Zeitinterval);            // Minutenreihen landen als Viertelstunden
            Assert.Contains("IMPORT_PROT_MINUTEN_GEMITTELT: 525600; 35040", Zeilen(e.Protokoll));

            Assert.Equal(222.08399999999997, e.Werte[0]);
            Assert.Equal(226.54266666666666, e.Werte[1]);
            Assert.Equal(237.82799999999997, e.Werte[100]);
            Assert.Equal(262.00533333333334, e.Werte[4000]);
            Assert.Equal(219.11866666666668, e.Werte[35039]);
            Assert.Equal(7771870.900000026, Summe(e.Werte));
        }

        // ==================================================================
        // Randfaelle der Leseschicht
        // ==================================================================

        [Fact]
        public void Fehlende_Datei_meldet_den_Fehlerschluessel_statt_zu_werfen()
        {
            GanglinienVorschau v = GanglinienDatei.Erkenne(Path.Combine(Path.GetTempPath(), "gibt-es-nicht.csv"));

            Assert.False(v.Lesbar);
            Assert.Single(v.Meldungen);
            Assert.Equal(PruefStufe.Fehler, v.Meldungen[0].Stufe);
            Assert.Equal(GanglinienDatei.SchluesselDateiFehlt, v.Meldungen[0].Schluessel);
        }

        [Fact]
        public void Vorschau_zeigt_hoechstens_zehn_Zeilen_und_die_erste_ist_die_Kopfzeile()
        {
            GanglinienVorschau v = GanglinienDatei.Erkenne(Probe("p01_stunden_semikolon_komma_kopf.csv"));

            Assert.Equal(10, GanglinienDatei.VorschauZeilen);
            Assert.Equal(10, v.Zeilen.Count);
            Assert.Equal("Zeitstempel", v.Zeilen[0][0]);
            Assert.Equal("Leistung kW", v.Zeilen[0][1]);
            Assert.Equal("01.01.2023 00:00", v.Zeilen[1][0]);
            Assert.Equal("220,00", v.Zeilen[1][1]);
        }

        [Fact]
        public void IstExcelDatei_trennt_die_Mappen_von_den_Textdateien()
        {
            Assert.True(GanglinienDatei.IstExcelDatei("x.xlsx"));
            Assert.True(GanglinienDatei.IstExcelDatei("x.XLSM"));
            Assert.True(GanglinienDatei.IstExcelDatei("x.xls"));
            Assert.True(GanglinienDatei.IstExcelDatei("x.xlsb"));
            Assert.False(GanglinienDatei.IstExcelDatei("x.csv"));
            Assert.False(GanglinienDatei.IstExcelDatei("x.txt"));
            Assert.False(GanglinienDatei.IstExcelDatei(""));
        }

        [Fact]
        public void TrennzeichenText_nennt_das_leere_Trennzeichen_und_den_Tabulator()
        {
            Assert.Equal("-", GanglinienDatei.TrennzeichenText('\0'));
            Assert.Equal("TAB", GanglinienDatei.TrennzeichenText('\t'));
            Assert.Equal(";", GanglinienDatei.TrennzeichenText(';'));
        }
    }
}
