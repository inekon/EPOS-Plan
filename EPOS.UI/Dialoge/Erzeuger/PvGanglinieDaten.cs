namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Wie „Aus dem Katalog erneuern…" im Dialog „Photovoltaik Ganglinie" ausgegangen ist
/// (<c>PvGanglinieDialog.AusKatalogErneuern</c>, Kern <c>PvGanglinieStammCtrl.AusKatalogErneuern</c>).
/// </summary>
/// <param name="Erfolgreich">Kopf und Reihe der Projektkopie tragen den Stand des Katalogs.</param>
/// <param name="Meldung">Der Satz für den Anwender — Bestätigung oder benannter Grund.</param>
public sealed record PvGanglinieErneuerung(bool Erfolgreich, string Meldung);
