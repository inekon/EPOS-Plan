using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Plausibilitätsprüfung EINES Wechselrichter-Katalogsatzes</b> (Konzept
    /// Wechselrichter 6, Stufe S1.6, Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026)
    /// — Muster <see cref="PvModulPlausibilitaet"/>.
    ///
    /// <para><b>Was hier NICHT geprüft wird:</b> die acht Auslegungsprüfungen P1…P8
    /// des Konzepts 4.2. Die gehören zur STRANGZEILE (Modul gegen Gerät) und damit zur
    /// Stufe S2; hier steht allein, was ein Katalogsatz für sich beantworten kann.</para>
    ///
    /// <para><b>Ein fehlender Wert ist kein Fehler.</b> Fast jede Spalte des Katalogs
    /// bedeutet NULL „keine Prüfung" bzw. „keine Grenze" (Konzept 3.1); nur
    /// <c>P_AC_Nenn</c> ist Pflichtfeld — wie bei der Photovoltaik allein die
    /// Nennleistung. Eine Prüfung, die auf unvollständigen Datenblättern rot leuchtet,
    /// wird weggeklickt statt gelesen (Konzept 4.2).</para>
    ///
    /// <para><b>Fehler sperren, Warnungen fragen zurück</b> — dieselbe Zweiteilung wie
    /// beim Modulimport: <see cref="Befund.Fehler"/> verhindert das Schreiben,
    /// <see cref="Befund.Warnungen"/> steht als Hinweis daneben.</para>
    /// </summary>
    public static class WechselrichterPlausibilitaet
    {
        /// <summary>Kleinste noch plausible AC-Nennleistung [kW] — darunter ist die Einheit falsch (W statt kW).</summary>
        public const double P_AC_MIN = 0.05;

        /// <summary>Größte noch plausible AC-Nennleistung [kW]; darüber liegt meist eine W-Angabe vor.</summary>
        public const double P_AC_MAX = 10000.0;

        /// <summary>Kleinster noch plausibler Wirkungsgrad einer Stützstelle (0…1).</summary>
        public const double ETA_MIN = 0.30;

        /// <summary>Größtes zulässiges DC/AC-Verhältnis eines Geräts (<c>P_DC_Max / P_AC_Nenn</c>).</summary>
        public const double DCAC_MAX = 3.0;

        /// <summary>
        /// <b>Ab wann ein Abfall im Teillastast gemeldet wird</b> — 0,01, also
        /// <b>ein Prozentpunkt</b> Wirkungsgrad zwischen zwei benachbarten Stützstellen
        /// (Anwenderentscheid <b>W6‑O‑8</b> vom 06.09.2026: „Empfehlung").
        ///
        /// <para><b>Warum es die Schwelle gibt.</b> Ohne sie meldete die Regel jeden noch
        /// so kleinen Rückgang — und traf damit 303 der 2 343 Geräte der
        /// Auslieferungsliste, 13 % des Bestands. Es ist kein Datenfehler: Der Import
        /// rechnet die Stützstellen aus den Sandia-Koeffizienten (Konzept 3.3.3), und
        /// die Modellparabel hat bei guten Geräten ihren Scheitel zwischen 30 und 50 %.
        /// Eine Warnung, die jedes zweite gute Gerät zurückfragt, wird weggeklickt statt
        /// gelesen.</para>
        ///
        /// <para><b>Woher der Wert kommt.</b> Gemessen am 06.09.2026 über die volle Liste
        /// (2 343 Geräte, alle sechs Stützstellen belegt), Abfall <c>η_i − η_{i+1}</c> je
        /// Intervall in Prozentpunkten:</para>
        /// <code>
        /// Intervall   Abfälle  > 0,5 PP  > 1,0 PP  > 2,0 PP   größter
        ///  5 → 10 %         0         0         0         0   —
        /// 10 → 20 %         0         0         0         0   —
        /// 20 → 30 %         2         0         0         0   0,372 PP
        /// 30 → 50 %       303         1         1         1   2,423 PP
        /// 50 → 100 %     2019       691       105        20   9,456 PP   (nicht geprüft)
        /// </code>
        /// <para>Je Gerät gerechnet: 302 der 303 Fälle liegen unter <b>0,4</b>
        /// Prozentpunkten (246 sogar unter 0,1), und zwischen 0,4 und 2,4 Prozentpunkten
        /// ist über den ganzen Bestand <b>keine einzige</b> Kennlinie. Die Schwelle liegt
        /// also nicht mitten in einer Verteilung, sondern in einer LÜCKE — jeder Wert
        /// zwischen 0,4 und 2,4 ergibt dasselbe Bild; 1,0 ist die runde Zahl darin. Übrig
        /// bleibt genau ein Gerät (OutBack Power GS8048A, 2,423 PP), und genau dafür ist
        /// die Regel da.</para>
        ///
        /// <para><b>Und ein Tippfehler bleibt sichtbar:</b> 0,79 statt 0,97 an einer
        /// Stützstelle sind 18 Prozentpunkte — das Achtzehnfache der Schwelle.</para>
        ///
        /// <para>Nachgemessen wird über <c>EPOS.Kern.Tests/CecWechselrichterAuslieferungTests</c>;
        /// die Fälle zur Schwelle stehen in <c>WechselrichterPlausibilitaetTests</c>.</para>
        /// </summary>
        public const double TEILLAST_ABFALL_SCHWELLE = 0.01;

        /// <summary>
        /// Ergebnis der Prüfung. <see cref="Fehler"/> sperrt das Schreiben,
        /// <see cref="Warnungen"/> ist ein Hinweis.
        /// </summary>
        public sealed class Befund
        {
            /// <summary>Harte Verstöße — der Satz darf so nicht geschrieben werden.</summary>
            public List<string> Fehler = new List<string>();

            /// <summary>Auffälligkeiten, die der Anwender bestätigen kann.</summary>
            public List<string> Warnungen = new List<string>();

            /// <summary>Wahr, solange kein harter Verstoß vorliegt.</summary>
            public bool Ok => Fehler.Count == 0;
        }

        /// <summary>Prüft einen Katalogsatz.</summary>
        /// <param name="m">Der Satz; <c>null</c> ist ein Fehler.</param>
        public static Befund Pruefe(WechselrichterModel m)
        {
            var b = new Befund();
            if (m == null)
            {
                b.Fehler.Add(MyResource.Resource.WRK_PLAUSI_KEIN_SATZ);
                return b;
            }

            PruefeLeistungen(m, b);
            PruefeSpannungen(m, b);
            PruefeMppt(m, b);
            PruefeKennlinie(m, b);
            return b;
        }

        // =================================================================
        //  Leistungen
        // =================================================================

        private static void PruefeLeistungen(WechselrichterModel m, Befund b)
        {
            // P_AC_Nenn ist das EINE Pflichtfeld (Konzept 6).
            if (!m.m_P_AC_Nenn.HasValue || m.m_P_AC_Nenn.Value <= 0.0)
            {
                b.Fehler.Add(MyResource.Resource.WRK_PLAUSI_P_AC_FEHLT);
                return;
            }

            double pac = m.m_P_AC_Nenn.Value;
            if (pac < P_AC_MIN || pac > P_AC_MAX)
                b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_P_AC_BEREICH,
                                              Z(pac), Z(P_AC_MIN), Z(P_AC_MAX)));

            // Paco <= Pdco: Ein Wechselrichter gibt nicht mehr ab, als er aufnimmt.
            // Sandia_Pdco steht in WATT, P_AC_Nenn in kW (Konzept 5.1).
            if (m.m_Sandia_Pdco.HasValue && m.m_Sandia_Pdco.Value > 0.0
                && pac * 1000.0 > m.m_Sandia_Pdco.Value)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_PACO_UEBER_PDCO,
                                           Z(pac * 1000.0), Z(m.m_Sandia_Pdco.Value)));

            if (m.m_S_AC_Max.HasValue && m.m_S_AC_Max.Value > 0.0 && m.m_S_AC_Max.Value < pac)
                b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_S_AC_KLEINER,
                                              Z(m.m_S_AC_Max.Value), Z(pac)));

            if (m.m_P_DC_Max.HasValue && m.m_P_DC_Max.Value > 0.0)
            {
                if (m.m_P_DC_Max.Value < pac)
                    b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_P_DC_KLEINER,
                                                  Z(m.m_P_DC_Max.Value), Z(pac)));
                else if (m.m_P_DC_Max.Value / pac > DCAC_MAX)
                    b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_DCAC_GROSS,
                                                  Z(m.m_P_DC_Max.Value / pac), Z(DCAC_MAX)));
            }
        }

        // =================================================================
        //  Spannungen
        // =================================================================

        private static void PruefeSpannungen(WechselrichterModel m, Befund b)
        {
            foreach (var paar in new[]
            {
                (Wert: m.m_U_Mpp_Min, Name: WechselrichterSchema.SPALTE_U_MPP_MIN),
                (Wert: m.m_U_Mpp_Max, Name: WechselrichterSchema.SPALTE_U_MPP_MAX),
                (Wert: m.m_U_Dc_Max, Name: WechselrichterSchema.SPALTE_U_DC_MAX),
                (Wert: m.m_U_Start, Name: WechselrichterSchema.SPALTE_U_START)
            })
            {
                if (paar.Wert.HasValue && paar.Wert.Value < 0.0)
                    b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_SPANNUNG_NEGATIV,
                                               paar.Name, Z(paar.Wert.Value)));
            }

            // U_Mpp_Min < U_Mpp_Max <= U_Dc_Max (Konzept 6).
            if (m.m_U_Mpp_Min.HasValue && m.m_U_Mpp_Max.HasValue
                && m.m_U_Mpp_Min.Value >= m.m_U_Mpp_Max.Value)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_MPP_FENSTER,
                                           Z(m.m_U_Mpp_Min.Value), Z(m.m_U_Mpp_Max.Value)));

            if (m.m_U_Mpp_Max.HasValue && m.m_U_Dc_Max.HasValue
                && m.m_U_Dc_Max.Value > 0.0 && m.m_U_Mpp_Max.Value > m.m_U_Dc_Max.Value)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_MPP_UEBER_UDC,
                                           Z(m.m_U_Mpp_Max.Value), Z(m.m_U_Dc_Max.Value)));

            if (m.m_U_Start.HasValue && m.m_U_Mpp_Max.HasValue
                && m.m_U_Start.Value > m.m_U_Mpp_Max.Value)
                b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_START_UEBER_MPP,
                                              Z(m.m_U_Start.Value), Z(m.m_U_Mpp_Max.Value)));

            if (m.m_I_Dc_Max.HasValue && m.m_I_Dc_Max.Value < 0.0)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_I_DC_NEGATIV,
                                           Z(m.m_I_Dc_Max.Value)));
        }

        // =================================================================
        //  MPPT
        // =================================================================

        private static void PruefeMppt(WechselrichterModel m, Befund b)
        {
            if (m.m_Anzahl_Mppt.HasValue && m.m_Anzahl_Mppt.Value < 1)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_MPPT_KLEINER_EINS,
                                           m.m_Anzahl_Mppt.Value.ToString(CultureInfo.InvariantCulture)));

            if (m.m_Straenge_Je_Mppt.HasValue && m.m_Straenge_Je_Mppt.Value < 1)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_STRAENGE_KLEINER_EINS,
                                           m.m_Straenge_Je_Mppt.Value.ToString(CultureInfo.InvariantCulture)));
        }

        // =================================================================
        //  Kennlinie
        // =================================================================

        private static void PruefeKennlinie(WechselrichterModel m, Befund b)
        {
            var etas = new double?[]
            {
                m.m_Eta05, m.m_Eta10, m.m_Eta20, m.m_Eta30, m.m_Eta50, m.m_Eta100
            };
            var namen = new[]
            {
                WechselrichterSchema.SPALTE_ETA05, WechselrichterSchema.SPALTE_ETA10,
                WechselrichterSchema.SPALTE_ETA20, WechselrichterSchema.SPALTE_ETA30,
                WechselrichterSchema.SPALTE_ETA50, WechselrichterSchema.SPALTE_ETA100
            };

            for (int i = 0; i < etas.Length; i++)
            {
                if (!etas[i].HasValue) continue;

                double e = etas[i].Value;
                if (e <= 0.0 || e > 1.0)
                    b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_ETA_BEREICH, namen[i], Z(e)));
                else if (e < ETA_MIN)
                    b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_ETA_KLEIN,
                                                  namen[i], Z(e), Z(ETA_MIN)));
            }

            // MONOTONIE — aber nur bis 50 %: Die Kennlinie eines Wechselrichters steigt
            // steil bis in den Bereich 30…50 % und faellt danach LEICHT wieder ab
            // (Anhang A des Konzepts: 0,975 bei 50 %, 0,970 bei 100 %). Wer ueber die
            // ganze Kurve Monotonie forderte, meldete jedes echte Datenblatt.
            //
            // …und erst ab TEILLAST_ABFALL_SCHWELLE (W6-O-8): Ein Rueckgang von
            // Zehntelprozentpunkten ist die Modellparabel des Sandia-Umwegs, kein
            // Datenfehler. Gemeldet wird, was DARUEBER liegt — mit der Zahl im Satz,
            // damit der Anwender die Groessenordnung sieht, statt nur zwei Werte.
            // Der Grenzfall GENAU auf der Schwelle bleibt still: gemeldet wird
            // ">", nicht ">=".
            for (int i = 1; i <= 4; i++)
            {
                if (!etas[i - 1].HasValue || !etas[i].HasValue) continue;

                double abfall = etas[i - 1].Value - etas[i].Value;
                if (abfall <= TEILLAST_ABFALL_SCHWELLE + 1e-9) continue;

                b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_ETA_MONOTON,
                                              namen[i - 1], Z(etas[i - 1].Value),
                                              namen[i], Z(etas[i].Value),
                                              Z(abfall * 100.0)));
            }

            PruefeAusweis(m, b, MyResource.Resource.WRK_PLAUSI_ETA_EURO_BEREICH,
                          WechselrichterSchema.SPALTE_ETA_EURO, m.m_Eta_Euro);
            PruefeAusweis(m, b, MyResource.Resource.WRK_PLAUSI_ETA_EURO_BEREICH,
                          WechselrichterSchema.SPALTE_ETA_MAX, m.m_Eta_Max);

            // Der Euro-Wirkungsgrad gegen die Stuetzstellen: Er ist ein AUSWEIS und
            // wird nicht erzwungen - aber eine Abweichung ueber einen Prozentpunkt
            // heisst, dass Kennlinie und Ausweis nicht zum selben Geraet gehoeren.
            double? gerechnet = WechselrichterKennlinie.EuroWirkungsgrad(etas);
            if (gerechnet.HasValue && m.m_Eta_Euro.HasValue
                && Math.Abs(gerechnet.Value - m.m_Eta_Euro.Value) > 0.01)
                b.Warnungen.Add(string.Format(MyResource.Resource.WRK_PLAUSI_ETA_EURO_ABWEICHUNG,
                                              Z(m.m_Eta_Euro.Value), Z(gerechnet.Value)));

            if (m.m_P_Standby.HasValue && m.m_P_Standby.Value < 0.0)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_STANDBY_NEGATIV,
                                           WechselrichterSchema.SPALTE_P_STANDBY, Z(m.m_P_Standby.Value)));

            if (m.m_P_Nacht.HasValue && m.m_P_Nacht.Value < 0.0)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_STANDBY_NEGATIV,
                                           WechselrichterSchema.SPALTE_P_NACHT, Z(m.m_P_Nacht.Value)));

            if (m.m_Kosten.HasValue && m.m_Kosten.Value < 0.0)
                b.Fehler.Add(string.Format(MyResource.Resource.WRK_PLAUSI_KOSTEN_NEGATIV,
                                           Z(m.m_Kosten.Value)));
        }

        private static void PruefeAusweis(WechselrichterModel m, Befund b, string vorlage,
                                          string name, double? wert)
        {
            if (!wert.HasValue) return;
            if (wert.Value > 0.0 && wert.Value <= 1.0) return;

            b.Fehler.Add(string.Format(vorlage, name, Z(wert.Value)));
        }

        // =================================================================
        //  Meldungstext
        // =================================================================

        /// <summary>
        /// Der Befund als EIN Text — Fehler zuerst, dann Hinweise. Bauart wörtlich
        /// <see cref="PvModulPlausibilitaet.Meldung"/>.
        /// </summary>
        public static string Meldung(Befund b)
        {
            if (b == null) return string.Empty;

            var zeilen = new List<string>();
            if (b.Fehler.Count > 0)
            {
                zeilen.Add(MyResource.Resource.WRK_PLAUSI_KOPF_FEHLER);
                foreach (string f in b.Fehler) zeilen.Add("  - " + f);
            }
            if (b.Warnungen.Count > 0)
            {
                if (zeilen.Count > 0) zeilen.Add(string.Empty);
                zeilen.Add(MyResource.Resource.WRK_PLAUSI_KOPF_HINWEIS);
                foreach (string w in b.Warnungen) zeilen.Add("  - " + w);
            }
            return string.Join(Environment.NewLine, zeilen);
        }

        /// <summary>
        /// Zahl für den Meldungstext — kulturinvariant, damit die Meldung
        /// reproduzierbar ist (wörtlich <c>PvModulPlausibilitaet.Z</c>).
        /// </summary>
        private static string Z(double wert)
        {
            return wert.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
