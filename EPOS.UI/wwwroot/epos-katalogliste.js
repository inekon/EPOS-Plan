// Die Tastenfuehrung der Katalogliste (Konzept Administrationsdialoge, Stufe 2:
// V4 "Die Zeile ist die Wahl" und V11 "Tastaturfuehrung").
//
// WARUM ES DAS GIBT. Die Liste ist EIN Tabulatorhalt; Pfeil hoch/runter, Pos1
// und Ende bewegen die gewaehlte Zeile. Zwei Dinge kann C# dafuer nicht:
//
//   1. Den Browser daran hindern, dieselben Tasten zum ROLLEN zu nehmen - und
//      zwar NUR, wenn die Liste selbst den Fokus hat. Im Spaltenkopf steht das
//      Feld des Spaltenfilters, und das braucht Pos1 und Ende fuer seinen Text.
//      Blazors @onkeydown:preventDefault gilt fuer jede Taste und jedes Ziel -
//      es sperrte auch den Tabulator, und die Liste waere eine Falle.
//   2. Die gewaehlte Zeile in den sichtbaren Bereich rollen. In einer
//      virtualisierten Liste steht sie oft gar nicht im Baum; gerollt wird
//      deshalb nach derselben Rechnung, die Virtualize macht: Kopfhoehe plus
//      Index mal Zeilenmass. Das Mass ist GESETZT (--epos-rasterzeile), jede
//      Zeile ist genau so hoch.
//
// Wer das Modul nicht laden kann, waehlt trotzdem per Tastatur - nur rollt die
// Liste dann nicht mit (die Komponente faengt das ab). Kein Zustand, keine
// Ablage, kein Netz.

/** Die Tasten der Liste: vier bewegen die Wahl, die Leertaste setzt das Kaestchen der
 *  Fokuszeile (Stufe 3, V6) - und rollte sonst die Liste um eine Seite. */
const TASTEN = new Set(["ArrowUp", "ArrowDown", "Home", "End", " "]);

/**
 * Haengt den Tastenhalter an die Huelle der Liste - einmal je Element.
 * Er haelt die vier Tasten vom Rollen ab, wenn die Huelle SELBST das Ziel ist.
 */
export function anmelden(huelle) {
    if (!huelle || huelle.__eposTastenfuehrung) return;
    const halter = e => {
        if (e.target === huelle && TASTEN.has(e.key)) e.preventDefault();
    };
    huelle.addEventListener("keydown", halter);
    huelle.__eposTastenfuehrung = halter;
}

/**
 * Rollt die Zeile mit dem Index in den sichtbaren Bereich - unter den stehenden
 * Spaltenkopf, nicht dahinter. Steht sie schon ganz sichtbar, rollt nichts.
 */
export function zeileZeigen(huelle, index, zeilenmass) {
    if (!huelle || index < 0 || !(zeilenmass > 0)) return;

    const kopf = huelle.querySelector("thead");
    const kopfhoehe = kopf ? kopf.getBoundingClientRect().height : 0;

    const oben = kopfhoehe + index * zeilenmass;
    const unten = oben + zeilenmass;

    if (oben - kopfhoehe < huelle.scrollTop) {
        huelle.scrollTop = oben - kopfhoehe;
    } else if (unten > huelle.scrollTop + huelle.clientHeight) {
        huelle.scrollTop = unten - huelle.clientHeight;
    }
}
