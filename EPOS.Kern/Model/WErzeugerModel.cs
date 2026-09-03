namespace WindowsFormsApplication1
{
    
    public class WErzeugerModel : WPModel
    {
        public int ID_Projekt;
        public string Bezeichner;
        public int ID_Type;
        public int ID_WP;
        public string Betriebsart;
        public bool Sperrung;
        public int Sperrzeit_von;
        public int Sperrzeit_bis;
        public int Vorlauf;
        public int Ruecklauf;
        public bool Bivalenter_Betrieb;
        public double Abschaltpunkt;
        public int Nutzungszeit;
        public int ID_SP;
        public int ID_PV;
        public int ID_Solar;
        public bool Heizstab;
        public double Volumen;
        public bool rendeMix;
        public int Solaranteil;
        public int ID_Kessel;
        public int ID_BHKW;
        public double Grenzleistung;
        public int Kollektormodulanzahl;
        public double PV_Leistung;
        public int m_Neigung;
        public int m_Azimut;
        public int ID_PUFFER;

        /// <summary>
        /// NICHT PERSISTENT. Der Anwender hat in der Oberfläche bestätigt, dass dieses
        /// Gerät ein ZWEITES MAL ins Projekt soll (Rückfrage aus
        /// <see cref="AnlagenEindeutigkeit"/>) - der Schreibweg legt dafür eine eigene
        /// Gerätekopie an, statt die vorhandene ID ein zweites Mal zu referenzieren.
        ///
        /// <para>
        /// WOZU DAS FELD. Ohne die Weitergabe der Antwort käme dieselbe Frage zweimal:
        /// einmal im Dialog beim Aufnehmen und ein zweites Mal in
        /// <c>WizardCtrl.Add_WP_Waermeerzeuger</c> beim Speichern. Der Wert wird in keine
        /// Spalte geschrieben; er lebt genau so lange wie die Modell-Liste des Dialogs.
        /// </para>
        /// </summary>
        public bool GeraetekopieErzwingen;

        /// <summary>
        /// <c>ID_Carrier</c> NULL-treu. In der Datenbank ist die Spalte NULL-fähig und
        /// führt beide Schreibweisen für „kein Energieträger": NULL (74 Zeilen im
        /// Bestand) und 0 (3 Zeilen). Als <c>int</c> allein ließ sich das nicht
        /// unterscheiden - jedes Speichern machte aus NULL eine 0.
        /// </summary>
        public int? ID_CarrierRoh;

        /// <summary>
        /// <c>ID_Carrier</c> als Zahl - die Sicht, die alle Aufrufer benutzen; 0 heißt
        /// „kein Energieträger", genauso wie NULL (SchemaKatalog, Schritt 8: „der
        /// lesende Code behandelt beides gleich", eine erzwungene Beziehung auf
        /// <c>energy_carrier.id</c> gibt es bewusst nicht).
        ///
        /// <para>
        /// Aus dem Feld wurde eine Eigenschaft über <see cref="ID_CarrierRoh"/>, damit
        /// die Leseseite NULL durchreichen kann, ohne dass eine einzige Aufrufstelle
        /// angefasst werden muss: Lesen liefert weiterhin <c>int</c>, Zuweisen setzt
        /// den Rohwert.
        /// </para>
        /// </summary>
        public int ID_Carrier
        {
            get { return ID_CarrierRoh ?? 0; }
            set { ID_CarrierRoh = value; }
        }

        // =============================================================================
        // Quellen-/Senken-Konfiguration (Paket 1, Konzept 5.3) - 27 Spalten
        // =============================================================================
        //
        // WARUM DIESE FELDER HIER STEHEN. Der Speicherweg ALLER Erzeuger laeuft ueber
        // WizardCtrl.Del_Projekt_Waermeerzeuger + Add_WP_Waermeerzeuger, also ueber
        // Loeschen und Neuanlegen. Was das Modell nicht kennt, kann Add_WP_Waermeerzeuger
        // nicht zurueckschreiben - die komplette Quellen-/Senken-Konfiguration ging
        // deshalb bei JEDEM Speichern verloren (Wizard, Karten, Kontextmenues). Modell,
        // Leseseite (WErzeugerCtrl.ReadAllFilter/ReadSingle) und Schreibseite
        // (WizardCtrl.AnlagenParameter) fuehren jetzt denselben Spaltensatz.
        //
        // NULL-SEMANTIK - der Grund fuer die nullable Typen:
        //
        //   WS_ID_Puffer, WS_ID_Puffer2, WQ_ID_Puffer sind FREMDSCHLUESSEL auf
        //   Tab_Pufferspeicher.ID mit erzwungener Beziehung (SchemaMigration Schritt 4).
        //   "kein Puffer" ist dort NULL - eine 0 waere eine Phantom-Referenz und wuerde
        //   vom INSERT abgewiesen. null bleibt null, 0 wird nie geschrieben.
        //
        //   WS_Ladeprio, WS_Ladeprio2, WS_Ladeprio_PV, WS_Ladegrenze, WS_Ladegrenze2
        //   sind KEINE Fremdschluessel: 0 heisst dort "nach Vorgabe" bzw. "nicht gesetzt"
        //   (Konzept 3.4), NULL heisst "unbelegt". Beide Zustaende kommen im Bestand vor
        //   und muessen den Roundtrip unterscheidbar ueberleben - deshalb int?/double?
        //   statt int/double.
        //
        //   Textspalten: null = NULL in der Datenbank, "" = leerer Text. Auch dieser
        //   Unterschied bleibt erhalten (Vorbelegung null, nicht "").

        /// <summary>Prioritaet - Einsatzreihenfolge in der Kaskade; NULL = unbelegt.</summary>
        public int? Prioritaet;

        /// <summary>BM_Typ - Betriebsmodus (Laufzeit | Leistung | PV); NULL = unbelegt.</summary>
        public string BM_Typ;

        // --- Waermequelle -------------------------------------------------------------

        /// <summary>WQ_Typ - Aussenluft | Konstant | Pufferspeicher | Profil | CSV | Erdreich.</summary>
        public string WQ_Typ;

        /// <summary>WQ_Temp - konstante Quelltemperatur [Grad C].</summary>
        public double? WQ_Temp;

        /// <summary>WQ_Monatswerte - "t1;...;t12" Monats-Mitteltemperaturen [Grad C].</summary>
        public string WQ_Monatswerte;

        /// <summary>WQ_Wochenwerte - "w1;...;w168" Tagesgang je Wochentag [K].</summary>
        public string WQ_Wochenwerte;

        /// <summary>WQ_CSV - Pfad zur CSV-Datei mit 8760 Stundenwerten.</summary>
        public string WQ_CSV;

        /// <summary>WQ_Puffer - Quell-Puffer ueber Bezeichner (Altweg vor WQ_ID_Puffer).</summary>
        public string WQ_Puffer;

        /// <summary>WQ_ID_Puffer - FK auf Tab_Pufferspeicher.ID; NULL = keiner (nie 0).</summary>
        public int? WQ_ID_Puffer;

        /// <summary>WQ_Spreizung - nutzbare Spreizung des Quellspeichers [K].</summary>
        public double? WQ_Spreizung;

        /// <summary>WQ_Regeneration - Nachladung des Quellspeichers [kW].</summary>
        public double? WQ_Regeneration;

        /// <summary>WQ_Unbegrenzt - Quelle immer verfuegbar (YESNO, kennt kein NULL).</summary>
        public bool WQ_Unbegrenzt;

        /// <summary>WQ_Tiefe - Erdreich: Verlegetiefe bzw. Sondenlaenge [m].</summary>
        public double? WQ_Tiefe;

        /// <summary>WQ_Flaeche - Erdreich: Kollektorflaeche [m2].</summary>
        public double? WQ_Flaeche;

        /// <summary>WQ_Anzahl - Erdreich: Anzahl Sonden.</summary>
        public int? WQ_Anzahl;

        /// <summary>WQ_Bodentyp - Erdreich: Katalogschluessel VDI 4640 Bl. 1.</summary>
        public string WQ_Bodentyp;

        /// <summary>WQ_Quellsystem - Erdreich: Kollektor | Sonde.</summary>
        public string WQ_Quellsystem;

        // --- Waermesenke --------------------------------------------------------------

        /// <summary>WS_Typ - Bedarfsart der Senke: Beides | Warmwasser | Heizung.</summary>
        public string WS_Typ;

        /// <summary>WS_Ziel - Heizkreis | PufferHeizung | PufferBrauchwasser.</summary>
        public string WS_Ziel;

        /// <summary>WS_ID_Puffer - FK auf Tab_Pufferspeicher.ID; NULL = keiner (nie 0).</summary>
        public int? WS_ID_Puffer;

        /// <summary>WS_Ladeprio - 0 = nach Vorgabe, NULL = unbelegt.</summary>
        public int? WS_Ladeprio;

        /// <summary>WS_Ladegrenze - eigene Ladeobergrenze [%]; 0 = Puffer-Regel, NULL = unbelegt.</summary>
        public double? WS_Ladegrenze;

        /// <summary>WS_Ladeprio_PV - Sonderprioritaet bei PV-Ueberschuss; 0 = keine.</summary>
        public int? WS_Ladeprio_PV;

        /// <summary>WS_Ziel2 - Zweitsenke; NULL/leer = keine.</summary>
        public string WS_Ziel2;

        /// <summary>WS_ID_Puffer2 - FK auf Tab_Pufferspeicher.ID; NULL = keiner (nie 0).</summary>
        public int? WS_ID_Puffer2;

        /// <summary>WS_Ladeprio2 - 0 = nach Vorgabe, NULL = unbelegt.</summary>
        public int? WS_Ladeprio2;

        /// <summary>WS_Ladegrenze2 - Ladeobergrenze der Zweitsenke [%]; 0 = Puffer-Regel.</summary>
        public double? WS_Ladegrenze2;

        // =============================================================================
        // PV-Anlagenparameter (Paket A des PV-Ertragsmodells, Stufe E1.3) - 2 Spalten
        // =============================================================================
        //
        // Dieselbe Begruendung, dieselbe NULL-Semantik wie oben: Der Speicherweg ist
        // Loeschen + Neuanlegen, was das Modell nicht kennt, geht bei jedem Speichern
        // verloren. Beide Felder sind nullable, weil NULL hier eine eigene Aussage
        // traegt - "nie gepflegt, es gilt der Vorgabewert" - und sich von einer
        // ausdruecklich eingetragenen 0 unterscheiden muss. Bei PV_Systemverluste sind
        // NULL und 0 rechnerisch gleich; der Unterschied bleibt trotzdem erhalten, damit
        // der Roundtrip aus einer nie gepflegten Zeile keine gepflegte macht.

        /// <summary>
        /// PV_WrWirkungsgrad - Wechselrichter-Wirkungsgrad der PV-Anlage (0…1).
        /// <b>NULL = 0,95</b> (der bis Paket A fest verdrahtete Faktor).
        /// </summary>
        public double? PV_WrWirkungsgrad;

        /// <summary>
        /// PV_Systemverluste - Systemverluste der PV-Anlage [%] (Verschmutzung,
        /// Mismatch, DC-Verkabelung). <b>NULL = 0</b>, also ergebnisneutral.
        /// </summary>
        public double? PV_Systemverluste;

        // =============================================================================
        // PV-Modellwahl und Wechselrichter (Paket B des PV-Ertragsmodells, Stufe E2)
        // =============================================================================
        //
        // Dieselbe Begruendung und dieselbe NULL-Semantik wie bei den zwei Feldern
        // darueber: Der Speicherweg ist Loeschen + Neuanlegen, was das Modell nicht
        // kennt, geht bei jedem Speichern verloren. Alle fuenf sind nullable, weil NULL
        // hier eine eigene Aussage traegt - und bei PV_Modell ist es sogar die
        // wichtigste: NULL heisst "vereinfachtes Modell", also der Rechenweg aus
        // Paket A, Zeichen fuer Zeichen.

        /// <summary>
        /// PV_Modell - das Rechenmodell dieser Anlage:
        /// <see cref="DbWerte.PV_MODELL_EINFACH"/> oder
        /// <see cref="DbWerte.PV_MODELL_ERWEITERT"/>. <b>NULL = EINFACH.</b>
        /// </summary>
        public string PV_Modell;

        /// <summary>
        /// PV_WrNennleistungKw - AC-Nennleistung des Wechselrichters [kW].
        /// <b>NULL = kein Clipping</b>; die Kennlinienauslastung bezieht sich dann auf
        /// die DC-Nennleistung der Anlage. Nur in ERWEITERT wirksam.
        /// </summary>
        public double? PV_WrNennleistungKw;

        /// <summary>PV_WrEta10 - Wechselrichter-Wirkungsgrad bei 10 % Auslastung (0…1).
        /// <b>NULL = 0,94.</b> Nur in ERWEITERT wirksam.</summary>
        public double? PV_WrEta10;

        /// <summary>PV_WrEta50 - Wirkungsgrad bei 50 % Auslastung; <b>NULL = 0,975.</b></summary>
        public double? PV_WrEta50;

        /// <summary>PV_WrEta100 - Wirkungsgrad bei 100 % Auslastung; <b>NULL = 0,97.</b></summary>
        public double? PV_WrEta100;

        public WErzeugerModel()
        {
            ID = 0;
            ID_Projekt = 0;
            Betriebsart = "";;
            Sperrung = false;
            Sperrzeit_von = 0;
            Sperrzeit_bis = 0;
            Vorlauf = 0;
            Ruecklauf = 0;
            Bivalenter_Betrieb= false;
            Abschaltpunkt = 0;
            Nutzungszeit = 0;
            ID_SP = 0;
            ID_PV = 0;
            ID_Solar = 0;
            Heizstab = false;
            Volumen = 0.0;
            rendeMix = false;
            Solaranteil = 0;
            ID_Kessel = 0;
            ID_BHKW = 0;
            Grenzleistung = 0;
            Kollektormodulanzahl = 0;
            PV_Leistung = 0.0;
            m_Neigung = 0;
            m_Azimut = 0;
            ID_PUFFER = 0;
            // 0 wie bisher: eine frisch angelegte Anlage bekommt denselben Wert wie vor
            // der Umstellung auf ID_CarrierRoh. NULL entsteht nur dort, wo die Leseseite
            // ein NULL aus der Datenbank durchreicht.
            ID_Carrier = 0;

            // Vorbelegung der neuen Spalten. Alles bleibt NULL ("unbelegt") - AUSSER den
            // fuenf Ladeprioritaets- und Ladegrenzenfeldern: Sie bekommen 0 ("nach
            // Vorgabe" / "nicht gesetzt", Konzept 3.4). Damit legt eine FRISCH ueber die
            // Oberflaeche erzeugte Anlage exakt dieselben Werte an wie
            // ProjektPuffer.AnlagenzeileParameter und WErzeugerCtrl.Insert
            // (Paket-4-Review, Punkt 9); der Schema-Nachweis der Migration meldet dann
            // keine "Anlagen ohne Ladeprio-Vorgabe". Eine GELESENE Anlage ueberschreibt
            // diese Vorbelegung in WErzeugerCtrl.AusZeile ausdruecklich - auch mit null,
            // sonst wuerde aus einem NULL in der Datenbank beim Speichern eine 0.
            WS_Ladeprio = 0;
            WS_Ladegrenze = 0.0;
            WS_Ladeprio_PV = 0;
            WS_Ladeprio2 = 0;
            WS_Ladegrenze2 = 0.0;
            WQ_Unbegrenzt = false;
        }
    }

}
