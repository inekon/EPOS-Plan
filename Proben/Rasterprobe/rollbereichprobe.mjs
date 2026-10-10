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
const KOMPAKT = '(max-width: 1199.98px), (max-height: 799.98px)';   // Medienabfrage der Kompaktstufe (epos-ui.css)
const ZEILE = 53;   // Zeilenhoehe, falls die Liste keine Zeile zeigt; sonst gemessen
const ZEILE_KOMPAKT = 46;   // dieselbe in der Kompaktstufe

const FENSTER = [{ breite: 1280, hoehe: 800 }, { breite: 1280, hoehe: 720 }, { breite: 1024, hoehe: 700 },
  { breite: 1024, hoehe: 768, neu: true }, { breite: 768, hoehe: 1024, neu: true }, { breite: 1093, hoehe: 614, neu: true }];
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
  ['kaeltemaschine', '/rollbereichprobe?fall=kaeltemaschine'],
].filter(([n]) => !NUR || n.startsWith(NUR));

const verstoesse = [];
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
    ueberlagerung: !!document.querySelector('.epos-ueberlagerung'), zweispalten: h(d && d.querySelector('.epos-zweispalten')),
    satzOffen: !!(d && d.querySelector('.epos-zweispalten--satz-offen')), ueberlauf,
    leistenfehler,
  };
}

/** Alle Knoepfe der Schlussleiste nach dem Rollen des Dialogkoerpers ans Ende sichtbar und treffbar? */
function erreichbar() {
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
    if (r.top < -1 || r.bottom > innerHeight + 1 || r.left < -1 || r.right > innerWidth + 1) return true;
    const e = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    return !(e && (e === k || k.contains(e)));
  }).map(k => k.textContent.trim());
  d.scrollTop = vorher;
  return { ok: fehlt.length === 0 && knoepfe.length > 0, n: knoepfe.length, fehlt };
}

async function ruhe(seite) {
  await seite.evaluate(() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))));
  await seite.waitForTimeout(120);
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
  // Unter der Mindesthoehe rollt der Dialogkoerper (nur in den neuen Fenstern erlaubt, dort Befund).
  if (m.eng && fenster.neu) { befunde.push(kennung + ': Dialogkoerper rollt (unter der Mindesthoehe)'); m = { ...m, dialogRollt: false }; }
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
        await seite.waitForSelector('body > #app > .epos-dialog', { timeout: 15000 });
        await ruhe(seite);
      };
      await laden();
      const istWirt = await seite.locator('.epos-zweispalten').count() > 0;
      const mess = async zustand => pruefen(fall, fenster, zustand, await seite.evaluate(messen, KOMPAKT), konsole.splice(0), istWirt,
        await seite.evaluate(erreichbar));

      await mess('Vorgabe');
      if (FOTOS && fenster.neu && (fall === 'heizkessel' || fall === 'gebaeude'))
        await seite.screenshot({ path: `${FOTOS}/${fall}_${fenster.breite}x${fenster.hoehe}.png` });
      if (istWirt) {
        if (await taste(seite, 'Home')) await mess('Trennlinie oben');
        if (await taste(seite, 'End')) await mess('Trennlinie unten');
        if (await satz(seite, true)) {
          await mess('Detailzeile auf, Trennlinie unten');
          if (await taste(seite, 'Home')) await mess('Detailzeile auf, Trennlinie oben');
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
          await seite.locator('.epos-knopf--bearbeiten-projekt').click();
          await seite.waitForSelector('.epos-satzbearbeitung', { timeout: 5000 });
          await ruhe(seite);
          await mess('Bearbeiten Projektsatz offen');
          await seite.keyboard.press('Escape');
          await ruhe(seite);
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
  }

  console.log('\nFall | Fenster | Zustand | Stufe (Schrift) | Rollbereiche | Baustein | Projektliste (Zeile) | Katalogliste (Kopf, Zeile) | Verstoesse');
  for (const z of zeilen) {
    const [fall, fenster, ...zust] = z.kennung.split(' ');
    console.log(`${fall} | ${fenster} | ${zust.join(' ')} | ${z.kompakt ? 'kompakt' : 'normal'} (${z.schrift} px)${z.eng ? ' eng' : ''} | ${z.roller} | ${z.zweispalten ?? '-'} | ${z.projekt ?? '-'} (${z.projektZeile ?? '-'}) | ${z.katalog ?? '-'} (${z.katalogKopf ?? '-'}, ${z.katalogZeile ?? '-'}) | ${z.fehler}`);
  }
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
