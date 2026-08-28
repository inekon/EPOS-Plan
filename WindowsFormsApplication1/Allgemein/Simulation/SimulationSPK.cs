using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WindowsFormsApplication1
{
    // Die fehleranfällige und pauschale Jahres-Verlustberechnung (mit den fiktiven Betriebsstunden und der asymmetrischen Bereitschaft)
    // wurde komplett entfernt. Stattdessen wird der Brennstoffverbrauch nun stündlich direkt in der Simulationsschleife ermittelt:
    //
    // - Läuft ein Kessel in einer Stunde, wird sein Verbrauch über den stündlichen Wirkungsgrad ermittelt.
    // - Steht er in einer Stunde still, wird ihm für diese exakte Stunde der anteilige Bereitschaftsverlust
    //   als Brennstoffverbrauch(Wärmeverlust) aufgeschlagen.
    //
    // Am Ende des Jahres wird der Jahresnutzungsgrad in Schritt 5 absolut präzise aus der summierten Nutzwärme und dem summierten Gesamtverbrauch gebildet.

    public class SimulationSPK
    {
        public const int MAX_SPK = 10;

        // Listen und Projektdaten
        public List<string> spk_list = new List<string>();

        public Dictionary<string, int> spk_carrier = new Dictionary<string, int>();


        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je Kessel, INDEXGLEICH zu <see cref="spk_list"/>
        /// (Konzept 6.2). Gefüllt von <c>SimulationControl.Simulation_SPK_Ctrl</c>.
        ///
        /// Warum eine zweite Liste statt einer Umstellung von <see cref="spk_list"/>:
        /// Der Bezeichner dort ist nicht nur Suchschlüssel der Kesseldaten, er ist
        /// zugleich der MODULNAME der Ergebniszeile (<c>SimulationRunner</c>). Eine
        /// Umstellung auf IDs hätte die Modulnamen aller Kesselergebnisse verändert.
        ///
        /// Gefüllt, aber noch von keinem Rechenpfad ausgewertet — auch der zweikanalige
        /// Weg wertet in Etappe 4b nur Wärmepumpen-Senken aus. Vorbereitung für
        /// Senkenauswertung und Ladepriorität je Kessel (Paket 5).
        /// </summary>
        public List<int> spk_anlagen_ids = new List<int>();

        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public float[] Waermebedarf = new float[8760];
        public float[] Restwaerme = new float[8760];
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] Stromverbrauch_stuendlich = new float[8760];
        public float[] Kesselleistung_stuendlich = new float[8760];
        public int Vorgabe_Betriebsbereitschaft;

        // Globale Ergebnisse
        public double Waermebedarf_gesamt = 0;
        public double Strombedarf_gesamt = 0;
        public double Maximale_Kesselleistung_Spk = 0;
        public double Stromverbrauch_Spk = 0;
        public double BruttoWaermeSpkErzeugung = 0;
        public double S_Waerme_spk = 0;
        public double Gasspitze_Spk = 0;

        // Globale Brennstoffzähler (in MWh)
        public double Gasverbrauch_SPK = 0;
        public double Oelverbrauch_SPK = 0;
        public double Rapsoelverbrauch_SPK = 0;
        public double Holzverbrauch_SPK = 0;
        public double Sonstigverbrauch_SPK = 0;
        public double Koks_SPK = 0;
        public double Kohle_SPK = 0;
        public double Pellets_SPK = 0;
        public double TierischeFette_SPK = 0;

        // Emissionen gesamt in kg
        public double Em_CO2_SPK = 0;
        public double Em_CO_SPK = 0;
        public double Em_SO2_SPK = 0;
        public double Em_NOX_SPK = 0;
        public double Em_Staub_SPK = 0;

        // Emissionen je Kessel
        public double[] CO2_SPK = new double[MAX_SPK];
        public double[] CO_SPK = new double[MAX_SPK];
        public double[] SO2_SPK = new double[MAX_SPK];
        public double[] NOX_SPK = new double[MAX_SPK];
        public double[] Staub_SPK = new double[MAX_SPK];

        // Kesselspezifische Arrays (Nutzwärme und Wirkungsgrade)
        public double[] s_waerme_Oel_Spk = new double[MAX_SPK];
        public double[] s_waerme_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];

        // Speicher für die korrekte Nutzungsgrad-Bilanz
        public double[] Kessel_Jahresnutzungsgrad_Spk = new double[MAX_SPK];
        private double[] Kessel_Verbrauch_MWh_Spk = new double[MAX_SPK];

        // Interne Kesselkonfigurationen
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        string[] Kessel_Name = new string[MAX_SPK];
        int[] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int[] Brennstoff_Art = new int[MAX_SPK];
        double[] Kessel_Leistung_Spk = new double[MAX_SPK];

        // PAKET A1: Hier stand "Berechnung(int ID_Projekt)" - der Einstieg des
        // einkanaligen Altpfads (Jahressumme, Kesseldaten_Einlesen,
        // Heizkessel_Simulation, Bilanz_und_Nutzungsgrad auf EINEM Bedarfsvektor). Er
        // ist mit dem Altpfad ersatzlos entfallen; der Einstieg des Moduls ist
        // Vorbereiten_Zweikanalig(), gerechnet wird in der Kaskadenschleife oder als
        // Vektorstufe (Berechnung_Zweikanalig).

        /// <summary>
        /// Schritt 2 der Kesselbilanz: Kesseldaten, Emissionsfaktoren, Wirkungsgrade und
        /// Bereitschaftsverluste je Kessel einlesen (Paket-5-Nacharbeit, Befund N6).
        /// </summary>
        /// <param name="heizkesselctrl">bereits erzeugter Controller des Aufrufers</param>
        /// <param name="Anzahl">Zahl der zu lesenden Kessel (bereits auf MAX_SPK begrenzt)</param>
        /// <returns>false = Abbruch (Kessel im Projekt nicht hinterlegt, B0-3).</returns>
        /// <remarks>
        /// PAKET 8 (Konzept 13.4): Der Parameter <c>mitDialog</c> ist entfallen. Er
        /// unterschied bis dahin den Altpfad (MessageBox) vom zweikanaligen Weg
        /// (<see cref="Fehlertext"/>, Nacharbeit N10) — Paket 8 verallgemeinert den
        /// Fehlerkanal, also wird dialogfrei gemeldet. Die Oberfläche zeigt den Text
        /// nach dem Lauf; dort ist ein Dialog richtig aufgehoben, mitten in der
        /// Kaskade war er es nie.
        /// </remarks>
        private bool Kesseldaten_Einlesen(HeizkesselCtrl heizkesselctrl, int Anzahl)
        {
            for (int i = 0; i < Anzahl; i++)
            {
                // B0-3: Projektfilter — gleicher Kesselname in mehreren Projekten lieferte
                // sonst die Daten des ersten Treffers (falsche Leistung/Brennstoff/Emissionen).
                heizkesselctrl.ReadAll("Bezeichner='" + spk_list[i].Replace("'", "''") + "' AND ID_Projekt=" + m_ID_Projekt);

                // B0-3: Mit dem Projektfilter kann die Treffermenge leer sein (Kessel aus
                // dem Projekt entfernt, Altdaten ohne ID_Projekt) — vorher lieferte der
                // erste Namenstreffer falsche, aber vorhandene Daten. Sauber abbrechen
                // statt items[0]-Zugriff mit ArgumentOutOfRangeException.
                if (heizkesselctrl.rows == 0)
                {
                    string text = string.Format(MyResource.Resource.SIMENG_KESSEL_NICHT_HINTERLEGT,
                                                spk_list[i]);
                    Fehlertext = text;
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + text);
                    return false;
                }

                Kessel_Name[i] = heizkesselctrl.items[0].Name;
                Kessel_Leistung_Spk[i] = heizkesselctrl.items[0].Ptherm;

                // Emissionen aus Brennstoff Tabelle laden
                DataTable dt = DataRepository.GetDataTable("select * from Tab_Brennstoff_Stamm where ID=?", new OleDbParameter("@s1", heizkesselctrl.items[0].Brennstoff));
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    CO2_SPK[i] = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0;
                    SO2_SPK[i] = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0;
                    NOX_SPK[i] = row["NOX"] != DBNull.Value ? Convert.ToDouble(row["NOX"]) : 0;
                    Staub_SPK[i] = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0;
                }

                // Wirkungsgrade einlesen
                Kessel_Wirk_Gas_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Gas;
                Kessel_Wirk_Oel_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Oel;

                // Absicherung Prozentwerte -> Faktor. Schwelle 1.5 statt 1.0 (18.08.2026):
                // Brennwertkessel liefern Hi-basierte Volllastwirkungsgrade bis ~104 % —
                // als Faktor gespeichert (710.01-Rückfall des Imports, z. B. Hoval
                // 103.5 % -> 1.035) hätte die alte Schwelle sie als Prozentwert gedeutet
                // und auf ~0.01 zerlegt. Echte Prozentwerte liegen >= 50, echte Faktoren
                // <= ~1.1; 1.5 trennt beide sauber (dieselbe Schwelle nutzt
                // WirtschaftlichkeitCtrl.LiesReferenzkessel seit Review 11).
                if (Kessel_Wirk_Gas_Spk[i] > 1.5) Kessel_Wirk_Gas_Spk[i] /= 100.0;
                if (Kessel_Wirk_Oel_Spk[i] > 1.5) Kessel_Wirk_Oel_Spk[i] /= 100.0;

                Brennstoff_Betrieb_Spk[i] = heizkesselctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];

                Betriebsbereitschaft_Verluste[i] = heizkesselctrl.items[0].Betriebsbereitschaftverlust;
                if (Betriebsbereitschaft_Verluste[i] > 1.0) Betriebsbereitschaft_Verluste[i] /= 100.0;

                Maximale_Kesselleistung_Spk += Kessel_Leistung_Spk[i];
            }

            return true;
        }

        /// <summary>
        /// Schritte 4 und 5 der Kesselbilanz: globale Brennstoffzähler, Emissionen und
        /// Jahresnutzungsgrad je Kessel. Beide Rechenwege (einkanalig und zweikanalig)
        /// benutzen sie unverändert.
        ///
        /// Voraussetzung: <c>s_waerme_Gas_Spk</c>, <c>s_waerme_Oel_Spk</c> und
        /// <c>Kessel_Verbrauch_MWh_Spk</c> stehen bereits in MWh, und <c>Gasspitze_Spk</c>
        /// ist aufsummiert.
        /// </summary>
        private void Bilanz_und_Nutzungsgrad(int Anzahl)
        {
            // 4. Verbrauch global bilanzieren und Emissionen berechnen
            for (int i = 0; i < Anzahl; i++)
            {
                double Kessel_Nutzkraft_Jahr = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i];
                S_Waerme_spk += Kessel_Nutzkraft_Jahr;

                double Kessel_Gesamtverbrauch_MWh = Kessel_Verbrauch_MWh_Spk[i];
                BruttoWaermeSpkErzeugung += Kessel_Gesamtverbrauch_MWh;

                // Den Verbrauch auf die globalen Brennstoffzähler buchen. Die Bereiche
                // spiegeln Tab_Brennstoff_Stamm.ID_Kategorie: 14 (Biogas) ist Kategorie 1
                // und zählt zum Gas. Was keinen eigenen Zähler hat (23 Fernwärme,
                // 24 Sonstige, 25 Wasserstoff, künftige IDs), fängt das else als
                // Sammelposten — dieselbe Verzweigung erwartet die Anzeige
                // (Form_Simulation_Detail, _kesselBrennstoffIds).
                if ((Brennstoff_Art[i] >= 1 && Brennstoff_Art[i] <= 5) || Brennstoff_Art[i] == 14) Gasverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if ((Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9) || (Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22)) Oelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 10) Koks_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 11) Kohle_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 12) Holzverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 17) TierischeFette_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 13)
                {
                    // Elektrowärme / Wärmepumpe
                    Stromverbrauch_Spk += Kessel_Nutzkraft_Jahr;
                    // B0-2: auch hier kein Aliasing — sonst bleibt der Strom-Vektor ab dem
                    // zweiten Lauf dauerhaft an die Kessel-Ganglinie gebunden.
                    Stromverbrauch_stuendlich = (float[])Kesselleistung_stuendlich.Clone();
                }
                else if (Brennstoff_Art[i] == 15) Pellets_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 16) Rapsoelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else Sonstigverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;

                // Emissionen basierend auf dem echten stündlich ermittelten Gesamtverbrauch
                Em_CO2_SPK += Kessel_Gesamtverbrauch_MWh * CO2_SPK[i];
                Em_SO2_SPK += Kessel_Gesamtverbrauch_MWh * SO2_SPK[i];
                Em_NOX_SPK += Kessel_Gesamtverbrauch_MWh * NOX_SPK[i];
                Em_CO_SPK += Kessel_Gesamtverbrauch_MWh * CO_SPK[i];
                Em_Staub_SPK += Kessel_Gesamtverbrauch_MWh * Staub_SPK[i];
            }

            // Emissionen final herunterskalieren (in kg)
            Em_CO2_SPK /= 1000;
            Em_SO2_SPK /= 1000;
            Em_NOX_SPK /= 1000;
            Em_CO_SPK /= 1000;
            Em_Staub_SPK /= 1000;
            if (Gasverbrauch_SPK < 0.1) Gasspitze_Spk = 0;

            // 5. JAHRESNUTZUNGSGRAD PRO KESSEL SAUBER ERMITTELN
            for (int i = 0; i < Anzahl; i++)
            {
                double erzeugteWaerme = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]; // Nutzwärme (MWh)
                double verbrauchterBrennstoff = Kessel_Verbrauch_MWh_Spk[i];       // Gesamtverbrauch inkl. Stillstand (MWh)

                if (erzeugteWaerme > 0 && verbrauchterBrennstoff > 0)
                {
                    double ngrad = (erzeugteWaerme / verbrauchterBrennstoff) * 100;

                    // Plausibilitätsgrenzen nach DIN
                    if (ngrad > 110.0) ngrad = 108.0;
                    if (ngrad < 1.0) ngrad = 1.0;

                    Kessel_Jahresnutzungsgrad_Spk[i] = ngrad;
                }
                else
                {
                    Kessel_Jahresnutzungsgrad_Spk[i] = 0; // Kessel stand still
                }
            }
        }

        // PAKET A1: Hier stand "Heizkessel_Simulation" - die EINKANALIGE Jahresschleife
        // der Kessel-Lastverteilung. Ihr einziger Aufrufer war Berechnung(int); beide
        // sind mit dem Altpfad entfallen. Die zweikanalige Fassung der Lastverteilung
        // steht in Stunde_Bedarf/Stunde_Abschluss.

        // ===================================================================
        // Zweikanaliger Weg (Paket 5 - Konzept 6.5, erster Punkt)
        // ===================================================================

        /// <summary>Anzahl der Kessel des zweikanaligen Wegs (nach der MAX_SPK-Grenze).</summary>
        private int _anzahlZweikanalig = 0;

        /// <summary>
        /// BRENNSTOFFBASIERTE Wärme, die ein Kessel in der LAUFENDEN Stunde erzeugt hat
        /// [kWh] — die Bezugsgröße von Verbrauch, Emissionen und Jahresnutzungsgrad.
        ///
        /// Ohne Quellpuffer ist sie identisch mit der ABGABE (<see cref="_kesselAbgabe"/>).
        /// Mit Quellpuffer (Etappe D5a) trennen sich beide: Der Kessel gibt weiterhin die
        /// volle Nutzwärme ab, hebt aber nur noch von der Puffertemperatur aus an — den
        /// Rest hat der Puffer beigesteuert.
        /// </summary>
        private readonly double[] _kesselStunde = new double[MAX_SPK];

        /// <summary>
        /// GESAMTE Wärmeabgabe eines Kessels in der laufenden Stunde [kWh], also
        /// brennstoffbasierte Wärme PLUS Quellwärme aus dem Puffer (Etappe D5a).
        ///
        /// Sie entscheidet allein darüber, ob der Kessel in dieser Stunde LÄUFT — und
        /// damit, ob ihm der Bereitschaftsverlust anzulasten ist. Ein Kessel, der
        /// vorgewärmtes Wasser nur noch geringfügig anhebt, steht nicht still.
        /// Ohne Quellpuffer ist das Feld Wert für Wert <see cref="_kesselStunde"/>.
        /// </summary>
        private readonly double[] _kesselAbgabe = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // WÄRMEQUELLE PUFFERSPEICHER (Etappe D5a, Konzept_KonfigUI_Hydraulik
        // Anforderung 6 — „Kessel-Kaskade")
        //
        // Ein Kessel mit WQ_Typ = Pufferspeicher und gültiger WQ_ID_Puffer bezieht seine
        // EINTRITTSTEMPERATUR aus diesem Puffer statt aus dem Systemrücklauf. Er muss die
        // Nutzwärme deshalb nicht mehr über die ganze Spreizung anheben, sondern nur noch
        // über den Rest:
        //
        //     Anteil = (T_Quelle − T_Rücklauf) / (T_Vorlauf − T_Rücklauf)      [0…1]
        //     Q_Puffer = Anteil · Q_nutz         (Entnahme = ENTLADUNG des Quellpuffers)
        //     Q_Kessel = Q_nutz − Q_Puffer       (nur DAS kostet Brennstoff)
        //
        // Das ist Zeile für Zeile die Konstruktion des Wärmepumpen-Quellbezugs, nur mit
        // dem Temperaturhub statt der Leistungszahl als Aufteilungsschlüssel: Dort gilt
        // Q_Quelle = Q · (1 − 1/COP), hier Q_Quelle = Q · Anteil.
        //
        // LIEFERT DER PUFFER WENIGER als Anteil · Q_nutz (er ist leer), springt der
        // Brennstoff für den Fehlbetrag ein — die Abgabe an den Kanal bleibt dieselbe,
        // die Energiebilanz geht auf. Die MENGE dieser Stunde ist dann aber nicht mehr
        // frei: Sie ist so zu wählen, dass der Fehlbetrag noch in die Nennleistung passt
        // (siehe MaxAbgabe, Nacharbeit E-K1-1).
        //
        // MODELLENTSCHEIDUNG zur Kesselleistung: Die Nennleistung begrenzt den EIGENEN,
        // brennstoffbasierten Beitrag — die Wärme aus dem Puffer kommt obendrauf:
        //
        //     Q_nutz(max) = min( P_nenn / (1 − Anteil),  P_nenn + Inhalt(Quellpuffer) )
        //
        // Das ist die hydraulische Kaskade, wie das Konzept sie beschreibt: Der Brenner
        // hebt weiter an, was der Puffer schon vorgewärmt hat — aber nur so weit, wie er
        // wirklich vorgewärmt hat. Die naheliegende Variante „Nennleistung begrenzt die
        // ganze Abgabe" ist an Szenario (d) gemessen und verworfen; die Begründung steht
        // in D5a_KombiKaskade_Protokoll.md, Abschnitt „Nacharbeit nach Reviews".
        // Eine Begrenzung nach Massenstrom und Wärmeübertrager kennt das Modell an keiner
        // Stelle. Der SPEICHER hat seit Paket P1 eine Lade-/Entladeleistungsgrenze
        // (Tab_Pufferspeicher.Ladeleistung_Max/Entladeleistung_Max, 0 = unbegrenzt); sie
        // greift über Entnahmefaehigkeit() auch hier. Eine eigene Grenze des
        // ÜBERTRAGERS zwischen Puffer und Kessel gibt es weiterhin nicht.
        // ------------------------------------------------------------------

        /// <summary>Quellpuffer je Kessel; <c>null</c> = keiner (Regelfall).</summary>
        private readonly SimulationPufferspeicher[] _quellSpeicher =
            new SimulationPufferspeicher[MAX_SPK];

        /// <summary>Anteil der Nutzwärme, den der Quellpuffer beisteuert (0…1); 0 = kein Bezug.</summary>
        private readonly double[] _quellAnteil = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // PAKET B1 — TEMPERATURKOPPLUNG DES KESSEL-QUELLBEZUGS (Konzept 8.4)
        //
        // GLEICHBEHANDLUNG mit der Wärmepumpe (Konzept 8.4, Punkt 1): Bis P1 war
        // T_Quelle die VORLAUFTEMPERATUR der Speicherzeile — eine Jahreskonstante, und
        // damit auch _quellAnteil. Für einen GETEILTEN Quellpuffer (zugleich Senke eines
        // anderen Erzeugers) liefert jetzt der Speicherzustand die Temperatur:
        //
        //     T_Quelle(h) = SchichtTemperatur an der Quell-Entnahmehöhe (bis Q1: oben)
        //     Anteil(h)   = (T_Quelle(h) − T_Rücklauf) / (T_Vorlauf − T_Rücklauf), 0…1
        //
        // KEINE NEUE PHYSIK: Die Formel, die Mengenrechnung, die beiden Schranken in
        // MaxAbgabe und die Buchung in QuellwaermeHolen bleiben Zeichen für Zeichen die
        // von D5a. Getauscht ist allein die HERKUNFT von T_Quelle — aus der
        // Speicherzeile wird der Speicherzustand.
        //
        // DERSELBE LESEZEITPUNKT wie bei der WP: je Stunde GENAU EINMAL, vor Phase B der
        // Rechenebene (Quelltemperatur_Stunde, gerufen aus der Kaskadenschleife). Der
        // Wert gilt für Bedarfs- UND Ladephase derselben Stunde.
        //
        // EIGENSTÄNDIGE Quellspeicher bleiben statisch — dieselbe Grenze wie in 8.2.
        // ------------------------------------------------------------------

        /// <summary>Je Kessel: true = <see cref="_quellAnteil"/> folgt stündlich dem Speicherzustand.</summary>
        private readonly bool[] _quellKopplung = new bool[MAX_SPK];

        /// <summary>Vorlauftemperatur des Hubs je Kessel [°C] (nur bei Kopplung belegt).</summary>
        private readonly double[] _quellVorlauf = new double[MAX_SPK];

        /// <summary>Rücklauftemperatur des Hubs je Kessel [°C] (nur bei Kopplung belegt).</summary>
        private readonly double[] _quellRuecklauf = new double[MAX_SPK];

        /// <summary>
        /// PAKET Q1: Quell-Entnahmehöhe je Kessel, 0…1 (1 = ganz oben), aus
        /// <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c> (Schema-Schritt 54).
        /// <c>QuellkopplungSetzen</c> belegt sie; ohne gepflegten Wert steht dort
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> und damit das
        /// B1-Verhalten.
        /// </summary>
        private readonly double[] _quellHoehe = new double[MAX_SPK];

        /// <summary>
        /// Quelltemperatur-Ganglinie je gekoppeltem Kessel [°C] — LAUFERGEBNIS
        /// (Konzept 8.4). <c>null</c> für jeden Kessel ohne Kopplung.
        /// </summary>
        private readonly float[][] _quellTemperatur = new float[MAX_SPK][];

        /// <summary>Stunden je Kessel, in denen der gekoppelte Puffer nicht über den Rücklauf kam.</summary>
        private readonly int[] _quellZuKalt = new int[MAX_SPK];

        /// <summary>true = der Quellbezug des Kessels folgt stündlich dem Speicher (Paket B1).</summary>
        public bool QuelleGekoppelt(int index)
        {
            return index >= 0 && index < MAX_SPK && _quellKopplung[index];
        }

        /// <summary>
        /// Quelltemperatur-Ganglinie eines gekoppelten Kessels [°C]; <c>null</c> ohne
        /// Kopplung (Paket B1) — Lesezugriff für Anzeige und Zeitreihen-Export.
        /// </summary>
        public float[] Quelltemperaturen(int index)
        {
            return (index >= 0 && index < MAX_SPK) ? _quellTemperatur[index] : null;
        }

        /// <summary>
        /// Richtet die TEMPERATURKOPPLUNG eines Kessel-Quellbezugs ein (Paket B1,
        /// Konzept 8.4). Aufgerufen von <c>SimulationControl.KesselQuellbezugSetzen</c>
        /// anstelle von <see cref="QuellbezugSetzen"/>, sobald der Quellpuffer ein
        /// GETEILTER Puffer ist.
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        /// <param name="speicher">geteilter Quellpuffer</param>
        /// <param name="vorlauf">Vorlauf des Kessel-Hubs [°C]</param>
        /// <param name="ruecklauf">Rücklauf des Kessel-Hubs [°C]</param>
        /// <param name="anschlusshoehe">
        /// PAKET Q1: Quell-Entnahmehöhe 0…1 aus <c>WQ_Anschlusshoehe</c>
        /// (Schema-Schritt 54); <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> ohne
        /// gepflegten Wert — das ist exakt das Verhalten von Paket B1.
        /// </param>
        public void QuellkopplungSetzen(int index, SimulationPufferspeicher speicher,
                                        double vorlauf, double ruecklauf,
                                        double anschlusshoehe = SimulationPufferspeicher.HOEHE_OBEN)
        {
            if (index < 0 || index >= MAX_SPK) return;
            if (speicher == null || vorlauf <= ruecklauf) return;

            _quellSpeicher[index] = speicher;
            _quellVorlauf[index] = vorlauf;
            _quellRuecklauf[index] = ruecklauf;
            _quellHoehe[index] = anschlusshoehe;
            _quellKopplung[index] = true;
            _quellTemperatur[index] = new float[8760];

            // Startwert aus dem aktuellen Zustand; die Stundenabfrage übersteuert ihn vor
            // jeder Phase B. Ohne diese Zeile stünde bis zur ersten Abfrage ein Anteil
            // von 0 — dasselbe Ergebnis, aber der Zustand wäre nicht selbsterklärend.
            _quellAnteil[index] = AnteilAus(speicher.QuellEntnahmeTemperatur(anschlusshoehe), index);
        }

        /// <summary>
        /// PAKET Q1: die Quell-Entnahmehöhe des Kessels <paramref name="index"/>, 0…1;
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> ohne Kopplung —
        /// Lesezugriff für Protokoll und Wirkproben.
        /// </summary>
        public double QuellAnschlusshoehe(int index)
        {
            if (index < 0 || index >= MAX_SPK || !_quellKopplung[index])
                return SimulationPufferspeicher.HOEHE_OBEN;
            return _quellHoehe[index];
        }

        /// <summary>Anteil (0…1) aus einer Quelltemperatur und dem Hub des Kessels.</summary>
        private double AnteilAus(double tQuelle, int index)
        {
            double spanne = _quellVorlauf[index] - _quellRuecklauf[index];
            if (spanne <= 0) return 0;

            double anteil = (tQuelle - _quellRuecklauf[index]) / spanne;
            if (anteil < 0) return 0;
            return anteil > 1 ? 1 : anteil;
        }

        /// <summary>
        /// Bildet den Quellanteil der Stunde für alle gekoppelten Kessel der AKTIVEN
        /// Rechenebene (Paket B1, Konzept 8.4) — GENAU EINMAL je Stunde und Ebene, vor
        /// Phase B, gerufen aus der Kaskadenschleife.
        ///
        /// <para>Ohne gekoppelten Kessel ist die Methode ein sofortiger Rücksprung.</para>
        /// </summary>
        /// <param name="alleEbenen">
        /// PAKET B2: true = ALLE gekoppelten Kessel, unabhängig von der aktiven
        /// Rechenebene — der Lesepunkt „Davor" (Vorbelegung) läuft am Stundenanfang und
        /// kennt deshalb noch keine Ebene. false = nur die Kessel der aktiven Ebene, der
        /// Lesepunkt „Danach" von Paket B1. In BEIDEN Modi wird je Stunde genau einmal je
        /// Kessel gelesen — und damit auch <see cref="_quellZuKalt"/> höchstens einmal je
        /// Stunde erhöht.
        /// </param>
        public void Quelltemperatur_Stunde(int stunde, bool alleEbenen = false)
        {
            if (stunde < 0 || stunde >= 8760) return;

            for (int i = 0; i < _anzahlZweikanalig && i < MAX_SPK; i++)
            {
                if (!_quellKopplung[i]) continue;
                if (!alleEbenen && !EbeneAktiv(i)) continue;

                SimulationPufferspeicher q = _quellSpeicher[i];
                if (q == null) continue;

                // PAKET Q1: an der gepflegten Quell-Entnahmehöhe statt fest oben.
                double tQuelle = q.QuellEntnahmeTemperatur(_quellHoehe[i]);
                if (_quellTemperatur[i] != null) _quellTemperatur[i][stunde] = (float)tQuelle;

                double anteil = AnteilAus(tQuelle, i);
                _quellAnteil[i] = anteil;

                // Der Puffer steht auf Rücklaufniveau: In dieser Stunde trägt er nichts
                // bei, der Kessel hebt wie ohne Kaskade von seinem Systemrücklauf aus an.
                // Gezählt und am Laufende EINMAL gemeldet (Gegenstück zur F13-Kappung der
                // Wärmepumpe) - stumm bliebe sonst ein Booster, der nie boostet.
                if (anteil <= 0) _quellZuKalt[i]++;
            }
        }

        /// <summary>
        /// PAKET B1: meldet je gekoppeltem Kessel EINMAL, in wie vielen Stunden der
        /// Quellpuffer nicht über den Systemrücklauf kam, und den Temperaturbereich der
        /// Quelle über das Jahr. Gerufen am Ende des Jahresdurchlaufs.
        /// </summary>
        public void QuellkopplungMelden()
        {
            for (int i = 0; i < _anzahlZweikanalig && i < MAX_SPK; i++)
            {
                if (!_quellKopplung[i] || _quellTemperatur[i] == null) continue;

                float min = float.MaxValue, max = float.MinValue;
                double summe = 0;
                for (int h = 0; h < 8760; h++)
                {
                    float v = _quellTemperatur[i][h];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    summe += v;
                }

                string name = (i < Kessel_Name.Length && Kessel_Name[i] != null) ? Kessel_Name[i] : "";

                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "Kessel_Quellkopplung_" + i + "_" + name,
                    string.Format(MyResource.Resource.SIMENG_KESSEL_QUELLKOPPLUNG_HINWEIS,
                                  name,
                                  min.ToString("F1"), max.ToString("F1"),
                                  (summe / 8760.0).ToString("F1"),
                                  _quellRuecklauf[i].ToString("F1"),
                                  _quellVorlauf[i].ToString("F1"),
                                  _quellZuKalt[i]));
            }
        }

        /// <summary>
        /// Meldungen über Quellentnahmen der laufenden Phase — die Kaskadenschleife führt
        /// daraus die Herkunftsrechnung fort und leert die Liste (siehe
        /// <see cref="Quellentnahme"/>). Ohne Quellpuffer bleibt sie durchgehend leer.
        /// </summary>
        public readonly List<Quellentnahme> Quellentnahmen = new List<Quellentnahme>();

        /// <summary>Aus Quellpuffern bezogene Wärme je Stunde [kWh]; ohne Quellbezug exakt 0.</summary>
        public double[] Quellwaerme_stuendlich = new double[8760];

        /// <summary>Jahressumme der Quellwärme [kWh]; ohne Quellbezug exakt 0.</summary>
        public double Quellwaerme_gesamt = 0;

        /// <summary>
        /// RECHENEBENE je Kessel (Etappe D5a) — indexgleich zu <see cref="spk_list"/>.
        /// Gesetzt von der Kaskadenschleife aus den Quellbezügen; <c>null</c> oder ein
        /// Vektor aus Nullen bedeutet „alle Kessel rechnen auf Ebene 0", also wie bisher.
        /// </summary>
        public int[] ModulEbenen;

        /// <summary>Ebene, die die Kaskadenschleife gerade abarbeitet (Etappe D5a).</summary>
        public int AktiveEbene = 0;

        /// <summary>true, wenn Kessel <paramref name="i"/> auf der aktiven Ebene rechnet.</summary>
        private bool EbeneAktiv(int i)
        {
            if (ModulEbenen == null || i < 0 || i >= ModulEbenen.Length) return AktiveEbene == 0;
            return ModulEbenen[i] == AktiveEbene;
        }

        /// <summary>
        /// Setzt den Quellbezug eines Kessels (Etappe D5a). Aufgerufen von
        /// <c>SimulationControl</c>, nachdem die Speicher-Registry offen ist.
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        /// <param name="speicher">Quellpuffer; <c>null</c> hebt den Bezug auf</param>
        /// <param name="anteil">Anteil der Nutzwärme aus dem Puffer (0…1)</param>
        public void QuellbezugSetzen(int index, SimulationPufferspeicher speicher, double anteil)
        {
            if (index < 0 || index >= MAX_SPK) return;

            if (anteil < 0) anteil = 0;
            if (anteil > 1) anteil = 1;

            _quellSpeicher[index] = (anteil > 0) ? speicher : null;
            _quellAnteil[index] = (speicher != null) ? anteil : 0;
        }

        /// <summary>Quellpuffer eines Kessels; <c>null</c> = keiner (für Anzeige und Protokoll).</summary>
        public SimulationPufferspeicher QuellSpeicher(int index)
        {
            if (index < 0 || index >= MAX_SPK) return null;
            return _quellSpeicher[index];
        }

        /// <summary>Quellanteil eines Kessels (0…1); 0 = kein Bezug.</summary>
        public double QuellAnteil(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            return _quellAnteil[index];
        }

        /// <summary>
        /// Höchste Wärmeabgabe eines Kessels in der laufenden Stunde [kWh].
        ///
        /// Ohne Quellpuffer ist das die verbliebene Nennleistung — Wert für Wert der
        /// bisherige Ausdruck. Mit Quellpuffer kommt der Puffer-Anteil obendrauf (siehe
        /// den Blockkommentar zum Quellbezug).
        ///
        /// <para><b>ZWEI Schranken, nicht eine</b> (Nacharbeit E-K1-1). Der Puffer ist
        /// nach der Formel mit <c>Anteil</c> beteiligt — LIEFERN kann er aber nur, was in
        /// ihm steht. Deshalb sind beide Grenzen zu bilden und die kleinere gilt:
        /// <code>
        ///   nachLeistung = P_rest / (1 − Anteil)     // der Puffer liefert wie gerechnet
        ///   nachQuelle   = P_rest + Inhalt           // der Puffer liefert weniger,
        ///                                            // der Brennstoff deckt den Rest
        /// </code>
        /// In beiden Fällen bleibt der BRENNSTOFFBASIERTE Beitrag
        /// <c>eigen = menge − geliefert</c> damit ≤ <see cref="_restLeistung"/>, und die
        /// Nennleistung ist je Stunde eingehalten. Vorher stand hier nur die erste
        /// Schranke; klemmte <c>Entladen</c> am Füllstand, wurde die Differenz
        /// brennstoffbasiert und <c>_restLeistung</c> beliebig negativ — der Kessel lief
        /// über seiner Nennleistung (gemessen: 200,8 MWh bei 19,3 kW · 8760 h = 169,1 MWh).
        /// </para>
        ///
        /// <para>Der Ausdruck ist STETIG in <c>Anteil</c>: Je näher der Anteil an 1 rückt,
        /// desto größer wird <c>nachLeistung</c>, und die bindende Schranke ist der
        /// Speicherinhalt. Bei <c>Anteil = 1</c> — der Puffer trägt die ganze Anhebung —
        /// bleibt <c>nachQuelle</c>: sein Inhalt plus die Nennleistung, mit der der
        /// Brenner den Fehlbetrag von der Rücklauftemperatur aus deckt.</para>
        /// </summary>
        private double MaxAbgabe(int i)
        {
            double eigen = _restLeistung[i];
            if (eigen <= 0) return 0;

            SimulationPufferspeicher q = _quellSpeicher[i];
            if (q == null || _quellAnteil[i] <= 0) return eigen;

            // Was der Quellpuffer in DIESER Stunde höchstens beisteuern kann. Entladen()
            // klemmt am Füllstand; die Entnahmefähigkeit liefert seit Paket P1 den Rest
            // des Stundenbudgets aus Entladeleistung_Max (0 = unbegrenzt, der Regelfall).
            double ausQuelle = Math.Min(q.SOC > 0 ? q.SOC : 0, q.Entnahmefaehigkeit());

            double nachQuelle = eigen + ausQuelle;
            if (_quellAnteil[i] >= 1) return nachQuelle;

            double nachLeistung = eigen / (1.0 - _quellAnteil[i]);
            return Math.Min(nachLeistung, nachQuelle);
        }

        /// <summary>
        /// Holt den Quellanteil einer gerade abgegebenen Wärmemenge aus dem Quellpuffer
        /// und meldet die Entnahme an die Herkunftsrechnung.
        /// </summary>
        /// <param name="ziel">Zielspeicher der Wärme; <c>null</c> = Direktdeckung</param>
        /// <returns>tatsächlich aus dem Puffer bezogene Wärme [kWh]</returns>
        private double QuellwaermeHolen(int i, double menge, int stunde,
                                        SimulationPufferspeicher ziel)
        {
            if (menge <= 0) return 0;

            SimulationPufferspeicher q = _quellSpeicher[i];
            if (q == null || _quellAnteil[i] <= 0) return 0;

            // PAKET E1: OHNE Kanalangabe — eine Quellentnahme trägt keinen Bedarfskanal.
            // Sie wird deshalb auf dem Heizkanal gebucht (Vorbelegung von Entladen,
            // dieselbe Näherung wie Kaskadenschleife.Anteil_Entladen ohne Kanal).
            double geliefert = q.Entladen(menge * _quellAnteil[i], stunde);
            if (geliefert <= 0) return 0;

            Quellwaerme_gesamt += geliefert;
            if (stunde >= 0 && stunde < 8760) Quellwaerme_stuendlich[stunde] += geliefert;

            Quellentnahmen.Add(new Quellentnahme { Quelle = q, Menge = geliefert, Ziel = ziel });
            return geliefert;
        }

        /// <summary>Noch nicht vergebene Leistung eines Kessels in der laufenden Stunde [kW].</summary>
        private readonly double[] _restLeistung = new double[MAX_SPK];

        /// <summary>Gasspitze je Kessel [kW] (zweikanaliger Weg).</summary>
        private readonly double[] _gasspitzeKessel = new double[MAX_SPK];

        /// <summary>Senkenliste je Kessel, indexgleich zu <see cref="spk_list"/> (Paket S1).</summary>
        private readonly List<Senkenliste> _kesselSenke = new List<Senkenliste>();

        /// <summary>
        /// In Pufferspeicher geladene Kesselwärme je Stunde [kWh] (zweikanaliger Weg,
        /// Nacharbeit N1).
        ///
        /// Sie ist ein TEIL der Nutzwärme (<see cref="S_Waerme_spk"/>): Dort steht die
        /// gesamte abgegebene Wärme, also Direktdeckung PLUS Speicherladung — und genau
        /// so gehört sie dorthin, denn der Brennstoffverbrauch und der Jahresnutzungsgrad
        /// beziehen sich auf sie. Getrennt geführt wird die Ladung, weil die
        /// Ergebnispersistenz Restbedarf und Deckungsgrad aus der DIREKTDECKUNG bilden
        /// muss — sonst wird der Restbedarf negativ und die Summe der Deckungen
        /// überschreitet 100 % (dieselbe Mitkorrektur wie bei der Solarthermie,
        /// Konzept 6.4).
        /// </summary>
        public double[] Speicherladung_stuendlich = new double[8760];

        /// <summary>Jahressumme der Speicherladung [kWh]; ohne Puffer-Senke exakt 0.</summary>
        public double Speicherladung_gesamt = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        ///
        /// Gefüllt von <see cref="Kaskadenschleife"/>; ohne Puffer-Senke
        /// exakt 0. Zusammen mit der Direktdeckung ergibt sich daraus der EIGENANTEIL des
        /// Kessels an der Bedarfsdeckung — die Größe, die
        /// <c>Tab_ErgebnisHeizkessel.Waermebedarfsdeckung</c> ausweist.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        // ------------------------------------------------------------------
        // KANALINDIZIERTE DECKUNGSBUCHFÜHRUNG (Paket K2, Konzept 4.4)
        //
        // ZUSÄTZLICHE Aufschlüsselung, kein Ersatz: Die Skalare des Moduls
        // (S_Waerme_spk, Speicherladung_gesamt, Speicherentladung_Anteil,
        // Kessel_Verbrauch_MWh_Spk …) werden unverändert gebildet und von
        // SimulationRunner unverändert gelesen. Es gilt
        //
        //   Σ Direktdeckung_Kanal[k]     == die in Phase B abgegebene Nutzwärme
        //                                   (= Kesselabgabe − Speicherladung)
        //   Σ Speicherentladung_Kanal[k] == Speicherentladung_Anteil
        //
        // bis auf die Rundungsklasse der getrennten Kanalarithmetik.
        // ------------------------------------------------------------------

        /// <summary>
        /// In Phase B direkt an den Bedarf abgegebene Kesselwärme je Kanal [kWh]
        /// (Konzept 4.4). Einen Skalar dieser Größe führt das Modul nicht — er steckt in
        /// <c>S_Waerme_spk</c> zusammen mit der Speicherladung; die Summe über die Kanäle
        /// ist genau der Direktanteil.
        /// </summary>
        public double[] Direktdeckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Anteil dieses Kessels an der bedarfsdeckenden Speicherentladung je Kanal [kWh]
        /// — die Aufschlüsselung von <see cref="Speicherentladung_Anteil"/>. Gefüllt von
        /// der <see cref="Kaskadenschleife"/>, wie der Skalar selbst.
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

        /// <summary>
        /// Fehlertext des zweikanaligen Wegs (Konzept 13.4: die Engine bleibt dialogfrei).
        /// Statt einer MessageBox mitten im Rechenlauf
        /// geht die Meldung über den Fehlerkanal Richtung
        /// <c>SimulationRunner.SimuliereUndSpeichere(… out fehler)</c> (Nacharbeit N10).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>Anzahl der Kessel, die im zweikanaligen Weg rechnen.</summary>
        public int KesselAnzahl { get { return _anzahlZweikanalig; } }

        /// <summary>
        /// Bezeichnung eines Kessels (<c>Tab_Heizkessel.Name</c>), indexgleich zu
        /// <see cref="spk_list"/>; "" außerhalb des Bereichs. Lesezugriff für Anzeigen
        /// und Zeitreihen-Beschriftungen (Paket B1).
        /// </summary>
        public string KesselName(int index)
        {
            if (index < 0 || index >= MAX_SPK || Kessel_Name[index] == null) return "";
            return Kessel_Name[index];
        }

        /// <summary>Senkenliste eines Kessels; <c>null</c> außerhalb des Indexbereichs (Paket S1).</summary>
        public Senkenliste KesselSenke(int index)
        {
            if (index < 0 || index >= _kesselSenke.Count) return null;
            return _kesselSenke[index];
        }

        /// <summary>
        /// Baut die Kessel des zweikanaligen Wegs auf — Schritte 1 und 2 aus
        /// <see cref="Berechnung"/>, Zeile für Zeile dieselben Abfragen und dieselben
        /// Absicherungen (B0-3, B0-12).
        /// </summary>
        /// <returns>false = Abbruch (Kessel nicht im Projekt hinterlegt).</returns>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenliste> senken)
        {
            m_ID_Projekt = ID_Projekt;

            Init();
            Fehlertext = "";
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);
            Array.Clear(_kesselStunde, 0, _kesselStunde.Length);
            Array.Clear(_restLeistung, 0, _restLeistung.Length);
            Array.Clear(_gasspitzeKessel, 0, _gasspitzeKessel.Length);
            _kesselSenke.Clear();

            Waermebedarf_gesamt = 0;
            Max_Waermebedarf = 0;
            Strombedarf_gesamt = Strombedarf_stuendlich.Sum();

            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();
            int Anzahl = spk_list.Count;

            // B0-12, dialogfrei (Nacharbeit N10, seit Paket 8 auf BEIDEN Wegen): Der Lauf
            // rechnet mit den ersten MAX_SPK Kesseln weiter und meldet das als Warnung im
            // Protokollkanal (Konzept 13.4). Das VERHALTEN ist dasselbe wie vorher.
            if (Anzahl > MAX_SPK)
            {
                SimulationProtokoll.Aktuell.Warnung(string.Format(
                    MyResource.Resource.SIMENG_KESSEL_MAX_UEBERSCHRITTEN, Anzahl, MAX_SPK, MAX_SPK));
                Anzahl = MAX_SPK;
            }

            // Schritt 2 aus Berechnung() — EINE Fassung für beide Wege (Nacharbeit N6).
            if (!Kesseldaten_Einlesen(heizkesselctrl, Anzahl)) return false;

            // Senkenliste je Kessel: keine Physik, sondern die Konfiguration des
            // zweikanaligen Wegs — deshalb hier und nicht im gemeinsamen Einlesen.
            for (int i = 0; i < Anzahl; i++)
            {
                int idAnlage = (i < spk_anlagen_ids.Count) ? spk_anlagen_ids[i] : 0;
                _kesselSenke.Add(SenkeZuAnlage(senken, idAnlage));
            }

            _anzahlZweikanalig = Anzahl;
            return true;
        }

        /// <summary>
        /// Senkenliste einer Anlage; ohne Zeile gilt die Rang-1-Invariante
        /// Heizkreis/Beides (Konzept 4.6/5.1).
        /// </summary>
        private static Senkenliste SenkeZuAnlage(List<Senkenliste> senken, int idAnlage)
        {
            if (senken != null && idAnlage > 0)
                foreach (Senkenliste s in senken)
                    if (s != null && s.AnlagenID == idAnlage) return s;

            return Senkenliste.Vorbelegung(idAnlage);
        }

        /// <summary>
        /// Stundenbeginn: jeder Kessel hat seine volle Nennleistung zur Verfügung, und
        /// der STUFENEINGANG wird festgehalten.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N1: Der Stufeneingang ist der Kanalstand VOR der
        /// Vorabentladung (Phase A) — dieselbe Bezugsgröße, die die Stufe an ihrer
        /// Kaskadenposition sieht und die die Wärmepumpe seit Etappe 4b führt. Bis dahin
        /// stand er in <see cref="Stunde_Bedarf"/> und damit NACH Phase A; die Größe
        /// <c>Tab_ErgebnisHeizkessel.Waermebedarf</c> fiel dadurch still ab, sobald ein
        /// Speicher vorab entlud. Ohne Speicher in der Stufe ändert sich nichts — dann
        /// gibt Phase A nichts ab.
        ///
        /// <para>PAKET K2: Der Stufeneingang ist die Summe ÜBER ALLE Kanäle des
        /// Restbedarfsfeldes — ohne Prozesswärmeanteil Zeichen für Zeichen die bisherige
        /// Größe <c>rest_heiz + rest_ww</c>.</para>
        /// </summary>
        public void Stunde_Start(int stunde, double[] rest)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                _kesselStunde[i] = 0;
                _kesselAbgabe[i] = 0;
                _restLeistung[i] = Kessel_Leistung_Spk[i];
            }

            double eingang = Kaskadenschleife.RestSumme(rest);
            if (eingang < 0) eingang = 0;
            if (stunde >= 0 && stunde < 8760) Waermebedarf[stunde] = (float)eingang;
            if (Max_Waermebedarf < eingang) Max_Waermebedarf = eingang;
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Heizkessel: die
        /// KANALGERECHTE Fassung der Lastverteilung (bis Paket A1: Heizkessel_Simulation).
        ///
        /// Konzept 6.5 beschreibt sie als „zweiten Schleifendurchlauf mit erhaltenem
        /// Zwischenzustand". Umgesetzt ist genau das, nur ohne zweiten Durchlauf: Der
        /// Kessel bedient in EINER Stunde erst den einen, dann den anderen Kanal — bei
        /// Bedarfsart <c>Beides</c> mit Warmwasservorrang, wie überall in dieser Engine
        /// (<c>SenkeAbziehen</c>) —, und die abgegebene Nutzwärme sammelt sich in
        /// <see cref="_kesselStunde"/>. Der Zwischenzustand ist damit erhalten, und die
        /// BEREITSCHAFTSVERLUSTE fallen nur EINMAL je Stunde und Kessel an: Sie werden
        /// nicht hier, sondern in <see cref="Stunde_Abschluss"/> gebucht, und zwar an
        /// genau einer Stelle für beide Kanäle und die Speicherladung zusammen.
        ///
        /// Ein Kessel OHNE Direktsenke deckt hier NICHTS — er lädt ausschließlich
        /// (Ladephasen), und damit gilt derselbe Doppelzählungs-Freibeweis wie bei der
        /// Wärmepumpe.
        ///
        /// <para>PAKET K2: <paramref name="rest"/> ist der offene Bedarf je Kanal und
        /// tritt an die Stelle des Paares <c>ref rest_heiz, ref rest_ww</c>; es wird
        /// IN-PLACE fortgeschrieben. Die Bezugsgröße <c>verfuegbar</c> kommt nicht mehr
        /// aus einer eigenen Dreifach-Verzweigung über <c>WS_Typ</c>, sondern aus
        /// <c>Kanalabzug.Offen</c> — derselben Quelle, gegen die gleich abgezogen
        /// wird (Konzept 4.3). PAKET S1: gefragt wird die ganze SENKENLISTE des Kessels
        /// statt einer einzelnen Bedarfsart (Konzept 5.2).</para>
        /// </summary>
        public void Stunde_Bedarf(int stunde, double[] rest)
        {
            // Der Stufeneingang steht seit der Nacharbeit N1 in Stunde_Start - VOR der
            // Vorabentladung (Phase A).
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                if (_restLeistung[i] <= 0) continue;

                // D5a: In dieser Phase rechnen nur die Kessel der aktiven Rechenebene.
                // Ohne Quellbezug steht jeder Kessel auf Ebene 0 und die Prüfung ist
                // immer wahr.
                if (!EbeneAktiv(i)) continue;

                // PAKET S1: Gefragt wird die DIREKTSENKEN-KETTE des Kessels
                // (Konzept 5.2). Ein Kessel ganz ohne Direktsenke lädt ausschließlich und
                // deckt hier nichts - die Nachfolge der Prüfung „Hauptsenke != Heizkreis".
                Senkenliste senken = _kesselSenke[i];
                if (senken != null && !senken.HatDirektsenke) continue;

                double verfuegbar = Kanalabzug.Offen(senken, rest);

                if (verfuegbar <= 0) continue;

                double menge = Math.Min(MaxAbgabe(i), verfuegbar);
                if (menge <= 0) continue;

                // K2: Abzug über die eine Kanalregel, mit gemessener Aufschlüsselung je
                // Kanal (Konzept 4.4). Die abgezogene Gesamtmenge ist konstruktiv genau
                // "menge" - sie ist auf den offenen Kanalbedarf begrenzt.
                //
                // PAKET E2: derselbe Abzug schreibt zusätzlich die Kanalganglinie der
                // Stunde - aus derselben gemessenen rest-Differenz.
                Kanalabzug.Abziehen(senken, menge, rest, Direktdeckung_Kanal,
                                    Direktdeckung_KanalStuendlich, stunde);

                _kesselAbgabe[i] += menge;

                // D5a: Der Quellpuffer trägt seinen Temperaturhub bei; nur der Rest kostet
                // Brennstoff und verbraucht Nennleistung. Ohne Quellbezug ist der Abzug
                // exakt 0 und beide Zeilen sind die bisherigen.
                double eigen = menge - QuellwaermeHolen(i, menge, stunde, null);
                _restLeistung[i] -= eigen;
                _kesselStunde[i] += eigen;

                // E-K1-1: MaxAbgabe hält „eigen ≤ Restleistung" rechnerisch ein; die
                // Klemmung fängt allein die Gleitkomma-Reste. Ohne Quellbezug ist
                // _restLeistung nie negativ und die Zeile wirkungslos.
                if (_restLeistung[i] < 0) _restLeistung[i] = 0;
            }

            if (stunde >= 0 && stunde < 8760)
                Restwaerme[stunde] = (float)Kaskadenschleife.RestSumme(rest);
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.5: „Senkenauswertung je Kessel —
        /// Puffer laden bis Abschaltschwelle").
        ///
        /// Die Abschaltschwelle steckt in <see cref="Ladeauftrag.ObergrenzeStunde"/>: Sie
        /// ist nach der Auflösungsregel 3.4 bereits bestimmt — eigene Ladegrenze, sonst
        /// <c>Schwelle_Aus</c> für die vorrangige und <c>Schwelle_Aus_Nachrang</c> für
        /// nachrangige Anlagen. Der Kessel ist mit Vorgaberang 40 der letzte Lader; wo
        /// eine Solar-Reservezone gepflegt ist, lädt er also nur bis dorthin.
        ///
        /// KEIN <c>SenkeAbziehen</c>; Bilanzraum und Durchsatzbudget wie in Paket 4.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss, double[] absehbar)
        {
            if (a == null || a.Speicher == null) return 0;

            int i = a.Modulindex;
            if (i < 0 || i >= _anzahlZweikanalig) return 0;
            if (_restLeistung[i] <= 0) return 0;

            SimulationPufferspeicher sp = a.Speicher;

            // D5a: Beim KOMBISPEICHER ist das Durchsatzbudget die Summe beider Kanäle;
            // die gemeinsame Fassung steht in der Kaskadenschleife. Ohne Kombispeicher
            // liefert sie Anweisung für Anweisung das Bisherige.
            double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
            double durchlass = Kaskadenschleife.DurchlassBudget(sp, absehbar);
            if (ladefaehig + durchlass <= 0) return 0;

            double menge = Math.Min(MaxAbgabe(i), ladefaehig + durchlass);
            if (menge <= 0) return 0;

            double ladung = sp.Laden(menge, stunde, durchlass);
            if (ladung <= 0) return 0;

            double genutzterDurchlass = ladung - ladefaehig;
            if (genutzterDurchlass > 0)
                Kaskadenschleife.DurchlassBuchen(sp, absehbar, genutzterDurchlass);

            _kesselAbgabe[i] += ladung;

            // D5a: Auch die Speicherladung kann zum Teil aus dem Quellpuffer stammen.
            // Gemeldet wird sie mit ZIEL — die Kaskadenschleife bucht die Herkunft in den
            // Zielspeicher um, statt sie dem Kessel gutzuschreiben.
            double ausQuelle = QuellwaermeHolen(i, ladung, stunde, sp);
            _restLeistung[i] -= (ladung - ausQuelle);
            _kesselStunde[i] += ladung - ausQuelle;

            // E-K1-1: siehe Stunde_Bedarf — nur die Gleitkomma-Klemmung.
            if (_restLeistung[i] < 0) _restLeistung[i] = 0;

            // N1 (Paket-5-Nacharbeit): Die Speicherladung getrennt mitführen. Sie bleibt
            // Teil der Nutzwärme (der Brennstoff dafür ist geflossen), darf aber nicht als
            // BEDARFSDECKUNG gelten — sonst meldet Tab_ErgebnisHeizkessel einen negativen
            // Restwärmebedarf und eine Deckungssumme über 100 % (gemessen an 1018/1023).
            //
            // D5a: Geführt wird der EIGENE Anteil. Die Ergebnisbildung zieht diese Größe
            // von der (ebenfalls brennstoffbasierten) Nutzwärme ab; stünde hier die volle
            // Ladung, würde die Differenz um die Quellwärme zu klein — bei reiner
            // Puffer-Hauptsenke sogar negativ.
            Speicherladung_gesamt += ladung - ausQuelle;
            if (stunde >= 0 && stunde < 8760)
                Speicherladung_stuendlich[stunde] += ladung - ausQuelle;

            return ladung;
        }

        /// <summary>
        /// Brennstoffbilanz der Stunde — GENAU EINMAL je Stunde und Kessel (Konzept 6.5).
        ///
        /// Das ist die zentrale Bedingung der zweikanaligen Umstellung: Läuft der Kessel,
        /// folgt sein Verbrauch dem Wirkungsgrad; steht er, wird ihm der anteilige
        /// BEREITSCHAFTSVERLUST als Verbrauch aufgeschlagen. Würde diese Entscheidung je
        /// Kanal getroffen, fiele der Stillstandsverlust in einer Stunde zweimal an — der
        /// Jahresnutzungsgrad (Schritt 5) kippte entsprechend.
        ///
        /// Aufgerufen wird die Methode in Phase G, also nach Bedarfsdeckung, Ladephase und
        /// Nachentladung: Erst dann steht fest, was der Kessel in dieser Stunde insgesamt
        /// abgegeben hat.
        /// </summary>
        public void Stunde_Abschluss(int stunde)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                double KesselLeistung = _kesselStunde[i];

                bool oel = Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9 ||
                           Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22;

                double wirk = oel ? Kessel_Wirk_Oel_Spk[i] : Kessel_Wirk_Gas_Spk[i];
                if (wirk <= 0) wirk = 0.90; // Fallback

                double stuendlicherBrennstoffverbrauchKW;

                // D5a: „Läuft der Kessel?" entscheidet die ABGABE, nicht der
                // brennstoffbasierte Anteil. Ohne Quellpuffer sind beide gleich, und die
                // Verzweigung ist Wort für Wort die bisherige.
                if (_kesselAbgabe[i] > 0)
                {
                    // Kessel läuft -> Verbrauch über Wirkungsgrad (in dieser Stunde kein Stillstandsverlust)
                    stuendlicherBrennstoffverbrauchKW = KesselLeistung / wirk;

                    if (oel)
                    {
                        s_waerme_Oel_Spk[i] += KesselLeistung;
                    }
                    else
                    {
                        s_waerme_Gas_Spk[i] += KesselLeistung;

                        double Gasleistung = KesselLeistung / wirk;
                        if (_gasspitzeKessel[i] < Gasleistung) _gasspitzeKessel[i] = Gasleistung;
                    }
                }
                else
                {
                    // Kessel steht in dieser Stunde still -> Bereitschaftsverlust, EINMAL.
                    stuendlicherBrennstoffverbrauchKW = Betriebsbereitschaft_Verluste[i] * Kessel_Leistung_Spk[i];
                }

                Kessel_Verbrauch_MWh_Spk[i] += stuendlicherBrennstoffverbrauchKW;

                if (stunde >= 0 && stunde < 8760)
                    Kesselleistung_stuendlich[stunde] += (float)KesselLeistung;
            }
        }

        /// <summary>Jahressummen, Emissionen und Jahresnutzungsgrad des zweikanaligen Wegs.</summary>
        public void Abschluss_Zweikanalig()
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                s_waerme_Gas_Spk[i] /= 1000;
                s_waerme_Oel_Spk[i] /= 1000;
                Kessel_Verbrauch_MWh_Spk[i] /= 1000;
                Gasspitze_Spk += _gasspitzeKessel[i];
            }

            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000;

            Bilanz_und_Nutzungsgrad(_anzahlZweikanalig);
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte in einer
        /// eigenen Jahresschleife an der Kaskadenposition des Heizkessels.
        ///
        /// Der Weg für Projekte, in denen kein Kessel eine Puffer-Senke trägt. Ohne
        /// Speicher haben die Phasen A, C, D und E für diese Stufe keinen Inhalt; Phase G
        /// beschränkt sich auf die Brennstoffbilanz der Stunde. Gegenüber der bis Paket A1
        /// ändert sich allein die Kanalführung — die je Stunde und Kessel abgegebene
        /// Nutzwärme, der Brennstoffverbrauch und die Restwärme sind dieselben Zahlen.
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
                Stunde_Abschluss(stunde);

                for (int k = 0; k < Kanal.ANZAHL; k++)
                    kanaele.Bedarf[k][stunde] = (float)rest[k];
            }

            Abschluss_Zweikanalig();
            return true;
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays müssen die gleiche Länge aufweisen.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++) { result[i] = array1[i] + array2[i]; }
            return result;
        }

        public void Init()
        {
            // N8 (Paket-5-Nacharbeit): Die Kesselzahl des zweikanaligen Wegs gehört zum
            // Zustand dieses Moduls und muss deshalb HIER zurückgesetzt werden. Bisher
            // stand sie nur am Ende von Vorbereiten_Zweikanalig - bricht das mittendrin
            // ab (Kessel nicht im Projekt), stünde der Wert des Vorlaufs neben einer
            // bereits geleerten _kesselSenke, und die Stundenschritte liefen über
            // Kessel, die es in diesem Lauf nicht gibt.
            _anzahlZweikanalig = 0;

            // Speichergrößen (Paket 5 / Nacharbeit N1, N2): Ohne Puffer-Senke bleiben diese
            // Größen auf 0, damit die Ergebnisbildung in SimulationRunner dort
            // nachweislich bitgleich der bisherigen ist.
            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            Speicherladung_gesamt = 0;
            Speicherentladung_Anteil = 0;

            // K2: die Kanalaufschlüsselung derselben Größen (Konzept 4.4).
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);

            // E2: und ihre Ganglinienfassung, an derselben Stelle.
            Direktdeckung_KanalStuendlich.Nullen();
            Speicherentladung_KanalStuendlich.Nullen();

            // D5a: Der Quellbezug gehört zum Laufzustand. ModulEbenen/AktiveEbene setzt
            // die Kaskadenschleife je Lauf neu; die Quellpuffer setzt SimulationControl,
            // nachdem die Registry offen ist.
            Array.Clear(Quellwaerme_stuendlich, 0, Quellwaerme_stuendlich.Length);
            Quellwaerme_gesamt = 0;
            Quellentnahmen.Clear();
            Array.Clear(_quellSpeicher, 0, _quellSpeicher.Length);
            Array.Clear(_quellAnteil, 0, _quellAnteil.Length);
            Array.Clear(_kesselAbgabe, 0, _kesselAbgabe.Length);

            // PAKET B1: Die Temperaturkopplung gehört aus demselben Grund zum
            // Laufzustand - sie wird beim Aufbau der Quellbezüge neu gesetzt.
            Array.Clear(_quellKopplung, 0, _quellKopplung.Length);
            Array.Clear(_quellVorlauf, 0, _quellVorlauf.Length);
            Array.Clear(_quellRuecklauf, 0, _quellRuecklauf.Length);
            Array.Clear(_quellTemperatur, 0, _quellTemperatur.Length);
            Array.Clear(_quellZuKalt, 0, _quellZuKalt.Length);
            ModulEbenen = null;
            AktiveEbene = 0;

            Maximale_Kesselleistung_Spk = 0;
            Stromverbrauch_Spk = 0;

            for (int j = 0; j < MAX_SPK; j++)
            {
                s_waerme_Gas_Spk[j] = 0;
                s_waerme_Oel_Spk[j] = 0;
                Kessel_Wirk_Gas_Spk[j] = 0;
                Kessel_Wirk_Oel_Spk[j] = 0;
                Betriebsbereitschaft_Verluste[j] = 0;
                Kessel_Name[j] = "";
                Brennstoff_Betrieb_Spk[j] = 0;
                Kessel_Leistung_Spk[j] = 0;
                Kessel_Verbrauch_MWh_Spk[j] = 0;
                Kessel_Jahresnutzungsgrad_Spk[j] = 0;

                CO2_SPK[j] = 0;
                CO_SPK[j] = 0;
                CO2_SPK[j] = 0;
                SO2_SPK[j] = 0;
                NOX_SPK[j] = 0;
                Staub_SPK[j] = 0;
            }

            BruttoWaermeSpkErzeugung = 0;
            S_Waerme_spk = 0;
            Gasverbrauch_SPK = 0;
            Oelverbrauch_SPK = 0;
            Rapsoelverbrauch_SPK = 0;
            Holzverbrauch_SPK = 0;
            Sonstigverbrauch_SPK = 0;
            Stromverbrauch_Spk = 0;
            Kohle_SPK = 0;
            Koks_SPK = 0;
            Pellets_SPK = 0;
            TierischeFette_SPK = 0;

            Em_CO2_SPK = 0;
            Em_CO_SPK = 0;
            Em_SO2_SPK = 0;
            Em_NOX_SPK = 0;
            Em_Staub_SPK = 0;

            Gasspitze_Spk = 0;

            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Stromverbrauch_stuendlich, 0, Stromverbrauch_stuendlich.Length);
            Array.Clear(Kesselleistung_stuendlich, 0, Kesselleistung_stuendlich.Length);
        }
    }
}