using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Photovoltaik;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Geräteimports (Anwenderentscheide <b>W6‑O‑1</b> und
    /// <b>W6‑O‑3</b> vom 06.09.2026).
    ///
    /// <para><b>Sie löst zwei Hüllen ab</b> — <c>PvModulImportHuelle</c> (CEC und
    /// PVsyst-PAN, iU9‑W13.3) und <c>WechselrichterImportHuelle</c> (CEC, W6‑E‑2/S1.5).
    /// Beide waren Zeile für Zeile Zwillinge; verschieden waren nur die Dienste und der
    /// Schreibweg. Genau die stehen jetzt als Fallunterscheidung an EINER Stelle —
    /// <see cref="Gaben"/>.</para>
    ///
    /// <para><b>Die Datenbank- und Netzseite steht hier, nicht in der Komponente.</b>
    /// Die zwei CEC-Listen kommen aus <see cref="CECDataService"/> bzw.
    /// <see cref="CecWechselrichterDienst"/>, die PVsyst-Dateien aus
    /// <see cref="PanDataService"/> bzw. <see cref="OndWechselrichterDienst"/>, der
    /// Schreibweg aus <see cref="PhotovoltaikStammCtrl"/> bzw.
    /// <see cref="WechselrichterStammCtrl"/>.</para>
    ///
    /// <para><b>Der Netzabruf läuft in <c>Task.Run</c></b> (Risiko R‑W13‑3): Drei URLs
    /// mit je 45 Sekunden Zeitgrenze sind im schlechtesten Fall über zwei Minuten. Der
    /// Fortschrittsmelder samt Abbruch hängt am Baustein <c>Fortschritt</c>.</para>
    ///
    /// <para><b>Die Dienste leben so lange wie der Dialog</b> — sie entstehen in
    /// <see cref="Gaben"/> und nicht als statische Felder (Lehre aus Befund W13‑B46: Die
    /// PAN-Sitzungsliste überlebte das Schließen der Maske und den
    /// Projektwechsel).</para>
    /// </summary>
    internal static class ModulImportHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1 216 × 758).</summary>
        private static readonly Size MASS = new Size(1240, 800);

        /// <summary>
        /// Zeigt den Import als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.PvImport</c> bzw.
        /// <c>Masken.WechselrichterImport</c>).
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="art">Modul- oder Wechselrichterimport.</param>
        /// <param name="quelle">Die Quelle, deren Knopf hervorgehoben aufmacht.</param>
        /// <returns><c>true</c>, wenn etwas geschrieben wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, ModulImportArt art, string quelle)
        {
            bool ok = false;
            BlazorDialogForm<ModulImportDialog> dlg = null;

            ModulImportProfil profil = ModulImportProfil.Finde(art, ImportTexte.Zu);

            var werte = new Dictionary<string, object>(Gaben(art, quelle))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<ModulImportDialog>(profil.Titel, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente. Die Dienste entstehen HIER und leben damit
        /// so lange wie der Dialog.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(ModulImportArt art, string quelle)
        {
            ModulImportWege wege = art == ModulImportArt.Wechselrichter
                ? WechselrichterWege()
                : ModulWege();

            return new Dictionary<string, object>
            {
                ["Art"] = art,
                ["ProfilVorgabe"] = ModulImportProfil.Finde(art, ImportTexte.Zu),
                ["Quelle"] = string.IsNullOrEmpty(quelle) ? ModulImportProfil.QuelleCec : quelle,
                ["Wege"] = wege
            };
        }

        // =====================================================================
        //  Die Wege der PV-Module (CEC, CEC-Datei, PAN)
        // =====================================================================

        private static ModulImportWege ModulWege()
        {
            CECDataService cec = new CECDataService();
            PanDataService pan = new PanDataService();

            return new ModulImportWege
            {
                Netz = (schluessel, melder, abbruch) => Task.Run(async () =>
                {
                    var r = await cec.LoadDataAsync(melder, abbruch).ConfigureAwait(false);
                    return Module(cec, r.success, r.meldung);
                }, abbruch),

                DateiWaehlen = q => Waehlen(q, MyResource.Resource.PVIMP_TITEL),

                DateiLaden = (q, pfad) => Task.Run(() =>
                    q.Schluessel == ModulImportProfil.QuelleCecDatei
                        ? CecDatei(cec, pfad)
                        : PanDatei(pan, pfad)),

                Vorpruefen = satz => Task.Run(() => ModulVorpruefen((UnifiedModule)satz)),
                Anlegen = (satz, name) => Task.Run(() => ModulAnlegen((UnifiedModule)satz, name)),
                Ueberschreiben = (satz, id) => Task.Run(() => ModulUeberschreiben((UnifiedModule)satz, id)),
                Meldungstext = Meldungstext
            };
        }

        /// <summary>Die Modulliste des Dienstes als neutrale Satzliste.</summary>
        private static ImportLeseErgebnis Module(CECDataService dienst, bool erfolg, CecFortschritt meldung)
        {
            if (!erfolg) return new ImportLeseErgebnis(false, null, meldung);

            List<object> module = dienst.AllModules
                .Select(m => (object)UnifiedModule.FromPanCec(m)).ToList();
            return new ImportLeseErgebnis(true, module, meldung);
        }

        /// <summary>
        /// Die AUSGELIEFERTE CEC-Modulliste aus einer Datei (Anwenderentscheid W6‑O‑3,
        /// auf die Modulseite mitgenommen): derselbe Zerleger wie beim Netzabruf,
        /// nur ohne Netz.
        /// </summary>
        private static ImportLeseErgebnis CecDatei(CECDataService dienst, string pfad)
        {
            var r = dienst.LoadFromFile(pfad);
            return Module(dienst, r.success, r.meldung);
        }

        /// <summary>
        /// Liest eine <c>.pan</c>-Datei und nimmt sie in die Sitzungsliste auf.
        ///
        /// <para><b>ANSI (Windows-1252) ausdrücklich</b> — wörtlich wie der Vorläufer
        /// (<c>_btnPAN_Click</c> :622): PVsyst schreibt seine Dateien nicht in UTF‑8,
        /// und ein Herstellername mit Umlaut würde sonst zu U+FFFD. <b>Der Dateiname
        /// reist mit</b> (Befund W13‑B45).</para>
        /// </summary>
        private static ImportLeseErgebnis PanDatei(PanDataService dienst, string pfad)
        {
            try
            {
                string inhalt = File.ReadAllText(pfad, AnsiEncoding.Get());
                dienst.Einlesen(inhalt, Path.GetFileName(pfad));

                List<object> module = dienst.AllModules
                    .Select(m => (object)UnifiedModule.FromPanCec(m)).ToList();

                return new ImportLeseErgebnis(true, module,
                    new CecFortschritt("PAN_MSG_GELESEN",
                        module.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                return new ImportLeseErgebnis(false, null,
                    new CecFortschritt("PAN_MSG_LESEFEHLER", ex.Message));
            }
        }

        /// <summary>
        /// Die Vorprüfung des gewählten Moduls gegen <c>Tab_PV_STAMM</c> — dieselbe
        /// <see cref="DublettenPruefung"/> wie bei den vier VDI-Importen, nur mit genau
        /// EINEM Kandidaten.
        /// </summary>
        private static ImportVorpruefung ModulVorpruefen(UnifiedModule modul)
        {
            KatalogDefinition katalog = KatalogRegistry.Finde("PV");

            var kandidat = new ImportKandidat { Name = modul.Name, Tag = null };
            foreach (var paar in modul.Vergleichswerte(modul.Name))
                kandidat.Werte[paar.Key] = paar.Value;

            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                katalog, new List<ImportKandidat> { kandidat });

            PvModulPlausibilitaet.Befund plausi = PvModulPlausibilitaet.Pruefe(modul.NachModell());

            return new ImportVorpruefung(
                pruefungen.Count > 0 ? pruefungen[0].Befund : ImportBefund.Neu,
                pruefungen,
                DublettenPruefung.VergebeneNamen(katalog),
                plausi.Ok && plausi.Warnungen.Count == 0 ? "" : PvModulPlausibilitaet.Meldung(plausi),
                !plausi.Ok);
        }

        /// <summary>Legt das Modul als neuen Katalogsatz an.</summary>
        private static bool ModulAnlegen(UnifiedModule modul, string name)
        {
            try
            {
                PhotovoltaikModel model = modul.NachModell();
                if (!string.IsNullOrEmpty(name)) model.m_szName = name;
                return new PhotovoltaikStammCtrl().InsertFrom(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des PV-Moduls: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Aktualisiert genau die Importfelder des Bestandssatzes — Id, Bezeichner und
        /// Anwenderfelder bleiben stehen (Dublettenkonzept 4.2).
        /// </summary>
        private static bool ModulUeberschreiben(UnifiedModule modul, int bestandsId)
        {
            try
            {
                PhotovoltaikStammCtrl ctrl = new PhotovoltaikStammCtrl();
                modul.NachModell(ctrl);
                return ctrl.UpdateImport(bestandsId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des PV-Moduls: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        //  Die Wege der Wechselrichter (CEC, CEC-Datei, OND)
        // =====================================================================

        private static ModulImportWege WechselrichterWege()
        {
            var cec = new CecWechselrichterDienst();
            var ond = new OndWechselrichterDienst();

            return new ModulImportWege
            {
                Netz = (schluessel, melder, abbruch) => Task.Run(async () =>
                {
                    var r = await cec.LadenAsync(melder, abbruch).ConfigureAwait(false);
                    return Geraete(cec, r.Erfolg, r.Meldung);
                }, abbruch),

                DateiWaehlen = q => Waehlen(q, MyResource.Resource.WRK_IMP_TITEL),

                DateiLaden = (q, pfad) => Task.Run(() =>
                {
                    if (q.Schluessel == ModulImportProfil.QuelleCecDatei)
                    {
                        var r = cec.AusDatei(pfad);
                        return Geraete(cec, r.Erfolg, r.Meldung);
                    }

                    var o = ond.AusDatei(pfad);
                    return o.Erfolg
                        ? new ImportLeseErgebnis(true, ond.AlleGeraete.Cast<object>().ToList(), o.Meldung)
                        : new ImportLeseErgebnis(false, null, o.Meldung);
                }),

                Vorpruefen = satz => Task.Run(() => WrVorpruefen(satz)),
                Anlegen = (satz, name) => Task.Run(() => WrAnlegen(satz, name)),
                Ueberschreiben = (satz, id) => Task.Run(() => WrUeberschreiben(satz, id)),
                Meldungstext = Meldungstext
            };
        }

        /// <summary>Die Geräteliste des CEC-Dienstes als neutrale Satzliste.</summary>
        private static ImportLeseErgebnis Geraete(CecWechselrichterDienst dienst, bool erfolg,
                                                  CecFortschritt meldung)
        {
            return erfolg
                ? new ImportLeseErgebnis(true, dienst.AlleGeraete.Cast<object>().ToList(), meldung)
                : new ImportLeseErgebnis(false, null, meldung);
        }

        /// <summary>
        /// Der Katalogsatz zu einem Gerät — aus der CEC-Zeile oder aus der OND-Datei.
        /// <b>Die Quelle entscheidet den Satztyp, nicht die Ausprägung</b>; alles
        /// Weitere (Vorprüfung, Plausibilität, Schreibweg) ist danach gleich.
        /// </summary>
        private static WechselrichterModel Modell(object satz)
        {
            if (satz is CecWechselrichter cec) return cec.NachModell();
            if (satz is OndWechselrichter ond) return ond.NachModell();
            return null;
        }

        /// <summary>Die Vergleichswerte eines Geräts für die Dublettenprüfung.</summary>
        private static IDictionary<string, object> Vergleichswerte(object satz, string name)
        {
            if (satz is CecWechselrichter cec) return cec.Vergleichswerte(name);
            if (satz is OndWechselrichter ond) return ond.Vergleichswerte(name);
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Die Vorprüfung des gewählten Geräts gegen <c>Tab_Wechselrichter_STAMM</c> —
        /// dieselbe <see cref="DublettenPruefung"/> wie beim Modulimport.
        /// </summary>
        private static ImportVorpruefung WrVorpruefen(object satz)
        {
            WechselrichterModel m = Modell(satz);
            if (m == null) return new ImportVorpruefung(ImportBefund.Neu, null, null);

            KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");

            var kandidat = new ImportKandidat { Name = m.m_szName, Tag = null };
            foreach (KeyValuePair<string, object> paar in Vergleichswerte(satz, m.m_szName))
                kandidat.Werte[paar.Key] = paar.Value;

            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                katalog, new List<ImportKandidat> { kandidat });

            WechselrichterPlausibilitaet.Befund plausi = WechselrichterPlausibilitaet.Pruefe(m);

            return new ImportVorpruefung(
                pruefungen.Count > 0 ? pruefungen[0].Befund : ImportBefund.Neu,
                pruefungen,
                DublettenPruefung.VergebeneNamen(katalog),
                plausi.Ok && plausi.Warnungen.Count == 0
                    ? "" : WechselrichterPlausibilitaet.Meldung(plausi),
                !plausi.Ok);
        }

        /// <summary>Legt das Gerät als neuen Katalogsatz an.</summary>
        private static bool WrAnlegen(object satz, string name)
        {
            try
            {
                WechselrichterModel m = Modell(satz);
                if (m == null) return false;
                if (!string.IsNullOrEmpty(name)) m.m_szName = name;
                return new WechselrichterStammCtrl().InsertFrom(m);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des Wechselrichters: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Aktualisiert genau die Importfelder des Bestandssatzes — Id, Bezeichner,
        /// Beschreibung und die Anwenderkosten bleiben stehen (Dublettenkonzept 4.2).
        /// </summary>
        private static bool WrUeberschreiben(object satz, int bestandsId)
        {
            try
            {
                var ctrl = new WechselrichterStammCtrl();
                if (satz is CecWechselrichter cec) cec.NachModell(ctrl);
                else if (satz is OndWechselrichter ond) ond.NachModell(ctrl);
                else return false;

                return ctrl.UpdateImport(bestandsId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des Wechselrichters: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        //  Gemeinsames
        // =====================================================================

        /// <summary>
        /// Der Dateiwähler — HINTER dem Blazor-Ereignis (Befund W13‑B‑1, siehe
        /// <c>IDateiDienst</c>). Filter und Unterordner kommen aus der
        /// <see cref="ImportQuelle"/> des Profils; der Startordner ist der
        /// Herstellerdatenpfad der Einstellungen, in dem die Auslieferungsdateien
        /// <c>CEC Modules.csv</c> und <c>CEC Inverters.csv</c> liegen (W6‑O‑3).
        /// </summary>
        private static Task<string> Waehlen(ImportQuelle quelle, string titel)
        {
            string basis = Properties.Settings.Default.VDI3805Path ?? "";
            string ordner = string.IsNullOrEmpty(quelle.Unterordner)
                ? basis
                : Path.Combine(basis, quelle.Unterordner);

            return Dienste.Datei.DateiOeffnenAsync(titel, quelle.Dateifilter, ordner);
        }

        /// <summary>
        /// Übersetzt einen Meldungsschlüssel der Dienste (CEC, PAN, OND) in den Satz des
        /// Ressourcenkatalogs; ein unbekannter Schlüssel bleibt stehen.
        /// </summary>
        internal static string Meldungstext(CecFortschritt meldung)
        {
            string vorlage = MyResource.Resource.ResourceManager.GetString(meldung.Schluessel ?? "");
            if (string.IsNullOrEmpty(vorlage)) return meldung.Schluessel ?? "";

            return meldung.Werte.Length == 0
                ? vorlage
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, meldung.Werte);
        }
    }
}
