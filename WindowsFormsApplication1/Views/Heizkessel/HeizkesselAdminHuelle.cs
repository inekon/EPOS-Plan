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
    /// <para><b>Der Speicherweg vom 18.08.2026 bleibt — und trägt jetzt das volle
    /// Profil.</b> Editierbar sind die zwanzig Spalten, die
    /// <see cref="KatalogBrowserProfil"/> als <c>Editierbar</c> führt (Beschreibung,
    /// Leistung, Investitionskosten, Brennwert, Vor- und Rücklauf sowie Hersteller,
    /// Energieträger, Wirkungsgrade, Betriebsbereitschaftsverlust, Raumbedarf,
    /// Wartungskosten, Nutzungsdauer und die fünf Emissionsfaktoren); geschrieben wird über
    /// <see cref="HeizkesselStammCtrl.AnzeigefelderSchreiben"/> — samt Dublettenklammer
    /// und Lesen-Ändern-Schreiben.</para>
    /// </summary>
    internal static class HeizkesselAdminHuelle
    {
        /// <summary>Zeigt den Katalogbrowser als eigenes Fenster (<c>Masken.HeizkesselAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            KatalogBrowserProfil profil = Profil();
            // "Import..." (Konzept Administrationsdialoge 7.1 d) steht nur im eigenen
            // Verwaltungsfenster, nicht in der Katalogueberlagerung eines Projektdialogs.
            return KatalogBrowserHuelle.Oeffnen(besitzer, profil, KatalogBrowserHuelle.MitWegen(
                Gaben(), Wege(() => KatalogImportHuelle.Gaben(KatalogImportArt.Heizkessel))));
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
        /// <summary>
        /// Die zwei Wege, die der PROJEKTDIALOG fuer seinen Modulaufklapper braucht:
        /// alle Felder eines Katalogsatzes lesen und sie zurueckschreiben
        /// (Anwenderentscheid 15.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Dieselben zwei Delegaten, die auch der Katalogbrowser bekam.</b> Sie stehen
        /// hier und nicht ein zweites Mal im Projektdialog-Wirt: Welche Spalten ein
        /// Heizkesselsatz fuehrt und wie sie zurueckgeschrieben werden, ist EINE Frage
        /// mit EINER Antwort.
        /// </remarks>
        internal static KatalogBrowserWege Wege(Func<IReadOnlyDictionary<string, object>> import = null)
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new HeizkesselStammCtrl();

            return new KatalogBrowserWege
            {
                // "Import..." (7.1 d) - nur, wenn das eigene Fenster ihn hereinreicht.
                ImportGaben = import,
                Katalogzeilen = () => ctrl.Katalogfilterzeilen(),
                Detail = name => KatalogBrowserHuelle.Felder(profil, ctrl.KatalogsatzAnzeige(name)),
                Existiert = name => new HeizkesselStammCtrl().Exists(name),
                Loeschen = Loeschen,
                Speichern = (name, felder, _) => Schreiben(name, felder),
                // AD-Q11 (23.09.2026): ein Auslieferungssatz wird nie ueberschrieben;
                // "Duplizieren..." legt den eigenen Satz an.
                Duplizieren = (id, name) => KatalogBrowserHuelle.Kopie(HeizkesselStammCtrl.Duplizieren(id, name)),
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                Schloss = Schlosswege.Aus(HeizkesselStammCtrl.SchlossSetzen)
            };
        }

        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new HeizkesselStammCtrl();

            var gaben = KatalogBrowserHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = Wege();

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

        /// <summary>
        /// Schreibt die Felder des Aufklappers zurück — ALLE editierbaren Spalten des
        /// Profils, nicht nur die sechs des Vorläufers.
        /// </summary>
        /// <remarks>
        /// <para><b>Gelesen wird genau, was das Profil als <c>Editierbar</c> führt</b>
        /// (Anwenderentscheid 15.09.2026): zu den sechs Feldern von 2026-08-18 kommen
        /// Hersteller, Energieträger, die beiden Wirkungsgrade, der
        /// Betriebsbereitschaftsverlust, Raumbedarf, Wartungskosten samt Bezugsgröße,
        /// Nutzungsdauer und die fünf Emissionsfaktoren — zwanzig Spalten. Blieben sie
        /// hier stehen, verfielen die Änderungen des Aufklappers STILL: Die Komponente
        /// gibt alle Felder zurück, die Hülle las nur sechs davon.</para>
        /// <para><b>Ein leeres Textfeld heißt „unverändert lassen".</b> Energieträger und
        /// Bezugsgröße der Wartungskosten sind Nachschlagewerte; kommen sie leer herein,
        /// rührt <c>KatalogFeldPruefung.AusListe</c> die Spalte nicht an — so bleibt ein
        /// Altbestandssatz mit einem Wert außerhalb der Liste in seinen übrigen Feldern
        /// pflegbar.</para>
        /// </remarks>
        private static KatalogSpeicherErgebnis Schreiben(string name,
                                                         IReadOnlyList<BrowserFeldwert> felder)
        {
            var werte = new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBeschreibung),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldPtherm),
                KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldInvestitionskosten),
                KatalogBrowserHuelle.Schalter(felder, KatalogBrowserProfil.FeldBrennwert),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldVorlauf),
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldRuecklauf),
                Brennstoff: KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBrennstoff),
                Firma: KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldFirma),
                WirkungsgradGas:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWirkungsgradGas),
                WirkungsgradOel:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWirkungsgradOel),
                Betriebsbereitschaftverlust:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldBBVerlust),
                Raumbedarf: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldRaumbedarf),
                Wartungskosten:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWartungskosten),
                WartungskostenEinheit:
                    KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldWartungEinheit),
                Nutzungsdauer:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldNutzungsdauer),
                CO2: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldCo2),
                SO2: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldSo2),
                NOx: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldNox),
                CO: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldCo),
                Staub: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldStaub));

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
