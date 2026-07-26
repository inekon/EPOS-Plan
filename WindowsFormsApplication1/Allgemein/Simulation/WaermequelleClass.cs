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
    ///   WQ_Typ          - Wärmequelle: Aussenluft | Konstant | Pufferspeicher | Profil | CSV
    ///   WQ_Temp         - konstante Quelltemperatur [°C] (Typ Konstant)
    ///   WQ_Monatswerte  - "t1;...;t12" Monats-Mitteltemperaturen [°C] (Typ Profil)
    ///   WQ_Wochenwerte  - "w1;...;w168" Tagesgang je Wochentag [K] (Typ Profil)
    ///   WQ_CSV          - Pfad zur CSV-Datei mit 8760 Stundenwerten (Typ CSV)
    ///   WS_Typ          - Wärmesenke: Beides | Warmwasser | Heizung
    ///
    /// Für Luft-Wasser-Wärmepumpen ist die Quelle immer die Außenluft
    /// (Außentemperatur der Klimaregion). Für Sole-Wasser / Wasser-Wasser
    /// liefert Quelltemperatur() das Jahresprofil der Quelltemperatur, das in
    /// der Simulation anstelle der Außentemperatur in die Kennlinien eingeht.
    /// </summary>
    public static class WaermequelleClass
    {
        // Betriebsmodus der Wärmepumpe (Leistungssteuerung)
        public const string MODUS_LAUFZEIT = "Laufzeit";   // maximale Leistung, Speicher laden
        public const string MODUS_LEISTUNG = "Leistung";   // nur den Bedarf decken (moduliert)
        public const string MODUS_PV = "PV";               // bei PV-Überschuss maximale Leistung

        // Wärmesenke: welchen Bedarfsanteil deckt der Erzeuger ab?
        public const string SENKE_BEIDES = "Beides";
        public const string SENKE_WARMWASSER = "Warmwasser";
        public const string SENKE_HEIZUNG = "Heizung";

        public const string TYP_AUSSENLUFT = "Aussenluft";
        public const string TYP_KONSTANT = "Konstant";
        public const string TYP_PUFFER = "Pufferspeicher";
        public const string TYP_PROFIL = "Profil";
        public const string TYP_CSV = "CSV";

        /// <summary>Anzeigetexte für die Auswahl im Dialog.</summary>
        public static readonly string[] TypAnzeige =
        {
            "Außenluft (Klimadaten)",
            "Konstante Temperatur",
            "Pufferspeicher",
            "Quellprofil (Monatswerte)",
            "CSV-Datei (Stundenwerte)"
        };

        public static readonly string[] TypWerte =
        {
            TYP_AUSSENLUFT, TYP_KONSTANT, TYP_PUFFER, TYP_PROFIL, TYP_CSV
        };

        /// <summary>
        /// Hinweistext zum CSV-Format (wird beim Einlesen angezeigt).
        /// </summary>
        public const string CSV_FORMAT_HINWEIS =
            "Erwartetes CSV-Format für das Quelltemperatur-Profil:\n\n" +
            "- 8760 Zeilen = Stundenwerte für ein Jahr (01.01. 00:00 bis 31.12. 23:00)\n" +
            "- je Zeile ein Temperaturwert in °C (Dezimal-Komma oder -Punkt)\n" +
            "- optional mit Zeitstempel: \"Zeitstempel;Temperatur\" (Semikolon-getrennt,\n" +
            "  es wird der letzte Zahlenwert der Zeile verwendet)\n" +
            "- eine Kopfzeile wird automatisch erkannt und übersprungen";

        private static bool _schemaGeprueft = false;

        /// <summary>
        /// Legt die benötigten Spalten in Tab_Energieanlagen an, falls sie fehlen.
        /// Wird nur einmal pro Programmlauf tatsächlich geprüft.
        ///
        /// WICHTIG: bewusst über eine eigene, stille OleDb-Verbindung - die
        /// DataRepository-Methoden zeigen bei Fehlern MessageBoxen an und liefern
        /// leere Ergebnisse statt null, damit ließe sich das Fehlen einer Spalte
        /// nicht sauber erkennen.
        /// </summary>
        public static void SchemaSicherstellen()
        {
            if (_schemaGeprueft) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // Vorhandene Spalten in einem Rutsch ermitteln
                    DataTable dt = new DataTable();
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM Tab_Energieanlagen", conn))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.FillSchema(dt, SchemaType.Source);
                    }

                    SpalteSicherstellen(conn, dt, "Prioritaet", "LONG");
                    SpalteSicherstellen(conn, dt, "WQ_Typ", "TEXT(50)");
                    SpalteSicherstellen(conn, dt, "WQ_Temp", "DOUBLE");
                    SpalteSicherstellen(conn, dt, "WQ_Monatswerte", "TEXT(255)");
                    SpalteSicherstellen(conn, dt, "WQ_Wochenwerte", "MEMO"); // 168 Werte
                    SpalteSicherstellen(conn, dt, "WQ_CSV", "TEXT(255)");
                    SpalteSicherstellen(conn, dt, "WQ_Puffer", "TEXT(255)");      // Quell-Pufferspeicher
                    SpalteSicherstellen(conn, dt, "WQ_Spreizung", "DOUBLE");      // nutzbare Spreizung [K]
                    SpalteSicherstellen(conn, dt, "WQ_Regeneration", "DOUBLE");   // Nachladung [kW]
                    SpalteSicherstellen(conn, dt, "WQ_Unbegrenzt", "YESNO");      // Quelle immer verfügbar
                    SpalteSicherstellen(conn, dt, "WS_Typ", "TEXT(50)");          // Wärmesenke
                    SpalteSicherstellen(conn, dt, "BM_Typ", "TEXT(50)");          // Betriebsmodus
                }

                // Speicherregelung je Pufferspeicher-Zuordnung (Ein-/Abschaltschwelle in %)
                SpalteSicherstellen("Z_ProjektPufferSp", "Schwelle_Ein", "DOUBLE");
                SpalteSicherstellen("Z_ProjektPufferSp", "Schwelle_Aus", "DOUBLE");

                _schemaGeprueft = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SchemaSicherstellen fehlgeschlagen: " + ex.Message);
            }
        }

        /// <summary>
        /// Legt eine Spalte in einer beliebigen Tabelle an, falls sie fehlt
        /// (still, ohne Fehlerdialoge). Wird u. a. für die Speicherregelung in
        /// Z_ProjektPufferSp verwendet.
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

        private static void SpalteSicherstellen(OleDbConnection conn, DataTable schema, string spalte, string typDefinition)
        {
            if (schema.Columns.Contains(spalte)) return; // Spalte existiert bereits

            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                    "ALTER TABLE Tab_Energieanlagen ADD COLUMN [" + spalte + "] " + typDefinition, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Spalte " + spalte + " konnte nicht angelegt werden: " + ex.Message);
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
        /// </summary>
        public static bool WertSchreiben(int idEnergieanlage, string spalte, object wert)
        {
            try
            {
                string sql = "UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = " + idEnergieanlage;
                return DataRepository.ExecuteSQL(sql,
                    new System.Data.OleDb.OleDbParameter("@w", wert ?? (object)DBNull.Value));
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
        /// <param name="idProjekt">Projekt-ID (für Pufferspeicher-Zuordnung)</param>
        /// <param name="wpTyp">WP-Typ aus Tab_WP ("Luft-Wasser", "Sole-Wasser", "Wasser-Wasser")</param>
        /// <param name="aussentemp">Außentemperatur der Klimaregion (8760 Werte)</param>
        public static float[] Quelltemperatur(int idEnergieanlage, int idProjekt, string wpTyp, float[] aussentemp)
        {
            // Luft-Wasser: immer Außenluft
            if (string.IsNullOrEmpty(wpTyp) || wpTyp == "Luft-Wasser") return aussentemp;

            string typ = WertLesen(idEnergieanlage, "WQ_Typ") as string;
            if (string.IsNullOrEmpty(typ) || typ == TYP_AUSSENLUFT) return aussentemp;

            try
            {
                switch (typ)
                {
                    case TYP_KONSTANT:
                        {
                            object v = WertLesen(idEnergieanlage, "WQ_Temp");
                            if (v == null) return aussentemp;
                            return KonstantesProfil(Convert.ToSingle(v));
                        }

                    case TYP_PUFFER:
                        {
                            // Temperatur des als Wärmequelle gewählten Pufferspeichers
                            object v = WertLesen(idEnergieanlage, "WQ_Temp");
                            if (v != null) return KonstantesProfil(Convert.ToSingle(v));

                            // Fallback (Altdaten): mittlere Temperatur der Zuordnung
                            object vor = DataRepository.ExecuteScalar(
                                "SELECT Vorlauf FROM Z_ProjektPufferSp WHERE ID_Projekt=" + idProjekt +
                                " AND Erzeuger='Wärmepumpe' ORDER BY Prioritaet");
                            object rue = DataRepository.ExecuteScalar(
                                "SELECT Ruecklauf FROM Z_ProjektPufferSp WHERE ID_Projekt=" + idProjekt +
                                " AND Erzeuger='Wärmepumpe' ORDER BY Prioritaet");
                            if (vor == null || vor == DBNull.Value || rue == null || rue == DBNull.Value)
                                return aussentemp;
                            float mittel = (Convert.ToSingle(vor) + Convert.ToSingle(rue)) / 2f;
                            return KonstantesProfil(mittel);
                        }

                    case TYP_PROFIL:
                        {
                            string monat = WertLesen(idEnergieanlage, "WQ_Monatswerte") as string;
                            string woche = WertLesen(idEnergieanlage, "WQ_Wochenwerte") as string;
                            float[] profil = ProfilAusMonatsUndWochenwerten(monat, woche);
                            return profil ?? aussentemp;
                        }

                    case TYP_CSV:
                        {
                            string pfad = WertLesen(idEnergieanlage, "WQ_CSV") as string;
                            float[] profil = ProfilAusCsv(pfad);
                            return profil ?? aussentemp;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Quelltemperatur (" + typ + ") konnte nicht ermittelt werden: " + ex.Message);
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
        /// </summary>
        public static SimulationPufferspeicher Quellspeicher(int idEnergieanlage, string wpTyp)
        {
            // Luft-Wasser-WP entnimmt keine Wärme aus einem Speicher
            if (string.IsNullOrEmpty(wpTyp) || wpTyp == "Luft-Wasser") return null;

            string typ = WertLesen(idEnergieanlage, "WQ_Typ") as string;
            if (typ != TYP_PUFFER) return null;

            // "unbegrenzt verfügbar" -> nur die Temperatur wirkt, keine Bilanz
            object unbegrenzt = WertLesen(idEnergieanlage, "WQ_Unbegrenzt");
            if (unbegrenzt != null && Convert.ToBoolean(unbegrenzt)) return null;

            string bezeichner = WertLesen(idEnergieanlage, "WQ_Puffer") as string;
            if (string.IsNullOrEmpty(bezeichner)) return null;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Gesamtvolumen, Bereitschaftsverluste FROM [" + PufferSpStammCtrl.TABLE +
                    "] WHERE Bezeichner = ?",
                    new OleDbParameter("@bez", bezeichner));
                if (dt == null || dt.Rows.Count == 0) return null;

                double volumen = dt.Rows[0]["Gesamtvolumen"] != DBNull.Value
                    ? Convert.ToDouble(dt.Rows[0]["Gesamtvolumen"]) : 0;
                double verluste = dt.Rows[0]["Bereitschaftsverluste"] != DBNull.Value
                    ? Convert.ToDouble(dt.Rows[0]["Bereitschaftsverluste"]) : 0;
                if (volumen <= 0) return null;

                object oSpreizung = WertLesen(idEnergieanlage, "WQ_Spreizung");
                double spreizung = oSpreizung != null ? Convert.ToDouble(oSpreizung) : 5;
                if (spreizung <= 0) spreizung = 5;

                object oRegeneration = WertLesen(idEnergieanlage, "WQ_Regeneration");
                double regeneration = oRegeneration != null ? Convert.ToDouble(oRegeneration) : 0;

                SimulationPufferspeicher sp = new SimulationPufferspeicher();
                sp.Bezeichner = bezeichner;
                sp.Erzeuger = "Wärmequelle";
                // Spreizung als Temperaturhub der nutzbaren Kapazität verwenden
                sp.Init(volumen, (int)Math.Round(spreizung), 0, verluste);
                sp.RegenerationProStunde = regeneration;
                // Quellspeicher startet gefüllt - er ist die vorhandene Wärmequelle
                sp.SOC = sp.Q_max;
                return sp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Quellspeicher konnte nicht aufgebaut werden: " + ex.Message);
                return null;
            }
        }

        private static float[] KonstantesProfil(float temperatur)
        {
            float[] t = new float[8760];
            for (int i = 0; i < 8760; i++) t[i] = temperatur;
            return t;
        }

        /// <summary>
        /// Baut das Jahresprofil (8760 Stundenwerte) der Quelltemperatur aus
        /// Monats- und Wochenwerten - analog zur Brauchwasser-Stundenverteilung:
        ///
        ///   Quelltemperatur(h) = Monatswert(Monat) + Wochenwert(Wochentag, Stunde)
        ///
        /// Wochentag: das Simulationsjahr beginnt am 1. Januar eines
        /// Nicht-Schaltjahres (8760 Stunden), der Wochentag wird daraus abgeleitet
        /// (Index 0 = Montag ... 6 = Sonntag).
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

            // Wochentag des 1. Januar aus dem nächsten Nicht-Schaltjahr ableiten
            int jahr = DateTime.Now.Year;
            while (DateTime.IsLeapYear(jahr)) jahr++;
            // DayOfWeek: Sonntag = 0 ... Samstag = 6 -> umrechnen auf Montag = 0
            int wochentag = ((int)new DateTime(jahr, 1, 1).DayOfWeek + 6) % 7;

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
