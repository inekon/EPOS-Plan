namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Die aktuelle Wahl im Dialog <see cref="VorlagenUebernahmeDialog"/> (iU9-W1.4).
///
/// <para>
/// Der Dialog rechnet und schreibt nicht selbst (Hausmuster
/// <c>Form_BkUebernahme</c>): Zaehlen und Schreiben erledigt die Huelle ueber
/// <c>KostenVorlagenUebernahmeCtrl</c>. Dieses Buendel ist alles, was sie dafuer
/// braucht.
/// </para>
/// </summary>
/// <param name="AusVorlage"><c>true</c> = Quelle ist eine Vorlage/Variante des
/// Admin-Katalogs, <c>false</c> = ein anderes Projekt samt Anlage.</param>
/// <param name="ZielProjektId">Das gewaehlte Zielprojekt.</param>
/// <param name="QuellVorlageId">Die gewaehlte Quellvorlage (nur bei <c>AusVorlage</c>).</param>
/// <param name="QuellProjektId">Das gewaehlte Quellprojekt (nur ohne <c>AusVorlage</c>).</param>
/// <param name="QuellAnlageId">Die gewaehlte Quellanlage; 0 = ohne Anlagenzuordnung.</param>
/// <param name="Invest">Die im Katalogblock gewaehlte KATEGORIE — <c>true</c> =
/// Investitionskosten, <c>false</c> = Betriebskosten. Die Huelle uebersetzt sie in
/// die Kategorie-Id; ohne <c>AusVorlage</c> bleibt die Kategorie, aus der der
/// Dialog geoeffnet wurde.</param>
public sealed record VorlagenUebernahmeWahl(
    bool AusVorlage,
    int ZielProjektId,
    int QuellVorlageId,
    int QuellProjektId,
    int QuellAnlageId,
    bool Invest);

/// <summary>
/// EINE Zeile der Positionsvorschau unter der Variantenwahl — nur Anzeige, kein
/// Editor.
///
/// <para>Die Maske kennt keine Datenbank: Was hier steht, ist bereits fertig
/// formatiert (Satz samt Einheit, Nutzungsdauer als Text), gebaut von der Huelle
/// aus <c>KostenVorlagenCtrl.Positionen</c> und <c>BemessungKatalog</c>. Die
/// Spaltenkoepfe sind dieselben wie im Positionsraster der Kostenverwaltung
/// (<c>KDLG_SP_*</c>).</para>
///
/// <para><b>ANWENDERBEFUND 19.09.2026:</b> Die Zeile sagt auch, was mit ihr
/// geschaehe. „Die Quelle enthaelt 7 Positionen, das Ziel fuehrt bereits 7"
/// beantwortet nicht, WELCHE davon fehlen — <c>Zustand</c> tut es je Zeile, mit
/// derselben Dublettenregel, nach der <c>AusVorlage</c> anlegt oder ueberspringt
/// (gleiche Position an derselben Anlage).</para>
/// </summary>
/// <param name="Bezeichnung">Die Position (Spalte <c>KDLG_SP_POSITION</c>).</param>
/// <param name="Bemessung">Anzeigetext der Bemessungsart im Gewerk.</param>
/// <param name="Satz">Der Satz samt Einheitenzeichen, leer wenn ungepflegt.</param>
/// <param name="Nutzungsdauer">Nutzungsdauer [a] als Text; auf der Betriebsseite leer.</param>
/// <param name="Zustand">„vorhanden" oder „wird angelegt"; leer ohne Zielprojekt.</param>
/// <param name="Vorhanden"><c>true</c> = das Ziel fuehrt die Position schon.</param>
public sealed record VorlagenPositionZeile(
    string Bezeichnung,
    string Bemessung,
    string Satz,
    string Nutzungsdauer,
    string Zustand,
    bool Vorhanden);

/// <summary>
/// Was der Dialog beim Schliessen meldet (Ae25 samt Kategoriewahl).
///
/// <para><b>Warum nicht mehr nur ein <c>bool</c>.</b> Uebernommen wird in die im
/// Katalogblock GEWAEHLTE Kategorie, und die muss nicht die sein, aus der der
/// Dialog geoeffnet wurde. Der Wirt schaltet danach auf sie um — sonst zeigte die
/// Kostenverwaltung die andere Seite, und der Anwender saehe von seiner Uebernahme
/// nichts.</para>
/// </summary>
/// <param name="Geschrieben">Ae25: <c>true</c> = OK hat uebernommen, <c>false</c> = abgebrochen.</param>
/// <param name="Invest">Die Kategorie, in die uebernommen wurde (<c>true</c> = Investition).</param>
public sealed record VorlagenUebernahmeSchluss(bool Geschrieben, bool Invest);

/// <summary>
/// Was die Huelle zur aktuellen Wahl sagt: der Klartext der Vorschau (§ 8 Nr. 3)
/// und ob sich damit uebernehmen laesst.
///
/// <para>Beides gehoert zusammen, weil beides aus denselben Zaehlungen faellt —
/// <c>VorschauAktualisieren</c> setzte <c>lblVorschau.Text</c> und
/// <c>btnUebernehmen.Enabled</c> in einem Zug.</para>
/// </summary>
/// <param name="Text">Der Vorschautext; leer = kein Zielprojekt gewaehlt.</param>
/// <param name="UebernahmeMoeglich">Ist der Uebernehmen-Knopf bedienbar?</param>
public sealed record VorlagenUebernahmeVorschau(string Text, bool UebernahmeMoeglich);

/// <summary>Das Ergebnis eines Uebernahmelaufs — der Klartext des Controllers.</summary>
/// <param name="Fehler"><c>true</c>, wenn etwas schiefging (Warnbanner statt Hinweis).</param>
/// <param name="Meldung">Die Meldungen des Controllers, bereits zu einem Text verbunden.</param>
public sealed record VorlagenUebernahmeAntwort(bool Fehler, string Meldung);
