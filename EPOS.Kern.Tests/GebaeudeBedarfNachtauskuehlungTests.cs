using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nachtauskühlstunden in der Bedarfsauskunft</b> (Entwurf KP2, Welle K4; Konzept
    /// Konditionierungsprofile 3.7, E54): <c>Nachtauskuehlstunden_H</c> aus dem Gebäudeergebnis des
    /// Laufs (Schritt 152) wird über <c>GebaeudeBedarfCtrl</c>, die Hülle und das DTO getragen — eine
    /// sichtbare Zeile baut erst U1.
    ///
    /// <para><b>Die Probe ist der Lauf selbst</b> (Kern-Regel „Eine Auskunft ruft den Rechenweg des
    /// Laufs"): In der Arbeitskopie bekommt das eine Gebäude des Projekts 1007 die getrennte
    /// Luftwechselangabe und eine Nachtzeile der Lüftung (3 1/h); der Lauf schreibt die Zahl nach
    /// <c>Tab_ErgebnisGebaeude</c>, und die Auskunft muss dieselbe tragen — ohne Nachtauskühlung
    /// beide <c>null</c>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeBedarfNachtauskuehlungTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeBedarfNachtauskuehlungTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _db.Dispose();

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        /// <summary>Die Nachtauskühlung am Projektgebäude: getrennte Angabe 0,2 + 0,4 1/h, Nachtzeile der Lüftung 3 1/h.</summary>
        private static void NachtauskuehlungSetzen(int idGebaeude)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Gebaeude\" SET \"" + GebaeudeSchema.SPALTE_LUFTWECHSEL_INFILTRATION + "\" = ?, \"" +
                GebaeudeSchema.SPALTE_LUFTWECHSEL_NUTZER + "\" = ? WHERE \"ID\" = ?",
                new DbParam("@i", 0.2), new DbParam("@n", 0.4), new DbParam("@g", idGebaeude)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE + "\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\") " +
                "VALUES (?, ?, ?, ?, 0)",
                new DbParam("@g", idGebaeude), new DbParam("@gr", DbWerte.KOND_GROESSE_LUEFTUNG),
                new DbParam("@z", DbWerte.KOND_ZEILE_NACHT), new DbParam("@w", 3.0)));
        }

        /// <summary>Das DTO des Bedarfsdialogs zur einzigen Projektzeile.</summary>
        private static GebaeudeBedarfDaten Dialogdaten()
        {
            IReadOnlyDictionary<string, object> liste =
                GebaeudeHuelle.Gaben(PROJEKT, "", Z_ProjGebCtrl.LiesProjekt(PROJEKT), wizard: false);
            GebaeudeProjektZeile zeile = ((List<GebaeudeProjektZeile>)liste["Zeilen"])[0];
            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(zeile, PROJEKT);
            Assert.NotNull(gaben);
            return (GebaeudeBedarfDaten)gaben["Daten"];
        }

        /// <summary>
        /// <b>Die Zahl des Laufs:</b> Controller, Hülle und DTO tragen genau die Nachtauskühlstunden,
        /// die der Lauf in <c>Tab_ErgebnisGebaeude</c> schreibt.
        /// </summary>
        [Fact]
        public void Die_Bedarfsauskunft_traegt_die_Nachtauskuehlstunden_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            Z_ProjGebModel z = Z_ProjGebCtrl.LiesProjekt(PROJEKT).Single();
            NachtauskuehlungSetzen(z.ID_Gebaeude);

            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            int? lauf = new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single().NachtauskuehlstundenH;
            Assert.NotNull(lauf);
            _ausgabe.WriteLine("Projekt 1007 mit Nachtauskühlung: " + lauf + " Nachtauskühlstunden im Lauf.");

            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, Klimaregion(PROJEKT), z.ID_Z);
            Assert.True(e.Erfolgreich, e.Befund);
            Assert.Equal(lauf, e.NachtauskuehlstundenH);
            Assert.Equal(lauf, Dialogdaten().NachtauskuehlstundenH);
        }

        /// <summary>Ohne Nachtauskühlung bleibt die Kennzahl im Lauf und in der Auskunft <c>null</c> (Muster E30).</summary>
        [Fact]
        public void Ohne_Nachtauskuehlung_bleibt_die_Auskunft_null()
        {
            if (!_db.Vorhanden) return;
            Z_ProjGebModel z = Z_ProjGebCtrl.LiesProjekt(PROJEKT).Single();

            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, Klimaregion(PROJEKT), z.ID_Z);
            Assert.True(e.Erfolgreich, e.Befund);
            Assert.Null(e.NachtauskuehlstundenH);
            // Entwurf KP3, Festlegung 26 (B17): ohne Sommerlüftung auch diese Kennzahl null, nicht 0.
            Assert.Null(e.SommerlueftungsstundenH);
            Assert.Null(Dialogdaten().NachtauskuehlstundenH);
            Assert.All(Dialogdaten().Zonen, zone => Assert.Null(zone.NachtauskuehlstundenH));
        }
    }
}
