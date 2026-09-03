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
    /// <summary>
    /// Katalog „Gesetzliche Parameter", vorgewählt auf die Klasse CO₂-Preis —
    /// der Knopf der Emissionsgruppe im Wirtschaftlichkeits-Parameterdialog
    /// (Vorläufer: <c>Form_WirtschaftlichkeitParameter.btnGesetzeskatalog_Click</c>).
    /// </summary>
    public const string GesetzesparameterCo2 = "GESETZESPARAMETER_CO2";

    /// <summary>
    /// Katalog „Gesetzliche Parameter" ohne Vorwahl — dieselbe Maske, aber
    /// von einem Einstieg aus, der keine Klasse meint.
    /// </summary>
    public const string Gesetzesparameter = "GESETZESPARAMETER";

    /// <summary>
    /// Katalog „Administration Heizkessel" — der Knopf „Admin" des
    /// Heizkessel-Projektdialogs (Vorläufer:
    /// <c>Form_Heizkessel.btn_Admin_Click</c>, iU9-W6.3).
    /// </summary>
    public const string HeizkesselAdmin = "HEIZKESSEL_ADMIN";

    /// <summary>
    /// Katalog „Administration Stromspeicher" — der Knopf „Bearbeiten" des
    /// Stromspeicher-Projektdialogs (Vorläufer:
    /// <c>Form_Stromspeicher.btn_Bearbeiten_Click</c>, iU9-W6.6).
    /// </summary>
    public const string StromspeicherAdmin = "STROMSPEICHER_ADMIN";

    /// <summary>
    /// Katalog „Administration Photovoltaik" — der Knopf „Bearbeiten" des
    /// PV-Projektdialogs (Vorläufer: <c>Form_PV.btn_Bearbeiten_Click</c> über
    /// <c>MenueCtrl.PV()</c>, iU9-W6.5).
    /// </summary>
    public const string PvAdmin = "PV_ADMIN";

    /// <summary>
    /// Katalog „Administration Pufferspeicher" — der Knopf „Bearbeiten" des
    /// Pufferspeicher-Projektdialogs (Vorläufer:
    /// <c>Form_PufferSp.btn_Bearbeiten_Click</c> über <c>MenueCtrl.PufferSp()</c>,
    /// iU9-W6.7).
    /// </summary>
    public const string PufferSpAdmin = "PUFFERSP_ADMIN";

    /// <summary>
    /// Katalog „Stammdaten Solarthermieganglinien" — der Knopf „Bearbeiten…" des
    /// Solarganglinien-Dialogs (Vorläufer:
    /// <c>Form_Solarganglinie.btn_Bearbeiten_Click</c> über <c>MenueCtrl.Solarganglinie()</c>,
    /// iU9-W7.0f). Die Verwaltung selbst bleibt bis Welle 14b eine WinForms-Maske.
    /// </summary>
    public const string SolarganglinieAdmin = "SOLARGANGLINIE_ADMIN";
}
