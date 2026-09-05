using EPOS.UI.Bausteine;

namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — die DREI UEBERLAGERUNGEN.
/// </summary>
/// <remarks>
/// <para>
/// Werkzeugliste, Textanzeige (Aktionsprotokoll und Sendevorschau) und
/// Rechtshinweis. Im Vorlaeufer waren das drei ZWEITE FENSTER; in einer WebView
/// waere jedes davon eine zweite <c>BlazorWebView</c> ueber der ersten
/// (Risiko R2), und auf iOS gibt es zweite Fenster gar nicht.
/// </para>
/// <para>
/// <b>Sie melden sich an den Wirt</b> (<c>UeberlagerungGeaendert</c>), der den
/// Zustand an <c>KiAusfuehrer.Ueberlagerung</c> weiterreicht (W15b.0d,
/// Entscheid E-8): Solange eine offen steht, weist der Ausfuehrer Aktionen ab —
/// dieselbe Zusage, die im Bestand die Modalitaetspruefung trug.
/// </para>
/// </remarks>
public partial class KiChatDialog
{
    // =====================================================================
    //  Die drei Ueberlagerungen
    // =====================================================================

    private async Task BereichZeigen(Bereich bereich)
    {
        _bereich = bereich;
        await UeberlagerungGeaendert.InvokeAsync(true);
        StateHasChanged();
    }

    private async Task BereichSchliessen()
    {
        _bereich = Bereich.Keiner;
        await UeberlagerungGeaendert.InvokeAsync(false);
        StateHasChanged();
    }

    private Task WerkzeugeOeffnen() => BereichZeigen(Bereich.Werkzeuge);

    private async Task VorschauOeffnen()
    {
        if (Vorschau is null) return;
        _textTitel = Texte.VorschauTitel;
        _textKopf = Texte.VorschauKopf;
        _textInhalt = await Vorschau();
        await BereichZeigen(Bereich.Text);
    }

    private async Task ProtokollOeffnen()
    {
        if (Protokoll is null) return;
        _textTitel = Texte.ProtokollTitel;
        _textKopf = "";
        _textInhalt = await Protokoll();
        await BereichZeigen(Bereich.Text);
    }

    private async Task RechtshinweisOeffnen()
    {
        if (Rechtshinweisinhalt is not null)
        {
            await BereichZeigen(Bereich.Rechtshinweis);
            return;
        }
        if (Rechtshinweis is not null) await Rechtshinweis();
    }

    private Task DokuOeffnen()
        => string.IsNullOrEmpty(Texte.DokuAdresse)
            ? Task.CompletedTask
            : AdresseGewaehlt.InvokeAsync(Texte.DokuAdresse);

    private async Task EinstellungenOeffnen()
    {
        if (Einstellungen is null) return;
        if (!await Einstellungen()) return;

        Anhaengen(new[] { new Gespraechszeile(Gespraechsrolle.Erfolg, Texte.Gespeichert) });
        StatusSetzen();
        StateHasChanged();
    }

    /// <summary>
    /// Eine von Hand gewaehlte Aktion. <b>Erst schliessen, dann ausfuehren</b>
    /// (Bestand <c>:1295-1296</c>): Der Ausfuehrer weist Aktionen ab, solange eine
    /// Ueberlagerung offen ist.
    /// </summary>
    private async Task VonHandAusfuehren(
        (KiKern.KiAktion Aktion, IReadOnlyDictionary<string, object> Werte) wahl)
    {
        await BereichSchliessen();
        if (Ausfuehren is null) return;

        Anhaengen(new[]
        {
            new Gespraechszeile(Gespraechsrolle.Anwender, wahl.Aktion.Name)
        });

        _beschaeftigt = true;
        StateHasChanged();
        try
        {
            Anhaengen(await Ausfuehren(wahl.Aktion.Name, wahl.Werte));
        }
        finally
        {
            _beschaeftigt = false;
            StateHasChanged();
        }
    }
}
