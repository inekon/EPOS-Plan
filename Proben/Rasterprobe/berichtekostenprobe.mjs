// =====================================================================
//  SICHTPROBE „BERICHTE & KOSTEN" (Konzept Navigation, Variante A, A4)
// =====================================================================
//
//  WOZU. bunit misst weder Maß noch Umbruch (EPOS.UI/CLAUDE.md). Die Regeln der
//  Reiterzeile sind aber Aussagen über KÄSTEN: vier Reiter mit Statuszeile,
//  jeder ≥ 44 px hoch; ab 900 px die lange Zeile und das Leistenende (Stamm,
//  Umschalter, Hilfe, Rückweg) in DERSELBEN Zeile wie die Reiter, darunter die
//  Kurzform und das Ende in einer eigenen Zeile; die Warnung in der Warnfarbe
//  des Hauses; keine Seite, die quer rollt. Diese Probe misst sie auf der Seite
//  /berichtekosten des Wirtes bei 1 280 × 900 und 820 × 1 180 (iPad hochkant).
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node berichtekostenprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/fotos
//
//  Rückgabe 0 = kein Verstoß, 1 = mindestens einer, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/berichtekostenprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const WARNFARBE = 'rgb(138, 91, 0)';   // --epos-warn-text

const FAELLE = [
  { name: 'b1280_uebersicht', breite: 1280, hoehe: 900, adresse: '/berichtekosten?kultur=de-DE' },
  { name: 'b1280_bericht', breite: 1280, hoehe: 900, adresse: '/berichtekosten?kultur=de-DE&seite=BERICHT' },
  { name: 'b1280_ansicht', breite: 1280, hoehe: 900, adresse: '/berichtekosten?kultur=de-DE&ansicht=1&seite=KOSTEN' },
  { name: 'b1280_langer_stamm', breite: 1280, hoehe: 900,
    adresse: '/berichtekosten?kultur=de-DE&stamm=' + encodeURIComponent('Sehr langer Name eines Stammprojekts mit Nahwärmenetz und drei Gebäuden') },
  { name: 'b820_uebersicht', breite: 820, hoehe: 1180, adresse: '/berichtekosten?kultur=de-DE' },
  { name: 'b820_ansicht', breite: 820, hoehe: 1180, adresse: '/berichtekosten?kultur=de-DE&ansicht=1&seite=WIRTSCHAFT' },
];

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };

let browser;
try {
  browser = await chromium.launch();
  if (FOTOS) await mkdir(FOTOS, { recursive: true });

  for (const f of FAELLE) {
    const seite = await browser.newPage({ viewport: { width: f.breite, height: f.hoehe } });
    await seite.goto(WURZEL + f.adresse);
    await seite.waitForSelector('button[id^="bk-reiter-"]', { timeout: 15000 });
    await seite.waitForTimeout(500);

    const m = await seite.evaluate(() => {
      const r = (e) => { const b = e.getBoundingClientRect(); return { x: b.x, y: b.y, w: b.width, h: b.height }; };
      const zu = (sel, k) => { const e = k.querySelector(sel); return e ? getComputedStyle(e).display !== 'none' : null; };
      const kopf = document.querySelector('.epos-berichtekosten > .epos-reiter > .epos-reiter-kopfzeile');
      const ende = kopf.querySelector(':scope > .epos-reiter-leistenende');
      const warn = document.querySelector('.epos-reiter-status--warnung');
      return {
        querrollen: document.documentElement.scrollWidth > window.innerWidth,
        stilblatt: getComputedStyle(document.querySelector('.epos-reiter-knopf')).minHeight,
        knoepfe: [...document.querySelectorAll('button[id^="bk-reiter-"]')].map(k => ({
          id: k.id, ...r(k), lang: zu('.epos-reiter-status-lang', k), kurz: zu('.epos-reiter-status-kurz', k),
          text: k.innerText.replace(/\s+/g, ' ').trim() })),
        warnfarbe: warn ? getComputedStyle(warn).color : null,
        leiste: r(kopf.querySelector(':scope > .epos-reiter-leiste')),
        ende: r(ende),
        endeKinder: [...ende.children].map(k => ({ klasse: k.className, ...r(k) })),
      };
    });
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${f.name}.png` });

    console.log(`${f.name}: ${m.knoepfe.map(k => k.text).join(' | ')}`);
    if (m.stilblatt !== '44px') melde(f.name, 'Stilblatt nicht geladen (min-height ' + m.stilblatt + ')');
    if (m.querrollen) melde(f.name, 'die Seite rollt quer');
    if (m.knoepfe.length !== 4) melde(f.name, `${m.knoepfe.length} statt 4 Reiter`);
    for (const k of m.knoepfe) if (k.h < 44) melde(f.name, `${k.id} ist ${k.h} px hoch`);
    if (m.warnfarbe !== WARNFARBE) melde(f.name, 'Warnfarbe ' + m.warnfarbe);
    if (f.breite > 900) {
      if (m.knoepfe.some(k => k.lang === false)) melde(f.name, 'lange Statuszeile verborgen');
      if (m.ende.y >= m.leiste.y + m.leiste.h - 1) melde(f.name, 'Leistenende nicht neben den Reitern');
    } else {
      if (m.knoepfe.some(k => k.kurz === false)) melde(f.name, 'Kurzform fehlt');
      if (m.ende.y < m.leiste.y + m.leiste.h - 1) melde(f.name, 'Leistenende nicht in eigener Zeile');
    }
    for (const k of m.endeKinder)
      if (k.x + k.w > f.breite + 0.5 || k.x < -0.5) melde(f.name, `${k.klasse} ragt aus dem Fenster`);
    await seite.close();
  }
} catch (e) {
  console.error('AUFBAUFEHLER: ' + (e && e.message ? e.message : e));
  if (browser) await browser.close();
  process.exit(2);
}
await browser.close();
console.log(verstoesse.length === 0 ? 'Sichtprobe Berichte & Kosten: kein Verstoß' : `${verstoesse.length} Verstöße`);
process.exit(verstoesse.length === 0 ? 0 : 1);
