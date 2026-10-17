using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle EB, Schritt EB-2 — die Ergebnisreihen in der Oberfläche</b> (Anwenderentscheide 17.10.2026): die
    /// BHKW-Einspeisung („Stromeinspeisung BHKW“) und die PV-Abregelung als zuschaltbare Reihen im Stromgang (ab Werk aus,
    /// CSV folgt der Wahl), dazu die drei Kennzahlen der Übersicht „Ergebnisse – Energie“ in der Gruppe Strom samt dem
    /// Rückfall der Einspeisung auf die Strommatrix bei einem Altergebnis (EB-Q3) und „—“ ohne Wert.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErgebnisreihenOberflaecheTests : IDisposable
    {
        private const int PV_BHKW = 1018;
        private const int PV_GRENZE = 1040;
        private const int SPEICHER_STANDBY = 1007;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-eb2-" + Guid.NewGuid().ToString("N"));

        public ErgebnisreihenOberflaecheTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            _kultur.Dispose();
            try { Directory.Delete(_ordner, recursive: true); } catch (IOException) { }
        }

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";
            public string DateiSpeichern(string titel, string filter, string vorschlag) => Antwort;
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => true;
        }

        private static async Task<SimulationErgebnisDienste> Gelaufen(int projekt)
        {
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, projekt, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Rueckmeldung lauf = await dienste.Laufen!((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            return dienste;
        }

        private static List<string> Namen(Zeichenmodell m) => m.Reihen.Select(r => r.Name).ToList();

        private static Kennzahl K(string schluessel) => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel);

        // =====================================================================
        //  Stromgang: Stromeinspeisung BHKW und Abregelung
        // =====================================================================

        [Fact]
        public async Task Stromgang_fuehrt_die_BHKW_Einspeisung_ab_Werk_aus_und_die_CSV_folgt_der_Wahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = await Gelaufen(PV_BHKW);
            SimulationErgebnisDaten d = dienste.Laden!(PV_BHKW);

            // Die Wahl steht hinter den Netzreihen; ohne Einspeisegrenze gibt es keine Abregelung zu wählen.
            IReadOnlyList<Ganglinienreihe> reihen = d.Stromgang.Reihen;
            Assert.Equal("BHKW_EINSPEISUNG", reihen[reihen.Count - 2].Schluessel);
            Assert.Equal("PV_ABREGELUNG", reihen[reihen.Count - 1].Schluessel);
            Assert.True(reihen[reihen.Count - 2].Vorhanden);
            Assert.False(reihen[reihen.Count - 1].Vorhanden);
            Assert.Equal(R.CHART_LEGENDE_BHKW_EINSPEISUNG, reihen[reihen.Count - 2].Text);

            // Ab Werk (nur Summe) und mit allen alten Reihen: keine neue Linie, die übrigen Reihen gleich.
            string[] alt = { "GESAMT", "PROFIL_LASTGANG", "BHKW_STROM", "PV", "NETZBEZUG", "NETZEINSPEISUNG" };
            Zeichenmodell vorher = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1, alt))!;
            Assert.DoesNotContain(R.CHART_LEGENDE_BHKW_EINSPEISUNG, Namen(vorher));
            Zeichenmodell an = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1,
                alt.Concat(new[] { "BHKW_EINSPEISUNG" }).ToArray()))!;
            Assert.Contains(R.CHART_LEGENDE_BHKW_EINSPEISUNG, Namen(an));
            Assert.Equal(Namen(vorher), Namen(an).Where(n => n != R.CHART_LEGENDE_BHKW_EINSPEISUNG).ToList());
            foreach (var r in vorher.Reihen)
                Assert.Equal(r.Werte, an.Reihen.Single(x => x.Name == r.Name).Werte);

            // CSV: eine Spalte mehr, nur mit Wahl.
            string ohne = Path.Combine(_ordner, "ohne.csv");
            Dienste.Datei = new Dateiprobe { Antwort = ohne };
            dienste.CsvStromgang!(new[] { "PROFIL_LASTGANG", "NETZEINSPEISUNG" });
            string kopfOhne = File.ReadLines(ohne, Encoding.UTF8).First();
            Assert.DoesNotContain(R.CHART_LEGENDE_BHKW_EINSPEISUNG, kopfOhne);

            string mit = Path.Combine(_ordner, "mit.csv");
            Dienste.Datei = new Dateiprobe { Antwort = mit };
            dienste.CsvStromgang!(new[] { "PROFIL_LASTGANG", "NETZEINSPEISUNG", "BHKW_EINSPEISUNG" });
            string kopfMit = File.ReadLines(mit, Encoding.UTF8).First();
            Assert.Contains(R.CHART_LEGENDE_BHKW_EINSPEISUNG, kopfMit);
            Assert.Equal(kopfOhne.Split(';').Length + 1, kopfMit.Split(';').Length);
        }

        /// <summary>Die Reihen sind keine neue Rechnung: ihre Summen treffen die Jahreswerte des Laufs.</summary>
        [Fact]
        public void Die_Reihen_treffen_die_Jahreswerte_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(PV_BHKW, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationErgebnisCtrl.Netzreihen netz = SimulationErgebnisCtrl.NetzreihenViertelstunde(r.sim);
            Assert.NotNull(netz.BhkwEinspeisungKw);
            Assert.Equal(35040, netz.BhkwEinspeisungKw.Length);
            double bhkw = SimulationErgebnisCtrl.BhkwEinspeisungMwh(r.sim);
            Assert.True(bhkw > 1.0, "Projekt 1018 speist BHKW-Strom ein.");
            Assert.Equal(bhkw, netz.BhkwEinspeisungKw.Sum() / 4000.0, 6);
            // Der Anteil steckt in der Netzeinspeisung - keine zweite Einspeisung.
            Assert.True(netz.BhkwEinspeisungKw.Sum() <= netz.EinspeisungKw.Sum() + 1e-6);
            Assert.Equal(SimulationErgebnisCtrl.PvAbregelungMwh(r.sim), (netz.PvAbregelungKw?.Sum() ?? 0.0) / 4000.0, 6);

            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(PV_GRENZE,
                new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));
            var g = new SimulationRunner();
            Assert.True(g.Simuliere(PV_GRENZE, out fehler), "Lauf gescheitert: " + fehler);
            SimulationErgebnisCtrl.Netzreihen mitGrenze = SimulationErgebnisCtrl.NetzreihenViertelstunde(g.sim);
            double abregelung = SimulationErgebnisCtrl.PvAbregelungMwh(g.sim);
            Assert.True(abregelung > 0.1);
            Assert.Equal(abregelung, mitGrenze.PvAbregelungKw.Sum() / 4000.0, 6);
        }

        [Fact]
        public async Task Mit_Einspeisegrenze_ist_die_Abregelung_waehlbar_und_ab_Werk_aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(PV_GRENZE,
                new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));

            SimulationErgebnisDienste dienste = await Gelaufen(PV_GRENZE);
            SimulationErgebnisDaten d = dienste.Laden!(PV_GRENZE);
            Ganglinienreihe wahl = d.Stromgang.Reihen.Single(r => r.Schluessel == "PV_ABREGELUNG");
            Assert.True(wahl.Vorhanden);
            Assert.Equal(R.CHART_LEGENDE_PV_ABREGELUNG, wahl.Text);

            Zeichenmodell vorgabe = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1, new[] { "GESAMT" }))!;
            Assert.DoesNotContain(R.CHART_LEGENDE_PV_ABREGELUNG, Namen(vorgabe));
            Zeichenmodell an = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1,
                new[] { "GESAMT", "PV_ABREGELUNG" }))!;
            Assert.Contains(R.CHART_LEGENDE_PV_ABREGELUNG, Namen(an));

            string mit = Path.Combine(_ordner, "abregelung.csv");
            Dienste.Datei = new Dateiprobe { Antwort = mit };
            dienste.CsvStromgang!(new[] { "PROFIL_LASTGANG", "PV_ABREGELUNG" });
            Assert.Contains(R.CHART_LEGENDE_PV_ABREGELUNG, File.ReadLines(mit, Encoding.UTF8).First());
        }

        /// <summary>
        /// EB-3: die CSV des Stromspeicher-Reiters hängt den Standby des Speichersystems als letzte Spalte an — die
        /// bisherigen Spalten stehen unverändert davor, die Summe trifft den Standby des Laufs.
        /// </summary>
        [Fact]
        public async Task Csv_Stromspeicher_haengt_den_Standby_als_letzte_Spalte_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, SPEICHER_STANDBY, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Rueckmeldung lauf = await dienste.Laufen!((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);

            // Gegenprobe ohne Standby: keine neue Spalte.
            string ohne = Path.Combine(_ordner, "speicher-ohne.csv");
            Dienste.Datei = new Dateiprobe { Antwort = ohne };
            dienste.CsvSpeicher!();
            string[] kopfOhne = File.ReadLines(ohne, Encoding.UTF8).First().Split(';');
            Assert.DoesNotContain(R.SP_CSV_STANDBY, kopfOhne);

            // Der Lauf bekommt 20 W Standby des Speichersystems (die Reihe, aus der auch das Betriebsbild zeichnet):
            // 0,02 kW · 8760 h = 175,2 kWh/a, je Viertelstunde 0,005 kWh.
            var sim = (SimulationControl)typeof(SimulationErgebnisHuelle)
                .GetField("sim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(huelle)!;
            int n = sim.Speicherergebnis.SoCKwh.Length;
            sim.SpeichersystemStandbyAusPvKw = Enumerable.Repeat(0.01, n).ToArray();
            sim.SpeichersystemStandbyAusNetzKw = Enumerable.Repeat(0.01, n).ToArray();
            sim.SpeichersystemEigenverbrauchKwh = 175.2;

            string datei = Path.Combine(_ordner, "speicher.csv");
            Dienste.Datei = new Dateiprobe { Antwort = datei };
            dienste.CsvSpeicher!();

            List<string> zeilen = File.ReadLines(datei, Encoding.UTF8).ToList();
            string[] kopf = zeilen[0].Split(';');
            Assert.Equal(R.SP_CSV_STANDBY, kopf[kopf.Length - 1]);
            Assert.Equal(kopfOhne, kopf.Take(kopf.Length - 1).ToArray());       // die bisherigen Spalten unverändert
            Assert.Contains(R.SP_CSV_SOC, kopf);
            Assert.Single(kopf, k => k == R.SP_CSV_STANDBY);

            // Die Spalte ist dieselbe Reihe wie im Betriebsbild: ihre Summe ist der Standby des Laufs.
            var de = new System.Globalization.CultureInfo("de-DE");
            double summe = 0.0;
            foreach (string z in zeilen.Skip(1))
            {
                string[] f = z.Split(';');
                if (f.Length != kopf.Length) continue;
                if (double.TryParse(f[f.Length - 1], System.Globalization.NumberStyles.Float, de, out double w)) summe += w;
            }
            Assert.Equal(175.2, summe, 1);
        }

        // =====================================================================
        //  Übersicht „Ergebnisse – Energie“: drei Kennzahlen in der Gruppe Strom
        // =====================================================================

        [Fact]
        public void Die_drei_Kennzahlen_stehen_in_der_Gruppe_Strom_seit_Katalog_v27()
        {
            foreach (string s in new[] { KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG,
                                         KennzahlenKatalog.SCHLUESSEL_PV_ABREGELUNG,
                                         KennzahlenKatalog.SCHLUESSEL_SPEICHER_STANDBY })
            {
                Kennzahl k = K(s);
                Assert.Equal(Energievergleich.GRUPPE_STROM, Energievergleich.Gruppe(k));
                Assert.Equal(27, k.Seit);   // EB-4: v26 ist der Hilfsstrom von origin, die Ergebnisreihen tragen v27
                Assert.Contains(Energievergleich.Kennzahlen(), x => x.Schluessel == s);
            }
            Assert.Equal("kWh/a", K(KennzahlenKatalog.SCHLUESSEL_SPEICHER_STANDBY).Einheit);
        }

        /// <summary>Synthetisch: Wert aus der Ergebniszeile, Rückfall Strommatrix (EB-Q3), sonst „—“ (null), nie 0.</summary>
        [Fact]
        public void Einspeisung_BHKW_nimmt_die_Ergebniszeile_dann_die_Strommatrix_sonst_keinen_Wert()
        {
            Kennzahl k = K(KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG);
            var v = new VariantenDaten { Ergebnis = new ErgebnisModel { BHKW = new ErgebnisBHKWModel() } };

            v.Ergebnis.BHKW.StromeinspeisungMwh = 5.32;
            v.BhkwEinspeisungStrommatrixMwh = 9.99;
            Assert.Equal(5.32, k.Wert(v));                  // die Ergebniszeile geht vor

            v.Ergebnis.BHKW.StromeinspeisungMwh = null;
            Assert.Equal(9.99, k.Wert(v));                  // Altergebnis: Strommatrix

            v.BhkwEinspeisungStrommatrixMwh = null;
            Assert.Null(k.Wert(v));                         // keine stille Null

            Assert.Null(k.Wert(new VariantenDaten { Ergebnis = new ErgebnisModel() }));   // ohne BHKW keine Zeile
        }

        [Fact]
        public void Abregelung_und_Standby_nur_mit_Wert_ueber_null()
        {
            Kennzahl ab = K(KennzahlenKatalog.SCHLUESSEL_PV_ABREGELUNG);
            Kennzahl st = K(KennzahlenKatalog.SCHLUESSEL_SPEICHER_STANDBY);
            var v = new VariantenDaten { Ergebnis = new ErgebnisModel { Photovoltaik = new ErgebnisPhotovoltaikModel() } };

            v.Ergebnis.Photovoltaik.AbregelungMwh = 0.0;
            Assert.Null(ab.Wert(v));
            v.Ergebnis.Photovoltaik.AbregelungMwh = 2.5;
            Assert.Equal(2.5, ab.Wert(v));

            Assert.Null(st.Wert(v));                         // ohne Speicherzeile
            v.Ergebnis.Stromspeicher.Add(new ErgebnisStromspeicherModel { EigenverbrauchSystemKwh = null });
            Assert.Null(st.Wert(v));                         // Altergebnis: nicht erhoben
            v.Ergebnis.Stromspeicher.Add(new ErgebnisStromspeicherModel { EigenverbrauchSystemKwh = 438.0 });
            Assert.Equal(438.0, st.Wert(v));
        }

        /// <summary>Über den Ladeweg der Übersicht: gespeicherter Lauf, Altergebnis mit NULL (Rückfall Strommatrix oder „—“).</summary>
        [Fact]
        public void Uebersicht_liest_die_gespeicherten_Werte_und_faellt_beim_Altergebnis_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(PV_GRENZE,
                new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));
            foreach (int projekt in new[] { PV_BHKW, 1046, PV_GRENZE })
            {
                int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(kopf > 0, "Lauf " + projekt + " gescheitert: " + fehler);
            }

            VariantenDaten v1018 = EnergievergleichCtrl.Lade(PV_BHKW, true);
            Assert.Equal(v1018.Ergebnis.BHKW.StromeinspeisungMwh, v1018.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG]);
            Assert.Null(v1018.BhkwEinspeisungStrommatrixMwh);   // kein Rückfall nötig

            VariantenDaten v1046 = EnergievergleichCtrl.Lade(1046, true);
            Assert.Equal(v1046.Ergebnis.Stromspeicher.Single().EigenverbrauchSystemKwh,
                         v1046.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_SPEICHER_STANDBY]);
            Assert.Null(v1046.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_PV_ABREGELUNG]);

            VariantenDaten v1040 = EnergievergleichCtrl.Lade(PV_GRENZE, true);
            Assert.Equal(v1040.Ergebnis.Photovoltaik.AbregelungMwh, v1040.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_PV_ABREGELUNG]);

            // Die Tabelle der Übersicht führt die Zeilen in der Gruppe Strom.
            EnergievergleichErgebnis vgl = Energievergleich.Vergleiche(new[] { v1018 }, false, CultureInfo.GetCultureInfo("de-DE"));
            EnergievergleichZeile zeile = vgl.Zeilen.Single(z => z.Schluessel == KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG);
            Assert.Equal("Einspeisung BHKW", zeile.Bezeichnung);

            // Altergebnis: NULL in der Ergebniszeile - der Wert kommt aus der Strommatrix, ohne Matrix „—“.
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_ErgebnisBHKW\" SET \"Stromeinspeisung\" = NULL WHERE \"ID_Ergebnis\" = ?",
                                           new DbParam("@e", v1018.Ergebnis.ID));
            VariantenDaten alt = EnergievergleichCtrl.Lade(PV_BHKW, true);
            Assert.Null(alt.Ergebnis.BHKW.StromeinspeisungMwh);
            double? matrix = EnergievergleichCtrl.BhkwEinspeisungRueckfall(alt.Ergebnis, PV_BHKW);
            Assert.Equal(matrix, alt.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG]);
            EnergievergleichErgebnis vglAlt = Energievergleich.Vergleiche(new[] { alt, v1046 }, false,
                                                                         CultureInfo.GetCultureInfo("de-DE"));
            EnergievergleichZeile zAlt = vglAlt.Zeilen.Single(z => z.Schluessel == KennzahlenKatalog.SCHLUESSEL_BHKW_EINSPEISUNG);
            Assert.NotEqual("0,0", zAlt.Texte[0]);                // keine stille Null
            if (matrix == null) Assert.Null(zAlt.Werte[0]);
        }
    }
}
