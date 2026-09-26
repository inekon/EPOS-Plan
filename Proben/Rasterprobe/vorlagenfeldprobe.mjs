// =====================================================================
//  MARKENPROBE (BV-E6) — die Platzhaltermarke im echten Browser
// =====================================================================
//
//  WOZU. bunit misst weder Maß noch Überdeckung (EPOS.UI/CLAUDE.md). Die
//  Hausregeln der Marke (Konzept Berichtsvorlagen 9.7) sind aber Aussagen über
//  KÄSTEN: ein voller 44-px-Knopf, der weder den Titel seines Elements noch
//  eine andere Marke überdeckt; eine Aufklappung, die im Fenster bleibt; keine
//  Seite, die quer rollt. Diese Probe misst sie auf der Seite
//  /vorlagenfeldprobe des Wirtes (zehn Varianten mit echten Katalogschlüsseln)
//  in allen drei Stellungen bei 1 280 × 900.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node vorlagenfeldprobe.mjs --url http://127.0.0.1:5299 --fotos /tmp/fotos
//
//  Rückgabe 0 = kein Verstoß, 1 = mindestens einer, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/vorlagenfeldprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const BREITE = Number(arg('breite', '1280'));
const HOEHE = 900;

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };

const schneiden = (a, b) => a.x < b.x + b.width - 0.5 && b.x < a.x + a.width - 0.5
                          && a.y < b.y + b.height - 0.5 && b.y < a.y + a.height - 0.5;

let browser;
try {
  browser = await chromium.launch();
} catch (e) {
  console.error('Chromium startet nicht: ' + e.message);
  process.exit(2);
}

try {
  if (FOTOS) await mkdir(FOTOS, { recursive: true });
  const seite = await browser.newPage({ viewport: { width: BREITE, height: HOEHE } });

  for (const stellung of ['aus', 'marken', 'schluessel']) {
    const fall = stellung + '@' + BREITE;
    await seite.goto(WURZEL + '/vorlagenfeldprobe?kultur=de-DE&stellung=' + stellung, { waitUntil: 'networkidle' });
    await seite.waitForFunction(() => !!window.Blazor, null, { timeout: 15000 });
    await seite.waitForTimeout(400);

    // --- Stilblatt geladen? Ohne es misst die Probe nichts.
    const touch = await seite.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue('--epos-touchziel').trim());
    if (touch !== '44px') { melde(fall, 'Stilblatt fehlt (--epos-touchziel = "' + touch + '")'); continue; }

    const stand = await seite.evaluate(() => document.getElementById('vfp-stand')?.dataset);
    console.log(fall + ': ' + JSON.stringify(stand));

    // --- Der Umschalter: drei Stellungen, jede ≥ 44 × 44, genau eine an.
    const stellungen = await seite.$$eval('button.epos-vorlagenfeld-stellung', els => els.map(e => {
      const r = e.getBoundingClientRect();
      return { text: e.textContent, w: r.width, h: r.height, an: e.getAttribute('aria-checked') };
    }));
    if (stellungen.length !== 3) melde(fall, 'Umschalter mit ' + stellungen.length + ' Stellungen');
    for (const s of stellungen) if (s.w < 44 || s.h < 44) melde(fall, 'Stellung "' + s.text + '" ' + s.w + '×' + s.h);
    if (stellungen.filter(s => s.an === 'true').length !== 1) melde(fall, 'nicht genau eine Stellung an');

    const marken = await seite.$$eval('.vfp-traeger', els => els.map(t => {
      const knopf = t.querySelector('button.epos-vorlagenfeld-marke');
      const titel = t.querySelector('.vfp-titel');
      const bereich = document.createRange();
      bereich.selectNodeContents(titel);
      const tr = bereich.getBoundingClientRect();
      const kr = knopf ? knopf.getBoundingClientRect() : null;
      return {
        variante: t.dataset.variante, schluessel: t.dataset.schluessel,
        knopf: kr && { x: kr.x, y: kr.y, width: kr.width, height: kr.height },
        titel: { x: tr.x, y: tr.y, width: tr.width, height: tr.height },
        chip: knopf?.querySelector('.epos-vorlagenfeld-schluessel')?.textContent ?? null,
      };
    }));

    const quer = await seite.evaluate(() => document.scrollingElement.scrollWidth - window.innerWidth);
    if (quer > 0) melde(fall, 'die Seite rollt quer um ' + quer + ' px');

    if (stellung === 'aus') {
      const dom = await seite.$$eval('.epos-vorlagenfeld', els => els.length);
      if (dom !== 0) melde(fall, dom + ' Marken im DOM, obwohl ausgeschaltet');
      if (await seite.$('.epos-vorlagenfeld-zeile')) melde(fall, 'leise Zeile steht, obwohl ausgeschaltet');
      if (FOTOS) await seite.screenshot({ path: FOTOS + '/vfp-' + fall + '.png', fullPage: true });
      continue;
    }

    if (marken.length !== 10) melde(fall, marken.length + ' Varianten statt 10');
    for (const m of marken) {
      if (!m.knopf) { melde(fall, 'Variante ' + m.variante + ' (' + m.schluessel + ') ohne Marke'); continue; }
      if (m.knopf.width < 44 || m.knopf.height < 44)
        melde(fall, 'Marke ' + m.variante + ' nur ' + m.knopf.width.toFixed(1) + '×' + m.knopf.height.toFixed(1));
      if (schneiden(m.knopf, m.titel)) melde(fall, 'Marke ' + m.variante + ' überdeckt den Titel');
      if (m.knopf.x < 0 || m.knopf.x + m.knopf.width > BREITE + 0.5) melde(fall, 'Marke ' + m.variante + ' ragt aus dem Fenster');
      if (stellung === 'schluessel' && m.chip !== m.schluessel)
        melde(fall, 'Chip ' + m.variante + ' zeigt "' + m.chip + '" statt "' + m.schluessel + '"');
    }
    for (let i = 0; i < marken.length; i++)
      for (let j = i + 1; j < marken.length; j++)
        if (marken[i].knopf && marken[j].knopf && schneiden(marken[i].knopf, marken[j].knopf))
          melde(fall, 'Marken ' + marken[i].variante + ' und ' + marken[j].variante + ' überschneiden sich');

    if (stellung === 'schluessel') {
      const zeile = await seite.$eval('.epos-vorlagenfeld-zeile-text', e => e.textContent).catch(() => '');
      if (!/10 Platzhalter/.test(zeile)) melde(fall, 'leise Zeile: "' + zeile.trim() + '"');
    }

    if (FOTOS) await seite.screenshot({ path: FOTOS + '/vfp-' + fall + '.png', fullPage: true });

    if (stellung === 'marken') {
      // --- Überfahren öffnet, die Aufklappung bleibt im Fenster; Klick heftet an, Esc löst.
      for (const nr of ['1', '6', '10']) {
        const traeger = '.vfp-traeger[data-variante="' + nr + '"]';
        await seite.hover(traeger + ' button.epos-vorlagenfeld-marke');
        await seite.waitForTimeout(80);
        const auf = await seite.$eval(traeger + ' .epos-vorlagenfeld-aufklappung', e => {
          const r = e.getBoundingClientRect();
          return { sichtbar: getComputedStyle(e).display !== 'none', x: r.x, rechts: r.right, breite: r.width };
        });
        if (!auf.sichtbar) melde(fall, 'Überfahren von ' + nr + ' öffnet keine Aufklappung');
        else if (auf.x < 0 || auf.rechts > BREITE + 0.5)
          melde(fall, 'Aufklappung ' + nr + ' ragt aus dem Fenster (' + auf.x.toFixed(0) + '…' + auf.rechts.toFixed(0) + ')');
        if (FOTOS && nr === '10') await seite.screenshot({ path: FOTOS + '/vfp-' + fall + '-ueberfahren.png' });
      }

      await seite.mouse.move(5, HOEHE - 5);
      const t6 = '.vfp-traeger[data-variante="6"]';
      await seite.click(t6 + ' button.epos-vorlagenfeld-marke');
      await seite.waitForSelector(t6 + ' .epos-vorlagenfeld--offen', { timeout: 3000 }).catch(() => melde(fall, 'Klick heftet nicht an'));
      await seite.mouse.move(5, HOEHE - 5);
      const offen = await seite.$eval(t6 + ' .epos-vorlagenfeld-aufklappung', e => getComputedStyle(e).display !== 'none');
      if (!offen) melde(fall, 'angeheftete Aufklappung verschwindet beim Wegfahren');

      // Kopieren ohne Zwischenablage: der Text steht markiert im Feld.
      await seite.click(t6 + ' .epos-vorlagenfeld-kopieren');
      await seite.waitForSelector(t6 + ' textarea.epos-vorlagenfeld-auf-textfeld', { timeout: 3000 })
        .catch(() => melde(fall, 'ohne Zwischenablage erscheint kein Feld'));
      await seite.waitForTimeout(200);
      const markiert = await seite.$eval(t6 + ' textarea.epos-vorlagenfeld-auf-textfeld',
        e => ({ text: e.value, von: e.selectionStart, bis: e.selectionEnd })).catch(() => null);
      if (!markiert || markiert.text !== '{{projekt.kunde}}' || markiert.von !== 0 || markiert.bis !== markiert.text.length)
        melde(fall, 'Text nicht markiert: ' + JSON.stringify(markiert));
      if (FOTOS) await seite.screenshot({ path: FOTOS + '/vfp-' + fall + '-angeheftet.png' });

      await seite.keyboard.press('Escape');
      await seite.waitForTimeout(150);
      if (await seite.$(t6 + ' .epos-vorlagenfeld--offen')) melde(fall, 'Esc löst die Aufklappung nicht');
    }
  }

  // =====================================================================
  //  DIE WIRTE (Seite /vorlagenfeldwirte): Kennzahlkachel, DiagrammSvg und
  //  Vergleichstabelle je ohne und mit Vorlagenfeld. "Aus" verschiebt nichts
  //  (alle Hoehen links = rechts); die Tabellenzeilen sind in jeder Stellung
  //  gleich hoch; die Marke ueberdeckt weder Titel noch Leiste.
  // =====================================================================
  const wseite = await browser.newPage({ viewport: { width: BREITE, height: HOEHE } });
  let zeilenAus = null;
  for (const stellung of ['aus', 'marken', 'schluessel']) {
    const fall = 'wirte-' + stellung + '@' + BREITE;
    await wseite.goto(WURZEL + '/vorlagenfeldwirte?kultur=de-DE&stellung=' + stellung, { waitUntil: 'networkidle' });
    await wseite.waitForFunction(() => !!window.Blazor, null, { timeout: 15000 });
    await wseite.waitForTimeout(600);

    const mass = await wseite.$$eval('.vfw-spalte', spalten => spalten.map(sp => {
      const rel = (el, bezug) => {
        if (!el) return null;
        const r = el.getBoundingClientRect(), b = bezug.getBoundingClientRect();
        return { y: +(r.y - b.y).toFixed(2), h: +r.height.toFixed(2), x: r.x, width: r.width, abs: { x: r.x, y: r.y, width: r.width, height: r.height } };
      };
      const text = el => { if (!el) return null; const b = document.createRange(); b.selectNodeContents(el); const r = b.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; };
      const wirt = n => sp.querySelector('[data-wirt="' + n + '"]');
      const k = wirt('kachel'), d = wirt('diagramm'), t = wirt('tabelle');
      const knopf = w => { const e = w.querySelector('button.epos-vorlagenfeld-marke'); if (!e) return null; const r = e.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; };
      return {
        mit: sp.dataset.mit,
        kachel: { hoehe: +k.getBoundingClientRect().height.toFixed(2), titel: rel(k.querySelector('.epos-kennzahlkachel-titel'), k),
                  wert: rel(k.querySelector('.epos-kennzahlkachel-wert'), k), titeltext: text(k.querySelector('.epos-kennzahlkachel-titel')), knopf: knopf(k) },
        diagramm: { hoehe: +d.getBoundingClientRect().height.toFixed(2), leiste: rel(d.querySelector('.epos-diagramm-leiste'), d),
                    bild: rel(d.querySelector('svg'), d), knopf: knopf(d),
                    leistenknoepfe: [...d.querySelectorAll('.epos-diagramm-leiste button')].map(b => { const r = b.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; }) },
        tabelle: { hoehe: +t.getBoundingClientRect().height.toFixed(2), tabelle: rel(t.querySelector('table'), t),
                   zeilen: [...t.querySelectorAll('table tr')].map(z => +z.getBoundingClientRect().height.toFixed(2)), knopf: knopf(t) },
      };
    }));
    const ohne = mass.find(m => m.mit === '0'), mit = mass.find(m => m.mit === '1');
    if (!ohne || !mit) { melde(fall, 'Wirteseite unvollständig'); continue; }
    const gleich = (a, b) => Math.abs(a - b) <= 0.5;
    console.log(fall + ': Kachel ' + ohne.kachel.hoehe + '/' + mit.kachel.hoehe + ' px, Diagramm ' + ohne.diagramm.hoehe + '/' + mit.diagramm.hoehe
                + ' px, Tabellenzeilen ' + JSON.stringify(ohne.tabelle.zeilen) + ' / ' + JSON.stringify(mit.tabelle.zeilen));

    // Zeilenhoehe der Vergleichstabelle: mit = ohne, und in jeder Stellung wie in "Aus".
    if (ohne.tabelle.zeilen.length !== mit.tabelle.zeilen.length || ohne.tabelle.zeilen.some((h, i) => !gleich(h, mit.tabelle.zeilen[i])))
      melde(fall, 'Tabellenzeilen mit Marke ' + JSON.stringify(mit.tabelle.zeilen) + ' statt ' + JSON.stringify(ohne.tabelle.zeilen));
    if (stellung === 'aus') zeilenAus = mit.tabelle.zeilen;
    else if (zeilenAus && mit.tabelle.zeilen.some((h, i) => !gleich(h, zeilenAus[i])))
      melde(fall, 'Tabellenzeilen ' + JSON.stringify(mit.tabelle.zeilen) + ' statt wie in „Aus“ ' + JSON.stringify(zeilenAus));

    if (stellung === 'aus') {
      // Keine Layoutverschiebung: alle Lagen und Hoehen wie ohne Vorlagenfeld.
      const paare = [
        ['Kachel', ohne.kachel.hoehe, mit.kachel.hoehe], ['Kacheltitel', ohne.kachel.titel?.y, mit.kachel.titel?.y],
        ['Kachelwert', ohne.kachel.wert?.y, mit.kachel.wert?.y], ['Diagramm', ohne.diagramm.hoehe, mit.diagramm.hoehe],
        ['Leiste', ohne.diagramm.leiste?.y, mit.diagramm.leiste?.y], ['Leistenhöhe', ohne.diagramm.leiste?.h, mit.diagramm.leiste?.h],
        ['Diagrammbild', ohne.diagramm.bild?.y, mit.diagramm.bild?.y], ['Tabellenwirt', ohne.tabelle.hoehe, mit.tabelle.hoehe],
        ['Tabelle', ohne.tabelle.tabelle?.y, mit.tabelle.tabelle?.y],
      ];
      for (const [name, a, b] of paare)
        if (a == null || b == null || !gleich(a, b)) melde(fall, name + ' verschiebt sich in „Aus“: ' + a + ' → ' + b);
      for (const w of ['kachel', 'diagramm', 'tabelle']) if (mit[w].knopf) melde(fall, w + ': Marke sichtbar, obwohl ausgeschaltet');
    } else {
      for (const w of ['kachel', 'diagramm', 'tabelle']) {
        const kn = mit[w].knopf;
        if (!kn) { melde(fall, w + ' ohne Marke'); continue; }
        if (kn.width < 44 || kn.height < 44) melde(fall, w + ': Marke nur ' + kn.width.toFixed(1) + '×' + kn.height.toFixed(1));
        if (kn.x + kn.width > BREITE + 0.5) melde(fall, w + ': Marke ragt aus dem Fenster');
      }
      if (mit.kachel.knopf && mit.kachel.titeltext && schneiden(mit.kachel.knopf, mit.kachel.titeltext)) melde(fall, 'Marke überdeckt den Kacheltitel');
      for (const b of mit.diagramm.leistenknoepfe)
        if (mit.diagramm.knopf && schneiden(mit.diagramm.knopf, b)) melde(fall, 'Marke überdeckt einen Knopf der Zoomleiste');
    }
    const quer = await wseite.evaluate(() => document.scrollingElement.scrollWidth - window.innerWidth);
    if (quer > 0) melde(fall, 'die Seite rollt quer um ' + quer + ' px');
    if (FOTOS) await wseite.screenshot({ path: FOTOS + '/vfw-' + fall + '.png', fullPage: true });
  }
} finally {
  await browser.close();
}

console.log(verstoesse.length === 0 ? 'MARKENPROBE: kein Verstoß' : 'MARKENPROBE: ' + verstoesse.length + ' Verstöße');
process.exit(verstoesse.length === 0 ? 0 : 1);
