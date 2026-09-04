using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Gesetzeskatalogs (iU9-W14c.2).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Sie
    /// besteht ausschließlich aus Aufrufen von
    /// <see cref="GesetzKatalog"/> — Klassenliste, Zeilen, Anlegen, Ändern, Löschen
    /// und die Prüfung. Ein neuer Controller war nicht nötig: Der Fachteil dieser
    /// Maske lag schon vollständig im Kern (1 123 Zeilen), die Maske hielt nur
    /// Anzeige, zwei Listen und eine Dublettenprüfung.</para>
    ///
    /// <para><b>Drei Aufrufer, zwei Betriebsarten.</b> Über
    /// <see cref="Oeffnen"/> erscheint der Katalog als eigenes Fenster (Menü
    /// Administration). Über <see cref="Gaben"/> liefert die Hülle denselben
    /// Parametersatz an einen Razor-Wirt, der ihn als <c>Ueberlagerung</c> zeigt —
    /// den Kostendialog und den Wirtschaftlichkeits-Parameterdialog. <b>Die zwei
    /// <c>Sprungziel</c>-Zweige entfallen damit</b> (Befund W14c-B13): Beide
    /// Sprungquellen waren schon vorher Razor, und zwei WebViews übereinander sind
    /// Risiko R2 des Wellenplans.</para>
    ///
    /// <para><b><c>StelleKatalogSicher</c> läuft zuerst</b>, wie im Vorläufer: Der
    /// Katalog muss stehen, BEVOR die Oberfläche ihn liest.</para>
    /// </summary>
    internal static class GesetzeskatalogHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 940 × 560, Mindestmaß 760 × 420).</summary>
        private static readonly Size MASS = new Size(940, 560);

        /// <summary>
        /// Zeigt den Katalog als eigenes Fenster — der Weg von
        /// <c>MDIMainForm.InitGesetzeMenue</c>.
        /// </summary>
        /// <param name="besitzer">Das Fenster, über dem der Dialog erscheint.</param>
        /// <param name="vorwahlKlasse">Vorgewählter Bereich; leer = der erste.</param>
        internal static bool Oeffnen(IWin32Window besitzer, string vorwahlKlasse = "")
        {
            bool ok = false;
            BlazorDialogForm<GesetzeskatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(vorwahlKlasse))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GesetzeskatalogDialog>(
                MyResource.Resource.GESETZ_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — für die Überlagerung in
        /// <c>KostenKomponenteDialog</c> (über <c>ErtragBonus</c>) und in
        /// <c>WirtschaftlichkeitParameterDialog</c>.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(string vorwahlKlasse = "")
        {
            // Der Katalog muss stehen, BEVOR die Oberflaeche ihn liest (Vorlaeufer :55).
            GesetzKatalog.StelleKatalogSicher();

            return new Dictionary<string, object>
            {
                ["Klassen"] = new Func<Task<IReadOnlyList<(string, string)>>>(KlassenLesen),
                ["Zeilen"] = new Func<string, Task<IReadOnlyList<GesetzeskatalogDialog.Zeile>>>(ZeilenLesen),
                ["Anlegen"] = new Func<GesetzeskatalogZeileDialog.Zeilenwerte, Task<bool>>(Anlegen),
                ["Aendern"] = new Func<GesetzeskatalogZeileDialog.Zeilenwerte, Task<bool>>(Aendern),
                ["Loeschen"] = new Func<int, Task<bool>>(
                    id => Task.FromResult(GesetzKatalog.Loeschen(id))),
                ["Pruefen"] = new Func<GesetzeskatalogZeileDialog.Zeilenwerte, int, Task<string>>(Pruefen),
                ["Klassenvorrat"] = Klassenvorrat(),
                ["Einheiten"] = GesetzKatalog.Einheiten(),
                ["Statuswerte"] = GesetzKatalog.Statuswerte(),
                ["Vorwahl"] = vorwahlKlasse ?? ""
            };
        }

        /// <summary>
        /// Die Klassen, die im Katalog VORKOMMEN — samt Anzeigename. Jeder Aufruf legt
        /// eine frische Fassade an; damit sieht die Liste eine gerade angelegte Klasse
        /// (<c>KlassenErgaenzen</c> des Vorläufers).
        /// </summary>
        private static Task<IReadOnlyList<(string, string)>> KlassenLesen()
        {
            IReadOnlyList<(string, string)> liste = new GesetzKatalog().Klassen()
                .Select(k => (k, GesetzKatalog.KlasseAnzeige(k)))
                .ToList();
            return Task.FromResult(liste);
        }

        /// <summary>Der Klassenvorrat des Zeilendialogs — die acht wählbaren Klassen.</summary>
        private static IReadOnlyList<(string, string)> Klassenvorrat()
        {
            return GesetzKatalog.KlassenVorrat()
                .Select(k => (k, GesetzKatalog.KlasseAnzeige(k)))
                .ToList();
        }

        private static Task<IReadOnlyList<GesetzeskatalogDialog.Zeile>> ZeilenLesen(string klasse)
        {
            IReadOnlyList<GesetzeskatalogDialog.Zeile> liste = new GesetzKatalog().Zeilen(klasse)
                .Select(z => new GesetzeskatalogDialog.Zeile(
                    z.Id, z.Schluessel, z.Klasse, z.JahrVon, z.WertText, z.Einheit, z.Status, z.Quelle))
                .ToList();
            return Task.FromResult(liste);
        }

        private static Task<bool> Anlegen(GesetzeskatalogZeileDialog.Zeilenwerte w)
        {
            return Task.FromResult(GesetzKatalog.Anlegen(
                w.Schluessel, w.Klasse, w.JahrVon, w.Wert, w.Einheit, w.Status, w.Quelle) != 0);
        }

        /// <summary>
        /// Ändern nimmt Jahr, Wert, Einheit, Status und Quelle — <b>nicht Schlüssel und
        /// Klasse</b>: Sie sind die Identität der Reihe und im Zeilendialog gesperrt.
        /// </summary>
        private static Task<bool> Aendern(GesetzeskatalogZeileDialog.Zeilenwerte w)
        {
            return Task.FromResult(GesetzKatalog.Aendern(
                w.Id, w.JahrVon, w.Wert, w.Einheit, w.Status, w.Quelle));
        }

        /// <summary>
        /// Die Prüfung des Kerns, in einen Meldungstext übersetzt: leer = in Ordnung
        /// (W14c.0b — sie läuft genau einmal, Befund W14c-B7).
        /// </summary>
        private static Task<string> Pruefen(GesetzeskatalogZeileDialog.Zeilenwerte w, int eigeneId)
        {
            GesetzPruefBefund befund = GesetzKatalog.Pruefe(
                new GesetzParameter(w.Id, w.Schluessel, w.Klasse, w.JahrVon, w.Wert,
                                    w.Einheit, w.Status, w.Quelle),
                eigeneId);
            return Task.FromResult(befund.Ok ? "" : befund.Meldung);
        }
    }
}
