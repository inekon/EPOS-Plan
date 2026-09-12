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
public sealed record VorlagenUebernahmeWahl(
    bool AusVorlage,
    int ZielProjektId,
    int QuellVorlageId,
    int QuellProjektId,
    int QuellAnlageId);

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
