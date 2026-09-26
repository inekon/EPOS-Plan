using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Baustein 5: Berechnungsergebnisse je Variante (Phase-2-Basis; die vier
    /// Ganglinien-Diagrammtypen folgen in Phase 3 über den ChartRenderer).
    /// </summary>
    public class ErgebnisseBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_ERGEBNISSE; } }
        public string Titel { get { return "Ergebnisse je Variante"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>. Sie weicht vom
        /// Titel des Häkchens ab („Ergebnisse je Variante“).</summary>
        public const string UEBERSCHRIFT = "Berechnungsergebnisse je Variante";

        // Die Kernkennzahlen des Variantenkapitels stehen in Berichtstabellen.Kernkennzahlen (BV-E5).

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1(UEBERSCHRIFT);

            foreach (VariantenDaten v in daten.Varianten)
            {
                k.Ueberschrift2((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                k.Hinweis("Simulationsstand: " + (v.SimulationsStand.HasValue
                    ? v.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm", k.Kultur) : "—") +
                    (v.FrischSimuliert ? " (für diesen Bericht neu gerechnet)" : ""));

                if (v.Fehler != null)
                { k.Text("Für dieses Projekt konnten keine Ergebnisse geladen werden: " + v.Fehler); continue; }

                // BV-E5: dieselbe Tafel wie {{stand.tabelle.kennzahlen}} (Berichtstabellen.Standkennzahlen).
                Berichtstabelle kennzahlen = Berichtstabellen.Standkennzahlen(v, BerichtTexte.Englisch, k.Kultur);
                if (!kennzahlen.IstLeer) k.Fuege(WordTabellenschreiber.Direkt(k, kennzahlen));
                else k.Text("Keine Kennzahlen verfügbar.");

                // Die vier Ganglinientypen aus der In-Memory-Simulation (Konzept Kap. 6.2).
                if (v.Zeitreihen != null)
                    ZeichneGanglinien(k, v.Zeitreihen);
                else
                    k.Hinweis("Ganglinien nicht verfügbar — sie entstehen nur, wenn für den Bericht " +
                              "frisch simuliert wurde (Baustein „Ergebnisse je Variante“ aktiv).");
            }
        }

        /// <summary>
        /// Die vier Ganglinientypen des Berichts.
        ///
        /// <para><b>Alle vier gehen über das ZEICHENMODELL</b> und stehen damit als
        /// SVG mit PNG-Rückfall im Dokument (Entscheid DG-E3-8). Die Strombilanz kam
        /// als letzte dazu, als die Gruppe (c) ihr
        /// <c>StrombilanzMonateModell</c> mitbrachte.</para>
        /// </summary>
        private static void ZeichneGanglinien(WordKontext k, ZeitreihenSatz z)
        {
            // BV-E5: dieselben Modelle wie die Bildplatzhalter stand.bild.* (Berichtsbilder).
            Zeichnung.Zeichenmodell m = Sicher(() => Berichtsbilder.JahresverlaufWaerme(z));
            if (m != null)
            {
                k.Bild(m, 620, 280);
                k.Beschriftung("Wärmeerzeugung im Jahresverlauf (gestapelte Erzeuger, Bedarf als Linie, Tagesmittel)");
            }
            m = Sicher(() => Berichtsbilder.DauerlinieWaerme(z));
            if (m != null)
            {
                k.Bild(m, 620, 280);
                k.Beschriftung("Jahresdauerlinie Wärme (geordnete Bedarfs- und Erzeugerdauerlinien)");
            }
            m = Sicher(() => Berichtsbilder.StrombilanzMonate(z));
            if (m != null)
            {
                k.Bild(m, 620, 280);
                k.Beschriftung("Strombilanz im Monatsverlauf (Deckung gestapelt, Einspeisung separat, Bedarf als Linie)");
            }
            m = Sicher(() => Berichtsbilder.Speicherverlauf(z));
            if (m != null)
            {
                k.Bild(m, 620, 260);
                k.Beschriftung("Speicherverlauf in charakteristischen Wochen (Winter/Übergang/Sommer)");
            }
        }

        private static Zeichnung.Zeichenmodell Sicher(Func<Zeichnung.Zeichenmodell> f)
        {
            try { return f(); } catch { return null; }   // ein Diagrammfehler kippt nicht den Bericht
        }
    }

    /// <summary>
    /// Baustein 6: Variantenvergleich — Kennzahlentabellen je Gruppe mit Blocksplitting,
    /// kompakte Delta-Tabelle, Erzeuger-Einzellisten und Brennstoffmengen (Konzept Kap. 4–5).
    /// Balkendiagramme folgen in Phase 3.
    /// </summary>
    public class VergleichBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_VERGLEICH; } }
        public string Titel { get { return "Variantenvergleich"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Variantenvergleich";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();

            // E5/F7: Die CO₂-Zeilen tragen den Modus, in dem sie gerechnet wurden.
            // Bei uneinheitlichen Varianten sagt die Beschriftung genau das — sonst
            // stünde über einer Spalte eine Methode, in der ihre Zahl nicht entstand.
            List<Kennzahl> katalog =
                KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));

            k.Ueberschrift1(UEBERSCHRIFT);
            if (varianten.Count == 0)
                k.Hinweis("Es wurden keine Varianten ausgewählt — die Tabellen zeigen nur das Stammprojekt.");

            // ---------------- Kennzahlentabellen je Gruppe ----------------
            // KU2 Welle 3: die Gruppe „Kälte“ zwischen Effizienz und Emissionen (KennzahlenKatalog.GRUPPEN).
            // BV-E5: dieselben Tafeln wie {{tabelle.vergleich.<gruppe>}} (Berichtstabellen.Vergleichsgruppe),
            // Blockteilung zu drei Varianten mit wiederholter Stammspalte, Δ-Spalte nur bei genau einer Variante.
            foreach (string gruppe in KennzahlenKatalog.GRUPPEN)
            {
                Berichtstabelle tafel = Berichtstabellen.Vergleichsgruppe(daten, gruppe, BerichtTexte.Englisch, k.Kultur);
                if (tafel.IstLeer) continue;   // Gruppe ohne verfügbare Werte (z. B. Kosten bis Phase 5)

                k.Ueberschrift2(gruppe);
                foreach (IReadOnlyList<int> block in tafel.Bloecke())
                {
                    k.Fuege(WordTabellenschreiber.Direkt(k, tafel, block));
                    k.Abstand();
                }
            }

            // ---------------- kompakte Delta-Tabelle ----------------
            if (varianten.Count >= 2)
            {
                k.Ueberschrift2("Abweichung zum Stamm (Schlüsselkennzahlen, in %)");
                // BV-E5: dieselbe Tafel wie {{tabelle.vergleich.delta_prozent}}.
                Berichtstabelle delta = Berichtstabellen.DeltaProzent(daten, BerichtTexte.Englisch, k.Kultur);
                if (!delta.IstLeer) k.Fuege(WordTabellenschreiber.Direkt(k, delta));
            }

            // ---------------- Balkendiagramme je Schlüsselkennzahl (Konzept Kap. 6.1) ----------------
            if (daten.Varianten.Count >= 2)
            {
                k.Ueberschrift2("Kennzahlen im Vergleich (Diagramme)");
                // BV-E5: dieselben Balken und dasselbe Modell wie bild.vergleich.balken.<k> (Berichtsbilder).
                foreach (string schluessel in Berichtsbilder.Balkenkennzahlen)
                {
                    Kennzahl kz = katalog.FirstOrDefault(x => x.Schluessel == schluessel);
                    if (kz == null) continue;
                    List<ChartRenderer.Balken> balken = Berichtsbilder.Vergleichsbalken(daten.Varianten, schluessel);
                    if (balken.Count < 2) continue;
                    Zeichnung.Zeichenmodell m2 = Sicher(() => Berichtsbilder.Vergleich(
                        daten.Varianten, katalog, schluessel, BerichtTexte.Englisch));
                    if (m2 != null)
                    {
                        int hoehe = (150 + balken.Count * 64) / 2;
                        k.Bild(m2, 620, hoehe);
                        k.Beschriftung(kz.Label(BerichtTexte.Englisch) + " je Variante (Stamm hervorgehoben)");
                    }
                }
            }

            // ---------------- Deckungsdiagramme je Projekt (aus dem Bestand übernommen) ----------------
            k.Ueberschrift2("Deckungsdiagramme");
            k.Hinweis("Anteile an der Wärme- bzw. Stromdeckung je Projekt (aus den Deckungsgraden " +
                      "der Erzeuger; der Rest ist ungedeckte Wärme bzw. Netzbezug).");
            foreach (VariantenDaten v in daten.Varianten)
            {
                ErgebnisModel m = v.Ergebnis;
                if (m == null) continue;
                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                // BV-E5: dieselben Segmente und Modelle wie stand.bild.deckung_waerme/_strom (Berichtsbilder).
                ErgebnisModel ergebnis = m;
                if (Berichtsbilder.Waermedeckung(m).Count > 0)
                    k.Bild(Sicher(() => Berichtsbilder.Deckung(ergebnis, true)),
                           Berichtsbilder.ANZEIGE_BREITE_KUCHEN, Berichtsbilder.ANZEIGE_HOEHE_KUCHEN);
                if (Berichtsbilder.Stromdeckung(m).Count > 0)
                    k.Bild(Sicher(() => Berichtsbilder.Deckung(ergebnis, false)),
                           Berichtsbilder.ANZEIGE_BREITE_KUCHEN, Berichtsbilder.ANZEIGE_HOEHE_KUCHEN);
            }

            // ---------------- Erzeuger-Einzellisten je Projekt ----------------
            k.Ueberschrift2("Erzeuger — Einzelauflistung je Projekt");
            k.Hinweis("Je Projekt eine Zeile pro Gerät (Modul) mit erzeugter Energie; bei BHKW/Kessel " +
                      "der Brennstoff, bei der Wärmepumpe Strom (inkl. Heizstab).");
            foreach (VariantenDaten v in daten.Varianten)
            {
                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                if (v.Ergebnis == null) { k.Text("(kein Ergebnis vorhanden)"); continue; }
                // BV-E5: dieselbe Liste wie {{stand.tabelle.erzeuger}}.
                k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Erzeuger(v, BerichtTexte.Englisch, k.Kultur)));
            }

            // ---------------- Brennstoffmengen je Projekt ----------------
            bool hatMengen = daten.Varianten.Any(v => v.Brennstoffmengen != null && v.Brennstoffmengen.Rows.Count > 0);
            if (hatMengen)
            {
                k.Ueberschrift2("Brennstoffmengen");
                k.Hinweis("Aus dem Brennstoffverbrauch über den projektspezifischen effektiven Heizwert " +
                          "in die Abrechnungseinheit umgerechnete Menge.");
                foreach (VariantenDaten v in daten.Varianten)
                {
                    k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                    // BV-E5: dieselbe Tafel wie {{stand.tabelle.brennstoffmengen}} (hier mit Leerzeile).
                    k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Brennstoffmengen(v, true, BerichtTexte.Englisch, k.Kultur)));
                }
            }
        }

        private static double? Wert(VariantenDaten v, string schluessel)
        { return v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null; }

        private static Zeichnung.Zeichenmodell Sicher(Func<Zeichnung.Zeichenmodell> f)
        {
            try { return f(); } catch { return null; }   // ein Diagrammfehler kippt nicht den Bericht
        }
    }
}
