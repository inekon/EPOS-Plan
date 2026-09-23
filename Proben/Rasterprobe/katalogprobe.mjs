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
//
// Seit Stufe 1 der Neuordnung ist der Rahmen eine Flexspalte mit zwei
// rollenden Bereichen; die Gegenprobe stellt deshalb auch das RASTER von damals
// wieder her (zwei auto-Reihen), den Eingabeblock ohne Hoechsthoehe und die
// Liste mit ihren 457,6 px - sonst gaebe es den Befund in der neuen Anordnung
// gar nicht zu zeigen.
const VORHER_STIL = `
  .epos-katalog-paar.epos-katalog-fuellend {
    display: grid !important; grid-template-rows: auto auto !important;
    flex: 1 1 auto !important; min-height: 0 !important; overflow: visible !important;
  }
  .epos-katalog-paar > .epos-katalog-liste,
  .epos-katalog-paar > .epos-katalog-eingabe { min-height: 0 !important; max-height: none !important;
    overflow: visible !important; }
  .epos-katalog-liste .epos-katalogliste > .epos-raster-huelle { max-height: 457.6px !important;
    flex: 0 1 auto !important; }
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
    //
    // EIN VORFAHR IST KEINE UEBERDECKUNG. Der Knopf des Hauses traegt
    // border-radius 6px (--epos-ecke); Chromium schnappt seinen Kasten fuer die
    // Trefferpruefung auf ganze Bildpunkte. Je nach Bruchteil der Breite liegt
    // der 2px-Eckpunkt damit um Bruchteile eines Bildpunktes NEBEN der
    // abgerundeten Ecke - gemessen an der Waermepumpen-Fussleiste: rechter Rand
    // 262,484 px, geschnappt 262, Abstand zum Bogenmittelpunkt 6,009 statt
    // hoechstens 6. elementFromPoint meldet dann die Leiste, also den Vorfahren
    // des Knopfes. Ein Vorfahr kann sein eigenes Kind aber nicht ueberdecken -
    // er zeichnet darunter. Der Punkt liegt schlicht ausserhalb der runden Ecke,
    // und das ist kein Befund. Eine echte Ueberdeckung kommt IMMER aus einem
    // anderen Zweig (img.epos-chartbild, ein Eingabeblock) und wird weiter
    // gemeldet.
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
        if (oben === b || b.contains(oben) || oben.contains(b)) continue;
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

// =====================================================================
//  STUFE 1 DER NEUORDNUNG (Konzept Administrationsdialoge, V1 V2 V7)
// =====================================================================
//
//  KAESTEN fragt, ob sich Kaesten UEBERLAGERN (KL-5). Die Neuordnung fragt
//  weiter: WIE VIELE Bereiche rollen, liegt einer IM anderen, rollt die Liste
//  QUER, und welche Spalte ist daran schuld? Alles in Fensterkoordinaten, im
//  Zustand nach der Wahl der ersten Zeile.
//
//  Ein Element ROLLT, wenn es overflow auto/scroll traegt UND mehr Inhalt hat
//  als Platz (scrollHeight > clientHeight + 1 bzw. quer). Ein Element mit
//  overflow: auto, das gerade nichts zu rollen hat, ist kein Rollbereich -
//  der Anwender sieht keinen Balken.
const STUFE1 = () => {
  const wurzel = document.querySelector('.epos-katalog-dialog');
  if (!wurzel) return { fehler: 'kein .epos-katalog-dialog im Baum' };

  const name = el => el.tagName.toLowerCase() +
    (el.className && typeof el.className === 'string'
      ? '.' + el.className.trim().split(/\s+/).slice(0, 3).join('.') : '');

  const bereiche = [];
  for (const el of [wurzel, ...wurzel.querySelectorAll('*')]) {
    const s = getComputedStyle(el);
    if (s.display === 'none') continue;
    const y = (s.overflowY === 'auto' || s.overflowY === 'scroll') && el.scrollHeight > el.clientHeight + 1;
    const x = (s.overflowX === 'auto' || s.overflowX === 'scroll') && el.scrollWidth > el.clientWidth + 1;
    if (x || y) bereiche.push({ el, name: name(el), x, y,
      client: `${el.clientWidth}x${el.clientHeight}`, roll: `${el.scrollWidth}x${el.scrollHeight}` });
  }
  const verschachtelt = [];
  for (const a of bereiche)
    for (const b of bereiche)
      if (a !== b && a.el.contains(b.el)) verschachtelt.push(`${b.name} in ${a.name}`);

  const huelle = wurzel.querySelector('.epos-katalogliste .epos-raster-huelle');
  const tabelle = huelle ? huelle.querySelector('table') : null;
  const hr = huelle ? huelle.getBoundingClientRect() : null;
  const spalten = tabelle ? [...tabelle.querySelectorAll('thead th')].map(t => {
    const r = t.getBoundingClientRect();
    return {
      text: (t.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 28),
      klasse: String(t.className || '').replace(/\s+/g, ' ').trim(),
      sichtbar: getComputedStyle(t).display !== 'none',
      breite: +r.width.toFixed(1),
      // rechter Rand relativ zur INNENkante der Huelle, ohne Rollstand
      rechts: huelle ? +(r.right - hr.left - huelle.clientLeft + huelle.scrollLeft).toFixed(1) : 0
    };
  }) : [];

  // Die erste echte Datenzeile und die Zelle des Bezeichners darin.
  const zeile = tabelle ? [...tabelle.querySelectorAll('tbody > tr')]
    .find(z => z.querySelector('td') && !z.querySelector('td.grid-cell-placeholder')) : null;
  let bezIndex = spalten.findIndex(s => /bezeichner/.test(s.klasse));
  if (bezIndex < 0) bezIndex = spalten.findIndex(s => /^(Bezeichner|Modell|Region|Bezeichnung)/.test(s.text));
  const bezZelle = zeile && bezIndex >= 0 ? zeile.children[bezIndex] : null;
  const bezTraeger = bezZelle ? (bezZelle.querySelector('[title]') || bezZelle) : null;

  const rechteck = el => {
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { links: +r.left.toFixed(1), oben: +r.top.toFixed(1), breite: +r.width.toFixed(1), hoehe: +r.height.toFixed(1) };
  };

  return {
    fenster: { breite: innerWidth, hoehe: innerHeight },
    dialogRollt: wurzel.scrollHeight > wurzel.clientHeight + 1,
    dialogMass: `${wurzel.clientWidth}x${wurzel.clientHeight} / Inhalt ${wurzel.scrollWidth}x${wurzel.scrollHeight}`,
    bereiche: bereiche.map(b => ({ name: b.name, quer: b.x, hoch: b.y, client: b.client, roll: b.roll })),
    verschachtelt,
    rahmen: rechteck(wurzel.querySelector('.epos-katalog-paar')),
    liste: rechteck(wurzel.querySelector('.epos-katalog-liste')),
    eingabe: rechteck(wurzel.querySelector('.epos-katalog-eingabe')),
    huelle: huelle ? { ...rechteck(huelle), innen: huelle.clientWidth, inhalt: huelle.scrollWidth,
                       innenHoehe: huelle.clientHeight, inhaltHoehe: huelle.scrollHeight,
                       maxHoehe: getComputedStyle(huelle).maxHeight } : null,
    tabelle: rechteck(tabelle),
    querUeberlauf: huelle ? huelle.scrollWidth - huelle.clientWidth : 0,
    spalten,
    zeilenhoehe: zeile ? +zeile.getBoundingClientRect().height.toFixed(3) : null,
    bezeichner: bezTraeger ? {
      text: (bezTraeger.textContent || '').trim().slice(0, 40),
      gekuerzt: bezZelle.scrollWidth > bezZelle.clientWidth + 1,
      titel: bezTraeger.getAttribute('title') || '',
      textOverflow: getComputedStyle(bezZelle).textOverflow
    } : null
  };
};

// ------------------------------------------------ STUFE 2 der Neuordnung
//  (Konzept Administrationsdialoge, V4 "die Zeile ist die Wahl" und V10 "das
//  Schloss"). Gemessen im Zustand nach der Wahl der ersten Zeile:
//    * die Hoehe JEDER gezeichneten Datenzeile - alle gleich dem Zeilenmass,
//      keine bricht um (ein Umbruch machte sie hoeher);
//    * die Wahlspalte ist weg (kein th.epos-spalte-wahl, kein runder Knopf);
//    * das Schloss eines Auslieferungssatzes steht ganz in seiner Zelle,
//      sichtbar und nicht abgeschnitten, 16 x 16 px.
const STUFE2 = () => {
  const liste = document.querySelector('.epos-katalog-liste .epos-katalogliste');
  if (!liste) return { fehler: 'keine Katalogliste im Rahmen' };
  const huelle = liste.querySelector('.epos-raster-huelle');
  const zeilen = [...liste.querySelectorAll('tbody > tr')]
    .filter(z => z.querySelector('td') && !z.querySelector('td.grid-cell-placeholder'));
  const hoehen = zeilen.slice(0, 12).map(z => +z.getBoundingClientRect().height.toFixed(3));

  const schloss = liste.querySelector('tbody .epos-schloss');
  let schlossbefund = null;
  if (schloss) {
    const r = schloss.getBoundingClientRect();
    const zelle = schloss.closest('td').getBoundingClientRect();
    schlossbefund = {
      breite: +r.width.toFixed(1), hoehe: +r.height.toFixed(1),
      inZelle: r.left >= zelle.left - 0.5 && r.right <= zelle.right + 0.5 &&
               r.top >= zelle.top - 0.5 && r.bottom <= zelle.bottom + 0.5,
      sichtbar: getComputedStyle(schloss).visibility !== 'hidden' && r.width > 0,
      titel: schloss.getAttribute('title') || ''
    };
  }
  // Der linke Balken der Fokuszeile steht an ihrer ersten SICHTBAREN Zelle - und nur
  // dort, auch wenn die erste Spalte weicht (Waermepumpe, schmal).
  const fokus = liste.querySelector('tbody tr.epos-zeile--gewaehlt');
  let balken = null;
  if (fokus) {
    const sichtbar = [...fokus.children].filter(td => getComputedStyle(td).display !== 'none');
    const mit = sichtbar.map(td => /inset/.test(getComputedStyle(td).boxShadow));
    balken = { ersteZelle: mit[0] === true, weitere: mit.slice(1).filter(Boolean).length };
  }

  return {
    hoehen,
    balken,
    wahlspalte: liste.querySelectorAll('th.epos-spalte-wahl, .epos-anlagenwahl').length,
    zeilenwahl: liste.classList.contains('epos-katalogliste--zeilenwahl'),
    tabulatorhalt: huelle ? huelle.getAttribute('tabindex') : null,
    gewaehlt: liste.querySelectorAll('tbody tr.epos-zeile--gewaehlt').length,
    schloss: schlossbefund
  };
};

//  DIE TASTATUR (V11): Die Liste bekommt den Fokus, dann Ende, Pos1 und zweimal
//  Pfeil runter. Nach jedem Schritt: Welche Zeile ist gewaehlt (aria-rowindex),
//  steht sie GANZ im sichtbaren Teil der Huelle (unter dem stehenden Kopf), und
//  hat die Liste den Fokus behalten? Rollt das Skript nicht mit oder nimmt der
//  Browser die Taste zum Rollen, sieht man es hier.
async function tastenprobe(seite) {
  const huelle = '.epos-katalog-liste .epos-raster-huelle[tabindex="0"]';
  if (await seite.locator(huelle).count() === 0) return { fehler: 'die Liste ist kein Tabulatorhalt' };
  await seite.focus(huelle);

  const lies = () => seite.evaluate(sel => {
    const h = document.querySelector(sel);
    const zeile = h.querySelector('tbody tr.epos-zeile--gewaehlt');
    const kopf = h.querySelector('thead');
    const hr = h.getBoundingClientRect();
    const oben = hr.top + h.clientTop + (kopf ? kopf.getBoundingClientRect().height : 0);
    const unten = hr.top + h.clientTop + h.clientHeight;
    const zr = zeile ? zeile.getBoundingClientRect() : null;
    const alle = h.querySelectorAll('tbody tr[aria-rowindex]');
    return {
      index: zeile ? +zeile.getAttribute('aria-rowindex') - 2 : -1,
      zeilen: +(h.querySelector('table').getAttribute('aria-rowcount') || 0) - 1,
      sichtbar: zr ? (zr.top >= oben - 0.5 && zr.bottom <= unten + 0.5) : false,
      fokus: document.activeElement === h,
      rollstand: Math.round(h.scrollTop),
      gezeichnet: alle.length
    };
  }, huelle);

  const schritte = {};
  await seite.keyboard.press('End');    await schlaf(900); schritte.ende = await lies();
  await seite.keyboard.press('Home');   await schlaf(900); schritte.pos1 = await lies();
  await seite.keyboard.press('ArrowDown'); await schlaf(500);
  await seite.keyboard.press('ArrowDown'); await schlaf(900); schritte.runter2 = await lies();
  return schritte;
}

// ------------------------------------------------ STUFE 3 der Neuordnung
//  (Konzept Administrationsdialoge, V3 "Stammblatt neben der Liste", V6
//  "Kaestchen", V8 "Auswahlleiste", V12 "Vergleich"). Gemessen wird im Zustand
//  nach der Wahl der ersten Zeile und dann Schritt fuer Schritt:
//    start     - wo Liste, Auswahlleiste und Stammblatt stehen; wie viele
//                Zeilen GANZ im Rollbereich unter dem Kopf stehen;
//    gewaehlt  - drei Kaestchen gesetzt: die Leiste nennt "3 gewaehlt", und
//                die Liste springt nicht (die Huelle bleibt, wo sie war);
//    vergleich - (breit) "Vergleichen": die Vergleichstabelle steht IM Blatt
//                und rollt nicht quer ueber seinen Rand;
//    blatt     - (schmal) "Stammblatt ›": das Blatt tritt an die Stelle der
//                Liste, die Auswahlleiste bleibt; "‹ Liste" fuehrt zurueck.
//  Die Knoepfe werden an ihren KLASSEN gefunden, nicht am Text - der Wirt
//  laeuft in der Sprache des Rechners.
async function stufe3probe(seite, f) {
  const lies = () => seite.evaluate(() => {
    const r = el => {
      if (!el) return null;
      const b = el.getBoundingClientRect();
      return { links: +b.left.toFixed(1), oben: +b.top.toFixed(1), rechts: +b.right.toFixed(1),
               unten: +b.bottom.toFixed(1), breite: +b.width.toFixed(1), hoehe: +b.height.toFixed(1) };
    };
    const liste = document.querySelector('.epos-katalog-liste');
    const blatt = document.querySelector('.epos-katalog-stammblatt');
    const auswahl = document.querySelector('.epos-auswahlleiste');
    const huelle = document.querySelector('.epos-katalog-liste .epos-raster-huelle');
    const kopf = huelle ? huelle.querySelector('thead') : null;
    let ganz = 0;
    if (huelle && huelle.getBoundingClientRect().height > 0) {
      const h = huelle.getBoundingClientRect();
      const oben = h.top + huelle.clientTop + (kopf ? kopf.getBoundingClientRect().height : 0);
      const unten = h.top + huelle.clientTop + huelle.clientHeight;
      for (const z of huelle.querySelectorAll('tbody > tr')) {
        if (!z.querySelector('td') || z.querySelector('td.grid-cell-placeholder')) continue;
        const b = z.getBoundingClientRect();
        if (b.top >= oben - 0.5 && b.bottom <= unten + 0.5) ganz++;
      }
    }
    const vergleich = document.querySelector('.epos-stammblatt .epos-vergleichstabelle');
    const vh = vergleich ? vergleich.querySelector('.epos-vergleichstabelle-huelle') : null;
    const was = auswahl ? auswahl.querySelector('.epos-auswahlleiste-was, .epos-auswahlleiste-leise') : null;
    const name = document.querySelector('.epos-stammblatt-name');
    return {
      liste: r(liste), blatt: r(blatt), auswahl: r(auswahl), huelle: r(huelle),
      ganzeZeilen: ganz,
      auswahlText: was ? was.textContent.trim() : null,
      vergleich: vergleich ? { ...r(vergleich), quer: vh ? vh.scrollWidth - vh.clientWidth : 0,
                               zeilen: vergleich.querySelectorAll('tbody tr').length } : null,
      seiteQuer: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      blattKopf: name ? name.textContent.trim().slice(0, 40) : null
    };
  });

  const e = { start: await lies() };
  const kaestchen = seite.locator('.epos-katalog-liste tbody td.epos-spalte-kaestchen input');
  if (await kaestchen.count() >= 5) {
    await kaestchen.nth(1).check(); await schlaf(300);
    await kaestchen.nth(3).check(); await schlaf(300);
    await kaestchen.nth(4).check(); await schlaf(600);
    e.gewaehlt = await lies();
    if (f.breite >= 900) {
      await seite.locator('.epos-auswahlleiste .epos-auswahlleiste-knopf').first().click();
      await schlaf(800);
      e.vergleich = await lies();
      await seite.locator('.epos-auswahlleiste .epos-auswahlleiste-aufheben').click();
      await schlaf(500);
    }
  }
  if (f.breite < 900) {
    const auf = seite.locator('.epos-auswahlleiste .epos-nur-schmal');
    if (await auf.count()) {
      await auf.click(); await schlaf(600);
      e.blatt = await lies();
      const zu = seite.locator('.epos-stammblatt-zurliste');
      if (await zu.count()) { await zu.click(); await schlaf(600); e.zurueck = await lies(); }
    }
  }
  return e;
}

// ------------------------------------------------ STUFE 4 der Neuordnung
//  (Konzept Administrationsdialoge, V9 und V14): die einlesenden Verwaltungen
//  - Klimadaten und die drei Zeitreihen - im Stammblatt, das Einlesen als
//  Ueberlagerung hinter "Import...". Gemessen:
//    blatt  - das Bild der Gruppe Jahresverlauf bzw. Ganglinie steht GANZ im
//             Stammblatt (rechts nicht ueber dessen Inhalt hinaus), und der
//             Inhalt des Blatts rollt nicht quer. Schmal (unter 900 px) steht
//             das Blatt erst nach "Stammblatt ›" da - gemessen wird im
//             geoeffneten Blatt, danach fuehrt "‹ Liste" zurueck;
//    offen  - "Import..." (button.epos-importknopf, an der KLASSE gefunden: Der
//             Wirt laeuft in der Sprache des Rechners) oeffnet EINE Ueberlagerung
//             mit Titel und genau einem Kreuz, sie steht ganz im Fenster, rollt
//             nicht quer, und die Seite auch nicht;
//    zu     - ihr Kreuz schliesst sie wieder.
async function stufe4probe(seite, f) {
  const lies = () => seite.evaluate(() => {
    const r = el => {
      if (!el) return null;
      const b = el.getBoundingClientRect();
      return { links: +b.left.toFixed(1), oben: +b.top.toFixed(1), rechts: +b.right.toFixed(1),
               unten: +b.bottom.toFixed(1), breite: +b.width.toFixed(1), hoehe: +b.height.toFixed(1) };
    };
    const blatt = document.querySelector('.epos-katalog-stammblatt');
    const inhalt = document.querySelector('.epos-stammblatt-inhalt');
    const flaeche = [...document.querySelectorAll('.epos-stammblatt .epos-diagramm-svg-flaeche')]
      .find(e => e.getBoundingClientRect().width > 0.5);
    const svg = flaeche ? flaeche.querySelector('svg') : null;
    const ueb = document.querySelector('.epos-ueberlagerung');
    return {
      blatt: r(blatt), inhalt: r(inhalt), bild: r(flaeche), svg: r(svg),
      inhaltQuer: inhalt ? inhalt.scrollWidth - inhalt.clientWidth : null,
      seiteQuer: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      ueberlagerung: ueb ? {
        ...r(ueb),
        titel: ((ueb.querySelector('.epos-ueberlagerung-titel') || {}).textContent || '').trim(),
        kreuze: ueb.querySelectorAll('.epos-ueberlagerung-zu').length,
        quer: ueb.scrollWidth - ueb.clientWidth
      } : null,
      einlesen: document.querySelectorAll('.epos-einlesen').length
    };
  });

  const e = {};
  const schmal = f.breite < 900;
  if (schmal) {
    const auf = seite.locator('.epos-auswahlleiste .epos-nur-schmal');
    if (await auf.count()) { await auf.click(); await schlaf(800); }
  }
  await schlaf(600);
  e.blatt = await lies();
  if (schmal) {
    const zu = seite.locator('.epos-stammblatt-zurliste');
    if (await zu.count()) { await zu.click(); await schlaf(500); }
  }

  const knopf = seite.locator('button.epos-importknopf');
  if (await knopf.count()) {
    await knopf.click();
    await schlaf(800);
    e.offen = await lies();
    const kreuz = seite.locator('.epos-ueberlagerung .epos-ueberlagerung-zu');
    if (await kreuz.count()) {
      await kreuz.first().click();
      await schlaf(600);
      e.zu = await lies();
    }
  }
  return e;
}

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
                  (f.bilder === false ? '&bilder=0' : '') +
                  (f.art ? '&art=' + f.art : '') + (f.voll ? '&voll=1' : '');
  await seite.goto(adresse, { waitUntil: 'domcontentloaded' });

  // EIN PROJEKTDIALOG (Faelle P): kein Katalograhmen, nur die geerbte Katalogliste.
  // Gemessen wird, ob sie in ihrer Spalte steht (nicht auf null faellt - sie ist
  // seit V2 ein Container ihrer Breite) und wie weit sie quer rollt.
  if (f.projekt) {
    await seite.waitForSelector('.epos-katalogliste tbody tr', { timeout: 30000 });
    await schlaf(800);
    const p = await seite.evaluate(() => {
      const liste = document.querySelector('.epos-katalogliste');
      const huelle = liste.querySelector('.epos-raster-huelle');
      return {
        liste: +liste.getBoundingClientRect().width.toFixed(1),
        eltern: +liste.parentElement.getBoundingClientRect().width.toFixed(1),
        klasse: liste.className,
        quer: huelle.scrollWidth - huelle.clientWidth,
        innen: huelle.clientWidth,
        spalten: [...liste.querySelectorAll('thead th')].map(t =>
          ((t.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 24) || '[]') +
          (getComputedStyle(t).display === 'none' ? ' AUS' : ' ' + t.getBoundingClientRect().width.toFixed(0) + 'px'))
      };
    });
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${f.name}.png`, fullPage: false });
    await kontext.close();
    return { name: f.name, fall: f, adresse, projekt: p };
  }

  await seite.waitForSelector('.epos-katalog-dialog', { timeout: 30000 });


  // STILPROBE wie in der Rasterprobe: Ohne epos-ui.css gibt es keine
  // Hoechsthoehe, keine Rollbehaelter und damit auch keine Ueberlagerung -
  // der Lauf saehe gruen aus und haette nichts gemessen.
  // Gelesen wird der RAHMEN der Huelle und nicht mehr ihre Hoechsthoehe: Seit
  // Stufe 1 der Neuordnung nimmt die Liste im Katalogdialog die Resthoehe und
  // traegt KEINE Hoechsthoehe mehr (max-height: none) - der Rahmen aber steht
  // nur im Hausstilblatt (1px solid --epos-rahmen).
  const stil = await seite.evaluate(() => {
    const h = document.querySelector('.epos-raster-huelle');
    return h ? getComputedStyle(h).borderTopStyle : '(keine Huelle)';
  });
  if (stil !== 'solid') {
    throw new Error(`Das Stilblatt epos-ui.css wirkt nicht (Rahmen der Huelle: ${stil}). ` +
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
  // Seit Stufe 2 der Neuordnung (V4) ist die ZEILE die Wahl: Geklickt wird die
  // Klickflaeche des Namens (.epos-zeilenzelle--name) - die erste Zelle kann in einer
  // schmalen Liste weichen (Waermepumpe: Hersteller). Ein Wirt mit Wahlspalte
  // behaelt den Knopf.
  await schlaf(400);
  await seite.locator('.epos-katalog-liste tbody tr :is(.epos-zeilenzelle--name, button)').first()
             .click({ timeout: 15000 });
  await schlaf(1400);

  // Die Gegenproben setzen ihr altes Mass ERST NACH der Wahl der Zeile: Die Wahl
  // laedt nur den Eingabeblock, und im alten Mass liegt die erste Zeile unter dem
  // klebenden Spaltenkopf - der Klick traefe den Kopf.
  if (f.vorher || VORHER) await seite.addStyleTag({ content: VORHER_STIL });
  if (f.stil) await seite.addStyleTag({ content: f.stil });
  if (f.vorher || VORHER || f.stil) await schlaf(400);

  const k = await seite.evaluate(KAESTEN);
  if (f.stufe1) k.stufe1 = await seite.evaluate(STUFE1);
  if (f.stufe2) {
    k.stufe2 = await seite.evaluate(STUFE2);
    k.tasten = await tastenprobe(seite);
  }
  if (f.stufe3) k.stufe3 = await stufe3probe(seite, f);
  if (f.stufe4) k.stufe4 = await stufe4probe(seite, f);

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
  if (e.projekt) {
    // Die Liste fuellt ihre Spalte (hoechstens der Rand fehlt) - faellt sie als
    // Container auf ihre Eigenbreite null, ist das der Befund.
    if (e.projekt.liste < e.projekt.eltern - 4)
      m.push(`die Katalogliste ist ${e.projekt.liste} px breit in einer Spalte von ${e.projekt.eltern} px`);
    if (e.fall.querHoechstens !== undefined && e.projekt.quer > e.fall.querHoechstens)
      m.push(`die Liste rollt quer um ${e.projekt.quer} px`);
    return m;
  }

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

  // STUFE 1 (V1, V2): ein Rollbereich nie im anderen, der Dialog rollt nicht,
  // die Liste rollt nicht quer, die Zeile ist so hoch wie ItemSize.
  const s = e.stufe1;
  if (s) {
    if (s.fehler) { m.push(s.fehler); return m; }
    for (const v of s.verschachtelt) m.push(`Rollbereich in Rollbereich: ${v}`);
    if (s.dialogRollt) m.push(`der Dialog rollt (${s.dialogMass})`);
    if (s.querUeberlauf > 1) {
      const schuld = s.spalten.filter(sp => sp.sichtbar && sp.rechts > s.huelle.innen + 0.5)
                              .map(sp => `„${sp.text}" bis ${sp.rechts}`);
      m.push(`die Liste rollt quer um ${s.querUeberlauf} px (Innenbreite ${s.huelle.innen}); ` +
             `jenseits: ${schuld.join(', ') || '—'}`);
    }
    const soll = e.fall.zeile || 53;
    if (s.zeilenhoehe !== null && Math.abs(s.zeilenhoehe - soll) > 0.5)
      m.push(`Zeilenhoehe ${s.zeilenhoehe} px statt ${soll} (ItemSize)`);
    if (e.fall.bezeichnerKurz && s.bezeichner && s.bezeichner.gekuerzt && !s.bezeichner.titel)
      m.push('ein gekuerzter Bezeichner traegt keinen Kurztext (title)');
  }

  // STUFE 2 (V4, V10, V11): jede Zeile im Mass, keine Wahlspalte, das Schloss
  // ganz in seiner Zelle, die Tastatur waehlt und rollt mit.
  const z = e.stufe2;
  if (z) {
    if (z.fehler) { m.push(z.fehler); return m; }
    const soll = e.fall.zeile || 53;
    const falsch = z.hoehen.filter(h => Math.abs(h - soll) > 0.5);
    if (falsch.length) m.push(`Zeilen nicht im Mass ${soll} px (Umbruch?): ${falsch.join(', ')}`);
    if (!z.zeilenwahl) m.push('die Liste ist nicht im Modus "Zeile ist die Wahl"');
    if (z.wahlspalte) m.push(`noch ${z.wahlspalte} Elemente der Wahlspalte`);
    if (z.tabulatorhalt !== '0') m.push('die Liste ist kein Tabulatorhalt');
    if (z.gewaehlt !== 1) m.push(`${z.gewaehlt} Zeilen als gewaehlt markiert (soll 1)`);
    if (z.balken && (!z.balken.ersteZelle || z.balken.weitere))
      m.push(`der linke Balken der Fokuszeile steht falsch (${JSON.stringify(z.balken)})`);
    if (e.fall.schloss && !z.schloss) m.push('kein Schloss an einem Auslieferungssatz');
    if (z.schloss && (!z.schloss.inZelle || !z.schloss.sichtbar))
      m.push(`das Schloss steht nicht ganz in seiner Zelle (${JSON.stringify(z.schloss)})`);
    const t = e.tasten;
    if (t && t.fehler) m.push(t.fehler);
    else if (t) {
      const letzte = t.ende.zeilen - 1;
      if (t.ende.index !== letzte) m.push(`Ende waehlt Zeile ${t.ende.index} statt ${letzte}`);
      if (t.pos1.index !== 0) m.push(`Pos1 waehlt Zeile ${t.pos1.index} statt 0`);
      // Am Ende bleibt die Wahl stehen: bei einer Zeile (Solarganglinie) ist das die 0.
      const zwei = Math.min(2, t.runter2.zeilen - 1);
      if (t.runter2.index !== zwei) m.push(`zweimal Pfeil runter waehlt Zeile ${t.runter2.index} statt ${zwei}`);
      for (const [was, s2] of Object.entries(t)) {
        if (!s2.sichtbar) m.push(`nach „${was}" steht die gewaehlte Zeile nicht ganz im Bild`);
        if (!s2.fokus) m.push(`nach „${was}" hat die Liste den Fokus verloren`);
      }
      if (t.pos1.rollstand !== 0) m.push(`nach Pos1 rollt die Liste bei ${t.pos1.rollstand} px statt 0`);
    }
  }

  // STUFE 3 (V3, V6, V8, V12): das Stammblatt neben der Liste (breit) bzw. als
  // Blatt ueber ihr (schmal), die Liste mit ihrer ganzen Hoehe, die Kaestchen.
  const d = e.stufe3;
  if (d) {
    const s = d.start;
    if (s.seiteQuer > 0) m.push(`die Seite rollt quer um ${s.seiteQuer} px`);
    if (f3breit(e)) {
      if (!s.blatt || s.blatt.hoehe < 100) m.push('kein Stammblatt neben der Liste');
      else {
        if (s.blatt.links < s.liste.rechts - 0.5) m.push(`das Stammblatt (x ${s.blatt.links}) steht nicht rechts der Liste (bis ${s.liste.rechts})`);
        if (s.blatt.breite < 339.5 || s.blatt.breite > 440.5) m.push(`das Stammblatt ist ${s.blatt.breite} px breit (soll 340 … 440)`);
      }
      if (s.auswahl && s.auswahl.links < s.liste.rechts - 0.5) m.push('die Auswahlleiste steht nicht ueber dem Stammblatt');
    } else {
      if (s.blatt && s.blatt.hoehe > 0.5) m.push('schmal steht das Stammblatt schon beim Oeffnen ueber der Liste');
      if (!s.auswahl || s.auswahl.hoehe < 1) m.push('schmal fehlt die Auswahlleiste');
    }
    if (e.fall.mindestZeilen && s.ganzeZeilen < e.fall.mindestZeilen)
      m.push(`die Liste zeigt ${s.ganzeZeilen} ganze Zeilen (soll mindestens ${e.fall.mindestZeilen})`);
    if (d.gewaehlt) {
      if (!/^3\b/.test(d.gewaehlt.auswahlText || '')) m.push(`nach drei Kaestchen nennt die Leiste „${d.gewaehlt.auswahlText}"`);
      if (s.huelle && d.gewaehlt.huelle && Math.abs(d.gewaehlt.huelle.oben - s.huelle.oben) > 0.5)
        m.push(`die Liste springt mit den Kaestchen (${s.huelle.oben} → ${d.gewaehlt.huelle.oben})`);
    }
    if (d.vergleich) {
      const v = d.vergleich;
      if (!v.vergleich) m.push('„Vergleichen" zeigt keine Vergleichstabelle im Stammblatt');
      else if (v.blatt && v.vergleich.rechts > v.blatt.rechts + 0.5)
        m.push(`die Vergleichstabelle ragt ueber das Stammblatt (${v.vergleich.rechts} > ${v.blatt.rechts})`);
      if (v.vergleich && v.vergleich.quer > 1) m.push(`die Vergleichstabelle rollt quer um ${v.vergleich.quer} px`);
      if (v.seiteQuer > 0) m.push(`im Vergleich rollt die Seite quer um ${v.seiteQuer} px`);
    }
    if (d.blatt) {
      if (!d.blatt.blatt || d.blatt.blatt.hoehe < 100) m.push('„Stammblatt ›" zeigt kein Blatt');
      if (d.blatt.liste && d.blatt.liste.hoehe > 0.5) m.push('das Blatt steht neben statt ueber der Liste');
      if (!d.blatt.auswahl || d.blatt.auswahl.hoehe < 1) m.push('mit offenem Blatt fehlt die Auswahlleiste');
      if (d.zurueck && (!d.zurueck.liste || d.zurueck.liste.hoehe < 1 || (d.zurueck.blatt && d.zurueck.blatt.hoehe > 0.5)))
        m.push('„‹ Liste" fuehrt nicht zur Liste zurueck');
    } else if (!f3breit(e)) m.push('schmal fehlt der Knopf „Stammblatt ›"');
  }

  // STUFE 4 (V9, V14): das Bild passt in die Gruppe, das Blatt rollt nicht quer,
  // "Import..." oeffnet die Ueberlagerung mit Titel und einem Kreuz, das Kreuz
  // schliesst sie, nichts rollt quer.
  const v = e.stufe4;
  if (v) {
    const b = v.blatt;
    if (!b || !b.bild) m.push('kein Diagramm in der Gruppe des Stammblatts');
    else {
      if (b.inhalt && b.bild.rechts > b.inhalt.rechts + 0.5)
        m.push(`das Diagramm ragt ueber das Stammblatt (${b.bild.rechts} > ${b.inhalt.rechts})`);
      if (b.svg && b.svg.breite > b.bild.breite + 0.5)
        m.push(`das SVG ist breiter als seine Flaeche (${b.svg.breite} > ${b.bild.breite})`);
    }
    if (b && b.inhaltQuer > 1) m.push(`das Stammblatt rollt quer um ${b.inhaltQuer} px`);
    if (b && b.seiteQuer > 0) m.push(`mit dem Blatt rollt die Seite quer um ${b.seiteQuer} px`);
    const o = v.offen;
    if (!o || !o.ueberlagerung) m.push('„Import…" oeffnet keine Ueberlagerung');
    else {
      const u = o.ueberlagerung;
      if (!u.titel) m.push('die Ueberlagerung des Einlesens traegt keinen Titel');
      if (u.kreuze !== 1) m.push(`die Ueberlagerung traegt ${u.kreuze} Kreuze (soll 1)`);
      if (u.links < -0.5 || u.oben < -0.5 || u.rechts > e.fenster.breite + 0.5 || u.unten > e.fenster.hoehe + 0.5)
        m.push(`die Ueberlagerung ragt aus dem Fenster (${u.links},${u.oben})-(${u.rechts},${u.unten})`);
      if (u.quer > 1) m.push(`die Ueberlagerung rollt quer um ${u.quer} px`);
      if (o.seiteQuer > 0) m.push(`mit der Ueberlagerung rollt die Seite quer um ${o.seiteQuer} px`);
      if (o.einlesen !== 1) m.push('in der Ueberlagerung stehen die Felder des Einlesens nicht');
      if (!v.zu || v.zu.ueberlagerung || v.zu.einlesen) m.push('das Kreuz schliesst die Ueberlagerung nicht');
    }
  }

  return m;
}

// Steht der Rahmen breit (ab 900 px Rahmenbreite)? Der Rahmen ist das Fenster
// abzueglich 2 x 16 px Polsterung der Maske.
const f3breit = e => e.fenster.breite - 32 >= 900;

function zeige(e) {
  console.log('');
  console.log('=== ' + e.name + ' ===  ' + e.adresse);
  if (e.fehler) { console.log('  FEHLER: ' + e.fehler); return; }
  if (e.projekt) {
    const p = e.projekt;
    console.log(`  [Projektdialog] Katalogliste ${p.liste} px in ${p.eltern} px  (${p.klasse})` +
                `   Huelle innen ${p.innen} px, quer ${p.quer} px`);
    console.log('            Spalten: ' + p.spalten.join(' · '));
    return;
  }
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

  const s = e.stufe1;
  if (!s || s.fehler) return;
  const r = k => k ? `${k.breite}x${k.hoehe} @ (${k.links},${k.oben})` : '—';
  console.log(`  [Stufe 1] Dialog ${s.dialogMass}  rollt: ${s.dialogRollt ? 'JA' : 'nein'}`);
  console.log(`            Rahmen ${r(s.rahmen)}   Liste ${r(s.liste)}   Eingabe ${r(s.eingabe)}`);
  if (s.huelle)
    console.log(`            Huelle ${r(s.huelle)}  innen ${s.huelle.innen} / Inhalt ${s.huelle.inhalt} px quer, ` +
                `${s.huelle.innenHoehe} / ${s.huelle.inhaltHoehe} px hoch, max-height ${s.huelle.maxHoehe}` +
                `   Tabelle ${r(s.tabelle)}   Querueberlauf ${s.querUeberlauf} px`);
  console.log(`            Rollbereiche (${s.bereiche.length}): ` +
              (s.bereiche.map(b => `${b.name} [${b.hoch ? 'hoch' : ''}${b.quer ? ' quer' : ''} ${b.client} von ${b.roll}]`).join(' | ') || '—'));
  console.log(`            verschachtelt: ${s.verschachtelt.join(' | ') || 'keiner'}`);
  console.log(`            Spalten: ` + s.spalten.map(sp =>
    `${sp.text || '[]'} ${sp.sichtbar ? sp.breite + 'px→' + sp.rechts : 'AUS'}`).join(' · '));
  console.log(`            Zeilenhoehe ${s.zeilenhoehe}   Bezeichner: ` +
              (s.bezeichner ? `„${s.bezeichner.text}" gekuerzt=${s.bezeichner.gekuerzt} ` +
                              `title=${s.bezeichner.titel ? 'ja' : 'nein'} text-overflow=${s.bezeichner.textOverflow}` : '—'));
  const st2 = e.stufe2;
  if (!st2 || st2.fehler) return;
  console.log(`  [Stufe 2] Zeilenhoehen ${st2.hoehen.join(' | ')}   Wahlspalte ${st2.wahlspalte}   ` +
              `Tabulatorhalt ${st2.tabulatorhalt}   gewaehlt ${st2.gewaehlt}   ` +
              `Balken ${st2.balken ? (st2.balken.ersteZelle ? 'erste Zelle' : 'FEHLT') + (st2.balken.weitere ? ' +' + st2.balken.weitere : '') : '—'}   Schloss ` +
              (st2.schloss ? `${st2.schloss.breite}x${st2.schloss.hoehe} inZelle=${st2.schloss.inZelle}` : '—'));
  const t = e.tasten;
  if (t && !t.fehler)
    console.log('            Tasten: ' + Object.entries(t).map(([w, s2]) =>
      `${w} → Zeile ${s2.index}/${s2.zeilen} sichtbar=${s2.sichtbar} fokus=${s2.fokus} roll ${s2.rollstand}`).join(' · '));
  const v4 = e.stufe4;
  if (v4) {
    for (const [was, s4] of Object.entries(v4)) {
      if (!s4) continue;
      const u = s4.ueberlagerung;
      console.log(`  [Stufe 4] ${was.padEnd(6)} Blatt ${r(s4.blatt)}  Inhalt ${r(s4.inhalt)} quer ${s4.inhaltQuer}  ` +
                  `Bild ${r(s4.bild)}  SVG ${r(s4.svg)}  Seite quer ${s4.seiteQuer}` +
                  (u ? `  Ueberlagerung ${r(u)} „${u.titel}" Kreuze ${u.kreuze} quer ${u.quer}` : '  Ueberlagerung —'));
    }
  }
  const d = e.stufe3;
  if (!d) return;
  for (const [was, s3] of Object.entries(d)) {
    if (!s3) continue;
    console.log(`  [Stufe 3] ${was.padEnd(9)} Liste ${r(s3.liste)}  Auswahl ${r(s3.auswahl)} „${s3.auswahlText}"  ` +
                `Blatt ${r(s3.blatt)} „${s3.blattKopf}"  ganze Zeilen ${s3.ganzeZeilen}` +
                (s3.vergleich ? `  Vergleich ${r(s3.vergleich)} ${s3.vergleich.zeilen} Zeilen, quer ${s3.vergleich.quer}` : ''));
  }
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
  // DER RAHMEN MIT EINGABEBLOCK (seit Stufe 4): Klimadaten und Zeitreihen tragen
  // das Stammblatt, keine Verwaltung steht mehr "Liste oben, Eingabeblock darunter".
  // Die Seite "rahmen" stellt diese Anordnung des Katalograhmens nach - K4 misst
  // sie mit der Behebung von KL-5, G1 ohne.
  { name: 'K4_rahmen_mit_Eingabeblock', maske: 'rahmen',     zeilen: 35, breite: 1180, hoehe: 780 },
  // DIE GEGENPROBE: derselbe Rahmen mit dem Mass VOR KL-5. Sie MUSS den Befund
  // zeigen - sonst belegt die Behebung nichts.
  { name: 'G1_rahmen_vor_KL5',        maske: 'rahmen',       zeilen: 35, breite: 1180, hoehe: 780,
    vorher: true, mussFehlschlagen: true }
];

// STUFE 1 DER NEUORDNUNG (Konzept Administrationsdialoge, Abschnitt 7): alle
// sieben Komponenten im Katalograhmen, jede Auspraegung, im Fenstermass des
// Anwenders (1 088 x 624 CSS-Pixel = 85 % x 90 % von 1 920 x 1 040 bei 150 %)
// und schmal (400 x 624). Die Zeilenzahl ist die der Testdatenbank (Konzept
// 3.5), die Zeilen sind VOLL belegt (Zeilenbau.Voll). Die virtualisierte Liste
// im Rahmen (6 654 Stromspeicher) misst rasterprobe.mjs, Faelle J und K.
const STUFE1_MASKEN = [
  ['N01', 'heizkessel',       'browser',      'heizkessel',       63],
  ['N02', 'bhkw',             'browser',      'bhkw',             79],
  ['N03', 'solarkollektoren', 'browser',      'solarkollektoren', 7],
  ['N04', 'pufferspeicher',   'browser',      'pufferspeicher',   13],
  ['N05', 'pv',               'modul',        'photovoltaik',     35],
  ['N06', 'wechselrichter',   'modul',        'wechselrichter',   35],
  ['N07', 'stromspeicher',    'modul',        'stromspeicher',    35],
  ['N08', 'waermepumpe',      'waermepumpe',  '',                 51],
  ['N09', 'klima',            'klima',        '',                 32],
  ['N10', 'brauchwasser',     'bedarf',       'brauchwasser',     16],
  ['N11', 'prozesswaerme',    'bedarf',       'prozesswaerme',    32],
  ['N12', 'stromverbraucher', 'bedarf',       'stromverbraucher', 41],
  ['N13', 'waermebedarf',     'waermebedarf', '',                 4],
  ['N14', 'solarganglinie',   'solar',        '',                 1],
  ['N15', 'stromganglinie',   'stromganglinie', '',               3]
];
// DIE GEGENPROBE ZU STUFE 1: derselbe Heizkessel mit den Regeln von VOR Stufe 1
// - der Rahmen rollt in sich, die Liste traegt ihre Hoechsthoehe, der
// Eingabeblock rollt nicht selbst, die Liste ist kein Container (keine Spalte
// weicht) und der Bezeichner kuerzt nicht. Sie MUSS den Befund zeigen:
// Rollbereich in Rollbereich und waagerechten Ueberlauf.
const STUFE1_VORHER_STIL = `
  .epos-katalog-paar.epos-katalog-fuellend { display: grid !important;
    grid-template-rows: auto auto !important; overflow: auto !important; }
  .epos-katalog-paar > .epos-katalog-eingabe { max-height: none !important; overflow: visible !important; }
  .epos-katalog-liste .epos-katalogliste > .epos-raster-huelle { max-height: 457.6px !important;
    flex: 0 1 auto !important; }
  .epos-katalogliste--raenge { container-type: normal !important; }
  .epos-katalogliste table.epos-raster td.epos-spalte--bezeichner { max-width: none !important; }
`;
FAELLE.push({ name: 'G2_heizkessel_vor_Stufe1', maske: 'browser', art: 'heizkessel', zeilen: 63,
              breite: 1088, hoehe: 624, voll: true, stufe1: true, stil: STUFE1_VORHER_STIL,
              mussFehlschlagen: true });

// EIN PROJEKTDIALOG ALS NACHBAR (nicht Gegenstand der Neuordnung): Er erbt die
// Katalogliste samt Raengen. Er darf nicht schlechter werden - die Liste faellt
// nicht zusammen und rollt bei 1 088 px nicht quer.
FAELLE.push({ name: 'P2a_projekt_heizkessel_1088x624', maske: 'projekt-heizkessel', zeilen: 63,
              breite: 1088, hoehe: 624, projekt: true, querHoechstens: 1 });
FAELLE.push({ name: 'P2b_projekt_heizkessel_400x624', maske: 'projekt-heizkessel', zeilen: 63,
              breite: 400, hoehe: 624, projekt: true });

// STUFE 2 (V4, V10, V11): In den Verwaltungen ist die Zeile die Wahl - 46 px
// statt 53, ohne Wahlspalte, mit Schloss (Zeilenbau.Voll macht jede siebte Zeile
// zum Auslieferungssatz, die erste eingeschlossen) und mit Tastatur.
// STUFE 3 (V3, V6, V8, V12): die Verwaltungen mit Stammblatt. Bei 1 088 x 624
// muss die Liste mindestens ACHT ganze Zeilen zeigen - das war die Schwaeche
// von Stufe 1 (drei Zeilen neben dem Eingabeblock darunter).
// STUFE 4 (V9, V14): Klimadaten und die drei Zeitreihen tragen seit Stufe 4 das
// Stammblatt - sie kommen zur Menge der Stufe 3 und bekommen dazu die Messung des
// Bildes in der Gruppe und der Ueberlagerung des Einlesens.
const STUFE3 = new Set(['N01', 'N02', 'N03', 'N04', 'N05', 'N06', 'N07', 'N08',
                        'N09', 'N10', 'N11', 'N12', 'N13', 'N14', 'N15']);
const STUFE4 = new Set(['N09', 'N13', 'N14', 'N15']);
for (const [nr, name, maske, art, zeilen] of STUFE1_MASKEN) {
  const s3 = STUFE3.has(nr);
  const s4 = STUFE4.has(nr);
  FAELLE.push({ name: `${nr}a_${name}_1088x624`, maske, art, zeilen, breite: 1088, hoehe: 624,
                voll: true, stufe1: true, stufe2: true, zeile: 46, schloss: true, bezeichnerKurz: true,
                stufe3: s3, stufe4: s4, mindestZeilen: s3 && zeilen >= 8 ? 8 : 0 });
  FAELLE.push({ name: `${nr}b_${name}_400x624`, maske, art, zeilen, breite: 400, hoehe: 624,
                voll: true, stufe1: true, stufe2: true, zeile: 46, schloss: true, bezeichnerKurz: true,
                stufe3: s3, stufe4: s4 });
}

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
