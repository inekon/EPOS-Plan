using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // ETAPPE E8a — DIE GLIEDERUNG DES ZAHLUNGSBILDS (Konzept Wirtschaftlichkeit § 2.11.4
    // V‑C; Mockup Dialog_Formel_Zahlenprobe.html, Kategorie 8: ValERI-Block 2
    // „Zahlungsreihen", Gliederung „Woraus entsteht die Zahl?" mit Nominalsumme und
    // Differenzspalte (U46), Brückenbild (U41), Tafel „Was daraus im Lauf wird" (U47)).
    //
    // ALLES IN DIESER DATEI IST AUSGABE. Gerechnet wird nichts Neues: Die Jahresreihen
    // stehen seit Etappe E7 im Zahlungsbild des KapitalwertRechners; hier werden sie nach
    // den sechs Bestandteilen der Norm (6.1 bis 6.4) geordnet und je Bestandteil
    // abgezinst — mit demselben Faktor (1 + i)^−t wie im Rechner. Die Summe der
    // Bestandteil-Barwerte ist der Kapitalwert des Bildes; die Gliederung prüft das
    // selbst (Stimmig), und gegen den gespeicherten Lauf wird sie abgeglichen, bevor
    // Seite oder Bericht sie zeigen.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// ETAPPE E8a — ein <b>Bestandteil</b> der Gliederung des Kapitalwerts: seine nominalen
    /// Beträge je Jahr, sein Barwert und seine Nominalsumme.
    /// </summary>
    public sealed class Zahlungsbestandteil
    {
        /// <summary>Der sprachneutrale Schlüssel (<see cref="Zahlungsgliederung.INVESTITION"/> …).</summary>
        public string Schluessel = "";

        /// <summary>
        /// Nominaler Betrag je Jahr [€], Index 0 … T — <b>Ausgaben negativ, Einnahmen
        /// positiv</b>, wie in der Mehrjahrestabelle des Berichts. Die Investition steht im
        /// Jahr 0, der Restwert im Jahr T.
        /// </summary>
        public double[] JeJahr = new double[0];

        /// <summary>
        /// Der Barwert [€] — Σ JeJahr[t] · (1 + i)^−t. Beim Restwert ist es der Barwert,
        /// den der Rechner selbst ausweist (<c>Zahlungsbild.RestwertBarwert</c>).
        /// </summary>
        public double Barwert;

        /// <summary>Die Nominalsumme [€] — was über den Zeitraum tatsächlich fließt.</summary>
        public double Nominal
        {
            get
            {
                double summe = 0;
                if (JeJahr != null) foreach (double w in JeJahr) summe += w;
                return summe;
            }
        }

        /// <summary>Der Betrag des Jahres <paramref name="t"/>; außerhalb der Reihe 0.</summary>
        public double Wert(int t)
            => JeJahr != null && t >= 0 && t < JeJahr.Length ? JeJahr[t] : 0.0;

        /// <summary>
        /// Die Jahre mit einem Betrag ungleich 0, aufsteigend — beim Bestandteil
        /// <see cref="Zahlungsgliederung.ERSATZ"/> die Jahre der fälligen
        /// Ersatzbeschaffungen (U47).
        /// </summary>
        public List<int> Jahre()
        {
            var jahre = new List<int>();
            if (JeJahr != null)
                for (int t = 0; t < JeJahr.Length; t++)
                    if (JeJahr[t] != 0) jahre.Add(t);
            return jahre;
        }
    }

    /// <summary>
    /// ETAPPE E8a — die <b>Gliederung EINES Zahlungsbilds</b> in die sechs Bestandteile
    /// Investition, Betriebskosten, Energiekosten, Erlöse, Ersatzbeschaffungen und
    /// Restwert (DIN EN 17463, 6.1 bis 6.4).
    ///
    /// <para><b>Die Zuordnung.</b> Investition = I₀ nach Zuschussabzug im Jahr 0.
    /// Betriebskosten = die Betriebszeile des Bildes (beide Preissteigerungstöpfe,
    /// PAKET FX3). Energiekosten = Energie und CO₂-Abgabe. Erlöse = Einspeiseerlös und
    /// die benannten Erlösreihen (KWK-Zuschlag, Steuergutschriften, PV-Vergütung), im
    /// Jahr 0 die Pauschale nach § 9 KWKG — alles, was im Kapitalwert steht (Block A der
    /// Erlösrubrik). Ersatzbeschaffungen = die Ersatzreihe des Bildes; eine nach KD6
    /// verschobene Erstbeschaffung zahlt über dieselbe Reihe und steht deshalb hier,
    /// dieselbe Grenze wie im Barwert „Ersatzbeschaffungen" der Vergleichstabelle.
    /// Restwert = der nominale Restwert im Jahr T.</para>
    ///
    /// <para><b>Die Selbstprüfung.</b> Die sechs Barwerte summieren sich zum Kapitalwert des
    /// Bildes (<see cref="Stimmig"/>); die Abweichung ist Rundung im letzten Bit, weil der
    /// Rechner Energie und CO₂-Abgabe in EINER Klammer abzinst.</para>
    /// </summary>
    public sealed class Zahlungsgliederung
    {
        /// <summary>Investition I₀ nach Zuschussabzug (Jahr 0).</summary>
        public const string INVESTITION = "INVESTITION";

        /// <summary>Betriebskosten (Kategorien 2 und 3).</summary>
        public const string BETRIEB = "BETRIEB";

        /// <summary>Energiekosten samt CO₂-Abgabe (Kategorie 4).</summary>
        public const string ENERGIE = "ENERGIE";

        /// <summary>Zahlungswirksame Erlöse (Block A der Erlösrubrik).</summary>
        public const string ERLOESE = "ERLOESE";

        /// <summary>Ersatzbeschaffungen (Kategorie 1).</summary>
        public const string ERSATZ = "ERSATZ";

        /// <summary>Restwert am Ende des Betrachtungszeitraums (Kategorie 1).</summary>
        public const string RESTWERT = "RESTWERT";

        /// <summary>Die Bestandteile in der Reihenfolge der Seite, des Brückenbilds und der
        /// Zahlungsreihen.</summary>
        public static readonly IReadOnlyList<string> Reihenfolge = new[]
        {
            INVESTITION, BETRIEB, ENERGIE, ERLOESE, ERSATZ, RESTWERT
        };

        /// <summary>Kleinste Toleranz der Abgleiche [€] — eine Zahl der Seite zeigt ganze Euro.</summary>
        public const double TOLERANZ_EUR = 0.01;

        /// <summary>Der Betrachtungszeitraum des Bildes [a] — Jahre 0 … <see cref="Jahre"/>.</summary>
        public int Jahre;

        /// <summary>Der Kalkulationszins, mit dem abgezinst ist [%].</summary>
        public double ZinsProzent;

        /// <summary>Der Kapitalwert (Nettobarwert) des Bildes [€], wie der Rechner ihn ausweist.</summary>
        public double Kapitalwert;

        /// <summary>Die sechs Bestandteile in der <see cref="Reihenfolge"/>.</summary>
        public List<Zahlungsbestandteil> Bestandteile = new List<Zahlungsbestandteil>();

        /// <summary>Ein Bestandteil nach Schlüssel; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public Zahlungsbestandteil Bestandteil(string schluessel)
        {
            foreach (Zahlungsbestandteil b in Bestandteile)
                if (string.Equals(b.Schluessel, schluessel, StringComparison.Ordinal)) return b;
            return null;
        }

        /// <summary>Die Summe der Bestandteil-Barwerte [€].</summary>
        public double SummeBarwerte
        {
            get
            {
                double summe = 0;
                foreach (Zahlungsbestandteil b in Bestandteile) summe += b.Barwert;
                return summe;
            }
        }

        /// <summary>
        /// Summieren sich die Barwerte zum Kapitalwert des Bildes? Nein heißt: Das Bild
        /// ist mit einem anderen Zins gerechnet, als hier abgezinst wurde — dann zeigt es
        /// niemand.
        /// </summary>
        public bool Stimmig
            => !double.IsNaN(SummeBarwerte) && Math.Abs(SummeBarwerte - Kapitalwert) <= Toleranz(Kapitalwert);

        /// <summary>Die nominale Summe aller Bestandteile je Jahr [€] — im Jahr T mit dem Restwert.</summary>
        public double[] NettoJeJahr()
        {
            var netto = new double[Jahre + 1];
            foreach (Zahlungsbestandteil b in Bestandteile)
                for (int t = 0; t <= Jahre; t++) netto[t] += b.Wert(t);
            return netto;
        }

        /// <summary>
        /// Der Barwert je Jahr [€] — die abgezinste Summe der Bestandteile; im Jahr T mit
        /// dem Restwert-Barwert. Die Summe über alle Jahre ist <see cref="SummeBarwerte"/>.
        /// </summary>
        public double[] BarwertJeJahr()
        {
            var barwert = new double[Jahre + 1];
            double i = ZinsProzent / 100.0;
            foreach (Zahlungsbestandteil b in Bestandteile)
            {
                bool restwert = string.Equals(b.Schluessel, RESTWERT, StringComparison.Ordinal);
                for (int t = 0; t <= Jahre; t++)
                {
                    if (restwert) { if (t == Jahre) barwert[t] += b.Barwert; continue; }
                    double w = b.Wert(t);
                    if (w != 0) barwert[t] += t == 0 ? w : w * Math.Pow(1.0 + i, -t);
                }
            }
            return barwert;
        }

        /// <summary>
        /// Die Gliederung eines Zahlungsbilds.
        /// </summary>
        /// <param name="bild">Das Zahlungsbild eines Standes in einem Szenario.</param>
        /// <param name="zinsProzent">Der Zins, mit dem das Bild gerechnet ist [%] — der des
        /// Szenarios (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>).</param>
        /// <returns><c>null</c>, wenn das Bild keine Reihe führt.</returns>
        public static Zahlungsgliederung Aus(KapitalwertRechner.Zahlungsbild bild, double zinsProzent)
        {
            if (bild == null || bild.NominalReihe == null || bild.NominalReihe.Length < 2) return null;
            int T = bild.NominalReihe.Length - 1;
            double i = zinsProzent / 100.0;

            var investition = new double[T + 1];
            var betrieb = new double[T + 1];
            var energie = new double[T + 1];
            var erloese = new double[T + 1];
            var ersatz = new double[T + 1];
            var restwert = new double[T + 1];

            investition[0] = -bild.Investition;
            erloese[0] = Summe(bild.ErloesReihen, 0);
            for (int t = 1; t <= T; t++)
            {
                betrieb[t] = -Wert(bild.BetriebJeJahr, t);
                energie[t] = -(Wert(bild.EnergieJeJahr, t) + Wert(bild.BehgJeJahr, t));
                erloese[t] = Wert(bild.EinspeiseerloesJeJahr, t) + Summe(bild.ErloesReihen, t);
                ersatz[t] = -Wert(bild.ErsatzJeJahr, t);
            }
            restwert[T] = bild.RestwertNominal;

            var g = new Zahlungsgliederung { Jahre = T, ZinsProzent = zinsProzent, Kapitalwert = bild.Kapitalwert };
            g.Bestandteile.Add(Teil(INVESTITION, investition, i));
            g.Bestandteile.Add(Teil(BETRIEB, betrieb, i));
            g.Bestandteile.Add(Teil(ENERGIE, energie, i));
            g.Bestandteile.Add(Teil(ERLOESE, erloese, i));
            g.Bestandteile.Add(Teil(ERSATZ, ersatz, i));
            g.Bestandteile.Add(new Zahlungsbestandteil
            {
                Schluessel = RESTWERT, JeJahr = restwert, Barwert = bild.RestwertBarwert
            });
            return g;
        }

        /// <summary>
        /// Die <b>Differenz</b> zweier Gliederungen, Bestandteil für Bestandteil — Stand
        /// minus Referenz, je Jahr, als Barwert und als Kapitalwert. Die Summe der
        /// Barwertdifferenzen ist die Kapitalwertdifferenz (Differenzspalte U46,
        /// Brückenbild U41). <c>null</c>, wenn eine Seite fehlt oder die Zeiträume
        /// verschieden sind.
        /// </summary>
        public static Zahlungsgliederung Differenz(Zahlungsgliederung stand, Zahlungsgliederung referenz)
        {
            if (stand == null || referenz == null || stand.Jahre != referenz.Jahre) return null;
            var d = new Zahlungsgliederung
            {
                Jahre = stand.Jahre,
                ZinsProzent = stand.ZinsProzent,
                Kapitalwert = stand.Kapitalwert - referenz.Kapitalwert
            };
            foreach (string s in Reihenfolge)
            {
                Zahlungsbestandteil a = stand.Bestandteil(s), b = referenz.Bestandteil(s);
                var je = new double[stand.Jahre + 1];
                for (int t = 0; t <= stand.Jahre; t++)
                    je[t] = (a == null ? 0 : a.Wert(t)) - (b == null ? 0 : b.Wert(t));
                d.Bestandteile.Add(new Zahlungsbestandteil
                {
                    Schluessel = s,
                    JeJahr = je,
                    Barwert = (a == null ? 0 : a.Barwert) - (b == null ? 0 : b.Barwert)
                });
            }
            return d;
        }

        /// <summary>
        /// Trägt die Gliederung denselben Kapitalwert wie der gespeicherte Lauf? Nur dann
        /// zeigen Seite und Bericht ihre Jahresreihen neben dessen Kennzahlen.
        /// </summary>
        public static bool Passt(Zahlungsgliederung g, double? kapitalwert)
        {
            if (g == null || !kapitalwert.HasValue) return false;
            return Math.Abs(g.Kapitalwert - kapitalwert.Value) <= Toleranz(kapitalwert.Value);
        }

        /// <summary>Die Toleranz eines Abgleichs: ein Cent oder 1e‑9 des Betrags.</summary>
        public static double Toleranz(double betrag)
            => Math.Max(TOLERANZ_EUR, Math.Abs(betrag) * 1e-9);

        /// <summary>Der Anzeigename eines Bestandteils — aus <c>MyResource</c> (Drei-Schichten-Regel).</summary>
        public static string Titel(string schluessel)
        {
            switch (schluessel)
            {
                case INVESTITION: return MyResource.Resource.WIRT_GL_INVESTITION;
                case BETRIEB: return MyResource.Resource.WIRT_GL_BETRIEB;
                case ENERGIE: return MyResource.Resource.WIRT_GL_ENERGIE;
                case ERLOESE: return MyResource.Resource.WIRT_GL_ERLOESE;
                case ERSATZ: return MyResource.Resource.WIRT_GL_ERSATZ;
                case RESTWERT: return MyResource.Resource.WIRT_GL_RESTWERT;
                default: return schluessel ?? "";
            }
        }

        private static Zahlungsbestandteil Teil(string schluessel, double[] jeJahr, double i)
        {
            double barwert = 0;
            for (int t = 0; t < jeJahr.Length; t++)
                if (jeJahr[t] != 0) barwert += t == 0 ? jeJahr[t] : jeJahr[t] * Math.Pow(1.0 + i, -t);
            return new Zahlungsbestandteil { Schluessel = schluessel, JeJahr = jeJahr, Barwert = barwert };
        }

        private static double Wert(double[] reihe, int t)
            => reihe != null && t >= 0 && t < reihe.Length ? reihe[t] : 0.0;

        private static double Summe(IList<KapitalwertRechner.ErloesReihe> reihen, int t)
        {
            double summe = 0;
            if (reihen != null)
                foreach (KapitalwertRechner.ErloesReihe r in reihen)
                    if (r != null) summe += r.Wert(t);
            return summe;
        }
    }

    /// <summary>
    /// ETAPPE E8a — die <b>Gliederungen einer Vergleichsgruppe in den drei Szenarien</b>:
    /// je Szenario und Stand die <see cref="Zahlungsgliederung"/> aus den drei
    /// vollständigen Läufen, die der Verlauf ohnehin rechnet
    /// (<see cref="WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien"/> über den
    /// Betrachtungszeitraum) — keine eigene Rechnung.
    ///
    /// <para><b>Nur was zum gespeicherten Lauf passt.</b> Eine Gliederung, deren
    /// Kapitalwert nicht der des gespeicherten (bzw. in Sicht 2 gerechneten) Ergebnisses
    /// ist — die Parameter sind seither gespeichert, der Verlauf hat neu simuliert —,
    /// wird nicht aufgenommen; der Stand steht dann in <see cref="Abweichend"/>, und die
    /// Seite sagt, dass „Berechnen" die Reihen neu rechnet.</para>
    /// </summary>
    public sealed class Zahlungsgliederungen
    {
        private readonly Dictionary<string, Dictionary<int, Zahlungsgliederung>> _je =
            new Dictionary<string, Dictionary<int, Zahlungsgliederung>>(StringComparer.Ordinal);

        /// <summary>Der Betrachtungszeitraum der Läufe [a].</summary>
        public int Jahre;

        /// <summary>Die Stände, deren Reihen in mindestens einem Szenario nicht zum
        /// gespeicherten Lauf passen (oder in sich nicht stimmig sind).</summary>
        public HashSet<int> Abweichend = new HashSet<int>();

        /// <summary>Die Gliederung eines Standes in einem Szenario; <c>null</c> = keine.</summary>
        public Zahlungsgliederung Von(int idProjekt, string szenario)
        {
            Dictionary<int, Zahlungsgliederung> je;
            Zahlungsgliederung g;
            return szenario != null && _je.TryGetValue(szenario, out je) && je.TryGetValue(idProjekt, out g)
                 ? g : null;
        }

        /// <summary>Trägt der Stand in ALLEN drei Szenarien eine Gliederung?</summary>
        public bool Vollstaendig(int idProjekt)
        {
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                if (Von(idProjekt, s) == null) return false;
            return true;
        }

        /// <summary>Keine einzige Gliederung.</summary>
        public bool Leer
        {
            get
            {
                foreach (Dictionary<int, Zahlungsgliederung> je in _je.Values)
                    if (je.Count > 0) return false;
                return true;
            }
        }

        /// <summary>
        /// Die Gliederungen aus den drei Läufen eines Verlaufs.
        /// </summary>
        /// <param name="verlauf">Die drei Läufe über den Betrachtungszeitraum.</param>
        /// <param name="p">Der Parametersatz der Gruppe — er liefert je Szenario den Zins,
        /// mit dem der Lauf gerechnet ist.</param>
        /// <param name="gespeichert">Die Ergebnisse, neben denen die Reihen stehen;
        /// <c>null</c> = ohne Abgleich (nur die Selbstprüfung).</param>
        public static Zahlungsgliederungen Aus(WirtschaftlichkeitVerlaufSzenarien verlauf,
                                              WirtschaftlichkeitParameter p,
                                              IEnumerable<WirtschaftlichkeitErgebnis> gespeichert)
        {
            var satz = new Zahlungsgliederungen();
            if (verlauf == null || p == null) return satz;
            satz.Jahre = verlauf.Jahre;

            var ergebnisse = new List<WirtschaftlichkeitErgebnis>();
            if (gespeichert != null)
                foreach (WirtschaftlichkeitErgebnis e in gespeichert) if (e != null) ergebnisse.Add(e);

            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                WirtschaftlichkeitVerlauf lauf = verlauf.Lauf(s);
                if (lauf == null) continue;
                double zins = p.FuerSzenario(s).Zinssatz;
                var je = new Dictionary<int, Zahlungsgliederung>();
                foreach (VerlaufSerie serie in lauf.Absolut)
                {
                    if (serie == null || serie.Bild == null) continue;
                    Zahlungsgliederung g = Zahlungsgliederung.Aus(serie.Bild, zins);
                    if (g == null) continue;
                    bool passt = g.Stimmig;
                    if (passt && gespeichert != null)
                    {
                        WirtschaftlichkeitErgebnis e = Finde(ergebnisse, serie.IdProjekt, s);
                        passt = e != null && Zahlungsgliederung.Passt(g, e.Kapitalwert);
                    }
                    if (passt) je[serie.IdProjekt] = g;
                    else satz.Abweichend.Add(serie.IdProjekt);
                }
                satz._je[s] = je;
            }
            return satz;
        }

        /// <summary>
        /// Die <b>Leitversion</b> — der Stand, dessen Differenz zur Referenz Differenzspalte,
        /// Brückenbild und „Was daraus im Lauf wird" zeigen: der Stand mit der größten
        /// Kapitalwertdifferenz im Erwartungsfall (in Sicht 2 ist das B, der einzige
        /// Stand neben A). Ohne Differenz der erste Stand außer der Referenz; 0 = keiner.
        /// </summary>
        /// <param name="alle">Die Ergebnisse der Gruppe (alle Szenarien).</param>
        /// <param name="staende">Die gezeigten Stände in Gruppenreihenfolge.</param>
        /// <param name="idReferenz">Die wirksame Referenz.</param>
        public static int Leitversion(IEnumerable<WirtschaftlichkeitErgebnis> alle,
                                      IEnumerable<int> staende, int idReferenz)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (alle != null) foreach (WirtschaftlichkeitErgebnis e in alle) if (e != null) liste.Add(e);

            int beste = 0, erster = 0;
            double besteDiff = double.NegativeInfinity;
            if (staende == null) return 0;
            foreach (int id in staende)
            {
                if (id == idReferenz) continue;
                if (erster == 0) erster = id;
                WirtschaftlichkeitErgebnis e = Finde(liste, id, WirtschaftlichkeitSzenario.ERWARTET);
                if (e == null || !e.KapitalwertDiff.HasValue) continue;
                if (e.KapitalwertDiff.Value > besteDiff)
                {
                    besteDiff = e.KapitalwertDiff.Value;
                    beste = id;
                }
            }
            return beste != 0 ? beste : erster;
        }

        private static WirtschaftlichkeitErgebnis Finde(List<WirtschaftlichkeitErgebnis> alle, int idProjekt,
                                                        string szenario)
        {
            foreach (WirtschaftlichkeitErgebnis e in alle)
                if (e.IdProjekt == idProjekt && string.Equals(e.Szenario, szenario, StringComparison.Ordinal))
                    return e;
            return null;
        }
    }
}
