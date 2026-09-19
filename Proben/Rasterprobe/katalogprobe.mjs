// =====================================================================
//  KATALOGPROBE (KL-5) — der GANZE Katalogdialog im echten Browser
// =====================================================================
//
//  WOZU. Die Rasterprobe daneben misst EINE Liste. Der Anwenderbefund von
//  KL-5 ist aber kein Listenfehler, sondern ein RAHMENfehler: Im Dialog
//  "Klimadaten" (1 180 x 780, nach einem Regionalimport mit 35 Regionen)
//  malen die Reiterleiste und der Diagrammkasten UEBER die Listenzeilen, der
//  Eingabeblock ueber die Fussleiste - "Loeschen" ist nur noch als "…oeschen"
//  zu lesen, "Beenden" halb verdeckt, "Daten einlesen" steht vor allem
//  anderen. Derselbe Befund kam aus der "Stromverbraucher Verwaltung".
//
//  Eine Ueberlagerung ist eine Aussage ueber KAESTEN, nicht ueber Markup:
//  bunit hat kein Layout und sieht sie grundsaetzlich nicht. Diese Probe
//  misst deshalb im Chromium die Rechtecke von
//    (1) Liste        .epos-katalog-liste
//    (2) Eingabe      .epos-katalog-eingabe
//    (3) Reiterleiste .epos-reiter-leiste      (nur wo es Reiter gibt)
//    (4) Diagramm     img.epos-chartbild       (nur wo es Bilder gibt)
//    (5) Fussleiste   die LETZTE .epos-leiste unter dem Rahmen
//    (6) jeden Knopf der Fussleiste
//  und meldet jede Ueberschneidung als VERSTOSS. Dazu prueft sie, dass jeder
//  Knopf der Fussleiste vollstaendig im Fenster liegt und von keinem anderen
//  Element ueberdeckt wird (elementFromPoint auf den vier Ecken und der
//  Mitte) - genau das, was der Anwender im Bild sieht.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node katalogprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/fotos
//
//  Rueckgabe 0 = kein Verstoss, 1 = mindestens einer, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

// Playwright liegt in dieser Umgebung GLOBAL (npm -g) - derselbe Weg wie in
// rasterprobe.mjs.
const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/katalogprobe.cjs')('playwright');
})();

// ---------------------------------------------------------------- Aufruf
const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');

// --vorher nimmt der Behebung ihre Wirkung wieder weg: Es setzt die
// Katalogpaar-Reihen auf das Mass VOR KL-5 zurueck (eine Flexzelle, die unter
// ihren Inhalt schrumpfen darf, und Rasterreihen ohne Mindestmass). Der Lauf
// MUSS dann rot sein - sonst belegt die Behebung nichts.
const VORHER = process.argv.includes('--vorher');

// Der Stand VOR KL-5, Zeile fuer Zeile: Der Rahmen durfte unter seinen Inhalt
// schrumpfen (flex 1 1 auto, min-height 0), hatte KEINEN eigenen Rollbalken
// (overflow: visible), und seine zwei Kinder trugen min-height: 0 - womit die
// Mindestgroesse einer Rasterreihe null war.
const VORHER_STIL = `
  .epos-katalog-paar.epos-katalog-fuellend {
    flex: 1 1 auto !important; min-height: 0 !important; overflow: visible !important;
  }
  .epos-katalog-paar > .epos-katalog-liste,
  .epos-katalog-paar > .epos-katalog-eingabe { min-height: 0 !important; }
`;

// --------------------------------------------------- Messung im Browser
// Alle Kaesten in Fensterkoordinaten. Rechtecke, die keine Flaeche haben
// (verborgene Reiterblaetter, leere Leisten), zaehlen nicht mit: Ein
// display:none-Element hat 0 x 0 und ueberschnitte sonst scheinbar alles.
const KAESTEN = () => {
  // DER GEMESSENE KASTEN IST DER SICHTBARE, nicht der gerechnete.
  // getBoundingClientRect gibt die Lage im Layout - auch dann, wenn ein
  // Vorfahr mit overflow: auto den Ueberstand laengst abschneidet und
  // wegrollt. Wer damit auf Ueberlagerung prueft, meldet jeden gerollten
  // Dialog als Verstoss. Deshalb wird jeder Kasten mit den Kaesten aller
  // rollenden Vorfahren geschnitten: Was dabei uebrig bleibt, ist das, was
  // der Anwender wirklich sieht - und nur darueber ist eine Ueberlagerung
  // eine Aussage. Beide Zahlen stehen im Protokoll (mass = Layout,
  // sicht = sichtbar).
  const schneide = (a, b) => ({
    links: Math.max(a.links, b.links), oben: Math.max(a.oben, b.oben),
    rechts: Math.min(a.rechts, b.rechts), unten: Math.min(a.unten, b.unten)
  });

  const roh = el => {
    const r = el.getBoundingClientRect();
    return {
      links: +r.left.toFixed(1), oben: +r.top.toFixed(1),
      rechts: +r.right.toFixed(1), unten: +r.bottom.toFixed(1),
      breite: +r.width.toFixed(1), hoehe: +r.height.toFixed(1)
    };
  };

  const mass = el => {
    const m = roh(el);
    let s = { links: m.links, oben: m.oben, rechts: m.rechts, unten: m.unten };
    for (let v = el.parentElement; v && v !== document.documentElement; v = v.parentElement) {
      const st = getComputedStyle(v);
      if (st.overflowX === 'visible' && st.overflowY === 'visible') continue;
      s = schneide(s, roh(v));
    }
    m.sicht = {
      links: +s.links.toFixed(1), oben: +s.oben.toFixed(1),
      rechts: +s.rechts.toFixed(1), unten: +s.unten.toFixed(1),
      breite: +Math.max(0, s.rechts - s.links).toFixed(1),
      hoehe: +Math.max(0, s.unten - s.oben).toFixed(1)
    };
    return m;
  };
  const da = el => el && el.getBoundingClientRect().width > 0.5 &&
                   el.getBoundingClientRect().height > 0.5;

  const wurzel = document.querySelector('.epos-katalog-dialog');
  if (!wurzel) return { fehler: 'kein .epos-katalog-dialog im Baum' };

  const liste = wurzel.querySelector('.epos-katalog-liste');
  const eingabe = wurzel.querySelector('.epos-katalog-eingabe');
  const rahmen = wurzel.querySelector('.epos-katalog-paar');
  const huelle = wurzel.querySelector('.epos-katalog-liste .epos-raster-huelle');
  const reiterleiste = eingabe ? eingabe.querySelector('.epos-reiter-leiste') : null;
  const bild = eingabe ? [...eingabe.querySelectorAll('img.epos-chartbild')].find(da) : null;

  // DIE FUSSLEISTE ist die letzte .epos-leiste, die NICHT im Rahmen steht -
  // im Klimadialog traegt die Liste selbst eine Leiste ("Loeschen").
  const leisten = [...wurzel.querySelectorAll('.epos-leiste')]
    .filter(e => !rahmen || !rahmen.contains(e));
  const fuss = leisten.length ? leisten[leisten.length - 1] : null;

  const knopf = b => ({
    text: b.textContent.trim().replace(/\s+/g, ' ').slice(0, 40),
    ...mass(b),
    // Wer liegt an den fuenf Punkten dieses Knopfes ZUOBERST? Ist es nicht der
    // Knopf selbst (oder ein Kind), verdeckt ihn etwas.
    verdeckt: (() => {
      const r = b.getBoundingClientRect();
      const punkte = [
        [r.left + 2, r.top + 2], [r.right - 2, r.top + 2],
        [r.left + 2, r.bottom - 2], [r.right - 2, r.bottom - 2],
        [(r.left + r.right) / 2, (r.top + r.bottom) / 2]
      ];
      const fremd = [];
      for (const [x, y] of punkte) {
        if (x < 0 || y < 0 || x > innerWidth || y > innerHeight) { fremd.push('ausserhalb'); continue; }
        const oben = document.elementFromPoint(x, y);
        if (!oben) { fremd.push('nichts'); continue; }
        if (oben === b || b.contains(oben)) continue;
        fremd.push(oben.tagName.toLowerCase() +
          (oben.className ? '.' + String(oben.className).trim().split(/\s+/).slice(0, 2).join('.') : ''));
      }
      return fremd;
    })()
  });

  return {
    fenster: { breite: innerWidth, hoehe: innerHeight },
    dialog: { ...mass(wurzel), rollHoehe: wurzel.scrollHeight, overflowY: getComputedStyle(wurzel).overflowY },
    rahmen: rahmen ? { ...mass(rahmen), rollHoehe: rahmen.scrollHeight,
                       reihen: getComputedStyle(rahmen).gridTemplateRows } : null,
    liste: liste ? { ...mass(liste), rollHoehe: liste.scrollHeight } : null,
    eingabe: eingabe ? { ...mass(eingabe), rollHoehe: eingabe.scrollHeight } : null,
    huelle: huelle ? { ...mass(huelle), maxHoehe: getComputedStyle(huelle).maxHeight,
                       rollHoehe: huelle.scrollHeight } : null,
    reiterleiste: da(reiterleiste) ? mass(reiterleiste) : null,
    bild: bild ? mass(bild) : null,
    fuss: da(fuss) ? mass(fuss) : null,
    fussknoepfe: fuss ? [...fuss.querySelectorAll('button')].filter(da).map(knopf) : [],
    zeilen: wurzel.querySelectorAll('.epos-katalog-liste table tbody tr').length
  };
};

// ---------------------------------------------------------------- Helfer
const schlaf = ms => new Promise(r => setTimeout(r, ms));

// Ueberschneidung zweier Rechtecke in Quadratpixeln. Eine gemeinsame KANTE
// (der Eingabeblock beginnt genau dort, wo die Liste endet) ist keine
// Ueberlagerung, deshalb die Toleranz von einem halben Pixel.
function ueberschneidung(a, b, toleranz = 0.5) {
  if (!a || !b) return 0;
  const x = a.sicht || a, y = b.sicht || b;
  if (x.breite <= 0 || x.hoehe <= 0 || y.breite <= 0 || y.hoehe <= 0) return 0;
  const breite = Math.min(x.rechts, y.rechts) - Math.max(x.links, y.links) - toleranz;
  const hoehe = Math.min(x.unten, y.unten) - Math.max(x.oben, y.oben) - toleranz;
  if (breite <= 0 || hoehe <= 0) return 0;
  return +(breite * hoehe).toFixed(0);
}

// ------------------------------------------------------------ Ein Fall
async function fall(browser, f) {
  const kontext = await browser.newContext({
    viewport: { width: f.breite, height: f.hoehe },
    deviceScaleFactor: 1
  });
  const seite = await kontext.newPage();

  const adresse = `${WURZEL}/katalogprobe?maske=${f.maske}&zeilen=${f.zeilen}` +
                  (f.bilder === false ? '&bilder=0' : '');
  await seite.goto(adresse, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector('.epos-katalog-dialog', { timeout: 30000 });

  if (f.vorher || VORHER) await seite.addStyleTag({ content: VORHER_STIL });

  // STILPROBE wie in der Rasterprobe: Ohne epos-ui.css gibt es keine
  // Hoechsthoehe, keine Rollbehaelter und damit auch keine Ueberlagerung -
  // der Lauf saehe gruen aus und haette nichts gemessen.
  const stil = await seite.evaluate(() => {
    const h = document.querySelector('.epos-raster-huelle');
    return h ? getComputedStyle(h).maxHeight : '(keine Huelle)';
  });
  if (stil === 'none' || stil === '(keine Huelle)') {
    throw new Error(`Das Stilblatt epos-ui.css wirkt nicht (max-height der Huelle: ${stil}). ` +
      'Der Wirt liefert seine statischen Dateien nicht aus - Messung wertlos.');
  }

  // AUF DIE ZEILEN WARTEN, nicht auf das Markup: Der erste Treffer von
  // .epos-katalog-dialog kommt schon aus dem VORzeichnen des Servers - da ist
  // die Liste noch leer, weil die Dialoge ihre Zeilen erst nach dem ersten
  // Zeichenlauf holen (OnAfterRenderAsync). Ohne dieses Warten misst die Probe
  // den leeren Dialog, und der ueberlagert nie etwas.
  await seite.waitForSelector('.epos-katalog-liste tbody tr', { timeout: 30000 });

  // Die erste Zeile waehlen: Erst dann holt der Dialog seine Ansicht und
  // befuellt die Diagramme - genau der Zustand des Anwenderbildes. Ueber einen
  // Locator und nicht ueber ein Element: Das QuickGrid baut seine Zeilen nach
  // dem ersten Zeichenlauf noch einmal auf, und ein festgehaltenes Element
  // haengt danach nicht mehr im Baum ("Element is not attached to the DOM").
  await schlaf(400);
  await seite.locator('.epos-katalog-liste tbody tr button').first()
             .click({ timeout: 15000 });
  await schlaf(1400);

  const k = await seite.evaluate(KAESTEN);

  if (FOTOS) {
    const marke = (f.vorher || VORHER) ? 'vorher' : 'nachher';
    await seite.screenshot({ path: `${FOTOS}/${f.name}_${marke}.png`, fullPage: false });
  }

  await kontext.close();
  return { name: f.name, fall: f, adresse, ...k };
}

// ------------------------------------------------------- Die Sollwerte
function pruefe(e) {
  const m = [];
  if (e.fehler) return [e.fehler];

  const paare = [
    ['Eingabeblock über Liste', e.eingabe, e.liste],
    ['Reiterleiste über Liste', e.reiterleiste, e.liste],
    ['Diagramm über Liste', e.bild, e.liste],
    // Die LISTENHUELLE gegen den Eingabeblock: Sie ist der zweite Weg desselben
    // Befundes. Wird die erste Rasterreihe unter ihren Inhalt gestaucht, bleibt
    // die Huelle in ihrer Hoechsthoehe stehen und ragt aus dem Listenblock
    // heraus - gemessen 457,6 px Huelle in einem 310 px hohen Block, also
    // 255 px quer ueber die Felder darunter (Fall C1 mit --vorher).
    ['Listenhuelle über Eingabeblock', e.huelle, e.eingabe],
    ['Listenhuelle über Fussleiste', e.huelle, e.fuss],
    ['Eingabeblock über Fussleiste', e.eingabe, e.fuss],
    ['Diagramm über Fussleiste', e.bild, e.fuss],
    ['Liste über Fussleiste', e.liste, e.fuss]
  ];
  for (const [was, a, b] of paare) {
    const flaeche = ueberschneidung(a, b);
    if (flaeche > 0) m.push(`${was}: ${flaeche} px² Überschneidung`);
  }

  // Die Fussleiste steht UNTER dem Rahmen und wird nie von ihm berührt.
  if (e.rahmen && e.fuss && e.fuss.oben < e.rahmen.sicht.unten - 0.5)
    m.push(`Fussleiste beginnt bei y=${e.fuss.oben}, der Rahmen endet erst bei ${e.rahmen.sicht.unten}`);

  // Jeder Knopf der Fussleiste GANZ sichtbar: im Fenster, ungeschnitten und
  // von nichts überdeckt. Das ist der Abnahmepunkt A-KL5-1 in Zahlen.
  for (const b of e.fussknoepfe) {
    if (b.verdeckt.length) m.push(`Knopf „${b.text}" verdeckt durch ${[...new Set(b.verdeckt)].join(', ')}`);
    if (b.unten > e.fenster.hoehe + 0.5 || b.rechts > e.fenster.breite + 0.5 || b.oben < -0.5)
      m.push(`Knopf „${b.text}" ragt aus dem Fenster (${b.links},${b.oben})-(${b.rechts},${b.unten})`);
    if (b.sicht.hoehe < b.hoehe - 0.5 || b.sicht.breite < b.breite - 0.5)
      m.push(`Knopf „${b.text}" ist beschnitten (${b.sicht.breite}x${b.sicht.hoehe} von ${b.breite}x${b.hoehe})`);
  }

  return m;
}

function zeige(e) {
  console.log('');
  console.log('=== ' + e.name + ' ===  ' + e.adresse);
  if (e.fehler) { console.log('  FEHLER: ' + e.fehler); return; }
  const z = k => {
    if (!k) return '—';
    const roh = `y ${k.oben} … ${k.unten}  (h ${k.hoehe}, x ${k.links} … ${k.rechts})`;
    if (!k.sicht) return roh;
    const gleich = Math.abs(k.sicht.oben - k.oben) < 0.6 && Math.abs(k.sicht.unten - k.unten) < 0.6;
    return gleich ? roh : `${roh}  sichtbar y ${k.sicht.oben} … ${k.sicht.unten} (h ${k.sicht.hoehe})`;
  };
  console.log(`  Fenster ${e.fenster.breite}x${e.fenster.hoehe}   Zeilen im Raster: ${e.zeilen}`);
  console.log(`  Dialog        ${z(e.dialog)}  rollHoehe ${e.dialog.rollHoehe}  overflow-y ${e.dialog.overflowY}`);
  console.log(`  Rahmen        ${z(e.rahmen)}  rollHoehe ${e.rahmen ? e.rahmen.rollHoehe : '—'}  Reihen ${e.rahmen ? e.rahmen.reihen : '—'}`);
  console.log(`  Liste         ${z(e.liste)}  rollHoehe ${e.liste ? e.liste.rollHoehe : '—'}`);
  console.log(`  Listenhuelle  ${z(e.huelle)}  max-height ${e.huelle ? e.huelle.maxHoehe : '—'}`);
  console.log(`  Eingabe       ${z(e.eingabe)}  rollHoehe ${e.eingabe ? e.eingabe.rollHoehe : '—'}`);
  console.log(`  Reiterleiste  ${z(e.reiterleiste)}`);
  console.log(`  Diagramm      ${z(e.bild)}`);
  console.log(`  Fussleiste    ${z(e.fuss)}`);
  for (const b of e.fussknoepfe)
    console.log(`     Knopf „${b.text}"  ${z(b)}  ${b.verdeckt.length ? 'VERDECKT durch ' + b.verdeckt.join(', ') : 'frei'}`);
}

// ------------------------------------------------------------- Hauptlauf
// Die drei gemessenen Masken je bei 1 180 x 780 (das Fenstermass der
// Klimadaten-Huelle) und bei 1 600 x 1 000 (vom Anwender aufgezogen), jeweils
// mit 35 Zeilen - dem Stand nach einem TRY-Regionalimport. Dazu die vier
// uebrigen Katalograhmen-Masken einmal, und der Klimadialog VOR dem Import
// (Platzhalter statt Bildern), wo das Bild in Ordnung war.
const FAELLE = [
  { name: 'K1_klima_1180x780',        maske: 'klima',        zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'K2_klima_1600x1000',       maske: 'klima',        zeilen: 35, breite: 1600, hoehe: 1000 },
  { name: 'K3_klima_ohne_Bilder',     maske: 'klima',        zeilen: 35, breite: 1180, hoehe: 780, bilder: false },
  { name: 'B1_bedarf_1180x780',       maske: 'bedarf',       zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'B2_bedarf_1600x1000',      maske: 'bedarf',       zeilen: 35, breite: 1600, hoehe: 1000 },
  { name: 'M1_modul_1180x780',        maske: 'modul',        zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'M2_modul_1600x1000',       maske: 'modul',        zeilen: 35, breite: 1600, hoehe: 1000 },
  { name: 'W1_waermebedarf_1180x780', maske: 'waermebedarf', zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'S1_solar_1180x780',        maske: 'solar',        zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'C1_browser_1180x780',      maske: 'browser',      zeilen: 35, breite: 1180, hoehe: 780 },
  { name: 'P1_waermepumpe_1180x780',  maske: 'waermepumpe',  zeilen: 35, breite: 1180, hoehe: 780 },
  // DIE GEGENPROBE: derselbe Klimadialog mit dem Mass VOR KL-5. Sie MUSS den
  // Befund zeigen - sonst belegt die Behebung nichts.
  { name: 'G1_klima_vor_KL5',         maske: 'klima',        zeilen: 35, breite: 1180, hoehe: 780,
    vorher: true, mussFehlschlagen: true }
];

if (FOTOS) await mkdir(FOTOS, { recursive: true });

const browser = await chromium.launch({ headless: true });
let schlecht = 0;
let gezaehlt = 0;

try {
  for (const f of FAELLE) {
    if (NUR && !f.name.startsWith(NUR)) continue;
    const e = await fall(browser, f);
    gezaehlt++;
    zeige(e);
    const m = pruefe(e);

    if (f.mussFehlschlagen && !VORHER) {
      if (m.length) console.log('  Gegenprobe zeigt den Befund wie erwartet: ' + m.join('; '));
      else { schlecht++; console.log('  GEGENPROBE OHNE WIRKUNG: auch mit dem Mass von vor KL-5 ' +
                                     'ueberlagert nichts - dann belegt die Behebung nichts.'); }
      continue;
    }

    if (m.length) { schlecht++; console.log('  VERSTOSS: ' + m.join('; ')); }
    else console.log('  ohne Ueberlagerung');
  }
} catch (fehler) {
  console.log('');
  console.log('ABBRUCH: ' + fehler.message);
  await browser.close();
  process.exit(2);
}

await browser.close();

console.log('');
console.log(schlecht === 0
  ? `ALLE ${gezaehlt} FAELLE OHNE UEBERLAGERUNG.`
  : `${schlecht} von ${gezaehlt} FAELLEN MIT UEBERLAGERUNG.`);

process.exit(schlecht === 0 ? 0 : 1);
