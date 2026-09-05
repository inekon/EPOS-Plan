using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Stromganglinien-Zuordnung (iU9-W12.5).
    ///
    /// <para><b>Sie schreibt nichts</b> — genau wie der Vorläufer. Die Liste kommt
    /// herein, wird an Ort und Stelle bearbeitet und geht zurück; abgelegt wird sie
    /// beim Aufrufer (<c>Form_Start.pBox_StromMessdaten_Click</c> und
    /// <c>StromganglinieKontextMenuCtrl.ContextMenuItemNeu_Click</c>, beide mit
    /// <c>WizardCtrl.Del_Stromganglinie</c> + <c>Add_Stromganglinie</c>). Das ist
    /// Risiko R‑W12‑4 des Wellenplans: Eine Hülle, die selbst ablegt, würde
    /// dieselbe Zuordnung zweimal schreiben.</para>
    ///
    /// <para><b>Die Ganglinien-Id holt die Hülle.</b> Die Komponente kennt den
    /// Katalog nur über Bezeichner; beim Zurückschreiben löst
    /// <see cref="StromganglinieStammCtrl.FindeStamm"/> ihn in die Id auf — die
    /// Abfrage, die bis iU9-W12.0g als konkatenierter <c>SELECT *</c> in der Maske
    /// stand (Befund W12-B4).</para>
    ///
    /// <para><b>Die Verwaltung ist eine ÜBERLAGERUNG, kein zweites Fenster.</b>
    /// „Bearbeiten…" zeigt <c>StromganglinieAdminDialog</c> in derselben WebView;
    /// die Hülle reicht dafür nur den Parametersatz
    /// <see cref="StromganglinieAdminHuelle.Gaben"/> durch (Risiko R2).</para>
    ///
    /// <para><b>Seit iU9-W16a.1 ist sie auch die ASSISTENTENSEITE 6</b> (Befund
    /// W12-O-3: <c>Wizard_Stromlastgang</c> war derselbe Vorgang für den Assistenten,
    /// nur mit zwei <c>ListBox</c> statt zweier Raster und mit einem konkatenierten
    /// <c>SELECT</c> darin). Es entsteht KEINE zweite Komponente — dieselbe
    /// <c>StromganglinieDialog</c> läuft mit <c>Wizard = true</c> (ohne Schlussleiste)
    /// in einer <see cref="BlazorAssistentSeite{TKomponente, TModell}"/>. Der
    /// Unterschied zum Dialogweg ist der RÜCKWEG: Dort schreibt
    /// <see cref="Zurueckschreiben"/> nach dem Schließen, hier nach jeder Änderung
    /// (Rückruf <c>Geaendert</c>) — der Assistent blättert weiter, es gibt kein
    /// Schließen.</para>
    /// </summary>
    internal static class StromganglinieHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 678 × 345).</summary>
        private static readonly Size MASS = new Size(880, 520);

        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — für den Dialog- wie für den
        /// Assistentenweg.
        /// </summary>
        /// <param name="projektId">Das Projekt (für die Zuordnungszeilen).</param>
        /// <param name="liste">
        /// Die geteilte Zuordnungsliste; sie wird an Ort und Stelle bearbeitet.
        /// </param>
        /// <param name="wizard">Assistentenbetrieb: keine OK/Abbrechen-Leiste.</param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int projektId, List<Z_ProjektStromganglinieModel> liste, bool wizard)
        {
            if (liste == null) throw new ArgumentNullException(nameof(liste));

            List<GanglinienProjektZeile> zeilen = Zeilen(liste);

            // iU9-W12-E-2: Der Vorrat gehoert zu DIESEM Dialog, nicht zur Klasse -
            // sonst hielten zwei nacheinander geoeffnete Dialoge dieselbe Reihe fest.
            Grafikvorrat vorrat = new Grafikvorrat();

            var werte = new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Katalog"] = new Func<Task<List<GanglinienKatalogZeile>>>(KatalogLesen),
                ["Verwaltung"] = StromganglinieAdminHuelle.Gaben(),

                // iU9-W12-E-1: die Datenbankseite des Dialogs. Die vier Wege sind
                // DIESELBEN, die die Verwaltung benutzt — Import, Löschen und Vorschau
                // kommen wörtlich von dort, damit es keinen zweiten Importweg gibt.
                ["DateiWaehlen"] = new Func<string, Task<string>>(StromganglinieAdminHuelle.DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(StromganglinieAdminHuelle.Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen,
                                        Task<GanglinienVorschau>>(StromganglinieAdminHuelle.Vorschau),
                ["Loeschen"] = new Func<string, Task<bool>>(StromganglinieAdminHuelle.Loeschen),
                ["HatProjektzuordnung"] = new Func<string, Task<bool>>(HatProjektzuordnung),
                ["Kopieren"] = new Func<string, string, Task<bool>>(Kopieren),

                // iU9-W12-E-2: die Grafik der markierten Ganglinie. Gerechnet wird im
                // Kern (StromganglinieAuswertungCtrl), gezeichnet auch
                // (ChartRenderer.GanglinieNormiert) - die Komponente bekommt Zahlen
                // und ein PNG.
                ["Kennzahlen"] = new Func<GanglinienWahl, Task<GanglinienKennzahlen>>(
                    w => Task.FromResult(vorrat.Kennzahlen(w))),
                ["Bildauftrag"] = new Func<GanglinienWahl, bool, Diagrammbereich, byte[]>(
                    (w, sortiert, bereich) => vorrat.Bild(w, sortiert, bereich)),

                // Die Anzeigeeinheit (Entscheid W8-O-5): dieselbe gemerkte Wahl wie in
                // den Bedarfsansichten.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib)
            };

            // Der Assistent schliesst nicht - er blaettert. Deshalb geht der Stand nach
            // JEDER Aenderung in die geteilte Liste zurueck; der Dialogweg tut es
            // einmal nach ShowDialog.
            if (wizard)
                werte["Geaendert"] = new Action(() => Zurueckschreiben(projektId, zeilen, liste));

            return werte;
        }

        /// <summary>Die Zuordnungsmodelle als Anzeigezeilen der Komponente.</summary>
        private static List<GanglinienProjektZeile> Zeilen(List<Z_ProjektStromganglinieModel> liste)
        {
            List<GanglinienProjektZeile> zeilen = new List<GanglinienProjektZeile>();
            foreach (Z_ProjektStromganglinieModel m in liste)
                zeilen.Add(new GanglinienProjektZeile(m.m_ID_Z, m.m_ID_Stromganglinie,
                                                      m.m_szStromganglinie ?? ""));
            return zeilen;
        }

        /// <summary>
        /// Zeigt die Zuordnung modal und schreibt die Liste an Ort und Stelle
        /// zurück.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Das Projekt.</param>
        /// <param name="liste">
        /// Die Zuordnungen des Projekts — sie wird bearbeitet, nicht kopiert.
        /// </param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<Z_ProjektStromganglinieModel> liste)
        {
            if (liste == null) throw new ArgumentNullException(nameof(liste));

            bool ok = false;
            BlazorDialogForm<StromganglinieDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(projektId, liste, wizard: false));
            List<GanglinienProjektZeile> zeilen = (List<GanglinienProjektZeile>)werte["Zeilen"];

            werte["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
            {
                ok = b;
                if (dlg != null) dlg.Schliessen(b);
            });

            dlg = new BlazorDialogForm<StromganglinieDialog>(
                MyResource.Resource.STROMGL_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            Zurueckschreiben(projektId, zeilen, liste);
            return ok;
        }

        /// <summary>
        /// Die bearbeitete Liste zurück in die Modelle des Aufrufers — AUCH bei
        /// Abbruch: Der Vorläufer führte dieselbe Liste, und die Aufrufer leiten
        /// ihren Kachelstatus UNABHÄNGIG vom Ergebnis aus <c>DateiListe.Count</c> ab
        /// (<c>Form_Start</c> :487-490).
        /// </summary>
        private static void Zurueckschreiben(int projektId,
                                             List<GanglinienProjektZeile> zeilen,
                                             List<Z_ProjektStromganglinieModel> ziel)
        {
            ziel.Clear();

            foreach (GanglinienProjektZeile z in zeilen)
            {
                int idGanglinie = z.GanglinieId;
                if (idGanglinie == 0)
                {
                    StromganglinieModel satz = StromganglinieStammCtrl.FindeStamm(z.Bezeichner);
                    if (satz == null) continue;      // der Katalogeintrag ist weg
                    idGanglinie = satz.ID;
                }

                ziel.Add(new Z_ProjektStromganglinieModel
                {
                    m_ID_Z = z.Schluessel,
                    m_ID_Projekt = projektId,
                    m_ID_Stromganglinie = idGanglinie,
                    m_szStromganglinie = z.Bezeichner
                });
            }
        }

        /// <summary>
        /// Der Katalog — <b>dieselbe</b> Quelle wie in der Verwaltung.
        ///
        /// <para><b>Bis iU9-W12-E-1 war es NICHT dieselbe</b>: Diese Hülle baute die
        /// Zeilen selbst und setzte das ReadOnly-Kennzeichen fest auf <c>false</c>
        /// (der Dialog brauchte es nicht). Seit der Dialog löschen kann, braucht er es
        /// — ein Auslieferungssatz wäre sonst nicht als solcher zu erkennen. Statt die
        /// Schleife ein zweites Mal zu schreiben, ruft sie jetzt
        /// <see cref="StromganglinieAdminHuelle.KatalogLesen"/>.</para>
        /// </summary>
        private static Task<List<GanglinienKatalogZeile>> KatalogLesen()
            => StromganglinieAdminHuelle.KatalogLesen();

        /// <summary>
        /// Gibt es zu dieser Katalogganglinie eine Projektzuordnung? (W12-E-1) —
        /// die erste der zwei Löschsperren.
        /// </summary>
        private static Task<bool> HatProjektzuordnung(string bezeichner)
            => Task.FromResult(new StromganglinieStammCtrl().HatProjektzuordnung(bezeichner));

        /// <summary>
        /// „Speichern unter": die Katalogganglinie unter neuem Namen (W12-E-1).
        /// <see cref="StromganglinieStammCtrl.KopiereStamm"/> prüft die Dublette
        /// selbst und liefert dann <c>0</c> — der Dialog meldet es als Banner.
        /// </summary>
        private static Task<bool> Kopieren(string quelle, string ziel)
            => Task.FromResult(new StromganglinieStammCtrl().KopiereStamm(quelle, ziel) > 0);

        // =================================================================
        // Die Grafik der markierten Ganglinie (W12-E-2)
        // =================================================================

        /// <summary>
        /// <b>Die gelesene Reihe der zuletzt markierten Ganglinie</b> (iU9-W12‑E‑2).
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
            private StromganglinieAuswertung _stand;

            /// <summary>Die drei Kennzahlen; <c>null</c> = keine brauchbare Reihe.</summary>
            internal GanglinienKennzahlen Kennzahlen(GanglinienWahl wahl)
            {
                StromganglinieAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;

                return new GanglinienKennzahlen(a.JahresarbeitMwh, a.SpitzeKw,
                                                a.VollbenutzungsstundenH);
            }

            /// <summary>
            /// Die Jahresganglinie — <b>dasselbe Bild wie B1 des Bedarfsreiters</b>
            /// (<c>SimulationErgebnisHuelle.BildBedarfStrom</c>): normiert auf den
            /// Jahreshöchstwert, x wahlweise Monatsgrenzen oder die vier Stundenmarken,
            /// Farbe Rot. Nur die Reihe ist eine andere — hier steht GENAU EINE, die
            /// gewählte Ganglinie.
            /// </summary>
            /// <param name="sortiert">Dauerlinie statt Ganglinie.</param>
            /// <param name="bereich">Der aufgezogene Bildausschnitt (Datenzoom, Befund
            /// A‑1); <c>null</c> = das ganze Jahr. Was an dieser Stelle des Bildes
            /// steht, weiß nur der Renderer — deshalb rechnet
            /// <c>ChartRenderer.FensterAusBild</c>.</param>
            internal byte[] Bild(GanglinienWahl wahl, bool sortiert, Diagrammbereich bereich)
            {
                StromganglinieAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;

                float[] werte = a.Stundenwerte;

                var reihen = new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(MyResource.Resource.CHART_ACHSE_STROMBEDARF,
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
                    MyResource.Resource.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE, reihen,
                    MyResource.Resource.CHART_ACHSE_STROMBEDARF,
                    sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                    sortiert, fenster);
            }

            /// <summary>Liest die Reihe — oder gibt die schon gelesene zurück.</summary>
            private StromganglinieAuswertung Lesen(GanglinienWahl wahl)
            {
                if (wahl == null) return null;

                string schluessel = (wahl.AusKatalog ? "K|" : "P|") + wahl.GanglinieId
                                    + "|" + wahl.Bezeichner;
                if (schluessel == _schluessel) return _stand;

                _schluessel = schluessel;
                _stand = wahl.AusKatalog
                    ? StromganglinieAuswertungCtrl.AusKatalog(wahl.Bezeichner)
                    : StromganglinieAuswertungCtrl.AusProjekt(wahl.GanglinieId, wahl.Bezeichner);
                return _stand;
            }
        }
    }
}
