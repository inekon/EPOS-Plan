using System;

namespace EPOS.UI.Dienste;

/// <summary>
/// Der geteilte Zustand einer nicht-modalen Seite (iU9-W5.0).
///
/// <para><b>Wozu.</b> Eine <c>BlazorDialogForm</c> setzt ihre Parameter EINMAL,
/// beim Aufbau - ein Dialog lebt kurz, und was sich waehrenddessen aendert,
/// haelt die Komponente selbst. Eine SEITE lebt so lange wie ihre Maske, und
/// unter ihr wechselt das Projekt: Wer im Kopfband der Startmaske auf eine
/// andere Version derselben Gruppe umschaltet, erwartet, dass die Seite folgt.
/// Die WebView deswegen wegzuwerfen und neu zu bauen waere jedes Mal ein
/// Aufblitzen und eine Drittelsekunde Wartezeit (Risiko R5).</para>
///
/// <para><b>Bauweise.</b> Ein gewoehnliches Objekt mit einem Ereignis. Die
/// Huelle schreibt (<see cref="ProjektSetzen"/>), die Komponente liest und
/// haengt sich an <see cref="Geaendert"/>. Es ist bewusst KEIN Dienst aus dem
/// Verzeichnis der Huelle: Ein Dienst waere fuer alle Seiten derselbe, dieser
/// Zustand gehoert genau einer.</para>
///
/// <para><b>Faden.</b> Geschrieben wird aus dem Oberflaechenfaden von WinForms,
/// gelesen im Blazor-Verteiler. Die Komponente ruft in ihrem Empfaenger
/// deshalb <c>InvokeAsync</c>, bevor sie zeichnet - dasselbe Muster wie
/// <c>AppWurzel.OeffneMaske</c>.</para>
///
/// <para><b>Warum hier und nicht in der Windows-Fassung.</b> Die Komponente
/// darf WinForms nicht kennen (Hausregel EPOS.UI). Der Zustand steht deshalb in
/// der Bibliothek; die Huelle legt ihn an und reicht ihn als Parameter
/// herein.</para>
/// </summary>
public sealed class SeitenZustand
{
    /// <summary>
    /// Name des Komponentenparameters, unter dem die Huelle den Zustand
    /// hereinreicht. Ein Schluessel, kein Anzeigetext.
    /// </summary>
    public const string PARAMETER = "Zustand";

    /// <summary><c>Tab_Projekt.ID</c> des offenen Projekts; <c>0</c> = keins.</summary>
    public int ProjektId { get; private set; }

    /// <summary>Projektname des offenen Projekts; <c>""</c> = keins.</summary>
    public string ProjektName { get; private set; } = "";

    /// <summary>
    /// Wird nach jeder Aenderung ausgeloest - Projektwechsel und ausdrueckliches
    /// Auffrischen.
    /// </summary>
    public event Action? Geaendert;

    /// <summary>
    /// Setzt das offene Projekt. Meldet nur, wenn sich wirklich etwas geaendert
    /// hat - ein Reiterwechsel loest den Setzer bei jedem Betreten aus, und ein
    /// Neuzeichnen ohne Anlass waere sichtbar.
    /// </summary>
    public void ProjektSetzen(int id, string name)
    {
        string neu = name ?? "";
        if (ProjektId == id && string.Equals(ProjektName, neu, StringComparison.Ordinal)) return;

        ProjektId = id;
        ProjektName = neu;
        Melden();
    }

    /// <summary>
    /// Bittet die Seite, ihre Daten neu zu lesen - ohne dass sich das Projekt
    /// geaendert haette (nach einem Simulationslauf, nach einer Uebernahme).
    /// </summary>
    public void Auffrischen() => Melden();

    private void Melden()
    {
        Action? h = Geaendert;
        if (h != null) h();
    }
}
