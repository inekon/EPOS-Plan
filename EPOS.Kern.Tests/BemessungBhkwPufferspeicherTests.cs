using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID 15.09.2026, wortgleich: „‚je kW Leistung' beim BHKW ist
    /// ‚je kW elektr. Leistung', beim Pufferspeicher soll das Volumen die Bezugsgröße
    /// sein (€/Ltr.)".
    ///
    /// <para><b>Was der Entscheid schließt.</b> Die Bemessungsart „je kW Leistung"
    /// bemisst sich je Gewerk an dessen EINER Baugröße
    /// (<see cref="TechnikPlanwertCtrl"/>, Regel H4c). Zwei Gewerke konnte diese Regel
    /// allein nicht entscheiden: Das BHKW führt <c>Pel</c> UND <c>Ptherm</c>, der
    /// Pufferspeicher gar keine Leistung — beide blieben deshalb ohne Bezugsgröße, und
    /// ein gepflegter Satz ergab über den Anwenderentscheid I-2 den erfassten Betrag,
    /// bei einer satzbasierten Zeile also 0. Der Entscheid legt die Größen fest: beim
    /// BHKW die ELEKTRISCHE Leistung (<c>Tab_BHKW.Pel</c> [kW el]), beim
    /// Pufferspeicher das VOLUMEN (<c>Tab_Pufferspeicher.Gesamtvolumen</c> [l]).</para>
    ///
    /// <para><b>Die Beschriftung folgt der Größe.</b> Ein Satzfeld mit „€/kW" hinter
    /// einem Literwert wäre falsch: Am BHKW heißt die Art „je kW elektr. Leistung", am
    /// Pufferspeicher „je Liter" mit „€/Ltr." — beides aus den Ressourcen, in beiden
    /// Sprachen. Der Persistenzwert bleibt <c>EUR_PRO_KW_LEISTUNG</c>
    /// (Drei-Schichten-Regel); nur die Anzeige hängt am Gewerk.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie bei den Baugrößen der H4c
    /// (<see cref="BetriebskostenBaugroesseTests"/>): Die Testdatenbank führt weder eine
    /// BHKW- noch eine Pufferspeicherzeile mit dieser Bemessung. Die Fälle legen
    /// deshalb in der ARBEITSKOPIE ein eigenes Projekt mit runden Zahlen an — ein
    /// BHKW mit 50 kW el / 100 kW th und ein Pufferspeicher mit 1.000 l.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BemessungBhkwPufferspeicherTests : IDisposable
    {
        // Schlüssel oberhalb des Bestands der Testdatenbank und neben den Blöcken der
        // H4c-Fälle (190…) und der Solarthermie (191…) — die Fälle stören keine
        // Bestandszeile.
        private const int PROJEKT = 193001;
        private const int STAMM_ID = 193900;

        private const int A_BHKW = 193101;
        private const int A_BHKW_LEER = 193102;    // Modul ohne elektrische Leistung
        private const int A_PUFFER = 193103;
        private const int A_PUFFER_LEER = 193104;  // Speicher ohne Volumen

        private const int G_BHKW = 193201;
        private const int G_BHKW_LEER = 193202;
        private const int G_PUFFER = 193203;
        private const int G_PUFFER_LEER = 193204;

        private const int P_BHKW_KW = 193000001;
        private const int P_BHKW_EL = 193000002;
        private const int P_BHKW_HEIZ = 193000003;
        private const int P_BHKW_OHNE = 193000004;
        private const int P_PUFFER_LTR = 193000005;
        private const int P_PUFFER_KWH = 193000006;
        private const int P_PUFFER_OHNE = 193000007;

        /// <summary><c>Tab_KostenKomponente.ID</c> der beiden Gewerke des Entscheids.</summary>
        private const int K_BHKW = 7;
        private const int K_PUFFER = 6;

        // ---- Die Baugrößen des Beispiels ----------------------------------------
        private const double BHKW_PEL = 50.0;      // kW el
        private const double BHKW_PTHERM = 100.0;  // kW th
        private const int PUFFER_LITER = 1000;     // l

        // ---- Die Sätze des Beispiels --------------------------------------------
        private const double SATZ_KW = 900.0;      // €/kW    ⇒ 45.000,00 €
        private const double SATZ_KW_TH = 300.0;   // €/kW    ⇒ 30.000,00 €
        private const double SATZ_LTR = 1.5;       // €/Ltr.  ⇒  1.500,00 €
        private const double SATZ_KWH = 20.0;      // €/kWh   ⇒ ohne Bezugsgröße

        // Die Kultur ist auf de-DE gepinnt: Die Fälle halten deutsche Ressourcentexte
        // gegen Equal, und der Windows-Läufer steht auf en-US. Die gemeinsame
        // Vorrichtung pinnt ALLE VIER Kulturwerte — CurrentUICulture allein genügt
        // nicht, sobald ein Aufruf auf einem anderen Faden landet.
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose()
        {
            _kultur.Dispose();
        }

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void BeispielAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'Bemessung 15.09.')");
            Sql("INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (" + STAMM_ID + ", 'Aggregat', 1)");

            Sql("INSERT INTO Tab_BHKW (ID, ID_Projekt, Bezeichner, Pel, Ptherm) VALUES (" +
                G_BHKW + ", " + PROJEKT + ", 'BHKW 50/100', " + Z(BHKW_PEL) + ", " + Z(BHKW_PTHERM) + ")");
            Sql("INSERT INTO Tab_BHKW (ID, ID_Projekt, Bezeichner, Pel, Ptherm) VALUES (" +
                G_BHKW_LEER + ", " + PROJEKT + ", 'BHKW ohne Pel', 0, " + Z(BHKW_PTHERM) + ")");
            Sql("INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, Bezeichner, Gesamtvolumen) VALUES (" +
                G_PUFFER + ", " + PROJEKT + ", 'Puffer 1000', " + PUFFER_LITER + ")");
            Sql("INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, Bezeichner, Gesamtvolumen) VALUES (" +
                G_PUFFER_LEER + ", " + PROJEKT + ", 'Puffer ohne Volumen', 0)");

            Anlage(A_BHKW, "BHKW", WizardItemClass.BHKW_TYP, "ID_BHKW", G_BHKW);
            Anlage(A_BHKW_LEER, "BHKW ohne Pel", WizardItemClass.BHKW_TYP, "ID_BHKW", G_BHKW_LEER);
            Anlage(A_PUFFER, "Puffer", WizardItemClass.PUFFER_TYP, "ID_PUFFER", G_PUFFER);
            Anlage(A_PUFFER_LEER, "Puffer ohne Volumen", WizardItemClass.PUFFER_TYP, "ID_PUFFER", G_PUFFER_LEER);

            Position(P_BHKW_KW, K_BHKW, A_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_KW);
            Position(P_BHKW_EL, K_BHKW, A_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, SATZ_KW);
            Position(P_BHKW_HEIZ, K_BHKW, A_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, SATZ_KW_TH);
            Position(P_BHKW_OHNE, K_BHKW, A_BHKW_LEER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_KW);
            Position(P_PUFFER_LTR, K_PUFFER, A_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_LTR);
            Position(P_PUFFER_KWH, K_PUFFER, A_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, SATZ_KWH);
            Position(P_PUFFER_OHNE, K_PUFFER, A_PUFFER_LEER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_LTR);
        }

        /// <summary>Die sieben Geräteverweise müssen ALLE gesetzt werden — ihr
        /// Spaltenvorgabewert 0 läse sich als Verweis auf ein Gerät, das es nicht gibt
        /// (derselbe Befund wie in <see cref="BetriebskostenBaugroesseTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Anlage(int id, string name, int typ, string verweisSpalte, int geraet)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = id + ", " + PROJEKT + ", '" + name + "', " + typ;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (string.Equals(s, verweisSpalte, StringComparison.Ordinal)
                                 ? geraet.ToString() : "NULL");
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        /// <summary>Eine satzbasierte INVESTITIONSzeile — EingegebenerWert 0, wie der
        /// Dialog sie speichert.</summary>
        private static void Position(int id, int komponente, int anlage, string bemessung, double satz)
        {
            Sql("INSERT INTO Tab_ProjektWerte " +
                "(ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                " Nutzungsdauer, Kostenart, Bemessung, Einheitpreis, ID_Anlage) VALUES (" +
                id + ", " + PROJEKT + ", " + STAMM_ID + ", " + komponente + ", " +
                DbWerte.KOSTEN_KATEGORIE_INVESTITION + ", 0.0, 20, '" +
                DbWerte.KOSTENART_KAPITALGEBUNDEN + "', '" + bemessung + "', " +
                Z(satz) + ", " + anlage + ")");
        }

        private static string Z(double wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }

        /// <summary>Der wirksame Investitionsbetrag EINER Zeile aus dem Rechenweg des
        /// Kerns — derselbe Weg, den Dialog und Kapitalwertrechnung gehen.</summary>
        private static InvestKaskade.Zeile Kaskadenzeile(int positionsId)
        {
            Dictionary<int, InvestKaskade.Zeile> karte =
                InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            return karte[positionsId];
        }

        // =====================================================================
        // BHKW — die elektrische Leistung
        // =====================================================================

        /// <summary>
        /// DER ENTSCHEID am BHKW: „je kW Leistung" nimmt <c>Pel</c>, nicht
        /// <c>Ptherm</c> und nicht die Summe. 50 kW el, nicht 100 kW th und nicht 150.
        /// </summary>
        [Fact]
        public void Bhkw_je_kW_Leistung_nimmt_die_elektrische_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(BHKW_PEL, TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, A_BHKW).Value, 6);
        }

        /// <summary>
        /// Der Betrag der Kostenverwaltung: 900 €/kW × 50 kW el = 45.000,00 €. Er kommt
        /// aus der Investitionskaskade — demselben Rechenweg, den auch Kapitalwert und
        /// Kostenseite lesen.
        /// </summary>
        [Fact]
        public void Bhkw_Satz_je_kW_ergibt_den_Betrag_aus_der_elektrischen_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            InvestKaskade.Zeile z = Kaskadenzeile(P_BHKW_KW);
            Assert.Equal(BHKW_PEL, z.Basis.Value, 6);
            Assert.Equal(45000.0, z.Betrag, 6);
        }

        /// <summary>
        /// Zwei Namen, EINE Größe: „je kW Leistung" und „je kW elektrisch" treffen am
        /// BHKW denselben Wert — dieselbe H4c-Regel wie bei Wärmepumpe und Kessel.
        /// </summary>
        [Fact]
        public void Bhkw_je_kW_Leistung_und_je_kW_elektrisch_treffen_dieselbe_Groesse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(Kaskadenzeile(P_BHKW_EL).Basis.Value,
                         Kaskadenzeile(P_BHKW_KW).Basis.Value, 6);
            Assert.Equal(Kaskadenzeile(P_BHKW_EL).Betrag, Kaskadenzeile(P_BHKW_KW).Betrag, 6);
        }

        /// <summary>
        /// Die THERMISCHE Seite bleibt, wo sie war: „je kW Heizleistung" rechnet
        /// weiter mit <c>Ptherm</c>. Der Entscheid tritt neben sie, nicht an ihre Stelle.
        /// </summary>
        [Fact]
        public void Bhkw_je_kW_Heizleistung_bleibt_bei_der_thermischen_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            InvestKaskade.Zeile z = Kaskadenzeile(P_BHKW_HEIZ);
            Assert.Equal(BHKW_PTHERM, z.Basis.Value, 6);
            Assert.Equal(30000.0, z.Betrag, 6);
        }

        /// <summary>
        /// KEIN STILLES 0: Führt das Modul keine elektrische Leistung, bleibt es beim
        /// erfassten Betrag (Anwenderentscheid I-2) — die Zeile nennt aber den GRUND,
        /// und zwar den der PFLEGE („kein Gerät mit dieser Baugröße"), nicht mehr den
        /// des Gewerks.
        /// </summary>
        [Fact]
        public void Bhkw_ohne_Pel_nennt_den_Pflegegrund_statt_still_0()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, K_BHKW,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_BHKW_LEER);

            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_BHKW_OHNE);
            Assert.Null(z.Basis);
            Assert.Equal(0.0, z.Raster.BetragNetto.Value, 6);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET, z.BasisGrund);
        }

        // =====================================================================
        // Pufferspeicher — das Volumen
        // =====================================================================

        /// <summary>
        /// DER ENTSCHEID am Pufferspeicher: Die Bezugsgröße ist das VOLUMEN in Litern
        /// (<c>Tab_Pufferspeicher.Gesamtvolumen</c>) — 1.000 l.
        /// </summary>
        [Fact]
        public void Pufferspeicher_bemisst_sich_am_Volumen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(PUFFER_LITER, TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, A_PUFFER).Value, 6);
        }

        /// <summary>
        /// Der Betrag der Kostenverwaltung: 1,50 €/Ltr. × 1.000 l = 1.500,00 €.
        /// </summary>
        [Fact]
        public void Pufferspeicher_Satz_je_Liter_ergibt_den_Betrag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            InvestKaskade.Zeile z = Kaskadenzeile(P_PUFFER_LTR);
            Assert.Equal(PUFFER_LITER, z.Basis.Value, 6);
            Assert.Equal(1500.0, z.Betrag, 6);
        }

        /// <summary>
        /// Die kWh-KAPAZITÄT bleibt dem Pufferspeicher fremd: Ohne Temperaturpaar gibt
        /// es keine belastbare Umrechnung des Volumens — daran ändert der Entscheid
        /// nichts, er benennt gerade deshalb das Volumen selbst als Bezugsgröße.
        /// </summary>
        [Fact]
        public void Pufferspeicher_kennt_weiterhin_keine_kWh_Kapazitaet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Null(TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, A_PUFFER));

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, K_PUFFER,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_PUFFER);
            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_PUFFER_KWH);
            Assert.Null(z.Basis);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK, z.BasisGrund);
        }

        /// <summary>
        /// KEIN STILLES 0: Ein Speicher ohne gepflegtes Volumen bleibt beim erfassten
        /// Betrag — und die Zeile nennt den Pflegegrund.
        /// </summary>
        [Fact]
        public void Pufferspeicher_ohne_Volumen_nennt_den_Pflegegrund_statt_still_0()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, K_PUFFER,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_PUFFER_LEER);

            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_PUFFER_OHNE);
            Assert.Null(z.Basis);
            Assert.Equal(0.0, z.Raster.BetragNetto.Value, 6);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET, z.BasisGrund);
        }

        // =====================================================================
        // Die Landkarte Art ↔ Gewerk
        // =====================================================================

        /// <summary>Beide Gewerke KENNEN „je kW Leistung" jetzt — ein fehlender Betrag
        /// ist dort ein Pflegebefund, nicht mehr „die Art passt nicht zum Gewerk".</summary>
        [Theory]
        [InlineData(K_BHKW)]
        [InlineData(K_PUFFER)]
        public void Beide_Gewerke_kennen_je_kW_Leistung(int komponente)
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(
                komponente, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET,
                         WirtschaftlichkeitCtrl.BasisGrund(
                             DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, komponente));
        }

        /// <summary>Die ÜBRIGEN Erzeugerarten bleiben unverändert — der Entscheid
        /// greift nur an den Komponenten 6 und 7. Wärmepumpe, Kessel und Stromspeicher
        /// führen „je kW Leistung" wie zuvor.</summary>
        [Theory]
        [InlineData(1)]   // Wärmepumpe   — Nennleistung
        [InlineData(2)]   // Heizkessel   — Ptherm
        [InlineData(4)]   // Solarthermie — Kollektorfeldleistung
        [InlineData(5)]   // Stromspeicher— Leistung
        public void Die_uebrigen_Gewerke_behalten_je_kW_Leistung(int komponente)
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(
                komponente, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET,
                         WirtschaftlichkeitCtrl.BasisGrund(
                             DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, komponente));
        }

        /// <summary>Und die Arten, die zu keinem der beiden Gewerke gehören, bleiben
        /// fremd: Das BHKW erzeugt keine kWp, der Pufferspeicher keine kWh.</summary>
        [Theory]
        [InlineData(K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KWP)]
        [InlineData(K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET)]
        [InlineData(K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET)]
        [InlineData(K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        [InlineData(K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR)]
        public void Unpassende_Kombinationen_bleiben_ohne_Bezugsgroesse(int komponente, string bemessung)
        {
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(komponente, bemessung));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrund(bemessung, komponente));
        }

        // =====================================================================
        // Die Beschriftung folgt der Größe
        // =====================================================================

        /// <summary>Am BHKW heißt „je kW Leistung" ausdrücklich „je kW elektr.
        /// Leistung" — sonst stünde über einer Pel-Bemessung ein mehrdeutiger Name.</summary>
        [Fact]
        public void Am_Bhkw_heisst_die_Art_je_kW_elektr_Leistung()
        {
            Assert.Equal("je kW elektr. Leistung",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_BHKW));
            Assert.Equal("€/kW",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_BHKW));
        }

        /// <summary>Am Pufferspeicher heißt sie „je Liter", und der Satz trägt
        /// „€/Ltr." — die Einheit gehört zur Bezugsgröße.</summary>
        [Fact]
        public void Am_Pufferspeicher_heisst_die_Art_je_Liter()
        {
            Assert.Equal("je Liter",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFER));
            Assert.Equal("€/Ltr.",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFER));
            Assert.Equal("€/Ltr.", BetriebskostenCtrl.SatzEinheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFER));
        }

        /// <summary>Beide Texte stehen in BEIDEN Sprachen in den Ressourcen — kein
        /// Textliteral in der Maske.</summary>
        [Fact]
        public void Die_Beschriftungen_stehen_in_beiden_Sprachen()
        {
            using var en = new Kulturvorrichtung("en-US");

            Assert.Equal("per kW electric capacity",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_BHKW));
            Assert.Equal("per litre",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFER));
        }

        /// <summary>Die übrigen Gewerke behalten den allgemeinen Namen — und wer ohne
        /// Gewerk fragt, bekommt ihn ebenfalls.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(5)]
        public void Die_uebrigen_Gewerke_behalten_den_allgemeinen_Namen(int komponente)
        {
            Assert.Equal("je kW Leistung",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, komponente));
            Assert.Equal("€/kW",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, komponente));
        }

        /// <summary>Die Sonderbeschriftung gilt NUR für diese eine Art: „je kW
        /// Heizleistung" heißt am BHKW weiter so, und „je kWh Kapazität" am
        /// Pufferspeicher auch.</summary>
        [Fact]
        public void Nur_diese_eine_Art_traegt_eine_Sonderbeschriftung()
        {
            Assert.Equal("je kW Heizleistung",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, K_BHKW));
            Assert.Equal("je kWh Kapazität",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, K_PUFFER));
        }
    }
}
