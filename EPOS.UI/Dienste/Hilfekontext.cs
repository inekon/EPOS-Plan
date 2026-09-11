using System.Globalization;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dienste;

/// <summary>
/// WO DER ANWENDER GERADE STEHT — der Hilfekontext EINER Ansicht (Auftrag #221,
/// Anwenderentscheid <b>KI‑D‑E‑1</b> vom 11.09.2026).
///
/// <para><b>Wozu es ihn gibt.</b> Bis hierher trug die Kopfband-Pille des
/// <c>Hauptfenster</c>s einen FESTEN Hilfeschlüssel (<c>Hauptfenster.btn_Help</c>) und
/// stand damit über jeder Ansicht, während die Ansicht darunter eine zweite Pille mit
/// ihrem eigenen Schlüssel zeichnete. Der Anwender hat das am 11.09.2026 gemeldet:
/// „Die KI-Buttons haben keine unterschiedliche Funktion im Kontext. Daher ist es nicht
/// sinnvoll, auf einer Sicht zwei KI-Buttons zu sehen." Die Antwort ist EINE Pille je
/// Bildschirm — und damit die Frage, WELCHEN Schlüssel sie trägt. Diese Klasse ist die
/// Antwort darauf: Die Ansicht meldet ihn nach oben, die Pille folgt.</para>
///
/// <para><b>Er trägt Bedienbegriffe und keine Projektdaten</b> — dieselbe Grenze wie
/// <c>KiAufrufkontext.Dialogname</c>. „Simulation", „Ergebnis", „Stromspeicher" sind
/// Namen der Oberfläche; ein Projekt- oder Kundenname gehört nicht hinein, denn der
/// Kontext geht mit der Frage an den Assistenten.</para>
///
/// <para><b>Er ist ein WERT und wird verglichen.</b> Eine Ansicht meldet ihn nach jedem
/// Zeichenlauf; ohne Vergleich zöge jede Meldung einen weiteren Zeichenlauf nach sich.</para>
/// </summary>
public sealed class Hilfekontext : IEquatable<Hilfekontext>
{
    /// <summary>Keine Ansicht führt den Kontext — es gilt der Schlüssel des Wirts.</summary>
    public static readonly Hilfekontext Keiner = new Hilfekontext("");

    /// <summary>Legt einen Kontext an; alles außer dem Schlüssel ist freiwillig.</summary>
    /// <param name="schluessel">Der Hilfeschlüssel der Ansicht (<c>Form_Simulation_Detail.btn_Help</c>).</param>
    /// <param name="ansicht">Der Name der Ansicht in der Oberflächensprache.</param>
    /// <param name="schritt">Der Schritt der Ablaufleiste; leer = die Ansicht hat keine.</param>
    /// <param name="reiter">Das offene Reiterblatt; leer = die Ansicht hat keines.</param>
    public Hilfekontext(string? schluessel, string? ansicht = null,
                        string? schritt = null, string? reiter = null)
    {
        Schluessel = (schluessel ?? "").Trim();
        Ansicht = (ansicht ?? "").Trim();
        Schritt = (schritt ?? "").Trim();
        Reiter = (reiter ?? "").Trim();
    }

    /// <summary>Der Hilfeschlüssel — er bestimmt Hilfeseite UND Bereich des Assistenten.</summary>
    public string Schluessel { get; }

    /// <summary>Der Name der Ansicht („Simulation").</summary>
    public string Ansicht { get; }

    /// <summary>Der Schritt der Ablaufleiste („Ergebnis"); leer = keiner.</summary>
    public string Schritt { get; }

    /// <summary>Das offene Reiterblatt („Stromspeicher"); leer = keines.</summary>
    public string Reiter { get; }

    /// <summary>Führt dieser Kontext überhaupt einen Schlüssel?</summary>
    public bool Da => Schluessel.Length > 0;

    /// <summary>
    /// Die STELLE als ein Satzstück: „Simulation · Ergebnis · Stromspeicher". Genau das
    /// zeigt die Kontextzeile des Chats als Dialognamen.
    /// </summary>
    /// <remarks>
    /// <b>Das Trennzeichen kommt aus der Ressource</b> (<c>KI_KONTEXT_STELLE</c>) und
    /// steht nicht als Literal hier — dieselbe Regel wie bei jedem anderen Anzeigetext
    /// der Bibliothek. Leere Teile fallen weg; sind alle leer, ist auch die Stelle leer,
    /// und die Kontextzeile nennt dann nur den Bereich.
    /// </remarks>
    public string Dialogname
    {
        get
        {
            string ergebnis = "";

            foreach (string teil in new[] { Ansicht, Schritt, Reiter })
            {
                if (teil.Length == 0) continue;
                ergebnis = ergebnis.Length == 0 ? teil : Zusammen(ergebnis, teil);
            }

            return ergebnis;
        }
    }

    /// <inheritdoc/>
    public bool Equals(Hilfekontext? andere)
        => andere is not null
        && string.Equals(Schluessel, andere.Schluessel, StringComparison.Ordinal)
        && string.Equals(Ansicht, andere.Ansicht, StringComparison.Ordinal)
        && string.Equals(Schritt, andere.Schritt, StringComparison.Ordinal)
        && string.Equals(Reiter, andere.Reiter, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? andere) => Equals(andere as Hilfekontext);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Schluessel, Ansicht, Schritt, Reiter);

    /// <inheritdoc/>
    public override string ToString()
        => Dialogname.Length == 0 ? Schluessel : Schluessel + " (" + Dialogname + ")";

    /// <summary>Zwei Teile mit dem Trennzeichen der Ressource; ohne sie bleibt ein Punkt.</summary>
    private static string Zusammen(string links, string rechts)
    {
        string format = Text("KI_KONTEXT_STELLE", "{0} · {1}");
        try { return string.Format(CultureInfo.CurrentCulture, format, links, rechts); }
        catch (FormatException) { return links + " · " + rechts; }
    }

    private static string Text(string schluessel, string rueckfall)
    {
        try { return Resource.ResourceManager.GetString(schluessel) ?? rueckfall; }
        catch (Exception) { return rueckfall; }
    }
}

/// <summary>
/// Der Weg, auf dem eine Ansicht ihren <see cref="Hilfekontext"/> nach oben meldet
/// (Auftrag #221).
///
/// <para><b>Warum ein CascadingValue und kein Parameter je Ansicht.</b> Die
/// <c>AppWurzel</c> führt heute sechs Ansichten, und jede zweite davon wird von einem
/// Wörterbuch bestückt, das eine Hülle baut (<c>@attributes</c>). Ein weiterer
/// <c>[Parameter]</c> je Ansicht hieße: eine Zeile in der Wurzel, eine im Dialog, eine
/// in jeder Hülle, die den Satz baut — und eine Gelegenheit, ihn zu vergessen. Der
/// Melder kommt stattdessen von oben und wird nur dort gelesen, wo eine Ansicht ihn
/// braucht.</para>
///
/// <para><b>Er ist EIN Objekt für die Lebensdauer der Wurzel</b> (<c>IsFixed</c>): Ein
/// neuer Melder je Zeichenlauf zöge jede Ansicht darunter in einen neuen Parametersatz.</para>
/// </summary>
public sealed class Hilfekontextmelder
{
    private readonly Action<Hilfekontext?> _senke;

    /// <summary>Legt den Melder über die Senke, die den Kontext aufnimmt.</summary>
    public Hilfekontextmelder(Action<Hilfekontext?> senke)
        => _senke = senke ?? throw new ArgumentNullException(nameof(senke));

    /// <summary>
    /// Meldet den Kontext der Ansicht; <c>null</c> heißt „ich führe keinen mehr".
    /// </summary>
    /// <remarks>
    /// <b>Die Senke vergleicht.</b> Eine Ansicht meldet nach jedem Zeichenlauf — sie
    /// weiß nicht, ob sich ihr Schritt geändert hat, und soll es nicht wissen müssen.
    /// </remarks>
    public void Melden(Hilfekontext? kontext) => _senke(kontext);
}
