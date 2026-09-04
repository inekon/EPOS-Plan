using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des BHKW-Katalogbrowsers (iU9-W14a.1,
    /// Ausprägung <see cref="KatalogBrowserArt.Bhkw"/>).
    ///
    /// <para>Vorbild <c>Views/BHKW/Form_BHKWAdmin</c> (465 Z., die größte Maske der
    /// Welle) — im selben Schritt gelöscht (Regel M1). Der Katalogeditor
    /// <see cref="EPOS.UI.Dialoge.Erzeuger.BhkwKatalogDialog"/> steht seit W6.2.</para>
    ///
    /// <para><b>Der einzige Browser mit Schreibschutzanzeige.</b> In der
    /// Auslieferungsdatenbank sind ALLE Sätze von <c>Tab_BHKW_STAMM</c>
    /// schreibgeschützt; die Liste zeichnet sie grau, und „Speichern" fragt vorher
    /// nach (<c>Form_BHKWAdmin.cs:202, :418</c>).</para>
    ///
    /// <para><b>Die achte Leistungsstufe trifft jetzt</b> (Befund W14-B10, Abweichung
    /// A-3): Der Vorläufer füllte die Klappliste aus <c>LeistungText</c> (letzter
    /// Eintrag „größer 1200 kW") und verglich gegen „über 1.200 kW" — die Stufe traf
    /// NIE und zeigte still alle Leistungen. Der Kern entscheidet über den INDEX.</para>
    /// </summary>
    internal static class BhkwAdminHuelle
    {
        /// <summary>Zeigt den Katalogbrowser als eigenes Fenster (<c>Masken.BhkwAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            return KatalogBrowserHuelle.Oeffnen(besitzer, Profil(), Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static KatalogBrowserProfil Profil()
        {
            return KatalogBrowserProfil.Finde(KatalogBrowserArt.Bhkw, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new BHKWStammCtrl();

            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil);

            gaben["FilterEins"] = KatalogBrowserHuelle.MitAlle(ctrl.Brennstoffart_Gruppe);
            gaben["FilterZwei"] = Leistungsstufen();

            gaben["Wege"] = new KatalogBrowserWege
            {
                Liste = (gruppe, leistung) => Zeilen(profil, ctrl, gruppe, leistung),
                Detail = name => KatalogBrowserHuelle.Felder(profil,
                                                             BHKWStammCtrl.KatalogsatzAnzeige(name)),
                Existiert = name => BHKWStammCtrl.IdZu(name) > 0,
                Loeschen = Loeschen,
                Speichern = Schreiben,
                IstGeschuetzt = BHKWStammCtrl.IstSchreibgeschuetzt
            };

            gaben["EditorInhalt"] = KatalogBrowserHuelle.Editor<BhkwKatalogDialog>();
            gaben["EditorGaben"] = new Func<string, bool, Action<string>,
                                            IReadOnlyDictionary<string, object>>(EditorGaben);
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Die zweispaltige Liste. Der vierzeilige Eigenschaftentext stand im Vorläufer
        /// mit drei deutschen Literalen IM DATENSTROM (<c>Z. 196-200</c>); die
        /// Beschriftungen kommen jetzt aus dem Profil und damit aus dem Textkatalog.
        /// </summary>
        private static IReadOnlyList<BrowserZeile> Zeilen(KatalogBrowserProfil profil,
                                                          BHKWStammCtrl ctrl,
                                                          int gruppe, int leistung)
        {
            string g = gruppe <= 0 || gruppe > ctrl.Brennstoffart_Gruppe.Count
                     ? "Alle" : ctrl.Brennstoffart_Gruppe[gruppe - 1];

            var liste = new List<BrowserZeile>();
            foreach (var z in ctrl.Filtern(g, leistung))
            {
                string text = z.Firma
                            + "\n" + profil.Zeilenbauplan[0] + z.Brennstoff
                            + "\n" + profil.Zeilenbauplan[1] + z.Ptherm + " kW"
                            + "\n" + profil.Zeilenbauplan[2] + z.Pel + " kW";

                liste.Add(new BrowserZeile(z.Id, z.Bezeichner, text,
                                           BHKWStammCtrl.IstSchreibgeschuetzt(z.Bezeichner)));
            }
            return liste;
        }

        /// <summary>
        /// Die NEUN Filterstufen: „Alle" voran, dann die acht aus
        /// <c>BHKWStammCtrl.LeistungText</c>. Der Index ist der Steuerwert.
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Leistungsstufen()
        {
            var texte = new List<string> { MyResource.Resource.PSP_FILTER_ALLE };
            foreach (string t in BHKWStammCtrl.LeistungText)
                if (!string.IsNullOrEmpty(t)) texte.Add(t);
            return KatalogBrowserHuelle.Nummeriert(texte);
        }

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            var ctrl = new BHKWStammCtrl();

            // ReadOnly-Schutz: schreibgeschuetzte Saetze sind nicht loeschbar. Der
            // Vorlaeufer meldete das mit einer eigenen MessageBox (Z. 262-267); der
            // Grund kommt jetzt als Text zurueck.
            if (ctrl.IsReadOnly(name))
                return new KatalogSpeicherErgebnis(false,
                    MyResource.Resource.KBROW_MSG_SCHUTZ_LOESCHEN, "");

            bool ok = ctrl.Delete(name);
            return new KatalogSpeicherErgebnis(ok,
                ok ? "" : MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, name);
        }

        private static KatalogSpeicherErgebnis Schreiben(string name,
                                                         IReadOnlyList<BrowserFeldwert> felder,
                                                         bool schutzUebergehen)
        {
            var werte = new BHKWStammCtrl.AnzeigefelderBhkw(
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldFirma),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldPtherm),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldPel),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldGrenzleistung),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldVorlauf),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldRuecklauf));

            BHKWStammCtrl.SpeicherErgebnis e =
                BHKWStammCtrl.AnzeigefelderSchreiben(name, werte, schutzUebergehen);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static IReadOnlyDictionary<string, object> EditorGaben(string name, bool neu,
                                                                       Action<string> fertig)
        {
            return new Dictionary<string, object>(BhkwHuelle.KatalogGaben(name, neu))
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
