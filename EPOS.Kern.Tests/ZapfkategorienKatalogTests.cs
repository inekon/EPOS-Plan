using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kategorien als Katalogkopie</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, 3.2; Stufe Z4,
    /// Gruppe 1): Lesen samt Summe, Hinweis und Sperre; der Vorgabesatz aus den freien Kategorien;
    /// Speichern an Ort und Stelle bei einer freien Nutzungsart, als neue Katalogversion bei einer
    /// gesperrten; die Regeln von <see cref="Zapfkategoriensatz.Pruefen"/>; Provenienz je Kategorie.
    /// Jeder Fall in einer eigenen leeren Datei (<see cref="TwwTestdatenbank"/>), alle Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfkategorienKatalogTests
    {
        private static Zapfkategorie K(string name, double v, int dauer, double anteil, double sigma, double? kappung = null)
            => new Zapfkategorie(0, name, v, sigma, dauer, anteil, null) { KappungLJeMin = kappung };

        [Fact]
        public void Lesen_nennt_Kategorien_Summe_Hinweis_und_Sperre()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", "T1", satz);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung benutzt", "T1", satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(frei, "Zweite", 2, 8.0, 5, 0.5, 2.0);
            TwwTestdatenbank.KategorieAnlegen(frei, "Erste", 1, 4.0, 1, 0.3, 1.0);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone", 10.0);
            TwwTestdatenbank.KategorieAnlegen(geliefert, "Einzige", 1, 6.0, 3, 1.0, 1.5, readOnly: true);

            TwwKategorienStand s = TwwNutzungsartCtrl.KategorienLesen(frei);
            Assert.True(s.Frei);
            Assert.Equal(new[] { "Erste", "Zweite" }, s.Kategorien.Select(k => k.Name).ToArray());
            Assert.Equal(0.8, s.SummeAnteil, 12);
            Assert.Equal("KATEGORIE_SUMME_NORMIERT", s.Hinweis.Kennung);
            Assert.Contains("0.8", s.Hinweis.Klartext);

            Assert.Equal(TwwKatalogAusgang.BenutztGesperrt, TwwNutzungsartCtrl.KategorienLesen(benutzt).Sperre);
            TwwKategorienStand g = TwwNutzungsartCtrl.KategorienLesen(geliefert);
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, g.Sperre);
            Assert.Null(g.Hinweis);
            Assert.Equal(TwwKatalogAusgang.NichtGefunden, TwwNutzungsartCtrl.KategorienLesen(9999).Sperre);
        }

        [Fact]
        public void Ohne_Tabelle_nennt_Lesen_und_Speichern_die_fehlenden_Tabellen()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            TwwTestdatenbank.SchemaAnlegen(mitT2: false);
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen, TwwNutzungsartCtrl.KategorienLesen(1).Sperre);
            Assert.Empty(TwwNutzungsartCtrl.KategorienVorgabe());
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen,
                         TwwNutzungsartCtrl.KategorienSpeichern(1, new[] { K("A", 1.0, 1, 1.0, 0.0) }).Ausgang);
        }

        [Fact]
        public void Der_Vorgabesatz_sind_die_freien_Kategorien_der_kleinsten_Nutzungsart()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            Assert.Empty(TwwNutzungsartCtrl.KategorienVorgabe());

            TwwTestdatenbank.KategorieAnlegen(a, "Fiktiv", 1, 3.0, 1, 1.0, 1.0);
            TwwTestdatenbank.KategorieAnlegen(b, "Lang", 2, 8.0, 5, 0.4, 2.0, herkunftsart: TwwSchema.HERKUNFT_FREI);
            TwwTestdatenbank.KategorieAnlegen(b, "Kurz", 1, 2.0, 1, 0.6, 1.0, kappung: 10.0, herkunftsart: TwwSchema.HERKUNFT_FREI);

            IReadOnlyList<Zapfkategorie> v = TwwNutzungsartCtrl.KategorienVorgabe();
            Assert.Equal(new[] { "Kurz", "Lang" }, v.Select(k => k.Name).ToArray());
            Assert.All(v, k => Assert.Equal(0, k.IdNutzungsart));
            Assert.All(v, k => Assert.Equal(Herkunftsart.Frei, k.Herkunft.Art));
            Assert.Equal(10.0, v[0].KappungLJeMin);
        }

        /// <summary>
        /// Eine freie Nutzungsart trägt die Kategorien an Ort und Stelle: gleiche Werte behalten ihre
        /// Provenienz, geänderte werden Eigenkonstruktion der Katalogversion der Zeile, der Status wird
        /// EIGEN, der Freigabevermerk entfällt; unverändert wird nichts geschrieben.
        /// </summary>
        [Fact]
        public void Eine_freie_Nutzungsart_speichert_an_Ort_und_Stelle_mit_Provenienz_je_Kategorie()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int n = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(n, "Erste", 1, 4.0, 1, 0.75, 1.0, status: TwwSchema.STATUS_IMPORT);
            TwwTestdatenbank.KategorieAnlegen(n, "Zweite", 2, 8.0, 5, 0.25, 2.0, status: TwwSchema.STATUS_IMPORT);
            DataRepository.ExecuteNonQuery("UPDATE Tab_TwwNutzungsart_STAMM SET Freigabe = 'geprüft' WHERE ID = ?", new DbParam("@id", n));

            List<Zapfkategorie> gelesen = TwwNutzungsartCtrl.KategorienLesen(n).Kategorien.ToList();
            TwwKategorienErgebnis gleich = TwwNutzungsartCtrl.KategorienSpeichern(n, gelesen);
            Assert.True(gleich.Ok);
            Assert.False(gleich.NeueZeile);
            Assert.Equal("geprüft", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Freigabe FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", n))));

            // Reihenfolge getauscht, „Zweite" mit neuem σ, dazu eine neue Kategorie.
            var entwurf = new List<Zapfkategorie>
            {
                gelesen[1] with { StreuungLJeMin = 3.0 },
                gelesen[0],
                K("Dritte", 1.0, 1, 0.5, 0.5)
            };
            TwwKategorienErgebnis e = TwwNutzungsartCtrl.KategorienSpeichern(n, entwurf);
            Assert.True(e.Ok);
            Assert.Equal(n, e.IdNutzungsart);
            Assert.False(e.NeueZeile);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Kategorie, Reihenfolge, Sigma, Herkunftsart, Quelle, Version, Status, ReadOnly FROM Tab_TwwZapfkategorie_STAMM " +
                "WHERE ID_Nutzungsart = ? ORDER BY Reihenfolge", new DbParam("@id", n));
            Assert.Equal(new[] { "Zweite", "Erste", "Dritte" }, dt.Rows.Cast<DataRow>().Select(r => (string)r["Kategorie"]).ToArray());
            Assert.Equal(3.0, Convert.ToDouble(dt.Rows[0]["Sigma"]));
            Assert.Equal(TwwSchema.HERKUNFT_EIGENKONSTRUKTION, dt.Rows[0]["Herkunftsart"]);
            Assert.Equal(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, dt.Rows[0]["Quelle"]);
            Assert.Equal("T1", dt.Rows[0]["Version"]);
            Assert.Equal(TwwSchema.HERKUNFT_FIKTIV, dt.Rows[1]["Herkunftsart"]);
            Assert.Equal(TwwTestdatenbank.QUELLE, dt.Rows[1]["Quelle"]);
            Assert.Equal(TwwSchema.HERKUNFT_EIGENKONSTRUKTION, dt.Rows[2]["Herkunftsart"]);
            Assert.All(dt.Rows.Cast<DataRow>(), r => Assert.Equal(TwwSchema.STATUS_EIGEN, r["Status"]));
            Assert.All(dt.Rows.Cast<DataRow>(), r => Assert.Equal(0L, Convert.ToInt64(r["ReadOnly"])));
            object freigabe = DataRepository.ExecuteScalar(
                "SELECT Freigabe FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", n));
            Assert.True(freigabe == null || freigabe == DBNull.Value);

            // Der Rechenweg nimmt den gespeicherten Satz an.
            Zapfkategoriensatz s = Zapfkategoriensatz.Aus(ZapfprofilCtrl.Zapfkategorien(new[] { n }), n, "Zone");
            Assert.Equal(3, s.Werte.Count);
        }

        /// <summary>
        /// Eine gesperrte Nutzungsart (ausgelieferte Kategorie, benutzt) ergibt per „Speichern unter"
        /// eine neue Nutzungsart mit den neuen Kategorien; ohne Katalogversion oder mit vergebener wird
        /// nichts geschrieben, die alte bleibt unberührt.
        /// </summary>
        [Fact]
        public void Eine_gesperrte_Nutzungsart_ergibt_eine_neue_Katalogversion()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int n = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz, beleg: "INT-7");
            TwwTestdatenbank.KategorieAnlegen(n, "Erste", 1, 4.0, 1, 1.0, 1.0, readOnly: true);
            int belegt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T2", satz);

            var entwurf = new[] { K("Erste", 4.0, 1, 1.0, 1.5) };
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig, TwwNutzungsartCtrl.KategorienSpeichern(n, entwurf).Ausgang);
            Assert.Equal(TwwKatalogAusgang.NameBelegt, TwwNutzungsartCtrl.KategorienSpeichern(n, entwurf, "T2").Ausgang);

            TwwKategorienErgebnis e = TwwNutzungsartCtrl.KategorienSpeichern(n, entwurf, "T3");
            Assert.True(e.Ok);
            Assert.True(e.NeueZeile);
            Assert.NotEqual(n, e.IdNutzungsart);
            Assert.NotEqual(belegt, e.IdNutzungsart);

            DataTable kopf = DataRepository.GetDataTable(
                "SELECT Bezeichner, Katalogversion, ID_Vorlage, Status, ReadOnly, Beleg, ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?",
                new DbParam("@id", e.IdNutzungsart));
            Assert.Equal("Nutzung A", kopf.Rows[0]["Bezeichner"]);
            Assert.Equal("T3", kopf.Rows[0]["Katalogversion"]);
            Assert.Equal((long)n, Convert.ToInt64(kopf.Rows[0]["ID_Vorlage"]));
            Assert.Equal(TwwSchema.STATUS_EIGEN, kopf.Rows[0]["Status"]);
            Assert.Equal("INT-7", kopf.Rows[0]["Beleg"]);
            Assert.Equal((long)satz, Convert.ToInt64(kopf.Rows[0]["ID_Tagesgangsatz"]));

            Zapfkategorie neu = Assert.Single(TwwNutzungsartCtrl.KategorienLesen(e.IdNutzungsart).Kategorien);
            Assert.Equal(1.5, neu.StreuungLJeMin);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, neu.Herkunft.Art);
            Assert.Equal("T3", neu.Herkunft.Version);
            Assert.True(TwwNutzungsartCtrl.KategorienLesen(e.IdNutzungsart).Frei);

            // Die alte bleibt unberührt.
            Zapfkategorie alt = Assert.Single(TwwNutzungsartCtrl.KategorienLesen(n).Kategorien);
            Assert.Equal(1.0, alt.StreuungLJeMin);
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, TwwNutzungsartCtrl.KategorienLesen(n).Sperre);
        }

        [Fact]
        public void Die_Regeln_lehnen_benannt_ab_und_schreiben_nichts()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int n = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(n, "Erste", 1, 4.0, 1, 1.0, 1.0);

            var faelle = new (IReadOnlyList<Zapfkategorie> Entwurf, string Kennung)[]
            {
                (new Zapfkategorie[0], "KATEGORIE_KEINE"),
                (new[] { K(" ", 1.0, 1, 1.0, 0.0) }, "KATEGORIE_NAME_FEHLT"),
                (new[] { K("A", 1.0, 1, 1.0, 0.0), K("A ", 2.0, 1, 1.0, 0.0) }, "KATEGORIE_NAME_DOPPELT"),
                (new[] { K("A", -1.0, 1, 1.0, 0.0) }, "KATEGORIE_VOLUMENSTROM"),
                (new[] { K("A", 1.0, 1, 1.0, double.NaN) }, "KATEGORIE_STREUUNG"),
                (new[] { K("A", 1.0, 0, 1.0, 0.0) }, "KATEGORIE_DAUER"),
                (new[] { K("A", 1.0, 1, -0.5, 0.0) }, "KATEGORIE_ANTEIL"),
                (new[] { K("A", 1.0, 1, 1.0, 0.0, kappung: 0.0) }, "KATEGORIE_KAPPUNG"),
                (new[] { K("A", 1.0, 1, 0.0, 0.0) }, "KATEGORIE_ANTEIL_NULL"),
                (new[] { K("A", 0.0, 1, 1.0, 0.0) }, "KATEGORIE_MITTEL")
            };
            foreach (var (entwurf, kennung) in faelle)
            {
                TwwKategorienErgebnis e = TwwNutzungsartCtrl.KategorienSpeichern(n, entwurf, "T9");
                Assert.Equal(TwwKatalogAusgang.RasterUngueltig, e.Ausgang);
                Assert.Equal(kennung, e.Grund.Kennung);
            }
            Assert.Equal("Erste", Assert.Single(TwwNutzungsartCtrl.KategorienLesen(n).Kategorien).Name);

            // Eine Kategorie ohne Anteil darf ein gestutztes Mittel 0 haben — sie zieht nie.
            Assert.Null(Zapfkategoriensatz.Pruefen(new[] { K("A", 1.0, 1, 1.0, 0.0), K("B", 0.0, 1, 0.0, 0.0) }));
        }

        /// <summary>Der unveränderte Vorgabesatz behält an einer Nutzungsart ohne Kategorien seine freie Provenienz.</summary>
        [Fact]
        public void Der_unveraenderte_Vorgabesatz_behaelt_seine_Provenienz()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int quelle = TwwTestdatenbank.NutzungsartAnlegen("Nutzung Quelle", "T1", satz);
            int leer = TwwTestdatenbank.NutzungsartAnlegen("Nutzung leer", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(quelle, "Kurz", 1, 2.0, 1, 0.6, 1.0, herkunftsart: TwwSchema.HERKUNFT_FREI);
            TwwTestdatenbank.KategorieAnlegen(quelle, "Lang", 2, 8.0, 5, 0.4, 2.0, herkunftsart: TwwSchema.HERKUNFT_FREI);

            IReadOnlyList<Zapfkategorie> vorgabe = TwwNutzungsartCtrl.KategorienVorgabe();
            var entwurf = new List<Zapfkategorie> { vorgabe[0], vorgabe[1] with { Anteil = 0.5 } };
            Assert.True(TwwNutzungsartCtrl.KategorienSpeichern(leer, entwurf).Ok);

            IReadOnlyList<Zapfkategorie> k = TwwNutzungsartCtrl.KategorienLesen(leer).Kategorien;
            Assert.Equal(Herkunftsart.Frei, k[0].Herkunft.Art);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, k[1].Herkunft.Art);
        }
    }
}
