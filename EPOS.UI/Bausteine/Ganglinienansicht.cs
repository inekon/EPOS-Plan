using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Zeitreihe im Stammblatt</b> (Konzept Administrationsdialoge, Stufe 4: V9
/// Gruppe „Ganglinie" in den Verwaltungen Wärmebedarf Lastgang, Solarthermie- und
/// Stromganglinie) — das Bild des Jahresverlaufs und die drei Kennzahlen.
///
/// <para><b>Gerechnet wird im Kern</b>: Das Modell kommt aus
/// <c>ChartRenderer.JahresverlaufModell</c>, die Kennzahlen aus
/// <c>GanglinienAuswertungCtrl</c> — dieselben Zahlen, die die Projektdialoge über
/// <see cref="GanglinienGrafik"/> zeigen. Die Oberfläche bekommt beides fertig.</para>
/// </summary>
/// <param name="Modell">Der Jahresverlauf als Zeichenmodell; <c>null</c> = keine brauchbare Reihe.</param>
/// <param name="Kennzahlen">Jahresarbeit, Spitze, Volllaststunden; <c>null</c> = keine Reihe.</param>
/// <param name="Meldung">Warum es kein Bild gibt; leer = es gibt eines.</param>
public sealed record Ganglinienansicht(Zeichenmodell? Modell, GanglinienKennzahlen? Kennzahlen,
                                       string Meldung = "")
{
    /// <summary>Keine Reihe — mit dem Grund.</summary>
    public static Ganglinienansicht Ohne(string meldung) => new(null, null, meldung ?? "");
}
