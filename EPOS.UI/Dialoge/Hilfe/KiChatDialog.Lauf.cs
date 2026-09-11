using System.Diagnostics;
using System.Globalization;
using EPOS.UI.Bausteine;
using KiKern;
using Microsoft.AspNetCore.Components;

namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — DER LAUFENDE RECHENVORGANG (Auftrag #214).
/// </summary>
/// <remarks>
/// <para>
/// <b>Der Restpunkt aus #201.</b> Der Kern trug Fortschritt und Abbruchmarke seit
/// Etappe S3 bis in die drei Rechenaktionen hinein
/// (<c>KiLaufumgebung</c>, <c>KiAusfuehrung.Fortschritt</c>) — nur hörte niemand zu:
/// Die Windows-Hülle reichte <c>CancellationToken.None</c> herein und belegte die
/// Senke nicht. Ein Simulationslauf über den Assistenten lief deshalb minutenlang
/// ohne Rückmeldung und war nicht abbrechbar.
/// </para>
/// <para>
/// <b>Warum das hier steht und nicht in der Hülle.</b> Der Baustein
/// <c>Fortschritt</c> gehört dieser Bibliothek, der Zustand dazu gehört neben ihn.
/// Läge er in <c>KiChatHuelle</c>, müsste ihn die iOS-Hülle ein zweites Mal bauen —
/// und die zwei Fassungen liefen beim ersten Umbau auseinander. Die Hülle reicht nur
/// noch durch, was sie ohnehin hat: den Fadenwechsel und die zwei Enden, die sie am
/// Kern anhängt (<see cref="KiChatSteuerung.Fortschritt"/>,
/// <see cref="KiChatSteuerung.Abbruchmarke"/>).
/// </para>
/// <para>
/// <b>Eine Anforderung zur Zeit.</b> Die Einläufigkeit ist und bleibt die des
/// Ausführers (Fachkonzept 3.4, Pflicht 1); dieser Dialog spiegelt sie nur — er
/// sperrt Eingabefeld, Senden und die Aktionsknöpfe, solange ein Lauf steht, und
/// hält genau EINE <see cref="CancellationTokenSource"/>.
/// </para>
/// <para>
/// <b>Der Balken erscheint erst, wenn wirklich gerechnet wird.</b> Eine gewöhnliche
/// Frage beantwortet der Verlauf mit „Der Assistent denkt nach…"; ein zweiter
/// Balken daneben sagte nichts dazu. Meldet der Kern dagegen einen Schritt, ist eine
/// lange Aktion im Gang — dann steht der Balken, und mit ihm der Abbruchknopf.
/// </para>
/// </remarks>
public partial class KiChatDialog
{
    // =====================================================================
    //  Zustand
    // =====================================================================

    /// <summary>Die Quelle der Abbruchmarke; <c>null</c> = es läuft nichts.</summary>
    private CancellationTokenSource? _lauf;

    /// <summary>Die Uhr des laufenden Vorgangs — sie liefert die Dauer im Verlauf.</summary>
    private Stopwatch? _laufUhr;

    /// <summary>Wie der Vorgang im Verlauf heißen soll; leer = noch unbenannt.</summary>
    private string _laufName = "";

    /// <summary>Der Anteil 0…1; <c>null</c> = unbestimmt (der Balken läuft).</summary>
    private double? _laufAnteil;

    /// <summary>Die Zeile unter dem Balken.</summary>
    private string _laufText = "";

    /// <summary>Steht der Balken? Er kommt mit dem ersten gemeldeten Schritt.</summary>
    private bool _laufZeigen;

    /// <summary>Hat der Anwender abgebrochen?</summary>
    private bool _laufAbgebrochen;

    private EventCallback? _laufAbbruchWeg;
    private IProgress<KiFortschritt>? _laufSenke;

    /// <summary>Läuft gerade eine Anforderung? (Prüfhilfe für Tests.)</summary>
    public bool Laeuft => _lauf is not null;

    /// <summary>Steht der Fortschrittsbalken? (Prüfhilfe für Tests.)</summary>
    public bool LaufSichtbar => _lauf is not null && _laufZeigen;

    /// <summary>
    /// Die SENKE, die der Wirt an <c>KiAusfuehrung.Fortschritt</c> hängt. Sie lebt so
    /// lange wie die Komponente — der Kern meldet über sie aus jedem Faden.
    /// </summary>
    private IProgress<KiFortschritt> Laufsenke => _laufSenke ??= new Fortschrittssenke(this);

    /// <summary>
    /// Die Abbruchmarke der gerade laufenden Anforderung; ohne Lauf
    /// <see cref="CancellationToken.None"/>.
    /// </summary>
    private CancellationToken Abbruchmarke()
    {
        CancellationTokenSource? quelle = _lauf;
        if (quelle is null) return CancellationToken.None;

        try { return quelle.Token; }
        catch (ObjectDisposedException) { return CancellationToken.None; }
    }

    // =====================================================================
    //  Beginnen und beenden
    // =====================================================================

    /// <summary>
    /// Beginnt einen Lauf: neue Abbruchquelle, neue Uhr, Balken je nach
    /// <paramref name="zeigen"/>.
    /// </summary>
    /// <param name="name">
    /// Der Name für den Verlaufseintrag — bei einer von Hand gewählten Aktion ihr
    /// Titel, bei einer Frage leer: Dort nennt ihn der ERSTE gemeldete Schritt.
    /// </param>
    /// <param name="zeigen">
    /// Soll der Balken sofort stehen? Bei einer von Hand gewählten Aktion ja (sie IST
    /// die Rechnung), bei einer Frage erst mit dem ersten Schritt.
    /// </param>
    private void LaufBeginnen(string name, bool zeigen)
    {
        _lauf = new CancellationTokenSource();
        _laufUhr = Stopwatch.StartNew();
        _laufName = Laufname(name);
        _laufAnteil = null;
        _laufAbgebrochen = false;
        _laufZeigen = zeigen;
        _laufText = _laufName.Length == 0 || string.IsNullOrEmpty(Texte.LaufLaeuft)
            ? _laufName
            : string.Format(CultureInfo.CurrentCulture, Texte.LaufLaeuft, _laufName);
    }

    /// <summary>
    /// Beendet den Lauf, räumt den Balken weg und schreibt die Schlusszeile in den
    /// Verlauf — benannt und mit Dauer.
    /// </summary>
    /// <remarks>
    /// <b>Die Schlusszeile gibt es nur, wenn der Balken stand.</b> Eine gewöhnliche
    /// Frage hat keine Laufzeit, über die zu berichten wäre; ihre Antwort steht ohnehin
    /// im Verlauf. Und sie gibt es nur MIT Namen: Ein „— abgebrochen nach 12,3 s" ohne
    /// Gegenstand wäre keine Auskunft.
    /// </remarks>
    private void LaufBeenden()
    {
        CancellationTokenSource? quelle = _lauf;
        Stopwatch? uhr = _laufUhr;
        bool gezeigt = _laufZeigen;
        bool abgebrochen = _laufAbgebrochen;
        string name = _laufName;

        _lauf = null;
        _laufUhr = null;
        _laufZeigen = false;
        _laufAnteil = null;
        _laufText = "";
        _laufName = "";
        _laufAbgebrochen = false;

        if (uhr is not null) uhr.Stop();

        if (gezeigt && name.Length > 0 && uhr is not null)
        {
            string dauer = uhr.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture);
            string format = abgebrochen ? Texte.LaufAbgebrochen : Texte.LaufFertig;

            if (!string.IsNullOrEmpty(format))
                Anhaengen(new[]
                {
                    new Gespraechszeile(
                        abgebrochen ? Gespraechsrolle.Warnung : Gespraechsrolle.Leise,
                        string.Format(CultureInfo.CurrentCulture, format, name, dauer))
                });
        }

        // Erst NACH dem letzten Zugriff entsorgen: Bis hierher kann der Kern die Marke
        // noch halten (eine Registrierung der Ansicht, ein verbundenes Token).
        try { quelle?.Dispose(); } catch (Exception) { /* eine Quelle stirbt nur einmal */ }
    }

    // =====================================================================
    //  Abbrechen
    // =====================================================================

    /// <summary>
    /// Der Rückruf am Baustein <c>Fortschritt</c>. Er wird GEMERKT und nicht bei jedem
    /// Zeichnen neu gebaut — ein neuer Rückruf gälte Blazor als geänderter Parameter.
    /// </summary>
    private EventCallback LaufAbbruchWeg
        => _laufAbbruchWeg ??= EventCallback.Factory.Create(this, LaufAbbrechen);

    /// <summary>
    /// Setzt die Abbruchmarke. Der Lauf hält erst an der nächsten Phasengrenze an —
    /// genau das sagt die Zeile unter dem Balken, damit niemand ein zweites Mal drückt.
    /// </summary>
    private void LaufAbbrechen()
    {
        CancellationTokenSource? quelle = _lauf;
        if (quelle is null) return;

        try
        {
            if (quelle.IsCancellationRequested) return;
            _laufAbgebrochen = true;
            if (!string.IsNullOrEmpty(Texte.LaufAbbruch)) _laufText = Texte.LaufAbbruch;
            quelle.Cancel();
        }
        catch (ObjectDisposedException) { /* der Lauf war schneller */ }

        StateHasChanged();
    }

    // =====================================================================
    //  Die Schritte des Kerns
    // =====================================================================

    /// <summary>
    /// Ein gemeldeter Schritt. Er kommt aus einem fremden Faden und wird deshalb über
    /// den Blazor-Verteiler eingetragen.
    /// </summary>
    private void SchrittGemeldet(KiFortschritt? schritt)
    {
        if (schritt is null) return;

        _ = InvokeAsync(() =>
        {
            // Ein Schritt, der nach dem Ende eintrifft, wird verworfen - er beschriebe
            // einen Lauf, den es nicht mehr gibt.
            if (_lauf is null) return;

            _laufZeigen = true;
            _laufAnteil = schritt.Anteil;

            if (_laufName.Length == 0) _laufName = Laufname(schritt.Text);

            // Nach dem Klick auf „Abbrechen" bleibt die Abbruchzeile stehen: Sie ist die
            // Antwort auf den Klick, und der Lauf meldet bis zur Phasengrenze weiter.
            if (!_laufAbgebrochen && !string.IsNullOrEmpty(schritt.Text)) _laufText = schritt.Text;

            StateHasChanged();
        });
    }

    /// <summary>
    /// Der Name eines Laufs für den Verlauf: ohne nachgestellte Auslassungspunkte.
    /// </summary>
    /// <remarks>
    /// Die erste Meldung einer Rechenaktion endet auf „ …" („Die Flotte wird
    /// bewertet …"). In der Schlusszeile steht sie vor einem Gedankenstrich, und dort
    /// wären die drei Punkte ein Satzzeichen zuviel.
    /// </remarks>
    private static string Laufname(string? text)
        => (text ?? "").Trim().TrimEnd('…', '.', ' ').Trim();

    /// <summary>Die Senke, die der Wirt an den Kern hängt.</summary>
    private sealed class Fortschrittssenke : IProgress<KiFortschritt>
    {
        private readonly KiChatDialog _wirt;

        internal Fortschrittssenke(KiChatDialog wirt) { _wirt = wirt; }

        /// <inheritdoc/>
        public void Report(KiFortschritt wert) => _wirt.SchrittGemeldet(wert);
    }
}
