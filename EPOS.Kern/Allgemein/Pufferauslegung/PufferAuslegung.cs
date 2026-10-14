using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE FASSADE DER PUFFERSPEICHER-AUSLEGUNG (Konzept Pufferspeicher-Auslegung, Abschnitt 3.4
    // und 5): je Zone das bemessende Kriterium, die Summe der Zonen, die Rundung auf die
    // Nenninhalte, die Praxisgrenze, der Katalogvorschlag, die Kennzahlen und die Warnliste.
    // Rein funktional und deterministisch: kein Zugriff auf Datenbank, Dienste oder Uhr.
    // ====================================================================================

    /// <summary>Die Pufferspeicher-Auslegung (Konzept 5) — <see cref="Rechnen"/> ist der einzige Einstieg.</summary>
    public static partial class PufferAuslegung
    {
        /// <summary>Die feste Nenninhaltsliste [l] — gleich den gesäten Schlüsseln <c>Speicherauslegung.Nenninhalt.Liste.*</c>.</summary>
        public static readonly IReadOnlyList<double> NENNINHALTE_VORGABE = new double[]
        {
            100, 150, 200, 300, 400, 500, 800, 1000, 1500, 2000, 3000, 5000, 8000, 10000
        };

        /// <summary>Das Raster über dem Listenende [l].</summary>
        public const double RASTER_VORGABE_L = 1000;

        /// <summary>
        /// Der kleinste Nenninhalt ≥ V; über dem Listenende V aufgerundet auf das Raster
        /// (<paramref name="ueberEnde"/> = true). V ≤ 0 → 0.
        /// </summary>
        public static double Runden(double volumenL, IReadOnlyList<double> liste, double rasterL, out bool ueberEnde)
        {
            ueberEnde = false;
            if (!(volumenL > 0)) return 0;
            foreach (double w in liste ?? NENNINHALTE_VORGABE)
                if (w >= volumenL) return w;
            ueberEnde = true;
            double r = rasterL > 0 ? rasterL : RASTER_VORGABE_L;
            return Math.Ceiling(volumenL / r) * r;
        }

        /// <summary>
        /// Rechnet die Auslegung. Ein unzulässiger Eingang (Spreizung oder nutzbarer Anteil nicht
        /// positiv) wird mit <see cref="ArgumentException"/> benannt abgelehnt.
        /// </summary>
        public static PufferAuslegungErgebnis Rechnen(PufferAuslegungEingang eingang)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            PufferRechengroessen g = PufferRechengroessen.Aus(eingang);
            PufferAuslegungParameter p = g.P;
            var zonen = new List<PufferZonenergebnis>();
            double qAusl = 0, vHz = 0;
            double? zirk = null, lJeP = null;
            bool? imBand = null;

            if (eingang.KlasseHeizung) zonen.Add(HeizzoneRechner.Rechnen(g, out qAusl, out vHz));
            if (eingang.KlasseBrauchwasser) zonen.Add(BrauchwasserzoneRechner.Rechnen(g, out zirk, out lJeP, out imBand));
            if (eingang.KlasseProzess) zonen.Add(ProzesszoneRechner.Rechnen(g));

            double summe = zonen.Sum(z => z.VolumenL);
            double empfehlung = Runden(summe, eingang.Nenninhalte, eingang.NenninhaltRasterL ?? RASTER_VORGABE_L, out bool ueberEnde);
            bool praxis = false;
            if (summe > g.PraxisgrenzeL)
            {
                praxis = true;
                empfehlung = g.PraxisgrenzeL;
                g.Warnung(PufferWarncode.PRAXISGRENZE, PufferStufe.Warnung,
                          Textbaustein.T("PA_PRAXISGRENZE_TEXT", "Die Summe der Zonen ({0} l) überschreitet die Praxisgrenze {1} l: Das ist ein Saisonalspeicher, keine Pufferauslegung.", (double)summe, (double)g.PraxisgrenzeL),
                          PufferAuslegungVorgaben.Quellentext(p.Quelle(PufferAuslegungVorgaben.PRAXISGRENZE)), null);
            }

            // ---- Kombipuffer und Tank-im-Tank ----
            PufferZonenergebnis zH = zonen.FirstOrDefault(z => z.Zone == PufferZone.Heizung);
            PufferZonenergebnis zB = zonen.FirstOrDefault(z => z.Zone == PufferZone.Brauchwasser);
            bool kombi = zH != null && zB != null;
            double? anteilH = kombi && zH.VolumenL + zB.VolumenL > 0 ? zH.VolumenL / (zH.VolumenL + zB.VolumenL) : (double?)null;
            bool wp = eingang.Erzeuger?.IstWaermepumpe == true || eingang.Vorlage == PufferVorlage.WP_MONO ||
                      eingang.Vorlage == PufferVorlage.WP_BIVALENT;
            if (kombi && wp && eingang.Zapfprofil?.Topologie == PufferBwTopologie.Speicher)
                g.Warnung(PufferWarncode.TANK_IM_TANK, PufferStufe.Hinweis,
                          Textbaustein.T("PA_TANK_IM_TANK_TEXT", "Tank-im-Tank-Kombispeicher sind für Wärmepumpen weniger geeignet (hohes Temperaturniveau)."),
                          Textbaustein.T("PAUS_HERK_VDI_782", "VDI 4645 E 2026-03, 7.8.2"), null);

            // ---- Katalogvorschlag (Platzhalter-Schnittstelle bis W3) ----
            PufferKatalogsatz vorschlag = null;
            if (summe > 0 && eingang.Katalog != null)
                foreach (PufferKatalogsatz s in eingang.Katalog.OrderBy(s => s.VolumenL).ThenBy(s => s.Id))
                    if (s.VolumenL >= summe && s.Kombispeicher == kombi) { vorschlag = s; break; }

            // ---- Bereitschaftsverlust der Empfehlung (K11) ----
            PufferVerlust verlust = null;
            if (empfehlung > 0)
            {
                verlust = HeizzoneRechner.Bereitschaftsverlust(empfehlung, vorschlag?.BereitschaftsverlustKwhD,
                                                               eingang.VorlaufC, eingang.RuecklaufC, p);
                if (verlust.Extrapoliert)
                    g.Warnung(PufferWarncode.EXTRAPOLATION, PufferStufe.Hinweis,
                              Textbaustein.T("PA_EXTRAPOLATION_TEXT", "Bereitschaftsverlust über {0} l aus der Klasse-C-Grenze extrapoliert.", p.Wert(PufferAuslegungVorgaben.BEREIT_EXTRAPOLATION)), HeizzoneRechner.HERKUNFT_K11, null);
            }

            // ---- Kennzahlen ----
            string typ = g.Typ;
            PufferErzeuger erz = eingang.Erzeuger ?? new PufferErzeuger();
            double faust = p.VorlageWert(typ, "Faustwert_l_kW", 0);
            double? faustL = faust > 0
                ? faust * (eingang.Vorlage == PufferVorlage.SOLAR ? erz.KollektorflaecheM2 : erz.NennleistungKw)
                : (double?)null;
            (double Min, double Max)? band = eingang.KlasseHeizung && qAusl > 0 ? HeizzoneRechner.Band(eingang.Uebergabeart, qAusl, p) : null;
            (double Min, double Max)? bandWp = eingang.KlasseHeizung && wp && erz.NennleistungKw > 0
                ? HeizzoneRechner.BandWp(erz.NennleistungKw, p) : null;
            var kz = new PufferKennzahlen
            {
                Nutzanteil = g.Eta,
                SpreizungK = g.DeltaT,
                AuslegungsheizlastKw = qAusl,
                AnlagenvolumenL = vHz,
                BandMinL = band?.Min,
                BandMaxL = band?.Max,
                BandWpMinL = bandWp?.Min,
                BandWpMaxL = bandWp?.Max,
                KapazitaetKwh = g.DeltaT > 0 ? empfehlung * g.C * g.DeltaT * g.Eta / 1000.0 : 0,
                Verlust = verlust,
                LiterJePerson = lJeP,
                LiterJePersonImBand = imBand,
                ZirkulationKwhD = zirk,
                ZonenanteilHeizung = anteilH,
                SchichtenMindest = kombi ? 2 : 1,
                FaustwertGegenprobeL = faustL
            };

            PufferZonenergebnis groesste = zonen.Where(z => z.Bemessend != null).OrderByDescending(z => z.VolumenL).FirstOrDefault();
            return new PufferAuslegungErgebnis
            {
                Zonen = zonen,
                SummeL = summe,
                EmpfehlungL = empfehlung,
                UeberListenende = ueberEnde,
                AnPraxisgrenze = praxis,
                Bemessend = groesste == null ? null : groesste.Zone + ": " + groesste.Bemessend,
                Katalogvorschlag = vorschlag,
                Kennzahlen = kz,
                Warnungen = g.Warnungen.ToArray()
            };
        }
    }
}
