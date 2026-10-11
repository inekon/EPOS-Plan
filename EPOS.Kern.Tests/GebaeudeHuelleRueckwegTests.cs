using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Gebäude-Rückwegs</b> (Regel 5.2, Konzept Katalogauswahl 5.14): die Gaben <c>RueckwegVorschau</c> und
    /// <c>Rueckweg</c> der <see cref="GebaeudeHuelle"/> und der rückwärtskompatible Weg <c>InDbUebernehmen</c> (= „neu"), der
    /// jetzt den Ursprung am Projektgebäude setzt. Arbeitskopie der Testdatenbank; ohne sie schweigt der Fall.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeHuelleRueckwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1052;

        [Fact]
        public void Die_Gaben_reichen_Vorschau_und_Rueckweg_und_InDbUebernehmen_setzt_den_Ursprung()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyDictionary<string, object> gaben =
                GebaeudeHuelle.Gaben(PROJEKT, "", Z_ProjGebCtrl.LiesProjekt(PROJEKT), false);
            var vorschau = Assert.IsType<Func<GebaeudeProjektZeile, GebaeudeRueckwegVorschau>>(gaben["RueckwegVorschau"]);
            var rueckweg = Assert.IsType<Func<GebaeudeProjektZeile, bool, string, GebaeudeDbUebernahme>>(gaben["Rueckweg"]);
            var inDb = Assert.IsType<Func<GebaeudeProjektZeile, string, GebaeudeDbUebernahme>>(gaben["InDbUebernehmen"]);

            int idZ = (int)Zahl("SELECT MIN(\"ID_ProjektGebaeude\") FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" = " + PROJEKT);
            var zeile = new GebaeudeProjektZeile { IdZ = idZ, HatProjektkopie = true };
            long ursprung = Zahl("SELECT \"ID_Gebaeude_Stamm\" FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = " + idZ);

            Assert.Null(vorschau(new GebaeudeProjektZeile { IdZ = idZ, HatProjektkopie = false }));
            GebaeudeRueckwegVorschau v = vorschau(zeile);
            Assert.Equal((int?)ursprung, v.IdUrsprung);
            Assert.True(v.KannUeberschreiben);

            // Überschreiben: der Name des Ursprungs, die Statuszeile nennt die Zonen.
            GebaeudeDbUebernahme ueber = rueckweg(zeile, true, "egal");
            Assert.True(ueber.Ok, ueber.Meldung);
            Assert.Equal(v.NameUrsprung, ueber.Name);
            Assert.Contains("3 Zonen", ueber.Meldung);

            // Ein vergebener Name steht am Namensfeld.
            GebaeudeDbUebernahme belegt = rueckweg(zeile, false, v.NameUrsprung);
            Assert.False(belegt.Ok);
            Assert.True(belegt.AmNamen);

            // Der heutige Aufruf (= neu) legt an und setzt den Ursprung auf den neuen Satz.
            GebaeudeDbUebernahme neu = inDb(zeile, "Hüllenprobe Rückweg");
            Assert.True(neu.Ok, neu.Meldung);
            Assert.Equal("Hüllenprobe Rückweg", neu.Name);
            long idNeu = Zahl("SELECT \"ID\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"Bezeichner\" = 'Hüllenprobe Rückweg'");
            Assert.True(idNeu > 0);
            Assert.Equal(idNeu, Zahl("SELECT \"ID_Gebaeude_Stamm\" FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = " + idZ));
            Assert.NotEqual(ursprung, idNeu);
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
