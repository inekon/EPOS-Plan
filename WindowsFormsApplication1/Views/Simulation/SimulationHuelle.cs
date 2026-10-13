using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-SCHALE der Ansicht „Simulation" — seit Auftrag <b>#208</b> nur noch
    /// ein ADAPTER.
    ///
    /// <para><b>Was hier bis #208 stand</b> (Auftrag #207): das Halten der zwei
    /// Hülleninstanzen, das Zusammenfassen ihrer Parametersätze und die Projektzeile.
    /// Das ist alles plattformfrei und liegt seither in
    /// <c>EPOS.UI.Daten/Simulation/SimulationAnsichtQuelle.cs</c>; sonst wäre die
    /// Simulation auf iOS unerreichbar geblieben, obwohl von den 5 490 Zeilen der zwei
    /// Datenhüllen genau sechs Windows waren.</para>
    ///
    /// <para><b>Was hier geblieben ist</b>, ist genau das Windows-Stück: der
    /// FENSTERBESITZER. Der Wärmepumpen-Assistent öffnet aus sich heraus weitere
    /// WinForms-Fenster (<c>WaermepumpenHuelle.Gaben(IWin32Window, …)</c>) und braucht
    /// deshalb eines, über dem er erscheinen kann — die Ansicht selbst hat keines
    /// (Entscheid E-5). Er geht als benannter Weg in die Quelle
    /// (<c>SimulationPlattformwege</c>); der Nachzug über <c>WizardCtrl</c> ist
    /// Kern-Arbeit und liegt deshalb weiterhin in der Datenhülle.</para>
    ///
    /// <para><b>Muster:</b> <c>StromspeicherAuslegungHuelle.AnsichtGaben</c> (#192) und
    /// <c>AssistentHuelle.AnsichtGaben</c> (#62b) — ein DELEGAT je Betreten statt eines
    /// stehenden Wörterbuchs, weil der Satz den Stand der Hülle mitbringt.</para>
    /// </summary>
    internal sealed class SimulationHuelle
    {
        private readonly ProjektKontextCtrl _kontext;
        private readonly SimulationAnsichtQuelle _quelle;

        /// <param name="besitzer">
        /// Das Fenster, über dem die Unterdialoge des Wärmepumpen-Assistenten
        /// erscheinen — die Ansicht hat kein eigenes (Entscheid E-5).
        /// </param>
        /// <param name="kontext">Das offene Projekt (K2).</param>
        /// <param name="bedarf">
        /// Die zwei Bedarfsrechnungen des offenen Projekts (Befund W16-B29,
        /// Entscheid E-5). Sie gehören dem PROJEKT und werden mit der Startseite
        /// geteilt — deren Reiter „Simulation" rechnet dieselben Zahlen.
        /// </param>
        internal SimulationHuelle(Func<Form> besitzer, ProjektKontextCtrl kontext,
                                  BedarfsZustand bedarf)
        {
            if (besitzer == null) throw new ArgumentNullException(nameof(besitzer));
            _kontext = kontext ?? throw new ArgumentNullException(nameof(kontext));

            // KATALOGAUSWAHL V1, STUFE 3: dieselbe Projektkopievormerkung wie in
            // WaermepumpenHuelle.Oeffnen - je Oeffnen des Dialogs eine; Abbrechen raeumt die in
            // der Sitzung angelegten Kopien ab, OK die nicht mehr referenzierten.
            Projektkopievormerkung vormerkung = null;
            _quelle = new SimulationAnsichtQuelle(bedarf, new SimulationPlattformwege
            {
                WaermepumpeGaben = (idProjekt, modelle) =>
                {
                    vormerkung = new Projektkopievormerkung(
                        name => new WPCtrl().DeleteFromProjekt(name, idProjekt));
                    return WaermepumpenHuelle.Gaben(besitzer(), idProjekt, modelle, wizard: false,
                                                    vormerkung: vormerkung);
                },
                WaermepumpeAbschluss = (ok, modelle) =>
                {
                    Projektkopievormerkung offen = vormerkung;
                    vormerkung = null;
                    offen?.Abschliessen(ok, id => modelle.Exists(it => it.ID_WP == id &&
                        (it.ID_Type == WizardItemClass.WP_TYP || it.ID_Type == WizardItemClass.REF_WP_TYP)));
                },

                // ANWENDERWUNSCH 16.09.2026: die Konfiguration EINER Waermepumpe hinter
                // dem Knopf ihrer Karte. Windows ist hier nicht die Ursache - die
                // ABBILDUNG liegt in der Waermepumpen-Huelle dieser Schale (AusModell),
                // und eine zweite Wahrheit ueber dieselben vierzehn Felder gibt es
                // nicht. GESCHRIEBEN wird seither ueber den schmalen Weg des Kerns
                // (WErzeugerCtrl.KonfigurationSchreiben) - die Anlagen-Ids bleiben.
                WaermepumpeKonfigLesen = KonfigurationLesen,
                WaermepumpeKonfigSchreiben = KonfigurationSchreiben,
                WaermepumpeTraegerkatalog =
                    () => ErzeugerTraegerHuelle.Katalog(DbWerte.ERZEUGER_WAERMEPUMPE)
            });
        }

        /// <summary>
        /// Der Parametersatz der Ansicht SIMULATION; <c>null</c> = kein Projekt offen.
        /// </summary>
        internal IReadOnlyDictionary<string, object> AnsichtGaben()
            => _quelle.AnsichtGaben(_kontext.Id, _kontext.Name);

        // =================================================================
        //  Die Konfiguration EINER Wärmepumpen-Anlage (Anwenderwunsch 16.09.2026)
        // =================================================================

        /// <summary>
        /// Die Anlagenzeile als Feldsatz und zurück — beide Wege liegen plattformfrei in
        /// <see cref="WaermepumpeAnlagenaht"/> (KM‑K2), damit der Kältemaschinen-Dialog dieselbe
        /// Anlagenzeile über denselben Lese- und Schreibweg bearbeitet.
        /// </summary>
        private static WaermepumpeAnlageDaten KonfigurationLesen(int idProjekt, int idAnlage)
            => WaermepumpeAnlagenaht.Lesen(idProjekt, idAnlage);

        /// <summary>Schreibt die Konfigurationsfelder der Anlage zurück (plattformfrei, KM‑K2).</summary>
        private static AnlagenkonfigErgebnis KonfigurationSchreiben(
            int idProjekt, int idAnlage, WaermepumpeAnlageDaten daten)
            => WaermepumpeAnlagenaht.Schreiben(idProjekt, idAnlage, daten);
    }
}
