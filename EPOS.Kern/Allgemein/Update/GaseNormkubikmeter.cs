using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Schemaschritt 109 — der Stammtext der fünf Gase auf Nm³</b> (Schritt G des
    /// Analysepapiers Wirtschaftlichkeit § 6, Entscheid U‑1 Weg (a) vom 30.08.2026,
    /// Freigabe A9 vom 20.09.2026 „vor dem nächsten Vorlagenbau").
    ///
    /// <para><b>Der Befund U‑1 (Einheitenbruch).</b> Seit Schritt 26a rechnen die
    /// gasförmigen Energieträger in Normkubikmetern (<c>energy_carrier.billing_unit</c>
    /// = „Nm³", die Umrechnungsregeln ebenso) — der Brennstoffstamm
    /// <c>Tab_Brennstoff_Stamm</c> der fünf Gase (Stadtgas 1, Erdgas LL 2, Erdgas E 3,
    /// Biogas 14, Wasserstoff 25) führte aber weiter „m³". Die Zuordnung eines Trägers
    /// leitet ihre Identitätsregel aus diesem Stammtext ab
    /// (<c>WizardCtrl.ConvIdErmitteln</c>, <c>EnergietraegerKatalogCtrl</c>) und fand
    /// deshalb keine (−1), und die Preishistorie einer neuen Zuordnung bekam „m³" statt
    /// „Nm³" (eine Zeile der Testdatenbank, Projekt 1039).</para>
    ///
    /// <para><b>Der Schritt (reines DML, Muster Schritt 26a):</b> <c>Einheit</c> „m³" →
    /// „Nm³" und <c>PreisEinheit</c> „€/m3" → „€/Nm³" an genau diesen fünf Brennstoffen,
    /// dazu jede Preiszeile ihrer Träger, die noch „m³" führt. Der Brennstoff 24
    /// („Sonstige", Kategorie 11) führt ebenfalls „m³", ist aber kein Gas und bleibt,
    /// wie er ist. Gesetzt wird nur, wo der alte Text steht; der zweite Lauf fasst
    /// nichts an.</para>
    ///
    /// <para><b>Ergebnisneutral:</b> Reine Semantik — die Heizwerte sind seit jeher
    /// Normwerte; kein Zahlenwert ändert sich, und kein Rechenweg liest den Stammtext
    /// (Kosten und Emissionen lesen <c>billing_unit</c> und die Faktoren). Die
    /// Einfrierliste der Referenzbasis nennt am Brennstoffstamm nur CO₂/SO₂/NOₓ/Staub.
    /// Wirkung hat der Schritt allein bei der NÄCHSTEN Zuordnung eines Gasträgers: Sie
    /// findet jetzt die Identitätsregel und schreibt „Nm³" in die Historie.</para>
    /// </summary>
    public static class GaseNormkubikmeter
    {
        /// <summary>Die fünf Gase des Entscheids U‑1 — <c>Tab_Brennstoff_Stamm.ID</c>.</summary>
        public static readonly int[] BRENNSTOFFE = { 1, 2, 3, 14, 25 };

        /// <summary>Die Liste der fünf Ids als SQL-Literal — eine feste Konstante des
        /// Entscheids, kein zusammengesetzter Eingabewert.</summary>
        private const string LISTE = "(1, 2, 3, 14, 25)";

        /// <summary>Der alte Stammtext der Einheit.</summary>
        public const string ALT = DbWerte.EINHEIT_KUBIKMETER;

        /// <summary>Der neue Stammtext der Einheit.</summary>
        public const string NEU = DbWerte.EINHEIT_NORMKUBIKMETER;

        /// <summary>Die alte Preiseinheit, wie sie der Stamm führt (ASCII-„3").</summary>
        public const string PREIS_ALT = "€/m3";

        /// <summary>Dieselbe alte Preiseinheit mit hochgestellter „³".</summary>
        public const string PREIS_ALT_HOCH = "€/m³";

        /// <summary>Die neue Preiseinheit.</summary>
        public const string PREIS_NEU = "€/Nm³";

        /// <summary>Teil 1: die Einheit der fünf Gase.</summary>
        public const string SQL_EINHEIT =
            "UPDATE [Tab_Brennstoff_Stamm] SET [Einheit] = ? WHERE [ID] IN " + LISTE + " AND [Einheit] = ?";

        /// <summary>Teil 2: die Preiseinheit der fünf Gase.</summary>
        public const string SQL_PREISEINHEIT =
            "UPDATE [Tab_Brennstoff_Stamm] SET [PreisEinheit] = ? WHERE [ID] IN " + LISTE +
            " AND [PreisEinheit] IN (?, ?)";

        /// <summary>Teil 3: jede Preiszeile eines Gasträgers, die noch „m³" führt.</summary>
        public const string SQL_PREISZEILEN =
            "UPDATE [energy_price] SET [arbeitspreis_unit] = ? WHERE [arbeitspreis_unit] = ? " +
            "AND [carrier_id] IN (SELECT [id] FROM [energy_carrier] WHERE [ID_Brennstoff] IN " + LISTE + ")";

        /// <summary>Wie viele Stammzeilen und Preiszeilen tragen noch den alten Text?</summary>
        public const string SQL_OFFEN =
            "SELECT (SELECT COUNT(*) FROM [Tab_Brennstoff_Stamm] WHERE [ID] IN " + LISTE +
            " AND ([Einheit] = ? OR [PreisEinheit] IN (?, ?))) + " +
            "(SELECT COUNT(*) FROM [energy_price] WHERE [arbeitspreis_unit] = ? AND [carrier_id] IN " +
            "(SELECT [id] FROM [energy_carrier] WHERE [ID_Brennstoff] IN " + LISTE + "))";

        /// <summary>Was der Schritt getan hat.</summary>
        public sealed class Bericht
        {
            public int Einheiten;
            public int Preiseinheiten;
            public int Preiszeilen;

            /// <summary>Der Satz für das Protokoll.</summary>
            public string Text()
            {
                return Einheiten.ToString(CultureInfo.InvariantCulture) + " Einheit(en) m³ -> Nm³, " +
                       Preiseinheiten.ToString(CultureInfo.InvariantCulture) + " Preiseinheit(en) -> €/Nm³, " +
                       Preiszeilen.ToString(CultureInfo.InvariantCulture) + " Preiszeile(n) m³ -> Nm³";
            }
        }

        /// <summary>
        /// Führt die drei Anweisungen aus — wiederholbar: Auf einer nachgezogenen
        /// Datenbank trifft keine mehr.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            b.Einheiten = DataRepository.ExecuteNonQuery(SQL_EINHEIT,
                new DbParam("@neu", NEU), new DbParam("@alt", ALT));
            b.Preiseinheiten = DataRepository.ExecuteNonQuery(SQL_PREISEINHEIT,
                new DbParam("@neu", PREIS_NEU), new DbParam("@alt", PREIS_ALT),
                new DbParam("@althoch", PREIS_ALT_HOCH));
            b.Preiszeilen = DataRepository.ExecuteNonQuery(SQL_PREISZEILEN,
                new DbParam("@neu", NEU), new DbParam("@alt", ALT));
            return b;
        }

        /// <summary>Offene Zeilen (siehe <see cref="SQL_OFFEN"/>); 0 = gelaufen.</summary>
        public static int Offen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_OFFEN,
                    new DbParam("@alt", ALT), new DbParam("@palt", PREIS_ALT),
                    new DbParam("@palthoch", PREIS_ALT_HOCH), new DbParam("@zalt", ALT));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
    }
}
