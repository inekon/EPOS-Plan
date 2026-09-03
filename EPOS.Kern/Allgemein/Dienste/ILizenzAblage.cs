namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ablage für Geheimnisse: Lizenztoken, Zeitanker und KI-Schlüssel. Unter Windows
    /// DPAPI-verschlüsselte Dateien in <c>%APPDATA%\wp-plan</c>, unter iOS der
    /// Schlüsselbund. 10 Fundstellen in 2 Dateien (Vermessung iU5, Abschnitt A.4).
    ///
    /// <para><b>Der Geltungsbereich ist ein Aufrufparameter und darf sich NIE ändern.</b>
    /// Das Lizenztoken und sein Zeitanker liegen im GERÄTE-Bereich
    /// (<c>DataProtectionScope.LocalMachine</c>) — nur so gilt eine einmal aktivierte
    /// Lizenz für alle Windows-Konten desselben Rechners. Der KI-Schlüssel liegt im
    /// BENUTZER-Bereich (<c>CurrentUser</c>), weil er persönlich ist und nicht an
    /// Kollegen weitergereicht werden soll. Wer den Bereich einer bestehenden Ablage
    /// umstellt, macht jede installierte Lizenz bzw. jeden hinterlegten Schlüssel
    /// unlesbar — der Inhalt ist danach nicht mehr zu entschlüsseln.</para>
    /// </summary>
    public interface ILizenzAblage
    {
        /// <summary>
        /// Entschlüsselten Inhalt lesen; <c>null</c>, wenn nichts abgelegt ist oder der
        /// Inhalt nicht entschlüsselt werden kann (anderes Konto, beschädigte Datei).
        /// </summary>
        /// <param name="name">Ablagename, im Bestand ein Dateiname
        /// (<c>lizenz.dat</c>, <c>lizenz-zeit.dat</c>, <c>ki-schluessel.dat</c>).</param>
        /// <param name="nurDiesesGeraet">
        /// <c>true</c> = Gerätebereich (Lizenz), <c>false</c> = Benutzerbereich
        /// (KI-Schlüssel). Siehe den Klassenkommentar.
        /// </param>
        byte[] Lesen(string name, bool nurDiesesGeraet);

        /// <summary>Inhalt verschlüsselt ablegen.</summary>
        void Schreiben(string name, byte[] inhalt, bool nurDiesesGeraet);

        /// <summary>Ablage entfernen. Fehlt sie, geschieht nichts.</summary>
        void Loeschen(string name);

        /// <summary><c>true</c>, wenn unter diesem Namen etwas abgelegt ist.</summary>
        bool Vorhanden(string name);

        /// <summary>
        /// Der Ablageort in Klartext — nur für Protokolle und Anwendermeldungen
        /// (die Übernahmemeldung des KI-Schlüssels nennt ihn).
        /// </summary>
        string Ablageort(string name);
    }
}
