namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die reine LESEFRAGE an <c>Tab_Energieanlagen</c>: „gibt es im Projekt schon eine
    /// Zeile auf dieses Gerät?" — plus die Namen der vier gesperrten Geräteverweise.
    ///
    /// <para><b>Warum getrennt von <see cref="AnlagenEindeutigkeit"/> (Umsetzungskonzept
    /// iU3, Kante K4).</b> <c>PufferSpCtrl</c> braucht im Rechen- und Speicherpfad genau
    /// diese eine Abfrage. <see cref="AnlagenEindeutigkeit"/> bringt daneben den
    /// Dialogteil mit (Rückfragen, Meldungen, WinForms) und den
    /// DDL-Teil der Migration; beides gehört nicht in den Kern. Hier steht deshalb nur,
    /// was ohne Oberfläche auskommt: <see cref="StilleDb"/> und
    /// <see cref="SchemaKatalog"/>.</para>
    ///
    /// <para><see cref="AnlagenEindeutigkeit"/> behält seine öffentliche Fläche und leitet
    /// hierher weiter — bestehende Aufrufer bleiben unverändert gültig.</para>
    /// </summary>
    public static class Anlagenzeilen
    {
        /// <summary>Geräteverweis Wärmepumpe in <c>Tab_Energieanlagen</c>.</summary>
        public const string SPALTE_WP = "ID_WP";

        /// <summary>Geräteverweis Heizkessel in <c>Tab_Energieanlagen</c>.</summary>
        public const string SPALTE_KESSEL = "ID_Kessel";

        /// <summary>Geräteverweis BHKW in <c>Tab_Energieanlagen</c>.</summary>
        public const string SPALTE_BHKW = "ID_BHKW";

        /// <summary>Geräteverweis Pufferspeicher in <c>Tab_Energieanlagen</c>.</summary>
        public const string SPALTE_PUFFER = "ID_PUFFER";

        /// <summary>Gibt es im Projekt bereits eine Anlagenzeile auf dieses Gerät?</summary>
        public static bool ZeileVorhanden(string spalte, int idProjekt, int idGeraet)
        {
            if (idGeraet <= 0 || idProjekt <= 0) return false;

            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                "WHERE ID_Projekt = ? AND [" + spalte + "] = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@ger", DbParamTyp.Integer, idGeraet))) > 0;
        }
    }
}
