using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die zwölf Betriebskostenpositionen nach VDI 2067 (Etappe E3, Konzept
    /// <c>Konzept_BHKW_Kosten_Erloese.md</c> Abschnitt 4.1) — Positionskatalog,
    /// Bezugsgrößen und der EINE Rechenweg je Bemessungsart.
    ///
    /// <para>
    /// <b>Warum ein eigener Controller.</b> Drei Aufrufer brauchen dieselben Regeln: die
    /// Kostenseite (<see cref="UcBkKosten"/>), der Kostendialog
    /// <c>Form_KostenKomponente</c> (Sperren der abgeleiteten Beträge) und die
    /// Wirtschaftlichkeit
    /// (<c>WirtschaftlichkeitCtrl.LiesBetriebskosten</c>). Eine zweite Kopie der Formel
    /// wäre genau die Sorte Doppelpflege, an der die Kostenseite schon einmal
    /// auseinandergelaufen ist (Befund D1).
    /// </para>
    ///
    /// <para>
    /// <b>Die Bezugsgröße ist PERSISTENT, nicht abgeleitet.</b> Beim Speichern schreibt
    /// der Dialog die ermittelte Menge nach <c>Tab_ProjektWerte.Menge</c> und den Satz
    /// nach <c>Einheitpreis</c>; der Betrag entsteht aus genau diesen beiden Zahlen
    /// (<see cref="Betrag"/>). Damit ist die Herleitung „0,041 €/kWh × 72.000 kWh"
    /// nachvollziehbar gespeichert (Leitentscheidung L5) — und die Wirtschaftlichkeit
    /// rechnet ohne einen einzigen zusätzlichen Datenbankzugriff denselben Wert wie die
    /// Kostenmaske.
    /// </para>
    ///
    /// <para>
    /// <b>Zugehörigkeit und Bemessungsgrundlage sind zwei verschiedene Dinge.</b> Alle
    /// zwölf Positionen gehören zur Betriebskostenrechnung der KWK-Anlage und werden
    /// deshalb unter der Komponente „BHKW" geführt — auch „Instandhaltung Heizkessel",
    /// die sich an der KESSELinvestition bemisst. So bleibt jeder Betrag in der
    /// Kostenverwaltung und in den Komponentensummen sichtbar; welche Größe ihn trägt,
    /// steht in <c>Menge</c> und im Dialog.
    /// </para>
    /// </summary>
    internal static class BetriebskostenCtrl
    {
        // ------------------------------------------------------------- Bezugsgrößen

        /// <summary>Keine Bezugsgröße — die Position ist ein fester Jahresbetrag.</summary>
        internal const string BEZUG_KEINE = "KEINE";

        /// <summary>Investitionssumme der Komponente BHKW (Kategorie 1).</summary>
        internal const string BEZUG_INVEST_BHKW = "INVEST_BHKW";

        /// <summary>Investitionssumme der Komponente Heizkessel (Kategorie 1).</summary>
        internal const string BEZUG_INVEST_KESSEL = "INVEST_KESSEL";

        /// <summary>Investitionssumme des GESAMTEN Projekts (Kategorie 1).</summary>
        internal const string BEZUG_INVEST_GESAMT = "INVEST_GESAMT";

        /// <summary>Elektrische Jahreserzeugung aller BHKW-Module [kWh/a].</summary>
        internal const string BEZUG_STROM_BHKW = "STROM_BHKW";

        /// <summary>
        /// Summe der thermischen Vollbenutzungsstunden über alle BHKW-Module [h/a].
        /// <b>Näherung</b> — siehe <see cref="DbWerte.BEMESSUNG_EUR_PRO_H"/>.
        /// </summary>
        internal const string BEZUG_VBH_BHKW = "VBH_BHKW";

        /// <summary>Summe der Brennstoffkosten des Projekts [€/a].</summary>
        internal const string BEZUG_BRENNSTOFFKOSTEN = "BRENNSTOFFKOSTEN";

        // ------------------------------------------------------------- Positionskatalog

        /// <summary>Eine der zwölf Positionen: was sie heißt, wie sie bemessen wird, woran.</summary>
        internal sealed class Position
        {
            /// <summary>Persistenzwert aus <see cref="DbWerte"/> — zugleich der Schlüssel.</summary>
            public string Bezeichnung;

            /// <summary>Kostenart nach VDI 2067 (<c>DbWerte.KOSTENART_*</c>).</summary>
            public string Kostenart;

            /// <summary>
            /// Die zulässigen Bemessungsarten. Genau EINE gilt (L7); hat die Position
            /// mehr als eine zur Wahl, ist die Auswahl im Dialog sichtbar und die übrigen
            /// Felder sind gesperrt.
            /// </summary>
            public string[] Bemessungen;

            /// <summary>Bezugsgrößen-Schlüssel je Bemessungsart, gleiche Reihenfolge wie <see cref="Bemessungen"/>.</summary>
            public string[] Bezuege;

            /// <summary>Empfehlungsbereich der VDI 2067 in Prozent (0/0 = keiner).</summary>
            public double EmpfehlungVon;
            public double EmpfehlungBis;

            /// <summary>true = der Anwender vergibt einen eigenen Text („Sonstige Kosten").</summary>
            public bool FreieBezeichnung;

            /// <summary>Bezugsgröße zur gewählten Bemessung, <see cref="BEZUG_KEINE"/> wenn unbekannt.</summary>
            public string BezugZu(string bemessung)
            {
                if (Bemessungen == null) return BEZUG_KEINE;
                for (int i = 0; i < Bemessungen.Length; i++)
                    if (string.Equals(Bemessungen[i], bemessung, StringComparison.Ordinal))
                        return (Bezuege != null && i < Bezuege.Length) ? Bezuege[i] : BEZUG_KEINE;
                return BEZUG_KEINE;
            }

            /// <summary>Vorgewählte Bemessung — die erste der Liste.</summary>
            public string Vorgabe
            {
                get
                {
                    return (Bemessungen != null && Bemessungen.Length > 0)
                        ? Bemessungen[0] : DbWerte.BEMESSUNG_BETRAG;
                }
            }
        }

        /// <summary>
        /// Die zwölf Positionen in der Reihenfolge des Dialogs. Empfehlungsbereiche aus
        /// den Beschriftungen der Altmaske <c>Dial_BetriebKost</c>
        /// (<c>Analyse_Altanwendung_BHKW-Plan.md</c>, Abschnitt 2.6).
        ///
        /// <para>
        /// <b>„Instandhaltung BHKW" steht NEBEN der Wartung, nicht statt ihrer.</b> Die
        /// Altanwendung beschriftete das Feld mit „oder", addierte den Betrag aber
        /// (Befund 7). Hier sind es zwei Zeilen mit zwei eigenen Beträgen, und der Dialog
        /// sagt das in seinem Hinweistext ausdrücklich.
        /// </para>
        ///
        /// <para>
        /// <b>Wärmezentrale, bauliche Anlagen und Stromeinspeisung bemessen sich an der
        /// GESAMTinvestition.</b> Die Altanwendung kannte dafür eigene Investitionsgruppen
        /// (Heizraum, Schornstein, Abgasanlage, Öllagerung, Gasanschluss). EPOS-Plan führt
        /// keine solchen Gruppen: Es kennt sieben Komponenten und den FREITEXT
        /// <c>Tab_ProjektWerte.Gruppe</c>, dessen Bestand („test", „Arbeitspreis",
        /// „Infrastruktur", „Allgemein" …) je Projekt anders aussieht und deshalb keine
        /// verlässliche Bezugsgröße hergibt. Neue Investitionsgruppen zu erfinden wäre ein
        /// Datenmodelleingriff ohne Auftrag. Die drei Positionen bemessen sich deshalb an
        /// der Investitionssumme des Projekts — sichtbar benannt, damit niemand eine
        /// engere Bezugsgröße unterstellt.
        /// </para>
        /// </summary>
        internal static readonly Position[] Katalog =
        {
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_WARTUNG_BHKW,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                // L7: genau EINE Bemessung gilt, sichtbar ausgewählt.
                Bemessungen = new[] { DbWerte.BEMESSUNG_EUR_PRO_KWH,
                                      DbWerte.BEMESSUNG_EUR_PRO_H,
                                      DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_STROM_BHKW, BEZUG_VBH_BHKW, BEZUG_INVEST_BHKW }
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_INSTANDHALTUNG_BHKW,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_BHKW },
                EmpfehlungVon = 3.0, EmpfehlungBis = 9.0
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_INSTANDHALTUNG_KESSEL,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_KESSEL },
                EmpfehlungVon = 1.5, EmpfehlungBis = 2.5
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_GESAMT },
                EmpfehlungVon = 1.8, EmpfehlungBis = 2.2
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_INSTANDHALTUNG_BAULICH,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_GESAMT },
                EmpfehlungVon = 1.0, EmpfehlungBis = 1.5
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_INSTANDHALTUNG_STROMEINSPEISUNG,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_GESAMT },
                EmpfehlungVon = 1.8, EmpfehlungBis = 2.2
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_PERSONAL,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_GESAMT },
                EmpfehlungVon = 1.0, EmpfehlungBis = 4.0
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_VERWALTUNG,
                Kostenart = DbWerte.KOSTENART_SONSTIGE,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION },
                Bezuege     = new[] { BEZUG_INVEST_GESAMT },
                EmpfehlungVon = 0.8, EmpfehlungBis = 2.0
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_HILFSENERGIE,
                Kostenart = DbWerte.KOSTENART_BEDARFSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN },
                Bezuege     = new[] { BEZUG_BRENNSTOFFKOSTEN }
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_RESERVELEISTUNG,
                Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessungen = new[] { DbWerte.BEMESSUNG_BETRAG },
                Bezuege     = new[] { BEZUG_KEINE }
            },
            new Position
            {
                Bezeichnung = DbWerte.VDI_POS_SONSTIGE,
                Kostenart = DbWerte.KOSTENART_SONSTIGE,
                Bemessungen = new[] { DbWerte.BEMESSUNG_BETRAG },
                Bezuege     = new[] { BEZUG_KEINE },
                FreieBezeichnung = true
            }
        };

        /// <summary>Position mit dieser Bezeichnung, oder null.</summary>
        internal static Position Finde(string bezeichnung)
        {
            foreach (Position p in Katalog)
                if (string.Equals(p.Bezeichnung, bezeichnung, StringComparison.Ordinal)) return p;
            return null;
        }

        // ------------------------------------------------------------- Der Rechenweg

        /// <summary>
        /// Der EINE Rechenweg je Bemessungsart. Reine Funktion — kein Datenbankzugriff,
        /// keine Kultur, kein Zustand (Leitentscheidung L9).
        /// </summary>
        /// <param name="bemessung">Wert aus <c>DbWerte.BEMESSUNG_*</c>; leer gilt als <c>BETRAG</c>.</param>
        /// <param name="eingegeben">Der erfasste Betrag [€/a] — gilt bei <c>BETRAG</c>.</param>
        /// <param name="menge">Bezugsmenge (€, h/a, kWh/a).</param>
        /// <param name="satz">Satz (%, €/h, €/kWh).</param>
        /// <param name="istErloes">true = Erlös; das Ergebnis ist dann ≤ 0.</param>
        /// <remarks>
        /// <b>Vorzeichen.</b> Der Rückgabewert ist immer die Zahlungswirkung in €/a:
        /// positiv = Ausgabe, negativ = Einnahme. Bei einer Erlösposition wird der Betrag
        /// deshalb auf sein negatives Vorzeichen gezwungen — ein Erlös kann so nirgends
        /// als Kosten in eine Summe geraten, gleichgültig mit welchem Vorzeichen Menge
        /// und Satz erfasst wurden.
        /// </remarks>
        internal static double Betrag(string bemessung, double eingegeben,
                                      double? menge, double? satz, bool istErloes)
        {
            double wert;

            if (string.IsNullOrEmpty(bemessung) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_JAHRESBETRAG, StringComparison.Ordinal))
            {
                // JAHRESBETRAG ist die zweite ABSOLUTE Art (Etappe KD1/KD3, Konzept
                // Kostendialoge § 5.3): fester Jahresbetrag ohne Bezugsgröße — er trägt
                // seinen Wert wie BETRAG direkt in EingegebenerWert. Ohne diesen Zweig
                // fiele er unten in die "nicht gepflegt = 0"-Klammer.
                wert = eingegeben;
            }
            else if (!menge.HasValue || !satz.HasValue)
            {
                // ANWENDERENTSCHEID I-2 (30.08.2026, Paket FX2): „Wenn eine Bemessungsart
                // 0 ergibt, nimm den erfassten Wert > 0."
                //
                // Fehlt eine der beiden Zahlen, ist die Ableitung NICHT RECHENBAR. Bis
                // hierher galt dann 0 — eine abgeleitete Zeile mit erfasstem Betrag fiel
                // also still aus der Rechnung (Befund I-2 der Rechenwege-Formelkarte).
                // Sie verhält sich jetzt wie BETRAG: der erfasste Wert gilt. Das ist
                // dieselbe Klammer, die der „unbekannte Wert"-Zweig unten schon immer
                // hatte, und dieselbe Vorsicht — ein Betrag verschwindet nicht wortlos.
                //
                // NUR dieser Zweig ändert sich. Eine ECHTE Ableitung mit Menge 0 (die
                // Baugröße ist wirklich 0) läuft weiter unten durch und ergibt 0 — der
                // Unterschied ist „nicht ermittelbar" gegen „ermittelt und null", und
                // genau den unterscheidet das Datenmodell mit NULL.
                //
                // FOLGE für die Endenergie-Arten (Konzept BHKW-Wirtschaftlichkeit § 4.5,
                // „ohne Lauf keine Menge, kein Betrag"): Liefert der Auflöser null, greift
                // ab jetzt der erfasste Wert statt der 0. Das ist vom Anwenderentscheid
                // ausdrücklich gedeckt und mit ihm gemessen.
                wert = eingegeben;
            }
            else
            {
                double m = menge.Value, s = satz.Value;

                // ETAPPE H1 — die beiden Endenergie-Bemessungen rechnen wie jede andere
                // Prozentangabe. Die MENGE ist dabei ein ERGEBNISWERT aus dem
                // Simulationslauf, kein Eingabewert (Festlegung 29.08.2026); im Dialog
                // wird nur der Satz gepflegt.
                //
                // WEG B liefert eine Strommenge, keine Kosten: Der Bezugsgroessen-Aufloeser
                // uebergibt fuer PROZENT_ENDENERGIEBEDARF deshalb den BEWERTETEN Bedarf
                // (kWh x Strombezugspreis). Das ist rechnerisch dasselbe wie
                // "Menge x Satz/100 x Preis" - die Multiplikation ist kommutativ - und
                // kommt ohne zweite Formel aus. Die unbewertete Menge bleibt fuer die
                // Herleitungszeile erhalten.
                if (string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, StringComparison.Ordinal))
                    wert = m * s / 100.0;
                else if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWP, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, StringComparison.Ordinal))
                    wert = m * s;
                else
                    wert = eingegeben;      // unbekannter Wert: wie BETRAG, nie stillschweigend 0
            }

            if (istErloes && wert > 0) wert = -wert;
            return wert;
        }

        /// <summary>Kurzform für eine gelesene Position.</summary>
        internal static double Betrag(double eingegeben, KostenPositionCtrl.Zusatz z)
        {
            if (z == null) return eingegeben;
            return Betrag(z.Bemessung, eingegeben, z.Menge, z.Einheitpreis, z.IstErloes);
        }

        /// <summary>
        /// Einheitenzeichen des SATZES einer Bemessungsart („%", „€/h", „€/kWh", „€").
        /// <b>Nicht lokalisiert</b> — reine Einheitenzeichen ohne Wortbestand, in beiden
        /// Sprachen gleich; dieselbe Ausnahme wie bei den typografischen Marken
        /// (Lokalisierungskatalog).
        /// </summary>
        internal static string SatzEinheit(string bemessung)
        {
            // Die KD-Bemessungen (Etappe KD1+) tragen ihre Einheit im BemessungKatalog —
            // EINE Wahrheit für Alt-Dialog und Komponenten-Kostendialog.
            BemessungKatalog.Info kd = BemessungKatalog.Finde(bemessung);
            if (kd != null &&
                !string.Equals(bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                return kd.Einheit;

            if (string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal))
                return "%";
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return "€/h";
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal))
                return "€/kWh";
            return DbWerte.KOSTEN_EINHEIT_EURO;
        }

        /// <summary>
        /// Einheitenzeichen der BEZUGSMENGE einer Bemessungsart („€", „h/a", „kWh/a").
        /// <inheritdoc cref="SatzEinheit" path="/summary/text()[last()]"/>
        /// </summary>
        internal static string MengenEinheit(string bemessung)
        {
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return "h/a";
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal))
                return "kWh/a";
            return DbWerte.KOSTEN_EINHEIT_EURO;
        }

        // ------------------------------------------------------------- Bezugsgrößen lesen

        /// <summary>Die Bezugsgrößen eines Projekts; <c>null</c> = nicht ermittelbar.</summary>
        internal sealed class Bezugsgroessen
        {
            public double? InvestBhkw;
            public double? InvestKessel;
            public double? InvestGesamt;
            public double? StromKwh;          // elektrische Jahreserzeugung aller Module
            public double? VbhSumme;          // Σ VbhThermisch je Modul (Näherung)
            public double? Brennstoffkosten;  // €/a

            /// <summary>Zeitstempel des zugrunde liegenden Simulationslaufs.</summary>
            public DateTime? Laufstand;

            /// <summary>Wert zum Bezugsschlüssel, oder null.</summary>
            public double? Wert(string bezug)
            {
                switch (bezug)
                {
                    case BEZUG_INVEST_BHKW: return InvestBhkw;
                    case BEZUG_INVEST_KESSEL: return InvestKessel;
                    case BEZUG_INVEST_GESAMT: return InvestGesamt;
                    case BEZUG_STROM_BHKW: return StromKwh;
                    case BEZUG_VBH_BHKW: return VbhSumme;
                    case BEZUG_BRENNSTOFFKOSTEN: return Brennstoffkosten;
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Ermittelt alle Bezugsgrößen eines Projekts. Jede einzeln gefangen — eine
        /// fehlende Größe darf die übrigen elf Positionen nicht mitreißen.
        /// </summary>
        internal static Bezugsgroessen LiesBezugsgroessen(int projektID)
        {
            var b = new Bezugsgroessen();

            b.InvestGesamt = InvestSumme(projektID, 0);
            b.InvestBhkw = InvestSumme(projektID, KOMPONENTE_BHKW);
            b.InvestKessel = InvestSumme(projektID, KOMPONENTE_HEIZKESSEL);

            int idErgebnis = LetztesErgebnis(projektID, out b.Laufstand);
            if (idErgebnis > 0)
            {
                b.StromKwh = ModulSumme(idErgebnis, "Stromproduktion", 1000.0);   // MWh/a → kWh/a
                b.VbhSumme = ModulSumme(idErgebnis, SchemaKatalog.SPALTE_MODUL_VBH_THERMISCH, 1.0);
                b.Brennstoffkosten = LiesBrennstoffkosten(projektID);
            }
            return b;
        }

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> der Komponenten, deren Investition als
        /// Bezugsgröße dient. Dieselben Nummern wie in <c>Tab_KostenKomponente</c>;
        /// sie stehen hier als benannte Konstanten, damit die Zuordnung im
        /// Betriebskostenpfad nicht als nackte Zahl auftaucht.
        /// </summary>
        internal const int KOMPONENTE_HEIZKESSEL = 2;
        internal const int KOMPONENTE_BHKW = 7;

        /// <summary>
        /// Summe der Investitionspositionen (Kategorie 1). <paramref name="komponentenID"/>
        /// = 0 heißt „ganzes Projekt".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ETAPPE K5 — ohne Zuschusspositionen.</b> Die prozentualen Bemessungen der
        /// VDI 2067 („% der Investitionssumme") rechnen ausdrücklich <b>vor</b>
        /// Zuschussabzug (Konzept § 7.4, letzter Punkt). Das ist keine Feinheit, sondern
        /// beides zusammen: fachlich richtig — instand zu halten ist die Anlage, nicht der
        /// Eigenanteil — und die Auflösung eines Alt-Widerspruchs, denn die Altanwendung
        /// war an dieser Stelle uneinheitlich (Dialog gegen Blatt, Anhang A).
        /// </para>
        /// <para>
        /// <b>Ohne den Ausschluss wäre es sogar falsch herum.</b> Ein Zuschuss steht als
        /// POSITIVER Betrag in <c>EingegebenerWert</c> (Begründung an
        /// <see cref="DbWerte.KOSTENART_ZUSCHUSS"/>). Eine ungefilterte Summe würde ihn
        /// also nicht abziehen, sondern ADDIEREN — die Instandhaltung bemäße sich an einer
        /// Investitionssumme, die es nie gab.
        /// </para>
        /// <para>
        /// <b>Rückfallebene.</b> Fehlt die Spalte <c>Kostenart</c> (nie migrierte
        /// Datenbank), läuft die Abfrage ohne die Einschränkung — also genau wie vor K5.
        /// In einer solchen Datenbank kann es keine Zuschusszeile geben.
        /// </para>
        /// </remarks>
        private static double? InvestSumme(int projektID, int komponentenID)
        {
            return InvestSumme(projektID, komponentenID, 0);
        }

        /// <summary>ETAPPE H4a: dieselbe Abfrage mit optionalem Anlagenfilter
        /// (Schritt 45) — der Kern beider Überladungen; K5-Zuschussausschluss und
        /// Kostenart-Toleranz unverändert.</summary>
        private static double? InvestSumme(int projektID, int komponentenID, int idAnlage)
        {
            bool mitKostenart = false;
            try { mitKostenart = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }

            try
            {
                string sql = "SELECT SUM(EingegebenerWert) FROM " + SchemaKatalog.TAB_PROJEKTWERTE +
                             " WHERE ProjektID = ? AND KategorieID = ?";
                var ps = new List<DbParam>
                {
                    new DbParam("@p", projektID),
                    new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                };
                if (komponentenID > 0)
                {
                    sql += " AND KomponentenID = ?";
                    ps.Add(new DbParam("@c", komponentenID));
                }
                if (idAnlage > 0 && AnlagenSpalteVorhanden())
                {
                    sql += " AND [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] = ?";
                    ps.Add(new DbParam("@a", idAnlage));
                }
                if (mitKostenart)
                {
                    // NULL und Leerstring bleiben drin: Das sind die Bestandszeilen (bzw.
                    // die, die Schritt 19b nicht erreicht hat), und sie sind Investitionen.
                    sql += " AND (([" + SchemaKatalog.SPALTE_PW_KOSTENART + "] IS NULL) OR ([" +
                           SchemaKatalog.SPALTE_PW_KOSTENART + "] <> ?))";
                    ps.Add(new DbParam("@art", DbWerte.KOSTENART_ZUSCHUSS));
                }

                object o = DataRepository.ExecuteScalar(sql, ps.ToArray());
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToDouble(o);
            }
            catch { return null; }
        }

        /// <summary>ETAPPE H4a: Cache der Spaltenprobe <c>Tab_ProjektWerte.ID_Anlage</c>
        /// (Muster <see cref="WirtschaftlichkeitCtrl.SpalteVorhanden"/>).</summary>
        private static bool? _anlagenSpalte;

        private static bool AnlagenSpalteVorhanden()
        {
            if (_anlagenSpalte.HasValue) return _anlagenSpalte.Value;
            _anlagenSpalte = WirtschaftlichkeitCtrl.SpalteVorhanden(
                SchemaKatalog.TAB_PROJEKTWERTE, SchemaKatalog.SPALTE_PW_ID_ANLAGE);
            return _anlagenSpalte.Value;
        }

        /// <summary>
        /// ETAPPE H4a: Bezugsgröße „% der Investition" (Konzept Kostendialoge § 5.3:
        /// Summe der Investitionskosten der Komponente VOR Zuschussabzug) — stufig:
        /// Trägt die Position eine Anlage und existieren Investitionszeilen an genau
        /// dieser Anlage, zählt deren Summe; sonst die Komponentensumme (die
        /// dokumentierte Regel), notfalls das ganze Projekt. null = nichts erfasst.
        /// </summary>
        internal static double? InvestSummeFuer(int projektID, int komponentenID, int idAnlage)
        {
            if (idAnlage > 0)
            {
                double? anlage = InvestSumme(projektID, komponentenID, idAnlage);
                if (anlage.HasValue && anlage.Value != 0) return anlage;
            }
            if (komponentenID > 0)
            {
                double? komponente = InvestSumme(projektID, komponentenID);
                if (komponente.HasValue && komponente.Value != 0) return komponente;
            }
            return InvestSumme(projektID, 0);
        }

        /// <summary>ID und Zeitstempel des jüngsten Simulationslaufs, 0 = keiner.</summary>
        private static int LetztesErgebnis(int projektID, out DateTime? stand)
        {
            stand = null;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Zeitstempel FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                    new DbParam("@p", projektID));
                if (dt == null || dt.Rows.Count == 0) return 0;

                if (dt.Rows[0]["Zeitstempel"] != DBNull.Value)
                    stand = Convert.ToDateTime(dt.Rows[0]["Zeitstempel"]);
                return dt.Rows[0]["ID"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["ID"]) : 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Summe einer Spalte über alle BHKW-Modulzeilen eines Laufs. null, wenn die
        /// Spalte fehlt (Datenbank vor Migrationsschritt 18) oder kein Modul existiert —
        /// nie 0, denn „nicht erhoben" ist etwas anderes als „erhoben und null".
        /// </summary>
        private static double? ModulSumme(int idErgebnis, string spalte, double faktor)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT SUM(m.[" + spalte + "]) FROM " + ErgebnisCtrl.TAB_BHKW + " AS e " +
                    "INNER JOIN " + ErgebnisCtrl.TAB_BHKW_MODUL + " AS m ON e.ID = m.ID_ErgebnisBHKW " +
                    "WHERE e.ID_Ergebnis = ?",
                    new DbParam("@e", idErgebnis));
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToDouble(o) * faktor;
            }
            catch { return null; }
        }

        /// <summary>
        /// Summe der Brennstoffkosten [€/a] des jüngsten Laufs.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Differenz.</b> <c>KostenEmissionRechner</c> ist die EINE Stelle,
        /// die Brennstoffmengen mit Trägerpreisen und Heizwerten verrechnet; er legt
        /// jedoch nur <c>Energiekosten</c> (Brennstoffe + Netzstrom) und
        /// <c>StromkostenNetz</c> ab. Die Differenz ist damit der Brennstoffanteil —
        /// derselbe Weg, den <c>WirtschaftlichkeitCtrl</c> für den Tarifersatz geht
        /// (dort <c>Energiekosten − StromkostenNetz + Tarif</c>). Eine zweite
        /// Preisverrechnung wäre eine doppelte Wahrheit.
        ///
        /// Fehlt eine der beiden Größen, gibt es keine Bezugsgröße und damit keine
        /// Hilfsenergie-Vorbelegung — statt einer Zahl, die nur nach Genauigkeit aussieht.
        /// </remarks>
        private static double? LiesBrennstoffkosten(int projektID)
        {
            try
            {
                var ergCtrl = new ErgebnisCtrl();
                ErgebnisModel erg = ergCtrl.Load(projektID);
                if (erg == null) return null;

                var v = new VariantenDaten { IdProjekt = projektID, Ergebnis = erg };
                KostenEmissionRechner.Berechne(v);

                if (!v.Energiekosten.HasValue) return null;
                if (!v.StromkostenNetz.HasValue) return v.Energiekosten.Value;   // reines Brennstoffsystem
                return v.Energiekosten.Value - v.StromkostenNetz.Value;
            }
            catch { return null; }
        }

        // ------------------------------------------------------------- Lesen und Schreiben

        /// <summary>Eine Zeile des Betriebskosten-Dialogs.</summary>
        internal sealed class Zeile
        {
            /// <summary>Die Katalogposition (nie null).</summary>
            public Position Pos;

            /// <summary><c>Tab_ProjektWerte.ID</c>, 0 = noch nicht erfasst.</summary>
            public int Id;

            /// <summary>Gewählte Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>).</summary>
            public string Bemessung;

            /// <summary>Satz [%], [€/h] oder [€/kWh]; null = nicht gepflegt.</summary>
            public double? Satz;

            /// <summary>Fester Jahresbetrag [€/a] — nur bei <c>BETRAG</c> maßgeblich.</summary>
            public double Fest;

            /// <summary>Bezugsmenge zur gewählten Bemessung; null = nicht ermittelbar.</summary>
            public double? Menge;

            /// <summary>Freie Bezeichnung („Sonstige Kosten"); leer = Katalogname.</summary>
            public string EigenerName = "";

            /// <summary>Der abgeleitete Jahresbetrag [€/a] netto.</summary>
            public double Netto
            {
                get { return Betrag(Bemessung, Fest, Menge, Satz, false); }
            }
        }

        /// <summary>
        /// Liest die zwölf Positionen eines Projekts. Nicht erfasste Positionen kommen mit
        /// <c>Id = 0</c> und der Vorgabebemessung zurück — der Dialog zeigt also immer
        /// alle zwölf Zeilen, unabhängig davon, was schon gepflegt ist.
        /// </summary>
        internal static List<Zeile> Lies(int projektID, Bezugsgroessen bezug)
        {
            var liste = new List<Zeile>();
            Dictionary<int, KostenPositionCtrl.Zusatz> zusatz =
                KostenPositionCtrl.LiesZusatz(projektID, DbWerte.KOSTEN_KATEGORIE_BETRIEB);

            foreach (Position p in Katalog)
            {
                var z = new Zeile { Pos = p, Bemessung = p.Vorgabe };

                int stammID = KostenPositionCtrl.StammIdNeben(p.Bezeichnung);
                if (stammID > 0)
                    z.Id = KostenPositionCtrl.FindePosition(projektID, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                                                            KOMPONENTE_BHKW, stammID);

                if (z.Id > 0)
                {
                    KostenPositionCtrl.Zusatz zu;
                    if (!zusatz.TryGetValue(z.Id, out zu)) zu = KostenPositionCtrl.LiesZusatzNachId(z.Id);

                    if (zu != null)
                    {
                        // Nur eine im Katalog dieser Position vorgesehene Bemessung
                        // uebernehmen - sonst stuende im Dialog eine Auswahl, die die
                        // Position gar nicht kennt (etwa EUR_PRO_H bei "Personalkosten").
                        if (p.Bemessungen != null &&
                            Array.IndexOf(p.Bemessungen, zu.Bemessung) >= 0)
                            z.Bemessung = zu.Bemessung;

                        z.Satz = zu.Einheitpreis;
                        z.Fest = KostenPositionCtrl.LiesBetrag(z.Id);
                    }
                }

                z.Menge = (bezug != null) ? bezug.Wert(p.BezugZu(z.Bemessung)) : null;
                liste.Add(z);
            }
            return liste;
        }

        /// <summary>
        /// Schreibt die zwölf Positionen. Angelegt wird nur, was gepflegt ist; eine Zeile
        /// ohne Satz und ohne Betrag entsteht nicht — und eine bereits vorhandene wird auf
        /// 0 gesetzt statt gelöscht, damit eine vom Anwender angelegte Gruppierung nicht
        /// verschwindet.
        /// </summary>
        /// <returns>Zahl der geschriebenen Zeilen.</returns>
        internal static int Speichere(int projektID, List<Zeile> zeilen)
        {
            if (zeilen == null) return 0;
            KostenPositionCtrl.StelleSpaltenSicher();

            int n = 0;
            foreach (Zeile z in zeilen)
            {
                if (z == null || z.Pos == null) continue;

                bool gepflegt = z.Satz.HasValue || Math.Abs(z.Fest) > 0.0000001;
                if (!gepflegt && z.Id <= 0) continue;      // nichts zu tun

                int stammID = KostenPositionCtrl.StammIdNeben(z.Pos.Bezeichnung);
                if (stammID <= 0) continue;

                int id = z.Id;
                if (id <= 0)
                    id = KostenPositionCtrl.SetzeBetrag(projektID, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                                                        KOMPONENTE_BHKW, stammID, 0.0,
                                                        DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI, true);
                if (id <= 0) continue;

                var zu = new KostenPositionCtrl.Zusatz
                {
                    Kostenart = z.Pos.Kostenart,
                    Bemessung = z.Bemessung,
                    IstErloes = false,       // die zwoelf VDI-Positionen sind samtlich Kosten
                    Menge = string.Equals(z.Bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal)
                            ? null : z.Menge,
                    Einheitpreis = string.Equals(z.Bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal)
                            ? null : z.Satz
                };

                if (KostenPositionCtrl.SetzeBetragMitZusatz(id, z.Netto, zu)) n++;
                z.Id = id;
            }
            return n;
        }
    }
}
