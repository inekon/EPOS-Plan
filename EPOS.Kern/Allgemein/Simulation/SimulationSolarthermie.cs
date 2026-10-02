using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class SimulationSolarthermie
    {
        public List<int> solarthermie_list = new List<int>();
        // Ergebnis je Solarkollektor(feld) fuer die Auflistung in der Ergebnismaske.
        public List<SolarKollektorErgebnis> Kollektor_Ergebnisse = new List<SolarKollektorErgebnis>();
        public int m_ID_Projekt = 0;
        private long nID_Klimaregion;

        public double WaermeproduktionGesamtKwh = 0;
        public double WaermebedarfGesamtKwh = 0;
        public double Max_Waermebedarf;

        public double[] Waermebedarf = new double[8760];
        public double[] Restwaerme = new double[8760];
        public double[] Waermeproduktion = new double[8760];
        public double[] Ueberschuss = new double[8760];

        public double Lon = 0;
        public double Lat = 0;
        public double UeberschussSummeKwh = 0;
        public double Waermeproduktion_max = 0;
        public double RestwaermeSummeKwh = 0;

        // ===================================================================
        // Zweikanaliger Weg (Paket 5 - Konzept 6.4)
        // ===================================================================

        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je Kollektorfeld, INDEXGLEICH zur Reihenfolge der
        /// Felder (Konzept 6.2).
        ///
        /// Über sie findet <c>SimulationControl.LadeordnungAufbauen</c> zu einer
        /// Senkenzuordnung das rechnende Modul — <c>solarthermie_list</c> trägt die
        /// Katalog-ID (<c>ID_SOLAR</c>) und ist als Schlüssel ungeeignet.
        /// </summary>
        public List<int> solar_anlagen_ids = new List<int>();

        /// <summary>
        /// In Pufferspeicher geladene Solarwärme je Stunde [kWh] (zweikanaliger Weg).
        ///
        /// Sie ist ein TEIL von <see cref="Waermeproduktion"/>: Dort steht ab Paket 5 der
        /// gesamte NUTZBARE Ertrag, also Direktdeckung PLUS Speicherladung. Getrennt
        /// geführt wird die Ladung, weil die Ergebnispersistenz den Restbedarf aus der
        /// DIREKTDECKUNG bilden muss — sonst wird er negativ und die Deckung überschreitet
        /// 100 % (Konzept 6.4, zwingende Mitkorrektur an <c>SimulationRunner</c>).
        /// </summary>
        public double[] Speicherladung_stuendlich = new double[8760];

        /// <summary>Jahressumme der Speicherladung [kWh]; ohne Puffer-Senke exakt 0.</summary>
        public double SpeicherladungGesamtKwh = 0;

        /// <summary>
        /// Anteil der Produktion, der den Momentanbedarf DIREKT deckt [kWh].
        /// <c>WaermeproduktionGesamtKwh = DirektdeckungGesamtKwh + SpeicherladungGesamtKwh</c>.
        /// </summary>
        public double DirektdeckungGesamtKwh = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        ///
        /// Gefüllt von <see cref="Kaskadenschleife"/>; ohne Puffer-Senke
        /// exakt 0. Direktdeckung PLUS dieser Anteil ist der EIGENANTEIL der Solarthermie
        /// an der Bedarfsdeckung — die Größe hinter
        /// <c>Tab_ErgebnisSolarthermie.Waermebedarfsdeckung</c>.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        // ------------------------------------------------------------------
        // KANALINDIZIERTE DECKUNGSBUCHFÜHRUNG (Paket K2, Konzept 4.4)
        //
        // ZUSÄTZLICHE Aufschlüsselung, kein Ersatz: DirektdeckungGesamtKwh,
        // SpeicherladungGesamtKwh, Speicherentladung_Anteil und die Ganglinien
        // werden unverändert gebildet und gelesen. Es gilt
        //
        //   Σ Direktdeckung_Kanal[k]     == DirektdeckungGesamtKwh
        //   Σ Speicherentladung_Kanal[k] == Speicherentladung_Anteil
        //
        // bis auf die Rundungsklasse der getrennten Kanalarithmetik.
        // ------------------------------------------------------------------

        /// <summary>
        /// Direkt gedeckter Momentanbedarf je Kanal [kWh] (Phase B) — die Aufschlüsselung
        /// von <see cref="DirektdeckungGesamtKwh"/>.
        /// </summary>
        public double[] Direktdeckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Anteil dieses Erzeugers an der bedarfsdeckenden Speicherentladung je Kanal
        /// [kWh] — die Aufschlüsselung von <see cref="Speicherentladung_Anteil"/>.
        /// Gefüllt von der <see cref="Kaskadenschleife"/>, wie der Skalar selbst.
        /// </summary>
        public double[] Speicherentladung_Kanal = new double[Kanal.ANZAHL];

        // ------------------------------------------------------------------
        // PAKET E2 (Nachtrag zu Konzept 4.4) — DIESELBEN GRÖSSEN ALS GANGLINIE,
        // gebucht an genau derselben Stelle und aus derselben Variablen. Je Kanal k gilt
        //   Σ_h Direktdeckung_KanalStuendlich[k][h]     == Direktdeckung_Kanal[k]
        //   Σ_h Speicherentladung_KanalStuendlich[k][h] == Speicherentladung_Kanal[k]
        // bis auf die Assoziativität der double-Addition.
        // ------------------------------------------------------------------

        /// <summary>Stundenfassung von <see cref="Direktdeckung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Direktdeckung_KanalStuendlich = new Kanalganglinie();

        /// <summary>Stundenfassung von <see cref="Speicherentladung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Speicherentladung_KanalStuendlich = new Kanalganglinie();

        /// <summary>Potenzieller Bruttoertrag je Kollektorfeld und Stunde [kWh].</summary>
        private double[][] _potenzialFeld = new double[0][];

        /// <summary>Noch nicht untergebrachtes Potenzial je Feld in der laufenden Stunde [kWh].</summary>
        private double[] _restPotenzial = new double[0];

        /// <summary>Jahressumme des nutzbaren Ertrags je Feld [kWh] (Deckung + Ladung).</summary>
        private double[] _prodFeld = new double[0];

        /// <summary>Jahressumme des verworfenen Überschusses je Feld [kWh].</summary>
        private double[] _ueberFeld = new double[0];

        /// <summary>In der laufenden Stunde genutzte Wärme je Feld (Deckung plus Ladung) [kWh] — die Betriebsbedingung des Pumpenstroms.</summary>
        private double[] _abgabeStunde = new double[0];

        /// <summary>Pumpenleistung je Feld [W]; <c>null</c> = nicht gepflegt (ST1).</summary>
        private double?[] _pumpeW = new double?[0];

        /// <summary><c>Hilfsenergie_Anteil</c> je Feld [%] — der Ersatzweg ohne Pumpenleistung (ST1).</summary>
        private double?[] _hilfsAnteil = new double?[0];

        /// <summary>Jahressumme des Pumpenstroms je Feld [kWh].</summary>
        private double[] _pumpeFeld = new double[0];

        /// <summary>Die Eingänge der Kennlinie je Feld (ST2); <c>null</c> = Potenzial steht vorab fest.</summary>
        private Kollektorstunden[] _modell = new Kollektorstunden[0];

        /// <summary>Ausgewertete Stunden je Feld (Zeilen der Klimadaten, höchstens 8760).</summary>
        private int[] _stunden = new int[0];

        /// <summary>Senkenpuffer je Feld, aus dessen unterster Zone die Arbeitstemperatur kommt (ST2); <c>null</c> = keiner.</summary>
        private SimulationPufferspeicher[] _temperaturSpeicher = new SimulationPufferspeicher[0];

        /// <summary>Mittlere Fluidtemperatur je Feld, gewichtet mit dem Potenzial [°C·kWh] — für den Ausweis.</summary>
        private double[] _tMittelGewichtet = new double[0];

        /// <summary>
        /// Der gerechnete RÜCKLAUF des Heizkreises je Stunde [°C] (Anlagenkopplung AK1) — die
        /// Eintrittsseite eines Felds mit Arbeitstemperatur aus dem Speicher, das keinen Puffer
        /// lädt (ST2). <c>null</c> ohne gekoppeltes Gebäude. Gesetzt von <c>SimulationControl</c>.
        /// </summary>
        public double[] Heizkreisruecklauf;

        /// <summary>
        /// Pumpenstrom der Solarkreise je Stunde [kWh] (ST1): Pumpenleistung · 1 h in jeder Stunde,
        /// in der ein Feld Wärme abgibt, hilfsweise der Hilfsenergieanteil auf die genutzte Wärme.
        /// <c>SimulationControl</c> bucht die Reihe in den Strombedarf; ohne gepflegten Wert bleibt
        /// sie 0.
        /// </summary>
        public double[] Pumpenstrom_stuendlich = new double[8760];

        /// <summary>Jahressumme des Pumpenstroms [kWh].</summary>
        public double PumpenstromGesamtKwh = 0;

        private readonly List<string> _feldName = new List<string>();
        private readonly List<double> _feldFlaeche = new List<double>();
        private readonly List<long> _feldAnzahl = new List<long>();
        private readonly List<Senkenliste> _feldSenke = new List<Senkenliste>();
        private readonly List<bool> _feldGanglinie = new List<bool>();

        /// <summary>Anzahl der Kollektorfelder des zweikanaligen Wegs.</summary>
        public int FelderAnzahl { get { return _feldName.Count; } }

        /// <summary>Senkenliste eines Kollektorfelds (nie null nach dem Aufbau, Paket S1).</summary>
        public Senkenliste FeldSenke(int index)
        {
            if (index < 0 || index >= _feldSenke.Count) return null;
            return _feldSenke[index];
        }

        /// <summary>
        /// Die Weiche des letzten Aufbaus (Folgeauftrag 4, Entscheid ST8 Weg a): was das
        /// Projekt an Solarthermieganglinie führt. Nie <c>null</c> nach
        /// <see cref="Vorbereiten_Zweikanalig"/>; Regel in <see cref="SolarganglinieWeiche"/>.
        /// </summary>
        public SolarganglinieWeiche.Stand Ganglinie = SolarganglinieWeiche.Keine();

        /// <summary>
        /// true, wenn der letzte Aufbau die Solarthermie über die Ganglinie rechnet: EIN
        /// Feld, dessen Potenzial die Ganglinienwerte sind (kW je Stunde = kWh).
        /// </summary>
        public bool RechnetGanglinie { get; private set; }

        // PAKET A1: Hier stand "Berechnung(int ID_Projekt)" - der Einstieg des
        // einkanaligen Altpfads (Klimaregion, Kollektorfelder_Lesen, Jahresschleife je
        // Feld auf EINEM Bedarfsvektor mit Bilanzieren). Er ist mit dem Altpfad
        // ersatzlos entfallen; der Einstieg des Moduls ist Vorbereiten_Zweikanalig(),
        // gerechnet wird in der Kaskadenschleife oder als Vektorstufe
        // (Berechnung_Zweikanalig).

        /// <summary>
        /// EIN Kollektorfeld des Projekts samt seinem stündlichen Bruttopotenzial.
        /// Zwischenergebnis von <see cref="Kollektorfelder_Lesen"/>; beide Rechenwege
        /// arbeiten damit weiter (Paket-5-Nacharbeit, Befund N6).
        /// </summary>
        private sealed class SolarFeld
        {
            /// <summary>Tab_Energieanlagen.ID des Felds.</summary>
            public int ID_Anlage;
            public string Name = "";
            /// <summary>
            /// Rechnende Kollektorfläche gesamt [m²] = Bezugsfläche eines Moduls · Anzahl — die Fläche,
            /// auf die η₀, a₁ und a₂ bezogen sind: Apertur (Vorgabe) oder Brutto (ST6,
            /// <see cref="Solarkreis.Modulbezugsflaeche"/>).
            /// </summary>
            public double Flaeche;
            public long Anzahl;
            /// <summary>Zahl der ausgewerteten Stunden (Zeilen der Klimadaten, höchstens 8760).</summary>
            public int Stunden;
            /// <summary>Potenzieller Bruttoertrag je Stunde [kWh].</summary>
            public double[] Potenzial = new double[8760];
            /// <summary><c>Tab_Energieanlagen.Bezeichner</c> der Anlagenzeile ("" ohne Zeile).</summary>
            public string Anlagenname = "";
            /// <summary>
            /// Eigene Senkenliste des Felds; <c>null</c> = aus den Senkenlisten des Projekts
            /// über <see cref="ID_Anlage"/> (der Regelfall). Gesetzt nur für die Ganglinie
            /// ohne Anlagenzeile (alle Wärmekanäle direkt).
            /// </summary>
            public Senkenliste Senke;
            /// <summary>true für das Feld der Solarthermieganglinie.</summary>
            public bool IstGanglinie;
            /// <summary>Pumpenleistung [W]; <c>null</c> = nicht gepflegt (ST1).</summary>
            public double? PumpenleistungW;
            /// <summary><c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> [%]; Ersatzweg des Pumpenstroms.</summary>
            public double? HilfsenergieAnteil;
            /// <summary>
            /// Die Eingänge der Kollektorkennlinie je Stunde (ST2): Strahlung, Außentemperatur,
            /// Einfallswinkel und Kennwerte. <c>null</c> für die Ganglinie und die Testfelder — ihr
            /// <see cref="Potenzial"/> steht vorab fest.
            /// </summary>
            public Kollektorstunden Modell;
        }

        /// <summary>
        /// DIE EINGÄNGE DER KOLLEKTORKENNLINIE eines Felds — alles, was der Wirkungsgrad einer
        /// Stunde braucht, außer der Arbeitstemperatur (ST2, Welle M2). Gelesen wird einmal vor der
        /// Stundenschleife (<see cref="Kollektorfelder_Lesen"/>); die Formel selbst rechnet in
        /// <see cref="Stunde_Start"/>, mit der Arbeitstemperatur der Stunde.
        /// </summary>
        private sealed class Kollektorstunden
        {
            /// <summary>Gesamtstrahlung auf die Kollektorebene je Stunde [W/m²] (<c>CalculateHourly</c>).</summary>
            public readonly double[] Gesamt = new double[8760];
            /// <summary>Direktanteil G_b je Stunde [W/m²] (ST5).</summary>
            public readonly double[] Direkt = new double[8760];
            /// <summary>Diffus- und Bodenreflexanteil G_dr je Stunde [W/m²] (ST5).</summary>
            public readonly double[] DiffusReflex = new double[8760];
            /// <summary>Außentemperatur je Stunde [°C].</summary>
            public readonly double[] Aussen = new double[8760];
            /// <summary>Kosinus des Einfallswinkels je Stunde.</summary>
            public readonly double[] CosTheta = new double[8760];
            public double H0, K1, K2, Kdir50, Kdfu;
            /// <summary>Führt der Satz ein K_dfu (ST5)?</summary>
            public bool MitKdfu;
            /// <summary>Faktor nach den Verlusten des Solarkreises (ST3 Stufe 1).</summary>
            public double Verlustfaktor;
            /// <summary>Arbeitstemperatur aus dem Speicher (ST2) statt fest 50 °C?</summary>
            public bool AusSpeicher;
            /// <summary>Grädigkeit des Wärmeübertragers [K] (ST4).</summary>
            public double GraedigkeitK;
            /// <summary>Spreizung des Kollektorkreises [K] (ST2).</summary>
            public double SpreizungK;
        }

        /// <summary>
        /// Klimaregion des Projekts und ihre Geokoordinaten — Schritte 1 und 2 aus
        /// <see cref="Berechnung"/>, seit der Paket-5-Nacharbeit gemeinsam mit
        /// <see cref="Vorbereiten_Zweikanalig"/> (Befund N6).
        ///
        /// Die Projektabfrage läuft über <see cref="StilleDb"/> statt über den
        /// Altzugriff <c>RecordSet</c> (Befund N9): Der schluckt SQL-Fehler still — bei
        /// einem Fehlschlag bliebe <c>nID_Klimaregion</c> auf dem Wert des VORLAUFS
        /// stehen und die Solarthermie rechnete mit dem Wetter eines anderen Projekts.
        /// Jetzt steht der Fehlschlag im Protokollkanal (und damit weiterhin auch auf
        /// der Konsole — <c>SimulationProtokoll.Eintragen</c> schreibt beides). Die Abfrage ist
        /// parametrisiert und liefert denselben Wert wie zuvor — der byte-identische
        /// Regressionslauf mit Flag AUS belegt das.
        /// </summary>
        private void KlimaregionUndGeoLesen()
        {
            object v = StilleDb.Scalar("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                                       StilleDb.Par("@id", DbParamTyp.Integer, m_ID_Projekt));
            if (v != null) nID_Klimaregion = StilleDb.Zahl(v);
            // Protokollkanal-Nachzug: WARNUNG - die Solarthermie rechnet mit dem Wetter
            // eines anderen Projekts weiter, das ist eine Ersatzannahme mit
            // Ergebniswirkung. Einmal je Lauf (die Methode läuft in beiden Rechenwegen).
            else SimulationProtokoll.Aktuell.WarnungEinmal("solar-klimaregion-fehlt",
                                   "Solarthermie: Zu Projekt " + m_ID_Projekt + " ließ sich keine " +
                                   "Klimaregion lesen - es gilt der zuletzt gelesene Wert (" +
                                   nID_Klimaregion + ").");

            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrlklima.ReadSingle("select * from Tab_Klimaregion where ID=" + nID_Klimaregion);

            if (ctrlklima.rows > 0)
            {
                Lon = ctrlklima.Longitude;
                Lat = ctrlklima.Latitude;
            }
        }

        /// <summary>
        /// Liest die Kollektorfelder des Projekts und die EINGÄNGE ihrer Kennlinie für das ganze
        /// Jahr — Strahlung auf die Kollektorebene (getrennt nach Direkt- und Diffusanteil, ST5),
        /// Außentemperatur, Einfallswinkel, Kennwerte, Fläche und Verluste. Die Schritte 1 und 2
        /// des Kollektormodells (spezifische Leistung, potenzielle Erzeugung) rechnen seit der
        /// Welle M2 je Stunde in <see cref="Stunde_Start"/>, weil die Arbeitstemperatur aus dem
        /// Speicher vom Füllstand der Vorstunde abhängt (ST2).
        ///
        /// EINE Fassung (Paket-5-Nacharbeit, Befund N6): Bis dahin stand dieser Block
        /// zweimal im Modul — je einmal für den einkanaligen und den zweikanaligen Weg.
        /// Term für Term waren beide gleich; vereinheitlicht wurde auf die abgesicherte
        /// Stundenzahl <c>Math.Min(rows, 8760)</c>.
        /// </summary>
        private List<SolarFeld> Kollektorfelder_Lesen()
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);

            List<SolarFeld> felder = new List<SolarFeld>();

            for (int n = 0; n < ctrl.rows; n++)
            {
                int nId = ctrl.items[n].ID_Solar;
                int nAzimuth = ctrl.items[n].m_Azimut;
                int nNeigung = ctrl.items[n].m_Neigung;
                long nAnzahl = ctrl.items[n].Kollektormodulanzahl;

                SolarkollektorenCtrl ctrlsol = new SolarkollektorenCtrl();
                ctrlsol.ReadSingle(nId);
                // ST6: die Fläche, auf die die Kennwerte bezogen sind - Apertur (Vorgabe) oder
                // Brutto (Modulfläche). Brutto ohne gepflegte Modulfläche rechnet mit der Apertur.
                bool flaechenRueckfall;
                double nFlaeche = Solarkreis.Modulbezugsflaeche(ctrlsol.m_Bezugsflaeche, ctrlsol.m_Aperturfläche,
                                                                ctrlsol.m_Modulfläche, out flaechenRueckfall);
                if (flaechenRueckfall)
                    SimulationProtokoll.Aktuell.WarnungEinmal("solar-bezugsflaeche-" + nId,
                        "Solarthermie: Die Kennwerte des Kollektors ‚" + ctrlsol.m_szKollektorname + "‘ sind auf " +
                        "die Bruttofläche bezogen, der Satz führt aber keine Modulfläche - der Lauf rechnet " +
                        "mit der Aperturfläche.");

                // B1 (Paket A): der zentrale Ortszeit-Lesepfad. Bis dahin stand die
                // Kollektorreihe im UTC-Raster und damit 1 bis 2 Stunden vor dem
                // Waermebedarf - die Jahressumme blieb richtig, die stundenscharfe
                // Deckung nicht.
                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadOrtszeit((int)nID_Klimaregion, m_ID_Projekt);

                // Konstanten für das Kollektormodell
                double h0 = ctrlsol.m_h0;
                double k1 = ctrlsol.m_k1;
                double k2 = ctrlsol.m_k2;
                double kdir50 = ctrlsol.m_Kdir;
                // ST5: Einfallswinkelkorrektur der Diffusstrahlung; 0 = nicht bekannt, dann gilt
                // für die Diffusstrahlung der Faktor der Direktstrahlung (Rechnung wie zuvor).
                double kdfu = ctrlsol.m_Kdfu;
                bool mitKdfu = Solarkreis.KdfuGepflegt(kdfu);

                // ST3 Stufe 1: die Verluste des Solarkreises aus dem Feld; leer = 8 % - der
                // Faktor (100 - 8) / 100 ist bitgleich das frühere Literal 0,92.
                double leitungsverluste = Solarkreis.Verlustfaktor(ctrl.items[n].Solarkreisverluste_Prozent);

                SolarFeld f = new SolarFeld();
                f.ID_Anlage = ctrl.items[n].ID;
                f.Anlagenname = ctrl.items[n].Bezeichner ?? "";
                f.Name = ctrlsol.m_szKollektorname;
                f.Flaeche = nFlaeche * nAnzahl;
                f.Anzahl = nAnzahl;
                f.Stunden = Math.Min(ctrldat.rows, 8760);
                f.PumpenleistungW = ctrl.items[n].Pumpenleistung_W;
                f.HilfsenergieAnteil = HilfsenergieAnteilLesen(f.ID_Anlage);

                // ST2 (Welle M2): die Kennwerte und der Weg der Arbeitstemperatur; die Formel
                // rechnet je Stunde in Stunde_Start.
                Kollektorstunden m = new Kollektorstunden
                {
                    H0 = h0, K1 = k1, K2 = k2, Kdir50 = kdir50, Kdfu = kdfu, MitKdfu = mitKdfu,
                    Verlustfaktor = leitungsverluste,
                    AusSpeicher = Solarkreis.ArbeitstemperaturAusSpeicher(ctrl.items[n].Arbeitstemperatur_Weg),
                    GraedigkeitK = Solarkreis.Graedigkeit(ctrl.items[n].Uebertrager_Graedigkeit_K),
                    SpreizungK = Solarkreis.Spreizung(ctrl.items[n].Kollektor_Spreizung_K)
                };
                f.Modell = m;

                for (int i = 0; i < f.Stunden; i++)
                {
                    SolardatenModel zeile = ctrldat.items[i];

                    // CalculateHourly berechnet bereits die effektive Strahlung auf der geneigten Fläche [cite: 52, 69, 71]
                    //
                    // E1.4 (Paket A): Der Sonnenstand rechnet auf UTC-Basis und erwartet
                    // einen 1-BASIERTEN Tag. Beides steht seit dem Ortszeit-Lesepfad an der
                    // Zeile selbst - der Index i ist jetzt die ORTSZEIT-Position und taugt
                    // dafuer nicht mehr.
                    double gTilted = SolarCalculator.CalculateHourly(
                        Lon, Lat, nNeigung, nAzimuth,
                        zeile.Globalstrahlung,
                        zeile.Direktstrahlung,
                        zeile.Diffusstrahlung,
                        zeile.Außen_Temp,
                        zeile.TagUtc, zeile.StundeUtc);

                    double ta = zeile.Außen_Temp;

                    // WICHTIG: cosTheta für IAM-Berechnung sauber ermitteln
                    // Wir nutzen hier den internen Wert aus dem Calculator
                    double currentCosTheta = SolarCalculator.lastCosTheta;

                    // ST5 (EN ISO 9806): die Strahlung auf der Kollektorebene getrennt in den
                    // Direktanteil G_b = DNI · cos θ - dieselbe Multiplikation wie in
                    // SolarCalculator.CalculateHourly - und den Rest G_dr aus Diffus- und
                    // Bodenreflexstrahlung. In einer Nachtstunde liefert CalculateHourly 0 und
                    // lässt cos θ stehen; dann ist auch G_b 0.
                    double gDirekt = gTilted > 0 ? zeile.Direktstrahlung * currentCosTheta : 0;
                    double gDiffusReflex = gTilted - gDirekt;

                    // ST2: Die Schritte 1 (spezifische Leistung) und 2 (Bruttoertrag) rechnen ab
                    // der Welle M2 in der Stundenschleife (PotenzialStunde) - hier stehen nur ihre
                    // Eingänge.
                    m.Gesamt[i] = gTilted;
                    m.Direkt[i] = gDirekt;
                    m.DiffusReflex[i] = gDiffusReflex;
                    m.Aussen[i] = ta;
                    m.CosTheta[i] = currentCosTheta;
                }

                felder.Add(f);
            }

            return felder;
        }

        /// <summary>
        /// <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> einer Anlagenzeile [%] — eine Fachspalte, die
        /// das Anlagenmodell nicht trägt; <c>null</c> ohne Wert oder ohne Spalte.
        /// </summary>
        private static double? HilfsenergieAnteilLesen(int idAnlage)
        {
            if (idAnlage <= 0 || !DataRepository.SpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN, "Hilfsenergie_Anteil"))
                return null;
            object v = StilleDb.Scalar("SELECT Hilfsenergie_Anteil FROM Tab_Energieanlagen WHERE ID = ?",
                                       StilleDb.Par("@id", DbParamTyp.Integer, idAnlage));
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        public void Init()
        {
            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Waermeproduktion, 0, Waermeproduktion.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            WaermeproduktionGesamtKwh = 0;
            UeberschussSummeKwh = 0;
            Kollektor_Ergebnisse.Clear();

            // Speichergrößen (Paket 5) - ohne Puffer-Senke bleiben sie auf 0, damit
            // die Mitkorrektur in SimulationRunner dort nachweislich wirkungslos ist.
            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            SpeicherladungGesamtKwh = 0;

            // ST1: der Pumpenstrom der Solarkreise.
            Array.Clear(Pumpenstrom_stuendlich, 0, Pumpenstrom_stuendlich.Length);
            PumpenstromGesamtKwh = 0;
            DirektdeckungGesamtKwh = 0;
            Speicherentladung_Anteil = 0;

            // K2: die Kanalaufschlüsselung derselben Größen (Konzept 4.4).
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);

            // E2: und ihre Ganglinienfassung, an derselben Stelle.
            Direktdeckung_KanalStuendlich.Nullen();
            Speicherentladung_KanalStuendlich.Nullen();
        }

        public (double produktion, double restbedarf, double ueberschuss) BerechneSolarthermie(
            double waermebedarf, double strahlung, double flaeche,
            double h0, double k1, double k2, double kdir50,
            double tStorage, double ta, double cosTheta, double leitungsverluste)
        {
            // 1. Spezifische Leistung berechnen (W/m²)
            double leistungProQm = CalculateThermalPower(strahlung, ta, tStorage, cosTheta, h0, k1, k2, kdir50);

            // 2. Gesamtproduktion in kW (Wh -> kWh)
            double potenzielleErzeugung = (leistungProQm * flaeche * leitungsverluste) / 1000.0;

            // 3. Bilanzierung
            return Bilanzieren(waermebedarf, potenzielleErzeugung);
        }

        /// <summary>
        /// Schritt 3 des Kollektormodells: der KAPPUNGSPUNKT. Was über den Momentanbedarf
        /// hinausgeht, ist im einkanaligen Weg verworfen.
        ///
        /// Eigene Methode seit der Paket-5-Nacharbeit (Befund N6): Der zweikanalige Weg
        /// braucht die Schritte 1 und 2 OHNE Schritt 3 — er entscheidet erst in der
        /// Stunde, ob der Ertrag deckt, lädt oder verworfen wird. Die Anweisungen sind
        /// unverändert.
        /// </summary>
        public static (double produktion, double restbedarf, double ueberschuss) Bilanzieren(
            double waermebedarf, double potenzielleErzeugung)
        {
            double produktion = Math.Min(potenzielleErzeugung, waermebedarf);
            double ueberschuss = Math.Max(0, potenzielleErzeugung - waermebedarf);
            double restbedarf = Math.Max(0, waermebedarf - produktion);

            return (produktion, restbedarf, ueberschuss);
        }

        public double CalculateThermalPower(double gTilted, double tAmb, double tStorage,
                                          double cosTheta, double h0, double a1, double a2, double kDir50)
        {
            if (gTilted <= 0) return 0;

            // IAM (Incident Angle Modifier) Berechnung [cite: 50, 67, 69]
            double thetaRad = Plattformrundung.Acos(Math.Min(Math.Max(cosTheta, 0), 1));

            // Physikalische b0-Näherung für Flachkollektoren
            double cos50 = Plattformrundung.Cos(50.0 * Math.PI / 180.0);
            double b0 = (1.0 - kDir50) / (1.0 / cos50 - 1.0);

            // IAM Faktor (Vermeidung von Division durch Null bei 90°)
            double cosThetaClamped = Math.Max(cosTheta, 0.001);
            double iam = 1.0 - b0 * (1.0 / cosThetaClamped - 1.0);
            iam = Math.Max(Math.Min(iam, 1.0), 0.0);

            // Wirkungsgrad-Modell nach EN 12975
            double h0_effektiv = h0 * iam;
            double dT = tStorage - tAmb;

            // Thermischer Wirkungsgrad
            double wirkungsgrad = h0_effektiv - (a1 * dT / gTilted) - (a2 * dT * dT / gTilted);

            double leistung = gTilted * wirkungsgrad;
            return Math.Max(0, leistung);
        }

        // ===================================================================
        // Zweikanaliger Weg - Aufbau, Stundenschritte, Abschluss (Konzept 6.4)
        // ===================================================================

        /// <summary>
        /// Baut die Kollektorfelder des zweikanaligen Wegs auf und liest die Eingänge ihrer
        /// Kennlinie für das ganze Jahr.
        ///
        /// Der Bruttoertrag eines Kollektorfelds hängt vom Wetter, von der Ausrichtung, von den
        /// Kollektorkennwerten und von der Arbeitstemperatur ab — nicht vom Wärmebedarf der Stunde.
        /// Mit fester Arbeitstemperatur (Vorgabe) steht das Potenzial damit vorab fest; mit der
        /// Arbeitstemperatur aus dem Speicher (ST2, Welle M2) folgt es dem Füllstand der Vorstunde.
        /// In beiden Fällen bildet <see cref="Stunde_Start"/> das Potenzial der Stunde, bevor die
        /// Verwendung (Direktdeckung, Speicherladung, Verwurf) entschieden wird.
        /// </summary>
        /// <param name="senken">Geordnete Senkenlisten des Projekts (Konzept 5.1).</param>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenliste> senken)
        {
            m_ID_Projekt = ID_Projekt;

            // Klimaregion, Geokoordinaten und Kollektorfelder samt Potenzial kommen aus
            // denselben Methoden wie die Kollektorbilanz (Paket-5-Nacharbeit, N6) — damit
            // ist „dieselben Aufrufe, dieselbe Reihenfolge, dieselben Zahlen" nicht mehr
            // eine Zusage über zwei Kopien, sondern dieselbe Anweisungsfolge.
            KlimaregionUndGeoLesen();

            Init();
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);

            List<SolarFeld> felder = Kollektorfelder_Lesen();

            // Folgeauftrag 4 (ST8 Weg a): die Weiche Kollektorfeld / Ganglinie. Mit
            // vollständiger Ganglinie tritt EIN Feld an die Stelle der Kollektorfelder;
            // alles Weitere (Senken, Pufferladung, Kaskadenplatz, Überschuss) bleibt der
            // Weg des Kollektorfelds.
            Ganglinie = SolarganglinieWeiche.Lesen(ID_Projekt);
            felder = GanglinieEinsetzen(felder, Ganglinie);

            FelderUebernehmen(felder, senken);
            return true;
        }

        /// <summary>
        /// Übernimmt die Felder in die Rechenstruktur des zweikanaligen Wegs — indexgleich
        /// <see cref="solar_anlagen_ids"/>, Name, Fläche, Anzahl, Senkenliste und Potenzial.
        /// </summary>
        private void FelderUebernehmen(List<SolarFeld> felder, List<Senkenliste> senken)
        {
            solar_anlagen_ids.Clear();
            _feldName.Clear();
            _feldFlaeche.Clear();
            _feldAnzahl.Clear();
            _feldSenke.Clear();
            _feldGanglinie.Clear();

            List<double[]> potenziale = new List<double[]>();
            _pumpeW = new double?[felder.Count];
            _hilfsAnteil = new double?[felder.Count];
            _modell = new Kollektorstunden[felder.Count];
            _stunden = new int[felder.Count];
            _temperaturSpeicher = new SimulationPufferspeicher[felder.Count];
            _tMittelGewichtet = new double[felder.Count];

            for (int n = 0; n < felder.Count; n++)
            {
                SolarFeld f = felder[n];

                potenziale.Add(f.Potenzial);
                solar_anlagen_ids.Add(f.ID_Anlage);
                _feldName.Add(f.Name);
                _feldFlaeche.Add(f.Flaeche);
                _feldAnzahl.Add(f.Anzahl);
                _feldSenke.Add(f.Senke ?? SenkeZuAnlage(senken, f.ID_Anlage));
                _feldGanglinie.Add(f.IstGanglinie);

                // Die Ganglinie (Abschnitt 14) bleibt ohne Pumpenstrom: Sie ist ein gegebener Ertrag.
                _pumpeW[n] = f.IstGanglinie ? null : f.PumpenleistungW;
                _hilfsAnteil[n] = f.IstGanglinie ? null : f.HilfsenergieAnteil;
                _modell[n] = f.IstGanglinie ? null : f.Modell;
                _stunden[n] = f.Stunden;
            }

            _potenzialFeld = potenziale.ToArray();
            _restPotenzial = new double[_potenzialFeld.Length];
            _prodFeld = new double[_potenzialFeld.Length];
            _ueberFeld = new double[_potenzialFeld.Length];
            _abgabeStunde = new double[_potenzialFeld.Length];
            _pumpeFeld = new double[_potenzialFeld.Length];
        }

        /// <summary>
        /// DIE WEICHE IM RECHENWEG (Folgeauftrag 4, Entscheid ST8 Weg a). Ohne vollständige
        /// Ganglinie bleiben die Kollektorfelder; mit ihr rechnet genau EIN Feld, dessen
        /// Potenzial die Ganglinienwerte sind — <b>absolut, Wert = kW in der Stunde = kWh</b>,
        /// ohne Bezug auf eine Fläche.
        ///
        /// <para><b>Senken und Puffer.</b> Führt das Projekt eine Solarthermie-Anlagenzeile,
        /// ist die mit der kleinsten <c>Tab_Energieanlagen.ID</c> der TRÄGER: Das Feld rechnet
        /// unter ihrer ID, also mit ihren Senken (<c>Z_AnlageSenke</c>, sonst der Vorbelegung
        /// Heizkreis/Beides), ihrer Pufferladung samt Nachrang-Schwelle und an ihrem
        /// Kaskadenplatz. Weitere Anlagenzeilen rechnen nicht. Ohne Anlagenzeile deckt die
        /// Ganglinie alle Wärmekanäle (Heizung, Brauchwasser, Prozesswärme) direkt, ohne
        /// Puffer.</para>
        ///
        /// <para><b>Rückfälle, alle benannt:</b> Eine zugeordnete, aber unvollständige
        /// Ganglinie (nicht genau 8 760 Werte, leere, negative oder nicht endliche Werte) ist
        /// eine Warnung, und der Lauf rechnet mit dem Kollektorfeld.</para>
        /// </summary>
        private List<SolarFeld> GanglinieEinsetzen(List<SolarFeld> kollektorfelder,
                                                  SolarganglinieWeiche.Stand stand)
        {
            RechnetGanglinie = false;
            if (stand == null || !stand.Zugeordnet) return kollektorfelder;

            SimulationProtokoll protokoll = SimulationProtokoll.Aktuell;

            if (!stand.Vollstaendig)
            {
                protokoll.WarnungEinmal("solar-ganglinie-unvollstaendig",
                    "Solarthermie: Die Ganglinie ‚" + stand.Bezeichner + "‘ ist unvollständig (" +
                    stand.Mangel + ") - der Lauf rechnet mit dem Kollektorfeld" +
                    (kollektorfelder.Count == 0
                        ? "; das Projekt führt keines, die Solarthermie liefert nichts."
                        : "."));
                return kollektorfelder;
            }

            if (stand.WeitereZuordnungen > 0)
                protokoll.WarnungEinmal("solar-ganglinie-mehrere",
                    "Solarthermie: Dem Projekt sind " + (stand.WeitereZuordnungen + 1) +
                    " Ganglinien zugeordnet - es rechnet nur die zuerst zugeordnete ‚" +
                    stand.Bezeichner + "‘.");

            SolarFeld traeger = null;
            foreach (SolarFeld f in kollektorfelder)
                if (traeger == null || f.ID_Anlage < traeger.ID_Anlage) traeger = f;

            SolarFeld g = new SolarFeld();
            g.IstGanglinie = true;
            g.Name = string.Format(CultureInfo.CurrentCulture,
                                   MyResource.Resource.SIM_SOLARGANGLINIE_FELDNAME, stand.Bezeichner);
            g.Flaeche = 0;
            g.Anzahl = 0;
            g.Stunden = SolarganglinieWeiche.STUNDEN;
            Array.Copy(stand.Werte, g.Potenzial, SolarganglinieWeiche.STUNDEN);

            string summe = stand.SummeKwh.ToString("F0", CultureInfo.InvariantCulture);

            if (traeger != null)
            {
                g.ID_Anlage = traeger.ID_Anlage;
                g.Anlagenname = traeger.Anlagenname;
                protokoll.HinweisEinmal("solar-ganglinie-traeger",
                    "Solarthermie: Das Projekt rechnet mit der Ganglinie ‚" + stand.Bezeichner +
                    "‘ (Jahressumme " + summe + " kWh) statt mit dem Kollektorfeld. Senken, Puffer " +
                    "und Kaskadenplatz der Anlage „" + traeger.Anlagenname + "“ (ID " +
                    traeger.ID_Anlage + ") gelten für die Ganglinie.");

                if (kollektorfelder.Count > 1)
                    protokoll.HinweisEinmal("solar-ganglinie-weitere-felder",
                        "Solarthermie: Neben der Ganglinie rechnen die " + (kollektorfelder.Count - 1) +
                        " weiteren Kollektorfelder des Projekts nicht.");
            }
            else
            {
                g.ID_Anlage = 0;
                g.Senke = AlleWaermekanaeleDirekt();
                protokoll.HinweisEinmal("solar-ganglinie-ohne-anlage",
                    "Solarthermie: Das Projekt rechnet mit der Ganglinie ‚" + stand.Bezeichner +
                    "‘ (Jahressumme " + summe + " kWh). Es führt keine Solarthermie-Anlage - die " +
                    "Ganglinie deckt alle Wärmekanäle direkt, ohne Puffer.");
            }

            RechnetGanglinie = true;
            return new List<SolarFeld> { g };
        }

        /// <summary>
        /// Senkenliste der Ganglinie ohne Anlagenzeile: Heizkreis/Beides (Heizung und
        /// Brauchwasser) auf Rang 1, Prozesswärme auf Rang 2 — alle Wärmekanäle direkt,
        /// kein Puffer.
        /// </summary>
        internal static Senkenliste AlleWaermekanaeleDirekt()
        {
            Senkenliste l = Senkenliste.Vorbelegung(0);
            l.Zeilen.Add(new Senkenzeile { Ziel = Senke.Prozesswaerme, Rang = 2 });
            return l;
        }

        /// <summary>
        /// Testeinstieg ohne Datenbank: baut die Felder wie
        /// <see cref="Vorbereiten_Zweikanalig"/>, aber aus vorgegebenen Anlagen-IDs
        /// (Kollektorfelder ohne Potenzial) und einem vorgegebenen Stand der Weiche.
        /// </summary>
        internal void Vorbereiten_OhneDatenbank(IList<int> kollektorAnlagen,
                                                SolarganglinieWeiche.Stand stand,
                                                List<Senkenliste> senken)
        {
            Init();
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);

            List<SolarFeld> felder = new List<SolarFeld>();
            if (kollektorAnlagen != null)
                foreach (int id in kollektorAnlagen)
                    felder.Add(new SolarFeld { ID_Anlage = id, Name = "Feld " + id,
                                               Anlagenname = "Anlage " + id, Stunden = 8760 });

            Ganglinie = stand ?? SolarganglinieWeiche.Keine();
            FelderUebernehmen(GanglinieEinsetzen(felder, Ganglinie), senken);
        }

        /// <summary>Jahressumme des Bruttopotenzials eines Felds [kWh] — Prüfgröße der Tests.</summary>
        internal double PotenzialSumme(int feld)
            => feld >= 0 && feld < _potenzialFeld.Length ? _potenzialFeld[feld].Sum() : 0;

        /// <summary>Bruttopotenzial eines Felds in einer Stunde [kWh] — Prüfgröße der Tests.</summary>
        internal double Potenzial(int feld, int stunde)
            => feld >= 0 && feld < _potenzialFeld.Length && stunde >= 0 && stunde < 8760 ? _potenzialFeld[feld][stunde] : 0;

        /// <summary>Rechnende Kollektorfläche eines Felds [m²] — Prüfgröße der Tests.</summary>
        internal double FeldFlaeche(int feld)
            => feld >= 0 && feld < _feldFlaeche.Count ? _feldFlaeche[feld] : 0;

        /// <summary>Ein Kollektorfeld für den Testeinstieg <see cref="Vorbereiten_Testfelder"/>.</summary>
        internal sealed class Testfeld
        {
            /// <summary>Anlagen-ID des Felds (Schlüssel der Senkenliste).</summary>
            public int ID_Anlage;
            /// <summary>Bruttopotenzial je Stunde [kWh]; <c>null</c> = 0.</summary>
            public double[] Potenzial;
            /// <summary>Pumpenleistung [W]; <c>null</c> = nicht gepflegt.</summary>
            public double? PumpenleistungW;
            /// <summary>Hilfsenergieanteil [%]; <c>null</c> = nicht gepflegt.</summary>
            public double? HilfsenergieAnteil;
            /// <summary>
            /// Mit Wert: das Feld rechnet über die Kennlinie (ST2) — konstante Gesamtstrahlung
            /// [W/m², ganz direkt, senkrecht] je Stunde; <see cref="Potenzial"/> gilt dann nicht.
            /// </summary>
            public double? Strahlung;
            /// <summary>Außentemperatur der Kennlinienstunden [°C].</summary>
            public double Aussen = 10;
            /// <summary>Kennwerte der Kennlinie.</summary>
            public double H0 = 0.8, K1 = 3.0, K2 = 0.01, Kdir50 = 0.95;
            /// <summary>Rechnende Fläche [m²].</summary>
            public double Flaeche = 10;
            /// <summary>Arbeitstemperatur aus dem Speicher (ST2)?</summary>
            public bool AusSpeicher;
            /// <summary>Grädigkeit und Spreizung [K]; <c>null</c> = Vorgabe.</summary>
            public double? GraedigkeitK, SpreizungK;
        }

        /// <summary>
        /// Testeinstieg ohne Datenbank für den Solarkreis (Welle M2): Felder mit vorgegebenem
        /// Potenzial und Pumpenangaben, gerechnet über dieselben Stundenschritte wie im Lauf.
        /// </summary>
        internal void Vorbereiten_Testfelder(IList<Testfeld> testfelder, List<Senkenliste> senken)
        {
            Init();
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);

            List<SolarFeld> felder = new List<SolarFeld>();
            foreach (Testfeld t in testfelder)
            {
                SolarFeld f = new SolarFeld { ID_Anlage = t.ID_Anlage, Name = "Feld " + t.ID_Anlage,
                                              Anlagenname = "Anlage " + t.ID_Anlage, Stunden = 8760,
                                              PumpenleistungW = t.PumpenleistungW,
                                              HilfsenergieAnteil = t.HilfsenergieAnteil };
                if (t.Potenzial != null) Array.Copy(t.Potenzial, f.Potenzial, Math.Min(8760, t.Potenzial.Length));
                if (t.Strahlung.HasValue)
                {
                    var m = new Kollektorstunden
                    {
                        H0 = t.H0, K1 = t.K1, K2 = t.K2, Kdir50 = t.Kdir50,
                        Verlustfaktor = Solarkreis.Verlustfaktor(null),
                        AusSpeicher = t.AusSpeicher,
                        GraedigkeitK = Solarkreis.Graedigkeit(t.GraedigkeitK),
                        SpreizungK = Solarkreis.Spreizung(t.SpreizungK)
                    };
                    for (int h = 0; h < 8760; h++)
                    {
                        m.Gesamt[h] = t.Strahlung.Value;
                        m.Direkt[h] = t.Strahlung.Value;
                        m.Aussen[h] = t.Aussen;
                        m.CosTheta[h] = 1.0;
                    }
                    f.Modell = m;
                    f.Flaeche = t.Flaeche;
                }
                felder.Add(f);
            }

            Ganglinie = SolarganglinieWeiche.Keine();
            RechnetGanglinie = false;
            FelderUebernehmen(felder, senken);
        }

        /// <summary>
        /// Senkenliste einer Anlage; ohne Zeile gilt die Rang-1-Invariante
        /// Heizkreis/Beides — dieselbe Regel wie beim Kontextaufbau der Wärmepumpe
        /// (Konzept 4.6/5.1).
        /// </summary>
        private static Senkenliste SenkeZuAnlage(List<Senkenliste> senken, int idAnlage)
        {
            if (senken != null)
                foreach (Senkenliste s in senken)
                    if (s != null && s.AnlagenID == idAnlage) return s;

            return Senkenliste.Vorbelegung(idAnlage);
        }

        /// <summary>
        /// Stundenbeginn: das Potenzial der Stunde steht jedem Feld voll zur Verfügung,
        /// und der STUFENEINGANG wird festgehalten.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N1: Der Stufeneingang ist der Kanalstand VOR der
        /// Vorabentladung (Phase A) — dieselbe Bezugsgröße wie bei der
        /// Wärmepumpe. Vorher stand er in <see cref="Stunde_Bedarf"/>, also nach Phase A.
        ///
        /// <para>PAKET K2: Der Stufeneingang ist die Summe ÜBER ALLE Kanäle des
        /// Restbedarfsfeldes — ohne Prozesswärmeanteil Zeichen für Zeichen die bisherige
        /// Größe <c>rest_heiz + rest_ww</c>.</para>
        /// </summary>
        public void Stunde_Start(int stunde, double[] rest)
        {
            for (int f = 0; f < _restPotenzial.Length; f++)
            {
                // ST2 (Welle M2): das Potenzial der Stunde aus der Kennlinie, mit der
                // Arbeitstemperatur dieser Stunde - fest 50 °C oder aus dem Speicherzustand der
                // Vorstunde. Ganglinie und Testfelder bringen ihr Potenzial mit.
                if (stunde >= 0 && stunde < 8760 && _modell[f] != null)
                    _potenzialFeld[f][stunde] = PotenzialStunde(f, stunde);

                _restPotenzial[f] = (stunde >= 0 && stunde < 8760) ? _potenzialFeld[f][stunde] : 0;
                _abgabeStunde[f] = 0;
            }

            double eingang = Kaskadenschleife.RestSumme(rest);
            if (eingang < 0) eingang = 0;
            if (stunde >= 0 && stunde < 8760) Waermebedarf[stunde] = eingang;
        }

        /// <summary>
        /// DAS POTENZIAL EINER STUNDE [kWh] — Schritte 1 und 2 des Kollektormodells (ST2, Welle M2):
        /// spezifische Leistung nach der Kennlinie (EN ISO 9806, mit getrennter Diffuskorrektur nach
        /// ST5, wenn der Satz K_dfu führt) bei der Arbeitstemperatur der Stunde, mal rechnender
        /// Fläche, mal Faktor nach den Verlusten des Solarkreises.
        ///
        /// <para><b>Fest 50 °C rechnet Anweisung für Anweisung wie der Jahresvorlauf vor der
        /// Welle</b> (dieselbe Funktion, dieselben Operanden) — das Ergebnis ist byte-gleich.</para>
        /// </summary>
        private double PotenzialStunde(int f, int stunde)
        {
            Kollektorstunden m = _modell[f];
            if (stunde >= _stunden[f]) return 0;

            double tMittel = Arbeitstemperatur(f, stunde);

            double leistungProQm = m.MitKdfu
                ? Solarkreis.LeistungJeQm(m.Direkt[stunde], m.DiffusReflex[stunde], m.Aussen[stunde], tMittel,
                                          m.CosTheta[stunde], m.H0, m.K1, m.K2, m.Kdir50, m.Kdfu)
                : CalculateThermalPower(m.Gesamt[stunde], m.Aussen[stunde], tMittel, m.CosTheta[stunde],
                                        m.H0, m.K1, m.K2, m.Kdir50);
            double potenzial = (leistungProQm * _feldFlaeche[f] * m.Verlustfaktor) / 1000.0;

            if (potenzial > 0) _tMittelGewichtet[f] += tMittel * potenzial;
            return potenzial;
        }

        /// <summary>
        /// DIE ARBEITSTEMPERATUR eines Felds in einer Stunde [°C] — die mittlere Fluidtemperatur
        /// ϑ_m, auf die sich die Kennlinie bezieht (EN ISO 9806; EN 15316-4-3).
        ///
        /// <para><b>Fest</b> (Vorgabe): 50 °C für das ganze Jahr. <b>Aus dem Speicher</b> (ST2 mit
        /// ST4): <c>ϑ_ein = ϑ_unten + ΔT_WT</c>, <c>ϑ_m = ϑ_ein + ΔT_Koll/2</c>. ϑ_unten ist die
        /// Temperatur der untersten Zone des Senkenpuffers am Ende der Vorstunde
        /// (<see cref="SimulationPufferspeicher.T_unten"/>; bei einer Zone
        /// <c>ϑ_RL + SOC/Q_max · (ϑ_VL − ϑ_RL)</c>), ohne Puffer der gerechnete Rücklauf des
        /// Heizkreises (Anlagenkopplung). Ohne beides rechnet das Feld fest und sagt es.</para>
        /// </summary>
        private double Arbeitstemperatur(int f, int stunde)
        {
            Kollektorstunden m = _modell[f];
            if (m == null || !m.AusSpeicher) return Solarkreis.ARBEITSTEMPERATUR_FEST_C;

            double unten = double.NaN;
            SimulationPufferspeicher sp = _temperaturSpeicher[f];
            if (sp != null) unten = sp.T_unten;
            else if (Heizkreisruecklauf != null && stunde >= 0 && stunde < Heizkreisruecklauf.Length)
                unten = Heizkreisruecklauf[stunde];

            if (double.IsNaN(unten) || double.IsInfinity(unten))
            {
                SimulationProtokoll.Aktuell.HinweisEinmal("solar-arbeitstemperatur-fest-" + f,
                    "Solarthermie: Das Kollektorfeld „" + _feldName[f] + "“ soll seine Arbeitstemperatur aus " +
                    "dem Speicher bilden, lädt aber keinen Puffer und hat keinen gerechneten Heizkreisrücklauf - " +
                    "es rechnet mit der festen Arbeitstemperatur " +
                    Solarkreis.ARBEITSTEMPERATUR_FEST_C.ToString("0", CultureInfo.CurrentCulture) + " °C.");
                return Solarkreis.ARBEITSTEMPERATUR_FEST_C;
            }

            return Solarkreis.MittlereFluidtemperatur(unten, m.GraedigkeitK, m.SpreizungK);
        }

        /// <summary>
        /// Setzt den SENKENPUFFER eines Felds, aus dessen unterster Zone die Arbeitstemperatur kommt
        /// (ST2) — aufgerufen von <c>SimulationControl</c>, sobald die Speicher-Registry offen ist.
        /// Ohne Arbeitstemperatur aus dem Speicher wirkungslos.
        /// </summary>
        public void TemperaturSpeicherSetzen(int feld, SimulationPufferspeicher speicher)
        {
            if (feld < 0 || feld >= _temperaturSpeicher.Length) return;
            if (_modell[feld] == null || !_modell[feld].AusSpeicher) return;
            _temperaturSpeicher[feld] = speicher;
        }

        /// <summary>Bildet das Feld <paramref name="feld"/> seine Arbeitstemperatur aus dem Speicher (ST2)?</summary>
        public bool ArbeitstemperaturAusSpeicher(int feld)
            => feld >= 0 && feld < _modell.Length && _modell[feld] != null && _modell[feld].AusSpeicher;

        /// <summary>Der Senkenpuffer, aus dem das Feld seine Arbeitstemperatur bildet; <c>null</c> = keiner.</summary>
        public SimulationPufferspeicher TemperaturSpeicher(int feld)
            => feld >= 0 && feld < _temperaturSpeicher.Length ? _temperaturSpeicher[feld] : null;

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Solarthermie: Die
        /// Felder mit Hauptsenke HEIZKREIS decken den Momentanbedarf ihres Kanals.
        ///
        /// Ein Feld mit Puffer-Hauptsenke deckt hier NICHTS — es lädt ausschließlich
        /// (Phase C). Daraus folgt derselbe Doppelzählungs-Freibeweis wie bei der
        /// Wärmepumpe: Eine Anlage ist eindeutig in Phase B ODER in Phase C.
        ///
        /// <para>PAKET K2: <paramref name="rest"/> ist der offene Bedarf je Kanal und
        /// tritt an die Stelle des Paares <c>ref rest_heiz, ref rest_ww</c>; es wird
        /// IN-PLACE fortgeschrieben. Die Bezugsgröße <c>verfuegbar</c> kommt nicht mehr
        /// aus einer eigenen Dreifach-Verzweigung über <c>WS_Typ</c>, sondern aus
        /// <see cref="Kanalabzug.Offen"/> — derselben Quelle, gegen die gleich abgezogen
        /// wird (Konzept 4.3).</para>
        /// </summary>
        public void Stunde_Bedarf(int stunde, double[] rest)
        {
            // Der Stufeneingang steht seit der Nacharbeit N1 in Stunde_Start - VOR der
            // Vorabentladung (Phase A).
            for (int f = 0; f < _restPotenzial.Length; f++)
            {
                if (_restPotenzial[f] <= 0) continue;

                // PAKET S1: Gefragt wird die DIREKTSENKEN-KETTE des Felds (Konzept 5.2).
                // Ein Feld ganz ohne Direktsenke lädt ausschließlich und deckt hier
                // nichts - das ist die Nachfolge der Prüfung „Hauptsenke != Heizkreis".
                Senkenliste senken = _feldSenke[f];
                if (senken != null && !senken.HatDirektsenke) continue;

                double verfuegbar = Kanalabzug.Offen(senken, rest);

                if (verfuegbar <= 0) continue;

                double prod = Math.Min(_restPotenzial[f], verfuegbar);
                if (prod <= 0) continue;

                // K2: Abzug über die eine Kanalregel, mit gemessener Aufschlüsselung je
                // Kanal (Konzept 4.4). Die abgezogene Gesamtmenge ist konstruktiv genau
                // "prod" - sie ist auf den offenen Kanalbedarf begrenzt.
                //
                // PAKET E2: derselbe Abzug schreibt zusätzlich die Kanalganglinie der
                // Stunde - aus derselben gemessenen rest-Differenz.
                Kanalabzug.Abziehen(senken, prod, rest, Direktdeckung_Kanal,
                                    Direktdeckung_KanalStuendlich, stunde);

                _restPotenzial[f] -= prod;
                _prodFeld[f] += prod;
                _abgabeStunde[f] += prod;
                DirektdeckungGesamtKwh += prod;
                if (stunde >= 0 && stunde < 8760) Waermeproduktion[stunde] += prod;
            }

            if (stunde >= 0 && stunde < 8760) Restwaerme[stunde] = Kaskadenschleife.RestSumme(rest);
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.3/6.4): Der Überschuss des Felds
        /// geht in den zugeordneten Puffer — Hauptsenke zuerst, die Zweitsenke bekommt
        /// nur, was danach übrig ist (Konzept 13.5, Variante A; die Ladephase ruft diese
        /// Methode zuerst für alle Haupt- und danach für alle Zweitsenken auf).
        ///
        /// KEIN <c>SenkeAbziehen</c> — die geladene Wärme deckt keinen Bedarf, sie liegt
        /// im Speicher. Der Bilanzraum aus der Nutzerentscheidung zu 4b-1 gilt unverändert:
        /// Die Aufnahme darf die freie Kapazität um die im selben Zeitschritt absehbare
        /// Entnahme übersteigen, und dieses Durchsatzbudget wird je Kanal nur einmal
        /// vergeben.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss, double[] absehbar)
        {
            if (a == null || a.Speicher == null) return 0;

            int f = a.Modulindex;
            if (f < 0 || f >= _restPotenzial.Length) return 0;
            if (_restPotenzial[f] <= 0) return 0;

            SimulationPufferspeicher sp = a.Speicher;

            // D5a: Beim KOMBISPEICHER ist das Durchsatzbudget die Summe beider Kanäle —
            // die gemeinsame Fassung steht in der Kaskadenschleife und liefert ohne
            // Kombispeicher Anweisung für Anweisung das Bisherige.
            double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
            double durchlass = Kaskadenschleife.DurchlassBudget(sp, absehbar);
            if (ladefaehig + durchlass <= 0) return 0;

            double menge = Math.Min(_restPotenzial[f], ladefaehig + durchlass);
            if (menge <= 0) return 0;

            double ladung = sp.Laden(menge, stunde, durchlass);
            if (ladung <= 0) return 0;

            double genutzterDurchlass = ladung - ladefaehig;
            if (genutzterDurchlass > 0)
                Kaskadenschleife.DurchlassBuchen(sp, absehbar, genutzterDurchlass);

            _restPotenzial[f] -= ladung;
            _prodFeld[f] += ladung;
            _abgabeStunde[f] += ladung;
            SpeicherladungGesamtKwh += ladung;
            if (stunde >= 0 && stunde < 8760)
            {
                Waermeproduktion[stunde] += ladung;
                Speicherladung_stuendlich[stunde] += ladung;
            }

            return ladung;
        }

        /// <summary>
        /// Stundenende: Was weder den Bedarf gedeckt hat noch in einen Speicher passte,
        /// ist VERWORFEN und wird als Überschuss gebucht — die Größe, die vor Paket 5 der
        /// gesamte Überschuss war (Kappungspunkt in <see cref="BerechneSolarthermie"/>).
        /// </summary>
        public void Stunde_Ende(int stunde)
        {
            for (int f = 0; f < _restPotenzial.Length; f++)
            {
                // ST1: der Pumpenstrom der Stunde - nur, wenn das Feld Wärme abgegeben hat.
                double pumpe = Solarkreis.PumpenstromKwh(_pumpeW[f], _hilfsAnteil[f], _abgabeStunde[f]);
                if (pumpe > 0)
                {
                    _pumpeFeld[f] += pumpe;
                    if (stunde >= 0 && stunde < 8760) Pumpenstrom_stuendlich[stunde] += pumpe;
                }

                double rest = _restPotenzial[f];
                if (rest <= 0) continue;

                _ueberFeld[f] += rest;
                if (stunde >= 0 && stunde < 8760) Ueberschuss[stunde] += rest;
                _restPotenzial[f] = 0;
            }
        }

        /// <summary>
        /// Die mittlere Arbeitstemperatur eines Felds über das Jahr, gewichtet mit dem Potenzial
        /// der Stunden [°C]; NaN ohne Kennlinie oder ohne Potenzial.
        /// </summary>
        private double ArbeitstemperaturMittel(int f)
        {
            if (f >= _modell.Length || _modell[f] == null) return double.NaN;
            double summe = _potenzialFeld[f].Sum();
            return summe > 0 ? _tMittelGewichtet[f] / summe : double.NaN;
        }

        /// <summary>Jahressummen und Feldauflistung des zweikanaligen Wegs.</summary>
        public void Abschluss_Zweikanalig()
        {
            Kollektor_Ergebnisse.Clear();
            for (int f = 0; f < _feldName.Count; f++)
            {
                Kollektor_Ergebnisse.Add(new SolarKollektorErgebnis
                {
                    Name = _feldName[f],
                    Flaeche = _feldFlaeche[f],
                    Anzahl = _feldAnzahl[f],
                    WaermeproduktionKwh = _prodFeld[f],
                    UeberschussKwh = _ueberFeld[f],
                    PumpenstromKwh = _pumpeFeld[f],
                    ArbeitstemperaturMittelC = ArbeitstemperaturMittel(f),
                    ArbeitstemperaturAusSpeicher = ArbeitstemperaturAusSpeicher(f),
                    IstGanglinie = f < _feldGanglinie.Count && _feldGanglinie[f]
                });
            }

            PumpenstromGesamtKwh = Pumpenstrom_stuendlich.Sum();

            WaermebedarfGesamtKwh = Waermebedarf.Sum();
            Max_Waermebedarf = Waermebedarf.Max();
            WaermeproduktionGesamtKwh = Waermeproduktion.Sum();
            Waermeproduktion_max = Waermeproduktion.Max();
            UeberschussSummeKwh = Ueberschuss.Sum();
            RestwaermeSummeKwh = Restwaerme.Sum();
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte, aber in
        /// einer eigenen Jahresschleife an der Kaskadenposition der Solarthermie.
        ///
        /// Sie ist der Weg für Projekte, in denen kein Kollektorfeld eine Puffer-Senke
        /// trägt. Ohne Speicher gibt es nichts zu ordnen: Die Phasen A, C, D, E und G
        /// haben für diese Stufe keinen Inhalt, und die Stufe bleibt ein Vektormodul an
        /// ihrer Kaskadenposition. Sie rechnet KANALGERECHT — die Anlage deckt ihren
        /// Kanal nach <c>WS_Typ</c> (bei „Beides" mit Warmwasservorrang), statt wie bis
        /// Paket 5 auf der Kanalsumme.
        /// </summary>
        public bool Berechnung_Zweikanalig(int ID_Projekt, Kanalsatz kanaele,
                                           List<Senkenliste> senken)
        {
            if (kanaele == null) return false;
            if (!Vorbereiten_Zweikanalig(ID_Projekt, senken)) return false;

            double[] rest = new double[Kanal.ANZAHL];

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                // Kuehlkonzept 4.2 (F-K4): nur die Waermekanaele - der Kuehlkanal bleibt in
                // rest[] 0 und im Kanalsatz unberuehrt; kein Waermeerzeuger sieht Kaeltebedarf.
                foreach (int k in Kanal.KANAELE_WAERME) rest[k] = kanaele.Bedarf[k][stunde];

                // Ohne Speicher gibt es keine Vorabentladung: Der Stufeneingang ist der
                // Kanalstand an dieser Kaskadenposition.
                Stunde_Start(stunde, rest);
                Stunde_Bedarf(stunde, rest);
                Stunde_Ende(stunde);

                foreach (int k in Kanal.KANAELE_WAERME)
                    kanaele.Bedarf[k][stunde] = (double)rest[k];
            }

            Abschluss_Zweikanalig();
            return true;
        }
    }

    // Ergebnis eines einzelnen Solarkollektor(felds) fuer die Ergebnis-Auflistung.
    public class SolarKollektorErgebnis
    {
        public string Name = "";
        public double Flaeche;          // rechnende Kollektorflaeche gesamt (m^2) = Bezugsflaeche eines Moduls (Apertur oder Brutto) * Anzahl
        public long Anzahl;
        public double WaermeproduktionKwh; // kWh/a
        public double UeberschussKwh;      // kWh/a

        /// <summary>Pumpenstrom des Solarkreises [kWh/a] (ST1); 0 ohne gepflegte Pumpe.</summary>
        public double PumpenstromKwh;

        /// <summary>
        /// Mittlere Arbeitstemperatur des Felds [°C], gewichtet mit dem Potenzial der Stunden (ST2);
        /// fest 50 °C, aus dem Speicher der Mittelwert des Laufs; NaN für die Ganglinie.
        /// </summary>
        public double ArbeitstemperaturMittelC = double.NaN;

        /// <summary>Bildet das Feld seine Arbeitstemperatur aus dem Speicher (ST2)?</summary>
        public bool ArbeitstemperaturAusSpeicher;

        /// <summary>
        /// true für die Zeile der Solarthermieganglinie (Folgeauftrag 4): Sie hat keine
        /// Fläche und keine Anzahl — <see cref="Flaeche"/> und <see cref="Anzahl"/> stehen
        /// auf 0 und werden nicht angezeigt.
        /// </summary>
        public bool IstGanglinie;

        /// <summary>Jahresertrag [kWh/a] = genutzte Wärme + Überschuss.</summary>
        public double JahresertragKwh => WaermeproduktionKwh + UeberschussKwh;

        /// <summary>Nutzanteil [%] = genutzte Wärme / Jahresertrag; <c>null</c> ohne Ertrag.</summary>
        public double? NutzanteilProzent
            => JahresertragKwh > 0 ? WaermeproduktionKwh / JahresertragKwh * 100.0 : (double?)null;
    }
}