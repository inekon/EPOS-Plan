using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using Xbim.Ifc4.Interfaces;
using S = WindowsFormsApplication1.IfcStandardsaetze;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die IFC-Standardsätze der Simulationsdaten lesen</b> (Welle DA-3, Konzept Datenaustausch 4.2; E138 F2, F6 a, F8 b):
    /// je Raum Belegung, Lasten, Auslegung, Licht und die Sollwertsätze vollständig, dazu die herstellereigenen
    /// <c>RoomVentilationType</c>/<c>RatedMinAirExchangeRate</c> (nur nach dem Namen, nur als Vorschlag); je Fenster der Glasanteil;
    /// je Baustoff der Wärmedurchlasswiderstand (<c>EPOS_Baustoff</c>) und die optischen Werte gegen die Konstanten; am Grundstück
    /// Breite und Länge; die Rückfallwerte in <c>EPOS_Zone</c>/<c>EPOS_Bauteil</c>/<c>EPOS_Gebaeude</c>.
    /// <para><b>Rangfolge:</b> Standardsatz vor <c>EPOS_*</c> (Konzept 3.3), Datei vor Profil — das Profil füllt nur die Lücken
    /// (<see cref="GbxmlKonditionierung.Bilden"/>, Herkunft „aus IFC-Datei (Standardsätze)“). Die Leistungen des Raums (W) werden
    /// nach der Fläche (<see cref="Flaechenart"/>) auf W/m² und W je Person umgerechnet.</para>
    /// </summary>
    internal sealed partial class IfcAbbildBauer
    {
        /// <summary>Die Rohwerte eines Raums vor der Flächenbestimmung.</summary>
        private sealed class Raumstandard
        {
            public double? People, Lighting, Equipment, Lueftungsstrom, Natuerlich, Mechanisch, Hersteller, AussenluftEpos, WaermeEpos,
                           GeraeteEpos, LichtEpos;
            public bool? Kunstlicht;
            public string HerstellerName;
            public int Werte;
        }

        private readonly List<(AbbildRaum Raum, Raumstandard Werte)> _raumstandard = new List<(AbbildRaum, Raumstandard)>();
        private readonly SortedSet<string> _durchgereicht = new SortedSet<string>(StringComparer.Ordinal);
        private int _zahlDurchgereicht;
        private readonly HashSet<int> _optikGeprueft = new HashSet<int>();

        /// <summary>Breite und Länge aus <c>IfcSite.RefLatitude</c>/<c>RefLongitude</c> (das erste Grundstück mit beiden).</summary>
        private void Lage()
        {
            if (_abbild.BreiteGrad.HasValue && _abbild.LaengeGrad.HasValue) return;
            foreach (IIfcSite site in Sortiert<IIfcSite>())
            {
                double? breite = Grad(site.RefLatitude), laenge = Grad(site.RefLongitude);
                if (!breite.HasValue || !laenge.HasValue) continue;
                _abbild.BreiteGrad = breite;
                _abbild.LaengeGrad = laenge;
                return;
            }
        }

        /// <summary>Ein zusammengesetzter Winkel (Grad, Minuten, Sekunden, Millionstelsekunden) in Grad; <c>null</c> = keiner.</summary>
        private static double? Grad(object w)
        {
            // Schemafrei über den Werttyp (die Struktur ist je Schema eine eigene; Quelltextwache des Imports).
            IList<long> teile = ((w as Xbim.Common.IExpressValueType)?.Value as IEnumerable<long>)?.ToList();
            if (teile == null || teile.Count < 3) return null;
            double betrag = Math.Abs(teile[0]) + Math.Abs(teile[1]) / 60.0 + Math.Abs(teile[2]) / 3600.0
                            + (teile.Count > 3 ? Math.Abs(teile[3]) / 3.6e9 : 0.0);
            bool negativ = teile.Any(t => t < 0);
            double g = Math.Round(negativ ? -betrag : betrag, 9);
            return double.IsFinite(g) && Math.Abs(g) <= 180.0 ? g : null;
        }

        /// <summary>Die Gebäudeart aus <c>EPOS_Gebaeude.Gebaeudeart</c>, wenn das Gebäude keine trägt.</summary>
        private void EposGebaeudeLesen(IIfcBuilding b, AbbildGebaeude g)
        {
            // Welle DA-4: das Erdreich (G3, G4) aus den Text-Werten von EPOS_Gebaeude.
            var werte = new List<(string, string)>();
            // Nachzug N4: dazu die obere Raumtemperatur des Gebäudes (B10).
            foreach (string name in new[] { GebaeudeRestdaten.ERDREICH_UMFANG, GebaeudeRestdaten.ERDREICH_U_WIRKSAM, GebaeudeRestdaten.KELLERTEMPERATUR,
                                            GebaeudeRestdaten.GEBAEUDE_MAXIMALERAUMTEMPERATUR })
                if (IfcEigenschaften.Finden(_bezuege, b, IfcSchreiber.EPOS_GEBAEUDE, name) is IfcFund fund && Textwert(fund) is string t)
                    werte.Add((name, t));
            var uebersprungen = new List<string>();
            g.Erdreich = GebaeudeRestdaten.ErdreichLesen(werte, uebersprungen);
            if (uebersprungen.Count > 0)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "RESTDATEN_UEBERSPRUNGEN", g.Anzeigename,
                    Ganz(uebersprungen.Count), string.Join(", ", uebersprungen)));
            if (!string.IsNullOrWhiteSpace(g.Art)) return;
            IfcFund f = IfcEigenschaften.Finden(_bezuege, b, IfcSchreiber.EPOS_GEBAEUDE, S.EPOS_GEBAEUDEART);
            string art = f == null ? null : Textwert(f)?.Trim();
            if (!string.IsNullOrEmpty(art)) g.Art = art;
        }

        /// <summary>Die Standardsätze und die Rückfallwerte eines Raums: was ohne Fläche geht, sofort; die Leistungen gemerkt.</summary>
        private void StandardsaetzeLesen(IIfcSpace s, AbbildRaum r)
        {
            var w = new Raumstandard();
            double? Z(string satz, string name, bool unten = false)
            {
                IfcFund f = IfcEigenschaften.Finden(_bezuege, s, satz, name);
                double? z = f == null ? null : Zahl(f, unten);
                if (z.HasValue && double.IsFinite(z.Value)) { w.Werte++; return z; }
                return null;
            }
            double? T(string satz, string name)
            {
                double? z = Z(satz, name, unten: true);
                return z.HasValue ? Math.Round(_einheiten.NachCelsius(z.Value), 4) : null;
            }
            bool? B(string satz, string name)
            {
                bool? b = Wahrheit(IfcEigenschaften.Finden(_bezuege, s, satz, name));
                if (b.HasValue) w.Werte++;
                return b;
            }

            // Belegung.
            double? zahl = NichtNegativ(Z(S.BELEGUNG, S.BELEGUNG_ZAHL));
            double? jePerson = Positiv(Z(S.BELEGUNG, S.BELEGUNG_FLAECHE));
            // Lasten des Raums [W].
            w.People = NichtNegativ(Z(S.LAST, S.LAST_PERSONEN));
            w.Lighting = NichtNegativ(Z(S.LAST, S.LAST_LICHT));
            w.Equipment = NichtNegativ(Z(S.LAST, S.LAST_GERAETE));
            // Auslegung.
            double? heizen = T(S.AUSLEGUNG, S.AUSLEGUNG_HEIZEN), kuehlen = T(S.AUSLEGUNG, S.AUSLEGUNG_KUEHLEN);
            w.Lueftungsstrom = NichtNegativ(Z(S.AUSLEGUNG, S.AUSLEGUNG_LUEFTUNG));
            w.Kunstlicht = B(S.LICHT, S.LICHT_KUNSTLICHT);
            // Sollwerte (in IFC4X3 entfallen, wie beim Heizsollwert nicht gelesen).
            if (_abbild.SchemaStand != IfcSchemaStand.Ifc4x3)
            {
                double? sommer = T(S.SOLLWERTE, S.SOLL_KUEHLEN);
                r.SollKuehlenC ??= sommer;
                r.Nachtabsenkung ??= B(S.SOLLWERTE, S.SOLL_NACHTABSENKUNG);
                w.Natuerlich = NichtNegativ(Z(S.SOLLWERTE, S.SOLL_FREIE_LUEFTUNG_RATE));
                w.Mechanisch = NichtNegativ(Z(S.SOLLWERTE, S.SOLL_MECH_LUEFTUNG_RATE));
            }
            r.SollHeizenC ??= heizen;
            r.SollKuehlenC ??= kuehlen;
            r.LuftwechselNutzerJeH ??= w.Natuerlich;

            // Durchgereicht: gelesen, gezählt, ohne Zielfeld.
            foreach ((string satz, string name) in S.DURCHGEREICHT)
                if (IfcEigenschaften.Finden(_bezuege, s, satz, name) != null)
                {
                    _durchgereicht.Add(satz + "." + name);
                    _zahlDurchgereicht++;
                }

            // Herstellereigen, nach dem Namen in jedem Satz.
            IfcFund hersteller = IfcEigenschaften.AlleMitNamen(_bezuege, s, new[] { S.HERSTELLER_MINDESTLUFTWECHSEL }).FirstOrDefault();
            if (hersteller != null && Positiv(Zahl(hersteller)) is double h)
            {
                w.Hersteller = h;
                w.HerstellerName = hersteller.Satz + "." + hersteller.Eigenschaft.Name;
            }
            IfcFund art = IfcEigenschaften.AlleMitNamen(_bezuege, s, new[] { S.HERSTELLER_LUEFTUNGSART }).FirstOrDefault();
            if (art != null)
            {
                _durchgereicht.Add(art.Satz + "." + art.Eigenschaft.Name);
                _zahlDurchgereicht++;
            }

            // Rückfall EPOS_Zone: nur, was kein Standardsatz trägt (Konzept 3.3).
            double? E(string name)
            {
                IfcFund f = IfcEigenschaften.Finden(_bezuege, s, IfcSchreiber.EPOS_ZONE, name);
                return f == null ? null : NichtNegativ(Zahl(f));
            }
            r.Personen ??= zahl ?? E(S.EPOS_PERSONEN);
            r.FlaecheJePersonM2 ??= jePerson;
            r.LuftwechselJeH ??= E(S.EPOS_INFILTRATION);
            w.GeraeteEpos = E(S.EPOS_GERAETE);
            w.LichtEpos = E(S.EPOS_LICHT);
            w.AussenluftEpos = E(S.EPOS_AUSSENLUFT_PERSON);
            w.WaermeEpos = E(S.EPOS_WAERME_PERSON);
            _raumstandard.Add((r, w));
        }

        /// <summary>
        /// Nach der Fläche: die Leistungen des Raums je Fläche und je Person, die Lüftung, der Vorschlag aus der herstellereigenen
        /// Eigenschaft (nur gemeldet, nie als Wert übernommen) und die Lasten für den Zonenplan (<see cref="AbbildRaum.GbxmlLasten"/> mit Herkunft IFC); dazu eine Meldung
        /// je Datei mit der Zählung.
        /// </summary>
        private void StandardsaetzeAbbilden()
        {
            int raeume = 0, werte = 0;
            var vorschlaege = new List<(string Raum, double Wert, string Name)>();
            foreach ((AbbildRaum r, Raumstandard w) in _raumstandard)
            {
                double? flaeche = r.FlaecheM2 > 0.0 ? r.FlaecheM2 : null;
                if (w.Lighting.HasValue && flaeche.HasValue) r.LichtWm2 ??= Math.Round(w.Lighting.Value / flaeche.Value, S.STELLEN);
                if (w.Equipment.HasValue && flaeche.HasValue) r.GeraeteWm2 ??= Math.Round(w.Equipment.Value / flaeche.Value, S.STELLEN);
                r.LichtWm2 ??= w.LichtEpos ?? (w.Kunstlicht == false ? 0.0 : null);
                r.GeraeteWm2 ??= w.GeraeteEpos;
                double? personen = S.Personenzahl(r);
                double? waerme = w.People.HasValue && personen > 0.0 ? Math.Round(w.People.Value / personen.Value, S.STELLEN) : w.WaermeEpos;
                double? jeFlaeche = w.AussenluftEpos.HasValue || !w.Lueftungsstrom.HasValue || !flaeche.HasValue ? null
                    : Math.Round(w.Lueftungsstrom.Value * 3600.0 / flaeche.Value, S.STELLEN);
                double? luftwechsel = w.Natuerlich.HasValue || w.Mechanisch.HasValue ? (w.Natuerlich ?? 0.0) + (w.Mechanisch ?? 0.0) : null;
                // Herstellereigen: nur ein Vorschlag mit Herkunft (Konzept 4.2) — genannt, nicht übernommen; es gilt das Profil.
                if (!luftwechsel.HasValue && w.Hersteller.HasValue)
                    vorschlaege.Add((r.Name ?? r.Kennung, w.Hersteller.Value, w.HerstellerName));
                var lasten = new GbxmlRaumlasten
                {
                    AusIfc = true, WaermeJePersonW = waerme, AussenluftJePersonM3h = w.AussenluftEpos, AussenluftJeFlaecheM3hM2 = jeFlaeche,
                    ZonenLuftwechselJeH = luftwechsel,
                };
                bool traegt = lasten.Werte > 0 || w.People.HasValue || w.Lighting.HasValue || w.Equipment.HasValue
                              || w.LichtEpos.HasValue || w.GeraeteEpos.HasValue;
                if (traegt && r.GbxmlLasten == null) r.GbxmlLasten = lasten;
                if (w.Werte > 0) { raeume++; werte += w.Werte; }
            }
            if (vorschlaege.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "HERSTELLER_VORSCHLAG", Ganz(vorschlaege.Count),
                    vorschlaege[0].Name, Zahl(vorschlaege.Min(v => v.Wert)), Zahl(vorschlaege.Max(v => v.Wert)),
                    Beispiele(vorschlaege.Select(v => v.Raum).ToList())));
            if (raeume > 0 || _zahlDurchgereicht > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "STANDARDSAETZE", Ganz(raeume), Ganz(werte),
                                                       Ganz(_zahlDurchgereicht), _durchgereicht.Count == 0 ? "—" : string.Join(", ", _durchgereicht)));
        }

        /// <summary>
        /// Die Fensterwerte der Standardsätze: der Glasanteil (<c>Pset_WindowCommon.GlazingAreaFraction</c>) als 1 − Rahmenanteil,
        /// wenn die Datei keinen Rahmenanteil nennt; die Verglasungswerte (U der Verglasung, Lichttransmission) durchgereicht.
        /// </summary>
        private void FensterStandardLesen(IIfcElement o, AbbildBauteil b)
        {
            if (!b.Rahmenanteil.HasValue)
            {
                IfcFund f = IfcEigenschaften.Finden(_bezuege, o, S.FENSTER, S.FENSTER_GLASANTEIL);
                double? glas = f == null ? null : Zahl(f);
                if (glas >= 0.0 && glas <= 1.0)
                {
                    b.Rahmenanteil = Math.Round(1.0 - glas.Value, S.STELLEN);
                    b.RahmenanteilHerkunft = Importherkunft.Ifc;
                    b.RahmenanteilBeleg = S.FENSTER + "." + S.FENSTER_GLASANTEIL + " (1 − Glasanteil)";
                }
            }
            foreach (string name in new[] { S.VERGLASUNG_U, S.VERGLASUNG_U_WINTER, S.VERGLASUNG_LICHT })
                if (IfcEigenschaften.Finden(_bezuege, o, S.VERGLASUNG, name) != null)
                {
                    _durchgereicht.Add(S.VERGLASUNG + "." + name);
                    _zahlDurchgereicht++;
                }
        }

        /// <summary>
        /// Der Wärmebrückenzuschlag eines Bauteils aus <c>EPOS_Bauteil.WaermebrueckeUA</c> [W/K]; dazu (DA-8) Absorptions- und
        /// Emissionsgrad aus <c>EPOS_Bauteil</c>, nur im Bereich 0 … 1.
        /// </summary>
        private void EposBauteilLesen(IIfcElement e, AbbildBauteil b)
        {
            double? Grad(string name)
            {
                IfcFund g = IfcEigenschaften.Finden(_bezuege, e, IfcSchreiber.EPOS_BAUTEIL, name);
                double? w = g == null ? null : Zahl(g);
                return w >= 0.0 && w <= 1.0 ? w : null;
            }
            b.AbsorptionsgradSolar ??= Grad(S.EPOS_ABSORPTION);
            b.Emissionsgrad ??= Grad(S.EPOS_EMISSION);
            if (b.WaermebrueckeWK.HasValue) return;
            IfcFund f = IfcEigenschaften.Finden(_bezuege, e, IfcSchreiber.EPOS_BAUTEIL, S.EPOS_WAERMEBRUECKE);
            if (f != null) b.WaermebrueckeWK = NichtNegativ(Zahl(f));
        }

        /// <summary>Eine Zahl aus einem Stoffsatz (Name egal in Groß-/Kleinschreibung); <c>null</c> = keine.</summary>
        private double? Stoffzahl(IIfcMaterial stoff, string satz, string name)
        {
            if (stoff == null || _abbild.SchemaStand == IfcSchemaStand.Ifc2x3) return null;
            try
            {
                foreach (IIfcMaterialProperties p in _bezuege.Stoffsaetze(stoff))
                {
                    if (!IfcEigenschaften.Gleich(IfcEigenschaften.Text(p.Name), satz)) continue;
                    foreach (IIfcPropertySingleValue e in p.Properties.OfType<IIfcPropertySingleValue>())
                        if (IfcEigenschaften.Gleich(e.Name.ToString(), name)) return IfcEigenschaften.Zahl(e.NominalValue);
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MissingMethodException || ex is InvalidCastException)
            {
            }
            return null;
        }

        /// <summary>Der Wärmedurchlasswiderstand einer Schicht aus <c>EPOS_Baustoff</c> [m²K/W]; <c>null</c> = keiner.</summary>
        private double? StoffWiderstand(IIfcMaterial stoff) => Positiv(Stoffzahl(stoff, IfcSchreiber.EPOS_BAUSTOFF, S.EPOS_WIDERSTAND));

        /// <summary>
        /// Die optischen Werte eines Baustoffs (<c>Pset_MaterialOptical</c>) gegen die Konstanten des Rechenwegs (E138 F8 b):
        /// eine Abweichung über <see cref="IfcStandardsaetze.OPTIK_TOLERANZ"/> wird gemeldet, übernommen wird nichts (kein Feld).
        /// </summary>
        private void OptikPruefen(IIfcMaterial stoff)
        {
            if (stoff == null || !_optikGeprueft.Add(stoff.EntityLabel)) return;
            void Pruefen(string name, double? datei, double konstante)
            {
                if (datei.HasValue && Math.Abs(datei.Value - konstante) > S.OPTIK_TOLERANZ)
                    _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "OPTIK_ABWEICHUNG", stoff.Name.ToString(), name,
                                                           Zahl(Math.Round(datei.Value, 4)), Zahl(konstante)));
            }
            double? reflexion = Stoffzahl(stoff, S.OPTIK, S.OPTIK_REFLEXION);
            Pruefen("α = 1 − " + S.OPTIK_REFLEXION, reflexion.HasValue ? 1.0 - reflexion.Value : null, GebaeudeFestwerte.ABSORPTIONSGRAD_OPAK);
            Pruefen(S.OPTIK_EMISSION, Stoffzahl(stoff, S.OPTIK, S.OPTIK_EMISSION), GebaeudeFestwerte.EMISSIONSGRAD_AUSSEN);
        }
    }
}
