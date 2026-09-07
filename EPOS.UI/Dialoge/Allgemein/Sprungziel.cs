namespace EPOS.UI.Dialoge.Allgemein;

/// <summary>
/// Die SPRUNGZIELE — sprachneutrale ASCII-Schlüssel für Fenster, die eine
/// Razor-Komponente öffnen lassen möchte, ohne sie zu kennen (iU9-W2.2).
///
/// <para><b>Die Liste ist LEER, und das ist das Ergebnis, nicht ein Rest.</b>
/// Der Mechanismus hat zehn Ziele getragen; jedes davon ist gefallen, sobald
/// sein Ziel selbst eine Razor-Komponente wurde — aus jedem Sprung ist eine
/// <c>Ueberlagerung</c> im selben Fenster geworden (Risiko R2: zwei WebViews
/// übereinander kosten Speicher, Aufbauzeit und eine Fokusreihenfolge, die
/// niemand mehr erklären kann).</para>
///
/// <para><b>Der Weg dorthin</b>, damit niemand ihn zweimal geht:</para>
/// <list type="bullet">
///   <item><description>iU9-W13.2 — <c>WaermebedarfExternAdmin</c>; das Ziel wurde
///     selbst Blazor.</description></item>
///   <item><description>iU9-W14a.4 — die fünf Katalogverwaltungen der Erzeuger
///     (<c>HeizkesselAdmin</c>, <c>StromspeicherAdmin</c>, <c>PvAdmin</c>,
///     <c>PufferSpAdmin</c>, <c>PufferSpAdminNurLesen</c>).</description></item>
///   <item><description>iU9-W14b.2 — <c>SolarganglinieAdmin</c>.</description></item>
///   <item><description>iU9-W14c.3 — die zwei Gesetzesziele
///     (<c>Gesetzesparameter</c>, <c>GesetzesparameterCo2</c>).</description></item>
///   <item><description><b>W11b‑B‑5</b> (Windows-Abnahme V2 vom 07.09.2026) —
///     <c>SpeicherOptimierung</c>, das letzte. Es war das einzige Ziel MIT
///     Parameter (dem gerechneten Lauf) und das einzige, dessen Antwort nicht
///     „mit OK geschlossen" hieß, sondern
///     <c>Form_SpeicherOptimierung.AuslegungUebernommen</c>. Der Entscheid
///     iF22 („die Maske bleibt WinForms, sie ist der einzige Ort des Programms,
///     an dem ScottPlot läuft") ist mit zwei Befunden des Anwenders überholt
///     worden — „Texte überschneiden sich" und „Dialog stürzt nach kurzer Zeit
///     ab" —, und nach der Arbeitsregel iZ5 wurde daraus die Überlagerung
///     <c>EPOS.UI/Dialoge/Strom/SpeicherOptimierungDialog.razor</c>. Mit ihm ist
///     auch die Windows-Seite gefallen:
///     <c>WindowsFormsApplication1/Allgemein/Blazor/Sprungbruecke.cs</c> hatte
///     keinen Zweig mehr.</description></item>
/// </list>
///
/// <para><b>Wozu die Klasse dann noch steht.</b> Sie ist die Registerstelle des
/// Musters, nicht sein Rest: Käme je wieder eine Razor-Komponente, die ein
/// <b>WinForms</b>-Fenster öffnen lassen muss, gehört ihr Schlüssel hierher —
/// und die Windows-Brücke dazu wieder neu angelegt. Solange
/// <c>WindowsFormsApplication1</c> keine Fachmaske mehr führt, gibt es dafür
/// keinen Anlass. <b>Ein Blazor-Ziel gehört ausdrücklich NICHT hierher</b>: Es
/// wird eine <c>Ueberlagerung</c> im selben Fenster.</para>
///
/// <para><b>Das Muster, falls es wiederkommt.</b> Die Komponente nimmt
/// <code>[Parameter] public Func&lt;string, Task&lt;bool&gt;&gt;? Sprung { get; set; }</code>
/// und ruft ihn mit einem Schlüssel dieser Klasse; was daraufhin erscheint,
/// entscheidet allein die Plattformhülle. <b>Kein Delegat ist kein Fehler</b> —
/// dann zeigt der Dialog den Knopf gar nicht erst; ein Knopf, der nichts tut,
/// wäre eine Behauptung, die nicht stimmt.</para>
/// </summary>
public static class Sprungziel
{
}
