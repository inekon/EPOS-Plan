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

            // (3) Typaufbau, auf das U abgeglichen.
            if (u is double uz && Typaufbau(s, b, uz) is Ersatzergebnis e)
                return new Pruefvorschlag(Vorschlagsart.Aufbau, Vorschlagsherkunft.Typaufbau,
                                          Modellpruefung.Ressource("MP_VORSCHLAG_TYPAUFBAU", e.Typ.Bezeichner, Zahl(uz)),
                                          Typaufbau: e.Typ.Code, U: uz);
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
