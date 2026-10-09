// =====================================================================
//  KALENDERPROBE (Kalenderbedienung Stufe 2) — die Kalenderbedienung im Reiter „Konditionierung“
//  des Gebäude-Katalogeditors im echten Browser
// =====================================================================
//
//  WOZU. Die Kalenderbedienung (EPOS.UI/Dialoge/Bedarf/KalenderbedienungAbschnitt.razor) ordnet
//  Jahresraster, Zuordnungstabelle und Schnellfelder über Stilregeln an; bunit hat kein Layout
//  (EPOS.UI/CLAUDE.md). Diese Probe öffnet auf der Seite /konditionierungsprobe?fall=karte des
//  Wirtes den Reiter „Konditionierung“, klappt die Karte „Heizen“ im Einzelnen auf und misst:
//    - 1 280 × 800: das Jahresraster mit 12 Monaten × 31 Zellen (365 Tage, 7 Leerzellen) in
//      12 Zeilen, jede Tageszelle mindestens 20 px hoch;
//    - die Maske „gilt für“ je Zeile: eine neu angelegte gemeinsame Zeile trägt fünf
//      Maskenknöpfe, ein Klick schaltet den Knopf um;
//    - der Block „Benannte Wochen“ und die Schnellfelder Wochenende (sieben Tagesknöpfe) und
//      Feiertagsland stehen sichtbar;
//    - 1 024 × 700: kein horizontales Rollen von Seite, Überlagerung, Kalenderbedienung und
//      Rollkasten des Jahresrasters;
//    - die Konsole bleibt fehlerfrei (console.error und Seitenfehler).
//  GEGENPROBE: dieselbe Messung in einem absichtlich verengten Fenster (360 × 700) mit den Sollwerten
//  des Breitfensters — sie MUSS rot werden, sonst misst die Probe nichts.
//
//  Aufruf (Wirt wie in LIESMICH.md, Abschnitt „Aufruf“):
//    node kalenderprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner außerhalb des Repositorys>] [--ohne-gegenprobe]
//  Rückgabe 0 = alle Sollwerte erfüllt (und die Gegenprobe rot), 1 = Verstoß, 2 = Aufruf- oder Verbindungsfehler.
// =====================================================================

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/kalenderprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const ZELLE_MIN = 20;
const schlaf = ms => new Promise(r => setTimeout(r, ms));

/** Öffnet die Karte „Heizen“ im Einzelnen; liefert Seite und die gesammelten Konsolenfehler. */
async function oeffnen(browser, breite, hoehe) {
  const kontext = await browser.newContext({ viewport: { width: breite, height: hoehe } });
  const seite = await kontext.newPage();
  const fehler = [];
  seite.on('console', m => { if (m.type() === 'error') fehler.push(m.text()); });
  seite.on('pageerror', e => fehler.push(String(e && e.message ? e.message : e)));
  await seite.goto(`${WURZEL}/konditionierungsprobe?fall=karte&kultur=de-DE`, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector('.epos-ueberlagerung', { timeout: 30000 });
  await schlaf(800);
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(500);
  await seite.locator('.epos-ueberlagerung .epos-kond-karte[data-groesse="0"] button.epos-kond-einzelheiten').click();
  await seite.waitForSelector('.epos-ueberlagerung .epos-kalb', { timeout: 10000 });
  await schlaf(600);
  return { kontext, seite, fehler };
}

/** Misst Jahresraster, Blöcke und Querrollen im Browser. */
const MESSEN = () => {
  const w = document.querySelector('.epos-ueberlagerung .epos-kalb');
  if (!w) return { fehlt: true };
  const sichtbar = e => !!e && e.getClientRects().length > 0 && e.getBoundingClientRect().height > 0;
  const jahr = w.querySelector('.epos-kalb-jahr');
  const zellen = jahr ? [...jahr.children].filter(e => !e.classList.contains('epos-kalb-monat')) : [];
  const tage = zellen.filter(e => e.classList.contains('epos-kalb-jahrtag'));
  const zeilen = new Set(zellen.map(e => Math.round(e.getBoundingClientRect().top)));
  const quer = e => (e ? e.scrollWidth - e.clientWidth : null);
  const ueb = document.querySelector('.epos-ueberlagerung');
  // Das rollende Kind der Überlagerung (der Dialogkasten) zählt mit.
  const uebQuer = Math.max(quer(ueb), ...[...ueb.querySelectorAll('*')]
    .filter(e => ['auto', 'scroll'].includes(getComputedStyle(e).overflowX) && !e.closest('.epos-kalb'))
    .map(quer));
  return {
    monate: jahr ? jahr.querySelectorAll('.epos-kalb-monat').length : 0,
    zellen: zellen.length,
    tage: tage.length,
    zeilen: zeilen.size,
    tagMin: tage.length ? Math.min(...tage.map(e => e.getBoundingClientRect().height)) : 0,
    wochen: sichtbar(w.querySelector('.epos-kalb-wochen')) && sichtbar(w.querySelector('.epos-kalb-woche-anlegen')),
    wochenende: [...w.querySelectorAll('.epos-kalb-wochenende button.epos-kalb-wochenendtag')].filter(sichtbar).length,
    land: sichtbar(w.querySelector('.epos-kalb-land select')) || sichtbar(w.querySelector('.epos-kalb-land')),
    seiteQuer: quer(document.scrollingElement),
    uebQuer,
    kalbQuer: quer(w),
    rolleQuer: quer(w.querySelector('.epos-kalb-rolle'))
  };
};

const verstoesse = [];
const melde = (marke, text) => { verstoesse.push(`${marke}: ${text}`); console.log(`  VERSTOSS ${marke}: ${text}`); };

/** Legt eine gemeinsame Zeile an und schaltet einen Knopf ihrer Maske „gilt für“. */
async function maske(seite, marke) {
  // Ein zweiter angelegter Kalender (Lüftung), damit die Maske mehr als eine wirksame Größe trägt.
  const anlegen = seite.locator('.epos-ueberlagerung .epos-kond-karte[data-groesse="2"] button.epos-kond-anlegen');
  if (await anlegen.count() > 0) { await anlegen.first().click(); await schlaf(600); }
  const w = seite.locator('.epos-ueberlagerung .epos-kalb');
  await w.locator('button.epos-kalb-zeile-neu').click(); await schlaf(400);
  const editor = w.locator('.epos-kalb-zuordnung .epos-kalb-editor');
  await editor.locator('input[type=date]').nth(0).fill('2025-03-10'); await schlaf(200);
  await editor.locator('input[type=date]').nth(1).fill('2025-03-14'); await schlaf(200);
  await editor.locator('input:not([type=date])').first().fill('Probezeile'); await schlaf(200);
  // Wirkung „aus“ (KalenderWirkung.Aus = 1): eine Angabe für alle Größen — die Zeile wird EINE gemeinsame Zeile.
  await editor.locator('select').first().selectOption('1'); await schlaf(300);
  await editor.locator('button.epos-kalb-uebernehmen').click(); await schlaf(600);
  const bits = w.locator('tr.epos-kalb-zeile[data-name="Probezeile"] .epos-kalb-maske button.epos-kalb-maskenbit');
  const anzahl = await bits.count();
  if (anzahl !== 5) {
    const namen = await seite.locator('.epos-ueberlagerung tr.epos-kalb-zeile').evaluateAll(z => z.map(x => x.dataset.name + " [" + x.innerText.replace(/\s+/g, " ") + "]"));
    melde(marke, `die Zeile „Probezeile“ trägt ${anzahl} statt 5 Maskenknöpfe „gilt für“ (Zeilen: ${namen.join(', ')})`);
    return;
  }
  const stand = async () => (await w.locator('tr.epos-kalb-zeile[data-name="Probezeile"] .epos-kalb-maske button.epos-kalb-maskenbit')
    .evaluateAll(k => k.map(b => b.getAttribute('aria-pressed')))).join(' ');
  const vorher = await stand();
  await bits.nth(2).click(); await schlaf(600);                                  // die Lüftung
  const nachher = await stand();
  console.log(`  Maske „gilt für“: ${anzahl} Knöpfe, Lüftung geklickt: [${vorher}] → [${nachher}]`);
  if (vorher === nachher) melde(marke, 'ein Klick auf die Maske „gilt für“ schaltet den Knopf nicht um');
}

/** Ein Lauf: Fenster öffnen, messen, Sollwerte prüfen. */
async function lauf(browser, marke, breite, hoehe, { raster, quer, mitMaske }) {
  const { kontext, seite, fehler } = await oeffnen(browser, breite, hoehe);
  try {
    const m = await seite.evaluate(MESSEN);
    if (m.fehlt) { melde(marke, 'die Kalenderbedienung fehlt'); return; }
    console.log(`  ${breite} × ${hoehe}: Jahresraster ${m.monate} Monate, ${m.zellen} Zellen (${m.tage} Tage) in ${m.zeilen} Zeilen, ` +
                `Tageszelle min ${m.tagMin.toFixed(1)} px; Benannte Wochen ${m.wochen ? 'sichtbar' : 'fehlt'}, ` +
                `Wochenende ${m.wochenende} Tagesknöpfe, Feiertagsland ${m.land ? 'sichtbar' : 'fehlt'}; ` +
                `quer: Seite ${m.seiteQuer}, Überlagerung ${m.uebQuer}, Kalenderbedienung ${m.kalbQuer}, Rollkasten ${m.rolleQuer}`);
    if (raster) {
      if (m.monate !== 12 || m.zellen !== 12 * 31 || m.tage !== 365)
        melde(marke, `Jahresraster ${m.monate} × ${m.zellen} Zellen, ${m.tage} Tage (soll 12, 372, 365)`);
      if (m.zeilen !== 12) melde(marke, `das Jahresraster steht in ${m.zeilen} statt 12 Zeilen`);
      if (m.tagMin < ZELLE_MIN) melde(marke, `Tageszelle ${m.tagMin.toFixed(1)} px (soll ≥ ${ZELLE_MIN})`);
      if (!m.wochen) melde(marke, 'der Block „Benannte Wochen“ ist nicht sichtbar');
      if (m.wochenende !== 7) melde(marke, `Schnellfeld Wochenende mit ${m.wochenende} statt 7 sichtbaren Tagesknöpfen`);
      if (!m.land) melde(marke, 'das Schnellfeld Feiertagsland ist nicht sichtbar');
    }
    if (quer && (m.seiteQuer > 0 || m.uebQuer > 0 || m.kalbQuer > 0 || m.rolleQuer > 0))
      melde(marke, `horizontales Rollen (Seite ${m.seiteQuer}, Überlagerung ${m.uebQuer}, Kalenderbedienung ${m.kalbQuer}, Rollkasten ${m.rolleQuer})`);
    if (mitMaske) await maske(seite, marke);
    if (FOTOS) {
      await seite.locator('.epos-ueberlagerung .epos-kalb-jahresraster').scrollIntoViewIfNeeded();
      await seite.screenshot({ path: `${FOTOS}/kalender_${breite}x${hoehe}.png` });
    }
    console.log(`  Konsole: ${fehler.length} Fehler`);
    for (const f of fehler.slice(0, 5)) melde(marke, `Konsolenfehler: ${f.slice(0, 200)}`);
  } finally {
    await kontext.close();
  }
}

let browser;
let gegenprobeRot = true;
try {
  if (FOTOS) await mkdir(FOTOS, { recursive: true });
  browser = await chromium.launch({ headless: true });
  console.log('Kalenderprobe — /konditionierungsprobe?fall=karte, Karte „Heizen“ im Einzelnen');
  await lauf(browser, 'raster', 1280, 800, { raster: true, quer: false, mitMaske: true });
  await lauf(browser, 'schmal', 1024, 700, { raster: false, quer: true, mitMaske: false });
  if (MIT_GEGENPROBE) {
    console.log('Gegenprobe — verengtes Fenster 360 × 700 mit den Sollwerten des Breitfensters (muss rot werden)');
    const zuvor = verstoesse.length;
    await lauf(browser, 'gegenprobe', 360, 700, { raster: true, quer: true, mitMaske: false });
    const neu = verstoesse.splice(zuvor);
    gegenprobeRot = neu.length > 0;
    console.log(gegenprobeRot ? `  Gegenprobe rot (${neu.length} Verstöße) — die Probe misst.` : '  GEGENPROBE GRÜN — die Probe misst nichts.');
  }
} catch (e) {
  console.error('AUFBAUFEHLER: ' + (e && e.message ? e.message : e));
  if (browser) await browser.close();
  process.exit(2);
}
await browser.close();
console.log('');
const gut = verstoesse.length === 0 && gegenprobeRot;
console.log(gut ? 'KALENDERPROBE: kein Verstoß.'
                : `KALENDERPROBE: ${verstoesse.length} Verstöße${gegenprobeRot ? '' : ', Gegenprobe nicht rot'}.`);
process.exit(gut ? 0 : 1);
