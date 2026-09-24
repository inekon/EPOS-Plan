using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Schemaschritte der Anlagenkopplung, Stufe AK1 Welle 1</b> (Konzept Anlagenkopplung
    /// Kapitel 8; Entscheide E22, E24, E25) — EINE Quelle fuer Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>, die Arbeitskopie der Tests und den Nachweis.
    ///
    /// <para><b>Zwei Schritte, zwei Nummern</b> — wie bei der Kuehlung traegt jeder Papiername
    /// seine eigene Nummer, und keiner ist mit einem anderen verschmolzen:</para>
    /// <list type="bullet">
    /// <item><b>AK-S1 (Schritt 122)</b> — die Waermeuebergabe: dreizehn Spalten je Gebaeudetabelle
    /// samt drittem Neubau der Sicht <c>Abfrage_Projektgebaeude</c>
    /// (<see cref="GebaeudeSchema.Uebergabespalten"/>; sie stehen dort, weil die Klasse der
    /// Gebaeudefamilie die Sicht baut) und die Projektspalte
    /// <c>Tab_Einstellungen.Anlagenkopplung</c> (<see cref="Projektspalte"/>) — zusammen
    /// 27 <see cref="SchemaSpalte"/>-Eintraege (8.1).</item>
    /// <item><b>AK-S3, Waermeteil (Schritt 123)</b> — drei Ergebnisspalten an
    /// <c>Tab_ErgebnisEnergiebedarf</c> (<see cref="Ergebnisspalten"/>, 8.3). Der Komfortteil
    /// kommt mit AK2, der Kaelteteil (F-A16: <c>Kuehl_Vorlauf_Mittel</c>,
    /// <c>Komfort_Ueberschreitungsstunden</c>, <c>Komfort_Kelvinstunden_Kuehlung</c>) mit einem
    /// eigenen Schritt; keines dieser Gegenstuecke steht bisher im Schema.</item>
    /// </list>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL, kein DML: Die Schalter stehen danach auf 0, alle
    /// uebrigen Spalten auf NULL — und NULL ist die Vorgabe: Uebergabeart ideal, Kopplung aus
    /// (<see cref="DbWerte.ANLAGENKOPPLUNG_AUS"/>), Ergebnis „nicht erhoben". Kein Rechenweg liest
    /// die Spalten; der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para><b>Nicht in <see cref="SchemaKatalog.Alle"/></b> — wie die Kuehlschritte: Die
    /// Rueckfallebene fuehrt die Anlagenkopplung nicht, und ihre Leser fragen die Spalte ueber
    /// die Zeile.</para>
    /// </summary>
    public static class AnlagenkopplungSchema
    {
        // =====================================================================
        //  AK-S1, Projektebene — die Kopplungsstufe des Projekts (8.1)
        // =====================================================================

        /// <summary>
        /// <c>Tab_Einstellungen.Anlagenkopplung</c>: die Kopplungsstufe des Projekts,
        /// <see cref="DbWerte.ANLAGENKOPPLUNG_AUS"/>, <c>…_AK1</c>, <c>…_AK2</c> oder <c>…_AK3</c>.
        /// <b>NULL = AUS</b> — kein DDL-DEFAULT auf einem Fachwert. Die Spalte laesst alle vier
        /// Werte zu, damit sie nicht zweimal geaendert werden muss; angeboten wird eine Stufe
        /// erst mit ihrem Rechenweg (Kuehlkonzept K7, Anlagenkopplung 9.4).
        /// </summary>
        public const string SPALTE_ANLAGENKOPPLUNG = "Anlagenkopplung";

        /// <summary>Die vier zugelassenen Werte der Projektspalte, in der Reihenfolge der Stufen.</summary>
        public static readonly string[] KOPPLUNGSSTUFEN =
        {
            DbWerte.ANLAGENKOPPLUNG_AUS, DbWerte.ANLAGENKOPPLUNG_AK1,
            DbWerte.ANLAGENKOPPLUNG_AK2, DbWerte.ANLAGENKOPPLUNG_AK3,
        };

        /// <summary>
        /// Der eine <see cref="SchemaSpalte"/>-Eintrag der Projektebene. Die Typangabe
        /// <c>TEXT(4)</c> nennt die Laenge nach dem Papier; angelegt wird die Spalte mit
        /// <see cref="TYP_ANLAGENKOPPLUNG"/> — die Wertliste ersetzt die Laengenpruefung, die die
        /// allgemeine Typuebersetzung daraus machte (<see cref="SqliteTyp"/>).
        /// </summary>
        public static readonly SchemaSpalte Projektspalte =
            new SchemaSpalte(SchemaKatalog.TAB_EINSTELLUNGEN, SPALTE_ANLAGENKOPPLUNG, "TEXT(4)");

        /// <summary>
        /// Alles hinter dem Spaltennamen des <c>ADD COLUMN</c> der Projektspalte:
        /// <c>TEXT CHECK ("Anlagenkopplung" IN ('AUS','AK1','AK2','AK3'))</c> — nullbar, ohne
        /// Vorgabe. SQLite prueft ein <c>CHECK</c> bei <c>ADD COLUMN</c> gegen die vorhandenen
        /// Zeilen; NULL besteht ihn.
        /// </summary>
        public static readonly string TYP_ANLAGENKOPPLUNG =
            "TEXT CHECK (\"" + SPALTE_ANLAGENKOPPLUNG + "\" IN (" +
            string.Join(",", KOPPLUNGSSTUFEN.Select(w => "'" + w + "'")) + "))";

        /// <summary>
        /// Die SQLite-Typdefinition einer Spalte dieser Schritte — <see cref="TYP_ANLAGENKOPPLUNG"/>
        /// fuer die Projektspalte, sonst die allgemeine Uebersetzung
        /// (<see cref="StilleDb.SqliteSpaltenTyp"/>). EINE Stelle fuer Migration, Werkzeug und Tests.
        /// </summary>
        public static string SqliteTyp(SchemaSpalte s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return s.Tabelle == Projektspalte.Tabelle && s.Name == Projektspalte.Name
                ? TYP_ANLAGENKOPPLUNG
                : StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition);
        }

        /// <summary>
        /// Alle 27 Eintraege von AK-S1 (Schritt 122): die 26 Uebergabespalten der
        /// Gebaeudetabellen (<see cref="GebaeudeSchema.Uebergabespalten"/>), dann die
        /// Projektspalte.
        /// </summary>
        public static IEnumerable<SchemaSpalte> UebergabeSpalten()
        {
            foreach (SchemaSpalte s in GebaeudeSchema.Uebergabespalten) yield return s;
            yield return Projektspalte;
        }

        /// <summary>
        /// Steht AK-S1 (Schritt 122) vollstaendig? Die 26 Uebergabespalten und die Sicht
        /// (<see cref="GebaeudeSchema.UebergabespaltenVollstaendig"/>) und die Projektspalte.
        /// </summary>
        public static bool UebergabeVollstaendig()
        {
            return GebaeudeSchema.UebergabespaltenVollstaendig()
                && DataRepository.SpalteVorhanden(Projektspalte.Tabelle, Projektspalte.Name);
        }

        /// <summary>
        /// Fuehrt AK-S1 (Schritt 122) aus — fuer <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg ueber ihre eigenen
        /// Helfer, aus denselben Definitionen. Erst der Gebaeudeteil samt Sichtneubau in einem
        /// Vorgang (<see cref="GebaeudeSchema.UebergabespaltenAlle"/>), dann die Projektspalte.
        /// <b>Wiederholbar:</b> Eine vorhandene Spalte wird uebergangen, die Sicht immer neu
        /// gebaut. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens 27).</returns>
        public static int UebergabeAlle(IList<string> bericht)
        {
            int angelegt = GebaeudeSchema.UebergabespaltenAlle(bericht);

            if (DataRepository.SpalteVorhanden(Projektspalte.Tabelle, Projektspalte.Name))
            {
                bericht?.Add(Projektspalte.Tabelle + "." + Projektspalte.Name + ": vorhanden");
                return angelegt;
            }

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("ALTER TABLE [" + Projektspalte.Tabelle + "] ADD COLUMN [" +
                                 Projektspalte.Name + "] " + SqliteTyp(Projektspalte));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(Projektspalte.Tabelle + "." + Projektspalte.Name + ": angelegt");
            return angelegt + 1;
        }

        // =====================================================================
        //  AK-S3, Waermeteil — die drei Ergebnisspalten (8.3)
        // =====================================================================

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Vorlauf_Mittel</c> [°C] — heizzeitgewichtetes Mittel des gefahrenen Vorlaufs.</summary>
        public const string SPALTE_VORLAUF_MITTEL = "Vorlauf_Mittel";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Ruecklauf_Mittel</c> [°C] — dasselbe fuer den Ruecklauf.</summary>
        public const string SPALTE_RUECKLAUF_MITTEL = "Ruecklauf_Mittel";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Uebergabe_Begrenzt_Stunden</c> [h] — Stunden, in denen die Uebergabe die Grenze war.</summary>
        public const string SPALTE_UEBERGABE_BEGRENZT_STUNDEN = "Uebergabe_Begrenzt_Stunden";

        /// <summary>
        /// Die drei <see cref="SchemaSpalte"/>-Eintraege des Waermeteils von AK-S3 (Schritt 123) in
        /// der Reihenfolge von Anlagenkopplung 8.3 — <b>DOUBLE, nullbar, ohne Vorgabe und ohne
        /// Nachtrag</b>: NULL heisst „nicht erhoben", und jede vorhandene Ergebniszeile ist eine
        /// Zeile ohne Kopplung. Die Reihen (Vorlauf, Ruecklauf, Uebergabe je Stunde) bleiben
        /// draussen — sie reisen erst mit der Rechnung als bedingte Dateien des
        /// Referenzlauf-Exports (8.3, 11.4).
        ///
        /// <para><b>Der Referenzlauf-Export</b> liest <c>Tab_ErgebnisEnergiebedarf</c> mit
        /// <c>SELECT *</c>; er nimmt eine dieser Spalten erst in <c>aggregate.csv</c> auf, wenn ein
        /// Lauf sie erhebt — solange sie NULL ist, bleibt die Datei unveraendert
        /// (<c>Referenzlauf/Ergebnisexport.cs</c>).</para>
        /// </summary>
        public static readonly SchemaSpalte[] Ergebnisspalten =
        {
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_VORLAUF_MITTEL,             "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_RUECKLAUF_MITTEL,           "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, SPALTE_UEBERGABE_BEGRENZT_STUNDEN, "DOUBLE"),
        };

        /// <summary>Steht der Waermeteil von AK-S3 (Schritt 123)? Alle drei Ergebnisspalten stehen.</summary>
        public static bool ErgebnisspaltenVollstaendig()
        {
            return Ergebnisspalten.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
        }

        /// <summary>
        /// Fuehrt den Waermeteil von AK-S3 (Schritt 123) in EINEM Vorgang aus — fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration der
        /// Schale geht denselben Weg ueber ihre eigenen Helfer. <b>Wiederholbar</b>, <b>kein DML</b>.
        /// </summary>
        /// <param name="bericht">Nimmt eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens drei).</returns>
        public static int ErgebnisspaltenAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Ergebnisspalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " + SqliteTyp(s));
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
                         Ergebnisspalten.Length.ToString(CultureInfo.InvariantCulture) +
                         " Ergebnisspalte(n) der Waermeuebergabe an " +
                         SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF + " angelegt");
            return angelegt;
        }

        // =====================================================================
        //  Das Wochenprofil — Format und strenger Leser (4.3, H8, H-F10)
        // =====================================================================

        /// <summary>Zahl der Werte eines Wochenprofils: Montag 00:00 bis Sonntag 23:00.</summary>
        public const int WOCHENWERTE = 168;

        /// <summary>Das Trennzeichen der Werte; Dezimaltrennzeichen ist der Punkt (<see cref="CultureInfo.InvariantCulture"/>).</summary>
        public const char TRENNZEICHEN = ';';

        /// <summary>Hoechste Laenge des Textes — dieselbe Zahl wie die Typangabe <c>TEXT(1400)</c> von <c>Sollwertprofil</c>.</summary>
        public const int PROFIL_LAENGE_MAX = 1400;

        /// <summary>Hoechstzahl der Nachkommastellen, die der Schreiber ablegt.</summary>
        public const int NACHKOMMASTELLEN = 2;

        /// <summary>Was der strenge Leser in einem Profiltext gefunden hat.</summary>
        public enum WochenprofilBefund
        {
            /// <summary>Kein Profil (NULL, leer oder nur Leerraum) — es gelten die Bestandswerte.</summary>
            KeinProfil,
            /// <summary>Genau 168 Zahlen — gelesen.</summary>
            Gelesen,
            /// <summary>Nicht genau 168 Werte — benannter Fehler, kein Auffuellen und kein Abschneiden.</summary>
            FalscheWertzahl,
            /// <summary>Ein Wert ist keine endliche Zahl in der Schreibweise des Profils — benannter Fehler.</summary>
            KeineZahl,
        }

        /// <summary>Das Ergebnis des strengen Lesers: Befund, Werte und die Angaben fuer die Meldung.</summary>
        public sealed class Wochenprofil
        {
            internal Wochenprofil(WochenprofilBefund befund, double[] werte, int gefunden, int stelle)
            {
                Befund = befund;
                Werte = werte;
                Gefunden = gefunden;
                Stelle = stelle;
            }

            /// <summary>Was gefunden wurde.</summary>
            public WochenprofilBefund Befund { get; }

            /// <summary>Die 168 Werte bei <see cref="WochenprofilBefund.Gelesen"/>, sonst <c>null</c>.</summary>
            public double[] Werte { get; }

            /// <summary>Die Zahl der gefundenen Werte (0 ohne Profil).</summary>
            public int Gefunden { get; }

            /// <summary>Die Stelle (1 bis 168) des ersten Werts, der keine Zahl ist; sonst 0.</summary>
            public int Stelle { get; }
        }

        /// <summary>
        /// <b>Der strenge Leser</b> (4.3, H-F10): genau 168 Werte, getrennt durch
        /// <see cref="TRENNZEICHEN"/>, jeder eine endliche Zahl mit Punkt als Dezimaltrennzeichen;
        /// Leerraum um einen Wert ist erlaubt. Alles andere ist ein <b>benannter</b> Befund — ein
        /// Profil mit 167 Werten ist ein Datenfehler, und ein still ergaenzter Wert waere eine
        /// erfundene Betriebszeit. Ein leerer Text ist wie NULL „kein Profil".
        ///
        /// <para>Der Leser sagt nichts ueber die Groesse der Werte: Ob ein Sollwert oder ein
        /// Verfuegbarkeitsfaktor (AK2) im zulaessigen Bereich liegt, prueft sein Verwender.</para>
        /// </summary>
        public static Wochenprofil WochenprofilLesen(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new Wochenprofil(WochenprofilBefund.KeinProfil, null, 0, 0);

            string[] teile = text.Split(TRENNZEICHEN);
            if (teile.Length != WOCHENWERTE)
                return new Wochenprofil(WochenprofilBefund.FalscheWertzahl, null, teile.Length, 0);

            var werte = new double[WOCHENWERTE];
            for (int i = 0; i < WOCHENWERTE; i++)
            {
                if (!double.TryParse(teile[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                                     out double w) || !double.IsFinite(w))
                    return new Wochenprofil(WochenprofilBefund.KeineZahl, null, teile.Length, i + 1);
                werte[i] = w;
            }
            return new Wochenprofil(WochenprofilBefund.Gelesen, werte, WOCHENWERTE, 0);
        }

        /// <summary>
        /// <b>Der Schreiber</b> — das Gegenstueck zu <see cref="WochenprofilLesen"/>: 168 endliche
        /// Werte, je auf hoechstens <see cref="NACHKOMMASTELLEN"/> Nachkommastellen gerundet, mit
        /// Punkt als Dezimaltrennzeichen, getrennt durch <see cref="TRENNZEICHEN"/>. Ein Text ueber
        /// <see cref="PROFIL_LAENGE_MAX"/> Zeichen wird abgewiesen, statt an der Laengenpruefung der
        /// Spalte zu scheitern.
        /// </summary>
        /// <exception cref="ArgumentException">Nicht genau 168 Werte, ein Wert nicht endlich oder der Text zu lang.</exception>
        public static string WochenprofilSchreiben(IReadOnlyList<double> werte)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            if (werte.Count != WOCHENWERTE)
                throw new ArgumentException("Ein Wochenprofil hat genau " + WOCHENWERTE.ToString(CultureInfo.InvariantCulture) +
                                            " Werte, uebergeben sind " + werte.Count.ToString(CultureInfo.InvariantCulture) + ".",
                                            nameof(werte));

            var teile = new string[WOCHENWERTE];
            for (int i = 0; i < WOCHENWERTE; i++)
            {
                double w = werte[i];
                if (!double.IsFinite(w))
                    throw new ArgumentException("Der Wert an Stelle " + (i + 1).ToString(CultureInfo.InvariantCulture) +
                                                " ist keine endliche Zahl.", nameof(werte));
                double gerundet = Math.Round(w, NACHKOMMASTELLEN, MidpointRounding.AwayFromZero);
                if (gerundet == 0) gerundet = 0;     // keine "-0" im Text
                teile[i] = gerundet.ToString("0.##", CultureInfo.InvariantCulture);
            }

            string text = string.Join(TRENNZEICHEN.ToString(), teile);
            if (text.Length > PROFIL_LAENGE_MAX)
                throw new ArgumentException("Das Wochenprofil braucht " + text.Length.ToString(CultureInfo.InvariantCulture) +
                                            " Zeichen, zulaessig sind " + PROFIL_LAENGE_MAX.ToString(CultureInfo.InvariantCulture) + ".",
                                            nameof(werte));
            return text;
        }
    }
}
