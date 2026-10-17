// =====================================================================
//  KENNZAHLENPROBE - KEINE KACHEL DER KENNZAHLENZEILE UEBERLAPPT
// =====================================================================
//
//  WOZU. In der Kennzahlenzeile der Ergebnisuebersicht (Waerme, Strom, Kaelte)
//  ragten bei schmaler Spalte Zahl und Einheit des Bedarfs in die Beschriftung
//  der Nachbarkachel „Deckung durch Erzeuger". Seitdem ist das Kennzahlenband
//  ein Container: Keine Kachel wird schmaler als ihr Inhalt, eine Kachel ohne
//  Platz rueckt in die naechste Zeile, unter 480 px stehen die Kacheln
//  untereinander (epos-ui.css, „Das Kennzahlenband"). bunit misst keine Breite -
//  diese Probe misst sie auf der Seite /legendenprobe des Wirtes (echter
//  UebersichtReiter, synthetische Kennzahlen, Kaelte mit Erzeuger).
//
//  JE FALL (Rahmenbreite) UND JE KENNZAHLENBAND:
//    - jedes Inhaltsstueck einer Kachel (Beschriftung, Zahl samt Einheit; per
//      Range gemessen, also die Ausdehnung des Textes, nicht die des Elements)
//      liegt innerhalb seiner Kachel;
//    - kein Inhaltsstueck einer Kachel schneidet ein Inhaltsstueck oder den
//      Kasten einer anderen Kachel (Bounding-Box-Vergleich);
//    - keine zwei Kacheln ueberschneiden sich;
//    - Zahl und Einheit liegen auf EINER Zeile;
//    - kein Wort der Beschriftung ist gebrochen (Range.getClientRects).
//
//  GEGENPROBE (laeuft mit, abschaltbar mit --ohne-gegenprobe): dieselben Faelle
//  (a) mit der alten Regel (Drittelraster mit minmax(0, 1fr), min-width: 0) und
//  (b) mit einer erzwungenen Ueberlappung (die Zahl jeder Folgekachel 80 px nach
//  links verschoben) muessen je Verstoesse liefern.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node kennzahlenprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--ohne-gegenprobe]
//
//  Rueckgabe 0 = kein Verstoss und beide Gegenproben rot, 1 = Verstoss oder eine
//  Gegenprobe gruen, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/kennzahlenprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');

// Fensterbreite 1 280 (zwei Spalten), Breite des Rahmens um den Reiter.
const FAELLE = [
  { name: '1100', breite: 1280, rahmen: '1100px' },
  { name: '760', breite: 1280, rahmen: '760px' },
  { name: '600', breite: 1280, rahmen: '600px' },
];

const GEGENPROBEN = [
  { name: 'alte Regel', css: `
.epos-simueb-kennzahlen { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); container-type: normal; }
.epos-simueb-kennzahl { min-width: 0; display: block; border-right: 1px solid; border-bottom: 0; padding: 10px; }
.epos-simueb-kennzahl-wert { margin-left: 0; }
` },
  { name: 'erzwungene Ueberlappung', css: `
.epos-simueb-kennzahl + .epos-simueb-kennzahl .epos-simueb-kennzahl-wert { position: relative; left: -80px; }
` },
];

function messen() {
  const rechteck = r => ({ l: r.left, r: r.right, o: r.top, u: r.bottom });
  const textRechteck = el => { const g = document.createRange(); g.selectNodeContents(el); return rechteck(g.getBoundingClientRect()); };
  const baender = [...document.querySelectorAll('.epos-simueb-kennzahlen')];
  return baender.map((band, bi) => {
    const kacheln = [...band.querySelectorAll(':scope > .epos-simueb-kennzahl')].map(k => {
      const text = k.querySelector('.epos-simueb-kennzahl-text');
      const wert = k.querySelector('.epos-simueb-kennzahl-wert');
      // Zahl und Einheit auf einer Zeile: Die Rechtecke des Werts (Zahl in 20 px, Einheit
      // kleiner) ueberdecken sich senkrecht; eine neue Zeile beginnt erst unter der letzten.
      const g = document.createRange(); g.selectNodeContents(wert);
      const rects = [...g.getClientRects()].filter(q => q.width > 0).sort((x, y) => x.top - y.top);
      let wertZeilen = 0, unten = -Infinity;
      for (const q of rects) { if (q.top >= unten - 1) wertZeilen++; unten = Math.max(unten, q.bottom); }
      // Woerter der Beschriftung je auf einer Zeile.
      const gebrochen = [];
      const knoten = [...text.childNodes].find(n => n.nodeType === 3 && n.textContent.trim().length > 0);
      if (knoten) {
        const t = knoten.textContent; let pos = 0;
        for (const w of t.trim().split(/\s+/)) {
          const a = t.indexOf(w, pos); if (a < 0) continue; pos = a + w.length;
          const wr = document.createRange(); wr.setStart(knoten, a); wr.setEnd(knoten, a + w.length);
          const tops = new Set([...wr.getClientRects()].filter(q => q.width > 0).map(q => Math.round(q.top)));
          if (tops.size > 1) gebrochen.push(w);
        }
      }
      return {
        name: text.textContent.trim(), wertText: wert.textContent.trim(),
        kasten: rechteck(k.getBoundingClientRect()),
        stuecke: [{ art: 'Beschriftung', box: textRechteck(text) }, { art: 'Zahl', box: textRechteck(wert) }],
        wertZeilen, gebrochen,
      };
    });
    return { band: bi, breite: band.getBoundingClientRect().width, kacheln };
  });
}

const schneiden = (a, b) => Math.min(a.r, b.r) - Math.max(a.l, b.l) > 0.5 && Math.min(a.u, b.u) - Math.max(a.o, b.o) > 0.5;
const innen = (s, k) => s.l >= k.l - 0.5 && s.r <= k.r + 0.5 && s.o >= k.o - 0.5 && s.u <= k.u + 0.5;

function pruefen(f, erg, melde) {
  if (erg.length !== 4) melde(f.name, `${erg.length} Kennzahlenbaender statt 4`);
  for (const b of erg) {
    const ort = `${f.name} Band ${b.band + 1} (${Math.round(b.breite)} px)`;
    b.kacheln.forEach((k, i) => {
      for (const s of k.stuecke)
        if (!innen(s.box, k.kasten)) melde(ort, `„${k.name}" ${s.art} ragt aus der Kachel`);
      if (k.wertZeilen !== 1) melde(ort, `„${k.name}" Zahl und Einheit auf ${k.wertZeilen} Zeilen`);
      if (k.gebrochen.length) melde(ort, `„${k.name}" Wort gebrochen: ${k.gebrochen.join(', ')}`);
      b.kacheln.forEach((n, j) => {
        if (j === i) return;
        if (j > i && schneiden(k.kasten, n.kasten)) melde(ort, `Kacheln „${k.name}" und „${n.name}" ueberschneiden sich`);
        for (const s of k.stuecke)
          if (schneiden(s.box, n.kasten)) melde(ort, `„${k.name}" ${s.art} schneidet Kachel „${n.name}"`);
      });
    });
  }
}

async function fall(browser, f, css, foto) {
  const kontext = await browser.newContext({ viewport: { width: f.breite, height: 900 } });
  const seite = await kontext.newPage();
  await seite.goto(WURZEL + '/legendenprobe?kultur=de-DE', { waitUntil: 'networkidle' });
  await seite.waitForSelector('.epos-simueb-kaelte .epos-simueb-kennzahlen');
  await seite.evaluate(b => { document.getElementById('legendenprobe-rahmen').style.width = b; }, f.rahmen);
  if (css) await seite.addStyleTag({ content: css });
  await seite.waitForTimeout(150);
  const erg = await seite.evaluate(messen);
  if (FOTOS && foto) {
    await mkdir(FOTOS, { recursive: true });
    await seite.screenshot({ path: `${FOTOS}/kennzahlen-${f.name}${foto}.png`, fullPage: true });
  }
  await kontext.close();
  return erg;
}

let browser;
try { browser = await chromium.launch(); } catch (e) { console.error('Aufbaufehler: ' + e.message); process.exit(2); }

let verstoesse = 0;
const gegen = GEGENPROBEN.map(() => 0);
try {
  for (const f of FAELLE) {
    const erg = await fall(browser, f, '', '-neu');
    const zeilen = erg.map(b => {
      const hoehen = new Set(b.kacheln.map(k => Math.round(k.kasten.o)));
      return `${Math.round(b.breite)} px ${hoehen.size === 1 ? 'nebeneinander' : hoehen.size === b.kacheln.length ? 'untereinander' : hoehen.size + ' Reihen'}`;
    });
    console.log(`Rahmen ${f.name}: ${zeilen.join(' | ')}`);
    pruefen(f, erg, (o, t) => { verstoesse++; console.log('  VERSTOSS ' + o + ': ' + t); });
    if (MIT_GEGENPROBE) {
      const teile = [];
      for (const [gi, g] of GEGENPROBEN.entries()) {
        const alt = await fall(browser, f, g.css, gi === 0 ? '-alt' : '');
        let n = 0; pruefen(f, alt, () => { n++; }); gegen[gi] += n;
        teile.push(`${g.name} ${n}`);
      }
      console.log(`  Gegenprobe: ${teile.join(', ')} Verstoesse`);
    }
  }
} catch (e) { console.error('Aufbaufehler: ' + e.message); await browser.close(); process.exit(2); }
await browser.close();

const gegenRot = !MIT_GEGENPROBE || gegen.every(n => n > 0);
console.log(`\nErgebnis: ${verstoesse} Verstoesse, Gegenprobe ${MIT_GEGENPROBE ? GEGENPROBEN.map((g, i) => `${g.name} ${gegen[i] > 0 ? 'rot (' + gegen[i] + ')' : 'GRUEN'}`).join(', ') : 'aus'}`);
process.exit(verstoesse === 0 && gegenRot ? 0 : 1);
