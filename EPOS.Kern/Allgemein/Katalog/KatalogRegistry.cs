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
                    "h0", "k1", "k2", "Kdir", "Kdfu", "Vorlauf", "Ruecklauf" }
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
