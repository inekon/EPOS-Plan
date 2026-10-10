// Die Rollnachfuehrung der Projektliste (Baustein ProjektListe).
//
// WARUM ES DAS GIBT. "Speichern unter" oeffnet mit dem gerade offenen Projekt als
// Wahl. Steht es in der Mitte einer langen Liste, saehe der Anwender seine Wahl
// sonst nicht. C# kann den Rollbehaelter nicht stellen; dieses Modul rollt die
// gewaehlte Zeile an den oberen Rand des Rollbereichs - unter den stehenden
// Spaltenkopf, nicht dahinter.
//
// Gerollt wird NUR der Rollbehaelter der Liste (scrollTop), nie das Fenster:
// scrollIntoView rollte jeden Vorfahren mit (Hausregel "Ein Erstfokus rollt nicht").
// Wer das Modul nicht laden kann, sieht die Wahl trotzdem - nur rollt die Liste
// dann nicht (die Komponente faengt das ab). Kein Zustand, keine Ablage, kein Netz.

/**
 * Rollt die Zeile mit dem Index (Reihenfolge der gezeigten Zeilen) an den oberen
 * Rand des Rollbereichs. Reicht die Liste darunter nicht mehr, steht der Behaelter
 * am Ende - die Zeile ist dann sichtbar, nur nicht ganz oben.
 */
export function zeileOben(huelle, index) {
    if (!huelle || index < 0) return;

    const zeilen = huelle.querySelectorAll("tbody > tr");
    const zeile = zeilen[index];
    if (!zeile) return;

    const kopf = huelle.querySelector("thead");
    const kopfhoehe = kopf ? kopf.getBoundingClientRect().height : 0;
    const abstand = zeile.getBoundingClientRect().top - huelle.getBoundingClientRect().top;

    huelle.scrollTop = Math.max(0, huelle.scrollTop + abstand - kopfhoehe);
}
