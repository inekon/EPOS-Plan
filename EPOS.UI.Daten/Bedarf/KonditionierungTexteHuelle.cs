using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FÜLLUNG des Textbündels der Konditionierung (<see cref="KonditionierungTexte"/>) aus
    /// <c>MyResource</c> — Teilkonzept Konditionierungsprofile 7.7, Stufe KP2.
    ///
    /// <para><b>Rückfall.</b> Jede Eigenschaft fällt auf ihren deutschen Vorgabewert im Bündel
    /// zurück, wenn ihr Schlüssel fehlt oder leer ist — dasselbe Muster wie
    /// <c>GebaeudeKatalogHuelle.Texte()</c>. <c>HuellenTextschluesselWacheTests</c> verlangt jeden
    /// Literalschlüssel dieser Datei in beiden Ressourcendateien, damit der Rückfall in Englisch nie
    /// still einspringt.</para>
    /// </summary>
    internal static class KonditionierungTexteHuelle
    {
        /// <summary>Das Textbündel der Konditionierung — der Rückfall ist der Vorgabewert des Bündels.</summary>
        internal static KonditionierungTexte Texte()
        {
            var t = new KonditionierungTexte();

            // Reiter, Matrix und die fünf Größen
            t.Reiter = Text_("KOND_LBL_REITER", t.Reiter);
            t.Matrix = Text_("KOND_LBL_MATRIX", t.Matrix);
            t.GroesseHeizen = Text_("KOND_LBL_GROESSE_HEIZEN", t.GroesseHeizen);
            t.GroesseKuehlen = Text_("KOND_LBL_GROESSE_KUEHLEN", t.GroesseKuehlen);
            t.GroesseLueftung = Text_("KOND_LBL_GROESSE_LUEFTUNG", t.GroesseLueftung);
            t.GroesseGeraete = Text_("KOND_LBL_GROESSE_GERAETE", t.GroesseGeraete);
            t.GroessePersonen = Text_("KOND_LBL_GROESSE_PERSONEN", t.GroessePersonen);
            t.SpalteHeizen = Text_("KOND_LBL_SPALTE_HEIZEN", t.SpalteHeizen);
            t.SpalteKuehlen = Text_("KOND_LBL_SPALTE_KUEHLEN", t.SpalteKuehlen);
            t.SpalteLueftung = Text_("KOND_LBL_SPALTE_LUEFTUNG", t.SpalteLueftung);
            t.SpalteGeraete = Text_("KOND_LBL_SPALTE_GERAETE", t.SpalteGeraete);
            t.SpaltePersonen = Text_("KOND_LBL_SPALTE_PERSONEN", t.SpaltePersonen);

            // Zeilen der Matrix
            t.ZeileVorlage = Text_("KOND_LBL_ZEILE_VORLAGE", t.ZeileVorlage);
            t.ZeileNennwert = Text_("KOND_LBL_ZEILE_NENNWERT", t.ZeileNennwert);
            t.ZeileTag = Text_("KOND_LBL_ZEILE_TAG", t.ZeileTag);
            t.ZeileNacht = Text_("KOND_LBL_ZEILE_NACHT", t.ZeileNacht);
            t.ZeileWochenende = Text_("KOND_LBL_ZEILE_WOCHENENDE", t.ZeileWochenende);
            t.ZeileFerien = Text_("KOND_LBL_ZEILE_FERIEN", t.ZeileFerien);
            t.ZeileSaison = Text_("KOND_LBL_ZEILE_SAISON", t.ZeileSaison);
            t.FeldHeizenWochenende = Text_("KOND_LBL_FELD_HEIZEN_WOCHENENDE", t.FeldHeizenWochenende);
            t.FeldHeizenFerien = Text_("KOND_LBL_FELD_HEIZEN_FERIEN", t.FeldHeizenFerien);
            t.LabelNachtfenster = Text_("KOND_LBL_NACHTFENSTER", t.LabelNachtfenster);
            t.LabelNachtfensterVon = Text_("KOND_LBL_NACHTFENSTER_VON", t.LabelNachtfensterVon);
            t.LabelNachtfensterBis = Text_("KOND_LBL_NACHTFENSTER_BIS", t.LabelNachtfensterBis);
            t.LabelSaisonStart = Text_("KOND_LBL_SAISON_START", t.LabelSaisonStart);
            t.LabelSaisonEnde = Text_("KOND_LBL_SAISON_ENDE", t.LabelSaisonEnde);

            // Zusatzzeilen, Lüftung und Herleitungen
            t.LabelFerienzeitraeume = Text_("KOND_LBL_FERIENZEITRAEUME", t.LabelFerienzeitraeume);
            t.LabelMaxRaumtemperatur = Text_("KOND_LBL_MAXRAUMTEMPERATUR", t.LabelMaxRaumtemperatur);
            t.LabelSommerlueftung = Text_("KOND_LBL_SOMMERLUEFTUNG", t.LabelSommerlueftung);
            t.LabelInfiltration = Text_("KOND_LBL_INFILTRATION", t.LabelInfiltration);
            t.LabelNutzerlueftung = Text_("KOND_LBL_NUTZERLUEFTUNG", t.LabelNutzerlueftung);
            t.LabelNachtauskuehlung = Text_("KOND_LBL_NACHTAUSKUEHLUNG", t.LabelNachtauskuehlung);
            t.LabelAussenabstand = Text_("KOND_LBL_AUSSENABSTAND", t.LabelAussenabstand);
            t.TextJahresmittel = Text_("KOND_TXT_JAHRESMITTEL", t.TextJahresmittel);
            t.TextHerleitungPersonen = Text_("KOND_TXT_HERLEITUNG_PERSONEN", t.TextHerleitungPersonen);
            t.TextHerleitungGeraete = Text_("KOND_TXT_HERLEITUNG_GERAETE", t.TextHerleitungGeraete);
            t.PlatzhalterKeine = Text_("KOND_TXT_KEINE", t.PlatzhalterKeine);
            t.PlatzhalterLeer = Text_("KOND_TXT_LEER", t.PlatzhalterLeer);
            t.PlatzhalterVorgabe = Text_("KOND_TXT_VORGABE", t.PlatzhalterVorgabe);
            t.PlatzhalterGanzjaehrig = Text_("KOND_TXT_GANZJAEHRIG", t.PlatzhalterGanzjaehrig);
            t.ZelleAus = Text_("KOND_LBL_AUS", t.ZelleAus);
            t.AusSchalter = Text_("KOND_LBL_AUS_SCHALTER", t.AusSchalter);

            // Zustand einer Kalenderkarte
            t.ZustandMatrix = Text_("KOND_TXT_ZUSTAND_MATRIX", t.ZustandMatrix);
            t.ZustandVorlage = Text_("KOND_TXT_ZUSTAND_VORLAGE", t.ZustandVorlage);
            t.ZustandAngelegt = Text_("KOND_TXT_ZUSTAND_ANGELEGT", t.ZustandAngelegt);
            t.ZustandAngelegtEine = Text_("KOND_TXT_ZUSTAND_ANGELEGT_EINE", t.ZustandAngelegtEine);
            t.ZustandGebaeude = Text_("KOND_TXT_ZUSTAND_GEBAEUDE", t.ZustandGebaeude);
            t.ZustandEigen = Text_("KOND_TXT_ZUSTAND_EIGEN", t.ZustandEigen);

            // Knöpfe
            t.KnopfKalenderAnlegen = Text_("KOND_BTN_KALENDER_ANLEGEN", t.KnopfKalenderAnlegen);
            t.KnopfVerwerfen = Text_("KOND_BTN_VERWERFEN", t.KnopfVerwerfen);
            t.KnopfMatrixErneut = Text_("KOND_BTN_MATRIX_ERNEUT", t.KnopfMatrixErneut);
            t.KnopfZuruecknehmen = Text_("KOND_BTN_ZURUECKNEHMEN", t.KnopfZuruecknehmen);
            t.KnopfUebernehmen = Text_("KOND_BTN_UEBERNEHMEN", t.KnopfUebernehmen);
            t.KnopfAlsVorlage = Text_("KOND_BTN_ALS_VORLAGE", t.KnopfAlsVorlage);
            t.KnopfVorlagenVerwalten = Text_("KOND_BTN_VORLAGEN_VERWALTEN", t.KnopfVorlagenVerwalten);
            t.KnopfKatalogErneut = Text_("KOND_BTN_KATALOG_ERNEUT", t.KnopfKatalogErneut);
            t.KnopfUebernehmenAnpassen = Text_("KOND_BTN_UEBERNEHMEN_ANPASSEN", t.KnopfUebernehmenAnpassen);
            t.KnopfErben = Text_("KOND_BTN_ERBEN", t.KnopfErben);
            t.KnopfDuplizieren = Text_("KOND_BTN_DUPLIZIEREN", t.KnopfDuplizieren);
            t.KnopfUmbenennen = Text_("KOND_BTN_UMBENENNEN", t.KnopfUmbenennen);
            t.KnopfLoeschen = Text_("KOND_BTN_LOESCHEN", t.KnopfLoeschen);
            t.KnopfSpeichern = Text_("KOND_BTN_SPEICHERN", t.KnopfSpeichern);
            t.KnopfAbbrechen = Text_("KOND_BTN_ABBRECHEN", t.KnopfAbbrechen);
            t.KnopfSchliessen = Text_("KOND_BTN_SCHLIESSEN", t.KnopfSchliessen);
            t.KnopfZeitfenster = Text_("KOND_BTN_ZEITFENSTER", t.KnopfZeitfenster);
            t.KnopfFeiertage = Text_("KOND_BTN_FEIERTAGE", t.KnopfFeiertage);
            t.KnopfZeitstruktur = Text_("KOND_BTN_ZEITSTRUKTUR", t.KnopfZeitstruktur);
            t.KnopfPeriodeNeu = Text_("KOND_BTN_PERIODE_NEU", t.KnopfPeriodeNeu);
            t.KnopfPeriodeBearbeiten = Text_("KOND_BTN_PERIODE_BEARBEITEN", t.KnopfPeriodeBearbeiten);
            t.KnopfPeriodeLoeschen = Text_("KOND_BTN_PERIODE_LOESCHEN", t.KnopfPeriodeLoeschen);
            t.KnopfRangHoeher = Text_("KOND_BTN_RANG_HOEHER", t.KnopfRangHoeher);
            t.KnopfRangNiedriger = Text_("KOND_BTN_RANG_NIEDRIGER", t.KnopfRangNiedriger);

            // Kalenderkarte
            t.LabelGrundangabe = Text_("KOND_LBL_GRUNDANGABE", t.LabelGrundangabe);
            t.LabelStandardwoche = Text_("KOND_LBL_STANDARDWOCHE", t.LabelStandardwoche);
            t.LabelWochenraster = Text_("KOND_LBL_WOCHENRASTER", t.LabelWochenraster);
            t.LabelZeitfenster = Text_("KOND_LBL_ZEITFENSTER", t.LabelZeitfenster);
            t.LabelZeitfensterTage = Text_("KOND_LBL_ZEITFENSTER_TAGE", t.LabelZeitfensterTage);
            t.LabelZeitfensterVon = Text_("KOND_LBL_ZEITFENSTER_VON", t.LabelZeitfensterVon);
            t.LabelZeitfensterBis = Text_("KOND_LBL_ZEITFENSTER_BIS", t.LabelZeitfensterBis);
            t.LabelZeitfensterWert = Text_("KOND_LBL_ZEITFENSTER_WERT", t.LabelZeitfensterWert);
            t.LabelPerioden = Text_("KOND_LBL_PERIODEN", t.LabelPerioden);
            t.SpalteArt = Text_("KOND_LBL_SPALTE_ART", t.SpalteArt);
            t.SpalteName = Text_("KOND_LBL_SPALTE_NAME", t.SpalteName);
            t.SpalteVon = Text_("KOND_LBL_SPALTE_VON", t.SpalteVon);
            t.SpalteBis = Text_("KOND_LBL_SPALTE_BIS", t.SpalteBis);
            t.SpalteWert = Text_("KOND_LBL_SPALTE_WERT", t.SpalteWert);
            t.SpalteRang = Text_("KOND_LBL_SPALTE_RANG", t.SpalteRang);
            t.ArtZeitraum = Text_("KOND_LBL_ART_ZEITRAUM", t.ArtZeitraum);
            t.ArtFerien = Text_("KOND_LBL_ART_FERIEN", t.ArtFerien);
            t.ArtFeiertag = Text_("KOND_LBL_ART_FEIERTAG", t.ArtFeiertag);
            t.ArtBetriebspause = Text_("KOND_LBL_ART_BETRIEBSPAUSE", t.ArtBetriebspause);
            t.WertEigeneWoche = Text_("KOND_TXT_WERT_EIGENE_WOCHE", t.WertEigeneWoche);
            t.WertWieWochentag = Text_("KOND_TXT_WERT_WIE_WOCHENTAG", t.WertWieWochentag);
            t.LabelWerkzeugFeiertage = Text_("KOND_LBL_WERKZEUG_FEIERTAGE", t.LabelWerkzeugFeiertage);
            t.LabelWerkzeugZeitstruktur = Text_("KOND_LBL_WERKZEUG_ZEITSTRUKTUR", t.LabelWerkzeugZeitstruktur);
            t.LabelWieHeizung = Text_("KOND_LBL_WIE_HEIZUNG", t.LabelWieHeizung);
            t.LabelWieAnwesenheit = Text_("KOND_LBL_WIE_ANWESENHEIT", t.LabelWieAnwesenheit);
            t.LabelVorschauWoche = Text_("KOND_LBL_VORSCHAU_WOCHE", t.LabelVorschauWoche);
            t.LabelTeppichbild = Text_("KOND_LBL_TEPPICHBILD", t.LabelTeppichbild);
            t.TextTeppichbild = Text_("KOND_TXT_TEPPICHBILD", t.TextTeppichbild);
            t.TextQuelle = Text_("KOND_TXT_QUELLE", t.TextQuelle);
            t.HinweisPerioden = Text_("KOND_TXT_HINWEIS_PERIODEN", t.HinweisPerioden);
            t.HinweisFeiertage = Text_("KOND_TXT_HINWEIS_FEIERTAGE", t.HinweisFeiertage);
            t.HinweisZeitstruktur = Text_("KOND_TXT_HINWEIS_ZEITSTRUKTUR", t.HinweisZeitstruktur);

            // Vorlagen
            t.LabelVorlageAuswahl = Text_("KOND_LBL_VORLAGE_AUSWAHL", t.LabelVorlageAuswahl);
            t.TextVorlageKeine = Text_("KOND_TXT_VORLAGE_KEINE", t.TextVorlageKeine);
            t.LabelVorlageName = Text_("KOND_LBL_VORLAGE_NAME", t.LabelVorlageName);
            t.LabelVorlageBeschreibung = Text_("KOND_LBL_VORLAGE_BESCHREIBUNG", t.LabelVorlageBeschreibung);
            t.LabelVorlageNutzung = Text_("KOND_LBL_VORLAGE_NUTZUNG", t.LabelVorlageNutzung);
            t.NutzungWohnen = Text_("KOND_LBL_NUTZUNG_WOHNEN", t.NutzungWohnen);
            t.NutzungBuero = Text_("KOND_LBL_NUTZUNG_BUERO", t.NutzungBuero);
            t.NutzungSchule = Text_("KOND_LBL_NUTZUNG_SCHULE", t.NutzungSchule);
            t.NutzungSonstige = Text_("KOND_LBL_NUTZUNG_SONSTIGE", t.NutzungSonstige);
            t.NutzungKeine = Text_("KOND_LBL_NUTZUNG_KEINE", t.NutzungKeine);
            t.LabelVorlageAusgeliefert = Text_("KOND_LBL_VORLAGE_AUSGELIEFERT", t.LabelVorlageAusgeliefert);
            t.LabelVorlageEigen = Text_("KOND_LBL_VORLAGE_EIGEN", t.LabelVorlageEigen);
            t.LabelVerwaltung = Text_("KOND_LBL_VERWALTUNG", t.LabelVerwaltung);
            t.LabelGroesse = Text_("KOND_LBL_GROESSE", t.LabelGroesse);
            t.HinweisVorlageSofort = Text_("KOND_TXT_VORLAGE_SOFORT", t.HinweisVorlageSofort);
            t.GrundVorlageGesperrt = Text_("KOND_TXT_VORLAGE_GESPERRT", t.GrundVorlageGesperrt);
            t.HinweisVorlageLoeschen = Text_("KOND_TXT_VORLAGE_LOESCHEN", t.HinweisVorlageLoeschen);
            t.HinweisVorlageUebernehmen = Text_("KOND_TXT_VORLAGE_UEBERNEHMEN", t.HinweisVorlageUebernehmen);
            t.HinweisVorlageSpeichern = Text_("KOND_TXT_VORLAGE_SPEICHERN", t.HinweisVorlageSpeichern);

            // Hinweise und Gründe
            t.HinweisVdi6007 = Text_("KOND_TXT_HINWEIS_VDI6007", t.HinweisVdi6007);
            t.HinweisAltfelder = Text_("KOND_TXT_HINWEIS_ALTFELDER", t.HinweisAltfelder);
            t.GrundKuehlenGesperrt = Text_("KOND_TXT_KUEHLEN_GESPERRT", t.GrundKuehlenGesperrt);
            t.HinweisKuehlenZone = Text_("KOND_TXT_KUEHLEN_ZONE", t.HinweisKuehlenZone);
            t.HinweisTagesbilanz = Text_("KOND_TXT_HINWEIS_TAGESBILANZ", t.HinweisTagesbilanz);
            t.HinweisLesemodus = Text_("KOND_TXT_HINWEIS_LESEMODUS", t.HinweisLesemodus);
            t.MeldungGemeinjahr = Text_("KOND_TXT_GEMEINJAHR", t.MeldungGemeinjahr);
            t.HinweisAus = Text_("KOND_TXT_HINWEIS_AUS", t.HinweisAus);
            t.HinweisNachtauskuehlung = Text_("KOND_TXT_HINWEIS_NACHTAUSKUEHLUNG", t.HinweisNachtauskuehlung);
            t.HinweisNachtfensterLeer = Text_("KOND_TXT_HINWEIS_NACHTFENSTER", t.HinweisNachtfensterLeer);
            t.HinweisSaison = Text_("KOND_TXT_HINWEIS_SAISON", t.HinweisSaison);
            t.GrundVomGebaeude = Text_("KOND_TXT_GRUND_VOM_GEBAEUDE", t.GrundVomGebaeude);
            t.HinweisUnbeheizt = Text_("KOND_TXT_HINWEIS_UNBEHEIZT", t.HinweisUnbeheizt);
            t.GrundOhneTabellen = Text_("KOND_TXT_GRUND_OHNE_TABELLEN", t.GrundOhneTabellen);

            // Katalogauswahl
            t.SpalteKatalogKalender = Text_("KOND_LBL_KATALOG_KALENDER", t.SpalteKatalogKalender);
            t.TextKatalogKalender = Text_("KOND_TXT_KATALOG_KALENDER", t.TextKatalogKalender);

            // Reiter und Rückfragen (Welle U1)
            t.HinweisZuruecknehmen = Text_("KOND_TXT_HINWEIS_ZURUECKNEHMEN", t.HinweisZuruecknehmen);
            t.GrundNichtsZurueck = Text_("KOND_TXT_GRUND_NICHTS_ZURUECK", t.GrundNichtsZurueck);
            t.TextNichts = Text_("KOND_TXT_NICHTS", t.TextNichts);
            t.TextPostenMatrixzellen = Text_("KOND_TXT_POSTEN_MATRIXZELLEN", t.TextPostenMatrixzellen);
            t.TextPostenKalender = Text_("KOND_TXT_POSTEN_KALENDER", t.TextPostenKalender);
            t.TextPostenStandardwoche = Text_("KOND_TXT_POSTEN_STANDARDWOCHE", t.TextPostenStandardwoche);
            t.TextPostenFerienperioden = Text_("KOND_TXT_POSTEN_FERIENPERIODEN", t.TextPostenFerienperioden);
            t.TextPostenSaison = Text_("KOND_TXT_POSTEN_SAISON", t.TextPostenSaison);
            t.TextPostenEigenePerioden = Text_("KOND_TXT_POSTEN_EIGENE_PERIODEN", t.TextPostenEigenePerioden);
            t.TextPostenFeiertage = Text_("KOND_TXT_POSTEN_FEIERTAGE", t.TextPostenFeiertage);
            t.TextPostenNachtzeiten = Text_("KOND_TXT_POSTEN_NACHTZEITEN", t.TextPostenNachtzeiten);
            t.TextPostenFerienzeitraeume = Text_("KOND_TXT_POSTEN_FERIENZEITRAEUME", t.TextPostenFerienzeitraeume);
            t.TextPostenLuftwechsel = Text_("KOND_TXT_POSTEN_LUFTWECHSEL", t.TextPostenLuftwechsel);
            t.TextPostenZonenkalender = Text_("KOND_TXT_POSTEN_ZONENKALENDER", t.TextPostenZonenkalender);
            t.TextPostenBauteile = Text_("KOND_TXT_POSTEN_BAUTEILE", t.TextPostenBauteile);

            // Zonenmatrix (Welle U4)
            t.PlatzhalterWieGebaeude = Text_("KOND_TXT_PLATZHALTER_WIE_GEBAEUDE", t.PlatzhalterWieGebaeude);
            t.HinweisZoneAufteilen = Text_("KOND_TXT_ZONE_AUFTEILEN", t.HinweisZoneAufteilen);
            t.HinweisZoneOhneWirkung = Text_("KOND_TXT_ZONE_OHNE_WIRKUNG", t.HinweisZoneOhneWirkung);
            t.LabelZoneKalender = Text_("KOND_LBL_ZONE_KALENDER", t.LabelZoneKalender);
            t.HinweisZoneMatrix = Text_("KOND_TXT_ZONE_MATRIX", t.HinweisZoneMatrix);

            // Gebäudeverwaltung (Welle U4)
            t.KnopfKonditionierung = Text_("KOND_BTN_KONDITIONIERUNG", t.KnopfKonditionierung);
            t.HinweisVerwaltungSpeichern = Text_("KOND_TXT_VERWALTUNG_SPEICHERN", t.HinweisVerwaltungSpeichern);
            t.HinweisVerwaltungAltfelder = Text_("KOND_TXT_VERWALTUNG_ALTFELDER", t.HinweisVerwaltungAltfelder);

            // Vorlagen je Karte (Welle U2)
            t.TextVorschauVorlage = Text_("KOND_TXT_VORSCHAU_VORLAGE", t.TextVorschauVorlage);
            t.GrundKeineVorlage = Text_("KOND_TXT_GRUND_KEINE_VORLAGE", t.GrundKeineVorlage);
            t.TextVorlageGespeichert = Text_("KOND_TXT_VORLAGE_GESPEICHERT", t.TextVorlageGespeichert);
            t.TextVorlageUmbenannt = Text_("KOND_TXT_VORLAGE_UMBENANNT", t.TextVorlageUmbenannt);
            t.TextVorlageGeloescht = Text_("KOND_TXT_VORLAGE_GELOESCHT", t.TextVorlageGeloescht);
            t.TextVorlageDupliziert = Text_("KOND_TXT_VORLAGE_DUPLIZIERT", t.TextVorlageDupliziert);
            t.TextVorlagenLeer = Text_("KOND_TXT_VORLAGEN_LEER", t.TextVorlagenLeer);
            t.SpalteAktionen = Text_("KOND_LBL_SPALTE_AKTIONEN", t.SpalteAktionen);

            // Die Karte im Einzelnen (Welle U3)
            t.KnopfEinzelheiten = Text_("KOND_BTN_EINZELHEITEN", t.KnopfEinzelheiten);
            t.GrundNichtAngelegt = Text_("KOND_TXT_GRUND_NICHT_ANGELEGT", t.GrundNichtAngelegt);
            t.HinweisGrundangabe = Text_("KOND_TXT_HINWEIS_GRUNDANGABE", t.HinweisGrundangabe);
            t.TextGrundangabeWoche = Text_("KOND_TXT_GRUNDANGABE_WOCHE", t.TextGrundangabeWoche);
            t.KnopfWocheAnlegen = Text_("KOND_BTN_WOCHE_ANLEGEN", t.KnopfWocheAnlegen);
            t.KnopfWocheVerwerfen = Text_("KOND_BTN_WOCHE_VERWERFEN", t.KnopfWocheVerwerfen);
            t.TextWocheVorgabe = Text_("KOND_TXT_WOCHE_VORGABE", t.TextWocheVorgabe);
            t.TextVermerk = Text_("KOND_TXT_VERMERK", t.TextVermerk);
            t.HinweisZeitfenster = Text_("KOND_TXT_HINWEIS_ZEITFENSTER", t.HinweisZeitfenster);
            t.GrundZeitfensterTage = Text_("KOND_TXT_GRUND_ZEITFENSTER_TAGE", t.GrundZeitfensterTage);
            t.GrundZeitfensterZeiten = Text_("KOND_TXT_GRUND_ZEITFENSTER_ZEITEN", t.GrundZeitfensterZeiten);
            t.GrundZeitfensterWert = Text_("KOND_TXT_GRUND_ZEITFENSTER_WERT", t.GrundZeitfensterWert);
            t.LabelAngabe = Text_("KOND_LBL_ANGABE", t.LabelAngabe);
            t.LabelAngabeWochentag = Text_("KOND_LBL_ANGABE_WOCHENTAG", t.LabelAngabeWochentag);
            t.LabelWochentag = Text_("KOND_LBL_WOCHENTAG", t.LabelWochentag);
            t.TextAusMatrix = Text_("KOND_TXT_AUS_MATRIX", t.TextAusMatrix);
            t.TextPeriodenLeer = Text_("KOND_TXT_PERIODEN_LEER", t.TextPeriodenLeer);
            t.GrundPeriodeUnvollstaendig = Text_("KOND_TXT_GRUND_PERIODE", t.GrundPeriodeUnvollstaendig);
            t.GrundRangOben = Text_("KOND_TXT_GRUND_RANG_OBEN", t.GrundRangOben);
            t.GrundRangUnten = Text_("KOND_TXT_GRUND_RANG_UNTEN", t.GrundRangUnten);
            t.GrundRangBand = Text_("KOND_TXT_GRUND_RANG_BAND", t.GrundRangBand);
            t.TextTeppichLeer = Text_("KOND_TXT_TEPPICH_LEER", t.TextTeppichLeer);
            t.KnopfInDenKalender = Text_("KOND_BTN_IN_DEN_KALENDER", t.KnopfInDenKalender);
            t.HinweisInDenKalender = Text_("KOND_TXT_HINWEIS_IN_DEN_KALENDER", t.HinweisInDenKalender);
            t.HinweisVorlageZeile = Text_("KOND_TXT_HINWEIS_VORLAGE_ZEILE", t.HinweisVorlageZeile);
            t.TextVorschauLeer = Text_("KOND_TXT_VORSCHAU_LEER", t.TextVorschauLeer);
            t.TextVorschauOhneAnteile = Text_("KOND_TXT_VORSCHAU_OHNE_ANTEILE", t.TextVorschauOhneAnteile);

            // Kopieren nach … (Teilkonzept 3.5, 7.4)
            t.KnopfKopierenNach = Text_("KOND_BTN_KOPIEREN_NACH", t.KnopfKopierenNach);
            t.KnopfKopieren = Text_("KOND_BTN_KOPIEREN", t.KnopfKopieren);
            t.LabelKopierenNach = Text_("KOND_LBL_KOPIEREN_NACH", t.LabelKopierenNach);
            t.LabelKopierziel = Text_("KOND_LBL_KOPIERZIEL", t.LabelKopierziel);
            t.LabelKomfortsollwert = Text_("KOND_LBL_KOMFORTSOLLWERT", t.LabelKomfortsollwert);
            t.HinweisKopierenNach = Text_("KOND_TXT_KOPIEREN_NACH", t.HinweisKopierenNach);
            t.HinweisKopierenDirekt = Text_("KOND_TXT_KOPIEREN_DIREKT", t.HinweisKopierenDirekt);
            t.HinweisKopierenZeitstruktur = Text_("KOND_TXT_KOPIEREN_ZEITSTRUKTUR", t.HinweisKopierenZeitstruktur);
            t.GrundKeinKopierziel = Text_("KOND_TXT_GRUND_KEIN_KOPIERZIEL", t.GrundKeinKopierziel);
            t.TextVorlageKopiert = Text_("KOND_TXT_VORLAGE_KOPIERT", t.TextVorlageKopiert);
            t.MeldungKomfortsollwert = Text_("KOND_TXT_MELDUNG_KOMFORTSOLLWERT", t.MeldungKomfortsollwert);
            // Die Abkürzung „alle Größen“ (E57, Welle U5)
            t.LabelVorlageAlle = Text_("KOND_LBL_VORLAGE_ALLE", t.LabelVorlageAlle);
            t.HinweisVorlageAlle = Text_("KOND_TXT_HINWEIS_VORLAGE_ALLE", t.HinweisVorlageAlle);
            return t;
        }

        /// <summary>
        /// Das Bündel der Rückfragen der Konditionierung (<see cref="KonditionierungFragetexte"/>,
        /// <c>KOND_FRAGE_*</c>) — der Rückfall ist der Vorgabewert des Bündels.
        /// </summary>
        internal static KonditionierungFragetexte Fragen()
        {
            var f = new KonditionierungFragetexte();
            f.Verwerfen = Text_("KOND_FRAGE_VERWERFEN", f.Verwerfen);
            f.MatrixErneut = Text_("KOND_FRAGE_MATRIX_ERNEUT", f.MatrixErneut);
            f.KatalogErneut = Text_("KOND_FRAGE_KATALOG_ERNEUT", f.KatalogErneut);
            f.Aufteilen = Text_("KOND_FRAGE_AUFTEILEN", f.Aufteilen);
            f.SpeichernUnter = Text_("KOND_FRAGE_SPEICHERN_UNTER", f.SpeichernUnter);
            f.Zonen = Text_("KOND_FRAGE_ZONEN", f.Zonen);
            f.OhneEinzelheiten = Text_("KOND_FRAGE_OHNE_EINZELHEITEN", f.OhneEinzelheiten);
            f.VorlageUebernehmen = Text_("KOND_FRAGE_VORLAGE_UEBERNEHMEN", f.VorlageUebernehmen);
            f.VorlageLoeschen = Text_("KOND_FRAGE_VORLAGE_LOESCHEN", f.VorlageLoeschen);
            f.Sollwertprofil = Text_("KOND_FRAGE_SOLLWERTPROFIL", f.Sollwertprofil);

            // Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…“ (E57, Welle U5)
            f.VorlageAlleTitel = Text_("KOND_FRAGE_VORLAGE_ALLE_TITEL", f.VorlageAlleTitel);
            f.VorlageAlle = Text_("KOND_FRAGE_VORLAGE_ALLE", f.VorlageAlle);
            f.VorlageAlleZeile = Text_("KOND_FRAGE_VORLAGE_ALLE_ZEILE", f.VorlageAlleZeile);
            f.VorlageAlleUebernehmen = Text_("KOND_FRAGE_VORLAGE_ALLE_UEBERNEHMEN", f.VorlageAlleUebernehmen);
            f.VorlageAlleErsetzt = Text_("KOND_FRAGE_VORLAGE_ALLE_ERSETZT", f.VorlageAlleErsetzt);
            f.VorlageAlleAufteilen = Text_("KOND_FRAGE_VORLAGE_ALLE_AUFTEILEN", f.VorlageAlleAufteilen);
            f.VorlageAlleOhne = Text_("KOND_FRAGE_VORLAGE_ALLE_OHNE", f.VorlageAlleOhne);
            f.VorlageAlleGesperrt = Text_("KOND_FRAGE_VORLAGE_ALLE_GESPERRT", f.VorlageAlleGesperrt);
            return f;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
