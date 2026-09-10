using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERBEFUND 10.09.2026 (H4c): Die Bemessungen der BETRIEBSKOSTEN rechnen mit
    /// der Baugröße — „analog zu Investitionskosten".
    ///
    /// <para><b>Der Befund.</b> Kostenverwaltung, Komponente Stromspeicher, Reiter
    /// „Kosten Invest/Betrieb": Die Zeile „Wartung / Sichtprüfung Speicher" mit der
    /// Bemessung „je kWh elektrisch" und einem Satz von 1 €/kWh wies 0 €/a aus. Der
    /// Speicher war damit das einzige Gewerk ohne jede Leistungsgröße in der
    /// Gerätewelt-Landkarte (<c>TechnikPlanwertCtrl</c>) und ohne Strommenge im
    /// Lauf-Auflöser (<c>EndenergieAufloeser</c>) — beide Wege lieferten null, und über
    /// den Anwenderentscheid I-2 galt der erfasste Betrag; bei einer satzbasierten
    /// Zeile ist das die 0.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Die Testdatenbank führt keine
    /// Speicheranlage mit Kostenpositionen und keinen Speicher-Simulationslauf. Die
    /// Fälle legen deshalb in der ARBEITSKOPIE ein eigenes Projekt mit je einer Anlage
    /// je Gewerk an — mit runden Zahlen, damit jedes Ergebnis von Hand nachrechenbar
    /// bleibt. Der Gerätestand des Befundes ist übernommen: Growatt 100 kW / 129 kWh.</para>
    ///
    /// <para><b>Warum die Verweisspalten ausdrücklich NULL sind</b> (#166, 10.09.2026).
    /// <see cref="Anlage"/> setzt ALLE SIEBEN Verweisspalten von
    /// <c>Tab_Energieanlagen</c> — <c>ID_WP, ID_Solar, ID_PV, ID_SP, ID_Kessel,
    /// ID_BHKW, ID_PUFFER</c> —, den einen benutzten mit der Geräte-ID, die sechs
    /// übrigen mit <c>NULL</c>. Das ist keine Schreibfreude, sondern die Bedingung
    /// dafür, dass überhaupt eine Anlagenzeile landet: Die sieben Spalten stehen im
    /// Schema auf <c>DEFAULT 0</c> (<c>sql/schema/001_grundschema.sql</c>), und wer
    /// eine weglässt, schreibt damit eine 0 statt nichts. Die 0 fällt zweimal.
    /// <b>Erstens am Fremdschlüssel:</b> Jede der sieben Spalten trägt einen FOREIGN
    /// KEY auf ihre Gerätetabelle, und die Verbindung des Kerns schaltet
    /// <c>PRAGMA foreign_keys = ON</c> (<c>SqliteDatenzugriff.cs</c>) — ein
    /// Gerät mit der ID 0 gibt es nirgends, also fällt schon die ERSTE Zeile mit
    /// „FOREIGN KEY constraint failed". <b>Zweitens am UNIQUE-Index:</b>
    /// <c>idx_Anlage_ID_WP/ID_Kessel/ID_PUFFER/ID_BHKW</c> stehen je auf
    /// <c>(ID_Projekt, &lt;Verweis&gt;)</c> (<c>sql/schema/003_indizes_fk.sql</c>, in
    /// Bestandsdatenbanken nachgezogen von
    /// <c>EPOS.Kern/Allgemein/Update/AnlagenEindeutigkeit.cs</c>) — ab der ZWEITEN
    /// Anlage desselben Projekts kollidiert 0 mit 0. NULL kollidiert dagegen weder
    /// mit einem Fremdschlüssel noch in SQLite mit NULL. Der Bestand der
    /// Testdatenbank trägt für nicht belegte Verweise deshalb ausnahmslos NULL (die 0
    /// kommt in <c>Tab_Energieanlagen</c> 0-mal vor), und der Produktweg setzt alle
    /// sieben Spalten ausdrücklich: <c>AnlagenSql.AnlagenParameter</c> übergibt
    /// <c>DBNull.Value</c> für jeden Verweis, den der Anlagentyp nicht meint. Wer
    /// diese Datei anfasst, hält es genauso.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenBaugroesseTests
    {
        // Schlüssel weit oberhalb des Bestands der Testdatenbank (Höchststände am
        // 10.09.2026: Projekt 1045, Anlage 14931, Gerät 1017061, Position 101600605,
        // Kostenfaktor 150, Ergebnis 212) — die Fälle stören keine Bestandszeile.
        private const int PROJEKT = 190001;
        private const int STAMM_ID = 190900;
        private const int ERGEBNIS = 190500;

        private const int A_SPEICHER = 190101;
        private const int A_PV = 190102;
        private const int A_WP = 190103;
        private const int A_SOLAR = 190104;
        private const int A_KESSEL = 190105;

        private const int G_SPEICHER = 190201;
        private const int G_PV = 190202;
        private const int G_WP = 190203;
        private const int G_SOLAR = 190204;
        private const int G_KESSEL = 190205;

        private const int P_KAPAZITAET = 190000001;
        private const int P_LEISTUNG = 190000002;
        private const int P_KWH_EL = 190000003;
        private const int P_KWP = 190000004;
        private const int P_HEIZLEISTUNG = 190000005;
        private const int P_M2 = 190000006;
        private const int P_KW_KESSEL = 190000007;

        // ---- Die Baugrößen des Beispiels ----------------------------------------
        private const double SP_LEISTUNG = 100.0;    // kW   (Growatt 100 kW)
        private const double SP_ENERGIE = 129.0;     // kWh  (… / 129 kWh)
        private const double SP_ENTLADUNG = 50000.0; // kWh/a des Laufs
        private const double PV_MODUL_W = 400.0;     // W je Modul
        private const int PV_ANZAHL = 25;            // Module ⇒ 10 kWp
        private const double WP_NENNLEISTUNG = 12.0; // kW Heizleistung
        private const double SOLAR_APERTUR = 2.5;    // m² je Modul
        private const int SOLAR_ANZAHL = 8;          // Module ⇒ 20 m²
        private const double KESSEL_PTHERM = 40.0;   // kW

        // ---- Die Sätze des Beispiels --------------------------------------------
        private const double SATZ_KAPAZITAET = 2.0;    // €/kWh  ⇒ 258,00 €/a
        private const double SATZ_LEISTUNG = 3.0;      // €/kW   ⇒ 300,00 €/a
        private const double SATZ_KWH_EL = 0.01;       // €/kWh  ⇒ 500,00 €/a
        private const double SATZ_KWP = 15.0;          // €/kWp  ⇒ 150,00 €/a
        private const double SATZ_HEIZLEISTUNG = 25.0; // €/kW   ⇒ 300,00 €/a
        private const double SATZ_M2 = 4.0;            // €/m²   ⇒  80,00 €/a
        private const double SATZ_KW_KESSEL = 5.0;     // €/kW   ⇒ 200,00 €/a

        // =====================================================================
        // Aufbau
        // =====================================================================

        /// <summary>Legt Projekt, Geräte, Anlagen und die sieben Betriebszeilen an.</summary>
        private static void BeispielAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'H4c Baugroessen')");
            Sql("INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (" + STAMM_ID + ", 'Wartung H4c', 0)");

            // --- Geräte ---
            Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie) VALUES (" +
                G_SPEICHER + ", " + PROJEKT + ", 'Growatt 100/129', " + Z(SP_LEISTUNG) + ", " + Z(SP_ENERGIE) + ")");
            Sql("INSERT INTO Tab_PV (ID, ID_Projekt, Bezeichner, Leistung) VALUES (" +
                G_PV + ", " + PROJEKT + ", 'Modul 400', " + Z(PV_MODUL_W) + ")");
            Sql("INSERT INTO Tab_WP (ID, ID_Projekt, Bezeichner, Nennleistung) VALUES (" +
                G_WP + ", " + PROJEKT + ", 'WP 12', " + Z(WP_NENNLEISTUNG) + ")");
            Sql("INSERT INTO Tab_Solarkollektoren (ID, ID_Projekt, Bezeichner, Aperturflaeche) VALUES (" +
                G_SOLAR + ", " + PROJEKT + ", 'Kollektor 2,5', " + Z(SOLAR_APERTUR) + ")");
            Sql("INSERT INTO Tab_Heizkessel (ID, ID_Projekt, Bezeichner, Ptherm) VALUES (" +
                G_KESSEL + ", " + PROJEKT + ", 'Kessel 40', " + Z(KESSEL_PTHERM) + ")");

            // --- Anlagen (ID_Type wie im Assistenten, sonst zählt die PV nicht mit) ---
            Anlage(A_SPEICHER, "Speicher", WizardItemClass.SP_TYP, "ID_SP", G_SPEICHER, null, 0);
            Anlage(A_PV, "PV", WizardItemClass.PV_TYP, "ID_PV", G_PV, "PV_Leistung", PV_ANZAHL);
            Anlage(A_WP, "WP", WizardItemClass.WP_TYP, "ID_WP", G_WP, null, 0);
            Anlage(A_SOLAR, "Solar", WizardItemClass.SOLAR_TYP, "ID_Solar", G_SOLAR,
                   "Kollektormodulanzahl", SOLAR_ANZAHL);
            Anlage(A_KESSEL, "Kessel", WizardItemClass.KESSEL_TYP, "ID_Kessel", G_KESSEL, null, 0);

            // --- Die Betriebskostenzeilen (Kategorie 2), alle satzbasiert ---
            Position(P_KAPAZITAET, 5, A_SPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, SATZ_KAPAZITAET);
            Position(P_LEISTUNG, 5, A_SPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_LEISTUNG);
            Position(P_KWH_EL, 5, A_SPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, SATZ_KWH_EL);
            Position(P_KWP, 3, A_PV, DbWerte.BEMESSUNG_EUR_PRO_KWP, SATZ_KWP);
            Position(P_HEIZLEISTUNG, 1, A_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, SATZ_HEIZLEISTUNG);
            Position(P_M2, 4, A_SOLAR, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, SATZ_M2);
            Position(P_KW_KESSEL, 2, A_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_KW_KESSEL);
        }

        /// <summary>Der Speicherlauf — eine Zeile in Tab_ErgebnisStromspeicher.</summary>
        private static void LaufAnlegen(double entladungKwh)
        {
            Sql("INSERT INTO Tab_Ergebnis (ID, ID_Projekt, Bezeichner, Sim_Stromspeicher) VALUES (" +
                ERGEBNIS + ", " + PROJEKT + ", 'H4c Lauf', 1)");
            Sql("INSERT INTO Tab_ErgebnisStromspeicher " +
                "(ID, ID_Ergebnis, ID_Energieanlage, Bezeichner, Entladung_Gesamt, Ladung_Gesamt) VALUES (" +
                ERGEBNIS + ", " + ERGEBNIS + ", " + A_SPEICHER + ", 'Speicher', " +
                Z(entladungKwh) + ", " + Z(entladungKwh * 1.1) + ")");
        }

        /// <summary>Die sieben Verweisspalten von <c>Tab_Energieanlagen</c>, in der
        /// Reihenfolge von <c>AnlagenSql.SQL_ANLAGE_INSERT</c>.</summary>
        private static readonly string[] VERWEISSPALTEN =
            { "ID_WP", "ID_Solar", "ID_PV", "ID_SP", "ID_Kessel", "ID_BHKW", "ID_PUFFER" };

        /// <summary>
        /// Eine Anlagenzeile — mit ALLEN SIEBEN Verweisspalten ausdrücklich: die eine
        /// benutzte trägt die Geräte-ID, die sechs übrigen <c>NULL</c>.
        ///
        /// <para><b>Warum ausdrücklich NULL.</b> Siehe den Absatz „Warum die
        /// Verweisspalten ausdrücklich NULL sind" im Klassenkopf: weggelassen heißt
        /// <c>DEFAULT 0</c>, und die 0 fällt zweimal — am Fremdschlüssel und am
        /// UNIQUE-Index.</para>
        /// </summary>
        private static void Anlage(int id, string name, int typ, string verweisSpalte, int geraet,
                                   string mengenSpalte, int menge)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = id + ", " + PROJEKT + ", '" + name + "', " + typ;
            foreach (string spalte in VERWEISSPALTEN)
            {
                spalten += ", [" + spalte + "]";
                werte += ", " + (spalte == verweisSpalte ? geraet.ToString() : "NULL");
            }
            if (mengenSpalte != null)
            {
                spalten += ", [" + mengenSpalte + "]";
                werte += ", " + menge;
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        /// <summary>Eine satzbasierte Betriebskostenzeile — EingegebenerWert 0, wie der
        /// Dialog sie speichert (KostenProjektPositionenCtrl.Speichern).</summary>
        private static void Position(int id, int komponente, int anlage, string bemessung, double satz)
        {
            Sql("INSERT INTO Tab_ProjektWerte " +
                "(ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                " Kostenart, Bemessung, Einheitpreis, ID_Anlage) VALUES (" +
                id + ", " + PROJEKT + ", " + STAMM_ID + ", " + komponente + ", " +
                DbWerte.KOSTEN_KATEGORIE_BETRIEB + ", 0.0, '" +
                DbWerte.KOSTENART_BETRIEBSGEBUNDEN + "', '" + bemessung + "', " + Z(satz) + ", " + anlage + ")");
        }

        /// <summary>
        /// Eine Zahl als SQL-LITERAL — mit Punkt, unabhängig von der laufenden Kultur.
        ///
        /// <para><b>Warum das nötig ist</b> (#166, 10.09.2026). SQL kennt nur den Punkt.
        /// <c>double.ToString()</c> nimmt dagegen die Kultur des Threads, und die ist in
        /// diesem Testprojekt nicht verlässlich Invariant: <c>CecWechselrichterAuslieferungTests</c>
        /// setzt im Konstruktor <c>CultureInfo.DefaultThreadCurrentCulture</c> auf
        /// <c>de-DE</c> und stellt sie nicht zurück — sie gilt danach PROZESSWEIT für
        /// jeden Thread, den xunit neu aufmacht. Lief diese Klasse allein, war die
        /// Kultur Invariant und alles ging gut; im vollen Lauf wurde aus den 2,5 m²
        /// Aperturfläche das Literal <c>2,5</c> — ein Komma, das SQL als
        /// WERTETRENNER liest, also fünf Werte für vier Spalten. Deshalb hier
        /// ausdrücklich <see cref="CultureInfo.InvariantCulture"/> statt einer Annahme
        /// über die Kultur des Läufers.</para>
        /// </summary>
        private static string Z(double wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Setzt ab — und sagt es, wenn es nicht klappt.
        /// <c>DataRepository.ExecuteSQL</c> schluckt jeden Datenbankfehler und gibt nur
        /// <c>false</c> zurück; ohne diese Prüfung fiele ein misslungener INSERT erst
        /// drei Schritte später als „Nullable object must have a value" auf, ohne den
        /// SQL-Text zu nennen.</summary>
        private static void Sql(string sql)
        {
            Assert.True(DataRepository.ExecuteSQL(sql), "SQL fehlgeschlagen: " + sql);
        }

        /// <summary>Der Jahresbetrag EINER Zeile aus dem Rechenweg des Kerns.</summary>
        private static double Betrag(int positionsId)
        {
            Dictionary<int, KostenPositionNachweis> karte =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            return karte[positionsId].BetragJahr;
        }

        private static double? Menge(int positionsId)
        {
            Dictionary<int, KostenPositionNachweis> karte =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            return karte[positionsId].Menge;
        }

        // =====================================================================
        // Der Stromspeicher — die Lücke des Befundes
        // =====================================================================

        /// <summary>
        /// „je kWh Kapazität" × 2 €/kWh an einem 129-kWh-Speicher = 258,00 €/a. Diese
        /// Kombination stand schon vor H4c in der Landkarte; sie steht hier als Anker
        /// dafür, dass der Umbau sie nicht verschoben hat.
        /// </summary>
        [Fact]
        public void Speicher_je_kWh_Kapazitaet_rechnet_mit_der_Kapazitaet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(SP_ENERGIE, Menge(P_KAPAZITAET).Value, 6);
            Assert.Equal(258.0, Betrag(P_KAPAZITAET), 6);
        }

        /// <summary>
        /// H4c: „je kW Leistung" × 3 €/kW an einem 100-kW-Speicher = 300,00 €/a. VOR
        /// dem Befund gab es für den Speicher keine Leistungsgröße — der Betrag war 0.
        /// </summary>
        [Fact]
        public void Speicher_je_kW_Leistung_rechnet_mit_der_Speicherleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(SP_LEISTUNG, Menge(P_LEISTUNG).Value, 6);
            Assert.Equal(300.0, Betrag(P_LEISTUNG), 6);
        }

        /// <summary>
        /// H4c: „je kW elektrisch" trifft am Speicher DIESELBE Leistung wie „je kW
        /// Leistung" — die Leistung eines Stromspeichers IST elektrisch. Zwei Namen,
        /// eine Zahl.
        /// </summary>
        [Fact]
        public void Speicher_je_kW_elektrisch_trifft_dieselbe_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(SP_LEISTUNG,
                TechnikPlanwertCtrl.BaugroesseSumme(PROJEKT, 5,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_SPEICHER).Value, 6);
        }

        /// <summary>
        /// H4c: „je kWh elektrisch" am Speicher = ENTLADENE Jahresenergie des jüngsten
        /// Laufs. 50.000 kWh × 0,01 €/kWh = 500,00 €/a — der Fall des Anwenderbefundes.
        /// </summary>
        [Fact]
        public void Speicher_je_kWh_elektrisch_rechnet_mit_der_entladenen_Jahresenergie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            LaufAnlegen(SP_ENTLADUNG);

            Assert.Equal(SP_ENTLADUNG, Menge(P_KWH_EL).Value, 6);
            Assert.Equal(500.0, Betrag(P_KWH_EL), 6);
        }

        /// <summary>
        /// OHNE Lauf bleibt es beim Anwenderentscheid I-2: keine Menge, also gilt der
        /// erfasste Betrag — und der Dialog nennt jetzt den GRUND, statt still 0 zu
        /// zeigen.
        /// </summary>
        [Fact]
        public void Speicher_je_kWh_elektrisch_ohne_Lauf_faellt_auf_den_erfassten_Wert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();   // kein LaufAnlegen

            Assert.Null(Menge(P_KWH_EL));
            Assert.Equal(0.0, Betrag(P_KWH_EL), 6);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                         WirtschaftlichkeitCtrl.BasisGrund(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 5));
        }

        // =====================================================================
        // Die übrigen Gewerke — „Prüfe bei anderen Kostenpositionen ebenfalls"
        // =====================================================================

        /// <summary>PV „je kWp": 25 Module × 400 W = 10 kWp × 15 €/kWp = 150,00 €/a.</summary>
        [Fact]
        public void Photovoltaik_je_kWp_rechnet_mit_der_installierten_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(10.0, Menge(P_KWP).Value, 6);
            Assert.Equal(150.0, Betrag(P_KWP), 6);
        }

        /// <summary>H4c: „je kW elektrisch" an der PV trifft DIESELBEN kWp — kWp IST
        /// die elektrische Leistung; zwei Namen dürfen nicht zwei Zahlen ergeben.</summary>
        [Fact]
        public void Photovoltaik_je_kW_elektrisch_trifft_dieselben_kWp()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(10.0,
                TechnikPlanwertCtrl.BaugroesseSumme(PROJEKT, 3,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_PV).Value, 6);
        }

        /// <summary>Wärmepumpe „je kW Heizleistung": 12 kW × 25 €/kW = 300,00 €/a.</summary>
        [Fact]
        public void Waermepumpe_je_kW_Heizleistung_rechnet_mit_der_Nennleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(WP_NENNLEISTUNG, Menge(P_HEIZLEISTUNG).Value, 6);
            Assert.Equal(300.0, Betrag(P_HEIZLEISTUNG), 6);
        }

        /// <summary>Solarthermie „je m²": 8 Module × 2,5 m² = 20 m² × 4 €/m² = 80,00 €/a.</summary>
        [Fact]
        public void Solarthermie_je_Quadratmeter_rechnet_mit_der_Aperturflaeche()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(20.0, Menge(P_M2).Value, 6);
            Assert.Equal(80.0, Betrag(P_M2), 6);
        }

        /// <summary>Heizkessel „je kW Leistung": 40 kW × 5 €/kW = 200,00 €/a.</summary>
        [Fact]
        public void Heizkessel_je_kW_Leistung_rechnet_mit_Ptherm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(KESSEL_PTHERM, Menge(P_KW_KESSEL).Value, 6);
            Assert.Equal(200.0, Betrag(P_KW_KESSEL), 6);
        }

        /// <summary>H4c: Der Kessel führt nur EINE Leistung — „je kW Heizleistung"
        /// trifft sie deshalb ebenso wie „je kW Leistung".</summary>
        [Fact]
        public void Heizkessel_je_kW_Heizleistung_trifft_dasselbe_Ptherm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(KESSEL_PTHERM,
                TechnikPlanwertCtrl.BaugroesseSumme(PROJEKT, 2,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, A_KESSEL).Value, 6);
        }

        // =====================================================================
        // Die Landkarte selbst — was bewusst NICHT rechnet
        // =====================================================================

        /// <summary>
        /// H4c-Regel: Eine Kombination rechnet, wenn das Gewerk GENAU EINE Größe führt,
        /// die die Art meint. Das BHKW führt <c>Pel</c> UND <c>Ptherm</c> — „je kW
        /// Leistung" bleibt dort deshalb ohne Bezugsgröße, und der Pufferspeicher trägt
        /// überhaupt keine (ohne Temperaturpaar keine kWh, Speicher-Registry).
        /// </summary>
        [Theory]
        [InlineData(7, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]     // BHKW: zwei Leistungen
        [InlineData(6, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET)]  // Pufferspeicher
        [InlineData(1, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]   // Wärmepumpe
        [InlineData(4, DbWerte.BEMESSUNG_EUR_PRO_KWP)]             // Solarthermie
        public void Unpassende_Kombinationen_bleiben_ohne_Bezugsgroesse(int komponente, string bemessung)
        {
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(komponente, bemessung));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrund(bemessung, komponente));
        }

        /// <summary>
        /// H4c: Passt die Art zum Gewerk, ist ein fehlender Betrag ein PFLEGE-Befund —
        /// der Grund unterscheidet das, damit der Anwender weiß, wo er suchen muss.
        /// </summary>
        [Theory]
        [InlineData(5, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(5, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET)]
        [InlineData(3, DbWerte.BEMESSUNG_EUR_PRO_KWP)]
        [InlineData(1, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(2, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(4, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR)]
        [InlineData(7, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        public void Passende_Kombinationen_melden_das_fehlende_Geraet(int komponente, string bemessung)
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(komponente, bemessung));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET,
                         WirtschaftlichkeitCtrl.BasisGrund(bemessung, komponente));
        }

        /// <summary>Ein fester Betrag braucht keine Bezugsgröße — dort gibt es auch
        /// keinen Grund zu nennen.</summary>
        [Fact]
        public void Absolute_Arten_tragen_keinen_Grund()
        {
            Assert.Equal("", WirtschaftlichkeitCtrl.BasisGrund(DbWerte.BEMESSUNG_BETRAG, 5));
            Assert.Equal("", WirtschaftlichkeitCtrl.BasisGrund(DbWerte.BEMESSUNG_JAHRESBETRAG, 5));
            Assert.Equal("", WirtschaftlichkeitCtrl.BasisGrund("", 5));
        }

        // =====================================================================
        // Der Dialog liest denselben Rechenweg
        // =====================================================================

        /// <summary>
        /// H4c: Was der Rechenkern rechnet, zeigt der Dialog — und wo es keine
        /// Bezugsgröße gibt, trägt die Zeile den GRUND (Werkzeugtipp der Hülle).
        /// </summary>
        [Fact]
        public void Der_Dialog_zeigt_Betrag_Basis_und_Grund_derselben_Rechnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, 5, DbWerte.KOSTEN_KATEGORIE_BETRIEB);

            KostenProjektPositionenCtrl.Zeile leistung = zeilen.Find(z => z.Raster.Id == P_LEISTUNG);
            Assert.Equal(300.0, leistung.Raster.BetragNetto.Value, 6);
            Assert.Equal(SP_LEISTUNG, leistung.Basis.Value, 6);
            Assert.Equal("", leistung.BasisGrund);

            // Ohne Lauf hat „je kWh elektrisch" keine Basis — und sagt, warum.
            KostenProjektPositionenCtrl.Zeile kwh = zeilen.Find(z => z.Raster.Id == P_KWH_EL);
            Assert.Null(kwh.Basis);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF, kwh.BasisGrund);
        }

        /// <summary>
        /// H4c: Beim Wechsel der Bemessung zieht der Dialog die Bezugsgröße frisch nach
        /// — sonst rechnete er bis zum Speichern mit der Basis der ALTEN Art weiter
        /// (die 129 kWh Kapazität als Grundlage eines €/kW-Satzes).
        /// </summary>
        [Fact]
        public void Der_Wechsel_der_Bemessung_zieht_die_Bezugsgroesse_nach()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, 5, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_KAPAZITAET);
            Assert.Equal(SP_ENERGIE, z.Basis.Value, 6);

            KostenProjektPositionenCtrl.BasisNachziehen(z, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG);
            Assert.Equal(SP_LEISTUNG, z.Basis.Value, 6);
            Assert.Equal("", z.BasisGrund);

            // Und eine Art, die es an diesem Gewerk nicht gibt, meldet den Grund —
            // ohne die gespeicherte Menge anzufassen.
            KostenProjektPositionenCtrl.BasisNachziehen(z, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH);
            Assert.Null(z.Basis);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK, z.BasisGrund);
        }
    }
}
