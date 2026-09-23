using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Hülle des Dialogs „Tarifstruktur Strom" (iU9-W2.3).
    ///
    /// <para><b>Seit Etappe E3, Schritt 6 liegt sie in <c>EPOS.UI.Daten</c>,
    /// und sie hat keine Fensterhälfte mehr.</b> Der Dialog erscheint
    /// ausschließlich als <c>Ueberlagerung</c> — auf der Wirtschaftlichkeitsseite
    /// und als Ziel der beiden Tarif-Sprünge (E3 Schritt 7). Das zweite
    /// WinForms-Fenster, das die Sprünge bis dahin hochfuhren, war Risiko R2 und
    /// ist ersatzlos weg.</para>
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <see cref="TarifstrukturDialog"/> in <c>EPOS.UI</c>; die WinForms-Fassung
    /// <c>Form_Tarifstruktur</c> ist mit demselben Schritt GELÖSCHT (Regel M1).
    /// Vorbild dieser Klasse ist <see cref="BhkwWirtschaftlichkeitHuelle"/>:
    /// Datenseite hier, Anzeige dort.</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Geladen wird mit
    /// <c>WirtschaftlichkeitCtrl.LadeTarif(idStamm)</c> — demselben Aufruf, den
    /// der Konstruktor der Maske machte —, geschrieben mit
    /// <c>SpeichereTarif</c>. Die Komponente kennt keine Datenbank
    /// (Hausregel <c>EPOS.UI/CLAUDE.md</c>).</para>
    ///
    /// <para><b>Die Sicht bleibt Ä18.</b> Es gilt EIN Tarifsatz je Stamm; die
    /// <see cref="TarifSicht"/> bestimmt nur, welche Blöcke der Dialog baut und
    /// damit auch überschreibt. Die Aufzählung ist mit dem Port nach
    /// <c>EPOS.UI</c> gewandert (sie stand im Quelltext der Maske) und heißt
    /// unverändert.</para>
    ///
    /// <para><b>Q11 (E7b): nur noch das Rollenmodell.</b> Die Sichten
    /// „Strombezug" und „Komplett" sind entfallen, mit ihnen Zonenpreise,
    /// Hochtarif-Fenster, Modellwahl und Staffel; der Dialog speichert jeden Satz im
    /// Rollenmodell. Die Leistungspreis-Staffel pflegt der Stromträger in der
    /// Kostenverwaltung (<c>EnergietraegerHuelle</c>).</para>
    /// </summary>
    internal static class TarifstrukturHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// der Tarifdialog in einer <c>Ueberlagerung</c> darin — dasselbe
        /// Fenster, dieselbe WebView (Risiko R2). <c>Geschlossen</c> setzt der
        /// Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idStamm, TarifSicht sicht)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            TarifParameter tarif = ctrl.LadeTarif(idStamm);

            return new Dictionary<string, object>
            {
                ["Tarif"] = tarif,
                ["Sicht"] = sicht,

                // Der Schreibweg. Die Komponente hat den Bildschirmzustand
                // unmittelbar davor in denselben Satz uebernommen.
                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.SpeichereTarif(tarif); }
                    catch { return false; }
                })
            };
        }

        /// <summary>Bereichstitel — derselbe Text wie in der Komponente.</summary>
        internal static string Titel(TarifSicht sicht)
        {
            return new TarifstrukturTexte().Titel(sicht);
        }
    }
}
