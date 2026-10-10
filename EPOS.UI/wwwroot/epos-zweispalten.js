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
//   4. Die MINDESTHOEHE des Bausteins (KB1, Konzept 4.8): die Summe der
//      Untergrenzen seiner Zeilen, gemessen, als --epos-zweispalten-min am
//      Dialog. Reicht das Fenster nicht dafuer, setzt das Skript
//      data-zweispalten-eng, und der Dialogkoerper rollt; sonst nie.
//      Aufgeklappt hat die Detailzeile Vorrang (DZ1, Konzept 4.4): Das Stilblatt stellt
//      Projektliste und Katalog auf ihre Untergrenzen und gibt der Satzflaeche den Rest;
//      die Untergrenze der Satzflaeche misst satzOffenMin, die Summe misst mindesthoehe.
//
// KEINE PIXELZAHL IM SKRIPT. Die Masse kommen aus dem gerechneten Stilblatt
// (min-height der Listen, Hoehen der Bereiche, Zeilenabstand). C# fuehrt die
// Hoehe der Projektliste in Pixeln der Normalstufe; in der Kompaktstufe
// rechnet das Skript gemessene Hoehen mit --epos-zeilenskala zurueck, damit
// Trennlinie, Grenzen und gemerkte Hoehe in beiden Stufen dasselbe meinen.
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

/** Die Zeilenskala der Stufe aus dem Stilblatt (Normalstufe 1, Kompaktstufe kleiner). */
export function skala(wurzel) {
    const s = wurzel ? parseFloat(getComputedStyle(wurzel).getPropertyValue("--epos-zeilenskala")) : NaN;
    return Number.isFinite(s) && s > 0 ? s : 1;
}

/**
 * Hoehe der Projektliste, ihre Untergrenze und die Obergrenze, die der Katalog laesst -
 * in Pixeln der Normalstufe (gemessen geteilt durch die Zeilenskala).
 */
export function masse(wurzel) {
    const projekt = wurzel && liste(wurzel, "projekt");
    if (!projekt) return null;
    const s = skala(wurzel);
    const hoehe = projekt.getBoundingClientRect().height;
    const min = px(getComputedStyle(projekt).minHeight);
    const katalog = liste(wurzel, "katalog");
    let spielraum = 0;
    if (katalog) {
        spielraum = Math.max(0, katalog.getBoundingClientRect().height - px(getComputedStyle(katalog).minHeight));
    }
    return { hoehe: Math.round(hoehe / s), min: Math.round(min / s), max: Math.round(Math.max(min, hoehe + spielraum) / s) };
}

/**
 * Mindesthoehe des Bausteins, wenn das Fenster sie nicht hergibt - sonst 0. Gemessen, nicht
 * gerechnet: Ohne Mindesthoehe nimmt der Baustein den Platz, den der Dialog ihm laesst; reicht er
 * nicht, stehen alle Rasterzeilen auf ihrer Untergrenze (Projektliste, Katalog mit Kopf und zwei
 * Zeilen, Detailzeile) und laufen ueber - ihre Summe samt Zeilenabstaenden ist die Mindesthoehe.
 * Gilt fuer jeden Wirt, gleich was seine Bereiche ausser den Listen tragen.
 */
export function mindesthoehe(wurzel) {
    const dialog = wurzel && wurzel.parentElement;
    if (!dialog) return 0;
    const alt = dialog.style.getPropertyValue("--epos-zweispalten-min");
    dialog.style.setProperty("--epos-zweispalten-min", "0px");
    const platz = wurzel.getBoundingClientRect().height;
    let summe = 0, n = 0;
    for (const kind of wurzel.children) {
        if (getComputedStyle(kind).display === "none") continue;
        n++;
        summe += kind.getBoundingClientRect().height;
    }
    summe += Math.max(0, n - 1) * px(getComputedStyle(wurzel).rowGap);
    if (alt) dialog.style.setProperty("--epos-zweispalten-min", alt); else dialog.style.removeProperty("--epos-zweispalten-min");
    return summe > platz + 1 ? Math.ceil(summe) : 0;
}

/**
 * Was die aufgeklappte Detailzeile mindestens von ihrem Inhalt zeigt: der Inhalt der Satzflaeche
 * (ohne ihr Polster), hoechstens --epos-satz-untergrenze aus dem Stilblatt; zugeklappt 0.
 */
export function satzOffenMin(wurzel) {
    const satz = wurzel && wurzel.querySelector(":scope > .epos-zweispalten-bereich--satz > .epos-zweispalten-satz");
    if (!satz || satz.hidden || !wurzel.classList.contains("epos-zweispalten--satz-offen")) return 0;
    const s = getComputedStyle(satz);
    const inhalt = satz.scrollHeight - px(s.paddingTop) - px(s.paddingBottom);
    return Math.max(0, Math.round(Math.min(inhalt, px(getComputedStyle(wurzel).getPropertyValue("--epos-satz-untergrenze")))));
}

/** Setzt Mindesthoehe und Rollschalter am Dialog (nur, wenn sich etwas aendert). */
function engPruefen(wurzel) {
    const dialog = wurzel.parentElement;
    if (!dialog || !dialog.classList.contains("epos-dialog") || !wurzel.isConnected) return;
    const offenMin = satzOffenMin(wurzel);
    if (Math.abs(px(dialog.style.getPropertyValue("--epos-satz-offen-min")) - offenMin) >= 1)
        dialog.style.setProperty("--epos-satz-offen-min", offenMin + "px");
    const min = mindesthoehe(wurzel);
    if (Math.abs(px(dialog.style.getPropertyValue("--epos-zweispalten-min")) - min) >= 1)
        dialog.style.setProperty("--epos-zweispalten-min", min + "px");
    const eng = dialog.scrollHeight > dialog.clientHeight + 1;
    if (eng !== dialog.hasAttribute("data-zweispalten-eng")) dialog.toggleAttribute("data-zweispalten-eng", eng);
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
        zug = { y: e.clientY, h: m.hoehe, min: m.min, max: m.max, s: skala(wurzel) };
        trenner.setPointerCapture(e.pointerId);
        wurzel.classList.add("epos-zweispalten--zieht");
        e.preventDefault();
    };
    const bewegt = e => {
        if (!zug) return;
        const h = Math.max(zug.min, Math.min(zug.max, zug.h + (e.clientY - zug.y) / zug.s));
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

    // Mindesthoehe und Rollschalter: bei jeder Groessenaenderung von Dialog, Baustein und Zeilen,
    // einmal je Bild (zweimal: der gesetzte Wert bestimmt erst, ob der Dialog ueberlaeuft).
    let bild = 0;
    const planen = () => {
        if (bild) return;
        bild = requestAnimationFrame(() => { bild = 0; engPruefen(wurzel); requestAnimationFrame(() => engPruefen(wurzel)); });
    };
    let beobachter = null;
    if (typeof ResizeObserver === "function") {
        beobachter = new ResizeObserver(planen);
        beobachter.observe(wurzel);
        if (wurzel.parentElement) beobachter.observe(wurzel.parentElement);
        for (const kind of wurzel.children) beobachter.observe(kind);
    }
    planen();
    wurzel.__eposZweispalten = { taste, doppel, beobachter };
}

/** Loest die Halter wieder (der Baustein wird verworfen). */
export function abmelden(wurzel) {
    if (!wurzel || !wurzel.__eposZweispalten) return;
    wurzel.removeEventListener("keydown", wurzel.__eposZweispalten.taste);
    wurzel.removeEventListener("dblclick", wurzel.__eposZweispalten.doppel);
    if (wurzel.__eposZweispalten.beobachter) wurzel.__eposZweispalten.beobachter.disconnect();
    delete wurzel.__eposZweispalten;
}
