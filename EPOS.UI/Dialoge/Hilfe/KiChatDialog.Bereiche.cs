using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;

namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — die VIER UEBERLAGERUNGEN.
/// </summary>
/// <remarks>
/// <para>
/// Werkzeugliste, Textanzeige (Aktionsprotokoll und Sendevorschau), Rechtshinweis
/// und — seit dem Befund <b>W15b‑B‑1</b> der Windows-Abnahme vom 05.09.2026 — die
/// EINSTELLUNGEN. Im Vorlaeufer waren das vier ZWEITE FENSTER; in einer WebView
/// waere jedes davon eine zweite <c>BlazorWebView</c> ueber der ersten
/// (Risiko R2), und auf iOS gibt es zweite Fenster gar nicht.
/// </para>
/// <para>
/// <b>Der Befund W15b‑B‑1.</b> „Einstellungen…" öffnete ein leeres Fenster, dann
/// stürzte die Anwendung ab. Die Hülle gab
/// <c>Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster))</c> heraus
/// (<c>KiChatHuelle.Gaben.cs:208</c>) — ein zweites modales Fenster mit einer
/// zweiten WebView2, aufgezogen SYNCHRON im <c>WebMessageReceived</c>-Rückruf der
/// ersten. Genau das Muster von <b>W16b‑B‑1</b> (leere Startkachel-Dialoge) und
/// <b>W13‑B‑1</b> (Dateiwähler). Der Weg des Hauses dagegen ist Entscheid
/// <b>E‑5</b>: Überlagerung statt zweitem Fenster.
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
    //  Die vier Ueberlagerungen
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

    // Die zwei Rueckwege werden GEMERKT und nicht bei jedem Zeichnen neu gebaut:
    // Ein neuer Rueckruf gilt Blazor als geaenderter Parameter, und der eingebettete
    // Dialog setzte dann mitten in der Eingabe seine Anfangswerte erneut.
    private EventCallback<bool>? _bereichFertig;
    private EventCallback<bool>? _einstellungenFertig;

    /// <summary>
    /// Der Rueckweg, den ein eingebetteter Dialog bekommt: „ich bin fertig".
    /// </summary>
    /// <remarks>
    /// Ohne ihn taete der Schliessknopf des eingebetteten Dialogs nichts — er meldet
    /// sein Ergebnis an einen <c>EventCallback</c>, und den kann nur der Wirt
    /// aufloesen. Das Ergebnis selbst interessiert hier nicht; der Rechtshinweis
    /// wird nur gelesen.
    /// </remarks>
    private EventCallback<bool> BereichFertig
        => _bereichFertig ??= EventCallback.Factory.Create<bool>(this, _ => BereichSchliessen());

    /// <summary>
    /// Derselbe Rueckweg fuer die EINSTELLUNGEN — hier zaehlt das Ergebnis:
    /// <c>true</c> = gespeichert.
    /// </summary>
    private EventCallback<bool> EinstellungenFertig
        => _einstellungenFertig ??=
               EventCallback.Factory.Create<bool>(this, EinstellungenAbgeschlossen);

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

    /// <summary>
    /// „Einstellungen…" — <b>als Ueberlagerung</b>, wenn die Huelle einen Inhalt
    /// mitgibt (Befund W15b‑B‑1); sonst auf dem alten Weg ueber den Delegaten.
    /// </summary>
    private async Task EinstellungenOeffnen()
    {
        if (Einstellungsinhalt is not null)
        {
            await BereichZeigen(Bereich.Einstellungen);
            return;
        }

        if (Einstellungen is null) return;
        if (!await Einstellungen()) return;

        EinstellungenVermerken();
        StateHasChanged();
    }

    /// <summary>
    /// Die eingebetteten Einstellungen sind zu: schliessen und, wenn gespeichert
    /// wurde, dieselbe Zeile in den Verlauf schreiben wie auf dem Delegatenweg.
    /// </summary>
    private async Task EinstellungenAbgeschlossen(bool gespeichert)
    {
        await BereichSchliessen();
        if (!gespeichert) return;

        EinstellungenVermerken();
        StateHasChanged();
    }

    private void EinstellungenVermerken()
    {
        Anhaengen(new[] { new Gespraechszeile(Gespraechsrolle.Erfolg, Texte.Gespeichert) });
        StatusSetzen();
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

        // W15b‑E‑4: im Verlauf steht der TITEL, nicht der Bezeichner. Der Bezeichner
        // geht weiterhin an den Ausfuehrer — er ist der Schluessel des Registers.
        Anhaengen(new[]
        {
            new Gespraechszeile(Gespraechsrolle.Anwender, wahl.Aktion.Titel)
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
