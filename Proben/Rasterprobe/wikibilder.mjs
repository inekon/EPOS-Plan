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
  // Die zweite Reihe (Beispieldaten in Wirt/Seiten/Wikibild.Weitere.cs). Hohe Dialoge in einem hohen Fenster;
  // der Betriebskalender steht als eigener Dialog, wie ihn der Knopf "Betriebskalender…" oeffnet.
  ['klimadaten', 'Klimadaten'],
  ['kaeltebedarf', 'Kaeltebedarf'],
  ['betriebskalender', 'Betriebskalender', { seite: '/wikibild?bild=betriebskalender', wahl: '.epos-wikibild .epos-dialog', hoehe: 1300 }],
  ['waermebedarf', 'Waermebedarf'],
  ['strombedarf', 'Strombedarf', { seite: '/wikibild?bild=strombedarf', wahl: '.epos-wikibild .epos-dialog', zeile: ['.epos-projektzeile', 'Messung Bürogebäude'], breite: 1400, hoehe: 800 }],
  ['zapfprofil', 'Zapfprofil', { seite: '/wikibild?bild=zapfprofil', wahl: '.epos-wikibild .epos-dialog', hoehe: 1500 }],
  ['baustoffe', 'Baustoffe'],
  // Die uebrigen Bilder (Beispieldaten in Wirt/Seiten/Wikibild.Uebrige.cs).
  ['einstellungen', 'Einstellungen', { seite: '/wikibild?bild=einstellungen', wahl: '.epos-wikibild .epos-dialog', reiter: 'Datenbank', hoehe: 900 }],
  ['lizenz', 'Lizenz', { seite: '/wikibild?bild=lizenz', wahl: '.epos-wikibild .epos-lizenz', hoehe: 1200 }],
  ['projektverwaltung', 'Projektverwaltung', { seite: '/wikibild?bild=projektverwaltung', wahl: '.epos-wikibild .epos-dialog', hoehe: 800 }],
  ['varianten', 'Varianten', { seite: '/wikibild?bild=varianten', wahl: '.epos-wikibild .epos-dialog', hoehe: 800 }],
  ['projekttransfer', 'Projekttransfer', { seite: '/wikibild?bild=projekttransfer', wahl: '.epos-wikibild .epos-dialog', hoehe: 900 }],
  ['erdreich', 'Erdreich', { seite: '/wikibild?bild=erdreich', wahl: '.epos-wikibild .epos-dialog', hoehe: 1600 }],
  ['simulation', 'Simulation', { seite: '/wikibild?bild=simulation', wahl: '.epos-wikibild .epos-dialog', hoehe: 1600 }],
];

// Bei diesen Bildern steht der Bereich "Gewaehlter Satz" aufgeklappt - die Wiki-Seite
// beschreibt seine Felder.
const BREIT = new Set(['brauchwasser', 'prozesswaerme', 'waermepumpe', 'stromspeicher', 'kaeltebedarf', 'waermebedarf', 'strombedarf']);
const SATZ_OFFEN = new Set(['heizkessel', 'bhkw', 'solarkollektoren', 'brauchwasser', 'prozesswaerme', 'kaeltebedarf', 'waermebedarf']);

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
    // Brauchwasser, Prozesswaerme, Waermepumpe, Stromspeicher: die Kopfzeile der Katalogliste (Titel, Suchfeld, Zaehler,
    // Spalten..., Vergleichen, Uebernehmen) steht erst ab rund 1380 px ungekuerzt; die Seite
    // /wikibild laesst den Dialog dafuer breiter werden (Bildbreite bleibt <= 1400 px).
    const fenster = fremd ? [[fremd.breite ?? 1280, fremd.hoehe], [1100, fremd.hoehe]]
      : BREIT.has(name) ? [[1400, 800]] : [[1280, 800], [1100, 720]];
    for (const [w, h] of fenster) {
      const kontext = await browser.newContext({ viewport: { width: w, height: h }, deviceScaleFactor: 1, locale: 'de-DE' });
      const seite = await kontext.newPage();
      const adresse = fremd ? `${WURZEL}${fremd.seite}&kultur=de-DE` : `${WURZEL}/wikibild?bild=${name}&kultur=de-DE`;
      await seite.goto(adresse, { waitUntil: 'networkidle' });
      let dialog = seite.locator(fremd ? (fremd.wahl ?? '.epos-ueberlagerung') : '.epos-wikibild .epos-dialog').first();
      await dialog.waitFor({ state: 'visible', timeout: 15000 });
      if (fremd) {
        await seite.waitForTimeout(800);
        if (fremd.reiter) await dialog.locator('[role=tab]', { hasText: new RegExp('^' + fremd.reiter + '$') }).first().click();
        if (fremd.zeile) {
          // Eine Zeile der Liste waehlen (Text der Zeile), damit Satz und Bild dazu stehen.
          await seite.waitForTimeout(500);
          await dialog.locator(fremd.zeile[0], { hasText: fremd.zeile[1] }).first().click();
        }
        if (fremd.klick) {
          await seite.waitForTimeout(500);
          await dialog.locator('button', { hasText: fremd.klick }).first().click();
        }
        if (fremd.bild) {
          // Aufgenommen wird, was der Klick geoeffnet hat (z. B. das Blatt ueber dem Dialog).
          dialog = seite.locator(fremd.bild).last();
          await dialog.waitFor({ state: 'visible', timeout: 15000 });
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
