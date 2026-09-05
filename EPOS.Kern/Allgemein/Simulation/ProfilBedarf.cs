using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Quellmodus einer Profilrechnung (Konzept 4.2, „Gemeinsame Profilroutine").
    ///
    /// Bis Paket K1 leiteten alle drei Bedarfszweige den Modus aus <c>list != null</c> ab —
    /// also aus der Frage, ob der Aufrufer eine Namensliste mitgebracht hat. Das ist
    /// zweierlei in einer Angabe und war die Ursache von V0-4 (Kopf aus dem Katalog,
    /// Typprofil aus der Projektkopie). Der Modus ist deshalb jetzt ein EXPLIZITER
    /// Bestandteil der Quellbeschreibung.
    /// </summary>
    public enum ProfilQuellmodus
    {
        /// <summary>
        /// Echte Projektrechnung: Kopf- und Typdaten kommen aus den PROJEKTKOPIEN,
        /// mit Pflichtfilter <c>ID_Projekt</c> (V0-3 — Bezeichner und Typname sind über
        /// Projekte hinweg nicht eindeutig).
        /// </summary>
        Projektrechnung,

        /// <summary>
        /// Katalogvorschau der Admin-/Auswahldialoge: Kopf- und Typdaten kommen aus den
        /// <c>_STAMM</c>-Tabellen. Diese tragen kein <c>ID_Projekt</c>, dort entfällt der
        /// Filter.
        /// </summary>
        Katalogvorschau,

        /// <summary>
        /// <b>Vorschau AUS EINEM PROJEKT</b> — der Knopf „Simulation…" der drei
        /// Bedarfsprofil-Dialoge (iU9-W9.5). Kopf- und Typdaten kommen ZUERST aus dem
        /// <c>_STAMM</c>-Katalog; findet sich der Name dort nicht, gilt die
        /// PROJEKTKOPIE als Rückfall (<see cref="ProfilQuelle.Rueckfall"/>).
        ///
        /// <para><b>Warum es diesen Modus gibt</b> (Befund W9‑B‑4/B‑5 der Windows-Abnahme
        /// vom 05.09.2026). Die Liste dieses Dialogs ist GEMISCHT: Eine gespeicherte
        /// Zuordnung trägt den Namen ihrer PROJEKTKOPIE (<c>Z_Projekt*Ctrl.LiesProjekt</c>
        /// liest <c>Tab_Prozesswaerme.Bezeichner</c> bzw.
        /// <c>Tab_Stromverbraucher.Bezeichner</c>), eine eben erst aufgenommene Zeile
        /// dagegen den Namen ihres KATALOGEINTRAGS — ihre Projektkopie entsteht erst
        /// beim Speichern (<c>WizardCtrl.Add_Projekt_*</c> → <c>CopyFromStamm</c>).
        /// Keine der beiden Quellen allein kennt also alle Namen. Bis zur Behebung
        /// schlug die Vorschau ausschließlich im Katalog nach und lieferte für jede
        /// umbenannte oder nur im Projekt angelegte Kopie zwölf Nullmonate samt leerem
        /// Bild.</para>
        ///
        /// <para><b>Warum der Katalog zuerst kommt.</b> So bleibt jede Zahl, die diese
        /// Vorschau heute zeigt, zeichengleich — der Rückfall greift nur dort, wo bisher
        /// eine Nullreihe stand. Dass eine im Projekt GEÄNDERTE Kopie damit weiterhin
        /// mit der Katalogverteilung angezeigt wird (Brauchwasser 1007: Januar 1,900
        /// statt 0,552 MWh), ist die verbleibende Unstimmigkeit; sie zu drehen ändert
        /// angezeigte Zahlen und braucht einen eigenen Anwenderentscheid (W9‑O‑3c).</para>
        /// </summary>
        Projektvorschau
    }

    /// <summary>
    /// Beschreibung EINER Bedarfsart für <see cref="ProfilBedarf"/>: Tabellen, Spalten,
    /// Filterregel und Protokolltexte.
    ///
    /// Die Tabellen- und Spaltenweichen standen seit V0 dreifach im Code (Brauchwasser,
    /// Prozesswärme, Strom) — hier stehen sie einmal. Instanzen entstehen ausschließlich
    /// über die drei Fabrikmethoden; sie sind die vollständige Liste der Bedarfsarten mit
    /// Monatswert-plus-Wochenprofil-Struktur.
    /// </summary>
    public class ProfilQuelle
    {
        /// <summary>Quellmodus — Projektkopie oder Katalog (<see cref="ProfilQuellmodus"/>).</summary>
        public ProfilQuellmodus Modus = ProfilQuellmodus.Projektrechnung;

        /// <summary>Abfrage/Tabelle, aus der die Namen der Profile eines Projekts stammen.</summary>
        public string NamenAbfrage = "";

        /// <summary>Kopftabelle mit den zwölf Monatswerten und dem Typbezug.</summary>
        public string KopfTabelle = "";

        /// <summary>Typtabelle mit den 168 Wochenwerten.</summary>
        public string TypTabelle = "";

        /// <summary>Schlüsselspalte der Typtabelle: <c>Typname</c> bzw. im Katalog <c>Bezeichner</c>.</summary>
        public string TypSchluesselSpalte = "Typname";

        /// <summary>
        /// true = Kopf- UND Typabfrage filtern zusätzlich auf <c>ID_Projekt</c> (V0-3).
        /// Bewusst KEINE Ableitung aus <see cref="Modus"/>: Der Stromzweig rechnet im
        /// Projektmodus, filtert aber nicht (siehe <see cref="Strom"/>).
        /// </summary>
        public bool ProjektfilterAktiv;

        /// <summary>
        /// Zweite Quelle für EINEN Namen, den <see cref="KopfTabelle"/> nicht kennt;
        /// <c>null</c> = kein Rückfall (so bei <see cref="ProfilQuellmodus.Projektrechnung"/>
        /// und <see cref="ProfilQuellmodus.Katalogvorschau"/>, deren Verhalten damit
        /// unberührt bleibt).
        ///
        /// <para>Gesetzt ist er allein bei <see cref="ProfilQuellmodus.Projektvorschau"/>
        /// und trägt dort die PROJEKTKOPIEN. Wird er gezogen, liefert er Kopf UND
        /// Typprofil — beides aus derselben Quelle, denn genau ihre Vermischung war der
        /// Befund V0-4.</para>
        /// </summary>
        public ProfilQuelle Rueckfall;

        /// <summary>Zuordnungstabelle Projekt ↔ Profil mit der Projekt-Jahressumme.</summary>
        public string ZuordnungTabelle = "";

        /// <summary>Spalte der Projekt-Jahressumme in <see cref="ZuordnungTabelle"/>.</summary>
        public string ZuordnungSummeSpalte = "Summe";

        /// <summary>Protokollpräfix der Bedarfsart („Brauchwasser: " …) für die generischen Meldungen.</summary>
        public string Praefix = "";

        /// <summary>Meldung „kein Kopfdatensatz im Projekt" — Platzhalter {0} = Profilname.</summary>
        public string TextKopfFehlt = "";

        /// <summary>Meldung „kein Wochenprofil" — Platzhalter {0} = Typ, {1} = Profilname.</summary>
        public string TextTypprofilFehlt = "";

        /// <summary>Meldung „Typ nicht definiert" (Abbruch) — Platzhalter {0} = Profilname.</summary>
        public string TextTypUndefiniert = "";

        /// <summary>Brauchwasser (Konzept 4.2, Kanal <see cref="Kanal.BRAUCHWASSER"/>).</summary>
        public static ProfilQuelle Brauchwasser(ProfilQuellmodus modus)
        {
            bool stamm = modus != ProfilQuellmodus.Projektrechnung;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Brauchwasser(ProfilQuellmodus.Projektrechnung) : null,
                Modus = modus,
                NamenAbfrage = "Abfrage_Monatswaerme_Brauchwasser",
                KopfTabelle = stamm ? "Tab_Brauchwasser_STAMM" : "Tab_Brauchwasser",
                TypTabelle = stamm ? "Tab_Brauchwassertyp_STAMM" : "Tab_Brauchwassertyp",
                TypSchluesselSpalte = stamm ? "Bezeichner" : "Typname",
                ProjektfilterAktiv = !stamm,
                ZuordnungTabelle = "Z_Projekt_Brauchwasser",
                ZuordnungSummeSpalte = "Summe",
                Praefix = MyResource.Resource.SIMENG_PRAEFIX_BRAUCHWASSER,
                TextKopfFehlt = MyResource.Resource.SIMENG_BRAUCHWASSER_KOPF_FEHLT,
                TextTypprofilFehlt = MyResource.Resource.SIMENG_BRAUCHWASSER_TYPPROFIL_FEHLT,
                TextTypUndefiniert = MyResource.Resource.SIMENG_BRAUCHWASSER_TYP_UNDEFINIERT
            };
        }

        /// <summary>Prozesswärme (Konzept 4.2, Kanal <see cref="Kanal.PROZESS"/>).</summary>
        public static ProfilQuelle Prozesswaerme(ProfilQuellmodus modus)
        {
            bool stamm = modus != ProfilQuellmodus.Projektrechnung;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Prozesswaerme(ProfilQuellmodus.Projektrechnung) : null,
                Modus = modus,
                NamenAbfrage = "Abfrage_Monatswaerme_Prozesse",
                KopfTabelle = stamm ? "Tab_Prozesswaerme_STAMM" : "Tab_Prozesswaerme",
                TypTabelle = stamm ? "Tab_Prozesstyp_STAMM" : "Tab_Prozesstyp",
                TypSchluesselSpalte = stamm ? "Bezeichner" : "Typname",
                ProjektfilterAktiv = !stamm,
                ZuordnungTabelle = "Z_Projekt_Prozesswaerme",
                ZuordnungSummeSpalte = "Summe",
                Praefix = MyResource.Resource.SIMENG_PRAEFIX_PROZESSWAERME,
                TextKopfFehlt = MyResource.Resource.SIMENG_PROZESSWAERME_KOPF_FEHLT,
                TextTypprofilFehlt = MyResource.Resource.SIMENG_PROZESSWAERME_TYPPROFIL_FEHLT,
                TextTypUndefiniert = MyResource.Resource.SIMENG_PROZESSWAERME_TYP_UNDEFINIERT
            };
        }

        /// <summary>
        /// Stromverbraucherprofile.
        ///
        /// KATALOGQUELLE (Berichtigung K1, Befund 27.08.2026). K1 hielt hier fest, es
        /// gebe „keine <c>_STAMM</c>-Fassung", und ließ die Katalogvorschau deshalb auf
        /// den PROJEKTKOPIEN rechnen. Das ist falsch: <c>Tab_Stromverbraucher_STAMM</c>
        /// und <c>Tab_Stromverbrauchertyp_STAMM</c> gibt es, und genau daraus füllen die
        /// Zuordnungs- und Admin-Dialoge ihre Auswahlliste
        /// (<see cref="StromverbraucherStammCtrl"/>). Die Vorschau suchte den
        /// Katalognamen anschließend in <c>Tab_Stromverbraucher</c>, wo er meist gar
        /// nicht steht — kein Kopfsatz, Anteil 0, und der Ergebnisdialog zeigte zwölf
        /// Nullmonate. Der Modus schaltet die Quelle jetzt wie bei Brauchwasser und
        /// Prozesswärme um.
        ///
        /// EINE ABWEICHUNG BLEIBT: Der Typschlüssel heißt auch im Katalog
        /// <c>Typname</c> — <c>Tab_Stromverbrauchertyp_STAMM</c> führt keine Spalte
        /// <c>Bezeichner</c>, anders als die beiden Wärme-Typkataloge.
        ///
        /// <see cref="ProjektfilterAktiv"/> ist FALSE. Im Katalog ist das zwingend (die
        /// <c>_STAMM</c>-Tabellen tragen kein <c>ID_Projekt</c>). Für die
        /// Projektrechnung bleibt es der offene Punkt K1-O1: V0-3 hat den Pflichtfilter
        /// ausdrücklich nur an Brauchwasser und Prozesswärme nachgezogen; ob der
        /// Stromzweig dieselbe Mehrdeutigkeit hat, gehört als eigener Befund geklärt.
        /// </summary>
        public static ProfilQuelle Strom(ProfilQuellmodus modus)
        {
            bool stamm = modus != ProfilQuellmodus.Projektrechnung;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Strom(ProfilQuellmodus.Projektrechnung) : null,
                Modus = modus,
                NamenAbfrage = "Abfrage_Monatsstrom",
                KopfTabelle = stamm ? "Tab_Stromverbraucher_STAMM" : "Tab_Stromverbraucher",
                TypTabelle = stamm ? "Tab_Stromverbrauchertyp_STAMM" : "Tab_Stromverbrauchertyp",
                TypSchluesselSpalte = "Typname",
                ProjektfilterAktiv = false,
                ZuordnungTabelle = "Z_Projekt_Stromverbraucher",
                ZuordnungSummeSpalte = "Summe",
                Praefix = MyResource.Resource.SIMENG_PRAEFIX_STROMBEDARF,
                TextKopfFehlt = MyResource.Resource.SIMENG_STROMPROFIL_KOPF_FEHLT,
                TextTypprofilFehlt = MyResource.Resource.SIMENG_STROMPROFIL_TYPPROFIL_FEHLT,
                TextTypUndefiniert = MyResource.Resource.SIMENG_STROMPROFIL_TYP_UNDEFINIERT
            };
        }
    }

    /// <summary>
    /// Mitschrift eines Profillaufs. Sie trägt den Namen des GERADE bearbeiteten Profils
    /// über die Methodengrenze hinweg — der Sammel-<c>catch</c> des Aufrufers braucht ihn
    /// für seine Diagnose (Paket-8-Nacharbeit, Befund N6: „zuletzt bearbeitet: Stromprofil
    /// '…'"), kann ihn aber aus einer geworfenen Ausnahme nicht mehr erfragen.
    /// </summary>
    public class ProfilLaufInfo
    {
        /// <summary>Profil, das gerade bearbeitet wird bzw. zuletzt bearbeitet wurde.</summary>
        public string AktuellerName = "";

        /// <summary>Zahl der Profile, die in den Zielvektor eingegangen sind.</summary>
        public int Gerechnet;

        /// <summary>Zahl der Profile, die mit Anteil 0 übersprungen wurden.</summary>
        public int Uebersprungen;
    }

    /// <summary>
    /// DIE gemeinsame Profilroutine „12 Monatswerte × 168-Stunden-Wochenprofil → 8760"
    /// (Konzept 4.2, Paket K1).
    ///
    /// Bis K1 stand derselbe Algorithmus DREIMAL im Code: Prozesswärme und Brauchwasser in
    /// <see cref="SimulationWaermebedarf"/>, Strom in <see cref="SimulationStrombedarf"/>.
    /// Die drei Fassungen sind über die Jahre auseinandergelaufen — V0-3 und V0-4 haben
    /// genau diese Divergenz repariert (fehlender Projektfilter, vertauschte
    /// Katalog-/Projektquelle, stehengebliebenes Wochenprofil). Hier steht sie einmal.
    ///
    /// ABLAUF je Profil:
    ///  1. Kopfsatz lesen (Monat_1…Monat_12 und Typbezug) — im Projektmodus mit
    ///     Pflichtfilter <c>ID_Projekt</c>.
    ///  2. Projekt-Jahressumme aus der Zuordnungstabelle; ist sie gesetzt (&gt; 0), werden
    ///     die zwölf Monatswerte darauf skaliert (<c>pjv / jv</c>).
    ///  3. Wochenprofil des Typs lesen (Spalten „1" … „168") — Puffer vor JEDEM Durchlauf
    ///     genullt (V0-3).
    ///  4. <see cref="WPPlan.Core.BhkwPlan.StromWocheToJahr"/> mit dem Wochentag des
    ///     1. Januar (F3) und Aufaddieren auf den Zielvektor.
    ///
    /// ZAHLENWEG UNVERÄNDERT: dieselben Casts, dieselbe <c>float</c>/<c>double</c>-Führung
    /// und dieselben Kernfunktionen wie im Bestand. Geändert sind allein der KALENDER (F3)
    /// und der DATENZUGANG (<see cref="DataRepository"/> mit <c>?</c>-Parametern statt
    /// <c>RecordSet</c> mit zusammengesetztem SQL — Projektvorgabe).
    /// </summary>
    public static class ProfilBedarf
    {
        /// <summary>Stundenzahl des Simulationsjahres — wie überall im Rechenkern fest.</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>Stunden eines Wochenprofils.</summary>
        public const int WOCHEN_STUNDEN = 168;

        /// <summary>Monate.</summary>
        public const int MONATE = 12;

        /// <summary>
        /// Wochentag des 1. Januar der ALTKONVENTION: Sonntag. Bis Paket K1 kachelte
        /// <see cref="WPPlan.Core.BhkwPlan.StromWocheToJahr"/> hart mit diesem Wert
        /// (Montag = 0 … Sonntag = 6). Er bleibt der Rückfallwert, wenn sich aus den
        /// Klimadaten kein Kalender ableiten lässt.
        /// </summary>
        public const int WOCHENTAG_ALTKONVENTION = 6;

        /// <summary>
        /// <b>Der Quellmodus eines Aufrufs</b> — die eine Regel für alle drei
        /// Bedarfszweige (Befund W9‑B‑4/B‑5 der Windows-Abnahme vom 05.09.2026).
        ///
        /// <para>Bis hierher stand in jedem Zweig <c>list == null ? Projektrechnung :
        /// Katalogvorschau</c>. Das ist die Ableitung, die der Kopf von
        /// <see cref="ProfilQuellmodus"/> seit V0-4 als „zweierlei in einer Angabe"
        /// beschreibt: Ob eine NAMENSLISTE mitkommt, sagt nichts darüber, ob die Namen
        /// aus einem KATALOG oder aus einem PROJEKT stammen.</para>
        ///
        /// <para>Beides zusammen sagt es:</para>
        /// <list type="bullet">
        ///   <item>ohne Liste → der Lauf holt die Namen selbst aus dem Projekt:
        ///     <see cref="ProfilQuellmodus.Projektrechnung"/> (unverändert; hier hängt
        ///     der Referenzlauf).</item>
        ///   <item>mit Liste, ohne Projekt → die Katalogverwaltung zeigt EINEN
        ///     Katalogsatz: <see cref="ProfilQuellmodus.Katalogvorschau"/>
        ///     (unverändert).</item>
        ///   <item>mit Liste UND Projekt → der Bedarfsprofil-Dialog zeigt die
        ///     Zuordnungen eines Projekts:
        ///     <see cref="ProfilQuellmodus.Projektvorschau"/> — Katalog zuerst,
        ///     Projektkopie als Rückfall. Das ist die Behebung.</item>
        /// </list>
        /// </summary>
        public static ProfilQuellmodus Vorschaumodus(List<string> namen, int idProjekt)
        {
            if (namen == null) return ProfilQuellmodus.Projektrechnung;
            return idProjekt != 0 ? ProfilQuellmodus.Projektvorschau
                                  : ProfilQuellmodus.Katalogvorschau;
        }

        // =================================================================================
        // Kalender (Konzept 4.2, Entscheidung F3)
        // =================================================================================

        /// <summary>
        /// Leitet den Wochentag des 1. Januar aus den Wochenend-Kennzeichen der Klimadaten
        /// ab (<c>Tab_Klimadaten.WE</c>, ein Flag je Tag des Jahres).
        ///
        /// VERFAHREN: In den ersten 14 Tagen wird das erste zusammenhängende WE-PAAR
        /// gesucht (Samstag + Sonntag). Aus dessen Tagesindex folgt der Wochentag des
        /// 1. Januar zu <c>(5 − samstagIndex) mod 7</c> — Montag = 0, denn der Samstag ist
        /// der sechste Tag der Woche (Index 5).
        ///
        /// Gesucht wird in ZWEI Durchgängen. Der erste nimmt nur ein ISOLIERTES Paar (der
        /// Tag davor und der Tag danach sind kein WE); damit trägt das Verfahren auch dann,
        /// wenn die Klimadaten einen Feiertag am Freitag oder Montag mit als WE führen und
        /// dadurch ein Dreierblock entsteht. Erst wenn es kein isoliertes Paar gibt, zählt
        /// das erste Paar überhaupt.
        ///
        /// Findet sich gar kein Paar (alle Tage WE, kein Tag WE, Datenlücke), bleibt es bei
        /// <see cref="WOCHENTAG_ALTKONVENTION"/> — mit Protokollhinweis, denn dann rechnet
        /// der Lauf wie vor K1 und das soll nicht unbemerkt bleiben.
        /// </summary>
        public static int WochentagJan1AusWE(bool[] we)
        {
            int grenze = 14;
            if (we != null && we.Length < grenze) grenze = we.Length;

            if (we != null && grenze >= 2)
            {
                // 1. Durchgang: isoliertes Paar (Vortag und Folgetag sind kein Wochenende).
                for (int i = 0; i + 1 < grenze; i++)
                {
                    if (!we[i] || !we[i + 1]) continue;
                    bool davorFrei = i == 0 || !we[i - 1];
                    bool danachFrei = i + 2 >= grenze || !we[i + 2];
                    if (davorFrei && danachFrei) return Normieren(5 - i);
                }

                // 2. Durchgang: erstes Paar ueberhaupt.
                for (int i = 0; i + 1 < grenze; i++)
                    if (we[i] && we[i + 1]) return Normieren(5 - i);
            }

            SimulationProtokoll.Aktuell.HinweisEinmal(
                "KALENDER_WE_UNBESTIMMT",
                MyResource.Resource.SIMENG_KALENDER_WOCHENENDE_UNBESTIMMT);
            return WOCHENTAG_ALTKONVENTION;
        }

        /// <summary>Modulo mit nichtnegativem Ergebnis — C#-<c>%</c> liefert bei negativem Zähler negativ.</summary>
        private static int Normieren(int wochentag)
        {
            return ((wochentag % 7) + 7) % 7;
        }

        /// <summary>
        /// Derselbe Kalender, aber ohne bereits geladene Klimadaten: liest die
        /// Wochenend-Kennzeichen der ersten Tage direkt aus <c>Tab_Klimadaten</c>.
        ///
        /// Gedacht für <see cref="SimulationStrombedarf"/>, das keine Klimaregion kennt und
        /// keine Klimadaten lädt — die Kalendervereinheitlichung (F3) gilt aber für ALLE
        /// drei Bedarfsarten. Ohne Klimaregion (0) bleibt es bei der Altkonvention, ohne
        /// Hinweis: Das ist kein Datenfehler, sondern eine Vorschau ohne Projektbezug.
        /// </summary>
        public static int WochentagJan1AusKlimaregion(int idKlimaregion)
        {
            if (idKlimaregion <= 0) return WOCHENTAG_ALTKONVENTION;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT WE FROM Tab_Klimadaten WHERE ID_Klimaregion=? ORDER BY ID",
                new DbParam("?", idKlimaregion));

            if (dt == null || dt.Rows.Count == 0) return WochentagJan1AusWE(null);

            int anzahl = Math.Min(14, dt.Rows.Count);
            bool[] we = new bool[anzahl];
            for (int i = 0; i < anzahl; i++)
                we[i] = dt.Rows[i]["WE"] != DBNull.Value && Convert.ToBoolean(dt.Rows[i]["WE"]);

            return WochentagJan1AusWE(we);
        }

        // =================================================================================
        // Namensliste
        // =================================================================================

        /// <summary>
        /// Die im Projekt hinterlegten Profilnamen einer Bedarfsart
        /// (<see cref="ProfilQuelle.NamenAbfrage"/>, gefiltert auf <c>ID_Projekt</c>).
        /// </summary>
        public static List<string> NamenLesen(ProfilQuelle quelle, int idProjekt)
        {
            List<string> namen = new List<string>();
            if (quelle == null) return namen;

            // SELECT * wie im Bestand: Die Abfrage_*-Objekte sind gespeicherte
            // Access-Abfragen; ein einzeln benannter Ausgabewert wäre eine zusätzliche
            // Annahme über ihre Spaltenliste, die K1 nicht treffen muss.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + quelle.NamenAbfrage + " WHERE ID_Projekt=?",
                new DbParam("?", idProjekt));

            if (dt == null || !dt.Columns.Contains("Bezeichner")) return namen;
            foreach (DataRow row in dt.Rows)
                if (row["Bezeichner"] != DBNull.Value)
                    namen.Add(row["Bezeichner"].ToString());

            return namen;
        }

        // =================================================================================
        // Die Rechnung
        // =================================================================================

        /// <summary>
        /// Rechnet die genannten Profile und ADDIERT ihr Ergebnis auf <paramref name="ziel"/>.
        /// Der Zielvektor wird NICHT genullt — das ist Sache des Aufrufers, der damit auch
        /// mehrere Bedarfsarten in denselben Kanal legen könnte.
        ///
        /// FEHLERPFADE (V0-Stand, hier für alle drei Bedarfsarten gleich):
        ///  * kein Kopfsatz im Projektmodus → Protokollwarnung, Anteil 0, weiter mit dem
        ///    nächsten Profil (V0-3/V0-4: kein stiller Fremdwert aus einem anderen Projekt)
        ///  * Typbezug leer → Protokollwarnung und ABBRUCH der Bedarfsart (Rückgabe false);
        ///    ohne Typ gibt es keine Verteilung, und ein halb gerechneter Bedarf soll nicht
        ///    wie ein vollständiger aussehen
        ///  * kein Wochenprofil zum Typ → Warnung, Anteil 0, weiter (V0-3: mit dem genullten
        ///    Profil weiterzurechnen lieferte NaN aus der Monatsnormierung)
        ///  * Monatswerte summieren sich zu 0, obwohl eine Projekt-Jahressumme skaliert
        ///    werden soll → Warnung, Anteil 0 (die Skalierung <c>pjv/jv</c> wäre 0/0)
        ///  * Wochenprofil enthält nur Nullen → Warnung, Anteil 0 (die Monatsnormierung
        ///    wäre eine Division durch 0)
        /// In der Katalogvorschau bleiben „kein Kopfsatz" und „kein Wochenprofil" still:
        /// Dort ist die Auswahl des Anwenders die Ursache, nicht die Projektdatenlage.
        /// </summary>
        /// <param name="quelle">Tabellen-, Spalten- und Textbeschreibung der Bedarfsart.</param>
        /// <param name="idProjekt">Projekt; 0 = ohne Projektbezug (keine Jahressummen-Skalierung).</param>
        /// <param name="namen">Zu rechnende Profile; <c>null</c> = die des Projekts (<see cref="NamenLesen"/>).</param>
        /// <param name="wochentagJan1">Wochentag des 1. Januar, Montag = 0 … Sonntag = 6 (F3).</param>
        /// <param name="moAnfang">Stundenindex des Monatsanfangs (12 Werte).</param>
        /// <param name="moEnde">Stundenindex des Monatsendes, inklusive (12 Werte).</param>
        /// <param name="ziel">Zielvektor [8760], wird AUFADDIERT.</param>
        /// <param name="monatssummen">optional [12]: Monatssummen des Zielvektors nach der Rechnung.</param>
        /// <param name="info">optional: Mitschrift für die Diagnose des Aufrufers.</param>
        /// <returns>false, wenn die Bedarfsart abgebrochen wurde (Typbezug leer).</returns>
        public static bool Rechnen(ProfilQuelle quelle, int idProjekt, List<string> namen,
                                   int wochentagJan1, int[] moAnfang, int[] moEnde,
                                   float[] ziel, float[] monatssummen = null,
                                   ProfilLaufInfo info = null)
        {
            if (quelle == null) throw new ArgumentNullException("quelle");
            if (ziel == null) throw new ArgumentNullException("ziel");

            bool projektmodus = quelle.Modus == ProfilQuellmodus.Projektrechnung;
            List<string> liste = namen ?? NamenLesen(quelle, idProjekt);

            // Rechenpuffer je Aufruf statt Klassenfelder (Konzept 4.2): Die alten
            // Zwischenspeicher monats_waerme/wochen_waerme/temp waren instanzweit und
            // wurden von Brauchwasser UND Prozesswärme benutzt - genau daran hing der
            // V0-3-Befund „Profil des vorigen Durchlaufs".
            float[] monatswerte = new float[MONATE];
            float[] wochenwerte = new float[WOCHEN_STUNDEN];
            float[] jahreswerte = new float[STUNDEN_JAHR];

            bool vollstaendig = true;

            for (int k = 0; k < liste.Count; k++)
            {
                string name = liste[k];
                if (info != null) info.AktuellerName = name;

                // Die Quelle DIESES Satzes. Sie weicht nur in der Projektvorschau von
                // der aeusseren ab: Kennt der Katalog den Namen nicht, gilt die
                // Projektkopie (Befund W9-B-4/B-5). Kopf UND Typprofil kommen danach aus
                // derselben Quelle - ihre Vermischung war der Befund V0-4.
                ProfilQuelle satzquelle = quelle;
                DataRow kopf = KopfLesen(quelle, idProjekt, name);
                if (kopf == null && quelle.Rueckfall != null)
                {
                    satzquelle = quelle.Rueckfall;
                    kopf = KopfLesen(satzquelle, idProjekt, name);
                }

                if (kopf == null)
                {
                    // V0-3/V0-4: Im Projektmodus liefert die Projektkopie keinen Satz zu
                    // diesem Namen - Anteil 0 statt eines fremden Wertes.
                    if (projektmodus)
                        SimulationProtokoll.Aktuell.Warnung(string.Format(quelle.TextKopfFehlt, name));
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                string bezeichner = kopf["Bezeichner"] != DBNull.Value
                                    ? kopf["Bezeichner"].ToString() : name;

                // Projekt-Jahressumme: skalieren, wenn der Anwender sie geändert hat.
                float pjv = 0;
                if (idProjekt != 0) pjv = ProjektJahressumme(satzquelle, idProjekt, bezeichner);

                float jv = 0;
                for (int i = 0; i < MONATE; i++)
                {
                    double d = (double)kopf["Monat_" + (i + 1).ToString()];
                    monatswerte[i] = (float)d;
                    jv += monatswerte[i];
                }

                if (pjv > 0)
                {
                    if (jv <= 0)
                    {
                        // Sonst 0 · pjv / 0 = NaN in allen zwölf Monatswerten - und damit
                        // im ganzen Jahresvektor.
                        SimulationProtokoll.Aktuell.Warnung(string.Format(
                            MyResource.Resource.SIMENG_PROFIL_MONATSSUMME_NULL, quelle.Praefix, name));
                        if (info != null) info.Uebersprungen++;
                        continue;
                    }
                    for (int i = 0; i < MONATE; i++)
                        monatswerte[i] = monatswerte[i] * pjv / jv;
                }

                object objTyp = kopf["Typ"];
                if (DBNull.Value.Equals(objTyp) || objTyp == null)
                {
                    // Ohne Typbezug gibt es keine Verteilung. Wie im Bestand bricht die
                    // ganze Bedarfsart ab (Protokollkanal statt MessageBox, Paket 8).
                    SimulationProtokoll.Aktuell.Warnung(string.Format(quelle.TextTypUndefiniert, name));
                    vollstaendig = false;
                    break;
                }

                string typ = objTyp.ToString();

                // V0-3: Wochenprofil vor JEDEM Ladevorgang nullen.
                Array.Clear(wochenwerte, 0, wochenwerte.Length);

                if (!WochenprofilLesen(satzquelle, idProjekt, typ, wochenwerte))
                {
                    if (projektmodus)
                        SimulationProtokoll.Aktuell.Warnung(string.Format(
                            quelle.TextTypprofilFehlt, typ, name));
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                float profilsumme = 0;
                for (int i = 0; i < WOCHEN_STUNDEN; i++) profilsumme += wochenwerte[i];
                if (profilsumme <= 0)
                {
                    // StromWocheToJahr normiert je Monat auf die Profilsumme des Monats;
                    // ein reines Nullprofil ergäbe dort 0/0 = NaN über alle 8760 Stunden.
                    SimulationProtokoll.Aktuell.Warnung(string.Format(
                        MyResource.Resource.SIMENG_PROFIL_WOCHENPROFIL_NULL,
                        quelle.Praefix, typ, name));
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                // Jahresverteilung gemäß Wochenprofil - mit dem Wochentag des 1. Januar (F3).
                WPPlan.Core.BhkwPlan.StromWocheToJahr(wochenwerte, monatswerte, jahreswerte,
                                                      moAnfang, moEnde, wochentagJan1);
                WPPlan.Core.BhkwPlan.VectorenAddieren(jahreswerte, ziel);
                if (info != null) info.Gerechnet++;
            }

            if (monatssummen != null)
                WPPlan.Core.BhkwPlan.MonatsSumme(ziel, monatssummen, moAnfang, moEnde);

            return vollstaendig;
        }

        /// <summary>Kopfsatz eines Profils; <c>null</c> = kein Treffer.</summary>
        private static DataRow KopfLesen(ProfilQuelle quelle, int idProjekt, string name)
        {
            string sql = "SELECT * FROM " + quelle.KopfTabelle + " WHERE Bezeichner=?";
            DataTable dt;

            if (quelle.ProjektfilterAktiv)
                dt = DataRepository.GetDataTable(sql + " AND ID_Projekt=?",
                                                 new DbParam("?", name),
                                                 new DbParam("?", idProjekt));
            else
                dt = DataRepository.GetDataTable(sql, new DbParam("?", name));

            return (dt != null && dt.Rows.Count > 0) ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Vom Anwender im Projekt hinterlegte Jahressumme des Profils; 0 = keine.
        /// Ersetzt die drei <c>Z_Projekt*Ctrl</c>-Lesungen des Bestands durch EINE
        /// parametrisierte Abfrage - gelesen wird wie dort die erste Trefferzeile.
        /// </summary>
        private static float ProjektJahressumme(ProfilQuelle quelle, int idProjekt, string bezeichner)
        {
            object wert = DataRepository.ExecuteScalar(
                "SELECT " + quelle.ZuordnungSummeSpalte + " FROM " + quelle.ZuordnungTabelle +
                " WHERE ID_Projekt=? AND Bezeichner=?",
                new DbParam("?", idProjekt),
                new DbParam("?", bezeichner));

            if (wert == null || wert == DBNull.Value) return 0;
            return (float)Convert.ToDouble(wert);
        }

        /// <summary>
        /// Liest die 168 Wochenwerte des Typs in <paramref name="wochenwerte"/>.
        /// Rückgabe false = kein Typsatz gefunden (der Puffer bleibt unberührt).
        /// </summary>
        private static bool WochenprofilLesen(ProfilQuelle quelle, int idProjekt, string typ,
                                              float[] wochenwerte)
        {
            string sql = "SELECT * FROM " + quelle.TypTabelle +
                         " WHERE " + quelle.TypSchluesselSpalte + "=?";
            DataTable dt;

            if (quelle.ProjektfilterAktiv)
                dt = DataRepository.GetDataTable(sql + " AND ID_Projekt=?",
                                                 new DbParam("?", typ),
                                                 new DbParam("?", idProjekt));
            else
                dt = DataRepository.GetDataTable(sql, new DbParam("?", typ));

            if (dt == null || dt.Rows.Count == 0) return false;

            DataRow row = dt.Rows[0];
            for (int i = 0; i < WOCHEN_STUNDEN; i++)
            {
                double dw = (double)row[(i + 1).ToString()];
                wochenwerte[i] = (float)dw;
            }
            return true;
        }
    }
}
