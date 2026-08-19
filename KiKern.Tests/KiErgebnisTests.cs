using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Ergebnisobjekt (Fachkonzept 3.6): Status, Nutzdaten, still gesammelte Meldungen.
    /// </summary>
    public class KiErgebnisTests
    {
        private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Zeilen(int n)
        {
            var liste = new List<IReadOnlyDictionary<string, object?>>();
            for (int i = 0; i < n; i++)
                liste.Add(new Dictionary<string, object?> { ["id"] = 1000 + i });
            return liste;
        }

        [Fact]
        public void Ok_IstErfolgreichUndZaehltSeineZeilen()
        {
            KiErgebnis e = KiErgebnis.Ok("3 Projekte", Zeilen(3));

            Assert.True(e.Erfolg);
            Assert.Equal(KiStatus.Ausgefuehrt, e.Status);
            Assert.Equal(3, e.Anzahl);
            Assert.Equal(3, e.Zeilen.Count);
        }

        [Fact]
        public void OkOhneZeilen_HatAnzahlNull()
        {
            KiErgebnis e = KiErgebnis.Ok("nichts gefunden");

            Assert.True(e.Erfolg);
            Assert.Equal(0, e.Anzahl);
            Assert.Empty(e.Zeilen);
        }

        [Fact]
        public void AusdrueckicheAnzahl_SchlaegtDieZeilenzahl()
        {
            // Etwa bei „1 Ergebnis, 4 Kennzahlen" - die Zeilenzahl waere irrefuehrend.
            KiErgebnis e = KiErgebnis.Ok("1 Lauf", Zeilen(4), anzahl: 1);

            Assert.Equal(1, e.Anzahl);
            Assert.Equal(4, e.Zeilen.Count);
        }

        [Theory]
        [InlineData(KiStatus.Abgelehnt)]
        [InlineData(KiStatus.Abgebrochen)]
        [InlineData(KiStatus.Fehlgeschlagen)]
        public void NichtausgefuehrteErgebnisse_SindNichtErfolgreich(KiStatus status)
        {
            KiErgebnis e = status switch
            {
                KiStatus.Abgelehnt => KiErgebnis.Abgelehnt("Vorbedingung"),
                KiStatus.Abgebrochen => KiErgebnis.Abgebrochen("abgebrochen"),
                _ => KiErgebnis.Fehlgeschlagen("Fehler")
            };

            Assert.False(e.Erfolg);
            Assert.Equal(status, e.Status);
        }

        [Fact]
        public void StilleFehler_WerdenUebernommen_LeereVerworfen()
        {
            KiErgebnis e = KiErgebnis.Ok("ok").MitMeldungen(new[] { " Tabelle fehlt ", "", null!, "   " });

            Assert.Single(e.Meldungen);
            Assert.Equal("Tabelle fehlt", e.Meldungen[0]);
        }

        [Fact]
        public void MeldungenOhneQuelle_AendernNichts()
        {
            Assert.Empty(KiErgebnis.Ok("ok").MitMeldungen(null).Meldungen);
        }

        [Fact]
        public void Kurzfassung_NenntZahlUndText()
        {
            Assert.Equal("3x; 3 Projekte", KiErgebnis.Ok("3 Projekte", Zeilen(3)).Kurzfassung());
        }

        [Fact]
        public void KurzfassungOhneNutzdaten_IstNurDerText()
        {
            Assert.Equal("Vorbedingung nicht erfüllt",
                         KiErgebnis.Abgelehnt("Vorbedingung nicht erfüllt").Kurzfassung());
        }

        [Fact]
        public void Dauer_LaesstSichNachtraeglichSetzen()
        {
            KiErgebnis e = KiErgebnis.Ok("ok").MitDauer(TimeSpan.FromMilliseconds(42));

            Assert.Equal(42, e.Dauer.TotalMilliseconds, 3);
        }
    }
}
