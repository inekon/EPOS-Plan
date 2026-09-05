namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Was die Pruefung einer Spotpreis-Datei ergibt (iU9-W3.2) — das, was
/// <c>SpotpreisImportCtrl.Lauf</c> nach aussen zeigt.
///
/// <para>Die Komponente kennt weder den Leser noch die Aufbereitung: Sie
/// zeigt das Protokoll, schaltet „Uebernehmen" frei und nennt das Jahr in
/// der Statuszeile. Alles Weitere bleibt im Kern.</para>
/// </summary>
/// <param name="Erfolgreich">Ist die Datei brauchbar? Nur dann darf uebernommen werden.</param>
/// <param name="Protokoll">Das vollstaendige Validierungsprotokoll, Zeile fuer Zeile.</param>
/// <param name="Jahr">Das erkannte Jahr der Reihe; 0, wenn keines erkannt wurde.</param>
public sealed record SpotpreisPruefung(bool Erfolgreich, string Protokoll, int Jahr);

/// <summary>
/// Was das Schreiben einer geprueften Reihe ergibt (iU9-W3.2).
/// </summary>
/// <param name="ReiheId">Die angelegte <c>Tab_Preisreihe.ID</c>; 0 = nichts geschrieben.</param>
/// <param name="Werte">Zahl der geschriebenen Stundenwerte (fuer die Statuszeile).</param>
public sealed record SpotpreisSpeicherung(int ReiheId, int Werte);
