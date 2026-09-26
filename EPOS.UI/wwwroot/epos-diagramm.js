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
// GEZOOMT WIRD DIE viewBox DES INNEREN svg (Konzept Diagramme, Etappe E2,
// Entscheid DG-Q4). Das Bild ist ein SVG-Baum, und die Reihen liegen im inneren
// <svg class="epos-flaeche"> in DATENKOORDINATEN. Vergroessert wird dessen
// viewBox, und zwar NUR auf der Zeitachse: x und Breite aendern sich, y und
// Hoehe bleiben - sonst verloere ein Leistungsbild seine Null. Eine
// Attributaenderung, kein Neuzeichnen, kein Rundlauf; der Pruefstand hat dafuer
// 0,1 ms je Schritt gemessen. Gemeldet wird der sichtbare Ausschnitt
// (FensterGemeldet) und die Stelle unter dem Zeiger (ZeigerGemeldet).
//
// EINEN ZWEITEN MODUS GIBT ES NICHT MEHR. Bis zum Abschluss der Etappe E3 kannte
// dieses Modul daneben einen CSS-Transform auf einem PNG - fuer den Baustein
// Diagramm, der ein Pixelbild vergroesserte und ein aufgezogenes Rechteck als
// ANTEILE meldete, aus denen der Kern das Bild neu zeichnete. Mit der letzten
// umgestellten Bildstelle ist dieser Weg samt seinem Baustein entfallen; was
// bleibt, ist ein Modul, das genau eine Sache tut.
//
// DIE VOLLEN GRENZEN LIEST DAS MODUL AUS data-voll am inneren <svg>,
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
 *        ZoomGemeldet(stufe), FensterGemeldet(von, bis) und
 *        ZeigerGemeldet(stelle|null).
 */
export function binden(flaeche, hilfe) {
    if (!flaeche || ZUSTAENDE.has(flaeche)) return;

    const z = {
        hilfe: hilfe,
        // DAS INNERE svg WIRD JE ZUGRIFF NACHGESCHLAGEN, NICHT BEIM BINDEN GEMERKT.
        // Blazor vergleicht die Kinder des Bildes nach ihrer Stelle: Aendert sich
        // die Zahl der Knoten VOR der Datenflaeche (eine Reihe mehr oder weniger,
        // eine andere y-Teilung nach einem neuen Lauf), steht an der Stelle des
        // inneren svg vorher ein anderes Element - Blazor setzt ein NEUES svg ein.
        // Eine beim Binden gemerkte Kopie zeigte dann auf ein Element ausserhalb
        // des DOM: Rad, Ziehen und Rechteck veraenderten ein unsichtbares Bild,
        // und "das Chart laesst sich nicht zoomen" (Befund 26.09.2026).
        _svg: flaeche.querySelector("svg." + KLASSE_FLAECHE),
        get svg() {
            if (!this._svg || !this._svg.isConnected || !flaeche.contains(this._svg)) {
                const neu = flaeche.querySelector("svg." + KLASSE_FLAECHE);
                if (neu) this._svg = neu;
            }
            return this._svg;
        },
        kastenGesetzt: null, // zuletzt GESETZTER Ausschnitt { x, b } - nachziehen() stellt ihn wieder her
        gummi: flaeche.querySelector(".epos-diagramm-gummi"),
        stufe: 1,          // Vergroesserung (nur als Bezug der Kneifgeste)
        bereichsmodus: false,
        zeiger: new Map(), // laufende Beruehrungen/Zeiger je pointerId
        zugAb: null,       // Startpunkt eines Verschiebens
        gummiAb: null,     // Startpunkt eines Rechtecks
        kneifAb: 0,        // Fingerabstand beim Beginn der Kneifgeste
        kneifStufe: 1,     // Zoomstufe beim Beginn der Kneifgeste
        gemeldet: 1,       // zuletzt an .NET gemeldete Stufe
        fensterVon: null,  // zuletzt gemeldeter Ausschnitt
        fensterBis: null,
        radUhr: 0,         // Zeitgeber, der das Ende einer Radgeste feststellt
        zeigerStunde: null,// zuletzt gemeldete Zeigerstelle
        zeigerX: null,     // clientX, auf den der naechste Bildaufbau wartet
        zeigerRahmen: 0,   // laufende requestAnimationFrame-Anforderung
        handler: []
    };
    if (!z.svg) return;
    ZUSTAENDE.set(flaeche, z);

    // --- Rad: Zoom um den Zeiger. Ohne preventDefault rollt die Seite mit. ---
    an(flaeche, "wheel", e => {
        if (gehoertDemBaustein(e.target)) return;   // ueber dem Farbwaehler rollt die Seite
        e.preventDefault();
        const schritt = e.ctrlKey ? KNEIF_SCHRITT : RAD_SCHRITT;
        const faktor = Math.exp(-e.deltaY * schritt);
        zoomeViewbox(z, faktor, e.clientX);
        // Eine Radgeste hat kein Ende - sie hoert auf. Der Ausschnitt wird
        // deshalb erst nach einer kurzen Ruhe gemeldet, nicht je Rast.
        radEnde(z);
    }, { passive: false });

    // --- Zeiger nieder: entweder ein Rechteck aufziehen oder verschieben. ---
    an(flaeche, "pointerdown", e => {
        if (e.button !== 0 && e.pointerType === "mouse") return;
        // BAUSTEINEIGENES: Geht der Zeiger auf einem Legendeneintrag, im
        // Farbwaehler oder auf einem Formularelement nieder, gehoert er dem
        // Baustein (Blazor-Klick). Hier beginnt dann keine Geste, und vor allem
        // KEIN FANG (siehe gehoertDemBaustein).
        if (gehoertDemBaustein(e.target)) return;
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
            z.kneifStufe = stufeJetzt(z);
            return;
        }

        const p = punkt(flaeche, e.clientX, e.clientY);
        if (rechteckGewollt(z, e)) {
            z.gummiAb = p;
            zeichneGummi(z, p, p);
        } else {
            const b = kasten(z);
            z.zugAb = { x: e.clientX, kx: b.x, kb: b.b };
            flaeche.classList.add("epos-diagramm--zieht");
        }
    });

    // --- Zeiger bewegt: Rechteck nachziehen, kneifen oder verschieben. ---
    an(flaeche, "pointermove", e => {
        // Die ZEIGERSTELLE haengt nicht an einem gedrueckten Knopf: Sie meldet
        // sich bei jeder Bewegung ueber der Flaeche, hoechstens einmal je
        // Bildaufbau (requestAnimationFrame).
        zeigerMerken(z, e.clientX);

        if (!z.zeiger.has(e.pointerId)) return;
        z.zeiger.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (z.zeiger.size === 2 && z.kneifAb > 0) {
            e.preventDefault();
            const jetzt = abstand(z.zeiger);
            const m = mitte(z.zeiger);
            setzeStufeViewbox(z, z.kneifStufe * (jetzt / z.kneifAb), m.x);
            return;
        }

        if (z.gummiAb) {
            e.preventDefault();
            zeichneGummi(z, z.gummiAb, punkt(flaeche, e.clientX, e.clientY));
            return;
        }

        if (z.zugAb) {
            e.preventDefault();
            verschiebeViewbox(z, e.clientX);
        }
    }, { passive: false });

    // --- Der Zeiger verlaesst die Flaeche: die Zeigerzeile wird leer. ---
    an(flaeche, "pointerleave", () => zeigerWeg(z));

    // --- Zeiger hoch: das Rechteck auswerten und melden. ---
    const beendet = e => {
        const zog = !!z.zugAb || z.kneifAb > 0;
        z.zeiger.delete(e.pointerId);
        if (z.zeiger.size < 2) z.kneifAb = 0;
        flaeche.classList.remove("epos-diagramm--zieht");
        z.zugAb = null;

        if (!z.gummiAb) {
            // ENDE EINER GESTE: erst jetzt wird der Ausschnitt gemeldet -
            // waehrend des Zuges waere das ein Zeichenlauf je Bildpunkt.
            if (zog) meldeFenster(z);
            return;
        }
        const bis = punkt(flaeche, e.clientX, e.clientY);
        const ab = z.gummiAb;
        z.gummiAb = null;
        versteckeGummi(z);

        // Ein zu kleines Rechteck ist ein verrutschter Klick, kein Bereich.
        if (Math.abs(bis.x - ab.x) < RECHTECK_MIN || Math.abs(bis.y - ab.y) < RECHTECK_MIN) return;
        gummiViewbox(z, flaeche, ab, bis);
    };
    an(flaeche, "pointerup", beendet);
    an(flaeche, "pointercancel", beendet);

    // --- Doppelklick: zurueck auf 1:1. Dieselbe Geste wie im Vorbild. ---
    an(flaeche, "dblclick", e => {
        if (gehoertDemBaustein(e.target)) return;
        e.preventDefault();
        stelleHer(flaeche, true);
    });

    // --- Tastatur: + groesser, - kleiner, 0 zurueck. ---
    an(flaeche, "keydown", e => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;
        if (gehoertDemBaustein(e.target)) return;   // "0" im Hexfeld ist eine Ziffer, kein Befehl
        // Die Tasten zoomen um die MITTE der Flaeche - dort steht kein Zeiger,
        // auf den sich das Bild beziehen koennte.
        const mitteClient = flaeche.getBoundingClientRect().left + flaeche.clientWidth / 2;
        if (e.key === "+" || e.key === "=") {
            e.preventDefault();
            zoomeViewbox(z, TASTE_FAKTOR, mitteClient);
            meldeFenster(z);
        }
        else if (e.key === "-") {
            e.preventDefault();
            zoomeViewbox(z, 1 / TASTE_FAKTOR, mitteClient);
            meldeFenster(z);
        }
        else if (e.key === "0") { e.preventDefault(); stelleHer(flaeche, true); }
    });

    // --- Safari: ohne das zoomt die SEITE statt des Bildes. ---
    for (const name of ["gesturestart", "gesturechange", "gestureend"]) {
        an(flaeche, name, e => e.preventDefault(), { passive: false });
    }

    // --- Ein Bild ist kein Ziehgut; der Standardzug des Browsers stoert nur. ---
    an(flaeche, "dragstart", e => e.preventDefault());

    // --- Ziehen markiert keinen Text (Befund 26.09.2026, Gebaeudedialog). ---
    // Ein Zug ueber die Flaeche beginnt sonst eine Textauswahl ueber Titel,
    // Legende und Achsen des SVG. user-select: none im Stilblatt ist der erste
    // Weg; dieser Handler der zweite, fuer eine WebView, die die Regel an
    // SVG-Text nicht haelt. Das pointerdown bleibt OHNE preventDefault - sonst
    // bekaeme die Flaeche keinen Fokus mehr, und die Tasten + - 0 liefen ins Leere.
    // Das Hexfeld des Farbwaehlers gehoert dem Baustein und bleibt markierbar.
    an(flaeche, "selectstart", e => {
        if (gehoertDemBaustein(e.target)) return;
        e.preventDefault();
    });
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

    const v = vollKasten(z);
    setzeKasten(z, v.x, v.b);
    zeigerWeg(z);
    if (melden) meldeFenster(z);
    else { z.fensterVon = null; z.fensterBis = null; z.gemeldet = 1; }
}

/**
 * Zieht den Ausschnitt nach, nachdem die Komponente eine neue Instanz DESSELBEN
 * Bildes gezeichnet hat (DG-E3-15: der Zoom bleibt stehen). Zwei Faelle schreiben
 * dabei die viewBox der Datenflaeche auf die volle Breite, ohne dass das Modul es
 * merkt: Blazor setzt ein NEUES inneres svg ein (andere Knotenzahl davor), oder
 * es schreibt die viewBox des alten neu (andere y-Spanne nach einem neuen Lauf).
 * Dann stuende das Bild voll da, waehrend Leiste und Achsenteilung den Ausschnitt
 * zeigen. Hier wird der zuletzt gesetzte Ausschnitt (x und Breite) wieder
 * aufgelegt; y und Hoehe bleiben die des neuen Bildes. Gemeldet wird nichts.
 */
export function nachziehen(flaeche) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z || !z.svg || !z.kastenGesetzt) return;
    const jetzt = kasten(z);
    if (jetzt.x === z.kastenGesetzt.x && jetzt.b === z.kastenGesetzt.b) return;
    setzeKasten(z, z.kastenGesetzt.x, z.kastenGesetzt.b);
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
// Was der Baustein DiagrammSvg selbst bedient - Legendeneintraege (Text und
// Farbfeld mit data-marke="legende:..."), der Farbwaehler samt seiner
// Schliessflaeche und jedes Formularelement -, laesst das Modul in Ruhe: kein
// Fang, keine Geste, kein Zoom, keine Taste. Mit Fang wanderte das Ziel des
// click-Ereignisses auf die Flaeche, und ein Legendeneintrag oder ein Knopf im
// Waehler bekaeme seinen Klick nie (gemessen 20.09.2026 im Wirt, Chromium).
const BAUSTEIN_EIGEN = '[data-marke^="legende:"], .epos-farbwahl, .epos-farbwahl-schliessflaeche, button, input, select, textarea, label';
function gehoertDemBaustein(el) {
    return !!(el && el.closest && el.closest(BAUSTEIN_EIGEN));
}

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

/** Die Vergroesserung, die die aktuelle viewBox bedeutet. */
function stufeJetzt(z) {
    const voll = vollKasten(z);
    const b = kasten(z);
    return b.b > 0 ? voll.b / b.b : 1;
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

// ============================================================ Die viewBox
//
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
    z.kastenGesetzt = { x: xx, b: b };

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
    if (z.radUhr) clearTimeout(z.radUhr);
    z.radUhr = setTimeout(() => { z.radUhr = 0; meldeFenster(z); }, RAD_RUHE);
}

/**
 * Die STELLE auf der x-Achse unter dem Zeiger - hoechstens EINMAL je
 * Bildaufbau. Die 2 px duenne Linie trifft elementFromPoint fast nie
 * (Pruefstand, Abschnitt 5); gerechnet wird deshalb x -> viewBox -> Stelle,
 * und den Wert liest die Komponente aus dem Modell.
 *
 * DG-E3-11: Gemeldet wird eine GLEITKOMMAZAHL. Die Achse zaehlt nicht mehr
 * immer Stunden - auf einer C-Raten-Achse von 0,1 bis 2,0 fiele eine
 * ganzzahlige Stelle mit dem ganzen Bild zusammen. Gerundet wird auf ein
 * Tausendstel der SICHTBAREN Breite: feiner als ein Bildpunkt, und zugleich
 * weniger Meldungen als die frueheren ganzen Stunden eines Jahres (8 760 auf
 * rund 1 000 Bildpunkte).
 */
function zeigerMerken(z, clientX) {
    z.zeigerX = clientX;
    if (z.zeigerRahmen) return;
    z.zeigerRahmen = requestAnimationFrame(() => {
        z.zeigerRahmen = 0;
        if (z.zeigerX === null) return;
        const b = kasten(z);
        const voll = vollKasten(z);
        let stelle = b.x + svgAnteil(z, z.zeigerX) * b.b;
        stelle = Math.min(voll.x + voll.b, Math.max(voll.x, stelle));
        const schritt = b.b / 1000;
        if (schritt > 0) stelle = Math.round(stelle / schritt) * schritt;
        if (stelle === z.zeigerStunde) return;
        z.zeigerStunde = stelle;
        if (!z.hilfe) return;
        try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", stelle); } catch (e) { /* Huelle ist weg */ }
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
