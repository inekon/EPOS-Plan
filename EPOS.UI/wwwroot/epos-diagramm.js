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
// ZWEI MODI, DIESELBEN HANDLER (Konzept Diagramme, Etappe E2, Entscheid DG-Q4).
// binden(flaeche, hilfe, optionen) kennt seit E2 einen zweiten Modus:
//
//   TRANSFORM (Vorgabe, Baustein Diagramm) - das Bild ist ein PNG, vergroessert
//     wird es ueber einen CSS-Transform auf .epos-diagramm-inhalt. Ein
//     aufgezogenes Rechteck meldet ANTEILE des Bildes (BereichGemeldet), aus
//     denen der Kern das Bild neu zeichnet.
//
//   VIEWBOX  (optionen.modus === "viewbox", Baustein DiagrammSvg) - das Bild
//     ist ein SVG-Baum, und die Reihen liegen im inneren <svg class=
//     "epos-flaeche"> in DATENKOORDINATEN. Vergroessert wird dessen viewBox,
//     und zwar NUR auf der Zeitachse: x und Breite aendern sich, y und Hoehe
//     bleiben. Eine Attributaenderung, kein Neuzeichnen, kein Rundlauf - der
//     Pruefstand hat dafuer 0,1 ms je Schritt gemessen. Gemeldet wird der
//     sichtbare Ausschnitt in ganzen Stunden (FensterGemeldet) und die Stunde
//     unter dem Zeiger (ZeigerGemeldet).
//
// Es sind DIESELBEN Handler: Rad, Kneifgeste, Ziehen, Doppelklick, Tasten und
// Gummiband haengen einmal an der Flaeche und verzweigen erst dort, wo sie
// wirken. Damit gilt jede der drei WebView-Eigenheiten oben fuer beide Modi -
// besonders die Safari-Kneifgeste, die sonst die ganze Seite zoomte.
//
// DIE VOLLEN GRENZEN LIEST DER VIEWBOX-MODUS AUS data-voll am inneren <svg>,
// nicht aus einer beim Binden gemerkten Kopie: Wechselt das Modell (eine andere
// Region, eine andere Reihe), schreibt Blazor dort neue Grenzen - eine Kopie
// waere dann still veraltet, und Klemmung wie Zuruecksetzen liefen ins Leere.
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

/** Ruhe nach der letzten Radrast, ab der eine Radgeste als beendet gilt [ms]. */
const RAD_RUHE = 150;

/** Der Klassenname des inneren svg in Datenkoordinaten (SvgSchreiber.KLASSE_FLAECHE). */
const KLASSE_FLAECHE = "epos-flaeche";

/** Je Rahmen ein Zustand; der Rahmen haelt ihn, nicht dieses Modul. */
const ZUSTAENDE = new WeakMap();

/**
 * Haengt die Bedienung an eine Zeichenflaeche.
 *
 * @param {HTMLElement} flaeche der Rahmen mit overflow:hidden
 * @param {object} hilfe DotNetObjectReference auf die Komponente; sie fuehrt
 *        im Transform-Modus ZoomGemeldet(stufe) und BereichGemeldet(x0,x1,y0,y1),
 *        im viewBox-Modus ZoomGemeldet(stufe), FensterGemeldet(von, bis) und
 *        ZeigerGemeldet(stunde|null).
 * @param {object} [optionen] { modus: "viewbox" } schaltet auf die viewBox des
 *        inneren svg um; ohne Angabe bleibt es beim CSS-Transform.
 */
export function binden(flaeche, hilfe, optionen) {
    if (!flaeche || ZUSTAENDE.has(flaeche)) return;

    const viewbox = !!(optionen && optionen.modus === "viewbox");

    const z = {
        hilfe: hilfe,
        viewbox: viewbox,
        svg: viewbox ? flaeche.querySelector("svg." + KLASSE_FLAECHE) : null,
        inhalt: viewbox ? null : flaeche.querySelector(".epos-diagramm-inhalt"),
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
        fensterVon: null,  // zuletzt gemeldeter Ausschnitt (viewBox-Modus)
        fensterBis: null,
        radUhr: 0,         // Zeitgeber, der das Ende einer Radgeste feststellt
        zeigerStunde: null,// zuletzt gemeldete Zeigerstunde
        zeigerX: null,     // clientX, auf den der naechste Bildaufbau wartet
        zeigerRahmen: 0,   // laufende requestAnimationFrame-Anforderung
        handler: []
    };
    if (viewbox ? !z.svg : !z.inhalt) return;
    ZUSTAENDE.set(flaeche, z);

    // --- Rad: Zoom um den Zeiger. Ohne preventDefault rollt die Seite mit. ---
    an(flaeche, "wheel", e => {
        e.preventDefault();
        const schritt = e.ctrlKey ? KNEIF_SCHRITT : RAD_SCHRITT;
        const faktor = Math.exp(-e.deltaY * schritt);
        const p = punkt(flaeche, e.clientX, e.clientY);
        zoomeUm(z, flaeche, faktor, p.x, p.y, e.clientX);
        // Eine Radgeste hat kein Ende - sie hoert auf. Der Ausschnitt wird
        // deshalb erst nach einer kurzen Ruhe gemeldet, nicht je Rast.
        radEnde(z);
    }, { passive: false });

    // --- Zeiger nieder: entweder ein Rechteck aufziehen oder verschieben. ---
    an(flaeche, "pointerdown", e => {
        if (e.button !== 0 && e.pointerType === "mouse") return;
        // Der Fang haelt die Bewegung bei uns, auch wenn der Zeiger den Rahmen
        // verlaesst. Er kann fehlschlagen, wenn der Zeiger schon wieder weg ist -
        // dann geht es ohne ihn weiter.
        try { flaeche.setPointerCapture(e.pointerId); } catch (fehler) { /* ohne Fang */ }
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
            const b = z.viewbox ? kasten(z) : null;
            z.zugAb = {
                x: e.clientX, y: e.clientY, vx: z.vx, vy: z.vy,
                kx: b ? b.x : 0, kb: b ? b.b : 0
            };
            flaeche.classList.add("epos-diagramm--zieht");
        }
    });

    // --- Zeiger bewegt: Rechteck nachziehen, kneifen oder verschieben. ---
    an(flaeche, "pointermove", e => {
        // Die ZEIGERSTUNDE haengt nicht an einem gedrueckten Knopf: Sie meldet
        // sich bei jeder Bewegung ueber der Flaeche, hoechstens einmal je
        // Bildaufbau (requestAnimationFrame).
        if (z.viewbox) zeigerMerken(z, e.clientX);

        if (!z.zeiger.has(e.pointerId)) return;
        z.zeiger.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (z.zeiger.size === 2 && z.kneifAb > 0) {
            e.preventDefault();
            const jetzt = abstand(z.zeiger);
            const m = mitte(z.zeiger);
            const p = punkt(flaeche, m.x, m.y);
            setzeStufe(z, flaeche, z.kneifStufe * (jetzt / z.kneifAb), p.x, p.y, m.x);
            return;
        }

        if (z.gummiAb) {
            e.preventDefault();
            zeichneGummi(z, z.gummiAb, punkt(flaeche, e.clientX, e.clientY));
            return;
        }

        if (z.zugAb) {
            e.preventDefault();
            if (z.viewbox) { verschiebeViewbox(z, e.clientX); return; }
            z.vx = z.zugAb.vx + (e.clientX - z.zugAb.x);
            z.vy = z.zugAb.vy + (e.clientY - z.zugAb.y);
            male(z, flaeche);
        }
    }, { passive: false });

    // --- Der Zeiger verlaesst die Flaeche: die Zeigerzeile wird leer. ---
    an(flaeche, "pointerleave", () => { if (z.viewbox) zeigerWeg(z); });

    // --- Zeiger hoch: das Rechteck auswerten und melden. ---
    const beendet = e => {
        const zog = !!z.zugAb || z.kneifAb > 0;
        z.zeiger.delete(e.pointerId);
        if (z.zeiger.size < 2) z.kneifAb = 0;
        flaeche.classList.remove("epos-diagramm--zieht");
        z.zugAb = null;

        if (!z.gummiAb) {
            // ENDE EINER GESTE: erst jetzt meldet der viewBox-Modus seinen
            // Ausschnitt - waehrend des Zuges waere das ein Zeichenlauf je
            // Bildpunkt.
            if (z.viewbox && zog) meldeFenster(z);
            return;
        }
        const bis = punkt(flaeche, e.clientX, e.clientY);
        const ab = z.gummiAb;
        z.gummiAb = null;
        versteckeGummi(z);

        // Ein zu kleines Rechteck ist ein verrutschter Klick, kein Bereich.
        if (Math.abs(bis.x - ab.x) < RECHTECK_MIN || Math.abs(bis.y - ab.y) < RECHTECK_MIN) return;
        if (z.viewbox) gummiViewbox(z, flaeche, ab, bis);
        else meldeBereich(z, ab, bis);
    };
    an(flaeche, "pointerup", beendet);
    an(flaeche, "pointercancel", beendet);

    // --- Doppelklick: zurueck auf 1:1. Dieselbe Geste wie im Vorbild. ---
    an(flaeche, "dblclick", e => {
        e.preventDefault();
        stelleHer(flaeche, true);
    });

    // --- Tastatur: + groesser, - kleiner, 0 zurueck. ---
    an(flaeche, "keydown", e => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;
        const m = { x: flaeche.clientWidth / 2, y: flaeche.clientHeight / 2 };
        const mitteClient = flaeche.getBoundingClientRect().left + m.x;
        if (e.key === "+" || e.key === "=") {
            e.preventDefault();
            zoomeUm(z, flaeche, TASTE_FAKTOR, m.x, m.y, mitteClient);
            if (z.viewbox) meldeFenster(z);
        }
        else if (e.key === "-") {
            e.preventDefault();
            zoomeUm(z, flaeche, 1 / TASTE_FAKTOR, m.x, m.y, mitteClient);
            if (z.viewbox) meldeFenster(z);
        }
        else if (e.key === "0") { e.preventDefault(); stelleHer(flaeche, true); }
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
    if (z.radUhr) clearTimeout(z.radUhr);
    if (z.zeigerRahmen) cancelAnimationFrame(z.zeigerRahmen);
    for (const h of z.handler) flaeche.removeEventListener(h.name, h.fn, h.opt);
    ZUSTAENDE.delete(flaeche);
}

/**
 * Zurueck auf 1:1 - der Knopf „1:1" der Komponente ruft es, und die Komponente
 * ruft es auch, wenn ein ANDERES Modell eingesetzt wurde (andere Grenzen).
 * Gemeldet wird dabei NICHTS: Die Komponente weiss es ja schon.
 */
export function zuruecksetzen(flaeche) {
    stelleHer(flaeche, false);
}

/**
 * Der gemeinsame Rumpf des Zuruecksetzens.
 * @param {boolean} melden true fuer Doppelklick und Taste 0 - dort ist es eine
 *        GESTE des Anwenders, von der die Komponente noch nichts weiss.
 */
function stelleHer(flaeche, melden) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z) return;

    if (z.viewbox) {
        const v = vollKasten(z);
        setzeKasten(z, v.x, v.b);
        zeigerWeg(z);
        if (melden) meldeFenster(z);
        else { z.fensterVon = null; z.fensterBis = null; z.gemeldet = 1; }
        return;
    }

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

/**
 * Zoomt um den Faktor, wobei der Punkt (px, py) stehen bleibt.
 * @param {number} [clientX] Fensterkoordinate desselben Punktes - der
 *        viewBox-Modus rechnet gegen das innere svg, nicht gegen den Rahmen.
 */
function zoomeUm(z, flaeche, faktor, px, py, clientX) {
    if (z.viewbox) { zoomeViewbox(z, faktor, clientX); return; }
    setzeStufe(z, flaeche, z.stufe * faktor, px, py);
}

/** Setzt die Stufe absolut, wobei der Punkt (px, py) stehen bleibt. */
function setzeStufe(z, flaeche, neu, px, py, clientX) {
    if (z.viewbox) { setzeStufeViewbox(z, neu, clientX); return; }
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

// ============================================================ viewBox-Modus
//
// Hier steht die zweite Wirkung derselben Handler (Entscheid DG-Q4, Etappe E2).
// Veraendert wird AUSSCHLIESSLICH x und Breite der viewBox des inneren
// <svg class="epos-flaeche">; y und Hoehe bleiben, denn gezoomt wird nur die
// ZEITACHSE - die Werteachse bleibt voll, sonst verloere ein Leistungsbild
// seine Null (dieselbe Regel wie beim Achsenfenster des Renderers).
//
// Die Grenzen kommen aus data-voll am selben Element, nicht aus einer Kopie
// vom Binden: Blazor schreibt sie neu, sobald ein anderes Modell im Baum steht.

/** Die vier Zahlen eines viewBox-Textes; null, wenn er nicht lesbar ist. */
function vierZahlen(text) {
    if (!text) return null;
    const t = text.trim().split(/[\s,]+/).map(Number);
    if (t.length < 4 || t.some(w => !isFinite(w))) return null;
    return { x: t[0], y: t[1], b: t[2], h: t[3] };
}

/** Die VOLLEN Grenzen (data-voll), mit der aktuellen viewBox als Rueckfall. */
function vollKasten(z) {
    return vierZahlen(z.svg.getAttribute("data-voll"))
        || vierZahlen(z.svg.getAttribute("viewBox"))
        || { x: 0, y: 0, b: 1, h: 1 };
}

/** Die aktuelle viewBox; ohne lesbaren Wert die vollen Grenzen. */
function kasten(z) {
    return vierZahlen(z.svg.getAttribute("viewBox")) || vollKasten(z);
}

/**
 * Setzt x und Breite der viewBox (y und Hoehe bleiben, wie sie sind), klemmt
 * an die vollen Grenzen und meldet die ANGEZEIGTE Stufe, wenn sie sich aendert.
 */
function setzeKasten(z, x, breite) {
    const voll = vollKasten(z);
    const jetzt = kasten(z);

    const minBreite = voll.b / STUFE_MAX;
    let b = Math.min(voll.b, Math.max(minBreite, breite));
    let xx = Math.min(voll.x + voll.b - b, Math.max(voll.x, x));

    z.svg.setAttribute("viewBox",
        xx + " " + jetzt.y + " " + b + " " + jetzt.h);

    // Nur melden, wenn sich die ANGEZEIGTE Stufe aendert - sonst laeuft bei
    // jeder Radbewegung ein Zeichenlauf der Komponente mit.
    const grob = Math.round((voll.b / b) * 10) / 10;
    if (grob !== z.gemeldet && z.hilfe) {
        z.gemeldet = grob;
        try { z.hilfe.invokeMethodAsync("ZoomGemeldet", grob); } catch (e) { /* Huelle ist weg */ }
    }
}

/** Der Anteil 0…1, an dem eine Fensterkoordinate ueber der Datenflaeche liegt. */
function svgAnteil(z, clientX) {
    const r = z.svg.getBoundingClientRect();
    if (!r || r.width <= 0) return 0.5;
    return Math.min(1, Math.max(0, (clientX - r.left) / r.width));
}

/** Zoomt um den Faktor, wobei der Datenwert unter dem Zeiger stehen bleibt. */
function zoomeViewbox(z, faktor, clientX) {
    if (!(faktor > 0)) return;
    const b = kasten(z);
    setzeViewboxUm(z, b.b / faktor, clientX);
}

/** Setzt die Stufe absolut (Kneifgeste), Bezug ist wieder der Zeiger. */
function setzeStufeViewbox(z, stufe, clientX) {
    const voll = vollKasten(z);
    stufe = Math.min(STUFE_MAX, Math.max(STUFE_MIN, stufe));
    setzeViewboxUm(z, voll.b / stufe, clientX);
}

/** Neue Breite um den Punkt clientX herum - er behaelt seinen Datenwert. */
function setzeViewboxUm(z, breite, clientX) {
    const b = kasten(z);
    const anteil = clientX === undefined || clientX === null ? 0.5 : svgAnteil(z, clientX);
    const daten = b.x + anteil * b.b;
    setzeKasten(z, daten - anteil * breite, breite);
}

/** Verschieben: Der Datenwert unter dem Finger bleibt unter dem Finger. */
function verschiebeViewbox(z, clientX) {
    const r = z.svg.getBoundingClientRect();
    if (!r || r.width <= 0 || !z.zugAb) return;
    const jeBildpunkt = z.zugAb.kb / r.width;
    setzeKasten(z, z.zugAb.kx - (clientX - z.zugAb.x) * jeBildpunkt, z.zugAb.kb);
}

/**
 * Das aufgezogene Rechteck im viewBox-Modus: Die Oberflaeche SETZT den
 * Ausschnitt selbst - sie kennt die Datenkoordinaten, es gibt nichts
 * nachzuzeichnen. Gemeldet wird er danach als Fenster.
 */
function gummiViewbox(z, flaeche, ab, bis) {
    const r = flaeche.getBoundingClientRect();
    const b = kasten(z);
    const links = svgAnteil(z, r.left + Math.min(ab.x, bis.x));
    const rechts = svgAnteil(z, r.left + Math.max(ab.x, bis.x));
    if (rechts - links < 1e-6) return;

    const x0 = b.x + links * b.b;
    const x1 = b.x + rechts * b.b;
    setzeKasten(z, x0, x1 - x0);
    meldeFenster(z);
}

/** Meldet den sichtbaren Ausschnitt in GANZEN Stunden - nur bei Aenderung. */
function meldeFenster(z) {
    if (!z.hilfe) return;
    const b = kasten(z);
    const von = Math.round(b.x);
    const bis = Math.round(b.x + b.b);
    if (von === z.fensterVon && bis === z.fensterBis) return;
    z.fensterVon = von;
    z.fensterBis = bis;
    try { z.hilfe.invokeMethodAsync("FensterGemeldet", von, bis); } catch (e) { /* Huelle ist weg */ }
}

/** Eine Radgeste gilt nach RAD_RUHE Millisekunden Ruhe als beendet. */
function radEnde(z) {
    if (!z.viewbox) return;
    if (z.radUhr) clearTimeout(z.radUhr);
    z.radUhr = setTimeout(() => { z.radUhr = 0; meldeFenster(z); }, RAD_RUHE);
}

/**
 * Die Stunde unter dem Zeiger - hoechstens EINMAL je Bildaufbau. Die 2 px
 * duenne Linie trifft elementFromPoint fast nie (Pruefstand, Abschnitt 5);
 * gerechnet wird deshalb x -> viewBox -> Stunde, und den Wert liest die
 * Komponente aus dem Modell.
 */
function zeigerMerken(z, clientX) {
    z.zeigerX = clientX;
    if (z.zeigerRahmen) return;
    z.zeigerRahmen = requestAnimationFrame(() => {
        z.zeigerRahmen = 0;
        if (z.zeigerX === null) return;
        const b = kasten(z);
        const voll = vollKasten(z);
        let stunde = Math.round(b.x + svgAnteil(z, z.zeigerX) * b.b);
        stunde = Math.min(Math.round(voll.x + voll.b), Math.max(Math.round(voll.x), stunde));
        if (stunde === z.zeigerStunde) return;
        z.zeigerStunde = stunde;
        if (!z.hilfe) return;
        try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", stunde); } catch (e) { /* Huelle ist weg */ }
    });
}

/** Der Zeiger ist weg: die Zeigerzeile wird leer. */
function zeigerWeg(z) {
    z.zeigerX = null;
    if (z.zeigerStunde === null) return;
    z.zeigerStunde = null;
    if (!z.hilfe) return;
    try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", null); } catch (e) { /* Huelle ist weg */ }
}
