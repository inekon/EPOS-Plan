using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>NEBENBEFUND ZU #363 (19.09.2026): stehen die Vorlagenpositionen zweimal?</b>
    /// Im Bildschirmfoto des Anwenders trug das Betriebsraster eines Heizkessels die
    /// drei Positionen „Vollwartung", „Instandhaltung" und „Hilfsenergiekosten"
    /// zweimal.
    ///
    /// <para><b>Was hier gepinnt wird.</b> Die Übernahme legt KEINE Dublette an:
    /// Der Dublettencheck läuft je Anlage über die StammID des Positionslexikons
    /// (<c>KostenVorlagenUebernahmeCtrl.StammIdSicher</c> bildet denselben Namen
    /// immer auf dieselbe StammID ab), also übergeht ein zweiter Lauf jede Zeile, die
    /// schon steht — und eine ZWEITE Anlage derselben Komponente bekommt trotzdem
    /// ihre eigenen Zeilen.</para>
    ///
    /// <para><b>Und wie es trotzdem doppelt aussehen kann.</b> Das Raster „ohne
    /// Anlagenzuordnung" (<c>KostenProjektPositionenCtrl.Lies</c> mit
    /// <c>idAnlage</c> 0) sammelt die Zeilen OHNE Anlage UND die mit einem Verweis
    /// auf eine gelöschte Anlage. Waren es zwei gelöschte Anlagen, stehen ihre Sätze
    /// dort nebeneinander — dieselben Namen zweimal, ohne dass je eine Dublette
    /// entstanden wäre. Der dritte Fall hält genau das fest, damit die Stelle bekannt
    /// bleibt.</para>
    ///
    /// <para>Eigene Arbeitskopie je Fall (<see cref="TestDatenbank"/>): alle drei
    /// SCHREIBEN.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenUebernahmeDublettenTests
    {
        private const int PROJEKT = 1030;
        private const int KOMPONENTE_BHKW = 7;
        private const int KATEGORIE_BETRIEB = 2;

        /// <summary>Die zwei BHKW-Anlagen des Projekts — jede führt ihre eigenen
        /// Betriebszeilen.</summary>
        private const int ANLAGE_EINS = 14920;
        private const int ANLAGE_ZWEI = 14921;

        /// <summary>
        /// Zweimal übernehmen legt nichts doppelt an: Der zweite Lauf meldet jede
        /// Position als „bereits vorhanden", und die Zeilenzahl der Anlage bleibt.
        /// </summary>
        [Fact]
        public void Zweimal_uebernehmen_legt_keine_Dublette_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenVorlageKopf vorlage = Standardvorlage();
            int vorher = Zeilen(ANLAGE_EINS).Count;

            UebernahmeErgebnis e =
                KostenVorlagenUebernahmeCtrl.AusVorlage(PROJEKT, vorlage, ANLAGE_EINS);

            Assert.False(e.Fehler);
            Assert.Equal(vorher, Zeilen(ANLAGE_EINS).Count - e.Angelegt);
            Assert.True(e.Uebersprungen > 0, "Keine einzige Zeile wurde als vorhanden erkannt.");

            // Und noch einmal: jetzt steht alles, also entsteht nichts mehr.
            int stand = Zeilen(ANLAGE_EINS).Count;
            UebernahmeErgebnis zweite =
                KostenVorlagenUebernahmeCtrl.AusVorlage(PROJEKT, vorlage, ANLAGE_EINS);

            Assert.Equal(0, zweite.Angelegt);
            Assert.Equal(stand, Zeilen(ANLAGE_EINS).Count);
            Assert.Empty(Doppelte(ANLAGE_EINS));
        }

        /// <summary>
        /// Der Check läuft JE ANLAGE: Fehlen die Zeilen der zweiten Anlage, entstehen
        /// sie — und die der ersten bleiben unberührt. Ohne diese Trennung bekäme die
        /// zweite Anlage nichts, weil die Namen im Projekt schon vorkommen.
        /// </summary>
        [Fact]
        public void Die_zweite_Anlage_bekommt_ihre_eigenen_Zeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = ? " +
                "AND KomponentenID = ? AND ID_Anlage = ?",
                new DbParam("@p", PROJEKT), new DbParam("@g", KATEGORIE_BETRIEB),
                new DbParam("@k", KOMPONENTE_BHKW), new DbParam("@a", ANLAGE_ZWEI));
            Assert.Empty(Zeilen(ANLAGE_ZWEI));

            int ersteVorher = Zeilen(ANLAGE_EINS).Count;

            UebernahmeErgebnis e =
                KostenVorlagenUebernahmeCtrl.AusVorlage(PROJEKT, Standardvorlage(), ANLAGE_ZWEI);

            Assert.False(e.Fehler);
            Assert.True(e.Angelegt > 0, "Die zweite Anlage bekam nichts.");
            Assert.Equal(ersteVorher, Zeilen(ANLAGE_EINS).Count);
            Assert.Empty(Doppelte(ANLAGE_ZWEI));
        }

        /// <summary>
        /// DIE EINE LAGE, IN DER DIESELBEN NAMEN ZWEIMAL IN EINEM RASTER STEHEN: Die
        /// Zeilen zweier gelöschter Anlagen fallen beide in das Raster „ohne
        /// Anlagenzuordnung". Das ist keine Dublette — es sind die Sätze zweier
        /// Anlagen, die es nicht mehr gibt.
        /// </summary>
        [Fact]
        public void Verwaiste_Anlagenverweise_sammeln_sich_in_einem_Raster()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int eins = Zeilen(ANLAGE_EINS).Count;
            int zwei = Zeilen(ANLAGE_ZWEI).Count;
            Assert.True(eins > 0 && zwei > 0);

            // Beide Anlagen verschwinden — ihre Kostenzeilen bleiben stehen.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID IN (?, ?)",
                new DbParam("@a", ANLAGE_EINS), new DbParam("@b", ANLAGE_ZWEI));

            List<KostenProjektPositionenCtrl.Zeile> ohneZuordnung = Zeilen(0);
            Assert.Equal(eins + zwei, ohneZuordnung.Count);

            // Und dort stehen dieselben Bezeichnungen dann zweimal.
            Assert.NotEmpty(Doppelte(0));
        }

        // ----------------------------------------------------------- Helfer ---

        private static KostenVorlageKopf Standardvorlage()
        {
            foreach (KostenVorlageKopf v in
                     KostenVorlagenCtrl.Vorlagen(KOMPONENTE_BHKW, KATEGORIE_BETRIEB))
                if (v.IstStandard) return v;
            throw new InvalidOperationException("Keine Standardvorlage der Betriebskosten.");
        }

        private static List<KostenProjektPositionenCtrl.Zeile> Zeilen(int idAnlage)
        {
            return KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE_BHKW,
                                                    KATEGORIE_BETRIEB, idAnlage);
        }

        /// <summary>Bezeichnungen, die in diesem Raster mehr als einmal vorkommen.</summary>
        private static List<string> Doppelte(int idAnlage)
        {
            var gezaehlt = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KostenProjektPositionenCtrl.Zeile z in Zeilen(idAnlage))
            {
                string name = z.Raster.Bezeichnung ?? "";
                gezaehlt[name] = gezaehlt.TryGetValue(name, out int n) ? n + 1 : 1;
            }

            var doppelt = new List<string>();
            foreach (KeyValuePair<string, int> p in gezaehlt)
                if (p.Value > 1) doppelt.Add(p.Key);
            return doppelt;
        }
    }
}
