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
//  Abgetastet werden nur Elemente mit einer Marke "fluss:" oder "knoten:" - die Farbfelder
//  der Legende (Marke "legende:", ohne Wert) und die Stufentitel tragen keinen Zeigertext.
//
//  FALL "hervorhebung" (--fall hervorhebung, SK-2d-2): das Umfeld beim Ueberfahren. Fuer
//  einen Wandler und einen Bus des Stamms stimmt die Menge der hervorgehobenen Baender
//  (Klasse epos-fluss-an, Marke "fluss:") mit dem Soll des Kerns ueberein
//  (EnergieflussAbbildung.Umfeld, vom Wirt verborgen in #uep-umfeld-soll), ein Band
//  ausserhalb ist abgeblendet (Deckkraft < 0,5), eines im Umfeld voll; in der Darstellung
//  "Stufen" leuchtet ein Verbinderband allein, mit beiden Stummeln. Maus aus dem Bild -
//  nichts mehr abgeblendet. GEGENPROBEN: ohne das Modul epos-energiefluss.js (Abruf
//  verweigert) und ohne die Abblendregel des Stilblatts - beide muessen verfehlen.
//
//  FALL "wahl" (--fall wahl, SK-2d-2): die Knopfgruppen ueber dem Bild. Kette ohne
//  Stufentitel; "Stufen" zeichnet die vier Titel; Filter "Strom" behaelt weniger Baender;
//  "Kaelte" (die Probe kennt keine Kaelte) zeigt den Hinweis im Bild (Marke "hinweis") und
//  als leise Zeile; Pfeil links geht auf "Waerme" und behaelt den Fokus in der Gruppe.
//
//  SCHRIFT IM SATZ (abschaltbar mit --ohne-schrift). Die Ganglinie der aufgeklappten
//  Satzflaeche (Strom-, Solar-, PV-Ganglinie auf /rollbereichprobe) wird im Behaeltermass
//  gezeichnet und schreibt in der Schrift der Oberflaeche - auch die Achsenteilung, die
//  DiagrammSvg selbst nachzeichnet, und auch im Projektassistenten (?huelle=assistent).
//  Je Fall und Fenster (1 400 x 800, 1 100 x 720): Flaeche mit contain: size, kein
//  Vorgabebild 1 240 x 560, jeder sichtbare Text am Schirm hoechstens SCHRIFT_MAX_PX und
//  alle Texte gleich gross (Spanne <= 1 px); dazu die Satz-Ueberlagerung ("Vergroessern")
//  der Stromganglinie. GEGENPROBE: derselbe Dialog auf /wikibild?bild=strombedarf, wo er
//  nicht als Wurzel des Fensters steht - dort meldet niemand ein Mass, das Vorgabebild
//  steht mit seiner Berichtsschrift (20 bis 29 px) im Satz; die Probe muss verfehlen.
//
//  Die Schriftmessung gehoert zum Fall "zoom".
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node diagrammprobe.mjs --url http://127.0.0.1:5299 [--fall zoom|fluss|hervorhebung|wahl] [--fotos <ordner>] [--ohne-gegenprobe] [--ohne-schrift]
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
const MIT_SCHRIFT = !process.argv.includes('--ohne-schrift');
/** Schrift der Oberflaeche 13 px (kompakt 12 px), mit Rundungsluft. */
const SCHRIFT_MAX_PX = 13.5;

/** Schriftgroessen der sichtbaren Texte des ersten sichtbaren Bildes unter `wahl`, am Schirm in px. */
async function schriftMessen(seite, wahl) {
  return seite.locator(wahl).first().evaluate(el => {
    const v = [...el.querySelectorAll('.epos-diagramm-svg-flaeche > svg')].find(x => x.getBoundingClientRect().width > 0);
    if (!v) return null;
    const k = v.getScreenCTM().a;
    const groessen = [...v.querySelectorAll('text')]
      .filter(t => !t.closest('[display=none]') && t.getBoundingClientRect().width > 0)
      .map(t => parseFloat(getComputedStyle(t).fontSize) * k);
    return { viewBox: v.getAttribute('viewBox'), contain: getComputedStyle(v.parentElement).contain, groessen };
  });
}

/** Befunde eines gemessenen Bildes gegen die Sollwerte der Satzschrift. */
function schriftBefunde(name, m) {
  if (!m) return [`${name}: kein sichtbares Bild`];
  const f = [];
  if (!/\bsize\b|strict/.test(m.contain || '')) f.push(`${name}: Flaeche ohne contain: size (${m.contain})`);
  if (m.viewBox === '0 0 1240 560') f.push(`${name}: Vorgabebild 1 240 x 560 statt Behaeltermass`);
  if (m.groessen.length === 0) return f.concat(`${name}: keine Texte`);
  const max = Math.max(...m.groessen), min = Math.min(...m.groessen);
  if (max > SCHRIFT_MAX_PX) f.push(`${name}: Schrift bis ${max.toFixed(1)} px (Soll <= ${SCHRIFT_MAX_PX})`);
  if (max - min > 1) f.push(`${name}: Schrift ${min.toFixed(1)} bis ${max.toFixed(1)} px (Spanne > 1 px)`);
  return f;
}

/** Ein Fall: Projektzeile waehlen, Satz aufklappen, Bild messen; wahlweise die Satz-Ueberlagerung dazu. */
async function schriftFall(browser, adresse, wahl, breite, hoehe, mitUeberlagerung) {
  const kontext = await browser.newContext({ viewport: { width: breite, height: hoehe }, deviceScaleFactor: 1, locale: 'de-DE' });
  const seite = await kontext.newPage();
  try {
    await seite.goto(WURZEL + adresse, { waitUntil: 'networkidle' });
    const dialog = seite.locator(wahl).first();
    await dialog.waitFor({ state: 'visible', timeout: 15000 });
    await seite.waitForTimeout(500);
    await dialog.locator('.epos-projektzeile').first().click();
    await dialog.locator('.epos-zweispalten-satzzeile').first().click();
    // Das Mass kommt nach dem Aufklappen (ResizeObserver, Rundlauf); hoechstens 5 s warten.
    for (let i = 0; i < 25; i++) {
      const m = await schriftMessen(seite, wahl);
      if (m && m.viewBox !== '0 0 1240 560') break;
      await seite.waitForTimeout(200);
    }
    await seite.waitForTimeout(400);
    const name = `${adresse} ${breite}x${hoehe}`;
    const satz = await schriftMessen(seite, wahl);
    const ergebnis = [{ name: name + ' Satz', m: satz }];
    if (mitUeberlagerung) {
      await dialog.locator('button.epos-zweispalten-vergroessern:not([disabled])').first().click();
      await seite.locator('.epos-satzueberlagerung-koerper .epos-diagramm-svg-flaeche > svg').first().waitFor({ timeout: 10000 });
      await seite.waitForTimeout(1200);
      ergebnis.push({ name: name + ' Ueberlagerung', m: await schriftMessen(seite, '.epos-satzueberlagerung-koerper') });
    }
    if (FOTOS) await dialog.screenshot({ path: `${FOTOS}/schrift_${adresse.replace(/\W+/g, '_')}_${breite}x${hoehe}.png` });
    return ergebnis;
  } finally {
    await kontext.close();
  }
}

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
          const markiert = el.closest('[data-marke]');
          const marke = markiert && svg.contains(markiert) ? markiert.getAttribute('data-marke') : '';
          if (!marke.startsWith('fluss:') && !marke.startsWith('knoten:')) continue;
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

const BREIT = '.epos-ergebnis-fluss-mass--breit';

/** Ein Punkt im breiten Bild, an dem das oberste Element die Marke traegt (das n-te Element dieser Marke zuerst). */
async function punktFuer(seite, marke, n = 0) {
  return seite.evaluate(({ marke, n, BREIT }) => {
    const svg = document.querySelector(BREIT + ' svg');
    const els = [...svg.querySelectorAll('[data-marke]')].filter(e => e.getAttribute('data-marke') === marke);
    const reihe = els.slice(n).concat(els.slice(0, n));
    for (const el of reihe) {
      const b = el.getBoundingClientRect();
      for (let gy = 1; gy < 12; gy++)
        for (let gx = 1; gx < 12; gx++) {
          const x = b.x + b.width * gx / 12, y = b.y + b.height * gy / 12;
          const t = document.elementFromPoint(x, y);
          const m = t && t.closest('[data-marke]');
          if (m === el) return { x, y };
        }
    }
    return null;
  }, { marke, n, BREIT });
}

/** Der Zustand der Hervorhebung im breiten Bild. */
async function hervorStand(seite) {
  return seite.evaluate(BREIT => {
    const figur = document.querySelector('.epos-ergebnis-fluss-bild');
    const svg = document.querySelector(BREIT + ' svg');
    const alle = [...svg.querySelectorAll('[data-marke^="fluss:"], [data-marke^="knoten:"]')];
    const deck = el => parseFloat(getComputedStyle(el).opacity);
    const an = alle.filter(e => e.classList.contains('epos-fluss-an'));
    const flussAn = [...new Set(an.map(e => e.getAttribute('data-marke')).filter(m => m.startsWith('fluss:')))].sort();
    const gezeichnet = [...new Set(alle.map(e => e.getAttribute('data-marke')).filter(m => m.startsWith('fluss:')))];
    const aus = alle.filter(e => !e.classList.contains('epos-fluss-an') && e.getAttribute('data-marke').startsWith('fluss:'));
    const abgeblendet = aus.length > 0 && aus.every(e => deck(e) < 0.5);
    const voll = an.length > 0 && an.every(e => deck(e) > 0.99);
    const stufen = svg.querySelectorAll('[data-marke^="stufe:"]').length;
    const stufenVoll = [...svg.querySelectorAll('[data-marke^="stufe:"], [data-marke^="legende:"]')].every(e => deck(e) > 0.99);
    return { hervor: figur.classList.contains('epos-ergebnis-fluss-bild--hervor'), flussAn, gezeichnet, abgeblendet, voll,
             ausZahl: aus.length, stufen, stufenVoll, minDeck: alle.length ? Math.min(...alle.map(deck)) : 1 };
  }, BREIT);
}

/**
 * FALL "hervorhebung": das Umfeld eines Wandlers, eines Busses und eines Verbinderbandes gegen das Soll des Kerns.
 * gegen = null | 'ohne-modul' | 'ohne-regel'.
 */
async function laufHervorhebung(browser, gegen) {
  const seite = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const befunde = [];
  const faelle = [];
  try {
    if (gegen === 'ohne-modul') await seite.route('**/epos-energiefluss.js', route => route.abort());
    await seite.goto(WURZEL + '/uebersichtprobe?ansicht=fluss', { waitUntil: 'networkidle' });
    await seite.waitForSelector(BREIT + ' svg', { timeout: 20000 });
    await seite.locator('.epos-ergebnis-fluss-bild').scrollIntoViewIfNeeded();
    if (gegen === 'ohne-regel')
      await seite.addStyleTag({ content: '.epos-ergebnis-fluss-bild [data-marke] { opacity: 1 !important; }' });
    await seite.waitForTimeout(300);

    const soll = await seite.evaluate(() => JSON.parse(document.getElementById('uep-umfeld-soll').getAttribute('data-soll')));
    const frei = await seite.evaluate(() => {
      const r = document.querySelector('.epos-ergebnis-fluss-bild').getBoundingClientRect();
      return { x: r.x + r.width / 2, y: r.bottom + 40 };
    });

    // Je ein Wandler und ein Bus mit Element im Bild; der Wandler mit dem groessten Umfeld (der Weg reicht ueber Nachbarn).
    const imBild = await seite.evaluate(BREIT => [...document.querySelectorAll(BREIT + ' svg [data-marke^="knoten:"]')]
      .map(e => e.getAttribute('data-marke').substring(7)), BREIT);
    const kandidat = art => Object.entries(soll).filter(([id, s]) => s.art === art && imBild.includes(id))
      .sort((a, b) => b[1].umfeld.length - a[1].umfeld.length)[0];
    const knoten = [['Wandler', kandidat('Wandler')], ['Bus', kandidat('Bus')]];
    for (const [art, k] of knoten) {
      if (!k) { befunde.push(`kein ${art} im Bild`); continue; }
      const [id, s] = k;
      const p = await punktFuer(seite, 'knoten:' + id);
      if (!p) { befunde.push(`${art} ${id}: kein freier Punkt`); continue; }
      await seite.mouse.move(frei.x, frei.y);
      await seite.mouse.move(p.x, p.y);
      await seite.waitForTimeout(150);
      const st = await hervorStand(seite);
      const sollGezeichnet = s.umfeld.filter(m => st.gezeichnet.includes(m)).sort();
      const gleich = JSON.stringify(sollGezeichnet) === JSON.stringify(st.flussAn);
      faelle.push(`${art} ${id}: ${st.flussAn.length}/${sollGezeichnet.length} Baender`);
      if (sollGezeichnet.length === 0) befunde.push(`${art} ${id}: Soll ohne gezeichnetes Band`);
      if (!gleich) befunde.push(`${art} ${id}: hervorgehoben [${st.flussAn.join(', ')}] statt [${sollGezeichnet.join(', ')}]`);
      if (st.ausZahl === 0) befunde.push(`${art} ${id}: kein Band ausserhalb des Umfelds`);
      else if (!st.abgeblendet) befunde.push(`${art} ${id}: Baender ausserhalb nicht abgeblendet`);
      if (!st.voll) befunde.push(`${art} ${id}: Umfeld nicht voll`);
      if (!st.stufenVoll) befunde.push(`${art} ${id}: Legende oder Stufentitel abgeblendet`);
    }

    // Maus aus dem Bild: nichts mehr abgeblendet.
    await seite.mouse.move(frei.x, frei.y);
    await seite.waitForTimeout(150);
    const leer = await hervorStand(seite);
    if (leer.hervor || leer.minDeck < 0.99) befunde.push('nach dem Verlassen bleibt etwas abgeblendet');

    // Stufen: ein Verbinderband (eine Marke, mehrere Pfade) leuchtet allein, mit beiden Stummeln.
    await seite.click('[data-gruppe="darstellung"] [data-wahl="stufen"]');
    await seite.waitForFunction(B => document.querySelectorAll(B + ' svg [data-marke^="stufe:"]').length > 0, BREIT, { timeout: 10000 })
      .catch(() => befunde.push('Stufen erscheinen nicht'));
    await seite.waitForTimeout(300);
    const verbinder = await seite.evaluate(BREIT => {
      const zahl = new Map();
      for (const e of document.querySelectorAll(BREIT + ' svg path[data-marke^="fluss:"]'))
        zahl.set(e.getAttribute('data-marke'), (zahl.get(e.getAttribute('data-marke')) || 0) + 1);
      return [...zahl.entries()].filter(([, n]) => n > 1).map(([m, n]) => ({ m, n }));
    }, BREIT);
    if (verbinder.length === 0) befunde.push('kein Verbinderband in den Stufen');
    else {
      const v = verbinder[0];
      const p = await punktFuer(seite, v.m, 0);
      if (!p) befunde.push(`Verbinder ${v.m}: kein freier Punkt`);
      else {
        await seite.mouse.move(frei.x, frei.y);
        await seite.mouse.move(p.x, p.y);
        await seite.waitForTimeout(150);
        const st = await hervorStand(seite);
        const pfadeAn = await seite.evaluate(({ BREIT, m }) => [...document.querySelectorAll(BREIT + ' svg path[data-marke]')]
          .filter(e => e.getAttribute('data-marke') === m && e.classList.contains('epos-fluss-an')).length, { BREIT, m: v.m });
        faelle.push(`Verbinder ${v.m}: ${pfadeAn}/${v.n} Pfade`);
        if (JSON.stringify(st.flussAn) !== JSON.stringify([v.m])) befunde.push(`Verbinder ${v.m}: hervorgehoben [${st.flussAn.join(', ')}]`);
        if (pfadeAn !== v.n) befunde.push(`Verbinder ${v.m}: ${pfadeAn} von ${v.n} Pfaden hervorgehoben`);
        if (!st.abgeblendet) befunde.push(`Verbinder ${v.m}: Rest nicht abgeblendet`);
      }
    }
    if (FOTOS) {
      await mkdir(FOTOS, { recursive: true });
      await seite.locator('.epos-ergebnis-fluss').screenshot({ path: `${FOTOS}/hervorhebung_${gegen || 'probe'}.png` });
    }
    return { faelle, befunde };
  } finally {
    await seite.close();
  }
}

/** FALL "wahl": Darstellung und Formfilter wechseln das Bild, ein leerer Filter nennt seinen Grund. */
async function laufWahl(browser) {
  const seite = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const befunde = [];
  try {
    await seite.goto(WURZEL + '/uebersichtprobe?ansicht=fluss', { waitUntil: 'networkidle' });
    await seite.waitForSelector(BREIT + ' svg', { timeout: 20000 });
    const zaehle = () => seite.evaluate(BREIT => {
      const svg = document.querySelector(BREIT + ' svg');
      const marken = new Set([...svg.querySelectorAll('[data-marke^="fluss:"]')].map(e => e.getAttribute('data-marke')));
      return { baender: marken.size, stufen: svg.querySelectorAll('[data-marke^="stufe:"]').length,
               hinweis: svg.querySelectorAll('[data-marke="hinweis"]').length,
               zeile: (document.querySelector('.epos-ergebnis-fluss-filterhinweis') || {}).textContent || '',
               darstellung: document.querySelector('.epos-ergebnis-fluss-bild').getAttribute('data-darstellung'),
               filter: document.querySelector('.epos-ergebnis-fluss-bild').getAttribute('data-filter') };
    }, BREIT);
    const warte = async (bed) => { const ende = Date.now() + 5000; let z = await zaehle();
      while (!bed(z) && Date.now() < ende) { await seite.waitForTimeout(50); z = await zaehle(); } return z; };

    const kette = await zaehle();
    if (kette.darstellung !== 'kette' || kette.filter !== 'alle') befunde.push(`Vorgabe ${kette.darstellung}/${kette.filter}`);
    if (kette.stufen !== 0) befunde.push('die Kette traegt Stufentitel');
    if (kette.baender < 3) befunde.push(`nur ${kette.baender} Baender in der Kette`);

    await seite.click('[data-gruppe="darstellung"] [data-wahl="stufen"]');
    const stufen = await warte(z => z.stufen > 0);
    if (stufen.stufen !== 4) befunde.push(`Stufen: ${stufen.stufen} Titel statt 4`);

    await seite.click('[data-gruppe="filter"] [data-wahl="strom"]');
    const strom = await warte(z => z.filter === 'strom');
    if (!(strom.baender > 0 && strom.baender < kette.baender)) befunde.push(`Strom: ${strom.baender} Baender (Alle ${kette.baender})`);

    await seite.click('[data-gruppe="filter"] [data-wahl="kaelte"]');
    const kaelte = await warte(z => z.filter === 'kaelte');
    if (kaelte.baender !== 0) befunde.push(`Kaelte: ${kaelte.baender} Baender`);
    if (kaelte.hinweis !== 1) befunde.push('Kaelte: kein Hinweis im Bild');
    if (!/K(ä|ae)lte/.test(kaelte.zeile)) befunde.push(`Kaelte: Hinweiszeile "${kaelte.zeile}"`);

    await seite.keyboard.press('ArrowLeft');
    const waerme = await warte(z => z.filter === 'waerme');
    const fokus = await seite.evaluate(() => document.activeElement && document.activeElement.getAttribute('data-wahl'));
    if (waerme.filter !== 'waerme') befunde.push(`Pfeil links: Filter ${waerme.filter}`);
    if (fokus !== 'waerme') befunde.push(`Pfeil links: Fokus auf ${fokus}`);
    if (FOTOS) {
      await mkdir(FOTOS, { recursive: true });
      await seite.locator('.epos-ergebnis-fluss').screenshot({ path: `${FOTOS}/wahl_waerme_stufen.png` });
    }
    return { zahlen: `Kette ${kette.baender} · Stufen ${stufen.stufen} Titel · Strom ${strom.baender} · Kaelte Hinweis ${kaelte.hinweis}`, befunde };
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
  if (FALL === 'hervorhebung') {
    const p = await laufHervorhebung(browser, null);
    console.log(`PROBE   Hervorhebung · ${p.faelle.join(' · ')}`);
    for (const b of p.befunde) console.log('  VERSTOSS: ' + b);
    if (p.befunde.length > 0) rc = 1;
    if (MIT_GEGENPROBE) {
      for (const gegen of ['ohne-modul', 'ohne-regel']) {
        const g = await laufHervorhebung(browser, gegen);
        const verfehlt = g.befunde.length > 0;
        console.log(`GEGEN   ${gegen}: ${verfehlt ? 'verfehlt (richtig)' : 'erfuellt (FALSCH)'}` +
                    (g.befunde.length ? ' - ' + g.befunde.slice(0, 3).join('; ') : ''));
        if (!verfehlt) rc = 1;
      }
    }
  } else if (FALL === 'wahl') {
    const p = await laufWahl(browser);
    console.log(`PROBE   Wahl · ${p.zahlen}`);
    for (const b of p.befunde) console.log('  VERSTOSS: ' + b);
    if (p.befunde.length > 0) rc = 1;
  } else if (FALL === 'fluss') {
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

  if (MIT_SCHRIFT) {
    const faelle = [];
    for (const fall of ['stromganglinie', 'solarganglinie', 'pvganglinie'])
      for (const huelle of ['', 'assistent'])
        for (const [b, h] of [[1400, 800], [1100, 720]])
          faelle.push([`/rollbereichprobe?fall=${fall}${huelle ? '&huelle=' + huelle : ''}&kultur=de-DE`, b, h,
                       fall === 'stromganglinie' && !huelle]);
    let n = 0;
    for (const [adresse, b, h, mitUeb] of faelle) {
      for (const { name, m } of await schriftFall(browser, adresse, '#app .epos-dialog', b, h, mitUeb)) {
        n++;
        const f = schriftBefunde(name, m);
        const spanne = m && m.groessen.length ? `${Math.min(...m.groessen).toFixed(1)}-${Math.max(...m.groessen).toFixed(1)} px` : '-';
        console.log(`SCHRIFT ${f.length ? 'VERSTOSS' : 'ok      '} ${name} · ${m ? m.viewBox : '-'} · ${spanne}`);
        for (const x of f) console.log('  VERSTOSS: ' + x);
        if (f.length) rc = 1;
      }
    }
    console.log(`SCHRIFT ${n} Bilder gemessen`);
    if (MIT_GEGENPROBE) {
      const [{ name, m }] = await schriftFall(browser, '/wikibild?bild=strombedarf&kultur=de-DE', '.epos-wikibild .epos-dialog', 1400, 800, false);
      const f = schriftBefunde(name, m);
      console.log(`GEGEN   Satz ausserhalb des Fensters: ${f.length ? 'verfehlt (richtig)' : 'erfuellt (FALSCH)'}` +
                  (f.length ? ' - ' + f.join('; ') : ''));
      if (!f.length) rc = 1;
    }
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
