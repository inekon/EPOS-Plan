#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Waechter: findet kaputte Pascal-Script-Kommentare im [Code]-Abschnitt von
Setup/EPOS-Plan.iss.

Hintergrund (Auftrag #181): Ein '{ ... }'-Kommentar in Inno Setups Pascal
Script endet am ERSTEN '}' - er kennt kein Schachteln. Enthaelt der
Kommentartext eine Inno-Setup-Konstante wie '{app}', endet der Kommentar
schon an deren schliessender Klammer, und der Rest der Zeile wird als Code
gelesen. Genau das brach in EPOS-Plan.iss Zeile 436 Spalte 37
(ISCC 6.7.1: "Compiling [Code] section ... Syntax error.").

Dieses Skript sucht im [Code]-Abschnitt jeden '{ ... }'-Kommentar und meldet
ihn, wenn
  (a) sein Text vor der ersten schliessenden Klammer '}' ein weiteres '{'
      enthaelt (das ist die Situation von '{app}' im Kommentartext), oder
  (b) auf der Zeile der schliessenden Klammer '}' danach noch Prosa steht
      (die Zeile beginnt dort mit einem Backslash, enthaelt einen Umlaut
      oder endet mit einem Satzpunkt) - ein Zeichen dafuer, dass der
      Kommentar zu frueh endete und der Rest eigentlich noch Kommentartext
      sein sollte.

Pascal-Zeichenketten ('...') und '//'-Zeilenkommentare sind ausgenommen:
Klammern darin zaehlen nicht mit (siehe z. B. die GUIDs in Zeile 420/464/470,
die als String stehen und deshalb in Ordnung sind).

Aufruf ohne Argument prueft Setup/EPOS-Plan.iss neben diesem Skript und gibt
1 zurueck, wenn ein Fund vorliegt, sonst 0. '--selbsttest' prueft die
Erkennung selbst an zwei eingebetteten Beispielen (eines mit '{app}' im
Kommentar, das gefunden werden muss, eines ohne, das sauber bleiben muss)
und gibt 0 zurueck, wenn beide Erwartungen zutreffen, sonst 1.

Reines Python 3, keine Fremdpakete.
"""

from __future__ import annotations

import os
import re
import sys
from dataclasses import dataclass, field

UMLAUTE = "äöüÄÖÜß"


@dataclass
class Fund:
    start_zeile: int
    start_spalte: int
    schluss_zeile: int
    schluss_spalte: int
    geschachtelt: bool = False
    geschachtelt_pos: tuple[int, int] | None = None
    prosa: bool = False
    prosa_rest: str = ""

    def melden(self) -> str:
        gruende = []
        if self.geschachtelt:
            zz, zs = self.geschachtelt_pos  # type: ignore[misc]
            gruende.append(
                f"enthaelt vor der ersten schliessenden Klammer ein weiteres "
                f"'{{' (Zeile {zz}, Spalte {zs})"
            )
        if self.prosa:
            gruende.append(
                f"die Zeile der schliessenden Klammer setzt danach mit Prosa "
                f"fort: {self.prosa_rest!r}"
            )
        grund = " und ".join(gruende) if gruende else "auffaelliger Kommentar"
        return (
            f"Zeile {self.schluss_zeile}, Spalte {self.schluss_spalte}: "
            f"Kommentar beginnend Zeile {self.start_zeile}, Spalte "
            f"{self.start_spalte} {grund}."
        )


def extrahiere_code_abschnitt(zeilen: list[str]) -> tuple[int, int]:
    """Liefert (start, ende) als 0-basierte Halboffen-Indizes des
    [Code]-Abschnitts innerhalb 'zeilen'. 'ende' ist exklusiv.
    Wirft ValueError, wenn kein [Code]-Abschnitt gefunden wird."""
    start = None
    ende = len(zeilen)
    kopf_muster = re.compile(r"^\[[^\]]+\]\s*$")
    code_muster = re.compile(r"^\[code\]\s*$", re.IGNORECASE)
    for i, zeile in enumerate(zeilen):
        text = zeile.strip()
        if start is None:
            if code_muster.match(text):
                start = i
            continue
        if kopf_muster.match(text):
            ende = i
            break
    if start is None:
        raise ValueError("Kein [Code]-Abschnitt gefunden.")
    return start, ende


def pruefe_zeilen(zeilen: list[str], erste_zeilennummer: int = 1) -> list[Fund]:
    """Prueft eine Liste von Zeilen (ohne Zeilenumbruch) auf kaputte
    '{ ... }'-Kommentare. 'erste_zeilennummer' ist die 1-basierte Nummer der
    ersten Zeile in 'zeilen', fuer die Meldung."""
    funde: list[Fund] = []

    zustand = "code"  # "code", "string", "kommentar"
    kommentar_start = (0, 0)
    kommentar_geschachtelt = False
    kommentar_geschachtelt_pos = None

    for offset, roh_zeile in enumerate(zeilen):
        zeilen_nr = erste_zeilennummer + offset
        zeile = roh_zeile.rstrip("\n").rstrip("\r")
        i = 0
        n = len(zeile)
        while i < n:
            zeichen = zeile[i]
            spalte = i + 1  # 1-basiert

            if zustand == "code":
                if zeichen == "'":
                    zustand = "string"
                    i += 1
                    continue
                if zeichen == "/" and i + 1 < n and zeile[i + 1] == "/":
                    break  # Rest der Zeile ist ein //-Kommentar
                if zeichen == "{":
                    zustand = "kommentar"
                    kommentar_start = (zeilen_nr, spalte)
                    kommentar_geschachtelt = False
                    kommentar_geschachtelt_pos = None
                    i += 1
                    continue
                i += 1
                continue

            if zustand == "string":
                if zeichen == "'":
                    # '' innerhalb einer Zeichenkette ist ein escapetes
                    # Hochkomma - die Zeichenkette laeuft weiter.
                    if i + 1 < n and zeile[i + 1] == "'":
                        i += 2
                        continue
                    zustand = "code"
                    i += 1
                    continue
                i += 1
                continue

            # zustand == "kommentar"
            if zeichen == "{":
                if not kommentar_geschachtelt:
                    kommentar_geschachtelt = True
                    kommentar_geschachtelt_pos = (zeilen_nr, spalte)
                i += 1
                continue
            if zeichen == "}":
                schluss = (zeilen_nr, spalte)
                rest = zeile[i + 1:]
                rest_getrimmt = rest.strip()
                prosa = False
                if rest_getrimmt and not rest_getrimmt.startswith("//"):
                    if rest_getrimmt.startswith("\\"):
                        prosa = True
                    elif any(u in rest_getrimmt for u in UMLAUTE):
                        prosa = True
                    elif rest_getrimmt.endswith("."):
                        prosa = True
                if kommentar_geschachtelt or prosa:
                    fund = Fund(
                        start_zeile=kommentar_start[0],
                        start_spalte=kommentar_start[1],
                        schluss_zeile=schluss[0],
                        schluss_spalte=schluss[1],
                        geschachtelt=kommentar_geschachtelt,
                        geschachtelt_pos=kommentar_geschachtelt_pos,
                        prosa=prosa,
                        prosa_rest=rest_getrimmt,
                    )
                    funde.append(fund)
                zustand = "code"
                i += 1
                continue
            i += 1
            continue

        # Zeilenende: Pascal-Zeichenketten in dieser Datei laufen nicht
        # ueber Zeilengrenzen - eine offen gebliebene Zeichenkette wird
        # defensiv zurueckgesetzt, damit ein einzelner Tippfehler nicht die
        # Erkennung fuer den Rest der Datei verdirbt. Ein offener
        # Kommentar dagegen laeuft bewusst ueber die Zeile weiter (mehrzeilige
        # Kommentare sind der Regelfall).
        if zustand == "string":
            zustand = "code"

    return funde


def pruefe_datei(pfad: str) -> list[Fund]:
    with open(pfad, "r", encoding="utf-8-sig") as f:
        zeilen = f.readlines()
    start, ende = extrahiere_code_abschnitt(zeilen)
    return pruefe_zeilen(zeilen[start:ende], erste_zeilennummer=start + 1)


# ---------------------------------------------------------------------------
# Selbsttest
# ---------------------------------------------------------------------------

BEISPIEL_KAPUTT = """[Code]
{ ---- Beispiel ----
  Text mit einem Pfad aus {app}\\Vorlage\\Datei.
  Ende des Kommentars. }
"""

BEISPIEL_SAUBER = """[Code]
{ ---- Beispiel ----
  Text mit einem Pfad aus der Konstante app, ohne Klammern geschrieben.
  Ende des Kommentars. }
"""


def selbsttest() -> bool:
    ok = True

    zeilen_kaputt = BEISPIEL_KAPUTT.splitlines(keepends=True)
    start, ende = extrahiere_code_abschnitt(zeilen_kaputt)
    funde_kaputt = pruefe_zeilen(zeilen_kaputt[start:ende], erste_zeilennummer=start + 1)
    if funde_kaputt:
        print("Selbsttest 1/2 OK: kaputtes Beispiel wurde gefunden:")
        for f in funde_kaputt:
            print("  " + f.melden())
    else:
        print("Selbsttest 1/2 FEHLGESCHLAGEN: kaputtes Beispiel wurde NICHT gefunden.")
        ok = False

    zeilen_sauber = BEISPIEL_SAUBER.splitlines(keepends=True)
    start, ende = extrahiere_code_abschnitt(zeilen_sauber)
    funde_sauber = pruefe_zeilen(zeilen_sauber[start:ende], erste_zeilennummer=start + 1)
    if not funde_sauber:
        print("Selbsttest 2/2 OK: sauberes Beispiel bleibt ohne Fund.")
    else:
        print("Selbsttest 2/2 FEHLGESCHLAGEN: sauberes Beispiel wurde faelschlich gemeldet:")
        for f in funde_sauber:
            print("  " + f.melden())
        ok = False

    return ok


def main(argv: list[str]) -> int:
    if "--selbsttest" in argv:
        bestanden = selbsttest()
        print("Selbsttest bestanden." if bestanden else "Selbsttest NICHT bestanden.")
        return 0 if bestanden else 1

    if len(argv) > 1:
        pfad = argv[1]
    else:
        pfad = os.path.join(os.path.dirname(os.path.abspath(__file__)), "EPOS-Plan.iss")

    try:
        funde = pruefe_datei(pfad)
    except (OSError, ValueError) as exc:
        print(f"Fehler beim Pruefen von {pfad}: {exc}")
        return 1

    if not funde:
        print(f"{pfad}: keine kaputten Kommentare im [Code]-Abschnitt gefunden.")
        return 0

    print(f"{pfad}: {len(funde)} kaputte(r) Kommentar(e) im [Code]-Abschnitt gefunden:")
    for fund in funde:
        print("  " + fund.melden())
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
