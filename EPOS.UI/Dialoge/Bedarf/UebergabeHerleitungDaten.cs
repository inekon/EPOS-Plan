namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die hergeleiteten Vorgaben der Wärmeübergabe</b> (Anlagenkopplung 8.4, H10), wie die Hülle
/// sie aus dem Kern liefert (<c>UebergabeHerleitungsquelle</c>) — die zwei Zahlen, die die Gruppe
/// „Wärmeübergabe" als Vorgabe neben die leeren Felder schreibt, dazu die Kälteseite (E37) für den
/// Unterabschnitt „Kühlübergabe".
/// </summary>
/// <param name="AuslegungAussenC">Kältestes Tagesmittel der Klimareihe, abgerundet [°C]; <c>null</c> = keine Zahl.</param>
/// <param name="AuslegungsheizlastKw">Stationäre Heizlast des Gebäudes im Auslegungspunkt [kW]; <c>null</c> = keine Zahl.</param>
/// <param name="Befund">Warum es keine Zahl gibt (eine Prüfung des Kerns); leer mit Zahl.</param>
/// <param name="Kuehlung">Die Herleitung der Kühlübergabe; <c>null</c> = nichts herzuleiten (keine Kühlübergabeart, kein Kühlsollwert, kein Projekt).</param>
public sealed record UebergabeHerleitungDaten(double? AuslegungAussenC, double? AuslegungsheizlastKw, string Befund,
                                              KuehluebergabeHerleitungDaten? Kuehlung = null);

/// <summary>
/// <b>Die hergeleiteten Vorgaben der Kühlübergabe</b> (E37; Anlagenkopplung 7.2, 8.4; A2) — was
/// der Kern für den Satz im Dialog herleitet: den Auslegungstag samt Kühllast (= Nennleistung bei
/// leerem Feld, sensibel), Quelle und Höhe des festen Kaltwasser-Vorlaufs und die Frage, ob das
/// geöffnete Projekt die Kälteseite rechnet. Ausdrücklich kein Normnachweis.
/// </summary>
/// <param name="AuslegungstagText">Der Auslegungstag als Tag und Monat; leer ohne Herleitung.</param>
/// <param name="AuslegungstagMittelC">Das Tagesmittel der Außenluft am Auslegungstag [°C].</param>
/// <param name="AuslegungskuehllastKw">Die Kühllast des Auslegungstags [kW], sensibel.</param>
/// <param name="VorlaufAusAnlage">Kommt der Kaltwasser-Vorlauf aus einer Wärmepumpe im Kühlbetrieb (sonst die Auslegung)?</param>
/// <param name="VorlaufQuelleC">Der Kaltwasser-Vorlauf der Quelle vor dem Hochmischen [°C].</param>
/// <param name="VorlaufC">Der feste Kaltwasser-Vorlauf am Gebäude [°C] = max(Quelle, Grenze).</param>
/// <param name="Gekappt">Mischt das Gebäude das Kaltwasser auf die Vorlaufgrenze hoch?</param>
/// <param name="VorlaufgrenzeC">Die wirksame Vorlaufgrenze [°C]; <c>null</c> = keine Grenze.</param>
/// <param name="ProjektKoppelt">Rechnet das geöffnete Projekt eine Kopplungsstufe?</param>
/// <param name="ProjektKuehlt">Rechnet das geöffnete Projekt Kälte (Projekteinstellung „Kühlung rechnen")?</param>
/// <param name="Befund">Warum es keine Zahl gibt (eine Prüfung des Kerns); leer mit Zahl.</param>
public sealed record KuehluebergabeHerleitungDaten(string AuslegungstagText, double? AuslegungstagMittelC,
                                                   double? AuslegungskuehllastKw, bool VorlaufAusAnlage,
                                                   double? VorlaufQuelleC, double? VorlaufC, bool Gekappt,
                                                   double? VorlaufgrenzeC, bool ProjektKoppelt, bool ProjektKuehlt,
                                                   string Befund);
