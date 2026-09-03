using Foundation;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IPfade"/> - die Ablagewurzeln in der
/// App-Sandbox.
///
/// <para><b>Warum nicht <c>Environment.SpecialFolder</c>.</b> Die
/// Standardfassung <see cref="StandardPfade"/> nimmt es, und unter Windows,
/// Linux und macOS ist das richtig. Auf Apple-Mobile liefert die .NET-Laufzeit
/// dafuer aber XDG-artige Pfade (<c>~/.config</c>, <c>~/.local/share</c>)
/// innerhalb der Sandbox - Ordner, die die Systemdienste von iOS weder sichern
/// noch aufraeumen und die Apple in seinen Regeln nicht vorsieht. Gefragt wird
/// deshalb <c>NSSearchPath</c>, also dieselbe API, die eine native App nimmt.</para>
///
/// <para><b>Die Zuordnung.</b> iOS kennt keinen Maschinenbereich - eine App hat
/// genau eine Sandbox, und in der ist genau ein Anwender. Die beiden Windows-
/// Wurzeln „alle Benutzer" (<c>CommonApplicationData</c>) und „dieser Benutzer,
/// lokal" (<c>LocalApplicationData</c>) fallen deshalb auf denselben Ordner
/// zusammen.</para>
///
/// <list type="table">
///   <item><term><see cref="Anwendungsdaten"/></term>
///         <description><c>Library/Application Support/wp-plan</c> - Lizenz,
///         KI-Schluessel, Wiki- und Semantikablage. Kleingeschrieben wie im
///         Bestand: Der Lizenztoken haengt an dieser Zeichenkette.</description></item>
///   <item><term><see cref="Produktdaten"/></term>
///         <description><c>Library/Application Support/EPOS-Plan</c> - der
///         Hilfe-Zwischenspeicher. Unter Windows bildet ihn
///         <c>Application.ProductName</c>; hier steht der Name fest, weil es
///         kein WinForms gibt.</description></item>
///   <item><term><see cref="BenutzerLokal"/>, <see cref="Gemeinsam"/></term>
///         <description>beide <c>Library/Application Support/WP-Plan</c>.</description></item>
///   <item><term><see cref="Dokumente"/></term>
///         <description><c>Documents</c> der Sandbox. Mit
///         <c>UIFileSharingEnabled</c> in der Info.plist sieht der Anwender
///         diesen Ordner in der App „Dateien" - dort landen Berichte, CSV und
///         Datenbanksicherungen.</description></item>
/// </list>
///
/// <para><b>Was NICHT nach Application Support gehoert</b>, ist der
/// Zwischenspeicher: Alles unter <c>Library/Application Support</c> wird von
/// iCloud gesichert. Das ist fuer Lizenz und Einstellungen gewollt (der
/// Anwender bekommt sie auf ein Ersatzgeraet mit) und fuer die Datenbank
/// ebenfalls - eine gerechnete Variante ist Arbeit, keine Zwischenablage.</para>
/// </summary>
public sealed class IosPfade : StandardPfade
{
    /// <inheritdoc/>
    public override string Anwendungsdaten => Path.Combine(Unterstuetzung(), OrdnerKlein);

    /// <inheritdoc/>
    public override string Produktdaten => Path.Combine(Unterstuetzung(), "EPOS-Plan");

    /// <inheritdoc/>
    public override string BenutzerLokalBasis => Unterstuetzung();

    /// <inheritdoc/>
    public override string BenutzerLokal => Path.Combine(Unterstuetzung(), OrdnerGross);

    /// <inheritdoc/>
    public override string Gemeinsam => Path.Combine(Unterstuetzung(), OrdnerGross);

    /// <inheritdoc/>
    public override string Dokumente => Wurzel(NSSearchPathDirectory.DocumentDirectory);

    /// <summary><c>Library/Application Support</c> der Sandbox.</summary>
    private static string Unterstuetzung() => Wurzel(NSSearchPathDirectory.ApplicationSupportDirectory);

    /// <summary>
    /// Die erste Wurzel einer Suchordnung im Benutzerbereich; <c>""</c>, wenn es
    /// keine gibt. Der Ordner wird hier NICHT angelegt - das tut
    /// <see cref="StandardPfade.Unterordner"/>, wenn ein Aufrufer es will.
    /// (<c>Application Support</c> ist beim ersten Start tatsaechlich noch nicht
    /// da; iOS legt ihn nicht von sich aus an.)
    /// </summary>
    private static string Wurzel(NSSearchPathDirectory ordnung)
    {
        try
        {
            string[] treffer = NSSearchPath.GetDirectories(ordnung, NSSearchPathDomain.User);
            return treffer.Length > 0 ? treffer[0] : "";
        }
        catch
        {
            return "";
        }
    }
}
