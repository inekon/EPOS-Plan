using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine im Projekt angelegte ERZEUGERANLAGE, so wie die Simulationskonfiguration sie
    /// braucht (iU9-W10b.0b).
    ///
    /// <para><b>Woher sie kommt.</b> Bis W10b war das eine PRIVATE Klasse in
    /// <c>Form_Simulation_Config.Uebersicht.cs</c>:80-123. Mit dem Port der Maske nach
    /// Blazor waere sie mit ihr verschwunden - dabei ist sie kein Anzeigebegriff, sondern
    /// die Zeile aus <c>Tab_Energieanlagen</c> samt ihrer Senkenkette. Sie steht deshalb
    /// hier, gefuellt von <see cref="WErzeugerCtrl.AnlagenMitWp"/>.</para>
    ///
    /// <para><b>Kein Anzeigetext.</b> Alle Felder sind Persistenzwerte;
    /// <c>WQ_Typ</c>, <c>BM_Typ</c> und <c>WpTyp</c> sind Steuerwerte im Sinne der
    /// Drei-Schichten-Regel und werden nie uebersetzt.</para>
    /// </summary>
    public sealed class AnlagenInfo
    {
        /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
        public int ID;

        /// <summary>1 WP, 2 Solarthermie, 10 Heizkessel, 11 BHKW.</summary>
        public int ID_Type;

        public string Bezeichner = "";

        /// <summary>Einsatzreihenfolge (0 = nicht gesetzt).</summary>
        public int Prioritaet;

        /// <summary>Bauart der Waermepumpe: Luft-Wasser / Sole-Wasser / Wasser-Wasser.</summary>
        public string WpTyp = "";

        /// <summary>Waermequelle (<c>WaermequelleClass.TYP_*</c>).</summary>
        public string WQ_Typ = "";

        public double WQ_Temp;

        /// <summary>Betriebsmodus (<c>WaermequelleClass.MODUS_*</c>).</summary>
        public string BM_Typ = "";

        /// <summary>
        /// Auslegungstemperaturen der ANLAGE (<c>Tab_Energieanlagen.Vorlauf</c> /
        /// <c>[Rücklauf]</c> - die Spalte traegt dort den Umlaut, siehe
        /// <c>ProjektPuffer.SQL_SYSTEM_RUECKLAUF</c>). 0 = nicht gepflegt.
        /// </summary>
        public int Vorlauf;

        public int Ruecklauf;

        /// <summary>
        /// Die SENKENLISTE der Anlage in Rangfolge (Konzept 5.1/5.3). Nie <c>null</c>,
        /// nie leer - ohne eigene Zeile steht hier die Rang-1-Vorbelegung
        /// <c>Heizkreis/Beides</c>, dieselbe, mit der die Engine rechnet.
        /// </summary>
        public List<Z_AnlageSenkeModel> Senken = new List<Z_AnlageSenkeModel>();

        /// <summary>Die Senkenzeile eines Rangs (0-basiert); <c>null</c>, wenn es sie nicht gibt.</summary>
        public Z_AnlageSenkeModel SenkeAufRang(int index)
        {
            return index >= 0 && index < Senken.Count ? Senken[index] : null;
        }

        public bool IstWaermepumpe
        {
            get { return ID_Type == ProjektPuffer.TYP_WP; }
        }
    }

    /// <summary>
    /// Nur ID und Bezeichner einer Anlage - die Kurzform, mit der die Speicherkarten
    /// ihre Zeile „Quelle fuer" bauen (<see cref="WErzeugerCtrl.Quellnutzer"/>).
    /// </summary>
    public sealed class AnlagenKurz
    {
        public int ID;
        public string Bezeichner = "";
    }
}
