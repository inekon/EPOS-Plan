using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Welle KB-7b — die Wasserkosten des Rückkühlwerks</b> (Entwurf
    /// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitt 4; Anwenderentscheide
    /// vom 16.10.2026: ein Preis je Projekt, Preissteigerung p_B, ein Arbeitspreis samt Abwasser, kein Grundpreis).
    ///
    /// <para><b>Genau eine Buchung, in der Wirtschaftlichkeit.</b> Menge = Σ <c>Wasser_m3</c> der
    /// Kältemaschinen-Ergebnisse des Laufs (szenarioskaliert wie alle Mengen, <see cref="SzenarioMengen"/>);
    /// Preis = Arbeitspreis des Wasserträgers [€/m³] über <see cref="KostenEmissionRechner.PreisSatz"/>
    /// (Rückfallkette und Szenariopreise wie jeder Träger); Kosten = Menge × Preis [€/a] in den
    /// <b>Betriebs-Topf</b> (sofort wirksam, Steigerung p_B). Wasser ist Betriebsmittel, keine Endenergie:
    /// Es läuft nicht durch Energiekosten, CO₂ und BEHG des <see cref="KostenEmissionRechner"/>, nicht durch
    /// den <see cref="EndenergieAufloeser"/>, und der Wasserträger ist nie ein verwendeter oder zulässiger
    /// Träger eines Erzeugers (<see cref="ProjektEnergietraegerCtrl.Verwendete"/>,
    /// <see cref="EnergietraegerZulaessigkeit"/>).</para>
    ///
    /// <para><b>Erkannt am Preismodell, nie am Namen</b> (<see cref="WasserpreisTraegerkarteSchema.PREISMODELL_WASSER"/>).</para>
    ///
    /// <para><b>Ohne Preis benannt.</b> Menge &gt; 0 und kein Arbeitspreis (leer oder 0): keine Kosten, ein
    /// Hinweis der Wirtschaftlichkeit nennt die Menge (Frage KD‑Q8).</para>
    /// </summary>
    internal static class Wasserkosten
    {
        /// <summary>Das Ergebnis eines Standes: Menge, wirksamer Preis, Kosten und der Träger.</summary>
        internal sealed class Ergebnis
        {
            /// <summary>Wasser der Rückkühlwerke im Lauf [m³/a] (szenarioskaliert).</summary>
            public double MengeM3;

            /// <summary>Wirksamer Arbeitspreis des Wasserträgers [€/m³]; <c>null</c> = keiner (leer oder 0).</summary>
            public double? PreisEurM3;

            /// <summary>Wasserkosten [€/a] = Menge × Preis; 0 ohne Menge oder ohne Preis.</summary>
            public double KostenEur;

            /// <summary><c>energy_carrier.id</c> des Wasserträgers; 0 = keiner im Katalog.</summary>
            public int IdTraeger;

            /// <summary>Wasser verbraucht, aber kein Preis — die Kosten fehlen benannt.</summary>
            public bool OhnePreis => MengeM3 > 0 && !PreisEurM3.HasValue;
        }

        /// <summary>
        /// Der Wasserträger des Projekts — analog <see cref="Emissionsquelle.StromTraeger(int)"/>: der dem
        /// Projekt zugeordnete Träger mit <c>pricing_model = 'WATER'</c>, sonst der Katalogträger
        /// (<see cref="WasserpreisTraegerkarteSchema.TraegerId"/>); 0 = keiner.
        /// </summary>
        internal static int WasserTraeger(int idProjekt)
        {
            if (idProjekt > 0)
            {
                object o = null;
                try
                {
                    o = StilleDb.ScalarStreng(
                        "SELECT ec.id FROM energy_project_settings AS s " +
                        "INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id " +
                        "WHERE s.ID_Projekt = ? AND ec.pricing_model = ? ORDER BY ec.id LIMIT 1",
                        new DbParam("@p", idProjekt),
                        new DbParam("@m", WasserpreisTraegerkarteSchema.PREISMODELL_WASSER));
                }
                catch (Exception) { o = null; }   // Datenbank ohne Trägerkarte: der Katalogträger entscheidet
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            long? katalog = null;
            try { katalog = WasserpreisTraegerkarteSchema.TraegerId(); }
            catch (Exception) { katalog = null; }
            return katalog.HasValue ? (int)katalog.Value : 0;
        }

        /// <summary>Ist der Träger der Wasserträger (<c>pricing_model = 'WATER'</c>)? Am Preismodell erkannt.</summary>
        internal static bool IstWasserTraeger(int carrierId)
        {
            if (carrierId <= 0) return false;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT pricing_model FROM energy_carrier WHERE id = ?", new DbParam("@id", carrierId));
                return o != null && o != DBNull.Value &&
                       string.Equals(Convert.ToString(o, CultureInfo.InvariantCulture).Trim(),
                                     WasserpreisTraegerkarteSchema.PREISMODELL_WASSER, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>Ist das Preismodell das des Wasserträgers?</summary>
        internal static bool IstWasserPreismodell(string preismodell) =>
            string.Equals((preismodell ?? "").Trim(), WasserpreisTraegerkarteSchema.PREISMODELL_WASSER,
                          StringComparison.OrdinalIgnoreCase);

        /// <summary>Σ <c>Wasser_m3</c> der Kältemaschinen-Ergebnisse des Standes [m³/a]; 0 ohne Rückkühlwerk.</summary>
        internal static double MengeM3(VariantenDaten v)
        {
            List<ErgebnisKaeltemaschineModel> km = v?.Ergebnis?.Kaeltemaschinen;
            if (km == null) return 0;
            double summe = 0;
            foreach (ErgebnisKaeltemaschineModel k in km)
                if (k != null && k.Wasser_m3.HasValue && k.Wasser_m3.Value > 0 && !double.IsNaN(k.Wasser_m3.Value))
                    summe += k.Wasser_m3.Value;
            return summe;
        }

        /// <summary>
        /// Menge × Preis eines Standes. <paramref name="preisSzenario"/> ist das Szenario, dessen Trägerpreise
        /// wirken (<c>null</c> = Erwartet) — derselbe Schlüssel, den die Energiekosten des Standes bekommen.
        /// Ohne Menge wird kein Preis gelesen.
        /// </summary>
        internal static Ergebnis Rechne(int idProjekt, VariantenDaten v, string preisSzenario)
        {
            var e = new Ergebnis { MengeM3 = MengeM3(v) };
            if (e.MengeM3 <= 0) return e;
            e.IdTraeger = WasserTraeger(idProjekt);
            if (e.IdTraeger > 0)
            {
                KostenEmissionRechner.PreisSatz(idProjekt, e.IdTraeger, preisSzenario,
                                                out double? arbeit, out _, out _);
                if (arbeit.HasValue && arbeit.Value > 0 && !double.IsNaN(arbeit.Value)) e.PreisEurM3 = arbeit.Value;
            }
            if (e.PreisEurM3.HasValue) e.KostenEur = e.MengeM3 * e.PreisEurM3.Value;
            return e;
        }

        /// <summary>Die Nachweiszeile der Betriebskosten („Wasser Rückkühlwerk: Menge × Preis“); <c>null</c> ohne Kosten.</summary>
        internal static KostenPositionNachweis Nachweis(Ergebnis e)
        {
            if (e == null || !e.PreisEurM3.HasValue || e.MengeM3 <= 0) return null;
            CultureInfo k = CultureInfo.CurrentCulture;
            return new KostenPositionNachweis
            {
                Bezeichnung = string.Format(k, MyResource.Resource.WIRT_BK_WASSER_RKW,
                                            e.MengeM3.ToString("N1", k), e.PreisEurM3.Value.ToString("N2", k)),
                Kostenart = DbWerte.KOSTENART_BEDARFSGEBUNDEN,
                Bemessung = DbWerte.BEMESSUNG_BETRAG,
                Menge = e.MengeM3,
                Einheitpreis = e.PreisEurM3,
                BetragJahr = e.KostenEur
            };
        }

        /// <summary>Der Hinweis ohne Preis (Menge in m³/a); leer, wenn Kosten stehen oder kein Wasser floss.</summary>
        internal static string HinweisOhnePreis(Ergebnis e)
        {
            if (e == null || !e.OhnePreis) return "";
            CultureInfo k = CultureInfo.CurrentCulture;
            return string.Format(k, MyResource.Resource.WIRT_HINWEIS_WASSER_OHNE_PREIS, e.MengeM3.ToString("N1", k));
        }
    }
}
