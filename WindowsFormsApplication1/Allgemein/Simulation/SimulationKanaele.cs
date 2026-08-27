using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Transportstruktur der zweikanaligen Wärmerechnung (Konzept 6.1).
    ///
    /// Leitidee des Konzepts: KEIN neuer Datentyp in den Erzeugermodulen, sondern eine
    /// Transportklasse, die zwischen <see cref="SimulationControl"/> und den Modulen
    /// gereicht wird. Der Heizkanal und der Warmwasserkanal werden getrennt geführt;
    /// <see cref="Summe"/> stellt für die (noch) einkanaligen Rechenwege denselben Vektor
    /// bereit, mit dem sie heute arbeiten, und <see cref="Uebernehmen"/> verteilt deren
    /// Ergebnis wieder auf die beiden Kanäle.
    ///
    /// BENUTZT WIRD SIE NUR IM ZWEIKANALIGEN WEG (Etappe 4b), also wenn die
    /// Projekteinstellung <c>Kaskade_Zweikanalig</c> gesetzt ist (Konzept Kapitel 9,
    /// Feature-Flag). Der einkanalige Altpfad rührt sie nicht an — er bleibt die
    /// Rückfallebene und rechnet weiter auf einem Summenvektor.
    ///
    /// Feldgrößen sind wie im gesamten Rechenkern fest verdrahtet: 8760 Stunden,
    /// <c>float</c>-Vektoren mit Zwischenrechnung in <c>double</c>.
    /// </summary>
    public class Waermekanaele
    {
        /// <summary>Stundenzahl des Simulationsjahres — wie überall im Rechenkern fest.</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>Heizwärmebedarf bzw. -deckung je Stunde [kWh].</summary>
        public float[] Heiz = new float[STUNDEN_JAHR];

        /// <summary>Warmwasserbedarf bzw. -deckung je Stunde [kWh].</summary>
        public float[] WW = new float[STUNDEN_JAHR];

        /// <summary>
        /// Summe beider Kanäle je Stunde — die Sicht, mit der die einkanaligen
        /// Rechenwege (Kessel, BHKW, Solarthermie) heute arbeiten.
        ///
        /// Liefert bewusst einen NEUEN Vektor: Ein zurückgegebenes internes Array wäre in
        /// diesem Rechenkern eine Aliasing-Falle — die Module überschreiben ihre
        /// Eingangsvektoren in-place (siehe B0-2 in Konzept Kapitel 8), und ein solcher
        /// Schreibzugriff würde sonst stillschweigend den Heizkanal verändern.
        /// </summary>
        public float[] Summe()
        {
            float[] s = new float[STUNDEN_JAHR];
            for (int h = 0; h < STUNDEN_JAHR; h++)
                s[h] = Heiz[h] + WW[h];
            return s;
        }

        /// <summary>
        /// Übernimmt eine EINKANALIG ermittelte Restsumme und verteilt sie je Stunde
        /// proportional zum Kanalanteil dieser Stunde zurück auf Heiz- und WW-Kanal
        /// (Konzept 6.1, „Kompatibilitätsanker").
        ///
        /// Rechenregel je Stunde h:
        /// <code>
        ///   vorher = vorherHeiz[h] + vorherWW[h]
        ///   WW[h]   = restSumme[h] * vorherWW[h] / vorher
        ///   Heiz[h] = restSumme[h] - WW[h]
        /// </code>
        ///
        /// RANDFÄLLE — bewusst festgelegt und hier dokumentiert:
        ///
        /// 1. <b>Kanalanteil unbestimmt</b> (<c>vorher == 0</c>, also weder Heiz- noch
        ///    WW-Bedarf in dieser Stunde): Es gibt kein Verhältnis, nach dem verteilt
        ///    werden könnte. Der Rest geht VOLLSTÄNDIG auf den HEIZKANAL. Das ist
        ///    dieselbe Regel, mit der Konzept 3.2 (Entscheidung O2) die Netzverluste
        ///    zuordnet — die einzige altverhaltenserhaltende Variante, weil der
        ///    Warmwasserkanal in solchen Stunden nachweislich keinen Bedarf hatte.
        ///
        /// 2. <b>Restsumme 0</b>: Beide Kanäle werden 0. Das ist kein Sonderfall der
        ///    Formel, sondern fällt aus ihr heraus; es steht hier nur, weil es die
        ///    häufigste Stunde eines Jahreslaufs ist (Sommernacht ohne Bedarf).
        ///
        /// 3. <b>Rundungsrest</b>: <c>Heiz</c> wird als DIFFERENZ gebildet, nicht als
        ///    zweites Produkt. Damit gilt <c>Heiz[h] + WW[h] == restSumme[h]</c>
        ///    <b>bis auf höchstens ein ulp</b> (≤ 1,2·10⁻⁷ relativ) — die
        ///    Energieerhaltung hängt nicht daran, wie sich zwei getrennt gerundete
        ///    Produkte addieren. Der Rundungsrest landet aus demselben Grund wie in
        ///    Randfall 1 auf dem Heizkanal.
        ///
        ///    ZUR GENAUIGKEIT — die Zusicherung lautete bis zur Paket-4-Review
        ///    „bitgleich", und das ist nachweislich zu stark: Die Differenz
        ///    <c>rest − ww</c> wird in <c>float</c> gebildet und dabei GERUNDET, wenn
        ///    ihr exaktes Ergebnis nicht auf das Raster des Exponenten fällt. Die
        ///    Rückaddition <c>Heiz + WW</c> kann dann um ein ulp neben
        ///    <c>restSumme</c> liegen. Gegenbeispiel (im Selbsttest, Punkt 2b):
        ///    <c>rest = 207393100</c>, vorher <c>5,9786716 / 0,7120331</c>. Bitgleich
        ///    ist die Erhaltung überall dort, wo die Differenz exakt darstellbar ist —
        ///    also in praktisch allen Stunden eines Jahreslaufs mit Wärmemengen im
        ///    einstelligen bis dreistelligen kWh-Bereich.
        ///
        /// 4. <b>Negative Restsumme</b>: wird NICHT abgeschnitten, sondern nach derselben
        ///    Regel verteilt. Ein negativer Rest ist ein Bilanzfehler des Aufrufers
        ///    (Konzept 6.4 beschreibt genau so einen bei der Solarthermie); ihn hier
        ///    stillschweigend auf 0 zu klemmen würde ihn verstecken.
        ///
        /// <b>SEIT PAKET 6 OHNE PRODUKTIVEN AUFRUFER</b> (Nacharbeit, Befund N10): Das
        /// BHKW war die letzte Erzeugerart am Kompatibilitätsanker; im zweikanaligen Weg
        /// rechnen alle vier Arten auf den beiden Kanälen. Die Methode BLEIBT trotzdem —
        /// bewusst und begründet:
        ///
        ///   1. Sie ist die in Konzept 6.1 SPEZIFIZIERTE Kanalarithmetik, nicht ein
        ///      zufällig entstandener Helfer. Jede künftige einkanalige Stufe (ein
        ///      Importmodul, ein Fremdverfahren) braucht genau diese Regel — und sie
        ///      zweimal zu erfinden ist teurer, als sie einmal zu behalten.
        ///   2. Ihre Zusage ist die einzige, die im Selbsttest FESTGENAGELT ist: exakte
        ///      Erhaltung im Normalbereich, höchstens ein ULP im Extremfall (Punkte 2a
        ///      und 2b). Mit der Methode fielen sechs der acht Testfälle weg.
        ///
        /// Der Unterschied zu den beiden entfernten Methoden in <c>SimulationControl</c>
        /// ist genau das: Die waren private Hilfsmittel ohne Zusage und ohne Test.
        /// </summary>
        /// <param name="restSumme">einkanalig ermittelter Rest je Stunde [kWh]</param>
        /// <param name="vorherHeiz">Heizkanal VOR dem einkanaligen Schritt [kWh]</param>
        /// <param name="vorherWW">WW-Kanal VOR dem einkanaligen Schritt [kWh]</param>
        public void Uebernehmen(float[] restSumme, float[] vorherHeiz, float[] vorherWW)
        {
            if (restSumme == null) throw new ArgumentNullException("restSumme");
            if (vorherHeiz == null) throw new ArgumentNullException("vorherHeiz");
            if (vorherWW == null) throw new ArgumentNullException("vorherWW");

            if (restSumme.Length < STUNDEN_JAHR || vorherHeiz.Length < STUNDEN_JAHR ||
                vorherWW.Length < STUNDEN_JAHR)
                throw new ArgumentException(
                    "Waermekanaele.Uebernehmen erwartet Vektoren mit mindestens " +
                    STUNDEN_JAHR + " Stundenwerten.");

            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                float rest = restSumme[h];

                // Zwischenrechnung in double - Konvention des Rechenkerns.
                double vorher = (double)vorherHeiz[h] + (double)vorherWW[h];

                if (vorher > 0)
                {
                    // Randfall 3: WW proportional, Heiz als Differenz -> exakte Erhaltung.
                    float ww = (float)(rest * (vorherWW[h] / vorher));
                    WW[h] = ww;
                    Heiz[h] = rest - ww;
                }
                else
                {
                    // Randfall 1: kein Kanalanteil bekannt -> alles auf den Heizkanal.
                    Heiz[h] = rest;
                    WW[h] = 0f;
                }
            }
        }

        /// <summary>
        /// Tiefe Kopie: neue Vektoren mit denselben Werten. Nötig, weil die
        /// Erzeugermodule ihre Eingangsvektoren in-place überschreiben — eine flache
        /// Kopie würde den Zustand des Aufrufers mitverändern.
        /// </summary>
        public Waermekanaele Clone()
        {
            Waermekanaele k = new Waermekanaele();
            Array.Copy(Heiz, k.Heiz, STUNDEN_JAHR);
            Array.Copy(WW, k.WW, STUNDEN_JAHR);
            return k;
        }

#if DEBUG

        /// <summary>
        /// Selbsttest der Kanalarithmetik — ausschließlich im Debug-Build, nach dem
        /// Muster von <see cref="ErdreichTemperatur.Selbsttest"/> (kein Testcode im
        /// Release-Assembly). Wird nicht automatisch aufgerufen; das Ergebnis steht im
        /// Umsetzungsprotokoll zu Paket 4.
        ///
        /// ZUGESICHERT wird (jede Verletzung setzt das Gesamtergebnis auf FEHLGESCHLAGEN):
        ///   1. <see cref="Summe"/> = Heiz + WW und liefert einen eigenen Vektor
        ///   2. ERHALTUNG: nach <see cref="Uebernehmen"/> gilt Heiz + WW == Restsumme
        ///      bis auf höchstens EIN ULP, über alle 8760 Stunden eines gemischten
        ///      Testfalls (2a: bitgleich im normalen Wertebereich; 2b: das
        ///      Rundungs-Gegenbeispiel aus der Review, das die alte Zusage „bitgleich"
        ///      widerlegt und die neue einhält)
        ///   3. Proportionalität: 30/10 vorher, Rest 8 -> Heiz 6, WW 2
        ///   4. Randfall „Kanalanteil 0": Rest geht vollständig auf den Heizkanal
        ///   5. Randfall „Restsumme 0": beide Kanäle 0
        ///   6. negative Restsumme wird verteilt statt geklemmt
        ///   7. <see cref="Clone"/> kopiert Werte und trennt die Vektoren
        ///   8. <see cref="Senkenzuordnung"/>: Vorbelegung und Ziel-Abbildung hin und zurück
        ///
        /// SEIT PAKET 6 hat <see cref="Uebernehmen"/> keinen produktiven Aufrufer mehr
        /// (Nacharbeit, Befund N10) — die Punkte 2 bis 6 sichern damit eine
        /// SPEZIFIZIERTE, aber derzeit ungenutzte Zusage. Sie bleiben bewusst stehen:
        /// Die Begründung steht am Methodenkopf; ohne sie wäre die Kanalarithmetik aus
        /// Konzept 6.1 nirgends mehr geprüft.
        /// </summary>
        public static string Selbsttest()
        {
            StringBuilder sb = new StringBuilder();
            bool allesOk = true;

            sb.AppendLine("Selbsttest Waermekanaele / Senkenzuordnung (Konzept 6.1)");
            sb.AppendLine();

            // --- 1. Summe --------------------------------------------------
            Waermekanaele k = new Waermekanaele();
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                k.Heiz[h] = h % 7;
                k.WW[h] = (h % 3) * 0.5f;
            }
            float[] summe = k.Summe();
            bool summeOk = true;
            for (int h = 0; h < STUNDEN_JAHR; h++)
                if (summe[h] != k.Heiz[h] + k.WW[h]) { summeOk = false; break; }

            summe[0] = 999f;                       // eigener Vektor? (Aliasing-Probe)
            bool eigen = k.Heiz[0] != 999f && k.WW[0] != 999f;

            sb.AppendLine("1. Summe(): elementweise = " + (summeOk ? "OK" : "FEHLER") +
                          ", eigener Vektor = " + (eigen ? "OK" : "FEHLER"));
            if (!summeOk || !eigen) allesOk = false;

            // --- 2. Erhaltung ueber ein volles Jahr ------------------------
            // Gemischter Testfall: reine Heizstunden, reine WW-Stunden, gemischte
            // Stunden und Stunden ganz ohne Bedarf, dazu krumme Werte.
            float[] vorHeiz = new float[STUNDEN_JAHR];
            float[] vorWW = new float[STUNDEN_JAHR];
            float[] rest = new float[STUNDEN_JAHR];
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                switch (h % 4)
                {
                    case 0: vorHeiz[h] = 12.34f; vorWW[h] = 0f; break;       // nur Heizung
                    case 1: vorHeiz[h] = 0f; vorWW[h] = 3.7f; break;         // nur Warmwasser
                    case 2: vorHeiz[h] = 8.1f; vorWW[h] = 2.9f; break;       // gemischt
                    default: vorHeiz[h] = 0f; vorWW[h] = 0f; break;          // kein Bedarf
                }
                rest[h] = (h % 11) * 0.37f;
            }

            Waermekanaele erg = new Waermekanaele();
            erg.Uebernehmen(rest, vorHeiz, vorWW);

            int verletzt = 0;
            for (int h = 0; h < STUNDEN_JAHR; h++)
                if (erg.Heiz[h] + erg.WW[h] != rest[h]) verletzt++;

            sb.AppendLine("2a. Erhaltung Heiz + WW == Restsumme (bitgleich, Normalbereich): " +
                          (verletzt == 0 ? "OK" : "FEHLER in " + verletzt + " Stunden"));
            if (verletzt != 0) allesOk = false;

            // --- 2b. Rundungsfall: die Zusage lautet EIN ULP, nicht bitgleich --------
            // Wertemuster aus der Paket-4-Review. rest liegt bei 2,07e8; dort ist das
            // float-Raster 16 - die Differenz rest - ww fällt nicht darauf, und die
            // Rueckaddition landet ein ulp neben rest. Der Test sichert BEIDES ab:
            // dass die Abweichung auftritt (die alte Zusage also zu stark war) und
            // dass sie ein ulp nicht überschreitet.
            Waermekanaele ulpK = new Waermekanaele();
            float[] ulpRest = new float[STUNDEN_JAHR];
            float[] ulpHeiz = new float[STUNDEN_JAHR];
            float[] ulpWW = new float[STUNDEN_JAHR];
            ulpRest[42] = 207393100f;
            ulpHeiz[42] = 5.9786716f;
            ulpWW[42] = 0.7120331f;
            ulpK.Uebernehmen(ulpRest, ulpHeiz, ulpWW);

            float ulpSumme = ulpK.Heiz[42] + ulpK.WW[42];
            double ulpAbw = Math.Abs((double)ulpSumme - ulpRest[42]);
            // ein ulp bei diesem Exponenten: 2^-23 relativ, großzügig als 1,2e-7 gefasst
            double ulpGrenze = Math.Abs((double)ulpRest[42]) * 1.2e-7;
            bool ulpOk = ulpAbw <= ulpGrenze;

            sb.AppendLine("2b. Rundungsfall rest=207393100, vorher 5,9786716/0,7120331 -> " +
                          "Abweichung " + ulpAbw.ToString("G4") + " kWh (Grenze 1 ulp = " +
                          ulpGrenze.ToString("G4") + ")   " + (ulpOk ? "OK" : "FEHLER") +
                          (ulpAbw > 0 ? "   [nicht bitgleich - genau dafür steht der Fall]" : ""));
            if (!ulpOk) allesOk = false;

            // --- 3. Proportionalitaet --------------------------------------
            Waermekanaele p = new Waermekanaele();
            float[] pRest = new float[STUNDEN_JAHR];
            float[] pHeiz = new float[STUNDEN_JAHR];
            float[] pWW = new float[STUNDEN_JAHR];
            pRest[100] = 8f; pHeiz[100] = 30f; pWW[100] = 10f;
            p.Uebernehmen(pRest, pHeiz, pWW);
            bool proOk = Math.Abs(p.Heiz[100] - 6f) < 1e-4 && Math.Abs(p.WW[100] - 2f) < 1e-4;
            sb.AppendLine("3. Proportional 30/10 bei Rest 8 -> Heiz " + p.Heiz[100] +
                          " / WW " + p.WW[100] + "   " + (proOk ? "OK" : "FEHLER"));
            if (!proOk) allesOk = false;

            // --- 4. Randfall Kanalanteil 0 ---------------------------------
            Waermekanaele r0 = new Waermekanaele();
            float[] r0Rest = new float[STUNDEN_JAHR];
            r0Rest[200] = 5f;                       // vorher-Vektoren bleiben 0
            r0.Uebernehmen(r0Rest, new float[STUNDEN_JAHR], new float[STUNDEN_JAHR]);
            bool r0Ok = r0.Heiz[200] == 5f && r0.WW[200] == 0f;
            sb.AppendLine("4. Kanalanteil 0 -> Heiz " + r0.Heiz[200] + " / WW " + r0.WW[200] +
                          "   " + (r0Ok ? "OK" : "FEHLER"));
            if (!r0Ok) allesOk = false;

            // --- 5. Randfall Restsumme 0 -----------------------------------
            bool s0Ok = erg.Heiz[0] == 0f && erg.WW[0] == 0f;   // h = 0: rest = 0, vorher 12,34/0
            sb.AppendLine("5. Restsumme 0 bei vorhandenem Bedarf -> Heiz " + erg.Heiz[0] +
                          " / WW " + erg.WW[0] + "   " + (s0Ok ? "OK" : "FEHLER"));
            if (!s0Ok) allesOk = false;

            // --- 6. negative Restsumme -------------------------------------
            Waermekanaele neg = new Waermekanaele();
            float[] nRest = new float[STUNDEN_JAHR];
            float[] nHeiz = new float[STUNDEN_JAHR];
            float[] nWW = new float[STUNDEN_JAHR];
            nRest[300] = -4f; nHeiz[300] = 3f; nWW[300] = 1f;
            neg.Uebernehmen(nRest, nHeiz, nWW);
            bool negOk = Math.Abs(neg.Heiz[300] + 3f) < 1e-4 && Math.Abs(neg.WW[300] + 1f) < 1e-4;
            sb.AppendLine("6. Restsumme -4 bei 3/1 -> Heiz " + neg.Heiz[300] + " / WW " +
                          neg.WW[300] + "   " + (negOk ? "OK" : "FEHLER"));
            if (!negOk) allesOk = false;

            // --- 7. Clone ---------------------------------------------------
            Waermekanaele kopie = erg.Clone();
            bool gleich = true;
            for (int h = 0; h < STUNDEN_JAHR; h++)
                if (kopie.Heiz[h] != erg.Heiz[h] || kopie.WW[h] != erg.WW[h]) { gleich = false; break; }
            kopie.Heiz[500] = -77f;
            bool getrennt = erg.Heiz[500] != -77f;
            sb.AppendLine("7. Clone(): Werte gleich = " + (gleich ? "OK" : "FEHLER") +
                          ", Vektoren getrennt = " + (getrennt ? "OK" : "FEHLER"));
            if (!gleich || !getrennt) allesOk = false;

            // --- 8. Senkenzuordnung ----------------------------------------
            Senkenzuordnung sz = new Senkenzuordnung();
            bool szOk = sz.Haupt == Senke.Heizkreis && sz.Zweit == null &&
                        sz.IDPufferHaupt == 0 && sz.IDPufferZweit == 0 &&
                        sz.WSTyp == WaermequelleClass.SENKE_BEIDES;

            szOk &= Senkenzuordnung.SenkeAusZiel(WaermesenkeClass.ZIEL_PUFFER_HEIZUNG) == Senke.PufferHeizung;
            szOk &= Senkenzuordnung.SenkeAusZiel(WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER) == Senke.PufferBrauchwasser;
            szOk &= Senkenzuordnung.SenkeAusZiel("Unfug") == Senke.Heizkreis;
            szOk &= Senkenzuordnung.SenkeAusZiel(null) == Senke.Heizkreis;
            szOk &= Senkenzuordnung.ZielAusSenke(Senke.PufferBrauchwasser) == WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER;
            szOk &= Senkenzuordnung.ZielAusSenke(Senke.PufferHeizung) == WaermesenkeClass.ZIEL_PUFFER_HEIZUNG;
            szOk &= Senkenzuordnung.ZielAusSenke(Senke.Heizkreis) == WaermesenkeClass.ZIEL_HEIZKREIS;

            // D5a: das dritte Puffer-Ziel muss hin und zurueck abbilden, und der
            // Kombispeicher muss BEIDE Kanaele bedienen (Anforderungen 4/7).
            szOk &= Senkenzuordnung.SenkeAusZiel(WaermesenkeClass.ZIEL_PUFFER_KOMBI) == Senke.PufferKombi;
            szOk &= Senkenzuordnung.ZielAusSenke(Senke.PufferKombi) == WaermesenkeClass.ZIEL_PUFFER_KOMBI;
            szOk &= WaermesenkeClass.IstPufferZiel(WaermesenkeClass.ZIEL_PUFFER_KOMBI);
            szOk &= WaermesenkeClass.VerwendungZuZiel(WaermesenkeClass.ZIEL_PUFFER_KOMBI) ==
                    WaermesenkeClass.VERWENDUNG_KOMBI;

            SimulationPufferspeicher kombi = new SimulationPufferspeicher
            { Verwendung = SimulationPufferspeicher.VERWENDUNG_KOMBI };
            szOk &= kombi.IstKombi && kombi.BedientKanal(true) && kombi.BedientKanal(false) &&
                    !kombi.IstBrauchwasserkanal && !kombi.IstQuelle;

            SimulationPufferspeicher heiz = new SimulationPufferspeicher
            { Verwendung = SimulationPufferspeicher.VERWENDUNG_HEIZUNG };
            szOk &= heiz.BedientKanal(false) && !heiz.BedientKanal(true);

            SimulationPufferspeicher quelle = new SimulationPufferspeicher
            { Verwendung = SimulationPufferspeicher.VERWENDUNG_QUELLE };
            szOk &= !quelle.BedientKanal(false) && !quelle.BedientKanal(true);

            sb.AppendLine("8. Senkenzuordnung Vorbelegung und Ziel-Abbildung: " + (szOk ? "OK" : "FEHLER"));
            if (!szOk) allesOk = false;

            sb.AppendLine();
            sb.AppendLine(allesOk ? "ERGEBNIS: alle Pruefungen bestanden."
                                  : "ERGEBNIS: mindestens eine Pruefung FEHLGESCHLAGEN.");
            return sb.ToString();
        }

#endif
    }

    /// <summary>
    /// Die Bedarfskanäle des Dreikanalmodells als INDIZES (Konzept 4.1, Leitentscheidung
    /// L2 — Paket K1).
    ///
    /// Kanäle sind bewusst indiziert und nicht boolesch: Jede Kanalstruktur des
    /// Rechenkerns (Restbedarf, Entladeordnung, Durchsatzbudget, <c>SenkeAbziehen</c>)
    /// läuft künftig über diesen Index. Damit ist der Rechenkern auf MEHRERE HEIZKREISE
    /// vorbereitet — es wäre allein <see cref="ANZAHL"/> zu erhöhen; Persistenz und
    /// Oberfläche kanalbezogener Parameter blieben ein eigener Ausbauschritt.
    ///
    /// Die Reihenfolge der Indizes ist KEINE Rangfolge. Die Knappheitsreihenfolge des
    /// Abzugs (Konzept 4.3: Brauchwasser → Prozess → Heizung) ist eine eigene Größe —
    /// seit Paket K2 steht sie als <see cref="KnappheitsReihenfolge"/> daneben und wird
    /// je Lauf aus der Projekteinstellung gebildet.
    /// </summary>
    public static class Kanal
    {
        /// <summary>Raumwärme: Gebäudewärme und externe Lastgänge ohne eigene Kanalangabe.</summary>
        public const int HEIZUNG = 0;

        /// <summary>Trinkwarmwasser: Brauchwasserprofile und als Brauchwasser gekennzeichnete Lastgänge.</summary>
        public const int BRAUCHWASSER = 1;

        /// <summary>Prozesswärme: Prozessprofile und als Prozesswärme gekennzeichnete Lastgänge.</summary>
        public const int PROZESS = 2;

        /// <summary>Zahl der Kanäle. Alle Kanalfelder werden über diese Konstante bemessen.</summary>
        public const int ANZAHL = 3;

        /// <summary>
        /// Abbildung des PERSISTENZWERTES einer Kanalzuordnung auf den Kanalindex
        /// (Drei-Schichten-Regel: in der Datenbank steht deutscher, eingefrorener Text —
        /// <see cref="DbWerte.KANAL_HEIZUNG"/> &amp; Co., im Rechenkern der Index).
        ///
        /// LEER, <c>null</c> und JEDER UNBEKANNTE WERT ergeben den HEIZKANAL. Das ist die
        /// altverhaltenserhaltende Vorbelegung aus Konzept 4.2/F18: Bestandsganglinien
        /// tragen keine Kanalangabe und sind bis heute im Heizbedarf mitgelaufen.
        ///
        /// Der Vergleich ist bewusst toleranter als <see cref="Senkenzuordnung.SenkeAusZiel"/>
        /// (dort ordinal): Der Wert kommt aus einer NEUEN Spalte über Bestandsdaten, in
        /// der neben NULL auch Leerstrings und abweichende Groß-/Kleinschreibung
        /// vorkommen können.
        /// </summary>
        public static int AusText(string kanal)
        {
            if (string.IsNullOrWhiteSpace(kanal)) return HEIZUNG;

            string wert = kanal.Trim();
            if (string.Equals(wert, DbWerte.KANAL_BRAUCHWASSER, StringComparison.OrdinalIgnoreCase))
                return BRAUCHWASSER;
            if (string.Equals(wert, DbWerte.KANAL_PROZESS, StringComparison.OrdinalIgnoreCase))
                return PROZESS;
            return HEIZUNG;
        }

        /// <summary>Sprechender Name eines Kanalindex — ausschließlich für Protokolltexte.</summary>
        public static string Name(int kanal)
        {
            switch (kanal)
            {
                case BRAUCHWASSER: return DbWerte.KANAL_BRAUCHWASSER;
                case PROZESS: return DbWerte.KANAL_PROZESS;
                default: return DbWerte.KANAL_HEIZUNG;
            }
        }

        // ==============================================================
        // KNAPPHEITSREIHENFOLGE (Konzept 4.3, Entscheidung F10 — Paket K2)
        // ==============================================================

        /// <summary>
        /// Vorbelegung der Knappheitsreihenfolge: BRAUCHWASSER → PROZESS → HEIZUNG
        /// (Konzept 4.3). Warmwasser zuerst ist das Komfortkriterium der App
        /// („Beides (Warmwasser zuerst)"), Prozess vor Heizung die Abwägung
        /// Produktionsausfall gegen Raumkomfort.
        ///
        /// Das Feld ist <c>private</c> und wird NIE herausgegeben: Ein öffentliches
        /// <c>int[]</c> wäre veränderlich, und ein einziger Schreibzugriff irgendwo im
        /// Haus verstellte die Abzugsregel des gesamten Rechenkerns. Kopien liefert
        /// <see cref="KnappheitVorgabe"/>; die einzige Stelle, die das Feld direkt liest,
        /// ist <see cref="KnappheitsReihenfolge"/> selbst.
        /// </summary>
        private static readonly int[] KNAPPHEIT_STANDARD = { BRAUCHWASSER, PROZESS, HEIZUNG };

        /// <summary>Eine EIGENE Kopie der Vorbelegung {B, P, H} (siehe <see cref="KNAPPHEIT_STANDARD"/>).</summary>
        public static int[] KnappheitVorgabe()
        {
            return (int[])KNAPPHEIT_STANDARD.Clone();
        }

        /// <summary>
        /// Parst die projektweite Übersteuerung der Knappheitsreihenfolge
        /// (<c>Tab_Einstellungen.Kanal_Knappheitsreihenfolge</c>, Konzept 4.3/F10) in ein
        /// Feld von Kanalindizes.
        ///
        /// FORMAT: sprachneutrale ASCII-Schlüssel, getrennt durch Semikolon —
        /// <c>BRAUCHWASSER;PROZESS;HEIZUNG</c>. Das sind KEINE Anzeigetexte und keine
        /// Persistenzwerte der Kanalspalte (die heißen deutsch „Brauchwasser" /
        /// „Prozesswaerme" / „Heizung", <see cref="DbWerte.KANAL_HEIZUNG"/> &amp; Co.),
        /// sondern Steuerwerte nach der zweiten Schicht der Drei-Schichten-Regel; sie
        /// stehen als <c>DbWerte.KNAPPHEIT_*</c>. Komma wird als Trenner mitakzeptiert —
        /// die Spalte wird von Hand gepflegt, und ein Komma ist der wahrscheinlichste
        /// Tippfehler.
        ///
        /// GÜLTIG ist ausschließlich eine Reihenfolge, die JEDEN Kanal GENAU EINMAL nennt.
        /// Alles andere (leer, unbekannter Schlüssel, doppelter Kanal, fehlender Kanal)
        /// ergibt die Vorbelegung {B, P, H} und EINE Protokollwarnung je Lauf. Eine
        /// unvollständige Reihenfolge zu „ergänzen" wäre die schlechtere Wahl: Der
        /// Anwender bekäme eine Ordnung, die er nicht eingestellt hat, und keine Meldung
        /// darüber, dass seine Eingabe unbrauchbar war.
        /// </summary>
        /// <param name="spec">Rohtext der Projekteinstellung; <c>null</c>/leer = Vorbelegung.</param>
        public static int[] KnappheitsReihenfolge(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec)) return KnappheitVorgabe();

            string[] teile = spec.Split(new char[] { ';', ',' },
                                        StringSplitOptions.RemoveEmptyEntries);

            int[] ordnung = new int[ANZAHL];
            bool[] gesehen = new bool[ANZAHL];
            int n = 0;
            bool ok = teile.Length == ANZAHL;

            for (int i = 0; ok && i < teile.Length; i++)
            {
                int kanal = AusSchluessel(teile[i]);
                if (kanal < 0 || gesehen[kanal]) { ok = false; break; }

                gesehen[kanal] = true;
                ordnung[n++] = kanal;
            }

            if (!ok || n != ANZAHL)
            {
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "knappheitsreihenfolge-ungueltig",
                    "Knappheitsreihenfolge: Die Projekteinstellung „" + spec.Trim() +
                    "\" ist unbrauchbar - erwartet werden die drei Schlüssel " +
                    DbWerte.KNAPPHEIT_BRAUCHWASSER + ";" + DbWerte.KNAPPHEIT_PROZESS + ";" +
                    DbWerte.KNAPPHEIT_HEIZUNG + " in beliebiger Reihenfolge, jeder genau " +
                    "einmal. Gerechnet wird mit der Vorbelegung " +
                    Name(BRAUCHWASSER) + " -> " + Name(PROZESS) + " -> " + Name(HEIZUNG) + ".");
                return KnappheitVorgabe();
            }

            return ordnung;
        }

        /// <summary>
        /// Abbildung STEUERWERT (<c>DbWerte.KNAPPHEIT_*</c>) → Kanalindex; −1 = unbekannt.
        ///
        /// Der Vergleich ist tolerant gegen Groß-/Kleinschreibung und umgebende
        /// Leerzeichen (die Spalte wird von Hand gepflegt), aber NICHT gegen andere
        /// Schreibweisen: Ein unbekannter Schlüssel muss auffallen, sonst rechnete der
        /// Lauf still mit einer anderen Reihenfolge als der eingestellten.
        /// </summary>
        private static int AusSchluessel(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return -1;

            string wert = schluessel.Trim();
            if (string.Equals(wert, DbWerte.KNAPPHEIT_BRAUCHWASSER, StringComparison.OrdinalIgnoreCase))
                return BRAUCHWASSER;
            if (string.Equals(wert, DbWerte.KNAPPHEIT_PROZESS, StringComparison.OrdinalIgnoreCase))
                return PROZESS;
            if (string.Equals(wert, DbWerte.KNAPPHEIT_HEIZUNG, StringComparison.OrdinalIgnoreCase))
                return HEIZUNG;
            return -1;
        }
    }

    /// <summary>
    /// Transportstruktur der DREIKANALIGEN Bedarfsrechnung (Konzept 4.1, Paket K1) —
    /// die Verallgemeinerung von <see cref="Waermekanaele"/> auf <see cref="Kanal.ANZAHL"/>
    /// indizierte Kanäle.
    ///
    /// SEIT PAKET K2 ist sie die Transportstruktur des GANZEN Rechenwegs: Die Kaskade
    /// holt ihre Kanäle über <c>SimulationWaermebedarf.KanaeleDrei()</c> und schreibt die
    /// Restbedarfe in dieselbe Struktur zurück. Die Übergangsabbildung
    /// <c>SimulationWaermebedarf.Kanaele()</c> auf <see cref="Waermekanaele"/> hat damit
    /// keinen Aufrufer mehr; <see cref="Waermekanaele"/> bleibt allein als die in
    /// Konzept 6.1 spezifizierte Kanalarithmetik samt ihrem Selbsttest bestehen.
    ///
    /// Feldgrößen wie im gesamten Rechenkern fest verdrahtet: 8760 Stunden,
    /// <c>float</c>-Vektoren mit Zwischenrechnung in <c>double</c>.
    /// </summary>
    public class Kanalsatz
    {
        /// <summary>Stundenzahl des Simulationsjahres — wie überall im Rechenkern fest.</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>
        /// Bedarf bzw. Deckung je Kanal und Stunde [kWh]:
        /// <c>Bedarf[<see cref="Kanal.HEIZUNG"/>][h]</c> usw. Die
        /// <see cref="Kanal.ANZAHL"/> Vektoren werden im Konstruktor angelegt; das
        /// äußere Feld ist <c>readonly</c>, damit niemand die Kanalstruktur austauscht —
        /// die Vektoren selbst werden (Konvention des Rechenkerns) in-place beschrieben.
        /// </summary>
        public readonly float[][] Bedarf;

        public Kanalsatz()
        {
            Bedarf = new float[Kanal.ANZAHL][];
            for (int k = 0; k < Kanal.ANZAHL; k++)
                Bedarf[k] = new float[STUNDEN_JAHR];
        }

        /// <summary>Heizkanal — Kurzform für <c>Bedarf[Kanal.HEIZUNG]</c>.</summary>
        public float[] Heizung { get { return Bedarf[Kanal.HEIZUNG]; } }

        /// <summary>Brauchwasserkanal — Kurzform für <c>Bedarf[Kanal.BRAUCHWASSER]</c>.</summary>
        public float[] Brauchwasser { get { return Bedarf[Kanal.BRAUCHWASSER]; } }

        /// <summary>Prozesskanal — Kurzform für <c>Bedarf[Kanal.PROZESS]</c>.</summary>
        public float[] Prozess { get { return Bedarf[Kanal.PROZESS]; } }

        /// <summary>
        /// Summe aller Kanäle je Stunde — die Sicht, mit der die (noch) einkanaligen
        /// Rechenwege und alle Altleser des Gesamtbedarfs arbeiten (Dauerlinie, Maximum,
        /// Monatswerte).
        ///
        /// Liefert bewusst einen NEUEN Vektor: Ein zurückgegebenes internes Array wäre in
        /// diesem Rechenkern eine Aliasing-Falle — die Module überschreiben ihre
        /// Eingangsvektoren in-place (Regel B0-2, Konzept 8), und ein solcher
        /// Schreibzugriff würde sonst stillschweigend einen Kanal verändern.
        ///
        /// GERUNDET WIRD NACH JEDEM SCHRITT auf <c>float</c> — dieselbe Konvention wie in
        /// <see cref="WPPlan.Core.BhkwPlan.VectorenAddieren"/>, mit der der Bestand seinen
        /// Summenvektor aufgebaut hat. Die Addition läuft in Indexreihenfolge
        /// (Heizung → Brauchwasser → Prozess); da float-Addition nicht assoziativ ist,
        /// kann das Ergebnis um bis zu ein ULP neben einer anders geklammerten Summe
        /// derselben Werte liegen (Konzept 4.2, Toleranz „1-ULP-Klasse").
        /// </summary>
        public float[] Summe()
        {
            float[] s = new float[STUNDEN_JAHR];
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                float w = Bedarf[0][h];
                for (int k = 1; k < Kanal.ANZAHL; k++)
                    w = (float)((double)w + Bedarf[k][h]);
                s[h] = w;
            }
            return s;
        }

        /// <summary>
        /// Verteilt einen KONSTANTEN Stundenbetrag (die Netzverluste, Konzept 4.2/F2)
        /// je Stunde PROPORTIONAL zu den Kanalbedarfen dieser Stunde.
        ///
        /// Rechenregel je Stunde h und Kanal k &gt; 0:
        /// <code>
        ///   summe   = Σ Bedarf[i][h]
        ///   anteil  = betrag · Bedarf[k][h] / summe
        ///   Heizung = Heizung + (betrag − Σ anteil)      // Rest als DIFFERENZ
        /// </code>
        ///
        /// RANDFÄLLE — bewusst festgelegt:
        ///
        /// 1. <b>Kanalanteil unbestimmt</b> (<c>summe ≤ 0</c>, also in dieser Stunde
        ///    überhaupt kein Bedarf): Der Betrag geht VOLLSTÄNDIG auf den HEIZKANAL.
        ///    Dieselbe Randfallregel wie in <see cref="Waermekanaele.Uebernehmen"/> und
        ///    ausdrücklich so in Konzept 4.2 festgelegt.
        /// 2. <b>Rundungsrest</b>: Der Heizanteil wird als DIFFERENZ gebildet, nicht als
        ///    weiteres Produkt. Damit ist die aufgeschlagene Menge je Stunde exakt
        ///    <c>betrag</c> — bis auf die float-Rundung der Rückaddition (dieselbe
        ///    ULP-Zusage wie in <see cref="Waermekanaele.Uebernehmen"/>, Randfall 3).
        ///
        /// ERGEBNISWIRKUNG (F2, entschieden 27.08.2026): Das ersetzt die
        /// Altverhaltens-Zuordnung „Netzverluste vollständig auf Heizung". Für jedes
        /// Projekt MIT Brauchwasser- oder Prozessanteil ändert sich damit die
        /// Kanalaufteilung — die Jahressumme bleibt unverändert.
        /// </summary>
        /// <param name="betragJeStunde">Netzverlust je Stunde [kWh], konstant über das Jahr.</param>
        public void NetzverlusteVerteilen(float betragJeStunde)
        {
            float[] heiz = Bedarf[Kanal.HEIZUNG];

            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                // Zwischenrechnung in double - Konvention des Rechenkerns.
                double summe = 0;
                for (int k = 0; k < Kanal.ANZAHL; k++)
                    summe += Bedarf[k][h];

                if (summe > 0)
                {
                    double vergeben = 0;
                    for (int k = 0; k < Kanal.ANZAHL; k++)
                    {
                        if (k == Kanal.HEIZUNG) continue;
                        float anteil = (float)(betragJeStunde * (Bedarf[k][h] / summe));
                        Bedarf[k][h] = (float)((double)Bedarf[k][h] + anteil);
                        vergeben += anteil;
                    }
                    heiz[h] = (float)((double)heiz[h] + ((double)betragJeStunde - vergeben));
                }
                else
                {
                    // Randfall 1: kein Kanalanteil bekannt -> alles auf den Heizkanal.
                    heiz[h] = (float)((double)heiz[h] + betragJeStunde);
                }
            }
        }

        /// <summary>
        /// Tiefe Kopie: neue Vektoren mit denselben Werten. Nötig, weil die
        /// Erzeugermodule ihre Eingangsvektoren in-place überschreiben — eine flache
        /// Kopie würde den Zustand des Aufrufers mitverändern (Regel B0-2).
        /// </summary>
        public Kanalsatz Clone()
        {
            Kanalsatz k = new Kanalsatz();
            for (int i = 0; i < Kanal.ANZAHL; i++)
                Array.Copy(Bedarf[i], k.Bedarf[i], STUNDEN_JAHR);
            return k;
        }

        /// <summary>
        /// Toleranzmaßstab der Energieprobe (Konzept 4.2 und 11.3): „1-ULP-Klasse".
        ///
        /// EINE Stelle für Selbsttest UND Laufprobe — jede zweite Fassung wäre die
        /// Stelle, an der beide auseinanderlaufen. Ab Betrag 1 gilt die relative Grenze
        /// 1,2·10⁻⁷ (ein ulp im float-Raster, großzügig gefasst wie im bestehenden
        /// <see cref="Waermekanaele.Selbsttest"/>), darunter die absolute Grenze 10⁻⁶ —
        /// dort ist die relative Grenze kleiner als jede sinnvolle Wärmemenge.
        ///
        /// <paramref name="rundungsschritte"/> ist die ZAHL DER float-SPEICHERUNGEN, die
        /// die beiden verglichenen Größen trennen. Vorbelegung 1 = die Grundregel oben,
        /// unverändert. Sie zu kennen ist kein Feinschliff, sondern nötig: Wer eine
        /// double-Referenz gegen eine Kette aus n float-Zwischenspeicherungen hält, misst
        /// die Summe von n Rundungen. Der Grundwert 1,2·10⁻⁷ deckt rund zwei davon ab
        /// (eine halbe ulp sind 6·10⁻⁸ relativ); bei fünf Rundungen — drei Kanalvektoren
        /// plus zwei Additionen in <see cref="Summe"/> — schlägt eine feste
        /// Ein-Schritt-Grenze in einem Teil der 8760 Stunden an, OHNE dass irgendetwas
        /// falsch gerechnet wäre. Eine Probe, die in jedem Lauf grundlos meldet, ist
        /// keine Probe mehr. Strukturfehler (ein verschluckter oder doppelt gebuchter
        /// Anteil) liegen um Größenordnungen darüber und werden auch mit dem
        /// aufgeweiteten Maßstab sicher gefunden.
        /// </summary>
        public static bool ErhaltungOk(double erwartet, double ist, int rundungsschritte = 1)
        {
            double abweichung = Math.Abs(ist - erwartet);
            double betrag = Math.Abs(erwartet);
            double grenze = betrag >= 1.0 ? betrag * 1.2e-7 : 1e-6;
            if (rundungsschritte > 1) grenze *= rundungsschritte;
            return abweichung <= grenze;
        }

        /// <summary>
        /// Rundungsschritte zwischen einer double-Referenzsumme und
        /// <see cref="Summe"/>: je Kanal eine Speicherung des Kanalwertes, dazu die
        /// Additionen der Summenbildung. Der Maßstab der Energieprobe (11.3).
        /// </summary>
        public const int ERHALTUNG_SCHRITTE_SUMME = 2 * Kanal.ANZAHL - 1;

#if DEBUG

        /// <summary>
        /// Selbsttest des Kanalsatzes — ausschließlich im Debug-Build, nach dem Muster
        /// von <see cref="Waermekanaele.Selbsttest"/> (kein Testcode im Release-Assembly).
        /// Wird nicht automatisch aufgerufen; das Ergebnis steht im Umsetzungsprotokoll
        /// zu Paket K1.
        ///
        /// ZUGESICHERT wird (jede Verletzung setzt das Gesamtergebnis auf FEHLGESCHLAGEN):
        ///   1. Konstruktion: <see cref="Kanal.ANZAHL"/> genullte Vektoren à 8760, alle
        ///      voneinander getrennt
        ///   2. <see cref="Summe"/> = schrittweise float-Summe der Kanäle und liefert
        ///      einen EIGENEN Vektor (Aliasing-Probe)
        ///   3. <see cref="Clone"/> kopiert alle Kanäle und trennt die Vektoren
        ///   4. <see cref="NetzverlusteVerteilen"/>: Proportionalität (60/30/10 bei
        ///      Betrag 10 → 6/3/1) und Randfall „kein Bedarf" (alles auf Heizung)
        ///   5. ERHALTUNG über ein volles Jahr: nach der Verteilung gilt je Stunde
        ///      Kanalsumme == vorherige Kanalsumme + Betrag, im Maßstab
        ///      <see cref="ErhaltungOk"/> (1-ULP-Klasse) — dieselbe Zusage, die die
        ///      Energieprobe des Laufs prüft (Konzept 11.3)
        ///   6. <see cref="Kanal.AusText"/>: die drei Persistenzwerte, dazu leer, null
        ///      und Unfug → Heizkanal
        ///   7. <see cref="Kanal.KnappheitsReihenfolge"/> (Paket K2): Vorbelegung
        ///      {B, P, H} als EIGENER Vektor, gültige Übersteuerung, tolerante
        ///      Schreibweise — und Rückfall auf die Vorbelegung bei jeder unbrauchbaren
        ///      Eingabe (leer, Unfug, unvollständig, doppelt, zu viele)
        /// </summary>
        public static string Selbsttest()
        {
            StringBuilder sb = new StringBuilder();
            bool allesOk = true;

            sb.AppendLine("Selbsttest Kanalsatz (Konzept 4.1/4.2, Paket K1)");
            sb.AppendLine();

            // --- 1. Konstruktion -------------------------------------------
            Kanalsatz neu = new Kanalsatz();
            bool bauOk = neu.Bedarf != null && neu.Bedarf.Length == Kanal.ANZAHL;
            for (int k = 0; bauOk && k < Kanal.ANZAHL; k++)
            {
                if (neu.Bedarf[k] == null || neu.Bedarf[k].Length != STUNDEN_JAHR) { bauOk = false; break; }
                for (int h = 0; h < STUNDEN_JAHR; h++)
                    if (neu.Bedarf[k][h] != 0f) { bauOk = false; break; }
            }
            // Vektoren getrennt? (ein gemeinsames Array waere die schlimmste Falle)
            neu.Bedarf[Kanal.BRAUCHWASSER][7] = 1f;
            bauOk &= neu.Bedarf[Kanal.HEIZUNG][7] == 0f && neu.Bedarf[Kanal.PROZESS][7] == 0f;
            sb.AppendLine("1. Konstruktion: " + Kanal.ANZAHL + " genullte, getrennte Vektoren = " +
                          (bauOk ? "OK" : "FEHLER"));
            if (!bauOk) allesOk = false;

            // --- 2. Summe ---------------------------------------------------
            Kanalsatz k2 = new Kanalsatz();
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                k2.Heizung[h] = h % 7;
                k2.Brauchwasser[h] = (h % 3) * 0.5f;
                k2.Prozess[h] = (h % 5) * 0.25f;
            }
            float[] summe = k2.Summe();
            bool summeOk = true;
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                float w = k2.Heizung[h];
                w = (float)((double)w + k2.Brauchwasser[h]);
                w = (float)((double)w + k2.Prozess[h]);
                if (summe[h] != w) { summeOk = false; break; }
            }
            summe[0] = 999f;                       // eigener Vektor? (Aliasing-Probe)
            bool eigen = k2.Heizung[0] != 999f && k2.Brauchwasser[0] != 999f && k2.Prozess[0] != 999f;
            sb.AppendLine("2. Summe(): elementweise = " + (summeOk ? "OK" : "FEHLER") +
                          ", eigener Vektor = " + (eigen ? "OK" : "FEHLER"));
            if (!summeOk || !eigen) allesOk = false;

            // --- 3. Clone ---------------------------------------------------
            Kanalsatz kopie = k2.Clone();
            bool gleich = true;
            for (int k = 0; k < Kanal.ANZAHL && gleich; k++)
                for (int h = 0; h < STUNDEN_JAHR; h++)
                    if (kopie.Bedarf[k][h] != k2.Bedarf[k][h]) { gleich = false; break; }
            kopie.Prozess[500] = -77f;
            bool getrennt = k2.Prozess[500] != -77f;
            sb.AppendLine("3. Clone(): Werte gleich = " + (gleich ? "OK" : "FEHLER") +
                          ", Vektoren getrennt = " + (getrennt ? "OK" : "FEHLER"));
            if (!gleich || !getrennt) allesOk = false;

            // --- 4. Netzverluste: Proportionalitaet und Randfall ------------
            Kanalsatz nv = new Kanalsatz();
            nv.Heizung[100] = 60f; nv.Brauchwasser[100] = 30f; nv.Prozess[100] = 10f;
            nv.NetzverlusteVerteilen(10f);
            bool proOk = Math.Abs(nv.Heizung[100] - 66f) < 1e-3 &&
                         Math.Abs(nv.Brauchwasser[100] - 33f) < 1e-3 &&
                         Math.Abs(nv.Prozess[100] - 11f) < 1e-3;
            // Stunde 200 hat keinen Bedarf -> alles auf den Heizkanal
            bool randOk = nv.Heizung[200] == 10f && nv.Brauchwasser[200] == 0f && nv.Prozess[200] == 0f;
            sb.AppendLine("4. Netzverluste 10 auf 60/30/10 -> " + nv.Heizung[100] + "/" +
                          nv.Brauchwasser[100] + "/" + nv.Prozess[100] + "   " +
                          (proOk ? "OK" : "FEHLER") + "; Randfall ohne Bedarf -> Heizung " +
                          nv.Heizung[200] + "   " + (randOk ? "OK" : "FEHLER"));
            if (!proOk || !randOk) allesOk = false;

            // --- 5. Erhaltung ueber ein volles Jahr -------------------------
            // Gemischter Testfall: reine Heizstunden, reine Brauchwasserstunden,
            // Prozessstunden, gemischte Stunden und Stunden ganz ohne Bedarf.
            Kanalsatz jahr = new Kanalsatz();
            double[] vorher = new double[STUNDEN_JAHR];
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                switch (h % 5)
                {
                    case 0: jahr.Heizung[h] = 12.34f; break;
                    case 1: jahr.Brauchwasser[h] = 3.7f; break;
                    case 2: jahr.Prozess[h] = 7.03f; break;
                    case 3: jahr.Heizung[h] = 8.1f; jahr.Brauchwasser[h] = 2.9f; jahr.Prozess[h] = 1.7f; break;
                    default: break;                                  // kein Bedarf
                }
                vorher[h] = (double)jahr.Heizung[h] + jahr.Brauchwasser[h] + jahr.Prozess[h];
            }
            const float betrag = 0.4713f;
            jahr.NetzverlusteVerteilen(betrag);
            float[] nachher = jahr.Summe();
            int verletzt = 0;
            double groesste = 0;
            for (int h = 0; h < STUNDEN_JAHR; h++)
            {
                double erwartet = vorher[h] + betrag;
                double abw = Math.Abs((double)nachher[h] - erwartet);
                if (abw > groesste) groesste = abw;
                if (!ErhaltungOk(erwartet, nachher[h], ERHALTUNG_SCHRITTE_SUMME)) verletzt++;
            }
            sb.AppendLine("5. Erhaltung Kanalsumme == vorher + Netzverlust (1-ULP-Klasse, " +
                          ERHALTUNG_SCHRITTE_SUMME + " Rundungsschritte): " +
                          (verletzt == 0 ? "OK" : "FEHLER in " + verletzt + " Stunden") +
                          ", groesste Abweichung " + groesste.ToString("G4") + " kWh");
            if (verletzt != 0) allesOk = false;

            // --- 6. Kanal.AusText ------------------------------------------
            bool textOk = Kanal.AusText(DbWerte.KANAL_HEIZUNG) == Kanal.HEIZUNG &&
                          Kanal.AusText(DbWerte.KANAL_BRAUCHWASSER) == Kanal.BRAUCHWASSER &&
                          Kanal.AusText(DbWerte.KANAL_PROZESS) == Kanal.PROZESS &&
                          Kanal.AusText(null) == Kanal.HEIZUNG &&
                          Kanal.AusText("") == Kanal.HEIZUNG &&
                          Kanal.AusText("   ") == Kanal.HEIZUNG &&
                          Kanal.AusText("Unfug") == Kanal.HEIZUNG &&
                          Kanal.AusText(" " + DbWerte.KANAL_PROZESS.ToUpperInvariant() + " ") == Kanal.PROZESS;
            sb.AppendLine("6. Kanal.AusText(): Persistenzwerte und Vorbelegung Heizung = " +
                          (textOk ? "OK" : "FEHLER"));
            if (!textOk) allesOk = false;

            // --- 7. Kanal.KnappheitsReihenfolge (Paket K2, F10) -------------
            // Zugesichert: die Vorbelegung, eine gültige Übersteuerung, und dass JEDE
            // unbrauchbare Eingabe auf die Vorbelegung zurückfällt statt eine halbe
            // Ordnung zu liefern (fehlender Kanal, doppelter Kanal, Unfug, leer).
            int[] vorgabe = Kanal.KnappheitVorgabe();
            bool knappOk = vorgabe.Length == Kanal.ANZAHL &&
                           vorgabe[0] == Kanal.BRAUCHWASSER && vorgabe[1] == Kanal.PROZESS &&
                           vorgabe[2] == Kanal.HEIZUNG;

            // Eigener Vektor? (dieselbe Aliasing-Falle wie bei Summe())
            vorgabe[0] = -1;
            knappOk &= Kanal.KnappheitVorgabe()[0] == Kanal.BRAUCHWASSER;

            int[] uebersteuert = Kanal.KnappheitsReihenfolge(
                DbWerte.KNAPPHEIT_HEIZUNG + ";" + DbWerte.KNAPPHEIT_BRAUCHWASSER + ";" +
                DbWerte.KNAPPHEIT_PROZESS);
            knappOk &= uebersteuert.Length == Kanal.ANZAHL && uebersteuert[0] == Kanal.HEIZUNG &&
                       uebersteuert[1] == Kanal.BRAUCHWASSER && uebersteuert[2] == Kanal.PROZESS;

            // Kleinschreibung und Leerzeichen sind zulässig, Komma als Trenner auch.
            int[] locker = Kanal.KnappheitsReihenfolge(
                " " + DbWerte.KNAPPHEIT_PROZESS.ToLowerInvariant() + " , " +
                DbWerte.KNAPPHEIT_HEIZUNG + " ; " + DbWerte.KNAPPHEIT_BRAUCHWASSER);
            knappOk &= locker[0] == Kanal.PROZESS && locker[1] == Kanal.HEIZUNG &&
                       locker[2] == Kanal.BRAUCHWASSER;

            string[] unbrauchbar =
            {
                null, "", "   ", "Unfug",
                DbWerte.KNAPPHEIT_BRAUCHWASSER,                                  // unvollständig
                DbWerte.KNAPPHEIT_BRAUCHWASSER + ";" + DbWerte.KNAPPHEIT_BRAUCHWASSER +
                    ";" + DbWerte.KNAPPHEIT_HEIZUNG,                             // doppelt
                DbWerte.KNAPPHEIT_BRAUCHWASSER + ";" + DbWerte.KNAPPHEIT_PROZESS + ";" +
                    DbWerte.KNAPPHEIT_HEIZUNG + ";" + DbWerte.KNAPPHEIT_HEIZUNG  // zu viele
            };
            foreach (string s in unbrauchbar)
            {
                int[] r = Kanal.KnappheitsReihenfolge(s);
                knappOk &= r.Length == Kanal.ANZAHL && r[0] == Kanal.BRAUCHWASSER &&
                           r[1] == Kanal.PROZESS && r[2] == Kanal.HEIZUNG;
            }

            sb.AppendLine("7. Kanal.KnappheitsReihenfolge(): Vorbelegung, Übersteuerung und " +
                          "Rückfall bei unbrauchbarer Eingabe = " + (knappOk ? "OK" : "FEHLER"));
            if (!knappOk) allesOk = false;

            sb.AppendLine();
            sb.AppendLine(allesOk ? "ERGEBNIS: alle Pruefungen bestanden."
                                  : "ERGEBNIS: mindestens eine Pruefung FEHLGESCHLAGEN.");
            return sb.ToString();
        }

#endif
    }

    /// <summary>
    /// Ziel einer Wärmemenge (Konzept 6.1). Die Werte entsprechen den Textwerten der
    /// Spalte <c>WS_Ziel</c> (<see cref="WaermesenkeClass"/>) — die Abbildung steht in
    /// <see cref="Senkenzuordnung.SenkeAusZiel"/> bzw.
    /// <see cref="Senkenzuordnung.ZielAusSenke"/>.
    ///
    /// In der Datenbank bleibt der TEXT die führende Ablage (Drei-Schichten-Regel,
    /// Konzept 13.6): DB-Werte sind deutsch und unlokalisiert, der enum ist die
    /// Rechendarstellung.
    /// </summary>
    public enum Senke
    {
        /// <summary>Direkte Deckung des Momentanbedarfs — Verhalten wie bisher.</summary>
        Heizkreis,

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Heizung".</summary>
        PufferHeizung,

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Brauchwasser".</summary>
        PufferBrauchwasser,

        /// <summary>
        /// Die Anlage lädt einen KOMBISPEICHER (Verwendung „Kombi", Etappe D5a): einen
        /// Puffer, der Heizung und Warmwasser aus EINEM Vorrat bedient. Für die
        /// Ladephasen C/D ist das kein Sonderfall — geladen wird kanalneutral; der
        /// Unterschied steckt allein in der Entladung (Kaskadenschleife, K-1).
        /// </summary>
        PufferKombi
    }

    /// <summary>
    /// Senkenzuordnung genau einer Anlage (<c>Tab_Energieanlagen.ID</c>), Konzept 6.1.
    ///
    /// Jede Anlage hat GENAU EINE Hauptsenke und optional eine Zweitsenke. Daraus folgt
    /// die Reihenfolge-Invariante aus Konzept 6.3: Eine Anlage steht eindeutig entweder
    /// in der Bedarfskaskade (Hauptsenke Heizkreis) oder in der Ladephase (Hauptsenke
    /// Puffer) — nur die Zweitsenke überlappt.
    ///
    /// Gefüllt wird sie von <see cref="WaermesenkeClass.SenkenLaden"/>; ausgewertet wird
    /// sie im zweikanaligen Weg (<c>Kaskadenkontext.SenkeJeModul</c>). Der einkanalige
    /// Altpfad wertet sie nicht aus — dort entscheidet weiter <c>WS_Typ</c> allein.
    /// </summary>
    public class Senkenzuordnung
    {
        /// <summary>Tab_Energieanlagen.ID der Anlage.</summary>
        public int AnlagenID;

        /// <summary>Hauptsenke (WS_Ziel).</summary>
        public Senke Haupt = Senke.Heizkreis;

        /// <summary>WS_ID_Puffer — 0 = keiner (in der Datenbank NULL, nie 0: Fremdschlüssel).</summary>
        public int IDPufferHaupt;

        /// <summary>Zweitsenke (WS_Ziel2); <c>null</c> = keine.</summary>
        public Senke? Zweit;

        /// <summary>WS_ID_Puffer2 — 0 = keiner.</summary>
        public int IDPufferZweit;

        /// <summary>
        /// WS_Typ — Bedarfsart; nur wirksam, wenn <see cref="Haupt"/> = <see cref="Senke.Heizkreis"/>
        /// ist (Konzept 3.1). Werte: <see cref="WaermequelleClass.SENKE_BEIDES"/> |
        /// <see cref="WaermequelleClass.SENKE_WARMWASSER"/> | <see cref="WaermequelleClass.SENKE_HEIZUNG"/>.
        /// </summary>
        public string WSTyp = WaermequelleClass.SENKE_BEIDES;

        /// <summary>true, wenn eine Zweitsenke gesetzt ist.</summary>
        public bool HatZweitsenke
        {
            get { return Zweit.HasValue; }
        }

        /// <summary>
        /// Abbildung Textwert (<c>WS_Ziel</c>) -> <see cref="Senke"/>. Alles Unbekannte,
        /// Leere und <c>null</c> wird zu <see cref="Senke.Heizkreis"/> — dieselbe Regel,
        /// mit der <see cref="WaermesenkeClass.Normalisieren"/> arbeitet (Konzept 4.6,
        /// erste Zeile der Tabelle).
        /// </summary>
        public static Senke SenkeAusZiel(string ziel)
        {
            if (string.Equals(ziel, WaermesenkeClass.ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return Senke.PufferHeizung;
            if (string.Equals(ziel, WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return Senke.PufferBrauchwasser;
            if (string.Equals(ziel, WaermesenkeClass.ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return Senke.PufferKombi;
            return Senke.Heizkreis;
        }

        /// <summary>Abbildung <see cref="Senke"/> -> Textwert der Spalte <c>WS_Ziel</c>.</summary>
        public static string ZielAusSenke(Senke senke)
        {
            switch (senke)
            {
                case Senke.PufferHeizung: return WaermesenkeClass.ZIEL_PUFFER_HEIZUNG;
                case Senke.PufferBrauchwasser: return WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER;
                case Senke.PufferKombi: return WaermesenkeClass.ZIEL_PUFFER_KOMBI;
                default: return WaermesenkeClass.ZIEL_HEIZKREIS;
            }
        }

        public override string ToString()
        {
            string s = "Anlage " + AnlagenID + ": " + Haupt;
            if (IDPufferHaupt > 0) s += " (Puffer " + IDPufferHaupt + ")";
            if (Haupt == Senke.Heizkreis) s += " [" + WSTyp + "]";
            if (HatZweitsenke)
            {
                s += " + Zweitsenke " + Zweit.Value;
                if (IDPufferZweit > 0) s += " (Puffer " + IDPufferZweit + ")";
            }
            return s;
        }
    }

    /// <summary>
    /// EIN Ladevorgang der Ladephase: eine Anlage lädt EINEN Speicher (Konzept 6.3 C/D).
    ///
    /// Der Auftrag verbindet die Rechenseite (Modulindex, Speicherinstanz) mit der
    /// bereits aufgelösten Ordnungsseite aus <see cref="Ladeordnung"/> — Ladepriorität,
    /// PV-Sonderpriorität (3.5) und die nach der Auflösungsregel 3.4 ermittelte
    /// Obergrenze. Es wird nichts nachgerechnet, was dort schon steht: Anzeige und
    /// Engine benutzen dieselbe Quelle.
    /// </summary>
    public class Ladeauftrag
    {
        /// <summary>
        /// Index des Erzeugermoduls in der Modulliste SEINER Erzeugerart: bei
        /// <c>TYP_WP</c> die Position in <c>SimulationWaermepumpe.wp_list</c>, bei
        /// <c>TYP_SOLARTHERMIE</c> das Kollektorfeld
        /// (<c>SimulationSolarthermie.solar_anlagen_ids</c>), bei <c>TYP_KESSEL</c> der
        /// Kessel (<c>SimulationSPK.spk_anlagen_ids</c>).
        /// </summary>
        public int Modulindex;

        /// <summary>
        /// Erzeugerart der ladenden Anlage (<c>ProjektPuffer.TYP_*</c>) — sie entscheidet,
        /// welches Modul <see cref="Modulindex"/> auflöst und die Ladung bucht
        /// (Paket 5: Solarthermie und Heizkessel stehen mit in der Ladeordnung).
        /// </summary>
        public int Erzeugerart = ProjektPuffer.TYP_WP;

        /// <summary>Tab_Energieanlagen.ID der ladenden Anlage.</summary>
        public int AnlagenID;

        /// <summary>true = der Speicher ist die ZWEITsenke dieser Anlage (Phase D).</summary>
        public bool Zweitsenke;

        /// <summary>Zielspeicher — dieselbe Instanz wie in der Registry.</summary>
        public SimulationPufferspeicher Speicher;

        /// <summary>
        /// Ladeobergrenze als ANTEIL der nutzbaren Kapazität (0…1) in Stunden OHNE
        /// PV-Überschuss, aufgelöst nach Konzept 3.4
        /// (<c>Ladeordnung.ObergrenzenAufloesen</c>): eigene Ladegrenze, sonst
        /// <c>Schwelle_Aus</c> für die vorrangige und <c>Schwelle_Aus_Nachrang</c> für
        /// nachrangige Anlagen. Ladefähigkeit = <c>Q_max · Obergrenze − SOC</c>.
        /// </summary>
        public double Obergrenze = 0.95;

        /// <summary>
        /// Dieselbe Größe für Stunden MIT PV-Überschuss (Konzept 3.5). Der Vorrang an
        /// einem Puffer — und damit die Obergrenze — hängt an der Priorität, und die ist
        /// zeitabhängig: Zieht <c>WS_Ladeprio_PV</c> eine Anlage nach vorn, gilt für sie
        /// in dieser Stunde <c>Schwelle_Aus</c> statt der Reservezone. Ohne
        /// PV-Sonderpriorität sind beide Werte gleich.
        /// </summary>
        public double ObergrenzePV = 0.95;

        /// <summary>Wirksame Ladepriorität ohne PV-Sonderfall (Konzept 3.4).</summary>
        public int Ladeprio = Ladeordnung.PRIO_SONSTIGE;

        /// <summary>Betriebsmodus der Anlage (BM_Typ) — Bedingung der PV-Sonderregel.</summary>
        public string BMTyp = "";

        /// <summary>
        /// RECHENEBENE der ladenden Anlage (Etappe D5a, Konzept Abschnitt 5
        /// „Kessel-Kaskade"): 0 = die Anlage hat keinen Quellpuffer oder ihr Quellpuffer
        /// wird in diesem Lauf von niemandem geladen; n = sie bezieht ihre Quellwärme aus
        /// einem Puffer, den eine Anlage der Ebene n−1 lädt.
        ///
        /// Die Kaskadenschleife durchläuft die Phasen B/C/D je Ebene aufsteigend — damit
        /// rechnet ein Erzeuger mit Puffer-Quelle NACH „seinem" Puffer. Bei genau einer
        /// Ebene (jedes Bestandsprojekt) ist die Schleife Anweisung für Anweisung die
        /// bisherige.
        /// </summary>
        public int Ebene = 0;

        /// <summary>Die in dieser Stunde gültige Obergrenze (Konzept 3.4/3.5).</summary>
        public double ObergrenzeStunde(bool pvUeberschuss)
        {
            return pvUeberschuss ? ObergrenzePV : Obergrenze;
        }

        public override string ToString()
        {
            return "Anlage " + AnlagenID + (Zweitsenke ? " [Zweitsenke]" : "") +
                   " -> Puffer " + (Speicher != null ? Speicher.ID_Pufferspeicher : 0) +
                   " (Prio " + Ladeprio + ", Obergrenze " +
                   (Obergrenze * 100).ToString("0.#") + " %, mit PV " +
                   (ObergrenzePV * 100).ToString("0.#") + " %)";
        }
    }

    /// <summary>
    /// EINE Entnahme aus einem Quellpuffer durch einen nachgelagerten Erzeuger
    /// (Etappe D5a, Kessel-Kaskade).
    ///
    /// Das Erzeugermodul bucht die Entnahme physikalisch selbst (es ruft
    /// <see cref="SimulationPufferspeicher.Entladen"/>), kennt aber die
    /// HERKUNFTSRECHNUNG nicht — die führt allein die Kaskadenschleife (Regel
    /// „Vermischung im Speicher", Nutzerentscheidung 5-1). Über diese Meldung erfährt sie,
    /// welche Menge welchem Speicher entnommen wurde und wohin sie gegangen ist:
    ///
    ///   <see cref="Ziel"/> = <c>null</c> → die Wärme hat DIREKT Bedarf gedeckt; sie wird
    ///   wie eine bedarfsdeckende Entladung den Ladern des Quellpuffers gutgeschrieben —
    ///   also demjenigen, der sie erzeugt hat, nicht dem, der sie nur angehoben hat.
    ///
    ///   <see cref="Ziel"/> ≠ <c>null</c> → die Wärme ist in einen anderen Speicher
    ///   gewandert; ihre Herkunftsanteile werden mit umgebucht.
    /// </summary>
    public class Quellentnahme
    {
        /// <summary>Speicher, aus dem entnommen wurde.</summary>
        public SimulationPufferspeicher Quelle;

        /// <summary>Tatsächlich entnommene Wärmemenge [kWh].</summary>
        public double Menge;

        /// <summary>Zielspeicher der Wärme; <c>null</c> = Direktdeckung.</summary>
        public SimulationPufferspeicher Ziel;
    }

    /// <summary>
    /// Transportstruktur der DREIKANALIGEN Kaskade (Konzept 6.1: „kein neuer Datentyp in
    /// den Erzeugermodulen, sondern eine Transportklasse in <c>SimulationControl</c>").
    ///
    /// Sie trägt alles, was die Stundenschleife der Reihenfolge-Invariante (6.3) braucht
    /// und was ein Erzeugermodul nicht selbst wissen kann: die Speicher-Registry, die
    /// Entladereihenfolge je Kanal (3.6), die Knappheitsreihenfolge des Laufs (4.3), die
    /// vorsortierten Ladeaufträge (3.4/3.5) und die Senkenzuordnung je Modul (3.1).
    ///
    /// Aufgebaut wird sie EINMAL je Lauf von <c>SimulationControl</c>; die Module lesen
    /// nur. Die Speicherinstanzen sind dieselben Objekte wie in der Registry — es gibt
    /// keine zweite Speicherverwaltung (Konzept 6.2).
    /// </summary>
    public class Kaskadenkontext
    {
        public int ID_Projekt;

        /// <summary>
        /// ALLE Speicher des Laufs in Aufnahmereihenfolge der Registry — die Menge, für
        /// die <c>StundeAbschliessen()</c> je Stunde GENAU EINMAL läuft (Phase G).
        /// Enthält Senken- UND Quellspeicher; die geteilte Instanz je Puffer-ID sorgt
        /// dafür, dass ein von zwei Modulen benutzter Quellspeicher hier nur einmal steht.
        /// </summary>
        public List<SimulationPufferspeicher> AlleSpeicher = new List<SimulationPufferspeicher>();

        /// <summary>
        /// Entladereihenfolge JE KANAL (Konzept 3.6), Phasen A und E —
        /// <c>Entladen[<see cref="Kanal.HEIZUNG"/>]</c> usw. Ersetzt seit Paket K2 die
        /// beiden Einzellisten <c>EntladenHeizung</c>/<c>EntladenBrauchwasser</c>.
        ///
        /// Ein Speicher steht in der Liste JEDES Kanals seines Klassen-Sets (Konzept 6.1);
        /// dieselbe Instanz kann also mehrfach vorkommen — genau das ist der
        /// Kombispeicher, verallgemeinert. Die Reihenfolge, in der die Kanäle abgearbeitet
        /// werden, ist die Knappheitsreihenfolge <see cref="Knappheit"/> (4.3).
        ///
        /// Die Listen werden im Konstruktor angelegt; das äußere Feld bleibt zuweisbar,
        /// weil <c>SimulationControl</c> die fertigen Ordnungen einhängt.
        /// </summary>
        public List<SimulationPufferspeicher>[] Entladen = new List<SimulationPufferspeicher>[Kanal.ANZAHL];

        /// <summary>
        /// KNAPPHEITSREIHENFOLGE des Laufs (Konzept 4.3, F10): die Kanalindizes in der
        /// Ordnung, in der ein knappes Wärmeangebot vergeben wird. Vorbelegung
        /// {BRAUCHWASSER, PROZESS, HEIZUNG}; übersteuert wird sie projektweit über
        /// <c>Tab_Einstellungen.Kanal_Knappheitsreihenfolge</c>
        /// (<see cref="Kanal.KnappheitsReihenfolge"/>).
        ///
        /// Sie gilt an DREI Stellen und deshalb nur EINMAL hier: im Abzug
        /// (<c>Kaskadenschleife.SenkeAbziehen</c>), in der Entladung eines Speichers mit
        /// mehrelementigem Klassen-Set (Phasen A/E — die verallgemeinerte Kombi-Regel K-1)
        /// und bei der Abbuchung des Durchsatzbudgets.
        /// </summary>
        public int[] Knappheit = Kanal.KnappheitVorgabe();

        public Kaskadenkontext()
        {
            for (int k = 0; k < Kanal.ANZAHL; k++)
                Entladen[k] = new List<SimulationPufferspeicher>();
        }

        /// <summary>
        /// Ladeaufträge in der Reihenfolge für Stunden OHNE PV-Überschuss (Konzept 3.4).
        /// Haupt- und Zweitsenken stehen in derselben Liste; die Phasen C und D filtern
        /// über <see cref="Ladeauftrag.Zweitsenke"/>, die Reihenfolge bleibt dieselbe.
        /// </summary>
        public List<Ladeauftrag> LadenOhnePV = new List<Ladeauftrag>();

        /// <summary>
        /// Dieselben Aufträge in der Reihenfolge für Stunden MIT PV-Überschuss — die
        /// zeitabhängige Priorität aus Konzept 3.5. Zwei vorsortierte Listen statt einer
        /// Sortierung je Stunde: Das Ergebnis ist dasselbe, aber es wird nicht 8760-mal
        /// sortiert.
        /// </summary>
        public List<Ladeauftrag> LadenMitPV = new List<Ladeauftrag>();

        /// <summary>
        /// Senkenzuordnung je Erzeugermodul, indexgleich mit der Modulliste. Ein
        /// <c>null</c>-Eintrag bedeutet „Vorbelegung": Hauptsenke Heizkreis, Bedarfsart
        /// Beides.
        /// </summary>
        public List<Senkenzuordnung> SenkeJeModul = new List<Senkenzuordnung>();

        /// <summary>
        /// Protokollzeilen des Kontextaufbaus (Konzept 13.4: dialogfrei). Hier landen die
        /// Abgrenzungen von Etappe 4b — etwa eine BHKW-Anlage mit migrierter Puffer-Senke,
        /// die bis Paket 6 wie eine Heizkreis-Anlage rechnet.
        /// </summary>
        public List<string> Hinweise = new List<string>();

        /// <summary>
        /// QUELLPUFFER je Anlage (Etappe D5a): <c>Tab_Energieanlagen.ID</c> →
        /// Speicherinstanz, die diese Anlage als WÄRMEQUELLE benutzt
        /// (<c>WQ_Typ = Pufferspeicher</c>, <c>WQ_ID_Puffer</c>).
        ///
        /// Enthalten sind NUR Bezüge auf Speicher, die in diesem Lauf mitrechnen — die
        /// Menge, aus der die Rechenebenen (<see cref="Ladeauftrag.Ebene"/>) und der
        /// Zyklus-Guard gebildet werden. Ein Quellbezug auf einen reinen Quellspeicher
        /// (Erdsonde-Ersatz, nicht von einem Erzeuger geladen) steht hier ebenfalls, führt
        /// aber zu Ebene 0: Er hat keinen Lader, auf den zu warten wäre.
        /// </summary>
        public Dictionary<int, SimulationPufferspeicher> QuellpufferJeAnlage =
            new Dictionary<int, SimulationPufferspeicher>();

        /// <summary>Ladeaufträge in der für diese Stunde gültigen Reihenfolge (3.4/3.5).</summary>
        public List<Ladeauftrag> Ladeordnung_Stunde(bool pvUeberschuss)
        {
            return pvUeberschuss ? LadenMitPV : LadenOhnePV;
        }

        /// <summary>
        /// Entladereihenfolge EINES Kanals (Konzept 3.6). Ein unbekannter Index liefert
        /// eine leere Liste statt einer Ausnahme — der Rechenkern bleibt dialogfrei, und
        /// ein Kanal ohne Speicher ist der Normalfall.
        /// </summary>
        public List<SimulationPufferspeicher> Entladeordnung(int kanal)
        {
            if (Entladen == null || kanal < 0 || kanal >= Entladen.Length || Entladen[kanal] == null)
                return new List<SimulationPufferspeicher>();
            return Entladen[kanal];
        }

        /// <summary>
        /// ÜBERGANGSBRÜCKE der zweikanaligen Fassung (Paket K2). Sie bleibt allein für
        /// Aufrufer, die noch in Heiz-/Warmwasser-Begriffen denken; neuer Code benutzt
        /// <see cref="Entladeordnung(int)"/>. Der PROZESSkanal ist über sie nicht
        /// erreichbar — das ist Absicht, sie soll nicht wachsen.
        /// </summary>
        public List<SimulationPufferspeicher> Entladeordnung(bool brauchwasser)
        {
            return Entladeordnung(brauchwasser ? Kanal.BRAUCHWASSER : Kanal.HEIZUNG);
        }

        /// <summary>true, wenn mindestens ein KOMBISPEICHER im Lauf mitrechnet (D5a).</summary>
        public bool HatKombispeicher()
        {
            foreach (SimulationPufferspeicher sp in AlleSpeicher)
                if (sp != null && sp.IstKombi) return true;
            return false;
        }
    }
}
