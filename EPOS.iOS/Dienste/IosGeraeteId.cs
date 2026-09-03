using UIKit;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IGeraeteId"/>:
/// <c>identifierForVendor</c> und Geraetemodell.
///
/// <para><b>Was <see cref="Kennung"/> liefern muss</b>, sind die ROHEN
/// Merkmale, aus denen <c>GeraeteId.Ermitteln</c> den SHA-256-Abdruck bildet.
/// Unter Windows ist das <c>&lt;MachineGuid&gt;|&lt;Laufwerk&gt;|&lt;Groesse&gt;</c>;
/// hier ist es <c>&lt;identifierForVendor&gt;|&lt;Modell&gt;</c>.</para>
///
/// <para><b>Ein iPad ist damit ein NEUES Geraet am Lizenzserver</b> - der
/// Abdruck ist ein anderer als der des Windows-Rechners desselben Anwenders.
/// Das ist gewollt: Es IST ein anderes Geraet. Was das fuer die Zahl der
/// gebundenen Geraete je Lizenz bedeutet, ist eine kaufmaennische Frage
/// (iF12), keine technische.</para>
///
/// <para><b>Was <c>identifierForVendor</c> ueberlebt und was nicht.</b> Er
/// bleibt ueber Aktualisierungen und Neustarts stabil, solange irgendeine App
/// desselben Anbieters installiert ist. Werden ALLE Apps des Anbieters
/// entfernt und eine davon neu installiert, entsteht ein neuer Wert - dann
/// gilt das iPad als neues Geraet und die Lizenz muss neu aktiviert werden.
/// Eine stabilere Kennung gibt es auf iOS nicht: Die Seriennummer ist seit
/// iOS 7 nicht mehr lesbar, und ein selbst erzeugter Wert im Schluesselbund
/// waere kein GERAETE-Merkmal, sondern ein weiteres Geheimnis.</para>
/// </summary>
public sealed class IosGeraeteId : IGeraeteId
{
    /// <inheritdoc/>
    public string Kennung
    {
        get
        {
            try
            {
                UIDevice geraet = UIDevice.CurrentDevice;
                string anbieter = geraet.IdentifierForVendor?.AsString() ?? "";
                return anbieter + "|" + (geraet.Model ?? "");
            }
            catch
            {
                // Leer heisst "kein Merkmal ermittelbar" - derselbe Zustand wie
                // KeineGeraeteId. Der Abdruck ist dann offensichtlich
                // unbrauchbar und wird vom Lizenzserver nicht gebunden.
                return "";
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Der Anzeigename ist der vom Anwender vergebene Geraetename („iPad von
    /// Anna"). Seit iOS 16 liefert er ohne besondere Berechtigung den
    /// Modellnamen statt des selbst vergebenen - das ist fuer eine Anzeige
    /// unschaedlich; der Abdruck haengt nicht daran.
    /// </remarks>
    public string Anzeige
    {
        get
        {
            try { return UIDevice.CurrentDevice.Name ?? ""; }
            catch { return ""; }
        }
    }
}
