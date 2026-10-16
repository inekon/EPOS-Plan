using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Normkalender einer Kältevorlage mit Nutzungstagen nach Norm</b> (Konzept Kältebedarfsprofile 6.2, 8;
    /// Entscheid des Anwenders vom 16.10.2026; Welle KBP-5b). Wer eine Vorlage mit <c>Nutzungstage_Norm</c> übernimmt
    /// (Vorlage 8 „Prozesskälte einschichtig“: 230 Tage nach DIN/TS 18599-10:2025-10, Hallenprofile), bekommt im Projekt
    /// den Betriebskalender „Norm 230 Tage (Land)“ vorgewählt.
    ///
    /// <para><b>Bildung:</b> Feiertage wie Sonntag; Betriebsferien in zwei der vier Ferienzeiträume — ab dem 24.12. über
    /// den Jahreswechsel bis höchstens zum 6.1. und ab dem ersten Montag ab dem 1.8. —, Tag für Tag so bemessen, dass im
    /// Gemeinjahr-Raster des Projekts genau die Normtage als Nutzungstage bleiben (Werktage Montag bis Freitag ohne
    /// Feiertage und ohne Ferien). Feiertagsland aus dem ersten Gebäude des Projekts mit Land, sonst
    /// <see cref="LAND_VORGABE"/>.</para>
    ///
    /// <para><b>Ferientage wie Sonntag:</b> Die Kalenderschicht (<see cref="Betriebskalenderschicht"/>) legt einen
    /// Ferientag auf <c>f · m_s</c> (Tagesmittel des Wochenprofils), nicht auf den Sonntag. Der Ferienfaktor wird
    /// deshalb so gewählt, dass ein Ferientag die Tagesmenge des Sonntags trägt:
    /// <c>f = Σ Sonntag / Σ m_s</c> (<see cref="Ferienfaktor"/>, auf zwei Stellen). „Ferien kürzen“ bleibt aus —
    /// die Monatsmengen und damit die Jahressumme bleiben erhalten.</para>
    ///
    /// <para><b>Name und Wiederverwendung</b> (KBP-5c, Entscheid 16.10.2026): Die Kalender sind projektübergreifend
    /// sichtbar, deshalb trägt der Name das Feiertagsland — „Norm 230 Tage (BY)“, ohne Land „Norm 230 Tage
    /// (bundesweit)“. Genommen wird ein Kalender dieses Namens, den eine Zuordnung des Projekts schon nutzt, sonst einer
    /// dieses Namens mit gleichem Inhalt; erst dann entsteht ein neuer. Steht der Name schon mit anderem Inhalt
    /// (anderes Wochentagsraster der Klimaregion), heißt der neue „Norm 230 Tage (BY, 1.1. Do)“ mit dem Wochentag des
    /// 1. Januar, als letzter Ausweg mit laufender Nummer — nie entsteht ein Doppelname (<see cref="Namensfolge"/>,
    /// <see cref="Wahl"/>).</para>
    /// </summary>
    public static class NormkalenderCtrl
    {
        /// <summary>Das Feiertagsland, wenn kein Gebäude des Projekts eines führt: <c>null</c> = die neun bundeseinheitlichen Feiertage.</summary>
        public const string LAND_VORGABE = null;

        /// <summary>Beginn der Winterferien: Jahrestag 358 = 24. Dezember.</summary>
        public const int WINTER_BEGINN = 358;

        /// <summary>Spätestes Ende der Winterferien im neuen Jahr: Jahrestag 6 = 6. Januar.</summary>
        public const int WINTER_ENDE_SPAETESTENS = 6;

        /// <summary>Frühester Beginn der Sommerferien: Jahrestag 213 = 1. August (dann der nächste Montag).</summary>
        public const int SOMMER_BEGINN = 213;

        private const string TAB_KAELTE = "Z_Projekt_Kaeltebedarf";

        /// <summary>Höchste laufende Nummer eines Ausweichnamens („… #2“ bis „… #99“).</summary>
        private const int NUMMER_HOECHSTENS = 99;

        /// <summary>
        /// Der Name des Normkalenders aus der Zahl der Normtage und dem Feiertagsland (KBP-5c, Entscheid 16.10.2026):
        /// „Norm 230 Tage (BY)“ mit dem Länderkürzel, ohne bekanntes Land „Norm 230 Tage (bundesweit)“. Die Kalender
        /// sind projektübergreifend sichtbar — das Land im Namen hält Kalender verschiedenen Inhalts auseinander.
        /// </summary>
        public static string Name(int normtage, string bundesland = LAND_VORGABE)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KBP_KALENDER_NORM_NAME, normtage,
                             Landzusatz(bundesland));

        /// <summary>
        /// Der Ausweichname, wenn ein Kalender des Namens <see cref="Name"/> schon mit anderem Inhalt besteht (anderes
        /// Wochentagsraster der Klimaregion): dazu der Wochentag des 1. Januar im Raster — „Norm 230 Tage (BY, 1.1. Do)“.
        /// </summary>
        public static string NameMitWochentag(int normtage, string bundesland, Gemeinjahrkalender raster)
        {
            // W0 zählt ab Montag (0), DayOfWeek ab Sonntag (0).
            string tag = CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames[(raster.W0 + 1) % 7];
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KBP_KALENDER_NORM_NAME_TAG, normtage,
                                 Landzusatz(bundesland), tag);
        }

        /// <summary>Der Zusatz in der Klammer: das Länderkürzel, ohne bekanntes Land „bundesweit“.</summary>
        private static string Landzusatz(string bundesland)
            => Landesfeiertage.Bekannt(bundesland) ? bundesland : MyResource.Resource.KBP_KALENDER_NORM_BUNDESWEIT;

        /// <summary>
        /// Die Namen, unter denen der Normkalender gesucht und angelegt wird, in dieser Reihenfolge: <see cref="Name"/>,
        /// <see cref="NameMitWochentag"/>, dann der Ausweichname mit laufender Nummer („… #2“) — falls auch dieser Name
        /// schon mit anderem Inhalt besteht (etwa ein anderer Ferienfaktor). Ein Doppelname entsteht so nie.
        /// </summary>
        public static IEnumerable<string> Namensfolge(int normtage, string bundesland, Gemeinjahrkalender raster)
        {
            yield return Name(normtage, bundesland);
            string tag = NameMitWochentag(normtage, bundesland, raster);
            yield return tag;
            for (int n = 2; n <= NUMMER_HOECHSTENS; n++)
                yield return tag + " #" + n.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Ist der Jahrestag-Index <paramref name="d"/> (0 = 1. Januar) im Raster ein Werktag Montag bis Freitag?</summary>
        private static bool Werktag(Gemeinjahrkalender raster, int d) => (raster.W0 + d) % 7 < 5;

        /// <summary>
        /// Die Nutzungstage eines Kalenders im Raster: Werktage Montag bis Freitag, die weder Feiertag noch Ferientag sind.
        /// </summary>
        public static int Nutzungstage(Betriebskalender k, Gemeinjahrkalender raster)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            byte[] arten = k.Tagesarten(raster);
            int n = 0;
            for (int d = 0; d < 365; d++)
                if (arten[d] == Betriebskalender.TAG_NORMAL && Werktag(raster, d)) n++;
            return n;
        }

        /// <summary>
        /// Der Ferienfaktor, mit dem ein Ferientag die Tagesmenge des Sonntags trägt: <c>Σ Sonntag / Σ m_s</c>, auf zwei
        /// Stellen, in 0 … 1; 0 ohne Wochenprofil oder ohne Last.
        /// </summary>
        public static double Ferienfaktor(double[] woche)
        {
            if (woche == null || woche.Length < 168) return 0.0;
            double sonntag = 0.0, alle = 0.0;
            for (int h = 0; h < 168; h++)
            {
                alle += woche[h];
                if (h >= 144) sonntag += woche[h];
            }
            if (!(alle > 0)) return 0.0;
            double f = sonntag * 7.0 / alle;
            return Math.Round(Math.Min(1.0, Math.Max(0.0, f)), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Bildet den Normkalender (rein, ohne Datenbank). Liegen ohne Ferien schon höchstens <paramref name="normtage"/>
        /// Nutzungstage vor, bleibt er ohne Ferien.
        /// </summary>
        public static Betriebskalender Bilden(int normtage, string bundesland, Gemeinjahrkalender raster, double[] woche)
        {
            string land = Landesfeiertage.Bekannt(bundesland) ? bundesland : LAND_VORGABE;
            var k = new Betriebskalender
            {
                Bezeichner = Name(normtage, land),
                Bundesland = land,
                Ferienfaktor = Ferienfaktor(woche),
                FeiertagWieSonntag = true,
                FerienKuerzen = false
            };
            byte[] arten = k.Tagesarten(raster);
            int ueber = Nutzungstage(k, raster) - normtage;
            if (ueber <= 0) return k;

            bool zaehlt(int tag) => arten[tag - 1] == Betriebskalender.TAG_NORMAL && Werktag(raster, tag - 1);

            // Winter: ab dem 24.12. Tag für Tag, über den Jahreswechsel bis höchstens zum 6.1.
            var winter = new List<int>();
            for (int t = WINTER_BEGINN; t <= 365; t++) winter.Add(t);
            for (int t = 1; t <= WINTER_ENDE_SPAETESTENS; t++) winter.Add(t);
            int winterBis = -1;
            foreach (int t in winter)
            {
                if (ueber == 0) break;
                if (zaehlt(t)) { ueber--; winterBis = t; }
            }
            if (winterBis > 0) k.Ferien.Add(new Ferienzeitraum(WINTER_BEGINN, winterBis));
            if (ueber == 0) return k;

            // Sommer: ab dem ersten Montag ab dem 1.8., bis die Normtage erreicht sind.
            int beginn = SOMMER_BEGINN;
            while (!((raster.W0 + beginn - 1) % 7 == 0)) beginn++;
            int sommerBis = -1;
            for (int t = beginn; t < WINTER_BEGINN && ueber > 0; t++)
                if (zaehlt(t)) { ueber--; sommerBis = t; }
            if (sommerBis > 0) k.Ferien.Add(new Ferienzeitraum(beginn, sommerBis));
            return k;
        }

        /// <summary>Das Feiertagsland des Projekts: das erste Gebäude (nach Name) mit Land, sonst <see cref="LAND_VORGABE"/>.</summary>
        public static string Feiertagsland(int idProjekt)
        {
            foreach (BetriebskalenderGebaeudequelle q in BetriebskalenderGebaeudeCtrl.Quellen(idProjekt))
                if (Landesfeiertage.Bekannt(q.Feiertagsland)) return q.Feiertagsland;
            return LAND_VORGABE;
        }

        /// <summary>
        /// <b>Die Vorwahl beim Übernehmen</b>: trägt die Projektkopie <paramref name="idKopie"/> Nutzungstage nach Norm,
        /// liefert sie die ID des Normkalenders — vorhanden oder neu angelegt; sonst <c>null</c> (die Zeile bleibt ohne
        /// Vorwahl). Ohne Kalendertabelle oder bei gescheitertem Schreiben ebenfalls <c>null</c>.
        /// </summary>
        public static int? Vorwahl(int idProjekt, int idKopie) => Vorwahl(idProjekt, idKopie, out _);

        /// <summary>
        /// Wie <see cref="Vorwahl(int, int)"/>; <paramref name="name"/> ist der tatsächliche Name des vorgewählten
        /// Kalenders (für die Herleitungszeile), sonst <c>null</c>.
        /// </summary>
        public static int? Vorwahl(int idProjekt, int idKopie, out string name)
        {
            name = null;
            Betriebskalender soll = Soll(idProjekt, idKopie, out Gemeinjahrkalender raster, out int tage);
            if (soll == null) return null;
            (int? vorhanden, string frei) = Wahl(idProjekt, soll, Namensfolge(tage, soll.Bundesland, raster));
            if (vorhanden.HasValue)
            {
                name = BetriebskalenderCtrl.Lies(vorhanden.Value)?.Bezeichner ?? frei;
                return vorhanden;
            }
            if (frei == null) return null;
            soll.Bezeichner = frei;
            int id = BetriebskalenderCtrl.Speichern(soll);
            if (id <= 0) return null;
            name = frei;
            return id;
        }

        /// <summary>
        /// Der Name des Normkalenders, den <see cref="Vorwahl(int, int)"/> für diese Projektkopie nähme oder anlegte — ohne
        /// zu schreiben (die Herleitungszeile einer geladenen Zeile); <c>null</c> ohne Normtage oder ohne Kalendertabelle.
        /// </summary>
        public static string Bezeichner(int idProjekt, int idKopie)
        {
            Betriebskalender soll = Soll(idProjekt, idKopie, out Gemeinjahrkalender raster, out int tage);
            if (soll == null) return null;
            (int? vorhanden, string frei) = Wahl(idProjekt, soll, Namensfolge(tage, soll.Bundesland, raster));
            return vorhanden.HasValue ? BetriebskalenderCtrl.Lies(vorhanden.Value)?.Bezeichner ?? frei : frei;
        }

        /// <summary>Der Sollkalender der Projektkopie; <c>null</c> ohne Normtage, ohne Projekt oder ohne Kalendertabelle.</summary>
        private static Betriebskalender Soll(int idProjekt, int idKopie, out Gemeinjahrkalender raster, out int tage)
        {
            raster = default;
            tage = 0;
            if (idProjekt <= 0 || idKopie <= 0 || !BetriebskalenderCtrl.TabelleVorhanden()) return null;
            int? normtage = KaeltebedarfStammCtrl.ProjektNutzungstage(idKopie);
            if (normtage is not int t || t <= 0) return null;
            tage = t;
            raster = Konditionierungdatenweg.Raster(idProjekt);
            return Bilden(tage, Feiertagsland(idProjekt), raster, KaeltebedarfStammCtrl.ProjektWoche(idKopie));
        }

        /// <summary>
        /// Ein vorhandener Kalender eines der <paramref name="namen"/>: zuerst einer, den eine Zuordnung des Projekts nutzt
        /// (früherer Name vor späterem); dann je Name in Reihenfolge einer mit gleichem Inhalt. Sonst bleibt
        /// <c>Id = null</c>, und <c>Frei</c> ist der erste Name, unter dem noch kein Kalender steht — der Name eines neuen
        /// Kalenders, damit kein Doppelname mit anderem Inhalt entsteht; <c>null</c>, wenn alle Namen belegt sind.
        /// </summary>
        public static (int? Id, string Frei) Wahl(int idProjekt, Betriebskalender soll, IEnumerable<string> namen)
        {
            if (soll == null || namen == null) return (null, null);
            List<string> folge = namen.Select(n => (n ?? "").Trim()).Where(n => n.Length > 0).Distinct().ToList();
            List<Betriebskalender> alle = BetriebskalenderCtrl.Liste();
            Dictionary<string, List<Betriebskalender>> jeName = folge.ToDictionary(
                n => n, n => alle.Where(k => string.Equals((k.Bezeichner ?? "").Trim(), n, StringComparison.Ordinal))
                                 .OrderBy(k => k.ID).ToList(), StringComparer.Ordinal);

            if (jeName.Values.Any(l => l.Count > 0))
            {
                HashSet<int> genutzt = GenutzteKalender(idProjekt);
                foreach (string n in folge)
                {
                    Betriebskalender projekt = jeName[n].FirstOrDefault(k => genutzt.Contains(k.ID));
                    if (projekt != null) return (projekt.ID, n);
                }
            }
            foreach (string n in folge)
            {
                if (jeName[n].Count == 0) return (null, n);
                Betriebskalender gleich = jeName[n].FirstOrDefault(k => Gleich(k, soll));
                if (gleich != null) return (gleich.ID, n);
            }
            return (null, null);
        }

        /// <summary>Die Kalender, die eine Zuordnung des Projekts nutzt (die drei Bedarfszuordnungen und die Kälte).</summary>
        private static HashSet<int> GenutzteKalender(int idProjekt)
        {
            var genutzt = new HashSet<int>();
            foreach (string tab in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                foreach (int? id in BetriebskalenderCtrl.KalenderDerZuordnungen(tab, idProjekt).Values)
                    if (id.HasValue) genutzt.Add(id.Value);
            // Die Kältezuordnung führt ihren Kalender in derselben Spalte, steht aber nicht in der Liste der drei.
            if (DataRepository.SpalteVorhanden(TAB_KAELTE, BedarfNetzKalenderSchema.SPALTE_ID_KALENDER))
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Betriebskalender FROM Z_Projekt_Kaeltebedarf WHERE ID_Projekt = ? AND ID_Betriebskalender IS NOT NULL",
                    new DbParam("?", idProjekt));
                if (dt != null)
                    foreach (DataRow r in dt.Rows) genutzt.Add(Convert.ToInt32(r[0], CultureInfo.InvariantCulture));
            }
            return genutzt;
        }

        /// <summary>Gleicher Inhalt: Land, Ferien in Reihenfolge, Faktor und beide Schalter.</summary>
        private static bool Gleich(Betriebskalender a, Betriebskalender b)
            => string.Equals(a.Bundesland, b.Bundesland, StringComparison.Ordinal)
               && Math.Abs(a.Ferienfaktor - b.Ferienfaktor) < 1e-9
               && a.FeiertagWieSonntag == b.FeiertagWieSonntag
               && a.FerienKuerzen == b.FerienKuerzen
               && (a.Ferien ?? new List<Ferienzeitraum>()).SequenceEqual(b.Ferien ?? new List<Ferienzeitraum>());
    }
}
