using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Stromspeicher-Modulkatalogs (iU9-W14a.3,
    /// Ausprägung <see cref="ModulKatalogArt.Stromspeicher"/>).
    ///
    /// <para>Vorbild <c>Views/Stromspeicher/Form_AdminStromspeicher</c> (505 Z., die
    /// zweitgrößte Maske der Welle) — im selben Schritt gelöscht (Regel M1).</para>
    ///
    /// <para><b>Die sechs AP3-Gerätefelder stehen jetzt im Profil.</b> Der Vorläufer
    /// baute sie zur LAUFZEIT mit gerechneten Koordinaten auf und vergrößerte dabei das
    /// Fenster von 614 auf 1 036 px (Befund W14-B43); die Feldkarte sah sie deshalb gar
    /// nicht (Risiko R-W14-10). In Razor sind sie die zweite Feldgruppe.</para>
    ///
    /// <para><b>Entscheid E-5 (Befund W14-B39).</b> Der Kontextmenüweg
    /// <c>StromspeicherKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c> füllte
    /// <c>frm.list_spmodel</c> mit EINER Anlagenzeile, setzte <c>m_bItemBearbeiten</c>
    /// und schrieb die Liste nach OK zurück — die Maske hat <c>list_spmodel</c>
    /// allerdings NIE verändert, der Rückweg schrieb also das unveränderte Modell
    /// zurück. Er öffnet jetzt denselben Katalog wie jeder andere Weg; das
    /// Zurückschreiben entfällt (ein Leerlauf weniger, kein Verhaltensunterschied).</para>
    /// </summary>
    internal static class StromspeicherAdminHuelle
    {
        /// <summary>Zeigt den Modulkatalog als eigenes Fenster (<c>Masken.StromspeicherAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            return ModulKatalogHuelle.Oeffnen(besitzer, Profil(), Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static ModulKatalogProfil Profil()
        {
            return ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            ModulKatalogProfil profil = Profil();
            var gaben = ModulKatalogHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = new ModulKatalogWege
            {
                Liste = Zeilen,
                Detail = name => ModulKatalogHuelle.Felder(
                    profil, StromspeicherStammCtrl.KatalogsatzAnzeige(name)),
                Speichern = Schreiben,
                Loeschen = Loeschen
            };
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static IReadOnlyList<ModulZeile> Zeilen()
        {
            var liste = new List<ModulZeile>();
            foreach (var z in StromspeicherStammCtrl.KatalogZeilen())
                liste.Add(new ModulZeile(z.Id, z.Bezeichner));
            return liste;
        }

        private static KatalogSpeicherErgebnis Schreiben(IReadOnlyList<ModulFeldwert> felder,
                                                         bool neu, string schluessel)
        {
            var m = new StromspeicherModel
            {
                m_szBezeichner = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldBezeichner),
                m_szTyp = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldTyp),
                m_Energie = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldEnergie),
                m_Leistung = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldLeistung),
                m_Degradation = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldDegradation),
                m_Ladezustand = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldLadezustand),
                m_Modulkosten = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldModulkosten),

                m_WirkungsgradRT = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldWirkungsgradRt),
                m_ZyklenZugesichert = ModulKatalogHuelle.Ganzzahl(felder, ModulKatalogProfil.FeldZyklen),
                m_Verschleisskosten = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldVerschleisskosten),
                m_Leistungskosten = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldLeistungskosten),
                m_InvestitionFix = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldInvestitionFix),
                m_StandbyVerbrauch = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldStandby)
            };

            StromspeicherStammCtrl.SpeicherErgebnis e =
                StromspeicherStammCtrl.SpeichernAus(m, neu, schluessel);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            StromspeicherStammCtrl.SpeicherErgebnis e = StromspeicherStammCtrl.Loeschen(name);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
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
