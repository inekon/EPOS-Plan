using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Hülle des Dialogs „BHKW-Wirtschaftlichkeit" (Etappe B5b).
    ///
    /// <para><b>Seit Etappe E3, Schritt 6 liegt sie in <c>EPOS.UI.Daten</c>,
    /// und sie hat keine Fensterhälfte mehr.</b> Der Dialog erscheint
    /// ausschließlich als <c>Ueberlagerung</c> der Wirtschaftlichkeitsseite —
    /// auf Windows wie auf iOS.</para>
    ///
    /// <para><b>Stichtag iZ5, zweite Maske.</b> Der Dialog lebt seit B5b als
    /// Razor-Komponente <see cref="BhkwWirtschaftlichkeitDialog"/> in
    /// <c>EPOS.UI</c>; die WinForms-Fassung <c>Form_BhkwWirtschaftlichkeit</c> ist
    /// mit demselben Schritt GELOESCHT (Regel M1: keine zweite Fassung derselben
    /// Maske).</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente kennt keine Datenbank
    /// (Hausregel <c>EPOS.UI/CLAUDE.md</c>). Alles, was sie zeigt, wird hier
    /// geladen — mit denselben Controllern und in derselben Reihenfolge wie zuvor
    /// im Konstruktor des Formulars — und alles, was sie schreibt, wird hier
    /// geschrieben: <c>KwkgAnlagenCtrl.Speichere(g, true)</c> (K7, alle Spalten des
    /// Dialogs — seit Etappe E7c samt Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und
    /// Stromkennzahl aus Schemaschritt 105, gepflegt in der Überlagerung „Sätze und
    /// Herkunft") und
    /// <c>WirtschaftlichkeitCtrl.SpeichereParameter</c>. Beides sind ZWEI benannte
    /// Wege, und der Dialog ruft sie ausschliesslich in seinem OK-Weg — er traegt
    /// OK und Abbrechen, und bis zum OK steht seine Eingabe in seinem
    /// Arbeitsstand.</para>
    ///
    /// <para><b>Der Sprung in die Tarifstruktur gehoert dem WIRT.</b> Der
    /// Sprungknopf „BHKW-Tarif…" der Stromsteuergruppe fuehrt in den Tarifdialog
    /// (Sicht BHKW; der zweite Sprung „Strombezug…" ist mit Q11 entfallen). Zu B5b war
    /// das eine WinForms-Maske ohne Weg, sie aus einem Blazor-Dialog heraus zu
    /// oeffnen; seit iU9-W2.3 ist der Tarifdialog SELBST Razor, und zwei WebViews
    /// uebereinander sind Risiko R2. Die Komponente meldet den Wunsch deshalb in
    /// ihrem Ergebnis (<c>BhkwSprung</c>); seit E3 Schritt 7 oeffnet der Wirt der
    /// Ueberlagerung das Ziel als zweite Ueberlagerung im selben Fenster — das
    /// nachgelagerte Zweitfenster ist ersatzlos weg (siehe
    /// <c>Dokumentation/ueberholt/Protokolle/Reporting/B5b_Blazor_Port_Protokoll.md</c>).</para>
    /// </summary>
    internal static class BhkwWirtschaftlichkeitHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// er in einer <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe
        /// WebView (Risiko R2). <c>Geschlossen</c> setzt der Wirt; den Sprung in
        /// die Tarifsicht wertet er selbst aus (<c>BhkwSprung</c>).
        /// </summary>
        /// <param name="titel">Der Fenster- bzw. Bereichstitel.</param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int idStamm, List<WirtschaftlichkeitErgebnis> ergebnisseAusLauf, out string titel)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            var anlagenCtrl = new KwkgAnlagenCtrl();
            var katalog = new GesetzKatalog();

            WirtschaftlichkeitParameter parameter = ctrl.LadeParameter(idStamm);
            WirtschaftlichkeitCtrl.ErzeugerFlags erzeuger = ctrl.ErzeugerDerGruppe(idStamm);

            var pc = new ProjektCtrl();
            try { pc.ReadSingle(idStamm); } catch { }
            string stammName = pc.rows > 0 ? pc.m_szProjektname : "";

            List<KwkgAnlagenAngabe> anlagen = anlagenCtrl.LadeGruppe(idStamm, stammName);

            // Die laufunabhaengige Doppelpflege-Pruefung — genau der Zweig, der keinen
            // Steuerlauf braucht. Sie ist internal zum Kern und deshalb hier, nicht in
            // der Komponente.
            var doppelpflege = new List<KohaerenzHinweis>();
            try { doppelpflege.AddRange(KohaerenzPruefung.Pruefe(idStamm, null)); }
            catch { }

            titel = Titel(stammName);

            return new Dictionary<string, object>
            {
                ["IdStamm"] = idStamm,
                ["StammName"] = stammName,
                ["Anlagen"] = anlagen,
                ["Parameter"] = parameter,
                ["HatHeizkessel"] = erzeuger != null && erzeuger.Heizkessel,
                ["Doppelpflege"] = doppelpflege,

                // Der Gesetzeskatalog als DELEGAT — dieselbe Uebergabe, die
                // KwkgSatzRechner selbst verlangt (Leitentscheidung L9). Die Komponente
                // bleibt damit datenbankfrei und rechnet trotzdem mit dem einen Katalog.
                ["Katalog"] = new Func<string, int, GesetzParameter>(katalog.WertMitHerkunft),

                ["GrenzeAusschreibungRueckfall"] = WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW,
                ["GrenzeStromsteuerRueckfall"] = 2000.0,

                ["ErgebnisseAusLauf"] = (IReadOnlyList<WirtschaftlichkeitErgebnis>)
                    (ergebnisseAusLauf ?? new List<WirtschaftlichkeitErgebnis>()),
                ["ErgebnisseLaden"] =
                    new Func<IReadOnlyList<int>, IReadOnlyList<WirtschaftlichkeitErgebnis>>(
                        ids => ctrl.LadeErgebnisse(new List<int>(ids))),

                // DIE ZWEI SCHREIBWEGE, je einer fuer eine Zeile und fuer die
                // Projektvorgaben. Der Dialog ruft sie NUR im OK-Weg und weiss
                // dadurch, welcher Schritt durch ist: Scheitert einer, bleibt er
                // offen, und ein zweites OK wiederholt das Geschriebene nicht.
                ["SpeichereAnlage"] = new Func<KwkgAnlagenAngabe, bool>(
                    a => SpeichereAnlage(anlagenCtrl, a)),
                ["SpeichereVorgaben"] = new Func<WirtschaftlichkeitParameter, bool>(
                    p => SpeichereVorgaben(ctrl, p)),

                // ETAPPE E9b (E9a-Q7): Ist das Tarif-Rollenmodell wirksam, bewertet es die
                // Einspeisung mit seinem Einspeisetarif - eine Szenario-Einspeiseverguetung
                // KWK bleibt dann ohne Wirkung. Derselbe Satz wie am Ergebnis.
                ["EinspeisungKwkSzenarioHinweise"] = EinspeisungKwkSzenarioHinweise(ctrl, idStamm)
            };
        }

        /// <summary>
        /// ETAPPE E9b (Konzept § 2.11.5, Pflege; E9a‑Q7) — die Kohärenzzeile des ±-Knopfes der
        /// Einspeisevergütung KWK. Ein Lesefehler kostet die Zeile, nie den Dialog.
        /// </summary>
        private static IReadOnlyList<string> EinspeisungKwkSzenarioHinweise(WirtschaftlichkeitCtrl ctrl,
                                                                           int idStamm)
        {
            var liste = new List<string>();
            try
            {
                if (ctrl.LadeTarif(idStamm).Wirksam)
                {
                    string t = null;
                    try { t = MyResource.Resource.ResourceManager.GetString("WIRT_SZ_ROLLEN_EINSPEISUNG"); }
                    catch { }
                    liste.Add(string.IsNullOrEmpty(t)
                        ? "Szenario-Einspeisevergütung ohne Wirkung: Das Tarif-Rollenmodell bewertet " +
                          "die Einspeisung mit dem Einspeisetarif des Tarifsatzes."
                        : t);
                }
            }
            catch { }
            return liste;
        }

        /// <summary>
        /// Schreibt EINE Anlagenzeile mit allen Spalten des Dialogs (K7; ETAPPE E7c:
        /// dazu Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und Stromkennzahl). Die
        /// Gerätezeile (P_el, P_th) liest der Kern mit, geschrieben wird sie nie.
        /// </summary>
        private static bool SpeichereAnlage(KwkgAnlagenCtrl anlagenCtrl, KwkgAnlagenAngabe anlage)
        {
            try { return anlagenCtrl.Speichere(anlage, true); }
            catch { return false; }
        }

        /// <summary>
        /// Schreibt die Projektvorgaben.
        ///
        /// <para><b>Nur die Felder dieses Dialogs.</b> Alles Uebrige steht unveraendert
        /// im geladenen Parametersatz und geht wertgleich in die Zeile zurueck; das ist
        /// dieselbe Eigenschaft, mit der der Parameterdialog seine ausgeblendeten
        /// Gruppen unveraendert laesst.</para>
        ///
        /// <para>K3 = a: Der Modus des § 9 Abs. 1 Nr. 3 wird NICHT gespeichert — es gibt
        /// dafuer bis B6 (M-3) keine Spalte.</para>
        /// </summary>
        private static bool SpeichereVorgaben(WirtschaftlichkeitCtrl ctrl,
                                              WirtschaftlichkeitParameter parameter)
        {
            try { return ctrl.SpeichereParameter(parameter); }
            catch { return false; }
        }

        /// <summary>Bereichstitel — derselbe Text wie in der Komponente.</summary>
        internal static string Titel(string stammName)
        {
            string t = BhwTexte.T("BHW_TITEL", "BHKW-Wirtschaftlichkeit");
            return string.IsNullOrEmpty(stammName) ? t : t + " — " + stammName;
        }
    }
}
