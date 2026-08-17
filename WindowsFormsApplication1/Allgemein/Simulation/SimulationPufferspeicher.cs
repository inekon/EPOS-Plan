using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einfaches Energiebilanz-Modell eines thermischen Pufferspeichers für die
    /// Jahressimulation (Stundenschritte, 1 h => kW entspricht kWh).
    ///
    /// Stufe 1 der Pufferspeicher-Integration:
    /// - Nutzbare Kapazität aus Volumen und Temperaturspreizung der Zuordnung
    ///   (Z_ProjektPufferSp: Vorlauf/Rücklauf; Tab_Pufferspeicher: Gesamtvolumen):
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
        // Projektkopie Tab_Pufferspeicher gefüllt. Im EINKANALIGEN Altpfad sind sie
        // nicht im Rechenpfad; ausgewertet werden sie im zweikanaligen Weg (Etappe 4b):
        // die aus der Kaskade gelöste Ladephase (6.3 C/D) mit Prioritätsauflösung
        // (3.4) und die Entladereihenfolge bei mehreren Puffern je Kanal (3.6).
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
        /// IM ZWEIKANALIGEN WEG bleibt die Einschränkung bestehen, nur mit einem anderen
        /// Kriterium: Dort tragen das Flag die Speicher mit einer SENKEN-Referenz
        /// (<c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c> einer Projektanlage) und die
        /// Quellspeicher (<c>WQ_ID_Puffer</c>) — also genau die, die ein Erzeuger laden
        /// oder entladen kann (<c>SimulationControl.RegistryFuerZweikanaligOeffnen</c>).
        /// Ein Puffer, der nur über die Alt-Zuordnung <c>Z_ProjektPufferSp</c> im Projekt
        /// hängt und keine Senkenreferenz trägt, rechnet auch dort nicht mit: Er würde
        /// sonst mit lauter Nullen in der Ergebnispersistenz erscheinen und über
        /// <c>puffer_wp</c> eine Speicherkapazität melden, die kein Erzeuger benutzt.
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
        // OHNE Durchlass — jeder Aufruf des Altpfads — ist B durchgehend 0: Die
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
        /// <param name="vorlauf">Vorlauftemperatur [°C] (Z_ProjektPufferSp)</param>
        /// <param name="ruecklauf">Rücklauftemperatur [°C] (Z_ProjektPufferSp)</param>
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
            Reset();
        }

        /// <summary>Setzt den Speicherzustand für einen neuen Simulationslauf zurück.</summary>
        public void Reset()
        {
            // Hysterese wie beim abzulösenden _speicherLaden: Der Lauf beginnt mit
            // leerem Speicher, also zuerst laden (im Altpfad bleibt das Feld ungelesen).
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
        /// bisherige — der Altpfad ruft ausschließlich diese Form auf.
        /// </summary>
        /// <param name="durchlass">
        /// Im selben Zeitschritt absehbare Entnahme [kWh], um die die Aufnahme über die
        /// freie Kapazität hinausgehen darf. Negative Werte gelten als 0.
        /// </param>
        public double Laden(double energieKWh, int stunde, double durchlass)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            if (durchlass < 0) durchlass = 0;
            double frei = Q_max - SOC + durchlass;
            double ladung = Math.Min(energieKWh, frei);
            if (ladung <= 0) return 0;

            // N6: Aufnahme in SPEICHERUMSATZ und DURCHFLUSS zerlegen. Der Teil bis Q_max
            // ist Umsatz, alles darüber fließt in derselben Stunde weiter. Ohne Durchlass
            // ist der zweite Summand konstruktiv 0 — der Altpfad rechnet bitgleich.
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
            return ladung;
        }

        /// <summary>
        /// Entnimmt die angeforderte Energie [kWh] aus dem Speicher und liefert
        /// zurück, wie viel tatsächlich geliefert werden konnte (Rest: Speicher leer).
        /// </summary>
        public double Entladen(double energieKWh, int stunde)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            double entnahme = Math.Min(energieKWh, SOC);
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
            if (stunde >= 0 && stunde < 8760)
            {
                Entladung_stuendlich[stunde] += (float)umsatz;
                if (durchfluss > 0) Durchsatz_Entladung_stuendlich[stunde] += (float)durchfluss;
            }
            return entnahme;
        }

        /// <summary>
        /// Verrechnet den stündlichen Bereitschaftsverlust (anteilig zum Füllstand)
        /// und speichert den Speicherzustand der Stunde für die Auswertung.
        /// </summary>
        public void StundeAbschliessen(int stunde)
        {
            Abschluesse++;

            if (Q_max > 0 && SOC > 0)
            {
                // Der Anteil ist auf 1 begrenzt: Mit dem Durchlass (Laden mit
                // hydraulischer Weiche) kann SOC innerhalb einer Stunde über Q_max
                // liegen. Bis Phase G ist er normalerweise wieder darunter — bliebe
                // doch etwas stehen, dürfte daraus kein überhöhter Bereitschaftsverlust
                // werden. Ohne Durchlass gilt SOC <= Q_max, die Klemmung greift dann nie
                // und der Altpfad rechnet bitgleich wie bisher.
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
            }

            if (stunde >= 0 && stunde < 8760) SOC_stuendlich[stunde] = (float)SOC;
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
        /// </summary>
        public bool IstBrauchwasserkanal
        {
            get { return Verwendung == VERWENDUNG_BRAUCHWASSER; }
        }

        /// <summary>
        /// true = KOMBISPEICHER (Etappe D5a): bedient Heizung UND Warmwasser aus einem
        /// Vorrat. <see cref="IstBrauchwasserkanal"/> bleibt für ihn <c>false</c> — er ist
        /// kein reiner Warmwasserspeicher; die Kanalfrage beantwortet
        /// <see cref="BedientKanal"/>.
        ///
        /// <b>Warum der ORDINALE Vergleich hier richtig ist</b> (Etappe D5b, Befund
        /// K3-2): <see cref="Verwendung"/> wird ausschließlich aus
        /// <c>WaermesenkeClass.WirksameVerwendung</c> oder aus den Konstanten dieser
        /// Klasse gefüllt, und <c>WirksameVerwendung</c> normalisiert seit D5b auf die
        /// kanonische Schreibweise. Ein Datenbankwert <c>"kombi"</c> kommt hier deshalb
        /// als <c>"Kombi"</c> an — vorher stand er in beiden Entladereihenfolgen
        /// (die vergleichen <c>OrdinalIgnoreCase</c>), verhielt sich im Lauf aber wie ein
        /// Heizungspuffer.
        /// </summary>
        public bool IstKombi
        {
            get { return Verwendung == VERWENDUNG_KOMBI; }
        }

        /// <summary>
        /// Bedient dieser Speicher den angefragten Kanal? Ein Kombispeicher beantwortet
        /// BEIDE Fragen mit <c>true</c>, jeder andere Senkenspeicher genau seine eigene
        /// (Etappe D5a). Quellspeicher bedienen keinen Kanal.
        /// </summary>
        /// <param name="brauchwasser">true = Warmwasserkanal, false = Heizkanal</param>
        public bool BedientKanal(bool brauchwasser)
        {
            if (IstQuelle) return false;
            if (IstKombi) return true;
            return IstBrauchwasserkanal == brauchwasser;
        }

        /// <summary>
        /// Ladefähigkeit [kWh] gegen eine Obergrenze nach Konzept 3.4:
        /// <c>Q_max · Obergrenze − SOC − <see cref="Reserviert"/></c>, nie negativ.
        ///
        /// Der reservierte Betrag (Befund N3) ist bereits vergeben: Ein Erzeuger, der
        /// seine Produktion in Phase B gegen diesen Raum entschieden hat, muss ihn in
        /// den Ladephasen noch vorfinden. Ohne Reservierung — jeder Aufruf des Altpfads
        /// und jede Stunde ohne BHKW in Phase B — ist das Feld 0 und der Ausdruck
        /// bitgleich der bisherige.
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
        /// Höchste Entnahme [kWh] in EINER Stunde.
        ///
        /// Vorgemerkter Parameter (Paket 4, Nutzerentscheidung zu 4b-1): Eine Lade- bzw.
        /// Entladeleistung je Speicher [kW] ist fachlich sinnvoll — ein 800-l-Puffer mit
        /// DN 25 kann keine 200 kW durchreichen —, existiert aber weder im Datenmodell
        /// noch in der Oberfläche. Bis dahin gilt UNBEGRENZT; das ist zugleich die
        /// bisherige Annahme des Modells („keine Begrenzung der Be-/Entladeleistung",
        /// siehe Kopfkommentar).
        /// </summary>
        public double EntladeleistungMax = 0;   // 0 = unbegrenzt

        /// <summary>Entnahmefähigkeit einer Stunde [kWh]; unbegrenzt, solange kein Wert gepflegt ist.</summary>
        public double Entnahmefaehigkeit()
        {
            return EntladeleistungMax > 0 ? EntladeleistungMax : double.MaxValue;
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
        /// Dieselbe Regel wie im einkanaligen Altpfad, nur nicht mehr modulübergreifend,
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
    }
}
