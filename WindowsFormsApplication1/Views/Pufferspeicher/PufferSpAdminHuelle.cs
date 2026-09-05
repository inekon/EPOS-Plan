using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Pufferspeicher-Katalogbrowsers (iU9-W14a.1,
    /// Ausprägung <see cref="KatalogBrowserArt.Pufferspeicher"/>).
    ///
    /// <para>Vorbild <c>Views/Pufferspeicher/Form_PufferSp_Admin</c> (213 Z.) — im
    /// selben Schritt gelöscht (Regel M1), zusammen mit ihrem Katalogeditor
    /// <c>Form_PufferSp_Bearbeiten</c> (354 Z.), dessen einziger Aufrufer sie war. Der
    /// Editor ist seit W14a.2 die Razor-Komponente
    /// <see cref="PufferSpKatalogDialog"/> und erscheint hier als Überlagerung.</para>
    ///
    /// <para><b>Die einzige Ausprägung mit einem zweiten Zustand.</b>
    /// <see cref="Oeffnen(IWin32Window, bool)"/> mit <c>nurLesen: true</c> sperrt
    /// „Neu…", „Bearbeiten…" und „Löschen" und lässt Liste und Detailblock stehen —
    /// wortgleich <c>Form_PufferSp_Admin.m_bReadOnly</c>, das der Projektdialog über
    /// <c>Sprungziel.PufferSpAdminNurLesen</c> setzte (W10a.0c).</para>
    ///
    /// <para><b>Mit dieser Maske fällt der letzte „unklar"-Zustand des Bestands</b>
    /// (Befund W14-B25): <c>Form_PufferSp_Admin</c> schaltete <c>btn_Neu</c> und
    /// <c>btn_Bearbeiten</c> in einem Zweig ab und nie wieder ein, und deshalb galt der
    /// Weg zu ihrem Kind als zweifelhaft.</para>
    /// </summary>
    internal static class PufferSpAdminHuelle
    {
        /// <summary>Zeigt den Katalogbrowser als eigenes Fenster (<c>Masken.PufferSpAdmin</c>).</summary>
        /// <param name="nurLesen">
        /// <c>true</c> sperrt die drei Bearbeitungsknöpfe — der Weg aus dem
        /// Projektdialog, der den Auslieferungskatalog nur nachschlägt.
        /// </param>
        internal static bool Oeffnen(IWin32Window besitzer, bool nurLesen = false)
        {
            return KatalogBrowserHuelle.Oeffnen(besitzer, Profil(), Gaben(nurLesen));
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static KatalogBrowserProfil Profil()
        {
            return KatalogBrowserProfil.Finde(KatalogBrowserArt.Pufferspeicher, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(bool nurLesen = false)
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new PufferSpStammCtrl();

            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil, nurLesen);

            gaben["FilterEins"] = KatalogBrowserHuelle.MitAlle(PufferSpStammCtrl.Hersteller());
            gaben["FilterZwei"] = KatalogBrowserHuelle.Nummeriert(PufferSpStammCtrl.VolumenTexte());

            gaben["Wege"] = new KatalogBrowserWege
            {
                Liste = (hersteller, volumen) => Zeilen(ctrl, hersteller, volumen),
                Detail = name => KatalogBrowserHuelle.Felder(
                    profil, PufferSpStammCtrl.KatalogsatzAnzeige(name)),

                // Befund W14-B27: Der Vorlaeufer prueft mit inline-SQL, obwohl
                // PufferSpStammCtrl.Exists im Kern liegt und die Schwestermaske sie nutzt.
                Existiert = name => new PufferSpStammCtrl().Exists(name),
                Loeschen = Loeschen
            };

            gaben["EditorInhalt"] = KatalogBrowserHuelle.Editor<PufferSpKatalogDialog>();
            gaben["EditorGaben"] = new Func<string, bool, Action<string>,
                                            IReadOnlyDictionary<string, object>>(EditorGaben);
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static IReadOnlyList<BrowserZeile> Zeilen(PufferSpStammCtrl ctrl,
                                                          int hersteller, int volumen)
        {
            IReadOnlyList<string> namen = PufferSpStammCtrl.Hersteller();
            string h = hersteller <= 0 || hersteller > namen.Count ? "" : namen[hersteller - 1];

            var liste = new List<BrowserZeile>();
            foreach (var z in ctrl.Filtern(h, volumen))
                liste.Add(new BrowserZeile(z.Id, z.Bezeichner));
            return liste;
        }

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            PufferSpStammCtrl.SpeicherErgebnis e = PufferSpStammCtrl.Loeschen(name);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        // =====================================================================
        // Der Katalogeditor (W14a.2)
        // =====================================================================

        /// <summary>
        /// Der Parametersatz des Pufferspeicher-Katalogeditors. Hier — und nur hier —
        /// sind der Feldsatz der Oberfläche (<see cref="PufferSpKatalogDaten"/>) und der
        /// des Kerns (<c>PufferSpModel</c>) zugleich sichtbar.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> EditorGaben(string name, bool neu,
                                                                        Action<string> fertig)
        {
            var daten = new PufferSpKatalogDaten { Name = name ?? "" };
            var typen = new List<(int, string)>
            {
                (0, MyResource.Resource.PSPK_TYP_SOLAR),
                (1, MyResource.Resource.PSPK_TYP_PUFFER),
                (2, MyResource.Resource.PSPK_TYP_KOMBI)
            };

            if (neu)
            {
                // Vorbelegungen von MODE_NEU (Form_PufferSp_Bearbeiten Z. 78-82):
                // kein Speichertyp, kein Hersteller, drei Nullen.
                daten.SpeichertypIndex = null;
                daten.Firma = "";
                daten.Bereitschaftsverluste = 0;
                daten.Investitionskosten = 0;
                daten.Gesamtvolumen = 0;
            }
            else
            {
                AusKatalog(daten, typen, name);
            }

            var gaben = new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = neu ? KatalogModus.Neu : KatalogModus.Bearbeiten,
                ["Speichertypen"] = typen,

                ["Ueberschreiben"] = new Func<PufferSpKatalogDaten, KatalogSpeicherErgebnis>(
                    d => Uebersetzen(PufferSpStammCtrl.Ueberschreiben(NachModell(d, typen)))),
                ["Anlegen"] = new Func<PufferSpKatalogDaten, string, KatalogSpeicherErgebnis>(
                    (d, n) => Uebersetzen(PufferSpStammCtrl.Anlegen(NachModell(d, typen), n))),

                ["TitelText"] = MyResource.Resource.PSPK_TITEL,
                ["GruppeBezeichnung"] = MyResource.Resource.PSPK_GRP_BEZEICHNUNG,
                ["LabelName"] = MyResource.Resource.PSPK_LBL_NAME,
                ["LabelHersteller"] = MyResource.Resource.PSPK_LBL_HERSTELLER,
                ["LabelSpeichertyp"] = MyResource.Resource.PSPK_LBL_SPEICHERTYP,
                ["GruppeTechnik"] = MyResource.Resource.PSPK_GRP_TECHNIK,
                ["LabelVerluste"] = MyResource.Resource.PSPK_LBL_VERLUSTE,
                ["FeldVerluste"] = MyResource.Resource.PSPK_FELD_VERLUSTE,
                ["LabelVolumen"] = MyResource.Resource.PSPK_LBL_VOLUMEN,
                ["FeldVolumen"] = MyResource.Resource.PSPK_FELD_VOLUMEN,
                ["GruppeKosten"] = MyResource.Resource.PSPK_GRP_KOSTEN,
                ["LabelInvest"] = MyResource.Resource.PSPK_LBL_INVEST,
                ["FeldInvest"] = MyResource.Resource.PSPK_FELD_INVEST,
                ["BtnUeberschreibenText"] = MyResource.Resource.HZKK_BTN_UEBERSCHREIBEN,
                ["BtnSpeichernUnterText"] = MyResource.Resource.HZKK_BTN_SPEICHERN_UNTER,
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungZahlUngueltig"] = MyResource.Resource.HZKK_MSG_ZAHL,
                ["MeldungNameFehlt"] = MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                ["HilfeSchluessel"] = "Form_PufferSp_Bearbeiten.btn_Help",

                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), fertig)
            };
            return gaben;
        }

        /// <summary>
        /// Kern → Oberfläche. Trägt der Satz einen Speichertyp, den keiner der drei
        /// trifft, hängt die Hülle ihn als VIERTEN Auswahleintrag an — der Ersatz für
        /// den Freitext der editierbaren <c>ComboBox</c>. So verschwindet ein
        /// Fremdimport nicht stillschweigend (wortgleich
        /// <c>SpeichertypAnzeigen</c> Z. 120-133).
        /// </summary>
        private static void AusKatalog(PufferSpKatalogDaten daten,
                                       List<(int, string)> typen, string name)
        {
            // Ueber KatalogsatzAnzeige statt ueber ein zweites SQL: Die Methode liefert
            // dieselben sechs Felder parametrisiert und im Rohformat der Datenbank
            // (W14a.0c); die Zahlregel des Hauses liest sie zurueck.
            IReadOnlyDictionary<string, string> satz = PufferSpStammCtrl.KatalogsatzAnzeige(name);
            if (satz == null) return;

            daten.Firma = Feld(satz, KatalogBrowserProfil.FeldFirma);
            daten.Bereitschaftsverluste = Kommazahl(satz, KatalogBrowserProfil.FeldVerluste);
            daten.Gesamtvolumen = Ganzzahl(satz, KatalogBrowserProfil.FeldVolumen);
            daten.Investitionskosten = Kommazahl(satz, KatalogBrowserProfil.FeldInvestitionskosten);

            var anzeige = new List<string>();
            foreach (var t in typen) anzeige.Add(t.Item2);

            string gespeichert = Feld(satz, KatalogBrowserProfil.FeldSpeichertyp);
            int index = PufferSpStammCtrl.SpeichertypIndex(gespeichert, anzeige);
            if (index >= 0)
            {
                daten.SpeichertypIndex = index;
                return;
            }

            string roh = (gespeichert ?? "").Trim();
            if (roh.Length == 0) { daten.SpeichertypIndex = null; return; }

            typen.Add((typen.Count, roh));
            daten.SpeichertypIndex = typen.Count - 1;
        }

        /// <summary>
        /// Oberfläche → Kern. Der Speichertyp geht über den INDEX (Befund L0-1); ein
        /// angehängter vierter Eintrag ist ein Freitext und geht unverändert durch.
        /// </summary>
        private static PufferSpModel NachModell(PufferSpKatalogDaten d, List<(int, string)> typen)
        {
            int index = d.SpeichertypIndex ?? -1;
            string typ;
            if (index >= 0 && index >= PufferSpStammCtrl.SPEICHERTYP_DB_WERTE.Length
                           && index < typen.Count)
                typ = typen[index].Item2;                       // Freitext des Bestands
            else
                typ = PufferSpStammCtrl.SpeichertypDbWert(index);

            return new PufferSpModel
            {
                Name = (d.Name ?? "").Trim(),
                Firma = (d.Firma ?? "").Trim(),
                Speichertyp = typ,
                Gesamtvolumen = d.Gesamtvolumen ?? 0,
                Betriebsbereitschaftverlust = d.Bereitschaftsverluste ?? 0,
                Investitionskosten = d.Investitionskosten ?? 0
            };
        }

        private static string Feld(IReadOnlyDictionary<string, string> satz, string schluessel)
        {
            string t;
            return satz.TryGetValue(schluessel, out t) ? (t ?? "") : "";
        }

        private static double Kommazahl(IReadOnlyDictionary<string, string> satz, string schluessel)
        {
            double d;
            return Program.ZahlParsen(Feld(satz, schluessel), out d) ? d : 0.0;
        }

        private static int Ganzzahl(IReadOnlyDictionary<string, string> satz, string schluessel)
        {
            int n;
            return Program.GanzzahlParsen(Feld(satz, schluessel), out n) ? n : 0;
        }

        private static KatalogSpeicherErgebnis Uebersetzen(PufferSpStammCtrl.SpeicherErgebnis e)
        {
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static string Text(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }
    }
}
