using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die drei ANZEIGEÜBERSETZUNGEN des Stromspeichers — eine Stelle statt drei
    /// (iU9-W11a.5, Befund W11-B42).
    ///
    /// <para><b>Woher sie kommen.</b> <c>BetriebsartText</c>, <c>BerechnungsartText</c>
    /// und <c>AmortisationText</c> standen in
    /// <c>Form_SpeicherVariantenVergleich</c> (:529-562), <c>AmortisationText</c>
    /// zusätzlich in <c>Form_SpeicherOptimierung</c> (:1119) und — unter dem Namen
    /// <c>SpAmortisationstext</c> — in <c>Form_Simulation_Detail</c> (:7502). Sie
    /// übersetzen Persistenzwerte (<c>DbWerte.SP_*</c>, deutsch und eingefroren) bzw.
    /// einen Engine-Zustand in Anzeigetexte — genau die Schichtgrenze, für die es die
    /// Drei-Schichten-Regel gibt.</para>
    ///
    /// <para><b>Es war eine VIERFACHUNG, keine Dreifachung.</b> Die vierte Fassung stand
    /// in <c>Form_Simulation_Config.Karten.cs</c> und ist mit iU9‑W10b nach
    /// <c>SimulationKonfigHuelle</c> gewandert. Sie wich ab: Sie kannte die
    /// Preissteuerung, die drei anderen nicht. Beim Zusammenführen der beiden Wellen
    /// ist ihr Wissen hierher gezogen und die Hülle ruft diese Methoden — vier Kopien,
    /// eine Wahrheit (W11a‑O‑4).</para>
    ///
    /// <para><b>Zwei Ressourcenpaare für denselben Text.</b> Der Bestand führt
    /// <c>OPT_AMORT_NIE</c>/<c>OPT_AMORT_UEBER</c> (Optimierung, Variantenvergleich) UND
    /// <c>SP_ERG_NICHT_AMORTISIERBAR</c>/<c>SP_ERG_UEBER_NUTZUNGSDAUER</c>
    /// (Ergebnisseite). Beide Paare tragen in BEIDEN Sprachen denselben Wortlaut
    /// („nicht amortisierbar" / „&gt; Nutzungsdauer" bzw. „not amortisable" /
    /// „&gt; service life") — nachgeprüft am Katalog. Genommen ist hier das
    /// <c>SP_ERG_*</c>-Paar; das andere bleibt für seine übrigen Verwender stehen.</para>
    ///
    /// <para><b>Eine Formatabweichung.</b> Die Ergebnisseite formatierte die Jahre mit
    /// <c>"N1"</c>, Optimierung und Variantenvergleich mit <c>"0.0"</c>. Beide liefern
    /// dieselbe Zeichenkette, solange die Amortisationszeit unter 1 000 Jahren bleibt —
    /// erst darüber setzt <c>"N1"</c> ein Tausendertrennzeichen. Genommen ist
    /// <c>"N1"</c>. Ein Fall, in dem sich das auswirkt, ist fachlich ausgeschlossen (die
    /// Engine meldet über der Nutzungsdauer einen Zustand, keine Zahl).</para>
    /// </summary>
    public static class SpeicherAnzeigeCtrl
    {
        /// <summary>
        /// Betriebsart als Anzeigetext. Unbekannte Werte kommen unverändert zurück —
        /// besser der Persistenzwert als gar nichts.
        /// </summary>
        public static string BetriebsartText(string wert)
        {
            if (wert == DbWerte.SP_BETRIEBSART_GRAUSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM;
            if (wert == DbWerte.SP_BETRIEBSART_GRUENSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM;
            return wert ?? "";
        }

        /// <summary>
        /// Berechnungsart als Anzeigetext.
        ///
        /// <para><b>Die Lücke ist beim Zusammenführen mit W10b geschlossen worden.</b>
        /// Drei der vier Fassungen des Bestands (Variantenvergleich,
        /// Auslegungsoptimierung, Ergebnisseite) kannten nur zwei Berechnungsarten; die
        /// Preissteuerung erschien dort mit ihrem Persistenzwert
        /// „Arbitrage". Die VIERTE — <c>Form_Simulation_Config.BerechnungsartAnzeige</c>,
        /// mit iU9‑W10b nach <c>SimulationKonfigHuelle</c> gewandert — kannte sie. Diese
        /// Fassung ist die vollständigere und steht jetzt hier; alle vier Aufrufer
        /// bekommen damit denselben Text (W11a‑O‑4).</para>
        ///
        /// <para><b>Der entfallene Wert wird BENANNT umgesetzt</b>, nicht verschwiegen:
        /// Eine Variante mit der entfallenen Berechnungsart
        /// (<see cref="SpeicherAltstand"/>) zeigt den Text, der beides sagt — dass die
        /// Dauernutzung gilt und woher der Stand kommt. So steht an jeder Anzeigestelle
        /// dasselbe, ohne dass jede von ihnen die Regel kennen müßte.</para>
        ///
        /// <para>Ein unbekannter Wert kommt weiterhin unverändert zurück. Die vierte
        /// Fassung fiel dort auf „Dauernutzung" zurück — das ist eine Behauptung über
        /// Daten, die man nicht kennt; der Persistenzwert ist ehrlicher. Alle vier
        /// Schreiber setzen ohnehin nur <c>DbWerte.SP_BERECHNUNG_*</c>.</para>
        /// </summary>
        public static string BerechnungsartText(string wert)
        {
            if (SpeicherAltstand.IstEntfalleneBerechnungsart(wert))
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ALTSTAND;
            if (wert == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG;
            if (wert == DbWerte.SP_BERECHNUNG_ARBITRAGE)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE;
            return wert ?? "";
        }

        /// <summary>
        /// Amortisationszeit als Text: die Jahre, oder der Klartext des Sonderfalls
        /// (Fachkonzept 7.1 — die V7-Mappe schrieb beides in dieselbe Zelle, die Engine
        /// trennt Zustand und Zahl).
        ///
        /// <para><b>Kein negatives Null</b> (Anwenderwunsch 08.09.2026, W11b‑B‑14). Die
        /// dynamische Amortisation rechnet <c>-ln(1 - I·i/E)/ln(1+i)</c>; bei
        /// <c>I = 0</c> ist der Zähler <c>-ln(1) = -0</c>, und <c>(-0)/x</c> bleibt in
        /// IEEE 754 ein negatives Null. <c>"N1"</c> schreibt dafür „−0,0“ — ein
        /// Vorzeichen ohne Gegenstand. Der Betrag unter einem halben Zehntel wird
        /// deshalb VOR dem Formatieren auf glatt 0 gezogen; das ist dieselbe Zahl, die
        /// <c>"N1"</c> ohnehin anzeigt, nur ohne das irreführende Minus.</para>
        /// </summary>
        public static string AmortisationText(Amortisation a)
        {
            switch (a.Status)
            {
                case AmortisationStatus.NichtAmortisierbar:
                    return MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR;
                case AmortisationStatus.UeberNutzungsdauer:
                    return MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER;
                default:
                    double jahre = System.Math.Abs(a.Jahre) < 0.05 ? 0.0 : a.Jahre;
                    return jahre.ToString("N1", CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Dieselbe Angabe, aber mit der Investition als Prüfstein (Anwenderwunsch
        /// 08.09.2026, W11b‑B‑14).
        ///
        /// <para><b>Ohne Investition ist die Amortisation nicht bestimmbar.</b> Die Engine
        /// rechnet <c>T_stat = I / E_a,äq</c> und liefert bei <c>I = 0</c> folgerichtig
        /// 0 — auf dem Schirm „0,0 a“, was wie „amortisiert sich sofort“ aussieht. In
        /// der Sache heißt <c>I = 0</c> aber fast immer: Die Kosten sind nicht gepflegt.
        /// Eine Amortisationszeit zu behaupten, wo es nichts zurückzuverdienen gibt, ist
        /// keine Aussage über den Speicher, sondern eine über eine leere Eingabe;
        /// deshalb steht hier der Gedankenstrich, den die Seite schon für jede andere
        /// unbestimmte Kennzahl führt.</para>
        /// </summary>
        public static string AmortisationText(Amortisation a, double investitionEur)
        {
            if (investitionEur <= 0.0) return SpeicherKennzahlenBlock.UNBESTIMMT;
            return AmortisationText(a);
        }

        /// <summary>
        /// <b>Womit rechnet DIESES Projekt seinen Strom­speicher?</b> — der eine Satz, mit
        /// dem jede Ansicht eine Projektspalte beschriften kann (Auftrag VF-1,
        /// Anwenderbefund 17.09.2026).
        ///
        /// <para><b>Der Befund.</b> In der Vergleichsgruppe der Wirtschaftlichkeit stand
        /// stumm eine Spalte „mit Flotte" neben einer „ohne" — der Anwender hielt die
        /// Variante für „mit Speicher". Der Simulationsreiter sagt es seit jeher
        /// (Herkunftszeile), die Wirtschaftlichkeit sagte es nicht.</para>
        ///
        /// <para><b>Drei Fälle, eine Herleitung.</b> Führt das Projekt eine AKTIVIERTE
        /// Projektflotte, gilt sie — derselbe Text und dieselben Angaben wie im
        /// Simulationsreiter (<see cref="FlottenKontextText"/>). Sonst entscheidet die
        /// aktive Speichervariante mit ihrer Berechnungsart. Führt das Projekt beides
        /// nicht, steht das auch da; eine leere Spalte wäre keine Auskunft.</para>
        ///
        /// <para><b>Diese eine Methode liest, ihre Nachbarn übersetzen nur.</b> Sie
        /// beantwortet eine Frage über ein PROJEKT, und die steht in der Datenbank. Ein
        /// Lesefehler liefert den leeren Text — die Auskunft ist Ausweis, kein Ergebnis,
        /// und darf keine Ansicht zu Fall bringen.</para>
        /// </summary>
        /// <param name="idProjekt"><c>Tab_Projekt.ID</c>.</param>
        public static string SpeicherKontextText(int idProjekt)
        {
            if (idProjekt <= 0) return "";
            try
            {
                FlottenStudieKonfiguration flotte = SpeicherFlottenProjektCtrl.AktiveKonfiguration(idProjekt);
                if (flotte != null && flotte.Einheiten != null && flotte.Einheiten.Count > 0)
                    return FlottenKontextText(flotte, MyResource.Resource.SP_KONTEXT_FLOTTE);

                StromspeicherVarianteModel variante =
                    new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                if (variante != null)
                    return string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SP_KONTEXT_EINZEL,
                        BerechnungsartText(SpeicherAltstand.Berechnungsart(variante.Berechnungsart)));

                return MyResource.Resource.SP_KONTEXT_OHNE;
            }
            catch { return ""; }
        }

        /// <summary>
        /// Die Angaben EINER Flotte als Satz: Betriebsziel, Einheitenzahl und — wo es eines
        /// gibt — das Peak-Ziel samt seinem Modus.
        ///
        /// <para><b>Eine Herleitung, zwei Rahmen.</b> Der Simulationsreiter sagt
        /// „Gerechnet mit Speicherflotte: …" über den ANGEZEIGTEN LAUF, die
        /// Wirtschaftlichkeit „mit Speicherflotte: …" über eine SPALTE. Verschieden ist nur
        /// der Kopfsatz; was danach kommt, darf es nicht sein — deshalb kommt der Rahmen
        /// als Format herein und die Angaben entstehen hier.</para>
        /// </summary>
        /// <param name="flotte">Der Lesestand der Flotte.</param>
        /// <param name="kopfformat">
        /// Das Format des Kopfsatzes mit <c>{0}</c> = Betriebsziel und <c>{1}</c> =
        /// Einheitenzahl (<c>SIM_SP_HERKUNFT</c> bzw. <c>SP_KONTEXT_FLOTTE</c>).
        /// </param>
        public static string FlottenKontextText(FlottenStudieKonfiguration flotte, string kopfformat)
        {
            if (flotte == null) return "";
            FlottenSimulationOptionen o = flotte.Optionen ?? new FlottenSimulationOptionen();
            CultureInfo kultur = CultureInfo.CurrentCulture;

            string zeile = string.Format(kultur, kopfformat,
                SpeicherFlottenAnzeigeCtrl.Zieltext(o.Betriebsziel),
                flotte.Einheiten == null ? 0 : flotte.Einheiten.Count);

            if ((o.Betriebsziel == FlottenBetriebsziel.PeakShaving ||
                 o.Betriebsziel == FlottenBetriebsziel.MultiUse) &&
                o.WirtschaftlicherPeakZielwertKw.HasValue)
            {
                zeile += " · " + string.Format(kultur, MyResource.Resource.SIM_SP_HERKUNFT_PEAK,
                    o.WirtschaftlicherPeakZielwertKw.Value.ToString("N2", kultur),
                    o.PeakZielAdaptiv ? MyResource.Resource.FLOTTE_PEAKMODUS_ADAPTIV
                                      : MyResource.Resource.FLOTTE_PEAKMODUS_FEST);
            }
            return zeile;
        }
    }
}
