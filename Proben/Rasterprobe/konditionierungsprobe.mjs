// =====================================================================
//  KONDITIONIERUNGSPROBE (Stufe KP2, Welle U0b) — der Gebäude-Katalogeditor
//  in der BREITEN Überlagerung, im echten Browser
// =====================================================================
//
//  WOZU. Der Editor steht in jeder Betriebsart in der breiten Überlagerung
//  (Entwurf KP2, Festlegung 7), und das Wochenraster ordnet sich nach der
//  Breite seines BEHÄLTERS an (Container-Abfrage). Beides sind Aussagen über
//  KÄSTEN; bunit hat kein Layout (EPOS.UI/CLAUDE.md). Diese Probe misst auf der
//  Seite /konditionierungsprobe des Wirtes je Fall und je Breite
//  (390 × 844, 820 × 1 180, 1 180 × 820, 1 300 × 900):
//    - kein Querrollen: scrollWidth ≤ clientWidth für Seite, Überlagerung und
//      JEDES Reiterblatt (jeder Reiter wird angewählt);
//    - die Überlagerung ganz im Fenster, ihre Breite min(96vw, 1 400 px), und
//      beim Öffnen stehen ihr Titel und ihr Kreuz im Bild (nicht weggerollt);
//    - Bedienziele ≥ 44 × 44 px (Knopf, Feld, Klappliste, Reiter; ein
//      Kästchen zählt mit seiner Beschriftung; die Hilfepille hat das
//      Hausmaß 28 px und wird nur genannt);
//    - im Reiterblatt überdeckt kein Bedienziel ein anderes und keines ragt
//      aus dem Blatt (Reiter 1 darf in der breiten Überlagerung nicht leiden);
//    - Esc und ✕ schließen die Überlagerung, Esc auch aus einem Feld heraus;
//    - je Fall das, was ihn ausmacht: zwei Zonen im Reiter „Zonen“ (projekt),
//      Infiltration und Nutzerlüftung leer mit Platzhalter neben der
//      Luftwechselrate (gesamt), Grundzeile, weich gesperrtes OK samt Grund und
//      freies „Speichern unter“ (gesperrt; OK meldet den Grund und schreibt
//      nichts), kein „Speichern unter“ (neu), die Anordnung des Wochenrasters
//      je Behälterbreite samt sechs Zellen „aus“, im breitesten Fenster auch
//      an den Schwellen 1 150/1 149/600/599 px, jede Zelle ≥ 44 px (bausteine).
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node konditionierungsprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--nur <fall>] [--kultur de-DE]
//
//  Rückgabe 0 = kein Verstoß, 1 = mindestens einer, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

// Playwright liegt in dieser Umgebung GLOBAL (npm -g) - derselbe Weg wie in
// katalogprobe.mjs.
const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/konditionierungsprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const KULTUR = arg('kultur', 'de-DE');

const FAELLE = ['projekt', 'gesamt', 'gesperrt', 'neu', 'bausteine'];
const FENSTER = [
  { breite: 390, hoehe: 844 },     // Telefon hochkant
  { breite: 820, hoehe: 1180 },    // Tablet hochkant
  { breite: 1180, hoehe: 820 },    // Tablet quer
  { breite: 1300, hoehe: 900 }     // Arbeitsplatz
];
const ZIEL = 44;                   // --epos-touchziel
const TOL = 0.5;

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('    VERSTOSS: ' + text); };
const schlaf = ms => new Promise(r => setTimeout(r, ms));

// --------------------------------------------------- Messung im Browser
// Gemessen wird in der Überlagerung; das Reiterblatt ist das sichtbare
// .epos-reiter-blatt (nur das aktive steht im Baum).
const MESSEN = ([ziel, tol]) => {
  const r = el => { const b = el.getBoundingClientRect();
    return { l: +b.left.toFixed(1), o: +b.top.toFixed(1), r: +b.right.toFixed(1), u: +b.bottom.toFixed(1),
             b: +b.width.toFixed(1), h: +b.height.toFixed(1) }; };
  const quer = el => el ? el.scrollWidth - el.clientWidth : null;
  const ueb = document.querySelector('.epos-ueberlagerung');
  if (!ueb) return { fehler: 'keine .epos-ueberlagerung im Baum' };
  const blatt = ueb.querySelector('.epos-reiter-blatt');
  const dialog = ueb.querySelector('.epos-ueberlagerung-inhalt > .epos-dialog');

  // Die Bedienziele: sichtbar, mit Fläche; ein Kästchen oder Knopf der Wahl
  // zählt mit seiner Beschriftung (die ist das Ziel, das der Finger trifft).
  const WAHL = new Set(['checkbox', 'radio']);
  const ziele = [...ueb.querySelectorAll('button, input:not([type=hidden]), select, textarea, a[href], [role=tab], summary')]
    .map(el => {
      const traeger = el.tagName === 'INPUT' && WAHL.has(el.type) && el.closest('label') ? el.closest('label') : el;
      return { el, traeger, m: r(traeger) };
    })
    .filter(z => z.m.b > tol && z.m.h > tol && getComputedStyle(z.el).visibility !== 'hidden');
  const name = z => {
    const t = (z.el.getAttribute('aria-label') || z.el.getAttribute('title') || z.el.innerText || z.el.value
               || z.el.getAttribute('placeholder') || '').replace(/\s+/g, ' ').trim().slice(0, 32);
    return `${z.el.tagName.toLowerCase()}${z.el.type && z.el.tagName === 'INPUT' ? '[' + z.el.type + ']' : ''}` +
           `.${(z.el.className || '').toString().split(' ').filter(Boolean).slice(0, 2).join('.')} „${t}“`;
  };
  // AUSNAHME: die Hilfepille im Dialogkopf (Fragezeichen und Assistent) hat
  // das Hausmaß --epos-infoknopf (28 px, Vorbild InfoKnopf 28 × 28) - eine
  // Festlegung des Hauses, nicht dieses Editors. Sie wird gezählt und
  // genannt, aber nicht als Verstoß gewertet.
  const pille = z => z.el.classList.contains('epos-hilfepille__feld');
  const klein = ziele.filter(z => !pille(z) && (z.m.b < ziel - tol || z.m.h < ziel - tol))
    .map(z => `${name(z)} ${z.m.b}×${z.m.h}`);
  const pillen = ziele.filter(pille).map(z => `${z.m.b}×${z.m.h}`);

  // Im Reiterblatt: Überdeckung zweier Ziele und Ziele, die aus dem Blatt
  // ragen. Ein Ziel in einem eigenen Rollbehälter (Wochenraster des Bestands,
  // Rasterhülle) zählt beim Herausragen nicht - dort rollt der Behälter.
  const imBlatt = blatt ? ziele.filter(z => blatt.contains(z.el)) : [];
  const ueberdeckt = [];
  for (let i = 0; i < imBlatt.length; i++)
    for (let j = i + 1; j < imBlatt.length; j++) {
      const a = imBlatt[i], b = imBlatt[j];
      if (a.traeger.contains(b.traeger) || b.traeger.contains(a.traeger)) continue;
      const w = Math.min(a.m.r, b.m.r) - Math.max(a.m.l, b.m.l);
      const h = Math.min(a.m.u, b.m.u) - Math.max(a.m.o, b.m.o);
      if (w > 1 && h > 1) ueberdeckt.push(`${name(a)} / ${name(b)} (${w.toFixed(0)}×${h.toFixed(0)})`);
    }
  const rollt = el => { for (let v = el.parentElement; v && v !== blatt; v = v.parentElement)
                          if (getComputedStyle(v).overflowX !== 'visible') return true; return false; };
  const mb = blatt ? r(blatt) : null;
  const heraus = mb ? imBlatt.filter(z => !rollt(z.traeger) && (z.m.l < mb.l - tol || z.m.r > mb.r + tol))
                              .map(z => `${name(z)} ${z.m.l}…${z.m.r}`) : [];

  const aktiv = ueb.querySelector('[role=tab][aria-selected=true]');
  return {
    stilblatt: getComputedStyle(ueb).position,
    fenster: { b: window.innerWidth, h: window.innerHeight },
    seiteQuer: quer(document.documentElement),
    ueb: { ...r(ueb), quer: quer(ueb) },
    dialog: dialog ? r(dialog) : null,
    blatt: blatt ? { ...mb, quer: quer(blatt) } : null,
    reiter: aktiv ? aktiv.innerText.replace(/\s+/g, ' ').trim() : '',
    ziele: ziele.length, zieleBlatt: imBlatt.length, klein, pillen, ueberdeckt, heraus
  };
};

// Fall "bausteine": die Anordnung des Wochenrasters aus den Kästen seiner
// Zellen - Zeilen je Tag (verschiedene Oberkanten der Montagszellen) und
// Zellen je Zeile; dazu die Zellen "aus" und die Gemeinjahrdaten.
const BAUSTEINE = ([ziel, tol, breite]) => {
  const raster = document.querySelector('.epos-wochenraster--umbrechend');
  if (!raster) return { fehler: 'kein .epos-wochenraster--umbrechend' };
  // Die Schwellen: Der Behälter bekommt eine feste Breite (1 150/1 149/600/599 px),
  // die Abfrage greift beim erzwungenen Layout der ersten Messung.
  raster.style.width = breite ? breite + 'px' : '';
  const montag = raster.querySelector('tbody tr');
  const zellen = [...montag.querySelectorAll('td .epos-eingabe')].map(e => e.getBoundingClientRect());
  const oben = [...new Set(zellen.map(b => Math.round(b.top)))];
  const alle = [...raster.querySelectorAll('td .epos-eingabe')].map(e => e.getBoundingClientRect());
  const aus = [...raster.querySelectorAll('td .epos-eingabe.epos-eingabe--aus')];
  const datum = [...document.querySelectorAll('.epos-gemeinjahrdatum .epos-eingabe')];
  return {
    behaelter: raster.clientWidth,
    zeilenJeTag: oben.length,
    zellenJeZeile: oben.length ? zellen.filter(b => Math.round(b.top) === oben[0]).length : 0,
    kleinsteZelle: alle.length ? `${Math.min(...alle.map(b => b.width)).toFixed(1)}×${Math.min(...alle.map(b => b.height)).toFixed(1)}` : '',
    zuKlein: alle.filter(b => b.width < ziel - tol || b.height < ziel - tol).length,
    zellen: alle.length,
    aus: aus.length,
    ausText: [...new Set(aus.map(e => e.value))].join('|'),
    rasterQuer: raster.scrollWidth - raster.clientWidth,
    datum: datum.map(e => ({ wert: e.value, h: +e.getBoundingClientRect().height.toFixed(1) })),
    saison: (document.querySelector('#probe-saison') || {}).innerText || ''
  };
};

// ------------------------------------------------------------ Ein Fall
async function fall(browser, name, f) {
  const marke = `${name} ${f.breite}×${f.hoehe}`;
  console.log(`\n${marke}`);
  const kontext = await browser.newContext({ viewport: { width: f.breite, height: f.hoehe }, deviceScaleFactor: 1 });
  const seite = await kontext.newPage();
  const fehler = [];
  seite.on('pageerror', e => fehler.push(e.message));
  seite.on('console', m => { if (m.type() === 'error') fehler.push(m.text()); });

  await seite.goto(`${WURZEL}/konditionierungsprobe?fall=${name}&kultur=${KULTUR}`, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector('.epos-ueberlagerung', { timeout: 30000 });
  await schlaf(800);

  // --- beim Öffnen stehen Titel und Kreuz der Überlagerung im Bild (der Fokus
  // des Dialogs darf den Bereich nicht rollen)
  const kopf = await seite.evaluate(() => {
    const ueb = document.querySelector('.epos-ueberlagerung');
    const zu = ueb.querySelector('.epos-ueberlagerung-zu');
    const u = ueb.getBoundingClientRect(), z = zu ? zu.getBoundingClientRect() : null;
    return { gerollt: ueb.scrollTop, zu: !!zu,
             imBild: z ? z.top >= u.top - 0.5 && z.bottom <= u.bottom + 0.5 : false,
             titel: ((ueb.querySelector('.epos-ueberlagerung-titel') || {}).innerText || '').trim() };
  });
  console.log(`  Beim Öffnen: Titel „${kopf.titel}“, Kreuz ${kopf.imBild ? 'im Bild' : 'außerhalb'} (gerollt ${kopf.gerollt} px)`);
  if (!kopf.zu || !kopf.imBild) melde(marke, `Titel und Kreuz stehen beim Öffnen nicht im Bild (gerollt ${kopf.gerollt} px)`);

  // --- je Reiter (bausteine: ein Blatt ohne Reiter)
  const reiter = await seite.locator('.epos-ueberlagerung [role=tab]').count();
  const stufen = Math.max(reiter, 1);
  let erstes = null;
  for (let i = 0; i < stufen; i++) {
    if (reiter) { await seite.locator('.epos-ueberlagerung [role=tab]').nth(i).click(); await schlaf(400); }
    const m = await seite.evaluate(MESSEN, [ZIEL, TOL]);
    if (m.fehler) { melde(marke, m.fehler); break; }
    if (!erstes) erstes = m;
    const zeile = `  ${reiter ? 'Reiter ' + (i + 1) + ' „' + m.reiter + '“' : 'Blatt'}: ` +
      `Überlagerung ${m.ueb.b} px (quer ${m.ueb.quer}), Dialog ${m.dialog ? m.dialog.b : '—'} px, ` +
      `Blatt ${m.blatt ? m.blatt.b + ' px (quer ' + m.blatt.quer + ')' : '—'}, Seite quer ${m.seiteQuer}, ` +
      `Ziele ${m.ziele} (im Blatt ${m.zieleBlatt}), unter 44: ${m.klein.length}` +
      (m.pillen.length ? ` (Hilfepille ${m.pillen.join(', ')} ausgenommen)` : '') +
      `, überdeckt: ${m.ueberdeckt.length}, heraus: ${m.heraus.length}`;
    console.log(zeile);
    if (m.stilblatt !== 'fixed') melde(marke, 'Stilblatt nicht geladen (Überlagerung ' + m.stilblatt + ')');
    if (m.seiteQuer > 0) melde(marke, `die Seite rollt quer (${m.seiteQuer} px)`);
    if (m.ueb.quer > 0) melde(marke, `die Überlagerung rollt quer (${m.ueb.quer} px)`);
    if (m.blatt && m.blatt.quer > 0) melde(marke, `Reiter ${i + 1}: das Blatt rollt quer (${m.blatt.quer} px)`);
    if (m.ueb.l < -TOL || m.ueb.r > m.fenster.b + TOL) melde(marke, `die Überlagerung ragt aus dem Fenster (${m.ueb.l}…${m.ueb.r})`);
    const soll = Math.min(0.96 * f.breite, 1400);
    if (Math.abs(m.ueb.b - soll) > 1) melde(marke, `die Überlagerung ist ${m.ueb.b} px breit (soll min(96vw, 1400) = ${soll.toFixed(1)})`);
    for (const k of m.klein) melde(marke, `Reiter ${i + 1}: Bedienziel unter 44 px: ${k}`);
    for (const u of m.ueberdeckt) melde(marke, `Reiter ${i + 1}: Bedienziele überdecken sich: ${u}`);
    for (const h of m.heraus) melde(marke, `Reiter ${i + 1}: Bedienziel ragt aus dem Blatt: ${h}`);
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_${reiter ? 'reiter' + (i + 1) : 'blatt'}.png` });
  }
  if (reiter) { await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300); }

  // --- was den Fall ausmacht
  if (name === 'projekt') {
    const zonen = seite.locator('.epos-ueberlagerung [role=tab]').last();
    await zonen.click(); await schlaf(400);
    const z = await seite.evaluate(() => [...document.querySelectorAll('.epos-zonenliste tbody tr')]
      .map(t => t.innerText.replace(/\s+/g, ' ').trim().slice(0, 40)));
    console.log(`  Zonen: ${z.length} (${z.join(' | ')})`);
    if (z.length !== 2) melde(marke, `${z.length} statt 2 Zonen im Reiter „Zonen“`);
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
  }
  if (name === 'gesamt') {
    // Die Gesamtangabe: Infiltration und Nutzerlüftung leer (ohne Platzhalter -
    // es gilt die Luftwechselrate), die Herleitungszeile nennt den wirksamen
    // Luftwechsel 0,60 aus der Luftwechselrate.
    const l = await seite.evaluate(() => {
      const eingabe = text => {
        const x = [...document.querySelectorAll('.epos-ueberlagerung label.epos-feld')]
          .find(f => ((f.querySelector('.epos-feld-text') || {}).innerText || '').trim() === text);
        const e = x ? x.querySelector('input') : null;
        return e ? { wert: e.value, platzhalter: e.getAttribute('placeholder') || '' } : null;
      };
      const zeile = [...document.querySelectorAll('.epos-ueberlagerung .epos-herleitung-text')]
        .map(e => e.innerText.trim()).find(t => t.includes('0,60')) || '';
      return { rate: eingabe('Luftwechselrate :'), infiltration: eingabe('Infiltration :'),
               nutzer: eingabe('Nutzerlüftung :'), zeile };
    });
    console.log(`  Lüftung: Luftwechselrate „${l.rate?.wert}“, Infiltration „${l.infiltration?.wert}“, ` +
                `Nutzerlüftung „${l.nutzer?.wert}“; Herleitung „${l.zeile}“`);
    if (!l.rate || l.rate.wert !== '0,6') melde(marke, 'die Luftwechselrate steht nicht als 0,6 da');
    for (const [n, e] of [['Infiltration', l.infiltration], ['Nutzerlüftung', l.nutzer]])
      if (!e || e.wert !== '') melde(marke, `${n} ist nicht leer`);
    if (!l.zeile) melde(marke, 'keine Herleitungszeile mit dem wirksamen Luftwechsel 0,60');
  }
  if (name === 'gesperrt' || name === 'neu') {
    const s = await seite.evaluate(() => {
      const ueb = document.querySelector('.epos-ueberlagerung');
      const zeile = ueb.querySelector('.epos-gebk-sperrzeile');
      const leisten = [...ueb.querySelectorAll('.epos-leiste')];
      const leiste = leisten[leisten.length - 1];
      const ok = leiste ? leiste.querySelector('.epos-knopf--primaer') : null;
      const unter = leiste ? [...leiste.querySelectorAll('button')].find(b => b.innerText.includes('Speichern unter')) : null;
      return {
        zeile: zeile ? zeile.innerText.replace(/\s+/g, ' ').trim() : null,
        schloss: zeile ? !!zeile.querySelector('.epos-schloss') : false,
        okGesperrt: ok ? ok.getAttribute('aria-disabled') : null,
        okGrund: ok ? ok.getAttribute('title') || '' : '',
        okDisabled: ok ? ok.disabled : null,
        unter: unter ? { gesperrt: unter.getAttribute('aria-disabled'), disabled: unter.disabled } : null
      };
    });
    console.log(`  Grundzeile: ${s.zeile === null ? '—' : '„' + s.zeile + '“'}; OK aria-disabled=${s.okGesperrt} ` +
                `disabled=${s.okDisabled}; „Speichern unter“: ${s.unter ? 'frei' : 'fehlt'}`);
    if (name === 'gesperrt') {
      if (!s.zeile || !s.schloss || !s.zeile.includes('Speichern unter')) melde(marke, 'die Grundzeile samt Schloss fehlt');
      if (s.okGesperrt !== 'true' || s.okDisabled || s.okGrund !== s.zeile) melde(marke, 'OK ist nicht weich gesperrt mit dem Grund am Knopf');
      if (!s.unter || s.unter.gesperrt || s.unter.disabled) melde(marke, '„Speichern unter“ ist nicht frei');
      // Der Versuch meldet den Grund, schreibt nichts, und die Überlagerung bleibt stehen.
      // force: Playwright hält aria-disabled für "nicht bedienbar" - die weiche
      // Sperre IST aber anklickbar, genau das ist ihr Sinn.
      const leisten = seite.locator('.epos-ueberlagerung .epos-leiste');
      await leisten.last().locator('.epos-knopf--primaer').click({ force: true }); await schlaf(500);
      const v = await seite.evaluate(() => ({
        offen: !!document.querySelector('.epos-ueberlagerung'),
        banner: [...document.querySelectorAll('.epos-ueberlagerung .epos-warnbanner-text')].map(e => e.innerText.trim()),
        gespeichert: !!document.querySelector('#probe-gespeichert')
      }));
      console.log(`  OK-Versuch: Überlagerung ${v.offen ? 'steht' : 'zu'}, Meldung ${v.banner.length ? '„' + v.banner[0].slice(0, 40) + '…“' : '—'}, ` +
                  `geschrieben: ${v.gespeichert ? 'ja' : 'nein'}`);
      if (!v.offen || v.gespeichert || !v.banner.some(t => t === s.zeile)) melde(marke, 'der OK-Versuch meldet den Grund nicht oder schreibt');
      if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_ok-versuch.png` });
    } else {
      if (s.zeile !== null) melde(marke, 'Grundzeile ohne Sperre');
      if (s.okGesperrt === 'true' || s.okDisabled) melde(marke, 'OK ist gesperrt');
      if (s.unter) melde(marke, '„Speichern unter“ im Modus Neu');
    }
  }
  const anordnung = b => b.behaelter >= 1150 ? [1, 24, '7 × 24'] : b.behaelter >= 600 ? [2, 12, '2 × 12'] : [4, 6, '4 × 6'];
  if (name === 'bausteine' && f.breite >= 1300) {
    // An den Schwellen selbst (nur im breitesten Fenster ist Platz für 1 150 px).
    const s = [];
    for (const w of [1150, 1149, 600, 599]) {
      const b = await seite.evaluate(BAUSTEINE, [ZIEL, TOL, w]);
      const soll = anordnung(b);
      s.push(`${b.behaelter} px → ${b.zeilenJeTag} × ${b.zellenJeZeile}, kleinste Zelle ${b.kleinsteZelle}`);
      if (b.zeilenJeTag !== soll[0] || b.zellenJeZeile !== soll[1]) melde(marke, `Schwelle ${w} px: ${b.zeilenJeTag} × ${b.zellenJeZeile} statt ${soll[2]}`);
      if (b.zuKlein) melde(marke, `Schwelle ${w} px: ${b.zuKlein} Zellen unter 44 × 44 px (kleinste ${b.kleinsteZelle})`);
      if (b.rasterQuer > 0) melde(marke, `Schwelle ${w} px: das Wochenraster rollt quer (${b.rasterQuer} px)`);
    }
    await seite.evaluate(BAUSTEINE, [ZIEL, TOL, 0]);
    console.log('  Schwellen: ' + s.join('; '));
  }
  if (name === 'bausteine') {
    const b = await seite.evaluate(BAUSTEINE, [ZIEL, TOL, 0]);
    if (b.fehler) melde(marke, b.fehler);
    else {
      const soll = anordnung(b);
      console.log(`  Wochenraster: Behälter ${b.behaelter} px → ${b.zeilenJeTag} Zeile(n) je Tag × ${b.zellenJeZeile} Zellen ` +
                  `(soll ${soll[2]}), kleinste Zelle ${b.kleinsteZelle}, ${b.zellen} Zellen, „aus“: ${b.aus} (${b.ausText}), ` +
                  `quer ${b.rasterQuer}; Gemeinjahrdatum ${b.datum.map(d => d.wert + ' ' + d.h + ' px').join(', ')}; ${b.saison}`);
      if (b.zeilenJeTag !== soll[0] || b.zellenJeZeile !== soll[1]) melde(marke, `Wochenraster ${b.zeilenJeTag} × ${b.zellenJeZeile} statt ${soll[2]}`);
      if (b.zuKlein) melde(marke, `${b.zuKlein} Zellen unter 44 × 44 px`);
      if (b.zellen !== 168) melde(marke, `${b.zellen} statt 168 Zellen`);
      if (b.aus !== 6 || b.ausText !== 'aus') melde(marke, `„aus“: ${b.aus} Zellen (${b.ausText}) statt 6`);
      if (b.rasterQuer > 0) melde(marke, `das Wochenraster rollt quer (${b.rasterQuer} px)`);
      if (b.datum.map(d => d.wert).join(' ') !== '01.10. 30.04.') melde(marke, 'Gemeinjahrdatum zeigt nicht 01.10. und 30.04.');
      if (b.datum.some(d => d.h < ZIEL - TOL)) melde(marke, 'ein Gemeinjahrdatum ist unter 44 px hoch');
    }
  }

  // --- Esc, ✕ und Esc aus einem Feld schließen
  const zu = async (wie, tun) => {
    if (!(await seite.locator('.epos-ueberlagerung').count())) {
      await seite.click('#probe-oeffnen');
      await seite.waitForSelector('.epos-ueberlagerung', { timeout: 10000 });
      await schlaf(400);
    }
    await tun();
    await schlaf(500);
    const e = await seite.evaluate(() => ({
      offen: !!document.querySelector('.epos-ueberlagerung'),
      ausgang: (document.querySelector('#probe-ausgang') || {}).dataset?.ausgang || ''
    }));
    if (e.offen) melde(marke, `${wie} schließt die Überlagerung nicht`);
    return `${wie} → ${e.offen ? 'offen' : 'zu (' + e.ausgang + ')'}`;
  };
  const wege = [];
  wege.push(await zu('Esc', () => seite.keyboard.press('Escape')));
  wege.push(await zu('✕', () => seite.locator('.epos-ueberlagerung .epos-ueberlagerung-zu').click()));
  wege.push(await zu('Esc im Feld', async () => {
    const feld = seite.locator('.epos-ueberlagerung input.epos-eingabe').first();
    await feld.focus();
    await seite.keyboard.press('Escape');
  }));
  console.log('  Schließen: ' + wege.join('; '));

  if (fehler.length) melde(marke, 'Fehler im Browser: ' + fehler.slice(0, 3).join(' | '));
  await kontext.close();
  return erstes;
}

// ------------------------------------------------------------ Ablauf
let browser;
try {
  if (FOTOS) await mkdir(FOTOS, { recursive: true });
  browser = await chromium.launch({ headless: true });
  for (const name of FAELLE) {
    if (NUR && name !== NUR) continue;
    for (const f of FENSTER) await fall(browser, name, f);
  }
} catch (e) {
  console.error('AUFBAUFEHLER: ' + (e && e.message ? e.message : e));
  if (browser) await browser.close();
  process.exit(2);
}
await browser.close();
console.log('');
console.log(verstoesse.length === 0 ? 'KONDITIONIERUNGSPROBE: kein Verstoß.' : `KONDITIONIERUNGSPROBE: ${verstoesse.length} Verstöße.`);
process.exit(verstoesse.length === 0 ? 0 : 1);
