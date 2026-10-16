// =====================================================================
//  DIAGRAMMPROBE - DER ZEIGERBALKEN FOLGT DER MAUS IM SELBEN BILD
// =====================================================================
//
//  WOZU (Auftrag GX). Der Anwender meldete: "Beim Zoomen der Grafik wird die
//  Selektion (senkrechter Balken der Position der Maus) verlangsamt und ist nicht
//  synchron zur Maus-Bewegung." Ursache war ein Rundlauf je Mausbewegung: Das Modul
//  meldete die Stelle an .NET, und Blazor zeichnete Linie und Zeile neu - bei x12
//  samt der roh nachgerechneten Pfade. Seitdem setzt epos-diagramm.js den Balken
//  unmittelbar im pointermove und rechnet die Zeile aus der Zeigertafel, die der
//  Baustein einmal je Zeichnen mitgibt. bunit hat weder Layout noch JavaScript -
//  diese Probe misst es auf der Seite /diagrammsvg des Wirtes (Jahresgang, 8 760
//  Stunden, echtes Modell aus dem Kern).
//
//  ABLAUF. Seite laden, Zoom per Rad auf x12 (bis zum Anschlag), warten, bis die
//  Zeile dem Modul gehoert (.epos-diagramm-zeigerzeile--modul). Dann die Maus in
//  SCHRITTE Schritten ueber die Datenflaeche fuehren und je Schritt messen:
//    - Balken sichtbar und |Balkenmitte - Maus x| <= 1 px - gemessen im selben
//      Ereignis-Durchlauf, ohne auf einen Rundlauf zu warten (die Messung laeuft
//      unmittelbar nach mouse.move, bevor ein weiterer Bildaufbau kommt);
//    - die Zeigerzeile nennt nach dem naechsten Bildaufbau Stunde und Datum
//      ("… h · <Tag>. <Monat> …");
//    - die Pfade der Reihen sind DIESELBEN Elemente wie vor der Bewegung (nicht
//      neu erzeugt) und ihr d-Attribut ist unveraendert - kein Neuzeichnen.
//  Dazu die Zahl der Pfad-Mutationen (MutationObserver) ueber die ganze Bewegung:
//  Soll 0.
//
//  GEGENPROBE (abschaltbar mit --ohne-gegenprobe): dieselbe Bewegung auf dem ALTEN
//  Weg - die Probe liefert das Modul so aus, dass es die Tafel ablehnt, und der
//  Baustein zeichnet Linie und Zeile nach einem Rundlauf je Bewegung selbst. Dort
//  steht die Linie nicht im selben Bild wie die Maus; die Probe muss verfehlen.
//
//  FALL "fluss" (--fall fluss, SK-3): das Energieflussbild der Uebersicht
//  (/uebersichtprobe?ansicht=fluss, echte Huelle, synthetische Ergebnisse). Ein
//  Bild OHNE Zeichenflaeche: die Zeigerzeile zeigt den Zeigertext des Elements unter
//  der Maus (Band oder Knoten), nicht einen Wert aus einer Reihe. Die Probe tastet
//  das breite Bild in einem Raster ab, nimmt je getroffenem Band oder Knoten einen
//  Punkt und misst je Element: Maus darauf - die Zeile nennt binnen 2 s einen Text
//  mit "MWh/a"; ein Band ("Von -> Nach") steht mit demselben Wortlaut bis "%)" in
//  der verborgenen Textalternative, ein Knoten mit seinem Namen; Maus daneben - die
//  Zeile ist leer. Mindestens 3 Baender und 3 Knoten, und das erste Element liefert
//  am Ende denselben Text wie am Anfang. GEGENPROBE: dieselben Punkte mit
//  pointer-events: none auf den Elementen des Bildes - die Zeile bleibt leer, die
//  Probe muss verfehlen.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node diagrammprobe.mjs --url http://127.0.0.1:5299 [--fall zoom|fluss] [--fotos <ordner>] [--ohne-gegenprobe]
//
//  Rueckgabe 0 = alle Sollwerte erfuellt (und Gegenprobe verfehlt), 1 = Verstoss,
//  2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/diagrammprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const FALL = arg('fall', 'zoom');
const SCHRITTE = 40;
const TOLERANZ_PX = 1;

/**
 * Das Rechteck der Datenflaeche (Viewport des inneren svg) in Fensterkoordinaten.
 * NICHT boundingBox(): Die meldet beim inneren svg die Huelle der gezeichneten
 * Pfade, und die ist im Zoom ein Vielfaches breiter als die Flaeche.
 */
async function datenRechteck(seite) {
  return seite.evaluate(() => {
    const svg = document.querySelector('svg.epos-flaeche');
    const m = svg.ownerSVGElement.getScreenCTM();
    const x = m.e + m.a * svg.x.baseVal.value, y = m.f + m.d * svg.y.baseVal.value;
    return { x, y, width: m.a * svg.width.baseVal.value, height: m.d * svg.height.baseVal.value };
  });
}

async function lauf(browser, ohneTafel) {
  const seite = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const befunde = [];
  try {
    if (ohneTafel) {
      // DIE GEGENPROBE stellt den alten Weg her: Das Modul lehnt die Tafel ab, und der
      // Baustein zeichnet Linie und Zeile wieder selbst - nach einem Rundlauf je Bewegung.
      await seite.route('**/epos-diagramm.js', async route => {
        const antwort = await route.fetch();
        const text = (await antwort.text()).replace(
          'export function zeigertafel(flaeche, reihen, zustand) {',
          'export function zeigertafel(flaeche, reihen, zustand) { return false;');
        await route.fulfill({ response: antwort, body: text });
      });
    }
    await seite.goto(WURZEL + '/diagrammsvg', { waitUntil: 'networkidle' });
    await seite.waitForSelector('svg.epos-flaeche', { timeout: 20000 });
    const rahmen = await seite.$('.epos-diagramm-svg-flaeche');
    let r = await datenRechteck(seite);

    // Zoom bis zum Anschlag (x12) um die Mitte.
    await seite.mouse.move(r.x + r.width / 2, r.y + r.height / 2);
    for (let i = 0; i < 30; i++) await seite.mouse.wheel(0, -400);
    await seite.waitForTimeout(600);   // Radruhe, Fenstermeldung, Nachladen
    const stufe = (await seite.textContent('.epos-diagramm-stufe') || '').trim();

    if (!ohneTafel) {
      await seite.waitForSelector('.epos-diagramm-zeigerzeile--modul', { timeout: 10000 })
        .catch(() => befunde.push('Zeigerzeile gehoert nicht dem Modul'));
    }

    r = await datenRechteck(seite);
    // Die Pfade merken und Mutationen zaehlen.
    await seite.evaluate(() => {
      window.__pfade = [...document.querySelectorAll('svg.epos-flaeche path')];
      window.__d = window.__pfade.map(p => p.getAttribute('d'));
      window.__mutationen = 0;
      window.__beob = new MutationObserver(l => { window.__mutationen += l.length; });
      window.__beob.observe(document.querySelector('svg.epos-flaeche'),
                            { subtree: true, childList: true, attributes: true, attributeFilter: ['d'] });
    });

    const abstaende = [];
    let mitDatum = 0;
    for (let s = 0; s <= SCHRITTE; s++) {
      const x = r.x + 2 + (r.width - 4) * s / SCHRITTE;
      const y = r.y + r.height / 2;
      await seite.mouse.move(x, y);
      // Unmittelbar - im selben Durchlauf, bevor ein weiterer Bildaufbau kommt.
      const lage = await seite.evaluate(() => {
        // Der Balken des Moduls - oder, auf dem alten Weg, die Linie des Bausteins.
        const b = document.querySelector('.epos-diagramm-zeigerbalken');
        const el = b && !b.hidden ? b : document.querySelector('line.epos-diagramm-zeiger');
        if (!el) return null;
        const rr = el.getBoundingClientRect();
        return rr.left + rr.width / 2;
      });
      if (lage === null) { abstaende.push(Infinity); continue; }
      abstaende.push(Math.abs(lage - x));
      // Die Zeile nach dem naechsten Bildaufbau.
      const zeile = await seite.evaluate(() => new Promise(fertig =>
        requestAnimationFrame(() => requestAnimationFrame(() =>
          fertig((document.querySelector('.epos-diagramm-zeigerzeile') || {}).textContent || '')))));
      if (/ h · \d+\.? ?[A-Za-zÄÖÜäöü.]+|h · [A-Z][a-z]+ \d+/.test(zeile)) mitDatum++;
    }

    const stand = await seite.evaluate(() => {
      const jetzt = [...document.querySelectorAll('svg.epos-flaeche path')];
      const gleich = jetzt.length === window.__pfade.length &&
                     jetzt.every((p, i) => p === window.__pfade[i] && p.isConnected &&
                                            p.getAttribute('d') === window.__d[i]);
      window.__beob.disconnect();
      return { gleich, mutationen: window.__mutationen, pfade: jetzt.length };
    });

    const groesster = Math.max(...abstaende);
    const ueber = abstaende.filter(a => a > TOLERANZ_PX).length;
    if (ueber > 0) befunde.push(`${ueber} von ${abstaende.length} Schritten mit Abstand > ${TOLERANZ_PX} px (groesster ${groesster})`);
    if (!stand.gleich) befunde.push('Pfade neu erzeugt oder veraendert');
    if (stand.mutationen > 0) befunde.push(`${stand.mutationen} Pfad-Mutationen waehrend der Bewegung`);
    if (!ohneTafel && mitDatum < abstaende.length) befunde.push(`Zeile mit Datum nur in ${mitDatum} von ${abstaende.length} Schritten`);

    if (FOTOS) {
      await mkdir(FOTOS, { recursive: true });
      await rahmen.screenshot({ path: `${FOTOS}/diagramm_${ohneTafel ? 'gegenprobe' : 'probe'}.png` });
    }
    return { stufe, schritte: abstaende.length, groesster, mitDatum, pfade: stand.pfade,
             mutationen: stand.mutationen, befunde };
  } finally {
    await seite.close();
  }
}

/**
 * FALL "fluss": die Zeigerzeile folgt der Maus ueber Band und Knoten des Energieflussbilds.
 * ohneZeiger = Gegenprobe (pointer-events: none auf den Elementen des Bildes).
 */
async function laufFluss(browser, ohneZeiger) {
  const seite = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const befunde = [];
  try {
    await seite.goto(WURZEL + '/uebersichtprobe?ansicht=fluss', { waitUntil: 'networkidle' });
    await seite.waitForSelector('.epos-ergebnis-fluss-mass--breit svg', { timeout: 20000 });
    await seite.locator('.epos-ergebnis-fluss-bild').scrollIntoViewIfNeeded();
    await seite.waitForTimeout(300);

    // Die verborgene Textalternative (Sollwerte) und die Kandidaten: je Element ein Punkt.
    const lage = await seite.evaluate(() => {
      const huelle = document.querySelector('.epos-ergebnis-fluss-mass--breit');
      const svg = huelle.querySelector('svg');
      const r = svg.getBoundingClientRect();
      const text = [...document.querySelectorAll('.epos-ergebnis-fluss-text li')].map(l => l.textContent);
      const punkte = [];
      const gesehen = new Set();
      let n = 0;
      for (let gy = 0; gy < 40; gy++) {
        for (let gx = 0; gx < 80; gx++) {
          const x = r.x + 4 + (r.width - 8) * gx / 79, y = r.y + 4 + (r.height - 8) * gy / 39;
          const el = document.elementFromPoint(x, y);
          if (!el || !svg.contains(el) || el === svg || !['path', 'rect'].includes(el.tagName)) continue;
          const b = el.getBoundingClientRect();
          if (b.width >= r.width * 0.95 && b.height >= r.height * 0.95) continue;   // Hintergrund
          if (gesehen.has(el)) continue;
          gesehen.add(el);
          el.setAttribute('data-probe', String(n));
          punkte.push({ n: n++, x, y, art: el.tagName });
        }
      }
      // Ein Punkt daneben: im Bild, aber auf keinem Element.
      let leer = null;
      for (let gy = 0; gy < 40 && !leer; gy++)
        for (let gx = 0; gx < 80 && !leer; gx++) {
          const x = r.x + 4 + (r.width - 8) * gx / 79, y = r.y + 4 + (r.height - 8) * gy / 39;
          const el = document.elementFromPoint(x, y);
          if (el && svg.contains(el) && el.tagName === 'rect' && el.getBoundingClientRect().width >= r.width * 0.95) leer = { x, y };
        }
      return { text, punkte, leer };
    });
    if (lage.punkte.length === 0) befunde.push('keine Baender oder Knoten im Bild getroffen');
    if (!lage.leer) befunde.push('kein freier Punkt im Bild');

    if (ohneZeiger) {
      await seite.addStyleTag({ content: '.epos-ergebnis-fluss-bild svg path, .epos-ergebnis-fluss-bild svg rect { pointer-events: none !important; }' });
    }

    const zeile = () => seite.evaluate(() =>
      ((document.querySelector('.epos-ergebnis-fluss-mass--breit .epos-diagramm-zeigerzeile') || {}).textContent || '').trim());
    const warte = async (bedingung) => {
      const ende = Date.now() + 2000;
      let t = await zeile();
      while (!bedingung(t) && Date.now() < ende) { await seite.waitForTimeout(40); t = await zeile(); }
      return t;
    };

    const texte = [];
    let baender = 0, knoten = 0, ohneText = 0, nichtInAlternative = 0, nichtLeer = 0;
    for (const p of lage.punkte) {
      if (lage.leer) {
        await seite.mouse.move(lage.leer.x, lage.leer.y);
        const t0 = await warte(t => t === '');
        if (t0 !== '') nichtLeer++;
      }
      await seite.mouse.move(p.x, p.y);
      const t = await warte(t => t !== '');
      texte.push(t);
      if (!t || !t.includes('MWh/a')) { ohneText++; continue; }
      if (t.includes(' → ')) {
        baender++;
        const kopf = t.slice(0, t.indexOf('%)') + 2);
        if (!lage.text.some(l => l.startsWith(kopf))) nichtInAlternative++;
      } else {
        knoten++;
        const name = t.slice(0, t.indexOf(':'));
        if (!lage.text.some(l => l.startsWith(name + ':') || l.includes(name + ' → ') || l.includes(' → ' + name + ':'))) nichtInAlternative++;
      }
    }
    // Wiederholt: das erste Element liefert denselben Text.
    if (lage.punkte.length > 0 && lage.leer) {
      await seite.mouse.move(lage.leer.x, lage.leer.y);
      await warte(t => t === '');
      await seite.mouse.move(lage.punkte[0].x, lage.punkte[0].y);
      const t = await warte(t => t !== '');
      if (t !== texte[0]) befunde.push('erstes Element nennt beim zweiten Mal einen anderen Text');
    }

    if (ohneText > 0) befunde.push(`${ohneText} von ${lage.punkte.length} Elementen ohne Zeigertext mit MWh/a`);
    if (nichtInAlternative > 0) befunde.push(`${nichtInAlternative} Zeigertexte nicht in der Textalternative`);
    if (nichtLeer > 0) befunde.push(`${nichtLeer} mal blieb die Zeile neben dem Bild stehen`);
    if (baender < 3) befunde.push(`nur ${baender} Baender mit Zeigertext`);
    if (knoten < 3) befunde.push(`nur ${knoten} Knoten mit Zeigertext`);

    if (FOTOS) {
      await mkdir(FOTOS, { recursive: true });
      await seite.locator('.epos-ergebnis-fluss').screenshot({ path: `${FOTOS}/fluss_${ohneZeiger ? 'gegenprobe' : 'probe'}.png` });
    }
    return { elemente: lage.punkte.length, baender, knoten, alternative: lage.text.length, befunde };
  } finally {
    await seite.close();
  }
}

let browser;
try {
  browser = await chromium.launch();
} catch (e) {
  console.error('Aufbaufehler: Chromium startet nicht - ' + e.message);
  process.exit(2);
}

let rc = 0;
try {
  if (FALL === 'fluss') {
    const p = await laufFluss(browser, false);
    console.log(`PROBE   Fluss · ${p.elemente} Elemente · ${p.baender} Baender und ${p.knoten} Knoten mit Zeigertext · ` +
                `${p.alternative} Zeilen Textalternative`);
    for (const b of p.befunde) console.log('  VERSTOSS: ' + b);
    if (p.befunde.length > 0) rc = 1;
    if (MIT_GEGENPROBE) {
      const g = await laufFluss(browser, true);
      const verfehlt = g.befunde.length > 0;
      console.log(`GEGEN   ohne Zeigerereignisse: ${verfehlt ? 'verfehlt (richtig)' : 'erfuellt (FALSCH)'}` +
                  (g.befunde.length ? ' - ' + g.befunde.join('; ') : ''));
      if (!verfehlt) rc = 1;
    }
  } else {
  const p = await lauf(browser, false);
  console.log(`PROBE   Stufe ${p.stufe} · ${p.schritte} Schritte · groesster Abstand ${p.groesster.toFixed(2)} px · ` +
              `Zeile mit Datum ${p.mitDatum}/${p.schritte} · ${p.pfade} Pfade · ${p.mutationen} Mutationen`);
  for (const b of p.befunde) console.log('  VERSTOSS: ' + b);
  if (p.befunde.length > 0) rc = 1;

  if (MIT_GEGENPROBE) {
    const g = await lauf(browser, true);
    const verfehlt = g.befunde.length > 0;
    console.log(`GEGEN   ohne Tafel: ${verfehlt ? 'verfehlt (richtig)' : 'erfuellt (FALSCH)'}` +
                (g.befunde.length ? ' - ' + g.befunde.join('; ') : ''));
    if (!verfehlt) rc = 1;
  }
  }
} catch (e) {
  console.error('Aufbaufehler: ' + (e && e.stack || e));
  rc = 2;
} finally {
  await browser.close();
}
console.log(rc === 0 ? 'ERGEBNIS: alle Sollwerte erfuellt.' : 'ERGEBNIS: VERFEHLT.');
process.exit(rc);
