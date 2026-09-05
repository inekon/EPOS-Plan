using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die VORSCHAU AUS EINEM PROJEKT</b> — der Knopf „Simulation…" der drei
    /// Bedarfsprofil-Dialoge (Kachel „Prozesswärme", „Brauchwasser",
    /// „Standardlastprofil" → <c>BedarfsProfileDialog</c>, iU9-W9.5).
    ///
    /// <para><b>Der Befund der Windows-Abnahme vom 05.09.2026</b> — „Simulation
    /// bringt Ergebnis 0 (monatlicher Verlauf), Grafik bleibt leer": an der
    /// Prozesswärme <b>W9‑B‑4</b>, am Standardlastprofil <b>W9‑B‑5</b>. Eine
    /// Ursache, eine Behebung.</para>
    ///
    /// <para><b>Die Ursache ist die Namensauflösung, nicht die Einheit.</b> Der Dialog
    /// listet die Zuordnungen des Projekts, und ihre Namen kommen aus der
    /// PROJEKTKOPIE (<c>Z_Projekt*Ctrl.LiesProjekt</c> liest
    /// <c>Tab_Stromverbraucher.Bezeichner</c> bzw. <c>Tab_Prozesswaerme.Bezeichner</c>).
    /// Eine Projektkopie heißt aber nicht zwingend wie ihr Katalogeintrag — sie trägt
    /// vielfach den Zusatz „ (P‹Projekt›)" oder ist nur im Projekt angelegt. Die
    /// Vorschau schlug diesen Namen bis hierher AUSSCHLIESSLICH im
    /// <c>_STAMM</c>-Katalog nach (<see cref="ProfilQuellmodus.Katalogvorschau"/>,
    /// abgeleitet aus <c>list != null</c>), fand nichts, übersprang das Profil still
    /// — und lieferte zwölf Nullmonate samt leerem Bild.</para>
    ///
    /// <para><b>Die Reihenfolge ist seit W9‑O‑3c die der KOPIE</b> (Anwenderentscheid
    /// vom 05.09.2026): Die Projektvorschau liest die Projektkopie zuerst und den
    /// Katalog als Rückfall — Vorschau und Lauf zeigen damit überall dieselben
    /// Zahlen.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Fälle</b> (<see cref="TestDatenbank"/>);
    /// die Arbeitskopie wird je Klasse geteilt und hier nur GELESEN.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfsProfilVorschauTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BedarfsProfilVorschauTests(TestDatenbank db) { _db = db; }

        /// <summary>Die Namen, die der Dialog anzeigt und an die Vorschau gibt.</summary>
        private static List<string> StromNamen(int idProjekt)
        {
            var namen = new List<string>();
            foreach (Z_ProjektStromverbraucherModel m in Z_ProjektStromverbraucherCtrl.LiesProjekt(idProjekt))
                namen.Add(m.m_szVerbraucher ?? "");
            return namen;
        }

        private static List<string> ProzessNamen(int idProjekt)
        {
            var namen = new List<string>();
            foreach (Z_ProjektProzesswaermeModel m in Z_ProjektProzesswaermeCtrl.LiesProjekt(idProjekt))
                namen.Add(m.szProzessname ?? "");
            return namen;
        }

        private static List<string> BrauchwasserNamen(int idProjekt)
        {
            var namen = new List<string>();
            foreach (Z_ProjektBrauchwasserModel m in Z_ProjektBrauchwasserCtrl.LiesProjekt(idProjekt))
                namen.Add(m.szBezeichner ?? "");
            return namen;
        }

        // ==================================================================
        //  1 — Der Befund: die umbenannte Projektkopie
        // ==================================================================

        /// <summary>
        /// <b>W9‑B‑5, der Fall des Befunds.</b> Projekt 1017 führt genau ein
        /// Standardlastprofil; seine Projektkopie heißt <c>EFH_3_Pers (P1017)</c>,
        /// der Katalogeintrag dagegen <c>EFH_3_Pers</c>. Der Dialog zeigt den
        /// Kopienamen — und genau den bekommt die Vorschau.
        ///
        /// <para>Bis zur Behebung lieferte sie darauf eine Nullreihe. Sie muss
        /// dasselbe rechnen wie der PROJEKTLAUF, denn nichts anderes ist eine
        /// Vorschau.</para>
        /// </summary>
        [Fact]
        public void Standardlastprofil_Vorschau_rechnet_wie_der_Projektlauf()
        {
            if (!_db.Vorhanden) return;

            List<string> namen = StromNamen(1017);
            Assert.Equal(new[] { "EFH_3_Pers (P1017)" }, namen);

            // Der Weg des Dialogs: Namensliste plus Projekt.
            var vorschau = new SimulationStrombedarf { m_ID_Projekt = 1017 };
            float[] gezeigt = vorschau.Stromprofil_Strombedarf_berechnen(namen);

            // Der Weg des Laufs: dasselbe Projekt, ohne Namensliste.
            var lauf = new SimulationStrombedarf { m_ID_Projekt = 1017 };
            float[] gerechnet = lauf.Stromprofil_Strombedarf_berechnen();

            Assert.NotNull(gezeigt);
            Assert.NotNull(gerechnet);
            Assert.True(gezeigt.Sum() > 0, "Die Vorschau liefert eine Nullreihe (Befund W9-B-5).");
            Assert.Equal(gerechnet.Sum(), gezeigt.Sum(), 1);
        }

        /// <summary>
        /// Dieselbe Reihe auch MONATLICH — die Tabelle „Strombedarf monatlich" und das
        /// Säulenbild hängen an ihr, und der Befund nennt beide.
        /// </summary>
        [Fact]
        public void Standardlastprofil_Vorschau_fuellt_die_zwoelf_Monate()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationStrombedarf { m_ID_Projekt = 1017 };
            float[] reihe = sim.Stromprofil_Strombedarf_berechnen(StromNamen(1017));
            Assert.NotNull(reihe);

            Array.Copy(reihe, sim.Strombedarf_viertelStundenwerte, reihe.Length);
            WPPlan.Core.BhkwPlan.MonatsSumme(sim.Strombedarf_viertelStundenwerte,
                                             sim.Strombedarf_monat, sim.mo_anfang, sim.mo_ende);

            Assert.All(sim.Strombedarf_monat.Take(12),
                       m => Assert.True(m > 0, "Ein Monatswert ist 0 (Befund W9-B-5)."));
            Assert.Equal(67.462, sim.Strombedarf_monat[0], 2);
        }

        // ==================================================================
        //  2 — Die Wache: gleiche Quellen, gleiche Zahlen
        // ==================================================================

        /// <summary>
        /// <b>Deckt sich die Kopie mit dem Katalog, ändert sich NICHTS.</b> Die zwei
        /// Proben sind Fälle, in denen die Projektkopie zeichengleich zu ihrem
        /// Katalogeintrag steht — die Zahlen bleiben deshalb auch nach dem Entscheid
        /// W9‑O‑3c die am 05.09.2026 eingefrorenen.
        ///
        /// <para><b>Die dritte Probe ist mit W9‑O‑3c umgezogen.</b> Bis dahin stand hier
        /// auch Brauchwasser 1007 mit dem Satz „der Katalog bleibt die erste Quelle" und
        /// dem Januarwert 1,900 MWh. Genau diese Aussage hat der Anwenderentscheid vom
        /// 05.09.2026 aufgehoben: Die Kopie kommt zuerst, und dort trägt sie eine
        /// ANDERE Verteilung. Der Fall steht jetzt als
        /// <see cref="Brauchwasser_Vorschau_zeigt_die_Verteilung_der_Projektkopie"/> mit
        /// dem Wert der Kopie.</para>
        /// </summary>
        [Fact]
        public void Bekannte_Katalognamen_liefern_unveraenderte_Monatswerte()
        {
            if (!_db.Vorhanden) return;

            // Prozesswaerme, Projekt 1041 - "Hotel_1" steht in beiden Quellen mit
            // denselben zwoelf Monatswerten und demselben Typprofil.
            var p = new SimulationWaermebedarf { m_ID_Projekt = 1041 };
            p.Prozesswaerme_berechnen(ProzessNamen(1041));
            Assert.Equal(30000.0, p.prozesswerte.Sum(), 0);
            Assert.Equal(2.548, p.Waermebedarf_Prozess_Monat[0], 3);
            Assert.Equal(2.301, p.Waermebedarf_Prozess_Monat[1], 3);

            // Stromverbraucher, Projekt 1024 - "Buero_Konst" ebenso.
            var s = new SimulationStrombedarf { m_ID_Projekt = 1024 };
            float[] reihe = s.Stromprofil_Strombedarf_berechnen(StromNamen(1024));
            Assert.NotNull(reihe);
            Assert.Equal(365000.0, reihe.Sum(), 0);
        }

        // ==================================================================
        //  3 — Der Entscheid W9‑O‑3c: die Kopie zuerst
        // ==================================================================

        /// <summary>
        /// <b>W9‑O‑3c („Empfehlung", Anwenderentscheid vom 05.09.2026).</b> Die
        /// Vorschau liest die PROJEKTKOPIE zuerst und fällt erst danach auf den
        /// Katalog zurück — Vorschau und Lauf zeigen damit überall dieselben Zahlen.
        ///
        /// <para>Projekt 1007 führt das Brauchwasserprofil „Haushalt-3". Katalog und
        /// Projektkopie tragen dieselbe Jahressumme (4,0597 MWh aus
        /// <c>Z_Projekt_Brauchwasser</c>), aber eine ANDERE Monatsverteilung: Der
        /// Katalog steht im Januar auf 1,900 MWh, die im Projekt bearbeitete Kopie
        /// auf 0,34 von 2,4997 — auf die Jahressumme skaliert 0,552 MWh. Der
        /// Projektlauf rechnet seit jeher mit der Kopie; die Vorschau zeigte bis zu
        /// diesem Entscheid den Katalogwert.</para>
        /// </summary>
        [Fact]
        public void Brauchwasser_Vorschau_zeigt_die_Verteilung_der_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            var b = new SimulationWaermebedarf { m_ID_Projekt = 1007 };
            b.Brauchwasserwaerme_berechnen(new List<string> { BrauchwasserNamen(1007)[0] });

            // Die Jahressumme bleibt: sie kommt aus Z_Projekt_Brauchwasser und wird
            // auf beide Verteilungen gleich aufskaliert.
            Assert.Equal(4059.700, b.brauchwasserwerte.Sum(), 1);

            // Die Verteilung ist die der KOPIE (W9-O-3c): 0,552 statt 1,900 MWh.
            Assert.Equal(0.552, b.Waermebedarf_Brauchwasser_Monat[0], 3);
            Assert.Equal(0.553, b.Waermebedarf_Brauchwasser_Monat[1], 3);
        }

        /// <summary>
        /// <b>Der Rückfall bleibt der Katalog.</b> Eine eben aufgenommene, noch nicht
        /// gespeicherte Zeile trägt den Namen ihres KATALOGEINTRAGS — ihre
        /// Projektkopie entsteht erst beim Speichern (<c>WizardCtrl.Add_Projekt_*</c>
        /// → <c>CopyFromStamm</c>). Für sie muss die Vorschau weiterhin im Katalog
        /// nachschlagen.
        ///
        /// <para>„Haushalt-3 neu" steht im Brauchwasserkatalog und in KEINER
        /// Projektkopie von 1007; es gibt auch keine Zuordnung in
        /// <c>Z_Projekt_Brauchwasser</c>, also wird nicht skaliert — die zwölf
        /// Katalogmonate stehen unverändert da (Januar 0,400 MWh, Jahr 2,5597 MWh).</para>
        /// </summary>
        [Fact]
        public void Ein_nur_im_Katalog_bekannter_Name_kommt_aus_dem_Katalog()
        {
            if (!_db.Vorhanden) return;

            var b = new SimulationWaermebedarf { m_ID_Projekt = 1007 };
            b.Brauchwasserwaerme_berechnen(new List<string> { "Haushalt-3 neu" });

            Assert.Equal(2559.700, b.brauchwasserwerte.Sum(), 1);
            Assert.Equal(0.400, b.Waermebedarf_Brauchwasser_Monat[0], 3);
        }

        /// <summary>
        /// <b>Die KATALOGVERWALTUNG bleibt Katalogvorschau.</b> Sie öffnet ohne
        /// Projekt (<c>idProjekt = 0</c>) und darf die Projektkopien gar nicht sehen —
        /// die eingefrorenen Zahlen aus <c>BedarfVerwaltungTests</c> gelten
        /// unverändert.
        /// </summary>
        [Fact]
        public void Die_Katalogverwaltung_rechnet_unveraendert_ohne_Projekt()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Prozesswaerme, 0, "CONT");
            Assert.True(v.Erfolgreich);
            Assert.Equal(365000.0, v.Waerme.prozesswerte.Sum(), 1);
            Assert.Equal(365.0, v.Waerme.Waermebedarf_Prozess, 4);
            Assert.Equal(31.0, v.Waerme.Waermebedarf_Prozess_Monat[0], 3);

            BedarfsVorschau s = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Stromverbraucher, 0, "Büro_Konst");
            Assert.True(s.Erfolgreich);
            Assert.Equal(365.0, s.Strom.Strombedarf_Gebaeude_gesamt, 4);
        }

        // ==================================================================
        //  4 — W8‑B‑3: der Profilbedarf stand auf null
        // ==================================================================

        /// <summary>
        /// <b>W8‑B‑3, der Befund der Windows-Abnahme vom 05.09.2026.</b> Der Weg
        /// „Standard Stromprofil" → „Simulation" zeigte <c>max. Strombedarf 3,72 kW</c>
        /// neben <c>Gesamter Strombedarf 0</c>, <c>Stromganglinie 0</c> und
        /// <c>Strombedarf Gebäude 0</c> — bei 8 000 kWh Jahresbedarf im selben Dialog.
        ///
        /// <para><b>Die Ursache war eine fehlende Zeile in der ABSCHRIFT.</b> Die
        /// Windows-Hülle (<c>BedarfsProfileHuelle.Rechenstand.Rechnen</c>) trug die
        /// Vorschaurechnung ein zweites Mal, von Hand nachgezogen. Darin fehlte
        /// <c>Strombedarf_Gebaeude_gesamt = reihe.Sum() / 1000</c> — das Feld blieb 0,
        /// und die Zeile darunter überschrieb <c>Strombedarf_gesamt</c> mit eben dieser
        /// 0. Der Spitzenwert stand daneben richtig da, weil er aus der Reihe kam und
        /// nicht aus der Summe. Dieselbe Klasse Fehler wie W9‑B‑4/B‑5: eine zweite
        /// Fassung derselben Rechnung, aus der etwas herausfällt.</para>
        ///
        /// <para><b>Die Behebung</b> ist
        /// <see cref="SimulationStrombedarf.ProfilbedarfUebernehmen"/> im Kern; Katalog-
        /// und Projektvorschau nehmen sie beide.</para>
        /// </summary>
        [Fact]
        public void Der_Profilbedarf_steht_in_der_Projektvorschau()
        {
            if (!_db.Vorhanden) return;

            List<string> namen = StromNamen(1017);
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(
                BedarfsArt.Stromverbraucher, 1017, namen);

            Assert.True(v.Erfolgreich);
            Assert.True(v.Strom.Strombedarf_Gebaeude_gesamt > 0,
                        "Strombedarf aus Profil ist 0 (Befund W8-B-3).");
            Assert.True(v.Strom.Strombedarf_gesamt > 0,
                        "Gesamter Strombedarf ist 0 (Befund W8-B-3).");

            // Die eingefrorene Zahl: die Stundenreihe des Projekts in MWh. Sie ist
            // zeichengleich zu der, die der PROJEKTLAUF fuer dieselbe Reihe ausweist -
            // 672 000 kWh im Jahr, verteilt auf das Standardlastprofil EFH_3_Pers.
            Assert.Equal(672.000, v.Strom.Strombedarf_Gebaeude_gesamt, 3);

            // Die Vorschau kennt keine Stromganglinie - sie rechnet die AUSGEWAEHLTEN
            // Profile. Die Gesamtsumme ist deshalb die Summe beider Posten mit einem
            // Summanden 0, nicht eine zweite, eigene Groesse.
            Assert.Equal(0.0f, v.Strom.Stromganglinie_gesamt);
            Assert.Equal(v.Strom.Strombedarf_Gebaeude_gesamt + v.Strom.Stromganglinie_gesamt,
                         v.Strom.Strombedarf_gesamt, 6);

            // Der Spitzenwert ist eine LEISTUNG in kW - er war nie 0 und bleibt es nicht.
            Assert.True(v.Strom.Strombedarf_Max > 0);
        }

        /// <summary>
        /// <b>Die Vorschau rechnet, was der LAUF rechnet.</b> Die Summe der Monatswerte
        /// und die Gesamtsumme sind dieselbe Größe in derselben Einheit (MWh) — genau
        /// die Probe, die den Befund W8‑B‑3 sofort gezeigt hätte.
        /// </summary>
        [Fact]
        public void Gesamtsumme_und_Monatswerte_der_Vorschau_passen_zusammen()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(
                BedarfsArt.Stromverbraucher, 1017, StromNamen(1017));
            Assert.True(v.Erfolgreich);

            double monatssumme = 0;
            for (int m = 0; m < 12; m++) monatssumme += v.Strom.Strombedarf_monat[m];

            Assert.Equal(monatssumme, v.Strom.Strombedarf_gesamt, 2);

            // Und die Stuetzstellen sagen dem Bild, dass STUNDEN vorliegen - nicht
            // Viertelstunden wie nach einem vollen Lauf.
            Assert.Equal(8760, v.Strom.Stuetzstellen);
        }

        /// <summary>
        /// <b>Die zwei Wärmewege dürfen sich nicht verschlechtern</b> (W9‑B‑4/B‑5,
        /// W8‑O‑5): Die Projektvorschau der Prozesswärme führt ihre Summe in MWh, die
        /// des Brauchwassers in kWh — beides unverändert, nur jetzt aus dem Kern.
        /// </summary>
        [Fact]
        public void Die_beiden_Waermewege_der_Projektvorschau_bleiben_wie_sie_waren()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau p = BedarfsVorschauCtrl.ProjektVorschau(
                BedarfsArt.Prozesswaerme, 1041, ProzessNamen(1041));
            Assert.True(p.Erfolgreich);
            Assert.Equal(30.0, p.Waerme.Waermebedarf_Prozess, 3);          // MWh
            Assert.Equal(2.548, p.Waerme.Waermebedarf_Prozess_Monat[0], 3);

            BedarfsVorschau b = BedarfsVorschauCtrl.ProjektVorschau(
                BedarfsArt.Brauchwasser, 1007, BrauchwasserNamen(1007));
            Assert.True(b.Erfolgreich);
            Assert.Equal(4059.700, b.Waerme.Waermebedarf_Brauchwasser, 1); // kWh
            Assert.Equal(0.552, b.Waerme.Waermebedarf_Brauchwasser_Monat[0], 3);
        }
    }

    /// <summary>
    /// Derselbe Befund für ein Profil, das es NUR im Projekt gibt — die zweite
    /// Ausprägung von W9‑B‑4 und die, die der Anwender an der Prozesswärme gesehen
    /// hat.
    ///
    /// <para><b>Eigene Klasse, weil dieser Fall SCHREIBT.</b> Er benennt die
    /// Projektkopie in der Arbeitskopie um und darf die lesenden Fälle nicht
    /// stören; <see cref="TestDatenbank"/> gibt jeder Klasse ihre eigene Kopie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfsProfilVorschauNurImProjektTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BedarfsProfilVorschauNurImProjektTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Projekt 1041 führt die Prozesswärme „Hotel_1". Wird ihre Projektkopie
        /// umbenannt — genau das, was der Kopiervorgang mit „ (P‹Projekt›)" tut —,
        /// kennt der Katalog den Namen nicht mehr. Die Vorschau muss ihn dann in der
        /// PROJEKTKOPIE finden statt zwölf Nullen zu zeigen.
        /// </summary>
        [Fact]
        public void Ein_nur_im_Projekt_bekanntes_Profil_wird_gerechnet()
        {
            if (!_db.Vorhanden) return;

            const string neu = "Hotel_1 (P1041)";
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Prozesswaerme SET Bezeichner=? WHERE ID_Projekt=? AND Bezeichner=?",
                new DbParam("@neu", neu), new DbParam("@pid", 1041),
                new DbParam("@alt", "Hotel_1")));

            var namen = new List<string>();
            foreach (Z_ProjektProzesswaermeModel m in Z_ProjektProzesswaermeCtrl.LiesProjekt(1041))
                namen.Add(m.szProzessname ?? "");
            Assert.Equal(new[] { neu }, namen);

            var sim = new SimulationWaermebedarf { m_ID_Projekt = 1041 };
            sim.Prozesswaerme_berechnen(namen);
            sim.ProzesssummeUebernehmen();

            Assert.True(sim.prozesswerte.Sum() > 0,
                        "Die Vorschau liefert eine Nullreihe (Befund W9-B-4).");
            Assert.True(sim.Waermebedarf_Prozess > 0);
            Assert.All(sim.Waermebedarf_Prozess_Monat.Take(12),
                       m => Assert.True(m > 0, "Ein Monatswert ist 0 (Befund W9-B-4)."));
        }
    }
}
