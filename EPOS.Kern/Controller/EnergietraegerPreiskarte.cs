using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EINHEITEN- UND UMRECHNUNGSRECHNUNG der Trägerkarte — ohne Datenbank,
    /// ohne Oberfläche, damit sie sich einzeln nachweisen lässt.
    ///
    /// <para><b>Die Leitregel: Heizwert und Brennwert sind Stoffwerte je
    /// ABRECHNUNGSEINHEIT</b> (<c>energy_carrier.billing_unit</c>: Nm³, L, kg,
    /// kWh) und tragen immer die Einheit <c>kWh/&lt;Abrechnungseinheit&gt;</c>.
    /// Sie werden nie mit der Preisbasis umgerechnet — ein Brennstoff hat einen
    /// Heizwert, gleichgültig worin man ihn abrechnet.</para>
    ///
    /// <para><b>Nur der ARBEITSPREIS folgt der Preisbasis.</b> Die Klappliste
    /// „Preisbasis" sagt, in welcher Einheit der Anwender ihn eingeben will;
    /// angezeigt wird <c>Basiswert ÷ Faktor</c>, gespeichert wird immer der
    /// Basiswert je Abrechnungseinheit (<see cref="AnzeigeArbeitspreis"/> und
    /// <see cref="BasisArbeitspreis"/> sind die beiden Richtungen desselben
    /// Faktors). Der LEISTUNGSPREIS steht in €/(kW·a) bzw. €/(kW·Monat) und
    /// kennt die Preisbasis überhaupt nicht; der Grundpreis steht in €/a.</para>
    ///
    /// <para><b>Gerechnet wird über die Basiswerte</b> — Arbeitspreis je
    /// Abrechnungseinheit ÷ Heizwert je Abrechnungseinheit —, und die Formelzeile
    /// nennt die Einheiten, damit die Division lesbar bleibt:
    /// „0,50 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh".</para>
    /// </summary>
    public static class EnergietraegerPreiskarte
    {
        /// <summary>
        /// Die Einheit von Heiz- und Brennwert: <c>kWh/&lt;Abrechnungseinheit&gt;</c>.
        /// Ohne Abrechnungseinheit bleibt es bei „kWh".
        /// </summary>
        public static string HeizwertEinheit(string abrechnungseinheit)
        {
            string einheit = (abrechnungseinheit ?? "").Trim();
            return einheit.Length == 0 ? DbWerte.EINHEIT_KWH : DbWerte.EINHEIT_KWH + "/" + einheit;
        }

        /// <summary>
        /// Die Einheit des Arbeitspreises: <c>€/&lt;Preisbasis&gt;</c>. Ein Träger
        /// ohne Heizwert (Strom, Fernwärme) rechnet unmittelbar in kWh ab.
        /// </summary>
        public static string ArbeitspreisEinheit(string preisbasis, bool mitHeizwert)
        {
            if (!mitHeizwert) return "€ / " + DbWerte.EINHEIT_KWH;
            string einheit = (preisbasis ?? "").Trim();
            return einheit.Length == 0 ? "€" : "€/" + einheit;
        }

        /// <summary>Die Einheit des Leistungspreises — sie folgt dem Modus, nicht der Preisbasis.</summary>
        public static string LeistungspreisEinheit(bool monat)
        {
            return monat ? "€/(kW·Monat)" : "€/(kW·a)";
        }

        /// <summary>Ist diese Einheit die Kilowattstunde? (Schreibweise egal.)</summary>
        public static bool IstKwh(string einheit)
        {
            return string.Equals(EnergietraegerPreisCtrl.EinheitSchluessel(einheit),
                                 EnergietraegerPreisCtrl.EinheitSchluessel(DbWerte.EINHEIT_KWH),
                                 StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Arbeitspreis, wie er ANGEZEIGT wird: Basiswert je Abrechnungseinheit
        /// geteilt durch den Faktor der Preisbasis. Ein Faktor von 0 (oder keine
        /// Preisbasis) lässt den Wert stehen.
        /// </summary>
        public static double AnzeigeArbeitspreis(double basiswert, double faktor)
        {
            return faktor == 0.0 ? basiswert : basiswert / faktor;
        }

        /// <summary>Die Gegenrichtung: aus dem angezeigten Arbeitspreis der Basiswert.</summary>
        public static double BasisArbeitspreis(double anzeige, double faktor)
        {
            return faktor == 0.0 ? anzeige : anzeige * faktor;
        }

        /// <summary>
        /// Die beiden Texte der Formelgruppe: „Preis pro kWh" und die Herleitung
        /// darunter.
        /// </summary>
        public sealed class Formelzeile
        {
            /// <summary>„0,0476 €" — der Preis je Kilowattstunde.</summary>
            public string PreisJeKwh;

            /// <summary>Die Herleitung mit ihren Einheiten.</summary>
            public string Text;
        }

        /// <summary>
        /// Rechnet die Formelgruppe aus den BASISWERTEN.
        ///
        /// <para>Ein Träger ohne Heizwert — oder einer, der ohnehin nach kWh
        /// abrechnet — bekommt „Direktabrechnung nach kWh". Sonst steht die
        /// Division samt Einheiten da; steht die Preisbasis auf kWh, kommt der
        /// Hinweis „Direktabrechnung: … €/kWh" dazu, weil der Anwender den Preis
        /// dann bereits in €/kWh eingibt.</para>
        /// </summary>
        /// <param name="mitHeizwert">Führt der Träger einen Heizwert? (<c>pricing_model.has_hi</c>)</param>
        /// <param name="abrechnungseinheit">Die Einheit, in der Hi und der Basis-Arbeitspreis stehen.</param>
        /// <param name="preisbasis">Die gewählte Preisbasis der Klappliste.</param>
        /// <param name="arbeitspreisBasis">Arbeitspreis je Abrechnungseinheit [€].</param>
        /// <param name="heizwertBasis">Heizwert je Abrechnungseinheit [kWh].</param>
        /// <returns><c>null</c>, wenn sich nichts rechnen lässt (Heizwert ≤ 0) —
        /// dann bleiben die Texte stehen, wie sie sind.</returns>
        public static Formelzeile Formel(bool mitHeizwert, string abrechnungseinheit,
                                         string preisbasis, double arbeitspreisBasis,
                                         double heizwertBasis)
        {
            CultureInfo k = CultureInfo.CurrentCulture;

            if (!mitHeizwert || IstKwh(abrechnungseinheit))
            {
                return new Formelzeile
                {
                    PreisJeKwh = arbeitspreisBasis.ToString("N4", k) + " €",
                    Text = MyResource.Resource.ETV_FORMEL_DIREKT_KWH
                };
            }

            if (heizwertBasis <= 0.0) return null;

            double ergebnis = arbeitspreisBasis / heizwertBasis;
            string einheit = (abrechnungseinheit ?? "").Trim();

            // ANWENDERENTSCHEID Q7 (Mockup-Prüfung § 4) — der Arbeitspreis steht in der
            // Formelzeile mit VIER Nachkommastellen. Mit zwei rundete der Zähler weg,
            // was das Ergebnis darunter mit vier Stellen ausweist: „0,05 € ÷ 10,50 =
            // 0,0476 €" ging nicht auf, und die Formel stand dauerhaft falsch da.
            // Gerechnet wird unverändert mit dem vollen Wert.
            string text = string.Format(k, MyResource.Resource.ETV_FORMEL_JE_EINHEIT,
                                        arbeitspreisBasis.ToString("N4", k), einheit,
                                        heizwertBasis.ToString("N2", k),
                                        ergebnis.ToString("N4", k));

            if (IstKwh(preisbasis))
                text += "  " + string.Format(k, MyResource.Resource.ETV_FORMEL_DIREKT_BASIS,
                                             ergebnis.ToString("N4", k));

            return new Formelzeile { PreisJeKwh = ergebnis.ToString("N4", k) + " €", Text = text };
        }

        // =================================================================
        // Die ANZEIGEKANTE der Preisbestandteile (ET-D-1)
        // =================================================================

        /// <summary>
        /// Ein Preisanteil, wie er ANGEZEIGT wird: aus <c>ct/kWh</c> in
        /// <c>€/&lt;Abrechnungseinheit&gt;</c> über den Heizwert.
        ///
        /// <para><b>Gerechnet wird weiter in ct/kWh</b> (<see cref="Preisanteile"/>,
        /// <c>BrennstoffBestandteilCtrl</c>, Speicherweg). Diese Funktion und ihre
        /// Gegenrichtung <see cref="AnteilCtKwh"/> sind die EINE Stelle, an der ein
        /// Anteil die Einheit wechselt — genau einmal, an der Anzeigekante.</para>
        ///
        /// <para>Ohne Heizwert (≤ 0) gibt es keinen Weg in die Abrechnungseinheit;
        /// dann bleibt der Wert, wie er ist, und die Karte nennt weiter ct/kWh.</para>
        /// </summary>
        /// <param name="ctKwh">Der Anteil [ct/kWh].</param>
        /// <param name="heizwert">Heizwert je Abrechnungseinheit [kWh]; ≤ 0 = keiner.</param>
        public static double AnteilJeEinheit(double ctKwh, double heizwert)
        {
            if (heizwert <= 0.0) return ctKwh;
            double euroJeKwh = ctKwh / 100.0;
            return heizwert == 1.0 ? euroJeKwh : euroJeKwh * heizwert;
        }

        /// <summary>
        /// Die Gegenrichtung von <see cref="AnteilJeEinheit"/>:
        /// <c>€/&lt;Abrechnungseinheit&gt;</c> → <c>ct/kWh</c>.
        /// </summary>
        public static double AnteilCtKwh(double jeEinheit, double heizwert)
        {
            if (heizwert <= 0.0) return jeEinheit;
            double ct = jeEinheit * 100.0;
            return heizwert == 1.0 ? ct : ct / heizwert;
        }

        /// <summary>
        /// Die Einheit eines Preisanteils an der Anzeigekante:
        /// <c>€/&lt;Abrechnungseinheit&gt;</c>, solange ein Heizwert da ist —
        /// sonst <c>ct/kWh</c>.
        /// </summary>
        public static string AnteilEinheit(string abrechnungseinheit, double heizwert)
        {
            string einheit = (abrechnungseinheit ?? "").Trim();
            if (heizwert <= 0.0 || einheit.Length == 0 || IstKwh(einheit))
                return DbWerte.PREISREIHE_EINHEIT_CT_KWH;
            return "€/" + einheit;
        }

        /// <summary>Gramm je Kilogramm — die Brücke der CO₂-Masse.</summary>
        private const double G_JE_KG = 1000.0;

        /// <summary>
        /// Die CO₂-MASSE je Abrechnungseinheit [kg]: Emissionsfaktor [g/kWh] ×
        /// Heizwert [kWh je Einheit] ÷ 1000. Sie ist die Zahl, mit der die
        /// BEHG-Herleitung rechnet („65 €/t × 2,109 kg/m³") — eine Masse, keine
        /// Energiemenge. <c>null</c>, solange einer der beiden Werte fehlt.
        /// </summary>
        public static double? Co2MasseJeEinheit(double emissionsfaktorGKwh, double heizwert)
        {
            if (emissionsfaktorGKwh <= 0.0 || heizwert <= 0.0) return null;
            return emissionsfaktorGKwh * heizwert / G_JE_KG;
        }

        /// <summary>
        /// Der Umrechnungsfaktor Hs/Hi eines Trägers — die Brücke zwischen einem
        /// BRENNWERTbezogenen Katalogsatz (€/MWh) und dem HEIZWERTbezogenen
        /// Arbeitspreis. <c>null</c>, solange nicht beide Werte über null stehen;
        /// dann gibt es keinen belegbaren Faktor.
        /// </summary>
        public static double? FaktorHsHi(double hi, double hs)
        {
            if (hi <= 0.0 || hs <= 0.0) return null;
            return hs / hi;
        }

        /// <summary>
        /// Die Effektivzeile „effektiv: 1 Nm³ = 10,50 kWh (Hi) / 11,60 kWh (Hs)"
        /// — über der ABRECHNUNGSEINHEIT, denn dort stehen Hi und Hs.
        /// </summary>
        public static string Effektivzeile(string abrechnungseinheit, double hi, double hs)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            if (IstKwh(abrechnungseinheit))
                return MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV_KWH;

            return string.Format(k, MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV,
                                 (abrechnungseinheit ?? "").Trim(),
                                 hi.ToString("N2", k), hs.ToString("N2", k));
        }
    }
}
