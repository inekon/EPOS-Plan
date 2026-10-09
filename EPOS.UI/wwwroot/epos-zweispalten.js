// Die Katalogauswahl V1 "Gerahmt und gestapelt" (Konzept Projektdialoge mit
// Katalogauswahl, 4.3 bis 4.7) - was der Baustein Zweispaltenauswahl in C#
// nicht kann:
//
//   1. Die Trennlinie ZIEHEN. Waehrend des Ziehens setzt das Skript die Hoehe
//      der Projektliste unmittelbar (--epos-trenner-hoehe), ohne Rundweg zum
//      Server; erst nach dem Loslassen meldet es die gemessene Hoehe an C#,
//      das sie klemmt und ueber Dienste.Einstellungen merkt.
//   2. MESSEN, wie hoch die Projektliste werden darf: so hoch, dass die
//      Katalogliste ihre Mindesthoehe (Kopfzeile plus zwei Zeilen) behaelt.
//   3. Enter und Doppelklick NUR auf Listenzeilen: Enter im Suchfeld oder auf
//      einem Knopf bleibt, was es ist. Die Taste wird hier angehalten, damit
//      der Dialog sie nicht zusaetzlich als OK nimmt.
//
// Wer das Modul nicht laden kann (bunit), bedient die Trennlinie weiter per
// Tastatur; das Raster klemmt die Hoehe ohnehin im Stilblatt. Kein Netz, keine
// Ablage.

function liste(wurzel, bereich) {
    return wurzel.querySelector(".epos-zweispalten-bereich--" + bereich + " .epos-raster-huelle");
}

function px(wert) {
    const n = parseFloat(wert);
    return Number.isFinite(n) ? n : 0;
}

/** Hoehe der Projektliste, ihre Untergrenze und die Obergrenze, die der Katalog laesst. */
export function masse(wurzel) {
    const projekt = wurzel && liste(wurzel, "projekt");
    if (!projekt) return null;
    const hoehe = projekt.getBoundingClientRect().height;
    const min = px(getComputedStyle(projekt).minHeight);
    const katalog = liste(wurzel, "katalog");
    let spielraum = 0;
    if (katalog) {
        spielraum = Math.max(0, katalog.getBoundingClientRect().height - px(getComputedStyle(katalog).minHeight));
    }
    return { hoehe: Math.round(hoehe), min: Math.round(min), max: Math.round(Math.max(min, hoehe + spielraum)) };
}

// Ziele, die Enter selbst brauchen. Der Wahlknopf einer Zeile (Zeilenwahl) gehoert nicht dazu:
// Enter auf ihm waehlt die Zeile (Klick) und uebernimmt sie danach.
const TEXTZIELE = "input:not([type=radio]):not([type=checkbox]), textarea, select, "
                + "button:not(.epos-anlagenwahl):not(.epos-zeilenwahl--breit), [contenteditable=true]";

/** Haengt Ziehen, Enter, Entf, Esc und Doppelklick an die Wurzel des Bausteins - einmal je Element. */
export function anmelden(wurzel, dotnet) {
    if (!wurzel || wurzel.__eposZweispalten) return;
    const trenner = wurzel.querySelector(":scope > .epos-zweispalten-trenner");
    let zug = null;

    const unten = e => {
        const m = masse(wurzel);
        if (!m) return;
        zug = { y: e.clientY, h: m.hoehe, min: m.min, max: m.max };
        trenner.setPointerCapture(e.pointerId);
        wurzel.classList.add("epos-zweispalten--zieht");
        e.preventDefault();
    };
    const bewegt = e => {
        if (!zug) return;
        const h = Math.max(zug.min, Math.min(zug.max, zug.h + e.clientY - zug.y));
        wurzel.style.setProperty("--epos-trenner-hoehe", Math.round(h) + "px");
    };
    const los = () => {
        if (!zug) return;
        zug = null;
        wurzel.classList.remove("epos-zweispalten--zieht");
        const m = masse(wurzel);
        if (m) dotnet.invokeMethodAsync("TrennerGezogen", m.hoehe);
    };
    if (trenner) {
        trenner.addEventListener("pointerdown", unten);
        trenner.addEventListener("pointermove", bewegt);
        trenner.addEventListener("pointerup", los);
        trenner.addEventListener("pointercancel", los);
    }

    const taste = e => {
        if (e.key === "Escape" && wurzel.classList.contains("epos-zweispalten--satz-offen")
            && !e.target.closest(".epos-ueberlagerung")) {
            e.preventDefault(); e.stopPropagation();
            dotnet.invokeMethodAsync("SatzSchliessen");
            return;
        }
        if (e.target.closest(TEXTZIELE)) return;
        const huelle = e.target.closest(".epos-raster-huelle");
        if (!huelle || !wurzel.contains(huelle)) return;
        const bereich = huelle.closest(".epos-zweispalten-bereich--katalog") ? "Katalog"
                      : huelle.closest(".epos-zweispalten-bereich--projekt") ? "Projekt" : null;
        if (!bereich) return;
        let name = null;
        if (e.key === "Enter") name = "Enter";
        else if (e.key === "Delete" && bereich === "Projekt") name = "Entf";
        else if ((e.key === "a" || e.key === "A") && (e.ctrlKey || e.metaKey)
                 && huelle.closest("[data-mehrfach]")) name = "Alle";
        if (!name) return;
        e.stopPropagation();
        if (name === "Enter" && e.target.closest("button")) {
            // Erst waehlt der Klick des Wahlknopfs die Zeile, dann wird uebernommen.
            setTimeout(() => dotnet.invokeMethodAsync("ListenTaste", bereich, name), 0);
            return;
        }
        e.preventDefault();
        dotnet.invokeMethodAsync("ListenTaste", bereich, name);
    };
    const doppel = e => {
        if (e.target.closest(TEXTZIELE) || e.target.closest("thead")) return;
        if (!e.target.closest("tbody tr")) return;
        const huelle = e.target.closest(".epos-zweispalten-bereich--katalog .epos-raster-huelle");
        if (!huelle || !wurzel.contains(huelle)) return;
        dotnet.invokeMethodAsync("ListenTaste", "Katalog", "Enter");
    };
    wurzel.addEventListener("keydown", taste);
    wurzel.addEventListener("dblclick", doppel);
    wurzel.__eposZweispalten = { taste, doppel };
}

/** Loest die Halter wieder (der Baustein wird verworfen). */
export function abmelden(wurzel) {
    if (!wurzel || !wurzel.__eposZweispalten) return;
    wurzel.removeEventListener("keydown", wurzel.__eposZweispalten.taste);
    wurzel.removeEventListener("dblclick", wurzel.__eposZweispalten.doppel);
    delete wurzel.__eposZweispalten;
}
