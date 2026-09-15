using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERBEFUND 14.09.2026: Kostenverwaltung, Komponente Solarthermie, Reiter
    /// „Kosten Invest/Betrieb", Projekt „Beispiel WP WG 1 - Erdwärme". Die Zeile
    /// „Solarthermie" mit der Bemessung „je kW Leistung" und 700 €/kW wies
    /// <b>Betrag netto 0,00 €</b> aus — und unter dem Raster stand kein Wort dazu.
    ///
    /// <para><b>Der Befund.</b> Die Gerätewelt-Landkarte
    /// (<see cref="TechnikPlanwertCtrl"/>) führte für die Solarthermie nur ihre
    /// FLÄCHE. Eine Leistung gab es dort nicht — und auch sonst nirgends im Kern:
    /// <c>Tab_Solarkollektoren</c> hat keine Leistungsspalte, und die Simulation
    /// rechnet die Leistung Stunde für Stunde aus Einstrahlung und Temperaturen,
    /// kennt also keine Nenngröße. Über den Anwenderentscheid I-2 galt deshalb der
    /// erfasste Betrag; bei einer satzbasierten Zeile ist das die 0.</para>
    ///
    /// <para><b>Die Festlegung.</b> Die thermische Nennleistung eines Kollektorfelds
    /// ist die Konvention der europäischen Solarthermie-Statistik (Solar Keymark):
    /// 0,7 kW je m² Aperturfläche, mal der Modulanzahl der Anlagenzeile. Sie steht
    /// als benannte Konstante mit Quelle im Kern
    /// (<see cref="TechnikPlanwertCtrl.KOLLEKTOR_KW_JE_M2"/>) und wird dem Anwender
    /// im Herleitungstext genannt — eine gerechnete Bezugsgröße, die in keiner
    /// Gerätemaske steht, muss sich erklären.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie bei den Baugrößen der H4c
    /// (<see cref="BetriebskostenBaugroesseTests"/>): Die Testdatenbank führt keine
    /// Solaranlage mit einer kW-Kostenposition. Die Fälle legen deshalb in der
    /// ARBEITSKOPIE ein eigenes Projekt mit runden Zahlen an — 10 Module zu je
    /// 2,5 m² Apertur, also 25 m² und 17,5 kW.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SolarthermieLeistungTests : IDisposable
    {
        // Schlüssel weit oberhalb des Bestands der Testdatenbank und neben dem
        // Block der H4c-Fälle (190…) — die Fälle stören keine Bestandszeile.
        private const int PROJEKT = 191001;
        private const int STAMM_ID = 191900;

        private const int A_SOLAR = 191101;
        private const int A_LEER = 191102;     // Kollektorfeld ohne Fläche
        private const int G_SOLAR = 191201;
        private const int G_LEER = 191202;

        private const int P_KW_LEISTUNG = 191000001;
        private const int P_KW_HEIZ = 191000002;
        private const int P_M2 = 191000003;
        private const int P_OHNE_FELD = 191000004;

        // ---- Das Kollektorfeld des Beispiels ------------------------------------
        private const double APERTUR = 2.5;    // m² je Modul
        private const int MODULE = 10;         // Module  ⇒ 25 m²
        private const double KW = 17.5;        // 0,7 kW/m² × 25 m²
        private const double SATZ_KW = 700.0;  // €/kW  ⇒ 12.250,00 €
        private const double SATZ_M2 = 400.0;  // €/m²  ⇒ 10.000,00 €

        // Die Kultur ist auf de-DE gepinnt: Der Herleitungstext trägt Zahlen, und
        // ein Testlauf unter en-US soll dieselben sehen wie der Anwender
        // (Rückstellung wie iU9-#167 — die Kultur ist threadgebunden).
        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public SolarthermieLeistungTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void BeispielAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'Solarleistung 14.09.')");
            Sql("INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (" + STAMM_ID + ", 'Solarthermie', 1)");

            Sql("INSERT INTO Tab_Solarkollektoren (ID, ID_Projekt, Bezeichner, Aperturflaeche) VALUES (" +
                G_SOLAR + ", " + PROJEKT + ", 'Kollektor 2,5', " + Z(APERTUR) + ")");
            Sql("INSERT INTO Tab_Solarkollektoren (ID, ID_Projekt, Bezeichner, Aperturflaeche) VALUES (" +
                G_LEER + ", " + PROJEKT + ", 'Kollektor ohne Flaeche', 0)");

            Anlage(A_SOLAR, "Solarfeld", G_SOLAR, MODULE);
            Anlage(A_LEER, "Solarfeld ohne Flaeche", G_LEER, MODULE);

            Position(P_KW_LEISTUNG, A_SOLAR, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_KW);
            Position(P_KW_HEIZ, A_SOLAR, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, SATZ_KW);
            Position(P_M2, A_SOLAR, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, SATZ_M2);
            Position(P_OHNE_FELD, A_LEER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, SATZ_KW);
        }

        /// <summary>Die sieben Geräteverweise müssen ALLE gesetzt werden — ihr
        /// Spaltenvorgabewert 0 läse sich als Verweis auf ein Gerät, das es nicht
        /// gibt (derselbe Befund wie in <see cref="BetriebskostenBaugroesseTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Anlage(int id, string name, int geraet, int module)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = id + ", " + PROJEKT + ", '" + name + "', " + WizardItemClass.SOLAR_TYP;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (string.Equals(s, "ID_Solar", StringComparison.Ordinal)
                                 ? geraet.ToString() : "NULL");
            }
            spalten += ", [Kollektormodulanzahl]";
            werte += ", " + module;
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        /// <summary>Eine satzbasierte INVESTITIONSzeile — EingegebenerWert 0, wie der
        /// Dialog sie speichert; genau der Fall des Bildschirmfotos.</summary>
        private static void Position(int id, int anlage, string bemessung, double satz)
        {
            Sql("INSERT INTO Tab_ProjektWerte " +
                "(ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                " Nutzungsdauer, Kostenart, Bemessung, Einheitpreis, ID_Anlage) VALUES (" +
                id + ", " + PROJEKT + ", " + STAMM_ID + ", " + KOMPONENTE + ", " +
                DbWerte.KOSTEN_KATEGORIE_INVESTITION + ", 0.0, 20, '" +
                DbWerte.KOSTENART_KAPITALGEBUNDEN + "', '" + bemessung + "', " +
                Z(satz) + ", " + anlage + ")");
        }

        /// <summary><c>Tab_KostenKomponente.ID</c> der Solarthermie.</summary>
        private const int KOMPONENTE = 4;

        private static string Z(double wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }

        /// <summary>Der wirksame Investitionsbetrag EINER Zeile aus dem Rechenweg
        /// des Kerns — derselbe Weg, den Dialog und Kapitalwertrechnung gehen.</summary>
        private static InvestKaskade.Zeile Kaskadenzeile(int positionsId)
        {
            Dictionary<int, InvestKaskade.Zeile> karte =
                InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            return karte[positionsId];
        }

        // =====================================================================
        // Die Leistung des Kollektorfelds
        // =====================================================================

        /// <summary>
        /// DER BEFUND: „je kW Leistung" an der Solarthermie. 0,7 kW/m² × 2,5 m² ×
        /// 10 Module = 17,5 kW. VOR der Festlegung kannte die Landkarte für das
        /// Gewerk keine Leistung — <c>BaugroesseSumme</c> gab null, der Betrag 0.
        /// </summary>
        [Fact]
        public void Solarthermie_je_kW_Leistung_rechnet_mit_der_Kollektorfeldleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(KW, TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, KOMPONENTE, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, A_SOLAR).Value, 6);
        }

        /// <summary>
        /// „je kW Heizleistung" trifft DIESELBE Leistung: Ein Kollektorfeld hat nur
        /// eine, und sie ist thermisch — dieselbe H4c-Regel wie bei Kessel und
        /// Wärmepumpe (zwei Namen, EINE Größe).
        /// </summary>
        [Fact]
        public void Solarthermie_je_kW_Heizleistung_trifft_dieselbe_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(KW, TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, KOMPONENTE, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, A_SOLAR).Value, 6);
        }

        /// <summary>Die Leistung ist die Fläche mal der Konvention — nicht eine
        /// zweite, eigene Rechnung. 25 m² × 0,7 kW/m² = 17,5 kW.</summary>
        [Fact]
        public void Die_Leistung_ist_die_Aperturflaeche_mal_der_Konvention()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            double m2 = TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, KOMPONENTE, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, A_SOLAR).Value;

            Assert.Equal(APERTUR * MODULE, m2, 6);
            Assert.Equal(m2 * TechnikPlanwertCtrl.KOLLEKTOR_KW_JE_M2,
                         TechnikPlanwertCtrl.KollektorfeldKw(PROJEKT, A_SOLAR).Value, 6);
        }

        /// <summary>DER BETRAG DES BILDSCHIRMFOTOS: 700 €/kW × 17,5 kW =
        /// 12.250,00 €. Er kommt aus der Investitionskaskade — demselben Rechenweg,
        /// den auch Kapitalwert und Kostenseite lesen.</summary>
        [Fact]
        public void Der_Satz_je_kW_ergibt_den_Betrag_der_Kostenverwaltung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            InvestKaskade.Zeile z = Kaskadenzeile(P_KW_LEISTUNG);
            Assert.Equal(KW, z.Basis.Value, 6);
            Assert.Equal(SATZ_KW * KW, z.Betrag, 6);      // 12.250,00 €
        }

        /// <summary>Die Flächenbemessung bleibt unverändert — die Festlegung tritt
        /// neben sie, nicht an ihre Stelle. 400 €/m² × 25 m² = 10.000,00 €.</summary>
        [Fact]
        public void Die_Flaechenbemessung_bleibt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            InvestKaskade.Zeile z = Kaskadenzeile(P_M2);
            Assert.Equal(APERTUR * MODULE, z.Basis.Value, 6);
            Assert.Equal(SATZ_M2 * APERTUR * MODULE, z.Betrag, 6);
        }

        // =====================================================================
        // Die Landkarte Art ↔ Gewerk
        // =====================================================================

        /// <summary>H4c: Die Solarthermie KENNT die beiden thermischen
        /// Leistungsarten jetzt — ein fehlender Betrag ist dort ein Pflegebefund
        /// („kein Gerät"), nicht mehr „die Art passt nicht zum Gewerk".</summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR)]
        public void Die_thermischen_Arten_gehoeren_zur_Solarthermie(string bemessung)
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(KOMPONENTE, bemessung));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET,
                         WirtschaftlichkeitCtrl.BasisGrund(bemessung, KOMPONENTE));
        }

        /// <summary>Die STROMarten bleiben der Solarthermie fremd — ein Kollektor
        /// erzeugt keinen Strom. Die H4c-Regel wird nicht aufgeweicht.</summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWP)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET)]
        public void Die_Stromarten_bleiben_der_Solarthermie_fremd(string bemessung)
        {
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(KOMPONENTE, bemessung));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrund(bemessung, KOMPONENTE));
        }

        /// <summary>Und die kW-Arten bleiben an den ÜBRIGEN Gewerken, wie sie waren
        /// — die Festlegung greift nur an der Komponente 4. BHKW und Pufferspeicher
        /// haben ihre Bezugsgröße erst mit dem ANWENDERENTSCHEID 15.09.2026 bekommen
        /// (<see cref="BemessungBhkwPufferspeicherTests"/>), nicht mit dieser
        /// Festlegung; für die Photovoltaik bleibt eine Heizleistung fremd.</summary>
        [Fact]
        public void Die_Festlegung_beruehrt_kein_anderes_Gewerk()
        {
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(
                3, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG));    // Photovoltaik
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(
                6, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET));     // Pufferspeicher: keine kWh
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(
                2, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH));      // Heizkessel
        }

        // =====================================================================
        // Der Herleitungstext
        // =====================================================================

        /// <summary>
        /// Die 17,5 kW stehen in keiner Gerätemaske — deshalb nennt der Kern die
        /// Formel: „0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW".
        /// </summary>
        [Fact]
        public void Der_Herleitungstext_nennt_die_Formel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            string text = TechnikPlanwertCtrl.KollektorfeldHerleitung(PROJEKT, A_SOLAR);

            Assert.Equal("0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW", text);
        }

        /// <summary>Denselben Satz bekommt, wer über die allgemeine Einstiegstür
        /// fragt — es gibt nur EINE Herleitung, nicht eine je Aufrufer.</summary>
        [Fact]
        public void Die_allgemeine_Einstiegstuer_liefert_denselben_Satz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(TechnikPlanwertCtrl.KollektorfeldHerleitung(PROJEKT, A_SOLAR),
                         TechnikPlanwertCtrl.BaugroesseHerleitung(
                             PROJEKT, KOMPONENTE, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, A_SOLAR));

            // Eine Größe, die am Gerät abzulesen ist, erklärt sich selbst.
            Assert.Equal("", TechnikPlanwertCtrl.BaugroesseHerleitung(
                PROJEKT, KOMPONENTE, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, A_SOLAR));
        }

        /// <summary>Ohne Kollektorfeld gibt es nichts herzuleiten — und keinen
        /// erfundenen Satz.</summary>
        [Fact]
        public void Ohne_Kollektorfeld_bleibt_der_Herleitungstext_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal("", TechnikPlanwertCtrl.KollektorfeldHerleitung(PROJEKT, A_LEER));
            Assert.Null(TechnikPlanwertCtrl.KollektorfeldKw(PROJEKT, A_LEER));
        }

        // =====================================================================
        // Der Dialog — kein stilles 0
        // =====================================================================

        /// <summary>
        /// Die Zeile der Kostenverwaltung zeigt Betrag, Bezugsgröße UND Herleitung
        /// aus demselben Rechenweg — der Fall des Bildschirmfotos, jetzt mit Zahl.
        /// </summary>
        [Fact]
        public void Die_Dialogzeile_traegt_Betrag_Bezugsgroesse_und_Herleitung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_SOLAR);

            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_KW_LEISTUNG);
            Assert.Equal(SATZ_KW * KW, z.Raster.BetragNetto.Value, 6);
            Assert.Equal(KW, z.Basis.Value, 6);
            Assert.Equal("", z.BasisGrund);
            Assert.Equal("0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW", z.BasisHerleitung);
        }

        /// <summary>
        /// KEIN STILLES 0: Trägt das Kollektorfeld keine Fläche, bleibt es beim
        /// erfassten Betrag (Anwenderentscheid I-2) — die Zeile nennt aber den
        /// GRUND, und zwar den der PFLEGE („kein Gerät mit dieser Baugröße"), nicht
        /// den des Gewerks.
        /// </summary>
        [Fact]
        public void Ohne_Flaeche_nennt_die_Zeile_den_Pflegegrund_statt_still_0()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_LEER);

            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_OHNE_FELD);
            Assert.Null(z.Basis);
            Assert.Equal(0.0, z.Raster.BetragNetto.Value, 6);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET, z.BasisGrund);
            Assert.Equal("", z.BasisHerleitung);
        }

        /// <summary>
        /// Wechselt der Anwender die Bemessung von „je m² Kollektorfläche" auf
        /// „je kW Leistung", zieht der Dialog Bezugsgröße UND Herleitung sofort nach
        /// — sonst rechnete er bis zum Speichern mit den 25 m² weiter.
        /// </summary>
        [Fact]
        public void Der_Wechsel_auf_je_kW_zieht_Leistung_und_Herleitung_nach()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            List<KostenProjektPositionenCtrl.Zeile> zeilen =
                KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE,
                                                 DbWerte.KOSTEN_KATEGORIE_INVESTITION, A_SOLAR);
            KostenProjektPositionenCtrl.Zeile z = zeilen.Find(x => x.Raster.Id == P_M2);
            Assert.Equal(APERTUR * MODULE, z.Basis.Value, 6);
            Assert.Equal("", z.BasisHerleitung);

            KostenProjektPositionenCtrl.BasisNachziehen(z, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG);

            Assert.Equal(KW, z.Basis.Value, 6);
            Assert.Equal("", z.BasisGrund);
            Assert.Equal("0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW", z.BasisHerleitung);
        }
    }
}
