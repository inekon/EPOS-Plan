using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des externen Wärmebedarfs (iU9-W9.4) — der Ersatz für
    /// <c>Form_Waermebedarf</c>.
    ///
    /// <para><b>Der KANAL wird beim Laden mitgeholt.</b> Der Vorläufer baute seine Liste
    /// aus ausgeschriebenen SELECT-Listen ohne den Kanal und ließ ihn erst in
    /// <c>SetControls</c> über <c>KanaeleNachladen</c> nachtragen — der Speicherweg der
    /// Zuordnung ist Löschen + Neuanlegen, ohne das Nachladen fiele jede Ganglinie auf
    /// Heizung zurück (Migrationsschritt 48, F18). Seit W9.0d tut das
    /// <c>Z_ProjektGebGanglinieCtrl.LiesProjekt</c> in EINEM Zug.</para>
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert</b> — wie in den Wellen 6 bis 9:
    /// Die Hülle baut die Fachliste nach jeder Änderung an Ort und Stelle neu auf.</para>
    /// </summary>
    internal static class WaermebedarfExternHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 828 × 443).</summary>
        private static readonly Size MASS = new Size(900, 560);

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>
        /// Zeigt die Ganglinien eines Projekts als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_WBedarfDaten_Click</c> und dem Kontextmenü.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjWaermebedarfModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<WaermebedarfExternDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, projektName, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermebedarfExternDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Die WÄRMEBEDARFSSEITE des Assistenten — dieselbe Komponente, randlos.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, string projektName,
            List<Z_ProjWaermebedarfModel> modelle, bool wizard)
        {
            // Der Kanal steht in der Zuordnung; die aufrufenden Wege bauen ihre Liste
            // teils ohne ihn. Nachtragen, wie es SetControls tat.
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(projektId, modelle);

            var zeilen = new List<WaermebedarfExternZeile>();
            foreach (Z_ProjWaermebedarfModel m in modelle) zeilen.Add(AusModell(m));

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (WaermebedarfExternZeile z in zeilen) modelle.Add(NachModell(z, projektId));
            };

            // iU9-W9-E-2: Der Vorrat gehoert zu DIESEM Dialog, nicht zur Klasse -
            // sonst hielten zwei nacheinander geoeffnete Dialoge dieselbe Reihe fest.
            Grafikvorrat vorrat = new Grafikvorrat();

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Geaendert"] = geaendert,

                // iU9-W9-E-3: DIESELBE Katalogquelle wie die Verwaltung - samt
                // ReadOnly-Kennzeichen. Bis dahin baute diese Huelle eine eigene
                // Namensliste, und ein Auslieferungssatz war im Dialog nicht als
                // solcher zu erkennen (derselbe Befund wie W12-E-1 beim Strom).
                ["Katalog"] = new Func<Task<List<WaermebedarfAdminDialog.Katalogzeile>>>(
                    WaermebedarfAdminHuelle.KatalogLesen),
                ["Aufnehmen"] = new Func<string, WaermebedarfExternZeile>(
                    name => Aufnehmen(name, projektId)),
                ["HatProjektzuordnung"] = new Func<string, bool>(
                    name => new WaermebedarfStammCtrl().HatProjektzuordnung(name)),
                ["KatalogLoeschen"] = new Func<string, bool>(
                    name => new WaermebedarfStammCtrl().Delete(name)),
                // iU9-W13.2: Die Ganglinienverwaltung ist selbst Blazor. Statt des
                // Sprungs in ein WinForms-Fenster bekommt der Dialog ihren
                // PARAMETERSATZ und zeigt sie als Ueberlagerung (Risiko R2).
                ["VerwaltungGaben"] = WaermebedarfAdminHuelle.Gaben(),

                // iU9-W9-E-3: die Datenbankseite der neuen Knopfleiste. Import und
                // Vorschau sind WOERTLICH die Wege der Verwaltung - damit es keinen
                // zweiten Importweg gibt.
                ["DateiWaehlen"] = new Func<string, Task<string>>(
                    WaermebedarfAdminHuelle.DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(
                    WaermebedarfAdminHuelle.Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen,
                                        Task<GanglinienVorschau>>(
                    WaermebedarfAdminHuelle.Vorschau),
                ["Kopieren"] = new Func<string, string, Task<bool>>(Kopieren),

                // iU9-W9-E-3: die Grafik der markierten Ganglinie. Gerechnet wird im
                // Kern (GanglinienAuswertungCtrl), gezeichnet auch
                // (ChartRenderer.GanglinieNormiert) - die Komponente bekommt Zahlen
                // und ein PNG.
                ["Kennzahlen"] = new Func<GanglinienWahl, Task<GanglinienKennzahlen>>(
                    w => Task.FromResult(vorrat.Kennzahlen(w))),
                ["Bildauftrag"] = new Func<GanglinienWahl, bool, Diagrammbereich, byte[]>(
                    (w, sortiert, bereich) => vorrat.Bild(w, sortiert, bereich)),

                // Die Anzeigeeinheit (Entscheid W8-O-5): dieselbe gemerkte Wahl wie in
                // den Bedarfsansichten.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),

                ["Kanaele"] = Kanaele(),

                ["TitelText"] = Titel(),
                ["KopfbandText"] = Text_("WBX_KOPFBAND", "Wärmebedarfsdaten (Ganglinien)"),
                ["LabelProjektliste"] = Text_("WBX_LBL_PROJEKTLISTE", "Ausgewählt im Projekt"),
                ["LabelKatalog"] = Text_("WBX_LBL_KATALOG", "Wärmebedarf aus DB"),
                ["KanalBezeichnung"] = MyResource.Resource.KANAL_LABEL,
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                // Entscheid #76 (05.09.2026): Das Zeichen setzt der Baustein
                // Zweispaltenauswahl je nach Anordnung - der Knopf traegt Klartext.
                ["BtnHinzuText"] = Text_("AUSWAHL_BTN_UEBERNEHMEN", "In das Projekt übernehmen"),
                ["BtnEntfernenText"] = Text_("AUSWAHL_BTN_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnBearbeitenText"] = Text_("WBX_BTN_BEARBEITEN", "Einlesen/Bearbeiten.."),
                ["BtnLoeschenText"] = Text_("WBX_BTN_LOESCHEN", "DB Ganglinie löschen"),
                ["BtnImportierenText"] = MyResource.Resource.STROMGL_BTN_IMPORTIEREN,
                ["BtnSpeichernUnterText"] = MyResource.Resource.STROMGL_BTN_SPEICHERN_UNTER,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["MeldungZuordnung"] = Text_("WBX_MSG_ZUORDNUNG",
                    "Es existiert eine Projektzuordnung, Löschen nicht möglich!"),
                ["MeldungLoeschfrage"] =
                    Text_("BPRO_FRAGE_LOESCHEN", "Soll {0} wirklich gelöscht werden ?"),

                ["HilfeSchluessel"] = "Form_Waermebedarf.btn_Help"
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// „Speichern unter": die Katalogganglinie unter neuem Namen (W9-E-3).
        /// <see cref="WaermebedarfStammCtrl.KopiereStamm"/> prüft die Dublette
        /// selbst und liefert dann <c>0</c> — der Dialog meldet es als Banner.
        /// </summary>
        private static Task<bool> Kopieren(string quelle, string ziel)
            => Task.FromResult(new WaermebedarfStammCtrl().KopiereStamm(quelle, ziel) > 0);

        /// <summary>
        /// „◀" — je Zuordnung ein EIGENES Modell mit der Stamm-Id der Ganglinie und dem
        /// Kanal Heizung (<c>btn_Hinzu_Click</c>:206-238).
        /// </summary>
        private static WaermebedarfExternZeile Aufnehmen(string name, int projektId)
        {
            return new WaermebedarfExternZeile
            {
                IdZ = 0,
                IdGanglinie = new WaermebedarfStammCtrl().GetStammId(name),
                Bezeichner = name ?? "",
                Kanal = DbWerte.KANAL_HEIZUNG
            };
        }

        /// <summary>
        /// Die drei Kanäle: Steuerwert und Anzeigetext getrennt — der Anzeigetext ist NIE
        /// Steuerwert (Drei-Schichten-Regel, <c>KanalItem</c> des Vorläufers).
        /// </summary>
        internal static (string Wert, string Text)[] Kanaele()
        {
            return new[]
            {
                (DbWerte.KANAL_HEIZUNG, MyResource.Resource.KANAL_HEIZUNG_ANZEIGE),
                (DbWerte.KANAL_BRAUCHWASSER, MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE),
                (DbWerte.KANAL_PROZESS, MyResource.Resource.KANAL_PROZESS_ANZEIGE)
            };
        }

        // =================================================================================
        // Abbildung Zeile <-> Modell
        // =================================================================================

        internal static WaermebedarfExternZeile AusModell(Z_ProjWaermebedarfModel m)
        {
            return new WaermebedarfExternZeile
            {
                IdZ = m.m_ID_Z,
                IdGanglinie = m.m_ID_Ganglinie,
                Bezeichner = m.m_szBezeichner ?? "",
                Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(m.Kanal)
            };
        }

        internal static Z_ProjWaermebedarfModel NachModell(WaermebedarfExternZeile z, int projektId)
        {
            return new Z_ProjWaermebedarfModel
            {
                m_ID_Z = z.IdZ,
                m_ID_Projekt = projektId,
                m_ID_Ganglinie = z.IdGanglinie,
                m_szBezeichner = z.Bezeichner,
                Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(z.Kanal)
            };
        }

        private static string Titel()
        {
            return Text_("WBX_TITEL", "Wärmebedarf Extern");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        // =================================================================
        // Die Grafik der markierten Ganglinie (W9-E-3)
        // =================================================================

        /// <summary>
        /// <b>Die gelesene Reihe der zuletzt markierten Ganglinie</b> (iU9-W9‑E‑3) —
        /// Zwilling von <c>StromganglinieHuelle.Grafikvorrat</c> aus W12‑E‑2.
        ///
        /// <para><b>Wozu.</b> Der Dialog fragt zweimal nach derselben Ganglinie: einmal
        /// nach den Kennzahlen (bei der Markierung) und danach bei jedem Umschalten von
        /// „sortiert" oder jedem aufgezogenen Ausschnitt nach einem Bild. Ohne diesen
        /// Vorrat läge zwischen jedem Bild ein <c>SELECT</c> über 8 760 bzw. 35 040
        /// Wertzeilen. Gehalten wird GENAU EINE Reihe — die gezeigte; wer die
        /// Markierung wechselt, verwirft die vorige.</para>
        ///
        /// <para><b>Warum je Dialog und nicht statisch.</b> Ein Feld dieser Hülle wäre
        /// über alle Dialoge und alle Projekte hinweg dasselbe; ein Vorrat je
        /// <see cref="Gaben"/>-Aufruf fällt mit seinem Dialog.</para>
        /// </summary>
        private sealed class Grafikvorrat
        {
            private string _schluessel;
            private GanglinienAuswertung _stand;

            /// <summary>Die drei Kennzahlen; <c>null</c> = keine brauchbare Reihe.</summary>
            internal GanglinienKennzahlen Kennzahlen(GanglinienWahl wahl)
            {
                GanglinienAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;

                return new GanglinienKennzahlen(a.JahresarbeitMwh, a.SpitzeKw,
                                                a.VollbenutzungsstundenH);
            }

            /// <summary>
            /// Die Jahresganglinie — <b>dasselbe Bild wie B1 des Bedarfsreiters</b>
            /// (<c>SimulationErgebnisHuelle.BildBedarfWaerme</c>) und wie im
            /// <c>GebaeudeBedarfDialog</c> (W9.8): normiert auf den Jahreshöchstwert,
            /// x wahlweise Monatsgrenzen oder die vier Stundenmarken, Farbe Rot. Nur
            /// die Reihe ist eine andere — hier steht GENAU EINE, die gewählte
            /// Ganglinie.
            /// </summary>
            /// <param name="wahl">Katalogsatz oder Projektkopie.</param>
            /// <param name="sortiert">Dauerlinie statt Ganglinie.</param>
            /// <param name="bereich">Der aufgezogene Bildausschnitt (Datenzoom, Befund
            /// A‑1); <c>null</c> = das ganze Jahr. Was an dieser Stelle des Bildes
            /// steht, weiß nur der Renderer — deshalb rechnet
            /// <c>ChartRenderer.FensterAusBild</c>.</param>
            internal byte[] Bild(GanglinienWahl wahl, bool sortiert, Diagrammbereich bereich)
            {
                GanglinienAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;

                float[] werte = a.Stundenwerte;

                var reihen = new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(MyResource.Resource.CHART_ACHSE_WAERMELAST,
                                            Array.ConvertAll(werte, x => (double)x),
                                            SkiaSharp.SKColors.Red)
                };

                ChartRenderer.Achsenfenster fenster = bereich == null
                    ? null
                    : ChartRenderer.FensterAusBild(
                        new ChartRenderer.Bildausschnitt(bereich.XVon, bereich.XBis,
                                                         bereich.YVon, bereich.YBis),
                        werte.Length);

                return ChartRenderer.GanglinieNormiert(
                    MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE, reihen,
                    MyResource.Resource.CHART_ACHSE_WAERMELAST,
                    sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                    sortiert, fenster);
            }

            /// <summary>Liest die Reihe — oder gibt die schon gelesene zurück.</summary>
            private GanglinienAuswertung Lesen(GanglinienWahl wahl)
            {
                if (wahl == null) return null;

                string schluessel = (wahl.AusKatalog ? "K|" : "P|") + wahl.GanglinieId
                                    + "|" + wahl.Bezeichner;
                if (schluessel == _schluessel) return _stand;

                _schluessel = schluessel;
                _stand = wahl.AusKatalog
                    ? GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Waermebedarf,
                                                          wahl.Bezeichner)
                    : GanglinienAuswertungCtrl.AusProjekt(GanglinienQuelle.Waermebedarf,
                                                          wahl.GanglinieId, wahl.Bezeichner);
                return _stand;
            }
        }
    }
}
