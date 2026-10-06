// =====================================================================
//  FOKUSPROBE — springt ein Dialog beim Öffnen?
// =====================================================================
//
//  WOZU. Fast jeder Dialog und jede Überlagerung setzt beim ersten Zeichnen
//  den Fokus auf seine Wurzel (`tabindex -1`), damit Esc und der Tabulator
//  dort beginnen. Ein Fokus OHNE preventScroll rollt Chromium so, dass das
//  Element „sichtbar" wird — ist der Dialog höher als sein Rollbehälter
//  (Fenster, Überlagerung, Seitenfläche), zentriert Chromium ihn und schiebt
//  Dialogkopf, Reiter oder Schlussleiste aus dem Bild. bunit misst keine
//  Rollposition; diese Probe öffnet im echten Chromium die langen Dialoge der
//  Prüfseiten des Wirts und misst nach dem Öffnen, BEVOR irgendwer rollt:
//    - die Rollposition des Fensters und JEDES Elements (scrollTop/scrollLeft),
//    - die Lage des obersten Dialogkopfs und der Schlussleiste,
//    - das Fokusziel.
//  Ein Verstoß ist jede Rollposition ungleich 0 und ein Dialogkopf über der
//  Oberkante seines Fensters.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node fokusprobe.mjs --url http://127.0.0.1:5299 [--nur <fall>] [--fotos <ordner>]
//    node fokusprobe.mjs --gegenprobe   # Fokus ohne preventScroll nachgestellt: muss rot sein
//
//  Rückgabe 0 = kein Verstoß (bzw. bei --gegenprobe: mindestens ein Verstoß),
//  1 = sonst, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/fokusprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const GEGENPROBE = process.argv.includes('--gegenprobe');

const FENSTER = [
  { name: '1280x600', breite: 1280, hoehe: 600 },
  { name: '820x700', breite: 820, hoehe: 700 },
];

// Die Fälle: Seite des Wirts, wahlweise ein Knopf, der die Überlagerung öffnet.
// Fensterdialoge stehen als Wurzel in #app (wie BlazorDialogForm), Katalogdialoge
// in der Prüffläche, Überlagerungen über ihrem Wirt.
const FAELLE = [
  ...['heizkessel', 'bhkw', 'waermepumpen', 'gebaeude', 'dubletten', 'katalog']
    .map(f => ({ name: 'fenster-' + f, pfad: `/fensterprobe?fall=${f}&kultur=de-DE` })),
  { name: 'ueberlagerung-gebaeude-simulation', pfad: '/fensterprobe?fall=gebaeude&kultur=de-DE', knopf: 'Simulation...' },
  ...['klima', 'bedarf', 'modul', 'waermebedarf', 'solar', 'projekt-heizkessel', 'stromganglinie', 'browser',
      'waermepumpe', 'gebaeude', 'projekt-gebaeude', 'gebaeudetyp', 'peak', 'tww', 'wohnungen', 'kategorien']
    .map(m => ({ name: 'katalog-' + m, pfad: `/katalogprobe?maske=${m}&zeilen=40&voll=1&kultur=de-DE` })),
  ...['projekt', 'gesamt', 'neu', 'vorlagen', 'bausteine', 'verwaltung']
    .map(f => ({ name: 'konditionierung-' + f, pfad: `/konditionierungsprobe?fall=${f}&kultur=de-DE` })),
  { name: 'gebaeudeimport', pfad: '/gebaeudeimport?kultur=de-DE' },
].filter(f => !NUR || f.name.startsWith(NUR));

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };

/** Alle Rollstände, das Fokusziel und die Lage von Kopf und Schlussleiste. */
const messen = () => {
  const beschreibe = (e) => {
    if (!e || e === document.body) return 'body';
    const k = (e.className && e.className.toString().trim().split(/\s+/)[0]) || '';
    return e.tagName.toLowerCase() + (e.id ? '#' + e.id : '') + (k ? '.' + k : '');
  };
  const rollende = [];
  for (const e of document.querySelectorAll('*'))
    if (e.scrollTop > 0 || e.scrollLeft > 0) rollende.push(beschreibe(e) + '=' + Math.round(e.scrollTop) + (e.scrollLeft ? '/' + Math.round(e.scrollLeft) : ''));
  const sichtbar = (e) => e && e.getClientRects().length > 0;
  const u = [...document.querySelectorAll('.epos-ueberlagerung')].filter(sichtbar).pop();
  const bereich = u || document;
  const kopf = [...bereich.querySelectorAll(u ? '.epos-ueberlagerung-kopf, .epos-dialog-kopf' : '.epos-dialog-kopf, [role="tablist"]')].find(sichtbar);
  const knopf = [...bereich.querySelectorAll('.epos-knopf--primaer')].filter(sichtbar).pop();
  const r = (e) => e ? e.getBoundingClientRect() : null;
  const k = r(kopf), l = r(knopf);
  return {
    fensterY: Math.round(window.scrollY),
    rollende,
    kopfOben: k ? Math.round(k.top) : null,
    leisteUnten: l ? Math.round(l.bottom) : null,
    fensterHoehe: window.innerHeight,
    dokumentHoehe: document.documentElement.scrollHeight,
    fokus: beschreibe(document.activeElement),
    ueberlagerung: !!u,
  };
};

let browser;
try {
  browser = await chromium.launch();
  if (FOTOS) await mkdir(FOTOS, { recursive: true });

  for (const f of FENSTER) {
    for (const fall of FAELLE) {
      const name = f.name + '_' + fall.name;
      const seite = await browser.newPage({ viewport: { width: f.breite, height: f.hoehe } });
      await seite.goto(WURZEL + fall.pfad);
      await seite.waitForSelector('.epos-dialog, .epos-ueberlagerung', { timeout: 15000 });
      await seite.waitForTimeout(800);
      if (fall.knopf) {
        const geklickt = await seite.evaluate((t) => {
          const k = [...document.querySelectorAll('button')].find(b => b.textContent.trim() === t);
          if (k) k.click();
          return !!k;
        }, fall.knopf);
        if (!geklickt) { melde(name, 'Knopf „' + fall.knopf + '" fehlt'); await seite.close(); continue; }
        await seite.waitForSelector('.epos-ueberlagerung', { timeout: 5000 });
        await seite.waitForTimeout(600);
      }

      if (GEGENPROBE) {
        // Der Stand vor der Behebung: dasselbe Fokusziel, Fokus ohne preventScroll.
        await seite.evaluate(() => {
          const a = document.activeElement;
          if (a && a !== document.body) { a.blur(); a.focus(); }
        });
        await seite.waitForTimeout(200);
      }

      const m = await seite.evaluate(messen);
      console.log(`  ${name.padEnd(44)} Fenster-Y ${String(m.fensterY).padStart(4)}  Kopf ${m.kopfOben}  Leiste-unten ${m.leisteUnten}` +
                  ` (Fenster ${m.fensterHoehe}, Dokument ${m.dokumentHoehe})  Fokus ${m.fokus}` +
                  (m.rollende.length ? '  rollt: ' + m.rollende.join(', ') : ''));
      if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}.png` });

      if (m.fensterY !== 0 || m.rollende.length)
        melde(name, `beim Öffnen gerollt (Fenster-Y ${m.fensterY}${m.rollende.length ? ', ' + m.rollende.join(', ') : ''})`);
      if (m.kopfOben !== null && m.kopfOben < 0) melde(name, `Dialogkopf über dem Fenster (oben ${m.kopfOben})`);
      if (m.fokus === 'body') console.log(`    Hinweis ${name}: kein Fokusziel`);
      await seite.close();
    }
  }
} catch (e) {
  console.error('Aufbaufehler: ' + (e?.stack || e));
  if (browser) await browser.close();
  process.exit(2);
}
await browser.close();

if (GEGENPROBE) {
  console.log(verstoesse.length > 0
    ? `GEGENPROBE ROT wie verlangt — ${verstoesse.length} Verstoß/Verstöße ohne preventScroll.`
    : 'GEGENPROBE GRÜN — die Probe erkennt den Sprung nicht.');
  process.exit(verstoesse.length > 0 ? 0 : 1);
}
console.log(verstoesse.length === 0
  ? 'FOKUSPROBE GRÜN — kein Dialog rollt beim Öffnen.'
  : `FOKUSPROBE ROT — ${verstoesse.length} Verstoß/Verstöße.`);
process.exit(verstoesse.length === 0 ? 0 : 1);
