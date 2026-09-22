using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Vorlauf des 2-K-Lösers mit Abbruchkriterium (Stufe G0 der Gebäudesimulation;
    /// Konzept 4.6, Rechenschritte 7.2 und 10.4, Festlegung F-P5).
    ///
    /// <para><b>Was er tut.</b> Er setzt das Modell auf den Startwert zurück und rechnet eine
    /// Randfolge — in der Regel eine Woche, <see cref="WOCHE_H"/> Stunden — so oft hintereinander,
    /// bis sich die beiden Massentemperaturen über einen ganzen Durchlauf um weniger als die
    /// Schwelle ändern. Die Ergebnisse der Stunden werden verworfen; übrig bleibt der
    /// eingeschwungene Zustand im Modell, von dem aus der Aufrufer weiterrechnet. Erreicht der
    /// Vorlauf die Höchstzahl an Durchläufen, ohne die Schwelle zu unterschreiten, ist das ein
    /// benannter Fehler (<see cref="GebaeudeModellFehler.VorlaufNichtKonvergiert"/>) und kein
    /// stiller Weiterlauf.</para>
    ///
    /// <para><b>Das Maß.</b> Änderung eines Durchlaufs = größerer der beiden Beträge
    /// |θ_m,AW(Ende) − θ_m,AW(Anfang)| und |θ_m,IW(Ende) − θ_m,IW(Anfang)| — der
    /// Zustandsunterschied zweier aufeinanderfolgender Vorlaufwochen. Für eine periodische
    /// Randfolge ist er null genau im periodisch eingeschwungenen Zustand.</para>
    ///
    /// <para><b>Wo er gilt.</b> Jeder Aufrufer, der einen eingeschwungenen Anfangszustand
    /// braucht. Die Bandprüfung der Normfälle folgt dagegen der Messkette der Richtlinie und
    /// startet ohne Vorlauf aus dem vorgegebenen Startwert — sie prüft gerade den
    /// Einschwingvorgang; der Vorlauf wird an ihnen nur berichtend gemessen. Für
    /// Projektläufe bleibt nach Konzept 4.6 der feste Vorlauf von 30 Tagen (Stufe G1); die
    /// Länge des Vorlaufs im Mehrzonenfall ist offen (Register M6).</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, ohne Uhr, einfädig, durchgehend <c>double</c>;
    /// bei gleichem Eingang byte-gleich.</para>
    /// </summary>
    internal static class Vorlauf2K
    {
        /// <summary>Länge einer Vorlaufwoche [h] — die übliche Randfolge eines Durchlaufs.</summary>
        internal const int WOCHE_H = 168;

        /// <summary>
        /// Schwelle der Änderung eines Durchlaufs [K]: 0,01 K. Festlegung des Konzepts (4.6,
        /// Rechenschritte 7.2, F-P5), keine Zahl einer Richtlinie. Begründung: Sie liegt eine
        /// Größenordnung unter dem Prüfband nach E10 und unter der Darstellungsgenauigkeit
        /// einer Raumtemperatur (0,1 K), sodass der Rest des Einschwingens weder eine Prüfung
        /// noch eine Anzeige bewegt; zugleich liegt sie weit über dem Zahlenrauschen des
        /// Lösers, sodass das Kriterium nicht am letzten Bit entscheidet.
        /// </summary>
        internal const double SCHWELLE_K = 0.01;

        /// <summary>
        /// Höchstzahl der Durchläufe: zwölf (Wochen). Festlegung des Konzepts (4.6,
        /// Rechenschritte 7.2, F-P5), keine Zahl einer Richtlinie. Begründung: Bei der langsamen
        /// Zeitkonstante τ schrumpft die Änderung je Woche etwa um q = exp(−168 h/τ); nötig sind
        /// also rund 1 + ln(a₁/Schwelle)/ln(1/q) Durchläufe bei einer Änderung a₁ im ersten.
        /// Zwanzig Durchläufe decken damit die Referenzgebäude (τ bis rund 25 h) nach zwei bis
        /// drei Wochen und, bei 14 K Anfangsänderung, Zeitkonstanten bis rund 440 h (schwere
        /// Bauweise; die frei laufenden Normfälle brauchen 13); wer danach
        /// die Schwelle noch nicht unterschreitet, hat eine Masse oder einen Startwert, der
        /// benannt auffallen soll, oder eine Randfolge ohne periodischen Zustand. Die
        /// Rechenzeit bleibt unter einem Viertel eines Jahreslaufs.
        /// </summary>
        internal const int HOECHSTZAHL_DURCHLAEUFE = 20;

        /// <summary>
        /// Setzt <paramref name="modell"/> auf <paramref name="thetaStart"/> zurück und rechnet
        /// <paramref name="randfolge"/> wiederholt, bis die Änderung eines Durchlaufs unter
        /// <paramref name="schwelleK"/> liegt. Danach trägt das Modell den eingeschwungenen
        /// Zustand am Ende des letzten Durchlaufs.
        /// </summary>
        /// <exception cref="GebaeudeModellException">
        /// <see cref="GebaeudeModellFehler.VorlaufNichtKonvergiert"/>, wenn nach
        /// <paramref name="hoechstzahl"/> Durchläufen die Schwelle nicht unterschritten ist — das
        /// Modell trägt dann den Zustand am Ende des letzten Durchlaufs und darf nicht als
        /// eingeschwungen weiterrechnen; <see cref="GebaeudeModellFehler.RandUngueltig"/> bei
        /// leerer Randfolge oder ungültigem Startwert; jeder Fehler des Stundenschritts wird
        /// durchgereicht.
        /// </exception>
        internal static Vorlaufergebnis Einschwingen(
            Zonenmodell2K modell,
            double thetaStart,
            ReadOnlySpan<Stundenrand> randfolge,
            double schwelleK = SCHWELLE_K,
            int hoechstzahl = HOECHSTZAHL_DURCHLAEUFE)
        {
            if (modell == null) throw new ArgumentNullException(nameof(modell));
            if (!(schwelleK > 0.0) || double.IsInfinity(schwelleK)) throw new ArgumentOutOfRangeException(nameof(schwelleK));
            if (hoechstzahl < 1) throw new ArgumentOutOfRangeException(nameof(hoechstzahl));
            if (randfolge.Length == 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.RandUngueltig,
                    "Der Vorlauf braucht mindestens eine Stunde Randfolge.");

            modell.Zuruecksetzen(thetaStart);
            var aenderungen = new List<double>(hoechstzahl);
            for (int d = 0; d < hoechstzahl; d++)
            {
                double aw0 = modell.ThetaMAw;
                double iw0 = modell.ThetaMIw;
                for (int h = 0; h < randfolge.Length; h++) modell.Schritt(in randfolge[h]);
                double aenderung = Math.Max(Math.Abs(modell.ThetaMAw - aw0), Math.Abs(modell.ThetaMIw - iw0));
                aenderungen.Add(aenderung);
                if (aenderung < schwelleK)
                    return new Vorlaufergebnis(aenderungen.ToArray(), modell.ThetaMAw, modell.ThetaMIw);
            }

            double letzte = aenderungen[aenderungen.Count - 1];
            throw new GebaeudeModellException(GebaeudeModellFehler.VorlaufNichtKonvergiert,
                string.Format(CultureInfo.InvariantCulture,
                    "Der Vorlauf ist nach {0} Durchläufen zu je {1} Stunden nicht eingeschwungen: " +
                    "Änderung des letzten Durchlaufs {2:G4} K, Schwelle {3:G4} K.",
                    hoechstzahl, randfolge.Length, letzte, schwelleK));
        }
    }

    /// <summary>
    /// Das Ergebnis eines konvergierten Vorlaufs: die Änderung je Durchlauf und der
    /// eingeschwungene Zustand.
    /// </summary>
    internal sealed class Vorlaufergebnis
    {
        internal Vorlaufergebnis(double[] aenderungenK, double thetaMAw, double thetaMIw)
        {
            AenderungenK = aenderungenK ?? throw new ArgumentNullException(nameof(aenderungenK));
            ThetaMAw = thetaMAw;
            ThetaMIw = thetaMIw;
        }

        /// <summary>Zahl der gerechneten Durchläufe, ≥ 1.</summary>
        internal int Durchlaeufe => AenderungenK.Count;

        /// <summary>Änderung der Massentemperaturen je Durchlauf [K], in Rechenreihenfolge.</summary>
        internal IReadOnlyList<double> AenderungenK { get; }

        /// <summary>Temperatur des Außenbauteil-Massenknotens nach dem Vorlauf [°C].</summary>
        internal double ThetaMAw { get; }

        /// <summary>Temperatur des Innenbauteil-Massenknotens nach dem Vorlauf [°C].</summary>
        internal double ThetaMIw { get; }

        /// <summary>
        /// Geschätzter Restabstand zum periodischen Zustand [K], aus dem Verhältnis q der
        /// letzten beiden Änderungen als geometrischer Rest a·q/(1 − q). Nur ein Hinweis, kein
        /// Kriterium; NaN, wenn weniger als zwei Durchläufe vorliegen oder q nicht in (0, 1) liegt.
        /// </summary>
        internal double RestabschaetzungK
        {
            get
            {
                int n = AenderungenK.Count;
                if (n < 2 || !(AenderungenK[n - 2] > 0.0)) return double.NaN;
                double q = AenderungenK[n - 1] / AenderungenK[n - 2];
                if (!(q > 0.0) || !(q < 1.0)) return double.NaN;
                return AenderungenK[n - 1] * q / (1.0 - q);
            }
        }
    }
}
