# -*- coding: utf-8 -*-
r"""Sammel-Upload der Wiki-Seiten und -Dateien von EPOS-Plan nach wiki.epos-plan.de (MediaWiki-API).

Aufruf (Anwender, nicht Agent; Zugangsdaten nur als Umgebungsvariablen, nie als Argument):
    set WIKI_BOT_USER=<Benutzer@Botname>      (Spezial:BotPasswords; noetige Rechte: "Seiten bearbeiten" und
                                                "Seiten erstellen" fuer --seiten/--weiterleitungen/--logbuch, dazu fuer
                                                --dateien "Hochladen neuer Dateien" (uploadfile) und "Hochladen, Ersetzen
                                                und Verschieben von Dateien" (uploadeditmovefile))
    set WIKI_BOT_PASS=<Bot-Passwort>
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --trocken            (zeigt nur den Plan, laedt nichts)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --dateien            (laedt die Dateien aus dateien.tsv)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --seiten             (laedt die Seiten aus seiten.tsv)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --logbuch            (haengt den Abschnitt aus --logbuchdatei ins Update-Logbuch)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --weiterleitungen    (legt die Weiterleitungen aus weiterleitungen.tsv an, nach den Seiten)
Reihenfolge (Trockenlauf wie voller Lauf): Dateien vor Seiten vor Weiterleitungen vor Logbuch.
Optionen: --repo <Repo-Wurzel> (Vorgabe: zwei Ordner ueber diesem Skript oder EPOS_REPO), --nur <n,n,...>, --zusammenfassung "<Text>",
          --dateiliste <Datei> (Vorgabe dateien.tsv neben dem Skript), --logbuchdatei <Datei> (Vorgabe logbuch.wiki neben dem Skript),
          --marke "<Ueberschrift>" (Einfuegemarke, Vorgabe "== Version 1.2.0.0"), --version <a.b.c.d> (Abschnittsname, der schon
          vorhanden sein kann).
Ablauf je Seite (Hilfesystem-Konzept, Regel 3 und 13.3): Live-Stand lesen (action=raw), mit Repo-Quelle vergleichen, bei Gleichheit
ueberspringen, sonst Text vollstaendig ersetzen (bot=1), danach action=raw byte-gleich pruefen und action=parse auf Warnungen pruefen.
Kategorie und Anker stehen in der Repo-Quelle (UTF-8, ein BOM wird beim Lesen entfernt).
Ablauf je Datei (dateien.tsv: Dateiname TAB Repo-Pfad TAB Beschreibung): SHA-1 der lokalen Datei bilden, Live-Stand ueber
prop=imageinfo (iiprop=sha1) lesen: NEU (Seite fehlt), GLEICH (gleicher SHA-1, kein Upload) oder ERSATZ. Davor steht je Lauf
eine Pruefung von meta=siteinfo (uploadsenabled, fileextensions) und je SVG eine lokale Pruefung auf <script>, on...=-Attribute,
<foreignObject>, externe href/xlink:href (http/https) und <title> - jeder Treffer ist ein Abbruch ohne Upload, im Trockenlauf
nur eine Meldung. Der Upload selbst laeuft als action=upload mit multipart/form-data (filename, file, comment, text = Beschreibung
plus [[Kategorie:Grafiken]], token; ignorewarnings=1 nur bei ERSATZ), danach Ruecklese ueber imageinfo: SHA-1 muss zur lokalen
Datei passen. Das Skript aendert keine Datei im Repository.
"""
import os, sys, json, io, re, hashlib, mimetypes, urllib.request, urllib.parse, http.cookiejar, time

API = "https://wiki.epos-plan.de/api.php"
RAW = "https://wiki.epos-plan.de/index.php?title={t}&action=raw"
HIER = os.path.dirname(os.path.abspath(__file__))
REPO = os.environ.get("EPOS_REPO", os.path.abspath(os.path.join(HIER, "..", "..")))
ZUSAMMENFASSUNG = "Sammel-Upload: Repo-Quelle Projekte/Wiki (Dokumentation/aktuell/Wiki_Update_*.md)"
UA = "EPOS-Plan Wiki-Upload (Orchestrierung; Kontakt siehe Repository)"

cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))

def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with opener.open(req, timeout=60) as r:
        return r.status, r.read().decode("utf-8")

def api(params, post=False):
    params = dict(params); params["format"] = "json"
    data = urllib.parse.urlencode(params).encode("utf-8")
    if post:
        req = urllib.request.Request(API, data=data, headers={"User-Agent": UA})
    else:
        req = urllib.request.Request(API + "?" + data.decode("utf-8"), headers={"User-Agent": UA})
    with opener.open(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))

def live_raw(title):
    try:
        st, txt = get(RAW.format(t=urllib.parse.quote(title.replace(" ", "_"))))
        return txt
    except urllib.error.HTTPError as e:
        if e.code == 404:
            return None
        raise

def norm(s):
    return s.replace("\r\n", "\n").rstrip("\n")

def anmelden():
    user = os.environ.get("WIKI_BOT_USER"); pw = os.environ.get("WIKI_BOT_PASS")
    if not user or not pw:
        sys.exit("Zugangsdaten fehlen: WIKI_BOT_USER und WIKI_BOT_PASS als Umgebungsvariablen setzen (Bot-Passwort).")
    tok = api({"action": "query", "meta": "tokens", "type": "login"})["query"]["tokens"]["logintoken"]
    r = api({"action": "login", "lgname": user, "lgpassword": pw, "lgtoken": tok}, post=True)
    if r.get("login", {}).get("result") != "Success":
        sys.exit("Anmeldung fehlgeschlagen: " + json.dumps(r, ensure_ascii=False))
    csrf = api({"action": "query", "meta": "tokens", "type": "csrf"})["query"]["tokens"]["csrftoken"]
    print("Angemeldet als", r["login"].get("lgusername", user))
    return csrf

def bearbeite(title, text, summary, csrf, trocken):
    if trocken:
        print("   [trocken] wuerde speichern:", title, len(text), "Zeichen"); return True
    r = api({"action": "edit", "title": title, "text": text, "summary": summary, "bot": 1, "token": csrf,
             "contentformat": "text/x-wiki", "contentmodel": "wikitext"}, post=True)
    if r.get("edit", {}).get("result") != "Success":
        print("   FEHLER beim Speichern:", json.dumps(r, ensure_ascii=False)[:400]); return False
    print("   gespeichert, Revision", r["edit"].get("newrevid"))
    time.sleep(1.0)
    live = live_raw(title)
    if live is None or norm(live) != norm(text):
        print("   WARNUNG: Ruecklese nicht byte-gleich!"); return False
    p = api({"action": "parse", "page": title, "prop": "parsewarnings|categories"})
    warn = p.get("parse", {}).get("parsewarnings", [])
    cats = [c.get("*") or c.get("category") for c in p.get("parse", {}).get("categories", [])]
    print("   Ruecklese byte-gleich; Parse-Warnungen:", len(warn), "| Kategorien:", cats)
    return not warn

SVG_VERBOTENE_MUSTER = [
    (re.compile(r"<\s*script", re.IGNORECASE), "<script>"),
    (re.compile(r"\bon[a-zA-Z]+\s*=", re.IGNORECASE), "on...=-Attribut"),
    (re.compile(r"<\s*foreignObject", re.IGNORECASE), "<foreignObject>"),
    (re.compile(r'(?:xlink:href|href)\s*=\s*["\']\s*https?://', re.IGNORECASE), "externe href/xlink:href"),
    (re.compile(r"<\s*title", re.IGNORECASE), "<title>"),
]

def svg_pruefen(pfad):
    """Liefert die Namen aller verbotenen Muster, die die lokale SVG-Datei enthaelt (leer = unbedenklich)."""
    text = io.open(pfad, encoding="utf-8", errors="replace").read()
    return [meldung for muster, meldung in SVG_VERBOTENE_MUSTER if muster.search(text)]

def sha1_datei(pfad):
    h = hashlib.sha1()
    with open(pfad, "rb") as f:
        for block in iter(lambda: f.read(1 << 16), b""):
            h.update(block)
    return h.hexdigest()

def siteinfo_uploads():
    """(uploadsenabled?, Menge der zugelassenen Dateiendungen) nach meta=siteinfo."""
    r = api({"action": "query", "meta": "siteinfo", "siprop": "general|fileextensions"})
    allgemein = r.get("query", {}).get("general", {})
    endungen = set((e.get("ext") or "").lower() for e in r.get("query", {}).get("fileextensions", []))
    return "uploadsenabled" in allgemein, endungen

def datei_info(name):
    """imageinfo (sha1, size, mime) der Datei "name" im Wiki, None wenn sie dort nicht existiert."""
    r = api({"action": "query", "titles": "Datei:%s" % name, "prop": "imageinfo", "iiprop": "sha1|size|mime"})
    for seite in r.get("query", {}).get("pages", {}).values():
        if "missing" in seite:
            return None
        ii = seite.get("imageinfo")
        if ii:
            return ii[0]
    return None

def multipart_body(felder, dateiname, dateibytes, mime):
    grenze = "EPOSWikiUpload" + os.urandom(16).hex()
    teile = []
    for k, v in felder.items():
        teile.append(('--%s\r\nContent-Disposition: form-data; name="%s"\r\n\r\n%s\r\n' % (grenze, k, v)).encode("utf-8"))
    teile.append(('--%s\r\nContent-Disposition: form-data; name="file"; filename="%s"\r\nContent-Type: %s\r\n\r\n'
                  % (grenze, dateiname, mime)).encode("utf-8"))
    teile.append(dateibytes)
    teile.append(b"\r\n--%s--\r\n" % grenze.encode("ascii"))
    return b"".join(teile), grenze

def hochladen(dateiname, pfad, text, comment, csrf, ignorewarnings):
    inhalt = io.open(pfad, "rb").read()
    mime = mimetypes.guess_type(dateiname)[0] or "application/octet-stream"
    felder = {"action": "upload", "filename": dateiname, "comment": comment, "text": text,
              "token": csrf, "format": "json"}
    if ignorewarnings:
        felder["ignorewarnings"] = "1"
    body, grenze = multipart_body(felder, dateiname, inhalt, mime)
    req = urllib.request.Request(API, data=body,
                                  headers={"User-Agent": UA, "Content-Type": "multipart/form-data; boundary=%s" % grenze})
    with opener.open(req, timeout=120) as r:
        return json.loads(r.read().decode("utf-8"))

def dateien(trocken, csrf, nur, repo, comment, dateiliste):
    """Laedt jede Datei aus dateien.tsv (Dateiname TAB Repo-Pfad TAB Beschreibung) als Wiki-Datei hoch.

    Vorpruefung je Lauf: meta=siteinfo (uploadsenabled, fileextensions) - fehlt eines davon fuer eine Zeile, klarer
    Abbruch dieser Zeile ohne Upload (im Trockenlauf nur eine Meldung, da ohnehin nichts gespeichert wird). Vorpruefung
    je SVG-Datei: svg_pruefen(); ein Treffer ist immer ein Abbruch dieser Zeile, auch im Trockenlauf.
    """
    if not os.path.exists(dateiliste):
        print("Dateien: Liste nicht gefunden:", dateiliste); return
    inhalt = io.open(dateiliste, encoding="utf-8").read().strip()
    if not inhalt:
        print("Dateien: dateien.tsv ist leer - nichts zu tun."); return
    tsv = inhalt.split("\n")
    uploads_an, erlaubte_endungen = siteinfo_uploads()
    ok = 0; fehl = 0
    for i, zeile in enumerate(tsv, 1):
        if nur and i not in nur:
            continue
        dateiname, quelle, beschreibung = zeile.split("\t")
        voll = os.path.join(repo, quelle.replace("/", os.sep))
        if not os.path.exists(voll):
            print("%2d Datei:%s -> Quelle fehlt: %s" % (i, dateiname, quelle)); fehl += 1; continue
        endung = os.path.splitext(dateiname)[1].lstrip(".").lower()
        if endung == "svg":
            svg_fehler = svg_pruefen(voll)
            if svg_fehler:
                print("%2d Datei:%s -> ABBRUCH: unzulaessiger SVG-Inhalt (%s)" % (i, dateiname, ", ".join(svg_fehler)))
                fehl += 1; continue
        sha1_lokal = sha1_datei(voll)
        info = datei_info(dateiname)
        art = "NEU" if info is None else ("GLEICH" if info.get("sha1") == sha1_lokal else "ERSATZ")
        print("%2d Datei:%s -> %s (sha1 %s...)" % (i, dateiname, art, sha1_lokal[:12]))
        if art == "GLEICH":
            continue
        if not uploads_an or endung not in erlaubte_endungen:
            grund = "Uploads sind auf dem Wiki nicht aktiviert" if not uploads_an else ("Endung .%s ist nicht zugelassen" % endung)
            print("    ABBRUCH: %s - kein Upload." % grund); fehl += 1; continue
        if trocken:
            print("   [trocken] wuerde hochladen:", dateiname, "(%s)" % art); ok += 1; continue
        text = "%s\n\n[[Kategorie:Grafiken]]" % beschreibung
        r = hochladen(dateiname, voll, text, comment, csrf, art == "ERSATZ")
        up = r.get("upload", {})
        if up.get("result") != "Success":
            print("   FEHLER beim Hochladen:", json.dumps(r, ensure_ascii=False)[:400]); fehl += 1; continue
        print("   hochgeladen:", up.get("filename", dateiname))
        time.sleep(1.0)
        probe = datei_info(dateiname)
        if probe is None or probe.get("sha1") != sha1_lokal:
            print("   WARNUNG: Ruecklese-SHA-1 stimmt nicht mit der lokalen Datei ueberein!"); fehl += 1; continue
        print("   Ruecklese-SHA-1 stimmt mit der lokalen Datei ueberein.")
        ok += 1
    print("Dateien: %d hochgeladen, %d mit Fehler/Uebersprungen" % (ok, fehl))

def seiten(trocken, csrf, nur, repo, summary):
    tsv = io.open(os.path.join(HIER, "seiten.tsv"), encoding="utf-8").read().strip().split("\n")
    ok = 0; fehl = 0
    for i, zeile in enumerate(tsv, 1):
        if nur and i not in nur:
            continue
        title, quelle = zeile.split("\t")
        voll = os.path.join(repo, quelle.replace("/", os.sep))
        if not os.path.exists(voll):
            print("%2d %s -> Quelle fehlt: %s" % (i, title, quelle)); fehl += 1; continue
        text = io.open(voll, encoding="utf-8-sig").read()
        live = live_raw(title)
        art = "NEU" if live is None else ("GLEICH" if norm(live) == norm(text) else "ERSATZ")
        print("%2d %s -> %s (%d Zeichen)" % (i, title, art, len(text)))
        if art == "GLEICH":
            continue
        if bearbeite(title, text, summary, csrf, trocken):
            ok += 1
        else:
            fehl += 1
    print("Seiten: %d gespeichert, %d mit Fehler/Warnung" % (ok, fehl))

def weiterleitungen(trocken, csrf, nur, summary):
    """Macht jede Seite aus weiterleitungen.tsv (Titel TAB Ziel) zur Weiterleitung auf ihr Ziel.

    Laeuft nach den Seiten: Das Ziel muss stehen. MediaWiki folgt keiner doppelten Weiterleitung -
    deshalb stehen die Synonyme, die auf eine umgeleitete Seite zeigten, mit ihrem neuen Ziel direkt
    in der Liste.
    """
    tsv = io.open(os.path.join(HIER, "weiterleitungen.tsv"), encoding="utf-8").read().strip().split("\n")
    ok = 0; fehl = 0
    for i, zeile in enumerate(tsv, 1):
        if nur and i not in nur:
            continue
        title, ziel = zeile.split("\t")
        text = "#WEITERLEITUNG [[%s]]" % ziel
        live = live_raw(title)
        art = "NEU" if live is None else ("GLEICH" if norm(live) == norm(text) else "ERSATZ")
        print("%2d %s -> %s (%s)" % (i, title, ziel, art))
        if art == "GLEICH":
            continue
        if bearbeite(title, text, summary, csrf, trocken):
            ok += 1
        else:
            fehl += 1
    print("Weiterleitungen: %d gespeichert, %d mit Fehler/Warnung" % (ok, fehl))

def logbuch(trocken, csrf, datei, marke, version):
    neu = io.open(datei, encoding="utf-8").read().strip("\n") + "\n\n"
    title = "Update-Logbuch"
    live = live_raw(title)
    if live is None:
        sys.exit("Update-Logbuch nicht gefunden.")
    if version and ("== Version %s" % version) in live:
        print("Logbuch: Abschnitt", version, "ist schon vorhanden - nichts zu tun."); return
    if marke not in live:
        sys.exit("Logbuch: Einfuegemarke %r nicht gefunden." % marke)
    text = live.replace(marke, neu + marke, 1)
    print("Logbuch: fuege", neu.count("\n* "), "Saetze vor %r ein" % marke)
    bearbeite(title, text, "Update-Logbuch: Version %s (Sammel-Upload)" % (version or "?"), csrf, trocken)

if __name__ == "__main__":
    a = sys.argv[1:]
    def opt(name, vorgabe=None):
        return a[a.index(name) + 1] if name in a else vorgabe
    trocken = "--trocken" in a or not ("--seiten" in a or "--logbuch" in a or "--weiterleitungen" in a or "--dateien" in a)
    repo = opt("--repo", REPO)
    nur = set(int(x) for x in opt("--nur").split(",")) if "--nur" in a else None
    summary = opt("--zusammenfassung", ZUSAMMENFASSUNG)
    dateiliste = opt("--dateiliste", os.path.join(HIER, "dateien.tsv"))
    lbd = opt("--logbuchdatei", os.path.join(HIER, "logbuch.wiki"))
    marke = opt("--marke", "== Version 1.2.0.0"); version = opt("--version")
    csrf = None if trocken else anmelden()
    if trocken:
        print("TROCKENLAUF - es wird nichts hochgeladen.")
    if "--dateien" in a or trocken:
        dateien(trocken, csrf, nur if "--dateien" in a else None, repo, summary, dateiliste)
    if "--seiten" in a or trocken:
        seiten(trocken, csrf, nur, repo, summary)
    if "--weiterleitungen" in a or trocken:
        weiterleitungen(trocken, csrf, nur if "--weiterleitungen" in a else None, summary)
    if "--logbuch" in a or (trocken and os.path.exists(lbd)):
        logbuch(trocken, csrf, lbd, marke, version)
