using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Klimaexport</b> (Auftrag KD-5): Schreiber <see cref="KlimaCsv"/> mit <see cref="KlimaExportOptionen"/> —
    /// Trennzeichen × Dezimalzeichen × Zeitspalte, Kopfarten, Herkunftsblock, Altbestand ohne Rohdaten, Zeitbezug in
    /// beide Richtungen, Taupunkt, Rundlauf der Rechenspalten und der Leseweg <see cref="KlimaExportCtrl"/>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaCsvTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        // =====================================================================
        //  Format
        // =====================================================================

        public static IEnumerable<object[]> Formate()
        {
            foreach (KlimaTrennzeichen t in Enum.GetValues<KlimaTrennzeichen>())
                foreach (KlimaDezimalzeichen d in Enum.GetValues<KlimaDezimalzeichen>())
                    foreach (KlimaZeitspalte z in Enum.GetValues<KlimaZeitspalte>())
                        yield return new object[] { t, d, z };
        }

        [Theory]
        [MemberData(nameof(Formate))]
        public void Trennzeichen_Dezimalzeichen_und_Zeitspalte(KlimaTrennzeichen t, KlimaDezimalzeichen d, KlimaZeitspalte z)
        {
            var o = new KlimaExportOptionen
            {
                Trennzeichen = t, Dezimalzeichen = d, Zeitspalte = z, Herkunft = false,
                Zeitbezug = KlimaZeitbezug.Utc, Parameter = new List<string> { KlimaExportOptionen.R_TEMPERATUR }
            };
            KlimaExportDaten daten = Probe(rohdaten: false);
            if (o.Widerspruechlich)
            {
                // Komma neben Komma ergibt keine lesbare Datei: benannt verweigert.
                var ex = Assert.Throws<ArgumentException>(() => KlimaCsv.Text(daten, o));
                Assert.Contains(R.KEXP_DZ_KONFLIKT, ex.Message);
                return;
            }
            string[] zeilen = Zeilen(KlimaCsv.Text(daten, o));
            Assert.Equal(1 + 8760, zeilen.Length);
            string sep = o.TrennzeichenText;
            string[] kopf = zeilen[0].Split(sep);
            int zeitspalten = z switch { KlimaZeitspalte.Keine => 0, KlimaZeitspalte.StundeUndDatum => 2, _ => 1 };
            Assert.Equal(zeitspalten + 1, kopf.Length);
            Assert.Equal(R.KEXP_G_TEMPERATUR + " [°C]", kopf[^1]);

            // Zeile 2 = Stunde 2 (01:00 bis 02:00 UTC): Temperatur 0,5 °C.
            string[] h2 = zeilen[2].Split(sep);
            Assert.Equal(zeitspalten + 1, h2.Length);
            Assert.Equal(d == KlimaDezimalzeichen.Komma ? "0,5" : "0.5", h2[^1]);
            switch (z)
            {
                case KlimaZeitspalte.Stunde: Assert.Equal("2", h2[0]); break;
                case KlimaZeitspalte.DatumUhrzeit: Assert.Equal("01.01. 01:00", h2[0]); break;
                case KlimaZeitspalte.StundeUndDatum: Assert.Equal(new[] { "2", "01.01. 01:00" }, h2.Take(2)); break;
                case KlimaZeitspalte.Iso: Assert.Equal("2025-01-01T01:00Z", h2[0]); break;
            }
            // Letzte Zeile = 31.12. 23:00.
            if (z == KlimaZeitspalte.DatumUhrzeit) Assert.StartsWith("31.12. 23:00", zeilen[^1]);
            if (z == KlimaZeitspalte.Iso) Assert.StartsWith("2025-12-31T23:00Z", zeilen[^1]);
        }

        [Fact]
        public void Einheitenzeile_ohne_Kopf_und_Vorgabe_eine_Zeile()
        {
            KlimaExportDaten daten = Probe(rohdaten: false);
            var o = new KlimaExportOptionen { Herkunft = false, Kopfart = KlimaKopfart.MitEinheitenzeile };
            string[] z = Zeilen(KlimaCsv.Text(daten, o));
            Assert.Equal(2 + 8760, z.Length);
            Assert.Equal(new[] { R.KEXP_SP_STUNDE, R.KEXP_G_TEMPERATUR, R.KEXP_G_LUFTFEUCHTE, R.KEXP_G_GLOBAL,
                                 R.KEXP_G_DIREKT_NORMAL, R.KEXP_G_DIFFUS }, z[0].Split(';'));
            Assert.Equal(new[] { "", "°C", "%", "W/m²", "W/m²", "W/m²" }, z[1].Split(';'));

            o.Kopfart = KlimaKopfart.Ohne;
            z = Zeilen(KlimaCsv.Text(daten, o));
            Assert.Equal(8760, z.Length);
            Assert.StartsWith("1;", z[0]);

            // Vorgabe: Semikolon, Komma, laufende Stunde, MEZ (KD-Q8), eine Kopfzeile, Herkunft ja.
            var v = new KlimaExportOptionen();
            Assert.Equal((KlimaTrennzeichen.Semikolon, KlimaDezimalzeichen.Komma, KlimaZeitspalte.Stunde, KlimaZeitbezug.Mez,
                          KlimaKopfart.EineZeile, true),
                         (v.Trennzeichen, v.Dezimalzeichen, v.Zeitspalte, v.Zeitbezug, v.Kopfart, v.Herkunft));
            Assert.Equal(KlimaExportOptionen.VORGABE_PARAMETER, v.Parameter);
        }

        [Fact]
        public void Herkunftsblock_nennt_Region_Quelle_Ausfuehrung_Koordinaten_Hoehe_Zeitbezug_und_Lizenz()
        {
            KlimaExportDaten daten = Probe(rohdaten: true);
            var o = new KlimaExportOptionen { Parameter = new List<string> { KlimaExportOptionen.R_TEMPERATUR, KlimaExportOptionen.A_TAUPUNKT } };
            string[] z = Zeilen(KlimaCsv.Vorschau(daten, o));
            string[] block = z.TakeWhile(l => l.StartsWith("# ", StringComparison.Ordinal)).ToArray();
            Assert.Contains("# " + R.KEXP_KOPF_REGION + ": Probeort", block);
            Assert.Contains("# " + R.KEXP_KOPF_QUELLE + ": TRY_Datei", block);
            Assert.Contains("# " + R.KEXP_KOPF_AUSFUEHRUNG + ": TRY_probe.dat", block);
            Assert.Contains("# " + R.KEXP_KOPF_KOORDINATEN + ": 48.78 / 9.18", block);
            Assert.Contains("# " + R.KEXP_KOPF_HOEHE + ": 245 m", block);
            Assert.Contains("# " + R.KEXP_KOPF_ZEITBEZUG + ": " + R.KEXP_ZB_MEZ, block);
            Assert.Contains("# " + R.KEXP_KOPF_LIZENZ + ": " + R.KEXP_LIZENZ_DWD, block);
            Assert.Contains(block, l => l.Contains(R.KEXP_GERECHNET_KOPF, StringComparison.Ordinal) && l.Contains(R.KEXP_G_TAUPUNKT, StringComparison.Ordinal));
            // Vorschau: Block, Kopf und fünf Datenzeilen.
            Assert.Equal(block.Length + 1 + KlimaCsv.VORSCHAU_ZEILEN, z.Length);
            // Kein #-Kopf trägt das Trennzeichen.
            o.Trennzeichen = KlimaTrennzeichen.Komma;
            o.Dezimalzeichen = KlimaDezimalzeichen.Punkt;
            foreach (string l in Zeilen(KlimaCsv.Vorschau(daten, o)).Where(l => l.StartsWith("#", StringComparison.Ordinal)))
                Assert.DoesNotContain(",", l);
        }

        [Fact]
        public void Altbestand_ohne_Rohdaten_schreibt_nur_Rechenspalten_und_nennt_den_Grund()
        {
            KlimaExportDaten daten = Probe(rohdaten: false);
            KlimaExportGroesse roh = KlimaExportOptionen.Groesse(KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_LUFTDRUCK);
            Assert.Equal(R.KEXP_GRUND_OHNE_ROHDATEN, daten.Sperrgrund(roh));
            var o = new KlimaExportOptionen
            {
                Herkunft = false,
                Parameter = KlimaExportOptionen.Groessen.Select(g => g.Schluessel).ToList()
            };
            IReadOnlyList<KlimaExportGroesse> spalten = KlimaCsv.Spalten(daten, o);
            Assert.DoesNotContain(spalten, g => g.Art == KlimaWertart.Rohwert);
            Assert.Equal(12 + 1, spalten.Count); // zwölf Rechenspalten und der Taupunkt
            string kopf = Zeilen(KlimaCsv.Vorschau(daten, o))[0];
            Assert.DoesNotContain(string.Format(R.KEXP_ROH_NAME, ""), kopf);

            // Nur Rohwerte gewählt: nichts zu schreiben, benannt verweigert.
            o.Parameter = new List<string> { roh.Schluessel };
            var ex = Assert.Throws<ArgumentException>(() => KlimaCsv.Text(daten, o));
            Assert.Contains(R.KEXP_KEINE_PARAMETER, ex.Message);
        }

        [Fact]
        public void Rohwerte_mit_Rohdaten_und_Groesse_die_die_Quelle_nicht_fuehrt()
        {
            KlimaExportDaten daten = Probe(rohdaten: true);
            KlimaExportGroesse druck = KlimaExportOptionen.Groesse(KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_LUFTDRUCK);
            KlimaExportGroesse global = KlimaExportOptionen.Groesse(KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_GLOBAL);
            KlimaExportGroesse il = KlimaExportOptionen.Groesse(KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_QUALITAET);
            Assert.Equal("", daten.Sperrgrund(druck));
            Assert.Equal(R.KEXP_GRUND_NICHT_GEFUEHRT, daten.Sperrgrund(global)); // TRY führt G(h) nicht
            var o = new KlimaExportOptionen
            {
                Herkunft = false, Zeitbezug = KlimaZeitbezug.Mez,
                Parameter = new List<string> { druck.Schluessel, global.Schluessel, il.Schluessel }
            };
            string[] z = Zeilen(KlimaCsv.Vorschau(daten, o));
            Assert.Equal(new[] { R.KEXP_SP_STUNDE, druck.Kopfname + " [hPa]", il.Kopfname }, z[0].Split(';'));
            Assert.Equal("1;1001,0;1", z[1]); // Rohwerte im MEZ-Raster ungedreht, Ganzzahl ohne Nachkomma
        }

        // =====================================================================
        //  Zeitbezug und Taupunkt
        // =====================================================================

        [Fact]
        public void Zeitbezug_UTC_nach_MEZ_und_MEZ_nach_UTC()
        {
            double[] utc = Enumerable.Range(0, 8760).Select(i => (double)i).ToArray();
            double[] mez = KlimaCsv.Verschieben(utc, KlimaZeitbezug.Utc, KlimaZeitbezug.Mez);
            // Die erste MEZ-Stunde (0 bis 1 Uhr MEZ) ist die letzte UTC-Stunde des Jahres (zyklisch).
            Assert.Equal(8759.0, mez[0]);
            Assert.Equal(0.0, mez[1]);
            Assert.Equal(8758.0, mez[8759]);
            double[] zurueck = KlimaCsv.Verschieben(mez, KlimaZeitbezug.Mez, KlimaZeitbezug.Utc);
            Assert.Equal(utc, zurueck);
            Assert.Equal(utc, KlimaCsv.Verschieben(utc, KlimaZeitbezug.Utc, KlimaZeitbezug.Utc));

            // Dieselbe Drehung wie der TRY-Import (Konzept Klimadatenquellen § 5): utc[u] = mez[(u + 1) % 8760].
            for (int u = 0; u < 8760; u += 997) Assert.Equal(mez[(u + 1) % 8760], zurueck[u]);

            // Im Schreiber: Rechenwerte (UTC) unter MEZ eine Stunde später, Rohwerte (MEZ) unter UTC eine früher.
            KlimaExportDaten daten = Probe(rohdaten: true);
            var o = new KlimaExportOptionen
            {
                Herkunft = false, Zeitbezug = KlimaZeitbezug.Mez,
                Parameter = new List<string> { KlimaExportOptionen.R_TEMPERATUR, KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_LUFTDRUCK }
            };
            string[] z = Zeilen(KlimaCsv.Vorschau(daten, o));
            Assert.Equal("2;0,0;1002,0", z[2]); // Rechen-UTC-Stunde 1 (Wert 0) steht in MEZ-Stunde 2
            Assert.Equal("1;4379,5;1001,0", z[1]); // MEZ-Stunde 1 = letzte UTC-Stunde (8759 · 0,5)
            o.Zeitbezug = KlimaZeitbezug.Utc;
            z = Zeilen(KlimaCsv.Vorschau(daten, o));
            Assert.Equal("1;0,0;1002,0", z[1]); // Roh-MEZ-Stunde 2 steht in UTC-Stunde 1
            Assert.Equal("2;0,5;1003,0", z[2]);
        }

        [Theory]
        [InlineData(20.0, 50.0, 9.26)]
        [InlineData(0.0, 100.0, 0.0)]
        [InlineData(-10.0, 80.0, -12.8)]
        [InlineData(30.0, 70.0, 23.9)]
        public void Taupunkt_nach_Magnus(double t, double rf, double erwartet)
        {
            Assert.Equal(erwartet, KlimaCsv.Taupunkt(t, rf), 1);
        }

        [Fact]
        public void Taupunkt_ist_gerechnet_gekennzeichnet_und_leer_ohne_Feuchte()
        {
            Assert.True(double.IsNaN(KlimaCsv.Taupunkt(10, 0)));
            Assert.True(double.IsNaN(KlimaCsv.Taupunkt(double.NaN, 50)));
            KlimaExportGroesse tp = KlimaExportOptionen.Groesse(KlimaExportOptionen.A_TAUPUNKT);
            Assert.True(tp.Gerechnet);
            Assert.Equal(KlimaWertart.Abgeleitet, tp.Art);
            Assert.True(KlimaExportOptionen.Groesse(KlimaExportOptionen.F_SUED).Gerechnet);
            Assert.False(KlimaExportOptionen.Groesse(KlimaExportOptionen.R_TEMPERATUR).Gerechnet);
        }

        // =====================================================================
        //  Rundlauf und Leseweg
        // =====================================================================

        [Fact]
        public void Rundlauf_der_Rechenspalten_aus_der_Testdatenbank()
        {
            if (!_db.Vorhanden) return;
            KlimaExportDaten daten = KlimaExportCtrl.Lesen(1);
            Assert.NotNull(daten);
            Assert.False(daten.Rohdaten); // Altbestand der Testdatenbank (KD-Q7: nichts nachgeladen)
            Assert.True(daten.HatStunden);
            Assert.Equal(8760, daten.Rechenwerte["Temperatur"].Length);

            var o = new KlimaExportOptionen
            {
                Herkunft = true, Zeitbezug = KlimaZeitbezug.Utc, Trennzeichen = KlimaTrennzeichen.Tabulator,
                Dezimalzeichen = KlimaDezimalzeichen.Punkt, VolleGenauigkeit = true,
                Parameter = KlimaExportOptionen.Groessen.Where(g => g.Art == KlimaWertart.Rechenwert).Select(g => g.Schluessel).ToList()
            };
            string[] z = Zeilen(KlimaCsv.Text(daten, o)).Where(l => !l.StartsWith("#", StringComparison.Ordinal)).ToArray();
            Assert.Equal(1 + 8760, z.Length);
            List<KlimaExportGroesse> spalten = KlimaCsv.Spalten(daten, o).ToList();
            for (int i = 0; i < 8760; i++)
            {
                string[] zellen = z[i + 1].Split('\t');
                for (int k = 0; k < spalten.Count; k++)
                {
                    double soll = daten.Rechenwerte[spalten[k].Spalte][i];
                    if (!double.IsFinite(soll)) { Assert.Equal("", zellen[k + 1]); continue; }
                    Assert.Equal(soll, double.Parse(zellen[k + 1], CultureInfo.InvariantCulture)); // bitgleich
                }
            }
        }

        [Fact]
        public void Leseweg_liest_Rohwerte_nur_mit_Kennzeichen()
        {
            if (!_db.Vorhanden) return;
            Assert.Null(KlimaExportCtrl.Lesen("gibt es nicht"));
            int id = 1;
            var reihe = new KlimaRohreihe { Zeitbezug = KlimaRohwerteSchema.ZEITBEZUG_UTC };
            for (int h = 1; h <= 8760; h++)
                reihe.Stunden.Add(new KlimaRohstunde { Stunde = h, Temperatur = h * 0.1, Luftdruck_hPa = 1000 + h % 10 });
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.Equal(8760, KlimaRohwerteSchema.RohwerteSchreiben(v, id, reihe));
                v.Commit();
            }
            KlimaExportDaten mit = KlimaExportCtrl.Lesen(id);
            Assert.True(mit.Rohdaten);
            Assert.Equal(KlimaRohwerteSchema.ZEITBEZUG_UTC, mit.RohZeitbezug);
            Assert.Equal(1001.0, mit.Rohwerte[KlimaRohwerteSchema.SPALTE_LUFTDRUCK][0]);
            Assert.True(double.IsNaN(mit.Rohwerte[KlimaRohwerteSchema.SPALTE_WASSERDAMPF][0]));
            Assert.Equal(R.KEXP_GRUND_NICHT_GEFUEHRT,
                         mit.Sperrgrund(KlimaExportOptionen.Groesse(KlimaExportOptionen.ROH_PREFIX + KlimaRohwerteSchema.SPALTE_WASSERDAMPF)));

            // Kennzeichen zurück auf 0: die Zeilen bleiben, der Export liest sie nicht.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Klimaregion_STAMM SET Rohdaten = 0 WHERE ID_Klimaregion = ?", new DbParam("@id", id));
            Assert.False(KlimaExportCtrl.Lesen(id).Rohdaten);
        }

        // =====================================================================

        /// <summary>
        /// Eine erfundene Region: Rechenwerte (UTC) Temperatur = Stunde · 0,5, Feuchte 50 %; mit Rohdaten (MEZ, TRY-Art)
        /// Luftdruck 1000 + Stunde, Qualitätskennung 1, Globalstrahlung nicht geführt.
        /// </summary>
        private static KlimaExportDaten Probe(bool rohdaten)
        {
            var d = new KlimaExportDaten
            {
                IdRegion = 99, Name = "Probeort", Quelle = "TRY_Datei", Ausfuehrung = "TRY_probe.dat",
                Latitude = 48.78, Longitude = 9.18, StandortHoeheM = 245, Rohdaten = rohdaten,
                RohZeitbezug = KlimaRohwerteSchema.ZEITBEZUG_MEZ
            };
            foreach (KlimaExportGroesse g in KlimaExportOptionen.Groessen.Where(g => g.Art == KlimaWertart.Rechenwert))
                d.Rechenwerte[g.Spalte] = Enumerable.Range(0, 8760).Select(i => g.Spalte == "Luftfeuchte" ? 50.0 : i * 0.5).ToArray();
            if (rohdaten)
            {
                foreach (string s in KlimaRohwerteSchema.ROHSPALTEN)
                    d.Rohwerte[s] = Enumerable.Range(0, 8760).Select(i => s == KlimaRohwerteSchema.SPALTE_GLOBAL ? double.NaN
                                                                          : s == KlimaRohwerteSchema.SPALTE_QUALITAET ? 1.0
                                                                          : 1000.0 + i + 1).ToArray();
            }
            return d;
        }

        private static string[] Zeilen(string text)
            => text.EndsWith("\r\n", StringComparison.Ordinal) ? text[..^2].Split("\r\n") : text.Split("\r\n");
    }
}
