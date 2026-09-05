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
}

/// <summary>Ein Berichtsbaustein zur Auswahl (Vorbild <c>BerichtsKonfiguration.BausteinDef</c>).</summary>
public sealed class BausteinZeile
{
    /// <summary>Sprachneutraler Schlüssel des Bausteins.</summary>
    public string Schluessel { get; set; } = "";

    /// <summary>Anzeigetitel.</summary>
    public string Titel { get; set; } = "";
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
