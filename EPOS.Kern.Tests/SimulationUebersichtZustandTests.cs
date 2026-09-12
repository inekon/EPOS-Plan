using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der ERGEBNISZUSTAND der Simulationsübersicht (Auftrag <b>#236</b>,
    /// Anwenderrückmeldung vom 12.09.2026).
    ///
    /// <para><b>Der Befund.</b> Der Startseiten-Reiter „Simulation" zeigte rechts
    /// „Strombedarf 0,00 MWh/a", „Deckung 0,0 %" und die Marke „kein Stromerzeuger im
    /// Projekt", während die Projektzusammenfassung links 2 850,20 MWh/a nannte — aus
    /// einer eingelesenen Stromganglinie. Gerechnet hatte der Kern richtig; gezeichnet
    /// war ein NULLOBJEKT: <c>SimulationErgebnisHuelle.Zusammentragen</c> stieg bei
    /// ungültigem Ergebnis aus, bevor <c>d.Uebersicht</c> gebaut war, und das mit
    /// <c>new UebersichtDaten()</c> vorbelegte Feld sah für den Reiter aus wie ein
    /// Ergebnis.</para>
    ///
    /// <para><b>Was diese Fälle festhalten.</b> Die Zustände, in denen kein gültiges
    /// Ergebnis vorliegt — noch nicht gerechnet und veraltet (jeder Besuch der
    /// Stromspeicher-Auslegung, der speichert oder rechnet) —, tragen einen benannten
    /// <see cref="ErgebnisZustand"/> samt Anlass, die Übersicht bleibt dabei
    /// <c>null</c>, und die BEDARFSZAHLEN stehen in jedem Zustand da: Sie kommen aus
    /// der Bedarfsrechnung, nicht aus dem Lauf.</para>
    ///
    /// <para><b>Projekt 1030</b> ist der Fall des Anwenders in der Testdatenbank: nur
    /// eine Stromganglinie (<c>Z_ProjektStromganglinie</c>), keine
    /// Stromverbraucherprofile. Die Basis R7 führt dafür
    /// <c>Energiebedarf.Strombedarf_Gesamt;4790.09</c>. Der letzte Fall baut daraus in
    /// der ARBEITSKOPIE ein Projekt, das dem des Anwenders noch näher kommt: kein
    /// Wärmeerzeuger, kein Wärmebedarf, eine Speichereinheit.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationUebersichtZustandTests
    {
        /// <summary>Nur eine Stromganglinie, keine Stromverbraucher — der Fall des Anwenders.</summary>
        private const int PROJEKT_GANGLINIE = 1030;

        /// <summary>Das Prüfprojekt der Speicherflotte (SP‑O‑8) — Quelle der Flotteneinheit.</summary>
        private const int PROJEKT_FLOTTE = 1046;

        /// <summary>Der Strombedarf des Projekts 1030 laut Basis R7 [MWh/a].</summary>
        private const double STROMBEDARF_MWH = 4790.09;

        // =================================================================================
        // 1 — Vor dem ersten Lauf
        // =================================================================================

        /// <summary>
        /// <b>Der Befund selbst.</b> Ohne gerechneten Lauf trägt der Stand den Zustand
        /// „noch nicht gerechnet" — und trotzdem die beiden BEDARFSZAHLEN: Sie kommen
        /// aus der Bedarfsrechnung des Projekts, genau wie die Zusammenfassung in der
        /// linken Spalte des Startreiters. Eine Übersicht baut die Hülle dafür NICHT;
        /// ein vorbelegtes DTO wäre eine Aussage über einen Lauf, den es nicht gibt.
        /// </summary>
        [Fact]
        public void Ohne_Lauf_steht_der_Bedarf_und_keine_Uebersicht()
        {
            using var kopie = new TestDatenbank();
            if (!kopie.Vorhanden) return;

            SimulationErgebnisDaten d = Ergebnisdienste(PROJEKT_GANGLINIE).Laden(PROJEKT_GANGLINIE);

            Assert.Equal(ErgebnisZustand.NichtGerechnet, d.Zustand);
            Assert.False(d.ErgebnisGueltig);

            // Die Bedarfsrechnung steht — dieselbe Zahl wie in der Zusammenfassung.
            Assert.Equal(STROMBEDARF_MWH, d.Bedarf.StrombedarfGesamtMwh, 1);
            Assert.True(d.Bedarf.WaermebedarfGesamtMwh > 0.0);

            // … und es gibt kein Nullobjekt, das wie ein Ergebnis aussieht.
            Assert.Null(d.Uebersicht);
            Assert.Null(d.Kennzahlen);
        }

        // =================================================================================
        // 2 — Nach dem Lauf
        // =================================================================================

        /// <summary>
        /// Nach dem Lauf trägt die Übersicht den Strombedarf — dieselbe Zahl wie die
        /// Bedarfsrechnung (1030 führt weder Wärmepumpe noch Heizstab, der
        /// Eigenverbrauch ist dort 0).
        /// </summary>
        [Fact]
        public async Task Nach_dem_Lauf_traegt_die_Uebersicht_den_Strombedarf()
        {
            using var kopie = new TestDatenbank();
            if (!kopie.Vorhanden) return;

            SimulationErgebnisDienste dienste = Ergebnisdienste(PROJEKT_GANGLINIE);

            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);

            SimulationErgebnisDaten d = dienste.Laden(PROJEKT_GANGLINIE);

            Assert.Equal(ErgebnisZustand.Gueltig, d.Zustand);
            Assert.True(d.ErgebnisGueltig);
            Assert.Equal("", d.Zustandsgrund);

            Assert.NotNull(d.Uebersicht);
            Assert.True(d.Uebersicht.StrombedarfVorhanden);
            Assert.Equal(STROMBEDARF_MWH, d.Uebersicht.StrombedarfMwh, 1);
            Assert.Equal(STROMBEDARF_MWH, d.Bedarf.StrombedarfGesamtMwh, 1);
        }

        // =================================================================================
        // 3 — Veraltet: der Besuch der Stromspeicher-Auslegung
        // =================================================================================

        /// <summary>
        /// <b>Der Weg des Anwenders.</b> Wer nach dem Lauf in der Stromspeicher-Auslegung
        /// etwas speichert, macht das angezeigte Ergebnis VERALTET —
        /// <c>LaufGerechnet</c> bleibt dabei wahr, und der Startreiter montiert seine
        /// rechte Spalte deshalb weiter. Sie darf dort kein Nullobjekt zeichnen: Der
        /// Stand trägt den Zustand „veraltet" samt Anlass, die Bedarfszahlen stehen
        /// weiter, die Übersicht des überholten Laufs nicht mehr.
        /// </summary>
        [Fact]
        public async Task Nach_dem_Speichern_in_der_Auslegung_ist_das_Ergebnis_veraltet()
        {
            using var kopie = new TestDatenbank();
            if (!kopie.Vorhanden) return;

            SimulationErgebnisDienste dienste = Ergebnisdienste(PROJEKT_GANGLINIE);

            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            Assert.True(dienste.Laden(PROJEKT_GANGLINIE).ErgebnisGueltig);

            // Derselbe Weg, den der Reiter „Stromspeicher" und die Ansicht
            // STROMSPEICHER_AUSLEGUNG gehen — die Eingaben unverändert zurückgeschrieben.
            SpeicherOptimierungEingaben eingaben = dienste.OptimierungVorgaben().Eingaben;
            string fehler = await dienste.OptimierungEinstellungenSpeichern(eingaben);
            Assert.True(string.IsNullOrEmpty(fehler), fehler);

            SimulationErgebnisDaten d = dienste.Laden(PROJEKT_GANGLINIE);

            Assert.Equal(ErgebnisZustand.Veraltet, d.Zustand);
            Assert.False(d.ErgebnisGueltig);
            Assert.NotEqual("", d.Zustandsgrund);

            // Kein Nullobjekt — und der Bedarf steht weiter.
            Assert.Null(d.Uebersicht);
            Assert.Equal(STROMBEDARF_MWH, d.Bedarf.StrombedarfGesamtMwh, 1);
        }

        // =================================================================================
        // 4 — Das Projekt des Anwenders: nur Ganglinie, ein Speicher, kein Wärmeerzeuger
        // =================================================================================

        /// <summary>
        /// <b>Die zweite Reproduktion (Auftrag #236, A.3).</b> Das gemeldete Projekt
        /// „Stromspeicher Optimierung - ein Speicher" führt Technologie Stromspeicher,
        /// Wärmebedarf 0 und einen Strombedarf aus einer eingelesenen Ganglinie. Ein
        /// solches Projekt entsteht hier in der ARBEITSKOPIE aus 1030: Projektzeile und
        /// Einstellungen übernommen, die vier Kaskadenplätze geleert, die Ganglinie
        /// behalten, eine Speicheranlage und ein Flottenstand mit EINER Einheit dazu.
        ///
        /// <para><b>Ergebnis: der Lauf geht durch.</b> Danach nennt die Übersicht den
        /// Strombedarf der Ganglinie, und der Wärmebedarf ist 0 — der Fehler des
        /// Anwenders lag nicht am Rechenweg, sondern an der Anzeige.</para>
        /// </summary>
        [Fact]
        public async Task Ein_Speicherprojekt_mit_Ganglinie_rechnet_und_zeigt_den_Bedarf()
        {
            using var kopie = new TestDatenbank();
            if (!kopie.Vorhanden) return;

            const int projekt = 236001;
            SpeicherprojektAnlegen(projekt);

            SimulationErgebnisDienste dienste = Ergebnisdienste(projekt);

            // Vor dem Lauf: der Bedarf steht, die Übersicht nicht.
            SimulationErgebnisDaten vorher = dienste.Laden(projekt);
            Assert.Equal(ErgebnisZustand.NichtGerechnet, vorher.Zustand);
            Assert.Null(vorher.Uebersicht);
            Assert.Equal(STROMBEDARF_MWH, vorher.Bedarf.StrombedarfGesamtMwh, 1);
            Assert.Equal(0.0, vorher.Bedarf.WaermebedarfGesamtMwh, 3);
            Assert.True(vorher.ReiterStromspeicher);

            FlotteMitEinerEinheitSpeichern(dienste);

            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);

            SimulationErgebnisDaten d = dienste.Laden(projekt);

            Assert.Equal(ErgebnisZustand.Gueltig, d.Zustand);
            Assert.NotNull(d.Uebersicht);
            Assert.True(d.Uebersicht.StrombedarfVorhanden);
            Assert.Equal(STROMBEDARF_MWH, d.Uebersicht.StrombedarfMwh, 1);
            Assert.True(d.Speicher.FlotteImProjektAktiv);
        }

        // =================================================================================
        // Helfer
        // =================================================================================

        private static SimulationErgebnisDienste Ergebnisdienste(int idProjekt)
        {
            SimulationAnsichtQuelle quelle = new SimulationAnsichtQuelle(new BedarfsZustand(), null);
            IReadOnlyDictionary<string, object> gaben = quelle.AnsichtGaben(idProjekt, "Prüfprojekt");

            SimulationAnsichtDienste dienste = (SimulationAnsichtDienste)gaben["Dienste"];
            return (SimulationErgebnisDienste)dienste.Ergebnis["Dienste"];
        }

        /// <summary>
        /// Legt in der Arbeitskopie ein Projekt an, das nur eine Stromganglinie und eine
        /// Speicheranlage führt — abgeleitet aus 1030, mit leeren Kaskadenplätzen.
        /// </summary>
        private static void SpeicherprojektAnlegen(int projekt)
        {
            // Dieselbe Klimaregion wie 1030: Klimadaten und Solarstunden hängen an der
            // REGION, nicht am Projekt.
            Sql("INSERT INTO Tab_Projekt (ID,Projektname,ID_Klimaregion) " +
                "SELECT ?,?,ID_Klimaregion FROM Tab_Projekt WHERE ID=?",
                new("@neu", projekt), new("@name", "Stromspeicher Optimierung - ein Speicher"),
                new("@quelle", PROJEKT_GANGLINIE));

            // Einstellungen mit LEEREN Kaskadenplätzen; einzige Technologie ist der Speicher.
            Sql("INSERT INTO Tab_Einstellungen (ID,ID_Projekt,BHKW_Grenzleistung,Netzverluste," +
                "NetzverlusteEinheit,WP_Heizstab,Kessel_Betriebsbereitschaft," +
                "Tool_1,Tool_2,Tool_3,Tool_4,Tool_5,Tool_6," +
                "Ladefuellstand_Min,Ladefuellstand_Max,Ladeleistung_Max,Ladeschwellwert," +
                "Betriebsart,Leistungsgrenze) " +
                "SELECT ?,?,0,0,NetzverlusteEinheit,0,0,'','','','','',?,0,100,0,0,0,30 " +
                "FROM Tab_Einstellungen WHERE ID_Projekt=?",
                new("@id", projekt + 100), new("@neu", projekt),
                new("@sp", DbWerte.ERZEUGER_STROMSPEICHER), new("@quelle", PROJEKT_GANGLINIE));

            // Die eingelesene Stromganglinie — dieselbe Reihe, eine neue Zuordnung.
            Sql("INSERT INTO Z_ProjektStromganglinie (ID,ID_Projekt,ID_Ganglinie,Bezeichner) " +
                "SELECT ?,?,ID_Ganglinie,Bezeichner FROM Z_ProjektStromganglinie WHERE ID_Projekt=?",
                new("@id", projekt + 200), new("@neu", projekt),
                new("@quelle", PROJEKT_GANGLINIE));

            // EINE Speicheranlage. Die Geräteverweise stehen ausdrücklich auf NULL: Ihre
            // Vorgabe ist 0, und 0 gibt es in keiner der Zieltabellen (Fremdschlüssel).
            Sql("INSERT INTO Tab_Energieanlagen (ID,ID_Projekt,Bezeichner,ID_Type," +
                "ID_WP,ID_Kessel,ID_BHKW,ID_PV,ID_Solar,ID_SP,ID_PUFFER) " +
                "VALUES (?,?,?,?,NULL,NULL,NULL,NULL,NULL,NULL,NULL)",
                new("@anlage", projekt + 300), new("@neu", projekt),
                new("@name", "Speicher A"), new("@typ", WizardItemClass.SP_TYP));
        }

        /// <summary>
        /// Nimmt die erste Flotteneinheit des Prüfprojekts 1046 und speichert sie als
        /// EINZIGE Einheit des neuen Projekts — derselbe Schreibweg wie die Ansicht
        /// „Stromspeicher-Auslegung".
        /// </summary>
        private static void FlotteMitEinerEinheitSpeichern(SimulationErgebnisDienste dienste)
        {
            SpeicherOptimierungEingaben vorlage =
                Ergebnisdienste(PROJEKT_FLOTTE).OptimierungVorgaben().Eingaben;
            Assert.NotNull(vorlage.Auslegung.Flotte);
            Assert.NotEmpty(vorlage.Auslegung.Flotte.Einheiten);

            SpeicherOptimierungEingaben eingaben = dienste.OptimierungVorgaben().Eingaben.Kopie();
            eingaben.Auslegung.Flotte = vorlage.Auslegung.Flotte;
            while (eingaben.Auslegung.Flotte.Einheiten.Count > 1)
                eingaben.Auslegung.Flotte.Einheiten.RemoveAt(1);
            eingaben.Auslegung.FlottenProjektbetriebDeaktiviert = false;
            eingaben.Auslegung.FlottenGroessenOptimieren = false;

            string fehler = dienste.OptimierungEinstellungenSpeichern(eingaben).GetAwaiter().GetResult();
            Assert.True(string.IsNullOrEmpty(fehler), fehler);
        }

        private static void Sql(string sql, params DbParam[] parameter)
        {
            using var db = DataRepository.Vorgang();
            db.Ausfuehren(sql, parameter);
            db.Commit();
        }
    }
}
