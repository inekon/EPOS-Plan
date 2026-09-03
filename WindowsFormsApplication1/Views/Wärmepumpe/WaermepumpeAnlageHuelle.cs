using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Wärmepumpen-ANLAGE (iU9-W7.4) — der Ersatz für
    /// <c>Wizard_WPItem</c>.
    ///
    /// <para><b>Sie bearbeitet das Modell an Ort und Stelle.</b> Die acht Aufrufer des
    /// Vorläufers reichten ein <c>WErzeugerModel</c> aus ihrer Liste herein und lasen
    /// es nach dem OK wieder aus. Genau das leistet <see cref="Oeffnen"/>: Der Dialog
    /// bekommt eine KOPIE der Felder, und bei OK überträgt die Hülle sie zurück — in
    /// dasselbe Objekt, das der Aufrufer hält.</para>
    ///
    /// <para><b>Die Kostenverwaltung bleibt ein ZWEITES Fenster</b> (Abweichung A-1 aus
    /// Welle 6, unverändert): <see cref="KostenKomponenteHuelle"/> ist selbst eine
    /// Blazor-Hülle, und ihre Verschmelzung zur <c>Ueberlagerung</c> bräuchte deren
    /// Datenseite als Delegatensatz.</para>
    /// </summary>
    internal static class WaermepumpeAnlageHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1126 × 752).</summary>
        private static readonly Size MASS = new Size(1160, 800);

        /// <summary>
        /// Zeigt die Detailansicht als eigenes Fenster und schreibt bei OK in
        /// <paramref name="modell"/> zurück.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="modell">Die Anlagenzeile — sie wird bei OK an Ort und Stelle geändert.</param>
        /// <param name="projektId">
        /// Das GEÖFFNETE Projekt. Es dient als Rückfall beim Nachziehen der
        /// Anlagenzeile; der Vorläufer holte es sich aus <c>Program.startfrm</c>.
        /// </param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, WErzeugerModel modell, int projektId)
        {
            if (modell == null) return false;

            bool ok = false;
            BlazorDialogForm<WaermepumpeAnlageDialog> dlg = null;

            WaermepumpeAnlageDaten daten = AusModell(modell);

            var werte = new Dictionary<string, object>(Gaben(besitzer, daten, modell, projektId))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (b) NachModell(daten, modell);
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermepumpeAnlageDialog>(
                Text_("WPA_TITEL", "Detailansicht"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Detailansicht — für die Anzeige in einer
        /// <c>Ueberlagerung</c> der Wärmepumpen-Verwaltung (W7.5). <c>Geschlossen</c>
        /// setzt dort der Wirt, und er überträgt auch selbst zurück.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, WaermepumpeAnlageDaten daten,
            WErzeugerModel modell, int projektId)
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = daten,

                ["Stammliste"] = new Func<IReadOnlyList<WaermepumpeStammZeile>>(Stammliste),
                ["Vorlaeufe"] = new Func<int, IReadOnlyList<int>>(VorlaeufeZu),
                ["Bilder"] = new Func<int, KennlinienBilder>(
                    idWp => WaermepumpeStammHuelle.BilderZu(idWp, kuehlung: false)),
                ["Stammdaten"] = new Func<int, WaermepumpeStammDaten>(StammdatenZu),

                ["TemperaturenPruefen"] = new Func<int?, int?, string>(TemperaturenPruefen),

                ["KostenBereit"] = new Func<bool>(
                    () => WErzeugerCtrl.AnlagenzeileNachziehen(modell, projektId)),
                ["Kostensumme"] = new Func<(double, double)>(() => Kostensumme(modell)),
                ["KostenOeffnen"] = new Func<Task>(() => KostenOeffnen(besitzer, modell)),

                ["Katalog"] = new Func<IReadOnlyList<WaermepumpenKatalogZeile>>(
                    () => new WPStammCtrl().KatalogZeilen()),
                ["StammGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    WaermepumpeStammHuelle.Gaben),

                ["TitelText"] = Text_("WPA_TITEL", "Detailansicht"),
                ["LabelWpAuswahl"] = Text_("WPA_LBL_WP", "Wärmepumpen Auswahl:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["GruppeKenndaten"] = Text_("WPA_GRP_KENNDATEN", "Wärmepumpen Kenndaten"),
                ["GruppeAuslegung"] = Text_("WPA_GRP_AUSLEGUNG", "Auslegung für Verteilung"),
                ["GruppeSpitzenlast"] = Text_("WPA_GRP_SPITZENLAST", "Spitzenlast und Betrieb"),
                ["LabelBeschreibung"] = Text_("WPA_LBL_BESCHREIBUNG", "Bezeichnung"),
                ["LabelHersteller"] = Text_("WPS_LBL_HERSTELLER", "Hersteller"),
                ["LabelTyp"] = Text_("WPS_LBL_TYP", "Wärmepumpentyp"),
                ["LabelRegelung"] = Text_("WPS_LBL_REGELUNG", "Leistungsstufen"),
                ["LabelBaujahr"] = Text_("WPS_LBL_BAUJAHR", "Baujahr"),
                ["LabelNennleistung"] = Text_("WPS_LBL_NENNLEISTUNG", "Nennleistung"),
                ["LabelPHeizstab"] = Text_("WPS_LBL_HEIZSTAB", "Heizstab"),
                ["LabelPHeizstabKurz"] = Text_("WPA_LBL_PHEIZSTAB", "Leistung Heizstab"),
                ["LabelVorlauf"] = Text_("WPA_LBL_VORLAUF", "Vorlauf"),
                ["LabelRuecklauf"] = Text_("WPA_LBL_RUECKLAUF", "Rücklauf"),
                ["LabelRuecklaufKurz"] = Text_("WPA_LBL_RUECKLAUF", "Rücklauf"),
                ["LabelHeizstab"] = Text_("WPA_LBL_SPITZENLAST", "Wärmeerzeuger Spitzenlast:"),
                ["LabelSperrzeit"] = Text_("WPA_LBL_SPERRZEIT",
                    "Wärmepumpenleistung / maximale Betriebszeit:"),
                ["LabelVon"] = Text_("WPA_LBL_VON", "Sperrzeit von"),
                ["LabelBis"] = Text_("WPA_LBL_BIS", "Sperrzeit bis"),
                ["LabelNutzungszeit"] = Text_("WPA_LBL_NUTZUNGSZEIT", "Nutzungsdauer"),
                ["LabelBivalent"] = Text_("WPA_LBL_BIVALENT", "Bivalenter Betrieb"),
                ["LabelBetriebsart"] = Text_("WPA_LBL_BETRIEBSART", "Betriebsart"),
                ["LabelAbschalttemp"] = Text_("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur"),
                ["LabelAbschalttempKurz"] = Text_("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur"),
                ["LabelKennlinien"] = Text_("WPS_LBL_KENNLINIEN", "Kenndaten Kennlinien:"),
                ["ReiterCop"] = Text_("WPS_REITER_COP", "COP"),
                ["ReiterLeistung"] = Text_("WPS_REITER_LEISTUNG", "Leistung"),
                ["BtnKatalogText"] = Text_("WPK_BTN_KATALOG", "📋  Modul-Katalog..."),
                ["BtnParameterText"] = Text_("WPA_BTN_PARAMETER", "Parameter Bearbeiten..."),
                ["BtnKostenText"] = Text_("WPI_BTN_KOSTEN", "Kosten bearbeiten…"),
                ["TipKosten"] = Text_("WPI_TIP_KOSTEN",
                    "Kostenverwaltung dieser Anlage öffnen (Projektmodus)."),
                ["TipKostenNeu"] = Text_("WPI_TIP_KOSTEN_NEU",
                    "Kosten werden je ANLAGE gepflegt — die Wärmepumpe zuerst mit OK anlegen und speichern; danach über „Ändern..“ die Kosten bearbeiten."),
                ["TextKostenKeine"] = Text_("WPI_KOSTEN_KEINE", "Invest — · Betrieb —"),
                ["TextKostenSummen"] = Text_("WPI_KOSTEN_SUMMEN", "Invest {0:N0} € · Betrieb {1:N0} €/a"),
                ["HinweisSpitzenlast"] = Text_("WPA_HINWEIS_SPITZENLAST",
                    "Ein Spitzenlast Wärmeerzeuger kann notwendig sein aufgrund:"),
                ["HinweisBetrieb"] = Text_("WPA_HINWEIS_BETRIEB",
                    "Außentemperaturgesteuerter Betrieb:"),
                ["WarnungBetriebsart"] = Text_("WPA_MSG_BETRIEBSART", "Bitte Betriebsart auswählen!"),
                ["WarnungWaermepumpe"] = Text_("WPA_MSG_WAERMEPUMPE", "Bitte Wärmepumpe auswählen!"),
                ["WarnungFeldFormat"] = Text_("WPA_MSG_FELD", "Bitte {0} eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        private static IReadOnlyList<WaermepumpeStammZeile> Stammliste()
        {
            var ctrl = new WPStammCtrl();
            ctrl.ReadAll();

            var liste = new List<WaermepumpeStammZeile>();
            foreach (WPModel m in ctrl.items)
                liste.Add(new WaermepumpeStammZeile(m.ID, m.WPName ?? "", m.m_bReadOnly));
            return liste;
        }

        /// <summary>Die Vorlaufstufen eines Geräts (<c>FillVorlaufCombo</c>:125).</summary>
        private static IReadOnlyList<int> VorlaeufeZu(int idWp)
        {
            return KenndatenCtrl.Reihen(idWp).Vorlaeufe;
        }

        private static WaermepumpeStammDaten StammdatenZu(int idWp)
        {
            WPModel m = WaermepumpeGeraeteCtrl.Geraetedaten(idWp);
            if (m == null) return null;

            return new WaermepumpeStammDaten
            {
                Id = m.ID,
                Name = m.WPName ?? "",
                Firma = m.Firma ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Typ = m.Typ ?? "",
                Baujahr = m.Baujahr,
                Aufstellung = m.Aufstellung ?? "",
                Nennleistung = m.Nennleistung,
                Heizstab = (int)m.Heizung,
                Regelung = m.Regelung ?? "",
                Kuehlleistung = m.Kuehlleistung,
                Modulkosten = m.Modulkosten,
                MaxPtherm = m.maxPTherm,
                Bauart = m.Bauart ?? "",
                NurLesen = m.m_bReadOnly
            };
        }

        /// <summary>
        /// Die Prüfung aus <c>ProjektPuffer.TemperaturenPruefen</c> — sie kommt als
        /// Delegat in die Komponente, weil die Klasse im Kern <c>internal</c> ist.
        /// Ein leeres Feld meldet dort „als ganze Zahl eingeben"; die Komponente
        /// liefert dafür <c>null</c>, deshalb der Umweg über die Zeichenkette.
        /// </summary>
        private static string TemperaturenPruefen(int? vorlauf, int? ruecklauf)
        {
            int v, r;
            string fehler;
            bool ok = ProjektPuffer.TemperaturenPruefen(
                vorlauf?.ToString() ?? "", ruecklauf?.ToString() ?? "", out v, out r, out fehler);
            return ok ? null : fehler;
        }

        private static (double Invest, double Betrieb) Kostensumme(WErzeugerModel modell)
        {
            if (modell == null || modell.ID <= 0 || modell.ID_Projekt <= 0) return (0, 0);
            return (KostenSummenCtrl.AnlagenSumme(modell.ID_Projekt,
                        KostenSummenCtrl.KATEGORIE_INVESTITION, modell.ID),
                    KostenSummenCtrl.AnlagenSumme(modell.ID_Projekt,
                        KostenSummenCtrl.KATEGORIE_BETRIEB, modell.ID));
        }

        /// <summary>
        /// „Kosten bearbeiten…" (<c>btnKosten_Click</c>:566) — ein ZWEITES Fenster
        /// (A-1 aus Welle 6, unverändert).
        /// </summary>
        private static Task KostenOeffnen(IWin32Window besitzer, WErzeugerModel modell)
        {
            if (modell == null || modell.ID_Projekt <= 0) return Task.CompletedTask;

            string projektname = "";
            try
            {
                var pc = new ProjektCtrl();
                pc.ReadSingle(modell.ID_Projekt);
                if (pc.rows > 0) projektname = pc.m_szProjektname;
            }
            catch { }

            KostenKomponenteHuelle.OeffnenProjekt(besitzer, modell.ID_Projekt, projektname,
                                                  DbWerte.ERZEUGER_WAERMEPUMPE, false, modell.ID);
            return Task.CompletedTask;
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        /// <summary>Aus der Anlagenzeile in den Feldsatz — <c>SetControls</c>:151.</summary>
        internal static WaermepumpeAnlageDaten AusModell(WErzeugerModel m)
        {
            var d = new WaermepumpeAnlageDaten
            {
                Bezeichner = m.Bezeichner ?? "",
                IdWp = m.ID_WP,
                Vorlauf = m.Vorlauf,
                Ruecklauf = m.Ruecklauf,
                Heizstab = m.Heizstab,
                HeizstabLeistung = (int)m.Heizung,
                Sperrung = m.Sperrung,
                SperrzeitVon = m.Sperrzeit_von,
                SperrzeitBis = m.Sperrzeit_bis,
                Nutzungszeit = m.Nutzungszeit,
                BivalenterBetrieb = m.Bivalenter_Betrieb,
                Betriebsart = m.Betriebsart ?? "",
                Abschaltpunkt = m.Abschaltpunkt,
                Beschreibung = m.Beschreibung ?? "",
                Baujahr = m.Baujahr,
                Regelung = m.Regelung ?? "",
                Typ = m.Typ ?? "",
                Firma = m.Firma ?? "",
                Nennleistung = m.Nennleistung,
                Modulkosten = m.Modulkosten,
                Volumen = m.Volumen,
                Solaranteil = m.Solaranteil,
                RendeMix = m.rendeMix
            };
            return d;
        }

        /// <summary>
        /// Zurück in die Anlagenzeile — <c>btn_Beenden_Click</c>:238-268, mit denselben
        /// Zuweisungen und in derselben Reihenfolge. <c>ID_SP</c>, <c>ID_PV</c> und
        /// <c>ID_Solar</c> werden dabei wie dort auf 0 gesetzt: Eine
        /// Wärmepumpen-Anlagenzeile verweist auf kein anderes Gerät.
        /// </summary>
        internal static void NachModell(WaermepumpeAnlageDaten d, WErzeugerModel m)
        {
            m.Bezeichner = d.Bezeichner;
            m.Betriebsart = d.Betriebsart;
            m.Sperrung = d.Sperrung;
            m.Sperrzeit_bis = d.SperrzeitBis ?? 0;
            m.Sperrzeit_von = d.SperrzeitVon ?? 0;
            m.Ruecklauf = d.Ruecklauf ?? 0;
            m.Vorlauf = d.Vorlauf ?? 0;
            m.Bivalenter_Betrieb = d.BivalenterBetrieb;

            // Leer laesst den bisherigen Wert stehen - das Feld ist je nach
            // Betriebsart gar nicht sichtbar.
            if (d.Abschaltpunkt.HasValue) m.Abschaltpunkt = d.Abschaltpunkt.Value;

            m.ID_WP = d.IdWp;
            m.ID_SP = 0;
            m.ID_PV = 0;
            m.ID_Solar = 0;
            m.Heizstab = d.Heizstab;
            m.Heizung = d.HeizstabLeistung ?? 0;
            m.Volumen = d.Volumen;
            m.rendeMix = d.RendeMix;
            m.Solaranteil = d.Solaranteil;
            m.Nutzungszeit = d.Nutzungszeit ?? 0;

            // Ä23: Die Stammfelder der gewaehlten Waermepumpe gehoeren zur Zeile -
            // sonst zeigte die Verwaltungsliste nach einem Wechsel 0 kW.
            m.Regelung = d.Regelung;
            m.Nennleistung = d.Nennleistung;
            m.Modulkosten = d.Modulkosten;
            m.Baujahr = d.Baujahr;
            m.Beschreibung = d.Beschreibung;
            m.Firma = d.Firma;
            m.Typ = d.Typ;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
