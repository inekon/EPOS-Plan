using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die eine Stelle, an der die Investitionssumme eines BHKW definiert ist.
    /// <para>
    /// <b>Nutzerentscheid (22.08.2026): Die Einzelposten fuehren.</b> Modul, Montage und
    /// Inbetriebnahme, Lieferung, Schallschutzhaube und Abgasreinigung
    /// (<c>Kosten_Modul</c>, <c>Kosten_Montage</c>, <c>Kosten_Lieferung</c>,
    /// <c>Kosten_Schallschutzhaube</c>, <c>Kosten_Abgasreinigung</c>) werden frei
    /// erfasst; ihre Summe IST die Investition. Der spezifische Wert
    /// <c>Investition_kwel</c> [EUR/kWel] wird daraus abgeleitet und beim Speichern
    /// nachgezogen.
    /// </para>
    /// <para>
    /// <b>Anwenderentscheid W14a-E-8-B3 (07.09.2026): Drei Eingabewege auf DIESELBE
    /// Groesse.</b> „Entweder Investitionskosten als Summe/Gesamt oder
    /// Investitionskosten auf kWh elektrisch x Kosten pro kWh elektrisch (sollte
    /// umgerechnet werden, je nach Eingabe)." Der Katalogeditor nimmt die Investition
    /// deshalb wahlweise als GESAMTSUMME [EUR], als SPEZIFISCHEN Wert [EUR/kWel] oder
    /// - wie bisher - als die fuenf Einzelposten entgegen. Die Rechenregel dazu steht
    /// hier und nur hier:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>Gesamt = Modul + Montage + Lieferung +
    ///     Schallschutzhaube + Abgasreinigung</c> (<see cref="Gesamt"/>),</description></item>
    ///   <item><description><c>je kWel = Gesamt / Pel</c>
    ///     (<see cref="JeKWel"/>, fuer die Anzeige gerundet
    ///     <see cref="JeKWelEingabe"/>),</description></item>
    ///   <item><description><c>Gesamt = je kWel * Pel</c> (<see cref="GesamtAusJeKWel"/>) und</description></item>
    ///   <item><description><c>Modul = Gesamt - (Montage + Lieferung +
    ///     Schallschutzhaube + Abgasreinigung)</c>
    ///     (<see cref="ModulAusGesamt"/>).</description></item>
    /// </list>
    /// <para>
    /// <b>Der Ausgleich laeuft immer ueber <c>Kosten_Modul</c>.</b> Wer Gesamt oder den
    /// spezifischen Wert eingibt, aendert nur den Modulpreis; die vier Nebenposten
    /// bleiben stehen. Damit bleibt die Kostenplanung unberuehrt: Sie rechnet
    /// weiterhin mit den fuenf Posten (<c>TechnikPlanwertCtrl.BasenFuellen</c>,
    /// Basis <c>MODULPREIS</c> und vier <c>Neben(...)</c>-Zeilen), und keine Eingabe
    /// erzeugt einen sechsten Betrag, der zweimal zaehlen wuerde.
    /// </para>
    /// <para>
    /// <b>Warum die alte Fassung nur EINEN Weg zuliess.</b> Zur selben
    /// Investitionssumme fuehrten zwei UNGEPRUEFTE Wege, und sie liefen auseinander:
    /// Modul <c>A-Tron_21_F</c> traegt Pel = 21,00 kW, alle Einzelposten 0,00 EUR und
    /// <c>Investition_kwel</c> = 2000 - also 0 EUR auf dem einen und 42.000 EUR auf
    /// dem anderen Weg. Seit B3 gibt es die zwei Wege wieder, aber als UMRECHNUNG:
    /// Jede Eingabe landet in denselben fuenf Posten, das jeweils andere Feld folgt
    /// aus ihnen. Auseinanderlaufen koennen sie nicht mehr.
    /// </para>
    /// <para>
    /// Aufrufer: <c>BhkwKatalogDialog</c> (Anzeige, Eingabe und Umrechnung),
    /// <c>BhkwHuelle</c> (Bruecke zum Kern), <c>BHKWStammCtrl.Update</c> und
    /// <c>BHKWCtrl.Update</c> (Schreibwege). <c>BHKWCtrl.CopyFromStamm</c> rechnet
    /// bewusst NICHT nach: es kopiert einen Stammsatz unveraendert ins Projekt,
    /// Bestandsdaten bleiben dabei so, wie sie sind (der Bestandsabgleich ist ein
    /// eigener Schritt).
    /// </para>
    /// </summary>
    public static class BHKWKosten
    {
        /// <summary>
        /// Rundungsstelle jedes Eurobetrags: der Cent. Gerundet wird kaufmaennisch
        /// (<see cref="MidpointRounding.AwayFromZero"/>) und nicht auf die gerade
        /// Stelle - 0,005 EUR ist ein halber Cent und wird aufgerundet.
        /// </summary>
        private const int STELLEN_EURO = 2;

        /// <summary>
        /// Rundungsstelle des spezifischen Werts: 1 EUR/kWel. Feiner ist er nicht
        /// gemeint - er ist eine Kennzahl, keine Rechnungsposition. Die Rundung gilt
        /// fuer die ANZEIGE (<see cref="JeKWelEingabe"/>); was der Anwender tippt,
        /// wird so genommen, wie er es tippt, und erst das Euro-Ergebnis wieder auf
        /// Cent gerundet.
        /// </summary>
        private const int STELLEN_JE_KWEL = 0;

        /// <summary>Summe der fuenf Einzelposten [EUR] - die Investition des Geraets.</summary>
        public static double Summe(double modul, double montage, double lieferung,
                                   double schallschutzhaube, double abgasreinigung)
        {
            return modul + montage + lieferung + schallschutzhaube + abgasreinigung;
        }

        /// <summary>
        /// Die Investition als GESAMTSUMME [EUR] - <see cref="Summe"/> auf Cent
        /// gerundet. Das ist der Wert, den der Editor im Feld „Investition gesamt"
        /// zeigt und den <see cref="ModulAusGesamt"/> wieder zerlegt; die Rundung
        /// haelt den Weg Gesamt -> Modul -> Gesamt stabil.
        /// </summary>
        public static double Gesamt(double modul, double montage, double lieferung,
                                    double schallschutzhaube, double abgasreinigung)
        {
            return Cent(Summe(modul, montage, lieferung, schallschutzhaube, abgasreinigung));
        }

        /// <summary>Summe der VIER Nebenposten [EUR] - alles ausser dem Modulpreis.</summary>
        public static double Nebenposten(double montage, double lieferung,
                                         double schallschutzhaube, double abgasreinigung)
        {
            return Cent(montage + lieferung + schallschutzhaube + abgasreinigung);
        }

        /// <summary>
        /// true, wenn sich aus der Summe ein spezifischer Wert je kWel bilden laesst.
        /// Bei <paramref name="pel"/> = 0 ist er es nicht: jede Zahl mal 0 ergaebe wieder
        /// 0 und wuerde die erfasste Summe verschweigen. Der Dialog sperrt das Feld
        /// dann und sagt in der Herleitungszeile, warum.
        /// </summary>
        public static bool JeKWelBestimmbar(double pel)
        {
            return pel > 0.0;
        }

        /// <summary>
        /// Der abgeleitete spezifische Wert [EUR/kWel] = Summe / Pel, UNGERUNDET. Das
        /// ist der Wert, der in <c>Investition_kwel</c> geschrieben wird; er bleibt
        /// exakt, damit <c>Gesamt = Investition_kwel * Pel</c> die erfasste Summe
        /// wieder trifft. Ist er nicht bestimmbar (Pel = 0), liefert die Methode 0 -
        /// dann steht in der Spalte keine Zahl mehr, die zu den Posten nicht mehr
        /// passt. Die Summe selbst bleibt in den Postenspalten erhalten und geht von
        /// dort in die Kostenrechnung.
        /// </summary>
        public static double JeKWel(double summe, double pel)
        {
            return JeKWelBestimmbar(pel) ? summe / pel : 0.0;
        }

        /// <summary>
        /// Derselbe Wert fuer das EINGABEFELD: auf <see cref="STELLEN_JE_KWEL"/>
        /// gerundet. Die Anzeige liegt damit auf demselben Raster, auf dem der
        /// Anwender tippt - was er sieht, kann er unveraendert wieder eingeben, ohne
        /// dass die Gesamtsumme um Nachkommastellen wandert.
        /// </summary>
        public static double JeKWelEingabe(double gesamt, double pel)
        {
            return JeKWelBestimmbar(pel)
                ? Math.Round(gesamt / pel, STELLEN_JE_KWEL, MidpointRounding.AwayFromZero)
                : 0.0;
        }

        /// <summary>
        /// Die Gegenrichtung: aus dem spezifischen Wert [EUR/kWel] und der
        /// elektrischen Leistung die Gesamtsumme [EUR], auf Cent gerundet. Ohne
        /// elektrische Leistung gibt es keine Umrechnung - dann 0, und der Dialog
        /// laesst das Feld gesperrt.
        /// </summary>
        public static double GesamtAusJeKWel(double jeKWel, double pel)
        {
            return JeKWelBestimmbar(pel) ? Cent(jeKWel * pel) : 0.0;
        }

        /// <summary>
        /// Die Aufteilung einer eingegebenen Gesamtsumme auf die Posten: Der
        /// Modulpreis nimmt auf, was die vier Nebenposten uebrig lassen. Liegt die
        /// Gesamtsumme unter den Nebenposten, wird der Modulpreis 0 und NICHT negativ
        /// - ein negativer Geraetepreis waere keine Angabe, sondern ein Fehler. Dass
        /// die Eingabe dann nicht aufgeht, meldet <see cref="NebenpostenUeberschreiten"/>,
        /// und der Dialog sagt es in der Herleitungszeile.
        /// </summary>
        public static double ModulAusGesamt(double gesamt, double montage, double lieferung,
                                            double schallschutzhaube, double abgasreinigung)
        {
            double neben = Nebenposten(montage, lieferung, schallschutzhaube, abgasreinigung);
            return neben >= Cent(gesamt) ? 0.0 : Cent(Cent(gesamt) - neben);
        }

        /// <summary>
        /// true, wenn die vier Nebenposten die eingegebene Gesamtsumme UEBERSTEIGEN.
        /// Dann ist der Modulpreis 0 (<see cref="ModulAusGesamt"/>), und die Summe der
        /// Posten ist groesser als das, was der Anwender eingegeben hat. Genau dieser
        /// Fall bekommt im Dialog einen eigenen Hinweis - stillschweigend darf er
        /// nicht bleiben.
        /// </summary>
        public static bool NebenpostenUeberschreiten(double gesamt, double montage, double lieferung,
                                                     double schallschutzhaube, double abgasreinigung)
        {
            return Nebenposten(montage, lieferung, schallschutzhaube, abgasreinigung) > Cent(gesamt);
        }

        /// <summary>Kaufmaennisch auf den Cent gerundet.</summary>
        private static double Cent(double wert)
        {
            return Math.Round(wert, STELLEN_EURO, MidpointRounding.AwayFromZero);
        }
    }
}
