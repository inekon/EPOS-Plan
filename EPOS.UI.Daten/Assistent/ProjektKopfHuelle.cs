using System;
using System.Collections.Generic;
using EPOS.UI.Seiten.Assistent;
using EPOS.UI.Seiten.Start;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE der ersten Assistentenseite (iU9-W15a.6) — sie löst
    /// <c>Wizard_Projekt</c> ab.
    ///
    /// <para><b>Sie liegt seit W16a-O-4 in <c>EPOS.UI.Daten</c></b> und nicht mehr in
    /// der Windows-Anwendung: Sie ruft ausschließlich Kern-Controller
    /// (<c>ProjektCtrl.Kopf</c>, <c>KlimaregionStammCtrl</c>) und kennt keine
    /// Plattform. Damit steht der Projektkopf des Assistenten auch auf dem iPad.</para>
    ///
    /// <para><b>Weg (a) der Vermessung § 13.5.</b> <c>Wizard_Projekt</c> war die einzige
    /// Assistentenseite mit einem <c>Get*</c>-Rückweg (Befund W15a-B42). Statt eines
    /// neuen Vertrags trägt die Hülle eine EINELEMENTIGE geteilte Liste vom Typ
    /// <see cref="ProjektKopfDaten"/> — dieselbe Mechanik wie die vier Bedarfsseiten aus
    /// iU9-W9.0a, ohne Umbau an <c>BlazorAssistentSeite</c> (Risiko R-W15a-8).</para>
    ///
    /// <para><b>Bestücken liest den Projektkopf neu</b> (<c>ProjektCtrl.Kopf</c>) — außer
    /// im Neu-Zweig, wo der Assistent einen leeren Namen übergibt; dann bleibt stehen,
    /// was der Anwender bereits eingetippt hat. <see cref="ProjektKopfDaten.NameAenderbar"/>
    /// setzt der Rahmen VOR dem Bestücken, es ist der Ersatz für
    /// <c>SetEditProjektName(bool)</c>.</para>
    /// </summary>
    internal static class ProjektKopfHuelle
    {
        // iU9-W16a.5 / W16a-O-4: Das Wunschmass MASS (760 x 560) ist entfallen. Es
        // beschrieb die Groesse des randlosen WinForms-Formulars, das es seit W16a.5
        // nicht mehr gibt; im Assistenten steht die Seite als Razor-Komponente und
        // richtet sich nach ihrem Wirt. Mit dem Umzug nach EPOS.UI.Daten (W16a-O-4)
        // faellt damit auch die letzte System.Drawing-Zeile dieser Huelle.

        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ der Seite.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            string projektName, List<ProjektKopfDaten> modelle)
        {
            // Die geteilte Liste traegt GENAU EIN Element - der Rahmen legt es an,
            // wenn er selbst noch keines hat.
            if (modelle.Count == 0) modelle.Add(new ProjektKopfDaten());
            ProjektKopfDaten kopf = modelle[0];

            // Bearbeiten-Modus: den gespeicherten Stand lesen. Neu-Modus (leerer Name):
            // stehen lassen, was schon eingetippt ist - der Vorlaeufer setzte in diesem
            // Zweig nur die beiden Datumsfelder auf heute.
            if (!string.IsNullOrEmpty(projektName))
            {
                ProjektKopfDaten gelesen = ProjektCtrl.Kopf(projektName);
                if (gelesen != null)
                {
                    kopf.Name = gelesen.Name;
                    kopf.Beschreibung = gelesen.Beschreibung;
                    kopf.Kunde = gelesen.Kunde;
                    kopf.Bearbeiter = gelesen.Bearbeiter;
                    kopf.Erstelldatum = gelesen.Erstelldatum;
                    kopf.Aenderungsdatum = gelesen.Aenderungsdatum;
                    kopf.IdKlimaregion = gelesen.IdKlimaregion;
                    kopf.Klimaname = gelesen.Klimaname;
                }
            }

            // #527: EINE Lesung der Klappliste - Eintraege wie in der Kopfleiste, dazu die
            // blanken Stammnamen fuer die Vorbelegung und den Rueckweg der Seite.
            (IReadOnlyList<(int Id, string Text)> eintraege, IReadOnlyDictionary<int, string> namen)
                = Klimaregionen();

            if (string.IsNullOrEmpty(projektName) && string.IsNullOrEmpty(kopf.Name))
            {
                // Vorbelegung eines NEUEN Projekts (Nutzerauftrag 02.09.2026, mit Merge 5 aus
                // Wizard_Projekt): Bearbeiter = angemeldeter Benutzer, Klimaregion = die des
                // aktiven Projekts. Nur leere Felder werden belegt.
                //
                // #527: Die Region reist als STAMM-Id UND Stammname - so steht sie sichtbar
                // in der Klappliste, und die Pflichtregel sieht dasselbe wie der Anwender.
                // Vorher kam hier die Id der PROJEKTKOPIE herein: Sie traf keinen Eintrag,
                // das Feld stand leer, und die Regel hielt es trotzdem fuer gefuellt.
                if (string.IsNullOrEmpty(kopf.Bearbeiter)) kopf.Bearbeiter = Environment.UserName;
                if (kopf.IdKlimaregion <= 0 && string.IsNullOrEmpty(kopf.Klimaname))
                {
                    int stamm = ProjektCtrl.KlimaregionDesAktivenProjekts();
                    if (stamm > 0 && namen.TryGetValue(stamm, out string stammname))
                    {
                        kopf.IdKlimaregion = stamm;
                        kopf.Klimaname = stammname;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = kopf,
                ["Klimaregionen"] = eintraege,
                ["Klimanamen"] = namen,
                ["KlimaHerkunft"] = new Func<int, KlimaHerkunftGaben>(Herkunft),
                ["KlimaPlatzhalterText"] = Text_("START_KLIMA_REGION", "Region auswählen"),
                ["KlimaHerkunftText"] = Text_("START_KLIMA_HERKUNFT", "Klimadaten: {0} · {1} · {2} · Import {3}"),
                ["KlimaHerkunftKurzText"] = Text_("START_KLIMA_HERKUNFT_KURZ", "Klimadaten: {0} · {1}"),
                // Pflichtfelder und Namensdoppel (Nutzerauftrag 02.09.2026, Merge 5)
                ["VergebeneNamen"] = VergebeneNamen(),
                ["PflichtMarke"] = " *",
                ["PflichtText"] = Text_("WZP_PFLICHT", "(* = Pflichtfeld)"),
                ["TextNameLeer"] = Text_("WZP_NAME_LEER", "Bitte einen Projektnamen eingeben."),
                ["TextNameVorhanden"] = Text_("WZP_NAME_VORHANDEN", "Ein Projekt mit diesem Namen existiert bereits."),
                ["TextKlimaLeer"] = Text_("WZP_KLIMA_LEER", "Bitte eine Klimaregion wählen."),
                ["PlatzhalterBeschreibung"] = Text_("WZP_BESCHREIBUNG_HINT",
                    "Kurzbeschreibung: Vorhaben, Standort, Besonderheiten …"),

                ["KopfText"] = Text_("PKOPF_KOPF", "Projektkonfiguration"),
                ["HinweisText"] = Text_("PKOPF_HINWEIS",
                    "Geben Sie hier die administrativen Projektdaten ein:"),
                ["LabelName"] = Text_("PKOPF_LBL_NAME", "Projektname"),
                ["LabelBeschreibung"] = Text_("PKOPF_LBL_BESCHREIBUNG", "Beschreibung"),
                ["LabelKunde"] = Text_("PKOPF_LBL_KUNDE", "Kunde"),
                ["LabelBearbeiter"] = Text_("PKOPF_LBL_BEARBEITER", "Bearbeiter"),
                ["LabelAenderung"] = Text_("PKOPF_LBL_AENDERUNG", "Änderungsdatum"),
                ["LabelErstellt"] = Text_("PKOPF_LBL_ERSTELLT", "Erstelldatum"),
                ["LabelKlima"] = Text_("PKOPF_LBL_KLIMA", "Klimaregion")
            };
        }

        /// <summary>Alle vergebenen Projektnamen - fuer die Dublettenpruefung eines neuen Projekts.</summary>
        internal static IReadOnlyCollection<string> VergebeneNamen()
        {
            var namen = new List<string>();
            try { foreach (ProjektKopfZeile z in ProjektCtrl.NamenListe()) namen.Add(z.Name ?? ""); }
            catch (Exception ex) { Console.WriteLine("Projektnamen konnten nicht gelesen werden: " + ex.Message); }
            return namen;
        }

        /// <summary>
        /// <b>Die Klappliste der Klimaregion — dieselbe wie in der Kopfleiste der
        /// Startseite</b> (#527): aus <see cref="StartseiteCtrl.KlimaregionAuswahlzeilen"/>,
        /// der einen Quelle beider Listen. Die Einträge sind (Stamm-Id, Anzeigetext) —
        /// „Heidelberg (TRY 2045 sommerwarm)" —, in derselben Reihenfolge; dazu je Id der
        /// BLANKE Stammname, den der Speicherweg braucht (die Klammer ist nur Anzeige).
        ///
        /// <para>Die Liste lag hier bis dahin ein zweites Mal gebaut vor: eigene Schleife
        /// über <c>KlimaregionStammCtrl.ReadAll</c> mit blanken Namen und einer
        /// Datenbankabfrage je Eintrag für die Id — ohne Kurzform, anders als die
        /// Kopfleiste.</para>
        /// </summary>
        internal static (IReadOnlyList<(int Id, string Text)> Eintraege,
                         IReadOnlyDictionary<int, string> Namen) Klimaregionen()
        {
            var eintraege = new List<(int Id, string Text)>();
            var namen = new Dictionary<int, string>();
            foreach ((int Id, string Name, string Anzeige) z in StartseiteCtrl.KlimaregionAuswahlzeilen())
            {
                eintraege.Add((z.Id, z.Anzeige));
                namen[z.Id] = z.Name ?? "";
            }
            return (eintraege, namen);
        }

        /// <summary>
        /// Die Herkunft der GEWÄHLTEN Region für die Zeile unter der Klappliste — der
        /// Katalogsatz, den das Anlegen in das Projekt kopiert; <c>null</c> = keine Zeile.
        /// </summary>
        internal static KlimaHerkunftGaben Herkunft(int stammId)
            => stammId > 0 ? KlimaHerkunftAnzeige.Gaben(StartseiteCtrl.KlimaHerkunftStamm(stammId)) : null;

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
