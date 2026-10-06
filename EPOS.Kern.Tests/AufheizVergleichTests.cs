using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Aufheizoptimierung im Variantenvergleich</b> (Entwurf KP3, Welle O3b; E58 F3 (c), E59, B21; Festlegungen 29, 39):
    /// die Abweichungsmerkmale der Aufheizung unter „Gebäude“ — Schalter, Bemessung, Abzug, Reserve, Art (mit „manuell“),
    /// Aufschlag in Stunden und Prozent, manuelle Aufheizzeit des ersten Gebäudes —, die Kennzahlgruppe „Gebäude“ mit Δ und
    /// die Gebäudetafel je Stand (<c>stand.tabelle.gebaeude</c>, ohne Δ) samt ihren Vorlagenfeldern der Fassung 16.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizVergleichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        // Abweichungsmerkmale (B21, Festlegungen 29 und 39)
        // =================================================================

        /// <summary>Ein Projekt mit Aufheizeinstellung und einem Gebäude; <c>null</c> heißt NULL in der Datenbank.</summary>
        private static ProjektDetails Details(bool schalter, string bemessung = null, double? abzug = null, double? reserve = null,
                                              string art = null, double? aufschlagH = null, double? aufschlagProzent = null,
                                              params long?[] manuell)
        {
            var ein = new DataTable();
            ein.Columns.Add("ID", typeof(long));
            ein.Columns.Add("ID_Projekt", typeof(long));
            ein.Columns.Add("Aufheizoptimierung", typeof(long));
            ein.Columns.Add("Aufheiz_Bemessung", typeof(string));
            ein.Columns.Add("Aufheiz_Abzug_K", typeof(double));
            ein.Columns.Add("Aufheiz_Reserve", typeof(double));
            ein.Columns.Add("Aufheiz_Art", typeof(string));
            ein.Columns.Add("Aufheiz_Aufschlag_H", typeof(long));
            ein.Columns.Add("Aufheiz_Aufschlag_Prozent", typeof(double));
            ein.Rows.Add(1L, 1L, schalter ? 1L : 0L, (object)bemessung ?? DBNull.Value, (object)abzug ?? DBNull.Value,
                         (object)reserve ?? DBNull.Value, (object)art ?? DBNull.Value,
                         aufschlagH.HasValue ? (long)aufschlagH.Value : (object)DBNull.Value, (object)aufschlagProzent ?? DBNull.Value);

            var geb = new DataTable();
            geb.Columns.Add("ID", typeof(long));
            geb.Columns.Add("Gebaeudename", typeof(string));
            geb.Columns.Add("Aufheizzeit_Manuell_H", typeof(long));
            long?[] zeiten = manuell != null && manuell.Length > 0 ? manuell : new long?[] { null };
            for (int i = 0; i < zeiten.Length; i++)
                geb.Rows.Add(10L + i, "Gebäude " + (i + 1), (object)zeiten[i] ?? DBNull.Value);

            return new ProjektDetails { IdProjekt = 1, Einstellungen = ein, Gebaeude = geb };
        }

        private static Abweichung Finde(List<Abweichung> liste, string merkmal)
            => liste.SingleOrDefault(a => a.Gewerk == "Gebäude" && a.Merkmal == merkmal);

        /// <summary>
        /// <b>B21, rote Probe:</b> Der Vergleich mit und ohne Rampe (P8) meldete „keine Abweichungen“ — der Ermittler kannte
        /// die Aufheizspalten nicht. Jetzt steht der Schalter als Ja/Nein-Merkmal da.
        /// </summary>
        [Fact]
        public void Mit_und_ohne_Rampe_ist_eine_Abweichung()
        {
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Details(false), Details(true));
            Abweichung schalter = Finde(liste, R.ABW_MERKMAL_AUFH_SCHALTER);
            Assert.NotNull(schalter);
            Assert.Equal("Nein", schalter.WertStamm);
            Assert.Equal("Ja", schalter.WertVariante);
            Assert.Single(liste);
        }
    }
}
