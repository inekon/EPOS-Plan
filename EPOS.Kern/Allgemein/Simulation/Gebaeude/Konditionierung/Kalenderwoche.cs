using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Was die Prüfung einer Standardwoche ergibt (<see cref="Kalenderwoche.Lesen"/>).</summary>
    public enum Wochenbefund
    {
        /// <summary>168 Zellen gelesen, jede eine Zahl in den Grenzen der Größe oder „aus“.</summary>
        Gelesen,

        /// <summary>Der Text ist leer oder nur Leerraum — <b>keine Woche</b> (die Grundangabe gilt).</summary>
        KeineWoche,

        /// <summary>Nicht genau 168 Zellen; <see cref="Wochenlesung.Gefunden"/> nennt die Zahl.</summary>
        FalscheWertzahl,

        /// <summary>Eine Zelle ist weder eine endliche Zahl noch das Kennwort „aus“; <see cref="Wochenlesung.Stelle"/> nennt sie.</summary>
        KeineZahl,

        /// <summary>Eine Zelle liegt außerhalb der Grenzen ihrer Größe; <see cref="Wochenlesung.Stelle"/> nennt sie.</summary>
        WertAusserhalb,

        /// <summary>Der Text ist länger, als die Spalte trägt (<see cref="KonditionierungSchema.WOCHE_MAX_ZEICHEN"/>).</summary>
        ZuLang,
    }

    /// <summary>
    /// Der Befund eines Leseversuchs samt Werten, gefundener Zellzahl und Fundstelle —
    /// unveränderlich, ohne Ausnahme; die Meldung baut der Verwender
    /// (<see cref="GebaeudeModellFehler.KalenderUngueltig"/> im Lauf, <c>KOND_MSG_*</c> im Dialog).
    /// </summary>
    public sealed class Wochenlesung
    {
        internal Wochenlesung(Wochenbefund befund, double[] werte, int gefunden, int stelle)
        {
            Befund = befund;
            Werte = werte;
            Gefunden = gefunden;
            Stelle = stelle;
        }

        /// <summary>Der Befund.</summary>
        public Wochenbefund Befund { get; }

        /// <summary>
        /// Die 168 Zellen, Montag 00:00 bis Sonntag 23:00 — <b><see cref="double.NaN"/> ist das
        /// Kennzeichen „aus“</b>, unabhängig von der Größe; erst
        /// <see cref="Konditionierungskalender.Auswerten"/> setzt dafür den Wert der Größe ein
        /// (<see cref="Konditionierungsgroessen.AusWert"/>). <c>null</c>, wenn nicht gelesen.
        /// </summary>
        public double[] Werte { get; }

        /// <summary>Die Zahl der gefundenen Zellen (bei <see cref="Wochenbefund.FalscheWertzahl"/>).</summary>
        public int Gefunden { get; }

        /// <summary>Die Fundstelle 1 … 168 (bei <see cref="Wochenbefund.KeineZahl"/> und <see cref="Wochenbefund.WertAusserhalb"/>), sonst 0.</summary>
        public int Stelle { get; }
    }

    /// <summary>
    /// <b>Die Standardwoche eines Konditionierungskalenders als Text</b> (Konzept
    /// Konditionierungsprofile 5.2, Festlegung F4, Entscheid P2 (b)): 168 Zellen von Montag 00:00
    /// bis Sonntag 23:00, Trennzeichen <c>;</c>, Punkt als Dezimaltrennzeichen,
    /// <see cref="CultureInfo.InvariantCulture"/>. Jede Zelle ist eine Zahl mit bis zu vier
    /// Nachkommastellen <b>oder</b> das Kennwort <see cref="DbWerte.KOND_WOCHE_AUS"/>.
    ///
    /// <para><b>Ein eigener strenger Leser</b> — nicht der von <c>Sollwertprofil</c>: Der kennt kein
    /// „aus“, prüft keine Größengrenzen und bleibt unverändert, weil die Anlagenkopplung (AK1) auf
    /// ihm steht (Konzept 5.2). Genau 168 Zellen, kein Auffüllen und kein Abschneiden; jeder
    /// Verstoß ist ein <b>benannter</b> Befund mit Stelle — eine still ergänzte Zelle wäre eine
    /// erfundene Betriebszeit.</para>
    ///
    /// <para><b>Der Rundlauf</b> (<see cref="Rundlauf"/>) ist die Invariante hinter „Anlegen ändert
    /// keine Reihe" (Konzept 3.3 Regel 1): Der Schreiber setzt bis zu
    /// <see cref="NACHKOMMASTELLEN"/> Nachkommastellen, und „Kalender anlegen“ prüft je Wert
    /// Wert → Text → Wert <b>bitgleich</b>; sonst wird das Anlegen benannt abgelehnt, statt eine
    /// Reihe um eine Rundung zu verschieben.</para>
    ///
    /// <para>Unveränderlich, ohne Datenbank, ohne Uhr und ohne Zufall.</para>
    /// </summary>
    public static class Kalenderwoche
    {
        /// <summary>Die Zahl der Zellen einer Woche: 7 Tage × 24 Stunden.</summary>
        public const int WOCHENWERTE = 168;

        /// <summary>Die Zahl der Stunden eines Tages.</summary>
        public const int TAGESSTUNDEN = 24;

        /// <summary>Das Trennzeichen zwischen zwei Zellen.</summary>
        public const char TRENNZEICHEN = ';';

        /// <summary>Die Höchstzahl der Nachkommastellen einer Zelle (Konzept 5.2).</summary>
        public const int NACHKOMMASTELLEN = 4;

        /// <summary>Das Zahlenformat des Schreibers — bis zu vier Nachkommastellen, ohne Nullen am Ende.</summary>
        public const string FORMAT = "0.####";

        /// <summary>
        /// <b>Der strenge Leser.</b> Ein leerer Text (oder <c>null</c>) heißt
        /// <see cref="Wochenbefund.KeineWoche"/> — dann gilt die Grundangabe des Kalenders. Sonst
        /// genau 168 Zellen; Leerraum um eine Zelle ist erlaubt, das Kennwort
        /// <see cref="DbWerte.KOND_WOCHE_AUS"/> wird ohne Rücksicht auf Groß- und Kleinschreibung
        /// (<see cref="StringComparison.OrdinalIgnoreCase"/>) gelesen und als
        /// <see cref="double.NaN"/> abgelegt.
        /// </summary>
        /// <param name="text">Der gespeicherte Text der Spalte <c>Woche</c>.</param>
        /// <param name="groesse">Die Größe — sie bestimmt die Grenzen jeder Zelle (Konzept 3.6).</param>
        public static Wochenlesung Lesen(string text, Konditionierungsgroesse groesse)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new Wochenlesung(Wochenbefund.KeineWoche, null, 0, 0);
            if (text.Length > KonditionierungSchema.WOCHE_MAX_ZEICHEN)
                return new Wochenlesung(Wochenbefund.ZuLang, null, text.Length, 0);

            string[] teile = text.Split(TRENNZEICHEN);
            if (teile.Length != WOCHENWERTE)
                return new Wochenlesung(Wochenbefund.FalscheWertzahl, null, teile.Length, 0);

            var werte = new double[WOCHENWERTE];
            for (int i = 0; i < WOCHENWERTE; i++)
            {
                string zelle = teile[i].Trim();
                if (string.Equals(zelle, DbWerte.KOND_WOCHE_AUS, StringComparison.OrdinalIgnoreCase))
                {
                    werte[i] = double.NaN;
                    continue;
                }
                if (!double.TryParse(zelle, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                    || !double.IsFinite(w))
                    return new Wochenlesung(Wochenbefund.KeineZahl, null, teile.Length, i + 1);
                if (!Konditionierungsgroessen.ImBereich(groesse, w))
                    return new Wochenlesung(Wochenbefund.WertAusserhalb, null, teile.Length, i + 1);
                werte[i] = w;
            }
            return new Wochenlesung(Wochenbefund.Gelesen, werte, WOCHENWERTE, 0);
        }

        /// <summary>
        /// <b>Der Schreiber</b> — das Gegenstück zu <see cref="Lesen"/>: genau 168 Zellen, jede eine
        /// Zahl mit bis zu <see cref="NACHKOMMASTELLEN"/> Nachkommastellen oder
        /// <see cref="DbWerte.KOND_WOCHE_AUS"/> für <see cref="double.NaN"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Die Liste fehlt.</exception>
        /// <exception cref="ArgumentException">
        /// Nicht genau 168 Zellen, eine Zelle ist ±∞ oder liegt außerhalb der Grenzen der Größe,
        /// oder der Text wäre länger, als die Spalte trägt.
        /// </exception>
        public static string Schreiben(IReadOnlyList<double> werte, Konditionierungsgroesse groesse)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            if (werte.Count != WOCHENWERTE)
                throw new ArgumentException("Eine Standardwoche hat genau " + Zahl(WOCHENWERTE) +
                                           " Zellen, übergeben sind " + Zahl(werte.Count) + ".", nameof(werte));

            var teile = new string[WOCHENWERTE];
            for (int i = 0; i < WOCHENWERTE; i++)
            {
                double w = werte[i];
                if (double.IsNaN(w))
                {
                    teile[i] = DbWerte.KOND_WOCHE_AUS;
                    continue;
                }
                if (!Konditionierungsgroessen.ImBereich(groesse, w))
                    throw new ArgumentException("Die Zelle an Stelle " + Zahl(i + 1) + " (" +
                                               w.ToString("G17", CultureInfo.InvariantCulture) +
                                               ") liegt außerhalb der Grenzen " +
                                               Konditionierungsgroessen.Bereichstext(groesse) + ".", nameof(werte));
                teile[i] = Text(w);
            }

            string text = string.Join(TRENNZEICHEN.ToString(), teile);
            if (text.Length > KonditionierungSchema.WOCHE_MAX_ZEICHEN)
                throw new ArgumentException("Die Standardwoche braucht " + Zahl(text.Length) +
                                           " Zeichen, zulässig sind " +
                                           Zahl(KonditionierungSchema.WOCHE_MAX_ZEICHEN) + ".", nameof(werte));
            return text;
        }

        /// <summary>
        /// Eine einzelne Zelle als Text — bis zu vier Nachkommastellen, Punkt als
        /// Dezimaltrennzeichen, nie „-0“; <see cref="double.NaN"/> wird
        /// <see cref="DbWerte.KOND_WOCHE_AUS"/>.
        /// </summary>
        public static string Text(double wert)
        {
            if (double.IsNaN(wert)) return DbWerte.KOND_WOCHE_AUS;
            double gerundet = Math.Round(wert, NACHKOMMASTELLEN, MidpointRounding.AwayFromZero);
            if (gerundet == 0.0) gerundet = 0.0;      // kein "-0" im Text
            return gerundet.ToString(FORMAT, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Der Rundlauf</b> (Konzept 3.3 Regel 1): Kommt der Wert über Text und Leser
        /// <b>bitgleich</b> zurück? „aus“ (<see cref="double.IsNaN"/>) läuft immer rund; eine Zahl
        /// mit mehr als vier gültigen Nachkommastellen nicht — dann lehnt „Kalender anlegen“
        /// benannt ab, statt die Reihe um eine Rundung zu verschieben.
        /// </summary>
        public static bool Rundlauf(double wert)
        {
            if (double.IsNaN(wert)) return true;
            if (!double.IsFinite(wert)) return false;
            return double.TryParse(Text(wert), NumberStyles.Float, CultureInfo.InvariantCulture, out double zurueck)
                   && zurueck.Equals(wert);
        }

        /// <summary>
        /// Die Stelle einer Wochenstunde: Wochentag 0 = Montag … 6 = Sonntag, Stunde 0 … 23.
        /// EINE Stelle, an der die Ordnung „Tag mal 24 plus Stunde“ steht.
        /// </summary>
        public static int Stelle(int wochentag, int stundeDesTages)
            => wochentag * TAGESSTUNDEN + stundeDesTages;

        private static string Zahl(int n) => n.ToString(CultureInfo.InvariantCulture);
    }
}
