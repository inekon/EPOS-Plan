using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kommt die Speicherflotte in die Variante?</b> (Anwenderbefund 17.09.2026:
    /// „Der Speicher wird bei ‚Simulation durchführen' berücksichtigt, nicht aber in der
    /// Wirtschaftlichkeit im Vergleich mit einer Variante.")
    ///
    /// <para><b>Warum das eine Probe braucht.</b> Eine Variante ist eine vollständige
    /// Projektkopie (<see cref="VariantenCtrl"/> über <see cref="ProjektDuplizierenCtrl"/>),
    /// und die Wirtschaftlichkeit rechnet JEDES Projekt der Vergleichsgruppe für sich neu.
    /// Ob die Variante mit oder ohne Speicher rechnet, entscheidet allein ihre EIGENE
    /// Zeile <c>Tab_SpeicherAuslegung.@Projektflotte</c>. Fehlte sie in der Kopie, zeigte
    /// die Variante stumm die Zahlen ohne Speicher — und niemand sähe den Grund.</para>
    ///
    /// <para><b>Geprüft wird der KOPIERMOMENT und die UNABHÄNGIGKEIT danach</b>
    /// (Anwendereinwand 17.09.2026). Unmittelbar nach dem Duplizieren trägt die Kopie den
    /// Flottenstand des Quellprojekts, mit versetzten Anlagen-Ids — das ist die Zusicherung.
    /// <b>Sie gilt nur für diesen einen Augenblick.</b> Stamm und Varianten dürfen danach
    /// verschiedene Flotten führen, verschiedene Betriebsziele, verschiedene Kosten, und
    /// jede aktiviert oder deaktiviert ihre Flotte für sich. Keine Probe hier — und nichts
    /// im Programm — erzwingt spätere Gleichheit; die Antwort darauf ist der
    /// Speicherkontext je Vergleichsspalte
    /// (<see cref="SpeicherAnzeigeCtrl.SpeicherKontextText"/>), der sagt, womit jede Spalte
    /// gerechnet hat.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VarianteSpeicherflotteTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Prüfprojekt der Mehrspeicherrechnung — es führt <c>@Projektflotte</c>.</summary>
        private const int StammId = 1046;

        private readonly TestDatenbank _db;

        public VarianteSpeicherflotteTests(TestDatenbank db) { _db = db; }

        private static string Stammname() => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", StammId)));

        private static int VarianteAnlegen(string bezeichner)
        {
            string fehler;
            int neu = new VariantenCtrl().AnlegenAusStamm(StammId, Stammname(), bezeichner, out fehler);
            Assert.True(neu > 0, "Die Variante konnte nicht angelegt werden: " + fehler);
            return neu;
        }

        /// <summary>Die Bezugsspitze des Laufs [kW] — das Maß, an dem die Flotte sichtbar wird.</summary>
        private static double BezugsspitzeKw(int projektId)
        {
            var runner = new SimulationRunner();
            string fehler;
            Assert.True(runner.Simuliere(projektId, out fehler), "Der Lauf scheiterte: " + fehler);
            double[] rest = runner.sim.Rest_Strombedarf_viertelstuendlich;
            Assert.NotNull(rest);
            return rest.Max();
        }

        /// <summary>Die Speicheranlagen eines Projekts, aufsteigend.</summary>
        private static List<int> Speicheranlagen(int projektId)
        {
            var ids = new List<int>();
            foreach (DataRow r in DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
                new DbParam("@p", projektId),
                new DbParam("@t", WizardItemClass.SP_TYP)).Rows)
                ids.Add(Convert.ToInt32(r["ID"]));
            return ids;
        }

        /// <summary>Der rohe Flottenstand eines Projekts — die Messlatte für „unberührt".</summary>
        private static string Flottenstand(int projektId) => Convert.ToString(
            DataRepository.ExecuteScalar(
                "SELECT Daten FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", projektId),
                new DbParam("@b", SpeicherFlottenProjektCtrl.ProjektflottenStand)));

        private static void FlottenstandZurueckschreiben(int projektId, string daten)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_SpeicherAuslegung SET Daten = ? WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@d", daten), new DbParam("@p", projektId),
                new DbParam("@b", SpeicherFlottenProjektCtrl.ProjektflottenStand));
        }

        private static SpeicherOptimierungEingaben Projektflotte(int projektId) =>
            SpeicherAuslegungCtrl.Profile(projektId, 0)
                .First(p => p.Name == SpeicherFlottenProjektCtrl.ProjektflottenStand).Eingaben;

        // =====================================================================
        //  Der Kopiermoment
        // =====================================================================

        /// <summary>
        /// DER REGELWEG, im Augenblick des Kopierens: Die frisch angelegte Variante trägt
        /// den Flottenstand des Quellprojekts — Zeile, Einheiten, Betriebsziel, Peak-Ziel —
        /// und rechnet deshalb DIESELBE Bezugsspitze wie der Stamm.
        ///
        /// <para><b>Was diese Probe NICHT sagt.</b> Sie sagt nichts über später. Sobald der
        /// Anwender eine der beiden Flotten anfasst, laufen sie auseinander — das ist der
        /// Zweck einer Variante, kein Fehler. Die Unabhängigkeit prüft
        /// <see cref="Stamm_und_Variante_fuehren_ihre_Flotte_unabhaengig_voneinander"/>.</para>
        /// </summary>
        [Fact]
        public void Unmittelbar_nach_dem_Duplizieren_traegt_die_Kopie_den_Flottenstand_der_Quelle()
        {
            if (!_db.Vorhanden) return;

            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(StammId),
                "Vorbedingung: Der Stamm 1046 führt eine aktivierte Projektflotte.");
            FlottenStudieKonfiguration stamm = SpeicherFlottenProjektCtrl.AktiveKonfiguration(StammId);

            int variante = VarianteAnlegen("Flottenprobe");

            // 1. Die Zeile ist da — je Projekt genau eine, mit demselben Bezeichner.
            DataTable zeilen = DataRepository.GetDataTable(
                "SELECT ID_Projekt, Bezeichner FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?",
                new DbParam("@p", variante));
            Assert.Equal(1, zeilen.Rows.Count);
            Assert.Equal(SpeicherFlottenProjektCtrl.ProjektflottenStand,
                         Convert.ToString(zeilen.Rows[0]["Bezeichner"]));

            // 2. Sie ist in der Kopie AKTIV und trägt denselben Flottenstand.
            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(variante));
            FlottenStudieKonfiguration kopie = SpeicherFlottenProjektCtrl.AktiveKonfiguration(variante);
            Assert.NotNull(kopie);
            Assert.Equal(stamm.Einheiten.Count, kopie.Einheiten.Count);
            Assert.Equal(stamm.Optionen.Betriebsziel, kopie.Optionen.Betriebsziel);
            Assert.Equal(stamm.Optionen.Verteilung, kopie.Optionen.Verteilung);
            Assert.Equal(stamm.Optionen.WirtschaftlicherPeakZielwertKw,
                         kopie.Optionen.WirtschaftlicherPeakZielwertKw);
            Assert.True(SpeicherFlottenProjektCtrl.Pruefe(variante).Rechenbar);

            // 3. Und sie WIRKT: derselbe Lauf, dieselbe Bezugsspitze — solange niemand
            //    eine der beiden Flotten angefasst hat.
            Assert.Equal(BezugsspitzeKw(StammId), BezugsspitzeKw(variante), 6);
        }

        /// <summary>
        /// DER NACHZUG (Auftrag VF-1): Der Kopierlauf versetzt jede ID-Spalte; die
        /// Kennungen INNERHALB des Flotten-JSON versetzt seither
        /// <c>SpeicherAuslegungCtrl.KopieBezuegeNachziehen</c>. Eine Einheit, die eine
        /// Projektanlage vertritt (<c>FlottenEinheit.AnlageId</c>), zeigt in der Kopie
        /// deshalb auf die Anlage der KOPIE.
        ///
        /// <para><b>Wo es sichtbar wird.</b> Der Lauf selbst bleibt unberührt — die
        /// Einheiten tragen ihre Physik und ihre Kosten selbst. Gelesen wird
        /// <c>AnlageId</c> von der Oberfläche und von der Übernahme in Projektanlagen: Sie
        /// fand die fremde Anlage im eigenen Projekt nicht (<c>Geraetezeile</c> = 0) und
        /// legte eine ZWEITE Anlage an, statt die vorhandene zu ändern. Genau das prüft
        /// der zweite Teil.</para>
        /// </summary>
        [Fact]
        public void Variante_versetzt_die_AnlageId_der_Flotteneinheiten()
        {
            if (!_db.Vorhanden) return;

            // Die Speicheranlagen des Stamms; die Einheiten bekommen die ersten beiden.
            List<int> anlagen = Speicheranlagen(StammId);
            Assert.True(anlagen.Count >= 2, "Vorbedingung: Der Stamm führt mindestens zwei Speicheranlagen.");

            string sicherung = Flottenstand(StammId);
            try
            {
                SpeicherOptimierungEingaben eingaben = Projektflotte(StammId);
                IList<FlottenEinheit> einheiten = eingaben.Auslegung.Flotte.Einheiten;
                Assert.True(einheiten.Count >= 2);
                for (int i = 0; i < 2; i++) einheiten[i].AnlageId = anlagen[i].ToString();
                SpeicherAuslegungCtrl.Speichern(StammId, 0,
                    SpeicherFlottenProjektCtrl.ProjektflottenStand, eingaben);

                int variante = VarianteAnlegen("Anlagenbezug");

                // Die Kopie hat EIGENE Speicheranlagen — keine davon ist eine des Stamms.
                List<int> eigene = Speicheranlagen(variante);
                Assert.Equal(anlagen.Count, eigene.Count);
                Assert.Empty(eigene.Intersect(anlagen));

                // 1. Die AnlageId der Einheiten zeigt auf die Anlagen der KOPIE.
                FlottenStudieKonfiguration kopie = SpeicherFlottenProjektCtrl.AktiveKonfiguration(variante);
                Assert.NotNull(kopie);
                Assert.Equal(eigene[0].ToString(), kopie.Einheiten[0].AnlageId);
                Assert.Equal(eigene[1].ToString(), kopie.Einheiten[1].AnlageId);

                // 2. Und deshalb ÄNDERT die Übernahme die vorhandenen Anlagen, statt
                //    zweite anzulegen — der Befund, an dem der fehlende Versatz auffiel.
                int vorher = Speicheranlagen(variante).Count;
                FlottenUebernahmeErgebnis ergebnis =
                    SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                        variante, new List<FlottenEinheit> { kopie.Einheiten[0], kopie.Einheiten[1] });
                Assert.True(ergebnis.Erfolg, ergebnis.Meldung);
                Assert.Equal(0, ergebnis.Angelegt);
                Assert.Equal(2, ergebnis.Geaendert);
                Assert.Equal(vorher, Speicheranlagen(variante).Count);
            }
            finally { FlottenstandZurueckschreiben(StammId, sicherung); }
        }

        // =====================================================================
        //  Die Unabhängigkeit danach
        // =====================================================================

        /// <summary>
        /// <b>Nach dem Kopieren gehört jede Flotte ihrem Projekt</b> (Anwendereinwand
        /// 17.09.2026). Eine Variante darf eine ANDERE Flotte führen als ihr Stamm — ein
        /// anderes Peak-Ziel, andere Einheiten, gar keine —, und das Aktivieren und
        /// Deaktivieren wirkt je Projekt.
        ///
        /// <para>Die Probe prüft beide Richtungen: Änderungen an der Variante lassen den
        /// Stamm unberührt, und umgekehrt. Gemessen wird am ROHEN Flottenstand der
        /// Gegenseite — wäre dort auch nur ein Feld mitgewandert, wäre der Text ein
        /// anderer.</para>
        /// </summary>
        [Fact]
        public void Stamm_und_Variante_fuehren_ihre_Flotte_unabhaengig_voneinander()
        {
            if (!_db.Vorhanden) return;
            Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(StammId),
                "Vorbedingung: Der Stamm 1046 führt eine aktivierte Projektflotte.");

            string sicherung = Flottenstand(StammId);
            try
            {
                int eine = VarianteAnlegen("Unabhängig A");
                int andere = VarianteAnlegen("Unabhängig B");
                int stammEinheiten = SpeicherFlottenProjektCtrl.AktiveKonfiguration(StammId).Einheiten.Count;

                // 1. Die Variante wird DEAKTIVIERT — der Stamm rechnet weiter mit Flotte.
                SpeicherFlottenProjektCtrl.Deaktivieren(eine);
                Assert.False(SpeicherFlottenProjektCtrl.IstAktiv(eine));
                Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(StammId));
                Assert.Equal(sicherung, Flottenstand(StammId));
                Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(andere),
                    "Die zweite Variante hat mit der ersten nichts zu tun.");

                // 2. Die andere Variante bekommt eine ANDERE Flotte: ein eigenes Peak-Ziel
                //    und eine Einheit weniger. Der Stamm bleibt, wie er war.
                SpeicherOptimierungEingaben andersherum = Projektflotte(andere);
                andersherum.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw = 12.25;
                if (andersherum.Auslegung.Flotte.Einheiten.Count > 1)
                    andersherum.Auslegung.Flotte.Einheiten.RemoveAt(
                        andersherum.Auslegung.Flotte.Einheiten.Count - 1);
                SpeicherAuslegungCtrl.Speichern(andere, 0,
                    SpeicherFlottenProjektCtrl.ProjektflottenStand, andersherum);

                FlottenStudieKonfiguration geaendert = SpeicherFlottenProjektCtrl.AktiveKonfiguration(andere);
                Assert.Equal(12.25, geaendert.Optionen.WirtschaftlicherPeakZielwertKw);
                Assert.Equal(sicherung, Flottenstand(StammId));
                Assert.Equal(stammEinheiten,
                    SpeicherFlottenProjektCtrl.AktiveKonfiguration(StammId).Einheiten.Count);

                // 3. UMGEKEHRT: Der Stamm wird deaktiviert — die Variante merkt nichts.
                SpeicherFlottenProjektCtrl.Deaktivieren(StammId);
                Assert.False(SpeicherFlottenProjektCtrl.IstAktiv(StammId));
                Assert.True(SpeicherFlottenProjektCtrl.IstAktiv(andere));
                Assert.Equal(12.25, SpeicherFlottenProjektCtrl
                    .AktiveKonfiguration(andere).Optionen.WirtschaftlicherPeakZielwertKw);
            }
            finally { FlottenstandZurueckschreiben(StammId, sicherung); }
        }
    }
}
