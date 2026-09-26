using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zonen im Bericht und im Variantenvergleich</b> (Gebäudesimulation G6a, Welle 4): die
    /// Tabelle „Zonen" je Gebäude mit Zonen im Gebäudeblock der Projektbeschreibung — Werte nur aus
    /// <see cref="Zonenkennwerte"/>, Summenzeile, Stern am abgeleiteten Volumen —, der Abschnitt
    /// entfällt ohne Zonen; die vier Zonenmerkmale des <see cref="AbweichungsErmittler"/> (Zahl der
    /// Zonen, Σ Nutzfläche, Σ H_T, Rechenweg der Hülle), sonst meldete ein Zonenunterschied „Keine
    /// Abweichungen". Ohne Datenbank; die Kultur ist auf de-DE gepinnt. Dazu das Laden über
    /// <see cref="ProjektDetails.Lade"/> gegen die Testdatenbank.
    /// </summary>
    public sealed class ZonenBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Zwei Gebäude, wie <c>ProjektDetails</c> sie aus <c>Tab_Gebaeude</c> liest.</summary>
        internal static DataTable Gebaeude()
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(long));
            dt.Columns.Add("Gebaeudename", typeof(string));
            dt.Columns.Add("Nutzflaeche", typeof(double));
            dt.Columns.Add("Raumhoehe", typeof(double));
            dt.Columns.Add("Luftwechselrate", typeof(double));
            dt.Columns.Add("Gebaeude_Modell", typeof(string));
            dt.Rows.Add(1L, "Haus A", 150.0, 2.5, 0.5, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ);
            dt.Rows.Add(2L, "Haus B", 80.0, 2.6, 0.6, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ);
            return dt;
        }

        /// <summary>Zwei Zonen: Erdgeschoss mit Volumen, Obergeschoss ohne (abgeleitet), eine Wand mit Aufbau.</summary>
        internal static List<ZoneModel> ZweiZonen() => new()
        {
            new ZoneModel
            {
                ID = 11, Bezeichner = "Erdgeschoss", Nutzflaeche = 90, Volumen = 250,
                Bauteile =
                {
                    new BauteilModel { ID = 1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 100, ID_Aufbau = 7, Azimut = 180 },
                    new BauteilModel { ID = 2, Bezeichner = "Boden", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = 90, U_Wert = 0.4,
                                       Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH }
                }
            },
            new ZoneModel
            {
                ID = 12, Bezeichner = "Obergeschoss", Nutzflaeche = 60,
                Bauteile = { new BauteilModel { ID = 3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 90, U_Wert = 0.2, Psi_L = 2.0 } }
            }
        };

        private static ProjektDetails Details(bool mitZonen)
        {
            var d = new ProjektDetails { IdProjekt = 1, Gebaeude = Gebaeude() };
            if (mitZonen)
            {
                d.Zonen[1] = ZweiZonen();
                d.AufbauU[7] = 0.3;
            }
            d.Zonenmerkmale = ProjektDetails.BildeZonenmerkmale(d);
            return d;
        }

        private static BerichtsDaten Daten(ProjektDetails details)
        {
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten { IstStamm = true, Projektname = "Probe", Details = details });
            return d;
        }

        private static (string Text, int Tabellen) Schreibe(BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return (string.Join("\n", main.Document.Body.Descendants<Text>().Select(t => t.Text)),
                    main.Document.Body.Descendants<Table>().Count());
        }

        [Fact]
        public void Ohne_Zonen_entfaellt_der_Abschnitt()
        {
            (string text, int tabellen) = Schreibe(Daten(Details(mitZonen: false)));
            Assert.Contains("Haus A", text);
            Assert.DoesNotContain("H_ve [W/K]", text);
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_ZONEN, text.Split('\n'));
            Assert.Equal(3, tabellen);                            // Projekt und zwei Gebäude, keine Zonentabelle
        }

        /// <summary>
        /// Zwei Gebäude, nur Haus A mit Zonen: EINE Tabelle, unter Haus A und vor Haus B, mit den
        /// Werten der einen Formel und der Summenzeile; das abgeleitete Volumen trägt den Stern.
        /// </summary>
        [Fact]
        public void Die_Tabelle_steht_nur_beim_Gebaeude_mit_Zonen_mit_Werten_aus_Zonenkennwerten()
        {
            ProjektDetails d = Details(mitZonen: true);
            (string text, int tabellen) = Schreibe(Daten(d));
            Assert.Equal(Schreibe(Daten(Details(mitZonen: false))).Tabellen + 1, tabellen);
            Assert.Single(text.Split('\n'), z => z == ProjektbeschreibungBaustein.UEBERSCHRIFT_ZONEN);

            int a = text.IndexOf("Haus A", StringComparison.Ordinal);
            int zonen = text.IndexOf("H_ve [W/K]", StringComparison.Ordinal);
            int b = text.IndexOf("Haus B", StringComparison.Ordinal);
            Assert.True(a < zonen && zonen < b, "Die Zonentabelle steht unter Haus A, vor Haus B.");

            DataRow g = d.Gebaeude.Rows[0];
            Zonenkennwerte eg = d.Kennwerte(d.Zonen[1][0], g), og = d.Kennwerte(d.Zonen[1][1], g);
            // H_T: 0,3·100 + 0,4·90 = 66; 0,2·90 + 2 = 20. H_ve: 0,5·A·2,5·0,34.
            Assert.Equal(66.0, eg.HT, 9);
            Assert.Equal(20.0, og.HT, 9);
            Assert.Equal(250.0, eg.Volumen);
            Assert.False(eg.VolumenAbgeleitet);
            Assert.Equal(150.0, og.Volumen!.Value, 9);
            Assert.True(og.VolumenAbgeleitet);

            string[] zeilen = text.Split('\n');
            Assert.Contains("Erdgeschoss", zeilen);
            Assert.Contains("66,0", zeilen);
            Assert.Contains("150 *", zeilen);
            Assert.Contains("Summe", zeilen);
            Assert.Contains("86,0", zeilen);                     // Σ H_T
            Assert.Contains("150,0", zeilen);                    // Σ Nutzfläche
            Assert.Contains("400", zeilen);                      // Σ Volumen
            Assert.Contains(ProjektbeschreibungBaustein.HINWEIS_ZONENVOLUMEN, zeilen);
            Assert.Contains((eg.HVe + og.HVe).ToString("N1", System.Globalization.CultureInfo.GetCultureInfo("de-DE")), zeilen);
        }

        /// <summary>
        /// <b>Stufe G6b: die Zonenzeilen des Laufs in derselben Tabelle</b> (<c>Tab_ErgebnisZone</c>,
        /// E30) — „beheizt", Heizwärme und Spitze je Zone, über die Zone gefunden; die unbeheizte Zone
        /// zeigt „—", die Summenzeile summiert die Heizwärme und nicht die Spitzen. Ohne Zonenzeilen
        /// bleibt die Tabelle bei sechs Spalten.
        /// </summary>
        [Fact]
        public void Mit_Zonenzeilen_des_Laufs_traegt_die_Tabelle_beheizt_Heizwaerme_und_Spitze()
        {
            BerichtsDaten daten = Daten(Details(mitZonen: true));
            var geb = new ErgebnisGebaeudeModel { ID_Gebaeude = 1, Merkplatz = 0, Gebaeudename = "Haus A",
                                                  Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007 };
            geb.Zonen.Add(new ErgebnisZoneModel { ID_Zone = 11, Rang = 1, Bezeichner = "Erdgeschoss", IstBeheizt = true,
                                                  HeizwaermeMwh = 12.5, SpitzeKw = 7.2 });
            geb.Zonen.Add(new ErgebnisZoneModel { ID_Zone = 12, Rang = 2, Bezeichner = "Obergeschoss", IstBeheizt = false });
            daten.Varianten[0].Ergebnis = new ErgebnisModel();
            daten.Varianten[0].Ergebnis.Gebaeude.Add(geb);

            (string text, _) = Schreibe(daten);
            string[] zeilen = text.Split('\n');
            Assert.Contains("beheizt", zeilen);
            Assert.Contains("Heizwärme [MWh/a]", zeilen);
            Assert.Contains("Spitze [kW]", zeilen);
            Assert.Contains("ja", zeilen);
            Assert.Contains("nein", zeilen);
            Assert.Contains("7,2", zeilen);
            Assert.Equal(2, zeilen.Count(z => z == "12,5"));      // die Zone und die Summe
            Assert.Contains("H_ve [W/K]", zeilen);

            // Ohne Zonenzeilen bleibt es bei der Tabelle der Stufe G6a.
            (string ohne, _) = Schreibe(Daten(Details(mitZonen: true)));
            Assert.DoesNotContain("beheizt", ohne.Split('\n'));
        }

        [Fact]
        public void Die_Zonenmerkmale_melden_einen_Zonenunterschied()
        {
            ProjektDetails ohne = Details(mitZonen: false), mit = Details(mitZonen: true);

            Assert.Empty(AbweichungsErmittler.Vergleiche(ohne, Details(mitZonen: false)));
            Assert.Empty(AbweichungsErmittler.Vergleiche(mit, Details(mitZonen: true)));

            List<Abweichung> a = AbweichungsErmittler.Vergleiche(ohne, mit);
            Abweichung zahl = Assert.Single(a, x => x.Merkmal == R.ABW_MERKMAL_ZONENZAHL);
            Assert.Equal(("0", "2"), (zahl.WertStamm, zahl.WertVariante));
            Abweichung weg = Assert.Single(a, x => x.Merkmal == R.ABW_MERKMAL_HUELLRECHENWEG);
            Assert.Equal(R.ABW_WERT_KLASSENWEG, weg.WertStamm);
            Assert.Equal(string.Format(R.ABW_WERT_BAUTEILWEG_TEIL, 1, 2), weg.WertVariante);
            Abweichung flaeche = Assert.Single(a, x => x.Merkmal == R.ABW_MERKMAL_ZONENFLAECHE);
            Assert.Equal(("—", "150 m²"), (flaeche.WertStamm, flaeche.WertVariante));
            Abweichung ht = Assert.Single(a, x => x.Merkmal == R.ABW_MERKMAL_ZONEN_HT);
            Assert.Equal("86,0 W/K", ht.WertVariante);
            Assert.All(a, x => Assert.Equal("Gebäude", x.Gewerk));

            // Zwei Gebäude mit Zonen: der Rechenweg heißt Bauteilweg.
            ProjektDetails beide = Details(mitZonen: true);
            beide.Zonen[2] = new List<ZoneModel> { ZweiZonen()[1] };
            beide.Zonenmerkmale = ProjektDetails.BildeZonenmerkmale(beide);
            Assert.Equal(ProjektDetails.HUELLE_BAUTEILWEG, beide.Zonenmerkmale.Rows[0]["Huellrechenweg"]);
            Assert.Equal(R.ABW_WERT_BAUTEILWEG, ProjektDetails.Huellrechenwegtext(ProjektDetails.HUELLE_BAUTEILWEG));
        }
    }

    /// <summary>Das Laden der Zonen über <see cref="ProjektDetails.Lade"/> gegen die Testdatenbank (G6a, Welle 4).</summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenBerichtDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        [Fact]
        public void Lade_liest_die_Zonen_einmal_je_Projekt_und_bildet_die_Merkmale()
        {
            if (!_db.Vorhanden) return;
            ProjektDetails vorher = ProjektDetails.Lade(ZonenRundlauf.PROJEKT);
            Assert.Empty(vorher.Zonen);
            Assert.Equal(0L, vorher.Zonenmerkmale.Rows[0]["Zonenzahl"]);
            Assert.Equal(ProjektDetails.HUELLE_KLASSENWEG, vorher.Zonenmerkmale.Rows[0]["Huellrechenweg"]);

            ZonenRundlauf.Anlegen();
            ProjektDetails d = ProjektDetails.Lade(ZonenRundlauf.PROJEKT);
            Assert.Equal(new[] { ZonenRundlauf.GEBAEUDE_A, ZonenRundlauf.GEBAEUDE_B }, d.Zonen.Keys.OrderBy(k => k));
            Assert.Equal(6L, d.Zonenmerkmale.Rows[0]["Zonenzahl"]);
            Assert.Equal("TEIL:2/3", d.Zonenmerkmale.Rows[0]["Huellrechenweg"]);
            Assert.NotEmpty(d.AufbauU);
            Assert.Contains(d.AufbauU.Values, u => u.HasValue && u.Value > 0.0);

            // Die Wand mit Projektaufbau rechnet mit dessen U.
            DataRow a = d.Gebaeude.Rows.Cast<DataRow>().Single(r => Convert.ToInt32(r["ID"]) == ZonenRundlauf.GEBAEUDE_A);
            ZoneModel anbau = d.ZonenVon(ZonenRundlauf.GEBAEUDE_A).Single(z => z.Bezeichner == "Anbau A");
            Zonenkennwerte k = d.Kennwerte(anbau, a);
            double uWand = d.AufbauU[anbau.Bauteile[0].ID_Aufbau!.Value]!.Value;
            Assert.Equal(uWand * 18.5 + 1.1 * 4.2 + 1.2, k.HT, 9);
        }
    }
}
