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
        PufferBrauchwasser
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
            return Senke.Heizkreis;
        }

        /// <summary>Abbildung <see cref="Senke"/> -> Textwert der Spalte <c>WS_Ziel</c>.</summary>
        public static string ZielAusSenke(Senke senke)
        {
            switch (senke)
            {
                case Senke.PufferHeizung: return WaermesenkeClass.ZIEL_PUFFER_HEIZUNG;
                case Senke.PufferBrauchwasser: return WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER;
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
        /// <summary>Index des Erzeugermoduls in seiner Modulliste (in 4b: WP-Modul).</summary>
        public int Modulindex;

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
    /// Transportstruktur der zweikanaligen Kaskade (Konzept 6.1: „kein neuer Datentyp in
    /// den Erzeugermodulen, sondern eine Transportklasse in <c>SimulationControl</c>").
    ///
    /// Sie trägt alles, was die Stundenschleife der Reihenfolge-Invariante (6.3) braucht
    /// und was ein Erzeugermodul nicht selbst wissen kann: die Speicher-Registry, die
    /// Entladereihenfolge je Kanal (3.6), die vorsortierten Ladeaufträge (3.4/3.5) und
    /// die Senkenzuordnung je Modul (3.1).
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

        /// <summary>Heizungs-Puffer in Entladereihenfolge (Konzept 3.6), Phasen A und E.</summary>
        public List<SimulationPufferspeicher> EntladenHeizung = new List<SimulationPufferspeicher>();

        /// <summary>Brauchwasser-Puffer in Entladereihenfolge (Konzept 3.6), Phasen A und E.</summary>
        public List<SimulationPufferspeicher> EntladenBrauchwasser = new List<SimulationPufferspeicher>();

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

        /// <summary>Ladeaufträge in der für diese Stunde gültigen Reihenfolge (3.4/3.5).</summary>
        public List<Ladeauftrag> Ladeordnung_Stunde(bool pvUeberschuss)
        {
            return pvUeberschuss ? LadenMitPV : LadenOhnePV;
        }

        /// <summary>Entladereihenfolge des Kanals, den ein Speicher mit dieser Verwendung bedient.</summary>
        public List<SimulationPufferspeicher> Entladeordnung(bool brauchwasser)
        {
            return brauchwasser ? EntladenBrauchwasser : EntladenHeizung;
        }
    }
}
