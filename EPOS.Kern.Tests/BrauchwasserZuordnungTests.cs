using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>SV2 — die Zuordnung Projekt ↔ Brauchwasser wird über die ID aufgelöst</b>
    /// (Anwenderentscheid vom 02.10.2026, „dieselbe ID-Regel wie SV1 für Brauchwasser und
    /// Prozesswärme"; Vorbild <see cref="StromverbraucherZuordnungTests"/>).
    ///
    /// <para><b>Der Mangel.</b> <c>Z_Projekt_Brauchwasser.Summe</c> wurde über den BEZEICHNER
    /// gesucht — mit dem Namen der Projektkopie, den die Namenssicht lieferte. Trägt die
    /// Zuordnungszeile einen anderen Namen (den, unter dem sie angelegt wurde), griff die
    /// gepflegte Jahressumme nicht: Projekt 1026 rechnete dann das volle Profil der Kopie,
    /// 0,7429 statt 5 MWh/a. Unter gleichnamigen Kopien desselben Projekts galt die erste der
    /// Tabelle, nicht die zugeordnete (1043 führt zwei Kopien „EFH Wohnen, 1 Person", die Zeile
    /// zeigt auf die zweite).</para>
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Fälle schreiben. Ohne
    /// Datenbank schweigen sie (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BrauchwasserZuordnungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const string NAME = "EFH Wohnen, 1 Person";
        private const string UMBENANNT = "EFH Wohnen, 1 Person (P1026)";

        /// <summary>Projekt 1026: Zuordnung „EFH Wohnen, 1 Person" mit Summe 5 auf die Kopie 1163472.</summary>
        private const int P1026 = 1026;
        private const int KOPIE_1026 = 1163472;

        /// <summary>Projekt 1027 führt eine gleichnamige Kopie (1163473), ebenfalls mit Summe 5.</summary>
        private const int P1027 = 1027;
        private const int KOPIE_1027 = 1163473;

        /// <summary>Projekt 1043 führt ZWEI Kopien „EFH Wohnen, 1 Person"; die Zuordnung zeigt auf die zweite.</summary>
        private const int P1043 = 1043;
        private const int KOPIE_1043_ERSTE = 1933498;
        private const int KOPIE_1043_ZUGEORDNET = 1933499;

        /// <summary>
        /// Der Projektlauf der Profilroutine (<c>Brauchwasserwaerme_berechnen</c> ohne Liste,
        /// derselbe Aufruf wie in <c>Waermebedarf_berechnen</c>) — die Stundenreihe [kWh].
        /// </summary>
        private static double[] LaufReihe(int projekt)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt };
            sim.Brauchwasserwaerme_berechnen();
            return (double[])sim.brauchwasserwerte.Clone();
        }

        private static double Lauf(int projekt) => LaufReihe(projekt).Sum();

        /// <summary>Die Vorschau des Bedarfsprofildialogs mit den Namen, die er anzeigt.</summary>
        private static double[] VorschauReihe(int projekt, List<string> namen)
        {
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, projekt, namen);
            Assert.True(v.Erfolgreich);
            return (double[])v.Waerme.brauchwasserwerte.Clone();
        }

        /// <summary>Die Namen, die der Dialog zeigt: die der Projektkopien (<see cref="Z_ProjektBrauchwasserCtrl.LiesProjekt"/>).</summary>
        private static List<string> DialogNamen(int projekt)
            => Z_ProjektBrauchwasserCtrl.LiesProjekt(projekt).Select(m => m.szBezeichner).ToList();

        private static string Text(object wert) => Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static double Zahl(object wert) => Convert.ToDouble(wert, CultureInfo.InvariantCulture);

        private static void KopieUmbenennen(int idKopie, string name)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Brauchwasser SET Bezeichner = ? WHERE ID = ?",
                                                     new DbParam("?", name), new DbParam("?", idKopie)));

        /// <summary>Januar der Kopie überhöht, Stunde 1 ihres Wochenprofils auf 1000.</summary>
        private static void KopieVerziehen(int idKopie)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Brauchwasser SET Monat_1 = Monat_1 * 50 WHERE ID = ?", new DbParam("?", idKopie)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Brauchwassertyp SET \"1\" = 1000 WHERE ID_Brauchwasser = ?", new DbParam("?", idKopie)));
        }

        private static double GespeicherteSumme(int projekt)
            => Zahl(DataRepository.ExecuteScalar(
                "SELECT Summe FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("?", projekt)));

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        // =====================================================================
        //  Die Jahressumme über die ID
        // =====================================================================

        /// <summary>
        /// <b>Der Fall des Befunds.</b> Die Kopie von 1026 heißt anders als ihre Zuordnungszeile —
        /// der Lauf skaliert trotzdem auf die gepflegten 5 MWh/a, nicht auf die 0,7429 MWh/a der
        /// zwölf Monatswerte; der volle Wärmebedarfslauf weist dieselbe Menge aus. Eine geänderte
        /// Summe folgt der ID.
        /// </summary>
        [Fact]
        public void Die_Jahressumme_greift_ueber_die_ID_auch_bei_umbenannter_Kopie()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(NAME, Text(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("?", P1026))));
            Assert.Equal(5.0, GespeicherteSumme(P1026));

            KopieUmbenennen(KOPIE_1026, UMBENANNT);

            // 5 MWh = 5 000 kWh - nicht die 742,9 kWh des ungeskalierten Profils.
            Assert.Equal(5000.0, Lauf(P1026), 3);

            var lauf = new SimulationWaermebedarf();
            lauf.Waermebedarf_berechnen(P1026, Klimaregion(P1026));
            Assert.Equal(5.0, lauf.Waermebedarf_Brauchwasser, 6);

            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Brauchwasser SET Summe = ? WHERE ID_Projekt = ?",
                new DbParam("?", 7.5), new DbParam("?", P1026)));
            Assert.Equal(7500.0, Lauf(P1026), 3);
        }

        /// <summary>
        /// Die Projektvorschau (Namen aus dem Dialog, ohne Vorgabe) findet dieselbe Summe — über
        /// die ID ihres Kopfsatzes. Vorschau und Lauf liefern dieselbe Stundenreihe.
        /// </summary>
        [Fact]
        public void Die_Vorschau_findet_die_Jahressumme_ueber_die_ID_ihres_Kopfsatzes()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(KOPIE_1026, UMBENANNT);
            List<string> namen = DialogNamen(P1026);
            Assert.Equal(new[] { UMBENANNT }, namen);

            double[] vorschau = VorschauReihe(P1026, namen);
            Assert.Equal(5000.0, vorschau.Sum(), 3);
            Assert.Equal(LaufReihe(P1026), vorschau);
        }

        /// <summary>
        /// Das Sichern der Jahressumme trifft die Zeile über die Projektkopie — mit dem Namen,
        /// den der Dialog zeigt. Der Name der Zuordnungszeile zählt nicht mehr, und die
        /// gleichnamige Zeile eines anderen Projekts bleibt unberührt.
        /// </summary>
        [Fact]
        public void UpdateSumme_trifft_die_Zeile_ueber_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(KOPIE_1026, UMBENANNT);

            Assert.True(new Z_ProjektBrauchwasserCtrl().UpdateSumme(7.5, UMBENANNT, P1026));
            Assert.Equal(7.5, GespeicherteSumme(P1026));
            Assert.Equal(7500.0, Lauf(P1026), 3);

            // Der Bezeichner der Zuordnungszeile ("EFH Wohnen, 1 Person") nennt keine Kopie
            // dieses Projekts mehr - er trifft nichts.
            new Z_ProjektBrauchwasserCtrl().UpdateSumme(9.0, NAME, P1026);
            Assert.Equal(7.5, GespeicherteSumme(P1026));

            // 1027 traegt dieselben Namen und bleibt bei 5.
            Assert.Equal(5.0, GespeicherteSumme(P1027));
        }

        // =====================================================================
        //  Die zugeordnete Kopie, nie eine fremde
        // =====================================================================

        /// <summary>
        /// <b>Unter gleichnamigen Kopien desselben Projekts gilt die zugeordnete.</b> 1043 führt
        /// zwei Kopien „EFH Wohnen, 1 Person"; über den Namen träfe die erste. Sie wird verzogen —
        /// Monatswerte und Wochenprofil —, und weder Lauf noch Vorschau bewegen sich. Gegenprobe:
        /// Wird die zugeordnete verzogen, rechnet der Lauf anders.
        /// </summary>
        [Fact]
        public void Unter_gleichnamigen_Kopien_gilt_die_zugeordnete()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(KOPIE_1043_ERSTE, BrauchwasserStammCtrl.GetProjektId(NAME, P1043));
            Z_ProjektBrauchwasserModel zeile = Assert.Single(Z_ProjektBrauchwasserCtrl.LiesProjekt(P1043));
            Assert.Equal(KOPIE_1043_ZUGEORDNET, zeile.ID_Brauchwasser);

            double[] laufVorher = LaufReihe(P1043);
            double[] vorschauVorher = VorschauReihe(P1043, DialogNamen(P1043));
            Assert.Equal(5000.0, laufVorher.Sum(), 3);

            KopieVerziehen(KOPIE_1043_ERSTE);
            Assert.Equal(laufVorher, LaufReihe(P1043));
            Assert.Equal(vorschauVorher, VorschauReihe(P1043, DialogNamen(P1043)));

            KopieVerziehen(KOPIE_1043_ZUGEORDNET);
            Assert.NotEqual(laufVorher, LaufReihe(P1043));
        }

        /// <summary>
        /// Das Speichern der Zuordnungen bleibt bei der Kopie, auf die die Zeile per ID zeigt,
        /// und die Gleichheitsprobe des Dialogs erkennt den gespeicherten Stand als unverändert.
        /// Über den Namen gesucht, träfe <see cref="BrauchwasserStammCtrl.GetProjektId"/> die
        /// erste Kopie — beides stand bis hierher auf ihr.
        /// </summary>
        [Fact]
        public void Das_Speichern_bleibt_bei_der_zugeordneten_Kopie()
        {
            if (!_db.Vorhanden) return;

            List<Z_ProjektBrauchwasserModel> liste = Z_ProjektBrauchwasserCtrl.LiesProjekt(P1043);
            Assert.True(Z_ProjektBrauchwasserCtrl.GleichGespeichert(P1043, liste));

            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Brauchwasser(P1043));
            Assert.True(wiz.Add_Projekt_Brauchwasser(P1043, liste));

            Assert.Equal((long)KOPIE_1043_ZUGEORDNET, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Brauchwasser FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("?", P1043)),
                CultureInfo.InvariantCulture));

            // Die ID allein genuegt nicht: Name und Projekt muessen passen.
            Assert.Equal(KOPIE_1043_ZUGEORDNET,
                         BrauchwasserStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, NAME, P1043));
            Assert.Equal(0, BrauchwasserStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, "Haushalt-3", P1043));
            Assert.Equal(0, BrauchwasserStammCtrl.GetProjektIdUeberId(KOPIE_1043_ZUGEORDNET, NAME, 1044));
        }

        /// <summary>
        /// <b>Die Kopie eines anderen Projekts wird nie gelesen.</b> (a) 1026 und 1027 führen je
        /// eine Kopie „EFH Wohnen, 1 Person", die von 1026 steht zuerst in der Tabelle; sie wird
        /// verzogen, Lauf und Vorschau von 1027 bleiben. (b) Zeigt die Zuordnungszeile von 1027
        /// per ID auf die Kopie von 1026, rechnet sie nicht: Anteil 0 und eine Warnung — statt
        /// still die eigene gleichnamige Kopie zu nehmen.
        /// </summary>
        [Fact]
        public void Die_Kopie_eines_anderen_Projekts_wird_nie_gelesen()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal((long)KOPIE_1026, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Brauchwasser WHERE Bezeichner = ? ORDER BY ID", new DbParam("?", NAME)),
                CultureInfo.InvariantCulture));

            double[] laufVorher = LaufReihe(P1027);
            double[] vorschauVorher = VorschauReihe(P1027, DialogNamen(P1027));
            double[] lauf1026Vorher = LaufReihe(P1026);

            KopieVerziehen(KOPIE_1026);
            Assert.Equal(laufVorher, LaufReihe(P1027));
            Assert.Equal(vorschauVorher, VorschauReihe(P1027, DialogNamen(P1027)));
            Assert.NotEqual(lauf1026Vorher, LaufReihe(P1026));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Brauchwasser SET ID_Brauchwasser = ? WHERE ID_Projekt = ?",
                new DbParam("?", KOPIE_1026), new DbParam("?", P1027)));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Equal(0.0, Lauf(P1027));
            Assert.Equal(1, protokoll.Warnungen.Count(w => w.Contains(NAME)));
            Assert.Equal(KOPIE_1027, BrauchwasserStammCtrl.GetProjektId(NAME, P1027));
        }

        /// <summary>
        /// Der Komponentenbestand nennt je Zeile die Projektkopie, auf die sie per ID zeigt — wie
        /// die Kachel der Startseite —, nicht den Namen der Zuordnungszeile.
        /// </summary>
        [Fact]
        public void Der_Komponentenbestand_nennt_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            KopieUmbenennen(KOPIE_1026, UMBENANNT);

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(P1026);
            Assert.Equal(new[] { UMBENANNT }, bestand[KomponentenBestandCtrl.BRAUCHWASSER].Namen);
        }
    }
}
