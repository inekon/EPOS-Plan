using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <c>ProjektCtrl.Steckbrief</c> gegen die Testdatenbank (Anwenderwunsch 06.10.2026:
    /// Projektdaten unter der Projektliste des Assistenten).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektSteckbriefTests
    {
        [Fact]
        public void Referenzprojekt_liefert_alle_gepflegten_Felder()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektSteckbrief s = ProjektCtrl.Steckbrief(1030);

            Assert.NotNull(s);
            Assert.Equal(1030, s.Id);
            Assert.Equal("Referenz BHKW-Kaskade (Regressionstest)", s.Name);
            Assert.StartsWith("Regressionstest: BHKW-Kaskade", s.Beschreibung);
            Assert.Equal("EPOS-Plan intern", s.Kunde);
            Assert.Equal("Regressionstest", s.Bearbeiter);
            Assert.Equal(new DateTime(2026, 8, 19), s.Erstellt);
            Assert.Equal(new DateTime(2026, 8, 19), s.Geaendert);
            Assert.False(string.IsNullOrWhiteSpace(s.Klimaregion));
            Assert.Equal("", s.StammName);
            Assert.Equal(0, s.Varianten);
            Assert.Equal(new DateTime(2026, 8, 30, 6, 11, 59), s.LetzteSimulation);
        }

        [Fact]
        public void Variante_nennt_ihren_Stamm_und_der_Stamm_zaehlt_seine_Varianten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal("Wöhler", ProjektCtrl.Steckbrief(1023)?.StammName);
            Assert.Equal(2, ProjektCtrl.Steckbrief(1019)?.Varianten);
        }

        [Fact]
        public void Nicht_gepflegte_Felder_bleiben_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // 1006: ohne Bearbeiter, Kunde und Beschreibung, ohne Simulationslauf.
            ProjektSteckbrief s = ProjektCtrl.Steckbrief(1006);

            Assert.NotNull(s);
            Assert.Equal("", s.Bearbeiter);
            Assert.Equal("", s.Kunde);
            Assert.Equal("", s.Beschreibung);
            Assert.Null(s.LetzteSimulation);
            Assert.NotNull(s.Erstellt);
        }

        [Fact]
        public void Unbekanntes_Projekt_liefert_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(ProjektCtrl.Steckbrief(0));
            Assert.Null(ProjektCtrl.Steckbrief(987654));
        }
    }
}
