using System;
using System.Collections.Generic;
using System.Data.OleDb;
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

        public double Waermeproduktion_gesamt = 0;
        public double Waermebedarf_gesamt = 0;
        public double Max_Waermebedarf;

        public double[] Waermebedarf = new double[8760];
        public double[] Restwaerme = new double[8760];
        public double[] Waermeproduktion = new double[8760];
        public double[] Ueberschuss = new double[8760];

        public double Lon = 0;
        public double Lat = 0;
        public double Ueberschuss_summe = 0;
        public double Waermeproduktion_max = 0;
        public double Restwaerme_summe = 0;

        // ===================================================================
        // Zweikanaliger Weg (Paket 5 - Konzept 6.4)
        // ===================================================================

        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je Kollektorfeld, INDEXGLEICH zur Reihenfolge der
        /// Felder im zweikanaligen Weg (Konzept 6.2). Nur dort gefüllt; der einkanalige
        /// Altpfad braucht sie nicht.
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

        /// <summary>Jahressumme der Speicherladung [kWh]; im Altpfad immer exakt 0.</summary>
        public double Speicherladung_gesamt = 0;

        /// <summary>
        /// Anteil der Produktion, der den Momentanbedarf DIREKT deckt [kWh].
        /// <c>Waermeproduktion_gesamt = Direktdeckung_gesamt + Speicherladung_gesamt</c>.
        /// </summary>
        public double Direktdeckung_gesamt = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        ///
        /// Gefüllt von <see cref="Kaskadenschleife"/>; im Altpfad und ohne Puffer-Senke
        /// exakt 0. Direktdeckung PLUS dieser Anteil ist der EIGENANTEIL der Solarthermie
        /// an der Bedarfsdeckung — die Größe hinter
        /// <c>Tab_ErgebnisSolarthermie.Waermebedarfsdeckung</c>.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        // ------------------------------------------------------------------
        // KANALINDIZIERTE DECKUNGSBUCHFÜHRUNG (Paket K2, Konzept 4.4)
        //
        // ZUSÄTZLICHE Aufschlüsselung, kein Ersatz: Direktdeckung_gesamt,
        // Speicherladung_gesamt, Speicherentladung_Anteil und die Ganglinien
        // werden unverändert gebildet und gelesen. Es gilt
        //
        //   Σ Direktdeckung_Kanal[k]     == Direktdeckung_gesamt
        //   Σ Speicherentladung_Kanal[k] == Speicherentladung_Anteil
        //
        // bis auf die Rundungsklasse der getrennten Kanalarithmetik.
        // ------------------------------------------------------------------

        /// <summary>
        /// Direkt gedeckter Momentanbedarf je Kanal [kWh] (Phase B) — die Aufschlüsselung
        /// von <see cref="Direktdeckung_gesamt"/>.
        /// </summary>
        public double[] Direktdeckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Anteil dieses Erzeugers an der bedarfsdeckenden Speicherentladung je Kanal
        /// [kWh] — die Aufschlüsselung von <see cref="Speicherentladung_Anteil"/>.
        /// Gefüllt von der <see cref="Kaskadenschleife"/>, wie der Skalar selbst.
        /// </summary>
        public double[] Speicherentladung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>Potenzieller Bruttoertrag je Kollektorfeld und Stunde [kWh].</summary>
        private double[][] _potenzialFeld = new double[0][];

        /// <summary>Noch nicht untergebrachtes Potenzial je Feld in der laufenden Stunde [kWh].</summary>
        private double[] _restPotenzial = new double[0];

        /// <summary>Jahressumme des nutzbaren Ertrags je Feld [kWh] (Deckung + Ladung).</summary>
        private double[] _prodFeld = new double[0];

        /// <summary>Jahressumme des verworfenen Überschusses je Feld [kWh].</summary>
        private double[] _ueberFeld = new double[0];

        private readonly List<string> _feldName = new List<string>();
        private readonly List<double> _feldFlaeche = new List<double>();
        private readonly List<long> _feldAnzahl = new List<long>();
        private readonly List<Senkenliste> _feldSenke = new List<Senkenliste>();

        /// <summary>Anzahl der Kollektorfelder des zweikanaligen Wegs.</summary>
        public int FelderAnzahl { get { return _feldName.Count; } }

        /// <summary>Senkenliste eines Kollektorfelds (nie null nach dem Aufbau, Paket S1).</summary>
        public Senkenliste FeldSenke(int index)
        {
            if (index < 0 || index >= _feldSenke.Count) return null;
            return _feldSenke[index];
        }

        public bool Berechnung(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt;

            // 1./2. ID_Klimaregion und Geokoordinaten — EINE Fassung für beide Rechenwege
            // (Paket-5-Nacharbeit, Befund N6/N9).
            KlimaregionUndGeoLesen();

            Init();

            // 3. Wärmebedarf initialisieren
            Waermebedarf_gesamt = Waermebedarf.Sum();
            Max_Waermebedarf = Waermebedarf.Max();

            // 4. Kollektorfelder samt STÜNDLICHEM POTENZIAL einlesen — Schritte 1 und 2
            // des Kollektormodells, gemeinsam mit dem zweikanaligen Weg (N6).
            List<SolarFeld> felder = Kollektorfelder_Lesen();

            for (int i = 0; i < 8760; i++) Restwaerme[i] = Waermebedarf[i];

            // 5. Bilanzierung je Feld — der KAPPUNGSPUNKT des Altpfads: Was über den
            // Momentanbedarf hinausgeht, ist verworfen (im zweikanaligen Weg darf es
            // stattdessen einen Puffer laden, Konzept 6.4).
            for (int n = 0; n < felder.Count; n++)
            {
                SolarFeld f = felder[n];

                // Jahressummen dieses Kollektor(felds) fuer die Auflistung.
                double prodSummeKoll = 0;
                double ueberSummeKoll = 0;

                for (int i = 0; i < f.Stunden; i++)
                {
                    var (prod, rest, ueber) = Bilanzieren(Restwaerme[i], f.Potenzial[i]);

                    // Ergebnisse aufsummieren (für mehrere Kollektorfelder)
                    Waermeproduktion[i] += prod;
                    Restwaerme[i] = rest; // Restwärme wird pro Zeitschritt überschrieben
                    Ueberschuss[i] += ueber;

                    prodSummeKoll += prod;
                    ueberSummeKoll += ueber;
                }

                Kollektor_Ergebnisse.Add(new SolarKollektorErgebnis
                {
                    Name = f.Name,
                    Flaeche = f.Flaeche,
                    Anzahl = f.Anzahl,
                    Waermeproduktion = prodSummeKoll,
                    Ueberschuss = ueberSummeKoll
                });
            }

            Waermeproduktion_gesamt = Waermeproduktion.Sum();
            Waermeproduktion_max = Waermeproduktion.Max();
            Ueberschuss_summe = Ueberschuss.Sum();
            Restwaerme_summe = Restwaerme.Sum();

            return true;
        }

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
            /// <summary>Aperturfläche gesamt [m²] = Modulfläche · Anzahl.</summary>
            public double Flaeche;
            public long Anzahl;
            /// <summary>Zahl der ausgewerteten Stunden (Zeilen der Klimadaten, höchstens 8760).</summary>
            public int Stunden;
            /// <summary>Potenzieller Bruttoertrag je Stunde [kWh].</summary>
            public double[] Potenzial = new double[8760];
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
                                       StilleDb.Par("@id", OleDbType.Integer, m_ID_Projekt));
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
        /// Liest die Kollektorfelder des Projekts und rechnet ihr STÜNDLICHES POTENZIAL
        /// für das ganze Jahr — die Schritte 1 und 2 des Kollektormodells (spezifische
        /// Leistung, potenzielle Erzeugung), also alles, was NICHT vom Wärmebedarf und
        /// nicht vom Speicherfüllstand abhängt.
        ///
        /// EINE Fassung für beide Rechenwege (Paket-5-Nacharbeit, Befund N6): Bis dahin
        /// stand dieser Block zweimal im Modul — einmal in <see cref="Berechnung"/>,
        /// einmal in <see cref="Vorbereiten_Zweikanalig"/>. Term für Term waren beide
        /// gleich, aber ein Fix am Altpfad hätte im neuen Weg nicht gewirkt, und die
        /// Regressionssuite (Flag aus) hätte das nie gemeldet. Zwei Abweichungen waren
        /// bereits entstanden: die Stundenzahl (<c>rows</c> gegen
        /// <c>Math.Min(rows, 8760)</c>) — hier auf die abgesicherte Fassung vereinheitlicht,
        /// die zugleich einen möglichen Indexüberlauf des Altpfads schließt.
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
                double nFlaeche = ctrlsol.m_Aperturfläche;

                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");

                // Konstanten für das Kollektormodell
                double h0 = ctrlsol.m_h0;
                double k1 = ctrlsol.m_k1;
                double k2 = ctrlsol.m_k2;
                double kdir50 = ctrlsol.m_Kdir;
                double tStorage = 50; // Annahme Speichertemperatur
                double leitungsverluste = 0.92;

                SolarFeld f = new SolarFeld();
                f.ID_Anlage = ctrl.items[n].ID;
                f.Name = ctrlsol.m_szKollektorname;
                f.Flaeche = nFlaeche * nAnzahl;
                f.Anzahl = nAnzahl;
                f.Stunden = Math.Min(ctrldat.rows, 8760);

                for (int i = 0; i < f.Stunden; i++)
                {
                    // CalculateHourly berechnet bereits die effektive Strahlung auf der geneigten Fläche [cite: 52, 69, 71]
                    double gTilted = SolarCalculator.CalculateHourly(
                        Lon, Lat, nNeigung, nAzimuth,
                        ctrldat.items[i].Globalstrahlung,
                        ctrldat.items[i].Direktstrahlung,
                        ctrldat.items[i].Diffusstrahlung,
                        ctrldat.items[i].Außen_Temp,
                        i / 24, i % 24);

                    double ta = ctrldat.items[i].Außen_Temp;

                    // WICHTIG: cosTheta für IAM-Berechnung sauber ermitteln
                    // Wir nutzen hier den internen Wert aus dem Calculator
                    double currentCosTheta = SolarCalculator.lastCosTheta;

                    // Schritt 1: spezifische Leistung [W/m²], Schritt 2: Bruttoertrag [kWh]
                    double leistungProQm = CalculateThermalPower(gTilted, ta, tStorage, currentCosTheta,
                                                                 h0, k1, k2, kdir50);
                    f.Potenzial[i] = (leistungProQm * f.Flaeche * leitungsverluste) / 1000.0;
                }

                felder.Add(f);
            }

            return felder;
        }

        public void Init()
        {
            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Waermeproduktion, 0, Waermeproduktion.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            Waermeproduktion_gesamt = 0;
            Ueberschuss_summe = 0;
            Kollektor_Ergebnisse.Clear();

            // Zweikanaliger Weg (Paket 5) - im Altpfad bleiben alle Größen auf 0, damit
            // die Mitkorrektur in SimulationRunner dort nachweislich wirkungslos ist.
            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            Speicherladung_gesamt = 0;
            Direktdeckung_gesamt = 0;
            Speicherentladung_Anteil = 0;

            // K2: die Kanalaufschlüsselung derselben Größen (Konzept 4.4).
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);
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
            double thetaRad = Math.Acos(Math.Min(Math.Max(cosTheta, 0), 1));

            // Physikalische b0-Näherung für Flachkollektoren
            double cos50 = Math.Cos(50.0 * Math.PI / 180.0);
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
        /// Baut die Kollektorfelder des zweikanaligen Wegs auf und bestimmt ihr
        /// STÜNDLICHES POTENZIAL für das ganze Jahr.
        ///
        /// Der Bruttoertrag eines Kollektorfelds hängt ausschließlich vom Wetter, von der
        /// Ausrichtung und von den Kollektorkennwerten ab — nicht vom Wärmebedarf und
        /// nicht vom Speicherfüllstand (siehe <see cref="BerechneSolarthermie"/>: die
        /// Bilanzierung in Schritt 3 kappt nur, was Schritt 2 vorher unabhängig davon
        /// gerechnet hat). Genau deshalb lässt sich die Solarthermie überhaupt in die
        /// Stundenschleife der Kaskade einfügen: Ihr Potenzial steht vorab fest, die
        /// Verwendung (Direktdeckung, Speicherladung, Verwurf) entscheidet sich erst in
        /// der Stunde.
        ///
        /// Gerechnet wird mit denselben Aufrufen und in derselben Reihenfolge wie in
        /// <see cref="Berechnung"/> — die Potenzialwerte sind damit dieselben Zahlen.
        /// </summary>
        /// <param name="senken">Geordnete Senkenlisten des Projekts (Konzept 5.1).</param>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenliste> senken)
        {
            m_ID_Projekt = ID_Projekt;

            // Klimaregion, Geokoordinaten und Kollektorfelder samt Potenzial kommen aus
            // denselben Methoden wie im Altpfad (Paket-5-Nacharbeit, Befund N6) — damit
            // ist „dieselben Aufrufe, dieselbe Reihenfolge, dieselben Zahlen" nicht mehr
            // eine Zusage über zwei Kopien, sondern dieselbe Anweisungsfolge.
            KlimaregionUndGeoLesen();

            Init();
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);

            solar_anlagen_ids.Clear();
            _feldName.Clear();
            _feldFlaeche.Clear();
            _feldAnzahl.Clear();
            _feldSenke.Clear();

            List<SolarFeld> felder = Kollektorfelder_Lesen();
            List<double[]> potenziale = new List<double[]>();

            for (int n = 0; n < felder.Count; n++)
            {
                SolarFeld f = felder[n];

                potenziale.Add(f.Potenzial);
                solar_anlagen_ids.Add(f.ID_Anlage);
                _feldName.Add(f.Name);
                _feldFlaeche.Add(f.Flaeche);
                _feldAnzahl.Add(f.Anzahl);
                _feldSenke.Add(SenkeZuAnlage(senken, f.ID_Anlage));
            }

            _potenzialFeld = potenziale.ToArray();
            _restPotenzial = new double[_potenzialFeld.Length];
            _prodFeld = new double[_potenzialFeld.Length];
            _ueberFeld = new double[_potenzialFeld.Length];

            return true;
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
        /// Vorabentladung (Phase A) — dieselbe Bezugsgröße wie im Altpfad und bei der
        /// Wärmepumpe. Vorher stand er in <see cref="Stunde_Bedarf"/>, also nach Phase A.
        ///
        /// <para>PAKET K2: Der Stufeneingang ist die Summe ÜBER ALLE Kanäle des
        /// Restbedarfsfeldes — ohne Prozesswärmeanteil Zeichen für Zeichen die bisherige
        /// Größe <c>rest_heiz + rest_ww</c>.</para>
        /// </summary>
        public void Stunde_Start(int stunde, double[] rest)
        {
            for (int f = 0; f < _restPotenzial.Length; f++)
                _restPotenzial[f] = (stunde >= 0 && stunde < 8760) ? _potenzialFeld[f][stunde] : 0;

            double eingang = Kaskadenschleife.RestSumme(rest);
            if (eingang < 0) eingang = 0;
            if (stunde >= 0 && stunde < 8760) Waermebedarf[stunde] = eingang;
        }

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
                Kanalabzug.Abziehen(senken, prod, rest, Direktdeckung_Kanal);

                _restPotenzial[f] -= prod;
                _prodFeld[f] += prod;
                Direktdeckung_gesamt += prod;
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
            Speicherladung_gesamt += ladung;
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
                double rest = _restPotenzial[f];
                if (rest <= 0) continue;

                _ueberFeld[f] += rest;
                if (stunde >= 0 && stunde < 8760) Ueberschuss[stunde] += rest;
                _restPotenzial[f] = 0;
            }
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
                    Waermeproduktion = _prodFeld[f],
                    Ueberschuss = _ueberFeld[f]
                });
            }

            Waermebedarf_gesamt = Waermebedarf.Sum();
            Max_Waermebedarf = Waermebedarf.Max();
            Waermeproduktion_gesamt = Waermeproduktion.Sum();
            Waermeproduktion_max = Waermeproduktion.Max();
            Ueberschuss_summe = Ueberschuss.Sum();
            Restwaerme_summe = Restwaerme.Sum();
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte, aber in
        /// einer eigenen Jahresschleife an der Kaskadenposition der Solarthermie.
        ///
        /// Sie ist der Weg für Projekte, in denen kein Kollektorfeld eine Puffer-Senke
        /// trägt. Ohne Speicher gibt es nichts zu ordnen: Die Phasen A, C, D, E und G
        /// haben für diese Stufe keinen Inhalt, und die Stufe bleibt — wie im Altpfad —
        /// ein Vektormodul an ihrer Kaskadenposition. Der einzige Unterschied zum Altpfad
        /// ist die Kanalführung: Statt auf der Kanalsumme zu rechnen und den Rest über
        /// <c>Waermekanaele.Uebernehmen</c> proportional zurückzuverteilen, deckt die
        /// Anlage ihren Kanal nach <c>WS_Typ</c> (bei „Beides" mit Warmwasservorrang).
        /// </summary>
        public bool Berechnung_Zweikanalig(int ID_Projekt, Kanalsatz kanaele,
                                           List<Senkenliste> senken)
        {
            if (kanaele == null) return false;
            if (!Vorbereiten_Zweikanalig(ID_Projekt, senken)) return false;

            double[] rest = new double[Kanal.ANZAHL];

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                for (int k = 0; k < Kanal.ANZAHL; k++) rest[k] = kanaele.Bedarf[k][stunde];

                // Ohne Speicher gibt es keine Vorabentladung: Der Stufeneingang ist der
                // Kanalstand an dieser Kaskadenposition.
                Stunde_Start(stunde, rest);
                Stunde_Bedarf(stunde, rest);
                Stunde_Ende(stunde);

                for (int k = 0; k < Kanal.ANZAHL; k++)
                    kanaele.Bedarf[k][stunde] = (float)rest[k];
            }

            Abschluss_Zweikanalig();
            return true;
        }
    }

    // Ergebnis eines einzelnen Solarkollektor(felds) fuer die Ergebnis-Auflistung.
    public class SolarKollektorErgebnis
    {
        public string Name = "";
        public double Flaeche;          // Aperturflaeche gesamt (m^2) = Modulflaeche * Anzahl
        public long Anzahl;
        public double Waermeproduktion; // kWh/a
        public double Ueberschuss;      // kWh/a
    }
}