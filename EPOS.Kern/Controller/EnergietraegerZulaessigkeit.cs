using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>DIE eine Wahrheit: welcher Energieträger passt zu welcher Komponente</b>
    /// (Anwenderwunsch 14.09.2026: „Die Energieträgerverwaltung sollte nur die
    /// zugelassenen Energieträger für die ausgewählte Komponente zulassen — zum
    /// Beispiel Wärmepumpe → Strom.").
    ///
    /// <para><b>Die Regel, je Erzeugerart</b> (Persistenzwerte aus
    /// <see cref="DbWerte"/>):</para>
    /// <list type="bullet">
    ///   <item><description><b>Wärmepumpe, Photovoltaik, Stromspeicher, Heizstab</b> —
    ///     die elektrische Welt bezieht Strom, sonst nichts.
    ///     <see cref="ProjektEnergietraegerCtrl.StandardStromTraeger"/> beantwortet
    ///     dieselbe Frage für die VORBELEGUNG; hier steht, was überhaupt wählbar
    ///     ist.</description></item>
    ///   <item><description><b>Heizkessel</b> — die Brennstoffkategorie des Geräts:
    ///     ein Gaskessel bekommt Gasträger, ein Elektrokessel Strom, ein Holzkessel
    ///     feste Brennstoffe. Ohne Gerät gibt es keine Einengung, denn ein Kessel kann
    ///     alles verbrennen und den Strom dazu.</description></item>
    ///   <item><description><b>BHKW</b> — gasförmige und flüssige Brennstoffe (darin
    ///     Erdgas, Biogas und Heizöl); mit Gerät wie beim Kessel die Kategorie des
    ///     Geräts.</description></item>
    ///   <item><description><b>Solarthermie, Pufferspeicher</b> — sie beziehen keine
    ///     Energie und haben deshalb KEINEN Träger; dieselbe Aussage trifft
    ///     <see cref="ProjektEnergietraegerCtrl.Verwendete"/>, die beide bewusst
    ///     übergeht.</description></item>
    ///   <item><description><b>Ohne Erzeugerart</b> (Menü Administration, Knopf auf der
    ///     Kostenseite) — keine Einengung.</description></item>
    /// </list>
    ///
    /// <para><b>Gerechnet wird über den KATEGORIECODE, angezeigt über die GRUPPE.</b>
    /// <c>Tab_BrennstoffKategorien</c> führt beides: <c>Code</c> ist der sprachfreie
    /// Schlüssel (ANIMAL_FAT, ELECTRICITY, GASEOUS_FUEL, HEAT, LIQUID_FUEL,
    /// SOLID_FUEL) und steht zeichengleich in <c>energy_carrier.pricing_model</c>;
    /// <c>Gruppe</c> ist der Anzeigename („Gas", „Öl", „Strom" …) und steht in
    /// <c>energy_carrier.group_code</c>, nach dem die Energieträgerverwaltung ihre
    /// Liste gliedert. Die Einengung greift deshalb am Code und wird für Liste und
    /// Kopfzeile in Gruppen übersetzt — den umgekehrten Weg verbietet der Befund bei
    /// <see cref="DbWerte.UMRECHNUNG_NAME_Z_FAKTOR"/> ausdrücklich: Wasserstoff führt
    /// den eigenen Gruppennamen bei <c>pricing_model = GASEOUS_FUEL</c> und fiele
    /// sonst aus der Gasfamilie.</para>
    ///
    /// <para><b>Ein Rückgabewert, drei Aussagen.</b> <see cref="ZulaessigeGruppen"/>
    /// gibt <c>null</c> für „keine Einengung", eine LEERE Liste für „kein Träger" und
    /// sonst die Gruppennamen. Bleibt nach einer Einengung kein einziger Träger des
    /// Katalogs übrig (eine Kategorie ohne Katalogzeile, etwa „Pellets"), gilt
    /// <c>null</c>: Eine leere Auswahlliste wäre eine Sackgasse, keine Hilfe.</para>
    /// </summary>
    internal static class EnergietraegerZulaessigkeit
    {
        /// <summary>
        /// Klartext des Heizstabs — er ist ein Merkmal der Anlagenzeile
        /// (<c>Tab_Energieanlagen.Heizstab</c>), kein Gerät und keine Kostenkomponente,
        /// hebt seine Anlage aber in die elektrische Welt.
        /// </summary>
        internal const string ERZEUGER_HEIZSTAB = "Heizstab";

        /// <summary>Kategoriecode der elektrischen Welt (<c>Tab_BrennstoffKategorien.Code</c>).</summary>
        internal const string CODE_STROM = StromAufschlagCtrl.PRICING_MODEL_STROM;

        /// <summary>Kategoriecode der gasförmigen Brennstoffe.</summary>
        internal const string CODE_GASFOERMIG = "GASEOUS_FUEL";

        /// <summary>Kategoriecode der flüssigen Brennstoffe.</summary>
        internal const string CODE_FLUESSIG = "LIQUID_FUEL";

        private static readonly string[] NUR_STROM = { CODE_STROM };
        private static readonly string[] BRENNBAR_BHKW = { CODE_GASFOERMIG, CODE_FLUESSIG };
        private static readonly string[] KEINE = new string[0];

        // =====================================================================
        // Die Regel
        // =====================================================================

        /// <summary>
        /// Bezieht diese Komponente überhaupt Energie? <c>false</c> für Solarthermie
        /// und Pufferspeicher — sie tragen keinen Energieträger.
        /// </summary>
        internal static bool MitTraeger(string erzeugerart)
        {
            return !Gleich(erzeugerart, DbWerte.ERZEUGER_SOLARTHERMIE)
                && !Gleich(erzeugerart, DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER);
        }

        /// <summary>
        /// Die zulässigen KATEGORIECODES einer Komponente.
        /// <c>null</c> = keine Einengung, leere Liste = kein Träger.
        /// </summary>
        /// <param name="erzeugerart">Persistenzwert aus <see cref="DbWerte"/>
        /// (<c>ERZEUGER_*</c>, <c>KOSTEN_KOMPONENTE_PUFFERSPEICHER</c>) oder
        /// <see cref="ERZEUGER_HEIZSTAB"/>; leer = ohne Komponentenkontext.</param>
        /// <param name="geraeteId">Gerätezeile des Brenners
        /// (<c>Tab_Heizkessel.ID</c> bzw. <c>Tab_BHKW.ID</c>); 0 = unbekannt.</param>
        internal static IReadOnlyList<string> Kategoriecodes(string erzeugerart, int geraeteId)
        {
            if (string.IsNullOrWhiteSpace(erzeugerart)) return null;

            if (Gleich(erzeugerart, DbWerte.ERZEUGER_WAERMEPUMPE)
             || Gleich(erzeugerart, DbWerte.ERZEUGER_PHOTOVOLTAIK)
             || Gleich(erzeugerart, DbWerte.ERZEUGER_STROMSPEICHER)
             || Gleich(erzeugerart, ERZEUGER_HEIZSTAB))
                return NUR_STROM;

            if (!MitTraeger(erzeugerart)) return KEINE;

            bool kessel = Gleich(erzeugerart, DbWerte.ERZEUGER_HEIZKESSEL);
            bool bhkw = Gleich(erzeugerart, DbWerte.ERZEUGER_BHKW);
            if (!kessel && !bhkw) return null;   // fremde Komponente - keine Aussage

            // Der Brenner bestimmt sich über sein GERÄT: derselbe Weg, den der
            // Varianten-Anlegedialog geht (EnergietraegerVarianteCtrl.KategorieZu —
            // „Gas-Kessel → nur Gasträger", Anwenderbefund 03.09.2026).
            string code = KategoriecodeDesGeraets(erzeugerart, geraeteId);
            if (code.Length > 0) return new string[] { code };

            // Ohne Gerät: Der Kessel bleibt offen, das BHKW bekommt gasförmig und flüssig.
            return bhkw ? BRENNBAR_BHKW : null;
        }

        /// <summary>
        /// Die zulässigen GRUPPEN (<c>energy_carrier.group_code</c>) einer Komponente —
        /// das, wonach die Energieträgerverwaltung ihre Liste gliedert.
        /// <c>null</c> = keine Einengung, leere Liste = kein Träger.
        /// </summary>
        internal static IReadOnlyList<string> ZulaessigeGruppen(string erzeugerart, int geraeteId = 0)
        {
            IReadOnlyList<string> codes = Kategoriecodes(erzeugerart, geraeteId);
            if (codes == null) return null;
            if (codes.Count == 0) return KEINE;

            List<string> gruppen = GruppenZuCodes(codes);

            // Sicherheitsnetz: Eine Kategorie ohne Katalogzeile (etwa „Pellets")
            // ergäbe eine leere Auswahlliste. Dann gilt lieber keine Einengung.
            return gruppen.Count == 0 ? null : (IReadOnlyList<string>)gruppen;
        }

        /// <summary>
        /// Passt ein Träger zu den zulässigen Gruppen? <paramref name="gruppen"/>
        /// <c>null</c> = keine Einengung (alles passt), leer = nichts passt.
        /// </summary>
        internal static bool PasstGruppe(IReadOnlyList<string> gruppen, string groupCode)
        {
            if (gruppen == null) return true;
            if (gruppen.Count == 0) return false;
            if (string.IsNullOrEmpty(groupCode)) return false;

            foreach (string g in gruppen)
                if (string.Equals(g, groupCode, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>Darf dieser Katalogträger an dieser Komponente hängen? (ohne Gerätebezug)</summary>
        internal static bool IstZulaessig(string erzeugerart, int carrierId)
        {
            return IstZulaessig(erzeugerart, 0, carrierId);
        }

        /// <summary>
        /// Darf dieser Katalogträger an dieser Komponente hängen?
        /// Ohne Komponentenkontext immer <c>true</c>.
        /// </summary>
        internal static bool IstZulaessig(string erzeugerart, int geraeteId, int carrierId)
        {
            IReadOnlyList<string> gruppen = ZulaessigeGruppen(erzeugerart, geraeteId);
            if (gruppen == null) return true;
            if (gruppen.Count == 0 || carrierId <= 0) return false;
            return PasstGruppe(gruppen, GruppeDesTraegers(carrierId));
        }

        /// <summary>
        /// Der Katalog, eingeengt auf die zulässigen Träger einer Komponente — sortiert
        /// nach Gruppe und Name, wie die Energieträgerverwaltung ihn listet. Leere
        /// Liste = die Komponente bezieht keine Energie.
        /// </summary>
        internal static List<EnergyCarrier> ZulaessigerKatalog(string erzeugerart, int geraeteId = 0)
        {
            List<EnergyCarrier> alle;
            try { alle = KostenSummenCtrl.GetAllCarriers(0); }
            catch { alle = new List<EnergyCarrier>(); }

            IReadOnlyList<string> gruppen = ZulaessigeGruppen(erzeugerart, geraeteId);
            List<EnergyCarrier> liste;
            if (gruppen == null)
            {
                liste = alle;
            }
            else
            {
                liste = new List<EnergyCarrier>();
                foreach (EnergyCarrier c in alle)
                    if (PasstGruppe(gruppen, c.GroupCode)) liste.Add(c);
            }

            liste.Sort(delegate (EnergyCarrier a, EnergyCarrier b)
            {
                int g = string.Compare(a.GroupCode ?? "", b.GroupCode ?? "",
                                       StringComparison.CurrentCultureIgnoreCase);
                return g != 0 ? g : string.Compare(a.Name ?? "", b.Name ?? "",
                                       StringComparison.CurrentCultureIgnoreCase);
            });
            return liste;
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Der Kategoriecode des Geräts einer Brennerkomponente; leer, wenn die
        /// Komponente kein Gerät führt oder der Brennstoff unbekannt ist.
        /// </summary>
        private static string KategoriecodeDesGeraets(string erzeugerart, int geraeteId)
        {
            if (geraeteId <= 0) return "";

            int brennstoff = BrennstoffDesGeraets(erzeugerart, geraeteId);
            if (brennstoff <= 0) return "";

            // EINE Wahrheit für „welche Kategorie hat dieser Brennstoff": dieselbe
            // Abfrage, die der Varianten-Anlegedialog zieht.
            int kategorie = EnergietraegerVarianteCtrl.KategorieZu(brennstoff);
            if (kategorie <= 0) return "";

            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Code FROM Tab_BrennstoffKategorien WHERE ID = ?",
                    new DbParam("@id", kategorie));
                return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o).Trim();
            }
            catch { return ""; }
        }

        /// <summary><c>Brennstoff</c> der Gerätezeile; 0 = keine Brennerkomponente oder unbekannt.</summary>
        private static int BrennstoffDesGeraets(string erzeugerart, int geraeteId)
        {
            try
            {
                object o = null;
                if (Gleich(erzeugerart, DbWerte.ERZEUGER_HEIZKESSEL))
                    o = DataRepository.ExecuteScalar(
                        "SELECT Brennstoff FROM Tab_Heizkessel WHERE ID = ?",
                        new DbParam("@id", geraeteId));
                else if (Gleich(erzeugerart, DbWerte.ERZEUGER_BHKW))
                    o = DataRepository.ExecuteScalar(
                        "SELECT Brennstoff FROM Tab_BHKW WHERE ID = ?",
                        new DbParam("@id", geraeteId));

                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>Die Gruppennamen, die der Katalog unter diesen Kategoriecodes führt.</summary>
        private static List<string> GruppenZuCodes(IReadOnlyList<string> codes)
        {
            var gruppen = new List<string>();
            foreach (string code in codes)
            {
                if (string.IsNullOrEmpty(code)) continue;
                try
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT DISTINCT group_code FROM energy_carrier " +
                        "WHERE pricing_model = ? AND group_code IS NOT NULL AND group_code <> '' " +
                        "ORDER BY group_code",
                        new DbParam("@c", code));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r[0] == DBNull.Value) continue;
                        string g = Convert.ToString(r[0]).Trim();
                        if (g.Length > 0 && !gruppen.Contains(g)) gruppen.Add(g);
                    }
                }
                catch { }
            }
            gruppen.Sort(StringComparer.CurrentCultureIgnoreCase);
            return gruppen;
        }

        /// <summary><c>energy_carrier.group_code</c> eines Trägers; leer, wenn unbekannt.</summary>
        private static string GruppeDesTraegers(int carrierId)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT group_code FROM energy_carrier WHERE id = ?",
                    new DbParam("@id", carrierId));
                return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o).Trim();
            }
            catch { return ""; }
        }

        private static bool Gleich(string a, string b)
        {
            return string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
