using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie ein <see cref="Zahlenbedingung"/> vergleicht (Anwenderentscheid
    /// <b>W14a-E-10-Q1</b>, 07.09.2026: EIN Feld je Zahlenspalte).
    /// </summary>
    public enum Zahlenvergleich
    {
        /// <summary><c>=15</c> und die blosse Zahl <c>15</c>.</summary>
        Gleich,

        /// <summary><c>&gt;10</c>.</summary>
        Groesser,

        /// <summary><c>&gt;=10</c>.</summary>
        GroesserGleich,

        /// <summary><c>&lt;60</c>.</summary>
        Kleiner,

        /// <summary><c>&lt;=60</c>.</summary>
        KleinerGleich,

        /// <summary><c>10..60</c> — beide Grenzen EINGESCHLOSSEN.</summary>
        Bereich
    }

    /// <summary>
    /// Eine gelesene Zahlenbedingung — das Ergebnis von
    /// <see cref="Zahlenausdruck.Lesen(string, CultureInfo)"/>.
    /// </summary>
    public sealed class Zahlenbedingung
    {
        internal Zahlenbedingung(Zahlenvergleich vergleich, double wert, double obergrenze)
        {
            Vergleich = vergleich;
            Wert = wert;
            Obergrenze = obergrenze;
        }

        /// <summary>Welcher Vergleich.</summary>
        public Zahlenvergleich Vergleich { get; }

        /// <summary>Der Vergleichswert; bei <see cref="Zahlenvergleich.Bereich"/> die UNTERgrenze.</summary>
        public double Wert { get; }

        /// <summary>Die Obergrenze — nur bei <see cref="Zahlenvergleich.Bereich"/> belegt.</summary>
        public double Obergrenze { get; }

        /// <summary>
        /// Trifft die Bedingung auf einen Wert zu? Ein Wert, den es nicht gibt
        /// (<c>null</c> — die Spalte zeigt dort einen Halbgeviertstrich), trifft NIE:
        /// Wer nach „&gt;10" fragt, meint keine Leerstelle.
        /// </summary>
        public bool Trifft(double? wert)
        {
            if (wert == null) return false;
            double w = wert.Value;

            switch (Vergleich)
            {
                case Zahlenvergleich.Gleich:         return Gleich(w, Wert);
                case Zahlenvergleich.Groesser:       return w > Wert && !Gleich(w, Wert);
                case Zahlenvergleich.GroesserGleich: return w > Wert || Gleich(w, Wert);
                case Zahlenvergleich.Kleiner:        return w < Wert && !Gleich(w, Wert);
                case Zahlenvergleich.KleinerGleich:  return w < Wert || Gleich(w, Wert);
                case Zahlenvergleich.Bereich:
                    return (w > Wert || Gleich(w, Wert)) &&
                           (w < Obergrenze || Gleich(w, Obergrenze));
            }
            return true;
        }

        /// <summary>
        /// Gleichheit mit Zahlenrand. Die Werte der Spalten entstehen aus Divisionen
        /// (Wirkungsgrad, C-Rate, Stromkennzahl); ohne Rand traefe <c>=0,5</c> eine
        /// C-Rate von 0,49999999999999994 nicht. Derselbe Gedanke wie
        /// <c>Rechenrand</c> im Rechenweg — nur gehoert er HIER nicht zum Ergebnis,
        /// sondern zur Anzeige.
        /// </summary>
        private static bool Gleich(double a, double b)
        {
            double rand = 1e-9 + 1e-9 * Math.Abs(b);
            return Math.Abs(a - b) <= rand;
        }
    }

    /// <summary>
    /// <b>Der Ausdruck EINER Zahlenspalte</b> (Anwenderentscheid <b>W14a-E-10</b> vom
    /// 07.09.2026, Frage <b>Q1 = ja</b>, Konzept_Katalogfilter 5.6.3 und 8.1).
    ///
    /// <para><b>Warum es das gibt.</b> Der Filter sitzt seit W14a-E-10 im Spaltenkopf,
    /// und jedes Popover soll gleich aussehen — EIN Feld, egal ob Text- oder
    /// Zahlenspalte. Eine Zahlenspalte muss damit mehr koennen als „enthaelt 15":
    /// Sie versteht <c>&gt;10</c>, <c>&gt;=10</c>, <c>&lt;60</c>, <c>&lt;=60</c>,
    /// <c>=15</c>, <c>10..60</c> und die blosse Zahl <c>15</c> (= <c>=15</c>).
    /// Die Alternative — zwei Felder „von / bis" wie in den Importmasken — hat der
    /// Anwender mit „nur Suche" abgewaehlt (8.1, V2).</para>
    ///
    /// <para><b>Ein unverstandener Ausdruck ist KEIN Filter</b> — dieselbe Regel wie
    /// beim kaputten Suchmuster (<see cref="Suchmuster.Uebersetzen"/>): Der Anwender
    /// tippt gerade, und ein halb geschriebener Bereich darf die Liste nicht leeren.
    /// <see cref="Lesen(string, CultureInfo)"/> liefert dafuer <c>null</c>.</para>
    ///
    /// <para><b>Das Dezimaltrennzeichen ist das der KULTUR</b> (5.6.3). In de-DE
    /// trennt das Komma, in en-US der Punkt; der Bereichspunkt <c>..</c> wird VORHER
    /// abgespalten, damit <c>1.5..2.5</c> unter en-US und <c>1,5..2,5</c> unter de-DE
    /// dasselbe bedeuten. Tausenderpunkte sind nicht erlaubt — sonst waere <c>1.5</c>
    /// unter de-DE die Zahl 15.</para>
    ///
    /// <para><b>Ohne Oberflaeche pruefbar</b> — das ist der Grund, warum die Klasse im
    /// Kern steht und nicht im Popover (Konzept 5.6.7, zweiter Punkt).</para>
    /// </summary>
    public static class Zahlenausdruck
    {
        /// <summary>Das Zeichen, mit dem ein Bereich geschrieben wird.</summary>
        public const string BEREICH = "..";

        /// <summary>
        /// Liest einen Ausdruck. <c>null</c> heisst „kein Filter" — bei leerer
        /// Eingabe und bei jedem Ausdruck, den diese Klasse nicht versteht.
        /// </summary>
        /// <param name="eingabe">Der Text aus dem Popover.</param>
        /// <param name="kultur">
        /// Die Kultur des Dezimaltrennzeichens; <c>null</c> nimmt
        /// <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        public static Zahlenbedingung Lesen(string eingabe, CultureInfo kultur = null)
        {
            string text = (eingabe ?? "").Trim();
            if (text.Length == 0) return null;

            CultureInfo k = kultur ?? CultureInfo.CurrentCulture;

            // Der BEREICH zuerst: "10..60". Er wird vor dem Zahlenlesen abgespalten,
            // damit der Punkt der Bereichsschreibweise nicht mit dem
            // Dezimaltrennzeichen von en-US zusammenfaellt.
            int trenn = text.IndexOf(BEREICH, StringComparison.Ordinal);
            if (trenn >= 0)
            {
                double von, bis;
                if (!Zahl(text.Substring(0, trenn), k, out von)) return null;
                if (!Zahl(text.Substring(trenn + BEREICH.Length), k, out bis)) return null;

                // Verdrehte Grenzen ("60..10") werden getauscht statt abgelehnt: Die
                // Absicht ist eindeutig, und eine leere Liste waere die schlechtere
                // Antwort.
                if (bis < von) { double h = von; von = bis; bis = h; }
                return new Zahlenbedingung(Zahlenvergleich.Bereich, von, bis);
            }

            // Die VERGLEICHSZEICHEN, laengste Schreibweise zuerst.
            if (Beginnt(text, ">=")) return Einfach(Zahlenvergleich.GroesserGleich, text, 2, k);
            if (Beginnt(text, "<=")) return Einfach(Zahlenvergleich.KleinerGleich, text, 2, k);
            if (Beginnt(text, "=>")) return Einfach(Zahlenvergleich.GroesserGleich, text, 2, k);
            if (Beginnt(text, "=<")) return Einfach(Zahlenvergleich.KleinerGleich, text, 2, k);
            if (Beginnt(text, ">"))  return Einfach(Zahlenvergleich.Groesser, text, 1, k);
            if (Beginnt(text, "<"))  return Einfach(Zahlenvergleich.Kleiner, text, 1, k);
            if (Beginnt(text, "="))  return Einfach(Zahlenvergleich.Gleich, text, 1, k);

            // Die BLOSSE ZAHL ist die Gleichheit (Entscheid W14a-E-10-Q1: "15" = "=15",
            // nicht ">=15" — die Gleichheit ist die einzige Lesart, die bei einer
            // Textspalte dasselbe bedeutet).
            {
                double wert;
                if (!Zahl(text, k, out wert)) return null;
                return new Zahlenbedingung(Zahlenvergleich.Gleich, wert, wert);
            }
        }

        /// <summary>
        /// Versteht diese Klasse den Ausdruck? Leer gilt als verstanden (kein Filter);
        /// die Oberflaeche kann damit einen unverstandenen Ausdruck leise kennzeichnen.
        /// </summary>
        public static bool Verstanden(string eingabe, CultureInfo kultur = null)
        {
            return (eingabe ?? "").Trim().Length == 0 || Lesen(eingabe, kultur) != null;
        }

        private static bool Beginnt(string text, string zeichen)
        {
            return text.StartsWith(zeichen, StringComparison.Ordinal);
        }

        private static Zahlenbedingung Einfach(Zahlenvergleich vergleich, string text,
                                               int laenge, CultureInfo kultur)
        {
            double wert;
            if (!Zahl(text.Substring(laenge), kultur, out wert)) return null;
            return new Zahlenbedingung(vergleich, wert, wert);
        }

        /// <summary>
        /// Liest eine Zahl in der Kultur. OHNE <c>AllowThousands</c>: Der Punkt in
        /// „1.5" ist unter de-DE sonst ein Tausenderpunkt, und der Anwender bekaeme
        /// fuer 1,5 die Zahl 15.
        /// </summary>
        private static bool Zahl(string text, CultureInfo kultur, out double wert)
        {
            return double.TryParse((text ?? "").Trim(), NumberStyles.Float, kultur, out wert);
        }
    }
}
