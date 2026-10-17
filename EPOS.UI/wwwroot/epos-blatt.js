// Der Rollstand beim BLATTWECHSEL (Baustein Blattwechsel, Nachtrag N35).
//
// WARUM ES DAS GIBT. Ein Blatt tauscht den Inhalt seines Wirtsdialogs, der Rollbehaelter
// bleibt derselbe: In der Ueberlagerung "Brauchwasser..." rollt die Ueberlagerung selbst,
// im eigenen Fenster die Seite. Dessen Rollstand gehoert dem Browser und ueberlebt den
// Tausch - das Blatt stuende dann dort, wo der Wirt zuletzt stand (gemessen: 124 px
// gerollt, die Kopfzeile des Blattes ueber dem Rand), und der Rueckweg landete an
// irgendeiner Stelle des Wirts. C# kennt den Rollstand eines Vorfahren nicht.
//
// Der Baustein merkt sich den Stand VOR dem Tausch, rollt das Blatt nach oben und stellt
// den Stand beim Rueckweg wieder her. Kein Zustand hier, keine Ablage, kein Netz.
// Wer das Modul nicht laden kann, hat ein Blatt ohne Nachfuehrung - ein Schoenheitsfehler,
// kein Fehlschlag (der Baustein faengt das ab).

/**
 * Der naechste Vorfahr, der senkrecht rollt (overflow-y auto/scroll) - sonst die Seite.
 * Gemessen wird nur der eingestellte Wert, nicht ob gerade etwas zu rollen ist: Der
 * Wirt kann beim Tausch kuerzer sein als das Blatt.
 */
function rollvorfahr(element) {
    for (let e = element ? element.parentElement : null; e; e = e.parentElement) {
        const y = getComputedStyle(e).overflowY;
        if (y === "auto" || y === "scroll" || y === "overlay") return e;
    }
    return document.scrollingElement;
}

/** Der Rollstand des Rollvorfahren von element in px; 0, wenn es keinen gibt. */
export function rollstand(element) {
    const v = rollvorfahr(element);
    return v ? v.scrollTop : 0;
}

/** Setzt den Rollstand des Rollvorfahren von element. */
export function stelleRollstand(element, stand) {
    const v = rollvorfahr(element);
    if (v) v.scrollTop = stand;
}
