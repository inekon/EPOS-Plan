using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Ein Rechenlauf des Dialogs <see cref="KapitalwertVerlaufDialog"/> (iU9-W1.6):
/// zwei fertige Bilder und die beiden Textzeilen darunter.
///
/// <para>
/// Gebaut wird im Kern (<c>ChartRenderer.KapitalwertVerlaufModell</c>); die
/// Oberflaeche bekommt ein ZEICHENMODELL und setzt es ueber <c>DiagrammSvg</c> als
/// Vektorbild in den Baum (Etappe DG-E3, Gruppe (c)). Der Vorlaeufer
/// <c>Form_WirtschaftlichkeitVerlauf</c> baute <c>System.Drawing</c>-Bilder und
/// musste sie von Hand entsorgen — in <c>EPOS.UI</c> gibt es weder das eine noch das
/// andere.
/// </para>
///
/// <para><b>Beide Modelle kommen aus EINEM Lauf und bleiben stehen, bis der naechste
/// rechnet.</b> Der Baustein baut seinen Knotenbaum nur neu, wenn die REFERENZ des
/// Modells wechselt; ein je Zeichenlauf neu gebautes Modell verwuerfe mit dem Baum
/// auch Zoom, Zeigerstelle und abgewaehlte Reihen.</para>
/// </summary>
/// <param name="Differenz">Das obere Bild: Differenz zur Stamm-Referenz.</param>
/// <param name="Absolut">Das untere Bild: kumulierte Barwerte je Projekt.</param>
/// <param name="Restwerttext">Die Zeile unter den Bildern (Restwert-Barwerte,
/// davor die nicht berechenbaren Projekte) — frueher <c>lblRestwert</c>.</param>
/// <param name="Statustext">Die Zeile darunter (Zeitraum, Szenario, Hinweis auf
/// einen von T abweichenden Horizont) — frueher <c>lblStatus</c>.</param>
public sealed record KapitalwertVerlaufBilder(
    Zeichenmodell? Differenz,
    Zeichenmodell? Absolut,
    string Restwerttext,
    string Statustext);
