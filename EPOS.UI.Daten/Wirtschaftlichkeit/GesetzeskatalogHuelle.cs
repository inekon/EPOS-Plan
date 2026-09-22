using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Hülle des Gesetzeskatalogs (iU9-W14c.2).
    ///
    /// <para><b>Seit Etappe E3, Schritt 6 liegt sie in <c>EPOS.UI.Daten</c>.</b>
    /// Ihre Quellen sind Kern-Controller, sie kennt kein Fenster; Windows steuert
    /// nur noch den Adapter <c>Views/Admin/GesetzeskatalogFenster</c> für den
    /// Menüpunkt „Administration → Gesetzeskatalog" bei (Muster
    /// <c>EnergietraegerFenster</c>). Die beiden anderen Aufrufer — der
    /// Kostendialog und der Wirtschaftlichkeits-Parameterdialog — holen den
    /// Parametersatz unmittelbar und zeigen ihn als <c>Ueberlagerung</c>; das
    /// gilt auf jeder Plattform.</para>
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Sie
    /// besteht ausschließlich aus Aufrufen von
    /// <see cref="GesetzKatalog"/> — Klassenliste, Zeilen, Anlegen, Ändern, Löschen
    /// und die Prüfung. Ein neuer Controller war nicht nötig: Der Fachteil dieser
    /// Maske lag schon vollständig im Kern (1 123 Zeilen), die Maske hielt nur
    /// Anzeige, zwei Listen und eine Dublettenprüfung.</para>
    ///
    /// <para><b>Drei Aufrufer, zwei Betriebsarten.</b> Über den Fenster-Adapter
    /// erscheint der Katalog als eigenes Fenster (Menü Administration). Über
    /// <see cref="Gaben"/> liefert die Hülle denselben Parametersatz an einen
    /// Razor-Wirt, der ihn als <c>Ueberlagerung</c> zeigt — den Kostendialog und
    /// den Wirtschaftlichkeits-Parameterdialog. <b>Die zwei
    /// <c>Sprungziel</c>-Zweige entfallen damit</b> (Befund W14c-B13): Beide
    /// Sprungquellen waren schon vorher Razor, und zwei WebViews übereinander sind
    /// Risiko R2 des Wellenplans.</para>
    ///
    /// <para><b><c>StelleKatalogSicher</c> läuft zuerst</b>, wie im Vorläufer: Der
    /// Katalog muss stehen, BEVOR die Oberfläche ihn liest.</para>
    /// </summary>
    internal static class GesetzeskatalogHuelle
    {
        /// <summary>Gewünschte Innenbreite (Vorläufer: 940 × 560, Mindestmaß
        /// 760 × 420). Der Fenster-Adapter holt das Maß hier ab.</summary>
        internal const int FENSTER_BREITE = 940;

        /// <summary>Gewünschte Innenhöhe; siehe <see cref="FENSTER_BREITE"/>.</summary>
        internal const int FENSTER_HOEHE = 560;

        /// <summary>Der Fenstertitel — derselbe Text wie in der Komponente.</summary>
        internal static string Titel()
        {
            return MyResource.Resource.GESETZ_TITEL;
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
                ["Zeilen"] = new Func<string, Task<IReadOnlyList<Katalogfilterzeile>>>(ZeilenLesen),
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

        /// <summary>
        /// Die Zeilen einer Klasse als Zeilen der HAUS-Katalogliste (MN-1,
        /// 19.09.2026). Die Umrechnung steht im Kern
        /// (<c>GesetzKatalog.Katalogfilterzeilen</c>) — hier wird nichts mehr
        /// zusammengesetzt.
        /// </summary>
        private static Task<IReadOnlyList<Katalogfilterzeile>> ZeilenLesen(string klasse)
        {
            return Task.FromResult(new GesetzKatalog().Katalogfilterzeilen(klasse));
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
