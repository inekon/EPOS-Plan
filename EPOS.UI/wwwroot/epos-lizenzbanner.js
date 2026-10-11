// Die Hoehe des Lizenzbanners als Token (Baustein Lizenzbanner der AppWurzel).
//
// WARUM ES DAS GIBT. Das Lizenzbanner steht in #app UEBER der Ansicht. Ein Dialog
// voller Hoehe (Katalogdialog, Katalogauswahl, Projektwahl, Chat ...) rechnet seine
// Hoehe aus dem Fenster: 100dvh minus die sicheren Abstaende. Das Banner kam in
// dieser Rechnung nicht vor - das Dokument rollte um genau seine Hoehe, und die
// Schlussleiste stand unter dem Fensterrand. Jede dieser Hoehenregeln zieht deshalb
// auch --epos-banner-oben ab (epos-ui.css, Token neben --epos-sicher-oben).
//
// WARUM GEMESSEN. Der Text des Banners bricht um (Lesemodus: zwei Saetze; im
// schmalen Fenster vier, fuenf Zeilen). Eine feste Hoehe schnitte ihn ab oder liesse
// eine Luecke. Ein ResizeObserver am Wirt des Banners setzt das Token deshalb auf
// dessen wirkliche Hoehe - auch auf 0, wenn der Hinweis verfaellt oder geschlossen
// wird (der Wirt bleibt dann leer stehen). Kein Zustand ausser dem Beobachter, keine
// Ablage, kein Netz. Wer das Modul nicht laden kann, sieht das Banner trotzdem - nur
// rollt das Dokument dann um seine Hoehe.

const TOKEN = "--epos-banner-oben";
const beobachter = new WeakMap();

/** Beobachtet den Wirt des Banners und setzt das Token an der Wurzel des Dokuments. */
export function anmelden(wirt) {
    if (!wirt) return;
    abmelden(wirt);
    const setzen = () => {
        const hoehe = wirt.isConnected ? wirt.getBoundingClientRect().height : 0;
        document.documentElement.style.setProperty(TOKEN, hoehe + "px");
    };
    setzen();
    if (typeof ResizeObserver === "undefined") return;
    const ro = new ResizeObserver(setzen);
    ro.observe(wirt);
    beobachter.set(wirt, ro);
}

/** Beendet die Beobachtung und nimmt das Token zurueck (Rueckfall 0px aus :root). */
export function abmelden(wirt) {
    const ro = wirt ? beobachter.get(wirt) : undefined;
    if (ro) {
        ro.disconnect();
        beobachter.delete(wirt);
    }
    document.documentElement.style.removeProperty(TOKEN);
}
