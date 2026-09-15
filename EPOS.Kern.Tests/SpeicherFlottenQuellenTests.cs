using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERRÜCKMELDUNG 12.09.2026 (#239, Bildschirmfoto „Stromspeicher-Auslegung ›
    /// 1 Speicher"): „Stromspeicher hinzufügen geht nicht für Speicher aus der Datenbank —
    /// nur für duplizierung des vorhandenen. Bei mehreren angelegten Stromspeichern wird nur
    /// einer angezeigt, nach löschen steht er nicht mehr zur Auswahl."
    ///
    /// <para><b>Der Befund.</b> „+ Speicher hinzufügen" legte ausschließlich eine generische
    /// Einheit an (100 kWh, 50/50 kW, 95 %, SoC 10/90/50 %). Es gab keinen Weg zum
    /// Speicherkatalog und keinen zu den Speicheranlagen des Projekts — und weil
    /// <see cref="SpeicherFlottenStudieCtrl.Vorbelegung"/> nur beim ANLEGEN einer Flotte
    /// greift, war die Einheitenliste nach dem ersten Speichern eingefroren: Eine später
    /// angelegte Anlage kam nie hinein, eine entfernte nie zurück.</para>
    ///
    /// <para><b>Was hier geprüft wird</b> — die drei Wege des Kerns, die den Befund beheben:
    /// <see cref="SpeicherFlottenStudieCtrl.Projektanlagenkandidaten"/> (welche Anlagen es
    /// gibt), <see cref="SpeicherFlottenStudieCtrl.EinheitAusProjektanlage"/> (eine davon als
    /// Einheit — <b>bitgleich zur Vorbelegung</b>, denn es ist derselbe Weg) und
    /// <see cref="SpeicherFlottenStudieCtrl.EinheitAusKatalog"/> (ein Satz des
    /// Speicherkatalogs mit der Abbildung aus Konzept 1.8).</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie in
    /// <see cref="SpeicherFlottenAnlagenEinheitenTests"/>: Die Fälle legen ihren Zustand in
    /// der ARBEITSKOPIE selbst an, mit Schlüsseln weit oberhalb des Bestands (Höchststände
    /// am 12.09.2026: Projekt 1046, Anlage 14938, Gerät 1017063, Katalogsatz 17) und
    /// oberhalb der Schlüssel von <see cref="SpeicherFlottenAnlagenEinheitenTests"/>
    /// (210xxx).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeicherFlottenQuellenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 239001;

        private const int A_SP1 = 239101;
        private const int A_SP2 = 239102;

        private const int G_SP1 = 239201;
        private const int G_SP2 = 239202;

        private const int K_VOLL = 239301;
        private const int K_NACKT = 239302;
        private const int K_OHNE_LEISTUNG = 239303;

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void ProjektAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" +
                PROJEKT + ", '#239 Speicherquellen')");
        }

        private static void SpeicherGeraet(int id, string bezeichner, double energie, double leistung)
        {
            Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie, Wirkungsgrad_RT) " +
                "VALUES (" + id + ", " + PROJEKT + ", '" + bezeichner + "', " +
                Z(leistung) + ", " + Z(energie) + ", " + Z(0.9) + ")");
        }

        /// <summary>Die sieben Geräte-Verweisspalten der Anlagenzeile — ihr Vorgabewert 0
        /// wäre ein Fremdschlüssel auf ein Gerät, das es nicht gibt (Begründung wortgleich
        /// in <see cref="SpeicherFlottenAnlagenEinheitenTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Speicheranlage(int idAnlage, string bezeichner, int geraet)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = idAnlage + ", " + PROJEKT + ", '" + bezeichner + "', " + WizardItemClass.SP_TYP;

            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (s == "ID_SP" ? geraet.ToString(CultureInfo.InvariantCulture) : "NULL");
            }

            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        /// <summary>Ein Satz des Speicherkatalogs mit allen fünfzehn Fachspalten.</summary>
        private static void Katalogsatz(int id, string bezeichner, double energie, double leistung,
            double wirkungsgradRt, double ladezustand, double modulkosten, double leistungskosten,
            double investitionFix, double standbyW)
        {
            Sql("INSERT INTO Tab_Stromspeicher_STAMM " +
                "(ID, Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten, " +
                " ReadOnly, Wirkungsgrad_RT, Zyklen_Zugesichert, Verschleisskosten, Leistungskosten, " +
                " Investition_Fix, Standby_Verbrauch, Firma) VALUES (" +
                id + ", '" + bezeichner + "', 'Lithium-Ionen', " + Z(leistung) + ", " + Z(energie) + ", " +
                Z(1.5) + ", " + Z(ladezustand) + ", " + Z(modulkosten) + ", 0, " + Z(wirkungsgradRt) +
                ", 6000, " + Z(0.03) + ", " + Z(leistungskosten) + ", " + Z(investitionFix) + ", " +
                Z(standbyW) + ", 'Prüfhersteller')");
        }

        private static List<FlottenEinheit> Vorbelegung()
            => SpeicherFlottenStudieCtrl.Vorbelegung(PROJEKT, 0.0).Eingaben.Auslegung.Flotte.Einheiten;

        private static void ZweiAnlagen()
        {
            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher gross", 129.0, 100.0);
            SpeicherGeraet(G_SP2, "Speicher klein", 30.0, 15.0);
            Speicheranlage(A_SP1, "Speicher Halle", G_SP1);
            Speicheranlage(A_SP2, "Speicher Verwaltung", G_SP2);
            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));
        }

        // =====================================================================
        // 1 — Die Kandidaten
        // =====================================================================

        /// <summary>
        /// <b>Zwei Speicheranlagen, zwei Kandidaten</b> — in Anlagenreihenfolge, mit dem
        /// Anlagenbezeichner, der Kapazität und der Leistung DER Anlage. Der Schlüssel ist
        /// die <c>Tab_Energieanlagen.ID</c> als Text, denn genau so trägt ihn
        /// <see cref="FlottenEinheit.AnlageId"/>; daran erkennt die Oberfläche, ob eine
        /// Anlage schon in der Flotte steht.
        /// </summary>
        [Fact]
        public void Zwei_Anlagen_ergeben_zwei_Kandidaten_in_Anlagenreihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();

            IReadOnlyList<SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat> k =
                SpeicherFlottenStudieCtrl.Projektanlagenkandidaten(PROJEKT);

            Assert.Equal(2, k.Count);
            Assert.Equal(new[] { A_SP1.ToString(CultureInfo.InvariantCulture),
                                 A_SP2.ToString(CultureInfo.InvariantCulture) },
                         k.Select(x => x.AnlageId).ToArray());
            Assert.Equal(new[] { "Speicher Halle", "Speicher Verwaltung" },
                         k.Select(x => x.Name).ToArray());
            Assert.Equal(129.0, k[0].KapazitaetKWh, 6);
            Assert.Equal(100.0, k[0].LeistungKw, 6);
            Assert.Equal(30.0, k[1].KapazitaetKWh, 6);
            Assert.Equal(15.0, k[1].LeistungKw, 6);
        }

        /// <summary>Ohne Projekt keine Kandidaten — und keine Ausnahme.</summary>
        [Fact]
        public void Ohne_Projekt_gibt_es_keine_Kandidaten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Empty(SpeicherFlottenStudieCtrl.Projektanlagenkandidaten(0));
        }

        // =====================================================================
        // 2 — Die Einheit aus der Projektanlage
        // =====================================================================

        /// <summary>
        /// <b>Derselbe Weg wie die Vorbelegung.</b> Die nachgezogene Einheit trägt Feld für
        /// Feld dieselben Werte wie die, die beim ANLEGEN der Flotte entstanden wäre —
        /// Kapazität, Lade- und Entladeleistung, beide Wirkungsgrade, SoC-Band, Start-SoC,
        /// Name und Anlagenbezug. Nur die <c>Id</c> ist eine andere: Sie ist je Einheit
        /// eindeutig.
        /// </summary>
        [Fact]
        public void Die_Einheit_aus_der_Anlage_traegt_die_Werte_der_Vorbelegung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();
            List<FlottenEinheit> vorbelegt = Vorbelegung();
            Assert.Equal(2, vorbelegt.Count);

            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, A_SP2);

            Assert.NotNull(e);
            FlottenEinheit soll = vorbelegt[1];
            Assert.Equal(soll.Name, e.Name);
            Assert.Equal(soll.AnlageId, e.AnlageId);
            Assert.Equal(soll.KapazitaetKWh, e.KapazitaetKWh, 10);
            Assert.Equal(soll.LadeleistungKw, e.LadeleistungKw, 10);
            Assert.Equal(soll.EntladeleistungKw, e.EntladeleistungKw, 10);
            Assert.Equal(soll.Ladewirkungsgrad, e.Ladewirkungsgrad, 10);
            Assert.Equal(soll.Entladewirkungsgrad, e.Entladewirkungsgrad, 10);
            Assert.Equal(soll.SocMin, e.SocMin, 10);
            Assert.Equal(soll.SocMax, e.SocMax, 10);
            Assert.Equal(soll.SocStart, e.SocStart, 10);
            Assert.NotEqual(soll.Id, e.Id);
        }

        /// <summary>
        /// <b>Der Rundreisewirkungsgrad wird gleichmäßig aufgeteilt</b> —
        /// <c>eta_ch = eta_dis = sqrt(eta_RT)</c>, die Regel aus
        /// <c>SpeicherParameter.EtaCh</c>/<c>EtaDis</c>. Bei 0,9 sind das 0,948683…
        /// </summary>
        [Fact]
        public void Der_Rundreisewirkungsgrad_wird_gleichmaessig_aufgeteilt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();

            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, A_SP1);

            Assert.NotNull(e);
            Assert.Equal(Math.Sqrt(0.9), e.Ladewirkungsgrad, 10);
            Assert.Equal(Math.Sqrt(0.9), e.Entladewirkungsgrad, 10);
            Assert.Equal(0.9, e.Ladewirkungsgrad * e.Entladewirkungsgrad, 10);
        }

        /// <summary>Eine unbekannte Anlage liefert <c>null</c> — kein Rückfall auf irgendetwas.</summary>
        [Fact]
        public void Eine_unbekannte_Anlage_liefert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();

            Assert.Null(SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, 239999));
            Assert.Null(SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, 0));
            Assert.Null(SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(0, A_SP1));
        }

        // =====================================================================
        // 3 — Die Einheit aus dem Katalog
        // =====================================================================

        /// <summary>
        /// <b>Die Abbildung Katalog → Einheit</b> (Konzept „Stromspeicher-Dialoge" 1.8) an
        /// festen Werten: Energie → Kapazität, Leistung → beide Richtungen,
        /// <c>Wirkungsgrad_RT</c> → zweimal <c>sqrt</c>, SoC-Band 10/90 % aus der LEEREN
        /// Variantenzeile, Ladezustand → Start-SoC, die drei Kostenspalten und der
        /// Standby-Verbrauch von W nach kW.
        /// </summary>
        [Fact]
        public void Ein_Katalogsatz_wird_Feld_fuer_Feld_abgebildet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogsatz(K_VOLL, "Prüfhersteller: PS-50", 50.0, 25.0,
                        wirkungsgradRt: 0.92, ladezustand: 40.0, modulkosten: 480.0,
                        leistungskosten: 120.0, investitionFix: 2500.0, standbyW: 35.0);

            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusKatalog(K_VOLL);

            Assert.NotNull(e);
            Assert.Equal("Prüfhersteller: PS-50", e.Name);
            Assert.Null(e.AnlageId);
            Assert.False(string.IsNullOrWhiteSpace(e.Id));

            Assert.Equal(50.0, e.KapazitaetKWh, 10);
            Assert.Equal(25.0, e.LadeleistungKw, 10);
            Assert.Equal(25.0, e.EntladeleistungKw, 10);
            Assert.Equal(Math.Sqrt(0.92), e.Ladewirkungsgrad, 10);
            Assert.Equal(Math.Sqrt(0.92), e.Entladewirkungsgrad, 10);

            Assert.Equal(0.10, e.SocMin, 10);
            Assert.Equal(0.90, e.SocMax, 10);
            Assert.Equal(0.40, e.SocStart, 10);

            Assert.Equal(0.035, e.HilfsverbrauchKw, 10);

            Assert.True(e.EigeneKosten);
            Assert.Equal(480.0, e.InvestitionEuroProKWh, 10);
            Assert.Equal(120.0, e.InvestitionEuroProKw, 10);
            Assert.Equal(2500.0, e.InvestitionEuro, 10);
        }

        /// <summary>
        /// <b>Ohne Kosten kein Kostenschalter.</b> Stünde <c>EigeneKosten</c> auf
        /// <c>true</c>, überschrieben lauter Nullen die gemeinsamen Kostensätze aus
        /// Schritt 2 — das wäre teurer als gar keine Angabe.
        /// </summary>
        [Fact]
        public void Ein_Katalogsatz_ohne_Kosten_traegt_keine_eigenen_Kosten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogsatz(K_NACKT, "Prüfhersteller: PS-nackt", 20.0, 10.0,
                        wirkungsgradRt: 0.0, ladezustand: 0.0, modulkosten: 0.0,
                        leistungskosten: 0.0, investitionFix: 0.0, standbyW: 0.0);

            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusKatalog(K_NACKT);

            Assert.NotNull(e);
            Assert.False(e.EigeneKosten);
            Assert.Equal(0.0, e.InvestitionEuroProKWh, 10);
            Assert.Equal(0.0, e.HilfsverbrauchKw, 10);

            // EIN FEHLENDER WIRKUNGSGRAD BEKOMMT DIE NEUTRALE VORGABE (Anwenderentscheid
            // vom 15.09.2026): 95 % je Richtung, hinterlegt an EINER Stelle. Ein zweiter
            // Satz Vorgabewerte daneben waeren zwei Wahrheiten.
            Assert.Equal(FlottenGeraetevorgaben.LADEWIRKUNGSGRAD, e.Ladewirkungsgrad, 10);
            Assert.Equal(FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD, e.Entladewirkungsgrad, 10);

            // Ladezustand 0 heisst „nicht gepflegt" und wird auf SoC_min geklemmt.
            Assert.Equal(0.10, e.SocStart, 10);
        }

        /// <summary>
        /// <b>Fehlt die Leistungsangabe, gilt 1 C</b> — wörtlich die Regel des Laufs
        /// (<c>StromspeicherSimCtrl.LeseParameter</c>: „fehlt sie in den Altdaten, gilt 1 C").
        /// </summary>
        [Fact]
        public void Ein_Katalogsatz_ohne_Leistung_bekommt_ein_C()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogsatz(K_OHNE_LEISTUNG, "Prüfhersteller: PS-ohne-P", 64.0, 0.0,
                        wirkungsgradRt: 0.9, ladezustand: 0.0, modulkosten: 0.0,
                        leistungskosten: 0.0, investitionFix: 0.0, standbyW: 0.0);

            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusKatalog(K_OHNE_LEISTUNG);

            Assert.NotNull(e);
            Assert.Equal(64.0, e.LadeleistungKw, 10);
            Assert.Equal(64.0, e.EntladeleistungKw, 10);
        }

        /// <summary>Ein unbekannter Katalogsatz liefert <c>null</c>.</summary>
        [Fact]
        public void Ein_unbekannter_Katalogsatz_liefert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(SpeicherFlottenStudieCtrl.EinheitAusKatalog(239999));
            Assert.Null(SpeicherFlottenStudieCtrl.EinheitAusKatalog(0));
            Assert.Null(StromspeicherStammCtrl.Katalogsatz(239999));
        }

        // =====================================================================
        // 4 — Die Kante: der gespeicherte Stand bleibt der gespeicherte Stand
        // =====================================================================

        /// <summary>
        /// <b>Der Nachzug ist eine ANWENDERHANDLUNG</b> (SP‑O‑8). Die neuen Wege LESEN nur;
        /// ein gespeicherter Stand <c>@Aktuell</c> bleibt unverändert, auch wenn das Projekt
        /// zwei Anlagen führt und der Stand nur eine Einheit kennt. Der Referenzlauf ist
        /// damit unberührt — 1046 rechnet seinen Stand <c>@Projektflotte</c>.
        /// </summary>
        [Fact]
        public void Ein_gespeicherter_Stand_bleibt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();
            SpeicherOptimierungEingaben stand = SpeicherFlottenStudieCtrl
                .Vorbelegung(PROJEKT, 0.0).Eingaben.Kopie();
            stand.Auslegung.Flotte.Einheiten.RemoveAt(1);
            SpeicherAuslegungCtrl.Speichern(PROJEKT, SpeicherAuslegungCtrl.Anlage(PROJEKT),
                SpeicherAuslegungCtrl.AktuellerStand, stand);

            // Die Kandidaten kennen beide Anlagen — der Stand kennt eine Einheit.
            Assert.Equal(2, SpeicherFlottenStudieCtrl.Projektanlagenkandidaten(PROJEKT).Count);
            Assert.NotNull(SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(PROJEKT, A_SP2));
            Assert.Single(Vorbelegung());
        }

        // =====================================================================
        // 5 — Die GERAETEKANDIDATEN der Größensuche (Anwenderentscheid 15.09.2026)
        // =====================================================================

        /// <summary>
        /// <b>Die QUELLE entscheidet über die Kandidatenmenge.</b> Der Projektkatalog
        /// führt die zwei Speicheranlagen dieses Projekts, die Stammdaten den ganzen
        /// Katalog — zwei verschiedene Mengen aus demselben Aufruf, allein über die Quelle
        /// unterschieden.
        /// </summary>
        [Fact]
        public void Projektkatalog_und_Stammdaten_liefern_verschiedene_Kandidatenmengen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();
            Katalogsatz(K_VOLL, "Prüfhersteller: PS-50", 50.0, 25.0,
                        wirkungsgradRt: 0.92, ladezustand: 40.0, modulkosten: 480.0,
                        leistungskosten: 120.0, investitionFix: 2500.0, standbyW: 35.0);

            var projekt = SpeicherFlottenStudieCtrl.Geraetekandidaten(
                PROJEKT, FlottenKandidatenquelle.Projektkatalog);
            var stamm = SpeicherFlottenStudieCtrl.Geraetekandidaten(
                PROJEKT, FlottenKandidatenquelle.Stammdaten);

            // Der Projektkatalog: genau die zwei Anlagen, mit ihren Anlagen-Kennungen.
            Assert.Equal(2, projekt.Count);
            Assert.Equal(new[] { A_SP1.ToString(CultureInfo.InvariantCulture),
                                 A_SP2.ToString(CultureInfo.InvariantCulture) },
                         projekt.Select(x => x.Quellkennung).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.Contains(projekt, x => Math.Abs(x.KapazitaetKWh - 129.0) < 1e-9);

            // Die Stammdaten: der Katalog, darunter der eben angelegte Satz — und NICHT
            // die Projektanlagen.
            Assert.Contains(stamm, x => x.Quellkennung == K_VOLL.ToString(CultureInfo.InvariantCulture));
            Assert.DoesNotContain(stamm, x => x.Quellkennung == A_SP1.ToString(CultureInfo.InvariantCulture));
            Assert.NotEqual(projekt.Count, stamm.Count);
        }

        /// <summary>
        /// <b>Ein Katalogsatz rechnet mit NEUTRALEN Vorgaben und ist gekennzeichnet.</b>
        /// Der Katalog führt keine Betriebsführung — das SoC-Fenster kommt immer aus
        /// <see cref="FlottenGeraetevorgaben"/>; fehlt zusätzlich der
        /// Rundlaufwirkungsgrad, gelten auch die neutralen Wirkungsgrade.
        /// </summary>
        [Fact]
        public void Ein_Katalogsatz_ohne_Wirkungsgrad_bekommt_die_neutralen_Vorgaben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogsatz(K_NACKT, "Prüfhersteller: PS-nackt", 20.0, 10.0,
                        wirkungsgradRt: 0.0, ladezustand: 0.0, modulkosten: 0.0,
                        leistungskosten: 0.0, investitionFix: 0.0, standbyW: 0.0);

            FlottenGeraetekandidat kandidat = SpeicherFlottenStudieCtrl
                .Geraetekandidaten(PROJEKT, FlottenKandidatenquelle.Stammdaten)
                .Single(x => x.Quellkennung == K_NACKT.ToString(CultureInfo.InvariantCulture));

            Assert.True(kandidat.NeutraleKennwerte);
            Assert.Equal(FlottenGeraetevorgaben.LADEWIRKUNGSGRAD, kandidat.Geraet.Ladewirkungsgrad, 10);
            Assert.Equal(FlottenGeraetevorgaben.ENTLADEWIRKUNGSGRAD, kandidat.Geraet.Entladewirkungsgrad, 10);
            Assert.Equal(FlottenGeraetevorgaben.SOC_MIN, kandidat.Geraet.SocMin, 10);
            Assert.Equal(FlottenGeraetevorgaben.SOC_MAX, kandidat.Geraet.SocMax, 10);
            Assert.Empty(kandidat.Geraet.RainflowKurve);                     // keine Alterung
            Assert.Equal(0.0, kandidat.Geraet.GrenzverschleissEuroProKWhEntladung, 10);
        }

        /// <summary>
        /// <b>Der Gerätebestand landet in JEDER Suchachse</b> — dort liest ihn die
        /// Zählregel der Engine, und dieselben Geräte rechnet der Lauf.
        /// </summary>
        [Fact]
        public void Der_Bestand_wird_in_jede_Suchachse_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweiAnlagen();

            var auslegung = new FlottenAuslegungEingang
            {
                Suchmethode = FlottenSuchmethode.Groesse,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    // Eine ALTE Kopplung: Sie wird im selben Schritt benannt umgesetzt.
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndCRate,
                        KapazitaetVonKWh = 10, KapazitaetBisKWh = 200,
                        CRateVon = 0.5, CRateBis = 1.0,
                        Quelle = FlottenKandidatenquelle.Projektkatalog
                    }
                }
            };

            SpeicherFlottenStudieCtrl.GeraetebestandAuffrischen(PROJEKT, auslegung);

            Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, auslegung.Achsen[0].Modus);
            Assert.Equal(2, auslegung.Achsen[0].Geraete.Count);
            Assert.Equal(5.0, auslegung.Achsen[0].LeistungVonKw, 9);        // 10 kWh * 0,5 C
            Assert.Equal(200.0, auslegung.Achsen[0].LeistungBisKw, 9);      // 200 kWh * 1,0 C
        }

        /// <summary>
        /// <b>Die Übernahme eines Stammdatengeräts legt im PROJEKT an und lässt die
        /// Stammdaten unberührt.</b> Sie nimmt den Weg, den
        /// <see cref="SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen"/> für die
        /// gewählten Einheiten schon baut — ein zweiter Weg daneben wäre eine zweite
        /// Wahrheit darüber, was „in das Projekt übernehmen" heißt.
        /// </summary>
        [Fact]
        public void Ein_Stammdatengeraet_laesst_sich_ins_Projekt_uebernehmen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Katalogsatz(K_VOLL, "Prüfhersteller: PS-50", 50.0, 25.0,
                        wirkungsgradRt: 0.92, ladezustand: 40.0, modulkosten: 480.0,
                        leistungskosten: 120.0, investitionFix: 2500.0, standbyW: 35.0);

            FlottenGeraetekandidat gefunden = SpeicherFlottenStudieCtrl
                .Geraetekandidaten(PROJEKT, FlottenKandidatenquelle.Stammdaten)
                .Single(x => x.Quellkennung == K_VOLL.ToString(CultureInfo.InvariantCulture));

            int katalogVorher = StromspeicherStammCtrl.KatalogZeilen().Count;

            FlottenUebernahmeErgebnis ergebnis = SpeicherFlottenStudieCtrl
                .EinheitenInProjektUebernehmen(PROJEKT, new[] { gefunden.Geraet }, new[] { 1 });

            Assert.True(ergebnis.Erfolg, ergebnis.Meldung);
            Assert.Equal(1, ergebnis.Angelegt);
            Assert.Equal(0, ergebnis.Geaendert);

            // Im PROJEKT steht die neue Anlage — mit den Werten des Katalogsatzes.
            var projekt = SpeicherFlottenStudieCtrl.Geraetekandidaten(
                PROJEKT, FlottenKandidatenquelle.Projektkatalog);
            Assert.Single(projekt);
            Assert.Equal(50.0, projekt[0].KapazitaetKWh, 9);
            Assert.Equal(25.0, projekt[0].LeistungKw, 9);

            // Die STAMMDATEN sind unberührt: kein Satz dazu, keiner weg.
            Assert.Equal(katalogVorher, StromspeicherStammCtrl.KatalogZeilen().Count);
            Assert.NotNull(StromspeicherStammCtrl.Katalogsatz(K_VOLL));
        }

        // =====================================================================
        // Werkzeug
        // =====================================================================

        private static string Z(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
