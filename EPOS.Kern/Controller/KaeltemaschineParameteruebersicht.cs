using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Parameterübersicht einer Kältemaschine</b> (KD-3) — die Kennwerte, die „Vergleichen" ab zwei
    /// gewählten Sätzen nebeneinanderstellt, nach dem Muster der Wärmepumpe (<see cref="ParameterUebersichtCtrl"/>).
    ///
    /// <para><b>Warum nicht der allgemeine Weg.</b> <see cref="ParameterUebersichtCtrl.Werte"/> zeigt die Spalten der
    /// Stammtabelle roh: die Rückkühlart als Persistenzwert, keine Teillastwerte g und keine Zahl der Stützstellen.
    /// Die Kältemaschine vergleicht dagegen, was ein Planer nebeneinanderlegt — Nennkälteleistung, EER, Rückkühlart,
    /// Typ und Verdichterregelung, Kältemittel, Mindestteillast, Hilfsstrom der Rückkühlung, kleinster
    /// Kaltwasservorlauf, die Lesezeile g(0,25)/g(0,5)/g(0,75) der wirksamen Teillastkurve (dieselbe Rechnung wie die
    /// Gruppe „Teillast und Takten", <see cref="KaeltemaschineTeillastDialogrechnung.Lesezeile"/>), die Zahl der
    /// Kennlinienstützstellen und die Herkunft (<see cref="KaeltemaschineStammCtrl.HerkunftText"/>) — alles als
    /// Anzeigetext in der Oberflächensprache.</para>
    ///
    /// <para>Die Zeilen tragen feste Schlüssel (<see cref="ParameterEintrag.Spalte"/>), damit der Vergleich die Werte
    /// zweier Sätze Zeile für Zeile paart; ein leerer Wert ist <see cref="ParameterVerwendung.LEER"/>.</para>
    /// </summary>
    public static class KaeltemaschineParameteruebersicht
    {
        /// <summary>Schlüssel der Teillastzeile g(0,25).</summary>
        public const string ZEILE_G25 = "G25";

        /// <summary>Schlüssel der Teillastzeile g(0,5).</summary>
        public const string ZEILE_G50 = "G50";

        /// <summary>Schlüssel der Teillastzeile g(0,75).</summary>
        public const string ZEILE_G75 = "G75";

        /// <summary>Schlüssel der Zeile „Kennlinienstützstellen".</summary>
        public const string ZEILE_STUETZSTELLEN = "STUETZSTELLEN";

        /// <summary>Schlüssel der Zeile „Herkunft".</summary>
        public const string ZEILE_HERKUNFT = "HERKUNFT";

        private static readonly Verwendung[] SIM = { Verwendung.Simulation };
        private static readonly Verwendung[] DLG = { Verwendung.Dialog };

        /// <summary>
        /// Die Übersicht des Katalogsatzes mit dem Bezeichner (bei doppeltem Bezeichner der mit der kleinsten ID,
        /// Regel von <see cref="ParameterUebersichtCtrl"/>); ein unbekannter Satz liefert dieselben Zeilen mit lauter
        /// <see cref="ParameterVerwendung.LEER"/>.
        /// </summary>
        public static IReadOnlyList<Parameterwert> Werte(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return Werte(null, false);
            return Aus(Kopf("SELECT ID, " + Katalogfassung.SPALTE_SCHLUESSEL + " FROM " + KaeltemaschineStammCtrl.TABLE +
                            " WHERE Bezeichner = ? ORDER BY ID", new DbParam("@bez", bezeichner)));
        }

        /// <summary>Die Übersicht des Katalogsatzes mit der ID; unbekannt wie <see cref="Werte(string)"/>.</summary>
        public static IReadOnlyList<Parameterwert> WerteZuId(int id)
        {
            return Aus(Kopf("SELECT ID, " + Katalogfassung.SPALTE_SCHLUESSEL + " FROM " + KaeltemaschineStammCtrl.TABLE +
                            " WHERE ID = ?", new DbParam("@id", id)));
        }

        /// <summary>
        /// <b>Die Übersicht eines Satzes</b> ohne Datenbank: <paramref name="m"/> mit Kopf, Teillastfeldern und
        /// Kennlinie; <paramref name="katalogsatz"/> sagt, ob er einen Katalogschlüssel trägt (Herkunft
        /// „Auslieferung"). <c>null</c> liefert die Zeilen mit lauter <see cref="ParameterVerwendung.LEER"/>.
        /// </summary>
        public static IReadOnlyList<Parameterwert> Werte(KaeltemaschineModel m, bool katalogsatz)
        {
            const string LEER = ParameterVerwendung.LEER;
            KaeltemaschineTeillastDialogrechnung.Lesestand l = m == null ? null : KaeltemaschineTeillastDialogrechnung.Lesezeile(m);
            string g(double wert) => l == null ? LEER : Zahl(Math.Round(wert, 2));
            string Format(string vorlage, string zahl) => string.Format(CultureInfo.CurrentCulture, vorlage, zahl);

            return new[]
            {
                W(KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG, MyResource.Resource.KM_LBL_NENNKAELTELEISTUNG, "kW", SIM,
                  Zahl(m?.Nennkaelteleistung_kW)),
                W(KaeltemaschineSchema.SPALTE_NENN_EER, MyResource.Resource.KM_LBL_NENN_EER, "-", DLG, Zahl(m?.Nenn_EER)),
                W(KaeltemaschineSchema.SPALTE_RUECKKUEHLART, MyResource.Resource.KM_LBL_RUECKKUEHLART, "", SIM,
                  Text(KaeltemaschineStammCtrl.RueckkuehlartText(m?.Rueckkuehlart))),
                W("Typ", MyResource.Resource.KM_LBL_TYP, "", DLG, Text(m?.Typ)),
                W(KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG, MyResource.Resource.KM_LBL_VERDICHTERREGELUNG, "", SIM,
                  Text(Regelung(m?.Verdichterregelung))),
                W(KaeltemaschineSchema.SPALTE_KAELTEMITTEL, MyResource.Resource.KM_LBL_KAELTEMITTEL, "", DLG, Text(m?.Kaeltemittel)),
                W(KaeltemaschineSchema.SPALTE_MINDESTTEILLAST, MyResource.Resource.KM_LBL_MINDESTTEILLAST, "%", SIM,
                  Zahl(m?.Mindestteillast_Prozent)),
                W(KaeltemaschineSchema.SPALTE_HILFSSTROM_RUECKKUEHLUNG, MyResource.Resource.KM_LBL_HILFSSTROM, "kW", SIM,
                  Zahl(m?.Hilfsstrom_Rueckkuehlung_kW)),
                W(KaeltemaschineSchema.SPALTE_KALTWASSER_VORLAUF_MIN, MyResource.Resource.KM_LBL_KALTWASSER_MIN, "°C", SIM,
                  Zahl(m?.Kaltwasser_Vorlauf_Min)),
                W(ZEILE_G25, Format(MyResource.Resource.KM_VGL_G, Zahl(0.25)), "-", SIM, g(l?.G25 ?? 0)),
                W(ZEILE_G50, Format(MyResource.Resource.KM_VGL_G, Zahl(0.5)), "-", SIM, g(l?.G50 ?? 0)),
                W(ZEILE_G75, Format(MyResource.Resource.KM_VGL_G, Zahl(0.75)), "-", SIM, g(l?.G75 ?? 0)),
                W(ZEILE_STUETZSTELLEN, MyResource.Resource.KM_VGL_STUETZSTELLEN, "", SIM,
                  m == null ? LEER : (m.Kennlinie?.Count ?? 0).ToString(CultureInfo.CurrentCulture)),
                W(ZEILE_HERKUNFT, MyResource.Resource.ADM_VG_HERKUNFT, "", DLG,
                  m == null ? LEER : KaeltemaschineStammCtrl.HerkunftText(m.Typ, katalogsatz)),
            };
        }

        /// <summary>Der Anzeigetext einer Verdichterregelung (Persistenzwert → Oberflächensprache).</summary>
        private static string Regelung(string persistenzwert)
        {
            switch (persistenzwert)
            {
                case KaeltemaschineTeillastSchema.REGELUNG_EIN_AUS: return MyResource.Resource.KM_REGELUNG_EIN_AUS;
                case KaeltemaschineTeillastSchema.REGELUNG_STUFEN: return MyResource.Resource.KM_REGELUNG_STUFEN;
                case KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL: return MyResource.Resource.KM_REGELUNG_DREHZAHL;
                default: return persistenzwert;
            }
        }

        /// <summary>Lädt den Satz zur ersten Zeile von <paramref name="kopf"/>; ohne Zeile die leere Übersicht.</summary>
        private static IReadOnlyList<Parameterwert> Aus(DataRow kopf)
        {
            if (kopf == null) return Werte(null, false);
            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(KaeltemaschineStammCtrl.Ganz(kopf["ID"]));
            bool katalogsatz = !string.IsNullOrEmpty(KaeltemaschineStammCtrl.Text(kopf[Katalogfassung.SPALTE_SCHLUESSEL]));
            return Werte(m, katalogsatz);
        }

        /// <summary>Die erste Zeile der Abfrage; <c>null</c>, wenn es keine gibt oder die Datenbank schweigt.</summary>
        private static DataRow Kopf(string sql, DbParam p)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(sql, p);
                return (dt == null || dt.Rows.Count == 0) ? null : dt.Rows[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Parameterwert W(string spalte, string anzeige, string einheit, Verwendung[] verwendung, string wert)
            => new Parameterwert(new ParameterEintrag(spalte, anzeige, einheit, verwendung, "KaeltemaschineParameteruebersicht"), wert);

        private static string Zahl(double? wert)
            => wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value)
                ? wert.Value.ToString(CultureInfo.CurrentCulture)
                : ParameterVerwendung.LEER;

        private static string Text(string s) => string.IsNullOrWhiteSpace(s) ? ParameterVerwendung.LEER : s;
    }
}
