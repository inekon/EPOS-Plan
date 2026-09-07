using System;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Ausdruck EINER Zahlenspalte</b> (Anwenderentscheid <b>W14a-E-10</b> vom
    /// 07.09.2026, Frage <b>Q1 = ja</b>) — <see cref="Zahlenausdruck"/>.
    ///
    /// <para>Die Klasse ist der Grund, warum jedes Filter-Popover gleich aussehen kann:
    /// EIN Feld, egal ob Text- oder Zahlenspalte. Sie ist ohne Oberflaeche pruefbar —
    /// genau dafuer steht sie im Kern (Konzept_Katalogfilter 5.6.7).</para>
    ///
    /// <para><b>Die Kultur wird gepinnt</b> (Hausregel seit iU9-W8): Das
    /// Dezimaltrennzeichen ist das der Kultur, und beide Faelle — de-DE mit Komma,
    /// en-US mit Punkt — muessen dasselbe bedeuten.</para>
    /// </summary>
    public class ZahlenausdruckTests : IDisposable
    {
        private static readonly CultureInfo DE = new CultureInfo("de-DE");
        private static readonly CultureInfo EN = new CultureInfo("en-US");

        private readonly CultureInfo _vorher = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.DefaultThreadCurrentUICulture;

        /// <summary>
        /// Die Kultur ist PROZESSWEIT; sie wird nach der Klasse zurueckgestellt, damit
        /// der en-US-Fall keinem folgenden Test das Dezimaltrennzeichen wegnimmt.
        /// </summary>
        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _vorher;
            CultureInfo.DefaultThreadCurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher ?? CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = _vorherUi ?? CultureInfo.InvariantCulture;
        }

        // =================================================================
        //  Die SIEBEN Formen aus 5.6.3
        // =================================================================

        /// <summary>
        /// <c>&gt;10</c> — echt groesser. Die 10 selbst faellt heraus, 10,000000001
        /// ebenfalls (Zahlenrand): Der Rand macht aus „groesser" kein „groesser gleich",
        /// er faengt nur die Rechenungenauigkeit einer Division ab.
        /// </summary>
        [Fact]
        public void Groesser()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen(">10", DE);
            Assert.NotNull(b);
            Assert.Equal(Zahlenvergleich.Groesser, b.Vergleich);
            Assert.True(b.Trifft(10.5));
            Assert.False(b.Trifft(10));
            Assert.False(b.Trifft(9.9));
        }

        /// <summary><c>&gt;=10</c> — die 10 gehoert dazu.</summary>
        [Fact]
        public void GroesserGleich()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen(">=10", DE);
            Assert.Equal(Zahlenvergleich.GroesserGleich, b.Vergleich);
            Assert.True(b.Trifft(10));
            Assert.True(b.Trifft(10.5));
            Assert.False(b.Trifft(9.9));
        }

        /// <summary><c>&lt;60</c> — echt kleiner.</summary>
        [Fact]
        public void Kleiner()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen("<60", DE);
            Assert.Equal(Zahlenvergleich.Kleiner, b.Vergleich);
            Assert.True(b.Trifft(59.9));
            Assert.False(b.Trifft(60));
        }

        /// <summary><c>&lt;=60</c> — die 60 gehoert dazu.</summary>
        [Fact]
        public void KleinerGleich()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen("<=60", DE);
            Assert.Equal(Zahlenvergleich.KleinerGleich, b.Vergleich);
            Assert.True(b.Trifft(60));
            Assert.False(b.Trifft(60.1));
        }

        /// <summary><c>=15</c> — Gleichheit.</summary>
        [Fact]
        public void Gleich()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen("=15", DE);
            Assert.Equal(Zahlenvergleich.Gleich, b.Vergleich);
            Assert.True(b.Trifft(15));
            Assert.False(b.Trifft(15.1));
        }

        /// <summary>
        /// <c>10..60</c> — beide Grenzen eingeschlossen; verdrehte Grenzen werden
        /// getauscht statt abgelehnt (die Absicht ist eindeutig, und eine leere Liste
        /// waere die schlechtere Antwort).
        /// </summary>
        [Fact]
        public void Bereich()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen("10..60", DE);
            Assert.Equal(Zahlenvergleich.Bereich, b.Vergleich);
            Assert.True(b.Trifft(10));
            Assert.True(b.Trifft(35));
            Assert.True(b.Trifft(60));
            Assert.False(b.Trifft(9.9));
            Assert.False(b.Trifft(60.1));

            Zahlenbedingung verdreht = Zahlenausdruck.Lesen("60..10", DE);
            Assert.True(verdreht.Trifft(35));
        }

        /// <summary>
        /// <b>Die blosse Zahl ist die GLEICHHEIT</b> — der Entscheid W14a-E-10-Q1:
        /// „15" = „=15", nicht „&gt;=15". Sie ist die einzige Lesart, die bei einer
        /// Textspalte dasselbe bedeutet.
        /// </summary>
        [Fact]
        public void BlosseZahl_ist_die_Gleichheit()
        {
            Zahlenbedingung b = Zahlenausdruck.Lesen("15", DE);
            Assert.Equal(Zahlenvergleich.Gleich, b.Vergleich);
            Assert.True(b.Trifft(15));
            Assert.False(b.Trifft(16));
        }

        // =================================================================
        //  Zwei UNVERSTANDENE Ausdruecke
        // =================================================================

        /// <summary>
        /// <b>Ein unverstandener Ausdruck ist KEIN Filter</b> — dieselbe Regel wie beim
        /// kaputten Suchmuster (Konzept 3.3): Der Anwender tippt gerade, und ein halb
        /// geschriebener Bereich darf die Liste nicht leeren.
        /// </summary>
        [Fact]
        public void Unverstanden_ist_kein_Filter()
        {
            Assert.Null(Zahlenausdruck.Lesen("abc", DE));      // gar keine Zahl
            Assert.Null(Zahlenausdruck.Lesen("10..", DE));     // halb geschriebener Bereich
            Assert.Null(Zahlenausdruck.Lesen(">", DE));        // Zeichen ohne Zahl
            Assert.Null(Zahlenausdruck.Lesen("", DE));         // leer

            Assert.False(Zahlenausdruck.Verstanden("abc", DE));
            Assert.False(Zahlenausdruck.Verstanden("10..", DE));
            Assert.True(Zahlenausdruck.Verstanden("", DE));    // leer = kein Filter, kein Fehler
            Assert.True(Zahlenausdruck.Verstanden("10..60", DE));
        }

        // =================================================================
        //  Die Kultur
        // =================================================================

        /// <summary>
        /// <b>de-DE:</b> Das Komma trennt die Nachkommastellen. Der Punkt ist KEIN
        /// Tausenderpunkt — sonst waere „1.5" die Zahl 15 und der Anwender bekaeme
        /// eine andere Liste, als er meinte.
        /// </summary>
        [Fact]
        public void Kultur_de_DE()
        {
            Kultur(DE);

            Assert.True(Zahlenausdruck.Lesen(">=0,95", DE).Trifft(0.97));
            Assert.False(Zahlenausdruck.Lesen(">=0,95", DE).Trifft(0.94));

            Zahlenbedingung bereich = Zahlenausdruck.Lesen("1,5..2,5", DE);
            Assert.True(bereich.Trifft(2.0));
            Assert.False(bereich.Trifft(3.0));

            Assert.Null(Zahlenausdruck.Lesen("1.5", DE));      // Punkt ist kein Trennzeichen
        }

        /// <summary>
        /// <b>en-US:</b> Der Punkt trennt. Der BEREICHSPUNKT wird vorher abgespalten,
        /// damit <c>1.5..2.5</c> hier dasselbe bedeutet wie <c>1,5..2,5</c> in de-DE.
        /// </summary>
        [Fact]
        public void Kultur_en_US()
        {
            Kultur(EN);

            Assert.True(Zahlenausdruck.Lesen(">=0.95", EN).Trifft(0.97));
            Assert.False(Zahlenausdruck.Lesen(">=0.95", EN).Trifft(0.94));

            Zahlenbedingung bereich = Zahlenausdruck.Lesen("1.5..2.5", EN);
            Assert.True(bereich.Trifft(2.0));
            Assert.False(bereich.Trifft(3.0));

            Assert.Null(Zahlenausdruck.Lesen("1,5", EN));
        }

        /// <summary>
        /// Ein Wert, den es NICHT gibt (Halbgeviertstrich in der Spalte), trifft nie —
        /// wer nach „&gt;10" fragt, meint keine Leerstelle.
        /// </summary>
        [Fact]
        public void Leerwert_trifft_nie()
        {
            Assert.False(Zahlenausdruck.Lesen(">10", DE).Trifft(null));
            Assert.False(Zahlenausdruck.Lesen("<10", DE).Trifft(null));
            Assert.False(Zahlenausdruck.Lesen("10..60", DE).Trifft(null));
        }

        /// <summary>
        /// Der ZAHLENRAND: Eine C-Rate von 0,49999999999999994 (Leistung/Energie) ist
        /// die 0,5, nach der der Anwender fragt.
        /// </summary>
        [Fact]
        public void Gleichheit_haelt_den_Rechenrand_aus()
        {
            Assert.True(Zahlenausdruck.Lesen("=0,5", DE).Trifft(0.49999999999999994));
            Assert.True(Zahlenausdruck.Lesen("0,5", DE).Trifft(0.5000000000000001));
            Assert.False(Zahlenausdruck.Lesen("=0,5", DE).Trifft(0.51));
        }

        private static void Kultur(CultureInfo k)
        {
            CultureInfo.DefaultThreadCurrentCulture = k;
            CultureInfo.DefaultThreadCurrentUICulture = k;
            Thread.CurrentThread.CurrentCulture = k;
            Thread.CurrentThread.CurrentUICulture = k;
        }
    }
}
