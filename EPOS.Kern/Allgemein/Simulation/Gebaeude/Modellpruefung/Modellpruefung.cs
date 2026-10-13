using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Prüfstand eines Gebäudes</b> — alles, was die Modellprüfung liest, als reine Daten (ohne Datenbank): das Gebäude,
    /// seine Zonen samt Bauteilen, die Konditionierung, der Ort, die Aufbauten des Projekts und des Katalogs mit ihren
    /// Kennwerten und die Profilzuordnung. <c>ModellpruefungCtrl</c> füllt ihn aus der Datenbank; Tests füllen ihn von Hand.
    /// </summary>
    internal sealed class Modellpruefstand
    {
        internal int IdGebaeude;
        internal int IdProjekt;
        internal string Gebaeudeart;
        internal string Wohngebaeude;
        internal string Baualtersklasse;
        internal string Feiertagsland;
        internal double? Nutzflaeche, Raumhoehe, FlaecheNutzer, Luftwechselrate, Bauweise, SollTag;
        internal bool KuehlungAktiv;
        internal double? FlaecheAussenwand, UAussenwand, FlaecheFenster, UFenster, FlaecheDach, UDach, FlaecheGrund, UGrund;

        internal List<ZoneModel> Zonen = new List<ZoneModel>();
        internal List<ZonenluftstromModel> Luftstroeme = new List<ZonenluftstromModel>();

        /// <summary>Die Kalendergrößen je Eigentümer: Schlüssel 0 = Gebäude, sonst die Zone.</summary>
        internal Dictionary<int, HashSet<string>> Kalender = new Dictionary<int, HashSet<string>>();
        /// <summary>Die Nutzung der Kalenderkopien je Zone (erste Größe mit Nutzung).</summary>
        internal Dictionary<int, string> Kalendernutzung = new Dictionary<int, string>();
        /// <summary>Die Vorgabezeilen des Gebäudes (ohne Zone): FERIEN, SAISON …</summary>
        internal HashSet<string> Gebaeudevorgaben = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Ferien am Gebäude im Bestandsweg (<c>Tab_Gebaeude.Ferien</c> = 1).</summary>
        internal bool FerienBestand;

        internal bool KlimaregionVorhanden;
        /// <summary>Herkunft des Nordwinkels der jüngsten Importquelle; <c>null</c> = kein Import.</summary>
        internal string Nordwinkelherkunft;

        /// <summary>Ein anderes Gebäude desselben Projekts mit Ferien bzw. Heizperiode (Id) — Quelle des Vorschlags F5.</summary>
        internal int? FerienQuelle, SaisonQuelle;
        /// <summary>Das Feiertagsland eines anderen Gebäudes desselben Projekts.</summary>
        internal string FeiertagslandProjekt;

        internal Dictionary<int, Aufbaukennung> Projektaufbauten = new Dictionary<int, Aufbaukennung>();
        internal List<Aufbaukennung> Katalogaufbauten = new List<Aufbaukennung>();
        internal Raumnutzungsvorbelegung Vorbelegung = Raumnutzungsvorbelegung.Vorgabe;
        /// <summary>Hinweise der Quelle je Zone (DIN-Nummer, Raumtyp, Nutzungsklasse) — nur, wenn der Aufrufer sie kennt.</summary>
        internal Dictionary<int, Zonenquelle> Quellen = new Dictionary<int, Zonenquelle>();

        internal bool Mehrzonen => Zonen.Count > 0;

        internal bool HatKalender(int eigner, string groesse)
            => Kalender.TryGetValue(eigner, out HashSet<string> g) && g.Contains(groesse);

        internal bool HatNutzungskalender => Kalendernutzung.Count > 0;

        /// <summary>Die Nutzung einer Zone: <c>Tab_Zone.Nutzungsprofil</c>, sonst die ihrer Kalenderkopien.</summary>
        internal string Nutzung(ZoneModel z)
            => !string.IsNullOrWhiteSpace(z.Nutzungsprofil) ? z.Nutzungsprofil.Trim()
               : Kalendernutzung.TryGetValue(z.ID, out string n) ? n : null;
    }

    /// <summary>Ein Aufbau mit den Kennwerten, die die Regel braucht.</summary>
    internal sealed record Aufbaukennung(int Id, string Bezeichner, string Bauteilart, bool Ungeeignet, double? U,
                                         double? Kapazitaet, bool SchichtOhneStoffwert, int Folge);

    /// <summary>Die Hinweise der Quelle zu einer Zone (Profilregel 2.4 Stufe 1 und 2).</summary>
    internal sealed record Zonenquelle(int? DinNummer, string Raumtyp, string Nutzungsklasse);

    /// <summary>
    /// <b>Die Modellprüfung</b> (Entwurf Geführter Modellaufbau 2.2, E137): fünf Prüfgruppen über einem
    /// <see cref="Modellpruefstand"/>, je Befund der Vorschlag nach <see cref="Modellvorschlagsregeln"/>. Sie ruft die vorhandenen
    /// Prüfungen (Gruppe 5: <see cref="GebaeudeZonenCtrl.Pruefen(IList{ZoneModel}, IList{ZonenluftstromModel}, double?)"/>)
    /// und rechnet nichts nach. Ohne Datenbank.
    /// </summary>
    internal static class Modellpruefung
    {
        // ---- Befundcodes (stabil; Text: Ressource MP_BEFUND_<Code>) ----
        internal const string ZONE_OHNE_BAUTEILE = "ZONE_OHNE_BAUTEILE";
        internal const string ZONE_OHNE_AUSSENBAUTEIL = "ZONE_OHNE_AUSSENBAUTEIL";
        internal const string HUELLE_OHNE_AUSSENWAND = "HUELLE_OHNE_AUSSENWAND";
        internal const string HUELLE_OHNE_DACH = "HUELLE_OHNE_DACH";
        internal const string HUELLE_OHNE_BODEN = "HUELLE_OHNE_BODEN";
        internal const string BAUTEILART_UNBEKANNT = "BAUTEILART_UNBEKANNT";
        internal const string FLAECHE_FEHLT = "FLAECHE_FEHLT";
        internal const string RANDBEDINGUNG_FEHLT = "RANDBEDINGUNG_FEHLT";
        internal const string AZIMUT_FEHLT = "AZIMUT_FEHLT";
        internal const string EINZONE_FLAECHE_FEHLT = "EINZONE_FLAECHE_FEHLT";
        internal const string EINZONE_U_FEHLT = "EINZONE_U_FEHLT";

        internal const string NUR_U_WERT = "NUR_U_WERT";
        internal const string OHNE_U_OHNE_AUFBAU = "OHNE_U_OHNE_AUFBAU";
        internal const string AUFBAU_UNGEEIGNET = "AUFBAU_UNGEEIGNET";
        internal const string SCHICHT_OHNE_STOFFWERT = "SCHICHT_OHNE_STOFFWERT";
        internal const string KAPAZITAET_FEHLT = "KAPAZITAET_FEHLT";
        internal const string EINZONE_BAUWEISE_FEHLT = "EINZONE_BAUWEISE_FEHLT";

        internal const string ZONE_OHNE_PROFIL = "ZONE_OHNE_PROFIL";
        internal const string HEIZKALENDER_GEERBT = "HEIZKALENDER_GEERBT";
        internal const string UNBEHEIZT_MIT_HEIZKALENDER = "UNBEHEIZT_MIT_HEIZKALENDER";
        internal const string KUEHLKALENDER_FEHLT = "KUEHLKALENDER_FEHLT";
        internal const string LUEFTUNG_GEERBT = "LUEFTUNG_GEERBT";
        internal const string FERIEN_FEHLEN = "FERIEN_FEHLEN";
        internal const string HEIZPERIODE_FEHLT = "HEIZPERIODE_FEHLT";

        internal const string KLIMAREGION_FEHLT = "KLIMAREGION_FEHLT";
        internal const string NORDRICHTUNG_ANGENOMMEN = "NORDRICHTUNG_ANGENOMMEN";
        internal const string FEIERTAGSLAND_FEHLT = "FEIERTAGSLAND_FEHLT";

        internal const string PFLICHTGROESSE_FEHLT = "PFLICHTGROESSE_FEHLT";
        internal const string RECHENWEG_ABGELEHNT = "RECHENWEG_ABGELEHNT";

        /// <summary>Alle Befundcodes je Gruppe (für Ressourcen- und Vollständigkeitstests).</summary>
        internal static readonly IReadOnlyDictionary<Pruefgruppe, string[]> CODES = new Dictionary<Pruefgruppe, string[]>
        {
            [Pruefgruppe.GeometrieBauteile] = new[] { ZONE_OHNE_BAUTEILE, ZONE_OHNE_AUSSENBAUTEIL, HUELLE_OHNE_AUSSENWAND, HUELLE_OHNE_DACH,
                                                     HUELLE_OHNE_BODEN, BAUTEILART_UNBEKANNT, FLAECHE_FEHLT, RANDBEDINGUNG_FEHLT, AZIMUT_FEHLT,
                                                     EINZONE_FLAECHE_FEHLT, EINZONE_U_FEHLT },
            [Pruefgruppe.AufbautenStoffwerte] = new[] { NUR_U_WERT, OHNE_U_OHNE_AUFBAU, AUFBAU_UNGEEIGNET, SCHICHT_OHNE_STOFFWERT, KAPAZITAET_FEHLT,
                                                       EINZONE_BAUWEISE_FEHLT },
            [Pruefgruppe.NutzungKonditionierung] = new[] { ZONE_OHNE_PROFIL, HEIZKALENDER_GEERBT, UNBEHEIZT_MIT_HEIZKALENDER, KUEHLKALENDER_FEHLT,
                                                          LUEFTUNG_GEERBT, FERIEN_FEHLEN, HEIZPERIODE_FEHLT },
            [Pruefgruppe.OrtLage] = new[] { KLIMAREGION_FEHLT, NORDRICHTUNG_ANGENOMMEN, FEIERTAGSLAND_FEHLT },
            [Pruefgruppe.Rechenfaehigkeit] = new[] { PFLICHTGROESSE_FEHLT, RECHENWEG_ABGELEHNT },
        };

        /// <summary>Das Sprungziel je Gruppe (Farbmodus der Gebäudeansicht).</summary>
        internal static Pruefsprungziel Sprungziel(Pruefgruppe g) => g switch
        {
            Pruefgruppe.GeometrieBauteile => Pruefsprungziel.Randbedingung,
            Pruefgruppe.AufbautenStoffwerte => Pruefsprungziel.Aufbau,
            Pruefgruppe.NutzungKonditionierung => Pruefsprungziel.Zonen,
            _ => Pruefsprungziel.Befund,
        };

        /// <summary>Der Text eines Befunds aus der Ressource <c>MP_BEFUND_&lt;Code&gt;</c>.</summary>
        internal static string Text(string code, params object[] a)
            => Ressource("MP_BEFUND_" + code, a);

        /// <summary>Ein Ressourcentext, formatiert in der aktuellen Kultur; der Schlüssel selbst, wenn er fehlt.</summary>
        internal static string Ressource(string schluessel, params object[] a)
        {
            string f = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture) ?? schluessel;
            return a == null || a.Length == 0 ? f : string.Format(CultureInfo.CurrentCulture, f, a);
        }

        private static string Zahl(double w) => Math.Round(w, 3).ToString("0.###", CultureInfo.CurrentCulture);

        /// <summary><b>Prüft den Stand</b> und liefert Rechenstatus und die fünf Gruppen.</summary>
        internal static Modellpruefergebnis Pruefen(Modellpruefstand s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var befunde = new List<Modellbefund>();
            if (s.Mehrzonen)
            {
                GeometrieZonen(s, befunde);
                AufbautenZonen(s, befunde);
                NutzungZonen(s, befunde);
            }
            else
            {
                GeometrieEinzone(s, befunde);
                if (!(s.Bauweise > 0.0))
                    befunde.Add(new Modellbefund(Pruefgruppe.AufbautenStoffwerte, EINZONE_BAUWEISE_FEHLT, Pruefampel.Gelb, Text(EINZONE_BAUWEISE_FEHLT)));
            }
            Konditionierung(s, befunde);
            OrtLage(s, befunde);
            Rechenfaehigkeit(s, befunde);

            var gruppen = new List<Pruefgruppenergebnis>();
            foreach (Pruefgruppe g in Enum.GetValues(typeof(Pruefgruppe)).Cast<Pruefgruppe>().OrderBy(x => (int)x))
            {
                List<Modellbefund> b = befunde.Where(x => x.Gruppe == g).ToList();
                Pruefampel a = b.Count == 0 ? Pruefampel.Gruen : b.Max(x => x.Ampel);
                gruppen.Add(new Pruefgruppenergebnis(g, a, b, Sprungziel(g)));
            }
            Pruefampel gesamt = gruppen.Max(x => x.Ampel);
            Rechenstatus st = gesamt == Pruefampel.Rot ? Rechenstatus.NichtRechenfaehig
                              : gesamt == Pruefampel.Gelb ? Rechenstatus.RechnetMitVorgaben : Rechenstatus.Vollstaendig;
            return new Modellpruefergebnis(s.IdGebaeude, s.Mehrzonen, st, gruppen);
        }

        // =====================================================================
        //  Gruppe 1 — Geometrie und Bauteile
        // =====================================================================

        private static bool IstAussenrand(string rand)
            => rand == null || rand == DbWerte.RANDBEDINGUNG_AUSSENLUFT || rand == DbWerte.RANDBEDINGUNG_ERDREICH
               || rand == DbWerte.RANDBEDINGUNG_UNBEHEIZT;

        /// <summary>Der wirksame Rand eines Bauteils: leer heißt je nach Art „innen“ (<c>null</c>) oder Außenluft.</summary>
        internal static string Rand(BauteilModel b)
            => b.Randbedingung ?? (GebaeudeZonenCtrl.LeerHeisstInnen(b.Bauteilart) ? DbWerte.RANDBEDINGUNG_ZONE : DbWerte.RANDBEDINGUNG_AUSSENLUFT);

        private static void GeometrieZonen(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.GeometrieBauteile;
            bool wand = false, dach = false, boden = false;
            foreach (ZoneModel z in s.Zonen)
            {
                List<BauteilModel> bt = z.Bauteile ?? new List<BauteilModel>();
                if (bt.Count == 0)
                {
                    f.Add(new Modellbefund(G, ZONE_OHNE_BAUTEILE, Pruefampel.Rot, Text(ZONE_OHNE_BAUTEILE, z.Bezeichner), z.ID));
                    continue;
                }
                if (z.IstBeheizt && !bt.Any(b => IstAussenrand(Rand(b)) && Rand(b) != DbWerte.RANDBEDINGUNG_ZONE))
                    f.Add(new Modellbefund(G, ZONE_OHNE_AUSSENBAUTEIL, Pruefampel.Gelb, Text(ZONE_OHNE_AUSSENBAUTEIL, z.Bezeichner), z.ID));
                foreach (BauteilModel b in bt)
                {
                    string rand = Rand(b);
                    bool aussen = rand != DbWerte.RANDBEDINGUNG_ZONE;
                    Bauteilart? art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart);
                    double neigung = b.Neigung ?? (art is Bauteilart a0 ? BauteilEingang.VorgabeNeigung(a0) : 90.0);
                    if (aussen)
                    {
                        if (b.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND || b.Bauteilart == DbWerte.BAUTEILART_VORHANGFASSADE) wand = true;
                        if (b.Bauteilart == DbWerte.BAUTEILART_DACH || (b.Bauteilart == DbWerte.BAUTEILART_DECKE && neigung < 90.0)) dach = true;
                        if (b.Bauteilart == DbWerte.BAUTEILART_BODENPLATTE || (b.Bauteilart == DbWerte.BAUTEILART_DECKE && neigung >= 90.0)) boden = true;
                    }
                    if (art == null || b.Bauteilart == DbWerte.BAUTEILART_SONSTIGES)
                        f.Add(new Modellbefund(G, BAUTEILART_UNBEKANNT, Pruefampel.Gelb, Text(BAUTEILART_UNBEKANNT, b.Bezeichner, z.Bezeichner), z.ID, b.ID, b.Flaeche));
                    if (!(b.Flaeche > 0.0) || double.IsInfinity(b.Flaeche))
                        f.Add(new Modellbefund(G, FLAECHE_FEHLT, Pruefampel.Rot, Text(FLAECHE_FEHLT, b.Bezeichner, z.Bezeichner), z.ID, b.ID));
                    if (b.Randbedingung == null && !GebaeudeZonenCtrl.LeerHeisstInnen(b.Bauteilart))
                        f.Add(new Modellbefund(G, RANDBEDINGUNG_FEHLT, Pruefampel.Gelb, Text(RANDBEDINGUNG_FEHLT, b.Bezeichner, z.Bezeichner), z.ID, b.ID, b.Flaeche));
                    if (!b.Azimut.HasValue && GebaeudeZonenCtrl.BrauchtAzimut(b))
                        f.Add(new Modellbefund(G, AZIMUT_FEHLT, Pruefampel.Rot, Text(AZIMUT_FEHLT, b.Bezeichner, z.Bezeichner), z.ID, b.ID, b.Flaeche));
                }
            }
            if (!wand) f.Add(new Modellbefund(G, HUELLE_OHNE_AUSSENWAND, Pruefampel.Gelb, Text(HUELLE_OHNE_AUSSENWAND)));
            if (!dach) f.Add(new Modellbefund(G, HUELLE_OHNE_DACH, Pruefampel.Gelb, Text(HUELLE_OHNE_DACH)));
            if (!boden) f.Add(new Modellbefund(G, HUELLE_OHNE_BODEN, Pruefampel.Gelb, Text(HUELLE_OHNE_BODEN)));
        }

        private static void GeometrieEinzone(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.GeometrieBauteile;
            (string Name, double? A, double? U)[] felder =
            {
                (Ressource("MP_FELD_AUSSENWAND"), s.FlaecheAussenwand, s.UAussenwand),
                (Ressource("MP_FELD_FENSTER"), s.FlaecheFenster, s.UFenster),
                (Ressource("MP_FELD_DACH"), s.FlaecheDach, s.UDach),
                (Ressource("MP_FELD_GRUND"), s.FlaecheGrund, s.UGrund),
            };
            foreach ((string name, double? a, double? u) in felder)
            {
                if (!(a > 0.0)) f.Add(new Modellbefund(G, EINZONE_FLAECHE_FEHLT, Pruefampel.Gelb, Text(EINZONE_FLAECHE_FEHLT, name)));
                else if (!(u > 0.0)) f.Add(new Modellbefund(G, EINZONE_U_FEHLT, Pruefampel.Rot, Text(EINZONE_U_FEHLT, name), Flaeche: a));
            }
        }

        // =====================================================================
        //  Gruppe 2 — Aufbauten und Stoffwerte
        // =====================================================================

        private static bool Opak(string art)
            => art != DbWerte.BAUTEILART_FENSTER && art != DbWerte.BAUTEILART_TUER && art != DbWerte.BAUTEILART_VORHANGFASSADE;

        private static void AufbautenZonen(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.AufbautenStoffwerte;
            foreach (ZoneModel z in s.Zonen)
                foreach (BauteilModel b in z.Bauteile ?? new List<BauteilModel>())
                {
                    bool uDa = b.U_Wert is double u && u > 0.0;
                    if (b.ID_Aufbau is int ida && s.Projektaufbauten.TryGetValue(ida, out Aufbaukennung k))
                    {
                        if (k.Ungeeignet)
                            f.Add(new Modellbefund(G, AUFBAU_UNGEEIGNET, Pruefampel.Gelb, Text(AUFBAU_UNGEEIGNET, b.Bezeichner, k.Bezeichner), z.ID, b.ID, b.Flaeche));
                        if (k.SchichtOhneStoffwert)
                            f.Add(new Modellbefund(G, SCHICHT_OHNE_STOFFWERT, uDa ? Pruefampel.Gelb : Pruefampel.Rot,
                                                  Text(SCHICHT_OHNE_STOFFWERT, b.Bezeichner, k.Bezeichner), z.ID, b.ID, b.Flaeche));
                        else if (Opak(b.Bauteilart) && !(k.Kapazitaet > 0.0))
                            f.Add(new Modellbefund(G, KAPAZITAET_FEHLT, Pruefampel.Gelb, Text(KAPAZITAET_FEHLT, b.Bezeichner, k.Bezeichner), z.ID, b.ID, b.Flaeche));
                        continue;
                    }
                    if (!uDa)
                    {
                        Pruefvorschlag v = Opak(b.Bauteilart) ? Modellvorschlagsregeln.Aufbau(s, b) : null;
                        f.Add(new Modellbefund(G, OHNE_U_OHNE_AUFBAU, Pruefampel.Rot, Text(OHNE_U_OHNE_AUFBAU, b.Bezeichner, z.Bezeichner),
                                              z.ID, b.ID, b.Flaeche, v));
                        continue;
                    }
                    if (!Opak(b.Bauteilart)) continue;
                    Bauteilart? art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart);
                    Bauteilrand? rand = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung);
                    if (art is Bauteilart a && rand is Bauteilrand r && Ersatzaufbau.Erhaelt(a, r))
                        f.Add(new Modellbefund(G, NUR_U_WERT, Pruefampel.Gelb, Text(NUR_U_WERT, b.Bezeichner, z.Bezeichner, Zahl(b.U_Wert.Value)),
                                              z.ID, b.ID, b.Flaeche, Modellvorschlagsregeln.Aufbau(s, b)));
                }
        }

        // =====================================================================
        //  Gruppe 3 — Nutzung und Konditionierung
        // =====================================================================

        private static void NutzungZonen(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.NutzungKonditionierung;
            foreach (ZoneModel z in s.Zonen)
            {
                bool kalender = s.Kalender.TryGetValue(z.ID, out HashSet<string> gr) && gr.Count > 0;
                if (!z.IstBeheizt)
                {
                    if (s.HatKalender(z.ID, DbWerte.KOND_GROESSE_HEIZSOLL))
                        f.Add(new Modellbefund(G, UNBEHEIZT_MIT_HEIZKALENDER, Pruefampel.Gelb, Text(UNBEHEIZT_MIT_HEIZKALENDER, z.Bezeichner), z.ID));
                    continue;
                }
                if (s.Nutzung(z) == null && !kalender)
                {
                    f.Add(new Modellbefund(G, ZONE_OHNE_PROFIL, Pruefampel.Gelb, Text(ZONE_OHNE_PROFIL, z.Bezeichner), z.ID,
                                          Vorschlag: Modellvorschlagsregeln.Profil(s, z)));
                    continue;
                }
                if (!s.HatKalender(z.ID, DbWerte.KOND_GROESSE_HEIZSOLL) && !z.Raumsolltemperatur_Tag.HasValue)
                    f.Add(new Modellbefund(G, HEIZKALENDER_GEERBT, Pruefampel.Gelb, Text(HEIZKALENDER_GEERBT, z.Bezeichner), z.ID));
                if (!s.HatKalender(z.ID, DbWerte.KOND_GROESSE_LUEFTUNG) && !z.Luftwechsel_Infiltration.HasValue && !z.Luftwechsel_Nutzer.HasValue)
                    f.Add(new Modellbefund(G, LUEFTUNG_GEERBT, Pruefampel.Gelb, Text(LUEFTUNG_GEERBT, z.Bezeichner), z.ID));
                if (z.Kuehlung_Aktiv == true && !s.HatKalender(z.ID, DbWerte.KOND_GROESSE_KUEHLSOLL) && !z.Kuehl_Sollwert.HasValue
                    && !s.HatKalender(0, DbWerte.KOND_GROESSE_KUEHLSOLL))
                    f.Add(new Modellbefund(G, KUEHLKALENDER_FEHLT, Pruefampel.Gelb, Text(KUEHLKALENDER_FEHLT, z.Bezeichner), z.ID));
            }
        }

        private static void Konditionierung(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.NutzungKonditionierung;
            if (!s.Mehrzonen && s.KuehlungAktiv && !s.HatKalender(0, DbWerte.KOND_GROESSE_KUEHLSOLL))
                f.Add(new Modellbefund(G, KUEHLKALENDER_FEHLT, Pruefampel.Gelb, Text(KUEHLKALENDER_FEHLT, Ressource("MP_GEBAEUDE"))));
            // Ferien und Saison (F5): nur, wenn das Gebäude nach Kalendern eines Profils rechnet.
            if (!s.HatNutzungskalender) return;
            if (!s.FerienBestand && !s.Gebaeudevorgaben.Contains(DbWerte.KOND_ZEILE_FERIEN))
                f.Add(new Modellbefund(G, FERIEN_FEHLEN, Pruefampel.Gelb, Text(FERIEN_FEHLEN),
                                      Vorschlag: s.FerienQuelle is int q ? new Pruefvorschlag(Vorschlagsart.FerienSaison, Vorschlagsherkunft.Projekt,
                                          Ressource("MP_VORSCHLAG_FERIEN_PROJEKT"), IdQuelle: q, Name: DbWerte.KOND_ZEILE_FERIEN) : null));
            if (!s.Gebaeudevorgaben.Contains(DbWerte.KOND_ZEILE_SAISON))
                f.Add(new Modellbefund(G, HEIZPERIODE_FEHLT, Pruefampel.Gelb, Text(HEIZPERIODE_FEHLT),
                                      Vorschlag: s.SaisonQuelle is int q2 ? new Pruefvorschlag(Vorschlagsart.FerienSaison, Vorschlagsherkunft.Projekt,
                                          Ressource("MP_VORSCHLAG_SAISON_PROJEKT"), IdQuelle: q2, Name: DbWerte.KOND_ZEILE_SAISON) : null));
        }

        // =====================================================================
        //  Gruppe 4 — Ort und Lage
        // =====================================================================

        private static void OrtLage(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.OrtLage;
            if (!s.KlimaregionVorhanden)
                f.Add(new Modellbefund(G, KLIMAREGION_FEHLT, Pruefampel.Rot, Text(KLIMAREGION_FEHLT)));
            if (string.Equals(s.Nordwinkelherkunft, NordwinkelherkunftWerte.ANNAHME, StringComparison.Ordinal))
                f.Add(new Modellbefund(G, NORDRICHTUNG_ANGENOMMEN, Pruefampel.Gelb, Text(NORDRICHTUNG_ANGENOMMEN)));
            if (string.IsNullOrWhiteSpace(s.Feiertagsland) && (s.Kalender.Count > 0))
                f.Add(new Modellbefund(G, FEIERTAGSLAND_FEHLT, Pruefampel.Gelb, Text(FEIERTAGSLAND_FEHLT),
                                      Vorschlag: string.IsNullOrWhiteSpace(s.FeiertagslandProjekt) ? null
                                          : new Pruefvorschlag(Vorschlagsart.Feiertagsland, Vorschlagsherkunft.Projekt,
                                                               Ressource("MP_VORSCHLAG_FEIERTAGSLAND", s.FeiertagslandProjekt),
                                                               Name: s.FeiertagslandProjekt)));
        }

        // =====================================================================
        //  Gruppe 5 — Rechenfähigkeit
        // =====================================================================

        private static void Rechenfaehigkeit(Modellpruefstand s, List<Modellbefund> f)
        {
            const Pruefgruppe G = Pruefgruppe.Rechenfaehigkeit;
            (string Feld, double? Wert)[] pflicht =
            {
                (Ressource("MP_FELD_NUTZFLAECHE"), s.Nutzflaeche),
                (Ressource("MP_FELD_RAUMHOEHE"), s.Raumhoehe),
                (Ressource("MP_FELD_FLAECHE_NUTZER"), s.FlaecheNutzer),
                (Ressource("MP_FELD_LUFTWECHSEL"), s.Luftwechselrate),
            };
            foreach ((string feld, double? w) in pflicht)
                if (!(w > 0.0) || double.IsInfinity(w.Value))
                    f.Add(new Modellbefund(G, PFLICHTGROESSE_FEHLT, Pruefampel.Rot, Text(PFLICHTGROESSE_FEHLT, feld)));
            if (!s.Mehrzonen) return;
            string grund;
            try { grund = GebaeudeZonenCtrl.Pruefen(s.Zonen, s.Luftstroeme, s.SollTag); }
            catch (Exception ex) { grund = ex.Message; }
            if (!string.IsNullOrEmpty(grund))
                f.Add(new Modellbefund(G, RECHENWEG_ABGELEHNT, Pruefampel.Rot, Text(RECHENWEG_ABGELEHNT, grund)));
        }
    }
}
