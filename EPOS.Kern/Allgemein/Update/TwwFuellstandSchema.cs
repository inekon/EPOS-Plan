using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SPEICHERGROESSE DER FUELLSTANDSLINIE - Schemaschritt des Zapfprofilgenerators (Auftrag F1,
    // Anwenderauftrag 29.09.2026: "die Volumina der einzelnen Verfahren als Wahl der
    // Fuellstandslinie"; Umsetzungskonzept Zapfprofilgenerator 4.7, N10 (k), N11 (d), N13 (o),
    // N36 (d)).
    //
    // WOZU. Ueber dem Wochenbild der Auslegung steht die Wahl "Speichergroesse der
    // Fuellstandslinie". Sie fuehrte die vier Groessen der Auslegung (Nenninhalt des Punkts,
    // Punkt, Nenninhalt des Bands, V_max). Dazu kommen die Volumina der vier Verfahren des
    // Verfahrensvergleichs - dieselbe Zahl, die die Tabelle zeigt: 5 profilbasiert, 6 DIN 4708,
    // 7 Faustwert mit Gleichzeitigkeit, 8 klassischer Faustwert (nachrichtlich, aber waehlbar).
    // Tab_TwwProjekt.Fuellstand_Bezug traegt heute CHECK ("Fuellstand_Bezug" IN (1,2,3,4)) -
    // SQLite kann einem CHECK keinen Wert nachtragen, also wird die Tabelle neu gebaut.
    //
    // EINE TABELLE, EINE KINDTABELLE. Tab_TwwProjekt ist eine PROJEKTtabelle (STRICT, ID_Projekt
    // auf Tab_Projekt mit ON DELETE CASCADE); Tab_TwwKonstruktorzeile zeigt auf sie. Zone,
    // Wohnungstyp und Messreihe fuehren keinen Fuellstandsbezug (geprueft am Schema).
    //
    // DAS REZEPT des Hauses fuer STRICT-Tabellen mit Kindern (Schritte 96, 100, 152, 153):
    // Fremdschluessel AUS vor der Transaktion (DataRepository.VorgangOhneFremdschluessel), die
    // alte Tabelle weicht unter legacy_alter_table auf einen Hilfsnamen aus, die neue entsteht
    // unter dem echten Namen aus dem GELTENDEN sqlite_master-Text mit der neuen Pruefklausel, die
    // Zeilen ziehen namentlich um, der AUTOINCREMENT-Stand reist mit, Indizes und Trigger stehen
    // wieder, und foreign_key_check haelt die Tabelle und jede Kindtabelle noch IN der
    // Transaktion. Der Neubau selbst ist TwwBezugsartSchema.Neubau - EIN Rezept, kein zweites.
    //
    // GRUNDSCHEMA = SCHRITT. Die Spaltendefinition des Grundschemas (TwwSchema.SpaltenT3) fuehrt
    // dieselbe Wertemenge TwwSchema.FUELLSTAND_BEZUG_WERTE. Der Neubau tauscht im geltenden Text
    // allein die Pruefklausel - eine neu angelegte Datenbank und eine nachgezogene tragen danach
    // Zeichen fuer Zeichen denselben CREATE-Text (Test TwwFuellstandSchemaTests).
    //
    // KEIN DML, ERGEBNISNEUTRAL. Keine Zeile aendert einen Wert; die Speicherauslegung ist
    // nachrichtlich und geht in keinen Simulationslauf ein. Tab_TwwProjekt des Referenzprojekts
    // 1045 bleibt zellgleich (Einfrierregel "gesaete Zapfprofil-Eingaben").
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// <b>Die Verfahrensvolumina als Bezug der Füllstandslinie</b> — Schemaschritt (Nummer
    /// <see cref="SCHRITT"/>): Neubau von <c>Tab_TwwProjekt</c> mit der Prüfklausel
    /// <see cref="CHECK_NEU"/>. EINE Quelle für Migrationsschritt,
    /// <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class TwwFuellstandSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand, Paketanhebung und Tests verweisen hierher. Wird er beim
        /// Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = KesselHeizgrenzeSchema.SCHRITT + 1;

        /// <summary>Die Tabelle mit dem Bezug des Füllstands.</summary>
        public const string TABELLE = TwwSchema.TAB_TWW_PROJEKT;

        /// <summary>Die Tabellen, die der Schritt neu baut — heute genau eine.</summary>
        public static readonly IReadOnlyList<string> TABELLEN = new[] { TABELLE };

        /// <summary>Die Prüfklausel vor dem Schritt: die vier Größen der Auslegung.</summary>
        public const string CHECK_ALT = "CHECK (\"" + TwwSchema.SPALTE_FUELLSTAND_BEZUG + "\" IN (1,2,3,4))";

        /// <summary>Die Prüfklausel nach dem Schritt — dieselbe Wertemenge wie Grundschema und Schreibweg.</summary>
        public const string CHECK_NEU = "CHECK (\"" + TwwSchema.SPALTE_FUELLSTAND_BEZUG + "\" IN ("
                                        + TwwSchema.FUELLSTAND_BEZUG_WERTE + "))";

        /// <summary>Der Zusatz des Hilfsnamens, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        private const string HILFSZUSATZ = "_vor_Fuellstand_Verfahren";

        /// <summary>
        /// <b>Der Zieltext einer Tabelle</b>: der GELTENDE <c>sqlite_master</c>-Text, in dem allein
        /// <see cref="CHECK_ALT"/> durch <see cref="CHECK_NEU"/> ersetzt ist. <c>null</c>, wenn nichts
        /// zu tun ist (die Tabelle trägt schon <see cref="CHECK_NEU"/> oder die Spalte gar nicht —
        /// ein Stand vor Schritt 124). Trägt sie die Spalte mit einer anderen Prüfklausel, bricht der
        /// Aufruf ab, statt eine falsche Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            if (bestand.Contains(CHECK_NEU, StringComparison.Ordinal)) return null;
            int stelle = bestand.IndexOf(CHECK_ALT, StringComparison.Ordinal);
            if (stelle < 0)
            {
                if (bestand.IndexOf("\"" + TwwSchema.SPALTE_FUELLSTAND_BEZUG + "\"", StringComparison.Ordinal) < 0) return null;
                throw new InvalidOperationException(
                    tabelle + " traegt die Spalte " + TwwSchema.SPALTE_FUELLSTAND_BEZUG +
                    " mit einer unbekannten Pruefklausel - der Schritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " baut die Tabelle deshalb NICHT um.");
            }
            if (bestand.IndexOf(CHECK_ALT, stelle + CHECK_ALT.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(tabelle + " traegt die Pruefklausel des Fuellstandsbezugs zweimal.");
            if (!bestand.TrimEnd().EndsWith(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(tabelle + " ist keine STRICT-Tabelle - der Schritt baut sie nicht um.");
            return bestand.Substring(0, stelle) + CHECK_NEU + bestand.Substring(stelle + CHECK_ALT.Length);
        }

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + HILFSZUSATZ;

        /// <summary>Muss die Tabelle noch neu gebaut werden? <c>false</c> ohne die Tabelle.</summary>
        public static bool UmbauNoetig(string tabelle)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("p1", tabelle));
            string bestand = o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
            return bestand != null && Zieltext(tabelle, bestand) != null;
        }

        /// <summary>Wie viele der Tabellen müssen noch neu gebaut werden?</summary>
        public static int Offen()
        {
            int offen = 0;
            foreach (string t in TABELLEN)
                if (UmbauNoetig(t)) offen++;
            return offen;
        }

        /// <summary>Steht der Schritt? Keine Tabelle offen.</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// <b>Führt den Schritt aus</b> — in EINEM Vorgang mit abgeschalteten Fremdschlüsseln, nach
        /// dem Neubaurezept des Hauses (<see cref="TwwBezugsartSchema.Neubau"/>). Scheitert etwas,
        /// rollt der Vorgang zurück und die Ausnahme geht an den Aufrufer (Migration, Werkzeug).
        /// <b>Wiederholbar</b>: Eine fertige Tabelle wird übersprungen. <b>Kein DML.</b> Liefert die
        /// Zahl der neu gebauten Tabellen.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        public static int Ausfuehren(IList<string> bericht)
        {
            int umgebaut = 0;
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach (string t in TABELLEN)
                    {
                        string bestand = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                       new DbParam("p1", t)));
                        if (bestand == null)
                        {
                            Notiere(bericht, t + ": fehlt - nichts umzubauen");
                            continue;
                        }
                        string ziel = Zieltext(t, bestand);
                        if (ziel == null)
                        {
                            Notiere(bericht, t + ": traegt die Pruefklausel schon");
                            continue;
                        }

                        long vorher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        int indizes = TwwBezugsartSchema.Neubau(v, t, ziel);
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        if (vorher != nachher)
                            throw new InvalidOperationException(t + ": " + vorher.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) vor dem Neubau, " + nachher.ToString(CultureInfo.InvariantCulture) + " danach.");
                        long verletzt = TwwBezugsartSchema.Verletzt(v, t);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Neubau meldet foreign_key_check fuer " + t +
                                " oder eine Kindtabelle " + verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                        umgebaut++;
                        Notiere(bericht, t + ": neu gebaut mit " + CHECK_NEU + "; Zeilen " +
                                         vorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                                         nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                         indizes.ToString(CultureInfo.InvariantCulture));
                    }
                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster aus Schritt 96).
                    try { if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            return umgebaut;
        }

        private static long Zahl(DbVorgang v, string sql, params DbParam[] parameter)
        {
            object wert = v.Skalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static void Notiere(IList<string> bericht, string zeile)
        {
            if (bericht != null) bericht.Add(zeile);
        }
    }
}
