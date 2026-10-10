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
        /// Bedarfsprofil-Dialoge (iU9-W9.5). Kopf- und Typdaten kommen ZUERST aus der
        /// PROJEKTKOPIE; kennt das Projekt den Namen nicht, gilt der
        /// <c>_STAMM</c>-Katalog als Rückfall (<see cref="ProfilQuelle.Rueckfall"/>).
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
        /// <para><b>Warum die KOPIE zuerst kommt</b> (Anwenderentscheid <b>W9‑O‑3c</b>
        /// vom 05.09.2026, „Empfehlung"). Eine Vorschau, die etwas anderes zeigt als der
        /// Lauf, ist keine: Der Projektlauf rechnet mit der Projektkopie, also zeigt sie
        /// auch die Vorschau. Die erste Fassung (Behebung W9‑B‑4/B‑5) las den Katalog
        /// zuerst, damit jede damals richtige Zahl zeichengleich blieb; eine im Projekt
        /// GEÄNDERTE Kopie erschien dadurch mit der Katalogverteilung — Brauchwasser
        /// 1007: Januar 1,900 statt 0,552 MWh bei gleicher Jahressumme. Genau das ist
        /// mit dem Entscheid gedreht. Der Katalog bleibt als Rückfall für die noch nicht
        /// gespeicherte Zeile, die ihren Katalognamen trägt.</para>
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

        /// <summary>Kopftabelle mit den zwölf Monatswerten und dem Typbezug.</summary>
        public string KopfTabelle = "";

        /// <summary>Typtabelle mit den 168 Wochenwerten.</summary>
        public string TypTabelle = "";

        /// <summary>Schlüsselspalte der Typtabelle: <c>Typname</c> bzw. im Katalog <c>Bezeichner</c>.</summary>
        public string TypSchluesselSpalte = "Typname";

        /// <summary>
        /// true = Kopf- UND Typabfrage filtern zusätzlich auf <c>ID_Projekt</c> (V0-3).
        /// Gesetzt auf den Projektkopien aller drei Bedarfsarten (beim Stromverbraucher
        /// seit SV1, siehe <see cref="Strom"/>), nie auf den <c>_STAMM</c>-Tabellen.
        /// </summary>
        public bool ProjektfilterAktiv;

        /// <summary>
        /// Zweite Quelle für EINEN Namen, den <see cref="KopfTabelle"/> nicht kennt;
        /// <c>null</c> = kein Rückfall (so bei <see cref="ProfilQuellmodus.Projektrechnung"/>
        /// und <see cref="ProfilQuellmodus.Katalogvorschau"/>, deren Verhalten damit
        /// unberührt bleibt).
        ///
        /// <para>Gesetzt ist er allein bei <see cref="ProfilQuellmodus.Projektvorschau"/>
        /// und trägt dort den KATALOG (W9‑O‑3c: die Projektkopie ist die erste Quelle).
        /// Wird er gezogen, liefert er Kopf UND Typprofil — beides aus derselben Quelle,
        /// denn genau ihre Vermischung war der Befund V0-4.</para>
        /// </summary>
        public ProfilQuelle Rueckfall;

        /// <summary>Zuordnungstabelle Projekt ↔ Profil mit der Projekt-Jahressumme.</summary>
        public string ZuordnungTabelle = "";

        /// <summary>Spalte der Projekt-Jahressumme in <see cref="ZuordnungTabelle"/>.</summary>
        public string ZuordnungSummeSpalte = "Summe";

        /// <summary>
        /// Spalte in <see cref="ZuordnungTabelle"/>, die per ID auf den Kopfsatz der
        /// PROJEKTKOPIE zeigt — gesetzt bei allen drei Bedarfsarten (<c>ID_Brauchwasser</c>,
        /// <c>ID_Prozesswaerme</c>, <c>ID_Stromverbraucher</c>). Die Zuordnung wird allein
        /// über sie aufgelöst, nie über den Bezeichner der Zuordnungszeile.
        ///
        /// <para><b>Warum die ID</b> (Aufträge SV1 vom 30.09.2026 und SV2 vom 02.10.2026). Der
        /// Bezeichner der Zuordnungszeile ist der Name, unter dem die Zeile einmal angelegt
        /// wurde — meist der KATALOGNAME —, die Projektkopie heißt dagegen vielfach
        /// „… (P‹Projekt›)" oder ist umbenannt. Über den Namen gesucht, griff die gepflegte
        /// Jahressumme nicht (Stromverbraucher 1017 und 1047: „EFH_3_Pers" mit Summe 15 gegen
        /// die Kopie „EFH_3_Pers (P1017)", gerechnet wurde das volle Profil; beim Brauchwasser
        /// ebenso), und bei der Prozesswärme fehlte dann der Kopfsatz ganz — ihre Namenssicht
        /// <c>Abfrage_Monatswaerme_Prozesse</c> liefert den Bezeichner der Zuordnungszeile,
        /// nicht den der Kopie. Der Lauf rechnet deshalb JE ZUORDNUNGSZEILE: Kopfsatz über
        /// diese ID, Jahressumme aus derselben Zeile; die Vorschau sucht die Jahressumme über
        /// die ID ihres Kopfsatzes. Die Namenssichten <c>Abfrage_Monats*</c> liest der
        /// Rechenkern nicht mehr.</para>
        /// </summary>
        public string ZuordnungIdSpalte;

        /// <summary>
        /// Spalte der Typtabelle, die per ID auf ihren Kopfsatz zeigt; <c>null</c> = das
        /// Wochenprofil wird allein über den Typnamen gesucht.
        ///
        /// <para>Gesetzt bei allen drei Bedarfsarten auf den Projektkopien
        /// (<c>Tab_Brauchwassertyp.ID_Brauchwasser</c>, <c>Tab_Prozesstyp.ID_Prozesswaerme</c>,
        /// <c>Tab_Stromverbrauchertyp.ID_Stromverbraucher</c> — die ursprüngliche Beziehung;
        /// <c>ID_Projekt</c> steht erst seit FK-1 daneben), im Katalog nie. Das Wochenprofil
        /// kommt dann aus der Typzeile, die zu GENAU DIESEM Kopfsatz kopiert wurde; nur wo es
        /// sie nicht gibt, gilt die Typzeile gleichen Namens aus demselben Projekt.</para>
        /// </summary>
        public string TypKopfIdSpalte;

        /// <summary>
        /// Jahressummen je Profilname [MWh], die VOR der gespeicherten Zuordnung gelten;
        /// <c>null</c> = allein die Zuordnungstabelle. Die Vorschau des Bedarfsprofildialogs
        /// reicht hier den Stand des offenen Dialogs herein — den Jahresverbrauch, den der
        /// Anwender mit „Übernehmen" gesetzt, aber noch nicht gespeichert hat. Ohne ihn
        /// rechnete die Vorschau mit der gespeicherten Summe oder, bei einer neu
        /// aufgenommenen Zeile, mit der Katalogsumme. Ein Wert ≤ 0 skaliert nicht.
        /// </summary>
        public IReadOnlyDictionary<string, double> Jahressummen;

        /// <summary>Protokollpräfix der Bedarfsart („Brauchwasser: " …) für die generischen Meldungen.</summary>
        public string Praefix = "";

        /// <summary>Meldung „kein Kopfdatensatz im Projekt" — Platzhalter {0} = Profilname.</summary>
        public string TextKopfFehlt = "";

        /// <summary>Meldung „kein Wochenprofil" — Platzhalter {0} = Typ, {1} = Profilname.</summary>
        public string TextTypprofilFehlt = "";

        /// <summary>Meldung „Typ nicht definiert" (Abbruch) — Platzhalter {0} = Profilname.</summary>
        public string TextTypUndefiniert = "";

        /// <summary>
        /// Brauchwasser (Konzept 4.2, Kanal <see cref="Kanal.BRAUCHWASSER"/>).
        ///
        /// <para><b>Die Zuordnung wird über die ID aufgelöst</b> (Auftrag SV2,
        /// <see cref="ZuordnungIdSpalte"/>, <see cref="TypKopfIdSpalte"/>) wie beim
        /// Stromverbraucher: Kopfsatz, Jahressumme und Wochenprofil folgen
        /// <c>Z_Projekt_Brauchwasser.ID_Brauchwasser</c>, nicht dem Bezeichner der
        /// Zuordnungszeile. Weicht er vom Namen der Kopie ab, griff die gepflegte
        /// Jahressumme bis dahin nicht.</para>
        /// </summary>
        public static ProfilQuelle Brauchwasser(ProfilQuellmodus modus)
        {
            // W9-O-3c: STAMM-Tabellen liest allein die Katalogvorschau. Die
            // Projektvorschau rechnet auf den PROJEKTKOPIEN wie der Lauf und haelt den
            // Katalog nur als Rueckfall fuer die noch nicht gespeicherte Zeile bereit.
            bool stamm = modus == ProfilQuellmodus.Katalogvorschau;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Brauchwasser(ProfilQuellmodus.Katalogvorschau) : null,
                Modus = modus,
                KopfTabelle = stamm ? "Tab_Brauchwasser_STAMM" : "Tab_Brauchwasser",
                TypTabelle = stamm ? "Tab_Brauchwassertyp_STAMM" : "Tab_Brauchwassertyp",
                TypSchluesselSpalte = stamm ? "Bezeichner" : "Typname",
                ProjektfilterAktiv = !stamm,
                ZuordnungTabelle = "Z_Projekt_Brauchwasser",
                ZuordnungSummeSpalte = "Summe",
                ZuordnungIdSpalte = "ID_Brauchwasser",
                TypKopfIdSpalte = stamm ? null : "ID_Brauchwasser",
                Praefix = MyResource.Resource.SIMENG_PRAEFIX_BRAUCHWASSER,
                TextKopfFehlt = MyResource.Resource.SIMENG_BRAUCHWASSER_KOPF_FEHLT,
                TextTypprofilFehlt = MyResource.Resource.SIMENG_BRAUCHWASSER_TYPPROFIL_FEHLT,
                TextTypUndefiniert = MyResource.Resource.SIMENG_BRAUCHWASSER_TYP_UNDEFINIERT
            };
        }

        /// <summary>
        /// Prozesswärme (Konzept 4.2, Kanal <see cref="Kanal.PROZESS"/>).
        ///
        /// <para><b>Die Zuordnung wird über die ID aufgelöst</b> (Auftrag SV2,
        /// <see cref="ZuordnungIdSpalte"/>, <see cref="TypKopfIdSpalte"/>) wie beim
        /// Stromverbraucher: Kopfsatz, Jahressumme und Wochenprofil folgen
        /// <c>Z_Projekt_Prozesswaerme.ID_Prozesswaerme</c>. Über den Namen gelesen, hing der
        /// Lauf am Bezeichner der Zuordnungszeile — trug die Projektkopie einen anderen
        /// Namen, fehlte ihr Kopfsatz, und das Profil entfiel mit einer Warnung.</para>
        /// </summary>
        public static ProfilQuelle Prozesswaerme(ProfilQuellmodus modus)
        {
            // W9-O-3c: STAMM-Tabellen liest allein die Katalogvorschau. Die
            // Projektvorschau rechnet auf den PROJEKTKOPIEN wie der Lauf und haelt den
            // Katalog nur als Rueckfall fuer die noch nicht gespeicherte Zeile bereit.
            bool stamm = modus == ProfilQuellmodus.Katalogvorschau;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Prozesswaerme(ProfilQuellmodus.Katalogvorschau) : null,
                Modus = modus,
                KopfTabelle = stamm ? "Tab_Prozesswaerme_STAMM" : "Tab_Prozesswaerme",
                TypTabelle = stamm ? "Tab_Prozesstyp_STAMM" : "Tab_Prozesstyp",
                TypSchluesselSpalte = stamm ? "Bezeichner" : "Typname",
                ProjektfilterAktiv = !stamm,
                ZuordnungTabelle = "Z_Projekt_Prozesswaerme",
                ZuordnungSummeSpalte = "Summe",
                ZuordnungIdSpalte = "ID_Prozesswaerme",
                TypKopfIdSpalte = stamm ? null : "ID_Prozesswaerme",
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
        /// <para><b>Der Projektfilter gilt</b> (Auftrag SV1 vom 30.09.2026, schließt K1-O1).
        /// Im Katalog entfällt er zwingend (die <c>_STAMM</c>-Tabellen tragen kein
        /// <c>ID_Projekt</c>). Auf den Projektkopien fehlte er bis dahin: Kopf- und Typsatz
        /// wurden allein über den Namen gelesen, und bei gleichnamigen Kopien zweier
        /// Projekte konnte die des FREMDEN Projekts gelten (in der Testdatenbank rechneten
        /// 1024 und 1040 bis 1045 mit der Kopie von 1023, 1046 mit der von 1007, 1047 mit
        /// der von 1017 — dort zeichengleich, deshalb ohne Wirkung auf die Zahlen). Die
        /// Kopie wird jetzt stets im Kontext des Projekts gelesen: Projektkopie des eigenen
        /// Projekts, in der Projektvorschau sonst der Katalogsatz, nie die Kopie eines
        /// fremden Projekts.</para>
        ///
        /// <para><b>Die Zuordnung wird über die ID aufgelöst</b>
        /// (<see cref="ZuordnungIdSpalte"/>, <see cref="TypKopfIdSpalte"/>), nicht über den
        /// Bezeichner der Zuordnungszeile.</para>
        /// </summary>
        public static ProfilQuelle Strom(ProfilQuellmodus modus)
        {
            // W9-O-3c: STAMM-Tabellen liest allein die Katalogvorschau. Die
            // Projektvorschau rechnet auf den PROJEKTKOPIEN wie der Lauf und haelt den
            // Katalog nur als Rueckfall fuer die noch nicht gespeicherte Zeile bereit.
            bool stamm = modus == ProfilQuellmodus.Katalogvorschau;
            return new ProfilQuelle
            {
                Rueckfall = modus == ProfilQuellmodus.Projektvorschau
                            ? Strom(ProfilQuellmodus.Katalogvorschau) : null,
                Modus = modus,
                KopfTabelle = stamm ? "Tab_Stromverbraucher_STAMM" : "Tab_Stromverbraucher",
                TypTabelle = stamm ? "Tab_Stromverbrauchertyp_STAMM" : "Tab_Stromverbrauchertyp",
                TypSchluesselSpalte = "Typname",
                ProjektfilterAktiv = !stamm,
                ZuordnungTabelle = "Z_Projekt_Stromverbraucher",
                ZuordnungSummeSpalte = "Summe",
                ZuordnungIdSpalte = "ID_Stromverbraucher",
                TypKopfIdSpalte = stamm ? null : "ID_Stromverbraucher",
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

        /// <summary>
        /// Wird je GERECHNETEM Profil gerufen — mit seinem Kopfsatz und seiner Jahresreihe [kWh],
        /// bevor sie auf den Zielvektor addiert wird (PW1 Stufe 1: das Temperaturniveau des
        /// Prozesskanals, <see cref="Prozesstemperatur"/>). Die Reihe gehört der Routine und wird
        /// beim nächsten Profil überschrieben; der Empfänger liest sie nur. <c>null</c> = niemand
        /// fragt, der Zahlenweg ist derselbe.
        /// </summary>
        public Action<DataRow, double[]> JeProfil;
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
    ///     Pflichtfilter <c>ID_Projekt</c>; im Lauf über die ID aus der Zuordnungszeile
    ///     (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>, alle drei Bedarfsarten).
    ///  2. Projekt-Jahressumme aus der Zuordnungstabelle — im Lauf aus derselben Zeile, in
    ///     der Vorschau aus der Zeile, die per ID auf den Kopfsatz zeigt; ist sie gesetzt
    ///     (&gt; 0), werden die zwölf Monatswerte darauf skaliert (<c>pjv / jv</c>).
    ///  3. Wochenprofil des Typs lesen (Spalten „1" … „168") — Puffer vor JEDEM Durchlauf
    ///     genullt (V0-3).
    ///  4. <see cref="WPPlan.Core.BhkwPlan.StromWocheToJahr"/> mit dem Wochentag des
    ///     1. Januar (F3) und Aufaddieren auf den Zielvektor.
    ///
    /// ZAHLENWEG UNVERÄNDERT: dieselben Casts, dieselbe <c>double</c>/<c>double</c>-Führung
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
        ///     <see cref="ProfilQuellmodus.Projektvorschau"/> — Projektkopie zuerst,
        ///     Katalog als Rückfall (W9‑O‑3c). Das ist die Behebung.</item>
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
        // Die zu rechnenden Einträge
        // =================================================================================

        /// <summary>
        /// Ein zu rechnender Eintrag: aus einer Namensliste nur der Name, aus einer
        /// Zuordnungszeile (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>) dazu die ID des
        /// Kopfsatzes und die Jahressumme DIESER Zeile.
        /// </summary>
        private sealed class Profileintrag
        {
            /// <summary>Profilname; bei einer Zuordnungszeile deren Bezeichner (nur für Meldungen).</summary>
            public string Name = "";

            /// <summary>ID des Kopfsatzes laut Zuordnungszeile; 0 = über den Namen suchen.</summary>
            public int KopfId;

            /// <summary>Jahressumme der Zuordnungszeile [MWh]; <c>null</c> = nachschlagen.</summary>
            public double? Summe;

            /// <summary>
            /// Betriebskalender der Zuordnungszeile (PW2/BW2); 0 = keiner, <c>null</c> = über die ID
            /// des Kopfsatzes nachschlagen (Vorschau).
            /// </summary>
            public int? KalenderId;
        }

        /// <summary>
        /// Die Zuordnungszeilen eines Projekts, je Zeile ein Eintrag mit der ID ihres
        /// Kopfsatzes und ihrer Jahressumme — der Projektlauf aller drei Bedarfsarten
        /// (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>).
        ///
        /// <para>Je ZEILE, nicht je Name: Zwei Zeilen auf dieselbe Kopie rechnen zweimal,
        /// jede mit ihrer Summe. Die Reihenfolge ist die der Zuordnungs-ID.</para>
        /// </summary>
        private static List<Profileintrag> ZuordnungenLesen(ProfilQuelle quelle, int idProjekt)
        {
            var eintraege = new List<Profileintrag>();

            // PW2/BW2: die Kalenderspalte nur, wenn der Schemaschritt gelaufen ist.
            bool mitKalender = BedarfNetzKalenderSchema.KalenderspaltenVorhanden();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + quelle.ZuordnungIdSpalte + " AS KopfId, " + quelle.ZuordnungSummeSpalte +
                " AS Summe, Bezeichner" +
                (mitKalender ? ", " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " AS KalenderId" : "") +
                " FROM " + quelle.ZuordnungTabelle +
                " WHERE ID_Projekt=? ORDER BY ID",
                new DbParam("?", idProjekt));

            if (dt == null) return eintraege;
            foreach (DataRow row in dt.Rows)
            {
                eintraege.Add(new Profileintrag
                {
                    Name = row["Bezeichner"] != DBNull.Value ? row["Bezeichner"].ToString() : "",
                    KopfId = row["KopfId"] != DBNull.Value ? Convert.ToInt32(row["KopfId"]) : 0,
                    // NULL heißt wie im Bestand „keine Summe" (0 skaliert nicht).
                    Summe = row["Summe"] != DBNull.Value ? Convert.ToDouble(row["Summe"]) : 0.0,
                    KalenderId = mitKalender && row["KalenderId"] != DBNull.Value ? Convert.ToInt32(row["KalenderId"]) : 0
                });
            }
            return eintraege;
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
        ///  * Typbezug leer → Protokollwarnung, Anteil 0, weiter mit dem nächsten Profil
        ///    (Rückgabe false); die übrigen Profile rechnen vollständig, und die Summe hängt
        ///    nicht an der Reihenfolge der Profile (Befund PW6)
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
        /// <param name="namen">Zu rechnende Profile (Vorschau); <c>null</c> = die Zuordnungszeilen des
        /// Projekts, je Zeile über die ID (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>).</param>
        /// <param name="wochentagJan1">Wochentag des 1. Januar im Raster der Klimaregion, Montag = 0 … Sonntag = 6
        /// (F3). Trägt das Projekt eine Preisreihe mit Jahr, gilt stattdessen das Raster dieses Jahres
        /// (<see cref="Konditionierungdatenweg.Raster(int, int)"/>, E115).</param>
        /// <param name="moAnfang">Stundenindex des Monatsanfangs (12 Werte).</param>
        /// <param name="moEnde">Stundenindex des Monatsendes, inklusive (12 Werte).</param>
        /// <param name="ziel">Zielvektor [8760], wird AUFADDIERT.</param>
        /// <param name="monatssummen">optional [12]: Monatssummen des Zielvektors nach der Rechnung.</param>
        /// <param name="info">optional: Mitschrift für die Diagnose des Aufrufers.</param>
        /// <returns>false, wenn mindestens ein Profil ohne Typbezug übersprungen wurde.</returns>
        public static bool Rechnen(ProfilQuelle quelle, int idProjekt, List<string> namen,
                                   int wochentagJan1, int[] moAnfang, int[] moEnde,
                                   double[] ziel, double[] monatssummen = null,
                                   ProfilLaufInfo info = null)
        {
            if (quelle == null) throw new ArgumentNullException("quelle");
            if (ziel == null) throw new ArgumentNullException("ziel");

            bool projektmodus = quelle.Modus == ProfilQuellmodus.Projektrechnung;

            // SV1/SV2: Ohne Namensliste rechnet der Lauf JE ZUORDNUNGSZEILE (Kopfsatz über
            // die ID, Summe aus derselben Zeile) - bei allen drei Bedarfsarten. Die Vorschau
            // bringt ihre Namen mit. Ohne Namen und ohne Projektrechnung gibt es nichts zu
            // rechnen: Der Katalog kennt keine Zuordnung (Vorschaumodus liefert ohne Liste
            // stets die Projektrechnung).
            List<Profileintrag> liste;
            if (namen != null)
                liste = Namenseintraege(namen);
            else if (projektmodus)
                liste = ZuordnungenLesen(quelle, idProjekt);
            else
                liste = new List<Profileintrag>();

            // Rechenpuffer je Aufruf statt Klassenfelder (Konzept 4.2): Die alten
            // Zwischenspeicher monats_waerme/wochen_waerme/temp waren instanzweit und
            // wurden von Brauchwasser UND Prozesswärme benutzt - genau daran hing der
            // V0-3-Befund „Profil des vorigen Durchlaufs".
            double[] monatswerte = new double[MONATE];
            double[] wochenwerte = new double[WOCHEN_STUNDEN];
            double[] jahreswerte = new double[STUNDEN_JAHR];

            bool vollstaendig = true;

            // PW2/BW2: die Betriebskalender dieses Aufrufs - je ID einmal gelesen, ihre Tagesarten
            // gegen das Referenzjahr des Projekts aufgelöst. Ohne Kalender bleibt beides leer.
            var kalender = new Dictionary<int, Betriebskalender>();
            var tagesarten = new Dictionary<int, byte[]>();

            // Das eine Wochentagsraster des Projekts (E115) — dieselbe Auflösung wie Gebäudelauf und Zapfkalender:
            // ohne Preisreihenjahr w₀ der Klimaregion, mit ihm der Kalender des Jahres, für Kachelung und Feiertage.
            Gemeinjahrkalender raster = liste.Count > 0
                ? Konditionierungdatenweg.Raster(idProjekt, wochentagJan1)
                : default;
            if (liste.Count > 0) wochentagJan1 = raster.W0;

            for (int k = 0; k < liste.Count; k++)
            {
                Profileintrag eintrag = liste[k];
                string name = eintrag.Name;
                if (info != null) info.AktuellerName = name;

                // Die Quelle DIESES Satzes. Sie weicht nur in der Projektvorschau von
                // der aeusseren ab: Kennt das Projekt den Namen nicht, gilt der Katalog
                // (Befund W9-B-4/B-5, Reihenfolge nach W9-O-3c). Kopf UND Typprofil
                // kommen danach aus derselben Quelle - ihre Vermischung war der
                // Befund V0-4.
                ProfilQuelle satzquelle = quelle;
                DataRow kopf = eintrag.KopfId > 0
                               ? KopfLesenUeberId(quelle, idProjekt, eintrag.KopfId)
                               : KopfLesen(quelle, idProjekt, name);
                if (kopf == null && quelle.Rueckfall != null)
                {
                    satzquelle = quelle.Rueckfall;
                    kopf = KopfLesen(satzquelle, idProjekt, name);
                }

                if (kopf == null)
                {
                    // V0-3/V0-4: Im Projektmodus liefert die Projektkopie keinen Satz zu
                    // diesem Namen - Anteil 0 statt eines fremden Wertes. Dasselbe gilt,
                    // wenn eine Zuordnungszeile auf die Kopie eines ANDEREN Projekts zeigt
                    // (SV1): gelesen wird sie nie.
                    if (projektmodus)
                        SimulationProtokoll.Aktuell.Warnung(string.Format(quelle.TextKopfFehlt, name));
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                string bezeichner = kopf["Bezeichner"] != DBNull.Value
                                    ? kopf["Bezeichner"].ToString() : name;

                // Aus einer Zuordnungszeile gelesen, nennen Meldungen und Diagnose die
                // Projektkopie - der Bezeichner der Zeile kann ein alter Katalogname sein.
                if (eintrag.KopfId > 0)
                {
                    name = bezeichner;
                    if (info != null) info.AktuellerName = name;
                }

                // Projekt-Jahressumme: skalieren, wenn der Anwender sie geändert hat.
                // Die Vorgabe des offenen Dialogs geht der gespeicherten Zuordnung vor;
                // eine Zuordnungszeile bringt ihre Summe selbst mit (SV1/SV2), die
                // Vorschau sucht sie über die ID ihres Kopfsatzes.
                double pjv = 0;
                if (quelle.Jahressummen != null && quelle.Jahressummen.TryGetValue(name, out double vorgabe))
                    pjv = vorgabe;
                else if (eintrag.Summe.HasValue)
                    pjv = eintrag.Summe.Value;
                else if (idProjekt != 0)
                    pjv = ProjektJahressumme(satzquelle, idProjekt, kopf);

                double jv = 0;
                for (int i = 0; i < MONATE; i++)
                {
                    double d = (double)kopf["Monat_" + (i + 1).ToString()];
                    monatswerte[i] = (double)d;
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
                    // Ohne Typbezug gibt es keine Verteilung: DIESES Profil wird mit
                    // benannter Warnung übersprungen (Anteil 0), die übrigen rechnen
                    // vollständig - wie bei fehlendem Kopfsatz oder Wochenprofil. Ein
                    // Abbruch der ganzen Bedarfsart ließe die schon aufaddierten Profile
                    // davor stehen; die Summe hinge dann an der Reihenfolge (Befund PW6).
                    SimulationProtokoll.Aktuell.Warnung(string.Format(quelle.TextTypUndefiniert, name));
                    vollstaendig = false;
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                string typ = objTyp.ToString();

                // V0-3: Wochenprofil vor JEDEM Ladevorgang nullen.
                Array.Clear(wochenwerte, 0, wochenwerte.Length);

                if (!WochenprofilLesen(satzquelle, idProjekt, typ, KopfId(kopf), wochenwerte))
                {
                    if (projektmodus)
                        SimulationProtokoll.Aktuell.Warnung(string.Format(
                            quelle.TextTypprofilFehlt, typ, name));
                    if (info != null) info.Uebersprungen++;
                    continue;
                }

                double profilsumme = 0;
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
                // PW2/BW2: Mit Betriebskalender legt die Kalenderschicht Feiertage und Ferien
                // zwischen Kachelung und Monatsnormierung; ohne ihn rechnet der Weg wie zuvor.
                Betriebskalender kal = KalenderDesEintrags(quelle, satzquelle, idProjekt, eintrag, kopf, kalender);
                if (kal == null)
                {
                    WPPlan.Core.BhkwPlan.StromWocheToJahr(wochenwerte, monatswerte, jahreswerte,
                                                          moAnfang, moEnde, wochentagJan1);
                }
                else
                {
                    if (!tagesarten.TryGetValue(kal.ID, out byte[] arten))
                    {
                        arten = kal.Tagesarten(raster);
                        tagesarten[kal.ID] = arten;
                    }
                    if (!Betriebskalenderschicht.WocheZuJahr(wochenwerte, monatswerte, jahreswerte,
                                                             moAnfang, moEnde, wochentagJan1,
                                                             arten, kal.Ferienfaktor, kal.FerienKuerzen))
                        SimulationProtokoll.Aktuell.Hinweis(string.Format(
                            MyResource.Resource.SIMENG_KALENDER_MONAT_OHNE_BETRIEB,
                            quelle.Praefix, name, kal.Bezeichner));
                }
                info?.JeProfil?.Invoke(kopf, jahreswerte);
                WPPlan.Core.BhkwPlan.VectorenAddieren(jahreswerte, ziel);
                if (info != null) info.Gerechnet++;
            }

            if (monatssummen != null)
                WPPlan.Core.BhkwPlan.MonatsSumme(ziel, monatssummen, moAnfang, moEnde);

            return vollstaendig;
        }

        /// <summary>
        /// Der Betriebskalender eines Eintrags (PW2/BW2); <c>null</c> = keiner. Eine Zuordnungszeile
        /// bringt seine ID mit; die Projektvorschau sucht sie wie die Jahressumme über die ID des
        /// Kopfsatzes. Die Katalogvorschau und ein Katalogsatz (Rückfall) kennen keinen Kalender.
        /// Ein Kalender, den es nicht mehr gibt, gilt als keiner.
        /// </summary>
        private static Betriebskalender KalenderDesEintrags(ProfilQuelle quelle, ProfilQuelle satzquelle, int idProjekt,
                                                            Profileintrag eintrag, DataRow kopf,
                                                            Dictionary<int, Betriebskalender> cache)
        {
            int id = 0;
            if (eintrag.KalenderId.HasValue)
                id = eintrag.KalenderId.Value;
            else if (idProjekt != 0 && satzquelle.Modus != ProfilQuellmodus.Katalogvorschau &&
                     BedarfNetzKalenderSchema.KalenderspaltenVorhanden())
            {
                int kopfId = KopfId(kopf);
                if (kopfId > 0)
                {
                    object wert = DataRepository.ExecuteScalar(
                        "SELECT " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " FROM " + quelle.ZuordnungTabelle +
                        " WHERE ID_Projekt=? AND " + quelle.ZuordnungIdSpalte + "=? ORDER BY ID",
                        new DbParam("?", idProjekt),
                        new DbParam("?", kopfId));
                    if (wert != null && wert != DBNull.Value) id = Convert.ToInt32(wert);
                }
            }
            if (id <= 0) return null;
            if (!cache.TryGetValue(id, out Betriebskalender kal))
            {
                kal = BetriebskalenderCtrl.Lies(id);
                cache[id] = kal;
            }
            return kal;
        }

        /// <summary>Eine Namensliste als Einträge ohne Zuordnungsbezug.</summary>
        private static List<Profileintrag> Namenseintraege(List<string> namen)
        {
            var eintraege = new List<Profileintrag>(namen.Count);
            foreach (string n in namen) eintraege.Add(new Profileintrag { Name = n ?? "" });
            return eintraege;
        }

        /// <summary>Die ID eines Kopfsatzes; 0, wenn die Tabelle keine führt.</summary>
        private static int KopfId(DataRow kopf)
        {
            if (kopf == null || !kopf.Table.Columns.Contains("ID") || kopf["ID"] == DBNull.Value) return 0;
            return Convert.ToInt32(kopf["ID"]);
        }

        /// <summary>
        /// Kopfsatz eines Profils über den NAMEN; <c>null</c> = kein Treffer.
        ///
        /// <para>Unter gleichnamigen Kopien DESSELBEN Projekts gilt zuerst die, auf die eine
        /// Zuordnungszeile des Projekts per ID zeigt (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>,
        /// SV1/SV2) — mit genau der rechnet der Lauf. Erst danach irgendeine Kopie des Projekts
        /// (sie übernimmt beim Speichern auch <c>CopyFromStamm</c>).</para>
        /// </summary>
        private static DataRow KopfLesen(ProfilQuelle quelle, int idProjekt, string name)
        {
            string sql = "SELECT * FROM " + quelle.KopfTabelle + " WHERE Bezeichner=?";
            DataTable dt;

            if (quelle.ProjektfilterAktiv)
            {
                dt = DataRepository.GetDataTable(
                    "SELECT k.* FROM " + quelle.KopfTabelle + " k INNER JOIN " + quelle.ZuordnungTabelle +
                    " z ON z." + quelle.ZuordnungIdSpalte + " = k.ID" +
                    " WHERE k.Bezeichner=? AND k.ID_Projekt=? AND z.ID_Projekt=? ORDER BY z.ID",
                    new DbParam("?", name),
                    new DbParam("?", idProjekt),
                    new DbParam("?", idProjekt));
                if (dt != null && dt.Rows.Count > 0) return dt.Rows[0];

                dt = DataRepository.GetDataTable(sql + " AND ID_Projekt=?",
                                                 new DbParam("?", name),
                                                 new DbParam("?", idProjekt));
            }
            else
                dt = DataRepository.GetDataTable(sql, new DbParam("?", name));

            return (dt != null && dt.Rows.Count > 0) ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Kopfsatz über die ID aus einer Zuordnungszeile (SV1/SV2); <c>null</c> = kein Treffer.
        /// Mit Projektfilter gilt nur die Kopie DIESES Projekts: Zeigt die Zeile auf die
        /// Kopie eines anderen Projekts, wird sie nicht gelesen.
        /// </summary>
        private static DataRow KopfLesenUeberId(ProfilQuelle quelle, int idProjekt, int kopfId)
        {
            string sql = "SELECT * FROM " + quelle.KopfTabelle + " WHERE ID=?";
            DataTable dt;

            if (quelle.ProjektfilterAktiv)
                dt = DataRepository.GetDataTable(sql + " AND ID_Projekt=?",
                                                 new DbParam("?", kopfId),
                                                 new DbParam("?", idProjekt));
            else
                dt = DataRepository.GetDataTable(sql, new DbParam("?", kopfId));

            return (dt != null && dt.Rows.Count > 0) ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Vom Anwender im Projekt hinterlegte Jahressumme des Profils; 0 = keine. EINE
        /// parametrisierte Abfrage für alle drei Bedarfsarten — gelesen wird die erste
        /// Trefferzeile.
        ///
        /// <para><b>Über die ID</b> (<see cref="ProfilQuelle.ZuordnungIdSpalte"/>, SV1/SV2): die
        /// Zuordnungszeile des Projekts, die auf DIESEN Kopfsatz zeigt — nie über den
        /// Bezeichner der Zeile. Ein Katalogsatz (Rückfall der Projektvorschau) hat keine
        /// Zuordnungszeile — dort gilt allein die Vorgabe des Dialogs.</para>
        /// </summary>
        private static double ProjektJahressumme(ProfilQuelle quelle, int idProjekt, DataRow kopf)
        {
            if (quelle.Modus == ProfilQuellmodus.Katalogvorschau) return 0;
            int kopfId = KopfId(kopf);
            if (kopfId <= 0) return 0;

            object wert = DataRepository.ExecuteScalar(
                "SELECT " + quelle.ZuordnungSummeSpalte + " FROM " + quelle.ZuordnungTabelle +
                " WHERE ID_Projekt=? AND " + quelle.ZuordnungIdSpalte + "=? ORDER BY ID",
                new DbParam("?", idProjekt),
                new DbParam("?", kopfId));

            if (wert == null || wert == DBNull.Value) return 0;
            return (double)Convert.ToDouble(wert);
        }

        /// <summary>
        /// Liest die 168 Wochenwerte des Typs in <paramref name="wochenwerte"/>.
        /// Rückgabe false = kein Typsatz gefunden (der Puffer bleibt unberührt).
        ///
        /// <para>Kennt die Typtabelle ihren Kopfsatz per ID
        /// (<see cref="ProfilQuelle.TypKopfIdSpalte"/>, SV1/SV2: alle Projektkopien), gilt
        /// zuerst die Typzeile, die zu genau diesem Kopfsatz gehört; erst ohne sie die
        /// gleichnamige des Projekts.</para>
        /// </summary>
        private static bool WochenprofilLesen(ProfilQuelle quelle, int idProjekt, string typ,
                                              int kopfId, double[] wochenwerte)
        {
            string sql = "SELECT * FROM " + quelle.TypTabelle +
                         " WHERE " + quelle.TypSchluesselSpalte + "=?";
            DataTable dt = null;

            if (!string.IsNullOrEmpty(quelle.TypKopfIdSpalte) && kopfId > 0)
                dt = DataRepository.GetDataTable(sql + " AND " + quelle.TypKopfIdSpalte + "=?",
                                                 new DbParam("?", typ),
                                                 new DbParam("?", kopfId));

            if (dt == null || dt.Rows.Count == 0)
            {
                if (quelle.ProjektfilterAktiv)
                    dt = DataRepository.GetDataTable(sql + " AND ID_Projekt=?",
                                                     new DbParam("?", typ),
                                                     new DbParam("?", idProjekt));
                else
                    dt = DataRepository.GetDataTable(sql, new DbParam("?", typ));
            }

            if (dt == null || dt.Rows.Count == 0) return false;

            DataRow row = dt.Rows[0];
            for (int i = 0; i < WOCHEN_STUNDEN; i++)
            {
                double dw = (double)row[(i + 1).ToString()];
                wochenwerte[i] = (double)dw;
            }
            return true;
        }
    }
}
