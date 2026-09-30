using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die fünf Kennlinienfelder des Heizkessels an der Datenbankgrenze</b> — Lesen, Schreiben
    /// und Prüfen an EINER Stelle für Katalog (<see cref="HeizkesselStammCtrl"/>) und Projektkopie
    /// (<see cref="HeizkesselCtrl"/>). Konzept Kesselkennlinie 3.1 und 3.4, Etappe E1.
    /// </summary>
    /// <remarks>
    /// <para><b>Leer ist nicht 0.</b> Die vier Zahlenfelder sind nullbar; <c>null</c> heißt „nicht
    /// gepflegt", und was dann gilt, legen die Etappen E2 bis E4 fest (Normvorgaben, Entscheid F1
    /// vom 29.09.2026, Nachtrag im Konzept). Deshalb schreibt diese Klasse <c>DBNull</c> und nie
    /// eine 0 an die Stelle eines leeren Feldes.</para>
    /// <para><b>Der Schalter der Brennwertkennlinie gilt nur beim Brennwertkessel</b> (Konzept
    /// 3.1): <see cref="SchalterZumSchreiben"/> schreibt ihn nie ohne <c>Brennwert</c> = 1, und
    /// <see cref="Verstoss"/> lehnt die Kombination in den Katalogwegen benannt ab. Grund: Ein
    /// Anwenderkatalog mit gesetztem <c>Brennwert</c> soll nicht still anders rechnen.</para>
    /// <para><b>In E1 liest kein Rechenweg die Felder.</b></para>
    /// </remarks>
    public static class KesselKennlinieWerte
    {
        /// <summary>
        /// Die Prozentschwelle: Ein Wirkungsgrad über 1,5 gilt als Prozentangabe und wird durch 100
        /// geteilt — dieselbe Schwelle wie beim Nennwirkungsgrad in <c>SimulationSPK</c>.
        /// Heizwertbezogene Brennwertwerte liegen unter Hs/Hi (höchstens rund 1,11).
        /// </summary>
        public const double PROZENTSCHWELLE = 1.5;

        /// <summary>Kleinster plausibler Wirkungsgrad bei 30 % Last (Faktor, Hi).</summary>
        public const double ETA30_MIN = 0.5;

        /// <summary>
        /// Größter plausibler Wirkungsgrad bei 30 % Last (Faktor, Hi) — über dem Verhältnis
        /// Hs/Hi jedes Brennstoffs des Katalogs (Erdgas rund 1,11).
        /// </summary>
        public const double ETA30_MAX = 1.2;

        /// <summary>Kürzeste zulässige Mindestlaufzeit [min].</summary>
        public const int LAUFZEIT_MIN = 1;

        /// <summary>Längste zulässige Mindestlaufzeit [min] — eine Stunde, das Raster des Laufs.</summary>
        public const int LAUFZEIT_MAX = 60;

        /// <summary>
        /// Der Wirkungsgrad als Faktor: über <see cref="PROZENTSCHWELLE"/> durch 100 geteilt,
        /// sonst unverändert; <c>null</c> bleibt <c>null</c>.
        /// </summary>
        public static double? AlsFaktor(double? wert)
        {
            if (wert == null) return null;
            return wert.Value > PROZENTSCHWELLE ? wert.Value / 100.0 : wert.Value;
        }

        /// <summary>
        /// Liest die fünf Felder aus einer Zeile von <c>Tab_Heizkessel_STAMM</c> oder
        /// <c>Tab_Heizkessel</c>. Eine Spalte, die die Zeile nicht führt (Datenbank vor
        /// <see cref="KesselKennlinieSchema.SCHRITT"/>), zählt als leer bzw. 0.
        /// </summary>
        public static void AusZeile(HeizkesselModel ziel, DataRow zeile)
        {
            if (ziel == null || zeile == null) return;
            ziel.Wirkungsgrad_Teillast30 = Zahl(zeile, KesselKennlinieSchema.SPALTE_TEILLAST30);
            ziel.Mindestleistung = Zahl(zeile, KesselKennlinieSchema.SPALTE_MINDESTLEISTUNG);
            ziel.Anfahrverlust_kWh = Zahl(zeile, KesselKennlinieSchema.SPALTE_ANFAHRVERLUST);
            double? laufzeit = Zahl(zeile, KesselKennlinieSchema.SPALTE_MINDESTLAUFZEIT);
            ziel.Mindestlaufzeit_min = laufzeit.HasValue ? (int?)Convert.ToInt32(laufzeit.Value) : null;
            double? schalter = Zahl(zeile, KesselKennlinieSchema.SPALTE_KENNLINIE_BRENNWERT);
            ziel.Kennlinie_Brennwert = schalter.HasValue && Math.Abs(schalter.Value) > 0.5;
        }

        /// <summary>Überträgt die fünf Felder von einem Modell auf ein anderes.</summary>
        public static void Uebertragen(HeizkesselModel von, HeizkesselModel nach)
        {
            if (von == null || nach == null) return;
            nach.Wirkungsgrad_Teillast30 = von.Wirkungsgrad_Teillast30;
            nach.Kennlinie_Brennwert = von.Kennlinie_Brennwert;
            nach.Mindestleistung = von.Mindestleistung;
            nach.Anfahrverlust_kWh = von.Anfahrverlust_kWh;
            nach.Mindestlaufzeit_min = von.Mindestlaufzeit_min;
        }

        /// <summary>
        /// Der Schalter, wie er in die Datenbank geht: nur mit <c>Brennwert</c> gesetzt
        /// (Konzept 3.1). Als 0/1 (Boolean-Regel BETRIEB_SQLITE.md § 6).
        /// </summary>
        public static int SchalterZumSchreiben(HeizkesselModel m)
            => m != null && m.Kennlinie_Brennwert && m.Brennwert ? 1 : 0;

        /// <summary>Ein nullbarer Wert als Parameterwert: <c>null</c> wird <c>DBNull</c>.</summary>
        public static object Wert(double? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        /// <summary>Ein nullbarer Wert als Parameterwert: <c>null</c> wird <c>DBNull</c>.</summary>
        public static object Wert(int? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        /// <summary>
        /// Die fünf Parameter in der Spaltenreihenfolge von <see cref="KesselKennlinieSchema.SPALTEN"/>:
        /// η₃₀, Schalter, Mindestleistung, Anfahrverlust, Mindestlaufzeit.
        /// </summary>
        public static DbParam[] Parameter(HeizkesselModel m)
        {
            return new[]
            {
                new DbParam("@eta30", Wert(m.Wirkungsgrad_Teillast30)),
                new DbParam("@kbw", SchalterZumSchreiben(m)),
                new DbParam("@pmin", Wert(m.Mindestleistung)),
                new DbParam("@anf", Wert(m.Anfahrverlust_kWh)),
                new DbParam("@lauf", Wert(m.Mindestlaufzeit_min))
            };
        }

        /// <summary>
        /// Die Plausibilität der fünf Felder — <c>null</c>, wenn alles passt, sonst der erste
        /// Grund im Klartext, mit dem Feldnamen, wie er auf dem Schirm steht.
        /// </summary>
        /// <remarks>
        /// η₃₀ (als Faktor gelesen) zwischen <see cref="ETA30_MIN"/> und <see cref="ETA30_MAX"/>;
        /// Mindestleistung nicht negativ und höchstens die thermische Leistung (wenn diese
        /// gepflegt ist); Anfahrverlust nicht negativ; Mindestlaufzeit zwischen
        /// <see cref="LAUFZEIT_MIN"/> und <see cref="LAUFZEIT_MAX"/> Minuten; der Schalter nur
        /// beim Brennwertkessel. Ein leeres Feld ist immer zulässig.
        /// </remarks>
        public static string Verstoss(HeizkesselModel m)
        {
            if (m == null) return null;
            const KatalogBrowserArt art = KatalogBrowserArt.Heizkessel;

            double? eta30 = AlsFaktor(m.Wirkungsgrad_Teillast30);
            string grund = KatalogFeldPruefung.ErsterGrund(
                eta30.HasValue
                    ? KatalogFeldPruefung.ImBereich(art, KatalogBrowserProfil.FeldTeillast30,
                                                    eta30.Value, ETA30_MIN, ETA30_MAX)
                    : null,
                m.Mindestleistung.HasValue
                    ? (m.Ptherm > 0
                        ? KatalogFeldPruefung.ImBereich(art, KatalogBrowserProfil.FeldMindestleistung,
                                                        m.Mindestleistung.Value, 0, m.Ptherm)
                        : KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldMindestleistung,
                                                           m.Mindestleistung.Value))
                    : null,
                m.Anfahrverlust_kWh.HasValue
                    ? KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldAnfahrverlust,
                                                       m.Anfahrverlust_kWh.Value)
                    : null,
                m.Mindestlaufzeit_min.HasValue
                    ? KatalogFeldPruefung.ImBereich(art, KatalogBrowserProfil.FeldMindestlaufzeit,
                                                    m.Mindestlaufzeit_min.Value, LAUFZEIT_MIN, LAUFZEIT_MAX)
                    : null);
            if (!string.IsNullOrEmpty(grund)) return grund;

            if (m.Kennlinie_Brennwert && !m.Brennwert)
                return Text("HZKK_MSG_KENNLINIE_OHNE_BRENNWERT",
                            "Die Brennwertkennlinie ist nur bei einem Brennwertkessel zulässig.");

            return null;
        }

        private static double? Zahl(DataRow zeile, string spalte)
        {
            if (!zeile.Table.Columns.Contains(spalte)) return null;
            object v = zeile[spalte];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
