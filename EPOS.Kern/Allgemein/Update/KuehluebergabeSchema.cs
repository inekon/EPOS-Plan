using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Schemaschritte der Kälteseite der Anlagenkopplung</b> (Entscheid E37; Konzept
    /// Anlagenkopplung 8, 8.1, 8.3; Stufe AK1 Welle 4) — EINE Quelle für Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>, die Arbeitskopie der Tests und den Nachweis. Die
    /// Nummern stehen allein hier.
    ///
    /// <para><b>Drei Schritte, drei Nummern:</b></para>
    /// <list type="bullet">
    /// <item><b>KAK-S1 (<see cref="SCHRITT"/>)</b> — die Kühlübergabe am Gebäude: acht Spalten je
    /// Gebäudetabelle samt viertem Neubau der Sicht <c>Abfrage_Projektgebaeude</c> (98 Spalten;
    /// <see cref="GebaeudeSchema.Kuehluebergabespalten"/> — sie stehen dort, weil die Klasse der
    /// Gebäudefamilie die Sicht baut). 16 <see cref="SchemaSpalte"/>-Einträge.</item>
    /// <item><b>KAK-S3 (<see cref="SCHRITT_ERGEBNIS"/>)</b> — die Ergebniszahlen der Kälteseite:
    /// drei Spalten an <c>Tab_ErgebnisEnergiebedarf</c> (<see cref="Ergebnisspalten"/>, Muster
    /// 123) und fünf an <c>Tab_ErgebnisGebaeude</c> (<see cref="SpaltenKuehlkreis"/>, Muster 128).</item>
    /// <item><b>Zone (<see cref="SCHRITT_ZONE"/>)</b> — drei Spalten der Kühlübergabe an
    /// <c>Tab_Zone</c> (<see cref="SpaltenZone"/>), ohne Schalter wie die Heizseite; NULL heißt
    /// „Wert des Gebäudes" bzw. „Anteil der Zonenfläche". Gelesen erst ab G6.</item>
    /// </list>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL, kein DML: Der Schalter steht danach auf 0, alle
    /// übrigen Spalten auf NULL — und NULL ist die Vorgabe: Kühlübergabe ideal, Ergebnis „nicht
    /// kühlgekoppelt gerechnet". Kein Referenzprojekt rechnet gekoppelt; der Referenzlauf bleibt
    /// byte-gleich.</para>
    /// </summary>
    public static class KuehluebergabeSchema
    {
        // =====================================================================
        //  Die Nummern — vergeben unmittelbar vor dem Schemacommit gegen origin
        //  (Regel „lückenlos"); die EINE Stelle, an der sie stehen.
        // =====================================================================

        /// <summary>Schritt KAK-S1 — die Kühlübergabe an den Gebäudetabellen samt viertem Sichtneubau.</summary>
        public const int SCHRITT = 135;

        /// <summary>Schritt KAK-S3 — die Ergebnisspalten der Kälteseite.</summary>
        public const int SCHRITT_ERGEBNIS = SCHRITT + 1;

        /// <summary>Schritt Zone — die drei Spalten der Kühlübergabe an <c>Tab_Zone</c> (nach S-C).</summary>
        public const int SCHRITT_ZONE = SCHRITT + 2;

        // =====================================================================
        //  KAK-S1 — die Kühlübergabe am Gebäude (8.1)
        // =====================================================================

        /// <summary>Steht KAK-S1? Die 16 Spalten und die Sicht (<see cref="GebaeudeSchema.KuehluebergabespaltenVollstaendig"/>).</summary>
        public static bool GebaeudeVollstaendig() => GebaeudeSchema.KuehluebergabespaltenVollstaendig();

        /// <summary>
        /// Führt KAK-S1 aus — für <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>
        /// (<see cref="GebaeudeSchema.KuehluebergabespaltenAlle"/>); <b>wiederholbar</b>, <b>kein DML</b>.
        /// </summary>
        /// <returns>Die Zahl der angelegten Spalten (höchstens 16).</returns>
        public static int GebaeudeAlle(IList<string> bericht) => GebaeudeSchema.KuehluebergabespaltenAlle(bericht);

        // =====================================================================
        //  KAK-S3 — die Ergebnisspalten der Kälteseite (8.3)
        // =====================================================================

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Kuehl_Vorlauf_Mittel</c> [°C] — kältebedarfsgewichtetes Mittel des Kaltwasser-Vorlaufs.</summary>
        public const string SPALTE_KUEHL_VORLAUF_MITTEL = "Kuehl_Vorlauf_Mittel";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Kuehl_Ruecklauf_Mittel</c> [°C] — dasselbe für den Rücklauf.</summary>
        public const string SPALTE_KUEHL_RUECKLAUF_MITTEL = "Kuehl_Ruecklauf_Mittel";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Kuehl_Uebergabe_Begrenzt_Stunden</c> [h] — Stunden, in denen die Kühlübergabe die Grenze war.</summary>
        public const string SPALTE_KUEHL_UEBERGABE_BEGRENZT_STUNDEN = "Kuehl_Uebergabe_Begrenzt_Stunden";

        /// <summary>
        /// Die drei <see cref="SchemaSpalte"/>-Einträge an <c>Tab_ErgebnisEnergiebedarf</c> (Muster
        /// Schritt 123) — <b>DOUBLE, nullbar, ohne Vorgabe und ohne Nachtrag</b>: NULL heißt „nicht
        /// kühlgekoppelt gerechnet". Der Referenzlauf-Export nimmt sie erst mit einem Wert in
        /// <c>aggregate.csv</c> auf (<c>Referenzlauf/Ergebnisexport.cs</c>, <c>SpaltenNurMitWert</c>).
        /// </summary>
        public static readonly SchemaSpalte[] Ergebnisspalten =
        {
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_KUEHL_VORLAUF_MITTEL,             "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_KUEHL_RUECKLAUF_MITTEL,           "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_KUEHL_UEBERGABE_BEGRENZT_STUNDEN, "DOUBLE"),
        };

        /// <summary><c>Tab_ErgebnisGebaeude.Kuehl_Uebergabe_Art</c> — die Kühlübergabeart des Laufs; <b>NULL heißt „nicht kühlgekoppelt gerechnet"</b>.</summary>
        public const string SPALTE_ERGEBNIS_KUEHL_UEBERGABE_ART = "Kuehl_Uebergabe_Art";

        /// <summary><c>Tab_ErgebnisGebaeude.KuehlVorlaufMittel_C</c> — kältebedarfsgewichtetes Mittel des Vorlaufs [°C].</summary>
        public const string SPALTE_KUEHL_VORLAUF_MITTEL_C = "KuehlVorlaufMittel_C";

        /// <summary><c>Tab_ErgebnisGebaeude.KuehlRuecklaufMittel_C</c> — dasselbe für den Rücklauf [°C].</summary>
        public const string SPALTE_KUEHL_RUECKLAUF_MITTEL_C = "KuehlRuecklaufMittel_C";

        /// <summary><c>Tab_ErgebnisGebaeude.KuehlUebergabeBegrenzt_H</c> — Stunden mit begrenzter Kühlübergabe [h], einschließlich der Stunden an der Vorlaufgrenze.</summary>
        public const string SPALTE_KUEHL_UEBERGABE_BEGRENZT_H = "KuehlUebergabeBegrenzt_H";

        /// <summary><c>Tab_ErgebnisGebaeude.KuehlVorlaufgrenze_H</c> — der Anteil davon an der Vorlaufgrenze [h] (7.2).</summary>
        public const string SPALTE_KUEHL_VORLAUFGRENZE_H = "KuehlVorlaufgrenze_H";

        /// <summary>Die drei Kühlübergabearten als SQL-Literal (Quelle des <c>CHECK</c> im Ergebnis, ohne IDEAL: NULL heißt dort „nicht gekoppelt").</summary>
        public const string WERTE_KUEHLUEBERGABE_ART =
            "'" + DbWerte.KUEHLUEBERGABE_KUEHLDECKE + "','" + DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG + "','" +
            DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR + "'";

        /// <summary>
        /// <b>Die fünf Spalten an <c>Tab_ErgebnisGebaeude</c></b> (Muster Schritt 128) in
        /// Anlegereihenfolge: Name und SQLite-Definition (STRICT-Typ samt <c>CHECK</c>),
        /// <b>nullbar, ohne Vorgabe und ohne Nachtrag</b>. Die Stunden sind Summen von
        /// Zeitanteilen und deshalb <c>REAL</c> (0 … 8 760).
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenKuehlkreis = new[]
        {
            new KeyValuePair<string, string>(SPALTE_ERGEBNIS_KUEHL_UEBERGABE_ART,
                "TEXT CHECK (\"" + SPALTE_ERGEBNIS_KUEHL_UEBERGABE_ART + "\" IN (" + WERTE_KUEHLUEBERGABE_ART + "))"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_VORLAUF_MITTEL_C, "REAL"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_RUECKLAUF_MITTEL_C, "REAL"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_UEBERGABE_BEGRENZT_H,
                "REAL CHECK (\"" + SPALTE_KUEHL_UEBERGABE_BEGRENZT_H + "\" BETWEEN 0 AND 8760)"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_VORLAUFGRENZE_H,
                "REAL CHECK (\"" + SPALTE_KUEHL_VORLAUFGRENZE_H + "\" BETWEEN 0 AND 8760)"),
        };

        /// <summary>Steht KAK-S3? Die drei Spalten an <c>Tab_ErgebnisEnergiebedarf</c> und die fünf an <c>Tab_ErgebnisGebaeude</c>.</summary>
        public static bool ErgebnisVollstaendig()
            => Ergebnisspalten.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name))
               && ErgebnisGebaeudeSchema.Vorhanden()
               && SpaltenKuehlkreis.All(s => DataRepository.SpalteVorhanden(ErgebnisGebaeudeSchema.TAB, s.Key));

        /// <summary>
        /// Führt KAK-S3 in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer. <b>Wiederholbar</b>, <b>kein DML</b>; ohne <c>Tab_ErgebnisGebaeude</c> (Stand
        /// vor 107) bleibt deren Teil aus.
        /// </summary>
        /// <returns>Die Zahl der angelegten Spalten (höchstens acht).</returns>
        public static int ErgebnisAlle(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Ergebnisspalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();
            bool gebaeude = ErgebnisGebaeudeSchema.Vorhanden();
            var fehlendGebaeude = gebaeude
                ? SpaltenKuehlkreis.Where(s => !DataRepository.SpalteVorhanden(ErgebnisGebaeudeSchema.TAB, s.Key)).ToList()
                : new List<KeyValuePair<string, string>>();

            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    foreach (KeyValuePair<string, string> s in fehlendGebaeude)
                    {
                        v.Ausfuehren(ErgebnisGebaeudeSchema.SpalteAnlegen(s));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         (Ergebnisspalten.Length + SpaltenKuehlkreis.Count).ToString(CultureInfo.InvariantCulture) +
                         " Ergebnisspalte(n) der Kuehluebergabe angelegt" +
                         (gebaeude ? "" : " (" + ErgebnisGebaeudeSchema.TAB + " fehlt)"));
            return angelegt;
        }

        // =====================================================================
        //  Zone — die drei Spalten der Kühlübergabe an Tab_Zone (nach S-C)
        // =====================================================================

        /// <summary>
        /// Die Wertliste der Kühlübergabeart an der Zone — die drei Arten EINSCHLIESSLICH
        /// <c>IDEAL</c>, nach dem Muster von <see cref="ZonenSchema.WERTE_UEBERGABE_ART"/>: An der
        /// Zone heißt NULL „Wert des Gebäudes", eine ideal gekühlte Zone in einem kühlgekoppelten
        /// Gebäude braucht deshalb den ausdrücklichen Wert.
        /// </summary>
        public const string WERTE_KUEHLUEBERGABE_ART_ZONE =
            "'" + DbWerte.KUEHLUEBERGABE_IDEAL + "'," + WERTE_KUEHLUEBERGABE_ART;

        /// <summary>
        /// Die drei Spalten an <c>Tab_Zone</c>: Name und SQLite-Definition (STRICT-Typ, nullbar):
        /// Art mit Wertliste, Exponent und Nennleistung [kW] als <c>REAL</c>.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenZone = new[]
        {
            new KeyValuePair<string, string>(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART,
                "TEXT CHECK (\"" + GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART + "\" IN (" + WERTE_KUEHLUEBERGABE_ART_ZONE + "))"),
            new KeyValuePair<string, string>(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT, "REAL"),
            new KeyValuePair<string, string>(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN, "REAL"),
        };

        /// <summary>Die Anweisung, die eine Zonenspalte anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string ZonenspalteAnlegen(KeyValuePair<string, string> spalte)
            => "ALTER TABLE \"" + ZonenSchema.TAB_ZONE + "\" ADD COLUMN \"" + spalte.Key + "\" " + spalte.Value;

        /// <summary>Steht der Zonenschritt? <c>Tab_Zone</c> steht und trägt die drei Spalten.</summary>
        public static bool ZoneVollstaendig()
            => DataRepository.TabelleVorhanden(ZonenSchema.TAB_ZONE)
               && SpaltenZone.All(s => DataRepository.SpalteVorhanden(ZonenSchema.TAB_ZONE, s.Key));

        /// <summary>
        /// Führt den Zonenschritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c>
        /// und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b>, <b>kein DML</b>; ohne <c>Tab_Zone</c>
        /// (Stand vor S-C) tut er nichts.
        /// </summary>
        /// <returns>Die Zahl der angelegten Spalten (höchstens drei).</returns>
        public static int ZoneAlle(IList<string> bericht)
        {
            if (!DataRepository.TabelleVorhanden(ZonenSchema.TAB_ZONE))
            {
                bericht?.Add(ZonenSchema.TAB_ZONE + " fehlt (Stand vor Schritt " +
                             ZonenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + ") - nichts angelegt");
                return 0;
            }
            var fehlend = SpaltenZone.Where(s => !DataRepository.SpalteVorhanden(ZonenSchema.TAB_ZONE, s.Key)).ToList();

            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> s in fehlend)
                    {
                        v.Ausfuehren(ZonenspalteAnlegen(s));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         SpaltenZone.Count.ToString(CultureInfo.InvariantCulture) +
                         " Spalte(n) der Kuehluebergabe an " + ZonenSchema.TAB_ZONE + " angelegt");
            return angelegt;
        }
    }
}
