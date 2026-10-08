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
// FARBMODUS "randbedingung" (HottCAD-Verbund 4.3; daten.farbmodus). Jedes Dreieck eines Dateikoerpers traegt die Farbe
// seiner Gruppe R0 bis R7 aus daten.randfarben (die EINE Farbtafel kommt aus GebaeudeAnsichtRandgruppen.FARBEN, hier steht
// keine zweite); die Gruppenbytes stehen im dritten Teil des Bytefelds ab e.gruppenAb, ein Byte je Dreieck, 255 = entartet
// (neutral grau wie ein Raum ohne Zone, nicht ausgelassen). R0 ist halbtransparent, die uebrigen Gruppen decken.
// daten.randsichtbar blendet je Gruppe aus - an Raum- und Bauteilkoerpern. Die Bauteilkoerper (daten.bauteile, zweiter Teil
// des Bytefelds) stehen nur in diesem Modus als eigene Netze in ihrer Gruppenfarbe da, abschaltbar ueber
// daten.bauteilesichtbar. Prismen ohne Gruppen stehen neutral und ohne Platten da. Der Klick meldet zusaetzlich Gruppe und
// Bauteilkennung. Im Modus "zonen" bleibt alles wie oben; der Rueckruf traegt dann Gruppe und Bauteil als null.
//
// FARBMODUS "aufbau" (Konzept Bauteilaufbau 5.4, BA-3): derselbe Bauplan mit den Stufenbytes statt der Gruppenbytes - je
// Raum ab e.stufenAb (vierter Teil des Bytefelds), je Bauteilkoerper e.stufe -, der Farbtafel daten.aufbaufarben
// (GebaeudeAnsichtAufbaustufen.FARBEN) und den Schaltern daten.aufbausichtbar. Halbtransparent ist hier die neutrale
// Innenflaeche (Byte 5), nicht 0. Der Klick meldet als fuenften Wert das Dreieck des Netzes (Index der Datei, vor dem
// Umsortieren) - daran haengt in der Komponente das Bauteil der Raumflaeche (Steckbrief); in "zonen" ist es null.
//
// FARBMODUS "befund" (Abstimmung G5, B1; G5-3): derselbe Bauplan mit den Befundbytes - je Raum ab e.befundAb (fuenfter Teil
// des Bytefelds), je Bauteilkoerper e.befund -, der Farbtafel daten.befundfarben (GebaeudeAnsichtBefundstufen.FARBEN) und
// den Schaltern daten.befundsichtbar. Halbtransparent ist die neutrale Innenflaeche (Byte 4). Der Klick wie in "aufbau".
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

/** Der Gruppenwert eines entarteten Dreiecks (GebaeudeAnsichtRandgruppen.KEINE). */
const KEINE = 255;
/** Die Zahl der Gruppen R0 bis R7. */
const GRUPPEN = 8;
/** Die Deckung von R0: die Huelle bleibt von aussen und innen lesbar. */
const R0_DECKUNG = 0.28;

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

/** Ist der Farbmodus "randbedingung" gewaehlt? */
function imRandmodus(daten) { return daten.farbmodus === 'randbedingung'; }

/** Ist der Farbmodus "aufbau" gewaehlt (BA-3)? */
function imAufbaumodus(daten) { return daten.farbmodus === 'aufbau'; }

/** Ist der Farbmodus "befund" gewaehlt (G5-3)? */
function imBefundmodus(daten) { return daten.farbmodus === 'befund'; }

/** Ist ein Modus mit Gruppen je Dreieck gewaehlt? */
function imGruppenmodus(daten) { return imRandmodus(daten) || imAufbaumodus(daten) || imBefundmodus(daten); }

/** Die Zahl der Stufen mit Schalter (A, B, C, transparent, ohne Bauteil). */
const STUFEN = 5;
/** Das Stufenbyte der neutralen Innenflaeche (R0) - halbtransparent. */
const STUFE_INNEN = 5;

/** Die Zahl der Befunde mit Schalter (ohne, Koerper unlesbar, ohne Eigenschaften, ohne Bauteil). */
const BEFUNDE = 4;
/** Das Befundbyte der neutralen Innenflaeche (R0) - halbtransparent. */
const BEFUND_INNEN = 4;

/** Der halbtransparente Wert des Modus: R0 im Randmodus, die neutrale Innenflaeche im Aufbau- und im Befundmodus. */
function durchsichtigerWert(daten) { return imAufbaumodus(daten) ? STUFE_INNEN : imBefundmodus(daten) ? BEFUND_INNEN : 0; }

/** Der Wert eines Bauteilkoerpers im gewaehlten Modus: Befund, Stufe oder Gruppe. */
function bauteilwert(daten, b) { return imBefundmodus(daten) ? b.befund : imAufbaumodus(daten) ? b.stufe : b.gruppe; }

/** Ist die Gruppe (bzw. im Aufbaumodus die Stufe) eingeblendet? Ohne Schalter oder fuer entartete Dreiecke (255) immer. */
function gruppeSichtbar(daten, gruppe) {
    if (imBefundmodus(daten)) {
        if (gruppe < 0 || gruppe >= BEFUNDE) return true;
        const b = daten.befundsichtbar;
        return !Array.isArray(b) || b[gruppe] !== false;
    }
    if (imAufbaumodus(daten)) {
        if (gruppe < 0 || gruppe >= STUFEN) return true;
        const a = daten.aufbausichtbar;
        return !Array.isArray(a) || a[gruppe] !== false;
    }
    if (gruppe < 0 || gruppe >= GRUPPEN) return true;
    const s = daten.randsichtbar;
    return !Array.isArray(s) || s[gruppe] !== false;
}

/**
 * Ordnet die Dreiecke eines Netzes nach ihrer Gruppe (reine Funktion, ohne three.js - der node-Lauf prueft sie).
 * dreiecke: Indextripel (Int32Array/Uint32Array), gruppen: ein Byte je Dreieck oder null, sichtbar: (gruppe) => bool.
 * Gibt den umsortierten Index (Uint32Array), je vorkommender und sichtbarer Gruppe einen Abschnitt {gruppe, ab, zahl}
 * (ab und zahl in Indizes, aufsteigend nach Gruppe, 255 zuletzt), je umsortiertem Dreieck seine Gruppe und sein Index in der
 * uebergebenen Folge (dreieckUrsprung) zurueck.
 * Ausgeblendete Gruppen fallen ganz heraus - so trifft sie auch der Klick nicht. Ohne Gruppenbytes ist jedes Dreieck 255.
 */
export function randaufbau(dreiecke, gruppen, sichtbar) {
    const zahl = Math.floor(dreiecke.length / 3);
    const je = new Uint32Array(256);
    const gruppeVon = i => (gruppen && i < gruppen.length ? gruppen[i] : KEINE);
    for (let i = 0; i < zahl; i++) je[gruppeVon(i)]++;
    // Reihenfolge: R0 bis R7, dann alle uebrigen Werte (255 = entartet) - eine feste, deterministische Folge.
    const start = new Int32Array(256).fill(-1);
    let summe = 0;
    const abschnitte = [];
    for (let g = 0; g < 256; g++) {
        if (je[g] === 0 || !sichtbar(g)) continue;
        start[g] = summe;
        abschnitte.push({ gruppe: g, ab: summe * 3, zahl: je[g] * 3 });
        summe += je[g];
    }
    const index = new Uint32Array(summe * 3);
    const dreieckGruppe = new Uint8Array(summe);
    const dreieckUrsprung = new Uint32Array(summe);
    const stelle = start.slice();
    for (let i = 0; i < zahl; i++) {
        const g = gruppeVon(i);
        const d = stelle[g];
        if (d < 0) continue;
        stelle[g]++;
        index[d * 3] = dreiecke[i * 3];
        index[d * 3 + 1] = dreiecke[i * 3 + 1];
        index[d * 3 + 2] = dreiecke[i * 3 + 2];
        dreieckGruppe[d] = g;
        dreieckUrsprung[d] = i;
    }
    return { index, abschnitte, dreieckGruppe, dreieckUrsprung };
}

/**
 * Das Material einer Gruppe, je Szene einmal (alle Netze derselben Gruppe teilen es; entsorgeGruppe gibt es frei).
 * Warum Geometriegruppen mit eigenem Material und kein Farbattribut je Ecke: R0 braucht ein durchscheinendes Material
 * OHNE depthWrite (sonst verdeckt die Huelle, was dahinter liegt, je nach Zeichenfolge - sie flackert), die uebrigen
 * Gruppen ein deckendes MIT depthWrite. Ein Farbattribut trueg nur ein Material je Netz; Transparenz je Dreieck hiesse
 * alle Dreiecke durchscheinend zu sortieren. Gruppen trennen das sauber, Ausblenden ist ein fehlender Abschnitt, und der
 * Index bleibt geteilt (keine verdreifachten Ecken).
 */
function randmaterial(daten, gruppe, bauteil) {
    const z = zustand;
    const aufbau = imAufbaumodus(daten);
    const befund = imBefundmodus(daten);
    const schluessel = (befund ? 'f' : aufbau ? 'a' : 'g') + gruppe + (bauteil ? '|b' : '|r');
    let m = z.randmaterial.get(schluessel);
    if (m) return m;
    const tafel = befund ? (Array.isArray(daten.befundfarben) ? daten.befundfarben : [])
        : aufbau ? (Array.isArray(daten.aufbaufarben) ? daten.aufbaufarben : [])
        : (Array.isArray(daten.randfarben) ? daten.randfarben : []);
    const grenze = befund || aufbau ? tafel.length : GRUPPEN;
    const farbwert = gruppe >= 0 && gruppe < grenze && tafel[gruppe] ? tafel[gruppe] : farbe(z.canvas, -1);
    const r0 = gruppe === durchsichtigerWert(daten);
    m = new THREE.MeshLambertMaterial({
        color: new THREE.Color(farbwert), side: THREE.DoubleSide, flatShading: true,
        transparent: r0, opacity: r0 ? R0_DECKUNG : 1, depthWrite: !r0,
        // Bauteilkoerper liegen an den Raumflaechen an: leicht nach hinten versetzt, damit die Raumflaeche vorn bleibt.
        polygonOffset: bauteil, polygonOffsetFactor: bauteil ? 1 : 0, polygonOffsetUnits: bauteil ? 1 : 0,
    });
    z.randmaterial.set(schluessel, m);
    return m;
}

/** Baut aus Lage und Aufbau eine Geometrie mit je Gruppe einem Abschnitt und dem passenden Material. */
function randgeometrie(position, aufbau, daten, bauteil) {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', position);
    g.setIndex(new THREE.BufferAttribute(aufbau.index, 1));
    const materialien = [];
    aufbau.abschnitte.forEach((a, i) => {
        g.addGroup(a.ab, a.zahl, i);
        materialien.push(randmaterial(daten, a.gruppe, bauteil));
    });
    g.computeVertexNormals();
    return { geometrie: g, materialien };
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
function prisma(k, ziel, mitPlatten, box, rand) {
    const z = zustand;
    const zielzone = ziel !== null && k.zone === ziel;
    // Im Randmodus kennt ein Prisma keine Gruppen: neutral grau wie ein Raum ohne Zone.
    const grundfarbe = new THREE.Color(farbe(z.canvas, rand ? -1 : k.stelle));
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
        mesh.userData = { zone: k.zone ?? null, raum: k.raum, gruppen: null, bauteil: null };
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
function dateinetz(e, feld, bezug, ziel, box, daten) {
    const z = zustand;
    const rand = imGruppenmodus(daten);
    const aufbaumodus = imAufbaumodus(daten);
    const befundmodus = imBefundmodus(daten);
    const roh = abschnitt(feld, e.punkteAb, e.punktZahl * 3, Float32Array);
    // Achsen wie v3: (x, y, z) -> (x, z, -y); eine Drehung, der Umlauf der Dreiecke bleibt.
    const lage = new Float32Array(roh.length);
    for (let i = 0; i < roh.length; i += 3) {
        lage[i] = roh[i];
        lage[i + 1] = roh[i + 2];
        lage[i + 2] = -roh[i + 1];
    }
    const position = new THREE.BufferAttribute(lage, 3);
    const dreiecke = abschnitt(feld, e.dreieckeAb, e.dreieckZahl * 3, Int32Array);
    const bauteil = e.bauteil ?? null;
    const ort = v3(bezug[0], bezug[1], bezug[2]);
    let g, mesh;
    if (rand) {
        // Gruppen je Dreieck: Raeume aus dem dritten Teil des Felds, ein Bauteilkoerper traegt seine eine Gruppe.
        // Im Aufbaumodus die Stufen: Raeume ab stufenAb, ein Bauteilkoerper traegt seine Stufe.
        let gruppen = null;
        // Im Befundmodus die Befunde: Raeume ab befundAb, ein Bauteilkoerper traegt seinen Befund.
        const ab = befundmodus ? e.befundAb : aufbaumodus ? e.stufenAb : e.gruppenAb;
        const eine = befundmodus ? e.befund : aufbaumodus ? e.stufe : e.gruppe;
        if (bauteil !== null) gruppen = new Uint8Array(e.dreieckZahl).fill(Number.isInteger(eine) && eine >= 0 ? eine : KEINE);
        else if (Number.isInteger(ab) && ab >= 0) gruppen = feld.slice(ab, ab + e.dreieckZahl);
        const aufbau = randaufbau(dreiecke, gruppen, gr => gruppeSichtbar(daten, gr));
        const netz = randgeometrie(position, aufbau, daten, bauteil !== null);
        g = netz.geometrie;
        mesh = new THREE.Mesh(g, netz.materialien);
        mesh.userData = {
            zone: e.zone ?? null, raum: e.raum ?? null, gruppen: aufbau.dreieckGruppe, bauteil,
            ursprung: aufbau.dreieckUrsprung, durchsichtig: durchsichtigerWert(daten),
        };
    } else {
        g = new THREE.BufferGeometry();
        g.setAttribute('position', position);
        g.setIndex(new THREE.BufferAttribute(new Uint32Array(dreiecke), 1));
        g.computeVertexNormals();
        const material = new THREE.MeshLambertMaterial({
            color: new THREE.Color(farbe(z.canvas, e.stelle)), transparent: true, opacity: 0.8,
            side: THREE.DoubleSide, flatShading: true,
        });
        mesh = new THREE.Mesh(g, material);
        mesh.userData = { zone: e.zone ?? null, raum: e.raum, gruppen: null, bauteil: null };
    }
    mesh.position.copy(ort);
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
    z.randmaterial = new Map();
    const box = new THREE.Box3();
    const ziel = daten.ziel ?? null;
    const dateimodus = daten.modus === 'dateikoerper';

    const rand = imGruppenmodus(daten);
    for (const k of daten.koerper ?? []) prisma(k, ziel, !dateimodus && !rand, box, rand);
    if (dateimodus && feld) {
        const bezug = daten.bezugspunkt ?? [0, 0, 0];
        for (const e of daten.dateikoerper ?? []) dateinetz(e, feld, bezug, ziel, box, daten);
        // Die Bauteilkoerper nur im Randmodus, nur mit Schalter und nur, wo ihre Gruppe eingeblendet ist.
        if (rand && daten.bauteilesichtbar !== false) {
            for (const b of daten.bauteile ?? []) {
                if (!gruppeSichtbar(daten, bauteilwert(daten, b)) || !(b.dreieckZahl > 0)) continue;
                dateinetz({ ...b, raum: null, zone: null }, feld, bezug, ziel, box, daten);
            }
        }
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
    const t = trefferWahl(treffer);
    const d = t.object.userData;
    const gruppe = d.gruppen && t.faceIndex !== undefined && t.faceIndex < d.gruppen.length ? d.gruppen[t.faceIndex] : null;
    const dreieck = d.ursprung && t.faceIndex !== undefined && t.faceIndex < d.ursprung.length ? d.ursprung[t.faceIndex] : null;
    z.rueckruf.invokeMethodAsync('ZoneGewaehlt', d.zone ?? null, d.raum ?? null, gruppe, d.bauteil ?? null, dreieck).catch(() => { });
}

/**
 * Der gemeinte Treffer: der vorderste, der nicht in der durchscheinenden R0 liegt - durch sie hindurch sieht der Anwender,
 * was er anklickt; trifft der Strahl nur R0, die vorderste R0-Flaeche.
 */
export function trefferWahl(treffer) {
    for (const t of treffer) {
        const g = t.object.userData.gruppen;
        const durchsichtig = t.object.userData.durchsichtig ?? 0;
        if (!g || t.faceIndex === undefined || g[t.faceIndex] !== durchsichtig) return t;
    }
    return treffer[0];
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
        strahl: new THREE.Raycaster(), koerper: [], unten: null, beobachter: null, randmaterial: new Map(),
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

/**
 * Pruefhilfe fuer Proben: was die Szene gerade traegt - je Koerpernetz Raum, Bauteil und je Abschnitt Gruppe, Indexzahl und
 * Deckung, dazu die Geometrien und Programme, die der Renderer haelt (Lecks bei wiederholtem aktualisieren). Ohne Szene null.
 */
export function stand() {
    const z = zustand;
    if (!z) return null;
    const netze = z.koerper.map(m => ({
        raum: m.userData.raum ?? null,
        bauteil: m.userData.bauteil ?? null,
        abschnitte: Array.isArray(m.material)
            ? m.geometry.groups.map(g => ({
                gruppe: m.userData.gruppen && g.count > 0 ? m.userData.gruppen[g.start / 3] : null,
                zahl: g.count, deckung: m.material[g.materialIndex].opacity,
                tiefe: m.material[g.materialIndex].depthWrite, farbe: '#' + m.material[g.materialIndex].color.getHexString(),
            }))
            : [{ gruppe: null, zahl: m.geometry.index ? m.geometry.index.count : 0, deckung: m.material.opacity,
                tiefe: m.material.depthWrite, farbe: '#' + m.material.color.getHexString() }],
    }));
    return { netze, geometrien: z.renderer.info.memory.geometries, programme: (z.renderer.info.programs || []).length };
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
