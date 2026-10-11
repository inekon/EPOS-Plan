using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Vergleich der Änderungsstempel</b> (<see cref="KostenAenderungsstempel"/>) und die Frage
    /// dahinter (<see cref="WirtschaftlichkeitCtrl.Veraltung"/>, <see cref="WirtschaftlichkeitCtrl.ErgebnisAktuell"/>).
    ///
    /// <para><b>Geprüft wird:</b> das Urteil ohne Datenbank (strikt jünger, dieselbe Sekunde, Vorrang);
    /// der Gruppenvergleich in der Testdatenbank (ein Stempel an der Variante macht die ganze Gruppe
    /// veraltet, der Katalog jedes Projekt; ohne Ergebnis kein Urteil); der Simulationslauf geht vor;
    /// <b>eine Rechnung stempelt sich nicht selbst</b> — auch wenn sie dabei den Gesetzeskatalog sät —,
    /// und eine Änderung danach stellt auf Kosten- und Wirtschaftlichkeitsseite das Band mit seinem
    /// Grund.</para>
    ///
    /// <para>Geschrieben wird nur an einer Arbeitskopie der Testdatenbank je Fall.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KostenAenderungsstempelTests : IDisposable
    {
        /// <summary>Vergleichsgruppe „Wöhler“: Stamm 1019, Varianten „Test1“ (1023) und „Test2“ (1024).</summary>
        private const int STAMM = 1019;
        private const int TEST1 = 1023;
        private const int TEST2 = 1024;

        /// <summary>Ein Projekt außerhalb der Gruppe.</summary>
        private const int FREMD = 1030;

        private static readonly DateTime ERGEBNIS = new DateTime(2026, 9, 30, 12, 0, 0);

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        // =============================================================================
        //  Teil 1 - das Urteil, ohne Datenbank
        // =============================================================================

        [Fact]
        public void Ohne_Stempel_ist_nichts_veraltet()
        {
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Urteil(Stempel(null, null), ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Urteil(null, ERGEBNIS));
        }

        [Fact]
        public void Ein_strikt_juengerer_Stempel_macht_das_Ergebnis_veraltet()
        {
            Assert.Equal(Ergebnisveraltung.Kosten,
                         KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS.AddSeconds(1), null), ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Katalog,
                         KostenAenderungsstempel.Urteil(Stempel(null, ERGEBNIS.AddSeconds(1)), ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Keine,
                         KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS.AddSeconds(-1), ERGEBNIS.AddDays(-3)), ERGEBNIS));
        }

        /// <summary>
        /// <b>Dieselbe Sekunde gilt als davor</b> — auch wenn der Zeitstempel des Ergebnisses im Speicher
        /// Bruchteile trägt: Stempel wie Ergebnis werden auf die Sekunde verglichen.
        /// </summary>
        [Fact]
        public void Gleichstand_in_derselben_Sekunde_ist_aktuell()
        {
            DateTime mitBruchteil = ERGEBNIS.AddMilliseconds(900);
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS, ERGEBNIS), ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS, ERGEBNIS), mitBruchteil));
            Assert.Equal(Ergebnisveraltung.Kosten,
                         KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS.AddSeconds(1), null), mitBruchteil));
        }

        [Fact]
        public void Der_Gruppenstempel_geht_dem_Katalog_vor()
        {
            Assert.Equal(Ergebnisveraltung.Kosten,
                         KostenAenderungsstempel.Urteil(Stempel(ERGEBNIS.AddSeconds(5), ERGEBNIS.AddSeconds(9)), ERGEBNIS));
        }

        [Fact]
        public void Vorrang_ordnet_Simulation_vor_Kosten_vor_Katalog()
        {
            Assert.Equal(Ergebnisveraltung.Simulation,
                         KostenAenderungsstempel.Vorrang(Ergebnisveraltung.Kosten, Ergebnisveraltung.Simulation));
            Assert.Equal(Ergebnisveraltung.Kosten,
                         KostenAenderungsstempel.Vorrang(Ergebnisveraltung.Kosten, Ergebnisveraltung.Katalog));
            Assert.Equal(Ergebnisveraltung.Katalog,
                         KostenAenderungsstempel.Vorrang(Ergebnisveraltung.Keine, Ergebnisveraltung.Katalog));
            Assert.Equal(Ergebnisveraltung.Keine,
                         KostenAenderungsstempel.Vorrang(Ergebnisveraltung.Keine, Ergebnisveraltung.Keine));
        }

        [Fact]
        public void Nur_das_Stempelformat_zaehlt()
        {
            Assert.Equal(ERGEBNIS, KostenAenderungsstempel.Zeitpunkt("2026-09-30 12:00:00"));
            Assert.Null(KostenAenderungsstempel.Zeitpunkt("30.09.2026 12:00"));
            Assert.Null(KostenAenderungsstempel.Zeitpunkt(DBNull.Value));
            Assert.Null(KostenAenderungsstempel.Zeitpunkt(null));
        }

        // =============================================================================
        //  Teil 2 - der Gruppenvergleich in der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Ein Stempel an der VARIANTE macht die ganze Gruppe veraltet — Stamm und beide Varianten;
        /// ein Projekt außerhalb der Gruppe bleibt aktuell.
        /// </summary>
        [Fact]
        public void Ein_Stempel_an_der_Variante_macht_die_ganze_Gruppe_veraltet()
        {
            if (!_db.Vorhanden) return;

            Projektstempel(TEST1, ERGEBNIS.AddMinutes(5));
            foreach (int id in new[] { STAMM, TEST1, TEST2 })
                Assert.Equal(Ergebnisveraltung.Kosten, KostenAenderungsstempel.Pruefe(id, ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Pruefe(FREMD, ERGEBNIS));
            Assert.Null(KostenAenderungsstempel.Lesefehler);
        }

        /// <summary>Ein Stempel am STAMM gilt ebenso für jede Variante.</summary>
        [Fact]
        public void Ein_Stempel_am_Stamm_macht_jede_Variante_veraltet()
        {
            if (!_db.Vorhanden) return;

            Projektstempel(STAMM, ERGEBNIS.AddMinutes(5));
            Assert.Equal(Ergebnisveraltung.Kosten, KostenAenderungsstempel.Pruefe(TEST2, ERGEBNIS));
            Assert.Equal(ERGEBNIS.AddMinutes(5), KostenAenderungsstempel.Lies(TEST2).Gruppe);
        }

        /// <summary>Der Katalogstempel gilt für jedes Projekt — der Gruppenstempel geht ihm vor.</summary>
        [Fact]
        public void Der_Katalogstempel_gilt_fuer_jedes_Projekt()
        {
            if (!_db.Vorhanden) return;

            Katalogstempel(ERGEBNIS.AddMinutes(5));
            Assert.Equal(Ergebnisveraltung.Katalog, KostenAenderungsstempel.Pruefe(FREMD, ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Katalog, KostenAenderungsstempel.Pruefe(TEST1, ERGEBNIS));

            Projektstempel(TEST2, ERGEBNIS.AddMinutes(1));
            Assert.Equal(Ergebnisveraltung.Kosten, KostenAenderungsstempel.Pruefe(STAMM, ERGEBNIS));
            Assert.Equal(Ergebnisveraltung.Katalog, KostenAenderungsstempel.Pruefe(FREMD, ERGEBNIS));
        }

        /// <summary>Gleichstand auf die Sekunde: aktuell — auch über die Datenbank.</summary>
        [Fact]
        public void Gleichstand_in_derselben_Sekunde_ist_auch_in_der_Datenbank_aktuell()
        {
            if (!_db.Vorhanden) return;

            Projektstempel(TEST1, ERGEBNIS);
            Katalogstempel(ERGEBNIS);
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Pruefe(STAMM, ERGEBNIS.AddMilliseconds(400)));
        }

        /// <summary>Ohne Ergebnis kein Urteil und kein Band — <c>ErgebnisAktuell</c> bleibt dabei <c>false</c>.</summary>
        [Fact]
        public void Ohne_Ergebnis_kein_Band()
        {
            if (!_db.Vorhanden) return;

            Projektstempel(STAMM, ERGEBNIS.AddMinutes(5));
            Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Pruefe(STAMM, null));

            var ctrl = new WirtschaftlichkeitCtrl();
            Assert.Equal(Ergebnisveraltung.Keine, ctrl.Veraltung(null));
            Assert.False(ctrl.ErgebnisAktuell(null));
        }

        /// <summary>
        /// Die Regel hinter <c>ErgebnisAktuell</c>: Ein Ergebnis auf dem jüngsten Lauf ist mit
        /// jüngerem Stempel veraltet durch Kosten; mit fremdem Lauf geht die Simulation vor.
        /// </summary>
        [Fact]
        public void Veraltung_nennt_den_Simulationslauf_vor_den_Kosten()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            int lauf = LaufId(TEST1);
            Assert.True(lauf > 0);
            var e = new WirtschaftlichkeitErgebnis { IdProjekt = TEST1, IdErgebnis = lauf, Zeitstempel = ERGEBNIS };

            Assert.Equal(Ergebnisveraltung.Keine, ctrl.Veraltung(e));
            Assert.True(ctrl.ErgebnisAktuell(e));

            Projektstempel(STAMM, ERGEBNIS.AddSeconds(30));
            Assert.Equal(Ergebnisveraltung.Kosten, ctrl.Veraltung(e));
            Assert.False(ctrl.ErgebnisAktuell(e));

            e.IdErgebnis = lauf + 1;
            Assert.Equal(Ergebnisveraltung.Simulation, ctrl.Veraltung(e));
        }

        // =============================================================================
        //  Teil 3 - die Rechnung und das Band
        // =============================================================================

        /// <summary>
        /// <b>Die Rechnung stempelt sich nicht selbst.</b> Der Saatstand des Gesetzeskatalogs steht
        /// zurück; „Neu berechnen“ der Gruppe sät ihn nach (StelleTabellenSicher → StelleKatalogSicher
        /// schreibt <c>Tab_Gesetzesparameter</c> und stempelt den Katalog). Danach trägt jede Zeile des
        /// Laufs DENSELBEN Zeitstempel, der Katalogstempel liegt nicht nach ihm, jedes Ergebnis ist
        /// aktuell, und keine der beiden Seiten zeigt das Band.
        /// </summary>
        [Fact]
        public async Task Eine_Rechnung_ist_danach_aktuell_auch_wenn_sie_den_Katalog_saet()
        {
            if (!_db.Vorhanden) return;
            BerichteKostenHuelle huelle = GruppeGeladen();

            Schreibe("UPDATE \"Tab_Gesetzesparameter\" SET \"Wert\" = 1 WHERE \"Schluessel\" = ?",
                     new DbParam("?", DbWerte.GESETZ_KATALOG_GENERATION));
            StempelLeeren();

            List<WirtschaftlichkeitErgebnis> gespeichert = await Rechnen(huelle);

            Assert.True(Zahl("SELECT \"Wert\" FROM \"Tab_Gesetzesparameter\" WHERE \"Schluessel\" = '" +
                             DbWerte.GESETZ_KATALOG_GENERATION + "'") > 1, "Die Rechnung hat den Katalog nicht nachgesät.");
            DateTime? katalog = KostenAenderungsstempel.Lies(STAMM).Katalog;
            Assert.True(katalog.HasValue, "Die Saat in der Rechnung hat den Katalog nicht gestempelt.");

            DateTime zeit = gespeichert[0].Zeitstempel;
            Assert.All(gespeichert, e => Assert.Equal(zeit, e.Zeitstempel));
            Assert.True(katalog.Value <= zeit, katalog.Value.ToString("O") + " > " + zeit.ToString("O"));

            var ctrl = new WirtschaftlichkeitCtrl();
            Assert.All(gespeichert, e => Assert.True(ctrl.ErgebnisAktuell(e), e.IdProjekt + "/" + e.Szenario));

            KostenStand kosten = KostenLaden(huelle);
            Assert.False(kosten.Nachrechnen);
            Assert.Equal(Ergebnisveraltung.Keine, kosten.NachrechnenGrund);

            WirtschaftlichkeitStand wirt = WirtschaftLaden();
            Assert.Equal(Ergebnisveraltung.Keine, wirt.NachrechnenGrund);
            Assert.DoesNotContain(ErgebnisMatrix.WARN_PRAEFIX.Trim(), wirt.Statuszeile);
        }

        /// <summary>
        /// Eine Kostenänderung an der Variante NACH der Rechnung stellt das Band mit dem Grund
        /// „Kosten“; eine Änderung am Kostenkatalog mit dem Grund „Katalog“ — auf der Kostenseite und
        /// in der Statuszeile der Wirtschaftlichkeit, beide über die Trigger der Datenbank.
        /// </summary>
        [Fact]
        public async Task Eine_Aenderung_nach_der_Rechnung_stellt_das_Band_mit_ihrem_Grund()
        {
            if (!_db.Vorhanden) return;
            BerichteKostenHuelle huelle = GruppeGeladen();
            await Rechnen(huelle);

            // Strikt jünger als das Ergebnis: in der nächsten Sekunde ändern.
            Thread.Sleep(1100);
            Schreibe("UPDATE \"Tab_ProjektWerte\" SET \"EingegebenerWert\" = \"EingegebenerWert\" WHERE \"ID\" = " +
                     "(SELECT MIN(\"ID\") FROM \"Tab_ProjektWerte\" WHERE \"ProjektID\" = ?)", new DbParam("?", TEST1));

            KostenStand kosten = KostenLaden(huelle);
            Assert.True(kosten.Nachrechnen);
            Assert.Equal(Ergebnisveraltung.Kosten, kosten.NachrechnenGrund);
            WirtschaftlichkeitStand wirt = WirtschaftLaden();
            Assert.Equal(Ergebnisveraltung.Kosten, wirt.NachrechnenGrund);
            Assert.StartsWith(R.WIRT_STATUS_VERALTET_KOSTEN, wirt.Statuszeile, StringComparison.Ordinal);

            // Nur der Katalog: Die Projektstempel fallen, ein Energieträger ändert sich.
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = NULL");
            Schreibe("UPDATE \"energy_carrier\" SET \"price_work\" = \"price_work\" WHERE \"id\" = " +
                     "(SELECT MIN(\"id\") FROM \"energy_carrier\")");

            kosten = KostenLaden(huelle);
            Assert.True(kosten.Nachrechnen);
            Assert.Equal(Ergebnisveraltung.Katalog, kosten.NachrechnenGrund);
            wirt = WirtschaftLaden();
            Assert.Equal(Ergebnisveraltung.Katalog, wirt.NachrechnenGrund);
            Assert.StartsWith(R.WIRT_STATUS_VERALTET_KATALOG, wirt.Statuszeile, StringComparison.Ordinal);
        }

        // =============================================================================
        //  Teil 4 - was NICHT stempelt: Selbstheilung der Zuordnung, Simulationsfelder
        // =============================================================================

        /// <summary>Eine Position des Stamms 1019 auf der Wärmepumpe (Anlage 14922, Anker 1020022).</summary>
        private const long POSITION_WP = 101600051L;
        private const int ANLAGE_WP = 14922;

        /// <summary>
        /// <b>Die Selbstheilung der Anlagenzuordnung stempelt nicht.</b> Eine Position zeigt auf eine
        /// Anlage, die es nicht mehr gibt; <c>ZuordnungReparieren</c> schlüsselt sie über ihren Anker
        /// auf die Anlage desselben Geräts um (Gegenprobe: die Zeile ist geschrieben) — der
        /// Projektstempel bleibt, wie er stand: ein gesetzter Stempel mit seinem Text, ein leerer leer.
        /// </summary>
        [Fact]
        public void Die_Selbstheilung_der_Zuordnung_laesst_den_Stempel_stehen()
        {
            if (!_db.Vorhanden) return;

            Verwaisen();
            Projektstempel(STAMM, ERGEBNIS);
            KostenProjektPositionenCtrl.ZuordnungReparieren(STAMM);
            Assert.Equal(ANLAGE_WP, (int)Zahl("SELECT \"ID_Anlage\" FROM \"Tab_ProjektWerte\" WHERE \"ID\" = " + POSITION_WP));
            Assert.Equal(ERGEBNIS.ToString(KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture), StempelText(STAMM));

            Verwaisen();
            StempelLeeren();
            KostenProjektPositionenCtrl.ZuordnungReparieren(STAMM);
            Assert.Equal(ANLAGE_WP, (int)Zahl("SELECT \"ID_Anlage\" FROM \"Tab_ProjektWerte\" WHERE \"ID\" = " + POSITION_WP));
            Assert.Null(StempelText(STAMM));
        }

        /// <summary>
        /// Gegenprobe zur Selbstheilung: Dieselbe Umschlüsselung von Hand (ohne die Klammer des Kerns)
        /// stempelt — der Trigger an <c>Tab_ProjektWerte</c> ist also scharf.
        /// </summary>
        [Fact]
        public void Dieselbe_Umschluesselung_von_Hand_stempelt()
        {
            if (!_db.Vorhanden) return;
            Verwaisen();
            StempelLeeren();
            Schreibe("UPDATE \"Tab_ProjektWerte\" SET \"ID_Anlage\" = ? WHERE \"ID\" = ?",
                     new DbParam("?", ANLAGE_WP), new DbParam("?", POSITION_WP));
            Assert.NotNull(StempelText(STAMM));
        }

        /// <summary>
        /// <b>Nach der Rechnung öffnet der Anwender die Kostenseite</b>, und eine verwaiste Zuordnung
        /// ist zu heilen (etwa aus einem Bestand vor den Stempeln): Die Seite heilt beim Laden und
        /// zeigt trotzdem kein Band — die Heilung ist keine Kostenänderung.
        /// </summary>
        [Fact]
        public async Task Die_Kostenseite_heilt_beim_Laden_ohne_Band()
        {
            if (!_db.Vorhanden) return;
            BerichteKostenHuelle huelle = GruppeGeladen();
            Verwaisen();
            await Rechnen(huelle);

            // Wieder verwaist, der Stempel davon zurück auf leer - wie in einem Bestand, dessen
            // Waise älter ist als die Stempel. Dann eine Sekunde später die Seite laden.
            Verwaisen();
            StempelLeeren();
            Thread.Sleep(1100);

            // Das erste Laden urteilt vor der Heilung; das zweite sähe einen Stempel der Heilung.
            KostenLaden(huelle);
            Assert.Equal(ANLAGE_WP, (int)Zahl("SELECT \"ID_Anlage\" FROM \"Tab_ProjektWerte\" WHERE \"ID\" = " + POSITION_WP));
            KostenStand kosten = KostenLaden(huelle);
            Assert.False(kosten.Nachrechnen);
            Assert.Equal(Ergebnisveraltung.Keine, kosten.NachrechnenGrund);
        }

        /// <summary>
        /// <b>Die Simulation macht die Gruppe nicht veraltet:</b> Sie schreibt an der Anlage die
        /// Quell- und Sondenfelder (<c>WQ_*</c>); diese stehen nicht in der Spaltenliste des Triggers.
        /// </summary>
        [Fact]
        public void Die_Quellfelder_der_Simulation_stempeln_nicht()
        {
            if (!_db.Vorhanden) return;
            StempelLeeren();
            Schreibe("UPDATE \"Tab_Energieanlagen\" SET \"WQ_Tiefe\" = COALESCE(\"WQ_Tiefe\", 0) + 1 WHERE \"ID_Projekt\" = ?",
                     new DbParam("?", TEST1));
            Assert.Null(StempelText(TEST1));
            foreach (int id in new[] { STAMM, TEST1, TEST2 })
                Assert.Equal(Ergebnisveraltung.Keine, KostenAenderungsstempel.Pruefe(id, ERGEBNIS));
        }

        /// <summary>Die Position des Stamms zeigt auf eine Anlage, die es nicht gibt.</summary>
        private static void Verwaisen()
        {
            Schreibe("UPDATE \"Tab_ProjektWerte\" SET \"ID_Anlage\" = 999999 WHERE \"ID\" = ?",
                     new DbParam("?", POSITION_WP));
        }

        /// <summary>Der Projektstempel als gespeicherter Text; <c>null</c> = leer.</summary>
        private static string StempelText(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT \"Kosten_Geaendert\" FROM \"Tab_Projekt\" WHERE \"ID\" = ?", new DbParam("@p", projekt));
            if (o == null || o == DBNull.Value) return null;
            return o is DateTime d
                ? d.ToString(KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture)
                : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static KostenAenderungsstempel.Stempel Stempel(DateTime? gruppe, DateTime? katalog)
            => new KostenAenderungsstempel.Stempel { Gruppe = gruppe, Katalog = katalog };

        private static void Projektstempel(int projekt, DateTime zeit)
        {
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = ? WHERE \"ID\" = ?",
                     new DbParam("?", zeit.ToString(KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture)),
                     new DbParam("?", projekt));
        }

        private static void Katalogstempel(DateTime zeit)
        {
            Schreibe("UPDATE \"Tab_Applikation\" SET \"Kostenkatalog_Geaendert\" = ?",
                     new DbParam("?", zeit.ToString(KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture)));
        }

        private static void StempelLeeren()
        {
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = NULL");
            Schreibe("UPDATE \"Tab_Applikation\" SET \"Kostenkatalog_Geaendert\" = NULL");
        }

        private static void Schreibe(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql, parameter);
            v.Commit();
        }

        private static double Zahl(string sql)
            => Convert.ToDouble(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Der jüngste Simulationslauf des Projekts — dieselbe Abfrage wie der Kern.</summary>
        private static int LaufId(int projekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + ErgebnisCtrl.TAB_KOPF + " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                new DbParam("@p", projekt));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static Func<string, IReadOnlyDictionary<string, object>> Seiten(BerichteKostenHuelle huelle)
            => (Func<string, IReadOnlyDictionary<string, object>>)huelle.Gaben()["SeitenGaben"];

        /// <summary>Die Hülle mit der Gruppe 1019 — wie beim Öffnen des Reiters über die Übersicht.</summary>
        private static BerichteKostenHuelle GruppeGeladen()
        {
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(STAMM, "");
            ((Func<UebersichtStand>)Seiten(huelle)(BerichteKostenSeite.SEITE_UEBERSICHT)["Laden"])();
            return huelle;
        }

        /// <summary>„Neu berechnen“ der Kostenseite für die Gruppe — derselbe Lauf wie auf der Wirtschaftlichkeitsseite.</summary>
        private static async Task<List<WirtschaftlichkeitErgebnis>> Rechnen(BerichteKostenHuelle huelle)
        {
            var berechnen = (Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>)
                Seiten(huelle)(BerichteKostenSeite.SEITE_KOSTEN)["Berechnen"];
            LaufErgebnis erg = await berechnen(new List<int> { TEST1, TEST2 }, _ => { });
            Assert.True(erg.Erfolg, erg.Fehler);

            List<WirtschaftlichkeitErgebnis> gespeichert =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { STAMM, TEST1, TEST2 });
            Assert.Equal(new[] { STAMM, TEST1, TEST2 }, gespeichert.Select(e => e.IdProjekt).Distinct().OrderBy(i => i).ToArray());
            return gespeichert;
        }

        private static KostenStand KostenLaden(BerichteKostenHuelle huelle)
            => ((Func<KostenStand>)Seiten(huelle)(BerichteKostenSeite.SEITE_KOSTEN)["Laden"])();

        private static WirtschaftlichkeitStand WirtschaftLaden()
            => ((Func<WirtschaftlichkeitStand>)new WirtschaftlichkeitSeiteGaben(STAMM, "Wöhler").Gaben()["Laden"])();
    }
}
