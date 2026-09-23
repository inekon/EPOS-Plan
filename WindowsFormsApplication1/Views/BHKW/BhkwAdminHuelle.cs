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

        /// <summary>
        /// Die Wege, die der PROJEKTDIALOG für seinen Modulaufklapper braucht: alle
        /// Felder eines Katalogsatzes lesen, sie zurückschreiben und wissen, ob der
        /// Satz aus der Auslieferung stammt (Anwenderentscheid 15.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Dieselben Delegaten, die auch der Katalogbrowser bekommt.</b> Sie stehen
        /// hier und nicht ein zweites Mal im Projektdialog-Wirt: Welche Spalten ein
        /// BHKW-Satz führt und wie sie zurückgeschrieben werden, ist EINE Frage mit
        /// EINER Antwort. Den Schreibschutz liest die Verwaltung aus der Zeile
        /// (<c>Katalogfilterzeile.Geschuetzt</c>, AD-Q11); der Projektdialog fragt ihn
        /// unmittelbar bei <c>BHKWStammCtrl.IstSchreibgeschuetzt</c> — in der
        /// Auslieferungsdatenbank ist jeder Satz von <c>Tab_BHKW_STAMM</c> geschützt.
        /// </remarks>
        internal static KatalogBrowserWege Wege()
        {
            KatalogBrowserProfil profil = Profil();
            var ctrl = new BHKWStammCtrl();

            return new KatalogBrowserWege
            {
                // W14a-E-10: acht Spalten statt der vierzeiligen Eigenschaftenzelle;
                // die Stromkennzahl sigma rechnet der Controller mit.
                Katalogzeilen = () => ctrl.Katalogfilterzeilen(),
                Detail = name => KatalogBrowserHuelle.Felder(profil,
                                                             BHKWStammCtrl.KatalogsatzAnzeige(name)),
                Existiert = name => BHKWStammCtrl.IdZu(name) > 0,
                Loeschen = Loeschen,
                Speichern = Schreiben,
                // AD-Q11 (23.09.2026): ein Auslieferungssatz wird nie ueberschrieben;
                // "Duplizieren..." legt den eigenen Satz an.
                Duplizieren = (id, name) => KatalogBrowserHuelle.Kopie(BHKWStammCtrl.Duplizieren(id, name))
            };
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var gaben = KatalogBrowserHuelle.GemeinsameGaben(Profil());

            gaben["Wege"] = Wege();

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

        /// <summary>
        /// Schreibt die Felder des Aufklappers zurück — ALLE editierbaren Spalten des
        /// Profils, nicht nur die sechs des Vorläufers.
        /// </summary>
        /// <remarks>
        /// <para><b>Gelesen wird genau, was das Profil als <c>Editierbar</c> führt</b>
        /// (Anwenderentscheid 15.09.2026): Firma, Beschreibung, Brennstoff, Motortyp,
        /// die vier Leistungs- und Temperaturwerte, die zwei Wirkungsgradanteile,
        /// Raumbedarf, die fünf
        /// Kostenposten, Wartung, Nutzungsdauer und die fünf Emissionsfaktoren. Die
        /// Investition je kWel bleibt draußen: Sie ist die ABLEITUNG der fünf Posten
        /// (W14a-E-8-B3) und wird im Kern nachgerechnet.</para>
        /// <para><b>Der GESAMTwirkungsgrad bleibt ebenso draußen</b>
        /// (Anwenderentscheid 20.09.2026): Er ist die Summe der zwei Anteile und im
        /// Aufklapper nur Anzeige.</para>
        /// <para><b>Ein leeres Textfeld heißt „unverändert lassen".</b> Der Brennstoff
        /// ist ein Nachschlagewert; kommt er leer herein, rührt
        /// <c>KatalogFeldPruefung.AusListe</c> die Spalte nicht an — so bleibt ein
        /// Altbestandssatz mit einem Wert außerhalb der Liste in seinen übrigen Feldern
        /// pflegbar.</para>
        /// </remarks>
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
                KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldRuecklauf),
                Beschreibung: KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBeschreibung),
                Brennstoff: KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldBrennstoff),
                Motortyp: KatalogBrowserHuelle.Wert(felder, KatalogBrowserProfil.FeldMotortyp),
                Raumbedarf: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldRaumbedarf),
                KostenModul: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKostenModul),
                KostenMontage: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKostenMontage),
                KostenLieferung: KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKostenLieferung),
                KostenSchallschutzhaube:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKostenSchallschutz),
                KostenAbgasreinigung:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldKostenAbgasreinigung),
                WartungskostenJeKWhel:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWartungJeKwhel),
                Nutzungsdauer: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldNutzungsdauer),
                NOx: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldNox),
                SO2: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldSo2),
                CO: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldCo),
                CO2: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldCo2),
                Staub: KatalogBrowserHuelle.Ganzzahl(felder, KatalogBrowserProfil.FeldStaub),
                // DIE ZWEI ANTEILE statt des Gesamtwerts (Anwenderentscheid
                // 20.09.2026): Der Gesamtwirkungsgrad ist im Aufklapper Anzeige und
                // steht deshalb nicht mehr im Datensatz - der Kern bildet ihn als
                // Summe (BhkwWirkungsgrad.GesamtZumSchreiben).
                WirkungsgradEl:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWirkungsgradEl),
                WirkungsgradTh:
                    KatalogBrowserHuelle.Zahl(felder, KatalogBrowserProfil.FeldWirkungsgradTh));

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
