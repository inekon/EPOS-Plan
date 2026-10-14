// =====================================================================
//  ROLLPROBE „BERICHTE & KOSTEN" — bleibt die Reiterleiste beim Wechsel oben?
// =====================================================================
//
//  WOZU. Jede der vier Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht)
//  fokussiert beim ersten Zeichnen ihre Wurzel (`.epos-seite`, tabindex -1),
//  damit die Tastaturbedienung dort beginnt. Ein Fokus OHNE preventScroll rollt
//  den Browser so, dass das Element „sichtbar" wird — ist die Seite höher als
//  das Fenster, zentriert Chromium sie und schiebt die Reiterleiste aus dem
//  Blick. bunit misst keine Rollposition; diese Probe klickt im echten Chromium
//  nacheinander auf die vier Reiter und misst nach jedem Klick:
//    - die Rollposition des Fensters und jedes rollenden Vorfahren der Seite,
//    - ob die Reiterleiste (`[role=tablist]` des Bereichs) im Fenster liegt,
//    - ob die neue Seite oben beginnt (Oberkante der `.epos-seite` im Fenster),
//    - ob der Fokus auf der Seitenwurzel steht (die Tastaturbedienung bleibt).
//
//  AUFRUF (der Wirt muss laufen, siehe LIESMICH.md):
//    node berichtescrollprobe.mjs --url http://127.0.0.1:5299 [--fotos <ordner>]
//    node berichtescrollprobe.mjs --gegenprobe      # Fokus ohne preventScroll nachgestellt: muss rot sein
//
//  Rückgabe 0 = kein Verstoß, 1 = mindestens einer, 2 = Aufbaufehler.

import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { execSync } from 'node:child_process';

const { chromium } = await (async () => {
  try { return await import('playwright'); } catch { /* nicht lokal installiert */ }
  const wurzel = (process.env.NODE_PATH || execSync('npm root -g').toString()).trim();
  return createRequire(wurzel + '/berichtescrollprobe.cjs')('playwright');
})();

const arg = (name, vorgabe) => {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : vorgabe;
};
const WURZEL = arg('url', 'http://127.0.0.1:5299');
const FOTOS = arg('fotos', '');
const GEGENPROBE = process.argv.includes('--gegenprobe');
const REITER = ['UEBERSICHT', 'KOSTEN', 'WIRTSCHAFT', 'BERICHT'];

// Fenster niedriger als jede der vier Seiten — sonst rollt auch ein ungeschützter Fokus nicht.
const FENSTER = [
  { name: 'b1280x600', breite: 1280, hoehe: 600 },
  { name: 'b820x700', breite: 820, hoehe: 700 },
];

const verstoesse = [];
const melde = (fall, text) => { verstoesse.push(fall + ': ' + text); console.log('  VERSTOSS ' + fall + ': ' + text); };

let browser;
try {
  browser = await chromium.launch();
  if (FOTOS) await mkdir(FOTOS, { recursive: true });

  for (const f of FENSTER) {
    const seite = await browser.newPage({ viewport: { width: f.breite, height: f.hoehe } });
    await seite.goto(WURZEL + '/berichtekosten?kultur=de-DE');
    await seite.waitForSelector('button[id^="bk-reiter-"]', { timeout: 15000 });
    await seite.waitForTimeout(500);

    // Reihenfolge wie beim Anwender: von Übersicht aus nach rechts, dann zurück.
    for (const r of [...REITER.slice(1), 'UEBERSICHT', 'BERICHT', 'KOSTEN', 'WIRTSCHAFT']) {
      const fall = f.name + '_' + r;
      const knopf = seite.locator('button[id^="bk-reiter-"][id$="' + r + '"]').first();
      if (await knopf.count() === 0) { melde(fall, 'Reiter nicht gefunden'); continue; }
      await knopf.click();
      // Die neue Seite zeichnet und fokussiert ihre Wurzel in OnAfterRender.
      await seite.waitForFunction((k) => {
        const b = document.querySelector('button[id^="bk-reiter-"][id$="' + k + '"]');
        return b && b.getAttribute('aria-selected') === 'true';
      }, r, { timeout: 5000 });
      await seite.waitForTimeout(400);

      if (GEGENPROBE) {
        // Der Stand vor der Behebung: Fokus ohne preventScroll auf die Wurzel der neuen Seite.
        await seite.evaluate(() => {
          const w = [...document.querySelectorAll('.epos-berichtekosten .epos-seite')]
            .find(e => e.offsetParent !== null);
          if (w) { w.blur(); w.focus(); }
        });
        await seite.waitForTimeout(200);
      }

      const m = await seite.evaluate(() => {
        const bereich = document.querySelector('.epos-berichtekosten');
        const leiste = bereich.querySelector('[role="tablist"]');
        const wurzel = [...bereich.querySelectorAll('.epos-seite')].find(e => e.offsetParent !== null);
        const rollende = [];
        for (let e = wurzel; e; e = e.parentElement) {
          if (e.scrollTop > 0) rollende.push((e.className || e.tagName).toString().slice(0, 40) + '=' + e.scrollTop);
        }
        const l = leiste.getBoundingClientRect();
        const s = wurzel ? wurzel.getBoundingClientRect() : null;
        return {
          fensterY: Math.round(window.scrollY),
          rollende,
          leisteOben: Math.round(l.top), leisteUnten: Math.round(l.bottom),
          seiteOben: s ? Math.round(s.top) : null,
          seiteHoehe: s ? Math.round(s.height) : null,
          fensterHoehe: window.innerHeight,
          fokusAufWurzel: !!wurzel && document.activeElement === wurzel,
        };
      });
      console.log(`  ${fall.padEnd(24)} Fenster-Y ${String(m.fensterY).padStart(4)}  Leiste ${m.leisteOben}..${m.leisteUnten}  ` +
                  `Seite oben ${m.seiteOben} (Höhe ${m.seiteHoehe}/${m.fensterHoehe})  Fokus ${m.fokusAufWurzel ? 'Wurzel' : '-'}` +
                  (m.rollende.length ? '  rollt: ' + m.rollende.join(', ') : ''));
      if (FOTOS) await seite.screenshot({ path: `${FOTOS}/${fall}.png` });

      if (!GEGENPROBE && m.fensterY === 0 && m.seiteOben !== null && m.seiteOben + m.seiteHoehe <= m.fensterHoehe)
        console.log(`    Hinweis ${fall}: Seite passt ganz ins Fenster — ein Fokus rollt hier ohnehin nicht`);
      if (m.fensterY !== 0 || m.rollende.length) melde(fall, `Ansicht gerollt (Fenster-Y ${m.fensterY}${m.rollende.length ? ', ' + m.rollende.join(', ') : ''})`);
      if (m.leisteOben < 0 || m.leisteUnten > m.fensterHoehe) melde(fall, `Reiterleiste nicht im Fenster (${m.leisteOben}..${m.leisteUnten})`);
      if (m.seiteOben === null || m.seiteOben < m.leisteUnten - 1) melde(fall, `Seite beginnt nicht unter der Leiste (oben ${m.seiteOben})`);
      if (!m.fokusAufWurzel) melde(fall, 'Fokus nicht auf der Seitenwurzel');
    }
    await seite.close();
  }
} catch (e) {
  console.error('Aufbaufehler: ' + (e?.stack || e));
  if (browser) await browser.close();
  process.exit(2);
}
await browser.close();

console.log(verstoesse.length === 0
  ? 'ROLLPROBE GRÜN — die Reiterleiste bleibt bei allen vier Reitern sichtbar.'
  : `ROLLPROBE ROT — ${verstoesse.length} Verstoß/Verstöße.`);
process.exit(verstoesse.length === 0 ? 0 : 1);
