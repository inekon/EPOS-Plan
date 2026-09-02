using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Photovoltaik-Modul der Simulationskette: Erzeugung, Direktverbrauch,
    /// Überschuss und Reststrom im Stundenraster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Reine PV-Rechnung seit AP2b.</b> Bis dahin steckte hier eine zweite,
    /// verlustfreie Batterielogik (Fachkonzept 8.2, Rudiment 2): Sie lud aus dem
    /// PV-Überschuss, entlud gegen die Restlast und schlug die Entnahme dem
    /// PV-Ertrag zu. Der Speicher wird jetzt ausschließlich von der
    /// <c>SpeicherEngine</c> gerechnet (<c>StromspeicherSimCtrl</c>), diese Klasse
    /// kennt ihn nicht mehr.
    /// </para>
    /// <para>
    /// <b>Geänderte Ausweissemantik.</b> <see cref="Stromproduktion"/> ist seither
    /// der Direktverbrauch (die frühere Reihe <c>Stromproduktion_OhneSpeicher</c>),
    /// <see cref="Ueberschuss"/> der volle Erzeugungsüberschuss vor Speicherladung
    /// und <see cref="Reststrom"/> die Residuallast vor Speicherentladung. Der
    /// PV-Ertragsausweis der Oberfläche fällt dadurch um die frühere Speicherentnahme
    /// niedriger aus; die Speicherwirkung wird getrennt ausgewiesen (Umsetzungskonzept
    /// Frage 12).
    /// </para>
    /// </remarks>
    public class SimulationPV
    {
        // --- Datenstrukturen ---
        public List<int> photovoltaik_list = new List<int>();
        // Ergebnis je PV-Modul(feld) fuer die Auflistung in der Ergebnismaske.
        public List<PVModulErgebnis> Modul_Ergebnisse = new List<PVModulErgebnis>();
        public int m_ID_Projekt = 0;

        // Input-Arrays (15-Minuten-Werte vom Lastprofil)
        public float[] Strombedarf = new float[8760 * 4];

        // Interne Stunden-Arrays für die Simulation
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] pvPotentialGesamt_stuendlich = new float[8760];

        // Ergebnis-Arrays (Stündlich)
        public float[] Stromproduktion_Theoretisch = new float[8760];
        public float[] Stromproduktion = new float[8760];
        public float[] Reststrom = new float[8760];
        public float[] Ueberschuss = new float[8760];

        // Ergebnis-Arrays (Viertelstündlich für das UI/Chart)
        public float[] Stromproduktion_viertelstunde = new float[8760 * 4];
        public float[] Reststrom_viertelstunde = new float[8760 * 4];
        public float[] Ueberschuss_viertelstunde = new float[8760 * 4];

        /// <summary>
        /// V1 (PV-Konzept § 2.3, Etappe P1): BHKW-Stromüberschuss, der als NEGATIVER
        /// Restbedarf im übergebenen Strombedarf steht (der BHKW-Abzug klemmt bewusst
        /// nicht auf 0, damit die SpeicherEngine ihn laden kann). Er ist KEINE
        /// PV-Erzeugung und gehört nicht in <see cref="Ueberschuss"/> — sonst würde
        /// er als PV-Einspeisung vergütet. Hier getrennt ausgewiesen [kWh je Stunde].
        /// </summary>
        public float[] BhkwUeberschuss = new float[8760];

        /// <summary>Jahressumme von <see cref="BhkwUeberschuss"/> [kWh].</summary>
        public float BhkwUeberschuss_gesamt = 0;

        // Statistiken
        public double Stromproduktion_Max = 0;
        public double MaxPSolar = 0;
        public float Stromproduktion_gesamt = 0;
        public float Stromproduktion_Theoretisch_gesamt = 0;

        // =================================================================================
        // Vorgabewerte und Plausibilitaetsfenster (Stufe E1, Paket A)
        // =================================================================================

        /// <summary>
        /// Wechselrichter-Wirkungsgrad, wenn die Anlage keinen fuehrt
        /// (<c>Tab_Energieanlagen.PV_WrWirkungsgrad</c> NULL) — der bis Paket A fest
        /// verdrahtete Faktor. NICHT in <c>DbWerte</c>: Das ist keine Persistenzgroesse,
        /// sondern eine Rechenannahme.
        /// </summary>
        public const double WR_WIRKUNGSGRAD_VORGABE = 0.95;

        /// <summary>Systemverluste, wenn die Anlage keine fuehrt [%] — ergebnisneutral.</summary>
        public const double SYSTEMVERLUSTE_VORGABE = 0.0;

        /// <summary>Rueckfall der Zelltemperatur-Kennzahl NOCT [Grad C] (Stufe E1.2).</summary>
        public const double NOCT_RUECKFALL = 45.0;

        /// <summary>
        /// Physikalisches Fenster fuer <c>Tab_PV.T_NOCT</c> [Grad C]. Reale Module liegen
        /// bei 42…48 °C; ausserhalb 20…60 °C gilt der Katalogwert als nicht gepflegt.
        ///
        /// <para><b>Ein FENSTER, nicht „&gt; 0".</b> Der Katalog des Bestands ist an dieser
        /// Stelle vergiftet: In allen sechs Modulen der Referenzmenge steht in
        /// <c>T_NOCT</c> (wie in <c>alpha_SC</c> und <c>beta_OC</c>) der Wert von
        /// <c>I_Kurzschluss</c>, etwa 9,014. Der ist positiv und liefe mit dem Kriterium
        /// „&gt; 0" glatt in die Formel — <c>(9,014 − 20)/800</c> ist NEGATIV und ergaebe
        /// eine Zelltemperatur UNTER der Aussentemperatur bei Einstrahlung, also
        /// Mehrertrag statt der erwarteten ±0,5 %.</para>
        /// </summary>
        public const double NOCT_MIN = 20.0;

        /// <summary>Obergrenze des NOCT-Fensters [Grad C]; siehe <see cref="NOCT_MIN"/>.</summary>
        public const double NOCT_MAX = 60.0;

        /// <summary>
        /// Grenze der Katalog-Konsistenzpruefung (Stufe E1.1): Weichen
        /// <c>Leistung</c> und <c>Laenge·Breite·Wirkungsgrad·1000</c> um mehr als diesen
        /// Anteil voneinander ab, sind es zwei Wahrheiten ueber dieselbe Anlage.
        /// </summary>
        public const double KATALOG_TOLERANZ = 0.03;

        /// <summary>Unterste zulaessige Plausibilitaetsgrenze fuer gamma_PMP [%/K].</summary>
        public const double GAMMA_MIN = -1.0;

        /// <summary>
        /// Setzt ALLE Ergebnisgroessen zurueck (Stufe E1.5).
        ///
        /// <para>Bis Paket A blieben <see cref="Stromproduktion_Max"/>,
        /// <see cref="MaxPSolar"/>, die beiden <c>*_gesamt</c>-Summen und die drei
        /// <c>*_viertelstunde</c>-Reihen stehen. Innerhalb EINES Laufs ist das folgenlos
        /// (sie werden am Ende von <see cref="Berechnung"/> gesetzt bzw. neu gebaut) —
        /// aber die Instanz ueberlebt den Lauf, und ein zweiter Lauf auf demselben Objekt
        /// haette die Maxima des ersten weitergefuehrt. Zuruecksetzen heisst hier: was
        /// <see cref="Berechnung"/> fuellt, faengt bei 0 an.</para>
        /// </summary>
        public void Init()
        {
            Array.Clear(Stromproduktion, 0, Stromproduktion.Length);
            Array.Clear(Stromproduktion_Theoretisch, 0, Stromproduktion_Theoretisch.Length);
            Array.Clear(Reststrom, 0, Reststrom.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            Array.Clear(BhkwUeberschuss, 0, BhkwUeberschuss.Length);
            BhkwUeberschuss_gesamt = 0;
            Array.Clear(pvPotentialGesamt_stuendlich, 0, pvPotentialGesamt_stuendlich.Length);
            Modul_Ergebnisse.Clear();

            // E1.5: die Skalare und die Viertelstundenreihen gehoerten von Anfang an
            // hierher.
            Stromproduktion_Max = 0;
            MaxPSolar = 0;
            Stromproduktion_gesamt = 0;
            Stromproduktion_Theoretisch_gesamt = 0;
            Array.Clear(Stromproduktion_viertelstunde, 0, Stromproduktion_viertelstunde.Length);
            Array.Clear(Reststrom_viertelstunde, 0, Reststrom_viertelstunde.Length);
            Array.Clear(Ueberschuss_viertelstunde, 0, Ueberschuss_viertelstunde.Length);
        }

        public float[] Berechnung(int ID_Projekt)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();
            int nID_Klimaregion = 0;
            double Lon = 0, Lat = 0;

            Init();

            // Bedarf von 15-Min auf 1-Std mitteln
            Strombedarf_stuendlich = Viertelstunden_zu_stunden(Strombedarf);

            // Geodaten laden
            rs.Open("select * from Tab_Projekt where ID=" + ID_Projekt);
            if (rs.Next()) nID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            rs.Close();

            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrlklima.ReadSingle("select * from Tab_Klimaregion where ID=" + nID_Klimaregion);
            if (ctrlklima.rows > 0) { Lon = ctrlklima.Longitude; Lat = ctrlklima.Latitude; }

            // PV-POTENTIAL ALLER MODULE SAMMELN
            ctrl.ReadAllFilter("ID_Projekt=" + ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            
            for (int n = 0; n < ctrl.rows; n++)
            {
                PhotovoltaikCtrl ctrlsol = new PhotovoltaikCtrl();
                ctrlsol.ReadSingle(ctrl.items[n].ID_PV);

                long anzahlModule = (long)ctrl.items[n].PV_Leistung;   // PV_Leistung ist die MODULANZAHL
                double nFlaecheGesamt = ctrlsol.m_Breite * ctrlsol.m_Laenge * anzahlModule;
                double nennWirk = ctrlsol.m_Wirkungsgrad / 100.0;
                double tempKoeff = ctrlsol.m_Temp_Coeff_Pmax / 100.0;

                // --- Stufe E1: was das Modul und die Anlagenzeile beitragen -------------
                double pStcKw = PStcDerAnlage(ctrlsol, anzahlModule);   // 0 = Rueckfall Flaechenformel
                double tNoct = NoctDesModuls(ctrlsol);
                GammaPruefen(ctrlsol);

                // E1.3: Wechselrichter und Systemverluste JE ANLAGE. NULL = 0,95 bzw. 0 %,
                // damit ist der Vorgabefall bitgleich zum Bestand (Faktor 1,0 ist exakt).
                double etaWr = ctrl.items[n].PV_WrWirkungsgrad ?? WR_WIRKUNGSGRAD_VORGABE;
                double systemFaktor = 1.0 - (ctrl.items[n].PV_Systemverluste ?? SYSTEMVERLUSTE_VORGABE) / 100.0;

                // B1: der Ortszeit-Lesepfad. Die Zeile traegt ihre UTC-Herkunft mit -
                // der Sonnenstand rechnet weiter auf UTC-Basis.
                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadOrtszeit(nID_Klimaregion, ID_Projekt);

                double prodSummeMod = 0;

                // B4: auf das feste Jahresraster geklemmt. Ohne die Klemme lief eine
                // ueberlange Reihe in float[8760] und warf IndexOutOfRange.
                int stunden = Math.Min(ctrldat.rows, 8760);

                for (int i = 0; i < stunden; i++)
                {
                    SolardatenModel zeile = ctrldat.items[i];

                    // E1.4: TagUtc ist 1-BASIERT (1…365) - genau das erwartet
                    // CalculateHourly (und genau das liefert der Klimadaten-Import mit
                    // dt.DayOfYear). Bis Paket A stand hier i/24, also 0…364.
                    double effStr = SolarCalculator.CalculateHourly(Lon, Lat, ctrl.items[n].m_Neigung, ctrl.items[n].m_Azimut,
                                    zeile.Globalstrahlung, zeile.Direktstrahlung,
                                    zeile.Diffusstrahlung, zeile.Außen_Temp, zeile.TagUtc, zeile.StundeUtc);

                    if (effStr > MaxPSolar) MaxPSolar = effStr;

                    // Theoretische Erzeugung dieses Moduls berechnen
                    var erg = BerechnePV(Strombedarf_stuendlich[i], effStr, nFlaecheGesamt, nennWirk, tempKoeff,
                                         zeile.Außen_Temp, 1.0, pStcKw, tNoct);

                    // Aufsummieren auf das Stunden-Array (nach Wechselrichter und
                    // Systemverlusten - E1.3)
                    pvPotentialGesamt_stuendlich[i] += (float)(erg.potenzielleErzeugung * etaWr * systemFaktor);

                    prodSummeMod += erg.potenzielleErzeugung * etaWr * systemFaktor;
                }

                Modul_Ergebnisse.Add(new PVModulErgebnis
                {
                    Name = ctrl.items[n].Bezeichner,
                    Flaeche = nFlaecheGesamt,
                    Anzahl = anzahlModule,
                    Stromproduktion = prodSummeMod
                });
            }

            // SCHRITT: ZEITSCHRITT-SIMULATION (VERBRAUCH)
            for (int i = 0; i < 8760; i++)
            {
                double erzeugung = pvPotentialGesamt_stuendlich[i];
                double bedarfRoh = Strombedarf_stuendlich[i];

                // V1 (PV-Konzept § 2.3, Etappe P1): Ein NEGATIVER Restbedarf ist
                // BHKW-Überschuss — kein Bedarf und keine PV-Größe. Ohne die Klemme
                // wurde Min(erzeugung, bedarf) negativ und der BHKW-Überschuss
                // wanderte über „erzeugung − direktVerbrauch" in die PV-Einspeise-
                // reihe (Projekt 1018: 24.532 negative Viertelstunden). Für Projekte
                // ohne BHKW-Überschuss ist bedarfRoh nie negativ — ihr Ergebnis
                // bleibt identisch (Abnahmekriterium P1).
                double bedarf = Math.Max(0, bedarfRoh);
                BhkwUeberschuss[i] = (float)Math.Max(0, -bedarfRoh);

                Stromproduktion_Theoretisch[i] = (float)erzeugung;

                // Direktverbrauch - seit AP2b der EINZIGE Verrechnungsschritt hier.
                double direktVerbrauch = Math.Min(erzeugung, bedarf);

                // Ergebnisse für diese Stunde festschreiben
                Ueberschuss[i] = (float)(erzeugung - direktVerbrauch);   // Was ins Netz geht
                Reststrom[i] = (float)(bedarf - direktVerbrauch);        // Was vom Netz kommt
                Stromproduktion[i] = (float)direktVerbrauch;             // Genutzte Produktion

                if (erzeugung > Stromproduktion_Max) Stromproduktion_Max = erzeugung;
            }

            // SUMMEN & KONVERTIERUNG
            Stromproduktion_gesamt = Stromproduktion.Sum();
            Stromproduktion_Theoretisch_gesamt = Stromproduktion_Theoretisch.Sum();
            BhkwUeberschuss_gesamt = BhkwUeberschuss.Sum();

            // Für den Chart aufbereiten
            Stromproduktion_viertelstunde = Stundenwerte_zu_viertelstunden(Stromproduktion);
            Reststrom_viertelstunde = Stundenwerte_zu_viertelstunden(Reststrom);
            Ueberschuss_viertelstunde = Stundenwerte_zu_viertelstunden(Ueberschuss);

            return Stromproduktion_viertelstunde;
        }

        // --- Hilfsmethoden ---

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {
            float[] v = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                v[i * 4] = v[i * 4 + 1] = v[i * 4 + 2] = v[i * 4 + 3] = stundenwerte[i];
            }
            return v;
        }

        // Stundenwerte_zu_viertelstunden_Interpoliert ist mit AP2b entfallen: Die
        // lineare Spreizung glättete allein die Treppenstufen des stündlich gerechneten
        // Speicherfüllstands. Die SpeicherEngine liefert den Ladezustand nativ
        // viertelstündlich (SimulationControl.Speicherfuellstand_viertelstuendlich),
        // die Interpolation hat damit keinen Gegenstand mehr.

        public float[] Viertelstunden_zu_stunden(float[] v)
        {
            float[] s = new float[v.Length / 4];
            for (int i = 0; i < s.Length; i++)
            {
                s[i] = (v[i * 4] + v[i * 4 + 1] + v[i * 4 + 2] + v[i * 4 + 3]) / 4.0f;
            }
            return s;
        }

        /// <summary>
        /// Die Modulformel einer Stunde.
        ///
        /// <para><b>Stufe E1.1 — P_STC statt Flaeche x Wirkungsgrad.</b> Ist
        /// <paramref name="pStcKw"/> gesetzt, gilt
        /// <c>P_DC[kW] = P_STC[kW] · (G·cosTheta / 1000) · (1 + gamma·(T_Zelle − 25))</c>.
        /// Physikalisch ist das identisch zur Flaechenformel, SOLANGE
        /// <c>Wirkungsgrad = Leistung/(L·B·1000)</c> gilt — es bindet die Simulation aber
        /// an dieselbe Groesse wie <c>PhotovoltaikCtrl.KwpDesProjekts</c>, also an die
        /// kWp der Verguetungsrechnung. Zwei Wahrheiten ueber eine Anlage werden damit
        /// eine.</para>
        ///
        /// <para><b>Stufe E1.2 — T_NOCT.</b> <c>T_Zelle = T_amb + (NOCT − 20)/800 · G</c>.
        /// Bewusst als <c>(G / 800) · (NOCT − 20)</c> geschrieben: Mit dem Rueckfall
        /// NOCT = 45 ist das ZEICHENGLEICH die alte Zeile <c>(G/800) · 25</c> und damit
        /// bitgleich — eine algebraisch gleichwertige Umstellung waere es im letzten Bit
        /// nicht.</para>
        ///
        /// <para>Beide neuen Parameter haben Vorgabewerte, die den Bestand abbilden:
        /// <c>pStcKw = 0</c> heisst Flaechenformel, <c>tNoct = 45</c> die alte Konstante.
        /// Der Entwickler-Selbsttest <c>SimulationControl.TestePVAnlage</c> ruft die
        /// Methode weiterhin mit sieben Argumenten auf.</para>
        /// </summary>
        /// <param name="pStcKw">Nennleistung des MODULFELDS [kWp]; 0 = Flaechenformel.</param>
        /// <param name="tNoct">NOCT des Moduls [Grad C]; Vorgabe <see cref="NOCT_RUECKFALL"/>.</param>
        public (double produktion, double restbedarf, double ueberschuss, double potenzielleErzeugung) BerechnePV(
                double bedarf, double strahlung, double flaeche, double nennWirk, double tempKoeff, double tAmb, double cosTheta,
                double pStcKw = 0.0, double tNoct = NOCT_RUECKFALL)
        {
            double tCell = tAmb + (strahlung / 800.0) * (tNoct - 20.0);
            double tempFaktor = 1 + tempKoeff * (tCell - 25.0);

            double potErzeugung = pStcKw > 0.0
                ? pStcKw * (strahlung * cosTheta / 1000.0) * tempFaktor
                : (strahlung * cosTheta * flaeche * (nennWirk * tempFaktor)) / 1000.0;

            double prod = Math.Min(potErzeugung, bedarf);
            double rest = Math.Max(0, bedarf - prod);
            double ueb = Math.Max(0, potErzeugung - bedarf);

            return (prod, rest, ueb, potErzeugung);
        }

        // =================================================================================
        // Stufe E1: Katalogwerte pruefen und aufloesen
        // =================================================================================

        /// <summary>
        /// Die Nennleistung des Modulfelds [kWp] aus <c>Tab_PV.Leistung</c> (E1.1) — 0,
        /// wenn der Katalog keine fuehrt und deshalb die Flaechenformel gilt.
        ///
        /// <para>Nebenbei die <b>Konsistenzpruefung</b>: Weichen <c>Leistung</c> und
        /// <c>Laenge·Breite·Wirkungsgrad·1000</c> um mehr als
        /// <see cref="KATALOG_TOLERANZ"/> voneinander ab, sind die Ertragsrechnung und die
        /// kWp der Verguetung mit demselben Katalogeintrag nicht mehr in Deckung. Die
        /// Aenderung ist dann gewollt (Entscheidung Q1) — der Hinweis erklaert sie.</para>
        /// </summary>
        private double PStcDerAnlage(PhotovoltaikCtrl modul, long anzahlModule)
        {
            string modulName = string.IsNullOrEmpty(modul.m_szName) ? ("ID " + modul.m_ID) : modul.m_szName;

            if (modul.m_Leistung <= 0)
            {
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "pv-pstc-fehlt-" + modul.m_ID,
                    "PV-Modul \"" + modulName + "\": Der Katalog fuehrt keine Nennleistung " +
                    "(Tab_PV.Leistung = 0). Gerechnet wird ersatzweise ueber Flaeche x " +
                    "Wirkungsgrad wie bisher. Eine gepflegte Nennleistung brauchte auch die " +
                    "kWp-Ermittlung der Verguetungsrechnung.");
                return 0.0;
            }

            double ausFlaeche = modul.m_Laenge * modul.m_Breite * (modul.m_Wirkungsgrad / 100.0) * 1000.0;
            double abweichung = Math.Abs(modul.m_Leistung - ausFlaeche) / modul.m_Leistung;

            if (abweichung > KATALOG_TOLERANZ)
            {
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "pv-katalog-inkonsistent-" + modul.m_ID,
                    "PV-Modul \"" + modulName + "\": Die Nennleistung " +
                    modul.m_Leistung.ToString("N2", CultureInfo.InvariantCulture) + " W und der " +
                    "Wert aus Laenge x Breite x Wirkungsgrad (" +
                    ausFlaeche.ToString("N2", CultureInfo.InvariantCulture) + " W) weichen um " +
                    (abweichung * 100.0).ToString("N1", CultureInfo.InvariantCulture) + " % " +
                    "voneinander ab. Gerechnet wird mit der Nennleistung - derselben Groesse, " +
                    "aus der die Verguetungsrechnung ihre kWp bildet. Bitte den Katalogeintrag " +
                    "pruefen.");
            }

            return modul.m_Leistung / 1000.0 * anzahlModule;
        }

        /// <summary>
        /// Der NOCT des Moduls [Grad C] (E1.2) — <see cref="NOCT_RUECKFALL"/>, wenn der
        /// Katalogwert ausserhalb des physikalischen Fensters
        /// <see cref="NOCT_MIN"/>…<see cref="NOCT_MAX"/> liegt. Warum ein Fenster und
        /// nicht „&gt; 0": siehe <see cref="NOCT_MIN"/>.
        /// </summary>
        private double NoctDesModuls(PhotovoltaikCtrl modul)
        {
            double noct = modul.m_T_NOCT;
            if (noct >= NOCT_MIN && noct <= NOCT_MAX) return noct;

            string modulName = string.IsNullOrEmpty(modul.m_szName) ? ("ID " + modul.m_ID) : modul.m_szName;
            SimulationProtokoll.Aktuell.HinweisEinmal(
                "pv-noct-rueckfall-" + modul.m_ID,
                "PV-Modul \"" + modulName + "\": T_NOCT ist mit " +
                noct.ToString("N3", CultureInfo.InvariantCulture) + " Grad C nicht plausibel " +
                "(erwartet werden " + NOCT_MIN.ToString("N0", CultureInfo.InvariantCulture) + " bis " +
                NOCT_MAX.ToString("N0", CultureInfo.InvariantCulture) + " Grad C). Gerechnet wird " +
                "mit dem Rueckfall " + NOCT_RUECKFALL.ToString("N0", CultureInfo.InvariantCulture) +
                " Grad C. Der Wert laesst sich im Modulkatalog pflegen.");
            return NOCT_RUECKFALL;
        }

        /// <summary>
        /// Plausibilitaet von <c>gamma_PMP</c> (E1.5) — <b>ohne Rechenaenderung</b>: Der
        /// Katalogwert geht so in die Formel, wie er dasteht. Gemeldet wird nur, was
        /// erklaerungsbeduerftig ist.
        ///
        /// <para>0 heisst „kein Temperaturgang hinterlegt" (Hinweis) — im Bestand der Fall
        /// beim Jinkosolar-Modul und damit in vier Referenzprojekten. Ein POSITIVES gamma
        /// wuerde den Ertrag bei Waerme ERHOEHEN, ein Wert unter −1 %/K liegt jenseits
        /// jeder Modultechnik: beides eine Warnung.</para>
        /// </summary>
        private void GammaPruefen(PhotovoltaikCtrl modul)
        {
            double gamma = modul.m_Temp_Coeff_Pmax;    // [%/K], so wie im Katalog
            string modulName = string.IsNullOrEmpty(modul.m_szName) ? ("ID " + modul.m_ID) : modul.m_szName;

            if (gamma == 0.0)
            {
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "pv-gamma-null-" + modul.m_ID,
                    "PV-Modul \"" + modulName + "\": Es ist kein Temperaturgang hinterlegt " +
                    "(gamma_PMP = 0). Die Anlage rechnet damit ohne jeden Temperatureinfluss - " +
                    "reale Module verlieren rund 0,3 bis 0,45 % Leistung je Kelvin.");
                return;
            }

            if (gamma > 0.0 || gamma < GAMMA_MIN)
            {
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "pv-gamma-unplausibel-" + modul.m_ID,
                    "PV-Modul \"" + modulName + "\": gamma_PMP ist mit " +
                    gamma.ToString("N4", CultureInfo.InvariantCulture) + " %/K nicht plausibel " +
                    "(erwartet wird " + GAMMA_MIN.ToString("N1", CultureInfo.InvariantCulture) +
                    " bis 0). Gerechnet wird unveraendert mit diesem Wert; ein positives gamma " +
                    "ERHOEHT den Ertrag bei Waerme.");
            }
        }
    }

    // Ergebnis eines einzelnen PV-Modul(felds) fuer die Ergebnis-Auflistung.
    public class PVModulErgebnis
    {
        public string Name = "";
        public double Flaeche;          // m^2 gesamt
        public long Anzahl;             // Modulanzahl
        public double Stromproduktion;  // kWh/a (theoretisch, nach Wechselrichter)
    }
}