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
            return KatalogBrowserHuelle.Oeffnen(besitzer, Profil(), Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static KatalogBrowserProfil Profil()
        {
            return KatalogBrowserProfil.Finde(KatalogBrowserArt.Solarkollektoren, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            KatalogBrowserProfil profil = Profil();
            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = new KatalogBrowserWege
            {
                Liste = (_, __) => Zeilen(profil),
                Detail = name => KatalogBrowserHuelle.Felder(
                    profil, SolarkollektorenStammCtrl.KatalogsatzAnzeige(name)),
                Existiert = name => new SolarkollektorenStammCtrl().Exists(name),
                Loeschen = Loeschen
            };

            gaben["EditorInhalt"] = KatalogBrowserHuelle.Editor<SolarkollektorKatalogDialog>();
            gaben["EditorGaben"] = new Func<string, bool, Action<string>,
                                            IReadOnlyDictionary<string, object>>(EditorGaben);
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Die zweispaltige Liste. Der dreizeilige Eigenschaftentext stand im Vorläufer
        /// mit zwei deutschen Literalen IM DATENSTROM (<c>Z. 96</c>); die Beschriftungen
        /// kommen jetzt aus dem Profil.
        /// </summary>
        private static IReadOnlyList<BrowserZeile> Zeilen(KatalogBrowserProfil profil)
        {
            var liste = new List<BrowserZeile>();
            foreach (var z in SolarkollektorenStammCtrl.KatalogZeilen())
            {
                string text = z.Firma
                            + "\n" + profil.Zeilenbauplan[0] + z.Kollektortyp
                            + "\n" + profil.Zeilenbauplan[1] + z.Aperturflaeche + " m²";
                liste.Add(new BrowserZeile(z.Id, z.Bezeichner, text));
            }
            return liste;
        }

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            var ctrl = new SolarkollektorenStammCtrl();
            bool ok = ctrl.Delete(name);
            return new KatalogSpeicherErgebnis(ok,
                ok ? "" : MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, name);
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
