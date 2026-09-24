using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die gemessene Größe einer Messreihe (Schemaschritt T4 „Messreihen", Konzept 4.8). Die Zahlen
    /// sind die Werte der Spalte <c>Groesse</c> von <c>Tab_TwwMessreihe</c>
    /// (<see cref="TwwSchema.MESSGROESSE_ENERGIE"/> und Geschwister) — EINE Wahrheit für Ablage und
    /// Rechenweg.
    /// </summary>
    internal enum ZapfMessgroesse
    {
        /// <summary>Energie je Zeitschritt [kWh] — ein Wärmemengenzähler.</summary>
        Energie = 1,

        /// <summary>Volumen je Zeitschritt [m³] — ein Wasserzähler; die Energie folgt über Δθ.</summary>
        Volumen = 2,

        /// <summary>Mittlere Leistung im Zeitschritt [kW] — die Energie folgt über die Schrittlänge.</summary>
        Leistung = 3
    }

    /// <summary>Eine vollständige Stunde einer Messreihe: ihr Beginn und ihre Energie [kWh].</summary>
    internal sealed record Messstunde(DateTime Beginn, double Kwh);

    /// <summary>
    /// <b>Eine gemessene Reihe</b> (Umsetzungskonzept Zapfprofilgenerator 4.8, Kapitel 7 Zeile Z5;
    /// Schemaschritt T4 „Messreihen"): Kopf (Bezeichnung, Größe, Auflösung, Beginn, Quelle) und die
    /// Werte je Zeitschritt in der Einheit der <see cref="Groesse"/>. Sie kommt aus einer CSV-Datei
    /// des Anwenders (<see cref="Messreihenleser"/>) oder aus <c>Tab_TwwMessreihe</c>.
    ///
    /// <para><b>Der echte Kalender.</b> <see cref="Beginn"/> ist ein wirklicher Zeitpunkt: Die
    /// Messung kennt Schaltjahr, Wochentag und Sommerzeit, der Rechenkern rechnet 365 Tage ohne
    /// Schaltjahr. Die Reihe wird deshalb NIE in das 8760-Stunden-Raster geschoben; verglichen
    /// werden Monat, Wochentag und Tagesstunde (<see cref="Messvergleich"/>).</para>
    ///
    /// <para><b>Rein:</b> keine Datenbank, keine Umgebung, kein Text — Ablehnungen und Hinweise sind
    /// <see cref="ZapfSatz"/>e des Lesers.</para>
    /// </summary>
    internal sealed class Messreihe
    {
        /// <summary>Minuten je Stunde — die Umrechnung Leistung → Energie.</summary>
        internal const double MINUTEN_JE_STUNDE = 60.0;

        /// <summary>Minuten je Tag — die Länge der Reihe in Tagen.</summary>
        internal const int MINUTEN_JE_TAG = 1440;

        private readonly double[] _werte;
        private readonly double[] _mengeJeSchritt;

        /// <summary>
        /// Eine Reihe aus ihrem Kopf und ihren Werten. Die Werte werden kopiert; eine leere Reihe,
        /// eine Auflösung außerhalb 1 … 1440 und ein negativer Wert sind Programmierfehler — der
        /// Leser lehnt sie vorher benannt ab.
        /// </summary>
        internal Messreihe(string bezeichnung, ZapfMessgroesse groesse, int aufloesungMin, DateTime beginn,
                           IReadOnlyList<double> werte, string quelle, int luecken = 0, int schalttage = 0)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            if (werte.Count == 0) throw new ArgumentException("Eine Messreihe ohne Werte.", nameof(werte));
            if (aufloesungMin < 1 || aufloesungMin > MINUTEN_JE_TAG)
                throw new ArgumentOutOfRangeException(nameof(aufloesungMin));

            Bezeichnung = bezeichnung ?? "";
            Groesse = groesse;
            AufloesungMin = aufloesungMin;
            Beginn = beginn;
            Quelle = quelle ?? "";
            Luecken = luecken < 0 ? 0 : luecken;
            Schalttage = schalttage < 0 ? 0 : schalttage;

            _werte = new double[werte.Count];
            _mengeJeSchritt = new double[werte.Count];
            double stundenJeSchritt = aufloesungMin / MINUTEN_JE_STUNDE;
            double summe = 0.0, menge = 0.0, groesster = 0.0;
            for (int i = 0; i < werte.Count; i++)
            {
                double w = werte[i];
                if (double.IsNaN(w) || double.IsInfinity(w) || w < 0.0)
                    throw new ArgumentException("Ein Wert der Messreihe ist kein Betrag ≥ 0.", nameof(werte));
                _werte[i] = w;
                _mengeJeSchritt[i] = groesse == ZapfMessgroesse.Leistung ? w * stundenJeSchritt : w;
                summe += w;
                menge += _mengeJeSchritt[i];
                if (w > groesster) groesster = w;
            }
            SummeRoh = summe;
            Menge = menge;
            GroessterWert = groesster;
        }

        /// <summary>Die Bezeichnung der Reihe — der Schlüssel der Ablage je Projekt.</summary>
        internal string Bezeichnung { get; }

        /// <summary>Die gemessene Größe.</summary>
        internal ZapfMessgroesse Groesse { get; }

        /// <summary>Die Länge eines Zeitschritts [min].</summary>
        internal int AufloesungMin { get; }

        /// <summary>Der Zeitpunkt des ersten Zeitschritts (echter Kalender).</summary>
        internal DateTime Beginn { get; }

        /// <summary>Woher die Reihe kommt (Anwenderangabe oder Dateiname).</summary>
        internal string Quelle { get; }

        /// <summary>Wie viele Zeitschritte die Datei nicht führte und der Leser mit 0 gefüllt hat.</summary>
        internal int Luecken { get; }

        /// <summary>Wie viele 29. Februare die Reihe trägt (der Rechenkern rechnet 365 Tage).</summary>
        internal int Schalttage { get; }

        /// <summary>Die Werte je Zeitschritt in der Einheit der <see cref="Groesse"/>.</summary>
        internal IReadOnlyList<double> Werte => Array.AsReadOnly(_werte);

        /// <summary>Die Zahl der Zeitschritte.</summary>
        internal int Schritte => _werte.Length;

        /// <summary>Der Anteil der gefüllten Lücken an den Zeitschritten [-].</summary>
        internal double Lueckenanteil => (double)Luecken / Schritte;

        /// <summary>Die Länge der Reihe in Tagen [d] — auch gebrochen.</summary>
        internal double Tage => (double)Schritte * AufloesungMin / MINUTEN_JE_TAG;

        /// <summary>Die Summe der rohen Werte (bei <see cref="ZapfMessgroesse.Leistung"/> eine Leistungssumme ohne Sinn).</summary>
        internal double SummeRoh { get; }

        /// <summary>Der größte rohe Wert eines Zeitschritts.</summary>
        internal double GroessterWert { get; }

        /// <summary>
        /// Die Menge je Zeitschritt in der Einheit der Bilanz: kWh bei <see cref="ZapfMessgroesse.Energie"/>
        /// und <see cref="ZapfMessgroesse.Leistung"/> (dort <c>kW · Δt/60</c>), m³ bei
        /// <see cref="ZapfMessgroesse.Volumen"/>.
        /// </summary>
        internal IReadOnlyList<double> MengeJeSchritt => Array.AsReadOnly(_mengeJeSchritt);

        /// <summary>Die Summe von <see cref="MengeJeSchritt"/> — kWh bzw. m³ über die ganze Reihe.</summary>
        internal double Menge { get; }

        /// <summary>Trägt die Reihe ein Volumen (dann braucht die Energie eine Spreizung)?</summary>
        internal bool IstVolumen => Groesse == ZapfMessgroesse.Volumen;

        /// <summary>
        /// Lässt sich die Reihe auf ganze Stunden verdichten? Nur ein Raster, das eine Stunde ohne
        /// Rest teilt (1, 5, 10, 15, 60 min); eine Tagesreihe (1440) nicht.
        /// </summary>
        internal bool StundenweiseTauglich => AufloesungMin <= MINUTEN_JE_STUNDE
                                              && (int)MINUTEN_JE_STUNDE % AufloesungMin == 0;

        /// <summary>Der Zeitpunkt des Zeitschritts <paramref name="index"/> (0 = <see cref="Beginn"/>).</summary>
        internal DateTime Zeitpunkt(int index) => Beginn.AddMinutes((double)index * AufloesungMin);

        /// <summary>Das Ende der Reihe — der Zeitpunkt nach dem letzten Zeitschritt.</summary>
        internal DateTime Ende => Zeitpunkt(Schritte);

        /// <summary>
        /// <b>Die Energie je Zeitschritt [kWh].</b> Bei <see cref="ZapfMessgroesse.Volumen"/> über
        /// <c>Mengengeruest.EnergieKwh</c> mit der Spreizung <paramref name="spreizungK"/>
        /// (θ_Zapf − θ̄_KW, dieselbe Umrechnung wie beim Jahresmesswert in m³/a, 4.1); sonst ist
        /// <paramref name="spreizungK"/> ohne Wirkung. Eine Spreizung ≤ 0 wird benannt abgelehnt —
        /// dieselbe Ablehnung wie im Mengengerüst.
        /// </summary>
        internal IReadOnlyList<double> EnergieJeSchrittKwh(double spreizungK, string zone = "")
        {
            if (!IstVolumen) return MengeJeSchritt;
            var kwh = new double[Schritte];
            double faktor = Mengengeruest.EnergieKwh(Mengengeruest.LITER_JE_M3, spreizungK, zone);
            for (int i = 0; i < kwh.Length; i++) kwh[i] = _mengeJeSchritt[i] * faktor;
            return Array.AsReadOnly(kwh);
        }

        /// <summary>Die Energie der ganzen Reihe [kWh] — die Summe von <see cref="EnergieJeSchrittKwh"/>.</summary>
        internal double EnergieKwh(double spreizungK, string zone = "")
            => IstVolumen ? Menge * Mengengeruest.EnergieKwh(Mengengeruest.LITER_JE_M3, spreizungK, zone) : Menge;

        /// <summary>
        /// <b>Die vollständigen Stunden der Reihe</b> [kWh]: je Kalenderstunde die Summe ihrer
        /// Zeitschritte — aber nur, wenn die Stunde ALLE ihre Schritte trägt. Eine angeschnittene
        /// erste oder letzte Stunde fällt weg: Sie täuschte sonst einen kleinen Stundenwert vor und
        /// verschöbe die Spitze (Kapitel 7 Zeile Z5, Kennzahl (b)).
        ///
        /// <para>Eine Tagesreihe (<see cref="StundenweiseTauglich"/> false) liefert eine leere
        /// Liste — der Aufrufer nennt das benannt, er rechnet nicht mit null.</para>
        /// </summary>
        internal IReadOnlyList<Messstunde> Stundenwerte(double spreizungK, string zone = "")
        {
            if (!StundenweiseTauglich) return new Messstunde[0];
            int jeStunde = (int)MINUTEN_JE_STUNDE / AufloesungMin;
            IReadOnlyList<double> kwh = EnergieJeSchrittKwh(spreizungK, zone);
            var stunden = new List<Messstunde>(Schritte / jeStunde + 1);

            int i = 0;
            // Der erste Schritt, der auf einer Stunde beginnt (bei Minute 0 ist das der erste).
            while (i < Schritte && Zeitpunkt(i).Minute % (int)MINUTEN_JE_STUNDE != 0) i++;
            for (; i + jeStunde <= Schritte; i += jeStunde)
            {
                DateTime beginn = Zeitpunkt(i);
                double summe = 0.0;
                for (int k = 0; k < jeStunde; k++) summe += kwh[i + k];
                stunden.Add(new Messstunde(beginn, summe));
            }
            return stunden;
        }
    }
}
