using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Strommengen-Matrix Winter/Sommer × HT/NT (Konzept Kap. 2.5 / Stufe W3,
    /// Phase 8) — gebaut aus den Stundenreihen der In-Memory-Simulation
    /// (ZeitreihenSatz). Vereinfachtes Tarifmodell (Entscheidung 11.08.2026):
    /// Winterzeitraum als Monatsspanne, EIN HT-Fenster Mo–Fr; Wochentage über
    /// das Referenzjahr 2026 (Standardjahr, kein Schaltjahr).
    ///
    /// Je Zone werden geführt:
    ///  - Netzbezug [MWh]           (Zeitreihe NETZBEZUG)
    ///  - PV-Einspeisung [MWh]      (Zeitreihe PV_UEBERSCHUSS)
    ///  - KWK-Eigenstrom [MWh]      (stundenweise min(BHKW-Strom, Strombedarf))
    ///  - KWK-Einspeisung [MWh]     (BHKW-Strom − Eigenanteil)
    /// plus die Jahres-Bezugsspitze [kW] für die Leistungspreis-Staffel.
    ///
    /// Die KWK-Aufteilung ist eine dokumentierte Näherung: die Simulation führt
    /// den BHKW-Strom nicht getrennt nach Eigennutzung/Einspeisung — die
    /// stundenweise min-Regel bildet die Gleichzeitigkeit von Erzeugung und
    /// Bedarf ab (Grundlage des KWKG-Splits, Entscheidung 11.08.2026).
    /// </summary>
    public class StromMatrix
    {
        /// <summary>Zonen-Schlüssel (auch Persistenz in Tab_ErgebnisStromMatrix).</summary>
        public const string Z_WINTER_HT = "Winter HT";
        public const string Z_WINTER_NT = "Winter NT";
        public const string Z_SOMMER_HT = "Sommer HT";
        public const string Z_SOMMER_NT = "Sommer NT";
        public static readonly string[] Zonen = { Z_WINTER_HT, Z_WINTER_NT, Z_SOMMER_HT, Z_SOMMER_NT };

        /// <summary>Mengen einer Tarifzone [MWh].</summary>
        public class Zone
        {
            public string Name = "";
            public double BezugMWh;
            public double EinspeisungPvMWh;
            public double KwkEigenMWh;
            public double KwkEinspeisungMWh;
        }

        public Dictionary<string, Zone> ZonenWerte = new Dictionary<string, Zone>();

        /// <summary>Jahres-Bezugsspitze [kW] (Basis der Leistungspreis-Staffel).</summary>
        public double MaxBezugKW;

        /// <summary>true, wenn die STROMBEDARF-Reihe fehlte — der KWK-Split gilt dann
        /// als „alles Eigenstrom" und wird im Ergebnis als Hinweis ausgewiesen.</summary>
        public bool StrombedarfFehlt;

        public Zone Hole(string name)
        { return ZonenWerte.ContainsKey(name) ? ZonenWerte[name] : null; }

        /// <summary>Summe Netzbezug über alle Zonen [MWh] (Plausibilitätsabgleich).</summary>
        public double BezugGesamtMWh
        {
            get
            {
                double s = 0;
                foreach (Zone z in ZonenWerte.Values) s += z.BezugMWh;
                return s;
            }
        }

        // ------------------------------------------------------------- Aufbau

        /// <summary>
        /// Baut die Matrix aus den Stundenreihen. Liefert null, wenn die
        /// Bezugsreihe fehlt (dann bleibt die Flat-Preisrechnung aktiv).
        /// </summary>
        public static StromMatrix Baue(ZeitreihenSatz zeitreihen, TarifParameter tarif)
        {
            if (zeitreihen == null || tarif == null) return null;
            double[] bezug = zeitreihen.Hole(ZeitreihenSatz.NETZBEZUG);
            // Nur ein VOLLES Jahr ist eine gültige Basis — kürzere Reihen würden die
            // Volljahres-Flatkosten still durch Teiljahreswerte ersetzen (Review Phase 8).
            if (bezug == null || bezug.Length < ZeitreihenSatz.Stunden) return null;

            double[] pvUeber = zeitreihen.Hole(ZeitreihenSatz.PV_UEBERSCHUSS);
            double[] bhkw = zeitreihen.Hole(ZeitreihenSatz.BHKW_STROM);
            double[] bedarf = zeitreihen.Hole(ZeitreihenSatz.STROMBEDARF);
            double[] pvGenutzt = zeitreihen.Hole(ZeitreihenSatz.PV_GENUTZT);

            var m = new StromMatrix();
            m.StrombedarfFehlt = bedarf == null;
            foreach (string name in Zonen) m.ZonenWerte[name] = new Zone { Name = name };

            // Referenzjahr für die Wochentage (2026 beginnt an einem Donnerstag).
            DateTime start = new DateTime(2026, 1, 1);
            int stunden = ZeitreihenSatz.Stunden;

            for (int h = 0; h < stunden; h++)
            {
                DateTime t = start.AddHours(h);
                Zone z = m.ZonenWerte[ZonenName(t, tarif)];

                double b = bezug[h] / 1000.0;                     // kWh → MWh
                z.BezugMWh += b;
                if (b * 1000.0 > m.MaxBezugKW) m.MaxBezugKW = b * 1000.0;   // kWh/h ≙ kW

                if (pvUeber != null && h < pvUeber.Length)
                    z.EinspeisungPvMWh += pvUeber[h] / 1000.0;

                if (bhkw != null && h < bhkw.Length)
                {
                    double erz = bhkw[h];
                    double eigen = erz;   // ohne Bedarfsreihe: alles Eigenstrom (Hinweis via StrombedarfFehlt)
                    if (bedarf != null && h < bedarf.Length)
                    {
                        // PV-Eigennutzung derselben Stunde vom Bedarf abziehen —
                        // sonst wäre der KWK-Eigenanteil systematisch zu hoch.
                        double restBedarf = bedarf[h];
                        if (pvGenutzt != null && h < pvGenutzt.Length) restBedarf -= pvGenutzt[h];
                        eigen = Math.Min(erz, Math.Max(0, restBedarf));
                    }
                    z.KwkEigenMWh += eigen / 1000.0;
                    z.KwkEinspeisungMWh += Math.Max(0, erz - eigen) / 1000.0;
                }
            }
            return m;
        }

        /// <summary>Zonenzuordnung einer Stunde nach dem vereinfachten Tarifmodell.</summary>
        private static string ZonenName(DateTime t, TarifParameter tarif)
        {
            bool winter = IstWinter(t.Month, tarif.WinterVonMonat, tarif.WinterBisMonat);

            // HT: Mo–Fr innerhalb des Fensters [HtVonStunde, HtBisStunde).
            bool werktag = t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
            bool ht = werktag && t.Hour >= tarif.HtVonStunde && t.Hour < tarif.HtBisStunde;

            return winter ? (ht ? Z_WINTER_HT : Z_WINTER_NT)
                          : (ht ? Z_SOMMER_HT : Z_SOMMER_NT);
        }

        /// <summary>Monat in der (ggf. über den Jahreswechsel laufenden) Winterspanne?</summary>
        public static bool IstWinter(int monat, int von, int bis)
        {
            if (von <= bis) return monat >= von && monat <= bis;     // z. B. 1–3
            return monat >= von || monat <= bis;                     // z. B. 10–3
        }

        // ------------------------------------------------------------- Kosten

        /// <summary>Strom-Bezugskosten p. a. [€] nach Zonenpreisen + Leistungspreis-Staffel.</summary>
        public double Bezugskosten(TarifParameter tarif)
        {
            double summe = 0;
            summe += Kosten(Z_WINTER_HT, tarif.PreisBezugWinterHT);
            summe += Kosten(Z_WINTER_NT, tarif.PreisBezugWinterNT);
            summe += Kosten(Z_SOMMER_HT, tarif.PreisBezugSommerHT);
            summe += Kosten(Z_SOMMER_NT, tarif.PreisBezugSommerNT);
            summe += Leistungspreis(tarif);
            return summe;
        }

        /// <summary>Einspeiseerlöse p. a. [€] (PV- + KWK-Einspeisung) nach Zonenpreisen.</summary>
        public double Einspeiseerloes(TarifParameter tarif)
        {
            double summe = 0;
            summe += Erloes(Z_WINTER_HT, tarif.PreisEinspWinterHT);
            summe += Erloes(Z_WINTER_NT, tarif.PreisEinspWinterNT);
            summe += Erloes(Z_SOMMER_HT, tarif.PreisEinspSommerHT);
            summe += Erloes(Z_SOMMER_NT, tarif.PreisEinspSommerNT);
            return summe;
        }

        /// <summary>Zweistufige Leistungspreis-Staffel auf die Bezugsspitze [€/a].</summary>
        public double Leistungspreis(TarifParameter tarif)
        {
            if (MaxBezugKW <= 0) return 0;
            double grenze = Math.Max(0, tarif.StaffelGrenzeKW);
            double stufe1 = Math.Min(MaxBezugKW, grenze);
            double stufe2 = Math.Max(0, MaxBezugKW - grenze);
            return stufe1 * tarif.StaffelPreis1EurKW + stufe2 * tarif.StaffelPreis2EurKW;
        }

        private double Kosten(string zone, double preisEurKWh)
        {
            Zone z = Hole(zone);
            return z == null ? 0 : z.BezugMWh * 1000.0 * preisEurKWh;
        }

        private double Erloes(string zone, double preisEurKWh)
        {
            Zone z = Hole(zone);
            return z == null ? 0 : (z.EinspeisungPvMWh + z.KwkEinspeisungMWh) * 1000.0 * preisEurKWh;
        }

        // ------------------------------------------------------------- KWK-Summen

        /// <summary>KWK-Eigenstrom gesamt [MWh/a] (Basis KWKG-Eigenstromsatz).</summary>
        public double KwkEigenGesamtMWh
        {
            get { double s = 0; foreach (Zone z in ZonenWerte.Values) s += z.KwkEigenMWh; return s; }
        }

        /// <summary>KWK-Einspeisung gesamt [MWh/a] (Basis KWKG-Einspeisesatz).</summary>
        public double KwkEinspeisungGesamtMWh
        {
            get { double s = 0; foreach (Zone z in ZonenWerte.Values) s += z.KwkEinspeisungMWh; return s; }
        }
    }
}
