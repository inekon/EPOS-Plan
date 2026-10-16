// Die Hervorhebung des Energieflussbilds (Konzept Energieflussbild 3.3, Schritt SK-2d-2).
//
// WAS ES TUT. Beim Ueberfahren oder Antippen eines Knotens bleibt sein Umfeld voll - alle Baender des Knotens und
// von jedem Nachbarn, der kein Bus ist, rekursiv in derselben Richtung weiter; ein Bus beendet den Weg, sein Band
// zaehlt noch; ist der Knoten selbst ein Bus, nur seine eigenen Baender -, dazu die Knoten an diesen Baendern. Ein
// Band hebt sich selbst hervor; die beiden Stummel eines Verbinders tragen dieselbe Marke und leuchten gemeinsam.
// Der Rest wird abgeblendet: die figure traegt die Klasse epos-ergebnis-fluss-bild--hervor, die hervorgehobenen
// Elemente epos-fluss-an, das Stilblatt blendet ab. Stufentitel, Legende und Hinweis bleiben unberuehrt.
//
// WOHER. Die Kanten und Busse stehen als JSON an der figure (data-kanten: je Band [Marke, Von, Nach];
// data-busse: Kennungen) - aus den UNGEFILTERTEN Bilddaten der Huelle. Die Marke wird NIE zerlegt (Kennungen tragen
// selbst Doppelpunkte, etwa "verlust:puffer:3:0"); ein gefiltertes Bild ist eine Teilmenge, eine Marke ohne Element
// wird uebergangen. Referenz der Regel: EnergieflussAbbildung.Umfeld im Kern, gegen sie misst diagrammprobe.mjs.
//
// WARUM EIN EIGENES MODUL. Der Baustein DiagrammSvg und sein Modul epos-diagramm.js bleiben unberuehrt (Vertrag 3.3:
// keine Aenderung an Baustein, Schreiber, Maler oder Wache); die Zeigerzeile des Bausteins zeigt den Wert wie bisher.
// Hier haengt nur eine Ereignisdelegation an der figure - kein Rundlauf nach .NET, nichts im Bericht. Fehlt das Modul,
// steht das Bild ohne Hervorhebung da.

const HERVOR = "epos-ergebnis-fluss-bild--hervor";
const AN = "epos-fluss-an";
const ELEMENTE = '[data-marke^="fluss:"], [data-marke^="knoten:"]';

/** Bindet die Hervorhebung an die figure des Bildes - einmal je Element. */
export function binden(figur) {
    if (!figur || figur.__eposFluss) return;
    figur.__eposFluss = { marke: null, quelle: null, graph: null };
    figur.addEventListener("pointerover", e => beiZeiger(figur, e.target));
    // BERUEHRUNG: Antippen hebt hervor, Antippen daneben hebt auf; die Hervorhebung bleibt nach dem Loslassen stehen.
    figur.addEventListener("pointerdown", e => beiZeiger(figur, e.target));
    figur.addEventListener("pointerleave", e => { if (e.pointerType === "mouse") aufheben(figur); });
}

/** Hebt jede Hervorhebung auf (neues Bild, Maus verlaesst das Bild). */
export function aufheben(figur) {
    if (!figur) return;
    figur.classList.remove(HERVOR);
    for (const el of figur.querySelectorAll("." + AN)) el.classList.remove(AN);
    if (figur.__eposFluss) figur.__eposFluss.marke = null;
}

/** Die Marken des Umfelds eines Knotens (dieselbe Regel wie EnergieflussAbbildung.Umfeld); exportiert fuer die Probe. */
export function umfeld(figur, kennung) {
    const g = graph(figur);
    const marken = [];
    if (!g.knoten.has(kennung)) return marken;
    const gesehen = new Set();
    const merke = k => { if (!gesehen.has(k[0])) { gesehen.add(k[0]); marken.push(k[0]); } };
    if (g.busse.has(kennung)) {
        for (const k of g.kanten) if (k[1] === kennung || k[2] === kennung) merke(k);
        return marken;
    }
    const besucht = new Set();
    const lauf = (id, ein) => {
        const schluessel = (ein ? "<" : ">") + id;
        if (besucht.has(schluessel)) return;
        besucht.add(schluessel);
        for (const k of g.kanten) {
            if ((ein ? k[2] : k[1]) !== id) continue;
            merke(k);
            const nachbar = ein ? k[1] : k[2];
            if (!g.busse.has(nachbar)) lauf(nachbar, ein);
        }
    };
    lauf(kennung, true);
    lauf(kennung, false);
    return marken;
}

function graph(figur) {
    const z = figur.__eposFluss || (figur.__eposFluss = { marke: null, quelle: null, graph: null });
    const quelle = (figur.getAttribute("data-kanten") || "[]") + "\u0000" + (figur.getAttribute("data-busse") || "[]");
    if (z.quelle === quelle && z.graph) return z.graph;
    let kanten = [], busse = [];
    try { kanten = JSON.parse(figur.getAttribute("data-kanten") || "[]"); } catch (f) { kanten = []; }
    try { busse = JSON.parse(figur.getAttribute("data-busse") || "[]"); } catch (f) { busse = []; }
    kanten = kanten.filter(k => Array.isArray(k) && k.length === 3 && k[1] !== k[2]);
    const knoten = new Set();
    for (const k of kanten) { knoten.add(k[1]); knoten.add(k[2]); }
    z.quelle = quelle;
    z.graph = { kanten, busse: new Set(busse), knoten };
    return z.graph;
}

function beiZeiger(figur, ziel) {
    const el = ziel && ziel.closest ? ziel.closest("[data-marke]") : null;
    const marke = el && figur.contains(el) ? el.getAttribute("data-marke") : null;
    if (!marke || !(marke.startsWith("fluss:") || marke.startsWith("knoten:"))) { aufheben(figur); return; }
    const z = figur.__eposFluss;
    if (z.marke === marke && figur.classList.contains(HERVOR)) return;
    z.marke = marke;

    const an = new Set();
    if (marke.startsWith("knoten:")) {
        const kennung = marke.substring("knoten:".length);
        an.add(marke);
        const g = graph(figur);
        for (const m of umfeld(figur, kennung)) {
            an.add(m);
            const k = g.kanten.find(x => x[0] === m);
            if (k) { an.add("knoten:" + k[1]); an.add("knoten:" + k[2]); }
        }
    } else {
        an.add(marke);
        const k = graph(figur).kanten.find(x => x[0] === marke);
        if (k) { an.add("knoten:" + k[1]); an.add("knoten:" + k[2]); }
    }
    for (const e of figur.querySelectorAll(ELEMENTE)) e.classList.toggle(AN, an.has(e.getAttribute("data-marke")));
    figur.classList.add(HERVOR);
}
