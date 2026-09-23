using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7b — <b>die Strommatrix ohne Tarifzonen</b> (Entscheid Q11, Anwender
    /// 22.09.2026: „kein HT/NT").
    ///
    /// <para><b>Die Lage vorher.</b> Die Matrix trennte jede Stunde nach Winter/Sommer ×
    /// HT/NT, führte je Zone eine Teilsumme und bepreiste sie im Zonenmodell des
    /// Tarifsatzes mit vier Bezugs- und vier Einspeisepreisen samt einer zweistufigen
    /// Staffel auf die höchste Stundenlast. Gespeichert wurde eine Zeile je Zone in
    /// <c>Tab_ErgebnisStromMatrix</c>.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Die Matrix führt nur noch Jahressummen
    /// und Lastbilder — keine Zone, keinen Preis; der Tarifsatz liefert ihr allein die
    /// Winterspanne der Lastbilder. Ein Lauf schreibt EINE Jahreszeile und ersetzt dabei
    /// einen Altstand mit Zonenzeilen, statt ihn stehen zu lassen; der Leser summiert
    /// einen solchen Altstand zu denselben Jahressummen, statt eine Zone als Jahr zu
    /// lesen. Den Datenteil des Schemaschritts 103 prüft
    /// <see cref="ZeitzonentarifAbloesungTests"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromMatrixOhneZonenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Wärmepumpenprojekt (Netzbezug laut Testdatenbank 19,08 MWh/a).</summary>
        private const int PROJEKT_WP = 1026;

        /// <summary>Ein Projekt ohne gespeicherte Matrix — Ziel des Altstands.</summary>
        private const int PROJEKT_ALTSTAND = 1040;

        private const BindingFlags ALLE = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static;

        // =================================================================
        //  1 — Die Matrix (ohne Datenbank)
        // =================================================================

        /// <summary>
        /// Die Zonen, ihre Preisrechnung und die Zonenfelder des Tarifsatzes gibt es
        /// nicht mehr — ein Aufrufer, der sie sucht, findet nichts statt einer Zahl, die
        /// nicht mehr gilt.
        /// </summary>
        [Fact]
        public void Matrix_und_Tarifsatz_kennen_keine_Tarifzonen_mehr()
        {
            foreach (string name in new[]
                     {
                         "Z_WINTER_HT", "Z_WINTER_NT", "Z_SOMMER_HT", "Z_SOMMER_NT",
                         "Zone", "Zonen", "Hole", "ZonenName",
                         "Bezugskosten", "Einspeiseerloes", "EinspeiseerloesPv", "Leistungspreis"
                     })
                Assert.Empty(typeof(StromMatrix).GetMember(name, ALLE));

            foreach (string name in new[]
                     {
                         "HtVonStunde", "HtBisStunde",
                         "PreisBezugWinterHT", "PreisBezugWinterNT", "PreisBezugSommerHT", "PreisBezugSommerNT",
                         "PreisEinspWinterHT", "PreisEinspWinterNT", "PreisEinspSommerHT", "PreisEinspSommerNT",
                         "StaffelGrenzeKW", "StaffelPreis1EurKW", "StaffelPreis2EurKW"
                     })
                Assert.Empty(typeof(TarifParameter).GetMember(name, ALLE));

            Assert.Equal("Jahr", StromMatrix.ZEILE_JAHR);
        }

        /// <summary>
        /// Die Summen sind die schlichten Jahressummen der Stundenreihen, gleich zu
        /// welcher Tageszeit und an welchem Wochentag eine Menge anfällt. Die Reihe trägt
        /// ein HT/NT-Profil (Mo–Fr 6–22 Uhr 3 kWh, sonst 2 kWh) und eine Spitze von
        /// 12 kWh am 15. Januar um 10 Uhr — früher vier Teilsummen, jetzt eine.
        /// </summary>
        [Fact]
        public void Die_Matrix_fuehrt_die_Jahressummen_ohne_Zonen()
        {
            ZeitreihenSatz z = Reihen(out double bezugMWh, spitzeStunde: SPITZE_JANUAR);
            StromMatrix m = StromMatrix.Baue(z, new TarifParameter());

            Assert.NotNull(m);
            Assert.Equal(bezugMWh, m.BezugGesamtMWh, 9);
            Assert.Equal(BEDARF_KWH * ZeitreihenSatz.Stunden / 1000.0, m.BedarfGesamtMWh, 9);
            Assert.Equal(BHKW_KWH * ZeitreihenSatz.Stunden / 1000.0, m.KwkEigenGesamtMWh, 9);   // min(BHKW, Bedarf)
            Assert.Equal(0.0, m.KwkEinspeisungGesamtMWh, 9);
            Assert.Equal(0.0, m.EinspeisungPvGesamtMWh, 9);
            Assert.Equal(SPITZE_KWH, m.MaxBezugKW, 9);
            Assert.Equal(SPITZE_KWH, m.LastBezug.MaxMonat[0], 9);     // Januar
            Assert.Equal(HT_KWH, m.LastBezug.MaxMonat[5], 9);         // Juni: nur das Profil

            // Dieselben Mengen, um sechs Stunden verschoben: andere Tageszeiten, andere
            // „Zonen" - dieselben Jahressummen.
            StromMatrix verschoben = StromMatrix.Baue(Verschiebe(z, 6), new TarifParameter());
            Assert.Equal(m.BezugGesamtMWh, verschoben.BezugGesamtMWh, 9);
            Assert.Equal(m.MaxBezugKW, verschoben.MaxBezugKW, 9);
        }

        /// <summary>
        /// Der Tarifsatz liefert der Matrix nur noch die Winterspanne — die Trennung von
        /// Sommer- und Wintermaximum der Lastbilder (Leistungspreismodell „Staffel" des
        /// Rollentarifs). Die Spitze am 15. Oktober ist mit der Spanne Oktober–März ein
        /// Wintermaximum, mit November–Februar ein Sommermaximum; die Summen bleiben.
        /// </summary>
        [Fact]
        public void Der_Tarifsatz_liefert_nur_die_Winterspanne()
        {
            ZeitreihenSatz z = Reihen(out _, spitzeStunde: SPITZE_OKTOBER);

            StromMatrix oktober = StromMatrix.Baue(z, new TarifParameter { WinterVonMonat = 10, WinterBisMonat = 3 });
            StromMatrix november = StromMatrix.Baue(z, new TarifParameter { WinterVonMonat = 11, WinterBisMonat = 2 });

            Assert.Equal(SPITZE_KWH, oktober.LastBezug.MaxWinter, 9);
            Assert.Equal(HT_KWH, oktober.LastBezug.MaxSommer, 9);
            Assert.Equal(HT_KWH, november.LastBezug.MaxWinter, 9);
            Assert.Equal(SPITZE_KWH, november.LastBezug.MaxSommer, 9);

            Assert.Equal(oktober.BezugGesamtMWh, november.BezugGesamtMWh, 9);
            Assert.Equal(oktober.MaxBezugKW, november.MaxBezugKW, 9);
            Assert.Equal(oktober.BedarfGesamtMWh, november.BedarfGesamtMWh, 9);
        }

        // =================================================================
        //  2 — Persistenz und Leser (Testdatenbank)
        // =================================================================

        /// <summary>
        /// Ein Lauf schreibt EINE Jahreszeile (<see cref="StromMatrix.ZEILE_JAHR"/>) mit
        /// den gerundeten Jahressummen — und ersetzt dabei einen Altstand mit vier
        /// Zonenzeilen, statt ihn stehen zu lassen. Der Leser liest dieselben Summen.
        /// </summary>
        [Fact]
        public void Ein_Lauf_schreibt_eine_Jahreszeile_und_ersetzt_die_Zonenzeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Altstand: vier Zonenzeilen mit Werten, die der Lauf nicht erzeugt.
            Altstand(PROJEKT_WP);
            Assert.Equal(4, Matrixzeilen(PROJEKT_WP).Rows.Count);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_WP);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT_WP,
                IstStamm = true,
                Projektname = "Prüffall E7b",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT_WP)
            };
            KostenEmissionRechner.Berechne(v);
            v.Zeitreihen = Reihen(out double bezugMWh, spitzeStunde: SPITZE_JANUAR);

            var daten = new BerichtsDaten { IdStamm = PROJEKT_WP, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            ctrl.Berechne(daten, p);

            DataTable t = Matrixzeilen(PROJEKT_WP);
            Assert.Single(t.Rows);
            DataRow r = t.Rows[0];
            Assert.Equal(StromMatrix.ZEILE_JAHR, Convert.ToString(r["Zone"]));
            Assert.Equal(Math.Round(bezugMWh, 3), Convert.ToDouble(r["BezugMWh"]), 9);
            Assert.Equal(Math.Round(BEDARF_KWH * ZeitreihenSatz.Stunden / 1000.0, 3),
                         Convert.ToDouble(r["BedarfMWh"]), 9);
            Assert.Equal(SPITZE_KWH, Convert.ToDouble(r["MaxBezugKW"]), 9);

            StromMatrix m = ctrl.LadeStromMatrix(new List<int> { PROJEKT_WP })[PROJEKT_WP];
            Assert.Equal(Math.Round(bezugMWh, 3), m.BezugGesamtMWh, 9);
            Assert.Equal(SPITZE_KWH, m.MaxBezugKW, 9);
        }

        /// <summary>
        /// Ein Altstand mit vier Zonenzeilen (eine Datenbank vor Schemaschritt 103) wird
        /// weder falsch gelesen noch als Zone gezeigt: Der Leser summiert alle Zeilen des
        /// Projekts zu den Jahressummen, die höchste Stundenlast ist das Maximum.
        /// </summary>
        [Fact]
        public void Der_Leser_summiert_einen_Altstand_mit_vier_Zonenzeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Altstand(PROJEKT_ALTSTAND);

            StromMatrix m = new WirtschaftlichkeitCtrl()
                .LadeStromMatrix(new List<int> { PROJEKT_ALTSTAND })[PROJEKT_ALTSTAND];

            Assert.Equal(5.0, m.BezugGesamtMWh, 9);           // 1,5 + 1,0 + 2,0 + 0,5
            Assert.Equal(1.0, m.EinspeisungPvGesamtMWh, 9);   // 0,25 + 0,25 + 0,5 + 0
            Assert.Equal(2.5, m.KwkEigenGesamtMWh, 9);        // 0,75 + 0,5 + 1,0 + 0,25
            Assert.Equal(0.5, m.KwkEinspeisungGesamtMWh, 9);  // 0,125 + 0,125 + 0,25 + 0
            Assert.Equal(7.0, m.BedarfGesamtMWh, 9);          // 2,0 + 1,5 + 2,5 + 1,0
            Assert.Equal(9.6, m.MaxBezugKW, 9);               // Maximum, keine Summe
        }

        // =================================================================
        //  Vorrichtung
        // =================================================================

        private const double NT_KWH = 2.0;
        private const double HT_KWH = 3.0;
        private const double SPITZE_KWH = 12.0;
        private const double BEDARF_KWH = 4.0;
        private const double BHKW_KWH = 1.0;

        /// <summary>15. Januar 2026, 10 Uhr (Tag 14).</summary>
        private const int SPITZE_JANUAR = 14 * 24 + 10;

        /// <summary>15. Oktober 2026, 10 Uhr (Tag 287 — 273 Tage bis zum 1. Oktober).</summary>
        private const int SPITZE_OKTOBER = 287 * 24 + 10;

        /// <summary>
        /// Netzbezug mit HT/NT-Profil im Referenzjahr 2026 (Mo–Fr 6–22 Uhr
        /// <see cref="HT_KWH"/>, sonst <see cref="NT_KWH"/>) und einer Spitze; Strombedarf
        /// und BHKW-Strom flach.
        /// </summary>
        private static ZeitreihenSatz Reihen(out double bezugMWh, int spitzeStunde)
        {
            int n = ZeitreihenSatz.Stunden;
            var bezug = new double[n];
            var bedarf = new double[n];
            var bhkw = new double[n];
            DateTime start = new DateTime(2026, 1, 1);
            double summe = 0;
            for (int h = 0; h < n; h++)
            {
                DateTime t = start.AddHours(h);
                bool werktag = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                bezug[h] = h == spitzeStunde ? SPITZE_KWH
                         : werktag && t.Hour >= 6 && t.Hour < 22 ? HT_KWH : NT_KWH;
                bedarf[h] = BEDARF_KWH;
                bhkw[h] = BHKW_KWH;
                summe += bezug[h] / 1000.0;
            }
            bezugMWh = summe;

            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = bezug;
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = bedarf;
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkw;
            return z;
        }

        /// <summary>Dieselben Reihen, um <paramref name="stunden"/> Stunden rotiert.</summary>
        private static ZeitreihenSatz Verschiebe(ZeitreihenSatz quelle, int stunden)
        {
            var z = new ZeitreihenSatz();
            foreach (string schluessel in quelle.Reihen.Keys.ToList())
            {
                double[] alt = quelle.Reihen[schluessel];
                var neu = new double[alt.Length];
                for (int h = 0; h < alt.Length; h++) neu[(h + stunden) % alt.Length] = alt[h];
                z.Reihen[schluessel] = neu;
            }
            return z;
        }

        /// <summary>
        /// Ein Stand von vor E7b: vier Zonenzeilen je Projekt — die Schlüssel, die die
        /// Matrix bis dahin schrieb (<see cref="ZeitzonentarifAbloesung.ZONEN"/>).
        /// </summary>
        private static void Altstand(int projekt)
        {
            DataRepository.ExecuteNonQuery("DELETE FROM [Tab_ErgebnisStromMatrix] WHERE [ID_Projekt] = ?",
                                           new DbParam("@p", projekt));
            double[,] w =
            {
                // Bezug, PV, KWK eigen, KWK Einspeisung, Max, Bedarf
                { 1.5, 0.25, 0.75, 0.125, 8.4, 2.0 },
                { 1.0, 0.25, 0.5, 0.125, 5.0, 1.5 },
                { 2.0, 0.5, 1.0, 0.25, 9.6, 2.5 },
                { 0.5, 0.0, 0.25, 0.0, 3.0, 1.0 }
            };
            for (int i = 0; i < ZeitzonentarifAbloesung.ZONEN.Length; i++)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO [Tab_ErgebnisStromMatrix] ([ID], [ID_Projekt], [Zone], [BezugMWh], [EinspPvMWh], " +
                    "[KwkEigenMWh], [KwkEinspMWh], [MaxBezugKW], [BedarfMWh], [Zeitstempel]) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    new DbParam("@id", 98000 + projekt * 10 % 1000 + i), new DbParam("@p", projekt),
                    new DbParam("@z", ZeitzonentarifAbloesung.ZONEN[i]),
                    new DbParam("@b", w[i, 0]), new DbParam("@pv", w[i, 1]), new DbParam("@ke", w[i, 2]),
                    new DbParam("@ki", w[i, 3]), new DbParam("@mx", w[i, 4]), new DbParam("@bd", w[i, 5]),
                    new DbParam("@t", "2026-08-21 13:06:32"));
        }

        private static DataTable Matrixzeilen(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT * FROM [Tab_ErgebnisStromMatrix] WHERE [ID_Projekt] = ? ORDER BY [ID]",
                new DbParam("@p", projekt));
            Assert.NotNull(t);
            return t;
        }
    }
}
