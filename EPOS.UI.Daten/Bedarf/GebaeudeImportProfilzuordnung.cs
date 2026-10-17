using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Import;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Profilzuordnung je Raum als Daten des Dialogs</b> (Entwurf Zonenzuordnung IFC, Abschnitt 6; Auftrag ZZ-B) —
    /// plattformfrei neben <see cref="GebaeudeImportZonen"/>, ohne Datenbank: der Kern liest den Katalog über
    /// <see cref="Zonenplan.Vorbelegung"/>. Übersetzt <see cref="Zonenplan.Buendel"/> und <see cref="Zonenplan.Katalogbaum"/>
    /// in die DTO der Überlagerung „Nutzungsprofile zuordnen…“ und legt die vier Schritte der Profilzuordnung auf den Plan
    /// (<see cref="GebaeudePlanschrittArt.PROFIL"/>, <see cref="GebaeudePlanschrittArt.PROFIL_ZURUECK"/>,
    /// <see cref="GebaeudePlanschrittArt.VORSCHLAEGE"/>, <see cref="GebaeudePlanschrittArt.VORSCHLAEGE_ZURUECK"/>).
    /// </summary>
    internal static class GebaeudeImportProfilzuordnung
    {
        /// <summary>Die Profilzuordnung des Plans; <c>null</c>, wenn der Plan keine Raumprofile trägt (andere Formate, Proben ohne Raumart).</summary>
        internal static GebaeudeProfilzuordnungDaten Daten(Zonenplan plan)
        {
            if (plan == null || plan.Raumprofile.Count == 0) return null;
            Raumnutzungsvorbelegung v = plan.Vorbelegung;
            IReadOnlyList<Raumprofilbuendel> buendel = plan.Buendel();
            Raumprofilzaehlung n = Raumprofilzuordnung.Zaehlen(plan.Raumprofile);
            string ampel = n.Rueckfrage > 0 ? GebaeudeProfilampel.ROT : n.Vorschlag > 0 ? GebaeudeProfilampel.GELB : GebaeudeProfilampel.GRUEN;
            string ampeltext = ampel == GebaeudeProfilampel.ROT ? Formatieren(MyResource.Resource.GIMP_PZ_AMPEL_RUECKFRAGE, n.Rueckfrage)
                             : ampel == GebaeudeProfilampel.GELB ? Formatieren(MyResource.Resource.GIMP_PZ_AMPEL_VORSCHLAG, n.Vorschlag)
                             : MyResource.Resource.GIMP_PZ_AMPEL_GRUEN;
            List<GebaeudeProfilbuendelDaten> zeilen = buendel.Select(b => Buendeldaten(v, b)).ToList();
            return new GebaeudeProfilzuordnungDaten
            {
                Buendel = zeilen,
                Katalog = plan.Katalogbaum().Select(k => Knoten(v, k, "")).ToList(),
                Eindeutig = n.Eindeutig,
                Vorschlag = n.Vorschlag,
                Rueckfrage = n.Offen,
                Ohne = n.Ohne,
                Hand = n.Hand,
                OffeneBuendel = zeilen.Count(z => z.Offen),
                Zaehlertext = Formatieren(MyResource.Resource.GIMP_PZ_ZAEHLER, n.Eindeutig, n.Vorschlag, n.Offen, n.Ohne, n.Hand),
                Ampel = ampel,
                Ampeltext = ampeltext,
            };
        }

        /// <summary>Der Stand als sprachneutraler Schlüssel.</summary>
        internal static string Stand(Raumprofilstand s) => s switch
        {
            Raumprofilstand.Eindeutig => GebaeudeProfilstand.EINDEUTIG,
            Raumprofilstand.Vorschlag => GebaeudeProfilstand.VORSCHLAG,
            Raumprofilstand.Offen => GebaeudeProfilstand.OFFEN,
            Raumprofilstand.Ohne => GebaeudeProfilstand.OHNE,
            _ => GebaeudeProfilstand.HAND,
        };

        /// <summary>Der Stand als Anzeigetext.</summary>
        internal static string Standtext(Raumprofilstand s) => s switch
        {
            Raumprofilstand.Eindeutig => MyResource.Resource.GIMP_PZ_STAND_EINDEUTIG,
            Raumprofilstand.Vorschlag => MyResource.Resource.GIMP_PZ_STAND_VORSCHLAG,
            Raumprofilstand.Offen => MyResource.Resource.GIMP_PZ_STAND_OFFEN,
            Raumprofilstand.Ohne => MyResource.Resource.GIMP_PZ_STAND_OHNE,
            _ => MyResource.Resource.GIMP_PZ_STAND_HAND,
        };

        /// <summary>
        /// <b>Ein Schritt der Profilzuordnung am Plan</b>; <c>null</c>, wenn die Art keine der vier ist. Die Wahl „keine“ ist der
        /// leere Profilschlüssel; jede Operation bildet den Plan nach seiner Regel neu (Kern).
        /// </summary>
        internal static Planschritt Schritt(Zonenplan plan, GebaeudePlanschritt s)
        {
            IReadOnlyList<string> raeume = s.Raeume ?? Array.Empty<string>();
            return s.Art switch
            {
                GebaeudePlanschrittArt.PROFIL => plan.ProfilZuordnen(raeume, s.Nutzung ?? ""),
                GebaeudePlanschrittArt.PROFIL_ZURUECK => plan.ProfilzuordnungZuruecknehmen(raeume),
                GebaeudePlanschrittArt.VORSCHLAEGE => plan.VorschlaegeUebernehmen(),
                GebaeudePlanschrittArt.VORSCHLAEGE_ZURUECK => plan.VorschlaegeZuruecknehmen(),
                _ => null,
            };
        }

        private static GebaeudeProfilbuendelDaten Buendeldaten(Raumnutzungsvorbelegung v, Raumprofilbuendel b)
        {
            string min = b.FlaecheMinM2.ToString("0", CultureInfo.CurrentCulture);
            string max = b.FlaecheMaxM2.ToString("0", CultureInfo.CurrentCulture);
            return new GebaeudeProfilbuendelDaten
            {
                Schluessel = b.Raumart + "|" + b.Namensstamm,
                Anzeigename = b.Anzeigename,
                Raumart = string.IsNullOrEmpty(b.Raumart) ? "" : Formatieren(MyResource.Resource.GIMP_PZ_RAUMART, b.Raumart),
                Raeume = b.Raeume.Select(r => r.Raum.Kennung).ToList(),
                Flaeche = b.FlaecheMaxM2 <= 0.0 ? MyResource.Resource.GIMP_WERT_LEER : min == max ? min : min + "–" + max,
                Stand = Stand(b.Stand),
                StandText = Standtext(b.Stand),
                Gemischt = b.Gemischt,
                Profil = b.Vorschlag,
                ProfilText = b.Vorschlag == null ? "" : b.Vorschlag.Length == 0 ? MyResource.Resource.GIMP_PZ_KEINE
                                                       : Raumprofilschluessel.Anzeigename(v, b.Vorschlag),
                Begruendung = Raumprofilzuordnung.Begruendungstext(b.Begruendung),
                Kandidaten = b.Kandidaten.Select(k => new GebaeudeZonenregelDaten(k, Raumprofilschluessel.Anzeigename(v, k))).ToList(),
            };
        }

        private static GebaeudeProfilknotenDaten Knoten(Raumnutzungsvorbelegung v, Katalogknoten k, string pfad)
        {
            string schluessel = pfad + "/" + k.Schluessel;
            if (k.Kinder.Count == 0 && k.Profilschluessel != null)
                return new GebaeudeProfilknotenDaten("P:" + schluessel, k.Name, Array.Empty<GebaeudeProfilknotenDaten>(),
                                                     k.Profilschluessel, Kennwerte(k.Profil));
            return new GebaeudeProfilknotenDaten("K:" + schluessel, k.Name, k.Kinder.Select(x => Knoten(v, x, schluessel)).ToList());
        }

        /// <summary>Sollwert und Nutzungszeit als Kurztext; leer, wenn das Profil keinen Sollwert trägt.</summary>
        private static string Kennwerte(Raumnutzungsprofil p)
            => p?.Heiz_Soll is double soll && p.Nutzung_Von is int von && p.Nutzung_Bis is int bis
                ? Formatieren(MyResource.Resource.GIMP_PZ_KENNWERTE, soll.ToString("0.#", CultureInfo.CurrentCulture), von, bis)
                : "";

        private static string Formatieren(string vorlage, params object[] werte)
            => string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte);
    }
}
