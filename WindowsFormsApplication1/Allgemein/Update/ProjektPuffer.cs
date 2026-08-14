using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Gemeinsame Bausteine für Projekt-Pufferspeicher und ihre Anlagenzeile.
    ///
    /// Hier steht genau EINMAL, wie ein Projekt-Puffer (<c>Tab_Pufferspeicher</c>) und die
    /// zugehörige Zeile in <c>Tab_Energieanlagen</c> (<c>ID_Type = 12</c>) aussehen.
    /// Zwei Stellen brauchen das:
    ///
    ///   - <c>SchemaMigration</c>, Regeln R4 und R6 der einmaligen Datenmigration
    ///     (Konzept 5.5) — sie legt den BHKW-Pendelspeicher aus dem Alt-Parameter an.
    ///   - <c>PufferSpCtrl.SetPendelspeicherVolumenLiter</c> (Etappe 3) — die Oberfläche
    ///     legt denselben Puffer an, wenn der Anwender erstmals ein Volumen einträgt.
    ///
    /// Bewusst nur SQL-Text und Parameterlisten, keine Ausführung: die Migration arbeitet
    /// auf ihrer eigenen, stillen Verbindung (sie darf keine Dialoge zeigen und muss
    /// Fehlertexte auswerten), der Controller auf seiner. Gemeinsam ist die Struktur,
    /// nicht der Weg zur Datenbank.
    /// </summary>
    internal static class ProjektPuffer
    {
        /// <summary>Bezeichner des BHKW-Pendelspeichers (Konzept 5.5, Regel R6).</summary>
        public const string BEZ_PENDELSPEICHER = "BHKW-Pendelspeicher";

        /// <summary>Verwendung eines Heizungspuffers (Konzept 5.1).</summary>
        public const string VERWENDUNG_HEIZUNG = "Heizung";

        /// <summary>
        /// Literal des Erzeugers in <c>Z_ProjektPufferSp.Erzeuger</c>, auf das die Engine
        /// vergleicht (<c>SimulationControl.Do_Simulation</c>: alles andere wird mit
        /// <c>continue</c> übersprungen). Steht hier, damit Engine, Migration (R1) und
        /// Konfigurationsdialog dieselbe Zeichenkette meinen - eine Abweichung würde
        /// still die falsche Zuordnung auswählen.
        /// </summary>
        public const string ERZEUGER_WAERMEPUMPE = "Wärmepumpe";

        /// <summary>Senke "Pufferspeicher Heizung" an der Erzeugeranlage (Konzept 3.2).</summary>
        public const string WS_ZIEL_PUFFER_HEIZUNG = "PufferHeizung";

        /// <summary>Speichertyp, den der erzeugte Pendelspeicher bekommt.</summary>
        public const string SPEICHERTYP_PUFFER = "Pufferspeicher";

        // ID_Type aus WizardItemClass - hier bewusst als lokale Konstanten, damit weder
        // Migration noch Controller von der UI-Schicht abhängen.
        public const int TYP_WP = 1;
        public const int TYP_SOLARTHERMIE = 2;
        public const int TYP_KESSEL = 10;
        public const int TYP_BHKW = 11;
        public const int TYP_PUFFER = 12;

        /// <summary>
        /// Die vier Wärmeerzeuger-Typen als Kommaliste für eine IN-Klausel. Bewusst
        /// zusammengesetzt statt hingeschrieben: die Typnummern stehen dann genau
        /// einmal da. Reine Konstanten - kein Einfallstor für Fremdtext im SQL.
        /// </summary>
        public static readonly string WAERMEERZEUGER_TYPEN =
            TYP_WP.ToString(CultureInfo.InvariantCulture) + "," +
            TYP_SOLARTHERMIE.ToString(CultureInfo.InvariantCulture) + "," +
            TYP_KESSEL.ToString(CultureInfo.InvariantCulture) + "," +
            TYP_BHKW.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Umrechnung des Alt-Parameters <c>Tab_Einstellungen.Pendelspeicher</c> in das
        /// Gesamtvolumen eines Puffers.
        ///
        /// Der Alt-Parameter war in **m³** angegeben — belegt am früheren Anzeigetext
        /// "Volumen Pendelspeicher [m³]" (Form_Simulation_Detail, label56) und an der
        /// alten Kapazitätsformel <c>SimulationControl</c>:
        /// <c>kapazitaetPendelspeicher = Volumen * 20000 / 860</c>, also 23,26 kWh je
        /// Parametereinheit = 1000 l · 1,163 Wh/(l·K) · 20 K.
        ///
        /// <c>Tab_Pufferspeicher.Gesamtvolumen</c> führt dagegen **Liter**
        /// (Katalogbeleg: "Vitocell 140-E 600 Liter" -&gt; Gesamtvolumen 600).
        /// Seit Etappe 3 arbeiten Engine und Oberfläche durchgehend in Litern; der
        /// Faktor wird nur noch von der Migration gebraucht.
        /// </summary>
        public const double M3_IN_LITER = 1000.0;

        // --- Systemvorgaben des Projekts (Etappe 4) -----------------------------------

        /// <summary>
        /// Kleinster Vorlauf über die Wärmeerzeuger-Anlagen eines Projekts.
        ///
        /// Das ist die konservative Auslegung für einen GEMEINSAMEN Speicher: Er muss
        /// mit dem Erzeuger auskommen, der am wenigsten Vorlauf liefert. Genau diese
        /// Regel steht hinter den gespeicherten Access-Abfragen
        /// <c>Abfrage_Erzeuger_Vorlauftemperaturen</c> / <c>…Ruecklauftemperaturen</c>
        /// (Konzept 13.7) - deren Definitionen enden allerdings auf ein hartkodiertes
        /// <c>HAVING ID_Projekt = 8</c> und liefern für jedes Projekt 0 Zeilen. Sie sind
        /// damit tot; die Abfrage wird deshalb wie seit B0-9 direkt und parametrisiert
        /// formuliert.
        ///
        /// <c>Vorlauf &gt; 0</c> ist KEINE Temperatur-Untergrenze, sondern der Test auf
        /// "gepflegt": <c>Tab_Energieanlagen.Vorlauf</c> ist in Access mit dem
        /// Spalten-Default 0 belegt und nie NULL. Ohne den Filter wäre die Systemvorgabe
        /// jedes Projekts mit auch nur einer unvollständig erfassten Anlage 0.
        /// </summary>
        public static readonly string SQL_SYSTEM_VORLAUF =
            "SELECT MIN(Vorlauf) FROM Tab_Energieanlagen " +
            "WHERE ID_Projekt = ? AND ID_Type IN (" + WAERMEERZEUGER_TYPEN + ") AND Vorlauf > 0";

        /// <summary>
        /// Größter Rücklauf über die Wärmeerzeuger-Anlagen eines Projekts - das
        /// Gegenstück zu <see cref="SQL_SYSTEM_VORLAUF"/>.
        ///
        /// ACHTUNG: Die Spalte heißt <c>Rücklauf</c> MIT Umlaut (an der Datenbank
        /// verifiziert, Konzept 13.7 / Befund B0-4) - anders als
        /// <c>Z_ProjektPufferSp.Ruecklauf</c> und <c>Tab_Pufferspeicher.Ruecklauf</c>.
        /// </summary>
        public static readonly string SQL_SYSTEM_RUECKLAUF =
            "SELECT MAX([Rücklauf]) FROM Tab_Energieanlagen " +
            "WHERE ID_Projekt = ? AND ID_Type IN (" + WAERMEERZEUGER_TYPEN + ") AND [Rücklauf] > 0";

        /// <summary>Parameter zu <see cref="SQL_SYSTEM_VORLAUF"/> / <see cref="SQL_SYSTEM_RUECKLAUF"/>.</summary>
        public static OleDbParameter[] SystemTemperaturParameter(int idProjekt)
        {
            return new[] { Par("@proj", OleDbType.Integer, idProjekt) };
        }

        /// <summary>
        /// Taugt das Paar als Betriebsvorgabe eines Speichers? Verlangt wird genau das,
        /// was die Kapazitätsformel braucht: beide Werte vorhanden, Rücklauf über 0 und
        /// eine positive Spreizung.
        ///
        /// Der Test auf <c>vorlauf &gt; ruecklauf</c> ist nötig, weil im Bestand
        /// vertauschte Paare vorkommen (nachgewiesen: Projekte 1023/1024 der
        /// Arbeitskopie liefern als Systemvorgabe 45/60 °C). Ein solches Paar an den
        /// Speicher zu schreiben wäre schlechter als gar nichts - es sähe gepflegt aus
        /// und ergäbe doch nur den stillen Rückfall auf die Engine-Vorgabe.
        ///
        /// KEINE Untergrenze: 35/28 und tiefer sind gültige Paare.
        /// </summary>
        public static bool IstTemperaturpaar(int? vorlauf, int? ruecklauf)
        {
            return vorlauf.HasValue && ruecklauf.HasValue &&
                   ruecklauf.Value > 0 && vorlauf.Value > ruecklauf.Value;
        }

        // --- Betriebstemperaturen am Puffer (Etappe 4) --------------------------------

        /// <summary>
        /// Betriebstemperaturen der Puffer-Zeile - seit Etappe 4 die FÜHRENDE Ablage
        /// (Konzept 5.1: "Die Betriebsparameter wandern von der Zuordnung an den
        /// Speicher selbst").
        /// </summary>
        public const string SQL_PUFFER_TEMPERATUREN =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Pufferspeicher WHERE ID = ?";

        /// <summary>Schreibt die Betriebstemperaturen an die Puffer-Zeile.</summary>
        public const string SQL_PUFFER_TEMPERATUREN_UPDATE =
            "UPDATE Tab_Pufferspeicher SET Vorlauf = ?, Ruecklauf = ? WHERE ID = ?";

        // --- Projekt-Puffer -----------------------------------------------------------

        /// <summary>
        /// <c>Tab_Pufferspeicher.ID</c> ist KEIN AutoWert - die ID wird nach dem
        /// <c>GetMaxID + 1</c>-Muster aus <c>PufferSpCtrl.CopyFromStamm</c> vergeben und
        /// deshalb mitgeschrieben.
        /// </summary>
        public const string SQL_PUFFER_INSERT =
            "INSERT INTO Tab_Pufferspeicher " +
            "(ID, ID_Projekt, Bezeichner, Speichertyp, Gesamtvolumen, " +
            " Bereitschaftsverluste, Investitionskosten, Verwendung, Vorlauf, Ruecklauf) " +
            "VALUES (?,?,?,?,?,?,?,?,?,?)";

        /// <summary>
        /// Parameter zu <see cref="SQL_PUFFER_INSERT"/>. Bereitschaftsverluste und
        /// Investitionskosten sind 0: der Alt-Pendelspeicher kennt weder das eine noch
        /// das andere, und 0 hält das Simulationsergebnis unverändert.
        ///
        /// <paramref name="vorlauf"/>/<paramref name="ruecklauf"/> sind seit Etappe 4 die
        /// Vorbelegung aus den SYSTEMVORGABEN des Projekts
        /// (<see cref="SQL_SYSTEM_VORLAUF"/>). Gibt es dort keine Werte, bleiben beide
        /// Spalten NULL - dann greift weiter der Engine-Rückfall. Bewusst KEINE
        /// eingebaute Vorbelegung "55/35" o. ä.: Niedertemperatursysteme (z. B. 35/28)
        /// sollen sich aus den Erzeugern selbst ergeben.
        /// </summary>
        public static OleDbParameter[] PufferParameter(int idPuffer, int idProjekt,
                                                       string bezeichner, int volumenLiter,
                                                       int? vorlauf = null, int? ruecklauf = null)
        {
            // Nur ein BRAUCHBARES Paar wird geschrieben - eine halbe oder vertauschte
            // Angabe ergäbe am Speicher keine auswertbare Spreizung und würde den
            // Rückfall nur verdecken (siehe IstTemperaturpaar).
            bool paar = IstTemperaturpaar(vorlauf, ruecklauf);

            return new[]
            {
                Par("@id",    OleDbType.Integer,  idPuffer),
                Par("@proj",  OleDbType.Integer,  idProjekt),
                Par("@bez",   OleDbType.VarWChar, bezeichner),
                Par("@typ",   OleDbType.VarWChar, SPEICHERTYP_PUFFER),
                Par("@vol",   OleDbType.Integer,  volumenLiter),
                Par("@verl",  OleDbType.Double,   0.0),
                Par("@inv",   OleDbType.Double,   0.0),
                Par("@verw",  OleDbType.VarWChar, VERWENDUNG_HEIZUNG),
                Par("@vor",   OleDbType.Integer,  paar ? (object)vorlauf.Value   : System.DBNull.Value),
                Par("@rueck", OleDbType.Integer,  paar ? (object)ruecklauf.Value : System.DBNull.Value)
            };
        }

        // --- Anlagenzeile des Puffers (ID_Type = 12) ----------------------------------

        /// <summary>
        /// Puffer-Anlagenzeile exakt nach dem Muster von
        /// <c>WizardCtrl.Add_WP_Waermeerzeuger</c>. Zwei Punkte sind dabei zwingend:
        ///
        ///   - <c>Tab_Energieanlagen.ID</c> ist ein AutoWert und wird NICHT gesetzt
        ///     (der bestehende Erzeugungspfad tut das ebenfalls nicht).
        ///   - Sämtliche Komponenten-Fremdschlüssel müssen ausdrücklich auf NULL gesetzt
        ///     werden. Sie tragen in Access den Spalten-Default 0, und 0 verletzt die
        ///     erzwungenen Beziehungen (Tab_WP, Tab_BHKW, …) - ein INSERT ohne diese
        ///     Spalten scheitert mit "ein Datensatz in der Tabelle 'Tab_BHKW' muss in
        ///     Beziehung stehen". Genau deshalb schreibt auch der Wizard überall DBNull.
        ///     Einzige Ausnahme ist ID_PUFFER, das auf die Projektkopie zeigt.
        /// </summary>
        public const string SQL_ANLAGENZEILE_INSERT =
            "INSERT INTO Tab_Energieanlagen " +
            "(ID_Projekt, Bezeichner, ID_Type, Betriebsart, Sperrung, Sperrzeit_von, Sperrzeit_bis, " +
            " Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, " +
            " Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, " +
            " ID_WP, ID_Solar, ID_PV, ID_SP, ID_Kessel, ID_BHKW, ID_PUFFER, ID_Carrier, " +
            " Heizstab, Volumen, rendeMix, Solaranteil) " +
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

        /// <summary>Parameter zu <see cref="SQL_ANLAGENZEILE_INSERT"/>.</summary>
        public static OleDbParameter[] AnlagenzeileParameter(int idProjekt, string bezeichner, int idPuffer)
        {
            // ACHTUNG: die Parameter durchgehend über Par() mit ausdrücklichem Typ. Ein
            // literales 0 als Wert würde sonst an die Überladung
            // OleDbParameter(string, OleDbType) binden - OleDbType.Empty hat den Wert 0 -
            // und das INSERT scheitert mit "the OleDbType property is uninitialized".
            return new[]
            {
                Par("@proj",    OleDbType.Integer,  idProjekt),
                Par("@bez",     OleDbType.VarWChar, bezeichner),
                Par("@typ",     OleDbType.Integer,  TYP_PUFFER),
                Par("@art",     OleDbType.VarWChar, ""),
                Par("@sperr",   OleDbType.Boolean,  false),
                Par("@svon",    OleDbType.Integer,  0),
                Par("@sbis",    OleDbType.Integer,  0),
                Par("@vor",     OleDbType.Integer,  0),
                Par("@rueck",   OleDbType.Integer,  0),
                Par("@biv",     OleDbType.Boolean,  false),
                Par("@ab",      OleDbType.Double,   0.0),
                Par("@nutz",    OleDbType.Integer,  0),
                Par("@grenz",   OleDbType.Double,   0.0),
                Par("@koll",    OleDbType.Integer,  0),
                Par("@pvleist", OleDbType.Double,   0.0),
                Par("@neig",    OleDbType.Integer,  0),
                Par("@azim",    OleDbType.Integer,  0),
                Par("@wp",      OleDbType.Integer,  System.DBNull.Value),
                Par("@sol",     OleDbType.Integer,  System.DBNull.Value),
                Par("@pv",      OleDbType.Integer,  System.DBNull.Value),
                Par("@sp",      OleDbType.Integer,  System.DBNull.Value),
                Par("@kes",     OleDbType.Integer,  System.DBNull.Value),
                Par("@bhkw",    OleDbType.Integer,  System.DBNull.Value),
                Par("@puf",     OleDbType.Integer,  idPuffer > 0 ? (object)idPuffer : System.DBNull.Value),
                Par("@carrier", OleDbType.Integer,  System.DBNull.Value),
                Par("@stab",    OleDbType.Boolean,  false),
                Par("@vol",     OleDbType.Double,   0.0),
                Par("@mix",     OleDbType.Boolean,  false),
                Par("@solan",   OleDbType.Integer,  0)
            };
        }

        // --- Senke der BHKW-Anlagen ---------------------------------------------------

        /// <summary>
        /// Setzt alle BHKW-Anlagen eines Projekts auf den Pendelspeicher als Senke
        /// (Konzept 5.5, Regel R6). Heute liest die Engine <c>WS_Ziel</c> noch nicht -
        /// die Spalten werden in Paket 2 wirksam.
        /// </summary>
        public const string SQL_BHKW_AUF_PUFFER =
            "UPDATE Tab_Energieanlagen SET WS_Ziel = ?, WS_ID_Puffer = ? " +
            "WHERE ID_Projekt = ? AND ID_Type = ?";

        /// <summary>Parameter zu <see cref="SQL_BHKW_AUF_PUFFER"/>.</summary>
        public static OleDbParameter[] BhkwAufPufferParameter(int idProjekt, int idPuffer)
        {
            return new[]
            {
                Par("@ziel", OleDbType.VarWChar, WS_ZIEL_PUFFER_HEIZUNG),
                Par("@puf",  OleDbType.Integer,  idPuffer),
                Par("@proj", OleDbType.Integer,  idProjekt),
                Par("@typ",  OleDbType.Integer,  TYP_BHKW)
            };
        }

        // --- Validierung der Betriebstemperaturen (Etappe 4) --------------------------

        /// <summary>
        /// Höchster zulässiger Vorlauf [°C]. Oberhalb davon ist kein Warmwasser-Speicher
        /// mehr im Spiel (Siedepunkt bei Umgebungsdruck), der Wert ist also ein
        /// Tippfehlerschutz - keine fachliche Auslegungsgrenze.
        /// </summary>
        public const int MAX_VORLAUF_C = 110;

        /// <summary>
        /// Einheitliche Prüfung eines Temperaturpaars, überall dort zu verwenden, wo
        /// Vorlauf und Rücklauf eingegeben werden.
        ///
        /// Es gibt bewusst **keine Untergrenze** über <c>Rücklauf &gt; 0</c> hinaus:
        /// Niedertemperatursysteme (Flächenheizung, 35/28 und tiefer) müssen
        /// durchgehen. Geprüft wird nur, was physikalisch bzw. rechnerisch nötig ist:
        ///
        ///   - beide Felder sind ganze Zahlen (TryParse statt Int32.Parse - eine leere
        ///     oder kaputte Eingabe war bisher eine unbehandelte FormatException,
        ///     Konzept 4.6),
        ///   - Rücklauf &gt; 0 °C,
        ///   - Vorlauf &gt; Rücklauf (sonst ist die Spreizung 0 oder negativ und
        ///     <c>Q_max</c> nicht berechenbar),
        ///   - Vorlauf &lt;= <see cref="MAX_VORLAUF_C"/> °C.
        ///
        /// Der Fehlertext ist deutsch und benennt den konkreten Verstoß; der Aufrufer
        /// zeigt ihn und lässt den Dialog offen (DialogResult.None).
        /// </summary>
        /// <returns>true, wenn beide Werte gültig sind.</returns>
        public static bool TemperaturenPruefen(string vorlaufText, string ruecklaufText,
                                               out int vorlauf, out int ruecklauf,
                                               out string fehler)
        {
            vorlauf = 0;
            ruecklauf = 0;
            fehler = null;

            // Invariant geparst: es geht um ganze Grad, ein Dezimal- oder
            // Tausendertrennzeichen hat hier nichts zu suchen.
            if (!int.TryParse((vorlaufText ?? "").Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out vorlauf))
            {
                fehler = "Bitte eine Vorlauftemperatur als ganze Zahl eingeben (°C).";
                return false;
            }

            if (!int.TryParse((ruecklaufText ?? "").Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out ruecklauf))
            {
                fehler = "Bitte eine Rücklauftemperatur als ganze Zahl eingeben (°C).";
                return false;
            }

            if (ruecklauf <= 0)
            {
                fehler = "Die Rücklauftemperatur muss größer als 0 °C sein.";
                return false;
            }

            if (vorlauf <= ruecklauf)
            {
                fehler = "Die Vorlauftemperatur muss über der Rücklauftemperatur liegen." +
                         System.Environment.NewLine +
                         "Eingegeben: Vorlauf " + vorlauf + " °C, Rücklauf " + ruecklauf + " °C.";
                return false;
            }

            if (vorlauf > MAX_VORLAUF_C)
            {
                fehler = "Die Vorlauftemperatur darf höchstens " + MAX_VORLAUF_C + " °C betragen.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parameter mit ausdrücklichem Typ. Nötig überall dort, wo der Wert
        /// <see cref="System.DBNull"/> sein kann: aus DBNull allein kann der
        /// OLE-DB-Provider den Spaltentyp nicht ableiten.
        /// </summary>
        public static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            return new OleDbParameter(name, typ) { Value = wert ?? System.DBNull.Value };
        }
    }
}
