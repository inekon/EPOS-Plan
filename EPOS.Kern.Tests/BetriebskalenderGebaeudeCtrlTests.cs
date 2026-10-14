using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projektgebäude als Quellen eines Betriebskalenders</b> (Konzept Kältebedarfsprofile 7.2, Welle KBP-1;
    /// <see cref="BetriebskalenderGebaeudeCtrl"/>): Feiertagsland und Ferien je Gebäude des Projekts auf einer Kopie der
    /// Testdatenbank — Grenzen 0 und 366 heißen „aus“ (eine fremde Länderkennung verbietet das Schema, der Leser ließe sie fallen), ein
    /// Projekt ohne Gebäude liefert leer. Der Leser schreibt nichts.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BetriebskalenderGebaeudeCtrlTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Ohne_Projekt_und_ohne_Gebaeude_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.Empty(BetriebskalenderGebaeudeCtrl.Quellen(0));
            Assert.Empty(BetriebskalenderGebaeudeCtrl.Quellen(1030));   // Referenzprojekt ohne Gebäude
        }

        [Fact]
        public void Ferien_und_Land_des_Gebaeudes_in_ihrer_Reihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BetriebskalenderGebaeudequelle kond = Assert.Single(BetriebskalenderGebaeudeCtrl.Quellen(1051));
            Assert.Equal("Referenzbau Konditionierung", kond.Bezeichner);
            Assert.Null(kond.Feiertagsland);
            Assert.Equal(new[] { new Ferienzeitraum(357, 6), new Ferienzeitraum(213, 226) }, kond.Ferien);

            BetriebskalenderGebaeudequelle land = Assert.Single(BetriebskalenderGebaeudeCtrl.Quellen(1067));
            Assert.Equal("BY", land.Feiertagsland);
            Assert.Empty(land.Ferien);                                   // Ferienbeginn 366 = aus
        }

        [Fact]
        public void Aus_faellt_weg_und_das_Land_kommt_als_Kennung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            long id = Assert.Single(BetriebskalenderGebaeudeCtrl.Quellen(1051)).IdGebaeude;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Feiertagsland = ?, Ferienbeginn_3 = ?, Ferienende_3 = ?, " +
                "Ferienbeginn_4 = ?, Ferienende_4 = ? WHERE ID = ?",
                new DbParam("?", "HE"), new DbParam("?", 40.0), new DbParam("?", 366.0),
                new DbParam("?", 300.0), new DbParam("?", 305.0), new DbParam("?", id)));

            BetriebskalenderGebaeudequelle q = Assert.Single(BetriebskalenderGebaeudeCtrl.Quellen(1051));
            Assert.Equal("HE", q.Feiertagsland);
            Assert.Equal(new[] { 357, 213, 300 }, q.Ferien.Select(f => f.Von));
        }
    }
}
