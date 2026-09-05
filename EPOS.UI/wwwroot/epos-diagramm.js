// Der Zoom der Diagramme (Windows-Abnahme 05.09.2026, Befund A-1:
// "Allgemein bei Charts: das Zoomen funktioniert nicht").
//
// WARUM ES IHN GIBT. Im WinForms-Vorbild waren die Diagramme
// System.Windows.Forms.DataVisualization.Charting.Chart mit Achsenzoom - Rad,
// aufgezogenes Rechteck, Rollbalken, Zuruecksetzen. Seit iU7 zeichnet der Kern
// die Bilder selbst (ChartRenderer, SkiaSharp) und die Seiten zeigen ein PNG.
// Das Bild ist damit starr; W11b haelt das als Verlust fest (A-7, Risiko
// R-W11-5). Dieses Modul gibt den Zoom zurueck - nicht am Chart-Steuerelement,
// sondern am Bild: verschieben und vergroessern macht der Browser (CSS-Transform,
// kein Neuzeichnen), und WO der Anwender ein Rechteck aufzieht, meldet es die
// Komponente an den Kern, der das Bild mit diesem Achsenbereich NEU zeichnet.
//
// WARUM ALS MODUL. Geladen wird ueber import() aus der Komponente heraus, wie
// epos-verlauf.js. Damit braucht KEINE Wirtsseite eine <script>-Zeile - weder
// WindowsFormsApplication1/wwwroot/index.html noch EPOS.iOS/wwwroot/index.html.
// Wer die Datei nicht laden kann, sieht ein Bild ohne Zoom; das ist ein
// Schoenheitsfehler, kein Fehlschlag (die Komponente faengt es ab).
//
// ZWEI WEBVIEWS. Unter Windows laeuft WebView2 (Chromium), auf dem iPad
// WKWebView (Safari). Drei Stellen unterscheiden sich und stehen deshalb
// ausdruecklich hier:
//   * Safari meldet die Kneifgeste des Trackpads als "wheel" mit ctrlKey - das
//     ist derselbe Zoom, nur feiner; deshalb kein Sonderweg, nur ein
//     kleinerer Schritt.
//   * Safari kennt zusaetzlich gesturestart/gesturechange. Ohne
//     preventDefault zoomt die ganze SEITE statt des Bildes.
//   * touch-action: none steht im Stilblatt (Abschnitt Diagramm) - ohne das
//     nimmt der Browser den Finger fuer seinen eigenen Bildlauf, und
//     pointermove kommt nie an.
//
// MEHR STEHT HIER NICHT UND SOLL HIER NICHT STEHEN. Kein Zustand ueber die
// Sitzung hinaus, keine Ablage, kein Netz.

/** Kleinste Zoomstufe: 1 = das Bild in seiner Rahmenbreite. */
const STUFE_MIN = 1;

/** Groesste Zoomstufe - darueber sieht man nur noch Bildpunkte. */
const STUFE_MAX = 12;

/** Schrittweite eines Radrasts (Chromium meldet ~100 deltaY je Rast). */
const RAD_SCHRITT = 0.0016;

/** Schrittweite der Kneifgeste auf dem Trackpad (wheel mit ctrlKey). */
const KNEIF_SCHRITT = 0.012;

/** Faktor je Tastendruck (+ / -). */
const TASTE_FAKTOR = 1.25;

/** Ab dieser Kantenlaenge in Bildpunkten gilt ein Zug als Rechteck, nicht als Klick. */
const RECHTECK_MIN = 12;

/** Je Rahmen ein Zustand; der Rahmen haelt ihn, nicht dieses Modul. */
const ZUSTAENDE = new WeakMap();

/**
 * Haengt die Bedienung an eine Zeichenflaeche.
 *
 * @param {HTMLElement} flaeche der Rahmen mit overflow:hidden
 * @param {object} hilfe DotNetObjectReference auf die Komponente; sie fuehrt
 *        ZoomGemeldet(stufe) und BereichGemeldet(x0, x1, y0, y1).
 */
export function binden(flaeche, hilfe) {
    if (!flaeche || ZUSTAENDE.has(flaeche)) return;

    const z = {
        hilfe: hilfe,
        inhalt: flaeche.querySelector(".epos-diagramm-inhalt"),
        gummi: flaeche.querySelector(".epos-diagramm-gummi"),
        stufe: 1,          // Vergroesserung
        vx: 0, vy: 0,      // Verschiebung in Bildpunkten des Rahmens
        bereichsmodus: false,
        zeiger: new Map(), // laufende Beruehrungen/Zeiger je pointerId
        zugAb: null,       // Startpunkt eines Verschiebens
        gummiAb: null,     // Startpunkt eines Rechtecks
        kneifAb: 0,        // Fingerabstand beim Beginn der Kneifgeste
        kneifStufe: 1,     // Zoomstufe beim Beginn der Kneifgeste
        gemeldet: 1,       // zuletzt an .NET gemeldete Stufe
        handler: []
    };
    if (!z.inhalt) return;
    ZUSTAENDE.set(flaeche, z);

    // --- Rad: Zoom um den Zeiger. Ohne preventDefault rollt die Seite mit. ---
    an(flaeche, "wheel", e => {
        e.preventDefault();
        const schritt = e.ctrlKey ? KNEIF_SCHRITT : RAD_SCHRITT;
        const faktor = Math.exp(-e.deltaY * schritt);
        const p = punkt(flaeche, e.clientX, e.clientY);
        zoomeUm(z, flaeche, faktor, p.x, p.y);
    }, { passive: false });

    // --- Zeiger nieder: entweder ein Rechteck aufziehen oder verschieben. ---
    an(flaeche, "pointerdown", e => {
        if (e.button !== 0 && e.pointerType === "mouse") return;
        flaeche.setPointerCapture(e.pointerId);
        z.zeiger.set(e.pointerId, { x: e.clientX, y: e.clientY });

        // Zwei Finger: Kneifen. Der Abstand beim Aufsetzen ist der Bezug.
        if (z.zeiger.size === 2) {
            z.zugAb = null;
            z.gummiAb = null;
            z.kneifAb = abstand(z.zeiger);
            z.kneifStufe = z.stufe;
            return;
        }

        const p = punkt(flaeche, e.clientX, e.clientY);
        if (rechteckGewollt(z, e)) {
            z.gummiAb = p;
            zeichneGummi(z, p, p);
        } else {
            z.zugAb = { x: e.clientX, y: e.clientY, vx: z.vx, vy: z.vy };
            flaeche.classList.add("epos-diagramm--zieht");
        }
    });

    // --- Zeiger bewegt: Rechteck nachziehen, kneifen oder verschieben. ---
    an(flaeche, "pointermove", e => {
        if (!z.zeiger.has(e.pointerId)) return;
        z.zeiger.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (z.zeiger.size === 2 && z.kneifAb > 0) {
            e.preventDefault();
            const jetzt = abstand(z.zeiger);
            const m = mitte(z.zeiger);
            const p = punkt(flaeche, m.x, m.y);
            setzeStufe(z, flaeche, z.kneifStufe * (jetzt / z.kneifAb), p.x, p.y);
            return;
        }

        if (z.gummiAb) {
            e.preventDefault();
            zeichneGummi(z, z.gummiAb, punkt(flaeche, e.clientX, e.clientY));
            return;
        }

        if (z.zugAb) {
            e.preventDefault();
            z.vx = z.zugAb.vx + (e.clientX - z.zugAb.x);
            z.vy = z.zugAb.vy + (e.clientY - z.zugAb.y);
            male(z, flaeche);
        }
    }, { passive: false });

    // --- Zeiger hoch: das Rechteck auswerten und melden. ---
    const beendet = e => {
        z.zeiger.delete(e.pointerId);
        if (z.zeiger.size < 2) z.kneifAb = 0;
        flaeche.classList.remove("epos-diagramm--zieht");
        z.zugAb = null;

        if (!z.gummiAb) return;
        const bis = punkt(flaeche, e.clientX, e.clientY);
        const ab = z.gummiAb;
        z.gummiAb = null;
        versteckeGummi(z);

        // Ein zu kleines Rechteck ist ein verrutschter Klick, kein Bereich.
        if (Math.abs(bis.x - ab.x) < RECHTECK_MIN || Math.abs(bis.y - ab.y) < RECHTECK_MIN) return;
        meldeBereich(z, ab, bis);
    };
    an(flaeche, "pointerup", beendet);
    an(flaeche, "pointercancel", beendet);

    // --- Doppelklick: zurueck auf 1:1. Dieselbe Geste wie im Vorbild. ---
    an(flaeche, "dblclick", e => {
        e.preventDefault();
        zuruecksetzen(flaeche);
    });

    // --- Tastatur: + groesser, - kleiner, 0 zurueck. ---
    an(flaeche, "keydown", e => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;
        const m = { x: flaeche.clientWidth / 2, y: flaeche.clientHeight / 2 };
        if (e.key === "+" || e.key === "=") { e.preventDefault(); zoomeUm(z, flaeche, TASTE_FAKTOR, m.x, m.y); }
        else if (e.key === "-") { e.preventDefault(); zoomeUm(z, flaeche, 1 / TASTE_FAKTOR, m.x, m.y); }
        else if (e.key === "0") { e.preventDefault(); zuruecksetzen(flaeche); }
    });

    // --- Safari: ohne das zoomt die SEITE statt des Bildes. ---
    for (const name of ["gesturestart", "gesturechange", "gestureend"]) {
        an(flaeche, name, e => e.preventDefault(), { passive: false });
    }

    // --- Ein Bild ist kein Ziehgut; der Standardzug des Browsers stoert nur. ---
    an(flaeche, "dragstart", e => e.preventDefault());
}

/** Nimmt alle Handler wieder ab (die Komponente wird abgeraeumt). */
export function loesen(flaeche) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z) return;
    for (const h of z.handler) flaeche.removeEventListener(h.name, h.fn, h.opt);
    ZUSTAENDE.delete(flaeche);
}

/** Zurueck auf 1:1 - der Knopf, der Doppelklick und die Taste 0 rufen es. */
export function zuruecksetzen(flaeche) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z) return;
    z.stufe = 1; z.vx = 0; z.vy = 0;
    male(z, flaeche);
}

/** Schaltet das Aufziehen eines Rechtecks ein oder aus (Knopf "Bereich"). */
export function bereichsmodus(flaeche, an_) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z) return;
    z.bereichsmodus = !!an_;
    flaeche.classList.toggle("epos-diagramm--bereich", z.bereichsmodus);
}

// ---------------------------------------------------------------- Innenleben

/** Handler anhaengen UND merken - loesen() braucht dieselbe Funktion wieder. */
function an(el, name, fn, opt) {
    const z = ZUSTAENDE.get(el);
    el.addEventListener(name, fn, opt);
    if (z) z.handler.push({ name: name, fn: fn, opt: opt });
}

/** Ein Fensterpunkt in Bildpunkten des Rahmens. */
function punkt(flaeche, x, y) {
    const r = flaeche.getBoundingClientRect();
    return { x: x - r.left, y: y - r.top };
}

/** Der Abstand zweier Beruehrungen (Kneifgeste). */
function abstand(zeiger) {
    const p = [...zeiger.values()];
    return Math.hypot(p[0].x - p[1].x, p[0].y - p[1].y);
}

/** Die Mitte zwischen zwei Beruehrungen. */
function mitte(zeiger) {
    const p = [...zeiger.values()];
    return { x: (p[0].x + p[1].x) / 2, y: (p[0].y + p[1].y) / 2 };
}

/**
 * Soll dieser Zug ein Rechteck aufziehen? Mit der Maus die Umschalttaste,
 * sonst der Knopf "Bereich" - auf dem iPad gibt es keine Umschalttaste.
 */
function rechteckGewollt(z, e) {
    return z.bereichsmodus || (e.shiftKey && e.pointerType === "mouse");
}

/** Zoomt um den Faktor, wobei der Punkt (px, py) stehen bleibt. */
function zoomeUm(z, flaeche, faktor, px, py) {
    setzeStufe(z, flaeche, z.stufe * faktor, px, py);
}

/** Setzt die Stufe absolut, wobei der Punkt (px, py) stehen bleibt. */
function setzeStufe(z, flaeche, neu, px, py) {
    neu = Math.min(STUFE_MAX, Math.max(STUFE_MIN, neu));
    if (neu === z.stufe) return;
    // Der Punkt unter dem Zeiger behaelt seine Lage: Erst zurueckrechnen, wo er
    // im ungezoomten Bild liegt, dann mit der neuen Stufe wieder dorthin.
    z.vx = px - (px - z.vx) * neu / z.stufe;
    z.vy = py - (py - z.vy) * neu / z.stufe;
    z.stufe = neu;
    male(z, flaeche);
}

/**
 * Setzt die Verschiebung um und meldet die Stufe. Das Bild bleibt dabei im
 * Rahmen: Bei Stufe 1 sitzt es buendig, darueber darf kein Rand frei werden.
 */
function male(z, flaeche) {
    const bw = flaeche.clientWidth;
    const bh = flaeche.clientHeight;
    const iw = z.inhalt.offsetWidth * z.stufe;
    const ih = z.inhalt.offsetHeight * z.stufe;

    z.vx = iw <= bw ? (bw - iw) / 2 : Math.min(0, Math.max(bw - iw, z.vx));
    z.vy = ih <= bh ? (bh - ih) / 2 : Math.min(0, Math.max(bh - ih, z.vy));

    z.inhalt.style.transform =
        "translate(" + z.vx + "px, " + z.vy + "px) scale(" + z.stufe + ")";
    flaeche.classList.toggle("epos-diagramm--gezoomt", z.stufe > 1.001);

    // Nur melden, wenn sich die ANGEZEIGTE Stufe aendert - sonst laeuft bei
    // jeder Radbewegung ein Zeichenlauf der Komponente mit.
    const grob = Math.round(z.stufe * 10) / 10;
    if (grob !== z.gemeldet && z.hilfe) {
        z.gemeldet = grob;
        try { z.hilfe.invokeMethodAsync("ZoomGemeldet", grob); } catch (e) { /* Huelle ist weg */ }
    }
}

/** Zeichnet das aufgezogene Rechteck. */
function zeichneGummi(z, ab, bis) {
    if (!z.gummi) return;
    z.gummi.style.left = Math.min(ab.x, bis.x) + "px";
    z.gummi.style.top = Math.min(ab.y, bis.y) + "px";
    z.gummi.style.width = Math.abs(bis.x - ab.x) + "px";
    z.gummi.style.height = Math.abs(bis.y - ab.y) + "px";
    z.gummi.hidden = false;
}

function versteckeGummi(z) {
    if (z.gummi) z.gummi.hidden = true;
}

/**
 * Rechnet das Rechteck in ANTEILE DES BILDES um (0…1, linke obere Ecke zuerst)
 * und meldet sie. Der Kern macht daraus die Achsenbereiche - die Oberflaeche
 * kennt weder Stunden noch Kilowatt.
 */
function meldeBereich(z, ab, bis) {
    const iw = z.inhalt.offsetWidth * z.stufe;
    const ih = z.inhalt.offsetHeight * z.stufe;
    if (iw <= 0 || ih <= 0 || !z.hilfe) return;

    const anteil = p => ({
        x: Math.min(1, Math.max(0, (p.x - z.vx) / iw)),
        y: Math.min(1, Math.max(0, (p.y - z.vy) / ih))
    });
    const a = anteil(ab);
    const b = anteil(bis);

    try {
        z.hilfe.invokeMethodAsync("BereichGemeldet",
            Math.min(a.x, b.x), Math.max(a.x, b.x),
            Math.min(a.y, b.y), Math.max(a.y, b.y));
    } catch (e) { /* Huelle ist weg */ }
}
