namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Strang einer PV-Anlage — eine Zeile aus <c>Z_AnlageStrang</c> (Stufe S2,
    /// Migrationsschritt 66, <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 3.4,
    /// Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026).
    ///
    /// <para>
    /// <b>Was die Klasse möglich macht.</b> Bis hierher galten die
    /// Wechselrichterdaten für die GANZE Anlage: fünf Zahlen an der Anlagenzeile
    /// (Migrationsschritt 64). Eine Zeile je Strang macht daraus eine Liste beliebiger
    /// Länge — mehrere Geräte je Anlage (<see cref="Geraetenummer"/>), mehrere
    /// MPP-Tracker je Gerät (<see cref="Mppt"/>) und, das ist der Ost/West-Fall,
    /// eine eigene Ausrichtung je Teilfeld (<see cref="Neigung"/>,
    /// <see cref="Azimut"/>).
    /// </para>
    ///
    /// <para>
    /// <b>Öffentliche Felder statt Eigenschaften</b> — Hausmuster aller
    /// <c>*Model</c>-Klassen dieses Projekts (<c>Z_AnlageSenkeModel</c>,
    /// <c>PufferSpModel</c>, <c>WErzeugerModel</c>). Die Modelle sind reine
    /// Datenträger zwischen Controller und Oberfläche, ohne Logik und ohne Bindung.
    /// </para>
    ///
    /// <para>
    /// <b>Warum durchgehend <c>int?</c> und nicht <c>int</c>.</b> Anders als beim
    /// Senkenmodell trägt NULL hier fast überall eine eigene Aussage (Konzept 3.4,
    /// Spalte „NULL bedeutet"): kein Gerät zugeordnet, Gerät 1, MPPT 1, ein Strang
    /// parallel — und bei <see cref="Neigung"/>/<see cref="Azimut"/> „der Anlagenwert".
    /// Gerade dort wäre eine 0 falsch: <b>Azimut 0 ist eine GÜLTIGE Ausrichtung</b>
    /// (Süden). Wer NULL und 0 gleichsetzt, macht aus einem geerbten Wert
    /// stillschweigend einen gepflegten — dieselbe Überlegung, die
    /// <c>Z_AnlageSenkeModel.Anschlusshoehe</c> mit ihrem −1 löst und die
    /// <c>WechselrichterModel</c> zur durchgehenden <c>double?</c>-Form geführt hat.
    /// </para>
    ///
    /// <para>
    /// <b>Die Vorbelegung ist LEER, und das ist die Rückfallregel.</b> Ein frisch
    /// angelegtes Modell beschreibt einen Strang ohne Gerät, an Gerät 1 und MPPT 1,
    /// mit der Ausrichtung der Anlage — also genau das, was die Anlage ohne
    /// Strangzuordnung tut. Wer eine Zeile anlegt und nichts weiter setzt, bekommt
    /// damit kein Sonderverhalten.
    /// </para>
    /// </summary>
    public class AnlageStrangModel
    {
        /// <summary>Vorgabe für <see cref="Geraetenummer"/>, <see cref="Mppt"/> und
        /// <see cref="Straenge_Parallel"/>, wenn die Spalte NULL ist (Konzept 3.4).</summary>
        public const int VORGABE_EINS = 1;

        /// <summary>Primärschlüssel (AutoWert); 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>
        /// FK auf <c>Tab_Energieanlagen.ID</c> — die Anlage, deren Strang das ist.
        /// <b>Mit Löschweitergabe</b>: Fällt die Anlagenzeile, fällt diese Zeile mit
        /// (Kopf von <see cref="AnlageStrangSchema"/>).
        /// </summary>
        public int ID_Anlage;

        /// <summary>
        /// Reihenfolge innerhalb der Stränge DIESER Anlage, 1..n. Sie wird beim
        /// Schreiben lückenlos neu vergeben — was der Dialog mitgibt, zählt als
        /// Reihenfolge, nicht als Wert.
        /// </summary>
        public int Rang;

        /// <summary>
        /// Freitext („Dach Süd", „Ostseite"); leer = der Rang als Anzeige.
        /// </summary>
        public string Bezeichner = "";

        /// <summary>
        /// FK auf die PROJEKTKOPIE <c>Tab_Wechselrichter.ID</c>.
        /// <b><c>null</c> = kein Gerät zugeordnet</b> — der Strang steht dann in der
        /// Tabelle, rechnet aber (ab S3) nicht mit, und die Ampel meldet es.
        /// </summary>
        public int? ID_Wechselrichter;

        /// <summary>
        /// Welches PHYSISCHE Gerät dieses Typs (1…n); <c>null</c> =
        /// <see cref="VORGABE_EINS"/>.
        /// <para>Das Gruppierungsmerkmal des Clippings: Es rechnet je
        /// (Anlage, Wechselrichter, Gerätenummer), und die Gerätezahl für die Kosten
        /// ist <c>COUNT(DISTINCT …)</c> (Konzept 3.4, Q6).</para>
        /// </summary>
        public int? Geraetenummer;

        /// <summary>MPPT-Eingang dieses Geräts (1…n); <c>null</c> = <see cref="VORGABE_EINS"/>.</summary>
        public int? Mppt;

        /// <summary>
        /// Module in Reihe — die Größe, an der die Spannungsprüfungen P1 bis P3
        /// hängen. <c>null</c> heisst „noch nicht angegeben": Der Strang zählt dann
        /// keine Module, und die Prüfungen entfallen als „nicht prüfbar".
        /// </summary>
        public int? Module_Reihe;

        /// <summary>
        /// Parallel geschaltete Stränge; <c>null</c> = <see cref="VORGABE_EINS"/>.
        /// An dieser Zahl hängt die Stromprüfung P4.
        /// </summary>
        public int? Straenge_Parallel;

        /// <summary>
        /// Neigung dieses Teilfelds [°]; <b><c>null</c> = der Anlagenwert</b>
        /// (Konzept 3.4, Entwurfsentscheidung 2). Ohne Eintrag rechnet der Strang mit
        /// der Anlagenausrichtung, also exakt wie heute.
        /// </summary>
        public int? Neigung;

        /// <summary>
        /// Azimut dieses Teilfelds [°]; <b><c>null</c> = der Anlagenwert</b>.
        /// <b>0 ist ein gültiger Wert</b> (Süden) und darf nie als „leer" gelesen
        /// werden.
        /// </summary>
        public int? Azimut;

        /// <summary>
        /// Abweichender Modultyp (→ <c>Tab_PV.ID</c>); <c>null</c> = das Modul der
        /// Anlage.
        /// <para>In Stufe S2 zeigt die Oberfläche das Feld NICHT (Konzept 3.4,
        /// Entwurfsentscheidung 3); der Controller trägt es trotzdem hin und zurück,
        /// damit ein von Hand gesetzter Wert beim Speichern nicht verloren geht.</para>
        /// </summary>
        public int? ID_PV;

        /// <summary>Die Gerätenummer mit ihrem Vorgabewert — nie <c>null</c>.</summary>
        public int GeraetenummerOderEins => Geraetenummer ?? VORGABE_EINS;

        /// <summary>Der MPPT-Eingang mit seinem Vorgabewert — nie <c>null</c>.</summary>
        public int MpptOderEins => Mppt ?? VORGABE_EINS;

        /// <summary>Die Parallelzahl mit ihrem Vorgabewert — nie <c>null</c>.</summary>
        public int ParallelOderEins => Straenge_Parallel ?? VORGABE_EINS;

        /// <summary>
        /// Die Modulzahl dieses Strangs: <c>Module_Reihe × Straenge_Parallel</c>; 0,
        /// solange die Reihe nicht angegeben ist.
        ///
        /// <para>Aus der Summe dieser Zahl über alle Stränge entsteht die abgeleitete
        /// „Anzahl Module" der Anlage (Entscheidungsfrage <b>Q9</b>) und die
        /// Prüfung P8.</para>
        /// </summary>
        public int Modulzahl => (Module_Reihe ?? 0) <= 0 ? 0 : Module_Reihe.Value * ParallelOderEins;
    }
}
