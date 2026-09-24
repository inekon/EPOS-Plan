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
        /// <summary>Kapitalwertdifferenz in allen drei Szenarien positiv — auch der
        /// schlechteste der drei Werte (ETAPPE E5, Q4).</summary>
        Empfohlen,

        /// <summary>In Erwartet positiv, im schlechtesten der drei Szenarien aber nicht.</summary>
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

        /// <summary>
        /// Das Urteil gilt dem STAMMPROJEKT — das gibt es nur, wenn eine Variante die
        /// Referenz ist (ETAPPE E5, Q7). Schneidet der Stamm dann am besten ab, lautet der
        /// Vorschlag „Stammprojekt beibehalten" (<c>WIRT_EMPF_SATZ_STAMM</c>, Anwenderentscheid
        /// 22.09.2026 zu Frage (2) aus E5b) — nicht „Variante „Stamm"".
        /// </summary>
        public bool IstStamm;

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

        /// <summary>
        /// ETAPPE E5 (Empfehlung Q4, 22.09.2026): der <b>schlechteste</b> der drei
        /// Szenariowerte — die Einstufung urteilt über ihn und nicht über das Etikett
        /// „Worst". Die Etiketten beschreiben die Parametersätze, nicht ihre Wirkung auf
        /// die Differenz. <c>null</c>, wenn Worst oder Best fehlt.
        /// </summary>
        public double? Schlechtester
        {
            get
            {
                if (!DiffWorst.HasValue || !DiffBest.HasValue) return null;
                double w = Math.Min(DiffWorst.Value, DiffBest.Value);
                return DiffErwartet.HasValue ? Math.Min(w, DiffErwartet.Value) : w;
            }
        }

        /// <summary>
        /// ETAPPE E5 (Q4): der <b>beste</b> der drei Szenariowerte; <c>null</c>, wenn Worst
        /// oder Best fehlt.
        /// </summary>
        public double? Bester
        {
            get
            {
                if (!DiffWorst.HasValue || !DiffBest.HasValue) return null;
                double b = Math.Max(DiffWorst.Value, DiffBest.Value);
                return DiffErwartet.HasValue ? Math.Max(b, DiffErwartet.Value) : b;
            }
        }

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
    ///   <item><description><b>empfohlen</b> — Differenz in allen drei Szenarien positiv.</description></item>
    ///   <item><description><b>bedingt empfohlen</b> — in Erwartet positiv, im schlechtesten
    ///   der drei Szenarien nicht.</description></item>
    ///   <item><description><b>nicht empfohlen</b> — in Erwartet nicht positiv.</description></item>
    ///   <item><description>Fehlen Best/Worst, urteilt die Regel nach Erwartet und
    ///   nennt „Bandbreite nicht berechnet".</description></item>
    /// </list>
    ///
    /// <para><b>ETAPPE E5 (Empfehlung Q4, 22.09.2026): über Werte, nicht über
    /// Etiketten.</b> Geurteilt wird über den schlechtesten und den besten der drei
    /// Szenariowerte (<see cref="VariantenEmpfehlung.Schlechtester"/>,
    /// <see cref="VariantenEmpfehlung.Bester"/>) — ein Satz „ungünstig" kann eine
    /// Differenz auch heben. Der Satz nennt deshalb diese beiden Werte. Die Stufe selbst
    /// ändert sich dadurch nicht: „alle drei positiv" heißt dasselbe wie „der
    /// schlechteste positiv".</para>
    ///
    /// <para><b>ETAPPE E5 (Empfehlung Q7): Jede Version außer der Referenz</b> bekommt ein
    /// Urteil — ist eine Variante die Referenz, auch der Stamm
    /// (<see cref="Einstufungen(IEnumerable{WirtschaftlichkeitErgebnis}, int)"/>).</para>
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
        /// nichts zu sagen. Der Stamm bekommt hier kein Urteil (Bestand, Referenz =
        /// Stamm); wer die Referenz kennt, nimmt die Überladung mit <c>idReferenz</c>.</summary>
        public static List<VariantenEmpfehlung> Einstufungen(
            IEnumerable<WirtschaftlichkeitErgebnis> alle)
        {
            return Einstufungen(alle, 0);
        }

        /// <summary>
        /// ETAPPE E5 (Empfehlung Q7, 22.09.2026) — ein Urteil je Stand <b>außer der
        /// Referenz</b>: Ist eine Variante die Referenz (§ 2.9), bekommt auch der Stamm
        /// eines — er trägt dann eine Differenz gegen sie, und über die ist zu urteilen
        /// wie über jede andere. Ohne Referenz (0) bleibt es beim Bestand: kein Urteil für
        /// den Stamm.
        /// </summary>
        /// <param name="idReferenz"><c>Tab_Projekt.ID</c> der wirksamen Referenz; 0 = Stamm.</param>
        public static List<VariantenEmpfehlung> Einstufungen(
            IEnumerable<WirtschaftlichkeitErgebnis> alle, int idReferenz)
        {
            var liste = new List<VariantenEmpfehlung>();
            if (alle == null) return liste;

            var nachId = new Dictionary<int, VariantenEmpfehlung>();
            foreach (WirtschaftlichkeitErgebnis e in alle)
            {
                if (e == null) continue;
                if (idReferenz > 0 ? e.IdProjekt == idReferenz : e.IstStamm) continue;
                VariantenEmpfehlung v;
                if (!nachId.TryGetValue(e.IdProjekt, out v))
                {
                    v = new VariantenEmpfehlung { IdProjekt = e.IdProjekt, Anzeige = e.Anzeige ?? "" };
                    nachId[e.IdProjekt] = v;
                    liste.Add(v);
                }
                if (string.IsNullOrEmpty(v.Anzeige) && !string.IsNullOrEmpty(e.Anzeige))
                    v.Anzeige = e.Anzeige;
                if (e.IstStamm) v.IstStamm = true;

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

            // Erwartet ist positiv. „Empfohlen" verlangt, dass alle drei Szenarien tragen;
            // ein nicht positiver schlechtester Fall ist genau die Einschränkung, die
            // VALERI ausgewiesen sehen will. ETAPPE E5 (Q4): geurteilt wird über den
            // schlechtesten WERT der drei, nicht über das Etikett „Worst".
            v.Stufe = v.Schlechtester.Value > 0
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
            return Vorschlagstext(Einstufungen(alle), kultur, null);
        }

        /// <inheritdoc cref="Vorschlagstext(IEnumerable{WirtschaftlichkeitErgebnis},CultureInfo)"/>
        public static string Vorschlagstext(IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                            CultureInfo kultur, string referenz)
        {
            return Vorschlagstext(Einstufungen(alle), kultur, referenz);
        }

        /// <inheritdoc cref="Vorschlagstext(IEnumerable{WirtschaftlichkeitErgebnis},CultureInfo)"/>
        public static string Vorschlagstext(List<VariantenEmpfehlung> urteile, CultureInfo kultur)
        {
            return Vorschlagstext(urteile, kultur, null);
        }

        /// <summary>
        /// ETAPPE E2 (VALERI-Lücke G9, Befund A5) — derselbe Satz, aber mit der
        /// <b>gewählten Referenz beim Namen</b>.
        ///
        /// <para>Maßstab des Vorschlags ist <c>KapitalwertDiff</c>, und diese Größe
        /// rechnet seit § 2.9 gegen die gewählte Referenz, nicht mehr fest gegen das
        /// Stammprojekt. Der Satz „Keine Variante ist gegenüber dem Stammprojekt
        /// wirtschaftlich" war damit bei gewählter Variantenreferenz falsch. Genannt
        /// wird die Referenz so, wie <c>Referenzwahl.Deklarationszeile</c> sie im
        /// Paarvergleich schon nennt.</para>
        /// </summary>
        /// <param name="referenz">Anzeigename der Referenz; leer oder <c>null</c> =
        /// der sprachliche Rückfall „dem Referenzfall" für Aufrufer, die die Gruppe
        /// nicht kennen.</param>
        public static string Vorschlagstext(List<VariantenEmpfehlung> urteile, CultureInfo kultur,
                                            string referenz)
        {
            if (urteile == null || urteile.Count == 0) return "";
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            string refName = string.IsNullOrEmpty(referenz)
                           ? MyResource.Resource.WIRT_EMPF_REFERENZ_UNBENANNT : referenz;

            VariantenEmpfehlung v = Vorschlag(urteile);
            if (v == null)
                return string.Format(kultur, MyResource.Resource.WIRT_EMPF_KEINE, refName);

            // ETAPPE E5 (Q4): Der Zusatz nennt den schlechtesten und den besten der drei
            // Werte — die Grundlage der Stufe —, nicht die Werte unter den Etiketten.
            string zusatz;
            if (v.BandbreiteFehlt)
                zusatz = "; " + MyResource.Resource.WIRT_EMPF_OHNE_BANDBREITE;
            else
                zusatz = string.Format(kultur,
                    v.Stufe == EmpfehlungStufe.Empfohlen
                        ? MyResource.Resource.WIRT_EMPF_ALLE_POSITIV
                        : MyResource.Resource.WIRT_EMPF_NUR_ERWARTET,
                    Geld(v.Schlechtester, kultur), Geld(v.Bester, kultur));

            // Anwenderentscheid 22.09.2026 (Frage (2) aus E5b): Schneidet bei einer Variante
            // als Referenz das STAMMPROJEKT am besten ab, hat es einen eigenen Satz — es ist
            // keine Variante, sondern der Stand, der bleibt. Dieselben Platzhalter wie
            // WIRT_EMPF_SATZ; der Name ({0}) steht dort nicht, der Satz sagt „Stammprojekt".
            string muster = v.IstStamm
                          ? MyResource.Resource.WIRT_EMPF_SATZ_STAMM
                          : MyResource.Resource.WIRT_EMPF_SATZ;
            return string.Format(kultur, muster,
                                 v.Anzeige, Geld(v.DiffErwartet, kultur), zusatz, refName);
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

        // =====================================================================
        // ETAPPE E5 — V‑A (Konzept § 2.11.4) und der Szenario-Hinweis (§ 2.11.7)
        // =====================================================================

        /// <summary>
        /// ETAPPE E5 (V‑A, Konzept § 2.11.3 Block 5; Mockup Kategorie 8, „Bewertung nach
        /// DIN EN 17463"): die <b>Deklarationszeilen</b> der Bewertung — in dieser
        /// Reihenfolge nominal · Steuern · Restwert · Risiko.
        ///
        /// <para><b>Nicht zu verwechseln</b> mit <c>Referenzwahl.Deklarationszeile</c>: Die
        /// benennt die Referenz eines Paarvergleichs, diese Liste die Normdeklarationen
        /// (Befund A2, Verwechslungsfalle).</para>
        ///
        /// <para>Die Aussagen beschreiben den Rechenweg, wie er ist: Er rechnet nominal
        /// (6.3.2), bucht Energie- und Stromsteuerentlastungen und keine Ertragsteuern
        /// (7.1.2), führt keine Abschreibung als Zahlung und setzt den Restwert linear an
        /// (dokumentierte Abweichung von 6.4), schlägt kein Risiko auf (6.5 optional)
        /// und benennt nicht monetäre Wirkungen, statt sie zu bewerten. Seite und
        /// Bericht lesen dieselbe Liste; eine Deklaration, die anders lautet als der
        /// Rechenweg, wäre ein Fehler im Bericht.</para>
        ///
        /// <para><b>ETAPPE E5 (Empfehlung Q5, 22.09.2026):</b> „nicht monetäre Wirkungen
        /// benannt" steht nur, wenn ein Text gepflegt ist. Ohne Text lautet die
        /// Risikozeile „… nicht monetäre Wirkungen: keine benannt"
        /// (<c>WIRT_DEKL_RISIKO_OHNE_NM</c>) — sonst deklarierte der Bericht etwas, das
        /// niemand getan hat.</para>
        /// </summary>
        /// <param name="nichtMonetaer">Der gepflegte Text der nicht monetären Wirkungen
        /// (<see cref="WirtschaftlichkeitParameter.NichtMonetaer"/>); leer oder <c>null</c> =
        /// keine benannt.</param>
        public static IReadOnlyList<ValeriDeklaration> Deklarationen(string nichtMonetaer)
        {
            bool benannt = !string.IsNullOrWhiteSpace(nichtMonetaer);
            return new List<ValeriDeklaration>
            {
                new ValeriDeklaration { Schluessel = ValeriDeklaration.NOMINAL,
                                        Text = MyResource.Resource.WIRT_DEKL_NOMINAL },
                new ValeriDeklaration { Schluessel = ValeriDeklaration.STEUERN,
                                        Text = MyResource.Resource.WIRT_DEKL_STEUERN },
                new ValeriDeklaration { Schluessel = ValeriDeklaration.RESTWERT,
                                        Text = MyResource.Resource.WIRT_DEKL_RESTWERT },
                new ValeriDeklaration { Schluessel = ValeriDeklaration.RISIKO,
                                        Text = benannt ? MyResource.Resource.WIRT_DEKL_RISIKO
                                                       : MyResource.Resource.WIRT_DEKL_RISIKO_OHNE_NM }
            };
        }

        /// <summary>
        /// ETAPPE E5 (U2, Mockup Kategorie 8 „Was ist angenommen?") — die
        /// <b>Annahmentafel</b>: je Größe der WIRKSAME Wert in Ungünstig, Erwartet und
        /// Günstig und seine Herkunft („Vorgabe", solange niemand das Feld des Satzes
        /// gepflegt hat, sonst „gepflegt"). Dieselben Zahlen wie
        /// <see cref="SzenarioSatz.Nachweis"/> — nur als Tafel, eine Zeile je Größe.
        ///
        /// <para>Reine Ausgabe; die Zeilentitel sind die des Parameterdialogs
        /// (<c>WPAR_SZ_*</c>), damit Dialog und Seite dieselbe Größe gleich nennen.</para>
        /// </summary>
        /// <param name="p">Der Parametersatz der Gruppe; <c>null</c> = keine Tafel.</param>
        /// <param name="kultur">Zahlenformat; <c>null</c> = aktuelle Kultur.</param>
        public static List<AnnahmeZeile> Annahmen(WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            var liste = new List<AnnahmeZeile>();
            if (p == null) return liste;
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            SzenarioSatz w = p.SatzFuer(WirtschaftlichkeitSzenario.WORST)
                             ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            SzenarioSatz b = p.SatzFuer(WirtschaftlichkeitSzenario.BEST)
                             ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);

            const string SATZ = "0.0;−0.0;0.0";          // Zins [%] ohne Vorzeichen
            const string RATE = "+0.0;−0.0;0.0";         // Preissteigerung [%/a]
            const string AENDERUNG = "+0.#;−0.#;0";      // Investition, Erträge [%], Dauer [a]

            liste.Add(Annahme(AnnahmeZeile.ZINS, MyResource.Resource.WPAR_SZ_ZINS,
                w.ZinsWirksam(p.Zinssatz).ToString(SATZ, kultur) + " %",
                p.Zinssatz.ToString(SATZ, kultur) + " %",
                b.ZinsWirksam(p.Zinssatz).ToString(SATZ, kultur) + " %",
                w.Zinssatz.HasValue || b.Zinssatz.HasValue));
            liste.Add(Annahme(AnnahmeZeile.PREIS_E, MyResource.Resource.WPAR_SZ_PREIS_E,
                w.PreisEnergieWirksam(p.PreissteigerungEnergie).ToString(RATE, kultur) + " %/a",
                p.PreissteigerungEnergie.ToString(RATE, kultur) + " %/a",
                b.PreisEnergieWirksam(p.PreissteigerungEnergie).ToString(RATE, kultur) + " %/a",
                w.PreissteigerungEnergie.HasValue || b.PreissteigerungEnergie.HasValue));
            liste.Add(Annahme(AnnahmeZeile.PREIS_B, MyResource.Resource.WPAR_SZ_PREIS_B,
                w.PreisBetriebWirksam(p.PreissteigerungBetrieb).ToString(RATE, kultur) + " %/a",
                p.PreissteigerungBetrieb.ToString(RATE, kultur) + " %/a",
                b.PreisBetriebWirksam(p.PreissteigerungBetrieb).ToString(RATE, kultur) + " %/a",
                w.PreissteigerungBetrieb.HasValue || b.PreissteigerungBetrieb.HasValue));
            liste.Add(Annahme(AnnahmeZeile.PREIS_I, MyResource.Resource.WPAR_SZ_PREIS_I,
                w.PreisInvestWirksam(p.PreisInvestWirksam).ToString(RATE, kultur) + " %/a",
                p.PreisInvestWirksam.ToString(RATE, kultur) + " %/a",
                b.PreisInvestWirksam(p.PreisInvestWirksam).ToString(RATE, kultur) + " %/a",
                w.PreissteigerungInvestition.HasValue || b.PreissteigerungInvestition.HasValue));
            liste.Add(Annahme(AnnahmeZeile.INVEST, MyResource.Resource.WPAR_SZ_INVEST,
                w.InvestWirksam.ToString(AENDERUNG, kultur) + " %",
                0.0.ToString(AENDERUNG, kultur) + " %",
                b.InvestWirksam.ToString(AENDERUNG, kultur) + " %",
                w.InvestitionAenderung.HasValue || b.InvestitionAenderung.HasValue));
            liste.Add(Annahme(AnnahmeZeile.ERTRAG, MyResource.Resource.WPAR_SZ_ERTRAG,
                w.ErtragWirksam.ToString(AENDERUNG, kultur) + " %",
                0.0.ToString(AENDERUNG, kultur) + " %",
                b.ErtragWirksam.ToString(AENDERUNG, kultur) + " %",
                w.ErtragAenderung.HasValue || b.ErtragAenderung.HasValue));
            liste.Add(Annahme(AnnahmeZeile.DAUER, MyResource.Resource.WPAR_SZ_DAUER,
                w.DauerWirksam.ToString(AENDERUNG, kultur) + " a",
                0.0.ToString(AENDERUNG, kultur) + " a",
                b.DauerWirksam.ToString(AENDERUNG, kultur) + " a",
                w.NutzungsdauerAenderung.HasValue || b.NutzungsdauerAenderung.HasValue));

            // Der Betrachtungszeitraum gehört in die Tafel, weil er die Frage beantwortet, die
            // ein Leser als nächste stellt. ETAPPE E9a (Schritt B): je Szenario der WIRKSAME —
            // ohne Pflege in allen drei Szenarien der Projektwert (Herkunft wie bisher),
            // mit gepflegtem Zeitraum eines Szenarios „gepflegt".
            int tE = p.Betrachtungszeitraum;
            bool zeitraumGepflegt = w.ZeitraumGepflegt(tE) || b.ZeitraumGepflegt(tE);
            liste.Add(new AnnahmeZeile
            {
                Schluessel = AnnahmeZeile.ZEITRAUM,
                Groesse = MyResource.Resource.WIRT_ANN_ZEITRAUM,
                Unguenstig = w.ZeitraumWirksam(tE).ToString(CultureInfo.InvariantCulture) + " a",
                Erwartet = tE.ToString(CultureInfo.InvariantCulture) + " a",
                Guenstig = b.ZeitraumWirksam(tE).ToString(CultureInfo.InvariantCulture) + " a",
                Herkunft = zeitraumGepflegt ? MyResource.Resource.WIRT_ANN_GEPFLEGT
                                            : MyResource.Resource.WIRT_ANN_PROJEKTWERT,
                Gepflegt = zeitraumGepflegt
            });

            // ETAPPE E9a (Schritte B und D): Mengenänderung und Einspeisevergütungen stehen
            // nur da, wenn ein Szenario sie gepflegt hat — „wie Erwartet" wird nicht
            // wiederholt, und ohne Pflege bleibt die Tafel Zeile für Zeile die von vorher.
            if (w.MengeGepflegt || b.MengeGepflegt)
                liste.Add(Annahme(AnnahmeZeile.MENGE, MyResource.Resource.WIRT_ANN_MENGE,
                    w.MengeWirksam.ToString(AENDERUNG, kultur) + " %",
                    0.0.ToString(AENDERUNG, kultur) + " %",
                    b.MengeWirksam.ToString(AENDERUNG, kultur) + " %",
                    true));
            const string VERGUETUNG = "0.000";
            if (SzenarioSatz.Gepflegt(w.Einspeiseverguetung, p.Einspeiseverguetung) ||
                SzenarioSatz.Gepflegt(b.Einspeiseverguetung, p.Einspeiseverguetung))
                liste.Add(Annahme(AnnahmeZeile.VERGUETUNG, MyResource.Resource.WIRT_ANN_VERGUETUNG,
                    w.EinspeiseverguetungWirksam(p.Einspeiseverguetung).ToString(VERGUETUNG, kultur) + " €/kWh",
                    p.Einspeiseverguetung.ToString(VERGUETUNG, kultur) + " €/kWh",
                    b.EinspeiseverguetungWirksam(p.Einspeiseverguetung).ToString(VERGUETUNG, kultur) + " €/kWh",
                    true));
            double kwkErwartet = p.EinspeiseverguetungKWK ?? 0.0;
            if (SzenarioSatz.Gepflegt(w.EinspeiseverguetungKwk, kwkErwartet) ||
                SzenarioSatz.Gepflegt(b.EinspeiseverguetungKwk, kwkErwartet))
                liste.Add(Annahme(AnnahmeZeile.VERGUETUNG_KWK, MyResource.Resource.WIRT_ANN_VERGUETUNG_KWK,
                    (w.EinspeiseverguetungKwkWirksam(p.EinspeiseverguetungKWK) ?? 0.0).ToString(VERGUETUNG, kultur) + " €/kWh",
                    kwkErwartet.ToString(VERGUETUNG, kultur) + " €/kWh",
                    (b.EinspeiseverguetungKwkWirksam(p.EinspeiseverguetungKWK) ?? 0.0).ToString(VERGUETUNG, kultur) + " €/kWh",
                    true));
            return liste;
        }

        private static AnnahmeZeile Annahme(string schluessel, string groesse, string unguenstig,
                                            string erwartet, string guenstig, bool gepflegt)
        {
            return new AnnahmeZeile
            {
                Schluessel = schluessel,
                Groesse = groesse ?? "",
                Unguenstig = unguenstig,
                Erwartet = erwartet,
                Guenstig = guenstig,
                Herkunft = gepflegt ? MyResource.Resource.WIRT_ANN_GEPFLEGT
                                    : MyResource.Resource.WIRT_ANN_VORGABE,
                Gepflegt = gepflegt
            };
        }

        /// <summary>
        /// ETAPPE E5 (U10, Konzept § 2.11.7, Entscheid A14): der <b>Hinweistext</b> unter der
        /// Annahmentafel — was ein Szenario heute variiert und was nicht. Wortlaut der
        /// Konzeptfassung ohne den Roadmap-Satz (A14).
        ///
        /// <para><b>Die Zahlen im Text sind die WIRKSAMEN</b>: ohne Pflege die Vorgaben
        /// (±10 %, ±10 %, ±2 a — <see cref="SzenarioSatz.VORGABE_INVEST_PROZENT"/> und die
        /// Konstanten daneben), mit gepflegtem Satz die gepflegten Werte, ungünstig vor
        /// günstig („+15 / −5 %"). Der Text gilt, bis die vollständige Szenarioabdeckung
        /// (E9) ihn überflüssig macht.</para>
        /// </summary>
        /// <param name="p">Der Parametersatz der Gruppe; <c>null</c> = Vorgaben.</param>
        /// <param name="kultur">Zahlenformat; <c>null</c> = aktuelle Kultur.</param>
        public static string Szenariohinweis(WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            SzenarioSatz worst = (p != null ? p.SatzFuer(WirtschaftlichkeitSzenario.WORST) : null)
                                 ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            SzenarioSatz best = (p != null ? p.SatzFuer(WirtschaftlichkeitSzenario.BEST) : null)
                                ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
            return string.Format(kultur, MyResource.Resource.WIRT_SZEN_HINWEIS,
                                 Spanne(worst.InvestWirksam, best.InvestWirksam, kultur),
                                 Spanne(worst.ErtragWirksam, best.ErtragWirksam, kultur),
                                 Spanne(worst.DauerWirksam, best.DauerWirksam, kultur));
        }

        /// <summary>
        /// Die Spanne einer Szenariogröße als Zahl ohne Einheit: symmetrisch „±10",
        /// sonst ungünstig vor günstig „+15 / −5". Die Einheit steht im Ressourcentext.
        /// </summary>
        internal static string Spanne(double unguenstig, double guenstig, CultureInfo kultur)
        {
            if (Math.Abs(unguenstig + guenstig) < 1e-9)
                return Math.Abs(unguenstig) < 1e-9
                     ? "0"
                     : "±" + Math.Abs(unguenstig).ToString("0.#", kultur);
            return unguenstig.ToString("+0.#;−0.#;0", kultur) + " / " +
                   guenstig.ToString("+0.#;−0.#;0", kultur);
        }

        /// <summary>
        /// ETAPPE E8a (U48, Mockup „Was ist angenommen?"): die <b>Fußzeile</b> des Abschnitts —
        /// wie viele Szenarien gerechnet sind und woher ihre Annahmen kommen: „Drei Szenarien
        /// gerechnet · Annahmen aus Vorgaben, nichts gepflegt". Ist ein Feld des
        /// Szenario-Parametersatzes gepflegt, nennt die Zeile die gepflegten Größen — mit den
        /// Namen und nach der Regel der Annahmentafel (<see cref="Annahmen"/>), damit Tafel
        /// und Fußzeile dasselbe sagen.
        /// </summary>
        /// <param name="gerechnet">Die Zahl der Szenarien, für die ein Ergebnis vorliegt (0 … 3).</param>
        /// <param name="p">Der Parametersatz der Gruppe; <c>null</c> = die Herkunft bleibt ungesagt.</param>
        /// <param name="kultur">Zahlenformat; <c>null</c> = aktuelle Kultur.</param>
        public static string Szenarienfuss(int gerechnet, WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            string lauf = gerechnet >= 3 ? MyResource.Resource.WIRT_FUSS_DREI
                        : gerechnet <= 0 ? MyResource.Resource.WIRT_FUSS_KEINE
                        : string.Format(kultur, MyResource.Resource.WIRT_FUSS_TEIL, gerechnet);
            if (p == null) return lauf;

            var gepflegt = new List<string>();
            foreach (AnnahmeZeile z in Annahmen(p, kultur))
                if (z.Gepflegt) gepflegt.Add(z.Groesse);
            return lauf + " · " + (gepflegt.Count == 0
                ? MyResource.Resource.WIRT_FUSS_VORGABEN
                : string.Format(kultur, MyResource.Resource.WIRT_FUSS_GEPFLEGT, string.Join(", ", gepflegt.ToArray())));
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, Entscheid V‑3): das Label der nachrichtlichen Kennzahlen —
        /// „nachrichtlich (Anhang C)". Welche Kennzahlen es trägt (Amortisation und
        /// Zinsfuß), sagt <see cref="WirtschaftlichkeitZeilen.IstNachrichtlich"/>.
        /// </summary>
        public static string NachrichtlichLabel()
        {
            return MyResource.Resource.WIRT_KZ_NACHRICHTLICH;
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, Befund A2): die <b>Mehrdeutigkeitswarnung</b> des internen
        /// Zinsfußes — nur, wenn die Differenzreihe gegen die Referenz MEHR als einmal
        /// das Vorzeichen wechselt (<see cref="WirtschaftlichkeitErgebnis.IrrMehrdeutig"/>).
        /// <c>""</c> = keine Warnung, auch dann, wenn nicht gezählt wurde.
        /// </summary>
        public static string IzfWarnung(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || !e.IrrMehrdeutig) return "";
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.WIRT_IZF_MEHRDEUTIG,
                                 e.IrrVorzeichenwechsel.Value);
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, Befund A2; Q16): der <b>Grund</b>, warum ein Stand keinen
        /// internen Zinsfuß trägt, obwohl seine Differenz gerechnet ist — „kein Zinsfuß
        /// bestimmbar" (die Reihe wechselt ihr Vorzeichen nicht, oder die Nullstelle liegt
        /// außerhalb des Suchbereichs). <c>""</c>, wenn es einen Zinsfuß gibt oder keine
        /// Differenz (Referenz, Fehlgrund — dort sagt der Fehlgrund, was fehlt).
        /// </summary>
        public static string IzfGrund(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || e.IRR.HasValue || !e.KapitalwertDiff.HasValue) return "";
            return MyResource.Resource.WIRT_IZF_KEIN_WERT;
        }

        /// <summary>
        /// ETAPPE E5 (Q16): der Grund einer leeren Amortisationszelle — die Differenz ist
        /// gerechnet, ihr kumulierter Barwert erreicht die Nulllinie aber im
        /// Betrachtungszeitraum nicht. <c>""</c> in jedem anderen Fall.
        /// </summary>
        public static string AmortisationGrund(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || e.AmortisationJahre.HasValue || !e.KapitalwertDiff.HasValue) return "";
            return MyResource.Resource.WIRT_GRUND_KEINE_AMORTISATION;
        }

        /// <summary>
        /// ETAPPE E5 (Konzept § 6.3 Nr. 31, entschieden 22.09.2026): das Kennzeichen einer
        /// Ergebniszeile ohne Nachweisumschlag — „Nachweis liegt mit der nächsten Rechnung
        /// vor". <c>""</c>, wenn die Zeile ihren Nachweis trägt oder frisch gerechnet ist.
        /// </summary>
        public static string NachweisKennzeichen(WirtschaftlichkeitErgebnis e)
        {
            return e != null && e.OhneNachweis ? MyResource.Resource.WIRT_NACHWEIS_NAECHSTE_RECHNUNG : "";
        }
    }

    /// <summary>
    /// ETAPPE E5 (V‑A) — eine <b>Deklarationszeile</b> der Bewertung nach DIN EN 17463
    /// (<see cref="ValeriAusweis.Deklarationen"/>): ein sprachneutraler Schlüssel und der
    /// Text aus <c>MyResource</c> (Drei-Schichten-Regel).
    /// </summary>
    public sealed class ValeriDeklaration
    {
        /// <summary>„Rechnung nominal" (6.3.2).</summary>
        public const string NOMINAL = "NOMINAL";

        /// <summary>Energie-/Stromsteuerentlastungen ja, Ertragsteuern nein (7.1.2).</summary>
        public const string STEUERN = "STEUERN";

        /// <summary>Keine Abschreibung als Zahlung, Restwert linear (Abweichung von 6.4).</summary>
        public const string RESTWERT = "RESTWERT";

        /// <summary>Kein Risikozuschlag (6.5), nicht monetäre Wirkungen benannt.</summary>
        public const string RISIKO = "RISIKO";

        /// <summary>Der sprachneutrale Schlüssel (ASCII, eingefroren).</summary>
        public string Schluessel = "";

        /// <summary>Der Text in der Sprache der Oberfläche.</summary>
        public string Text = "";
    }

    /// <summary>
    /// ETAPPE E5 (U2) — eine Zeile der <b>Annahmentafel</b>
    /// (<see cref="ValeriAusweis.Annahmen"/>): die Größe, ihr wirksamer Wert in den drei
    /// Szenarien, fertig formatiert, und die Herkunft.
    /// </summary>
    public sealed class AnnahmeZeile
    {
        /// <summary>Kalkulationszins.</summary>
        public const string ZINS = "ZINS";

        /// <summary>Preissteigerung Energie.</summary>
        public const string PREIS_E = "PREIS_E";

        /// <summary>Preissteigerung Betrieb.</summary>
        public const string PREIS_B = "PREIS_B";

        /// <summary>Preissteigerung Investition und Ersatz.</summary>
        public const string PREIS_I = "PREIS_I";

        /// <summary>Änderung der Investition.</summary>
        public const string INVEST = "INVEST";

        /// <summary>Änderung der Erträge.</summary>
        public const string ERTRAG = "ERTRAG";

        /// <summary>Änderung der Nutzungsdauer.</summary>
        public const string DAUER = "DAUER";

        /// <summary>Betrachtungszeitraum — ohne Pflege in allen drei Szenarien gleich
        /// (ETAPPE E9a: je Szenario pflegbar, Schritt B).</summary>
        public const string ZEITRAUM = "ZEITRAUM";

        /// <summary>ETAPPE E9a: Mengenänderung je Szenario — nur, wenn gepflegt.</summary>
        public const string MENGE = "MENGE";

        /// <summary>ETAPPE E9a: Einspeisevergütung PV je Szenario — nur, wenn gepflegt.</summary>
        public const string VERGUETUNG = "VERGUETUNG";

        /// <summary>ETAPPE E9a: Einspeisevergütung KWK je Szenario — nur, wenn gepflegt.</summary>
        public const string VERGUETUNG_KWK = "VERGUETUNG_KWK";

        /// <summary>Der sprachneutrale Schlüssel der Größe.</summary>
        public string Schluessel = "";

        /// <summary>Der Anzeigename der Größe.</summary>
        public string Groesse = "";

        /// <summary>Der wirksame Wert im Szenario Ungünstig (Worst).</summary>
        public string Unguenstig = "";

        /// <summary>Der Wert im Szenario Erwartet.</summary>
        public string Erwartet = "";

        /// <summary>Der wirksame Wert im Szenario Günstig (Best).</summary>
        public string Guenstig = "";

        /// <summary>„Vorgabe", „gepflegt" oder „Projektwert …".</summary>
        public string Herkunft = "";

        /// <summary>ETAPPE E8a (U48): Trägt der Satz Ungünstig oder Günstig für diese Größe
        /// einen gepflegten Wert? Der Betrachtungszeitraum ist Projektwert und nie gepflegt.</summary>
        public bool Gepflegt;
    }
}
