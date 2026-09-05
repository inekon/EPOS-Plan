using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Photovoltaik-Modulkatalogs (iU9-W14a.3,
    /// Ausprägung <see cref="ModulKatalogArt.Photovoltaik"/>).
    ///
    /// <para>Vorbild <c>Views/Photovoltaik/Form_AdminPV</c> (297 Z.) — im selben Schritt
    /// gelöscht (Regel M1). Sie war die liegengebliebene der beiden Modulkataloge: EIN
    /// englischer Text bei 29 (Befund W14-B37), Löschen OHNE Rückfrage (W14-B35), ein
    /// Update ohne Fehlermeldung (W14-B33) und drei tote Stellen (W14-B31, B32, B38).
    /// Mit der gemeinsamen Komponente steht sie auf dem Stand des Stromspeichers.</para>
    ///
    /// <para><b>Der Maskenschlüssel lebt seit W14a.0h.</b> Bis dahin legte
    /// <c>Hauptfensterrahmen.MenuItem_PV_Bearbeiten_Click</c> die Maske selbst an, und
    /// <c>MenueCtrl.PV()</c>, <c>Masken.PvAdmin</c> und der Zweig in
    /// <c>WinFormsNavigation</c> waren eine tote Kette aus drei Stellen
    /// (Befund W14-B36).</para>
    /// </summary>
    internal static class PvAdminHuelle
    {
        /// <summary>Zeigt den Modulkatalog als eigenes Fenster (<c>Masken.PvAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            return ModulKatalogHuelle.Oeffnen(besitzer, Profil(), Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static ModulKatalogProfil Profil()
        {
            return ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            ModulKatalogProfil profil = Profil();
            var gaben = ModulKatalogHuelle.GemeinsameGaben(profil);

            gaben["Wege"] = new ModulKatalogWege
            {
                Liste = Zeilen,
                Detail = name => ModulKatalogHuelle.Felder(profil, Anzeige(name)),
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
            foreach (var z in new PhotovoltaikStammCtrl().Filtern("Alle"))
                liste.Add(new ModulZeile(z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>
        /// Die dreizehn Anzeigefelder eines Katalogmoduls, bereits als Text — der
        /// Detailblock von <c>Form_AdminPV.listBox_PV_SelectedIndexChanged</c>
        /// (Z. 136-167).
        /// </summary>
        /// <remarks>
        /// Die Formatierung ist wörtlich: Wirkungsgrad und Leistung mit <c>F2</c>
        /// (Z. 153-154), die übrigen roh. Gelesen wird über
        /// <see cref="PhotovoltaikStammCtrl.ReadSingle"/> statt über das inline-SQL
        /// des Vorläufers (Befund W14-B12).
        /// </remarks>
        private static IReadOnlyDictionary<string, string> Anzeige(string name)
        {
            var ctrl = new PhotovoltaikStammCtrl();
            ctrl.ReadSingle(name);
            if (ctrl.rows == 0) return null;

            PhotovoltaikModel m = ctrl.items[0];
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ModulKatalogProfil.FeldBezeichner] = m.m_szName ?? "",
                [ModulKatalogProfil.FeldFirma] = m.m_szFirma ?? "",
                [ModulKatalogProfil.FeldBeschreibung] = m.m_szBeschreibung ?? "",
                [ModulKatalogProfil.FeldLeistung] = m.m_Leistung.ToString("F2"),
                [ModulKatalogProfil.FeldWirkungsgrad] = m.m_Wirkungsgrad.ToString("F2"),
                [ModulKatalogProfil.FeldUMpp] = m.m_U_Mpp.ToString(),
                [ModulKatalogProfil.FeldULeerlauf] = m.m_U_Leerlauf.ToString(),
                [ModulKatalogProfil.FeldIMpp] = m.m_I_Mpp.ToString(),
                [ModulKatalogProfil.FeldIKurzschluss] = m.m_I_Kurzschluss.ToString(),
                [ModulKatalogProfil.FeldTempKoeff] = m.m_Temp_Coeff_Pmax.ToString(),
                [ModulKatalogProfil.FeldLaenge] = m.m_Laenge.ToString(),
                [ModulKatalogProfil.FeldBreite] = m.m_Breite.ToString(),
                [ModulKatalogProfil.FeldModulkosten] = m.m_Modulkosten.ToString(),
                // Paket A/B des PV-Ertragsmodells (Merge 5)
                [ModulKatalogProfil.FeldTNoct] = m.m_T_NOCT.ToString(),
                [ModulKatalogProfil.FeldTechnologie] = m.m_Technologie ?? ""
            };
        }

        private static KatalogSpeicherErgebnis Schreiben(IReadOnlyList<ModulFeldwert> felder,
                                                         bool neu, string schluessel)
        {
            var m = new PhotovoltaikModel
            {
                m_szName = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldBezeichner),
                m_szFirma = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldFirma),
                m_szBeschreibung = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldBeschreibung),
                m_Leistung = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldLeistung),
                m_Wirkungsgrad = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldWirkungsgrad),
                m_U_Mpp = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldUMpp),
                m_U_Leerlauf = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldULeerlauf),
                m_I_Mpp = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldIMpp),
                m_I_Kurzschluss = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldIKurzschluss),
                m_Temp_Coeff_Pmax = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldTempKoeff),
                m_Laenge = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldLaenge),
                m_Breite = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldBreite),
                m_Modulkosten = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldModulkosten),
                // Paket A/B des PV-Ertragsmodells (Merge 5): NOCT (0 = nicht gepflegt) und
                // Zelltechnologie (leer = NULL, siehe PhotovoltaikStammCtrl.TechnologieParam).
                m_T_NOCT = ModulKatalogHuelle.Zahl(felder, ModulKatalogProfil.FeldTNoct),
                m_Technologie = LeerAlsNull(ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldTechnologie))
            };

            PhotovoltaikStammCtrl.SpeicherErgebnis e =
                PhotovoltaikStammCtrl.SpeichernAus(m, neu, schluessel);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static string LeerAlsNull(string wert)
            => string.IsNullOrWhiteSpace(wert) ? null : wert.Trim();

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            PhotovoltaikStammCtrl.SpeicherErgebnis e = PhotovoltaikStammCtrl.Loeschen(name);
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
