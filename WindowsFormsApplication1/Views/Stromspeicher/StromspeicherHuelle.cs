using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Stromspeicher-Projektdialogs (iU9-W6.6).
    ///
    /// <para><b>Keine SQL in der Hülle.</b> Katalog, Projektkopie, Sammelspeichern, Rückweg und Löschen kommen aus
    /// <see cref="StromspeicherStammCtrl"/> und <see cref="StromspeicherCtrl"/>. Außerhalb des Assistenten legt
    /// „In das Projekt übernehmen" die Projektkopie sofort an (<c>CopyFromStamm</c>, Muster BHKW); was diese Sitzung
    /// neu anlegt oder entfernt, schließt erst OK ab (<see cref="Projektkopievormerkung"/>).</para>
    ///
    /// <para><b>Zwei Beschriftungen kommen aus dem Ressourcenkatalog</b>, nicht aus dem
    /// Designer: <c>SP_LABEL_ENERGIE</c> und <c>SP_LABEL_MODULKOSTEN</c>. Der Designer
    /// trug „Energie [kW]" und „Modulkosten" — beides fachlich falsch (Abnahmebefund 1
    /// zum ersten App-Start; <c>Tab_Stromspeicher.Energie</c> ist die nutzbare
    /// Nennkapazität in kWh, <c>Modulkosten</c> der kapazitätsbezogene Satz in €/kWh).
    /// Der Vorläufer korrigierte sie im Code (<c>EinheitenBeschriftungKorrigieren</c>);
    /// hier setzt sie die Hülle ein.</para>
    /// </summary>
    internal static class StromspeicherHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 838 × 501).</summary>
        private static readonly Size MASS = new Size(900, 600);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Stromspeicher_Click</c> und
        /// <c>StromspeicherKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<StromspeicherDialog> dlg = null;
            var vormerkung = new Projektkopievormerkung(name => new StromspeicherCtrl().DeleteFromProjekt(name, projektId));

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle, wizard: false, vormerkung: vormerkung))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<StromspeicherDialog>(
                Text_("SPD_TITEL", "Verwaltung Stromspeicher"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            // Entfernte Projektkopien gehen erst mit OK; Abbrechen (auch Kreuz und Esc) raeumt nur die in dieser
            // Sitzung neu angelegten ab - Varianten desselben Speichers teilen sich eine Kopie.
            vormerkung.Abschliessen(ok, id => modelle.Exists(it => it.ID_Type == idType && it.ID_SP == id));
            return ok;
        }

        /// <summary>Die Speicherseite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType,
            List<WErzeugerModel> modelle, bool wizard, Projektkopievormerkung vormerkung = null)
        {
            // Die Projektkopie gibt es nur mit Projekt und ausserhalb des Assistenten - dort zeigt ID_SP auf den Katalog.
            bool mitKopie = !wizard && projektId > 0;
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            // ET-5 (08.09.2026): Die Zeile zeigt den Traeger der Anlage - gespeichert oder,
            // solange keiner gespeichert ist, den Stromtraeger des Projekts (die Vorgabe).
            int stromVorgabe = ErzeugerTraegerHuelle.Vorauswahl(DbWerte.ERZEUGER_STROMSPEICHER, 0, projektId);
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                ErzeugerZeile zeile = ZeileZu(m);
                if (zeile.CarrierId <= 0) zeile.CarrierId = stromVorgabe;
                zeilen.Add(zeile);
                zuModell[m.ID] = m;
            }

            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                // W14a-E-10 / S2.1: Der Speicherkatalog bekommt seine ACHT Spalten -
                // er hatte als einziger gar keinen Filter. Dazu die Spalte "im
                // Projekt verwendet" (Q12).
                ["Katalogprofil"] = Katalogfilterprofil.MitVerwendung(
                    Anlagenart.Stromspeicher, Text_),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Stromspeicher, n, Text_)),

                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    StromspeicherStammCtrl.Katalogfilterzeilen),
                ["KatalogDetail"] = new Func<string, ErzeugerDetail>(
                    name => { var c = new StromspeicherStammCtrl(); c.ReadSingle(name); return DetailZu(c.rows == 0 ? null : c.items[0]); }),
                // Die Projektzeile liest ihre Projektkopie ueber die Geraete-ID: Varianten desselben Speichers tragen
                // eigene Namen. Im Assistenten zeigt die ID auf den Katalog.
                ["ProjektDetail"] = new Func<ErzeugerZeile, ErzeugerDetail>(
                    zeile => DetailZu(StromspeicherStammCtrl.Satz(mitKopie, zeile.GeraetId))),

                ["Aufnehmen"] = new Func<int, AufnahmeErgebnis>(
                    stammId => Aufnehmen(projektId, idType, mitKopie, modelle, zuModell, zaehler, stammId, vormerkung)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(zeile.Schluessel);
                        // Nur VORMERKEN - geloescht wird beim OK, und nur, wenn keine Zeile mehr auf die Kopie zeigt.
                        if (mitKopie && vormerkung != null)
                            vormerkung.Entfernt(StromspeicherStammCtrl.Satz(true, m.ID_SP)?.m_szBezeichner ?? m.Bezeichner, m.ID_SP);
                    }),

                // KATALOGAUSWAHL V1, STUFE 3: die Summe kWh der Projektliste - die Kapazitaet der Projektkopie
                // (im Assistenten des Katalogsatzes) je Zeile; Varianten zaehlen je Anlage.
                ["SummeKapazitaet"] = new Func<string>(
                    () => SummeKapazitaet(idType, mitKopie, modelle)
                              .ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)),
                ["LabelSumme"] = Text_("SPD_LBL_SUMME", "Summe aller ausgewählten Speicher [kWh]:"),

                // KA-E-8: Bearbeiten je Bereich und Mehrfach-Bearbeiten, geschrieben ueber den Kernweg in EINER
                // Transaktion (StromspeicherStammCtrl.SchreibenAlle) - samt den vier Kostenposten.
                ["ProjektsatzWege"] = mitKopie ? new Satzbearbeitungswege
                {
                    Lesen = id => StromspeicherAdminHuelle.SatzFelder(true, id),
                    Speichern = saetze => StromspeicherAdminHuelle.SammelSchreiben(true, saetze)
                } : null,
                ["KatalogsatzWege"] = new Satzbearbeitungswege
                {
                    Lesen = id => StromspeicherAdminHuelle.SatzFelder(false, id),
                    Speichern = saetze => StromspeicherAdminHuelle.SammelSchreiben(false, saetze)
                },

                // KA-E-13: ein einzelner ungesperrter Katalogsatz oeffnet den Modulkatalog mit diesem Satz
                // vorgewaehlt; er traegt selbst Neu..., Duplizieren... und Loeschen. Ueberlagerung im selben Fenster.
                ["EditorGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => new Dictionary<string, object>(StromspeicherAdminHuelle.Gaben()) { ["Vorwahl"] = name }),

                // Loeschen im Katalogfuss: leer = geloescht, sonst der Grund.
                ["KatalogLoeschen"] = new Func<int, string>(KatalogLoeschen),

                // DIE ZWEI WEGE DES MODULAUFKLAPPERS (Anwenderentscheid 15.09.2026).
                // Sie kommen aus derselben Quelle, aus der auch der Modulkatalog hinter
                // "Bearbeiten..." seine Felder bekommt; der Aufklapper IST sein Raster.
                // Uebersetzt wird zwischen den zwei Feldtypen in der
                // ModulFeldwertBruecke. Erst damit sind die sechs AP3-Geraetewerte
                // (Wirkungsgrad, Zyklen, Verschleiss- und Leistungskosten, Investition
                // fix, Standby) aus dem Projektdialog heraus ueberhaupt zu sehen.
                // SCHLOSS SETZEN / AUFHEBEN an der Katalogliste (AD-Q15) - derselbe Weg wie in
                // der Verwaltung. Die Verwendung im Projekt sperrt nichts (eigene Kopie).
                ["Schloss"] = Schlosswege.Aus(StromspeicherStammCtrl.SchlossSetzen),

                ["Katalogfelder"] = new Func<string, IReadOnlyList<BrowserFeldwert>>(
                    name => ModulFeldwertBruecke.Felder(StromspeicherAdminHuelle.Wege(), name)),
                ["KatalogfelderSpeichern"] =
                    new Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>(
                        (name, felder) => ModulFeldwertBruecke.Speichern(
                            StromspeicherAdminHuelle.Wege(), name, felder)),
                ["LabelAlleParameter"] = Text_("HZK_LBL_ALLE_DATEN", "Alle Daten anzeigen"),
                ["BtnFelderSpeichernText"] = Text_("HZK_BTN_FELDER_SPEICHERN", "Speichern"),

                // Die drei Knoepfe der Kostenleiste. Ohne Projekt gibt es keinen
                // Kostenkontext - dann bleibt der Delegat weg und die Leiste zeichnet
                // den Knopf gar nicht erst (ihre eigene Regel). Der Weg steht einmal
                // in ErzeugerKostenwege; alle Erzeugerdialoge teilen ihn sich.
                ["KostenOeffnen"] = projektId > 0
                    ? new Func<ErzeugerZeile, bool, Task>(
                        (zeile, betrieb) => ErzeugerKostenwege.Kosten(
                            besitzer, projektId, DbWerte.ERZEUGER_STROMSPEICHER, zeile, betrieb))
                    : null,
                ["EnergiekostenOeffnen"] = projektId > 0
                    ? new Func<ErzeugerZeile, Task>(
                        zeile => ErzeugerKostenwege.Energiekosten(
                            besitzer, projektId, DbWerte.ERZEUGER_STROMSPEICHER, zeile))
                    : null,

                ["KostenInvestText"] = Text_("KDLG_KNOPF_INVEST", "Investitionskosten…"),
                ["KostenBetriebText"] = Text_("KDLG_KNOPF_BETRIEB", "Betriebskosten…"),
                ["KostenEnergieText"] = Text_("KDLG_KNOPF_ENERGIE", "Energiekosten…"),

                ["TitelText"] = Text_("SPD_TITEL", "Verwaltung Stromspeicher"),
                ["KopfbandText"] = Text_("SPD_KOPFBAND", "Geben Sie Daten der Stromspeicher ein"),
                ["LabelProjektliste"] = Text_("SPD_LBL_PROJEKTLISTE", "ausgewählte Stromspeicher:"),
                ["LabelKatalogliste"] = Text_("SPD_LBL_KATALOGLISTE", "Stromspeicher aus Datenbank:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("HZK_BTN_LOESCHEN", "Löschen"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["FrageLoeschen"] = Text_("HZK_FRAGE_LOESCHEN",
                    "Der Katalogeintrag \"{0}\" wird für ALLE Projekte gelöscht. Fortfahren?"),
                ["TitelLoeschen"] = Text_("HZK_TITEL_LOESCHEN", "Löschen"),

                // ET-5 (Anwenderentscheid 08.09.2026): Traegerwahl in der Katalog-Gliederung
                // Gruppe > Art, gespeichert je Anlage; der gewaehlte Traeger wird dem Projekt
                // zugeordnet (ausserhalb des Assistenten).
                ["Traegerkatalog"] = ErzeugerTraegerHuelle.Katalog(DbWerte.ERZEUGER_STROMSPEICHER),
                ["LabelTraegerGruppe"] = ErzeugerTraegerHuelle.LabelGruppe,
                ["LabelTraegerArt"] = ErzeugerTraegerHuelle.LabelArt,
                ["TraegerWechseln"] = new Action<ErzeugerZeile, int>(
                    (zeile, neu) =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        m.ID_Carrier = neu;
                        ErzeugerTraegerHuelle.Zuordnen(projektId, wizard, neu);
                    }),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Nimmt den Speicher auf (<c>btn_Hinzu_Click</c>, Z. 123): je Klick eine EIGENE
        /// Modellinstanz mit der STAMM-Id in <c>ID_SP</c>.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, int idType, bool mitKopie,
                                                  List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId,
                                                  Projektkopievormerkung vormerkung)
        {
            var stamm = new StromspeicherStammCtrl();
            stamm.ReadAll();

            StromspeicherModel satz = null;
            foreach (StromspeicherModel s in stamm.items)
                if (s.m_ID == stammId) { satz = s; break; }

            if (satz == null)
                return new AufnahmeErgebnis(null,
                    Text_("SPD_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Stromspeicher wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_SP = satz.m_ID,
                ID_Type = idType,
                Bezeichner = satz.m_szBezeichner,
                // ET-5: Vorgabe der Stromtraeger des Projekts.
                ID_Carrier = ErzeugerTraegerHuelle.Vorauswahl(DbWerte.ERZEUGER_STROMSPEICHER, 0, projektId)
            };

            // KATALOGAUSWAHL V1, STUFE 3: die Projektkopie sofort (Muster BHKW) - erst damit haben Bearbeiten…,
            // „Alle Daten" und der Rueckweg einen Projektsatz. CopyFromStamm teilt die Kopie gleichen Namens.
            if (mitKopie)
            {
                var projektCtrl = new StromspeicherCtrl();
                bool schonDa = projektCtrl.GetProjektId(satz.m_szBezeichner, projektId) > 0;
                int kopie = projektCtrl.CopyFromStamm(stammId, projektId);
                if (kopie <= 0)
                    return new AufnahmeErgebnis(null,
                        Text_("HZK_MSG_KOPIE_FEHLER", "Der Datensatz konnte nicht in das Projekt übernommen werden."), true);
                model.ID_SP = kopie;
                // Eine NEUE Kopie raeumt ein Abbrechen wieder ab (Projektkopievormerkung).
                if (!schonDa) vormerkung?.Angelegt(satz.m_szBezeichner, kopie);
            }

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model));
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        /// <summary>Die Summe der Kapazitaeten der Projektliste (Projektkopie bzw. Katalogsatz je Zeile).</summary>
        private static double SummeKapazitaet(int idType, bool mitKopie, List<WErzeugerModel> modelle)
        {
            double summe = 0;
            foreach (WErzeugerModel m in modelle)
                if (m.ID_Type == idType) summe += StromspeicherStammCtrl.Satz(mitKopie, m.ID_SP)?.m_Energie ?? 0;
            return summe;
        }

        /// <summary>Loescht einen Katalogsatz; leer = geloescht, sonst der Grund.</summary>
        private static string KatalogLoeschen(int id)
        {
            StromspeicherModel satz = StromspeicherStammCtrl.Satz(false, id);
            if (satz == null) return Text_("KBROW_MSG_LOESCHEN_FEHLER", "Der Datensatz konnte nicht gelöscht werden.");
            StromspeicherStammCtrl.SpeicherErgebnis e = StromspeicherStammCtrl.Loeschen(satz.m_szBezeichner);
            return e.Ok ? "" : e.Meldung;
        }

        private static ErzeugerZeile ZeileZu(WErzeugerModel m)
        {
            return new ErzeugerZeile
            {
                Schluessel = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                CarrierId = m.ID_Carrier,
                GeraetId = m.ID_SP
            };
        }


        /// <summary>
        /// Der Detailblock (<c>listBox_SP_SelectedIndexChanged</c>, Z. 206) eines Katalogsatzes oder einer
        /// Projektkopie; <c>null</c> = leerer Block.
        /// </summary>
        private static ErzeugerDetail DetailZu(StromspeicherModel s)
        {
            if (s == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            var felder = new List<(string, string)>
            {
                (Text_("SPD_LBL_TYP", "Typ:"), s.m_szTyp ?? ""),
                (Text_("SPD_LBL_LEISTUNG", "Leistung [kW]:"), s.m_Leistung.ToString()),
                (MyResource.Resource.SP_LABEL_ENERGIE, s.m_Energie.ToString()),
                (Text_("SPD_LBL_DEGRADATION", "Degradation [%/a]:"), s.m_Degradation.ToString()),
                (Text_("SPD_LBL_LADEZUSTAND", "Ladezustand [%]:"), s.m_Ladezustand.ToString()),
                (MyResource.Resource.SP_LABEL_MODULKOSTEN, s.m_Modulkosten.ToString())
            };

            return new ErzeugerDetail(s.m_szBezeichner ?? "", "", felder);
        }


        /// <summary>
        /// Der Uebersetzer, den <see cref="Katalogfilterprofil.MitVerwendung"/>
        /// entgegennimmt: Schluessel rein, Text raus — ein fehlender Schluessel bleibt
        /// als Schluessel stehen, damit er auffaellt (Muster
        /// <c>KatalogBrowserProfil.Finde</c>).
        /// </summary>
        private static string Text_(string schluessel)
        {
            return Text_(schluessel, schluessel);
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// Der Zeilenzähler eines Dialoglaufs. Der Vorläufer hatte hier keinen — er legte
        /// die Zeilen ohne <c>ID</c> an. Für die Zuordnung Zeile ↔ Modell braucht die
        /// Hülle einen eindeutigen Schlüssel, sonst wären zwei gleiche Speicher
        /// ununterscheidbar (Abweichung A-18).
        /// </summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }
    }
}
