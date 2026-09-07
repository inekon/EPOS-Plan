using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Heizkessel-Katalogbrowsers (iU9-W14a.1,
    /// Ausprägung <see cref="KatalogBrowserArt.Heizkessel"/>).
    ///
    /// <para>Vorbild <c>Views/Heizkessel/Form_Heizkessel_Admin</c> (365 Z.) — im selben
    /// Schritt gelöscht (Regel M1). Der Katalogeditor
    /// <see cref="EPOS.UI.Dialoge.Erzeuger.HeizkesselKatalogDialog"/> steht seit W6.1
    /// und erscheint hier als <c>Ueberlagerung</c> im selben Fenster statt als zweite
    /// <c>BlazorWebView</c> (Risiko R2).</para>
    ///
    /// <para><b>Der Speicherweg vom 18.08.2026 bleibt.</b> Sechs Anzeigefelder sind
    /// editierbar (Beschreibung, Leistung, Investitionskosten, Brennwert, Vorlauf,
    /// Rücklauf); geschrieben wird über
    /// <see cref="HeizkesselStammCtrl.AnzeigefelderSchreiben"/> — samt Dublettenklammer
    /// und Lesen-Ändern-Schreiben.</para>
    /// </summary>
    internal static class HeizkesselAdminHuelle
    {
        /// <summary>Zeigt den Katalogbrowser als eigenes Fenster (<c>Masken.HeizkesselAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            KatalogBrowserProfil profil = Profil();
            return KatalogBrowserHuelle.Oeffnen(besitzer, profil, Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static KatalogBrowserProfil Profil()
        {
            return KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel, Text);
        }

        /// <summary>
        /// Der PARAMETERSATZ — auch für die Anzeige als <c>Ueberlagerung</c> im
        /// Projektdialog <c>HeizkesselDialog</c> (W6.3). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new HeizkesselStammCtrl();

            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = new KatalogBrowserWege
            {
                // W14a-E-10: die VOLLSTAENDIGE Liste mit ihren sechs Spalten;
                // gefiltert wird im Spaltenkopf, nicht mehr ueber zwei Klapplisten.
                Katalogzeilen = () => ctrl.Katalogfilterzeilen(),
                Detail = name => KatalogBrowserHuelle.Felder(profil, ctrl.KatalogsatzAnzeige(name)),
                Existiert = name => new HeizkesselStammCtrl().Exists(name),
                Loeschen = Loeschen,
                Speichern = (name, felder, _) => Schreiben(name, felder)
            };

            gaben["EditorInhalt"] = KatalogBrowserHuelle.Editor<HeizkesselKatalogDialog>();
            gaben["EditorGaben"] = new Func<string, bool, Action<string>,
                                            IReadOnlyDictionary<string, object>>(EditorGaben);
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            var ctrl = new HeizkesselStammCtrl();
            bool ok = ctrl.Delete(name);
            return new KatalogSpeicherErgebnis(ok, ok ? "" : Text("KBROW_MSG_LOESCHEN_FEHLER"), name);
        }

        private static KatalogSpeicherErgebnis Schreiben(string name,
                                                         IReadOnlyList<BrowserFeldwert> felder)
        {
            var werte = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBeschreibung),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldPtherm),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldInvestitionskosten),
                KatalogBrowserHuelle.Schalter(felder, KatalogBrowserProfil.FeldBrennwert),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldVorlauf),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldRuecklauf));

            HeizkesselStammCtrl.SpeicherErgebnis e =
                HeizkesselStammCtrl.AnzeigefelderSchreiben(name, werte);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        /// <summary>
        /// Der Parametersatz des Katalogeditors. Die Beschreibung reicht der Browser
        /// mit — wortgleich <c>Form_Heizkessel_Admin.btn_Bearbeiten_Click</c> (Z. 172).
        /// </summary>
        private static IReadOnlyDictionary<string, object> EditorGaben(string name, bool neu,
                                                                       Action<string> fertig)
        {
            string beschreibung = "";
            if (!neu)
            {
                var satz = new HeizkesselStammCtrl().KatalogsatzAnzeige(name);
                if (satz != null && satz.ContainsKey(KatalogBrowserProfil.FeldBeschreibung))
                    beschreibung = satz[KatalogBrowserProfil.FeldBeschreibung];
            }

            return new Dictionary<string, object>(HeizkesselHuelle.Gaben(name, beschreibung, neu))
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
