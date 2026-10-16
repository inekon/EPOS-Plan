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
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node diagrammprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--ohne-gegenprobe] [--ohne-schrift]
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

let browser;
try {
  browser = await chromium.launch();
} catch (e) {
  console.error('Aufbaufehler: Chromium startet nicht - ' + e.message);
  process.exit(2);
}

let rc = 0;
try {
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
} catch (e) {
  console.error('Aufbaufehler: ' + (e && e.stack || e));
  rc = 2;
} finally {
  await browser.close();
}
console.log(rc === 0 ? 'ERGEBNIS: alle Sollwerte erfuellt.' : 'ERGEBNIS: VERFEHLT.');
process.exit(rc);
