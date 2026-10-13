using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Vorgaben der Baualtersklasse übernehmen…“ im Einzonenweg</b> (Welle MA-7, Entwurf Geführter Modellaufbau 2.1 Nr. 1):
    /// die Tafel der Bauart je Klasse gegen den Gebäudekatalog der Testdatenbank, die Vorgabe aus derselben Tafel wie Import
    /// und Stufe C (<see cref="GebaeudeVorgaben"/>), Vorschau alt → neu, Anwenden, Rücknahme und Sperre mit Grund.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BaualterEinzonenvorgabenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BaualterEinzonenvorgabenTests(TestDatenbank db) { _db = db; }

        private static readonly Einzonenwerte LEER = new(null, null, null, null, null, null, null, null, null, null);

        [Fact]
        public void Die_Bauart_je_Klasse_ist_die_haeufigste_des_Gebaeudekatalogs()
        {
            if (!_db.Vorhanden) return;
            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude_STAMM WHERE 1 = 1" + Konditionierungsbestand.Ausser("Tab_Gebaeude_STAMM"));
            Assert.NotNull(t);
            var abweichungen = new List<string>();
            for (int i = 0; i < Einzonenvorgaben.BAUART_JE_KLASSE.Count; i++)
            {
                string klasse = ((char)('A' + i)).ToString();
                var zahl = new int[3];
                foreach (DataRow r in t.Rows)
                {
                    if (!string.Equals(Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture), klasse, StringComparison.Ordinal)) continue;
                    double bauweise = Zahl(r["Bauweise"]);
                    double flaeche = Zahl(r["Wohnflaeche_gesamt"]);
                    if (flaeche <= 0) flaeche = Zahl(r["Nutzflaeche"]);
                    if (bauweise <= 0 || flaeche <= 0) continue;
                    zahl[Gebaeudebauweise.BauartAusBauweise(bauweise, flaeche)]++;
                }
                Assert.True(zahl.Sum() > 0, "Klasse " + klasse + " hat keinen Katalogsatz mit Bauweise.");
                // Die häufigste; bei Gleichstand die schwerere.
                int haeufigste = Enumerable.Range(0, 3).OrderByDescending(b => zahl[b]).ThenByDescending(b => b).First();
                if (haeufigste != Einzonenvorgaben.BAUART_JE_KLASSE[i])
                    abweichungen.Add(klasse + ": Tafel " + Einzonenvorgaben.BAUART_JE_KLASSE[i] + ", Katalog " + haeufigste);
            }
            Assert.True(abweichungen.Count == 0, "BAUART_JE_KLASSE passt nicht zum Katalog: " + string.Join("; ", abweichungen));
        }

        private static double Zahl(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        [Fact]
        public void Die_Vorgabe_kommt_aus_der_Tafel_des_Imports()
        {
            for (int i = 0; i < 13; i++)
            {
                char k = (char)('A' + i);
                Einzonenvorgabe v = Einzonenvorgaben.Fuer(i);
                Assert.NotNull(v);
                Assert.Equal(k.ToString(), v.Kennung);
                Assert.False(v.AusStandard);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.U_AUSSENWAND), v.Werte.UAussenwand);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.U_FENSTER), v.Werte.UFenster);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.U_DACH), v.Werte.UDach);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.U_GRUND), v.Werte.UGrund);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.U_SONSTIGE), v.Werte.USonstige);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.G_WERT), v.Werte.Fensterdurchlassgrad);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.PSI_FENSTER_WAND), v.Werte.PsiFensterWand);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.PSI_WAND_DACH), v.Werte.PsiWandDach);
                Assert.Equal(GebaeudeVorgaben.Wert(k, GebaeudeZielfelder.PSI_AUSSENWAND_KELLER), v.Werte.PsiAussenwandKeller);
                Assert.Equal(Einzonenvorgaben.BAUART_JE_KLASSE[i], v.Werte.Bauart);
            }
            Assert.Null(Einzonenvorgaben.Fuer(-1));
            Assert.Null(Einzonenvorgaben.Fuer(13));
        }

        [Fact]
        public void Ein_Energiestandard_mit_Katalogsaetzen_geht_der_Klasse_vor()
        {
            Baualtersvorgabe s = GebaeudeVorgaben.Standards.FirstOrDefault(x => x.Katalogsaetze > 0);
            if (s == null) return;
            Einzonenvorgabe v = Einzonenvorgaben.Fuer(0, s.Energiestandard);
            Assert.True(v.AusStandard);
            Assert.Equal(s.Energiestandard, v.Kennung);
            Assert.Equal(s.UAussenwand, v.Werte.UAussenwand);
            // Die Bauart bleibt die der Klasse.
            Assert.Equal(Einzonenvorgaben.BAUART_JE_KLASSE[0], v.Werte.Bauart);
            Assert.False(Einzonenvorgaben.Fuer(0, "  ").AusStandard);
        }

        [Fact]
        public void Die_Vorschau_nennt_alt_und_neu_je_Feld()
        {
            Einzonenvorgabe v = Einzonenvorgaben.Fuer(4);
            Einzonenwerte ist = LEER with { UAussenwand = v.Werte.UAussenwand, UFenster = 9.9, Bauart = Gebaeudebauweise.LEICHT };
            IReadOnlyList<Einzonenposten> p = Einzonenvorgaben.Vorschau(ist, v);
            Assert.Equal(Einzonenvorgaben.Felder.Count(f => v.Werte.Wert(f) != null), p.Count);
            Einzonenposten wand = p.Single(x => x.Feld == Einzonenfeld.UAussenwand);
            Assert.False(wand.Aendert);
            Einzonenposten fenster = p.Single(x => x.Feld == Einzonenfeld.UFenster);
            Assert.Equal(9.9, fenster.Alt);
            Assert.Equal(v.Werte.UFenster, fenster.Neu);
            Assert.True(fenster.Aendert);
            Einzonenposten bauart = p.Single(x => x.Feld == Einzonenfeld.Bauart);
            Assert.Equal(Gebaeudebauweise.LEICHT, bauart.Alt);
            Assert.Equal(Gebaeudebauweise.SCHWER, bauart.Neu);
        }

        [Fact]
        public void Anwenden_und_Ruecknahme_lassen_eine_Handaenderung_stehen()
        {
            Einzonenvorgabe v = Einzonenvorgaben.Fuer(2);
            Einzonenwerte vorher = LEER with { UAussenwand = 1.11, UDach = 2.22, Fensterdurchlassgrad = 0.5, Bauart = Gebaeudebauweise.LEICHT };
            Einzonenwerte nachher = Einzonenvorgaben.Anwenden(vorher, v);
            Assert.Equal(v.Werte.UAussenwand, nachher.UAussenwand);
            Assert.Equal(v.Werte.UDach, nachher.UDach);
            Assert.Equal(v.Werte.Fensterdurchlassgrad, nachher.Fensterdurchlassgrad);
            Assert.Equal(v.Werte.Bauart, nachher.Bauart);
            // Der Anwender ändert danach das Dach von Hand.
            Einzonenwerte jetzt = nachher with { UDach = 0.33 };
            Einzonenwerte zurueck = Einzonenvorgaben.Zuruecknehmen(jetzt, vorher, nachher);
            Assert.Equal(1.11, zurueck.UAussenwand);
            Assert.Equal(0.33, zurueck.UDach);
            Assert.Equal(0.5, zurueck.Fensterdurchlassgrad);
            Assert.Equal(Gebaeudebauweise.LEICHT, zurueck.Bauart);
            Assert.Null(zurueck.UFenster);
        }

        [Fact]
        public void Die_Sperre_nennt_Zonen_vor_fehlender_Klasse()
        {
            Einzonenvorgabe v = Einzonenvorgaben.Fuer(0);
            Assert.Equal(Einzonensperre.Zonen, Einzonenvorgaben.Sperre(2, false, null));
            Assert.Equal(Einzonensperre.OhneKlasse, Einzonenvorgaben.Sperre(0, false, v));
            Assert.Equal(Einzonensperre.OhneKlasse, Einzonenvorgaben.Sperre(0, true, null));
            Assert.Equal(Einzonensperre.OhneWerte, Einzonenvorgaben.Sperre(0, true, v with { Werte = LEER }));
            Assert.Equal(Einzonensperre.Keine, Einzonenvorgaben.Sperre(0, true, v));
        }
    }
}
