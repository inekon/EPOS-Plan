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

            _quelle = new SimulationAnsichtQuelle(bedarf, new SimulationPlattformwege
            {
                WaermepumpeGaben = (idProjekt, modelle) =>
                    WaermepumpenHuelle.Gaben(besitzer(), idProjekt, modelle, wizard: false),

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
        /// Schreibt die KONFIGURATIONSFELDER der Anlage zurück — ein schmales UPDATE
        /// auf <c>Tab_Energieanlagen</c> über <c>WErzeugerCtrl.KonfigurationSchreiben</c>.
        /// </summary>
        /// <remarks>
        /// <para><b>Was hier bis zum 16.09.2026 stand</b>, war der Bestandsweg der
        /// Erzeuger: <c>NachModell</c> in die Zeile, dann
        /// <c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> + <c>Add_WP_Waermeerzeuger</c>
        /// für die ganze Anlagenliste des Projekts. Er war nicht falsch, aber zu teuer
        /// und hatte eine sichtbare Folge: <b>Löschen und Neuanlegen vergibt neue
        /// Anlagen-Ids.</b> Die Karte, die man gerade bearbeitet hatte, war danach nicht
        /// mehr markiert, und jede Zuordnung, die an der alten Id hängt, muss beim
        /// Neuanlegen gerettet werden. Für acht Felder ist das der falsche Preis.</para>
        ///
        /// <para><b>Warum es jetzt geht.</b> Der Grund für den Umweg war
        /// <c>WErzeugerCtrl.Update</c>: Dessen UPDATE führt sechzehn Spalten und darunter
        /// weder <c>Heizstab</c> noch <c>ID_Carrier</c> — die zwei Felder, um die es hier
        /// vor allem geht. <c>WErzeugerCtrl.KonfigurationSchreiben</c> ist genau für
        /// diesen Satz gebaut: acht Felder, read-modify-write (<c>null</c> = unverändert),
        /// adressiert über <c>ID</c> UND <c>ID_Projekt</c>, die Ids bleiben stehen.</para>
        ///
        /// <para><b>Nur die KONFIGURATION, nicht der ganze Feldsatz.</b> Der Dialog an
        /// der Karte zeigt den Baustein <c>WaermepumpeKonfiguration</c> und nichts sonst;
        /// Vorlauf, Rücklauf, Nutzungsdauer und die Stammfelder bearbeitet allein der
        /// Anlagendialog, und der geht weiter seinen eigenen Speicherweg. Ein Schreibweg,
        /// der hier mehr anfasste, als der Dialog zeigt, schriebe Werte zurück, die
        /// niemand angesehen hat.</para>
        /// </remarks>
        private static AnlagenkonfigErgebnis KonfigurationSchreiben(
            int idProjekt, int idAnlage, WaermepumpeAnlageDaten daten)
        {
            if (daten == null)
                return new AnlagenkonfigErgebnis(false, Text_("ANL_KONFIG_MSG_FEHLER",
                    "Die Konfiguration der Anlage konnte nicht gespeichert werden."));

            WErzeugerCtrl.SpeicherErgebnis e = WErzeugerCtrl.KonfigurationSchreiben(
                idAnlage, idProjekt,
                new WErzeugerCtrl.KonfigurationFelder(
                    Heizstab: daten.Heizstab,
                    Sperrung: daten.Sperrung,
                    SperrzeitVon: daten.SperrzeitVon ?? 0,
                    SperrzeitBis: daten.SperrzeitBis ?? 0,
                    BivalenterBetrieb: daten.BivalenterBetrieb,
                    Betriebsart: daten.Betriebsart ?? "",
                    Abschaltpunkt: daten.Abschaltpunkt,
                    // 0 heisst hier "nicht anfassen", nicht "kein Traeger": Der Dialog
                    // bietet keine Moeglichkeit, die Wahl ZURUECKZUNEHMEN - er laesst
                    // sie nur unberuehrt, solange die Anlage noch keine fuehrt.
                    IdCarrier: daten.CarrierId > 0 ? daten.CarrierId : (int?)null,
                    // KU2 Welle 3 (E33, E34): Kuehltraeger (0 = wie Heizbetrieb, NULL) und
                    // Abrechnungsart (false = anteilig, NULL) - gelesen mit der Zeile, also
                    // unveraendert, wenn niemand sie angefasst hat.
                    KuehlIdCarrier: daten.KuehlCarrierId ?? 0,
                    KuehlEigenerZaehler: daten.KuehlEigenerZaehler == true));

            // ET-5: der gewaehlte Traeger gehoert dem Projekt zugeordnet. Idempotent;
            // er steht auch dann an, wenn der Satz sonst unveraendert blieb.
            if (e.Ok) ErzeugerTraegerHuelle.Zuordnen(idProjekt, false, daten.CarrierId);

            // KU2 Welle 3 (Kuehlkonzept 8.2): die drei Geraetefelder des Kuehlbetriebs in die
            // Projektkopie - ueber den einen Schreibweg des Kerns samt Sperrgruenden, nur wenn
            // sie sich geaendert haben. Ein abgelehnter Kuehlbetrieb meldet seinen Grund.
            if (e.Ok)
            {
                string kuehlGrund = WaermepumpeGeraeteCtrl.KuehlkonfigurationNachziehen(
                    daten.IdWp, idProjekt, daten.Kuehlbetrieb, daten.KuehlVorlauf, daten.KuehlHilfsstromanteil);
                if (kuehlGrund != null) return new AnlagenkonfigErgebnis(false, kuehlGrund);
            }

            return new AnlagenkonfigErgebnis(e.Ok, e.Meldung ?? "");
        }

        /// <summary>
        /// Die Anlagenzeile zu einer Id — der LESEWEG braucht sie samt der Liste, in
        /// der sie steht; der Schreibweg adressiert seit dem 16.09.2026 unmittelbar
        /// über (<c>ID</c>, <c>ID_Projekt</c>).
        /// </summary>
        private static WErzeugerModel Anlage(int idProjekt, int idAnlage,
                                             out List<WErzeugerModel> modelle)
        {
            modelle = WErzeugerCtrl.ModelleJeTyp(idProjekt, WizardItemClass.WP_TYP);

            foreach (WErzeugerModel m in modelle)
                if (m.ID == idAnlage) return m;

            return null;
        }

        /// <summary>Ressourcentext mit deutschem Rueckfall (Drei-Schichten-Regel).</summary>
        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
