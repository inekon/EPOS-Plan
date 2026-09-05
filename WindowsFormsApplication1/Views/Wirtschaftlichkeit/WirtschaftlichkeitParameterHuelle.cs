using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Dialogs „Wirtschaftlichkeits-Parameter" (iU9-W2.5).
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <see cref="WirtschaftlichkeitParameterDialog"/> in <c>EPOS.UI</c>; die
    /// WinForms-Fassung <c>Form_WirtschaftlichkeitParameter</c> ist mit
    /// demselben Schritt GELÖSCHT (Regel M1).</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Geladen wird mit denselben
    /// Aufrufen wie im Konstruktor der Maske —
    /// <c>WirtschaftlichkeitCtrl.LadeParameter</c> und
    /// <c>ErzeugerDerGruppe</c> —, dazu die drei Größen, die die Maske selbst
    /// nachschlug: die Kraftwerksparkliste
    /// (<c>EmissionsBilanzRechner.LadeKatalog</c>), die Referenzkesselzeile
    /// (<c>LiesReferenzkessel</c>) und das Prognosejahr des CO₂-Pfads
    /// (<c>GesetzKatalog.AlleDerKlasse</c>). Geschrieben wird über
    /// <c>SpeichereParameter</c>.</para>
    ///
    /// <para><b>Zwei Wege, keiner mehr über die Brücke.</b> Der Gesetzeskatalog ist
    /// seit iU9-W14c.2 selbst eine Razor-Komponente und erscheint als
    /// <c>Ueberlagerung</c> IM Dialog — mit der Vorwahl CO₂-Preis, die bis dahin
    /// <c>Sprungziel.GesetzesparameterCo2</c> setzte. Der
    /// Sammeldialog „BHKW-Wirtschaftlichkeit" ist selbst eine Blazor-Hülle und
    /// bleibt nachgelagert (Risiko R2): Die Komponente meldet den Wunsch im
    /// Ergebnis, diese Hülle schließt den Dialog, zeigt das Ziel und lädt danach
    /// neu — der Sammeldialog schreibt denselben Parametersatz.</para>
    /// </summary>
    internal static class WirtschaftlichkeitParameterHuelle
    {
        /// <summary>Gewünschtes Innenmaß. Die Maske maß 445 px breit.</summary>
        private const int FENSTER_BREITE = 720;

        /// <summary>Rückfalljahr der CO₂-Prognose — das Jahr der Entscheidung E5,
        /// falls der Katalog (noch) keine Prognosezeile führt.</summary>
        private const int CO2_PROGNOSE_RUECKFALL = 2028;

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn gespeichert wurde — dann
        /// rechnet die Wirtschaftlichkeitsseite neu.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm)
        {
            bool gespeichert = false;

            // Der Sprung in den BHKW-Dialog schliesst dieses Fenster und bringt
            // es danach zurueck; deshalb eine Schleife statt eines Aufrufs.
            while (true)
            {
                WirtParameterErgebnis ergebnis = EinmalZeigen(besitzer, idStamm);
                if (ergebnis == null) return gespeichert;
                if (ergebnis.Gespeichert) gespeichert = true;
                if (ergebnis.Sprung == WirtParameterSprung.Keiner) return gespeichert;

                // Der Sammeldialog schreibt selbst; sein Ergebnis zaehlt wie ein
                // eigenes Speichern (die Wirtschaftlichkeit muss dann neu rechnen).
                if (BhkwWirtschaftlichkeitHuelle.Oeffnen(besitzer, idStamm, null))
                    gespeichert = true;
            }
        }

        /// <summary>Ein Durchgang: laden, zeigen, Ergebnis melden.</summary>
        private static WirtParameterErgebnis EinmalZeigen(IWin32Window besitzer, int idStamm)
        {
            WirtParameterErgebnis ergebnis = null;
            BlazorDialogForm<WirtschaftlichkeitParameterDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(idStamm))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<WirtParameterErgebnis>(
                    new object(), e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null && e.Gespeichert);
                    })
            };

            int hoehe = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 90);
            dlg = new BlazorDialogForm<WirtschaftlichkeitParameterDialog>(
                new WirtschaftlichkeitParameterTexte().Titel,
                new Size(FENSTER_BREITE, hoehe), werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// er in einer <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe
        /// WebView (Risiko R2). <c>Geschlossen</c> setzt der Wirt; den Sprung
        /// in die BHKW-Sicht wertet er selbst aus (<c>WirtParameterSprung</c>).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idStamm)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter parameter = ctrl.LadeParameter(idStamm);
            WirtschaftlichkeitCtrl.ErzeugerFlags erzeuger = ctrl.ErzeugerDerGruppe(idStamm);

            bool bhkw = erzeuger != null && erzeuger.Bhkw;
            bool brennstoff = erzeuger != null && erzeuger.Brennstoff;

            var parks = new List<(int Id, string Text)>();
            string refKessel = "";
            if (brennstoff)
            {
                parks.Add((0, Text("WPAR_PARK_KEINER", "(keine Emissionsbilanz)")));
                try
                {
                    foreach (Kraftwerkspark park in EmissionsBilanzRechner.LadeKatalog())
                        parks.Add((park.Id, park.Bezeichner));
                }
                catch { }
                refKessel = Referenzkesselzeile(ctrl, parameter);
            }

            return new Dictionary<string, object>
            {
                ["Parameter"] = parameter,
                ["HatBhkw"] = bhkw,
                ["HatBrennstoff"] = brennstoff,
                ["Kraftwerksparks"] = (IReadOnlyList<(int Id, string Text)>)parks,
                ["ReferenzkesselZeile"] = refKessel,
                ["Co2PrognoseAb"] = Co2PrognoseAb(),

                // iU9-W14c.3: Der Gesetzeskatalog laeuft nicht mehr ueber die
                // Sprungbruecke, sondern als Ueberlagerung IM Dialog - mit der
                // Vorwahl der Klasse CO2_PREIS, die Sprungbruecke.cs:100 setzte.
                // Damit ist dies der ERSTE Dialog des Bestands, der ohne Sprungziel
                // auskommt, seit er eines hatte (Ersteinsatz war iU9-W2.2).
                ["GesetzeGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => GesetzeskatalogHuelle.Gaben(DbWerte.GESETZ_KLASSE_CO2_PREIS)),

                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.SpeichereParameter(parameter); }
                    catch { return false; }
                })
            };
        }

        /// <summary>
        /// Die Anzeigezeile des Referenzkessels. Er kommt seit Phase 11 aus der
        /// Datenbank (größter Heizkessel des Stammprojekts) und wird hier nicht
        /// mehr gepflegt — nur noch gezeigt.
        /// </summary>
        private static string Referenzkesselzeile(WirtschaftlichkeitCtrl ctrl,
                                                  WirtschaftlichkeitParameter parameter)
        {
            try
            {
                ReferenzkesselInfo rk = ctrl.LiesReferenzkessel(parameter.IdStamm);
                if (rk == null || !rk.Gefunden)
                    return string.Format(
                        Text("WPAR_REFKESSEL_FEHLT",
                             "Referenzkessel: kein Heizkessel im Stammprojekt gepflegt — " +
                             "Vorgabe η {0} % gilt."),
                        parameter.RefKesselWirkungsgrad.ToString("N0"));

                string brennstoff = rk.BrennstoffName.Length > 0
                    ? ", " + rk.BrennstoffName
                    : Text("WPAR_REFKESSEL_VORGABE", ", Brennstoff aus Vorgabe");

                return string.Format(
                    Text("WPAR_REFKESSEL", "Referenzkessel (aus Projekt): {0} — η {1} %{2}"),
                    rk.Bezeichner, rk.WirkungsgradProzent.ToString("N0"), brennstoff);
            }
            catch { return ""; }
        }

        /// <summary>
        /// Das erste Kalenderjahr des CO₂-Pfads mit dem Status PROGNOSE — die
        /// Zahl, die die Zeile „Prognose ab …" nennt.
        /// </summary>
        private static int Co2PrognoseAb()
        {
            try
            {
                var katalog = new GesetzKatalog();
                foreach (GesetzParameter p in katalog.AlleDerKlasse(DbWerte.GESETZ_KLASSE_CO2_PREIS))
                    if (string.Equals(p.Schluessel, DbWerte.GESETZ_CO2_PREIS_NEHS, StringComparison.Ordinal) &&
                        string.Equals(p.Status, DbWerte.GESETZ_STATUS_PROGNOSE, StringComparison.Ordinal))
                        return p.JahrVon;      // AlleDerKlasse liefert nach Jahr sortiert
            }
            catch { }
            return CO2_PROGNOSE_RUECKFALL;
        }

        /// <summary>Anzeigetext mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
