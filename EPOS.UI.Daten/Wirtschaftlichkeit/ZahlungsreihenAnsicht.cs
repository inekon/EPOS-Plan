using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E8a (Konzept Wirtschaftlichkeit § 2.11.4 V‑C, Mockup Kategorie 8) — die
    /// <b>Tafeln der Zahlungsreihen</b> der Wirtschaftlichkeitsseite, fertig formatiert aus
    /// den Gliederungen des Kerns (<see cref="Zahlungsgliederungen"/>): ValERI-Block 2
    /// „Zahlungsreihen" je Stand und Szenario samt Zahlungsstrombild (U42), die Gliederung
    /// des Kapitalwerts mit Nominalsumme und Differenzspalte (U46), das Brückenbild (U41) und
    /// die Tafel „Was daraus im Lauf wird" (U47).
    ///
    /// <para><b>Gerechnet wird hier nichts.</b> Jede Zahl ist ein Bestandteil, ein Barwert
    /// oder eine Nominalsumme der Gliederung — die Hülle ordnet und formatiert nur, die
    /// Seite zeichnet. Ohne Gliederung (kein Lauf in dieser Sitzung) bleibt jede Tafel
    /// leer, und die Seite sagt es.</para>
    /// </summary>
    internal static class ZahlungsreihenAnsicht
    {
        /// <summary>Beträge mit Vorzeichen — Barwerte, Summen, Differenzen („+1.234", „−567").</summary>
        internal const string GELD = "+#,##0;−#,##0;0";

        /// <summary>Beträge eines Jahres — ohne Pluszeichen, 0 als Strich („keine Zahlung").</summary>
        private const string JAHR = "#,##0;−#,##0";

        /// <summary>
        /// Block 2: je Stand (mit Gliederung) und Szenario eine Tafel — Jahre als Zeilen, die
        /// sechs Bestandteile, Netto und Barwert als Spalten, darunter die Summe nominal und
        /// die Barwerte.
        /// </summary>
        /// <param name="satz">Die Gliederungen des Laufs; <c>null</c> = keine Tafeln.</param>
        /// <param name="staende">Die gezeigten Stände (Id, Name) in Gruppenreihenfolge.</param>
        /// <param name="szenarien">Die Persistenzwerte der Szenarien; der Index ist die Nummer
        /// der Szenario-Klappliste.</param>
        /// <param name="kultur">Die Kultur der Zahlen.</param>
        /// <param name="szenarionamen">U42: die Anzeigenamen der Szenarien in derselben
        /// Reihenfolge — sie stehen in der Unterzeile des Zahlungsstrombilds; <c>null</c> = die
        /// Persistenzwerte.</param>
        internal static List<ZahlungsreihenTafel> Jahrestafeln(Zahlungsgliederungen satz,
                                                               IList<KeyValuePair<int, string>> staende,
                                                               IList<string> szenarien, CultureInfo kultur,
                                                               IList<string> szenarionamen = null)
        {
            var tafeln = new List<ZahlungsreihenTafel>();
            if (satz == null || staende == null || szenarien == null) return tafeln;
            foreach (KeyValuePair<int, string> stand in staende)
                for (int nummer = 0; nummer < szenarien.Count; nummer++)
                {
                    Zahlungsgliederung g = satz.Von(stand.Key, szenarien[nummer]);
                    if (g == null) continue;
                    string szenarioname = szenarionamen != null && nummer < szenarionamen.Count
                        ? szenarionamen[nummer] : szenarien[nummer];
                    tafeln.Add(new ZahlungsreihenTafel
                    {
                        IdStand = stand.Key,
                        Szenario = nummer,
                        Tafel = Jahrestafel(g, kultur),
                        Unterzeile = string.Format(kultur, MyResource.Resource.WIRT_ZR_HINWEIS,
                                                   g.ZinsProzent.ToString("N2", kultur),
                                                   g.SummeBarwerte.ToString(GELD, kultur)),
                        Bild = Zahlungsstrom(satz, stand.Key, szenarien[nummer], stand.Value, szenarioname, kultur)
                    });
                }
            return tafeln;
        }

        /// <summary>
        /// U42 (Anwenderentscheid E8a‑Q1, Lesart a): das <b>Zahlungsstrombild</b> eines Standes
        /// in einem Szenario — die Positionen der Mehrjahrestafel aus demselben Lauf wie die
        /// Tafel darüber, gezeichnet vom Renderer des Kerns. <c>null</c> ohne Positionen; ein
        /// Zeichenfehler lässt das Bild aus, nie die Tafel.
        /// </summary>
        internal static Zeichenmodell Zahlungsstrom(Zahlungsgliederungen satz, int idStand, string szenario,
                                                    string name, string szenarioname, CultureInfo kultur)
        {
            Mehrjahresbild posten = satz == null ? null : satz.Posten(idStand, szenario);
            if (posten == null) return null;
            try
            {
                return ChartRenderer.ZahlungsstromModell(
                    ChartRenderer.Zahlungsstromreihe.Aus(posten),
                    ChartRenderer.Zahlungsstromreihe.Ersatzjahre(posten),
                    ChartRenderer.ZahlungsstromTexte.Fuer(name ?? "", szenarioname ?? "", kultur));
            }
            catch { return null; }
        }

        /// <summary>
        /// U46 (Mockup „Woraus entsteht die Zahl?"): die <b>Gliederung des Kapitalwerts</b> in
        /// einem Szenario — je Bestandteil und Stand der Barwert, darunter die Nominalsumme,
        /// als letzte Spalte die Differenz Leitversion − Referenz, als letzte Zeile der
        /// Nettobarwert. Die Differenzspalte ist Bestandteil für Bestandteil die Differenz der
        /// Barwerte und geht in der Kapitalwertdifferenz auf.
        /// </summary>
        /// <param name="satz">Die Gliederungen des Laufs; <c>null</c> = leere Tafel.</param>
        /// <param name="szenario">Der Persistenzwert des gezeigten Szenarios.</param>
        /// <param name="staende">Die Spalten der Seite (Id, Name) in Gruppenreihenfolge.</param>
        /// <param name="idReferenz">Die wirksame Referenz.</param>
        /// <param name="leitversion">Die Leitversion (<see cref="Zahlungsgliederungen.Leitversion"/>).</param>
        /// <param name="kultur">Die Kultur der Zahlen.</param>
        internal static ErgebnisMatrix Bestandteile(Zahlungsgliederungen satz, string szenario,
                                                    IList<KeyValuePair<int, string>> staende,
                                                    int idReferenz, int leitversion, CultureInfo kultur)
        {
            var tafel = new ErgebnisMatrix();
            if (satz == null || staende == null) return tafel;

            var gliederungen = new List<Zahlungsgliederung>();
            bool irgendeine = false;
            foreach (KeyValuePair<int, string> s in staende)
            {
                Zahlungsgliederung g = satz.Von(s.Key, szenario);
                gliederungen.Add(g);
                if (g != null) irgendeine = true;
            }
            if (!irgendeine) return tafel;

            // Die Differenzspalte: nur mit Leitversion UND Referenz, beide mit Gliederung.
            Zahlungsgliederung differenz = leitversion != 0 && leitversion != idReferenz
                ? Zahlungsgliederung.Differenz(satz.Von(leitversion, szenario), satz.Von(idReferenz, szenario))
                : null;

            var spalten = new List<string> { MyResource.Resource.WIRT_GL_BESTANDTEIL };
            foreach (KeyValuePair<int, string> s in staende) spalten.Add(s.Value ?? "");
            if (differenz != null)
                spalten.Add(string.Format(kultur, MyResource.Resource.WIRT_GL_DIFFERENZ,
                                          Name(staende, leitversion), Name(staende, idReferenz)));

            var zeilen = new List<MatrixZeile>();
            // ETAPPE E15: mit einem Risikoabzug in einer Gliederung die Zeile „Risikoabzug".
            foreach (string schluessel in Zahlungsgliederung.SchluesselVon(gliederungen))
            {
                // Die Investition fließt im Jahr 0 — Barwert und Nominalsumme sind dieselbe
                // Zahl, die zweite Zeile entfällt (Mockup).
                bool mitNominal = !string.Equals(schluessel, Zahlungsgliederung.INVESTITION, StringComparison.Ordinal);
                var zellen = new List<string>();
                var unter = new List<string>();
                foreach (Zahlungsgliederung g in gliederungen)
                {
                    Zahlungsbestandteil b = g == null ? null : g.Bestandteil(schluessel);
                    zellen.Add(b == null ? "—" : b.Barwert.ToString(GELD, kultur));
                    unter.Add(b == null || !mitNominal ? ""
                              : string.Format(kultur, MyResource.Resource.WIRT_GL_NOMINAL,
                                              Math.Abs(b.Nominal).ToString("N0", kultur)));
                }
                if (differenz != null)
                {
                    Zahlungsbestandteil db = differenz.Bestandteil(schluessel);
                    zellen.Add((db == null ? 0.0 : db.Barwert).ToString(GELD, kultur));
                    unter.Add("");
                }
                zeilen.Add(new MatrixZeile
                {
                    Titel = Zahlungsgliederung.Titel(schluessel),
                    Kennzeichen = Unterschrift(schluessel),
                    Zellen = zellen,
                    Unterwerte = unter
                });
            }

            // Der Nettobarwert — der Kapitalwert des Bildes, in der Differenzspalte die
            // Kapitalwertdifferenz, in der die Spalte aufgeht.
            var netto = new List<string>();
            foreach (Zahlungsgliederung g in gliederungen) netto.Add(g == null ? "—" : g.Kapitalwert.ToString(GELD, kultur));
            if (differenz != null) netto.Add(differenz.Kapitalwert.ToString(GELD, kultur));
            zeilen.Add(new MatrixZeile
            {
                Titel = MyResource.Resource.WIRT_GL_NETTOBARWERT,
                Zellen = netto,
                IstSumme = true
            });

            tafel.Spalten = spalten;
            tafel.Zeilen = zeilen;
            return tafel;
        }

        /// <summary>
        /// U41 (Mockup „Von der Investition zur Kapitalwertdifferenz"): das <b>Brückenbild</b>
        /// der Leitversion gegen die Referenz im gezeigten Szenario — dieselben Schritte wie die
        /// Differenzspalte der Gliederung, gezeichnet vom Renderer des Kerns. <c>null</c> ohne
        /// Leitversion, wenn sie die Referenz ist, oder wenn eine der beiden Gliederungen fehlt.
        /// </summary>
        internal static Zeichenmodell Bruecke(Zahlungsgliederungen satz, string szenario, int leitversion,
                                              int idReferenz, IList<KeyValuePair<int, string>> staende,
                                              string szenarioname, CultureInfo kultur)
        {
            if (satz == null || staende == null || leitversion == 0 || leitversion == idReferenz) return null;
            Zahlungsgliederung stand = satz.Von(leitversion, szenario), referenz = satz.Von(idReferenz, szenario);
            if (stand == null || referenz == null) return null;
            ChartRenderer.BrueckenTexte texte = ChartRenderer.BrueckenTexte.Fuer(
                Name(staende, leitversion), Name(staende, idReferenz), szenarioname, stand, kultur);
            return ChartRenderer.KapitalwertBrueckeModell(ChartRenderer.Brueckenschritt.Aus(stand, referenz), texte);
        }

        /// <summary>
        /// U47 (Mockup „Was ist angenommen?"): die Tafel <b>„Was daraus im Lauf wird"</b> — je
        /// Szenario (Ungünstig · Erwartet · Günstig, die Reihenfolge der Annahmentafel) die
        /// Wirkung auf die Leitversion: die Investition I₀, die Jahre der fälligen
        /// Ersatzbeschaffungen und der Restwert am Ende (nominal). Die drei Spalten sind die
        /// Szenarioläufe der Bandbreite — dieselben Zahlungsbilder, aus denen deren
        /// Kapitalwertdifferenzen stammen. Leer ohne Leitversion oder ohne Gliederung.
        /// </summary>
        internal static ErgebnisMatrix Laufwirkung(Zahlungsgliederungen satz, int leitversion, string name,
                                                   CultureInfo kultur)
        {
            var tafel = new ErgebnisMatrix();
            if (satz == null || leitversion == 0) return tafel;

            var laeufe = new List<Zahlungsgliederung>();
            bool irgendeiner = false;
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                Zahlungsgliederung g = satz.Von(leitversion, s);
                laeufe.Add(g);
                if (g != null) irgendeiner = true;
            }
            if (!irgendeiner) return tafel;

            var investition = new List<string>();
            var ersatz = new List<string>();
            var restwert = new List<string>();
            foreach (Zahlungsgliederung g in laeufe)
            {
                if (g == null) { investition.Add("—"); ersatz.Add("—"); restwert.Add("—"); continue; }
                investition.Add((-g.Bestandteil(Zahlungsgliederung.INVESTITION).Wert(0)).ToString("N0", kultur));
                List<int> jahre = g.Bestandteil(Zahlungsgliederung.ERSATZ).Jahre();
                ersatz.Add(jahre.Count == 0
                    ? MyResource.Resource.WIRT_LW_KEINE
                    : string.Join(" · ", jahre.ConvertAll(t => t.ToString(CultureInfo.InvariantCulture)).ToArray()));
                restwert.Add(g.Bestandteil(Zahlungsgliederung.RESTWERT).Nominal.ToString("N0", kultur));
            }

            tafel.Spalten = new List<string>
            {
                string.Format(kultur, MyResource.Resource.WIRT_LW_KOPF, name ?? ""),
                MyResource.Resource.WIRT_SZEN_WORST,
                MyResource.Resource.WIRT_SZEN_ERWARTET,
                MyResource.Resource.WIRT_SZEN_BEST
            };
            tafel.Zeilen = new List<MatrixZeile>
            {
                new MatrixZeile { Titel = MyResource.Resource.WIRT_GL_INVESTITION, Zellen = investition },
                new MatrixZeile { Titel = MyResource.Resource.WIRT_LW_ERSATZJAHRE, Zellen = ersatz },
                new MatrixZeile { Titel = MyResource.Resource.WIRT_LW_RESTWERT, Zellen = restwert }
            };
            return tafel;
        }

        /// <summary>
        /// Die Zeile unter der Überschrift der Gliederung: Barwert und Nominalsumme, der Zins
        /// DIESES Szenarios und der Zeitraum. <c>""</c> ohne Gliederung.
        /// </summary>
        internal static string Unterzeile(Zahlungsgliederungen satz, string szenario,
                                          IList<KeyValuePair<int, string>> staende, CultureInfo kultur)
        {
            if (satz == null || staende == null) return "";
            foreach (KeyValuePair<int, string> s in staende)
            {
                Zahlungsgliederung g = satz.Von(s.Key, szenario);
                if (g != null)
                    return string.Format(kultur, MyResource.Resource.WIRT_GL_UNTER,
                                         g.ZinsProzent.ToString("N1", kultur),
                                         g.Jahre.ToString(CultureInfo.InvariantCulture));
            }
            return "";
        }

        /// <summary>Die leise Zeile unter dem Namen eines Bestandteils; <c>""</c> = keine.</summary>
        private static string Unterschrift(string schluessel)
        {
            switch (schluessel)
            {
                case Zahlungsgliederung.INVESTITION: return MyResource.Resource.WIRT_GL_INVESTITION_UNTER;
                case Zahlungsgliederung.ENERGIE: return MyResource.Resource.WIRT_GL_ENERGIE_UNTER;
                case Zahlungsgliederung.ERLOESE: return MyResource.Resource.WIRT_GL_ERLOESE_UNTER;
                default: return "";
            }
        }

        /// <summary>Der Name eines Standes aus der Spaltenliste; <c>""</c>, wenn er dort fehlt.</summary>
        private static string Name(IList<KeyValuePair<int, string>> staende, int id)
        {
            foreach (KeyValuePair<int, string> s in staende)
                if (s.Key == id) return s.Value ?? "";
            return "";
        }

        /// <summary>Die Stände, für die es mindestens eine Tafel gibt — die Auswahl in Block 2.</summary>
        internal static List<(int Id, string Text)> Staende(IEnumerable<ZahlungsreihenTafel> tafeln,
                                                            IList<KeyValuePair<int, string>> staende)
        {
            var ids = new HashSet<int>();
            if (tafeln != null) foreach (ZahlungsreihenTafel t in tafeln) ids.Add(t.IdStand);
            var liste = new List<(int, string)>();
            if (staende != null)
                foreach (KeyValuePair<int, string> s in staende)
                    if (ids.Contains(s.Key)) liste.Add((s.Key, s.Value ?? ""));
            return liste;
        }

        /// <summary>
        /// Der Hinweis, wenn die Reihen eines gezeigten Standes nicht zu den gespeicherten
        /// Ergebnissen passen (<see cref="Zahlungsgliederungen.Abweichend"/>); <c>""</c> sonst —
        /// auch ohne Lauf: Den Fall sagt die Seite selbst.
        /// </summary>
        internal static string Hinweis(Zahlungsgliederungen satz, IList<KeyValuePair<int, string>> staende,
                                       CultureInfo kultur)
        {
            if (satz == null || staende == null || satz.Abweichend.Count == 0) return "";
            var namen = new List<string>();
            foreach (KeyValuePair<int, string> s in staende)
                if (satz.Abweichend.Contains(s.Key) && !string.IsNullOrEmpty(s.Value) && !namen.Contains(s.Value))
                    namen.Add(s.Value);
            return namen.Count == 0 ? ""
                 : string.Format(kultur, MyResource.Resource.WIRT_ZR_ABWEICHEND, string.Join(", ", namen.ToArray()));
        }

        /// <summary>Die Tafel EINER Gliederung.</summary>
        private static ErgebnisMatrix Jahrestafel(Zahlungsgliederung g, CultureInfo kultur)
        {
            var spalten = new List<string> { MyResource.Resource.WIRT_MJ_JAHR };
            IReadOnlyList<string> schluessel = g.Schluessel;   // E15: mit Risikoabzug eine Spalte mehr
            foreach (string s in schluessel) spalten.Add(Zahlungsgliederung.Titel(s));
            spalten.Add(MyResource.Resource.WIRT_MJ_NETTO);
            spalten.Add(MyResource.Resource.WIRT_MJ_BARWERT);

            double[] netto = g.NettoJeJahr();
            double[] barwert = g.BarwertJeJahr();
            var zeilen = new List<MatrixZeile>();
            for (int t = 0; t <= g.Jahre; t++)
            {
                var zellen = new List<string>();
                foreach (string s in schluessel) zellen.Add(Jahr(g.Bestandteil(s).Wert(t), kultur));
                zellen.Add(Jahr(netto[t], kultur));
                zellen.Add(Jahr(barwert[t], kultur));
                zeilen.Add(new MatrixZeile { Titel = t.ToString(CultureInfo.InvariantCulture), Zellen = zellen });
            }

            // Die Summe nominal: je Bestandteil, und das Netto über alle Jahre.
            var summe = new List<string>();
            double nettoSumme = 0;
            foreach (double w in netto) nettoSumme += w;
            foreach (string s in schluessel) summe.Add(g.Bestandteil(s).Nominal.ToString(GELD, kultur));
            summe.Add(nettoSumme.ToString(GELD, kultur));
            summe.Add("");
            zeilen.Add(new MatrixZeile { Titel = MyResource.Resource.WIRT_ZR_SUMME, Zellen = summe, IstSumme = true });

            // Die Barwerte: je Bestandteil — dieselben Zahlen wie die Gliederung — und die
            // Summe der Barwertspalte, der Nettobarwert.
            var barwerte = new List<string>();
            foreach (string s in schluessel) barwerte.Add(g.Bestandteil(s).Barwert.ToString(GELD, kultur));
            barwerte.Add("");
            barwerte.Add(g.SummeBarwerte.ToString(GELD, kultur));
            zeilen.Add(new MatrixZeile { Titel = MyResource.Resource.WIRT_MJ_BARWERT, Zellen = barwerte, IstSumme = true });

            return new ErgebnisMatrix { Spalten = spalten, Zeilen = zeilen };
        }

        /// <summary>Ein Jahresbetrag: gerundet 0 ist „—" (keine Zahlung in diesem Jahr).</summary>
        private static string Jahr(double wert, CultureInfo kultur)
            => Math.Round(wert) == 0 ? "—" : wert.ToString(JAHR, kultur);
    }
}
