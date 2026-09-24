#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
REFERENZFOLGE des portablen Zufalls des Zapfprofilgenerators (Stufe Z3, Umsetzungskonzept 4.4,
Frage ZU8: ganzzahliger Zufall, Normalverteilung ohne transzendente Funktion).

ZWECK. Eine unabhaengige Nachrechnung der Ziehfolgen - OHNE den C#-Code, nur nach der
Beschreibung der Verfahren:
  - SplitMix64 zum Saeen (Schritt 0x9E3779B97F4A7C15, Mischfaktoren 0xBF58476D1CE4E5B9 und
    0x94D049BB133111EB, Verschiebungen 30/27/31),
  - xoshiro256** zum Ziehen (Ausgabe rotl(s1 * 5, 7) * 9; Zustand wie bei den Autoren),
  - gleichverteilt in [0; 1) als obere 53 Bit mal 2^-53,
  - ganzzahlig in [0; n) nach Lemire (oberes Wort des 128-Bit-Produkts, Verwerfen unter
    (2^64 - n) mod n),
  - normal als Summe von zwoelf Gleichverteilten minus 6,
  - exponentiell nach J. von Neumann (1951): fallender Lauf ab u0, ungerade Laenge -> k + u0,
    sonst k + 1 und von vorn,
  - Poisson als Zahl der Ankuenfte vor lambda aus exponentiellen Abstaenden,
  - Realisierungsseed SplitMix64(SplitMix64(Seed) xor r), Kindseed SplitMix64(Basis xor Index).
Die Konstanten sind die der Algorithmen, keine Normzahlen.

VEROEFFENTLICHTER PRUEFVEKTOR. Die Umschrift von xoshiro256** wird nicht nur gegen sich selbst
geprueft: Aus dem Zustand {1, 2, 3, 4} liefert das Verfahren die veroeffentlichten Ausgaben
11520, 0, 1509978240, 1215971899390074240 (Pruefvektor der Referenzumsetzungen des Verfahrens,
etwa der Rust-Bibliothek rand_xoshiro). Das Skript rechnet sie nach und bricht ab, wenn die
Umschrift sie verfehlt; die CSV fuehrt sie als xoshiro_zustand_1234_*.

Der Test EPOS.Kern.Tests/ZapfZufallTests liest die CSV und verlangt BITGLEICHHEIT: jede
64-Bit-Zahl, jede ganze Zahl und jede Gleitkommazahl (Python-repr, kuerzeste Darstellung, die
genau diesen Wert zurueckgibt) muss die C#-Ziehung Bit fuer Bit treffen.

WIEDERHOLBAR. Das Skript liest nichts und schreibt die CSV neben sich; ein zweiter Lauf erzeugt
dieselben Bytes.

Aufruf (Windows: py, sonst python3):
    py EPOS.Kern.Tests/Proben/Zapfprofil/zufall_referenz_bauen.py
"""

import os

MASKE = (1 << 64) - 1
SCHRITT = 0x9E3779B97F4A7C15
MISCH1 = 0xBF58476D1CE4E5B9
MISCH2 = 0x94D049BB133111EB
ZWEI_HOCH_MINUS_53 = 1.0 / 9007199254740992.0

ORDNER = os.path.dirname(os.path.abspath(__file__))
ERGEBNIS = os.path.join(ORDNER, "zufall_referenz.csv")


def splitmix64(zustand):
    """Ein Schritt: (neuer Zustand, Ausgabe)."""
    zustand = (zustand + SCHRITT) & MASKE
    z = zustand
    z = ((z ^ (z >> 30)) * MISCH1) & MASKE
    z = ((z ^ (z >> 27)) * MISCH2) & MASKE
    return zustand, z ^ (z >> 31)


def mischen(x):
    return splitmix64(x & MASKE)[1]


def realisierungsseed(seed, r):
    return mischen(mischen(seed & MASKE) ^ r)


def kindseed(basis, index):
    return mischen(basis ^ index)


def links(x, k):
    return ((x << k) | (x >> (64 - k))) & MASKE


# Veroeffentlichter Pruefvektor von xoshiro256**: Zustand {1, 2, 3, 4}, die ersten vier Ausgaben.
PRUEFZUSTAND = (1, 2, 3, 4)
PRUEFVEKTOR = (11520, 0, 1509978240, 1215971899390074240)


class Zufall:
    @classmethod
    def aus_zustand(cls, s0, s1, s2, s3):
        """Generator mit dem Zustand von xoshiro256** unmittelbar (ohne SplitMix64)."""
        if s0 | s1 | s2 | s3 == 0:
            raise ValueError("Zustand aus vier Nullen")
        g = cls.__new__(cls)
        g.s = [s0 & MASKE, s1 & MASKE, s2 & MASKE, s3 & MASKE]
        return g

    def __init__(self, seed):
        zustand = seed & MASKE
        s = []
        for _ in range(4):
            zustand, w = splitmix64(zustand)
            s.append(w)
        if s[0] | s[1] | s[2] | s[3] == 0:
            s[0] = SCHRITT
        self.s = s

    def naechste(self):
        s0, s1, s2, s3 = self.s
        ergebnis = (links((s1 * 5) & MASKE, 7) * 9) & MASKE
        t = (s1 << 17) & MASKE
        s2 ^= s0
        s3 ^= s1
        s1 ^= s2
        s0 ^= s3
        s2 ^= t
        s3 = links(s3, 45)
        self.s = [s0, s1, s2, s3]
        return ergebnis

    def gleich(self):
        return (self.naechste() >> 11) * ZWEI_HOCH_MINUS_53

    def ganzzahl(self, n):
        m = n
        p = self.naechste() * m
        oben, unten = p >> 64, p & MASKE
        if unten < m:
            schwelle = ((1 << 64) - m) % m
            while unten < schwelle:
                p = self.naechste() * m
                oben, unten = p >> 64, p & MASKE
        return oben

    def normal(self):
        s = 0.0
        for _ in range(12):
            s += self.gleich()
        return s - 6.0

    def exponential(self):
        k = 0.0
        while True:
            u0 = self.gleich()
            u = u0
            laenge = 1
            while True:
                v = self.gleich()
                if v < u:
                    u = v
                    laenge += 1
                else:
                    break
            if laenge & 1 == 1:
                return k + u0
            k += 1.0

    def poisson(self, lam):
        if not lam > 0.0:
            return 0
        n = 0
        t = self.exponential()
        while t < lam:
            n += 1
            t += self.exponential()
        return n


def zeilen():
    z = []
    # SplitMix64 ab Zustand 0 und Mischen.
    zustand = 0
    for i in range(4):
        zustand, w = splitmix64(zustand)
        z.append((f"splitmix_0_{i}", f"0x{w:016X}"))
    for seed in (0, 1, 42, -1, 9223372036854775807):
        for r in range(3):
            z.append((f"realisierungsseed_{seed}_{r}", f"0x{realisierungsseed(seed, r):016X}"))
    basis = realisierungsseed(1, 0)
    for i in range(3):
        z.append((f"kindseed_{i}", f"0x{kindseed(basis, i):016X}"))
    # xoshiro256** - veroeffentlichter Pruefvektor aus dem Zustand {1, 2, 3, 4}.
    g = Zufall.aus_zustand(*PRUEFZUSTAND)
    ausgaben = [g.naechste() for _ in range(len(PRUEFVEKTOR))]
    if tuple(ausgaben) != PRUEFVEKTOR:
        raise SystemExit(f"Die Umschrift verfehlt den veroeffentlichten Pruefvektor: {ausgaben}")
    for i, w in enumerate(ausgaben):
        z.append((f"xoshiro_zustand_1234_{i}", f"0x{w:016X}"))
    # xoshiro256** - Rohfolge je Seed.
    for seed in (0, 1, 42, 0xFFFFFFFFFFFFFFFF):
        g = Zufall(seed)
        for i in range(20):
            z.append((f"naechste_{seed:X}_{i:02d}", f"0x{g.naechste():016X}"))
    # Ziehungen, je Art ein frischer Generator.
    g = Zufall(realisierungsseed(1, 0))
    for i in range(20):
        z.append((f"gleich_{i:02d}", repr(g.gleich())))
    for n in (1, 7, 60, 1440, 1000000007):
        g = Zufall(n)
        for i in range(20):
            z.append((f"ganzzahl_{n}_{i:02d}", str(g.ganzzahl(n))))
    g = Zufall(7)
    for i in range(20):
        z.append((f"normal_{i:02d}", repr(g.normal())))
    g = Zufall(8)
    for i in range(20):
        z.append((f"exponential_{i:02d}", repr(g.exponential())))
    for lam, name in ((0.5, "0_5"), (3.7, "3_7"), (20.0, "20")):
        g = Zufall(9)
        for i in range(20):
            z.append((f"poisson_{name}_{i:02d}", str(g.poisson(lam))))
    # Gemischte Folge wie im Generator: Stunde (gleich), Minute (ganzzahl 60), Normal, Poisson.
    g = Zufall(kindseed(kindseed(realisierungsseed(1, 2), 0), 3))
    for i in range(10):
        z.append((f"gemischt_{i:02d}_poisson", str(g.poisson(2.5))))
        z.append((f"gemischt_{i:02d}_gleich", repr(g.gleich())))
        z.append((f"gemischt_{i:02d}_minute", str(g.ganzzahl(60))))
        z.append((f"gemischt_{i:02d}_normal", repr(g.normal())))
    return z


def main():
    z = zeilen()
    with open(ERGEBNIS, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("# Referenzfolge des portablen Zufalls (Stufe Z3) - erzeugt von zufall_referenz_bauen.py\n")
        f.write("groesse,wert\n")
        for name, wert in z:
            f.write(f"{name},{wert}\n")
    print(f"{len(z)} Werte nach {ERGEBNIS} geschrieben.")


if __name__ == "__main__":
    main()
