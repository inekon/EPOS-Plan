using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die Feldsicherung (Fachkonzept 11.5): an, solange niemand sie beim Start
    /// abgeschaltet hat - und danach kein Weg zurueck.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum der ganze Lebenslauf in EINEM Fall steht.</b> Der Schalter ist bewusst
    /// prozessweit und bewusst einmalig; er laesst sich also nicht je Testfall
    /// zuruecksetzen. Ein zweiter Fall, der „Standard ist an" prueft, wuerde je nach
    /// Reihenfolge auf einen bereits abgeschalteten Schalter treffen und waere damit ein
    /// Schoenwetterfall. Genau deshalb pruefen wir Ausgangszustand, Abschalten,
    /// Einmaligkeit und die abgeleiteten Texte in einem Zug - und alle uebrigen Faelle
    /// dieser Klasse kommen ohne den Zustand aus.
    /// </para>
    /// <para>
    /// Die Testfaelle des Projekts laufen ohnehin nacheinander (<c>Testlauf.cs</c>).
    /// </para>
    /// </remarks>
    public class KiFeldsicherungTests : IDisposable
    {
        public void Dispose() => KiTexte.Lieferant = null;

        /// <summary>Eine Formularaktion (Stufe 2 mit Kennzeichen 2F).</summary>
        private static KiAktion Formularaktion()
            => new KiAktion("feld_setzen", "Trägt einen Wert ein.", Schutzstufe.Schreiben,
                            "KiDialogZugriff.Setze",
                            vorschau: _ => "Wartungskosten · 850 → 1200",
                            formularaktion: true);

        /// <summary>Eine gewoehnliche Schreibaktion - sie darf der Schalter nie erreichen.</summary>
        private static KiAktion Schreibaktion()
            => new KiAktion("kostenposition_setzen", "Setzt eine Kostenposition.",
                            Schutzstufe.Schreiben, "KostenCtrl.Update",
                            vorschau: _ => "Ich würde eine Kostenposition setzen.");

        [Fact]
        public void DerLebenslauf_AnAbschaltenEinmaligUndKeinWegZurueck()
        {
            KiAktion feldsetzen = Formularaktion();
            KiAktion kosten = Schreibaktion();

            Assert.True(KiFeldsicherung.Aktiv);
            Assert.Equal("", KiFeldsicherung.Grund);
            Assert.Equal("", KiFeldsicherung.Chathinweis());
            Assert.Equal("", KiFeldsicherung.Protokollvermerk());

            // VORHER: beide brauchen die Bestaetigung.
            Assert.True(KiBestaetigungspflicht.Gilt(feldsetzen));
            Assert.True(KiBestaetigungspflicht.Gilt(kosten));

            Assert.True(KiFeldsicherung.Abschalten("  Befehlszeilenschalter /ki-feldsicherung-aus  "));

            Assert.False(KiFeldsicherung.Aktiv);
            Assert.Equal("Befehlszeilenschalter /ki-feldsicherung-aus", KiFeldsicherung.Grund);
            Assert.Equal(KiTexte.FeldsicherungAus, KiFeldsicherung.Chathinweis());
            Assert.Equal(KiTexte.FeldsicherungVermerk, KiFeldsicherung.Protokollvermerk());

            // NACHHER: GENAU die Feldbestaetigung entfaellt - und nichts sonst. Das ist
            // der Nachweis zu Abnahmepunkt 6 des Umsetzungskonzepts: Die Stufe-2-
            // Bestaetigung einer DB-Schreibaktion erscheint weiterhin.
            Assert.False(KiBestaetigungspflicht.Gilt(feldsetzen));
            Assert.True(KiBestaetigungspflicht.Gilt(kosten));

            // Der Riegel selbst bleibt davon unberuehrt: Er sagt fuer BEIDE weiterhin
            // „bestaetigungspflichtig". Abgeschaltet ist eine Schicht darueber, nicht er.
            Assert.True(KiRiegel.BrauchtBestaetigung(feldsetzen));
            Assert.True(KiRiegel.BrauchtBestaetigung(kosten));

            // Zweiter Versuch: kein Erfolg, und der ERSTE Grund bleibt stehen.
            Assert.False(KiFeldsicherung.Abschalten("Anderer Weg"));
            Assert.Equal("Befehlszeilenschalter /ki-feldsicherung-aus", KiFeldsicherung.Grund);
            Assert.False(KiFeldsicherung.Aktiv);

            // Es gibt keinen Weg zurueck: Die Klasse bietet ueberhaupt keine Methode, die
            // wieder einschaltet - der Schalter ist ein Startzustand, kein Betriebsmodus.
            Assert.DoesNotContain("Einschalten", Namen());
            Assert.DoesNotContain("Anschalten", Namen());
            Assert.DoesNotContain("Zuruecksetzen", Namen());
        }

        [Fact]
        public void AbschaltenOhneGrund_IstEinProgrammierfehler()
        {
            // Die Argumentpruefung steht VOR dem Zustand; der Fall gilt deshalb
            // unabhaengig davon, ob die Sicherung in diesem Lauf schon abgeschaltet wurde.
            Assert.Throws<ArgumentException>(() => KiFeldsicherung.Abschalten(null!));
            Assert.Throws<ArgumentException>(() => KiFeldsicherung.Abschalten(""));
            Assert.Throws<ArgumentException>(() => KiFeldsicherung.Abschalten("   "));
        }

        [Fact]
        public void DerChathinweisSagtAuchWasWEITERGilt()
        {
            // „Feldsicherung AUS" allein liesse sich als „gar keine Bestaetigung mehr"
            // lesen - die Stufe-2-Bestaetigung bleibt aber bestehen (Fachkonzept 11.5).
            Assert.Contains("Feldsicherung AUS", KiTexte.FeldsicherungAus);
            Assert.Contains("Bestätigung", KiTexte.FeldsicherungAus);
            Assert.NotEqual("", KiTexte.FeldsicherungVermerk);
        }

        [Fact]
        public void DerProtokollvermerkPasstInEineProtokollzeile()
        {
            // Er geht in das Ergebnisfeld einer einzeiligen Protokollzeile (Fachkonzept 3.6).
            Assert.DoesNotContain("\n", KiTexte.FeldsicherungVermerk);
            Assert.DoesNotContain(KiProtokoll.Trenner, KiTexte.FeldsicherungVermerk);
        }

        [Fact]
        public void DieTexteKommenAusDemTextlieferanten()
        {
            var katalog = new Dictionary<string, string>
            {
                { KiTexte.Vorsatz + "FELDSICHERUNG_AUS", "Field guard OFF" },
                { KiTexte.Vorsatz + "FELDSICHERUNG_VERMERK", "field guard off" }
            };
            KiTexte.Lieferant = s => katalog.TryGetValue(s, out string? t) ? t : null;

            Assert.Equal("Field guard OFF", KiTexte.FeldsicherungAus);
            Assert.Equal("field guard off", KiTexte.FeldsicherungVermerk);
        }

        private static List<string> Namen()
        {
            var namen = new List<string>();
            foreach (var m in typeof(KiFeldsicherung).GetMethods()) namen.Add(m.Name);
            return namen;
        }
    }
}
