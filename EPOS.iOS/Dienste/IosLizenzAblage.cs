using Microsoft.Maui.Storage;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="ILizenzAblage"/>: der Schluesselbund
/// (Keychain) ueber die MAUI-Fassade <see cref="SecureStorage"/>.
///
/// <para><b>Das Gegenstueck zu DPAPI.</b> Unter Windows liegen Lizenztoken,
/// Zeitanker und KI-Schluessel als DPAPI-verschluesselte Dateien in
/// <c>%APPDATA%\wp-plan</c>. Auf iOS uebernimmt der Schluesselbund dieselbe
/// Aufgabe: Er ist geraetegebunden, app-gebunden und wird vom Betriebssystem
/// verschluesselt. Eine eigene Verschluesselung waere ein Rueckschritt.</para>
///
/// <para><b>Der Geltungsbereich wandert in den NAMEN.</b> Die Schnittstelle
/// fordert zwei Bereiche - Geraet (Lizenz) und Benutzer (KI-Schluessel) -, die
/// unter Windows verschiedene DPAPI-Schluessel bedeuten. Auf iOS gibt es diese
/// Unterscheidung nicht: Eine Sandbox hat genau einen Anwender. Damit
/// <c>Lesen(name, true)</c> und <c>Lesen(name, false)</c> trotzdem NICHT
/// dieselbe Ablage treffen - und ein Aufrufer, der den Bereich verwechselt,
/// dasselbe merkt wie unter Windows -, traegt der Schluesselbundeintrag ein
/// Suffix.</para>
///
/// <para><b>Folge fuer <see cref="Vorhanden"/> und <see cref="Loeschen"/>:</b>
/// Beide kennen den Bereich nicht (die Schnittstelle uebergibt ihn dort nicht)
/// und muessen deshalb BEIDE Eintraege betrachten. Das ist unter Windows
/// nicht anders - dort gibt es je Name genau eine Datei, und der Bereich
/// entscheidet nur, ob sie sich entschluesseln laesst.</para>
///
/// <para><b>Synchron, weil die Schnittstelle es ist.</b> Der gesamte
/// Lizenzweg des Kerns (<c>LizenzManager</c>, <c>KiChatService</c>) ist
/// synchron. Ein <c>GetAwaiter().GetResult()</c> auf den Schluesselbund ist
/// dabei unbedenklich: Die Aufrufe kommen nicht vom Zeichenfaden, und der
/// Schluesselbund hat keine Fortsetzung auf dem Hauptfaden.</para>
/// </summary>
public sealed class IosLizenzAblage : ILizenzAblage
{
    /// <inheritdoc/>
    public byte[]? Lesen(string name, bool nurDiesesGeraet)
    {
        try
        {
            string? text = SecureStorage.Default.GetAsync(Eintrag(name, nurDiesesGeraet))
                                                .GetAwaiter().GetResult();
            return string.IsNullOrEmpty(text) ? null : Convert.FromBase64String(text);
        }
        catch
        {
            // Wie unter Windows: nicht lesbar wird wie "nicht vorhanden"
            // behandelt. Ein beschaedigter Eintrag darf den Start nicht
            // verhindern.
            return null;
        }
    }

    /// <inheritdoc/>
    public void Schreiben(string name, byte[] inhalt, bool nurDiesesGeraet)
    {
        if (inhalt == null) { Loeschen(name); return; }

        // Ein Fehler wird hier - wie in DpapiLizenzAblage - BEWUSST nicht
        // gefangen: Wer ein Geheimnis ablegen will, muss erfahren, wenn es
        // nicht geklappt hat.
        SecureStorage.Default.SetAsync(Eintrag(name, nurDiesesGeraet), Convert.ToBase64String(inhalt))
                             .GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void Loeschen(string name)
    {
        foreach (bool geraet in new[] { true, false })
        {
            try { SecureStorage.Default.Remove(Eintrag(name, geraet)); } catch { }
        }
    }

    /// <inheritdoc/>
    public bool Vorhanden(string name)
    {
        foreach (bool geraet in new[] { true, false })
        {
            try
            {
                string? text = SecureStorage.Default.GetAsync(Eintrag(name, geraet))
                                                    .GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(text)) return true;
            }
            catch { }
        }
        return false;
    }

    /// <inheritdoc/>
    /// <remarks>Nur fuer Protokolle und Anwendermeldungen - es gibt keinen Pfad.</remarks>
    public string Ablageort(string name) => "Schlüsselbund: " + (name ?? "");

    private static string Eintrag(string name, bool nurDiesesGeraet)
        => "wp-plan." + (name ?? "") + (nurDiesesGeraet ? ".geraet" : ".benutzer");
}
