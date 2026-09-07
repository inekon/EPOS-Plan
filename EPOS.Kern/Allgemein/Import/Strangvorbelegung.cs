using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Womit ein NEU angelegter Strang anfängt</b> — Befund <b>W6‑B‑4</b> der
    /// Windows-Abnahme vom 07.09.2026.
    ///
    /// <para><b>Der Befund.</b> „Strang anlegen" erzeugte eine Zeile, in der jedes
    /// Zahlenfeld leer war; die Ampel darunter meldete daraufhin „Strang 1s1: 0 Module
    /// in Reihe, 1 parallel · Werte fehlen: Module in Reihe …". Der Anwender las das
    /// als Fehler und fragte „Anzahl Module fehlt?" — dabei KENNT die Anlage ihre
    /// Modulzahl (<c>Tab_Energieanlagen.PV_Leistung</c>, im Dialog „Anzahl Module"),
    /// und im Regelfall ist der erste Strang die ganze Anlage.</para>
    ///
    /// <para><b>Warum die Regel im KERN steht und nicht in der Razor-Komponente.</b>
    /// Sie ist Fachwissen über die Auslegung, kein Bedienverhalten: Was ein zweiter
    /// Strang bekommt, hängt an der Modulzahl der Anlage, an dem, was die bestehenden
    /// Stränge schon tragen, und an der Trackerzahl des Geräts. Eine zweite Fassung in
    /// der iOS-Schale liefe beim ersten Nachziehen auseinander (Hausregel „eine
    /// Wahrheit"). Die Komponente ruft sie und schreibt das Ergebnis in ihre Zeile.</para>
    ///
    /// <para><b>Sie ersetzt keine Prüfung.</b> Nach dem Anlegen läuft
    /// <see cref="StrangPlausibilitaet"/> wie bisher: P1 bis P8 sagen, ob die Reihe ins
    /// Spannungsfenster passt und ob die Modulsumme zur Anlage stimmt. Die Vorbelegung
    /// setzt nur den Ausgangspunkt, an dem der Planer weiterarbeitet — sie behauptet
    /// nicht, dass er richtig ist.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche</b> — reine Zahlen hinein, reine
    /// Zahlen heraus.</para>
    /// </summary>
    public static class Strangvorbelegung
    {
        /// <summary>
        /// Die vier Zahlen, mit denen ein neuer Strang anfängt. Sie sind nicht
        /// <c>nullable</c>: Eine Vorbelegung, die „nicht angegeben" vorbelegt, wäre
        /// keine.
        /// </summary>
        public sealed class Vorschlag
        {
            /// <summary>Module in Reihe.</summary>
            public int ModuleReihe;

            /// <summary>Parallel geschaltete Stränge.</summary>
            public int StraengeParallel;

            /// <summary>MPP-Tracker des Geräts.</summary>
            public int Mppt;

            /// <summary>Welches physische Gerät dieses Typs.</summary>
            public int Geraetenummer;
        }

        /// <summary>
        /// Der kleinste sinnvolle Wert für „Module in Reihe". Ein Strang mit null
        /// Modulen ist kein Strang; sind alle Module der Anlage schon zugeordnet, ist
        /// EINES der ehrliche Vorschlag — und P8 meldet die Abweichung sofort.
        /// </summary>
        public const int MINDESTREIHE = 1;

        /// <summary>
        /// Womit der nächste Strang anfängt.
        /// </summary>
        /// <param name="anzahlModuleAnlage">
        /// „Anzahl Module" der Anlagenzeile; 0 oder kleiner = unbekannt.
        /// </param>
        /// <param name="bestehendeStraenge">Zahl der Strangzeilen VOR dem Anlegen.</param>
        /// <param name="belegteModule">
        /// Σ (Reihe × parallel) über die bestehenden Stränge — dieselbe Summe, die
        /// <see cref="StrangPlausibilitaet.Befund.Modulsumme"/> führt.
        /// </param>
        /// <param name="hoechsterMppt">
        /// Grösste bereits belegte Trackernummer AM SELBEN Gerät; 0 = keine.
        /// </param>
        /// <param name="trackerDesGeraets">
        /// <c>Anzahl_Mppt</c> des gewählten Geräts; <c>null</c> = unbekannt (die
        /// CEC-Liste führt die Angabe nicht, offener Punkt W6‑O‑2).
        /// </param>
        /// <remarks>
        /// <para><b>Der erste Strang ist die ganze Anlage</b> — das ist der Regelfall
        /// einer PV-Anlage mit einem Wechselrichter, und genau ihn erwartet der Planer,
        /// der gerade „Anzahl Module" eingetragen hat.</para>
        /// <para><b>Jeder weitere bekommt den REST</b> (<c>Anzahl − Summe</c>),
        /// mindestens aber <see cref="MINDESTREIHE"/>. Damit stimmt die Modulsumme nach
        /// dem Anlegen wieder mit der Anlage überein, und P8 bleibt still.</para>
        /// <para><b>Der Tracker wandert weiter, solange das Gerät welche hat.</b> Ist
        /// die Trackerzahl unbekannt, bleibt es bei 1 — demselben konservativen Fall,
        /// auf dem auch die Prüfung P4/P5 rechnet. Ein Vorschlag, der einen Tracker
        /// erfindet, den das Gerät vielleicht nicht hat, wäre schlechter als keiner.</para>
        /// </remarks>
        public static Vorschlag FuerNeuenStrang(double anzahlModuleAnlage,
                                                int bestehendeStraenge,
                                                int belegteModule,
                                                int hoechsterMppt,
                                                int? trackerDesGeraets)
        {
            int anlage = anzahlModuleAnlage > 0.0
                       ? (int)Math.Round(anzahlModuleAnlage, MidpointRounding.AwayFromZero)
                       : 0;

            int reihe;
            if (bestehendeStraenge <= 0)
                reihe = anlage > 0 ? anlage : MINDESTREIHE;
            else
                reihe = Math.Max(MINDESTREIHE, anlage - Math.Max(0, belegteModule));

            return new Vorschlag
            {
                ModuleReihe = reihe,
                StraengeParallel = 1,
                Mppt = NaechsterTracker(hoechsterMppt, trackerDesGeraets),
                Geraetenummer = 1
            };
        }

        /// <summary>
        /// Der nächste freie MPP-Tracker: eine Nummer weiter als die höchste belegte,
        /// gedeckelt auf das, was das Gerät führt. Ohne bekannte Trackerzahl EINS.
        /// </summary>
        public static int NaechsterTracker(int hoechsterMppt, int? trackerDesGeraets)
        {
            if (!trackerDesGeraets.HasValue || trackerDesGeraets.Value < 1) return 1;

            int naechster = Math.Max(0, hoechsterMppt) + 1;
            if (naechster < 1) naechster = 1;
            return Math.Min(trackerDesGeraets.Value, naechster);
        }
    }
}
