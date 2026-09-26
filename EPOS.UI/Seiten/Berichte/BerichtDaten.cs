using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Eine Zeile der Variantenliste einer Seite des Reiters „Berichte &amp; Kosten"
/// (iU9-W5.2). Vorbild <c>BerichtsDatenSammler.VariantenStatus</c>, aber ohne
/// eine Kernklasse in EPOS.UI zu ziehen: Die Hülle formt sie um.
/// </summary>
public sealed class VarianteZeile
{
    /// <summary><c>Tab_Projekt.ID</c> der Version.</summary>
    public int IdProjekt { get; set; }

    /// <summary>„Stamm" bzw. „Variante" (Anzeigetext von der Hülle).</summary>
    public string Art { get; set; } = "";

    /// <summary>Bezeichner der Variante bzw. „(Stammprojekt)".</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Projektname.</summary>
    public string Projektname { get; set; } = "";

    /// <summary>Simulationsstand als Text (leer = nie simuliert).</summary>
    public string SimStand { get; set; } = "";

    /// <summary>
    /// AUFTRAG VF-1 (Anwenderbefund 17.09.2026): <b>womit diese Version ihren
    /// Stromspeicher rechnet</b> — „mit Speicherflotte: … · 2 Einheiten",
    /// „Einzelspeicher: …" oder „ohne Stromspeicher".
    ///
    /// <para>Stamm und Varianten dürfen verschiedene Flotten führen; in der
    /// Vergleichsgruppe stand das bis hierher nirgends, und eine Variante ohne Speicher
    /// sah aus wie eine mit. Leer = nicht lesbar; dann bleibt die Zelle leer, statt
    /// etwas zu behaupten.</para>
    /// </summary>
    public string Speicher { get; set; } = "";

    /// <summary>
    /// Der Simulationszeitpunkt OHNE Kennzeichen („05.09.26 16:23"); leer = nie
    /// simuliert.
    ///
    /// <para><b>Warum neben <see cref="SimStand"/>.</b> Der fertige Zellentext der
    /// Tabellen trägt das „⚠" und im Fehlfall den Wortlaut „— (fehlt) ⚠" schon in
    /// sich — für eine ZEILE ist das richtig. Die Statuszeile der Übersichtsseite
    /// (Anwenderwunsch 05.09.2026, W5‑E‑1) setzt den Stand dagegen selbst zusammen:
    /// „Simulation: 05.09.26 16:23" mit dem Warnzeichen als eigenem Element (mit
    /// Kurztext) bzw. „noch nicht simuliert". Dafür braucht sie den reinen Wert —
    /// aus dem fertigen Text ließe er sich nur durch Raten zurückgewinnen.</para>
    /// </summary>
    public string SimZeitpunkt { get; set; } = "";

    /// <summary>Ist das die Stammzeile? Sie bleibt immer gewählt (Referenz).</summary>
    public bool IstStamm { get; set; }

    /// <summary>
    /// Kein oder veralteter Simulationsstand — der Vorläufer färbte die Zeile
    /// dafür ziegelrot (<c>ForeColor = Color.Firebrick</c>).
    /// </summary>
    public bool Auffaellig { get; set; }

    /// <summary>
    /// <b>Das Simulationsergebnis ist älter als die letzte Projektänderung</b>
    /// (<c>BerichtsDatenSammler.VariantenStatus.Veraltet</c>) — anders als
    /// <see cref="Auffaellig"/> NICHT zugleich der Fall „nie simuliert“.
    ///
    /// <para>Das Warnband der Wirtschaftlichkeitsseite hängt daran: „nie simuliert“
    /// sagt schon die Statuszeile („Noch keine Berechnung gespeichert“), „veraltet“
    /// sagte bis zum Anwenderbefund 22.09.2026 allein die Farbe einer Tabellenzelle —
    /// und eine Farbe ist kein Satz.</para>
    /// </summary>
    public bool Veraltet { get; set; }
}

/// <summary>Ein Berichtsbaustein zur Auswahl (Vorbild <c>BerichtsKonfiguration.BausteinDef</c>).</summary>
public sealed class BausteinZeile
{
    /// <summary>Sprachneutraler Schlüssel des Bausteins.</summary>
    public string Schluessel { get; set; } = "";

    /// <summary>Anzeigetitel.</summary>
    public string Titel { get; set; } = "";

    /// <summary>
    /// BV-E2 (Konzept Berichtsvorlagen 10.2, „Häkchen (BV-Q1 c)"): <b>Wirkt der Baustein auch auf
    /// die Excel-Mappe?</b> (<c>BerichtsKonfiguration.BausteinDef.NurWord</c> = <c>false</c>). Die
    /// Mappe entsteht bis BV-E7 wie heute (BV-Q2) und führt jeden solchen Baustein. Wird Excel mit
    /// ausgegeben, bleibt sein Häkchen deshalb wählbar, auch wenn die Word-Vorlage das Kapitel nicht
    /// führt — ausgegraut ist ein Eintrag nur, wenn weder Word- noch Excel-Vorlage Kapitel bzw. Blatt
    /// führt.
    /// </summary>
    public bool InExcel { get; set; }
}

/// <summary>
/// Der Anzeigestand der Berichtsseite — was der Vorläufer in
/// <c>UcBericht.LadeDaten</c> aus Konfiguration und Variantenstatus
/// zusammentrug, in einer Antwort.
/// </summary>
public sealed class BerichtStand
{
    /// <summary>Stamm und Varianten der Vergleichsgruppe, Stamm zuerst.</summary>
    public IReadOnlyList<VarianteZeile> Varianten { get; set; } = Array.Empty<VarianteZeile>();

    /// <summary>Die Ids der angehakten Versionen (der Stamm ist immer dabei).</summary>
    public IReadOnlyList<int> GewaehlteVarianten { get; set; } = Array.Empty<int>();

    /// <summary>Die wählbaren Berichtsbausteine in Anzeigereihenfolge.</summary>
    public IReadOnlyList<BausteinZeile> Bausteine { get; set; } = Array.Empty<BausteinZeile>();

    /// <summary>Die Schlüssel der aktiven Bausteine.</summary>
    public IReadOnlyList<string> AktiveBausteine { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Das gewählte Ausgabeformat: 0 = Word, 1 = Excel, 2 = beide. Die
    /// PERSISTENZWERTE („Word"/„Excel"/„Beide", eingefroren und deutsch)
    /// kennt nur die Hülle — die Komponente rechnet mit der Nummer
    /// (Drei-Schichten-Regel).
    /// </summary>
    public int AusgabeId { get; set; }

    /// <summary>Der Zielordner der Ausgabe.</summary>
    public string Zielordner { get; set; } = "";
}

/// <summary>Was die Seite beim Erstellen an die Hülle übergibt.</summary>
public sealed class BerichtAuftrag
{
    /// <summary>Die angehakten Versionen OHNE den Stamm (wie <c>BerichtsKonfiguration.VariantenIds</c>).</summary>
    public IReadOnlyList<int> VariantenIds { get; set; } = Array.Empty<int>();

    /// <summary>Die Schlüssel der aktiven Bausteine.</summary>
    public IReadOnlyList<string> Bausteine { get; set; } = Array.Empty<string>();

    /// <summary>0 = Word, 1 = Excel, 2 = beide.</summary>
    public int AusgabeId { get; set; }

    /// <summary>Der Zielordner.</summary>
    public string Zielordner { get; set; } = "";

    /// <summary>Die Zahl der angehakten Versionen inklusive Stamm (für die Rückfrage).</summary>
    public int AnzahlMitStamm { get; set; }

    /// <summary>
    /// BV-E1: die gewählte Word-Vorlage (<see cref="Vorlagenzeile.Id"/>) zum Zeitpunkt des
    /// Starts; <c>null</c> = die Seite führt keine Vorlagengruppe.
    /// </summary>
    public int? VorlageId { get; set; }

    /// <summary>
    /// BV-E1 (Konzept 10.2, BV-Q6): die Antwort der ERWEITERTEN Startrückfrage —
    /// <see cref="Startweg.Eigene"/> (mit der gewählten Vorlage, unbekannte Stellen gelb)
    /// oder <see cref="Startweg.Standard"/> (für diesen Lauf die Standardvorlage). Leer
    /// heißt: Es stand die heutige Startrückfrage (<c>BK_BER_FRAGE_START</c>), die gewählte
    /// Vorlage gilt. <see cref="Startweg.Abbruch"/> kommt hier nie an — dann gibt es keinen
    /// Lauf.
    /// </summary>
    public string Vorlagenweg { get; set; } = "";
}

// =====================================================================
//  BV-E1 — die Gruppe „Vorlage" der Berichtsseite (Konzept 10.2)
// =====================================================================

/// <summary>
/// Eine wählbare Word-Vorlage der Gruppe „Vorlage" (BV-E1, Konzept 10.2): „Standard
/// (EPOS-Plan)" und die eigenen Vorlagen des Vorlagenordners, je mit einer STABILEN Id
/// aus der Hülle — die Seite meldet die Id, nie den Text (Hausregel <c>Auswahlfeld</c>).
/// </summary>
/// <param name="Id">Die Id, die <c>VorlageIdChanged</c> meldet.</param>
/// <param name="Text">Anzeigetext im Auswahlfeld („Standard (EPOS-Plan)", „Kurzbericht Kunde").</param>
/// <param name="Gesperrt">
/// Nicht wählbar — eine gespeicherte Vorlage, deren Datei fehlt, bleibt so als Eintrag
/// stehen (Konzept 10.2: „nicht vorhanden – Standard verwendet").
/// </param>
/// <param name="GesperrtHinweis">Der Grund der Sperre; er steht am Eintrag und unter dem Feld.</param>
/// <param name="Mitgeliefert">
/// Eine Datei der Auslieferung (Standardvorlage): nur lesbar, neben dem Feld steht das
/// Schloss (<c>Kennzeichen</c>), geändert wird nur eine Kopie („Neue Vorlage…").
/// </param>
public sealed record Vorlagenzeile(int Id, string Text, bool Gesperrt = false,
                                   string GesperrtHinweis = "", bool Mitgeliefert = false);

/// <summary>
/// Ein Eintrag des Menüs „…" der Vorlagengruppe — als DATEN, nicht als Knopf (Hausregel
/// „kein Delegat, kein Knopf"). Welche Einträge es gibt, entscheidet die Hülle je Plattform
/// und Vorlage: Windows „In Word öffnen", „Im Ordner zeigen", „Ersetzen…", „Entfernen";
/// iOS „Teilen…", „Ersetzen…", „Entfernen"; eine mitgelieferte Vorlage nur
/// „Schreibgeschützt öffnen".
/// </summary>
/// <param name="Id">Sprachneutrale Kennung, die <c>HandlungGewaehlt</c> meldet.</param>
/// <param name="Text">Beschriftung des Eintrags.</param>
/// <param name="Aktiv">
/// <c>false</c> = WEICH gesperrt: Der Eintrag bleibt stehen, trägt <c>aria-disabled</c> und
/// <paramref name="Grund"/> als Kurztext, und ein Klick meldet den Grund statt zu handeln.
/// </param>
/// <param name="Grund">Warum der Eintrag gerade nicht geht.</param>
/// <param name="Kurztext">
/// Kurztext am freien Eintrag („Änderungen werden nicht gespeichert" bei „Schreibgeschützt
/// öffnen"); leer = keiner.
/// </param>
/// <param name="Rueckfrage">
/// Die Frage VOR der Handlung („Vorlage „…“ aus dem Vorlagenordner entfernen?") — gesetzt,
/// fragt die Seite mit dem Baustein <c>Rueckfrage</c> (Vorgabe „Nein") und meldet die Handlung
/// erst auf „Ja"; leer = keine.
/// </param>
public sealed record Handlung(string Id, string Text, bool Aktiv = true, string Grund = "",
                              string Kurztext = "", string Rueckfrage = "");

/// <summary>
/// Die Prüfzeile unter der Vorlagenwahl (Konzept 9.7: eine <c>Herleitungszeile</c> mit
/// Symbol, Text und optionalem „anzeigen", kein <c>Kennzeichen</c>).
/// </summary>
/// <param name="Symbol">Das Zeichen vor dem Text („✓", „⚠", „✖"); reine Dekoration, der Text trägt die Aussage.</param>
/// <param name="Text">„geprüft, 23 Platzhalter, keine Befunde" oder „2 unbekannte Platzhalter".</param>
/// <param name="HatBefunde">
/// Es gibt Meldungen — dann steht „anzeigen" dahinter und öffnet die Prüfliste
/// (<c>PrueflisteGaben</c>).
/// </param>
/// <param name="Knopftext">Beschriftung des Knopfes; leer = „anzeigen" aus dem Textbündel.</param>
public sealed record Pruefstand(string Symbol, string Text, bool HatBefunde = false, string Knopftext = "");

/// <summary>
/// BV-E2 (Konzept Berichtsvorlagen 10.2, „Häkchen (BV-Q1 c)"): <b>was die gewählte Word-Vorlage an
/// Kapiteln führt</b> — die Grundlage der Häkchenliste. Die Hülle leitet ihn aus der Schnellprüfung
/// ab (<c>Pruefbefund.Bausteine</c>, <c>Pruefbefund.HatKapitel</c>); die Seite verbindet ihn mit dem
/// Ausgabeformat: Ein Eintrag steht nur dann ausgegraut da („in dieser Vorlage nicht enthalten",
/// weich gesperrt, der Grund am Element), wenn Word entsteht, die Vorlage sein Kapitel nicht führt
/// und — wird Excel mit ausgegeben — auch die Mappe ihn nicht führt (<see cref="BausteinZeile.InExcel"/>).
/// Das Häkchen selbst bleibt gespeichert: Ein Vorlagenwechsel bringt es zurück.
/// </summary>
/// <param name="NichtEnthalten">
/// Die Bausteinschlüssel (<see cref="BausteinZeile.Schluessel"/>), deren Kapitel die Vorlage NICHT
/// führt; ein Schlüssel, der fehlt, ist frei.
/// </param>
/// <param name="InhaltAusVorlage">
/// Die Vorlage führt weder <c>{{bericht.inhalt}}</c> noch ein <c>kapitel.*</c> — nur
/// Einzelplatzhalter. Entsteht nur Word, steht statt der Liste die leise Zeile „Den Inhalt bestimmt
/// die Vorlage"; mit Excel bleibt die Liste, denn die Mappe folgt den Häkchen wie heute.
/// </param>
/// <param name="DeckblattAusVorlage">
/// Der Bausteinschlüssel des Häkchens „Deckblatt", wenn die Vorlage das Deckblatt selbst trägt — aus
/// Platzhaltern, ohne Kapitel Deckblatt (<c>Pruefbefund.DeckblattAusPlatzhaltern</c>); <c>null</c> =
/// nein. Der Eintrag steht ausgegraut wie ein nicht enthaltener, aber mit dem Grund „Deckblatt kommt aus
/// der Vorlage": Sein Häkchen wirkt nicht, die Vorlage trägt das Deckblatt unabhängig davon. Er steht
/// nicht zugleich in <paramref name="NichtEnthalten"/>.
/// </param>
public sealed record Kapitelstand(IReadOnlyList<string> NichtEnthalten, bool InhaltAusVorlage = false,
                                  string? DeckblattAusVorlage = null);

/// <summary>
/// Die ERWEITERTE Rückfrage vor „Erstellen" (Konzept 10.2, BV-Q6): Liefert die Hülle sie,
/// ersetzt sie die heutige Startrückfrage — mit Befunden und drei Wegen.
/// </summary>
/// <param name="Titel">Überschrift; leer = die der heutigen Rückfrage (<c>TitelErstellen</c>).</param>
/// <param name="Text">
/// Der Satz über den Befunden. Ein <c>{0}</c> darin wird durch die Zahl der angehakten
/// Versionen samt Stamm ersetzt (sonst bleibt der Text, wie er ist — geschweifte Klammern
/// eines Platzhalters werden nicht gedeutet).
/// </param>
/// <param name="Befunde">Je Befund eine Zeile (Name der Vorlage, unbekannte Stellen, Sprache, Sicht).</param>
/// <param name="WegEigene">„Mit meiner Vorlage" — unbekannte Stellen erscheinen gelb.</param>
/// <param name="WegStandard">„Mit Standardvorlage" — nur für diesen Lauf.</param>
/// <param name="WegAbbrechen">„Abbrechen".</param>
/// <param name="EigeneMoeglich">
/// <c>false</c> = die eigene Vorlage ist nicht füllbar (nicht lesbar, fehlt) — dann stehen
/// nur „Mit Standardvorlage" und „Abbrechen" da.
/// </param>
public sealed record Startrueckfrage(string Titel, string Text, IReadOnlyList<string> Befunde,
                                     string WegEigene, string WegStandard, string WegAbbrechen,
                                     bool EigeneMoeglich = true);

/// <summary>
/// Die drei Antworten der erweiterten Startrückfrage, wie sie <c>StartGewaehlt</c> meldet
/// und <see cref="BerichtAuftrag.Vorlagenweg"/> trägt.
/// </summary>
public static class Startweg
{
    /// <summary>Mit der gewählten (eigenen) Vorlage.</summary>
    public const string Eigene = "eigene";

    /// <summary>Für diesen Lauf mit der Standardvorlage.</summary>
    public const string Standard = "standard";

    /// <summary>Kein Lauf.</summary>
    public const string Abbruch = "abbruch";
}

/// <summary>
/// „Neue Vorlage…" mit Muster (Konzept Berichtsvorlagen 10.2: „Kopie der Standardvorlage oder des Kurzberichts"):
/// der Name aus dem Namensdialog und die Kennung des gewählten Musters aus <c>Vorlagenmuster</c>.
/// </summary>
/// <param name="Name">Der Name der neuen Vorlage — getrimmt, nie leer.</param>
/// <param name="Muster">Die Kennung des Musters, wie die Hülle sie in <c>Vorlagenmuster</c> vergeben hat.</param>
public sealed record Neuvorlage(string Name, int Muster);

/// <summary>
/// Der FRISCHE Stand der Vorlagengruppe, wie ihn <c>VorlagenNeuLaden</c> liefert — nach
/// jeder Handlung der Gruppe und unmittelbar vor der Startrückfrage (die Schnellprüfung
/// läuft vor jedem Start, Konzept 6.8).
///
/// <para><b>Warum es ihn gibt.</b> Die übrigen Parameter der Gruppe sind der ANFANGSSTAND:
/// Der Gabensatz lebt so lange wie die Seite, und ohne Nachladen stünde nach „Neue
/// Vorlage…" die alte Liste da — dasselbe Muster wie <c>ListeNeuLaden</c> der
/// Energieträgerverwaltung (ET-5).</para>
/// </summary>
public sealed record Vorlagenstand
{
    /// <summary>Die wählbaren Vorlagen.</summary>
    public IReadOnlyList<Vorlagenzeile> Vorlagen { get; init; } = Array.Empty<Vorlagenzeile>();

    /// <summary>Die gewählte Vorlage; <c>null</c> = keine.</summary>
    public int? VorlageId { get; init; }

    /// <summary>Die Einträge des Menüs „…" zur gewählten Vorlage.</summary>
    public IReadOnlyList<Handlung> Handlungen { get; init; } = Array.Empty<Handlung>();

    /// <summary>Die Prüfzeile; <c>null</c> = keine.</summary>
    public Pruefstand? Pruefzeile { get; init; }

    /// <summary>Die erweiterte Startrückfrage; <c>null</c> = die heutige gilt.</summary>
    public Startrueckfrage? Startrueckfrage { get; init; }

    /// <summary>
    /// BV-E2: was die gewählte Vorlage an Kapiteln führt — die Häkchenliste folgt ihm nach jedem
    /// Vorlagenwechsel; <c>null</c> = jeder Eintrag frei (keine Vorlage geprüft, nicht lesbar oder
    /// ohne Platzhalter).
    /// </summary>
    public Kapitelstand? Kapitelstand { get; init; }

    /// <summary>
    /// BV-E7 (Konzept 10.2, Zeile „Excel-Vorlage“): die wählbaren Excel-Vorlagen — „Ohne Vorlage (EPOS-Plan)“ und die
    /// eigenen <c>.xlsx</c>/<c>.xltx</c> des Vorlagenordners; leer = keine Zeile.
    /// </summary>
    public IReadOnlyList<Vorlagenzeile> ExcelVorlagen { get; init; } = Array.Empty<Vorlagenzeile>();

    /// <summary>BV-E7: die gewählte Excel-Vorlage; <c>null</c> = keine.</summary>
    public int? ExcelVorlageId { get; init; }

    /// <summary>BV-E7: die Prüfzeile der Excel-Vorlage; <c>null</c> = keine (etwa „ohne Vorlage“).</summary>
    public Pruefstand? ExcelPruefzeile { get; init; }

    /// <summary>Kurzmeldung zur letzten Handlung für die Statuszeile; leer = keine.</summary>
    public string Meldung { get; init; } = "";

    /// <summary>Fehler der letzten Handlung (Warnband in der Gruppe); leer = keiner.</summary>
    public string Fehler { get; init; } = "";
}

/// <summary>Fortschritt eines langen Laufs (Vorbild <c>BerichtsDatenSammler.Fortschritt</c>).</summary>
/// <param name="Aktuell">Erledigte Schritte.</param>
/// <param name="Gesamt">Schritte insgesamt; 0 = unbekannt.</param>
/// <param name="Text">Was gerade läuft.</param>
public sealed record Laufschritt(int Aktuell, int Gesamt, string Text);

/// <summary>
/// Das Ergebnis eines Laufs (Bericht oder Projektvergleich).
///
/// <para>Der Vorläufer zeigte an dieser Stelle eine MessageBox mit den Pfaden,
/// den Hinweisen und der Frage „öffnen?". Die Seite macht daraus eine
/// Statuszeile (<see cref="Statuszeile"/>), eine Meldung im Fenster
/// (<see cref="Meldung"/>) und — wenn <see cref="Frage"/> belegt ist — eine
/// <c>Rueckfrage</c>, deren Ja <see cref="Datei"/> öffnet.</para>
/// </summary>
public sealed class LaufErgebnis
{
    /// <summary>Der Lauf ist durchgelaufen.</summary>
    public bool Erfolg { get; set; }

    /// <summary>Der Anwender hat abgebrochen.</summary>
    public bool Abgebrochen { get; set; }

    /// <summary>Kurztext für die Statuszeile.</summary>
    public string Statuszeile { get; set; } = "";

    /// <summary>Mehrzeilige Meldung (Pfade, Hinweise) — leer = keine.</summary>
    public string Meldung { get; set; } = "";

    /// <summary>Die Frage „öffnen?" — leer = keine Rückfrage.</summary>
    public string Frage { get; set; } = "";

    /// <summary>Was ein „Ja" auf <see cref="Frage"/> öffnet.</summary>
    public string Datei { get; set; } = "";

    /// <summary>Fehlertext — belegt heißt: Warnbanner statt Meldung.</summary>
    public string Fehler { get; set; } = "";
}
