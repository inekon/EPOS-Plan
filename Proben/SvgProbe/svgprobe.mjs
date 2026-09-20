// =====================================================================
//  SVG-PROBE (Konzept DG-1) — ein Jahresgang als SVG im ECHTEN Browser
// =====================================================================
//
//  WOZU. Das Konzept "Diagramme direkt in der Oberflaeche statt als Bild"
//  braucht Zahlen, keine Meinung: Wie gross ist ein SVG mit 8 760 Stuetzstellen
//  je Reihe, wie lange braucht Chromium, es zu zeichnen, und wie fluessig ist
//  ein Zoom ueber die viewBox? Dieselben Fragen fuer das PNG des Bestands, aus
//  demselben Wirt, mit denselben Reihen.
//
//  Gemessen wird je Fall:
//    (a) Blazor:   Zeit vom Aufruf der Seite bis das Diagramm im Baum steht
//                  (Blazor Server, also EINSCHLIESSLICH SignalR - nur ein Anhalt;
//                  in der WebView faehrt derselbe Zeichenlauf ueber den Prozess).
//    (b) Rendern:  das Markup aus /svgprobe/svg (bzw. das PNG) NETZFREI in einen
//                  leeren Behaelter setzen - 7 Wiederholungen, drei Zeiten.
//    (c) Zoom:     30 Schritte viewBox-Zoom auf der Zeitachse (Faktor 0,93 zur
//                  Mitte, zusammen rund 9-fach) und 30 Schritte Verschieben auf
//                  #svgprobe-flaeche; je Schritt drei Zeiten.
//    (d) Treffer:  50 elementFromPoint ueber der Zeichenflaeche - was "Werte am
//                  Mauszeiger" kostet - und die Umrechnung Zeiger -> Stunde.
//    (e) Text:     ist die Beschriftung TEXT (innerText findet "Reihe 1")?
//    (f) Baum:     Knoten im svg, Zeichen in den Pfaden, JS-Heap (CDP).
//    (g) Fotos:    1:1 und nach dem Zoom (--fotos <ordner>).
//
//  DREI ZEITEN je Vorgang, weil eine allein luegt:
//    sync    Einsetzen bzw. Attribut setzen + getBoundingClientRect(): Parsen,
//            Stil und Layout auf dem Hauptfaden, synchron gemessen.
//    rahmen  Zeit bis zum naechsten Bildaufbau (EIN requestAnimationFrame). Ein
//            Browser mit 60 Hz liefert ~16,7 ms, solange die Arbeit in den Rahmen
//            passt; ein ausgelassener Rahmen zeigt sich als 33 ms.
//    task    Hauptfaden-Arbeit laut CDP Performance.getMetrics (TaskDuration,
//            LayoutDuration, RecalcStyleDuration) als Differenz um den Vorgang -
//            die REINE Rechenzeit, unabhaengig vom Takt. Die Rasterung auf den
//            Rasterfaeden ist darin nicht enthalten.
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md; Port 5371):
//    node svgprobe.mjs --url http://127.0.0.1:5371 --fotos /tmp/svgfotos
//
//  Rueckgabe 0 = die Sollwerte der gebuendelten Faelle erfuellt, 1 = verfehlt,
//  2 = Aufruf- oder Verbindungsfehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

// Playwright liegt in dieser Umgebung GLOBAL (npm -g); ein ES-Modul sucht dort
// nicht von selbst. Erst der gewoehnliche Weg, dann der globale Wurzelordner.
const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/svgprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5371');
const FOTOS = arg('fotos', '');
const NUR = arg('nur', '');
const WIEDERHOLUNGEN = 7;
const ZOOMSCHRITTE = 30;

const schlaf = ms => new Promise(r => setTimeout(r, ms));
const median = a => { const s = [...a].sort((x, y) => x - y); return s.length ? +s[Math.floor(s.length / 2)].toFixed(2) : null; };
const maximum = a => a.length ? +Math.max(...a).toFixed(2) : null;

const EIN_RAHMEN = `new Promise(r => requestAnimationFrame(() => r(performance.now())))`;

// ------------------------------------------------------ CDP-Metriken
async function metriken(cdp) {
  try {
    const m = await cdp.send('Performance.getMetrics');
    const wert = n => m.metrics.find(x => x.name === n)?.value ?? 0;
    return { task: wert('TaskDuration') * 1000, layout: wert('LayoutDuration') * 1000,
             stil: wert('RecalcStyleDuration') * 1000, heapMB: wert('JSHeapUsedSize') / 1048576,
             knoten: wert('Nodes') };
  } catch { return null; }
}
const differenz = (a, b, teiler) => (a && b)
  ? { task: +((b.task - a.task) / teiler).toFixed(2), layout: +((b.layout - a.layout) / teiler).toFixed(2),
      stil: +((b.stil - a.stil) / teiler).toFixed(2) }
  : null;

// ------------------------------------------------ Messungen IM Browser
async function rendernMessen(seite, cdp, variante, reihen) {
  // Das Markup EINMAL holen; gemessen wird das Einsetzen, nicht das Netz.
  await seite.evaluate(async ({ variante, reihen }) => {
    if (variante === 'png') {
      const blob = await (await fetch(`/svgprobe/png?reihen=${reihen}`)).blob();
      window.__svgprobeQuelle = URL.createObjectURL(blob);
    } else {
      window.__svgprobeQuelle = await (await fetch(`/svgprobe/svg?variante=${variante}&reihen=${reihen}`)).text();
    }
  }, { variante, reihen });

  const sync = [], rahmen = [];
  const vorher = await metriken(cdp);
  for (let k = 0; k < WIEDERHOLUNGEN; k++) {
    const z = await seite.evaluate(async ({ variante, EIN_RAHMEN }) => {
      const buehne = document.getElementById('svgprobe-buehne');
      buehne.innerHTML = '';
      buehne.getBoundingClientRect();
      await eval(EIN_RAHMEN);
      const t0 = performance.now();
      if (variante === 'png') {
        const img = new Image();
        img.width = 1304; img.height = 440;
        const geladen = new Promise(r => { img.onload = r; });
        img.src = window.__svgprobeQuelle;
        buehne.appendChild(img);
        await geladen;                       // Dekodieren gehoert zum Bild
      } else {
        buehne.innerHTML = window.__svgprobeQuelle;
      }
      buehne.getBoundingClientRect();
      const t1 = performance.now();
      const t2 = await eval(EIN_RAHMEN);
      return { sync: t1 - t0, rahmen: t2 - t0 };
    }, { variante, EIN_RAHMEN });
    sync.push(z.sync); rahmen.push(z.rahmen);
  }
  const nachher = await metriken(cdp);
  return { sync, rahmen, jeVorgang: differenz(vorher, nachher, WIEDERHOLUNGEN) };
}

// Der Zoom: die viewBox des inneren svg. Jeder Schritt zieht das Fenster um
// den Faktor 0,93 zur Mitte zusammen; danach 30 Schritte Verschieben um ein
// Zwanzigstel der Fensterbreite.
async function zoomMessen(seite, cdp) {
  const vorher = await metriken(cdp);
  // Gezoomt wird das Diagramm der SEITE (das Blazor geliefert hat), nicht die
  // Kopie auf der Buehne - so zeigt das Foto "gezoomt" den Zustand.
  const e = await seite.evaluate(async ({ ZOOMSCHRITTE, EIN_RAHMEN }) => {
    const fl = document.querySelector('#svgprobe-inhalt #svgprobe-flaeche');
    if (!fl) return { fehler: 'keine Zeichenflaeche' };
    document.getElementById('svgprobe-buehne').innerHTML = '';
    window.scrollTo(0, 0);
    const start = fl.getAttribute('viewBox');
    let [x, y, b, h] = start.split(/\s+/).map(Number);
    const zoomSync = [], zoomRahmen = [], panSync = [], panRahmen = [];
    await eval(EIN_RAHMEN);
    // Zoom auf der ZEITACHSE wie der Datenzoom des Hauses (Achsenfenster, y bleibt
    // voll): Faktor 0,93 je Schritt, nach 30 Schritten ist rund ein Zwoelftel
    // des Jahres zu sehen - der Tagesgang wird sichtbar (Foto "gezoomt").
    for (let i = 0; i < ZOOMSCHRITTE; i++) {
      const nb = b * 0.93;
      x += (b - nb) / 2; b = nb;
      const t0 = performance.now();
      fl.setAttribute('viewBox', `${x} ${y} ${b} ${h}`);
      fl.getBoundingClientRect();
      zoomSync.push(performance.now() - t0);
      zoomRahmen.push((await eval(EIN_RAHMEN)) - t0);
    }
    for (let i = 0; i < ZOOMSCHRITTE; i++) {
      x += b / 20;
      const t0 = performance.now();
      fl.setAttribute('viewBox', `${x} ${y} ${b} ${h}`);
      fl.getBoundingClientRect();
      panSync.push(performance.now() - t0);
      panRahmen.push((await eval(EIN_RAHMEN)) - t0);
    }
    return { start, ende: fl.getAttribute('viewBox'), zoomSync, zoomRahmen, panSync, panRahmen,
             faktor: +(Number(start.split(/\s+/)[2]) / b).toFixed(1) };
  }, { ZOOMSCHRITTE, EIN_RAHMEN });
  const nachher = await metriken(cdp);
  e.jeSchritt = differenz(vorher, nachher, 2 * ZOOMSCHRITTE);
  return e;
}

// "Werte am Mauszeiger": Was kostet die Trefferpruefung (elementFromPoint) und
// die Umrechnung Zeiger -> Stunde ueber die viewBox? Der Wert selbst kaeme aus
// der Reihe (Index = Stunde), nicht aus dem Pfad.
async function trefferMessen(seite) {
  return seite.evaluate(() => {
    const fl = document.querySelector('#svgprobe-inhalt #svgprobe-flaeche');
    if (!fl) return null;
    window.scrollTo(0, 0);                 // elementFromPoint sieht nur den Sichtbereich
    const r = fl.getBoundingClientRect();
    const vb = fl.viewBox.baseVal;
    // Der Zeiger faehrt auf der ERSTEN Reihe entlang: y aus deren Pfad, damit
    // die Trefferpruefung zeigt, ob eine 2 px duenne Linie ueberhaupt zu treffen
    // ist (fuer "Werte am Mauszeiger" braucht man sie nicht - die Stunde kommt
    // aus x, der Wert aus der Reihe).
    const d = fl.querySelector('path').getAttribute('d');
    const xs = [], ys = [];
    for (const paar of d.slice(1).split(' ')) { const [x, y] = paar.split(','); xs.push(+x); ys.push(+y); }
    let aufReihe = 0, hintergrund = 0;
    const t0 = performance.now();
    for (let i = 0; i < 50; i++) {
      const px = r.left + (i + 0.5) / 50 * r.width;
      const stunde = vb.x + (px - r.left) / r.width * vb.width;
      let lo = 0, hi = xs.length - 1;
      while (lo < hi) { const m = (lo + hi) >> 1; if (xs[m] < stunde) lo = m + 1; else hi = m; }
      const py = r.top + (ys[lo] - vb.y) / vb.height * r.height;
      const el = document.elementFromPoint(px, py);
      if (el && el.classList && el.classList.contains('svgprobe-reihe')) aufReihe++;
      else if (el && el.tagName === 'rect') hintergrund++;
    }
    const ms = performance.now() - t0;
    const t1 = performance.now();
    let summe = 0;
    for (let i = 0; i < 10000; i++) {
      const vb = fl.viewBox.baseVal;
      summe += Math.round(vb.x + ((i % 1154) / 1154) * vb.width);
    }
    const msUmrechnung = performance.now() - t1;
    return { aufReihe, hintergrund, von: 50, ms: +ms.toFixed(2), msUmrechnung10000: +msUmrechnung.toFixed(2), summe };
  });
}

async function baumMessen(seite, cdp) {
  const baum = await seite.evaluate(() => {
    const svg = document.querySelector('#svgprobe-inhalt svg#svgprobe');
    const img = document.querySelector('#svgprobe-inhalt img');
    const stand = document.getElementById('svgprobe-stand');
    const el = svg || img;
    return {
      bytes: Number(stand.dataset.bytes),
      erzeugungMs: Number(stand.dataset.erzeugungMs),
      textIstText: document.body.innerText.includes('Reihe 1'),
      knoten: svg ? svg.querySelectorAll('*').length : 0,
      texte: svg ? svg.querySelectorAll('text').length : 0,
      pfadZeichen: svg ? [...svg.querySelectorAll('path')].reduce((s, p) => s + p.getAttribute('d').length, 0) : 0,
      breite: el ? el.getBoundingClientRect().width : 0,
      hoehe: el ? el.getBoundingClientRect().height : 0
    };
  });
  const m = await metriken(cdp);
  return { ...baum, heapMB: m ? +m.heapMB.toFixed(1) : null, knotenGesamt: m ? m.knoten : null };
}

// ------------------------------------------------------------ Ein Fall
async function fall(browser, f) {
  const kontext = await browser.newContext({ viewport: { width: 1400, height: 700 } });
  const seite = await kontext.newPage();
  const cdp = await kontext.newCDPSession(seite);
  try { await cdp.send('Performance.enable'); } catch { }

  const adresse = `${WURZEL}/svgprobe?variante=${f.variante}&reihen=${f.reihen}`;
  const tNav = Date.now();
  await seite.goto(adresse, { waitUntil: 'domcontentloaded' });
  await seite.waitForSelector(f.variante === 'png' ? '#svgprobe-inhalt img' : '#svgprobe-inhalt svg#svgprobe', { timeout: 30000 });
  const msBlazor = Date.now() - tNav;
  if (f.variante === 'png') await seite.waitForFunction(() => document.querySelector('#svgprobe-inhalt img').complete);
  await schlaf(300);

  const baum = await baumMessen(seite, cdp);
  const foto = async marke => { if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${f.name}_${marke}.png`, clip: { x: 0, y: 0, width: 1400, height: 560 } }); };
  await foto('1zu1');

  const rendern = await rendernMessen(seite, cdp, f.variante, f.reihen);
  let zoom = null, treffer = null;
  if (f.variante !== 'png') {
    zoom = await zoomMessen(seite, cdp);
    treffer = await trefferMessen(seite);
    await foto('gezoomt');
  }

  await kontext.close();
  return { ...f, adresse, msBlazor, baum, rendern, zoom, treffer };
}

function zeige(e) {
  console.log('');
  console.log(`=== ${e.name} ===  ${e.adresse}`);
  console.log(`  (a) Erzeugung im Kern ${e.baum.erzeugungMs} ms; Blazor bis Diagramm im Baum ${e.msBlazor} ms (Server + SignalR, nur Anhalt)`);
  const jv = e.rendern.jeVorgang;
  console.log(`  (b) Rendern netzfrei (${WIEDERHOLUNGEN}x): sync Median ${median(e.rendern.sync)} / Max ${maximum(e.rendern.sync)} ms; ` +
              `bis Bildaufbau Median ${median(e.rendern.rahmen)} / Max ${maximum(e.rendern.rahmen)} ms` +
              (jv ? `; Hauptfaden je Vorgang ${jv.task} ms (Layout ${jv.layout}, Stil ${jv.stil})` : ''));
  console.log(`  (f) Bytes ${e.baum.bytes} (${(e.baum.bytes / 1024).toFixed(1)} KB), Knoten im svg ${e.baum.knoten}, Texte ${e.baum.texte}, ` +
              `Pfadzeichen ${e.baum.pfadZeichen}, Knoten gesamt ${e.baum.knotenGesamt}, JS-Heap ${e.baum.heapMB} MB, ` +
              `Mass ${e.baum.breite}x${e.baum.hoehe}`);
  console.log(`  (e) Text ist Text (innerText findet "Reihe 1"): ${e.baum.textIstText}`);
  if (e.zoom && !e.zoom.fehler) {
    const js = e.zoom.jeSchritt;
    console.log(`  (c) Zoom ${ZOOMSCHRITTE} Schritte: sync Median ${median(e.zoom.zoomSync)} / Max ${maximum(e.zoom.zoomSync)} ms; ` +
                `bis Bildaufbau Median ${median(e.zoom.zoomRahmen)} / Max ${maximum(e.zoom.zoomRahmen)} ms  ` +
                `(Faktor ${e.zoom.faktor}, viewBox ${e.zoom.ende.split(/\s+/).map(v => (+v).toFixed(1)).join(' ')})`);
    console.log(`      Verschieben ${ZOOMSCHRITTE} Schritte: sync Median ${median(e.zoom.panSync)} / Max ${maximum(e.zoom.panSync)} ms; ` +
                `bis Bildaufbau Median ${median(e.zoom.panRahmen)} / Max ${maximum(e.zoom.panRahmen)} ms` +
                (js ? `; Hauptfaden je Schritt ${js.task} ms (Layout ${js.layout}, Stil ${js.stil})` : ''));
  }
  if (e.treffer) {
    console.log(`  (d) elementFromPoint 50x entlang Reihe 1: ${e.treffer.ms} ms, ${e.treffer.aufReihe}/${e.treffer.von} treffen die Linie, ` +
                `${e.treffer.hintergrund} den Hintergrund; Zeiger->Stunde 10 000x: ${e.treffer.msUmrechnung10000} ms`);
  }
}

// Sollwerte gelten fuer die Variante, die das Konzept vorschlaegt (gebuendelt):
// Einsetzen synchron <= 100 ms, ein Zoom- oder Verschiebeschritt bis zum
// Bildaufbau im Median <= 20 ms (kein ausgelassener Rahmen bei 60 Hz) und
// synchron <= 5 ms, Text ist Text. Roh, jeder n-te und PNG sind Vergleichswerte.
function pruefe(e) {
  const m = [];
  if (e.variante === 'png') return m;
  if (!e.baum.textIstText) m.push('Beschriftung ist kein Text');
  if (e.variante !== 'gebuendelt') return m;
  if (median(e.rendern.sync) > 100) m.push(`Einsetzen ${median(e.rendern.sync)} ms (soll <= 100)`);
  if (e.zoom) {
    if (median(e.zoom.zoomRahmen) > 20) m.push(`Zoomschritt bis Bildaufbau ${median(e.zoom.zoomRahmen)} ms (soll <= 20)`);
    if (median(e.zoom.panRahmen) > 20) m.push(`Verschiebeschritt bis Bildaufbau ${median(e.zoom.panRahmen)} ms (soll <= 20)`);
    if (median(e.zoom.zoomSync) > 5) m.push(`Zoomschritt synchron ${median(e.zoom.zoomSync)} ms (soll <= 5)`);
  }
  return m;
}

const FAELLE = [
  { name: 'G3_gebuendelt_3', variante: 'gebuendelt', reihen: 3 },
  { name: 'G6_gebuendelt_6', variante: 'gebuendelt', reihen: 6 },
  { name: 'R3_roh_3',        variante: 'roh',        reihen: 3 },
  { name: 'R6_roh_6',        variante: 'roh',        reihen: 6 },
  { name: 'N3_jedernte_3',   variante: 'jedernte',   reihen: 3 },
  { name: 'P3_png_3',        variante: 'png',        reihen: 3 },
  { name: 'P6_png_6',        variante: 'png',        reihen: 6 }
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
    if (m.length) { schlecht++; console.log('  SOLL VERFEHLT: ' + m.join('; ')); }
    else console.log(f.variante === 'gebuendelt' ? '  soll erfuellt' : '  (Vergleichswert)');
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
  ? `ALLE ${ergebnisse.length} FAELLE GEMESSEN, SOLLWERTE ERFUELLT.`
  : `${schlecht} FAELLE VERFEHLEN DIE SOLLWERTE.`);
if (process.env.SVGPROBE_JSON) { console.log('---JSON---'); console.log(JSON.stringify(ergebnisse, null, 1)); }
process.exit(schlecht === 0 ? 0 : 1);
