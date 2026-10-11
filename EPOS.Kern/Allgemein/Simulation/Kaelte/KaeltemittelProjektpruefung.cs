using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Die Gerätearten, deren Kältemittel die Zulässigkeitsprüfung liest (Konzept Kältemittel als Stammdatum 3.3).</summary>
    public enum KaeltemittelGeraeteart
    {
        /// <summary>Eine Kältemaschinen-Anlage mit ihrer Projektkopie <c>Tab_Kaeltemaschine</c>.</summary>
        Kaeltemaschine,

        /// <summary>Eine Wärmepumpen-Anlage, deren Projektgerät <c>Tab_WP</c> einen Kältemittel-Code führt.</summary>
        Waermepumpe
    }

    /// <summary>
    /// Ein Kälteerzeuger eines Projekts mit seinem Kältemittel und dem Ergebnis der Prüfung.
    /// <see cref="Ergebnis"/> ist <c>null</c> ohne Inbetriebnahmejahr (keine Prüfung) und bei Absorption;
    /// <see cref="Regel"/> ist bei Klasse 2 die treffende Regel des frühesten Stichtags (Fundstelle, Status).
    /// </summary>
    public sealed record KaeltemittelErzeugerpruefung(
        int ID_Anlage, int? ID_Geraet, KaeltemittelGeraeteart Art, string Bezeichner, string Code,
        string Geraeteklasse, KaeltemittelErgebnis Ergebnis, KaeltemittelRegel Regel);

    /// <summary>
    /// Ein Kälteerzeuger, wie ihn der Bericht braucht (Konzept Kältemittel als Stammdatum 5.3): Art, Gerät, Name,
    /// Kältemittel-Code und Ergebnisklasse (<c>null</c> = nicht geprüft). Gesammelt von <c>BerichtsDatenSammler</c>,
    /// gelesen von <see cref="Berichtstabellen.Kaelteerzeuger"/> — die Schreiber lesen keine Datenbank.
    /// </summary>
    public sealed record KaeltemittelBerichtszeile(
        KaeltemittelGeraeteart Art, int? ID_Geraet, string Bezeichner, string Code, KaeltemittelErgebnisklasse? Klasse)
    {
        /// <summary>Die Berichtszeilen aus der Prüfung eines Projekts.</summary>
        public static List<KaeltemittelBerichtszeile> Aus(IEnumerable<KaeltemittelErzeugerpruefung> pruefung)
        {
            var l = new List<KaeltemittelBerichtszeile>();
            if (pruefung == null) return l;
            foreach (KaeltemittelErzeugerpruefung p in pruefung)
                if (p != null) l.Add(new KaeltemittelBerichtszeile(p.Art, p.ID_Geraet, p.Bezeichner, p.Code, p.Ergebnis?.Klasse));
            return l;
        }
    }

    /// <summary>
    /// <b>Die Zulässigkeitsprüfung eines Projekts</b> (Konzept Kältemittel als Stammdatum 3.3, 5.1, 5.2) — der
    /// Datenbankweg vor der reinen Klasse <see cref="Kaeltemittelzulaessigkeit"/>: Er liest das Inbetriebnahmejahr
    /// des Projekts, je Kältemaschinen-Anlage die Projektkopie (Geräteart, Nennkälteleistung je Gerät, Verweis
    /// <c>ID_Kaeltemittel</c>, sonst der Text per Code, Geräte-GWP) und je Wärmepumpen-Anlage mit Kältemittel-Code das
    /// Projektgerät (Bauart, Typ, Nennleistung), gleicht den Code gegen <c>Tab_Kaeltemittel_STAMM</c> ab und prüft
    /// gegen den gesäten Regelsatz. Warnkriterium, Laufhinweis, Kältebahn und Bericht lesen denselben Weg.
    ///
    /// <para><b>Ohne Jahr prüft der Weg nichts</b> und liest weder Regeln noch Kältemitteltabelle; er liefert die
    /// Erzeuger dann nur mit ihrem Code (für die Berichtsspalte), wenn <c>mitCodes</c> gesetzt ist.</para>
    /// </summary>
    public static class KaeltemittelProjektpruefung
    {
        // Wärmepumpen-Anlagen des Projekts mit ihrem Projektgerät; der Code steht als Text am Gerät (KM4-Q9).
        private const string SQL_WAERMEPUMPEN =
            "SELECT a.ID AS ID_Anlage, a.Bezeichner AS Anlage, w.ID AS ID_WP, w.Bezeichner AS Geraet, w.Bauart AS Bauart, " +
            "w.Typ AS Typ, w.Nennleistung AS Nennleistung, w.Kaeltemittel AS Kaeltemittel " +
            "FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP " +
            "WHERE a.ID_Projekt = ? AND a.ID_Type = ? ORDER BY a.ID";

        private const string SQL_JAHR =
            "SELECT \"" + KaeltemittelSchema.SPALTE_INBETRIEBNAHMEJAHR + "\" FROM \"" + KaeltemittelSchema.TAB_PROJEKT +
            "\" WHERE \"ID\" = ?";

        /// <summary>
        /// Das geplante Inbetriebnahmejahr des Projekts (<c>Tab_Projekt.Inbetriebnahmejahr</c>); <c>null</c> = leer,
        /// außerhalb 2000 bis 2100, vor dem Schemaschritt oder bei einem Lesefehler. Dialogfrei.
        /// </summary>
        public static int? Inbetriebnahmejahr(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (!DataRepository.SpalteVorhanden(KaeltemittelSchema.TAB_PROJEKT, KaeltemittelSchema.SPALTE_INBETRIEBNAHMEJAHR))
                    return null;
                object o = StilleDb.Scalar(SQL_JAHR, StilleDb.Par("@id", DbParamTyp.Integer, idProjekt));
                if (o == null || o == DBNull.Value) return null;
                int jahr = Convert.ToInt32(o, CultureInfo.InvariantCulture);
                return jahr >= KaeltemittelSchema.INBETRIEBNAHMEJAHR_MIN && jahr <= KaeltemittelSchema.INBETRIEBNAHMEJAHR_MAX ? jahr : (int?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Prüft alle Kälteerzeuger des Projekts: jede Kältemaschinen-Anlage mit Projektkopie und jede
        /// Wärmepumpen-Anlage, deren Gerät einen Kältemittel-Code führt (KM4-Q16 — auch ohne Kühlbetrieb). Ohne
        /// Jahr leer, außer mit <paramref name="mitCodes"/>: dann dieselben Erzeuger mit Code und ohne Ergebnis.
        /// </summary>
        public static IReadOnlyList<KaeltemittelErzeugerpruefung> Pruefen(int idProjekt, bool mitCodes = false)
        {
            var liste = new List<KaeltemittelErzeugerpruefung>();
            if (idProjekt <= 0) return liste;
            int? jahr = Inbetriebnahmejahr(idProjekt);
            if (!jahr.HasValue && !mitCodes) return liste;

            IReadOnlyList<KaeltemittelRegel> regeln = jahr.HasValue
                ? Kaeltemittelregeln.Lesen(new GesetzKatalog())
                : Array.Empty<KaeltemittelRegel>();

            // 1. Kältemaschinen in der Folge ihrer Anlagenzeilen.
            foreach (KaeltemaschineAnlageModel a in KaeltemaschineAnlageCtrl.ListeStill(idProjekt))
            {
                if (a == null || a.AnlagenId <= 0 || !a.IdKaeltemaschine.HasValue) continue;
                KaeltemaschineModel m = KaeltemaschineCtrl.LadenStill(a.IdKaeltemaschine.Value);
                if (m == null) continue;

                string text = (m.Kaeltemittel ?? "").Trim();
                // Der Verweis zuerst, sonst der Text per Code (Konzept 3.3, KM4-Q17).
                KaeltemittelCtrl.Kaeltemittelsatz satz =
                    m.ID_Kaeltemittel.HasValue ? KaeltemittelCtrl.Laden(m.ID_Kaeltemittel.Value) : null;
                if (satz == null && text.Length > 0) satz = KaeltemittelCtrl.LadenZumCode(text);

                string klasse = Kaeltemittelregeln.GeraeteklasseKaeltemaschine(m.Geraeteart);
                var eingang = new KaeltemittelEingang
                {
                    Geraeteklasse = klasse,
                    LeistungKw = m.Nennkaelteleistung_kW,     // je Gerät, nicht × Anzahl (Konzept 3.3)
                    Stoff = Kaeltemittelregeln.Stoff(satz),
                    GeraeteGwp = m.Kaeltemittel_GWP,
                    Jahr = jahr,
                    Regeln = regeln,
                };
                string name = !string.IsNullOrWhiteSpace(a.Bezeichner) ? a.Bezeichner
                            : !string.IsNullOrWhiteSpace(m.Bezeichner) ? m.Bezeichner
                            : a.AnlagenId.ToString(CultureInfo.CurrentCulture);
                liste.Add(Zeile(a.AnlagenId, m.Id, KaeltemittelGeraeteart.Kaeltemaschine, name,
                                satz?.Code ?? (text.Length > 0 ? text : null), klasse, eingang, regeln));
            }

            // 2. Wärmepumpen mit Kältemittel-Code; Abgleich über den Code, Bivalenzvorgaben bleibt unberührt.
            DataTable dt = StilleDb.Tabelle(SQL_WAERMEPUMPEN,
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, WizardItemClass.WP_TYP));
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                {
                    string code = StilleDb.Text(StilleDb.Feld(r, "Kaeltemittel")).Trim();
                    if (code.Length == 0) continue;
                    KaeltemittelCtrl.Kaeltemittelsatz satz = KaeltemittelCtrl.LadenZumCode(code);
                    string klasse = Kaeltemittelregeln.GeraeteklasseWaermepumpe(
                        StilleDb.Text(StilleDb.Feld(r, "Bauart")), StilleDb.Text(StilleDb.Feld(r, "Typ")));
                    object nenn = StilleDb.Feld(r, "Nennleistung");
                    var eingang = new KaeltemittelEingang
                    {
                        Geraeteklasse = klasse,
                        // Nennleistung = Heizleistung (KM4-Q6).
                        LeistungKw = nenn == null || nenn == DBNull.Value ? (double?)null
                                   : Convert.ToDouble(nenn, CultureInfo.InvariantCulture),
                        Stoff = Kaeltemittelregeln.Stoff(satz),
                        GeraeteGwp = null,
                        Jahr = jahr,
                        Regeln = regeln,
                    };
                    int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID_Anlage"));
                    string anlage = StilleDb.Text(StilleDb.Feld(r, "Anlage"));
                    string geraet = StilleDb.Text(StilleDb.Feld(r, "Geraet"));
                    string name = !string.IsNullOrWhiteSpace(anlage) ? anlage
                                : !string.IsNullOrWhiteSpace(geraet) ? geraet
                                : idAnlage.ToString(CultureInfo.CurrentCulture);
                    liste.Add(Zeile(idAnlage, StilleDb.Zahl(StilleDb.Feld(r, "ID_WP")), KaeltemittelGeraeteart.Waermepumpe,
                                    name, satz?.Code ?? code, klasse, eingang, regeln));
                }
            return liste;
        }

        private static KaeltemittelErzeugerpruefung Zeile(int idAnlage, int? idGeraet, KaeltemittelGeraeteart art, string name,
                                                         string code, string klasse, KaeltemittelEingang eingang,
                                                         IReadOnlyList<KaeltemittelRegel> regeln)
        {
            KaeltemittelErgebnis e = Kaeltemittelzulaessigkeit.Pruefen(eingang);
            KaeltemittelRegel regel = null;
            if (e != null && e.Klasse == KaeltemittelErgebnisklasse.AbStichtag)
                foreach (KaeltemittelRegel r in regeln)
                    if (r != null && r.Schluessel == e.Regelschluessel && r.Stichtag == e.Stichtag) { regel = r; break; }
            return new KaeltemittelErzeugerpruefung(idAnlage, idGeraet, art, name ?? "", code, klasse, e, regel);
        }
    }
}
