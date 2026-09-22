using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E5 (U39, Konzept § 2.13 (3)) — der Befund EINER Technik eines Standes:
    /// Wie viele ihrer betragstragenden Investitionspositionen tragen keine
    /// Nutzungsdauer, und liegt der Betrachtungszeitraum über der Vorgabe der Technik?
    ///
    /// <para><b>Warum das ein Prüfauftrag ist.</b> Ohne Nutzungsdauer läuft eine
    /// Position im Rechenkern „wie T": kein Ersatz, kein Restwert. Liegt T über der
    /// Vorgabe der Technik, wäre ein Ersatz fällig gewesen — dann ist der Kapitalwert zu
    /// günstig ausgewiesen. Eine Planungsleistung wird zu Recht nicht ersetzt, eine
    /// Hydraulik ohne Dauer zu Unrecht nicht; welches von beiden, entscheidet der
    /// Anwender, nicht der Rechenweg.</para>
    /// </summary>
    public sealed class OhneDauerBefund
    {
        /// <summary><c>Tab_Projekt.ID</c> des Standes.</summary>
        public int IdProjekt;

        /// <summary>Anzeigename des Standes (Stamm oder Variante).</summary>
        public string Stand = "";

        /// <summary><c>Tab_KostenKomponente.ID</c> der Technik.</summary>
        public int KomponentenId;

        /// <summary>Anzeigename der Technik aus der Nutzungsdauertabelle; leer ohne Vorgabe.</summary>
        public string Technik = "";

        /// <summary>Die Vorgabe der Technik [a] (Standardzeile von <c>Tab_Nutzungsdauer</c>);
        /// <c>null</c> = die Technik hat keine.</summary>
        public double? VorgabeJahre;

        /// <summary>Der Betrachtungszeitraum T [a], gegen den geprüft wurde.</summary>
        public int Betrachtungszeitraum;

        /// <summary>k — betragstragende Positionen OHNE Nutzungsdauer.</summary>
        public int Ohne;

        /// <summary>n — alle betragstragenden Positionen der Technik.</summary>
        public int Alle;

        /// <summary>Die Positionen ohne Dauer als „Bezeichnung, Betrag €".</summary>
        public List<string> Positionen = new List<string>();

        /// <summary>
        /// <c>true</c>, wenn der Stand für diese Technik den Hinweis trägt: T über der
        /// Vorgabe der Technik UND mindestens eine betragstragende Position ohne Dauer
        /// (§ 2.13 (3)). Liegt die Vorgabe über T (Mockup: „Photovoltaik — Vorgabe 25 a
        /// über dem Zeitraum"), wäre kein Ersatz fällig — dann kein Hinweis.
        /// </summary>
        public bool Hinweis;

        /// <summary>
        /// Der Satz des Hinweises — derselbe, den die Kostenverwaltung über ihrer Tafel
        /// „Ersatz und Restwert" zeigt (<see cref="ErsatzRestwertTafel.Hinweis(IEnumerable{ErsatzRestwertTafel.Eingabe}, int, string, int, CultureInfo)"/>).
        /// Leer ohne Hinweis.
        /// </summary>
        public string Text = "";
    }

    /// <summary>
    /// ETAPPE E5 (U39) — die zwei Nutzungsdauer-Auskünfte der Ergebnisseite und des
    /// Berichts: die Zeitraumzeile (G7, <see cref="NutzungsdauerAbgleich.Hinweis"/>) und
    /// die Hinweiszeilen „k von n Positionen ohne Nutzungsdauer" (§ 2.13 (3)).
    /// </summary>
    public sealed class NutzungsdauerHinweise
    {
        /// <summary>
        /// Die Zeitraumzeile der Gruppe: T gegen die kürzeste und längste gepflegte
        /// Nutzungsdauer, Ersatzjahr und Restwert. Leer ohne Betrachtungszeitraum.
        /// </summary>
        public string Zeitraumzeile = "";

        /// <summary>Je Stand und Technik mit betragstragenden Positionen ein Befund —
        /// auch ohne Hinweis; die Zählung ist die Auskunft.</summary>
        public List<OhneDauerBefund> Befunde = new List<OhneDauerBefund>();

        /// <summary>
        /// Die Hinweiszeilen, wie Seite und Bericht sie zeigen: je Satz eine Zeile;
        /// tragen mehrere Stände denselben Satz, steht er einmal mit ihren Namen davor.
        /// </summary>
        public List<string> Zeilen = new List<string>();

        /// <summary>k über alle Befunde.</summary>
        public int Ohne
        {
            get { int k = 0; foreach (OhneDauerBefund b in Befunde) k += b.Ohne; return k; }
        }

        /// <summary>n über alle Befunde.</summary>
        public int Alle
        {
            get { int n = 0; foreach (OhneDauerBefund b in Befunde) n += b.Alle; return n; }
        }
    }

    /// <summary>
    /// ETAPPE E5 (U39, Konzept § 2.13 (3) und Mockup-Anhang U39) — <b>das Einsammeln der
    /// Positionen einer Vergleichsgruppe</b> für die Nutzungsdauer-Hinweise.
    ///
    /// <para><b>Warum ein Kern-Controller.</b> Die Zeitraumzeile sammelte bis hierher die
    /// Hülle der Ergebnisseite selbst (<c>WirtschaftlichkeitSeiteGaben.Zeitraumzeile</c>),
    /// und Wort- und Tabellenbericht je noch einmal. Die Textbildung lag schon im Kern
    /// (<see cref="NutzungsdauerAbgleich"/>, <see cref="ErsatzRestwertTafel"/>); zu tun
    /// war das Einsammeln — einmal, hier, damit iOS, Windows und Bericht dieselbe Zeile
    /// bekommen.</para>
    ///
    /// <para><b>Rein lesend und ohne Rechenwirkung.</b> Gelesen wird im Szenario
    /// ERWARTET, derselbe Stand, mit dem der Kapitalwert rechnet: die Positionen samt
    /// Kaskadenbetrag (<see cref="KostenProjektPositionenCtrl.Lies(int, int, int)"/>) und
    /// die Vorgabe der Technik (<see cref="NutzungsdauerCtrl.Standard"/>). Zuschusszeilen
    /// bleiben außen vor — sie sind keine Investitionsposition (K5).</para>
    /// </summary>
    internal static class NutzungsdauerHinweisCtrl
    {
        /// <summary>
        /// Bildet beide Auskünfte für die Stände einer Gruppe.
        /// </summary>
        /// <param name="betrachtungszeitraum">T [a] der Gruppe (Parametersatz des Stamms).</param>
        /// <param name="staende">Die Stände in Listenreihenfolge (Id und Anzeigename).</param>
        /// <param name="kultur">Zahlenformat der Sätze; <c>null</c> = aktuelle Kultur.</param>
        internal static NutzungsdauerHinweise Bilde(int betrachtungszeitraum,
                                                    IEnumerable<KeyValuePair<int, string>> staende,
                                                    CultureInfo kultur)
        {
            var h = new NutzungsdauerHinweise();
            if (betrachtungszeitraum <= 0 || staende == null) return h;
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            var liste = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<int, string> s in staende)
                if (s.Key > 0) liste.Add(s);

            // (a) G7 — die Zeitraumzeile. Dieselbe Leseliste, mit der der Kapitalwert
            // rechnet (LiesInvestitionen, Szenario Erwartet). Ein Lesefehler lässt die
            // Zeile still entfallen: Sie ist Ausweis, kein Ergebnis.
            try
            {
                var positionen = new List<KapitalwertRechner.InvestPosition>();
                foreach (KeyValuePair<int, string> s in liste)
                    positionen.AddRange(WirtschaftlichkeitCtrl.LiesInvestitionen(
                        s.Key, WirtschaftlichkeitSzenario.ERWARTET));
                h.Zeitraumzeile = NutzungsdauerAbgleich.Hinweis(betrachtungszeitraum, positionen, kultur);
            }
            catch { h.Zeitraumzeile = ""; }

            // (b) U39 — k von n je Stand und Technik.
            foreach (KeyValuePair<int, string> s in liste)
            {
                try
                {
                    foreach (int komponente in Komponenten(s.Key))
                    {
                        OhneDauerBefund b = Befund(s.Key, s.Value, komponente,
                                                   betrachtungszeitraum, kultur);
                        if (b != null) h.Befunde.Add(b);
                    }
                }
                catch { /* ein unlesbarer Stand kostet seine Befunde, nicht die der anderen */ }
            }

            // (c) Die Zeilen: derselbe Satz an mehreren Ständen steht EINMAL da, mit
            // ihren Namen davor. Eine Gruppe aus einem einzigen Stand braucht keinen Namen.
            var reihenfolge = new List<string>();
            var namen = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (OhneDauerBefund b in h.Befunde)
            {
                if (!b.Hinweis || string.IsNullOrEmpty(b.Text)) continue;
                List<string> n;
                if (!namen.TryGetValue(b.Text, out n))
                {
                    n = new List<string>();
                    namen[b.Text] = n;
                    reihenfolge.Add(b.Text);
                }
                if (!n.Contains(b.Stand)) n.Add(b.Stand);
            }
            foreach (string text in reihenfolge)
                h.Zeilen.Add(liste.Count > 1
                    ? string.Join(", ", namen[text].ToArray()) + ": " + text
                    : text);
            return h;
        }

        /// <summary>
        /// Die Techniken, an denen der Stand Investitionspositionen führt — aus derselben
        /// Kaskade, mit der der Kapitalwert rechnet; in der Reihenfolge ihres ersten
        /// Auftretens.
        /// </summary>
        private static List<int> Komponenten(int idProjekt)
        {
            var liste = new List<int>();
            foreach (InvestKaskade.Zeile z in InvestKaskade.Lies(idProjekt, WirtschaftlichkeitSzenario.ERWARTET))
                if (z.Komponente > 0 && !z.Zuschuss && !liste.Contains(z.Komponente))
                    liste.Add(z.Komponente);
            return liste;
        }

        /// <summary>
        /// Der Befund EINER Technik eines Standes; <c>null</c>, wenn sie keine
        /// betragstragende Investitionsposition führt.
        /// </summary>
        private static OhneDauerBefund Befund(int idProjekt, string stand, int komponentenId,
                                              int jahre, CultureInfo kultur)
        {
            var eingaben = new List<ErsatzRestwertTafel.Eingabe>();
            foreach (KostenProjektPositionenCtrl.Zeile z in KostenProjektPositionenCtrl.Lies(
                         idProjekt, komponentenId, DbWerte.KOSTEN_KATEGORIE_INVESTITION))
            {
                KostenVorlagenPosition pos = z.Raster;
                if (pos == null) continue;
                if (string.Equals((pos.Kostenart ?? "").Trim(), DbWerte.KOSTENART_ZUSCHUSS,
                                  StringComparison.Ordinal)) continue;
                eingaben.Add(new ErsatzRestwertTafel.Eingabe
                {
                    Bezeichnung = pos.Bezeichnung ?? "",
                    Betrag = pos.BetragNetto ?? 0,
                    Nutzungsdauer = pos.Nutzungsdauer,
                    StartJahr = z.StartJahr,
                    IstErloes = pos.IstErloes,
                    NutzungsdauerId = pos.NutzungsdauerId
                });
            }

            int alle;
            List<ErsatzRestwertTafel.Eingabe> ohne = ErsatzRestwertTafel.OhneDauer(eingaben, out alle);
            if (alle == 0) return null;

            var b = new OhneDauerBefund
            {
                IdProjekt = idProjekt,
                Stand = stand ?? "",
                KomponentenId = komponentenId,
                Betrachtungszeitraum = jahre,
                Ohne = ohne.Count,
                Alle = alle
            };
            foreach (ErsatzRestwertTafel.Eingabe e in ohne)
                b.Positionen.Add(ErsatzRestwertTafel.Eintrag(e, kultur));

            NutzungsdauerZeile vorgabe = NutzungsdauerCtrl.Standard(komponentenId);
            if (vorgabe != null && vorgabe.Nutzungsdauer.HasValue)
            {
                b.VorgabeJahre = vorgabe.Nutzungsdauer.Value;
                b.Technik = vorgabe.Technik ?? "";
            }

            // § 2.13 (3): T über der Vorgabe der Technik UND k ≥ 1. Der Satz ist der der
            // Kostenverwaltung — er nennt beides, T gegen die Vorgabe und die Positionen.
            b.Hinweis = b.Ohne > 0 && b.VorgabeJahre.HasValue && jahre > b.VorgabeJahre.Value;
            if (b.Hinweis)
                b.Text = ErsatzRestwertTafel.Hinweis(eingaben, komponentenId, "", jahre, kultur);
            return b;
        }
    }
}
