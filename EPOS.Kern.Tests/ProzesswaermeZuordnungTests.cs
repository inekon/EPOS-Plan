using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>SV2 — die Zuordnung Projekt ↔ Prozesswärme wird über die ID aufgelöst</b>
    /// (Anwenderentscheid vom 02.10.2026, „dieselbe ID-Regel wie SV1 für Brauchwasser und
    /// Prozesswärme"; Vorbild <see cref="StromverbraucherZuordnungTests"/>).
    ///
    /// <para><b>Der Mangel.</b> Die Namenssicht <c>Abfrage_Monatswaerme_Prozesse</c> lieferte
    /// dem Lauf den Bezeichner der ZUORDNUNGSZEILE, nicht den der Projektkopie. Trug die Kopie
    /// einen anderen Namen, fand der Lauf ihren Kopfsatz nicht: Warnung, Anteil 0 — Projekt 1041
    /// verlöre „Hotel_1" mit 30 MWh/a. Die Vorschau las dagegen den Namen der Kopie und suchte
    /// die Jahressumme über ihn: Sie zeigte dann 365 statt 30 MWh/a.</para>
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Fälle schreiben. Ohne
    /// Datenbank schweigen sie (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProzesswaermeZuordnungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const string NAME = "Hotel_1";
        private const string UMBENANNT = "Hotel_1 (P1041)";

        /// <summary>Projekt 1041: Zuordnung „Hotel_1" mit Summe 30 auf die Kopie 13 (zwölf Monatswerte: 365).</summary>
        private const int P1041 = 1041;
        private const int KOPIE_1041 = 13;

        /// <summary>
        /// Eine zweite, gleichnamige Kopie „Hotel_1" in 1041, die der Fall selbst anlegt — mit
        /// kleinerer ID als die zugeordnete, also die erste der Tabelle (die ID ist in der
        /// Testdatenbank frei).
        /// </summary>
        private const int TAEUSCHUNG = 11;

        /// <summary>Die Kopie „CONT" des Projekts 1023.</summary>
        private const int KOPIE_1023 = 1;

        private static double[] LaufReihe(int projekt)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt };
            sim.Prozesswaerme_berechnen();
            return (double[])sim.prozesswerte.Clone();
        }

        private static double Lauf(int projekt) => LaufReihe(projekt).Sum();

        private static BedarfsVorschau Vorschau(int projekt, List<string> namen)
        {
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Prozesswaerme, projekt, namen);
            Assert.True(v.Erfolgreich);
            return v;
        }

        /// <summary>Die Namen, die der Dialog zeigt: die der Projektkopien (<see cref="Z_ProjektProzesswaermeCtrl.LiesProjekt"/>).</summary>
        private static List<string> DialogNamen(int projekt)
            => Z_ProjektProzesswaermeCtrl.LiesProjekt(projekt).Select(m => m.szProzessname).ToList();

        private static string Text(object wert) => Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static double GespeicherteSumme(int projekt)
            => Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Summe FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("?", projekt)),
                CultureInfo.InvariantCulture);

        private static long ZugeordneteKopie(int projekt)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Prozesswaerme FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("?", projekt)),
                CultureInfo.InvariantCulture);

        private static void KopieUmbenennen(string name)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Prozesswaerme SET Bezeichner = ? WHERE ID = ?",
                                                     new DbParam("?", name), new DbParam("?", KOPIE_1041)));

        /// <summary>
        /// Legt die gleichnamige zweite Kopie an — Kopf und Typzeile als Abschrift der
        /// zugeordneten, dann verzogen: Januar überhöht, Stunde 1 des Wochenprofils auf 1000.
        /// </summary>
        private static void TaeuschungAnlegen()
        {
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Prozesswaerme WHERE ID = ?", new DbParam("?", TAEUSCHUNG)),
                CultureInfo.InvariantCulture));

            string monate = string.Join(", ", Enumerable.Range(1, 12).Select(i => "Monat_" + i));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Prozesswaerme (ID, ID_Projekt, Bezeichner, Typ, Beschreibung, " + monate + ", ReadOnly) " +
                "SELECT ?, ID_Projekt, Bezeichner, Typ, Beschreibung, " + monate + ", ReadOnly " +
                "FROM Tab_Prozesswaerme WHERE ID = ?",
                new DbParam("?", TAEUSCHUNG), new DbParam("?", KOPIE_1041)));

            string stunden = string.Join(", ", Enumerable.Range(1, 168).Select(i => "\"" + i + "\""));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Prozesstyp (ID, ID_Prozesswaerme, ID_Projekt, Typname, Beschreibung, " + stunden + ", ReadOnly) " +
                "SELECT ?, ?, ID_Projekt, Typname, Beschreibung, " + stunden + ", ReadOnly " +
                "FROM Tab_Prozesstyp WHERE ID_Prozesswaerme = ?",
                new DbParam("?", TAEUSCHUNG), new DbParam("?", TAEUSCHUNG), new DbParam("?", KOPIE_1041)));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Prozesswaerme SET Monat_1 = Monat_1 * 50 WHERE ID = ?", new DbParam("?", TAEUSCHUNG)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Prozesstyp SET \"1\" = 1000 WHERE ID_Prozesswaerme = ?", new DbParam("?", TAEUSCHUNG)));
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        // =====================================================================
        //  Kopfsatz und Jahressumme über die ID
        // =====================================================================

        /// <summary>
        /// <b>Der Fall des Befunds.</b> Heißt die Kopie anders als ihre Zuordnungszeile, kommt der
        /// Kopfsatz trotzdem — über die ID: 30 MWh/a, keine Warnung, auch im vollen
        /// Wärmebedarfslauf. Ebenso, wenn umgekehrt die Zuordnungszeile umbenannt wird.
        /// </summary>
        [Fact]
        public void Der_Kopfsatz_kommt_ueber_die_ID_auch_bei_abweichendem_Zuordnungsnamen()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(NAME, Text(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("?", P1041))));
            Assert.Equal(30.0, GespeicherteSumme(P1041));

            KopieUmbenennen(UMBENANNT);

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(30000.0, Lauf(P1041), 3);
            Assert.DoesNotContain(protokoll.Warnungen, w => w.Contains(NAME));

            var lauf = new SimulationWaermebedarf();
            lauf.Waermebedarf_berechnen(P1041, Klimaregion(P1041));
            Assert.Equal(30.0, lauf.Waermebedarf_Prozess, 6);

            // Umgekehrt: Kopie wieder "Hotel_1", die Zuordnungszeile heisst anders.
            KopieUmbenennen(NAME);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Prozesswaerme SET Bezeichner = ? WHERE ID_Projekt = ?",
                new DbParam("?", "Hotel alt"), new DbParam("?", P1041)));
            Assert.Equal(30000.0, Lauf(P1041), 3);
        }

        /// <summary>
        /// Die Projektvorschau (Namen aus dem Dialog, ohne Vorgabe) findet die gepflegte Summe
        /// über die ID ihres Kopfsatzes: 30 MWh/a, nicht die 365 MWh/a der zwölf Monatswerte.
        /// Vorschau und Lauf liefern dieselbe Stundenreihe.
        /// </summary>
        [Fact]
        public void Die_Vorschau_rechnet_mit_der_gepflegten_Summe_wie_der_Lauf()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(UMBENANNT);
            List<string> namen = DialogNamen(P1041);
            Assert.Equal(new[] { UMBENANNT }, namen);

            BedarfsVorschau v = Vorschau(P1041, namen);
            Assert.Equal(30.0, v.Waerme.Waermebedarf_Prozess, 6);
            Assert.Equal(LaufReihe(P1041), v.Waerme.prozesswerte);
        }

        /// <summary>
        /// Das Sichern der Jahressumme (Assistent, „Übernehmen") trifft die Zeile über die
        /// Projektkopie — mit dem Namen, den der Dialog zeigt; der Name der Zuordnungszeile
        /// trifft nichts mehr.
        /// </summary>
        [Fact]
        public void UpdateSumme_trifft_die_Zeile_ueber_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(UMBENANNT);

            Assert.True(new Z_ProjektProzesswaermeCtrl().UpdateSumme(12.0, UMBENANNT, P1041));
            Assert.Equal(12.0, GespeicherteSumme(P1041));
            Assert.Equal(12000.0, Lauf(P1041), 3);

            new Z_ProjektProzesswaermeCtrl().UpdateSumme(40.0, NAME, P1041);
            Assert.Equal(12.0, GespeicherteSumme(P1041));
        }

        // =====================================================================
        //  Die zugeordnete Kopie, nie eine fremde
        // =====================================================================

        /// <summary>
        /// <b>Unter gleichnamigen Kopien desselben Projekts gilt die zugeordnete.</b> Eine zweite
        /// Kopie „Hotel_1" mit verzogenen Werten steht VOR der zugeordneten in der Tabelle; über
        /// den Namen träfe sie Kopf- und Typsatz. Lauf und Vorschau bleiben unverändert.
        /// </summary>
        [Fact]
        public void Unter_gleichnamigen_Kopien_gilt_die_zugeordnete()
        {
            if (!_db.Vorhanden) return;

            double[] laufVorher = LaufReihe(P1041);
            double[] vorschauVorher = Vorschau(P1041, DialogNamen(P1041)).Waerme.prozesswerte;

            TaeuschungAnlegen();
            Assert.Equal(TAEUSCHUNG, ProzesswaermeStammCtrl.GetProjektId(NAME, P1041));

            Assert.Equal(laufVorher, LaufReihe(P1041));
            Assert.Equal(vorschauVorher, Vorschau(P1041, DialogNamen(P1041)).Waerme.prozesswerte);
        }

        /// <summary>
        /// Das Speichern der Zuordnungen bleibt bei der Kopie, auf die die Zeile per ID zeigt —
        /// auch neben einer gleichnamigen zweiten Kopie, die über den Namen zuerst träfe.
        /// </summary>
        [Fact]
        public void Das_Speichern_bleibt_bei_der_zugeordneten_Kopie()
        {
            if (!_db.Vorhanden) return;

            TaeuschungAnlegen();

            List<Z_ProjektProzesswaermeModel> liste = Z_ProjektProzesswaermeCtrl.LiesProjekt(P1041);
            Assert.Equal(KOPIE_1041, Assert.Single(liste).ID_Prozesswaerme);

            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Prozess(P1041));
            Assert.True(wiz.Add_Projekt_Prozess(P1041, liste));
            Assert.Equal((long)KOPIE_1041, ZugeordneteKopie(P1041));

            // Die ID allein genuegt nicht: Name und Projekt muessen passen.
            Assert.Equal(KOPIE_1041, ProzesswaermeStammCtrl.GetProjektIdUeberId(KOPIE_1041, NAME, P1041));
            Assert.Equal(0, ProzesswaermeStammCtrl.GetProjektIdUeberId(KOPIE_1041, "CONT", P1041));
            Assert.Equal(0, ProzesswaermeStammCtrl.GetProjektIdUeberId(KOPIE_1041, NAME, 1023));
        }

        /// <summary>
        /// Zeigt die Zuordnungszeile per ID auf die Kopie eines ANDEREN Projekts, wird sie nicht
        /// gelesen: Anteil 0 und eine Warnung im Protokoll — statt still die eigene Kopie gleichen
        /// Namens zu nehmen, wie es der Namensweg tat.
        /// </summary>
        [Fact]
        public void Eine_Zuordnung_auf_die_Kopie_eines_anderen_Projekts_rechnet_nicht()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Prozesswaerme SET ID_Prozesswaerme = ? WHERE ID_Projekt = ?",
                new DbParam("?", KOPIE_1023), new DbParam("?", P1041)));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(0.0, Lauf(P1041));
            Assert.Equal(1, protokoll.Warnungen.Count(w => w.Contains(NAME)));
        }

        /// <summary>
        /// Assistent und Komponentenbestand nennen je Zeile die Projektkopie, auf die sie per ID
        /// zeigt — wie die Kachel der Startseite —, nicht den Namen der Zuordnungszeile. Das
        /// Speichern des Assistenten bleibt bei dieser Kopie; über den alten Namen hätte es eine
        /// neue aus dem Katalog gezogen.
        /// </summary>
        [Fact]
        public void Assistent_und_Komponentenbestand_nennen_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(UMBENANNT);

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(P1041);
            Assert.Equal(new[] { UMBENANNT }, bestand[KomponentenBestandCtrl.PROZESS].Namen);

            string projektname = Text(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", P1041)));
            var a = new AssistentCtrl();
            a.LadeProzess(projektname);

            Z_ProjektProzesswaermeModel zeile = Assert.Single(a.Prozess);
            Assert.Equal(UMBENANNT, zeile.szProzessname);
            Assert.Equal(KOPIE_1041, zeile.ID_Prozesswaerme);
            Assert.Equal(30.0, zeile.Summe);

            long kopienVorher = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("?", P1041)),
                CultureInfo.InvariantCulture);
            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Prozess(P1041));
            Assert.True(wiz.Add_Projekt_Prozess(P1041, a.Prozess));
            Assert.Equal((long)KOPIE_1041, ZugeordneteKopie(P1041));
            Assert.Equal(kopienVorher, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("?", P1041)),
                CultureInfo.InvariantCulture));
        }
    }
}
