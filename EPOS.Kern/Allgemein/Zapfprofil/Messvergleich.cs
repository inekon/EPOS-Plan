using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wo die gemessene Spitze im Band der synthetischen Dauerlinie liegt (Kapitel 7 Zeile Z5,
    /// Kennzahl (b)). <see cref="Unbestimmt"/> heißt „nicht entscheidbar" — ohne Ensemble oder ohne
    /// Stundenwerte der Messung; nie eine stille Antwort.
    /// </summary>
    internal enum Spitzenlage
    {
        /// <summary>Nicht entscheidbar (kein Ensemble, keine Stundenwerte).</summary>
        Unbestimmt = 0,

        /// <summary>Unter der unteren Bandgrenze — die Rechnung überschätzt die Spitze stärker als erwartet.</summary>
        Unterhalb = 1,

        /// <summary>Im Band — die Abnahme der Stufe Z5 ist erfüllt.</summary>
        ImBand = 2,

        /// <summary>Über der oberen Bandgrenze — die Rechnung unterschätzt die Spitze.</summary>
        Oberhalb = 3
    }

    /// <summary>
    /// <b>Kennzahl (a) — Energie</b>: <c>Verhaeltnis = Q_gemessen / Q_gerechnet</c> und
    /// <c>Abweichung = Verhaeltnis − 1</c>. Beides sind Verhältniszahlen; die gemessene Menge selbst
    /// verlässt den Vergleich nicht (Konzept Kapitel 9 K5).
    /// </summary>
    internal sealed record Energieabgleich(double Verhaeltnis, double Abweichung);

    /// <summary>
    /// <b>Kennzahl (b) — Dauerlinie und Band</b> (Konzept 3.6 / Lehre 1: „Die Messspitze liegt bei
    /// etwa P90 der synthetischen Dauerlinie"): die gemessene Stundenspitze und die beiden
    /// Bandgrenzen, jede <b>bezogen auf die größte gerechnete Stundenleistung</b> (K5 — nur
    /// Verhältniszahlen).
    ///
    /// <para><b>Das Band ist ein Quantil der DAUERLINIE</b>, nicht der Ensemblespitzen: Die
    /// 8 760 Stundenwerte der gerechneten Reihe werden sortiert, und die Grenzen sind ihre Quantile
    /// <see cref="PerzentilUnten"/> (Vorgabe 0,85) und <see cref="PerzentilOben"/> (0,95), geteilt
    /// durch die größte Stundenleistung. Die Lehre sagt gerade, dass die echte Messspitze eines
    /// Objekts nicht das Maximum der Rechnung trifft, sondern ein hohes Quantil ihrer Dauerlinie;
    /// die Grenzen liegen deshalb UNTER 1, und <see cref="Spitzenlage.Oberhalb"/> heißt: Die Messung
    /// trifft eine höhere Stelle der Dauerlinie als erwartet.</para>
    ///
    /// <para><b>Ohne Ensemble bleibt diese Kennzahl rechenbar</b> — sie braucht nur die gerechnete
    /// Reihe. <see cref="Dauerlinienwerte"/> nennt, über wie viele Stundenwerte sie gebildet ist;
    /// <see cref="Spitzenlage.Unbestimmt"/> heißt „ohne Stundenwerte der Messung".</para>
    /// </summary>
    internal sealed record Bandabgleich(Spitzenlage Lage, double? Spitzenverhaeltnis, double? BandUnten,
                                        double? BandOben, int Dauerlinienwerte, double PerzentilUnten,
                                        double PerzentilOben);

    /// <summary>
    /// <b>Die Streuung der Realisierungsspitzen</b> — die eigene Kennzahl des Ensembles (Konzept 4.4,
    /// Gleichzeitigkeit): die Quantile <see cref="PerzentilUnten"/> und <see cref="PerzentilOben"/>
    /// der größten Stunde JE Realisierung, beide bezogen auf die größte Stundenleistung der
    /// gerechneten Reihe (K5), und das Verhältnis der beiden Grenzen zueinander
    /// (<see cref="Streubreite"/> = oben / unten; 1 = alle Realisierungen treffen dieselbe Spitze).
    ///
    /// <para>Sie sagt, <b>wie weit die Stochastik streut</b>, und ist damit etwas anderes als das
    /// Band der Dauerlinie, gegen das die Messung gehalten wird. <b>Ohne Ensemble</b> gibt es sie
    /// nicht: Der Vergleich liefert dann <c>null</c> und den Hinweis
    /// <c>MESSVERGLEICH_OHNE_ENSEMBLE</c>, nie eine stille Null.</para>
    /// </summary>
    internal sealed record Spitzenstreuung(double Unten, double Oben, double Streubreite, int Realisierungen,
                                           double PerzentilUnten, double PerzentilOben);

    /// <summary>
    /// <b>Kennzahl (c) — √N-Skalierung</b> (Kapitel 7 Zeile Z5): Die Spitze je Einheit fällt mit der
    /// Zahl der Einheiten wie <c>1/√N</c> (Gleichzeitigkeit, 4.4). Das Verhältnis der Spitzen JE
    /// EINHEIT ist dasselbe wie das der Spitzen — N kürzt sich heraus —, und es wird gegen
    /// <c>1/√N</c> gehalten:
    ///
    /// <code>
    /// Spitzenverhaeltnis  = P_mess / P_rech            ( = (P_mess/N) / (P_rech/N) )
    /// WurzelNVerhaeltnis  = 1 / √N
    /// Skalierungsmass     = Spitzenverhaeltnis · √N     (1 = die Überschätzung folgt genau 1/√N)
    /// </code>
    ///
    /// <para>Ein <see cref="Skalierungsmass"/> über 1 heißt: Die Rechnung überschätzt die Spitze
    /// WENIGER als das √N-Gesetz erwarten lässt; unter 1: mehr.</para>
    /// </summary>
    internal sealed record WurzelNAbgleich(int Einheiten, double Spitzenverhaeltnis, double WurzelNVerhaeltnis,
                                           double Skalierungsmass);

    /// <summary>
    /// <b>Die Form EINES Tagtyps</b> (Kennzahl (d)): die 24 Stundenanteile des mittleren Tagesgangs
    /// gemessen und gerechnet (je Summe 1), die Zahl der eingegangenen Tage und zwei Maße —
    /// <see cref="MittlereAbweichung"/> = <c>(1/24) · Σ |a_i − b_i|</c> (das Maß der Schwelle) und
    /// <see cref="VerschobenerAnteil"/> = <c>(1/2) · Σ |a_i − b_i|</c>, der Anteil der Tagesenergie,
    /// der in anderen Stunden liegt (0 … 1, nur zur Anschauung).
    /// </summary>
    internal sealed record Tagesgangabweichung(ZapfTagtyp Tagtyp, int TageGemessen, int TageGerechnet,
                                               double MittlereAbweichung, double VerschobenerAnteil,
                                               IReadOnlyList<double> AnteileGemessen,
                                               IReadOnlyList<double> AnteileGerechnet);

    /// <summary>
    /// <b>Kennzahl (d) — Formabgleich des Tagesgangs</b>: je Tagtyp, den BEIDE Seiten führen, eine
    /// <see cref="Tagesgangabweichung"/>; <see cref="Formmass"/> ist die größte mittlere Abweichung
    /// darunter und wird gegen <see cref="Schwelle"/> gehalten (Parameter
    /// <see cref="ZapfParameter.VALIDIERUNG_FORMSCHWELLE"/>). Ohne einen vollständigen Tag der
    /// Messung ist <see cref="Formmass"/> <c>null</c> und <see cref="ImRahmen"/> <c>false</c> —
    /// „nicht entschieden", nicht „in Ordnung".
    /// </summary>
    internal sealed record Formabgleich(IReadOnlyList<Tagesgangabweichung> JeTagtyp, double? Formmass,
                                        double Schwelle)
    {
        /// <summary>Liegt die Form im Rahmen? Ohne Maß <c>false</c> (nicht entschieden).</summary>
        internal bool ImRahmen => Formmass.HasValue && Formmass.Value <= Schwelle;
    }

    /// <summary>
    /// <b>Kennzahl (e) — Monatsverteilung</b>: je Monat der Anteil an der Jahresmenge, gemessen und
    /// gerechnet (je Summe 1), dazu die größte absolute Abweichung und ihr Monat (1 … 12).
    /// Anteile statt Mengen — die gemessene Menge verlässt den Vergleich nicht (K5).
    /// </summary>
    internal sealed record Monatsabgleich(IReadOnlyList<double> AnteileGemessen,
                                          IReadOnlyList<double> AnteileGerechnet,
                                          double GroessteAbweichung, int GroessterMonat);

    /// <summary>
    /// <b>Der Eingang des Vergleichs</b> (Stufe Z5): die gemessene Reihe, die gerechnete Jahresreihe
    /// (deterministisch ODER stochastisch — der Vergleich fragt nicht, welche), der Kalender der
    /// gerechneten Reihe, die Stundenspitzen der Realisierungen des Ensembles
    /// (<c>Jahresensemble.StundenspitzenKw</c>) und die Einheitenzahl der Zonen. Die drei Schwellen
    /// kommen aus dem Parametersatz (<see cref="Messvergleich.AusParametern"/>); die Vorgaben hier
    /// gelten nur, wenn kein Parametersatz da ist.
    /// </summary>
    internal sealed record Messvergleichseingang
    {
        /// <summary>Die gemessene Reihe.</summary>
        public Messreihe Reihe { get; init; }

        /// <summary>Die Spreizung θ_Zapf − θ̄_KW [K] — nur für eine Volumenreihe.</summary>
        public double SpreizungK { get; init; }

        /// <summary>Die gerechnete Jahresreihe (Zapfung plus Zirkulation), 8760 Stunden.</summary>
        public Bilanzreihe Gerechnet { get; init; }

        /// <summary>Die 365 Tagtypen der gerechneten Reihe (<c>Zapfkalender.Bilden</c>).</summary>
        public IReadOnlyList<ZapfTagtyp> Kalender { get; init; }

        /// <summary>Die größte Stunde JE Realisierung des Ensembles [kW]; leer = kein Ensemble.</summary>
        public IReadOnlyList<double> SynthetischeStundenspitzenKw { get; init; } = new double[0];

        /// <summary>Die Summe der Einheiten aller Zonen (N der √N-Skalierung); 0 = unbekannt.</summary>
        public int Einheiten { get; init; }

        /// <summary>
        /// Die <b>Feiertage des Messjahrs</b> als Jahrestage 1 … 365 im Raster des Kerns (ohne
        /// Schalttag); <c>null</c> oder leer = unbekannt. Genannt, zählt ein voller Messtag auf einem
        /// Feiertag im Formabgleich als Sonn-/Feiertag — wie derselbe Tag in der Rechnung (ein
        /// Feiertag am Samstag bleibt Samstag, 4.2). Ohne Angabe bleibt die Regel
        /// <see cref="Messvergleich.Tagtyp(DateTime)"/>.
        /// </summary>
        public IReadOnlyCollection<int> MessFeiertage { get; init; }

        /// <summary>Untere Bandgrenze als Perzentil [-] (Vorgabe 0,85).</summary>
        public double BandUnten { get; init; } = 0.85;

        /// <summary>Obere Bandgrenze als Perzentil [-] (Vorgabe 0,95).</summary>
        public double BandOben { get; init; } = 0.95;

        /// <summary>Schwelle des Formabgleichs [-] (Vorgabe 0,01).</summary>
        public double Formschwelle { get; init; } = 0.01;
    }

    /// <summary>
    /// <b>Was der Vergleich ergibt</b>: die fünf Kennzahlen, die benannten Hinweise und — wenn der
    /// Vergleich nicht rechenbar ist — der <see cref="Abbruch"/>. Kein Text, nur Zahlen und
    /// <see cref="ZapfSatz"/>e mit Kennung und Werten (N11 (k)).
    /// </summary>
    internal sealed class Messvergleichsergebnis
    {
        /// <summary>Ist der Vergleich gerechnet?</summary>
        internal bool Ok => Abbruch == null;

        /// <summary>Der Grund, aus dem der Vergleich nicht rechenbar ist; <c>null</c> = gerechnet.</summary>
        internal ZapfSatz Abbruch { get; set; }

        /// <summary>Kennzahl (a) — Energie; <c>null</c> bei einem Abbruch.</summary>
        internal Energieabgleich Energie { get; set; }

        /// <summary>Kennzahl (b) — Dauerlinie und Band.</summary>
        internal Bandabgleich Band { get; set; }

        /// <summary>
        /// Die Streuung der Realisierungsspitzen; <c>null</c> ohne Ensemble („unbestimmt", samt
        /// Hinweis <c>MESSVERGLEICH_OHNE_ENSEMBLE</c>).
        /// </summary>
        internal Spitzenstreuung Streuung { get; set; }

        /// <summary>Kennzahl (c) — √N-Skalierung; <c>null</c> ohne Einheitenzahl oder ohne Stundenwerte.</summary>
        internal WurzelNAbgleich WurzelN { get; set; }

        /// <summary>Kennzahl (d) — Formabgleich des Tagesgangs.</summary>
        internal Formabgleich Form { get; set; }

        /// <summary>Kennzahl (e) — Monatsverteilung.</summary>
        internal Monatsabgleich Monate { get; set; }

        /// <summary>Die benannten Hinweise — nie still.</summary>
        internal List<ZapfSatz> Hinweise { get; } = new List<ZapfSatz>();
    }

    /// <summary>
    /// <b>Der Vergleichsbericht „synthetisch gegen gemessen"</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 1, Punkt 3): Er hält eine
    /// gemessene Reihe gegen die gerechnete Jahresreihe des Zapfprofils und gegen das Ensemble und
    /// liefert die fünf Kennzahlen der Abnahme.
    ///
    /// <para><b>Nur Verhältniszahlen</b> (Konzept Kapitel 9 K5): Jede Zahl des Ergebnisses ist ein
    /// Verhältnis oder ein Anteil — keine gemessene Menge, keine gemessene Leistung. Solange die
    /// Freigabe eigener Messdaten aussteht, verlässt so nichts die Anlage, was das Objekt
    /// beschreibt.</para>
    ///
    /// <para><b>Der echte Kalender der Messung.</b> Die Messreihe wird NIE in das
    /// 8760-Stunden-Raster geschoben (der Kern rechnet 365 Tage ohne Schaltjahr, die Messung kennt
    /// ihren wirklichen Kalender). Verglichen wird über <b>Monat</b>, <b>Wochentag</b> und
    /// <b>Tagesstunde</b> — Größen, die beide Seiten führen. <b>Feiertage kennt die Messung nur, wenn
    /// sie genannt sind</b> (<see cref="Messvergleichseingang.MessFeiertage"/>): Ohne Angabe zählt
    /// jeder Tag als der Wochentag, der er ist — ein Feiertag am Dienstag also als Werktag —, und
    /// <c>MESSVERGLEICH_OHNE_FEIERTAGE</c> nennt das, sobald überhaupt ein voller Tag in den
    /// Formabgleich eingeht.</para>
    ///
    /// <para><b>Ein Teiljahr ist kein Fehler</b> (Konzept 4.8): Deckt die Messung nicht alle 365 Tage
    /// ab, rechnen (a) Energie und (e) Monate über GENAU DIE TAGE, die sie abdeckt — die Rechnung
    /// wird auf dieselben Tage bezogen, nicht auf ihr Jahr —, und <c>MESSVERGLEICH_TEILJAHR</c> nennt
    /// die Zahl der Tage. Ein 29. Februar der Messung hat im Raster des Kerns keinen Gegentag; seine
    /// Schritte fallen aus beiden Seiten heraus, und <c>MESSVERGLEICH_SCHALTTAG</c> nennt es.</para>
    ///
    /// <para><b>Welche Kennzahl welchen Ausschnitt nimmt.</b> (a) Energie und (e) Monate rechnen
    /// über ALLE Zeitschritte der Reihe; (b) Spitze, (c) √N und (d) Form über die
    /// <b>vollständigen Stunden</b> bzw. <b>vollständigen Tage</b> — eine angeschnittene Stunde
    /// täuschte eine kleine Spitze vor, ein angeschnittener Tag eine schiefe Form.</para>
    ///
    /// <para><b>Rein:</b> keine Datenbank, keine Umgebung, kein Text.</para>
    /// </summary>
    internal static class Messvergleich
    {
        /// <summary>Die drei Tagtypen, die einen Tagesgang tragen (ein Ruhetag trägt keinen).</summary>
        internal static readonly IReadOnlyList<ZapfTagtyp> TAGTYPEN = Array.AsReadOnly(new[]
        {
            ZapfTagtyp.Werktag, ZapfTagtyp.Samstag, ZapfTagtyp.SonnFeiertag
        });

        /// <summary>
        /// Belegt die drei Schwellen des Eingangs aus dem Parametersatz
        /// (<see cref="ZapfParameter.VALIDIERUNG_BAND_UNTEN"/>,
        /// <see cref="ZapfParameter.VALIDIERUNG_BAND_OBEN"/>,
        /// <see cref="ZapfParameter.VALIDIERUNG_FORMSCHWELLE"/>). Ein Schlüssel, den der Satz nicht
        /// führt, lässt die Vorgabe des Eingangs stehen — der Vergleich fällt nicht aus, weil eine
        /// Setzung fehlt.
        /// </summary>
        internal static Messvergleichseingang AusParametern(Messvergleichseingang eingang, Parametersatz p)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (p == null) return eingang;
            return eingang with
            {
                BandUnten = p.Enthaelt(ZapfParameter.VALIDIERUNG_BAND_UNTEN)
                    ? p.Wert(ZapfParameter.VALIDIERUNG_BAND_UNTEN) : eingang.BandUnten,
                BandOben = p.Enthaelt(ZapfParameter.VALIDIERUNG_BAND_OBEN)
                    ? p.Wert(ZapfParameter.VALIDIERUNG_BAND_OBEN) : eingang.BandOben,
                Formschwelle = p.Enthaelt(ZapfParameter.VALIDIERUNG_FORMSCHWELLE)
                    ? p.Wert(ZapfParameter.VALIDIERUNG_FORMSCHWELLE) : eingang.Formschwelle
            };
        }

        /// <summary>
        /// <b>Der Vergleich.</b> Ein unbrauchbarer Eingang ist eine benannte Ablehnung im Ergebnis,
        /// keine Ausnahme; was der Vergleich nicht entscheiden kann, bleibt <c>null</c> und wird
        /// benannt.
        /// </summary>
        internal static Messvergleichsergebnis Vergleichen(Messvergleichseingang e)
        {
            var erg = new Messvergleichsergebnis();
            if (e == null || e.Reihe == null)
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_OHNE_MESSREIHE");
                return erg;
            }
            if (e.Gerechnet == null)
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_OHNE_RECHNUNG");
                return erg;
            }
            if (!(e.Gerechnet.JahressummeKwh > 0.0))
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_RECHNUNG_OHNE_MENGE");
                return erg;
            }
            if (e.Kalender == null || e.Kalender.Count != Zapfkalender.TAGE)
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_KALENDER_RASTER", Zapfkalender.TAGE);
                return erg;
            }
            if (!(e.BandUnten > 0.0) || !(e.BandOben > e.BandUnten) || e.BandOben >= 1.0)
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_BAND_UNGUELTIG", e.BandUnten, e.BandOben);
                return erg;
            }

            IReadOnlyList<double> kwhJeSchritt;
            try
            {
                kwhJeSchritt = e.Reihe.EnergieJeSchrittKwh(e.SpreizungK);
            }
            catch (ZapfprofilEingabeException ex)
            {
                erg.Abbruch = ex.Satz ?? ZapfSatz.Neu("MESSVERGLEICH_SPREIZUNG_FEHLT", e.SpreizungK);
                return erg;
            }

            // ---- Welche Tage des Rechenjahres die Messung überhaupt abdeckt ---------------
            // Jeder Zeitschritt der Messung bekommt seinen Jahrestag im Raster des Kerns (365 Tage
            // ohne Schaltjahr). Ein Schalttag hat dort KEINEN Gegentag: Seine Schritte fallen aus
            // beiden Seiten heraus, und ein Hinweis nennt es - stiller als eine verschobene Stunde
            // wäre nichts.
            int[] tag = Jahrestage(e.Reihe, kwhJeSchritt.Count, out int schalttagschritte);
            var abgedeckt = new bool[Zapfkalender.TAGE];
            for (int i = 0; i < tag.Length; i++)
                if (tag[i] >= 0) abgedeckt[tag[i]] = true;
            int tageAbgedeckt = 0;
            foreach (bool b in abgedeckt) if (b) tageAbgedeckt++;
            if (schalttagschritte > 0)
                erg.Hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_SCHALTTAG", schalttagschritte));
            if (tageAbgedeckt == 0)
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_OHNE_VERGLEICHSTAG");
                return erg;
            }
            if (tageAbgedeckt < Zapfkalender.TAGE)
                erg.Hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_TEILJAHR", tageAbgedeckt, Zapfkalender.TAGE));

            // ---- (a) Energie -------------------------------------------------------------
            // BEIDE Seiten über DIESELBEN Tage: Eine Messung über zwei Monate gegen die Jahressumme
            // zu halten ergäbe ein Verhältnis um 1/6 und sagte nichts über die Rechnung.
            double gemessenKwh = 0.0;
            for (int i = 0; i < kwhJeSchritt.Count; i++)
                if (tag[i] >= 0) gemessenKwh += kwhJeSchritt[i];
            double[] tagessummenRech = TagessummenKwh(e.Gerechnet);
            double gerechnetKwh = 0.0;
            for (int d = 0; d < Zapfkalender.TAGE; d++)
                if (abgedeckt[d]) gerechnetKwh += tagessummenRech[d];
            if (!(gerechnetKwh > 0.0))
            {
                erg.Abbruch = ZapfSatz.Neu("MESSVERGLEICH_RECHNUNG_OHNE_MENGE");
                return erg;
            }
            double verhaeltnis = gemessenKwh / gerechnetKwh;
            erg.Energie = new Energieabgleich(verhaeltnis, verhaeltnis - 1.0);
            erg.Hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_ENERGIE_ABWEICHUNG", verhaeltnis - 1.0));

            // ---- (e) Monate --------------------------------------------------------------
            erg.Monate = Monate(kwhJeSchritt, tag, gemessenKwh, tagessummenRech, abgedeckt, gerechnetKwh);

            // ---- Die vollständigen Stunden der Messung -----------------------------------
            IReadOnlyList<Messstunde> stunden = e.Reihe.Stundenwerte(e.SpreizungK);
            if (stunden.Count == 0)
            {
                erg.Hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_OHNE_STUNDENWERTE", e.Reihe.AufloesungMin));
                erg.Band = new Bandabgleich(Spitzenlage.Unbestimmt, null, null, null, 0, e.BandUnten, e.BandOben);
                // Die Streuung des Ensembles braucht die Messung nicht - sie steht auch hier.
                erg.Streuung = Streuung(e, e.Gerechnet.GroessterStundenwertKw, erg.Hinweise);
                erg.Form = new Formabgleich(new Tagesgangabweichung[0], null, e.Formschwelle);
                return erg;
            }

            double spitzeMessKw = stunden.Max(s => s.Kwh);
            double spitzeRechKw = e.Gerechnet.GroessterStundenwertKw;
            double spitzenverhaeltnis = spitzeRechKw > 0.0 ? spitzeMessKw / spitzeRechKw : double.NaN;

            // ---- (b) Band der synthetischen Dauerlinie -----------------------------------
            erg.Band = Band(e, spitzenverhaeltnis, spitzeRechKw, erg.Hinweise);

            // ---- Die Streuung der Realisierungsspitzen (eigene Kennzahl) -----------------
            erg.Streuung = Streuung(e, spitzeRechKw, erg.Hinweise);

            // ---- (c) √N-Skalierung -------------------------------------------------------
            if (e.Einheiten >= 1 && !double.IsNaN(spitzenverhaeltnis))
            {
                double wurzel = Math.Sqrt(e.Einheiten);
                erg.WurzelN = new WurzelNAbgleich(e.Einheiten, spitzenverhaeltnis, 1.0 / wurzel,
                                                  spitzenverhaeltnis * wurzel);
            }
            else if (e.Einheiten < 1)
            {
                // Nur die FEHLENDE Einheitenzahl heißt „ohne Einheiten". Eine unbrauchbare Spitze
                // (kein gerechneter Stundenwert) ist ein anderer Grund und steht schon bei (b).
                erg.Hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_OHNE_EINHEITEN"));
            }

            // ---- (d) Form des Tagesgangs -------------------------------------------------
            erg.Form = Form(e, stunden, erg.Hinweise);
            if (erg.Form.Formmass.HasValue)
                erg.Hinweise.Add(erg.Form.ImRahmen
                    ? ZapfSatz.Neu("MESSVERGLEICH_FORM_IM_RAHMEN", erg.Form.Formmass.Value, e.Formschwelle)
                    : ZapfSatz.Neu("MESSVERGLEICH_FORM_UEBER_SCHWELLE", erg.Form.Formmass.Value, e.Formschwelle));
            return erg;
        }

        // =================================================================================
        //  (b) Das Band der synthetischen Spitze
        // =================================================================================

        /// <summary>
        /// Die beiden Bandgrenzen als <b>Quantile der synthetischen Dauerlinie</b> — die sortierten
        /// Stundenwerte der gerechneten Reihe —, beide, wie die gemessene Spitze, bezogen auf die
        /// größte gerechnete Stundenleistung (K5).
        ///
        /// <para><b>Warum die Dauerlinie und nicht die Ensemblespitzen:</b> Konzept 3.6 (Lehre 1)
        /// sagt „die Messspitze liegt bei etwa P90 der synthetischen Dauerlinie". Das ist eine
        /// Aussage über die EINE gerechnete Reihe — wo in ihrer Dauerlinie die Messung landet —, und
        /// sie gilt deshalb auch für eine deterministische Rechnung ohne Ensemble. Wie weit die
        /// Stochastik streut, sagt die eigene Kennzahl <see cref="Spitzenstreuung"/>; die beiden
        /// Fragen werden nicht mehr in einer Zahl vermischt.</para>
        ///
        /// <para>Der Rang eines Quantils folgt derselben Regel wie <c>Perzentilwerte.Rang</c>:
        /// <c>k = ⌈q · n⌉</c>, mindestens 1, höchstens n — EINE Regel im Bestand.</para>
        /// </summary>
        private static Bandabgleich Band(Messvergleichseingang e, double spitzenverhaeltnis, double spitzeRechKw,
                                         ICollection<ZapfSatz> hinweise)
        {
            IReadOnlyList<double> reihe = e.Gerechnet.StundenKwh;
            if (reihe == null || reihe.Count == 0 || !(spitzeRechKw > 0.0) || double.IsNaN(spitzenverhaeltnis))
            {
                return new Bandabgleich(Spitzenlage.Unbestimmt, double.IsNaN(spitzenverhaeltnis) ? null : spitzenverhaeltnis,
                                        null, null, 0, e.BandUnten, e.BandOben);
            }

            var geordnet = reihe.ToArray();
            Array.Sort(geordnet);
            double unten = geordnet[Perzentilwerte.Rang(geordnet.Length, e.BandUnten) - 1] / spitzeRechKw;
            double oben = geordnet[Perzentilwerte.Rang(geordnet.Length, e.BandOben) - 1] / spitzeRechKw;

            Spitzenlage lage = spitzenverhaeltnis < unten ? Spitzenlage.Unterhalb
                             : spitzenverhaeltnis > oben ? Spitzenlage.Oberhalb
                             : Spitzenlage.ImBand;
            hinweise.Add(lage switch
            {
                Spitzenlage.Unterhalb => ZapfSatz.Neu("MESSVERGLEICH_SPITZE_UNTER_BAND", spitzenverhaeltnis, unten),
                Spitzenlage.Oberhalb => ZapfSatz.Neu("MESSVERGLEICH_SPITZE_UEBER_BAND", spitzenverhaeltnis, oben),
                _ => ZapfSatz.Neu("MESSVERGLEICH_SPITZE_IM_BAND", spitzenverhaeltnis, unten, oben)
            });
            return new Bandabgleich(lage, spitzenverhaeltnis, unten, oben, geordnet.Length, e.BandUnten, e.BandOben);
        }

        /// <summary>
        /// Die Streuung der Realisierungsspitzen — dieselben zwei Quantile, aber über die größte
        /// Stunde JE Realisierung; <c>null</c> ohne Ensemble (dann steht der Hinweis
        /// <c>MESSVERGLEICH_OHNE_ENSEMBLE</c>, nie eine stille Null).
        /// </summary>
        private static Spitzenstreuung Streuung(Messvergleichseingang e, double spitzeRechKw,
                                                ICollection<ZapfSatz> hinweise)
        {
            IReadOnlyList<double> stichprobe = e.SynthetischeStundenspitzenKw ?? new double[0];
            if (stichprobe.Count == 0 || !(spitzeRechKw > 0.0))
            {
                hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_OHNE_ENSEMBLE"));
                return null;
            }

            var geordnet = stichprobe.ToArray();
            Array.Sort(geordnet);
            double unten = geordnet[Perzentilwerte.Rang(geordnet.Length, e.BandUnten) - 1] / spitzeRechKw;
            double oben = geordnet[Perzentilwerte.Rang(geordnet.Length, e.BandOben) - 1] / spitzeRechKw;
            double breite = unten > 0.0 ? oben / unten : double.NaN;
            hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_SPITZENSTREUUNG", geordnet.Length, unten, oben));
            return new Spitzenstreuung(unten, oben, breite, geordnet.Length, e.BandUnten, e.BandOben);
        }

        // =================================================================================
        //  (d) Die Form des Tagesgangs
        // =================================================================================

        /// <summary>
        /// Je Tagtyp, den BEIDE Seiten führen, die mittleren Stundenanteile und ihre Abweichung. Die
        /// Messung liefert nur <b>vollständige Tage</b> (24 vollständige Stunden) und bekommt ihren
        /// Tagtyp aus dem echten Wochentag und — wenn genannt — den Feiertagen des Messjahrs.
        /// </summary>
        private static Formabgleich Form(Messvergleichseingang e, IReadOnlyList<Messstunde> stunden,
                                         ICollection<ZapfSatz> hinweise)
        {
            // Die Messung: je Datum die 24 Stunden, nur vollständige Tage.
            var jeTag = new Dictionary<DateTime, double[]>();
            foreach (Messstunde s in stunden)
            {
                DateTime tag = s.Beginn.Date;
                if (!jeTag.TryGetValue(tag, out double[] gang)) jeTag[tag] = gang = Neu24();
                gang[s.Beginn.Hour] += s.Kwh;
            }
            var zaehler = new Dictionary<DateTime, int>();
            foreach (Messstunde s in stunden)
            {
                DateTime tag = s.Beginn.Date;
                zaehler[tag] = (zaehler.TryGetValue(tag, out int n) ? n : 0) + 1;
            }

            var summeMess = new Dictionary<ZapfTagtyp, double[]>();
            var tageMess = new Dictionary<ZapfTagtyp, int>();
            bool feiertagsfrage = false;
            bool feiertageGenannt = e.MessFeiertage != null && e.MessFeiertage.Count > 0;
            foreach (KeyValuePair<DateTime, double[]> kv in jeTag.OrderBy(x => x.Key))
            {
                if (zaehler[kv.Key] != Zapfkalender.STUNDEN_TAG) continue;      // angeschnittener Tag
                ZapfTagtyp typ = Tagtyp(kv.Key, e.MessFeiertage);
                // JEDER volle Tag kann ein Feiertag sein - der 1. Mai ebenso wie der 3. Oktober -,
                // und die Messung sagt es nicht. Der Hinweis haengt deshalb an jedem vollen Tag, nicht
                // nur an den Sonntagen: Ein Feiertag am Dienstag zaehlt hier als Werktag, und gerade
                // DAS ist die Abweichung, die der Anwender wissen muss.
                feiertagsfrage = !feiertageGenannt;
                if (!summeMess.TryGetValue(typ, out double[] s)) summeMess[typ] = s = Neu24();
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) s[h] += kv.Value[h];
                tageMess[typ] = (tageMess.TryGetValue(typ, out int n) ? n : 0) + 1;
            }
            if (feiertagsfrage) hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_OHNE_FEIERTAGE"));

            // Die Rechnung: je Tagtyp die Summe über die Tage des Kalenders (ein Ruhetag zählt nicht).
            var summeRech = new Dictionary<ZapfTagtyp, double[]>();
            var tageRech = new Dictionary<ZapfTagtyp, int>();
            IReadOnlyList<double> reihe = e.Gerechnet.StundenKwh;
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                ZapfTagtyp typ = e.Kalender[d];
                if (!TAGTYPEN.Contains(typ)) continue;
                if (!summeRech.TryGetValue(typ, out double[] s)) summeRech[typ] = s = Neu24();
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) s[h] += reihe[d * Zapfkalender.STUNDEN_TAG + h];
                tageRech[typ] = (tageRech.TryGetValue(typ, out int n) ? n : 0) + 1;
            }

            var liste = new List<Tagesgangabweichung>(TAGTYPEN.Count);
            double? formmass = null;
            foreach (ZapfTagtyp typ in TAGTYPEN)
            {
                bool mess = summeMess.ContainsKey(typ) && Summe(summeMess[typ]) > 0.0;
                bool rech = summeRech.ContainsKey(typ) && Summe(summeRech[typ]) > 0.0;
                if (!mess || !rech)
                {
                    if (mess != rech) hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_TAGTYP_FEHLT", Formvektor.Tagtyp(typ)));
                    continue;
                }
                double[] a = Anteile(summeMess[typ]);
                double[] b = Anteile(summeRech[typ]);
                double summeAbstand = 0.0;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) summeAbstand += Math.Abs(a[h] - b[h]);
                double mittel = summeAbstand / Zapfkalender.STUNDEN_TAG;
                liste.Add(new Tagesgangabweichung(typ, tageMess[typ], tageRech[typ], mittel, summeAbstand / 2.0,
                                                  Array.AsReadOnly(a), Array.AsReadOnly(b)));
                if (!formmass.HasValue || mittel > formmass.Value) formmass = mittel;
            }
            if (liste.Count == 0) hinweise.Add(ZapfSatz.Neu("MESSVERGLEICH_OHNE_VOLLEN_TAG"));
            return new Formabgleich(liste, formmass, e.Formschwelle);
        }

        /// <summary>
        /// Der Tagtyp eines wirklichen Datums: Montag bis Freitag Werktag, Samstag Samstag, Sonntag
        /// Sonn-/Feiertag. <b>Feiertage kennt die Messung nicht</b> — sie zählen als <b>Werktag ihres
        /// Wochentags</b> (ein Feiertag am Dienstag also als Werktag, nicht als Sonntag); der Hinweis
        /// <c>MESSVERGLEICH_OHNE_FEIERTAGE</c> nennt das an jedem vollen Tag.
        /// </summary>
        internal static ZapfTagtyp Tagtyp(DateTime tag)
        {
            switch (tag.DayOfWeek)
            {
                case DayOfWeek.Saturday: return ZapfTagtyp.Samstag;
                case DayOfWeek.Sunday: return ZapfTagtyp.SonnFeiertag;
                default: return ZapfTagtyp.Werktag;
            }
        }

        /// <summary>
        /// Der Tagtyp eines wirklichen Datums mit den <b>genannten Feiertagen</b> des Messjahrs
        /// (Jahrestage 1 … 365 im Raster des Kerns): ein Feiertag von Montag bis Freitag zählt als
        /// Sonn-/Feiertag, ein Feiertag am Samstag bleibt Samstag — dieselbe Regel wie
        /// <c>Zapfkalender.Bilden</c> (4.2). Ein 29. Februar hat keinen Jahrestag und bleibt beim
        /// Wochentag. Ohne Feiertage gilt <see cref="Tagtyp(DateTime)"/>.
        /// </summary>
        internal static ZapfTagtyp Tagtyp(DateTime tag, IReadOnlyCollection<int> feiertage)
        {
            ZapfTagtyp typ = Tagtyp(tag);
            if (typ != ZapfTagtyp.Werktag || feiertage == null || feiertage.Count == 0) return typ;
            if (tag.Month == 2 && tag.Day == 29) return typ;
            int jahrestag = tag.DayOfYear;
            if (DateTime.IsLeapYear(tag.Year) && jahrestag > 60) jahrestag--;
            return feiertage.Contains(jahrestag) ? ZapfTagtyp.SonnFeiertag : typ;
        }

        // =================================================================================
        //  (e) Die Monatsverteilung
        // =================================================================================

        /// <summary>
        /// Die Monatsanteile beider Seiten (je Summe 1) und die größte Abweichung — <b>beide über
        /// DIESELBEN Tage</b> (<paramref name="abgedeckt"/>). Die Messung zählt den Monat ihres
        /// wirklichen Zeitstempels, die Rechnung die Tage desselben Monats aus ihrem festen Raster;
        /// ein Monat, den die Messung nicht berührt, steht auf beiden Seiten auf 0. Beide Seiten
        /// geben Anteile ab, keine Mengen (K5).
        ///
        /// <para><b>Die größte Abweichung wird nur unter den ABGEDECKTEN Monaten gesucht</b> — ein
        /// Monat mit 0 gegen 0 ist keine Übereinstimmung, sondern eine Nichtaussage, und er darf den
        /// gefundenen Monat nicht verdrängen.</para>
        /// </summary>
        private static Monatsabgleich Monate(IReadOnlyList<double> kwhJeSchritt, IReadOnlyList<int> tag,
                                             double gemessenKwh, IReadOnlyList<double> tagessummenRech,
                                             IReadOnlyList<bool> abgedeckt, double gerechnetKwh)
        {
            var mess = new double[Zapfkalender.MONATE];
            for (int i = 0; i < kwhJeSchritt.Count; i++)
                if (tag[i] >= 0) mess[Zapfkalender.Monat(tag[i] + 1) - 1] += kwhJeSchritt[i];

            var rech = new double[Zapfkalender.MONATE];
            var monatAbgedeckt = new bool[Zapfkalender.MONATE];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                if (!abgedeckt[d]) continue;
                int m = Zapfkalender.Monat(d + 1) - 1;
                rech[m] += tagessummenRech[d];
                monatAbgedeckt[m] = true;
            }

            double[] anteileMess = Anteile(mess, gemessenKwh);
            double[] anteileRech = Anteile(rech, gerechnetKwh);
            double groesste = 0.0;
            int monat = 0;
            for (int m = 0; m < Zapfkalender.MONATE; m++)
            {
                if (!monatAbgedeckt[m]) continue;
                double d = Math.Abs(anteileMess[m] - anteileRech[m]);
                if (monat == 0 || d > groesste) { groesste = d; monat = m + 1; }
            }
            return new Monatsabgleich(Array.AsReadOnly(anteileMess), Array.AsReadOnly(anteileRech), groesste,
                                      monat == 0 ? 1 : monat);
        }

        /// <summary>
        /// Der <b>Jahrestag im Raster des Kerns</b> (0 … 364) zu jedem Zeitschritt der Messung —
        /// oder −1, wenn der Schritt keinen Gegentag hat. Dazu die Zahl der Schritte, die auf einen
        /// 29. Februar fallen.
        ///
        /// <para><b>Der Kern rechnet 365 Tage ohne Schaltjahr</b> (Kapitel 7). Ein 29. Februar der
        /// Messung hat deshalb keinen Vergleichstag: Seine Schritte fallen aus <b>beiden</b> Seiten
        /// heraus — sie dem 28. Februar oder dem 1. März zuzuschlagen verschöbe eine ganze
        /// Tagesmenge. Jeder Tag NACH dem 29. Februar eines Schaltjahres rückt um einen Tag zurück,
        /// damit der 1. März der Messung auf den 1. März der Rechnung fällt und nicht auf den
        /// 2. März.</para>
        ///
        /// <para>Läuft die Messung über mehr als ein Jahr, treffen mehrere Zeitschritte denselben
        /// Jahrestag; das ist gewollt (der Vergleich hält den Jahresgang gegen den Jahresgang) und
        /// steht schon im Hinweis <c>MESSREIHE_UEBER_EIN_JAHR</c> des Lesers.</para>
        /// </summary>
        private static int[] Jahrestage(Messreihe reihe, int schritte, out int schalttagschritte)
        {
            schalttagschritte = 0;
            var tag = new int[schritte];
            for (int i = 0; i < schritte; i++)
            {
                DateTime z = reihe.Zeitpunkt(i);
                if (z.Month == 2 && z.Day == 29) { tag[i] = -1; schalttagschritte++; continue; }
                int jahrestag = z.DayOfYear;
                if (DateTime.IsLeapYear(z.Year) && jahrestag > 60) jahrestag--;
                tag[i] = jahrestag - 1;
            }
            return tag;
        }

        /// <summary>Die 365 Tagessummen [kWh] der gerechneten Reihe — die Bezugsgröße jedes Teiljahrs.</summary>
        private static double[] TagessummenKwh(Bilanzreihe gerechnet)
        {
            IReadOnlyList<double> stunden = gerechnet.StundenKwh;
            var tage = new double[Zapfkalender.TAGE];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                double s = 0.0;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) s += stunden[d * Zapfkalender.STUNDEN_TAG + h];
                tage[d] = s;
            }
            return tage;
        }

        // =================================================================================
        //  Kleinkram
        // =================================================================================

        private static double[] Neu24() => new double[Zapfkalender.STUNDEN_TAG];

        private static double Summe(IReadOnlyList<double> werte)
        {
            double s = 0.0;
            foreach (double w in werte) s += w;
            return s;
        }

        /// <summary>Die Anteile einer Reihe an ihrer Summe (Summe 1); eine Summe von 0 ergibt Nullen.</summary>
        private static double[] Anteile(IReadOnlyList<double> werte) => Anteile(werte, Summe(werte));

        private static double[] Anteile(IReadOnlyList<double> werte, double summe)
        {
            var a = new double[werte.Count];
            if (!(summe > 0.0)) return a;
            for (int i = 0; i < a.Length; i++) a[i] = werte[i] / summe;
            return a;
        }
    }
}
