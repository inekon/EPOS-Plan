using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die lesende Datenseite des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.3, Stufe Z0, Posten P6): Weiche, Verfügbarkeit, Katalog und
    /// Projektdaten. Alle Katalogwerte sind erfunden (<see cref="TwwTestdatenbank"/>); die
    /// Fälle laufen in einer eigenen leeren Datei, einer liest die Testdatenbank nur.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilCtrlTests
    {
        // =================================================================================
        // 1 — Die Weiche
        // =================================================================================

        [Fact]
        public void Ohne_Tabelle_und_ohne_Zeile_gilt_der_Bestandsweg()
        {
            using (new TwwTestdatenbank(mitTwwSchema: false))
                Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(1));

            using (new TwwTestdatenbank())
                Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(1));
        }

        [Fact]
        public void Die_Weiche_liest_Tab_TwwProjekt_je_Projekt()
        {
            using var db = new TwwTestdatenbank();
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\", \"Weg\") VALUES (1, 'GENERATOR')");
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (2)");

            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(1));
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(2));    // Vorgabe der DDL: BESTAND
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(3));    // keine Zeile
        }

        /// <summary>Die Testdatenbank setzt bei keinem Projekt die Weiche (3.4) — nur lesend.</summary>
        [Fact]
        public void In_der_Testdatenbank_steht_kein_Projekt_auf_dem_Generator()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (int projekt in new[] { 1030, 1007, 1017, 1045, 1046 })
            {
                Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(projekt));
                ZapfprofilStand s = ZapfprofilCtrl.Lies(projekt);
                Assert.Equal(BrauchwasserWeg.Bestand, s.Weg);
                Assert.Empty(s.Zonen);
                Assert.Null(s.Projekt);
            }
        }

        // =================================================================================
        // 2 — Verfügbarkeit, benannt
        // =================================================================================

        [Fact]
        public void Ohne_Tabellen_nennt_die_Verfuegbarkeit_jede_fehlende_Tabelle()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();
            Assert.False(v.Ja);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.TabellenFehlen, v.Grund);
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                Assert.Contains(a.Key, v.Klartext);
        }

        [Fact]
        public void Ohne_Katalogversion_ist_der_Generator_benannt_nicht_verfuegbar()
        {
            using var db = new TwwTestdatenbank();

            ZapfVerfuegbarkeit ohne = ZapfprofilCtrl.Verfuegbar();
            Assert.False(ohne.Ja);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.KeineKatalogversion, ohne.Grund);
            Assert.Contains(TwwSchema.TAB_TWW_PARAMETER_STAMM, ohne.Klartext);

            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 1.0, "T1");
            ZapfVerfuegbarkeit mit = ZapfprofilCtrl.Verfuegbar();
            Assert.True(mit.Ja);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.Verfuegbar, mit.Grund);
            Assert.Contains("T1", mit.Klartext);
        }

        // =================================================================================
        // 3 — Der Katalog
        // =================================================================================

        [Fact]
        public void Ohne_Tabellen_ist_der_Katalog_leer()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            Assert.Empty(ZapfprofilCtrl.Katalog());
            Assert.Empty(ZapfprofilCtrl.Tagesgangsaetze());
            Assert.Null(ZapfprofilCtrl.LiesNutzungsart(1));
        }

        [Fact]
        public void Der_Katalog_traegt_Werte_Tagesgaenge_Provenienz_und_Status()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);

            IReadOnlyList<Nutzungsart> katalog = ZapfprofilCtrl.Katalog();
            Assert.Equal(new[] { "Nutzung A", "Nutzung B" }, katalog.Select(n => n.Name).ToArray());

            Nutzungsart na = katalog[0];
            Assert.Equal(a, na.Id);
            Assert.Equal("T1", na.Katalogversion);
            Assert.Equal(ZapfBezugsart.Personen, na.Bezug);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, na.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(50.0, na.Bezugstemperaturen.ZapftemperaturC);
            Assert.Equal(10.0, na.Bezugstemperaturen.KaltwasserC);
            Assert.Equal(ZapfBilanzgrenze.Zapfstelle, na.Grenze);
            Assert.Equal(ZapfKalenderart.Wohnen, na.Kalender);
            Assert.Null(na.Ferienfaktor);
            Assert.Equal(12, na.Monatsfaktoren.Length);
            Assert.All(na.Monatsfaktoren, m => Assert.Equal(1.0, m));
            Assert.Equal(new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 }, na.Wochenfaktoren);
            Assert.Equal(ZapfKatalogstatus.Eigen, na.Status);
            Assert.False(na.ReadOnly);
            Assert.Null(na.IdVorlage);

            // Provenienz je Wertgruppe samt Bandbreite (nur das mittlere Niveau ist gepflegt).
            Assert.Equal(TwwTestdatenbank.QUELLE, na.Herkunft.Bedarf.Quelle);
            Assert.Equal(Herkunftsart.Fiktiv, na.Herkunft.Bedarf.Art);
            Assert.Equal(Herkunftsart.Fiktiv, na.Herkunft.Jahresgang.Art);
            Assert.Equal(Herkunftsart.Fiktiv, na.Herkunft.Wochengang.Art);
            Assert.Equal(new double?[] { null, 1.5, null }, na.Herkunft.Bandbreite.Min);
            Assert.Equal(new double?[] { null, 2.5, null }, na.Herkunft.Bandbreite.Max);
            Assert.Equal(4, na.Herkunft.Tagesgang.Count);

            // Der Tagesgangsatz: vier Tagtypen, je 0,5 in Stunde 7 und 19.
            Assert.Equal(satz, na.Tagesgaenge.Id);
            Assert.True(na.Tagesgaenge.Vollstaendig);
            Assert.Equal("Satz A", na.Tagesgaenge.Bezeichner);
            for (int t = 0; t < 4; t++)
                for (int h = 0; h < 24; h++)
                    Assert.Equal(h == 6 || h == 18 ? 0.5 : 0.0, na.Tagesgaenge.Anteile[t, h]);

            Nutzungsart nb = katalog[1];
            Assert.Equal(b, nb.Id);
            Assert.Equal(ZapfKatalogstatus.Auslieferung, nb.Status);
            Assert.True(nb.ReadOnly);

            // Einzeln gelesen: dieselben Werte.
            Nutzungsart einzeln = ZapfprofilCtrl.LiesNutzungsart(a);
            Assert.Equal(na.Name, einzeln.Name);
            Assert.Equal(na.BedarfJeNiveauKwhJeEinheitTag, einzeln.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(satz, einzeln.Tagesgaenge.Id);
            Assert.Null(ZapfprofilCtrl.LiesNutzungsart(9999));
        }

        [Fact]
        public void Ein_unvollstaendiger_Tagesgangsatz_wird_als_solcher_gemeldet()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz halb", "T1", false, 1, 2);

            Tagesgangsatz s = Assert.Single(ZapfprofilCtrl.Tagesgangsaetze());
            Assert.Equal(satz, s.Id);
            Assert.False(s.Vollstaendig);
            Assert.NotNull(s.JeTagtyp[0]);
            Assert.NotNull(s.JeTagtyp[1]);
            Assert.Null(s.JeTagtyp[2]);
            Assert.Null(s.JeTagtyp[3]);
        }

        // =================================================================================
        // 4 — Die Projektdaten
        // =================================================================================

        [Fact]
        public void Lies_liefert_Zonen_in_Reihenfolge_samt_Wohnungen_und_Projektzeile()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int n = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);

            int zweite = TwwTestdatenbank.ZoneAnlegen(1, n, "Zone zwei", 20.0, reihenfolge: 2);
            int erste = TwwTestdatenbank.ZoneAnlegen(1, n, "Zone eins", 10.0, reihenfolge: 1);
            int fremd = TwwTestdatenbank.ZoneAnlegen(2, n, "Fremde Zone", 5.0);

            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_TwwWohnungstyp\" (\"ID_Zone\", \"Anzahl\", \"Raumzahl\", \"Reihenfolge\") VALUES (?, 4, 3.0, 2), (?, 2, NULL, 1), (?, 1, 2.0, 1)",
                new DbParam("@a", erste), new DbParam("@b", erste), new DbParam("@c", fremd));
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_TwwZone\" SET \"Niveau\" = 3, \"Topologie\" = 2, \"Zirkulation\" = 0, \"Ferienbeginn_1\" = 100, " +
                "\"Ferienende_1\" = 110, \"Auslastung_03\" = 0.5, \"Jahresmesswert\" = 1000.0, \"Jahresmesswert_Einheit\" = 1 WHERE \"ID\" = ?",
                new DbParam("@id", zweite));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\", \"Weg\", \"Seed\", \"Zirk_Lage\", \"Speicher_C\") VALUES (1, 'GENERATOR', 7, 2, 50.0)");

            ZapfprofilStand s = ZapfprofilCtrl.Lies(1);

            Assert.Equal(BrauchwasserWeg.Generator, s.Weg);
            Assert.Equal(new[] { erste, zweite }, s.Zonen.Select(z => z.Id).ToArray());

            ZonenStand z1 = s.Zonen[0];
            Assert.Equal("Zone eins", z1.Name);
            Assert.Equal(n, z1.IdNutzungsart);
            Assert.Null(z1.IdTagesgangsatz);
            Assert.Equal(10.0, z1.Bezugsmenge);
            Assert.Equal(ZapfNiveau.Mittel, z1.Niveau);            // Vorgabe der DDL
            Assert.Equal(ZapfTopologie.Speicher, z1.Topologie);
            Assert.True(z1.Zirkulation);
            Assert.True(z1.TagesbedarfAuto);
            Assert.All(z1.Ferienbeginn, f => Assert.Null(f));
            Assert.All(z1.Auslastung, x => Assert.Null(x));
            Assert.Equal(new[] { 1, 2 }, z1.Wohnungen.Select(w => w.Reihenfolge).ToArray());
            Assert.Null(z1.Wohnungen[0].Raumzahl);
            Assert.Equal(3.0, z1.Wohnungen[1].Raumzahl);
            Assert.Equal(4, z1.Wohnungen[1].Anzahl);

            ZonenStand z2 = s.Zonen[1];
            Assert.Equal(ZapfNiveau.Hoch, z2.Niveau);
            Assert.Equal(ZapfTopologie.Frischwasserstation, z2.Topologie);
            Assert.False(z2.Zirkulation);
            Assert.Equal(100, z2.Ferienbeginn[0]);
            Assert.Equal(110, z2.Ferienende[0]);
            Assert.Null(z2.Ferienbeginn[1]);
            Assert.Equal(0.5, z2.Auslastung[2]);
            Assert.Equal(1000.0, z2.Jahresmesswert);
            Assert.Equal(ZapfMesswerteinheit.KwhJeJahr, z2.JahresmesswertEinheit);
            Assert.Null(z2.JahresmesswertBilanzgrenze);
            Assert.Empty(z2.Wohnungen);

            ProjektStand p = s.Projekt;
            Assert.NotNull(p);
            Assert.Equal(BrauchwasserWeg.Generator, p.Weg);
            Assert.Equal(7, p.Seed);
            Assert.Equal(ZapfLeitungslage.AusserhalbHuelle, p.ZirkLage);
            Assert.Equal(50.0, p.SpeicherC);
            Assert.Null(p.ZirkLaengeM);
            Assert.Null(p.BedarfstagQuelle);
            // Die uebrigen Werte sind die Vorgaben der DDL - hier gelesen, nicht im Code abgeschrieben.
            Assert.Equal(ZapfZirkulationsmethode.Flaechenkennwert, p.ZirkMethode);
            Assert.Equal(ZapfSpeicherart.Ladespeicher, p.Speicherart);

            // Projekt 2 sieht nur seine eine Zone und keine Projektzeile.
            ZapfprofilStand s2 = ZapfprofilCtrl.Lies(2);
            Assert.Equal(BrauchwasserWeg.Bestand, s2.Weg);
            ZonenStand f = Assert.Single(s2.Zonen);
            Assert.Equal(fremd, f.Id);
            Assert.Single(f.Wohnungen);
            Assert.Null(s2.Projekt);
        }

        [Fact]
        public void Ohne_Tabellen_liefert_Lies_den_Bestandsweg_ohne_Zonen()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            ZapfprofilStand s = ZapfprofilCtrl.Lies(1);
            Assert.Equal(BrauchwasserWeg.Bestand, s.Weg);
            Assert.Empty(s.Zonen);
            Assert.Null(s.Projekt);
        }
    }
}
