# -*- coding: utf-8 -*-
r"""Sammel-Upload der Wiki-Seiten von EPOS-Plan nach wiki.epos-plan.de (MediaWiki-API).

Aufruf (Anwender, nicht Agent; Zugangsdaten nur als Umgebungsvariablen, nie als Argument):
    set WIKI_BOT_USER=<Benutzer@Botname>      (Spezial:BotPasswords, Rechte: Seiten bearbeiten, Seiten erstellen)
    set WIKI_BOT_PASS=<Bot-Passwort>
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --trocken            (zeigt nur den Plan, laedt nichts)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --seiten             (laedt die Seiten aus seiten.tsv)
    py -3 Werkzeuge\WikiUpload\wiki_upload.py --logbuch            (haengt den Abschnitt aus --logbuchdatei ins Update-Logbuch)
Optionen: --repo <Repo-Wurzel> (Vorgabe: zwei Ordner ueber diesem Skript oder EPOS_REPO), --nur <n,n,...>, --zusammenfassung "<Text>",
          --logbuchdatei <Datei> (Vorgabe logbuch.wiki neben dem Skript), --marke "<Ueberschrift>" (Einfuegemarke, Vorgabe
          "== Version 1.2.0.0"), --version <a.b.c.d> (Abschnittsname, der schon vorhanden sein kann).
Ablauf je Seite (Hilfesystem-Konzept, Regel 3 und 13.3): Live-Stand lesen (action=raw), mit Repo-Quelle vergleichen, bei Gleichheit
ueberspringen, sonst Text vollstaendig ersetzen (bot=1), danach action=raw byte-gleich pruefen und action=parse auf Warnungen pruefen.
Kategorie und Anker stehen in der Repo-Quelle (UTF-8, ein BOM wird beim Lesen entfernt). Das Skript aendert keine Datei im Repository.
"""
import os, sys, json, io, urllib.request, urllib.parse, http.cookiejar, time

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

def seiten(trocken, csrf, nur, repo, summary):
    tsv = io.open(os.path.join(HIER, "seiten.tsv"), encoding="utf-8").read().strip().split("\n")
    ok = 0; fehl = 0
    for i, zeile in enumerate(tsv, 1):
        if nur and i not in nur:
            continue
        title, quelle = zeile.split("\t")
        text = io.open(os.path.join(repo, quelle.replace("/", os.sep)), encoding="utf-8-sig").read()
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
    trocken = "--trocken" in a or not ("--seiten" in a or "--logbuch" in a)
    repo = opt("--repo", REPO)
    nur = set(int(x) for x in opt("--nur").split(",")) if "--nur" in a else None
    summary = opt("--zusammenfassung", ZUSAMMENFASSUNG)
    lbd = opt("--logbuchdatei", os.path.join(HIER, "logbuch.wiki"))
    marke = opt("--marke", "== Version 1.2.0.0"); version = opt("--version")
    csrf = None if trocken else anmelden()
    if trocken:
        print("TROCKENLAUF - es wird nichts hochgeladen.")
    if "--seiten" in a or trocken:
        seiten(trocken, csrf, nur, repo, summary)
    if "--logbuch" in a or (trocken and os.path.exists(lbd)):
        logbuch(trocken, csrf, lbd, marke, version)
