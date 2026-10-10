using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Pufferspeicher-Projektdialogs (iU9-W6.7).
    ///
    /// <para><b>Zwei Besonderheiten gegenüber den vier Schwestern.</b></para>
    /// <list type="number">
    /// <item><b>Die Eindeutigkeitsfrage.</b> Steht derselbe Speicher schon in der Liste,
    /// fragt der Dialog nach, BEVOR er ihn aufnimmt — der Anwender soll die Meldung
    /// sehen, während er den Speicher aufnimmt, nicht erst beim Speichern. Die Antwort
    /// wandert als <c>GeraetekopieErzwingen</c> ins Modell, damit der Schreibweg nicht
    /// ein zweites Mal fragt. Die Prüfung selbst macht <see cref="AnlagenEindeutigkeit"/>
    /// auf der geteilten Liste; sie ist reine Listenarbeit und braucht keine
    /// Datenbank.</item>
    /// <item><b>Zwei Detailquellen.</b> Eine Projektzeile zeigt ihre KOPIE aus
    /// <c>Tab_Pufferspeicher</c> — sie kann anders heißen als die Vorlage („… 600 Liter"
    /// gegen „… 600 Ltr") und im selben Projekt doppelt vorkommen. Eine frisch
    /// hinzugefügte Zeile hat noch keine Kopie; dort steht in <c>ID_PUFFER</c> die
    /// STAMM-Id, und der Rückfall greift.</item>
    /// </list>
    ///
    /// <para><b>Keine Assistentenseite.</b> Der Pufferspeicher steht nicht in den
    /// dreizehn Seiten des Assistenten (FR‑1) — deshalb gibt es hier kein
    /// <c>AssistentSeite()</c>.</para>
    /// </summary>
    internal static class PufferspeicherHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 785 × 553).</summary>
        private static readonly Size MASS = new Size(900, 620);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Pufferspeicher_Click</c> und
        /// <c>PufferSpKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<PufferspeicherDialog> dlg = null;
            PufferAuslegungAuftrag auslegen = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                }),

                // Stufe P2 (Einstieg A): „Auslegen…" schliesst die Verwaltung wie Abbrechen und
                // oeffnet danach die Pufferspeicher-Auslegung als freie Ansicht im Hauptfenster -
                // fuer die Projektkopie der gewaehlten Zeile, ohne sie fuer einen neuen Speicher.
                ["AuslegenOeffnen"] = new Func<int, Task>(geraetId =>
                {
                    bool kopie = geraetId > 0 && PufferSpCtrl.Detail(geraetId, projektId) != null;
                    auslegen = new PufferAuslegungAuftrag
                    {
                        IdProjekt = projektId,
                        IdPuffer = kopie ? geraetId : (int?)null,
                        Einstieg = MyResource.Resource.PAUS_EINSTIEG_VERWALTUNG
                    };
                    ok = false;
                    if (dlg != null) dlg.Schliessen(false);
                    return Task.CompletedTask;
                }),
                ["BtnAuslegenText"] = MyResource.Resource.PAUS_BTN_AUSLEGEN,
                ["HinweisAuslegenText"] = MyResource.Resource.PAUS_AUSLEGEN_VERWIRFT
            };

            dlg = new BlazorDialogForm<PufferspeicherDialog>(
                Text_("PSPD_TITEL", "Verwaltung Pufferspeicher"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            // Erst NACH dem Schliessen: Die Wurzel wechselt die Ansicht im Hauptfenster.
            if (auslegen != null) PufferAuslegungHuelle.Oeffnen(auslegen);
            return ok;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType, List<WErzeugerModel> modelle)
        {
            var stamm = new PufferSpStammCtrl();

            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                zeilen.Add(ZeileZu(m));
                zuModell[m.ID] = m;
            }

            // Die Zeilen, die dieser Dialog frisch aufnimmt: Ihre Projektkopie entsteht erst beim Speichern der
            // Konfiguration - bis dahin zeigt GeraetId auf den KATALOG (Katalogauswahl V1, Stufe 3).
            var neu = new HashSet<int>();
            Func<ErzeugerZeile, bool> hatKopie = z => projektId > 0 && z.GeraetId > 0 && !neu.Contains(z.Schluessel)
                                                      && PufferSpCtrl.Detail(z.GeraetId, projektId) != null;
            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                // W14a-E-10 / S2.1: DAS PROFIL statt Herstellerklappliste und
                // Volumenstufen, plus die Spalte "im Projekt verwendet" (Q12).
                ["Katalogprofil"] = Katalogfilterprofil.MitVerwendung(
                    Anlagenart.Pufferspeicher, Text_),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Pufferspeicher, n, Text_)),

                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    PufferSpStammCtrl.Katalogfilterzeilen),

                ["KatalogDetail"] = new Func<int, ErzeugerDetail>(
                    id => DetailZu(PufferSpStammCtrl.Detail(id))),

                // listBox_PufferSp_SelectedIndexChanged (Z. 231): erst die Projektkopie,
                // dann der Katalogsatz. Frisch hinzugefuegte Zeilen tragen in ID_PUFFER
                // noch die STAMM-Id - die Kopie legt erst WizardCtrl beim Speichern an.
                ["ProjektDetail"] = new Func<int, ErzeugerDetail>(
                    id => DetailZu(PufferSpCtrl.Detail(id, projektId)
                                   ?? PufferSpStammCtrl.Detail(id))),

                ["Dublettenfrage"] = new Func<int, string>(
                    stammId => Dublettenfrage(idType, modelle, stammId)),

                ["Aufnehmen"] = new Func<int, bool, AufnahmeErgebnis>(
                    (stammId, erzwingen) =>
                    {
                        AufnahmeErgebnis e = Aufnehmen(projektId, idType, modelle, zuModell, zaehler, stammId, erzwingen);
                        if (e.Zeile != null) neu.Add(e.Zeile.Schluessel);
                        return e;
                    }),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(zeile.Schluessel);
                    }),

                ["KatalogLoeschen"] = new Func<int, string>(id => KatalogLoeschen(stamm, id)),

                // KATALOGAUSWAHL V1, STUFE 3 (Konzept 4.9): Summe Volumen in der Projekt-Kopfleiste, der volle
                // Katalogeditor fuer einen ungesperrten Satz (KA-E-13; die Speicherverwaltung als Ueberlagerung
                // entfaellt, sie bleibt im Menue), Bearbeiten je Bereich (KA-E-8) und der Rueckweg (KA-E-9) -
                // die beiden letzten nur fuer Zeilen mit Projektkopie.
                ["SummeVolumen"] = new Func<string>(
                    () => SummeVolumen(projektId, idType, modelle, hatKopie)
                              .ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)),
                ["LabelSumme"] = Text_("PSPD_LBL_SUMME", "Summe Volumen [l]:"),
                ["HatProjektkopie"] = hatKopie,
                ["HinweisOhneKopie"] = Text_("PSPD_HINWEIS_OHNE_KOPIE",
                    "Die Projektkopie dieses Speichers entsteht mit OK; erst dann lässt sie sich bearbeiten oder in die Datenbank übernehmen."),
                ["EditorGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => OhneRahmen(PufferSpAdminHuelle.EditorGaben(name, false, _ => { }))),
                ["EditorTitel"] = MyResource.Resource.PSPK_TITEL,
                ["ProjektsatzWege"] = projektId <= 0 ? null : new Satzbearbeitungswege
                {
                    Lesen = id => KatalogBrowserHuelle.Felder(PufferSpAdminHuelle.Profil(), PufferSpStammCtrl.SatzAnzeige(true, id)),
                    Speichern = saetze => PufferSpAdminHuelle.SammelSchreiben(true, saetze)
                },
                ["KatalogsatzWege"] = new Satzbearbeitungswege
                {
                    Lesen = id => KatalogBrowserHuelle.Felder(PufferSpAdminHuelle.Profil(), PufferSpStammCtrl.SatzAnzeige(false, id)),
                    Speichern = saetze => PufferSpAdminHuelle.SammelSchreiben(false, saetze)
                },

                ["TitelText"] = Text_("PSPD_TITEL", "Verwaltung Pufferspeicher"),
                ["KopfbandText"] = Text_("PSPD_KOPFBAND", "Geben Sie die Daten der Pufferspeicher ein"),
                ["LabelProjektliste"] = Text_("PSPD_LBL_PROJEKTLISTE", "ausgewählt im Projekt"),
                ["LabelKatalogliste"] = Text_("PSPD_LBL_KATALOGLISTE", "Pufferspeicher aus Datenbank"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("HZK_BTN_LOESCHEN", "Löschen"),
                ["LabelAlleParameter"] = Text_("HZK_LBL_ALLE_DATEN", "Alle Daten anzeigen"),

                // DIE ZWEI WEGE DES MODULAUFKLAPPERS (Anwenderentscheid 15.09.2026).
                // Sie kommen aus derselben Quelle wie die der Speicherverwaltung - der
                // Aufklapper IST deren Raster; der Speicherweg steht seit demselben Tag
                // im Kern (PufferSpStammCtrl.AnzeigefelderSchreiben).
                // SCHLOSS SETZEN / AUFHEBEN an der Katalogliste (AD-Q15) - derselbe Weg wie in
                // der Verwaltung. Die Verwendung im Projekt sperrt nichts (eigene Kopie).
                ["Schloss"] = Schlosswege.Aus(PufferSpStammCtrl.SchlossSetzen),

                ["Katalogfelder"] = new Func<string, IReadOnlyList<BrowserFeldwert>>(
                    name => PufferSpAdminHuelle.Wege().Detail!(name)!),
                ["KatalogfelderSpeichern"] =
                    new Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>(
                        (name, felder) => PufferSpAdminHuelle.Wege().Speichern!(name, felder)),
                ["BtnFelderSpeichernText"] = Text_("HZK_BTN_FELDER_SPEICHERN", "Speichern"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["TitelDublette"] = MyResource.Resource.ANL_DUBLETTE_TITEL,
                ["TitelLoeschen"] = MyResource.Resource.PSP_TITEL_KATALOG_LOESCHUNG,
                ["FrageLoeschen"] = MyResource.Resource.PSP_MELDUNG_KATALOG_LOESCHEN,

                // DIE KOSTENKNOEPFE IM MODULBEREICH (Anwenderentscheid 15.09.2026:
                // "alle sechs Erzeuger im gleichen Schema"). Der Weg ist derselbe, den
                // Heizkessel und BHKW gehen - ErzeugerKostenwege nimmt die
                // Kostenkomponente als Zeichenkette entgegen; der Pufferspeicher ist
                // kein Erzeuger und traegt deshalb seinen eigenen Wert
                // (DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER).
                //
                // NUR ZWEI KNOEPFE: "Energiekosten…" fuehrt in die
                // Energietraegerverwaltung, und ein Speicher verbraucht keinen Traeger -
                // ohne Delegat zeichnet die Leiste den Knopf gar nicht erst (ihre
                // eigene Regel). Ohne Projekt gibt es ueberhaupt keinen Kostenkontext.
                ["KostenOeffnen"] = projektId > 0
                    ? new Func<ErzeugerZeile, bool, Task>(
                        (zeile, betrieb) => ErzeugerKostenwege.Kosten(
                            besitzer, projektId, DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
                            zeile, betrieb))
                    : null,
                ["KostenInvestText"] = Text_("KDLG_KNOPF_INVEST", "Investitionskosten…"),
                ["KostenBetriebText"] = Text_("KDLG_KNOPF_BETRIEB", "Betriebskosten…")
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Die Frage „dasselbe Gerät steht schon in der Liste" — leer, wenn es sie nicht
        /// gibt.
        /// </summary>
        /// <remarks>
        /// <b>Warum der Text hier zusammengesetzt wird.</b>
        /// <c>AnlagenEindeutigkeit.ZweitesGeraetBestaetigen</c> STELLT die Frage über den
        /// statischen Delegaten <c>Frage</c> — das ist im Engine-Modus die Konsole und
        /// unter Windows eine <c>MessageBox</c>. Eine Razor-Komponente will die Frage
        /// selbst stellen (Baustein <c>Rueckfrage</c>), also braucht sie den TEXT, nicht
        /// die Handlung. Er kommt aus denselben zwei Ressourcenschlüsseln.
        /// </remarks>
        /// <summary>
        /// Löscht einen Katalogsatz samt Satzvorlagen (KA‑E‑16, <c>PufferSpStammCtrl.Delete</c>, der eine Vorlage, die
        /// Projektzeilen noch brauchen, als Hinweis nennt). Leere Rückgabe = gelöscht; sonst der Grund.
        /// </summary>
        private static string KatalogLoeschen(PufferSpStammCtrl stamm, int id)
        {
            if (PufferSpStammCtrl.IsReadOnlyStatic(id))
                return Text_("KBROW_MSG_SCHUTZ_LOESCHEN",
                    "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.");
            return stamm.Delete(id) ? ""
                : Text_("HZK_MSG_LOESCHFEHLER", "Der Katalogeintrag konnte nicht gelöscht werden.");
        }

        /// <summary>
        /// Die Summe der Gesamtvolumina der Projektliste in Litern: je Zeile die Projektkopie, ohne Kopie (frisch
        /// aufgenommen) der Katalogsatz.
        /// </summary>
        private static double SummeVolumen(int projektId, int idType, List<WErzeugerModel> modelle,
                                           Func<ErzeugerZeile, bool> hatKopie)
        {
            double summe = 0;
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                PufferSpStammCtrl.SpeicherDetail d = hatKopie(ZeileZu(m))
                    ? PufferSpCtrl.Detail(m.ID_PUFFER, projektId)
                    : PufferSpStammCtrl.Detail(m.ID_PUFFER);
                if (d != null && Program.ZahlParsen(d.Gesamtvolumen, out double v)) summe += v;
            }
            return summe;
        }

        /// <summary>Die Gaben des Katalogeditors ohne Titel und ohne eigenen Rückruf - beides setzt der Dialog.</summary>
        private static IReadOnlyDictionary<string, object> OhneRahmen(IReadOnlyDictionary<string, object> gaben)
        {
            var kopie = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in gaben)
                if (kv.Key != "Geschlossen" && kv.Key != "TitelText") kopie[kv.Key] = kv.Value;
            return kopie;
        }

        private static string Dublettenfrage(int idType, List<WErzeugerModel> modelle, int stammId)
        {
            string bezeichner = PufferSpStammCtrl.Detail(stammId)?.Bezeichner ?? "";
            if (bezeichner.Length == 0) return "";

            if (!AnlagenEindeutigkeit.BereitsInListe(modelle, idType, bezeichner)) return "";

            return string.Format(MyResource.Resource.ANL_DUBLETTE_FRAGE, bezeichner.Trim());
        }

        /// <summary>
        /// Nimmt den Speicher auf (<c>btn_PufferSp_Hinzu_Click</c>, Z. 151): keine
        /// Projektkopie, <c>ID_PUFFER</c> ist die STAMM-Id.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, int idType,
                                                  List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId, bool erzwingen)
        {
            PufferSpStammCtrl.SpeicherDetail satz = PufferSpStammCtrl.Detail(stammId);
            if (satz == null)
                return new AufnahmeErgebnis(null,
                    Text_("PSPD_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Pufferspeicher wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_PUFFER = stammId,
                ID_Type = idType,
                Bezeichner = satz.Bezeichner,

                // Antwort des Anwenders weitergeben - der Schreibweg fragt sonst erneut.
                GeraetekopieErzwingen = erzwingen
            };

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model));
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        private static ErzeugerZeile ZeileZu(WErzeugerModel m)
        {
            return new ErzeugerZeile
            {
                Schluessel = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                GeraetId = m.ID_PUFFER
            };
        }

        /// <summary>
        /// Der Detailblock. Die Zahlen kommen bereits als Text mit einer Nachkommastelle
        /// aus dem Kern (<c>FeldText</c>) — genau wie im Vorläufer.
        /// </summary>
        private static ErzeugerDetail DetailZu(PufferSpStammCtrl.SpeicherDetail d)
        {
            if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            // HIER STAND DAS FELD „Investitionskosten [€]:" (PSPD_LBL_INVEST) — nur
            // lesbar, als letzte Zeile des Blocks. Anwenderentscheid 21.09.2026: „Die
            // Anzeige der Investitionskosten an dieser Stelle hat keine Funktion."
            // Gepflegt wird der Preis im Aufklapper „Alle Daten anzeigen" desselben
            // Dialogs: Der Katalogbrowser zeigt ihn dort editierbar und schreibt ihn
            // zurück (KatalogBrowserProfil.FeldInvestitionskosten). Die Anzeige hier war
            // damit eine Dublette ohne Funktion.
            //
            // DER WERT BLEIBT IM DATENSATZ. PufferSpStammCtrl.SpeicherDetail führt
            // Investitionskosten weiter — nur die zweite Anzeige ist weg.
            var felder = new List<(string, string)>
            {
                (Text_("PSPD_LBL_HERSTELLER", "Hersteller:"), d.Hersteller),
                (Text_("PSPD_LBL_TYP", "Speichertyp:"), d.Typ),
                (Text_("PSPD_LBL_VERLUSTE", "Bereitschaftsverluste:"), d.Bereitschaftsverluste),
                (Text_("PSPD_LBL_VOLUMEN", "Gesamtvolumen [l]:"), d.Gesamtvolumen)
            };

            return new ErzeugerDetail(d.Bezeichner, "", felder);
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

        /// <summary>Der Zeilenzähler eines Dialoglaufs (Vorbild <c>startindex</c>).</summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }
    }
}
