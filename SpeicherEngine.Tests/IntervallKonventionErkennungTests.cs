using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// <b>Die EINE Erkennungsregel</b> (Entscheid KI‑D‑Q9, 21.09.2026): Sie lag als
    /// privater Zweig in <c>GanglinienPruefung.KonventionAnwenden</c> und ist als
    /// <see cref="IntervallKonventionErkennung"/> herausgezogen, damit der CSV-Import
    /// der Speicher-Zeitreihen dieselbe Entscheidung trifft statt einer zweiten
    /// Abschrift. Der Wortlaut der Regel: <i>eine Reihe mit Intervallende beginnt
    /// genau ein Intervall nach Mitternacht des 01.01.</i>
    /// </summary>
    public sealed class IntervallKonventionErkennungTests
    {
        [Theory]
        [InlineData(15)]
        [InlineData(60)]
        public void Genau_ein_Intervall_nach_Neujahrsmitternacht_ist_Intervallende(int schritt)
        {
            DateTime erster = new DateTime(2026, 1, 1).AddMinutes(schritt);

            Assert.Equal(IntervallKonvention.Ende,
                IntervallKonventionErkennung.Erkenne(erster, schritt));
        }

        [Theory]
        [InlineData(2026, 1, 1, 0, 0, 15)]     // Mitternacht selbst
        [InlineData(2026, 1, 1, 0, 30, 15)]    // zwei Intervalle nach Mitternacht
        [InlineData(2026, 1, 1, 0, 15, 60)]    // 00:15, aber Stundenraster
        [InlineData(2026, 2, 1, 0, 15, 15)]    // richtige Uhrzeit, falscher Monat
        [InlineData(2026, 1, 2, 0, 15, 15)]    // richtige Uhrzeit, falscher Tag
        public void Alles_andere_ist_Intervallanfang(
            int jahr, int monat, int tag, int stunde, int minute, int schritt)
        {
            DateTime erster = new DateTime(jahr, monat, tag, stunde, minute, 0);

            Assert.Equal(IntervallKonvention.Anfang,
                IntervallKonventionErkennung.Erkenne(erster, schritt));
        }

        /// <summary>
        /// <b>Aufloesen entscheidet nur ueber „Automatisch".</b> Eine ausdrueckliche
        /// Wahl bleibt stehen, auch wenn die Reihe anders aussieht — sonst waere die
        /// Klappliste eine Empfehlung und keine Einstellung.
        /// </summary>
        [Fact]
        public void Eine_ausdrueckliche_Wahl_bleibt_stehen()
        {
            DateTime wieEnde = new DateTime(2026, 1, 1, 0, 15, 0);
            DateTime wieAnfang = new DateTime(2026, 1, 1, 0, 0, 0);

            Assert.Equal(IntervallKonvention.Anfang,
                IntervallKonventionErkennung.Aufloesen(IntervallKonvention.Anfang, wieEnde, 15));
            Assert.Equal(IntervallKonvention.Ende,
                IntervallKonventionErkennung.Aufloesen(IntervallKonvention.Ende, wieAnfang, 15));

            Assert.Equal(IntervallKonvention.Ende,
                IntervallKonventionErkennung.Aufloesen(IntervallKonvention.Automatisch, wieEnde, 15));
            Assert.Equal(IntervallKonvention.Anfang,
                IntervallKonventionErkennung.Aufloesen(IntervallKonvention.Automatisch, wieAnfang, 15));
        }
    }
}
