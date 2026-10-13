using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorschlagsregeln der Modellprüfung</b> (Entwurf Geführter Modellaufbau 2.3 und 2.4, E137 F3 und F6) — rein,
    /// ohne Datenbank; ausgeführt werden die Vorschläge in <c>ModellpruefungCtrl</c>.
    ///
    /// <para><b>Aufbau</b> eines Bauteils ohne Aufbau (F3): (1) der Aufbau des Projekts gleicher Bauteilart und Randbedingung,
    /// der im Gebäude die meiste Fläche trägt (bei Gleichstand der mit dem nächsten U), (2) ein geeigneter Katalogaufbau gleicher
    /// Bauteilart mit dem nächsten U — liegt ein U vor, nur innerhalb des Bands <see cref="KATALOG_U_BAND"/> —, (3) der
    /// Typaufbau, auf das U abgeglichen (<see cref="Ersatzaufbau.Bilden"/>; nur mit U). Das U des Bauteils bleibt
    /// (E95 Nr. 1); der Aufbau liefert Schichten und Wärmekapazität.</para>
    ///
    /// <para><b>Profil</b> einer Zone ohne Profil (F6): DIN-Nummer → Raumtyp der Quelle (bzw. Nutzungsklasse) → Gebäudeart →
    /// EPOS-Muster „Wohnen“ (Wohngebäude) bzw. „Sonstige“; die Kalender legt der Generator an.</para>
    /// </summary>
    internal static class Modellvorschlagsregeln
    {
        /// <summary>Höchste relative U-Abweichung eines Katalogaufbaus, wenn das Bauteil ein U trägt (25 %).</summary>
        internal const double KATALOG_U_BAND = 0.25;

        private static string Zahl(double w) => Math.Round(w, 3).ToString("0.###", CultureInfo.CurrentCulture);

        private static double Abstand(double? u, double? ziel)
            => u is double a && ziel is double b ? Math.Abs(a - b) : double.MaxValue;

        /// <summary>Der Baualtersbuchstabe des Gebäudes (A … L) oder <c>null</c>.</summary>
        internal static char? Klasse(string baualtersklasse)
        {
            string k = baualtersklasse?.Trim();
            return !string.IsNullOrEmpty(k) && char.IsLetter(k[0]) ? char.ToUpperInvariant(k[0]) : (char?)null;
        }

        /// <summary><b>Der Aufbauvorschlag</b> eines Bauteils ohne Aufbau; <c>null</c>, wenn keine Stufe greift.</summary>
        internal static Pruefvorschlag Aufbau(Modellpruefstand s, BauteilModel b)
        {
            double? u = b.U_Wert is double w && w > 0.0 ? w : (double?)null;
            string rand = Modellpruefung.Rand(b);

            // (1) Projektaufbau gleicher Bauteilart und Randbedingung mit der meisten Fläche im Gebäude.
            var projekt = s.Zonen.SelectMany(z => z.Bauteile ?? new List<BauteilModel>())
                .Where(x => x.ID_Aufbau is int id && x.Bauteilart == b.Bauteilart && Modellpruefung.Rand(x) == rand
                            && s.Projektaufbauten.TryGetValue(id, out Aufbaukennung k) && !k.Ungeeignet && !k.SchichtOhneStoffwert)
                .GroupBy(x => x.ID_Aufbau.Value)
                .Select(g => (Aufbau: s.Projektaufbauten[g.Key], Flaeche: g.Sum(x => x.Flaeche)))
                .OrderByDescending(x => x.Flaeche).ThenBy(x => Abstand(x.Aufbau.U, u)).ThenBy(x => x.Aufbau.Id)
                .FirstOrDefault();
            if (projekt.Aufbau != null)
                return new Pruefvorschlag(Vorschlagsart.Aufbau, Vorschlagsherkunft.Projektaufbau,
                                          Modellpruefung.Ressource("MP_VORSCHLAG_PROJEKTAUFBAU", projekt.Aufbau.Bezeichner),
                                          IdAufbau: projekt.Aufbau.Id, U: projekt.Aufbau.U);

            // (2) Katalogaufbau: geeignet, gleiche Bauteilart, U-Nähe (im Band, wenn ein U vorliegt), sonst Katalogfolge.
            Aufbaukennung katalog = s.Katalogaufbauten
                .Where(k => k.Bauteilart == b.Bauteilart && !k.Ungeeignet && !k.SchichtOhneStoffwert && k.U > 0.0)
                .Where(k => !(u is double z) || Math.Abs(k.U.Value - z) <= KATALOG_U_BAND * z)
                .OrderBy(k => Abstand(k.U, u)).ThenBy(k => k.Folge)
                .FirstOrDefault();
            if (katalog != null)
                return new Pruefvorschlag(Vorschlagsart.Aufbau, Vorschlagsherkunft.Katalogaufbau,
                                          Modellpruefung.Ressource("MP_VORSCHLAG_KATALOGAUFBAU", katalog.Bezeichner, Zahl(katalog.U.Value)),
                                          IdAufbau: katalog.Id, U: katalog.U);

            // (3) Typaufbau, auf das U abgeglichen. Die Saat wird per Eigenschaftsmuster gelesen: ein Punktzugriff
            // auf die Eigenschaft Typ hieße für die Modultrennungswache den Gebäudetyp des Altwegs (E20).
            if (u is double uz && Typaufbau(s, b, uz) is { Typ: TypaufbauSaat saat })
                return new Pruefvorschlag(Vorschlagsart.Aufbau, Vorschlagsherkunft.Typaufbau,
                                          Modellpruefung.Ressource("MP_VORSCHLAG_TYPAUFBAU", saat.Bezeichner, Zahl(uz)),
                                          Typaufbau: saat.Code, U: uz);
            return null;
        }

        /// <summary>Der Typaufbau eines Bauteils, auf <paramref name="u"/> abgeglichen; <c>null</c>, wenn das Bauteil keinen erhält.</summary>
        internal static Ersatzergebnis Typaufbau(Modellpruefstand s, BauteilModel b, double u)
        {
            if (!(GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart) is Bauteilart art)
                || !(GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung) is Bauteilrand rand)
                || !Ersatzaufbau.Erhaelt(art, rand)) return null;
            double neigung = b.Neigung ?? BauteilEingang.VorgabeNeigung(art);
            return Ersatzaufbau.Bilden(art, rand, neigung, null, Klasse(s.Baualtersklasse), u);
        }

        /// <summary>
        /// <b>Das fehlende Hüllbauteil einer Zone</b> (MA-3; Entwurf 2.2 Gruppe 1, Mehrzonenkonzept 5.3) — ein waagerechtes Bauteil
        /// mit der Fläche der Lücke <paramref name="flaeche"/> in Richtung <paramref name="oben"/>, nur mit U-Wert (Stufe C; den
        /// Aufbau schlägt danach die Gruppe 2 vor). Art und Randbedingung nach der Lage:
        /// <list type="number">
        /// <item>Ist die Zone in dieser Richtung über eine Trennfläche an einer Nachbarzone verankert und fehlt dieser die Fläche
        /// der Gegenrichtung, eine Decke an der Nachbarzone (<c>ZONE</c>) — sie schließt beide. Ist sie verankert, ohne dass der
        /// Nachbarzone etwas fehlt, liegt die Lücke nicht an der Trennfläche (etwa ein geneigtes Dach mit Neigung 0°): kein
        /// Vorschlag.</item>
        /// <item>Sonst nach oben ein Dach an Außenluft (Neigung 0°).</item>
        /// <item>Sonst nach unten eine Bodenplatte gegen Erdreich (Neigung 180°): Eine Zone, unter der keine Zone verankert ist,
        /// liegt im untersten Geschoss.</item>
        /// </list>
        /// Der U-Wert: (1) der eines Bauteils gleicher Art und Randbedingung im Gebäude (das U mit der meisten Fläche; das U des
        /// Bauteils, sonst das seines Projektaufbaus), (2) sonst das U des Typaufbaus (<see cref="Ersatzaufbau.Vorgabetyp"/>
        /// ohne Abgleich, an einer Trennfläche wie gegen unbeheizt). <c>null</c>, wenn keine Stufe greift.
        /// </summary>
        internal static Pruefvorschlag Huellbauteil(Modellpruefstand s, IList<ZoneModel> zonen, int index, bool oben, double flaeche)
        {
            if (!(flaeche > 0.0)) return null;
            ZoneModel zone = zonen[index];
            double neigung = oben ? 0.0 : 180.0;

            // (1) Verankert an einer Nachbarzone in dieser Richtung?
            var nachbarn = new HashSet<int>();
            foreach (BauteilModel b in zone.Bauteile ?? new List<BauteilModel>())
                if (b != null && b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE && b.ID_Nachbarzone is int n && Richtung(b, false) == oben)
                    nachbarn.Add(n);
            foreach (ZoneModel andere in zonen)
                if (andere != null && andere.ID != zone.ID)
                    foreach (BauteilModel b in andere.Bauteile ?? new List<BauteilModel>())
                        if (b != null && b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE && b.ID_Nachbarzone == zone.ID && Richtung(b, true) == oben)
                            nachbarn.Add(andere.ID);
            string art, rand;
            int? idNachbar = null;
            if (nachbarn.Count > 0)
            {
                // Der Nachbar, dem in der Gegenrichtung am meisten fehlt.
                (int Id, double Fehlt) best = (0, 0.0);
                foreach (int n in nachbarn.OrderBy(x => x))
                {
                    int j = Zonenkopplungsregeln.Index(zonen.ToList(), n);
                    if (j < 0) continue;
                    Zonenkopplungsregeln.Huellbilanz hn = Zonenkopplungsregeln.Bilanz(zonen, j);
                    double fehlt = oben ? hn.Oben - hn.Unten : hn.Unten - hn.Oben;   // der Nachbar braucht die Gegenrichtung
                    if (fehlt > best.Fehlt + 1e-9) best = (n, fehlt);
                }
                if (!(best.Fehlt > 1e-6)) return null;
                idNachbar = best.Id;
                art = DbWerte.BAUTEILART_DECKE;
                rand = DbWerte.RANDBEDINGUNG_ZONE;
            }
            else if (oben)
            {
                art = DbWerte.BAUTEILART_DACH;
                rand = DbWerte.RANDBEDINGUNG_AUSSENLUFT;
            }
            else
            {
                art = DbWerte.BAUTEILART_BODENPLATTE;
                rand = DbWerte.RANDBEDINGUNG_ERDREICH;
            }

            (double? u, Vorschlagsherkunft herkunft) = HuellU(s, art, rand, neigung);
            if (!(u > 0.0)) return null;
            string randText = rand == DbWerte.RANDBEDINGUNG_ZONE
                ? Modellpruefung.Ressource("MP_RAND_ZONE", zonen.FirstOrDefault(x => x?.ID == idNachbar)?.Bezeichner)
                : Modellpruefung.Ressource("MP_RAND_" + rand);
            string text = Modellpruefung.Ressource(herkunft == Vorschlagsherkunft.Projekt ? "MP_VORSCHLAG_HUELLBAUTEIL_PROJEKT" : "MP_VORSCHLAG_HUELLBAUTEIL_TYP",
                                                   BauteilaufbauCtrl.BauteilartText(art), Zahl(Math.Round(flaeche, 1)), Zahl(neigung), randText, Zahl(u.Value));
            return new Pruefvorschlag(Vorschlagsart.Huellbauteil, herkunft, text, Name: art, U: u, Randbedingung: rand,
                                      Neigung: neigung, Flaeche: flaeche, IdNachbarzone: idNachbar);
        }

        /// <summary>
        /// Weist das Bauteil nach oben (<c>true</c>), nach unten (<c>false</c>) oder steht es steil (<c>null</c>, Neigung zwischen
        /// 60° und 120°)? <paramref name="gespiegelt"/>: von der Nachbarzone aus gesehen.
        /// </summary>
        private static bool? Richtung(BauteilModel b, bool gespiegelt)
        {
            double n = b.Neigung ?? GebaeudeZonenCtrl.NeigungVorgabe(b.Bauteilart);
            if (gespiegelt) n = 180.0 - n;
            double c = Math.Cos(n * Math.PI / 180.0);
            return c > 0.5 ? true : c < -0.5 ? false : (bool?)null;
        }

        /// <summary>Der U-Wert des ergänzten Hüllbauteils und seine Herkunft (Regel bei <see cref="Huellbauteil"/>).</summary>
        internal static (double? U, Vorschlagsherkunft Herkunft) HuellU(Modellpruefstand s, string art, string rand, double neigung)
        {
            var jeU = new Dictionary<double, double>();
            foreach (BauteilModel b in s.Zonen.SelectMany(z => z.Bauteile ?? new List<BauteilModel>()))
            {
                if (b == null || b.Bauteilart != art || Modellpruefung.Rand(b) != rand || !(b.Flaeche > 0.0)) continue;
                double? u = b.U_Wert is double w && w > 0.0 ? w
                    : b.ID_Aufbau is int id && s.Projektaufbauten.TryGetValue(id, out Aufbaukennung k) && k.U > 0.0 ? k.U : null;
                if (!(u is double uw)) continue;
                double r = Math.Round(uw, 6);
                jeU[r] = (jeU.TryGetValue(r, out double a) ? a : 0.0) + b.Flaeche;
            }
            if (jeU.Count > 0)
                return (jeU.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First().Key, Vorschlagsherkunft.Projekt);

            if (!(GebaeudeZonenabbildung.ArtAusZeile(art) is Bauteilart kern)) return (null, Vorschlagsherkunft.Typaufbau);
            Bauteilrand r0 = rand == DbWerte.RANDBEDINGUNG_ERDREICH ? Bauteilrand.Erdreich
                             : rand == DbWerte.RANDBEDINGUNG_AUSSENLUFT ? Bauteilrand.Aussenluft : Bauteilrand.Unbeheizt;
            TypaufbauSaat saat = Ersatzaufbau.Vorgabetyp(kern, r0, neigung, null, Klasse(s.Baualtersklasse), 0.0);
            double? ut = saat == null ? null : Ersatzaufbau.U(TypaufbauSaattabelle.AlsModell(saat), neigung, r0);
            return (ut is double x && x > 0.0 ? Math.Round(x, 3) : (double?)null, Vorschlagsherkunft.Typaufbau);
        }

        /// <summary>Das Bauteil, das ein Hüllbauteil-Vorschlag anlegt (Bezeichner aus der Ressource, Herkunft Vorgabe).</summary>
        internal static BauteilModel AlsBauteil(Pruefvorschlag v, int idZone) => new BauteilModel
        {
            ID_Zone = idZone,
            Bezeichner = Modellpruefung.Ressource("MP_BAUTEIL_ERGAENZT", BauteilaufbauCtrl.BauteilartText(v.Name)),
            Bauteilart = v.Name,
            Flaeche = v.Flaeche ?? 0.0,
            U_Wert = v.U,
            Neigung = v.Neigung,
            Randbedingung = v.Randbedingung,
            ID_Nachbarzone = v.IdNachbarzone,
            Herkunft = DbWerte.HERKUNFT_VORGABE,
        };

        /// <summary>
        /// <b>Das Profil einer Zone</b> nach der Regel F6 samt Herkunft; <c>(null, …)</c>, wenn keine Stufe ein Profil trägt.
        /// </summary>
        internal static (Planprofil Profil, Vorschlagsherkunft Herkunft) ProfilNachRegel(Raumnutzungsvorbelegung v, Zonenquelle q,
                                                                                        string gebaeudeart, string wohngebaeude)
        {
            v ??= Raumnutzungsvorbelegung.Vorgabe;
            if (q?.DinNummer is int din && v.AusDinNummer(din) is Planprofil p1 && !p1.NichtImKatalog)
                return (p1, Vorschlagsherkunft.DinNummer);
            if (!string.IsNullOrWhiteSpace(q?.Raumtyp)
                && v.Aufloesen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, q.Raumtyp) is Planprofil p2 && !p2.NichtImKatalog)
                return (p2, Vorschlagsherkunft.Raumtyp);
            if (!string.IsNullOrWhiteSpace(q?.Nutzungsklasse) && v.AusKlasse(q.Nutzungsklasse) is Planprofil p3 && !p3.NichtImKatalog)
                return (p3, Vorschlagsherkunft.Raumtyp);
            if (Zonenvorbesetzung.AusGebaeudeart(v, gebaeudeart) is Planprofil p4)
                return (p4, Vorschlagsherkunft.Gebaeudeart);
            bool wohnen = (wohngebaeude ?? "").Trim().StartsWith("Wohn", StringComparison.OrdinalIgnoreCase);
            Planprofil p5 = v.AusKennung(wohnen ? DbWerte.KOND_NUTZUNG_WOHNEN : DbWerte.KOND_NUTZUNG_SONSTIGE);
            return (p5, Vorschlagsherkunft.EposMuster);
        }

        /// <summary><b>Der Profilvorschlag</b> einer Zone ohne Profil.</summary>
        internal static Pruefvorschlag Profil(Modellpruefstand s, ZoneModel z)
        {
            s.Quellen.TryGetValue(z.ID, out Zonenquelle q);
            (Planprofil p, Vorschlagsherkunft h) = ProfilNachRegel(s.Vorbelegung, q, s.Gebaeudeart, s.Wohngebaeude);
            if (p == null) return null;
            return new Pruefvorschlag(Vorschlagsart.Profil, h,
                                      Modellpruefung.Ressource("MP_VORSCHLAG_PROFIL_" + h.ToString().ToUpperInvariant(), p.Name),
                                      IdProfil: p.Id, Name: p.Name);
        }
    }
}
