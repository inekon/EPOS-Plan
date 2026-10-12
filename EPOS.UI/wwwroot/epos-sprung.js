// Das Sprungziel eines Dialogs in Sicht (Konzept Kaeltebedarf 3.5, Sprungziele.Kuehlung).
//
// Die Kachel „Gebaeudekuehlung" oeffnet den Gebaeudeeditor an der Gruppe „Kuehlung": Die Gruppe rollt an den oberen
// Rand ihres Rollbehaelters, danach bekommt ihr erstes bedienbares Feld den Fokus - ohne ein zweites Rollen.
// Geladen ueber import() aus der Komponente; fehlt das Modul, bleibt der Dialog, wie er ist.

/** Rollt das Element in Sicht und fokussiert sein erstes bedienbares Feld. */
export function inSicht(element) {
    if (!element) return;
    element.scrollIntoView({ block: "start", behavior: "auto" });
    const feld = element.querySelector(
        "input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]):not([aria-disabled='true'])");
    if (feld) feld.focus({ preventScroll: true });
}
