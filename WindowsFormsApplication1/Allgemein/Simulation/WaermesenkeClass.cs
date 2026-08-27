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

        // Persistenzwerte; seit Paket 9 / L0 zentral in DbWerte geführt, hier nur Aliasse.

        /// <summary>Direkte Deckung des Momentanbedarfs — Verhalten wie bisher.</summary>
        public const string ZIEL_HEIZKREIS = DbWerte.WS_ZIEL_HEIZKREIS;

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Heizung".</summary>
        public const string ZIEL_PUFFER_HEIZUNG = DbWerte.WS_ZIEL_PUFFER_HEIZUNG;

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Brauchwasser".</summary>
        public const string ZIEL_PUFFER_BRAUCHWASSER = DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER;

        /// <summary>
        /// Die Anlage lädt einen KOMBISPEICHER (Etappe D5a, Konzept_KonfigUI_Hydraulik
        /// Anforderungen 4/7): ein Wärmevorrat für Heizung UND Warmwasser.
        /// </summary>
        public const string ZIEL_PUFFER_KOMBI = DbWerte.WS_ZIEL_PUFFER_KOMBI;

        /// <summary>
        /// DIREKTSENKE PROZESSWÄRME (Paket S1, Konzept 4.4/5.1): Die Anlage deckt den
        /// Prozesskanal unmittelbar. Sie kommt ausschließlich in <c>Z_AnlageSenke</c>
        /// vor — die Altspalte <c>WS_Ziel</c> kennt sie nicht.
        /// </summary>
        public const string ZIEL_PROZESSWAERME = DbWerte.WS_ZIEL_PROZESS;

        /// <summary>
        /// Die Anlage lädt einen Puffer für PROZESSWÄRME (Paket S1, Konzept 5.1 — das
        /// sechste Senkenziel der Leitentscheidung L5).
        /// </summary>
        public const string ZIEL_PUFFER_PROZESS = DbWerte.WS_ZIEL_PUFFER_PROZESS;

        // --- Verwendung eines Projekt-Puffers (Konzept 5.1) ---------------------------

        public const string VERWENDUNG_HEIZUNG = DbWerte.PSP_VERWENDUNG_HEIZUNG;
        public const string VERWENDUNG_BRAUCHWASSER = DbWerte.PSP_VERWENDUNG_BRAUCHWASSER;

        /// <summary>Kombispeicher — bedient BEIDE Kanäle aus einem Vorrat (D5a).</summary>
        public const string VERWENDUNG_KOMBI = DbWerte.PSP_VERWENDUNG_KOMBI;

        // Eine eigene Liste der Erzeugertypen stand hier ursprünglich als
        // ERZEUGER_TYPEN. Sie wurde von niemandem gelesen: Wer die Typen braucht,
        // nimmt ProjektPuffer.WAERMEERZEUGER_TYPEN (die SQL-taugliche Fassung, die
        // Ladeordnung und ProjektPuffer bereits benutzen). Zwei Wahrheiten über
        // dieselbe Menge sind eine Fehlerquelle - die tote wurde entfernt.

        /// <summary>
        /// true, wenn das Ziel einen Pufferspeicher meint.
        ///
        /// PAKET S1: <see cref="ZIEL_PUFFER_PROZESS"/> kommt dazu. <see cref="ZIEL_PROZESSWAERME"/>
        /// ausdrücklich NICHT — das ist eine DIREKTsenke und darf in der Altspaltenpflege
        /// (<see cref="Normalisieren"/>) nicht als Puffer-Ziel durchgehen.
        /// </summary>
        public static bool IstPufferZiel(string ziel)
        {
            return string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal) ||
                   string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal) ||
                   string.Equals(ziel, ZIEL_PUFFER_KOMBI, StringComparison.Ordinal) ||
                   string.Equals(ziel, ZIEL_PUFFER_PROZESS, StringComparison.Ordinal);
        }

        /// <summary>true, wenn die Verwendung einen KOMBISPEICHER meint (D5a).</summary>
        public static bool IstKombiVerwendung(string verwendung)
        {
            return string.Equals(verwendung, VERWENDUNG_KOMBI, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verwendung, die ein Puffer für dieses Ziel haben muss; null bei Heizkreis.</summary>
        public static string VerwendungZuZiel(string ziel)
        {
            if (string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return VERWENDUNG_HEIZUNG;
            if (string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return VERWENDUNG_BRAUCHWASSER;
            if (string.Equals(ziel, ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return VERWENDUNG_KOMBI;
            return null;
        }

        /// <summary>Deutscher Anzeigename eines Ziels.</summary>
        public static string ZielAnzeige(string ziel)
        {
            if (string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_HEIZUNG;
            if (string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER;
            if (string.Equals(ziel, ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_KOMBI;
            return MyResource.Resource.SIM_HEIZKREIS;
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

            /// <summary>
            /// PAKET PARALLELVERBUND (Entscheidung des Anwenders 17.08.2026): die
            /// ZUSÄTZLICHEN Pufferspeicher, die mit <see cref="ID_Puffer"/> EINEN
            /// gemeinsamen Wärmevorrat bilden — Kapazitäten addiert, ein Füllstand, eine
            /// Schaltschwelle.
            ///
            /// <see cref="ID_Puffer"/> ist der LEITSPEICHER: Er steht weiter in
            /// <c>WS_ID_Puffer</c>, trägt die Schwellen und die Entladepriorität des
            /// Verbunds und ist die ID, unter der Rechenobjekt und Ergebniszeile laufen.
            /// Diese Liste enthält ihn ausdrücklich NICHT.
            ///
            /// <b>Leer = kein Verbund</b> und damit exakt das Verhalten vor dem Paket. Die
            /// Liste ist nie <c>null</c>.
            ///
            /// <b>Nur die HAUPTsenke kennt einen Verbund.</b> Die Zweitsenke bleibt EIN
            /// Ziel (Entscheidung des Anwenders): Sie verwertet Überschuss, und ein
            /// zweiter Vorrat mit eigener Schwellenlogik an dieser Stelle wäre eine
            /// Rechenänderung ohne fachlichen Auftrag.
            /// </summary>
            public List<int> VerbundMitglieder = new List<int>();

            /// <summary>true, wenn die Hauptsenke ein Parallelverbund aus mehreren Speichern ist.</summary>
            public bool HatVerbund
            {
                get { return VerbundMitglieder != null && VerbundMitglieder.Count > 0; }
            }

            public SenkeDaten Kopie()
            {
                SenkeDaten k = (SenkeDaten)MemberwiseClone();

                // MemberwiseClone kopiert die LISTENREFERENZ. Ohne diese Zeile teilten
                // Original und Kopie dieselbe Mitgliederliste, und der Ersatzdatensatz der
                // Kaskaden-Automatik (KonfigurationCtrl.KaskadeNotwendig normalisiert eine
                // Kopie) könnte den Dialogstand verändern, aus dem er gebildet wurde.
                k.VerbundMitglieder = VerbundMitglieder == null
                    ? new List<int>()
                    : new List<int>(VerbundMitglieder);
                return k;
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

            /// <summary>
            /// <c>Schwelle_Reserve</c> — Mindestfüllstand/Notreserve [%] (Paket
            /// BHKW-Regulär). Vorbelegung
            /// <see cref="Ladeordnung.SCHWELLE_RESERVE_DEFAULT"/>, wirksam ausschließlich
            /// im BHKW-Pfad.
            /// </summary>
            public double SchwelleReserve = Ladeordnung.SCHWELLE_RESERVE_DEFAULT;

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
                    ? string.Format(MyResource.Resource.SIM_PUFFER_MIT_VOLUMEN, Bezeichner, Gesamtvolumen)
                    : Bezeichner;
            }
        }

        // --- Lesen und Schreiben ------------------------------------------------------

        /// <summary>
        /// Liest die Senkenfelder einer Anlage; nie <c>null</c>.
        ///
        /// PAKET PARALLELVERBUND: Zusätzlich zur Anlagenzeile kommen die
        /// <see cref="SenkeDaten.VerbundMitglieder"/> aus <c>Z_AnlagePufferVerbund</c> mit —
        /// eine zweite Abfrage, und genau deshalb steht sie HIER und nicht in
        /// <see cref="AusDatenzeile"/>: Die Erzeuger-Übersicht baut ihre Karten aus EINER
        /// Projektabfrage über <c>AusDatenzeile</c>, und ein Verbund-Nachschlag je Zeile
        /// wäre dort ein N+1 auf einer Anzeigefläche. Die Karten brauchen den Verbund
        /// ohnehin nur als Zusatz am Chip und holen ihn punktuell.
        /// </summary>
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

            d = AusDatenzeile(dt.Rows[0]);
            d.VerbundMitglieder = VerbundLesen(idAnlage);

            // Nach dem Nachladen noch einmal normalisieren: Erst jetzt sind Leitspeicher
            // UND Mitglieder beisammen, und nur so fällt eine Zeile auf, die auf den
            // Leitspeicher selbst zeigt (Altbestand, von Hand eingetragen).
            Normalisieren(d);
            return d;
        }

        /// <summary>
        /// Die zusätzlichen Verbundmitglieder einer Anlage (Paket Parallelverbund); nie
        /// <c>null</c>, ohne Verbund leer.
        ///
        /// Die EINE Auflösungsstelle für alle Leser — Dialog, Registry-Speisung und
        /// Anzeigen greifen hierüber zu, damit es keine zweite Auslegung der
        /// Zuordnungstabelle gibt. Der Datenzugriff selbst steckt in
        /// <see cref="AnlagePufferVerbundCtrl"/> (Controller-Schicht).
        /// </summary>
        public static List<int> VerbundLesen(int idAnlage)
        {
            return AnlagePufferVerbundCtrl.MitgliederLesen(idAnlage);
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
        ///
        /// UND SEIT DER PAKET-5-NACHARBEIT (Befund N5) auch die Gegenrichtung: ein
        /// Puffer-ZIEL ohne Puffer-REFERENZ. Diese halbe Konfiguration entsteht aus
        /// Altdaten und aus abgebrochenen Dialogeingaben, und sie war der stille
        /// Totalausfall eines Erzeugers: Die Engine erkennt ihn (mangels
        /// <c>WS_ID_Puffer</c>) nicht als ladende Anlage — er bekommt also keinen
        /// Ladeauftrag —, aber die Bedarfsphase überspringt ihn, weil seine Hauptsenke
        /// nicht der Heizkreis ist. Ergebnis: Er produziert das ganze Jahr nichts, ohne
        /// jeden Hinweis (gemessen an einem präparierten 1018: Kesselproduktion
        /// 34,27 -> 0 MWh). Ein Ziel ohne Ziel ist kein Ziel — es gilt der Heizkreis.
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

            // N5: Puffer-Ziel OHNE Puffer -> Heizkreis (siehe Kopfkommentar). Steht hier
            // am Ende, damit die Negativ-Klemmung oben schon gelaufen ist.
            if (IstPufferZiel(d.Ziel) && d.ID_Puffer <= 0)
            {
                d.Ziel = ZIEL_HEIZKREIS;
                d.Ladeprio = 0;
                d.Ladegrenze = 0;
                d.LadeprioPV = 0;
            }

            if (d.HatZweitsenke && d.ID_Puffer2 <= 0)
            {
                d.Ziel2 = "";
                d.Ladeprio2 = 0;
                d.Ladegrenze2 = 0;
            }

            VerbundNormalisieren(d);
        }

        /// <summary>
        /// Räumt die Mitgliederliste des Parallelverbunds auf — dieselbe Denkweise wie für
        /// die Puffer-Slots eine Ebene höher: Was fachlich nicht sein kann, wird
        /// STILL entfernt, nicht dem Aufrufer als Fehler zugestellt.
        ///
        /// Vier Regeln, alle aus der Konfliktregel des Pakets:
        ///   1. OHNE Puffer-Ziel gibt es keinen Verbund. Steht die Hauptsenke auf
        ///      Heizkreis (auch nach der N5-Rückstufung oben), ist die Liste
        ///      gegenstandslos — sonst blieben Verbundzeilen an einer Anlage hängen, die
        ///      gar keinen Speicher lädt, und die Registry-Speisung fände Mitglieder ohne
        ///      Leitspeicher.
        ///   2. Der LEITSPEICHER ist kein Mitglied seiner selbst. Eine solche Zeile käme
        ///      aus Altbestand oder Handeintrag und verdoppelte die Kapazität des
        ///      Leitspeichers.
        ///   3. Keine Doppelnennung — ein Behälter zählt einmal.
        ///   4. Die ZWEITSENKE derselben Anlage kann nicht Mitglied des Hauptverbunds
        ///      sein: Sie ist ein eigenes Ladeziel mit eigener Priorität und eigener
        ///      Obergrenze. Diese Regel weist <see cref="Pruefen"/> zwar ausdrücklich mit
        ///      Meldung ab; sie steht ZUSÄTZLICH hier, weil Normalisieren auch auf
        ///      gelesenen Beständen läuft, die niemand mehr durch den Dialog schickt.
        ///
        /// Nicht geprüft wird hier, ob ein Mitglied zum Projekt gehört, die richtige
        /// Verwendung trägt oder anderweitig belegt ist — das braucht Datenbankzugriffe
        /// und gehört deshalb in <see cref="Pruefen"/> bzw. in die Engine-Warnung.
        /// Normalisieren bleibt eine reine Feldregel ohne SQL (Bestandsverhalten).
        /// </summary>
        private static void VerbundNormalisieren(SenkeDaten d)
        {
            if (d.VerbundMitglieder == null)
            {
                d.VerbundMitglieder = new List<int>();
                return;
            }

            if (!IstPufferZiel(d.Ziel) || d.ID_Puffer <= 0)
            {
                d.VerbundMitglieder.Clear();
                return;
            }

            List<int> sauber = new List<int>();
            foreach (int id in d.VerbundMitglieder)
            {
                if (id <= 0) continue;
                if (id == d.ID_Puffer) continue;                       // Regel 2
                if (sauber.Contains(id)) continue;                     // Regel 3
                if (d.HatZweitsenke && id == d.ID_Puffer2) continue;   // Regel 4
                sauber.Add(id);
            }

            d.VerbundMitglieder = sauber;
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

            // PAKET PARALLELVERBUND: die Mitglieder in EINEM Zug mit den Senkenfeldern.
            //
            // Das ist ausdrücklich Teil DIESER Methode und keine zweite Speicherstelle im
            // Dialog: Leitspeicher und Mitglieder gehören zusammen, und die Aufrufer
            // (Konfigurationsdialog, WpSenkeSpiegeln-Umfeld) müssen dafür nichts wissen.
            // IMMER geschrieben, auch die leere Liste - das ist der Weg, auf dem ein
            // Verbund im Dialog wieder aufgelöst wird (Delete/Insert in
            // AnlagePufferVerbundCtrl.Schreiben).
            ok &= AnlagePufferVerbundCtrl.Schreiben(idAnlage, d.VerbundMitglieder);

            return ok;
        }

        /// <summary>0 → DBNull (Fremdschlüssel), sonst die ID.</summary>
        private static object IdOderNull(int id)
        {
            return id > 0 ? (object)id : DBNull.Value;
        }

        /// <summary>
        /// Zieht die Vorbelegung der Ladeprioritäten für ein Projekt nach: <c>NULL</c> wird
        /// zu <c>0</c> („nach Vorgabe" bzw. „nicht gesetzt", Konzept 3.4).
        ///
        /// Dieselbe Anweisung wie Migrationsregel R5 — nur läuft die genau einmal je
        /// Datenbank. Anlagen, die DANACH entstehen, tragen wieder NULL: Die
        /// INSERT-Anweisungen der Erzeugerpfade führen diese Spalten nicht, und zwei von
        /// ihnen (<c>WizardCtrl</c>, <c>Form_BHKWEing</c>/<c>Form_Heizkessel</c>) sind für
        /// diese Paketarbeit gesperrt. Genau das hat der Schema-Nachweis des
        /// Referenzlaufs sichtbar gemacht („Anlagen ohne Ladeprio-Vorgabe: 2" — zwei über
        /// die Oberfläche angelegte Erzeuger in Projekt 1024).
        ///
        /// RECHNERISCH ändert sich dadurch nichts: <c>StilleDb.Zahl(NULL)</c> liefert 0,
        /// die Engine behandelt beides gleich. Es geht um die Konsistenz des Bestands —
        /// und darum, dass der Nachweis wieder das misst, wofür er gedacht ist.
        ///
        /// Dialogfrei (Konzept 13.4) und fehlertolerant: Fehlt eine Spalte, bleibt es beim
        /// Bestand. Aufgerufen am Engine-Einstieg, wo auch das Schema sichergestellt wird.
        /// </summary>
        /// <returns>Zahl der geänderten Zeilen; 0 auch im Fehlerfall.</returns>
        public static int VorbelegungNachziehen(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            int summe = 0;
            foreach (string spalte in new[]
                     { "WS_Ladeprio", "WS_Ladeprio2", "WS_Ladeprio_PV", "WS_Ladegrenze", "WS_Ladegrenze2" })
            {
                int n = StilleDb.NonQuery(
                    "UPDATE Tab_Energieanlagen SET [" + spalte + "] = 0 " +
                    "WHERE ID_Projekt = ? AND [" + spalte + "] IS NULL",
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
                if (n > 0) summe += n;
            }

            return summe;
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
            return ProjektPufferListe(idProjekt, verwendung, false);
        }

        /// <summary>
        /// Dieselbe Liste mit zwei UNTERSCHIEDLICHEN Fragestellungen (Etappe D5a).
        ///
        /// <para><b><paramref name="kanalSicht"/> = false — „welcher Puffer passt zu
        /// diesem SENKENZIEL?"</b> Gefiltert wird auf exakte Gleichheit der Verwendung.
        /// Das ist die Frage der Dialoge: Ein Kombi-Ziel verlangt einen Kombi-Puffer, ein
        /// Heizungs-Ziel einen Heizungs-Puffer (Konzept Abschnitt 7), und
        /// <see cref="PufferPasst"/> prüft genau dasselbe beim Speichern.</para>
        ///
        /// <para><b><paramref name="kanalSicht"/> = true — „welche Speicher BEDIENEN
        /// diesen Kanal?"</b> Ein KOMBISPEICHER bedient beide Kanäle aus einem Vorrat und
        /// erscheint deshalb in beiden Listen. Das ist die Frage der Entladereihenfolge
        /// (<see cref="Ladeordnung.Entladereihenfolge"/>, Konzept 3.6) und der
        /// Kanalanzeigen.</para>
        ///
        /// Ohne Kombispeicher im Projekt — jeder Bestandsdatenbestand — liefern beide
        /// Formen dieselbe Liste.
        /// </summary>
        public static List<PufferInfo> ProjektPufferListe(int idProjekt, string verwendung,
                                                          bool kanalSicht)
        {
            List<PufferInfo> liste = new List<PufferInfo>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Verwendung, Gesamtvolumen, Bereitschaftsverluste, " +
                "       Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, Schwelle_Aus_Nachrang, Entladeprio, " +
                "       Schwelle_Reserve " +
                "FROM Tab_Pufferspeicher WHERE ID_Projekt = ? ORDER BY Bezeichner, ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                PufferInfo p = AusZeile(r);
                if (string.IsNullOrEmpty(verwendung) || PasstZuFilter(p, verwendung, kanalSicht))
                    liste.Add(p);
            }

            return liste;
        }

        /// <summary>Filterregel von <see cref="ProjektPufferListe"/>; siehe dort.</summary>
        private static bool PasstZuFilter(PufferInfo p, string verwendung, bool kanalSicht)
        {
            string wirksam = WirksameVerwendung(p);

            if (string.Equals(wirksam, verwendung, StringComparison.OrdinalIgnoreCase))
                return true;

            // D5a: Der Kombispeicher bedient BEIDE Kanäle - aber nur, wenn nach dem Kanal
            // gefragt ist. Als Senkenziel bleibt er dem Ziel „PufferKombi" vorbehalten.
            return kanalSicht && IstKombiVerwendung(wirksam) &&
                   (string.Equals(verwendung, VERWENDUNG_HEIZUNG, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(verwendung, VERWENDUNG_BRAUCHWASSER, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Ein einzelner Projekt-Puffer; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public static PufferInfo PufferLesen(int idPuffer)
        {
            if (idPuffer <= 0) return null;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Verwendung, Gesamtvolumen, Bereitschaftsverluste, " +
                "       Vorlauf, Ruecklauf, Schwelle_Ein, Schwelle_Aus, Schwelle_Aus_Nachrang, Entladeprio, " +
                "       Schwelle_Reserve " +
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

            // PAKET BHKW-REGULÄR: Mindestfüllstand/Notreserve [%]. NULL bedeutet hier
            // „nicht gepflegt" und wird auf die Vorbelegung gehoben - dieselbe Regel wie
            // bei den drei Schaltschwellen darüber, und dieselbe 10, die
            // Migrationsschritt 13 in den Bestand schreibt.
            //
            // ABGRENZUNG zu den Schwellen: Hier wird NICHT „<= 0 -> Default" geprüft. Eine
            // ausdrückliche 0 ist die zulässige Aussage „dieser Speicher darf leergefahren
            // werden"; sie stammt dann vom Anwender und darf nicht überschrieben werden.
            // Nur das FEHLEN eines Werts wird vorbelegt, und das leistet der
            // Default-Parameter von StilleDb.Kommazahl.
            p.SchwelleReserve = StilleDb.Kommazahl(StilleDb.Feld(r, "Schwelle_Reserve"),
                                                   Ladeordnung.SCHWELLE_RESERVE_DEFAULT);
            if (p.SchwelleReserve < 0) p.SchwelleReserve = 0;

            if (p.SchwelleEin <= 0) p.SchwelleEin = Ladeordnung.SCHWELLE_EIN_DEFAULT;
            if (p.SchwelleAus <= 0) p.SchwelleAus = Ladeordnung.SCHWELLE_AUS_DEFAULT;
            if (p.SchwelleAusNachrang <= 0) p.SchwelleAusNachrang = p.SchwelleAus;
            return p;
        }

        /// <summary>
        /// Senkenzuordnungen ALLER Wärmeerzeuger eines Projekts in Kaskadenreihenfolge
        /// (Konzept 6.1) — die Form, in der die Engine sie ab Etappe 4b braucht.
        ///
        /// Gegenüber <see cref="Lesen"/> zwei Unterschiede: EINE Abfrage für das ganze
        /// Projekt statt einer je Anlage, und das Ergebnis ist die Rechendarstellung
        /// <see cref="Senkenzuordnung"/> (enum <see cref="Senke"/>) statt der
        /// Datenbanksicht <see cref="SenkeDaten"/>. Normalisiert wird über denselben Weg
        /// (<see cref="AusDatenzeile"/>), damit es keine zweite Auslegung der Felder gibt.
        ///
        /// Dialogfrei (Konzept 13.4). Fehlende Spalten liefern eine leere Liste statt
        /// einer MessageBox; nie <c>null</c>.
        ///
        /// Aufgerufen wird sie von <c>SimulationControl</c> je Lauf.
        ///
        /// <b>SEIT PAKET S1 Übergangsbestand:</b> Der dreikanalige Weg rechnet mit
        /// <see cref="SenkenlistenLaden"/>. Diese Fassung bleibt für das BHKW-Modul bis
        /// zu seinem eigenen Umbau; sie fällt mit dem Altpfad (Paket A1).
        /// </summary>
        public static List<Senkenzuordnung> SenkenLaden(int idProjekt)
        {
            List<Senkenzuordnung> liste = new List<Senkenzuordnung>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, WS_Ziel, WS_ID_Puffer, WS_Typ, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, " +
                "       WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2 " +
                "FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                SenkeDaten d = AusDatenzeile(r);            // enthält Normalisieren
                Senkenzuordnung z = new Senkenzuordnung();

                z.AnlagenID = StilleDb.Zahl(StilleDb.Feld(r, "ID"));

                // N5 (Paket-5-Nacharbeit): Eine halbe Puffer-Konfiguration - Ziel gesetzt,
                // Puffer nicht - hat Normalisieren gerade auf den Heizkreis zurückgesetzt.
                // Das ist die Rettung des Erzeugers vor dem stillen Totalausfall, aber es
                // ist auch eine stille Datenkorrektur: Sie gehört ins Lauf-Protokoll.
                //
                // PROTOKOLLKANAL-NACHZUG (Folgepaket zu Paket 9, Befund N9b): Genau das
                // war bis hierher NICHT umgesetzt - die Meldung ging nur auf die Konsole
                // und erreichte weder die Detailansicht noch die Zählung der
                // Referenzlauf-Suite. Sie läuft jetzt über den WARNUNGS-Kanal aus Paket 8
                // (Ersatzannahme statt hinterlegter Konfiguration); die Konsolenzeile
                // bleibt erhalten, sie steckt in SimulationProtokoll.Eintragen.
                // Je Anlage nur einmal: SenkenLaden läuft je Lauf einmal, der Schlüssel
                // schützt gegen einen zweiten Aufruf im selben Lauf.
                string rohZiel = StilleDb.Text(StilleDb.Feld(r, "WS_Ziel"));
                if (IstPufferZiel(rohZiel) && !IstPufferZiel(d.Ziel))
                    SimulationProtokoll.Aktuell.WarnungEinmal(
                        "senke-haupt-ohne-puffer-" + z.AnlagenID,
                        "Wärmesenke: Die Anlage " + z.AnlagenID + " ist auf " + rohZiel +
                        " gesetzt, hat aber KEINEN Pufferspeicher zugeordnet " +
                        "(WS_ID_Puffer leer). Sie rechnet deshalb auf den HEIZKREIS.");

                string rohZiel2 = StilleDb.Text(StilleDb.Feld(r, "WS_Ziel2"));
                if (IstPufferZiel(rohZiel2) && !d.HatZweitsenke)
                    SimulationProtokoll.Aktuell.WarnungEinmal(
                        "senke-zweit-ohne-puffer-" + z.AnlagenID,
                        "Wärmesenke: Die Anlage " + z.AnlagenID + " hat eine Zweitsenke " +
                        rohZiel2 + " ohne zugeordneten Pufferspeicher (WS_ID_Puffer2 " +
                        "leer). Die Zweitsenke bleibt unberücksichtigt.");
                z.Haupt = Senkenzuordnung.SenkeAusZiel(d.Ziel);
                z.IDPufferHaupt = d.ID_Puffer;
                z.WSTyp = d.Bedarfsart;

                // Nach Normalisieren gilt: HatZweitsenke ⇒ Ziel2 ist ein Puffer-Ziel.
                z.Zweit = d.HatZweitsenke ? (Senke?)Senkenzuordnung.SenkeAusZiel(d.Ziel2) : null;
                z.IDPufferZweit = d.HatZweitsenke ? d.ID_Puffer2 : 0;

                liste.Add(z);
            }

            return liste;
        }

        // ==================================================================
        // GEORDNETE SENKENLISTEN (Paket S1, Konzept 5.1) — die Nachfolge von
        // SenkenLaden für den dreikanaligen Weg
        // ==================================================================

        /// <summary>
        /// Die geordneten SENKENLISTEN aller Wärmeerzeuger eines Projekts, in
        /// Kaskadenreihenfolge (Paket S1, Konzept 5.1/5.2) — die Form, in der die Engine
        /// sie ab S1 braucht.
        ///
        /// <para><b>Quelle ist <c>Z_AnlageSenke</c></b>: n Zeilen je Anlage mit Rang,
        /// Ziel, Bedarfsart, Puffer und den Ladeparametern. Damit fällt die Beschränkung
        /// auf zwei Senkenplätze (<c>WS_*</c> / <c>WS_*2</c>), und Direktsenken sind auch
        /// ab Rang 2 möglich („Puffer zuerst, Rest direkt" — bis S1 nicht abbildbar).</para>
        ///
        /// <para><b>Normalisierung</b> — dieselbe Denkweise wie in
        /// <see cref="Normalisieren"/>: Was fachlich nicht sein kann, wird still
        /// begradigt und protokolliert, statt den Lauf abzubrechen (Konzept 13.4,
        /// dialogfrei).</para>
        /// <list type="number">
        /// <item>Unbekanntes <c>Ziel</c> → <see cref="ZIEL_HEIZKREIS"/>
        ///       (<see cref="Senkenzuordnung.SenkeAusZiel"/>, Konzept 4.6).</item>
        /// <item>Puffer-Ziel OHNE <c>ID_Puffer</c> → Heizkreis + Protokollwarnung. Das ist
        ///       Befund N5 auf der neuen Tabelle: Ohne Puffer entsteht kein Ladeauftrag,
        ///       und ohne Direktsenke deckte die Anlage auch nichts — sie produzierte das
        ///       ganze Jahr nichts, ohne jeden Hinweis.</item>
        /// <item>KEINE Zeile für eine Anlage → eine Zeile <c>Heizkreis/Beides</c>
        ///       (RANG-1-INVARIANTE, Konzept 5.1) + Protokollwarnung.</item>
        /// <item>Ränge werden nach dem Sortieren LÜCKENLOS neu vergeben
        ///       (<see cref="Senkenliste.Ordnen"/>): Die Ladephasen laufen über
        ///       Rang-Ebenen, eine Lücke wäre eine leere Phase.</item>
        /// </list>
        ///
        /// <para><b>RÜCKFALL OHNE TABELLE</b> (<c>Z_AnlageSenkeCtrl.SpalteVorhanden() ==
        /// false</c>, also eine Datenbank vor Migrationsschritt 50): gelesen werden die
        /// bisherigen <c>WS_*</c>-Spalten, in Listenform mit Rang 1 (Hauptsenke) und
        /// Rang 2 (Zweitsenke). Das ist Zeile für Zeile das Bestandsverhalten — und es ist
        /// der Grund, warum dieser Zweig überhaupt existiert: Auf einem halb migrierten
        /// Schema darf die Engine nicht raten, sondern muss mit den Daten rechnen, die
        /// wirklich da sind. Der Rückfall kennt naturgemäß keine Prozesswärme-Senken
        /// (die Altspalten haben dafür keinen Wert) und keine Ränge über 2.</para>
        ///
        /// Dialogfrei; nie <c>null</c>, nie ein <c>null</c>-Eintrag.
        /// </summary>
        public static List<Senkenliste> SenkenlistenLaden(int idProjekt)
        {
            List<Senkenliste> listen = new List<Senkenliste>();
            if (idProjekt <= 0) return listen;

            // Anlagen des Projekts in KASKADENREIHENFOLGE - dieselbe Abfrage und
            // dieselbe Sortierung wie in SenkenLaden, damit die Reihenfolge der Listen
            // zwischen beiden Wegen identisch bleibt.
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, WS_Ziel, WS_ID_Puffer, WS_Typ, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV, " +
                "       WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2 " +
                "FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
            if (dt == null) return listen;

            bool tabelleDa = Z_AnlageSenkeCtrl.SpalteVorhanden();

            if (!tabelleDa)
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "senkenliste-rueckfall-altspalten",
                    "Wärmesenken: Die Zuordnungstabelle Z_AnlageSenke ist in dieser " +
                    "Datenbank noch nicht angelegt (Migrationsschritt 50 nicht gelaufen). " +
                    "Der Lauf rechnet deshalb mit den Altspalten WS_Ziel/WS_Ziel2 in " +
                    "Listenform (Rang 1 und 2) - das ist das bisherige Verhalten. Senken " +
                    "ab Rang 3 und Prozesswärme-Senken gibt es auf diesem Schema nicht.");

            // Eine Abfrage für das GANZE Projekt statt einer je Anlage - dieselbe
            // Abwägung wie bei SenkenLaden gegenüber Lesen().
            List<Z_AnlageSenkeModel> zeilen =
                tabelleDa ? new Z_AnlageSenkeCtrl().LesenJeProjekt(idProjekt) : null;

            foreach (DataRow r in dt.Rows)
            {
                int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"));

                Senkenliste liste = tabelleDa
                    ? AusZuordnungstabelle(idAnlage, zeilen)
                    : AusAltspalten(idAnlage, r);

                listen.Add(liste);
            }

            return listen;
        }

        /// <summary>
        /// Senkenliste einer Anlage aus den gelesenen <c>Z_AnlageSenke</c>-Zeilen; ohne
        /// eigene Zeile die Rang-1-Vorbelegung mit Protokollwarnung (siehe
        /// <see cref="SenkenlistenLaden"/>).
        /// </summary>
        private static Senkenliste AusZuordnungstabelle(int idAnlage,
                                                        List<Z_AnlageSenkeModel> zeilen)
        {
            Senkenliste liste = new Senkenliste();
            liste.AnlagenID = idAnlage;

            if (zeilen != null)
                foreach (Z_AnlageSenkeModel m in zeilen)
                {
                    if (m == null || m.ID_Anlage != idAnlage) continue;

                    Senkenzeile z = new Senkenzeile();
                    z.Rang = m.Rang > 0 ? m.Rang : liste.Zeilen.Count + 1;
                    z.Ziel = Senkenzuordnung.SenkeAusZiel(m.Ziel);
                    z.IDPuffer = m.ID_Puffer > 0 ? m.ID_Puffer : 0;
                    z.Bedarfsart = string.IsNullOrEmpty(m.Bedarfsart)
                        ? WaermequelleClass.SENKE_BEIDES : m.Bedarfsart;
                    z.Ladeprio = m.Ladeprio;
                    z.LadeprioPV = m.Ladeprio_PV;
                    z.LadegrenzeProzent = m.Ladegrenze > 0 ? m.Ladegrenze : 0;

                    // N5 auf der neuen Tabelle: Puffer-Ziel ohne Puffer ist kein Ziel.
                    if (z.IstPuffersenke && z.IDPuffer <= 0)
                    {
                        SimulationProtokoll.Aktuell.WarnungEinmal(
                            "senkenzeile-ohne-puffer-" + idAnlage + "-" + z.Rang,
                            "Wärmesenke: Die Anlage " + idAnlage + " führt auf Rang " +
                            z.Rang + " das Ziel " + Senkenzuordnung.ZielAusSenke(z.Ziel) +
                            ", hat dort aber KEINEN Pufferspeicher zugeordnet " +
                            "(Z_AnlageSenke.ID_Puffer leer). Die Zeile rechnet deshalb auf " +
                            "den HEIZKREIS.");

                        z.Ziel = Senke.Heizkreis;
                        z.IDPuffer = 0;
                        z.Ladeprio = 0;
                        z.LadeprioPV = 0;
                        z.LadegrenzeProzent = 0;
                    }

                    ZeileKlemmen(z);
                    liste.Zeilen.Add(z);
                }

            if (liste.Zeilen.Count == 0)
            {
                // RANG-1-INVARIANTE (Konzept 5.1): Die Engine rechnet Heizkreis/Beides und
                // sagt es. Ohne diese Zeile hätte die Anlage überhaupt kein Ziel.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "senkenliste-leer-" + idAnlage,
                    "Wärmesenke: Für die Anlage " + idAnlage + " steht in Z_AnlageSenke " +
                    "keine einzige Zeile. Der Lauf rechnet die Vorbelegung " +
                    ZIEL_HEIZKREIS + "/" + WaermequelleClass.SENKE_BEIDES + ".");

                return Senkenliste.Vorbelegung(idAnlage);
            }

            liste.Ordnen();
            return liste;
        }

        /// <summary>
        /// RÜCKFALL OHNE <c>Z_AnlageSenke</c>: dieselbe Anlagenzeile, aber aus den
        /// Altspalten <c>WS_*</c> / <c>WS_*2</c> in Listenform (Rang 1 / Rang 2).
        ///
        /// Normalisiert wird über <see cref="AusDatenzeile"/> — kein zweiter Regelsatz
        /// für dieselben Felder. Eine Zweitsenke ist dort konstruktiv immer ein
        /// Puffer-Ziel mit gültigem Puffer.
        /// </summary>
        private static Senkenliste AusAltspalten(int idAnlage, DataRow r)
        {
            SenkeDaten d = AusDatenzeile(r);           // enthält Normalisieren

            Senkenliste liste = new Senkenliste();
            liste.AnlagenID = idAnlage;

            Senkenzeile eins = new Senkenzeile();
            eins.Rang = 1;
            eins.Ziel = Senkenzuordnung.SenkeAusZiel(d.Ziel);
            eins.IDPuffer = d.ID_Puffer;
            eins.Bedarfsart = d.Bedarfsart;
            eins.Ladeprio = d.Ladeprio;
            eins.LadeprioPV = d.LadeprioPV;
            eins.LadegrenzeProzent = d.Ladegrenze;
            ZeileKlemmen(eins);
            liste.Zeilen.Add(eins);

            if (d.HatZweitsenke)
            {
                Senkenzeile zwei = new Senkenzeile();
                zwei.Rang = 2;
                zwei.Ziel = Senkenzuordnung.SenkeAusZiel(d.Ziel2);
                zwei.IDPuffer = d.ID_Puffer2;
                zwei.Bedarfsart = WaermequelleClass.SENKE_BEIDES;
                zwei.Ladeprio = d.Ladeprio2;
                // WS_Ladeprio_PV2 gibt es nicht: Die PV-Sonderregel hängt im Bestand
                // konstruktiv an der Hauptsenke (Ladeordnung 3.5). Genau das bildet die
                // Migration mit "Rang 1 erbt, alle höheren Ränge 0" nach.
                zwei.LadeprioPV = 0;
                zwei.LadegrenzeProzent = d.Ladegrenze2;
                ZeileKlemmen(zwei);
                liste.Zeilen.Add(zwei);
            }

            return liste;
        }

        /// <summary>Negativwerte einer Senkenzeile klemmen (wie <see cref="Normalisieren"/>).</summary>
        private static void ZeileKlemmen(Senkenzeile z)
        {
            if (z.IDPuffer < 0) z.IDPuffer = 0;
            if (z.Ladeprio < 0) z.Ladeprio = 0;
            if (z.LadeprioPV < 0) z.LadeprioPV = 0;
            if (z.LadegrenzeProzent < 0) z.LadegrenzeProzent = 0;
            if (string.IsNullOrEmpty(z.Bedarfsart)) z.Bedarfsart = WaermequelleClass.SENKE_BEIDES;
        }

        /// <summary>
        /// Verwendung eines Puffers; leere Angabe gilt als „Heizung" (siehe ProjektPuffer)
        /// und eine abweichende Schreibweise wird auf den Persistenzwert normalisiert
        /// (Etappe D5b, siehe <see cref="NormalisierteVerwendung"/>).
        /// </summary>
        public static string WirksameVerwendung(PufferInfo p)
        {
            if (p == null) return VERWENDUNG_HEIZUNG;
            return NormalisierteVerwendung(p.Verwendung);
        }

        /// <summary>
        /// ETAPPE D5b (Review-1-Befund K3-2): bringt einen Verwendungs-DB-Wert auf seine
        /// KANONISCHE Schreibweise.
        ///
        /// <b>Der Befund.</b> Diese Klasse vergleicht seit jeher
        /// <c>OrdinalIgnoreCase</c> (<see cref="IstKombiVerwendung"/>,
        /// <see cref="PufferPasst"/>, <c>PasstZuFilter</c>), der Rechenkern dagegen
        /// ordinal (<c>SimulationPufferspeicher.IstKombi</c>,
        /// <c>IstBrauchwasserkanal</c>). Ein Datenbankwert <c>"kombi"</c> stünde damit in
        /// BEIDEN Entladereihenfolgen — die Anzeige nimmt ihn an —, verhielte sich im
        /// Lauf aber wie ein Heizungspuffer: Der Warmwasserkanal bekäme eine Zusage, die
        /// niemand einlöst. Dasselbe gilt für <c>"brauchwasser"</c> gegenüber
        /// <c>"Brauchwasser"</c>.
        ///
        /// <b>Die Auflösung an EINER Stelle.</b> Statt jeden Vergleich im Rechenkern
        /// umzustellen (das wäre die zweite Wahrheit über dieselbe Frage) wird der Wert
        /// dort normalisiert, wo er in den Lauf eintritt: <see cref="WirksameVerwendung"/>
        /// füllt <c>SimulationPufferspeicher.Verwendung</c> in
        /// <c>SimulationControl.SpeicherRegistryAufbauen</c>. Nach dieser Zeile gibt es
        /// im Rechenkern nur noch kanonische Werte, und die ordinalen Vergleiche dort
        /// sind wieder richtig.
        ///
        /// <b>Bestandspfad unberührt.</b> Für die drei kanonischen Werte, für die leere
        /// Angabe und für JEDEN unbekannten Wert ist das Ergebnis Zeichen für Zeichen
        /// das bisherige — die Referenz-Datenbank führt ausschließlich <c>""</c>,
        /// <c>"Heizung"</c> und <c>"Brauchwasser"</c>. Nur Schreibvarianten ändern sich,
        /// und die sind heute allein über direkte Datenbankeingriffe erreichbar (alle
        /// schreibenden Wege gehen über <c>DbWerte</c>).
        ///
        /// Unbekannte Werte laufen ausdrücklich UNVERÄNDERT durch: Eine Bestandsdatenbank
        /// darf eine Verwendung führen, die diese Fassung nicht kennt, und die soll
        /// sichtbar bleiben statt still zu „Heizung" zu werden.
        /// </summary>
        public static string NormalisierteVerwendung(string verwendung)
        {
            if (string.IsNullOrEmpty(verwendung)) return VERWENDUNG_HEIZUNG;

            if (string.Equals(verwendung, VERWENDUNG_HEIZUNG, StringComparison.OrdinalIgnoreCase))
                return VERWENDUNG_HEIZUNG;
            if (string.Equals(verwendung, VERWENDUNG_BRAUCHWASSER, StringComparison.OrdinalIgnoreCase))
                return VERWENDUNG_BRAUCHWASSER;
            if (string.Equals(verwendung, VERWENDUNG_KOMBI, StringComparison.OrdinalIgnoreCase))
                return VERWENDUNG_KOMBI;

            return verwendung;
        }

        /// <summary>
        /// Anzeigetext zu einem Verwendungs-DB-Wert (Paket 9, Befund L0-2).
        ///
        /// <para>
        /// <c>Tab_Pufferspeicher.Verwendung</c> trägt die deutschen Persistenzwerte
        /// „Heizung" und „Brauchwasser". Sie standen bisher unübersetzt in Auswahllisten,
        /// Aufzählungen und Meldungstexten — auf englischer Oberfläche mischten sich damit
        /// die Sprachen. Diese Funktion ist der EINE erlaubte Übergang von der
        /// Persistenz- in die Anzeigeschicht; der Wert selbst bleibt unverändert deutsch.
        /// </para>
        ///
        /// Unbekannte Werte laufen unverändert durch — eine Bestandsdatenbank kann eine
        /// Verwendung führen, die diese Fassung nicht kennt, und die soll sichtbar bleiben.
        /// </summary>
        public static string VerwendungAnzeige(string dbWert)
        {
            if (string.Equals(dbWert, VERWENDUNG_HEIZUNG, StringComparison.OrdinalIgnoreCase))
                return MyResource.Resource.PSP_VERWENDUNG_HEIZUNG_ANZEIGE;
            if (string.Equals(dbWert, VERWENDUNG_BRAUCHWASSER, StringComparison.OrdinalIgnoreCase))
                return MyResource.Resource.PSP_VERWENDUNG_BRAUCHWASSER_ANZEIGE;
            if (IstKombiVerwendung(dbWert)) return MyResource.Resource.PSP_VERWENDUNG_KOMBI_ANZEIGE;
            return dbWert;
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
            if (d == null) { erg.Ok = false; erg.Fehler = MyResource.Resource.SIM_KEINE_SENKENDATEN; return erg; }

            Normalisieren(d);

            // 1. Hauptsenke auf Puffer -> Projekt-Puffer muss existieren, Verwendung passen
            if (IstPufferZiel(d.Ziel))
            {
                string fehler;
                if (!PufferPasst(idProjekt, d.ID_Puffer, d.Ziel, MyResource.Resource.SIM_ROLLE_HAUPTSENKE, out fehler))
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
                if (!PufferPasst(idProjekt, d.ID_Puffer2, d.Ziel2, MyResource.Resource.SIM_ROLLE_ZWEITSENKE, out fehler))
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
                erg.Fehler = string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_ZWEITSENKE_GLEICH_HAUPTSENKE),
                    ZielAnzeige(d.Ziel), PufferName(d.ID_Puffer));
                return erg;
            }

            // 4. Derselbe Puffer als Quelle UND Senke der Anlage waere ein Kurzschluss
            int idQuellPuffer = QuellPufferDerAnlage(idProjekt, idAnlage);
            if (idQuellPuffer > 0 &&
                (idQuellPuffer == d.ID_Puffer || (d.HatZweitsenke && idQuellPuffer == d.ID_Puffer2)))
            {
                erg.Ok = false;
                erg.Fehler = string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_PUFFER_QUELLE_UND_SENKE),
                    PufferName(idQuellPuffer));
                return erg;
            }

            // 5. PARALLELVERBUND: Kein Mitglied darf anderweitig eigenständiges Ziel sein
            //
            // Steht VOR der Kanalwarnung, weil es ein BLOCKER ist. Die Regel selbst und
            // ihre Begründung stehen in AnlagePufferVerbundCtrl.KonfliktPruefen; hier wird
            // aus dem ersten Befund die Meldung gebaut. Ohne Verbund ist der Aufruf ein
            // No-op (leere Mitgliederliste -> leere Befundliste).
            if (d.HatVerbund)
            {
                string verbundFehler = VerbundKonfliktMeldung(idProjekt, idAnlage, d);
                if (verbundFehler != null)
                {
                    erg.Ok = false;
                    erg.Fehler = verbundFehler;
                    return erg;
                }
            }

            // 6. Kanal ohne Bedarf -> nur Hinweis, kein Blocker
            erg.Warnung = KanalWarnung(idProjekt, d);
            return erg;
        }

        /// <summary>
        /// Baut aus dem ERSTEN Konfliktbefund des Parallelverbunds die Anwendermeldung;
        /// <c>null</c>, wenn die Zuordnung in Ordnung ist.
        ///
        /// <b>Nur der erste Befund.</b> Dieselbe Bauart wie die Punkte 1 bis 4 darüber:
        /// Der Dialog nennt einen Grund, der Anwender räumt ihn weg, der nächste Versuch
        /// zeigt den nächsten. Eine Sammelmeldung über fünf Speicher wäre in einer
        /// MessageBox nicht lesbar.
        ///
        /// <b>Die Textbausteine kommen aus dem Ressourcenkatalog</b>, die IDs und
        /// Grundcodes aus dem Controller — der Grundcode ist ein STEUERWERT und wird nie
        /// angezeigt (Drei-Schichten-Regel).
        /// </summary>
        private static string VerbundKonfliktMeldung(int idProjekt, int idAnlage, SenkeDaten d)
        {
            string verwendungLeit = VerwendungZuZiel(d.Ziel);

            List<AnlagePufferVerbundCtrl.Konfliktbefund> befunde =
                AnlagePufferVerbundCtrl.KonfliktPruefen(idProjekt, idAnlage, d.ID_Puffer,
                                                        d.HatZweitsenke ? d.ID_Puffer2 : 0,
                                                        d.VerbundMitglieder, verwendungLeit);
            if (befunde.Count == 0) return null;

            AnlagePufferVerbundCtrl.Konfliktbefund b = befunde[0];
            string name = PufferName(b.ID_Puffer);
            string kopf;

            switch (b.Grund)
            {
                case AnlagePufferVerbundCtrl.GRUND_HAUPTSENKE:
                case AnlagePufferVerbundCtrl.GRUND_ZWEITSENKE:
                    kopf = string.Format(MyResource.Resource.SIM_VERBUND_KONFLIKT_SENKE,
                                         name, AnlagenName(b.ID_AndereAnlage));
                    break;

                case AnlagePufferVerbundCtrl.GRUND_ANDERER_VERBUND:
                    kopf = string.Format(MyResource.Resource.SIM_VERBUND_KONFLIKT_FREMDVERBUND,
                                         name, PufferName(b.ID_FremderLeit));
                    break;

                case AnlagePufferVerbundCtrl.GRUND_LEIT_IST_MITGLIED:
                    kopf = string.Format(MyResource.Resource.SIM_VERBUND_KONFLIKT_LEIT_IST_MITGLIED,
                                         name, PufferName(b.ID_FremderLeit));
                    break;

                case AnlagePufferVerbundCtrl.GRUND_QUELLE:
                    kopf = string.Format(MyResource.Resource.SIM_VERBUND_KONFLIKT_QUELLE, name);
                    break;

                default:
                    kopf = string.Format(MyResource.Resource.SIM_VERBUND_KONFLIKT_PASST_NICHT,
                                         name, VerwendungAnzeige(verwendungLeit));
                    break;
            }

            return kopf.Replace("\n", Environment.NewLine) + Environment.NewLine +
                   Environment.NewLine +
                   MyResource.Resource.SIM_VERBUND_KONFLIKT_ERKLAERUNG.Replace("\n", Environment.NewLine);
        }

        /// <summary>Bezeichner einer Anlage für Meldungen; leer, wenn es sie nicht (mehr) gibt.</summary>
        private static string AnlagenName(int idAnlage)
        {
            if (idAnlage <= 0) return "";

            return StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage)));
        }

        // --- Verbundkennzahlen für Dialog und Anzeigen ---------------------------------

        /// <summary>
        /// Nutzbare Gesamtkapazität eines Parallelverbunds [kWh]: <c>Q_max</c> des
        /// Leitspeichers plus <c>Q_max</c> jedes Mitglieds.
        ///
        /// <b>Q_max wird summiert, NICHT das Volumen.</b> Jeder Behälter bringt sein
        /// EIGENES Temperaturpaar mit; zwei mal 1000 l bei 60/40 und 50/40 ergeben nicht
        /// dieselbe Kapazität wie 2000 l bei einer der beiden Spreizungen. Die Summe der
        /// Einzelkapazitäten ist die physikalisch richtige Größe und dieselbe Zahl, mit
        /// der die Engine rechnet (<c>SimulationControl.VerbundAufaddieren</c>).
        ///
        /// Ein Mitglied ohne gepflegtes Temperaturpaar trägt hier 0 bei
        /// (<see cref="PufferInfo.Q_max"/> liefert dann 0) — die Engine setzt an dieser
        /// Stelle ihren ΔT-Rückfall an und meldet ihn. Die Dialoganzeige darf keine
        /// Ersatzannahme erfinden, die im Lauf anders ausfällt.
        /// </summary>
        public static double VerbundKapazitaet(int idLeit, IList<int> mitglieder)
        {
            double summe = 0;

            PufferInfo leit = PufferLesen(idLeit);
            if (leit != null) summe += leit.Q_max;

            if (mitglieder != null)
                foreach (int id in mitglieder)
                {
                    PufferInfo p = PufferLesen(id);
                    if (p != null) summe += p.Q_max;
                }

            return summe;
        }

        /// <summary>Existiert der Puffer im Projekt und passt seine Verwendung zum Ziel?</summary>
        private static bool PufferPasst(int idProjekt, int idPuffer, string ziel,
                                        string rolle, out string fehler)
        {
            fehler = null;
            string verlangt = VerwendungZuZiel(ziel);

            if (idPuffer <= 0)
            {
                fehler = string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_KEIN_PUFFER_GEWAEHLT),
                    rolle, ZielAnzeige(ziel), VerwendungAnzeige(verlangt));
                return false;
            }

            PufferInfo p = PufferLesen(idPuffer);
            if (p == null || p.ID_Projekt != idProjekt)
            {
                fehler = string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_PUFFER_FREMDES_PROJEKT),
                    rolle, VerwendungAnzeige(verlangt));
                return false;
            }

            if (!string.Equals(WirksameVerwendung(p), verlangt, StringComparison.OrdinalIgnoreCase))
            {
                fehler = string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_PUFFER_VERWENDUNG_PASST_NICHT),
                    p.Bezeichner, VerwendungAnzeige(WirksameVerwendung(p)),
                    rolle, VerwendungAnzeige(verlangt));
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

        // --- Validierung der QUELLENSEITE (Etappe D5b, Konzept Abschnitt 7) -----------
        //
        // Bis D5a konnte der Senkendialog den Kurzschluss „Quelle = eigene Senke" nur von
        // der SENKENseite aus verhindern (Pruefen, Punkt 4); von der QUELLENseite gab es
        // gar keinen Dialog, der ihn hätte prüfen können - für den Heizkessel war die
        // Quellenwahl nicht freigeschaltet, für die Wärmepumpe schrieb der Quellendialog
        // ungeprüft. Die Engine fängt beides ab (Kurzschluss-Guard E-K2-1, Zyklus-Guard
        // der Rechenebenen), aber erst im Lauf: beim Kurzschluss mit einer Warnung und
        // wirkungslosem Quellbezug, beim Ring mit einem ABBRUCH des ganzen Laufs.
        //
        // Die beiden folgenden Prüfungen sind das Dialog-Gegenstück dazu. Sie sind
        // dialogfrei und ohne Oberfläche aufrufbar (dieselbe Bauart wie Pruefen), damit
        // ein Prüfprogramm sie ohne Fenster fahren kann; die Engine-Guards bleiben als
        // ZWEITE Verteidigungslinie unangetastet - sie decken Altdaten und jeden Weg ab,
        // der nicht über diesen Dialog läuft.

        /// <summary>Ergebnis der Quellenprüfung; <see cref="Fehler"/> ist der Blockertext.</summary>
        public sealed class QuellPruefErgebnis
        {
            /// <summary>true = die Quelle darf gespeichert werden.</summary>
            public bool Ok = true;

            /// <summary>Meldungstext des Blockers; null, wenn <see cref="Ok"/>.</summary>
            public string Fehler;
        }

        /// <summary>
        /// Prüft einen beabsichtigten Quellpuffer-Bezug, BEVOR er geschrieben wird:
        /// erst der Kurzschluss (Quelle = eigene Senke, Konzept 4.6), dann der Ring über
        /// die Kaskadenkette (Konzept Abschnitt 7).
        ///
        /// <paramref name="idQuellPuffer"/> ist der GEWÜNSCHTE Bezug, nicht der
        /// gespeicherte — die Prüfung rechnet ihn in den vorhandenen Bestand hinein.
        /// 0 (Quelle entfernen) ist immer zulässig.
        /// </summary>
        public static QuellPruefErgebnis QuellePruefen(int idProjekt, int idAnlage, int idQuellPuffer)
        {
            QuellPruefErgebnis erg = new QuellPruefErgebnis();
            if (idProjekt <= 0 || idAnlage <= 0 || idQuellPuffer <= 0) return erg;

            erg.Fehler = KurzschlussMeldung(idAnlage, idQuellPuffer);
            if (erg.Fehler == null) erg.Fehler = RingMeldung(idProjekt, idAnlage, idQuellPuffer);

            erg.Ok = erg.Fehler == null;
            return erg;
        }

        /// <summary>
        /// KURZSCHLUSS (Konzept 4.6, Engine-Guard E-K2-1): Der Quellpuffer ist zugleich
        /// Haupt- oder Zweitsenke DERSELBEN Anlage — sie pumpte Wärme im Kreis.
        /// null = kein Kurzschluss.
        ///
        /// Gegenstück zu Punkt 4 in <see cref="Pruefen"/>, nur von der anderen Seite
        /// gefragt: Dort ist die Senke neu und die Quelle steht, hier steht die Senke und
        /// die Quelle ist neu. Gilt für Wärmepumpe UND Heizkessel — die Engine weist seit
        /// der D5a-Nacharbeit beide ab.
        /// </summary>
        public static string KurzschlussMeldung(int idAnlage, int idQuellPuffer)
        {
            if (idAnlage <= 0 || idQuellPuffer <= 0) return null;

            SenkeDaten d = Lesen(idAnlage);          // enthält Normalisieren
            string rolle = null;

            if (IstPufferZiel(d.Ziel) && d.ID_Puffer == idQuellPuffer)
                rolle = MyResource.Resource.SIM_ROLLE_HAUPTSENKE;
            else if (d.HatZweitsenke && d.ID_Puffer2 == idQuellPuffer)
                rolle = MyResource.Resource.SIM_ROLLE_ZWEITSENKE;

            if (rolle == null) return null;

            return string.Format(
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_QUELLE_GLEICH_EIGENE_SENKE),
                PufferName(idQuellPuffer), rolle);
        }

        /// <summary>
        /// RING in der Kaskadenkette (Konzept Abschnitt 7): Eine Anlage lädt einen
        /// Speicher, aus dem sie über weitere Erzeuger wieder ihre eigene Quellwärme
        /// bezieht — auch indirekt über A→B→C→A. null = zyklenfrei.
        ///
        /// <b>Dieselbe Auflösung wie die Engine.</b> Gerechnet wird die
        /// Ebenen-Relaxation aus <c>Kaskadenschleife.EbenenRelaxieren</c>:
        /// <c>Ebene(A) = 1 + max{ Ebene(L) : L lädt den Quellpuffer von A }</c>, iterativ,
        /// und was nach so vielen Runden wächst, wie es Anlagen gibt, kann nur ein Ring
        /// sein. Eine eigene Ringsuche daneben wäre eine zweite Auslegung derselben
        /// Frage — dann könnte der Dialog eine Konfiguration durchlassen, an der die
        /// Engine hinterher abbricht (oder umgekehrt).
        ///
        /// Übernommen sind auch die beiden Einschränkungen der Engine:
        /// <list type="bullet">
        ///   <item><description>Quellbezüge zählen nur bei Wärmepumpe und Heizkessel
        ///     (Befund E-K2-2, <see cref="WaermequelleClass.QuellenwahlMoeglich"/>) — jede
        ///     andere Art bleibt auf Ebene 0 und kann keinen Ring schließen.</description></item>
        ///   <item><description>Der Selbstbezug (die Anlage lädt ihren eigenen
        ///     Quellpuffer) ist übersprungen: Das ist der Kurzschluss aus 4.6, den
        ///     <see cref="KurzschlussMeldung"/> mit eigenem Text abfängt.</description></item>
        /// </list>
        ///
        /// Anders als die Engine arbeitet die Prüfung auf der DATENBANKSICHT statt auf den
        /// Ladeaufträgen: Ein Ladeauftrag entsteht erst im Lauf. Die Bedingung „lädt" ist
        /// deshalb dieselbe wie in <c>Ladeordnung.Ladereihenfolge</c> — Puffer-ID auf
        /// einem der beiden Senkenfelder UND ein Puffer-Ziel dazu.
        ///
        /// <b>ETAPPE D4 — die Ableitung steht jetzt in <see cref="Hydraulikbild"/>.</b>
        /// Sie war bis D5b eine lokale Rechnung IN dieser Methode; die Schema-Ansicht
        /// braucht dieselbe Abbildung (Lader je Puffer, Quelle je Anlage) und würde sie
        /// sonst ein drittes Mal schreiben (D5b-Restpunkt 2). Verschoben, nicht geändert:
        /// Abfrage, Zeilenordnung, Bedingungen und Relaxation sind unverändert; die
        /// Abfrage holt nur zusätzliche Spalten für die Zeichnung.
        /// </summary>
        public static string RingMeldung(int idProjekt, int idAnlage, int idQuellPuffer)
        {
            if (idProjekt <= 0 || idAnlage <= 0 || idQuellPuffer <= 0) return null;

            Hydraulikbild bild = Hydraulikbild.Lesen(idProjekt);
            if (bild == null) return null;
            if (!bild.KenntAnlage(idAnlage)) return null;          // nicht dieses Projekt

            // Der GEWÜNSCHTE Bezug ersetzt den gespeicherten - geprüft wird der Zustand
            // nach dem Speichern, nicht der davor.
            bool ring;
            Dictionary<int, int> ebene = bild.Ebenen(idAnlage, idQuellPuffer, out ring);
            if (!ring) return null;                                // zyklenfrei

            return string.Format(
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_QUELLE_KASKADE_RING),
                bild.RingBeteiligte(ebene, idAnlage, idQuellPuffer));
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

            // D5a: Das Kombi-Ziel bedient den Warmwasserkanal mit — ohne
            // Brauchwasseranteil im Projekt gilt derselbe Hinweis wie beim reinen
            // Brauchwasserpuffer (er ist ein Hinweis, kein Blocker: die Heizungshälfte
            // des Kombispeichers arbeitet weiter).
            bool brauchwasser =
                IstBrauchwasserseitig(d.Ziel) ||
                (d.HatZweitsenke && IstBrauchwasserseitig(d.Ziel2));

            if (!brauchwasser) return null;
            if (ProjektHatBrauchwasser(idProjekt)) return null;

            return Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_KEIN_BRAUCHWASSERBEDARF);
        }

        /// <summary>
        /// true, wenn das Ziel den WARMWASSERKANAL bedient — der reine Brauchwasserpuffer
        /// und der Kombispeicher (D5a).
        /// </summary>
        public static bool IstBrauchwasserseitig(string ziel)
        {
            return string.Equals(ziel, ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal) ||
                   string.Equals(ziel, ZIEL_PUFFER_KOMBI, StringComparison.Ordinal);
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
            if (d == null) return MyResource.Resource.SIM_HEIZKREIS;

            if (IstPufferZiel(d.Ziel))
            {
                string name = PufferName(d.ID_Puffer);
                string kurz = KurzformZuZiel(d.Ziel);
                return name.Length > 0 ? kurz + ": " + name : kurz;
            }

            // Heizkreis: die Bedarfsart ist hier die Feinsteuerung (Konzept 3.1)
            switch (d.Bedarfsart)
            {
                case WaermequelleClass.SENKE_WARMWASSER: return MyResource.Resource.SIM_HEIZKREIS_NUR_WARMWASSER;
                case WaermequelleClass.SENKE_HEIZUNG: return MyResource.Resource.SIM_HEIZKREIS_NUR_HEIZWAERME;
                default: return MyResource.Resource.SIM_HEIZKREIS_BEIDES;
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
            string kurz = KurzformZuZiel(d.Ziel2);
            return name.Length > 0 ? kurz + ": " + name : kurz;
        }

        /// <summary>
        /// Kurzform eines Puffer-Ziels für Übersichten („Puffer Heizung", „Puffer
        /// Brauchwasser", „Puffer Kombi"). Vorher stand die Zuordnung zweimal im Code —
        /// mit dem dritten Ziel aus D5a wäre daraus die dritte Fehlerquelle geworden.
        /// </summary>
        private static string KurzformZuZiel(string ziel)
        {
            if (string.Equals(ziel, ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return MyResource.Resource.SIM_PUFFER_HEIZUNG_KURZ;
            if (string.Equals(ziel, ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return MyResource.Resource.SIM_PUFFER_KOMBI_KURZ;
            return MyResource.Resource.SIM_PUFFER_BRAUCHWASSER_KURZ;
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
