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

            gaben["Wege"] = new KatalogBrowserWege
            {
                // W14a-E-10: acht Spalten statt der vierzeiligen Eigenschaftenzelle;
                // die Stromkennzahl sigma rechnet der Controller mit.
                Katalogzeilen = () => ctrl.Katalogfilterzeilen(),
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
