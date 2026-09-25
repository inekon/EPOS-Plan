using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Strommengen-Matrix (Konzept Kap. 2.5 / Stufe W3, Phase 8) — die JAHRESSUMMEN der
    /// Strommengen, gebaut aus den Stundenreihen der In-Memory-Simulation
    /// (ZeitreihenSatz), dazu die Lastbilder der Bezugsseite.
    ///
    /// <para><b>Keine Tarifzonen</b> (Entscheid Q11, Anwender 22.09.2026: „kein HT/NT";
    /// Konzept Wirtschaftlichkeit § 3.5). Die Matrix trennte jede Stunde nach
    /// Winter/Sommer × HT/NT und bepreiste die vier Zonen mit eigenen Bezugs- und
    /// Einspeisepreisen und einer zweistufigen Leistungspreis-Staffel auf die höchste
    /// Stundenlast. Den Zeitzonentarif gibt es nicht mehr: Den Netzbezug bepreist der
    /// Stromträger der Kostenverwaltung — Arbeits-, Grund- und Leistungspreis samt der
    /// dorthin verlegten Staffel auf die Viertelstundenspitze
    /// (<c>KostenEmissionRechner</c>) — oder, bei aktivem Rollentarif,
    /// <see cref="StromTarifRechner"/>. Die Matrix führt nur noch Mengen und Lasten.</para>
    ///
    /// Geführt werden, je als Jahressumme:
    ///  - Netzbezug [MWh]           (Zeitreihe NETZBEZUG)
    ///  - PV-Einspeisung [MWh]      (Zeitreihe PV_UEBERSCHUSS)
    ///  - KWK-Eigenstrom [MWh]      (stundenweise min(BHKW-Strom, Strombedarf nach PV))
    ///
    /// Strombedarf heißt hier der Bedarf aller Verbraucher des Anschlusses
    /// (<see cref="ZeitreihenSatz.STROMBEDARF_GESAMT"/>, E26): derselbe Umfang, von dem der
    /// Netzbezug der Rest ist. Auch der KWK-Split misst sich daran — die Simulation lässt
    /// das BHKW den Strom der Wärmepumpe decken, also ist dieser Strom Eigenstrom.
    ///  - KWK-Einspeisung [MWh]     (BHKW-Strom − Eigenanteil)
    ///  - Bedarf ohne jede Eigenerzeugung und PV-Eigennutzung [MWh] (Etappen E5/E7)
    /// plus die höchste Stundenlast des Netzbezugs [kW] und die zwei Lastbilder, an
    /// denen die Leistungspreismodelle des Rollentarifs bemessen werden.
    ///
    /// Die KWK-Aufteilung ist eine dokumentierte Näherung: die Simulation führt
    /// den BHKW-Strom nicht getrennt nach Eigennutzung/Einspeisung — die
    /// stundenweise min-Regel bildet die Gleichzeitigkeit von Erzeugung und
    /// Bedarf ab (Grundlage des KWKG-Splits, Entscheidung 11.08.2026).
    /// </summary>
    public class StromMatrix
    {
        /// <summary>
        /// Schlüssel der EINEN Zeile je Projekt in <c>Tab_ErgebnisStromMatrix.Zone</c>.
        ///
        /// <para>Die Spalte hieß nach den vier Tarifzonen; die Tabelle bleibt, wie sie
        /// ist, und trägt jetzt eine Jahreszeile. Gelesen wird ohne Blick auf den
        /// Schlüssel — der Leser summiert alle Zeilen eines Projekts
        /// (<c>WirtschaftlichkeitCtrl.LadeStromMatrix</c>), damit ein gespeicherter
        /// Stand mit vier Zonenzeilen dieselben Jahressummen liefert, bis der
        /// Schemaschritt 104 ihn zu einer Zeile zusammenfasst oder ein neuer Lauf ihn
        /// ersetzt.</para>
        /// </summary>
        public const string ZEILE_JAHR = "Jahr";

        /// <summary>Netzbezug gesamt [MWh/a] (Plausibilitätsabgleich, Restbezug des
        /// Rollentarifs).</summary>
        public double BezugGesamtMWh;

        /// <summary>PV-Einspeisung gesamt [MWh/a].</summary>
        public double EinspeisungPvGesamtMWh;

        /// <summary>KWK-Eigenstrom gesamt [MWh/a] (Basis KWKG-Eigenstromsatz).</summary>
        public double KwkEigenGesamtMWh;

        /// <summary>KWK-Einspeisung gesamt [MWh/a] (Basis KWKG-Einspeisesatz).</summary>
        public double KwkEinspeisungGesamtMWh;

        /// <summary>
        /// ETAPPE E5 — Strombedarf <b>ohne die Anlage</b> [MWh/a]: die Menge, die ohne
        /// jede Eigenerzeugung aus dem Netz käme. Sie ist die Bezugsgröße der
        /// Differenzmethode („Bezugskosten ohne Anlage").
        ///
        /// <para><b>OHNE JEDE EIGENERZEUGUNG</b> (Konzept Wirtschaftlichkeit § 6.3
        /// Nr. 32, Entscheid U6‑Q1 vom 22.09.2026): der Strombedarf der Stunde VOR
        /// Abzug der PV-Eigennutzung. Die vermiedene Menge führt damit KWK- und
        /// PV-Eigenverbrauch. 0 zusammen mit <see cref="StrombedarfFehlt"/> heißt
        /// „nicht bestimmbar", nicht „null".</para>
        /// </summary>
        public double BedarfGesamtMWh;

        /// <summary>
        /// ETAPPE E7 (Konzept § 6.3 Nr. 32) — die PV-Eigennutzung, soweit sie den
        /// Strombedarf der Stunde deckt [MWh/a]: <c>Strombedarf − max(0, Strombedarf −
        /// PV-Eigennutzung)</c>. Genau um diese Menge ist <see cref="BedarfGesamtMWh"/>
        /// größer als der Bedarf nach Photovoltaik, auf den der KWK-Eigenanteil begrenzt
        /// bleibt — der Verteilschlüssel der vermiedenen Kosten bringt sie für die
        /// Photovoltaik ein.
        /// </summary>
        public double PvEigenGesamtMWh;

        /// <summary>
        /// Höchste STUNDENlast des Netzbezugs [kW]. Sie ist ein Stundenmittel und damit
        /// kleiner als die gemessene Viertelstundenspitze (<see cref="Netzbezugsspitze"/>),
        /// an der der Leistungspreis des Stromträgers — auch seine Staffel — bemessen
        /// wird; hier steht sie als Größe des Lastbilds (<see cref="LastBezug"/>) und im
        /// Bericht.
        /// </summary>
        public double MaxBezugKW;

        // ------------------------------------------------------- Lastbilder (Etappe E5)

        /// <summary>
        /// Die Maxima EINER Bezugsgröße [kW] — die Bemessungsgrundlage aller drei
        /// Leistungspreismodelle des Rollentarifs (Etappe E5, Leitentscheidung L7).
        ///
        /// <para>Ein Modell braucht genau eines davon: <c>JAHRESHOECHSTLAST</c> das
        /// Jahresmaximum, <c>STAFFEL</c> Sommer- und Wintermaximum getrennt,
        /// <c>MONATLICH</c> die zwölf Monatsmaxima. Alle drei werden im selben
        /// Stundendurchlauf gebildet — die Wahl des Modells darf keinen zweiten
        /// Durchlauf und keine zweite Wahrheit erzeugen.</para>
        /// </summary>
        public class Lastbild
        {
            /// <summary>Jahreshöchstlast [kW].</summary>
            public double MaxJahr;
            /// <summary>Höchstlast in der Sommerspanne [kW] (Ergänzung von <see cref="MaxWinter"/>).</summary>
            public double MaxSommer;
            /// <summary>Höchstlast in der Winterspanne [kW] (Monatsspanne des Tarifs).</summary>
            public double MaxWinter;
            /// <summary>Monatsmaxima [kW], Index 0 = Januar.</summary>
            public double[] MaxMonat = new double[12];

            /// <summary>Nimmt eine Stundenlast auf (kWh/h ≙ kW).</summary>
            public void Nimm(double kw, int monatIndex, bool winter)
            {
                if (kw > MaxJahr) MaxJahr = kw;
                if (winter) { if (kw > MaxWinter) MaxWinter = kw; }
                else { if (kw > MaxSommer) MaxSommer = kw; }
                if (monatIndex >= 0 && monatIndex < 12 && kw > MaxMonat[monatIndex])
                    MaxMonat[monatIndex] = kw;
            }

            /// <summary>Summe der zwölf Monatsmaxima [kW] — Bemessung des Monatsmodells.</summary>
            public double SummeMonatsmaxima
            {
                get { double s = 0; for (int i = 0; i < 12; i++) s += MaxMonat[i]; return s; }
            }
        }

        /// <summary>ETAPPE E5 — Lastbild des Strombedarfs OHNE Anlage (Referenz) —
        /// seit E7 der VOLLE Strombedarf ohne jede Eigenerzeugung (Konzept § 6.3
        /// Nr. 32): der Leistungsanteil der Bezugsseite hängt am Lastbild des Bedarfs vor
        /// Abzug der Photovoltaik.</summary>
        public Lastbild LastBedarf = new Lastbild();

        /// <summary>ETAPPE E5 — Lastbild des tatsächlichen Netzbezugs (Restbezug).</summary>
        public Lastbild LastBezug = new Lastbild();

        /// <summary>true, wenn die STROMBEDARF-Reihe fehlte — der KWK-Split gilt dann
        /// als „alles Eigenstrom" und wird im Ergebnis als Hinweis ausgewiesen.</summary>
        public bool StrombedarfFehlt;

        // ------------------------------------------------------------- Aufbau

        /// <summary>
        /// Baut die Matrix aus den Stundenreihen. Liefert null, wenn die
        /// Bezugsreihe fehlt (dann bleibt die Flat-Preisrechnung aktiv).
        ///
        /// <para>Der Tarifsatz liefert allein die <b>Winterspanne</b>
        /// (<see cref="TarifParameter.WinterVonMonat"/> bis
        /// <see cref="TarifParameter.WinterBisMonat"/>) für Sommer- und Wintermaximum
        /// der Lastbilder — die Bemessung des Leistungspreismodells
        /// <c>STAFFEL</c> im Rollentarif. Eine Zone bildet er nicht mehr.</para>
        /// </summary>
        public static StromMatrix Baue(ZeitreihenSatz zeitreihen, TarifParameter tarif)
        {
            if (zeitreihen == null || tarif == null) return null;
            double[] bezug = zeitreihen.Hole(ZeitreihenSatz.NETZBEZUG);
            // Nur ein VOLLES Jahr ist eine gültige Basis — kürzere Reihen würden die
            // Volljahres-Flatkosten still durch Teiljahreswerte ersetzen (Review Phase 8).
            if (bezug == null || bezug.Length < ZeitreihenSatz.Stunden) return null;

            double[] pvUeber = zeitreihen.Hole(ZeitreihenSatz.PV_UEBERSCHUSS);
            double[] bhkw = zeitreihen.Hole(ZeitreihenSatz.BHKW_STROM);
            // E26 (Befund N3): Bezugsgröße ist der Bedarf ALLER Verbraucher des Anschlusses
            // (Strombedarf des Projekts plus Wärmepumpe, Heizstab, Elektrokessel, Kältestrom
            // der Stufenrechnung) — dieselbe Menge, die ohne Eigenerzeugung aus dem Netz käme
            // und von der NETZBEZUG der Rest ist. Die Reihe STROMBEDARF allein führt den
            // Erzeugerstrom nicht; mit ihr wurde die vermiedene Menge in jedem
            // Wärmepumpenprojekt negativ. Fehlt die Gesamtreihe (Zeitreihensatz ohne
            // Simulationslauf), gilt STROMBEDARF wie bisher.
            double[] bedarf = zeitreihen.Hole(ZeitreihenSatz.STROMBEDARF_GESAMT)
                              ?? zeitreihen.Hole(ZeitreihenSatz.STROMBEDARF);
            double[] pvGenutzt = zeitreihen.Hole(ZeitreihenSatz.PV_GENUTZT);

            var m = new StromMatrix();
            m.StrombedarfFehlt = bedarf == null;

            // Referenzjahr 2026 (Standardjahr, kein Schaltjahr) für die Monatsgrenzen.
            DateTime start = new DateTime(2026, 1, 1);
            int stunden = ZeitreihenSatz.Stunden;

            for (int h = 0; h < stunden; h++)
            {
                DateTime t = start.AddHours(h);
                bool winter = IstWinter(t.Month, tarif.WinterVonMonat, tarif.WinterBisMonat);
                int monat = t.Month - 1;

                double b = bezug[h] / 1000.0;                     // kWh → MWh
                m.BezugGesamtMWh += b;
                if (b * 1000.0 > m.MaxBezugKW) m.MaxBezugKW = b * 1000.0;   // kWh/h ≙ kW
                m.LastBezug.Nimm(bezug[h], monat, winter);                  // E5

                if (pvUeber != null && h < pvUeber.Length)
                    m.EinspeisungPvGesamtMWh += pvUeber[h] / 1000.0;

                // ETAPPE E5/E7 — zwei Bedarfsgrößen aus derselben Stunde:
                //  * der Bedarf NACH Photovoltaik begrenzt wie seit W3 den KWK-Eigenanteil
                //    (min-Regel unten) — unverändert (Konzept § 6.3 Nr. 32: „der KWK-Split
                //    bleibt unverändert");
                //  * der Bedarf OHNE JEDE EIGENERZEUGUNG ist seit E7 die Bezugsgröße der
                //    vermiedenen Kosten (Menge und Lastbild) — vor Abzug der PV-Eigennutzung.
                // Die Differenz beider ist die PV-Eigennutzung, soweit sie Bedarf deckt.
                // Ohne Bedarfsreihe bleiben alle drei 0 (StrombedarfFehlt).
                double bedarfNachPv = 0;
                if (bedarf != null && h < bedarf.Length)
                {
                    double bedarfVoll = bedarf[h] > 0 ? bedarf[h] : 0;
                    bedarfNachPv = bedarf[h];
                    if (pvGenutzt != null && h < pvGenutzt.Length) bedarfNachPv -= pvGenutzt[h];
                    if (bedarfNachPv < 0) bedarfNachPv = 0;
                    m.BedarfGesamtMWh += bedarfVoll / 1000.0;
                    m.PvEigenGesamtMWh += Math.Max(0, bedarfVoll - bedarfNachPv) / 1000.0;
                    m.LastBedarf.Nimm(bedarfVoll, monat, winter);
                }

                if (bhkw != null && h < bhkw.Length)
                {
                    double erz = bhkw[h];
                    double eigen = erz;   // ohne Bedarfsreihe: alles Eigenstrom (Hinweis via StrombedarfFehlt)
                    if (bedarf != null && h < bedarf.Length)
                    {
                        // PV-Eigennutzung derselben Stunde ist oben bereits abgezogen —
                        // sonst wäre der KWK-Eigenanteil systematisch zu hoch.
                        eigen = Math.Min(erz, bedarfNachPv);
                    }
                    m.KwkEigenGesamtMWh += eigen / 1000.0;
                    m.KwkEinspeisungGesamtMWh += Math.Max(0, erz - eigen) / 1000.0;
                }
            }
            return m;
        }

        /// <summary>Monat in der (ggf. über den Jahreswechsel laufenden) Winterspanne?</summary>
        public static bool IstWinter(int monat, int von, int bis)
        {
            if (von <= bis) return monat >= von && monat <= bis;     // z. B. 1–3
            return monat >= von || monat <= bis;                     // z. B. 10–3
        }
    }
}
