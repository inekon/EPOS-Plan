using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Ergebnis des Zapfprofilgenerators als Bestandsprofil</b> (Konzept Zapfprofil Bereiche
    /// und Methoden, Vorschlag ZC6): zwölf Monatssummen und eine mittlere Woche, so wie ein
    /// Brauchwasser-Profilsatz sie führt (Kopf <c>Monat_1…12</c>, Typ <c>"1"…"168"</c>).
    /// Unveränderlich; entsteht allein über <see cref="ZapfprofilProfilsatzAbbildung"/>.
    ///
    /// <para><b>Einheiten.</b> Die Monats- und Jahressummen stehen in kWh (Quelleinheit der
    /// Bilanzreihen) und in MWh (Einheit der Profilsätze und der Jahressumme
    /// <c>Z_Projekt_Brauchwasser.Summe</c>); die Anteile in Prozent mit der Summe 100 — so führt der
    /// Katalog seine Wochenprofile. Die mittlere Woche in kWh je Stunde ist zugleich ihre Leistung
    /// in kW.</para>
    /// </summary>
    internal sealed record ZapfprofilProfilsatz
    {
        /// <summary>Trägt der Satz den Zirkulationsverlust (Variante B, ZC‑Q6) oder nur die Zapfwärme (A)?</summary>
        public bool MitZirkulation { get; init; }

        /// <summary>Der Wochentag des 1. Januar im Raster der Klimaregion (Montag = 0 … Sonntag = 6).</summary>
        public int WochentagJan1 { get; init; }

        /// <summary>Die Jahressumme [kWh] der abgebildeten Stundenreihe.</summary>
        public double JahressummeKwh { get; init; }

        /// <summary>Die Jahressumme [MWh] — die Summe der Zuordnungszeile.</summary>
        public double JahressummeMwh { get; init; }

        /// <summary>Die zwölf Monatssummen [kWh], Januar zuerst.</summary>
        public IReadOnlyList<double> MonatssummenKwh { get; init; } = new double[0];

        /// <summary>Die zwölf Monatssummen [MWh] — die Monatswerte des Katalogkopfs.</summary>
        public IReadOnlyList<double> MonatssummenMwh { get; init; } = new double[0];

        /// <summary>Die zwölf Monatsanteile [%], Summe 100 (0 überall, wenn das Jahr 0 trägt).</summary>
        public IReadOnlyList<double> MonatsanteileProzent { get; init; } = new double[0];

        /// <summary>
        /// Die mittlere Woche [kWh je Stunde = kW], 168 Werte, Montag 0 Uhr zuerst: je Wochenstunde
        /// das Mittel aller Jahresstunden, die im Wochentagsraster der Klimaregion auf sie fallen
        /// (52 oder — für den Wochentag des 1. Januar — 53 Tage).
        /// </summary>
        public IReadOnlyList<double> WochenmittelKwh { get; init; } = new double[0];

        /// <summary>Das Wochenprofil [%], 168 Werte, Montag 0 Uhr zuerst, Summe 100 (0 überall ohne Bedarf).</summary>
        public IReadOnlyList<double> WochenanteileProzent { get; init; } = new double[0];

        /// <summary>Die Spitzenstunde der mittleren Woche [kW].</summary>
        public double SpitzenstundeWocheKw { get; init; }

        /// <summary>Die Wochenstunde der Spitze (0 = Montag 0–1 Uhr … 167 = Sonntag 23–24 Uhr).</summary>
        public int SpitzenstundeWocheIndex { get; init; }

        /// <summary>Das Wochenprofil als 7 × 24-Feld (Tag, Stunde) in Prozent — die Form von <see cref="TypProfilCtrl"/>.</summary>
        public double[,] WochenanteileFeld()
        {
            var feld = new double[ZapfprofilProfilsatzAbbildung.WOCHENTAGE, ZapfprofilProfilsatzAbbildung.STUNDEN_TAG];
            for (int i = 0; i < ZapfprofilProfilsatzAbbildung.WOCHENSTUNDEN; i++)
                feld[i / ZapfprofilProfilsatzAbbildung.STUNDEN_TAG, i % ZapfprofilProfilsatzAbbildung.STUNDEN_TAG] = WochenanteileProzent[i];
            return feld;
        }
    }

    /// <summary>
    /// <b>Die Abbildung Zapfprofil → Profilsatz</b> (ZC6): reine Rechenklasse ohne Datenbank,
    /// deterministisch, im Festraster 8760 Stunden, 168 Wochenstunden, 12 Monate, kein Schaltjahr.
    ///
    /// <list type="number">
    /// <item>Stundenreihe: die Zapfwärme, mit <c>mitZirkulation</c> zuzüglich des Zirkulationsverlusts
    /// (Variante B der Annahme ZC‑Q6 — der Bedarf, den der Generator in der Simulation liefert).</item>
    /// <item>Monate: die Summen der Kalendermonate des Gemeinjahrs; ihre Summe ist die Jahressumme.</item>
    /// <item>Woche: Tag d (0-basiert) fällt auf den Wochentag (WochentagJan1 + d) mod 7, Montag = 0 —
    /// dieselbe Regel wie <see cref="Zapfkalender.Wochentag"/> und <see cref="ProfilBedarf"/>. Je Wochenstunde
    /// das Mittel ihrer Jahresstunden; die Anteile sind auf 100 % normiert.</item>
    /// </list>
    ///
    /// <para><b>Was die Abbildung nicht trägt</b>: Ferien, Feiertage, Stochastik und Typtage — das
    /// Bestandsprofil kachelt die mittlere Woche über das Jahr und skaliert sie je Monat auf die
    /// Monatssumme. Erhalten bleiben Jahres- und Monatssummen.</para>
    /// </summary>
    internal static class ZapfprofilProfilsatzAbbildung
    {
        internal const int STUNDEN_JAHR = Zapfkalender.STUNDEN_JAHR;
        internal const int STUNDEN_TAG = Zapfkalender.STUNDEN_TAG;
        internal const int WOCHENTAGE = Zapfkalender.WOCHENTAGE;
        internal const int WOCHENSTUNDEN = WOCHENTAGE * STUNDEN_TAG;
        internal const int MONATE = Zapfkalender.MONATE;

        /// <summary>kWh je MWh — die Monatswerte der Profilsätze und die Jahressumme der Zuordnung führen MWh.</summary>
        internal const double KWH_JE_MWH = 1000.0;

        /// <summary>Die Abbildung eines Generatorergebnisses; <c>null</c> ohne Ergebnis oder ohne Zapfreihe.</summary>
        internal static ZapfprofilProfilsatz Abbilden(ZapfprofilErgebnis e, int wochentagJan1, bool mitZirkulation)
        {
            if (e?.Zapfung == null) return null;
            return Abbilden(e.Zapfung, e.Zirkulation, wochentagJan1, mitZirkulation);
        }

        /// <summary>
        /// Die Abbildung der Reihen <paramref name="zapfung"/> und (mit <paramref name="mitZirkulation"/>)
        /// <paramref name="zirkulation"/>; eine fehlende Zirkulationsreihe zählt 0.
        /// </summary>
        internal static ZapfprofilProfilsatz Abbilden(Bilanzreihe zapfung, Bilanzreihe zirkulation, int wochentagJan1,
                                                      bool mitZirkulation)
        {
            if (zapfung == null) throw new ArgumentNullException(nameof(zapfung));
            if (wochentagJan1 < 0 || wochentagJan1 >= WOCHENTAGE)
                throw new ArgumentOutOfRangeException(nameof(wochentagJan1), wochentagJan1,
                                                      "Der Wochentag des 1. Januar liegt zwischen 0 (Montag) und 6 (Sonntag).");

            IReadOnlyList<double> z = zapfung.StundenKwh;
            IReadOnlyList<double> k = mitZirkulation ? zirkulation?.StundenKwh : null;

            // 1. Die Stundenreihe des Satzes — eine Bilanzreihe prüft Länge und Endlichkeit.
            var stunden = new double[STUNDEN_JAHR];
            for (int h = 0; h < STUNDEN_JAHR; h++)
                stunden[h] = k == null ? z[h] : z[h] + k[h];
            var reihe = new Bilanzreihe(stunden);

            // 2. Monate und Jahr; die Jahressumme ist die Summe der Monate (bitgleich mit der Reihe bis auf die Rundung).
            var monateKwh = new double[MONATE];
            var monateMwh = new double[MONATE];
            double jahrKwh = 0.0;
            for (int m = 0; m < MONATE; m++)
            {
                monateKwh[m] = reihe.MonatssummenKwh[m];
                jahrKwh += monateKwh[m];
            }
            var monateProzent = new double[MONATE];
            for (int m = 0; m < MONATE; m++)
            {
                monateMwh[m] = monateKwh[m] / KWH_JE_MWH;
                monateProzent[m] = jahrKwh > 0.0 ? monateKwh[m] / jahrKwh * 100.0 : 0.0;
            }

            // 3. Die mittlere Woche im Wochentagsraster der Klimaregion.
            var summe = new double[WOCHENSTUNDEN];
            var anzahl = new int[WOCHENSTUNDEN];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                int wochentag = (wochentagJan1 + d) % WOCHENTAGE;
                for (int s = 0; s < STUNDEN_TAG; s++)
                {
                    int i = wochentag * STUNDEN_TAG + s;
                    summe[i] += stunden[d * STUNDEN_TAG + s];
                    anzahl[i]++;
                }
            }
            var mittel = new double[WOCHENSTUNDEN];
            double wochensumme = 0.0;
            double spitze = 0.0;
            int spitzeIndex = 0;
            for (int i = 0; i < WOCHENSTUNDEN; i++)
            {
                mittel[i] = summe[i] / anzahl[i];
                wochensumme += mittel[i];
                if (mittel[i] > spitze) { spitze = mittel[i]; spitzeIndex = i; }
            }
            var anteile = new double[WOCHENSTUNDEN];
            for (int i = 0; i < WOCHENSTUNDEN; i++)
                anteile[i] = wochensumme > 0.0 ? mittel[i] / wochensumme * 100.0 : 0.0;

            return new ZapfprofilProfilsatz
            {
                MitZirkulation = mitZirkulation,
                WochentagJan1 = wochentagJan1,
                JahressummeKwh = jahrKwh,
                JahressummeMwh = jahrKwh / KWH_JE_MWH,
                MonatssummenKwh = Array.AsReadOnly(monateKwh),
                MonatssummenMwh = Array.AsReadOnly(monateMwh),
                MonatsanteileProzent = Array.AsReadOnly(monateProzent),
                WochenmittelKwh = Array.AsReadOnly(mittel),
                WochenanteileProzent = Array.AsReadOnly(anteile),
                SpitzenstundeWocheKw = spitze,
                SpitzenstundeWocheIndex = spitzeIndex
            };
        }
    }
}
