using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Der Textlieferant: wie das Anwendungsprojekt die Texte des Kerns aus
    /// <c>MyResource</c> speist, ohne dass der Kern die Ressourcen kennt (Fachkonzept 3.7).
    /// </summary>
    /// <remarks>
    /// Die Tests setzen <see cref="KiTexte.Lieferant"/> und raeumen ihn danach wieder ab.
    /// Sie laufen deshalb bewusst NICHT nebenlaeufig zu anderen Textproben - xunit fuehrt
    /// die Faelle einer Klasse ohnehin nacheinander aus.
    /// </remarks>
    public class KiTexteTests : IDisposable
    {
        public void Dispose() => KiTexte.Lieferant = null;

        [Fact]
        public void OhneLieferantGiltDieDeutscheVorgabe()
        {
            KiTexte.Lieferant = null;

            Assert.Equal("Aktion", KiTexte.FeldAktion);
            Assert.Contains("liest nur", KiTexte.WirkungLesen);
        }

        [Fact]
        public void DerLieferantWirdBefragt()
        {
            var katalog = new Dictionary<string, string>
            {
                { KiTexte.Vorsatz + "FELD_AKTION", "Action" },
                { KiTexte.Vorsatz + "STUFE_LESEN", "Level 1 - read only" }
            };
            KiTexte.Lieferant = s => katalog.TryGetValue(s, out string? t) ? t : null;

            Assert.Equal("Action", KiTexte.FeldAktion);
            Assert.Equal("Level 1 - read only", KiTexte.Stufe(Schutzstufe.Lesen));
        }

        [Fact]
        public void FehltEinSchluesselGreiftDieVorgabe()
        {
            KiTexte.Lieferant = _ => null;

            Assert.Equal("Aktion", KiTexte.FeldAktion);
        }

        [Fact]
        public void EinLeererTextGiltAlsFehlend()
        {
            KiTexte.Lieferant = _ => "";

            Assert.Equal("Zweck", KiTexte.FeldZweck);
        }

        [Fact]
        public void EinWerfenderLieferantLaehmtNichts()
        {
            // Ein fehlender Text ist ein Schoenheitsfehler, kein Grund, eine Aktion
            // scheitern zu lassen.
            KiTexte.Lieferant = _ => throw new InvalidOperationException("Katalog kaputt");

            Assert.Equal("Angaben", KiTexte.FeldAngaben);
            Assert.Contains("liest nur", KiTexte.WirkungLesen);
        }

        [Fact]
        public void JederSchluesselTraegtDenVorsatz()
        {
            var gesehen = new List<string>();
            KiTexte.Lieferant = s => { gesehen.Add(s); return null; };

            _ = KiTexte.WirkungLesen;
            _ = KiTexte.AktionUnbekannt;
            _ = KiTexte.PflichtfeldFehlt;
            _ = KiTexte.RiegelZu;
            _ = KiTexte.RundendeckelErreicht;
            _ = KiTexte.MehrereWerkzeuge;
            _ = KiTexte.StufeRechnen;

            Assert.Equal(7, gesehen.Count);
            foreach (string s in gesehen) Assert.StartsWith("KI_KERN_", s);
        }

        [Fact]
        public void DerAusgetauschteTextKommtAuchImBestaetigungstextAn()
        {
            // Nachweis, dass die Umstellung von const auf Eigenschaft wirkt: der Kern liest
            // den Text bei JEDER Verwendung neu.
            KiTexte.Lieferant = s => s == KiTexte.Vorsatz + "FELD_ZWECK" ? "Purpose" : null;

            KiPruefErgebnis p = KiPruefung.PruefeJson(Registerabbild.Erzeuge(),
                                                     "projekte_auflisten", "{}");

            Assert.Contains("Purpose:", KiBestaetigung.Erzeuge(p.Aufruf!));
        }
    }
}
