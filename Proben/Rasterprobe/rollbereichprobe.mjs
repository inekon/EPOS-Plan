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
// Aufruf: node rollbereichprobe.mjs --url http://127.0.0.1:5299 [--nur <fall>] [--ohne-gegenprobe]
// Rueckgabe 0 = kein Verstoss und Gegenprobe rot, 1 = Verstoss oder Gegenprobe gruen, 2 = Aufbaufehler.

import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

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
const ZEILE = 53;   // Zeilenhoehe, falls die Liste keine Zeile zeigt; sonst gemessen

const FENSTER = [{ breite: 1280, hoehe: 800 }, { breite: 1280, hoehe: 720 }, { breite: 1024, hoehe: 700 }];
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

function messen() {
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
  return {
    roller: roller.length, verschachtelt,
    dokumentRollt: dok.scrollHeight > innerHeight + 1 || dok.scrollWidth > innerWidth + 1,
    dialogRollt: d ? d.scrollHeight > d.clientHeight + 1 : false,
    dialogHoehe: h(d), projekt: h(projekt), katalog: h(katalog), katalogKopf: h(kopfzeile), katalogMin,
    katalogZeile: h(katalog && katalog.querySelector('tbody tr')),
    ueberlagerung: !!document.querySelector('.epos-ueberlagerung'), zweispalten: h(d && d.querySelector('.epos-zweispalten')),
    satzOffen: !!(d && d.querySelector('.epos-zweispalten--satz-offen')), ueberlauf,
    leistenfehler,
  };
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

function pruefen(fall, fenster, zustand, m, konsole, istWirt) {
  const kennung = `${fall} ${fenster.breite}x${fenster.hoehe} ${zustand}`;
  const fehler = [];
  const aussen = p => fall === 'kaeltemaschine' || / in div\.epos-ueberlagerung/.test(p);
  const eigen = m.verschachtelt.filter(p => !aussen(p)), fremd = m.verschachtelt.filter(aussen);
  if (eigen.length) fehler.push('Rollbereich im Rollbereich: ' + eigen.join('; '));
  if (fremd.length) befunde.push(kennung + ': ' + fremd.join('; '));
  if (m.dokumentRollt) fehler.push('Dokument rollt');
  if (m.dialogRollt) fehler.push('Dialogkoerper rollt');
  if (istWirt) {
    // Kopf + zwei Zeilen; unter 600 px Bausteinhoehe Kopf + eine Zeile (Stilblatt, @container katalogauswahl).
    // Gemessen mit der echten Zeilenhoehe der Katalogliste (Kaestchenmodus 46 px, sonst 53 px).
    const zeile = m.katalogZeile || ZEILE;
    const soll = ((m.zweispalten !== null && m.zweispalten < 600) ? 1 : 2) * zeile;
    if (m.katalog !== null && m.katalogKopf !== null && m.katalog + 2 < m.katalogKopf + soll)
      fehler.push(`Katalogliste ${m.katalog} px < Kopf ${m.katalogKopf} + ${soll / zeile} Zeile(n) a ${zeile} px`);
    // Heizkessel (Stufe 2, Entscheid der Orchestrierung zu Konzept 4.8): in JEDEM Fenster Kopf und
    // mindestens zwei Katalogzeilen - erreicht durch Suche in der Kopfleiste und Kontext im
    // Dialogkopf, nicht durch eine Zeile als Untergrenze. Gemessen mit der echten Zeilenhoehe.
    if (fall === 'heizkessel' && m.katalog !== null && m.katalogKopf !== null) {
      const zeile = m.katalogZeile || ZEILE;
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
      const mess = async zustand => pruefen(fall, fenster, zustand, await seite.evaluate(messen), konsole.splice(0), istWirt);

      await mess('Vorgabe');
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
          const gezogen = (await seite.evaluate(messen)).projekt;
          await mess('gezogen');
          await laden();
          const nachher = (await seite.evaluate(messen)).projekt;
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
    gegen.verschachtelt = (await seite.evaluate(messen)).verschachtelt.length;
    await seite.evaluate(() => {
      const d = document.querySelector('body > #app > .epos-dialog');
      d.style.setProperty('overflow', 'auto', 'important');
      const klotz = document.createElement('div');
      klotz.style.cssText = 'height: 2000px; flex: 0 0 auto';
      d.insertBefore(klotz, d.querySelector('.epos-zweispalten'));
    });
    await ruhe(seite);
    gegen.dialog = (await seite.evaluate(messen)).dialogRollt;
    await kontext.close();
    console.log(`Gegenprobe: verschachtelt ${gegen.verschachtelt} Paar(e), Dialogkoerper rollt: ${gegen.dialog}`);
    if (gegen.verschachtelt === 0 || !gegen.dialog) { console.log('  GEGENPROBE GRUEN - die Probe sieht nichts'); rueckgabe = 1; }
  }

  console.log('\nFall | Fenster | Zustand | Rollbereiche | Baustein | Projektliste | Katalogliste (Kopf) | Verstoesse');
  for (const z of zeilen) {
    const [fall, fenster, ...zust] = z.kennung.split(' ');
    console.log(`${fall} | ${fenster} | ${zust.join(' ')} | ${z.roller} | ${z.zweispalten ?? '-'} | ${z.projekt ?? '-'} | ${z.katalog ?? '-'} (${z.katalogKopf ?? '-'}) | ${z.fehler}`);
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
