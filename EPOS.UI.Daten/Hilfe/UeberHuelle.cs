using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Hilfe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die plattformfreie HUELLE von „Hilfe → Version" (Konzept Administration und Support,
    /// Abschnitt 5): baut Kopf, Diagnoseblock (<see cref="Diagnosedaten.Textblock"/>) und Kontakt
    /// (<c>SUP_KONTAKT_*</c>) aus Kern und Ressourcen und reicht der Komponente nur die Ergebnisse.
    /// Produktname, Programmversion und Baudatum kennt allein die Schale und gibt sie herein;
    /// Zwischenablage, Ordner, Browser, Fernwartung und das Schalten des Entwicklermodus steuert
    /// sie als Rueckrufe bei. Windows baut das Fenster in <c>UeberFenster</c>.
    /// </summary>
    public static class UeberHuelle
    {
        /// <summary>Breite des Fensters in Pixeln.</summary>
        public const int FENSTER_BREITE = 760;

        /// <summary>Hoehe des Fensters in Pixeln.</summary>
        public const int FENSTER_HOEHE = 820;

        /// <summary>Der Fenstertitel, etwa „Über EPOS-Plan".</summary>
        public static string Titel(string produktname) =>
            string.Format(MyResource.Resource.HAUPT_UEBER_TITEL, produktname ?? "");

        /// <summary>
        /// Die Versionszeile: „Version 1.2.0.3 vom 11.10.2026"; ohne Baudatum wie die Kopfleiste
        /// („Version 1.2.0.3").
        /// </summary>
        public static string Versionszeile(string version, string baudatum) =>
            string.IsNullOrWhiteSpace(baudatum)
                ? string.Format(MyResource.Resource.HAUPT_VERSION, version ?? "")
                : string.Format(MyResource.Resource.UEB_VERSION_BAUDATUM, version ?? "", baudatum);

        /// <summary>Der Diagnoseblock des Kerns — auch der Rueckgabewert nach der Geste.</summary>
        public static string Diagnosetext(string programmversion) => Diagnosedaten.Textblock(programmversion);

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        /// <param name="produktname">Der Produktname der Schale.</param>
        /// <param name="version">Die Version der Kopfzeile (wie die Kopfleiste des Hauptfensters).</param>
        /// <param name="baudatum">Das Baudatum, fertig formatiert; leer = unbekannt.</param>
        /// <param name="programmversion">Die Programmversion des Diagnoseblocks.</param>
        /// <param name="adresseOeffnen">Oeffnet eine Adresse im Standardprogramm; <c>false</c> = misslungen.</param>
        /// <param name="kopieren">Legt einen Text in die Zwischenablage.</param>
        /// <param name="protokollordnerOeffnen">Oeffnet den Protokollordner; <c>null</c> = kein Knopf.</param>
        /// <param name="fernwartungOeffnen">Oeffnet den Fernwartungsdialog; <c>null</c> = kein Knopf.</param>
        /// <param name="entwicklermodusSchalten">Schaltet den Modus und liefert den neuen Block; <c>null</c> = keine Geste.</param>
        /// <param name="anstoss">Der Anstoss der Geste; <c>null</c> = keine Geste.</param>
        public static IReadOnlyDictionary<string, object> Gaben(string produktname, string version, string baudatum,
                                                                string programmversion,
                                                                Func<string, bool> adresseOeffnen,
                                                                Action<string> kopieren,
                                                                Action protokollordnerOeffnen = null,
                                                                Action fernwartungOeffnen = null,
                                                                Func<string> entwicklermodusSchalten = null,
                                                                UeberAnstoss anstoss = null)
        {
            var gaben = new Dictionary<string, object>
            {
                ["Daten"] = Daten(produktname, version, baudatum, programmversion),
                ["Texte"] = Texte(produktname),
            };
            if (adresseOeffnen != null) gaben["AdresseOeffnen"] = adresseOeffnen;
            if (kopieren != null) gaben["Kopieren"] = kopieren;
            if (protokollordnerOeffnen != null) gaben["ProtokollordnerOeffnen"] = protokollordnerOeffnen;
            if (fernwartungOeffnen != null) gaben["FernwartungOeffnen"] = fernwartungOeffnen;
            if (entwicklermodusSchalten != null) gaben["EntwicklermodusSchalten"] = entwicklermodusSchalten;
            if (anstoss != null) gaben["Anstoss"] = anstoss;
            return gaben;
        }

        /// <summary>Kopf, Diagnoseblock und Kontakt.</summary>
        public static UeberDaten Daten(string produktname, string version, string baudatum, string programmversion) =>
            new UeberDaten(
                produktname ?? "",
                MyResource.Resource.HAUPT_CLAIM,
                Versionszeile(version, baudatum),
                MyResource.Resource.HAUPT_UEBER_HAUS,
                Diagnosetext(programmversion),
                MyResource.Resource.SUP_KONTAKT_TELEFON,
                MyResource.Resource.SUP_KONTAKT_MAIL,
                MyResource.Resource.SUP_KONTAKT_ZEITEN);

        /// <summary>Das Textbuendel aus dem Ressourcenkatalog.</summary>
        public static UeberTexte Texte(string produktname) => new UeberTexte
        {
            Titel = Titel(produktname),
            ZoneDiagnose = MyResource.Resource.UEB_ZONE_DIAGNOSE,
            DiagnoseHinweis = MyResource.Resource.UEB_DIAGNOSE_HINWEIS,
            Kopieren = MyResource.Resource.UEB_KOPIEREN,
            Kopiert = MyResource.Resource.UEB_KOPIERT,
            Protokollordner = MyResource.Resource.UEB_PROTOKOLLORDNER,
            ZoneKontakt = MyResource.Resource.UEB_ZONE_KONTAKT,
            Telefon = MyResource.Resource.UEB_TELEFON,
            Mail = MyResource.Resource.UEB_MAIL,
            Zeiten = MyResource.Resource.UEB_ZEITEN,
            Fernwartung = MyResource.Resource.UEB_FERNWARTUNG,
            MailFehler = MyResource.Resource.UEB_MAIL_FEHLER,
            ModusGewechselt = MyResource.Resource.UEB_MODUS_GEWECHSELT,
            Schliessen = MyResource.Resource.UEB_SCHLIESSEN,
        };
    }
}
