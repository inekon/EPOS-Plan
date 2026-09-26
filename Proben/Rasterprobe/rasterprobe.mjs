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
//  In den Faellen mit "frei: true" (ein Raster ohne eigene Hoechsthoehe,
//  Begrenzt=false, etwa die Wohnungstabelle des Zapfprofils) misst sie dazu
//    (l) ob die Huelle selbst rollt (soll nicht: der Rollbereich ist der Dialog),
//        welcher Vorfahr wirklich rollt, und ob alle Zeilen gleich hoch sind;
//        dazu, ob die Seite oder die Ueberlagerung um das Raster QUER rollt
//        (soll nicht - quer rollt allein die Huelle, und in den Faellen mit
//        "querMax" auch sie hoechstens so weit).
//  "bereich" grenzt die Messung auf EIN Raster der Seite ein (CSS-Selektor),
//  "klick" ist der Schritt des Anwenders vor der Messung (etwa die Stufe).
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
// gewoehnliche Weg, dann der globale Wurzelordner. Ohne das volle Paket genuegt
// "playwright-core" (ohne eigenen Browser) zusammen mit --kanal, siehe unten.
const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  try { return await import('playwright-core'); } catch { /* auch nicht */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  const holen = createRequire(wurzel + '/rasterprobe.cjs');
  try { return holen('playwright'); } catch { return holen('playwright-core'); }
})();

// ---------------------------------------------------------------- Aufruf
const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
// --kanal msedge (oder chrome) nimmt den installierten Browser statt des
// Playwright-Chromium; Edge rechnet mit derselben Engine wie WebView2.
const KANAL = arg('kanal', '');
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
      // Nur die Melder IM Bereich zaehlen - andere Bausteine der Seite (Bilder,
      // Dialoge) duerfen eigene IntersectionObserver fuehren.
      const b = window.__probeBereich;
      for (const e of eintraege) if (!b || (e.target.closest && e.target.closest(b))) p.melder.push({
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
    const w = window.__probeBereich ? document.querySelector(window.__probeBereich) : document;
    const t = w && w.querySelector('table.quickgrid');
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
const ABLESEN = (bereich) => {
  const wurzel = bereich ? document.querySelector(bereich) : document;
  const tabelle = wurzel && wurzel.querySelector('table.quickgrid');
  if (!tabelle) return { fehler: 'kein QuickGrid im Baum' + (bereich ? ' unter ' + bereich : '') };

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

  // (l) Der Vorfahr, der WIRKLICH rollt: overflow-y auto/scroll UND mehr Inhalt
  // als Platz. Bei einem Raster ohne eigene Hoechsthoehe ist das der Dialog.
  const findeRollenden = el => {
    for (let v = el.parentElement; v && v !== document.documentElement; v = v.parentElement) {
      const oy = getComputedStyle(v).overflowY;
      if ((oy === 'auto' || oy === 'scroll') && v.scrollHeight > v.clientHeight + 1) return v;
    }
    return null;
  };
  const rollender = findeRollenden(tabelle);
  const echtHoehen = echteZeilen.map(z => +z.getBoundingClientRect().height.toFixed(3));
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
    echtMin: echtHoehen.length ? Math.min(...echtHoehen) : null,
    echtMax: echtHoehen.length ? Math.max(...echtHoehen) : null,
    rollenderVorfahr: rollender ? kennung(rollender) : '(keiner)',
    seiteQuer: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    ueberlagerungQuer: (u => u ? u.scrollWidth - u.clientWidth : null)(tabelle.closest('.epos-ueberlagerung')),
    rollbehaelter: kennung(behaelter),
    rollbehaelterHoehe: behaelter ? behaelter.clientHeight : null,
    huelle: huelle ? {
      kennung: kennung(huelle),
      clientHoehe: huelle.clientHeight,
      rollHoehe: huelle.scrollHeight,
      querUeberstand: huelle.scrollWidth - huelle.clientWidth,
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
  await seite.addInitScript(b => { window.__probeBereich = b; }, f.bereich || '');
  await seite.addInitScript(SONDE);
  const B = f.bereich || '';

  // f.pfad: eine andere Seite des Wirtes - die Katalogprobe mit dem GANZEN
  // Dialog (Neuordnung Stufe 1, Faelle J und K): dieselbe virtualisierte Liste,
  // aber im Katalograhmen, dessen Liste die Resthoehe nimmt statt 420 px.
  const adresse = f.pfad ? `${WURZEL}${f.pfad}`
                         : `${WURZEL}/probe?modus=${f.modus}&zeilen=${f.zeilen}&takt=${f.takt}`;
  await seite.goto(adresse, { waitUntil: 'domcontentloaded' });

  // f.klick: der Schritt des Anwenders vor der Messung (etwa die Stufe
  // "Erweitert" des Zapfprofils, erst mit ihr steht die Wohnungstabelle da).
  for (const k of [].concat(f.klick || [])) {
    await seite.waitForSelector(k, { timeout: 20000 });
    await schlaf(500);
    await seite.click(k);
  }

  // Auf den Aufbau der interaktiven Komponente warten (Blazor Server).
  await seite.waitForSelector((B ? B + ' ' : '') + 'table.quickgrid', { timeout: 20000 });

  // DIE GEGENPROBE (#235): Nimmt man der Zeile ihr gesetztes Mass wieder weg,
  // muss der Fehler zurueckkommen. Sonst belegt die Messung nur, dass es heute
  // laeuft - nicht, dass DIESE Zeile im Stilblatt es laufen laesst.
  // f.stil: ein Stilblatt der Gegenprobe, das eine Behebung wieder wegnimmt.
  if (f.stil) await seite.addStyleTag({ content: f.stil });

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
  const stil = await seite.evaluate(b => {
    const h = (b ? document.querySelector(b) : document).querySelector('.epos-raster-huelle');
    // Gelesen wird der RAHMEN der Huelle: Im Katalogdialog traegt die Liste seit
    // Stufe 1 der Neuordnung keine Hoechsthoehe mehr (max-height: none), der
    // Rahmen aber steht nur im Hausstilblatt.
    return h ? getComputedStyle(h).borderTopStyle : '(keine Huelle)';
  }, B);
  if (stil !== 'solid') {
    throw new Error(`Das Stilblatt epos-ui.css wirkt nicht (Rahmen der Huelle: ${stil}). ` +
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
  const bei500 = await seite.evaluate(ABLESEN, B);
  await fotoschuss('t0500');

  await schlaf(4500);                       // t = 5 s
  const bei5000 = await seite.evaluate(ABLESEN, B);
  await fotoschuss('t5000');

  const proben1 = await seite.evaluate(() => window.__probe.proben);
  const um1 = await seite.evaluate(() => window.__probe.umschaltungen);
  const melder1 = await seite.evaluate(() => window.__probe.melder.length);

  // Der Nullpunkt des Beobachtens ist das ENDE des Ladens (bzw. des Filterns):
  // vorher darf und muss "loading" schalten. Im Modus "sofort" ist das t = 0.
  const fertig = f.suche ? 2200 : (f.modus === 'laden' ? 1200 : 0);

  // --- (g) Rollen um 2 000 px und zurueck ---------------------------
  const rollmass = await seite.evaluate(b => {
    const h = (b ? document.querySelector(b) : document).querySelector('.epos-raster-huelle--hoch, .epos-raster-huelle');
    if (!h) return null;
    h.scrollTop = 2000;
    return { gesetzt: h.scrollTop, rollHoehe: h.scrollHeight };
  }, B);
  const t_roll = Date.now();
  let nachRollen = null;
  if (rollmass) {
    // Sollwert: binnen 300 ms echte Zeilen im sichtbaren Bereich.
    for (let i = 0; i < 12; i++) {
      await schlaf(25);
      const s = await seite.evaluate(b => {
        const t = (b ? document.querySelector(b) : document).querySelector('table.quickgrid');
        const zeilen = [...t.querySelectorAll('tbody > tr')]
          .filter(z => !/flex-shrink/.test(z.getAttribute('style') || ''));
        return {
          echt: zeilen.filter(z => !z.querySelector('td.grid-cell-placeholder')).length,
          platzhalter: zeilen.filter(z => z.querySelector('td.grid-cell-placeholder')).length
        };
      }, B);
      if (s.echt > 0) { nachRollen = { ...s, ms: Date.now() - t_roll }; break; }
    }
    if (!nachRollen) {
      const s = await seite.evaluate(ABLESEN, B);
      nachRollen = { echt: s.echt, platzhalter: s.platzhalter, ms: Date.now() - t_roll };
    }
    await schlaf(3000);                     // drei Sekunden Ruhe NACH dem Rollen
    await fotoschuss('gerollt');
  }

  const nachRollenStand = await seite.evaluate(ABLESEN, B);
  const umRollen = await seite.evaluate(() => window.__probe.umschaltungen.length) - um1.length;
  const melderRollen = await seite.evaluate(() => window.__probe.melder.length) - melder1;

  await seite.evaluate(b => {
    const h = (b ? document.querySelector(b) : document).querySelector('.epos-raster-huelle--hoch, .epos-raster-huelle');
    if (h) h.scrollTop = 0;
  }, B);
  await schlaf(800);
  const zurueck = await seite.evaluate(ABLESEN, B);
  await fotoschuss('zurueck');

  // --- STUFE 2 (V11): die Tastatur, nur in den Dialogfaellen mit tasten: true ---
  let tasten = null;
  if (f.tasten) {
    const sel = '.epos-katalogliste .epos-raster-huelle[tabindex="0"]';
    if (await seite.locator(sel).count() === 0) tasten = { fehler: 'die Liste ist kein Tabulatorhalt' };
    else {
      await seite.focus(sel);
      const lies = () => seite.evaluate(s => {
        const h = document.querySelector(s);
        const zeile = h.querySelector('tbody tr.epos-zeile--gewaehlt');
        const kopf = h.querySelector('thead');
        const hr = h.getBoundingClientRect();
        const oben = hr.top + h.clientTop + (kopf ? kopf.getBoundingClientRect().height : 0);
        const unten = hr.top + h.clientTop + h.clientHeight;
        const zr = zeile ? zeile.getBoundingClientRect() : null;
        return {
          index: zeile ? +zeile.getAttribute('aria-rowindex') - 2 : -1,
          sichtbar: zr ? (zr.top >= oben - 0.5 && zr.bottom <= unten + 0.5) : false,
          fokus: document.activeElement === h,
          rollstand: Math.round(h.scrollTop),
          platzhalter: h.querySelectorAll('td.grid-cell-placeholder').length
        };
      }, sel);
      const vorEnde = await seite.evaluate(() => window.__probe.melder.length);
      await seite.keyboard.press('End');
      await schlaf(3000);
      const melderEnde = await seite.evaluate(() => window.__probe.melder.length) - vorEnde;
      const ende = await lies();
      await fotoschuss('taste_ende');
      await seite.keyboard.press('Home');
      await schlaf(1500);
      const pos1 = await lies();
      tasten = { ende, pos1, melderEnde };
    }
  }

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
    rollmass, nachRollen, nachRollenStand, zurueck, umRollen, melder1, melderRollen, tasten,
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
              `  (echt ${b.echtHoehe}, Platzhalter ${b.platzhalterHoehe}, ItemSize ${e.fall.zeile || 53})`);
  if (e.fall.frei)
    console.log(`  (l) echte Zeilen ${b.echt} von ${e.fall.zeilen}, Hoehe ${b.echtMin} … ${b.echtMax} px;` +
                ` Huelle rollt senkrecht ${b.huelle ? b.huelle.rollHoehe - b.huelle.clientHoehe : '-'} px,` +
                ` quer ${b.huelle ? b.huelle.querUeberstand : '-'} px; rollender Vorfahr ${b.rollenderVorfahr};` +
                ` Seite quer ${b.seiteQuer} px` +
                (b.ueberlagerungQuer === null ? '' : `, Ueberlagerung quer ${b.ueberlagerungQuer} px`));
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
  if (e.tasten && !e.tasten.fehler)
    console.log(`  (k) Tastatur: Ende → Zeile ${e.tasten.ende.index} sichtbar=${e.tasten.ende.sichtbar} ` +
                `Platzhalter ${e.tasten.ende.platzhalter} Rollstand ${e.tasten.ende.rollstand}, ` +
                `${e.tasten.melderEnde} Melder in 3 s;  Pos1 → Zeile ${e.tasten.pos1.index} ` +
                `Rollstand ${e.tasten.pos1.rollstand}; Fokus ${e.tasten.ende.fokus && e.tasten.pos1.fokus}`);
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
  // Im Katalogdialog (Faelle J, K): Virtualize muss die HUELLE als Rollbehaelter
  // finden - fixe Hoehe aus dem Rahmen statt max-height - und die Zeile ist so
  // hoch wie ItemSize.
  // (l) Ein Raster ohne eigene Hoechsthoehe (Begrenzt=false): keine
  // Virtualisierung, also weder Abstandshalter noch Sichtbarkeitsmelder; alle
  // Zeilen gezeichnet und gleich hoch; die Huelle rollt NICHT selbst - der
  // Rollbereich ist der Dialog (sonst Rollbereich im Rollbereich).
  if (e.fall.frei) {
    const b = e.bei5000;
    if (b.abstandshalter.length) maengel.push(`${b.abstandshalter.length} Abstandshalter (soll 0: nicht virtualisiert)`);
    if (e.melder1 + e.melderRollen !== 0)
      maengel.push(`${e.melder1 + e.melderRollen} Sichtbarkeitsmeldungen (soll 0: nicht virtualisiert)`);
    if (b.echt !== e.fall.zeilen) maengel.push(`${b.echt} von ${e.fall.zeilen} Zeilen gezeichnet`);
    if (b.echtMin !== null && b.echtMax - b.echtMin > 0.5)
      maengel.push(`Zeilenhoehen ${b.echtMin} … ${b.echtMax} px (soll gleich)`);
    if (b.huelle && b.huelle.rollHoehe - b.huelle.clientHoehe > 1)
      maengel.push(`die Huelle rollt selbst (${b.huelle.rollHoehe - b.huelle.clientHoehe} px) - Rollbereich im Rollbereich`);
    if (/epos-raster-huelle/.test(b.rollenderVorfahr || ''))
      maengel.push(`der rollende Vorfahr ist die Huelle (${b.rollenderVorfahr})`);
    if (b.seiteQuer > 0) maengel.push(`die Seite rollt quer um ${b.seiteQuer} px (soll 0)`);
    if (b.ueberlagerungQuer !== null && b.ueberlagerungQuer > 1)
      maengel.push(`die Ueberlagerung rollt quer um ${b.ueberlagerungQuer} px (soll 0: quer rollt allein die Huelle)`);
    if (e.fall.querMax !== undefined && b.huelle && b.huelle.querUeberstand > e.fall.querMax)
      maengel.push(`die Huelle rollt quer um ${b.huelle.querUeberstand} px (soll <= ${e.fall.querMax})`);
  }
  if (e.fall.pfad && !e.fall.frei) {
    const b = e.bei5000;
    if (!/epos-raster-huelle/.test(b.rollbehaelter || ''))
      maengel.push(`Rollbehaelter ist ${b.rollbehaelter}, nicht die Huelle der Liste`);
    const soll = e.fall.zeile || 53;
    if (b.echtHoehe !== null && Math.abs(b.echtHoehe - soll) > 0.5)
      maengel.push(`Zeilenhoehe ${b.echtHoehe} px statt ${soll} (ItemSize)`);
    if (b.platzhalterHoehe !== null && b.platzhalterHoehe !== undefined &&
        Math.abs(b.platzhalterHoehe - soll) > 0.5)
      maengel.push(`Platzhalterzeile ${b.platzhalterHoehe} px statt ${soll}`);
  }
  // STUFE 2 (V11): die Tastatur in der virtualisierten Liste. Ende springt ans
  // Ende - die Zeile steht gezeichnet und ganz im Bild -, Pos1 zurueck an den
  // Anfang, und die Sichtbarkeitsmelder beruhigen sich danach wie nach dem Rollen.
  const t = e.tasten;
  if (t) {
    if (t.fehler) maengel.push(t.fehler);
    else {
      if (t.ende.index !== e.fall.zeilen - 1) maengel.push(`Ende waehlt Zeile ${t.ende.index} statt ${e.fall.zeilen - 1}`);
      if (!t.ende.sichtbar) maengel.push('nach Ende steht die gewaehlte Zeile nicht im Bild');
      if (t.ende.platzhalter) maengel.push(`${t.ende.platzhalter} Platzhalter nach Ende`);
      if (t.melderEnde > 12) maengel.push(`${t.melderEnde} Sichtbarkeitsmeldungen in 3 s nach Ende (soll <= 12)`);
      if (t.pos1.index !== 0 || t.pos1.rollstand !== 0)
        maengel.push(`Pos1: Zeile ${t.pos1.index}, Rollstand ${t.pos1.rollstand} (soll 0 / 0)`);
      if (!t.pos1.sichtbar) maengel.push('nach Pos1 steht die gewaehlte Zeile nicht im Bild');
      if (!t.ende.fokus || !t.pos1.fokus) maengel.push('die Liste verliert den Fokus');
    }
  }
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
    entpinnt: true, mussFehlschlagen: true },
  // NEUORDNUNG STUFE 1 (Konzept Administrationsdialoge, Abschnitt 7): die
  // Fenstermasse des Anwenders. J und K stellen die virtualisierte Liste IM
  // Katalogdialog (Stromspeicher-Verwaltung, 6 654 volle Zeilen) - dort nimmt
  // sie seit V1 die Resthoehe; L und M die freie Liste der Importmaske.
  // Seit Stufe 2 (V4) ist die Zeile dort die Wahl: 46 px statt 53, und die
  // Tastatur (Ende, Pos1) waehlt und rollt mit.
  { name: 'J_6654_Dialog_1088x624',   modus: 'sofort', zeilen: 6654, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=modul&art=stromspeicher&zeilen=6654&voll=1', zeile: 46, tasten: true },
  { name: 'K_6654_Dialog_400x624',    modus: 'sofort', zeilen: 6654, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=modul&art=stromspeicher&zeilen=6654&voll=1', zeile: 46, tasten: true },
  // ZAPFPROFILGENERATOR (Stufe Z4): der Katalog der Brauchwasser-Nutzungsarten
  // (Katalogliste, Zeile ist Wahl, 46 px) - virtualisiert (6 654 Zeilen) und im
  // Mass eines Katalogs unter der Schwelle (40); dazu die zwei Raster ohne eigene
  // Hoechsthoehe: die Wohnungstabelle der Stufe Erweitert (Zapfprofil) und das
  // Raster der Zapfkategorien, bearbeitbar und lesend.
  { name: 'T1_tww_6654_Dialog_1088x624', modus: 'sofort', zeilen: 6654, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=6654', zeile: 46, tasten: true },
  { name: 'T2_tww_6654_Dialog_400x624',  modus: 'sofort', zeilen: 6654, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=6654', zeile: 46, tasten: true },
  { name: 'T3_tww_40_Dialog_1088x624',   modus: 'sofort', zeilen: 40, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=40', zeile: 46, tasten: true },
  // GEBAEUDESIMULATION G3, WELLE K: die Katalogliste des Gebaeude-PROJEKTDIALOGS
  // (Zweispaltenauswahl: Projektliste oben, Uebernahmeleiste, Katalog darunter). Als
  // Projektdialog behaelt sie die Wahlspalte: Zeile 53 px, keine Tastenfuehrung (die
  // Zeile ist dort nicht die Wahl). "bereich" grenzt die Messung auf die Katalogliste
  // ein - die Projektliste darueber traegt ebenfalls eine .epos-raster-huelle, und
  // ohne Eingrenzung rollte die Probe die falsche. GD3 hat das Mass der Testdatenbank
  // (269 Gebaeude, weiter virtualisiert).
  { name: 'GD1_gebaeude_6654_Dialog_1088x624', modus: 'sofort', zeilen: 6654, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=projekt-gebaeude&zeilen=6654', zeile: 53, bereich: '.epos-katalogliste' },
  { name: 'GD2_gebaeude_6654_Dialog_400x624',  modus: 'sofort', zeilen: 6654, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=projekt-gebaeude&zeilen=6654', zeile: 53, bereich: '.epos-katalogliste' },
  { name: 'GD3_gebaeude_269_Dialog_1088x624',  modus: 'sofort', zeilen: 269, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=projekt-gebaeude&zeilen=269', zeile: 53, bereich: '.epos-katalogliste' },
  { name: 'Z1_wohnungen_12_1088x624',    modus: 'sofort', zeilen: 12, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=wohnungen&zeilen=12', frei: true, bereich: '.epos-zapfprofil-wohnungen',
    klick: 'fieldset[aria-label="Stufe"] label.epos-option:has-text("Erweitert")' },
  { name: 'Z2_wohnungen_12_400x624',     modus: 'sofort', zeilen: 12, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=wohnungen&zeilen=12', frei: true, bereich: '.epos-zapfprofil-wohnungen',
    klick: 'fieldset[aria-label="Stufe"] label.epos-option:has-text("Erweitert")' },
  { name: 'Z3_kategorien_10_1088x624',   modus: 'sofort', zeilen: 10, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=kategorien&zeilen=10', frei: true, bereich: '.epos-zapfprofil-kategorienraster' },
  { name: 'Z4_kategorien_10_400x624',    modus: 'sofort', zeilen: 10, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=kategorien&zeilen=10', frei: true, bereich: '.epos-zapfprofil-kategorienraster' },
  { name: 'Z5_kategorien_lesend_1088x624', modus: 'sofort', zeilen: 10, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=kategorien&art=lesen&zeilen=10', frei: true, bereich: '.epos-zapfprofil-kategorienraster' },
  // Das Raster der Zapfkategorien DORT, wo der Anwender es sieht: in der
  // Ueberlagerung "Kategorien..." des Katalogdialogs (position: fixed). Bei
  // 1 088 px rollt es nicht quer; bei 400 px rollt allein die Huelle. Z8 ist
  // die GEGENPROBE: ohne positionierten Vorfahren der versteckten Beschriftung
  // rollt die Ueberlagerung mit - sie MUSS die Sollwerte verfehlen.
  { name: 'Z6_kategorien_ueberlagerung_1088x624', modus: 'sofort', zeilen: 10, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=10', frei: true, querMax: 1,
    bereich: '.epos-ueberlagerung .epos-zapfprofil-kategorienraster', klick: ['button.epos-tww-kategorien'] },
  { name: 'Z7_kategorien_ueberlagerung_400x624', modus: 'sofort', zeilen: 10, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=10', frei: true,
    bereich: '.epos-ueberlagerung .epos-zapfprofil-kategorienraster',
    klick: ['.epos-auswahlleiste button.epos-nur-schmal', 'button.epos-tww-kategorien'] },
  { name: 'Z8_kategorien_ohne_Bezugskasten', modus: 'sofort', zeilen: 10, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/katalogprobe?maske=tww&zeilen=10', frei: true,
    bereich: '.epos-ueberlagerung .epos-zapfprofil-kategorienraster',
    klick: ['.epos-auswahlleiste button.epos-nur-schmal', 'button.epos-tww-kategorien'],
    stil: '.epos-zapfprofil-kategorienraster .epos-feld { position: static !important; }', mussFehlschlagen: true },
  // Stufe G6c, Welle C: die Liste "Flaechen je Zone" im Zuordnungsdialog des Gebaeudeimports
  // (Seite /gebaeudeimport, Zonenhaus je Geschoss, die Flaechen der Probe reihum auf 2 400 Zeilen
  // vervielfacht). Der Klick liest die Probe; gemessen nur in der Flaechenliste. Die Zeile ist die
  // Wahl (46 px), die Liste virtualisiert, Rollbehaelter ist ihre Huelle.
  { name: 'GI_gebaeudeimport_2400_1088x624', modus: 'sofort', zeilen: 2400, takt: 0, breite: 1088, hoehe: 624, dpr: 1,
    pfad: '/gebaeudeimport?datei=ifc4_zonen.ifc&flaechen=2400', zeile: 46, bereich: '.epos-gebimport-flaechen',
    klick: ['.epos-gebimport .epos-dateiwahl button'] },
  { name: 'GJ_gebaeudeimport_2400_400x624',  modus: 'sofort', zeilen: 2400, takt: 0, breite: 400, hoehe: 624, dpr: 1,
    pfad: '/gebaeudeimport?datei=ifc4_zonen.ifc&flaechen=2400', zeile: 46, bereich: '.epos-gebimport-flaechen',
    klick: ['.epos-gebimport .epos-dateiwahl button'] },
  { name: 'L_6654_sofort_1088x624',   modus: 'sofort', zeilen: 6654, takt: 0, breite: 1088, hoehe: 624, dpr: 1 },
  { name: 'M_6654_sofort_400x624',    modus: 'sofort', zeilen: 6654, takt: 0, breite: 400, hoehe: 624, dpr: 1 }
];

if (FOTOS) await mkdir(FOTOS, { recursive: true });

const browser = await chromium.launch(KANAL ? { headless: true, channel: KANAL } : { headless: true });
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
