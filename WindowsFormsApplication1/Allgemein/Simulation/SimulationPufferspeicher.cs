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
        public const string VERWENDUNG_HEIZUNG = "Heizung";

        /// <summary>Verwendung "Quelle": Quellspeicher einer Wärmepumpe (Wärmequelle).</summary>
        public const string VERWENDUNG_QUELLE = "Quelle";

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

        // Ganglinien für Auswertung, Charts und CSV-Export
        public float[] SOC_stuendlich = new float[8760];
        public float[] Ladung_stuendlich = new float[8760];
        public float[] Entladung_stuendlich = new float[8760];

        // Jahressummen [kWh]
        public double Ladung_gesamt = 0;
        public double Entladung_gesamt = 0;
        public double Verluste_gesamt = 0;

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
        /// (Division-durch-Null-Absicherung). Bezugsgröße ist der NUTZUMSATZ und der
        /// hängt an der Rolle:
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
        public void Init(double volumenLiter, int vorlauf, int ruecklauf, double bereitschaftsverlusteProTag)
        {
            double deltaT = vorlauf - ruecklauf;
            if (deltaT <= 0) deltaT = 10; // Fallback, falls keine Temperaturen gepflegt sind

            // 1,16 Wh/(l*K) -> kWh
            Q_max = volumenLiter * 1.16 * deltaT / 1000.0;
            VerlustProStunde = bereitschaftsverlusteProTag / 24.0;
            Reset();
        }

        /// <summary>Setzt den Speicherzustand für einen neuen Simulationslauf zurück.</summary>
        public void Reset()
        {
            SOC = 0;
            Ladung_gesamt = 0;
            Entladung_gesamt = 0;
            Verluste_gesamt = 0;
            SOC_Mittel = 0;
            SOC_Max = 0;
            Vollzyklen = 0;
            Array.Clear(SOC_stuendlich, 0, SOC_stuendlich.Length);
            Array.Clear(Ladung_stuendlich, 0, Ladung_stuendlich.Length);
            Array.Clear(Entladung_stuendlich, 0, Entladung_stuendlich.Length);
        }

        /// <summary>
        /// Lädt den Speicher mit der angebotenen Energie [kWh] und liefert zurück,
        /// wie viel davon tatsächlich aufgenommen wurde (Rest: Speicher voll).
        /// </summary>
        public double Laden(double energieKWh, int stunde)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            double frei = Q_max - SOC;
            double ladung = Math.Min(energieKWh, frei);
            if (ladung <= 0) return 0;

            SOC += ladung;
            Ladung_gesamt += ladung;
            if (stunde >= 0 && stunde < 8760) Ladung_stuendlich[stunde] += (float)ladung;
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

            SOC -= entnahme;
            Entladung_gesamt += entnahme;
            if (stunde >= 0 && stunde < 8760) Entladung_stuendlich[stunde] += (float)entnahme;
            return entnahme;
        }

        /// <summary>
        /// Verrechnet den stündlichen Bereitschaftsverlust (anteilig zum Füllstand)
        /// und speichert den Speicherzustand der Stunde für die Auswertung.
        /// </summary>
        public void StundeAbschliessen(int stunde)
        {
            if (Q_max > 0 && SOC > 0)
            {
                double verlust = VerlustProStunde * (SOC / Q_max);
                if (verlust > SOC) verlust = SOC;
                SOC -= verlust;
                Verluste_gesamt += verlust;
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

        /// <summary>Anzeigetext der Rolle (deutsch; Lokalisierung folgt mit Paket 9).</summary>
        public string RolleAnzeige()
        {
            return (Verwendung == VERWENDUNG_QUELLE) ? "Quellspeicher" : "Senkenspeicher";
        }

        /// <summary>
        /// Bezeichner für Anzeigen und Exportköpfe, mit dem einen Ersatztext für
        /// namenlose Speicher. Bewusst an EINER Stelle: der Text stand vorher in
        /// NavigatorWaerme, Form_Simulation_Detail (Tabelle) und dessen CSV-Export
        /// je einmal - drei Kopien, die auseinanderlaufen konnten.
        /// </summary>
        public string BezeichnerAnzeige()
        {
            return string.IsNullOrEmpty(Bezeichner) ? "Speicher" : Bezeichner;
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
