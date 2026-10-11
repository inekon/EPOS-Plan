using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Allgemein;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Gleichstand der Lade-Priorität</b> nach der Flottenübernahme (Anwenderentscheid
    /// 07.10.2026): Jedes weitere Stück einer vertretenen Anlage kopiert deren Senkenzeilen —
    /// Quelle und Kopie stehen danach mit DERSELBEN <c>Ladeprio</c> am selben Puffer.
    ///
    /// <para><b>Befund.</b> Die Ladefolge eines Puffers (<see cref="Ladeordnung.SortierenNachLadeprio"/>,
    /// dieselbe Implementierung für Anzeige und Engine — <c>SimulationControl</c> sortiert
    /// <c>LadenOhnePV</c> und <c>LadenMitPV</c> damit) löst einen Gleichstand über die Kette
    /// Ladepriorität → Kaskadenposition → Anlagenpriorität → Anlagen-ID → Senkenrang auf. Die
    /// Kette endet an der eindeutigen Anlagen-ID: Die Folge ist eine totale Ordnung, unabhängig
    /// von Lese- und Listenreihenfolge, und die Quelle (kleinere ID) lädt vor der Kopie. Den
    /// VORRANG (Obergrenze Schwelle_Aus) bekommen beide, denn er hängt an der kleinsten Zahl,
    /// nicht an der ersten Zeile (<see cref="Ladeordnung.ObergrenzenAufloesen"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class LadeprioGleichstandTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Referenzprojekt 1030, sein Kessel (Ladeprio 3) und dessen Puffer.</summary>
        private const int PROJEKT = 1030, QUELLE = 11334, PUFFER = 1054170;

        /// <summary>
        /// Ohne Datenbank: Zwei Einträge mit gleicher Priorität, gleicher Kaskadenposition und
        /// gleicher Anlagenpriorität ordnen sich in JEDER Eingangsreihenfolge gleich — nach ID.
        /// </summary>
        [Fact]
        public void Gleichstand_ordnet_unabhaengig_von_der_Eingangsreihenfolge_nach_ID()
        {
            Ladeordnung.LadeEintrag Eintrag(int id) => new Ladeordnung.LadeEintrag
            {
                ID_Anlage = id, ID_Type = 1, Ladeprio = 3, Rang = 1, Kaskadenposition = 2
            };

            var vor = new List<Ladeordnung.LadeEintrag> { Eintrag(10357), Eintrag(20001) };
            var rueck = new List<Ladeordnung.LadeEintrag> { Eintrag(20001), Eintrag(10357) };
            Ladeordnung.SortierenNachLadeprio(vor, e => e.Ladeprio);
            Ladeordnung.SortierenNachLadeprio(rueck, e => e.Ladeprio);

            Assert.Equal(new[] { 10357, 20001 }, vor.Select(e => e.ID_Anlage));
            Assert.Equal(new[] { 10357, 20001 }, rueck.Select(e => e.ID_Anlage));
        }

        /// <summary>
        /// Mit Datenbank: Der Kessel 11334 des Referenzprojekts 1030 (Ladeprio 3 am Puffer
        /// 1054170) wird mit seinen Kindzeilen kopiert (<see cref="AnlagenFachspalten.AnlagenkinderKopieren"/>,
        /// derselbe Weg wie die weiteren Stücke der Flottenübernahme). Die Ladefolge ist in zwei
        /// Läufen und bei umgekehrt gelesenen Senkenlisten gleich, die Quelle steht vor der
        /// Kopie, und beide tragen denselben Vorrang und dieselbe Obergrenze.
        /// </summary>
        /// <remarks>Ein Stromspeicher steht nie in einer Puffer-Ladefolge: Die Senkenlisten der
        /// Engine lesen nur Wärmeerzeuger (<c>ProjektPuffer.WAERMEERZEUGER_TYPEN</c>). Deshalb hier
        /// ein Wärmeerzeuger.</remarks>
        [Fact]
        public void Quelle_und_Kopie_mit_gleicher_Ladeprio_laden_deterministisch()
        {
            if (!_db.Vorhanden) return;

            List<string> spalten = DataRepository.SpaltenVonTabelle("Tab_Energieanlagen")
                .Where(x => !string.Equals(x, "ID", StringComparison.OrdinalIgnoreCase))
                .Select(x => "[" + x + "]").ToList();
            // Das Kesselgerät ist je Projekt eindeutig (idx_Anlage_ID_Kessel): Die Kopie
            // bleibt ohne Gerät - für die Ladefolge zählen Typ, Senke und Prioritäten.
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Energieanlagen (" + string.Join(", ", spalten) + ") SELECT " +
                string.Join(", ", spalten.Select(x => x == "[ID_Kessel]" ? "NULL" : x)) +
                " FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@q", QUELLE)));
            int kopie = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)),
                CultureInfo.InvariantCulture);
            Assert.True(kopie > QUELLE);
            DbVorgang v = DataRepository.Vorgang();
            try
            {
                Assert.True(AnlagenFachspalten.AnlagenkinderKopieren(v, QUELLE, kopie) > 0);
                v.Commit();
            }
            finally { v.Dispose(); }

            List<Ladeordnung.LadeEintrag> lauf1 = Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER);
            List<Ladeordnung.LadeEintrag> lauf2 = Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER);
            Assert.Equal(Text(lauf1), Text(lauf2));

            Ladeordnung.LadeEintrag q = Assert.Single(lauf1, x => x.ID_Anlage == QUELLE);
            Ladeordnung.LadeEintrag k = Assert.Single(lauf1, x => x.ID_Anlage == kopie);
            Assert.Equal(3, q.Ladeprio);
            Assert.Equal(q.Ladeprio, k.Ladeprio);
            Assert.Equal(lauf1.IndexOf(q) + 1, lauf1.IndexOf(k));
            Assert.Equal(q.Vorrangig, k.Vorrangig);
            Assert.Equal(q.Obergrenze, k.Obergrenze);

            // Dieselbe Folge, wenn die Senkenlisten in umgekehrter Reihenfolge ankommen.
            List<Senkenliste> senken = WaermesenkeClass.SenkenlistenLadenStill(PROJEKT);
            senken.Reverse();
            Assert.Equal(Text(lauf1), Text(Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER, senken)));
        }

        private static List<string> Text(List<Ladeordnung.LadeEintrag> liste) =>
            liste.Select(x => string.Join("|", x.ID_Anlage, x.Rang, x.Ladeprio,
                x.Obergrenze.ToString("R", CultureInfo.InvariantCulture), x.Vorrangig)).ToList();
    }
}
