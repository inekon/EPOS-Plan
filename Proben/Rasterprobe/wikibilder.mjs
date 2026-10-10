// WIKIBILDER - Bildschirmfotos der Dialoge fuer das Wiki.
//
// Nimmt je Bild die Seite /wikibild?bild=<name>&kultur=de-DE des Rasterprobe-Wirtes
// auf (echte EPOS.UI-Dialoge mit den neutralen Beispieldaten aus
// Wirt/Seiten/Wikibeispiele.cs) und schreibt das Bildschirmfoto des DIALOGS - nicht
// der leeren Seite - als Bildschirmfoto_<Maske>.png. Keine Messprobe: Es prueft nur,
// dass der Dialog steht und die Grenzen der Wache WikiBilderWacheTests einhaelt
// (Breite <= 1400 px, Groesse <= 300 KB). Regel: Konzept Hilfesystem 14.3 "PNG-Bilder".
//
// Aufruf (der Wirt laeuft, siehe LIESMICH.md):
//   node wikibilder.mjs --ziel ../../Projekte/Wiki/Dateien [--url http://127.0.0.1:5299] [--nur heizkessel]
//
// Rueckgabe 0 = alle Bilder geschrieben, 1 = ein Bild verfehlt Dialog oder Grenze,
// 2 = Aufruf- oder Verbindungsfehler.

import { mkdir, stat } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';
import path from 'node:path';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/wikibilder.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const ZIEL = arg('ziel', '');
const NUR = arg('nur', '');

const BREITE_MAX = 1400;
const GROESSE_MAX = 300 * 1024;

// Name der Gabe -> Maske im Dateinamen (ASCII).
const BILDER = [
  ['heizkessel', 'Heizkessel'],
  ['bhkw', 'BHKW'],
  ['waermepumpe', 'Waermepumpe'],
  ['pufferspeicher', 'Pufferspeicher'],
  ['stromspeicher', 'Stromspeicher'],
  ['photovoltaik', 'Photovoltaik'],
  ['solarkollektoren', 'Solarkollektoren'],
  ['brauchwasser', 'Brauchwasser'],
  ['prozesswaerme', 'Prozesswaerme'],
  ['gebaeude', 'Gebaeudedaten'],
  ['kaeltemaschine', 'Kaeltemaschine'],
  // Die Reiter des Gebaeudeeditors kommen aus der Konditionierungsprobe: deren Satz ist
  // ohne Datenbank und traegt neutrale Namen ("Büro OG", "Außenwand EG"). Aufgenommen
  // wird die Ueberlagerung in einem hohen Fenster (2600 px), damit Matrix, Kalenderkarten und Ferienfelder ganz darin stehen.
  ['konditionierung', 'Konditionierung', { seite: '/konditionierungsprobe?fall=vorlagen', reiter: 'Konditionierung', hoehe: 2600 }],
  ['zonen', 'Zonen', { seite: '/konditionierungsprobe?fall=projekt', reiter: 'Zonen', hoehe: 1000 }],
  ['zonendialog', 'Zonendialog', { seite: '/konditionierungsprobe?fall=projekt', reiter: 'Zonen', klick: 'Öffnen…', hoehe: 3000 }],
];

// Bei diesen Bildern steht der Bereich "Gewaehlter Satz" aufgeklappt - die Wiki-Seite
// beschreibt seine Felder.
const SATZ_OFFEN = new Set(['heizkessel', 'bhkw', 'solarkollektoren', 'brauchwasser', 'prozesswaerme']);

if (!ZIEL) {
  console.error('Aufruf: node wikibilder.mjs --ziel <ordner> [--url <adresse>] [--nur <name>]');
  process.exit(2);
}
await mkdir(ZIEL, { recursive: true });

let browser;
let fehler = 0;
try {
  browser = await chromium.launch();
  for (const [name, maske, fremd] of BILDER) {
    if (NUR && name !== NUR) continue;
    // Erst im ueblichen Fenster; ist das Bild zu gross, im kleineren noch einmal.
    let geschrieben = false;
    const fenster = fremd ? [[1280, fremd.hoehe], [1100, fremd.hoehe]] : [[1280, 800], [1100, 720]];
    for (const [w, h] of fenster) {
      const kontext = await browser.newContext({ viewport: { width: w, height: h }, deviceScaleFactor: 1, locale: 'de-DE' });
      const seite = await kontext.newPage();
      const adresse = fremd ? `${WURZEL}${fremd.seite}&kultur=de-DE` : `${WURZEL}/wikibild?bild=${name}&kultur=de-DE`;
      await seite.goto(adresse, { waitUntil: 'networkidle' });
      const dialog = seite.locator(fremd ? '.epos-ueberlagerung' : '.epos-wikibild .epos-dialog').first();
      await dialog.waitFor({ state: 'visible', timeout: 15000 });
      if (fremd) {
        await seite.waitForTimeout(800);
        await dialog.locator('[role=tab]', { hasText: new RegExp('^' + fremd.reiter + '$') }).first().click();
        if (fremd.klick) {
          await seite.waitForTimeout(500);
          await dialog.locator('button', { hasText: fremd.klick }).first().click();
        }
      }
      if (SATZ_OFFEN.has(name)) {
        const satzzeile = dialog.locator('.epos-zweispalten-satzzeile').first();
        if (await satzzeile.count()) await satzzeile.click();
      }
      // Listen und Raster zeichnen nach; kurz ruhen lassen.
      await seite.waitForTimeout(1200);
      const datei = path.join(ZIEL, `Bildschirmfoto_${maske}.png`);
      await dialog.screenshot({ path: datei, animations: 'disabled' });
      const kasten = await dialog.boundingBox();
      const groesse = (await stat(datei)).size;
      await kontext.close();
      const ok = Math.round(kasten.width) <= BREITE_MAX && groesse <= GROESSE_MAX;
      console.log(`${ok ? 'OK ' : '-- '} ${path.basename(datei)}  ${Math.round(kasten.width)}x${Math.round(kasten.height)} px  ${(groesse / 1024).toFixed(0)} KB  (Fenster ${w}x${h})`);
      if (ok) { geschrieben = true; break; }
    }
    if (!geschrieben) fehler++;
  }
} catch (e) {
  console.error('Fehler: ' + e.message);
  process.exit(2);
} finally {
  if (browser) await browser.close();
}
process.exit(fehler ? 1 : 0);
