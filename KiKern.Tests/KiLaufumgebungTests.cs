using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// <see cref="KiLaufumgebung"/> und <see cref="KiFortschritt"/> — was eine LANG
    /// laufende Aktion ueber ihren Lauf hinaus braucht (Etappe S3, Auftrag #201;
    /// geprueft mit Auftrag #214).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum diese Faelle erst mit #214 entstehen.</b> Bis dahin war die Umgebung nur
    /// ANGEBOTEN: Der Kern reichte sie in die drei Rechenaktionen hinein, aber niemand
    /// belegte die Senke, und niemand setzte die Marke. Mit #214 haengt der Chat beide
    /// Enden an — und damit sind ihre Zusagen Zusagen, die jemand einloest. Sie gehoeren
    /// deshalb festgehalten, und zwar HIER: Die zwei Klassen liegen in
    /// <c>KiKern</c> und kennen weder Datenbank noch Oberflaeche.
    /// </para>
    /// <para>
    /// <b>Die Klasse pinnt die Kultur nicht</b> — sie prueft keinen formatierten Text;
    /// die eine Zahl in <see cref="KiFortschritt.ToString"/> ist ein ganzzahliger
    /// Prozentwert und traegt kein Trennzeichen.
    /// </para>
    /// </remarks>
    public class KiLaufumgebungTests
    {
        // ==================================================================
        //  Der Schritt
        // ==================================================================

        /// <summary>
        /// Ein Anteil ausserhalb 0…1 wird GEKLEMMT statt abgewiesen: Eine Meldung
        /// „110 %" waere Unfug, ein geworfener Fehler im Fortschritt waere schlimmer.
        /// </summary>
        [Theory]
        [InlineData(-0.5, 0.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(0.25, 0.25)]
        [InlineData(1.0, 1.0)]
        [InlineData(1.5, 1.0)]
        public void Der_Anteil_wird_auf_null_bis_eins_geklemmt(double eingabe, double erwartet)
        {
            var schritt = new KiFortschritt(eingabe, "text");

            Assert.True(schritt.Anteil.HasValue);
            Assert.Equal(erwartet, schritt.Anteil.Value, 12);
        }

        /// <summary>
        /// <b>Unbestimmt ist ein ZUSTAND, kein Mangel</b> (dieselbe Regel wie im
        /// Baustein <c>Fortschritt</c>): <c>null</c> bleibt <c>null</c> und wird nicht
        /// zu 0 — eine Bisektion weiss nicht, wie viele Schritte sie noch braucht.
        /// </summary>
        [Fact]
        public void Ein_unbestimmter_Anteil_bleibt_unbestimmt()
        {
            Assert.False(new KiFortschritt(null, "text").Anteil.HasValue);
        }

        /// <summary>Ein fehlender Text ist leer, nie <c>null</c>.</summary>
        [Fact]
        public void Ein_fehlender_Text_ist_leer()
        {
            Assert.Equal("", new KiFortschritt(0.5, null).Text);
        }

        /// <summary>
        /// Die Kurzfassung nennt den Prozentwert nur, wenn es einen gibt.
        /// </summary>
        [Fact]
        public void Die_Kurzfassung_nennt_den_Prozentwert_nur_wenn_es_einen_gibt()
        {
            Assert.Equal("50 % rechnet", new KiFortschritt(0.5, "rechnet").ToString());
            Assert.Equal("rechnet", new KiFortschritt(null, "rechnet").ToString());
        }

        // ==================================================================
        //  Die Umgebung
        // ==================================================================

        /// <summary>Gemeldet wird, was gemeldet wurde — Anteil und Text unveraendert.</summary>
        [Fact]
        public void Ein_gemeldeter_Schritt_erreicht_den_Empfaenger()
        {
            var gesammelt = new List<KiFortschritt>();
            var umgebung = new KiLaufumgebung(new Senke(gesammelt), CancellationToken.None);

            umgebung.Melde(0.25, "Die Kaskade rechnet");

            Assert.Single(gesammelt);
            Assert.Equal(0.25, gesammelt[0].Anteil!.Value, 12);
            Assert.Equal("Die Kaskade rechnet", gesammelt[0].Text);
        }

        /// <summary>
        /// <b>Ohne Empfaenger geschieht nichts.</b> Eine Aktion muss sich nicht darum
        /// kuemmern, ob gerade jemand zuhoert — genau deshalb gibt es
        /// <see cref="KiLaufumgebung.Leer"/>.
        /// </summary>
        [Fact]
        public void Ohne_Empfaenger_geschieht_nichts()
        {
            KiLaufumgebung.Leer.Melde(0.5, "text");
            KiLaufumgebung.Leer.AbbruchPruefen();

            Assert.Null(KiLaufumgebung.Leer.Fortschritt);
            Assert.False(KiLaufumgebung.Leer.Abbruch.CanBeCanceled);
        }

        /// <summary>
        /// <b>Ein Fortschrittsfehler darf keinen Lauf kippen.</b> Der Empfaenger ist die
        /// Oberflaeche; wenn sie stolpert, rechnet der Kern trotzdem zu Ende.
        /// </summary>
        [Fact]
        public void Ein_werfender_Empfaenger_kippt_den_Lauf_nicht()
        {
            var umgebung = new KiLaufumgebung(new Stolpersenke(), CancellationToken.None);

            umgebung.Melde(0.5, "text");
        }

        // ==================================================================
        //  Die Abbruchmarke
        // ==================================================================

        /// <summary>Ohne Abbruch geht die Pruefung durch.</summary>
        [Fact]
        public void Ohne_Abbruch_geht_die_Pruefung_durch()
        {
            using var quelle = new CancellationTokenSource();

            new KiLaufumgebung(null, quelle.Token).AbbruchPruefen();
        }

        /// <summary>
        /// Mit gesetzter Marke wirft die Pruefung — das ist der Punkt zwischen zwei
        /// Phasen, an dem ein Lauf ohne halben Zustand aussteigt.
        /// </summary>
        [Fact]
        public void Mit_gesetzter_Marke_wirft_die_Pruefung()
        {
            using var quelle = new CancellationTokenSource();
            var umgebung = new KiLaufumgebung(null, quelle.Token);

            quelle.Cancel();

            Assert.True(umgebung.Abbruch.IsCancellationRequested);
            Assert.Throws<OperationCanceledException>(() => umgebung.AbbruchPruefen());
        }

        /// <summary>
        /// Die Marke wirkt auch, wenn sie ERST WAEHREND des Laufs gesetzt wird — der
        /// Anwender drueckt „Abbrechen", waehrend gerechnet wird.
        /// </summary>
        [Fact]
        public void Eine_waehrend_des_Laufs_gesetzte_Marke_wirkt()
        {
            using var quelle = new CancellationTokenSource();
            var umgebung = new KiLaufumgebung(null, quelle.Token);
            var schritte = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                if (i == 2) quelle.Cancel();

                try { umgebung.AbbruchPruefen(); }
                catch (OperationCanceledException) { break; }

                schritte.Add(i);
            }

            Assert.Equal(new[] { 0, 1 }, schritte);
        }

        // ==================================================================
        //  Pruefsenken
        // ==================================================================

        private sealed class Senke : IProgress<KiFortschritt>
        {
            private readonly List<KiFortschritt> _ziel;
            internal Senke(List<KiFortschritt> ziel) { _ziel = ziel; }
            public void Report(KiFortschritt wert) { _ziel.Add(wert); }
        }

        private sealed class Stolpersenke : IProgress<KiFortschritt>
        {
            public void Report(KiFortschritt wert)
                => throw new InvalidOperationException("Die Anzeige ist weg.");
        }
    }
}
