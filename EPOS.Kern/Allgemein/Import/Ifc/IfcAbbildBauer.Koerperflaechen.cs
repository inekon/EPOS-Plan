using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using Xbim.Ifc4.Interfaces;
using KF = WindowsFormsApplication1.Koerperflaechen;

namespace WindowsFormsApplication1
{
    internal sealed partial class IfcAbbildBauer
    {
        /// <summary>
        /// Schaltet den Körperweg der Bauteilflächen aus (<see cref="Koerperflaechenzuordnung"/>): Dann gilt der Rückfall
        /// ohne Zuordnung zu den Räumen. Nur für den Vergleich beider Wege (Abstimmung G5, A6).
        /// </summary>
        internal bool KoerperflaechenAus { get; set; }

        /// <summary>Die Abweichungen der Summe je Raum gegen die Körperfläche über <see cref="IfcBauteilkoerper.ABWEICHUNG_GRENZE"/>.</summary>
        private readonly List<(Bauteilart Art, string Name, double Abweichung)> _zuordnungAbweichungen
            = new List<(Bauteilart, string, double)>();

        /// <summary>Die Schlüssel der Gegenprobe je Gruppe (<see cref="Koerpergruppe"/>).</summary>
        private static readonly string[] ZUORDNUNG_SCHLUESSEL =
        {
            P + "KOERPERFLAECHEN_ABWEICHUNG_AUSSENWAND", P + "KOERPERFLAECHEN_ABWEICHUNG_INNENWAND", P + "KOERPERFLAECHEN_ABWEICHUNG_DACH",
            P + "KOERPERFLAECHEN_ABWEICHUNG_BODEN_DECKE", P + "KOERPERFLAECHEN_ABWEICHUNG_SONSTIGE",
        };

        /// <summary>
        /// <b>Bauteilflächen je Raum aus den Körpern</b> (Abstimmung G5, Teil G5-3, A1–A4): Für jedes Gebäude ohne Raumgrenzen
        /// ordnet <see cref="KF"/> die Bauteile mit Körper (G5-1), die weder eine Raumgrenze noch einen Nachbarraum
        /// noch einen Raumbezug tragen, den Räumen mit Körper (G7f) zu: je Raum und Bauteilseite eine Grenze der Herkunft
        /// <see cref="Grenzherkunft.Bauteilkoerper"/> mit Fläche, Außennormale, Schwerpunkt und Lage — Trennflächen mit
        /// wechselseitigen Gegenstücken, sonst Außenluft, Erdreich oder unbeheizt. Rangfolge: Raumgrenzen und Raumbezüge der
        /// Datei gehen vor; der Körperweg greift nur, wo sonst der Rückfall ohne Raum gälte.
        /// <list type="bullet">
        /// <item><b>Öffnungen nach Lage:</b> Fenster und Türen des Bauteils bekommen die Räume, in deren überlappendem Teil ihre
        /// Mitte liegt (Öffnungskörper, sonst Füllkörper) — an diese Zonen gehen sie samt Abzug.</item>
        /// <item><b>Gegenprobe:</b> Die Summe der stärker belegten Seite gegen die Körperfläche aus G5-1; über
        /// <see cref="IfcBauteilkoerper.ABWEICHUNG_GRENZE"/> eine Info je Bauteilart (doppelte oder fehlende Zuordnung).</item>
        /// </list>
        /// Meldungen: <c>IMP_IFC_PROT_KOERPERFLAECHEN</c> (I) je Gebäude, <c>IMP_IFC_PROT_KOERPERFLAECHEN_ABWEICHUNG_*</c> (I).
        /// </summary>
        private void Koerperflaechenzuordnung()
        {
            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                AbbildGebaeude g = _abbild.Gebaeude[gi];
                // 17.5: Der Weg der Bauteilflächen aus Raum- und Bauteilkörpern greift nur mit Körpern der Datei.
                if (g.ZahlGrenzen > 0 || !g.Raeume.Any(r => Dateikoerper.Beleg(r.Koerper))) continue;
                Raumflaechen(g);
                if (KoerperflaechenAus) continue;

                var mitBezug = new HashSet<string>(StringComparer.Ordinal);
                foreach (int label in _raumbezug.Keys)
                    if (_modell.Instances[label] is IIfcRoot wurzel) mitBezug.Add(wurzel.GlobalId.ToString());

                double untersterBoden = g.Raeume.Where(r => Dateikoerper.Beleg(r.Koerper) && r.Koerper.PunkteM.Count > 0)
                                                .Min(r => r.Koerper.PunkteM.Min(p => p[2]));
                List<Koerperflaechenraum> raeume = g.Raeume.Select(r =>
                {
                    if (!Dateikoerper.Beleg(r.Koerper) || r.Koerper.PunkteM.Count == 0) return new Koerperflaechenraum();
                    double unten = r.Koerper.PunkteM.Min(p => p[2]), oben = r.Koerper.PunkteM.Max(p => p[2]);
                    return new Koerperflaechenraum
                    {
                        Koerper = r.Koerper,
                        Unterirdisch = r.GeschossLageM.HasValue ? r.GeschossLageM.Value < -0.01 : oben <= 0.01,
                        Unterster = unten <= untersterBoden + 0.1,
                    };
                }).ToList();

                int bauteile = 0, flaechen = 0;
                double summe = 0.0;
                foreach ((AbbildBauteil b, List<Dateikoerper> koerper, _, _) in _koerperVormerkung.Where(v => v.Gebaeude == gi))
                {
                    if (!g.Bauteile.Contains(b) || b.Grenzen.Count > 0 || b.Nachbarn.Count > 0 || mitBezug.Contains(b.Kennung)) continue;
                    bool innen = b.Randbedingung == Randbedingung.Innen || b.Randbedingung == Randbedingung.Unbeheizt;
                    Koerperflaechenergebnis e = KF.Zuordnen(raeume, koerper, b.DickeM, innen);
                    if (e.Stuecke.Count == 0) continue;
                    bauteile++;
                    double[] massgeblich = b.Koerperflaeche?.Teile.FirstOrDefault()?.Normale;
                    foreach (Koerperflaechenstueck s in e.Stuecke)
                    {
                        b.Grenzen.Add(new AbbildGrenze
                        {
                            Kennung = b.Kennung + "|KF|" + s.Schluessel, RaumKennung = g.Raeume[s.Raum].Kennung, Lage = s.Lage,
                            FlaecheM2 = s.FlaecheM2, SchwerpunktM = s.SchwerpunktM, Normale = s.Normale,
                            GegenstueckKennung = s.Gegenschluessel == null ? null : b.Kennung + "|KF|" + s.Gegenschluessel,
                            Herkunft = Grenzherkunft.Bauteilkoerper,
                            NeigungGrad = Math.Round(IfcBauteilkoerper.Neigung(s.Normale), 6),
                            AzimutGrad = IfcBauteilkoerper.Azimut(s.Normale, _drehung) is double az ? Math.Round(az, 6) : (double?)null,
                            Gegenseite = massgeblich != null && KF.Punkt(massgeblich, s.Normale) < 0.0,
                        });
                        flaechen++;
                        summe += s.FlaecheM2;
                    }

                    // Öffnungen nach Lage: die Räume, in deren Überlappung die Mitte der Öffnung liegt.
                    double abstand = (b.DickeM > 0.0 ? b.DickeM.Value : Koerpernachbarschaft.TRENNDICKE_MAX_M) + KF.TOLERANZ_M;
                    foreach (AbbildBauteil o in b.Oeffnungen.Where(x => x.Grenzen.Count == 0))
                    {
                        double[] mitte = null;
                        if (_oeffnungsquelle.TryGetValue(o, out (IIfcOpeningElement Oeffnung, IIfcElement Element) q))
                            mitte = (q.Oeffnung != null ? Mitte(Rechenkoerper(q.Oeffnung)) : null)
                                    ?? Mitte(o.Koerper ?? (q.Element != null ? Rechenkoerper(q.Element) : null));
                        else mitte = Mitte(o.Koerper);
                        foreach (int r in e.RaeumeAn(mitte, abstand))
                            o.Grenzen.Add(new AbbildGrenze
                            {
                                Kennung = o.Kennung + "|KF|R" + r, RaumKennung = g.Raeume[r].Kennung, FlaecheM2 = o.BruttoflaecheM2,
                                SchwerpunktM = mitte, Herkunft = Grenzherkunft.Bauteilkoerper,
                                Lage = e.Stuecke.Any(s => s.Raum == r && s.Lage == Randbedingung.Innen) ? Randbedingung.Innen
                                     : e.Stuecke.Where(s => s.Raum == r).Select(s => s.Lage).DefaultIfEmpty(Randbedingung.Unbekannt).First(),
                            });
                    }

                    // Gegenprobe: die stärker belegte Seite gegen die Körperfläche aus G5-1.
                    if (b.Koerperflaeche != null && b.Koerperflaeche.FlaecheM2 > 0.0)
                    {
                        double abweichung = IfcBauteilkoerper.Abweichung(b.Koerperflaeche.FlaecheM2, e.SeiteM2);
                        if (abweichung > IfcBauteilkoerper.ABWEICHUNG_GRENZE)
                            _zuordnungAbweichungen.Add((b.Art, string.IsNullOrWhiteSpace(b.Name) ? b.Kennung : b.Name.Trim(), abweichung));
                    }
                }
                // Die Trennflächen koppeln die Räume wie Flächenpaare der Raumkörper; über Geschosse hinweg die Geschosse.
                var paare = new List<(string Unten, string Oben)>();
                var geschossJeRaum = g.Raeume.GroupBy(r => r.Kennung, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First().GeschossKennung, StringComparer.Ordinal);
                foreach (AbbildBauteil b in g.Bauteile)
                    foreach (AbbildGrenze x in b.Grenzen.Where(x => x.Herkunft == Grenzherkunft.Bauteilkoerper && x.GegenstueckKennung != null))
                    {
                        AbbildGrenze y = b.Grenzen.FirstOrDefault(z => z.Kennung == x.GegenstueckKennung);
                        if (y == null) continue;
                        g.KoerperpaareGebildet = true;
                        string gx = geschossJeRaum.TryGetValue(x.RaumKennung, out string a) ? a : null, gy = geschossJeRaum.TryGetValue(y.RaumKennung, out string c) ? c : null;
                        if (gx != null && gy != null && gx != gy) paare.Add((gx, gy));
                    }
                List<string> warm = g.Raeume.Where(r => r.Beheizt && r.GeschossKennung != null).Select(r => r.GeschossKennung).Distinct(StringComparer.Ordinal).ToList();
                g.KoerperflaechenGekoppelt = warm.Count > 1 && Gekoppelt(warm, paare);
                if (bauteile > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPERFLAECHEN", g.Anzeigename, Ganz(bauteile), Ganz(flaechen),
                        Zahl(Math.Round(summe, 2))));
            }
            for (int gruppe = 0; gruppe < ZUORDNUNG_SCHLUESSEL.Length; gruppe++)
            {
                var liste = _zuordnungAbweichungen.Where(x => Koerpergruppe(x.Art) == gruppe).ToList();
                if (liste.Count == 0) continue;
                var groesste = liste.Aggregate((a, b) => b.Abweichung > a.Abweichung ? b : a);
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, ZUORDNUNG_SCHLUESSEL[gruppe], Ganz(liste.Count),
                    Zahl(Math.Round(groesste.Abweichung * 100.0, 1)), groesste.Name, Zahl(IfcBauteilkoerper.ABWEICHUNG_GRENZE * 100.0)));
            }
        }

        /// <summary>
        /// <b>Raumfläche und Volumen aus dem Raumkörper</b> (G5-3): Ein Raum mit Körper ohne Flächenmenge und ohne
        /// Grundriss (etwa ein Körper als <c>IfcFacetedBrep</c> unter einem geneigten Dach) bekommt als Fläche die waagerechte
        /// Projektion der Bodenflächen seines Körpers (Normale bis 45° nach unten), gekennzeichnet wie der Grundriss
        /// (<see cref="AbbildRaum.FlaecheAusGrundriss"/>); ohne Volumenmenge das Volumen des geschlossenen Körpers
        /// (Divergenzsatz über die Dreiecke). Meldung <c>IMP_IFC_PROT_RAUMFLAECHE_KOERPER</c> (I).
        /// </summary>
        private void Raumflaechen(AbbildGebaeude g)
        {
            var namen = new List<string>();
            double summe = 0.0;
            foreach (AbbildRaum r in g.Raeume)
            {
                if (!Dateikoerper.Beleg(r.Koerper) || r.FlaecheM2.HasValue) continue;
                double boden = 0.0, volumen = 0.0;
                foreach (int[] d in r.Koerper.Dreiecke)
                {
                    double[] a = r.Koerper.PunkteM[d[0]], b = r.Koerper.PunkteM[d[1]], c = r.Koerper.PunkteM[d[2]];
                    double[] k = IfcPlatzierung.Kreuz(IfcPlatzierung.Minus(b, a), IfcPlatzierung.Minus(c, a));
                    double l = Math.Sqrt(k[0] * k[0] + k[1] * k[1] + k[2] * k[2]);
                    if (l <= 1e-12) continue;
                    if (k[2] / l < -0.7071) boden += -k[2] / 2.0;
                    volumen += (a[0] * (b[1] * c[2] - b[2] * c[1]) + a[1] * (b[2] * c[0] - b[0] * c[2]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
                }
                if (!(boden > 0.0)) continue;
                r.FlaecheM2 = Math.Round(boden, 6);
                r.FlaecheAusGrundriss = true;
                if (!r.VolumenM3.HasValue && volumen > 0.0) r.VolumenM3 = Math.Round(volumen, 6);
                summe += boden;
                namen.Add(string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name.Trim());
            }
            if (namen.Count == 0) return;
            // Die Warnung „kein Raum mit Mengenangaben“ trifft nicht mehr zu: Die Räume tragen nun eine Fläche.
            g.Meldungen.RemoveAll(m => m.Schluessel == P + "KEINE_RAEUME");
            g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "RAUMFLAECHE_KOERPER", Ganz(namen.Count), Zahl(Math.Round(summe, 1)), Beispiele(namen)));
        }

        /// <summary>Die Toleranz [m] der Deckenlage: so weit darf der Plattenkörper neben Höhe und Grundriss eines Körperpaars liegen.</summary>
        internal const double DECKENLAGE_TOLERANZ_M = 0.05;

        /// <summary>Die Geschosspaare (Gebäude, unten, oben), deren Körperdecken eine Hülldecke der Datei schon trägt (G5-3d).</summary>
        private readonly HashSet<(int Gebaeude, string Unten, string Oben)> _huelldeckenpaare = new HashSet<(int, string, string)>();

        /// <summary>Je Gebäude die Körperdecken, die einer Hülldecke der Datei weichen: Zahl und Fläche.</summary>
        private readonly Dictionary<int, (int Zahl, double FlaecheM2)> _huelldeckenErsetzt = new Dictionary<int, (int, double)>();

        /// <summary>Die Körper der vorgemerkten Platten (nicht der Wände), einmal gesammelt.</summary>
        private Dictionary<AbbildBauteil, List<Dateikoerper>> _plattenkoerper;

        private Dictionary<AbbildBauteil, List<Dateikoerper>> Plattenkoerper()
        {
            if (_plattenkoerper != null) return _plattenkoerper;
            _plattenkoerper = new Dictionary<AbbildBauteil, List<Dateikoerper>>();
            foreach (var v in _koerperVormerkung)
                if (v.Art != Bauteilkoerperart.Wand) _plattenkoerper.TryAdd(v.Bauteil, v.Koerper);
            return _plattenkoerper;
        }

        /// <summary>
        /// <b>Die Lage einer Platte gegen ein Körperpaar</b> (G5-3d): Kann <paramref name="b"/> die Decke zwischen den beiden
        /// Raumkörpern des Paars sein? Mit Körper zählt die Lage: Sein Höhenbereich muss die Höhen beider Körperflächen des
        /// Paars berühren und eine fast waagerechte Fläche des Körpers die Mitte des Paars (bzw. die Mehrheit seiner Prüfpunkte) tragen, je mit
        /// <see cref="DECKENLAGE_TOLERANZ_M"/> — 2 = getroffen, −1 = liegt woanders (etwa die Decke des Geschosses darüber).
        /// Ohne Körper zählt das Geschoss: 1 = Geschoss eines der beiden Räume, −1 = ein anderes, 0 = keines bekannt.
        /// Eine Platte gegen unbeheizt liegt nie zwischen zwei gleich beheizten Räumen (−1). Ein Wandpaar gibt immer 1.
        /// </summary>
        private int Deckenlage(AbbildBauteil b, Koerperpaar p, AbbildGebaeude g)
        {
            if (!p.Decke || p.Oben < 0) return 1;
            AbbildRaum oben = g.Raeume[p.Oben], unten = g.Raeume[p.Oben == p.RaumA ? p.RaumB : p.RaumA];
            // Eine Platte, die die Datei gegen unbeheizt erklärt, trennt keine zwei gleich beheizten Räume (die Erklärung geht vor).
            if (b.Randbedingung == Randbedingung.Unbeheizt && oben.Beheizt == unten.Beheizt) return -1;
            if (Plattenkoerper().TryGetValue(b, out List<Dateikoerper> koerper) && koerper.Any(k => k != null && k.PunkteM.Count > 0))
            {
                List<double[]> punkte = koerper.Where(k => k != null).SelectMany(k => k.PunkteM).ToList();
                const double T = DECKENLAGE_TOLERANZ_M;
                double zu = Math.Min(p.SchwerpunktA[2], p.SchwerpunktB[2]) - T, zo = Math.Max(p.SchwerpunktA[2], p.SchwerpunktB[2]) + T;
                double x = (p.SchwerpunktA[0] + p.SchwerpunktB[0]) / 2.0, y = (p.SchwerpunktA[1] + p.SchwerpunktB[1]) / 2.0;
                bool hoehe = punkte.Max(q => q[2]) >= zu && punkte.Min(q => q[2]) <= zo;
                if (!hoehe) return -1;
                // Der Grundriss: Liegt die Mitte des Paars (bzw. die Mehrheit seiner Prüfpunkte — die Mitte und die Ecken des
                // Rings, ein Viertel zur Mitte gerückt) über einer waagerechten Fläche des Plattenkörpers?
                var pruef = new List<(double X, double Y)> { (x, y) };
                if (p.RandA != null)
                    foreach (double[] e in p.RandA) pruef.Add((e[0] + 0.25 * (x - e[0]), e[1] + 0.25 * (y - e[1])));
                int getroffen = pruef.Count(q => koerper.Where(k => k != null).Any(k => UeberWaagerechterFlaeche(k, q.X, q.Y, T)));
                bool grundriss = 2 * getroffen > pruef.Count;
                return hoehe && grundriss ? 2 : -1;
            }
            if (b.GeschossKennung == null) return 0;
            return b.GeschossKennung == oben.GeschossKennung || b.GeschossKennung == unten.GeschossKennung ? 1 : -1;
        }

        /// <summary>Liegt (x, y) in der Projektion eines fast waagerechten Dreiecks (Normale bis 45°) des Körpers, Toleranz <paramref name="t"/> [m]?</summary>
        private static bool UeberWaagerechterFlaeche(Dateikoerper k, double x, double y, double t)
        {
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] n = IfcPlatzierung.Kreuz(IfcPlatzierung.Minus(b, a), IfcPlatzierung.Minus(c, a));
                double l = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
                if (l <= 1e-12 || Math.Abs(n[2]) / l < 0.7071) continue;
                if (ImDreieck(x, y, a, b, c, t)) return true;
            }
            return false;
        }

        /// <summary>Punkt (x, y) im Dreieck a, b, c (Projektion auf xy), mit Randtoleranz <paramref name="t"/> [m].</summary>
        private static bool ImDreieck(double x, double y, double[] a, double[] b, double[] c, double t)
        {
            double Seite(double[] p, double[] q)
            {
                double dx = q[0] - p[0], dy = q[1] - p[1], l = Math.Sqrt(dx * dx + dy * dy);
                return l <= 1e-12 ? 0.0 : (dx * (y - p[1]) - dy * (x - p[0])) / l;   // vorzeichenbehafteter Abstand zur Kante
            }
            double s1 = Seite(a, b), s2 = Seite(b, c), s3 = Seite(c, a);
            return (s1 >= -t && s2 >= -t && s3 >= -t) || (s1 <= t && s2 <= t && s3 <= t);
        }

        /// <summary>
        /// <b>Trägt eine Platte der Datei die Körperdecke schon als Hüllfläche?</b> (G5-3d) Eine Körperdecke zwischen einem
        /// beheizten und einem unbeheizten Raum entfällt, wenn eine Platte (<c>IfcSlab</c>, kein Dach) gegen unbeheizt
        /// (<see cref="Randbedingung.Unbeheizt"/>) ohne Raumgrenze, ohne Nachbarn oder mit dem beheizten Raum als einzigem
        /// Nachbarn an ihrer Stelle liegt (<see cref="Deckenlage"/>: getroffen; ohne Körper im Geschoss eines der beiden Räume
        /// bzw. ohne Geschoss mit dem beheizten Raum als Nachbarn). Die Seite muss passen: eine Kellerdecke bzw. ein Zonenboden
        /// nur unter, eine oberste Geschossdecke nur über dem beheizten Raum. Was die Datei sagt, geht vor; der Körper
        /// ergänzt nur. Das Geschosspaar merkt sich <see cref="_huelldeckenpaare"/> — dort bildet auch
        /// <see cref="GrundrissTrenndecken"/> keine Decke.
        /// </summary>
        private bool DurchHuelldeckeGedeckt(int gi, AbbildGebaeude g, Koerperpaar p)
        {
            if (!p.Decke || p.Oben < 0) return false;
            AbbildRaum oben = g.Raeume[p.Oben], unten = g.Raeume[p.Oben == p.RaumA ? p.RaumB : p.RaumA];
            if (oben.Beheizt == unten.Beheizt) return false;
            AbbildRaum warm = oben.Beheizt ? oben : unten;
            bool warmOben = warm == oben;
            foreach (AbbildBauteil b in g.Bauteile)
            {
                if (!string.Equals(b.Quelltyp, "IfcSlab", StringComparison.OrdinalIgnoreCase) || b.Quellart == nameof(IfcSlabTypeEnum.ROOF)) continue;
                if (b.Randbedingung != Randbedingung.Unbeheizt || b.Grenzen.Count > 0 || b.Nachbarn.Count > 1) continue;
                bool einraum = b.Nachbarn.Count == 1;
                if (einraum && b.Nachbarn[0].Kennung != warm.Kennung) continue;
                if (b.RandbedingungBeleg == AbbildBauteil.BELEG_KELLERDECKE && !warmOben) continue;
                if (b.RandbedingungBeleg == AbbildBauteil.BELEG_OBERSTE_DECKE && warmOben) continue;
                if (b.ZonenbodenOhneNachbar is bool boden && boden != warmOben) continue;
                int lage = Deckenlage(b, p, g);
                if (lage == 2 || lage == 1 || (lage == 0 && einraum))
                {
                    if (oben.GeschossKennung != null && unten.GeschossKennung != null && oben.GeschossKennung != unten.GeschossKennung)
                        _huelldeckenpaare.Add((gi, unten.GeschossKennung, oben.GeschossKennung));
                    (int zahl, double flaeche) = _huelldeckenErsetzt.TryGetValue(gi, out var bisher) ? bisher : (0, 0.0);
                    _huelldeckenErsetzt[gi] = (zahl + 1, flaeche + p.FlaecheM2);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Die Meldung <c>IMP_IFC_PROT_KOERPERDECKE_HUELLE</c> (I) je Gebäude, dessen Körperdecken Hülldecken der Datei wichen.</summary>
        private void HuelldeckenMelden(int gi, AbbildGebaeude g)
        {
            if (!_huelldeckenErsetzt.TryGetValue(gi, out var e) || e.Zahl == 0) return;
            g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPERDECKE_HUELLE", g.Anzeigename, Ganz(e.Zahl), Zahl(Math.Round(e.FlaecheM2, 2))));
        }

        /// <summary>Die Toleranz [m²], um die die Öffnungen einer Wand ihre Bruttofläche übersteigen dürfen (Rundung der Mengen).</summary>
        internal const double OEFFNUNG_UEBERSTAND_M2 = 0.01;

        /// <summary>
        /// <b>Öffnungen an der Wand, in der sie liegen</b> (G5-3d): Trägt eine Wand mit Körper, deren Bruttofläche die Datei nennt
        /// (nicht aus dem Körper), laut Datei mehr Fenster- und Türfläche, als sie brutto groß ist, fiele die Hülle still auf die Nettofläche der Datei ohne Abzug zurück. Dann wird
        /// jede ihrer Öffnungen nach Lage geprüft (<see cref="IfcOeffnungen.AbstandZurWand"/>; Mitte des Öffnungskörpers,
        /// sonst des Füllkörpers):
        /// <list type="bullet">
        /// <item><b>In der eigenen Wand:</b> Sie bleibt, solange die Wand sie trägt. Ist die Wand zu klein, geht sie an einen
        /// Teil ohne Darstellung derselben Wand (gleicher Typ und Namensstamm, siehe <see cref="Koerperflaechen"/> — der Körper
        /// deckt diese Teile mit) mit Platz für sie, die Seite gleicher Richtung zuerst, sonst der Teil mit der größten freien
        /// Fläche; sonst an eine andere Wand mit Körper, in deren Ebene und Umriss sie auch liegt; zuletzt an eine Wand gleicher
        /// Art desselben Geschosses an derselben Fassade (gleiche Randbedingung, Himmelsrichtung bis 45°) mit Platz, die größte
        /// freie Fläche zuerst.</item>
        /// <item><b>Nicht in der eigenen Wand:</b> Sie geht an die nächstgelegene Wand mit Körper, in deren Ebene und Umriss sie
        /// liegt und auf der sie Platz hat; findet sich keine, bleibt sie und wird benannt.</item>
        /// </list>
        /// Öffnungen ohne Lage bleiben, wo die Datei sie hinhängt. Meldungen: <c>IMP_IFC_PROT_OEFFNUNG_UMGEHAENGT</c> (I) mit
        /// der Zahl der zu kleinen Wände, der umgehängten Öffnungen und ihrer neuen Wände; <c>IMP_IFC_PROT_OEFFNUNG_OHNE_WAND</c>
        /// (W) für Öffnungen, die in keiner Wand liegen; <c>IMP_IFC_PROT_WAND_KLEINER_OEFFNUNGEN</c> (W) für Wände, die danach
        /// noch kleiner sind als ihre Öffnungen (dort gilt weiter die Nettofläche der Datei).
        /// </summary>
        private void OeffnungenNachLage()
        {
            var koerper = new Dictionary<AbbildBauteil, List<Dateikoerper>>();
            foreach (var v in _koerperVormerkung)
                if (v.Art == Bauteilkoerperart.Wand) koerper.TryAdd(v.Bauteil, v.Koerper);
            if (koerper.Count == 0) return;
            ILookup<(int, string, string), AbbildBauteil> teile = _teileOhneDarstellung.Where(t => t.Bauteil.Name != null)
                .ToLookup(t => (t.Gebaeude, t.Bauteil.Quelltyp, Namensstamm(t.Bauteil.Name)), t => t.Bauteil);
            static bool IstOeffnung(AbbildBauteil o) => o.Art == Bauteilart.Fenster || o.Art == Bauteilart.Tuer;
            static double Summe(AbbildBauteil b) => b.Oeffnungen.Where(IstOeffnung).Sum(o => o.BruttoflaecheM2 ?? 0.0);
            static double Frei(AbbildBauteil b) => (b.BruttoflaecheM2 ?? 0.0) - Summe(b);
            static bool ZuKlein(AbbildBauteil b) => b.BruttoflaecheM2 is double br && Summe(b) > br + OEFFNUNG_UEBERSTAND_M2;
            static string Name(AbbildBauteil b) => string.IsNullOrWhiteSpace(b.Name) ? b.Kennung : b.Name.Trim();
            double? Abstand(AbbildBauteil w, double[] mitte, out double[] n)
                => IfcOeffnungen.AbstandZurWand(koerper[w], w.Koerperflaeche.Teile.Select(t => t.Normale), mitte, out n);
            bool GleicheRichtung(AbbildBauteil teil, double[] n)
            {
                if (n == null || !teil.AzimutGrad.HasValue || !(IfcBauteilkoerper.Azimut(n, _drehung) is double az)) return false;
                double d = Math.Abs(((teil.AzimutGrad.Value - az) % 180.0 + 180.0) % 180.0);
                return Math.Min(d, 180.0 - d) <= 45.0;
            }

            int zuKlein = 0, zielwaende = 0;
            var umgehaengt = new List<string>();
            var ohneWand = new List<string>();
            var weiterZuKlein = new List<string>();
            for (int gi = -1; gi < _abbild.Gebaeude.Count; gi++)
            {
                List<AbbildBauteil> liste = gi < 0 ? _abbild.BauteileOhneGebaeude : _abbild.Gebaeude[gi].Bauteile;
                List<AbbildBauteil> waende = liste.Where(b => koerper.ContainsKey(b) && b.Koerperflaeche != null && b.Koerperflaeche.Teile.Count > 0).ToList();
                var ziele = new HashSet<AbbildBauteil>();
                // Nur Wände, deren Fläche die Datei nennt: Ist die Körperfläche selbst kleiner als die Öffnungen, bleibt es bei
                // Netto 0 (G5-2, B2).
                foreach (AbbildBauteil b in waende.Where(b => b.Flaechenherkunft != Flaechenherkunft.Koerper && ZuKlein(b)).ToList())
                {
                    zuKlein++;
                    List<AbbildBauteil> eigeneTeile = b.Name == null ? new List<AbbildBauteil>()
                        : teile[(gi, b.Quelltyp, Namensstamm(b.Name))].Where(t => t != b && liste.Contains(t) && t.BruttoflaecheM2 > 0.0).ToList();
                    var lagen = new List<(AbbildBauteil Oeffnung, double[] Mitte, double? Eigen, double[] Normale)>();
                    foreach (AbbildBauteil o in b.Oeffnungen.Where(IstOeffnung))
                    {
                        double[] mitte = Oeffnungsmitte(o);
                        if (mitte == null) continue;   // ohne Lage gilt die Zuordnung der Datei
                        double? eigen = Abstand(b, mitte, out double[] n);
                        lagen.Add((o, mitte, eigen, n));
                    }
                    // Erst die Öffnungen außerhalb der eigenen Wand, dann die großen zuerst.
                    foreach ((AbbildBauteil o, double[] mitte, double? eigen, double[] n) in
                             lagen.OrderBy(x => x.Eigen.HasValue).ThenByDescending(x => x.Oeffnung.BruttoflaecheM2 ?? 0.0).ToList())
                    {
                        if (eigen.HasValue && !ZuKlein(b)) continue;
                        double flaeche = o.BruttoflaecheM2 ?? 0.0;
                        AbbildBauteil ziel = null;
                        if (eigen.HasValue)
                            ziel = eigeneTeile.Where(t => Frei(t) + OEFFNUNG_UEBERSTAND_M2 >= flaeche)
                                              .OrderByDescending(t => GleicheRichtung(t, n)).ThenByDescending(Frei).FirstOrDefault();
                        ziel ??= waende.Where(w => w != b && Frei(w) + OEFFNUNG_UEBERSTAND_M2 >= flaeche)
                                       .Select(w => (Wand: w, Abstand: Abstand(w, mitte, out _))).Where(x => x.Abstand.HasValue)
                                       .OrderBy(x => x.Abstand.Value).Select(x => x.Wand).FirstOrDefault();
                        // Zuletzt, für eine Öffnung in der eigenen Wand: eine Wand desselben Geschosses an derselben Fassade
                        // (gleiche Randbedingung, Himmelsrichtung bis 45°) mit Platz, die größte freie Fläche zuerst.
                        if (ziel == null && eigen.HasValue && b.AzimutGrad is double richtung)
                            ziel = liste.Where(w => w != b && w.Art == b.Art && w.Randbedingung == b.Randbedingung && w.GeschossKennung == b.GeschossKennung
                                                    && w.AzimutGrad is double aw && Math.Abs(((aw - richtung) % 360.0 + 540.0) % 360.0 - 180.0) <= 45.0
                                                    && Frei(w) + OEFFNUNG_UEBERSTAND_M2 >= flaeche)
                                        .OrderByDescending(Frei).FirstOrDefault();
                        if (ziel == null)
                        {
                            if (!eigen.HasValue) ohneWand.Add(Name(o));
                            continue;
                        }
                        b.Oeffnungen.Remove(o);
                        ziel.Oeffnungen.Add(o);
                        o.Randbedingung = ziel.Randbedingung;
                        if (o.NeigungGrad == b.NeigungGrad) o.NeigungGrad = ziel.NeigungGrad;
                        ziele.Add(ziel);
                        umgehaengt.Add(Name(o));
                    }
                    if (ZuKlein(b)) weiterZuKlein.Add(Name(b));
                }
                zielwaende += ziele.Count;
            }
            if (umgehaengt.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "OEFFNUNG_UMGEHAENGT", Ganz(zuKlein), Ganz(umgehaengt.Count),
                    Ganz(zielwaende), Beispiele(umgehaengt)));
            if (ohneWand.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "OEFFNUNG_OHNE_WAND", Ganz(ohneWand.Count), Beispiele(ohneWand)));
            if (weiterZuKlein.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "WAND_KLEINER_OEFFNUNGEN", Ganz(weiterZuKlein.Count), Beispiele(weiterZuKlein)));
        }

        /// <summary>Die Mitte einer Öffnung: ihr Öffnungskörper, sonst ihr Füllkörper; <c>null</c> ohne Körper.</summary>
        private double[] Oeffnungsmitte(AbbildBauteil o)
        {
            if (_oeffnungsquelle.TryGetValue(o, out (IIfcOpeningElement Oeffnung, IIfcElement Element) q))
                return (q.Oeffnung != null ? Mitte(Rechenkoerper(q.Oeffnung)) : null)
                       ?? Mitte(o.Koerper ?? (q.Element != null ? Rechenkoerper(q.Element) : null));
            return Mitte(o.Koerper);
        }

        /// <summary>
        /// Ist das Raumpaar <paramref name="p"/> schon über ein Bauteil des Körperwegs verbunden (zwei Gegenstücke der Herkunft
        /// <see cref="Grenzherkunft.Bauteilkoerper"/> an diesen Räumen, Normale bis <see cref="KF.PARALLEL_GRAD"/>
        /// parallel)? Dann bildet <see cref="Koerpertrennflaechen"/> kein zweites Trennbauteil.
        /// </summary>
        private static bool Abgedeckt(AbbildGebaeude g, Koerperpaar p)
        {
            string ra = g.Raeume[p.RaumA].Kennung, rb = g.Raeume[p.RaumB].Kennung;
            double cosMax = Math.Cos(KF.PARALLEL_GRAD * Math.PI / 180.0);
            foreach (AbbildBauteil b in g.Bauteile)
            {
                if (b.Grenzen.Count == 0) continue;
                var jeKennung = b.Grenzen.Where(x => x.Herkunft == Grenzherkunft.Bauteilkoerper).ToDictionary(x => x.Kennung, StringComparer.Ordinal);
                foreach (AbbildGrenze x in jeKennung.Values)
                {
                    if (x.RaumKennung != ra || x.GegenstueckKennung == null || x.Normale == null) continue;
                    if (!jeKennung.TryGetValue(x.GegenstueckKennung, out AbbildGrenze y) || y.RaumKennung != rb) continue;
                    if (Math.Abs(KF.Punkt(x.Normale, p.NormaleA)) >= cosMax) return true;
                }
            }
            return false;
        }
    }
}
