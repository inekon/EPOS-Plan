using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Umrechnung der Klimadatenreihe von UTC auf ORTSZEIT (MEZ/MESZ) — Befund B1 des
    /// <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Paket A.
    ///
    /// <para><b>Der Befund.</b> <c>Tab_Solar</c> und <c>Tab_Solar_STAMM</c> führen je
    /// Klimaregion 8.760 Zeilen ohne Zeitspalte; der einzige Zeitbezug ist
    /// <c>ORDER BY ID</c>, also die Empfangsreihenfolge von PVGIS — und die ist
    /// <c>time(UTC)</c>. Lastgänge, Bedarfsprofile und die Spotpreisreihe laufen dagegen in
    /// Ortszeit. Erzeugung und Bedarf standen sich damit 1 h (Winter) bzw. 2 h (Sommer) zu
    /// früh gegenüber: Jahressummen richtig, Eigenverbrauchsquote, Autarkie,
    /// Speicherfahrweise und die § 51-Zuordnung zur Spotreihe systematisch verschoben.
    /// Die DB-Gegenprobe (Konzept Nachtrag 1) hat das bestätigt — die Maximumsstunde der
    /// Globalstrahlung folgt über 113° Längengrad dem LÄNGENGRAD, nicht der Ortszeit.</para>
    ///
    /// <para><b>Die Korrektur sitzt beim LESEN</b> (Entscheidung Q3), nicht beim Import:
    /// Die Bestandsregionen bleiben damit unverändert gültig, und
    /// <c>Sol_*</c>/<c>Sonnenwinkel</c>/<c>Temperatur</c> sind ohnehin auf DERSELBEN
    /// UTC-Stunde gerechnet — verschoben werden deshalb ganze Zeilen, nie einzelne
    /// Spalten.</para>
    ///
    /// <para><b>Keine <see cref="TimeZoneInfo"/>.</b> Die EU-Regel ist hier fest
    /// verdrahtet — dasselbe Vorgehen und derselbe Grund wie in
    /// <c>SpeicherEngine.GanglinienPruefung</c>: Das Ergebnis eines Laufs darf nicht von
    /// der Zeitzonentabelle des Rechners abhängen.</para>
    ///
    /// <para><b>Ohne Datenbank testbar.</b> Diese Klasse kennt weder
    /// <c>DataRepository</c> noch <c>SimulationProtokoll</c>; sie rechnet ausschließlich
    /// auf Indizes. Das Lesen und Umsortieren steht in
    /// <c>SolardatenCtrl.ReadOrtszeit</c>.</para>
    /// </summary>
    public static class SolarZeitbasis
    {
        /// <summary>Das feste Jahresraster des Hauses: 8.760 Stunden, 365 Tage.</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>Tage im Jahresraster (kein Schaltjahr — Wurzel-CLAUDE.md).</summary>
        public const int TAGE_JAHR = 365;

        /// <summary>Versatz Ortszeit − UTC in der Normalzeit (MEZ) [h].</summary>
        public const int OFFSET_MEZ = 1;

        /// <summary>Versatz Ortszeit − UTC in der Sommerzeit (MESZ) [h].</summary>
        public const int OFFSET_MESZ = 2;

        /// <summary>
        /// Tag im Jahresraster (1…365), an dem die Sommerzeit BEGINNT: der letzte Sonntag
        /// im März des Referenzjahres, umgerechnet auf den 365-Tage-Kalender.
        /// </summary>
        public static int TagSommerzeitBeginn(int referenzjahr)
        {
            return TagImRaster(LetzterSonntag(referenzjahr, 3));
        }

        /// <summary>
        /// Tag im Jahresraster (1…365), an dem die Sommerzeit ENDET: der letzte Sonntag im
        /// Oktober des Referenzjahres, umgerechnet auf den 365-Tage-Kalender.
        /// </summary>
        public static int TagSommerzeitEnde(int referenzjahr)
        {
            return TagImRaster(LetzterSonntag(referenzjahr, 10));
        }

        /// <summary>
        /// true, wenn der Ortszeit-Index <paramref name="ortszeitIndex"/> in die
        /// Sommerzeit fällt.
        ///
        /// <para>EU-Regel seit 1996: von 02:00 Ortszeit des letzten Märzsonntags
        /// (= 01:00 UTC) bis 03:00 Ortszeit des letzten Oktobersonntags (= 01:00 UTC).
        /// Gemessen wird an der GLATTEN Ortszeitachse des 8.760er-Rasters — Tag
        /// <c>L/24 + 1</c>, Stunde <c>L%24</c> —, nicht an einem Kalenderobjekt.</para>
        /// </summary>
        public static bool IstSommerzeit(int ortszeitIndex, int referenzjahr)
        {
            if (ortszeitIndex < 0 || ortszeitIndex >= STUNDEN_JAHR) return false;

            int tag = ortszeitIndex / 24 + 1;      // 1…365
            int stunde = ortszeitIndex % 24;       // 0…23

            int beginnTag = TagSommerzeitBeginn(referenzjahr);
            int endeTag = TagSommerzeitEnde(referenzjahr);

            bool nachBeginn = tag > beginnTag || (tag == beginnTag && stunde >= 2);
            bool vorEnde = tag < endeTag || (tag == endeTag && stunde < 3);

            return nachBeginn && vorEnde;
        }

        /// <summary>
        /// Der Versatz für diesen Ortszeit-Index: 1 (MEZ) oder 2 (MESZ) Stunden.
        /// </summary>
        public static int Offset(int ortszeitIndex, int referenzjahr)
        {
            return IstSommerzeit(ortszeitIndex, referenzjahr) ? OFFSET_MESZ : OFFSET_MEZ;
        }

        /// <summary>
        /// Der UTC-Index (0…8759), dessen Zeile an der Ortszeit-Position
        /// <paramref name="ortszeitIndex"/> steht: <c>U = L − Offset(L)</c>, mit
        /// Jahresumlauf für <c>L &lt; Offset(L)</c>.
        ///
        /// <para><b>Die beiden Umstellstunden — bewusst so und hier dokumentiert.</b>
        /// Das Raster hat feste 8.760 Fächer und eine GLATTE Ortszeitachse ohne
        /// Zeitsprung (genau wie die Ganglinienablage, <c>GanglinienPruefung</c>). Daraus
        /// folgt zwangsläufig:</para>
        /// <list type="bullet">
        ///   <item><description><b>Frühjahr:</b> Die Ortsstunde 02:00 des Umstelltags gibt
        ///     es in Wirklichkeit nicht. Sie bekommt dieselbe UTC-Stunde wie 01:00 — EINE
        ///     UTC-Stunde wird also doppelt gelesen.</description></item>
        ///   <item><description><b>Herbst:</b> Die Ortsstunde 02:00 gibt es zweimal, das
        ///     Raster führt sie einmal. EINE UTC-Stunde entfällt
        ///     dadurch.</description></item>
        /// </list>
        /// <para>Beide betroffenen Stunden liegen um 00:00/01:00 UTC Ende März bzw. Ende
        /// Oktober — Nachtstunden. Einstrahlung und Sonnenwinkel sind dort null; die
        /// Jahressumme der Globalstrahlung bleibt deshalb unverändert (im Prüfharness
        /// nachgerechnet). Die Außentemperatur verschiebt sich um zwei Stundenwerte, was
        /// gegenüber der 1-2-Stunden-Korrektur selbst nicht ins Gewicht fällt.</para>
        /// </summary>
        public static int UtcIndex(int ortszeitIndex, int referenzjahr)
        {
            int u = ortszeitIndex - Offset(ortszeitIndex, referenzjahr);
            if (u < 0) u += STUNDEN_JAHR;          // Jahresumlauf: 01.01. 00:00 MEZ = 31.12. 23:00 UTC
            return u;
        }

        /// <summary>
        /// Die vollständige Zuordnung Ortszeit → UTC als Feld der Länge 8.760:
        /// <c>zuordnung[L] = U</c>. Eine Wahrheit für Leser und Prüfharness.
        /// </summary>
        public static int[] Zuordnung(int referenzjahr)
        {
            int[] z = new int[STUNDEN_JAHR];
            for (int l = 0; l < STUNDEN_JAHR; l++) z[l] = UtcIndex(l, referenzjahr);
            return z;
        }

        /// <summary>
        /// Klartext der Umstelltage für das Simulationsprotokoll, z. B.
        /// „30.03./26.10." — bewusst ohne Jahr, das nennt der Aufrufer daneben.
        /// </summary>
        public static string UmstelltageText(int referenzjahr)
        {
            DateTime fruehjahr = LetzterSonntag(referenzjahr, 3);
            DateTime herbst = LetzterSonntag(referenzjahr, 10);
            return fruehjahr.ToString("dd.MM.", CultureInfo.InvariantCulture) + "/" +
                   herbst.ToString("dd.MM.", CultureInfo.InvariantCulture);
        }

        // =================================================================================
        // Intern
        // =================================================================================

        /// <summary>Der letzte Sonntag des Monats <paramref name="monat"/>.</summary>
        private static DateTime LetzterSonntag(int jahr, int monat)
        {
            if (jahr < 1) jahr = DbWerte.SOLAR_REFERENZJAHR_STANDARD;

            DateTime d = new DateTime(jahr, monat, DateTime.DaysInMonth(jahr, monat));
            while (d.DayOfWeek != DayOfWeek.Sunday) d = d.AddDays(-1);
            return d;
        }

        /// <summary>
        /// Tag im 365-Tage-Raster (1…365) zu einem Kalenderdatum. <b>Über Monat und Tag,
        /// NICHT über <c>DayOfYear</c>:</b> In einem Schaltjahr läge jedes Datum nach dem
        /// 29.02. sonst um einen Tag daneben, und das Raster des Hauses kennt keinen
        /// 29.02. (Wurzel-CLAUDE.md).
        /// </summary>
        private static int TagImRaster(DateTime d)
        {
            int tag = new DateTime(2001, d.Month, d.Day).DayOfYear;   // 2001 = Nicht-Schaltjahr
            if (tag < 1) tag = 1;
            if (tag > TAGE_JAHR) tag = TAGE_JAHR;
            return tag;
        }
    }
}
