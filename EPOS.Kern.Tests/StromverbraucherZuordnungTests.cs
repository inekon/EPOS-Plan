using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>SV1 — die Zuordnung Projekt ↔ Stromverbraucher wird über die ID aufgelöst</b>
    /// (Anwenderentscheid vom 30.09.2026, „Stromverbraucher-Mängel beheben, mit neuer
    /// Referenzbasis").
    ///
    /// <para><b>Mangel (a), die überlesene Jahressumme.</b> <c>Z_Projekt_Stromverbraucher.Summe</c>
    /// wurde über den BEZEICHNER gesucht — den Namen, unter dem die Zuordnungszeile einmal
    /// angelegt wurde. Die Projektkopie heißt vielfach anders („EFH_3_Pers (P1017)" gegen
    /// die Zeile „EFH_3_Pers" mit Summe 15 in den Referenzprojekten 1017 und 1047): Die
    /// Summe griff nicht, gerechnet wurde das volle Profil (672 MWh).</para>
    ///
    /// <para><b>Mangel (b), die fremde Projektkopie.</b> Kopf- und Typsatz wurden ohne
    /// Projektfilter über den Namen gelesen. Tragen zwei Projektkopien denselben Namen,
    /// galt die erste der Tabelle — in der Testdatenbank rechnete 1046 mit der Kopie „test"
    /// von 1007 (zeichengleich, deshalb bis hierher ohne Wirkung).</para>
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Fälle schreiben.
    /// Ohne Datenbank schweigen sie (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromverbraucherZuordnungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Referenzprojekt mit umbenannter Kopie „EFH_3_Pers (P1017)" (ID 1017103).</summary>
        private const int P1017 = 1017;
        private const int KOPIE_1017 = 1017103;

        /// <summary>Referenzprojekt 1046 mit der Kopie „test" (ID 1105643), zwei Zuordnungszeilen.</summary>
        private const int P1046 = 1046;
        private const int KOPIE_1046 = 1105643;

        /// <summary>Die gleichnamige Kopie „test" des Projekts 1007 — die erste der Tabelle.</summary>
        private const int KOPIE_1007 = 1017104;

        /// <summary>Projekt 1043 führt ZWEI Kopien „EFH_3_Pers"; die Zuordnung zeigt auf die zweite.</summary>
        private const int P1043 = 1043;
        private const int KOPIE_1043_ERSTE = 1105638;
        private const int KOPIE_1043_ZUGEORDNET = 1105639;

        private static double Lauf(int projekt)
        {
            double[] reihe = new SimulationStrombedarf { m_ID_Projekt = projekt }.Stromprofil_Strombedarf_berechnen();
            Assert.NotNull(reihe);
            return reihe.Sum();
        }

        private static double[] LaufReihe(int projekt)
        {
            double[] reihe = new SimulationStrombedarf { m_ID_Projekt = projekt }.Stromprofil_Strombedarf_berechnen();
            Assert.NotNull(reihe);
            return reihe;
        }

        private static string Text(object wert) => Convert.ToString(wert, CultureInfo.InvariantCulture);

        // =====================================================================
        //  (a) Die Jahressumme über die ID
        // =====================================================================

        /// <summary>
        /// <b>Der Fall des Befunds.</b> Die Zuordnungszeile von 1017 heißt „EFH_3_Pers", die
        /// Kopie „EFH_3_Pers (P1017)" — der Lauf skaliert trotzdem auf die gepflegten 15 MWh.
        /// Und er tut es auch, wenn die Kopie noch einmal umbenannt und die Summe geändert
        /// wird: Es zählt allein die ID.
        /// </summary>
        [Fact]
        public void Die_Jahressumme_greift_ueber_die_ID_auch_bei_umbenannter_Kopie()
        {
            if (!_db.Vorhanden) return;

            // Vorbedingung: Die Zuordnungszeile traegt NICHT den Namen der Kopie.
            Assert.Equal("EFH_3_Pers", Text(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", new DbParam("?", P1017))));
            Assert.Equal("EFH_3_Pers (P1017)", Text(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Stromverbraucher WHERE ID = ?", new DbParam("?", KOPIE_1017))));

            // 15 MWh = 15 000 kWh - nicht die 672 000 kWh des ungeskalierten Profils.
            Assert.Equal(15000.0, Lauf(P1017), 3);

            // Kopie umbenennen, Summe aendern: Der Lauf folgt der ID.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Stromverbraucher SET Bezeichner = ? WHERE ID = ?",
                new DbParam("?", "SV1 umbenannt"), new DbParam("?", KOPIE_1017)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Stromverbraucher SET Summe = ? WHERE ID_Projekt = ?",
                new DbParam("?", 20.0), new DbParam("?", P1017)));
            Assert.Equal(20000.0, Lauf(P1017), 3);
        }

        /// <summary>
        /// Die Projektvorschau (Namen aus dem Dialog, ohne Vorgabe) findet dieselbe Summe —
        /// über die ID ihres Kopfsatzes. Vorschau und Lauf zeigen dieselbe Zahl.
        /// </summary>
        [Fact]
        public void Die_Vorschau_findet_die_Jahressumme_ueber_die_ID_ihres_Kopfsatzes()
        {
            if (!_db.Vorhanden) return;

            List<string> namen = Z_ProjektStromverbraucherCtrl.LiesProjekt(P1017)
                                 .Select(m => m.m_szVerbraucher).ToList();
            Assert.Equal(new[] { "EFH_3_Pers (P1017)" }, namen);

            double[] vorschau = new SimulationStrombedarf { m_ID_Projekt = P1017 }
                                .Stromprofil_Strombedarf_berechnen(namen);
            Assert.NotNull(vorschau);
            Assert.Equal(15000.0, vorschau.Sum(), 3);
            Assert.Equal(Lauf(P1017), vorschau.Sum(), 6);
        }

        /// <summary>
        /// Das Sichern der Jahressumme (Assistent, „Übernehmen") trifft die Zeile über die
        /// Projektkopie — mit dem Namen, den der Dialog zeigt. Über den Bezeichner der
        /// Zuordnungszeile gesucht, traf es bei 1017 keine Zeile.
        /// </summary>
        [Fact]
        public void UpdateSumme_trifft_die_Zeile_ueber_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            Assert.True(new Z_ProjektStromverbraucherCtrl().UpdateSumme(20.0, "EFH_3_Pers (P1017)", P1017));
            Assert.Equal(20.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Summe FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", new DbParam("?", P1017)),
                CultureInfo.InvariantCulture));
            Assert.Equal(20000.0, Lauf(P1017), 3);

            // Die Kopie eines ANDEREN Projekts gleichen Namens (1047) bleibt unberuehrt.
            Assert.Equal(15.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Summe FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", new DbParam("?", 1047)),
                CultureInfo.InvariantCulture));
        }

        // =====================================================================
        //  (b) Nie die Kopie eines fremden Projekts
        // =====================================================================

        /// <summary>
        /// <b>Die gleichnamige Kopie eines anderen Projekts wird nicht gelesen.</b> 1046 und
        /// 1007 führen je eine Kopie „test"; die von 1007 steht zuerst in der Tabelle. Sie
        /// wird hier verändert — Monatswerte UND Wochenprofil —, und weder der Lauf noch die
        /// Vorschau von 1046 dürfen sich dadurch bewegen.
        /// </summary>
        [Fact]
        public void Die_gleichnamige_Kopie_eines_anderen_Projekts_wird_nicht_gelesen()
        {
            if (!_db.Vorhanden) return;

            // Vorbedingung: Ohne Projektfilter traefe der Name die Kopie von 1007.
            Assert.Equal((long)KOPIE_1007, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Stromverbraucher WHERE Bezeichner = ?", new DbParam("?", "test")),
                CultureInfo.InvariantCulture));
            Assert.Equal((long)1007, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Projekt FROM Tab_Stromverbraucher WHERE ID = ?", new DbParam("?", KOPIE_1007)),
                CultureInfo.InvariantCulture));

            double[] laufVorher = LaufReihe(P1046);
            double[] lauf1007Vorher = LaufReihe(1007);
            var vorschauNamen = new List<string> { "test" };
            double[] vorschauVorher = new SimulationStrombedarf { m_ID_Projekt = P1046 }
                                      .Stromprofil_Strombedarf_berechnen(vorschauNamen);
            Assert.NotNull(vorschauVorher);

            // Die Kopie von 1007 verziehen: Januar ueberhoeht, Stunde 1 des Wochenprofils auf 1000.
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromverbraucher SET Monat_1 = Monat_1 * 50 WHERE ID = ?", new DbParam("?", KOPIE_1007)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromverbrauchertyp SET \"1\" = 1000 WHERE ID_Stromverbraucher = ?", new DbParam("?", KOPIE_1007)));

            double[] laufNachher = LaufReihe(P1046);
            double[] vorschauNachher = new SimulationStrombedarf { m_ID_Projekt = P1046 }
                                       .Stromprofil_Strombedarf_berechnen(vorschauNamen);

            Assert.Equal(laufVorher, laufNachher);
            Assert.Equal(vorschauVorher, vorschauNachher);

            // Gegenprobe: Projekt 1007 selbst rechnet mit seiner veraenderten Kopie anders -
            // die Verziehung wirkt also, nur eben nicht auf ein fremdes Projekt.
            Assert.NotEqual(lauf1007Vorher, LaufReihe(1007));
        }

        /// <summary>
        /// Zeigt eine Zuordnungszeile per ID auf die Kopie eines ANDEREN Projekts, wird sie
        /// nicht gelesen: Anteil 0 und eine Warnung im Protokoll, kein stiller Fremdwert.
        /// </summary>
        [Fact]
        public void Eine_Zuordnung_auf_die_Kopie_eines_anderen_Projekts_rechnet_nicht()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Stromverbraucher SET ID_Stromverbraucher = ? WHERE ID_Projekt = ?",
                new DbParam("?", KOPIE_1007), new DbParam("?", P1046)));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(0.0, Lauf(P1046));
            Assert.Equal(2, protokoll.Warnungen.Count(w => w.Contains("test")));
        }

        // =====================================================================
        //  Die übrigen Lesestellen derselben Sorte
        // =====================================================================

        /// <summary>
        /// Das Speichern der Zuordnungen bleibt bei der Kopie, auf die die Zeile per ID zeigt.
        /// 1043 führt zwei Kopien „EFH_3_Pers"; über den Namen gesucht, träfe
        /// <see cref="StromverbraucherStammCtrl.GetProjektId"/> die erste.
        /// </summary>
        [Fact]
        public void Das_Speichern_bleibt_bei_der_zugeordneten_Kopie()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(KOPIE_1043_ERSTE, StromverbraucherStammCtrl.GetProjektId("EFH_3_Pers", P1043));

            List<Z_ProjektStromverbraucherModel> liste = Z_ProjektStromverbraucherCtrl.LiesProjekt(P1043);
            Assert.Single(liste);
            Assert.Equal(KOPIE_1043_ZUGEORDNET, liste[0].m_ID_Stromverbraucher);

            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Stromverbraucher(P1043));
            Assert.True(wiz.Add_Projekt_Stromverbraucher(P1043, liste));

            Assert.Equal((long)KOPIE_1043_ZUGEORDNET, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Stromverbraucher FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", new DbParam("?", P1043)),
                CultureInfo.InvariantCulture));

            // Die ID allein genuegt nicht: Name und Projekt muessen passen.
            Assert.Equal(KOPIE_1043_ZUGEORDNET,
                         StromverbraucherStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, "EFH_3_Pers", P1043));
            Assert.Equal(0, StromverbraucherStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, "Hotel_1", P1043));
            Assert.Equal(0, StromverbraucherStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, "EFH_3_Pers", 1044));
        }

        /// <summary>
        /// Assistent und Komponentenbestand nennen je Zeile die Projektkopie, auf die sie per
        /// ID zeigt — wie die Kachel der Startseite —, nicht den alten Katalognamen der
        /// Zuordnungszeile.
        /// </summary>
        [Fact]
        public void Assistent_und_Komponentenbestand_nennen_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(P1017);
            Assert.Equal(new[] { "EFH_3_Pers (P1017)" }, bestand[KomponentenBestandCtrl.STROMSTD].Namen);

            string name = Text(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", P1017)));
            var a = new AssistentCtrl();
            a.LadeStromverbraucher(name);

            Z_ProjektStromverbraucherModel zeile = Assert.Single(a.Stromverbraucher);
            Assert.Equal("EFH_3_Pers (P1017)", zeile.m_szVerbraucher);
            Assert.Equal(KOPIE_1017, zeile.m_ID_Stromverbraucher);
            Assert.Equal(15.0, zeile.m_Summe);
        }
    }
}
