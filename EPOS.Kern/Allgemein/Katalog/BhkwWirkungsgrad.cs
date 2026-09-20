using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER BHKW-WIRKUNGSGRAD HAT ZWEI ANTEILE - die eine Rechenstelle (Schemaschritt 99)
    //
    // ANWENDERENTSCHEID 20.09.2026: "Der Wirkungsgrad sollte sich aus dem elektrischen
    // und dem thermischen Wirkungsgrad ergeben."
    //
    // WAS DAS HEISST. Ein BHKW liefert aus einer Brennstoffmenge zweierlei: Strom und
    // Waerme. Der elektrische Wirkungsgrad sagt, welcher Teil des Brennstoffs zu Strom
    // wird, der thermische, welcher zu Waerme; erst BEIDE zusammen ergeben den
    // GESAMTwirkungsgrad, mit dem der Rechenweg arbeitet:
    //     Verbrauch = (Waerme + Strom) / Wirkungsgrad         (SimulationBHKW)
    // Gepflegt werden ab Schritt 99 die zwei Anteile; der Gesamtwirkungsgrad ist ihre
    // SUMME und wird beim Speichern nachgezogen - eine Wahrheit, drei Spalten.
    //
    // WARUM DIE SPALTE Wirkungsgrad BLEIBT. Der Rechenweg liest sie, und er bleibt
    // unangetastet: Schritt 99 aendert die PFLEGE, nicht die Formel. Wer die Anteile
    // speichert, schreibt die Summe mit; wer nur den Altbestand traegt (beide Anteile
    // NULL), rechnet unveraendert weiter.
    //
    // DIE RUECKRICHTUNG steht daneben (Aufteilen): Ein Altbestandssatz hat nur den
    // Gesamtwert. Aus ihm laesst sich die Aufteilung SCHAETZEN - im Verhaeltnis der
    // Leistungen, denn der Strom- und der Waermestrom eines Moduls stehen im selben
    // Verhaeltnis wie Pel und Ptherm:
    //     eta_el = eta * Pel / (Pel + Ptherm),  eta_th = eta * Ptherm / (Pel + Ptherm).
    // Diese Schaetzung ist ein VORSCHLAG - der Dialog zeigt sie, gespeichert wird sie
    // erst, wenn der Anwender speichert. Derselbe Ausdruck steht als SQL im Datenteil
    // von Schritt 99 (BhkwWirkungsgradAnteile).
    //
    // DAS BAND. Jeder Anteil liegt in (0; 1) - ein Anteil von 0 oder mehr ist keiner,
    // und ein einzelner Anteil von 1 liesse fuer den anderen nichts uebrig. Die SUMME
    // liegt in (0; 1,05]: 1,05 laesst das Brennwertgeraet zu, dessen Gesamtwirkungsgrad
    // auf den Heizwert bezogen ueber 1 liegt. Die Obergrenze kommt aus derselben Quelle
    // wie in Schritt 98 (BhkwWirkungsgradFaktor.BAND_BIS) - die Abweisung, die dort am
    // Dialog und am Aufklapper stand, steht ab hier HIER.
    // ====================================================================================

    /// <summary>
    /// Die eine Rechenstelle des BHKW-Wirkungsgrads (Schemaschritt 99, Anwenderentscheid
    /// 20.09.2026): <see cref="Gesamt"/> aus den zwei Anteilen, <see cref="Aufteilen"/>
    /// zurueck in die Anteile und <see cref="Pruefen"/> gegen das Band.
    /// </summary>
    public static class BhkwWirkungsgrad
    {
        /// <summary>Die Spalte des elektrischen Anteils.</summary>
        public const string SPALTE_EL = "Wirkungsgrad_el";

        /// <summary>Die Spalte des thermischen Anteils.</summary>
        public const string SPALTE_TH = "Wirkungsgrad_th";

        /// <summary>Die Spalte der Summe — sie bleibt, der Rechenweg liest sie.</summary>
        public const string SPALTE_GESAMT = "Wirkungsgrad";

        /// <summary>
        /// Die Untergrenze eines Anteils — <b>ausschliesslich</b>. Ein Anteil von 0 ist
        /// keiner; NULL heisst „nicht gepflegt" und wird gar nicht erst geprueft.
        /// </summary>
        public const double ANTEIL_VON = 0.0;

        /// <summary>
        /// Die Obergrenze eines Anteils — <b>ausschliesslich</b>. Ein einzelner Anteil
        /// von 1 liesse fuer den anderen nichts uebrig, und ein BHKW hat beide.
        /// </summary>
        public const double ANTEIL_BIS = 1.0;

        /// <summary>
        /// Die Obergrenze der SUMME — dieselbe Zahl wie das Band von Schritt 98
        /// (<see cref="BhkwWirkungsgradFaktor.BAND_BIS"/>): 1,05 laesst das
        /// Brennwertgeraet zu, dessen Gesamtwirkungsgrad auf den Heizwert bezogen ueber
        /// 1 liegt.
        /// </summary>
        public const double GESAMT_BIS = BhkwWirkungsgradFaktor.BAND_BIS;

        /// <summary>Stellen der Rundung — vier, wie die gepflegten Faktoren des Katalogs.</summary>
        public const int STELLEN = BhkwWirkungsgradFaktor.STELLEN;

        // =================================================================
        //  Die eine Richtung: zwei Anteile ergeben den Gesamtwirkungsgrad
        // =================================================================

        /// <summary>
        /// Der Gesamtwirkungsgrad — die SUMME der zwei Anteile. Mehr ist es nicht, und
        /// genau deshalb steht die Rechnung an einer Stelle: Wer sie abschreibt, hat
        /// zwei Wahrheiten.
        /// </summary>
        public static double Gesamt(double el, double th)
        {
            return el + th;
        }

        /// <summary>
        /// Derselbe Wert aus zwei Leerstellen: <c>null</c>, solange auch nur EIN Anteil
        /// fehlt — aus einem halben Paar laesst sich keine Summe bilden, und eine 0
        /// stuende dort fuer „Wirkungsgrad null".
        /// </summary>
        public static double? Gesamt(double? el, double? th)
        {
            if (!el.HasValue || !th.HasValue) return null;
            return Gesamt(el.Value, th.Value);
        }

        // =================================================================
        //  Die Rueckrichtung: ein Gesamtwert ergibt einen VORSCHLAG
        // =================================================================

        /// <summary>Was <see cref="Aufteilen"/> liefert — beides oder nichts.</summary>
        /// <param name="El">Der elektrische Anteil, <c>null</c> wenn nicht bestimmbar.</param>
        /// <param name="Th">Der thermische Anteil, <c>null</c> wenn nicht bestimmbar.</param>
        public sealed record Aufteilung(double? El, double? Th);

        /// <summary>
        /// Die Aufteilung eines Gesamtwirkungsgrads im Verhaeltnis der Leistungen —
        /// <b>ein Vorschlag, keine Behauptung</b>: Der Strom- und der Waermestrom eines
        /// Moduls stehen im selben Verhaeltnis wie <c>Pel</c> und <c>Ptherm</c>.
        /// </summary>
        /// <remarks>
        /// <para>Gebraucht an zwei Stellen: im Datenteil von Schemaschritt 99
        /// (<see cref="BhkwWirkungsgradAnteile"/>, dort als SQL derselben Form) und beim
        /// Lesen eines Altbestandssatzes, dessen zwei Anteile noch NULL sind — der
        /// Dialog zeigt die Aufteilung als Vorschlag, gespeichert wird sie erst beim
        /// Speichern.</para>
        /// <para><b>Ohne beide Leistungen gibt es nichts zu teilen</b>: Fehlt
        /// <c>Pel</c> oder <c>Ptherm</c> oder ist eine davon 0, bleiben beide Anteile
        /// <c>null</c>. Dasselbe gilt fuer einen Gesamtwert, der kein Faktor ist.</para>
        /// </remarks>
        public static Aufteilung Aufteilen(double? gesamt, double? pel, double? ptherm)
        {
            if (!gesamt.HasValue || gesamt.Value <= ANTEIL_VON) return new Aufteilung(null, null);
            if (!pel.HasValue || pel.Value <= 0) return new Aufteilung(null, null);
            if (!ptherm.HasValue || ptherm.Value <= 0) return new Aufteilung(null, null);

            double summe = pel.Value + ptherm.Value;
            double el = Runden(gesamt.Value * pel.Value / summe);
            double th = Runden(gesamt.Value * ptherm.Value / summe);
            return new Aufteilung(el, th);
        }

        /// <summary>Vier Stellen, Mitte vom Nullpunkt weg — so rundet auch SQLite.</summary>
        public static double Runden(double wert)
        {
            return Math.Round(wert, STELLEN, MidpointRounding.AwayFromZero);
        }

        // =================================================================
        //  Die Pruefung — drei Regeln, drei benannte Meldungen
        // =================================================================

        /// <summary>
        /// <c>null</c>, wenn die zwei Anteile gepflegt werden duerfen; sonst der
        /// Ablehnungsgrund im Klartext (lokalisiert).
        /// </summary>
        /// <remarks>
        /// <para><b>Beide leer heisst „nichts gepflegt"</b> und geht durch: Ein
        /// Altbestandssatz, dessen Leistungen fehlen, laesst sich nicht aufteilen, und
        /// er soll trotzdem in seinen uebrigen Feldern pflegbar bleiben. Ein HALBES
        /// Paar geht nicht durch — der fehlende Anteil verletzt die erste Regel.</para>
        /// <para><b>Drei Regeln, in dieser Reihenfolge:</b> der elektrische Anteil in
        /// (0; 1), der thermische in (0; 1), die Summe in (0;
        /// <see cref="GESAMT_BIS"/>]. Jede hat ihre eigene Meldung; abgelehnt wird
        /// BENANNT, nie still.</para>
        /// </remarks>
        public static string Pruefen(double? el, double? th)
        {
            if (!el.HasValue && !th.HasValue) return null;

            if (!Anteil(el))
                return Text("BHKWW_MSG_EL",
                            "Der elektrische Wirkungsgrad ist ein Faktor größer 0 und " +
                            "kleiner 1 (z. B. 0,30), kein Prozentwert.");

            if (!Anteil(th))
                return Text("BHKWW_MSG_TH",
                            "Der thermische Wirkungsgrad ist ein Faktor größer 0 und " +
                            "kleiner 1 (z. B. 0,60), kein Prozentwert.");

            double summe = Gesamt(el.Value, th.Value);
            if (summe <= ANTEIL_VON || summe > GESAMT_BIS)
                return string.Format(
                    Text("BHKWW_MSG_SUMME",
                         "Elektrischer und thermischer Wirkungsgrad ergeben zusammen " +
                         "höchstens {0}; {1} ist zu viel."),
                    GESAMT_BIS.ToString(CultureInfo.CurrentCulture),
                    summe.ToString("0.###", CultureInfo.CurrentCulture));

            return null;
        }

        /// <summary>Liegt ein einzelner Anteil im offenen Band (0; 1)?</summary>
        public static bool Anteil(double? wert)
        {
            return wert.HasValue && wert.Value > ANTEIL_VON && wert.Value < ANTEIL_BIS;
        }

        /// <summary>
        /// Der Wert, den der Schreibweg in die Spalte <c>Wirkungsgrad</c> stellt:
        /// die Summe der zwei Anteile, solange beide gepflegt sind — sonst der Wert,
        /// der schon dort stand (Altbestand vor Schritt 99).
        /// </summary>
        public static double GesamtZumSchreiben(double? el, double? th, double bisher)
        {
            double? summe = Gesamt(el, th);
            return summe ?? bisher;
        }

        /// <summary>Text aus den Ressourcen, sonst der deutsche Rueckfall.</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
