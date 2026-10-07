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
                if (g.ZahlGrenzen > 0 || !g.Raeume.Any(r => r.Koerper != null)) continue;
                Raumflaechen(g);
                if (KoerperflaechenAus) continue;

                var mitBezug = new HashSet<string>(StringComparer.Ordinal);
                foreach (int label in _raumbezug.Keys)
                    if (_modell.Instances[label] is IIfcRoot wurzel) mitBezug.Add(wurzel.GlobalId.ToString());

                double untersterBoden = g.Raeume.Where(r => r.Koerper != null && r.Koerper.PunkteM.Count > 0)
                                                .Min(r => r.Koerper.PunkteM.Min(p => p[2]));
                List<Koerperflaechenraum> raeume = g.Raeume.Select(r =>
                {
                    if (r.Koerper == null || r.Koerper.PunkteM.Count == 0) return new Koerperflaechenraum();
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
                if (r.Koerper == null || r.FlaecheM2.HasValue) continue;
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
