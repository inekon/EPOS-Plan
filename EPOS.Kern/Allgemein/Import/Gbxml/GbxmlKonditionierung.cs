using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Lasten und Zeitpläne der gbXML-Datei → Konditionierung einer Zone</b> (Welle DA-1, Konzept Datenaustausch der
    /// Simulationsdaten 4.1, E138). Formatfrei im Ergebnis: eine <see cref="Zonenkonditionierung"/> mit Herkunft
    /// <see cref="Konditionierungsherkunft.GbxmlDatei"/>, die <see cref="ZonenplanCtrl.ProjektdateiUebernehmen"/> über den Weg der
    /// Konditionierung schreibt — <b>Datei vor Profil</b> (F2): Was die Datei liefert, ersetzt die Kopie des Nutzungsprofils; was
    /// sie nicht liefert, bleibt beim Profil. Ein Nennwert ohne Zeitplan behält den Zeitplan des Profils
    /// (<see cref="Groessenkonditionierung.NennwertDatei"/>).
    ///
    /// <para><b>Abbildung:</b> Personen (Zahl × Wärme je Person) und Geräte (Geräte + Licht je Fläche × Fläche) als Nennwert [W]
    /// mit dem nennwertgewichteten Anteil der Pläne; Lüftung als Luftwechsel der Nutzer [1/h] aus dem Luftwechsel der Zone, sonst
    /// aus der Außenluft je Person und je Fläche über das Volumen; Heiz- und Kühlsollwert aus den Temperaturplänen, flächengewichtet.
    /// Die Jahresabschnitte aller Räume der Zone werden auf eine gemeinsame Teilung gelegt; der Abschnitt mit den meisten Tagen ist
    /// die Standardwoche, die übrigen werden Perioden (Art Betriebspause, wenn alles „aus“ ist; Ferien, wenn der Abschnitt so heißt;
    /// sonst Zeitraum), der Tagtyp <c>Holiday</c> die neun Feiertagsregeln.</para>
    ///
    /// <para><b>Verdichtet und benannt</b> (F3): unterschiedliche Pläne der Räume einer Zone (gewichtetes Mittel), Feiertage ohne
    /// eigenes Tagesbild in einem Teil der Räume (deren Sonntag), Werte außerhalb der Grenzen der Größe (begrenzt bzw. „aus“), Tage
    /// ohne Abschnitt (Standardwoche); <b>verworfen</b>: Pläne, die die Datei nicht abbildbar hält, Pläne falschen Typs, Perioden
    /// über der Höchstzahl des Kalenders, latente Wärme.</para>
    /// </summary>
    internal static class GbxmlKonditionierung
    {
        private const string P = GbxmlImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Die Abweichung zwischen Datei und Profil, ab der der Hinweis kommt (relativ).</summary>
        internal const double ABWEICHUNG_HINWEIS = 0.05;

        private static readonly Regex FERIEN = new Regex("ferien|urlaub|holiday|vacation", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private sealed record Teil(double Gewicht, GbxmlZeitplan Plan);

        private sealed class Zaehler
        {
            public int Verdichtet;
            public readonly HashSet<GbxmlZeitplan> Verworfen = new HashSet<GbxmlZeitplan>();
        }

        /// <summary>
        /// Die Konditionierung der Zone aus den Räumen mit gbXML-Lasten; <c>null</c>, wenn kein Raum etwas trägt (dann bleibt der
        /// Profilweg unverändert). Die Meldungen (Zähler, Verdichtungen, Verwürfe) stehen an <see cref="Zonenkonditionierung.Meldungen"/>.
        /// </summary>
        internal static Zonenkonditionierung Bilden(string zone, IEnumerable<AbbildRaum> raeume, double? flaecheM2, double? volumenM3)
        {
            List<AbbildRaum> mit = (raeume ?? Enumerable.Empty<AbbildRaum>()).Where(r => r.GbxmlLasten != null).ToList();
            if (mit.Count == 0) return null;
            string name = zone ?? "";
            var meldungen = new List<PruefMeldung>();
            var z = new Zaehler();
            var groessen = new List<Groessenkonditionierung>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                groessen.Add(Groesse(g, name, mit, flaecheM2, volumenM3, z, meldungen) ?? new Groessenkonditionierung
                {
                    Groesse = g, Herkunft = Konditionierungsherkunft.Vorlage,
                });
            if (!groessen.Any(x => x.Herkunft != Konditionierungsherkunft.Vorlage)) return null;
            int werte = mit.Sum(r => r.GbxmlLasten.Werte + new[] { r.Personen ?? r.FlaecheJePersonM2, r.GeraeteWm2, r.LichtWm2 }.Count(w => w.HasValue));
            int plaene = mit.SelectMany(r => r.GbxmlLasten.Plaene).Distinct().Count();
            foreach (GbxmlZeitplan p in mit.SelectMany(r => r.GbxmlLasten.Plaene).Where(p => p.Verworfen != null)) z.Verworfen.Add(p);
            var k = new Zonenkonditionierung { Zone = name, Groessen = groessen };
            k.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "ZONE_LASTEN", name, Ganz(werte), Ganz(plaene), Ganz(z.Verdichtet), Ganz(z.Verworfen.Count)));
            k.Meldungen.AddRange(meldungen);
            return k;
        }

        private static Groessenkonditionierung Groesse(Konditionierungsgroesse g, string zone, List<AbbildRaum> raeume, double? flaecheM2,
                                                       double? volumenM3, Zaehler z, List<PruefMeldung> meldungen)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Personen:
                {
                    var teile = new List<Teil>();
                    foreach (AbbildRaum r in raeume)
                    {
                        double? zahl = r.Personen ?? (r.FlaecheJePersonM2 > 0.0 && r.FlaecheM2 > 0.0 ? r.FlaecheM2 / r.FlaecheJePersonM2 : null);
                        double? w = r.GbxmlLasten.WaermeJePersonW;
                        if (w.HasValue && string.Equals(r.GbxmlLasten.WaermeArt, "Latent", StringComparison.OrdinalIgnoreCase))
                        {
                            meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "WAERME_LATENT", zone, r.Name ?? r.Kennung, Zahl(w.Value)));
                            w = null;
                        }
                        if (zahl > 0.0) teile.Add(new Teil(zahl.Value * (w ?? Matrixeingang.PERSON_W), r.GbxmlLasten.PersonenPlan));
                    }
                    return Anteilsgroesse(g, zone, teile, z, meldungen);
                }
                case Konditionierungsgroesse.Geraete:
                {
                    var teile = new List<Teil>();
                    foreach (AbbildRaum r in raeume.Where(x => x.FlaecheM2 > 0.0))
                    {
                        if (r.GeraeteWm2 > 0.0) teile.Add(new Teil(r.GeraeteWm2.Value * r.FlaecheM2.Value, r.GbxmlLasten.GeraetePlan));
                        if (r.LichtWm2 > 0.0) teile.Add(new Teil(r.LichtWm2.Value * r.FlaecheM2.Value, r.GbxmlLasten.LichtPlan));
                    }
                    return Anteilsgroesse(g, zone, teile, z, meldungen);
                }
                case Konditionierungsgroesse.Lueftung:
                    return Lueftung(zone, raeume, volumenM3, z, meldungen);
                default:
                {
                    bool heizen = g == Konditionierungsgroesse.Heizsoll;
                    var teile = raeume.Where(r => (heizen ? r.GbxmlLasten.HeizPlan : r.GbxmlLasten.KuehlPlan) != null)
                                      .Select(r => new Teil(r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 1.0, heizen ? r.GbxmlLasten.HeizPlan : r.GbxmlLasten.KuehlPlan))
                                      .ToList();
                    List<AbbildRaum> mitWert = raeume.Where(r => (heizen ? r.SollHeizenC : r.SollKuehlenC).HasValue).ToList();
                    double? auslegung = mitWert.Count == 0 ? null
                        : Math.Round(mitWert.Sum(r => (heizen ? r.SollHeizenC : r.SollKuehlenC).Value * (r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 1.0))
                                     / mitWert.Sum(r => r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 1.0), 1);
                    if (auslegung.HasValue) auslegung = Math.Clamp(auslegung.Value, Konditionierungsgroessen.Min(g), Konditionierungsgroessen.Max(g));
                    Konditionierungskalender kalender = Kalender(g, zone, Gueltig(teile, temperatur: true, g, zone, z, meldungen), null, z, meldungen);
                    var vorgaben = new List<Vorgabebeleg>();
                    if (auslegung.HasValue) vorgaben.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(auslegung.Value), null));
                    if (kalender == null && vorgaben.Count == 0) return null;
                    return Ergebnis(g, kalender, vorgaben, null, teile);
                }
            }
        }

        private static Groessenkonditionierung Anteilsgroesse(Konditionierungsgroesse g, string zone, List<Teil> teile, Zaehler z, List<PruefMeldung> meldungen)
        {
            if (teile.Count == 0) return null;
            double nennwert = Math.Round(teile.Sum(t => t.Gewicht), 1);
            if (!(nennwert > 0.0)) return null;
            List<Teil> gueltig = Gueltig(teile, temperatur: false, g, zone, z, meldungen);
            if (!gueltig.Any(t => t.Plan != null))
            {
                // Nennwert ohne Zeitplan: der Zeitplan des Profils bleibt (Datei vor Profil, F2).
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "NENNWERT_OHNE_PLAN", zone, Groessentext(g), Zahl(nennwert)));
                return new Groessenkonditionierung
                {
                    Groesse = g, Herkunft = Konditionierungsherkunft.GbxmlDatei, NennwertDatei = nennwert,
                    BemerkungText = Bemerkung(Enumerable.Empty<Teil>()),
                };
            }
            if (gueltig.Any(t => t.Plan == null))
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "PLAN_TEILWEISE", zone, Groessentext(g)));
            Konditionierungskalender kalender = Kalender(g, zone, gueltig, nennwert, z, meldungen);
            return Ergebnis(g, kalender, new List<Vorgabebeleg>(), kalender == null ? nennwert : null, teile);
        }

        private static Groessenkonditionierung Lueftung(string zone, List<AbbildRaum> raeume, double? volumenM3, Zaehler z, List<PruefMeldung> meldungen)
        {
            if (!(volumenM3 > 0.0)) return null;
            var teile = new List<Teil>();
            bool jePerson = false;
            foreach (AbbildRaum r in raeume)
            {
                GbxmlRaumlasten l = r.GbxmlLasten;
                double volumen = r.VolumenM3 > 0.0 ? r.VolumenM3.Value : 0.0;
                double strom = 0.0;
                if (l.ZonenLuftwechselJeH > 0.0 && volumen > 0.0) strom = l.ZonenLuftwechselJeH.Value * volumen;
                else
                {
                    double? zahl = r.Personen ?? (r.FlaecheJePersonM2 > 0.0 && r.FlaecheM2 > 0.0 ? r.FlaecheM2 / r.FlaecheJePersonM2 : null);
                    if (l.AussenluftJePersonM3h > 0.0 && zahl > 0.0) { strom += l.AussenluftJePersonM3h.Value * zahl.Value; jePerson = true; }
                    if (l.AussenluftJeFlaecheM3hM2 > 0.0 && r.FlaecheM2 > 0.0) strom += l.AussenluftJeFlaecheM3hM2.Value * r.FlaecheM2.Value;
                }
                if (strom > 0.0) teile.Add(new Teil(strom, l.LueftungPlan));
            }
            if (teile.Count == 0) return null;
            double n = Math.Round(teile.Sum(t => t.Gewicht) / volumenM3.Value, 3);
            if (n > Konditionierungsgroessen.LUEFTUNG_MAX)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "WERTE_BEGRENZT", zone, Groessentext(Konditionierungsgroesse.Lueftung), "1"));
                z.Verdichtet++;
                n = Konditionierungsgroessen.LUEFTUNG_MAX;
            }
            if (jePerson)
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "AUSSENLUFT_PERSON", zone, Zahl(n)));
            var vorgaben = new List<Vorgabebeleg> { new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(n), null) };
            List<Teil> gueltig = Gueltig(teile, temperatur: false, Konditionierungsgroesse.Lueftung, zone, z, meldungen);
            Konditionierungskalender kalender = null;
            if (gueltig.Any(t => t.Plan != null))
            {
                if (gueltig.Any(t => t.Plan == null))
                    meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "PLAN_TEILWEISE", zone, Groessentext(Konditionierungsgroesse.Lueftung)));
                kalender = Kalender(Konditionierungsgroesse.Lueftung, zone, gueltig, null, z, meldungen, faktor: n);
            }
            return Ergebnis(Konditionierungsgroesse.Lueftung, kalender, vorgaben, null, teile);
        }

        private static Groessenkonditionierung Ergebnis(Konditionierungsgroesse g, Konditionierungskalender kalender, List<Vorgabebeleg> vorgaben,
                                                        double? nennwert, List<Teil> teile)
            => new Groessenkonditionierung
            {
                Groesse = g, Herkunft = Konditionierungsherkunft.GbxmlDatei, Kalender = kalender, Vorgaben = vorgaben, NennwertDatei = nennwert,
                BemerkungText = Bemerkung(teile),
            };

        /// <summary>Die Bemerkung des Kalenders: „aus gbXML-Datei“ und die Namen der Pläne (≤ 200 Zeichen).</summary>
        private static string Bemerkung(IEnumerable<Teil> teile)
        {
            string t = MyResource.Resource.IMP_GBXML_KOND_HERKUNFT;
            string plaene = string.Join(", ", teile.Where(x => x.Plan != null).Select(x => x.Plan.Anzeige).Distinct());
            if (plaene.Length > 0) t += Kalenderherkunft.TRENNER + plaene;
            return t.Length <= KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN ? t : t.Substring(0, KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN);
        }

        /// <summary>Die Teile mit abbildbarem Plan passenden Typs; die übrigen Pläne fallen weg (benannt), der Teil bleibt ohne Plan.</summary>
        private static List<Teil> Gueltig(List<Teil> teile, bool temperatur, Konditionierungsgroesse g, string zone, Zaehler z, List<PruefMeldung> meldungen)
        {
            var aus = new List<Teil>();
            foreach (Teil t in teile)
            {
                GbxmlZeitplan p = t.Plan;
                if (p == null) { aus.Add(t); continue; }
                if (p.Verworfen != null) { z.Verworfen.Add(p); aus.Add(t with { Plan = null }); continue; }
                if (temperatur ? !p.IstTemperatur : !p.IstAnteil)
                {
                    if (z.Verworfen.Add(p))
                        meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "PLAN_ART", zone, p.Anzeige, p.Typ ?? "", Groessentext(g)));
                    aus.Add(t with { Plan = null });
                    continue;
                }
                aus.Add(t);
            }
            return temperatur ? aus.Where(t => t.Plan != null).ToList() : aus;
        }

        // ==================================================================
        //  Gemeinsame Teilung der Jahresabschnitte → Kalender
        // ==================================================================

        private sealed record Intervall(int Beginn, int Ende, double[] Woche, string Name);

        /// <summary>
        /// Der Kalender aus den Teilen: je Tag die gewichtete Woche der Teile (ein Teil ohne Plan trägt bei Anteilen 1), auf die
        /// gemeinsame Teilung aller Abschnittsgrenzen gelegt; <paramref name="faktor"/> skaliert die Anteile (Lüftung in 1/h).
        /// <c>null</c> ohne Teil mit Plan.
        /// </summary>
        private static Konditionierungskalender Kalender(Konditionierungsgroesse g, string zone, List<Teil> teile, double? nennwert,
                                                         Zaehler z, List<PruefMeldung> meldungen, double faktor = 1.0)
        {
            if (!teile.Any(t => t.Plan != null) || !(teile.Sum(t => t.Gewicht) > 0.0)) return null;
            bool temperatur = g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll;
            if (teile.Where(t => t.Plan != null).Select(t => t.Plan).Distinct().Count() > 1)
            {
                z.Verdichtet++;
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "VERDICHTET_RAEUME", zone, Groessentext(g),
                    string.Join(", ", teile.Where(t => t.Plan != null).Select(t => t.Plan.Anzeige).Distinct())));
            }
            int ohneAbschnitt = 0;
            foreach (GbxmlZeitplan p in teile.Where(t => t.Plan != null).Select(t => t.Plan).Distinct())
                ohneAbschnitt = Math.Max(ohneAbschnitt, Enumerable.Range(1, 365).Count(d => !p.Abschnitte.Any(a => a.Enthaelt(d))));
            if (ohneAbschnitt > 0)
            {
                z.Verdichtet++;
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "TAGE_OHNE_ABSCHNITT", zone, Groessentext(g), Ganz(ohneAbschnitt)));
            }

            // Die gemeinsame Teilung: jede Grenze eines Abschnitts eines Plans.
            var grenzen = new SortedSet<int> { 1 };
            foreach (GbxmlJahresabschnitt a in teile.Where(t => t.Plan != null).SelectMany(t => t.Plan.Abschnitte))
            {
                grenzen.Add(a.Beginn);
                if (a.Ende < 365) grenzen.Add(a.Ende + 1);
            }
            List<int> b = grenzen.ToList();
            var intervalle = new List<Intervall>();
            int begrenzt = 0;
            for (int i = 0; i < b.Count; i++)
            {
                int beginn = b[i], ende = i + 1 < b.Count ? b[i + 1] - 1 : 365;
                double[] woche = Mischen(teile, beginn, temperatur, faktor);
                begrenzt += Begrenzen(g, woche);
                string name = teile.Where(t => t.Plan != null).Select(t => Abschnitt(t.Plan, beginn)?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                if (intervalle.Count > 0 && Gleich(intervalle[^1].Woche, woche) && intervalle[^1].Name == name)
                    intervalle[^1] = intervalle[^1] with { Ende = ende };
                else intervalle.Add(new Intervall(beginn, ende, woche, name));
            }
            if (begrenzt > 0)
            {
                z.Verdichtet++;
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "WERTE_BEGRENZT", zone, Groessentext(g), Ganz(begrenzt)));
            }

            // Die Standardwoche: die Woche mit den meisten Tagen.
            double[] haupt = intervalle.GroupBy(x => Schluessel(x.Woche)).OrderByDescending(gr => gr.Sum(x => x.Ende - x.Beginn + 1))
                                       .First().First().Woche;
            List<Intervall> perioden = intervalle.Where(x => !Gleich(x.Woche, haupt)).ToList();

            // Die Feiertage: das Tagesbild „Holiday“ des Hauptabschnitts jedes Plans; ein Plan ohne nimmt seinen Sonntag.
            double[] feiertag = Feiertag(teile, temperatur, faktor, zone, g, z, meldungen);
            if (feiertag != null) Begrenzen(g, feiertag);
            int frei = Kalenderregel.PERIODEN_MAX - (feiertag != null ? DbWerte.KOND_FEIERTAGE.Count : 0);
            if (perioden.Count > frei)
            {
                List<Intervall> weg = perioden.OrderBy(x => x.Ende - x.Beginn).Take(perioden.Count - frei).ToList();
                perioden = perioden.Except(weg).ToList();
                z.Verdichtet++;
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "PLAN_PERIODEN", zone, Groessentext(g), Ganz(weg.Count), Ganz(frei)));
            }

            var regeln = new List<Kalenderregel>();
            if (feiertag != null)
            {
                var w = new double[Kalenderwoche.WOCHENWERTE];
                for (int d = 0; d < 7; d++) Array.Copy(feiertag, 0, w, d * Kalenderwoche.TAGESSTUNDEN, Kalenderwoche.TAGESSTUNDEN);
                for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                    regeln.Add(Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + k, KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[k],
                                                      DbWerte.KOND_FEIERTAGE[k], Angabe(w)));
            }
            var namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int rang = Standardfahrplan.RANG_EIGEN;
            foreach (Intervall x in perioden)
            {
                bool aus = x.Woche.All(v => double.IsNaN(v) || (!temperatur && v <= 0.0));
                string art = aus && !temperatur ? DbWerte.KOND_ART_BETRIEBSPAUSE
                           : x.Name != null && FERIEN.IsMatch(x.Name) ? DbWerte.KOND_ART_FERIEN : DbWerte.KOND_ART_ZEITRAUM;
                string basis = Kuerzen(string.IsNullOrWhiteSpace(x.Name) ? MyResource.Resource.IMP_GBXML_KOND_ABSCHNITT : x.Name.Trim(),
                                       KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN - 4);
                string name = basis;
                for (int n = 2; !namen.Add(name); n++) name = basis + " " + Ganz(n);
                regeln.Add(Kalenderregel.Zeitraum(rang++, art, name, x.Beginn, x.Ende,
                                                  aus && !temperatur ? Kalenderangabe.Abgeschaltet : Angabe(x.Woche)));
            }
            return new Konditionierungskalender(g, Angabe(haupt), nennwert, regeln);
        }

        private static double[] Mischen(List<Teil> teile, int tag, bool temperatur, double faktor)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            double summe = teile.Sum(t => t.Gewicht);
            foreach (Teil t in teile)
            {
                double[] eigene = t.Plan == null ? null : (Abschnitt(t.Plan, tag) ?? Hauptabschnitt(t.Plan)).Woche.Woche();
                for (int h = 0; h < w.Length; h++) w[h] += t.Gewicht * (eigene == null ? 1.0 : eigene[h]);
            }
            for (int h = 0; h < w.Length; h++) w[h] = Math.Round(w[h] / summe * (temperatur ? 1.0 : faktor), Kalenderwoche.NACHKOMMASTELLEN);
            return w;
        }

        private static double[] Feiertag(List<Teil> teile, bool temperatur, double faktor, string zone, Konditionierungsgroesse g,
                                         Zaehler z, List<PruefMeldung> meldungen)
        {
            List<Teil> mitPlan = teile.Where(t => t.Plan != null).ToList();
            if (!mitPlan.Any(t => Hauptabschnitt(t.Plan).Woche.Feiertag != null)) return null;
            if (mitPlan.Any(t => Hauptabschnitt(t.Plan).Woche.Feiertag == null))
            {
                z.Verdichtet++;
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "FEIERTAG_SONNTAG", zone, Groessentext(g)));
            }
            var f = new double[Kalenderwoche.TAGESSTUNDEN];
            double summe = teile.Sum(t => t.Gewicht);
            foreach (Teil t in teile)
            {
                GbxmlWochenplan w = t.Plan == null ? null : Hauptabschnitt(t.Plan).Woche;
                double[] tag = w == null ? null : w.Feiertag ?? w.Tag(6);
                for (int h = 0; h < f.Length; h++) f[h] += t.Gewicht * (tag == null ? 1.0 : tag[h]);
            }
            for (int h = 0; h < f.Length; h++) f[h] = Math.Round(f[h] / summe * (temperatur ? 1.0 : faktor), Kalenderwoche.NACHKOMMASTELLEN);
            return f;
        }

        /// <summary>Der Abschnitt, der den Tag enthält — bei Überschneidung der spätere der Datei; <c>null</c> = keiner.</summary>
        private static GbxmlJahresabschnitt Abschnitt(GbxmlZeitplan p, int tag) => p.Abschnitte.LastOrDefault(a => a.Enthaelt(tag));

        /// <summary>Der Abschnitt mit den meisten Tagen (bei Gleichstand der erste).</summary>
        private static GbxmlJahresabschnitt Hauptabschnitt(GbxmlZeitplan p)
            => p.Abschnitte.Aggregate((a, b) => b.Tage > a.Tage ? b : a);

        /// <summary>
        /// Die Grenzen der Größe (Prüfklauseln der Kalendertabellen): ein Heizsollwert unter dem kleinsten und ein Kühlsollwert über
        /// dem größten zulässigen heißt „aus“, alles andere wird begrenzt. Rückgabe: die Zahl der geänderten Zellen.
        /// </summary>
        private static int Begrenzen(Konditionierungsgroesse g, double[] w)
        {
            int n = 0;
            double min = Konditionierungsgroessen.Min(g), max = Konditionierungsgroessen.Max(g);
            for (int h = 0; h < w.Length; h++)
            {
                double v = w[h];
                if (double.IsNaN(v)) continue;
                double neu = g == Konditionierungsgroesse.Heizsoll && v < min ? double.NaN
                           : g == Konditionierungsgroesse.Kuehlsoll && v > max ? double.NaN
                           : Math.Clamp(v, min, max);
                if (!neu.Equals(v)) { w[h] = neu; n++; }
            }
            return n;
        }

        private static Kalenderangabe Angabe(double[] w)
        {
            if (w.All(double.IsNaN)) return Kalenderangabe.Abgeschaltet;
            if (w.All(v => v.Equals(w[0]))) return Kalenderangabe.AusWert(w[0]);
            return Kalenderangabe.AusWoche(w);
        }

        private static bool Gleich(double[] a, double[] b) => a.Length == b.Length && a.Zip(b, (x, y) => x.Equals(y)).All(e => e);

        private static string Schluessel(double[] w) => string.Join(";", w.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));

        // ==================================================================
        //  Abgleich mit dem Nutzungsprofil (E138 F2: Hinweis, es gilt die Datei)
        // ==================================================================

        /// <summary>
        /// Die Hinweise, wo die Datei vom Nutzungsprofil der Zone abweicht (Nennwert der Personen und der Geräte, mehr als
        /// <see cref="ABWEICHUNG_HINWEIS"/>); es gilt die Datei. Leer ohne Profil, ohne Fläche oder ohne gbXML-Herkunft.
        /// </summary>
        internal static List<PruefMeldung> Profilabgleich(Zonenkonditionierung k, Raumnutzungsprofil profil, double? flaecheM2)
        {
            var aus = new List<PruefMeldung>();
            if (k == null || profil == null || !(flaecheM2 > 0.0) || !k.AusGbxml) return aus;
            void Vergleich(Konditionierungsgroesse g, double? profilwert)
            {
                Groessenkonditionierung d = k.Groessen.FirstOrDefault(x => x.Groesse == g && x.Herkunft == Konditionierungsherkunft.GbxmlDatei);
                double? datei = d?.Kalender?.Nennwert ?? d?.NennwertDatei;
                if (!(datei > 0.0) || !(profilwert > 0.0)) return;
                if (Math.Abs(datei.Value - profilwert.Value) > ABWEICHUNG_HINWEIS * profilwert.Value)
                    aus.Add(new PruefMeldung(PruefStufe.Info, P + "PROFIL_ABWEICHUNG", k.Zone, Groessentext(g), Zahl(datei.Value),
                                             profil.Bezeichner ?? "", Zahl(Math.Round(profilwert.Value, 1))));
            }
            Vergleich(Konditionierungsgroesse.Personen, profil.Personen_Flaeche > 0.0
                ? flaecheM2 / profil.Personen_Flaeche * (profil.Personen_Waerme ?? Matrixeingang.PERSON_W) : null);
            Vergleich(Konditionierungsgroesse.Geraete, profil.Geraete_Leistung > 0.0 ? profil.Geraete_Leistung * flaecheM2 : null);
            return aus;
        }

        private static string Groessentext(Konditionierungsgroesse g) => Konditionierungsgroessen.Kennwort(g);

        private static string Kuerzen(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        private static string Zahl(double w) => w.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
