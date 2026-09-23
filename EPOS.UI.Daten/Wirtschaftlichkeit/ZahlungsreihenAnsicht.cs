using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E8a (Konzept Wirtschaftlichkeit § 2.11.4 V‑C, Mockup Kategorie 8) — die
    /// <b>Tafeln der Zahlungsreihen</b> der Wirtschaftlichkeitsseite, fertig formatiert aus
    /// den Gliederungen des Kerns (<see cref="Zahlungsgliederungen"/>): ValERI-Block 2
    /// „Zahlungsreihen" je Stand und Szenario.
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
        internal static List<ZahlungsreihenTafel> Jahrestafeln(Zahlungsgliederungen satz,
                                                               IList<KeyValuePair<int, string>> staende,
                                                               IList<string> szenarien, CultureInfo kultur)
        {
            var tafeln = new List<ZahlungsreihenTafel>();
            if (satz == null || staende == null || szenarien == null) return tafeln;
            foreach (KeyValuePair<int, string> stand in staende)
                for (int nummer = 0; nummer < szenarien.Count; nummer++)
                {
                    Zahlungsgliederung g = satz.Von(stand.Key, szenarien[nummer]);
                    if (g == null) continue;
                    tafeln.Add(new ZahlungsreihenTafel
                    {
                        IdStand = stand.Key,
                        Szenario = nummer,
                        Tafel = Jahrestafel(g, kultur),
                        Unterzeile = string.Format(kultur, MyResource.Resource.WIRT_ZR_HINWEIS,
                                                   g.ZinsProzent.ToString("N2", kultur),
                                                   g.SummeBarwerte.ToString(GELD, kultur))
                    });
                }
            return tafeln;
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
            foreach (string schluessel in Zahlungsgliederung.Reihenfolge)
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
                    zellen.Add(differenz.Bestandteil(schluessel).Barwert.ToString(GELD, kultur));
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
            foreach (string s in Zahlungsgliederung.Reihenfolge) spalten.Add(Zahlungsgliederung.Titel(s));
            spalten.Add(MyResource.Resource.WIRT_MJ_NETTO);
            spalten.Add(MyResource.Resource.WIRT_MJ_BARWERT);

            double[] netto = g.NettoJeJahr();
            double[] barwert = g.BarwertJeJahr();
            var zeilen = new List<MatrixZeile>();
            for (int t = 0; t <= g.Jahre; t++)
            {
                var zellen = new List<string>();
                foreach (string s in Zahlungsgliederung.Reihenfolge) zellen.Add(Jahr(g.Bestandteil(s).Wert(t), kultur));
                zellen.Add(Jahr(netto[t], kultur));
                zellen.Add(Jahr(barwert[t], kultur));
                zeilen.Add(new MatrixZeile { Titel = t.ToString(CultureInfo.InvariantCulture), Zellen = zellen });
            }

            // Die Summe nominal: je Bestandteil, und das Netto über alle Jahre.
            var summe = new List<string>();
            double nettoSumme = 0;
            foreach (double w in netto) nettoSumme += w;
            foreach (string s in Zahlungsgliederung.Reihenfolge) summe.Add(g.Bestandteil(s).Nominal.ToString(GELD, kultur));
            summe.Add(nettoSumme.ToString(GELD, kultur));
            summe.Add("");
            zeilen.Add(new MatrixZeile { Titel = MyResource.Resource.WIRT_ZR_SUMME, Zellen = summe, IstSumme = true });

            // Die Barwerte: je Bestandteil — dieselben Zahlen wie die Gliederung — und die
            // Summe der Barwertspalte, der Nettobarwert.
            var barwerte = new List<string>();
            foreach (string s in Zahlungsgliederung.Reihenfolge) barwerte.Add(g.Bestandteil(s).Barwert.ToString(GELD, kultur));
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
