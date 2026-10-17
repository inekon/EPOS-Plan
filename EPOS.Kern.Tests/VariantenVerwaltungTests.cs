using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die drei Handlungen der Gruppe „Variante verwalten“</b> (Anwenderauftrag 13.10.2026)
    /// auf einer Arbeitskopie der Testdatenbank: Anlegen aus einer Quelle der Gruppe,
    /// Umbenennen, Löschen — und die Schreibrichtung der Übernahme.
    ///
    /// <para><b>Der Anwenderbefund.</b> „Der Knopf ‚Umbenennen‘ erstellt eine Kopie“, und
    /// im Bildschirmfoto stand am Stammprojekt danach „Übergabe rechnen = Nein“ statt „Ja“.
    /// Stamm der Proben ist deshalb 1054 („Referenzprojekt Zonen mit Heizkreis“), das
    /// Projekt des Befunds: Es führt genau ein Gebäude mit <c>Heizkreis_Aktiv = 1</c>. Die
    /// Fälle halten fest, dass Umbenennen drei <c>UPDATE</c>s über die Id sind (kein Projekt,
    /// keine zweite Variantenzeile), dass Anlegen und Übernehmen den Stamm nie anfassen und
    /// dass Löschen allein die gewählte Variante trifft.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VariantenVerwaltungTests : IClassFixture<TestDatenbank>
    {
        /// <summary>„Referenzprojekt Zonen mit Heizkreis“ — ein Gebäude, „Übergabe rechnen“ = Ja.</summary>
        private const int StammId = 1054;

        private readonly TestDatenbank _db;

        public VariantenVerwaltungTests(TestDatenbank db) { _db = db; }

        // ---- Helfer auf der Datenbank -----------------------------------------

        private static string Projektname(int id) => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", id))) ?? "";

        private static int Projektzahl() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM Tab_Projekt"));

        private static int Variantenzahl() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM Tab_Variante"));

        private static string Variantenname(int idProjekt) => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Variantenname FROM Tab_Variante WHERE ID_Projekt = ?", new DbParam("@p", idProjekt))) ?? "";

        private static int StammRef(int idProjekt) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID_ProjektRef FROM Tab_Variante WHERE ID_Projekt = ?", new DbParam("@p", idProjekt)) ?? -1);

        /// <summary>Wie viele Variantenzeilen trägt ein Projekt? Regulär genau eine.</summary>
        private static int Variantenzeilen(int idProjekt) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = ?", new DbParam("@p", idProjekt)));

        /// <summary>„Übergabe rechnen“ des (einen) Gebäudes eines Projekts.</summary>
        private static int HeizkreisAktiv(int idProjekt) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT Heizkreis_Aktiv FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", idProjekt)));

        private static void HeizkreisSetzen(int idProjekt, int wert) => DataRepository.ExecuteSQL(
            "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = ? WHERE ID_Projekt = ?",
            new DbParam("@w", wert), new DbParam("@p", idProjekt));

        private static int Anlegen(string bezeichner)
        {
            string fehler;
            int neu = new VariantenCtrl().AnlegenAusStamm(StammId, Projektname(StammId), bezeichner, out fehler);
            Assert.True(neu > 0, "Die Variante konnte nicht angelegt werden: " + fehler);
            return neu;
        }

        private static AbweichungsErmittler.Merkmal UebergabeRechnen() =>
            AbweichungsErmittler.Felder.First(f => f.Tabelle == "Tab_Gebaeude" && f.Spalte == "Heizkreis_Aktiv");

        // ======================================================================
        //  Umbenennen
        // ======================================================================

        /// <summary>
        /// Umbenennen ist kein Anlegen: Projekt- und Variantenzahl bleiben, die Id bleibt,
        /// Bezeichner und Projektname wechseln, der Stamm bleibt samt „Übergabe rechnen“
        /// unberührt — und der alte Name ist weg (Regression zum Anwenderbefund 13.10.2026).
        /// </summary>
        [Fact]
        public void Umbenennen_benennt_um_und_legt_keine_Kopie_an()
        {
            if (!_db.Vorhanden) return;
            int variante = Anlegen("Umbenennprobe");
            string stammName = Projektname(StammId);
            string alterName = Projektname(variante);
            int projekte = Projektzahl(), varianten = Variantenzahl();

            string fehler, neuerName;
            Assert.True(new VariantenCtrl().Umbenennen(variante, "Umbenannt", out fehler, out neuerName), fehler);

            Assert.Equal(projekte, Projektzahl());
            Assert.Equal(varianten, Variantenzahl());
            Assert.Equal(stammName + " - Umbenannt", neuerName);
            Assert.Equal(neuerName, Projektname(variante));
            Assert.Equal("Umbenannt", Variantenname(variante));
            Assert.Equal(StammId, StammRef(variante));
            Assert.False(new VariantenCtrl().ProjektnameExistiert(alterName), "Der alte Name steht noch.");

            Assert.Equal(stammName, Projektname(StammId));
            Assert.Equal(1, HeizkreisAktiv(StammId));
            Assert.Equal(1, HeizkreisAktiv(variante));
        }

        /// <summary>
        /// Derselbe Bezeichner noch einmal bestätigt bekommt KEINEN Zähler — der eigene Name
        /// ist keine Kollision (Zielname mit Ausnahme der eigenen Id).
        /// </summary>
        [Fact]
        public void Umbenennen_auf_den_eigenen_Namen_haengt_keinen_Zaehler_an()
        {
            if (!_db.Vorhanden) return;
            int variante = Anlegen("Gleichbleibend");
            string vorher = Projektname(variante);

            string fehler, neuerName;
            Assert.True(new VariantenCtrl().Umbenennen(variante, "Gleichbleibend", out fehler, out neuerName), fehler);

            Assert.Equal(vorher, neuerName);
            Assert.Equal(vorher, Projektname(variante));
            Assert.Equal(vorher, new VariantenCtrl().Zielname(Projektname(StammId), "Gleichbleibend", variante));
            Assert.EndsWith(" (2)", new VariantenCtrl().Zielname(Projektname(StammId), "Gleichbleibend"));
        }

        /// <summary>Ein Stammprojekt lehnt der Weg benannt ab; es entsteht und ändert sich nichts.</summary>
        [Fact]
        public void Umbenennen_lehnt_ein_Stammprojekt_ab()
        {
            if (!_db.Vorhanden) return;
            string stammName = Projektname(StammId);
            int projekte = Projektzahl();

            string fehler, neuerName;
            Assert.False(new VariantenCtrl().Umbenennen(StammId, "Verboten", out fehler, out neuerName));

            Assert.Contains("Variante", fehler);
            Assert.Equal(stammName, Projektname(StammId));
            Assert.Equal(projekte, Projektzahl());
        }

        // ======================================================================
        //  Anlegen aus einer Quelle der Gruppe
        // ======================================================================

        /// <summary>
        /// Die neue Variante trägt die DATEN der Quellvariante (hier: „Übergabe rechnen“ = Nein)
        /// und hängt trotzdem am STAMM — Gruppe und Name kommen vom Stamm, der Inhalt von der
        /// Quelle; Stamm und Quelle bleiben, wie sie waren.
        ///
        /// <para><b>Genau EINE Variantenzeile.</b> Der generische Kopierlauf nimmt die
        /// <c>Tab_Variante</c>-Zeile der Quellvariante mit (samt deren Bezeichner); der
        /// Anlegeweg räumt sie, bevor er seine eigene schreibt — sonst stünde die neue Variante
        /// zweimal in der Gruppe, einmal unter dem Namen der Quelle (Anwenderbefund 13.10.2026,
        /// „Umbenennen erstellt eine Kopie“).</para>
        /// </summary>
        [Fact]
        public void AnlegenAusQuelle_kopiert_die_Quelle_und_haengt_am_Stamm()
        {
            if (!_db.Vorhanden) return;
            string stammName = Projektname(StammId);
            int quelle = Anlegen("Quelle ohne Uebergabe");
            HeizkreisSetzen(quelle, 0);
            int projekte = Projektzahl(), varianten = Variantenzahl();

            string fehler;
            int neu = new VariantenCtrl().AnlegenAusQuelle(StammId, stammName, quelle, "Aus der Quelle", out fehler);

            Assert.True(neu > 0, fehler);
            Assert.NotEqual(quelle, neu);
            Assert.Equal(projekte + 1, Projektzahl());
            Assert.Equal(varianten + 1, Variantenzahl());
            Assert.Equal(1, Variantenzeilen(neu));
            Assert.Equal(1, Variantenzeilen(quelle));
            Assert.Equal(StammId, StammRef(neu));
            Assert.Equal("Aus der Quelle", Variantenname(neu));
            Assert.Equal("Quelle ohne Uebergabe", Variantenname(quelle));
            Assert.Equal(stammName + " - Aus der Quelle", Projektname(neu));

            // Die Gruppe fuehrt die neue Variante genau einmal - unter ihrem eigenen Bezeichner.
            System.Collections.Generic.List<VariantenCtrl.VarianteInfo> gruppe =
                new VariantenCtrl().LadeGruppe(StammId, stammName);
            Assert.Single(gruppe, v => v.IdProjekt == neu);
            Assert.Equal("Aus der Quelle", gruppe.First(v => v.IdProjekt == neu).Variantenname);

            Assert.Equal(0, HeizkreisAktiv(neu));        // der Inhalt der Quelle
            Assert.Equal(0, HeizkreisAktiv(quelle));     // die Quelle unverändert
            Assert.Equal(1, HeizkreisAktiv(StammId));    // der Stamm unverändert
        }

        /// <summary>Der Stamm als Quelle ist der bisherige Weg — Zeile für Zeile dasselbe Ergebnis.</summary>
        [Fact]
        public void AnlegenAusQuelle_mit_dem_Stamm_kopiert_den_Stamm()
        {
            if (!_db.Vorhanden) return;
            string stammName = Projektname(StammId);

            string fehler;
            int neu = new VariantenCtrl().AnlegenAusQuelle(StammId, stammName, StammId, "Aus dem Stamm", out fehler);

            Assert.True(neu > 0, fehler);
            Assert.Equal(StammId, StammRef(neu));
            Assert.Equal(stammName + " - Aus dem Stamm", Projektname(neu));
            Assert.Equal(1, HeizkreisAktiv(neu));
        }

        /// <summary>Ein Projekt außerhalb der Gruppe ist keine Quelle: benannt abgelehnt, nichts angelegt.</summary>
        [Fact]
        public void AnlegenAusQuelle_lehnt_ein_Projekt_ausserhalb_der_Gruppe_ab()
        {
            if (!_db.Vorhanden) return;
            int projekte = Projektzahl(), varianten = Variantenzahl();

            string fehler;
            int neu = new VariantenCtrl().AnlegenAusQuelle(StammId, Projektname(StammId), 1030, "Fremd", out fehler);

            Assert.Equal(-1, neu);
            Assert.Contains("Vergleichsgruppe", fehler);
            Assert.Equal(projekte, Projektzahl());
            Assert.Equal(varianten, Variantenzahl());
            Assert.False(new VariantenCtrl().GehoertZurGruppe(StammId, 1030));
            Assert.True(new VariantenCtrl().GehoertZurGruppe(StammId, StammId));
        }

        // ======================================================================
        //  Löschen
        // ======================================================================

        /// <summary>Gelöscht wird die GEWÄHLTE Variante — die andere und der Stamm bleiben.</summary>
        [Fact]
        public void LoescheVariante_trifft_nur_die_gewaehlte()
        {
            if (!_db.Vorhanden) return;
            int bleibt = Anlegen("Bleibt");
            int geht = Anlegen("Geht");
            string stammName = Projektname(StammId);
            int projekte = Projektzahl();

            LoeschBefund befund = new VariantenCtrl().LoescheVariante(geht, Projektname(geht));

            Assert.Equal(LoeschStand.Geloescht, befund.Stand);
            Assert.Equal(projekte - 1, Projektzahl());
            Assert.Equal("", Projektname(geht));
            Assert.Equal(-1, StammRef(geht));

            Assert.Equal(stammName + " - Bleibt", Projektname(bleibt));
            Assert.Equal("Bleibt", Variantenname(bleibt));
            Assert.Equal(StammId, StammRef(bleibt));
            Assert.Equal(stammName, Projektname(StammId));
        }

        // ======================================================================
        //  Die Schreibrichtung der Übernahme
        // ======================================================================

        /// <summary>
        /// „Übernehmen aus…“ schreibt in das ZIEL — die Variante der Unterschiedstabelle — und
        /// nie in die Quelle: Mit dem Stamm als Quelle bekommt die Variante dessen Wert, der
        /// Stamm behält seinen; mit einer Variante als Quelle und einer anderen als Ziel bleibt
        /// der Stamm ebenfalls, wie er war.
        /// </summary>
        [Fact]
        public void Uebernahme_schreibt_nur_in_das_Ziel_und_nie_in_den_Stamm()
        {
            if (!_db.Vorhanden) return;
            AbweichungsErmittler.Merkmal feld = UebergabeRechnen();
            int ziel = Anlegen("Uebernahmeziel");
            HeizkreisSetzen(ziel, 0);

            string fehler;
            MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(StammId, ziel, feld);
            Assert.True(b.Moeglich, b.Grund);
            Assert.True(MerkmalUebernahmeCtrl.Schreibe(b, ziel, feld, out fehler), fehler);

            Assert.Equal(1, HeizkreisAktiv(ziel));
            Assert.Equal(1, HeizkreisAktiv(StammId));

            // Gegenrichtung: eine Variante ohne Übergabe als Quelle, eine zweite als Ziel.
            int quelle = Anlegen("Uebernahmequelle");
            HeizkreisSetzen(quelle, 0);
            MerkmalUebernahmeCtrl.Befund b2 = MerkmalUebernahmeCtrl.Pruefe(quelle, ziel, feld);
            Assert.True(b2.Moeglich, b2.Grund);
            Assert.True(MerkmalUebernahmeCtrl.Schreibe(b2, ziel, feld, out fehler), fehler);

            Assert.Equal(0, HeizkreisAktiv(ziel));
            Assert.Equal(0, HeizkreisAktiv(quelle));
            Assert.Equal(1, HeizkreisAktiv(StammId));
        }
    }
}
