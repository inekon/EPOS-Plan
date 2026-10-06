using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Blatts „Nutzungsprofile"</b> (Stufe NP3a; Konzept Nutzungsprofile 6.1) — sie baut aus
    /// <see cref="RaumnutzungCtrl"/> und <see cref="Raumnutzungsgenerator"/> die DTO der Razor-Seite und
    /// schreibt zurück. Plattformfrei; die Übersetzung zwischen den Typen der Oberfläche
    /// (<see cref="RaumnutzungProfilDaten"/>, <see cref="RaumnutzungArt"/>, <see cref="KonditionierungGroesse"/>)
    /// und denen des Kerns (<c>Raumnutzungsprofil</c>, <c>Konditionierungsgroesse</c>, die Kennungen des Schemas)
    /// steht <b>allein hier</b>.
    ///
    /// <para><b>Die Vorschau schreibt nicht:</b> Sie lässt den Generator über dem Feldsatz des Editors rechnen
    /// und liest die erzeugte <c>Konditionierungsvorlage</c> als Zeilenliste aus — ohne Datenbank.</para>
    /// <para><b>Ablehnungen kommen benannt</b> als Text des Kerns (NP-F11, NP-F19, NP-F20); still übergangen
    /// wird nichts.</para>
    /// </summary>
    internal static class RaumnutzungHuelle
    {
        // =================================================================
        //  Der Weg
        // =================================================================

        /// <summary>
        /// Das Bündel des Blatts. Ohne Katalogtabellen trägt es nur den Grund und keinen Delegaten — das Blatt
        /// nennt ihn dann und bietet keinen Knopf an.
        /// </summary>
        internal static RaumnutzungWeg Weg()
        {
            if (!RaumnutzungCtrl.Lesbar())
                return new RaumnutzungWeg { Sperrgrund = MyResource.Resource.RAUMNUTZUNG_MSG_SCHEMA_FEHLT };

            var ctrl = new RaumnutzungCtrl();
            return new RaumnutzungWeg
            {
                Projektdatei = ProjektdateiProfileHuelle.BlattWeg(),
                Kategorien = () => ctrl.Kategorien().Select(Kategorie).ToList(),
                Profile = id => ctrl.Profile(id).Select(Profil).ToList(),
                Zuordnungen = () => ctrl.Zuordnungen().Select(Zuordnung).ToList(),
                Vorschau = Vorschau,
                Nutzungstage = d => d == null ? null : Raumnutzungsgenerator.Nutzungstage(Kern(d)).OhneFerien,
                KategorieAnlegen = (bez, besch, quelle) => Ergebnis(ctrl.KategorieAnlegen(bez, besch, quelle)),
                KategorieAendern = (id, bez, besch, quelle) => Ergebnis(ctrl.KategorieAendern(id, bez, besch, quelle)),
                KategorieDuplizieren = (id, bez) => Ergebnis(ctrl.KategorieDuplizieren(id, bez)),
                KategorieLoeschen = id => Ergebnis(ctrl.KategorieLoeschen(id)),
                ProfilAnlegen = p => Ergebnis(ctrl.ProfilAnlegen(Kern(p))),
                ProfilAendern = p => Ergebnis(ctrl.ProfilAendern(Kern(p))),
                ProfilDuplizieren = (id, ziel, bez) => Ergebnis(ctrl.ProfilDuplizieren(id, ziel, bez)),
                ProfilLoeschen = id => Ergebnis(ctrl.ProfilLoeschen(id)),
                ZuordnungenAuf = id => ctrl.ZuordnungenAuf(id),
                ZuordnungSetzen = (art, schluessel, idProfil)
                    => Ergebnis(ctrl.ZuordnungSetzen(Kern(art), schluessel, idProfil)),
                ZuordnungLoeschen = id => Ergebnis(ctrl.ZuordnungLoeschen(id)),
                Csv = RaumnutzungCsvHuelle.Weg(ctrl),
                Bild = RaumnutzungBildHuelle.Weg(),
            };
        }

        /// <summary>Das Textbündel des Blatts aus <c>MyResource</c> — Rückfall ist der deutsche Vorgabewert.</summary>
        internal static RaumnutzungTexte Texte()
        {
            var t = new RaumnutzungTexte();

            t.Blatt = Text_("RNP_LBL_BLATT", t.Blatt);
            t.KnopfVerwalten = Text_("RNP_BTN_VERWALTEN", t.KnopfVerwalten);
            t.Katalog = Text_("RNP_LBL_KATALOG", t.Katalog);
            t.Editor = Text_("RNP_LBL_EDITOR", t.Editor);
            t.Vorschau = Text_("RNP_LBL_VORSCHAU", t.Vorschau);
            t.ReiterKatalog = Text_("RNP_LBL_REITER_KATALOG", t.ReiterKatalog);
            t.ReiterZuordnung = Text_("RNP_LBL_REITER_ZUORDNUNG", t.ReiterZuordnung);
            t.HinweisSofort = Text_("RNP_TXT_SOFORT", t.HinweisSofort);

            t.ArtEposMuster = Text_("RNP_LBL_ART_EPOS", t.ArtEposMuster);
            t.ArtDin = Text_("RNP_LBL_ART_DIN", t.ArtDin);
            t.ArtSia = Text_("RNP_LBL_ART_SIA", t.ArtSia);
            t.ArtVdi = Text_("RNP_LBL_ART_VDI", t.ArtVdi);
            t.ArtEigen = Text_("RNP_LBL_ART_EIGEN", t.ArtEigen);
            t.ZuordnungsartDin = Text_("RNP_LBL_ZUART_DIN", t.ZuordnungsartDin);
            t.ZuordnungsartIfc = Text_("RNP_LBL_ZUART_IFC", t.ZuordnungsartIfc);
            t.ZuordnungsartHottcad = Text_("RNP_LBL_ZUART_HOTTCAD", t.ZuordnungsartHottcad);

            t.KnopfKategorieNeu = Text_("RNP_BTN_KATEGORIE_NEU", t.KnopfKategorieNeu);
            t.KnopfUmbenennen = Text_("RNP_BTN_UMBENENNEN", t.KnopfUmbenennen);
            t.KnopfProfilNeu = Text_("RNP_BTN_PROFIL_NEU", t.KnopfProfilNeu);
            t.KnopfDuplizieren = Text_("RNP_BTN_DUPLIZIEREN", t.KnopfDuplizieren);
            t.KnopfLoeschen = Text_("RNP_BTN_LOESCHEN", t.KnopfLoeschen);
            t.KnopfSpeichern = Text_("RNP_BTN_SPEICHERN", t.KnopfSpeichern);
            t.KnopfAbbrechen = Text_("RNP_BTN_ABBRECHEN", t.KnopfAbbrechen);
            t.KnopfVorschau = Text_("RNP_BTN_VORSCHAU", t.KnopfVorschau);
            t.KnopfZeileNeu = Text_("RNP_BTN_ZEILE_NEU", t.KnopfZeileNeu);

            t.LabelNummer = Text_("RNP_LBL_NUMMER", t.LabelNummer);
            t.LabelName = Text_("RNP_LBL_NAME", t.LabelName);
            t.LabelBeschreibung = Text_("RNP_LBL_BESCHREIBUNG", t.LabelBeschreibung);
            t.LabelQuelle = Text_("RNP_LBL_QUELLE", t.LabelQuelle);
            t.LabelAktionen = Text_("RNP_LBL_AKTIONEN", t.LabelAktionen);
            t.LabelArt = Text_("RNP_LBL_ART", t.LabelArt);
            t.LabelSchluessel = Text_("RNP_LBL_SCHLUESSEL", t.LabelSchluessel);
            t.LabelProfil = Text_("RNP_LBL_PROFIL", t.LabelProfil);
            t.LabelKeine = Text_("RNP_LBL_KEINE", t.LabelKeine);

            t.GruppeZeit = Text_("RNP_LBL_GRUPPE_ZEIT", t.GruppeZeit);
            t.GruppeSollwerte = Text_("RNP_LBL_GRUPPE_SOLL", t.GruppeSollwerte);
            t.GruppeLuft = Text_("RNP_LBL_GRUPPE_LUFT", t.GruppeLuft);
            t.GruppeLasten = Text_("RNP_LBL_GRUPPE_LASTEN", t.GruppeLasten);

            t.LabelNutzungVon = Text_("RNP_LBL_NUTZUNG_VON", t.LabelNutzungVon);
            t.LabelNutzungBis = Text_("RNP_LBL_NUTZUNG_BIS", t.LabelNutzungBis);
            t.LabelBetriebVon = Text_("RNP_LBL_BETRIEB_VON", t.LabelBetriebVon);
            t.LabelBetriebBis = Text_("RNP_LBL_BETRIEB_BIS", t.LabelBetriebBis);
            t.LabelWoche = Text_("RNP_LBL_WOCHE", t.LabelWoche);
            t.LabelFeiertage = Text_("RNP_LBL_FEIERTAGE", t.LabelFeiertage);

            t.LabelHeizSoll = Text_("RNP_LBL_HEIZ_SOLL", t.LabelHeizSoll);
            t.LabelHeizAusserhalb = Text_("RNP_LBL_HEIZ_AUSSERHALB", t.LabelHeizAusserhalb);
            t.LabelHeizAus = Text_("RNP_LBL_HEIZ_AUS", t.LabelHeizAus);
            t.LabelKuehlSoll = Text_("RNP_LBL_KUEHL_SOLL", t.LabelKuehlSoll);
            t.LabelKuehlAusserhalb = Text_("RNP_LBL_KUEHL_AUSSERHALB", t.LabelKuehlAusserhalb);
            t.LabelKuehlAus = Text_("RNP_LBL_KUEHL_AUS", t.LabelKuehlAus);

            t.LabelLuft = Text_("RNP_LBL_LUFT", t.LabelLuft);
            t.LabelLuftAusserhalb = Text_("RNP_LBL_LUFT_AUSSERHALB", t.LabelLuftAusserhalb);
            t.LabelLuftEinheit = Text_("RNP_LBL_LUFT_EINHEIT", t.LabelLuftEinheit);

            t.LabelPersonenFlaeche = Text_("RNP_LBL_PERSONEN_FLAECHE", t.LabelPersonenFlaeche);
            t.LabelPersonenWaerme = Text_("RNP_LBL_PERSONEN_WAERME", t.LabelPersonenWaerme);
            t.LabelPersonenAnteil = Text_("RNP_LBL_PERSONEN_ANTEIL", t.LabelPersonenAnteil);
            t.LabelPersonenAusserhalb = Text_("RNP_LBL_PERSONEN_AUSSERHALB", t.LabelPersonenAusserhalb);
            t.LabelGeraete = Text_("RNP_LBL_GERAETE", t.LabelGeraete);
            t.LabelGeraeteAnteil = Text_("RNP_LBL_GERAETE_ANTEIL", t.LabelGeraeteAnteil);
            t.LabelGeraeteAusserhalb = Text_("RNP_LBL_GERAETE_AUSSERHALB", t.LabelGeraeteAusserhalb);
            t.LabelBeleuchtung = Text_("RNP_LBL_BELEUCHTUNG", t.LabelBeleuchtung);
            t.LabelBeleuchtungAnteil = Text_("RNP_LBL_BELEUCHTUNG_ANTEIL", t.LabelBeleuchtungAnteil);

            t.LabelZeilenbild = Text_("RNP_LBL_ZEILENBILD", t.LabelZeilenbild);
            t.LabelStunden = Text_("RNP_LBL_STUNDEN", t.LabelStunden);
            t.LabelZeile = Text_("RNP_LBL_ZEILE", t.LabelZeile);
            t.LabelWert = Text_("RNP_LBL_WERT", t.LabelWert);
            t.LabelFenster = Text_("RNP_LBL_FENSTER", t.LabelFenster);
            t.LabelWochentage = Text_("RNP_LBL_WOCHENTAGE", t.LabelWochentage);
            t.TagesartWerktag = Text_("RNP_LBL_TAGESART_WERKTAG", t.TagesartWerktag);
            t.TagesartFrei = Text_("RNP_LBL_TAGESART_FREI", t.TagesartFrei);

            t.Ausgeliefert = Text_("RNP_TXT_AUSGELIEFERT", t.Ausgeliefert);
            t.TextLeer = Text_("RNP_TXT_LEER", t.TextLeer);
            t.TextKategorieLeer = Text_("RNP_TXT_KATEGORIE_LEER", t.TextKategorieLeer);
            t.TextOhneWahl = Text_("RNP_TXT_OHNE_WAHL", t.TextOhneWahl);
            t.TextOhneWerte = Text_("RNP_TXT_OHNE_WERTE", t.TextOhneWerte);
            t.TextNichtBelegt = Text_("RNP_TXT_NICHT_BELEGT", t.TextNichtBelegt);
            t.GrundOhneTabellen = Text_("RNP_TXT_OHNE_TABELLEN", t.GrundOhneTabellen);
            t.FrageProfilLoeschen = Text_("RNP_FRAGE_PROFIL_LOESCHEN", t.FrageProfilLoeschen);
            t.FrageProfilZuordnung = Text_("RNP_FRAGE_PROFIL_ZUORDNUNG", t.FrageProfilZuordnung);
            t.FrageKategorieLoeschen = Text_("RNP_FRAGE_KATEGORIE_LOESCHEN", t.FrageKategorieLoeschen);
            t.FrageZuordnungLoeschen = Text_("RNP_FRAGE_ZUORDNUNG_LOESCHEN", t.FrageZuordnungLoeschen);
            t.TextNutzungstage = Text_("RNP_TXT_NUTZUNGSTAGE", t.TextNutzungstage);
            t.TextNutzungstageFerien = Text_("RNP_TXT_NUTZUNGSTAGE_FERIEN", t.TextNutzungstageFerien);
            t.KnopfUebernehmenProfil = Text_("RNP_BTN_PROFIL_UEBERNEHMEN", t.KnopfUebernehmenProfil);
            t.TitelUebernahme = Text_("RNP_LBL_UEBERNAHME", t.TitelUebernahme);
            t.KnopfUebernehmen = Text_("RNP_BTN_UEBERNEHMEN", t.KnopfUebernehmen);
            t.KnopfSchliessen = Text_("RNP_BTN_SCHLIESSEN", t.KnopfSchliessen);
            t.HinweisUebernahme = Text_("RNP_TXT_UEBERNAHME_OK", t.HinweisUebernahme);
            t.TextKeinProfil = Text_("RNP_TXT_KEIN_PROFIL", t.TextKeinProfil);
            t.TextOhneWerteKurz = Text_("RNP_TXT_OHNE_WERTE_KURZ", t.TextOhneWerteKurz);
            t.TageKurz = Text_("RNP_TXT_TAGE_KURZ", t.TageKurz);
            t.Nennwertzeile = Text_("RNP_TXT_NENNWERT", t.Nennwertzeile);
            t.FrageUebernehmen = Text_("RNP_FRAGE_UEBERNEHMEN", t.FrageUebernehmen);
            t.FrageZeile = Text_("RNP_FRAGE_ZEILE", t.FrageZeile);
            t.FrageUebernimmt = Text_("RNP_FRAGE_UEBERNIMMT", t.FrageUebernimmt);
            t.FrageErsetzt = Text_("RNP_FRAGE_ERSETZT", t.FrageErsetzt);
            t.FrageBleibt = Text_("RNP_FRAGE_BLEIBT", t.FrageBleibt);
            t.FrageUnbeheizt = Text_("RNP_FRAGE_UNBEHEIZT", t.FrageUnbeheizt);
            t.FrageAufteilen = Text_("RNP_FRAGE_AUFTEILEN", t.FrageAufteilen);
            t.FrageOhneWerte = Text_("RNP_FRAGE_OHNE_WERTE", t.FrageOhneWerte);
            t.FrageName = Text_("RNP_FRAGE_NAME", t.FrageName);
            t.GrundOhneWerte = Text_("RNP_GRUND_OHNE_WERTE", t.GrundOhneWerte);
            t.GrundOhneWahl = Text_("RNP_GRUND_OHNE_WAHL", t.GrundOhneWahl);
            t.TextUebernommen = Text_("RNP_TXT_UEBERNOMMEN", t.TextUebernommen);
            t.Zonenkopf = Text_("RNP_TXT_ZONENKOPF", t.Zonenkopf);
            t.LabelVorschauGroesse = Text_("RNP_LBL_VORSCHAU_GROESSE", t.LabelVorschauGroesse);
            t.KnopfProfileImport = Text_("RNP_BTN_PROFILE_IMPORT", t.KnopfProfileImport);
            t.LabelNutzungVorschlag = Text_("RNP_LBL_NUTZUNG_VORSCHLAG", t.LabelNutzungVorschlag);
            return t;
        }

        // =================================================================
        //  Lesen: Kern -> Oberfläche
        // =================================================================

        /// <summary>Eine Kategorie des Kerns als DTO.</summary>
        internal static RaumnutzungKategorieDaten Kategorie(RaumnutzungCtrl.Kategorie k)
            => new RaumnutzungKategorieDaten(k.Id, k.Bezeichner ?? "", Art(k.Art), k.Ausgeliefert,
                                             k.Beschreibung ?? "", k.Quellenhinweis ?? "");

        /// <summary>Eine Zuordnungszeile des Kerns als DTO.</summary>
        internal static RaumnutzungZuordnungDaten Zuordnung(RaumnutzungCtrl.Zuordnung z)
            => new RaumnutzungZuordnungDaten(z.Id, Zuordnungsart(z.Art), z.Schluessel ?? "", z.IdProfil,
                                             z.Ausgeliefert);

        /// <summary>Ein Profil des Kerns als Feldsatz der Oberfläche (Kennwerte, Zeilenbild, Stundenprofile).</summary>
        internal static RaumnutzungProfilDaten Profil(Raumnutzungsprofil p)
        {
            var d = new RaumnutzungProfilDaten
            {
                Id = p.Id,
                IdKategorie = p.IdKatalog,
                Nummer = p.Nummer ?? "",
                Bezeichner = p.Bezeichner ?? "",
                Beschreibung = p.Beschreibung ?? "",
                Ausgeliefert = p.Ausgeliefert,
                NutzungVon = p.Nutzung_Von,
                NutzungBis = p.Nutzung_Bis,
                BetriebVon = p.Betrieb_Von,
                BetriebBis = p.Betrieb_Bis,
                NutzungstageWoche = p.Nutzungstage_Woche ?? "",
                FeiertageWieSonntag = p.Feiertage_Wie_Sonntag,
                HeizSoll = p.Heiz_Soll,
                HeizSollAusserhalb = p.Heiz_Soll_Ausserhalb,
                HeizAusAusserhalb = p.Heiz_Aus_Ausserhalb,
                KuehlSoll = p.Kuehl_Soll,
                KuehlSollAusserhalb = p.Kuehl_Soll_Ausserhalb,
                KuehlAusAusserhalb = p.Kuehl_Aus_Ausserhalb,
                Aussenluft = p.Aussenluft,
                LuftEinheit = p.Aussenluft_Einheit == RaumnutzungSchema.EINHEIT_JE_FLAECHE
                    ? RaumnutzungLuftEinheit.JeFlaeche : RaumnutzungLuftEinheit.JeStunde,
                AussenluftAusserhalb = p.Aussenluft_Ausserhalb,
                PersonenFlaeche = p.Personen_Flaeche,
                PersonenWaerme = p.Personen_Waerme,
                PersonenAnteil = p.Personen_Anteil,
                PersonenAnteilAusserhalb = p.Personen_Anteil_Ausserhalb,
                GeraeteLeistung = p.Geraete_Leistung,
                GeraeteAnteil = p.Geraete_Anteil,
                GeraeteAnteilAusserhalb = p.Geraete_Anteil_Ausserhalb,
                BeleuchtungLeistung = p.Beleuchtung_Leistung,
                BeleuchtungAnteil = p.Beleuchtung_Anteil,
            };

            d.Zeilenbild = (p.Zeilen ?? new List<Vorgabezeile>())
                .Where(z => Konditionierungsgroessen.AusKennwort(z.Groesse, out _))
                .Select(z =>
                {
                    Konditionierungsgroessen.AusKennwort(z.Groesse, out Konditionierungsgroesse g);
                    return new RaumnutzungZeilenbildDaten(KonditionierungHuelle.Oberflaeche(g), z.Zeile ?? "",
                                                          z.Wert, z.Aus, z.Von, z.Bis, z.BedingtK);
                }).ToList();

            d.Stunden = (p.Stunden ?? new List<Raumnutzungsstunden>())
                .Where(s => Konditionierungsgroessen.AusKennwort(s.Groesse, out _))
                .Select(s =>
                {
                    Konditionierungsgroessen.AusKennwort(s.Groesse, out Konditionierungsgroesse g);
                    double[] werte = Raumnutzungsgenerator.Stundenwerte(s.Werte, g) ?? Array.Empty<double>();
                    return new RaumnutzungStundenDaten(KonditionierungHuelle.Oberflaeche(g),
                                                       s.Tagesart == RaumnutzungSchema.TAGESART_FREI
                                                           ? RaumnutzungTagesart.Frei : RaumnutzungTagesart.Werktag,
                                                       werte);
                }).ToList();
            return d;
        }

        // =================================================================
        //  Schreiben: Oberfläche -> Kern
        // =================================================================

        /// <summary>Der Feldsatz der Oberfläche als Profil des Kerns — die eine Stelle der Rückübersetzung.</summary>
        internal static Raumnutzungsprofil Kern(RaumnutzungProfilDaten d)
        {
            var p = new Raumnutzungsprofil
            {
                Id = d.Id,
                IdKatalog = d.IdKategorie,
                Nummer = Leer(d.Nummer),
                Bezeichner = d.Bezeichner ?? "",
                Beschreibung = Leer(d.Beschreibung),
                Ausgeliefert = d.Ausgeliefert,
                Nutzung_Von = d.NutzungVon,
                Nutzung_Bis = d.NutzungBis,
                Betrieb_Von = d.BetriebVon,
                Betrieb_Bis = d.BetriebBis,
                Nutzungstage_Woche = Leer(d.NutzungstageWoche),
                Feiertage_Wie_Sonntag = d.FeiertageWieSonntag,
                Heiz_Soll = d.HeizSoll,
                Heiz_Soll_Ausserhalb = d.HeizSollAusserhalb,
                Heiz_Aus_Ausserhalb = d.HeizAusAusserhalb,
                Kuehl_Soll = d.KuehlSoll,
                Kuehl_Soll_Ausserhalb = d.KuehlSollAusserhalb,
                Kuehl_Aus_Ausserhalb = d.KuehlAusAusserhalb,
                Aussenluft = d.Aussenluft,
                Aussenluft_Ausserhalb = d.AussenluftAusserhalb,
                Personen_Flaeche = d.PersonenFlaeche,
                Personen_Waerme = d.PersonenWaerme,
                Personen_Anteil = d.PersonenAnteil,
                Personen_Anteil_Ausserhalb = d.PersonenAnteilAusserhalb,
                Geraete_Leistung = d.GeraeteLeistung,
                Geraete_Anteil = d.GeraeteAnteil,
                Geraete_Anteil_Ausserhalb = d.GeraeteAnteilAusserhalb,
                Beleuchtung_Leistung = d.BeleuchtungLeistung,
                Beleuchtung_Anteil = d.BeleuchtungAnteil,
            };

            // Die Einheit steht nur da, wo die Außenluft einen Wert trägt (NP-F10).
            p.Aussenluft_Einheit = d.Aussenluft.HasValue || d.AussenluftAusserhalb.HasValue
                ? (d.LuftEinheit == RaumnutzungLuftEinheit.JeFlaeche
                    ? RaumnutzungSchema.EINHEIT_JE_FLAECHE : RaumnutzungSchema.EINHEIT_JE_STUNDE)
                : null;

            // Zeilenbild und Stundenprofile reisen unverändert mit (NP3a zeigt sie nur; NP4 bearbeitet sie).
            p.Zeilen = (d.Zeilenbild ?? Array.Empty<RaumnutzungZeilenbildDaten>()).Select(z => new Vorgabezeile
            {
                Groesse = Konditionierungsgroessen.Kennwort(KonditionierungHuelle.Kern(z.Groesse)),
                Zeile = z.Zeile,
                Wert = z.Wert,
                Aus = z.Aus,
                Von = z.Von,
                Bis = z.Bis,
                BedingtK = z.DeltaT,
            }).ToList();

            p.Stunden = (d.Stunden ?? Array.Empty<RaumnutzungStundenDaten>()).Select(s => new Raumnutzungsstunden(
                Konditionierungsgroessen.Kennwort(KonditionierungHuelle.Kern(s.Groesse)),
                s.Tagesart == RaumnutzungTagesart.Frei
                    ? RaumnutzungSchema.TAGESART_FREI : RaumnutzungSchema.TAGESART_WERKTAG,
                string.Join(";", s.Werte.Select(w => double.IsNaN(w)
                    ? DbWerte.KOND_WOCHE_AUS
                    : w.ToString("0.####", CultureInfo.InvariantCulture))))).ToList();
            return p;
        }

        // =================================================================
        //  Die Vorschau (ohne Datenbank, ohne Schreiben)
        // =================================================================

        /// <summary>
        /// Was der Generator aus dem Feldsatz machen würde: je Größe Weg, Zeilen, Nennwert samt Herleitung und
        /// Hinweis, dazu die Zeile des Tagesvergleichs (NP-F8).
        /// </summary>
        internal static RaumnutzungVorschau Vorschau(RaumnutzungProfilDaten d, double? flaeche, double? lichteHoehe)
        {
            if (d == null) return RaumnutzungVorschau.Keine;
            RaumnutzungTexte t = Texte();
            KonditionierungTexte kt = KonditionierungTexteHuelle.Texte();
            Raumnutzungsprofil p = Kern(d);

            var groessen = new List<RaumnutzungVorschauGroesse>();
            var hinweise = new List<string>();
            foreach (Raumnutzungsgroesse g in Raumnutzungsgenerator.Erzeugen(p, flaeche, lichteHoehe))
            {
                string hinweis = Hinweistext(g.Hinweis, t);
                if (hinweis.Length > 0 && !hinweise.Contains(hinweis)) hinweise.Add(hinweis);

                bool belegt = g.Weg != Raumnutzungsweg.Keiner && g.Vorlage != null;
                groessen.Add(new RaumnutzungVorschauGroesse(
                    KonditionierungHuelle.Oberflaeche(g.Groesse),
                    belegt,
                    Wegtext(g.Weg, t),
                    belegt ? Zeilen(g.Vorlage, g.Groesse, kt) : Array.Empty<RaumnutzungVorschauzeile>(),
                    g.Nennwert.HasValue && !string.IsNullOrEmpty(g.Nennwertherleitung) ? g.Nennwertherleitung : "",
                    hinweis));
            }

            return new RaumnutzungVorschau(d.ToString(), groessen, Nutzungstagezeile(p, null, t), hinweise);
        }

        /// <summary>Die Vorgabezeilen und, wo der Generator einen Kalender anlegt, die Standardwoche.</summary>
        private static IReadOnlyList<RaumnutzungVorschauzeile> Zeilen(Konditionierungsvorlage v,
                                                                      Konditionierungsgroesse g,
                                                                      KonditionierungTexte kt)
        {
            var liste = new List<RaumnutzungVorschauzeile>();
            if (v?.Inhalt == null) return liste;
            foreach (Vorgabezeile z in v.Inhalt.Vorgabezeilen().OrderBy(z => Rang(z.Zeile)))
                liste.Add(new RaumnutzungVorschauzeile(Zeilenname(z.Zeile, kt), Werttext(z.Wert, z.Aus, g, kt),
                                                       Fenster(z.Von, z.Bis), ""));

            // Ein angelegter Kalender (Stundenprofil oder Feiertagsregeln) bringt seine Standardwoche mit;
            // eine Grundangabe ohne Woche fuehrt keine.
            Konditionierungskalender k = v.Inhalt.Kalender(g);
            if (k?.Standardwoche is { Count: > 0 })
                liste.Add(new RaumnutzungVorschauzeile(kt.ZeileVorlage, Wochentext(k.Standardwoche, g), "", ""));
            return liste;
        }

        private static int Rang(string zeile)
        {
            int i = RaumnutzungSchema.ZEILEN.ToList().IndexOf(zeile ?? "");
            return i < 0 ? int.MaxValue : i;
        }

        private static string Zeilenname(string zeile, KonditionierungTexte kt)
        {
            if (zeile == DbWerte.KOND_ZEILE_TAG) return kt.ZeileTag;
            if (zeile == DbWerte.KOND_ZEILE_NACHT) return kt.ZeileNacht;
            if (zeile == DbWerte.KOND_ZEILE_WOCHENENDE) return kt.ZeileWochenende;
            if (zeile == DbWerte.KOND_ZEILE_FERIEN) return kt.ZeileFerien;
            return zeile ?? "";
        }

        private static string Werttext(double? wert, bool aus, Konditionierungsgroesse g, KonditionierungTexte kt)
        {
            if (aus) return kt.ZelleAus;
            if (!wert.HasValue) return "";
            bool anteil = !Konditionierungsgroessen.HatNennwert(g)
                          || g == Konditionierungsgroesse.Geraete || g == Konditionierungsgroesse.Personen;
            double w = anteil && (g == Konditionierungsgroesse.Geraete || g == Konditionierungsgroesse.Personen)
                ? wert.Value * 100.0 : wert.Value;
            return w.ToString("0.####", CultureInfo.CurrentCulture) + " " + Einheit(g);
        }

        private static string Einheit(Konditionierungsgroesse g) => g switch
        {
            Konditionierungsgroesse.Lueftung => "1/h",
            Konditionierungsgroesse.Geraete or Konditionierungsgroesse.Personen => "%",
            _ => "°C",
        };

        /// <summary>Die Standardwoche in Kurzform — die ersten acht Stunden, dann „…".</summary>
        private static string Wochentext(IReadOnlyList<double> woche, Konditionierungsgroesse g)
        {
            bool anteil = g == Konditionierungsgroesse.Geraete || g == Konditionierungsgroesse.Personen;
            IEnumerable<string> teile = woche.Take(8).Select(w => double.IsNaN(w)
                ? "–"
                : (anteil ? w * 100.0 : w).ToString("0.##", CultureInfo.CurrentCulture));
            return string.Join(" ", teile) + (woche.Count > 8 ? " …" : "");
        }

        private static string Fenster(int? von, int? bis)
            => von.HasValue && bis.HasValue
                ? von.Value.ToString(CultureInfo.CurrentCulture) + " – " + bis.Value.ToString(CultureInfo.CurrentCulture)
                : "";

        private static string Wegtext(Raumnutzungsweg w, RaumnutzungTexte t) => w switch
        {
            Raumnutzungsweg.Zeilenbild => t.LabelZeilenbild,
            Raumnutzungsweg.Stundenprofil => t.LabelStunden,
            Raumnutzungsweg.Kennwerte => t.Editor,
            _ => t.TextNichtBelegt,
        };

        internal static string Hinweistext(Raumnutzungshinweis h, RaumnutzungTexte t) => h switch
        {
            Raumnutzungshinweis.ProfilOhneWerte => t.TextOhneWerte,
            Raumnutzungshinweis.LueftungOhneHoehe => Text_("RNP_TXT_HINWEIS_HOEHE",
                "Außenluft in m³/(h·m²) ohne lichte Höhe des Ziels: die Lüftung wird nicht gesetzt."),
            Raumnutzungshinweis.FreierTagOhneTageswert => Text_("RNP_TXT_HINWEIS_FREIERTAG",
                "Ein freier Einzeltag braucht einen Tageswert dieser Größe; die Matrix kennt nur das Wochenende."),
            Raumnutzungshinweis.StundenprofilUnlesbar => Text_("RNP_TXT_HINWEIS_STUNDEN",
                "Ein Stundenprofil ist nicht lesbar; die Kennwerte gelten."),
            _ => "",
        };

        /// <summary>
        /// <b>Die Liste „Nutzungsprofil übernehmen…"</b> (Stufe NP3b; Konzept Nutzungsprofile 6.2): alle Profile in der
        /// Ordnung des Katalogs — Kategorien wie der Kern sie liefert, darin nach Nummer, dann Name —, mit den Kennwerten
        /// in Kurzform. Leer ohne Katalogtabellen.
        /// </summary>
        internal static IReadOnlyList<KonditionierungProfilwahl> Profilwahl()
        {
            if (!RaumnutzungCtrl.Lesbar()) return Array.Empty<KonditionierungProfilwahl>();
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungTexte t = Texte();
            var liste = new List<KonditionierungProfilwahl>();
            foreach (RaumnutzungCtrl.Kategorie k in ctrl.Kategorien())
                foreach (Raumnutzungsprofil p in ctrl.Profile(k.Id))
                    liste.Add(Wahl(k.Bezeichner, p, t));
            return liste;
        }

        /// <summary>Ein Profil als Eintrag der Liste „Nutzungsprofil übernehmen…".</summary>
        internal static KonditionierungProfilwahl Wahl(string kategorie, Raumnutzungsprofil p, RaumnutzungTexte t)
            => new KonditionierungProfilwahl(p.Id, kategorie ?? "", p.Nummer ?? "", p.Bezeichner ?? "",
                                             p.IstLeer ? t.TextOhneWerteKurz : Kurzform(p, t), p.IstLeer);

        /// <summary>
        /// <b>Die Kennwerte in Kurzform</b>: Wochentage, Nutzungszeit, Heizsollwert, Außenluft, Geräte und Personen, soweit
        /// belegt, durch „ · " getrennt („Mo–Fr · 7–18 h · 21 °C · 4 m³/(h·m²) · 10 W/m²"). Ein Profil nur mit Zeilenbild oder
        /// Stundenprofil nennt, was es an Kennwerten trägt; leer = keiner.
        /// </summary>
        internal static string Kurzform(Raumnutzungsprofil p, RaumnutzungTexte t)
        {
            if (p == null) return "";
            CultureInfo c = CultureInfo.CurrentCulture;
            var teile = new List<string>();
            string tage = Wochentage(p.Nutzungstage_Woche, t);
            if (tage.Length > 0)
            {
                teile.Add(tage);
                teile.Add(Raumnutzungsgenerator.Nutzungstage(p).OhneFerien.ToString(c) + " d");
            }
            if (p.Nutzung_Von.HasValue && p.Nutzung_Bis.HasValue)
                teile.Add(p.Nutzung_Von.Value.ToString(c) + "–" + p.Nutzung_Bis.Value.ToString(c) + " h");
            if (p.Heiz_Soll.HasValue) teile.Add(p.Heiz_Soll.Value.ToString("0.#", c) + " °C");
            if (p.Aussenluft.HasValue)
                teile.Add(p.Aussenluft.Value.ToString("0.##", c) + " " +
                          (string.Equals(p.Aussenluft_Einheit, RaumnutzungSchema.EINHEIT_JE_FLAECHE, StringComparison.Ordinal)
                              ? "m³/(h·m²)" : "1/h"));
            double licht = (p.Beleuchtung_Leistung ?? 0.0) * (p.Beleuchtung_Anteil ?? 1.0);
            if (p.Geraete_Leistung.HasValue || p.Beleuchtung_Leistung.HasValue)
                teile.Add(((p.Geraete_Leistung ?? 0.0) + licht).ToString("0.#", c) + " W/m²");
            if (p.Personen_Flaeche.HasValue) teile.Add(p.Personen_Flaeche.Value.ToString("0.#", c) + " m²/P");
            return string.Join(" · ", teile);
        }

        /// <summary>Die Nutzungstage „1111100" als „Mo–Fr", Läufe mit Bindestrich, sonst mit Komma; leer = ohne Angabe.</summary>
        private static string Wochentage(string woche, RaumnutzungTexte t)
        {
            if (woche == null || woche.Length != 7) return "";
            string[] namen = (t.TageKurz ?? "").Split(',');
            if (namen.Length != 7) return "";
            var laeufe = new List<string>();
            int i = 0;
            while (i < 7)
            {
                if (woche[i] != '1') { i++; continue; }
                int j = i;
                while (j + 1 < 7 && woche[j + 1] == '1') j++;
                laeufe.Add(j == i ? namen[i].Trim() : j == i + 1 ? namen[i].Trim() + ", " + namen[j].Trim()
                                                    : namen[i].Trim() + "–" + namen[j].Trim());
                i = j + 1;
            }
            return string.Join(", ", laeufe);
        }

        /// <summary>
        /// <b>Die Zeile der Nutzungstage</b> (E93): „Nutzungstage im Jahr: 252 (aus Wochenmuster und Feiertagen)" — mit den
        /// Ferien eines Ziels „…, abzüglich 22 Ferientage = 230". Abgeleitet im Kern
        /// (<see cref="Raumnutzungsgenerator.Nutzungstage"/>), keine Eingabe.
        /// </summary>
        /// <param name="p">Das Profil.</param>
        /// <param name="ziel">Die Ferienzeiträume des Ziels (die des Gebäudes); <c>null</c> = ohne Ziel.</param>
        /// <param name="t">Die Texte.</param>
        /// <param name="referenzjahr">Das Bezugsjahr; <c>null</c> = die Vorgabe der Konditionierung.</param>
        internal static string Nutzungstagezeile(Raumnutzungsprofil p, Matrixeingang ziel, RaumnutzungTexte t,
                                                 int? referenzjahr = null)
        {
            if (p == null) return "";
            Raumnutzungstage n = Raumnutzungsgenerator.Nutzungstage(p, ziel,
                referenzjahr ?? Konditionierungsarbeitsstand.BEZUGSJAHR_VORGABE);
            CultureInfo c = CultureInfo.CurrentCulture;
            return n.Ferientage > 0
                ? string.Format(c, t.TextNutzungstageFerien, n.OhneFerien.ToString(c), n.Ferientage.ToString(c), n.Tage.ToString(c))
                : string.Format(c, t.TextNutzungstage, n.OhneFerien.ToString(c));
        }

        // =================================================================
        //  Übersetzung der Kennungen
        // =================================================================

        /// <summary>Die Kennung der Kategorieart des Kerns als Aufzählungswert der Oberfläche.</summary>
        internal static RaumnutzungArt Art(string art)
        {
            if (art == RaumnutzungSchema.ART_DIN_V_18599_10) return RaumnutzungArt.Din18599;
            if (art == RaumnutzungSchema.ART_SIA_2024) return RaumnutzungArt.Sia2024;
            if (art == RaumnutzungSchema.ART_VDI_2078) return RaumnutzungArt.Vdi2078;
            if (art == RaumnutzungSchema.ART_EPOS_MUSTER) return RaumnutzungArt.EposMuster;
            return RaumnutzungArt.Eigen;
        }

        /// <summary>Die Zuordnungsart des Kerns als Aufzählungswert der Oberfläche.</summary>
        internal static RaumnutzungZuordnungsart Zuordnungsart(string art)
        {
            if (art == RaumnutzungSchema.ZUORDNUNG_IFC) return RaumnutzungZuordnungsart.IfcKlasse;
            if (art == RaumnutzungSchema.ZUORDNUNG_HOTTCAD) return RaumnutzungZuordnungsart.HottcadRaumtyp;
            return RaumnutzungZuordnungsart.DinNummer;
        }

        /// <summary>Die Zuordnungsart der Oberfläche als Kennung des Kerns.</summary>
        internal static string Kern(RaumnutzungZuordnungsart art) => art switch
        {
            RaumnutzungZuordnungsart.IfcKlasse => RaumnutzungSchema.ZUORDNUNG_IFC,
            RaumnutzungZuordnungsart.HottcadRaumtyp => RaumnutzungSchema.ZUORDNUNG_HOTTCAD,
            _ => RaumnutzungSchema.ZUORDNUNG_DIN,
        };

        private static RaumnutzungErgebnis Ergebnis(RaumnutzungCtrl.Ergebnis e)
            => e.Ok ? RaumnutzungErgebnis.MitId(e.Id) : RaumnutzungErgebnis.Fehler(e.Meldung);

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
