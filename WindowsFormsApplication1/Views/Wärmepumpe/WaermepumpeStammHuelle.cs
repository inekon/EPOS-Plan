using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1.Zeichnung;

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
    /// <see cref="KenndatenKuehlungCtrl"/>, <c>ChartRenderer.KennlinienModell</c>).</para>
    ///
    /// <para><b>Die Bilder entstehen HIER, nicht in der Komponente.</b> Der Renderer
    /// gehört zum Kern und liefert ein ZEICHENMODELL; <c>DiagrammSvg</c> macht daraus
    /// Razor-Elemente. Dasselbe Muster wie <see cref="KapitalwertVerlaufHuelle"/>
    /// (W1.6) und <see cref="KostenprofilHuelle"/> (W3.4) — nur ohne
    /// <c>Task.Run</c>: Zwei Kennlinienbilder sind in wenigen Millisekunden gebaut,
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
                }),

                // "Import..." (Konzept Administrationsdialoge 7.1 d) nur im eigenen
                // Fenster, nicht in der Ueberlagerung des Anlagendialogs.
                ["ImportGaben"] = new Func<IReadOnlyDictionary<string, object>>(Importsatz)
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
        /// Der Parametersatz des Wärmepumpenimports (VDI 3805 Blatt 22) hinter „Import…"
        /// (Konzept Administrationsdialoge 7.1 d) — derselbe wie im Menü „Daten &amp; Import".
        /// Er gehört dem <c>KatalogImportDialog</c>, nicht dem Stammdialog; deshalb steht er
        /// in einer eigenen Methode und nicht im Wörterbuch von <see cref="Oeffnen"/>.
        /// </summary>
        private static IReadOnlyDictionary<string, object> Importsatz()
        {
            return KatalogImportHuelle.Gaben(KatalogImportArt.Waermepumpe);
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

                // W14a-E-10 (07.09.2026): NEUN Spalten statt einer Namensspalte -
                // Hersteller, Modell, Quelle, P_N, VL min, VL max, Zuheizung, Kuehlen
                // und COP bei A2/W35. Der Controller rechnet die drei abgeleiteten
                // Groessen aus Tab_Kenndaten_STAMM einmal je Liste.
                ["Liste"] = new Func<IReadOnlyList<Katalogfilterzeile>>(Stammliste),
                ["Filterprofil"] = Katalogfilterprofil.Finde(Anlagenart.Waermepumpe,
                                                             KatalogBrowserHuelle.Text),
                ["Satz"] = new Func<int, WaermepumpeStammDaten>(SatzZu),
                ["Bilder"] = new Func<int, bool, KennlinienBilder>(BilderZu),

                // Die Farbwahl am Bild (Farbrollen, Bedienung Teil 2): Jede
                // Vorlaufkennlinie traegt eine Farbrolle, der Klick auf ihr Farbfeld
                // schreibt sie anwendungsweit. Kein Delegat, kein Waehler.
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),
                ["HatKuehlung"] = new Func<int, bool>(KenndatenKuehlungCtrl.HatKenndatenStamm),
                ["Speichern"] = new Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>(Speichern),
                ["GesperrtDurch"] = new Func<string, string>(
                    name => new WPStammCtrl().GesperrtDurchProjekt(name)),
                ["Loeschen"] = new Func<string, bool>(Loeschen),
                // AD-Q11 (23.09.2026): ein Auslieferungssatz wird nie ueberschrieben;
                // "Duplizieren..." legt den eigenen Satz samt Kennlinien an.
                ["Duplizieren"] = new Func<int, string, KatalogSpeicherErgebnis>(
                    (id, name) => KatalogBrowserHuelle.Kopie(WPStammCtrl.Duplizieren(id, name))),
                ["BtnDuplizierenText"] = MyResource.Resource.ADM_BTN_DUPLIZIEREN,
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                ["Schloss"] = Schlosswege.Aus(WPStammCtrl.SchlossSetzen),
                ["Kennlinien"] = new Func<int, IReadOnlyList<KennlinienZeile>>(KennlinienZu),
                ["KennlinienAbgleichen"] = new Func<int, IReadOnlyList<KennlinienZeile>, bool>(
                    KennlinienAbgleichen),
                // W14a-E-8 (06.09.2026): der Aufklapper „Alle Parameter und ihre
                // Verwendung anzeigen" unter dem Stammdatenblock. Er ist hier die
                // Auskunft, die im Bestand fehlte: Die Maske zeigt elf der achtzehn
                // Fachspalten von Tab_WP_STAMM.
                ["Uebersicht"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    bezeichner => ParameterUebersichtCtrl.Werte(
                        Anlagenart.Waermepumpe, bezeichner, KatalogBrowserHuelle.Text)),

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

                // W14a-O-1 (Anwenderentscheid 06.09.2026): die Modulkosten stehen
                // wieder im Raster - nur lesend, mit Herleitungszeile. Die
                // BESCHRIFTUNG kommt aus MODK_LBL_MODULKOSTEN, also aus demselben
                // Schluessel, den der Verwendungskatalog fuer diese Spalte fuehrt
                // (ParameterVerwendung.Waermepumpe): Raster und Aufklapper nennen den
                // Parameter damit wortgleich. Neu sind nur die beiden leisen Zeilen.
                ["LabelModulkosten"] = Text_("MODK_LBL_MODULKOSTEN", "Modulkosten"),
                ["HerleitungModulkosten"] = Text_("WPS_HERL_MODULKOSTEN",
                    "aus dem Datenbestand; Gerätekosten werden in der Kostenverwaltung gepflegt"),
                ["HinweisModulkostenLeer"] = Text_("WPS_HINWEIS_MODULKOSTEN_LEER",
                    "kein Planwert im Datenbestand"),

                ["LabelKennlinien"] = Text_("WPS_LBL_KENNLINIEN", "Kenndaten Kennlinien:"),
                ["OptionWaerme"] = Text_("WPS_OPT_WAERME", "Wärme"),
                ["OptionKuehlung"] = Text_("WPS_OPT_KUEHLUNG", "Kühlung"),
                ["ReiterCop"] = Text_("WPS_REITER_COP", "COP"),
                ["ReiterLeistung"] = Text_("WPS_REITER_LEISTUNG", "Leistung"),
                ["PlatzhalterBild"] = Text_("WPS_PLATZHALTER_BILD", "Keine Kennlinien vorhanden"),
                // DL-2 Nr. 4: Der Knopf der Fussleiste traegt den KURZEN Text, die
                // Ueberlagerung den vollen Wortlaut des Vorlaeufers.
                ["BtnKenndatenText"] = Text_("WPS_BTN_KENNDATEN_KURZ", "Kennliniendaten..."),
                ["TitelKenndatenText"] = Text_("WPS_BTN_KENNDATEN", "Kennliniendaten Ansicht/Bearbeiten..."),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["BtnNeuText"] = Text_("WPS_BTN_NEU", "Neu"),
                ["BtnLoeschenText"] = Text_("WPS_BTN_LOESCHEN", "Löschen"),
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

        private static IReadOnlyList<Katalogfilterzeile> Stammliste()
        {
            return new WPStammCtrl().Katalogfilterzeilen();
        }

        /// <summary>
        /// Ein Satz des STAMMKATALOGS zu seiner Id. <b>Seit dem 16.09.2026 auch der Weg
        /// des Anlagendialogs</b> (Gabe <c>StammSatz</c>): Dort stehen die Parameter des
        /// Stammgeräts unmittelbar im Kenndatenblock, statt hinter „Parameter Bearbeiten…".
        /// </summary>
        internal static WaermepumpeStammDaten SatzZu(int id)
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
        ///
        /// <para><b>ZEICHENMODELL statt PNG</b> (Etappe DG-E3, Gruppe (b)):
        /// <c>KennlinienModell</c> ist der Rumpf, den <c>Kennlinien</c> an den Maler
        /// gibt — dasselbe Bild, nur eben nicht mehr in Bildpunkten eingefroren. Die
        /// Oberfläche zeichnet es als SVG und liest den Wert am Zeiger unmittelbar
        /// aus dem Modell.</para>
        ///
        /// <para><b>EIN Lauf, EIN Paar.</b> Beide Modelle entstehen zusammen und
        /// bleiben als Paar im Dialog stehen, bis Zeile oder Betriebsart wechseln:
        /// Der Baustein <c>DiagrammSvg</c> baut seinen Knotenbaum an der REFERENZ des
        /// Modells fest, und ein je Zeichenlauf neu gebautes Modell verwürfe mit dem
        /// Baum auch die abgewählten Linien und die Zeigerstelle.</para>
        /// </summary>
        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags landet hier: Die Rolle
        /// bekommt anwendungsweit diese Farbe (<c>Diagrammfarben.Setze</c> schreibt
        /// die Einstellung und speist <c>Farbpalette.Aktuell</c>). Danach trägt sie
        /// jedes Diagramm und jeder Bericht — beide malen über dieselbe Palette.
        /// </summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }

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
                ChartRenderer.KennlinienModell(Text_("WPS_REITER_COP", "COP"),
                    Text_("WPS_REITER_COP", "COP"), Text_("WPS_ACHSE_TEMPERATUR", "Temperatur"),
                    satz.Cop, ChartRenderer.Kennlinienmarke.Kreis),
                ChartRenderer.KennlinienModell(yLeistung, yLeistung,
                    Text_("WPS_ACHSE_TEMPERATUR", "Temperatur"),
                    satz.Leistung, ChartRenderer.Kennlinienmarke.Kreuz));
        }

        /// <summary>
        /// Der Speicherweg (<c>btn_Speichern_Click</c>:372) — ohne die Pflichtprüfung
        /// der Modulkosten (Abweichung A-14). Seit W14a‑O‑1 steht die Zeile wieder in
        /// der Maske, aber NUR LESEND: <c>daten.Modulkosten</c> ist derselbe Wert, den
        /// <see cref="SatzZu"/> gelesen hat, und wird hier lediglich durchgereicht —
        /// zu prüfen gibt es nichts, was der Anwender eingegeben hätte.
        /// Die übrigen Zahlenfelder übernahm der Vorläufer STILL:
        /// Ein unlesbarer Text ließ den gelesenen Datensatzwert stehen. Hier meldet
        /// <c>Ganzzahlfeld</c> einen leeren Wert als <c>null</c>, und daraus wird 0 —
        /// derselbe Ausgang, weil bei einer Neuanlage 0 der Ausgangswert ist.
        /// </summary>
        internal static KatalogSpeicherErgebnis Speichern(WaermepumpeStammDaten daten, bool neu)
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

        /// <summary>
        /// Die Kennlinien eines KATALOGSATZES für den Editor — der Weg der
        /// Katalogverwaltung (<c>Tab_Kenndaten_STAMM</c>).
        /// </summary>
        internal static IReadOnlyList<KennlinienZeile> KennlinienZu(int idWp)
            => AusModell(KenndatenCtrl.LiesStamm(idWp));

        /// <summary>
        /// Der Rückschreibweg des Kennlinieneditors in den KATALOG
        /// (<c>Tab_Kenndaten_STAMM</c>).
        /// </summary>
        internal static bool KennlinienAbgleichen(int idWp, IReadOnlyList<KennlinienZeile> zeilen)
        {
            return KenndatenCtrl.Abgleichen(idWp, NachModell(zeilen));
        }

        /// <summary>
        /// Die PROJEKTKENNLINIEN eines Geräts für den Editor (<c>Tab_Kenndaten</c>) — der
        /// Weg des ANLAGENDIALOGS seit dem Anwenderentscheid vom 16.09.2026.
        ///
        /// <para><b>Der Unterschied zu <see cref="KennlinienZu"/> ist die TABELLE</b>, wie
        /// schon bei den Bildern (Befund W7‑B‑3): Gerechnet wird ausschließlich mit den
        /// Projektkennlinien; ein Editor auf dem Katalogsatz änderte einen Stand, den die
        /// Simulation gar nicht liest — und für jedes andere Projekt mit.</para>
        /// </summary>
        internal static IReadOnlyList<KennlinienZeile> KennlinienProjektZu(int idWp)
            => AusModell(KenndatenCtrl.LiesProjekt(idWp));

        /// <summary>Der Rückschreibweg in die PROJEKTKENNLINIEN (<c>Tab_Kenndaten</c>).</summary>
        internal static bool KennlinienProjektAbgleichen(int idWp, IReadOnlyList<KennlinienZeile> zeilen)
        {
            return KenndatenCtrl.AbgleichenProjekt(idWp, NachModell(zeilen));
        }

        /// <summary>Aus den Kernzeilen in die Editorzeilen — für beide Tabellen dieselbe Abbildung.</summary>
        private static IReadOnlyList<KennlinienZeile> AusModell(IReadOnlyList<KenndatenModel> quelle)
        {
            var liste = new List<KennlinienZeile>();
            if (quelle == null) return liste;

            foreach (KenndatenModel m in quelle)
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
