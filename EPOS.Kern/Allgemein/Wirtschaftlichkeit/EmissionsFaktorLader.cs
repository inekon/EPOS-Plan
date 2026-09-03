using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Faktorsatz EINES Trägers, wie ihn ein Rechenlauf braucht
    /// (Etappe E5, <c>Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md</c> § 3/F6/F7).
    /// </summary>
    public class EmissionsFaktorSatz
    {
        /// <summary>Reines CO₂ der Art <c>CO2</c> [g/kWh]; <c>null</c> = nicht gepflegt.
        /// Diese Größe ist NIE modusabhängig — an ihr hängen die BEHG-Abgabemenge
        /// (gesetzlich reines CO₂ nach EBeV) und jede Nachweisrechnung.</summary>
        public double? Co2GKwh;

        /// <summary>CO₂-Äquivalent nach F6 [g/kWh] über die ausgewählten Arten;
        /// <c>null</c>, wenn kein einziger Wert gepflegt ist oder der Artenkatalog
        /// fehlt (dann gilt <see cref="Co2GKwh"/> auch im Modus CO2E).</summary>
        public double? Co2eGKwh;

        /// <summary>Schwefeldioxid in der Einheit der Art (Auslieferung mg/kWh) —
        /// unverändert wie bisher, NICHT modusabhängig.</summary>
        public double? So2;

        /// <summary>Stickoxide in der Einheit der Art (Auslieferung mg/kWh) —
        /// unverändert wie bisher, NICHT modusabhängig.</summary>
        public double? Nox;

        /// <summary>Der CO₂-Wert ist selbst schon ein Äquivalent (F3) — dann IST die
        /// Summe genau dieser Wert.</summary>
        public bool Co2IstAequivalent;

        /// <summary>Ebene, aus der der CO₂-Wert stammt — für Protokoll und Prüfstand
        /// (<c>PROJEKT</c>, <c>KATALOG</c>, <c>STAMM</c>, <c>CARRIER</c>, <c>-</c>).</summary>
        public string Co2Ebene = "-";

        /// <summary>
        /// Der im gegebenen Modus WIRKSAME CO₂-Faktor [g/kWh] (F7). Im Modus
        /// <c>CO2E</c> die Summe nach F6; fehlt sie (kein Artenkatalog), bleibt es beim
        /// reinen CO₂ — eine Anwendung ohne Migrationsschritt 57 rechnet damit weiter
        /// wie bisher statt gar nicht.
        /// </summary>
        public double? Wirksam(string modus)
        {
            if (!EmissionsAusweis.IstAequivalent(modus)) return Co2GKwh;
            return Co2eGKwh ?? Co2GKwh;
        }
    }

    /// <summary>
    /// DIE Lesekette der Emissionsfaktoren je Träger — eine einzige Fassung für
    /// <see cref="KostenEmissionRechner"/> und <see cref="EmissionsBilanzRechner"/>
    /// (Etappe E5, Konzept § 3).
    ///
    /// <para><b>Lesekette je Emissionsart:</b></para>
    /// <list type="number">
    ///   <item><description><b>Projektwert</b> <c>energy_project_settings.co2/so2/nox</c>
    ///     — nur die drei Kernarten, nur im Projektkontext.</description></item>
    ///   <item><description><b>Aktive <c>emissionswert</c>-Zeile</b> des Trägers
    ///     (Etappe E2/E3) — für JEDE Art, auch CH₄ und N₂O.</description></item>
    ///   <item><description><b><c>Tab_Brennstoff_Stamm</c></b> über
    ///     <c>energy_carrier.id_brennstoff</c> — nur Kernarten.</description></item>
    ///   <item><description><b>Altspalte <c>energy_carrier.co2/so2/nox</c></b> —
    ///     nur Kernarten (F9).</description></item>
    /// </list>
    ///
    /// <para><b>Umsetzungsklärung 1 — warum der Projektwert VOR dem Katalog steht.</b>
    /// Konzept § 3 sagt „aktive Zeile → sonst Altspalten-Kette". Wörtlich genommen
    /// stünde der Katalog vor der Projektübersteuerung — und jedes Projekt verlöre
    /// seinen eigenen Faktor an den Katalog, sobald E5 greift. Das wäre ein
    /// Regressionsfehler und keine Verbesserung: Die Projektspalte ist bis heute die
    /// oberste Ebene beider Rechner. Sie bleibt es.</para>
    ///
    /// <para><b>Umsetzungsklärung 2 — ein Projektwert gilt als reines CO₂.</b> Zu
    /// einer Zahl in <c>energy_project_settings.co2</c> gibt es keine Herkunft: Sie
    /// kann eine Handeingabe, eine Altkopie des Katalogwertes oder ein übernommener
    /// Äquivalentwert sein. Sie wird deshalb mit <c>ist_co2e = falsch</c> geführt —
    /// im Modus CO2E rechnen die übrigen ausgewählten Arten also dazu. Das ist die
    /// konservative Deutung: Sie kann ein Äquivalent geringfügig zu hoch ansetzen,
    /// während die Gegenannahme CH₄ und N₂O stillschweigend unterschlüge.</para>
    ///
    /// <para><b>„Gepflegt" heißt größer als 0</b> — dieselbe Regel wie bisher in
    /// beiden Rechnern (<c>Erster()</c> bzw. die Vorrangkette in
    /// <c>KostenEmissionRechner.LadeTraeger</c>). Eine 0 fällt durch auf die nächste
    /// Ebene; ohne diese Regel blockierten die von Migrationsschritt 57 aus leeren
    /// Altspalten gesäten Nullzeilen den Brennstoff-Stamm.</para>
    ///
    /// <para><b>Kein Zwischenspeicher</b> — aus demselben Grund wie in
    /// <see cref="EmissionsBilanzRechner"/>: Je Lauf gibt es eine Handvoll Träger,
    /// und ein prozessweiter Cache über im Katalog pflegbare Zahlen wäre nach der
    /// ersten Änderung falsch.</para>
    /// </summary>
    public static class EmissionsFaktorLader
    {
        /// <summary>Herkunftsebene: Projektübersteuerung (Altspalte).</summary>
        public const string EBENE_PROJEKT = "PROJEKT";

        /// <summary>Herkunftsebene: aktive Zeile des Emissionswert-Katalogs.</summary>
        public const string EBENE_KATALOG = "KATALOG";

        /// <summary>Herkunftsebene: <c>Tab_Brennstoff_Stamm</c>.</summary>
        public const string EBENE_STAMM = "STAMM";

        /// <summary>Herkunftsebene: Altspalte in <c>energy_carrier</c> (F9).</summary>
        public const string EBENE_CARRIER = "CARRIER";

        /// <summary>
        /// Der Faktorsatz eines Trägers. <paramref name="idProjekt"/> <c>0</c> liest
        /// ohne Projektübersteuerung (Katalogsicht).
        /// </summary>
        public static EmissionsFaktorSatz Lade(int idProjekt, int carrierId)
        {
            var satz = new EmissionsFaktorSatz();
            if (carrierId <= 0) return satz;

            // Fehlt der Artenkatalog (Migrationsschritt 57 nicht gelaufen), liefert
            // Arten() eine leere Liste. Statt eines zweiten Rechenwegs treten dann drei
            // ERSATZARTEN an - damit bleibt es bei EINER Kette, und eine nicht migrierte
            // Datenbank rechnet weiter wie bisher (F9).
            List<EmissionsartModel> arten = EmissionskatalogCtrl.Arten(true);
            if (arten.Count == 0) arten = ErsatzKernarten();

            Dictionary<int, EmissionswertModel> aktive = EmissionskatalogCtrl.AktiveWerte(carrierId);
            Dictionary<string, double?> stamm = AltwerteStamm(carrierId);
            Dictionary<string, double?> carrier = AltwerteCarrier(carrierId);
            Dictionary<string, double?> projekt = idProjekt > 0
                ? AltwerteProjekt(idProjekt, carrierId)
                : new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

            var zeilen = new List<EmissionsZeile>();
            bool irgendeinWert = false;

            foreach (EmissionsartModel a in arten)
            {
                var z = new EmissionsZeile { Art = a };
                bool kern = EmissionenCtrl.IstKernart(a.Kuerzel);
                string ebene = "-";

                // 1. Projektübersteuerung (Umsetzungsklärung 1 + 2).
                double? wert = kern ? Gepflegt(projekt, a.Kuerzel) : null;
                if (wert.HasValue) ebene = EBENE_PROJEKT;

                // 2. Aktive Katalogzeile - die einzige Ebene, die auch Nicht-Kernarten führt.
                if (!wert.HasValue)
                {
                    EmissionswertModel w;
                    if (aktive.TryGetValue(a.ID, out w) && w != null &&
                        w.Wert.HasValue && w.Wert.Value > 0)
                    {
                        wert = w.Wert;
                        z.IstCo2e = w.IstCo2e;
                        z.Quelle = w.Quelle;
                        z.QuelleText = w.Herkunftstext;
                        ebene = EBENE_KATALOG;
                    }
                }

                // 3./4. Altspalten-Kette der Kernarten (F9).
                if (!wert.HasValue && kern)
                {
                    wert = Gepflegt(stamm, a.Kuerzel);
                    if (wert.HasValue) ebene = EBENE_STAMM;
                }
                if (!wert.HasValue && kern)
                {
                    wert = Gepflegt(carrier, a.Kuerzel);
                    if (wert.HasValue) ebene = EBENE_CARRIER;
                }

                z.Wert = wert;
                if (wert.HasValue) irgendeinWert = true;
                zeilen.Add(z);

                if (string.Equals(a.Kuerzel, DbWerte.EMISSIONSART_CO2, StringComparison.OrdinalIgnoreCase))
                {
                    // Der CO2-Wert wird UNVERÄNDERT durchgereicht: Beide Rechner führen
                    // ihn seit jeher in g/kWh, und die Altspalten sind genau das. Die
                    // Einheitennormierung (F4) gehört in die Summe F6, nicht hierher.
                    satz.Co2GKwh = wert;
                    satz.Co2IstAequivalent = z.IstCo2e;
                    satz.Co2Ebene = ebene;
                }
                else if (string.Equals(a.Kuerzel, DbWerte.EMISSIONSART_SO2, StringComparison.OrdinalIgnoreCase))
                    satz.So2 = wert;
                else if (string.Equals(a.Kuerzel, DbWerte.EMISSIONSART_NOX, StringComparison.OrdinalIgnoreCase))
                    satz.Nox = wert;
            }

            // CO2e-Summe nach F6, Sonderfall F3 - dieselbe Fassung wie im Reiter.
            if (irgendeinWert) satz.Co2eGKwh = EmissionenCtrl.SummeCo2eGKwh(zeilen);
            return satz;
        }

        /// <summary>
        /// Die drei Kernarten als Behelf, wenn der Artenkatalog fehlt. Sie tragen die
        /// Auslieferungsangaben aus Konzept F2/F4 (CO₂ g/kWh mit GWP 1; SO₂ und NOx
        /// mg/kWh mit GWP 0) — die CO₂e-Summe ist damit gleich dem CO₂-Wert, und der
        /// Modus CO2E rechnet wie CO2 statt gar nicht.
        /// </summary>
        private static List<EmissionsartModel> ErsatzKernarten()
        {
            return new List<EmissionsartModel>
            {
                new EmissionsartModel { Kuerzel = DbWerte.EMISSIONSART_CO2,
                    Einheit = DbWerte.EMISSION_EINHEIT_G_KWH, Co2Aequivalent = 1.0, IstPflicht = true },
                new EmissionsartModel { Kuerzel = DbWerte.EMISSIONSART_SO2,
                    Einheit = DbWerte.EMISSION_EINHEIT_MG_KWH, Co2Aequivalent = 0.0 },
                new EmissionsartModel { Kuerzel = DbWerte.EMISSIONSART_NOX,
                    Einheit = DbWerte.EMISSION_EINHEIT_MG_KWH, Co2Aequivalent = 0.0 }
            };
        }

        // ------------------------------------------------------------- Altspalten

        private static double? Gepflegt(Dictionary<string, double?> d, string kuerzel)
        {
            double? w;
            if (d == null || !d.TryGetValue(kuerzel, out w)) return null;
            return (w.HasValue && w.Value > 0) ? w : null;
        }

        private static Dictionary<string, double?> AltwerteProjekt(int idProjekt, int carrierId)
        {
            return Lies("SELECT co2, so2, nox FROM energy_project_settings " +
                        "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                        new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
        }

        private static Dictionary<string, double?> AltwerteStamm(int carrierId)
        {
            return Lies("SELECT bs.CO2 AS co2, bs.SO2 AS so2, bs.NOx AS nox " +
                        "FROM energy_carrier AS ec " +
                        "INNER JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                        "WHERE ec.id = ?",
                        new DbParam("@c", carrierId));
        }

        private static Dictionary<string, double?> AltwerteCarrier(int carrierId)
        {
            return Lies("SELECT co2, so2, nox FROM energy_carrier WHERE id = ?",
                        new DbParam("@c", carrierId));
        }

        private static Dictionary<string, double?> Lies(string sql, params DbParam[] p)
        {
            var d = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = DataRepository.GetDataTable(sql, p);
                if (dt == null || dt.Rows.Count == 0) return d;
                DataRow r = dt.Rows[0];
                d[DbWerte.EMISSIONSART_CO2] = D(r, "co2");
                d[DbWerte.EMISSIONSART_SO2] = D(r, "so2");
                d[DbWerte.EMISSIONSART_NOX] = D(r, "nox");
            }
            catch { }
            return d;
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); }
            catch { return null; }
        }
    }
}
