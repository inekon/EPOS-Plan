// Der einzige JavaScript-Anteil von EPOS.UI (iU9-W15b.6).
//
// WARUM ES IHN GIBT. Ein Gespraechsverlauf muss ans Ende springen, wenn eine
// Antwort dazukommt - und genau DANN NICHT, wenn der Anwender gerade weiter oben
// nachliest (Entscheid E-12). Beides braucht die gemessene Bildlaufstellung des
// Elements, und die gibt es in C# nicht. Der Vorlaeufer rief ScrollToCaret() und
// sprang IMMER (Form_KiChat.cs:1606); die Ruecksicht auf den nachlesenden
// Anwender ist die eine bewusste Verbesserung des Bausteins.
//
// WARUM ALS MODUL. Geladen wird ueber import() aus der Komponente heraus. Damit
// braucht KEINE Wirtsseite eine <script>-Zeile - weder die Windows-Huelle
// (WindowsFormsApplication1/wwwroot/index.html) noch die iOS-Huelle. Wer die
// Datei nicht laden kann, bekommt einen Verlauf ohne Nachfuehrung; das ist ein
// Schoenheitsfehler, kein Fehlschlag (die Komponente faengt das ab).
//
// MEHR STEHT HIER NICHT UND SOLL HIER NICHT STEHEN. Kein Zustand, keine Ablage,
// kein Netz - der Verlauf ist personenbezogen (Regel S-3).

/** Abstand zum unteren Rand, ab dem "der Anwender steht unten" noch gilt (px). */
const SPIELRAUM = 40;

/**
 * Steht der Anwender am unteren Ende der Liste?
 * Ein leeres oder noch nicht gemessenes Element gilt als "unten" - beim ersten
 * Zeichnen soll nachgefuehrt werden.
 */
export function istUnten(element) {
    if (!element) return true;
    return element.scrollHeight - element.scrollTop - element.clientHeight < SPIELRAUM;
}

/** Springt ans Ende der Liste. */
export function ansEnde(element) {
    if (!element) return;
    element.scrollTop = element.scrollHeight;
}
