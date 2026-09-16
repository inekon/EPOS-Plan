using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Waermepumpe;

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

            _quelle = new SimulationAnsichtQuelle(bedarf, new SimulationPlattformwege
            {
                WaermepumpeGaben = (idProjekt, modelle) =>
                    WaermepumpenHuelle.Gaben(besitzer(), idProjekt, modelle, wizard: false),

                // ANWENDERWUNSCH 16.09.2026: die Konfiguration EINER Waermepumpe hinter
                // dem Knopf ihrer Karte. Windows ist hier nicht die Ursache - die zwei
                // ABBILDUNGEN liegen in der Waermepumpen-Huelle dieser Schale
                // (AusModell/NachModell), und eine zweite Wahrheit ueber dieselben
                // vierzehn Felder gibt es nicht.
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
        /// Die Anlagenzeile als Feldsatz — <b>derselbe</b> Weg, den die Verwaltung
        /// nimmt (<c>WaermepumpenHuelle.Gaben</c>): Gerätestand zweistufig nachziehen,
        /// dann <c>AusModell</c>. Ohne das Nachziehen stünden Nennleistung, Typ und
        /// Hersteller auf 0 bzw. leer — und <c>NachModell</c> schriebe sie so zurück
        /// (Ä22/Ä23).
        /// </summary>
        /// <returns><c>null</c> = das Projekt führt keine Anlage dieser Id.</returns>
        private static WaermepumpeAnlageDaten KonfigurationLesen(int idProjekt, int idAnlage)
        {
            WErzeugerModel m = Anlage(idProjekt, idAnlage, out _);
            if (m == null) return null;

            WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);
            return WaermepumpeAnlageHuelle.AusModell(m);
        }

        /// <summary>
        /// Schreibt den Feldsatz zurück — <b>derselbe</b> Weg, den der Reiter
        /// „Wärmepumpe" nach seinem Übernehmen geht
        /// (<c>SimulationErgebnisHuelle.WaermepumpenFertig</c>, wörtlich
        /// <c>listView_SimWP_MouseDown</c> :5145-5150): <c>NachModell</c> in die
        /// Zeile, Träger dem Projekt zuordnen, dann die Wärmepumpen-Anlagen des
        /// Projekts als Ganzes neu schreiben.
        /// </summary>
        /// <remarks>
        /// <b>Warum die ganze Liste und nicht <c>WErzeugerCtrl.Update</c>.</b> Dessen
        /// UPDATE führt genau sechzehn Spalten und darunter WEDER <c>Heizstab</c> NOCH
        /// <c>ID_Carrier</c> — beide stehen in diesem Feldsatz. Ein Schreibweg, der
        /// zwei der bearbeiteten Felder still fallen ließe, wäre schlimmer als der
        /// teure: Löschen und Neuanlegen sichert Senken, Stränge, Varianten und
        /// Fachspalten (<c>Del_Projekt_Waermeerzeuger</c>) und ist der EINE
        /// Schreibweg aller Erzeuger.
        /// </remarks>
        private static bool KonfigurationSchreiben(int idProjekt, int idAnlage,
                                                   WaermepumpeAnlageDaten daten)
        {
            if (daten == null) return false;

            WErzeugerModel m = Anlage(idProjekt, idAnlage, out List<WErzeugerModel> modelle);
            if (m == null) return false;

            WaermepumpeAnlageHuelle.NachModell(daten, m);

            // ET-5: der gewaehlte Traeger gehoert dem Projekt zugeordnet.
            ErzeugerTraegerHuelle.Zuordnen(idProjekt, false, m.ID_Carrier);

            WizardCtrl wizctrl = new WizardCtrl();
            wizctrl.Del_Projekt_Waermeerzeuger(idProjekt, WizardItemClass.WP_TYP);
            return wizctrl.Add_WP_Waermeerzeuger(idProjekt, modelle);
        }

        /// <summary>
        /// Die Anlagenzeile zu einer Id samt der Liste, in der sie steht — die Liste
        /// ist es, die der Schreibweg braucht.
        /// </summary>
        private static WErzeugerModel Anlage(int idProjekt, int idAnlage,
                                             out List<WErzeugerModel> modelle)
        {
            modelle = WErzeugerCtrl.ModelleJeTyp(idProjekt, WizardItemClass.WP_TYP);

            foreach (WErzeugerModel m in modelle)
                if (m.ID == idAnlage) return m;

            return null;
        }
    }
}
