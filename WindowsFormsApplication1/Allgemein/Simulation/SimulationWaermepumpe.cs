using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class SimulationWaermepumpe
    {
        public const int MAX_WP = 10;

        public List<int> wp_list = new List<int>();
        public double Waermebedarf_gesamt;
        public float[] Waermebedarf_stuendlich = new float[8760];
        public float[] waermerestbedarf_stuendlich = new float[8760];
        public double waermerestbedarf_gesamt;

        public double[] WP_Strombedarf_monatlich = new double[12];
        public double[] WP_Waermeproduktion_monatlich = new double[12];
        public double[] Heizstab_monatlich = new double[12];
        public float[] Temperatur = new float[8760];

        public float[] WP_Strombedarf_stuendlich = new float[8760];
        public float[] WP_Waermeproduktion_stuendlich = new float[8760];
        public float[] WP_Waermeproduktion_stuendlich_sortiert = new float[8760];
        public float[] Heizstab_stuendlich = new float[8760];

        public double WP_Strombedarf_gesamt = 0;
        public double WP_Waermeproduktion_gesamt = 0;
        public double Heizstab_gesamt = 0;
        public double WP_Laufzeit = 0;

        public double[] Modul_WP_Strombedarf = new double[MAX_WP];
        public double[] Modul_WP_Waermeproduktion = new double[MAX_WP];
        public double[] Modul_Heizstab = new double[MAX_WP];
        public double[] Modul_WP_Laufzeit = new double[MAX_WP];

        public List<WErzeugerModel> wp_model = new List<WErzeugerModel>();
        private List<_Kenndaten> wp_kenndaten = new List<_Kenndaten>();

        // Quelltemperatur-Jahresprofil je WP-Modul (Wärmequelle):
        // Luft-Wasser = Außentemperatur; Sole-/Wasser-Wasser gemäß WQ_Typ
        // (Konstant, Pufferspeicher, Profil, CSV) - siehe WaermequelleClass.
        private List<float[]> wp_quelltemp = new List<float[]>();

        // Quell-Pufferspeicher je WP-Modul (Wärmequelle "Pufferspeicher");
        // null = keine Speicherbilanz (Außenluft, Konstant, Profil, CSV,
        // oder Quelle als unbegrenzt verfügbar konfiguriert).
        private List<SimulationPufferspeicher> wp_quellspeicher = new List<SimulationPufferspeicher>();

        /// <summary>
        /// Quellspeicher je WP-Modul in Modulreihenfolge (Einträge können null sein).
        /// Lesezugriff für Ergebnis-Persistenz und Anzeigen (Konzept 6.6/13.3) -
        /// die Liste selbst bleibt in der Hand der Simulation.
        /// </summary>
        public IReadOnlyList<SimulationPufferspeicher> Quellspeicher { get { return wp_quellspeicher; } }

        /// <summary>
        /// Ersetzt die Quellspeicher-INSTANZ eines Puffers durch eine andere
        /// (Etappe D5a, Kaskade/Booster).
        ///
        /// Zeigt <c>WQ_ID_Puffer</c> auf einen Puffer, der im selben Projekt schon als
        /// SENKE eines anderen Erzeugers rechnet, wäre eine zweite Instanz genau das,
        /// was Konzept 6.2 verbietet: zwei Speicherverwaltungen mit getrennter Bilanz.
        /// Sie startete außerdem VOLL (so baut <c>WaermequelleClass.Quellspeicher</c>
        /// einen Quellspeicher auf) und liefe damit auf Wärme hinaus, die niemand
        /// erzeugt hat. Die Booster-Konstellation des Konzepts — WP 1 lädt Puffer 1,
        /// WP 2 bezieht daraus ihre Quellwärme — braucht deshalb DIESELBE Instanz.
        ///
        /// Aufgerufen wird die Methode aus
        /// <c>SimulationControl.QuellspeicherUebernehmen</c>.
        /// </summary>
        /// <returns>Zahl der ersetzten Moduleinträge.</returns>
        public int QuellspeicherErsetzen(SimulationPufferspeicher alt, SimulationPufferspeicher neu)
        {
            if (alt == null || neu == null || ReferenceEquals(alt, neu)) return 0;

            int n = 0;
            for (int i = 0; i < wp_quellspeicher.Count; i++)
                if (ReferenceEquals(wp_quellspeicher[i], alt)) { wp_quellspeicher[i] = neu; n++; }

            return n;
        }

        /// <summary>
        /// Meldungen über Quellentnahmen der laufenden Phase (Etappe D5a). Die
        /// Kaskadenschleife führt daraus die Herkunftsrechnung fort und leert die Liste.
        ///
        /// Gefüllt wird sie nur, wenn der Quellspeicher ein SENKENspeicher ist — also in
        /// der Kaskade. Ein echter Quellspeicher (Erdreich-Ersatz, Grundwasser) trägt
        /// keine Herkunftsanteile; für ihn bliebe die Buchung wirkungslos.
        /// </summary>
        public readonly List<Quellentnahme> Quellentnahmen = new List<Quellentnahme>();

        /// <summary>
        /// RECHENEBENE je WP-Modul (Etappe D5a) — indexgleich zu <see cref="wp_list"/>.
        /// <c>null</c> bedeutet „alle Module auf Ebene 0", also wie bisher.
        /// </summary>
        public int[] ModulEbenen;

        /// <summary>Ebene, die die Kaskadenschleife gerade abarbeitet (Etappe D5a).</summary>
        public int AktiveEbene = 0;

        /// <summary>true, wenn Modul <paramref name="i"/> auf der aktiven Ebene rechnet.</summary>
        private bool EbeneAktiv(int i)
        {
            if (ModulEbenen == null || i < 0 || i >= ModulEbenen.Length) return AktiveEbene == 0;
            return ModulEbenen[i] == AktiveEbene;
        }

        /// <summary>
        /// Meldet eine Quellentnahme an die Herkunftsrechnung (Etappe D5a). Für echte
        /// Quellspeicher entfällt die Meldung — siehe <see cref="Quellentnahmen"/>.
        /// </summary>
        private void QuellentnahmeMelden(SimulationPufferspeicher quelle, double menge,
                                         SimulationPufferspeicher ziel)
        {
            if (quelle == null || menge <= 0 || quelle.IstQuelle) return;
            Quellentnahmen.Add(new Quellentnahme { Quelle = quelle, Menge = menge, Ziel = ziel });
        }

        /// <summary>
        /// Quelltemperatur-Jahresprofil je WP-Modul in Modulreihenfolge.
        /// Lesezugriff für die zweite Warnbedingung der Erdreich-Prüfung
        /// (Quelltemperatur minus Spreizung dauerhaft &lt; 0 °C, Konzept 13.1).
        ///
        /// <para><b>PAKET B1:</b> Für ein temperaturgekoppeltes Modul
        /// (<see cref="QuelleGekoppelt"/>) ist diese Reihe kein Eingangswert mehr,
        /// sondern ein LAUFERGEBNIS — sie entsteht Stunde für Stunde aus dem Zustand des
        /// geteilten Quellpuffers (Konzept 8.2).</para>
        /// </summary>
        public IReadOnlyList<float[]> Quelltemperaturen { get { return wp_quelltemp; } }

        // ==================================================================
        // PAKET B1 — BOOSTER-TEMPERATURKOPPLUNG (Konzept 8.2, Leitentscheidung L8)
        //
        // SCHNITTSTELLENWECHSEL, kein Wertetausch: Bis P1 lieferte
        // WaermequelleClass.Quelltemperatur EINMAL beim Modulaufbau ein komplettes
        // Jahresprofil float[8760], das die Stundenschleife danach nur noch ablas. Für
        // einen GETEILTEN Quellpuffer — ein Speicher, der zugleich Senke eines anderen
        // Erzeugers ist — ist das falsch: Seine Temperatur folgt dem Ladezustand, und
        // genau diese Aufwertung ist die Physik des Boosters.
        //
        // NEU für diesen Fall: Die Quelltemperatur wird JE STUNDE GENAU EINMAL gebildet,
        // unmittelbar vor Phase B der Rechenebene des beziehenden Moduls
        // (Kaskadenschleife, Aufruf von Quelltemperatur_Stunde), und gilt für die ganze
        // Stunde — Bedarfs- UND Ladephase derselben Ebene. Eine zweite Abfrage innerhalb
        // der Stunde wäre nicht reproduzierbar spezifiziert: Der SOC des Puffers ändert
        // sich zwischen den Phasen mehrfach.
        //
        // GELTUNGSBEREICH (Konzept 8.2, letzter Absatz): NUR der geteilte Puffer.
        // Eigenständige Quellspeicher (Erdsonden-Ersatz mit WQ_Spreizung, Start voll)
        // behalten die statische Quelltemperatur — ihr Temperaturpaar (Spreizung/0) sind
        // keine Speichertemperaturen, eine Zustandsformel darauf wäre Scheinphysik. Das
        // Unterscheidungsmerkmal ist die ROLLE der Speicherinstanz: Ein geteilter Puffer
        // ist ein SENKENspeicher der Registry (IstQuelle == false); die eigene
        // Quellinstanz eines eigenständigen Speichers trägt IstQuelle == true.
        // ==================================================================

        /// <summary>
        /// Je Modul: true = die Quelltemperatur folgt stündlich dem geteilten Quellpuffer
        /// (Paket B1). <c>null</c> vor <see cref="BoosterKopplungVorbereiten"/>.
        /// </summary>
        private bool[] _quellKopplung;

        /// <summary>
        /// PAKET Q1: Quell-Entnahmehöhe je Modul, 0…1 (1 = ganz oben) aus
        /// <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c> (Schema-Schritt 54). NULL in der
        /// Datenbank — und jede Datenbank vor Schritt 54 — ergibt
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> und damit exakt das
        /// Verhalten von Paket B1.
        /// </summary>
        private double[] _quellHoehe;

        /// <summary>true = Modul <paramref name="index"/> ist temperaturgekoppelt (Paket B1).</summary>
        public bool QuelleGekoppelt(int index)
        {
            return _quellKopplung != null && index >= 0 && index < _quellKopplung.Length &&
                   _quellKopplung[index];
        }

        /// <summary>
        /// PAKET Q1: die Quell-Entnahmehöhe des Moduls <paramref name="index"/>, 0…1;
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> ohne gepflegten Wert —
        /// Lesezugriff für Protokoll und Wirkproben.
        /// </summary>
        public double QuellAnschlusshoehe(int index)
        {
            if (_quellHoehe == null || index < 0 || index >= _quellHoehe.Length)
                return SimulationPufferspeicher.HOEHE_OBEN;
            return _quellHoehe[index];
        }

        /// <summary>Zahl der temperaturgekoppelten Module (Paket B1); 0 = Bestandsverhalten.</summary>
        public int GekoppelteModule
        {
            get
            {
                if (_quellKopplung == null) return 0;
                int n = 0;
                for (int i = 0; i < _quellKopplung.Length; i++) if (_quellKopplung[i]) n++;
                return n;
            }
        }

        /// <summary>
        /// Richtet die Temperaturkopplung ein (Paket B1) — EINMAL je Lauf, nachdem
        /// <c>SimulationControl.QuellspeicherUebernehmen</c> die geteilten Instanzen
        /// eingesetzt hat. Vorher steht nicht fest, welcher Quellpuffer geteilt ist.
        ///
        /// <para><b>Eigener Vektor für gekoppelte Module.</b>
        /// <c>WaermequelleClass.Quelltemperatur</c> gibt in mehreren Fällen den
        /// ÜBERGEBENEN Außentemperaturvektor unverändert zurück (Luft-Wasser, unbekannte
        /// Quelle, Fehlerrückfall) — mehrere Module teilen sich dann DASSELBE Array, und
        /// es ist zugleich <see cref="Temperatur"/>. Ein gekoppeltes Modul bekommt
        /// deshalb eine eigene Kopie, bevor die erste Stunde hineinschreibt.</para>
        /// </summary>
        /// <returns>Zahl der gekoppelten Module.</returns>
        public int BoosterKopplungVorbereiten()
        {
            _quellKopplung = new bool[wp_quellspeicher.Count];
            _quellHoehe = new double[wp_quellspeicher.Count];
            for (int i = 0; i < _quellHoehe.Length; i++)
                _quellHoehe[i] = SimulationPufferspeicher.HOEHE_OBEN;

            for (int i = 0; i < wp_quellspeicher.Count; i++)
            {
                SimulationPufferspeicher q = wp_quellspeicher[i];

                // Nur der GETEILTE Puffer (Senkenspeicher der Registry). Ein
                // eigenständiger Quellspeicher bleibt statisch (Konzept 8.2/7.6).
                if (q == null || q.IstQuelle) continue;

                // Ohne Temperaturachse gibt es nichts zu koppeln - dann bliebe
                // SchichtTemperatur konstant auf RL_eff und die Kopplung wäre eine
                // schlechtere Konstante als der Bestandswert.
                if (q.Q_max <= 0 || q.VL_eff <= q.RL_eff) continue;

                if (i < wp_quelltemp.Count && wp_quelltemp[i] != null)
                {
                    float[] eigen = new float[wp_quelltemp[i].Length];
                    Array.Copy(wp_quelltemp[i], eigen, eigen.Length);
                    wp_quelltemp[i] = eigen;
                }

                // PAKET Q1: die Quell-Entnahmehöhe der ANLAGE (Schema-Schritt 54).
                // EINMAL je Lauf gelesen - sie ist eine Konfigurationsgröße, keine
                // Zustandsgröße. Ohne die Spalte (Datenbank vor Schritt 54) und bei
                // NULL bleibt es bei „oben" und damit beim B1-Verhalten.
                if (i < wp_list.Count)
                    _quellHoehe[i] = AnschlusshoeheLesen(wp_list[i]);

                _quellKopplung[i] = true;
            }

            return GekoppelteModule;
        }

        /// <summary>
        /// PAKET Q1: <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c> einer Anlage, auf 0…1
        /// begrenzt; <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> bei NULL,
        /// fehlender Spalte oder unbrauchbarem Wert.
        ///
        /// <para>Ein Wert außerhalb 0…1 wird NICHT geklemmt, sondern verworfen: Er kann
        /// nicht aus dem Dialog stammen (der prüft) und ist damit ein Datenfehler; „oben"
        /// ist dafür die richtige, dokumentierte Vorgabe — dieselbe Auslegung, die auch
        /// <c>SimulationPufferspeicher.SchichtIndex</c> anwendet.</para>
        /// </summary>
        internal static double AnschlusshoeheLesen(int idAnlage)
        {
            object v = WaermequelleClass.WertLesenStill(
                idAnlage, SchemaKatalog.SPALTE_ANLAGE_WQ_ANSCHLUSSHOEHE);
            if (v == null) return SimulationPufferspeicher.HOEHE_OBEN;

            try
            {
                double h = Convert.ToDouble(v);
                if (h < 0 || h > 1) return SimulationPufferspeicher.HOEHE_OBEN;
                return h;
            }
            catch { return SimulationPufferspeicher.HOEHE_OBEN; }
        }

        /// <summary>
        /// Bildet die Quelltemperatur der Stunde für alle gekoppelten Module der AKTIVEN
        /// Rechenebene (Paket B1, Konzept 8.2).
        ///
        /// <para>Gerufen von der Kaskadenschleife GENAU EINMAL je Stunde und Ebene, vor
        /// Phase B. Der Wert steht danach für die ganze Stunde in
        /// <see cref="Quelltemperaturen"/> und wird von Bedarfs- und Ladephase gelesen.</para>
        ///
        /// <para>Ohne gekoppeltes Modul ist die Methode ein sofortiger Rücksprung — das
        /// ist der Regelfall und der Grund, warum der Bestand von Paket B1 keinen
        /// einzigen Takt sieht.</para>
        /// </summary>
        public void Quelltemperatur_Stunde(int stunde)
        {
            if (_quellKopplung == null) return;
            if (stunde < 0 || stunde >= 8760) return;

            for (int i = 0; i < _quellKopplung.Length; i++)
            {
                if (!_quellKopplung[i] || !EbeneAktiv(i)) continue;

                SimulationPufferspeicher q = wp_quellspeicher[i];
                if (q == null || i >= wp_quelltemp.Count || wp_quelltemp[i] == null) continue;

                // PAKET Q1: an der gepflegten Quell-Entnahmehöhe statt fest oben.
                wp_quelltemp[i][stunde] = (float)q.QuellEntnahmeTemperatur(_quellHoehe[i]);
            }
        }

        // Bauart der Wärmepumpe je Modul (Tab_WP.Typ): "Luft-Wasser",
        // "Sole-Wasser", "Wasser-Wasser". Wird beim Aufbau der Kaskade gelesen.
        private List<string> wp_typ = new List<string>();

        /// <summary>
        /// Bauart je WP-Modul in Modulreihenfolge (Tab_WP.Typ).
        ///
        /// Sie ist die WIRKSAMKEITSREGEL der Wärmequelle: Für "Luft-Wasser" liefern
        /// <see cref="WaermequelleClass.Quelltemperatur"/> und
        /// <see cref="WaermequelleClass.Quellspeicher"/> immer Außenluft bzw. null -
        /// eine gepflegte WQ_*-Konfiguration bleibt dort ohne jede Wirkung. Auswertungen
        /// (Erdreich-Auslegungsprüfung) müssen das mitprüfen, sonst weisen sie Zahlen
        /// für eine Quelle aus, die gar nicht gerechnet wurde.
        /// </summary>
        public IReadOnlyList<string> WPTypen { get { return wp_typ; } }

        // Wärmesenke je WP-Modul: Beides | Warmwasser | Heizung
        // (WS_Typ der Energieanlage; steuert, welchen Bedarfsanteil das Modul deckt)
        private List<string> wp_senke = new List<string>();

        // Betriebsmodus je WP-Modul: Laufzeit | Leistung | PV (BM_Typ)
        private List<string> wp_modus = new List<string>();

        /// <summary>
        /// Betriebsmodus je WP-Modul in Modulreihenfolge (<c>BM_Typ</c>). Lesezugriff für
        /// den Kontextaufbau der zweikanaligen Kaskade: Die zeitabhängige Ladepriorität
        /// (Konzept 3.5) greift nur bei Betriebsmodus <see cref="WaermequelleClass.MODUS_PV"/>.
        /// </summary>
        public IReadOnlyList<string> Betriebsmodi { get { return wp_modus; } }

        /// <summary>
        /// Stündlicher PV-Überschuss [kW] für den Betriebsmodus "PV-optimiert".
        /// Wird von SimulationControl gesetzt; null = kein PV-Strom verfügbar.
        /// </summary>
        public float[] PV_Ueberschuss_stuendlich = null;

        /// <summary>
        /// Warmwasser-(Brauchwasser-)Anteil des Wärmebedarfs als Stundenganglinie.
        /// Wird von SimulationControl gesetzt und für die Wärmesenken-Aufteilung
        /// benötigt; null = kein Warmwasseranteil bekannt.
        /// </summary>
        public float[] Warmwasserbedarf_stuendlich = null;
        private string[] WP_Betriebsart = new string[MAX_WP];
        private int[] WP_Heizung = new int[MAX_WP];

        public double Bivalenzpunkt = -100;

        // PAKET A1: Hier stand „_speicherLaden" — der modulübergreifende
        // Hysterese-Zustand der einkanaligen Stundenschleife. Er ist mit ihr entfallen;
        // die Hysterese führt seit Etappe 4b jeder SimulationPufferspeicher selbst.

        public bool Mit_Heizstab = false;
        public double Volumen_Pufferspeicher = 0;

        // Senkenspeicher der Wärmepumpe (Alias puffer_wp), von SimulationControl aus der
        // Speicher-Registry gesetzt; null = ohne Pufferspeicher rechnen.
        public SimulationPufferspeicher Pufferspeicher = null;
        private bool extrapolation = false;

        /// <summary>
        /// Projekteinstellung <c>Tab_Einstellungen.Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). <b>Vorbelegung true</b> — sie ersetzt die Rückfrage, die die
        /// Engine bis Paket 8 mitten in der Stundenschleife als MessageBox stellte
        /// („Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert
        /// werden?"). Gesetzt wird sie von <c>SimulationControl</c> aus der
        /// Projektkonfiguration; der Vorbelegungswert gilt, solange niemand sie setzt,
        /// und ist deshalb bewusst der bisherige Antwortwert.
        /// </summary>
        public bool Extrapolation_Erlaubt = true;

        /// <summary>
        /// V0-9 (Entscheidung F13): Stunden je Modul, in denen die Quelltemperatur ÜBER
        /// der obersten Kennlinien-Stützstelle lag und deshalb auf sie gekappt wurde.
        ///
        /// Die Kappung selbst bleibt — oberhalb der obersten Stützstelle gilt deren COP,
        /// konservativ und ohne erfundene Herstellerdaten; extrapoliert wird nach oben
        /// ausdrücklich nicht. Sie war bisher nur STILL: Bei einem Booster (Quelle =
        /// warmer Puffer mit 30–50 °C, Herstellerkennlinien meist bis ~20 °C) ist der
        /// Fall der Normalfall, und der Anwender erfuhr nichts davon. Gezählt wird
        /// deshalb je Modul und EINMAL je Lauf gemeldet
        /// (<see cref="KappungObenMelden"/>) — <c>berechne_wptherm</c> läuft je Modul
        /// bis zu 8760-mal, eine Meldung je Stunde wäre unlesbar.
        ///
        /// ERGEBNISNEUTRAL: reine Zählung neben der Rechnung.
        /// </summary>
        private int[] Modul_Kappung_Oben = new int[MAX_WP];

        /// <summary>
        /// PAKET B1 (Entscheidung F13): Stunden je Modul, in denen die Quelltemperatur
        /// eines TEMPERATURGEKOPPELTEN Moduls UNTER der untersten Kennlinien-Stützstelle
        /// lag und deshalb auf sie gekappt wurde.
        ///
        /// <para>Die Kappung nach unten gilt AUSSCHLIESSLICH für die Pufferquelle
        /// (Konzept 8.2/F13: „Kappung, keine Extrapolation"). Für die Außenluft-,
        /// Erdreich- und Profilpfade bleibt es bei der Projekteinstellung
        /// <see cref="Extrapolation_Erlaubt"/> und der linearen Verlängerung — dort ist
        /// die Unterschreitung ein echter Betriebszustand des Geräts, während sie am
        /// Booster nur heißt, dass der Quellpuffer gerade leer steht.</para>
        ///
        /// <para>Gemeldet wird EINMAL je Modul am Laufende
        /// (<see cref="KappungUntenMelden"/>) — dieselbe Bauart wie
        /// <see cref="KappungObenMelden"/>.</para>
        /// </summary>
        private int[] Modul_Kappung_Unten = new int[MAX_WP];

        /// <summary>Tiefste gekappte Quelltemperatur je Modul [°C] (F13-Protokoll).</summary>
        private double[] Modul_Kappung_Unten_Min = new double[MAX_WP];

        /// <summary>Höchste gekappte Quelltemperatur je Modul [°C] (F13-Protokoll).</summary>
        private double[] Modul_Kappung_Unten_Max = new double[MAX_WP];

        /// <summary>
        /// Fehlertext eines dialogfrei abgebrochenen Laufs (Paket 8, Konzept 13.4). Leer,
        /// wenn nichts anlag. <c>SimulationControl</c> holt ihn ab und reicht ihn über
        /// <see cref="SimulationControl.Fehlertext"/> an den Aufrufer weiter —
        /// dasselbe Muster, das <see cref="SimulationSPK"/> (N10) und
        /// <see cref="SimulationBHKW"/> (N8) bereits verwenden.
        /// </summary>
        public string Fehlertext = "";

        public string[] WP_Modul = new string[MAX_WP];

        public class _Kenndaten
        {
            public int ID_WP = 0;
            public int Vorlauf = 0;
            public int anz = 0;
            public _DAT[] dat;
        }

        public class _DAT
        {
            public double Temperatur = 0;
            public double COP = 0;
            public double Leistung = 0;
        }

        public SimulationWaermepumpe()
        {
        }

        const int STATUS = 0;
        const int COP = 1;
        const int PTHERM = 2;
        const int PEL = 3;

        // PAKET A1: Hier stand „Berechnung()" — der Einstieg des einkanaligen Altpfads
        // (ModuleAufbauen + Berechnung_Stundenschleife). Er ist ersatzlos entfallen; der
        // Einstieg des Moduls ist Vorbereiten_Zweikanalig(), gerechnet wird in der
        // Kaskadenschleife.

        /// <summary>
        /// Baut die Module der Kaskade auf: Kenndaten (Kennlinie je Vorlauf), Bauart,
        /// Wärmequelle (Quelltemperatur und ggf. Quellspeicher), Wärmesenke und
        /// Betriebsmodus je Anlage aus <see cref="wp_list"/>.
        ///
        /// Der Block stand bis Etappe 4b wörtlich am Anfang der Modulberechnung und ist
        /// von dort unverändert hierher gewandert.
        /// </summary>
        /// <returns>
        /// false = Abbruch (fehlende Kenndaten). Der Grund steht seit Paket 8 in
        /// <see cref="Fehlertext"/> und im <see cref="SimulationProtokoll"/> statt in
        /// einer MessageBox (Konzept 13.4).
        /// </returns>
        private bool ModuleAufbauen()
        {
            RecordSet rs = new RecordSet();
            WErzeugerCtrl wp = new WErzeugerCtrl();

            Volumen_Pufferspeicher = 0;
            Fehlertext = "";

            // NACHARBEIT PAKET 8, BEFUND N3: Auch das Extrapolationsmerkmal gehört in den
            // Rücksetzblock. Es wird beim ersten Unterschreiten der Kennlinie gesetzt und
            // blieb bisher über die Lebensdauer der Instanz stehen - im MDI-Fenster lebt
            // dieselbe SimulationControl (und damit dasselbe WP-Modul) über beliebig
            // viele Läufe. Ab dem zweiten Lauf wäre Extrapolation_Erlaubt sonst
            // wirkungslos: kein Abbruch bei Verbot, kein Hinweis bei Erlaubnis.
            //
            // ERGEBNISNEUTRAL: Das Merkmal steuert AUSSCHLIESSLICH die Meldung und die
            // Verbotsprüfung - die lineare Verlängerung der Kennlinie darunter läuft in
            // jedem Fall (siehe berechne_wptherm, der Zweig hinter "if (!extrapolation)"
            // endet vor der Rechnung).
            extrapolation = false;

            // V0-9: Der Kappungszähler gehört aus demselben Grund in den Rücksetzblock -
            // dieselbe Instanz rechnet im MDI-Fenster beliebig viele Läufe, und die
            // Stundenzahl der Meldung wäre sonst die Summe aller bisherigen Läufe.
            Array.Clear(Modul_Kappung_Oben, 0, Modul_Kappung_Oben.Length);

            // PAKET B1 (F13): derselbe Grund für den Zähler der UNTEREN Kappung.
            Array.Clear(Modul_Kappung_Unten, 0, Modul_Kappung_Unten.Length);
            for (int k = 0; k < MAX_WP; k++)
            {
                Modul_Kappung_Unten_Min[k] = double.MaxValue;
                Modul_Kappung_Unten_Max[k] = double.MinValue;
            }

            Init();

            wp_model.Clear();
            wp_kenndaten.Clear();
            wp_quelltemp.Clear();
            wp_quellspeicher.Clear();
            wp_typ.Clear();
            wp_senke.Clear();
            wp_modus.Clear();
            for (int i = 0; i < wp_list.Count; i++)
            {
                wp.ReadAllFilter("ID=" + wp_list[i]);
                WErzeugerModel model = wp.items[0];
         
                WP_Betriebsart[i] = model.Betriebsart != null ? model.Betriebsart : "";
                WP_Modul[i] = model.Bezeichner; 

                if (model.Volumen > 0) Volumen_Pufferspeicher = model.Volumen; 

                string wpTyp = "";
                rs.Open("select * from Tab_WP where ID=" + model.ID_WP);
                if (rs.Next())
                {
                    model.Grenzleistung = (int)rs.Read("Nennleistung");
                    WP_Heizung[i] = (int)rs.Read("Heizung");
                    object t = rs.Read("Typ");
                    if (t != null && t != DBNull.Value) wpTyp = t.ToString();
                }
                rs.Close();

                wp_model.Add(model);

                // K-3: einmaliger Hinweis, wenn eine bivalent-alternative Anlage mit der
                // Vorbelegung 0 °C als Bivalenztemperatur rechnet. Steht hier, weil der
                // Modulaufbau beide Rechenwege bedient und je Anlage genau einmal läuft.
                AlternativHinweisPruefen(model);

                // Bauart merken - sie entscheidet, ob die WQ_*-Konfiguration überhaupt
                // wirkt (siehe WPTypen); Auswertungen brauchen dieselbe Regel.
                wp_typ.Add(wpTyp ?? "");

                // Wärmequelle des Moduls: Luft-Wasser = Außenluft, sonst gemäß
                // WQ_Typ der Energieanlage (Fallback ist immer die Außenluft).
                wp_quelltemp.Add(WaermequelleClass.Quelltemperatur(wp_list[i], model.ID_Projekt, wpTyp, Temperatur));

                // Dient ein Pufferspeicher als Wärmequelle, muss die Quellwärme
                // tatsächlich aus diesem gedeckt werden (Bilanz je Stunde).
                SimulationPufferspeicher quellSp = WaermequelleClass.Quellspeicher(wp_list[i], wpTyp);
                // Zuordnung zur Energieanlage merken: sie bildet den technischen
                // Serienschlüssel QUELLE_<AnlagenID> der Anzeigen (Konzept 13.3).
                if (quellSp != null) quellSp.ID_Anlage = wp_list[i];
                wp_quellspeicher.Add(quellSp);

                // Wärmesenke des Moduls (Warmwasser und/oder Heizwärme)
                string senke = WaermequelleClass.WertLesen(wp_list[i], "WS_Typ") as string;
                wp_senke.Add(string.IsNullOrEmpty(senke) ? WaermequelleClass.SENKE_BEIDES : senke);

                // Betriebsmodus des Moduls (Laufzeit-/Leistungs-/PV-optimiert)
                string modus = WaermequelleClass.WertLesen(wp_list[i], "BM_Typ") as string;
                wp_modus.Add(string.IsNullOrEmpty(modus) ? WaermequelleClass.MODUS_LAUFZEIT : modus);

                _Kenndaten item = new _Kenndaten();
                item.Vorlauf = model.Vorlauf;
                item.ID_WP = model.ID_WP;

                RecordSet rsAnz = new RecordSet();
                rsAnz.Open("SELECT Count(*) FROM Tab_Kenndaten WHERE Tab_Kenndaten.ID_WP=" + model.ID_WP + " AND Tab_Kenndaten.Vorlauf=" + model.Vorlauf);
                rsAnz.Next();
                int anz = (int)rsAnz.Read(0);
                rsAnz.Close();

                // Absicherung (wichtig bei mehreren WPs im Projekt): Ohne Kenndaten
                // für den gewählten Vorlauf würde berechne_wptherm mit einem
                // Index-Fehler abstürzen. Stattdessen verständliche Meldung.
                //
                // PAKET 8 (Konzept 13.4): Die Meldung geht dialogfrei über den
                // Fehlerkanal statt über eine MessageBox. Der ABBRUCH ist unverändert
                // (return false an derselben Stelle) — nur der Meldeweg ist neu. Die
                // Oberfläche zeigt den Text nach dem Lauf als Dialog; im headless-Lauf
                // steht er in "out fehler".
                if (anz == 0)
                {
                    Cursor.Current = Cursors.Default;
                    Fehlertext = string.Format(MyResource.Resource.SIMENG_WP_KEINE_KENNDATEN,
                                               model.Bezeichner, model.Vorlauf);
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE + Fehlertext);
                    return false;
                }

                item.dat = new _DAT[anz];
                item.anz = anz; 

                rs.Open("SELECT * FROM Tab_Kenndaten WHERE ID_WP=" + model.ID_WP + " AND Vorlauf=" + model.Vorlauf + " order by Temperatur DESC");

                int index = 0;
                while (rs.Next())
                {
                    _DAT dat = new _DAT();
                    dat.COP = (double)rs.Read("COP");
                    dat.Temperatur = (int)rs.Read("Temperatur");
                    dat.Leistung = (double)rs.Read("Ptherm");
                    item.dat[index++] = dat;
                }
                rs.Close();

                wp_kenndaten.Add(item);
            }

            return true;
        }

        // PAKET A1: Hier stand "Berechnung_Stundenschleife" - die EINKANALIGE
        // Stundenschleife der Waermepumpen-Kaskade (rund 440 Zeilen: Speicher-Hysterese
        // ueber _speicherLaden, Aufteilung des Stundenbedarfs in rest_ww/rest_heiz,
        // Kennlinienauswertung je Modul, Heizstab, Bivalenzpunkt). Sie ist mit dem
        // Altpfad ersatzlos entfallen (Leitentscheidung L1); die Waermepumpe rechnet
        // ausschliesslich in der gemeinsamen Stundenschleife der Kaskadenschleife
        // (Zweikanalig_Start/-StundeStart/-Bedarfsphase/-Laden/Heizstabphase/-Ende).

        // ===================================================================
        // Dreikanaliger Rechenweg (Paket 4, Etappe 4b — Konzept 6.3; seit Paket A1
        // der einzige). Die Methodennamen tragen weiter den Zusatz „Zweikanalig",
        // mit dem sie in Etappe 4b hinter dem Feature-Flag entstanden sind.
        // ===================================================================

        /// <summary>
        /// Bereitet den Lauf vor: Modulaufbau (<see cref="ModuleAufbauen"/>), danach die
        /// Zusammenführung mehrfach benutzter Quellspeicher.
        ///
        /// Getrennt vom Rechenteil (<see cref="Zweikanalig_Start"/> und der
        /// Stundenkette der <c>Kaskadenschleife</c>), weil <c>SimulationControl</c>
        /// dazwischen die Speicher-Registry vervollständigen muss: Die Quellspeicher
        /// entstehen erst beim Modulaufbau, und die Registry ist die Menge, über die
        /// Phase G läuft.
        ///
        /// <para>K2-O9: Hier stand ein Verweis auf <c>Berechnung_Zweikanalig</c> — eine
        /// Methode, die das WP-Modul nie hatte (die drei anderen Erzeuger haben sie als
        /// Vektorstufe). Der Einstieg der Wärmepumpe ist die Stundenkette.</para>
        /// </summary>
        public bool Vorbereiten_Zweikanalig()
        {
            if (wp_list.Count >= MAX_WP) return false;

            Cursor.Current = Cursors.WaitCursor;

            if (!ModuleAufbauen()) return false;

            QuellspeicherZusammenfuehren();
            return true;
        }

        /// <summary>
        /// Mehrere Module am SELBEN Quellpuffer teilen sich ab Etappe 4b EINE Instanz
        /// (Konzept 6.2/6.3, offener Punkt 4 aus Etappe 4a).
        ///
        /// Bis dahin baute <see cref="WaermequelleClass.Quellspeicher"/> je Modul ein
        /// eigenes Objekt. Zwei Module am selben Speicher rechneten damit gegen zwei
        /// getrennte Füllstände — jedes durfte die volle Quellwärme entnehmen —, und
        /// <c>StundeAbschliessen</c> zog die Bereitschaftsverluste doppelt ab. Beides ist
        /// mit der gemeinsamen Instanz und der zentralen Phase G behoben.
        ///
        /// Es gewinnt die Instanz des ERSTEN Moduls in Modulreihenfolge. Ihre nutzbare
        /// Kapazität folgt der Spreizung <c>WQ_Spreizung</c> jener Anlage; unterscheiden
        /// sich die Spreizungen zweier Anlagen am selben Speicher, ist das eine
        /// Konfigurationsfrage und keine Rechenfrage — der Speicher hat einen
        /// Betriebszustand. Der Fall wird protokolliert.
        /// </summary>
        private void QuellspeicherZusammenfuehren()
        {
            Dictionary<int, SimulationPufferspeicher> ersteInstanz =
                new Dictionary<int, SimulationPufferspeicher>();

            for (int i = 0; i < wp_quellspeicher.Count; i++)
            {
                SimulationPufferspeicher q = wp_quellspeicher[i];
                if (q == null || q.ID_Pufferspeicher <= 0) continue;

                SimulationPufferspeicher vorhanden;
                if (!ersteInstanz.TryGetValue(q.ID_Pufferspeicher, out vorhanden))
                {
                    ersteInstanz[q.ID_Pufferspeicher] = q;
                    continue;
                }

                // Protokollkanal-Nachzug: WARNUNG - die Quellparameter der übrigen
                // Anlagen an diesem Speicher bleiben UNWIRKSAM. Je Speicher einmal.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                                  "quellspeicher-mehrfach-" + q.ID_Pufferspeicher,
                                  "Quellspeicher " + q.ID_Pufferspeicher +
                                  " wird von mehreren Modulen benutzt — ab Etappe 4b rechnen sie " +
                                  "gegen EINEN Füllstand (Konzept 6.3). Maßgeblich bleiben die " +
                                  "Quellparameter der Anlage " + vorhanden.ID_Anlage + ".");
                wp_quellspeicher[i] = vorhanden;
            }
        }

        // ------------------------------------------------------------------
        // Zustand der zweikanaligen Stundenschleife (Paket 5)
        //
        // Bis Paket 4 waren das lokale Variablen von Berechnung_Zweikanalig. Seit
        // Paket 5 führt die Stundenschleife nicht mehr die Wärmepumpe, sondern die
        // KASKADE (Kaskadenschleife): Solarthermie und Heizkessel sind eigene Stufen
        // derselben Schleife geworden, und ein Projekt ohne Wärmepumpe (1017, 1018)
        // braucht die Schleife trotzdem. Deshalb liegen die Größen jetzt als Felder
        // beim Modul und die Phasen als Stundenschritte vor. Die ausgeführten
        // Anweisungen und ihre Reihenfolge sind gegenüber Paket 4 unverändert.
        // ------------------------------------------------------------------

        /// <summary>
        /// Anteil der WP-Produktion, der den Momentanbedarf in Phase B DIREKT gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit, Befund N2). Nur im zweikanaligen Weg gefüllt.
        ///
        /// <c>WP_Waermeproduktion_gesamt = Direktdeckung_gesamt + Speicherladung</c>. Die
        /// Größe ist die Basis des EIGENANTEILS der Wärmepumpe an der Bedarfsdeckung:
        /// Bis zur Nacharbeit bildete <c>SimulationRunner</c> ihn als
        /// „Stufeneingang − Rest nach der Stufe" — mit einem zweiten Erzeuger in der
        /// Speicherstufe enthielt das dessen Lieferung mit, und beide meldeten sie.
        /// </summary>
        public double Direktdeckung_gesamt = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        /// Gefüllt von <see cref="Kaskadenschleife"/>; mit genau EINEM Lader je Speicher
        /// ist es dessen gesamte bedarfsdeckende Entladung.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        // ------------------------------------------------------------------
        // KANALINDIZIERTE DECKUNGSBUCHFÜHRUNG (Paket K2, Konzept 4.4)
        //
        // Die drei Felder sind eine ZUSÄTZLICHE Aufschlüsselung der bereits
        // vorhandenen Skalare — nicht ihr Ersatz. Es gilt je Lauf
        //
        //   Σ Direktdeckung_Kanal[k]      == Direktdeckung_gesamt
        //   Σ Speicherentladung_Kanal[k]  == Speicherentladung_Anteil
        //   Σ Heizstab_Kanal[k]           == Heizstab_gesamt
        //
        // bis auf die Rundungsklasse der getrennten Kanalarithmetik (die Skalare
        // summieren EINEN double-Strom, die Kanalfelder Kanal.ANZAHL getrennte).
        // SimulationRunner und Form_Simulation_Detail lesen weiterhin die Skalare;
        // die Kanalfelder sind die Voraussetzung für die Deckungsgrade je Kanal
        // (Paket E1, Konzept 4.4).
        //
        // Der Kanal einer Buchung wird NICHT hier entschieden: Er ergibt sich aus
        // dem Kanal, von dem SenkeAbziehen tatsächlich abgezogen hat (gemessen über
        // die rest-Differenz, siehe Kanalabzug am Dateiende).
        // ------------------------------------------------------------------

        /// <summary>
        /// Direkt gedeckter Momentanbedarf je Kanal [kWh] (Phase B) — die
        /// Aufschlüsselung von <see cref="Direktdeckung_gesamt"/>.
        /// </summary>
        public double[] Direktdeckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Anteil dieses Erzeugers an der bedarfsdeckenden Speicherentladung je Kanal
        /// [kWh] — die Aufschlüsselung von <see cref="Speicherentladung_Anteil"/>.
        /// Gefüllt von der <see cref="Kaskadenschleife"/>, wie der Skalar selbst.
        /// </summary>
        public double[] Speicherentladung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Heizstabwärme je Kanal [kWh] (Phase F) — die Aufschlüsselung von
        /// <see cref="Heizstab_gesamt"/>.
        /// </summary>
        public double[] Heizstab_Kanal = new double[Kanal.ANZAHL];

        // ------------------------------------------------------------------
        // PAKET E2 (Nachtrag zu Konzept 4.4) — DIESELBEN DREI GRÖSSEN ALS GANGLINIE.
        //
        // Sie werden an genau denselben Stellen gebucht wie die Jahressummen darüber,
        // aus derselben Variablen und im selben Schleifendurchlauf. Es gilt also je
        // Kanal k
        //
        //   Σ_h Direktdeckung_KanalStuendlich[k][h]      == Direktdeckung_Kanal[k]
        //   Σ_h Speicherentladung_KanalStuendlich[k][h]  == Speicherentladung_Kanal[k]
        //   Σ_h Heizstab_KanalStuendlich[k][h]           == Heizstab_Kanal[k]
        //
        // bis auf die Assoziativität der double-Addition (die Jahressumme läuft in EINEN
        // Akkumulator, die Ganglinie in 8760). Sie sind die Datengrundlage der
        // Kanalauswahl im Ergebnis-Diagramm der Detailansicht.
        // ------------------------------------------------------------------

        /// <summary>Stundenfassung von <see cref="Direktdeckung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Direktdeckung_KanalStuendlich = new Kanalganglinie();

        /// <summary>Stundenfassung von <see cref="Speicherentladung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Speicherentladung_KanalStuendlich = new Kanalganglinie();

        /// <summary>Stundenfassung von <see cref="Heizstab_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Heizstab_KanalStuendlich = new Kanalganglinie();

        private int _zkModule = 0;
        /// <summary>
        /// Ladeaufträge je Modul, RANG AUFSTEIGEND (Paket S1, Konzept 5.2) — an der
        /// Stelle des früheren Paares <c>_zkHauptauftrag</c>/<c>_zkZweitauftrag</c>.
        ///
        /// Die Ladephasen arbeiten die kaskadenübergreifende Ordnung ab; diese Listen
        /// braucht die BEDARFSPHASE: Sie prüft, ob ein Modul überhaupt noch etwas
        /// unterbringen kann (<c>Ladefaehig</c>), und sie nimmt bei einer reinen
        /// Ladeanlage den Bilanzraum der ERSTEN Puffersenke als Bezugsgröße.
        /// </summary>
        private List<Ladeauftrag>[] _zkAuftraege = new List<Ladeauftrag>[0];
        private double[] _zkLadeTherm = new double[0];   // Ladepotenzial der Stunde [kWh]
        private double[] _zkLadeEl = new double[0];      // zugehörige Stromaufnahme [kWh]
        private double[] _zkLadeRest = new double[0];    // davon noch nicht verbraucht
        private bool[] _zkPvGebunden = new bool[0];      // Modul im Betriebsmodus PV (13.5)

        /// <summary>
        /// K2: Kanalsplit der Direktdeckung EINER Moduliteration [kWh] — Zwischenablage
        /// zwischen dem Abzug und der Buchung in <see cref="Direktdeckung_Kanal"/>.
        ///
        /// Nötig, weil die Nacharbeit E-K1-3 die gerade gebuchte Direktdeckung anteilig
        /// wieder zurücknimmt (Quellwärme aus einem Kaskadenpuffer gehört dessen Lader).
        /// Auf dem Skalar ist das ein einfaches <c>-=</c>; der Kanalsplit derselben
        /// Buchung muss dieselbe Rücknahme erfahren, sonst liefe die Zusage
        /// „Σ Kanal == Skalar" auseinander. Zurückgenommen wird PROPORTIONAL zu der
        /// Buchung, die gerade erfolgt ist — das ist keine neue Verteilregel, sondern die
        /// Umkehrung EINER Buchung mit bekanntem Kanalsplit.
        /// </summary>
        private readonly double[] _deckungIteration = new double[Kanal.ANZAHL];

        /// <summary>
        /// Beginn des zweikanaligen Laufs: Eingangsgrößen festhalten, Zähler nullen,
        /// Senkenspeicher zurücksetzen und die Ladeaufträge je Modul auflösen.
        ///
        /// Voraussetzung: <see cref="Vorbereiten_Zweikanalig"/> ist gelaufen und
        /// <paramref name="kontext"/> ist aufgebaut.
        /// </summary>
        public bool Zweikanalig_Start(Kanalsatz kanaele, Kaskadenkontext kontext)
        {
            if (kanaele == null || kontext == null) return false;

            // Eingangsgrößen als EIGENE Vektoren festhalten (kein Aliasing auf die
            // Kanäle, die gleich fortgeschrieben werden — B0-2). PAKET K2: Der
            // Gesamtbedarf ist die Summe ALLER Kanäle; Bezug des Warmwasserbedarfs ist
            // der Brauchwasserkanal.
            Waermebedarf_stuendlich = kanaele.Summe();
            Warmwasserbedarf_stuendlich = (float[])kanaele.Brauchwasser.Clone();

            WP_Strombedarf_gesamt = 0;
            WP_Waermeproduktion_gesamt = 0;
            Heizstab_gesamt = 0;
            WP_Laufzeit = 0;
            Bivalenzpunkt = -100;

            // K2: die Kanalaufschlüsselung derselben Größen mit auf den Laufanfang.
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Heizstab_Kanal, 0, Kanal.ANZAHL);

            // E2: und ihre Ganglinienfassung, an derselben Stelle.
            Direktdeckung_KanalStuendlich.Nullen();
            Speicherentladung_KanalStuendlich.Nullen();
            Heizstab_KanalStuendlich.Nullen();

            // Senkenspeicher auf den Laufanfang. QUELLspeicher NICHT: sie starten
            // gefüllt (WaermequelleClass.Quellspeicher setzt SOC = Q_max), ein Reset
            // würde die vorhandene Wärmequelle löschen.
            foreach (SimulationPufferspeicher sp in kontext.AlleSpeicher)
                if (sp != null && !sp.IstQuelle) sp.Reset();

            int module = wp_model.Count;
            _zkModule = module;

            // Ladeaufträge je Modul vorab auflösen — die Ladephase iteriert über die
            // Prioritätsordnung, die Bedarfsphase braucht dieselben Aufträge für die
            // Ladefähigkeit als Bezugsgröße.
            //
            // PAKET S1: eine LISTE je Modul statt zweier Slots, nach Rang sortiert. Bei
            // höchstens zwei Senken — jedes migrierte Bestandsprojekt — enthält sie genau
            // die bisherigen Aufträge in genau der bisherigen Reihenfolge.
            List<Ladeauftrag>[] auftraege = new List<Ladeauftrag>[module];
            for (int i = 0; i < module; i++) auftraege[i] = new List<Ladeauftrag>();

            foreach (Ladeauftrag a in kontext.LadenOhnePV)
            {
                if (a == null || a.Erzeugerart != ProjektPuffer.TYP_WP) continue;
                if (a.Modulindex < 0 || a.Modulindex >= module) continue;
                auftraege[a.Modulindex].Add(a);
            }

            for (int i = 0; i < module; i++)
                auftraege[i].Sort(delegate (Ladeauftrag a, Ladeauftrag b)
                {
                    return a.Rang.CompareTo(b.Rang);
                });

            _zkAuftraege = auftraege;
            _zkLadeTherm = new double[module];
            _zkLadeEl = new double[module];
            _zkLadeRest = new double[module];
            _zkPvGebunden = new bool[module];
            return true;
        }

        /// <summary>Stundenbeginn: Ganglinienwerte und Ladepotenziale der Stunde nullen.</summary>
        public void Zweikanalig_StundeStart(int stunde)
        {
            WP_Strombedarf_stuendlich[stunde] = 0;
            WP_Waermeproduktion_stuendlich[stunde] = 0;
            waermerestbedarf_stuendlich[stunde] = 0;
            Heizstab_stuendlich[stunde] = 0;

            Array.Clear(_zkLadeTherm, 0, _zkModule);
            Array.Clear(_zkLadeEl, 0, _zkModule);
            Array.Clear(_zkLadeRest, 0, _zkModule);
            Array.Clear(_zkPvGebunden, 0, _zkModule);
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Wärmepumpen-Module:
        /// Bedarfsdeckung in Anlagenpriorität, Kennlinie, Betriebsarten, Sperrzeiten,
        /// Quellbegrenzung und die Bestimmung des Ladepotenzials der Stunde.
        ///
        /// <para>PAKET K2: <paramref name="rest"/> ist der offene Bedarf je Kanal
        /// (<see cref="Kanal.HEIZUNG"/>, <see cref="Kanal.BRAUCHWASSER"/>,
        /// <see cref="Kanal.PROZESS"/>) und tritt an die Stelle des Paares
        /// <c>ref rest_heiz, ref rest_ww</c>. Das Feld wird IN-PLACE fortgeschrieben —
        /// dieselbe Rolle, die vorher die <c>ref</c>-Parameter hatten.</para>
        /// </summary>
        /// <returns>false = Abbruch der Kennlinienauswertung.</returns>
        public bool Zweikanalig_Bedarfsphase(int stunde, Kaskadenkontext kontext,
                                             bool pvUeberschuss, double pvRest,
                                             double[] rest)
        {
                int module = _zkModule;
                List<Ladeauftrag>[] auftraege = _zkAuftraege;
                double[] ladeTherm = _zkLadeTherm;
                double[] ladeEl = _zkLadeEl;
                double[] ladeRest = _zkLadeRest;
                bool[] pvGebunden = _zkPvGebunden;

                for (int index = 0; index < module; index++)
                {
                    // D5a: In dieser Phase rechnen nur die Module der aktiven Rechenebene.
                    // Ohne Quellbezug auf einen geladenen Puffer steht jedes Modul auf
                    // Ebene 0, und die Prüfung ist immer wahr.
                    if (!EbeneAktiv(index)) continue;

                    WErzeugerModel model = wp_model[index];
                    _Kenndaten kenndaten = wp_kenndaten[index];
                    Senkenliste senken = kontext.SenkenlisteJeModul[index];
                    SimulationPufferspeicher quelle = wp_quellspeicher[index];

                    double[] result = berechne_wptherm(wp_quelltemp[index][stunde], model, kenndaten, index);
                    if (result[STATUS] == 0)
                    {
                        AbbruchAufraeumen();
                        return false;
                    }

                    // KANALGERECHTE BEZUGSGRÖSSE (Konzept 6.3): der offene Bedarf der
                    // eigenen DIREKTSENKEN-KETTE bzw. — bei einer reinen Ladeanlage — der
                    // Bilanzraum ihrer ersten Puffersenke (Ladefähigkeit + absehbare
                    // Entnahme, Nutzerentscheidung zu 4b-1).
                    double verfuegbar = Verfuegbar(senken, ErsterAuftrag(auftraege[index]),
                                                   rest, pvUeberschuss);

                    // Betriebsarten-Steuerung
                    if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_TEILPARALLEL)
                    {
                        if (Temperatur[stunde] <= model.Abschaltpunkt)
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_PARALLEL)
                    {
                        // die Wärmepumpe läuft weiter, der zweite Erzeuger unterstützt
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_ALTERNATIV)
                    {
                        // K-3: Die Umschaltung hängt an der Außentemperatur, nicht an
                        // einer Bezugsgröße der Stunde. Damit entfällt die Kanalfrage
                        // hier vollständig (siehe AlternativAus).
                        if (AlternativAus(model, stunde))
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }

                    // Sperrzeiten berücksichtigen
                    int std = stunde % 24;
                    if (std >= model.Sperrzeit_von && std < model.Sperrzeit_bis && model.Sperrung)
                    {
                        result[PTHERM] = 0;
                        result[PEL] = 0;
                    }

                    // Wärmequelle Pufferspeicher: die Verdampferwärme muss aus dem
                    // Speicher gedeckt werden (Regeneration steht oben, zentral).
                    if (quelle != null && result[PTHERM] > 0)
                    {
                        double quellAnteil = result[PTHERM] - result[PEL];
                        if (quellAnteil > 0 && quelle.SOC < quellAnteil)
                        {
                            double faktor = quelle.SOC / quellAnteil;
                            if (faktor < 0) faktor = 0;
                            result[PTHERM] *= faktor;
                            result[PEL] *= faktor;
                        }
                    }

                    // ABBRUCHBEDINGUNG (Konzept 6.3): aus „kein Bedarf" wird
                    // „kein Bedarf UND kein Ladepotenzial". Ein Modul mit Puffersenke muss
                    // auch dann laufen, wenn sein Kanal gerade nichts verlangt. Geprüft
                    // wird seit Paket S1 über ALLE seine Aufträge, nicht nur über zwei.
                    bool kannLaden = false;
                    for (int q = 0; q < auftraege[index].Count && !kannLaden; q++)
                        kannLaden = Ladefaehig(auftraege[index][q], pvUeberschuss, rest);

                    if (verfuegbar <= 0 && !kannLaden) continue;

                    // Betriebsmodus -> Ladepotenzial dieser Stunde
                    LadepotenzialBestimmen(index, senken, result, pvRest,
                                           ladeTherm, ladeEl, pvGebunden);

                    float vorherTherm = WP_Waermeproduktion_stuendlich[stunde];
                    float vorherEl = WP_Strombedarf_stuendlich[stunde];

                    // K2: Kanalsplit dieser Moduliteration leeren (siehe Feldkommentar).
                    Array.Clear(_deckungIteration, 0, Kanal.ANZAHL);

                    // Bedarfsdeckung NUR über DIREKTSENKEN. Eine Anlage ohne Direktsenke
                    // lädt ausschließlich (Ladephasen) — daraus folgt zusammen mit der
                    // sequenziellen Verteilung über die Senkenliste, dass jede kWh genau
                    // ein Ziel hat (Konzept 5.2).
                    if (senken != null && senken.HatDirektsenke && verfuegbar > 0)
                    {
                        if (result[PTHERM] < verfuegbar)
                        {
                            WP_Waermeproduktion_stuendlich[stunde] += (float)result[PTHERM];
                            WP_Waermeproduktion_gesamt += result[PTHERM];
                            WP_Strombedarf_stuendlich[stunde] += (float)result[PEL];
                            WP_Strombedarf_gesamt += result[PEL];
                            Modul_WP_Waermeproduktion[index] += result[PTHERM];
                            Modul_WP_Strombedarf[index] += result[PEL];

                            // B0-13 (aus der Parallelsitzung übernommen, Commit ae7b705):
                            // Laufzeit nur bei tatsächlicher Produktion - derselbe Guard
                            // wie im Teillast-Zweig und wie im Altpfad.
                            if (result[PTHERM] > 0)
                            {
                                WP_Laufzeit = WP_Laufzeit + 1;
                                Modul_WP_Laufzeit[index] += 1;
                            }

                            SenkeAbziehen(senken, result[PTHERM], rest, _deckungIteration);
                            Direktdeckung_gesamt += result[PTHERM];   // N2: Eigenanteil
                        }
                        else
                        {
                            WP_Waermeproduktion_stuendlich[stunde] += (float)verfuegbar;
                            WP_Waermeproduktion_gesamt += verfuegbar;
                            WP_Strombedarf_stuendlich[stunde] += (float)verfuegbar / (float)result[COP];
                            WP_Strombedarf_gesamt += verfuegbar / result[COP];
                            Modul_WP_Waermeproduktion[index] += verfuegbar;
                            Modul_WP_Strombedarf[index] += verfuegbar / result[COP];

                            // bei begrenzter Quelle bzw. Sperrzeit kann PTHERM 0 sein
                            if (result[PTHERM] > 0)
                            {
                                WP_Laufzeit = WP_Laufzeit + (verfuegbar / (float)result[PTHERM]);
                                Modul_WP_Laufzeit[index] += (verfuegbar / (float)result[PTHERM]);
                            }

                            SenkeAbziehen(senken, verfuegbar, rest, _deckungIteration);
                            Direktdeckung_gesamt += verfuegbar;       // N2: Eigenanteil
                        }
                    }

                    // Tatsächlich entnommene Quellwärme abziehen. StundeAbschliessen
                    // steht NICHT hier, sondern zentral in Phase G.
                    double erzeugt = WP_Waermeproduktion_stuendlich[stunde] - vorherTherm;
                    double strom = WP_Strombedarf_stuendlich[stunde] - vorherEl;
                    if (quelle != null)
                    {
                        double entnahme = erzeugt - strom;
                        // D5a: In der Kaskade ist der Quellpuffer ein SENKENspeicher mit
                        // Herkunftsanteilen — die Entnahme wird deshalb gemeldet (Ziel
                        // null = die Wärme deckt Bedarf).
                        if (entnahme > 0)
                        {
                            // PAKET E1: OHNE Kanalangabe — eine Quellentnahme trägt
                            // keinen Bedarfskanal und wird auf dem Heizkanal gebucht
                            // (Vorbelegung von Entladen, dieselbe Näherung wie
                            // Kaskadenschleife.Anteil_Entladen ohne Kanal).
                            double geliefert = quelle.Entladen(entnahme, stunde);
                            QuellentnahmeMelden(quelle, geliefert, null);

                            // NACHARBEIT E-K1-3 — SYMMETRIE ZUM KESSEL.
                            //
                            // In der KASKADE (der Quellpuffer ist ein Senkenspeicher mit
                            // Herkunftsanteilen) wird die entnommene Wärme über
                            // Anteil_Entladen dem Erzeuger gutgeschrieben, der den Puffer
                            // GELADEN hat. Bliebe die volle Erzeugung zugleich als eigene
                            // Direktdeckung stehen, wäre dieselbe kWh zweimal als Deckung
                            // ausgewiesen — genau der Fehler, den Befund N2 beseitigt hat.
                            // Der Wärmepumpe bleibt hier also nur ihr eigener Beitrag, die
                            // mit Strom erzeugte Anhebung; beim Kessel steht dafür
                            // _kesselStunde (nur „eigen").
                            //
                            // Der klassische QUELLSPEICHER (Erdreich, Grundwasser, eigene
                            // Puffer-Quellinstanz) ist ausgenommen: Er trägt keine
                            // Herkunftsanteile, Anteil_Entladen schreibt niemandem etwas
                            // gut, und ein Abzug wäre schlicht verlorene Deckung. Ohne
                            // Kaskade — jeder Bestandslauf — ist die Zeile wirkungslos.
                            if (!quelle.IstQuelle && geliefert > 0)
                            {
                                Direktdeckung_gesamt -= geliefert;
                                if (Direktdeckung_gesamt < 0) Direktdeckung_gesamt = 0;

                                // K2: dieselbe Rücknahme auf dem Kanalsplit derselben
                                // Buchung — proportional, damit Σ Kanal == Skalar bleibt.
                                DeckungIterationKuerzen(geliefert);
                            }
                        }
                    }

                    // K2: Kanalsplit dieser Moduliteration festschreiben — NACH der
                    // Korrektur E-K1-3, wie beim Skalar auch.
                    //
                    // PAKET E2: dieselbe Größe _deckungIteration[k], zusätzlich mit der
                    // Stunde indiziert. Eine Zeile neben der Jahressumme, aus demselben
                    // Wert — die Ganglinie kann von ihr nicht abweichen (Nachtrag zu
                    // Konzept 4.4).
                    for (int k = 0; k < Kanal.ANZAHL; k++)
                    {
                        Direktdeckung_Kanal[k] += _deckungIteration[k];
                        Direktdeckung_KanalStuendlich.Buchen(k, stunde, _deckungIteration[k]);
                    }

                    // Was vom Ladepotenzial nach der Bedarfsdeckung übrig ist, steht den
                    // Phasen C und D zur Verfügung.
                    ladeRest[index] = ladeTherm[index] - erzeugt;
                    if (ladeRest[index] < 0) ladeRest[index] = 0;

                } // end alle WP-Module

            return true;
        }

        /// <summary>
        /// Stundenende: der Restbedarf der Stunde nach allen Phasen — die Summe ÜBER ALLE
        /// Kanäle (K2). Ohne Prozesswärmeanteil ist das Zeichen für Zeichen die bisherige
        /// Größe <c>rest_heiz + rest_ww</c>.
        /// </summary>
        public void Zweikanalig_StundeEnde(int stunde, double[] rest)
        {
            waermerestbedarf_stuendlich[stunde] = (float)Kaskadenschleife.RestSumme(rest);
        }

        /// <summary>Abschluss des zweikanaligen Laufs: Sortierung, Jahressummen, Bivalenzpunkt.</summary>
        public void Zweikanalig_Ende(List<double> biv)
        {
            WPPlan.Core.BhkwPlan.Heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);

            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf_stuendlich, value => Waermebedarf_gesamt += value);
            waermerestbedarf_gesamt = 0;
            Array.ForEach(waermerestbedarf_stuendlich, value => waermerestbedarf_gesamt += value);

            Cursor.Current = Cursors.Default;

            // V0-9: Ende des Jahresdurchlaufs im zweikanaligen Weg - Kappungsstunden melden.
            KappungObenMelden();

            // PAKET B1 (F13): dieselbe Stelle für die Kappung nach unten am Booster.
            KappungUntenMelden();

            if (biv != null && biv.Count > 0)
                Bivalenzpunkt = biv.Max();
        }

        /// <summary>
        /// Bezugsgröße eines Moduls in dieser Stunde — die DREI Fälle aus Konzept 6.3:
        /// Warmwasserkanal, Heizkanal (bzw. beides mit Warmwasservorrang) und, dritter
        /// Fall, der BILANZRAUM der Hauptsenke bei einer Anlage, die einen Puffer lädt.
        ///
        /// Der dritte Fall stand bis zur Paket-4-Review auf der reinen Ladefähigkeit
        /// <c>Q_max · Obergrenze − SOC</c>. Damit drosselte der Speicherinhalt den
        /// Stundendurchsatz der Anlage (Befund 4b-1): Ein 600-l-Puffer ließ höchstens
        /// ~13 kWh/h durch, während der Momentanbedarf ein Vielfaches betragen kann.
        /// Fachlich ist ein Pufferspeicher aber eine hydraulische Weiche — er wird
        /// geladen, WÄHREND die Last aus ihm entnimmt. Nach der Nutzerentscheidung vom
        /// 14.08.2026 ist die Bezugsgröße deshalb
        /// <c>Ladefähigkeit + min(offener Kanalbedarf, Entnahmefähigkeit)</c>
        /// (<see cref="SimulationPufferspeicher.Bilanzraum"/>). Die Phasenstruktur bleibt
        /// unangetastet: Phase C lädt weiterhin ohne <c>SenkeAbziehen</c>, Phase E
        /// entlädt — nur darf die Aufnahme einer Stunde jetzt die freie Kapazität um die
        /// im selben Zeitschritt absehbare Entnahme übersteigen.
        ///
        /// <para>PAKET K2: Die drei WS_Typ-Fälle sind durch EINE Frage ersetzt — „wie viel
        /// offener Bedarf steht auf den Kanälen, die diese Senke bedient?". Beantwortet
        /// wird sie von <see cref="Kanalabzug.Offen(Senkenliste, double[])"/>, also von der
        /// EINEN Kanalregel (<c>Kaskadenschleife.SenkeAbziehen</c>) selbst. Die Maske hier
        /// ein zweites Mal zu bilden hieße, dieselbe Zuordnung an fünf Stellen zu führen —
        /// genau das, was Paket 5 mit <c>SenkeAbziehen</c> abgeschafft hat.</para>
        ///
        /// <para>PAKET S1: Gefragt wird nach der ganzen DIREKTSENKEN-KETTE (Konzept 5.2)
        /// statt nach einer Bedarfsart — eine Anlage kann mehrere Direktsenken haben, etwa
        /// Heizkreis auf Rang 1 und Prozesswärme auf Rang 2 (Migrationsregel R-Prozess).
        /// Der dritte Fall heißt jetzt „die Anlage hat GAR KEINE Direktsenke": Dann ist die
        /// Bezugsgröße der Bilanzraum ihrer ERSTEN Puffersenke.</para>
        ///
        /// <para>Der Fall <c>senken == null</c> ist kein Sonderfall: Ein
        /// <c>null</c>-Eintrag in <c>Kaskadenkontext.SenkenlisteJeModul</c> bedeutet
        /// ausdrücklich „Vorbelegung Heizkreis / Bedarfsart Beides", und genau so wird er
        /// gerechnet.</para>
        /// </summary>
        private double Verfuegbar(Senkenliste senken, Ladeauftrag ersterAuftrag,
                                  double[] rest, bool pvUeberschuss)
        {
            if (senken == null)
                return Kanalabzug.Offen(WaermequelleClass.SENKE_BEIDES, rest);

            if (!senken.HatDirektsenke)
            {
                if (ersterAuftrag == null || ersterAuftrag.Speicher == null) return 0;
                return ersterAuftrag.Speicher.Bilanzraum(
                    ersterAuftrag.ObergrenzeStunde(pvUeberschuss),
                    Kanalabzug.OffenFuerSpeicher(ersterAuftrag.Speicher, rest));
            }

            return Kanalabzug.Offen(senken, rest);
        }

        /// <summary>
        /// Der Ladeauftrag mit dem KLEINSTEN Rang eines Moduls (Paket S1) — die Nachfolge
        /// des früheren <c>_zkHauptauftrag[index]</c>. Die Listen sind schon sortiert
        /// (<see cref="Zweikanalig_Start"/>); <c>null</c> = die Anlage lädt nichts.
        /// </summary>
        private static Ladeauftrag ErsterAuftrag(List<Ladeauftrag> auftraege)
        {
            if (auftraege == null || auftraege.Count == 0) return null;
            return auftraege[0];
        }

        /// <summary>
        /// UMSCHALTKRITERIUM DES BIVALENT-ALTERNATIVEN BETRIEBS (K-3, Nutzerentscheidung
        /// vom 15.08.2026 zu <c>Konzept_KonfigUI_Hydraulik.md</c> Abschnitt 8).
        /// true = die Wärmepumpe ist in dieser Stunde abgeschaltet, der zweite Erzeuger
        /// übernimmt den Heizbetrieb allein.
        ///
        /// <para>
        /// Maßgeblich ist die AUSSENTEMPERATUR gegen die Bivalenztemperatur
        /// <c>Tab_Energieanlagen.Abschaltpunkt</c> — dieselbe Größe und dieselbe Spalte,
        /// die der teilparallele Zweig auswertet. Unterhalb der Bivalenztemperatur ist die
        /// Wärmepumpe aus; ab ihr läuft sie mit ihrer Leistung, und was sie in einer
        /// einzelnen Stunde nicht deckt, geht regulär an die nächste Kaskadenstufe.
        /// </para>
        ///
        /// <para>
        /// <b>Was das ablöst.</b> Bis zu dieser Änderung stand hier
        /// <c>if (result[PTHERM] &lt; Rest_waerme)</c> — die Wärmepumpe fiel in JEDER Stunde
        /// aus, die sie nicht vollständig deckte. Das ist keine Bivalenzregelung, sondern
        /// eine Leistungsprüfung: Sie traf einzelne Sommer-Warmwasserspitzen genauso wie
        /// Frosttage, erzeugte stündliches Pendeln zwischen Wärmepumpe und Kessel und
        /// hing an der Bedarfsganglinie statt am Außenklima. Der gepflegte
        /// <c>Abschaltpunkt</c> blieb dabei wirkungslos.
        /// </para>
        ///
        /// <para>
        /// <b>Bezugsgröße entfällt.</b> Im zweikanaligen Weg beantwortete
        /// <c>AlternativBezug</c> bis hierher die Frage, GEGEN WELCHEN Bedarf verglichen
        /// wird (Kanalbedarf statt Bilanzraum des Speichers, Paket-4-Review Punkt 1).
        /// Mit dem Temperaturkriterium stellt sich die Frage nicht mehr — verglichen wird
        /// Temperatur gegen Temperatur —, deshalb ist die Methode entfallen. Der Vorteil
        /// ist zugleich der Kern der Entscheidung: Die Abschaltung hängt weder am
        /// Momentanbedarf noch am Füllstand eines Puffers.
        /// </para>
        ///
        /// <para>
        /// <b>Vergleichsgrenze.</b> Abgeschaltet wird bei ECHTER Unterschreitung
        /// (<c>&lt;</c>); bei genau der Bivalenztemperatur läuft die Wärmepumpe noch. Der
        /// teilparallele Zweig schaltet dagegen schon bei <c>&lt;=</c> ab. Der Unterschied
        /// betrifft ausschließlich die Stunden mit exakter Gleichheit; der Teilparallel-Zweig
        /// bleibt bewusst unverändert, weil er nicht Gegenstand von K-3 ist.
        /// </para>
        ///
        /// <para>
        /// <b>Der Spaltenwert gilt immer wörtlich, auch 0 °C.</b> <c>Abschaltpunkt</c> ist
        /// im Bestand in keiner Zeile NULL, und der Wizard schreibt über
        /// <c>double.Parse</c> stets einen Wert (Vorbelegung des Eingabefelds: 0). Ein
        /// „nicht gepflegt" ist im Datenmodell daher nicht von einer bewusst gewählten
        /// Bivalenztemperatur von 0 °C zu unterscheiden — und 0 °C ist ein plausibler,
        /// gebräuchlicher Wert. Eine Ersatzregel für 0 würde also genau die Anlagen
        /// falsch rechnen, die den Wert gewollt so führen. Stattdessen weist
        /// <see cref="AlternativHinweisPruefen"/> beim Modulaufbau einmalig darauf hin,
        /// dass der Vorbelegungswert wirksam ist.
        /// </para>
        /// </summary>
        private bool AlternativAus(WErzeugerModel model, int stunde)
        {
            return Temperatur[stunde] < model.Abschaltpunkt;
        }

        /// <summary>
        /// Einmaliger Hinweis beim Modulaufbau, wenn eine bivalent-alternative Anlage die
        /// Bivalenztemperatur auf dem Vorbelegungswert 0 °C führt (K-3).
        ///
        /// Rein informativ — die Rechnung ist davon unberührt, der Wert gilt wörtlich
        /// (siehe <see cref="AlternativAus"/>). Der Hinweis existiert, weil 0 °C zugleich
        /// der Wert ist, den eine nie geöffnete Eingabemaske hinterlässt: Wer die Anlage
        /// nur auf „Alternativbetrieb" gestellt hat, soll sehen, mit welcher
        /// Bivalenztemperatur gerechnet wurde.
        /// </summary>
        private void AlternativHinweisPruefen(WErzeugerModel model)
        {
            if (model == null) return;
            if (!model.Bivalenter_Betrieb) return;
            if (model.Betriebsart != DbWerte.WP_BETRIEBSART_ALTERNATIV) return;
            if (model.Abschaltpunkt != 0) return;

            SimulationProtokoll.Aktuell.HinweisEinmal(
                "wp-alternativ-bivalenztemperatur-0-" + model.ID,
                MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE +
                string.Format(MyResource.Resource.SIMENG_WP_BIVALENZTEMPERATUR_VORBELEGUNG,
                              model.Bezeichner));
        }

        /// <summary>
        /// true, wenn der Auftrag in dieser Stunde noch Wärme aufnehmen kann — Ladung in
        /// den Speicher ODER Durchsatz zur Last (Bilanzraum, siehe <see cref="Verfuegbar"/>).
        /// </summary>
        private static bool Ladefaehig(Ladeauftrag auftrag, bool pvUeberschuss, double[] rest)
        {
            if (auftrag == null || auftrag.Speicher == null) return false;

            return auftrag.Speicher.Bilanzraum(
                       auftrag.ObergrenzeStunde(pvUeberschuss),
                       Kanalabzug.OffenFuerSpeicher(auftrag.Speicher, rest)) > 0;
        }

        /// <summary>
        /// Ladepotenzial der Stunde je Modul nach Betriebsmodus (Konzept 3.5, 13.5).
        ///
        ///   Laufzeit  — volle Leistung, der Überschuss darf laden,
        ///   Leistung  — kein Überschuss; die WP moduliert exakt auf den Bedarf,
        ///   PV        — Überschuss nur, soweit PV-Strom verfügbar ist.
        ///
        /// DAS PV-BUDGET WIRD HIER NICHT VERBRAUCHT (Paket-4-Review, Punkt 2).
        /// <paramref name="pvRest"/> geht als OBERGRENZE ein, abgezogen wird es erst in
        /// der Ladephase, und zwar um die TATSÄCHLICH geladene Menge (13.5). Der Grund
        /// ist die Reihenfolge der Phasen: Das Potenzial aller Module steht in Phase B
        /// fest, geladen wird erst in C und D. Hätte Modul 1 sein Potenzial hier
        /// abgebucht, es dann aber nicht unterbringen können — weil sein Speicher voll
        /// ist oder gar keiner zugeordnet ist —, wäre das Budget für Modul 2 schon
        /// verbraucht, ohne dass eine kWh PV-Strom geflossen wäre. Nicht untergebrachtes
        /// Potenzial bleibt so dem nächsten Modul erhalten; die modulübergreifende
        /// Reihenfolge (Anlagenpriorität) bleibt dieselbe, weil die Ladephase die
        /// Aufträge in Ladeprioritätsordnung abarbeitet und dabei sequenziell abbucht.
        ///
        /// SONDERFALL REINE LADEANLAGE (keine Direktsenke): Dort IST die Ladung der
        /// Auftrag und nicht der Überschuss über einen Bedarf hinaus. „Leistung" hätte
        /// hier keine Bedeutung — der Bilanzraum begrenzt ohnehin —, deshalb gilt für
        /// diese Anlagen dieselbe Regel wie für „Laufzeit". Andernfalls stünde eine
        /// korrekt konfigurierte Speicheranlage still.
        /// </summary>
        private void LadepotenzialBestimmen(int index, Senkenliste senken, double[] result,
                                            double pvRest, double[] ladeTherm, double[] ladeEl,
                                            bool[] pvGebunden)
        {
            string modus = wp_modus[index];
            bool pufferSenke = senken != null && !senken.HatDirektsenke;

            if (modus == WaermequelleClass.MODUS_PV)
            {
                pvGebunden[index] = true;

                double copPV = result[COP] > 0 ? result[COP] : 1;
                double maxThermPV = pvRest * copPV;
                double thermPV = Math.Min(result[PTHERM], maxThermPV);
                if (thermPV > 0)
                {
                    ladeTherm[index] += thermPV;
                    ladeEl[index] += thermPV / copPV;
                }
                return;
            }

            if (modus == WaermequelleClass.MODUS_LEISTUNG && !pufferSenke)
                return;   // kein Ladepotenzial

            ladeTherm[index] += result[PTHERM];
            ladeEl[index] += result[PEL];
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.3): der Anweisungsblock, der bis
        /// Paket 4 im Rumpf von <c>Ladephase</c> stand. Die Schleife über die
        /// kaskadenübergreifende Prioritätsordnung führt seit Paket 5 die
        /// <see cref="Kaskadenschleife"/> — sie muss Wärmepumpen, Solarthermie und
        /// Heizkessel in EINER Ordnung abarbeiten, und die Ordnung gehört nicht in ein
        /// einzelnes Erzeugermodul. Die ausgeführten Anweisungen sind unverändert; aus
        /// jedem <c>continue</c> ist ein <c>return 0</c> geworden.
        ///
        /// KEIN <c>SenkeAbziehen</c> — die geladene Wärme deckt keinen Bedarf, sie liegt
        /// im Speicher. Genau hier entstünde sonst die Doppelzählung, die der
        /// Deckungsgrad in der Detailansicht durch seine 100-%-Kappung verstecken würde.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss,
                                        double[] absehbar, ref double pvRest)
        {
                double[] ladeTherm = _zkLadeTherm;
                double[] ladeEl = _zkLadeEl;
                double[] ladeRest = _zkLadeRest;
                bool[] pvGebunden = _zkPvGebunden;

                if (a == null) return 0;

                int index = a.Modulindex;
                if (index < 0 || index >= ladeRest.Length) return 0;
                if (ladeRest[index] <= 0) return 0;

                SimulationPufferspeicher sp = a.Speicher;
                if (sp == null) return 0;

                // BILANZRAUM statt reiner Ladefähigkeit (Nutzerentscheidung zu 4b-1):
                // Was in dieser Stunde ohnehin wieder entnommen wird, darf der Speicher
                // zusätzlich aufnehmen — er ist eine hydraulische Weiche. Das Budget je
                // Kanal wird nur einmal vergeben (absehbar), damit zwei Speicher desselben
                // Kanals nicht dieselbe Entnahme doppelt durchreichen.
                // D5a: Beim KOMBISPEICHER ist das Budget die Summe beider Kanäle; die
                // gemeinsame Fassung steht in der Kaskadenschleife (DurchlassBudget /
                // DurchlassBuchen) und liefert ohne Kombispeicher Anweisung für Anweisung
                // das Bisherige.
                double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
                double durchlass = Kaskadenschleife.DurchlassBudget(sp, absehbar);
                if (ladefaehig + durchlass <= 0) return 0;

                double menge = Math.Min(ladeRest[index], ladefaehig + durchlass);

                // Stromseite über den mittleren COP des Ladepotenzials — dieselbe
                // Verbuchung wie im Bestand (dort copMittel aus dem Modulsummen-Paar).
                double cop = ladeEl[index] > 0 ? ladeTherm[index] / ladeEl[index] : 0;
                if (cop <= 0) return 0;

                // PV-BUDGET (13.5, Paket-4-Review Punkt 2): Ein Modul im Betriebsmodus PV
                // lädt nur, soweit PV-Strom übrig ist. Abgebucht wird die tatsächlich
                // geladene Menge — weiter unten, nach sp.Laden.
                if (pvGebunden[index])
                {
                    double maxTherm = pvRest * cop;
                    if (menge > maxTherm) menge = maxTherm;
                }

                // QUELLBILANZ AUCH BEIM LADEN (Konzept 6.3, Prüfbefund): Die
                // Speicherladung entnimmt der Quelle heute keine Wärme — im
                // zweikanaligen Weg tut sie es, und die Quelle begrenzt die Ladung.
                SimulationPufferspeicher quelle = wp_quellspeicher[index];
                if (quelle != null && quelle.Q_max > 0 && cop > 1)
                {
                    double entnahmeVoll = menge * (1.0 - 1.0 / cop);
                    if (entnahmeVoll > quelle.SOC)
                    {
                        double faktor = entnahmeVoll > 0 ? quelle.SOC / entnahmeVoll : 0;
                        if (faktor < 0) faktor = 0;
                        menge *= faktor;
                    }
                }
                if (menge <= 0) return 0;

                double ladung = sp.Laden(menge, stunde, durchlass);
                if (ladung <= 0) return 0;

                // Verbrauchtes Durchsatzbudget des Kanals abbuchen: alles, was über die
                // Ladefähigkeit hinausging, ist die Menge, die Phase E wieder entnimmt.
                double genutzterDurchlass = ladung - ladefaehig;
                if (genutzterDurchlass > 0)
                    Kaskadenschleife.DurchlassBuchen(sp, absehbar, genutzterDurchlass);

                if (pvGebunden[index])
                {
                    pvRest -= ladung / cop;
                    if (pvRest < 0) pvRest = 0;
                }

                ladeRest[index] -= ladung;
                if (ladeRest[index] < 0) ladeRest[index] = 0;

                WP_Waermeproduktion_stuendlich[stunde] += (float)ladung;
                WP_Waermeproduktion_gesamt += ladung;
                Modul_WP_Waermeproduktion[index] += ladung;

                double strom = ladung / cop;
                WP_Strombedarf_stuendlich[stunde] += (float)strom;
                WP_Strombedarf_gesamt += strom;
                Modul_WP_Strombedarf[index] += strom;

                if (ladeTherm[index] > 0)
                {
                    WP_Laufzeit += ladung / ladeTherm[index];
                    Modul_WP_Laufzeit[index] += ladung / ladeTherm[index];
                }

                if (quelle != null)
                {
                    double entnahme = ladung - strom;
                    // D5a: Ziel = der geladene Speicher — die Wärme hat nur den Speicher
                    // gewechselt, ihre Herkunft wandert mit (Anteil_Umbuchen).
                    // PAKET E1: OHNE Kanalangabe (Heizkanal-Vorbelegung, siehe oben).
                    if (entnahme > 0) QuellentnahmeMelden(quelle, quelle.Entladen(entnahme, stunde), sp);
                }

                return ladung;
        }

        /// <summary>
        /// Phase F: Heizstab auf den Kanalrest, mit der Semantik des Bestands — er sieht
        /// den AGGREGIERTEN Rest und ist je Modul auf <c>Tab_WP.Heizung</c> begrenzt.
        /// Die Aufteilung auf die Kanäle folgt dem Warmwasservorrang von
        /// <c>SENKE_BEIDES</c>; die Additionslogik ist die aus B0-5 (<c>+=</c> statt
        /// <c>=</c>, sonst überschreiben sich die Modulbeiträge in der Ganglinie).
        ///
        /// <para>PAKET K2: Der aggregierte Rest ist der offene Bedarf der Kanäle, die
        /// <c>SENKE_BEIDES</c> bedient — dieselbe Größe, gegen die gleich abgezogen wird,
        /// und deshalb aus derselben Quelle (<see cref="Kanalabzug.Offen"/>) statt aus
        /// einer zweiten Summenbildung. <see cref="Heizstab_Kanal"/> nimmt die
        /// Aufschlüsselung des Abzugs auf.</para>
        /// </summary>
        public void Heizstabphase(int stunde, double[] rest)
        {
            if (!Mit_Heizstab) return;

            for (int index = 0; index < wp_model.Count; index++)
            {
                double offen = Kanalabzug.Offen(WaermequelleClass.SENKE_BEIDES, rest);
                if (offen <= 0) break;
                if (WP_Heizung[index] <= 0) continue;

                double menge = Math.Min(offen, WP_Heizung[index]);
                Heizstab_stuendlich[stunde] += (float)menge;
                Heizstab_gesamt += menge;
                Modul_Heizstab[index] += menge;

                // PAKET E2: derselbe Abzug schreibt zusätzlich die Kanalganglinie des
                // Heizstabs — gemessen an derselben rest-Differenz wie Heizstab_Kanal.
                SenkeAbziehen(WaermequelleClass.SENKE_BEIDES, menge, rest, Heizstab_Kanal,
                              Heizstab_KanalStuendlich, stunde);
            }
        }

        /// <summary>
        /// Aufräumen beim Abbruch der Kennlinienauswertung, damit ein abgebrochener Lauf
        /// keine Teilergebnisse hinterlässt.
        /// </summary>
        private void AbbruchAufraeumen()
        {
            for (int i = 0; i < MAX_WP; i++)
            {
                Modul_WP_Strombedarf[i] = 0;
                Modul_WP_Waermeproduktion[i] = 0;
                Modul_Heizstab[i] = 0;
                Modul_WP_Laufzeit[i] = 0;
                WP_Modul[i] = "";
            }
            WP_Waermeproduktion_gesamt = 0;
            WP_Strombedarf_gesamt = 0;
            Heizstab_gesamt = 0;
            Cursor.Current = Cursors.Default;
        }

        // PAKET A1: Hier stand die Durchreichung
        // „SenkeAbziehen(string, double, ref double, ref double)" auf die
        // ref-Fassung in Kaskadenschleife. Sie hatte nur Aufrufer in der einkanaligen
        // Stundenschleife und ist mit ihr entfallen - zusammen mit der ref-Fassung
        // selbst.

        /// <summary>
        /// KANALINDIZIERTE Fassung (Paket K2) — dieselbe Regel, aber auf dem Kanalfeld
        /// <paramref name="rest"/>, und mit der Aufschlüsselung des Abzugs nach Kanälen.
        ///
        /// <paramref name="jeKanal"/> nimmt die tatsächlich abgezogenen Beträge je Kanal
        /// auf (<c>+=</c>). Sie werden GEMESSEN — aus der Differenz von
        /// <paramref name="rest"/> vor und nach dem Abzug —, nicht nach einer eigenen
        /// Regel verteilt: Welcher Kanal wie viel abgibt, entscheidet allein
        /// <see cref="Kaskadenschleife.SenkeAbziehen(string, double, double[])"/>
        /// (Kanalmaske und Knappheitsreihenfolge, Konzept 4.3).
        /// </summary>
        private void SenkeAbziehen(string senke, double menge, double[] rest, double[] jeKanal)
        {
            Kanalabzug.Abziehen(senke, menge, rest, jeKanal);
        }

        /// <summary>
        /// PAKET E2 — dieselbe Durchreichung, zusätzlich mit der Kanalganglinie der
        /// Stunde (Nachtrag zu Konzept 4.4).
        /// </summary>
        private void SenkeAbziehen(string senke, double menge, double[] rest, double[] jeKanal,
                                   Kanalganglinie ganglinie, int stunde)
        {
            Kanalabzug.Abziehen(senke, menge, rest, jeKanal, ganglinie, stunde);
        }

        /// <summary>
        /// SENKENLISTEN-Fassung (Paket S1, Konzept 5.2) — der Abzug läuft über die
        /// DIREKTSENKEN-KETTE der Anlage in Rangfolge statt über eine einzelne
        /// Bedarfsart. Aufschlüsselung wie oben: gemessen, nicht zweitgerechnet.
        /// </summary>
        private void SenkeAbziehen(Senkenliste senken, double menge, double[] rest,
                                   double[] jeKanal)
        {
            Kanalabzug.Abziehen(senken, menge, rest, jeKanal);
        }

        /// <summary>
        /// Nimmt <paramref name="menge"/> von der Direktdeckung DIESER Moduliteration
        /// zurück — die Kanalfassung der Korrektur E-K1-3 (Quellwärme aus einem
        /// Kaskadenpuffer gehört dessen Lader, nicht der Wärmepumpe).
        ///
        /// Zurückgenommen wird PROPORTIONAL zum Kanalsplit derselben Buchung. Das ist
        /// keine neue Verteilregel: Es ist die Umkehrung genau einer Buchung, deren
        /// Kanalanteile bekannt sind. Konstruktiv gilt
        /// <c>menge ≤ Σ _deckungIteration</c> (die Quellentnahme kann die erzeugte und
        /// gebuchte Wärme nicht übersteigen); der Faktor wird trotzdem bei 0 geklemmt,
        /// wie der Skalar auch.
        /// </summary>
        private void DeckungIterationKuerzen(double menge)
        {
            double summe = 0;
            for (int k = 0; k < Kanal.ANZAHL; k++) summe += _deckungIteration[k];
            if (summe <= 0) return;

            double faktor = (summe - menge) / summe;
            if (faktor < 0) faktor = 0;
            if (faktor >= 1) return;

            for (int k = 0; k < Kanal.ANZAHL; k++) _deckungIteration[k] *= faktor;
        }

        /// <summary>
        /// V0-9 (F13): meldet die Kennlinienkappung nach OBEN — je Modul einmal je Lauf,
        /// mit Anlagenbezeichnung, Zahl der gekappten Stunden und der obersten
        /// Stützstellen-Temperatur.
        ///
        /// Gerufen am Ende des Jahresdurchlaufs, in <see cref="Zweikanalig_Ende"/>.
        ///
        /// Der Einmal-Schlüssel ist sprachneutral (Schicht 2) und trägt den Modulindex:
        /// Zwei gleichnamige Anlagen derselben Kaskade sollen beide gemeldet werden.
        /// </summary>
        private void KappungObenMelden()
        {
            for (int i = 0; i < wp_model.Count && i < MAX_WP; i++)
            {
                if (Modul_Kappung_Oben[i] <= 0) continue;
                if (i >= wp_kenndaten.Count || wp_kenndaten[i] == null || wp_kenndaten[i].anz < 1) continue;

                string bezeichner = (wp_model[i] != null && wp_model[i].Bezeichner != null)
                                    ? wp_model[i].Bezeichner : "";
                string obergrenze = wp_kenndaten[i].dat[0].Temperatur.ToString("F1");

                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "WP_Kappung_Oben_" + i + "_" + bezeichner,
                    string.Format(MyResource.Resource.SIMENG_WP_KAPPUNG_OBEN_HINWEIS,
                                  bezeichner, Modul_Kappung_Oben[i], obergrenze));
            }
        }

        /// <summary>
        /// PAKET B1 (F13): meldet die Kennlinienkappung nach UNTEN — je Modul einmal je
        /// Lauf, mit Anlagenbezeichnung, Zahl der gekappten Stunden, der untersten
        /// Stützstelle und dem TEMPERATURBEREICH der Unterschreitung.
        ///
        /// <para>Der Bereich steht dabei bewusst im Text: „400 Stunden unter −5 °C" ist
        /// eine ganz andere Aussage, je nachdem, ob die Quelle dabei bei −5,2 °C oder bei
        /// −18 °C lag. Er sagt dem Anwender, wie weit die Kappung trägt und ob ein
        /// Kennfeld mit tieferen Stützstellen etwas ändern würde.</para>
        ///
        /// <para>Gerufen an derselben Stelle wie <see cref="KappungObenMelden"/>
        /// (<see cref="Zweikanalig_Ende"/>). Ohne gekoppeltes Modul steht der Zähler auf
        /// 0 und die Methode meldet nichts.</para>
        /// </summary>
        private void KappungUntenMelden()
        {
            for (int i = 0; i < wp_model.Count && i < MAX_WP; i++)
            {
                if (Modul_Kappung_Unten[i] <= 0) continue;
                if (i >= wp_kenndaten.Count || wp_kenndaten[i] == null || wp_kenndaten[i].anz < 1) continue;

                string bezeichner = (wp_model[i] != null && wp_model[i].Bezeichner != null)
                                    ? wp_model[i].Bezeichner : "";
                string untergrenze =
                    wp_kenndaten[i].dat[wp_kenndaten[i].anz - 1].Temperatur.ToString("F1");

                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "WP_Kappung_Unten_" + i + "_" + bezeichner,
                    string.Format(MyResource.Resource.SIMENG_WP_KAPPUNG_UNTEN_HINWEIS,
                                  bezeichner, Modul_Kappung_Unten[i], untergrenze,
                                  Modul_Kappung_Unten_Min[i].ToString("F1"),
                                  Modul_Kappung_Unten_Max[i].ToString("F1")));
            }
        }

        // V0-9: Der Parameter "index" ist der Modulindex innerhalb der Kaskade. Er wird
        // ausschließlich für den Kappungszähler Modul_Kappung_Oben gebraucht und geht in
        // die Rechnung nicht ein; Zweikanalig_Bedarfsphase führt ihn ohnehin als
        // Schleifenvariable.
        double[] berechne_wptherm(float temperatur, WErzeugerModel model, _Kenndaten kenndaten, int index)
        {

            double[] result = new double[4] { 0, 0, 0, 0 };
            double wptherm = 0;
            double cop = 0, ptherm = 0, pel = 0;
            double cop_maxSST = 0, t_maxSST = 0, ptherm_maxSST  = 0;
            int maxsst = kenndaten.anz; 

            cop_maxSST = kenndaten.dat[0].COP;
            t_maxSST = kenndaten.dat[0].Temperatur;
            ptherm_maxSST = kenndaten.dat[0].Leistung;

            if (kenndaten.anz < 2)
            {
                result[0] = 1;
                result[1] = cop_maxSST;
                result[2] = ptherm_maxSST;
                result[3] = ptherm_maxSST / cop_maxSST; // pel
                return result;   
            }


            if (temperatur >= t_maxSST)
            {
                // V0-9 (F13): Kappung auf die oberste Stützstelle - unverändert, aber ab
                // jetzt gezählt. Nur der ECHTE Überschreitungsfall wird gezählt; bei
                // Gleichstand liefert die Stützstelle den exakten Wert, das ist keine
                // Kappung. Gemeldet wird einmal am Ende des Laufs, siehe
                // KappungObenMelden. Die Rechnung darunter ist unberührt.
                if (temperatur > t_maxSST && index >= 0 && index < MAX_WP) Modul_Kappung_Oben[index]++;

                // t grösser als max sst der Kennlinie
                cop = cop_maxSST;
                ptherm = ptherm_maxSST;
                pel = ptherm / cop;
                wptherm = ptherm;
            }
            else
            {
                if (temperatur < kenndaten.dat[kenndaten.anz - 1].Temperatur)
                {
                    // ---------------------------------------------------------------
                    // PAKET B1 (Entscheidung F13) — KAPPUNG NACH UNTEN für die
                    // TEMPERATURGEKOPPELTE Pufferquelle, symmetrisch zur bestehenden
                    // Kappung nach oben (V0-9).
                    //
                    // Am Booster heißt eine Unterschreitung der untersten Stützstelle
                    // nicht „das Gerät läuft bei Extremkälte", sondern „der Quellpuffer
                    // steht gerade auf Rücklaufniveau". Eine lineare Verlängerung der
                    // Herstellerkennlinie in diesen Bereich wäre eine erfundene
                    // Betriebskennzahl; das Konzept entscheidet deshalb ausdrücklich auf
                    // Kappung — konservativ, mit Protokoll (Konzept 8.2, F13).
                    //
                    // Für ALLE ÜBRIGEN Quellen (Außenluft, Erdreich, Profil, CSV,
                    // konstante Temperatur, eigenständiger Quellspeicher) bleibt der
                    // Bestandsweg darunter unverändert — dort ist die
                    // Projekteinstellung Extrapolation_erlaubt maßgeblich.
                    // ---------------------------------------------------------------
                    if (QuelleGekoppelt(index))
                    {
                        if (index >= 0 && index < MAX_WP)
                        {
                            Modul_Kappung_Unten[index]++;
                            if (temperatur < Modul_Kappung_Unten_Min[index])
                                Modul_Kappung_Unten_Min[index] = temperatur;
                            if (temperatur > Modul_Kappung_Unten_Max[index])
                                Modul_Kappung_Unten_Max[index] = temperatur;
                        }

                        cop = kenndaten.dat[kenndaten.anz - 1].COP;
                        ptherm = kenndaten.dat[kenndaten.anz - 1].Leistung;
                        pel = (cop != 0) ? ptherm / cop : 0;
                        wptherm = ptherm;

                        result[0] = 1;
                        result[1] = cop;
                        result[2] = ptherm;
                        result[3] = pel;
                        return result;
                    }

                    // PAKET 8 (Konzept 13.4) — die EINZIGE echte Interaktion der Engine
                    // ist zur Vorab-Einstellung geworden.
                    //
                    // BISHER: MessageBox mitten in der Stundenschleife, „soll
                    // extrapoliert werden? Bei nein wird Simulation abgebrochen!". Jeder
                    // unbeaufsichtigte Lauf blieb daran hängen; die Referenzlauf-Suite
                    // musste einen Dialogwächter mitlaufen lassen, der "Ja" drückte.
                    //
                    // JETZT: Tab_Einstellungen.Extrapolation_erlaubt entscheidet vorab.
                    //   erlaubt (Vorbelegung, entspricht dem bisherigen "Ja"):
                    //       es wird extrapoliert wie bisher - Zeile für Zeile derselbe
                    //       Rechenweg - und der Lauf vermerkt es EINMAL im Protokoll.
                    //       Damit ist der Grenzfall erstmals sichtbar statt stumm.
                    //   verboten:
                    //       Abbruch über den Fehlerkanal, mit demselben Ausgang wie das
                    //       bisherige "Nein" (result[STATUS] = 0), aber mit sprechendem
                    //       Text statt einer Dialogantwort.
                    //
                    // Die Meldung steht in beiden Zweigen hinter dem extrapolation-Flag
                    // bzw. hinter HinweisEinmal: Der Fall tritt je Modul in bis zu 8760
                    // Stunden auf.
                    if (!extrapolation)
                    {
                        string bezeichner = (model != null && model.Bezeichner != null) ? model.Bezeichner : "";
                        string untergrenze = kenndaten.dat[kenndaten.anz - 1].Temperatur.ToString("F1");

                        if (!Extrapolation_Erlaubt)
                        {
                            Fehlertext = string.Format(
                                MyResource.Resource.SIMENG_WP_EXTRAPOLATION_VERBOTEN,
                                bezeichner, untergrenze);
                            SimulationProtokoll.Aktuell.Fehlermeldung(
                                MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE + Fehlertext);
                            result[0] = 0;
                            return result;
                        }

                        extrapolation = true;
                        // Der Einmal-Schlüssel ist sprachneutral (Schicht 2) - er darf sich
                        // mit der Oberflächensprache NICHT ändern, sonst käme die Meldung
                        // in einer Sprache mehrfach und in der anderen gar nicht.
                        SimulationProtokoll.Aktuell.HinweisEinmal(
                            "WP_Extrapolation_" + bezeichner + "_" + kenndaten.Vorlauf,
                            string.Format(MyResource.Resource.SIMENG_WP_EXTRAPOLATION_HINWEIS,
                                          bezeichner, untergrenze));
                    }
                    double[] x = new double[2];
                    double[] y = new double[2];
                    double[] xq = new double[2];
                    x[0] = kenndaten.dat[kenndaten.anz - 1].Temperatur;
                    x[1] = kenndaten.dat[kenndaten.anz - 2].Temperatur;
                    y[0] = kenndaten.dat[kenndaten.anz - 1].COP;
                    y[1] = kenndaten.dat[kenndaten.anz - 2].COP;
                    xq[0] = temperatur;
                    cop = Interp(x,y,xq);
                    y[0] = kenndaten.dat[kenndaten.anz - 1].Leistung;
                    y[1] = kenndaten.dat[kenndaten.anz - 2].Leistung;
                    ptherm = Interp(x, y, xq);
                    pel = ptherm / cop;
                    wptherm = ptherm;
                }
                else
                {
                    // Interpolation innerhalb der Kennlinie
                    for (int i = 1; i < kenndaten.anz; i++)
                    {
                        if (temperatur >= kenndaten.dat[i].Temperatur)
                        {
                            double[] x = new double[2];
                            double[] y = new double[2];
                            double[] xq = new double[2];
                            x[0] = kenndaten.dat[i - 1].Temperatur;
                            x[1] = kenndaten.dat[i].Temperatur;
                            y[0] = kenndaten.dat[i - 1].COP;
                            y[1] = kenndaten.dat[i].COP;
                            xq[0] = temperatur;
                            cop = Interp(x, y, xq);
                            y[0] = kenndaten.dat[i - 1].Leistung;
                            y[1] = kenndaten.dat[i].Leistung;
                            ptherm = Interp(x, y, xq);
                            pel = ptherm / cop;
                            wptherm = ptherm;
                            break;
                        }
                    }
                }
            }

            result[0] = 1;
            result[1] = cop;
            result[2] = ptherm;
            result[3] = pel;

            return result;
        }

        public static double Interp(double[] x, double[] y, double[] xq)
        {
            return y[0] +  (xq[0] - x[0]) * (y[1] - y[0]) / (x[1] - x[0]);    
        }

        public void Init()
        {
            // Quellenbezogene Listen mit zurücksetzen. Sie werden in Berechnung() neu
            // aufgebaut - ein Lauf, der die Wärmepumpe NICHT rechnet (Gewerk abgewählt)
            // oder vorzeitig abbricht, käme sonst nie dazu: SimulationControl.AlleSpeicher()
            // liefert dann weiter die Quellspeicher des Vorlaufs, und der veraltete
            // Speicher landete in Anzeige, CSV-Export und Tab_ErgebnisPufferspeicher.
            // Die Clear()-Aufrufe am Anfang von Berechnung() bleiben stehen (Init wird
            // dort als Erstes gerufen) - doppeltes Leeren ist unschädlich.
            wp_quelltemp.Clear();
            wp_quellspeicher.Clear();
            wp_typ.Clear();

            // PAKET B1: Die Kopplungsmaske gehört aus demselben Grund hierher - sie
            // entsteht erst NACH dem Modulaufbau (BoosterKopplungVorbereiten) und dürfte
            // aus einem Vorlauf nie in einen Lauf mit anderer Modulliste hineinreichen.
            _quellKopplung = null;

            // D5a: Rechenebenen und Quellentnahme-Meldungen gehören zum Laufzustand. Die
            // Kaskadenschleife setzt die Ebenen je Lauf neu; ohne Rücksetzen liefen sie
            // aus einem Vorlauf in einen Lauf mit anderer Modulliste.
            Quellentnahmen.Clear();
            ModulEbenen = null;
            AktiveEbene = 0;

            for (int i = 0; i < MAX_WP; i++)
            {
                Modul_WP_Strombedarf[i] = 0;
                Modul_WP_Waermeproduktion[i] = 0;
                Modul_Heizstab[i] = 0;
                Modul_WP_Laufzeit[i] = 0;
                WP_Modul[i] = "";
            }

            for (int i = 0; i < 8760; i++)
            {
                waermerestbedarf_stuendlich[i] = 0;
                WP_Strombedarf_stuendlich[i] = 0;
                WP_Waermeproduktion_stuendlich[i] = 0;
                WP_Waermeproduktion_stuendlich_sortiert[i] = 0;
                Heizstab_stuendlich[i] = 0;
            }
            WP_Waermeproduktion_gesamt = 0;
            Heizstab_gesamt = 0;
            WP_Strombedarf_gesamt = 0;
            WP_Laufzeit = 0;
            // B0-7: Bilanzgrößen mit zurücksetzen — bei einem Abbruch der Berechnung
            // blieben sonst Werte des Vorlaufs stehen und BaueErgebnis meldete eine
            // falsche Deckung/einen falschen Restbedarf.
            Waermebedarf_gesamt = 0;
            waermerestbedarf_gesamt = 0;
            Bivalenzpunkt = -100;

            // Paket-5-Nacharbeit N2: Eigenanteils-Größen, aus denen SimulationRunner
            // Restbedarf und Deckungsgrad der Wärmepumpe bildet.
            Direktdeckung_gesamt = 0;
            Speicherentladung_Anteil = 0;

            // K2: die Kanalaufschlüsselung derselben Größen (Konzept 4.4).
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Heizstab_Kanal, 0, Kanal.ANZAHL);

            // E2: und ihre Ganglinienfassung (Nachtrag zu Konzept 4.4).
            Direktdeckung_KanalStuendlich.Nullen();
            Speicherentladung_KanalStuendlich.Nullen();
            Heizstab_KanalStuendlich.Nullen();
        }
    }

}
