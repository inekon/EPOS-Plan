// =====================================================================
//  FENSTERPROBE - KOPF UND FUSS STEHEN (Anwenderentscheid 30.09.2026)
// =====================================================================
//
//  WOZU. Ein Dialog im eigenen Fenster (BlazorDialogForm) rollt als DOKUMENT.
//  Seit "Kopf+Fuss fest" haften Kopfzeile und Schlussleiste am Fenster, nur
//  der Inhalt dazwischen rollt (epos-ui.css, Abschnitt "Dialog im eigenen
//  Fenster", Baustein Fenstermarke). bunit misst weder Lage noch Rollstand -
//  diese Probe misst sie auf der Seite /fensterprobe des Wirtes, bei
//  1 088 x 624 CSS-Pixeln (Fenstermass des Anwenders) und 520 x 624
//  (Fenstermass.MindestBreite).
//
//  JE FENSTERDIALOG (Heizkessel, BHKW, Waermepumpen, Gebaeude, Dublettenpruefung):
//    - die Seite steht wie in der WebView2: body > #app > .epos-dialog, die
//      Fenstermarke in #app hinter dem Dialog; der Dialog ist hoeher als das Fenster;
//    - Rollstand oben, Mitte, Ende: der Kopf oben buendig (top 0), die
//      Schlussleiste unten buendig (bottom = Fensterhoehe), beide ueber die
//      volle Breite der Wurzel;
//    - am Ende steht alles, was im Markup vor der Leiste kommt, voll ueber ihr;
//    - keine andere Leiste haftet (Kostenknoepfe, Loeschzeile ...), genau ein
//      haftender Kopf;
//    - der Tabulator durch den ganzen Dialog: jedes angesprungene Feld steht
//      zwischen Kopf und Fuss; ein Knopf in Kopf oder Fuss rollt nichts;
//    - Waermepumpen: OK mit anschlagender Temperaturpruefung - das Warnband
//      der eingebetteten Detailansicht steht sichtbar UEBER der Leiste.
//    - Dublettenpruefung (Protokoll UNTER der Schlussleiste): oben haftet der Fuss
//      am Fensterrand, am Ende steht er ganz im Bild und das Protokoll frei darunter.
//  WP-STECKBRIEF (WP1a, --nur wp-steckbrief, 1 280 x 800 und 1 194 x 834): die Satz-Ueberlagerung der
//    Waermepumpe mit dem Geraetesteckbrief links (kein Querrollen, keine Ueberlappung, Arbeitspunkt folgt dem
//    Vorlauf) und die Konfiguration mit zugeklappten Betriebszeiten; Gegenprobe: ein zu breiter Steckbrief.
//  UEBERLAGERUNG (Gebaeude -> "Simulation..."): Die Ueberlagerung rollt nicht als
//    Ganzes, genau ihr Rollkoerper rollt (geb3); die Fussleiste des eingebetteten
//    Dialogs steht am Boden der Ueberlagerung, oben wie nach dem Rollen, sein Kopf
//    haftet NICHT, Kopf und Fuss des Fensters liegen unter der Abdunkelung, das
//    Dokument rollt nicht - und jede Zahl ist dieselbe wie ohne Fenstermarke.
//  KATALOGDIALOG (Bedarfsverwaltung): Kopf und Fuss statisch, das Dokument
//    rollt nicht, jede Zahl dieselbe wie ohne Fenstermarke.
//
//  IPAD 11 ZOLL (1 194 x 834 quer): dieselben Faelle MIT den sicheren Abstaenden
//    des iPads (Statusleiste oben 24 px, Home-Anzeige unten 20 px, gesetzt ueber die
//    Token --epos-sicher-oben/-unten des Themas, nicht ueber env()). Dort haftet der
//    Kopf unter dem oberen Abstand (top 24) und die Schlussleiste ueber dem unteren
//    (bottom = Fensterhoehe - 20); Katalogauswahl, Katalogdialog und Ueberlagerung
//    stehen ganz zwischen beiden. Gegenprobe: dasselbe Fenster ohne die Token muss rot
//    werden.
//
//  LIZENZBANNER (Fall „lizenzbanner"): das Banner der AppWurzel (Baustein Lizenzbanner,
//    Lesemodus-Satz, bricht im schmalen Fenster um) steht in #app VOR einem Dialog voller
//    Hoehe, ohne Fenstermarke wie in der AppWurzel auf iOS - je Katalogdialog (Bedarf) und
//    Katalogauswahl (Heizkessel). Das Dokument rollt nicht (scrollHeight = clientHeight),
//    das Banner steht oben, der Kopf des Dialogs darunter, die Schlussleiste im Fenster.
//    Gegenprobe: dasselbe mit --epos-banner-oben: 0px !important muss rot werden.
//
//  MELDUNGSLISTE (Fall „meldungsliste", Seite ?meldung=liste): ein langer Dialog im eigenen
//    Fenster, dessen Warnbanner (unmittelbares Kind der Wurzel) die Hinweisliste AUFGEKLAPPT
//    zeigt - 30 Hinweise, mehr als der Rollbereich (.epos-hinweisklappe-inhalt, hoechstens 40 %
//    der Fensterhoehe) fasst. Bei Rollstand oben, Mitte und Ende: Kopf und Schlussleiste haften
//    wie bei jedem Fensterdialog; das Banner steht ganz zwischen Kopf und Leiste, ab Mitte
//    unmittelbar unter dem Kopf; die Liste steht in ihm, ist hoechstens 40 % der Fensterhoehe hoch,
//    rollt (scrollHeight > clientHeight) und ist unverdeckt (elementFromPoint); ausser ihr rollt
//    nur das Dokument, kein Dialogkoerper. Das Mausrad ueber der Liste rollt die Liste bis an
//    ihr Ende, das Dokument bleibt stehen, Kopf, Banner und Leiste bleiben an Ort.
//    Dann der Tabulator von oben durch alle Felder: jedes angesprungene Feld steht ganz
//    sichtbar unter dem aufgeklappten Banner und ueber der Schlussleiste - das Polster
//    (scroll-padding-top) rechnet die gemessene Bannerhoehe (--epos-meldung-hoehe).
//    Gegenprobe: ohne Hoehengrenze der Liste, mit position: static am Banner, ohne
//    Fenstermarke und mit dem Token auf eine Bannerzeile muss die Messung rot werden.
//
//  GEGENPROBE (laeuft mit, abschaltbar mit --ohne-gegenprobe): dieselben
//  Fensterdialoge OHNE Fenstermarke (?marke=0) muessen die Haft-Kriterien
//  verfehlen, und MIT Marke, aber ohne scroll-padding muss der Tabulator
//  mindestens ein Feld unter Kopf oder Fuss legen. Bleibt die Gegenprobe
//  gruen, misst die Probe nichts.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node fensterprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>] [--nur <fall>] [--ohne-gegenprobe]
//
//  Rueckgabe 0 = kein Verstoss und Gegenprobe rot, 1 = Verstoss oder Gegenprobe
//  gruen, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/fensterprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const MIT_GEGENPROBE = !process.argv.includes('--ohne-gegenprobe');
const TOL = 1;   // px - Rollstaende sind gebrochen (Unterpixel)

const FENSTER = [{ breite: 1088, hoehe: 624 }, { breite: 520, hoehe: 624 }, { breite: 1194, hoehe: 834, ipad: true }];
/** WP1a: die Fenster des Geraetesteckbriefs (Desktop 1280 x 800, iPad quer 1194 x 834). */
const WP_FENSTER = [{ breite: 1280, hoehe: 800 }, { breite: 1194, hoehe: 834, ipad: true }];
// Sichere Abstaende des iPads (Punkte = CSS-Pixel); die Probe setzt die Token des Themas.
const SICHER = { oben: 24, unten: 20 };
const SICHER_TOKEN = `:root { --epos-sicher-oben: ${SICHER.oben}px; --epos-sicher-unten: ${SICHER.unten}px; }`;
const rand = f => f.ipad ? SICHER : { oben: 0, unten: 0 };
const FENSTERDIALOGE = ['dubletten'];
// Projektdialoge mit Katalogauswahl (Baustein Zweispaltenauswahl, Konzept Projektdialoge mit
// Katalogauswahl 4.1): Sie fuellen ihr Fenster, die Haftregel ist fuer sie gegenstandslos. Die
// Probe misst hier "nichts rollt ausser den Listen und der Detailzeile".
const KATALOGAUSWAHL = ['heizkessel', 'bhkw', 'waermepumpen', 'gebaeude', 'kaeltemaschine'];
// UeS2: Wirte mit Zusammenfassung - ihre Detailzeile hat keinen Vorrang (DZ1 entfaellt).
const ZUSAMMENFASSUNG = new Set(['heizkessel', 'bhkw', 'waermepumpen', 'photovoltaik', 'solarkollektoren', 'kaeltemaschine', 'bedarfsprofile', 'gebaeude']);   // Gebäude: Stufe 4
// Dialoge mit Inhalt UNTER der Schlussleiste (Protokoll der Dublettenpruefung): Am Ende steht
// der Fuss an seinem Platz ueber dem Nachlauf, nicht am Fensterrand - und er ueberdeckt ihn nicht.
const NACHLAUF = new Set(['dubletten']);

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };
const zahl = v => Math.round(v * 10) / 10;

// ---------------------------------------------------------------------------
//  Messungen in der Seite
// ---------------------------------------------------------------------------

/** Kopf, haftende Leiste und die Randdaten zum aktuellen Rollstand. */
function lage() {
  const d = document.querySelector('body > #app > .epos-dialog');
  const kopf = d && d.firstElementChild && d.firstElementChild.matches('.epos-dialog-kopf') ? d.firstElementChild : null;
  const fussKandidaten = d ? [...d.children].filter(e => e.matches('.epos-leiste, .epos-dialog-fuss')) : [];
  const fuss = fussKandidaten.find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss')) || null;
  const r = e => { if (!e) return null; const b = e.getBoundingClientRect(); return { top: b.top, bottom: b.bottom, left: b.left, right: b.right, hoehe: b.height }; };
  let nach = fuss ? fuss.nextElementSibling : null;
  while (nach && (nach.hidden || getComputedStyle(nach).position === 'fixed' || getComputedStyle(nach).display === 'none')) nach = nach.nextElementSibling;
  return {
    y: scrollY, innen: innerHeight, dokument: document.documentElement.scrollHeight,
    wurzel: r(d), kopf: r(kopf), fuss: r(fuss), nachfolger: r(nach),
    kopfPos: kopf ? getComputedStyle(kopf).position : '', fussPos: fuss ? getComputedStyle(fuss).position : '',
  };
}

async function rollen(seite, y) {
  await seite.evaluate(y => window.scrollTo(0, y), y);
  await seite.evaluate(() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))));
  return seite.evaluate(lage);
}

/**
 * Was im Markup vor der Schlussleiste steht und Flaeche hat: der tiefste Boden. Ein
 * Element in einem eigenen Rollbehaelter (Listenhuelle) zaehlt nicht - es ist dort
 * abgeschnitten; der Behaelter selbst zaehlt.
 */
function inhaltsboden() {
  const d = document.querySelector('body > #app > .epos-dialog');
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
  const imRollbehaelter = e => {
    for (let p = e.parentElement; p && p !== d; p = p.parentElement)
      if (getComputedStyle(p).overflowY !== 'visible') return true;
    return false;
  };
  let boden = -Infinity, wer = '';
  for (const e of d.querySelectorAll('*')) {
    if (fuss.contains(e) || e.contains(fuss)) continue;
    if (!(fuss.compareDocumentPosition(e) & Node.DOCUMENT_POSITION_PRECEDING)) continue;
    if (e.closest('.epos-ueberlagerung, .epos-ueberlagerung-hintergrund')) continue;
    const s = getComputedStyle(e);
    if (s.position === 'fixed' || s.visibility === 'hidden' || imRollbehaelter(e)) continue;
    const b = e.getBoundingClientRect();
    if (b.height <= 0 || b.width <= 0) continue;
    if (b.bottom > boden) { boden = b.bottom; wer = e.tagName.toLowerCase() + '.' + String(e.className).split(' ')[0]; }
  }
  return { boden, wer };
}

/** Haftende Elemente ausser Kopf und Schlussleiste des Fensters. */
function fremdeHafter() {
  const d = document.querySelector('body > #app > .epos-dialog');
  const kopf = d.firstElementChild;
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
  const leisten = [...d.querySelectorAll('.epos-leiste')].filter(e => e !== fuss && !fuss.contains(e)
    && getComputedStyle(e).position === 'sticky');
  const koepfe = [...document.querySelectorAll('.epos-dialog-kopf')].filter(e => getComputedStyle(e).position === 'sticky');
  return { leisten: leisten.map(e => e.className), koepfe: koepfe.length, kopfIstErster: kopf.matches('.epos-dialog-kopf') };
}

/** Ein Tabulatorschritt: wo steht das Ziel, gehoert es zu Kopf oder Fuss. */
function fokusLage() {
  const a = document.activeElement;
  const d = document.querySelector('body > #app > .epos-dialog');
  if (!a || !d || !d.contains(a) || a === d) return { draussen: true };
  const kopf = d.firstElementChild;
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
  const b = a.getBoundingClientRect();
  return {
    y: scrollY, top: b.top, bottom: b.bottom, flaeche: b.width > 0 && b.height > 0,
    rahmen: kopf.contains(a) || fuss.contains(a),
    nachFuss: !!(fuss.compareDocumentPosition(a) & Node.DOCUMENT_POSITION_FOLLOWING) && !fuss.contains(a),
    // Einen SCHREIBGESCHUETZTEN Textbereich rollt Chromium beim Fokus nicht ins Bild (ein
    // bearbeitbarer rollt) - mit und ohne die Regel. Gemessen wird deshalb nur, ob Kopf oder
    // Fuss verdecken, was von ihm im Fenster steht.
    nurLesenText: a.tagName === 'TEXTAREA' && a.readOnly,
    kopfBoden: kopf.getBoundingClientRect().bottom, fussDecke: fuss.getBoundingClientRect().top,
    fussBoden: fuss.getBoundingClientRect().bottom, innen: innerHeight,
    wer: a.tagName.toLowerCase() + '.' + String(a.className).split(' ')[0],
  };
}

// ---------------------------------------------------------------------------
//  Die Faelle
// ---------------------------------------------------------------------------

async function oeffnen(browser, fall, f, zusatz = '') {
  const seite = await browser.newPage({ viewport: { width: f.breite, height: f.hoehe } });
  await seite.goto(`${WURZEL}/fensterprobe?fall=${fall}&kultur=de-DE${zusatz}`);
  if (f.ipad && !f.ohneToken) await seite.addStyleTag({ content: SICHER_TOKEN });
  await seite.waitForSelector('body > #app > .epos-dialog', { timeout: 20000 });
  // iPad: Die Dublettenpruefung ist bei 834 px Fensterhoehe kuerzer als das Fenster und rollt
  // nicht; ein Fuellblock von 600 px unter dem Kopf macht sie lang, damit Haften messbar ist.
  if (f.ipad && fall === 'dubletten') await seite.evaluate(() => {
    const d = document.querySelector('body > #app > .epos-dialog');
    const fueller = document.createElement('div');
    fueller.className = 'ipad-fueller';
    fueller.style.cssText = 'height: 600px; flex: 0 0 auto';
    d.insertBefore(fueller, d.firstElementChild.nextSibling);
  });
  await seite.waitForTimeout(800);
  return seite;
}

/** Die Haft-Kriterien; liefert die Zahl der Verstoesse (fuer die Gegenprobe). */
async function haften(seite, name, melden, nachlauf = false, r = { oben: 0, unten: 0 }) {
  let n = 0;
  const fehler = t => { n++; if (melden) melde(name, t); };
  const oben = await rollen(seite, 0);
  if (!oben.kopf || !oben.fuss) { fehler('Kopf oder Schlussleiste fehlt'); return { n, zahlen: null }; }
  if (oben.dokument <= oben.innen + TOL) fehler(`der Dialog rollt nicht (${oben.dokument} <= ${oben.innen} px) - die Probe misst nichts`);
  const ende = await rollen(seite, 1e6);
  const mitte = await rollen(seite, Math.round(ende.y / 2));
  for (const [stand, m] of [['oben', oben], ['Mitte', mitte], ['Ende', ende]]) {
    if (nachlauf) {
      // Der Fuss steht ganz im Bild zwischen Kopf und Fensterrand - am Fensterrand, solange
      // sein Platz darunter laege, sonst an seinem Platz ueber dem Nachlauf ...
      if (m.fuss.top < (stand === 'oben' ? r.oben : m.kopf.bottom) - 0.5 || m.fuss.bottom > m.innen - r.unten + TOL)
        fehler(`${stand}: Fussleiste ${zahl(m.fuss.top)}..${zahl(m.fuss.bottom)} px nicht ganz im Bild (Kopf bis ${zahl(m.kopf.bottom)})`);
      // ... und am Ende steht der Nachlauf frei darunter.
      if (stand === 'Ende' && (!m.nachfolger || m.nachfolger.top < m.fuss.bottom - 0.5 || m.nachfolger.bottom > m.innen - r.unten + TOL))
        fehler(`Ende: Nachlauf ${m.nachfolger ? zahl(m.nachfolger.top) + '..' + zahl(m.nachfolger.bottom) : 'fehlt'} px, Fussleiste bis ${zahl(m.fuss.bottom)} px - ueberdeckt oder abgeschnitten`);
    } else {
      if (Math.abs(m.fuss.bottom - (m.innen - r.unten)) > TOL) fehler(`${stand}: Fussleiste endet bei ${zahl(m.fuss.bottom)} statt ${m.innen - r.unten} px (Rollstand ${zahl(m.y)})`);
      if (m.fuss.top >= m.innen) fehler(`${stand}: Fussleiste ausserhalb des Bildes`);
    }
    if (stand !== 'oben' && Math.abs(m.kopf.top - r.oben) > TOL) fehler(`${stand}: Kopf steht bei ${zahl(m.kopf.top)} statt ${r.oben} px`);
    if (m.kopf.top < r.oben - TOL) fehler(`${stand}: Kopf beginnt bei ${zahl(m.kopf.top)} px, unter dem oberen Abstand ${r.oben} px`);
    if (Math.abs(m.kopf.left - m.wurzel.left) > TOL || Math.abs(m.fuss.right - m.wurzel.right) > TOL)
      fehler(`${stand}: Kopf/Fuss nicht ueber die volle Breite der Wurzel`);
  }
  if (oben.kopfPos !== 'sticky') fehler('Kopf haftet nicht (position ' + oben.kopfPos + ')');
  if (oben.fussPos !== 'sticky') fehler('Fussleiste haftet nicht (position ' + oben.fussPos + ')');
  await rollen(seite, 1e6);
  const boden = await seite.evaluate(inhaltsboden);
  if (boden.boden > ende.fuss.top + 0.5) fehler(`am Ende steht ${boden.wer} bis ${zahl(boden.boden)} px unter der Leiste (ab ${zahl(ende.fuss.top)} px)`);
  return { n, zahlen: { kopf: zahl(oben.kopf.hoehe), fuss: zahl(oben.fuss.hoehe), dokument: oben.dokument, rollweg: zahl(ende.y),
                        boden: zahl(boden.boden), fussDecke: zahl(ende.fuss.top), fussBoden: zahl(ende.fuss.bottom),
                        nachlauf: ende.nachfolger ? [zahl(ende.nachfolger.top), zahl(ende.nachfolger.bottom)] : null } };
}

/** Der Tabulator durch den Dialog; liefert Schritte, verdeckte Felder und Spruenge. */
async function tabulator(seite) {
  await rollen(seite, 0);
  await seite.evaluate(() => document.querySelector('body > #app > .epos-dialog').focus({ preventScroll: true }));
  const erg = { schritte: 0, verdeckt: [], spruenge: [], angeschnitten: 0, ungerollt: 0 };
  for (let i = 0; i < 120; i++) {
    const vorher = await seite.evaluate(() => scrollY);
    await seite.keyboard.press('Tab');
    await seite.waitForTimeout(40);
    const m = await seite.evaluate(fokusLage);
    if (m.draussen) break;
    if (!m.flaeche) continue;
    erg.schritte++;
    if (m.rahmen) { if (Math.abs(m.y - vorher) > TOL) erg.spruenge.push(`${m.wer} rollt ${zahl(vorher)} -> ${zahl(m.y)}`); continue; }
    const oben = m.nachFuss ? m.fussBoden : m.kopfBoden, unten = m.nachFuss ? m.innen : m.fussDecke;
    if (m.nurLesenText) {
      const imFenster = Math.min(m.bottom, m.innen) - Math.max(m.top, 0);
      const frei = Math.min(m.bottom, unten) - Math.max(m.top, oben);
      if (imFenster <= 0) erg.ungerollt++;
      else if (frei < Math.min(20, imFenster) - 0.5)
        erg.verdeckt.push(`${m.wer} (schreibgeschuetzt) ${zahl(m.top)}..${zahl(m.bottom)}, im Fenster ${zahl(imFenster)} px, frei ${zahl(frei)} px`);
      else if (m.top < oben - 0.5 || m.bottom > unten + 0.5) erg.angeschnitten++;
      continue;
    }
    if (m.nachFuss) {   // der Nachlauf unter der Schlussleiste
      if (m.top < m.fussBoden - 0.5 || m.bottom > m.innen + 0.5)
        erg.verdeckt.push(`${m.wer} ${zahl(m.top)}..${zahl(m.bottom)} unter der Leiste (bis ${zahl(m.fussBoden)}, Fenster ${m.innen})`);
      continue;
    }
    if (m.top < m.kopfBoden - 0.5 || m.bottom > m.fussDecke + 0.5)
      erg.verdeckt.push(`${m.wer} ${zahl(m.top)}..${zahl(m.bottom)} (Kopf bis ${zahl(m.kopfBoden)}, Fuss ab ${zahl(m.fussDecke)})`);
  }
  return erg;
}

/**
 * Waermepumpen: OK bei Rollstand 0 mit anschlagender Temperaturpruefung. Das Band des Wirts
 * steht oben im Bild; das Band im Fussblock der eingebetteten Detailansicht steht, solange
 * die Detailansicht im Bild ist, UEBER der Schlussleiste des Fensters und frei (nicht verdeckt).
 */
async function warnband(seite, name, melden = true) {
  let n = 0;
  const melde_ = t => { n++; if (melden) melde(name, t); };
  await rollen(seite, 0);
  await seite.evaluate(() => {
    const d = document.querySelector('body > #app > .epos-dialog');
    const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer'));
    fuss.querySelector(':scope > .epos-knopf--primaer').click();
  });
  const ok = await seite.waitForSelector('.epos-dialog--eingebettet > .epos-dialog-fuss .epos-warnbanner', { timeout: 5000 })
    .then(() => true, () => false);
  if (!ok) { melde_('nach OK steht kein Warnband im Fussblock der Detailansicht'); return n; }
  const band = (auswahl) => seite.evaluate((auswahl) => {
    const e = document.querySelector(auswahl);
    if (!e) return null;
    const d = document.querySelector('body > #app > .epos-dialog');
    const fuss = [...d.children].find(x => x.querySelector(':scope > .epos-knopf--primaer'));
    const b = e.getBoundingClientRect(), f = fuss.getBoundingClientRect(), k = d.firstElementChild.getBoundingClientRect();
    const oben = document.elementFromPoint(b.left + b.width / 2, b.top + b.height / 2);
    return { top: b.top, bottom: b.bottom, fussDecke: f.top, kopfBoden: k.bottom, frei: e.contains(oben), y: scrollY };
  }, auswahl);
  const pruefe = (was, m) => {
    if (!m) { melde_(was + ' fehlt'); return; }
    const text = `${was} bei Rollstand ${zahl(m.y)}: ${zahl(m.top)}..${zahl(m.bottom)} px, Kopf bis ${zahl(m.kopfBoden)}, Leiste ab ${zahl(m.fussDecke)} px`;
    if (m.bottom > m.fussDecke + 0.5 || m.top < m.kopfBoden - 0.5 || !m.frei) melde_(text + ', frei ' + m.frei);
    else if (melden) console.log('    ' + text + ' - sichtbar');
  };
  pruefe('Band des Wirts', await band('body > #app > .epos-dialog > .epos-warnbanner'));
  // Die Detailansicht oben im Bild (ihre Oberkante 150 px unter dem Fensterrand), dann das Ende.
  const y = await seite.evaluate(() => scrollY + document.querySelector('.epos-wp-eingebettet').getBoundingClientRect().top - 150);
  await rollen(seite, y);
  pruefe('Band der Detailansicht', await band('.epos-dialog--eingebettet > .epos-dialog-fuss .epos-warnbanner'));
  await rollen(seite, 1e6);
  pruefe('Band der Detailansicht', await band('.epos-dialog--eingebettet > .epos-dialog-fuss .epos-warnbanner'));
  return n;
}

/** Die Ueberlagerung "Simulation..." des Gebaeudedialogs - Zahlen fuer den Vergleich mit/ohne Marke. */
async function ueberlagerung(browser, f, marke) {
  const seite = await oeffnen(browser, 'gebaeude', f, marke ? '' : '&marke=0');
  // Katalogauswahl V1, Stufe 4: „Simulation…" steht in der Satz-Ueberlagerung („Bearbeiten" der Detailzeile);
  // die Bedarfsansicht oeffnet darueber.
  // AW3b: mit der Knopfleiste am Satz oeffnet „Vergrößern" den Satz des Projektgebaeudes.
  await seite.locator('.epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten, .epos-zweispalten-satzknoepfe .epos-zweispalten-vergroessern').first().click();
  await seite.waitForSelector('.epos-ueberlagerung--satz', { timeout: 5000 });
  await seite.evaluate(() => {
    const k = [...document.querySelectorAll('.epos-satzueberlagerung-koerper button')].find(b => b.textContent.trim() === 'Simulation...');
    k.click();
  });
  await seite.waitForSelector('.epos-ueberlagerung:not(.epos-ueberlagerung--satz) .epos-ueberlagerung-inhalt > .epos-dialog', { timeout: 5000 });
  await seite.waitForTimeout(600);
  const messen = () => seite.evaluate(() => {
    const u = document.querySelector('.epos-ueberlagerung:not(.epos-ueberlagerung--satz)');
    const d = u.querySelector('.epos-ueberlagerung-inhalt > .epos-dialog');
    const rk = d.querySelector(':scope > .epos-dialog-rollkoerper');
    const fuss = d.querySelector(':scope > .epos-leiste:has(> .epos-status)');
    const kopf = d.querySelector(':scope > .epos-dialog-kopf');
    const uk = u.querySelector('.epos-ueberlagerung-kopf');
    const r = e => { const b = e.getBoundingClientRect(); return [Math.round(b.top * 10) / 10, Math.round(b.bottom * 10) / 10, Math.round(b.left * 10) / 10, Math.round(b.right * 10) / 10]; };
    const w = document.querySelector('body > #app > .epos-dialog');
    const wfuss = [...w.children].find(e => e.querySelector(':scope > .epos-knopf--primaer'));
    const wb = wfuss.getBoundingClientRect(), wk = w.firstElementChild.getBoundingClientRect();
    const unter = (x, y) => { const e = document.elementFromPoint(x, y); return e ? String(e.className).split(' ')[0] : ''; };
    return {
      ueberlagerung: r(u), rollhoehe: u.scrollHeight, fenster: u.clientHeight, fuss: r(fuss), fussPos: getComputedStyle(fuss).position,
      ueberlagerungRollbar: /^(auto|scroll)$/.test(getComputedStyle(u).overflowY),
      koerperRollbar: !!rk && /^(auto|scroll)$/.test(getComputedStyle(rk).overflowY),
      koerperRollhoehe: rk ? rk.scrollHeight : 0, koerperFenster: rk ? rk.clientHeight : 0, koerperStand: rk ? Math.round(rk.scrollTop) : 0,
      kopfPos: getComputedStyle(kopf).position, uekopf: r(uk), dokumentRollt: getComputedStyle(document.documentElement).overflow !== 'hidden',
      // Steht die Schlussleiste unter dem Fensterrand (KB1: Dialogkoerper unter der Mindesthoehe), zaehlt der unterste Bildpunkt.
      unterFensterfuss: unter(4, Math.min(wb.top + wb.height / 2, innerHeight - 2)), unterFensterkopf: unter(4, wk.top + wk.height / 2),
      rahmenBorder: parseFloat(getComputedStyle(u).borderBottomWidth),
    };
  });
  const oben = await messen();
  await seite.evaluate(() => { const k = document.querySelector('.epos-dialog-rollkoerper'); if (k) k.scrollTop = k.scrollHeight; });
  await seite.waitForTimeout(200);
  const unten = await messen();
  if (FOTOS && marke) await seite.screenshot({ path: `${FOTOS}/ueberlagerung_${f.breite}.png` });
  await seite.close();
  return { oben, unten };
}

/** UeS1: die Satz-Ueberlagerung des Heizkessels im eigenen Fenster (Bearbeiten… der Projektkopie). */
async function satzueberlagerung(browser, f, marke) {
  const seite = await oeffnen(browser, 'heizkessel', f, marke ? '' : '&marke=0');
  await seite.locator('.epos-knopf--bearbeiten-projekt').click();
  await seite.waitForSelector('.epos-ueberlagerung--satz', { timeout: 5000 });
  await seite.waitForTimeout(400);
  const m = await seite.evaluate(() => {
    const u = document.querySelector('.epos-ueberlagerung--satz');
    const r = e => { const b = e.getBoundingClientRect(); return [Math.round(b.top * 10) / 10, Math.round(b.bottom * 10) / 10]; };
    const w = document.querySelector('body > #app > .epos-dialog');
    const wfuss = [...w.children].find(e => e.querySelector(':scope > .epos-knopf--primaer'));
    const wb = wfuss.getBoundingClientRect(), wk = w.firstElementChild.getBoundingClientRect();
    const unter = (x, y) => { const e = document.elementFromPoint(x, y); return e ? String(e.className).split(' ')[0] : ''; };
    const k = u.querySelector('.epos-satzueberlagerung-koerper');
    return {
      ueberlagerung: r(u), kopf: r(u.querySelector(':scope > .epos-ueberlagerung-kopf')), fuss: r(u.querySelector('.epos-satzueberlagerung-fuss')),
      koerper: r(k), koerperRollt: getComputedStyle(k).overflowY, ganzRollt: u.scrollHeight > u.clientHeight + 1,
      dokumentRollt: getComputedStyle(document.documentElement).overflow !== 'hidden',
      unterFensterfuss: unter(4, Math.min(wb.top + wb.height / 2, innerHeight - 2)), unterFensterkopf: unter(4, wk.top + wk.height / 2),
    };
  });
  if (FOTOS && marke) await seite.screenshot({ path: `${FOTOS}/satzueberlagerung_${f.breite}.png` });
  await seite.close();
  return m;
}

async function katalog(browser, f, marke) {
  const seite = await oeffnen(browser, 'katalog', f, marke ? '' : '&marke=0');
  const m = await seite.evaluate(lage);
  if (FOTOS && marke) await seite.screenshot({ path: `${FOTOS}/katalog_${f.breite}.png` });
  await seite.close();
  return m;
}

/**
 * WP1a: die Satz-Ueberlagerung der Waermepumpe mit dem GERAETESTECKBRIEF links unter „Auslegung fuer Verteilung"
 * (die linke Spalte wird hoeher). Kopf und Fuss der Ueberlagerung zwischen den sicheren Abstaenden, nur der Koerper
 * rollt, der Steckbrief steht in der linken Spalte und bricht um statt quer zu rollen (kein waagerechter Rollbereich,
 * keine Ueberlappung mit den Kenndaten), sein Arbeitspunkt folgt dem Vorlauf. Dazu die Konfiguration: „Betriebszeiten"
 * steht zugeklappt, „Hoechster Vorlauf" sichtbar, Aufklappen zeigt den Knopf des Wochenrasters. Mit `stil` (Gegenprobe)
 * wird eine Stilzeile eingeschoben; liefert die Zahl der Verstoesse und die Masse.
 */
async function wpSteckbrief(browser, f, fotos, stil = '') {
  const seite = await oeffnen(browser, 'waermepumpen', f);
  if (stil) await seite.addStyleTag({ content: stil });
  await seite.locator('.epos-knopf--bearbeiten-projekt').first().click();
  await seite.waitForSelector('.epos-ueberlagerung--satz .epos-wp-steckbrief', { timeout: 5000 });
  await seite.waitForTimeout(400);
  const fehler = [];
  const m = await seite.evaluate(() => {
    const u = document.querySelector('.epos-ueberlagerung--satz');
    const r = e => { const b = e.getBoundingClientRect(); return [Math.round(b.top * 10) / 10, Math.round(b.bottom * 10) / 10]; };
    const k = u.querySelector('.epos-satzueberlagerung-koerper');
    const s = u.querySelector('.epos-wp-steckbrief');
    const links = u.querySelector('.epos-wp-abschnitt--auslegung'), rechts = u.querySelector('.epos-wp-abschnitt--kenndaten');
    const lb = links.getBoundingClientRect(), rb = rechts.getBoundingClientRect(), sb = s.getBoundingClientRect();
    return {
      ueberlagerung: r(u), kopf: r(u.querySelector(':scope > .epos-ueberlagerung-kopf')), fuss: r(u.querySelector('.epos-satzueberlagerung-fuss')),
      ganzRollt: u.scrollHeight > u.clientHeight + 1, koerperRollt: getComputedStyle(k).overflowY,
      koerperQuer: k.scrollWidth - k.clientWidth, steckbriefQuer: s.scrollWidth - s.clientWidth,
      inLinks: links.contains(s), zeilen: s.querySelectorAll('dt').length,
      steckbriefRechts: Math.round(sb.right), linksRechts: Math.round(lb.right), kenndatenLinks: Math.round(rb.left),
      nebeneinander: rb.top < lb.bottom && rb.left > lb.left + 10,
      steckbriefHoehe: Math.round(sb.height), linksHoehe: Math.round(lb.height), rechtsHoehe: Math.round(rb.height),
      heizleistung: (s.querySelector('dd[data-zeile=heizleistung]')?.textContent ?? '').trim(),
      beschriftung: [...s.querySelectorAll('dt')].map(e => e.textContent.trim()).find(x => x.startsWith('Heizleistung')) ?? '',
    };
  });
  if (m.ganzRollt) fehler.push('die Satz-Ueberlagerung rollt als Ganzes');
  if (m.koerperRollt !== 'auto') fehler.push('der Koerper ist kein Rollbereich (' + m.koerperRollt + ')');
  if (m.ueberlagerung[0] < rand(f).oben - TOL || m.ueberlagerung[1] > f.hoehe - rand(f).unten + TOL)
    fehler.push(`Satz-Ueberlagerung ${m.ueberlagerung[0]}..${m.ueberlagerung[1]} px nicht zwischen den sicheren Abstaenden`);
  if (!m.inLinks) fehler.push('der Steckbrief steht nicht in der linken Spalte');
  if (m.zeilen < 14) fehler.push(`der Steckbrief zeigt ${m.zeilen} statt 14 Zeilen`);
  if (m.koerperQuer > TOL) fehler.push(`der Koerper rollt quer (${m.koerperQuer} px)`);
  if (m.steckbriefQuer > TOL) fehler.push(`der Steckbrief rollt quer (${m.steckbriefQuer} px)`);
  if (m.steckbriefRechts > m.linksRechts + TOL) fehler.push(`der Steckbrief ragt aus der linken Spalte (${m.steckbriefRechts} > ${m.linksRechts})`);
  if (m.nebeneinander && m.linksRechts > m.kenndatenLinks + TOL) fehler.push(`linke Spalte ueberlappt die Kenndaten (${m.linksRechts} > ${m.kenndatenLinks})`);
  if (!/B0\/W35/.test(m.beschriftung)) fehler.push(`Arbeitspunkt nennt den Normpunkt nicht (${m.beschriftung})`);
  if (fotos && FOTOS) await seite.screenshot({ path: `${FOTOS}/wp_steckbrief_${f.breite}x${f.hoehe}.png` });

  // Der Arbeitspunkt folgt dem Vorlauf links.
  await seite.locator('.epos-wp-abschnitt--auslegung select').first().selectOption('55');
  await seite.waitForTimeout(300);
  const nachher = await seite.evaluate(() => [...document.querySelectorAll('.epos-wp-steckbrief dt')].map(e => e.textContent.trim()).find(x => x.startsWith('Heizleistung')) ?? '');
  if (!/W55/.test(nachher)) fehler.push(`Arbeitspunkt folgt dem Vorlauf nicht (${nachher})`);

  // Die Konfiguration: Betriebszeiten zugeklappt, „Hoechster Vorlauf" sichtbar; Aufklappen zeigt das Raster-Werkzeug.
  const konf = { zu: null, auf: null };
  await seite.locator('.epos-satzueberlagerung-koerper button', { hasText: 'Konfiguration…' }).first().click();
  await seite.waitForSelector('.epos-wp-betriebszeiten-klapp', { timeout: 5000 });
  await seite.waitForTimeout(300);
  const konfig = () => seite.evaluate(() => {
    const g = document.querySelector('.epos-wp-betriebszeiten-klapp');
    const knopf = g.querySelector('.epos-klappgruppe-knopf');
    const vorlauf = [...document.querySelectorAll('.epos-wp-betriebszeiten .epos-feld-text')].find(e => e.textContent.trim().startsWith('Höchster Vorlauf'));
    const vb = vorlauf ? vorlauf.getBoundingClientRect() : null;
    const kb = knopf.getBoundingClientRect();
    return { offen: knopf.getAttribute('aria-expanded'), zusatz: (g.querySelector('.epos-klappgruppe-zusatz')?.textContent ?? '').trim(),
             knopfHoehe: Math.round(kb.height), touchziel: parseFloat(getComputedStyle(knopf).getPropertyValue('--epos-touchziel')) || 44,
             zeitprogramm: !!document.querySelector('[data-aktion=zeitprogramm]'),
             vorlaufSichtbar: !!vb && vb.height > 0, vorlaufNachKopf: !!vb && vb.top >= kb.bottom - 1 };
  });
  konf.zu = await konfig();
  if (konf.zu.offen !== 'false') fehler.push('Betriebszeiten stehen beim Oeffnen nicht zugeklappt');
  if (konf.zu.zeitprogramm) fehler.push('zugeklappt steht der Knopf des Wochenrasters');
  if (!konf.zu.vorlaufSichtbar || !konf.zu.vorlaufNachKopf) fehler.push('„Hoechster Vorlauf" steht nicht unter der zugeklappten Gruppe');
  // Das Beruehrungsziel des Hauses (in der kompakten Stufe der Zweispaltendialoge unter 1 280 px 37 px).
  if (konf.zu.knopfHoehe < konf.zu.touchziel - TOL) fehler.push(`Klappknopf ${konf.zu.knopfHoehe} px unter dem Beruehrungsziel ${konf.zu.touchziel} px`);
  if (fotos && FOTOS) await seite.screenshot({ path: `${FOTOS}/wp_konfiguration_zu_${f.breite}x${f.hoehe}.png` });
  await seite.locator('.epos-wp-betriebszeiten-klapp .epos-klappgruppe-knopf').click();
  await seite.waitForTimeout(300);
  await seite.locator('[data-aktion=zeitprogramm]').click();
  await seite.waitForTimeout(300);
  konf.auf = await konfig();
  if (konf.auf.offen !== 'true' || !konf.auf.zeitprogramm) fehler.push('Aufklappen zeigt den Knopf des Wochenrasters nicht');
  if (!(await seite.locator('[data-kennung=wp-zeitprogramm]').count())) fehler.push('das Wochenraster oeffnet nicht');
  if (fotos && FOTOS) {
    await seite.locator('.epos-wp-betriebszeiten-klapp').scrollIntoViewIfNeeded();
    await seite.screenshot({ path: `${FOTOS}/wp_konfiguration_auf_${f.breite}x${f.hoehe}.png` });
  }
  await seite.close();
  return { n: fehler.length, fehler, m, nachher, konf };
}

/** Katalogauswahl: welche Elemente rollen, ob Dokument oder Dialog rollen, wo Kopf und Fuss stehen. */
function rollstand(unten = 0) {
  const d = document.querySelector('body > #app > .epos-dialog');
  const sichtbar = e => { const s = getComputedStyle(e); const b = e.getBoundingClientRect(); return s.display !== 'none' && s.visibility !== 'hidden' && b.width > 0 && b.height > 0; };
  const rollt = e => /^(auto|scroll)$/.test(getComputedStyle(e).overflowY);
  const erlaubt = e => e.matches('.epos-zweispalten-bereich .epos-raster-huelle, .epos-zweispalten-satz');
  // KB1: Unter der Mindesthoehe (data-zweispalten-eng) rollt der Dialogkoerper - die eine Ausnahme der Regel.
  const eng = d.hasAttribute('data-zweispalten-eng');
  const fremd = [...document.querySelectorAll('body *')].filter(e => sichtbar(e) && rollt(e) && !erlaubt(e) && !e.closest('.epos-ueberlagerung') && !(eng && e === d))
    .map(e => e.tagName.toLowerCase() + '.' + String(e.className).split(' ')[0]);
  const kopf = d.querySelector(':scope > .epos-dialog-kopf');
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
  const dok = document.scrollingElement;
  // Erreichbar: nach dem Rollen des Dialogkoerpers ans Ende steht die Schlussleiste im Fenster.
  let fussErreichbar = false;
  if (eng && fuss) { const v = d.scrollTop; d.scrollTop = d.scrollHeight; fussErreichbar = fuss.getBoundingClientRect().bottom <= innerHeight - unten + 1; d.scrollTop = v; }
  // DZ1: aufgeklappt Listen auf ihrer Untergrenze, die Satzflaeche reicht bis an den unteren Rand des Bausteins.
  const zw = d.querySelector(':scope > .epos-zweispalten.epos-zweispalten--satz-offen');
  // UeS2: mit Zusammenfassung (alle Erzeuger-Wirte, hier Heizkessel, BHKW, Waermepumpen) gibt es keinen Vorrang - die
  // Listen behalten ihre Aufteilung;
  // das haelt die Rollbereichprobe ("Vorrang trotz Zusammenfassung").
  const satzfl = zw && !zw.classList.contains('epos-zweispalten--zusammenfassung')
    && zw.querySelector(':scope > .epos-zweispalten-bereich--satz > .epos-zweispalten-satz');
  const ueber = sel => { const e = zw && zw.querySelector(sel); return e ? Math.round(e.getBoundingClientRect().height - (parseFloat(getComputedStyle(e).minHeight) || 0)) : 0; };
  const vorrang = satzfl ? { rest: Math.round(zw.getBoundingClientRect().bottom - satzfl.getBoundingClientRect().bottom),
    projekt: ueber('.epos-zweispalten-bereich--projekt .epos-raster-huelle'), katalog: ueber('.epos-zweispalten-bereich--katalog .epos-raster-huelle'),
    satz: Math.round(satzfl.getBoundingClientRect().height) } : null;
  return {
    eng, fussErreichbar, vorrang,
    fremd, listen: [...d.querySelectorAll('*')].filter(e => sichtbar(e) && rollt(e) && erlaubt(e)).length,
    dokument: dok.scrollHeight, innen: innerHeight, dialogRollt: d.scrollHeight > d.clientHeight + 1,
    kopf: kopf.getBoundingClientRect().top, kopfPos: getComputedStyle(kopf).position,
    fussBoden: fuss ? fuss.getBoundingClientRect().bottom : -1, fussPos: fuss ? getComputedStyle(fuss).position : '',
    y: scrollY,
  };
}

/** Prueft eine Katalogauswahl; liefert die Zahl der Verstoesse (fuer die Gegenprobe). */
async function nichtsRollt(seite, name, melden, rd = { oben: 0, unten: 0 }) {
  let n = 0;
  const m = melden ? (t => { n++; melde(name, t); }) : (() => n++);
  const pruefe = async stand => {
    const r = await seite.evaluate(rollstand, rd.unten);
    if (r.fremd.length) m(`${stand}: es rollt ausser Listen und Detailzeile: ${r.fremd.join(', ')}`);
    if (r.dokument > r.innen + TOL) m(`${stand}: das Dokument rollt (${r.dokument} px)`);
    if (r.vorrang) {
      if (r.vorrang.rest > 3) m(`${stand}: Satzflaeche ${r.vorrang.satz} px laesst ${r.vorrang.rest} px frei (Vorrang DZ1)`);
      if (r.vorrang.projekt > 2 || r.vorrang.katalog > 2) m(`${stand}: Listen ueber ihrer Untergrenze (Projekt +${r.vorrang.projekt} px, Katalog +${r.vorrang.katalog} px)`);
      else console.log(`    Vorrang ${name} ${stand}: Satzflaeche ${r.vorrang.satz} px, Listen auf Untergrenze`);
    }
    if (r.eng) {
      // KB1: Fenster unter der Mindesthoehe - der Dialogkoerper rollt, die Schlussleiste rollt mit und ist am Ende erreichbar.
      console.log(`    Befund ${name} ${stand}: unter der Mindesthoehe, der Dialogkoerper rollt (KB1), Schlussleiste erreichbar: ${r.fussErreichbar}`);
      if (!r.fussErreichbar) m(`${stand}: Schlussleiste auch nach dem Rollen nicht erreichbar`);
      if (r.kopfPos !== 'static' || r.fussPos !== 'static') m(`${stand}: Kopf ${r.kopfPos}, Schlussleiste ${r.fussPos}`);
    } else {
    if (r.dialogRollt) m(`${stand}: der Dialogkoerper rollt`);
    if (r.kopf < rd.oben - TOL || r.kopfPos !== 'static') m(`${stand}: Kopf bei ${zahl(r.kopf)} px, ${r.kopfPos}`);
    if (r.fussBoden > r.innen - rd.unten + TOL || r.fussPos !== 'static') m(`${stand}: Schlussleiste endet bei ${zahl(r.fussBoden)} px, ${r.fussPos}`);
    }
    return r;
  };
  const a = await pruefe('zu');
  const z = seite.locator('.epos-zweispalten-satzzeile');
  if (await z.count()) { await z.evaluate(e => e.click()); await seite.waitForTimeout(150); await pruefe('Detailzeile auf'); await z.evaluate(e => e.click()); await seite.waitForTimeout(150); }
  // Der Tabulator durch den ganzen Dialog rollt das Dokument nie.
  let gerollt = 0;
  await seite.evaluate(() => document.querySelector('body > #app > .epos-dialog').focus());
  for (let i = 0; i < 80; i++) { await seite.keyboard.press('Tab'); if (await seite.evaluate(() => scrollY) > 0) gerollt++; }
  if (gerollt) m(`Tabulator rollt das Dokument (${gerollt} Schritte)`);
  return { n, listen: a.listen, kopf: a.kopf, fuss: a.fussBoden, dokument: a.dokument };
}

/** Lizenzbanner vor einem Dialog voller Hoehe; liefert die Zahl der Verstoesse (fuer die Gegenprobe). */
const LIZENZFAELLE = ['katalog', 'heizkessel'];
async function lizenzbanner(browser, f, fall, melden, gegen = false) {
  const name = `lizenzbanner ${fall} ${f.breite}x${f.hoehe}${f.ipad ? ' iPad' : ''}`;
  let n = 0;
  const m = melden ? (t => { n++; melde(name, t); }) : (() => n++);
  const seite = await oeffnen(browser, fall, f, '&lizenz=1&marke=0');
  if (gegen) await seite.addStyleTag({ content: 'html { --epos-banner-oben: 0px !important; }' });
  await seite.waitForSelector('body > #app > .epos-lizenzbanner > .epos-warnbanner', { timeout: 20000 });
  await seite.waitForTimeout(300);
  const r = await seite.evaluate(() => {
    const b = document.querySelector('body > #app > .epos-lizenzbanner').getBoundingClientRect();
    const d = document.querySelector('body > #app > .epos-dialog');
    const kopf = d.querySelector(':scope > .epos-dialog-kopf') || d.firstElementChild;
    const fuss = [...d.querySelectorAll('.epos-knopf--primaer')].map(k => k.parentElement).pop()
      || d.querySelector('.epos-dialog-fuss');
    const dok = document.scrollingElement;
    return { bOben: b.top, bUnten: b.bottom, token: getComputedStyle(document.documentElement).getPropertyValue('--epos-banner-oben').trim(),
      dok: dok.scrollHeight, innen: dok.clientHeight, kopf: kopf.getBoundingClientRect().top,
      fuss: fuss ? fuss.getBoundingClientRect().bottom : -1, dialogUnten: d.getBoundingClientRect().bottom };
  });
  const rd = rand(f);
  if (r.dok > r.innen + TOL) m(`das Dokument rollt (${r.dok} > ${r.innen} px)`);
  if (r.bOben < rd.oben - TOL) m(`Banner beginnt bei ${zahl(r.bOben)} px, ueber dem sicheren Abstand`);
  if (r.kopf < r.bUnten - TOL) m(`Kopf bei ${zahl(r.kopf)} px unter dem Banner (${zahl(r.bUnten)} px)`);
  if (r.fuss < 0 || r.fuss > r.innen - rd.unten + TOL) m(`Schlussleiste endet bei ${zahl(r.fuss)} px, Fenster ${r.innen - rd.unten} px`);
  if (r.dialogUnten > r.innen - rd.unten + TOL) m(`Dialog endet bei ${zahl(r.dialogUnten)} px, Fenster ${r.innen - rd.unten} px`);
  if (melden) console.log(`    Banner ${zahl(r.bOben)}..${zahl(r.bUnten)} px (Token ${r.token}), Kopf ${zahl(r.kopf)}, Schlussleiste bis ${zahl(r.fuss)}, `
    + `Dokument ${r.dok}/${r.innen} px`);
  if (FOTOS && melden) await seite.screenshot({ path: `${FOTOS}/lizenzbanner_${fall}_${f.breite}.png` });
  await seite.close();
  return n;
}

/** Banner, Liste und Rahmen beim aktuellen Rollstand (Fall „meldungsliste"). */
function meldungslage() {
  const d = document.querySelector('body > #app > .epos-dialog');
  const banner = d.querySelector(':scope > .epos-warnbanner');
  const liste = banner && banner.querySelector(':scope > .epos-hinweisklappe-inhalt');
  const kopf = d.firstElementChild;
  const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
  const r = e => { const b = e.getBoundingClientRect(); return { top: b.top, bottom: b.bottom, left: b.left, right: b.right, hoehe: b.height }; };
  const rollt = e => /^(auto|scroll)$/.test(getComputedStyle(e).overflowY) && e.scrollHeight > e.clientHeight + 1;
  const fremd = [...document.querySelectorAll('body *')].filter(e => e !== liste && rollt(e))
    .map(e => e.tagName.toLowerCase() + '.' + String(e.className).split(' ')[0]);
  let unverdeckt = false;
  if (liste) {
    const b = liste.getBoundingClientRect();
    const t = document.elementFromPoint(b.left + b.width / 2, b.top + b.height / 2);
    unverdeckt = !!t && liste.contains(t);
  }
  const klapper = banner && banner.querySelector('.epos-warnbanner-klapper');
  return {
    y: scrollY, innen: innerHeight, dokument: document.documentElement.scrollHeight,
    kopf: r(kopf), fuss: r(fuss), banner: banner ? r(banner) : null, liste: liste ? r(liste) : null,
    bannerPos: banner ? getComputedStyle(banner).position : '',
    offen: klapper ? klapper.getAttribute('aria-expanded') : '', eintraege: liste ? liste.querySelectorAll('li').length : 0,
    listeHoehe: liste ? liste.scrollHeight : 0, listeFenster: liste ? liste.clientHeight : 0, listeStand: liste ? liste.scrollTop : 0,
    listeRollt: liste ? rollt(liste) : false, unverdeckt, fremd,
  };
}

/** Der Fall „meldungsliste"; liefert die Zahl der Verstoesse (fuer die Gegenprobe) und die Zahlen. */
async function meldungsliste(browser, f, melden, zusatz = '', stil = '') {
  const name = `meldungsliste ${f.breite}x${f.hoehe}${f.ipad ? ' iPad' : ''}`;
  let n = 0;
  const m = melden ? (t => { n++; melde(name, t); }) : (() => n++);
  const rd = rand(f);
  const seite = await oeffnen(browser, 'heizkessel', f, '&meldung=liste' + zusatz);
  if (stil) await seite.addStyleTag({ content: stil });
  await seite.waitForSelector('body > #app > .epos-dialog > .epos-warnbanner .epos-warnbanner-liste', { timeout: 20000 });
  await seite.waitForTimeout(200);
  const h = await haften(seite, name, melden, false, rd);
  n += h.n;
  const staende = {};
  const oben = await rollen(seite, 0);
  const ende = await rollen(seite, 1e6);
  const mitteY = Math.round(ende.y / 2);
  for (const [stand, y] of [['oben', 0], ['Mitte', mitteY], ['Ende', 1e6]]) {
    await rollen(seite, y);
    const l = await seite.evaluate(meldungslage);
    staende[stand] = l;
    if (!l.banner || !l.liste) { m(`${stand}: Banner oder Hinweisliste fehlt`); continue; }
    if (l.offen !== 'true' || l.eintraege !== 30) m(`${stand}: Liste nicht aufgeklappt (aria-expanded ${l.offen}, ${l.eintraege} Eintraege)`);
    if (l.banner.top < l.kopf.bottom - TOL || l.banner.bottom > l.fuss.top + TOL)
      m(`${stand}: Banner ${zahl(l.banner.top)}..${zahl(l.banner.bottom)} px nicht ganz zwischen Kopf (bis ${zahl(l.kopf.bottom)}) und Leiste (ab ${zahl(l.fuss.top)})`);
    if (stand !== 'oben' && Math.abs(l.banner.top - l.kopf.bottom) > TOL)
      m(`${stand}: Banner haftet nicht unter dem Kopf (${zahl(l.banner.top)} statt ${zahl(l.kopf.bottom)} px)`);
    if (l.liste.top < l.banner.top - TOL || l.liste.bottom > l.banner.bottom + TOL) m(`${stand}: Liste steht nicht im Banner`);
    if (!l.listeRollt) m(`${stand}: die Liste rollt nicht (${l.listeHoehe}/${l.listeFenster} px)`);
    if (l.listeFenster > 0.4 * l.innen + TOL) m(`${stand}: Liste ${l.listeFenster} px hoch, mehr als 40 % von ${l.innen} px`);
    if (!l.unverdeckt) m(`${stand}: Liste verdeckt (elementFromPoint)`);
    if (l.fremd.length) m(`${stand}: ausser der Liste rollt ${l.fremd.join(', ')}`);
  }
  // Das Mausrad ueber der Liste (Rollstand Mitte): die Liste rollt bis an ihr Ende, das Dokument nicht.
  const vor = await rollen(seite, mitteY);
  const l0 = await seite.evaluate(meldungslage);
  let rad = null;
  if (l0.liste && l0.banner) {
    await seite.mouse.move(l0.liste.left + 40, l0.liste.top + l0.liste.hoehe / 2);
    for (let i = 0; i < 12; i++) { await seite.mouse.wheel(0, 200); await seite.waitForTimeout(60); }
    await seite.waitForTimeout(300);
    const l1 = await seite.evaluate(meldungslage);
    rad = { stand: zahl(l1.listeStand), max: l1.listeHoehe - l1.listeFenster, y: zahl(l1.y), vorY: zahl(vor.y) };
    if (l1.listeStand < l1.listeHoehe - l1.listeFenster - TOL) m(`Mausrad: Liste nur bis ${zahl(l1.listeStand)} von ${l1.listeHoehe - l1.listeFenster} px gerollt`);
    if (Math.abs(l1.y - vor.y) > TOL) m(`Mausrad ueber der Liste rollt das Dokument (${zahl(vor.y)} -> ${zahl(l1.y)} px)`);
    if (Math.abs(l1.kopf.top - l0.kopf.top) > TOL || Math.abs(l1.fuss.bottom - l0.fuss.bottom) > TOL || Math.abs(l1.banner.top - l0.banner.top) > TOL)
      m('Mausrad: Kopf, Leiste oder Banner verschoben');
  }
  // Der Tabulator: jedes angesprungene Feld steht ganz sichtbar zwischen Banner und Schlussleiste
  // (das Polster rechnet die gemessene Hoehe des aufgeklappten Banners).
  let unterBanner = 0, schritte = 0;
  await rollen(seite, 0);
  await seite.evaluate(() => document.querySelector('body > #app > .epos-dialog').focus());
  for (let i = 0; i < 60; i++) {
    await seite.keyboard.press('Tab');
    const t = await seite.evaluate(() => {
      const a = document.activeElement;
      if (!a || a.tagName !== 'INPUT' || !a.closest('.epos-meldungslistenprobe-absatz')) return null;
      const b = a.getBoundingClientRect();
      const d = document.querySelector('body > #app > .epos-dialog');
      const bn = d.querySelector(':scope > .epos-warnbanner').getBoundingClientRect();
      const fuss = [...d.children].find(e => e.querySelector(':scope > .epos-knopf--primaer') || e.matches('.epos-dialog-fuss'));
      const fu = fuss ? fuss.getBoundingClientRect().top : innerHeight;
      return { verdeckt: b.top < bn.bottom - 0.5 || b.bottom > fu + 0.5, top: b.top, bottom: b.bottom, banner: bn.bottom, fuss: fu };
    });
    if (t === null) continue;
    schritte++;
    if (t.verdeckt) {
      unterBanner++;
      if (unterBanner <= 3) m(`Tabulator: Feld ${schritte} bei ${zahl(t.top)}..${zahl(t.bottom)} px nicht ganz zwischen Banner (bis ${zahl(t.banner)}) und Leiste (ab ${zahl(t.fuss)})`);
      else n++;
    }
  }
  if (schritte < 16) m(`Tabulator: nur ${schritte} Felder angesprungen`);
  if (FOTOS && melden) { await rollen(seite, mitteY); await seite.screenshot({ path: `${FOTOS}/meldungsliste_${f.breite}.png` }); }
  await seite.close();
  return { n, h: h.zahlen, oben: staende.oben, mitte: staende.Mitte, ende: staende.Ende, rad, schritte, unterBanner, dok: oben.dokument };
}

// ---------------------------------------------------------------------------
//  Lauf
// ---------------------------------------------------------------------------

let browser;
try {
  browser = await chromium.launch();
  if (FOTOS) await mkdir(FOTOS, { recursive: true });

  for (const f of FENSTER) {
    const kennung = `${f.breite}x${f.hoehe}`;
    for (const fall of KATALOGAUSWAHL) {
      if (NUR && NUR !== fall) continue;
      const name = `${fall} ${kennung}`;
      console.log(`- ${name} (Katalogauswahl)`);
      const seite = await oeffnen(browser, fall, f);
      const r = await nichtsRollt(seite, name, true, rand(f));
      console.log(`    rollend nur ${r.listen} Listen, Kopf ab ${zahl(r.kopf)} px, Schlussleiste bis ${zahl(r.fuss)} px, Dokument ${r.dokument} px`);
      await seite.close();
    }
    for (const fall of FENSTERDIALOGE) {
      if (NUR && NUR !== fall) continue;
      const name = `${fall} ${kennung}`;
      console.log(`- ${name}`);
      const seite = await oeffnen(browser, fall, f);
      const aufbau = await seite.evaluate(() => ({
        marke: !!document.querySelector('body > #app > .epos-fenstermarke'),
        wurzel: !!document.querySelector('body > #app > .epos-dialog:not(.epos-katalog-dialog)'),
      }));
      if (!aufbau.marke || !aufbau.wurzel) { console.log('  AUFBAUFEHLER: ' + JSON.stringify(aufbau)); process.exit(2); }
      const h = await haften(seite, name, true, NACHLAUF.has(fall), rand(f));
      if (h.zahlen) console.log(`    Kopf ${h.zahlen.kopf} px, Fuss ${h.zahlen.fuss} px, Dokument ${h.zahlen.dokument} px, Rollweg ${h.zahlen.rollweg} px; `
        + `am Ende Inhalt bis ${h.zahlen.boden} px, Leiste ${h.zahlen.fussDecke}..${h.zahlen.fussBoden} px`
        + (NACHLAUF.has(fall) && h.zahlen.nachlauf ? `, Nachlauf ${h.zahlen.nachlauf[0]}..${h.zahlen.nachlauf[1]} px` : ''));
      const fremd = await seite.evaluate(fremdeHafter);
      if (fremd.leisten.length) melde(name, 'weitere Leiste haftet: ' + fremd.leisten.join(', '));
      if (fremd.koepfe !== 1 || !fremd.kopfIstErster) melde(name, `${fremd.koepfe} haftende Koepfe, Kopf erstes Kind: ${fremd.kopfIstErster}`);
      const tab = await tabulator(seite);
      console.log(`    Tabulator: ${tab.schritte} Schritte, verdeckt ${tab.verdeckt.length}, Spruenge an Kopf/Fuss ${tab.spruenge.length}`
        + (tab.angeschnitten ? `, schreibgeschuetzte Textbereiche angeschnitten ${tab.angeschnitten}` : '')
        + (tab.ungerollt ? `, schreibgeschuetzte Textbereiche ausserhalb des Fensters (Chromium rollt sie nicht) ${tab.ungerollt}` : ''));
      tab.verdeckt.forEach(v => melde(name, 'Fokus verdeckt: ' + v));
      tab.spruenge.forEach(v => melde(name, 'Fokus in Kopf/Fuss rollt: ' + v));
      if (FOTOS) { await rollen(seite, 1e6); await seite.screenshot({ path: `${FOTOS}/${fall}_${f.breite}_ende.png` }); }
      if (fall === 'waermepumpen') await warnband(seite, name);
      await seite.close();
    }

    if (!NUR || NUR === 'ueberlagerung') {
      const name = `ueberlagerung ${kennung}`;
      console.log(`- ${name}`);
      const mit = await ueberlagerung(browser, f, true);
      const ohne = await ueberlagerung(browser, f, false);
      for (const [stand, m] of [['oben', mit.oben], ['unten', mit.unten]]) {
        if (Math.abs(m.fuss[1] - (m.ueberlagerung[1] - m.rahmenBorder)) > TOL)
          melde(name, `${stand}: Fuss des Unterdialogs endet bei ${m.fuss[1]} statt am Boden der Ueberlagerung (${m.ueberlagerung[1] - m.rahmenBorder})`);
        if (m.ueberlagerungRollbar || m.rollhoehe > m.fenster + 1) melde(name, `${stand}: die Ueberlagerung rollt als Ganzes (${m.rollhoehe}/${m.fenster} px)`);
        if (!m.koerperRollbar) melde(name, 'der Rollkoerper des Unterdialogs ist kein Rollbereich');
        if (stand === 'unten' && m.koerperRollhoehe > m.koerperFenster + 1 && m.koerperStand === 0) melde(name, 'der Rollkoerper rollt nicht');
        if (m.kopfPos !== 'static') melde(name, 'Kopf des Unterdialogs haftet (' + m.kopfPos + ') - doppelter Kopf');
        if (m.dokumentRollt) melde(name, 'das Dokument rollt unter der Ueberlagerung');
        if (m.ueberlagerung[0] < rand(f).oben - TOL || m.ueberlagerung[1] > f.hoehe - rand(f).unten + TOL)
          melde(name, `${stand}: Ueberlagerung ${m.ueberlagerung[0]}..${m.ueberlagerung[1]} px nicht zwischen den sicheren Abstaenden`);
        if (m.unterFensterfuss !== 'epos-ueberlagerung-hintergrund' || m.unterFensterkopf !== 'epos-ueberlagerung-hintergrund')
          melde(name, `Kopf/Fuss des Fensters liegen nicht unter der Abdunkelung (${m.unterFensterkopf}/${m.unterFensterfuss})`);
      }
      const a = JSON.stringify([mit.oben.ueberlagerung, mit.oben.fuss, mit.oben.uekopf, mit.unten.fuss, mit.unten.uekopf, mit.oben.rollhoehe]);
      const b = JSON.stringify([ohne.oben.ueberlagerung, ohne.oben.fuss, ohne.oben.uekopf, ohne.unten.fuss, ohne.unten.uekopf, ohne.oben.rollhoehe]);
      if (a !== b) melde(name, `mit Marke ${a} != ohne Marke ${b}`);
      console.log(`    Ueberlagerung ${JSON.stringify(mit.oben.ueberlagerung)}, Rollhoehe ${mit.oben.rollhoehe}/${mit.oben.fenster} px; `
        + `Rollkoerper ${mit.oben.koerperRollhoehe}/${mit.oben.koerperFenster} px (gerollt ${mit.unten.koerperStand} px); `
        + `Fuss oben ${JSON.stringify(mit.oben.fuss)}, unten ${JSON.stringify(mit.unten.fuss)}; Kopf des Unterdialogs ${mit.oben.kopfPos}; `
        + `mit = ohne Marke: ${a === b}`);
    }

    if (!NUR || NUR === 'satzueberlagerung') {
      const name = `satzueberlagerung ${kennung}`;
      console.log(`- ${name}`);
      const mit = await satzueberlagerung(browser, f, true);
      const ohne = await satzueberlagerung(browser, f, false);
      if (mit.ganzRollt) melde(name, 'die Satz-Ueberlagerung rollt als Ganzes');
      if (mit.koerperRollt !== 'auto') melde(name, 'der Koerper der Satz-Ueberlagerung ist kein Rollbereich (' + mit.koerperRollt + ')');
      if (mit.dokumentRollt) melde(name, 'das Dokument rollt unter der Satz-Ueberlagerung');
      if (mit.ueberlagerung[0] < rand(f).oben - TOL || mit.ueberlagerung[1] > f.hoehe - rand(f).unten + TOL)
        melde(name, `Satz-Ueberlagerung ${mit.ueberlagerung[0]}..${mit.ueberlagerung[1]} px nicht zwischen den sicheren Abstaenden`);
      if (mit.kopf[0] < mit.ueberlagerung[0] - TOL || mit.fuss[1] > mit.ueberlagerung[1] + TOL || mit.koerper[0] < mit.kopf[1] - TOL || mit.koerper[1] > mit.fuss[0] + TOL)
        melde(name, `Kopf ${JSON.stringify(mit.kopf)}, Koerper ${JSON.stringify(mit.koerper)}, Fuss ${JSON.stringify(mit.fuss)} nicht in dieser Folge in der Ueberlagerung`);
      if (mit.unterFensterfuss !== 'epos-ueberlagerung-hintergrund' || mit.unterFensterkopf !== 'epos-ueberlagerung-hintergrund')
        melde(name, `Kopf/Fuss des Fensters liegen nicht unter der Abdunkelung (${mit.unterFensterkopf}/${mit.unterFensterfuss})`);
      const a = JSON.stringify([mit.ueberlagerung, mit.kopf, mit.koerper, mit.fuss]), b = JSON.stringify([ohne.ueberlagerung, ohne.kopf, ohne.koerper, ohne.fuss]);
      if (a !== b) melde(name, `mit Marke ${a} != ohne Marke ${b}`);
      console.log(`    Satz-Ueberlagerung ${JSON.stringify(mit.ueberlagerung)}, Kopf ${JSON.stringify(mit.kopf)}, Koerper ${JSON.stringify(mit.koerper)}, `
        + `Fuss ${JSON.stringify(mit.fuss)}; mit = ohne Marke: ${a === b}`);
    }

    if (!NUR || NUR === 'katalog') {
      const name = `katalog ${kennung}`;
      console.log(`- ${name}`);
      const mit = await katalog(browser, f, true);
      const ohne = await katalog(browser, f, false);
      if (mit.kopfPos !== 'static' || mit.fussPos !== 'static') melde(name, `Kopf/Fuss haften (${mit.kopfPos}/${mit.fussPos})`);
      if (mit.dokument > mit.innen + TOL) melde(name, `das Dokument rollt (${mit.dokument} px)`);
      if (mit.kopf.top < rand(f).oben - TOL || mit.fuss.bottom > mit.innen - rand(f).unten + TOL)
        melde(name, `Kopf ab ${zahl(mit.kopf.top)} / Fuss bis ${zahl(mit.fuss.bottom)} px nicht zwischen den sicheren Abstaenden`);
      const a = JSON.stringify([mit.kopf, mit.fuss, mit.dokument]), b = JSON.stringify([ohne.kopf, ohne.fuss, ohne.dokument]);
      if (a !== b) melde(name, `mit Marke ${a} != ohne Marke ${b}`);
      console.log(`    Kopf ${zahl(mit.kopf.top)}..${zahl(mit.kopf.bottom)}, Fuss ${zahl(mit.fuss.top)}..${zahl(mit.fuss.bottom)} px, `
        + `Dokument ${mit.dokument} px, ${mit.kopfPos}/${mit.fussPos}; mit = ohne Marke: ${a === b}`);
    }

    if (!NUR || NUR === 'lizenzbanner') for (const fall of LIZENZFAELLE) {
      console.log(`- lizenzbanner ${fall} ${kennung}`);
      await lizenzbanner(browser, f, fall, true);
    }

    if (!NUR || NUR === 'meldungsliste') {
      console.log(`- meldungsliste ${kennung}`);
      const r = await meldungsliste(browser, f, true);
      const b = l => l && l.banner ? `${zahl(l.banner.top)}..${zahl(l.banner.bottom)}` : '-';
      if (r.ende && r.ende.liste) console.log(`    Kopf ${zahl(r.ende.kopf.top)}..${zahl(r.ende.kopf.bottom)}, Leiste ${zahl(r.ende.fuss.top)}..${zahl(r.ende.fuss.bottom)} px, `
        + `Dokument ${r.dok} px; Banner oben ${b(r.oben)}, Mitte ${b(r.mitte)}, Ende ${b(r.ende)} px; `
        + `Liste ${r.ende.listeFenster}/${r.ende.listeHoehe} px (${r.ende.eintraege} Eintraege, 40 % = ${zahl(0.4 * r.ende.innen)} px)`);
      if (r.rad) console.log(`    Mausrad: Liste ${r.rad.stand}/${r.rad.max} px gerollt, Dokument ${r.rad.vorY} -> ${r.rad.y} px`);
      console.log(`    Tabulator: ${r.schritte} Felder angesprungen, davon nicht ganz zwischen Banner und Leiste ${r.unterBanner}`);
    }
  }

  // ----------------------------------------- WP1a: Geraetesteckbrief der Waermepumpe
  if (!NUR || NUR === 'wp-steckbrief') {
    for (const f of WP_FENSTER) {
      const name = `wp-steckbrief ${f.breite}x${f.hoehe}`;
      console.log(`- ${name}`);
      const r = await wpSteckbrief(browser, f, true);
      for (const t of r.fehler) melde(name, t);
      console.log(`    Steckbrief ${r.m.zeilen} Zeilen, ${r.m.steckbriefHoehe} px hoch; linke Spalte ${r.m.linksHoehe} px, rechte ${r.m.rechtsHoehe} px; `
        + `quer ${r.m.koerperQuer}/${r.m.steckbriefQuer} px; ${r.m.beschriftung} -> ${r.nachher}; `
        + `Betriebszeiten zu: „${r.konf.zu.zusatz}", auf: ${r.konf.auf.offen}`);
    }
  }

  // -------------------------------------------------------------- Gegenprobe
  let gegenGruen = 0;
  if (MIT_GEGENPROBE && (!NUR || NUR === 'wp-steckbrief')) {
    // WP1a: ein Steckbrief, der nicht umbricht und breiter ist als seine Spalte, muss rot sein.
    for (const f of WP_FENSTER) {
      const g = await wpSteckbrief(browser, f, false, '.epos-wp-steckbrief { min-width: 1600px; } .epos-wp-steckbrief dd { white-space: nowrap; }');
      console.log(`    Gegenprobe wp-steckbrief zu breit ${f.breite}x${f.hoehe}: ${g.n} Verstoesse`);
      if (g.n === 0) gegenGruen++;
    }
  }
  if (MIT_GEGENPROBE) {
    console.log('- GEGENPROBE (ohne Fenstermarke / ohne scroll-padding - muss rot sein)');
    let fokusRot = 0;
    for (const f of FENSTER) for (const fall of FENSTERDIALOGE) {
      if (NUR && NUR !== fall) continue;
      const seite = await oeffnen(browser, fall, f, '&marke=0');
      const h = await haften(seite, '', false, NACHLAUF.has(fall));
      console.log(`    ohne Marke ${fall} ${f.breite}x${f.hoehe}: ${h.n} Haft-Verstoesse`);
      if (h.n === 0) gegenGruen++;
      await seite.close();
      if (NACHLAUF.has(fall)) {
        // Der Fuss mit negativem Rand trotz Nachlauf: er laege ueber dessen Beschriftung.
        const s4 = await oeffnen(browser, fall, f);
        await s4.addStyleTag({ content: '#app > .epos-dialog > .epos-leiste { margin-bottom: calc(-1 * var(--epos-karte-rand)) !important; }' });
        const h4 = await haften(s4, '', false, true);
        console.log(`    Fuss mit negativem Rand ueber dem Nachlauf ${fall} ${f.breite}x${f.hoehe}: ${h4.n} Verstoesse`);
        if (h4.n === 0) gegenGruen++;
        await s4.close();
      }
      const s2 = await oeffnen(browser, fall, f);
      await s2.addStyleTag({ content: 'html { scroll-padding: 0 !important; }' });
      const tab = await tabulator(s2);
      console.log(`    ohne scroll-padding ${fall} ${f.breite}x${f.hoehe}: verdeckt ${tab.verdeckt.length}`);
      fokusRot += tab.verdeckt.length;
      await s2.close();
      if (fall === 'waermepumpen') {
        // Das eingebettete Band mit bottom: 0 (ohne die Regel fuer .epos-dialog--eingebettet).
        const s3 = await oeffnen(browser, fall, f);
        await s3.addStyleTag({ content: '.epos-dialog--eingebettet > .epos-dialog-fuss { bottom: 0 !important; }' });
        const n = await warnband(s3, '', false);
        console.log(`    Warnband mit bottom 0 ${f.breite}x${f.hoehe}: ${n} Verstoesse`);
        if (n === 0) gegenGruen++;
        await s3.close();
      }
    }
    for (const f of FENSTER) for (const fall of KATALOGAUSWAHL) {
      if (NUR && NUR !== fall) continue;
      const s5 = await oeffnen(browser, fall, f);
      await s5.evaluate(() => {
        const d = document.querySelector('body > #app > .epos-dialog');
        d.style.setProperty('overflow', 'auto', 'important');
        // Ein rollender Dialogkoerper OHNE die Ausnahme der Mindesthoehe (KB1): Schalter gesperrt.
        d.toggleAttribute = () => false;
        d.removeAttribute('data-zweispalten-eng');
        const klotz = document.createElement('div');
        klotz.style.cssText = 'height: 2000px; flex: 0 0 auto';
        d.insertBefore(klotz, d.querySelector('.epos-zweispalten'));
      });
      const r5 = await nichtsRollt(s5, '', false);
      console.log(`    rollender Dialogkoerper ${fall} ${f.breite}x${f.hoehe}: ${r5.n} Verstoesse`);
      if (r5.n === 0) gegenGruen++;
      await s5.close();
    }
    // Vorrang der Detailzeile (DZ1): eine auf 40 px begrenzte Satzflaeche muss rot werden.
    for (const f of FENSTER) for (const fall of KATALOGAUSWAHL) {
      if ((NUR && NUR !== fall) || ZUSAMMENFASSUNG.has(fall)) continue;
      const s6 = await oeffnen(browser, fall, f);
      await s6.addStyleTag({ content: '.epos-zweispalten-satz { max-height: 40px !important; }' });
      const r6 = await nichtsRollt(s6, '', false, rand(f));
      console.log(`    Satzflaeche 40 px ${fall} ${f.breite}x${f.hoehe}: ${r6.n} Verstoesse`);
      if (r6.n === 0) gegenGruen++;
      await s6.close();
    }
    // iPad ohne die Token: Kopf und Schlussleiste reichen unter Statusleiste und Home-Anzeige.
    for (const f of FENSTER.filter(f => f.ipad)) {
      for (const fall of FENSTERDIALOGE) {
        if (NUR && NUR !== fall) continue;
        const s6 = await oeffnen(browser, fall, { ...f, ohneToken: true });
        const h6 = await haften(s6, '', false, NACHLAUF.has(fall), rand(f));
        console.log(`    ohne sichere Abstaende ${fall} ${f.breite}x${f.hoehe}: ${h6.n} Verstoesse`);
        if (h6.n === 0) gegenGruen++;
        await s6.close();
      }
      for (const fall of KATALOGAUSWAHL) {
        if (NUR && NUR !== fall) continue;
        const s7 = await oeffnen(browser, fall, { ...f, ohneToken: true });
        const r7 = await nichtsRollt(s7, '', false, rand(f));
        console.log(`    ohne sichere Abstaende ${fall} ${f.breite}x${f.hoehe}: ${r7.n} Verstoesse`);
        if (r7.n === 0) gegenGruen++;
        await s7.close();
      }
    }
    // Lizenzbanner ohne das Token: das Dokument rollt um die Bannerhoehe.
    if (!NUR || NUR === 'lizenzbanner') for (const f of FENSTER) for (const fall of LIZENZFAELLE) {
      const n8 = await lizenzbanner(browser, f, fall, false, true);
      console.log(`    Lizenzbanner ohne --epos-banner-oben ${fall} ${f.breite}x${f.hoehe}: ${n8} Verstoesse`);
      if (n8 === 0) gegenGruen++;
    }
    // Meldungsliste: ohne Hoehengrenze der Liste, mit statischem Banner, ohne Fenstermarke.
    if (!NUR || NUR === 'meldungsliste') for (const f of FENSTER) {
      for (const [was, zusatz, stil] of [
        ['ohne Hoehengrenze der Liste', '', '.epos-hinweisklappe-inhalt { max-height: none !important; }'],
        ['Banner static', '', '#app > .epos-dialog > .epos-warnbanner { position: static !important; }'],
        ['ohne Marke', '&marke=0', ''],
        ['Polster mit einer Bannerzeile', '', ':root { --epos-meldung-hoehe: calc(22px + 2 * var(--epos-rand)) !important; }'],
      ]) {
        const g = await meldungsliste(browser, f, false, zusatz, stil);
        console.log(`    Meldungsliste ${was} ${f.breite}x${f.hoehe}: ${g.n} Verstoesse (Tabulator: ${g.unterBanner} von ${g.schritte} Feldern verdeckt)`);
        if (g.n === 0) gegenGruen++;
      }
    }
    if (gegenGruen) console.log(`  GEGENPROBE GRUEN in ${gegenGruen} Faellen - die Probe misst die Regel nicht`);
    // Die Projektdialoge mit Katalogauswahl rollen nicht mehr; der einzige lange Fensterdialog des
    // Wirts (Dublettenpruefung) hat zu wenige Felder, um ohne scroll-padding eines zu verdecken. Die
    // Gegenprobe des scroll-padding ist deshalb nur noch Auskunft.
    if (fokusRot === 0) console.log('  Auskunft: ohne scroll-padding kein verdecktes Feld (kein langer Formulardialog im Wirt)');
  }

  console.log(verstoesse.length === 0 && gegenGruen === 0
    ? 'FENSTERPROBE ERFUELLT' + (MIT_GEGENPROBE ? ' (Gegenprobe rot)' : '')
    : `FENSTERPROBE VERFEHLT: ${verstoesse.length} Verstoesse, Gegenprobe gruen in ${gegenGruen} Faellen`);
  await browser.close();
  process.exit(verstoesse.length === 0 && gegenGruen === 0 ? 0 : 1);
} catch (e) {
  console.error('AUFBAUFEHLER: ' + (e && e.stack || e));
  if (browser) await browser.close();
  process.exit(2);
}
