using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>U30 — die Tafel „Ersatz und Restwert"</b> unter dem Investitionsraster der
    /// Kostenverwaltung (Mockup <c>Dialog_Formel_Zahlenprobe.html</c>, Abschnitt 1;
    /// Konzept Wirtschaftlichkeit § 2.13 (3)).
    ///
    /// <para><b>Sie rechnet nichts Neues.</b> Ersatzjahre, Ersatzbeträge und der
    /// lineare Restwert kommen aus <see cref="KapitalwertRechner.Ersatz"/> — derselben
    /// Funktion, die die Kapitalwertrechnung durchläuft (U30 hat sie dafür aus
    /// <see cref="KapitalwertRechner.Rechne"/> herausgelöst). Eine zweite Formel wäre
    /// eine zweite Wahrheit: dieselbe Falle, die die Investitionskaskade vor
    /// <see cref="InvestKaskade"/> dreimal verschieden beantwortet hat (Anwenderbefund
    /// W5‑B‑7). Hier entstehen allein die Texte.</para>
    ///
    /// <para><b>Was die Tafel zeigt.</b> Je Position Betrag, Nutzungsdauer n, die
    /// Ersatzjahre innerhalb T mit dem Barwert der Ersatzbeschaffungen, den Restwert im
    /// Jahr T (nominal und als Barwert) und die Herkunft der Dauer; darunter eine
    /// Summenzeile der Komponente. Erlös- und Zuschusszeilen bleiben außen vor — sie
    /// bekommen im Rechenkern weder Ersatz noch Restwert (K5).</para>
    ///
    /// <para><b>Der Hinweis darüber</b> (<see cref="Hinweis"/>) ist ein Prüfauftrag,
    /// keine Fehlermeldung: Eine Planungsleistung wird zu Recht nicht ersetzt — eine
    /// Hydraulik ohne gepflegte Dauer würde es zu Unrecht nicht.</para>
    /// </summary>
    internal static class ErsatzRestwertTafel
    {
        /// <summary>
        /// EINE Zeile der Tafel, fertig gesetzt. Die Hülle reicht sie durch, die
        /// Oberfläche formatiert nichts (Drei-Schichten-Regel).
        /// </summary>
        internal sealed class Zeile
        {
            /// <summary>Bezeichnung der Position, in der Summenzeile der Komponentenname.</summary>
            public string Position = "";

            /// <summary>Die Summenzeile der Komponente — sie wird hervorgehoben.</summary>
            public bool IstSumme;

            /// <summary>Betrag netto [€], gesetzt.</summary>
            public string Betrag = "";

            /// <summary>„15 a", „—" oder — in der Summenzeile — „3 von 4".</summary>
            public string Dauer = "";

            /// <summary>„Jahr 15", „— deckungsgleich mit T", „— läuft wie T" oder „—".</summary>
            public string Ersatz = "";

            /// <summary>„Barwert 132.149"; leer, wo es keine Ersatzbeschaffung gibt.</summary>
            public string ErsatzBarwert = "";

            /// <summary>Restwert im Jahr T, nominal und gesetzt; „—" ohne gepflegte Dauer.</summary>
            public string Restwert = "";

            /// <summary>„Barwert 75.995"; leer, wo kein Restwert bleibt.</summary>
            public string RestwertBarwert = "";

            /// <summary>Woher die Nutzungsdauer stammt (<see cref="NutzungsdauerCtrl.Herkunft"/>).</summary>
            public string Herkunft = "";
        }

        /// <summary>
        /// Die Eingabe einer Position — Betrag, Dauer, Startjahr und die Positionsart.
        /// Sie entsteht in der Hülle aus dem ARBEITSSTAND des Dialogs, nicht aus der
        /// Datenbank: Bis „Speichern" lebt jede Eingabe nur im Objekt (Ä12/Ä19), und
        /// die Tafel soll zeigen, was der Anwender gerade sieht.
        /// </summary>
        internal sealed class Eingabe
        {
            public string Bezeichnung = "";
            public double Betrag;
            public double? Nutzungsdauer;
            public int StartJahr;
            public bool IstErloes;
            public int? NutzungsdauerId;
        }

        // =====================================================================
        //  Die Zeilen
        // =====================================================================

        /// <summary>
        /// Die Tafel zu den Positionen EINER Komponente. Leere Liste = nichts zu
        /// zeigen (keine betragstragende Investitionsposition).
        /// </summary>
        /// <param name="positionen">Die Zeilen des Rasters im Arbeitsstand.</param>
        /// <param name="komponentenId"><c>Tab_KostenKomponente.ID</c> der Technik.</param>
        /// <param name="komponente">Anzeigename der Technik (Summenzeile).</param>
        /// <param name="zinsProzent">i [%] aus den Wirtschaftlichkeitsparametern.</param>
        /// <param name="jahre">T [a] aus den Wirtschaftlichkeitsparametern.</param>
        /// <param name="preisstInvestProzent">p_I [%/a] — das WIRKSAME p_I der Gruppe.</param>
        internal static IList<Zeile> Zeilen(IEnumerable<Eingabe> positionen, int komponentenId,
                                            string komponente, double zinsProzent, int jahre,
                                            double preisstInvestProzent)
        {
            var liste = new List<Zeile>();
            if (positionen == null || jahre <= 0) return liste;

            CultureInfo k = CultureInfo.CurrentCulture;
            int T = Math.Max(1, jahre);

            double summeBetrag = 0, summeRestwert = 0, summeRestwertBw = 0;
            double summeErsatz = 0, summeErsatzBw = 0;
            int mitDauer = 0, alle = 0;
            var summeJahre = new List<int>();

            foreach (Eingabe e in positionen)
            {
                if (e == null || e.IstErloes) continue;
                if (e.Betrag == 0) continue;           // wie im Rechenkern: 0 zählt nicht

                alle++;
                summeBetrag += e.Betrag;

                KapitalwertRechner.Ersatzbild b = KapitalwertRechner.Ersatz(
                    new KapitalwertRechner.InvestPosition
                    {
                        Betrag = e.Betrag,
                        Nutzungsdauer = e.Nutzungsdauer ?? 0,
                        StartJahr = e.StartJahr
                    },
                    T, preisstInvestProzent);

                if (!b.OhneDauer) mitDauer++;

                double ersatzNominal = 0, ersatzBarwert = 0;
                for (int n = 0; n < b.Ersatzjahre.Count; n++)
                {
                    ersatzNominal += b.Ersatzbetraege[n];
                    ersatzBarwert += KapitalwertRechner.Barwert(
                        b.Ersatzbetraege[n], b.Ersatzjahre[n], zinsProzent);
                    if (!summeJahre.Contains(b.Ersatzjahre[n])) summeJahre.Add(b.Ersatzjahre[n]);
                }
                double restwertBarwert = KapitalwertRechner.Barwert(b.Restwert, T, zinsProzent);

                summeErsatz += ersatzNominal;
                summeErsatzBw += ersatzBarwert;
                summeRestwert += b.Restwert;
                summeRestwertBw += restwertBarwert;

                liste.Add(new Zeile
                {
                    Position = e.Bezeichnung ?? "",
                    Betrag = Geld(e.Betrag, k),
                    Dauer = b.OhneDauer
                        ? MyResource.Resource.ND_TAFEL_STRICH
                        : string.Format(k, MyResource.Resource.ND_TAFEL_DAUER,
                                        b.Nutzungsdauer.ToString("0.#", k)),
                    Ersatz = ErsatzText(b, T, k),
                    ErsatzBarwert = ersatzNominal == 0 ? "" : Barwert(ersatzBarwert, k),
                    Restwert = b.OhneDauer ? MyResource.Resource.ND_TAFEL_STRICH
                                           : Geld(b.Restwert, k),
                    RestwertBarwert = b.Restwert == 0 ? "" : Barwert(restwertBarwert, k),
                    Herkunft = NutzungsdauerCtrl.Herkunft(komponentenId, e.NutzungsdauerId,
                                                          e.Nutzungsdauer)
                });
            }

            if (liste.Count == 0) return liste;

            summeJahre.Sort();
            liste.Add(new Zeile
            {
                Position = komponente ?? "",
                IstSumme = true,
                Betrag = Geld(summeBetrag, k),
                Dauer = string.Format(k, MyResource.Resource.ND_TAFEL_VON, mitDauer, alle),
                Ersatz = summeErsatz == 0
                    ? MyResource.Resource.ND_TAFEL_STRICH
                    : JahreText(summeJahre, k) + " · " + Geld(summeErsatz, k),
                ErsatzBarwert = summeErsatz == 0 ? "" : Barwert(summeErsatzBw, k),
                Restwert = Geld(summeRestwert, k),
                RestwertBarwert = summeRestwert == 0 ? "" : Barwert(summeRestwertBw, k),
                Herkunft = VorgabeText(komponentenId, k)
            });
            return liste;
        }

        /// <summary>
        /// Der Ersatztext einer Position: die Jahre, sonst der GRUND, warum keines
        /// kommt — „läuft wie T" ohne gepflegte Dauer, „deckungsgleich mit T", wo n
        /// genau T ist, sonst der Gedankenstrich (n &gt; T).
        /// </summary>
        private static string ErsatzText(KapitalwertRechner.Ersatzbild b, int t, CultureInfo k)
        {
            if (b.Ausserhalb) return MyResource.Resource.ND_TAFEL_STRICH;
            if (b.Ersatzjahre.Count > 0) return JahreText(b.Ersatzjahre, k);
            if (b.OhneDauer) return MyResource.Resource.ND_TAFEL_WIE_T;
            if (Math.Abs(b.Nutzungsdauer - t) < 1e-9) return MyResource.Resource.ND_TAFEL_GLEICH_T;
            return MyResource.Resource.ND_TAFEL_STRICH;
        }

        private static string JahreText(IList<int> jahre, CultureInfo k)
        {
            var texte = new List<string>();
            foreach (int j in jahre) texte.Add(j.ToString(CultureInfo.InvariantCulture));
            return string.Format(k, MyResource.Resource.ND_TAFEL_JAHR,
                                 string.Join(", ", texte.ToArray()));
        }

        /// <summary>„Vorgabe der Technik 15 a" — leer, wo die Technik keine hat.</summary>
        private static string VorgabeText(int komponentenId, CultureInfo k)
        {
            NutzungsdauerZeile s = NutzungsdauerCtrl.Standard(komponentenId);
            if (s == null || !s.Nutzungsdauer.HasValue) return "";
            return string.Format(k, MyResource.Resource.ND_TAFEL_VORGABE,
                                 s.Nutzungsdauer.Value.ToString("0.#", k));
        }

        // =====================================================================
        //  Der Hinweis über der Tafel (Konzept Wirtschaftlichkeit § 2.13 (3))
        // =====================================================================

        /// <summary>
        /// Der Satz ÜBER der Tafel: Betrachtungszeitraum über der Vorgabe der Technik,
        /// und k von n Positionen ohne Nutzungsdauer. Leer, wenn beides nicht zutrifft
        /// — dann gibt es nichts zu prüfen.
        /// </summary>
        internal static string Hinweis(IEnumerable<Eingabe> positionen, int komponentenId,
                                       string komponente, int jahre)
        {
            if (positionen == null || jahre <= 0) return "";
            CultureInfo k = CultureInfo.CurrentCulture;

            int alle = 0;
            var ohne = new List<string>();
            foreach (Eingabe e in positionen)
            {
                if (e == null || e.IstErloes || e.Betrag == 0) continue;
                alle++;
                if (e.Nutzungsdauer >= 1.0) continue;
                ohne.Add(string.Format(k, MyResource.Resource.ND_TAFEL_OHNE_EINTRAG,
                                       e.Bezeichnung ?? "", Geld(e.Betrag, k)));
            }
            if (alle == 0) return "";

            var teile = new List<string>();

            NutzungsdauerZeile s = NutzungsdauerCtrl.Standard(komponentenId);
            if (s != null && s.Nutzungsdauer.HasValue && jahre > s.Nutzungsdauer.Value)
                teile.Add(string.Format(k, MyResource.Resource.ND_TAFEL_HINWEIS_T,
                                        jahre.ToString(CultureInfo.InvariantCulture),
                                        s.Nutzungsdauer.Value.ToString("0.#", k),
                                        string.IsNullOrEmpty(komponente) ? s.Technik : komponente));

            if (ohne.Count > 0)
                teile.Add(string.Format(k, MyResource.Resource.ND_TAFEL_HINWEIS_OHNE,
                                        ohne.Count, alle, string.Join(" · ", ohne.ToArray())));

            if (teile.Count == 0) return "";

            string satz = string.Join("; ", teile.ToArray()) + ".";
            return ohne.Count > 0 ? satz + " " + MyResource.Resource.ND_TAFEL_HINWEIS_SCHLUSS : satz;
        }

        // ----------------------------------------------------------------- intern ---

        private static string Geld(double wert, CultureInfo k)
        {
            return wert.ToString("#,##0.00", k);
        }

        private static string Barwert(double wert, CultureInfo k)
        {
            return string.Format(k, MyResource.Resource.ND_TAFEL_BARWERT,
                                 wert.ToString("#,##0", k));
        }
    }
}
