// =====================================================================
//  KOPFLEISTENPROBE - DIE KATALOGAUSWAHL SCHNEIDET NICHTS AB
// =====================================================================
//
//  WOZU. Die Kopfleisten der Zweispaltenauswahl (Bereich "Im Projekt" und
//  "Katalog (Datenbank)") tragen Marke, Ueberschrift, Suchzeile der
//  Katalogliste (Suchfeld, Trefferzahl, Legende, Spalten...), Knoepfe des
//  Wirts und "In das Projekt uebernehmen". Bei der festen Dialogbreite
//  (.epos-dialog max-width 1160 px) passte das nicht in jedem Dialog in eine
//  Zeile: Ueberschrift und Platzhalter kuerzten, "Spalten..." lag unter
//  "Katalogverwaltung..." (Kaeltemaschine). bunit misst keine Breite - diese
//  Probe misst sie auf /wikibild (alle Projektdialoge mit Katalogauswahl, ohne
//  data-breit) und /fensterprobe (Katalogauswahl im eigenen Fenster, de-DE und
//  en-US).
//
//  JE KOPFLEISTE, je sichtbarem Knopf, Feld, Ueberschrift, Marke, Zaehler:
//    - liegt ganz in der Leiste und in seinem clippenden Vorfahren;
//    - sein Text (auch der Knopftext darin) ist nicht gekuerzt (scrollWidth);
//    - der Platzhalter des Suchfelds steht ganz (Probe: Wert = Platzhalter);
//    - keine zwei Elemente ueberlappen.
//  Fenster: 1 400 x 820 (Dialog 1 160 px breit), 1 088 x 624 (Fenstermass des
//  Anwenders, Kompaktstufe) - beide Kopfleisten; 520 x 624
//  (Fenstermass.MindestBreite) - nur die des Katalogs: Die Kopfleiste des
//  Projekts kuerzt unter 800 px Bereichsbreite ihre Knopftexte mit Auslassung
//  (Regel 4.8, title nennt den vollen Text).
//
//  GEGENPROBE (laeuft mit, --ohne-gegenprobe laesst sie aus): dieselben Faelle
//  mit den Regeln von vorher (Kopfleiste des Katalogs ohne Umbruch, Suchfeld
//  "0 3 16rem", Untergrenze 7rem) - der Lauf muss dann rot sein.
//
//  Aufruf (der Wirt laeuft, siehe LIESMICH.md):
//    node kopfleistenprobe.mjs [--url http://127.0.0.1:5299] [--fotos <ordner>] [--nur <fall>] [--ohne-gegenprobe]
//  Rueckgabe 0 = nichts abgeschnitten (und Gegenprobe rot), 1 = Verstoss,
//  2 = Aufruf- oder Verbindungsfehler.

import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/kopfleistenprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const OHNE_GEGENPROBE = process.argv.includes('--ohne-gegenprobe');

// Die Regeln von vorher, fuer die Gegenprobe ueber die geltenden gelegt.
const VORHER_CSS = `
.epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste { flex-wrap: nowrap !important; }
.epos-zweispalten-kopfleiste .epos-katalog-suchfeld { flex: 0 3 16rem !important; min-width: 7rem !important; }`;

const FAELLE = [
  ...['heizkessel', 'bhkw', 'waermepumpe', 'pufferspeicher', 'stromspeicher', 'photovoltaik', 'solarkollektoren',
    'brauchwasser', 'prozesswaerme', 'gebaeude', 'kaeltemaschine'].map(b => [`wiki-${b}`, `/wikibild?bild=${b}&kultur=de-DE`]),
  ...['heizkessel', 'bhkw', 'waermepumpen', 'gebaeude', 'kaelteprofile'].map(f => [`fenster-${f}`, `/fensterprobe?fall=${f}&kultur=de-DE`]),
  ...['heizkessel', 'bhkw', 'waermepumpen'].map(f => [`fenster-${f}-en`, `/fensterprobe?fall=${f}&kultur=en-US`]),
];
const FENSTER = [[1400, 820, false], [1088, 624, false], [520, 624, true]];

async function messen(browser, vorher) {
  let verstoesse = 0;
  for (const [name, pfad] of FAELLE) {
    if (NUR && !name.startsWith(NUR)) continue;
    for (const [w, h, nurKatalog] of FENSTER) {
      const kontext = await browser.newContext({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
      const seite = await kontext.newPage();
      await seite.goto(WURZEL + pfad, { waitUntil: 'networkidle' });
      await seite.locator('.epos-zweispalten-kopfleiste').first().waitFor({ state: 'visible', timeout: 15000 });
      // Die Bildseite verbreitert einzelne Dialoge fuers Wiki-Bild - gemessen wird die Hausbreite.
      await seite.evaluate(() => document.querySelectorAll('[data-breit]').forEach(e => e.removeAttribute('data-breit')));
      if (vorher) await seite.addStyleTag({ content: VORHER_CSS });
      await seite.waitForTimeout(700);
      const r = await seite.evaluate((nurKatalog) => {
        const aus = { dialog: 0, leisten: [] };
        const d = document.querySelector('.epos-dialog');
        if (d) aus.dialog = Math.round(d.getBoundingClientRect().width);
        const sicht = e => {
          const c = getComputedStyle(e), q = e.getBoundingClientRect();
          return c.display !== 'none' && c.visibility !== 'hidden' && q.width > 1 && q.height > 1 && c.clipPath !== 'inset(50%)';
        };
        const nm = e => {
          const k = typeof e.className === 'string' ? e.className.split(' ').filter(x => x.startsWith('epos')).slice(-1)[0] : '';
          const t = e.textContent.trim().replace(/\s+/g, ' ').slice(0, 24);
          return (k || e.tagName.toLowerCase()) + (t ? ` "${t}"` : '');
        };
        const auswahl = nurKatalog ? '.epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste' : '.epos-zweispalten-kopfleiste';
        for (const l of document.querySelectorAll(auswahl)) {
          const lr = l.getBoundingClientRect();
          if (lr.width === 0) continue;
          const befund = [];
          const kinder = [...l.querySelectorAll('button, input, h2, .epos-zweispalten-marke, .epos-zweispalten-wahlzahl, .epos-katalog-treffer, .epos-katalog-verwendetlegende, .epos-formularraster')].filter(sicht);
          for (const e of kinder) {
            const q = e.getBoundingClientRect();
            if (q.right > lr.right + 0.5 || q.left < lr.left - 0.5) befund.push(`ueber den Rand: ${nm(e)} ${Math.round(q.left)}..${Math.round(q.right)}, Leiste bis ${Math.round(lr.right)}`);
            for (let v = e.parentElement; v && v !== l.parentElement; v = v.parentElement) {
              if (getComputedStyle(v).overflowX === 'visible') continue;
              const vr = v.getBoundingClientRect();
              if (q.right > vr.right + 0.5) befund.push(`abgeschnitten: ${nm(e)} bis ${Math.round(q.right)}, ${nm(v).slice(0, 30)} bis ${Math.round(vr.right)}`);
              break;
            }
            if (e.tagName !== 'INPUT' && e.clientWidth > 0 && e.scrollWidth > e.clientWidth + 1) befund.push(`gekuerzt: ${nm(e)} ${e.scrollWidth} > ${e.clientWidth} px`);
            for (const t of e.querySelectorAll('span'))
              if (sicht(t) && t.clientWidth > 0 && t.scrollWidth > t.clientWidth + 1) befund.push(`gekuerzt: ${nm(t)} ${t.scrollWidth} > ${t.clientWidth} px`);
            if (e.tagName === 'INPUT' && e.placeholder && !e.value) {
              e.value = e.placeholder; const sw = e.scrollWidth, cw = e.clientWidth; e.value = '';
              if (sw > cw + 1) befund.push(`Platzhalter gekuerzt: "${e.placeholder}" ${sw} > ${cw} px`);
            }
          }
          for (let i = 0; i < kinder.length; i++) for (let j = i + 1; j < kinder.length; j++) {
            const a = kinder[i], c = kinder[j];
            if (a.contains(c) || c.contains(a)) continue;
            const p = a.getBoundingClientRect(), q = c.getBoundingClientRect();
            const ix = Math.min(p.right, q.right) - Math.max(p.left, q.left), iy = Math.min(p.bottom, q.bottom) - Math.max(p.top, q.top);
            if (ix > 0.5 && iy > 0.5) befund.push(`ueberlappt: ${nm(a)} / ${nm(c)} ${Math.round(ix)} x ${Math.round(iy)} px`);
          }
          const art = l.closest('.epos-zweispalten-bereich--katalog') ? 'Katalog' : 'Projekt';
          aus.leisten.push({ art, breite: Math.round(lr.width), hoehe: Math.round(lr.height), befund });
        }
        return aus;
      }, nurKatalog);
      const n = r.leisten.reduce((s, l) => s + l.befund.length, 0);
      verstoesse += n;
      if (!vorher || n) {
        console.log(`${n ? '--' : 'OK'} ${name} ${w}x${h}  Dialog ${r.dialog} px  ${r.leisten.map(l => `${l.art} ${l.breite}x${l.hoehe}`).join(', ')}`);
        if (!vorher) for (const l of r.leisten) for (const x of l.befund) console.log(`     ${l.art}: ${x}`);
      }
      if (FOTOS && !vorher) await seite.locator('.epos-dialog').first().screenshot({ path: `${FOTOS}/kopfleiste_${name}_${w}.png` });
      await kontext.close();
    }
  }
  return verstoesse;
}

let browser;
try {
  browser = await chromium.launch();
  const verstoesse = await messen(browser, false);
  console.log(`\nVerstoesse: ${verstoesse}`);
  let gegenRot = true;
  if (!OHNE_GEGENPROBE) {
    console.log('\n=== GEGENPROBE (Regeln von vorher) ===');
    const g = await messen(browser, true);
    gegenRot = g > 0;
    console.log(`Gegenprobe: ${g} Verstoesse - ${gegenRot ? 'rot, wie verlangt' : 'GRUEN - die Probe misst nichts'}`);
  }
  process.exitCode = verstoesse === 0 && gegenRot ? 0 : 1;
} catch (f) {
  console.error('ABBRUCH: ' + (f && f.message ? f.message : f));
  process.exitCode = 2;
} finally {
  if (browser) await browser.close();
}
