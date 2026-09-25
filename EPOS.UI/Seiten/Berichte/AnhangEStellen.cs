using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// BV-E2 (Konzept Berichtsvorlagen 9.5, 11 Nr. 3) — <b>die Stellen der Anhang-E-Checkliste in der
/// gewählten Word-Vorlage</b>. Die Überlagerung „Anhang-E-Checkliste…" der Wirtschaftlichkeitsseite
/// nennt je Punkt die Stelle im Bericht, wie die Checkliste des Kerns sie aus den Kapiteln der Vorlage
/// baut — mit ihren Überschriften oder „nicht im Bericht", dazu das Blatt der Mappe. Die Hülle baut den
/// Satz (<c>BerichtsvorlagenGaben.AnhangEStellenDerVorlage</c>); die Komponente zeigt ihn nur an.
/// </summary>
/// <param name="Bezug">
/// Die leise Zeile der Überlagerung — worauf sich die Spalte „Stelle im Bericht" bezieht: „… bezogen
/// auf die Standardvorlage." oder „… nennen die Kapitel der Vorlage „Kurzbericht“.".
/// </param>
/// <param name="Stellen">
/// Je Punktnummer (<c>ChecklistenPunkt.Nummer</c>) die Stelle in der gewählten Vorlage; sie ersetzt die
/// eigene Stelle des Punktes. Leer (oder ein Punkt ohne Eintrag) = die eigene Stelle.
/// </param>
public sealed record AnhangEStellen(string Bezug, IReadOnlyDictionary<string, string> Stellen);
