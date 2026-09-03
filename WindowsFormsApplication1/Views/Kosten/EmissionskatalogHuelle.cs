using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Emissionsfaktor-Katalog" (iU9-W3.3).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Schon die WinForms-Maske kannte
    /// keine SQL-Zeile — alle Regeln stehen seit Etappe E4 in
    /// <see cref="EmissionskatalogCtrl"/> und <see cref="EmissionenCtrl"/>
    /// (Hausmuster Ä9). Diese Hülle ändert daran nichts: Sie ruft dieselben
    /// Methoden mit denselben Parametern und wandelt zwischen den Fachmodellen
    /// des Kerns und den Anzeigezeilen der Komponente.</para>
    ///
    /// <para><b>Beide Aufrufwege bleiben.</b> Im Rückgabemodus (aus dem
    /// Emissions-Tab) reicht der Dialog den markierten Wert zurück, statt ihn
    /// zu schreiben; im Verwaltungsmodus schreibt
    /// <c>EmissionskatalogCtrl.Uebernehmen</c> sofort. Die drei Ergebnisse der
    /// gelöschten Maske — <c>Uebernommen</c>, <c>ArtenGeaendert</c>,
    /// <c>WerteGeaendert</c> — stehen in <see cref="Ergebnis"/>.</para>
    /// </summary>
    internal static class EmissionskatalogHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 920 × 519 mit
        /// zwei Listen nebeneinander; die Blazor-Fassung stellt sie untereinander
        /// (Befund 03.09.2026: lieber höher als umgebrochen).</summary>
        private static readonly Size FENSTER = new Size(940, 860);

        /// <summary>Was der Dialog dem Aufrufer zurückgibt — die drei
        /// Eigenschaften der gelöschten Maske.</summary>
        internal sealed class Ergebnis
        {
            /// <summary>Der im Rückgabemodus übernommene Katalogwert; <c>null</c> = keiner.</summary>
            internal EmissionswertModel Uebernommen;

            /// <summary>Arten wurden angelegt, geändert, gelöscht oder ab-/angewählt.</summary>
            internal bool ArtenGeaendert;

            /// <summary>Es wurde ein Trägerwert geschrieben.</summary>
            internal bool WerteGeaendert;
        }

        /// <summary>
        /// Zeigt den Dialog.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="carrierId">Träger; 0 = Verwaltungsmodus ohne Träger.</param>
        /// <param name="carrierName">Anzeigename für die Kopfzeile.</param>
        /// <param name="artVorwahl">Kürzel der Art, auf die vorgewählt wird; leer = die erste.</param>
        /// <param name="rueckgabemodus">true, wenn „Übernehmen" den Wert
        /// zurückreichen statt schreiben soll (Aufruf aus dem Emissions-Tab).</param>
        internal static Ergebnis Oeffnen(IWin32Window besitzer, int carrierId, string carrierName,
                                         string artVorwahl, bool rueckgabemodus)
        {
            int traeger = carrierId > 0 ? carrierId : 0;
            string name = carrierName ?? "";

            // Die geladenen Fachmodelle bleiben hier stehen: Die Komponente
            // arbeitet mit Ids, der Aufrufer erwartet ein EmissionswertModel.
            var arten = new List<EmissionsartModel>();
            var werte = new List<EmissionswertModel>();

            var ergebnis = new Ergebnis();
            BlazorDialogForm<EmissionskatalogDialog> dlg = null;

            // Der Modus-Schalter im Katalog wirkt IMMER auf die globale Vorgabe
            // (Konzept F7); der Projekt-Override sitzt im Emissions-Tab.
            string modusBeimOeffnen = EmissionenCtrl.VorgabeLesen();
            bool co2eBeimOeffnen = string.Equals(modusBeimOeffnen, DbWerte.EMISSION_MODUS_CO2E,
                                                 StringComparison.Ordinal);

            Func<IReadOnlyList<EmissionsartZeile>> artenLaden = () =>
            {
                arten.Clear();
                arten.AddRange(EmissionskatalogCtrl.Arten(false));

                var zeilen = new List<EmissionsartZeile>(arten.Count);
                foreach (EmissionsartModel a in arten) zeilen.Add(ArtZeile(a));
                return zeilen;
            };

            Func<int, IReadOnlyList<EmissionswertZeile>> werteLaden = artId =>
            {
                werte.Clear();
                werte.AddRange(EmissionskatalogCtrl.Werte(artId, traeger));

                var zeilen = new List<EmissionswertZeile>(werte.Count);
                foreach (EmissionswertModel w in werte) zeilen.Add(WertZeile(w));
                return zeilen;
            };

            var parameter = new Dictionary<string, object>
            {
                ["Arten"] = artenLaden(),
                ["ArtenLaden"] = artenLaden,
                ["WerteLaden"] = werteLaden,
                ["ArtVorwahl"] = artVorwahl ?? "",
                ["MitTraeger"] = traeger > 0,
                ["Rueckgabemodus"] = rueckgabemodus,
                ["ModusCo2e"] = co2eBeimOeffnen,
                ["Einheiten"] = (IReadOnlyList<ValueTuple<int, string>>)new List<ValueTuple<int, string>>
                {
                    new ValueTuple<int, string>(0, DbWerte.EMISSION_EINHEIT_G_KWH),
                    new ValueTuple<int, string>(1, DbWerte.EMISSION_EINHEIT_MG_KWH)
                },
                ["VorgabeQuelltext"] = DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT,

                // ---------------------------------------------------------- Arten
                ["AuswahlSetzen"] = new Func<int, bool, string>((artId, neu) =>
                {
                    string grund;
                    return EmissionskatalogCtrl.AuswahlSetzen(artId, neu, out grund)
                        ? null : (grund ?? "");
                }),

                ["ArtAnlegen"] = new Func<EmissionsartEingabe, string>(e =>
                {
                    var a = new EmissionsartModel
                    {
                        Kuerzel = e.Kuerzel,
                        Name = e.Name,
                        Einheit = e.Einheit,
                        Co2Aequivalent = e.Gwp,
                        AequivalentQuelle = e.AequivalentQuelle,
                        Sortierung = arten.Count > 0 ? arten[arten.Count - 1].Sortierung + 10 : 10
                    };
                    string grund;
                    return EmissionskatalogCtrl.ArtAnlegen(a, out grund) > 0 ? null : (grund ?? "");
                }),

                ["ArtAendern"] = new Func<EmissionsartEingabe, string>(e =>
                {
                    EmissionsartModel alt = ArtZuId(arten, e.Id);
                    if (alt == null) return "";

                    // Kopie wie im Vorläufer: Pflicht-, Auslieferungs- und
                    // Auswahlkennzeichen bleiben, was sie waren.
                    var kopie = new EmissionsartModel
                    {
                        ID = alt.ID,
                        Kuerzel = e.Kuerzel,
                        Name = e.Name,
                        Einheit = e.Einheit,
                        Co2Aequivalent = e.Gwp,
                        AequivalentQuelle = e.AequivalentQuelle,
                        IstPflicht = alt.IstPflicht,
                        IstAuslieferung = alt.IstAuslieferung,
                        Ausgewaehlt = alt.Ausgewaehlt,
                        Sortierung = alt.Sortierung
                    };
                    string grund;
                    return EmissionskatalogCtrl.ArtAendern(kopie, out grund) ? null : (grund ?? "");
                }),

                ["ArtLoeschenDelegat"] = new Func<int, string>(artId =>
                {
                    string grund;
                    return EmissionskatalogCtrl.ArtLoeschen(artId, out grund) ? null : (grund ?? "");
                }),

                // ---------------------------------------------------------- Werte
                ["WertAnlegen"] = new Func<EmissionswertEingabe, string>(e =>
                {
                    var w = new EmissionswertModel
                    {
                        EmissionsartId = e.ArtId,
                        CarrierId = e.AlsVorlage ? (int?)null
                                                 : (traeger > 0 ? (int?)traeger : null),
                        Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT,
                        QuelleText = e.QuelleText,
                        Wert = e.Wert,
                        IstCo2e = e.IstCo2e,
                        GueltigAb = DateTime.Today
                    };
                    string grund;
                    return EmissionskatalogCtrl.WertAnlegen(w, out grund) > 0 ? null : (grund ?? "");
                }),

                ["WertAendern"] = new Func<EmissionswertEingabe, string>(e =>
                {
                    EmissionswertModel alt = WertZuId(werte, e.Id);
                    if (alt == null) return "";

                    var kopie = new EmissionswertModel
                    {
                        ID = alt.ID,
                        EmissionsartId = alt.EmissionsartId,
                        CarrierId = alt.CarrierId,          // der Geltungsbereich bleibt
                        Quelle = alt.Quelle,
                        QuelleText = e.QuelleText,
                        Wert = e.Wert,
                        IstCo2e = e.IstCo2e,
                        IstAktiv = alt.IstAktiv,
                        HerkunftId = alt.HerkunftId,
                        GueltigAb = alt.GueltigAb
                    };
                    string grund;
                    return EmissionskatalogCtrl.WertAendern(kopie, out grund) ? null : (grund ?? "");
                }),

                ["WertLoeschenDelegat"] = new Func<int, string>(wertId =>
                {
                    string grund;
                    return EmissionskatalogCtrl.WertLoeschen(wertId, out grund) ? null : (grund ?? "");
                }),

                ["WertUebernehmenDelegat"] = new Func<int, string>(wertId =>
                {
                    EmissionswertModel w = WertZuId(werte, wertId);
                    if (w == null) return "";

                    string grund;
                    return EmissionskatalogCtrl.Uebernehmen(traeger, w, out grund)
                        ? null : (grund ?? "");
                }),

                ["Rueckfrage"] = new Func<string, bool>(
                    text => Dienste.Dialog.Frage(text, Text_("EMK_TITEL", "Emissionsfaktor-Katalog"))),

                // ---------------------------------------------------------- Texte
                ["TitelText"] = Text_("EMK_TITEL", "Emissionsfaktor-Katalog"),
                ["KontextText"] = traeger > 0
                    ? string.Format(CultureInfo.CurrentCulture,
                        Text_("EMK_KONTEXT_TRAEGER", "Träger: {0}"), name)
                    : Text_("EMK_KONTEXT_VERWALTUNG",
                        "Verwaltungsmodus — Arten und trägerunabhängige Vorlagen"),
                ["LabelModus"] = Text_("EMK_MODUS", "CO₂-Berechnung:"),
                ["ModusCo2Text"] = Text_("EMK_MODUS_CO2", "CO₂"),
                ["ModusCo2eText"] = Text_("EMK_MODUS_CO2E", "CO₂-Äquivalent (GWP₁₀₀)"),
                ["ModusOrtText"] = Text_("EMK_MODUS_ORT", "[globale Vorgabe]"),
                ["GruppeArten"] = Text_("EMK_GRP_ARTEN", "Emissionsarten"),
                ["GruppeWerte"] = Text_("EMK_GRP_WERTE", "Werte"),
                ["VorlageGruppeWerteArt"] = Text_("EMK_GRP_WERTE_ART", "Werte: {0}{1}"),
                ["WerteTitelZusatz"] = traeger > 0 ? " — " + name : "",
                ["SpalteWahl"] = Text_("EMK_SP_WAHL", "Wahl"),
                ["SpalteAuswahl"] = Text_("EMK_SP_AUSWAHL_KOPF", "im Tab"),
                ["HinweisAuswahl"] = Text_("EMK_SP_AUSWAHL",
                    "Ausgewählte Arten erscheinen als Feld im Emissions-Tab und gehen in die CO₂e-Summe ein."),
                ["SpalteKuerzel"] = Text_("EMK_SP_KUERZEL", "Kürzel"),
                ["SpalteName"] = Text_("EMK_SP_NAME", "Name"),
                ["SpalteEinheit"] = Text_("EMK_SP_EINHEIT", "Einheit"),
                ["SpalteGwp"] = Text_("EMK_SP_GWP", "GWP₁₀₀"),
                ["SpalteQuelle"] = Text_("EMK_SP_QUELLE", "Quelle"),
                ["SpalteWert"] = Text_("EMK_SP_WERT", "Wert"),
                ["SpalteCo2e"] = Text_("EMK_SP_CO2E", "bereits CO₂e?"),
                ["SpalteAktiv"] = Text_("EMK_SP_AKTIV", "aktiv"),
                ["TextJa"] = Text_("EMK_JA", "ja"),
                ["TextNein"] = Text_("EMK_NEIN", "nein"),
                ["TextGeltend"] = Text_("EMK_AKTIV", "◆ geltend"),
                ["TextTraeger"] = Text_("EMK_TRAEGER", "Träger"),
                ["TextVorlage"] = Text_("EMK_VORLAGE", "Vorlage"),
                ["PflichtKurz"] = Text_("EMK_TIP_PFLICHT",
                    "Pflichtart — nicht abwählbar, nicht löschbar."),
                ["AuslieferungKurz"] = Text_("EMK_TIP_AUSLIEFERUNG",
                    "Ausgelieferte Art — abwählbar, aber nicht löschbar."),
                ["UnveraenderlichKurz"] = Text_("EMK_TIP_UNVERAENDERLICH",
                    "Ausgelieferter Katalogwert — unveränderlich. Übernehmen ist möglich."),
                ["HinweisText"] = traeger > 0
                    ? Text_("EMK_HINWEIS_TRAEGER",
                        "„Übernehmen“ kopiert den markierten Wert als geltenden Trägerwert und " +
                        "vermerkt die Herkunft. Eine spätere Katalogänderung wirkt NICHT zurück. " +
                        "Werte ohne Träger sind Vorlagen für alle Träger.")
                    : Text_("EMK_HINWEIS_VERWALTUNG",
                        "Ohne Trägerkontext zeigt der Katalog die Arten und die " +
                        "trägerunabhängigen Vorlagen. Ausgelieferte Einträge sind " +
                        "unveränderlich — abwählen statt löschen."),
                ["ArtNeuText"] = Text_("EMK_ART_NEU", "Neu…"),
                ["ArtBearbeitenText"] = Text_("EMK_ART_BEARBEITEN", "Bearbeiten…"),
                ["ArtLoeschenText"] = Text_("EMK_ART_LOESCHEN", "Löschen"),
                ["UebernehmenText"] = Text_("EMK_UEBERNEHMEN", "Übernehmen"),
                ["WertNeuText"] = Text_("EMK_WERT_NEU", "Neu…"),
                ["WertBearbeitenText"] = Text_("EMK_WERT_BEARBEITEN", "Bearbeiten…"),
                ["WertLoeschenText"] = Text_("EMK_WERT_LOESCHEN", "Löschen"),
                ["ArtDialogNeu"] = Text_("EMK_ART_DLG_NEU", "Neue Emissionsart"),
                ["ArtDialogBearbeiten"] = Text_("EMK_ART_DLG_BEARB", "Emissionsart bearbeiten"),
                ["FeldKuerzel"] = Text_("EMK_ART_F_KUERZEL", "Kürzel:"),
                ["FeldName"] = Text_("EMK_ART_F_NAME", "Name:"),
                ["FeldEinheit"] = Text_("EMK_ART_F_EINHEIT", "Einheit:"),
                ["FeldGwp"] = Text_("EMK_ART_F_GWP", "CO₂-Äquivalent (GWP₁₀₀):"),
                ["FeldQuelle"] = Text_("EMK_ART_F_QUELLE", "Quelle des Faktors:"),
                ["PflichtHinweis"] = Text_("EMK_ART_PFLICHT_HINWEIS",
                    "CO₂ ist die Pflichtart: Der Äquivalenzfaktor bleibt 1."),
                ["WertDialogNeu"] = Text_("EMK_WERT_DLG_NEU", "Neuer eigener Wert"),
                ["WertDialogBearbeiten"] = Text_("EMK_WERT_DLG_BEARB", "Eigenen Wert bearbeiten"),
                ["FeldWertText"] = Text_("EMK_WERT_F_TEXT", "Bezeichnung/Quelle:"),
                ["FeldWert"] = Text_("EMK_WERT_F_WERT", "Wert:"),
                ["FeldCo2e"] = Text_("EMK_WERT_CO2E",
                    "Wert ist bereits ein CO₂-Äquivalent (nicht weiter aufsummieren)"),
                ["FeldVorlage"] = Text_("EMK_WERT_VORLAGE",
                    "Vorlage für ALLE Träger (ohne Trägerbindung)"),
                ["MeldungKuerzelLeer"] = Text_("EMK_ART_KUERZEL_LEER",
                    "Das Kürzel darf nicht leer sein."),
                ["MeldungGwpUngueltig"] = Text_("EMK_ART_GWP_UNGUELTIG",
                    "Der Äquivalenzfaktor muss eine Zahl sein (Komma oder Punkt)."),
                ["MeldungWertUngueltig"] = Text_("EMK_WERT_UNGUELTIG",
                    "Der Wert muss eine Zahl ≥ 0 sein (Komma oder Punkt)."),
                ["MeldungUnveraenderlich"] = Text_("EMK_WERT_UNVERAENDERLICH",
                    "Ausgelieferte Katalogwerte sind unveränderlich — sie werden über neue " +
                    "Jahreszeilen der gesetzlichen Parameter fortgeschrieben. Legen Sie " +
                    "einen eigenen Wert an."),
                ["MeldungUebernahmeLeer"] = Text_("EMK_UEBERNAHME_LEER",
                    "Der gewählte Eintrag trägt keinen Zahlenwert."),
                ["VorlageArtLoeschen"] = Text_("EMK_ART_LOESCHEN_FRAGE",
                    "Emissionsart „{0}“ löschen?"),
                ["VorlageWertLoeschen"] = Text_("EMK_WERT_LOESCHEN_FRAGE", "Wert „{0}“ löschen?"),
                ["FrageAbwaehlen"] = Text_("EMK_ART_ABWAEHLEN_FRAGE",
                    "Die Art stattdessen abwählen? Sie verschwindet dann aus den " +
                    "Emissionsfeldern und aus der CO₂e-Summe, ihre Werte bleiben erhalten."),
                // Der Vorläufer rief hier T("KDLG_BTN_OK", "OK") — den Schlüssel gab
                // es nie, gezeigt wurde immer der deutsche Rückfall. Jetzt steht der
                // Haustext (iU9-W3.5).
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = Text_("PVW_ABBRECHEN", "Abbrechen"),

                ["Geschlossen"] = EventCallback.Factory.Create<EmissionskatalogErgebnis>(
                    new object(), erg =>
                    {
                        ergebnis.ArtenGeaendert = erg.ArtenGeaendert;
                        ergebnis.WerteGeaendert = erg.WerteGeaendert;
                        if (erg.UebernommenId > 0)
                            ergebnis.Uebernommen = WertZuId(werte, erg.UebernommenId);

                        // Beenden(): Die globale Vorgabe nur schreiben, wenn sie
                        // sich geändert hat — wortgleich zum Vorläufer.
                        string modus = erg.ModusCo2e
                            ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
                        if (!string.Equals(modus, EmissionenCtrl.VorgabeLesen(),
                                           StringComparison.Ordinal))
                            EmissionenCtrl.VorgabeSchreiben(modus);

                        if (dlg != null) dlg.Schliessen(erg.Bestaetigt);
                    })
            };

            dlg = new BlazorDialogForm<EmissionskatalogDialog>(
                Text_("EMK_TITEL", "Emissionsfaktor-Katalog"), FENSTER, parameter);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        // =================================================================== Wandlung

        private static EmissionsartZeile ArtZeile(EmissionsartModel a)
        {
            return new EmissionsartZeile(
                a.ID, a.Kuerzel ?? "", a.Name ?? "", a.Einheit ?? "",
                a.Co2Aequivalent,
                a.Co2Aequivalent.ToString("0.###", CultureInfo.CurrentCulture),
                a.AequivalentQuelle ?? "",
                a.Ausgewaehlt, a.IstPflicht, a.IstAuslieferung);
        }

        private static EmissionswertZeile WertZeile(EmissionswertModel w)
        {
            return new EmissionswertZeile(
                w.ID,
                w.Herkunftstext ?? "",
                w.QuelleText ?? "",
                w.Wert,
                w.Wert.HasValue ? w.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "",
                w.IstCo2e,
                w.IstAktiv,
                w.CarrierId.HasValue,
                DarfAendern(w));
        }

        /// <summary>Wortgleich aus der gelöschten Maske: Nur ein EIGENER, nicht
        /// ausgelieferter Wert ist änderbar.</summary>
        private static bool DarfAendern(EmissionswertModel w)
        {
            return w != null && !w.IstAuslieferung && string.Equals(
                w.Quelle, DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT,
                StringComparison.OrdinalIgnoreCase);
        }

        private static EmissionsartModel ArtZuId(List<EmissionsartModel> arten, int id)
        {
            foreach (EmissionsartModel a in arten) if (a.ID == id) return a;
            return null;
        }

        private static EmissionswertModel WertZuId(List<EmissionswertModel> werte, int id)
        {
            foreach (EmissionswertModel w in werte) if (w.ID == id) return w;
            return null;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
