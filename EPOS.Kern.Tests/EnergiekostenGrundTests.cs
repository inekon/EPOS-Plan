using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 14.09.2026</b> (Auftrag #267):
    /// „Die Energiekosten sind 0 auch nach Berechnung. Kosten sind angegeben."
    ///
    /// <para><b>Die Lage im Bestand.</b> Projekt 1026 „Beispiel WP WG 1" führt eine
    /// Wärmepumpe, eine PV-Anlage und einen Stromspeicher. Keine dieser Anlagen trägt
    /// einen eigenen <c>ID_Carrier</c>, und dem Projekt ist in
    /// <c>energy_project_settings</c> nur „Erdgas E" zugeordnet — kein Stromträger.
    /// <see cref="KostenEmissionRechner"/> fand deshalb keinen Preis für den Netzbezug
    /// (19,08 MWh/a), <c>Energiekosten</c> blieb <c>null</c>, und die Seite zeigte „—"
    /// samt der irreführenden Aufforderung, die Arbeitspreise zu prüfen.</para>
    ///
    /// <para><b>Was hier festgehalten wird — drei Dinge.</b>
    /// <list type="number">
    ///   <item><description>Der RÜCKFALL: Ohne zugeordneten Stromträger bepreist die
    ///     Kostenrechnung den Netzbezug mit dem Auslieferungsträger des Katalogs —
    ///     derselbe, den die Kostenseite anzeigt und der Assistent zuordnet
    ///     (<see cref="ProjektEnergietraegerCtrl.StandardStromTraeger"/>) — und
    ///     vermerkt das.</description></item>
    ///   <item><description>Die PREISQUELLE: Ein Preis, der ausschließlich als
    ///     Preisstand in <c>energy_price</c> gepflegt ist, zählt. Vorher kannte die
    ///     Kette nur <c>energy_project_settings</c> und den Katalog, während
    ///     <c>StromPreisCtrl</c> dieselbe Datenbank über die Historie las.</description></item>
    ///   <item><description>KEIN STILLES NULL: Bleibt die Zahl aus, nennt
    ///     <c>EnergiekostenGrund</c> den Grund samt Ausweg, und
    ///     <c>WirtschaftlichkeitCtrl</c> reicht ihn als <c>Fehlgrund</c> durch.</description></item>
    /// </list></para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an — die Fälle schreiben
    /// (Trägerzuordnung, Preise). <c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergiekostenGrundTests : IDisposable
    {
        /// <summary>
        /// Pinnt die Kultur der ganzen Klasse auf <c>de-DE</c> (Hausmuster aus Auftrag #230):
        /// <c>WirtschaftlichkeitErgebnis.Hinweis</c> kommt aus den Satellitenressourcen und folgt
        /// <c>CurrentUICulture</c>; ohne Pinnung liest ein Lauf unter <c>en-US</c> den englischen
        /// Text, und der Vergleich gegen den deutschen Wortlaut faellt.
        /// </summary>
        private readonly Kulturvorrichtung _kultur = new();

        /// <summary>Stellt die vier Kulturwerte zurueck.</summary>
        public void Dispose() => _kultur.Dispose();

        /// <summary>„Beispiel WP WG 1" — Wärmepumpe, PV und Stromspeicher ohne
        /// <c>ID_Carrier</c>, dem Projekt ist nur „Erdgas E" zugeordnet.</summary>
        private const int PROJEKT_WP = 1026;

        /// <summary>„Wöhler – Test2" — führt „Elektrische Energie" zugeordnet und mit
        /// Projekt-Arbeitspreis; die Gegenprobe, an der sich nichts ändern darf.</summary>
        private const int PROJEKT_MIT_STROM = 1024;

        /// <summary><c>energy_carrier.id</c> von „Elektrische Energie" — der
        /// Auslieferungsträger des Katalogs (BK1).</summary>
        private const int STROM = 60;

        // =================================================================
        // 1 — Der Rückfall auf den Auslieferungs-Stromträger
        // =================================================================

        /// <summary>
        /// DER BEFUND SELBST. Ohne zugeordneten Stromträger, aber mit einem
        /// Katalogpreis für den Auslieferungsträger, entstehen Energiekosten — und der
        /// Rückfall wird benannt, statt still zu wirken.
        /// </summary>
        [Fact]
        public void Ohne_zugeordneten_Stromtraeger_bepreist_der_Auslieferungstraeger_den_Netzbezug()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Emissionsquelle.StromTraeger(PROJEKT_WP));   // Ausgangslage
            Katalogpreis(STROM, 0.35);

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal("Elektrische Energie", v.StromTraegerRueckfall);
            // 19,08 MWh × 1000 × 0,35 €/kWh
            Assert.Equal(6678.0, v.Energiekosten.Value, 2);
        }

        /// <summary>
        /// Trägt auch der Auslieferungsträger keinen Preis, bleibt die Zahl aus — aber
        /// mit dem Grund, der zur Behebung führt: erst zuordnen, dann bepreisen. Der
        /// Rückfallvermerk steht dann NICHT, denn bepreist wurde nichts.
        /// </summary>
        [Fact]
        public void Ohne_Preis_nennt_der_Grund_die_fehlende_Traegerzuordnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.False(v.Energiekosten.HasValue);
            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, v.EnergiekostenGrund);
            Assert.Null(v.StromTraegerRueckfall);
        }

        // =================================================================
        // 2 — Die Preisquelle: die Historie zählt
        // =================================================================

        /// <summary>
        /// Ein Preis, der NUR als Preisstand gepflegt ist (<c>energy_price</c>), trägt
        /// die Energiekosten. Genau diese Lage entsteht, wenn jemand in der
        /// Trägerkarte ein „Gültig ab" setzt, ohne dass die Projektspalte einen Wert
        /// bekommt — <c>custom_price_work</c> steht dann auf 0 und galt bis
        /// Auftrag #267 als „kein Preis".
        /// </summary>
        [Fact]
        public void Ein_Preis_allein_in_der_Historie_traegt_die_Energiekosten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stromtraeger_zuordnen(PROJEKT_WP, STROM, projektpreis: 0.0);
            Historienpreis(PROJEKT_WP, STROM, 0.35, new DateTime(2026, 1, 1));

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(6678.0, v.Energiekosten.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);   // der Träger stand zugeordnet
        }

        /// <summary>
        /// Der PROJEKTWERT bleibt die erste Stufe: Steht er, gilt er — auch wenn die
        /// Historie einen anderen Preis führt. Sonst verschöbe diese Etappe die Zahlen
        /// jedes Projekts, das beides gepflegt hat.
        /// </summary>
        [Fact]
        public void Der_Projektwert_steht_vor_der_Historie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stromtraeger_zuordnen(PROJEKT_WP, STROM, projektpreis: 0.40);
            Historienpreis(PROJEKT_WP, STROM, 0.35, new DateTime(2026, 1, 1));

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(7632.0, v.Energiekosten.Value, 2);   // 19,08 MWh × 0,40 €/kWh
        }

        /// <summary>
        /// Ein Projekt mit zugeordnetem Stromträger und Projektpreis rechnet aus
        /// seinem Arbeitspreis — ohne Rückfall und ohne Fehlgrund.
        ///
        /// <para><b>Die Zahl ist mit Schemaschritt 83 gestiegen</b> (SP-E-2): Der
        /// Strompreis von 1024 trug bis dahin 35,0 ct/kWh, und die 11,746 ct/kWh des
        /// Aufschlagsblocks wirkten ausschließlich in der Speichersimulation. Seit der
        /// Faltung stehen sie IM Arbeitspreis (46,746 ct/kWh) und damit in jeder
        /// Rechnung, die ihn liest — genau der Bedeutungswechsel, den der Entscheid
        /// verlangt. Die Simulationsergebnisse bleiben davon unberührt (Referenzlauf
        /// byte-gleich); dies hier ist eine GELDgröße.</para>
        /// </summary>
        [Fact]
        public void Ein_Projekt_mit_gepflegtem_Projektpreis_rechnet_aus_seinem_Arbeitspreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_MIT_STROM);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(188167.18, v.Energiekosten.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);
            Assert.Null(v.EnergiekostenGrund);
        }

        // =================================================================
        // 3 — Der Grund erreicht die Wirtschaftlichkeit
        // =================================================================

        /// <summary>
        /// <b>Nie ein stummes „—".</b> Der <c>Fehlgrund</c> des
        /// Wirtschaftlichkeitsergebnisses trägt den benannten Grund des Rechners statt
        /// des pauschalen Satzes „Arbeitspreise/Träger prüfen" — der hat den
        /// Anwenderbefund mit verursacht, weil die Preise gepflegt waren.
        /// </summary>
        [Fact]
        public void Der_Fehlgrund_der_Wirtschaftlichkeit_nennt_den_Grund_des_Rechners()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var daten = new BerichtsDaten { IdStamm = PROJEKT_WP };
            VariantenDaten stamm = Rechne(PROJEKT_WP);
            stamm.IstStamm = true;
            daten.Varianten.Add(stamm);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT_WP)))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) erg = e;

            Assert.NotNull(erg);
            Assert.Null(erg.EnergiekostenJahr);
            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, erg.Fehlgrund);
        }

        /// <summary>
        /// Wirkt der Rückfall, steht er in der Hinweiszeile des Ergebnisses — die
        /// Zahl entsteht aus einem Träger, den das Projekt nicht führt, und das gehört
        /// gesagt.
        /// </summary>
        [Fact]
        public void Der_Rueckfall_steht_in_der_Hinweiszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreis(STROM, 0.35);

            var daten = new BerichtsDaten { IdStamm = PROJEKT_WP };
            VariantenDaten stamm = Rechne(PROJEKT_WP);
            stamm.IstStamm = true;
            daten.Varianten.Add(stamm);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT_WP)))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) erg = e;

            Assert.NotNull(erg);
            Assert.NotNull(erg.Hinweis);
            Assert.Contains("Elektrische Energie", erg.Hinweis);
            Assert.Contains("kein Stromträger zugeordnet", erg.Hinweis);
        }

        // =================================================================
        // 4 — Die Texte stehen in MyResource, in BEIDEN Sprachen
        // =================================================================

        /// <summary>
        /// <b>Der Nachweis, dass die Schlüssel greifen.</b> Ein vertippter Schlüssel
        /// fiele sonst nirgends auf: <c>T</c> lieferte klaglos den deutschen Rückfall,
        /// und die englische Oberfläche zeigte deutschen Text. Geprüft wird deshalb
        /// jeder der neun Schlüssel in der neutralen UND in der englischen
        /// Ressourcendatei — und dass die Eigenschaft wirklich den Ressourcentext
        /// liefert und nicht ihren Rückfall.
        /// </summary>
        [Theory]
        [InlineData("WIRT_GRUND_KEIN_STROMTRAEGER")]
        [InlineData("WIRT_GRUND_STROMPREIS_FEHLT")]
        [InlineData("WIRT_GRUND_BRENNSTOFFPREIS_FEHLT")]
        [InlineData("WIRT_GRUND_VERBRAUCH_OHNE_TRAEGER")]
        [InlineData("WIRT_GRUND_KEIN_VERBRAUCH")]
        [InlineData("WIRT_GRUND_RECHENFEHLER")]
        [InlineData("WIRT_STROMTRAEGER_RUECKFALL")]
        [InlineData("WIRT_BTN_NEU_BERECHNEN")]
        [InlineData("WIRT_BAND_NACHRECHNEN")]
        [InlineData("WIRT_BAND_SIMULATION_VERALTET")]
        [InlineData("WIRT_HINWEIS_STROMBEDARF_OHNE_VERWENDUNG")]
        public void Jeder_Schluessel_steht_in_beiden_Ressourcendateien(string schluessel)
        {
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;
            Assert.False(string.IsNullOrEmpty(rm.GetString(schluessel, new CultureInfo("de-DE"))),
                         schluessel + " fehlt in Resource.resx");
            Assert.False(string.IsNullOrEmpty(rm.GetString(schluessel, new CultureInfo("en-US"))),
                         schluessel + " fehlt in Resource.en-US.resx");
        }

        /// <summary>
        /// Die Gründe werden AUS DER RESSOURCE gelesen, nicht aus ihrem Rückfall —
        /// sonst bliebe die Umstellung folgenlos.
        /// </summary>
        [Fact]
        public void Die_Gruende_kommen_aus_der_Ressource()
        {
            using var kultur = new Kulturpinnung("de-DE");
            ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            Assert.Equal(rm.GetString("WIRT_GRUND_KEIN_STROMTRAEGER"),
                         KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER);
            Assert.Equal(rm.GetString("WIRT_STROMTRAEGER_RUECKFALL"),
                         KostenEmissionRechner.HINWEIS_STROMTRAEGER_RUECKFALL);
        }

        /// <summary>Pinnt die Oberflächenkultur und stellt sie zurück (Regel des
        /// Kulturwächters: kein Test pinnt ohne Rückstellung).</summary>
        private sealed class Kulturpinnung : IDisposable
        {
            private readonly CultureInfo _vorherUi;
            private readonly CultureInfo _vorherDefaultUi;

            public Kulturpinnung(string kultur)
            {
                _vorherUi = Thread.CurrentThread.CurrentUICulture;
                _vorherDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;
                CultureInfo neu = new CultureInfo(kultur);
                Thread.CurrentThread.CurrentUICulture = neu;
                CultureInfo.DefaultThreadCurrentUICulture = neu;
            }

            public void Dispose()
            {
                Thread.CurrentThread.CurrentUICulture = _vorherUi;
                CultureInfo.DefaultThreadCurrentUICulture = _vorherDefaultUi;
            }
        }

        // =================================================================
        // 5 — STROMBEDARF OHNE VERWENDUNG (Anwenderentscheid 22.09.2026)
        // =================================================================
        //
        // „Falls es einen Bedarf Strom gibt … und keinen Erzeuger mit Zuordnung
        // Strombedarf, gebe nur eine Warnung aus … und bestimme die Energiekosten ohne
        // Stromkosten.“
        //
        // DIE LAGE DES BEFUNDS: Gaskessel mit Pufferspeicher, eine Kachel Strombedarf,
        // kein Erzeuger, der Strom verwendet. Die Gaskosten waren rechenbar, trotzdem
        // blieb JEDE Kennzahl leer — samt der Aufforderung, „der elektrischen Erzeugung“
        // einen Träger zuzuordnen, die es gar nicht gibt.
        //
        // DIE ZWEI FOLGEENTSCHEIDE DESSELBEN TAGES („1. Kostenentscheid folgen und diesen
        // Strom ebenfalls auslassen 2. auch auf 0 setzen“): Ohne Verwendung fallen auch
        // die EMISSIONEN des Netzbezugs weg, und die Regel greift UNABHÄNGIG vom Preis —
        // mit zugeordnetem Stromträger ebenso wie mit Katalogpreis des
        // Auslieferungsträgers. Grundsatz: „Energiekosten (Strom, Gas, …) sollen nur
        // anfallen, falls sie auch Verwendung finden.“ Die eine Regel steht in
        // ProjektEnergietraegerCtrl.StromOhneVerwendung; das BHKW zählt als Verwendung.

        /// <summary>„Beispiel WP WG 1 - Andere WP“ — Gaskessel UND Wärmepumpe, dem Projekt
        /// ist nur „Erdgas E“ zugeordnet, das gespeicherte Ergebnis führt 16,12 MWh/a
        /// Netzbezug. Nimmt man die Wärmepumpe heraus, steht genau die Lage des
        /// Befunds.</summary>
        private const int PROJEKT_KESSEL = 1027;

        /// <summary><c>energy_carrier.id</c> von „Erdgas E“.</summary>
        private const int GAS = 63;

        /// <summary>Der Netzbezug des gespeicherten Laufs von 1027 [MWh/a].</summary>
        private const double NETZBEZUG = 16.12;

        /// <summary>„Simulation Referenz BHKW-Kaskade“ — Gaskessel, zwei Gas-BHKW, Puffer;
        /// „Elektrische Energie“ zugeordnet zu 0,25 €/kWh (Grundpreis 2.400 €/a).</summary>
        private const int PROJEKT_BHKW = 1030;

        /// <summary>Der Netzbezug (Reststrom nach BHKW) des gespeicherten Laufs von 1030 [MWh/a].</summary>
        private const double NETZBEZUG_BHKW = 4357.78;

        /// <summary>Eine PV-Gerätezeile (<c>Tab_PV</c>) der Testdatenbank.</summary>
        private const int PV_GERAET = 1015244;

        /// <summary>Eine BHKW-Gerätezeile (<c>Tab_BHKW</c>) der Testdatenbank.</summary>
        private const int BHKW_GERAET = 1018148;

        /// <summary>
        /// DER BEFUND SELBST. Ohne Erzeuger, der Strom verwendet, tragen die Energiekosten
        /// den Brennstoff — ohne Stromkosten, ohne Fehlgrund, mit benanntem Hinweis.
        /// </summary>
        [Fact]
        public void Strombedarf_ohne_Verwendung_bepreist_nur_den_Brennstoff()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();

            VariantenDaten v = Rechne(PROJEKT_KESSEL);

            Assert.Null(v.EnergiekostenGrund);
            Assert.True(v.Energiekosten.HasValue);
            // 1 MWh = 1.000 kWh ÷ 10 kWh/Nm³ = 100 Nm³ × 0,50 €
            Assert.Equal(50.0, v.Energiekosten.Value, 4);
            Assert.Null(v.StromkostenNetz);
            Assert.Equal(NETZBEZUG, v.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);
            Assert.DoesNotContain(v.EnergiekostenJeAnlage, z => z.Anlage == NETZBEZUGSZEILE);
        }

        /// <summary>
        /// DER ERSTE FOLGEENTSCHEID („Kostenentscheid folgen und diesen Strom ebenfalls
        /// auslassen“): Ohne Verwendung trägt der Netzbezug auch zur CO₂-Bilanz nichts
        /// bei — dieselbe Zahl wie ohne jeden Netzbezug. Vorher bewertete der Rechner die
        /// 16,12 MWh/a mit dem Vorgabewert 435 g/kWh (+7,01 t/a) und meldete den Rückfall.
        /// </summary>
        [Fact]
        public void Strombedarf_ohne_Verwendung_laesst_auch_die_Emissionen_aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();

            VariantenDaten v = Rechne(PROJEKT_KESSEL);
            VariantenDaten ohneBezug = RechneOhneNetzbezug(PROJEKT_KESSEL);

            Assert.NotNull(v.StrombedarfOhneVerwendungMWh);
            Assert.True(v.CO2Gesamt.HasValue);
            Assert.Equal(ohneBezug.CO2Gesamt.Value, v.CO2Gesamt.Value, 6);
            // Kein Faktor gezogen — also auch keine Herleitung eines Faktors.
            Assert.False(v.CO2StrommixRueckfall);
            Assert.Null(v.CO2TraegerRueckfall);
        }

        /// <summary>
        /// DER ZWEITE FOLGEENTSCHEID („auch auf 0 setzen“): Die Regel greift UNABHÄNGIG
        /// vom Preis. Mit ZUGEORDNETEM Stromträger samt Projektpreis 0,30 €/kWh bleiben
        /// Stromkosten und Stromemissionen außen vor, solange kein Erzeuger Strom
        /// verwendet — vorher 50 + 16,12 MWh × 300 €/MWh = 4.886 €/a, jetzt 50 €/a.
        /// </summary>
        [Fact]
        public void Mit_zugeordnetem_Stromtraeger_bleibt_der_Strom_ohne_Verwendung_aussen_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();
            Stromtraeger_zuordnen(PROJEKT_KESSEL, STROM, 0.30);
            Assert.Equal(STROM, Emissionsquelle.StromTraeger(PROJEKT_KESSEL));

            VariantenDaten v = Rechne(PROJEKT_KESSEL);
            VariantenDaten ohneBezug = RechneOhneNetzbezug(PROJEKT_KESSEL);

            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal(50.0, v.Energiekosten.Value, 4);      // nur das Gas
            Assert.Null(v.StromkostenNetz);
            Assert.Equal(NETZBEZUG, v.StrombedarfOhneVerwendungMWh.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);
            Assert.DoesNotContain(v.EnergiekostenJeAnlage, z => z.Anlage == NETZBEZUGSZEILE);
            Assert.Equal(ohneBezug.CO2Gesamt.Value, v.CO2Gesamt.Value, 6);
            Assert.False(v.CO2StrommixRueckfall);
            Assert.Null(v.CO2TraegerRueckfall);
        }

        /// <summary>
        /// Dasselbe mit einem KATALOGPREIS des Auslieferungsträgers — ohne Zuordnung und
        /// mit Zuordnung ohne Projektpreis (dann gilt der Katalogpreis): Er bepreist den
        /// Strom ohne Verwendung nicht, und ein Rückfallvermerk entsteht nicht.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Ein_Katalogpreis_bepreist_den_Strom_ohne_Verwendung_nicht(bool zugeordnet)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();
            Katalogpreis(STROM, 0.35);
            if (zugeordnet) Stromtraeger_zuordnen(PROJEKT_KESSEL, STROM, 0.0);

            VariantenDaten v = Rechne(PROJEKT_KESSEL);

            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal(50.0, v.Energiekosten.Value, 4);
            Assert.Null(v.StromkostenNetz);
            Assert.Null(v.StromTraegerRueckfall);
            Assert.Equal(NETZBEZUG, v.StrombedarfOhneVerwendungMWh.Value, 2);
        }

        /// <summary>
        /// DIE EINE REGEL (<see cref="ProjektEnergietraegerCtrl.StromOhneVerwendung"/>):
        /// Netzbezug UND kein stromverwendender Erzeuger. Wärmepumpe (1027) und BHKW
        /// (1030) sind Verwendung; ohne Netzbezug oder ohne Projekt gibt es nichts
        /// auszulassen.
        /// </summary>
        [Fact]
        public void Die_eine_Regel_fragt_Netzbezug_und_Verwendung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT_KESSEL, NETZBEZUG));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT_BHKW, NETZBEZUG_BHKW));

            Kesselprojekt();
            Assert.True(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT_KESSEL, NETZBEZUG));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(PROJEKT_KESSEL, 0.0));
            Assert.False(ProjektEnergietraegerCtrl.StromOhneVerwendung(0, NETZBEZUG));
        }

        /// <summary>
        /// DIE GEGENPROBEN: Verwendet ein Erzeuger Strom — Wärmepumpe, Photovoltaik oder
        /// BHKW —, wird derselbe Netzbezug weiter bepreist (hier über den Rückfallträger
        /// mit Katalogpreis: 50 €/a Gas + 16,12 MWh × 350 €/MWh = 5.692 €/a) und bewertet
        /// (Netzbezug × Faktor des Rückfallträgers).
        /// </summary>
        [Theory]
        [InlineData("WP")]
        [InlineData("PV")]
        [InlineData("BHKW")]
        public void Ein_stromverwendender_Erzeuger_haelt_den_Netzbezug_in_der_Rechnung(string erzeuger)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            if (erzeuger == "WP")
            {
                Gaspreis();
                Kesselverbrauch();
            }
            else
            {
                Kesselprojekt();
                ErzeugerHinzu(erzeuger);
            }
            Katalogpreis(STROM, 0.35);
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_KESSEL));

            VariantenDaten v = Rechne(PROJEKT_KESSEL);
            VariantenDaten ohneBezug = RechneOhneNetzbezug(PROJEKT_KESSEL);

            Assert.Null(v.StrombedarfOhneVerwendungMWh);
            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal(50.0 + NETZBEZUG * 1000.0 * 0.35, v.Energiekosten.Value, 2);
            Assert.Equal("Elektrische Energie", v.StromTraegerRueckfall);

            double faktor = Emissionsquelle.Netzstrom(
                PROJEKT_KESSEL, Emissionsquelle.Modus(PROJEKT_KESSEL)).Co2GKwh;
            Assert.True(faktor > 0);
            Assert.Equal(NETZBEZUG * faktor / 1000.0,
                         v.CO2Gesamt.Value - ohneBezug.CO2Gesamt.Value, 6);
        }

        /// <summary>
        /// DAS BHKW VERWENDET STROM. 1030 (Gaskessel, zwei Gas-BHKW, Puffer, „Elektrische
        /// Energie“ zu 0,25 €/kWh) behält die Bepreisung seines Reststroms: 4.357,78 MWh/a ×
        /// 250 €/MWh = 1.089.445 €/a Arbeitspreis gegenüber einem Lauf ohne Netzbezug.
        /// Ohne das BHKW in der Regel fiele dieser Betrag unter „ohne Verwendung“ auf 0.
        /// </summary>
        [Fact]
        public void Ein_BHKW_Projekt_bepreist_seinen_Netzbezug_weiter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_BHKW);
            VariantenDaten ohneBezug = RechneOhneNetzbezug(PROJEKT_BHKW);

            Assert.Null(v.StrombedarfOhneVerwendungMWh);
            Assert.True(v.StromkostenNetz.HasValue);
            Assert.True(ohneBezug.StromkostenNetz.HasValue);
            Assert.Equal(NETZBEZUG_BHKW * 1000.0 * 0.25,
                         v.StromkostenNetz.Value - ohneBezug.StromkostenNetz.Value, 2);
        }

        /// <summary>
        /// Ein BHKW-Projekt OHNE zugeordneten Stromträger bekommt den Rückfallträger wie
        /// ein Wärmepumpenprojekt: Der Netzbezug wird mit dem Katalogpreis bepreist, der
        /// Rückfall benannt — kein „Strombedarf ohne Verwendung“, kein „kein Stromträger“.
        /// </summary>
        [Fact]
        public void Ein_BHKW_Projekt_ohne_Stromtraeger_bekommt_den_Rueckfalltraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "DELETE FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT_BHKW), new DbParam("@c", STROM));
            DataRepository.ExecuteSQL(
                "DELETE FROM energy_price WHERE id_projekt = ? AND carrier_id = ?",
                new DbParam("@p", PROJEKT_BHKW), new DbParam("@c", STROM));
            Assert.Equal(0, Emissionsquelle.StromTraeger(PROJEKT_BHKW));
            Katalogpreis(STROM, 0.35);

            VariantenDaten v = Rechne(PROJEKT_BHKW);

            Assert.Null(v.StrombedarfOhneVerwendungMWh);
            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal("Elektrische Energie", v.StromTraegerRueckfall);
            Assert.Equal(NETZBEZUG_BHKW * 1000.0 * 0.35, v.StromkostenNetz.Value, 2);
        }

        /// <summary>
        /// Die Entlastung nach § 9b StromStG ist eine Gutschrift auf den bezogenen Strom —
        /// ohne Verwendung gibt es nichts zu entlasten. Am selben Projekt: MIT seinen BHKW
        /// steht die Entlastung, OHNE sie entfällt sie, und der Hinweis nennt den Grund.
        /// </summary>
        [Fact]
        public void Ohne_Verwendung_entfaellt_die_Stromsteuer_Entlastung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProduzierendesGewerbe(PROJEKT_BHKW).StromsteuerEntlastungJahr1 > 0);

            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0",
                new DbParam("@p", PROJEKT_BHKW));
            WirtschaftlichkeitErgebnis ohne = ProduzierendesGewerbe(PROJEKT_BHKW);

            Assert.Equal(0.0, ohne.StromsteuerEntlastungJahr1, 6);
            Assert.NotNull(ohne.Hinweis);
            Assert.Contains("Strombedarf ohne Verwendung", ohne.Hinweis);
        }

        /// <summary>
        /// DIE GEGENPROBE. Dasselbe Projekt MIT seiner Wärmepumpe: Jetzt verwendet ein
        /// Erzeuger Strom, der fehlende Träger ist eine echte Lücke — und der bisherige
        /// Fehlgrund bleibt Wort für Wort stehen.
        /// </summary>
        [Fact]
        public void Mit_Waermepumpe_bleibt_der_Fehlgrund_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Gaspreis();
            Kesselverbrauch();

            VariantenDaten v = Rechne(PROJEKT_KESSEL);

            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, v.EnergiekostenGrund);
            Assert.False(v.Energiekosten.HasValue);
            Assert.Null(v.StrombedarfOhneVerwendungMWh);
        }

        /// <summary>
        /// DER HILFSSTROM ZÄHLT ALS VERWENDUNG. Eine Brenneranlage mit gepflegtem
        /// <c>Hilfsenergie_Anteil</c> bezieht Strom und wird mit dem Projekt-Stromträger
        /// bepreist; ohne Träger fiel dieser Anteil bis hierher still aus. Das Projekt
        /// braucht damit einen Stromträger — und der Fehlgrund gehört zurück.
        /// </summary>
        [Fact]
        public void Ein_Hilfsenergie_Anteil_macht_den_Stromtraeger_noetig()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();
            Assert.False(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_KESSEL));

            Hilfsenergie(5.0);
            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT_KESSEL));

            VariantenDaten v = Rechne(PROJEKT_KESSEL);
            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, v.EnergiekostenGrund);
            Assert.Null(v.StrombedarfOhneVerwendungMWh);
        }

        /// <summary>
        /// Der Hinweis reist als WARNUNG (nicht als Fehlgrund) in die Wirtschaftlichkeit —
        /// denselben Weg wie die beiden Rückfallzeilen, und damit bis ins Warnband der
        /// Seite.
        /// </summary>
        [Fact]
        public void Der_Hinweis_erreicht_die_Wirtschaftlichkeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Kesselprojekt();

            var daten = new BerichtsDaten { IdStamm = PROJEKT_KESSEL };
            VariantenDaten stamm = Rechne(PROJEKT_KESSEL);
            stamm.IstStamm = true;
            daten.Varianten.Add(stamm);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT_KESSEL)))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) erg = e;

            Assert.NotNull(erg);
            Assert.Null(erg.Fehlgrund);
            Assert.NotNull(erg.Hinweis);
            Assert.Contains("Strombedarf ohne Verwendung", erg.Hinweis);
            // Der Folgeentscheid: Der Hinweis nennt Kosten UND Emissionen.
            Assert.Contains("Energiekosten und Emissionen sind ohne diesen Strom bestimmt",
                            erg.Hinweis);
        }

        /// <summary>
        /// DIESELBE REGEL IN DER KOHÄRENZPRÜFUNG. Führt das Projekt keinen Erzeuger, der
        /// Strom verwendet, dann FEHLT der Stromträger nicht — er wird nicht gebraucht;
        /// die Zeile „dem Projekt ist kein Strom-Energieträger zugeordnet“ wäre eine
        /// Aufgabe ohne Gegenstand. MIT Wärmepumpe bleibt sie stehen.
        /// </summary>
        [Fact]
        public void Die_Kohaerenzpruefung_schweigt_ohne_Verwendung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string zeile = "kein Strom-Energieträger zugeordnet";

            // MIT Wärmepumpe: Der Befund steht.
            Assert.Contains(KohaerenzPruefung.Pruefe(PROJEKT_KESSEL, StromsteuerLauf()),
                            h => h.Text.Contains(zeile, StringComparison.Ordinal));

            // OHNE: Er fällt weg.
            Kesselprojekt();
            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(PROJEKT_KESSEL, StromsteuerLauf()),
                                  h => h.Text.Contains(zeile, StringComparison.Ordinal));
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>Ein Lauf mit gebuchter § 9b-Entlastung für ein produzierendes
        /// Gewerbe — nur dann prüft die Stromseite überhaupt.</summary>
        private static KohaerenzLauf StromsteuerLauf()
            => new KohaerenzLauf
            {
                Jahr = 2026,
                StromsteuerEntlastungEur = 1000.0,
                Steuer = new SteuerEingabe
                {
                    Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE
                }
            };

        /// <summary>Macht aus 1027 ein reines BRENNSTOFFprojekt: Wärmepumpe heraus,
        /// Gaspreis und Kesselverbrauch hinein.</summary>
        private static void Kesselprojekt()
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP > 0",
                new DbParam("@p", PROJEKT_KESSEL));
            Gaspreis();
            Kesselverbrauch();
        }

        /// <summary>Runde Werte statt der gepflegten: 10 kWh je Nm³, 0,50 € je Nm³.</summary>
        private static void Gaspreis()
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_work = ?, custom_hi = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", 0.5), new DbParam("@h", 10.0),
                new DbParam("@p", PROJEKT_KESSEL), new DbParam("@c", GAS));
        }

        /// <summary>Der gespeicherte Lauf von 1027 führt eine Modulzeile OHNE Verbrauch
        /// (Stand vor Befund B-1). Ein frischer Lauf füllt die Spalte; hier wird sie
        /// gesetzt, damit der Brennstoff überhaupt eine Menge hat.</summary>
        private static void Kesselverbrauch()
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ErgebnisHeizkesselModul SET Verbrauch = ? " +
                "WHERE ID_ErgebnisHeizkessel IN (SELECT h.ID FROM Tab_ErgebnisHeizkessel AS h " +
                "INNER JOIN Tab_Ergebnis AS e ON h.ID_Ergebnis = e.ID WHERE e.ID_Projekt = ?)",
                new DbParam("@v", 1.0), new DbParam("@p", PROJEKT_KESSEL));
        }

        /// <summary>Die Anlagenbezeichnung der Netzbezugszeile in den Energiekosten je Anlage.</summary>
        private static string NETZBEZUGSZEILE
            => WindowsFormsApplication1.MyResource.Resource.WIRT_ENK_NETZBEZUG;

        /// <summary>Fügt dem Projekt 1027 eine Anlagenzeile mit PV- bzw. BHKW-Gerät hinzu.</summary>
        private static void ErzeugerHinzu(string erzeuger)
        {
            if (erzeuger == "PV")
                DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_PV) VALUES (?, ?, ?)",
                    new DbParam("@p", PROJEKT_KESSEL), new DbParam("@b", "PV (Prüfstand)"),
                    new DbParam("@g", PV_GERAET));
            else
                DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_BHKW) VALUES (?, ?, ?)",
                    new DbParam("@p", PROJEKT_KESSEL), new DbParam("@b", "BHKW (Prüfstand)"),
                    new DbParam("@g", BHKW_GERAET));
        }

        /// <summary>Derselbe Lauf mit Netzbezug 0 — die Bilanz ohne jeden Strombezug.</summary>
        private static VariantenDaten RechneOhneNetzbezug(int idProjekt)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
            Assert.NotNull(erg);
            Assert.NotNull(erg.Energiebedarf);
            erg.Energiebedarf.Stromrestbedarf = 0;
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        /// <summary>Rechnet das Projekt als Stamm für ein Unternehmen des produzierenden
        /// Gewerbes — dann rechnet die Stromseite § 9b StromStG.</summary>
        private static WirtschaftlichkeitErgebnis ProduzierendesGewerbe(int idProjekt)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(idProjekt);
            p.Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE;
            ctrl.SpeichereParameter(p);

            VariantenDaten stamm = Rechne(idProjekt);
            stamm.IstStamm = true;
            var daten = new BerichtsDaten { IdStamm = idProjekt };
            daten.Varianten.Add(stamm);

            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in new WirtschaftlichkeitCtrl().Berechne(daten, p))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET && e.IdProjekt == idProjekt)
                    erg = e;
            Assert.NotNull(erg);
            return erg;
        }

        /// <summary>Gibt jeder Anlage des Projekts einen Hilfsenergie-Anteil [%].</summary>
        private static void Hilfsenergie(double anteil)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Hilfsenergie_Anteil = ? WHERE ID_Projekt = ?",
                new DbParam("@a", anteil), new DbParam("@p", PROJEKT_KESSEL));
        }

        private static VariantenDaten Rechne(int idProjekt)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static void Katalogpreis(int carrierId, double preis)
        {
            DataRepository.ExecuteSQL("UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@p", preis), new DbParam("@c", carrierId));
        }

        private static void Stromtraeger_zuordnen(int idProjekt, int carrierId, double projektpreis)
        {
            object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM energy_project_settings");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger], " +
                "custom_hi, custom_price_work, custom_price_base, custom_price_power) " +
                "VALUES (?, ?, ?, 1.0, ?, 0.0, 0.0)",
                new DbParam("@id", id), new DbParam("@p", idProjekt),
                new DbParam("@c", carrierId), new DbParam("@w", projektpreis));
        }

        private static void Historienpreis(int idProjekt, int carrierId, double preis, DateTime ab)
        {
            object max = DataRepository.ExecuteScalar("SELECT MAX(id) FROM energy_price");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_price (id, id_projekt, carrier_id, valid_from, grundpreis, " +
                "arbeitspreis, arbeitspreis_unit, Heizwert, leistungspreis) " +
                "VALUES (?, ?, ?, ?, 0.0, ?, 'kWh', 1.0, 0.0)",
                new DbParam("@id", id), new DbParam("@p", idProjekt), new DbParam("@c", carrierId),
                new DbParam("@d", DbParamTyp.Date) { Wert = ab }, new DbParam("@a", preis));
        }
    }
}
