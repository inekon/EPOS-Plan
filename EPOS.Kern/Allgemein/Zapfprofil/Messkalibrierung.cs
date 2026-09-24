using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der vorgeschlagene mittlere Tagesgang EINES Tagtyps (Stufe Z5): die 24 Stundenanteile
    /// (Σ 1) und die Zahl der vollständigen Messtage, die in das Mittel eingegangen sind.
    /// </summary>
    internal sealed record Tagesgangvorschlag(ZapfTagtyp Tagtyp, int Tage, IReadOnlyList<double> Anteile);

    /// <summary>
    /// <b>Der Kalibriervorschlag für eine Nichtwohn-Zone</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 1, Punkt 4 (b)):
    /// Tagesbedarf, Wochenfaktoren (Σ 1, Montag = 0) und die Tagesgänge je Tagtyp, alle aus der
    /// Messung.
    ///
    /// <para><b>Ein Vorschlag, kein Schreibvorgang.</b> Er wird nicht gespeichert; erst der
    /// Anwender übernimmt ihn in seine <b>Anwenderkopie</b> der Nutzungsart (Status <c>EIGEN</c>) —
    /// ein Auslieferungskatalog bleibt unberührt (Konzept 3.1, K7). Das Ergebnis trägt hier
    /// absolute Werte, weil es Parameter der eigenen Kopie sind und nie einen Bericht verlässt; der
    /// Vergleichsbericht selbst führt allein Verhältniszahlen (K5).</para>
    ///
    /// <para><b>Kleinste Quadrate.</b> Gesucht sind die Stundenanteile <c>a_h</c>, die
    /// <c>Σ_Tage Σ_h (x_{t,h} − a_h · Q_t)²</c> unter <c>Σ a_h = 1</c> kleinstmöglich machen. Bei
    /// gleichen Tagesmengen ist die Lösung genau das <b>Mittel der beobachteten Stundenanteile</b> —
    /// deshalb rechnet der Vorschlag Mittelwerte, und das ist die Lösung der kleinsten Quadrate, kein
    /// Ersatz dafür. Dasselbe gilt für die Wochenfaktoren über die Tagesmengen.</para>
    /// </summary>
    internal sealed record Nichtwohnvorschlag(double TagesbedarfKwh, double TagesbedarfJeEinheitKwh,
                                              IReadOnlyList<double> Wochenfaktoren,
                                              IReadOnlyList<Tagesgangvorschlag> Tagesgaenge,
                                              int VolleTage, double Bezugsmenge);

    /// <summary>
    /// <b>Die Kalibrierung gegen eine gemessene Reihe</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.1, 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 1, Punkt 4):
    ///
    /// <list type="number">
    /// <item><b>Energie</b> — der Jahreswert der Messreihe kalibriert das Mengengerüst GENAU wie
    /// ein von Hand gepflegter Jahresmesswert (4.1): Die Reihe wird zu einem
    /// <see cref="Messwert"/>, und <see cref="Mengengeruest.Kalibrieren"/> rechnet. Nach der
    /// Kalibrierung ist die Jahresenergie <b>exakt</b> der (Netto-)Messwert — ein zweiter
    /// Rechenweg entsteht nicht.</item>
    /// <item><b>Nichtwohn-Parameter</b> — Tagesbedarf, Wochenfaktoren und Tagesgänge einer Zone als
    /// <see cref="Nichtwohnvorschlag"/>, aus den vollständigen Tagen der Messung; nichts wird
    /// gespeichert.</item>
    /// </list>
    ///
    /// <para><b>Rein:</b> keine Datenbank, keine Umgebung, kein Text; jede Ablehnung ist ein
    /// <see cref="ZapfSatz"/> (N11 (k)).</para>
    /// </summary>
    internal static class Messkalibrierung
    {
        /// <summary>Vorgabe der kürzesten Reihe für einen Vorschlag [d] (Parameter <see cref="ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE"/>).</summary>
        internal const int MINDESTTAGE_VORGABE = 30;

        /// <summary>Wie weit die Reihe von 365 Tagen abweichen darf, ohne hochgerechnet zu werden [d].</summary>
        internal const double JAHRESRAND_TAGE = 1.0;

        /// <summary>
        /// Die kürzeste Reihe für einen Vorschlag [d] aus dem Parametersatz; ohne Satz oder ohne
        /// Schlüssel <see cref="MINDESTTAGE_VORGABE"/>. Ein Wert unter 1 wird auf 1 gehoben.
        /// </summary>
        internal static int Mindesttage(Parametersatz p)
        {
            if (p == null || !p.Enthaelt(ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE)) return MINDESTTAGE_VORGABE;
            double w = p.Wert(ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE);
            int n = (int)Math.Round(w);
            return n < 1 ? 1 : n;
        }

        // =================================================================================
        //  (a) Energie — der Jahreswert der Reihe kalibriert das Mengengerüst
        // =================================================================================

        /// <summary>
        /// <b>Der Jahresmesswert AUS der Reihe</b> (4.1): die Energie der Reihe, auf ein Jahr
        /// bezogen. Deckt die Reihe ein Jahr (365 ± <see cref="JAHRESRAND_TAGE"/> Tage), gilt ihre
        /// Energie unverändert; ist sie kürzer, wird sie auf 365 Tage <b>hochgerechnet</b> und das
        /// benannt — nie still. Eine Reihe unter <paramref name="mindesttage"/> Tagen wird benannt
        /// abgelehnt: Aus zwei Wochen einen Jahreswert zu machen hieße raten.
        /// </summary>
        /// <param name="reihe">Die gemessene Reihe.</param>
        /// <param name="spreizungK">θ_Zapf − θ̄_KW [K] — nur für eine Volumenreihe.</param>
        /// <param name="grenze">Die Bilanzgrenze des Messwerts (4.1).</param>
        /// <param name="speicherverlustKwhJeJahr">Der Speicherverlust [kWh/a] — nur bei Grenze 3.</param>
        /// <param name="mindesttage">Die kürzeste zugelassene Reihe [d].</param>
        /// <param name="fehler">Die benannte Ablehnung; <c>null</c> = der Messwert steht.</param>
        /// <param name="hinweise">Nimmt die Hochrechnung auf; darf <c>null</c> sein.</param>
        internal static Messwert Jahresmesswert(Messreihe reihe, double spreizungK, ZapfBilanzgrenze grenze,
                                                double? speicherverlustKwhJeJahr, int mindesttage,
                                                out ZapfSatz fehler, ICollection<ZapfSatz> hinweise = null)
        {
            fehler = null;
            if (reihe == null)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE");
                return null;
            }
            if (reihe.Tage < mindesttage)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_REIHE_ZU_KURZ", reihe.Tage, mindesttage);
                return null;
            }

            double kwh;
            try
            {
                kwh = reihe.EnergieKwh(spreizungK);
            }
            catch (ZapfprofilEingabeException ex)
            {
                fehler = ex.Satz ?? ZapfSatz.Neu("MESSVERGLEICH_SPREIZUNG_FEHLT", spreizungK);
                return null;
            }
            if (!(kwh > 0.0))
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MENGE");
                return null;
            }

            double jahrKwh = kwh;
            if (Math.Abs(reihe.Tage - Zapfkalender.TAGE) > JAHRESRAND_TAGE)
            {
                jahrKwh = kwh * Zapfkalender.TAGE / reihe.Tage;
                hinweise?.Add(ZapfSatz.Neu("MESSKALIBRIERUNG_HOCHGERECHNET", reihe.Tage, Zapfkalender.TAGE));
            }

            string zeitraum = reihe.Beginn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " – "
                              + reihe.Ende.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new Messwert(jahrKwh, grenze, speicherverlustKwhJeJahr, reihe.Quelle, zeitraum);
        }

        /// <summary>
        /// <b>Die Kalibrierung gegen die Reihe</b>: <see cref="Jahresmesswert"/> und dann
        /// <see cref="Mengengeruest.Kalibrieren"/> — DERSELBE Rechenweg wie beim von Hand gepflegten
        /// Jahresmesswert (4.1), kein zweiter. Nach der Kalibrierung ist die Jahresenergie exakt der
        /// Nettomesswert: bei Grenze 1 die Zapfung, bei Grenze 2 und 3 Zapfung plus Zirkulation.
        /// </summary>
        internal static Kalibrierergebnis Kalibrieren(Messreihe reihe, double spreizungK, ZapfBilanzgrenze grenze,
                                                      double? speicherverlustKwhJeJahr, double zapfungKwh,
                                                      double zirkulationKwh, int mindesttage, string zone,
                                                      out ZapfSatz fehler, ICollection<ZapfSatz> hinweise = null)
        {
            Messwert m = Jahresmesswert(reihe, spreizungK, grenze, speicherverlustKwhJeJahr, mindesttage,
                                        out fehler, hinweise);
            if (m == null) return null;
            try
            {
                return Mengengeruest.Kalibrieren(m, zapfungKwh, zirkulationKwh, zone ?? "");
            }
            catch (ZapfprofilEingabeException ex)
            {
                fehler = ex.Satz ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MENGE");
                return null;
            }
        }

        // =================================================================================
        //  (b) Die Nichtwohn-Parameter als Vorschlag
        // =================================================================================

        /// <summary>
        /// <b>Der Vorschlag für eine Nichtwohn-Zone</b> aus den vollständigen Tagen der Messung:
        ///
        /// <code>
        /// Q_d        = (1/T) · Σ_t Q_t                              Tagesbedarf [kWh/d]
        /// Q_d,Einheit= Q_d / n_Bezug                                je Einheit
        /// f_i        = m_i / Σ_j m_j       mit m_i = Mittel von Q_t über die Tage des Wochentags i
        ///              (Σ f = 1, Montag = 0; ein Wochentag ohne Messtag bekommt das Mittel der übrigen)
        /// a_{typ,h}  = (Σ_{t ∈ typ} x_{t,h}) / (Σ_{t ∈ typ} Q_t)    Stundenanteile je Tagtyp (Σ_h a = 1)
        /// </code>
        ///
        /// <para><b>Nur vollständige Tage</b> (24 vollständige Stunden): Ein angeschnittener Tag
        /// zöge den Tagesbedarf nach unten und verbog die Form. Eine Reihe ohne Stundenwerte
        /// (Tagesraster) oder unter <paramref name="mindesttage"/> vollständigen Tagen wird benannt
        /// abgelehnt.</para>
        ///
        /// <para><b>Deterministisch:</b> feste Summationsfolge nach Datum, nur Arithmetik, kein
        /// Zufall — derselbe Eingang ergibt dieselben Bits.</para>
        /// </summary>
        /// <param name="bezugsmenge">Die Bezugsmenge der Zone (Einheiten, Personen, m² …) &gt; 0.</param>
        internal static Nichtwohnvorschlag Nichtwohnparameter(Messreihe reihe, double spreizungK, double bezugsmenge,
                                                             int mindesttage, out ZapfSatz fehler,
                                                             ICollection<ZapfSatz> hinweise = null)
        {
            fehler = null;
            if (reihe == null)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE");
                return null;
            }
            if (!(bezugsmenge > 0.0))
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_BEZUGSMENGE");
                return null;
            }
            if (!reihe.StundenweiseTauglich)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_STUNDENWERTE", reihe.AufloesungMin);
                return null;
            }

            IReadOnlyList<Messstunde> stunden;
            try
            {
                stunden = reihe.Stundenwerte(spreizungK);
            }
            catch (ZapfprofilEingabeException ex)
            {
                fehler = ex.Satz ?? ZapfSatz.Neu("MESSVERGLEICH_SPREIZUNG_FEHLT", spreizungK);
                return null;
            }

            // Je Datum die 24 Stunden; nur Tage mit allen 24 vollständigen Stunden zählen.
            var gaenge = new Dictionary<DateTime, double[]>();
            var zahl = new Dictionary<DateTime, int>();
            foreach (Messstunde s in stunden)
            {
                DateTime tag = s.Beginn.Date;
                if (!gaenge.TryGetValue(tag, out double[] g)) gaenge[tag] = g = new double[Zapfkalender.STUNDEN_TAG];
                g[s.Beginn.Hour] += s.Kwh;
                zahl[tag] = (zahl.TryGetValue(tag, out int n) ? n : 0) + 1;
            }
            List<DateTime> volle = gaenge.Keys.Where(t => zahl[t] == Zapfkalender.STUNDEN_TAG)
                                              .OrderBy(t => t).ToList();
            if (volle.Count < mindesttage)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_REIHE_ZU_KURZ", (double)volle.Count, mindesttage);
                return null;
            }

            // --- Tagesbedarf und Wochenfaktoren -------------------------------------------
            double summeAlle = 0.0;
            var summeJeWochentag = new double[Zapfkalender.WOCHENTAGE];
            var tageJeWochentag = new int[Zapfkalender.WOCHENTAGE];
            foreach (DateTime t in volle)
            {
                double q = 0.0;
                foreach (double w in gaenge[t]) q += w;
                summeAlle += q;
                int wt = ((int)t.DayOfWeek + 6) % Zapfkalender.WOCHENTAGE;      // Montag = 0
                summeJeWochentag[wt] += q;
                tageJeWochentag[wt]++;
            }
            if (!(summeAlle > 0.0))
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MENGE");
                return null;
            }
            double tagesbedarf = summeAlle / volle.Count;

            // Mittel je Wochentag; ein Wochentag ohne Messtag bekommt das Mittel der uebrigen -
            // benannt, damit eine 0 nicht still einen Ruhetag erfindet.
            var mittel = new double[Zapfkalender.WOCHENTAGE];
            int belegt = 0;
            double summeMittel = 0.0;
            for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++)
                if (tageJeWochentag[i] > 0)
                {
                    mittel[i] = summeJeWochentag[i] / tageJeWochentag[i];
                    summeMittel += mittel[i];
                    belegt++;
                }
            if (belegt < Zapfkalender.WOCHENTAGE)
            {
                double ersatz = summeMittel / belegt;
                for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++)
                    if (tageJeWochentag[i] == 0) mittel[i] = ersatz;
                hinweise?.Add(ZapfSatz.Neu("MESSKALIBRIERUNG_WOCHENTAG_FEHLT",
                                           Zapfkalender.WOCHENTAGE - belegt, Zapfkalender.WOCHENTAGE));
            }
            double summeWoche = 0.0;
            foreach (double w in mittel) summeWoche += w;
            var faktoren = new double[Zapfkalender.WOCHENTAGE];
            for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++) faktoren[i] = mittel[i] / summeWoche;

            // --- Die Tagesgänge je Tagtyp -------------------------------------------------
            var liste = new List<Tagesgangvorschlag>(Messvergleich.TAGTYPEN.Count);
            foreach (ZapfTagtyp typ in Messvergleich.TAGTYPEN)
            {
                var summe = new double[Zapfkalender.STUNDEN_TAG];
                int tage = 0;
                double menge = 0.0;
                foreach (DateTime t in volle)
                {
                    if (Messvergleich.Tagtyp(t) != typ) continue;
                    tage++;
                    for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    {
                        summe[h] += gaenge[t][h];
                        menge += gaenge[t][h];
                    }
                }
                if (tage == 0 || !(menge > 0.0))
                {
                    hinweise?.Add(ZapfSatz.Neu("MESSKALIBRIERUNG_TAGTYP_FEHLT", Formvektor.Tagtyp(typ)));
                    continue;
                }
                var anteile = new double[Zapfkalender.STUNDEN_TAG];
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) anteile[h] = summe[h] / menge;
                liste.Add(new Tagesgangvorschlag(typ, tage, Array.AsReadOnly(anteile)));
            }
            if (liste.Count == 0)
            {
                fehler = ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_TAGESGANG");
                return null;
            }

            return new Nichtwohnvorschlag(tagesbedarf, tagesbedarf / bezugsmenge, Array.AsReadOnly(faktoren),
                                          liste, volle.Count, bezugsmenge);
        }
    }
}
