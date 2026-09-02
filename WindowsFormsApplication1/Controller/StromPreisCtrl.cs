using System;
using System.Data;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ergebnis der Preisbeschaffung: die drei Zeitreihen des Engine-Eingangs und das,
    /// was darueber im Protokoll und im Ergebnissatz stehen muss.
    /// </summary>
    public sealed class StromPreisErgebnis
    {
        /// <summary>Bezugspreis p_bezug [ct/kWh] je Intervall.</summary>
        public double[] BezugspreisCtKwh;

        /// <summary>
        /// ENERGIEpreis [ct/kWh] je Intervall - die gewaehlte Quelle <b>ohne</b> die
        /// Aufschlaege aus Fachkonzept 4.2.
        /// </summary>
        /// <remarks>
        /// Bezugsgroesse des Netzladepreises: <c>p_netzlade[i] = p_energie[i] +
        /// a_netzlade</c> (Fachkonzept 4.4). Fuer den Eigenverbrauch bleibt dagegen
        /// <see cref="BezugspreisCtKwh"/> massgeblich, weil dort der vermiedene
        /// Vollpreis den Nutzen bestimmt. AP10.
        /// </remarks>
        public double[] EnergiepreisCtKwh;

        /// <summary>Einspeiseverguetung v_pv [ct/kWh] je Intervall.</summary>
        public double[] VerguetungPvCtKwh;

        /// <summary>Einspeise-/KWK-Erloes v_bhkw [ct/kWh] je Intervall.</summary>
        public double[] VerguetungBhkwCtKwh;

        /// <summary>
        /// Erloes je ins Netz VERKAUFTER kWh [ct/kWh] je Intervall (AP10,
        /// Fachkonzept 2.2 Entladeprioritaet 2).
        /// </summary>
        /// <remarks>
        /// Bei der Preisquelle "Spotmarkt" ist das die Spotreihe - nur dort gibt es
        /// ueberhaupt einen zeitaufgeloesten Marktpreis. Sonst faellt der Erloes auf
        /// die Einspeiseverguetung <c>v_pv</c> zurueck: Ohne Marktpreisreihe ist die
        /// Verguetung der einzige belegbare Wert einer eingespeisten kWh, und der
        /// Verkauf lohnt dann praktisch nie - genau die konservative Aussage, die das
        /// Fachkonzept in 2.2 trifft.
        /// </remarks>
        public double[] ErloesCtKwh;

        /// <summary>
        /// Tatsaechlich verwendete Preisquelle (Werte aus <c>DbWerte.SP_PREISQUELLE_*</c>) -
        /// bei einem Rueckfall NICHT die gewuenschte, sondern die gerechnete.
        /// </summary>
        public string Quelle = DbWerte.SP_PREISQUELLE_FIXPREIS;

        /// <summary>
        /// Bezeichnung der verwendeten Preisversion fuer
        /// <c>Tab_ErgebnisStromspeicher.Preisversion</c> (Fachkonzept 4.1).
        /// </summary>
        public string Preisversion = "";

        /// <summary>Wirksamer Aufschlag [ct/kWh]; 0, wenn das Flag der Variante aus ist.</summary>
        public double AufschlagCtKwh;

        /// <summary>Mittelwert der ENERGIEpreisreihe vor dem Aufschlag [ct/kWh].</summary>
        public double EnergiepreisMittelCtKwh;

        /// <summary>Mittelwert des fertigen Bezugspreises [ct/kWh] - fuer die Anzeige.</summary>
        public double BezugspreisMittelCtKwh;

        /// <summary>Protokollhinweise, zeilenweise; leer, wenn alles glattging.</summary>
        public string Hinweis = "";
    }

    /// <summary>
    /// Beschafft die Preis- und Verguetungsreihen einer Speichersimulation
    /// (Fachkonzept Stromspeicher 4.1 bis 4.3, Arbeitspaket AP4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der einzige Ort, an dem p_bezug entsteht.</b> Bis AP4 standen im
    /// <c>StromspeicherSimCtrl</c> drei Konstanten (20 / 5 / 5 ct/kWh); sie sind durch
    /// diesen Controller ersetzt. <c>StromspeicherSimCtrl.BaueEingang</c> ruft
    /// ausschliesslich hier auf - eine zweite Preisbeschaffung an anderer Stelle waere
    /// genau die Doppelpflege, die das Fachkonzept 4.2 beim Aufschlagsblock ausschliesst.
    /// </para>
    /// <para>
    /// <b>Rechenfrei.</b> Der Controller liest und waehlt aus; jede Formel steht in der
    /// Engine (<c>PreisModell</c>, <c>Aufschlagssatz</c>) und ist dort headless getestet.
    /// </para>
    /// <para>
    /// <b>Kulturregel.</b> Zahlen kommen typisiert aus der <see cref="DataTable"/>; die
    /// beiden Profilzeichenketten werden mit <see cref="CultureInfo.InvariantCulture"/>
    /// zerlegt (Ablageformat von <c>Form_Quellprofil</c>). Der Text der Preisversion
    /// wird ebenfalls invariant gebildet - er wird gespeichert und muss auf jeder
    /// Windows-Einstellung gleich lauten.
    /// </para>
    /// </remarks>
    public class StromPreisCtrl
    {
        /// <summary>
        /// Umrechnung des Arbeitspreises: <c>energy_price.arbeitspreis</c> steht fuer
        /// Strom in EUR/kWh (Direktabrechnung nach kWh, <c>HasHi = false</c>), die
        /// Engine rechnet in ct/kWh.
        /// </summary>
        public const double EUR_JE_KWH_IN_CT = 100.0;

        /// <summary>
        /// Rueckfall-Arbeitspreis [ct/kWh], wenn weder <c>energy_price</c> noch
        /// <c>energy_project_settings</c> noch der Katalog einen Preis fuehren.
        /// Wertgleich dem Platzhalter, mit dem AP2b/AP3b gerechnet haben - so aendert
        /// ein Projekt ohne jede Preispflege sein Ergebnis durch AP4 nicht.
        /// </summary>
        public const double FIXPREIS_RUECKFALL_CT_KWH = StromspeicherSimCtrl.FIXPREIS_BEZUG_CT_KWH;

        private string _hinweis = "";

        // =================================================================
        // Oeffentliche Schnittstelle
        // =================================================================

        /// <summary>
        /// Baut Bezugspreis- und Verguetungsreihen fuer eine Speichervariante.
        /// </summary>
        /// <param name="idProjekt">Projekt-ID.</param>
        /// <param name="variante">
        /// Die zu rechnende Variante (Preisquelle, Reihenverweise, Aufschlagsflag).
        /// <c>null</c> = Vorbelegung, also Fixpreis mit Aufschlaegen.
        /// </param>
        /// <param name="anzahlIntervalle">
        /// Laenge der Zielreihen; im Produktivfall
        /// <see cref="RasterAdapter.ViertelstundenJahr"/>.
        /// </param>
        /// <param name="stichtag">
        /// Stichtag der Preisversion (Fachkonzept 4.1). <c>null</c> = automatisch, siehe
        /// <see cref="Stichtag"/>.
        /// </param>
        public StromPreisErgebnis Baue(int idProjekt, StromspeicherVarianteModel variante,
                                       int anzahlIntervalle, DateTime? stichtag = null)
        {
            if (anzahlIntervalle <= 0)
                throw new ArgumentOutOfRangeException(nameof(anzahlIntervalle));

            _hinweis = "";
            StromPreisErgebnis e = new StromPreisErgebnis();

            StromspeicherVarianteModel v = variante ?? new StromspeicherVarianteModel();

            StromAufschlagModel aufschlagModel;
            double[] energiereihe;

            // Der gesamte Datenzugriff liegt in einem einzigen dialogfreien Block -
            // dieselbe Regel wie in StromspeicherSimCtrl.LeseParameter.
            using (DataRepository.EngineModus())
            {
                StromAufschlagCtrl aufschlagCtrl = new StromAufschlagCtrl();
                aufschlagModel = aufschlagCtrl.ReadStrom(idProjekt);

                if (!aufschlagModel.AusDatenbank)
                    HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_KEIN_STROMTRAEGER);

                energiereihe = BaueEnergiereihe(idProjekt, v, anzahlIntervalle, stichtag, e);
            }

            // --- Aufschlag (Fachkonzept 4.2) -----------------------------------
            Aufschlagssatz satz = StromAufschlagCtrl.AlsAufschlagssatz(aufschlagModel);
            e.AufschlagCtKwh = v.Aufschlag_Anwenden ? satz.WirksamCtKwh : 0.0;

            double min, max, mittel;
            PreisModell.Spannweite(energiereihe, out min, out max, out mittel);
            e.EnergiepreisMittelCtKwh = mittel;
            e.EnergiepreisCtKwh = energiereihe;

            e.BezugspreisCtKwh = PreisModell.MitAufschlag(energiereihe, e.AufschlagCtKwh);

            PreisModell.Spannweite(e.BezugspreisCtKwh, out min, out max, out mittel);
            e.BezugspreisMittelCtKwh = mittel;

            // --- Verguetung (Fachkonzept 4.3) ----------------------------------
            // ETAPPE P4 (Befund V4, Entscheidung F7): Ist der PV-Verguetungsdialog
            // AKTIV, ist ER die fuehrende Verguetungswahrheit - v_pv kommt aus dem
            // Dialogsatz (Stufe 1, mengenunabhaengig), nicht mehr aus Verguetung_PV
            // des Aufschlagsblocks. Inaktiv bleibt alles beim Bestand.
            double vpvCt = aufschlagModel.Verguetung_PV;
            try
            {
                ProjektPhotovoltaikCtrl pvc = new ProjektPhotovoltaikCtrl();
                ProjektPhotovoltaikModel pvDialog = pvc.Lies(idProjekt);
                double? fuehrend = PvErloesRechner.VpvCtKwh(pvDialog,
                    PhotovoltaikCtrl.KwpDesProjekts(idProjekt),
                    new GesetzKatalog().Wert,
                    jahr => pvc.Jahresmarktwert(jahr, pvDialog));
                if (fuehrend.HasValue)
                {
                    vpvCt = fuehrend.Value;
                    HinweisErgaenzen("PV-Vergütungsdialog führt die Einspeisevergütung: " +
                                     vpvCt.ToString("N2") + " ct/kWh (V4/F7).");
                }
            }
            catch { /* fuehrender Satz ist Komfort - der Lauf kippt daran nicht */ }
            e.VerguetungPvCtKwh = SpeicherEingang.KonstanteReihe(vpvCt, anzahlIntervalle);
            e.VerguetungBhkwCtKwh = SpeicherEingang.KonstanteReihe(aufschlagModel.Verguetung_BHKW, anzahlIntervalle);

            // --- Verkaufserloes (Fachkonzept 2.2 / 6.5, AP10) ------------------
            // Kopiert, nicht verwiesen: Die Reihe geht in die ArbitrageOptionen und
            // soll sich nicht mitaendern, wenn ein Aufrufer eine der anderen anfasst.
            e.ErloesCtKwh = e.Quelle == DbWerte.SP_PREISQUELLE_SPOTMARKT
                ? (double[])energiereihe.Clone()
                : (double[])e.VerguetungPvCtKwh.Clone();

            e.Hinweis = _hinweis;
            return e;
        }

        // =================================================================
        // Energiepreisreihe je Quelle (Fachkonzept 4.1)
        // =================================================================

        private double[] BaueEnergiereihe(int idProjekt, StromspeicherVarianteModel v,
                                          int anzahl, DateTime? stichtag, StromPreisErgebnis e)
        {
            if (v.Preisquelle == DbWerte.SP_PREISQUELLE_SPOTMARKT)
            {
                double[] spot = BaueSpotreihe(idProjekt, v, anzahl, e);
                if (spot != null) return spot;
            }
            else if (v.Preisquelle == DbWerte.SP_PREISQUELLE_PROFIL)
            {
                double[] profil = BaueProfilreihe(idProjekt, v, anzahl, e);
                if (profil != null) return profil;
            }

            return BaueFixpreisreihe(idProjekt, anzahl, stichtag, e);
        }

        /// <summary>
        /// (c) Fixpreis - der Bestandsfall: der zum Stichtag gueltige Arbeitspreis des
        /// Strom-Carriers als konstante Reihe.
        /// </summary>
        private double[] BaueFixpreisreihe(int idProjekt, int anzahl, DateTime? stichtag,
                                           StromPreisErgebnis e)
        {
            e.Quelle = DbWerte.SP_PREISQUELLE_FIXPREIS;

            int carrier = StromAufschlagCtrl.StromCarrierId(idProjekt);
            double preisCtKwh = ArbeitspreisCtKwh(idProjekt, carrier, Stichtag(stichtag, 0), e);

            return SpeicherEingang.KonstanteReihe(preisCtKwh, anzahl);
        }

        /// <summary>(b) Kostenprofil - 12 Monats- und 168 Wochenwerte (Fachkonzept 4.1 b).</summary>
        private double[] BaueProfilreihe(int idProjekt, StromspeicherVarianteModel v, int anzahl,
                                         StromPreisErgebnis e)
        {
            KostenprofilModel profil = null;
            if (v.ID_Kostenprofil > 0) profil = new KostenprofilCtrl().ReadSingle(v.ID_Kostenprofil);

            if (profil == null)
            {
                HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_KEIN_PROFIL);
                return null;
            }

            double[] monat = ZahlenAusZeichenkette(profil.Monatswerte, PreisModell.MonateJahr);
            double[] woche = ZahlenAusZeichenkette(profil.Wochenwerte, PreisModell.WochenwerteJahr);

            if (monat == null)
            {
                HinweisErgaenzen(string.Format(MyResource.Resource.PREIS_HINWEIS_PROFIL_UNBRAUCHBAR,
                                               profil.Bezeichner));
                return null;
            }

            double[] stunden = PreisModell.AusMonatsUndWochenwerten(monat, woche);
            double[] reihe = AufZiellaenge(PreisModell.ZuViertelstunden(stunden), anzahl);

            e.Quelle = DbWerte.SP_PREISQUELLE_PROFIL;
            e.Preisversion = string.Format(CultureInfo.InvariantCulture, "{0}: {1}",
                                           DbWerte.SP_PREISQUELLE_PROFIL, Gekuerzt(profil.Bezeichner, 38));
            return reihe;
        }

        /// <summary>(a) Spotmarktreihe aus <c>Tab_Preisreihe</c> (Fachkonzept 4.1 a).</summary>
        private double[] BaueSpotreihe(int idProjekt, StromspeicherVarianteModel v, int anzahl,
                                       StromPreisErgebnis e)
        {
            PreisreiheCtrl ctrl = new PreisreiheCtrl();

            PreisreiheModel kopf = v.ID_Preisreihe > 0
                ? ctrl.ReadSingle(v.ID_Preisreihe)
                : ctrl.ReadZumJahr(idProjekt, DateTime.Today.Year);

            if (kopf == null)
            {
                HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_KEINE_SPOTREIHE);
                return null;
            }

            double[] werte = ctrl.ReadWerte(kopf.ID);
            if (werte.Length != RasterAdapter.StundenJahr && werte.Length != RasterAdapter.ViertelstundenJahr)
            {
                HinweisErgaenzen(string.Format(MyResource.Resource.PREIS_HINWEIS_SPOTREIHE_LAENGE,
                                               kopf.Bezeichner, werte.Length));
                return null;
            }

            double[] reihe = AufZiellaenge(PreisModell.ZuViertelstunden(werte), anzahl);

            e.Quelle = DbWerte.SP_PREISQUELLE_SPOTMARKT;
            e.Preisversion = string.Format(CultureInfo.InvariantCulture, "{0} {1}",
                                           Gekuerzt(kopf.Bezeichner, 40), kopf.Jahr);
            return reihe;
        }

        // =================================================================
        // Preisversion (Fachkonzept 4.1)
        // =================================================================

        /// <summary>
        /// Stichtag der Preisversion: der 1. Januar des Simulationsjahres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Offener Punkt.</b> Ein ausdrueckliches Feld "Simulationsjahr" gibt es im
        /// Datenmodell nicht (<c>Tab_Projekt</c> fuehrt nur Erstell- und
        /// Aenderungsdatum). Solange es fehlt, gilt: das Jahr der gewaehlten Spotreihe,
        /// wenn eine vorliegt - sie ist ein festes Kalenderdatum und macht das Ergebnis
        /// reproduzierbar -, sonst das laufende Kalenderjahr, also das Planungsjahr.
        /// <c>Tab_Projekt.Aenderungsdatum</c> waere die schlechtere Wahl: Es wandert bei
        /// jedem Speichern und wuerde denselben Lauf morgen anders bepreisen.
        /// </para>
        /// <para>
        /// <b>1. Januar, nicht 31. Dezember.</b> Ein Jahreslauf bewertet das GANZE Jahr;
        /// der Preis, der zu seinem Beginn galt, ist der, mit dem geplant wurde.
        /// </para>
        /// </remarks>
        public static DateTime Stichtag(DateTime? vorgabe, int jahrDerReihe)
        {
            if (vorgabe.HasValue) return vorgabe.Value;
            int jahr = jahrDerReihe > 0 ? jahrDerReihe : DateTime.Today.Year;
            return new DateTime(jahr, 1, 1);
        }

        /// <summary>
        /// Der zum Stichtag gueltige Arbeitspreis des Strom-Carriers [ct/kWh] und die
        /// Bezeichnung seiner Version.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Stichtagsregel</b> (Fachkonzept 4.1): die juengste Version mit
        /// <c>valid_from &lt;= Stichtag</c>. Auf <c>valid_to</c> wird bewusst NICHT
        /// gefiltert - <c>ucFuelSettings.SaveProjectAndHistory</c> schreibt die Spalte
        /// nirgends, sie ist in jeder Zeile NULL. Eine Bedingung darauf haette also
        /// jede Version ausgeschlossen. Fachlich ist das gleichwertig: Eine Preisreihe
        /// ohne Endedatum gilt bis zur naechsten, und genau die waehlt
        /// <c>ORDER BY valid_from DESC</c>.
        /// </para>
        /// <para>
        /// <b>Rueckfallkette</b>, jede Stufe protokolliert: (1) juengste Version bis zum
        /// Stichtag, (2) aelteste Version ueberhaupt - besser ein spaeterer Preis als
        /// gar keiner, (3) <c>energy_project_settings.custom_price_work</c>, (4) der
        /// Katalogpreis <c>energy_carrier.price_work</c>, (5) der Platzhalter aus AP2b.
        /// </para>
        /// </remarks>
        private double ArbeitspreisCtKwh(int idProjekt, int carrierId, DateTime stichtag,
                                         StromPreisErgebnis e)
        {
            if (carrierId <= 0)
            {
                e.Preisversion = "";
                HinweisErgaenzen(string.Format(MyResource.Resource.PREIS_HINWEIS_RUECKFALL_FIXPREIS,
                                               Anzeige(FIXPREIS_RUECKFALL_CT_KWH)));
                return FIXPREIS_RUECKFALL_CT_KWH;
            }

            // (1) und (2): die Preishistorie
            DataTable dt = DataRepository.GetDataTable(
                "SELECT valid_from, arbeitspreis FROM energy_price " +
                "WHERE carrier_id = ? AND id_projekt = ? AND valid_from <= ? " +
                "ORDER BY valid_from DESC LIMIT 1",
                new DbParam("@cid", DbParamTyp.Integer) { Wert = carrierId },
                new DbParam("@pid", DbParamTyp.Integer) { Wert = idProjekt },
                new DbParam("@date", DbParamTyp.Date) { Wert = stichtag });

            if (dt == null || dt.Rows.Count == 0)
            {
                dt = DataRepository.GetDataTable(
                    "SELECT valid_from, arbeitspreis FROM energy_price " +
                    "WHERE carrier_id = ? AND id_projekt = ? ORDER BY valid_from ASC LIMIT 1",
                    new DbParam("@cid", DbParamTyp.Integer) { Wert = carrierId },
                    new DbParam("@pid", DbParamTyp.Integer) { Wert = idProjekt });

                if (dt != null && dt.Rows.Count > 0)
                    HinweisErgaenzen(string.Format(MyResource.Resource.PREIS_HINWEIS_VERSION_SPAETER,
                                                   stichtag.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)));
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                double eurProKwh = Kommazahl(r["arbeitspreis"]);
                DateTime gueltigAb = Datum(r["valid_from"], stichtag);
                double ctKwh = eurProKwh * EUR_JE_KWH_IN_CT;

                e.Preisversion = string.Format(CultureInfo.InvariantCulture, "{0} / {1} ct/kWh",
                                               gueltigAb.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                                               ctKwh.ToString("0.000", CultureInfo.InvariantCulture));

                if (ctKwh > 0.0) return ctKwh;

                HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_ARBEITSPREIS_NULL);
            }

            // (3) Projekteinstellung
            double custom = Skalar("SELECT custom_price_work FROM energy_project_settings " +
                                   "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                                   idProjekt, carrierId) * EUR_JE_KWH_IN_CT;
            if (custom > 0.0)
            {
                e.Preisversion = MyResource.Resource.PREIS_VERSION_PROJEKTEINSTELLUNG;
                HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_OHNE_HISTORIE);
                return custom;
            }

            // (4) Katalogpreis
            object k = DataRepository.ExecuteScalar(
                "SELECT price_work FROM energy_carrier WHERE id = ?",
                new DbParam("@cid", carrierId));
            double katalog = Kommazahl(k) * EUR_JE_KWH_IN_CT;
            if (katalog > 0.0)
            {
                e.Preisversion = MyResource.Resource.PREIS_VERSION_KATALOG;
                HinweisErgaenzen(MyResource.Resource.PREIS_HINWEIS_KATALOGPREIS);
                return katalog;
            }

            // (5) Platzhalter
            e.Preisversion = "";
            HinweisErgaenzen(string.Format(MyResource.Resource.PREIS_HINWEIS_RUECKFALL_FIXPREIS,
                                           Anzeige(FIXPREIS_RUECKFALL_CT_KWH)));
            return FIXPREIS_RUECKFALL_CT_KWH;
        }

        // =================================================================
        // Kleinigkeiten
        // =================================================================

        /// <summary>
        /// Zerlegt eine <c>";"</c>-Zeichenkette in genau <paramref name="anzahl"/>
        /// Zahlen (Ablageformat von <c>Form_Quellprofil</c>, <see cref="CultureInfo.InvariantCulture"/>).
        /// </summary>
        /// <returns>
        /// Das Array, oder <c>null</c> wenn die Zeichenkette leer ist oder zu wenige
        /// Teile hat. Fehlende Einzelwerte am Ende sind 0 - ein nicht gepflegter
        /// Wochenwert ist "keine Abweichung", kein Fehler.
        /// </returns>
        public static double[] ZahlenAusZeichenkette(string text, int anzahl)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string[] teile = text.Split(';');
            if (teile.Length < anzahl) return null;

            double[] werte = new double[anzahl];
            for (int i = 0; i < anzahl; i++)
            {
                double w;
                if (double.TryParse(teile[i], NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                    werte[i] = w;
            }
            return werte;
        }

        /// <summary>
        /// Bringt eine Reihe auf die Ziellaenge: kuerzen oder mit dem letzten Wert
        /// auffuellen. Nur fuer den Schaltjahresfall der Engine (35.136 statt 35.040)
        /// und fuer Testlaengen - im Produktivfall stimmt die Laenge bereits.
        /// </summary>
        private static double[] AufZiellaenge(double[] reihe, int anzahl)
        {
            if (reihe.Length == anzahl) return reihe;

            double[] ziel = new double[anzahl];
            for (int i = 0; i < anzahl; i++)
                ziel[i] = reihe[i < reihe.Length ? i : reihe.Length - 1];
            return ziel;
        }

        private void HinweisErgaenzen(string hinweis)
        {
            if (string.IsNullOrEmpty(hinweis)) return;
            _hinweis = string.IsNullOrEmpty(_hinweis) ? hinweis : _hinweis + Environment.NewLine + hinweis;
        }

        private static double Skalar(string sql, int idProjekt, int carrierId)
        {
            object v = DataRepository.ExecuteScalar(sql,
                new DbParam("@pid", idProjekt),
                new DbParam("@cid", carrierId));
            return Kommazahl(v);
        }

        private static double Kommazahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0.0;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return 0.0; }
        }

        private static DateTime Datum(object o, DateTime vorgabe)
        {
            if (o == null || o == DBNull.Value) return vorgabe;
            try { return Convert.ToDateTime(o, CultureInfo.InvariantCulture); }
            catch { return vorgabe; }
        }

        private static string Anzeige(double d)
        {
            return d.ToString("0.###", CultureInfo.CurrentCulture);
        }

        /// <summary>Kuerzt einen Bezeichner auf die Feldbreite von <c>Preisversion</c> (TEXT(50)).</summary>
        private static string Gekuerzt(string text, int laenge)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= laenge ? text : text.Substring(0, laenge);
        }
    }
}
