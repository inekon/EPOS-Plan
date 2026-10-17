using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Import;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Nordrichtung</b> (Abstimmungspapier G5, Abschnitt 7): baut aus dem Kern die DTO für die Vorgabe im
    /// Zuordnungsdialog (<see cref="GebaeudeImportHuelle.NordrichtungDaten"/>) und den Abschnitt „Ausrichtung“ des
    /// Gebäudedialogs (<see cref="Lesen"/>, <see cref="Aendern"/>). Eingabe ist die Richtung der Planoberseite α in Grad
    /// (0° = Nord, im Uhrzeigersinn); der Nordwinkel ist (360° − α) mod 360° (<see cref="Nordrichtung"/>).
    /// </summary>
    internal static class GebaeudeAusrichtungHuelle
    {
        /// <summary>Die acht Knöpfe der Schnellwahl in der Anzeigesprache.</summary>
        internal static IReadOnlyList<GebaeudeNordSchnellwahl> Schnellwahl()
            => Nordrichtung.Schnellwahl
                .Select(s => new GebaeudeNordSchnellwahl(s.Kuerzel, Text_("GEB_AUSRICHTUNG_" + s.Kuerzel, s.Kuerzel), s.PlanoberseiteGrad))
                .ToList();

        /// <summary>Der sprachneutrale Schlüssel einer Herkunft (<c>DATEI</c>, <c>EINGABE</c>, <c>ANNAHME</c>).</summary>
        internal static string HerkunftSchluessel(Nordwinkelherkunft h) => GebaeudeImportCtrl.HerkunftWert(h);

        /// <summary>Die Herkunft als Anzeigetext.</summary>
        internal static string HerkunftText(Nordwinkelherkunft h) => h switch
        {
            Nordwinkelherkunft.Datei => Text_("GEB_AUSRICHTUNG_HERKUNFT_DATEI", "aus der Datei"),
            Nordwinkelherkunft.Eingabe => Text_("GEB_AUSRICHTUNG_HERKUNFT_EINGABE", "eingegeben"),
            _ => Text_("GEB_AUSRICHTUNG_HERKUNFT_ANNAHME", "angenommen (Planoberseite = Nord)"),
        };

        /// <summary>
        /// Die Nordrichtung eines gelesenen Abbilds (N1–N3); <c>null</c> = nichts gelesen. Der Dateiwert ist der Nordwinkel,
        /// den die Datei nennt (IFC: <c>TrueNorth</c>/<c>IfcMapConversion</c>, gbXML: <c>CADModelAzimuth</c>), umgerechnet in
        /// die Richtung der Planoberseite.
        /// </summary>
        internal static GebaeudeNordrichtungDaten Daten(GebaeudeAbbild abbild)
        {
            if (abbild == null) return null;
            double? datei = Nordrichtung.PlanoberseiteAusNordwinkel(abbild.NordwinkelGrad);
            double nordwinkel = Nordrichtung.Normiert(abbild.NordwinkelWirksamGrad) ?? 0.0;
            string dateiText = datei is double d
                ? string.Format(CultureInfo.CurrentCulture, Text_("GEB_AUSRICHTUNG_DATEI_NENNT", "Die Datei nennt die Nordrichtung: Planoberseite nach {0}°."), Grad(d))
                : Text_("GEB_AUSRICHTUNG_DATEI_OHNE", "Die Datei nennt keine Nordrichtung.");
            return new GebaeudeNordrichtungDaten(
                datei, dateiText,
                Nordrichtung.PlanoberseiteAusNordwinkel(nordwinkel) ?? 0.0, nordwinkel,
                HerkunftSchluessel(abbild.NordwinkelHerkunft), HerkunftText(abbild.NordwinkelHerkunft),
                Text_("GEB_AUSRICHTUNG_PLANOBERSEITE", "Planoberseite zeigt nach"), Schnellwahl());
        }

        /// <summary>
        /// <b>Die gespeicherte Ausrichtung eines Gebäudes</b> für den Abschnitt „Ausrichtung“ des Gebäudedialogs (N5/N6).
        /// Ohne Importquelle ist sie nicht änderbar, der Hinweis nennt den Grund.
        /// </summary>
        internal static GebaeudeAusrichtungDaten Lesen(int idGebaeude)
        {
            GebaeudeImportCtrl.Ausrichtung a = idGebaeude > 0 ? new GebaeudeImportCtrl().LesenAusrichtung(idGebaeude) : null;
            string titel = Text_("GEB_AUSRICHTUNG_TITEL", "Ausrichtung");
            string beschriftung = Text_("GEB_AUSRICHTUNG_PLANOBERSEITE", "Planoberseite zeigt nach");
            if (a == null)
                return new GebaeudeAusrichtungDaten(false, Text_("GEB_AUSRICHTUNG_KEINE_QUELLE",
                    "Das Gebäude stammt aus keiner Importdatei — seine Ausrichtung lässt sich nur an den einzelnen Bauteilen ändern."),
                    0.0, null, HerkunftSchluessel(Nordwinkelherkunft.Annahme), HerkunftText(Nordwinkelherkunft.Annahme),
                    titel, beschriftung, Schnellwahl());
            return new GebaeudeAusrichtungDaten(true, "", a.PlanoberseiteGrad, a.NordwinkelGrad, HerkunftSchluessel(a.Herkunft),
                                                HerkunftText(a.Herkunft), titel, beschriftung, Schnellwahl(),
                                                new GebaeudeImportCtrl().DrehbareBauteile(idGebaeude));
        }

        /// <summary>
        /// <b>Ausrichtung ändern</b> (N5): dreht alle Bauteile des Gebäudes auf die neue Richtung der Planoberseite und speichert
        /// sie an der Quelle — in einem Vorgang des Kerns. Liefert die Zahl der gedrehten Bauteile.
        /// </summary>
        internal static GebaeudeAusrichtungErgebnis Aendern(int idGebaeude, double planoberseiteGrad)
        {
            GebaeudeImportCtrl.Ausrichtungsergebnis e = new GebaeudeImportCtrl().AusrichtungAendern(idGebaeude, planoberseiteGrad);
            double planoberseite = Nordrichtung.PlanoberseiteAusNordwinkel(e.NordwinkelGrad) ?? 0.0;
            return new GebaeudeAusrichtungErgebnis(e.Ok, e.Meldung ?? "", e.GedrehteBauteile, planoberseite);
        }

        private static string Grad(double w) => w.ToString("0.#", CultureInfo.CurrentCulture);

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
