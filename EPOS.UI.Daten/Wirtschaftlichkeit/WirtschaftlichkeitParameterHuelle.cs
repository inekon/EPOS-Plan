using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE des Dialogs „Wirtschaftlichkeits-Parameter" (iU9-W2.5), seit
    /// Etappe E3 Schritt 1 plattformfrei in <c>EPOS.UI.Daten</c>: Sie führt keine
    /// einzige WinForms-Anweisung, ihre Quellen sind Kern-Controller, und sie
    /// kennt kein Fenster. Was die Plattform beisteuert, kommt über
    /// <see cref="Wirtschaftlichkeitswege"/> herein.
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <c>WirtschaftlichkeitParameterDialog</c> in <c>EPOS.UI</c>; die
    /// WinForms-Fassung <c>Form_WirtschaftlichkeitParameter</c> ist mit
    /// demselben Schritt GELÖSCHT (Regel M1).</para>
    ///
    /// <para><b>Hier liegt nur noch die Datenseite.</b> Der Dialog erscheint als
    /// <c>Ueberlagerung</c> IN der Wirtschaftlichkeitsseite — dasselbe Fenster,
    /// dieselbe WebView (Risiko R2) —, deshalb baut diese Hülle allein den
    /// Parametersatz und zeigt kein eigenes Fenster. Geladen wird mit denselben
    /// Aufrufen wie im Konstruktor der Maske —
    /// <c>WirtschaftlichkeitCtrl.LadeParameter</c> und
    /// <c>ErzeugerDerGruppe</c> —, dazu die drei Größen, die die Maske selbst
    /// nachschlug: die Kraftwerksparkliste
    /// (<c>EmissionsBilanzRechner.LadeKatalog</c>), die Referenzkesselzeile
    /// (<c>LiesReferenzkessel</c>) und das Prognosejahr des CO₂-Pfads
    /// (<c>GesetzKatalog.AlleDerKlasse</c>). Geschrieben wird über
    /// <c>SpeichereParameter</c>.</para>
    ///
    /// <para><b>Ein einziger Unterdialog.</b> Der Gesetzeskatalog ist selbst eine
    /// Razor-Komponente und erscheint als <c>Ueberlagerung</c> IM Dialog — mit
    /// der Vorwahl CO₂-Preis, die bis dahin <c>Sprungziel.GesetzesparameterCo2</c>
    /// setzte. Ein Folgefenster meldet der Dialog nicht; in den Sammeldialog
    /// „BHKW-Wirtschaftlichkeit" führt der eigene Knopf der Fußleiste.</para>
    /// </summary>
    internal static class WirtschaftlichkeitParameterHuelle
    {
        /// <summary>Rückfalljahr der CO₂-Prognose — das Jahr der Entscheidung E5,
        /// falls der Katalog (noch) keine Prognosezeile führt.</summary>
        private const int CO2_PROGNOSE_RUECKFALL = 2028;

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). <c>Geschlossen</c> setzt der
        /// Wirt — die Wirtschaftlichkeitsseite, in deren <c>Ueberlagerung</c> der
        /// Dialog steht.
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

            var werte = new Dictionary<string, object>
            {
                ["Parameter"] = parameter,
                ["HatBhkw"] = bhkw,
                ["HatBrennstoff"] = brennstoff,
                ["Kraftwerksparks"] = (IReadOnlyList<(int Id, string Text)>)parks,
                ["ReferenzkesselZeile"] = refKessel,
                ["Co2PrognoseAb"] = Co2PrognoseAb(),

                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.SpeichereParameter(parameter); }
                    catch { return false; }
                })
            };

            // iU9-W14c.3: Der Gesetzeskatalog laeuft nicht mehr ueber die
            // Sprungbruecke, sondern als Ueberlagerung IM Dialog - mit der
            // Vorwahl der Klasse CO2_PREIS, die Sprungbruecke.cs:100 setzte.
            // Damit ist dies der ERSTE Dialog des Bestands, der ohne Sprungziel
            // auskommt, seit er eines hatte (Ersteinsatz war iU9-W2.2).
            //
            // E3/1: Die gerufene Huelle zeigt noch ein eigenes Fenster und liegt
            // deshalb bis E3 Schritt 6 in der Windows-Schale; der Weg kommt als
            // benannte Naht herein. Kein Delegat, kein Knopf - ohne eingehaengte
            // Naht bleibt der Schluessel weg, und der Dialog zeigt den Katalog
            // gar nicht erst an (@if (GesetzeGaben is not null)).
            Func<string, IReadOnlyDictionary<string, object>> gesetze =
                Wirtschaftlichkeitswege.GesetzeskatalogGaben;
            if (gesetze != null)
                werte["GesetzeGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => gesetze(DbWerte.GESETZ_KLASSE_CO2_PREIS));

            return werte;
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
