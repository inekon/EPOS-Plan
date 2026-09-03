using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IDialogDienst"/>: die Systemmeldung
/// (<c>UIAlertController</c>) ueber <c>Page.DisplayAlert</c>.
///
/// <para><b>Wofuer dieser Adapter da ist - und wofuer nicht.</b> Er bedient die
/// 47 Meldungen und 4 Rueckfragen des KERNS, also Stellen, die es seit Jahren
/// gibt und die auf jeder Plattform irgendwie erscheinen muessen. NEUE Dialoge
/// entstehen nach der Arbeitsregel M4 als Blazor-Ueberlagerung in EPOS.UI und
/// laufen nie hierher.</para>
///
/// <para><b>Die synchrone Rueckfrage ist die heikle Stelle</b> (iR-f).
/// <c>Frage</c> muss ein <c>bool</c> zurueckgeben, <c>DisplayAlert</c> liefert
/// ein <c>Task&lt;bool&gt;</c>. Vom HAUPTFADEN aus laesst sich darauf nicht
/// warten - das waere ein Selbstblock, und die Meldung erschiene nie. Kommt der
/// Aufruf von dort, wird deshalb NICHT gefragt, sondern die schadensaermere
/// Antwort gegeben: „nein" bzw. „Abbruch", genau wie in
/// <see cref="StilleDialoge"/>. Der Aufrufer sieht damit denselben Ausgang wie
/// ein Anwender, der ablehnt.</para>
///
/// <para><b>Ohne Fenster gibt es keine Meldung.</b> Vor dem ersten Zeichnen -
/// und im Pruefmodus der CI - ist keine Seite da; die Meldung geht dann auf die
/// Konsole. Das ist der Zustand, den <see cref="StilleDialoge"/> kennt, und der
/// Grund, warum ein Startfehler im Simulator-Protokoll sichtbar wird.</para>
/// </summary>
public sealed class IosDialogDienst : IDialogDienst
{
    private const string OK = "OK";
    private const string JA = "Ja";
    private const string NEIN = "Nein";
    private const string ABBRECHEN = "Abbrechen";

    /// <inheritdoc/>
    public void Meldung(string text, string? titel = null) => Zeige(titel ?? "", text);

    /// <inheritdoc/>
    public void Warnung(string text, string? titel = null) => Zeige(titel ?? "", text, "WARNUNG");

    /// <inheritdoc/>
    public void Fehler(string text, string? titel = null) => Zeige(titel ?? "", text, "FEHLER");

    /// <inheritdoc/>
    public bool Frage(string text, string? titel = null, bool warnend = false, bool vorgabeNein = false)
    {
        // warnend und vorgabeNein tragen unter Windows Symbol und Vorbelegung.
        // iOS kennt beides nicht: Eine Systemmeldung hat kein Symbol, und die
        // "zerstoererische" Rolle einer Schaltflaeche waere hier eine neue
        // Aussage. Beide Angaben bleiben deshalb ohne Wirkung - der Wortlaut
        // der Frage traegt die Warnung ohnehin.
        Microsoft.Maui.Controls.Page? seite = Seite();
        if (seite == null || MainThread.IsMainThread)
        {
            Protokoll("FRAGE - " + (titel ?? "") + ": " + text + " - ohne Bedienung: nein.");
            return false;
        }

        try
        {
            return MainThread.InvokeOnMainThreadAsync(
                () => seite.DisplayAlert(titel ?? "", text, JA, NEIN)).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public JaNeinAbbruch Wahl(string text, string? titel = null)
    {
        Microsoft.Maui.Controls.Page? seite = Seite();
        if (seite == null || MainThread.IsMainThread)
        {
            Protokoll("WAHL - " + (titel ?? "") + ": " + text + " - ohne Bedienung: Abbruch.");
            return JaNeinAbbruch.Abbruch;
        }

        try
        {
            // Eine Dreifachwahl ist auf iOS ein Aktionsblatt, keine Meldung.
            string? antwort = MainThread.InvokeOnMainThreadAsync(
                () => seite.DisplayActionSheet(titel ?? text, ABBRECHEN, null, JA, NEIN))
                .GetAwaiter().GetResult();

            if (antwort == JA) return JaNeinAbbruch.Ja;
            if (antwort == NEIN) return JaNeinAbbruch.Nein;
            return JaNeinAbbruch.Abbruch;
        }
        catch
        {
            return JaNeinAbbruch.Abbruch;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Die Wartekurve ist auf iOS die Aktivitaetsanzeige der Seite
    /// (<c>Page.IsBusy</c>). Ohne Seite bleibt sie folgenlos - wie in
    /// <see cref="StilleDialoge"/>.
    /// </remarks>
    public void Warten(bool an)
    {
        Microsoft.Maui.Controls.Page? seite = Seite();
        if (seite == null) return;

        try { MainThread.BeginInvokeOnMainThread(() => seite.IsBusy = an); } catch { }
    }

    // =====================================================================

    private static void Zeige(string titel, string text, string art = "")
    {
        Microsoft.Maui.Controls.Page? seite = Seite();
        if (seite == null)
        {
            Protokoll((art.Length > 0 ? art + " - " : "") + titel + ": " + text);
            return;
        }

        // Eine reine Meldung braucht keine Antwort und darf deshalb auch vom
        // Hauptfaden aus gezeigt werden - es wird nicht gewartet.
        try { MainThread.BeginInvokeOnMainThread(() => seite.DisplayAlert(titel, text, OK)); }
        catch { Protokoll(titel + ": " + text); }
    }

    /// <summary>
    /// Die gerade sichtbare Seite; <c>null</c>, solange keine gezeichnet wird.
    ///
    /// <para>Bewusst ueber die Fensterliste und NICHT ueber
    /// <c>Application.MainPage</c> - die Eigenschaft ist seit .NET 9
    /// abgekuendigt.</para>
    /// </summary>
    private static Microsoft.Maui.Controls.Page? Seite()
    {
        try
        {
            Application? anwendung = Application.Current;
            if (anwendung == null) return null;

            foreach (Window fenster in anwendung.Windows)
            {
                if (fenster.Page != null) return fenster.Page;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void Protokoll(string zeile)
    {
        try { Console.WriteLine(zeile); } catch { }
    }
}
