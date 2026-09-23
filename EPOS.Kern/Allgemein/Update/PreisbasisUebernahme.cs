using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Schemaschritt 108 — die Preisbasis der Trägerkarte als eigener Kartenzustand</b>
    /// (Schritt F des Analysepapiers Wirtschaftlichkeit § 6, Entscheid ET‑D‑3 Rest,
    /// Mockup U32).
    ///
    /// <para><b>Der Befund U32.</b> Die Karte merkte sich die gewählte Preisbasis bis
    /// hierher allein über <c>energy_project_settings.ID_Umrechnung</c> — eine
    /// REGELkennung. Führte der Brennstoff keine Regel nach kWh, wurde beim Speichern −1
    /// (NULL) abgelegt, und die gewählte Preisbasis „kWh" fiel beim nächsten Öffnen still
    /// auf die Abrechnungseinheit zurück. Seit Schritt 108 steht der Kartenzustand in
    /// einer eigenen Spalte <c>Preisbasis</c> (TEXT, nullbar): der Einheitentext der
    /// gewählten Basis. <c>ID_Umrechnung</c> bleibt, was es ist — die Regel der
    /// Einheitenprüfung.</para>
    ///
    /// <para><b>Der Datenteil (einmalig, wiederholbar):</b> Jede Zeile ohne Preisbasis
    /// bekommt sie aus ihrer <c>ID_Umrechnung</c> — zeigt die Regel nach kWh, ist die
    /// Basis „kWh", sonst die Abrechnungseinheit des Trägers
    /// (<c>energy_carrier.billing_unit</c>). Das ist genau die Basis, die die Karte bis
    /// hierher beim Öffnen gezeigt hat; es ändert sich also kein Kartenbild. Eine Zeile,
    /// deren Träger keine Abrechnungseinheit führt, bleibt NULL (= Abrechnungseinheit).
    /// Gesetzt wird nur, wo die Spalte leer ist; der zweite Lauf fasst nichts an.</para>
    ///
    /// <para><b>Ergebnisneutral:</b> Die Preisbasis ist eine Eingabehilfe, keine
    /// Speichereinheit — gespeichert und gerechnet wird der Basiswert je
    /// Abrechnungseinheit. Kein Rechenweg liest die Spalte; der Referenzlauf bleibt
    /// byte-gleich.</para>
    /// </summary>
    public static class PreisbasisUebernahme
    {
        /// <summary>Die Tabelle der Projektübersteuerung.</summary>
        public const string TABELLE = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Die Spalte des Kartenzustands.</summary>
        public const string SPALTE = SchemaKatalog.SPALTE_EPS_PREISBASIS;

        /// <summary>Der Einheitentext der Kilowattstunde, wie ihn die Karte führt.</summary>
        public const string KWH = DbWerte.EINHEIT_KWH;

        /// <summary>Teil 1: Zeigt <c>ID_Umrechnung</c> auf eine Regel nach kWh, ist die
        /// Basis „kWh". Verglichen wird wie in der Karte ohne Groß-/Kleinschreibung und
        /// ohne Randleerzeichen.</summary>
        public const string SQL_KWH =
            "UPDATE [" + TABELLE + "] SET [" + SPALTE + "] = ? WHERE [" + SPALTE + "] IS NULL " +
            "AND [ID_Umrechnung] IN (SELECT [ID] FROM [energy_conversion] WHERE upper(trim([to_unit])) = ?)";

        /// <summary>Teil 2: alle übrigen Zeilen — die Abrechnungseinheit ihres Trägers;
        /// ohne Abrechnungseinheit bleibt die Zeile NULL.</summary>
        public const string SQL_ABRECHNUNGSEINHEIT =
            "UPDATE [" + TABELLE + "] SET [" + SPALTE + "] = " +
            "(SELECT NULLIF(trim(k.[billing_unit]), '') FROM [energy_carrier] AS k " +
            "WHERE k.[id] = [" + TABELLE + "].[ID_Energieträger]) " +
            "WHERE [" + SPALTE + "] IS NULL";

        /// <summary>Wie viele Zeilen tragen noch keine Preisbasis, obwohl ihr Träger eine
        /// Abrechnungseinheit führt? 0 = der Schritt ist gelaufen.</summary>
        public const string SQL_OFFEN =
            "SELECT COUNT(*) FROM [" + TABELLE + "] AS s INNER JOIN [energy_carrier] AS k " +
            "ON k.[id] = s.[ID_Energieträger] WHERE s.[" + SPALTE + "] IS NULL " +
            "AND NULLIF(trim(k.[billing_unit]), '') IS NOT NULL";

        /// <summary>Was der Datenteil getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zeilen, die „kWh" bekamen (Regel nach kWh).</summary>
            public int Kwh;

            /// <summary>Zeilen, die die Abrechnungseinheit ihres Trägers bekamen.</summary>
            public int Abrechnungseinheit;

            /// <summary>Der Satz für das Protokoll.</summary>
            public string Text()
            {
                return Kwh.ToString(CultureInfo.InvariantCulture) + " Zeile(n) auf kWh (Regel nach kWh), " +
                       Abrechnungseinheit.ToString(CultureInfo.InvariantCulture) +
                       " Zeile(n) auf die Abrechnungseinheit gesetzt";
            }
        }

        /// <summary>Steht die Spalte? Ohne sie tut der Datenteil nichts.</summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>
        /// Führt den Datenteil aus — erst die Regel nach kWh, dann die Abrechnungseinheit,
        /// beide nur auf leeren Zeilen. Wiederholbar: Auf einer nachgezogenen Datenbank
        /// trifft er nichts mehr.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            if (!Vorhanden()) return b;
            b.Kwh = DataRepository.ExecuteNonQuery(SQL_KWH,
                new DbParam("@kwh", KWH),
                new DbParam("@schluessel", KWH.ToUpperInvariant()));
            b.Abrechnungseinheit = DataRepository.ExecuteNonQuery(SQL_ABRECHNUNGSEINHEIT);
            return b;
        }

        /// <summary>Wie viele Zeilen sind noch offen (siehe <see cref="SQL_OFFEN"/>)?</summary>
        public static int Offen()
        {
            if (!Vorhanden()) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_OFFEN);
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
    }
}
