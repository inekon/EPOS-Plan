namespace EPOS.UI.Dialoge.Allgemein;

/// <summary>
/// Die SPRUNGZIELE — sprachneutrale ASCII-Schlüssel für Fenster, die eine
/// Razor-Komponente öffnen lassen möchte, ohne sie zu kennen (iU9-W2.2).
///
/// <para><b>Das Problem.</b> Manche Dialoge führen weiter: Der
/// Wirtschaftlichkeits-Parameterdialog hat einen Knopf „CO₂-Preispfad
/// pflegen…", der in den Gesetzeskatalog springt. Der Katalog ist (bis
/// Welle 14c) eine WinForms-Maske. Eine Komponente in dieser Bibliothek darf
/// WinForms nicht kennen — sie darf es nicht einmal übersetzen
/// (<c>EnableWindowsTargeting=false</c>).</para>
///
/// <para><b>Die Lösung: ein Delegat mit Schlüssel.</b> Die Komponente nimmt
/// einen Parameter
/// <code>[Parameter] public Func&lt;string, Task&lt;bool&gt;&gt;? Sprung { get; set; }</code>
/// und ruft ihn mit einem der Schlüssel dieser Klasse. Was daraufhin erscheint,
/// entscheidet allein die Plattformhülle: unter Windows
/// <c>WindowsFormsApplication1.Sprungbruecke</c> (Schlüssel → <c>Form</c>,
/// Muster <c>Dienste.Navigation</c>/<c>Masken</c>), auf iOS später ein
/// Seitenwechsel. Die Antwort ist <c>true</c>, wenn das Ziel mit OK geschlossen
/// wurde — dann lädt der Dialog nach, was sich geändert haben kann.</para>
///
/// <para><b>Kein Delegat ist kein Fehler.</b> Ist <c>Sprung</c> nicht gesetzt
/// (Prüfstand, iOS ohne dieses Ziel), zeigt der Dialog den Knopf gar nicht
/// erst. Ein Knopf, der nichts tut, wäre eine Behauptung, die nicht stimmt.</para>
///
/// <para><b>Grenze (Risiko R1 des Wellenplans).</b> Unter Windows läuft der
/// Rückruf im Oberflächenfaden — das Zielfenster öffnet eine VERSCHACHTELTE
/// Nachrichtenschleife über dem Blazor-Dialog, genau wie ein
/// <c>OpenFileDialog</c> in einem Click-Ereignis. Für ein WinForms-Ziel ist das
/// erprobt. Wo das Ziel selbst eine Blazor-Hülle ist, bleibt es beim
/// NACHGELAGERTEN Sprung (schließen → Ziel → wieder öffnen, Muster
/// <c>BhkwWirtschaftlichkeitHuelle.TarifOeffnen</c>): Zwei WebViews
/// übereinander sind Risiko R2, und dafür gibt es bis Welle 4 keinen
/// Baustein.</para>
/// </summary>
public static class Sprungziel
{
    // iU9-W14c.3: Die ZWEI Gesetzeszweige sind hier weg - Gesetzesparameter und
    // GesetzesparameterCo2. Sie waren die letzten zwei abloesbaren Schluessel
    // ueberhaupt: Beide Sprungquellen waren schon vorher Razor (Befund W14c-B13),
    // aus jedem Sprung ist eine UEBERLAGERUNG im selben Fenster geworden, und die
    // Vorwahl der Klasse CO2_PREIS reicht der Wirt als Parameter hinein.
    //
    // WAS BLEIBT, IST EIN ENTSCHEID, KEIN REST (R-W14c-11): SpeicherOptimierung
    // steht bis Welle 16. Form_SpeicherOptimierung bleibt WinForms (iF22) - sie ist
    // der einzige Ort des Programms, an dem ScottPlot laeuft. Wer Sprungziel und
    // Sprungbruecke jetzt "aufraeumt", bricht sie.

    // iU9-W14a.4: Die FUENF Katalogverwaltungen der Erzeuger sind hier weg -
    // HeizkesselAdmin, StromspeicherAdmin, PvAdmin, PufferSpAdmin und
    // PufferSpAdminNurLesen. Ihre Ziele sind selbst Blazor geworden; aus jedem
    // Sprung ist eine UEBERLAGERUNG im selben Fenster geworden (Muster W4/W10a,
    // Risiko R2), und der Aufrufer bekommt den Parametersatz der Verwaltung
    // statt eines Schluessels. Die Sprungbruecke bleibt fuer die WinForms-Ziele.

    /// <summary>
    /// „Auslegung optimieren …" — die Rastersuche des Stromspeichers
    /// (<c>Form_SpeicherOptimierung</c>, Vorläufer
    /// <c>Form_Simulation_Detail.SpOptimierung_Click</c>:5992, iU9-W11b.0).
    ///
    /// <para><b>Warum sie eine Brücke braucht und keine Überlagerung.</b> Die Maske
    /// bleibt WinForms (Entscheid iF22): Sie ist der einzige Ort des Programms, an dem
    /// <c>ScottPlot.WinForms</c> läuft — Heatmap und Schnittkurve der Rastersuche. Eine
    /// Razor-Fassung gibt es dafür bis auf Weiteres nicht.</para>
    ///
    /// <para><b>Was die Antwort bedeutet.</b> <c>true</c> heißt hier NICHT „mit OK
    /// geschlossen", sondern <c>Form_SpeicherOptimierung.AuslegungUebernommen</c> —
    /// der Anwender hat den Bestpunkt übernommen, und damit hat sich die Speichervariante
    /// geändert. Die Seite liest sie danach neu; neu gerechnet wird bewusst nicht
    /// (wörtlich wie im Vorläufer).</para>
    /// </summary>
    public const string SpeicherOptimierung = "SPEICHER_OPTIMIERUNG";
}
