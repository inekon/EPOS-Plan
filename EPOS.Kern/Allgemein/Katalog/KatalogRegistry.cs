using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Katalog-Registry der Dublettenpruefung (Konzept_Dublettenpruefung_Import_EPOS-Plan.md,
    // Abschnitt 6.1). EINE Beschreibung je Katalogtabelle des Admin-Menues: Tabelle,
    // Schluessel-/Namensspalte, was NICHT zum Inhaltsvergleich gehoert, und die
    // Datenbloecke (Kennlinien, Ganglinienwerte), die am Kopfsatz haengen.
    //
    // Die Registry ist reine Beschreibung (keine DB-Zugriffe). Verwendet von
    // DublettenPruefung (Scan + Import-Vorpruefung), der Admin-Dublettensuche und der
    // Schema-Migration (Bereinigungs-Ausweitung). Spaltenlisten erhoben am 21.08.2026
    // gegen die Produktivdatenbank; die Werte sind Persistenz-Bezeichner und bleiben
    // deutsch und eingefroren (Drei-Schichten-Regel).
    // ====================================================================================

    /// <summary>
    /// Ein Datenblock: Detailtabelle, deren Zeilen ueber <see cref="FkSpalte"/> am
    /// Kopfsatz haengen und deren Werte zum Inhaltsvergleich des Kopfsatzes gehoeren
    /// (WP-Kennlinien, Ganglinien-/Verteilungswerte, Klimadaten).
    /// </summary>
    public class KatalogDatenblock
    {
        public string Tabelle;
        public string FkSpalte;
        /// <summary>ORDER BY-Ausdruck fuer eine stabile Reihenfolge beim Hashen.</summary>
        public string Sortierung;
        /// <summary>Die inhaltstragenden Spalten (alles andere wird ignoriert).</summary>
        public string[] WertSpalten;
    }

    /// <summary>
    /// Eine Pruefabfrage der Verwendungspruefung vor dem Loeschen (Konzept 5.3):
    /// Zaehlt, ob der Katalogsatz aus <see cref="Tabelle"/>.<see cref="Spalte"/>
    /// heraus referenziert wird - ueber den Bezeichner (<see cref="UeberName"/> = true)
    /// oder ueber die Katalog-ID (false). Befuellt wird sie NUR fuer nachgewiesene
    /// Referenzen; Kataloge mit Kopiersemantik (STAMM wird ins Projekt kopiert,
    /// Verweise zeigen auf die Projektkopie) fuehren bewusst ein leeres Array.
    /// </summary>
    public class VerwendungsPruefung
    {
        public string Tabelle;
        public string Spalte;
        /// <summary>true: Vergleich gegen den Bezeichner; false: gegen die Katalog-ID.</summary>
        public bool UeberName;
    }

    /// <summary>Beschreibung eines Katalogs fuer die Dublettenpruefung.</summary>
    public class KatalogDefinition
    {
        /// <summary>Sprachneutraler Schluessel (ASCII), z.B. "WP" - nie Anzeigetext.</summary>
        public string Schluessel;
        public string Tabelle;
        public string IdSpalte = "ID";
        public string NamensSpalte = "Bezeichner";
        /// <summary>
        /// Spalten, die ZUSAETZLICH zu IdSpalte, NamensSpalte, "ReadOnly" und
        /// "Beschreibung" nicht in den Inhaltsvergleich eingehen - vom Anwender
        /// gepflegte Kosten-/Verwaltungsfelder (Konzept 3.2).
        /// </summary>
        public string[] AusschlussSpalten = new string[0];
        public KatalogDatenblock[] Datenbloecke = new KatalogDatenblock[0];
        /// <summary>
        /// Spalten, die der Dateiimport dieses Katalogs befuellt (Basis fuer den
        /// Inhaltsvergleich beim Import und fuer das feldgenaue Ueberschreiben, D2).
        /// null = Katalog hat keinen Dateiimport.
        /// </summary>
        public string[] ImportSpalten;

        /// <summary>
        /// Nachgewiesene Verwendungsstellen des Katalogsatzes fuer die Pruefung vor dem
        /// Loeschen (Konzept 5.3, Erhebung 21.08.2026). Leeres Array = Kopiersemantik
        /// belegt, das Loeschen im Katalog beruehrt keine Projektdaten.
        /// </summary>
        public VerwendungsPruefung[] VerwendungsPruefungen = new VerwendungsPruefung[0];

        /// <summary>
        /// <b>Eine benutzte Zeile ist unveraenderlich</b> (Umsetzungskonzept
        /// Zapfprofilgenerator 3.2): true, wenn eine Verwendung nach
        /// <see cref="VerwendungsPruefungen"/> das Loeschen und Umbenennen SPERRT, statt
        /// nur nachzufragen — ebenso eine Zeile der Auslieferung (<c>ReadOnly</c>).
        /// <see cref="KatalogBereinigung.SatzLoeschen"/>,
        /// <see cref="KatalogBereinigung.SatzUmbenennen"/> und
        /// <see cref="KatalogBereinigung.GruppeBereinigen"/> halten die Sperre selbst, der
        /// Dublettendialog meldet sie. false (Vorgabe) = die Verwendung ist eine Rueckfrage.
        /// </summary>
        public bool VerwendungSperrt;

        /// <summary>
        /// Steht der Katalog im Dublettendialog der Verwaltung? false fuer Kataloge, deren
        /// Oberflaeche noch nicht gebaut ist (Anzeigename, Texte) — sie bleiben fuer
        /// Scan, Bereinigung und Verwendungspruefung im Kern erreichbar.
        /// </summary>
        public bool ImDublettendialog = true;

        /// <summary>
        /// Spalten, die mit dem Namen den NATUERLICHEN SCHLUESSEL bilden — etwa
        /// <c>Katalogversion</c> bei den Tww-Katalogen, deren Zeilen denselben Bezeichner in
        /// mehreren Versionen tragen. Die Namensgruppen des Scans bilden sich ueber Name UND
        /// diese Spalten; zwei Versionen eines Namens sind darum keine Namensdublette.
        /// Leer (Vorgabe) = der Name allein ist der Schluessel.
        /// </summary>
        public string[] SchluesselZusatzSpalten = new string[0];

        /// <summary>
        /// <b>Eine Spalte, die das Schloss UMGEKEHRT fuehrt</b> (1 = aenderbar) — beim
        /// Gebaeudetyp <c>Veraenderbar</c>: Dort ist ein Satz gesperrt, wenn er
        /// <c>ReadOnly</c> traegt ODER nicht <c>Veraenderbar</c> ist
        /// (<c>TagVCtrl.Katalogfilterzeilen</c>). <see cref="Auslieferungskennzeichen"/>
        /// schaltet sie deshalb mit. Leer (Vorgabe) = das Schloss ist allein <c>ReadOnly</c>.
        /// </summary>
        public string SchlossGegenspalte = "";

        /// <summary>
        /// <b>Das Schloss folgt einem Freigabestatus</b> und laesst sich nicht von Hand
        /// umschalten — die Tww-Kataloge: Ihr <c>ReadOnly</c> haengt an <c>Status</c>
        /// (Auslieferung, eigener Satz, Freigabe mit Vier-Augen-Vermerk). Ein Umschalten
        /// ginge an dieser Freigabe vorbei; <see cref="Auslieferungskennzeichen"/> lehnt
        /// es benannt ab (Entscheid AD-Q15).
        /// </summary>
        public bool SchlossAusStatus;
    }

    public static class KatalogRegistry
    {
        // ------------------------------------------------------------------------------
        // Verwendungserhebung zu den VerwendungsPruefungen (Konzept 5.3, offener Punkt
        // 9.3), erhoben am 21.08.2026 gegen den Code-Bestand:
        //
        // KOPIERSEMANTIK - Projekte KOPIEREN Katalogsaetze, alle persistierten Verweise
        // zeigen auf die Projektkopie, nie auf die _STAMM-Tabelle. Loeschen im Katalog
        // beruehrt darum keine Projektdaten; diese Kataloge fuehren bewusst ein LEERES
        // VerwendungsPruefungen-Array:
        //  - Erzeuger (WP, Heizkessel, BHKW, Pufferspeicher, Solarkollektoren, PV,
        //    Stromspeicher): WPCtrl/HeizkesselCtrl/BHKWCtrl/PufferSpCtrl/
        //    SolarkollektorenCtrl/PhotovoltaikCtrl/StromspeicherCtrl.CopyFromStamm -
        //    "Beziehungen verweisen auf die Projekt-Tabelle, nicht auf STAMM".
        //  - Gebaeude: GebaeudeStammCtrl.CopyFromStamm -> Tab_Gebaeude; Z_ProjektGebaeude
        //    fuehrt kein ID_Gebaeude mehr (WizardCtrl.Add_Projekt_ZuordungGebäude).
        //  - Klimaregion: KlimaregionStammCtrl.ApplyRegionToProjekt kopiert Region samt
        //    Klimadaten/Solar; Tab_Projekt.ID_Klimaregion zeigt auf die Projektkopie.
        //  - Profile/Ganglinien (Brauchwasser, Stromverbraucher, Prozesswaerme, Strom-/
        //    Solarganglinie, Waermebedarf): *StammCtrl.CopyFromStamm bzw.
        //    ApplyGanglinieToProjekt (Aufrufe in WizardCtrl.Add_*); die Z_Projekt*-Zeilen
        //    verweisen auf die Projektkopie, die Simulation liest ausschliesslich die
        //    Projekttabellen (SimulationStrombedarf, SimulationWaermebedarf).
        //
        // ECHTE REFERENZEN - nur die vier Typprofil-Kataloge werden dauerhaft
        // referenziert, und zwar KATALOGINTERN: die Kopfsaetze verweisen ueber die
        // Textspalte "Typ" auf den Namen des Typprofils. Diese Kataloge tragen
        // entsprechende Pruefabfragen (UeberName = true), Fundstellen an der Definition.
        // ------------------------------------------------------------------------------

        private static readonly KatalogDefinition[] _alle = new[]
        {
            new KatalogDefinition
            {
                Schluessel = "WP",
                Tabelle = WPStammCtrl.TABLE,                       // Tab_WP_STAMM
                AusschlussSpalten = new[] { "Modulkosten" },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = WPStammCtrl.CURVE,               // Tab_Kenndaten_STAMM
                        FkSpalte = "ID_WP",
                        Sortierung = "Vorlauf, Temperatur",
                        WertSpalten = new[] { "Vorlauf", "Temperatur", "COP", "Ptherm" }
                    },
                    new KatalogDatenblock
                    {
                        Tabelle = WPStammCtrl.CURVE_K,             // Tab_Kenndaten_Kuehlung_STAMM
                        FkSpalte = "ID_WP",
                        Sortierung = "Vorlauf, Temperatur",
                        WertSpalten = new[] { "Vorlauf", "Temperatur", "COP", "Pkuehl", "Last" }
                    }
                },
                ImportSpalten = new[] { "Firma", "Typ", "Baujahr", "Aufstellung", "Nennleistung",
                    "maxPtherm", "Heizung", "Regelung", "Bauart", "Kuehlleistung" }
            },
            new KatalogDefinition
            {
                Schluessel = "HEIZKESSEL",
                Tabelle = SchemaKatalog.TAB_HEIZKESSEL_STAMM,
                AusschlussSpalten = new[] { "Investitionskosten", "Wartungskosten",
                    "Wartungskosten_Einheit", "Nutzungsdauer" },
                ImportSpalten = new[] { "Firma", "Ptherm", "Brennstoff", "Wirkungsgrad_Gas",
                    "Wirkungsgrad_Öl", "Raumbedarf", "CO2", "SO2", "NOx", "CO", "Staub",
                    "Betriebsbereitschaftverlust" }
            },
            new KatalogDefinition
            {
                Schluessel = "PUFFERSPEICHER",
                Tabelle = "Tab_Pufferspeicher_STAMM",
                AusschlussSpalten = new[] { "Investitionskosten" },
                ImportSpalten = new[] { "Hersteller", "Speichertyp", "Bereitschaftsverluste",
                    "Gesamtvolumen" }
            },
            new KatalogDefinition
            {
                Schluessel = "SOLARKOLLEKTOREN",
                Tabelle = "Tab_Solarkollektoren_STAMM",
                AusschlussSpalten = new[] { "Investitionskosten" },
                ImportSpalten = new[] { "Firma", "Kollektortyp", "Modulflaeche", "Aperturflaeche",
                    "h0", "k1", "k2", "Kdir", "Kdfu" }
            },
            new KatalogDefinition
            {
                Schluessel = "PV",
                Tabelle = SchemaKatalog.TAB_PV_STAMM,
                AusschlussSpalten = new[] { "Modulkosten" },
                // "Technologie" (Migrationsschritt 63, Stufe E2.3) gehoert in die
                // Import-Schnittmenge: Beide Importe liefern sie (CEC "Technology",
                // PAN "Technol"), und das erweiterte Rechenmodell waehlt daran den
                // Huld-Koeffizientensatz. Ohne sie meldete die Dublettenpruefung zwei
                // Katalogsaetze als inhaltsgleich, die sich rechnerisch unterscheiden.
                ImportSpalten = new[] { "Firma", "Leistung", "Wirkungsgrad", "U_Mpp",
                    "U_Leerlauf", "I_Mpp", "I_Kurzschluss", "alpha_SC", "beta_OC",
                    "gamma_PMP", "T_NOCT", "Laenge", "Breite", "Technologie" }
            },
            new KatalogDefinition
            {
                Schluessel = "WECHSELRICHTER",
                Tabelle = SchemaKatalog.TAB_WECHSELRICHTER_STAMM,
                // "Kosten" ist ein ANWENDERFELD - wie "Modulkosten" bei der
                // Photovoltaik und aus demselben Grund ausgeschlossen: Der Import
                // liefert keinen Preis, und zwei Saetze, die sich nur im Preis
                // unterscheiden, sind derselbe Wechselrichter (Konzept 5.4).
                AusschlussSpalten = new[] { "Kosten" },
                // Die Sandia-Spalten stehen bewusst NICHT hier: Zwei Katalogsaetze,
                // die sich nur in C3 unterscheiden, rechnen in EPOS-Plan identisch
                // (Konzept 3.3.2) - sie als verschieden zu melden waere falscher
                // Alarm. Dieselbe Abwaegung hat der PV-Eintrag mit "Technologie" in
                // die ANDERE Richtung getroffen: Dort waehlt die Spalte den
                // Koeffizientensatz und gehoert deshalb hinein.
                ImportSpalten = new[] { "Firma", "P_AC_Nenn", "S_AC_Max", "P_DC_Max",
                    "U_Mpp_Min", "U_Mpp_Max", "U_Dc_Max", "U_Start", "I_Dc_Max",
                    "Anzahl_Mppt", "Straenge_Je_Mppt",
                    "Eta05", "Eta10", "Eta20", "Eta30", "Eta50", "Eta100",
                    "Eta_Euro", "Eta_Max", "P_Standby", "P_Nacht", "Herkunft" }
                // VerwendungsPruefungen: LEER - Kopiersemantik. Projekte verweisen auf
                // die Projektkopie Tab_Wechselrichter (WechselrichterCtrl.CopyFromStamm),
                // nie auf den Katalog.
            },
            new KatalogDefinition
            {
                Schluessel = "BHKW",
                Tabelle = "Tab_BHKW_STAMM",
                AusschlussSpalten = new[] { "Investition_kwel", "Wartungskosten_kwhel",
                    "Nutzungsdauer", "Kosten_Modul", "Kosten_Montage", "Kosten_Lieferung",
                    "Kosten_Schallschutzhaube", "Kosten_Abgasreinigung" }
            },
            new KatalogDefinition
            {
                Schluessel = "STROMSPEICHER",
                Tabelle = SchemaKatalog.TAB_STROMSPEICHER_STAMM,
                AusschlussSpalten = new[] { "Modulkosten", "Verschleisskosten",
                    "Leistungskosten", "Investition_Fix" }
            },
            new KatalogDefinition
            {
                Schluessel = "GEBAEUDE",
                Tabelle = "Tab_Gebaeude_STAMM"
            },
            // ------------------------------------------------------------------------
            // Gebaeudesimulation, Stufe G3 (Softwarearchitektur 2.6, W21): der
            // Baustoffkatalog (Schritt S-A) und der Aufbaukatalog samt Schichten (S-B).
            // Der Registerschluessel spiegelt den Tabellennamen - BAUTEILAUFBAU, nicht
            // AUFBAU. KOPIERSEMANTIK wie bei den Erzeugern: Projekte verweisen auf die
            // Projektkopien (BaustoffCtrl/BauteilaufbauCtrl.CopyFromStamm), nie auf den
            // Katalog.
            //
            // BAUSTOFF traegt EINE Verwendungspruefung - katalogintern ueber die ID: Eine
            // Katalogschicht zeigt mit Tab_Bauteilschicht_STAMM.ID_Baustoff auf den Stoff,
            // und der Fremdschluessel ist restriktiv (W11). Ein benutzter Stoff laesst sich
            // deshalb nicht loeschen; die Pruefung sagt es vorher.
            //
            // Die Schichten sind der Datenblock des Aufbaus (Muster Waermepumpe): Sie zaehlen
            // zum Inhalt, sortiert nach Reihenfolge, und gehen mit ihm (Kaskade). Die
            // Stoffwerte der Schicht sind eine Kopie und damit Inhalt; ID_Baustoff ebenso.
            //
            // ImDublettendialog = false: Anzeigename und Texte stehen (ADM_KATALOG_*). Gepflegt
            // werden beide Kataloge in ihren eigenen Verwaltungen (EPOS.UI/Dialoge/Bedarf/
            // BaustoffKatalogDialog.razor und BauteilaufbauDialog.razor); der Dublettendialog
            // fuehrt sie nicht. Fuer Scan, Bereinigung und Verwendungspruefung bleiben sie im
            // Kern erreichbar.
            // ------------------------------------------------------------------------
            new KatalogDefinition
            {
                Schluessel = "BAUSTOFF",
                Tabelle = SchemaKatalog.TAB_BAUSTOFF_STAMM,
                ImDublettendialog = false,
                // Der Hersteller gehoert zum natuerlichen Schluessel: Derselbe Name bei zwei
                // Herstellern ist keine Namensdublette.
                SchluesselZusatzSpalten = new[] { BaustoffSchema.SPALTE_HERSTELLER },
                VerwendungsPruefungen = new[]
                {
                    new VerwendungsPruefung
                    {
                        Tabelle = SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM,
                        Spalte = BauteilaufbauSchema.SPALTE_ID_BAUSTOFF,
                        UeberName = false
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "BAUTEILAUFBAU",
                Tabelle = SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM,
                ImDublettendialog = false,
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM,
                        FkSpalte = BauteilaufbauSchema.SPALTE_ID_AUFBAU,
                        Sortierung = "Reihenfolge, ID",
                        WertSpalten = new[] { "Reihenfolge", "ID_Baustoff", "Dicke", "IstLuftschicht",
                                              "Lambda", "Rho", "cp" }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "KLIMAREGION",
                Tabelle = "Tab_Klimaregion_STAMM",
                IdSpalte = "ID_Klimaregion",
                NamensSpalte = "Name",
                AusschlussSpalten = new[] { "Details" },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_Klimadaten_STAMM",
                        FkSpalte = "ID_Klimaregion",
                        Sortierung = "ID_Klimadaten",
                        WertSpalten = new[] { "Sol_Nord", "Sol_Ost", "Sol_Sued", "Sol_West",
                            "Temperatur", "WE", "TagTyp_W", "TagTyp_NW", "Globalstrahlung",
                            "Direktstrahlung", "Diffusstrahlung", "Sonnenwinkel" }
                    },
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_Solar_STAMM",
                        FkSpalte = "ID_Klimaregion",
                        Sortierung = "ID",
                        WertSpalten = new[] { "Temperatur", "Sol_Nord", "Sol_Ost", "Sol_Sued",
                            "Sol_West", "Globalstrahlung", "Direktstrahlung", "Diffusstrahlung",
                            "Sonnenwinkel" }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "BRAUCHWASSER",
                Tabelle = "Tab_Brauchwasser_STAMM"
            },
            new KatalogDefinition
            {
                Schluessel = "BRAUCHWASSERTYP",
                Tabelle = "Tab_Brauchwassertyp_STAMM",
                VerwendungsPruefungen = new[]
                {
                    // Brauchwasser-Koepfe verweisen per Typ = Bezeichner auf ihr Typprofil
                    // (BrauchwasserStammCtrl.CopyFromStamm liest TYP_STAMM WHERE Bezeichner = Kopf.Typ).
                    new VerwendungsPruefung { Tabelle = "Tab_Brauchwasser_STAMM", Spalte = "Typ", UeberName = true }
                }
            },
            // ------------------------------------------------------------------------
            // Die drei Kataloge des Zapfprofilgenerators (Umsetzungskonzept
            // Zapfprofilgenerator 3.2, Stufe Z0, Posten P8). Anders als die Kataloge
            // oben gilt hier KEINE Kopiersemantik: Zone, Nutzungsart und Projekt
            // verweisen ueber die ID unmittelbar auf die Katalogzeile, eine benutzte
            // Zeile ist unveraenderlich (TwwNutzungsartCtrl). Die Verwendungspruefungen
            // laufen deshalb ueber die ID (UeberName = false).
            //
            // Der natuerliche Schluessel ist (Bezeichner, Katalogversion), nicht der
            // Bezeichner allein: Zwei Versionen einer Nutzungsart tragen denselben
            // Namen. Die Namensgruppen bilden sich deshalb ueber beide Spalten
            // (SchluesselZusatzSpalten) - zwei Versionen sind keine Namensdublette, und
            // die Leerkopien-Regel der KatalogBereinigung trifft sie nie.
            // "Katalogversion" bleibt ausserdem Vergleichsspalte des Inhalts.
            // Ausgeschlossen sind nur die Verwaltungsfelder: Vorlage, Status, interner
            // Beleg und Vier-Augen-Vermerk.
            //
            // VerwendungSperrt: Eine benutzte oder ausgelieferte Zeile ist gesperrt -
            // Loeschen, Umbenennen und Bereinigen lehnen ab (Konzept 3.2).
            // ImDublettendialog = false: Anzeigenamen und Texte kommen erst mit der
            // Oberflaechenstufe (Konzept 5.4); bis dahin zeigt die Verwaltung die
            // Kataloge nicht.
            //
            // Die Zapfkategorien (Schemaschritt T2, Stufe Z3) sind ein Datenblock der
            // Nutzungsart: Sie gehoeren zu ihrer Katalogversion, gehen mit ihr (Kaskade)
            // und zaehlen zum Inhalt. Eine Kategorie mit ReadOnly sperrt ihre Nutzungsart
            // wie deren eigenes ReadOnly (KatalogBereinigung.Sperrgrund). Einer Datenbank
            // ohne die Tabelle (Stand vor 115) fehlt der Block nur.
            // ------------------------------------------------------------------------
            new KatalogDefinition
            {
                Schluessel = "TWW_NUTZUNGSART",
                Tabelle = TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
                VerwendungSperrt = true,
                SchlossAusStatus = true,
                ImDublettendialog = false,
                SchluesselZusatzSpalten = new[] { "Katalogversion" },
                AusschlussSpalten = new[] { "ID_Vorlage", "Status", "Beleg", "Freigabe" },
                VerwendungsPruefungen = new[]
                {
                    // Tab_TwwZone.ID_Nutzungsart - Fremdschluessel ohne ON DELETE (Konzept 3.1).
                    new VerwendungsPruefung { Tabelle = TwwSchema.TAB_TWW_ZONE, Spalte = "ID_Nutzungsart", UeberName = false }
                },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
                        FkSpalte = "ID_Nutzungsart",
                        Sortierung = "Reihenfolge, ID",
                        WertSpalten = new[]
                        {
                            "Kategorie", "Reihenfolge", "Volumenstrom_l_min", "Dauer_min", "Anteil", "Sigma", "Kappung_l_min"
                        }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "TWW_TAGESGANGSATZ",
                Tabelle = TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
                VerwendungSperrt = true,
                SchlossAusStatus = true,
                ImDublettendialog = false,
                SchluesselZusatzSpalten = new[] { "Katalogversion" },
                AusschlussSpalten = new[] { "Status", "Beleg" },
                VerwendungsPruefungen = new[]
                {
                    // Vorgabesatz einer Nutzungsart und Expertenwahl einer Zone (Konzept 3.1).
                    new VerwendungsPruefung { Tabelle = TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, Spalte = "ID_Tagesgangsatz", UeberName = false },
                    new VerwendungsPruefung { Tabelle = TwwSchema.TAB_TWW_ZONE, Spalte = "ID_Tagesgangsatz", UeberName = false }
                },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                        FkSpalte = "ID_Tagesgangsatz",
                        Sortierung = "Tagtyp",
                        WertSpalten = TagesgangWertspalten()
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "TWW_BEDARFSTAG",
                Tabelle = TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
                VerwendungSperrt = true,
                SchlossAusStatus = true,
                ImDublettendialog = false,
                SchluesselZusatzSpalten = new[] { "Katalogversion" },
                AusschlussSpalten = new[] { "Status", "Beleg" },
                VerwendungsPruefungen = new[]
                {
                    // Gewaehlter Bedarfstag der Auslegung (Konzept 3.1, ON DELETE SET NULL) -
                    // eine benutzte Zeile ist trotzdem unveraenderlich (Konzept 3.2).
                    new VerwendungsPruefung { Tabelle = TwwSchema.TAB_TWW_PROJEKT, Spalte = "ID_Bedarfstag", UeberName = false }
                },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
                        FkSpalte = "ID_Bedarfstag",
                        Sortierung = "Reihenfolge, ID",
                        WertSpalten = new[] { "Minute_Beginn", "Dauer_min", "Energie_Kwh", "Reihenfolge" }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "STROMVERBRAUCHER",
                Tabelle = "Tab_Stromverbraucher_STAMM"
            },
            new KatalogDefinition
            {
                Schluessel = "STROMVERBRAUCHERTYP",
                Tabelle = "Tab_Stromverbrauchertyp_STAMM",
                NamensSpalte = "Typname",
                VerwendungsPruefungen = new[]
                {
                    // Stromverbraucher-Koepfe verweisen per Typ = Typname auf ihr Typprofil
                    // (StromverbraucherStammCtrl: "Kopf verweist per Typ = Typname").
                    new VerwendungsPruefung { Tabelle = "Tab_Stromverbraucher_STAMM", Spalte = "Typ", UeberName = true }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "PROZESSWAERME",
                Tabelle = "Tab_Prozesswaerme_STAMM"
            },
            new KatalogDefinition
            {
                Schluessel = "PROZESSTYP",
                Tabelle = "Tab_Prozesstyp_STAMM",
                VerwendungsPruefungen = new[]
                {
                    // Prozesswaerme-Koepfe verweisen per Typ = Bezeichner auf ihr Typprofil
                    // (ProzesswaermeStammCtrl.CopyFromStamm liest TYP_STAMM WHERE Bezeichner = Kopf.Typ).
                    new VerwendungsPruefung { Tabelle = "Tab_Prozesswaerme_STAMM", Spalte = "Typ", UeberName = true }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "STROMGANGLINIE",
                Tabelle = "Tab_Stromganglinie_STAMM",
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_StromganglinieDaten_STAMM",
                        FkSpalte = "ID_Ganglinie",
                        Sortierung = "ID",
                        WertSpalten = new[] { "Wert" }
                    }
                },
                ImportSpalten = new[] { "Zeitinterval" }
            },
            new KatalogDefinition
            {
                Schluessel = "SOLARGANGLINIE",
                Tabelle = "Tab_Solarganglinie_STAMM",
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_SolarganglinieDaten_STAMM",
                        FkSpalte = "ID_Ganglinie",
                        Sortierung = "ID",
                        WertSpalten = new[] { "Wert" }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "WAERMEBEDARF",
                Tabelle = "Tab_Waermebedarf_STAMM",
                // LEERES Array und nicht null (iU9-W13.0g): null heisst "dieser
                // Katalog hat keinen Dateiimport" (:66-70) - und genau daran lag es,
                // dass die Waermebedarfsverwaltung als einzige Importmaske des
                // Bestands ohne Dublettenpruefung auskam (Befund W13-B2). Der
                // Kopfsatz traegt ausser dem Bezeichner nichts, was sich
                // vergleichen liesse; die 8 760 Werte stehen im Datenblock. Ein
                // leeres Array sagt deshalb genau das Richtige: pruefe den NAMEN,
                // vergleiche keinen Inhalt (Dublettenkonzept 4.4).
                ImportSpalten = new string[0],
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_WaermebedarfDaten_STAMM",
                        FkSpalte = "ID_Ganglinie",
                        Sortierung = "ID",
                        WertSpalten = new[] { "Wert" }
                    }
                }
            },
            new KatalogDefinition
            {
                Schluessel = "GEBAEUDETYP",
                Tabelle = "Tab_DBTagV_STAMM",
                AusschlussSpalten = new[] { "Veraenderbar" },
                // Das Schloss des Gebaeudetyps ist ReadOnly ODER nicht Veraenderbar
                // (TagVCtrl.Katalogfilterzeilen) - das Umschalten schaltet beide.
                SchlossGegenspalte = "Veraenderbar",
                VerwendungsPruefungen = new[]
                {
                    // Gebaeude-Katalogsaetze verweisen per Typ = Bezeichner auf ihren Tagesverlauf
                    // (GebaeudeStammCtrl.CopyTagVForGebaeude: Katalog-Bezeichner == Gebaeudetyp).
                    new VerwendungsPruefung { Tabelle = "Tab_Gebaeude_STAMM", Spalte = "Typ", UeberName = true }
                },
                Datenbloecke = new[]
                {
                    new KatalogDatenblock
                    {
                        Tabelle = "Tab_DBTagVDaten_STAMM",
                        FkSpalte = "ID_TagV",
                        Sortierung = "ID",
                        WertSpalten = new[] { "Verteilung" }
                    }
                }
            },
        };

        /// <summary>Alle Kataloge des Admin-Menues (Entscheidung 9.5 des Konzepts).</summary>
        public static IReadOnlyList<KatalogDefinition> Alle => _alle;

        /// <summary>
        /// Die Kataloge, die der Dublettendialog der Verwaltung zeigt und scannt — alle
        /// mit <see cref="KatalogDefinition.ImDublettendialog"/>, in Registryreihenfolge.
        /// </summary>
        public static IReadOnlyList<KatalogDefinition> Dublettendialog
            => Array.FindAll(_alle, k => k.ImDublettendialog);

        /// <summary>
        /// Die Wertspalten eines Tagesgangs: der Tagtyp und die 24 Stundenanteile
        /// <c>Anteil_01</c> … <c>Anteil_24</c> — aus einer Schleife, wie
        /// <c>ZapfprofilCtrl.AnteilSpalte</c> sie bildet, nie aus einer Eingabe.
        /// </summary>
        private static string[] TagesgangWertspalten()
        {
            var spalten = new string[25];
            spalten[0] = "Tagtyp";
            for (int h = 1; h <= 24; h++)
                spalten[h] = "Anteil_" + h.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            return spalten;
        }

        /// <summary>
        /// Der lokalisierte Anzeigename eines Katalogs; ein unbekannter Schluessel
        /// zeigt sich selbst (iU9-W14c.0h).
        ///
        /// <para><b>Warum das hier steht</b> (Befund W14c-B40): Die neunzehn Kataloge
        /// standen an ZWEI Orten - als <see cref="KatalogDefinition"/> hier und als
        /// neunzehn <c>case</c> in <c>Form_KatalogDubletten.KatalogAnzeige</c>. Ein
        /// neuer Katalog brauchte beide Stellen. Jetzt steht die Liste einmal da.</para>
        /// </summary>
        public static string Anzeige(string schluessel)
        {
            switch (schluessel)
            {
                case "WP": return MyResource.Resource.ADM_KATALOG_WP;
                case "HEIZKESSEL": return MyResource.Resource.ADM_KATALOG_HEIZKESSEL;
                case "PUFFERSPEICHER": return MyResource.Resource.ADM_KATALOG_PUFFERSPEICHER;
                case "SOLARKOLLEKTOREN": return MyResource.Resource.ADM_KATALOG_SOLARKOLLEKTOREN;
                case "PV": return MyResource.Resource.ADM_KATALOG_PV;
                case "WECHSELRICHTER": return MyResource.Resource.ADM_KATALOG_WECHSELRICHTER;
                case "BHKW": return MyResource.Resource.ADM_KATALOG_BHKW;
                case "STROMSPEICHER": return MyResource.Resource.ADM_KATALOG_STROMSPEICHER;
                case "GEBAEUDE": return MyResource.Resource.ADM_KATALOG_GEBAEUDE;
                case "BAUSTOFF": return MyResource.Resource.ADM_KATALOG_BAUSTOFF;
                case "BAUTEILAUFBAU": return MyResource.Resource.ADM_KATALOG_BAUTEILAUFBAU;
                case "KLIMAREGION": return MyResource.Resource.ADM_KATALOG_KLIMAREGION;
                case "BRAUCHWASSER": return MyResource.Resource.ADM_KATALOG_BRAUCHWASSER;
                case "BRAUCHWASSERTYP": return MyResource.Resource.ADM_KATALOG_BRAUCHWASSERTYP;
                case "STROMVERBRAUCHER": return MyResource.Resource.ADM_KATALOG_STROMVERBRAUCHER;
                case "STROMVERBRAUCHERTYP": return MyResource.Resource.ADM_KATALOG_STROMVERBRAUCHERTYP;
                case "PROZESSWAERME": return MyResource.Resource.ADM_KATALOG_PROZESSWAERME;
                case "PROZESSTYP": return MyResource.Resource.ADM_KATALOG_PROZESSTYP;
                case "STROMGANGLINIE": return MyResource.Resource.ADM_KATALOG_STROMGANGLINIE;
                case "SOLARGANGLINIE": return MyResource.Resource.ADM_KATALOG_SOLARGANGLINIE;
                case "WAERMEBEDARF": return MyResource.Resource.ADM_KATALOG_WAERMEBEDARF;
                case "GEBAEUDETYP": return MyResource.Resource.ADM_KATALOG_GEBAEUDETYP;
                default: return schluessel ?? "";
            }
        }

        /// <summary>Definition zu einem sprachneutralen Schluessel, sonst null.</summary>
        public static KatalogDefinition Finde(string schluessel)
        {
            foreach (KatalogDefinition k in _alle)
                if (string.Equals(k.Schluessel, schluessel, StringComparison.Ordinal))
                    return k;
            return null;
        }

        /// <summary>Definition zu einer Tabelle (fuer Migration/Aufrufer mit Tabellennamen).</summary>
        public static KatalogDefinition FindeTabelle(string tabelle)
        {
            foreach (KatalogDefinition k in _alle)
                if (string.Equals(k.Tabelle, tabelle, StringComparison.OrdinalIgnoreCase))
                    return k;
            return null;
        }
    }
}
