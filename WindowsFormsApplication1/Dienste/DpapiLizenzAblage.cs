using System;
using System.IO;
using System.Security.Cryptography;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="ILizenzAblage"/>: DPAPI-verschlüsselte Dateien
    /// in <c>%APPDATA%\wp-plan</c>.
    ///
    /// <para><b>Der Geltungsbereich kommt vom Aufrufer und wird hier NIE geraten.</b>
    /// <c>nurDiesesGeraet = true</c> ergibt <see cref="DataProtectionScope.LocalMachine"/>
    /// (Lizenztoken, Zeitanker), <c>false</c> ergibt
    /// <see cref="DataProtectionScope.CurrentUser"/> (KI-Schlüssel). Wer eine bestehende
    /// Ablage auf den jeweils anderen Bereich umstellt, macht sie unlesbar: Eine mit
    /// <c>LocalMachine</c> verschlüsselte Datei lässt sich mit <c>CurrentUser</c> nicht
    /// entschlüsseln und umgekehrt. Bei der Lizenz heißt das: jede installierte Lizenz
    /// wäre entwertet.</para>
    ///
    /// <para><b>Nicht entschlüsselbar wird wie „nicht vorhanden" behandelt.</b> Das ist
    /// der Bestand: <c>LizenzManager.TokenLaden</c> und <c>KiChatService.SchluesselLesen</c>
    /// fangen jeden Fehler und liefern „kein Token" bzw. „kein Schlüssel". Ein anderes
    /// Windows-Konto oder eine beschädigte Datei darf den Start nicht verhindern.</para>
    /// </summary>
    public sealed class DpapiLizenzAblage : ILizenzAblage
    {
        /// <inheritdoc/>
        public byte[] Lesen(string name, bool nurDiesesGeraet)
        {
            try
            {
                string datei = Ablageort(name);
                if (!File.Exists(datei)) return null;

                return ProtectedData.Unprotect(File.ReadAllBytes(datei), null, Bereich(nurDiesesGeraet));
            }
            catch { return null; }
        }

        /// <inheritdoc/>
        public void Schreiben(string name, byte[] inhalt, bool nurDiesesGeraet)
        {
            if (inhalt == null) { Loeschen(name); return; }

            // Verzeichnis anlegen wie im Bestand (LizenzManager.Verzeichnis,
            // KiChatService.Verzeichnis) - beide legen den Ordner beim Bilden des
            // Pfades an. Ein Fehler beim Verschluesseln oder Schreiben wird bewusst
            // NICHT gefangen: Wer ein Geheimnis ablegen will, muss erfahren, wenn es
            // nicht geklappt hat. Die beiden Aufrufer fangen selbst, wo sie es wollen.
            string datei = Dienste.Pfade.Verbinde(Dienste.Pfade.Unterordner(Dienste.Pfade.Anwendungsdaten), name);
            File.WriteAllBytes(datei, ProtectedData.Protect(inhalt, null, Bereich(nurDiesesGeraet)));
        }

        /// <inheritdoc/>
        public void Loeschen(string name)
        {
            try
            {
                string datei = Ablageort(name);
                if (File.Exists(datei)) File.Delete(datei);
            }
            catch { }
        }

        /// <inheritdoc/>
        public bool Vorhanden(string name)
        {
            try { return File.Exists(Ablageort(name)); }
            catch { return false; }
        }

        /// <inheritdoc/>
        public string Ablageort(string name)
        {
            return Dienste.Pfade.Verbinde(Dienste.Pfade.Anwendungsdaten, name);
        }

        private static DataProtectionScope Bereich(bool nurDiesesGeraet)
        {
            return nurDiesesGeraet ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser;
        }
    }
}
