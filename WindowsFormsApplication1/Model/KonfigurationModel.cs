namespace WindowsFormsApplication1
{
    public class KonfigurationModel
    {
        public int m_ID;
        public int m_ID_Projekt;
        public double m_Netzverluste;
        public string m_szNetzverlusteEinheit;
        public double m_BHKW_Grenzleistung;
        public bool m_WP_Heizstab;
        public int m_Kessel_Betriebsbereitschaft;
        public string m_Tool_1;
        public string m_Tool_2;
        public string m_Tool_3;
        public string m_Tool_4;
        public string m_Tool_5;
        public string m_Tool_6;
        public int m_Ladefuellstand_Min;
        public int m_Ladefuellstand_Max;
        public int m_Ladeleistung_Max;
        public double m_Ladeschwellwert;
        public string m_Ladefuellstand_Min_Auswahl;
        public string m_Ladefuellstand_Max_Auswahl;
        public string m_Ladeleistung_Max_Auswahl;
        public int Betriebsart;
        public int Leistungsgrenze;

        /// <summary>
        /// TOT seit Etappe 3 (14.08.2026): Alt-Parameter Tab_Einstellungen.Pendelspeicher
        /// in m³. Wird von KonfigurationCtrl nur noch gelesen und geschrieben, damit der
        /// positionsbasierte Zugriff row[0..22] und die INSERT-/UPDATE-Spaltenlisten
        /// unverändert bleiben — den Wert wertet niemand mehr aus.
        /// Das Volumen des BHKW-Pendelspeichers steht in LITERN im Projekt-Puffer
        /// "BHKW-Pendelspeicher" (PufferSpCtrl.PendelspeicherVolumenLiter).
        /// </summary>
        public double Pendelspeicher;

        // PAKET L (Aufräumen): Das Feld Kaskade_Zweikanalig ist ENTFALLEN. Es trug die
        // Spalte Tab_Einstellungen.Kaskade_Zweikanalig, ehemals das Feature-Flag der
        // zweikanaligen Kaskade (Konzept Kapitel 9). Mit Paket A1 hat die Engine sie
        // nicht mehr gelesen, mit Paket L ist auch der letzte Lese-/Schreibweg der
        // Anwendung entfallen (KonfigurationCtrl.KaskadeZweikanalig*).
        //
        // DIE SPALTE SELBST BLEIBT (Konzept Kapitel 15, "Stillgelegt: Lese-Altlast nach
        // Migration"): Migrationsschritt 51 setzt sie im Bestand auf WAHR und loescht
        // nichts. Das Feld hier war NAMENSBASIERT gelesen und haengt deshalb NICHT an der
        // Ordinalkette row[0..22] von ReadSingle - sein Wegfall verschiebt keine Position
        // und beruehrt weder die INSERT- noch die UPDATE-Spaltenliste.

        /// <summary>
        /// Projekteinstellung „Extrapolation der Wärmepumpen-Kennlinie erlaubt"
        /// (Paket 8, Konzept 13.4), Spalte <c>Tab_Einstellungen.Extrapolation_erlaubt</c>.
        /// <b>Vorbelegung an (WAHR)</b>.
        ///
        /// Sie löst die Rückfrage „Temperatur unterschreitet Kennlinien Untergrenze, soll
        /// extrapoliert werden?" ab, die die Engine bisher mitten im Rechenlauf als
        /// MessageBox stellte und die jeden unbeaufsichtigten Lauf blockierte.
        ///
        ///   WAHR   — es wird wie bisher extrapoliert; der Lauf vermerkt das als Hinweis
        ///            im <see cref="SimulationProtokoll"/> (sichtbar statt stumm).
        ///   FALSCH — der Lauf bricht über den Fehlerkanal ab, mit sprechendem Text
        ///            statt einer MessageBox mit Abbruch.
        ///
        /// WARUM WAHR und nicht — wie Konzept 13.4 vorschlägt — „nein": WAHR ist die
        /// Antwort, die in jedem dokumentierten Lauf gegeben wurde (Referenzlauf-Suite:
        /// fünf von neun Projekten mit Rückfrage, jedes Mal „Ja"). Nur damit bleibt
        /// Paket 8 ergebnisneutral. Die Kappung auf die unterste Stützstelle, die 13.4
        /// für „nein" vorsieht, wäre eine RECHENÄNDERUNG und gehört nicht in ein
        /// Infrastrukturpaket (siehe Paket-8-Protokoll, offene Punkte).
        ///
        /// Gelesen wird NAMENSBASIERT (<c>KonfigurationCtrl.ReadSingle</c>), nicht über
        /// die Ordinalkette row[0..22]; geschrieben ausschließlich über
        /// <see cref="KonfigurationCtrl.ExtrapolationErlaubtSchreiben"/> — damit die
        /// INSERT-/UPDATE-Spaltenlisten der Konfiguration unverändert bleiben und ein
        /// noch nicht migrierter Bestand das Speichern nicht scheitern lässt.
        /// </summary>
        public bool Extrapolation_erlaubt;

        /// <summary>
        /// Projektweite KANAL-KNAPPHEITSREIHENFOLGE (Paket K2, Konzept
        /// Brauchwasser/Heizung/Pufferspeicher § 4.3, Entscheidung F10 vom 27.08.2026),
        /// Spalte <c>Tab_Einstellungen.Kanal_Knappheitsreihenfolge</c>.
        /// <b>Vorbelegung <see cref="DbWerte.KNAPPHEIT_DEFAULT"/></b>
        /// (<c>BRAUCHWASSER;PROZESS;HEIZUNG</c>).
        ///
        /// <para>Sie beantwortet die Frage, die sich mit der dreikanaligen Kaskade
        /// erstmals stellt: In welcher Rangfolge wird eine mehrelementige Kanalmaske
        /// bedient, wenn die Wärme nicht für alle reicht? Bis hierher kannte
        /// <c>SenkeAbziehen</c> nur „Warmwasser vor Heizung", fest verdrahtet; der
        /// Vorgabewert ist genau diese Regel, um den Prozesskanal ergänzt
        /// (Produktionsausfall wiegt schwerer als Raumkomfort).</para>
        ///
        /// <para>Der Wert ist eine LISTE sprachneutraler ASCII-Schlüssel, getrennt
        /// durch Semikolon — kein Anzeigetext und kein einzelner Steuerwert. Die
        /// Ausnahme von der Deutsch-Regel ist in <c>DbWerte</c> begründet
        /// (Konzept Kapitel 15).</para>
        ///
        /// <para>Gelesen wird NAMENSBASIERT (<c>KonfigurationCtrl.ReadSingle</c>), nicht
        /// über die Ordinalkette row[0..22]; geschrieben ausschließlich über
        /// <see cref="KonfigurationCtrl.KnappheitsreihenfolgeSchreiben"/> — dieselbe
        /// Begründung wie bei <see cref="Extrapolation_erlaubt"/>.</para>
        /// </summary>
        public string Kanal_Knappheitsreihenfolge = DbWerte.KNAPPHEIT_DEFAULT;

        public KonfigurationModel()
        {
            m_ID = 0;
            m_ID_Projekt = 0;
            m_Netzverluste = 0;
            m_szNetzverlusteEinheit = "";
            m_BHKW_Grenzleistung = 0;
            m_WP_Heizstab = false;
            m_Kessel_Betriebsbereitschaft = 0;
            m_Tool_1 = "";
            m_Tool_2 = "";
            m_Tool_3 = "";
            m_Tool_4 = "";
            m_Tool_5 = "";
            m_Tool_6 = "";
            m_Ladefuellstand_Min = 0;
            m_Ladefuellstand_Max = 0;
            m_Ladeleistung_Max =0;
            m_Ladeschwellwert = 0;
            m_Ladefuellstand_Min_Auswahl = "";
            m_Ladefuellstand_Max_Auswahl = "";
            m_Ladeleistung_Max_Auswahl = "";
            Betriebsart = 0;
            Leistungsgrenze = 0;
            Pendelspeicher = 0;
            Extrapolation_erlaubt = true;   // Vorbelegung: erlaubt (Konzept 13.4, Paket 8)
            // Vorbelegung BRAUCHWASSER;PROZESS;HEIZUNG (Paket K2, F10) - die bis dahin
            // fest verdrahtete Reihenfolge, um den Prozesskanal ergaenzt.
            Kanal_Knappheitsreihenfolge = DbWerte.KNAPPHEIT_DEFAULT;
        }
    }
}
