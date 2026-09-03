using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rechenvorschriften des ERWEITERTEN PV-Modells (Stufe E2 des
    /// <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Nachtrag 2 N2.3) —
    /// <b>ohne Datenbank und ohne Oberflaeche</b>, damit sie sich einzeln nachrechnen
    /// lassen (Muster <see cref="SolarZeitbasis"/> aus Paket A).
    ///
    /// <para>Enthalten sind drei Dinge: das <b>Huld-Schwachlichtmodell</b> mit den
    /// PVGIS-Koeffizientensaetzen, die <b>Teillastkennlinie des Wechselrichters</b> und
    /// die <b>Zuordnung der Importtexte</b> auf die fuenf Persistenzwerte der Spalte
    /// <c>Technologie</c>. Die anisotrope Transposition nach Hay-Davies steht dagegen
    /// bei der Sonnengeometrie (<see cref="SolarCalculator.CalculateHourlyHayDavies"/>),
    /// weil sie deren Zwischengroessen braucht.</para>
    ///
    /// <para><b>Nichts davon wirkt im Modell EINFACH.</b> Der Rechenweg aus Paket A
    /// bleibt Zeichen fuer Zeichen unberuehrt; diese Klasse wird dort nicht aufgerufen.</para>
    /// </summary>
    public static class PvErweitertesModell
    {
        // =================================================================================
        // Huld-Schwachlichtmodell (E2.3)
        // =================================================================================

        /// <summary>
        /// Untergrenze der bezogenen Einstrahlung <c>G' = G_t / 1000</c>, unterhalb derer
        /// das Huld-Modell 0 liefert (= 1 W/m²).
        ///
        /// <para><b>Warum eine Grenze noetig ist:</b> Das Modell rechnet mit
        /// <c>ln G'</c>. Fuer <c>G' → 0</c> laeuft der Logarithmus gegen −∞, das Quadrat
        /// gegen +∞ — der relative Wirkungsgrad explodiert, und <c>G' = 0</c> ergaebe
        /// ohne die Klemme <c>0 · (−∞)</c>, also <c>NaN</c>. Unterhalb 1 W/m² erzeugt
        /// ein Modul ohnehin nichts.</para>
        /// </summary>
        public const double G_STRICH_MIN = 0.001;

        /// <summary>
        /// Die sechs Huld-Koeffizienten je Zelltechnologie (Huld u. a. 2010/2011, in
        /// PVGIS 5 und in <c>pvlib.pvarray.huld</c> derselbe Satz — am 03.09.2026 gegen
        /// die pvlib-Quelle <c>pvlib/pvarray.py</c> geprueft, Zeichen fuer Zeichen
        /// gleich; pvlib fuehrt daneben einen zweiten, neueren PVGIS-6-Satz, den das
        /// Konzept NICHT vorgibt).
        ///
        /// <para><b>Zur Schreibweise:</b> Das Konzept (und die PVGIS-Veroeffentlichung)
        /// notieren das Modell RELATIV —
        /// <c>P_DC = P_STC · G' · (1 + k1 ln G' + …)</c>, also mit
        /// <c>η_rel(G' = 1, T' = 0) = 1</c> als Pruefwert. pvlib schreibt dieselbe
        /// Gleichung mit ausgeklammertem <c>P_dc0</c>; fuer <c>P_dc0 = 1</c> sind beide
        /// identisch. Umgesetzt ist die relative Form — nur sie hat den exakten
        /// Pruefwert.</para>
        ///
        /// <para><b>Fuer <c>A_SI</c> und <c>SONSTIGE</c> gibt es keinen Satz</b> (und
        /// fuer eine nicht gepflegte Technologie erst recht nicht). Das erweiterte
        /// Modell faellt dort auf die Modulformel des einfachen Modells zurueck und
        /// sagt es im Protokoll — eine fremde Kennlinie waere schlechter als die
        /// bekannte lineare Naeherung.</para>
        /// </summary>
        /// <returns><c>null</c>, wenn es fuer diese Technologie keinen Satz gibt.</returns>
        public static double[] HuldKoeffizienten(string technologie)
        {
            if (string.IsNullOrEmpty(technologie)) return null;

            if (string.Equals(technologie, DbWerte.PV_TECHNOLOGIE_C_SI, StringComparison.Ordinal))
                return new[] { -0.017237, -0.040465, -0.004702, 0.000149, 0.000170, 0.000005 };

            if (string.Equals(technologie, DbWerte.PV_TECHNOLOGIE_CIS, StringComparison.Ordinal))
                return new[] { -0.005554, -0.038724, -0.003723, -0.000905, -0.001256, 0.000001 };

            if (string.Equals(technologie, DbWerte.PV_TECHNOLOGIE_CDTE, StringComparison.Ordinal))
                return new[] { -0.046689, -0.072844, -0.002262, 0.000276, 0.000159, -0.000006 };

            return null;
        }

        /// <summary>
        /// Der relative Wirkungsgrad des Huld-Modells:
        /// <c>η_rel = 1 + k1·lnG' + k2·(lnG')² + k3·T' + k4·T'·lnG' + k5·T'·(lnG')² + k6·T'²</c>.
        ///
        /// <para><b>Er ersetzt in ERWEITERT den linearen γ-Gang vollstaendig</b> — die
        /// Temperaturabhaengigkeit steckt in k3…k6. <c>gamma_PMP</c> bleibt die Groesse
        /// des einfachen Modells.</para>
        ///
        /// <para><b>Pruefwert:</b> <c>EtaRelativ(k, 1, 0) == 1.0</c> exakt, fuer jeden
        /// Koeffizientensatz — <c>Math.Log(1.0)</c> ist exakt 0, und alle sechs
        /// Summanden enthalten entweder <c>lnG'</c> oder <c>T'</c>.</para>
        /// </summary>
        /// <param name="k">Koeffizientensatz aus <see cref="HuldKoeffizienten"/>.</param>
        /// <param name="gStrich">G' = Einstrahlung auf die Modulebene / 1000 [kW/m²].</param>
        /// <param name="tStrich">T' = Zelltemperatur − 25 [K].</param>
        public static double EtaRelativ(double[] k, double gStrich, double tStrich)
        {
            if (k == null || k.Length < 6) return 1.0;
            if (gStrich <= 0.0) return 0.0;

            double lnG = Math.Log(gStrich);
            double lnG2 = lnG * lnG;

            return 1.0
                 + k[0] * lnG
                 + k[1] * lnG2
                 + k[2] * tStrich
                 + k[3] * tStrich * lnG
                 + k[4] * tStrich * lnG2
                 + k[5] * tStrich * tStrich;
        }

        /// <summary>
        /// Die Gleichstromleistung eines Modulfelds nach Huld [kW]:
        /// <c>P_DC = P_STC · G' · η_rel</c>.
        ///
        /// <para>Unter <see cref="G_STRICH_MIN"/> ist das Ergebnis 0. Ein NEGATIVES
        /// Ergebnis wird auf 0 geklemmt: Bei sehr wenig Licht kann <c>η_rel</c>
        /// rechnerisch unter null laufen, ein Modul speist aber nicht ein.</para>
        /// </summary>
        /// <param name="pStcKw">Nennleistung des Modulfelds [kWp].</param>
        /// <param name="gTilted">Einstrahlung auf die Modulebene [W/m²].</param>
        /// <param name="tZelle">Zelltemperatur [°C].</param>
        public static double LeistungHuld(double[] k, double pStcKw, double gTilted, double tZelle)
        {
            double gStrich = gTilted / 1000.0;
            if (gStrich < G_STRICH_MIN) return 0.0;

            double p = pStcKw * gStrich * EtaRelativ(k, gStrich, tZelle - 25.0);
            return p > 0.0 ? p : 0.0;
        }

        // =================================================================================
        // Wechselrichter-Teillastkennlinie (E2.2)
        // =================================================================================

        /// <summary>Wirkungsgrad bei 10 % Auslastung, wenn die Anlage keinen fuehrt.</summary>
        public const double WR_ETA10_VORGABE = 0.94;

        /// <summary>Wirkungsgrad bei 50 % Auslastung, wenn die Anlage keinen fuehrt.</summary>
        public const double WR_ETA50_VORGABE = 0.975;

        /// <summary>Wirkungsgrad bei 100 % Auslastung, wenn die Anlage keinen fuehrt.</summary>
        public const double WR_ETA100_VORGABE = 0.97;

        /// <summary>Untere Stuetzstelle der Kennlinie (10 % Auslastung).</summary>
        public const double AUSLASTUNG_UNTEN = 0.1;

        /// <summary>Mittlere Stuetzstelle (50 %).</summary>
        public const double AUSLASTUNG_MITTE = 0.5;

        /// <summary>Obere Stuetzstelle (100 % = AC-Nennleistung).</summary>
        public const double AUSLASTUNG_OBEN = 1.0;

        /// <summary>
        /// Der Wirkungsgrad des Wechselrichters bei der Auslastung
        /// <paramref name="auslastung"/> = <c>P_DC,sys / P_AC,nenn</c> — lineare
        /// Interpolation ueber die drei Stuetzstellen (0,1; η10), (0,5; η50),
        /// (1,0; η100).
        ///
        /// <list type="bullet">
        ///   <item><description>unter 10 %: linear von (0; 0) auf (0,1; η10) — der
        ///     Wechselrichter braucht eine Mindestleistung, um ueberhaupt
        ///     einzuspeisen;</description></item>
        ///   <item><description>ueber 100 %: konstant η100 — dahinter greift das
        ///     Clipping, nicht ein weiter fallender Wirkungsgrad.</description></item>
        /// </list>
        ///
        /// <para><b>An den drei Stuetzstellen ist das Ergebnis exakt der eingegebene
        /// Wert</b> (Pruefkriterium): Die Interpolationsformeln sind so geschrieben,
        /// dass am linken Rand der Zaehler exakt 0 wird und am rechten Rand der
        /// jeweils naechste Abschnitt beginnt.</para>
        /// </summary>
        public static double EtaWechselrichter(double auslastung, double eta10, double eta50, double eta100)
        {
            if (auslastung <= 0.0) return 0.0;

            if (auslastung < AUSLASTUNG_UNTEN)
                return eta10 * auslastung / AUSLASTUNG_UNTEN;

            if (auslastung < AUSLASTUNG_MITTE)
                return eta10 + (eta50 - eta10) * (auslastung - AUSLASTUNG_UNTEN)
                                              / (AUSLASTUNG_MITTE - AUSLASTUNG_UNTEN);

            if (auslastung < AUSLASTUNG_OBEN)
                return eta50 + (eta100 - eta50) * (auslastung - AUSLASTUNG_MITTE)
                                                / (AUSLASTUNG_OBEN - AUSLASTUNG_MITTE);

            return eta100;
        }

        // =================================================================================
        // Zuordnung der Importtexte auf die Persistenzwerte (E2.3)
        // =================================================================================

        /// <summary>
        /// Die Technologiebezeichnung der CEC-Datenbank (<c>Technology</c>) auf einen der
        /// fuenf Persistenzwerte.
        ///
        /// <para>Verglichen wird auf TEILZEICHENKETTEN und in dieser Reihenfolge, weil
        /// die CEC-Texte keine geschlossene Werteliste sind (Beispiele der Datei
        /// „CEC Modules": „Mono-c-Si", „Multi-c-Si", „Thin Film", „CIGS", „CdTe",
        /// „a-Si/nc", „HIT-Si"). <c>a-Si</c> steht VOR <c>c-Si</c>, sonst faenge die
        /// Silizium-Regel die Duennschichtmodule mit ein.</para>
        ///
        /// <para><b>Ergaenzung gegenueber dem Konzepttext:</b> <c>HIT</c>, <c>PERC</c>
        /// und <c>TOPCon</c> fallen ebenfalls auf <c>C_SI</c>. Es sind kristalline
        /// Siliziumzellen; sie unter „SONSTIGE" zu fuehren, haette ihnen ohne Not den
        /// Koeffizientensatz genommen.</para>
        /// </summary>
        public static string TechnologieAusCec(string cecText)
        {
            return TechnologieAusText(cecText);
        }

        /// <summary>
        /// Der PVsyst-Schluessel einer PAN-Datei (<c>Technol</c>, z. B. <c>mtSiMono</c>,
        /// <c>mtCIS</c>, <c>mtCdTe</c>, <c>mtAmorphous</c>) auf einen der fuenf
        /// Persistenzwerte. Dieselbe Teilzeichenketten-Regel wie
        /// <see cref="TechnologieAusCec"/> — die PVsyst-Schluessel enthalten dieselben
        /// Kuerzel (<c>CdTe</c>, <c>CIS</c>, <c>Amorphous</c>, <c>SiMono</c>,
        /// <c>SiPoly</c>, <c>HIT</c>, <c>TOPCon</c>).
        /// </summary>
        public static string TechnologieAusPan(string panTechnol)
        {
            return TechnologieAusText(panTechnol);
        }

        /// <summary>
        /// Die gemeinsame Regel hinter <see cref="TechnologieAusCec"/> und
        /// <see cref="TechnologieAusPan"/>. <c>null</c> bei leerem Eingabetext — „nicht
        /// gepflegt" ist etwas anderes als „SONSTIGE".
        /// </summary>
        public static string TechnologieAusText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            string t = text.Trim().ToLowerInvariant();

            if (t.Contains("cdte")) return DbWerte.PV_TECHNOLOGIE_CDTE;
            if (t.Contains("cigs") || t.Contains("cis")) return DbWerte.PV_TECHNOLOGIE_CIS;
            if (t.Contains("a-si") || t.Contains("asi") || t.Contains("amorph") ||
                t.Contains("thin film") || t.Contains("thinfilm"))
                return DbWerte.PV_TECHNOLOGIE_A_SI;
            if (t.Contains("c-si") || t.Contains("csi") || t.Contains("mono") ||
                t.Contains("multi") || t.Contains("poly") || t.Contains("hit") ||
                t.Contains("perc") || t.Contains("topcon"))
                return DbWerte.PV_TECHNOLOGIE_C_SI;

            return DbWerte.PV_TECHNOLOGIE_SONSTIGE;
        }
    }
}
