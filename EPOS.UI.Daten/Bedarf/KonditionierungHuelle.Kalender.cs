using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kalenderbedienung an der Oberfläche</b> (Konzept Konditionierungsprofile 7.8, E110; Welle K1b): die
    /// Übersetzung zwischen den Sätzen der Seite (<see cref="KalenderAnsicht"/>, <see cref="KalenderZeilenschluessel"/>,
    /// <see cref="KalenderWirkungsangabe"/>, <see cref="KalenderProfilort"/>) und der Kern-Schicht
    /// <see cref="Kalenderbedienung"/>. Werte gehen wie bei <see cref="Konditionierungsarbeit"/> in der Anzeigeeinheit
    /// hinaus (Geräte und Personen in %) und als Anteil herein; geschrieben wird allein im OK-Weg des Editors.
    /// </summary>
    internal static partial class KonditionierungHuelle
    {
        /// <summary>Die Ansicht einer Größe am Ort; <c>null</c> bei einem ungültigen Stand.</summary>
        internal static KalenderAnsicht Kalenderansicht(KonditionierungStand s, Kalendereigentuemer art, Bezug bezug,
                                                        KonditionierungOrt o)
        {
            if (s?.Gebaeude == null || o == null) return null;
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(s, art, bezug);
                Konditionierungsort ort = Ort(o);
                bool anteil = Konditionierungsgroessen.HatNennwert(ort.Groesse);
                IReadOnlyList<Rastertag> raster = Kalenderbedienung.Jahresraster(a, ort);
                return new KalenderAnsicht
                {
                    Profile = Kalenderbedienung.Wochenprofile(a, ort)
                        .Select(w => new KalenderWochenprofil(w.Rang, w.Name ?? "", w.Werte.Select(v => Skaliert(v, anteil, true)).ToArray(), w.IdWoche))
                        .ToList(),
                    Zuordnungen = Kalenderbedienung.Zuordnungen(a, ort.Zone).Select(z => Zeile(z, a.Ebene(ort.Zone)?.Wochen)).ToList(),
                    Ferien = Kalenderbedienung.Ferienzeitraeume(a).Select(f => new KalenderFerienzeile(f.Name, f.Beginn, f.Ende)).ToList(),
                    Raster = raster.Select(t => new KalenderRastertag(t.Tag, t.Monat, t.TagImMonat, t.Wochentag, (KalenderTagart)(int)t.Art,
                                                                       t.Quelle, Schluessel(t.Schluessel))).ToList(),
                    Band = Kalenderbedienung.Jahresband(raster)
                        .Select(b => new KalenderBandabschnitt(b.Beginn, b.Ende, (KalenderTagart)(int)b.Art, b.Quelle, Schluessel(b.Schluessel)))
                        .ToList(),
                    Rangwarnungen = Kalenderbedienung.Rangabweichungen(a, ort.Zone)
                        .Select(w => new KalenderRangwarnung(w.Erste.Name, w.Zweite.Name, Oberflaeche(w.Oben), Oberflaeche(w.Unten)))
                        .ToList(),
                    Wochenendtage = Kalenderbedienung.Wochenendtage(a),
                    Feiertagsland = Kalenderbedienung.Feiertagsland(a),
                    Feiertagslaender = Landesfeiertage.BUNDESLAENDER,
                };
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Eine Zuordnungszeile des Kerns an der Oberfläche; die Wirkung ist die der ersten Größe.</summary>
        private static KalenderZuordnungszeile Zeile(Zuordnungszeile z, IReadOnlyList<BenannteWoche> wochen)
        {
            IReadOnlyList<Konditionierungsgroesse> gilt = z.GiltFuer;
            KalenderWirkungsangabe wirkung = KalenderWirkungsangabe.Abgeschaltet;
            if (gilt.Count > 0 && z.Angaben.TryGetValue(gilt[0], out Kalenderangabe a))
            {
                bool anteil = Konditionierungsgroessen.HatNennwert(gilt[0]);
                wirkung = a.Art switch
                {
                    Angabeart.Woche => new KalenderWirkungsangabe(KalenderWirkung.Wochenprofil, wochen?.FirstOrDefault(w => w.Id == z.IdWoche)?.Name ?? z.Schluessel.Name),
                    Angabeart.Aus => KalenderWirkungsangabe.Abgeschaltet,
                    Angabeart.Wert => new KalenderWirkungsangabe(KalenderWirkung.Wert, null, 7, Skaliert(a.Wert, anteil, true)),
                    _ => new KalenderWirkungsangabe(KalenderWirkung.WieWochentag, null, a.WieWochentag),
                };
            }
            return new KalenderZuordnungszeile(Schluessel(z.Schluessel), z.ErsterTag, gilt.Select(Oberflaeche).ToList(),
                                               z.Raenge.ToDictionary(x => Oberflaeche(x.Key), x => x.Value), wirkung, z.IstFerien,
                                               z.Maske, z.IstGemeinsam, z.IdWoche);
        }

        /// <summary>Der Schlüssel des Kerns an der Oberfläche; <c>null</c> bleibt <c>null</c>.</summary>
        internal static KalenderZeilenschluessel Schluessel(Zuordnungsschluessel s)
            => s == null ? null : new KalenderZeilenschluessel(s.Name, s.Beginn, s.Ende, s.Feiertagsregel);

        /// <summary>Der Schlüssel der Oberfläche im Kern; <c>null</c> bleibt <c>null</c>.</summary>
        internal static Zuordnungsschluessel Schluessel(KalenderZeilenschluessel s)
            => s == null ? null
               : s.IstFeiertag ? Zuordnungsschluessel.Feiertag(s.Name, s.Feiertagsregel)
               : Zuordnungsschluessel.Zeitraum(s.Name, s.Beginn, s.Ende);

        /// <summary>Der Profilort der Oberfläche im Kern.</summary>
        private static Profilort Profilort(KalenderProfilort p) => new Profilort(Ort(p.Ort), p.Rang, p.IdWoche);

        /// <summary>
        /// Die Wirkung der Oberfläche im Kern. Ein Wert kommt in der Anzeigeeinheit herein und wird als Anteil
        /// gerechnet, wenn jede gewählte Größe einen Anteil führt (Geräte, Personen); die Grenzen prüft der Schreibweg.
        /// </summary>
        internal static Zuordnungsangabe Wirkung(KalenderWirkungsangabe w, IReadOnlyList<KonditionierungGroesse> gilt)
        {
            if (w == null) throw new ArgumentException("Eine Zuordnung ohne Wirkung.", nameof(w));
            bool anteil = gilt is { Count: > 0 } && gilt.All(g => Konditionierungsgroessen.HatNennwert(Kern(g)));
            return w.Wirkung switch
            {
                KalenderWirkung.Wochenprofil => Zuordnungsangabe.Profilwoche(w.Profil ?? ""),
                KalenderWirkung.Aus => Zuordnungsangabe.Abgeschaltet,
                KalenderWirkung.WieWochentag => Zuordnungsangabe.AlsWochentag(w.Wochentag),
                _ => w.Wert is double v
                    ? Zuordnungsangabe.AlsWert(Skaliert(v, anteil, false))
                    : throw new ArgumentException("Eine Wertangabe ohne Wert.", nameof(w)),
            };
        }
    }
}
