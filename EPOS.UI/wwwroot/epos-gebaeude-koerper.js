// Die KOERPERANSICHT eines Gebaeudes (Baustein GebaeudeAnsicht, Reiter "Koerper"; Gebaeudesimulation
// G7b, Entscheid E66; Datenaustauschkonzept 14.2 und 14.4).
//
// WAS ES ZEIGT. Je Raum sein Grundrisspolygon um die Raumhoehe extrudiert, in der Farbe seiner Zone
// (dieselben Token --epos-grundriss-zone-0 ... -9 wie der Grundriss, nach der STELLE der Zone), ohne
// Zone grau. Eine Platte je Bauteil an Kante, Boden und Decke - nur, wo die Daten ein Bauteil kennen,
// sonst der nackte Koerper. Ein schematischer Koerper (Umriss erfunden oder Hoehe als Vorgabe) ist
// durchscheinender und gestrichelt umrandet; die Kennzeichnung in Worten steht im HTML daneben (Razor).
// Dazu ein Raster am Boden und ein Nordpfeil. Keine Anzeigetexte hier.
//
// ZWEI DARSTELLUNGEN (Datenaustauschkonzept 15.4; daten.modus). "exportmodell" ist die Ansicht oben.
// "dateikoerper" zeichnet je Raum mit Dateikoerper das Dreiecksnetz der Datei als BufferGeometry: die Punkte
// (float32, relativ zum Bezugspunkt daten.bezugspunkt) und Indizes (int32) stehen in EINEM Bytefeld je Gebaeude
// (Uint8Array, Little-Endian), das Verzeichnis daten.dateikoerper nennt je Raum die Byte-Offsets und Zahlen. Das
// Netz liegt relativ, das Mesh steht am Bezugspunkt - so bleiben auch georeferenzierte Koordinaten um 10^6 m auf
// den Zentimeter genau. Material beidseitig (offene Netze bleiben sichtbar), flach schattiert; die Linien sind
// die Randkanten der Datei als LineSegments (keine EdgesGeometry - die zeigte die Triangulationsdiagonalen).
// Raeume ohne Dateikoerper stehen als Prisma aus dem Umriss da, ohne Platten. aktualisieren() nimmt einen
// Wechsel der Darstellung entgegen; Modul und Kamera bleiben.
//
// KOORDINATEN. Die Daten sind Meter: x nach Osten, y nach Norden, z nach oben. three.js rechnet mit
// y nach oben; abgebildet wird (x, y, z) -> (x, z, -y), Norden zeigt also nach -z.
//
// BEDIENUNG. OrbitControls (ziehen dreht, rechts verschiebt, Rad zoomt). Ein Klick ohne Ziehen trifft
// ueber den Raycaster einen Koerper und meldet dem Baustein Zonenschluessel und Raumkennung
// (rueckruf.invokeMethodAsync('ZoneGewaehlt', zone, raum)) - Kennungen, nie ein Name.
//
// DETERMINISTISCH. Keine Zufallsfarben, keine Animation: gezeichnet wird bei Aenderung (Kamera,
// Groesse, Daten). Kann die Umgebung kein WebGL, wirft erzeugen() nicht, sondern gibt false zurueck -
// der Baustein meldet das benannt statt eines leeren Bildes.

import * as THREE from './three/three.module.js';
import { OrbitControls } from './three/OrbitControls.js';

const PALETTE = 10;
const KLICK_PX = 5;
const PLATTE_ABSTAND = 0.02;

// Plattenfarben je Bauteilart (sprachneutrale Schluessel der Daten).
const PLATTENFARBE = {
    Aussenwand: 0xb8b4aa, Innenwand: 0xd8d5ce, Fenster: 0x8ecae6, Vorhangfassade: 0x8ecae6,
    Tuer: 0xa47148, Dach: 0x8d7b68, Decke: 0xc9c5bc, Bodenplatte: 0x7a756c, Sonstiges: 0xcfcac0,
};
const DURCHSICHTIG = { Fenster: true, Vorhangfassade: true };

let zustand = null;

/** Die Farbe eines Tokens am Element (CSS-Variable), sonst der Rueckfall. */
function token(element, name, rueckfall) {
    const wert = getComputedStyle(element).getPropertyValue(name).trim();
    return wert || rueckfall;
}

function farbe(element, stelle) {
    if (stelle === null || stelle === undefined || stelle < 0) return token(element, '--epos-grundriss-ohnezone', '#d9d8d2');
    return token(element, '--epos-grundriss-zone-' + (Math.abs(stelle) % PALETTE), '#cccccc');
}

/** Ein Punkt der Daten (x Ost, y Nord, z oben) im Raum von three.js. */
function v3(x, y, z) { return new THREE.Vector3(x, z, -y); }

function entsorgeGruppe(gruppe) {
    gruppe.traverse(o => {
        if (o.geometry) o.geometry.dispose();
        if (o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach(m => m.dispose());
    });
    gruppe.clear();
}

/** Ein Viereck als Platte: zwei Dreiecke aus vier Punkten. */
function viereck(a, b, c, d, material) {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute([
        a.x, a.y, a.z, b.x, b.y, b.z, c.x, c.y, c.z, a.x, a.y, a.z, c.x, c.y, c.z, d.x, d.y, d.z], 3));
    g.computeVertexNormals();
    return new THREE.Mesh(g, material);
}

function plattenmaterial(art) {
    const durchsichtig = DURCHSICHTIG[art] === true;
    return new THREE.MeshLambertMaterial({
        color: PLATTENFARBE[art] ?? PLATTENFARBE.Sonstiges, side: THREE.DoubleSide,
        transparent: durchsichtig, opacity: durchsichtig ? 0.55 : 1,
        polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -1,
    });
}

/** Eine Flaeche (Boden oder Decke) aus dem Polygon in der Hoehe z. */
function deckflaeche(punkte, z, material) {
    const form = new THREE.Shape(punkte.map(p => new THREE.Vector2(p.x, p.y)));
    const g = new THREE.ShapeGeometry(form);
    g.rotateX(-Math.PI / 2);
    g.translate(0, z, 0);
    return new THREE.Mesh(g, material);
}

/** Ein Prisma aus dem Umriss: je Polygon extrudiert, Kanten, mit Platten nur im Exportmodell. */
function prisma(k, ziel, mitPlatten, box) {
    const z = zustand;
    const zielzone = ziel !== null && k.zone === ziel;
    const grundfarbe = new THREE.Color(farbe(z.canvas, k.stelle));
    const material = new THREE.MeshLambertMaterial({
        color: grundfarbe, transparent: true, opacity: k.schematisch ? 0.45 : 0.8,
        side: THREE.DoubleSide, depthWrite: !k.schematisch,
    });
    const linie = k.schematisch
        ? new THREE.LineDashedMaterial({ color: 0x333333, dashSize: 0.4, gapSize: 0.25 })
        : new THREE.LineBasicMaterial({ color: zielzone ? 0x000000 : 0x555555 });

    (k.polygone ?? []).forEach((polygon, pi) => {
        if (!polygon || polygon.length < 3) return;
        const form = new THREE.Shape(polygon.map(p => new THREE.Vector2(p.x, p.y)));
        const g = new THREE.ExtrudeGeometry(form, { depth: k.hoehe, bevelEnabled: false });
        g.rotateX(-Math.PI / 2);
        g.translate(0, k.unterkante, 0);
        const mesh = new THREE.Mesh(g, material);
        mesh.userData = { zone: k.zone ?? null, raum: k.raum };
        z.gruppe.add(mesh);
        z.koerper.push(mesh);

        const kanten = new THREE.LineSegments(new THREE.EdgesGeometry(g), linie);
        if (k.schematisch) kanten.computeLineDistances();
        z.gruppe.add(kanten);

        if (mitPlatten) {
            // Platten je Kante, wo ein Bauteil bekannt ist; leicht nach aussen versetzt.
            const arten = (k.kanten && k.kanten[pi]) || [];
            for (let i = 0; i < polygon.length; i++) {
                const art = arten[i];
                if (!art) continue;
                const p = polygon[i], q = polygon[(i + 1) % polygon.length];
                const dx = q.x - p.x, dy = q.y - p.y, l = Math.hypot(dx, dy);
                if (l <= 0) continue;
                // Gegen den Uhrzeigersinn: die Aussennormale ist (dy, -dx) / l.
                const nx = dy / l * PLATTE_ABSTAND, ny = -dx / l * PLATTE_ABSTAND;
                const u = k.unterkante, o = k.unterkante + k.hoehe;
                z.gruppe.add(viereck(v3(p.x + nx, p.y + ny, u), v3(q.x + nx, q.y + ny, u),
                    v3(q.x + nx, q.y + ny, o), v3(p.x + nx, p.y + ny, o), plattenmaterial(art)));
            }
            if (k.boden) z.gruppe.add(deckflaeche(polygon, k.unterkante - PLATTE_ABSTAND, plattenmaterial(k.boden)));
            if (k.decke) z.gruppe.add(deckflaeche(polygon, k.unterkante + k.hoehe + PLATTE_ABSTAND, plattenmaterial(k.decke)));
        }

        g.computeBoundingBox();
        box.union(g.boundingBox);
    });
}

/** Ein Abschnitt des Bytefelds als eigenes, ausgerichtetes Feld (Little-Endian wie alle Zielplattformen). */
function abschnitt(feld, ab, zahl, Art) {
    const bytes = feld.slice(ab, ab + zahl * 4);
    return new Art(bytes.buffer, bytes.byteOffset, zahl);
}

/** Der Dateikoerper eines Raums: Netz relativ zum Bezugspunkt, Mesh am Bezugspunkt, Randkanten als Linien. */
function dateinetz(e, feld, bezug, ziel, box) {
    const z = zustand;
    const roh = abschnitt(feld, e.punkteAb, e.punktZahl * 3, Float32Array);
    // Achsen wie v3: (x, y, z) -> (x, z, -y); eine Drehung, der Umlauf der Dreiecke bleibt.
    const lage = new Float32Array(roh.length);
    for (let i = 0; i < roh.length; i += 3) {
        lage[i] = roh[i];
        lage[i + 1] = roh[i + 2];
        lage[i + 2] = -roh[i + 1];
    }
    const position = new THREE.BufferAttribute(lage, 3);
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', position);
    g.setIndex(new THREE.BufferAttribute(new Uint32Array(abschnitt(feld, e.dreieckeAb, e.dreieckZahl * 3, Int32Array)), 1));
    g.computeVertexNormals();
    const material = new THREE.MeshLambertMaterial({
        color: new THREE.Color(farbe(z.canvas, e.stelle)), transparent: true, opacity: 0.8,
        side: THREE.DoubleSide, flatShading: true,
    });
    const ort = v3(bezug[0], bezug[1], bezug[2]);
    const mesh = new THREE.Mesh(g, material);
    mesh.position.copy(ort);
    mesh.userData = { zone: e.zone ?? null, raum: e.raum };
    z.gruppe.add(mesh);
    z.koerper.push(mesh);

    const k = new THREE.BufferGeometry();
    k.setAttribute('position', position);
    k.setIndex(new THREE.BufferAttribute(new Uint32Array(abschnitt(feld, e.kantenAb, e.kantenZahl * 2, Int32Array)), 1));
    const zielzone = ziel !== null && e.zone === ziel;
    const kanten = new THREE.LineSegments(k, new THREE.LineBasicMaterial({ color: zielzone ? 0x000000 : 0x555555 }));
    kanten.position.copy(ort);
    z.gruppe.add(kanten);

    g.computeBoundingBox();
    box.union(g.boundingBox.clone().translate(ort));
}

/** Baut die Szene aus den Daten (und dem Bytefeld der Dateikoerper); gibt die Ausdehnung zurueck. */
function bauen(daten, feld) {
    const z = zustand;
    entsorgeGruppe(z.gruppe);
    z.koerper = [];
    const box = new THREE.Box3();
    const ziel = daten.ziel ?? null;
    const dateimodus = daten.modus === 'dateikoerper';

    for (const k of daten.koerper ?? []) prisma(k, ziel, !dateimodus, box);
    if (dateimodus && feld) {
        const bezug = daten.bezugspunkt ?? [0, 0, 0];
        for (const e of daten.dateikoerper ?? []) dateinetz(e, feld, bezug, ziel, box);
    }
    if (box.isEmpty()) box.set(new THREE.Vector3(-5, 0, -5), new THREE.Vector3(5, 3, 5));

    // Raster am Boden und Nordpfeil an der Suedwestecke.
    const groesse = box.getSize(new THREE.Vector3());
    const mitte = box.getCenter(new THREE.Vector3());
    const spanne = Math.max(groesse.x, groesse.z, 1);
    const raster = Math.ceil(spanne * 1.4 / 2) * 2;
    const gitter = new THREE.GridHelper(raster, raster, 0x999999, 0xdddddd);
    gitter.position.set(mitte.x, box.min.y - 0.01, mitte.z);
    z.gruppe.add(gitter);
    const pfeillaenge = Math.max(spanne * 0.2, 1);
    const fuss = new THREE.Vector3(box.min.x - pfeillaenge * 0.6, box.min.y, box.max.z + pfeillaenge * 0.6);
    z.gruppe.add(new THREE.ArrowHelper(new THREE.Vector3(0, 0, -1), fuss, pfeillaenge, 0xc0392b,
        pfeillaenge * 0.3, pfeillaenge * 0.18));
    return box;
}

function kamera(box) {
    const z = zustand;
    const mitte = box.getCenter(new THREE.Vector3());
    const groesse = box.getSize(new THREE.Vector3());
    const r = Math.max(groesse.length(), 1);
    z.kamera.near = r / 100;
    z.kamera.far = r * 20;
    z.kamera.position.set(mitte.x + r * 0.9, mitte.y + r * 0.8, mitte.z + r * 1.1);
    z.kamera.updateProjectionMatrix();
    z.steuerung.target.copy(mitte);
    z.steuerung.update();
}

function zeichnen() {
    if (!zustand) return;
    zustand.renderer.render(zustand.szene, zustand.kamera);
}

function groesseAnpassen() {
    const z = zustand;
    if (!z) return;
    const b = Math.max(z.canvas.clientWidth, 1), h = Math.max(z.canvas.clientHeight, 1);
    z.renderer.setSize(b, h, false);
    z.kamera.aspect = b / h;
    z.kamera.updateProjectionMatrix();
    zeichnen();
}

function beiZeigerUnten(e) { zustand.unten = { x: e.clientX, y: e.clientY }; }

function beiZeigerOben(e) {
    const z = zustand;
    if (!z || !z.unten) return;
    const weg = Math.hypot(e.clientX - z.unten.x, e.clientY - z.unten.y);
    z.unten = null;
    if (weg > KLICK_PX || !z.rueckruf) return;
    const rahmen = z.canvas.getBoundingClientRect();
    const zeiger = new THREE.Vector2(
        (e.clientX - rahmen.left) / rahmen.width * 2 - 1, -(e.clientY - rahmen.top) / rahmen.height * 2 + 1);
    z.strahl.setFromCamera(zeiger, z.kamera);
    const treffer = z.strahl.intersectObjects(z.koerper, false);
    if (treffer.length === 0) return;
    const d = treffer[0].object.userData;
    z.rueckruf.invokeMethodAsync('ZoneGewaehlt', d.zone, d.raum).catch(() => { });
}

/** Ist WebGL in dieser Umgebung verfuegbar? */
function webgl() {
    try {
        const probe = document.createElement('canvas');
        return !!(probe.getContext('webgl2') || probe.getContext('webgl'));
    } catch (e) { return false; }
}

/**
 * Legt die Ansicht am canvas an; feld ist das Bytefeld der Dateikoerper (nur im Modus "dateikoerper"). Gibt false zurueck, wenn die Umgebung kein WebGL kann (dann bleibt
 * das canvas leer und der Baustein meldet es benannt).
 */
export function erzeugen(canvas, daten, rueckruf, feld) {
    entsorgen();
    if (!canvas || !webgl()) return false;
    let renderer;
    try {
        renderer = new THREE.WebGLRenderer({ canvas, antialias: true, preserveDrawingBuffer: true });
    } catch (e) { return false; }
    renderer.setPixelRatio(window.devicePixelRatio || 1);
    renderer.setClearColor(new THREE.Color(token(canvas, '--epos-flaeche-hell', '#f7f6f2')), 1);

    const szene = new THREE.Scene();
    szene.add(new THREE.HemisphereLight(0xffffff, 0x8a8a80, 2.2));
    const sonne = new THREE.DirectionalLight(0xffffff, 1.6);
    sonne.position.set(30, 50, 20);
    szene.add(sonne);
    const gruppe = new THREE.Group();
    szene.add(gruppe);

    const kam = new THREE.PerspectiveCamera(40, 1, 0.1, 1000);
    const steuerung = new OrbitControls(kam, canvas);
    steuerung.enableDamping = false;

    zustand = {
        canvas, renderer, szene, gruppe, kamera: kam, steuerung, rueckruf,
        strahl: new THREE.Raycaster(), koerper: [], unten: null, beobachter: null,
    };
    steuerung.addEventListener('change', zeichnen);
    canvas.addEventListener('pointerdown', beiZeigerUnten);
    canvas.addEventListener('pointerup', beiZeigerOben);
    if (typeof ResizeObserver !== 'undefined') {
        zustand.beobachter = new ResizeObserver(groesseAnpassen);
        zustand.beobachter.observe(canvas);
    }
    const box = bauen(daten || {}, feld || null);
    groesseAnpassen();
    kamera(box);
    zeichnen();
    return true;
}

/**
 * Neue Daten (andere Zuordnung, andere Zielzone, andere Darstellung samt Bytefeld); die Kamera bleibt, wo der
 * Anwender sie hingedreht hat.
 */
export function aktualisieren(daten, feld) {
    if (!zustand) return false;
    bauen(daten || {}, feld || null);
    zeichnen();
    return true;
}

/** Gibt Renderer, Geometrien und Ereignisse frei; mehrfach aufrufbar. */
export function entsorgen() {
    const z = zustand;
    if (!z) return;
    zustand = null;
    try {
        z.canvas.removeEventListener('pointerdown', beiZeigerUnten);
        z.canvas.removeEventListener('pointerup', beiZeigerOben);
        if (z.beobachter) z.beobachter.disconnect();
        z.steuerung.dispose();
        entsorgeGruppe(z.gruppe);
        z.renderer.dispose();
    } catch (e) { /* die Seite ist schon weg - nichts zu loesen */ }
}
