namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Ein Rechenlauf des Dialogs <see cref="KapitalwertVerlaufDialog"/> (iU9-W1.6):
/// zwei fertige Bilder und die beiden Textzeilen darunter.
///
/// <para>
/// Gezeichnet wird im Kern (<c>ChartRenderer.KapitalwertVerlauf</c>, SkiaSharp);
/// die Oberflaeche zeigt nur noch PNG-Bytes. Der Vorlaeufer
/// <c>Form_WirtschaftlichkeitVerlauf</c> baute daraus <c>System.Drawing</c>-Bilder
/// und musste sie von Hand entsorgen — in <c>EPOS.UI</c> gibt es weder das eine
/// noch das andere.
/// </para>
/// </summary>
/// <param name="Differenz">Das obere Bild: Differenz zur Stamm-Referenz.</param>
/// <param name="Absolut">Das untere Bild: kumulierte Barwerte je Projekt.</param>
/// <param name="Restwerttext">Die Zeile unter den Bildern (Restwert-Barwerte,
/// davor die nicht berechenbaren Projekte) — frueher <c>lblRestwert</c>.</param>
/// <param name="Statustext">Die Zeile darunter (Zeitraum, Szenario, Hinweis auf
/// einen von T abweichenden Horizont) — frueher <c>lblStatus</c>.</param>
public sealed record KapitalwertVerlaufBilder(
    byte[]? Differenz,
    byte[]? Absolut,
    string Restwerttext,
    string Statustext);
