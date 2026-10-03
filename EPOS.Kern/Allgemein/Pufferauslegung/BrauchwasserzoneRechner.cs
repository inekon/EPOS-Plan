using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE BRAUCHWASSERZONE DER PUFFERSPEICHER-AUSLEGUNG (Konzept 3.2, 3.5). Die Zone kommt aus
    // der Zapfprofil-Speicherauslegung (F2): Topologie Speicher -> der Nenninhalt des
    // Trinkwasserspeichers; Frischwasser- oder Wohnungsstation -> der Heizwasseranteil im Puffer
    //     V_B = (D_max + dD_Zirk) * 1000 * (1 + z_D) / (c * dT_B * eta_s),
    // dT_B = T_oben - T_Ruecklauf (Vorgabe 65/25 °C), z_D = 0,15 (VDI 4645 Anhang I).
    //
    // ZIRKULATION: Traegt das Zapfprofil keine Zirkulation, kommt ein Zuschlag dazu - aus der
    // Zirkulationsleistung des Projekts, als Anteil des Tagesbedarfs oder je Wohneinheit. Der
    // Tageswert Q_Z,d [kWh/d] geht im Verhaeltnis D_max / Q_TWW,d in das Defizit ein (dieselbe
    // Tagesspitze, die das Zapfdefizit bildet; Setzung dieses Kerns, Konzept 3.2 nennt nur die
    // Tageswerte); ohne Tagesbedarf entfaellt der Zuschlag mit Rechenweg-Vermerk.
    // ====================================================================================

    /// <summary>Die Brauchwasserzone (Konzept 3.2) — Topologie-Weiche, Zuschläge, Zirkulation, Kennzahl je Person.</summary>
    public static class BrauchwasserzoneRechner
    {
        public const string HERKUNFT_SPEICHER = "Zapfprofil-Speicherauslegung (TwwSpeicherauslegung)";
        public const string HERKUNFT_FRISCHWASSER = "VDI 4645 E 2026-03, Anhang I; Recherche Runde 2, Abschnitt 4.1";
        public const string HERKUNFT_W551 = "DVGW W 551";
        public const string HERKUNFT_IEA = "IEA SHC Task/Annex 46";

        /// <summary>Kaltwasser- und Zapftemperatur, auf die sich ein Tagesbedarf in Litern bezieht (60/10 °C).</summary>
        public const double ZAPF_C = 60.0, KALT_C = 10.0;

        /// <summary>Grenze der Großanlage für den Hygienehinweis [l] (DVGW W 551, zitiert).</summary>
        public const double W551_GROSSANLAGE_L = 400.0;

        /// <summary>Mindesttemperatur oben im Puffer für die Frischwasserbereitung [°C] (IEA Annex 46, zitiert).</summary>
        public const double HYGIENE_VORLAUF_C = 55.0;

        private static string Z(double w) => PufferRechengroessen.Zahl(w);

        /// <summary>V_B für Frischwasser-/Wohnungsstation: (D_max + ΔD) · 1000 · (1 + z) / (c · ΔT_B · η_s).</summary>
        public static double Frischwasservolumen(double dmaxKwh, double zuschlagDefizitKwh, double z, double c, double deltaTB, double eta) =>
            (dmaxKwh + zuschlagDefizitKwh) * 1000.0 * (1 + z) / (c * deltaTB * eta);

        /// <summary>Zirkulation je Wohneinheit [kWh/d] = n_WE · W_je_WE · 24 / 1000.</summary>
        public static double ZirkulationJeWe(double wohneinheiten, double wJeWe) => wohneinheiten * wJeWe * 24.0 / 1000.0;

        /// <summary>Der Tagesbedarf als Wärme [kWh/d]: Angabe, sonst Liter bei 60/10 °C; <c>null</c> = unbekannt.</summary>
        public static double? TagesbedarfKwh(PufferZapfprofil zp, double c)
        {
            if (zp == null) return null;
            if (zp.TagesbedarfKwh.HasValue) return zp.TagesbedarfKwh;
            if (zp.TagesbedarfL.HasValue) return zp.TagesbedarfL.Value * c * (ZAPF_C - KALT_C) / 1000.0;
            return null;
        }

        /// <summary>Der wirksame Zirkulationsweg: Eingabe, sonst Zapfprofil (trägt es eine), sonst Projekt, sonst Anteil.</summary>
        public static PufferZirkulationWeg Weg(PufferAuslegungEingang e)
        {
            if (e.ZirkulationWeg.HasValue)
            {
                if (e.ZirkulationWeg.Value != PufferZirkulationWeg.ZAPFPROFIL || e.Zapfprofil?.ZirkulationKwhD != null)
                    return e.ZirkulationWeg.Value;
            }
            if (e.Zapfprofil?.ZirkulationKwhD != null) return PufferZirkulationWeg.ZAPFPROFIL;
            if ((e.ZirkulationProjektKw ?? 0) > 0) return PufferZirkulationWeg.PROJEKT;
            return PufferZirkulationWeg.ANTEIL;
        }

        /// <summary>Der Zirkulationsverlust je Tag [kWh/d] nach dem wirksamen Weg; <c>null</c> = nicht bestimmbar.</summary>
        public static double? ZirkulationKwhD(PufferAuslegungEingang e, PufferAuslegungParameter p, double c, PufferZirkulationWeg weg)
        {
            switch (weg)
            {
                case PufferZirkulationWeg.ZAPFPROFIL:
                    return e.Zapfprofil?.ZirkulationKwhD;
                case PufferZirkulationWeg.PROJEKT:
                    return (e.ZirkulationProjektKw ?? 0) * (e.ZirkulationLaufzeitHd ?? 24.0);
                case PufferZirkulationWeg.JE_WE:
                    return e.Wohneinheiten.HasValue ? ZirkulationJeWe(e.Wohneinheiten.Value, p.Wert(PufferAuslegungVorgaben.ZIRK_W_JE_WE)) : (double?)null;
                default:
                    double? q = TagesbedarfKwh(e.Zapfprofil, c);
                    return q.HasValue ? p.Wert(PufferAuslegungVorgaben.ZIRK_ANTEIL) * q.Value : (double?)null;
            }
        }

        /// <summary>Rechnet die Brauchwasserzone; Warnungen landen in <paramref name="g"/>.</summary>
        internal static PufferZonenergebnis Rechnen(PufferRechengroessen g, out double? zirkulationKwhD,
                                                    out double? literJePerson, out bool? imBand)
        {
            PufferAuslegungEingang e = g.E;
            PufferAuslegungParameter p = g.P;
            const PufferZone ZONE = PufferZone.Brauchwasser;
            var k = new List<PufferKriterium>();
            zirkulationKwhD = null;
            literJePerson = null;
            imBand = null;
            PufferZapfprofil zp = e.Zapfprofil;
            if (zp == null)
            {
                g.Warnung(PufferWarncode.KEINE_REIHE, PufferStufe.Warnung,
                          "Kein Zapfprofil-Ergebnis: Die Brauchwasserzone bleibt leer.", HERKUNFT_SPEICHER, ZONE);
                return HeizzoneRechner.Bemessen(ZONE, k, false);
            }

            double tOben = e.TPufferObenC ?? p.Wert(PufferAuslegungVorgaben.FW_OBEN);
            switch (zp.Topologie)
            {
                case PufferBwTopologie.Speicher when zp.NenninhaltL.HasValue:
                    k.Add(new PufferKriterium
                    {
                        Kennung = PufferKriteriumKennung.B_SPEICHER, Bezeichnung = "Trinkwasserspeicher aus dem Zapfprofil",
                        VolumenL = zp.NenninhaltL.Value, Aktiv = true, Herkunft = HERKUNFT_SPEICHER,
                        Rechenweg = "Nenninhalt " + Z(zp.NenninhaltL.Value) + " l (Zuschläge und Zirkulation in der Zapfprofil-Auslegung)"
                    });
                    if (zp.NenninhaltL.Value > W551_GROSSANLAGE_L)
                        g.Warnung(PufferWarncode.HYGIENE_W551, PufferStufe.Hinweis,
                                  "Trinkwasserspeicher über " + Z(W551_GROSSANLAGE_L) + " l: Anforderungen an Großanlagen beachten.", HERKUNFT_W551, ZONE);
                    break;

                case PufferBwTopologie.Durchfluss:
                    k.Add(new PufferKriterium
                    {
                        Kennung = PufferKriteriumKennung.B_FRISCHWASSER, Bezeichnung = "Durchfluss ohne Speicher",
                        VolumenL = 0, Aktiv = true, Herkunft = HERKUNFT_SPEICHER, Rechenweg = "Durchflussbereitung: kein Brauchwasseranteil im Puffer"
                    });
                    break;

                default:
                {
                    double dTB = e.DeltaTBK ?? (tOben - p.Wert(PufferAuslegungVorgaben.FW_RUECKLAUF));
                    if (!(dTB > 0))
                        throw new ArgumentException("ΔT_B der Brauchwasserzone muss positiv sein (" + Z(dTB) + " K).");
                    double zuschlag = p.Wert(PufferAuslegungVorgaben.FW_ZUSCHLAG);
                    PufferZirkulationWeg weg = Weg(e);
                    zirkulationKwhD = ZirkulationKwhD(e, p, g.C, weg);
                    double? qTag = TagesbedarfKwh(zp, g.C);
                    double dZ = 0;
                    string zText;
                    if (weg == PufferZirkulationWeg.ZAPFPROFIL)
                        zText = "Zirkulation im Zapfprofil enthalten";
                    else if (weg == PufferZirkulationWeg.ANTEIL)
                    {
                        dZ = p.Wert(PufferAuslegungVorgaben.ZIRK_ANTEIL) * zp.DmaxKwh;
                        zText = "Zirkulation " + Z(p.Wert(PufferAuslegungVorgaben.ZIRK_ANTEIL)) + " · D_max = " + Z(dZ) + " kWh";
                    }
                    else if (zirkulationKwhD.HasValue && qTag.HasValue && qTag.Value > 0)
                    {
                        dZ = zirkulationKwhD.Value * zp.DmaxKwh / qTag.Value;
                        zText = "Zirkulation " + Z(zirkulationKwhD.Value) + " kWh/d · D_max / Q_d = " + Z(dZ) + " kWh";
                    }
                    else
                        zText = "Zirkulation ohne Tagesbedarf nicht umzurechnen: kein Zuschlag";
                    double v = Frischwasservolumen(zp.DmaxKwh, dZ, zuschlag, g.C, dTB, g.Eta);
                    k.Add(new PufferKriterium
                    {
                        Kennung = PufferKriteriumKennung.B_FRISCHWASSER,
                        Bezeichnung = zp.Topologie == PufferBwTopologie.Wohnungsstation ? "Wohnungsstationen am Puffer" :
                                      zp.Topologie == PufferBwTopologie.Speicher ? "Trinkwasser am Puffer (ohne Nenninhalt)" : "Frischwasserstation am Puffer",
                        VolumenL = v, Aktiv = true, EnthaeltNutzanteil = true, Herkunft = HERKUNFT_FRISCHWASSER,
                        Rechenweg = "(" + Z(zp.DmaxKwh) + " + " + Z(dZ) + ") kWh · 1000 · (1 + " + Z(zuschlag) + ") / (" + Z(g.C) + " · " +
                                    Z(dTB) + " K · " + Z(g.Eta) + "); " + zText
                    });
                    if (tOben < HYGIENE_VORLAUF_C)
                        g.Warnung(PufferWarncode.HYGIENE_TEMPERATUR, PufferStufe.Warnung,
                                  "Puffer oben " + Z(tOben) + " °C: Für die Frischwasserbereitung mindestens " + Z(HYGIENE_VORLAUF_C) +
                                  " °C Vorlauf; periodisches Aufheizen ist kein Ersatz.", HERKUNFT_IEA, ZONE);
                    break;
                }
            }

            PufferZonenergebnis zone = HeizzoneRechner.Bemessen(ZONE, k, false);

            // ---- Kennzahl je Person und Überdimensionierung (Konzept 3.5) ----
            if (zp.TagesbedarfL.HasValue && (zp.Personen ?? 0) > 0)
            {
                literJePerson = zp.TagesbedarfL.Value / zp.Personen.Value;
                imBand = literJePerson >= p.Wert(PufferAuslegungVorgaben.BW_MIN) && literJePerson <= p.Wert(PufferAuslegungVorgaben.BW_MAX);
            }
            if (zp.TagesbedarfL.HasValue && zp.TagesbedarfL.Value > 0)
            {
                double grenze = p.Wert(PufferAuslegungVorgaben.BW_UEBER) * zp.TagesbedarfL.Value;
                if (zone.VolumenL > grenze)
                    g.Warnung(PufferWarncode.BW_UEBERDIMENSIONIERT, PufferStufe.Hinweis,
                              "Die Brauchwasserzone (" + Z(zone.VolumenL) + " l) übersteigt das " +
                              Z(p.Wert(PufferAuslegungVorgaben.BW_UEBER)) + "-fache des Tagesbedarfs (" + Z(zp.TagesbedarfL.Value) + " l).",
                              HERKUNFT_IEA, ZONE);
            }
            return zone;
        }
    }
}
