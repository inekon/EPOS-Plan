using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-SCHALE des Projektassistenten (iU9-W16a.5) — sie löste
    /// <c>WizardParent</c> ab und ist seit dem Befund <b>W16a-O-4</b> nur noch das,
    /// was an diesem Lauf WINDOWS ist.
    ///
    /// <para><b>Was hier weg ist und wohin.</b> Der Parametersatz eines
    /// Assistentenlaufs steht seit W16a-O-4 in
    /// <see cref="AssistentAnsichtQuelle"/> (<c>EPOS.UI.Daten</c>) — plattformfrei,
    /// mit den zwei Seitenhüllen, die keine Plattform kennen
    /// (<see cref="KomponentenauswahlHuelle"/>, <see cref="ProjektKopfHuelle"/>).
    /// Solange das Ganze hier lag, meldete <c>AppWurzel</c> auf dem iPad, der
    /// Assistent stehe dort nicht zur Verfügung; dieselbe Begründung und dasselbe
    /// Muster wie beim Umzug der Simulationshüllen (Auftrag #208).</para>
    ///
    /// <para><b>Was hier BLEIBT</b>, ist die Naht
    /// <see cref="AssistentPlattformwege"/> mit drei Stücken:
    /// <list type="bullet">
    /// <item>die elf Seitenhüllen von <see cref="Seitengaben"/> — sie reichen einen
    ///       <c>IWin32Window</c> an die Katalogdialoge weiter, die sie aus sich
    ///       heraus öffnen, und wandern mit iU11;</item>
    /// <item>der Kurzhinweis „Daten gespeichert" der Razor-Startseite;</item>
    /// <item>der Kurzhinweis „Projekt &lt;Name&gt; geöffnet!" nach dem Rückweg aus
    ///       dem linken Band.</item>
    /// </list></para>
    ///
    /// <para><b>Seit W16a-E-1 / W16b-O-5 ist der Assistent eine FREIE ANSICHT</b>
    /// (Aufgabe #62b, 11.09.2026) — es gibt kein <c>BlazorDialogForm</c> mehr und
    /// kein <c>DialogResult</c>. Der Nachzug des Projektkontexts geschieht unmittelbar
    /// hinter dem gelungenen Speicherlauf (jetzt in
    /// <c>AssistentAnsichtQuelle.ProjektkontextNachziehen</c>), und die Startseite
    /// erfährt ihn über <c>ProjektKontextCtrl.Gewechselt</c> wie jeden anderen
    /// Projektwechsel.</para>
    /// </summary>
    internal static class AssistentHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ EINES neuen Assistentenlaufs — der Ersatz für
        /// <c>Oeffnen(besitzer, betriebsart)</c> seit #62b. Gerufen wird er von
        /// <c>AppWurzel</c> über <c>Hauptfenster</c>, sobald die Ansicht ASSISTENT
        /// betreten wird — einmal je Lauf.
        /// </summary>
        /// <param name="betriebsart">
        /// <see cref="AssistentCtrl.BETRIEBSART_NEU"/> oder <c>…_BEARBEITEN</c>.
        /// </param>
        internal static IReadOnlyDictionary<string, object> AnsichtGaben(int betriebsart)
        {
            return AssistentAnsichtQuelle.AnsichtGaben(betriebsart, Wege());
        }

        /// <summary>
        /// Der nächste Lauf merkt sein Projekt als „zuletzt geöffnet" — gesetzt von
        /// den zwei Startkacheln, verbraucht von <see cref="AnsichtGaben"/>.
        /// </summary>
        internal static void NaechsterLaufMerktProjekt()
        {
            AssistentAnsichtQuelle.NaechsterLaufMerktProjekt();
        }

        /// <summary>
        /// Der PARAMETERSATZ der Seite zu einem bereits bestehenden Lauf — der Weg
        /// der Prüfstände.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(AssistentCtrl ctrl,
                                                                  bool merken = false)
        {
            return AssistentAnsichtQuelle.Gaben(ctrl, Wege(), merken);
        }

        // =================================================================================
        // Die Naht
        // =================================================================================

        /// <summary>
        /// Was die WINDOWS-Schale beisteuert: die elf Seitenhüllen mit
        /// Fensterbesitzer und die zwei Kurzhinweise der Startseite.
        /// </summary>
        private static AssistentPlattformwege Wege()
        {
            return new AssistentPlattformwege
            {
                SeitenGaben = Seitengaben,
                Kurzhinweis = satz => StartseiteHuelle.Aktuelle?.Kurzhinweis(satz),
                HinweisProjektGeoeffnet =
                    () => StartseiteHuelle.Aktuelle?.HinweisProjektGeoeffnet()
            };
        }

        /// <summary>
        /// Der Parametersatz EINER Seite jenseits der zwei plattformfreien. Die
        /// Zuordnung Nummer → Hülle ist die bitgleiche Übernahme von
        /// <c>AssistentSeiten.ERZEUGER</c>; die zwei fehlenden Fälle
        /// (<c>KOMPONENTEN_ITEM</c>, <c>PROJEKT_ITEM</c>) beantwortet seit W16a-O-4
        /// <see cref="AssistentAnsichtQuelle"/> selbst.
        /// </summary>
        private static IReadOnlyDictionary<string, object> Seitengaben(
            AssistentCtrl ctrl, int nr, string projektName)
        {
            int id = ctrl.ProjektId;
            string name = projektName ?? "";

            switch (nr)
            {
                case WizardItemClass.GEBAEUDE_ITEM:
                    return GebaeudeHuelle.Gaben(id, name, ctrl.Gebaeude,
                                                wizard: true, admin: false);

                case WizardItemClass.WAERMEBEDARF_ITEM:
                    return WaermebedarfExternHuelle.Gaben(null, id, name, ctrl.Waermebedarf,
                                                          wizard: true);

                case WizardItemClass.PROZESS_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenProzess(id, ctrl.Prozess);

                case WizardItemClass.STROMSTD_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenStrom(id, ctrl.Stromverbraucher);

                case WizardItemClass.STROMLASTGANG_ITEM:
                    return StromganglinieHuelle.Gaben(id, ctrl.Stromganglinie, wizard: true);

                case WizardItemClass.WP_ITEM:
                    return WaermepumpenHuelle.Gaben(null, id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SOLAR_ITEM:
                    return SolarkollektorHuelle.ProjektGaben(id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.PV_ITEM:
                    return PhotovoltaikHuelle.Gaben(null, id, WizardItemClass.PV_TYP,
                                                    ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SP_ITEM:
                    return StromspeicherHuelle.Gaben(null, id, WizardItemClass.SP_TYP,
                                                     ctrl.Erzeuger, wizard: true);

                case WizardItemClass.KESSEL_ITEM:
                    return HeizkesselHuelle.Gaben(null, id, WizardItemClass.KESSEL_TYP,
                                                  ctrl.Erzeuger, wizard: true);

                case WizardItemClass.BHKW_ITEM:
                    return BhkwHuelle.Gaben(null, id, WizardItemClass.BHKW_TYP,
                                            ctrl.Erzeuger, wizard: true);

                default:
                    return null;
            }
        }
    }
}
