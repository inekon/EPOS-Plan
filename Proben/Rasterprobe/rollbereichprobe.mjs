// Rollbereichprobe - "kein Rollbereich im Rollbereich" (Konzept Projektdialoge mit
// Katalogauswahl, Abschnitt 4.8 und 8, Probenpflicht). Pruefstand, nicht Auslieferung.
//
// Je Projektdialog mit Katalogauswahl, je Fenster (1 280 x 800, 1 280 x 720, 1 024 x 700)
// und je Zustand (Vorgabe, Trennlinie oben, Trennlinie unten, Detailzeile auf bei beiden
// Grenzen, gezogen und neu geladen, Ueberlagerung offen) misst die Probe im echten
// Chromium:
//   - kein sichtbares Element mit overflow auto|scroll liegt in einem anderen;
//   - das Dokument rollt nicht, der Dialogkoerper rollt nicht;
//   - die Katalogliste behaelt Kopfzeile plus zwei Zeilen, die Projektliste ihre Untergrenze;
//   - die Kopfleisten brechen nicht um und schneiden keinen Knopf ab;
//   - die Konsole bleibt ohne Fehler.
// Gegenprobe (laeuft mit): ein absichtlich verschachtelter Rollbereich und ein rollender
// Dialogkoerper muessen rot werden.
//
// KOMPAKTSTUFE UND ROLLBALKEN (KB1): drei weitere Fenster - 1 024 x 768 und 768 x 1 024 (iPad quer
// und hoch), 1 093 x 614 (Laptop bei 125 %). Dort gilt: Kompaktstufe aktiv (Schrift 12 px,
// Projektzeile 46 px, Katalogzeile 46 bzw. 40 px mit Zeilenmass), alle Knoepfe der Schlussleiste
// nach dem Rollen sichtbar, Katalogliste Kopf und zwei Zeilen, Projektliste mindestens ihre
// Untergrenze, Kopfleisten ohne Umbruch, Konsole fehlerfrei. Der Dialogkoerper darf dort nur
// rollen, wenn das Fenster die Mindesthoehe nicht hergibt (data-zweispalten-eng). In allen
// Fenstern misst die Probe die Stufe nach der Medienabfrage der Kompaktstufe. Zweite Gegenprobe
// (Rollbalken): ein 400 px hoher Klotz im Dialog muss den Dialogkoerper rollen lassen und die
// Schlussleiste erreichbar halten; derselbe Klotz mit overflow: hidden muss rot werden.
//
// VORRANG DER DETAILZEILE (DZ1, Konzept 4.4 und 4.8): Aufgeklappt stehen Projektliste und
// Katalogliste auf ihren Untergrenzen (je hoechstens 2 px darueber), und die Satzflaeche reicht bis
// an den unteren Rand des Bausteins (Rest hoechstens 3 px). Traegt sie eine Ganglinie, nutzt das
// Bild die Hoehe, die Kennzahlen und Leisten lassen (oder die volle Breite). Dritte Gegenprobe: eine
// Satzflaeche, die auf 100 px begrenzt kleiner als der Rest bleibt, muss rot werden.
//
// VERDICHTETER KOPF DER GANGLINIE (DZ1-N1, Konzept 4.9): Aufgeklappt mit Ganglinie ist der Kopf der
// Satzflaeche (Kopfzeile, Kennzahlenzeile mit Zoom und Infoknoepfen) ab 1 024 px Breite hoechstens 90 px hoch
// (768 x 1 024 bricht die Kennzahlen um: 93 px), die Kurve
// mindestens --epos-kurve-min (150 px) hoch, die Satzflaeche rollt nicht in sich.
//
// KURVE IN VOLLER BREITE (DZ1-N2, Konzept 4.9): Die Kurve ist mindestens 90 % so breit wie die
// Satzflaeche, und das Zeichenmodell steht in Behaeltergroesse (viewBox = Flaeche, je hoechstens 3 px
// Abweichung - Achsen und Schrift 1:1). Bei Strom-, Solar- und PV-Ganglinie (ROLLT_NICHT) rollt der
// Dialogkoerper in 1 280 x 800 und 1 280 x 720 nur, solange die Kurve auf ihrer Untergrenze steht (KB1;
// bei 1 280 x 800 rund 40 px, gewollt). Vierte Gegenprobe in der Solarganglinie bei 1 280 x 800: eine
// Kurve, die auf 60 px gedrueckt wird, eine Kurve, die auf 300 px Breite begrenzt wird, und eine
// Untergrenze der Satzflaeche von 600 px (der Dialogkoerper rollt, obwohl die Kurve ueber ihrer
// Untergrenze steht) muessen rot werden.
// IPAD 11 ZOLL: vier weitere Fenster - 1 180 x 820 (iPad Air 11"), 1 194 x 834 (iPad Pro 11"),
// 1 210 x 834 (iPad Pro 11" M4) quer und 834 x 1 194 hochkant, jeweils MIT den sicheren Abstaenden des
// iPads (Statusleiste oben 24 px, Home-Anzeige unten 20 px). Die Probe setzt dafuer die zwei Token des
// Themas (:root{--epos-sicher-oben:24px;--epos-sicher-unten:20px}), nicht env(). Dort gilt zusaetzlich:
// der Dialog steht ganz zwischen den Abstaenden, eine offene Ueberlagerung ebenso, und jeder Knopf der
// Schlussleiste liegt ueber der Home-Anzeige. Die AppWurzel zeigt auf iOS keine Kopfleiste (sie ist dort
// leer); ein weiterer oberer Abzug entfaellt. Dritte Gegenprobe (sichere Abstaende): dasselbe Fenster
// 1 194 x 834 OHNE die Token - der Dialog reicht unter Statusleiste und Home-Anzeige und muss rot werden.
//
// Aufruf: node rollbereichprobe.mjs --url http://127.0.0.1:5299 [--nur <fall>] [--ohne-gegenprobe] [--fotos <ordner>]
// Rueckgabe 0 = kein Verstoss und Gegenprobe rot, 1 = Verstoss oder Gegenprobe gruen, 2 = Aufbaufehler.

import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/rollbereichprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const NUR = arg('nur', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const FOTOS = arg('fotos', '');
const KOMPAKT = '(max-width: 1279.98px), (max-height: 799.98px)';   // Medienabfrage der Kompaktstufe (epos-ui.css)
const ZEILE = 53;   // Zeilenhoehe, falls die Liste keine Zeile zeigt; sonst gemessen
const ZEILE_KOMPAKT = 46;   // dieselbe in der Kompaktstufe
const KOPF_MAX = 90;   // DZ1-N1: Kopf der Satzflaeche mit Ganglinie (ohne Polster)
const BREITE_ANTEIL = 0.9;   // DZ1-N2: Kurvenbreite mindestens 90 % der Satzflaechenbreite
const MASS_TOLERANZ = 3;     // DZ1-N2: Zeichenmodell in Behaeltergroesse, je Richtung hoechstens 3 px
const UNTERGRENZE_SPIEL = 5; // DZ1-N2: "auf der Untergrenze" heisst Kurve hoechstens 5 px ueber --epos-kurve-min (Rand der Flaeche, Rundung der Kopfmasse)
const ROLLT_NICHT = ['stromganglinie', 'solarganglinie', 'pvganglinie'];   // DZ1-N2: Rollen in 1 280 x 800 / 720 nur auf der Untergrenze

const FENSTER = [{ breite: 1280, hoehe: 800 }, { breite: 1280, hoehe: 720 }, { breite: 1024, hoehe: 700 },
  { breite: 1024, hoehe: 768, neu: true }, { breite: 768, hoehe: 1024, neu: true }, { breite: 1093, hoehe: 614, neu: true },
  { breite: 1180, hoehe: 820, neu: true, ipad: true }, { breite: 1194, hoehe: 834, neu: true, ipad: true },
  { breite: 1210, hoehe: 834, neu: true, ipad: true }, { breite: 834, hoehe: 1194, neu: true, ipad: true }];
// Sichere Abstaende des iPads (Punkte = CSS-Pixel), in beiden Lagen gleich; links und rechts 0.
const SICHER = { oben: 24, unten: 20 };
const SICHER_TOKEN = `:root { --epos-sicher-oben: ${SICHER.oben}px; --epos-sicher-unten: ${SICHER.unten}px; }`;
const rand = fenster => fenster.ipad ? SICHER : { oben: 0, unten: 0 };
const FAELLE = [
  ['heizkessel', '/fensterprobe?fall=heizkessel'],
  ['bhkw', '/fensterprobe?fall=bhkw'],
  ['waermepumpen', '/fensterprobe?fall=waermepumpen'],
  ['gebaeude', '/fensterprobe?fall=gebaeude'],
  ['pufferspeicher', '/rollbereichprobe?fall=pufferspeicher'],
  ['stromspeicher', '/rollbereichprobe?fall=stromspeicher'],
  ['photovoltaik', '/rollbereichprobe?fall=photovoltaik'],
  ['solarkollektoren', '/rollbereichprobe?fall=solarkollektoren'],
  ['bedarfsprofile', '/rollbereichprobe?fall=bedarfsprofile'],
  ['waermebedarf', '/rollbereichprobe?fall=waermebedarf'],
  ['stromganglinie', '/rollbereichprobe?fall=stromganglinie'],
  ['solarganglinie', '/rollbereichprobe?fall=solarganglinie'],
  ['pvganglinie', '/rollbereichprobe?fall=pvganglinie'],
  ['kaeltemaschine', '/rollbereichprobe?fall=kaeltemaschine'],
].filter(([n]) => !NUR || n.startsWith(NUR));

const verstoesse = [];
const kurven = [];      // DZ1-N2: gemessene Kurvenmasse je Fall, Fenster und Zustand
const befunde = [];     // ausserhalb des Bausteins: Kaeltemaschinenkatalog (Stufe 5), Ueberlagerungen des Hauses
const zeilen = [];

// ---------------------------------------------------------------------------
//  Messung in der Seite
// ---------------------------------------------------------------------------

function messen(KOMPAKT_ABFRAGE) {
  const sichtbar = e => {
    const s = getComputedStyle(e);
    if (s.display === 'none' || s.visibility === 'hidden') return false;
    const b = e.getBoundingClientRect();
    return b.width > 0 && b.height > 0;
  };
  const rollt = e => /^(auto|scroll)$/.test(getComputedStyle(e).overflowY) || /^(auto|scroll)$/.test(getComputedStyle(e).overflowX);
  const name = e => e.tagName.toLowerCase() + (e.className && typeof e.className === 'string' ? '.' + e.className.trim().split(/\s+/).slice(0, 2).join('.') : '');
  const roller = [...document.querySelectorAll('body *')].filter(e => sichtbar(e) && rollt(e));
  const verschachtelt = [];
  for (const a of roller) for (const b of roller) if (a !== b && a.contains(b)) verschachtelt.push(name(b) + ' in ' + name(a));
  const d = document.querySelector('body > #app > .epos-dialog');
  const h = e => e ? Math.round(e.getBoundingClientRect().height) : null;
  const projekt = d && d.querySelector('.epos-zweispalten-bereich--projekt .epos-raster-huelle');
  const katalog = d && (d.querySelector('.epos-zweispalten-bereich--katalog .epos-raster-huelle') || d.querySelector('.epos-raster-huelle'));
  const kopfzeile = katalog && katalog.querySelector('thead');
  const katalogMin = katalog ? parseFloat(getComputedStyle(katalog).minHeight) || 0 : 0;
  const leisten = d ? [...d.querySelectorAll('.epos-zweispalten-kopfleiste')].filter(sichtbar) : [];
  const leistenfehler = [];
  for (const l of leisten) {
    const r = l.getBoundingClientRect();
    if (r.height > 52) leistenfehler.push('Kopfleiste ' + Math.round(r.height) + ' px hoch (umgebrochen)');
    // MO3: Die Rasterzeile der Projektliste rechnet bei aufgeklappter Detailzeile mit einer Kopfleiste in
    // Touchzielhoehe; ein Rand an einem Knopf im Leistenzusatz liess sie 6 px ueberlaufen (Waermepumpen).
    else if (l.closest('.epos-zweispalten-bereich--projekt') && d.querySelector('.epos-zweispalten--satz-offen')) {
      const ziel = parseFloat(getComputedStyle(l).minHeight) || 0;
      if (ziel > 0 && r.height > ziel + 1) leistenfehler.push('Kopfleiste der Projektliste ' + Math.round(r.height) + ' px hoeher als das Touchziel ' + ziel + ' px');
    }
    for (const k of l.querySelectorAll('button')) {
      const b = k.getBoundingClientRect();
      if (b.right > r.right + 1 || b.left < r.left - 1) leistenfehler.push('Knopf "' + k.textContent.trim() + '" abgeschnitten');
    }
  }
  // Ueberlauf: ein Bereich oder sein Inhalt ist hoeher als sein Platz (die Liste ragte ueber die Fussleiste).
  const ueberlauf = d ? [...d.querySelectorAll('.epos-zweispalten, .epos-zweispalten-bereich, .epos-zweispalten-inhalt')].filter(sichtbar)
    .filter(e => e.scrollHeight > e.clientHeight + 1).map(e => name(e) + ' ' + (e.scrollHeight - e.clientHeight) + ' px') : [];
  const dok = document.scrollingElement || document.documentElement;
  const eng = !!(d && d.hasAttribute('data-zweispalten-eng'));
  return {
    roller: roller.length, verschachtelt: eng ? verschachtelt.filter(p => !p.endsWith(' in ' + name(d))) : verschachtelt, eng,
    kompakt: matchMedia(KOMPAKT_ABFRAGE).matches, schrift: d ? parseFloat(getComputedStyle(d).fontSize) : null,
    projektMin: projekt ? parseFloat(getComputedStyle(projekt).minHeight) || 0 : 0,
    projektZeile: projekt && projekt.querySelector('tbody tr') ? Math.round(projekt.querySelector('tbody tr').getBoundingClientRect().height * 10) / 10 : null,
    dokumentRollt: dok.scrollHeight > innerHeight + 1 || dok.scrollWidth > innerWidth + 1,
    dialogRollt: d ? d.scrollHeight > d.clientHeight + 1 : false,
    dialogHoehe: h(d), projekt: h(projekt), katalog: h(katalog), katalogKopf: h(kopfzeile), katalogMin,
    katalogZeile: h(katalog && katalog.querySelector('tbody tr')),
    dialogOben: d ? d.getBoundingClientRect().top : null, dialogUnten: d ? d.getBoundingClientRect().bottom : null,
    ueberlagerungRahmen: (u => u ? [u.getBoundingClientRect().top, u.getBoundingClientRect().bottom] : null)(document.querySelector('.epos-ueberlagerung')),
    ueberlagerung: !!document.querySelector('.epos-ueberlagerung'), zweispalten: h(d && d.querySelector('.epos-zweispalten')),
    satzOffen: !!(d && d.querySelector('.epos-zweispalten--satz-offen')), ueberlauf,
    leistenfehler, ...satzmasse(d),
  };
  // DZ1: die Satzflaeche, ihr Rest bis zum unteren Rand des Bausteins und das Bild der Ganglinie.
  function satzmasse(d) {
    const zw = d && d.querySelector('.epos-zweispalten');
    const satz = zw && zw.querySelector(':scope > .epos-zweispalten-bereich--satz > .epos-zweispalten-satz');
    if (!satz || satz.hidden || !sichtbar(satz)) return { satz: null };
    const r = satz.getBoundingClientRect(), z = zw.getBoundingClientRect();
    // DZ1-N1: Der Rahmen von DiagrammSvg loest sich im Raster des Bausteins auf (display: contents); der
    // Platz der Kurve ist die Rasterzeile "bild" - vom oberen Rand der Kurve bis zum unteren des Bausteins.
    const grafik = satz.querySelector('.epos-ganglinie-grafik');
    const fl = grafik && grafik.querySelector(':scope > .epos-diagramm-svg > .epos-diagramm-svg-flaeche');
    let bild = null, bildPlatz = null, bildVoll = false, kopf = null, kurveMin = null;
    let bildBreite = null, satzBreite = null, modell = null;
    if (fl) {
      const f = fl.getBoundingClientRect(), g = grafik.getBoundingClientRect();
      bild = Math.round(f.height);
      bildBreite = Math.round(f.width);
      const sp = getComputedStyle(satz);
      satzBreite = Math.round(satz.clientWidth - (parseFloat(sp.paddingLeft) || 0) - (parseFloat(sp.paddingRight) || 0));
      const vb = fl.querySelector(':scope > svg')?.viewBox?.baseVal;
      if (vb && vb.width > 0) modell = { breite: Math.round(vb.width), hoehe: Math.round(vb.height) };
      bildPlatz = Math.round(g.bottom - f.top);
      bildVoll = f.width >= g.width - 2;
      kopf = Math.round(f.top - r.top - (parseFloat(getComputedStyle(satz).paddingTop) || 0));
      kurveMin = parseFloat(getComputedStyle(grafik).getPropertyValue('--epos-kurve-min')) || 0;
    }
    return { satz: Math.round(r.height), satzRest: Math.round(z.bottom - r.bottom), satzRollt: satz.scrollHeight > satz.clientHeight + 1,
             bild, bildPlatz, bildVoll, kopf, kurveMin, bildBreite, satzBreite, modell, ganglinie: !!grafik,
             dialogUeberhang: (() => { const d = document.querySelector('body > #app > .epos-dialog'); return d ? Math.max(0, d.scrollHeight - d.clientHeight) : 0; })() };
  }
}

/** Alle Knoepfe der Schlussleiste nach dem Rollen des Dialogkoerpers ans Ende sichtbar und treffbar? */
function erreichbar(rand = { oben: 0, unten: 0 }) {
  const d = document.querySelector('body > #app > .epos-dialog');
  if (!d) return { ok: false, fehlt: ['kein Dialog'] };
  const vorher = d.scrollTop;
  // Nur ein Rollbalken, den der Anwender bedienen kann, zaehlt: overflow hidden rollt per Skript, nicht per Hand.
  const rollbar = /^(auto|scroll)$/.test(getComputedStyle(d).overflowY);
  if (rollbar) d.scrollTop = d.scrollHeight;
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer'));
  if (!fuss) { d.scrollTop = vorher; return { ok: false, fehlt: ['keine Schlussleiste'] }; }
  const knoepfe = [...fuss.querySelectorAll('button')].filter(k => k.getBoundingClientRect().width > 0);
  const fehlt = knoepfe.filter(k => {
    const r = k.getBoundingClientRect();
    if (r.top < rand.oben - 1 || r.bottom > innerHeight - rand.unten + 1 || r.left < -1 || r.right > innerWidth + 1) return true;
    const e = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    return !(e && (e === k || k.contains(e)));
  }).map(k => k.textContent.trim());
  d.scrollTop = vorher;
  return { ok: fehlt.length === 0 && knoepfe.length > 0, n: knoepfe.length, fehlt };
}

/**
 * UeS1: die Satz-Ueberlagerung (Konzept 4.4, 4.6). Kopf und Fussleiste sichtbar und treffbar, OK/Abbrechen
 * ueber der Home-Anzeige (sichere Abstaende), die Ueberlagerung rollt nicht als Ganzes, und in ihr rollt
 * hoechstens der Koerper - kein anderer Rollbereich in ihr.
 */
function satzUeberlagerung(rand = { oben: 0, unten: 0 }) {
  const u = document.querySelector('.epos-ueberlagerung--satz');
  if (!u) return { da: false };
  const sichtbar = e => { const s = getComputedStyle(e); if (s.display === 'none' || s.visibility === 'hidden') return false; const b = e.getBoundingClientRect(); return b.width > 0 && b.height > 0; };
  const rollt = e => /^(auto|scroll)$/.test(getComputedStyle(e).overflowY) || /^(auto|scroll)$/.test(getComputedStyle(e).overflowX);
  const koerper = u.querySelector('.epos-satzueberlagerung-koerper');
  const fremdeRoller = [...u.querySelectorAll('*')].filter(e => e !== koerper && sichtbar(e) && rollt(e) && e.scrollHeight > e.clientHeight + 1)
    .map(e => e.tagName.toLowerCase() + '.' + String(e.className).trim().split(/\s+/)[0]);
  const treffbar = k => {
    const r = k.getBoundingClientRect();
    if (r.width === 0 || r.top < rand.oben - 1 || r.bottom > innerHeight - rand.unten + 1 || r.left < -1 || r.right > innerWidth + 1) return false;
    const e = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    return !!(e && (e === k || k.contains(e)));
  };
  const kopf = u.querySelector(':scope > .epos-ueberlagerung-kopf');
  const knoepfe = [...u.querySelectorAll('.epos-satzueberlagerung-fuss button')];
  const r = u.getBoundingClientRect();
  return {
    da: true, oben: Math.round(r.top), unten: Math.round(r.bottom), breite: Math.round(r.width),
    koerper: koerper ? Math.round(koerper.getBoundingClientRect().height) : null,
    koerperRollt: !!(koerper && koerper.scrollHeight > koerper.clientHeight + 1),
    ganzRollt: u.scrollHeight > u.clientHeight + 1 && rollt(u),
    kopf: !!(kopf && sichtbar(kopf) && kopf.getBoundingClientRect().top >= rand.oben - 1),
    fuss: knoepfe.length > 0 && knoepfe.every(treffbar), knoepfe: knoepfe.map(k => k.textContent.trim()),
    fragment: document.querySelectorAll('.epos-modulparameter').length,
    fremdeRoller,
  };
}

const satzUebZeilen = [];
function pruefeSatzUeberlagerung(fall, fenster, zustand, u) {
  const kennung = `${fall} ${fenster.breite}x${fenster.hoehe} ${zustand}`;
  const fehler = [];
  if (!u.da) fehler.push('Satz-Ueberlagerung fehlt');
  else {
    if (!u.kopf) fehler.push('Kopf der Satz-Ueberlagerung nicht sichtbar');
    if (!u.fuss) fehler.push('Fussleiste der Satz-Ueberlagerung nicht sichtbar oder nicht treffbar (' + u.knoepfe.join(', ') + ')');
    if (u.ganzRollt) fehler.push('Satz-Ueberlagerung rollt als Ganzes');
    if (u.fremdeRoller.length) fehler.push('Rollbereich in der Satz-Ueberlagerung ausser dem Koerper: ' + u.fremdeRoller.join('; '));
    if (u.fragment !== 1) fehler.push(`Fragment ${u.fragment}-mal gezeichnet statt einmal`);
    satzUebZeilen.push(`${kennung}: Ueberlagerung ${u.oben}..${u.unten} px, Breite ${u.breite} px, Koerper ${u.koerper} px${u.koerperRollt ? ' (rollt)' : ''}, Kopf ${u.kopf ? 'sichtbar' : 'FEHLT'}, Fussleiste ${u.fuss ? 'sichtbar' : 'FEHLT'} (${u.knoepfe.join(' / ')})`);
  }
  for (const f of fehler) { verstoesse.push(kennung + ': ' + f); console.log('  VERSTOSS ' + kennung + ': ' + f); }
  return fehler.length;
}

async function ruhe(seite) {
  await seite.evaluate(() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))));
  await seite.waitForTimeout(120);
}

/** DZ1-N2: wartet, bis das Zeichenmodell der Ganglinie in der gemessenen Behaeltergroesse steht (hoechstens 4 s). */
async function massAbwarten(seite) {
  await seite.waitForFunction(t => {
    const fl = document.querySelector('.epos-zweispalten-satz .epos-ganglinie-grafik .epos-diagramm-svg-flaeche');
    const vb = fl && fl.querySelector(':scope > svg')?.viewBox?.baseVal;
    if (!fl || !vb) return true;
    return Math.abs(vb.width - fl.clientWidth) <= t && Math.abs(vb.height - fl.clientHeight) <= t;
  }, MASS_TOLERANZ, { timeout: 4000 }).catch(() => {});
  await ruhe(seite);
}

async function taste(seite, key) {
  const t = seite.locator('.epos-zweispalten-trenner');
  if (await t.count() === 0) return false;
  await t.focus();
  await seite.keyboard.press(key);
  await ruhe(seite);
  return true;
}

async function satz(seite, offen) {
  const z = seite.locator('.epos-zweispalten-satzzeile');
  if (await z.count() === 0) return false;
  const ist = (await z.getAttribute('aria-expanded')) === 'true';
  if (ist !== offen) { await z.evaluate(e => e.click()); await ruhe(seite); }
  return true;
}

function pruefen(fall, fenster, zustand, m, konsole, istWirt, frei) {
  const kennung = `${fall} ${fenster.breite}x${fenster.hoehe} ${zustand}`;
  const fehler = [];
  // Stufe: nach der Medienabfrage; Schrift 13 px (Normalstufe) bzw. 12 px (Kompaktstufe).
  const sollSchrift = m.kompakt ? 12 : 13;
  if (m.schrift !== null && Math.abs(m.schrift - sollSchrift) > 0.1) fehler.push(`Schrift ${m.schrift} px statt ${sollSchrift} px (${m.kompakt ? 'Kompakt' : 'Normal'}stufe)`);
  if (fenster.neu && !m.kompakt) fehler.push('Kompaktstufe nicht aktiv');
  if (istWirt && m.projektZeile !== null) {
    const soll = m.kompakt ? 46 : 53;
    if (Math.abs(m.projektZeile - soll) > 1) fehler.push(`Projektzeile ${m.projektZeile} px statt ${soll} px`);
  }
  if (istWirt && m.katalogZeile !== null) {
    const soll = m.kompakt ? [46, 40] : [53, 46];
    if (!soll.some(s => Math.abs(m.katalogZeile - s) <= 1)) fehler.push(`Katalogzeile ${m.katalogZeile} px statt ${soll.join(' oder ')} px`);
  }
  if (istWirt && m.projekt !== null && m.projekt + 1 < m.projektMin) fehler.push(`Projektliste ${m.projekt} px unter ihrer Untergrenze ${Math.round(m.projektMin)} px`);
  if (frei && !frei.ok && !m.ueberlagerung) fehler.push('Schlussleiste nicht erreichbar: ' + frei.fehlt.join(', '));
  // iPad: der Dialog (und eine offene Ueberlagerung) steht ganz zwischen Statusleiste und Home-Anzeige.
  if (fenster.ipad) {
    const r = rand(fenster), unten = fenster.hoehe - r.unten;
    if (m.dialogOben !== null && (m.dialogOben < r.oben - 1 || m.dialogUnten > unten + 1))
      fehler.push(`Dialog ${Math.round(m.dialogOben)}..${Math.round(m.dialogUnten)} px nicht zwischen den sicheren Abstaenden (${r.oben}..${unten})`);
    if (m.ueberlagerungRahmen && (m.ueberlagerungRahmen[0] < r.oben - 1 || m.ueberlagerungRahmen[1] > unten + 1))
      fehler.push(`Ueberlagerung ${Math.round(m.ueberlagerungRahmen[0])}..${Math.round(m.ueberlagerungRahmen[1])} px nicht zwischen den sicheren Abstaenden`);
  }
  // Unter der Mindesthoehe rollt der Dialogkoerper (nur in den neuen Fenstern erlaubt, dort Befund).
  // DZ1: Mit aufgeklappter Ganglinie gehoert deren Untergrenze (260 px) zur Mindesthoehe - dann darf der
  // Dialogkoerper in jedem Fenster rollen (Befund).
  // DZ1-N1: aufgeklappt mit Ganglinie - Kopf hoechstens 90 px, Kurve mindestens ihre Untergrenze, kein
  // eigener Rollbalken; in 1 280 x 800 und 1 280 x 720 rollt der Dialogkoerper der ROLLT_NICHT-Faelle nicht.
  if (istWirt && m.satzOffen && m.ganglinie && m.bild !== null) {
    if (fenster.breite >= 1024 && m.kopf > KOPF_MAX) fehler.push(`Ganglinie: Kopf der Satzflaeche ${m.kopf} px > ${KOPF_MAX} px`);
    if (m.bild + 1 < m.kurveMin) fehler.push(`Ganglinie: Kurve ${m.bild} px < Untergrenze ${m.kurveMin} px`);
    if (m.satzRollt) fehler.push('Ganglinie: Satzflaeche rollt in sich');
    // DZ1-N2: volle Breite und Zeichenmodell in Behaeltergroesse.
    if (m.bildBreite < BREITE_ANTEIL * m.satzBreite)
      fehler.push(`Ganglinie: Kurve ${m.bildBreite} px breit < ${Math.round(BREITE_ANTEIL * 100)} % der Satzflaeche (${m.satzBreite} px)`);
    if (!m.modell || Math.abs(m.modell.breite - m.bildBreite) > MASS_TOLERANZ || Math.abs(m.modell.hoehe - m.bild) > MASS_TOLERANZ)
      fehler.push(`Ganglinie: Zeichenmodell ${m.modell ? m.modell.breite + ' x ' + m.modell.hoehe : 'fehlt'} nicht in Behaeltergroesse ${m.bildBreite} x ${m.bild}`);
    if (fenster.breite === 1280 && (fenster.hoehe === 800 || fenster.hoehe === 720) && ROLLT_NICHT.includes(fall) && m.dialogRollt
        && m.bild > m.kurveMin + UNTERGRENZE_SPIEL)
      fehler.push(`Ganglinie: Dialogkoerper rollt bei ${fenster.breite} x ${fenster.hoehe}, obwohl die Kurve (${m.bild} px) ueber ihrer Untergrenze steht`);
    kurven.push(`${fall} ${fenster.breite}x${fenster.hoehe} ${zustand}: Kurve ${m.bildBreite} x ${m.bild} px, Satzflaeche ${m.satzBreite} px, Modell ${m.modell ? m.modell.breite + ' x ' + m.modell.hoehe : '-'}, Dialog rollt ${m.dialogUeberhang} px`);
  }
  if (m.eng && (fenster.neu || (m.satzOffen && m.ganglinie))) { befunde.push(kennung + ': Dialogkoerper rollt (unter der Mindesthoehe)'); m = { ...m, dialogRollt: false }; }
  const aussen = p => fall === 'kaeltemaschine' || / in div\.epos-ueberlagerung/.test(p);
  const eigen = m.verschachtelt.filter(p => !aussen(p)), fremd = m.verschachtelt.filter(aussen);
  if (eigen.length) fehler.push('Rollbereich im Rollbereich: ' + eigen.join('; '));
  if (fremd.length) befunde.push(kennung + ': ' + fremd.join('; '));
  if (m.dokumentRollt) fehler.push('Dokument rollt');
  if (m.dialogRollt) fehler.push('Dialogkoerper rollt');
  if (istWirt) {
    // Kopf + zwei Zeilen; unter 600 px Bausteinhoehe Kopf + eine Zeile (Stilblatt, @container katalogauswahl).
    // Gemessen mit der echten Zeilenhoehe der Katalogliste (Kaestchenmodus 46 px, sonst 53 px).
    const zeile = m.katalogZeile || (m.kompakt ? ZEILE_KOMPAKT : ZEILE);
    // Kompaktstufe: immer zwei Zeilen (darunter rollt der Dialogkoerper).
    const soll = ((!m.kompakt && m.zweispalten !== null && m.zweispalten < 600) ? 1 : 2) * zeile;
    if (m.katalog !== null && m.katalogKopf !== null && m.katalog + 2 < m.katalogKopf + soll)
      fehler.push(`Katalogliste ${m.katalog} px < Kopf ${m.katalogKopf} + ${soll / zeile} Zeile(n) a ${zeile} px`);
    // Heizkessel (Stufe 2, Entscheid der Orchestrierung zu Konzept 4.8): in JEDEM Fenster Kopf und
    // mindestens zwei Katalogzeilen - erreicht durch Suche in der Kopfleiste und Kontext im
    // Dialogkopf, nicht durch eine Zeile als Untergrenze. Gemessen mit der echten Zeilenhoehe.
    if (fall === 'heizkessel' && m.katalog !== null && m.katalogKopf !== null) {
      const zeile = m.katalogZeile || (m.kompakt ? ZEILE_KOMPAKT : ZEILE);
      if (m.katalog + 2 < m.katalogKopf + 2 * zeile)
        fehler.push(`Heizkessel: Katalogliste ${m.katalog} px < Kopf ${m.katalogKopf} + 2 Zeilen a ${zeile} px`);
    }
    // DZ1: aufgeklappt Listen auf ihren Untergrenzen, die Satzflaeche nimmt den Rest.
    if (m.satzOffen && m.satz !== null) {
      if (m.projekt !== null && m.projekt > m.projektMin + 2) fehler.push(`Detailzeile auf: Projektliste ${m.projekt} px ueber ihrer Untergrenze ${Math.round(m.projektMin)} px`);
      if (m.katalog !== null && m.katalog > m.katalogMin + 2) fehler.push(`Detailzeile auf: Katalogliste ${m.katalog} px ueber ihrer Untergrenze ${Math.round(m.katalogMin)} px`);
      if (m.satzRest > 3) fehler.push(`Detailzeile auf: Satzflaeche ${m.satz} px laesst ${m.satzRest} px frei`);
      if (m.bild !== null && !m.bildVoll && m.bild + 2 < m.bildPlatz) fehler.push(`Ganglinie ${m.bild} px hoch, Platz ${m.bildPlatz} px`);
    }
    fehler.push(...m.leistenfehler);
    if (m.ueberlauf.length) fehler.push('Ueberlauf: ' + m.ueberlauf.join('; '));
  }
  if (konsole.length) fehler.push('Konsole: ' + konsole.join(' | '));
  zeilen.push({ kennung, ...m, fehler: fehler.length });
  for (const f of fehler) { verstoesse.push(kennung + ': ' + f); console.log('  VERSTOSS ' + kennung + ': ' + f); }
}

// ---------------------------------------------------------------------------
//  Lauf
// ---------------------------------------------------------------------------

if (FOTOS) mkdirSync(FOTOS, { recursive: true });
const browser = await chromium.launch();
let rueckgabe = 0;
try {
  for (const [fall, pfad] of FAELLE) {
    for (const fenster of FENSTER) {
      const kontext = await browser.newContext({ viewport: { width: fenster.breite, height: fenster.hoehe } });
      const seite = await kontext.newPage();
      const konsole = [];
      seite.on('console', m => { if (m.type() === 'error') konsole.push(m.text().slice(0, 160)); });
      seite.on('pageerror', e => konsole.push(String(e).slice(0, 160)));
      const laden = async () => {
        await seite.goto(WURZEL + pfad + (pfad.includes('?') ? '&' : '?') + 'zeilen=40', { waitUntil: 'networkidle' });
        if (fenster.ipad) await seite.addStyleTag({ content: SICHER_TOKEN });
        await seite.waitForSelector('body > #app > .epos-dialog', { timeout: 15000 });
        await ruhe(seite);
      };
      await laden();
      const istWirt = await seite.locator('.epos-zweispalten').count() > 0;
      const mess = async zustand => pruefen(fall, fenster, zustand, await seite.evaluate(messen, KOMPAKT), konsole.splice(0), istWirt,
        await seite.evaluate(erreichbar, rand(fenster)));

      await mess('Vorgabe');
      if (FOTOS && fenster.neu && (fall === 'heizkessel' || fall === 'gebaeude' || (fenster.ipad && fall === 'kaeltemaschine')))
        await seite.screenshot({ path: `${FOTOS}/${fall}_${fenster.breite}x${fenster.hoehe}.png` });
      if (istWirt) {
        if (await taste(seite, 'Home')) await mess('Trennlinie oben');
        if (await taste(seite, 'End')) await mess('Trennlinie unten');
        // Ganglinien-Dialoge (DZ1): erst eine Projektzeile waehlen - die Detailzeile zeigt dann ihre Ganglinie.
        if (/ganglinie|waermebedarf/.test(fall) && await seite.locator('.epos-zweispalten-bereich--projekt tbody tr button').count()) {
          await seite.locator('.epos-zweispalten-bereich--projekt tbody tr button').first().click();
          await ruhe(seite);
        }
        if (await satz(seite, true)) {
          if (/ganglinie|waermebedarf/.test(fall) && await seite.locator('.epos-zweispalten-satz .epos-ganglinie-grafik .epos-diagramm-svg-flaeche').count() === 0) {
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Detailzeile ohne Ganglinie`); console.log('  VERSTOSS ' + verstoesse.at(-1));
          }
          await massAbwarten(seite);
          await mess('Detailzeile auf, Trennlinie unten');
          if (FOTOS && /ganglinie|waermebedarf/.test(fall))
            await seite.screenshot({ path: `${FOTOS}/${fall}_${fenster.breite}x${fenster.hoehe}.png` });
          if (await taste(seite, 'Home')) { await massAbwarten(seite); await mess('Detailzeile auf, Trennlinie oben'); }
          await satz(seite, false);
          await mess('Detailzeile wieder zu');
        }
        // Ziehen mit der Maus und Neuladen: die Hoehe kommt aus Dienste.Einstellungen wieder.
        const t = seite.locator('.epos-zweispalten-trenner');
        if (await t.count()) {
          await taste(seite, 'Home');
          const b = await t.boundingBox();
          await seite.mouse.move(b.x + b.width / 2, b.y + 4);
          await seite.mouse.down();
          await seite.mouse.move(b.x + b.width / 2, b.y + 4 + 2 * ZEILE, { steps: 6 });
          await seite.mouse.up();
          await ruhe(seite);
          const gezogen = (await seite.evaluate(messen, KOMPAKT)).projekt;
          await mess('gezogen');
          await laden();
          const nachher = (await seite.evaluate(messen, KOMPAKT)).projekt;
          await mess('neu geladen');
          if (Math.abs(nachher - gezogen) > 2) {
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Hoehe nach Neuladen ${nachher} statt ${gezogen} px`);
            console.log('  VERSTOSS ' + verstoesse.at(-1));
          }
          await taste(seite, 'Home');               // Ausgang fuer den naechsten Fall
          await t.dblclick();                        // Vorgabe und Ablage geloescht
          await ruhe(seite);
        }
        // Heizkessel (Stufe 2): Detailzeile auf mit Kosten, Ueberlagerung "Bearbeiten..." fuer die
        // Projektkopie und fuer zwei angekreuzte Katalogsaetze (Blaetterleiste).
        if (fall === 'heizkessel') {
          await satz(seite, true);
          if (await seite.locator('.epos-zweispalten-satz .epos-kostenleiste button').count() === 0)
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Detailzeile ohne Kostenknoepfe`);
          await mess('Detailzeile auf mit Kosten');
          await satz(seite, false);
          // UeS1: „Bearbeiten…" EINER Projektkopie oeffnet die Satz-Ueberlagerung in voller Hoehe.
          await seite.locator('.epos-knopf--bearbeiten-projekt').click();
          await seite.waitForSelector('.epos-ueberlagerung--satz', { timeout: 5000 });
          await ruhe(seite);
          await mess('Satz-Ueberlagerung offen');
          pruefeSatzUeberlagerung(fall, fenster, 'Satz-Ueberlagerung offen', await seite.evaluate(satzUeberlagerung, rand(fenster)));
          if (FOTOS && ((fenster.breite === 1280 && fenster.hoehe === 800) || (fenster.ipad && (fenster.breite === 1194 || fenster.breite === 834))))
            await seite.screenshot({ path: `${FOTOS}/heizkessel_satzueberlagerung_${fenster.breite}x${fenster.hoehe}.png` });
          await seite.keyboard.press('Escape');
          await ruhe(seite);
          if (await seite.locator('.epos-ueberlagerung--satz').count())
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Esc schliesst die Satz-Ueberlagerung nicht`);
          // Derselbe Weg ueber „Vergroessern" in der Detailzeile; Abbrechen schliesst.
          await seite.locator('.epos-zweispalten-vergroessern').click();
          await seite.waitForSelector('.epos-ueberlagerung--satz', { timeout: 5000 });
          await ruhe(seite);
          pruefeSatzUeberlagerung(fall, fenster, 'Satz-Ueberlagerung ueber Vergroessern', await seite.evaluate(satzUeberlagerung, rand(fenster)));
          await seite.locator('.epos-satzueberlagerung-abbrechen').click();
          await ruhe(seite);
          if (await seite.locator('.epos-ueberlagerung--satz').count())
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Abbrechen schliesst die Satz-Ueberlagerung nicht`);
          const k = seite.locator('.epos-zweispalten-bereich--katalog td .epos-kaestchenzelle input');
          await k.nth(0).check();
          await k.nth(1).check();
          await ruhe(seite);
          await seite.locator('.epos-knopf--bearbeiten-katalog').click();
          await seite.waitForSelector('.epos-satzbearbeitung', { timeout: 5000 });
          await ruhe(seite);
          // Zwei ungesperrte Saetze: Blaetterleiste; ein gesperrter darunter: Hinweiszeile.
          if (await seite.locator('.epos-satzbearbeitung-blaetter, .epos-satzbearbeitung-hinweis--uebersprungen').count() === 0)
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Mehrfach-Bearbeiten ohne Blaetterleiste und ohne Hinweis`);
          await mess('Bearbeiten mehrfach offen');
          await seite.keyboard.press('Escape');
          await ruhe(seite);
          if (await seite.locator('.epos-satzbearbeitung').count())
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Esc schliesst "Bearbeiten..." nicht`);
          await mess('nach Bearbeiten');
          // Stufe 2b: die Rueckfrage „In die Datenbank übernehmen…" als Ueberlagerung - drei Zeilen, kein eigener
          // Rollbereich; Esc schliesst sie.
          await seite.locator('.epos-knopf--rueckweg').click();
          await seite.waitForSelector('.epos-rueckweg', { timeout: 5000 });
          await ruhe(seite);
          if (await seite.locator('.epos-rueckweg-zeile').count() !== 3)
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Rueckfrage ohne ihre drei Zeilen`);
          await mess('Rueckfrage-Ueberlagerung offen');
          await seite.keyboard.press('Escape');
          await ruhe(seite);
          if (await seite.locator('.epos-rueckweg').count())
            verstoesse.push(`${fall} ${fenster.breite}x${fenster.hoehe}: Esc schliesst die Rueckfrage nicht`);
        }
        // Ueberlagerung offen: Gebaeude -> "Simulation..." (wie die Fensterprobe).
        if (fall === 'gebaeude') {
          const k = seite.locator('body > #app > .epos-dialog button', { hasText: 'Simulation...' });
          if (await k.count()) { await k.first().click(); await seite.waitForSelector('.epos-ueberlagerung', { timeout: 5000 }); await ruhe(seite); await mess('Ueberlagerung offen'); }
          else { verstoesse.push(`${fall}: Knopf "Simulation..." nicht gefunden`); }
        }
      }
      await kontext.close();
    }
  }

  // ---- Gegenprobe --------------------------------------------------------
  let gegen = { verschachtelt: 0, dialog: false };
  if (MIT_GEGENPROBE) {
    const kontext = await browser.newContext({ viewport: { width: 1280, height: 800 } });
    const seite = await kontext.newPage();
    await seite.goto(WURZEL + '/fensterprobe?fall=heizkessel&zeilen=40', { waitUntil: 'networkidle' });
    await seite.waitForSelector('.epos-zweispalten');
    await seite.addStyleTag({ content: '.epos-zweispalten-bereich--katalog > .epos-zweispalten-inhalt { overflow: auto !important; }' });
    await ruhe(seite);
    gegen.verschachtelt = (await seite.evaluate(messen, KOMPAKT)).verschachtelt.length;
    await seite.evaluate(() => {
      const d = document.querySelector('body > #app > .epos-dialog');
      d.style.setProperty('overflow', 'auto', 'important');
      const klotz = document.createElement('div');
      klotz.style.cssText = 'height: 2000px; flex: 0 0 auto';
      d.insertBefore(klotz, d.querySelector('.epos-zweispalten'));
    });
    await ruhe(seite);
    gegen.dialog = (await seite.evaluate(messen, KOMPAKT)).dialogRollt;
    await kontext.close();
    console.log(`Gegenprobe: verschachtelt ${gegen.verschachtelt} Paar(e), Dialogkoerper rollt: ${gegen.dialog}`);
    if (gegen.verschachtelt === 0 || !gegen.dialog) { console.log('  GEGENPROBE GRUEN - die Probe sieht nichts'); rueckgabe = 1; }

    // Satz-Ueberlagerung (UeS1): ein Rollbereich im Koerper, eine als Ganzes rollende Ueberlagerung mit
    // weggerollter Fussleiste muessen rot werden.
    {
      const k6 = await browser.newContext({ viewport: { width: 1280, height: 720 } });
      const s6 = await k6.newPage();
      await s6.goto(WURZEL + '/fensterprobe?fall=heizkessel&zeilen=40', { waitUntil: 'networkidle' });
      await s6.waitForSelector('.epos-zweispalten');
      await s6.locator('.epos-knopf--bearbeiten-projekt').click();
      await s6.waitForSelector('.epos-ueberlagerung--satz', { timeout: 5000 });
      await s6.addStyleTag({ content: '.epos-satzueberlagerung-koerper .epos-modulparameter { overflow: auto !important; max-height: 60px !important; }' });
      await ruhe(s6);
      const vor = verstoesse.length;
      pruefeSatzUeberlagerung('gegenprobe-satzueberlagerung', { breite: 1280, hoehe: 720 }, 'Rollbereich im Koerper', await s6.evaluate(satzUeberlagerung));
      const rot1 = verstoesse.slice(vor).some(v => v.includes('ausser dem Koerper'));
      verstoesse.splice(vor); satzUebZeilen.pop();
      await s6.addStyleTag({ content: '.epos-satzueberlagerung-koerper .epos-modulparameter { overflow: visible !important; max-height: none !important; }'
        + ' .epos-ueberlagerung.epos-ueberlagerung--satz { overflow-y: auto !important; }'
        + ' .epos-satzueberlagerung, .epos-ueberlagerung--satz > .epos-ueberlagerung-inhalt, .epos-satzueberlagerung-koerper { flex: 0 0 auto !important; overflow: visible !important; }'
        + ' .epos-satzueberlagerung-koerper::after { content: ""; display: block; height: 2000px; }' });
      await ruhe(s6);
      const vor2 = verstoesse.length;
      pruefeSatzUeberlagerung('gegenprobe-satzueberlagerung', { breite: 1280, hoehe: 720 }, 'rollt als Ganzes', await s6.evaluate(satzUeberlagerung));
      const neu2 = verstoesse.slice(vor2);
      const rot2 = neu2.some(v => v.includes('als Ganzes')) && neu2.some(v => v.includes('Fussleiste'));
      verstoesse.splice(vor2); satzUebZeilen.pop();
      console.log(`Gegenprobe Satz-Ueberlagerung: Rollbereich im Koerper - ${rot1 ? 'rot' : 'gruen'}; rollt als Ganzes, Fussleiste weggerollt - ${rot2 ? 'rot' : 'gruen'}`);
      if (!rot1 || !rot2) { console.log('  GEGENPROBE GRUEN - die Probe sieht die Satz-Ueberlagerung nicht'); rueckgabe = 1; }
      await k6.close();
    }

    // Vorrang (DZ1): eine Satzflaeche, die auf 100 px begrenzt kleiner als der Rest bleibt, muss rot werden.
    {
      const k3 = await browser.newContext({ viewport: { width: 1280, height: 800 } });
      const s3 = await k3.newPage();
      await s3.goto(WURZEL + '/fensterprobe?fall=heizkessel&zeilen=40', { waitUntil: 'networkidle' });
      await s3.waitForSelector('.epos-zweispalten');
      await s3.addStyleTag({ content: '.epos-zweispalten-satz { max-height: 100px !important; }' });
      await satz(s3, true);
      const vor = verstoesse.length;
      const n = zeilen.length;
      pruefen('gegenprobe-vorrang', { breite: 1280, hoehe: 800 }, 'Satzflaeche 100 px', await s3.evaluate(messen, KOMPAKT), [], true, null);
      const rot = verstoesse.slice(vor).some(v => v.includes('laesst'));
      verstoesse.splice(vor); zeilen.splice(n);
      console.log(`Gegenprobe Vorrang: Satzflaeche auf 100 px begrenzt - ${rot ? 'rot' : 'gruen'}`);
      if (!rot) { console.log('  GEGENPROBE GRUEN - die Probe sieht den Vorrang nicht'); rueckgabe = 1; }
      await k3.close();
    }

    // Kopfleiste (MO3): ein Rand von 6 px am Umstellknopf der Waermepumpen laesst die Kopfleiste der
    // Projektliste bei aufgeklappter Detailzeile ueberlaufen - das muss rot werden.
    {
      const k5 = await browser.newContext({ viewport: { width: 1280, height: 800 } });
      const s5 = await k5.newPage();
      await s5.goto(WURZEL + '/fensterprobe?fall=waermepumpen&zeilen=40', { waitUntil: 'networkidle' });
      await s5.waitForSelector('.epos-zweispalten');
      await s5.addStyleTag({ content: '.epos-wp-umstellen { margin-top: 6px !important; }' });
      await satz(s5, true);
      const vor = verstoesse.length, n = zeilen.length, b = befunde.length;
      pruefen('waermepumpen', { breite: 1280, hoehe: 800 }, 'Gegenprobe Kopfleiste', await s5.evaluate(messen, KOMPAKT), [], true, null);
      const rot = verstoesse.slice(vor).some(v => v.includes('Touchziel')) && verstoesse.slice(vor).some(v => v.includes('Ueberlauf'));
      verstoesse.splice(vor); zeilen.splice(n); befunde.splice(b);
      console.log(`Gegenprobe Kopfleiste: Umstellknopf mit 6 px Rand - ${rot ? 'rot' : 'gruen'}`);
      if (!rot) { console.log('  GEGENPROBE GRUEN - die Probe sieht die Kopfleiste nicht'); rueckgabe = 1; }
      await k5.close();
    }

    // Verdichteter Kopf (DZ1-N1): eine auf 60 px gedrueckte Kurve und eine Kurven-Untergrenze von 300 px
    // (der Dialogkoerper muss dann rollen) in der Solarganglinie bei 1 280 x 800 muessen rot werden.
    for (const [art, stil, muster] of [
      ['Kurve 60 px', '.epos-ganglinie-grafik .epos-diagramm-svg-flaeche { max-height: 60px !important; }', 'Untergrenze'],
      ['Breite 300 px', '.epos-ganglinie-grafik .epos-diagramm-svg-flaeche { max-width: 300px !important; }', 'der Satzflaeche'],
      ['Satzflaeche 600 px', '.epos-zweispalten { --epos-satz-untergrenze: 600px !important; }', 'Dialogkoerper rollt bei']]) {
      const k4 = await browser.newContext({ viewport: { width: 1280, height: 800 } });
      const s4 = await k4.newPage();
      await s4.goto(WURZEL + '/rollbereichprobe?fall=solarganglinie&zeilen=40', { waitUntil: 'networkidle' });
      await s4.waitForSelector('.epos-zweispalten');
      await s4.addStyleTag({ content: stil });
      await s4.locator('.epos-zweispalten-bereich--projekt tbody tr button').first().click();
      await ruhe(s4);
      await satz(s4, true);
      await ruhe(s4); await massAbwarten(s4);
      const vor = verstoesse.length, n = zeilen.length, b = befunde.length, kn = kurven.length;
      pruefen('solarganglinie', { breite: 1280, hoehe: 800 }, 'Gegenprobe ' + art, await s4.evaluate(messen, KOMPAKT), [], true, null);
      const rot = verstoesse.slice(vor).some(v => v.includes(muster));
      verstoesse.splice(vor); zeilen.splice(n); befunde.splice(b); kurven.splice(kn);
      console.log(`Gegenprobe verdichteter Kopf: ${art} - ${rot ? 'rot' : 'gruen'}`);
      if (!rot) { console.log('  GEGENPROBE GRUEN - die Probe sieht die Ganglinie nicht'); rueckgabe = 1; }
      await k4.close();
    }

    // Rollbalken (KB1): ein Klotz von 400 px im Dialog - der Baustein faellt auf seine Mindesthoehe,
    // der Dialogkoerper rollt, die Schlussleiste bleibt erreichbar. Derselbe Klotz ohne Rollbalken
    // (overflow: hidden) muss rot werden.
    for (const ohne of [false, true]) {
      const k2 = await browser.newContext({ viewport: { width: 1093, height: 614 } });
      const s2 = await k2.newPage();
      const kon = [];
      s2.on('pageerror', e => kon.push(String(e).slice(0, 160)));
      await s2.goto(WURZEL + '/fensterprobe?fall=heizkessel&zeilen=40', { waitUntil: 'networkidle' });
      await s2.waitForSelector('.epos-zweispalten');
      await ruhe(s2);
      if (ohne) await s2.addStyleTag({ content: 'body > #app > .epos-dialog { overflow: hidden !important; }' });
      await s2.evaluate(() => {
        const d = document.querySelector('body > #app > .epos-dialog');
        const klotz = document.createElement('div');
        klotz.className = 'kb1-klotz';
        klotz.style.cssText = 'height: 400px; flex: 0 0 auto';
        d.insertBefore(klotz, d.querySelector('.epos-zweispalten'));
      });
      await ruhe(s2); await ruhe(s2);
      const m = await s2.evaluate(messen, KOMPAKT);
      const frei = await s2.evaluate(erreichbar);
      const zeile = m.katalogZeile || ZEILE;
      const gruen = frei.ok && m.eng && m.katalog + 2 >= m.katalogKopf + 2 * zeile && m.projekt + 1 >= m.projektMin && !kon.length;
      console.log(`Gegenprobe Rollbalken ${ohne ? 'ohne Rollbalken' : 'mit Rollbalken'}: eng ${m.eng}, Schlussleiste erreichbar ${frei.ok}`
        + ` (${frei.n ?? 0} Knoepfe${frei.fehlt.length ? ', fehlt ' + frei.fehlt.join(', ') : ''}), Projektliste ${m.projekt} px, Katalogliste ${m.katalog} px`);
      if (!ohne && !gruen) { verstoesse.push('Rollbalken: Klotz 400 px - ' + JSON.stringify({ eng: m.eng, frei, projekt: m.projekt, katalog: m.katalog, kon })); console.log('  VERSTOSS ' + verstoesse.at(-1)); }
      if (ohne && frei.ok) { console.log('  GEGENPROBE GRUEN - ohne Rollbalken bleibt die Schlussleiste erreichbar, die Probe sieht nichts'); rueckgabe = 1; }
      await k2.close();
    }

    // Sichere Abstaende (iPad): Heizkessel bei 1 194 x 834 mit dem Mass der Abstaende, aber OHNE die Token -
    // der Dialog reicht unter Statusleiste und Home-Anzeige, die Probe muss es sehen.
    {
      const ipad = FENSTER.find(f => f.ipad && f.breite === 1194);
      const k3 = await browser.newContext({ viewport: { width: ipad.breite, height: ipad.hoehe } });
      const s3 = await k3.newPage();
      await s3.goto(WURZEL + '/fensterprobe?fall=heizkessel&zeilen=40', { waitUntil: 'networkidle' });
      await s3.waitForSelector('.epos-zweispalten');
      await ruhe(s3);
      const m = await s3.evaluate(messen, KOMPAKT);
      const frei = await s3.evaluate(erreichbar, SICHER);
      const vorher = verstoesse.length, vorherB = befunde.length;
      pruefen('gegenprobe-heizkessel', ipad, 'ohne Token', m, [], true, frei);
      const rot = verstoesse.length - vorher;
      verstoesse.splice(vorher); befunde.splice(vorherB); zeilen.pop();
      console.log(`Gegenprobe sichere Abstaende (ohne Token): Dialog ${Math.round(m.dialogOben)}..${Math.round(m.dialogUnten)} px, `
        + `Schlussleiste ueber der Home-Anzeige ${frei.ok}, ${rot} Verstoss(e)`);
      if (rot === 0) { console.log('  GEGENPROBE GRUEN - ohne Token steht der Dialog zwischen den Abstaenden, die Probe sieht nichts'); rueckgabe = 1; }
      await k3.close();
    }
  }

  console.log('\nFall | Fenster | Zustand | Stufe (Schrift) | Rollbereiche | Baustein | Projektliste (Zeile) | Katalogliste (Kopf, Zeile) | Satzflaeche (Bild/Platz) | Verstoesse');
  for (const z of zeilen) {
    const [fall, fenster, ...zust] = z.kennung.split(' ');
    console.log(`${fall} | ${fenster} | ${zust.join(' ')} | ${z.kompakt ? 'kompakt' : 'normal'} (${z.schrift} px)${z.eng ? ' eng' : ''} | ${z.roller} | ${z.zweispalten ?? '-'} | ${z.projekt ?? '-'} (${z.projektZeile ?? '-'}) | ${z.katalog ?? '-'} (${z.katalogKopf ?? '-'}, ${z.katalogZeile ?? '-'}) | ${z.satz ?? '-'}${z.bild !== null && z.bild !== undefined ? ` (${z.bild}/${z.bildPlatz})` : ''} | ${z.fehler}`);
  }
  console.log(`\nSatz-Ueberlagerung (UeS1, ${satzUebZeilen.length}):`);
  for (const z of satzUebZeilen) console.log('  ' + z);
  console.log(`\nKurven der Ganglinie (DZ1-N2, ${kurven.length}):`);
  for (const k of kurven) console.log('  ' + k);
  console.log(`\n${zeilen.length} Zustaende, ${verstoesse.length} Verstoesse`);
  if (befunde.length) {
    console.log(`\nBefunde ausserhalb des Bausteins (${befunde.length}, nicht gezaehlt):`);
    for (const b of befunde) console.log('  BEFUND ' + b);
  }
  if (verstoesse.length) rueckgabe = 1;
} catch (e) {
  console.error('Aufbaufehler: ' + (e && e.stack || e));
  rueckgabe = 2;
} finally {
  await browser.close();
}
process.exit(rueckgabe);
