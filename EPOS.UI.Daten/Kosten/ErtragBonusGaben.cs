using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EPOS.UI.Dialoge.Allgemein;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Abschnitts „Ertrag/Bonus" (iU9-W4.1/W4.2) — Nachfolge
    /// der gelöschten <c>Views/Kosten/ucErtragBonus.cs</c> (217 Z.), Etappe KD5,
    /// Konzept Kostendialoge § 6.
    ///
    /// <para><b>Was hierher gehört und was nicht.</b> Der Abschnitt ist reine
    /// ANZEIGE vorhandener Wahrheiten; die Zahlen kommen aus DENSELBEN
    /// Katalogschlüsseln, mit denen der <c>KwkgSatzRechner</c> rechnet
    /// (Abnahmekriterium KD5). Das Lesen des <see cref="GesetzKatalog"/> ist
    /// Datenseite und steht deshalb hier — die Razor-Komponente
    /// <c>ErtragBonus</c> bekommt die fertigen Sätze.</para>
    ///
    /// <para><b>Der Gesetzeskatalog ist seit iU9-W14c.3 eine Ueberlagerung.</b> Bis
    /// dahin sprang das Reiterblatt über die Sprungbrücke (iU9-W2.2) in das
    /// WinForms-Fenster <c>Form_Gesetzesparameter</c>. Das Ziel ist jetzt selbst eine
    /// Razor-Komponente; der Wunsch geht deshalb als <c>GesetzeGewuenscht</c> an den
    /// Wirt <c>KostenKomponenteDialog</c>, der den Katalog im selben Fenster zeigt
    /// (Risiko R2) und danach diese Gaben neu bauen lässt.</para>
    /// </summary>
    internal static class ErtragBonusGaben
    {
        /// <summary>FK5: nur BHKW und Photovoltaik führen den Abschnitt.</summary>
        internal static bool HatInhalt(string komponente)
        {
            return string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_BHKW, StringComparison.Ordinal) ||
                   string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Parameter der Komponente für die gewählte Komponente.
        /// </summary>
        /// <param name="idProjekt">
        /// Das GEÖFFNETE Projekt; <c>0</c> = Admin-Kontext (Kostendialog ohne Projekt).
        /// Es entscheidet über die Vergütungswahl (Konzept § 2.16): Nur eine Variante
        /// hat eine Wahl, und nur ohne Projekt erscheint die Klappliste (VV‑Q7).
        /// </param>
        internal static IReadOnlyDictionary<string, object> Bauen(string komponente, int idProjekt = 0)
        {
            bool bhkw = string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_BHKW,
                                      StringComparison.Ordinal);
            bool pv = string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK,
                                    StringComparison.Ordinal);

            var werte = new Dictionary<string, object>
            {
                ["IstBhkw"] = bhkw,
                ["IstPv"] = pv,
                ["TitelKwkg"] = T("KDLG_ERTRAG_G_KWKG",
                    "KWKG-Zuschlag (§ 7 KWKG 2025) — Anzeige aus dem Gesetzeskatalog"),
                ["TitelEinspeisung"] = T("KDLG_ERTRAG_EINSP_TITEL",
                    "Eingespeister KWK-Strom (Tranchen):"),
                ["TitelDauer"] = T("KDLG_ERTRAG_G_DAUER", "Förderdauer und Jahresdeckel"),
                ["TitelSteuern"] = T("KDLG_ERTRAG_G_STEUERN",
                    "Steuervergünstigungen (HF6, Sätze aus dem Gesetzeskatalog)"),
                ["TitelVerweise"] = T("KDLG_ERTRAG_G_VERWEISE", "Pflegeorte (eine Wahrheit je Größe)"),
                ["GesetzeText"] = T("KDLG_ERTRAG_BTN_GESETZE", "Gesetzesparameter…"),
                ["TitelPv"] = T("KDLG_ERTRAG_G_PV",
                    "PV-Vergütung (EEG) — eine Vergütungswahrheit (V4/F7)"),
                ["LabelPvProjekt"] = T("KDLG_ERTRAG_PV_PROJEKT", "Projekt:"),
                ["LabelPvWahl"] = T("KDLG_ERTRAG_PV_WAHL", "Vergütung:"),
                ["UebernehmenText"] = T("KDLG_ERTRAG_PV_UEBERNEHMEN", "vom Stammprojekt übernehmen"),
                ["EigeneText"] = T("KDLG_ERTRAG_PV_EIGENE", "eigene Vergütung"),
                ["PvOeffnenText"] = T("KDLG_ERTRAG_PV_OEFFNEN", "PV-Vergütungsdialog öffnen…"),
                ["LeerText"] = T("KDLG_ERTRAG_LEER",
                    "Diese Komponente führt keine laufenden Erträge — Förderungen/Zuschüsse "
                    + "laufen als Zuschuss-Position in den Investitionskosten (FK5).")
            };

            if (bhkw) BhkwFuellen(werte);
            if (pv) PvFuellen(werte, idProjekt);
            return werte;
        }

        // =====================================================================
        // BHKW (§ 6.1)
        // =====================================================================

        /// <summary>
        /// Werte aus DENSELBEN Katalogschlüsseln, die der KwkgSatzRechner liest —
        /// wortgleich aus <c>ucErtragBonus.BhkwFuellen</c>.
        /// </summary>
        private static void BhkwFuellen(Dictionary<string, object> werte)
        {
            GesetzKatalog.StelleKatalogSicher();
            GesetzKatalog k = new GesetzKatalog();
            int jahr = DateTime.Now.Year;
            CultureInfo ci = CultureInfo.CurrentCulture;

            Func<string, string> w = schluessel =>
            {
                double? wert = k.Wert(schluessel, jahr);
                return wert.HasValue ? wert.Value.ToString("0.0#", ci) : "—";
            };

            werte["EinspeisungText"] =
                Z("KDLG_ERTRAG_T50", "bis 50 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW)) +
                Z("KDLG_ERTRAG_T100", "über 50 bis 100 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW)) +
                Z("KDLG_ERTRAG_T250", "über 100 bis 250 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW)) +
                Z("KDLG_ERTRAG_T2MW", "über 250 kW bis 2 MW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW)) +
                Z("KDLG_ERTRAG_UE2MW", "über 2 MW (neu/modernisiert)", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW)) +
                Z("KDLG_ERTRAG_UE2MWN", "über 2 MW (nachgerüstet)", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW_NACHGER));

            werte["SonderregelText"] = string.Format(ci,
                T("KDLG_ERTRAG_SONDERREGEL",
                  "Sonderregel neue Anlagen ≤ 50 kWel (§ 7 Abs. 3a): eingespeist {0} · " +
                  "nicht eingespeist {1} ct/kWh — geht Abs. 1 und 2 vor."),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN));

            werte["EigenText"] = string.Format(ci,
                T("KDLG_ERTRAG_EIGEN",
                  "Selbst genutzter KWK-Strom (§ 7 Abs. 2, nur in den Tatbeständen des " +
                  "§ 6 Abs. 3 — z. B. Anlage ≤ 100 kW): bis 50 kW {0} · 50–100 kW {1} ct/kWh. " +
                  "Tatbestand und Anlagenart werden JE ANLAGE in der Wirtschaftlichkeit " +
                  "gepflegt (KWKG-Module) — hier keine Zweitpflege."),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW));

            // Dauer: Vollbenutzungsstunden-Kontingent + Jahresdeckel-Reihe.
            StringBuilder deckel = new StringBuilder();
            foreach (KeyValuePair<int, double> p in k.Reihe(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL))
                deckel.AppendFormat(ci, "{0}: {1:N0} · ", p.Key, p.Value);
            double? vbh = k.Wert(DbWerte.GESETZ_KWKG_VBH_NEUANLAGE, jahr);
            werte["DauerText"] = string.Format(ci,
                T("KDLG_ERTRAG_DAUER",
                  "Neue Anlagen: {0} Vollbenutzungsstunden Förderkontingent. " +
                  "Jahresdeckel [Vbh/a]: {1}(Kontingent-Override je Anlage in der Wirtschaftlichkeit)."),
                vbh.HasValue ? vbh.Value.ToString("N0", ci) : "—", deckel.ToString());

            werte["SteuernText"] =
                string.Format(ci, T("KDLG_ERTRAG_ST_BEFREIUNG",
                    "Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 StromStG: hocheffiziente Anlagen " +
                    "≤ 2 MW im räumlichen Zusammenhang (4,5 km); ab 2026 CO₂-Kriterium.")) + "\n\n" +
                string.Format(ci, T("KDLG_ERTRAG_ST_53A",
                    "Energiesteuer-Entlastung § 53a Abs. 5 EnergieStG: Erdgas {0} €/MWh · " +
                    "Heizöl {1} €/1.000 l · Flüssiggas {2} €/1.000 kg (Mindestnutzungsgrad {3} %)."),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_HEIZOEL_EL),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_FLUESSIGGAS),
                    w(DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD)) + "\n\n" +
                string.Format(ci, T("KDLG_ERTRAG_ST_9B",
                    "Stromsteuer-Entlastung § 9b StromStG: {0} €/MWh, Sockelbetrag {1} €/a."),
                    w(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B),
                    w(DbWerte.GESETZ_STROMST_SOCKELBETRAG_9B));

            werte["Fk7Text"] = T("KDLG_ERTRAG_FK7",
                "FK7: Der STROMPREIS-Teil der BHKW-Einspeisevergütung bleibt in der " +
                "Tarifstruktur des Projekts (Einsp_* ist rein KWK) — dieser Reiter zeigt " +
                "die gesetzlichen KWKG-/Steuergrößen an; gerechnet wird ausschließlich vom " +
                "KwkgSatzRechner und den Steuer-Gutschriftrechnern der Wirtschaftlichkeit. " +
                "Projektbezogene Schalter (Tatbestand, Anlagenart, Pauschalmodus § 9, " +
                "Kontingent-Override) werden dort je Anlage gepflegt.");

            // iU9-W14c.3: Bis hierher stand hier Sprungbruecke.Fuer(null) und die
            // Komponente sprang mit Sprungziel.Gesetzesparameter in ein
            // WinForms-Fenster. Der Katalog ist jetzt selbst eine Razor-Komponente;
            // ErtragBonus ist ein REITERBLATT und kann keine Ueberlagerung oeffnen -
            // es meldet den Wunsch als GesetzeGewuenscht nach oben, und
            // KostenKomponenteDialog zeigt den Katalog (Risiko R2). Der Wirt laedt
            // danach die Gaben dieses Reiterblatts neu, wie es btnGesetze_Click mit
            // BhkwFuellen tat.
        }

        private static string Z(string schluessel, string rueckfall, string wert)
        {
            string beschriftung = T(schluessel, rueckfall);
            return (beschriftung + ":").PadRight(34) + wert.PadLeft(6) + " ct/kWh\n";
        }

        // =====================================================================
        // Photovoltaik (§ 6.2)
        // =====================================================================

        private static void PvFuellen(Dictionary<string, object> werte, int idProjekt)
        {
            werte["PvErklaerungText"] = T("KDLG_ERTRAG_PV",
                "Die PV-Vergütung wird je Projekt im Vergütungsdialog gepflegt — " +
                "demselben Formular, das auch der Knopf „Photovoltaik…\" im " +
                "Wirtschaftlichkeits-Reiter öffnet. Eine Variante übernimmt die " +
                "Vergütung ihres Stammprojekts oder führt eigene Werte. " +
                "Anzulegender Wert, Vermarktungsform, § 51/§ 51a und 60-%-Begrenzung " +
                "wirken über die PV-Erlösreihe direkt in der Kapitalwertrechnung.");

            var eintraege = new List<ValueTuple<int, string>>();
            foreach (KeyValuePair<int, string> p in KostenVorlagenUebernahmeCtrl.Projekte())
                eintraege.Add(new ValueTuple<int, string>(p.Key, p.Value));
            werte["Projekte"] = (IReadOnlyList<ValueTuple<int, string>>)eintraege;

            // KONZEPT § 2.16 — die Wahl je Variante. Im PROJEKTMODUS steht der Stand
            // fest: Das geöffnete Projekt ist vorgewählt, die Klappliste entfällt
            // (VV‑Q7), und nur eine Variante bekommt die Optionsgruppe. Im
            // Admin-Kontext (idProjekt = 0) bleibt alles wie zuvor — dort gibt es
            // keinen Stand, über dessen Vergütung zu entscheiden wäre.
            bool projektModus = idProjekt > 0;
            werte["ProjektlisteZeigen"] = !projektModus;
            if (projektModus) werte["ProjektVorwahl"] = (int?)idProjekt;

            int idStamm = projektModus ? new VariantenCtrl().StammRefDerVariante(idProjekt) : -1;
            bool istVariante = idStamm > 0 && idStamm != idProjekt;
            werte["IstVariante"] = istVariante;

            var pvc = new ProjektPhotovoltaikCtrl();
            if (istVariante)
            {
                PvVerguetungStand stand = pvc.LiesAufgeloest(idProjekt);
                werte["Uebernommen"] = stand.Uebernommen;
                werte["HerkunftText"] = HerkunftZeile(stand, idStamm);
                werte["WahlGeaendert"] = EventCallback.Factory.Create<bool>(new object(),
                    uebernehmen => WahlSchreiben(idProjekt, uebernehmen));
            }
            else if (projektModus)
            {
                werte["Uebernommen"] = false;
                werte["HerkunftText"] = StammZeile(idProjekt);
            }

            // iU9-W2.4: der PV-Vergütungsdialog ist selbst eine Blazor-Hülle. Zwei
            // WebViews übereinander sind Risiko R2 — der Sprung bleibt deshalb
            // NACHGELAGERT (Muster BhkwWirtschaftlichkeitHuelle.TarifOeffnen): Der
            // Kostendialog steht weiter, der Vergütungsdialog legt sich als
            // eigenes Fenster darüber. Beim Anfassen von Welle 5 wird daraus eine
            // Überlagerung.
            //
            // E3/1: Weil der Vergütungsdialog bis E3 Schritt 6 ein eigenes Fenster
            // zeigt, bleibt seine Hülle in der Windows-Schale; der Sprung kommt als
            // benannte Naht herein (Wirtschaftlichkeitswege.PvVerguetungOeffnen).
            Action<int> pvOeffnen = Wirtschaftlichkeitswege.PvVerguetungOeffnen;
            if (pvOeffnen != null)
                werte["PvOeffnen"] = EventCallback.Factory.Create<int>(new object(),
                    id => pvOeffnen(id));
        }

        /// <summary>
        /// Die Erklärzeile der Variante (Konzept § 2.16): „übernommen von ‹Stamm› ·
        /// anzulegender Wert ‹AW› ct/kWh, ‹Vermarktungsform›" oder „eigene Werte dieser
        /// Variante".
        ///
        /// <para>Der anzulegende Wert steht nur da, wenn er GEPFLEGT ist: Ohne Override
        /// leitet ihn der <c>EegSatzRechner</c> aus dem Katalog her, und diese Zeile
        /// rechnet nicht — sie sagt, woher die Angaben kommen.</para>
        /// </summary>
        private static string HerkunftZeile(PvVerguetungStand stand, int idStamm)
        {
            if (!stand.Uebernommen)
                return T("KDLG_ERTRAG_PV_HERK_EIGEN", "eigene Werte dieser Variante");

            string name = StartseiteCtrl.Projektname(idStamm) ?? "";
            string aw = stand.Modell != null && stand.Modell.AwOverride.HasValue
                ? stand.Modell.AwOverride.Value.ToString("0.##", CultureInfo.CurrentCulture)
                : "—";
            string form = stand.Modell != null
                ? WirtschaftlichkeitZeilen.PvFormAnzeige(stand.Modell.Vermarktungsform) : "—";

            return string.Format(CultureInfo.CurrentCulture,
                T("KDLG_ERTRAG_PV_HERK_STAMM",
                  "übernommen von {0} · anzulegender Wert {1} ct/kWh, {2}"),
                name, aw, form);
        }

        /// <summary>
        /// Die Zeile des STAMMPROJEKTS: „Stammprojekt — ‹n› Varianten übernehmen diese
        /// Vergütung". Gezählt wird über die Auflösung, nicht über die Spalte allein —
        /// „keine Zeile" heißt ebenfalls übernehmen.
        /// </summary>
        private static string StammZeile(int idStamm)
        {
            int n = 0;
            try
            {
                var pvc = new ProjektPhotovoltaikCtrl();
                foreach (VariantenCtrl.VarianteInfo v in
                         new VariantenCtrl().LadeGruppe(idStamm, StartseiteCtrl.Projektname(idStamm)))
                {
                    if (v.IstStamm || v.IdProjekt == idStamm) continue;
                    if (pvc.LiesAufgeloest(v.IdProjekt).Uebernommen) n++;
                }
            }
            catch { }

            return string.Format(CultureInfo.CurrentCulture,
                T("KDLG_ERTRAG_PV_STAMM_N",
                  "Stammprojekt — {0} Variante(n) übernehmen diese Vergütung"), n);
        }

        /// <summary>
        /// Die Wahl schreiben (Konzept § 2.16). „Eigene Vergütung" legt die STAMMWERTE
        /// vor (VV‑Q6) und öffnet danach den Vergütungsdialog für diese Variante — der
        /// Anwender will die Abweichung gleich eingeben, nicht erst einen zweiten Knopf
        /// suchen. „Übernehmen" lässt die Zeile stehen: Sie ist der Rückweg.
        /// </summary>
        private static void WahlSchreiben(int idProjekt, bool uebernehmen)
        {
            var pvc = new ProjektPhotovoltaikCtrl();
            if (uebernehmen)
            {
                pvc.SetzeUebernahme(idProjekt, true);
                return;
            }

            if (pvc.Lies(idProjekt) == null) pvc.Speichern(pvc.VorlageAusStamm(idProjekt));
            else pvc.SetzeUebernahme(idProjekt, false);

            // E3/1: derselbe Weg wie am Knopf — über die benannte Naht, weil die
            // gerufene Hülle bis E3 Schritt 6 ein eigenes Fenster zeigt.
            Wirtschaftlichkeitswege.PvVerguetungOeffnen?.Invoke(idProjekt);
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
