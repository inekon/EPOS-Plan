using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Hilfe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die plattformfreie HUELLE von „Hilfe → Fernwartung…" (Konzept Administration und Support,
    /// Abschnitt 4.2): fragt den Kern — Freigabe (<see cref="LizenzCtrl.FernwartungErlaubt()"/>,
    /// AS-Q8 Lesart b), Diagnoseblock (<see cref="Diagnosedaten.Textblock"/>), Kontakt
    /// (<c>SUP_KONTAKT_*</c>) und Fernwartungsseite (<see cref="Fernwartung.SEITE"/>) — und reicht
    /// der Komponente nur die Ergebnisse. Browser, Zwischenablage und den Start von Quick Assist
    /// steuert die Plattform als Rueckrufe bei; Windows baut das Fenster in
    /// <c>FernwartungFenster</c> und startet Quick Assist ueber <c>QuickAssistStart</c>. iOS
    /// bietet den Dialog nicht an (Konzept 4.5; <c>AppWurzel</c> fuehrt das Ziel nicht) — reichte
    /// eine Plattform ihn ohne Startrueckruf, fehlte der Knopf und ein Satz nennt Windows.
    /// </summary>
    public static class FernwartungHuelle
    {
        /// <summary>Breite des Fensters in Pixeln.</summary>
        public const int FENSTER_BREITE = 760;

        /// <summary>Hoehe des Fensters in Pixeln.</summary>
        public const int FENSTER_HOEHE = 820;

        /// <summary>Der Fenstertitel.</summary>
        public static string Titel() => MyResource.Resource.FW_TITEL;

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        /// <param name="programmversion">Die Programmversion, wie die Schale sie kennt.</param>
        /// <param name="adresseOeffnen">Oeffnet eine Adresse im Standardprogramm; <c>false</c> = misslungen.</param>
        /// <param name="kopieren">Legt einen Text in die Zwischenablage.</param>
        /// <param name="fernwartungStarten">Startet Quick Assist; <c>false</c> = misslungen;
        /// <c>null</c>, wo die Plattform es nicht kann.</param>
        public static IReadOnlyDictionary<string, object> Gaben(string programmversion,
                                                                Func<string, bool> adresseOeffnen,
                                                                Action<string> kopieren,
                                                                Func<bool> fernwartungStarten = null)
        {
            var gaben = new Dictionary<string, object>
            {
                ["Daten"] = Daten(programmversion),
                ["Texte"] = Texte(),
                ["HilfeSchluessel"] = "Form_Fernwartung.btn_Help",
            };
            if (adresseOeffnen != null) gaben["AdresseOeffnen"] = adresseOeffnen;
            if (kopieren != null) gaben["Kopieren"] = kopieren;
            if (fernwartungStarten != null) gaben["FernwartungStarten"] = fernwartungStarten;
            return gaben;
        }

        /// <summary>Freigabe, Kontakt, Diagnoseblock und Adresse.</summary>
        public static FernwartungDaten Daten(string programmversion)
        {
            Fernwartungsfreigabe freigabe = LizenzCtrl.FernwartungErlaubt();
            return new FernwartungDaten(
                freigabe.Erlaubt,
                freigabe.Erlaubt ? "" : freigabe.Grund ?? "",
                MyResource.Resource.SUP_KONTAKT_TELEFON,
                MyResource.Resource.SUP_KONTAKT_MAIL,
                MyResource.Resource.SUP_KONTAKT_ZEITEN,
                Diagnosedaten.Textblock(programmversion),
                Fernwartung.SEITE);
        }

        /// <summary>Das Textbuendel aus dem Ressourcenkatalog.</summary>
        public static FernwartungTexte Texte() => new FernwartungTexte
        {
            Titel = MyResource.Resource.FW_TITEL,
            ZoneKontakt = MyResource.Resource.FW_ZONE_KONTAKT,
            Telefon = MyResource.Resource.FW_TELEFON,
            Mail = MyResource.Resource.FW_MAIL,
            Zeiten = MyResource.Resource.FW_ZEITEN,
            ZoneAngaben = MyResource.Resource.FW_ZONE_ANGABEN,
            AngabenHinweis = MyResource.Resource.FW_ANGABEN_HINWEIS,
            Kopieren = MyResource.Resource.FW_KOPIEREN,
            Kopiert = MyResource.Resource.FW_KOPIERT,
            ZoneFernwartung = MyResource.Resource.FW_ZONE_FERNWARTUNG,
            Erklaerung = MyResource.Resource.FW_QA_ERKLAERUNG,
            Schritt1 = MyResource.Resource.FW_QA_SCHRITT_1,
            Schritt2 = MyResource.Resource.FW_QA_SCHRITT_2,
            Schritt3 = MyResource.Resource.FW_QA_SCHRITT_3,
            QuickAssistStarten = MyResource.Resource.FW_QA_STARTEN,
            QuickAssistFehler = MyResource.Resource.FW_QA_FEHLER,
            QuickAssistNurWindows = MyResource.Resource.FW_QA_NUR_WINDOWS,
            SeiteOeffnen = MyResource.Resource.FW_SEITE_OEFFNEN,
            SeiteFehler = MyResource.Resource.FW_SEITE_FEHLER,
            Schliessen = MyResource.Resource.FW_SCHLIESSEN,
        };
    }
}
