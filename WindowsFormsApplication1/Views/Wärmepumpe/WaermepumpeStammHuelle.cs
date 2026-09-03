using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Wärmepumpen-STAMMDIALOGS (iU9-W7.3).
    ///
    /// <para><b>Was sie liefert.</b> Zehn Delegaten: die Stammliste, einen Satz zu
    /// seiner Id, die beiden Kennlinienbilder, die Kühlungsauskunft, den Speicherweg,
    /// die Löschsperre, das Löschen, die Kennlinien für den Editor, deren
    /// Rückschreibweg und den Katalog für die Überlagerung. Die Datenseite steht
    /// vollständig im Kern (<see cref="WPStammCtrl"/>, <see cref="KenndatenCtrl"/>,
    /// <see cref="KenndatenKuehlungCtrl"/>, <c>ChartRenderer.Kennlinien</c>).</para>
    ///
    /// <para><b>Die Bilder entstehen HIER, nicht in der Komponente.</b> Der Renderer
    /// gehört zum Kern und liefert PNG-Bytes; <c>ChartBild</c> zeigt sie als
    /// <c>data:</c>-URL. Dasselbe Muster wie <see cref="KapitalwertVerlaufHuelle"/>
    /// (W1.6) und <see cref="KostenprofilHuelle"/> (W3.4) — nur ohne
    /// <c>Task.Run</c>: Zwei Kennlinienbilder sind in wenigen Millisekunden gezeichnet,
    /// und der Aufruf kommt aus einem Rückruf, der ein Ergebnis erwartet.</para>
    /// </summary>
    internal static class WaermepumpeStammHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 877 × 642 zzgl. der zwei Bilder).</summary>
        private static readonly Size MASS = new Size(1000, 760);

        /// <summary>
        /// Zeigt den Stammdialog als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation.OeffneMaske(Masken.WpAdministration)</c> und, bis
        /// Welle 7.4, von <c>Wizard_WPItem.btn_WP_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit „Beenden" geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<WaermepumpeStammDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermepumpeStammDialog>(
                Text_("WPS_TITEL", "Datenbank Wärmepumpen"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Stammdialogs — für die Anzeige in einer
        /// <c>Ueberlagerung</c> des Anlagendialogs (W7.4). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var daten = new WaermepumpeStammDaten();

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,

                ["Liste"] = new Func<IReadOnlyList<WaermepumpeStammZeile>>(Stammliste),
                ["Satz"] = new Func<int, WaermepumpeStammDaten>(SatzZu),
                ["Bilder"] = new Func<int, bool, KennlinienBilder>(BilderZu),
                ["HatKuehlung"] = new Func<int, bool>(KenndatenKuehlungCtrl.HatKenndaten),
                ["Speichern"] = new Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>(Speichern),
                ["GesperrtDurch"] = new Func<string, string>(
                    name => new WPStammCtrl().GesperrtDurchProjekt(name)),
                ["Loeschen"] = new Func<string, bool>(Loeschen),
                ["Kennlinien"] = new Func<int, IReadOnlyList<KennlinienZeile>>(KennlinienZu),
                ["KennlinienAbgleichen"] = new Func<int, IReadOnlyList<KennlinienZeile>, bool>(
                    (idWp, zeilen) => KenndatenCtrl.Abgleichen(idWp, NachModell(zeilen))),
                ["Katalog"] = new Func<IReadOnlyList<WaermepumpenKatalogZeile>>(
                    () => new WPStammCtrl().KatalogZeilen()),

                ["TitelText"] = Text_("WPS_TITEL", "Datenbank Wärmepumpen"),
                ["KopfbandText"] = Text_("WPS_KOPFBAND",
                    "Verwaltung Daten zu Wärmepumpen und deren Kennlinien"),
                ["LabelListe"] = Text_("WPS_LBL_LISTE", "Wärmepumpen Auswahl:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["GruppeStammdaten"] = Text_("WPS_GRP_STAMM", "Wärmepumpe"),
                ["LabelName"] = Text_("WPS_LBL_NAME", "Name"),
                ["LabelHersteller"] = Text_("WPS_LBL_HERSTELLER", "Hersteller"),
                ["LabelBeschreibung"] = Text_("WPS_LBL_BESCHREIBUNG", "Beschreibung"),
                ["LabelTyp"] = Text_("WPS_LBL_TYP", "Wärmepumpentyp"),
                ["LabelRegelung"] = Text_("WPS_LBL_REGELUNG", "Leistungsstufen"),
                ["LabelAufstellung"] = Text_("WPS_LBL_AUFSTELLUNG", "Aufstellung"),
                ["LabelBaujahr"] = Text_("WPS_LBL_BAUJAHR", "Baujahr"),
                ["LabelNennleistung"] = Text_("WPS_LBL_NENNLEISTUNG", "Nennleistung"),
                ["LabelHeizstab"] = Text_("WPS_LBL_HEIZSTAB", "Heizstab"),
                ["LabelKuehlleistung"] = Text_("WPS_LBL_KUEHLLEISTUNG", "Kühlleistung"),
                ["LabelKennlinien"] = Text_("WPS_LBL_KENNLINIEN", "Kenndaten Kennlinien:"),
                ["OptionWaerme"] = Text_("WPS_OPT_WAERME", "Wärme"),
                ["OptionKuehlung"] = Text_("WPS_OPT_KUEHLUNG", "Kühlung"),
                ["ReiterCop"] = Text_("WPS_REITER_COP", "COP"),
                ["ReiterLeistung"] = Text_("WPS_REITER_LEISTUNG", "Leistung"),
                ["BtnKenndatenText"] = Text_("WPS_BTN_KENNDATEN", "Kennliniendaten Ansicht/Bearbeiten..."),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["BtnNeuText"] = Text_("WPS_BTN_NEU", "Neu"),
                ["BtnLoeschenText"] = Text_("WPS_BTN_LOESCHEN", "Löschen"),
                ["BtnKatalogText"] = Text_("WPK_BTN_KATALOG", "📋  Modul-Katalog..."),
                ["BtnBeendenText"] = MyResource.Resource.WP_BTN_BEENDEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,
                ["FrageLoeschen"] = Text_("WPS_FRAGE_LOESCHEN",
                    "Wollen Sie wirklich die Wärmepumpe löschen?"),
                ["MeldungReadOnlySpeichern"] = Text_("WPS_MSG_READONLY_SPEICHERN",
                    "Diese Wärmepumpe ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden."),
                ["MeldungReadOnlyLoeschen"] = Text_("WPS_MSG_READONLY_LOESCHEN",
                    "Diese Wärmepumpe ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden."),
                ["MeldungReadOnlyKenndaten"] = Text_("WPS_MSG_READONLY_KENNDATEN",
                    "Diese Wärmepumpe ist schreibgeschützt (ReadOnly). Die Kennliniendaten können nur angesehen, nicht geändert werden."),
                ["MeldungProjektFormat"] = Text_("WPS_MSG_PROJEKT",
                    "Löschen nicht möglich! Diese Wärmepumpe ist dem Projekt {0} zugeordnet!")
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

        private static WaermepumpeStammDaten SatzZu(int id)
        {
            var ctrl = new WPStammCtrl();
            ctrl.ReadAll("ID=" + id);
            if (ctrl.rows == 0) return null;

            WPModel m = ctrl.items[0];
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
                // WPModel.Heizung ist ein double, das Feld "Heizstab" der Maske eine
                // Ganzzahl (der Vorlaeufer las es mit Program.GanzzahlParsen).
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
        /// Die beiden Bilder eines Geräts. Wärme und Kühlung lesen aus verschiedenen
        /// Tabellen und tragen verschiedene Punktmarken — Kreis für den COP, Kreuz für
        /// die Leistung, wie <c>MarkerStyle.Circle</c>/<c>.Cross</c> im Vorläufer.
        /// </summary>
        internal static KennlinienBilder BilderZu(int idWp, bool kuehlung)
        {
            if (idWp <= 0) return KennlinienBilder.Leer;

            KennlinienSatz satz = kuehlung
                ? KenndatenKuehlungCtrl.Reihen(idWp)
                : KenndatenCtrl.Reihen(idWp);

            string yLeistung = kuehlung
                ? Text_("WPS_ACHSE_PKUEHL", "Leistung")
                : Text_("WPS_REITER_LEISTUNG", "Leistung");

            return new KennlinienBilder(
                ChartRenderer.Kennlinien(Text_("WPS_REITER_COP", "COP"),
                    Text_("WPS_REITER_COP", "COP"), Text_("WPS_ACHSE_TEMPERATUR", "Temperatur"),
                    satz.Cop, ChartRenderer.Kennlinienmarke.Kreis),
                ChartRenderer.Kennlinien(yLeistung, yLeistung,
                    Text_("WPS_ACHSE_TEMPERATUR", "Temperatur"),
                    satz.Leistung, ChartRenderer.Kennlinienmarke.Kreuz));
        }

        /// <summary>
        /// Der Speicherweg (<c>btn_Speichern_Click</c>:372) — ohne die Pflichtprüfung
        /// der Modulkosten, weil die Zeile seit Ä19 nicht mehr gezeichnet wird
        /// (Abweichung A-14). Die übrigen Zahlenfelder übernahm der Vorläufer STILL:
        /// Ein unlesbarer Text ließ den gelesenen Datensatzwert stehen. Hier meldet
        /// <c>Ganzzahlfeld</c> einen leeren Wert als <c>null</c>, und daraus wird 0 —
        /// derselbe Ausgang, weil bei einer Neuanlage 0 der Ausgangswert ist.
        /// </summary>
        private static KatalogSpeicherErgebnis Speichern(WaermepumpeStammDaten daten, bool neu)
        {
            var ctrl = new WPStammCtrl();

            var modell = new WPModel
            {
                ID = daten.Id,
                WPName = (daten.Name ?? "").Trim(),
                Firma = daten.Firma,
                Beschreibung = daten.Beschreibung,
                Typ = daten.Typ,
                Baujahr = daten.Baujahr ?? 0,
                Aufstellung = daten.Aufstellung,
                Nennleistung = daten.Nennleistung ?? 0,
                maxPTherm = daten.MaxPtherm,
                Heizung = daten.Heizstab ?? 0,
                Regelung = daten.Regelung,
                Modulkosten = daten.Modulkosten,
                Bauart = daten.Bauart,
                Kuehlleistung = daten.Kuehlleistung
            };

            WPStammCtrl.SpeicherErgebnis ergebnis = ctrl.Speichern(modell, neu);
            return new KatalogSpeicherErgebnis(ergebnis.Ok, ergebnis.Meldung, ergebnis.Name);
        }

        private static bool Loeschen(string name)
        {
            var ctrl = new WPStammCtrl();
            ctrl.ReadSingle("select * from " + WPStammCtrl.TABLE + " where Bezeichner='" +
                            (name ?? "").Replace("'", "''") + "'");
            return ctrl.Delete();
        }

        // =================================================================================
        // Kennlinien: zwischen Editor und Kern uebersetzen
        // =================================================================================

        private static IReadOnlyList<KennlinienZeile> KennlinienZu(int idWp)
        {
            var liste = new List<KennlinienZeile>();
            foreach (KenndatenModel m in KenndatenCtrl.LiesStamm(idWp))
                liste.Add(new KennlinienZeile
                {
                    Id = m.m_ID,
                    Vorlauf = m.m_nVorlauf,
                    Temperatur = m.m_nTemperatur,
                    Cop = m.m_nCOP,
                    Ptherm = m.m_nPTherm
                });
            return liste;
        }

        private static IReadOnlyList<KenndatenModel> NachModell(IReadOnlyList<KennlinienZeile> zeilen)
        {
            var liste = new List<KenndatenModel>();
            if (zeilen == null) return liste;

            foreach (KennlinienZeile z in zeilen)
                liste.Add(new KenndatenModel
                {
                    m_ID = z.Id,
                    m_nVorlauf = z.Vorlauf,
                    m_nTemperatur = z.Temperatur ?? 0,
                    m_nCOP = z.Cop ?? 0,
                    m_nPTherm = z.Ptherm ?? 0
                });
            return liste;
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
