using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wärmesenke einer Wärmeerzeuger-Anlage (Konzept 3.1, 3.4, 4.2, 4.6, 5.3).
    ///
    /// Jede Anlage hat genau EINE Hauptsenke und optional eine Zweitsenke. Diese Klasse
    /// ist der dialogfreie Kern dazu: Lesen, Schreiben, Prüfen (4.6), Anzeigetext — und
    /// die Übergangsbrücke auf die Alt-Zuordnung <c>Z_ProjektPufferSp</c>, die die Engine
    /// bis Paket 4 auswertet.
    ///
    /// Der Dialog <see cref="Form_Waermesenke"/> ist reine Oberfläche darüber; ein
    /// headless laufendes Prüfprogramm kann dieselben Wege ohne Fenster benutzen.
    /// </summary>
    public static class WaermesenkeClass
    {
        // --- Hauptsenke: Werte der Spalte WS_Ziel (Konzept 5.3) -----------------------

        /// <summary>Direkte Deckung des Momentanbedarfs — Verhalten wie bisher.</summary>
        public const string ZIEL_HEIZKREIS = "Heizkreis";

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Heizung".</summary>
        public const string ZIEL_PUFFER_HEIZUNG = "PufferHeizung";

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Brauchwasser".</summary>
        public const string ZIEL_PUFFER_BRAUCHWASSER = "PufferBrauchwasser";

        // --- Verwendung eines Projekt-Puffers (Konzept 5.1) ---------------------------

        public const string VERWENDUNG_HEIZUNG = ProjektPuffer.VERWENDUNG_HEIZUNG;   // "Heizung"
        public const string VERWENDUNG_BRAUCHWASSER = "Brauchwasser";

        // Eine eigene Liste der Erzeugertypen stand hier ursprünglich als
        // ERZEUGER_TYPEN. Sie wurde von niemandem gelesen: Wer die Typen braucht,
        // nimmt ProjektPuffer.WAERMEERZEUGER_TYPEN (die SQL-taugliche Fassung, die
        // Ladeordnung und ProjektPuffer bereits benutzen). Zwei Wahrheiten über
        // dieselbe Menge sind eine Fehlerquelle - die tote wurde entfernt.

        /// <summary>true, wenn das Ziel einen Pufferspeicher meint.</summary>
        public static bool IstPufferZiel(string ziel)
        {
            return string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal) ||
                   string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal);
        }

        /// <summary>Verwendung, die ein Puffer für dieses Ziel haben muss; null bei Heizkreis.</summary>
        public static string VerwendungZuZiel(string ziel)
        {
            if (string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return VERWENDUNG_HEIZUNG;
            if (string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return VERWENDUNG_BRAUCHWASSER;
            return null;
        }

        /// <summary>Deutscher Anzeigename eines Ziels.</summary>
        public static string ZielAnzeige(string ziel)
        {
            if (string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return "Pufferspeicher Heizung";
            if (string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return "Pufferspeicher Brauchwasser";
            return "Heizkreis";
        }

        // --- Datensatz ----------------------------------------------------------------

        /// <summary>Die Senkenfelder einer Anlage (Konzept 5.3).</summary>
        public sealed class SenkeDaten
        {
            /// <summary>WS_Ziel — Hauptsenke.</summary>
            public string Ziel = ZIEL_HEIZKREIS;

            /// <summary>WS_ID_Puffer — 0 = keiner (in der Datenbank NULL, nie 0: FK!).</summary>
            public int ID_Puffer;

            /// <summary>WS_Typ — Bedarfsart, nur bei Hauptsenke Heizkreis wirksam (Konzept 3.1).</summary>
            public string Bedarfsart = WaermequelleClass.SENKE_BEIDES;

            /// <summary>WS_Ladeprio — 0 = Vorgabe nach Erzeugertyp.</summary>
            public int Ladeprio;

            /// <summary>WS_Ladegrenze [%] — 0 = nicht gesetzt, dann gilt die Puffer-Regel.</summary>
            public double Ladegrenze;

            /// <summary>WS_Ladeprio_PV — 0 = keine Sonderregel bei PV-Überschuss.</summary>
            public int LadeprioPV;

            /// <summary>WS_Ziel2 — Zweitsenke; leer = keine.</summary>
            public string Ziel2 = "";

            /// <summary>WS_ID_Puffer2 — 0 = keiner.</summary>
            public int ID_Puffer2;

            /// <summary>WS_Ladeprio2 — 0 = Vorgabe.</summary>
            public int Ladeprio2;

            /// <summary>WS_Ladegrenze2 [%] — 0 = nicht gesetzt.</summary>
            public double Ladegrenze2;

            public bool HatZweitsenke
            {
                get { return !string.IsNullOrEmpty(Ziel2); }
            }

            public SenkeDaten Kopie()
            {
                return (SenkeDaten)MemberwiseClone();
            }
        }

        /// <summary>Ein Projekt-Pufferspeicher, so wie die Dialoge ihn brauchen.</summary>
        public sealed class PufferInfo
        {
            public int ID;
            public int ID_Projekt;
            public string Bezeichner = "";
            public string Verwendung = "";
            public int Gesamtvolumen;
            public double Bereitschaftsverluste;
            public int Vorlauf;
            public int Ruecklauf;
            public double SchwelleEin;
            public double SchwelleAus;
            public double SchwelleAusNachrang;
            public int Entladeprio;

            /// <summary>true, wenn <c>Verwendung</c> in der Datenbank nicht gepflegt ist.</summary>
            public bool VerwendungFehlt;

            /// <summary>Nutzbare Kapazität [kWh] aus Volumen und Spreizung; 0 ohne Temperaturpaar.</summary>
            public double Q_max
            {
                get
                {
                    if (Vorlauf <= Ruecklauf || Ruecklauf <= 0) return 0;
                    return Gesamtvolumen * 1.16 * (Vorlauf - Ruecklauf) / 1000.0;
                }
            }

            public override string ToString()
            {
                return Gesamtvolumen > 0
                    ? Bezeichner + " (" + Gesamtvolumen + " l)"
                    : Bezeichner;
            }
        }

        // --- Lesen und Schreiben ------------------------------------------------------

        /// <summary>Liest die Senkenfelder einer Anlage; nie <c>null</c>.</summary>
        public static SenkeDaten Lesen(int idAnlage)
        {
            SenkeDaten d = new SenkeDaten();
            if (idAnlage <= 0) return d;

            DataTable dt = StilleDb.Tabelle(
                "SELECT WS_Ziel, WS_ID_Puffer, WS_Typ, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, " +
                "       WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2 " +
                "FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage));
            if (dt == null || dt.Rows.Count == 0) return d;

            return AusDatenzeile(dt.Rows[0]);
        }

        /// <summary>
        /// Baut die Senkenfelder aus einer bereits gelesenen Zeile von
        /// <c>Tab_Energieanlagen</c>. Fehlende Spalten liefern die Vorbelegung.
        ///
        /// Die Erzeuger-Übersicht liest ihre Anlagen ohnehin in einer Abfrage; ohne diese
        /// Methode käme je Zeile eine zweite Abfrage dazu.
        /// </summary>
        public static SenkeDaten AusDatenzeile(DataRow r)
        {
            SenkeDaten d = new SenkeDaten();
            if (r == null) return d;

            string ziel = StilleDb.Text(StilleDb.Feld(r, "WS_Ziel"));
            d.Ziel = ziel.Length > 0 ? ziel : ZIEL_HEIZKREIS;
            d.ID_Puffer = StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer"));

            string bedarfsart = StilleDb.Text(StilleDb.Feld(r, "WS_Typ"));
            if (bedarfsart.Length > 0) d.Bedarfsart = bedarfsart;

            d.Ladeprio = StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio"));
            d.Ladegrenze = StilleDb.Kommazahl(StilleDb.Feld(r, "WS_Ladegrenze"));
            d.LadeprioPV = StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio_PV"));

            d.Ziel2 = StilleDb.Text(StilleDb.Feld(r, "WS_Ziel2"));
            d.ID_Puffer2 = StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer2"));
            d.Ladeprio2 = StilleDb.Zahl(StilleDb.Feld(r, "WS_Ladeprio2"));
            d.Ladegrenze2 = StilleDb.Kommazahl(StilleDb.Feld(r, "WS_Ladegrenze2"));

            Normalisieren(d);
            return d;
        }

        /// <summary>
        /// Räumt einen Datensatz auf: unbekanntes Ziel wird zu <see cref="ZIEL_HEIZKREIS"/>
        /// (Konzept 4.6, erste Zeile der Tabelle), Puffer-IDs ohne Puffer-Ziel entfallen,
        /// eine Zweitsenke ohne Ziel wird ganz gelöscht.
        /// </summary>
        public static void Normalisieren(SenkeDaten d)
        {
            if (d == null) return;

            if (!IstPufferZiel(d.Ziel))
            {
                d.Ziel = ZIEL_HEIZKREIS;
                d.ID_Puffer = 0;
                d.Ladeprio = 0;
                d.Ladegrenze = 0;
                d.LadeprioPV = 0;
            }

            if (string.IsNullOrEmpty(d.Ziel2) || !IstPufferZiel(d.Ziel2))
            {
                // Zweitsenken sind in Paket 2 ausschließlich Puffer-Ziele (siehe Protokoll).
                d.Ziel2 = "";
                d.ID_Puffer2 = 0;
                d.Ladeprio2 = 0;
                d.Ladegrenze2 = 0;
            }

            if (string.IsNullOrEmpty(d.Bedarfsart)) d.Bedarfsart = WaermequelleClass.SENKE_BEIDES;
            if (d.ID_Puffer < 0) d.ID_Puffer = 0;
            if (d.ID_Puffer2 < 0) d.ID_Puffer2 = 0;
            if (d.Ladegrenze < 0) d.Ladegrenze = 0;
            if (d.Ladegrenze2 < 0) d.Ladegrenze2 = 0;
        }

        /// <summary>
        /// Schreibt die Senkenfelder an die Anlage.
        ///
        /// WICHTIG: Die drei FK-Spalten bekommen <c>NULL</c> statt 0. Schritt 4 der
        /// <see cref="SchemaMigration"/> hat auf <c>Tab_Pufferspeicher.ID</c> eine
        /// erzwungene Beziehung gelegt; 0 ist keine gültige Puffer-ID und das UPDATE
        /// würde abgewiesen (SchemaKatalog, Kopfkommentar).
        /// </summary>
        public static bool Schreiben(int idAnlage, SenkeDaten d)
        {
            if (idAnlage <= 0 || d == null) return false;
            Normalisieren(d);

            // Die drei Spalten, die NULL bekommen können, gehen über die Überladung mit
            // ausdrücklichem OleDbType (StilleDb.Par-Regel): aus DBNull allein leitet der
            // Provider keinen Spaltentyp ab.
            bool ok = true;
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ziel", d.Ziel);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_ID_Puffer",
                                                 OleDbType.Integer, IdOderNull(d.ID_Puffer));
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Typ", d.Bedarfsart);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ladeprio", d.Ladeprio);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ladegrenze", d.Ladegrenze);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ladeprio_PV", d.LadeprioPV);

            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ziel2", OleDbType.VarWChar,
                                                 d.HatZweitsenke ? (object)d.Ziel2 : DBNull.Value);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_ID_Puffer2",
                                                 OleDbType.Integer, IdOderNull(d.ID_Puffer2));
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ladeprio2", d.Ladeprio2);
            ok &= WaermequelleClass.WertSchreiben(idAnlage, "WS_Ladegrenze2", d.Ladegrenze2);

            return ok;
        }

        /// <summary>0 → DBNull (Fremdschlüssel), sonst die ID.</summary>
        private static object IdOderNull(int id)
        {
            return id > 0 ? (object)id : DBNull.Value;
        }

        // --- Projekt-Pufferspeicher ---------------------------------------------------

        /// <summary>
        /// Alle Projekt-Puffer eines Projekts, optional auf eine Verwendung gefiltert.
        ///
        /// Eine LEERE <c>Verwendung</c> zählt als „Heizung": genau das ist die Vorbelegung,
        /// mit der Migration (5.5) und <c>ProjektPuffer.PufferParameter</c> Puffer anlegen.
        /// Altbestand, der über das frühere implizite <c>CopyFromStamm</c> entstanden ist,
        /// bliebe sonst unsichtbar und wäre nicht mehr auswählbar.
        /// </summary>
        public static List<PufferInfo> ProjektPufferListe(int idProjekt, string verwendung)
        {
            List<PufferInfo> liste = new List<PufferInfo>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Verwendung, Gesamtvolumen, Bereitschaftsverluste, " +
                "       Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, Schwelle_Aus_Nachrang, Entladeprio " +
                "FROM Tab_Pufferspeicher WHERE ID_Projekt = ? ORDER BY Bezeichner, ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                PufferInfo p = AusZeile(r);
                if (string.IsNullOrEmpty(verwendung) ||
                    string.Equals(WirksameVerwendung(p), verwendung, StringComparison.OrdinalIgnoreCase))
                    liste.Add(p);
            }

            return liste;
        }

        /// <summary>Ein einzelner Projekt-Puffer; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public static PufferInfo PufferLesen(int idPuffer)
        {
            if (idPuffer <= 0) return null;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Verwendung, Gesamtvolumen, Bereitschaftsverluste, " +
                "       Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, Schwelle_Aus_Nachrang, Entladeprio " +
                "FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer));
            if (dt == null || dt.Rows.Count == 0) return null;

            return AusZeile(dt.Rows[0]);
        }

        private static PufferInfo AusZeile(DataRow r)
        {
            PufferInfo p = new PufferInfo();
            p.ID = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
            p.ID_Projekt = StilleDb.Zahl(StilleDb.Feld(r, "ID_Projekt"));
            p.Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
            p.Verwendung = StilleDb.Text(StilleDb.Feld(r, "Verwendung"));
            p.VerwendungFehlt = p.Verwendung.Length == 0;
            p.Gesamtvolumen = StilleDb.Zahl(StilleDb.Feld(r, "Gesamtvolumen"));
            p.Bereitschaftsverluste = StilleDb.Kommazahl(StilleDb.Feld(r, "Bereitschaftsverluste"));
            p.Vorlauf = StilleDb.Zahl(StilleDb.Feld(r, "Vorlauf"));
            p.Ruecklauf = StilleDb.Zahl(StilleDb.Feld(r, "Ruecklauf"));
            p.SchwelleEin = StilleDb.Kommazahl(StilleDb.Feld(r, "Schwelle_Ein"), Ladeordnung.SCHWELLE_EIN_DEFAULT);
            p.SchwelleAus = StilleDb.Kommazahl(StilleDb.Feld(r, "Schwelle_Aus"), Ladeordnung.SCHWELLE_AUS_DEFAULT);
            p.SchwelleAusNachrang = StilleDb.Kommazahl(StilleDb.Feld(r, "Schwelle_Aus_Nachrang"), p.SchwelleAus);
            p.Entladeprio = StilleDb.Zahl(StilleDb.Feld(r, "Entladeprio"));
            if (p.SchwelleEin <= 0) p.SchwelleEin = Ladeordnung.SCHWELLE_EIN_DEFAULT;
            if (p.SchwelleAus <= 0) p.SchwelleAus = Ladeordnung.SCHWELLE_AUS_DEFAULT;
            if (p.SchwelleAusNachrang <= 0) p.SchwelleAusNachrang = p.SchwelleAus;
            return p;
        }

        /// <summary>Verwendung eines Puffers; leere Angabe gilt als „Heizung" (siehe ProjektPuffer).</summary>
        public static string WirksameVerwendung(PufferInfo p)
        {
            if (p == null) return VERWENDUNG_HEIZUNG;
            return p.Verwendung.Length > 0 ? p.Verwendung : VERWENDUNG_HEIZUNG;
        }

        // --- Validierung nach Konzept 4.6 ---------------------------------------------

        /// <summary>Ergebnis der Senkenprüfung.</summary>
        public sealed class PruefErgebnis
        {
            /// <summary>true = speichern erlaubt.</summary>
            public bool Ok = true;

            /// <summary>Meldungstext des Blockers; null, wenn <see cref="Ok"/>.</summary>
            public string Fehler;

            /// <summary>true = dem Anwender den Absprung „Pufferspeicher anlegen…" anbieten.</summary>
            public bool AbsprungPufferVerwaltung;

            /// <summary>Hinweis ohne Blockerwirkung (Konzept 4.6, letzte Zeile).</summary>
            public string Warnung;
        }

        /// <summary>
        /// Prüft die Senkeneinstellung einer Anlage nach der Tabelle in Konzept 4.6.
        /// Blockiert werden: Puffer-Senke ohne passenden Projekt-Puffer, Zweitsenke gleich
        /// Hauptsenke, Puffer gleichzeitig Quelle und Senke derselben Anlage. Ein Kanal
        /// ohne Bedarf ergibt nur eine Warnung.
        /// </summary>
        public static PruefErgebnis Pruefen(int idProjekt, int idAnlage, SenkeDaten d)
        {
            PruefErgebnis erg = new PruefErgebnis();
            if (d == null) { erg.Ok = false; erg.Fehler = "Keine Senkendaten übergeben."; return erg; }

            Normalisieren(d);

            // 1. Hauptsenke auf Puffer -> Projekt-Puffer muss existieren, Verwendung passen
            if (IstPufferZiel(d.Ziel))
            {
                string fehler;
                if (!PufferPasst(idProjekt, d.ID_Puffer, d.Ziel, "Hauptsenke", out fehler))
                {
                    erg.Ok = false;
                    erg.Fehler = fehler;
                    erg.AbsprungPufferVerwaltung = true;
                    return erg;
                }
            }

            // 2. Zweitsenke -> derselbe Test
            if (d.HatZweitsenke && IstPufferZiel(d.Ziel2))
            {
                string fehler;
                if (!PufferPasst(idProjekt, d.ID_Puffer2, d.Ziel2, "Zweitsenke", out fehler))
                {
                    erg.Ok = false;
                    erg.Fehler = fehler;
                    erg.AbsprungPufferVerwaltung = true;
                    return erg;
                }
            }

            // 3. Zweitsenke darf nicht die Hauptsenke sein
            //
            // Nach Normalisieren gilt: HatZweitsenke ⇒ Ziel2 IST ein Puffer-Ziel (alles
            // andere wird dort gelöscht, siehe Normalisieren und Abweichung 1 im
            // Protokoll). „Beide sind kein Puffer" kann es hier deshalb nicht geben; der
            // frühere zweite Disjunkt war unerreichbar und hätte nur vorgetäuscht, der
            // Fall werde behandelt. Übrig bleibt die eine Frage, auf die es ankommt:
            // zeigen Haupt- und Zweitsenke auf DENSELBEN Speicher?
            if (d.HatZweitsenke && IstPufferZiel(d.Ziel) && d.ID_Puffer == d.ID_Puffer2)
            {
                erg.Ok = false;
                erg.Fehler = "Die Zweitsenke muss sich von der Hauptsenke unterscheiden." +
                             Environment.NewLine +
                             "Beide zeigen auf " + ZielAnzeige(d.Ziel) +
                             " „" + PufferName(d.ID_Puffer) + "\".";
                return erg;
            }

            // 4. Derselbe Puffer als Quelle UND Senke der Anlage waere ein Kurzschluss
            int idQuellPuffer = QuellPufferDerAnlage(idProjekt, idAnlage);
            if (idQuellPuffer > 0 &&
                (idQuellPuffer == d.ID_Puffer || (d.HatZweitsenke && idQuellPuffer == d.ID_Puffer2)))
            {
                erg.Ok = false;
                erg.Fehler = "Der Pufferspeicher „" + PufferName(idQuellPuffer) + "\" ist bereits die " +
                             "WÄRMEQUELLE dieser Anlage." + Environment.NewLine +
                             "Derselbe Speicher kann nicht zugleich Quelle und Senke sein " +
                             "(Kurzschluss); bitte einen anderen Speicher wählen.";
                return erg;
            }

            // 5. Kanal ohne Bedarf -> nur Hinweis, kein Blocker
            erg.Warnung = KanalWarnung(idProjekt, d);
            return erg;
        }

        /// <summary>Existiert der Puffer im Projekt und passt seine Verwendung zum Ziel?</summary>
        private static bool PufferPasst(int idProjekt, int idPuffer, string ziel,
                                        string rolle, out string fehler)
        {
            fehler = null;
            string verlangt = VerwendungZuZiel(ziel);

            if (idPuffer <= 0)
            {
                fehler = "Für die " + rolle + " „" + ZielAnzeige(ziel) + "\" ist kein Pufferspeicher gewählt." +
                         Environment.NewLine + Environment.NewLine +
                         "Im Projekt muss ein Pufferspeicher mit der Verwendung „" + verlangt +
                         "\" angelegt sein.";
                return false;
            }

            PufferInfo p = PufferLesen(idPuffer);
            if (p == null || p.ID_Projekt != idProjekt)
            {
                fehler = "Der für die " + rolle + " gewählte Pufferspeicher gehört nicht zu diesem Projekt " +
                         "oder wurde entfernt." + Environment.NewLine + Environment.NewLine +
                         "Bitte einen Projekt-Pufferspeicher mit der Verwendung „" + verlangt + "\" anlegen.";
                return false;
            }

            if (!string.Equals(WirksameVerwendung(p), verlangt, StringComparison.OrdinalIgnoreCase))
            {
                fehler = "Der Pufferspeicher „" + p.Bezeichner + "\" hat die Verwendung „" +
                         WirksameVerwendung(p) + "\", die " + rolle + " verlangt aber „" + verlangt + "\"." +
                         Environment.NewLine + Environment.NewLine +
                         "Bitte einen passenden Speicher wählen oder die Verwendung in der " +
                         "Pufferspeicher-Verwaltung ändern.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Puffer, der als WÄRMEQUELLE dieser Anlage dient — über die neue Spalte
        /// <c>WQ_ID_Puffer</c> und (Altweg) über den Bezeichner in <c>WQ_Puffer</c>.
        /// 0, wenn die Anlage keinen Puffer als Quelle nutzt.
        /// </summary>
        public static int QuellPufferDerAnlage(int idProjekt, int idAnlage)
        {
            if (idAnlage <= 0) return 0;

            DataTable dt = StilleDb.Tabelle(
                "SELECT WQ_Typ, WQ_ID_Puffer, WQ_Puffer FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage));
            if (dt == null || dt.Rows.Count == 0) return 0;

            DataRow r = dt.Rows[0];
            if (!string.Equals(StilleDb.Text(StilleDb.Feld(r, "WQ_Typ")),
                               WaermequelleClass.TYP_PUFFER, StringComparison.Ordinal))
                return 0;

            int id = StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer"));
            if (id > 0) return id;

            // Altweg: Bezeichner. Deterministisch die kleinste ID, wie GetProjektId.
            string bezeichner = StilleDb.Text(StilleDb.Feld(r, "WQ_Puffer"));
            if (bezeichner.Length == 0) return 0;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?",
                StilleDb.Par("@bez", OleDbType.VarWChar, bezeichner),
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt)));
        }

        /// <summary>
        /// Warnung „Puffer wird geladen, aber sein Kanal hat keinen Bedarf" (Konzept 4.6).
        /// Geprüft wird der Brauchwasserkanal: ohne Zuordnung in
        /// <c>Z_Projekt_Brauchwasser</c> hat das Projekt keinen Warmwasseranteil.
        /// null = kein Hinweis.
        /// </summary>
        public static string KanalWarnung(int idProjekt, SenkeDaten d)
        {
            if (d == null || idProjekt <= 0) return null;

            bool brauchwasser =
                string.Equals(d.Ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal) ||
                (d.HatZweitsenke && string.Equals(d.Ziel2, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal));

            if (!brauchwasser) return null;
            if (ProjektHatBrauchwasser(idProjekt)) return null;

            return "Hinweis: Dem Projekt ist kein Brauchwasserbedarf zugeordnet." +
                   Environment.NewLine +
                   "Ein Brauchwasserspeicher wird dann zwar geladen, aber nie entladen.";
        }

        /// <summary>true, wenn dem Projekt mindestens ein Brauchwasser-Anteil zugeordnet ist.</summary>
        public static bool ProjektHatBrauchwasser(int idProjekt)
        {
            object v = StilleDb.Scalar(
                "SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));

            // Ist die Abfrage nicht auswertbar (fehlende Tabelle), NICHT warnen -
            // eine Warnung aus Unkenntnis ist schlechter als keine.
            if (v == null) return true;
            return StilleDb.Zahl(v) > 0;
        }

        // --- Anzeige ------------------------------------------------------------------

        /// <summary>Kompakte Anzeige der Hauptsenke für die Übersicht (Konzept 4.1).</summary>
        public static string HauptsenkeAnzeige(SenkeDaten d)
        {
            if (d == null) return "Heizkreis";

            if (IstPufferZiel(d.Ziel))
            {
                string name = PufferName(d.ID_Puffer);
                string kurz = string.Equals(d.Ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal)
                    ? "Puffer Heizung" : "Puffer Brauchw.";
                return name.Length > 0 ? kurz + ": " + name : kurz;
            }

            // Heizkreis: die Bedarfsart ist hier die Feinsteuerung (Konzept 3.1)
            switch (d.Bedarfsart)
            {
                case WaermequelleClass.SENKE_WARMWASSER: return "Heizkreis (nur Warmwasser)";
                case WaermequelleClass.SENKE_HEIZUNG: return "Heizkreis (nur Heizwärme)";
                default: return "Heizkreis (beides)";
            }
        }

        /// <summary>
        /// Kompakte Anzeige der Zweitsenke; „–" ohne Zweitsenke.
        ///
        /// Ein Ziel2, das kein Puffer-Ziel ist, gilt hier als KEINE Zweitsenke — dieselbe
        /// Regel, mit der <see cref="Normalisieren"/> es aus dem Datensatz entfernt. Der
        /// frühere Rückfall auf <c>ZielAnzeige(d.Ziel2)</c> war nach dem Normalisieren
        /// unerreichbar und hätte für nicht normalisierte Daten „Heizkreis" als
        /// Zweitsenke ausgewiesen — genau das, was Normalisieren verwirft.
        /// </summary>
        public static string ZweitsenkeAnzeige(SenkeDaten d)
        {
            if (d == null || !d.HatZweitsenke) return "–";
            if (!IstPufferZiel(d.Ziel2)) return "–";

            string name = PufferName(d.ID_Puffer2);
            string kurz = string.Equals(d.Ziel2, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal)
                ? "Puffer Heizung" : "Puffer Brauchw.";
            return name.Length > 0 ? kurz + ": " + name : kurz;
        }

        /// <summary>Bezeichner eines Puffers; "" wenn es ihn nicht gibt.</summary>
        public static string PufferName(int idPuffer)
        {
            if (idPuffer <= 0) return "";
            return StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer)));
        }

        // --- Übergangsbrücke auf Z_ProjektPufferSp (entfällt mit Paket 4) -------------

        /// <summary>
        /// ÜBERGANGSBRÜCKE (Etappe A von Konzept 4.4, entfällt mit Paket 4).
        ///
        /// Die Engine liest den Wärmepumpen-Pufferspeicher bis Paket 4 aus
        /// <c>Z_ProjektPufferSp</c> (<c>SimulationControl.Do_Simulation</c>: erste Zeile
        /// mit <c>Erzeuger = 'Wärmepumpe'</c> nach <c>Prioritaet</c>). Damit eine im
        /// Senkendialog (4.2) gesetzte Puffer-Senke sofort wirkt, spiegelt diese Methode
        /// das neue Modell auf die Alt-Zuordnung:
        ///
        ///   - Hauptsenke <c>PufferHeizung</c> an einer WP  ⇒ genau EINE WP-Zuordnungszeile
        ///     auf diesen Puffer (vorhandene Zeile auf denselben Puffer bleibt samt ihren
        ///     Schwellen erhalten, alle übrigen WP-Zeilen entfallen).
        ///   - keine WP mit Puffer-Senke                     ⇒ alle WP-Zuordnungszeilen weg.
        ///
        /// Zeilen anderer Erzeuger bleiben unberührt — die Engine überspringt sie ohnehin
        /// (<c>continue</c>), und Konzept 5.5/R2 hält fest, dass wirkungslose
        /// Altzuordnungen wirkungslos bleiben.
        ///
        /// Wird AUSSCHLIESSLICH aus Bedienhandlungen heraus gerufen, nie aus dem
        /// Rechenlauf — deshalb ist die Regression unberührt.
        /// </summary>
        /// <returns>true, wenn nichts schiefging.</returns>
        public static bool WpSenkeSpiegeln(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            // 1. Führende Wärmepumpe mit Puffer-Heizungs-Senke suchen. Die Reihenfolge ist
            //    dieselbe, mit der die Engine die Zuordnung auswählt (Prioritaet, ID).
            DataTable wp = StilleDb.Tabelle(
                "SELECT ID, WS_Ziel, WS_ID_Puffer FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@typ", OleDbType.Integer, ProjektPuffer.TYP_WP));

            int idPuffer = 0;
            if (wp != null)
            {
                foreach (DataRow r in wp.Rows)
                {
                    if (!string.Equals(StilleDb.Text(StilleDb.Feld(r, "WS_Ziel")),
                                       ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal)) continue;

                    int id = StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer"));
                    if (id > 0) { idPuffer = id; break; }
                }
            }

            // 2. Keine Puffer-Senke -> Alt-Zuordnung der Wärmepumpe entfernen
            if (idPuffer <= 0)
            {
                return StilleDb.NonQuery(
                    "DELETE FROM Z_ProjektPufferSp WHERE ID_Projekt = ? AND Erzeuger = ?",
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                    StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE)) >= 0;
            }

            PufferInfo p = PufferLesen(idPuffer);
            if (p == null) return false;

            // 3. Alle WP-Zeilen, die auf einen ANDEREN Puffer zeigen, entfallen
            StilleDb.NonQuery(
                "DELETE FROM Z_ProjektPufferSp WHERE ID_Projekt = ? AND Erzeuger = ? " +
                "AND (ID_Pufferspeicher IS NULL OR ID_Pufferspeicher <> ?)",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE),
                StilleDb.Par("@puf", OleDbType.Integer, idPuffer));

            // 4. Zeile auf DIESEN Puffer anlegen oder aktualisieren. Ein vorhandener
            //    Datensatz behält seine Schwellen (B0-1: sie überleben sonst nicht).
            int vorhanden = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM Z_ProjektPufferSp WHERE ID_Projekt = ? AND Erzeuger = ? " +
                "AND ID_Pufferspeicher = ?",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE),
                StilleDb.Par("@puf", OleDbType.Integer, idPuffer)));

            if (vorhanden > 0)
            {
                // TEMPERATUREN NUR BEI GÜLTIGEM PAAR NACHFÜHREN (Konzept 5.1, Stufenmodell).
                //
                // Der Puffer ist seit Etappe 4 die FÜHRENDE Ablage, die Zuordnung die
                // Rückfallstufe 2. Trägt der Puffer kein brauchbares Paar (beide Werte
                // gesetzt, Rücklauf > 0, Vorlauf > Rücklauf), liefert PufferInfo 0/0 —
                // und 0/0 in die Zuordnung zu schreiben LÖSCHT die Rückfallstufe. Die
                // Engine fiele danach auf ihre Vorgabespreizung von 10 K durch, obwohl in
                // der Zuordnung ein gepflegtes Paar stand. Deshalb: kein Paar am Puffer
                // ⇒ die Zuordnungswerte bleiben unangetastet, nur der Name wird geführt.
                if (!ProjektPuffer.IstTemperaturpaar(p.Vorlauf, p.Ruecklauf))
                {
                    return StilleDb.NonQuery(
                        "UPDATE Z_ProjektPufferSp SET Pufferspeicher = ? " +
                        "WHERE ID_Projekt = ? AND Erzeuger = ? AND ID_Pufferspeicher = ?",
                        StilleDb.Par("@bez", OleDbType.VarWChar, p.Bezeichner),
                        StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                        StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE),
                        StilleDb.Par("@puf", OleDbType.Integer, idPuffer)) >= 0;
                }

                return StilleDb.NonQuery(
                    "UPDATE Z_ProjektPufferSp SET Pufferspeicher = ?, Vorlauf = ?, Ruecklauf = ? " +
                    "WHERE ID_Projekt = ? AND Erzeuger = ? AND ID_Pufferspeicher = ?",
                    StilleDb.Par("@bez", OleDbType.VarWChar, p.Bezeichner),
                    StilleDb.Par("@vor", OleDbType.Integer, p.Vorlauf),
                    StilleDb.Par("@rue", OleDbType.Integer, p.Ruecklauf),
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                    StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE),
                    StilleDb.Par("@puf", OleDbType.Integer, idPuffer)) >= 0;
            }

            // Prioritaet 0: die Zeile steht damit vor allen übrigen Zuordnungen, und genau
            // die erste WP-Zeile wertet die Engine aus. Beim nächsten "Speichern" vergibt
            // Form_Simulation_Config die Prioritäten ohnehin neu in Listenreihenfolge.
            //
            // Hier NEUE Zeile: es gibt keinen Bestand, der geschont werden müsste. Hat der
            // Puffer kein Temperaturpaar, stehen in p.Vorlauf/p.Ruecklauf 0 — und 0/0 ist
            // in der Zuordnung genau die richtige Aussage „hier steht nichts", auf die die
            // Engine mit ihrer Vorgabespreizung antwortet.
            return StilleDb.NonQuery(
                "INSERT INTO Z_ProjektPufferSp " +
                "(ID_Projekt, ID_Pufferspeicher, Erzeuger, Pufferspeicher, Vorlauf, Ruecklauf, Prioritaet) " +
                "VALUES (?,?,?,?,?,?,?)",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                StilleDb.Par("@puf", OleDbType.Integer, idPuffer),
                StilleDb.Par("@erz", OleDbType.VarWChar, ProjektPuffer.ERZEUGER_WAERMEPUMPE),
                StilleDb.Par("@bez", OleDbType.VarWChar, p.Bezeichner),
                StilleDb.Par("@vor", OleDbType.Integer, p.Vorlauf),
                StilleDb.Par("@rue", OleDbType.Integer, p.Ruecklauf),
                StilleDb.Par("@prio", OleDbType.Integer, 0)) >= 0;
        }
    }
}
