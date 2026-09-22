using System;
using System.Globalization;
using System.Resources;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Stromträger-Rückfall der CO₂-SEITE</b> (Anwenderentscheid 15.09.2026:
    /// „bereits zugewiesene CO₂-Zahlen nicht überschreiben").
    ///
    /// <para><b>Die Lage.</b> Die KOSTENseite bepreist den Netzbezug seit dem
    /// Anwenderbefund vom 14.09.2026 mit dem Auslieferungsträger des Katalogs, wenn dem
    /// Projekt keiner zugeordnet ist. Die CO₂-Seite hatte diesen Rückfall nicht: Sie
    /// rechnete dann mit dem anonymen Vorgabewert
    /// <see cref="Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH"/> — dieselbe Lage, zwei
    /// Antworten.</para>
    ///
    /// <para><b>Die Regel, die hier festgehalten wird: Der Rückfall FÜLLT NUR LÜCKEN.</b>
    /// Er greift allein dort, wo gar kein Stromträger zugeordnet ist — wo also keine Zahl
    /// aus den Projektdaten stand, sondern der Vorgabewert. Wo ein Träger zugeordnet ist,
    /// bleibt sein Faktor unangetastet; trägt der zugeordnete Träger selbst keinen, bleibt
    /// es beim Vorgabewert wie bisher (dieselbe Regel, die <see cref="Emissionsquelle.Fuer"/>
    /// für den Brennstoff-Rückfall trägt und die der Wächter in
    /// <see cref="EnergietraegerRueckfallTests"/> hält).</para>
    ///
    /// <para><b>Und die Grenze, die der Datenstand zieht.</b> „Keine Zahl" und „Zahl ist 0"
    /// sind in den vier Ebenen der Lesekette nicht unterscheidbar: Jede Ebene zählt einen
    /// Wert erst ab „größer als 0" als gepflegt, und die drei Altspalten
    /// (<c>energy_project_settings.co2</c>, <c>energy_carrier.co2</c>,
    /// <c>Tab_Brennstoff_Stamm.CO2</c>) stehen im Schema auf <c>DEFAULT 0</c> statt auf
    /// <c>NULL</c>. Der letzte Fall hält das fest — er ist die Begründung dafür, dass der
    /// Rückfall hier an der TRÄGERZUORDNUNG ansetzt und nicht am Wert.</para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an — die Fälle schreiben (Katalog-
    /// und Projektwerte). <c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class Co2StromtraegerRueckfallTests : IDisposable
    {
        /// <summary>Pinnt die Kultur der ganzen Klasse (Hausmuster): Die Hinweiszeile
        /// kommt aus den Satellitenressourcen und folgt <c>CurrentUICulture</c>.</summary>
        private readonly Kulturvorrichtung _kultur = new();

        /// <summary>Stellt die vier Kulturwerte zurueck.</summary>
        public void Dispose() => _kultur.Dispose();

        /// <summary>„Beispiel WP WG 1" — Wärmepumpe, PV und Stromspeicher ohne
        /// <c>ID_Carrier</c>, dem Projekt ist nur „Erdgas E" zugeordnet: die LÜCKE.
        /// Netzbezug 19,08 MWh/a.</summary>
        private const int PROJEKT_OHNE_STROM = 1026;

        /// <summary>„Wöhler – Test2" — führt „Elektrische Energie" zugeordnet und mit
        /// Projekt-CO₂-Wert 560 g/kWh: die Gegenprobe, an der sich nichts ändern darf.</summary>
        private const int PROJEKT_MIT_STROM = 1024;

        /// <summary>Ein Projekt, dem zwei Stromträger mit Projekt-CO₂-Wert <b>0</b>
        /// zugeordnet sind — die gepflegte Null.</summary>
        private const int PROJEKT_MIT_NULL = 1017;

        /// <summary>„Simulation Referenz BHKW-Kaskade" — Gaskessel, zwei Gas-BHKW und ein
        /// Puffer, ohne Wärmepumpe, PV, Stromspeicher und Heizstab; „Elektrische Energie"
        /// zugeordnet mit Projekt-CO₂-Wert 560 g/kWh, Netzbezug 4.357,78 MWh/a. Ohne
        /// seine BHKW (<see cref="OhneBhkw"/>) ist es ein reines Kesselprojekt.</summary>
        private const int PROJEKT_OHNE_ELEKTRIK = 1030;

        /// <summary>Der Netzbezug des Projekts 1030 [MWh/a] aus dem gespeicherten Lauf.</summary>
        private const double NETZBEZUG_1030_MWH = 4357.78;

        /// <summary>Der Projekt-CO₂-Wert des zugeordneten Stromträgers von 1030 [g/kWh].</summary>
        private const double FAKTOR_1030 = 560.0;

        /// <summary><c>energy_carrier.id</c> von „Elektrische Energie" — der
        /// Auslieferungsträger des Katalogs.</summary>
        private const int STROM = 60;

        /// <summary><c>energy_carrier.id</c> von „Strom Variante" — der Träger, den
        /// Projekt 1017 führt.</summary>
        private const int STROM_VARIANTE = 54;

        /// <summary>Der Netzbezug des Projekts 1026 [MWh/a] aus dem gespeicherten
        /// Simulationsergebnis.</summary>
        private const double NETZBEZUG_MWH = 19.08;

        // =================================================================
        // 1 — Die Lücke wird gefüllt
        // =================================================================

        /// <summary>
        /// Ohne zugeordneten Stromträger rechnet die CO₂-Seite mit dem Faktor des
        /// Auslieferungsträgers — und sagt, dass der Wert GELIEHEN ist.
        /// </summary>
        [Fact]
        public void Ohne_zugeordneten_Stromtraeger_fuellt_der_Auslieferungstraeger_die_Luecke()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Emissionsquelle.StromTraeger(PROJEKT_OHNE_STROM));   // Ausgangslage
            KatalogFaktor(STROM, 500.0);

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(
                PROJEKT_OHNE_STROM, Emissionsquelle.Modus(PROJEKT_OHNE_STROM));

            Assert.Equal(500.0, f.Co2GKwh, 6);
            Assert.True(f.Co2Gepflegt);
            Assert.Equal(STROM, f.RueckfallTraegerId);
            Assert.Contains("kein Stromträger zugeordnet", f.Herkunft);
        }

        /// <summary>
        /// Derselbe Fall im Rechner: Die Herleitung steht an der Variante, und die
        /// Strommix-Fahne fällt — die Zahl stammt jetzt aus dem Katalog, nicht aus dem
        /// Vorgabewert.
        /// </summary>
        [Fact]
        public void Der_Rechner_vermerkt_den_geliehenen_Traeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);
            VariantenDaten v = Rechne(PROJEKT_OHNE_STROM);

            Assert.Equal("Elektrische Energie", v.CO2TraegerRueckfall);
            Assert.False(v.CO2StrommixRueckfall);
        }

        /// <summary>
        /// Der geliehene Faktor wirkt auf GENAU den Netzbezug und auf nichts sonst:
        /// Zwei Läufe mit 500 und 300 g/kWh unterscheiden sich um
        /// 19,08 MWh × 200 g/kWh ÷ 1000 = 3,816 t/a. Der Brennstoffanteil der Bilanz
        /// bleibt dabei außen vor — er ändert sich nicht mit.
        /// </summary>
        [Fact]
        public void Der_geliehene_Faktor_wirkt_nur_auf_den_Netzbezug()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);
            double hoch = Rechne(PROJEKT_OHNE_STROM).CO2Gesamt.Value;

            KatalogFaktor(STROM, 300.0);
            double tief = Rechne(PROJEKT_OHNE_STROM).CO2Gesamt.Value;

            Assert.Equal(NETZBEZUG_MWH * 200.0 / 1000.0, hoch - tief, 6);
        }

        // =================================================================
        // 2 — Ein vorhandener Satz bleibt
        // =================================================================

        /// <summary>
        /// Der Projektwert des ZUGEORDNETEN Trägers gewinnt — auch wenn der
        /// Auslieferungsträger einen ganz anderen Faktor trägt. Genau das war die Frage
        /// des Anwenders: Der Rückfall darf keine ausgewiesene Zahl verschieben.
        /// </summary>
        [Fact]
        public void Ein_zugeordneter_Stromtraeger_behaelt_seinen_Faktor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);   // der Auslieferungsträger — hier belanglos
            Assert.Equal(STROM, Emissionsquelle.StromTraeger(PROJEKT_MIT_STROM));

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(
                PROJEKT_MIT_STROM, Emissionsquelle.Modus(PROJEKT_MIT_STROM));

            Assert.Equal(560.0, f.Co2GKwh, 6);                    // Projektwert
            Assert.Equal(EmissionsFaktorLader.EBENE_PROJEKT, f.Ebene);
            Assert.Equal(0, f.RueckfallTraegerId);

            Assert.Null(Rechne(PROJEKT_MIT_STROM).CO2TraegerRueckfall);
        }

        /// <summary>
        /// Trägt der zugeordnete Träger in KEINER Ebene einen Faktor, bleibt es beim
        /// Vorgabewert — der Rückfall springt NICHT ein, obwohl der Auslieferungsträger
        /// einen Faktor hätte. Dieselbe Regel wie beim Brennstoff-Rückfall: Die
        /// Lesekette hat vier Ebenen durchsucht; ein zweiter Anlauf über einen fremden
        /// Träger verdeckte die Lücke, statt sie zu füllen.
        /// </summary>
        [Fact]
        public void Ein_zugeordneter_Traeger_ohne_Faktor_bleibt_beim_Vorgabewert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);                       // der Geber hätte etwas
            Stromtraeger_zuordnen(PROJEKT_OHNE_STROM, STROM_VARIANTE);
            Faktorlos(STROM_VARIANTE);

            Assert.Equal(STROM_VARIANTE, Emissionsquelle.StromTraeger(PROJEKT_OHNE_STROM));

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(
                PROJEKT_OHNE_STROM, Emissionsquelle.Modus(PROJEKT_OHNE_STROM));

            Assert.Equal(Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH, f.Co2GKwh, 6);
            Assert.False(f.Co2Gepflegt);
            Assert.Equal(0, f.RueckfallTraegerId);

            VariantenDaten v = Rechne(PROJEKT_OHNE_STROM);
            Assert.True(v.CO2StrommixRueckfall);
            Assert.Null(v.CO2TraegerRueckfall);
        }

        /// <summary>
        /// Ein reines Kesselprojekt bekommt keinen Rückfall: Kein Erzeuger verwendet dort
        /// Strom, und ein Träger, den niemand zugeordnet hat, wäre eine Erfindung.
        /// Dieselbe Klemme wie auf der Kostenseite.
        ///
        /// <para>Bis zum Anwenderentscheid vom 22.09.2026 stand hier 1030 UNVERÄNDERT —
        /// seine zwei BHKW zählen seither als Stromverwendung (siehe den Fall darunter);
        /// das reine Kesselprojekt entsteht deshalb erst ohne sie.</para>
        /// </summary>
        [Fact]
        public void Ohne_elektrisches_Gewerk_gibt_es_keinen_Rueckfalltraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            OhneBhkw(PROJEKT_OHNE_ELEKTRIK);
            Assert.Equal(0, Emissionsquelle.KatalogStromTraeger(PROJEKT_OHNE_ELEKTRIK));
            Assert.Equal(STROM, Emissionsquelle.KatalogStromTraeger(PROJEKT_OHNE_STROM));
        }

        /// <summary>
        /// <b>Das BHKW verwendet Strom</b> (Anwenderentscheide 22.09.2026): Es erzeugt ihn,
        /// sein Eigenverbrauch deckt den Strombedarf, der Rest wird bezogen. Ein
        /// BHKW-Projekt bekommt deshalb denselben Rückfallträger wie ein
        /// Wärmepumpenprojekt.
        /// </summary>
        [Fact]
        public void Ein_BHKW_zaehlt_als_Stromverwendung_und_bekommt_den_Rueckfalltraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_OHNE_ELEKTRIK));
            Assert.Equal(STROM, Emissionsquelle.KatalogStromTraeger(PROJEKT_OHNE_ELEKTRIK));
        }

        // =================================================================
        // 2b — Strombedarf ohne Verwendung (Anwenderentscheide 22.09.2026)
        // =================================================================

        /// <summary>
        /// <b>Ohne Verwendung keine Emission</b> — auch mit ZUGEORDNETEM Stromträger und
        /// gepflegtem Projektfaktor. 1030 ohne seine BHKW führt einen Netzbezug von
        /// 4.357,78 MWh/a, aber keinen Erzeuger, der Strom verwendet: Der Netzbezug trägt
        /// zur CO₂-Bilanz nichts mehr bei — die Differenz zum Lauf MIT BHKW ist genau
        /// 4.357,78 MWh × 560 g/kWh ÷ 1000 = 2.440,36 t/a. Der Brennstoffanteil bleibt
        /// (das gespeicherte Ergebnis ist in beiden Rechnungen dasselbe).
        /// </summary>
        [Fact]
        public void Ohne_Verwendung_traegt_der_Netzbezug_nichts_zur_Bilanz_bei()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten mit = Rechne(PROJEKT_OHNE_ELEKTRIK);
            Assert.Null(mit.StrombedarfOhneVerwendungMWh);
            Assert.True(mit.CO2Gesamt.HasValue);

            OhneBhkw(PROJEKT_OHNE_ELEKTRIK);
            VariantenDaten ohne = Rechne(PROJEKT_OHNE_ELEKTRIK);

            Assert.Equal(NETZBEZUG_1030_MWH, ohne.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.True(ohne.CO2Gesamt.HasValue);
            Assert.Equal(NETZBEZUG_1030_MWH * FAKTOR_1030 / 1000.0,
                         mit.CO2Gesamt.Value - ohne.CO2Gesamt.Value, 4);
            // Kein Faktor gezogen - also auch keine Herleitung eines geliehenen oder
            // vorgegebenen Werts.
            Assert.False(ohne.CO2StrommixRueckfall);
            Assert.Null(ohne.CO2TraegerRueckfall);
            // Die BEHG-Basis kennt ohnehin nur Brennstoff: unverändert.
            Assert.Equal(mit.CO2Brennstoff, ohne.CO2Brennstoff);
        }

        /// <summary>
        /// Ohne Verwendung und OHNE zugeordneten Stromträger: weder der Rückfallträger noch
        /// der Vorgabewert werden gezogen — auch dann nicht, wenn der Auslieferungsträger
        /// einen Faktor trägt.
        /// </summary>
        [Fact]
        public void Ohne_Verwendung_greift_weder_Rueckfall_noch_Vorgabewert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            OhneBhkw(PROJEKT_OHNE_ELEKTRIK);
            DataRepository.ExecuteSQL(
                "DELETE FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT_OHNE_ELEKTRIK), new DbParam("@c", STROM));
            Assert.Equal(0, Emissionsquelle.StromTraeger(PROJEKT_OHNE_ELEKTRIK));
            KatalogFaktor(STROM, 500.0);

            VariantenDaten v = Rechne(PROJEKT_OHNE_ELEKTRIK);

            Assert.NotNull(v.StrombedarfOhneVerwendungMWh);
            Assert.False(v.CO2StrommixRueckfall);
            Assert.Null(v.CO2TraegerRueckfall);
        }

        /// <summary>
        /// <b>Die Gegenprobe mit BHKW:</b> Der Netzbezug wird weiter mit dem Faktor des
        /// zugeordneten Trägers bewertet — 4.357,78 MWh × 560 g/kWh ÷ 1000 = 2.440,36 t/a
        /// gegenüber einem Lauf ohne Netzbezug.
        /// </summary>
        [Fact]
        public void Mit_BHKW_wird_der_Netzbezug_weiter_bewertet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten mit = Rechne(PROJEKT_OHNE_ELEKTRIK);

            ErgebnisModel ohneBezug = new ErgebnisCtrl().Load(PROJEKT_OHNE_ELEKTRIK);
            ohneBezug.Energiebedarf.Stromrestbedarf = 0;
            var v0 = new VariantenDaten { IdProjekt = PROJEKT_OHNE_ELEKTRIK, Ergebnis = ohneBezug };
            KostenEmissionRechner.Berechne(v0);

            Assert.Null(mit.StrombedarfOhneVerwendungMWh);
            Assert.False(mit.CO2StrommixRueckfall);
            Assert.Equal(NETZBEZUG_1030_MWH * FAKTOR_1030 / 1000.0,
                         mit.CO2Gesamt.Value - v0.CO2Gesamt.Value, 4);
        }

        // =================================================================
        // 3 — Die gepflegte Null
        // =================================================================

        /// <summary>
        /// Ein Projekt, dessen zugeordneter Stromträger den Projektwert <b>0</b> trägt,
        /// zieht KEINEN Rückfall: Es entscheidet die Lesekette SEINES Trägers, nicht der
        /// Auslieferungsträger.
        /// </summary>
        [Fact]
        public void Eine_gepflegte_Null_zieht_keinen_Rueckfall()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);            // der Auslieferungsträger
            KatalogFaktor(STROM_VARIANTE, 300.0);   // der zugeordnete Träger

            Assert.Equal(0.0, Projektwert(PROJEKT_MIT_NULL, STROM_VARIANTE), 6);
            Assert.Equal(STROM_VARIANTE, Emissionsquelle.StromTraeger(PROJEKT_MIT_NULL));

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(
                PROJEKT_MIT_NULL, Emissionsquelle.Modus(PROJEKT_MIT_NULL));

            Assert.Equal(300.0, f.Co2GKwh, 6);
            Assert.Equal(0, f.RueckfallTraegerId);
        }

        /// <summary>
        /// <b>Der Befund, der die Reichweite des Rückfalls begrenzt</b> (Auftrag #293,
        /// Punkt 2): „Keine Zahl" und „Zahl ist 0" sind im heutigen Datenstand NICHT
        /// unterscheidbar. Jede Ebene der Lesekette zählt erst „größer als 0" als
        /// gepflegt, und die Altspalten stehen im Schema auf <c>DEFAULT 0</c> — eine
        /// bewusst eingetragene 0 sieht aus wie eine nie gefüllte Spalte und fällt
        /// durch auf die nächste Ebene.
        ///
        /// <para>Deshalb setzt der Rückfall an der TRÄGERZUORDNUNG an und nicht am Wert:
        /// „kein Träger zugeordnet" ist eine Tatsache der Struktur, die keine Deutung
        /// braucht. Fällt dieser Fall, ist die Frage neu zu stellen — und zwar beim
        /// Anwender.</para>
        /// </summary>
        [Fact]
        public void Eine_gepflegte_Null_ist_heute_nicht_von_einer_Luecke_zu_unterscheiden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KatalogFaktor(STROM, 500.0);

            // Die 0 im Projektwert wirkt wie eine leere Spalte: Die Kette geht weiter.
            Assert.Equal(0.0, Projektwert(PROJEKT_MIT_NULL, STROM_VARIANTE), 6);
            EmissionsFaktorSatz satz = EmissionsFaktorLader.Lade(PROJEKT_MIT_NULL, STROM_VARIANTE);
            Assert.NotEqual(EmissionsFaktorLader.EBENE_PROJEKT, satz.Co2Ebene);
            Assert.True(satz.Co2GKwh.HasValue && satz.Co2GKwh.Value > 0);

            // Und genauso auf der Katalogebene: 0 zählt nicht als gepflegter Wert.
            KatalogFaktor(STROM_VARIANTE, 0.0);
            satz = EmissionsFaktorLader.Lade(PROJEKT_MIT_NULL, STROM_VARIANTE);
            Assert.NotEqual(EmissionsFaktorLader.EBENE_KATALOG, satz.Co2Ebene);
        }

        // =================================================================
        // 4 — Die fünf Referenzprojekte
        // =================================================================

        /// <summary>
        /// <b>Der Rückfall verschiebt in den fünf CI-Projekten KEINE Zahl</b> — die
        /// Abnahme des Auftrags in Testform.
        ///
        /// <para><b>1030</b> (560 g/kWh, Projektwert am zugeordneten Träger) und
        /// <b>1017</b> (435 g/kWh aus der aktiven Katalogzeile seines Trägers) führen
        /// einen zugeordneten Stromträger — für sie ändert sich schon deshalb nichts.
        /// <b>1007</b>, <b>1045</b> und <b>1046</b> führen keinen: Sie bekommen über den
        /// Rückfall den Faktor des Auslieferungsträgers — und der trägt in seiner aktiven
        /// Katalogzeile genau die 435 g/kWh, mit denen sie vorher als Vorgabewert
        /// gerechnet haben. Der Fall wird rot, sobald jemand einen gesäten Faktor
        /// verschiebt; dann ist die Referenzbasis ohnehin neu einzufrieren
        /// (Einfrierregel).</para>
        /// </summary>
        /// <param name="idProjekt">Das Referenzprojekt.</param>
        /// <param name="erwartet">Sein Netzstrom-Faktor [g/kWh].</param>
        /// <param name="ausRueckfall">Kam er aus dem Rückfall?</param>
        [Theory]
        [InlineData(1030, 560.0, false)]
        [InlineData(1007, 435.0, true)]
        [InlineData(1017, 435.0, false)]
        [InlineData(1045, 435.0, true)]
        [InlineData(1046, 435.0, true)]
        public void Die_fuenf_Referenzprojekte_behalten_ihren_Netzstromfaktor(
            int idProjekt, double erwartet, bool ausRueckfall)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(
                idProjekt, Emissionsquelle.Modus(idProjekt));

            Assert.Equal(erwartet, f.Co2GKwh, 6);
            Assert.Equal(ausRueckfall, f.RueckfallTraegerId > 0);

            // Wo der Rückfall greift, trägt er GENAU den bisherigen Vorgabewert —
            // deshalb bleiben die ausgewiesenen Zahlen dieser Projekte unverändert.
            if (ausRueckfall)
                Assert.Equal(Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH, f.Co2GKwh, 6);
        }

        // =================================================================
        // 5 — Die Herleitungszeile
        // =================================================================

        /// <summary>Der Schlüssel steht in beiden Ressourcendateien.</summary>
        [Theory]
        [InlineData("WIRT_CO2_TRAEGER_RUECKFALL")]
        public void Jeder_Schluessel_steht_in_beiden_Ressourcendateien(string schluessel)
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;
            Assert.False(string.IsNullOrEmpty(rm.GetString(schluessel, new CultureInfo("de-DE"))),
                         schluessel + " fehlt in Resource.resx");
            Assert.False(string.IsNullOrEmpty(rm.GetString(schluessel, new CultureInfo("en-US"))),
                         schluessel + " fehlt in Resource.en-US.resx");
        }

        /// <summary>
        /// Die Hinweiszeile kommt AUS DER RESSOURCE, nicht aus ihrem Rückfall — und sie
        /// führt den Platzhalter für den Trägernamen.
        /// </summary>
        [Fact]
        public void Die_Herleitungszeile_kommt_aus_der_Ressource()
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;
            Assert.Equal(rm.GetString("WIRT_CO2_TRAEGER_RUECKFALL", new CultureInfo("de-DE")),
                         KostenEmissionRechner.HINWEIS_CO2_TRAEGER_RUECKFALL);
            Assert.Contains("{0}", KostenEmissionRechner.HINWEIS_CO2_TRAEGER_RUECKFALL);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static VariantenDaten Rechne(int idProjekt)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        /// <summary>Nimmt dem Projekt seine BHKW-Anlagenzeilen — aus 1030 wird ein reines
        /// Kesselprojekt. Das gespeicherte Ergebnis bleibt, wie es ist.</summary>
        private static void OhneBhkw(int idProjekt)
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0",
                new DbParam("@p", idProjekt));
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(idProjekt));
        }

        /// <summary>Setzt den AKTIVEN CO₂-Katalogwert eines Trägers (Ebene KATALOG).</summary>
        private static void KatalogFaktor(int carrierId, double wert)
        {
            DataRepository.ExecuteSQL(
                "UPDATE emissionswert SET wert = ? WHERE carrier_id = ? " +
                "AND emissionsart_id = (SELECT id FROM emissionsart WHERE kuerzel = ?) " +
                "AND ist_aktiv = 1",
                new DbParam("@w", wert), new DbParam("@c", carrierId),
                new DbParam("@a", DbWerte.EMISSIONSART_CO2));
        }

        /// <summary>Nimmt einem Träger JEDE CO₂-Quelle: Katalogzeile, Altspalte und den
        /// Weg in den Brennstoff-Stamm.</summary>
        private static void Faktorlos(int carrierId)
        {
            KatalogFaktor(carrierId, 0.0);
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET co2 = 0, ID_Brennstoff = 0 WHERE id = ?",
                new DbParam("@c", carrierId));
        }

        /// <summary>Der Projekt-CO₂-Wert einer Zuordnung; <c>-1</c> = keine Zeile.</summary>
        private static double Projektwert(int idProjekt, int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT co2 FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
            return (o == null || o == DBNull.Value) ? -1.0 : Convert.ToDouble(o);
        }

        /// <summary>Ordnet dem Projekt einen Stromträger zu — ohne Projekt-CO₂-Wert.</summary>
        private static void Stromtraeger_zuordnen(int idProjekt, int carrierId)
        {
            object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM energy_project_settings");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger], co2) " +
                "VALUES (?, ?, ?, 0)",
                new DbParam("@id", id), new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
        }
    }
}
