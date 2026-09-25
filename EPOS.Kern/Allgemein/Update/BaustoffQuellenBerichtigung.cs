using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE HERKUNFT DER ROHDICHTE IN DER QUELLE ZWEIER SAATZEILEN - Migrationsschritt
    // BaustoffQuellenBerichtigung.SCHRITT (Gebaeudesimulation G3, Nachweis zu N1.44 im
    // Konzept Gebaeudesimulation). Die Regel stammt aus Entscheid E39: Stammt die Rohdichte
    // einer Herstellerzeile aus einer Umweltproduktdeklaration, dann nennt die Quelle das.
    //
    // ANLASS. Zwei Herstellerzeilen der Saat (BaustoffSaat.cs) nannten in der Quelle allein
    // das Datenblatt, obwohl ihre Rohdichte aus einer Umweltproduktdeklaration stammt:
    //   1041 Kingspan Kooltherm K5 - die Rohdichte steht in der FDES (franzoesische
    //        Umweltdeklaration) fuer 120 mm; das deutsche Produktblatt nennt keine Zahl.
    //   1066 Baumit DaemmPutz DP 85 - die Rohdichte ist die Mindest-Trockenrohdichte, die die
    //        VDPM-EPD fuer die Brandklasse A2-s1,d0 des Datenblatts verlangt; das Datenblatt
    //        selbst nennt keine.
    // Die Saat traegt die berichtigten Texte. BaustoffSchema.SaatSchreiben ueberschreibt aber
    // nie, und der Schritt des Baustoffkatalogs ist in bestehenden Datenbanken gelaufen -
    // dieser Schritt zieht die zwei Texte dort nach.
    //
    // NUR DER WORTGLEICHE ALTE TEXT. Eine Zeile wird allein berichtigt, wenn ihre Quelle
    // Zeichen fuer Zeichen dem alten Saattext entspricht; eine vom Anwender geaenderte Quelle
    // bleibt stehen.
    //   Tab_Baustoff_STAMM - die Zeile unter der festen Saat-Id.
    //   Tab_Baustoff       - jede Projektkopie der Zeile. Die Kopie fuehrt keine Id des
    //                        Katalogs; sie haengt an ihm ueber den natuerlichen Schluessel der
    //                        Saat, Hersteller und Bezeichner (BaustoffCtrl.CopyFromStamm).
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Quelle; Lambda, Rho und cp bleiben, wie sie
    // sind. Der Referenzlauf bleibt byte-gleich.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt keine Zeile mehr den alten Text; ein zweiter Lauf tut
    // nichts. Die Nachprobe (Offen) fragt dieselben Bedingungen. Ohne die Tabellen des
    // Baustoffkatalogs gibt es nichts zu berichtigen.
    // ====================================================================================

    /// <summary>
    /// <b>Die Quelle zweier Herstellerzeilen des Baustoffkatalogs nennt die Herkunft der
    /// Rohdichte</b> (Schemaschritt <see cref="SCHRITT"/>): 1041 und 1066, deren Rohdichte aus
    /// einer Umweltproduktdeklaration stammt — EINE Quelle für Migration, Werkzeug
    /// <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests und Nachweis. Anlass und Bauform
    /// stehen im Kopf der Datei.
    /// </summary>
    public static class BaustoffQuellenBerichtigung
    {
        /// <summary>
        /// Die Nummer des Schemaschritts — die EINZIGE Stelle im Code, an der sie als Zahl
        /// steht (Migration, Werkzeug und Protokoll lesen sie von hier).
        /// </summary>
        public const int SCHRITT = 143;

        /// <summary>Der alte Saattext der Zeile 1041 (Kingspan Kooltherm K5), wie ihn Schritt 132 gesät hat.</summary>
        public const string ALT_1041 = "Kingspan, Produktblatt Kooltherm K5 WDVS-Dämmplatte (DE), Version 15, 07/2026";

        /// <summary>Der alte Saattext der Zeile 1066 (Baumit DämmPutz DP 85), wie ihn Schritt 132 gesät hat.</summary>
        public const string ALT_1066 = "Baumit, Produktdatenblatt DämmPutz DP 85, 18.09.2025";

        /// <summary>
        /// Die zwei Berichtigungen — Id, natürlicher Schlüssel und neuer Text kommen aus der Saat
        /// (<see cref="BaustoffSchema.SaatZu"/>), der alte Text steht hier.
        /// </summary>
        public static readonly IReadOnlyList<Zeile> Berichtigungen = new[]
        {
            Aus(1041, ALT_1041),
            Aus(1066, ALT_1066),
        };

        // =================================================================
        //  Die Anweisungen - fest, mit ?-Parametern
        // =================================================================

        /// <summary>Berichtigung im Katalog. Parameter: neu, Id, alt.</summary>
        public const string SQL_STAMM =
            "UPDATE \"" + SchemaKatalog.TAB_BAUSTOFF_STAMM + "\" SET \"Quelle\" = ? WHERE \"ID\" = ? AND \"Quelle\" = ?";

        /// <summary>Zählung im Katalog. Parameter: Id, alt.</summary>
        public const string SQL_STAMM_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_BAUSTOFF_STAMM + "\" WHERE \"ID\" = ? AND \"Quelle\" = ?";

        /// <summary>Berichtigung in den Projektkopien. Parameter: neu, Hersteller, Bezeichner, alt.</summary>
        public const string SQL_PROJEKT =
            "UPDATE \"" + SchemaKatalog.TAB_BAUSTOFF + "\" SET \"Quelle\" = ? " +
            "WHERE \"Hersteller\" = ? AND \"Bezeichner\" = ? AND \"Quelle\" = ?";

        /// <summary>Zählung in den Projektkopien. Parameter: Hersteller, Bezeichner, alt.</summary>
        public const string SQL_PROJEKT_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_BAUSTOFF + "\" " +
            "WHERE \"Hersteller\" = ? AND \"Bezeichner\" = ? AND \"Quelle\" = ?";

        // =================================================================
        //  Der Schritt
        // =================================================================

        /// <summary>Was ein Lauf von <see cref="Ausfuehren"/> getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Berichtigungen, je Eintrag „Tabelle Id oder Kopien: Quelle berichtigt".</summary>
            public List<string> Berichtigt { get; } = new List<string>();

            /// <summary>Zahl der berichtigten Zeilen im Katalog.</summary>
            public int Katalog { get; internal set; }

            /// <summary>Zahl der berichtigten Projektkopien.</summary>
            public int Kopien { get; internal set; }

            /// <summary>Eine Zeile für Protokoll und Konsole.</summary>
            public string Text()
            {
                return "berichtigt " + Katalog.ToString(CultureInfo.InvariantCulture) + " Katalogzeile(n) und " +
                       Kopien.ToString(CultureInfo.InvariantCulture) + " Projektkopie(n)" +
                       (Berichtigt.Count > 0 ? " (" + string.Join("; ", Berichtigt) + ")" : "");
            }
        }

        /// <summary>
        /// Der ganze Schritt: je Berichtigung erst die Katalogzeile, dann die Projektkopien —
        /// jeweils nur mit dem wortgleichen alten Text. <b>Wiederholbar.</b> Fehler werfen, der
        /// Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            if (!BaustoffSchema.TabellenVorhanden()) return b;

            foreach (Zeile z in Berichtigungen)
            {
                int n = DataRepository.ExecuteNonQuery(SQL_STAMM, P("@neu", z.Neu), P("@id", z.Id), P("@alt", z.Alt));
                if (n > 0)
                {
                    b.Katalog += n;
                    b.Berichtigt.Add(SchemaKatalog.TAB_BAUSTOFF_STAMM + " " + z.Id.ToString(CultureInfo.InvariantCulture));
                }

                int k = DataRepository.ExecuteNonQuery(SQL_PROJEKT, P("@neu", z.Neu), P("@her", z.Hersteller),
                                                       P("@bez", z.Bezeichner), P("@alt", z.Alt));
                if (k > 0)
                {
                    b.Kopien += k;
                    b.Berichtigt.Add(SchemaKatalog.TAB_BAUSTOFF + " " + k.ToString(CultureInfo.InvariantCulture) +
                                     " Kopie(n) von " + z.Id.ToString(CultureInfo.InvariantCulture));
                }
            }
            return b;
        }

        /// <summary>
        /// Wie viele Zeilen (Katalog und Projektkopien) tragen noch den alten Text? 0 = der
        /// Schritt ist gelaufen (die Nachprobe der Migration).
        /// </summary>
        public static long Offen()
        {
            if (!BaustoffSchema.TabellenVorhanden()) return 0;
            long offen = 0;
            foreach (Zeile z in Berichtigungen)
            {
                offen += Zahl(SQL_STAMM_ZAEHLUNG, P("@id", z.Id), P("@alt", z.Alt));
                offen += Zahl(SQL_PROJEKT_ZAEHLUNG, P("@her", z.Hersteller), P("@bez", z.Bezeichner), P("@alt", z.Alt));
            }
            return offen;
        }

        // =================================================================
        //  intern
        // =================================================================

        /// <summary>Eine Berichtigung: Saat-Id, natürlicher Schlüssel, alter und neuer Text.</summary>
        public sealed class Zeile
        {
            internal Zeile(int id, string hersteller, string bezeichner, string alt, string neu)
            {
                Id = id;
                Hersteller = hersteller;
                Bezeichner = bezeichner;
                Alt = alt;
                Neu = neu;
            }

            /// <summary>Die feste Saat-Id im Katalog.</summary>
            public int Id { get; }

            /// <summary>Hersteller der Saatzeile — mit dem Bezeichner der Schlüssel der Projektkopie.</summary>
            public string Hersteller { get; }

            /// <summary>Bezeichner der Saatzeile.</summary>
            public string Bezeichner { get; }

            /// <summary>Der alte Text, wortgleich.</summary>
            public string Alt { get; }

            /// <summary>Der neue Text — der der Saat.</summary>
            public string Neu { get; }
        }

        private static Zeile Aus(int id, string alt)
        {
            BaustoffSaat s = BaustoffSchema.SaatZu(id)
                             ?? throw new InvalidOperationException("Keine Saatzeile " + id.ToString(CultureInfo.InvariantCulture) + ".");
            return new Zeile(id, s.Hersteller, s.Bezeichner, alt, s.Quelle);
        }

        private static DbParam P(string name, object wert) => new DbParam(name, wert);

        private static long Zahl(string sql, params DbParam[] parameter)
        {
            object wert = DataRepository.ExecuteScalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return 0;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }
}
