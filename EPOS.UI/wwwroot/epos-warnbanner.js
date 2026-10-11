// Die Hoehe des Meldungsbanners im Fensterdialog als Token (Baustein Warnbanner).
//
// WARUM ES DAS GIBT. Ein Warnbanner, das unmittelbares Kind der Dialogwurzel ist,
// haftet im eigenen Fenster unter dem Kopf (epos-ui.css, „Dialog im eigenen Fenster").
// Ein Feld, das der Tabulator anspringt, rollt der Browser um scroll-padding-top
// herein - das Polster muss also die Hoehe des Banners kennen, sonst laege das Feld
// unter ihm.
//
// WARUM GEMESSEN. Eine feste Bannerzeile reicht nicht: der Text bricht um, und die
// aufgeklappte Hinweisliste (Hinweisklappe, bis 40 % der Fensterhoehe) macht das
// Banner um ein Vielfaches hoeher. Dasselbe Muster wie beim Lizenzbanner
// (epos-lizenzbanner.js, Token --epos-banner-oben): ein ResizeObserver setzt die
// wirkliche Hoehe als Token --epos-meldung-hoehe an der Wurzel des Dokuments. Das
// Stilblatt rechnet es nur, solange ein solches Banner steht (Polster mit Banner);
// ein stehengebliebener Wert ist danach wirkungslos, und das naechste Banner setzt
// ihn neu. Ein Banner an anderer Stelle (Ueberlagerung, Block, Lizenzbanner) meldet
// nichts. Kein Zustand ausser dem Beobachter, keine Ablage, kein Netz. Ohne das Modul
// gilt der Rueckfall des Stilblatts: eine Bannerzeile.

const TOKEN = "--epos-meldung-hoehe";
const ORT = "body > #app > .epos-dialog > .epos-warnbanner";
const beobachter = new WeakMap();

/** Beobachtet ein Warnbanner an der Dialogwurzel und setzt dessen Hoehe als Token. */
export function hoeheMelden(banner) {
    if (!banner || beobachter.has(banner) || !banner.isConnected || !banner.matches(ORT)) return;
    const setzen = () => {
        if (!banner.isConnected) {
            const ro = beobachter.get(banner);
            if (ro) ro.disconnect();
            beobachter.delete(banner);
            return;
        }
        document.documentElement.style.setProperty(TOKEN, banner.getBoundingClientRect().height + "px");
    };
    setzen();
    if (typeof ResizeObserver === "undefined") return;
    const ro = new ResizeObserver(setzen);
    ro.observe(banner);
    beobachter.set(banner, ro);
}
