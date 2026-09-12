// =====================================================================
//  RASTERPROBE (#235) — die virtualisierte Katalogliste im ECHTEN Browser
// =====================================================================
//
//  WOZU. bunit rendert Komponenten ohne Layout und ohne JavaScript. Genau
//  daran ist der zweite Anlauf gegen das "Blinken" der Auswahlliste (#212)
//  vorbeigegangen: Was Virtualize tut, entscheidet sich in den
//  IntersectionObservern seines JavaScript-Teils und an den PIXELN, die das
//  Stilblatt setzt — beides sieht bunit nicht.
//
//  Diese Probe misst deshalb im Chromium:
//    (a) wie oft die Klasse "loading" am QuickGrid umschaltet (je Sekunde),
//    (b) Platzhalter- und Echtzeilen zu t = 0,5 / 1 / 2 / 5 s,
//    (c) die gemessene Hoehe einer tr gegen ItemSize (53),
//    (d) die Hoehen der zwei Virtualize-Abstandshalter und die Rollhoehe,
//    (e) welches Element Virtualize als Rollbehaelter findet,
//    (f) Bildschirmfotos zu t = 0,5 und 5 s,
//    (g) dasselbe nach einem Rollen um 2 000 px und zurueck,
//    (h) dasselbe unter der Virtualisierungsschwelle (119 Zeilen),
//    (i) wie oft die zwei Sichtbarkeitsmelder von Virtualize zurueckmelden.
//
//  (i) ist der schaerfste Wert. Streiten sich die beiden - weil ItemSize nicht
//  zur wirklichen Zeilenhoehe passt -, melden sie im Takt des Bildaufbaus
//  (gemessen 370 Mal in drei Sekunden statt vier).
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node rasterprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/fotos
//
//  Rueckgabe 0 = alle Sollwerte erfuellt, 1 = mindestens einer verfehlt,
//  2 = Aufruf- oder Verbindungsfehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

// Playwright liegt in dieser Umgebung GLOBAL (npm -g), und ein ES-Modul sucht
// dort nicht von selbst — NODE_PATH gilt nur fuer CommonJS. Also erst der
// gewoehnliche Weg, dann der globale Wurzelordner.
const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/rasterprobe.cjs')('playwright');
})();

// ---------------------------------------------------------------- Aufruf
const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
// --entpinnt nimmt ALLEN Faellen das gesetzte Zeilenmass wieder weg: der Stand
// VOR dem Fix, aus demselben Programm gemessen. Der Lauf muss dann rot sein.
const ENTPINNT = process.argv.includes('--entpinnt');

// ------------------------------------------------------- Die Messsonde
// Sie wird VOR jedem Skript der Seite eingespielt, damit kein Umschalten
// der Klasse "loading" verlorengeht — auch nicht das allererste.
const SONDE = () => {
  const p = {
    t0: performance.now(),
    umschaltungen: [],   // { t, an }
    proben: [],          // { t, platzhalter, echt, laden, vor, nach }
    melder: []           // { t, halter, schneidet } — die Sichtbarkeitsmelder
  };
  window.__probe = p;

  // Die SICHTBARKEITSMELDER von Virtualize mitschreiben. Sie sind der direkte
  // Blick auf den Fehler: Im kranken Zustand melden sie im Takt des
  // Bildaufbaus (gemessen alle 16 ms), im gesunden zwei- bis viermal und dann
  // nie wieder.
  const Echt = window.IntersectionObserver;
  function Mit(rueckruf, gaben) {
    const huelle = (eintraege, wer) => {
      for (const e of eintraege) p.melder.push({
        t: +(performance.now() - p.t0).toFixed(0),
        halter: (e.target.getAttribute('style') || '').slice(8, 24),
        schneidet: e.isIntersecting
      });
      return rueckruf(eintraege, wer);
    };
    return new Echt(huelle, gaben);
  }
  Mit.prototype = Echt.prototype;
  window.IntersectionObserver = Mit;

  // Beobachtet wird DOCUMENT und nicht documentElement: Zum Zeitpunkt des
  // Einspielens (document_start) ist documentElement noch null, und ein
  // observe(null) wuerde die ganze Sonde mitreissen — die Probe meldete dann
  // stillschweigend "0 Umschaltungen".
  const beobachter = new MutationObserver(muts => {
    for (const m of muts) {
      if (m.attributeName !== 'class') continue;
      const ziel = m.target;
      if (!(ziel instanceof Element)) continue;
      const alt = m.oldValue || '';
      if (!ziel.classList.contains('quickgrid') && !alt.includes('quickgrid')) continue;
      const vorher = alt.split(/\s+/).includes('loading');
      const nachher = ziel.classList.contains('loading');
      if (vorher !== nachher) p.umschaltungen.push({ t: +(performance.now() - p.t0).toFixed(0), an: nachher });
    }
  });
  beobachter.observe(document, {
    subtree: true, attributes: true, attributeFilter: ['class'], attributeOldValue: true
  });

  setInterval(() => {
    const t = document.querySelector('table.quickgrid');
    const tb = t && t.querySelector('tbody');
    if (!tb) { p.proben.push({ t: +(performance.now() - p.t0).toFixed(0), platzhalter: 0, echt: 0, laden: false }); return; }

    // Die zwei Abstandshalter sind <div style="...flex-shrink: 0; display: table-row">;
    // alles andere im tbody ist eine Zeile.
    const kinder = [...tb.children];
    const halter = kinder.filter(e => /flex-shrink/.test(e.getAttribute('style') || ''));
    const zeilen = kinder.filter(e => !halter.includes(e));
    p.proben.push({
      t: +(performance.now() - p.t0).toFixed(0),
      platzhalter: zeilen.filter(z => z.querySelector('td.grid-cell-placeholder')).length,
      echt: zeilen.filter(z => !z.querySelector('td.grid-cell-placeholder')).length,
      laden: t.classList.contains('loading'),
      vor: halter.length ? halter[0].offsetHeight : -1,
      nach: halter.length > 1 ? halter[1].offsetHeight : -1
    });
  }, 50);
};

// --------------------------------------------------- Messung im Browser
// Alles, was sich nur AM ELEMENT ablesen laesst: Zeilenmass, Abstandshalter,
// Rollbehaelter. findeRollbehaelter bildet Virtualize.ts nach
// (findClosestScrollContainer): der naechste Vorfahr, dessen overflow-y
// nicht 'visible' ist; body und html zaehlen nicht mit.
const ABLESEN = () => {
  const tabelle = document.querySelector('table.quickgrid');
  if (!tabelle) return { fehler: 'kein QuickGrid im Baum' };

  const tbody = tabelle.querySelector('tbody');
  const alle = [...tbody.children];
  const abstand = alle.filter(e => /flex-shrink/.test(e.getAttribute('style') || ''));
  const zeilen = alle.filter(e => !abstand.includes(e));
  const platzhalterZeilen = zeilen.filter(z => z.querySelector('td.grid-cell-placeholder'));
  const echteZeilen = zeilen.filter(z => !z.querySelector('td.grid-cell-placeholder'));

  const findeRollbehaelter = el => {
    while (el && el !== document.body && el !== document.documentElement) {
      if (getComputedStyle(el).overflowY !== 'visible') return el;
      el = el.parentElement;
    }
    return null;
  };

  const kennung = el => {
    if (!el) return '(Fenster)';
    return el.tagName.toLowerCase() +
      (el.id ? '#' + el.id : '') +
      (el.className ? '.' + String(el.className).trim().split(/\s+/).join('.') : '');
  };

  const behaelter = abstand.length ? findeRollbehaelter(abstand[0]) : findeRollbehaelter(tabelle);
  const huelle = tabelle.closest('.epos-raster-huelle, .epos-raster-huelle--hoch');
  const kopf = tabelle.querySelector('thead');

  const mass = el => {
    const r = el.getBoundingClientRect();
    return { hoehe: +r.height.toFixed(3), oben: +r.top.toFixed(1) };
  };

  return {
    theme: tabelle.getAttribute('theme'),
    klassen: tabelle.className,
    laden: tabelle.classList.contains('loading'),
    zeilenGesamt: zeilen.length,
    platzhalter: platzhalterZeilen.length,
    echt: echteZeilen.length,
    zeilenhoehen: zeilen.slice(0, 6).map(z => +z.getBoundingClientRect().height.toFixed(3)),
    echtHoehe: echteZeilen.length ? +echteZeilen[0].getBoundingClientRect().height.toFixed(3) : null,
    platzhalterHoehe: platzhalterZeilen.length
      ? +platzhalterZeilen[0].getBoundingClientRect().height.toFixed(3) : null,
    abstandshalter: abstand.map(e => ({
      tag: e.tagName.toLowerCase(),
      stil: e.getAttribute('style'),
      gemessen: +e.getBoundingClientRect().height.toFixed(3),
      offsetHoehe: e.offsetHeight,
      offsetOben: e.offsetTop,
      display: getComputedStyle(e).display
    })),
    rollbehaelter: kennung(behaelter),
    rollbehaelterHoehe: behaelter ? behaelter.clientHeight : null,
    huelle: huelle ? {
      kennung: kennung(huelle),
      clientHoehe: huelle.clientHeight,
      rollHoehe: huelle.scrollHeight,
      rollStand: huelle.scrollTop,
      overflowY: getComputedStyle(huelle).overflowY,
      hoeheStil: getComputedStyle(huelle).height,
      maxHoehe: getComputedStyle(huelle).maxHeight
    } : null,
    kopf: kopf ? {
      position: getComputedStyle(kopf.querySelector('th') || kopf).position,
      ...mass(kopf)
    } : null,
    tabelleHoehe: +tabelle.getBoundingClientRect().height.toFixed(3)
  };
};

// ---------------------------------------------------------------- Helfer
const schlaf = ms => new Promise(r => setTimeout(r, ms));

function zuZeit(proben, ms) {
  let treffer = null;
  for (const p of proben) { if (p.t <= ms + 25) treffer = p; else break; }
  return treffer || { t: 0, platzhalter: 0, echt: 0, laden: false };
}

function verdichte(proben) {
  const aus = [];
  let letzt = '';
  for (const p of proben) {
    const s = `${p.platzhalter}|${p.echt}|${p.laden ? 'L' : '-'}|${p.vor}|${p.nach}`;
    if (s === letzt) continue;
    letzt = s;
    aus.push(`${p.t}:${s}`);
  }
  return aus.length > 24 ? aus.slice(0, 12).join(' ') + ' … ' + aus.slice(-8).join(' ') : aus.join(' ');
}

function umschaltungenJeSekunde(um, von, bis) {
  const drin = um.filter(u => u.t >= von && u.t < bis);
  return { zahl: drin.length, jeSekunde: +(drin.length / ((bis - von) / 1000)).toFixed(2) };
}

// ------------------------------------------------------------ Ein Fall
async function fall(browser, f) {
  const kontext = await browser.newContext({
    viewport: { width: f.breite, height: f.hoehe },
    deviceScaleFactor: f.dpr
  });
  const seite = await kontext.newPage();
  await seite.addInitScript(SONDE);

  const adresse = `${WURZEL}/probe?modus=${f.modus}&zeilen=${f.zeilen}&takt=${f.takt}`;
  await seite.goto(adresse, { waitUntil: 'domcontentloaded' });

  // Auf den Aufbau der interaktiven Komponente warten (Blazor Server).
  await seite.waitForSelector('table.quickgrid', { timeout: 20000 });

  // DIE GEGENPROBE (#235): Nimmt man der Zeile ihr gesetztes Mass wieder weg,
  // muss der Fehler zurueckkommen. Sonst belegt die Messung nur, dass es heute
  // laeuft - nicht, dass DIESE Zeile im Stilblatt es laufen laesst.
  if (f.entpinnt || ENTPINNT) {
    await seite.addStyleTag({
      content: '.epos-raster-huelle--hoch .epos-raster > tbody > tr,' +
               '.epos-raster-huelle--hoch .epos-raster > tbody > tr > td' +
               '{ height: auto !important; }'
    });
  }

  // STILPROBE. Ohne epos-ui.css hat die Huelle keine Hoechsthoehe, es gibt
  // nichts zu rollen und nichts zu virtualisieren - und die Zaehler stehen
  // dann alle auf 0, was wie ein Erfolg aussieht. Das darf nicht durchgehen.
  const stil = await seite.evaluate(() => {
    const h = document.querySelector('.epos-raster-huelle');
    return h ? getComputedStyle(h).maxHeight : '(keine Huelle)';
  });
  if (stil === 'none' || stil === '(keine Huelle)') {
    throw new Error(`Das Stilblatt epos-ui.css wirkt nicht (max-height der Huelle: ${stil}). ` +
      'Der Wirt liefert seine statischen Dateien nicht aus - Messung wertlos.');
  }

  const fotoschuss = async (marke) => {
    if (!FOTOS) return;
    await seite.screenshot({ path: `${FOTOS}/${f.name}_${marke}.png`, fullPage: false });
  };

  // (j) FILTERN: die zweite Handlung des Anwenders nach dem Abruf. Sie tauscht
  // die Zeilenmenge aus, wechselt den @key des Rasters (Befund W6-B-2) und
  // faehrt die Virtualisierung damit von vorn an.
  if (f.suche) {
    await schlaf(1500);
    await seite.fill('.epos-katalog-suchfeld input', f.suche);
  }

  await schlaf(500);
  const bei500 = await seite.evaluate(ABLESEN);
  await fotoschuss('t0500');

  await schlaf(4500);                       // t = 5 s
  const bei5000 = await seite.evaluate(ABLESEN);
  await fotoschuss('t5000');

  const proben1 = await seite.evaluate(() => window.__probe.proben);
  const um1 = await seite.evaluate(() => window.__probe.umschaltungen);
  const melder1 = await seite.evaluate(() => window.__probe.melder.length);

  // Der Nullpunkt des Beobachtens ist das ENDE des Ladens (bzw. des Filterns):
  // vorher darf und muss "loading" schalten. Im Modus "sofort" ist das t = 0.
  const fertig = f.suche ? 2200 : (f.modus === 'laden' ? 1200 : 0);

  // --- (g) Rollen um 2 000 px und zurueck ---------------------------
  const rollmass = await seite.evaluate(() => {
    const h = document.querySelector('.epos-raster-huelle--hoch, .epos-raster-huelle');
    if (!h) return null;
    h.scrollTop = 2000;
    return { gesetzt: h.scrollTop, rollHoehe: h.scrollHeight };
  });
  const t_roll = Date.now();
  let nachRollen = null;
  if (rollmass) {
    // Sollwert: binnen 300 ms echte Zeilen im sichtbaren Bereich.
    for (let i = 0; i < 12; i++) {
      await schlaf(25);
      const s = await seite.evaluate(() => {
        const t = document.querySelector('table.quickgrid');
        const zeilen = [...t.querySelectorAll('tbody > tr')]
          .filter(z => !/flex-shrink/.test(z.getAttribute('style') || ''));
        return {
          echt: zeilen.filter(z => !z.querySelector('td.grid-cell-placeholder')).length,
          platzhalter: zeilen.filter(z => z.querySelector('td.grid-cell-placeholder')).length
        };
      });
      if (s.echt > 0) { nachRollen = { ...s, ms: Date.now() - t_roll }; break; }
    }
    if (!nachRollen) {
      const s = await seite.evaluate(ABLESEN);
      nachRollen = { echt: s.echt, platzhalter: s.platzhalter, ms: Date.now() - t_roll };
    }
    await schlaf(3000);                     // drei Sekunden Ruhe NACH dem Rollen
    await fotoschuss('gerollt');
  }

  const nachRollenStand = await seite.evaluate(ABLESEN);
  const umRollen = await seite.evaluate(() => window.__probe.umschaltungen.length) - um1.length;
  const melderRollen = await seite.evaluate(() => window.__probe.melder.length) - melder1;

  await seite.evaluate(() => {
    const h = document.querySelector('.epos-raster-huelle--hoch, .epos-raster-huelle');
    if (h) h.scrollTop = 0;
  });
  await schlaf(800);
  const zurueck = await seite.evaluate(ABLESEN);
  await fotoschuss('zurueck');

  const um2 = await seite.evaluate(() => window.__probe.umschaltungen);
  const proben2 = await seite.evaluate(() => window.__probe.proben);

  await kontext.close();

  return {
    name: f.name, fall: f, adresse,
    ruhe: umschaltungenJeSekunde(um1, fertig, 5000),
    umschaltungenGesamt: um2.length,
    t0500: zuZeit(proben1, 500),
    t1000: zuZeit(proben1, 1000),
    t2000: zuZeit(proben1, 2000),
    t5000: zuZeit(proben1, 5000),
    bei500, bei5000,
    rollmass, nachRollen, nachRollenStand, zurueck, umRollen, melder1, melderRollen,
    probenZahl: proben1.length,
    umschaltungen: um2,
    // Der Verlauf, auf ZUSTANDSWECHSEL eingedampft: eine Zeile je Aenderung
    // von Platzhalter-/Zeilenzahl, Ladeflagge oder Abstandshalter.
    verlauf: verdichte(proben1)
  };
}

// -------------------------------------------------------------- Ausgabe
function zeige(e) {
  const b = e.bei5000;
  console.log('');
  console.log('=== ' + e.name + ' ===  ' + e.adresse);
  console.log(`  Fenster ${e.fall.breite}x${e.fall.hoehe} @ dpr ${e.fall.dpr}`);
  if (b.fehler) { console.log('  FEHLER: ' + b.fehler); return; }
  console.log(`  (a) loading-Umschaltungen nach dem Laden (bis 5 s): ${e.ruhe.zahl}  (${e.ruhe.jeSekunde}/s)`);
  console.log(`  (b) Platzhalter/echt  t=0,5s ${e.t0500.platzhalter}/${e.t0500.echt}` +
              `   t=1s ${e.t1000.platzhalter}/${e.t1000.echt}` +
              `   t=2s ${e.t2000.platzhalter}/${e.t2000.echt}` +
              `   t=5s ${e.t5000.platzhalter}/${e.t5000.echt}` +
              `   (${e.probenZahl} Proben)`);
  console.log(`      Verlauf (t|Platzh|echt|laden|HalterVor|HalterNach): ${e.verlauf}`);
  console.log(`  (c) Zeilenhoehen gemessen: ${JSON.stringify(b.zeilenhoehen)}` +
              `  (echt ${b.echtHoehe}, Platzhalter ${b.platzhalterHoehe}, ItemSize 53)`);
  console.log(`  (d) Abstandshalter: ${JSON.stringify(b.abstandshalter)}`);
  console.log(`      Huelle: ${JSON.stringify(b.huelle)}`);
  console.log(`  (e) Rollbehaelter (wie Virtualize.ts ihn sucht): ${b.rollbehaelter}` +
              `  clientHeight ${b.rollbehaelterHoehe}`);
  console.log(`      Kopf: ${JSON.stringify(b.kopf)}   Tabelle ${b.tabelleHoehe} px   theme=${b.theme}`);
  console.log(`  (g) nach Rollen 2000px: ${JSON.stringify(e.nachRollen)}` +
              `  nach 3 s Ruhe ${e.nachRollenStand.platzhalter} Platzhalter / ${e.nachRollenStand.echt} echt` +
              `  (${e.umRollen} loading-Umschaltungen in diesen 3 s)`);
  console.log(`      zurueck: ${e.zurueck.platzhalter} Platzhalter / ${e.zurueck.echt} echt`);
  console.log(`      Umschaltungen gesamt ueber den ganzen Lauf: ${e.umschaltungenGesamt}`);
  console.log(`  (i) Sichtbarkeitsmelder: ${e.melder1} beim Aufbau, ` +
              `${e.melderRollen} in den 3 s nach dem Rollen (soll <= 12)`);
}

// ------------------------------------------------------- Die Sollwerte
function pruefe(e) {
  const maengel = [];
  const virtualisiert = e.fall.zeilen >= 120;
  if (e.ruhe.zahl !== 0) maengel.push(`${e.ruhe.zahl} loading-Umschaltungen nach dem Laden (soll 0)`);
  if (e.t5000.platzhalter !== 0) maengel.push(`${e.t5000.platzhalter} Platzhalter bei t=5 s (soll 0)`);
  if (e.t5000.echt === 0) maengel.push('keine echte Zeile bei t=5 s');
  if (virtualisiert && e.nachRollen && e.nachRollen.echt === 0)
    maengel.push('nach dem Rollen keine echte Zeile');
  if (virtualisiert && e.nachRollen && e.nachRollen.ms > 300)
    maengel.push(`nach dem Rollen erst nach ${e.nachRollen.ms} ms echte Zeilen (soll <= 300)`);
  if (e.nachRollenStand.platzhalter !== 0)
    maengel.push(`${e.nachRollenStand.platzhalter} Platzhalter drei Sekunden nach dem Rollen (soll 0)`);
  if (e.umRollen !== 0)
    maengel.push(`${e.umRollen} loading-Umschaltungen in den drei Sekunden nach dem Rollen (soll 0)`);
  if (e.zurueck.platzhalter !== 0)
    maengel.push(`${e.zurueck.platzhalter} Platzhalter nach dem Zurueckrollen (soll 0)`);
  if (virtualisiert && e.melderRollen > 12)
    maengel.push(`${e.melderRollen} Sichtbarkeitsmeldungen in den drei Sekunden nach dem ` +
                 'Rollen (soll <= 12; mehr heisst: die zwei Melder schieben das Fenster gegeneinander)');
  return maengel;
}

// ------------------------------------------------------------- Hauptlauf
const FAELLE = [
  { name: 'A_6654_sofort_1300x900',   modus: 'sofort', zeilen: 6654, takt: 0, breite: 1300, hoehe: 900, dpr: 1 },
  { name: 'B_6654_laden_1300x900',    modus: 'laden',  zeilen: 6654, takt: 0, breite: 1300, hoehe: 900, dpr: 1 },
  { name: 'C_6654_laden_1300x700',    modus: 'laden',  zeilen: 6654, takt: 0, breite: 1300, hoehe: 700, dpr: 1 },
  { name: 'D_6654_laden_dpr125',      modus: 'laden',  zeilen: 6654, takt: 0, breite: 1300, hoehe: 900, dpr: 1.25 },
  { name: 'E_6654_laden_takt10',      modus: 'laden',  zeilen: 6654, takt: 10, breite: 1300, hoehe: 900, dpr: 1 },
  { name: 'F_119_sofort_gegenprobe',  modus: 'sofort', zeilen: 119,  takt: 0, breite: 1300, hoehe: 900, dpr: 1 },
  { name: 'G_20746_laden_1300x900',   modus: 'laden',  zeilen: 20746, takt: 0, breite: 1300, hoehe: 900, dpr: 1 },
  // Filtern nach dem Abruf: 6 654 -> rund 440 Zeilen, weiter virtualisiert.
  { name: 'I_6654_gefiltert_sonnen',  modus: 'sofort', zeilen: 6654, takt: 0, breite: 1300, hoehe: 900, dpr: 1,
    suche: 'sonnen' },
  // Die GEGENPROBE zum Fix: dieselbe Seite, aber das gesetzte Zeilenmass per
  // Stilblatt wieder weggenommen. Sie MUSS die Sollwerte verfehlen.
  { name: 'H_6654_ohne_Zeilenmass',   modus: 'sofort', zeilen: 6654, takt: 0, breite: 1300, hoehe: 900, dpr: 1,
    entpinnt: true, mussFehlschlagen: true }
];

if (FOTOS) await mkdir(FOTOS, { recursive: true });

const browser = await chromium.launch({ headless: true });
const ergebnisse = [];
let schlecht = 0;

try {
for (const f of FAELLE) {
  if (NUR && !f.name.startsWith(NUR)) continue;
  const e = await fall(browser, f);
  ergebnisse.push(e);
  zeige(e);
  const m = pruefe(e);

  if (f.mussFehlschlagen) {
    // Die Gegenprobe ist genau dann in Ordnung, wenn sie den Fehler ZEIGT.
    if (m.length) console.log('  Gegenprobe zeigt den Fehler wie erwartet: ' + m.join('; '));
    else { schlecht++; console.log('  GEGENPROBE OHNE WIRKUNG: ohne gesetztes Zeilenmass ' +
                                   'bleibt die Liste ruhig - dann belegt der Fix nichts.'); }
    continue;
  }

  if (m.length) { schlecht++; console.log('  SOLL VERFEHLT: ' + m.join('; ')); }
  else console.log('  soll erfuellt');
}
} catch (fehler) {
  // Aufruf, Wirt oder Stilblatt - kein Messergebnis, sondern ein Aufbaufehler.
  // Er bekommt eine EIGENE Rueckgabe (2), damit er nicht als "Sollwert
  // verfehlt" durchgeht.
  console.log('');
  console.log('ABBRUCH: ' + fehler.message);
  await browser.close();
  process.exit(2);
}

await browser.close();

console.log('');
console.log(schlecht === 0
  ? `ALLE ${ergebnisse.length} FAELLE ERFUELLEN DIE SOLLWERTE.`
  : `${schlecht} von ${ergebnisse.length} FAELLEN VERFEHLEN DIE SOLLWERTE.`);

if (process.env.RASTERPROBE_JSON) {
  console.log('---JSON---');
  console.log(JSON.stringify(ergebnisse, null, 1));
}

process.exit(schlecht === 0 ? 0 : 1);
