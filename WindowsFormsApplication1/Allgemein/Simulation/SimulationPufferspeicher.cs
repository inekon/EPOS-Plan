using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einfaches Energiebilanz-Modell eines thermischen Pufferspeichers für die
    /// Jahressimulation (Stundenschritte, 1 h => kW entspricht kWh).
    ///
    /// Stufe 1 der Pufferspeicher-Integration:
    /// - Nutzbare Kapazität aus Volumen und Temperaturspreizung der Projektkopie
    ///   (Tab_Pufferspeicher: Gesamtvolumen, Vorlauf/Rücklauf):
    ///     Q_max [kWh] = Volumen [l] * 1,16 Wh/(l*K) * (Vorlauf - Rücklauf) / 1000
    /// - Bereitschaftsverluste [kWh/24h] wirken stündlich, anteilig zum Füllstand.
    /// - Keine Temperaturschichtung, keine Begrenzung der Be-/Entladeleistung
    ///   (bewusste Vereinfachung, siehe Konzept).
    /// </summary>
    public class SimulationPufferspeicher
    {
        /// <summary>Verwendung "Heizung": Senkenspeicher eines Wärmeerzeugers (Konzept 6.6).</summary>
        public const string VERWENDUNG_HEIZUNG = DbWerte.PSP_VERWENDUNG_HEIZUNG;

        /// <summary>Verwendung "Quelle": Quellspeicher einer Wärmepumpe (Wärmequelle).</summary>
        public const string VERWENDUNG_QUELLE = DbWerte.PSP_VERWENDUNG_QUELLE;

        /// <summary>
        /// Verwendung "Brauchwasser": Senkenspeicher im Warmwasserkanal (Konzept 5.1).
        ///
        /// Damit trägt <see cref="Verwendung"/> drei Werte: die beiden KANÄLE der
        /// Projektkopie ("Heizung" | "Brauchwasser", Spalte <c>Tab_Pufferspeicher.Verwendung</c>)
        /// und die ROLLE "Quelle", die es dort nicht gibt. Das ist gewollt: Für Anzeige
        /// (<see cref="RolleAnzeige"/>), Serienschlüssel (<see cref="Schluessel"/>) und
        /// Vollzyklen-Bezug zählt allein, ob es sich um einen Quellspeicher handelt —
        /// alles andere ist ein Senkenspeicher.
        /// </summary>
        public const string VERWENDUNG_BRAUCHWASSER = DbWerte.PSP_VERWENDUNG_BRAUCHWASSER;

        /// <summary>
        /// Verwendung „Kombi": EIN Wärmevorrat für BEIDE Kanäle (Etappe D5a,
        /// Konzept_KonfigUI_Hydraulik Anforderungen 4/7).
        ///
        /// Für das Speichermodell selbst ändert der Wert NICHTS — Laden, Entladen,
        /// Bereitschaftsverluste und Kennzahlen arbeiten unverändert auf EINEM
        /// <see cref="SOC"/>. Genau darin besteht die Kombi-Eigenschaft: Die
        /// Kaskadenschleife führt denselben Speicher in beiden Entladereihenfolgen und
        /// bedient je Stunde beide Kanäle aus diesem einen Vorrat.
        /// </summary>
        public const string VERWENDUNG_KOMBI = DbWerte.PSP_VERWENDUNG_KOMBI;

        public string Bezeichner = "";
        public string Erzeuger = "";

        /// <summary>
        /// ID des Speicherdatensatzes (Tab_Pufferspeicher bzw. Tab_Pufferspeicher_STAMM),
        /// 0 = unbekannt. Wird als ID_Pufferspeicher in Tab_ErgebnisPufferspeicher abgelegt
        /// und bildet den technischen Serienschlüssel PUFFER_&lt;ID&gt; der Anzeigen (Konzept 13.3).
        /// </summary>
        public int ID_Pufferspeicher = 0;

        /// <summary>
        /// ID der Energieanlage, zu der dieser Speicher gehört (nur bei Quellspeichern
        /// gesetzt); bildet den Serienschlüssel QUELLE_&lt;AnlagenID&gt;. 0 = unbekannt.
        /// </summary>
        public int ID_Anlage = 0;

        /// <summary>
        /// Rolle des Speichers im Lauf: <see cref="VERWENDUNG_HEIZUNG"/> oder
        /// <see cref="VERWENDUNG_QUELLE"/>. Wird in Tab_ErgebnisPufferspeicher.Verwendung
        /// (TEXT(50)) abgelegt und in den Anzeigen als Rolle ausgewiesen.
        /// </summary>
        public string Verwendung = VERWENDUNG_HEIZUNG;

        /// <summary>Nutzbare Speicherkapazität [kWh]</summary>
        public double Q_max = 0;

        /// <summary>Aktueller Speicherinhalt (State of Charge) [kWh]</summary>
        public double SOC = 0;

        /// <summary>Bereitschaftsverlust bei vollem Speicher [kWh je Stunde]</summary>
        public double VerlustProStunde = 0;

        /// <summary>
        /// Regeneration/Nachladung [kW] - nur bei Verwendung als Wärmequelle
        /// (der Speicher wird laufend aus Umwelt-/Abwärme nachgeladen).
        /// </summary>
        public double RegenerationProStunde = 0;

        /// <summary>
        /// Einschaltschwelle der Speicherregelung als Anteil der nutzbaren
        /// Kapazität (0..1): Fällt der Füllstand darunter, läuft der Erzeuger an.
        /// </summary>
        public double SchwelleEin = 0.10;

        /// <summary>
        /// Abschaltschwelle als Anteil der nutzbaren Kapazität (0..1): Ab diesem
        /// Füllstand gilt der Speicher als voll und der Erzeuger schaltet ab.
        /// Bewusst unter 100 %, da die Bereitschaftsverluste den Füllstand jede
        /// Stunde absenken.
        /// </summary>
        public double SchwelleAus = 0.95;

        // ------------------------------------------------------------------
        // Registry-Felder (Paket 4 - Konzept 6.2/3.4/3.6)
        //
        // Sie werden von SimulationControl.SpeicherRegistryAufbauen aus der
        // Projektkopie Tab_Pufferspeicher gefüllt. Ausgewertet werden sie von der aus
        // der Kaskade geloesten Ladephase (6.3 C/D) mit Prioritaetsaufloesung (3.4) und
        // von der Entladereihenfolge bei mehreren Puffern je Kanal (3.6).
        // ------------------------------------------------------------------

        /// <summary>
        /// Abschaltschwelle für NACHRANGIGE Erzeuger als Anteil der nutzbaren Kapazität
        /// (0..1), Spalte <c>Schwelle_Aus_Nachrang</c> (Konzept 3.4, zweite Stufe der
        /// Ladeobergrenzen). Vorbelegt mit <see cref="SchwelleAus"/> — dann ist die
        /// zweite Stufe wirkungslos, und genau das ist der verhaltensneutrale Default.
        /// </summary>
        public double SchwelleAusNachrang = 0.95;

        /// <summary>
        /// Entladereihenfolge bei mehreren Puffern desselben Kanals (Konzept 3.6),
        /// Spalte <c>Entladeprio</c>. 0 = automatisch.
        /// </summary>
        public int Entladeprio = 0;

        /// <summary>
        /// MINDESTFÜLLSTAND/NOTRESERVE als Anteil der nutzbaren Kapazität (0…1), Spalte
        /// <c>Schwelle_Reserve</c> (Paket BHKW-Regulär, Entscheidung des Anwenders
        /// 17.08.2026, Punkt 3). 0 = keine Reserve.
        ///
        /// Sie wirkt NUR, wenn <see cref="BhkwReserveGilt"/> gesetzt ist — siehe dort.
        /// </summary>
        public double SchwelleReserve = 0;

        /// <summary>
        /// <c>true</c>, wenn dieser Speicher im BILANZRAUM DES BHKW steht, also Ziel eines
        /// BHKW-Ladeauftrags ist (<c>Kaskadenschleife.BhkwAuftraegeZuordnen</c> setzt das
        /// Feld je Lauf). Erst dann ist <see cref="SchwelleReserve"/> wirksam.
        ///
        /// <para><b>WARUM DIESER SCHALTER — und warum die Reserve nicht global gilt.</b>
        /// Die Entscheidung des Anwenders lautet ausdrücklich: Die Notreserve wirkt auf die
        /// BHKW-Entladung, alle anderen Erzeuger entladen unverändert bis 0. Ein BHKW ist
        /// eine Maschine mit Anlaufverhalten — fährt sein Speicher vollständig leer, gibt es
        /// keinen Vorrat, aus dem die nächste Bedarfsspitze bis zum Anlaufen gedeckt werden
        /// könnte. Wärmepumpe, Kessel und Solarthermie haben dieses Problem nicht, und für
        /// sie wäre eine Untergrenze eine stille Verhaltensänderung: Sie ließe Bedarf offen,
        /// obwohl Wärme im Speicher liegt.</para>
        ///
        /// <para><b>WARUM AM SPEICHER und nicht an der Entladung.</b> Die Entladung eines
        /// Speichers ist NICHT erzeugerbezogen: Ein Puffer wird entladen, weil Bedarf offen
        /// ist, nicht weil ein bestimmter Erzeuger ihn geladen hat. Es gibt keinen „BHKW-
        /// Entladevorgang", den man einzeln begrenzen könnte. Die Notreserve ist deshalb als
        /// das ausgedrückt, was sie fachlich ist: eine Eigenschaft DES SPEICHERS — er soll
        /// nicht leerlaufen —, aktiviert genau an den Speichern, auf die ein BHKW angewiesen
        /// ist.</para>
        ///
        /// <para><b>DOKUMENTIERTE ABGRENZUNG (geteilter Speicher).</b> Lädt außer dem BHKW
        /// noch ein anderer Erzeuger denselben Puffer, wirkt die Reserve auch auf dessen
        /// Entladung — ein Speicher hat einen Füllstand, nicht zwei, und eine getrennte
        /// Untergrenze je Herkunftsanteil wäre neue Physik. Der Regelfall ist der
        /// EIGENE BHKW-Puffer (Migrationsregel R6 und
        /// <c>ProjektPuffer.SQL_BHKW_AUF_PUFFER</c> legen ihn genau so an); der geteilte
        /// Fall bleibt als offener Punkt vermerkt.</para>
        ///
        /// <para>OHNE BHKW im Projekt ist das Feld an JEDEM Speicher <c>false</c>, und
        /// <see cref="EntnahmeObergrenze"/> liefert <c>double.MaxValue</c> — die Entladung
        /// rechnet dann Anweisung für Anweisung wie bisher.</para>
        /// </summary>
        public bool BhkwReserveGilt = false;

        /// <summary>Projekt, zu dem die Speicherzeile gehört (Tab_Pufferspeicher.ID_Projekt).</summary>
        public int ID_Projekt = 0;

        /// <summary>
        /// Hysteresezustand des Speichers: <c>true</c>, solange er nachgeladen wird.
        ///
        /// Ersetzt ab Etappe 4b den heute MODULÜBERGREIFENDEN <c>bool _speicherLaden</c>
        /// in <c>SimulationWaermepumpe</c> (Konzept 6.2). Der ist bei mehreren Speichern
        /// nicht mehr tragfähig: Ein Zustand kann nicht für zwei Speicher gleichzeitig
        /// gelten. Hier gehört er dorthin, wo er hingehört — an den Speicher.
        ///
        /// ALTPFAD: gesetzt, aber ungelesen — dort gilt weiter das modulübergreifende
        /// Feld. Im zweikanaligen Weg ist dieses Feld der Hysteresezustand
        /// (<see cref="HystereseFortschreiben"/>, Phase A der Reihenfolge-Invariante).
        ///
        /// Vorbelegung <c>true</c> — dieselbe wie beim abzulösenden Feld: Der Lauf
        /// startet mit leerem Speicher, also zuerst laden.
        /// </summary>
        public bool LaedtGerade = true;

        /// <summary>
        /// <c>true</c>, wenn dieser Speicher im laufenden Rechengang tatsächlich
        /// mitrechnet.
        ///
        /// Im ALTPFAD trägt das GENAU der Senkenspeicher, den die Z-basierte
        /// Initialisierung ermittelt (Alias <c>SimulationControl.puffer_wp</c>), sowie
        /// jeder Quellspeicher eines WP-Moduls. Alle übrigen Registry-Einträge sind
        /// Vorbereitung und stehen auf <c>false</c>.
        ///
        /// Warum das nötig ist — und nicht bloß Zierde: <c>puffer_wp</c> ist laut Konzept
        /// 6.7 der „erste Heizungs-Puffer der Registry". Die Registry enthält aber seit
        /// der Datenmigration 5.5 auch Puffer, die niemand rechnet — etwa den von
        /// Regel R6 angelegten „BHKW-Pendelspeicher" oder den Puffer einer reinen
        /// Solarthermie-Zuordnung. Ohne diese Einschränkung zeigte <c>puffer_wp</c> in
        /// Projekten ohne Wärmepumpen-Zuordnung plötzlich auf einen solchen Speicher, und
        /// aus einem „kein Puffer" würde still ein „Puffer mit Q_max" — mit voller
        /// Wirkung auf Ergebnis und Anzeige.
        ///
        /// DAS FLAG TRAGEN die Speicher mit einer SENKEN-Referenz einer Projektanlage
        /// (Senkenliste bzw. die gespiegelten Altspalten) und die Quellspeicher
        /// (<c>WQ_ID_Puffer</c>) — also genau die, die ein Erzeuger laden oder entladen
        /// kann (<c>SimulationControl.RegistryFuerZweikanaligOeffnen</c>). Ein Puffer ohne
        /// Senkenreferenz rechnet nicht mit: Er wuerde sonst mit lauter Nullen in der
        /// Ergebnispersistenz erscheinen und ueber <c>puffer_wp</c> eine Speicherkapazitaet
        /// melden, die kein Erzeuger benutzt.
        /// </summary>
        public bool ImRechenpfad = false;

        /// <summary>
        /// Zähler der <see cref="StundeAbschliessen"/>-Aufrufe seit <see cref="Reset"/>.
        ///
        /// Konzept 6.3 verlangt GENAU EINEN Aufruf je Speicher und Stunde (Phase G) —
        /// heute ruft die Wärmepumpe ihn teils innerhalb der Modulschleife, wodurch die
        /// Bereitschaftsverluste eines von zwei Modulen genutzten Quellspeichers doppelt
        /// gezählt werden. Der Zähler macht die Einhaltung MESSBAR: Nach einem
        /// vollständigen Jahreslauf muss er 8760 sein.
        ///
        /// Reine Instrumentierung — er geht in kein Ergebnis und in keine Ganglinie ein.
        /// </summary>
        public int Abschluesse = 0;

        // Ganglinien für Auswertung, Charts und CSV-Export
        public float[] SOC_stuendlich = new float[8760];
        public float[] Ladung_stuendlich = new float[8760];
        public float[] Entladung_stuendlich = new float[8760];

        // Jahressummen [kWh]
        public double Ladung_gesamt = 0;
        public double Entladung_gesamt = 0;
        public double Verluste_gesamt = 0;

        /// <summary>
        /// PAKET E1 (Konzept 4.4): dieselbe Jahressumme wie
        /// <see cref="Entladung_gesamt"/>, aber KANALINDIZIERT
        /// (<see cref="Kanal.HEIZUNG"/>/<c>BRAUCHWASSER</c>/<c>PROZESS</c>) [kWh].
        ///
        /// <para>Gebucht in <see cref="Entladen(double,int,int)"/> — also an genau der
        /// Stelle, an der auch der Skalar fortgeschrieben wird, aus derselben Größe
        /// <c>umsatz</c>. Es gibt keine zweite Rechnung und keinen zweiten Rundungsweg;
        /// die Summe der drei Werte ist der Skalar.</para>
        ///
        /// <para>Sie geht als <c>Entladung_Heizung/_Brauchwasser/_Prozess</c> in
        /// <c>Tab_ErgebnisPufferspeicher</c> (Migrationsschritt 52). Die DURCHFLUSSmenge
        /// zählt hier — wie im Skalar — NICHT mit; sie steht getrennt in
        /// <see cref="Durchsatz_Entladung_gesamt"/>.</para>
        /// </summary>
        public readonly double[] Entladung_Kanal = new double[Kanal.ANZAHL];

        // ------------------------------------------------------------------
        // DURCHSATZ getrennt vom UMSATZ (Nacharbeit Paket 6, Befund N6)
        //
        // Der Speicher ist eine hydraulische Weiche: Was er in derselben Stunde
        // wieder abgibt, ist DURCHGEFLOSSEN und war nie Speicherinhalt (Laden mit
        // Durchlass, Nutzerentscheidung zu 4b-1). Bis Paket 6 zählte diese Menge in
        // Ladung_gesamt/Entladung_gesamt mit — und damit auch in Vollzyklen. Bei einer
        // Puffer-HAUPTsenke läuft die GESAMTE Produktion durch den Speicher; die
        // Kennzahl meldete dann Werte wie 6.719 Vollzyklen an einem 23-kWh-Puffer und
        // maß in Wahrheit den Durchsatz, nicht die Speicherbeanspruchung.
        //
        // ZERLEGUNG des Füllstands: A = min(SOC, Q_max) ist der SPEICHERINHALT,
        // B = max(0, SOC − Q_max) der Anteil, der in dieser Stunde nur durchfließt.
        //   Laden   füllt zuerst A, dann B.
        //   Entladen entnimmt zuerst B, dann A (der Durchfluss verlässt den Speicher
        //            zuerst — Phase E holt ihn ausdrücklich vorab, DurchsatzEntladen).
        //   Bereitschaftsverluste treffen ebenfalls zuerst B.
        // Damit gilt jede der beiden Bilanzen für sich exakt:
        //   Ladung_gesamt − Entladung_gesamt − Verluste_gesamt                   = A
        //   Durchsatz_Ladung_gesamt − Durchsatz_Entladung_gesamt
        //                            − Durchsatz_Verluste_gesamt                 = B
        // und ihre Summe ist der bisherige Gesamtausdruck (= SOC).
        //
        // OHNE Durchlass ist B durchgehend 0: Die
        // Durchsatzgrößen bleiben exakt 0,0 und die drei Altgrößen sind bitgleich die
        // bisherigen.
        //
        // NICHT PERSISTIERT: Tab_ErgebnisPufferspeicher hat keine Spalte dafür. Eine
        // Schemaänderung gehört nicht in diese Nacharbeit; der Durchsatz steht am
        // Objekt und in der Protokollmeldung. Vorgemerkte Erweiterung.
        // ------------------------------------------------------------------

        /// <summary>Durchgeflossene Wärme je Stunde [kWh] (Aufnahme über Q_max hinaus).</summary>
        public float[] Durchsatz_Ladung_stuendlich = new float[8760];

        /// <summary>Wieder abgegebener Durchfluss je Stunde [kWh].</summary>
        public float[] Durchsatz_Entladung_stuendlich = new float[8760];

        /// <summary>Jahressumme der durchgeflossenen Aufnahme [kWh]; ohne Durchlass exakt 0.</summary>
        public double Durchsatz_Ladung_gesamt = 0;

        /// <summary>Jahressumme der wieder abgegebenen Durchflussmenge [kWh]; ohne Durchlass exakt 0.</summary>
        public double Durchsatz_Entladung_gesamt = 0;

        /// <summary>Bereitschaftsverluste, die auf den Durchflussanteil entfielen [kWh]; praktisch 0.</summary>
        public double Durchsatz_Verluste_gesamt = 0;

        /// <summary>Aufnahme der Stunde einschließlich Durchfluss [kWh] — die physikalische Menge.</summary>
        public double Ladung_gesamt_Brutto { get { return Ladung_gesamt + Durchsatz_Ladung_gesamt; } }

        /// <summary>Abgabe der Stunde einschließlich Durchfluss [kWh] — die physikalische Menge.</summary>
        public double Entladung_gesamt_Brutto { get { return Entladung_gesamt + Durchsatz_Entladung_gesamt; } }

        /// <summary>
        /// In dieser Stunde bereits VERGEBENE Ladefähigkeit [kWh] (Nacharbeit Paket 6,
        /// Befund N3).
        ///
        /// Das BHKW entscheidet seine Motorzuschaltung in Phase B — also VOR den
        /// Ladephasen C und D — gegen den Wärmeraum seiner Senke. Ohne Reservierung
        /// könnte ein Erzeuger mit besserer Ladepriorität (Solarthermie 10, Wärmepumpe
        /// 20 gegen BHKW 30) diesen Raum in Phase C aufbrauchen; das BHKW hätte dann
        /// bereits produziert, und die Wärme würde verworfen (gemessen an einem
        /// präparierten 1024: 12,06 MWh Verwurf).
        ///
        /// <see cref="Ladefaehigkeit"/> zieht den reservierten Betrag ab; der
        /// reservierende Erzeuger gibt ihn unmittelbar vor seinem eigenen Ladevorgang
        /// wieder frei (<see cref="ReservierungFreigeben"/>). Die Kaskadenschleife setzt
        /// das Feld zu Beginn jeder Stunde zurück — eine nicht eingelöste Reservierung
        /// kann sich deshalb nicht in die nächste Stunde schleppen.
        ///
        /// ALTPFAD: 0 und ungelesen (dort gibt es weder Ladeaufträge noch Ladephasen).
        /// </summary>
        public double Reserviert = 0;

        /// <summary>
        /// ΔT [K], mit dem <see cref="Init"/> gerechnet hat, WEIL kein Temperaturpaar
        /// gepflegt war; 0 = es galt das gepflegte Paar.
        ///
        /// Projektgrundsatz „sichtbar falsch ist besser als still falsch" (Nacharbeit
        /// Paket 6, Befund N2): Ein Rückfall halbiert (10 K) oder verändert (20 K) die
        /// nutzbare Kapazität gegenüber einem gepflegten Paar. Der Aufrufer protokolliert
        /// ihn deshalb; das Modell selbst bleibt dialog- und ausgabefrei.
        /// </summary>
        public double RueckfallDeltaT = 0;

        // ------------------------------------------------------------------
        // Kennzahlen des Laufs (Konzept 6.6) - erst nach KennzahlenBerechnen()
        // gültig, davor 0. Sie werden in Tab_ErgebnisPufferspeicher abgelegt
        // und speisen die Ergebnistabelle der Detailansicht.
        // ------------------------------------------------------------------

        /// <summary>Mittlerer Füllstand über das Jahr [kWh] (Mittel von SOC_stuendlich).</summary>
        public double SOC_Mittel = 0;

        /// <summary>Höchster Füllstand des Jahres [kWh] (Maximum von SOC_stuendlich).</summary>
        public double SOC_Max = 0;

        /// <summary>
        /// Vollzyklen des Jahres (Konzept 6.6), 0 bei Q_max &lt;= 0
        /// (Division-durch-Null-Absicherung).
        ///
        /// NACHARBEIT PAKET 6, BEFUND N6: Bezugsgröße ist der SPEICHERUMSATZ ohne den
        /// Durchfluss (siehe <see cref="Durchsatz_Ladung_gesamt"/>). Vorher zählte die
        /// hydraulische Weiche mit, und bei einer Puffer-Hauptsenke meldete die Kennzahl
        /// Werte, die nichts mehr über die Beanspruchung des Speichers aussagten.
        ///
        /// Der NUTZUMSATZ hängt an der Rolle:
        ///
        ///   Senkenspeicher (Heizung): Ladung_gesamt / Q_max — er startet leer und wird
        ///                             vom Erzeuger beladen, die Ladung ist der Umsatz.
        ///   Quellspeicher   (Quelle): Entladung_gesamt / Q_max — er startet VOLL
        ///                             (WaermequelleClass.Quellspeicher setzt SOC = Q_max)
        ///                             und wird entzogen; über Ladung_gesamt gerechnet
        ///                             fehlte genau die erste Füllung, und ohne
        ///                             Regeneration käme 0 heraus, obwohl der Speicher
        ///                             das ganze Jahr gearbeitet hat.
        /// </summary>
        public double Vollzyklen = 0;

        /// <summary>
        /// Initialisiert den Speicher aus den Zuordnungs- und Stammdaten.
        /// </summary>
        /// <param name="volumenLiter">Gesamtvolumen [l] (Tab_Pufferspeicher)</param>
        /// <param name="vorlauf">Vorlauftemperatur [°C] (Tab_Pufferspeicher)</param>
        /// <param name="ruecklauf">Rücklauftemperatur [°C] (Tab_Pufferspeicher)</param>
        /// <param name="bereitschaftsverlusteProTag">Bereitschaftsverluste [kWh/24h] (Tab_Pufferspeicher)</param>
        /// <param name="rueckfallDeltaT">
        /// ΔT [K], das gilt, wenn kein vollständiges Temperaturpaar vorliegt. Vorgabe
        /// 10 K — der generische Notnagel. Der BHKW-PENDELSPEICHER übergibt hier 20 K:
        /// Die Altformel <c>Liter · 20 / 860</c> hatte diese Spreizung fest verdrahtet,
        /// und ein Rückfall auf 10 K würde seine Kapazität ohne fachlichen Grund
        /// halbieren (Nacharbeit Paket 6, Befund N2).
        /// </param>
        public void Init(double volumenLiter, int vorlauf, int ruecklauf,
                         double bereitschaftsverlusteProTag, double rueckfallDeltaT = 10)
        {
            double deltaT = vorlauf - ruecklauf;

            RueckfallDeltaT = 0;
            if (deltaT <= 0)
            {
                // Fallback, falls keine Temperaturen gepflegt sind. Er wird am Objekt
                // vermerkt, damit der Aufrufer ihn protokollieren kann (N2).
                deltaT = rueckfallDeltaT > 0 ? rueckfallDeltaT : 10;
                RueckfallDeltaT = deltaT;
            }

            // 1,16 Wh/(l*K) -> kWh
            Q_max = volumenLiter * 1.16 * deltaT / 1000.0;
            VerlustProStunde = bereitschaftsverlusteProTag / 24.0;

            // PAKET P1 (Konzept 7.2): Das WIRKSAME TEMPERATURPAAR wird festgehalten
            // statt - wie bisher - sofort zu Q_max verrechnet und verworfen. Es ist die
            // Bezugsachse der Schichtebene: T[i] liegt zwischen RL_eff und VL_eff, und
            // die Schichtenergie ist der Anteil daran. Die Rückfallregel bleibt Wort für
            // Wort dieselbe, die eine Zeile höher schon in Q_max eingegangen ist -
            // RL_eff ist der gepflegte Rücklauf, VL_eff = RL_eff + ΔT.
            VolumenLiter = volumenLiter;
            RL_eff = ruecklauf;
            VL_eff = ruecklauf + deltaT;

            // MINDEST-NUTZTEMPERATUR: Vorbelegung RL_eff für JEDEN Kanal - die
            // verhaltensneutrale Vorgabe aus Konzept 7.2. Unterhalb des Rücklaufs trägt
            // keine Schicht Energie; die Bedingung „T ≥ T_Nutz" ist damit für jede
            // Schicht mit Inhalt erfüllt, und die Entladefähigkeit ist der gesamte
            // Vorrat. Der Registry-Aufbau übersteuert allein den Brauchwasserkanal, und
            // nur, wenn T_Nutz_BW gepflegt ist (F7).
            for (int k = 0; k < TNutz.Length; k++) TNutz[k] = RL_eff;

            Reset();
        }

        /// <summary>Setzt den Speicherzustand für einen neuen Simulationslauf zurück.</summary>
        public void Reset()
        {
            // Hysterese wie beim abzulösenden _speicherLaden: Der Lauf beginnt mit
            // leerem Speicher, also zuerst laden.
            LaedtGerade = true;
            SOC = 0;
            Ladung_gesamt = 0;
            Entladung_gesamt = 0;
            Verluste_gesamt = 0;
            SOC_Mittel = 0;
            SOC_Max = 0;
            Vollzyklen = 0;
            Abschluesse = 0;
            Array.Clear(SOC_stuendlich, 0, SOC_stuendlich.Length);
            Array.Clear(Ladung_stuendlich, 0, Ladung_stuendlich.Length);
            Array.Clear(Entladung_stuendlich, 0, Entladung_stuendlich.Length);

            // N6: Durchsatzzähler und Reservierung gehören zum Laufzustand.
            Reserviert = 0;
            Durchsatz_Ladung_gesamt = 0;
            Durchsatz_Entladung_gesamt = 0;
            Durchsatz_Verluste_gesamt = 0;
            Array.Clear(Durchsatz_Ladung_stuendlich, 0, Durchsatz_Ladung_stuendlich.Length);
            Array.Clear(Durchsatz_Entladung_stuendlich, 0, Durchsatz_Entladung_stuendlich.Length);

            // PAKET E1: die Kanalzeile der Entladung gehört ebenso zum Laufzustand.
            Array.Clear(Entladung_Kanal, 0, Entladung_Kanal.Length);

            // PAKET P1: Schichtebene und Stundenbudget gehören zum Laufzustand.
            // SchichtenAufbauen setzt alle Schichten auf RL_eff - der leere Speicher
            // (Konzept 7.2, „T[i], Start = RL_eff").
            T_oben_Mittel = null;
            T_oben_Min = null;
            Array.Clear(T_oben_stuendlich, 0, T_oben_stuendlich.Length);
            Array.Clear(T_unten_stuendlich, 0, T_unten_stuendlich.Length);
            SchichtInvarianteVerletzungen = 0;
            SchichtInvarianteMaxAbweichung = 0;
            BudgetZuruecksetzen();
            SchichtenAufbauen();
        }

        /// <summary>
        /// Reserviert Ladefähigkeit für einen Erzeuger, der seine Produktion bereits in
        /// Phase B festgelegt hat (Befund N3). Mehrfachaufrufe summieren sich.
        /// </summary>
        public void Reservieren(double energieKWh)
        {
            if (energieKWh <= 0) return;
            Reserviert += energieKWh;
        }

        /// <summary>Gibt die Reservierung wieder frei — unmittelbar vor dem eigenen Ladevorgang.</summary>
        public void ReservierungFreigeben()
        {
            Reserviert = 0;
        }

        /// <summary>
        /// Lädt den Speicher mit der angebotenen Energie [kWh] und liefert zurück,
        /// wie viel davon tatsächlich aufgenommen wurde (Rest: Speicher voll).
        /// </summary>
        public double Laden(double energieKWh, int stunde)
        {
            return Laden(energieKWh, stunde, 0);
        }

        /// <summary>
        /// Laden mit DURCHLASS — der Speicher als hydraulische Weiche (Paket 4,
        /// Nutzerentscheidung zu Befund 4b-1 vom 14.08.2026).
        ///
        /// Ein Pufferspeicher drosselt die Anlage nicht auf seinen Inhalt: Er wird
        /// geladen, WÄHREND die Last aus ihm entnimmt. In der Stundenbilanz heißt das,
        /// dass die Aufnahme einer Stunde die freie Kapazität um genau die Menge
        /// übersteigen darf, die im selben Zeitschritt wieder entnommen wird
        /// (<paramref name="durchlass"/>, ermittelt über <see cref="Bilanzraum"/>).
        /// Der Füllstand liegt dann VORÜBERGEHEND über <see cref="Q_max"/>; die
        /// Nachentladung (Phase E) zieht ihn im selben Zeitschritt wieder herunter,
        /// bevor Phase G die Bereitschaftsverluste rechnet und den Wert in die
        /// Ganglinie schreibt. Ohne Durchlass (<c>0</c>) ist das Verhalten exakt das
        /// bisherige.
        /// </summary>
        /// <param name="durchlass">
        /// Im selben Zeitschritt absehbare Entnahme [kWh], um die die Aufnahme über die
        /// freie Kapazität hinausgehen darf. Negative Werte gelten als 0.
        /// </param>
        public double Laden(double energieKWh, int stunde, double durchlass)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            // PAKET P1 (Konzept 6.3, Befund K2-O6): LADELEISTUNGSGRENZE als BUDGET DER
            // STUNDE. Ohne gepflegte Grenze (Vorbelegung 0) ist das Budget
            // double.MaxValue, der Zweig wird gar nicht erst betreten und die Rechnung
            // bleibt Anweisung für Anweisung die bisherige.
            StundeBeginnen(stunde);

            if (durchlass < 0) durchlass = 0;
            double frei = Q_max - SOC + durchlass;
            double ladung = Math.Min(energieKWh, frei);
            if (LadeleistungMax > 0 && ladung > _ladebudget) ladung = _ladebudget;
            if (ladung <= 0) return 0;

            // N6: Aufnahme in SPEICHERUMSATZ und DURCHFLUSS zerlegen. Der Teil bis Q_max
            // ist Umsatz, alles darüber fließt in derselben Stunde weiter. Ohne Durchlass
            // ist der zweite Summand konstruktiv 0 - die Buchung ist dann bitgleich die
            // bisherige.
            double raumBisQmax = Q_max - SOC;
            if (raumBisQmax < 0) raumBisQmax = 0;
            double umsatz = Math.Min(ladung, raumBisQmax);
            double durchfluss = ladung - umsatz;

            SOC += ladung;
            Ladung_gesamt += umsatz;
            Durchsatz_Ladung_gesamt += durchfluss;
            if (stunde >= 0 && stunde < 8760)
            {
                Ladung_stuendlich[stunde] += (float)umsatz;
                if (durchfluss > 0) Durchsatz_Ladung_stuendlich[stunde] += (float)durchfluss;
            }

            if (LadeleistungMax > 0) _ladebudget -= ladung;

            // PAKET P1 (Konzept 7.4 Punkt 1): Die SOC-Buchung steht - jetzt vollzieht die
            // Schichtebene den UMSATZanteil nach. Der DURCHFLUSS bleibt bewusst draußen
            // (Konzept 7.3): Er ist hydraulisch durchströmende Wärme und war nie
            // Speicherinhalt; eine bei VL_eff gekappte Temperatursumme könnte ihn gar
            // nicht darstellen.
            Schicht_Beladen(umsatz);
            return ladung;
        }

        /// <summary>
        /// Entnimmt die angeforderte Energie [kWh] aus dem Speicher und liefert
        /// zurück, wie viel tatsächlich geliefert werden konnte (Rest: Speicher leer).
        /// </summary>
        /// <param name="kanal">
        /// PAKET E1: Bedarfskanal, in den diese Entnahme geht — die Entladeordnung läuft
        /// je Kanal, der Aufrufer kennt ihn also. Er entscheidet ausschließlich über die
        /// KANALZEILE <see cref="Entladung_Kanal"/>; an der Speicherphysik und an
        /// <see cref="Entladung_gesamt"/> ändert er nichts.
        ///
        /// <para>VORBELEGUNG <see cref="Kanal.HEIZUNG"/>: Die Entnahme eines Moduls aus
        /// seinem QUELLpuffer trägt keinen Bedarfskanal — sie wird auf dem Heizkanal
        /// gebucht, genau wie in
        /// <c>Kaskadenschleife.Anteil_Entladen(sp, gedeckt)</c> (altverhaltenserhaltende
        /// Vorbelegung des Kanalmodells, Konzept 4.2/F18). Ohne diese eine Konvention
        /// wäre die Summenzusage „Σ Entladung_Kanal = Entladung_gesamt" für
        /// Quellspeicherzeilen nicht einlösbar.</para>
        /// </param>
        public double Entladen(double energieKWh, int stunde, int kanal = Kanal.HEIZUNG)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            // PAKET P1 (Konzept 6.3, Befund K2-O6): ENTLADELEISTUNGSGRENZE als BUDGET DER
            // STUNDE - nicht je Aufruf. Ein Heizungspuffer wird in derselben Stunde für
            // den Prozess- UND den Heizkanal durchlaufen (Zwei-Pass); je Aufruf gewährt
            // hätte er die Stundengrenze zweimal bekommen. Ohne gepflegte Grenze
            // (Vorbelegung 0) ist das Budget double.MaxValue und der Zweig wird nicht
            // betreten - die Rechnung bleibt die bisherige.
            StundeBeginnen(stunde);

            double entnahme = Math.Min(energieKWh, SOC);
            if (EntladeleistungMax > 0 && entnahme > _entladebudget) entnahme = _entladebudget;
            if (entnahme <= 0) return 0;

            // N6: Gegenstück zur Zerlegung in Laden — der Durchfluss verlässt den
            // Speicher zuerst. Ohne Durchlass ist SOC <= Q_max, der erste Summand 0 und
            // die Buchung bitgleich die bisherige.
            double ueber = SOC - Q_max;
            if (ueber < 0) ueber = 0;
            double durchfluss = Math.Min(entnahme, ueber);
            double umsatz = entnahme - durchfluss;

            SOC -= entnahme;
            Entladung_gesamt += umsatz;
            Durchsatz_Entladung_gesamt += durchfluss;

            // PAKET E1: dieselbe Menge, nur kanalindiziert. Der Skalar darüber bleibt
            // getrennt akkumuliert und wird ausdrücklich NICHT aus dieser Zeile
            // aufsummiert — er ist der führende Wert der Ergebniszeile und soll sich
            // durch die Aufteilung nicht um die Rundung einer Summe verschieben (dieselbe
            // Regel wie bei Kaskadenschleife._entladungJeArt).
            if (kanal >= 0 && kanal < Kanal.ANZAHL) Entladung_Kanal[kanal] += umsatz;

            if (stunde >= 0 && stunde < 8760)
            {
                Entladung_stuendlich[stunde] += (float)umsatz;
                if (durchfluss > 0) Durchsatz_Entladung_stuendlich[stunde] += (float)durchfluss;
            }

            if (EntladeleistungMax > 0) _entladebudget -= entnahme;

            // PAKET P1 (Konzept 7.4 Punkt 2): IDEALE VERDRÄNGUNG am Anschluss. Die
            // SOC-Buchung steht; die Schichtebene vollzieht den Umsatzanteil nach, der
            // Durchfluss bleibt draußen (7.3).
            Schicht_Entladen(umsatz, kanal);
            return entnahme;
        }

        /// <summary>
        /// Verrechnet den stündlichen Bereitschaftsverlust (anteilig zum Füllstand)
        /// und speichert den Speicherzustand der Stunde für die Auswertung.
        /// </summary>
        public void StundeAbschliessen(int stunde)
        {
            Abschluesse++;

            // PAKET P1 (Konzept 7.4 Punkt 3): VERTIKALER AUSGLEICH vor den Verlusten -
            // Wärmeleitung zwischen Nachbarschichten und anschließende
            // Inversionsmischung. Beides ist ein reiner Austausch INNERHALB des
            // Speichers und lässt die Summe der Schichtenergie unberührt; bei N = 1 gibt
            // es kein Schichtpaar und die Methode tut nichts.
            Schicht_Ausgleich();

            if (Q_max > 0 && SOC > 0)
            {
                // Der Anteil ist auf 1 begrenzt: Mit dem Durchlass (Laden mit
                // hydraulischer Weiche) kann SOC innerhalb einer Stunde über Q_max
                // liegen. Bis Phase G ist er normalerweise wieder darunter — bliebe
                // doch etwas stehen, dürfte daraus kein überhöhter Bereitschaftsverlust
                // werden. Ohne Durchlass gilt SOC <= Q_max, die Klemmung greift dann nie
                // und die Rechnung bleibt bitgleich wie bisher.
                double anteil = SOC / Q_max;
                if (anteil > 1) anteil = 1;

                double verlust = VerlustProStunde * anteil;
                if (verlust > SOC) verlust = SOC;

                // N6: Steht ausnahmsweise noch Durchfluss im Speicher (Phase E konnte
                // ihn nicht vollständig abgeben), trägt er den Verlust zuerst - sonst
                // ginge die getrennte Bilanz um genau diesen Betrag nicht mehr auf.
                // Im Regelfall ist SOC <= Q_max und der Ausdruck exakt 0.
                double ueber = SOC - Q_max;
                if (ueber < 0) ueber = 0;
                double verlustDurchfluss = Math.Min(verlust, ueber);

                SOC -= verlust;
                Verluste_gesamt += verlust - verlustDurchfluss;
                Durchsatz_Verluste_gesamt += verlustDurchfluss;

                // PAKET P1 (Konzept 7.4 Punkt 4): Der SCHICHTANTEIL des Verlusts - alles
                // außer dem Anteil, den der Durchfluss getragen hat - wird auf die
                // Schichten verteilt: nach Wärmeabgabefläche und gewichtet mit
                // (T[i] − RL_eff)/(VL_eff − RL_eff). Bei N = 1 ist das exakt die
                // füllstandsanteilige Rechnung, die eine Zeile höher schon auf dem SOC
                // gelaufen ist.
                Schicht_Verluste(verlust - verlustDurchfluss);

                // Auftrieb ein zweites Mal: Der Deckelanteil der obersten Schicht lässt
                // sie etwas stärker abkühlen als ihre Nachbarin darunter - siehe
                // Schicht_Inversion. Ohne diesen Durchgang stünde die winzige Inversion
                // im Stundenergebnis.
                Schicht_Inversion();
            }

            if (stunde >= 0 && stunde < 8760) SOC_stuendlich[stunde] = (float)SOC;

            // PAKET P1: Die Schicht-Invariante Σ Schichtenergie == min(SOC, Q_max)
            // (Konzept 7.3) am Ende JEDER Stunde nachziehen und - im Debug-Build -
            // prüfen. Danach die Ganglinien der obersten und untersten Schicht.
            Schicht_Nachfuehren();
            Schicht_Ganglinie(stunde);

#if DEBUG
            SchichtprobeMelden(stunde);
#endif
        }

        /// <summary>
        /// Wertet nach dem Lauf die Ganglinie SOC_stuendlich aus und bildet die
        /// Kennzahlen der Ergebnis-Persistenz (Konzept 6.6):
        /// SOC_Mittel und SOC_Max aus der Stundenganglinie,
        /// Vollzyklen = Ladung_gesamt / Q_max mit Division-durch-Null-Absicherung.
        ///
        /// Mehrfachaufruf ist unschädlich - die Methode rechnet ausschließlich aus
        /// den Ganglinien und Jahressummen, nicht inkrementell.
        /// </summary>
        public void KennzahlenBerechnen()
        {
            double summe = 0;
            double max = 0;
            int n = (SOC_stuendlich != null) ? SOC_stuendlich.Length : 0;
            for (int i = 0; i < n; i++)
            {
                double v = SOC_stuendlich[i];
                summe += v;
                if (v > max) max = v;
            }

            SOC_Mittel = (n > 0) ? summe / n : 0;
            SOC_Max = max;

            // Rollenabhängige Bezugsgröße - siehe Kommentar an Vollzyklen.
            double umsatz = (Verwendung == VERWENDUNG_QUELLE) ? Entladung_gesamt : Ladung_gesamt;
            Vollzyklen = (Q_max > 0) ? umsatz / Q_max : 0;

            // PAKET P1 (Befund E1-O5): die beiden Temperaturkennzahlen der OBERSTEN
            // Schicht aus derselben Ganglinie - Jahresmittel und Jahresminimum.
            Schicht_Kennzahlen();
        }

        // ------------------------------------------------------------------
        // Kanal, Rolle und Ladefähigkeit (Paket 4, Etappe 4b - Konzept 3.2/3.4)
        // ------------------------------------------------------------------

        /// <summary>true = QUELLspeicher (Rolle), sonst Senkenspeicher.</summary>
        public bool IstQuelle
        {
            get { return Verwendung == VERWENDUNG_QUELLE; }
        }

        /// <summary>
        /// true = der Speicher bedient den BRAUCHWASSERkanal (Konzept 3.2).
        ///
        /// Alles andere — auch eine leere Verwendung aus dem früheren impliziten
        /// <c>CopyFromStamm</c> — zählt als Heizungskanal. Das ist dieselbe Regel wie in
        /// <c>WaermesenkeClass.WirksameVerwendung</c>; einen namenlosen Kanal gibt es
        /// nicht.
        ///
        /// <b>KEIN KANALTEST MEHR</b> (Paket K2): Die Frage „bedient dieser Speicher
        /// Kanal x?" beantwortet <see cref="BedientKanal(int)"/> aus dem Klassen-Set.
        /// Diese Eigenschaft liest allein die ANZEIGE-/Ergebnisrolle
        /// <see cref="Verwendung"/>.
        /// </summary>
        public bool IstBrauchwasserkanal
        {
            get { return Verwendung == VERWENDUNG_BRAUCHWASSER; }
        }

        // ------------------------------------------------------------------
        // KLASSEN-SET (Konzept 6.1, Entscheidung L6/F5-Alternative — Paket K2)
        //
        // Die eine Verwendungs-Zeichenkette wird durch DREI unabhängige Ja/Nein-Flags
        // abgelöst: Ein Speicher kann Heizung, Brauchwasser und Prozesswärme in
        // beliebiger Kombination bedienen. „Kombi" ist damit nur noch der ANZEIGENAME
        // des Sets {Heizung, Brauchwasser}, kein eigener Kanalbegriff mehr.
        //
        // Verwendung BLEIBT — als Anzeige- und Ergebnisrolle (RolleAnzeige, Schluessel,
        // Vollzyklen, Tab_ErgebnisPufferspeicher.Verwendung) und als Rückfallebene,
        // solange kein Set gesetzt ist (siehe BedientKanal).
        // ------------------------------------------------------------------

        /// <summary>
        /// KLASSEN-SET des Speichers (Konzept 6.1): <c>NutztKanal[<see cref="Kanal.HEIZUNG"/>]</c>
        /// usw. Gefüllt wird es beim Registry-Aufbau aus den Spalten
        /// <c>Tab_Pufferspeicher.Nutzung_Heizung/_Brauchwasser/_Prozess</c> (Schritt 49).
        ///
        /// EIN LEERES SET IST KEINE AUSSAGE, sondern „nicht gesetzt" — dann gilt die
        /// Ableitung aus <see cref="Verwendung"/> (siehe <see cref="BedientKanal(int)"/>).
        /// Das ist bewusst so gebaut: Es gibt Speicherobjekte, die ohne Registry-Aufbau
        /// entstehen (Quellspeicher in <c>WaermequelleClass</c>, Hilfsobjekte der
        /// Verbund-Kapazität, Selbsttests). Ohne diesen Rückfall wären sie stumm
        /// kanallos, und ein Speicher ohne Kanal wird von niemandem entladen.
        /// </summary>
        public bool[] NutztKanal = new bool[Kanal.ANZAHL];

        /// <summary>true, wenn mindestens ein Flag des Klassen-Sets gesetzt ist.</summary>
        public bool KlassenSetGesetzt
        {
            get
            {
                if (NutztKanal == null) return false;
                for (int k = 0; k < NutztKanal.Length; k++) if (NutztKanal[k]) return true;
                return false;
            }
        }

        /// <summary>
        /// Setzt das Klassen-Set (Konzept 6.1). Ein Quellspeicher behält das LEERE Set —
        /// seine Rolle ist Wärmequelle, er bedient keinen Bedarfskanal.
        /// </summary>
        public void KlassenSetSetzen(bool heizung, bool brauchwasser, bool prozess)
        {
            if (NutztKanal == null || NutztKanal.Length != Kanal.ANZAHL)
                NutztKanal = new bool[Kanal.ANZAHL];

            NutztKanal[Kanal.HEIZUNG] = heizung;
            NutztKanal[Kanal.BRAUCHWASSER] = brauchwasser;
            NutztKanal[Kanal.PROZESS] = prozess;
        }

        /// <summary>
        /// Ableitung des Klassen-Sets aus <see cref="Verwendung"/> — die Rückfallebene
        /// für jedes Speicherobjekt ohne gepflegte Flags (Migration nicht gelaufen,
        /// Objekt außerhalb des Registry-Aufbaus gebaut).
        ///
        /// <code>
        /// Kombi         -> {Heizung, Brauchwasser}
        /// Brauchwasser  -> {Brauchwasser}
        /// alles Übrige  -> {Heizung}          (auch eine LEERE Verwendung)
        /// </code>
        ///
        /// Das ist Wort für Wort die bisherige Regel aus <see cref="IstBrauchwasserkanal"/>
        /// und der alten <c>BedientKanal(bool)</c>-Fassung — deshalb rechnet jedes
        /// Bestandsprojekt mit ihr unverändert.
        /// </summary>
        private bool VerwendungBedient(int kanal)
        {
            if (Verwendung == VERWENDUNG_KOMBI)
                return kanal == Kanal.HEIZUNG || kanal == Kanal.BRAUCHWASSER;
            if (Verwendung == VERWENDUNG_BRAUCHWASSER)
                return kanal == Kanal.BRAUCHWASSER;
            return kanal == Kanal.HEIZUNG;
        }

        /// <summary>
        /// true = KOMBISPEICHER: bedient Heizung UND Brauchwasser aus einem Vorrat
        /// (Etappe D5a; seit Paket K2 aus dem Klassen-Set abgeleitet statt aus dem
        /// Persistenzwert „Kombi").
        ///
        /// <see cref="IstBrauchwasserkanal"/> bleibt für ihn <c>false</c> — er ist kein
        /// reiner Warmwasserspeicher; die Kanalfrage beantwortet
        /// <see cref="BedientKanal(int)"/>.
        ///
        /// Ein Set mit Prozesswärme ändert an dieser Frage nichts: {H, B, P} IST ein
        /// Kombispeicher, {H, P} ist keiner. Gefragt ist genau die Konstellation, für die
        /// die Oberfläche und die Ergebnisrolle den Namen „Kombi" führen.
        /// </summary>
        public bool IstKombi
        {
            get { return BedientKanal(Kanal.HEIZUNG) && BedientKanal(Kanal.BRAUCHWASSER); }
        }

        /// <summary>
        /// Bedient dieser Speicher den angefragten KANAL (Konzept 6.1)? Quellspeicher
        /// bedienen keinen.
        ///
        /// Gelesen wird das Klassen-Set <see cref="NutztKanal"/>; ist keines gesetzt,
        /// gilt die Ableitung aus <see cref="Verwendung"/> (siehe
        /// <see cref="VerwendungBedient"/>).
        ///
        /// <b>ACHTUNG — das ist das PERSISTIERTE Set, nicht die Entladeordnung.</b> Die
        /// Interimsregel I2 (Paket K2: ein Speicher mit Heizung im Set bedient
        /// übergangsweise auch den Prozesskanal) wirkt AUSSCHLIESSLICH beim Aufbau der
        /// Entladelisten und im Durchsatzbudget — sie steht in
        /// <c>Kaskadenschleife.EntladetKanal</c> und wird mit Paket S1 abgerissen. Am
        /// Speicher selbst darf sie nicht stehen: Sonst wäre nicht mehr unterscheidbar,
        /// welche Kanäle der Anwender eingestellt hat und welche eine Übergangsregel
        /// hinzuerfindet.
        /// </summary>
        public bool BedientKanal(int kanal)
        {
            if (IstQuelle) return false;
            if (kanal < 0 || kanal >= Kanal.ANZAHL) return false;

            if (!KlassenSetGesetzt) return VerwendungBedient(kanal);
            return NutztKanal[kanal];
        }

        /// <summary>
        /// DÜNNE BRÜCKE der zweikanaligen Fassung (Paket K2) — für Aufrufer, die noch in
        /// Heiz-/Warmwasser-Begriffen denken. Neuer Code benutzt
        /// <see cref="BedientKanal(int)"/>; der Prozesskanal ist über diese Fassung
        /// bewusst NICHT erreichbar.
        /// </summary>
        /// <param name="brauchwasser">true = Brauchwasserkanal, false = Heizkanal</param>
        public bool BedientKanal(bool brauchwasser)
        {
            return BedientKanal(brauchwasser ? Kanal.BRAUCHWASSER : Kanal.HEIZUNG);
        }

        /// <summary>
        /// Klassen-Set als Text für Protokoll- und Hinweiszeilen, z. B.
        /// „Heizung + Prozesswaerme"; leeres Set → „—".
        /// </summary>
        public string KlassenSetText()
        {
            string s = "";
            for (int k = 0; k < Kanal.ANZAHL; k++)
                if (BedientKanal(k)) s += (s.Length > 0 ? " + " : "") + Kanal.Name(k);
            return s.Length > 0 ? s : "—";
        }

        /// <summary>
        /// Ladefähigkeit [kWh] gegen eine Obergrenze nach Konzept 3.4:
        /// <c>Q_max · Obergrenze − SOC − <see cref="Reserviert"/></c>, nie negativ.
        ///
        /// Der reservierte Betrag (Befund N3) ist bereits vergeben: Ein Erzeuger, der
        /// seine Produktion in Phase B gegen diesen Raum entschieden hat, muss ihn in
        /// den Ladephasen noch vorfinden. Ohne Reservierung — jede Stunde ohne BHKW in
        /// Phase B — ist das Feld 0 und der Ausdruck bitgleich der bisherige.
        /// </summary>
        /// <param name="obergrenzeAnteil">
        /// Obergrenze als ANTEIL der nutzbaren Kapazität (0…1), bereits aufgelöst
        /// (<c>Ladeordnung.ObergrenzenAufloesen</c>). Werte ≤ 0 gelten als „nicht
        /// gesetzt" und fallen auf <see cref="SchwelleAus"/> zurück.
        /// </param>
        public double Ladefaehigkeit(double obergrenzeAnteil)
        {
            if (Q_max <= 0) return 0;

            double grenze = obergrenzeAnteil > 0 ? obergrenzeAnteil : SchwelleAus;
            double frei = Q_max * grenze - SOC - Reserviert;
            return frei > 0 ? frei : 0;
        }

        /// <summary>
        /// Höchste Entnahme [kWh] in EINER Stunde, 0 = unbegrenzt — Spalte
        /// <c>Tab_Pufferspeicher.Entladeleistung_Max</c> (Paket P1,
        /// Migrationsschritt 53).
        ///
        /// <para>Fachlich seit Paket 4 vorgemerkt (Nutzerentscheidung zu 4b-1: ein
        /// 800-l-Puffer mit DN 25 kann keine 200 kW durchreichen); bis P1 gab es weder
        /// Datenmodell noch Oberfläche dafür. Die Vorbelegung 0 ist die bisherige
        /// Annahme des Modells („keine Begrenzung der Be-/Entladeleistung", siehe
        /// Kopfkommentar) — der Parameter ist damit verhaltensneutral.</para>
        ///
        /// <para><b>Sie wirkt als BUDGET DER STUNDE</b>, siehe
        /// <see cref="StundeBeginnen"/> (Befund K2-O6).</para>
        /// </summary>
        public double EntladeleistungMax = 0;   // 0 = unbegrenzt

        /// <summary>
        /// Höchste Aufnahme [kWh] in EINER Stunde, 0 = unbegrenzt — Spalte
        /// <c>Tab_Pufferspeicher.Ladeleistung_Max</c>; das Gegenstück zu
        /// <see cref="EntladeleistungMax"/> und mit derselben Budget-Mechanik.
        /// </summary>
        public double LadeleistungMax = 0;      // 0 = unbegrenzt

        /// <summary>
        /// Entnahmefähigkeit einer Stunde [kWh] — der noch VERFÜGBARE Rest des
        /// Stundenbudgets, unbegrenzt solange kein Wert gepflegt ist.
        ///
        /// <para>Bis Paket P1 lieferte die Methode die Grenze selbst. Der Unterschied ist
        /// der Zwei-Pass-Durchlauf (Befund K2-O6): Ein Speicher, der Heizung UND
        /// Prozesswärme bedient, wird in derselben Stunde zweimal befragt — mit der
        /// Grenze statt des Rests hätte er sie zweimal bekommen.</para>
        /// </summary>
        public double Entnahmefaehigkeit()
        {
            if (EntladeleistungMax <= 0) return double.MaxValue;
            return _entladebudget > 0 ? _entladebudget : 0;
        }

        /// <summary>
        /// Höchste bedarfsdeckende ENTNAHME, die dieser Speicher jetzt noch zulässt [kWh] —
        /// die eine Stelle, an der der Mindestfüllstand aus
        /// <see cref="SchwelleReserve"/> wirkt (Paket BHKW-Regulär).
        ///
        /// <code>
        /// ohne BHKW-Bezug oder ohne Reserve : double.MaxValue   (bisheriges Verhalten)
        /// sonst                             : max(0, SOC − Q_max · SchwelleReserve)
        /// </code>
        ///
        /// <para><b>Bewusst NICHT in <see cref="Entladen"/> eingebaut.</b> Diese Methode ist
        /// die Speicherphysik aller vier Erzeugerarten und aller Phasen; eine Untergrenze
        /// dort wäre eine globale Verhaltensänderung — genau das, was die Entscheidung des
        /// Anwenders ausschließt. Die Grenze wird deshalb von der
        /// <c>Kaskadenschleife</c> vor der Entladung auf den ANGEFORDERTEN Bedarf gelegt
        /// (Phasen A und E). <see cref="Entladen"/> selbst bleibt Zeile für Zeile
        /// unverändert und kennt weiterhin nur die Grenze „Speicher leer".</para>
        ///
        /// <para><b>Der Bezug ist Q_max, nicht SOC.</b> Die Reserve ist ein Anteil der
        /// NUTZBAREN KAPAZITÄT — dieselbe Bezugsgröße wie bei Ein- und Abschaltschwelle.
        /// Ein Anteil des momentanen Füllstands wäre keine feste Marke, sondern eine, die
        /// mit dem Leerlaufen mitwandert und nie erreicht würde.</para>
        ///
        /// <para><b>Der DURCHSATZ bleibt unangetastet.</b> Steht der Füllstand über
        /// <c>Q_max</c> (Durchleitung derselben Stunde, Befund N6), ist der Überhang
        /// vollständig entnehmbar: <c>SOC − Q_max · Reserve</c> ist dann größer als der
        /// Überhang. Die Reserve begrenzt also den VORRAT, nicht die hydraulische Weiche —
        /// und deshalb war auf der Ladeseite (Bilanzraum, Ladefähigkeit) nichts zu
        /// ändern.</para>
        /// </summary>
        public double EntnahmeObergrenze()
        {
            if (!BhkwReserveGilt || SchwelleReserve <= 0 || Q_max <= 0) return double.MaxValue;

            double ueberReserve = SOC - Q_max * SchwelleReserve;
            return ueberReserve > 0 ? ueberReserve : 0;
        }

        /// <summary>
        /// BILANZRAUM einer Stunde [kWh] — wie viel Wärme der Speicher in diesem
        /// Zeitschritt insgesamt aufnehmen kann (Paket 4, Nutzerentscheidung zu Befund
        /// 4b-1 vom 14.08.2026):
        ///
        /// <code>
        /// Bilanzraum = (Q_max · Obergrenze − SOC)                       [SOC-Zielwert, 3.4]
        ///            + min(offener Kanalbedarf, Entnahmefähigkeit)      [Durchsatz]
        /// </code>
        ///
        /// Der erste Summand ist die Ladefähigkeit aus Konzept 3.4 — der ZIELFÜLLSTAND
        /// samt Reservezone. Der zweite ist der DURCHSATZ: Ein Pufferspeicher ist eine
        /// hydraulische Weiche und drosselt die Anlage nicht auf seinen Inhalt; was im
        /// selben Zeitschritt wieder entnommen wird, kann er zusätzlich aufnehmen.
        /// Beide Größen sind bewusst getrennt — der Zielfüllstand steuert, WIE VOLL der
        /// Speicher wird, der Durchsatz, WIE VIEL durch ihn hindurchgeht.
        ///
        /// Ohne offenen Kanalbedarf ist der Bilanzraum genau die Ladefähigkeit.
        /// </summary>
        /// <param name="obergrenzeAnteil">Obergrenze als Anteil (0…1), siehe <see cref="Ladefaehigkeit"/>.</param>
        /// <param name="offenerKanalbedarf">
        /// Noch offener Bedarf des Kanals, den DIESER Speicher bedient [kWh]. Er wird vom
        /// Aufrufer über alle Ladevorgänge einer Stunde hinweg nur EINMAL vergeben —
        /// sonst reichten zwei Speicher desselben Kanals dieselbe Entnahme doppelt durch.
        /// </param>
        public double Bilanzraum(double obergrenzeAnteil, double offenerKanalbedarf)
        {
            double lade = Ladefaehigkeit(obergrenzeAnteil);
            if (offenerKanalbedarf <= 0) return lade;

            return lade + Math.Min(offenerKanalbedarf, Entnahmefaehigkeit());
        }

        /// <summary>
        /// Schreibt die Hysterese der Speicherregelung fort (Konzept 6.2,
        /// <see cref="LaedtGerade"/>) und liefert zurück, ob der Speicher in dieser
        /// Stunde ENTLADEN darf.
        ///
        /// Dieselbe Regel wie in der frueheren einkanaligen Fassung, nur nicht mehr modulübergreifend,
        /// sondern am Speicher: Unter der Einschaltschwelle beginnt die Nachladung, ab der
        /// Abschaltschwelle endet sie. Solange nachgeladen wird, deckt der Speicher keinen
        /// Bedarf vorab (Phase A) — die Nachentladung (Phase E) greift davon unabhängig,
        /// genau wie heute die Entladung vor Heizstab und Folge-Erzeuger.
        /// </summary>
        public bool HystereseFortschreiben()
        {
            if (Q_max <= 0) return false;

            if (!LaedtGerade && SOC <= Q_max * SchwelleEin) LaedtGerade = true;
            if (LaedtGerade && SOC >= Q_max * SchwelleAus) LaedtGerade = false;

            return !LaedtGerade;
        }

        /// <summary>Anzeigetext der Rolle (lokalisiert seit Paket 9 / L6).</summary>
        public string RolleAnzeige()
        {
            return (Verwendung == VERWENDUNG_QUELLE)
                ? MyResource.Resource.PSP_ROLLE_QUELLSPEICHER
                : MyResource.Resource.PSP_ROLLE_SENKENSPEICHER;
        }

        /// <summary>
        /// Bezeichner für Anzeigen und Exportköpfe, mit dem einen Ersatztext für
        /// namenlose Speicher. Bewusst an EINER Stelle: der Text stand vorher in
        /// NavigatorWaerme, Form_Simulation_Detail (Tabelle) und dessen CSV-Export
        /// je einmal - drei Kopien, die auseinanderlaufen konnten.
        /// </summary>
        public string BezeichnerAnzeige()
        {
            return string.IsNullOrEmpty(Bezeichner)
                ? MyResource.Resource.PSP_BEZEICHNER_ERSATZ
                : Bezeichner;
        }

        /// <summary>Anzeigetext "Bezeichner (Rolle)" für Legende, Auswahlliste und CSV-Kopf.</summary>
        public string Anzeige()
        {
            return BezeichnerAnzeige() + " (" + RolleAnzeige() + ")";
        }

        /// <summary>
        /// Technischer Schlüssel für Chart-Serien und Exportspalten (Konzept 13.3):
        /// PUFFER_&lt;ID&gt; für Senkenspeicher, QUELLE_&lt;AnlagenID&gt; für Quellspeicher.
        /// Der Anzeigetext gehört ausschließlich in LegendText bzw. den Spaltenkopf -
        /// sonst kollidiert die Umstellung mit der Lokalisierung (Paket 9).
        /// </summary>
        public string Schluessel(int index)
        {
            if (Verwendung == VERWENDUNG_QUELLE)
                return "QUELLE_" + ((ID_Anlage > 0) ? ID_Anlage : index);
            return "PUFFER_" + ((ID_Pufferspeicher > 0) ? ID_Pufferspeicher : index);
        }

        // ==================================================================
        // SCHICHTSPEICHERMODELL (Paket P1, Konzept § 7)
        //
        // MEHRZONEN-MODELL mit idealer Einschichtung und vertikalem Ausgleich:
        // N übereinanderliegende Schichten gleichen Volumens, oben Schicht 0.
        //
        // DIE ARCHITEKTURENTSCHEIDUNG, aus der die Byte-Zusage für N = 1 folgt
        // (Konzept 7.3): SOC bleibt die FÜHRENDE Zustandsgröße. Die gesamte
        // Energiearithmetik des Bestands - Laden mit Durchlass, Zerlegung
        // Umsatz/Durchfluss, Reservierung, Bilanzraum, Hysterese, Schwellen,
        // Bereitschaftsverluste, Vollzyklen - steht Wort für Wort unverändert. Die
        // Schichtebene ist eine ZWEITE Zustandsebene, die ausschließlich den
        // SPEICHERINHALT A = min(SOC, Q_max) auf N Temperaturen verteilt:
        //
        //     Σ_i E[i]  ==  min(SOC, Q_max)          [Schicht-Invariante]
        //     E[i] ∈ [0, Q_max/N]   ⇔   T[i] ∈ [RL_eff, VL_eff]
        //
        // Der ÜBERHANG B = max(0, SOC − Q_max) - der Durchfluss derselben Stunde -
        // wird bewusst NICHT in Schichten geführt (Konzept 7.3): Er ist hydraulisch
        // durchströmende Wärme, folgt weiter der N6-Buchung und zählt für die
        // Entnahmefähigkeit stets als nutzbar. Eine bei VL_eff gekappte
        // Temperatursumme kann nie über Q_max liegen; ohne diese Trennung wäre der
        // Durchlass-Mechanismus im Schichtmodell nicht darstellbar.
        //
        // WO N = 1 BYTE-GLEICH BLEIBT - konstruktiv, nicht über Sonderzweige:
        //   * Laden/Entladen/Verluste buchen ZUERST den SOC; die Schichtaufrufe
        //     stehen dahinter und rechnen nur auf E[].
        //   * Bei N = 1 ist E[0] == A, jede Schichtoperation ist die identische
        //     Buchung auf demselben Betrag, und T[0] ist die reine Umrechnung
        //     T = RL_eff + A/Q_max · (VL_eff − RL_eff) (Konzept 8.2).
        //   * Wärmeleitung und Inversionsmischung brauchen ein Schichtpaar - bei
        //     N = 1 gibt es keines.
        //   * Die ENTLADEFÄHIGKEIT (T_Nutz-Bedingung, Entnahmehöhe) liefert bei
        //     N = 1 double.MaxValue und klemmt damit nichts. Das ist die eine
        //     Stelle, an der die Schichtebene in den Energiepfad zurückwirken
        //     könnte - sie ist an N gebunden, nicht an eine Toleranz.
        //   * Die Leistungsgrenzen sind mit ihrer Vorbelegung 0 unbegrenzt und
        //     betreten ihren Zweig nicht.
        // ==================================================================

        /// <summary>Höchste zulässige Schichtzahl (Konzept 7.2: N = 1…10).</summary>
        public const int SCHICHTEN_MAX = 10;

        /// <summary>
        /// Effektive vertikale Wärmeleitfähigkeit ohne gepflegten Wert [W/(m·K)] —
        /// Konzept 7.2. Sie enthält die Wandleitung und ist deshalb deutlich größer als
        /// die des ruhenden Wassers (0,6).
        /// </summary>
        public const double LAMBDA_EFF_DEFAULT = 1.5;

        /// <summary>
        /// Höhen-Durchmesser-Verhältnis ohne gepflegte Höhe (Konzept 7.2) — der
        /// Formfaktor eines stehenden Pufferspeichers.
        /// </summary>
        public const double HD_VERHAELTNIS_DEFAULT = 2.5;

        /// <summary>
        /// Kappung des vertikalen Ausgleichs je Schichtpaar und Stunde (Konzept 7.4
        /// Punkt 3): höchstens 25 % der Temperaturdifferenz. Damit ist der Schritt
        /// unbedingt stabil und monoton — die Reihenfolge zweier Schichten kann nicht
        /// kippen —, und es braucht keine Unterschritte.
        /// </summary>
        public const double AUSGLEICH_KAPPUNG = 0.25;

        /// <summary>
        /// Gepflegte Schichtzahl aus <c>Tab_Pufferspeicher.Schichten_Anzahl</c>
        /// (Migrationsschritt 53), Vorbelegung 1 = das Ein-Zonen-Modell des Bestands.
        /// Wirksam wird sie über <see cref="SchichtenWirksam"/>.
        /// </summary>
        public int SchichtenAnzahl = 1;

        /// <summary>
        /// Behälterhöhe [m] aus <c>Tab_Pufferspeicher.Hoehe</c>; 0 = nicht gepflegt,
        /// dann wird sie aus dem Volumen über <see cref="HD_VERHAELTNIS_DEFAULT"/>
        /// abgeleitet (Konzept 7.2).
        /// </summary>
        public double Hoehe = 0;

        /// <summary>
        /// Effektive vertikale Wärmeleitfähigkeit [W/(m·K)] aus
        /// <c>Tab_Pufferspeicher.Lambda_Eff</c>; Vorbelegung
        /// <see cref="LAMBDA_EFF_DEFAULT"/>.
        /// </summary>
        public double LambdaEff = LAMBDA_EFF_DEFAULT;

        /// <summary>Gesamtvolumen [l], das <see cref="Init"/> bekommen hat — Bezugsgröße der Geometrie.</summary>
        public double VolumenLiter = 0;

        /// <summary>
        /// Wirksame Vorlauftemperatur [°C] (Konzept 7.2): das gepflegte Paar, sonst
        /// <c>RL_eff + RueckfallDeltaT</c>. Es ist DASSELBE ΔT, das in
        /// <see cref="Q_max"/> steht — die Rückfallregel wird nicht zweimal ausgelegt.
        /// </summary>
        public double VL_eff = 0;

        /// <summary>Wirksame Rücklauftemperatur [°C] — die Nullmarke der Schichtenergie.</summary>
        public double RL_eff = 0;

        /// <summary>
        /// Mindest-Nutztemperatur je Kanal [°C] (Konzept 7.2/7.4). Vorbelegung
        /// <see cref="RL_eff"/> und damit verhaltensneutral: Unterhalb des Rücklaufs
        /// trägt keine Schicht Energie, die Bedingung ist für jede Schicht mit Inhalt
        /// erfüllt. Gepflegt wird heute nur der Brauchwasserkanal
        /// (<c>T_Nutz_BW</c>, Entscheidung F7).
        /// </summary>
        public readonly double[] TNutz = new double[Kanal.ANZAHL];

        /// <summary>
        /// ENTNAHMEHÖHE je Kanal, 0…1 (1 = ganz oben). Sie entscheidet, welche Schichten
        /// ein Kanal überhaupt erreicht (Konzept 7.4 Punkt 2) — und damit beim
        /// Kombispeicher, dass die Heizung die Brauchwasser-Bereitschaftszone oben nicht
        /// antastet (7.5). Vorbelegung oben; der Registry-Aufbau setzt die
        /// Konzept-Vorgaben aus dem Klassen-Set.
        /// </summary>
        public readonly double[] Entnahmehoehe = new double[Kanal.ANZAHL]
            { 1.0, 1.0, 1.0 };

        /// <summary>
        /// EINSPEISEHÖHE des gerade laufenden Ladevorgangs, 0…1 (Konzept 7.4 Punkt 1),
        /// aus <c>Z_AnlageSenke.Anschlusshoehe</c> der ladenden Senke. Die
        /// Kaskadenschleife setzt sie unmittelbar vor dem Ladeaufruf des Moduls und
        /// nimmt sie danach zurück; ohne gepflegte Höhe (NULL/−1) gilt oben.
        ///
        /// <para><b>Warum am Speicher und nicht als Parameter von
        /// <see cref="Laden(double,int,double)"/>:</b> Die vier Erzeugermodule buchen
        /// ihre Ladung selbst und tief in ihrer eigenen Mengenrechnung. Ein zusätzlicher
        /// Parameter hätte vier Modulsignaturen und ein Dutzend Aufrufstellen berührt,
        /// ohne dass eines der Module die Höhe je auswertet — sie gehört zur HYDRAULIK
        /// des Behälters, nicht zum Erzeuger.</para>
        /// </summary>
        public double EinspeisehoeheAktuell = 1.0;

        /// <summary>Stundenganglinie der obersten Schicht [°C]; 0, wo keine Schichtrechnung läuft.</summary>
        public float[] T_oben_stuendlich = new float[8760];

        /// <summary>Stundenganglinie der untersten Schicht [°C]; siehe <see cref="T_oben_stuendlich"/>.</summary>
        public float[] T_unten_stuendlich = new float[8760];

        /// <summary>
        /// Jahresmittel der obersten Schichttemperatur [°C]; <c>null</c> = nicht erhoben
        /// (Quellspeicher, Speicher ohne Kapazität) — Spalte
        /// <c>Tab_ErgebnisPufferspeicher.T_oben_Mittel</c> aus Schritt 52.
        /// </summary>
        public double? T_oben_Mittel;

        /// <summary>Jahresminimum der obersten Schichttemperatur [°C]; siehe <see cref="T_oben_Mittel"/>.</summary>
        public double? T_oben_Min;

        /// <summary>
        /// Zahl der Stunden, in denen die Schicht-Invariante <c>Σ E[i] == min(SOC,
        /// Q_max)</c> über die Toleranz hinaus verletzt war (Konzept 11.3). Reine
        /// Instrumentierung — sie geht in kein Ergebnis ein; nach einem gesunden Lauf
        /// ist sie 0.
        /// </summary>
        public int SchichtInvarianteVerletzungen = 0;

        /// <summary>Größte gemessene Abweichung der Schicht-Invariante [kWh].</summary>
        public double SchichtInvarianteMaxAbweichung = 0;

        // --- Laufzeitzustand der Schichtebene -----------------------------------------

        /// <summary>Energie je Schicht [kWh], Index 0 = oben; <c>null</c> vor dem Aufbau.</summary>
        private double[] _schicht;

        /// <summary>Energie einer VOLLEN Schicht [kWh] = <c>Q_max / N</c>.</summary>
        private double _schichtMax;

        /// <summary>Wärmekapazität einer Schicht [kWh/K] — <c>_schichtMax / (VL_eff − RL_eff)</c>.</summary>
        private double _schichtKapazitaet;

        /// <summary>Wärmedurchgang je Schichtpaar und Stunde [kWh/K] (Konzept 7.4 Punkt 3).</summary>
        private double _leitwert;

        /// <summary>Wärmeabgabefläche je Schicht [m²] — Mantel, oben/unten zusätzlich der Deckel.</summary>
        private double[] _schichtFlaeche;

        /// <summary>Arbeitsfelder der Inversionsmischung — vorab angelegt, nicht je Stunde.</summary>
        private double[] _blockWert;
        private int[] _blockLaenge;

        /// <summary>Stunde, für die das Leistungsbudget gilt; −1 = noch nicht gesetzt.</summary>
        private int _budgetStunde = -1;

        /// <summary>Verbleibende Aufnahme der Stunde [kWh]; <c>double.MaxValue</c> = unbegrenzt.</summary>
        private double _ladebudget = double.MaxValue;

        /// <summary>Verbleibende Abgabe der Stunde [kWh]; <c>double.MaxValue</c> = unbegrenzt.</summary>
        private double _entladebudget = double.MaxValue;

        /// <summary>
        /// WIRKSAME Schichtzahl des Laufs — die Zahl, mit der wirklich gerechnet wird.
        ///
        /// <list type="bullet">
        /// <item>QUELLSPEICHER bleiben Ein-Zonen-Modelle (Konzept 7.6): Ihre Kapazität
        /// folgt der Anlagen-Spreizung <c>WQ_Spreizung</c>, ihr Temperaturpaar
        /// (Spreizung/0) sind keine Speichertemperaturen — eine Schichtebene darauf wäre
        /// Scheinphysik.</item>
        /// <item>Ohne Kapazität oder ohne Spreizung gibt es keine Temperaturachse.</item>
        /// <item>Gekappt auf <see cref="SCHICHTEN_MAX"/>.</item>
        /// </list>
        ///
        /// Der VERBUND-Leitspeicher wird nicht hier, sondern beim Registry-Aufbau auf 1
        /// gezwungen (Konzept 6.3): Dort steht die Information, dass sein
        /// <see cref="Q_max"/> die Summe mehrerer Behälter ist — eine aus SEINEM Volumen
        /// abgeleitete Schichtebene wäre falsch.
        /// </summary>
        public int SchichtenWirksam
        {
            get
            {
                if (IstQuelle) return 1;
                if (Q_max <= 0 || VL_eff <= RL_eff) return 1;
                if (SchichtenAnzahl < 1) return 1;
                return SchichtenAnzahl > SCHICHTEN_MAX ? SCHICHTEN_MAX : SchichtenAnzahl;
            }
        }

        /// <summary>true = der Speicher rechnet mit mehr als einer Schicht.</summary>
        public bool Geschichtet
        {
            get { return SchichtenWirksam > 1; }
        }

        /// <summary>
        /// Legt die Schichtebene aus den aktuellen Parametern neu an: N Schichten auf
        /// <see cref="RL_eff"/> (der leere Speicher) und die daraus abgeleiteten
        /// Geometrie- und Leitungsgrößen.
        ///
        /// <para>Aufgerufen von <see cref="Reset"/> (also auch aus <see cref="Init"/>)
        /// und vom Registry-Aufbau, nachdem er Schichtzahl, Höhe, λ und die
        /// Verbund-Kapazität gesetzt hat. Mehrfachaufruf ist unschädlich.</para>
        /// </summary>
        public void SchichtenAufbauen()
        {
            int n = SchichtenWirksam;

            if (_schicht == null || _schicht.Length != n) _schicht = new double[n];
            else Array.Clear(_schicht, 0, n);

            if (_blockWert == null || _blockWert.Length != n)
            {
                _blockWert = new double[n];
                _blockLaenge = new int[n];
            }

            _schichtMax = (Q_max > 0) ? Q_max / n : 0;

            double spreizung = VL_eff - RL_eff;
            _schichtKapazitaet = (spreizung > 0) ? _schichtMax / spreizung : 0;

            // GEOMETRIE: Höhe entweder gepflegt oder aus dem Volumen über H/D = 2,5.
            //   V = π/4 · D² · H  und  H = 2,5 · D   ⇒   D = (4V / (π · 2,5))^(1/3)
            double vM3 = VolumenLiter / 1000.0;
            double hoehe = Hoehe > 0 ? Hoehe : 0;
            if (hoehe <= 0 && vM3 > 0)
            {
                double d = Math.Pow(4.0 * vM3 / (Math.PI * HD_VERHAELTNIS_DEFAULT), 1.0 / 3.0);
                hoehe = HD_VERHAELTNIS_DEFAULT * d;
            }

            double quer = (hoehe > 0) ? vM3 / hoehe : 0;

            // WÄRMEDURCHGANG je Schichtpaar: k = λ_eff · A_quer / (H/N) [W/K], mal 1 h
            // und durch 1000 ⇒ kWh/K. Bei N = 1 gibt es kein Paar, der Wert bleibt 0.
            double lambda = LambdaEff > 0 ? LambdaEff : LAMBDA_EFF_DEFAULT;
            _leitwert = (n > 1 && hoehe > 0 && quer > 0)
                ? lambda * quer * n / hoehe / 1000.0
                : 0;

            // WÄRMEABGABEFLÄCHE je Schicht: Mantelabschnitt π·D·(H/N); die oberste und
            // die unterste Schicht tragen zusätzlich den jeweiligen Deckel. Ohne
            // Geometrie (Volumen 0) sind alle Schichten gleich gewichtet - dann steht die
            // Verteilung allein auf der Temperaturgewichtung.
            if (_schichtFlaeche == null || _schichtFlaeche.Length != n)
                _schichtFlaeche = new double[n];

            if (quer > 0 && hoehe > 0)
            {
                double durchmesser = Math.Sqrt(4.0 * quer / Math.PI);
                double mantel = Math.PI * durchmesser * (hoehe / n);
                for (int i = 0; i < n; i++) _schichtFlaeche[i] = mantel;
                _schichtFlaeche[0] += quer;
                _schichtFlaeche[n - 1] += quer;
            }
            else
            {
                for (int i = 0; i < n; i++) _schichtFlaeche[i] = 1;
            }
        }

        /// <summary>
        /// Temperatur der Schicht <paramref name="i"/> [°C] (0 = oben):
        /// <c>RL_eff + E[i]/E_max · (VL_eff − RL_eff)</c>.
        ///
        /// <para>Bei N = 1 ist das die Ein-Zonen-Ersatztemperatur aus Konzept 8.2 —
        /// dieselbe Formel, mit der die Booster-Wärmepumpe später ihre Quelltemperatur
        /// bildet.</para>
        /// </summary>
        public double SchichtTemperatur(int i)
        {
            if (_schicht == null || i < 0 || i >= _schicht.Length) return RL_eff;
            if (_schichtMax <= 0) return RL_eff;

            double f = _schicht[i] / _schichtMax;
            if (f < 0) f = 0;
            else if (f > 1) f = 1;
            return RL_eff + f * (VL_eff - RL_eff);
        }

        /// <summary>Energie der Schicht <paramref name="i"/> [kWh] — für Anzeigen und Proben.</summary>
        public double SchichtEnergie(int i)
        {
            return (_schicht != null && i >= 0 && i < _schicht.Length) ? _schicht[i] : 0;
        }

        /// <summary>Temperatur der obersten Schicht [°C].</summary>
        public double T_oben { get { return SchichtTemperatur(0); } }

        /// <summary>
        /// QUELLTEMPERATUR an der Quell-Entnahmehöhe [°C] — die eine Größe, mit der ein
        /// Booster (Wärmepumpe oder Heizkessel) aus diesem Speicher bezieht
        /// (Paket B1, Konzept 8.2/8.4).
        ///
        /// <para><b>Bis Paket Q1 fest OBEN.</b> Die Quell-Entnahmehöhe
        /// (<c>WQ_Anschlusshoehe</c>) entsteht erst mit dem Schema-Schritt 54; bis dahin
        /// gilt die Konzept-Vorgabe „Default oben", also <see cref="T_oben"/>. Diese
        /// Eigenschaft ist die EINE Stelle, an der Q1 die Höhe einsetzt — die Aufrufer in
        /// <c>SimulationWaermepumpe</c> und <c>SimulationSPK</c> bleiben dann
        /// unverändert.</para>
        ///
        /// <para>Bei N = 1 liefert <see cref="SchichtTemperatur"/> die
        /// Ein-Zonen-Ersatztemperatur <c>RL_eff + A/Q_max · (VL_eff − RL_eff)</c> aus
        /// Konzept 8.2 — dieselbe Formel, ohne Sonderzweig.</para>
        /// </summary>
        public double QuellEntnahmeTemperatur
        {
            get { return SchichtTemperatur(0); }
        }

        /// <summary>Temperatur der untersten Schicht [°C].</summary>
        public double T_unten
        {
            get { return SchichtTemperatur(_schicht != null ? _schicht.Length - 1 : 0); }
        }

        /// <summary>
        /// Setzt das LEISTUNGSBUDGET der Stunde zurück (Befund K2-O6) — idempotent je
        /// Stunde, damit der Zwei-Pass-Durchlauf eines Puffers (Prozess- und Heizkanal
        /// derselben Stunde) die Grenze nur EINMAL bekommt.
        ///
        /// <para>Aufgerufen wird sie von der Kaskadenschleife zu Beginn jeder Stunde -
        /// an derselben Stelle, an der auch die Reservierungen der Vorstunde verfallen -
        /// und vorsorglich von <see cref="Laden(double,int,double)"/> und
        /// <see cref="Entladen"/> selbst, damit auch ein Speicher außerhalb der
        /// Stundenschleife nie mit einem fremden Budget rechnet.</para>
        /// </summary>
        public void StundeBeginnen(int stunde)
        {
            if (_budgetStunde == stunde) return;

            _budgetStunde = stunde;
            _ladebudget = LadeleistungMax > 0 ? LadeleistungMax : double.MaxValue;
            _entladebudget = EntladeleistungMax > 0 ? EntladeleistungMax : double.MaxValue;
        }

        /// <summary>Budget auf „unbegrenzt" und „keine Stunde gesetzt" (Laufanfang).</summary>
        private void BudgetZuruecksetzen()
        {
            _budgetStunde = -1;
            _ladebudget = double.MaxValue;
            _entladebudget = double.MaxValue;
        }

        /// <summary>
        /// ENTLADEFÄHIGKEIT dieses Speichers für einen Kanal [kWh] (Konzept 7.4
        /// Punkt 2): <c>Durchfluss B + Σ Energie der zugänglichen Schichten mit
        /// T[i] ≥ T_Nutz[kanal]</c>.
        ///
        /// <para><b>Bei N = 1 double.MaxValue</b> — und damit ohne jede Wirkung. Das ist
        /// die eine Stelle, an der die Schichtebene in den Energiepfad zurückwirkt; sie
        /// ist deshalb an die Schichtzahl gebunden, nicht an eine Zahlenprüfung. Ein
        /// Bestandsprojekt (N = 1 nach Migrationsschritt 53) rechnet unverändert.</para>
        ///
        /// <para><b>Zugänglich</b> sind die Schichten von der Entnahmehöhe des Kanals
        /// ABWÄRTS; oberhalb bleibt der Vorrat unangetastet (das ist die
        /// Brauchwasser-Bereitschaftszone des Kombispeichers, 7.5). Der DURCHFLUSS zählt
        /// stets voll mit: Er strömt in derselben Stunde hindurch und hat die
        /// Vorlauftemperatur des Erzeugers.</para>
        /// </summary>
        public double EntladefaehigkeitKanal(int kanal)
        {
            if (!Geschichtet || _schicht == null) return double.MaxValue;
            if (kanal < 0 || kanal >= Kanal.ANZAHL) return double.MaxValue;

            double durchfluss = SOC - Q_max;
            if (durchfluss < 0) durchfluss = 0;

            double grenze = TNutz[kanal];
            double summe = 0;
            for (int i = SchichtIndex(Entnahmehoehe[kanal]); i < _schicht.Length; i++)
            {
                if (_schicht[i] <= 0) continue;
                // 1e-9 als Zahlenrand, nicht als Fachtoleranz: Bei T_Nutz == RL_eff (die
                // verhaltensneutrale Vorbelegung) darf die Bedingung nicht an der
                // letzten Stelle einer Division scheitern.
                if (SchichtTemperatur(i) >= grenze - 1e-9) summe += _schicht[i];
            }

            return durchfluss + summe;
        }

        /// <summary>
        /// Schichtindex zu einer Anschlusshöhe 0…1 (1 = oben ⇒ Index 0, 0 = unten ⇒
        /// Index N−1). Werte außerhalb gelten als oben — dieselbe Auslegung wie NULL in
        /// der Datenbank.
        /// </summary>
        private int SchichtIndex(double hoehe)
        {
            int n = (_schicht != null) ? _schicht.Length : 1;
            if (hoehe >= 1 || hoehe < 0) return 0;

            int idx = (int)Math.Floor((1.0 - hoehe) * n);
            if (idx < 0) idx = 0;
            else if (idx > n - 1) idx = n - 1;
            return idx;
        }

        /// <summary>
        /// BELADUNG der Schichtebene (Konzept 7.4 Punkt 1): Von der Einspeisehöhe
        /// ABWÄRTS werden die Schichten nacheinander auf <see cref="VL_eff"/> gehoben —
        /// das Temperaturband wandert nach unten.
        ///
        /// <para><b>Der Aufstieg als zweiter Durchgang.</b> Ist von der Einspeisehöhe
        /// abwärts alles voll, steigt die weitere Wärme nach OBEN (Auftrieb). Das ist
        /// keine Zutat, sondern eine Notwendigkeit: Der SOC hat die Menge bereits
        /// aufgenommen (er kennt nur die Gesamtkapazität), und ohne diesen Durchgang
        /// bräche die Schicht-Invariante. Bei der Vorgabe „Einspeisung oben" — jedem
        /// Bestandsdatensatz — beginnt die erste Schleife ohnehin bei Index 0 und der
        /// zweite Durchgang läuft leer.</para>
        /// </summary>
        private void Schicht_Beladen(double umsatz)
        {
            if (umsatz <= 0 || _schicht == null || _schichtMax <= 0) return;

            int start = SchichtIndex(EinspeisehoeheAktuell);
            double rest = umsatz;

            for (int i = start; i < _schicht.Length && rest > 0; i++)
                rest -= Fuellen(i, rest);

            for (int i = start - 1; i >= 0 && rest > 0; i--)
                rest -= Fuellen(i, rest);
        }

        /// <summary>Füllt eine Schicht bis <see cref="_schichtMax"/> und meldet die aufgenommene Menge.</summary>
        private double Fuellen(int i, double menge)
        {
            double frei = _schichtMax - _schicht[i];
            if (frei <= 0) return 0;

            double teil = (menge < frei) ? menge : frei;
            _schicht[i] += teil;
            return teil;
        }

        /// <summary>
        /// ENTNAHME der Schichtebene (Konzept 7.4 Punkt 2): ideale Verdrängung am
        /// Anschluss. Zugänglich sind die Schichten von der Entnahmehöhe ABWÄRTS; der
        /// Rücklauf tritt UNTEN ein, deshalb fällt die unterste Schicht zuerst auf
        /// <see cref="RL_eff"/> zurück und die darüber rücken nach.
        ///
        /// <para>Der zweite Durchgang oberhalb der Entnahmehöhe ist ein SICHERHEITSNETZ
        /// für die Invariante, kein Rechenweg: Die Kaskadenschleife klemmt den
        /// angeforderten Bedarf zuvor auf <see cref="EntladefaehigkeitKanal"/>, und die
        /// zugängliche Energie ist nie kleiner als diese. Er greift also nur, wenn ein
        /// Aufrufer diese Klemmung umgeht.</para>
        /// </summary>
        private void Schicht_Entladen(double umsatz, int kanal)
        {
            if (umsatz <= 0 || _schicht == null) return;

            double hoehe = (kanal >= 0 && kanal < Kanal.ANZAHL) ? Entnahmehoehe[kanal] : 1.0;
            int start = SchichtIndex(hoehe);
            double rest = umsatz;

            for (int i = _schicht.Length - 1; i >= start && rest > 0; i--)
                rest -= Leeren(i, rest);

            for (int i = start - 1; i >= 0 && rest > 0; i--)
                rest -= Leeren(i, rest);
        }

        /// <summary>Entleert eine Schicht bis <see cref="RL_eff"/> und meldet die abgegebene Menge.</summary>
        private double Leeren(int i, double menge)
        {
            double da = _schicht[i];
            if (da <= 0) return 0;

            double teil = (menge < da) ? menge : da;
            _schicht[i] -= teil;
            return teil;
        }

        /// <summary>
        /// VERTIKALER AUSGLEICH (Konzept 7.4 Punkt 3): Wärmeleitung zwischen
        /// Nachbarschichten <c>ΔQ = k · (T[i+1] − T[i])</c>, je Paar auf 25 % der
        /// Temperaturdifferenz gekappt, anschließend Inversionsmischung.
        ///
        /// <para>Beides ist ein Austausch INNERHALB des Speichers und lässt die Summe
        /// unberührt. Bei N = 1 gibt es kein Schichtpaar — die Methode kehrt sofort
        /// zurück.</para>
        /// </summary>
        private void Schicht_Ausgleich()
        {
            if (_schicht == null || _schicht.Length < 2) return;

            if (_leitwert > 0 && _schichtKapazitaet > 0)
            {
                for (int i = 0; i < _schicht.Length - 1; i++)
                {
                    // Positives dT heißt: die UNTERE Schicht ist wärmer, Wärme fließt
                    // nach oben.
                    double dT = SchichtTemperatur(i + 1) - SchichtTemperatur(i);
                    if (dT == 0) continue;

                    double q = _leitwert * dT;

                    // Kappung auf 25 % der Temperaturdifferenz: Nach dem Austausch
                    // beträgt die Differenz mindestens die Hälfte der vorigen - die
                    // Reihenfolge zweier Schichten kann nicht kippen, und der Schritt ist
                    // ohne Unterschritte stabil.
                    double max = AUSGLEICH_KAPPUNG * Math.Abs(dT) * _schichtKapazitaet;
                    if (q > max) q = max;
                    else if (q < -max) q = -max;

                    _schicht[i] += q;
                    _schicht[i + 1] -= q;
                }

                Schicht_Klemmen();
            }

            Schicht_Inversion();
        }

        /// <summary>
        /// INVERSIONSMISCHUNG (Auftrieb, Konzept 7.4 Punkt 3): Ist eine untere Schicht
        /// wärmer als die darüber, werden beide volumengewichtet gemischt — bei gleichen
        /// Schichtvolumina ist das das arithmetische Mittel. Wiederholt, bis die
        /// Schichtung monoton ist; mehr Durchläufe als Schichten kann eine
        /// Blasensortierung über N Elemente nicht brauchen.
        ///
        /// <para><b>Sie läuft ZWEIMAL je Stunde:</b> einmal im vertikalen Ausgleich (der
        /// Konzeptreihenfolge folgend vor den Verlusten) und ein zweites Mal NACH der
        /// Verlustverteilung. Der Grund ist der Deckel: Die oberste und die unterste
        /// Schicht tragen zusätzlich zur Mantelfläche eine Stirnfläche und verlieren
        /// deshalb etwas mehr als die Schichten dazwischen — bei durchgeladenem Speicher
        /// entsteht daraus eine winzige Inversion an der Oberkante. Ohne den zweiten
        /// Durchgang stünde sie in der Ganglinie und in <c>T_oben</c>, obwohl der Auftrieb
        /// sie physikalisch sofort auflöst. Der Vorgang ist energieerhaltend und bei
        /// N = 1 wirkungslos.</para>
        /// </summary>
        private void Schicht_Inversion()
        {
            if (_schicht == null || _schicht.Length < 2) return;

            int n = _schicht.Length;
            int bloecke = 0;

            // BLOCKMITTELUNG statt paarweisem Tauschen. Zwei benachbarte Schichten
            // paarweise zu mitteln, bis nichts mehr kippt, KONVERGIERT NUR IM GRENZWERT:
            // Jede Mittelung stößt die nächste an, und nach einer festen Zahl Durchläufe
            // bleibt ein Rest stehen, der als winzige Inversion in der Ganglinie landet
            // (an Projekt 1024 gemessen: 1,3e-5 K in 6842 von 8760 Stunden). Dieses
            // Verfahren stellt die stabile Schichtung dagegen in EINEM Durchlauf her: Ein
            // Block wird mit dem darüber verschmolzen, solange er wärmer ist, und trägt
            // danach das Mittel aller beteiligten Schichten. Bei gleichen
            // Schichtvolumina ist das genau die volumengewichtete Mischung des Auftriebs,
            // und die Summe bleibt erhalten.
            for (int i = 0; i < n; i++)
            {
                _blockWert[bloecke] = _schicht[i];
                _blockLaenge[bloecke] = 1;
                bloecke++;

                while (bloecke > 1 && _blockWert[bloecke - 2] < _blockWert[bloecke - 1])
                {
                    double s = _blockWert[bloecke - 2] * _blockLaenge[bloecke - 2] +
                               _blockWert[bloecke - 1] * _blockLaenge[bloecke - 1];
                    int l = _blockLaenge[bloecke - 2] + _blockLaenge[bloecke - 1];
                    bloecke--;
                    _blockWert[bloecke - 1] = s / l;
                    _blockLaenge[bloecke - 1] = l;
                }
            }

            int index = 0;
            for (int b = 0; b < bloecke; b++)
                for (int k = 0; k < _blockLaenge[b]; k++) _schicht[index++] = _blockWert[b];
        }

        /// <summary>
        /// Verteilt den SCHICHTANTEIL der Bereitschaftsverluste (Konzept 7.4 Punkt 4) —
        /// nach Wärmeabgabefläche und gewichtet mit
        /// <c>(T[i] − RL_eff)/(VL_eff − RL_eff)</c>. Eine Schicht auf Rücklauftemperatur
        /// verliert nichts mehr: Sie hat keine Übertemperatur gegen die Umgebung.
        ///
        /// <para>Bei N = 1 ist das Ergebnis exakt die füllstandsanteilige Rechnung des
        /// Bestands — es gibt genau eine Schicht, und sie bekommt den ganzen Betrag, den
        /// die SOC-Buchung eine Zeile zuvor abgezogen hat.</para>
        /// </summary>
        private void Schicht_Verluste(double verlust)
        {
            if (verlust <= 0 || _schicht == null) return;

            if (_schicht.Length == 1)
            {
                _schicht[0] -= verlust;
                if (_schicht[0] < 0) _schicht[0] = 0;
                return;
            }

            double spreizung = VL_eff - RL_eff;
            if (spreizung <= 0) return;

            double summe = 0;
            for (int i = 0; i < _schicht.Length; i++)
            {
                double g = _schichtFlaeche[i] * (SchichtTemperatur(i) - RL_eff) / spreizung;
                if (g > 0) summe += g;
            }
            if (summe <= 0) return;

            for (int i = 0; i < _schicht.Length; i++)
            {
                double g = _schichtFlaeche[i] * (SchichtTemperatur(i) - RL_eff) / spreizung;
                if (g <= 0) continue;

                _schicht[i] -= verlust * (g / summe);
                if (_schicht[i] < 0) _schicht[i] = 0;
            }
        }

        /// <summary>Hält jede Schicht im zulässigen Band [0, E_max] ⇔ [RL_eff, VL_eff].</summary>
        private void Schicht_Klemmen()
        {
            if (_schicht == null) return;

            for (int i = 0; i < _schicht.Length; i++)
            {
                if (_schicht[i] < 0) _schicht[i] = 0;
                else if (_schicht[i] > _schichtMax) _schicht[i] = _schichtMax;
            }
        }

        /// <summary>
        /// Zieht die Schichtebene am Ende der Stunde auf die INVARIANTE
        /// <c>Σ E[i] == min(SOC, Q_max)</c> nach (Konzept 7.3/11.3) und misst dabei die
        /// Abweichung.
        ///
        /// <para><b>Warum das nötig ist und warum es nichts verdeckt.</b> Jede
        /// Schichtoperation bucht denselben Betrag, den die SOC-Arithmetik gebucht hat;
        /// zwischen beiden kann sich deshalb nur die Rundung einer Verteilung über N
        /// Summanden schieben (Größenordnung 1e-16 je Stunde) — und die Klemmung an den
        /// Bandgrenzen. Beides wird hier ausgeglichen, BEVOR es sich über 8760 Stunden
        /// aufaddiert. Eine echte Abweichung wäre um Zehnerpotenzen größer und wird
        /// gezählt (<see cref="SchichtInvarianteVerletzungen"/>); im Debug-Build meldet
        /// sie zusätzlich <see cref="Schichtprobe"/>.</para>
        ///
        /// <para>Bei N = 1 ist der Faktor exakt 1,0 — <c>E[0]</c> und <c>A</c> entstehen
        /// aus denselben Buchungen.</para>
        /// </summary>
        private void Schicht_Nachfuehren()
        {
            if (_schicht == null) return;

            double ziel = (SOC < Q_max) ? SOC : Q_max;
            if (ziel < 0) ziel = 0;

            double summe = 0;
            for (int i = 0; i < _schicht.Length; i++) summe += _schicht[i];

            double abweichung = summe - ziel;
            double toleranz = 1e-6 * (Q_max > 1 ? Q_max : 1);

            // ERSTBEFÜLLUNG VON AUSSEN ist keine Verletzung: Ein QUELLSPEICHER startet
            // VOLL — <c>WaermequelleClass.Quellspeicher</c> setzt <c>SOC = Q_max</c>
            // direkt nach <see cref="Init"/>, ohne je <see cref="Laden"/> zu rufen. Die
            // Schichtebene hat diesen Vorgang nie gesehen und steht auf null; sie wird
            // gleich unten aufgefüllt. Gezählt wird deshalb nur, was AUS einer laufenden
            // Buchung auseinandergelaufen ist (summe > 0).
            if (summe > 0 && Math.Abs(abweichung) > toleranz)
            {
                SchichtInvarianteVerletzungen++;
                if (Math.Abs(abweichung) > SchichtInvarianteMaxAbweichung)
                    SchichtInvarianteMaxAbweichung = Math.Abs(abweichung);
            }

            if (Math.Abs(abweichung) <= 1e-12 * (Q_max > 1 ? Q_max : 1)) return;

            if (summe > 0)
            {
                double faktor = ziel / summe;
                for (int i = 0; i < _schicht.Length; i++) _schicht[i] *= faktor;
            }
            else if (ziel > 0)
            {
                // Kein Inhalt, aber Zielenergie: von oben auffüllen (der Fall tritt nur
                // auf, wenn ein Speicher außerhalb der Schichtbuchung gefüllt wurde).
                double rest = ziel;
                for (int i = 0; i < _schicht.Length && rest > 0; i++)
                    rest -= Fuellen(i, rest);
            }

            Schicht_Klemmen();
        }

        /// <summary>Schreibt die Temperaturganglinien der Stunde (nur bei echter Schichtrechnung).</summary>
        private void Schicht_Ganglinie(int stunde)
        {
            if (stunde < 0 || stunde >= 8760) return;
            if (IstQuelle || Q_max <= 0 || VL_eff <= RL_eff) return;

            T_oben_stuendlich[stunde] = (float)T_oben;
            T_unten_stuendlich[stunde] = (float)T_unten;
        }

        /// <summary>
        /// Bildet <see cref="T_oben_Mittel"/> und <see cref="T_oben_Min"/> aus der
        /// Ganglinie der obersten Schicht (Befund E1-O5).
        ///
        /// <para><b>QUELLSPEICHER bleiben NULL.</b> Ihr Temperaturpaar ist ein
        /// Ersatzwertpaar aus der Anlagen-Spreizung (Spreizung/0), keine
        /// Speichertemperatur; eine Zustandsformel darauf wäre Scheinphysik
        /// (Konzept 8.2). NULL heißt in der Ergebniszeile „nicht erhoben" — genau das
        /// trifft zu.</para>
        /// </summary>
        private void Schicht_Kennzahlen()
        {
            T_oben_Mittel = null;
            T_oben_Min = null;

            if (IstQuelle || Q_max <= 0 || VL_eff <= RL_eff) return;
            if (T_oben_stuendlich == null || T_oben_stuendlich.Length == 0) return;

            double summe = 0;
            double min = double.MaxValue;
            for (int i = 0; i < T_oben_stuendlich.Length; i++)
            {
                double v = T_oben_stuendlich[i];
                summe += v;
                if (v < min) min = v;
            }

            T_oben_Mittel = summe / T_oben_stuendlich.Length;
            T_oben_Min = min;
        }

#if DEBUG

        /// <summary>
        /// PRÜFHAKEN der Schichtebene — ausschließlich im Debug-Build, nach dem Muster
        /// von <c>Kaskadenschleife.Entladeprobe</c> (kein Prüfcode im Release-Assembly).
        ///
        /// Parameter: Puffer-ID, Stunde, <c>min(SOC, Q_max)</c>, Summe der
        /// Schichtenergie, Temperaturen von oben nach unten. Damit lassen sich die drei
        /// Invarianten aus Konzept 11.3 an einem echten Lauf messen: Summengleichheit,
        /// Monotonie nach der Inversionsmischung und <c>RL_eff ≤ T[i] ≤ VL_eff</c>.
        /// </summary>
        public static Action<int, int, double, double, double[]> Schichtprobe;

        /// <summary>Meldet den Schichtzustand an <see cref="Schichtprobe"/>, sofern gesetzt.</summary>
        public void SchichtprobeMelden(int stunde)
        {
            if (Schichtprobe == null || _schicht == null) return;

            double ziel = (SOC < Q_max) ? SOC : Q_max;
            if (ziel < 0) ziel = 0;

            double summe = 0;
            double[] temperaturen = new double[_schicht.Length];
            for (int i = 0; i < _schicht.Length; i++)
            {
                summe += _schicht[i];
                temperaturen[i] = SchichtTemperatur(i);
            }

            Schichtprobe(ID_Pufferspeicher, stunde, ziel, summe, temperaturen);
        }

#endif
    }
}
