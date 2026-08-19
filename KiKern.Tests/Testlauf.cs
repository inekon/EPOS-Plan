using Xunit;

// ---------------------------------------------------------------------------------------
// Die Testfaelle dieses Projekts laufen NACHEINANDER, nicht nebenlaeufig.
//
// Grund: zwei Dinge des Kerns sind zwangslaeufig prozessweit und werden von Tests gesetzt -
//   * KiTexte.Lieferant (der Textlieferant, Fachkonzept 3.7) und
//   * CultureInfo.CurrentCulture (Kulturregel, Fachkonzept 3.2).
// Liefen die Testklassen parallel, saehe eine Klasse den Lieferanten oder die Kultur einer
// anderen - der Fehler waere sporadisch und der Befund wertlos. Die Laufzeit spielt keine
// Rolle: das ganze Projekt braucht deutlich unter einer Sekunde.
// ---------------------------------------------------------------------------------------
[assembly: CollectionBehavior(DisableTestParallelization = true)]
