using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Wechselrichterverwaltung — die DRITTE Ausprägung von
    /// <see cref="ModulKatalogArt"/> (Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026,
    /// Stufe S1.4 des <c>Konzept_Wechselrichter_EPOS-Plan.md</c>).
    ///
    /// <para><b>Sie löst keine WinForms-Maske ab, sie schließt eine Lücke.</b> Der
    /// Wechselrichter war die einzige Gerätefamilie ohne Katalog — es gibt keinen
    /// Vorläufer, dessen Feldnamen, Meldungen oder Layout zu erben wären. Alles, was
    /// diese Hülle tut, folgt deshalb Zeile für Zeile
    /// <see cref="PvAdminHuelle"/>.</para>
    ///
    /// <para><b>Der Herstellerfilter ist neu</b> (Konzept 6): Die zwei älteren
    /// Ausprägungen führen keinen; die CEC-Wechselrichterliste bringt über zweitausend
    /// Geräte von 152 Herstellern. Er hängt an zwei Delegaten
    /// (<c>ModulKatalogWege.Hersteller</c>/<c>.ListeGefiltert</c>) — ohne sie zeichnet
    /// der Dialog die Filterzeile gar nicht.</para>
    /// </summary>
    internal static class WechselrichterAdminHuelle
    {
        /// <summary>Zeigt die Verwaltung als eigenes Fenster (<c>Masken.WechselrichterAdmin</c>).</summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            return ModulKatalogHuelle.Oeffnen(besitzer, Profil(), Gaben());
        }

        /// <summary>Das übersetzte Profil der Ausprägung.</summary>
        internal static ModulKatalogProfil Profil()
        {
            return ModulKatalogProfil.Finde(ModulKatalogArt.Wechselrichter, Text);
        }

        /// <summary>Der PARAMETERSATZ — auch für eine Überlagerung in einem Blazor-Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            ModulKatalogProfil profil = Profil();
            Dictionary<string, object> gaben = ModulKatalogHuelle.GemeinsameGaben(profil);

            gaben["TextAlle"] = MyResource.Resource.PVIMP_ALLE;
            gaben["Wege"] = new ModulKatalogWege
            {
                Liste = () => Zeilen(""),
                Hersteller = Hersteller,
                ListeGefiltert = Zeilen,
                Detail = name => ModulKatalogHuelle.Felder(profil, Anzeige(name)),
                Speichern = Schreiben,
                Loeschen = Loeschen
            };
            return gaben;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static IReadOnlyList<string> Hersteller()
        {
            return WechselrichterStammCtrl.Hersteller();
        }

        private static IReadOnlyList<ModulZeile> Zeilen(string hersteller)
        {
            var liste = new List<ModulZeile>();
            foreach (WechselrichterStammCtrl.KatalogZeile z in
                     new WechselrichterStammCtrl().Filtern(hersteller))
                liste.Add(new ModulZeile(z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>
        /// Die Anzeigefelder eines Katalogsatzes, bereits als Text.
        /// </summary>
        /// <remarks>
        /// <b>Kein Format über der Rohzahl.</b> Der PV-Katalog zeigt Leistung und
        /// Wirkungsgrad mit <c>F2</c>, weil sein Vorläufer das tat; hier gibt es keinen
        /// Vorläufer, und ein <c>F2</c> auf einem Sandia-Wirkungsgrad (0,9547) verlöre
        /// zwei Stellen beim ersten Speichern. Gezeigt wird deshalb, was in der
        /// Datenbank steht — in der Kultur des Anwenders, wie überall im Haus.
        /// </remarks>
        private static IReadOnlyDictionary<string, string> Anzeige(string name)
        {
            var ctrl = new WechselrichterStammCtrl();
            ctrl.ReadSingle(name);
            if (ctrl.rows == 0) return null;

            WechselrichterModel m = ctrl.items[0];
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ModulKatalogProfil.FeldBezeichner] = m.m_szName ?? "",
                [ModulKatalogProfil.FeldFirma] = m.m_szFirma ?? "",
                [ModulKatalogProfil.FeldBeschreibung] = m.m_szBeschreibung ?? "",
                [ModulKatalogProfil.FeldPAcNenn] = Zahl(m.m_P_AC_Nenn),
                [ModulKatalogProfil.FeldSAcMax] = Zahl(m.m_S_AC_Max),
                [ModulKatalogProfil.FeldPDcMax] = Zahl(m.m_P_DC_Max),
                [ModulKatalogProfil.FeldKosten] = Zahl(m.m_Kosten),
                [ModulKatalogProfil.FeldHerkunft] = m.m_Herkunft ?? "",

                [ModulKatalogProfil.FeldUMppMin] = Zahl(m.m_U_Mpp_Min),
                [ModulKatalogProfil.FeldUMppMax] = Zahl(m.m_U_Mpp_Max),
                [ModulKatalogProfil.FeldUDcMax] = Zahl(m.m_U_Dc_Max),
                [ModulKatalogProfil.FeldUStart] = Zahl(m.m_U_Start),
                [ModulKatalogProfil.FeldIDcMax] = Zahl(m.m_I_Dc_Max),
                [ModulKatalogProfil.FeldAnzahlMppt] = Ganz(m.m_Anzahl_Mppt),
                [ModulKatalogProfil.FeldStraengeJeMppt] = Ganz(m.m_Straenge_Je_Mppt),

                [ModulKatalogProfil.FeldEta05] = Zahl(m.m_Eta05),
                [ModulKatalogProfil.FeldEta10] = Zahl(m.m_Eta10),
                [ModulKatalogProfil.FeldEta20] = Zahl(m.m_Eta20),
                [ModulKatalogProfil.FeldEta30] = Zahl(m.m_Eta30),
                [ModulKatalogProfil.FeldEta50] = Zahl(m.m_Eta50),
                [ModulKatalogProfil.FeldEta100] = Zahl(m.m_Eta100),
                [ModulKatalogProfil.FeldEtaEuro] = Zahl(m.m_Eta_Euro),
                [ModulKatalogProfil.FeldEtaMax] = Zahl(m.m_Eta_Max),
                [ModulKatalogProfil.FeldPStandby] = Zahl(m.m_P_Standby),
                [ModulKatalogProfil.FeldPNacht] = Zahl(m.m_P_Nacht)
            };
        }

        /// <summary>
        /// Der Schreibweg der Maske.
        /// </summary>
        /// <remarks>
        /// <b>Ein leeres Feld bleibt NULL.</b> Der PV-Katalog weicht bei jedem leeren
        /// Zahlenfeld auf 0 aus (<c>ModulKatalogHuelle.Zahl</c>); beim Wechselrichter
        /// wäre das falsch — eine 0 bei <c>U_Dc_Max</c> hieße „Grenze null Volt" und
        /// sperrte jeden Strang, während NULL „keine Prüfung" heißt (Konzept 3.1). Die
        /// Hülle nimmt deshalb <see cref="ZahlOderNull"/>.
        /// </remarks>
        private static KatalogSpeicherErgebnis Schreiben(IReadOnlyList<ModulFeldwert> felder,
                                                         bool neu, string schluessel)
        {
            var m = new WechselrichterModel
            {
                m_szName = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldBezeichner),
                m_szFirma = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldFirma),
                m_szBeschreibung = ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldBeschreibung),

                m_P_AC_Nenn = ZahlOderNull(felder, ModulKatalogProfil.FeldPAcNenn),
                m_S_AC_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldSAcMax),
                m_P_DC_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldPDcMax),
                m_Kosten = ZahlOderNull(felder, ModulKatalogProfil.FeldKosten),

                m_U_Mpp_Min = ZahlOderNull(felder, ModulKatalogProfil.FeldUMppMin),
                m_U_Mpp_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldUMppMax),
                m_U_Dc_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldUDcMax),
                m_U_Start = ZahlOderNull(felder, ModulKatalogProfil.FeldUStart),
                m_I_Dc_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldIDcMax),
                m_Anzahl_Mppt = GanzOderNull(felder, ModulKatalogProfil.FeldAnzahlMppt),
                m_Straenge_Je_Mppt = GanzOderNull(felder, ModulKatalogProfil.FeldStraengeJeMppt),

                m_Eta05 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta05),
                m_Eta10 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta10),
                m_Eta20 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta20),
                m_Eta30 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta30),
                m_Eta50 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta50),
                m_Eta100 = ZahlOderNull(felder, ModulKatalogProfil.FeldEta100),
                m_Eta_Euro = ZahlOderNull(felder, ModulKatalogProfil.FeldEtaEuro),
                m_Eta_Max = ZahlOderNull(felder, ModulKatalogProfil.FeldEtaMax),
                m_P_Standby = ZahlOderNull(felder, ModulKatalogProfil.FeldPStandby),
                m_P_Nacht = ZahlOderNull(felder, ModulKatalogProfil.FeldPNacht),

                m_Herkunft = LeerAlsNull(ModulKatalogHuelle.Wert(felder, ModulKatalogProfil.FeldHerkunft))
            };

            // Die Sandia-Spalten fuehrt die Maske nicht - sie sind mitgeschriebenes
            // Katalogwissen des Imports (Konzept 3.3.3). Beim Aendern eines
            // importierten Satzes duerfen sie nicht mit NULL ueberschrieben werden;
            // deshalb kommen sie aus dem BESTAND (dieselbe Ueberlegung wie bei
            // alpha_SC/beta_OC in PhotovoltaikStammCtrl.SpeichernAus).
            if (!neu)
            {
                var bestand = new WechselrichterStammCtrl();
                bestand.ReadSingle(schluessel ?? m.m_szName);
                if (bestand.rows > 0)
                {
                    WechselrichterModel alt = bestand.items[0];
                    m.m_Sandia_Pdco = alt.m_Sandia_Pdco;
                    m.m_Sandia_Vdco = alt.m_Sandia_Vdco;
                    m.m_Sandia_Pso = alt.m_Sandia_Pso;
                    m.m_Sandia_C0 = alt.m_Sandia_C0;
                    m.m_Sandia_C1 = alt.m_Sandia_C1;
                    m.m_Sandia_C2 = alt.m_Sandia_C2;
                    m.m_Sandia_C3 = alt.m_Sandia_C3;
                    if (string.IsNullOrEmpty(m.m_Herkunft)) m.m_Herkunft = alt.m_Herkunft;
                }
            }
            else if (string.IsNullOrEmpty(m.m_Herkunft))
            {
                m.m_Herkunft = DbWerte.WR_HERKUNFT_HAND;
            }

            WechselrichterStammCtrl.SpeicherErgebnis e =
                WechselrichterStammCtrl.SpeichernAus(m, neu, schluessel);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static KatalogSpeicherErgebnis Loeschen(string name)
        {
            WechselrichterStammCtrl.SpeicherErgebnis e = WechselrichterStammCtrl.Loeschen(name);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        // =====================================================================
        // Umwandlungen
        // =====================================================================

        /// <summary>Eine Zahl als Anzeigetext; NULL wird zur LEEREN Zeichenkette.</summary>
        private static string Zahl(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString(CultureInfo.CurrentCulture) : "";
        }

        /// <summary>Eine Ganzzahl als Anzeigetext; NULL wird zur LEEREN Zeichenkette.</summary>
        private static string Ganz(int? wert)
        {
            return wert.HasValue ? wert.Value.ToString(CultureInfo.CurrentCulture) : "";
        }

        /// <summary>Der Feldwert als Zahl; ein leeres oder unlesbares Feld ergibt <c>null</c>.</summary>
        private static double? ZahlOderNull(IReadOnlyList<ModulFeldwert> felder, string schluessel)
        {
            string text = ModulKatalogHuelle.Wert(felder, schluessel);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return Program.ZahlParsen(text, out double d) ? (double?)d : null;
        }

        /// <summary>Der Feldwert als Ganzzahl; ein leeres oder unlesbares Feld ergibt <c>null</c>.</summary>
        private static int? GanzOderNull(IReadOnlyList<ModulFeldwert> felder, string schluessel)
        {
            string text = ModulKatalogHuelle.Wert(felder, schluessel);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return Program.GanzzahlParsen(text, out int n) ? (int?)n : null;
        }

        private static string LeerAlsNull(string wert)
            => string.IsNullOrWhiteSpace(wert) ? null : wert.Trim();

        private static string Text(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }
    }
}
