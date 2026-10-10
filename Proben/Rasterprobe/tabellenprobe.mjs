// TABELLENPROBE — stehen Kopf und Wert der Ergebnistabellen uebereinander?
// (Auftrag TA, Reiter Waermepumpe: Modul- und Speichertabelle)
//
// Die Seite /tabellenprobe des Wirtes stellt den echten WaermepumpeReiter mit
// einem Modul und zwei Speichern. Je Tabelle und Zahlenspalte (th mit
// .epos-simerg-zahl) misst die Probe den rechten Rand des Kopftexts gegen den
// rechten Rand des Werts in jeder Datenzeile; je Textspalte die linken Raender.
// Soll: Abstand hoechstens 2 px. Dazu: keine Tabelle ist breiter als ihr
// Inhalt verlangt (sie fuellt den Rahmen nicht kuenstlich auf).
//
// Gegenprobe (abschaltbar mit --ohne-gegenprobe): derselbe Lauf mit der ALTEN
// Regel (Koepfe links, Tabelle 100 % breit) muss rot sein.
//
// Rueckgabe 0 = alle Sollwerte erfuellt, 1 = mindestens einer verfehlt,
// 2 = Aufruf- oder Verbindungsfehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/tabellenprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const TOLERANZ = 2;

const FAELLE = [
  { name: '1280', breite: 1280, rahmen: '' },
  { name: '1280-rahmen-760', breite: 1280, rahmen: '760px' },
  { name: '480', breite: 480, rahmen: '' },
];

// Der Stand vor dem Auftrag: Koepfe erben text-align: left, die Tabelle spannt 100 %.
const ALTE_REGEL = `
.epos-raster th.epos-simerg-zahl { text-align: left !important; }
table.epos-simerg-tabelle { width: 100% !important; }
`;

function messen() {
  const rand = el => {
    const r = document.createRange();
    r.selectNodeContents(el);
    const q = [...r.getClientRects()].filter(x => x.width > 0);
    if (q.length === 0) return null;
    return { links: Math.min(...q.map(x => x.left)), rechts: Math.max(...q.map(x => x.right)) };
  };
  const erg = [];
  const tabellen = [...document.querySelectorAll('#tabellenprobe-rahmen table.epos-raster')];
  tabellen.forEach((t, ti) => {
    const koepfe = [...t.querySelectorAll('thead tr:last-child > th')];
    const zeilen = [...t.querySelectorAll('tbody tr')]
      .map(z => [...z.children])
      .filter(z => z.length === koepfe.length);
    const spalten = koepfe.map((k, i) => {
      const zahl = k.classList.contains('epos-simerg-zahl');
      const kr = rand(k);
      const abstaende = zeilen.map(z => {
        const wr = rand(z[i]);
        if (!kr || !wr) return 0;
        return zahl ? Math.abs(kr.rechts - wr.rechts) : Math.abs(kr.links - wr.links);
      });
      return { kopf: k.textContent.trim(), zahl, max: Math.max(0, ...abstaende) };
    });
    // Inhaltsbreite: dieselbe Tabelle mit width:max-content gemessen; passt der
    // Inhalt nicht in den Behaelter, darf die Tabelle ueberstehen (nowrap).
    const breite = t.getBoundingClientRect().width;
    const behaelter = t.parentElement.getBoundingClientRect().width;
    const alt = t.style.width;
    t.style.width = 'max-content';
    const inhalt = t.getBoundingClientRect().width;
    t.style.width = alt;
    erg.push({ tabelle: ti, zeilen: zeilen.length, breite, inhalt, behaelter, spalten });
  });
  return erg;
}

async function fall(browser, f, alt) {
  const kontext = await browser.newContext({ viewport: { width: f.breite, height: 900 } });
  const seite = await kontext.newPage();
  await seite.goto(WURZEL + '/tabellenprobe?kultur=de-DE', { waitUntil: 'networkidle' });
  await seite.waitForSelector('#tabellenprobe-rahmen table.epos-raster tbody tr');
  if (f.rahmen) await seite.evaluate(b => { document.getElementById('tabellenprobe-rahmen').style.width = b; }, f.rahmen);
  if (alt) await seite.addStyleTag({ content: ALTE_REGEL });
  await seite.waitForTimeout(150);
  const erg = await seite.evaluate(messen);
  if (FOTOS) {
    await mkdir(FOTOS, { recursive: true });
    await seite.screenshot({ path: `${FOTOS}/tabelle-${f.name}${alt ? '-alt' : ''}.png`, fullPage: true });
  }
  await kontext.close();
  return erg;
}

function pruefen(f, erg, melde) {
  if (erg.length !== 2) melde(f.name, `${erg.length} Tabellen statt 2 (Modul und Speicher)`);
  for (const t of erg) {
    const ort = `${f.name} Tabelle ${t.tabelle + 1}`;
    if (t.zeilen === 0) melde(ort, 'keine Datenzeile');
    if (t.inhalt <= t.behaelter && t.breite > t.inhalt + 1) melde(ort, `breiter als ihr Inhalt (${t.breite.toFixed(1)} > ${t.inhalt.toFixed(1)} px)`);
    for (const s of t.spalten)
      if (s.max > TOLERANZ)
        melde(ort, `Spalte „${s.kopf}“ (${s.zahl ? 'Zahl, rechte' : 'Text, linke'} Raender) ${s.max.toFixed(1)} px auseinander`);
  }
}

let browser;
try {
  browser = await chromium.launch();
} catch (e) {
  console.error('Chromium startet nicht: ' + e.message);
  process.exit(2);
}

let rot = 0;
try {
  for (const f of FAELLE) {
    const fehler = [];
    const erg = await fall(browser, f, false);
    pruefen(f, erg, (o, t) => fehler.push(`${o}: ${t}`));
    const groesste = Math.max(0, ...erg.flatMap(t => t.spalten.map(s => s.max)));
    const zahlen = erg.map(t => t.spalten.filter(s => s.zahl).length).join('+');
    console.log(`${fehler.length ? 'ROT ' : 'GRÜN'} ${f.name}: Zahlenspalten ${zahlen}, größter Abstand ${groesste.toFixed(2)} px, `
      + `Breiten ${erg.map(t => Math.round(t.breite)).join('/')} px`);
    for (const z of fehler) console.log('     ' + z);
    if (fehler.length) rot++;
    if (process.env.TABELLENPROBE_JSON) console.log(JSON.stringify(erg));
  }

  if (MIT_GEGENPROBE) {
    const f = FAELLE[0];
    const fehler = [];
    const erg = await fall(browser, f, true);
    pruefen(f, erg, (o, t) => fehler.push(`${o}: ${t}`));
    const groesste = Math.max(0, ...erg.flatMap(t => t.spalten.map(s => s.max)));
    console.log(`${fehler.length ? 'GRÜN' : 'ROT '} Gegenprobe (alte Regel) ${f.name}: ${fehler.length} Verstöße, `
      + `größter Abstand ${groesste.toFixed(2)} px`);
    if (!fehler.length) rot++;
  }
} catch (e) {
  console.error('Verbindungsfehler: ' + e.message);
  await browser.close();
  process.exit(2);
}
await browser.close();
process.exit(rot ? 1 : 0);
