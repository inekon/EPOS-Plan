// Bis DG-E3-11 den Oberflaechen-Parameter Achsenart des Bausteins DiagrammSvg
// abloest, meint der Name Achsenart in den Tests die Aufzaehlung des Bausteins
// (EPOS.UI.Bausteine.Achsenart), nicht die des Kerns
// (WindowsFormsApplication1.Zeichnung.Achsenart), die seit Gruppe (b) der Etappe
// DG-E3 daneben steht. Ohne den Alias ist der Verweis in jeder Testdatei
// mehrdeutig, die beide Namensraeume oeffnet (CS0104). Das Gegenstueck fuer die
// Razor-Dateien liegt in EPOS.UI/Dialoge/_Imports.razor und EPOS.UI/Seiten/_Imports.razor.
global using Achsenart = EPOS.UI.Bausteine.Achsenart;
