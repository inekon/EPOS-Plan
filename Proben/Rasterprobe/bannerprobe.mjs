// =====================================================================
//  BANNERPROBE - DIE MELDUNG STEHT IM BILD (Anwenderentscheid 06.10.2026)
// =====================================================================
//
//  WOZU. Die langen Dialoge zeigen ihre Meldung als Warnbanner oben im Inhalt,
//  direkt nach Kopf und Kontextzeile. Im eigenen Fenster haftet der Kopf, der
//  Inhalt rollt - wer unten arbeitet, sah die Meldung nicht (Befund „Gebaeude in
//  DB loeschen funktioniert nicht"). Seitdem haftet ein Banner, das unmittelbares
//  Kind der Dialogwurzel ist, unter dem Kopf (epos-ui.css, Abschnitt „Dialog im
//  eigenen Fenster"). bunit misst keine Lage - diese Probe misst sie auf der Seite
//  /fensterprobe?meldung=1 des Wirtes, bei 1 088 x 624 und 520 x 624 CSS-Pixeln.
//
//  JE DIALOG (Heizkessel, BHKW, Gebaeude):
//    - ans Ende rollen, dann die Meldung ausloesen, ohne dass der Klick rollt
//      (Heizkessel/BHKW: Katalogsatz waehlen, „Hinzufuegen" - die Traegerwahl
//      schlaegt fehl; Gebaeude: „In DB uebernehmen" einer ungespeicherten Zeile);
//    - das Banner steht ganz im Fenster, unter dem Kopf und ueber der
//      Schlussleiste, und ist unverdeckt (elementFromPoint an vier Punkten);
//    - Kopf (top 0) und Schlussleiste (bottom = Fensterhoehe) haften weiter;
//    - der ausloesende Knopf steht nach der Meldung, wo er vorher stand (nichts
//      springt; den Zuwachs oben gleicht der Scroll-Anker des Browsers aus);
//    - das Kreuz ist sichtbar, ein Klick blendet das Banner aus;
//    - in Ruhe (Rollstand 0) steht das Banner dort, wo es ohne die Haftregel
//      stuende - die Regel verschiebt nichts.
//  OHNE FENSTERMARKE (wie Ueberlagerung, Blatt und Seite auf iOS): das Banner
//    rollt mit (position static), das Kreuz ist verborgen.
//
//  IPAD 11 ZOLL (1 194 x 834 quer): dieselben Faelle MIT den sicheren Abstaenden des
//    iPads (oben 24 px, unten 20 px, ueber die Token --epos-sicher-oben/-unten): Kopf,
//    Banner und Schlussleiste stehen zwischen beiden. Gegenprobe: dasselbe Fenster ohne
//    die Token muss rot werden.
//
//  GEGENPROBE (laeuft mit, abschaltbar mit --ohne-gegenprobe): dieselben Faelle
//  mit position: static am Banner muessen das Sichtkriterium verfehlen. Bleibt
//  die Gegenprobe gruen, misst die Probe nichts.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node bannerprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--nur <fall>] [--ohne-gegenprobe]
//
//  Rueckgabe 0 = kein Verstoss und Gegenprobe rot, 1 = Verstoss oder Gegenprobe
//  gruen, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/bannerprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const TOL = 1;

const FENSTER = [{ breite: 1088, hoehe: 624 }, { breite: 520, hoehe: 624 }, { breite: 1194, hoehe: 834, ipad: true }];
// Sichere Abstaende des iPads (Punkte = CSS-Pixel); die Probe setzt die Token des Themas.
const SICHER = { oben: 24, unten: 20 };
const SICHER_TOKEN = `:root { --epos-sicher-oben: ${SICHER.oben}px; --epos-sicher-unten: ${SICHER.unten}px; }`;
const rand = f => f.ipad ? SICHER : { oben: 0, unten: 0 };
// Je Fall: was vor dem Rollen gewaehlt wird, und der Knopf, der die Meldung ausloest.
const FAELLE = {
  heizkessel: { wahl: '.epos-zweispalten-spalte--unten tbody tr .epos-zeilenzelle--name', ausloeser: '.epos-zweispalten-knopf--uebernehmen', fuellt: true },
  bhkw: { wahl: '.epos-zweispalten-spalte--unten tbody tr .epos-zeilenzelle--name', ausloeser: '.epos-zweispalten-knopf--uebernehmen', fuellt: true },
  gebaeude: { wahl: '', ausloeser: '.epos-gebaeude-in-db', fuellt: true },
};
const BANNER = 'body > #app > .epos-dialog > .epos-warnbanner';

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };
const zahl = v => Math.round(v * 10) / 10;

// ---------------------------------------------------------------------------
//  Messung in der Seite
// ---------------------------------------------------------------------------

function lage([selektor, ausloeser]) {
  const d = document.querySelector('body > #app > .epos-dialog');
  const kopf = d.firstElementChild && d.firstElementChild.matches('.epos-dialog-kopf') ? d.firstElementChild : null;
  const fuss = [...d.children].find(e => e.matches('.epos-leiste') && e.querySelector(':scope > .epos-knopf--primaer')) || null;
  const banner = document.querySelector(selektor);
  const r = e => { if (!e) return null; const b = e.getBoundingClientRect(); return { top: b.top, bottom: b.bottom, left: b.left, right: b.right }; };
  let unverdeckt = null, kreuz = null, pos = '';
  if (banner) {
    const b = banner.getBoundingClientRect();
    const punkte = [[b.left + 6, b.top + 3], [b.right - 6, b.top + 3], [b.left + 6, b.bottom - 3], [b.right - 6, b.bottom - 3]];
    unverdeckt = punkte.every(([x, y]) => { const e = document.elementFromPoint(x, y); return !!e && banner.contains(e); });
    const k = banner.querySelector('.epos-warnbanner-schliessen');
    kreuz = k ? getComputedStyle(k).display : 'fehlt';
    pos = getComputedStyle(banner).position;
  }
  const knopf = ausloeser ? document.querySelector(ausloeser) : null;
  return { y: scrollY, innen: innerHeight, dokument: document.documentElement.scrollHeight,
           kopf: r(kopf), fuss: r(fuss), banner: r(banner), knopf: r(knopf), unverdeckt, kreuz, pos };
}

const rahmen = seite => seite.evaluate(() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))));

async function oeffnen(browser, fall, f, zusatz = '') {
  const seite = await browser.newPage({ viewport: { width: f.breite, height: f.hoehe } });
  await seite.goto(`${WURZEL}/fensterprobe?fall=${fall}&kultur=de-DE&meldung=1${zusatz}`);
  if (f.ipad && !f.ohneToken) await seite.addStyleTag({ content: SICHER_TOKEN });
  await seite.waitForSelector('body > #app > .epos-dialog', { timeout: 20000 });
  await seite.waitForTimeout(800);
  return seite;
}

/**
 * Waehlen, ans Ende rollen, Meldung ausloesen (Klick aus dem Skript - er rollt
 * nicht, wie ein Klick des Anwenders auf einen sichtbaren Knopf). Liefert die
 * Lage vor und nach der Meldung.
 */
async function ausloesen(seite, fall) {
  const fa = FAELLE[fall];
  if (fa.wahl) {
    const ok = await seite.evaluate(s => { const e = document.querySelector(s); if (!e) return false; e.click(); return true; }, fa.wahl);
    if (!ok) throw new Error(`${fall}: keine Katalogzeile (${fa.wahl})`);
    await seite.waitForTimeout(300);
  }
  await seite.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  await rahmen(seite);
  const vor = await seite.evaluate(lage, [BANNER, fa.ausloeser]);
  const ok = await seite.evaluate(s => { const e = document.querySelector(s); if (!e || e.disabled) return false; e.click(); return true; }, fa.ausloeser);
  if (!ok) throw new Error(`${fall}: Ausloeser fehlt oder gesperrt (${fa.ausloeser})`);
  await seite.waitForSelector(BANNER, { timeout: 5000 });
  await seite.waitForTimeout(200);
  await rahmen(seite);
  const nach = await seite.evaluate(lage, [BANNER, fa.ausloeser]);
  return { vor, nach };
}

/** Das Sichtkriterium; liefert die Zahl der Verstoesse (fuer die Gegenprobe). */
function sichtbar(name, l, vor, melden, fuellt = false, r = { oben: 0, unten: 0 }) {
  let n = 0;
  const v = t => { n++; if (melden) melde(name, t); };
  const b = l.banner;
  if (!b) { v('kein Banner'); return n; }
  if (vor.y < 1 && !fuellt) v(`Dialog rollt nicht (Rollweg ${zahl(vor.y)} px) - die Probe misst nichts`);
  if (b.top < r.oben - TOL || b.bottom > l.innen - r.unten + TOL) v(`Banner ${zahl(b.top)}..${zahl(b.bottom)} px ausserhalb des Fensters (${r.oben}..${l.innen - r.unten})`);
  if (l.kopf && b.top < l.kopf.bottom - TOL) v(`Banner oben ${zahl(b.top)} px unter dem Kopf (bis ${zahl(l.kopf.bottom)} px)`);
  if (l.fuss && b.bottom > l.fuss.top + TOL) v(`Banner unten ${zahl(b.bottom)} px hinter der Schlussleiste (ab ${zahl(l.fuss.top)} px)`);
  if (!l.unverdeckt) v('Banner verdeckt (elementFromPoint)');
  return n;
}

// ---------------------------------------------------------------------------
//  Ablauf
// ---------------------------------------------------------------------------

let browser;
try {
  if (FOTOS) await mkdir(FOTOS, { recursive: true });
  browser = await chromium.launch();
  const faelle = Object.keys(FAELLE).filter(f => !NUR || NUR === f);

  for (const f of FENSTER) for (const fall of faelle) {
    const name = `${fall} ${f.breite}x${f.hoehe}`;
    console.log(`- ${name}`);
    const seite = await oeffnen(browser, fall, f);
    const { vor, nach } = await ausloesen(seite, fall);
    sichtbar(name, nach, vor, true, !!FAELLE[fall].fuellt, rand(f));
    // Waechst der Inhalt oben um das Banner, haelt Chromium den Inhalt im Bild fest (Scroll-
    // Anker): der Rollstand waechst um die Bannerhoehe, der Knopf unter der Maus bleibt stehen.
    // Katalogauswahl (Zweispaltenauswahl): der Dialog fuellt das Fenster und rollt nicht - Kopf und
    // Schlussleiste stehen ohnehin im Bild; das Banner nimmt den Listen Hoehe, der Knopf ruckt um sie.
    if (FAELLE[fall].fuellt) {
      if (nach.y > TOL || nach.dokument > nach.innen + TOL) melde(name, `das Dokument rollt (${zahl(nach.y)} / ${nach.dokument} px)`);
      // KB1: Unter der Mindesthoehe rollt der Dialogkoerper; die Schlussleiste muss dann am Ende des Rollwegs im Bild stehen.
      const eng = await seite.evaluate(unten => {
        const d = document.querySelector('body > #app > .epos-dialog');
        if (!d || !d.hasAttribute('data-zweispalten-eng')) return null;
        const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer'));
        const v = d.scrollTop; d.scrollTop = d.scrollHeight;
        const ok = !!fuss && fuss.getBoundingClientRect().bottom <= innerHeight - unten + 1; d.scrollTop = v; return ok;
      }, rand(f).unten);
      if (eng !== null) console.log(`    Befund ${name}: unter der Mindesthoehe, der Dialogkoerper rollt (KB1), Schlussleiste erreichbar: ${eng}`);
      if (eng === false || (eng === null && (!nach.kopf || nach.kopf.top < rand(f).oben - TOL || !nach.fuss || nach.fuss.bottom > nach.innen - rand(f).unten + TOL)))
        melde(name, 'Kopf oder Schlussleiste ausserhalb des Fensters');
      if (!nach.knopf || nach.knopf.top - vor.knopf.top > nach.banner.bottom - nach.banner.top + 12 + TOL)
        melde(name, `der ausloesende Knopf sprang um mehr als das Banner (${zahl(vor.knopf.top)} -> ${zahl(nach.knopf && nach.knopf.top)} px)`);
    } else {
    if (!vor.knopf || !nach.knopf || Math.abs(nach.knopf.top - vor.knopf.top) > TOL)
      melde(name, `der ausloesende Knopf sprang ${vor.knopf && zahl(vor.knopf.top)} -> ${nach.knopf && zahl(nach.knopf.top)} px`);
    if (!nach.kopf || Math.abs(nach.kopf.top) > TOL) melde(name, `Kopf haftet nicht (top ${nach.kopf && zahl(nach.kopf.top)})`);
    if (!nach.fuss || Math.abs(nach.fuss.bottom - nach.innen) > TOL) melde(name, `Schlussleiste haftet nicht (bottom ${nach.fuss && zahl(nach.fuss.bottom)})`);
    }
    if (nach.kreuz !== 'inline-flex' && nach.kreuz !== 'flex') melde(name, `Kreuz nicht sichtbar (display ${nach.kreuz})`);
    console.log(`    Rollstand ${zahl(vor.y)} -> ${zahl(nach.y)} px, Knopf ${zahl(vor.knopf.top)} -> ${zahl(nach.knopf.top)} px, Kopf ..${zahl(nach.kopf.bottom)}, Banner ${zahl(nach.banner.top)}..${zahl(nach.banner.bottom)}, `
      + `Fuss ${zahl(nach.fuss.top)}.. px, ${nach.pos}, unverdeckt ${nach.unverdeckt}, Kreuz ${nach.kreuz}`);
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/banner_${fall}_${f.breite}.png` });

    // In Ruhe: dieselbe Lage wie ohne Fenstermarke.
    await seite.evaluate(() => window.scrollTo(0, 0));
    await rahmen(seite);
    const ruhe = await seite.evaluate(lage, [BANNER, ''])
    const statisch = await seite.addStyleTag({ content: '#app > .epos-dialog > .epos-warnbanner { position: static !important; }' });
    await rahmen(seite);
    const ruheStatisch = await seite.evaluate(lage, [BANNER, '']);
    await statisch.evaluate(e => e.remove());
    await rahmen(seite);;

    // Das Kreuz blendet aus (echter Klick - das Kreuz steht im Bild).
    await seite.click(BANNER + ' .epos-warnbanner-schliessen');
    await seite.waitForTimeout(200);
    if (await seite.$(BANNER)) melde(name, 'Kreuz blendet das Banner nicht aus');
    await seite.close();

    const ohne = await oeffnen(browser, fall, f, '&marke=0');
    await ausloesen(ohne, fall);
    await ohne.evaluate(() => window.scrollTo(0, 0));
    await rahmen(ohne);
    const ruheOhne = await ohne.evaluate(lage, [BANNER, '']);
    await ohne.close();
    if (ruheOhne.pos !== 'static') melde(name, `ohne Marke haftet das Banner (${ruheOhne.pos})`);
    if (ruheOhne.kreuz !== 'none') melde(name, `ohne Marke steht das Kreuz (display ${ruheOhne.kreuz})`);
    if (Math.abs(ruhe.banner.top - ruheStatisch.banner.top) > TOL || Math.abs(ruhe.banner.bottom - ruheStatisch.banner.bottom) > TOL)
      melde(name, `in Ruhe Banner bei ${zahl(ruhe.banner.top)} px, ohne Haftregel ${zahl(ruheStatisch.banner.top)} px`);
    console.log(`    in Ruhe Banner ${zahl(ruhe.banner.top)}..${zahl(ruhe.banner.bottom)} px (ohne Haftregel ${zahl(ruheStatisch.banner.top)}..${zahl(ruheStatisch.banner.bottom)}); `
      + `ohne Marke ${ruheOhne.pos}, Kreuz ${ruheOhne.kreuz}`);
  }

  // -------------------------------------------------------------- Gegenprobe
  let gegenGruen = 0;
  if (MIT_GEGENPROBE) {
    console.log('- GEGENPROBE (Banner mit position: static - muss rot sein)');
    for (const f of FENSTER) for (const fall of faelle) {
      const seite = await oeffnen(browser, fall, f);
      await seite.addStyleTag({ content: '#app > .epos-dialog > .epos-warnbanner { position: static !important; }' });
      const { vor, nach } = await ausloesen(seite, fall);
      const n = sichtbar('', nach, vor, false);
      console.log(`    ${fall} ${f.breite}x${f.hoehe}: ${n} Verstoesse, Banner ${nach.banner ? zahl(nach.banner.top) + '..' + zahl(nach.banner.bottom) : '-'} px`);
      if (n === 0) gegenGruen++;
      await seite.close();
    }
    // iPad ohne die Token: Kopf, Banner oder Schlussleiste reichen unter Statusleiste oder Home-Anzeige.
    for (const f of FENSTER.filter(f => f.ipad)) for (const fall of faelle) {
      const seite = await oeffnen(browser, fall, { ...f, ohneToken: true });
      const { vor, nach } = await ausloesen(seite, fall);
      const r = rand(f);
      const n = sichtbar('', nach, vor, false, true, r)
        + ((!nach.kopf || nach.kopf.top < r.oben - TOL) ? 1 : 0) + ((!nach.fuss || nach.fuss.bottom > nach.innen - r.unten + TOL) ? 1 : 0);
      console.log(`    ohne sichere Abstaende ${fall} ${f.breite}x${f.hoehe}: ${n} Verstoesse, Kopf ab ${nach.kopf ? zahl(nach.kopf.top) : '-'}, Fuss bis ${nach.fuss ? zahl(nach.fuss.bottom) : '-'} px`);
      if (n === 0) gegenGruen++;
      await seite.close();
    }
    if (gegenGruen) console.log(`  GEGENPROBE GRUEN in ${gegenGruen} Faellen - die Probe misst die Regel nicht`);
  }

  console.log(verstoesse.length === 0 && gegenGruen === 0
    ? 'BANNERPROBE ERFUELLT' + (MIT_GEGENPROBE ? ' (Gegenprobe rot)' : '')
    : `BANNERPROBE VERFEHLT: ${verstoesse.length} Verstoesse, Gegenprobe gruen in ${gegenGruen} Faellen`);
  await browser.close();
  process.exit(verstoesse.length === 0 && gegenGruen === 0 ? 0 : 1);
} catch (e) {
  console.error('AUFBAUFEHLER: ' + (e && e.stack || e));
  if (browser) await browser.close();
  process.exit(2);
}
