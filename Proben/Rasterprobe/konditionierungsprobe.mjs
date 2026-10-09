// =====================================================================
//  KONDITIONIERUNGSPROBE (Stufe KP2, Wellen U0b, U1, U2, U3, U4 und U5) — der Gebäude-Katalogeditor
//  in der BREITEN Überlagerung, die Vorlagen je Karte und die Abkürzung „alle Größen“, die Karte im
//  Einzelnen, der Zonendialog als Blatt und die Gebäudeverwaltung mit dem Blatt „Konditionierung“,
//  im echten Browser — in beiden Kulturen (de-DE, en-US)
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
//      Kästchen zählt mit seiner Beschriftung; auch die zwei Felder der
//      Hilfepille, Welle U1);
//    - im Reiterblatt überdeckt kein Bedienziel ein anderes und keines ragt
//      aus dem Blatt (Reiter 1 darf in der breiten Überlagerung nicht leiden);
//    - Esc und ✕ schließen die Überlagerung, Esc auch aus einem Feld heraus;
//    - je Fall das, was ihn ausmacht: zwei Zonen im Reiter „Zonen“ (projekt),
//      Infiltration und Nutzerlüftung leer mit Platzhalter neben der
//      Luftwechselrate (gesamt), Grundzeile, weich gesperrtes OK samt Grund und
//      freies „Speichern unter“ (gesperrt; OK meldet den Grund und schreibt
//      nichts), kein „Speichern unter“ (neu), die Anordnung des Wochenrasters
//      je Behälterbreite samt sechs Zellen „aus“, im breitesten Fenster auch
//      an den Schwellen 1 150/1 149/600/599 px, jede Zelle ≥ 44 px (bausteine);
//    - der Reiter „Konditionierung“ (Welle U1): Umbruch am BEHÄLTER — ab 900 px
//      die Matrix mit fünf Spalten und fünf Karten, darunter eine Spalte, eine
//      Karte und fünf Reiter je Größe; die Tabelle rollt nicht quer. Je Fall:
//      „Kalender anlegen“ und „Zurücknehmen“ (projekt), die Rückfrage
//      „aufteilen“ an der Gesamtangabe samt Ergebnis (gesamt), die Werte als
//      Text ohne Handlung (gesperrt), Karten mit „Kalender anlegen“ (neu), der
//      benannt gesperrte Reiter ohne Karten (ohnetabellen).
//    - der Zonendialog als BLATT (Welle U4, Fall projekt): nach „Kalender
//      anlegen“ am Gebäude öffnet „Öffnen“ die Zone „Wohnen EG“ als breites
//      Blatt über dem Editor; gemessen wie ein Reiterblatt (kein Querrollen,
//      Ziele ≥ 44 px, keine Überdeckung), dazu die Zonenmatrix am Behälter;
//      die Zone ERBT („Heizen · Tag“ leer, Platzhalter „Vorgabe 20“), folgt dem
//      Gebäude („vom Gebäude“, die Heizspalte ohne Wirkung, „Vom Gebäude
//      übernehmen und anpassen“), ÜBERSCHREIBT (eigene Gewinne) und hat nach
//      „übernehmen“ einen eigenen Kalender; Esc führt zurück zum Editor.
//    - die GEBÄUDEVERWALTUNG (Welle U4, Fall verwaltung) wie im eigenen Fenster:
//      die Gruppe „Konditionierung“ mit fünf Zustandszeilen, „Konditionierung…“
//      öffnet das breite Blatt mit derselben Komponente (Matrix, Karten); das
//      Blatt gemessen wie oben, die Matrix am Behälter, eine Zelle zählt als
//      Änderung im Fuß, Esc führt zurück; der ausgelieferte Satz steht im Blatt
//      als Text.
//    - die Vorlagen (Welle U2) aus der Ablage der Seite mit den 14 der Saat: je
//      Karte die Auswahlliste, zusammen 14 Einträge, „Büro“ in jeder Liste
//      zuerst; im Fall „vorlagen“ Wahl „Büro“ mit Beschreibung und Vorschau der
//      Woche, „Übernehmen“ (Herkunft in Karte und Zeile „Vorlage“, Wahl leer),
//      die Rückfrage P12 am angelegten Kalender („Nein“ lässt alles), „Als
//      Vorlage speichern…“ inline (Formular gemessen, danach steht die eigene
//      Vorlage zuletzt in der Liste) und die Verwaltung als BLATT: Liste der
//      Größe samt der eigenen, Vorschau per Klick, Umschalter, Bedienziele,
//      Überdeckung und Querrollen im Blatt; Esc führt zurück in den Editor.
//    - die KARTE IM EINZELNEN (Welle U3, Fall karte): „Kalender bearbeiten…“ an
//      der Karte „Heizen“ (angelegt, Sommerferien, neun Feiertage, Zeitfenster,
//      eine eigene Periode) — gemessen wie ein Reiterblatt (kein Querrollen,
//      Ziele ≥ 44 px, keine Überdeckung, nichts ragt heraus), dazu die Anordnung
//      des Wochenrasters je Behälterbreite, die Periodenliste ohne Querrollen
//      (unter 600 px ohne die Spalten „Art“ und „Von–Bis“, kein Wort bricht in
//      einer Zelle), sieben Tagesknöpfe, der Vermerk, das
//      Teppichbild mit höchstens 2 000 Elementen und der Wert samt Quelle am
//      Zeiger. Je Breite EIN Foto der aufgeklappten Karte.
//    - die ABKÜRZUNG „alle Größen“ (Welle U5, E57; Fall vorlagen, neu geöffnet): die Liste
//      in der Kopfzelle der Zeile „Vorlage“ (Ziel ≥ 44 px, breit und schmal, die Namen
//      Büro, Schule, Wohnen), die Wahl „Büro“ stellt GENAU EINE Rückfrage mit fünf
//      Größenzeilen (Vorgabe „Nein“), „Nein“ lässt die Herkunft leer, „Ja“ setzt sie in
//      allen fünf Karten und Zellen der Zeile „Vorlage“, die Wahl steht danach auf „—“.
//    - BEIDE KULTUREN (Welle U5): mit --kultur en-US vergleicht die Probe die englischen
//      Texte (Tafel TEXTE); Felder der Matrix findet sie über ihre Zelle, die Antworten
//      einer Rückfrage über die Reihenfolge der Knöpfe — nicht über ihren Text. Zahlen und
//      Gemeinjahrdaten zeigt die Oberfläche in beiden Kulturen gleich (Komma, „TT.MM.“).
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

// Die Texte, die die Probe vergleicht, je Kultur (Welle U5). Was sich über die Struktur finden lässt (Zelle der
// Matrix, Reihenfolge der Knöpfe einer Rückfrage, Klassen), steht nicht hier. Die Vorlagennamen sind Daten und
// bleiben deutsch (Glossar § 10). Die Felder zeigen Zahlen immer mit Komma (`Zahlen.Anzeigetext`), die
// Herleitungszeilen im Zahlformat der Kultur (`wirksam`).
const TEXTE = {
  'de-DE': {
    zonen: 'Zonen', oeffnen: /^Öffnen/, vorgabe: 'Vorgabe', vomGebaeude: 'vom Gebäude', angelegt: 'angelegt',
    speichernUnter: 'Speichern unter', speichern: 'Speichern', verwerfen: 'Verwerfen', geaendert: 'geändert',
    luftwechselrate: 'Luftwechselrate :', wirksam: '0,60',
    groessen: ['Heizen', 'Kühlen', 'Lüftung', 'Geräte', 'Personen'],
    alleLabel: 'alle Größen', alleTitel: 'Vorlage in allen Größen übernehmen',
    alleSatz: v => `Die Vorlage „${v}“ in allen Größen übernehmen?`
  },
  'en-US': {
    zonen: 'Zones', oeffnen: /^Open/, vorgabe: 'Default value', vomGebaeude: 'from the building', angelegt: 'created',
    speichernUnter: 'Save as', speichern: 'Save', verwerfen: 'Discard', geaendert: 'changed',
    luftwechselrate: 'Air exchange rate:', wirksam: '0.60',
    groessen: ['Heating', 'Cooling', 'Ventilation', 'Equipment', 'People'],
    alleLabel: 'all quantities', alleTitel: 'Apply template to all quantities',
    alleSatz: v => `Apply the template “${v}” to all quantities?`
  }
};
const T = TEXTE[KULTUR];
if (!T) { console.error(`AUFBAUFEHLER: keine Texte für die Kultur ${KULTUR} (de-DE, en-US)`); process.exit(2); }

const FAELLE = ['projekt', 'gesamt', 'gesperrt', 'neu', 'ohnetabellen', 'vorlagen', 'bausteine', 'verwaltung', 'karte'];
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
// .epos-reiter-blatt (nur das aktive steht im Baum) - oder ein anderer Behälter
// (Welle U2: das Blatt der Vorlagenverwaltung; Welle U4: das Zonenblatt) in einer
// anderen Wurzel (Welle U4: die Gebäudeverwaltung wie im eigenen Fenster).
const MESSEN = ([ziel, tol, wurzelSel = '.epos-ueberlagerung', blattSel = '.epos-reiter-blatt']) => {
  const r = el => { const b = el.getBoundingClientRect();
    return { l: +b.left.toFixed(1), o: +b.top.toFixed(1), r: +b.right.toFixed(1), u: +b.bottom.toFixed(1),
             b: +b.width.toFixed(1), h: +b.height.toFixed(1) }; };
  const quer = el => el ? el.scrollWidth - el.clientWidth : null;
  const ueb = document.querySelector(wurzelSel);
  if (!ueb) return { fehler: 'kein ' + wurzelSel + ' im Baum' };
  const blatt = ueb.querySelector(blattSel);
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
  // Die Hilfepille zählt wie jedes Bedienziel (Welle U1: 44 px statt des
  // Hausmaßes 28 px); ihre Maße stehen zusätzlich im Protokoll.
  const pille = z => z.el.classList.contains('epos-hilfepille__feld');
  const klein = ziele.filter(z => z.m.b < ziel - tol || z.m.h < ziel - tol)
    .map(z => `${name(z)} ${z.m.b}×${z.m.h}`);
  const pillen = ziele.filter(pille).map(z => `${z.m.b}×${z.m.h}`);

  // Im Reiterblatt: Überdeckung zweier Ziele und Ziele, die aus dem Blatt
  // ragen. Ein Ziel in einem eigenen Rollbehälter (Wochenraster des Bestands,
  // Rasterhülle) zählt beim Herausragen nicht - dort rollt der Behälter.
  const imBlatt = blatt ? ziele.filter(z => blatt.contains(z.el)) : [];
  const haftet = el => { for (let v = el; v && v !== blatt; v = v.parentElement)
                           if (getComputedStyle(v).position === 'sticky') return true; return false; };
  const ueberdeckt = [];
  for (let i = 0; i < imBlatt.length; i++)
    for (let j = i + 1; j < imBlatt.length; j++) {
      const a = imBlatt[i], b = imBlatt[j];
      if (a.traeger.contains(b.traeger) || b.traeger.contains(a.traeger)) continue;
      // Eine HAFTENDE Ebene (die Fußleiste des Dialogs im Blatt, #572-Nachtrag) liegt mit Absicht über
      // dem Inhalt, der unter ihr durchrollt - das ist keine Überdeckung zweier Bedienziele.
      if (haftet(a.traeger) !== haftet(b.traeger)) continue;
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

// Der Reiter „Konditionierung“ (Welle U1): der Behälter .epos-kond, die
// sichtbaren Spalten der Matrix, die sichtbaren Reiter je Größe und Karten.
const KOND = (wurzelSel = '.epos-ueberlagerung') => {
  const k = document.querySelector(wurzelSel + ' .epos-kond');
  if (!k) return null;
  const sichtbar = el => getComputedStyle(el).display !== 'none' && el.getBoundingClientRect().width > 0;
  const tab = k.querySelector('table.epos-kond-matrix');
  const alle = sel => [...k.querySelectorAll(sel)];
  return {
    breite: k.clientWidth,
    spalten: alle('table.epos-kond-matrix th[scope=col]').filter(sichtbar).length,
    reiter: alle('.epos-kond-groessen [role=tab]').filter(sichtbar).length,
    karten: alle('.epos-kond-karte').filter(sichtbar).length,
    alleKarten: alle('.epos-kond-karte').length,
    felder: alle('table.epos-kond-matrix input').length,
    texte: alle('table.epos-kond-matrix .epos-kond-text').length,
    tabQuer: tab ? tab.scrollWidth - tab.clientWidth : 0,
    sperre: ((k.querySelector('.epos-kond-sperrzeile') || {}).innerText || '').trim(),
    kopf: alle('.epos-kond-kopf button').map(b => b.innerText.trim()),
    anlegen: alle('.epos-kond-karte button.epos-kond-anlegen').filter(sichtbar).length,
    // Welle U4, Zone: die Zustandszeilen je Größe und die Spalten ohne Wirkung.
    zonenzeilen: alle('.epos-kond-zonenzeile').filter(sichtbar).map(z => ({
      groesse: z.dataset.groesse,
      zustand: ((z.querySelector('.epos-kond-zonenzeile-zustand') || {}).innerText || '').trim(),
      uebernehmen: !!z.querySelector('button.epos-kond-uebernehmen')
    })),
    ohneWirkung: [...new Set(alle('td.epos-kond--ohnewirkung').map(td => td.dataset.groesse))],
    // Welle U2: je Karte die Auswahlliste der Vorlagen (Einträge ohne den Platzhalter)
    // und die Zeile „Vorlage“ der Matrix
    listen: alle('.epos-kond-karte .epos-kond-vorlagewahl select').map(l =>
      [...l.options].filter(o => o.value !== '').map(o => o.textContent.trim())),
    herkunft: alle('td.epos-kond-herkunft').map(t => t.innerText.trim())
  };
};

// Welle U2: die Karte einer Größe - Wahl, Beschreibung, Vorschau, Zustand, Knöpfe.
const KARTE = g => {
  const k = document.querySelector(`.epos-ueberlagerung .epos-kond-karte[data-groesse="${g}"]`);
  if (!k) return null;
  const text = sel => ((k.querySelector(sel) || {}).innerText || '').trim();
  const liste = k.querySelector('.epos-kond-vorlagewahl select');
  const svg = k.querySelector('.epos-kond-vorschau svg');
  const knopf = k.querySelector('button.epos-kond-uebernehmen');
  return {
    zustand: text('.epos-kond-karte-zustand'),
    wahl: liste && liste.selectedOptions[0] ? liste.selectedOptions[0].textContent.trim() : '',
    wert: liste ? liste.value : null,
    eintraege: liste ? [...liste.options].filter(o => o.value !== '').map(o => o.textContent.trim()) : [],
    beschreibung: text('.epos-kond-vorlage-beschreibung'),
    vorschauVorlage: text('.epos-kond-vorschau-vorlage'),
    vorschau: svg ? { b: +svg.getBoundingClientRect().width.toFixed(1),
                      formen: svg.querySelectorAll('path, polyline, rect').length } : null,
    uebernehmenGesperrt: knopf ? knopf.getAttribute('aria-disabled') : 'fehlt',
    formular: !!k.querySelector('.epos-kond-vorlage-speichern'),
    gespeichert: text('.epos-kond-vorlage-gespeichert'),
    schloss: !!k.querySelector('.epos-vorlage-schlossplatz')
  };
};

// Welle U2: das Blatt der Vorlagenverwaltung.
const VERWALTUNG = () => {
  const b = document.querySelector('.epos-ueberlagerung section.epos-blatt');
  if (!b) return null;
  const text = (el, sel) => ((el.querySelector(sel) || {}).innerText || '').trim();
  const zeilen = [...b.querySelectorAll('table.epos-kond-vorlagenliste tbody tr')];
  const svg = b.querySelector('.epos-kond-verwaltung-vorschau svg');
  return {
    titel: text(b, '.epos-blatt-titel'),
    zurueck: text(b, '.epos-blatt-zurueck'),
    groesse: text(b, 'button.epos-kond-verwaltung-groesse[aria-selected=true]'),
    namen: zeilen.map(z => text(z, '.epos-kond-vorlage-zeigen')).filter(n => n.length > 0),
    schloesser: b.querySelectorAll('table.epos-kond-vorlagenliste .epos-schloss').length,
    // Löschen steht an jeder Zeile; an einer ausgelieferten weich gesperrt (Grund am Knopf).
    loeschen: [...b.querySelectorAll('table.epos-kond-vorlagenliste button.epos-kond-loeschen')]
      .filter(k => k.getAttribute('aria-disabled') !== 'true').length,
    loeschenGesperrt: [...b.querySelectorAll('table.epos-kond-vorlagenliste button.epos-kond-loeschen')]
      .filter(k => k.getAttribute('aria-disabled') === 'true' && (k.getAttribute('title') || '').length > 0).length,
    vorschau: svg ? svg.querySelectorAll('path, polyline, rect').length : 0,
    editorSichtbar: !!document.querySelector('.epos-ueberlagerung .epos-reiter-blatt')
  };
};

// Die Prüfung der Matrix am Behälter - dieselbe Regel für Reiter, Zonenblatt und Verwaltungsblatt.
const kondPruefen = (marke, k, wo) => {
  const breit = k.breite >= 900;
  console.log(`  ${wo}: Behälter ${k.breite} px → ${k.spalten} Spalte(n), ${k.reiter} Reiter je Größe, ` +
              `${k.karten}/${k.alleKarten} Karten sichtbar, Felder ${k.felder}, Texte ${k.texte}, quer ${k.tabQuer}` +
              (k.sperre ? `; Grund „${k.sperre.slice(0, 50)}…“` : '') + (k.kopf.length ? `; Kopf: ${k.kopf.join(' | ')}` : ''));
  if (breit && (k.spalten !== 5 || k.reiter !== 0 || k.karten !== k.alleKarten))
    melde(marke, `${wo} breit (${k.breite} px): ${k.spalten} Spalten, ${k.reiter} Reiter, ${k.karten}/${k.alleKarten} Karten`);
  if (!breit && (k.spalten !== 1 || k.reiter !== 5 || k.karten !== Math.min(1, k.alleKarten)))
    melde(marke, `${wo} schmal (${k.breite} px): ${k.spalten} Spalten, ${k.reiter} Reiter, ${k.karten}/${k.alleKarten} Karten`);
  if (k.tabQuer > 0) melde(marke, `${wo}: die Matrix rollt quer (${k.tabQuer} px)`);
};

// Die Messung eines BLATTS (Zonendialog, Verwaltung): kein Querrollen, Ziele ≥ 44 px,
// keine Überdeckung, nichts ragt heraus.
const blattPruefen = (marke, m, wo) => {
  console.log(`  ${wo}: Blatt ${m.blatt ? m.blatt.b + ' px (quer ' + m.blatt.quer + ')' : '—'}, Seite quer ${m.seiteQuer}, ` +
              `Wurzel quer ${m.ueb.quer}, Ziele im Blatt ${m.zieleBlatt}, unter 44: ${m.klein.length}, ` +
              `überdeckt: ${m.ueberdeckt.length}, heraus: ${m.heraus.length}`);
  if (!m.blatt) { melde(marke, `${wo}: kein Blatt`); return; }
  if (m.seiteQuer > 0) melde(marke, `${wo}: die Seite rollt quer (${m.seiteQuer} px)`);
  if (m.ueb.quer > 0) melde(marke, `${wo}: die Wurzel rollt quer (${m.ueb.quer} px)`);
  if (m.blatt.quer > 0) melde(marke, `${wo}: das Blatt rollt quer (${m.blatt.quer} px)`);
  for (const k of m.klein) melde(marke, `${wo}: Bedienziel unter 44 px: ${k}`);
  for (const u of m.ueberdeckt) melde(marke, `${wo}: Bedienziele überdecken sich: ${u}`);
  for (const h of m.heraus) melde(marke, `${wo}: Bedienziel ragt aus dem Blatt: ${h}`);
};

// Ein Feld der Matrix nach seiner ZELLE (Größe, Zeile; i = das wievielte Eingabefeld der Zelle) — unabhängig von
// der Sprache der Beschriftung (Welle U5). Zeilen: 0 Nennwert, 1 Tag, 2 Nacht (Wert, Nachtfenster, ΔT), 3 Wochenende,
// 4 Ferien, 5 Saison.
const ZFELD = ([g, z, i = 0, wurzel = '.epos-ueberlagerung']) => {
  const td = document.querySelector(`${wurzel} td.epos-kond-zelle[data-groesse="${g}"][data-zeile="${z}"]`);
  const e = td ? td.querySelectorAll('input')[i] : null;
  return e ? { wert: e.value, platzhalter: e.getAttribute('placeholder') || '' } : null;
};
const zfeld = (seite, g, z, i = 0, wurzel = '.epos-ueberlagerung') =>
  seite.locator(`${wurzel} td.epos-kond-zelle[data-groesse="${g}"][data-zeile="${z}"] input`).nth(i);
// Die Antworten einer Rückfrage nach ihrer Reihenfolge: zuerst „Ja“, dann „Nein“ (Baustein Rueckfrage).
const ja = seite => seite.locator('.epos-rueckfrage .epos-leiste button').nth(0);
const nein = seite => seite.locator('.epos-rueckfrage .epos-leiste button').nth(1);

// Ein Feld nach seiner (vorgelesenen) Beschriftung — nur außerhalb der Matrix (Reiter 1).
const FELD = text => {
  const l = [...document.querySelectorAll('.epos-ueberlagerung label.epos-feld')]
    .find(f => ((f.querySelector('.epos-feld-text') || {}).textContent || '').trim() === text);
  const e = l ? l.querySelector('input') : null;
  return e ? { wert: e.value, platzhalter: e.getAttribute('placeholder') || '' } : null;
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

// ------------------------------------------------------------ Die Zone als Blatt (Welle U4)
// Das Gebäude hat eben „Heizen“ angelegt. Die Zone „Wohnen EG“ trägt keine eigenen Werte.
async function zoneProbe(seite, marke, name, f) {
  // Der Reiter „Zonen“ des Editors - nicht der letzte Reiter je Größe der schmalen Matrix.
  await seite.locator('.epos-ueberlagerung .epos-reiter-leiste [role=tab]', { hasText: new RegExp('^' + T.zonen + '$') }).click(); await schlaf(400);
  const zeile = seite.locator('.epos-ueberlagerung .epos-zonenliste tbody tr', { hasText: 'Wohnen EG' });
  await zeile.locator('button', { hasText: T.oeffnen }).click(); await schlaf(700);
  const blatt = await seite.evaluate(() => {
    const b = document.querySelector('.epos-ueberlagerung section.epos-blatt');
    return b ? { breit: b.classList.contains('epos-blatt--breit'),
                 titel: ((b.querySelector('.epos-blatt-titel') || {}).innerText || '').trim(),
                 reiterDa: !!document.querySelector('.epos-ueberlagerung .epos-reiter-blatt') } : null;
  });
  if (!blatt) { melde(marke, 'Zone: „Öffnen“ zeigt kein Blatt'); return; }
  console.log(`  Zone: Blatt „${blatt.titel}“${blatt.breit ? ' (breit)' : ''}, Editor dahinter ${blatt.reiterDa ? 'noch gezeichnet' : 'ausgeblendet'}`);
  if (!blatt.breit) melde(marke, 'Zone: das Blatt ist nicht breit');
  if (blatt.reiterDa) melde(marke, 'Zone: der Editor zeichnet seinen Inhalt unter dem Blatt');
  blattPruefen(marke, await seite.evaluate(MESSEN, [ZIEL, TOL, '.epos-ueberlagerung', 'section.epos-blatt']), 'Zonenblatt');
  const k = await seite.evaluate(KOND);
  if (!k) { melde(marke, 'Zone: keine Zonenmatrix'); return; }
  kondPruefen(marke, k, 'Zonenmatrix');

  // erbt: leere Zelle mit dem Wert des Gebäudes als Platzhalter
  const tag = await seite.evaluate(ZFELD, [0, 1]);
  const heizen = k.zonenzeilen.find(z => z.groesse === '0');
  console.log(`  Zone erbt: „Heizen · Tag“ „${tag?.wert}“ (Platzhalter „${tag?.platzhalter}“); ` +
              `Heizen „${heizen?.zustand}“${heizen?.uebernehmen ? ' mit „Vom Gebäude übernehmen und anpassen“' : ''}; ` +
              `ohne Wirkung: ${k.ohneWirkung.join(',') || '—'}`);
  if (!tag || tag.wert !== '' || tag.platzhalter !== T.vorgabe + ' 20') melde(marke, `Zone: „Heizen · Tag“ erbt nicht „${T.vorgabe} 20“`);
  if (!heizen || heizen.zustand !== T.vomGebaeude || !heizen.uebernehmen) melde(marke, `Zone: Heizen steht nicht „${T.vomGebaeude}“ mit „übernehmen“`);
  if (!k.ohneWirkung.includes('0')) melde(marke, 'Zone: die Heizspalte steht nicht „ohne Wirkung“');
  if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_zone-vom-gebaeude.png` });

  // überschreibt: eigene Gewinne der Zone (Nennwert der Geräte) - in der schmalen Anordnung erst die Größe wählen
  const geraete = seite.locator('.epos-ueberlagerung .epos-kond-groessen [role=tab]').nth(3);
  if (await geraete.isVisible()) { await geraete.click(); await schlaf(300); }
  const feld = zfeld(seite, 3, 0);
  const vorher = await feld.getAttribute('placeholder');
  await feld.fill('300'); await schlaf(500);
  const eigen = await seite.evaluate(ZFELD, [3, 0]);
  console.log(`  Zone überschreibt: „Geräte · Nennwert“ Platzhalter „${vorher}“ → „${eigen?.wert}“`);
  if (!vorher || !vorher.startsWith(T.vorgabe)) melde(marke, 'Zone: „Geräte · Nennwert“ nennt keinen geerbten Wert');
  if (eigen?.wert !== '300') melde(marke, 'Zone: die eigene Zelle steht nicht');

  // „Vom Gebäude übernehmen und anpassen“: die Heizspalte der Zone bekommt einen eigenen Kalender
  const heiz = seite.locator('.epos-ueberlagerung .epos-kond-groessen [role=tab]').nth(0);
  if (await heiz.isVisible()) { await heiz.click(); await schlaf(300); }
  await seite.locator('.epos-ueberlagerung .epos-kond-zonenzeile[data-groesse="0"] button.epos-kond-uebernehmen').click();
  await schlaf(500);
  const n = await seite.evaluate(KOND);
  const h2 = n.zonenzeilen.find(z => z.groesse === '0');
  console.log(`  Zone nach „übernehmen“: Heizen „${h2?.zustand}“, ohne Wirkung: ${n.ohneWirkung.join(',') || '—'}`);
  if (!h2 || h2.zustand === T.vomGebaeude || h2.uebernehmen) melde(marke, 'Zone: „übernehmen“ legt keinen eigenen Kalender an');
  if (n.ohneWirkung.includes('0')) melde(marke, 'Zone: die Heizspalte bleibt nach „übernehmen“ ohne Wirkung');
  blattPruefen(marke, await seite.evaluate(MESSEN, [ZIEL, TOL, '.epos-ueberlagerung', 'section.epos-blatt']), 'Zonenblatt danach');
  if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_zone-eigen.png` });

  // Esc führt zurück zum Editor - die Überlagerung bleibt.
  await seite.locator('.epos-ueberlagerung section.epos-blatt .epos-blatt-kopf').focus();
  await seite.keyboard.press('Escape'); await schlaf(500);
  const z = await seite.evaluate(() => ({
    blatt: !!document.querySelector('.epos-ueberlagerung section.epos-blatt'),
    ueb: !!document.querySelector('.epos-ueberlagerung'),
    reiter: !!document.querySelector('.epos-ueberlagerung .epos-reiter-blatt')
  }));
  console.log(`  Zone Esc: Blatt ${z.blatt ? 'steht' : 'zu'}, Editor ${z.reiter ? 'wieder da' : 'fehlt'}, Überlagerung ${z.ueb ? 'steht' : 'zu'}`);
  if (z.blatt || !z.ueb || !z.reiter) melde(marke, 'Zone: Esc führt nicht zurück zum Editor');
}

// ------------------------------------------------------------ Die Verwaltung (Welle U4)
async function verwaltung(browser, f) {
  const name = 'verwaltung';
  const marke = `${name} ${f.breite}×${f.hoehe}`;
  console.log(`\n${marke}`);
  const kontext = await browser.newContext({ viewport: { width: f.breite, height: f.hoehe }, deviceScaleFactor: 1 });
  const seite = await kontext.newPage();
  const fehler = [];
  seite.on('pageerror', e => fehler.push(e.message));
  seite.on('console', m => { if (m.type() === 'error') fehler.push(m.text()); });
  await seite.goto(`${WURZEL}/konditionierungsprobe?fall=${name}&kultur=${KULTUR}`, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector('.epos-gebaeude-admin', { timeout: 30000 });
  await schlaf(800);

  // schmal: erst „Stammblatt ›“
  const zumBlatt = seite.locator('.epos-auswahlleiste button.epos-nur-schmal');
  if (await zumBlatt.count() && await zumBlatt.first().isVisible()) { await zumBlatt.first().click(); await schlaf(400); }

  const gruppe = await seite.evaluate(() => {
    // Die Gruppe „Konditionierung“ ist die mit dem Knopf „Konditionierung…“ — in beiden Sprachen.
    const g = [...document.querySelectorAll('.epos-stammblatt section.epos-stammblattgruppe')]
      .find(s => s.querySelector('button.epos-gebaeude-kondknopf'));
    if (!g) return null;
    const knopf = g.querySelector('button.epos-gebaeude-kondknopf');
    const kb = knopf ? knopf.getBoundingClientRect() : null;
    return {
      zeilen: [...g.querySelectorAll('.epos-stammblattwert')].map(w => w.innerText.replace(/\s+/g, ' ').trim()),
      knopf: knopf ? knopf.innerText.trim() : '', knopfMass: kb ? `${kb.width.toFixed(1)}×${kb.height.toFixed(1)}` : '',
      knopfKlein: kb ? kb.width < 43.5 || kb.height < 43.5 : true,
      quer: g.scrollWidth - g.clientWidth
    };
  });
  if (!gruppe) { melde(marke, 'keine Gruppe „Konditionierung“ im Stammblatt'); await kontext.close(); return; }
  console.log(`  Gruppe: ${gruppe.zeilen.length} Zustandszeilen (${gruppe.zeilen.join(' | ')}); Knopf „${gruppe.knopf}“ ${gruppe.knopfMass}; quer ${gruppe.quer}`);
  if (gruppe.zeilen.length !== 5) melde(marke, `${gruppe.zeilen.length} statt 5 Zustandszeilen`);
  if (gruppe.knopfKlein) melde(marke, `„Konditionierung…“ unter 44 px (${gruppe.knopfMass})`);
  if (gruppe.quer > 0) melde(marke, `die Gruppe rollt quer (${gruppe.quer} px)`);
  if (FOTOS) {
    await seite.locator('.epos-stammblatt button.epos-gebaeude-kondknopf').scrollIntoViewIfNeeded();
    await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_stammblatt.png` });
  }

  await seite.locator('.epos-stammblatt button.epos-gebaeude-kondknopf').click(); await schlaf(700);
  const b = await seite.evaluate(() => {
    const s = document.querySelector('.epos-gebaeude-admin section.epos-blatt');
    return s ? { breit: s.classList.contains('epos-blatt--breit'), liste: !!document.querySelector('.epos-katalograhmen'),
                 hinweis: ((document.querySelector('.epos-gebaeude-kondblatt-hinweis') || {}).innerText || '').trim() } : null;
  });
  if (!b) { melde(marke, '„Konditionierung…“ zeigt kein Blatt'); await kontext.close(); return; }
  console.log(`  Blatt${b.breit ? ' (breit)' : ''}: Liste ${b.liste ? 'noch gezeichnet' : 'ausgeblendet'}; „${b.hinweis.slice(0, 60)}…“`);
  if (!b.breit) melde(marke, 'das Blatt „Konditionierung“ ist nicht breit');
  if (b.liste) melde(marke, 'die Verwaltung zeichnet Liste und Stammblatt unter dem Blatt');
  blattPruefen(marke, await seite.evaluate(MESSEN, [ZIEL, TOL, '.epos-gebaeude-admin', 'section.epos-blatt']), 'Verwaltungsblatt');
  const k = await seite.evaluate(KOND, '.epos-gebaeude-admin');
  if (!k) melde(marke, 'keine Matrix im Blatt');
  else {
    kondPruefen(marke, k, 'Verwaltungsmatrix');
    if (k.felder === 0 || k.anlegen === 0 || k.sperre) melde(marke, `Blatt: ${k.felder} Felder, ${k.anlegen} „Kalender anlegen“, Grund „${k.sperre}“`);
  }
  if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_blatt.png` });

  // Eine Zelle zählt als Änderung; Esc führt zurück, nichts ist verworfen.
  const personen = seite.locator('.epos-kond-groessen [role=tab]').nth(4);
  if (await personen.isVisible()) { await personen.click(); await schlaf(300); }
  await zfeld(seite, 4, 0, 0, 'section.epos-blatt').fill('800');
  await schlaf(400);
  await seite.locator('section.epos-blatt .epos-blatt-kopf').focus();
  await seite.keyboard.press('Escape'); await schlaf(500);
  if (await zumBlatt.count() && await zumBlatt.first().isVisible()) { await zumBlatt.first().click(); await schlaf(400); }
  const z = await seite.evaluate(([sp]) => ({
    blatt: !!document.querySelector('.epos-gebaeude-admin section.epos-blatt'),
    fuss: ((document.querySelector('.epos-stammblatt-hinweis') || {}).innerText || '').trim(),
    speichern: [...document.querySelectorAll('.epos-leiste button')].find(x => x.innerText.trim() === sp)?.disabled
  }), [T.speichern]);
  console.log(`  Esc: Blatt ${z.blatt ? 'steht' : 'zu'}; Fuß „${z.fuss}“; Speichern ${z.speichern ? 'gesperrt' : 'frei'}`);
  if (z.blatt) melde(marke, 'Esc führt nicht zurück');
  if (!z.fuss.includes(T.geaendert) || z.speichern) melde(marke, 'die Zelle zählt nicht als Änderung');

  // Der ausgelieferte Satz: im Blatt nur Text (erst „Verwerfen“, sonst hält die Liste den Wechsel an).
  await seite.locator('.epos-leiste button', { hasText: new RegExp('^' + T.verwerfen + '$') }).click(); await schlaf(400);
  // schmal: „‹ Liste“ zurück zur Liste
  const zurueck = seite.locator('.epos-stammblatt button.epos-stammblatt-zurliste');
  if (await zurueck.count() && await zurueck.first().isVisible()) { await zurueck.first().click(); await schlaf(400); }
  await seite.locator('.epos-katalogliste tbody tr', { hasText: 'Probehaus ausgeliefert' }).locator('td', { hasText: 'Probehaus ausgeliefert' }).first().click();
  await schlaf(500);
  if (await zumBlatt.count() && await zumBlatt.first().isVisible()) { await zumBlatt.first().click(); await schlaf(400); }
  await seite.locator('.epos-stammblatt button.epos-gebaeude-kondknopf').click(); await schlaf(600);
  const l = await seite.evaluate(KOND, '.epos-gebaeude-admin');
  console.log(`  Ausgeliefert: ${l ? l.felder + ' Felder, ' + l.texte + ' Texte, ' + l.anlegen + ' „Kalender anlegen“' : 'keine Matrix'}`);
  if (!l || l.felder !== 0 || l.texte === 0 || l.anlegen !== 0) melde(marke, 'der ausgelieferte Satz steht im Blatt nicht als Text');
  blattPruefen(marke, await seite.evaluate(MESSEN, [ZIEL, TOL, '.epos-gebaeude-admin', 'section.epos-blatt']), 'Blatt ausgeliefert');
  if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_ausgeliefert.png` });

  if (fehler.length) melde(marke, 'Fehler im Browser: ' + fehler.slice(0, 3).join(' | '));
  await kontext.close();
}

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
      (m.pillen.length ? ` (Hilfepille ${m.pillen.join(', ')})` : '') +
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
    const k = await seite.evaluate(KOND);
    if (k) kondPruefen(marke, k, 'Konditionierung');
    // Fall „karte“ (Welle U3): je Breite EIN Foto — das der aufgeklappten Karte (unten).
    if (FOTOS && name !== 'karte') await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_${reiter ? 'reiter' + (i + 1) : 'blatt'}.png` });
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
    // Die Gesamtangabe: Luftwechselrate 0,6 im ersten Reiter samt Herleitung 0,60;
    // Infiltration und Nutzerlüftung stehen leer in der Spalte „Lüftung“ des
    // Reiters „Konditionierung“ (E56 F3 (a)).
    const r1 = await seite.evaluate(([feld, lw, wirksam]) => {
      const zeile = [...document.querySelectorAll('.epos-ueberlagerung .epos-herleitung-text')]
        .map(e => e.innerText.trim()).find(t => t.includes(wirksam)) || '';
      return { rate: eval(feld)(lw), zeile };
    }, [FELD.toString(), T.luftwechselrate, T.wirksam]);
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(400);
    const vor = { infiltration: await seite.evaluate(ZFELD, [2, 0]), nutzer: await seite.evaluate(ZFELD, [2, 1]) };
    console.log(`  Lüftung: Luftwechselrate „${r1.rate?.wert}“, Infiltration „${vor.infiltration?.wert}“, ` +
                `Nutzerlüftung „${vor.nutzer?.wert}“; Herleitung „${r1.zeile}“`);
    if (!r1.rate || r1.rate.wert !== '0,6') melde(marke, 'die Luftwechselrate steht nicht als 0,6 da');
    for (const [n, e] of [['Infiltration', vor.infiltration], ['Nutzerlüftung', vor.nutzer]])
      if (!e || e.wert !== '') melde(marke, `${n} ist nicht leer`);
    if (!r1.zeile) melde(marke, `keine Herleitungszeile mit dem wirksamen Luftwechsel ${T.wirksam}`);

    // F5 (a): die Nachtauskühlung an der Gesamtangabe fragt „aufteilen“; „Ja“ teilt auf
    // (Infiltration 0,3, Nutzerlüftung 0,3) und setzt die Zelle - ein Schritt.
    const groesse = seite.locator('.epos-ueberlagerung .epos-kond-groessen [role=tab]').nth(2);
    if (await groesse.isVisible()) { await groesse.click(); await schlaf(300); }
    const nacht = zfeld(seite, 2, 2);
    await nacht.fill('2'); await schlaf(500);
    const frage = ((await seite.locator('.epos-rueckfrage-text').allInnerTexts())[0] || '').trim();
    console.log(`  Aufteilen: „${frage.slice(0, 90)}…“`);
    if (!frage.includes('0,6') || !frage.includes('0,3')) melde(marke, 'die Rückfrage „aufteilen“ nennt Rate und Aufteilung nicht');
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_aufteilen.png` });
    await ja(seite).click(); await schlaf(500);
    // Die Zelle „Lüftung · Nacht“ trägt den Wert, das Nachtfenster und - jetzt - ΔT der Nachtauskühlung.
    const nach = {
      infiltration: await seite.evaluate(ZFELD, [2, 0]), nutzer: await seite.evaluate(ZFELD, [2, 1]),
      nacht: await seite.evaluate(ZFELD, [2, 2, 0]), dt: await seite.evaluate(ZFELD, [2, 2, 2])
    };
    console.log(`  Nach „Ja“: Infiltration „${nach.infiltration?.wert}“, Nutzerlüftung „${nach.nutzer?.wert}“, ` +
                `Nachtauskühlung „${nach.nacht?.wert}“, ΔT ${nach.dt ? 'steht (Platzhalter ' + nach.dt.platzhalter + ')' : 'fehlt'}`);
    if (nach.infiltration?.wert !== '0,3' || nach.nutzer?.wert !== '0,3' || nach.nacht?.wert !== '2')
      melde(marke, 'nach „aufteilen“ stehen Infiltration, Nutzerlüftung und Nachtauskühlung nicht richtig');
    if (!nach.dt) melde(marke, 'ΔT der Nachtauskühlung fehlt');
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
  }
  if (name === 'projekt') {
    // „Kalender anlegen“ an der Karte „Heizen“ und „Zurücknehmen“ im Kopf.
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(400);
    const kopf = await seite.evaluate(KOND);
    await seite.locator('.epos-ueberlagerung .epos-kond-karte[data-groesse="0"] button.epos-kond-anlegen').click();
    await schlaf(500);
    const p = await seite.evaluate(() => ({
      zustand: ((document.querySelector('.epos-kond-karte[data-groesse="0"] .epos-kond-karte-zustand') || {}).innerText || '').trim(),
      zurueck: (document.querySelector('.epos-kond-zuruecknehmen') || { getAttribute: () => 'fehlt' }).getAttribute('aria-disabled')
    }));
    console.log(`  Projekt: Kopf ${kopf.kopf.join(' | ')}; Heizen nach „Kalender anlegen“: „${p.zustand}“, ` +
                `Zurücknehmen aria-disabled=${p.zurueck}`);
    if (kopf.kopf.length !== 2) melde(marke, 'im Kopf fehlen „Aus dem Katalog erneut übernehmen…“ oder „Zurücknehmen“');
    if (!p.zustand.startsWith(T.angelegt)) melde(marke, '„Kalender anlegen“ legt nicht an');
    if (p.zurueck !== null) melde(marke, '„Zurücknehmen“ ist nach dem Anlegen noch gesperrt');
    if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${name}_${f.breite}_angelegt.png` });
    await zoneProbe(seite, marke, name, f);
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
  }
  if (name === 'gesperrt' || name === 'neu' || name === 'ohnetabellen') {
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(400);
    const k = await seite.evaluate(KOND);
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
    if (name === 'gesperrt' && (k.felder !== 0 || k.texte === 0 || k.anlegen !== 0 || k.kopf.length !== 0))
      melde(marke, `Lesemodus: ${k.felder} Felder, ${k.texte} Texte, ${k.anlegen} Knöpfe „Kalender anlegen“`);
    if (name === 'neu' && (k.felder === 0 || k.anlegen === 0 || k.sperre))
      melde(marke, `Neu: ${k.felder} Felder, ${k.anlegen} Knöpfe „Kalender anlegen“, Grund „${k.sperre}“`);
    if (name === 'ohnetabellen' && (!k.sperre || k.alleKarten !== 0 || k.felder === 0))
      melde(marke, `ohne Tabellen: Grund „${k.sperre}“, ${k.alleKarten} Karten, ${k.felder} Felder`);
  }
  if (name === 'gesperrt' || name === 'neu') {
    const s = await seite.evaluate(([su]) => {
      const ueb = document.querySelector('.epos-ueberlagerung');
      const zeile = ueb.querySelector('.epos-gebk-sperrzeile');
      const leisten = [...ueb.querySelectorAll('.epos-leiste')];
      const leiste = leisten[leisten.length - 1];
      const ok = leiste ? leiste.querySelector('.epos-knopf--primaer') : null;
      const unter = leiste ? [...leiste.querySelectorAll('button')].find(b => b.innerText.includes(su)) : null;
      return {
        zeile: zeile ? zeile.innerText.replace(/\s+/g, ' ').trim() : null,
        schloss: zeile ? !!zeile.querySelector('.epos-schloss') : false,
        okGesperrt: ok ? ok.getAttribute('aria-disabled') : null,
        okGrund: ok ? ok.getAttribute('title') || '' : '',
        okDisabled: ok ? ok.disabled : null,
        unter: unter ? { gesperrt: unter.getAttribute('aria-disabled'), disabled: unter.disabled } : null
      };
    }, [T.speichernUnter]);
    console.log(`  Grundzeile: ${s.zeile === null ? '—' : '„' + s.zeile + '“'}; OK aria-disabled=${s.okGesperrt} ` +
                `disabled=${s.okDisabled}; „Speichern unter“: ${s.unter ? 'frei' : 'fehlt'}`);
    if (name === 'gesperrt') {
      if (!s.zeile || !s.schloss || !s.zeile.includes(T.speichernUnter)) melde(marke, 'die Grundzeile samt Schloss fehlt');
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
  if (name !== 'bausteine' && name !== 'ohnetabellen' && name !== 'gesperrt') {
    // Welle U2: je Karte die Auswahlliste; zusammen die 14 der Saat, „Büro“ in jeder zuerst.
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(400);
    const k = await seite.evaluate(KOND);
    const soll = await seite.evaluate(() => +document.querySelector('#probe-stand').dataset.vorlagen);
    const summe = k.listen.reduce((n, l) => n + l.length, 0);
    console.log(`  Vorlagen: ${k.listen.length} Listen mit ${k.listen.map(l => l.length).join('/')} Einträgen ` +
                `(zusammen ${summe}, Ablage ${soll}), zuerst „${[...new Set(k.listen.map(l => l[0]))].join('|')}“`);
    if (k.listen.length !== 5) melde(marke, `${k.listen.length} statt 5 Auswahllisten der Vorlagen`);
    if (summe !== soll || soll !== 14) melde(marke, `die Listen führen ${summe} Vorlagen, die Ablage ${soll} (soll 14)`);
    if (k.listen.some(l => l[0] !== 'Büro')) melde(marke, '„Büro“ steht nicht in jeder Liste zuerst');
    await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
  }
  if (name === 'vorlagen') { await vorlagen(seite, marke, f); await vorlageAlle(seite, marke, f); }
  if (name === 'karte') await karte(seite, marke, f);
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

// ------------------------------------------------------------ Welle U2: die Vorlagen
// Wahl mit Vorschau, Übernehmen, Rückfrage P12, „Als Vorlage speichern…“ und die
// Verwaltung als Blatt - an der Karte „Heizen“ (sie steht auch schmal vorn).
async function vorlagen(seite, marke, f) {
  const karte = seite.locator('.epos-ueberlagerung .epos-kond-karte[data-groesse="0"]');
  const pruefe = async (was, behaelter) => {
    const m = await seite.evaluate(MESSEN, [ZIEL, TOL, '.epos-ueberlagerung', behaelter || '.epos-reiter-blatt']);
    if (m.fehler) { melde(marke, m.fehler); return; }
    console.log(`  ${was}: ${m.blatt ? 'Behälter ' + m.blatt.b + ' px (quer ' + m.blatt.quer + ')' : 'kein Behälter'}, ` +
                `Seite quer ${m.seiteQuer}, Überlagerung quer ${m.ueb.quer}, Ziele ${m.ziele} (im Behälter ${m.zieleBlatt}), ` +
                `unter 44: ${m.klein.length}, überdeckt: ${m.ueberdeckt.length}, heraus: ${m.heraus.length}`);
    if (!m.blatt) melde(marke, `${was}: kein Behälter ${behaelter || '.epos-reiter-blatt'}`);
    if (m.seiteQuer > 0 || m.ueb.quer > 0 || (m.blatt && m.blatt.quer > 0)) melde(marke, `${was}: Querrollen`);
    for (const k of m.klein) melde(marke, `${was}: Bedienziel unter 44 px: ${k}`);
    for (const u of m.ueberdeckt) melde(marke, `${was}: Bedienziele überdecken sich: ${u}`);
    for (const h of m.heraus) melde(marke, `${was}: Bedienziel ragt heraus: ${h}`);
  };
  // Ein Foto zeigt das Fenster; die Karte wird dafür ins Bild gerollt (sie steht unter der Matrix).
  const foto = async (teil, ziel) => {
    if (!FOTOS) return;
    if (ziel) await ziel.scrollIntoViewIfNeeded();
    await seite.screenshot({ path: `${FOTOS}/vorlagen_${f.breite}_${teil}.png` });
  };

  await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(500);
  const vor = await seite.evaluate(KARTE, 0);
  console.log(`  Heizen vor der Wahl: „${vor.zustand}“, ${vor.eintraege.length} Vorlagen, ` +
              `Übernehmen aria-disabled=${vor.uebernehmenGesperrt}`);
  if (vor.uebernehmenGesperrt !== 'true') melde(marke, '„Übernehmen“ ist ohne Wahl nicht weich gesperrt');

  // Wahl „Büro“: Beschreibung und Vorschau der Woche, die „Übernehmen“ ergäbe (nach der Wahl sofort).
  await karte.locator('.epos-kond-vorlagewahl select').selectOption({ label: 'Büro' }); await schlaf(900);
  const wahl = await seite.evaluate(KARTE, 0);
  console.log(`  Wahl „${wahl.wahl}“: Schloss ${wahl.schloss ? 'ja' : 'nein'}, Beschreibung „${wahl.beschreibung.slice(0, 40)}…“, ` +
              `„${wahl.vorschauVorlage}“, Vorschau ${wahl.vorschau ? wahl.vorschau.b + ' px, ' + wahl.vorschau.formen + ' Formen' : 'fehlt'}, ` +
              `Übernehmen aria-disabled=${wahl.uebernehmenGesperrt}`);
  if (wahl.wahl !== 'Büro' || !wahl.schloss || !wahl.beschreibung || !wahl.vorschauVorlage.includes('Büro'))
    melde(marke, 'die Wahl „Büro“ zeigt Schloss, Beschreibung oder Vorschauzeile nicht');
  if (!wahl.vorschau || wahl.vorschau.formen === 0) melde(marke, 'keine Vorschau der Woche');
  if (wahl.uebernehmenGesperrt !== null) melde(marke, '„Übernehmen“ bleibt nach der Wahl gesperrt');
  await pruefe('Karte mit Wahl');
  await foto('vorschau', karte);

  // „Übernehmen“: Herkunft in der Karte (breit auch in der Zeile „Vorlage“), die Wahl ist danach leer (F2 (a)).
  await karte.locator('button.epos-kond-uebernehmen').click(); await schlaf(700);
  const nach = await seite.evaluate(KARTE, 0);
  const kond = await seite.evaluate(KOND);
  console.log(`  Nach „Übernehmen“: „${nach.zustand}“, Wahl ${nach.wert === '' ? 'leer' : '„' + nach.wahl + '“'}, ` +
              `Zeile „Vorlage“: ${kond.herkunft.length ? kond.herkunft.map(h => h || '—').join(' | ') : '—'}`);
  if (!nach.zustand.includes('Büro')) melde(marke, '„Übernehmen“ nennt die Herkunft nicht');
  if (nach.wert !== '') melde(marke, 'die Wahl ist nach „Übernehmen“ nicht leer');
  if (kond.spalten === 5 && kond.herkunft[0] !== 'Büro') melde(marke, 'die Zeile „Vorlage“ nennt „Büro“ nicht');
  await foto('uebernommen', karte);

  // Noch einmal an den angelegten Kalender: die Rückfrage P12, „Nein“ lässt alles.
  const zweite = vor.eintraege[1];
  await karte.locator('.epos-kond-vorlagewahl select').selectOption({ label: zweite }); await schlaf(700);
  await karte.locator('button.epos-kond-uebernehmen').click(); await schlaf(500);
  const frage = ((await seite.locator('.epos-rueckfrage-text').allInnerTexts())[0] || '').trim();
  console.log(`  Rückfrage P12 („${zweite}“): „${frage.slice(0, 110)}…“`);
  if (!frage.includes(zweite)) melde(marke, 'die Rückfrage P12 nennt die Vorlage nicht');
  await foto('rueckfrage');
  await nein(seite).click(); await schlaf(500);
  const ohne = await seite.evaluate(KARTE, 0);
  if (!ohne.zustand.includes('Büro')) melde(marke, '„Nein“ hat die Herkunft geändert');

  // „Als Vorlage speichern…“: das Formular inline; „Speichern“ schreibt sofort, die eigene steht zuletzt.
  await karte.locator('button.epos-kond-als-vorlage').click(); await schlaf(400);
  await pruefe('„Als Vorlage speichern…“');
  await foto('als-vorlage', karte.locator('.epos-kond-vorlage-speichern'));
  await karte.locator('.epos-kond-vorlage-name input').fill('Probe eigene'); await schlaf(200);
  await karte.locator('button.epos-kond-vorlage-schreiben').click(); await schlaf(700);
  const eigen = await seite.evaluate(KARTE, 0);
  console.log(`  Gespeichert: „${eigen.gespeichert}“; Liste ${eigen.eintraege.length} ` +
              `(zuletzt „${eigen.eintraege[eigen.eintraege.length - 1]}“)`);
  if (eigen.formular || !eigen.gespeichert || eigen.eintraege.length !== vor.eintraege.length + 1
      || eigen.eintraege[eigen.eintraege.length - 1] !== 'Probe eigene')
    melde(marke, '„Als Vorlage speichern…“ legt die eigene Vorlage nicht zuletzt in die Liste');

  // Die Verwaltung als Blatt: die Liste der Größe, Vorschau per Klick, Umschalter; Esc führt zurück.
  await karte.locator('button.epos-kond-verwalten').click(); await schlaf(600);
  let v = await seite.evaluate(VERWALTUNG);
  if (!v) { melde(marke, '„Vorlagen verwalten“ öffnet kein Blatt'); return; }
  console.log(`  Verwaltung: „${v.zurueck}“ · „${v.titel}“ · Größe „${v.groesse}“, ${v.namen.length} Vorlagen ` +
              `(${v.schloesser} Schlösser, Löschen ${v.loeschen} frei / ${v.loeschenGesperrt} weich gesperrt mit Grund), ` +
              `Editor daneben: ${v.editorSichtbar ? 'ja' : 'nein'}`);
  if (v.editorSichtbar) melde(marke, 'der Editor steht neben dem Blatt');
  if (v.namen.length !== eigen.eintraege.length || v.loeschen !== 1 || v.loeschenGesperrt !== v.schloesser)
    melde(marke, 'die Verwaltung zeigt die Liste der Größe nicht samt der eigenen');
  await pruefe('Verwaltung', 'section.epos-blatt');
  await foto('verwaltung');
  await seite.locator('.epos-ueberlagerung table.epos-kond-vorlagenliste .epos-kond-vorlage-zeigen').first().click();
  await schlaf(700);
  v = await seite.evaluate(VERWALTUNG);
  console.log(`  Vorschau in der Verwaltung: ${v.vorschau} Formen`);
  if (!v.vorschau) melde(marke, 'die Verwaltung zeigt keine Vorschau der gewählten Vorlage');
  await pruefe('Verwaltung mit Vorschau', 'section.epos-blatt');
  await foto('verwaltung-vorschau');
  await seite.locator('.epos-ueberlagerung button.epos-kond-verwaltung-groesse[data-groesse="4"]').click(); await schlaf(500);
  const personen = await seite.evaluate(VERWALTUNG);
  console.log(`  Umschalter „${personen.groesse}“: ${personen.namen.length} Vorlagen (${personen.namen.slice(0, 3).join(', ')} …)`);
  if (!personen.namen.length || personen.namen[0] !== 'Büro') melde(marke, 'der Umschalter zeigt die Liste „Personen“ nicht');
  await seite.keyboard.press('Escape'); await schlaf(600);
  const zurueck = await seite.evaluate(() => ({
    ueb: !!document.querySelector('.epos-ueberlagerung'),
    blatt: !!document.querySelector('.epos-ueberlagerung section.epos-blatt'),
    kond: !!document.querySelector('.epos-ueberlagerung .epos-kond')
  }));
  console.log(`  Esc im Blatt: Überlagerung ${zurueck.ueb ? 'steht' : 'zu'}, Blatt ${zurueck.blatt ? 'offen' : 'zu'}, ` +
              `Reiter „Konditionierung“ ${zurueck.kond ? 'wieder da' : 'fehlt'}`);
  if (!zurueck.ueb || zurueck.blatt || !zurueck.kond) melde(marke, 'Esc im Blatt führt nicht zurück in den Editor');
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
}

// ------------------------------------------------------------ Welle U5: die Abkürzung „alle Größen“ (E57)
// Die Liste in der Kopfzelle der Zeile „Vorlage“: ihr Kasten, ihre Wahl, ihre Namen, Beschriftung und Schloss.
const ALLE = () => {
  const s = document.querySelector(
    '.epos-ueberlagerung table.epos-kond-matrix tr[data-zeile="vorlage"] > th[scope="row"] .epos-kond-vorlage-alle select');
  if (!s) return null;
  const gruppe = s.closest('.epos-kond-vorlage-alle');
  const b = s.getBoundingClientRect();
  return {
    b: +b.width.toFixed(1), h: +b.height.toFixed(1),
    sichtbar: b.width > 0 && b.height > 0 && getComputedStyle(s).visibility !== 'hidden',
    wert: s.value, wahl: s.selectedOptions[0] ? s.selectedOptions[0].textContent.trim() : '',
    eintraege: [...s.options].filter(o => o.value !== '').map(o => o.textContent.trim()),
    platzhalter: ((s.querySelector('option[value=""]') || {}).textContent || '').trim(),
    beschriftung: ((gruppe.querySelector('.epos-feld-text') || {}).textContent || '').trim(),
    hinweis: gruppe.getAttribute('title') || '',
    schloss: !!gruppe.querySelector('.epos-schloss')
  };
};

// Die offene Rückfrage: wie viele stehen, Titel (aria-label ihrer Überlagerung), Zeilen, Vorgabe „Nein“.
const FRAGE = () => {
  const r = [...document.querySelectorAll('.epos-rueckfrage')];
  const erste = r[0];
  const knoepfe = erste ? [...erste.querySelectorAll('.epos-leiste button')] : [];
  return {
    anzahl: r.length,
    titel: erste ? (erste.closest('.epos-ueberlagerung') || { getAttribute: () => '' }).getAttribute('aria-label') || '' : '',
    zeilen: erste ? (((erste.querySelector('.epos-rueckfrage-text') || {}).innerText) || '').split('\n').map(z => z.trim()).filter(z => z) : [],
    neinVorgabe: knoepfe.length >= 2 && knoepfe[1].classList.contains('epos-knopf--primaer')
  };
};

// Neu geöffnet (keine Herkunft): Liste, die EINE Rückfrage, „Nein“, „Ja“ — breit und schmal.
async function vorlageAlle(seite, marke, f) {
  await seite.goto(`${WURZEL}/konditionierungsprobe?fall=vorlagen&kultur=${KULTUR}`, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector('.epos-ueberlagerung', { timeout: 30000 });
  await schlaf(800);
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(500);
  const pruefe = async was => {
    const m = await seite.evaluate(MESSEN, [ZIEL, TOL]);
    if (m.fehler) { melde(marke, m.fehler); return; }
    console.log(`  ${was}: Blatt ${m.blatt ? m.blatt.b + ' px (quer ' + m.blatt.quer + ')' : '—'}, Ziele im Blatt ${m.zieleBlatt}, ` +
                `unter 44: ${m.klein.length}, überdeckt: ${m.ueberdeckt.length}, heraus: ${m.heraus.length}`);
    if (m.seiteQuer > 0 || m.ueb.quer > 0 || (m.blatt && m.blatt.quer > 0)) melde(marke, `${was}: Querrollen`);
    for (const k of m.klein) melde(marke, `${was}: Bedienziel unter 44 px: ${k}`);
    for (const u of m.ueberdeckt) melde(marke, `${was}: Bedienziele überdecken sich: ${u}`);
    for (const h of m.heraus) melde(marke, `${was}: Bedienziel ragt aus dem Blatt: ${h}`);
  };
  const herkunft = async () => {
    const k = await seite.evaluate(KOND);
    const karten = [];
    for (let g = 0; g < 5; g++) karten.push((await seite.evaluate(KARTE, g)).zustand);
    return { zellen: k.herkunft, karten, breit: k.spalten === 5 };
  };

  const vor = await seite.evaluate(ALLE);
  if (!vor) { melde(marke, 'keine Liste „alle Größen“ in der Kopfzelle der Zeile „Vorlage“'); return; }
  const h0 = await herkunft();
  console.log(`  Alle Größen (${h0.breit ? 'breit' : 'schmal'}): „${vor.beschriftung}“ ${vor.b}×${vor.h} px, Wahl „${vor.wahl}“, ` +
              `Namen ${vor.eintraege.join(', ')}, Hinweis ${vor.hinweis ? 'da' : 'fehlt'}`);
  if (!vor.sichtbar) melde(marke, 'die Liste „alle Größen“ ist nicht sichtbar');
  if (vor.b < ZIEL - TOL || vor.h < ZIEL - TOL) melde(marke, `die Liste „alle Größen“ ist unter 44 px (${vor.b}×${vor.h})`);
  if (vor.beschriftung !== T.alleLabel || vor.platzhalter !== '—' || !vor.hinweis)
    melde(marke, `Beschriftung „${vor.beschriftung}“, Platzhalter „${vor.platzhalter}“ oder Hinweis der Liste stimmen nicht`);
  if (vor.eintraege.join('|') !== 'Büro|Schule|Wohnen') melde(marke, `die Liste führt ${vor.eintraege.join(', ')} (soll Büro, Schule, Wohnen)`);
  if (vor.wert !== '' || vor.schloss) melde(marke, 'die Liste „alle Größen“ steht nicht auf „—“');
  if (h0.zellen.some(h => h !== '—') || h0.karten.some(z => z.includes('Büro'))) melde(marke, 'vor der Wahl steht schon eine Herkunft');
  await pruefe('Alle Größen vor der Wahl');

  // Die Wahl „Büro“: GENAU EINE Rückfrage mit fünf Größenzeilen, Vorgabe „Nein“; die Liste zeigt die Wahl samt Schloss.
  const liste = seite.locator('.epos-ueberlagerung tr[data-zeile="vorlage"] > th .epos-kond-vorlage-alle select');
  await liste.selectOption({ label: 'Büro' }); await schlaf(700);
  const frage = await seite.evaluate(FRAGE);
  const groessen = frage.zeilen.filter(z => T.groessen.some(g => z.startsWith(g + ': ')));
  const mitWahl = await seite.evaluate(ALLE);
  console.log(`  Wahl „Büro“: ${frage.anzahl} Rückfrage „${frage.titel}“ (Vorgabe ${frage.neinVorgabe ? '„Nein“' : '„Ja“'}), ` +
              `${groessen.length} Größenzeilen: ${groessen.join(' | ')}; Liste „${mitWahl.wahl}“${mitWahl.schloss ? ' mit Schloss' : ''}`);
  if (frage.anzahl !== 1) melde(marke, `${frage.anzahl} statt genau einer Rückfrage`);
  if (frage.titel !== T.alleTitel) melde(marke, `die Rückfrage heißt „${frage.titel}“`);
  if (frage.zeilen[0] !== T.alleSatz('Büro')) melde(marke, `der Satz der Rückfrage lautet „${frage.zeilen[0]}“`);
  if (groessen.length !== 5 || T.groessen.some((g, i) => !(groessen[i] || '').startsWith(g + ': ')))
    melde(marke, `die Rückfrage nennt ${groessen.length} Größenzeilen (soll fünf, in der Reihenfolge der Spalten)`);
  if (!frage.neinVorgabe) melde(marke, 'die Rückfrage hat nicht „Nein“ als Vorgabe');
  if (mitWahl.wahl !== 'Büro' || !mitWahl.schloss) melde(marke, 'die Liste zeigt die Wahl „Büro“ nicht samt Schloss');
  if (FOTOS) await seite.screenshot({ path: `${FOTOS}/vorlagen_${f.breite}_alle-rueckfrage.png` });

  // „Nein“ lässt die Herkunft leer, die Wahl steht wieder auf „—“.
  await nein(seite).click(); await schlaf(600);
  const hN = await herkunft();
  const nachNein = await seite.evaluate(ALLE);
  console.log(`  Nach „Nein“: Zeile „Vorlage“ ${hN.zellen.join(' | ')}; Wahl „${nachNein.wahl}“`);
  if ((await seite.evaluate(FRAGE)).anzahl !== 0) melde(marke, 'nach „Nein“ steht noch eine Rückfrage');
  if (hN.zellen.some(h => h !== '—') || hN.karten.some(z => z.includes('Büro'))) melde(marke, '„Nein“ hat eine Herkunft gesetzt');
  if (nachNein.wert !== '' || nachNein.wahl !== '—') melde(marke, 'nach „Nein“ steht die Liste nicht auf „—“');

  // „Ja“: alle fünf Karten und Zellen der Zeile „Vorlage“ nennen „Büro“; keine zweite Frage; die Wahl steht auf „—“.
  await liste.selectOption({ label: 'Büro' }); await schlaf(700);
  await ja(seite).click(); await schlaf(1000);
  const hJ = await herkunft();
  const nachJa = await seite.evaluate(ALLE);
  console.log(`  Nach „Ja“: Zeile „Vorlage“ ${hJ.zellen.join(' | ')}; Karten ${hJ.karten.map(z => '„' + z + '“').join(', ')}; ` +
              `Wahl „${nachJa.wahl}“`);
  if ((await seite.evaluate(FRAGE)).anzahl !== 0) melde(marke, 'nach „Ja“ steht eine zweite Rückfrage');
  if (hJ.zellen.length !== 5 || hJ.zellen.some(h => h !== 'Büro')) melde(marke, 'die Zeile „Vorlage“ nennt nicht in allen fünf Zellen „Büro“');
  if (hJ.karten.some(z => !z.includes('Büro'))) melde(marke, 'nicht alle fünf Karten nennen die Herkunft „Büro“');
  if (nachJa.wert !== '' || nachJa.wahl !== '—') melde(marke, 'nach „Ja“ steht die Liste nicht auf „—“');
  await pruefe('Alle Größen nach „Ja“');
  if (FOTOS) {
    await liste.scrollIntoViewIfNeeded();
    await seite.screenshot({ path: `${FOTOS}/vorlagen_${f.breite}_alle-uebernommen.png` });
  }
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
}

// ------------------------------------------------------------ Welle U3: die Karte im Einzelnen
// Die Karte „Heizen“ aufgeklappt: Grundangabe, Zeitfenster, Wochenraster, Periodenliste, Werkzeuge,
// Teppichbild — ihre Kästen, die Anordnung des Rasters am Behälter und das Teppichbild.
const EINZELN = () => {
  // Die Einzelheiten stehen als eigener Abschnitt unter allen Karten; die Karte bleibt an ihrem Platz.
  const karteEl = document.querySelector('.epos-ueberlagerung .epos-kond-karte[data-groesse="0"]');
  const k = document.querySelector('.epos-ueberlagerung .epos-kond-karte-einzelheiten[data-groesse="0"]');
  if (!karteEl || !k) return { offen: false, klasse: false, karte: 0, raster: 0, brueche: [], tage: 0, vermerk: '', teppichZeile: '' };
  const raster = k.querySelector('.epos-wochenraster--umbrechend');
  const montag = raster ? raster.querySelector('tbody tr') : null;
  const oben = montag ? new Set([...montag.querySelectorAll('td')].map(td => Math.round(td.getBoundingClientRect().top))) : new Set();
  const tab = k.querySelector('table.epos-kond-periodentabelle');
  const art = tab ? tab.querySelector('th.epos-kond-periode-art') : null;
  const svg = k.querySelector('.epos-kond-teppich svg');
  const text = sel => ((k.querySelector(sel) || {}).innerText || '').trim();
  // Wortbrüche: ein Wort einer sichtbaren Zelle (ohne die Knopfspalte), dessen Rechtecke auf mehr als
  // einer Zeile stehen — die Tabelle bricht sonst still im Wort („Karfrei|tag“), statt quer zu rollen.
  const brueche = [];
  for (const td of tab ? tab.querySelectorAll('tbody td:not(.epos-kond-periode-aktionen)') : []) {
    const gang = document.createTreeWalker(td, NodeFilter.SHOW_TEXT);
    for (let t = gang.nextNode(); t; t = gang.nextNode()) {
      if (!t.parentElement || t.parentElement.offsetParent === null) continue;
      for (const m of t.data.matchAll(/\S+/g)) {
        const r = document.createRange();
        r.setStart(t, m.index); r.setEnd(t, m.index + m[0].length);
        if (new Set([...r.getClientRects()].map(q => Math.round(q.top))).size > 1) brueche.push(m[0]);
      }
    }
  }
  const zeitraum = tab ? tab.querySelector('th.epos-kond-periode-zeitraum') : null;
  return {
    offen: !!k.querySelector('.epos-kond-inhalt'),
    klasse: karteEl.classList.contains('epos-kond-karte--offen')
            && !!karteEl.querySelector('.epos-kond-karte-knoepfe, .epos-kond-vorlagewahl')
            && Math.abs(k.getBoundingClientRect().width - k.parentElement.getBoundingClientRect().width) < 1,
    karte: Math.round(k.getBoundingClientRect().width),
    raster: raster ? raster.clientWidth : 0,
    rasterQuer: raster ? raster.scrollWidth - raster.clientWidth : null,
    zeilenJeTag: oben.size,
    perioden: tab ? tab.querySelectorAll('tbody tr').length : 0,
    matrixbereich: tab ? tab.querySelectorAll('tbody tr.epos-kond-periode--matrix').length : 0,
    periodenQuer: tab ? tab.scrollWidth - tab.clientWidth : null,
    art: art ? getComputedStyle(art).display !== 'none' : null,
    zeitraum: zeitraum ? getComputedStyle(zeitraum).display !== 'none' : null,
    brueche,
    tage: k.querySelectorAll('button.epos-kond-tag').length,
    vermerk: text('.epos-kond-vermerk'),
    teppichElemente: svg ? svg.querySelectorAll('*').length : 0,
    teppichWerte: svg ? svg.querySelectorAll('[data-wert]').length : 0,
    teppichZeile: text('.epos-kond-teppich-zeile')
  };
};

async function karte(seite, marke, f) {
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(1).click(); await schlaf(500);
  await seite.locator('.epos-ueberlagerung .epos-kond-karte[data-groesse="0"] button.epos-kond-einzelheiten').click();
  await schlaf(900);
  const karteLoc = seite.locator('.epos-ueberlagerung .epos-kond-karte-einzelheiten[data-groesse="0"]');

  const m = await seite.evaluate(MESSEN, [ZIEL, TOL]);
  if (m.fehler) { melde(marke, m.fehler); return; }
  console.log(`  Karte im Einzelnen: Blatt ${m.blatt ? m.blatt.b + ' px (quer ' + m.blatt.quer + ')' : '—'}, ` +
              `Seite quer ${m.seiteQuer}, Überlagerung quer ${m.ueb.quer}, Ziele im Blatt ${m.zieleBlatt}, ` +
              `unter 44: ${m.klein.length}, überdeckt: ${m.ueberdeckt.length}, heraus: ${m.heraus.length}`);
  if (m.seiteQuer > 0 || m.ueb.quer > 0 || (m.blatt && m.blatt.quer > 0)) melde(marke, 'Karte im Einzelnen: Querrollen');
  for (const k of m.klein) melde(marke, `Karte im Einzelnen: Bedienziel unter 44 px: ${k}`);
  for (const u of m.ueberdeckt) melde(marke, `Karte im Einzelnen: Bedienziele überdecken sich: ${u}`);
  for (const h of m.heraus) melde(marke, `Karte im Einzelnen: Bedienziel ragt aus dem Blatt: ${h}`);

  const e = await seite.evaluate(EINZELN);
  const soll = e.raster >= 1150 ? 1 : e.raster >= 600 ? 2 : 4;
  console.log(`  Karte ${e.karte} px${e.klasse ? ' (ganze Zeile)' : ''}: Raster ${e.raster} px → ${e.zeilenJeTag} Zeile(n) je Tag ` +
              `(soll ${soll}), quer ${e.rasterQuer}; Perioden ${e.perioden} (Matrixbereich ${e.matrixbereich}), ` +
              `Spalten „Art“ ${e.art ? 'sichtbar' : 'aus'}, „Von–Bis“ ${e.zeitraum ? 'sichtbar' : 'aus'}, ` +
              `quer ${e.periodenQuer}, Wortbrüche ${e.brueche.length}; Tagesknöpfe ${e.tage}; ` +
              `Vermerk „${e.vermerk.slice(0, 60)}“; Teppich ${e.teppichElemente} Elemente, ${e.teppichWerte} mit Wert; „${e.teppichZeile}“`);
  if (!e.offen || !e.klasse) melde(marke, '„Kalender bearbeiten…“ zeigt die Einzelheiten nicht über die ganze Zeile neben der stehenden Karte');
  if (e.zeilenJeTag !== soll) melde(marke, `das Wochenraster steht in ${e.zeilenJeTag} Zeilen je Tag (soll ${soll} bei ${e.raster} px)`);
  if (e.rasterQuer > 0) melde(marke, `das Wochenraster rollt quer (${e.rasterQuer} px)`);
  if (e.perioden !== 11 || e.matrixbereich !== 1) melde(marke, `Periodenliste: ${e.perioden} Zeilen, ${e.matrixbereich} des Matrixbereichs (soll 11, 1)`);
  if (e.periodenQuer > 0) melde(marke, `die Periodenliste rollt quer (${e.periodenQuer} px)`);
  if (e.brueche.length) melde(marke, `die Periodenliste bricht im Wort: ${e.brueche.slice(0, 5).join(', ')}`);
  if (e.tage !== 7) melde(marke, `${e.tage} statt 7 Tagesknöpfe am Zeitfenster`);
  if (!e.vermerk) melde(marke, 'kein Vermerk des letzten Werkzeugs');
  if (e.teppichElemente === 0 || e.teppichElemente > 2000) melde(marke, `Teppichbild mit ${e.teppichElemente} Elementen (soll 1 … 2 000)`);
  if (!e.teppichZeile.includes('2025')) melde(marke, 'die Zeile unter dem Teppichbild nennt das Bezugsjahr nicht');

  // Der Wert am Element: Zeigen auf ein Feld nennt Zeitraum, Wert und Quelle.
  const feld = karteLoc.locator('.epos-kond-teppich svg [data-wert]').first();
  await feld.scrollIntoViewIfNeeded(); await feld.hover({ force: true }); await schlaf(400);
  const zeiger = ((await karteLoc.locator('.epos-kond-teppich .epos-diagramm-zeigerzeile').allInnerTexts())[0] || '').trim();
  console.log(`  Teppich am Zeiger: „${zeiger}“`);
  if (!zeiger.includes('·')) melde(marke, 'das Teppichbild nennt am Zeiger keine Quelle');

  if (FOTOS) {
    await karteLoc.locator('.epos-kond-inhalt').scrollIntoViewIfNeeded();
    await seite.screenshot({ path: `${FOTOS}/karte_${f.breite}_einzelheiten.png` });
  }
  await seite.locator('.epos-ueberlagerung [role=tab]').nth(0).click(); await schlaf(300);
}

// ------------------------------------------------------------ Ablauf
let browser;
try {
  if (FOTOS) await mkdir(FOTOS, { recursive: true });
  browser = await chromium.launch({ headless: true });
  for (const name of FAELLE) {
    if (NUR && name !== NUR) continue;
    for (const f of FENSTER) {
      if (name === 'verwaltung') await verwaltung(browser, f);
      else await fall(browser, name, f);
    }
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
