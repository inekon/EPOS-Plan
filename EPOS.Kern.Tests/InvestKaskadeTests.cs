using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERBEFUND W5‑B‑7 (08.09.2026): „% der Investitionskosten ist immer 0."
    ///
    /// <para>Bis zum Befund lasen DREI Stellen dieselben Kategorie-1-Zeilen auf DREI
    /// Arten — Kapitalwertrechnung (volle Kaskade), Dialog Kostenverwaltung (nur die
    /// GESPEICHERTE Menge, für „% der Investition" gibt es nie eine) und die Seite
    /// „Berichte &amp; Kosten" (rohes <c>SUM(EingegebenerWert)</c>). Seither fragen alle
    /// drei <see cref="InvestKaskade"/>. Diese Fälle halten die Kaskadenregeln fest und
    /// belegen, dass Dialog und Kostenseite dieselbe Zahl zeigen.</para>
    ///
    /// <para><b>Das Beispiel des Anwenders</b> (Wärmepumpe des Projekts 1040 als Träger,
    /// weil die Testdatenbank keine Photovoltaik-Kostenzeilen führt): eine direkte Zeile
    /// über 5.660,00 € und drei Prozentzeilen mit 3 %, 10 % und 10 % ergeben
    /// 5.660,00 + 169,80 + 566,00 + 566,00 = <b>6.961,80 €</b> — genau die Zahl, die der
    /// Anwender in der Wirtschaftlichkeit sah, während Dialog (5.660,00 €) und
    /// Anlagentabelle etwas anderes zeigten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class InvestKaskadeTests
    {
        private const int PROJEKT = 1040;
        private const int WAERMEPUMPE = 1;        // Tab_KostenKomponente.ID
        private const int ANLAGE = 14728;         // Tab_Energieanlagen.ID der WP in 1040

        private const int ID_HAUPT = 101600487;   // BETRAG, IsMainComponent
        private const int ID_P3 = 101600494;
        private const int ID_P10A = 101600495;
        private const int ID_P10B = 101600497;
        private static readonly int[] ID_NULLZEILEN =
            { 101600491, 101600492, 101600493, 101600496 };

        /// <summary>
        /// Legt das Beispiel an: 5.660,00 € direkt, dazu 3 % / 10 % / 10 % der
        /// Investition. Geschrieben wird in die ARBEITSKOPIE der Testdatenbank.
        /// </summary>
        private static void BeispielAnlegen()
        {
            Setze(ID_HAUPT, 5660.0, DbWerte.BEMESSUNG_BETRAG, null);
            foreach (int id in ID_NULLZEILEN) Setze(id, 0.0, DbWerte.BEMESSUNG_BETRAG, null);
            Setze(ID_P3, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 3.0);
            Setze(ID_P10A, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0);
            Setze(ID_P10B, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0);
        }

        private static void Setze(int id, double wert, string bemessung, double? satz)
        {
            var p = new DbParam("@e", DbParamTyp.Double);
            p.Wert = satz.HasValue ? (object)satz.Value : DBNull.Value;
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ?, Bemessung = ?, " +
                "Einheitpreis = ?, Menge = NULL, Kostenart = ? WHERE ID = " + id,
                new DbParam("@w", wert),
                new DbParam("@b", bemessung),
                p,
                new DbParam("@k", DbWerte.KOSTENART_KAPITALGEBUNDEN));
        }

        private static Dictionary<int, InvestKaskade.Zeile> Kaskade()
        {
            return InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
        }

        // =====================================================================
        // Die Kaskadenregeln (Konzept Kostendialoge § 5.3)
        // =====================================================================

        [Fact]
        public void Prozentzeile_rechnet_Basis_mal_Satz_aus_den_direkten_Zeilen_der_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Dictionary<int, InvestKaskade.Zeile> k = Kaskade();

            Assert.Equal(5660.00, k[ID_HAUPT].Betrag, 2);
            Assert.Equal(169.80, k[ID_P3].Betrag, 2);
            Assert.Equal(566.00, k[ID_P10A].Betrag, 2);
            Assert.Equal(566.00, k[ID_P10B].Betrag, 2);

            // Die BASIS ist die Auskunft für den Werkzeugtipp — dieselbe Zahl, mit der
            // gerechnet wurde, nicht eine zweite Rechnung.
            Assert.Equal(5660.00, k[ID_P3].Basis.Value, 2);
            Assert.Equal(5660.00, k[ID_P10A].Basis.Value, 2);
            Assert.Null(k[ID_HAUPT].Basis);          // BETRAG hat keine Bezugsgröße
        }

        [Fact]
        public void Prozentzeilen_zaehlen_einander_nicht_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // ANWENDERENTSCHEID I-3: Basis sind AUSSCHLIESSLICH die direkten Zeilen.
            // Zählten die Prozentzeilen einander mit, hinge das Ergebnis an der
            // Lesereihenfolge (die Abfrage trägt kein ORDER BY) und die zweite
            // 10-%-Zeile bekäme mehr als die erste.
            Dictionary<int, InvestKaskade.Zeile> k = Kaskade();
            Assert.Equal(k[ID_P10A].Betrag, k[ID_P10B].Betrag, 6);
            Assert.Equal(5660.00, k[ID_P10B].Basis.Value, 2);
        }

        [Fact]
        public void Basis_faellt_ohne_Anlagenzeilen_auf_die_Komponente_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // Die Wärmepumpe des Projekts 1040 führt nur EINE Anlage. Eine zweite
            // direkte Zeile OHNE Anlagenbezug gehört damit zur Komponente, aber nicht
            // zur Anlage: Solange die Prozentzeile an der Anlage hängt, bleibt ihre
            // Basis die Anlagensumme.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = 4000, ID_Anlage = NULL " +
                "WHERE ID = " + ID_NULLZEILEN[0]);
            Assert.Equal(5660.00, Kaskade()[ID_P10A].Basis.Value, 2);

            // Verliert die Prozentzeile ihren Anlagenbezug, gilt die KOMPONENTENsumme
            // (5.660 + 4.000) — die stufige Regel Anlage → Komponente → Projekt.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL WHERE ID = " + ID_P10A);
            Dictionary<int, InvestKaskade.Zeile> k = Kaskade();
            Assert.Equal(9660.00, k[ID_P10A].Basis.Value, 2);
            Assert.Equal(966.00, k[ID_P10A].Betrag, 2);
        }

        [Fact]
        public void Zuschuss_bleibt_ausserhalb_von_Basis_und_Anlagensumme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            // Eine zweite direkte Zeile über 1.000 € — erst als Investition, dann als
            // Zuschuss. Der Zuschuss darf weder die Basis noch die Anlagensumme heben.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = 1000 WHERE ID = " + ID_NULLZEILEN[1]);
            Assert.Equal(6660.00, Kaskade()[ID_P10A].Basis.Value, 2);

            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Kostenart = ? WHERE ID = " + ID_NULLZEILEN[1],
                new DbParam("@k", DbWerte.KOSTENART_ZUSCHUSS));

            Dictionary<int, InvestKaskade.Zeile> k = Kaskade();
            Assert.True(k[ID_NULLZEILEN[1]].Zuschuss);
            Assert.Equal(5660.00, k[ID_P10A].Basis.Value, 2);
            Assert.Equal(6961.80,
                KostenSummenCtrl.AnlagenSumme(PROJEKT, DbWerte.KOSTEN_KATEGORIE_INVESTITION, ANLAGE), 2);
        }

        // =====================================================================
        // Dieselbe Zahl an allen drei Stellen
        // =====================================================================

        [Fact]
        public void Dialogzeilen_zeigen_den_Betrag_der_Kaskade_statt_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, WAERMEPUMPE,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, ANLAGE);

            double summe = 0;
            var betraege = new Dictionary<int, double>();
            foreach (KostenProjektPositionenCtrl.Zeile z in zeilen)
            {
                betraege[z.Raster.Id] = z.Raster.BetragNetto ?? 0;
                summe += z.Raster.BetragNetto ?? 0;
            }

            // Das Fehlerbild: 0,00 in jeder Prozentzeile, 5.660,00 im Summenfuß.
            Assert.Equal(169.80, betraege[ID_P3], 2);
            Assert.Equal(566.00, betraege[ID_P10A], 2);
            Assert.Equal(566.00, betraege[ID_P10B], 2);
            Assert.Equal(6961.80, summe, 2);

            // Die Basis steht für den Werkzeugtipp bereit („3 % von 5.660,00 €").
            foreach (KostenProjektPositionenCtrl.Zeile z in zeilen)
                if (z.Raster.Id == ID_P3) Assert.Equal(5660.00, z.Basis.Value, 2);
        }

        [Fact]
        public void Anlagensumme_der_Kostenseite_ist_die_Summe_der_Kaskade()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // (1) die Zahl der Anlagenzeile und der Kostenzeile im Anlagendialog
            Assert.Equal(6961.80,
                KostenSummenCtrl.AnlagenSumme(PROJEKT, DbWerte.KOSTEN_KATEGORIE_INVESTITION, ANLAGE), 2);

            // (2) dieselbe Zahl in der Tabelle der Kostenseite
            DataTable t = KostenSummenCtrl.LiesAnlagenSummen(
                PROJEKT, DbWerte.KOSTEN_KATEGORIE_INVESTITION);
            Assert.NotNull(t);
            double? ausTabelle = null;
            foreach (DataRow r in t.Rows)
                if (r["ID_Anlage"] != DBNull.Value && Convert.ToInt32(r["ID_Anlage"]) == ANLAGE)
                    ausTabelle = Convert.ToDouble(r["Summe"]);
            Assert.True(ausTabelle.HasValue);
            Assert.Equal(6961.80, ausTabelle.Value, 2);

            // (3) und in der Komponentensumme, die Photovoltaik-Vergütung und
            //     Rückfallweg der Kostenseite lesen
            DataTable kt = KostenSummenCtrl.LiesKomponentenSummen(
                PROJEKT, DbWerte.KOSTEN_KATEGORIE_INVESTITION);
            double? jeKomponente = null;
            foreach (DataRow r in kt.Rows)
                if (string.Equals(Convert.ToString(r["Komponente"]),
                                  DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE, StringComparison.Ordinal))
                    jeKomponente = Convert.ToDouble(r["Summe"]);
            Assert.True(jeKomponente.HasValue);
            Assert.Equal(6961.80, jeKomponente.Value, 2);
        }

        [Fact]
        public void Kachel_und_Anlagentabelle_zeigen_dieselbe_Investition()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // Die Kachel „Investition" der Kostenseite liest LiesInvestitionen …
            double zuschuss;
            double kachel = 0;
            foreach (KapitalwertRechner.InvestPosition p in
                     WirtschaftlichkeitCtrl.LiesInvestitionen(
                         PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, out zuschuss))
                kachel += p.Betrag;

            // … die Tabelle darunter die Anlagensummen. Beide müssen übereinstimmen.
            DataTable t = KostenSummenCtrl.LiesAnlagenSummen(
                PROJEKT, DbWerte.KOSTEN_KATEGORIE_INVESTITION);
            double tabelle = 0;
            foreach (DataRow r in t.Rows) tabelle += Convert.ToDouble(r["Summe"]);

            Assert.Equal(kachel, tabelle, 6);
            Assert.Equal(0.0, zuschuss, 6);
        }

        // =====================================================================
        // Regression: LiesInvestitionen rechnet wie vor dem Herauslösen
        // =====================================================================

        /// <summary>
        /// Die Kaskade ist aus <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c> HERAUSGELÖST,
        /// nicht nachgebaut — die Kapitalwertrechnung muss deshalb dieselben Summen
        /// liefern wie vorher.
        ///
        /// <para><b>Die Vergleichszahlen</b> sind am 08.09.2026 auf der unberührten
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> gemessen. Auf diesem Stand trägt
        /// KEINE Kategorie-1-Zeile einen Satz, eine Menge, einen Best-/Worst-Wert oder
        /// die Kostenart „Zuschuss"; jede Zeile ist damit ihr eigener
        /// <c>EingegebenerWert</c>, in allen drei Szenarien gleich. Genau das prüft
        /// dieser Fall — Zeile für Zeile gegen die Datenbank und Projekt für Projekt
        /// gegen die gemessene Summe.</para>
        /// </summary>
        [Theory]
        [InlineData(1007, 0.0)]
        [InlineData(1018, 45312.5)]
        [InlineData(1019, 7001.0)]
        [InlineData(1023, 7001.0)]
        [InlineData(1024, 12001.0)]
        [InlineData(1026, 6775.5)]
        [InlineData(1028, 6775.5)]
        [InlineData(1029, 6775.5)]
        [InlineData(1030, 410000.0)]
        [InlineData(1031, 45312.5)]
        [InlineData(1032, 10001.0)]
        [InlineData(1040, 54975.5)]
        [InlineData(1041, 6775.5)]
        [InlineData(1042, 13000.0)]
        [InlineData(1043, 19775.5)]
        [InlineData(1044, 19775.5)]
        public void LiesInvestitionen_bleibt_zahlengleich(int idProjekt, double erwartet)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                double zuschuss;
                double summe = 0;
                foreach (KapitalwertRechner.InvestPosition p in
                         WirtschaftlichkeitCtrl.LiesInvestitionen(idProjekt, szenario, out zuschuss))
                    summe += p.Betrag;

                Assert.Equal(erwartet, summe, 4);
                Assert.Equal(0.0, zuschuss, 6);
            }
        }
    }
}
