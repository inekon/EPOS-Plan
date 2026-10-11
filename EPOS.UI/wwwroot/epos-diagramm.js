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
// DER ZEIGER LAEUFT OHNE RUNDLAUF (Auftrag GX: "beim Zoomen ist der senkrechte
// Balken verlangsamt und nicht synchron zur Maus"). Frueher meldete das Modul die
// Stelle je Bildaufbau an .NET, und Blazor zeichnete Linie und Zeile neu - bei
// ×12 mit den roh nachgerechneten Pfaden ein Zeichenlauf ueber den ganzen Baum je
// Mausbewegung. Jetzt gibt der Baustein einmal je Zeichnen eine ZEIGERTAFEL
// (zeigertafel: Reihen, abgewaehlte Reihen, Einheiten, Datumsregel, Stufen der
// stehenden Ansicht). Den Balken setzt das Modul unmittelbar im pointermove, die
// Zeile rechnet es je Bildaufbau aus der Tafel - nach derselben Regel wie die
// Zeigerzeile des Bausteins. .NET erfaehrt die Stelle nur noch gedrosselt
// (MELDE_RUHE) und zeichnet dafuer nichts neu.
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

/** Hoechstens so oft meldet das Modul die Zeigerstelle an .NET, solange es die Zeile selbst fuehrt [ms]. */
const MELDE_RUHE = 100;

/** Die Tage der zwoelf Monate im Gemeinjahr (Feiertage.TageJeMonat des Kerns). */
const TAGE_JE_MONAT = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

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
        reihen: null,      // Zeigertafel: die Reihen des Modells (zeigertafel)
        zustand: null,     // Zeigertafel: abgewaehlte Reihen, Einheiten, Datumsregel, Stufen
        meldeUhr: 0,       // Drossel der Meldung an .NET, solange das Modul die Zeile fuehrt
        gemeldeteStelle: undefined,
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
        // Der BALKEN folgt sofort - im selben Ereignis, nicht erst im naechsten Bild.
        if (z.zustand) balkenSetzen(z, flaeche, e.clientX);
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

/**
 * DAS BEHAELTERMASS (DZ1-N2): Meldet der Komponente die Groesse der Flaeche in ganzen
 * Bildpunkten (MassGemeldet(breite, hoehe)) - beim Binden und nach jeder Groessenaenderung,
 * entprellt. Nur eine Flaeche, die ihre Groesse vom Behaelter nimmt (contain: size), wird
 * beobachtet: Sonst haengt ihre Hoehe am Seitenverhaeltnis des Modells, und ein Modell in
 * gemessener Groesse koennte sich selbst nachlaufen. Aenderungen unter 2 px zaehlen nicht.
 */
export function massBeobachten(flaeche, hilfe) {
    if (!flaeche || MASSE.has(flaeche) || typeof ResizeObserver === "undefined") return;
    const contain = getComputedStyle(flaeche).contain || "";
    if (!/\bsize\b|strict/.test(contain)) return;
    const b = { uhr: 0, breite: 0, hoehe: 0, beobachter: null };
    const melden = () => {
        b.uhr = 0;
        const breite = Math.floor(flaeche.clientWidth), hoehe = Math.floor(flaeche.clientHeight);
        if (breite < 1 || hoehe < 1) return;
        if (Math.abs(breite - b.breite) < 2 && Math.abs(hoehe - b.hoehe) < 2) return;
        b.breite = breite;
        b.hoehe = hoehe;
        try { hilfe.invokeMethodAsync("MassGemeldet", breite, hoehe); } catch (e) { /* Huelle ist weg */ }
    };
    b.beobachter = new ResizeObserver(() => {
        if (b.uhr) clearTimeout(b.uhr);
        b.uhr = setTimeout(melden, b.breite ? 120 : 0);
    });
    b.beobachter.observe(flaeche);
    MASSE.set(flaeche, b);
}

/** Beobachtete Flaechen des Behaeltermasses. */
const MASSE = new WeakMap();

/** Nimmt alle Handler wieder ab (die Komponente wird abgeraeumt). */
export function loesen(flaeche) {
    const m = MASSE.get(flaeche);
    if (m) {
        if (m.uhr) clearTimeout(m.uhr);
        m.beobachter.disconnect();
        MASSE.delete(flaeche);
    }
    const z = ZUSTAENDE.get(flaeche);
    if (!z) return;
    if (z.radUhr) clearTimeout(z.radUhr);
    if (z.zeigerRahmen) cancelAnimationFrame(z.zeigerRahmen);
    if (z.meldeUhr) clearTimeout(z.meldeUhr);
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

/**
 * DAS RECHTECK DER DATENFLAECHE in Fensterkoordinaten - ihr VIEWPORT, nicht die
 * Huelle ihres Inhalts (Auftrag GX). getBoundingClientRect() eines inneren svg
 * liefert in Chromium die Ausdehnung der GEZEICHNETEN Pfade: Bei x12 ragen sie
 * zwoelfmal ueber den Rahmen hinaus, das Rechteck war zwoelfmal zu breit - und die
 * Zeigerstelle lief zwoelfmal langsamer als die Maus ("der Balken ist verlangsamt").
 * Gerechnet wird deshalb aus x, y, Breite und Hoehe des inneren svg in den
 * Koordinaten des aeusseren und dessen Bildschirmmatrix.
 */
function flaechenRechteck(z) {
    const svg = z.svg;
    const aussen = svg ? svg.ownerSVGElement : null;
    const m = aussen && aussen.getScreenCTM ? aussen.getScreenCTM() : null;
    if (!m || !svg.x || !svg.width) return svg.getBoundingClientRect();
    const left = m.e + m.a * svg.x.baseVal.value;
    const top = m.f + m.d * svg.y.baseVal.value;
    const width = m.a * svg.width.baseVal.value;
    const height = m.d * svg.height.baseVal.value;
    return { left, top, width, height, right: left + width, bottom: top + height };
}

/** Der Anteil 0…1, an dem eine Fensterkoordinate ueber der Datenflaeche liegt. */
function svgAnteil(z, clientX) {
    const r = flaechenRechteck(z);
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
    const r = flaechenRechteck(z);
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
        if (z.zustand) {
            // DAS MODUL FUEHRT DIE ZEILE: schreiben, .NET nur gedrosselt unterrichten.
            zeileSchreiben(z, zeigerzeile(z, stelle));
            meldenGedrosselt(z);
            return;
        }
        if (!z.hilfe) return;
        try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", stelle); } catch (e) { /* Huelle ist weg */ }
    });
}

/** Meldet die Zeigerstelle hoechstens alle MELDE_RUHE Millisekunden an .NET. */
function meldenGedrosselt(z) {
    if (z.meldeUhr || !z.hilfe) return;
    z.meldeUhr = setTimeout(() => {
        z.meldeUhr = 0;
        if (z.zeigerStunde === z.gemeldeteStelle) return;
        z.gemeldeteStelle = z.zeigerStunde;
        try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", z.zeigerStunde); } catch (e) { /* Huelle ist weg */ }
    }, MELDE_RUHE);
}

/** Der Zeiger ist weg: die Zeigerzeile wird leer. */
function zeigerWeg(z) {
    z.zeigerX = null;
    if (z.zustand) {
        const balken = balkenElement(z);
        if (balken) balken.hidden = true;
        zeileSchreiben(z, "");
        if (z.meldeUhr) { clearTimeout(z.meldeUhr); z.meldeUhr = 0; }
    }
    if (z.zeigerStunde === null) return;
    z.zeigerStunde = null;
    z.gemeldeteStelle = null;
    if (!z.hilfe) return;
    try { z.hilfe.invokeMethodAsync("ZeigerGemeldet", null); } catch (e) { /* Huelle ist weg */ }
}

// =====================================================================
//  DIE ZEIGERTAFEL (Auftrag GX)
// =====================================================================

/**
 * Der Baustein gibt einmal je Zeichnen, was die Zeigerzeile braucht. Antwortet
 * das Modul mit true, fuehrt ES ab jetzt Balken und Zeile.
 *
 * @param {HTMLElement} flaeche der gebundene Rahmen
 * @param {Array|null} reihen die Reihen des Modells ({name, fenster, werte, unten,
 *        xWerte}); null = dieselben wie zuletzt
 * @param {object} zustand {versteckt, einheiten, kultur, wertachse, mass, jeStunde,
 *        monate, muster, stufen}
 * @returns {boolean} true, wenn die Tafel steht
 */
export function zeigertafel(flaeche, reihen, zustand) {
    const z = ZUSTAENDE.get(flaeche);
    if (!z || !zustand) return false;
    if (reihen) z.reihen = reihen;
    if (!z.reihen) return false;
    const kultur = zustand.kultur || "de-DE";
    try {
        z.zahl3 = new Intl.NumberFormat(kultur, { useGrouping: false, maximumFractionDigits: 3 });
        z.zahl0 = new Intl.NumberFormat(kultur, { maximumFractionDigits: 0 });
        z.zahl2 = new Intl.NumberFormat(kultur, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    } catch (e) {
        return false;
    }
    z.zustand = zustand;
    z.versteckt = new Set(zustand.versteckt || []);
    // Steht der Zeiger gerade ueber dem Bild, gilt die neue Tafel sofort.
    if (z.zeigerStunde !== null && z.zeigerStunde !== undefined)
        zeileSchreiben(z, zeigerzeile(z, z.zeigerStunde));
    return true;
}

function balkenElement(z) {
    const svg = z.svg;
    const rahmen = svg ? svg.closest(".epos-diagramm-svg-flaeche") : null;
    return rahmen ? rahmen.querySelector(".epos-diagramm-zeigerbalken") : null;
}

/** Setzt den Balken auf clientX, geklemmt an die Datenflaeche - ohne Rundlauf. */
function balkenSetzen(z, flaeche, clientX) {
    const balken = balkenElement(z);
    if (!balken || !z.svg) return;
    const fr = flaeche.getBoundingClientRect();
    const sr = flaechenRechteck(z);
    if (sr.width <= 0) return;
    const x = Math.min(sr.right, Math.max(sr.left, clientX));
    balken.style.left = (x - fr.left - flaeche.clientLeft) + "px";
    balken.style.top = (sr.top - fr.top - flaeche.clientTop) + "px";
    balken.style.height = sr.height + "px";
    balken.hidden = false;
}

function zeileSchreiben(z, text) {
    const rahmen = z.svg ? z.svg.closest(".epos-diagramm-svg-flaeche") : null;
    const wurzel = rahmen ? rahmen.parentElement : null;
    const zeile = wurzel ? wurzel.querySelector(".epos-diagramm-zeigerzeile--modul") : null;
    if (zeile && zeile.textContent !== text) zeile.textContent = text;
}

/** Rundung wie Math.Round in .NET: die Haelfte zur geraden Zahl. */
function rundeGerade(v) {
    const f = Math.floor(v);
    const d = v - f;
    if (d > 0.5) return f + 1;
    if (d < 0.5) return f;
    return f % 2 === 0 ? f : f + 1;
}

/** Datum des Gemeinjahres zur Jahresstunde (Zeitachse.Datum des Kerns). */
function datum(t, stunde) {
    let tag = Math.floor(klemmen(stunde) / 24);
    let monat = 0;
    while (monat < 11 && tag >= TAGE_JE_MONAT[monat]) { tag -= TAGE_JE_MONAT[monat]; monat++; }
    const name = t.monate && t.monate.length === 12 ? t.monate[monat] : String(monat + 1);
    return (t.muster || "{0}. {1}").replace("{0}", String(tag + 1)).replace("{1}", name);
}

/** Uhrzeit zur Jahresstunde (Zeitachse.Uhrzeit des Kerns). */
function uhrzeit(stunde) {
    const h = klemmen(stunde);
    let minuten = rundeGerade((h - Math.floor(h / 24) * 24) * 60);
    if (minuten >= 1440) minuten = 1439;
    const zwei = n => (n < 10 ? "0" : "") + n;
    return zwei(Math.floor(minuten / 60)) + ":" + zwei(minuten % 60);
}

function klemmen(stunde) {
    if (!(stunde >= 0)) return 0;
    return Math.min(stunde, 8760 - 1e-6);
}

/** Die Stelle auf der x-Achse (DiagrammSvg.Zeigerstelle). */
function zeigerstelle(z, x) {
    const t = z.zustand;
    const mass = t.mass || "";
    if (t.jeStunde > 0) {
        const stunde = rundeGerade(x) / t.jeStunde;
        const ganz = Math.abs(stunde - Math.round(stunde)) < 1e-9;
        const zahl = ganz ? z.zahl0.format(stunde) : z.zahl2.format(stunde);
        const kopf = mass.length > 0 ? zahl + " " + mass : zahl;
        return kopf + " · " + datum(t, stunde) + " " + uhrzeit(stunde);
    }
    const zahl = t.wertachse ? z.zahl3.format(x) : z.zahl0.format(rundeGerade(x));
    return mass.length > 0 ? zahl + " " + mass : zahl;
}

/** Der Index in werte zur Stelle x, -1 ausserhalb (DiagrammSvg.Reihenindex). */
function reihenindex(r, x) {
    if (!r.werte || r.werte.length === 0 || !r.fenster) return -1;
    const von = r.fenster[0], bis = r.fenster[1];
    if (x < von - 0.5 || x > bis + 0.5) return -1;
    const n = r.werte.length;
    if (r.xWerte && r.xWerte.length > 0) {
        let beste = -1, abstand = Number.MAX_VALUE;
        const m = Math.min(n, r.xWerte.length);
        for (let i = 0; i < m; i++) {
            if (r.xWerte[i] === null) continue;
            const d = Math.abs(r.xWerte[i] - x);
            if (d >= abstand) continue;
            abstand = d;
            beste = i;
        }
        return beste;
    }
    const schritt = n > 1 ? (bis - von) / (n - 1) : 0;
    let index = schritt > 0 ? rundeGerade((x - von) / schritt) : 0;
    if (index < 0) index = 0;
    if (index >= n) index = n - 1;
    return index;
}

/** Die ganze Zeile unter dem Zeiger (DiagrammSvg.Zeigerzeile). */
function zeigerzeile(z, stelle) {
    const t = z.zustand;
    if (!t || !z.reihen || stelle === null || stelle === undefined) return "";

    let x = stelle;
    let kopf = null;
    const st = t.stufen;
    if (st && st.grenzen && st.grenzen.length > 0) {
        let s = 0;
        while (s + 1 < st.grenzen.length && st.grenzen[s + 1] <= stelle) s++;
        x = st.stellen[s];
        kopf = st.koepfe[s];
    }
    const teile = [kopf !== null ? kopf : zeigerstelle(z, x)];

    const genannt = new Set();
    for (let i = 0; i < z.reihen.length; i++) {
        const r = z.reihen[i];
        const name = r.name || "";
        if (z.versteckt.has(name) || genannt.has(name)) continue;
        genannt.add(name);
        const index = reihenindex(r, x);
        if (index < 0) continue;
        let roh = r.werte[index];
        if (roh === null || roh === undefined || !isFinite(roh)) continue;
        if (r.unten && index < r.unten.length && r.unten[index] !== null && isFinite(r.unten[index]))
            roh -= r.unten[index];
        const einheit = t.einheiten ? (t.einheiten[i] || "") : "";
        const wert = z.zahl3.format(roh);
        teile.push(einheit.length > 0 ? name + ": " + wert + " " + einheit : name + ": " + wert);
    }
    return teile.join(" · ");
}
