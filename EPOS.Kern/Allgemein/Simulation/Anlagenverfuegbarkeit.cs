using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Warum die Anlage in einer Stunde nicht mehr anbieten kann</b> — die Anlagenseite der beiden
    /// Aufzählungen aus Anlagenkopplung 5.3 (F2). Sie entsteht im <see cref="Anlagenfahrplan"/> bzw. in der
    /// Verteilung (<see cref="Verfuegbarkeitsverteilung"/>) und reist neben dem gebäudeseitigen
    /// Begrenzungsgrund <c>Verfuegbarkeit</c> mit (Paarungsregel), nie an seiner Stelle.
    /// </summary>
    internal enum Verfuegbarkeitsgrund
    {
        /// <summary><c>KEINE_BEGRENZUNG</c>: Jeder Erzeuger steht mit voller Leistung bereit.</summary>
        KeineBegrenzung,

        /// <summary><c>SPERRZEIT</c>: Eine Wärmepumpe ist gesperrt (<c>Sperrung</c>, <c>Sperrzeit_von/bis</c>, Sperrfenster).</summary>
        Sperrzeit,

        /// <summary><c>ZEITPROGRAMM</c>: Das Zeitprogramm eines Erzeugers gibt weniger als den vollen Faktor frei.</summary>
        Zeitprogramm,

        /// <summary><c>LEISTUNGSGRENZE</c>: Die Summe der Nennleistungen reicht für den unbegrenzten Bedarf der Stunde nicht.</summary>
        Leistungsgrenze,

        /// <summary><c>SPEICHER_LEER</c>: In einer Sperrstunde reicht der Speichervorrat über die Sperrdauer nicht.</summary>
        SpeicherLeer,

        /// <summary><c>ABSCHALTPUNKT</c>: Eine bivalent betriebene Wärmepumpe ist unter ihrem Abschaltpunkt aus.</summary>
        Abschaltpunkt,

        /// <summary><c>UMSCHALTUNG</c>: Eine reversible Maschine steht an einem Kühltag der Heizseite nicht zur Verfügung (7.3).</summary>
        Umschaltung,

        /// <summary><c>KEIN_ERZEUGER</c>: Das Projekt hat keinen Wärmeerzeuger in der Kaskade.</summary>
        KeinErzeuger,
    }

    /// <summary>
    /// <b>Die Naht <c>Anlagenverfuegbarkeit</c></b> (Anlagenkopplung 5.3, F2): je Stunde die obere Schranke
    /// dessen, was ankommen kann, die höchste Vorlauftemperatur, die die Anlagenseite stellt, und der Grund.
    /// Für das Projekt bildet sie der <see cref="Anlagenfahrplan"/>, je Gebäude und Zone die
    /// <see cref="Verfuegbarkeitsverteilung"/>; das Modul <c>Gebaeude/</c> bekommt sie fertig über den
    /// Stundenrand und liest keine Anlagendaten.
    /// </summary>
    internal readonly struct Anlagenverfuegbarkeit
    {
        internal Anlagenverfuegbarkeit(double leistungKw, double vorlaufC, Verfuegbarkeitsgrund grund)
        {
            LeistungKw = leistungKw;
            VorlaufC = vorlaufC;
            Grund = grund;
        }

        /// <summary>Obere Schranke der Heizleistung in dieser Stunde [kW], ≥ 0.</summary>
        internal double LeistungKw { get; }

        /// <summary>Höchster Vorlauf, den die verfügbaren Erzeuger anbieten [°C]; NaN = keine Grenze bekannt.</summary>
        internal double VorlaufC { get; }

        /// <summary>Warum die Schranke unter der vollen Leistung liegt; <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/> sonst.</summary>
        internal Verfuegbarkeitsgrund Grund { get; }

        /// <summary>Dieselbe Verfügbarkeit mit einer anderen Leistung (Verteilung, Skalierung).</summary>
        internal Anlagenverfuegbarkeit MitLeistung(double leistungKw) => new Anlagenverfuegbarkeit(leistungKw, VorlaufC, Grund);

        /// <summary>Dieselbe Verfügbarkeit mit einem anderen Grund.</summary>
        internal Anlagenverfuegbarkeit MitGrund(Verfuegbarkeitsgrund grund) => new Anlagenverfuegbarkeit(LeistungKw, VorlaufC, grund);
    }

    /// <summary>
    /// <b>Die Verteilung der Schranke</b> (Anlagenkopplung 6.2, F4) — proportional zum unbegrenzten Bedarf der
    /// Stunde, nach dem Vorbild <c>Kanalsatz.NetzverlusteVerteilen</c>, mit zwei benannten Regeln:
    /// <list type="bullet">
    /// <item><b>Randfall Summe ≤ 0:</b> Niemand verlangt etwas — jeder Teilnehmer bekommt die volle Schranke.</item>
    /// <item><b>Rundungsrest:</b> Der Rest der Gleitkommarechnung geht an den Teilnehmer mit dem größten Anteil,
    /// bei Gleichstand an den mit der kleineren Id — nie an den zuletzt bedienten. So hängt das Ergebnis nicht an
    /// der Zeilenreihenfolge.</item>
    /// </list>
    /// Dieselbe Regel gilt in beiden Stufen: Projekt → Gebäude und Gebäude → Zonen. Ein einziger Teilnehmer bekommt
    /// die Schranke unverändert (bitgleich).
    /// </summary>
    internal static class Verfuegbarkeitsverteilung
    {
        /// <summary>
        /// Teilt <paramref name="schranke"/> auf die Teilnehmer auf. Negative oder nicht endliche Schlüssel zählen
        /// als 0. Rückgabe in der Reihenfolge der Eingabe.
        /// </summary>
        /// <param name="schranke">Die aufzuteilende Leistung (beliebige Einheit), ≥ 0.</param>
        /// <param name="ids">Die Ids der Teilnehmer — nur für den Gleichstand des Rundungsrests.</param>
        /// <param name="schluessel">Der unbegrenzte Bedarf je Teilnehmer in derselben Stunde.</param>
        internal static double[] Verteilen(double schranke, IReadOnlyList<long> ids, IReadOnlyList<double> schluessel)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (schluessel == null) throw new ArgumentNullException(nameof(schluessel));
            if (ids.Count != schluessel.Count) throw new ArgumentException("Ids und Schlüssel passen nicht zusammen.", nameof(schluessel));
            int n = ids.Count;
            var anteil = new double[n];
            if (n == 0) return anteil;
            if (n == 1)
            {
                anteil[0] = schranke;
                return anteil;
            }

            double summe = 0.0;
            for (int i = 0; i < n; i++) summe += Gueltig(schluessel[i]);
            if (!(summe > 0.0))
            {
                for (int i = 0; i < n; i++) anteil[i] = schranke;
                return anteil;
            }

            double verteilt = 0.0;
            int groesster = -1;
            for (int i = 0; i < n; i++)
            {
                anteil[i] = schranke * (Gueltig(schluessel[i]) / summe);
                verteilt += anteil[i];
                if (groesster < 0 || anteil[i] > anteil[groesster]
                    || (anteil[i] == anteil[groesster] && ids[i] < ids[groesster]))
                    groesster = i;
            }
            anteil[groesster] += schranke - verteilt;
            return anteil;
        }

        private static double Gueltig(double x) => x > 0.0 && !double.IsInfinity(x) ? x : 0.0;
    }
}
