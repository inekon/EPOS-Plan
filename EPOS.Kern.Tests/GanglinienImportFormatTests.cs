using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Formatbeschreibung des Ganglinienimports</b> (IM-4): die Formate selbst, die
    /// Probelesung nach einem Sonderformat (feste Werteanzahl, Temperaturachse, keine Kennzahlen)
    /// und der Lastgang, der seinen Weg unverändert geht und Summe und Höchstleistung nennt.
    /// Ohne Datenbank; die Texte unter de-DE (Kulturvorrichtung).
    /// </summary>
    public class GanglinienImportFormatTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly List<string> _dateien = new();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Eine Datei mit Kopfzeile und <paramref name="anzahl"/> Werten (Semikolon, Komma).</summary>
        private string Datei(int anzahl, Func<int, double> wert)
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-format-" + Guid.NewGuid().ToString("N") + ".csv");
            var zeilen = new List<string> { "Nr;Wert" };
            for (int i = 0; i < anzahl; i++)
                zeilen.Add((i + 1) + ";" + wert(i).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));
            File.WriteAllLines(pfad, zeilen);
            _dateien.Add(pfad);
            return pfad;
        }

        private static GanglinienProbe Probe(string pfad, GanglinienImportFormat format)
        {
            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad, format);
            Assert.True(v.Lesbar);
            return v.Probe;
        }

        // ================================================================== die Formate

        [Fact]
        public void Die_Ganglinie_waehlt_Einheit_und_Raster_und_nennt_Summe_und_Nennleistung()
        {
            GanglinienImportFormat g = GanglinienImportFormat.Ganglinie;
            Assert.True(g.IstGanglinie);
            Assert.Equal("kW", g.Einheit);
            Assert.Equal(0, g.Werteanzahl);
            Assert.True(g.Einheitswahl);
            Assert.True(g.Rasterwahl);
            Assert.True(g.Summe);
            Assert.True(g.Nennleistung);
            Assert.False(g.Jahresfeld);
            Assert.Equal(GanglinienBildart.Jahr, g.Bildart);
            Assert.Equal(Resource.IMPORT_TITEL_OPTIONEN, g.Titel);
        }

        [Theory]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_TAG, 365)]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE, 8760)]
        [InlineData(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT, 12)]
        public void Das_Quellprofil_traegt_Grad_Celsius_die_feste_Werteanzahl_und_keine_Kennzahlen(string betriebsart, int anzahl)
        {
            GanglinienImportFormat q = GanglinienImportFormat.Quellprofil(betriebsart);
            Assert.False(q.IstGanglinie);
            Assert.Equal(GanglinienImportFormat.KENNUNG_QUELLPROFIL, q.Kennung);
            Assert.Equal("°C", q.Einheit);
            Assert.Equal(anzahl, q.Werteanzahl);
            Assert.False(q.Einheitswahl);
            Assert.False(q.Rasterwahl);
            Assert.False(q.Summe);
            Assert.False(q.Nennleistung);
            Assert.False(q.Jahresfeld);
            Assert.Equal(GanglinienBildart.Jahr, q.Bildart);
            Assert.Equal(Farbrolle.QUELLTEMPERATUR, q.Farbrolle);
            Assert.Equal(Resource.CHART_ACHSE_QUELLTEMPERATUR, q.Achsentitel);
            Assert.Equal(Resource.IMPORT_TITEL_QUELLPROFIL, q.Titel);
        }

        // ================================================================== Probe: Quellprofil

        [Fact]
        public void Ein_Quellprofil_mit_365_Tageswerten_wird_gelesen_und_als_Jahresprofil_mit_Temperaturachse_gezeichnet()
        {
            string pfad = Datei(365, i => 5 + i % 7 + 0.25);
            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG));

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(365, p.Werte.Length);
            Assert.Equal(5.25, p.Werte[0], 9);
            Assert.Equal(11.25, p.Werte[6], 9);
            Assert.Equal("", p.Kennzahlen);
            Assert.Equal("365 Datenzeilen gelesen → 365 Werte in °C", p.Zeile);

            // Das Bild zeigt das Jahresprofil der Simulation (8 760 Stunden), die Achse ist die Quelltemperatur.
            string svg = SvgSchreiber.Text(p.Modell);
            Assert.Contains(Resource.CHART_ACHSE_QUELLTEMPERATUR, svg);
            Assert.DoesNotContain(Resource.CHART_ACHSE_LEISTUNG, svg);
        }

        [Theory]
        [InlineData(364)]
        [InlineData(366)]
        [InlineData(8760)]
        public void Eine_abweichende_Werteanzahl_wird_benannt_abgelehnt(int anzahl)
        {
            string pfad = Datei(anzahl, i => 10);
            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG));

            Assert.False(p.Erfolgreich);
            Assert.Null(p.Modell);
            Assert.Empty(p.Werte);
            string n = anzahl.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
            Assert.Equal($"Die Datei liefert {n} Werte, erwartet sind genau 365.", p.Grund);
            Assert.StartsWith(n + " Datenzeilen gelesen → abgelehnt: ", p.Zeile);
        }

        [Fact]
        public void Die_Zeilenzahl_des_Dialogs_schneidet_ein_passendes_Fenster_aus()
        {
            string pfad = Datei(400, i => i);
            var o = GanglinienDatei.Erkenne(pfad).Vorschlag.Kopie();
            o.ErsteDatenzeile = 2 + 10;
            o.Zeilenzahl = 365;
            GanglinienProbe p = GanglinienDatei.Vorschau(pfad, o,
                GanglinienImportFormat.Quellprofil(DbWerte.WQ_PROFIL_BETRIEBSART_TAG)).Probe;

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(10, p.Werte[0], 9);
            Assert.Equal(374, p.Werte[364], 9);
        }

        [Fact]
        public void Eine_eigene_Pruefung_lehnt_benannt_ab_und_eine_Ausnahme_steht_im_Ausnahmeprotokoll()
        {
            string pfad = Datei(12, i => i);
            var abgelehnt = new GanglinienImportFormat
            {
                Kennung = "PRUEFFORMAT", Einheit = "x", Werteanzahl = 12,
                Pruefung = w => w[0] < 1 ? "erster Wert zu klein" : ""
            };
            GanglinienProbe p = Probe(pfad, abgelehnt);
            Assert.False(p.Erfolgreich);
            Assert.Equal("erster Wert zu klein", p.Grund);

            var vermerkt = new List<string>();
            Action<string> mitschreiben = z => { lock (vermerkt) vermerkt.Add(z); };
            Ausnahmeprotokoll.Vermerkt += mitschreiben;
            try
            {
                var wirft = new GanglinienImportFormat
                {
                    Kennung = "WIRFT", Einheit = "x",
                    Pruefung = _ => throw new InvalidOperationException("Probefehler")
                };
                GanglinienProbe q = Probe(pfad, wirft);
                Assert.False(q.Erfolgreich);
                Assert.Equal("Probefehler", q.Grund);
            }
            finally { Ausnahmeprotokoll.Vermerkt -= mitschreiben; }
            lock (vermerkt)
                Assert.Contains(vermerkt, z => z.Contains("WIRFT") && z.Contains("Probefehler"));
        }

        [Fact]
        public void Ein_eigener_Bildzeichner_ersetzt_das_Jahresbild()
        {
            string pfad = Datei(24, i => i);
            Zeichenmodell eigen = ChartRenderer.JahresverlaufModell("Tag", new double[24], "y", Farbrolle.BEDARF);
            var tag = new GanglinienImportFormat
            {
                Kennung = "TAGESBILD", Einheit = "l/h", Werteanzahl = 24, Bildart = GanglinienBildart.Tag,
                Bildzeichner = _ => eigen
            };
            GanglinienProbe p = Probe(pfad, tag);
            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Same(eigen, p.Modell);
        }

        // ================================================================== Probe: Ganglinie

        [Fact]
        public void Die_Ganglinie_geht_ihren_Weg_und_nennt_Jahressumme_und_Hoechstleistung()
        {
            string pfad = Datei(8760, i => i == 100 ? 7.5 : 1.0);
            GanglinienProbe ohne = Probe(pfad, null);
            GanglinienProbe mit = Probe(pfad, GanglinienImportFormat.Ganglinie);

            Assert.True(mit.Erfolgreich, mit.Grund);
            Assert.Equal(ohne.Zeile, mit.Zeile);
            Assert.Equal(ohne.WerteKw, mit.WerteKw);
            Assert.Equal("Jahressumme 8.766 kWh · Höchstleistung 7,5 kW", mit.Kennzahlen);
        }

        // ================================================================== Optionen

        [Fact]
        public void Das_Jahr_reist_mit_der_Kopie_der_Optionen()
        {
            var o = new GanglinienImportOptionen { Jahr = 2025, Zeilenzahl = 8760 };
            GanglinienImportOptionen k = o.Kopie();
            Assert.Equal(2025, k.Jahr);
            Assert.Equal(0, new GanglinienImportOptionen().Jahr);
        }
    

        // ================================================================== Spotpreis

        /// <summary>Eine Spotpreisdatei im Format der Bundesnetzagentur/SMARD: ein Jahr, je Stunde eine Zeile.</summary>
        private string Spotdatei(int jahr, Func<int, double> preis)
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-spot-" + Guid.NewGuid().ToString("N") + ".csv");
            var zeilen = new List<string> { "Datum;von;Zeitzone von;bis;Zeitzone bis;Spotmarktpreis in ct/kWh" };
            var t = new DateTime(jahr, 1, 1);
            for (int i = 0; t.Year == jahr; i++, t = t.AddHours(1))
            {
                DateTime bis = t.AddHours(1);
                zeilen.Add(t.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture) + ";"
                           + t.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) + ";CET;"
                           + bis.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) + ";CET;"
                           + preis(i).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));
            }
            File.WriteAllLines(pfad, zeilen);
            _dateien.Add(pfad);
            return pfad;
        }

        [Fact]
        public void Der_Spotpreis_ist_Preis_je_Stunde_mit_Preisachse_und_Jahresfeld()
        {
            GanglinienImportFormat f = GanglinienImportFormat.Spotpreis();
            Assert.Equal(GanglinienImportFormat.KENNUNG_SPOTPREIS, f.Kennung);
            Assert.Equal(DbWerte.PREISREIHE_EINHEIT_CT_KWH, f.Einheit);
            Assert.Equal("ct/kWh", f.Einheit);
            Assert.Equal(0, f.Werteanzahl);                 // die DATEI ist frei, die Reihe hat 8 760
            Assert.True(f.Jahresfeld);
            Assert.Equal(GanglinienBildart.Jahr, f.Bildart);
            Assert.False(f.Einheitswahl || f.Rasterwahl || f.Summe || f.Nennleistung);
            Assert.Equal(Resource.IMPORT_ACHSE_SPOTPREIS, f.Achsentitel);
            Assert.Equal(Resource.IMPORT_TITEL_SPOTPREIS, f.Titel);
            Assert.NotNull(f.Dateibild);
            Assert.NotNull(f.Jahrvorschlag);
        }

        [Fact]
        public void Der_Spotpreis_liest_genau_die_Reihe_des_bisherigen_Wegs_und_schlaegt_das_Jahr_der_Datei_vor()
        {
            string pfad = Spotdatei(2025, i => 5.0 + (i % 24) * 0.25 - (i == 4000 ? 12.0 : 0.0));
            SpotpreisImportCtrl.Lauf alt = new SpotpreisImportCtrl().Pruefe(pfad);
            Assert.True(alt.Erfolgreich);

            GanglinienVorschau v = GanglinienDatei.Erkenne(pfad, GanglinienImportFormat.Spotpreis());
            Assert.True(v.Lesbar);
            Assert.Equal(2025, v.Vorschlag.Jahr);
            GanglinienProbe p = v.Probe;

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(8760, p.Werte.Length);
            Assert.Equal(alt.Reihe.StundenreiheCtKwh, p.Werte);
            Assert.Equal("8.760 Datenzeilen gelesen → 8.760 Werte in ct/kWh", p.Zeile);
            Assert.StartsWith("Jahr der Datei 2025 · Mittel ", p.Kennzahlen);
            Assert.Contains(Resource.IMPORT_ACHSE_SPOTPREIS, SvgSchreiber.Text(p.Modell));
        }

        [Fact]
        public void Ein_Schaltjahr_mit_8784_Zeilen_wird_wie_bisher_auf_8760_Stundenpreise_gebracht()
        {
            string pfad = Spotdatei(2024, i => 1.0 + i * 0.001);
            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Spotpreis());

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(8784, p.Datenzeilen);
            Assert.Equal(new SpotpreisImportCtrl().Pruefe(pfad).Reihe.StundenreiheCtKwh, p.Werte);
            Assert.Equal(2024, GanglinienDatei.Erkenne(pfad, GanglinienImportFormat.Spotpreis()).Vorschlag.Jahr);
        }

        [Fact]
        public void Eine_Spotpreisdatei_ohne_lesbare_Zeile_wird_benannt_abgelehnt()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-spot-" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllLines(pfad, new[] { "Datum;Wert", "kein;1,5", "Datum;2,5" });
            _dateien.Add(pfad);

            GanglinienProbe p = Probe(pfad, GanglinienImportFormat.Spotpreis());
            Assert.False(p.Erfolgreich);
            Assert.Equal(Resource.PREIS_IMPORT_KEINE_DATEN, p.Grund);
            Assert.Empty(p.Werte);
        }

        [Fact]
        public void Das_Jahr_aus_dem_Dialog_ersetzt_nur_das_Jahr_der_Reihe()
        {
            string pfad = Spotdatei(2025, i => 3.0 + (i % 7));
            var ctrl = new SpotpreisImportCtrl();
            SpotpreisImportCtrl.Lauf alt = ctrl.Pruefe(pfad);

            SpotpreisImportCtrl.Lauf ohne = SpotpreisImportHuelle.Pruefe(ctrl, pfad, 0);
            SpotpreisImportCtrl.Lauf vorschlag = SpotpreisImportHuelle.Pruefe(ctrl, pfad, 2025);
            SpotpreisImportCtrl.Lauf anders = SpotpreisImportHuelle.Pruefe(ctrl, pfad, 2030);

            foreach (SpotpreisImportCtrl.Lauf l in new[] { ohne, vorschlag, anders })
            {
                Assert.True(l.Erfolgreich);
                Assert.Equal(alt.Reihe.StundenreiheCtKwh, l.Reihe.StundenreiheCtKwh);
                Assert.Equal(alt.Protokoll, l.Protokoll);
            }
            Assert.Equal(2025, ohne.Jahr);
            Assert.Equal(2025, vorschlag.Jahr);
            Assert.Equal(2030, anders.Jahr);
        }

        // ================================================================== TWW-Messreihe

        /// <summary>Eine Messreihe ab <paramref name="beginn"/> im Raster <paramref name="minuten"/> (Zeitstempel ISO, kWh).</summary>
        private string Messdatei(DateTime beginn, int minuten, int schritte, Func<int, double> wert, string kopf = "Wert [kWh]")
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-tww-" + Guid.NewGuid().ToString("N") + ".csv");
            var zeilen = new List<string> { "Zeitstempel;" + kopf };
            for (int i = 0; i < schritte; i++)
                zeilen.Add(beginn.AddMinutes((double)i * minuten).ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                           + ";" + wert(i).ToString(System.Globalization.CultureInfo.InvariantCulture));
            File.WriteAllLines(pfad, zeilen);
            _dateien.Add(pfad);
            return pfad;
        }

        private static EPOS.UI.Dialoge.Bedarf.TwwMessreiheneingabeDaten Eingabe(int? groesse = null)
            => new() { GroesseId = groesse, LueckenschwelleAnteil = 0.05 };

        private static Messreihe Alt(string pfad, EPOS.UI.Dialoge.Bedarf.TwwMessreiheneingabeDaten e)
        {
            Messreihe r = Messreihenleser.AusText(File.ReadAllText(pfad), Path.GetFileName(pfad),
                                                  ZapfprofilHuelle.AlsOptionen(e), out ZapfSatz fehler);
            Assert.True(r != null, fehler?.Klartext);
            return r;
        }

        [Fact]
        public void Die_TWW_Messreihe_zeigt_eine_Tagesansicht_mit_freier_Werteanzahl()
        {
            GanglinienImportFormat f = ZapfprofilHuelle.Messformat(Eingabe());
            Assert.Equal(GanglinienImportFormat.KENNUNG_TWW_MESSREIHE, f.Kennung);
            Assert.Equal(GanglinienBildart.Tag, f.Bildart);
            Assert.Equal(0, f.Werteanzahl);
            Assert.False(f.Jahresfeld);
            Assert.Equal(Resource.IMPORT_TITEL_TWW_MESSREIHE, f.Titel);
            Assert.Equal(Resource.IMPORT_ACHSE_TWW_ENERGIE, f.Achsentitel);
            Assert.Equal("", f.Einheit);                    // „aus der Kopfzeile": erst die Datei nennt sie
            GanglinienImportFormat vol = ZapfprofilHuelle.Messformat(Eingabe((int)ZapfMessgroesse.Volumen));
            Assert.Equal("m³", vol.Einheit);
            Assert.Equal(Resource.IMPORT_ACHSE_TWW_VOLUMEN, vol.Achsentitel);
        }

        [Fact]
        public void Die_TWW_Messreihe_liest_wie_das_Einspielen_und_zeigt_den_ersten_vollstaendigen_Tag()
        {
            // 15-Minuten-Reihe ab 06:00 über drei Tage: Der erste volle Tag beginnt am 2. Januar 00:00.
            var beginn = new DateTime(2025, 1, 1, 6, 0, 0);
            string pfad = Messdatei(beginn, 15, 3 * 96, i => i % 4 == 0 ? 0.4 : 0.1);
            var e = Eingabe();
            Messreihe alt = Alt(pfad, e);

            GanglinienProbe p = Probe(pfad, ZapfprofilHuelle.Messformat(e));

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal(alt.MengeJeSchritt, p.Werte);
            Assert.Equal("kWh", p.Einheit);
            Assert.Equal("288 Datenzeilen gelesen → 288 Werte in kWh", p.Zeile);
            Assert.Equal("3,0 Tage gelesen · gezeigt: 02.01.2025", p.Kennzahlen);
            Assert.Equal(72, GanglinienTagesbild.ErsterVollerTag(alt));
            double[] stunden = GanglinienTagesbild.Werte(alt, out DateTime tag);
            Assert.Equal(new DateTime(2025, 1, 2), tag);
            Assert.Equal(24, stunden.Length);
            Assert.All(stunden, s => Assert.Equal(0.7, s, 9));
            string svg = SvgSchreiber.Text(p.Modell);
            Assert.Contains(Resource.IMPORT_ACHSE_TAGESSTUNDE, svg);
            Assert.Contains(Resource.IMPORT_ACHSE_TWW_ENERGIE, svg);
        }

        [Fact]
        public void Eine_Tagesreihe_zeigt_die_Tagesmengen_der_ersten_Tage()
        {
            string pfad = Messdatei(new DateTime(2025, 3, 1), 1440, 40, i => 2.0 + i, "Volumen [m3]");
            GanglinienProbe p = Probe(pfad, ZapfprofilHuelle.Messformat(Eingabe()));

            Assert.True(p.Erfolgreich, p.Grund);
            Assert.Equal("m³", p.Einheit);
            Assert.Equal("40,0 Tage gelesen · gezeigt: die ersten 31 Tage", p.Kennzahlen);
            Assert.Contains(Resource.IMPORT_ACHSE_TAG_DER_REIHE, SvgSchreiber.Text(p.Modell));
        }

        [Fact]
        public void Eine_Ablehnung_des_Messreihenlesers_ist_die_Ablehnung_im_Optionendialog()
        {
            // Ein negativer Wert: Der Leser des Einspielens lehnt ab - mit demselben Satz wie die Prüfung.
            string pfad = Messdatei(new DateTime(2025, 1, 1), 60, 48, i => i == 5 ? -1.0 : 0.5);
            var e = Eingabe();
            GanglinienProbe p = Probe(pfad, ZapfprofilHuelle.Messformat(e));

            Assert.False(p.Erfolgreich);
            Assert.False(string.IsNullOrEmpty(p.Grund));
            Assert.Null(Messreihenleser.AusText(File.ReadAllText(pfad), Path.GetFileName(pfad),
                                                ZapfprofilHuelle.AlsOptionen(e), out ZapfSatz fehler));
            Assert.Equal(ZapfprofilHuelle.Satztext(fehler), p.Grund);
            Assert.Empty(p.Werte);
        }
    }
}
