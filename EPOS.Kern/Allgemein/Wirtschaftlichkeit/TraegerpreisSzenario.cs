using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E9a (Schritt C, Schemaschritt 117) — <b>die Trägerpreise je Szenario</b>:
    /// Arbeits-, Grund- und Leistungspreis eines Energieträgers im Projekt für Günstig (Best)
    /// und Ungünstig (Worst), gepflegt an der Projektübersteuerung
    /// (<c>energy_project_settings</c>, Spalten aus
    /// <see cref="SchemaKatalog.Schritt117_TraegerpreisSzenario"/>), gelesen über
    /// <see cref="EnergietraegerPreisCtrl.SzenarioLesen"/>, geschrieben über
    /// <see cref="EnergietraegerPreisCtrl.SzenarioSchreiben"/>.
    ///
    /// <para><b>Die Einheit ist die der Erwartet-Spalten</b> (<c>custom_price_work</c> je
    /// Abrechnungseinheit, <c>custom_price_base</c> in €/a, <c>custom_price_power</c> je
    /// Leistungspreis-Modus des Trägers) — ein Szenariopreis ist dieselbe Größe mit einem
    /// anderen Wert.</para>
    ///
    /// <para><b>Die EINE Regel</b> (<see cref="Wirksam"/>, Konzept § 2.11.5; Entscheid
    /// E9a‑Q3, Lesart a): Ein gepflegter Szenariopreis ersetzt den wirksamen Erwartet-Preis
    /// des Trägers ALS GANZES — nach der vollständigen Rückfallkette Projektwert → Preisstand
    /// → Katalog. NULL und 0 heißen „wie Erwartet" (E9a‑Q5, Lesart a), und ein Wert, der sich
    /// nicht um mehr als <see cref="SzenarioSatz.GEPFLEGT_SCHWELLE"/> vom Erwartet-Preis
    /// unterscheidet, ist keine Pflege. Die Preisanteile der Zerlegung bleiben prozentual.
    /// Wer den Preis eines Trägers je Szenario braucht, fragt hier — eine zweite Fassung der
    /// Regel gibt es nicht (<c>KostenEmissionRechner.LadeTraeger</c>,
    /// <c>WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh</c>).</para>
    /// </summary>
    public sealed class TraegerpreisSzenario
    {
        /// <summary>Arbeitspreis Günstig je Abrechnungseinheit; null = wie Erwartet.</summary>
        public double? ArbeitspreisBest { get; set; }

        /// <summary>Arbeitspreis Ungünstig je Abrechnungseinheit; null = wie Erwartet.</summary>
        public double? ArbeitspreisWorst { get; set; }

        /// <summary>Grundpreis Günstig [€/a]; null = wie Erwartet.</summary>
        public double? GrundpreisBest { get; set; }

        /// <summary>Grundpreis Ungünstig [€/a]; null = wie Erwartet.</summary>
        public double? GrundpreisWorst { get; set; }

        /// <summary>Leistungspreis Günstig (Einheit wie <c>custom_price_power</c>); null = wie
        /// Erwartet.</summary>
        public double? LeistungspreisBest { get; set; }

        /// <summary>Leistungspreis Ungünstig (Einheit wie <c>custom_price_power</c>); null = wie
        /// Erwartet.</summary>
        public double? LeistungspreisWorst { get; set; }

        /// <summary>true, wenn keines der sechs Felder einen Wert ungleich 0 trägt — dann
        /// rechnet der Träger in allen drei Szenarien mit seinem Erwartet-Preis.</summary>
        public bool Leer
        {
            get
            {
                return !Wert(ArbeitspreisBest) && !Wert(ArbeitspreisWorst) &&
                       !Wert(GrundpreisBest) && !Wert(GrundpreisWorst) &&
                       !Wert(LeistungspreisBest) && !Wert(LeistungspreisWorst);
            }
        }

        /// <summary>Der Arbeitspreis eines Szenarios, wie gepflegt; null für ERWARTET.</summary>
        public double? Arbeitspreis(string szenario)
        {
            return Waehle(szenario, ArbeitspreisBest, ArbeitspreisWorst);
        }

        /// <summary>Der Grundpreis eines Szenarios, wie gepflegt; null für ERWARTET.</summary>
        public double? Grundpreis(string szenario)
        {
            return Waehle(szenario, GrundpreisBest, GrundpreisWorst);
        }

        /// <summary>Der Leistungspreis eines Szenarios, wie gepflegt; null für ERWARTET.</summary>
        public double? Leistungspreis(string szenario)
        {
            return Waehle(szenario, LeistungspreisBest, LeistungspreisWorst);
        }

        /// <summary>
        /// DIE EINE REGEL: der wirksame Preis eines Szenarios aus dem wirksamen Erwartet-Preis
        /// und dem gepflegten Szenariowert. Ohne Pflege (null, 0 oder gleich dem
        /// Erwartet-Preis) kommt der Erwartet-Preis unverändert zurück — dieselbe Referenz,
        /// dasselbe Bit.
        /// </summary>
        /// <param name="erwartet">Der wirksame Erwartet-Preis; <c>null</c> = keiner gepflegt.</param>
        /// <param name="szenariowert">Der Szenariowert (<see cref="Arbeitspreis"/> …).</param>
        /// <param name="gepflegt">true, wenn der Szenariowert den Preis ersetzt.</param>
        public static double? Wirksam(double? erwartet, double? szenariowert, out bool gepflegt)
        {
            gepflegt = SzenarioSatz.Gepflegt(szenariowert, erwartet ?? 0.0);
            return gepflegt ? szenariowert : erwartet;
        }

        /// <summary>Flache Kopie.</summary>
        public TraegerpreisSzenario Kopie() { return (TraegerpreisSzenario)MemberwiseClone(); }

        /// <summary>
        /// ETAPPE E9a (Bericht, Norm 9 c) — die gepflegten Trägerpreise eines Szenarios als
        /// EINE Nachweiszeile über die Stände: „Trägerpreise dieses Szenarios: ‹Stand›,
        /// ‹Träger›: Arbeitspreis …, Grundpreis …, Leistungspreis …; …". Nur, was gepflegt
        /// ist — „wie Erwartet" wird nicht wiederholt; ohne einen gepflegten Wert
        /// <c>null</c>. Die Zahlen stehen in der Einheit der Trägerkarte.
        /// </summary>
        public static string Nachweiszeile(System.Collections.Generic.IEnumerable<VariantenDaten> staende,
                                           string szenario, System.Globalization.CultureInfo kultur)
        {
            if (staende == null || (!string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal) &&
                                    !string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)))
                return null;
            var teile = new System.Collections.Generic.List<string>();
            foreach (VariantenDaten v in staende)
            {
                if (v == null) continue;
                foreach (System.Collections.Generic.KeyValuePair<int, TraegerpreisSzenario> kv in
                         EnergietraegerPreisCtrl.SzenarioJeTraeger(v.IdProjekt))
                {
                    var preise = new System.Collections.Generic.List<string>();
                    Teil(preise, MyResource.Resource.WIRT_SZ_TP_ARBEIT, kv.Value.Arbeitspreis(szenario), "N4", kultur);
                    Teil(preise, MyResource.Resource.WIRT_SZ_TP_GRUND, kv.Value.Grundpreis(szenario), "N2", kultur);
                    Teil(preise, MyResource.Resource.WIRT_SZ_TP_LEISTUNG, kv.Value.Leistungspreis(szenario), "N2", kultur);
                    if (preise.Count == 0) continue;
                    teile.Add((v.IstStamm ? "Stamm" : v.Anzeige) + ", " + Emissionsquelle.TraegerName(kv.Key) +
                              ": " + string.Join(", ", preise));
                }
            }
            return teile.Count == 0 ? null
                 : string.Format(kultur, MyResource.Resource.WIRT_SZ_TRAEGERPREISE, string.Join("; ", teile));
        }

        private static void Teil(System.Collections.Generic.List<string> ziel, string titel, double? wert,
                                 string format, System.Globalization.CultureInfo kultur)
        {
            if (wert.HasValue && wert.Value != 0) ziel.Add(titel + " " + wert.Value.ToString(format, kultur));
        }

        private static bool Wert(double? w) { return w.HasValue && w.Value != 0; }

        private static double? Waehle(string szenario, double? best, double? worst)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal)) return best;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)) return worst;
            return null;
        }
    }
}
