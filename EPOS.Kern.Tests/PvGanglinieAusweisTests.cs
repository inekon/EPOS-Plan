using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kennzeichnung der PV-Ganglinie</b> in Ergebnisreiter und Bericht (<see cref="PvGanglinieAusweis"/>):
    /// der Quelltext in beiden Sprachen, die Rückfallnennung ohne gepflegte Nennleistung und die Kenndaten der
    /// Photovoltaik im Bericht — Quelle vorn, die Merkmale des Modulmodells als „entfällt (Ganglinie)".
    /// </summary>
    public sealed class PvGanglinieAusweisTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static PvGanglinieWeiche.Stand Stand(int raster, double? nenn)
        {
            int n = PvGanglinieWeiche.Erwartet(raster);
            var werte = new double?[n];
            for (int i = 0; i < n; i++) werte[i] = i % 24 == 12 ? 7.5 : 0.0;
            return PvGanglinieWeiche.Pruefen(4711, "PV Dach Süd", raster, nenn, werte);
        }

        [Fact]
        public void Der_Ausweis_nennt_Name_Raster_und_Nennleistung()
        {
            PvGanglinieAusweis a = PvGanglinieAusweis.Aus(Stand(PvGanglinieWeiche.RASTER_VIERTEL, 9.8));
            Assert.NotNull(a);
            Assert.Equal("Ganglinie ‚PV Dach Süd‘ (Viertelstundenwerte), Nennleistung 9,80 kWp",
                         a.Text(CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal("Generation profile ‘PV Dach Süd’ (quarter-hourly values), rated power 9.80 kWp",
                         a.Text(CultureInfo.GetCultureInfo("en-US")));
            Assert.Equal("entfällt (Ganglinie)", PvGanglinieAusweis.Entfaellt(CultureInfo.GetCultureInfo("de-DE")));
        }

        [Fact]
        public void Ohne_gepflegte_Nennleistung_nennt_er_die_Spitze_der_Reihe()
        {
            PvGanglinieAusweis a = PvGanglinieAusweis.Aus(Stand(PvGanglinieWeiche.RASTER_STUNDE, null));
            Assert.False(a.NennleistungGepflegt);
            Assert.Equal(7.5, a.NennleistungKwp);
            Assert.Equal("Ganglinie ‚PV Dach Süd‘ (Stundenwerte), Nennleistung 7,50 kWp (Spitze der Reihe)",
                         a.Text(CultureInfo.GetCultureInfo("de-DE")));
        }

        [Fact]
        public void Ohne_rechnende_Ganglinie_gibt_es_keinen_Ausweis()
        {
            Assert.Null(PvGanglinieAusweis.Aus(PvGanglinieWeiche.Keine()));
            Assert.Null(PvGanglinieAusweis.Aus(null));
        }

        /// <summary>Ein Stand mit einer PV-Komponentenzeile (Tab_PV) und wahlweise einer Ganglinie.</summary>
        private static VariantenDaten Variante(bool stamm, string name, PvGanglinieAusweis ganglinie)
        {
            var pv = new DataTable();
            foreach (string s in new[] { "Bezeichner", "Firma", "Technologie" }) pv.Columns.Add(s, typeof(string));
            foreach (string s in new[] { "Leistung", "Wirkungsgrad" }) pv.Columns.Add(s, typeof(double));
            pv.Rows.Add("Modul 400", "Hersteller", "mono", 400.0, 21.0);
            var d = new ProjektDetails { IdProjekt = stamm ? 1 : 2, PvGanglinie = ganglinie };
            d.KomponentenAnzahl["Photovoltaik"] = 1;
            d.Komponenten["Photovoltaik"] = pv.Rows[0];
            d.KomponentenAlle["Photovoltaik"] = pv;
            return new VariantenDaten { IstStamm = stamm, Projektname = name, Details = d };
        }

        [Fact]
        public void Die_Kenndaten_im_Bericht_nennen_die_Quelle_und_lassen_das_Modulmodell_entfallen()
        {
            PvGanglinieAusweis a = PvGanglinieAusweis.Aus(Stand(PvGanglinieWeiche.RASTER_VIERTEL, 9.8));
            var daten = new BerichtsDaten { Stammprojektname = "Probe" };
            daten.Varianten.Add(Variante(true, "Probe", null));
            daten.Varianten.Add(Variante(false, "Mit Ganglinie", a));

            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            Berichtstabelle t = Berichtstabellen.Kenndaten(daten, "Photovoltaik", false, de);
            Assert.False(t.IstLeer);

            List<string> quelle = t.Zeilen[0].Zellen.Select(z => z.Text).ToList();
            Assert.Equal("Quelle", quelle[0]);
            Assert.Equal("Modulmodell", quelle[1]);
            Assert.Equal(a.Text(de), quelle[2]);

            foreach (Tabellenzeile z in t.Zeilen.Skip(1))
            {
                Assert.NotEqual("entfällt (Ganglinie)", z.Zellen[1].Text);
                Assert.Equal("entfällt (Ganglinie)", z.Zellen[2].Text);
            }

            // Ohne Ganglinie bleibt die Tafel, wie sie war: keine Quellzeile.
            var ohne = new BerichtsDaten { Stammprojektname = "Probe" };
            ohne.Varianten.Add(Variante(true, "Probe", null));
            Berichtstabelle t2 = Berichtstabellen.Kenndaten(ohne, "Photovoltaik", false, de);
            Assert.Equal(t.Zeilen.Count - 1, t2.Zeilen.Count);
            Assert.DoesNotContain(t2.Zeilen, z => z.Zellen[0].Text == "Quelle");
        }
            /// <summary>
        /// <b>Die Excel-Fassung</b> trägt dieselbe Tafel auf der Übersicht: Quellzeile („Modulmodell" bzw. der Ausweis der
        /// Ganglinie) und „entfällt (Ganglinie)" an den Modulmerkmalen des Ganglinienstands; ohne Ganglinie kein Block.
        /// </summary>
        [Fact]
        public void Die_Excel_Uebersicht_nennt_die_Quelle_und_laesst_das_Modulmodell_entfallen()
        {
            PvGanglinieAusweis a = PvGanglinieAusweis.Aus(Stand(PvGanglinieWeiche.RASTER_STUNDE, null));
            var daten = new BerichtsDaten { Stammprojektname = "Probe" };
            daten.Varianten.Add(Variante(true, "Probe", null));
            daten.Varianten.Add(Variante(false, "Mit Ganglinie", a));
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            Berichtstabelle tafel = Berichtstabellen.Kenndaten(daten, "Photovoltaik", false, de);

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.Worksheets.Add("Probe");
            int ende = ExcelBerichtGenerator.PvKenndatenBlock(ws, 5, daten);
            Assert.Equal(5 + 1 + 1 + tafel.Zeilen.Count + 1, ende);
            Assert.Equal("Kenndaten Photovoltaik", ws.Cell(5, 1).GetString());
            Assert.Equal("Merkmal", ws.Cell(6, 1).GetString());
            Assert.Equal("Quelle", ws.Cell(7, 1).GetString());
            Assert.Equal("Modulmodell", ws.Cell(7, 2).GetString());
            Assert.Equal(a.Text(de), ws.Cell(7, 3).GetString());
            Assert.Contains("Spitze", ws.Cell(7, 3).GetString());
            for (int r = 8; r < 7 + tafel.Zeilen.Count; r++)
            {
                Assert.NotEqual("entfällt (Ganglinie)", ws.Cell(r, 2).GetString());
                Assert.Equal("entfällt (Ganglinie)", ws.Cell(r, 3).GetString());
            }

            var ohne = new BerichtsDaten { Stammprojektname = "Probe" };
            ohne.Varianten.Add(Variante(true, "Probe", null));
            IXLWorksheet ws2 = wb.Worksheets.Add("Ohne");
            Assert.Equal(5, ExcelBerichtGenerator.PvKenndatenBlock(ws2, 5, ohne));
            Assert.True(ws2.Cell(5, 1).IsEmpty());
        }
        }
}
