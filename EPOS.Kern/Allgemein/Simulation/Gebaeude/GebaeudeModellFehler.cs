using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die benannten Fehlgründe des Gebäudemodells nach VDI 6007 Blatt 1 (Stufe G0 der
    /// Gebäudesimulation, Umsetzungskonzept 1.3).
    ///
    /// <para><b>Warum benannt.</b> Blatt 1, 6.8 verbietet den stillen Rückfall: Ein
    /// Parameter, der das Netz unphysikalisch macht, wird nicht auf einen Klemmwert
    /// gezogen, sondern bricht die Rechnung mit einem Grund ab, den der Aufrufer lesen und
    /// dem Anwender nennen kann. Jeder Grund steht hier genau einmal; die Tests prüfen
    /// gegen den Grund, nicht gegen den Meldungstext.</para>
    /// </summary>
    internal enum GebaeudeModellFehler
    {
        /// <summary>Eine Kapazität ist nicht endlich oder nicht größer null.</summary>
        KapazitaetUngueltig,

        /// <summary>Ein Widerstand des Netzes ist nicht endlich oder nicht größer null.</summary>
        WiderstandUngueltig,

        /// <summary>
        /// Der Restwiderstand der Außenwände R_Rest,AW ist null oder negativ — rechnerisch
        /// bei sehr schlecht gedämmten Außenbauteilen; ebenso der Restwiderstand der aus
        /// Wänden und Fenstern zusammengefassten Gruppe. Kein Klemmwert (Rechenschritte A4).
        /// </summary>
        RRestAwNichtPositiv,

        /// <summary>
        /// Der Widerstand des Fensterzweigs ist null oder negativ (R_1,AF bzw. R_Rest,AF),
        /// oder nur einer der beiden ist gesetzt. Kein Klemmwert (Rechenschritte A7a).
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
