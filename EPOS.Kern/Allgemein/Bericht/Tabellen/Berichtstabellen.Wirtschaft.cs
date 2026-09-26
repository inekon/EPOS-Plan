using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RR = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Strukturtabellen der Wirtschaftlichkeit (Anhang A: <c>tabelle.wirtschaft.*</c>, <c>stand.tabelle.*</c>) —
    /// aus dem Wertesatz des Laufs (<see cref="WirtschaftsBerichtswerte"/>) und derselben Zeilendefinition
    /// (<see cref="WirtschaftlichkeitZeilen"/>), aus der der Baustein, die Seite und die Mappe lesen. Die Texte aus
    /// <c>MyResource</c> kommen wie im Baustein in der Sprache der Oberfläche und laufen nicht noch einmal durch
    /// <see cref="BerichtTexte.T(string, bool)"/>.
    /// </summary>
    public static partial class Berichtstabellen
    {
        // =====================================================================
        //  Kennzahltafel (tabelle.wirtschaft.kennzahlen, .guenstig, .unguenstig)
        // =====================================================================

        /// <summary>
        /// <b><c>tabelle.wirtschaft.kennzahlen</c></b> — die Kennzahltafel eines Szenarios: die sichtbaren Zeilen
        /// (<see cref="WirtschaftlichkeitZeilen.Sichtbare"/>) gegen die Referenz der Tafel, je Stand eine Spalte. In
        /// Sicht 1 Stamm und Varianten mit Blockteilung, in der Paarsicht genau A | B (unteilbar). Überschriften der
        /// Rubrik sind Gruppenzeilen, Summen Summenzeilen, die Referenzspalte trägt die Rolle Stamm, eine Zelle mit
        /// Vorbehalt die Rolle Warnung samt Satz (<see cref="Tabellenzelle.Warnung"/>).
        /// </summary>
        public static Berichtstabelle Wirtschaftskennzahlen(BerichtsDaten daten, WirtschaftsBerichtswerte werte, string szenario,
                                                            bool englisch, CultureInfo kultur)
        {
            List<WirtschaftlichkeitErgebnis> alle = werte?.Ergebnisse ?? new List<WirtschaftlichkeitErgebnis>();
            if (alle.Count == 0) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAMM), kultur);

            int idReferenz = werte.IdReferenzTafel;
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(werte.Zeilen(idReferenz), alle);
            int idRefSpalte = idReferenz > 0 ? idReferenz : stamm.IdProjekt;
            bool paar = daten.Sicht != null && daten.Sicht.IstPaar;

            var spalten = new List<VariantenDaten>();
            if (paar)
            {
                foreach (int id in daten.Sicht.Spalten(null))
                {
                    VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == id);
                    if (v != null) spalten.Add(v);
                }
            }
            else
            {
                spalten.Add(stamm);
                spalten.AddRange(daten.Varianten.Where(v => !v.IstStamm));
            }
            if (spalten.Count == 0) return Leer(nameof(RR.BV_GRUND_KEIN_PAAR), kultur);

            var t = new Berichtstabelle { Teilbar = !paar }.Feste(3100);
            foreach (VariantenDaten v in spalten)
                t.Spalte(!paar && v.IstStamm ? Spaltenart.Stamm : Spaltenart.Stand, 0, v.IdProjekt);
            t.MitKopf(new[] { Zellen.Kopf(BerichtTexte.T("Kennzahl", englisch), Tabellenausrichtung.Links) }
                .Concat(spalten.Select(v => Zellen.Kopf(Standkopf(v, englisch)))));

            foreach (WirtZeile z in zeilen)
            {
                Tabellenrolle zeilenrolle = z.IstUeberschrift ? Tabellenrolle.Gruppe
                                          : z.IstSumme ? Tabellenrolle.Summe : Tabellenrolle.Keine;
                string titel = (z.Einzug > 0 ? "    " : "") + z.Titel +
                               (z.Nachrichtlich ? " — " + ValeriAusweis.NachrichtlichLabel() : "");
                var zellen = new List<Tabellenzelle>
                {
                    new Tabellenzelle
                    {
                        Text = titel, Fett = z.IstUeberschrift || z.IstSumme, Rolle = zeilenrolle,
                        Hinterlegung = z.IstUeberschrift ? Tabellenhinterlegung.Kopf : Tabellenhinterlegung.Keine,
                    },
                };
                foreach (VariantenDaten v in spalten)
                {
                    WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x => x.IdProjekt == v.IdProjekt && x.Szenario == szenario);
                    string txt = z.IstUeberschrift ? "" : z.Anzeige(e, kultur);
                    string warnung = z.Warnung(e);
                    Tabellenrolle rolle = zeilenrolle | (v.IdProjekt == idRefSpalte ? Tabellenrolle.Stamm : Tabellenrolle.Keine);
                    string satz = null;
                    if (!string.IsNullOrEmpty(warnung))
                    {
                        rolle |= Tabellenrolle.Warnung;
                        satz = WirtschaftlichkeitBaustein.Zellwarnung(v, z, warnung);
                    }
                    zellen.Add(new Tabellenzelle
                    {
                        Text = txt,
                        Zahl = z.IstUeberschrift || z.IstText || txt == Tabellenzelle.STRICH ? null : z.ExcelWert(e),
                        Format = z.IstText ? null : z.Format,
                        Fett = z.IstSumme,
                        Rolle = rolle,
                        Warnung = satz,
                        Hinterlegung = z.IstUeberschrift ? Tabellenhinterlegung.Kopf
                                     : v.IdProjekt == idRefSpalte ? Tabellenhinterlegung.Stamm : Tabellenhinterlegung.Keine,
                        Ausrichtung = z.IstText ? Tabellenausrichtung.Links
                                    : txt == Tabellenzelle.STRICH ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                    });
                }
                t.Zeile(zellen, zeilenrolle);
            }
            foreach (Tabellenzeile z in t.Zeilen)
                foreach (Tabellenzelle c in z.Zellen)
                    if (c.Warnung != null && !t.Hinweise.Contains(c.Warnung)) t.Hinweis(c.Warnung);
            if (t.IstLeer) t.Leergrund = Grund(nameof(RR.BV_GRUND_ZEILE_FEHLT), kultur);
            return t;
        }

        // =====================================================================
        //  Szenarienübersicht (tabelle.wirtschaft.szenarien)
        // =====================================================================

        /// <summary>
        /// <b><c>tabelle.wirtschaft.szenarien</c></b> — die Szenarienübersicht aus der Bandbreite der Bewertung: die
        /// Referenzzeile (Rolle Stamm), je Version ΔKW in Ungünstig, Erwartet, Günstig, Spanne, Amortisation und
        /// Einstufung.
        /// </summary>
        public static Berichtstabelle Szenarien(WirtschaftlichkeitBewertung bewertung, bool englisch, CultureInfo kultur)
        {
            WirtschaftlichkeitBandbreite band = bewertung?.Bandbreite ?? new WirtschaftlichkeitBandbreite();
            if (band.Leer) return Leer(nameof(RR.BV_GRUND_NUR_STAMM), kultur);

            var t = new Berichtstabelle().Feste(2100, 0, 0, 0, 0, 0, 0);
            t.MitKopf(new[]
            {
                Zellen.Kopf(RR.WIRT_SZ_SP_VARIANTE, Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_SZ_SP_WORST),
                Zellen.Kopf(RR.WIRT_SZ_SP_ERWARTET),
                Zellen.Kopf(RR.WIRT_SZ_SP_BEST),
                Zellen.Kopf(RR.WIRT_SZ_SP_SPANNE),
                Zellen.Kopf(RR.WIRT_SZ_SP_AMORT),
                Zellen.Kopf(RR.WIRT_EMPF_SPALTE),
            });

            var referenz = new List<Tabellenzelle>
            {
                Zellen.Text(BerichtTexte.T(band.Referenzname, englisch), rolle: Tabellenrolle.Stamm, fett: true, h: Tabellenhinterlegung.Stamm),
            };
            for (int i = 1; i <= 4; i++)
                referenz.Add(Zellen.Text(RR.WIRT_ZEILE_STAMM_REFERENZ, Tabellenausrichtung.Mitte, Tabellenrolle.Stamm, h: Tabellenhinterlegung.Stamm));
            for (int i = 5; i <= 6; i++)
                referenz.Add(Zellen.Text(Tabellenzelle.STRICH, Tabellenausrichtung.Mitte, Tabellenrolle.Stamm, h: Tabellenhinterlegung.Stamm));
            t.Zeile(referenz);

            foreach (BandbreitenZeile z in band.Zeilen)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(z.Anzeige) };
                foreach (double? wert in new[] { z.Worst, z.Erwartet, z.Best })
                    zellen.Add(Zellen.Zahl(Tabellenformat.FW(wert, "N0", kultur), wert, "N0", einheit: "€"));
                zellen.Add(Zellen.Zahl(Tabellenformat.FW(z.Spanne, "N0", kultur), z.Spanne, "N0", einheit: "€"));
                zellen.Add(Zellen.Zahl(Tabellenformat.FW(z.AmortisationJahre, "N1", kultur), z.AmortisationJahre, "N1", einheit: "a"));
                VariantenEmpfehlung u = z.Urteil;
                zellen.Add(Zellen.Text(u == null ? Tabellenzelle.STRICH : u.StufeText,
                                       u == null ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Links));
                t.Zeile(zellen);
            }
            t.Hinweis(string.Format(kultur, RR.WIRT_SZ_DELTA_FUSS, band.Referenzname));
            return t;
        }

        // =====================================================================
        //  Nicht monetarisierbare Wirkungen (tabelle.wirtschaft.nicht_monetaer)
        // =====================================================================

        /// <summary>
        /// <b><c>tabelle.wirtschaft.nicht_monetaer</c></b> — je benannte Wirkung Kategorie, Beschreibung, Dauer, Wirkung
        /// auf Organisation, Mitarbeiter und Umwelt und Beurteilung (DIN EN 17463 6.1, 8.2).
        /// </summary>
        public static Berichtstabelle NichtMonetaer(IReadOnlyList<ProjektWirkung> wirkungen, CultureInfo kultur)
        {
            List<ProjektWirkung> zeilen = (wirkungen ?? new List<ProjektWirkung>())
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung)).ToList();
            if (zeilen.Count == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(1300, 0, 900, 1150, 1150, 1150, 1200);
            string[] kopf =
            {
                RR.WIRT_NM_SP_KATEGORIE, RR.WIRT_NM_SP_BESCHREIBUNG, RR.WIRT_NM_SP_DAUER, RR.WIRT_NM_SP_ORGANISATION,
                RR.WIRT_NM_SP_MITARBEITER, RR.WIRT_NM_SP_UMWELT, RR.WIRT_NM_SP_BEURTEILUNG
            };
            t.MitKopf(kopf.Select((k, i) => Zellen.Kopf(k, i == 1 ? Tabellenausrichtung.Links : Tabellenausrichtung.Mitte)));
            foreach (ProjektWirkung z in zeilen)
            {
                string[] werte =
                {
                    NichtMonetaereWirkungen.KategorieText(z.Kategorie), z.Beschreibung.Trim(),
                    NichtMonetaereWirkungen.DauerText(z.Dauer), NichtMonetaereWirkungen.WirkungText(z.WirkungOrganisation),
                    NichtMonetaereWirkungen.WirkungText(z.WirkungMitarbeiter), NichtMonetaereWirkungen.WirkungText(z.WirkungUmwelt),
                    NichtMonetaereWirkungen.BeurteilungText(z, kultur)
                };
                t.Zeile(werte.Select((w, i) => Zellen.Text(w, i == 1 ? Tabellenausrichtung.Links : Tabellenausrichtung.Mitte)));
            }
            return t;
        }

        // =====================================================================
        //  Je Stand (stand.tabelle.*)
        // =====================================================================

        /// <summary>
        /// <b><c>stand.tabelle.betriebskosten</c></b> — die Betriebskostenpositionen eines Stands nach Kostenart
        /// (Gruppenzeilen), je Position Gruppe, Bemessung, Herleitung und Betrag, dazu die Summe (Summenzeile). Die
        /// Probe gegen die angesetzten Betriebskosten steht als Hinweis unter der Tabelle.
        /// </summary>
        public static Berichtstabelle Betriebskosten(VariantenDaten v, List<WirtschaftlichkeitErgebnis> alle, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            WirtschaftlichkeitErgebnis e = (alle ?? new List<WirtschaftlichkeitErgebnis>()).FirstOrDefault(x =>
                x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.Betriebskosten != null && x.Betriebskosten.Count > 0 &&
                x.IdProjekt == v.IdProjekt);
            if (e == null) return Leer(alle == null || alle.Count == 0 ? nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT)
                                                                        : nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(2700, 1500, 1600, 2400, 0);
            t.MitKopf(new[]
            {
                Zellen.Kopf(RR.WIRT_BK_SP_POSITION, Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_BK_SP_GRUPPE, Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_BK_SP_BEMESSUNG, Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_BK_SP_HERLEITUNG, Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_BK_SP_BETRAG),
            });

            double summe = 0, summeErstesJahr = 0;
            foreach (string art in WirtschaftlichkeitZeilen.Kostenarten)
            {
                List<KostenPositionNachweis> block = e.Betriebskosten
                    .Where(x => string.Equals(x.Kostenart ?? "", art, StringComparison.Ordinal)).ToList();
                if (block.Count == 0) continue;

                var gz = new List<Tabellenzelle>
                {
                    Zellen.Text(WirtschaftlichkeitZeilen.KostenartText(art), rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Stamm),
                };
                for (int i = 1; i < 5; i++) gz.Add(Zellen.Text("", rolle: Tabellenrolle.Gruppe, fett: true, h: Tabellenhinterlegung.Stamm));
                t.Zeile(gz, Tabellenrolle.Gruppe);

                foreach (KostenPositionNachweis n in block)
                {
                    t.Zeile(new[]
                    {
                        Zellen.Text(n.Bezeichnung),
                        Zellen.Text(n.Gruppe),
                        Zellen.Text(WirtschaftlichkeitZeilen.BemessungText(n.Bemessung, n.Komponente)),
                        Zellen.Text(WirtschaftlichkeitZeilen.HerleitungZeile(n, kultur)),
                        new Tabellenzelle { Text = Tabellenformat.F(n.BetragJahr, 0, kultur), Zahl = n.BetragJahr, Format = "N0",
                                            Einheit = "€", Ausrichtung = Tabellenausrichtung.Rechts },
                    });
                    summe += n.BetragJahr;
                    if (WirtschaftlichkeitZeilen.LaeuftImErstenJahr(n)) summeErstesJahr += n.BetragJahr;
                }
            }

            var sz = new List<Tabellenzelle>
            {
                Zellen.Text(RR.WIRT_BK_SUMME, rolle: Tabellenrolle.Summe, fett: true, h: Tabellenhinterlegung.Kopf),
            };
            for (int i = 1; i < 4; i++) sz.Add(Zellen.Text("", rolle: Tabellenrolle.Summe, fett: true, h: Tabellenhinterlegung.Kopf));
            sz.Add(new Tabellenzelle
            {
                Text = Tabellenformat.F(summe, 0, kultur), Zahl = summe, Format = "N0", Einheit = "€",
                Ausrichtung = Tabellenausrichtung.Rechts, Fett = true, Hinterlegung = Tabellenhinterlegung.Kopf, Rolle = Tabellenrolle.Summe,
            });
            t.Zeile(sz, Tabellenrolle.Summe);
            t.Hinweis(WirtschaftlichkeitZeilen.GliederungAbweichung(summeErstesJahr, e.BetriebskostenJahr, kultur));
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.kwkg_module</c></b> — je BHKW-Modul der KWKG-Rechnung eine Zeile mit elf Spalten (mit dem
        /// zweiten Fall des § 2 Nr. 16 KWKG sechzehn); die Herleitung der Sätze steht als Hinweis darunter.
        /// </summary>
        public static Berichtstabelle KwkgModule(VariantenDaten v, List<WirtschaftlichkeitErgebnis> alle, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            WirtschaftlichkeitErgebnis e = (alle ?? new List<WirtschaftlichkeitErgebnis>()).FirstOrDefault(x =>
                x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.KwkgModule != null && x.KwkgModule.Count > 0 &&
                x.IdProjekt == v.IdProjekt);
            if (e == null) return Leer(alle == null || alle.Count == 0 ? nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT)
                                                                        : nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            bool mitFall2 = KwkgFall2Spalten.Noetig(e.KwkgModule);
            int spalten = mitFall2 ? 16 : 11;
            var t = new Berichtstabelle { Schmal = true }.Feste(mitFall2 ? 1255 : 1655);
            for (int i = 1; i < spalten; i++) t.Spalte(Spaltenart.Fest, 0);

            var kopfTexte = new List<string>
            {
                RR.WIRT_KWKG_SP_MODUL, RR.WIRT_KWKG_SP_PEL, RR.WIRT_KWKG_SP_VBH, RR.WIRT_KWKG_SP_SATZ_EIGEN,
                RR.WIRT_KWKG_SP_SATZ_EINSP, RR.WIRT_KWKG_SP_SATZQUELLE, RR.WIRT_KWKG_SP_DECKEL, RR.WIRT_KWKG_SP_KONTINGENT,
                RR.WIRT_KWKG_SP_BEGINN, RR.WIRT_KWKG_SP_JAHR1, RR.WIRT_KWKG_SP_ERSCHOEPFT
            };
            if (mitFall2) kopfTexte.AddRange(KwkgFall2Spalten.Kopf());
            t.MitKopf(kopfTexte.Select((k, i) => Zellen.Kopf(k, i == 0 ? Tabellenausrichtung.Links : Tabellenausrichtung.Mitte)));

            foreach (KwkgModulNachweis m in e.KwkgModule)
            {
                var werte = new List<string>
                {
                    m.Bezeichner,
                    Tabellenformat.F(m.PelKW, 0, kultur),
                    Tabellenformat.F(m.VbhElektrisch, 0, kultur),
                    Tabellenformat.F(m.SatzEigenCt, KwkgSatzHerkunft.NACHKOMMASTELLEN, kultur),
                    Tabellenformat.F(m.SatzEinspeisungCt, KwkgSatzHerkunft.NACHKOMMASTELLEN, kultur),
                    m.SatzAusAnlage ? RR.WIRT_KWKG_SATZ_QUELLE_ANLAGE : RR.WIRT_KWKG_SATZ_QUELLE_PROJEKT,
                    m.JahresdeckelH > 0 ? Tabellenformat.F(m.JahresdeckelH, 0, kultur) : RR.WIRT_KWKG_DECKEL_STAFFEL,
                    Tabellenformat.F(m.KontingentH, 0, kultur),
                    m.Foerderbeginn.ToString(CultureInfo.InvariantCulture),
                    Tabellenformat.F(m.Jahr1Eur, 0, kultur),
                    m.ErschoepftAbJahr > 0 ? m.ErschoepftAbJahr.ToString(CultureInfo.InvariantCulture) : RR.WIRT_KWKG_ERSCHOEPFT_NIE
                };
                if (mitFall2) werte.AddRange(KwkgFall2Spalten.Werte(m, kultur));
                t.Zeile(werte.Select((w, i) => Zellen.Text(w, i == 0 ? Tabellenausrichtung.Links : Tabellenausrichtung.Rechts)));
            }
            foreach (KwkgModulNachweis m in e.KwkgModule)
                if (m.HerleitungEigen.Length > 0 || m.HerleitungEinspeisung.Length > 0)
                    t.Hinweis(string.Format(RR.WIRT_KWKG_HERLEITUNG_ZEILE, m.Bezeichner, m.HerleitungEigen, m.HerleitungEinspeisung));
            return t;
        }

        /// <summary>
        /// Die Mehrjahrestafel eines Stands aus seinem <see cref="Mehrjahresbild"/>: Jahre als Zeilen, Positionen als
        /// Spalten (Summenspalten mit der Rolle Summe), die Abschlusszeile „Restwert im Jahr T“ als Summenzeile.
        /// </summary>
        public static Berichtstabelle Mehrjahrestafel(Mehrjahresbild bild, CultureInfo kultur)
        {
            if (bild == null || bild.Spalten.Count == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle { Schmal = true }.Feste(620);
            foreach (MehrjahresSpalte _ in bild.Spalten) t.Spalte(Spaltenart.Fest, 0);
            t.MitKopf(new[] { Zellen.Kopf(RR.WIRT_MJ_JAHR, Tabellenausrichtung.Links) }
                .Concat(bild.Spalten.Select(s => Zellen.Kopf(s.Titel))));

            for (int jahr = 0; jahr <= bild.Jahre; jahr++)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(jahr.ToString(CultureInfo.InvariantCulture)) };
                foreach (MehrjahresSpalte s in bild.Spalten)
                {
                    double wert = s.Wert(jahr);
                    zellen.Add(new Tabellenzelle
                    {
                        Text = wert == 0 ? Tabellenzelle.STRICH : Tabellenformat.F(wert, 0, kultur),
                        Zahl = wert == 0 ? (double?)null : wert, Format = "N0", Einheit = "€",
                        Ausrichtung = wert == 0 ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                        Rolle = s.IstSumme ? Tabellenrolle.Summe : Tabellenrolle.Keine,
                        Hinterlegung = s.IstSumme ? Tabellenhinterlegung.Stamm : Tabellenhinterlegung.Keine,
                    });
                }
                t.Zeile(zellen);
            }

            var abschluss = new List<Tabellenzelle> { Zellen.Text(RR.WIRT_MJ_RESTWERT_T, rolle: Tabellenrolle.Summe, fett: true) };
            foreach (MehrjahresSpalte s in bild.Spalten)
            {
                double? wert = s.Schluessel == "BARWERT" ? bild.RestwertBarwert
                             : s.Schluessel == "KUMULIERT" ? bild.Kapitalwert : (double?)null;
                string txt = wert.HasValue ? Tabellenformat.F(wert.Value, 0, kultur) : Tabellenzelle.STRICH;
                abschluss.Add(new Tabellenzelle
                {
                    Text = txt, Zahl = wert, Format = "N0", Einheit = "€", Fett = true, Rolle = Tabellenrolle.Summe,
                    Hinterlegung = Tabellenhinterlegung.Stamm,
                    Ausrichtung = txt == Tabellenzelle.STRICH ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                });
            }
            t.Zeile(abschluss, Tabellenrolle.Summe);
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.mehrjahres</c></b> — die Mehrjahrestafel eines Stands im Erwartungsfall aus dem
        /// Kapitalwertverlauf des Laufs; ohne Verlauf leer mit Grund.
        /// </summary>
        public static Berichtstabelle Mehrjahres(VariantenDaten v, WirtschaftsBerichtswerte werte, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            if (werte == null || werte.Ergebnisse.Count == 0) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            if (werte.VerlaufEntfaellt) return Leer(nameof(RR.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            WirtschaftlichkeitVerlauf verlauf = werte.Verlauf?.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            VerlaufSerie serie = verlauf?.Absolut.FirstOrDefault(s => s.IdProjekt == v.IdProjekt);
            Mehrjahresbild bild = Mehrjahresbild.Baue(serie);
            if (bild == null)
            {
                var leer = new Berichtstabelle
                {
                    Leergrund = RR.WIRT_MJ_ENTFAELLT + (serie != null && serie.Fehlgrund != null ? " (" + serie.Fehlgrund + ")" : ""),
                };
                return leer;
            }
            return Mehrjahrestafel(bild, kultur);
        }

        /// <summary>
        /// <b><c>stand.tabelle.vermiedene_kosten</c></b> — der Nachweis der vermiedenen Kosten (Arbeit, Leistung, gesamt)
        /// im Erwartungsfall; leer, wenn der Stand keine vermiedenen Kosten trägt.
        /// </summary>
        public static Berichtstabelle VermiedeneKosten(VariantenDaten v, List<WirtschaftlichkeitErgebnis> alle, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            return VermiedeneKosten(v.IdProjekt, alle, kultur);
        }

        /// <summary>Die Nachweistafel der vermiedenen Kosten zu einer Projektkennung (der Weg des Bausteins).</summary>
        public static Berichtstabelle VermiedeneKosten(int idProjekt, List<WirtschaftlichkeitErgebnis> alle, CultureInfo kultur)
        {
            WirtschaftlichkeitErgebnis e = (alle ?? new List<WirtschaftlichkeitErgebnis>()).FirstOrDefault(x =>
                x.IdProjekt == idProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            if (e == null) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            if (e.VermiedenGesamtJahr == 0 && e.VermiedenArbeitJahr == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(5200, 0);
            foreach ((string label, double wert) in new[]
            {
                (RR.WIRT_ZEILE_VERMIEDEN_ARBEIT, e.VermiedenArbeitJahr),
                (RR.WIRT_ZEILE_VERMIEDEN_LEISTUNG, e.VermiedenLeistungJahr),
                (RR.WIRT_ZEILE_VERMIEDEN_GESAMT, e.VermiedenGesamtJahr),
            })
                t.Zeile(new[]
                {
                    Zellen.Text(label),
                    new Tabellenzelle { Text = Tabellenformat.F(wert, 0, kultur), Zahl = wert, Format = "N0", Einheit = "€",
                                        Ausrichtung = Tabellenausrichtung.Rechts },
                });
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.sensitivitaet</c></b> — die Sensitivitätszeilen eines Stands: Kapitalwert bei −Δ, Basis
        /// (Rolle Stamm), +Δ und die Steigung.
        /// </summary>
        public static Berichtstabelle Sensitivitaet(VariantenDaten v, IEnumerable<SensitivitaetZeile> sens, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            List<SensitivitaetZeile> zeilen = (sens ?? Enumerable.Empty<SensitivitaetZeile>()).Where(x => x.IdProjekt == v.IdProjekt).ToList();
            if (zeilen.Count == 0) return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(3300, 0, 0, 0, 0);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Parameter", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("KW bei −Δ [€]", englisch)),
                Zellen.Kopf(BerichtTexte.T("KW Basis [€]", englisch)),
                Zellen.Kopf(BerichtTexte.T("KW bei +Δ [€]", englisch)),
                Zellen.Kopf(RR.WIRT_SENS_SP_STEIGUNG),
            });
            foreach (SensitivitaetZeile z in zeilen)
            {
                var zellen = new List<Tabellenzelle> { Zellen.Text(z.Parameter) };
                double?[] werte = { z.KwMinus, z.KwBasis, z.KwPlus };
                for (int i = 0; i < 3; i++)
                    zellen.Add(Zellen.Zahl(Tabellenformat.FW(werte[i], "N0", kultur), werte[i], "N0",
                                           i == 1 ? Tabellenrolle.Stamm : Tabellenrolle.Keine,
                                           h: i == 1 ? Tabellenhinterlegung.Stamm : Tabellenhinterlegung.Keine, einheit: "€"));
                string steigung = z.Steigung.HasValue ? Tabellenformat.FW(z.Steigung, "N2", kultur) + " " + z.SteigungEinheit : Tabellenzelle.STRICH;
                zellen.Add(Zellen.Zahl(steigung, z.Steigung, "N2", einheit: z.SteigungEinheit));
                t.Zeile(zellen);
            }
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.strommengen</c></b> — die Strommengen eines Stands in der Jahreszeile: Bedarf ohne Anlage,
        /// Netzbezug, PV-Einspeisung, KWK-Eigenstrom und KWK-Einspeisung in MWh.
        /// </summary>
        public static Berichtstabelle Strommengen(VariantenDaten v, Dictionary<int, StromMatrix> matrizen, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            if (matrizen == null || !matrizen.TryGetValue(v.IdProjekt, out StromMatrix m) || m == null)
                return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(2000, 0, 0, 0, 0, 0);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T(RR.WIRT_MATRIX_ZEITRAUM, englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(RR.WIRT_MATRIX_BEDARF),
                Zellen.Kopf(BerichtTexte.T("Netzbezug [MWh]", englisch)),
                Zellen.Kopf(BerichtTexte.T("PV-Einspeisung [MWh]", englisch)),
                Zellen.Kopf(BerichtTexte.T("KWK-Eigenstrom [MWh]", englisch)),
                Zellen.Kopf(BerichtTexte.T("KWK-Einspeisung [MWh]", englisch)),
            });
            Func<double, Tabellenzelle> mwh = x => new Tabellenzelle
            {
                Text = Tabellenformat.F(x, 1, kultur), Zahl = x, Format = "N1", Einheit = "MWh", Ausrichtung = Tabellenausrichtung.Rechts,
            };
            t.Zeile(new[]
            {
                Zellen.Text(RR.WIRT_MATRIX_JAHR),
                mwh(m.BedarfGesamtMWh), mwh(m.BezugGesamtMWh), mwh(m.EinspeisungPvGesamtMWh),
                mwh(m.KwkEigenGesamtMWh), mwh(m.KwkEinspeisungGesamtMWh),
            });
            return t;
        }

        /// <summary>
        /// Die Emissionsbilanz eines Stands: CO₂ (im Modus des Laufs), SO₂ und NOx gekoppelt, getrennt und die
        /// Vermeidung. Leer, wenn die Bilanz keinen CO₂-Wert trägt.
        /// </summary>
        public static Berichtstabelle Emissionsbilanz(EmissionsBilanz b, bool englisch, CultureInfo kultur)
        {
            if (b == null || (!b.CO2GekoppeltT.HasValue && !b.CO2GetrenntT.HasValue))
                return Leer(nameof(RR.BV_GRUND_TABELLE_LEER), kultur);

            var t = new Berichtstabelle().Feste(2800, 0, 0, 0);
            t.MitKopf(new[]
            {
                Zellen.Kopf(BerichtTexte.T("Schadstoff", englisch), Tabellenausrichtung.Links),
                Zellen.Kopf(BerichtTexte.T("Gekoppelt (System)", englisch)),
                Zellen.Kopf(BerichtTexte.T("Getrennt (Referenz)", englisch)),
                Zellen.Kopf(BerichtTexte.T("Vermeidung", englisch)),
            });
            Action<string, double?, double?> zeile = (label, gek, getr) =>
            {
                double? diff = gek.HasValue && getr.HasValue ? getr.Value - gek.Value : (double?)null;
                string d = diff.HasValue ? Tabellenformat.F(diff.Value, 1, kultur) : Tabellenzelle.STRICH;
                t.Zeile(new[]
                {
                    Zellen.Text(label),
                    new Tabellenzelle { Text = Tabellenformat.FW(gek, "N1", kultur), Zahl = gek, Format = "N1", Ausrichtung = Tabellenausrichtung.Rechts },
                    new Tabellenzelle { Text = Tabellenformat.FW(getr, "N1", kultur), Zahl = getr, Format = "N1", Ausrichtung = Tabellenausrichtung.Rechts },
                    Zellen.Zahl(d, diff, "N1"),
                });
            };
            zeile(EmissionsAusweis.BilanzZeile(b.Modus), b.CO2GekoppeltT, b.CO2GetrenntT);
            zeile("SO₂ [kg/a]", b.SO2GekoppeltKg, b.SO2GetrenntKg);
            zeile("NOx [kg/a]", b.NOxGekoppeltKg, b.NOxGetrenntKg);
            return t;
        }

        /// <summary>
        /// <b><c>stand.tabelle.emissionsbilanz</c></b> — die Emissionsbilanz eines Stands aus dem Wertesatz; nur mit
        /// Kraftwerkspark und bei aktuellem Ergebnis der Wirtschaftlichkeit.
        /// </summary>
        public static Berichtstabelle Emissionsbilanz(VariantenDaten v, WirtschaftsBerichtswerte werte, bool englisch, CultureInfo kultur)
        {
            if (v == null) return Leer(nameof(RR.BV_GRUND_KEIN_STAND), kultur);
            if (werte == null || werte.Ergebnisse.Count == 0) return Leer(nameof(RR.BV_GRUND_KEINE_WIRTSCHAFTLICHKEIT), kultur);
            if (werte.Parameter == null || werte.Parameter.IdKraftwerkspark <= 0) return Leer(nameof(RR.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            WirtschaftlichkeitErgebnis erw = werte.Erwartet(v.IdProjekt);
            if (erw == null || !werte.ErgebnisAktuell(erw)) return Leer(nameof(RR.BV_GRUND_NICHT_VERFUEGBAR), kultur);
            return Emissionsbilanz(werte.Emissionsbilanz(v.IdProjekt), englisch, kultur);
        }
    }
}
