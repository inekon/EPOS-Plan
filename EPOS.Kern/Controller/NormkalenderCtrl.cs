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
    /// den Betriebskalender „Norm 230 Tage“ vorgewählt.
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
    /// <para><b>Wiederverwendung:</b> Die Kalender sind projektübergreifend. Genommen wird ein Kalender dieses Namens,
    /// den eine Zuordnung des Projekts schon nutzt, sonst einer dieses Namens mit gleichem Inhalt; erst dann entsteht
    /// ein neuer.</para>
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

        /// <summary>Der Name des Normkalenders, aus der Zahl der Normtage gebildet („Norm 230 Tage“).</summary>
        public static string Name(int normtage)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KBP_KALENDER_NORM_NAME, normtage);

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
            var k = new Betriebskalender
            {
                Bezeichner = Name(normtage),
                Bundesland = Landesfeiertage.Bekannt(bundesland) ? bundesland : LAND_VORGABE,
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
        public static int? Vorwahl(int idProjekt, int idKopie)
        {
            if (idProjekt <= 0 || idKopie <= 0 || !BetriebskalenderCtrl.TabelleVorhanden()) return null;
            int? normtage = KaeltebedarfStammCtrl.ProjektNutzungstage(idKopie);
            if (normtage is not int tage || tage <= 0) return null;

            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(idProjekt);
            Betriebskalender soll = Bilden(tage, Feiertagsland(idProjekt), raster, KaeltebedarfStammCtrl.ProjektWoche(idKopie));
            int? vorhanden = Vorhanden(idProjekt, soll);
            if (vorhanden.HasValue) return vorhanden;
            int id = BetriebskalenderCtrl.Speichern(soll);
            return id > 0 ? id : (int?)null;
        }

        /// <summary>
        /// Ein vorhandener Kalender gleichen Namens: zuerst einer, den eine Zuordnung des Projekts nutzt, dann einer mit
        /// gleichem Inhalt; <c>null</c> = keiner.
        /// </summary>
        public static int? Vorhanden(int idProjekt, Betriebskalender soll)
        {
            if (soll == null) return null;
            string name = (soll.Bezeichner ?? "").Trim();
            List<Betriebskalender> gleichnamig = BetriebskalenderCtrl.Liste()
                .Where(k => string.Equals((k.Bezeichner ?? "").Trim(), name, StringComparison.Ordinal))
                .OrderBy(k => k.ID).ToList();
            if (gleichnamig.Count == 0) return null;

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
            Betriebskalender projekt = gleichnamig.FirstOrDefault(k => genutzt.Contains(k.ID));
            if (projekt != null) return projekt.ID;

            Betriebskalender gleich = gleichnamig.FirstOrDefault(k => Gleich(k, soll));
            return gleich?.ID;
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
