// =====================================================================
//  LEGENDENPROBE - DIE RINGLEGENDE BRICHT NUR AN WORTGRENZEN
// =====================================================================
//
//  WOZU. Die Legende neben den Ringen der Ergebnisuebersicht (Waerme, Strom,
//  Kaelte) brach bei schmaler Spalte Buchstabe fuer Buchstabe um. Seitdem ist
//  die Ringzeile ein Container: unter 520 px steht die Legende unter dem Ring,
//  ein Eintrag ist hoechstens zweizeilig (Name oben, Menge und Anteil darunter),
//  und kein Name bricht im Wort (epos-ui.css, „Ring und Legende"). bunit misst
//  keine Breite - diese Probe misst sie auf der Seite /legendenprobe des Wirtes
//  (echter UebersichtReiter, synthetische Ringdaten, lange Namen).
//
//  JE FALL (Fensterbreite, Rahmenbreite) UND JE RINGZEILE:
//    - Lage: Ringzeile < 520 px -> Legende unter dem Ring, sonst daneben;
//    - kein Legendeneintrag hoeher als zwei Zeilenhoehen (Summenzeile mit ihrem
//      Rand von 5 px);
//    - kein Wort gebrochen: Breite jedes Namenselements >= Breite seines
//      laengsten Worts (per Range an einer ungebrochenen Kopie gemessen), und
//      jedes Wort liegt auf einer Zeile (Range.getClientRects);
//    - das Zahlenpaar steht innerhalb des Eintrags.
//
//  GEGENPROBE (laeuft mit, abschaltbar mit --ohne-gegenprobe): dieselben Faelle
//  mit der alten Regel (Raster neben dem Ring bis 620 px Fensterbreite,
//  overflow-wrap: anywhere am Namen) muessen Verstoesse liefern.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node legendenprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--ohne-gegenprobe]
//
//  Rueckgabe 0 = kein Verstoss und Gegenprobe rot, 1 = Verstoss oder Gegenprobe
//  gruen, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/legendenprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const GRENZE = 520;

// Fensterbreite und Breite des Rahmens um den Reiter (leer = volle Breite).
const FAELLE = [
  { name: '1280', breite: 1280, rahmen: '' },
  { name: '1280-rahmen-1100', breite: 1280, rahmen: '1100px' },
  { name: '1280-rahmen-760', breite: 1280, rahmen: '760px' },
  { name: '480', breite: 480, rahmen: '' },
  { name: '360', breite: 360, rahmen: '' },
];

// Die ALTE Regel, wie sie vor der Container-Abfrage stand; das Zahlenpaar wird
// aufgeloest, damit die vier Rasterspalten wieder Kaestchen, Name, Wert, Anteil sind.
const ALTE_REGEL = `
.epos-simueb-ringzeile { display: grid; grid-template-columns: minmax(0, 200px) minmax(0, 1fr); container-type: normal; }
@media (max-width: 620px) { .epos-simueb-ringzeile { grid-template-columns: minmax(0, 1fr); } }
.epos-simueb-legende li { display: grid; grid-template-columns: 14px minmax(0, 1fr) auto auto; }
.epos-simueb-legende-name { overflow-wrap: anywhere; }
.epos-simueb-legende-zahlen { display: contents; }
.epos-simueb-legende-anteil { min-width: 5ch; }
`;

function messen(grenze) {
  const erg = [];
  const zeilen = [...document.querySelectorAll('.epos-simueb-ringzeile')];
  zeilen.forEach((z, zi) => {
    const r = z.getBoundingClientRect();
    const ring = z.querySelector('.epos-simueb-ring').getBoundingClientRect();
    const leg = z.querySelector('.epos-simueb-legendenblock').getBoundingClientRect();
    const unter = leg.top >= ring.bottom - 1;
    const daneben = leg.left >= ring.right - 1 && leg.top < ring.bottom && leg.bottom > ring.top;
    const eintraege = [];
    for (const li of z.querySelectorAll('ul.epos-simueb-legende > li')) {
      const name = li.querySelector('.epos-simueb-legende-name');
      const zahlen = li.querySelector('.epos-simueb-legende-zahlen') || li.querySelector('.epos-simueb-legende-anteil');
      const text = name.textContent.trim();
      const knoten = [...name.childNodes].find(n => n.nodeType === 3 && n.textContent.trim().length > 0);
      // Zeilenhoehe: die Hoehe EINER Zeilenbox in der Schrift des Namens (Blockkopie).
      const probe = document.createElement('span');
      probe.style.cssText = 'position:absolute;visibility:hidden;display:block;white-space:nowrap;left:0;top:0';
      probe.textContent = 'Xg';
      name.appendChild(probe);
      const zh = probe.getBoundingClientRect().height;
      name.removeChild(probe);
      // Laengstes Wort, ungebrochen gemessen: Kopie mit nowrap im Namen, per Range.
      const woerter = text.split(/\s+/);
      const kopie = document.createElement('span');
      kopie.style.cssText = 'position:absolute;visibility:hidden;white-space:nowrap;left:0;top:0';
      name.appendChild(kopie);
      let laengstes = 0, wortLang = '';
      for (const w of woerter) {
        kopie.textContent = w;
        const wr = document.createRange(); wr.selectNodeContents(kopie);
        const b = wr.getBoundingClientRect().width;
        if (b > laengstes) { laengstes = b; wortLang = w; }
      }
      name.removeChild(kopie);
      // Liegt jedes Wort auf EINER Zeile?
      const gebrochen = [];
      if (knoten) {
        const t = knoten.textContent;
        let pos = 0;
        for (const w of woerter) {
          const a = t.indexOf(w, pos); if (a < 0) continue; pos = a + w.length;
          const wr = document.createRange(); wr.setStart(knoten, a); wr.setEnd(knoten, a + w.length);
          const tops = new Set([...wr.getClientRects()].filter(q => q.width > 0).map(q => Math.round(q.top)));
          if (tops.size > 1) gebrochen.push(w);
        }
      }
      const lb = li.getBoundingClientRect(), nb = name.getBoundingClientRect(), zb = zahlen.getBoundingClientRect();
      eintraege.push({
        text, hoehe: lb.height, zeilenhoehe: zh, summe: li.classList.contains('epos-simueb-legende-summe'),
        nameBreite: nb.width, laengstes, wortLang, gebrochen,
        zahlenInnen: zb.right <= lb.right + 1 && zb.left >= lb.left - 1,
      });
    }
    erg.push({ zeile: zi, breite: r.width, unter, daneben, eintraege });
  });
  return erg;
}

async function fall(browser, f, alt) {
  const kontext = await browser.newContext({ viewport: { width: f.breite, height: 900 } });
  const seite = await kontext.newPage();
  await seite.goto(WURZEL + '/legendenprobe?kultur=de-DE', { waitUntil: 'networkidle' });
  await seite.waitForSelector('.epos-simueb-kaelte .epos-simueb-ringzeile');
  if (f.rahmen) await seite.evaluate(b => { document.getElementById('legendenprobe-rahmen').style.width = b; }, f.rahmen);
  if (alt) await seite.addStyleTag({ content: ALTE_REGEL });
  await seite.waitForTimeout(150);
  const erg = await seite.evaluate(messen, GRENZE);
  if (FOTOS) {
    await mkdir(FOTOS, { recursive: true });
    await seite.screenshot({ path: `${FOTOS}/legende-${f.name}${alt ? '-alt' : ''}.png`, fullPage: true });
  }
  await kontext.close();
  return erg;
}

function pruefen(f, erg, melde) {
  if (erg.length !== 3) melde(f.name, `${erg.length} Ringzeilen statt 3`);
  for (const z of erg) {
    const ort = `${f.name} Ring ${z.zeile + 1} (${Math.round(z.breite)} px)`;
    if (z.breite < GRENZE && !z.unter) melde(ort, 'Legende steht nicht unter dem Ring');
    if (z.breite >= GRENZE && !z.daneben) melde(ort, 'Legende steht nicht neben dem Ring');
    for (const e of z.eintraege) {
      const grenze = 2 * e.zeilenhoehe + (e.summe ? 5 : 0) + 1;
      if (e.hoehe > grenze) melde(ort, `„${e.text}" ${e.hoehe.toFixed(1)} px hoch > ${grenze.toFixed(1)} px`);
      if (e.nameBreite + 0.5 < e.laengstes) melde(ort, `„${e.text}" Name ${e.nameBreite.toFixed(1)} px < Wort „${e.wortLang}" ${e.laengstes.toFixed(1)} px`);
      if (e.gebrochen.length) melde(ort, `„${e.text}" Wort gebrochen: ${e.gebrochen.join(', ')}`);
      if (!e.zahlenInnen) melde(ort, `„${e.text}" Zahlen ragen aus dem Eintrag`);
    }
  }
}

let browser;
try {
  browser = await chromium.launch();
} catch (e) { console.error('Aufbaufehler: ' + e.message); process.exit(2); }

let verstoesse = 0, gegen = 0;
try {
  for (const f of FAELLE) {
    const erg = await fall(browser, f, false);
    const zeilen = erg.map(z => {
      const max = Math.max(...z.eintraege.map(e => e.hoehe / e.zeilenhoehe));
      return `${Math.round(z.breite)} px ${z.unter ? 'unter' : z.daneben ? 'neben' : '?'} (max ${max.toFixed(2)} Zeilen)`;
    });
    console.log(`${f.name}: ${zeilen.join(' | ')}`);
    pruefen(f, erg, (o, t) => { verstoesse++; console.log('  VERSTOSS ' + o + ': ' + t); });
    if (MIT_GEGENPROBE) {
      const alt = await fall(browser, f, true);
      let n = 0;
      pruefen(f, alt, () => { n++; });
      gegen += n;
      console.log(`  Gegenprobe alte Regel: ${n} Verstoesse`);
    }
  }
} catch (e) { console.error('Aufbaufehler: ' + e.message); await browser.close(); process.exit(2); }
await browser.close();

const gegenRot = !MIT_GEGENPROBE || gegen > 0;
console.log(`\nErgebnis: ${verstoesse} Verstoesse, Gegenprobe ${MIT_GEGENPROBE ? (gegen > 0 ? 'rot (' + gegen + ')' : 'GRUEN') : 'aus'}`);
process.exit(verstoesse === 0 && gegenRot ? 0 : 1);
