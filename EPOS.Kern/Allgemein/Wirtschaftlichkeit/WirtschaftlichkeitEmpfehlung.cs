using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026) — die VALERI-Lücken, die OHNE
    // neues Datenmodell zu schließen sind: G9 (Entscheidungsempfehlung als Text),
    // G7 (Betrachtungszeitraum gegen die Nutzungsdauern) und die offengelegten
    // Vereinfachungen G1/G3/G5 samt der Herleitung der Eigennutzung (G10).
    //
    // ALLES IN DIESER DATEI IST AUSGABE. Kein Kapitalwert, keine Reihe und keine
    // Kennzahl ändert sich dadurch — die Klassen lesen fertige Ergebnisse und
    // formen daraus Sätze. Genau das verlangt DIN EN 17463 zusätzlich zur
    // Rechnung: eine begründete Empfehlung und offengelegte Annahmen.
    //
    // WARUM IM KERN. Seite, Word-Bericht und Excel-Blatt sollen denselben Satz
    // zeigen. Drei Formulierungen derselben Aussage sind drei Gelegenheiten,
    // auseinanderzulaufen (Lehre aus Etappe E7, Divergenzen D1…D5).
    // ---------------------------------------------------------------------------

    /// <summary>Die Einstufung EINER Variante gegenüber dem Stammprojekt (G9).</summary>
    public enum EmpfehlungStufe
    {
        /// <summary>Kapitalwertdifferenz in Worst, Erwartet und Best positiv.</summary>
        Empfohlen,

        /// <summary>In Erwartet positiv, im ungünstigen Fall aber nicht.</summary>
        Bedingt,

        /// <summary>In Erwartet nicht positiv — die Maßnahme trägt sich nicht.</summary>
        Nicht
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (G9): das Urteil über EINE Variante — die drei
    /// Kapitalwertdifferenzen und die Stufe, die sich daraus ergibt.
    /// </summary>
    public sealed class VariantenEmpfehlung
    {
        /// <summary><c>Tab_Projekt.ID</c> der Variante.</summary>
        public int IdProjekt;

        /// <summary>Anzeigename der Variante (aus dem Ergebnis).</summary>
        public string Anzeige = "";

        /// <summary>Die Einstufung nach der Regel in <see cref="WirtschaftlichkeitEmpfehlung"/>.</summary>
        public EmpfehlungStufe Stufe;

        /// <summary>Kapitalwertdifferenz zum Stamm im Szenario Worst [€]; null = nicht gerechnet.</summary>
        public double? DiffWorst;

        /// <summary>Kapitalwertdifferenz zum Stamm im Szenario Erwartet [€].</summary>
        public double? DiffErwartet;

        /// <summary>Kapitalwertdifferenz zum Stamm im Szenario Best [€]; null = nicht gerechnet.</summary>
        public double? DiffBest;

        /// <summary>
        /// true, wenn Best oder Worst fehlt. Dann urteilt die Regel allein nach
        /// Erwartet — und sagt das dazu, statt eine Bandbreite zu behaupten, die
        /// niemand gerechnet hat.
        /// </summary>
        public bool BandbreiteFehlt;

        /// <summary>Die Stufe als Text („empfohlen" / „bedingt empfohlen" /
        /// „nicht empfohlen"), bei fehlender Bandbreite mit dem Zusatz.</summary>
        public string StufeText
        {
            get
            {
                string s = Stufe == EmpfehlungStufe.Empfohlen
                         ? MyResource.Resource.WIRT_EMPF_STUFE_JA
                         : Stufe == EmpfehlungStufe.Bedingt
                           ? MyResource.Resource.WIRT_EMPF_STUFE_BEDINGT
                           : MyResource.Resource.WIRT_EMPF_STUFE_NEIN;
                return BandbreiteFehlt
                     ? s + " (" + MyResource.Resource.WIRT_EMPF_OHNE_BANDBREITE + ")"
                     : s;
            }
        }
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G9): die
    /// <b>Entscheidungsempfehlung</b> — „Vorschlag zur Entscheidung: …".
    ///
    /// <para><b>Was DIN EN 17463 verlangt.</b> Der Bewertungsbericht endet nicht mit
    /// einer Zahl, sondern mit einer Empfehlung: Welche Maßnahme ist gegenüber dem
    /// Weiterbetrieb (Referenzfall) vorteilhaft, und wie belastbar ist das über die
    /// Bandbreite? EPOS-Plan hatte die Zahlen längst — den Satz gab es nicht.</para>
    ///
    /// <para><b>Die Regel (Anwenderentscheid).</b> Maßstab ist die
    /// Kapitalwertdifferenz zum Stammprojekt (<see cref="WirtschaftlichkeitErgebnis.KapitalwertDiff"/>),
    /// nicht der absolute Kapitalwert: Der Stamm IST die Unterlassensalternative
    /// (Entscheidung 11.08.2026).</para>
    /// <list type="bullet">
    ///   <item><description><b>empfohlen</b> — Differenz in Worst, Erwartet und Best positiv.</description></item>
    ///   <item><description><b>bedingt empfohlen</b> — in Erwartet positiv, im ungünstigen Fall nicht.</description></item>
    ///   <item><description><b>nicht empfohlen</b> — in Erwartet nicht positiv.</description></item>
    ///   <item><description>Fehlen Best/Worst, urteilt die Regel nach Erwartet und
    ///   nennt „Bandbreite nicht berechnet".</description></item>
    /// </list>
    ///
    /// <para><b>Der Gesamtvorschlag</b> ist die Variante mit der höchsten
    /// Erwartet-Differenz unter den empfohlenen, sonst unter den bedingt
    /// empfohlenen. Gibt es keine, lautet der Satz: keine Variante ist
    /// wirtschaftlich, es bleibt beim Weiterbetrieb.</para>
    ///
    /// <para><b>Ohne Grundlage kein Satz.</b> Ohne Variante mit Erwartet-Ergebnis
    /// (kein Stamm in der Gruppe, nichts gerechnet) bleibt der Text LEER, und
    /// Seite wie Bericht zeichnen die Zeile gar nicht erst. Ein Vorschlag ohne
    /// Zahlen wäre eine Behauptung.</para>
    /// </summary>
    public static class WirtschaftlichkeitEmpfehlung
    {
        /// <summary>Vorzeichenbehaftetes Geldformat der Empfehlung („+12.300 €").</summary>
        private const string GELD = "+#,##0;−#,##0;0";

        /// <summary>Ein Urteil je Variante, in der Reihenfolge des Auftretens.
        /// Varianten ohne Erwartet-Differenz bleiben außen vor — über sie ist
        /// nichts zu sagen.</summary>
        public static List<VariantenEmpfehlung> Einstufungen(
            IEnumerable<WirtschaftlichkeitErgebnis> alle)
        {
            var liste = new List<VariantenEmpfehlung>();
            if (alle == null) return liste;

            var nachId = new Dictionary<int, VariantenEmpfehlung>();
            foreach (WirtschaftlichkeitErgebnis e in alle)
            {
                if (e == null || e.IstStamm) continue;
                VariantenEmpfehlung v;
                if (!nachId.TryGetValue(e.IdProjekt, out v))
                {
                    v = new VariantenEmpfehlung { IdProjekt = e.IdProjekt, Anzeige = e.Anzeige ?? "" };
                    nachId[e.IdProjekt] = v;
                    liste.Add(v);
                }
                if (string.IsNullOrEmpty(v.Anzeige) && !string.IsNullOrEmpty(e.Anzeige))
                    v.Anzeige = e.Anzeige;

                if (string.Equals(e.Szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal))
                    v.DiffWorst = e.KapitalwertDiff;
                else if (string.Equals(e.Szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal))
                    v.DiffBest = e.KapitalwertDiff;
                else if (string.Equals(e.Szenario, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal))
                    v.DiffErwartet = e.KapitalwertDiff;
            }

            var fertig = new List<VariantenEmpfehlung>();
            foreach (VariantenEmpfehlung v in liste)
            {
                if (!v.DiffErwartet.HasValue) continue;   // kein Stamm, nichts gerechnet
                Stufen(v);
                fertig.Add(v);
            }
            return fertig;
        }

        /// <summary>Die Regel selbst — als eigene Methode, damit die Prüffälle sie
        /// einzeln greifen können.</summary>
        private static void Stufen(VariantenEmpfehlung v)
        {
            bool bandbreite = v.DiffWorst.HasValue && v.DiffBest.HasValue;
            v.BandbreiteFehlt = !bandbreite;

            if (v.DiffErwartet.Value <= 0) { v.Stufe = EmpfehlungStufe.Nicht; return; }
            if (!bandbreite) { v.Stufe = EmpfehlungStufe.Empfohlen; return; }

            // Erwartet ist positiv. „Empfohlen" verlangt, dass auch der ungünstige und
            // der günstige Fall tragen; ein negativer Worst-Fall ist genau die
            // Einschränkung, die VALERI ausgewiesen sehen will.
            v.Stufe = v.DiffWorst.Value > 0 && v.DiffBest.Value > 0
                    ? EmpfehlungStufe.Empfohlen
                    : EmpfehlungStufe.Bedingt;
        }

        /// <summary>Die vorgeschlagene Variante; <c>null</c> = keine ist wirtschaftlich
        /// (oder es gibt keine mit Erwartet-Ergebnis).</summary>
        public static VariantenEmpfehlung Vorschlag(IEnumerable<WirtschaftlichkeitErgebnis> alle)
        {
            return Vorschlag(Einstufungen(alle));
        }

        /// <summary>Dieselbe Wahl auf bereits gebildeten Urteilen — für Aufrufer, die
        /// die Liste ohnehin schon halten (Tabelle und Satz im selben Baustein).</summary>
        public static VariantenEmpfehlung Vorschlag(List<VariantenEmpfehlung> urteile)
        {
            if (urteile == null) return null;
            VariantenEmpfehlung beste = Beste(urteile, EmpfehlungStufe.Empfohlen);
            return beste ?? Beste(urteile, EmpfehlungStufe.Bedingt);
        }

        private static VariantenEmpfehlung Beste(List<VariantenEmpfehlung> urteile, EmpfehlungStufe stufe)
        {
            VariantenEmpfehlung treffer = null;
            foreach (VariantenEmpfehlung v in urteile)
            {
                if (v.Stufe != stufe || !v.DiffErwartet.HasValue) continue;
                if (treffer == null || v.DiffErwartet.Value > treffer.DiffErwartet.Value) treffer = v;
            }
            return treffer;
        }

        /// <summary>
        /// Der fertige Satz für Seite, Word und Excel. <c>""</c> = keine Grundlage
        /// (keine Variante mit Erwartet-Ergebnis) — dann wird gar keine Zeile
        /// gezeichnet.
        /// </summary>
        public static string Vorschlagstext(IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                            CultureInfo kultur)
        {
            return Vorschlagstext(Einstufungen(alle), kultur);
        }

        /// <inheritdoc cref="Vorschlagstext(IEnumerable{WirtschaftlichkeitErgebnis},CultureInfo)"/>
        public static string Vorschlagstext(List<VariantenEmpfehlung> urteile, CultureInfo kultur)
        {
            if (urteile == null || urteile.Count == 0) return "";
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            VariantenEmpfehlung v = Vorschlag(urteile);
            if (v == null) return MyResource.Resource.WIRT_EMPF_KEINE;

            string zusatz;
            if (v.BandbreiteFehlt)
                zusatz = "; " + MyResource.Resource.WIRT_EMPF_OHNE_BANDBREITE;
            else
                zusatz = string.Format(kultur,
                    v.Stufe == EmpfehlungStufe.Empfohlen
                        ? MyResource.Resource.WIRT_EMPF_ALLE_POSITIV
                        : MyResource.Resource.WIRT_EMPF_NUR_ERWARTET,
                    Geld(v.DiffWorst, kultur), Geld(v.DiffBest, kultur));

            return string.Format(kultur, MyResource.Resource.WIRT_EMPF_SATZ,
                                 v.Anzeige, Geld(v.DiffErwartet, kultur), zusatz);
        }

        /// <summary>Betrag mit Vorzeichen und Einheit; „—" wenn nicht gerechnet.</summary>
        public static string Geld(double? wert, CultureInfo kultur)
        {
            if (!wert.HasValue) return "—";
            return wert.Value.ToString(GELD, kultur ?? CultureInfo.CurrentCulture) + " €";
        }
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G7): der
    /// <b>Betrachtungszeitraum gegen die Nutzungsdauern</b>.
    ///
    /// <para>DIN EN 17463 verlangt eine Begründung des Zeitraums. T ist in EPOS-Plan
    /// frei wählbar (1…50 a) und wurde nie gegen die Nutzungsdauern der
    /// Investitionspositionen gestellt — dabei entscheidet genau dieses Verhältnis
    /// darüber, ob am Ende ein Restwert steht (T kürzer als die längste Dauer) und ob
    /// zwischendurch ersetzt wird (T länger als die kürzeste).</para>
    ///
    /// <para><b>Kein Blocker, reiner Ausweis.</b> Die Zeile sagt, was der Rechenkern
    /// ohnehin tut; sie verhindert nichts und schlägt nichts vor. Der Rechenweg des
    /// <see cref="KapitalwertRechner"/> ist unberührt.</para>
    /// </summary>
    public static class NutzungsdauerAbgleich
    {
        /// <summary>
        /// Die Hinweiszeile zu T und den Nutzungsdauern der Positionen.
        /// <c>""</c> = kein Betrachtungszeitraum (T ≤ 0) und damit nichts zu sagen.
        /// </summary>
        /// <param name="betrachtungszeitraum">T [a] aus den Projektparametern.</param>
        /// <param name="positionen">Die Investitionspositionen des Erwartet-Laufs.
        /// Eine Nutzungsdauer &lt; 1 heißt im Rechenkern „wie T" und zählt hier
        /// deshalb nicht als gepflegte Dauer.</param>
        public static string Hinweis(int betrachtungszeitraum,
                                     IEnumerable<KapitalwertRechner.InvestPosition> positionen,
                                     CultureInfo kultur)
        {
            if (betrachtungszeitraum <= 0) return "";
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            int t = betrachtungszeitraum;

            double laengste = 0, kuerzeste = 0;
            int ersatzJahr = 0;
            bool gepflegt = false;

            if (positionen != null)
                foreach (KapitalwertRechner.InvestPosition p in positionen)
                {
                    if (p == null || p.Betrag == 0 || p.Nutzungsdauer < 1.0) continue;
                    double n = p.Nutzungsdauer;
                    if (!gepflegt || n > laengste) laengste = n;
                    if (!gepflegt || n < kuerzeste) kuerzeste = n;
                    gepflegt = true;

                    // Dieselbe Rundung wie im Rechenkern: tj = round(start + n), und
                    // ersetzt wird nur INNERHALB des Zeitraums (tj < T).
                    int start = p.StartJahr > 1 ? p.StartJahr : 0;
                    int tj = (int)Math.Round(start + n, MidpointRounding.AwayFromZero);
                    if (tj >= 1 && tj < t && (ersatzJahr == 0 || tj < ersatzJahr)) ersatzJahr = tj;
                }

            if (!gepflegt)
                return string.Format(kultur, MyResource.Resource.WIRT_T_OHNE_DAUER, t);

            string zeile = string.Format(kultur, MyResource.Resource.WIRT_T_KOPF, t,
                                         kuerzeste.ToString("0.#", kultur),
                                         laengste.ToString("0.#", kultur));

            var teile = new List<string>();
            if (t < laengste) teile.Add(MyResource.Resource.WIRT_T_RESTWERT);
            if (ersatzJahr > 0)
                teile.Add(string.Format(kultur, MyResource.Resource.WIRT_T_ERSATZ, ersatzJahr));
            if (teile.Count == 0) teile.Add(MyResource.Resource.WIRT_T_GLEICH);

            return zeile + " · " + string.Join(" ", teile.ToArray());
        }
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026): die zwei AUSWEIS-Sätze, die
    /// der VALERI-Abgleich zusätzlich verlangt — je einmal formuliert, damit Seite
    /// und Bericht dasselbe sagen.
    /// </summary>
    public static class ValeriAusweis
    {
        /// <summary>
        /// <b>G1/G3/G5 offengelegt</b> (Anwenderentscheid: nicht umsetzen, aber
        /// benennen). Kein Endjahr je Kostenposition, keine Degradation außer beim
        /// PV-Ertrag, Energiekosten als Gesamtrechnung des Simulationslaufs ab Jahr 1
        /// (dokumentierte Vereinfachung FK10). Eine Vereinfachung, die im Bericht
        /// steht, ist eine Annahme; eine, die nicht dasteht, ist ein Fehler.
        /// </summary>
        public static string Vereinfachungen()
        {
            return MyResource.Resource.WIRT_VEREINFACHUNGEN;
        }

        /// <summary>
        /// <b>G10</b>: Eigenverbrauchsquote und Einspeiseanteil sind in EPOS-Plan
        /// aus der Stundensimulation ABGELEITET und nicht als Annahme gesetzt —
        /// fachlich besser als die VALERI-Vorlage, die sie erfragt. Genau deshalb
        /// muss die Herleitung im Bericht stehen: Sonst sieht der Leser eine Zahl,
        /// deren Herkunft er für eine Schätzung halten muss.
        /// <para>Nur auszugeben, wenn die Gruppe überhaupt Photovoltaik führt.</para>
        /// </summary>
        public static string EigennutzungHerleitung()
        {
            return MyResource.Resource.WIRT_PV_HERLEITUNG;
        }
    }
}
