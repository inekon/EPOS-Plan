using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wärmequelle je Wärmepumpe (Tab_Energieanlagen).
    ///
    /// Neue Spalten (werden bei Bedarf automatisch angelegt):
    ///   Prioritaet      - Einsatzreihenfolge der WPs in der Kaskade (1 = zuerst)
    ///   WQ_Typ          - Wärmequelle: Aussenluft | Konstant | Pufferspeicher | Profil | CSV | Erdreich
    ///   WQ_Temp         - konstante Quelltemperatur [°C] (Typ Konstant)
    ///   WQ_Monatswerte  - "t1;...;t12" Monats-Mitteltemperaturen [°C] (Typ Profil)
    ///   WQ_Wochenwerte  - "w1;...;w168" Tagesgang je Wochentag [K] (Typ Profil)
    ///   WQ_CSV          - Pfad zur CSV-Datei mit 8760 Stundenwerten (Typ CSV)
    ///   WQ_Quellsystem  - Erdreich: Kollektor | Sonde
    ///   WQ_Tiefe        - Erdreich: Verlegetiefe bzw. Länge je Sonde [m]
    ///   WQ_Flaeche      - Erdreich: Kollektorfläche [m²] (Auslegungsprüfung)
    ///   WQ_Anzahl       - Erdreich: Anzahl Sonden (Auslegungsprüfung)
    ///   WQ_Bodentyp     - Erdreich: Katalogschlüssel VDI 4640 Bl. 1 (ErdreichTemperatur)
    ///   WS_Typ          - Wärmesenke: Beides | Warmwasser | Heizung
    ///
    /// Für Luft-Wasser-Wärmepumpen ist die Quelle immer die Außenluft
    /// (Außentemperatur der Klimaregion). Für Sole-Wasser / Wasser-Wasser
    /// liefert Quelltemperatur() das Jahresprofil der Quelltemperatur, das in
    /// der Simulation anstelle der Außentemperatur in die Kennlinien eingeht.
    /// </summary>
    public static class WaermequelleClass
    {
        // Sämtliche Werte dieses Blocks sind Persistenzwerte. Sie werden seit Paket 9 / L0
        // zentral in DbWerte geführt; hier stehen nur noch Aliasse, damit die vorhandenen
        // Aufrufstellen unverändert bleiben und keine zweite Wahrheit entsteht.

        // Betriebsmodus der Wärmepumpe (Leistungssteuerung)
        public const string MODUS_LAUFZEIT = DbWerte.BM_TYP_LAUFZEIT;   // maximale Leistung, Speicher laden
        public const string MODUS_LEISTUNG = DbWerte.BM_TYP_LEISTUNG;   // nur den Bedarf decken (moduliert)
        public const string MODUS_PV = DbWerte.BM_TYP_PV;               // bei PV-Überschuss maximale Leistung

        // Wärmesenke: welchen Bedarfsanteil deckt der Erzeuger ab?
        public const string SENKE_BEIDES = DbWerte.WS_TYP_BEIDES;
        public const string SENKE_WARMWASSER = DbWerte.WS_TYP_WARMWASSER;
        public const string SENKE_HEIZUNG = DbWerte.WS_TYP_HEIZUNG;

        public const string TYP_AUSSENLUFT = DbWerte.WQ_TYP_AUSSENLUFT;
        public const string TYP_KONSTANT = DbWerte.WQ_TYP_KONSTANT;
        public const string TYP_PUFFER = DbWerte.WQ_TYP_PUFFERSPEICHER;
        public const string TYP_PROFIL = DbWerte.WQ_TYP_PROFIL;
        public const string TYP_CSV = DbWerte.WQ_TYP_CSV;
        public const string TYP_ERDREICH = DbWerte.WQ_TYP_ERDREICH;

        /// <summary>Keine gesonderte Wärmequelle (Etappe D5b) — der leere Spaltenwert.</summary>
        public const string TYP_OHNE = DbWerte.WQ_TYP_OHNE;

        /// <summary>
        /// PAKET Q1: Trägt diese Anlage KEINE gesonderte Wärmequelle
        /// (<see cref="TYP_OHNE"/> = <c>DbWerte.WQ_TYP_OHNE</c>)?
        ///
        /// <para><b>Warum eine Methode und nicht dreimal derselbe Vergleich.</b> Der
        /// Leerwert wurde bis Q1 an drei Stellen unterschiedlich geprüft:
        /// <see cref="Quelltemperatur"/> mit <c>string.IsNullOrEmpty</c>,
        /// <see cref="QuelleAnzeige"/> ebenso, <c>Warnkriterien.SoleOhneQuellePruefen</c>
        /// dagegen mit <c>Trim().Length &gt; 0</c>. Die Konstante
        /// <c>DbWerte.WQ_TYP_OHNE</c> kam an keiner der drei Stellen vor, obwohl sie
        /// genau dafür angelegt wurde (Drei-Schichten-Regel: kein Persistenzwert als
        /// Literal im Code). Diese Methode ist jetzt die eine Wahrheit.</para>
        ///
        /// <para><b>Verhaltensneutral</b> gegenüber allen drei Fassungen: Sie fasst
        /// NULL, Leerstring und reinen Weißraum zusammen. Für NULL und "" entscheiden
        /// alle drei ohnehin gleich; ein Wert aus reinem Weißraum lief bisher in
        /// <see cref="Quelltemperatur"/> durch den <c>switch</c> ohne Treffer und endete
        /// ebenfalls bei der Außentemperatur — dasselbe Ergebnis, jetzt auf demselben
        /// Weg. In der produktiven Datenbank kommt er nicht vor (gemessen 28.08.2026:
        /// 125 × NULL, 5 × "", 1 × Erdreich, 1 × Pufferspeicher).</para>
        /// </summary>
        public static bool OhneQuelle(string wqTyp)
        {
            return (wqTyp ?? "").Trim() == TYP_OHNE;
        }

        /// <summary>
        /// Größte plausible Verlegetiefe eines Erdkollektors [m]. Reale Kollektoren
        /// liegen bei 1…2 m; der Erdreichdialog begrenzt die Eingabe auf 10 m.
        /// Steht in WQ_Tiefe mehr, kann es nur eine Sondenlänge sein - siehe den
        /// Konsistenz-Check in Quelltemperatur().
        /// </summary>
        public const double MAX_KOLLEKTORTIEFE_M = 10.0;

        /// <summary>
        /// Anzeigetexte für die Auswahl im Dialog — lokalisiert (Paket 9 / L6).
        ///
        /// ACHTUNG: TypAnzeige und TypWerte sind indexgekoppelt
        /// (Form_Simulation_Config: WaermequelleAuswahlAnzeigen / WqCombo_SelectedIndexChanged).
        /// Neue Wärmequellen deshalb immer ANHÄNGEN, nie einfügen oder umsortieren -
        /// sonst zeigen bestehende Projekte auf die falsche Quelle (Konzept 5.3).
        /// Der STEUERWERT ist der Index bzw. <see cref="TypWerte"/>; der Text hier ist
        /// reine Anzeige (Drei-Schichten-Regel).
        ///
        /// Bewusst eine Eigenschaft statt eines <c>static readonly</c>-Feldes: Ein Feld
        /// würde beim ersten Typzugriff eingefroren und bliebe bei einem Sprachwechsel
        /// im Prozess (Sprachgleichheitsprobe der Referenzlauf-Suite) auf der alten
        /// Sprache stehen. Je Aufruf ein NEUES Array, damit ein Aufrufer die Liste nicht
        /// versehentlich für alle verändert.
        /// </summary>
        public static string[] TypAnzeige
        {
            get
            {
                return new[]
                {
                    MyResource.Resource.SIMQ_TYP_AUSSENLUFT,
                    MyResource.Resource.SIMQ_TYP_KONSTANTE_TEMPERATUR,
                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                    MyResource.Resource.SIMQ_TYP_QUELLPROFIL,
                    MyResource.Resource.SIMQ_TYP_CSV_DATEI,
                    MyResource.Resource.SIMQ_TYP_ERDREICH
                };
            }
        }

        public static readonly string[] TypWerte =
        {
            TYP_AUSSENLUFT, TYP_KONSTANT, TYP_PUFFER, TYP_PROFIL, TYP_CSV, TYP_ERDREICH
        };

        // --- Freischaltung der Quellenwahl je Erzeugerart (Etappe D5b) ----------------
        //
        // Konzept_KonfigUI_Hydraulik, Abschnitt 4, letzte Zeile: „Quelle Pufferspeicher
        // auch für Heizkessel zulässig — WQ_Typ-Freischaltung je ID_Type (WP: alle Typen;
        // Kessel: nur Puffer); Erdsonde/Erdreich bleibt WP-exklusiv (Anforderung 5)."
        //
        // Die ENGINE zieht dieselbe Grenze seit der D5a-Nacharbeit (Befund E-K2-2):
        // SimulationControl.QuellbezuegeAufbauen nimmt nur TYP_WP und TYP_KESSEL auf, weil
        // nur deren Module eine Ebenenmaske auswerten; Solarthermie und BHKW bekommen eine
        // Warnung und bleiben auf Ebene 0. Was die Engine ohnehin abweist, darf die
        // Oberfläche gar nicht erst anbieten - das ist der Auftrag dieser beiden Methoden.

        /// <summary>
        /// Darf diese Erzeugerart überhaupt eine Wärmequelle wählen? Wärmepumpe (alle
        /// Typen) und Heizkessel (nur Pufferspeicher) - sonst nein.
        /// </summary>
        public static bool QuellenwahlMoeglich(int idType)
        {
            return idType == ProjektPuffer.TYP_WP || idType == ProjektPuffer.TYP_KESSEL;
        }

        /// <summary>
        /// Die STEUERWERTE der Wärmequellen-Auswahl für eine Erzeugerart; nie
        /// <c>null</c>, aber leer für Arten ohne Quellenwahl.
        ///
        /// <b>Indexkopplung.</b> Wie bei <see cref="TypWerte"/>/<see cref="TypAnzeige"/>
        /// gehören Steuerwert und Anzeigetext über den INDEX zusammen; der Aufrufer muss
        /// beide Listen mit demselben <paramref name="idType"/> holen (siehe
        /// <c>Form_Simulation_Config.WaermequelleAuswahlAnzeigen</c>, die sich die
        /// Werteliste für die Auswertung des Ereignisses merkt).
        /// </summary>
        public static string[] TypWerteFuer(int idType)
        {
            if (idType == ProjektPuffer.TYP_WP) return (string[])TypWerte.Clone();

            // Heizkessel: die Eintrittstemperatur kommt aus dem Systemrücklauf ODER aus
            // einem Quellpuffer (Kaskade). Erdreich, Quellprofil, CSV und konstante
            // Temperatur sind Modelle der VERDAMPFERseite und haben am Brenner keine
            // Entsprechung - der Rechenkern liest sie dort auch nirgends.
            if (idType == ProjektPuffer.TYP_KESSEL) return new[] { TYP_OHNE, TYP_PUFFER };

            return new string[0];
        }

        /// <summary>Die ANZEIGETEXTE zu <see cref="TypWerteFuer"/>, indexgleich.</summary>
        public static string[] TypAnzeigeFuer(int idType)
        {
            if (idType == ProjektPuffer.TYP_WP) return TypAnzeige;

            if (idType == ProjektPuffer.TYP_KESSEL)
                return new[]
                {
                    MyResource.Resource.SIMQ_QUELLE_SYSTEMRUECKLAUF,
                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER
                };

            return new string[0];
        }

        // --- Anzeige der Wärmequelle (Etappe D4: EINE Quelle für Liste und Schema) ----
        //
        // Die beiden folgenden Methoden standen bis D5b als PRIVATE Methoden in
        // Form_Simulation_Config.Uebersicht.cs (WaermequelleAnzeige / ErdreichAnzeige).
        // Mit der Schema-Ansicht braucht denselben Text ein zweiter Aufrufer; verschoben
        // statt kopiert, damit Karte und Schema nicht auseinanderlaufen können. Der
        // Formular-Code ruft sie unverändert auf und reicht nur seine Felder durch.

        /// <summary>
        /// Kompakte Anzeige der Wärmequelle EINER WÄRMEPUMPE.
        ///
        /// Bei Luft-Wasser (und bei fehlender Bauart) ist die Quelle immer die Außenluft —
        /// die Spalte <c>WQ_Typ</c> wird dort gar nicht ausgewertet, weder hier noch in der
        /// Engine.
        /// </summary>
        /// <param name="idProjekt">Projekt, gegen das ein Quellpuffer aufgelöst wird</param>
        /// <param name="idAnlage">Tab_Energieanlagen.ID</param>
        /// <param name="wpTyp">Tab_WP.Typ (Bauart)</param>
        /// <param name="wqTyp">WQ_Typ</param>
        /// <param name="wqTemp">WQ_Temp [°C]</param>
        public static string QuelleAnzeige(int idProjekt, int idAnlage,
                                           string wpTyp, string wqTyp, double wqTemp)
        {
            // Luft-Wasser-WP: Quelle ist immer die Außenluft (Klimadaten)
            if (string.IsNullOrEmpty(wpTyp) || wpTyp == DbWerte.WP_BAUART_LUFT_WASSER)
                return MyResource.Resource.SIMQ_QUELLE_AUSSENLUFT;

            switch (wqTyp)
            {
                case TYP_KONSTANT:
                    return string.Format(MyResource.Resource.SIMQ_QUELLE_KONSTANT, wqTemp.ToString("0.#"));

                case TYP_PUFFER:
                    {
                        // E0 / ETAPPE D5b: Aufgelöst wird über die EINE Rangfolge, die auch
                        // Engine und Erzeugerkarte benutzen - Fremdschlüssel WQ_ID_Puffer,
                        // dann der Bezeichner in der Projektkopie.
                        int idPuffer = WaermesenkeClass.QuellPufferDerAnlage(idProjekt, idAnlage);
                        string name = idPuffer > 0 ? WaermesenkeClass.PufferName(idPuffer) : "";

                        // ANZEIGE-Rückfall (und nur der): Löst sich nichts auf, steht der
                        // Alttext aus WQ_Puffer da, damit der Anwender sieht, worauf die
                        // Anlage einmal zeigte.
                        if (name.Length == 0) name = WertLesen(idAnlage, "WQ_Puffer") as string;

                        return string.IsNullOrEmpty(name)
                            ? MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER
                            : string.Format(MyResource.Resource.SIMQ_QUELLE_PUFFER_NAME, name);
                    }

                case TYP_PROFIL: return MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL;
                case TYP_CSV: return MyResource.Resource.SIMQ_QUELLE_CSVPROFIL;
                case TYP_ERDREICH: return ErdreichAnzeige(idAnlage);

                default:
                    // Sole-/Wasser-Wasser-WP ohne konfigurierte Quelle (WQ_Typ leer):
                    // "Aussenluft" waere hier ein Kategorienfehler in der Anzeige — die
                    // Bauart kann physisch keine Luftquelle haben; gerechnet wird bis
                    // Paket B1 zwar ersatzweise mit der Aussenluft, aber der Chip sagt
                    // jetzt ehrlich, dass die Wahl aussteht (Nutzerbefund 27.08.2026,
                    // Warnkriterium QUELLE_FEHLT meldet dasselbe auf Karte und im
                    // Laufprotokoll). Ein UNBEKANNTER Altwert bleibt dagegen bei der
                    // bisherigen Aussenluft-Anzeige.
                    // PAKET Q1: derselbe Leerwert-Test wie in Engine und Warnkatalog.
                    if (OhneQuelle(wqTyp))
                        return MyResource.Resource.SIMQ_QUELLE_FEHLT;
                    return MyResource.Resource.SIMQ_QUELLE_AUSSENLUFT;
            }
        }

        /// <summary>
        /// Kompakte Anzeige der Wärmequelle Erdreich, z. B. „Erdreich Kollektor 1,5 m"
        /// oder „Erdsonde 2×90 m".
        /// </summary>
        public static string ErdreichAnzeige(int idAnlage)
        {
            string quellsystem = WertLesen(idAnlage, "WQ_Quellsystem") as string;
            object oTiefe = WertLesen(idAnlage, "WQ_Tiefe");
            double tiefe = oTiefe != null ? Convert.ToDouble(oTiefe) : 0;

            if (string.Equals(quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                              StringComparison.OrdinalIgnoreCase))
            {
                object oAnzahl = WertLesen(idAnlage, "WQ_Anzahl");
                int anzahl = oAnzahl != null ? Convert.ToInt32(oAnzahl) : 0;
                if (anzahl < 1) anzahl = 1;
                return string.Format(MyResource.Resource.SIMQ_ERDSONDE_ANZEIGE,
                                     anzahl, tiefe.ToString("0.#"));
            }

            if (tiefe <= 0) tiefe = ErdreichTemperatur.TIEFE_DEFAULT;
            return string.Format(MyResource.Resource.SIMQ_ERDKOLLEKTOR_ANZEIGE, tiefe.ToString("0.#"));
        }

        /// <summary>
        /// Hinweistext zum CSV-Format (wird beim Einlesen angezeigt) — lokalisiert
        /// (Paket 9 / L6).
        ///
        /// Aus <c>const</c> wurde eine Eigenschaft: Eine Konstante kann keine Ressource
        /// referenzieren (Konzept 13.6 nennt genau diesen Fall), und ein
        /// <c>static readonly</c>-Feld würde die Sprache beim ersten Typzugriff
        /// einfrieren. Die Aufrufstellen bleiben unverändert.
        /// </summary>
        public static string CSV_FORMAT_HINWEIS
        {
            get { return MyResource.Resource.SIMQ_CSV_FORMAT_HINWEIS; }
        }

        private static bool _schemaGeprueft = false;

        /// <summary>
        /// Rückfallebene der Schema-Ausrollung (ADR-001): legt die benötigten Spalten an,
        /// falls sie fehlen. Wird nur einmal pro Programmlauf tatsächlich geprüft.
        ///
        /// Der reguläre Weg ist die versionierte <see cref="SchemaMigration"/> beim
        /// Programmstart. Diese Methode bleibt bestehen, damit die Konfiguration und der
        /// Simulationsstart auch dann tragfähig sind, wenn die Migration (noch) nicht
        /// gelaufen ist. Sie iteriert über DENSELBEN Spaltenkatalog
        /// (<see cref="SchemaKatalog.Alle"/>) - es gibt keine zweite Spaltenliste mehr.
        ///
        /// Verhalten unverändert: still (keine Dialoge, Fehler nur auf die Konsole),
        /// idempotent, ohne Rückgabewert.
        ///
        /// WICHTIG: bewusst über eine eigene, stille OleDb-Verbindung - die
        /// DataRepository-Methoden zeigen bei Fehlern MessageBoxen an und liefern
        /// leere Ergebnisse statt null, damit ließe sich das Fehlen einer Spalte
        /// nicht sauber erkennen.
        ///
        /// PROTOKOLLKANAL-NACHZUG, KATEGORIE (c): Diese Methode und ihre Helfer
        /// (<see cref="TabellenSchemaLesen"/>, die beiden <c>SpalteSicherstellen</c>,
        /// <see cref="TabelleStill"/>, die beiden <c>WertSchreiben</c>) melden
        /// ausdrücklich WEITER NUR auf die Konsole. Zwei Gründe: Es sind
        /// Schema-/Infrastrukturdiagnosen ohne Anwenderaussage, UND sie laufen auch
        /// AUSSERHALB eines Simulationslaufs (Form_Simulation_Config,
        /// KonfigurationCtrl, die Senkendialoge). Ein Kanaleintrag von dort landete im
        /// Protokoll des zuletzt gelaufenen Laufs — sichtbar falsch statt still richtig.
        /// </summary>
        public static void SchemaSicherstellen()
        {
            if (_schemaGeprueft) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    string aktuelleTabelle = null;
                    DataTable dt = null;

                    // Der Katalog ist nach Tabellen gruppiert abgelegt; das Schema wird
                    // deshalb je Tabelle nur einmal gelesen.
                    foreach (SchemaSpalte s in SchemaKatalog.Alle)
                    {
                        if (!string.Equals(aktuelleTabelle, s.Tabelle, StringComparison.OrdinalIgnoreCase))
                        {
                            aktuelleTabelle = s.Tabelle;
                            dt = TabellenSchemaLesen(conn, s.Tabelle);
                        }

                        if (dt == null) continue;          // Tabelle nicht lesbar - still übergehen
                        SpalteSicherstellen(conn, dt, s.Tabelle, s.Name, s.TypDefinition);
                    }
                }

                _schemaGeprueft = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SchemaSicherstellen fehlgeschlagen: " + ex.Message);
            }
        }

        /// <summary>
        /// Spaltenliste einer Tabelle; null, wenn die Tabelle nicht lesbar ist.
        /// </summary>
        private static DataTable TabellenSchemaLesen(OleDbConnection conn, string tabelle)
        {
            try
            {
                DataTable dt = new DataTable();
                using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + tabelle + "]", conn))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.FillSchema(dt, SchemaType.Source);
                }
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema von " + tabelle + " nicht lesbar: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Legt eine Spalte in einer beliebigen Tabelle an, falls sie fehlt
        /// (still, ohne Fehlerdialoge).
        /// </summary>
        public static void SpalteSicherstellen(string tabelle, string spalte, string typDefinition)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    DataTable dt = new DataTable();
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + tabelle + "]", conn))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.FillSchema(dt, SchemaType.Source);
                    }

                    if (dt.Columns.Contains(spalte)) return;

                    using (OleDbCommand cmd = new OleDbCommand(
                        "ALTER TABLE [" + tabelle + "] ADD COLUMN [" + spalte + "] " + typDefinition, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Spalte " + tabelle + "." + spalte + " konnte nicht angelegt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Legt eine Spalte auf einer bereits offenen Verbindung an. Die Tabelle wird
        /// übergeben - die frühere Fassung hatte Tab_Energieanlagen hartkodiert und war
        /// damit für Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen unbrauchbar
        /// (Konzept 5.6).
        /// </summary>
        private static void SpalteSicherstellen(OleDbConnection conn, DataTable schema,
                                                string tabelle, string spalte, string typDefinition)
        {
            if (schema.Columns.Contains(spalte)) return; // Spalte existiert bereits

            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "ALTER TABLE [" + tabelle + "] ADD COLUMN [" + spalte + "] " + typDefinition, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Spalte " + tabelle + "." + spalte + " konnte nicht angelegt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Liest einen Einzelwert aus einer beliebigen Tabelle - still, ohne
        /// Fehlerdialoge (fehlende Spalte/Datensatz liefert null). Bewusst mit
        /// eigener Verbindung, da DataRepository bei Fehlern MessageBoxen zeigt.
        /// </summary>
        public static object WertLesenStill(string tabelle, string spalte, int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "SELECT [" + spalte + "] FROM [" + tabelle + "] WHERE ID = " + id, conn))
                    {
                        object v = cmd.ExecuteScalar();
                        return (v == DBNull.Value) ? null : v;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Beliebige skalare Abfrage - still, ohne Fehlerdialoge (Etappe 4). Eine noch
        /// nicht migrierte Datenbank liefert hier null statt einer MessageBox mitten im
        /// Engine-Lauf; genau dafür ist der Rückfallweg da.
        /// </summary>
        private static object SkalarStill(string sql)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        object v = cmd.ExecuteScalar();
                        return (v == DBNull.Value) ? null : v;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tabellenabfrage ohne Dialog (Paket 2, Konzept 13.4) - Gegenstück zu
        /// <see cref="SkalarStill"/> für den Quellspeicher-Aufbau im Engine-Pfad.
        /// </summary>
        private static DataTable TabelleStill(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                DataTable dt = new DataTable();
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        using (OleDbDataAdapter ad = new OleDbDataAdapter(cmd))
                        {
                            ad.Fill(dt);
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stille Tabellenabfrage fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Ganzzahl aus einem Datenbankwert; 0 bei null, DBNull oder Unfug.</summary>
        private static int ZahlOderNull(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o); }
            catch { return 0; }
        }

        /// <summary>
        /// Wie <see cref="WertLesen(int,string)"/>, aber STILL (Paket 2, Konzept 13.4).
        ///
        /// <see cref="WertLesen(int,string)"/> geht über <c>DataRepository.ExecuteScalar</c>
        /// und kann im Fehlerfall eine MessageBox zeigen — mitten im Rechenlauf ist das ein
        /// hängender Referenzlauf. Alles, was aus dem ENGINE-Pfad heraus liest
        /// (<see cref="Quelltemperatur"/>, <see cref="Quellspeicher"/>), benutzt deshalb
        /// diese Fassung. Der Rückgabewert ist identisch: der Spaltenwert oder <c>null</c>.
        ///
        /// <see cref="WertLesen(int,string)"/> bleibt für die Oberfläche bestehen — dort ist
        /// ein Fehlerdialog erwünscht und der breite Aufrufkreis unangetastet.
        /// </summary>
        public static object WertLesenStill(int idEnergieanlage, string spalte)
        {
            return WertLesenStill("Tab_Energieanlagen", spalte, idEnergieanlage);
        }

        /// <summary>
        /// Liest einen Wert (WQ_*, Prioritaet) einer Energieanlage; null wenn nicht vorhanden.
        /// </summary>
        public static object WertLesen(int idEnergieanlage, string spalte)
        {
            try
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT [" + spalte + "] FROM Tab_Energieanlagen WHERE ID=" + idEnergieanlage);
                return (v == DBNull.Value) ? null : v;
            }
            catch { return null; }
        }

        /// <summary>
        /// Schreibt einen Wert (WQ_*, Prioritaet) einer Energieanlage.
        ///
        /// Der Typ des Parameters wird aus dem WERT abgeleitet. Das trägt für alles, was
        /// tatsächlich einen Wert hat — für <see cref="DBNull"/> nicht: dort gibt es
        /// nichts abzuleiten. Wer NULL schreibt, nimmt die Überladung mit
        /// ausdrücklichem <see cref="OleDbType"/>.
        /// </summary>
        public static bool WertSchreiben(int idEnergieanlage, string spalte, object wert)
        {
            try
            {
                string sql = "UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = " + idEnergieanlage;
                return DataRepository.ExecuteSQL(sql,
                    new OleDbParameter("@w", wert ?? (object)DBNull.Value));
            }
            catch (Exception ex)
            {
                Console.WriteLine("WertSchreiben " + spalte + " fehlgeschlagen: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Wie <see cref="WertSchreiben(int,string,object)"/>, aber mit AUSDRÜCKLICHEM
        /// Spaltentyp — die Regel, nach der auch <c>StilleDb.Par</c> und
        /// <c>ProjektPuffer.Par</c> gebaut sind.
        ///
        /// Aus <see cref="DBNull"/> allein leitet der OLE-DB-Provider keinen Typ ab; er
        /// rät. Bei den drei Fremdschlüsselspalten der Wärmesenke
        /// (<c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c>) und bei <c>WS_Ziel2</c> ist NULL
        /// aber der Normalfall — „keine Zweitsenke" heißt genau das, und 0 wäre wegen der
        /// erzwungenen Beziehung aus Schritt 4 der SchemaMigration nicht einmal erlaubt.
        /// Mit dem ausdrücklichen Typ hängt das Ergebnis nicht mehr daran, wie ACE rät.
        /// </summary>
        public static bool WertSchreiben(int idEnergieanlage, string spalte,
                                         OleDbType typ, object wert)
        {
            try
            {
                string sql = "UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = " + idEnergieanlage;
                return DataRepository.ExecuteSQL(sql,
                    new OleDbParameter("@w", typ) { Value = wert ?? (object)DBNull.Value });
            }
            catch (Exception ex)
            {
                Console.WriteLine("WertSchreiben " + spalte + " fehlgeschlagen: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Liefert das Jahresprofil (8760 Stundenwerte) der Quelltemperatur für eine
        /// Wärmepumpe. Fallback ist immer die Außentemperatur (aussentemp).
        /// </summary>
        /// <param name="idEnergieanlage">Tab_Energieanlagen.ID der WP</param>
        /// <param name="idProjekt">
        /// Projekt-ID. SEIT PAKET A1 UNGENUTZT: Sie diente den beiden entfallenen
        /// Rückfallstufen über die Alt-Zuordnung <c>Z_ProjektPufferSp</c>. Der Parameter
        /// bleibt, um die Signatur dieser öffentlichen Methode nicht zu ändern.
        /// </param>
        /// <param name="wpTyp">WP-Typ aus Tab_WP ("Luft-Wasser", "Sole-Wasser", "Wasser-Wasser")</param>
        /// <param name="aussentemp">Außentemperatur der Klimaregion (8760 Werte)</param>
        public static float[] Quelltemperatur(int idEnergieanlage, int idProjekt, string wpTyp, float[] aussentemp)
        {
            // Luft-Wasser: immer Außenluft
            if (string.IsNullOrEmpty(wpTyp) || wpTyp == DbWerte.WP_BAUART_LUFT_WASSER) return aussentemp;

            // Paket 2 / Konzept 13.4: im Engine-Pfad wird STILL gelesen - ein Fehlerdialog
            // aus DataRepository heraus würde den Rechenlauf anhalten.
            string typ = WertLesenStill(idEnergieanlage, "WQ_Typ") as string;
            // PAKET Q1: derselbe Leerwert-Test wie im Warnkatalog und in der Anzeige
            // (siehe OhneQuelle) - verhaltensgleich zum bisherigen IsNullOrEmpty.
            if (OhneQuelle(typ) || typ == TYP_AUSSENLUFT) return aussentemp;

            try
            {
                switch (typ)
                {
                    case TYP_KONSTANT:
                        {
                            object v = WertLesenStill(idEnergieanlage, "WQ_Temp");
                            if (v == null) return aussentemp;
                            return KonstantesProfil(Convert.ToSingle(v));
                        }

                    case TYP_PUFFER:
                        {
                            // Temperatur des als Wärmequelle gewählten Pufferspeichers
                            object v = WertLesenStill(idEnergieanlage, "WQ_Temp");
                            if (v != null) return KonstantesProfil(Convert.ToSingle(v));

                            // Fallback: mittlere Temperatur (Vorlauf + Rücklauf) / 2 des
                            // als Quelle GEWÄHLTEN Puffers (WQ_ID_Puffer, von
                            // Migrationsregel R3 aus dem Bezeichner aufgelöst).
                            //
                            // PAKET A1: Darunter standen zwei weitere Stufen, die beide
                            // die Alt-Zuordnung Z_ProjektPufferSp befragten (der Puffer
                            // der Wärmepumpen-Zuordnung, danach das Temperaturpaar der
                            // Zuordnungszeile selbst). Beide sind mit der Stilllegung der
                            // Zuordnung ersatzlos entfallen; ohne gewählten Quellpuffer
                            // gilt wie bisher die Außentemperatur.
                            int vorlauf, ruecklauf;

                            int idQuellPuffer = ZahlOderNull(WertLesenStill(idEnergieanlage, "WQ_ID_Puffer"));
                            if (PufferSpCtrl.TemperaturenLesen(idQuellPuffer, out vorlauf, out ruecklauf))
                                return KonstantesProfil((vorlauf + ruecklauf) / 2f);

                            return aussentemp;
                        }

                    case TYP_PROFIL:
                        {
                            // --- PAKET Q1, Stufe 1: das gewählte QUELLPROFIL ------------
                            // Schlüsselkopplung über WQ_ID_Quellprofil (Schritt 54,
                            // Konzept 8.1 Punkt 4). Sie trägt alle drei Betriebsarten -
                            // 12 Monats-, 365 Tages- oder 8760 Stundenwerte - und kachelt
                            // sie in QuellprofilCtrl.Jahresprofil, bei der Tagesvariante
                            // ausdrücklich KALENDERUNABHÄNGIG.
                            int idProfil = ZahlOderNull(WertLesenStill(idEnergieanlage, "WQ_ID_Quellprofil"));
                            if (idProfil > 0)
                            {
                                float[] ausProfil = QuellprofilCtrl.Jahresprofil(idProfil);
                                if (ausProfil != null) return ausProfil;

                                // Protokollkanal: WARNUNG - die Anlage zeigt auf ein
                                // Profil, das fehlt oder unvollständig ist; gerechnet
                                // wird danach mit einer Ersatzannahme voller
                                // Ergebniswirkung auf die JAZ.
                                SimulationProtokoll.Aktuell.WarnungEinmal(
                                    "quellprofil-unlesbar-" + idEnergieanlage,
                                    string.Format(
                                        MyResource.Resource.SIMENG_QUELLPROFIL_UNLESBAR,
                                        idProfil, idEnergieanlage));
                            }

                            // --- Stufe 2: LESE-ALTLAST der delimitierten Zeichenketten ---
                            // WQ_Monatswerte/WQ_Wochenwerte bleiben lesbar (Konzept 15,
                            // Muster WQ_Puffer -> WQ_ID_Puffer). Schritt 54 übernimmt sie
                            // NICHT automatisch; sie verschwinden erst, wenn der Anwender
                            // im Dialog ein Quellprofil speichert.
                            string monat = WertLesenStill(idEnergieanlage, "WQ_Monatswerte") as string;
                            string woche = WertLesenStill(idEnergieanlage, "WQ_Wochenwerte") as string;
                            float[] profil = ProfilAusMonatsUndWochenwerten(monat, woche);
                            return profil ?? aussentemp;
                        }

                    case TYP_CSV:
                        {
                            string pfad = WertLesenStill(idEnergieanlage, "WQ_CSV") as string;
                            float[] profil = ProfilAusCsv(pfad);
                            return profil ?? aussentemp;
                        }

                    case TYP_ERDREICH:
                        {
                            // Erdreichmodell nach VDI 4640 Blatt 1 (Konzept 4.5/13.1).
                            // Kein eigener DB-Zugriff auf die Klimadaten: der
                            // 8760er-Außentemperaturvektor ist bereits durchgereicht.
                            string quellsystem = WertLesenStill(idEnergieanlage, "WQ_Quellsystem") as string;
                            string bodentyp = WertLesenStill(idEnergieanlage, "WQ_Bodentyp") as string;

                            object oTiefe = WertLesenStill(idEnergieanlage, "WQ_Tiefe");
                            double tiefe = oTiefe != null ? Convert.ToDouble(oTiefe) : 0;

                            // Konsistenz-Check gegen teilgeschriebene Feldsätze:
                            // Der Dialog schreibt WQ_Quellsystem, WQ_Tiefe, WQ_Flaeche,
                            // WQ_Anzahl und WQ_Bodentyp als fünf einzelne UPDATEs ohne
                            // Transaktion; WertSchreiben schluckt Fehler. Bleibt dabei
                            // ausgerechnet WQ_Quellsystem auf "Kollektor" stehen, während
                            // WQ_Tiefe schon die Sondenlänge trägt, würde die Kusuda-
                            // Dämpfung exp(−90/2,72) ≈ 0 die Quelle stillschweigend auf
                            // eine Konstante von T_m zusammenfallen lassen. Reale
                            // Verlegetiefen eines Kollektors liegen bei 1…2 m, der Dialog
                            // begrenzt sie auf 10 m - alles darüber ist eine Sondenlänge.
                            bool alsSonde = string.Equals(quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                                                          StringComparison.OrdinalIgnoreCase);
                            if (!alsSonde && tiefe > MAX_KOLLEKTORTIEFE_M)
                            {
                                // Protokollkanal-Nachzug: WARNUNG - die Anlage wird
                                // ANDERS gerechnet, als sie konfiguriert ist. Je Anlage
                                // einmal (Quelltemperatur läuft je Modul einmal).
                                SimulationProtokoll.Aktuell.WarnungEinmal(
                                                  "quelle-tiefe-unstimmig-" + idEnergieanlage,
                                                  "Quelltemperatur: WQ_Quellsystem = '" + (quellsystem ?? "") +
                                                  "' mit WQ_Tiefe = " + tiefe + " m ist unstimmig - " +
                                                  "die Anlage wird als Erdsonde gerechnet.");
                                alsSonde = true;
                            }

                            if (alsSonde)
                            {
                                // Erdsonde: konstante Quelltemperatur. Fehlt die Länge,
                                // entfällt der geothermische Anteil (max(0, …) = 0).
                                return ErdreichTemperatur.JahresprofilSonde(aussentemp, tiefe);
                            }

                            // Fallback: Kollektor mit Vorgabetiefe 1,5 m und Sand feucht
                            // (ErdreichTemperatur setzt beides bei fehlenden Werten selbst).
                            return ErdreichTemperatur.JahresprofilKollektor(aussentemp, tiefe, bodentyp);
                        }
                }
            }
            catch (Exception ex)
            {
                // Protokollkanal-Nachzug: WARNUNG - Rückfall auf die Außentemperatur,
                // also eine Ersatzannahme mit voller Ergebniswirkung auf die JAZ.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "quelltemperatur-fehlgeschlagen-" + idEnergieanlage,
                    "Quelltemperatur (" + typ + ") konnte nicht ermittelt werden: " + ex.Message +
                    " - es gilt die Außentemperatur.");
            }

            return aussentemp;
        }

        /// <summary>
        /// Liefert den Quell-Pufferspeicher einer Wärmepumpe (Wärmequelle
        /// "Pufferspeicher") als einsatzbereites Speichermodell - oder null,
        /// wenn keiner konfiguriert ist bzw. die Quelle als unbegrenzt gilt.
        ///
        /// Der Speicher wird in der Simulation je Stunde um die Verdampferwärme
        /// entladen (Wärmeproduktion - Stromaufnahme) und durch die eingestellte
        /// Regeneration nachgeladen.
        ///
        /// AUFLÖSUNG DES SPEICHERS seit Paket 4 (Etappe 4a) - Konzept 7, Zeile
        /// <c>WaermequelleClass</c>: „<c>WQ_ID_Puffer</c> statt Bezeichner, Projekt-
        /// statt Stammtabelle". Die Kette hat drei Stufen:
        ///
        ///   1. <c>WQ_ID_Puffer</c> -> Zeile in <c>Tab_Pufferspeicher</c> (Projektkopie).
        ///      Migrationsregel R3 hat die Spalte aus dem Bezeichner aufgelöst.
        ///   2. <c>WQ_Puffer</c> (Bezeichner) in der PROJEKTKOPIE, kleinste ID -
        ///      deterministisch wie <c>WaermesenkeClass.QuellPufferDerAnlage</c>.
        ///   3. <c>WQ_Puffer</c> im KATALOG <c>_STAMM</c> - der bisherige Weg, jetzt nur
        ///      noch Rückfallebene für Altbestand ohne Projektkopie.
        ///
        /// ERWARTETE UND EINZIGE ERGEBNISWIRKUNG: <c>ID_Pufferspeicher</c> zeigt nicht
        /// mehr auf die Katalogzeile, sondern auf die Projektkopie. Der Wert landet in
        /// <c>Tab_ErgebnisPufferspeicher.ID_Pufferspeicher</c> und im Serienschlüssel der
        /// Anzeigen. Alle RECHENGRÖSSEN bleiben gleich, weil die Projektkopie dieselben
        /// Katalogwerte trägt (Volumen, Bereitschaftsverluste) und Spreizung wie
        /// Regeneration ohnehin an der ANLAGE hängen, nicht am Speicher.
        ///
        /// Warum die Umstellung fachlich nötig ist: Die Katalogzeile ist projektweit
        /// geteilt. Zwei Projekte mit demselben Speichertyp zeigten bisher auf dieselbe
        /// ID - Ergebniszeilen und Anzeigen konnten sie nicht auseinanderhalten, und der
        /// Kurzschluss-Test „derselbe Speicher als Quelle UND Senke" (Konzept 4.6) hätte
        /// nie greifen können, weil die Senke immer eine Projekt-ID trägt.
        /// </summary>
        public static SimulationPufferspeicher Quellspeicher(int idEnergieanlage, string wpTyp)
        {
            // Luft-Wasser-WP entnimmt keine Wärme aus einem Speicher
            if (string.IsNullOrEmpty(wpTyp) || wpTyp == DbWerte.WP_BAUART_LUFT_WASSER) return null;

            // Paket 2 / Konzept 13.4: Stufe 1 des Quellspeicher-Zugriffs läuft still.
            // Vorher hing hier der komplette Aufbau an DataRepository - eine fehlende
            // Spalte oder eine gesperrte Datei hätte mitten im Rechenlauf eine MessageBox
            // gezeigt. Gelesen werden dieselben Werte, das Ergebnis ist unverändert.
            string typ = WertLesenStill(idEnergieanlage, "WQ_Typ") as string;
            if (typ != TYP_PUFFER) return null;

            // "unbegrenzt verfügbar" -> nur die Temperatur wirkt, keine Bilanz
            object unbegrenzt = WertLesenStill(idEnergieanlage, "WQ_Unbegrenzt");
            if (unbegrenzt != null && Convert.ToBoolean(unbegrenzt)) return null;

            try
            {
                DataRow zeile = QuellspeicherZeile(idEnergieanlage);
                if (zeile == null) return null;

                int idSpeicher = zeile["ID"] != DBNull.Value ? Convert.ToInt32(zeile["ID"]) : 0;
                string bezeichner = zeile["Bezeichner"] != DBNull.Value
                    ? Convert.ToString(zeile["Bezeichner"]) : "";
                double volumen = zeile["Gesamtvolumen"] != DBNull.Value
                    ? Convert.ToDouble(zeile["Gesamtvolumen"]) : 0;
                double verluste = zeile["Bereitschaftsverluste"] != DBNull.Value
                    ? Convert.ToDouble(zeile["Bereitschaftsverluste"]) : 0;
                if (volumen <= 0) return null;

                object oSpreizung = WertLesenStill(idEnergieanlage, "WQ_Spreizung");
                double spreizung = oSpreizung != null ? Convert.ToDouble(oSpreizung) : 5;
                if (spreizung <= 0) spreizung = 5;

                object oRegeneration = WertLesenStill(idEnergieanlage, "WQ_Regeneration");
                double regeneration = oRegeneration != null ? Convert.ToDouble(oRegeneration) : 0;

                SimulationPufferspeicher sp = new SimulationPufferspeicher();
                sp.Bezeichner = bezeichner;
                sp.Erzeuger = "Wärmequelle";
                // Konzept 6.6: Rolle und Speicher-ID für die Ergebniszeile - seit
                // Etappe 4a die ID der PROJEKTKOPIE (siehe Auflösungskette oben).
                sp.ID_Pufferspeicher = idSpeicher;
                sp.ID_Projekt = zeile.Table.Columns.Contains("ID_Projekt") &&
                                zeile["ID_Projekt"] != DBNull.Value
                    ? Convert.ToInt32(zeile["ID_Projekt"]) : 0;
                sp.Verwendung = SimulationPufferspeicher.VERWENDUNG_QUELLE;
                // Spreizung als Temperaturhub der nutzbaren Kapazität verwenden
                sp.Init(volumen, (int)Math.Round(spreizung), 0, verluste);
                sp.RegenerationProStunde = regeneration;
                // Quellspeicher startet gefüllt - er ist die vorhandene Wärmequelle
                sp.SOC = sp.Q_max;
                return sp;
            }
            catch (Exception ex)
            {
                // Protokollkanal-Nachzug: WARNUNG - die Wärmepumpe rechnet danach OHNE
                // Quellspeicher, die Quelle gilt als unbegrenzt.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "quellspeicher-aufbau-fehlgeschlagen-" + idEnergieanlage,
                    "Quellspeicher konnte nicht aufgebaut werden: " + ex.Message +
                    " - die Anlage " + idEnergieanlage + " rechnet ohne Quellspeicher.");
                return null;
            }
        }

        /// <summary>
        /// Die Speicherzeile hinter der Wärmequelle einer Anlage, nach der dreistufigen
        /// Auflösungskette aus <see cref="Quellspeicher"/>. Liefert immer die Spalten
        /// <c>ID</c>, <c>Bezeichner</c>, <c>Gesamtvolumen</c>,
        /// <c>Bereitschaftsverluste</c> (aus der Projektkopie zusätzlich
        /// <c>ID_Projekt</c>) oder <c>null</c>, wenn nichts gefunden wurde.
        ///
        /// Getrennt von <see cref="Quellspeicher"/>, damit die Kette an einer Stelle
        /// steht und nachvollziehbar bleibt, aus welcher Tabelle die Zeile stammt.
        /// </summary>
        private static DataRow QuellspeicherZeile(int idEnergieanlage)
        {
            // --- Stufe 1: Fremdschlüssel auf die Projektkopie ------------------------
            int idPuffer = ZahlOderNull(WertLesenStill(idEnergieanlage, "WQ_ID_Puffer"));
            if (idPuffer > 0)
            {
                DataTable dt = TabelleStill(
                    "SELECT ID, ID_Projekt, Bezeichner, Gesamtvolumen, Bereitschaftsverluste " +
                    "FROM [" + PufferSpCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", OleDbType.Integer) { Value = idPuffer });
                if (dt != null && dt.Rows.Count > 0) return dt.Rows[0];

                // Protokollkanal-Nachzug: HINWEIS - die Auflösungskette greift eine Stufe
                // tiefer, gerechnet wird vollwertig.
                SimulationProtokoll.Aktuell.HinweisEinmal(
                                  "quellspeicher-id-ohne-zeile-" + idEnergieanlage,
                                  "Quellspeicher: WQ_ID_Puffer = " + idPuffer + " der Anlage " +
                                  idEnergieanlage + " zeigt auf keine Speicherzeile - " +
                                  "es gilt der Bezeichner.");
            }

            string bezeichner = WertLesenStill(idEnergieanlage, "WQ_Puffer") as string;
            if (string.IsNullOrEmpty(bezeichner)) return null;

            // --- Stufe 2: Bezeichner in der Projektkopie -----------------------------
            // Deterministisch die kleinste ID: Projekte können denselben Speichertyp
            // durch wiederholtes Duplizieren mehrfach enthalten (Dedup-Aufhebung 5.2).
            int idProjekt = ZahlOderNull(SkalarStill(
                "SELECT ID_Projekt FROM Tab_Energieanlagen WHERE ID = " + idEnergieanlage));
            if (idProjekt > 0)
            {
                DataTable dt = TabelleStill(
                    "SELECT TOP 1 ID, ID_Projekt, Bezeichner, Gesamtvolumen, Bereitschaftsverluste " +
                    "FROM [" + PufferSpCtrl.TABLE + "] WHERE Bezeichner = ? AND ID_Projekt = ? " +
                    "ORDER BY ID",
                    new OleDbParameter("@bez", bezeichner),
                    new OleDbParameter("@proj", OleDbType.Integer) { Value = idProjekt });
                if (dt != null && dt.Rows.Count > 0) return dt.Rows[0];
            }

            // --- Stufe 3: Bezeichner im Katalog (Altbestand ohne Projektkopie) -------
            DataTable stamm = TabelleStill(
                "SELECT ID, Bezeichner, Gesamtvolumen, Bereitschaftsverluste FROM [" +
                PufferSpStammCtrl.TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", bezeichner));
            if (stamm != null && stamm.Rows.Count > 0)
            {
                // Protokollkanal-Nachzug: HINWEIS - Stufe 3 der Auflösungskette.
                SimulationProtokoll.Aktuell.HinweisEinmal(
                                  "quellspeicher-aus-katalog-" + idEnergieanlage,
                                  "Quellspeicher: Anlage " + idEnergieanlage + " hat keine " +
                                  "Projektkopie von \"" + bezeichner + "\" - es gilt der Katalog.");
                return stamm.Rows[0];
            }

            return null;
        }

        private static float[] KonstantesProfil(float temperatur)
        {
            float[] t = new float[8760];
            for (int i = 0; i < 8760; i++) t[i] = temperatur;
            return t;
        }

        /// <summary>
        /// Wochentag des 1. Januar im Altweg-Wochengang, Montag = 0.
        ///
        /// <para><b>PAKET Q1 — Befund K1-O6 erledigt.</b> Hier stand bis Q1
        /// <c>DateTime.Now.Year</c>: Der Wochentag wurde aus dem nächsten
        /// Nicht-Schaltjahr AB DEM HEUTIGEN DATUM abgeleitet. Damit hing das
        /// Rechenergebnis eines unveränderten Projekts davon ab, WANN man es rechnete —
        /// derselbe Lauf hätte 2027 eine andere Quelltemperatur-Ganglinie ergeben als
        /// 2026. Das ist keine Kalenderkonvention, sondern eine Zeitbombe, und es war
        /// zugleich die DRITTE Kalenderkonvention des Programms neben „1. Januar =
        /// Sonntag" des Bedarfspfads und <c>Tab_Klimadaten.WE</c> des Gebäudepfads
        /// (Konzept 8.1 Punkt 2, Randnotiz; K1-Entscheidung F3).</para>
        ///
        /// <para><b>Warum 3 (Donnerstag) und nicht die Klimadaten-Konvention aus F3.</b>
        /// Der Altweg ist ab Q1 <b>Lese-Altlast</b> (Konzept 15): Er trägt nur noch
        /// Bestandsdaten in <c>WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c>, und wer ein
        /// neues Profil pflegt, bekommt die kalenderunabhängige Tagesvariante. Für eine
        /// Altlast ist die richtige Änderung die KLEINSTMÖGLICHE: 3 ist genau der Wert,
        /// den <c>DateTime.Now.Year</c> heute liefert (2026 ist kein Schaltjahr, der
        /// 1. Januar 2026 ist ein Donnerstag). Der Wert ist damit für den aktuellen
        /// Bestand ergebnisgleich und ab sofort unveränderlich. Ihn stattdessen auf den
        /// Klimadaten-Kalender umzustellen wäre eine Ergebnisänderung an genau der
        /// Stelle, die dieses Paket stilllegt.</para>
        ///
        /// <para><b>Ohne Wirkung auf den Bestand.</b> In der produktiven Datenbank hat
        /// KEINE Anlage <c>WQ_Typ = 'Profil'</c>, und <c>WQ_Monatswerte</c> wie
        /// <c>WQ_Wochenwerte</c> sind in allen 131 Zeilen leer (gemessen 28.08.2026).
        /// Der Wochentag geht ohnehin nur in den ADDITIVEN Wochengang ein — ohne
        /// gepflegte Wochenwerte ist <c>woche[…]</c> durchweg 0 und der Kalender
        /// wirkungslos.</para>
        /// </summary>
        public const int WOCHENTAG_JAN1_ALTWEG = 3;

        /// <summary>
        /// Baut das Jahresprofil (8760 Stundenwerte) der Quelltemperatur aus
        /// Monats- und Wochenwerten - analog zur Brauchwasser-Stundenverteilung:
        ///
        ///   Quelltemperatur(h) = Monatswert(Monat) + Wochenwert(Wochentag, Stunde)
        ///
        /// Wochentag: Index 0 = Montag ... 6 = Sonntag, Startwert
        /// <see cref="WOCHENTAG_JAN1_ALTWEG"/>.
        ///
        /// <para><b>LESE-ALTLAST seit Paket Q1</b> (Konzept 15): Der Weg trägt nur noch
        /// Bestandsdaten aus <c>WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c>. Neue Profile
        /// entstehen als <c>Tab_Quellprofil</c>-Zeilen und rechnen über
        /// <c>QuellprofilCtrl.Jahresprofil</c> — die Tagesvariante dort ist
        /// kalenderunabhängig und braucht diesen Wochengang gar nicht.</para>
        /// </summary>
        /// <param name="monatswerteString">"t1;...;t12" Monats-Mitteltemperaturen [°C]</param>
        /// <param name="wochenwerteString">"w1;...;w168" Abweichungen [K], darf leer sein</param>
        public static float[] ProfilAusMonatsUndWochenwerten(string monatswerteString, string wochenwerteString)
        {
            if (string.IsNullOrEmpty(monatswerteString)) return null;

            string[] teile = monatswerteString.Split(';');
            if (teile.Length < 12) return null;

            float[] monat = new float[12];
            for (int m = 0; m < 12; m++)
            {
                if (!ZahlParsen(teile[m], out monat[m])) return null;
            }

            // Wochenwerte (optional): 7 Tage x 24 Stunden Abweichung [K]
            float[] woche = new float[168];
            if (!string.IsNullOrEmpty(wochenwerteString))
            {
                string[] wTeile = wochenwerteString.Split(';');
                for (int i = 0; i < 168 && i < wTeile.Length; i++)
                    ZahlParsen(wTeile[i], out woche[i]);
            }

            int[] tageProMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            // PAKET Q1 / Befund K1-O6: FESTER Wochentag statt DateTime.Now.Year -
            // Begründung und Ergebnisgleichheit bei WOCHENTAG_JAN1_ALTWEG.
            int wochentag = WOCHENTAG_JAN1_ALTWEG;

            float[] profil = new float[8760];
            int index = 0;
            for (int m = 0; m < 12; m++)
            {
                for (int tag = 0; tag < tageProMonat[m]; tag++)
                {
                    for (int h = 0; h < 24 && index < 8760; h++)
                        profil[index++] = monat[m] + woche[wochentag * 24 + h];

                    wochentag = (wochentag + 1) % 7;
                }
            }
            // Restliche Stunden (Rundung) mit Dezemberwert auffüllen
            while (index < 8760) profil[index++] = monat[11];

            return profil;
        }

        /// <summary>
        /// Liest ein Quelltemperatur-Jahresprofil aus einer CSV-Datei
        /// (siehe CSV_FORMAT_HINWEIS). Liefert null bei Fehlern.
        /// </summary>
        public static float[] ProfilAusCsv(string pfad)
        {
            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad)) return null;

            float[] profil = new float[8760];
            int index = 0;

            foreach (string zeileRoh in File.ReadLines(pfad))
            {
                if (index >= 8760) break;

                string zeile = zeileRoh.Trim();
                if (zeile.Length == 0) continue;

                // Letzten Zahlenwert der Zeile verwenden (erlaubt "Zeitstempel;Wert").
                // Erst Semikolon/Tab als Trenner versuchen (Komma = Dezimaltrennzeichen),
                // dann Komma als Trenner (Punkt = Dezimaltrennzeichen).
                float wert = LetzteZahl(zeile.Split(';', '\t'), true);
                if (float.IsNaN(wert) && zeile.IndexOf(',') >= 0)
                    wert = LetzteZahl(zeile.Split(','), false);

                if (float.IsNaN(wert)) continue; // z. B. Kopfzeile

                profil[index++] = wert;
            }

            return index == 8760 ? profil : null;
        }

        /// <summary>
        /// PAKET Q1: liest <paramref name="anzahl"/> Zahlenwerte aus einer CSV-Datei —
        /// der Importweg der Quellprofile (12, 365 oder 8760 Werte).
        ///
        /// <para><b>Dieselben Trennzeichen- und Dezimalregeln wie
        /// <see cref="ProfilAusCsv"/></b>: Erst Semikolon/Tabulator als Feldtrenner mit
        /// Komma als Dezimalzeichen, sonst Komma als Feldtrenner mit Punkt als
        /// Dezimalzeichen; gezählt wird der LETZTE parsebare Wert einer Zeile, damit
        /// „Zeitstempel;Wert" ohne Vorbehandlung durchgeht. Zeilen ohne Zahl (Kopfzeilen)
        /// werden übersprungen.</para>
        ///
        /// <para><b>ANSI statt UTF-8</b> (Projektregel <c>KONTEXT_Importkodierung_ANSI</c>):
        /// Deutsche Zählerexporte und Messreihen sind fast nie UTF-8. Gelesen wird über
        /// <see cref="AnsiEncoding"/> mit BOM-Erkennung — ein vorhandenes BOM schlägt die
        /// Vorgabe, genau wie in <c>GanglinienDatei.LeserOeffnen</c>. Für reine
        /// Zahlenzeilen ist das ohne Folge; es rettet die Kopfzeile mit Umlauten davor,
        /// als Ersatzzeichenfolge in einer Fehlermeldung zu landen.</para>
        ///
        /// <para><b>Genau <paramref name="anzahl"/> Werte oder <c>null</c>.</b> Eine zu
        /// kurze Datei stillschweigend mit Nullen aufzufüllen hieße, ein
        /// Temperaturprofil zu erfinden.</para>
        /// </summary>
        public static double[] WerteAusCsv(string pfad, int anzahl)
        {
            if (string.IsNullOrEmpty(pfad) || anzahl <= 0 || !File.Exists(pfad)) return null;

            double[] werte = new double[anzahl];
            int index = 0;

            try
            {
                using (StreamReader leser = new StreamReader(pfad, AnsiEncoding.Get(), true))
                {
                    string zeileRoh;
                    while ((zeileRoh = leser.ReadLine()) != null)
                    {
                        if (index >= anzahl) break;

                        string zeile = zeileRoh.Trim();
                        if (zeile.Length == 0) continue;

                        float wert = LetzteZahl(zeile.Split(';', '\t'), true);
                        if (float.IsNaN(wert) && zeile.IndexOf(',') >= 0)
                            wert = LetzteZahl(zeile.Split(','), false);

                        if (float.IsNaN(wert)) continue;   // z. B. Kopfzeile

                        werte[index++] = wert;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WerteAusCsv fehlgeschlagen: " + ex.Message);
                return null;
            }

            return index == anzahl ? werte : null;
        }

        /// <summary>
        /// Liefert den letzten parsebaren Zahlenwert aus den Feldern, sonst NaN.
        /// </summary>
        private static float LetzteZahl(string[] felder, bool kommaAlsDezimal)
        {
            for (int f = felder.Length - 1; f >= 0; f--)
            {
                string t = felder[f] != null ? felder[f].Trim() : "";
                if (t.Length == 0) continue;
                if (kommaAlsDezimal) t = t.Replace(',', '.');
                float w;
                if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                    return w;
            }
            return float.NaN;
        }

        /// <summary>
        /// Parst eine Zahl mit Dezimal-Komma oder -Punkt.
        /// </summary>
        public static bool ZahlParsen(string text, out float wert)
        {
            wert = 0f;
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Trim().Replace(',', '.');
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out wert);
        }
    }
}
