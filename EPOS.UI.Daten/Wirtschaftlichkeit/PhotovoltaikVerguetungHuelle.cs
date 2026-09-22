using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Hülle des Dialogs „PV-Vergütung (EEG)" (iU9-W2.4).
    ///
    /// <para><b>Seit Etappe E3, Schritt 6 liegt sie in <c>EPOS.UI.Daten</c>,
    /// und sie hat keine Fensterhälfte mehr.</b> Der Dialog erscheint
    /// ausschließlich als <c>Ueberlagerung</c> — auf der
    /// Wirtschaftlichkeitsseite und im Reiterblatt „Ertrag/Bonus" der
    /// Kostenverwaltung. Das zweite WinForms-Fenster, das der Knopf des
    /// Reiterblatts bis dahin hochfuhr, war Risiko R2 und ist ersatzlos
    /// weg.</para>
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <see cref="PhotovoltaikVerguetungDialog"/> in <c>EPOS.UI</c>; die
    /// WinForms-Fassung <c>Form_PhotovoltaikVerguetung</c> ist mit demselben
    /// Schritt GELÖSCHT (Regel M1).</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Alles, was der Dialog zeigt, wird
    /// hier geladen — mit denselben Controllern und in derselben Reihenfolge wie
    /// zuvor in <c>SetControls(idStamm)</c>: Vergütungssatz, rechnerische
    /// Leistung, Mengen des letzten Laufs, PV-Kosten aus der Kostenwelt,
    /// Strompreis und Wirtschaftlichkeitsparameter. Geschrieben wird über
    /// <c>ProjektPhotovoltaikCtrl.Speichern</c>.</para>
    ///
    /// <para><b>Ein Weg nach draußen.</b> Der Marktwert-Import wählt seine
    /// Datei seit E3 Schritt 2 über <c>Dienste.Datei.DateiOeffnenAsync</c> —
    /// plattformfrei, hinter dem Blazor-Ereignis, ohne Fensterbesitzer (Muster
    /// <see cref="SpotpreisImportHuelle"/>). Den Sprung in den Tarifdialog
    /// meldet die Komponente in ihrem Ergebnis (<c>PvSprung</c>); ihn bedient der
    /// WIRT der Überlagerung, nicht diese Hülle (E3 Schritt 7).</para>
    /// </summary>
    internal static class PhotovoltaikVerguetungHuelle
    {
        /// <summary>Bereichstitel — derselbe Text wie in der Komponente.</summary>
        internal static string Titel()
        {
            return new PhotovoltaikVerguetungTexte().Titel;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// er in einer <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe
        /// WebView (Risiko R2). Dasselbe tut seit E3 Schritt 6 das Reiterblatt
        /// „Ertrag/Bonus" der Kostenverwaltung. <c>Geschlossen</c> setzt der
        /// Wirt; den Sprung in den Tarifdialog wertet er selbst aus
        /// (<c>PvSprung</c>).
        ///
        /// <para>Seit E3 Schritt 2 braucht dieser Satz KEINEN Fensterbesitzer
        /// mehr: Der Marktwert-Import wählt über <c>Dienste.Datei</c>.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idStamm)
        {
            var ctrl = new ProjektPhotovoltaikCtrl();
            var katalog = new GesetzKatalog();

            // KONZEPT § 2.16 — der Dialog zeigt die Zeile, die für diesen Stand GILT.
            // Übernimmt eine Variante, sind das die Werte ihres Stamms; sie bekommt
            // dann die Hinweiszeile und den Knopf „eigene Werte" (VV‑Q3/VV‑Q6). Die
            // Wahl selbst trifft der Reiter Ertrag/Bonus.
            PvVerguetungStand stand = ctrl.LiesAufgeloest(idStamm);
            ProjektPhotovoltaikModel modell = stand.Uebernommen && stand.Modell != null
                ? Uebernahmesicht(stand.Modell, idStamm)
                : ctrl.LiesOderVorbelegt(idStamm);
            double kwpRechnerisch = PhotovoltaikCtrl.KwpDesProjekts(idStamm);

            double einspeisungMWh = 0, erzeugungMWh = 0, bedarfMWh = 0;
            double? evQuote = null, autarkie = null;
            try
            {
                ErgebnisModel erg = new ErgebnisCtrl().Load(idStamm);
                if (erg != null && erg.Photovoltaik != null)
                {
                    einspeisungMWh = erg.Photovoltaik.Ueberschuss;
                    erzeugungMWh = erg.Photovoltaik.Stromproduktion;
                    bedarfMWh = erg.Photovoltaik.Strombedarf;
                }
                // Quoten MIT Speicher aus der Speicherrechnung (N.3: stets als
                // Paar); bei mehreren Anlagen die erste Zeile mit Werten.
                if (erg != null && erg.Stromspeicher != null)
                    foreach (ErgebnisStromspeicherModel sp in erg.Stromspeicher)
                        if (sp.Eigenverbrauchsquote > 0 || sp.Autarkiegrad > 0)
                        {
                            evQuote = sp.Eigenverbrauchsquote;
                            autarkie = sp.Autarkiegrad;
                            break;
                        }
            }
            catch { }

            // PV-Kosten aus der Kostenwelt (dieselbe Leselogik wie Bericht und
            // Kostendialog); Betrieb: fehlende Zeile bleibt null (nicht 0).
            double? investPv = null, betriebPv = null;
            try
            {
                investPv = KomponentenSumme(idStamm, KostenSummenCtrl.KATEGORIE_INVESTITION);
                betriebPv = KomponentenSumme(idStamm, KostenSummenCtrl.KATEGORIE_BETRIEB);
            }
            catch { }

            double? strompreis = null;
            try { strompreis = WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh(idStamm); }
            catch { }

            WirtschaftlichkeitParameter wirt = null;
            try { wirt = new WirtschaftlichkeitCtrl().LadeParameter(idStamm); }
            catch { }

            return new Dictionary<string, object>
            {
                ["Modell"] = modell,
                ["KwpRechnerisch"] = kwpRechnerisch,
                ["EinspeisungMWh"] = einspeisungMWh,
                ["ErzeugungMWh"] = erzeugungMWh,
                ["BedarfMWh"] = bedarfMWh,
                ["EvQuoteSpeicher"] = evQuote,
                ["AutarkieSpeicher"] = autarkie,
                ["InvestPv"] = investPv,
                ["BetriebPv"] = betriebPv,
                ["StrompreisEurKwh"] = strompreis,
                ["WirtParameter"] = wirt,

                // Die beiden Kataloge als DELEGAT - dieselbe Uebergabe, die
                // EegSatzRechner und PvErloesRechner selbst verlangen (L9).
                ["Katalog"] = new Func<string, int, double?>(katalog.Wert),
                ["Jahresmarktwert"] = new Func<int, double?>(jahr => ctrl.Jahresmarktwert(jahr, modell)),

                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.Speichern(modell); }
                    catch { return false; }
                }),

                ["MarktwerteImportieren"] = new Func<Task<MarktwertImport>>(
                    () => MarktwerteImportieren(ctrl)),

                // KONZEPT § 2.16 — Herkunft und der eine Weg heraus.
                ["Uebernommen"] = stand.Uebernommen,
                ["HerkunftText"] = HerkunftZeile(stand, idStamm),
                ["EigeneWerte"] = stand.Uebernommen
                    ? EventCallback.Factory.Create(new object(),
                        () => ctrl.Speichern(ctrl.VorlageAusStamm(idStamm)))
                    : default(EventCallback)
            };
        }

        /// <summary>
        /// Die ÜBERNOMMENE Zeile, wie der Dialog sie zeigen soll: die Werte des Stamms,
        /// aber auf dieses Projekt gemünzt (Konzept § 2.16).
        ///
        /// <para><b>Warum die Kopie und nicht die Stammzeile selbst.</b> „Übernehmen"
        /// des Dialogs schreibt <c>ProjektPhotovoltaikCtrl.Speichern</c> mit der
        /// <c>ID_Projekt</c> des Modells. Gäbe die Hülle die Stammzeile unverändert
        /// heraus, schriebe ein Klick die Änderung in den STAMM zurück — und damit in
        /// jede andere übernehmende Variante. Die Kopie trägt dieses Projekt und das
        /// Kennzeichen „eigene Werte": Wer hier speichert, hat sich für eigene Werte
        /// entschieden, und genau das entsteht.</para>
        /// </summary>
        private static ProjektPhotovoltaikModel Uebernahmesicht(ProjektPhotovoltaikModel stamm,
                                                                int idProjekt)
        {
            ProjektPhotovoltaikModel kopie = ProjektPhotovoltaikCtrl.Kopie(stamm);
            kopie.ID = 0;
            kopie.ID_Projekt = idProjekt;
            kopie.UebernahmeStamm = false;
            return kopie;
        }

        /// <summary>
        /// Die Hinweiszeile des Dialogs (Konzept § 2.16): „Vergütung dieser Variante:
        /// eigene Werte", „übernommen vom Stammprojekt ‹Name›" oder — beim Stamm —
        /// „Stammprojekt — ‹n› Varianten übernehmen diese Vergütung".
        /// </summary>
        private static string HerkunftZeile(PvVerguetungStand stand, int idProjekt)
        {
            if (stand.Uebernommen)
                return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    Text("PVW_HERKUNFT_STAMM",
                         "Vergütung dieser Variante: übernommen vom Stammprojekt „{0}“ — " +
                         "die Felder zeigen dessen Werte. „Eigene Werte“ übernimmt sie in " +
                         "diese Variante."),
                    StartseiteCtrl.Projektname(stand.IdQuelle) ?? "");

            int idStamm;
            try { idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt); }
            catch { idStamm = -1; }

            if (idStamm > 0 && idStamm != idProjekt)
                return Text("PVW_HERKUNFT_EIGEN", "Vergütung dieser Variante: eigene Werte");

            int n = 0;
            try
            {
                var pvc = new ProjektPhotovoltaikCtrl();
                foreach (VariantenCtrl.VarianteInfo v in
                         new VariantenCtrl().LadeGruppe(idProjekt, StartseiteCtrl.Projektname(idProjekt)))
                {
                    if (v.IstStamm || v.IdProjekt == idProjekt) continue;
                    if (pvc.LiesAufgeloest(v.IdProjekt).Uebernommen) n++;
                }
            }
            catch { }

            return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                Text("PVW_HERKUNFT_STAMMPROJEKT",
                     "Stammprojekt — {0} Variante(n) übernehmen diese Vergütung"), n);
        }

        /// <summary>
        /// P6 (Konzept 6.3): netztransparenz-CSV in die Marktwert-Stammreihen.
        /// <c>null</c> = der Anwender hat die Dateiauswahl abgebrochen.
        ///
        /// <para><b>Die Dateiwahl läuft über <c>Dienste.Datei</c></b> (Etappe E3,
        /// Schritt 2 — Befund P4) und nicht mehr über einen
        /// <c>OpenFileDialog</c>; Muster <see cref="SpotpreisImportHuelle"/>.
        /// Die <c>…Async</c>-Form legt den Wähler HINTER das Blazor-Ereignis
        /// (Regel (b) der Hüllenschicht, Befund W13‑B‑1) — damit braucht der
        /// Import keinen Fensterbesitzer mehr, und derselbe Aufruf beantwortet
        /// auf iOS der FilePicker.</para>
        /// </summary>
        private static async Task<MarktwertImport> MarktwerteImportieren(ProjektPhotovoltaikCtrl ctrl)
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                Text("PVW_BTN_MARKTWERTE", "Marktwerte importieren…"),
                Text("PVW_IMPORT_FILTER", "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*"),
                null);

            // Dienste.Datei liefert "" bei Abbruch - dieselbe Pruefung wie in
            // SpotpreisImportHuelle.
            if (string.IsNullOrEmpty(pfad)) return null;

            string bericht;
            bool ok = ctrl.ImportiereMarktwerteCsv(pfad, out bericht);
            return new MarktwertImport(ok, bericht ?? "");
        }

        /// <summary>Summe der PV-Komponente einer Kostenkategorie; null = keine Zeile.</summary>
        private static double? KomponentenSumme(int idProjekt, int kategorie)
        {
            DataTable dt = KostenSummenCtrl.LiesKomponentenSummen(idProjekt, kategorie);
            if (dt == null) return null;
            foreach (DataRow r in dt.Rows)
            {
                if (!string.Equals(Convert.ToString(r["Komponente"]),
                                   DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal))
                    continue;
                return r["Summe"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["Summe"]);
            }
            return null;
        }

        /// <summary>Anzeigetext mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
