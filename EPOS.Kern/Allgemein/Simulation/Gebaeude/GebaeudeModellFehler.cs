using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die benannten Fehlgründe des Gebäudemodells nach VDI 6007 Blatt 1 (Stufe G0 der
    /// Gebäudesimulation, Umsetzungskonzept 1.3).
    ///
    /// <para><b>Warum benannt.</b> Kein stiller Rückfall: Ein Parameter, der das Netz
    /// unphysikalisch macht, wird nicht auf einen selbst gewählten Klemmwert gezogen,
    /// sondern bricht die Rechnung mit einem Grund ab, den der Aufrufer lesen und dem
    /// Anwender nennen kann. Die einzigen Setzwerte sind die, die die Richtlinie selbst
    /// vorschreibt (Gl. (28a)–(28c) der Außenbauteilgruppe); sie werden im Parametersatz
    /// ausgewiesen (<see cref="ErsatzparameterRC.Gruppenfall"/>). Jeder Grund steht hier
    /// genau einmal; die Tests prüfen gegen den Grund, nicht gegen den Meldungstext.</para>
    /// </summary>
    internal enum GebaeudeModellFehler
    {
        /// <summary>Eine Kapazität ist nicht endlich oder nicht größer null.</summary>
        KapazitaetUngueltig,

        /// <summary>Ein Widerstand des Netzes ist nicht endlich oder nicht größer null.</summary>
        WiderstandUngueltig,

        /// <summary>
        /// Die Außenbauteilgruppe hat keinen gültigen Fall nach Gl. (27)–(28c): Im Regelfall
        /// ist der Restwiderstand null oder negativ und der Grenzfall (28a) greift nicht oder
        /// ist mangels äußerem Übergangswiderstand nicht prüfbar; oder der Gesamtwiderstand
        /// eines Zweigs bzw. der Gruppe ist nicht positiv; oder R_Rest,AW ist nicht endlich.
        /// Rechnerisch bei sehr schlecht gedämmten Außenbauteilen (Rechenschritte A4, A7a).
        /// </summary>
        RRestAwNichtPositiv,

        /// <summary>
        /// Der Widerstand des Fensterzweigs ist null oder negativ (R_1,AF bzw. R_Rest,AF),
        /// nur einer der beiden ist gesetzt, oder die Fensterfläche ist null. Kein Klemmwert
        /// (Rechenschritte A7a).
        /// </summary>
        FensterzweigUngueltig,

        /// <summary>Eine Bezugsfläche ist negativ, nicht endlich oder die Summe null.</summary>
        FlaecheUngueltig,

        /// <summary>Ein Transmissionsleitwert (Σ U·A) ist negativ oder nicht endlich.</summary>
        LeitwertUngueltig,

        /// <summary>
        /// Ein Eigenwert der Systemmatrix ist nicht reell oder nicht negativ. Für ein
        /// passives RC-Netz ist das ausgeschlossen; tritt es auf, ist der Parametersatz
        /// widersprüchlich.
        /// </summary>
        EigenwerteNichtNegativ,

        /// <summary>Das Gleichungssystem der Oberflächen- und Luftknoten ist singulär.</summary>
        SystemSingulaer,

        /// <summary>Eine Randbedingung der Stunde ist nicht endlich oder widersprüchlich.</summary>
        RandUngueltig,

        /// <summary>
        /// Die Stunde ist nach der zulässigen Zahl von Abschnitten nicht zur Ruhe gekommen.
        /// Es entsteht kein Teilstundenergebnis (Rechenschritte 7.1).
        /// </summary>
        AbschnittsdeckelErreicht,

        /// <summary>
        /// Der Vorlauf hat nach der Höchstzahl an Durchläufen die Schwelle der Zustandsänderung
        /// nicht unterschritten (<see cref="Vorlauf2K"/>, Rechenschritte 7.2). Kein stiller
        /// Weiterlauf.
        /// </summary>
        VorlaufNichtKonvergiert,
    }

    /// <summary>
    /// Der Abbruch des Gebäudemodells mit einem benannten <see cref="GebaeudeModellFehler"/>.
    /// Der Meldungstext ist für das Protokoll gedacht; entschieden wird über
    /// <see cref="Grund"/>.
    /// </summary>
    internal sealed class GebaeudeModellException : Exception
    {
        internal GebaeudeModellException(GebaeudeModellFehler grund, string meldung)
            : base(meldung)
        {
            Grund = grund;
        }

        /// <summary>Der benannte Grund des Abbruchs.</summary>
        internal GebaeudeModellFehler Grund { get; }
    }
}
