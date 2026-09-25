using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// BV-E2 (Konzept Berichtsvorlagen 9.5, 11 Nr. 3) — <b>die Stellen der Anhang-E-Checkliste in der
/// gewählten Word-Vorlage</b>. Die Überlagerung „Anhang-E-Checkliste…" der Wirtschaftlichkeitsseite
/// nennt je Punkt die Stelle im Bericht; mit einer eigenen Vorlage sind das die Überschriften ihrer
/// Kapitel (oder „nicht im Bericht"), sonst die Stelle der Standardvorlage mit dem Zusatz „bezogen auf
/// die Standardvorlage". Die Hülle baut den Satz (<c>BerichtsvorlagenGaben.AnhangEStellenDerVorlage</c>);
/// die Komponente zeigt ihn nur an.
/// </summary>
/// <param name="Bezug">
/// Die leise Zeile der Überlagerung — worauf sich die Spalte „Stelle im Bericht" bezieht: „… bezogen
/// auf die Standardvorlage." oder „… nennen die Kapitel der Vorlage „Kurzbericht“ …".
/// </param>
/// <param name="Stellen">
/// Je Punktnummer (<c>ChecklistenPunkt.Nummer</c>) die Stelle in der gewählten Vorlage; unter ihr steht
/// leise die Stelle der Standardvorlage. Leer (oder ein Punkt ohne Eintrag) = die Stelle der
/// Standardvorlage allein.
/// </param>
public sealed record AnhangEStellen(string Bezug, IReadOnlyDictionary<string, string> Stellen);
