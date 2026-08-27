using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Senke einer Wärmeerzeuger-Anlage — eine Zeile aus
    /// <c>Z_AnlageSenke</c> (Paket S1, Migrationsschritt 50, Konzept
    /// Brauchwasser/Heizung/Pufferspeicher § 5.1).
    ///
    /// <para>
    /// <b>Was die Klasse ablöst.</b> Bis hierher hatte jede Anlage genau ZWEI
    /// Senkenplätze, als zwei Spaltensätze in <c>Tab_Energieanlagen</c>
    /// (<c>WS_Ziel</c>/<c>WS_ID_Puffer</c>/… und <c>WS_Ziel2</c>/<c>WS_ID_Puffer2</c>/…).
    /// Ein Modell je Zeile macht daraus eine Liste beliebiger Länge, die sich
    /// umsortieren lässt (<see cref="Rang"/>) und in der jede Senke ihre eigenen
    /// Ladeparameter führt.
    /// </para>
    ///
    /// <para>
    /// <b>Öffentliche Felder statt Eigenschaften</b> — Hausmuster aller
    /// <c>*Model</c>-Klassen dieses Projekts (<c>Z_ProjektPufferSpModel</c>,
    /// <c>WErzeugerModel</c>). Die Modelle sind reine Datenträger zwischen
    /// Controller und Oberfläche, ohne Logik und ohne Bindung.
    /// </para>
    ///
    /// <para>
    /// <b>Die Vorbelegung IST die Rückfallregel.</b> Ein frisch angelegtes Modell
    /// beschreibt <c>Heizkreis</c>/<c>Beides</c> — genau das, was die Engine rechnet,
    /// wenn sie zu einer Anlage gar keine Senkenzeile findet
    /// (<c>WaermesenkeClass.Normalisieren</c>). Wer eine Zeile anlegt und nichts
    /// weiter setzt, bekommt damit kein Sonderverhalten, sondern das Bestandsverhalten.
    /// </para>
    /// </summary>
    public class Z_AnlageSenkeModel
    {
        /// <summary>Primärschlüssel (AutoWert); 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>FK auf <c>Tab_Energieanlagen.ID</c> — die Anlage, deren Senke das ist.</summary>
        public int ID_Anlage;

        /// <summary>
        /// Reihenfolge innerhalb der Senken DIESER Anlage, 1..n. Rang 1 ist Pflicht:
        /// Der Dialog verweigert das Entfernen der letzten Zeile (§ 5.1).
        /// </summary>
        public int Rang;

        /// <summary>
        /// Das Ziel — ausschließlich einer der sechs <c>DbWerte.WS_ZIEL_*</c>-Werte.
        /// Zwei davon sind Direktsenken (<c>Heizkreis</c>, <c>Prozesswaerme</c>), vier
        /// sind Ladeziele auf einen Puffer.
        /// </summary>
        public string Ziel = DbWerte.WS_ZIEL_HEIZKREIS;

        /// <summary>
        /// Der abgedeckte Bedarfsanteil — <b>nur bei <c>Ziel = Heizkreis</c>
        /// wirksam</b>, Werte <c>DbWerte.WS_TYP_BEIDES</c>/<c>_WARMWASSER</c>/<c>_HEIZUNG</c>.
        /// Bei jedem anderen Ziel ist das Feld bedeutungslos und trägt die neutrale
        /// Vorbelegung; der Prozesskanal hat mit <c>WS_ZIEL_PROZESS</c> ein eigenes
        /// Ziel und braucht deshalb keinen vierten Bedarfsart-Wert (§ 4.4).
        /// </summary>
        public string Bedarfsart = DbWerte.WS_TYP_BEIDES;

        /// <summary>
        /// FK auf <c>Tab_Pufferspeicher.ID</c> — nur bei den vier Puffer-Zielen belegt.
        /// <b>0 = keiner</b>; in der Datenbank steht dann NULL, denn 0 verletzte die
        /// restriktive Beziehung (Hausregel wie bei <c>WS_ID_Puffer</c>).
        /// </summary>
        public int ID_Puffer;

        /// <summary>Ladepriorität; 0 = Vorgabe nach Erzeugertyp (Ladeordnung).</summary>
        public int Ladeprio;

        /// <summary>
        /// Sonderpriorität bei PV-Überschuss; 0 = keine. Die Migration vergibt sie nur
        /// an Rang 1 — eine Spalte <c>WS_Ladeprio_PV2</c> gab es nie, die Sonderregel
        /// hing konstruktiv an der Hauptsenke.
        /// </summary>
        public int Ladeprio_PV;

        /// <summary>
        /// Eigene Ladeobergrenze <b>in PROZENT</b> — dieselbe Einheit wie
        /// <c>WS_Ladegrenze</c>, <c>Schwelle_Aus</c> und <c>Schwelle_Aus_Nachrang</c>;
        /// die Umrechnung /100 bleibt beim Bau des Ladeauftrags. 0 = nicht gesetzt,
        /// dann gilt die Regel des Puffers.
        /// </summary>
        public double Ladegrenze;

        /// <summary>
        /// Einspeisehöhe 0..1 am Schichtspeicher; <b>-1 = nicht gesetzt</b> (in der
        /// Datenbank NULL), dann speist die Senke wie bisher oben ein.
        /// VORGRIFF auf Paket P1: Schritt 50 legt nur die Spalte an, gelesen wird sie
        /// erst mit dem Schichtmodell (§ 7.4). -1 statt 0 als Leerwert, weil 0 eine
        /// GÜLTIGE Höhe ist (ganz unten).
        /// </summary>
        public double Anschlusshoehe = -1;
    }
}
