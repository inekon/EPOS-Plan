using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Solarkollektor-Katalogbrowsers (iU9-W14a.1,
    /// Ausprägung <see cref="KatalogBrowserArt.Solarkollektoren"/>).
    ///
    /// <para>Vorbild <c>Views/Solarthermie/Form_SolarKollektorenAdmin</c> (188 Z., die
    /// schmalste Maske der Welle) — im selben Schritt gelöscht (Regel M1). Der
    /// Katalogeditor <see cref="SolarkollektorKatalogDialog"/> steht seit W7.6.</para>
    ///
    /// <para><b>Die einzige Ausprägung ohne Filterleiste und ohne Speicherweg.</b> Der
    /// Vorläufer führte einen Filterparameter, den alle drei Aufrufer leer ließen
    /// (Befund W14-B18); er entfällt ersatzlos.</para>
    /// </summary>
    internal static class SolarkollektorAdminHuelle
    {
        /// <summary>Zeigt den Katalogbrowser als eigenes Fenster (<c>Masken.SolarkollektorenAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            // "Import..." (Konzept Administrationsdialoge 7.1 d) steht nur im eigenen
            // Verwaltungsfenster, nicht in der Katalogueberlagerung eines Projektdialogs.
            return KatalogBrowserHuelle.Oeffnen(besitzer, Profil(), KatalogBrowserHuelle.MitWegen(
                Gaben(), Wege(() => KatalogImportHuelle.Gaben(KatalogImportArt.Solarkollektoren))));
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static KatalogBrowserProfil Profil()
        {
            return KatalogBrowserProfil.Finde(KatalogBrowserArt.Solarkollektoren, Text);
        }

        /// <summary>
        /// Die Wege des Katalogs — Liste, Detail, Existiert, Loeschen und seit dem
        /// 15.09.2026 der SPEICHERWEG.
        /// </summary>
        /// <remarks>
        /// <b>Dieselben zwei Delegaten, die auch der Projektdialog braucht.</b> Sein
        /// Aufklapper „Alle Daten anzeigen" liest ueber <c>Detail</c> und schreibt ueber
        /// <c>Speichern</c>; beides steht hier und nicht ein zweites Mal im Wirt: Welche
        /// Spalten ein Kollektorsatz fuehrt und wie sie zurueckgeschrieben werden, ist
        /// EINE Frage mit EINER Antwort (Muster <see cref="HeizkesselAdminHuelle"/>).
        /// </remarks>
        internal static KatalogBrowserWege Wege(Func<IReadOnlyDictionary<string, object>> import = null)
        {
            KatalogBrowserProfil profil = Profil();

            return new KatalogBrowserWege
            {
                // "Import..." (7.1 d) - nur, wenn das eigene Fenster ihn hereinreicht.
                ImportGaben = import,
                // W14a-E-10: sechs Spalten statt der dreizeiligen Eigenschaftenzelle -
                // und der ERSTE Filter dieses Katalogs ueberhaupt (bis hierher
                // KatalogFilterArt.Keiner).
                Katalogzeilen = SolarkollektorenStammCtrl.Katalogfilterzeilen,
                Detail = name => KatalogBrowserHuelle.Felder(
                    profil, SolarkollektorenStammCtrl.KatalogsatzAnzeige(name)),
                Existiert = name => new SolarkollektorenStammCtrl().Exists(name),
                Loeschen = Loeschen,
                Speichern = (name, felder, _) => Schreiben(name, felder),
                // AD-Q11 (23.09.2026): ein Auslieferungssatz wird nie ueberschrieben;
                // "Duplizieren..." legt den eigenen Satz an.
                Duplizieren = (id, name) => KatalogBrowserHuelle.Kopie(SolarkollektorenStammCtrl.Duplizieren(id, name)),
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                Schloss = Schlosswege.Aus(SolarkollektorenStammCtrl.SchlossSetzen)
            };
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            KatalogBrowserProfil profil = Profil();
            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = Wege();

            gaben["EditorInhalt"] = KatalogBrowserHuelle.Editor<SolarkollektorKatalogDialog>();
            gaben["EditorGaben"] = new Func<string, bool, Action<string>,
                                            IReadOnlyDictionary<string, object>>(EditorGaben);
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            var ctrl = new SolarkollektorenStammCtrl();
            bool ok = ctrl.Delete(name);
            return new KatalogSpeicherErgebnis(ok,
                ok ? "" : MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, name);
        }

        /// <summary>
        /// Die elf editierbaren Anzeigefelder zurueck in den Katalogsatz — der Weg
        /// des Knopfes „Speichern" im Aufklapper und in der Speicherleiste des Browsers.
        /// </summary>
        /// <remarks>
        /// <b>Die Zahlregel des Hauses liest die Felder</b> (<c>KatalogBrowserHuelle.Zahl</c>
        /// und <c>…Ganzzahl</c>, komma- wie punkttolerant); der Bezeichner steht nicht in
        /// der Liste, er ist der SCHLUESSEL und im Profil nicht editierbar.
        /// </remarks>
        private static KatalogSpeicherErgebnis Schreiben(string name,
                                                         IReadOnlyList<BrowserFeldwert> felder)
        {
            var werte = new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor(
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldKollektortyp),
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldFirma),
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBeschreibung),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldModulflaeche),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldAperturflaeche),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldH0),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldK1),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldK2),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKdir),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKdiff),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldInvestitionskosten));

            SolarkollektorenStammCtrl.SpeicherErgebnis e =
                SolarkollektorenStammCtrl.AnzeigefelderSchreiben(name, werte);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static IReadOnlyDictionary<string, object> EditorGaben(string name, bool neu,
                                                                       Action<string> fertig)
        {
            return new Dictionary<string, object>(SolarkollektorHuelle.KatalogGaben(name, neu))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), fertig)
            };
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
