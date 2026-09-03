namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welches der drei Bedarfsblaetter gemeint ist (iU9-W8.0b).
    ///
    /// <para><b>Warum ein Aufzaehlungstyp und keine Zeichenkette.</b> Zehn WinForms-Masken
    /// werden in Welle 8 vier Razor-Komponenten; jede von ihnen bedient bis zu drei
    /// Auspraegungen desselben Blatts. Wo die Auspraegung ein Text waere, koennte eine
    /// Uebersetzung oder ein Tippfehler sie still ins Leere laufen lassen - Risiko R-W8-1
    /// des Arbeitsplans. Ein Aufzaehlungstyp kann das nicht.</para>
    ///
    /// <para><b>Er liegt im Kern, nicht in der Oberflaeche</b>, weil ihn BEIDE Seiten
    /// brauchen: <see cref="BedarfStammCtrl"/> und <see cref="TypProfilCtrl"/> verteilen
    /// danach auf drei Tabellen, die Razor-Komponenten waehlen danach ihre Beschriftungen.
    /// Ein zweiter Aufzaehlungstyp in EPOS.UI waere eine zweite Wahrheit und braeuchte eine
    /// Abbildung, die genau den Fehler machen kann, den der Typ verhindern soll. Dasselbe
    /// Vorgehen wie bei <c>DbWerte</c>, das die Tarifkomponenten seit iU9-W2.3 unmittelbar
    /// aus dem Kern nehmen.</para>
    /// </summary>
    public enum BedarfsArt
    {
        /// <summary>Stromverbraucher — <c>Tab_Stromverbraucher_STAMM</c> / <c>Tab_Stromverbrauchertyp_STAMM</c>
        /// (Schluesselspalte des Typkatalogs: <c>Typname</c>).</summary>
        Stromverbraucher,

        /// <summary>Prozesswaerme — <c>Tab_Prozesswaerme_STAMM</c> / <c>Tab_Prozesstyp_STAMM</c>
        /// (Schluesselspalte: <c>Bezeichner</c>).</summary>
        Prozesswaerme,

        /// <summary>Brauchwasser — <c>Tab_Brauchwasser_STAMM</c> / <c>Tab_Brauchwassertyp_STAMM</c>
        /// (Schluesselspalte: <c>Bezeichner</c>).</summary>
        Brauchwasser
    }
}
