namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Eine Projektwärmepumpe im Kühlbetrieb</b> in der Projektliste der Kältemaschine (KA‑E‑26, KM‑K1) — eine Zeile je
/// Projektkopie; die Summe zählt jede ihrer Anlagen. Die Hülle füllt sie aus <c>KuehlerzeugerKatalogCtrl.ProjektWaermepumpen</c>.
/// </summary>
/// <param name="IdWp">Die Projektkopie (<c>Tab_WP.ID</c>).</param>
/// <param name="IdStamm">Der Katalogsatz der Kopie; <c>null</c> = keiner.</param>
/// <param name="Bezeichner">Der Name der Kopie.</param>
/// <param name="Firma">Der Hersteller.</param>
/// <param name="Quelle">Die Bauart als Quelle (Luft, Sole, Wasser).</param>
/// <param name="Anlagen">Zahl der Anlagen des Projekts mit diesem Gerät.</param>
/// <param name="Rang">Rang in der Kältefolge; <c>null</c> = Vorgabefolge.</param>
/// <param name="KuehlleistungKw">Kühlleistung am Kühlvorlauf [kW].</param>
/// <param name="Eer">EER am Kühlvorlauf.</param>
/// <param name="KuehlVorlauf">Kühlvorlauf, mit dem gerechnet wird [°C].</param>
/// <param name="Kuehltraeger">Kühlträger (Anzeigetext).</param>
/// <param name="EigenerZaehler">Eigener Zähler der Kühlung?</param>
/// <param name="Zulaessigkeit">Die Lesezeile „Zulässigkeit“ (Kältemittel, Inbetriebnahmejahr der Anlage).</param>
public sealed record KuehlWaermepumpeZeile(int IdWp, int? IdStamm, string Bezeichner, string Firma, string Quelle, int Anlagen,
                                           int? Rang, double? KuehlleistungKw, double? Eer, int? KuehlVorlauf,
                                           string Kuehltraeger, bool EigenerZaehler, string Zulaessigkeit);

/// <summary>
/// Das Ergebnis von „In das Projekt übernehmen“ einer Katalog-Wärmepumpe: <paramref name="Ok"/> = Kühlbetrieb eingeschaltet;
/// sonst nennt <paramref name="Meldung"/> den Grund (Ablehnung des Kerns oder Fehler). <paramref name="Merker"/> ist der
/// Zettel der Hülle für „Abbrechen nimmt die Übernahme zurück“.
/// </summary>
/// <param name="Hinweis">
/// KM‑K2: ein Hinweis zur übernommenen Wärmepumpe, der keine Ablehnung ist — etwa der fehlende Platz der Wärmekaskade
/// (ohne ihn kühlt sie nicht). <c>null</c> = kein Hinweis.
/// </param>
public sealed record KuehlWpUebernahme(bool Ok, int IdWp, string Bezeichner, bool SchonImProjekt, string? Meldung,
                                       object? Merker, string? Hinweis = null);

/// <summary>
/// <b>Der Bearbeitungsweg einer Wärmepumpen-Anlagenzeile</b> (KM‑K2, Naht <c>WaermepumpeAnlageGaben</c>): Der Wirt
/// reicht den Parametersatz der Anlagenfelder (<c>WaermepumpeAnlageFelder</c>) samt dem Schreibweg herein, dann öffnet
/// „Bearbeiten…" einer Wärmepumpe im Kältemaschinen-Dialog dieselbe Überlagerung wie der Wärmepumpendialog
/// (Gerätesteckbrief, Klappgruppe „Kühlbetrieb", Betriebszeiten). Ohne die Naht bleibt die benannte Sperre stehen.
/// </summary>
/// <param name="IdAnlage">Die bearbeitete Anlagenzeile (<c>Tab_Energieanlagen.ID</c>).</param>
/// <param name="Bezeichner">Der Name der Anlage — der Titel der Überlagerung.</param>
/// <param name="Gaben">Der Parametersatz der Anlagenfelder; <c>Daten</c> ist der Feldsatz, in den sie schreiben.</param>
/// <param name="Schreiben">
/// Schreibt den Feldsatz dieser Gaben; <c>null</c> = geschrieben, sonst der Grund der Ablehnung (er bleibt in der
/// Überlagerung stehen). Plattformfrei — die Datenseite liegt im Kern, nicht in der Schale.
/// </param>
public sealed record WaermepumpeAnlagegaben(int IdAnlage, string Bezeichner,
                                            IReadOnlyDictionary<string, object> Gaben, Func<string?> Schreiben);
