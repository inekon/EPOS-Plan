using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE KWKG-VORGABEN DES PROJEKTS WANDERN IN DIE ANLAGENZEILEN
    // (Etappe BK1, Entscheid BK-E-1 (a) vom 18.09.2026, Migrationsschritt 89)
    //
    // WOZU. Der KWK-Zuschlag hatte ZWEI Wahrheiten: eine je Anlage
    // (Tab_Energieanlagen.KWKG_*, Schemaschritt 22) und eine je Projekt
    // (Tab_ProjektWirtschaftlichkeit.KWKG_*, Schemaschritt 28). Gerechnet wurde mit einer
    // Rueckfallkette - was an der Anlage leer war, holte sich der Rechenweg aus dem
    // Projekt. Das Gesetz kennt diese Projektgroessen nicht: § 7 bemisst den Satz an der
    // LEISTUNG der einzelnen Anlage, § 8 das Kontingent an IHRER Anlagenart und IHREM
    // Kostenanteil. Eine Kaskade aus zwei verschieden alten Modulen konnte deshalb nie
    // richtig gerechnet werden, und der Anwender pflegte Felder, deren Wirkung davon
    // abhing, ob ein zweites Feld anderswo leer war.
    //
    // WAS DER SCHRITT TUT. Er legt die fehlende Spalte KWKG_Kostenanteil an
    // (SchemaKatalog.Schritt89_KwkAnlagenwahrheit) und traegt danach EINMALIG in jede
    // BHKW-Anlagenzeile den Projektwert nach, der an ihrer Stelle leer ist - genau den
    // Wert, den der Rueckfall ihr bisher zugewiesen hat. Erst danach gibt der Rechenweg
    // den Rueckfall auf.
    //
    // ERGEBNISNEUTRAL, UND DARIN LIEGT DER BEWEIS. Jede Anlage rechnet nach dem Schritt
    // mit derselben Zahl wie davor: Stand die Zahl schon an der Anlage, faesst der
    // Schritt sie nicht an (WHERE ... IS NULL); stand sie nur am Projekt, steht sie
    // danach an der Anlage - dort, wo der Rueckfall sie ohnehin gelesen hat. Ein
    // Projekt, dessen Anlage einen EIGENEN Wert und dessen Projektzeile einen ABWEICHENDEN
    // Wert fuehrt, behaelt den Anlagenwert; das ist dieselbe Entscheidung, die der
    // Rueckfall getroffen hat.
    //
    // DER TATBESTAND MUSS MIT. KWKG_Tatbestand (Projekt) und KWKG_Eigenstromfall (Anlage)
    // sind dieselbe Angabe des § 6 Abs. 3 an zwei Orten. Ohne ihn stuende an der Anlage
    // ein Satz auf selbst genutzten Strom ohne die Voraussetzung, die ihn traegt.
    //
    // 0 UND LEER SIND DASSELBE. Die Nullsemantik des ganzen Dialogs: Eine 0 heisst "kein
    // eigener Wert", nicht "ausdruecklich null". Die Zielbedingung fasst deshalb beide,
    // und die Quellbedingung ueberspringt eine Quelle, die selbst 0 bzw. leer ist - dort
    // gibt es nichts zu uebertragen.
    //
    // IDEMPOTENZ. Jede Anweisung traegt ihre Bedingung selbst: Nach dem ersten Lauf ist
    // die Zielzelle gefuellt (und damit nicht mehr NULL/0), oder die Quelle war leer und
    // bleibt es. Der Zweitlauf trifft keine Zeile.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KlimaWaisenBereinigung (62), BhkwLeistungsgrenzeVorgabe (67) und
    // StromspeicherFirmaNachtrag (68): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt im Access-Zweig, das Werkzeug Werkzeuge/Testdatenbankschema und den
    // Nachweis in EPOS.Kern.Tests.
    //
    // BEFUND auf Referenzlaeufe/Kenndaten_Test.sqlite (18.09.2026, vor dem Schritt):
    // Fuenf Projekte fuehren BHKW-Anlagenzeilen (1017, 1018, 1024, 1030, 1031), KEINE
    // einzige traegt eine eigene KWKG-Angabe. Genau EIN Projekt fuehrt KWKG-Vorgaben:
    // 1030 (Bonus 4,00 / Einspeisung 8,00 ct/kWh, Kontingent 30.000 Vbh, Stichtag
    // 01.09.2026, Inbetriebnahme 01.03.2027) - dorthin wandern sie, in beide Module.
    // ====================================================================================

    /// <summary>
    /// Die Anweisungen des Migrationsschritts 89: <b>eine je Spaltenpaar</b>, in der
    /// Reihenfolge, in der sie laufen. Der Schritt selbst steht bei
    /// <c>SchemaMigration.SCHRITT_89_KWK_ANLAGENWAHRHEIT</c>, die DDL bei
    /// <see cref="SchemaKatalog.Schritt89_KwkAnlagenwahrheit"/>.
    /// </summary>
    public static class KwkAnlagenwahrheit
    {
        /// <summary>Die Anlagentabelle — Ziel jeder Anweisung.</summary>
        public const string TABELLE = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>Die Projekttabelle — Quelle jeder Anweisung.</summary>
        public const string QUELLE = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT;

        /// <summary>
        /// Ein übertragenes Wertepaar: die Spalte an der Anlage, die Spalte am Projekt
        /// und die Art, in der „leer" geschrieben wird.
        /// </summary>
        public sealed class Paar
        {
            /// <summary>Spalte in <see cref="TABELLE"/>.</summary>
            public readonly string Anlage;

            /// <summary>Spalte in <see cref="QUELLE"/>.</summary>
            public readonly string Projekt;

            /// <summary>true = Text (leer ist <c>NULL</c> oder <c>''</c>),
            /// false = Zahl bzw. Datum (leer ist <c>NULL</c>, bei Zahlen auch 0).</summary>
            public readonly bool Text;

            /// <summary>true = eine 0 zählt als „nicht gepflegt" (alle Zahlenspalten);
            /// false = nur <c>NULL</c> ist leer (die beiden Datumsspalten).</summary>
            public readonly bool NullWieLeer;

            public Paar(string anlage, string projekt, bool text, bool nullWieLeer)
            {
                Anlage = anlage;
                Projekt = projekt;
                Text = text;
                NullWieLeer = nullWieLeer;
            }
        }

        /// <summary>
        /// Die NEUN Wertepaare des Schritts. Die Reihenfolge ist ohne Wirkung — jede
        /// Anweisung fasst eine andere Zielspalte an —, folgt aber der Reihenfolge des
        /// Dialogs: erst die Sätze, dann die Mengengrößen, dann die Einordnungen, zuletzt
        /// die Daten.
        /// </summary>
        public static IReadOnlyList<Paar> Paare
        {
            get
            {
                return new[]
                {
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN,
                             SchemaKatalog.SPALTE_PW_KWKG_BONUS,             false, true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP,
                             SchemaKatalog.SPALTE_PW_KWKG_BONUS_EINSPEISUNG, false, true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT,
                             SchemaKatalog.SPALTE_PW_KWKG_KONTINGENT,        false, true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_DECKEL,
                             SchemaKatalog.SPALTE_PW_KWKG_JAHRESDECKEL,      false, true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL,
                             SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL,      false, true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART,
                             SchemaKatalog.SPALTE_PW_KWKG_ANLAGENART,        true,  true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL,
                             SchemaKatalog.SPALTE_PW_KWKG_TATBESTAND,        true,  true),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG,
                             SchemaKatalog.SPALTE_PW_KWKG_STICHTAG,          false, false),
                    new Paar(SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME,
                             SchemaKatalog.SPALTE_PW_KWKG_INBETRIEBNAHME,    false, false),
                };
            }
        }

        /// <summary>
        /// Die Übertragung EINES Paares. <c>UPDATE</c> auf die BHKW-Anlagenzeilen, deren
        /// Zielzelle leer ist und deren Projektzeile einen Wert führt.
        /// </summary>
        public static string Uebertragung(Paar paar)
        {
            string unterabfrage = "(SELECT w.[" + paar.Projekt + "] FROM " + QUELLE +
                                  " AS w WHERE w.ID_Projekt = " + TABELLE + ".ID_Projekt)";
            return "UPDATE " + TABELLE +
                   " SET [" + paar.Anlage + "] = " + unterabfrage +
                   " WHERE " + Zielbedingung(paar) + " AND " + Quellbedingung(paar, unterabfrage);
        }

        /// <summary>
        /// Zählt die Zeilen, die <see cref="Uebertragung"/> anfassen wird — vor und nach
        /// dem Schritt, damit der Lauf-Bericht sagen kann, was er getan hat. <b>Dieselbe
        /// Bedingung</b> wie die Übertragung; sonst zählte der Bericht etwas anderes, als
        /// der Schritt anfasst.
        /// </summary>
        public static string Zaehlung(Paar paar)
        {
            string unterabfrage = "(SELECT w.[" + paar.Projekt + "] FROM " + QUELLE +
                                  " AS w WHERE w.ID_Projekt = " + TABELLE + ".ID_Projekt)";
            return "SELECT COUNT(*) FROM " + TABELLE +
                   " WHERE " + Zielbedingung(paar) + " AND " + Quellbedingung(paar, unterabfrage);
        }

        /// <summary>„Die Anlage führt hier keinen eigenen Wert" — und es ist eine
        /// BHKW-Zeile; Kessel und Wärmepumpen kennen den KWK-Zuschlag nicht.</summary>
        private static string Zielbedingung(Paar paar)
        {
            string leer = paar.Text
                ? "([" + paar.Anlage + "] IS NULL OR [" + paar.Anlage + "] = '')"
                : paar.NullWieLeer
                    ? "([" + paar.Anlage + "] IS NULL OR [" + paar.Anlage + "] = 0)"
                    : "[" + paar.Anlage + "] IS NULL";
            return "ID_Type = " + WizardItemClass.BHKW_TYP + " AND " + leer;
        }

        /// <summary>„Das Projekt führt hier einen Wert" — sonst gibt es nichts zu
        /// übertragen, und die Anweisung soll die Zelle nicht mit einer 0 belegen, die
        /// dieselbe Aussage trüge wie das NULL davor.</summary>
        private static string Quellbedingung(Paar paar, string unterabfrage)
        {
            if (paar.Text) return unterabfrage + " IS NOT NULL AND " + unterabfrage + " <> ''";
            if (paar.NullWieLeer) return unterabfrage + " > 0";
            return unterabfrage + " IS NOT NULL";
        }
    }
}
