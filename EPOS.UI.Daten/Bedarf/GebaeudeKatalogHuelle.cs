using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Gebäude-KATALOGEDITORS (<c>GebaeudeKatalogDialog</c>) — seit
    /// Stufe G1 in <c>EPOS.UI.Daten</c> (Umsetzungskonzept Gebäudesimulation 2.8, Entscheid
    /// E27/A10). Sie baut den Parametersatz aus dem Kern und führt den EINEN Schreibweg
    /// (E27/U1) aus; ein eigenes Fenster hat der Editor nicht — er erscheint als
    /// Überlagerung im Gebäudedialog.
    ///
    /// <para><b>Die Ableitungen des Vorläufers stehen hier</b>: <c>Bewohner</c>,
    /// <c>gesamte_Fensterflaeche</c> (Süd + Ost/West + Nord) und die Nutzfläche entstehen
    /// beim Schreiben; was keine Maske anfasst (<c>ID</c>, <c>spez_Waermeverbrauch</c>,
    /// <c>Waermebedarf</c>, die drei G2-Spalten), bleibt aus dem geladenen Satz
    /// erhalten.</para>
    ///
    /// <para><b>NULL-erhaltend.</b> Die zwölf Felder der VDI-Struktur gehen so in den Kern,
    /// wie der Dialog sie liefert — ein leeres Feld bleibt NULL, und der Katalogschreibweg
    /// (<c>GebaeudeStammCtrl.Insert</c>/<c>Overwrite</c>) schreibt NULL.</para>
    ///
    /// <para><b>Die ReadOnly-Sperre prüft die HÜLLE</b>, nicht der Controller —
    /// <c>Overwrite</c> meldete sie über <c>Meldung.Hinweis</c>, und das wäre in einer
    /// WebView ein modaler Kasten über dem Dialog.</para>
    /// </summary>
    internal static class GebaeudeKatalogHuelle
    {
        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>; die Überlagerung im
        /// Gebäudedialog setzt ihn selbst.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            string bezeichner, GebaeudeKatalogModus modus)
        {
            GebaeudeModel geladen = modus == GebaeudeKatalogModus.Neu
                ? new GebaeudeModel()
                : Laden(bezeichner) ?? new GebaeudeModel();

            // Die Brauchwasser-Zuordnungen des laufenden Projekts. Sie werden erst beim
            // Oeffnen der Ueberlagerung gelesen und bei OK zurueckgeschrieben - zusammen mit
            // dem Arbeitsstand des Zapfprofils (Behaelter je Oeffnen, 5.2).
            var brauchwasser = new List<Z_ProjektBrauchwasserModel>();
            var zapfprofil = new ZapfprofilBehaelter[1];

            return new Dictionary<string, object>
            {
                ["Daten"] = AusModell(geladen),
                ["Modus"] = modus,

                ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudetypen()),
                ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudearten(null)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["Katalognamen"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Katalognamen()),
                ["Lies"] = new Func<string, GebaeudeKatalogDaten>(
                    n => { GebaeudeModel m = Laden(n); return m == null ? null : AusModell(m); }),
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    (d, istNeu, bez) => Schreiben(d, istNeu, bez)),

                // "Brauchwasser..." auf dem zweiten Reiter zeigt die Brauchwasser-Profilliste
                // des LAUFENDEN Projekts als Ueberlagerung - nur, wo die Schale den Weg
                // eingehaengt hat (Gebaeudewege).
                ["BrauchwasserGaben"] = Gebaeudewege.BrauchwasserGaben == null
                    ? null
                    : new Func<IReadOnlyDictionary<string, object>>(() => BrauchwasserGaben(brauchwasser, zapfprofil)),
                ["BrauchwasserFertig"] = new Action<bool>(
                    ok => BrauchwasserSchreiben(ok, brauchwasser, zapfprofil[0])),

                ["Texte"] = Texte(),
                ["TitelText"] = Titel(),

                ["GruppeKopf"] = Text_("GEBK_GRP_KENNGROESSEN", "Kenngrößen"),
                ["GruppeRaumtemperaturen"] = Text_("GEBK_GRP_RAUMTEMPERATUREN", "Raumtemperaturen"),
                ["GruppeFerienAnfang"] = Text_("GEBK_GRP_FERIEN_ANFANG", "Ferien Anfang"),
                ["GruppeFerienEnde"] = Text_("GEBK_GRP_FERIEN_ENDE", "Ferien Ende"),
                ["GruppeSonstiges"] = Text_("GEBK_GRP_SONSTIGES", "Sonstiges"),

                ["LabelName"] = Text_("GEBK_LBL_NAME", "Name :"),
                ["LabelGebaeudetyp"] = Text_("GEBK_LBL_GEBAEUDETYP", "Gebäudetyp :"),
                ["LabelBeschreibung"] = Text_("GEBK_LBL_BESCHREIBUNG", "Beschreibung :"),
                ["LabelGebaeudeart"] = Text_("GEBK_LBL_GEBAEUDEART", "Gebäudeart :"),
                ["LabelBaujahr"] = Text_("GEBK_LBL_BAUJAHR", "Baujahr :"),
                ["LabelVerwendung"] = Text_("GEBK_LBL_VERWENDUNG", "Verwendung :"),
                ["LabelBauart"] = Text_("GEBK_LBL_BAUART", "Bauart :"),
                ["LabelWohnflaeche"] = Text_("GEBK_LBL_WOHNFLAECHE", "Nutzfläche :"),
                ["LabelFlaecheNutzer"] = Text_("GEBK_LBL_FLAECHE_NUTZER", "Fläche / Nutzer :"),
                ["LabelWaermegewinne"] = Text_("GEBK_LBL_WAERMEGEWINNE", "Interne Wärmegewinne :"),
                ["LabelFensterdurchlassgrad"] =
                    Text_("GEBK_LBL_FENSTERDURCHLASS", "Fensterdurchlaßgrad :"),
                ["HinweisFensterdurchlassgrad"] = Text_("GEBK_HINWEIS_FENSTERDURCHLASS", "(z.B. 0,4)"),
                ["LabelRaumhoehe"] = Text_("GEBK_LBL_RAUMHOEHE", "Raumhöhe :"),
                ["LabelLuftwechsel"] = Text_("GEBK_LBL_LUFTWECHSEL", "Luftwechselrate :"),

                ["LabelFFNord"] = Text_("GEBK_LBL_FF_NORD", "Fensterfläche Nord :"),
                ["LabelFFSued"] = Text_("GEBK_LBL_FF_SUED", "Fensterfläche Süd :"),
                ["LabelFFOstWest"] = Text_("GEBK_LBL_FF_OSTWEST", "Fensterfläche Ost + West :"),

                ["LabelSollTag"] = Text_("GEBK_LBL_SOLL_TAG", "Soll am Tag :"),
                ["LabelNachtAbsenkung"] = Text_("GEBK_LBL_NACHTABSENKUNG", "Nachtabsenkung auf :"),
                ["LabelMaxTemperatur"] = Text_("GEBK_LBL_MAXTEMPERATUR", "Maximalraumtemperatur :"),
                ["LabelWEAbsenkung"] = Text_("GEBK_LBL_WE_ABSENKUNG", "Wochenendabsenkung :"),
                ["LabelSollFerien"] = Text_("GEBK_LBL_SOLL_FERIEN", "Soll in Ferien :"),
                ["LabelTag"] = Text_("GEBK_LBL_TAG", "Tag :"),
                ["LabelMonat"] = Text_("GEBK_LBL_MONAT", "Monat :"),
                ["LabelBrauchwasserprofile"] =
                    Text_("GEBK_LBL_BRAUCHWASSERPROFILE", "Brauchwasserprofile :"),

                ["Ferienzeitraeume"] = Ferienzeitraeume(),
                ["Bauarten"] = Bauarten(),
                ["Verwendungen"] = Verwendungen(),
                ["Verwendungswerte"] = VERWENDUNGSWERTE,

                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["BtnSpeichernUnterText"] = Text_("GEBK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnBrauchwasserText"] = Text_("GEBK_BTN_BRAUCHWASSER", "Brauchwasser..."),

                ["MeldungZahlFehlt"] = Text_("GEBK_MSG_ZAHL", "Bitte {0} als Zahl eingeben."),
                ["MeldungNameFehlt"] = Text_("GEBK_MSG_NAME_LEER", "Gebäudenamen eingeben!"),
                ["MeldungFerienWinter"] = Text_(Ferienzeit.MELDUNG_WINTER,
                    "Die Ferien müssen über die Jahresgrenze gehen!"),
                ["MeldungFerienOstern"] = Text_(Ferienzeit.MELDUNG_OSTERN,
                    "Fehler: Bei der Eingabe der Osterferien!"),
                ["MeldungFerienSommer"] = Text_(Ferienzeit.MELDUNG_SOMMER,
                    "Fehler: Bei der Eingabe der Sommerferien!"),
                ["MeldungFerienHerbst"] = Text_(Ferienzeit.MELDUNG_HERBST,
                    "Fehler: Bei der Eingabe der Herbstferien!"),

                ["FeldWohnflaeche"] = Text_("GEBK_FELD_WOHNFLAECHE", "Nutzfläche"),
                ["FeldFlaecheNutzer"] = Text_("GEBK_FELD_FLAECHE_NUTZER", "Fläche / Nutzer"),
                ["FeldWaermegewinne"] = Text_("GEBK_FELD_WAERMEGEWINNE", "Interne Wärmegewinne"),
                ["FeldFensterdurchlassgrad"] =
                    Text_("GEBK_FELD_FENSTERDURCHLASS", "Fensterdurchlaßgrad"),
                ["FeldRaumhoehe"] = Text_("GEBK_FELD_RAUMHOEHE", "Raumhöhe"),
                ["FeldFFSued"] = Text_("GEBK_FELD_FF_SUED", "Fensterfläche Süd"),
                ["FeldFFNord"] = Text_("GEBK_FELD_FF_NORD", "Fensterfläche Nord"),
                ["FeldFlaecheAussenwand"] = Text_("GEBK_FELD_FL_AUSSENWAND", "Fläche Außenwand"),
                ["FeldDachflaeche"] = Text_("GEBK_FELD_DACHFLAECHE", "Gebäude Dachfläche"),
                ["FeldGrundflaeche"] = Text_("GEBK_FELD_GRUNDFLAECHE", "Gebäude Grundfläche"),
                ["FeldSonstigeFlaechen"] = Text_("GEBK_FELD_SONST_FLAECHEN", "sonstige Flächen"),
                ["FeldUAussenwand"] = Text_("GEBK_FELD_U_AUSSENWAND", "U-Wert Außenwand"),
                ["FeldUFenster"] = Text_("GEBK_FELD_U_FENSTER", "U-Wert Fenster"),
                ["FeldUDachflaeche"] = Text_("GEBK_FELD_U_DACHFLAECHE", "U-Wert Dachfläche"),
                ["FeldUGrundflaeche"] = Text_("GEBK_FELD_U_GRUNDFLAECHE", "U-Wert Grundfläche"),
                ["FeldUSonstiges"] = Text_("GEBK_FELD_U_SONSTIGES", "U-Wert Sonstiges"),

                ["HilfeSchluessel"] = "Form_Gebaeude1.btn_Help"
            };
        }

        /// <summary>Das Textbündel der VDI-6007-Struktur — der Rückfall ist der Vorgabewert des Bündels.</summary>
        internal static GebaeudeHuelleTexte Texte()
        {
            var t = new GebaeudeHuelleTexte();
            t.ReiterHuelle = Text_("GEBK_REITER_HUELLE", t.ReiterHuelle);
            t.ReiterTemperaturen = Text_("GEBK_REITER_TEMPERATUREN", t.ReiterTemperaturen);
            t.GruppeHuelle = Text_("GEBK_GRP_HUELLE", t.GruppeHuelle);
            t.GruppeLeitwerte = Text_("GEBK_GRP_LEITWERTE", t.GruppeLeitwerte);
            t.GruppeFenster = Text_("GEBK_GRP_FENSTER_ORIENTIERUNG", t.GruppeFenster);
            t.GruppeModellparameter = Text_("GEBK_GRP_MODELLPARAMETER", t.GruppeModellparameter);
            t.GruppeTagesbilanz = Text_("GEBK_GRP_TAGESBILANZ", t.GruppeTagesbilanz);

            t.SpalteBauteil = Text_("GEBK_SP_BAUTEIL", t.SpalteBauteil);
            t.SpalteKennwert = Text_("GEBK_SP_U_PSI", t.SpalteKennwert);
            t.SpalteGroesse = Text_("GEBK_SP_A_L", t.SpalteGroesse);
            t.SpalteRandbedingung = Text_("GEBK_SP_RANDBEDINGUNG", t.SpalteRandbedingung);
            t.SpalteLeitwert = Text_("GEBK_SP_UA", t.SpalteLeitwert);
            t.ZeileAussenwand = Text_("GEBK_ZEILE_AUSSENWAND", t.ZeileAussenwand);
            t.ZeileFenster = Text_("GEBK_ZEILE_FENSTER", t.ZeileFenster);
            t.ZeileDach = Text_("GEBK_ZEILE_DACH", t.ZeileDach);
            t.ZeileBodenplatte = Text_("GEBK_ZEILE_BODENPLATTE", t.ZeileBodenplatte);
            t.ZeileSonstiges = Text_("GEBK_ZEILE_SONSTIGES", t.ZeileSonstiges);
            t.ZeileWbFenster = Text_("GEBK_ZEILE_WB_FENSTER", t.ZeileWbFenster);
            t.ZeileWbKeller = Text_("GEBK_ZEILE_WB_KELLER", t.ZeileWbKeller);
            t.ZeileWbDach = Text_("GEBK_ZEILE_WB_DACH", t.ZeileWbDach);
            t.RandAussenluft = Text_("GEBK_RAND_AUSSENLUFT", t.RandAussenluft);
            t.RandErdreich = Text_("GEBK_RAND_ERDREICH", t.RandErdreich);
            t.RandKeller = Text_("GEBK_RAND_KELLER", t.RandKeller);
            t.LabelKellertemperatur = Text_("GEBK_LBL_KELLERTEMPERATUR", t.LabelKellertemperatur);
            t.HinweisFensterflaeche = Text_("GEBK_HINWEIS_FENSTER_SUMME", t.HinweisFensterflaeche);

            t.LabelHT = Text_("GEBK_LBL_HT", t.LabelHT);
            t.LabelHVe = Text_("GEBK_LBL_HVE", t.LabelHVe);
            t.LabelHGes = Text_("GEBK_LBL_HGES", t.LabelHGes);
            t.LabelHTGewichtet = Text_("GEBK_LBL_HT_GEWICHTET", t.LabelHTGewichtet);
            t.HinweisGewichte = Text_("GEBK_HINWEIS_GEWICHTE", t.HinweisGewichte);
            t.HinweisLueftung = Text_("GEBK_HINWEIS_LUEFTUNG", t.HinweisLueftung);

            t.LabelFFOst = Text_("GEBK_LBL_FF_OST", t.LabelFFOst);
            t.LabelFFWest = Text_("GEBK_LBL_FF_WEST", t.LabelFFWest);
            t.LabelFFSummeOstWest = Text_("GEBK_LBL_FF_SUMME_OW", t.LabelFFSummeOstWest);
            t.LabelFFGesamt = Text_("GEBK_LBL_FF_GESAMT", t.LabelFFGesamt);
            t.FeldFFOst = Text_("GEBK_FELD_FF_OST", t.FeldFFOst);
            t.FeldFFWest = Text_("GEBK_FELD_FF_WEST", t.FeldFFWest);

            t.LabelRahmenanteil = Text_("GEBK_LBL_RAHMENANTEIL", t.LabelRahmenanteil);
            t.LabelVerschattung = Text_("GEBK_LBL_VERSCHATTUNG", t.LabelVerschattung);
            t.LabelMasseanteil = Text_("GEBK_LBL_MASSEANTEIL", t.LabelMasseanteil);
            t.LabelInnenflaechenfaktor = Text_("GEBK_LBL_INNENFLAECHENFAKTOR", t.LabelInnenflaechenfaktor);
            t.LabelHeizungStrahlung = Text_("GEBK_LBL_HEIZUNG_STRAHLUNG", t.LabelHeizungStrahlung);
            t.LabelHeizleistungMax = Text_("GEBK_LBL_HEIZLEISTUNG_MAX", t.LabelHeizleistungMax);
            t.LabelAussenStrahlung = Text_("GEBK_LBL_AUSSEN_STRAHLUNG", t.LabelAussenStrahlung);
            t.LabelInfiltration = Text_("GEBK_LBL_INFILTRATION", t.LabelInfiltration);
            t.LabelNutzerlueftung = Text_("GEBK_LBL_NUTZERLUEFTUNG", t.LabelNutzerlueftung);
            t.LabelSommerlueftung = Text_("GEBK_LBL_SOMMERLUEFTUNG", t.LabelSommerlueftung);
            t.HinweisLuftwechsel = Text_("GEBK_HINWEIS_LUFTWECHSEL", t.HinweisLuftwechsel);
            t.HerkunftInfiltrationNutzer = Text_("GEBK_HERKUNFT_INFILTRATION_NUTZER", t.HerkunftInfiltrationNutzer);
            t.HerkunftLuftwechselrate = Text_("GEBK_HERKUNFT_LUFTWECHSELRATE", t.HerkunftLuftwechselrate);
            t.HerkunftVorgabe = Text_("GEBK_HERKUNFT_VORGABE", t.HerkunftVorgabe);
            t.HinweisSommerlueftung = Text_("GEBK_HINWEIS_SOMMERLUEFTUNG", t.HinweisSommerlueftung);
            t.VorgabeFormat = Text_("GEBK_VORGABE", t.VorgabeFormat);
            t.VorgabeUnbegrenzt = Text_("GEBK_VORGABE_UNBEGRENZT", t.VorgabeUnbegrenzt);
            t.HinweisModellparameter = Text_("GEBK_HINWEIS_MODELLPARAMETER", t.HinweisModellparameter);

            t.LabelRechenweg = Text_("GEBK_LBL_RECHENWEG", t.LabelRechenweg);
            t.RechenwegVdi6007 = Text_("GEBK_RECHENWEG_VDI6007", t.RechenwegVdi6007);
            t.RechenwegTagesbilanz = Text_("GEBK_RECHENWEG_TAGESBILANZ", t.RechenwegTagesbilanz);
            t.ZeileRechenwegVdi6007 = Text_("GEBK_ZEILE_RECHENWEG_VDI6007", t.ZeileRechenwegVdi6007);
            t.ZeileRechenwegTagesbilanz = Text_("GEBK_ZEILE_RECHENWEG_TAGESBILANZ", t.ZeileRechenwegTagesbilanz);
            t.ZeileRechenwegVorgabe = Text_("GEBK_ZEILE_RECHENWEG_VORGABE", t.ZeileRechenwegVorgabe);

            t.HinweisSpeichernUnter = Text_("GEBK_HINWEIS_SPEICHERN_UNTER", t.HinweisSpeichernUnter);

            t.MeldungUngueltig = Text_("GEBK_MSG_UNGUELTIG", t.MeldungUngueltig);
            t.MeldungNutzflaeche = Text_("GEBK_MSG_NUTZFLAECHE", t.MeldungNutzflaeche);
            t.MeldungFlaecheNutzer = Text_("GEBK_MSG_FLAECHE_NUTZER", t.MeldungFlaecheNutzer);
            t.MeldungRaumhoehe = Text_("GEBK_MSG_RAUMHOEHE", t.MeldungRaumhoehe);
            t.MeldungLuftwechsel = Text_("GEBK_MSG_LUFTWECHSEL", t.MeldungLuftwechsel);
            t.MeldungGWert = Text_("GEBK_MSG_G_WERT", t.MeldungGWert);
            t.MeldungUBereich = Text_("GEBK_MSG_U_BEREICH", t.MeldungUBereich);
            t.MeldungBauweise = Text_("GEBK_MSG_BAUWEISE", t.MeldungBauweise);
            t.MeldungRRest = Text_("GEBK_MSG_RREST", t.MeldungRRest);
            t.MeldungOstWest = Text_("GEBK_MSG_OST_WEST", t.MeldungOstWest);
            return t;
        }

        // =================================================================================
        // Datenseite
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Brauchwasser-Profilliste. Die Zuordnungen des laufenden
        /// Projekts werden hier frisch gelesen — der Vorläufer tat dasselbe beim Klick.
        /// </summary>
        private static IReadOnlyDictionary<string, object> BrauchwasserGaben(
            List<Z_ProjektBrauchwasserModel> ziel, ZapfprofilBehaelter[] zapfprofil)
        {
            int projektId = Dienste.Projekt.Id;
            zapfprofil[0] = new ZapfprofilBehaelter(projektId);

            ziel.Clear();
            ziel.AddRange(Z_ProjektBrauchwasserCtrl.LiesProjekt(projektId));

            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektBrauchwasserModel m in ziel)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser,
                    Name = m.szBezeichner ?? "", Summe = m.Summe
                });

            Action geaendert = () =>
            {
                ziel.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    ziel.Add(new Z_ProjektBrauchwasserModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Brauchwasser = z.IdStamm,
                        szBezeichner = z.Name, Summe = z.Summe
                    });
            };

            return Gebaeudewege.BrauchwasserGaben?.Invoke(projektId, zeilen, geaendert, zapfprofil[0]);
        }

        /// <summary>
        /// Nach OK wird die Zuordnung geschrieben — Löschen + Neuanlegen samt
        /// Änderungsdatum (<c>btn_Brauchwasser_Click</c>:246-254), und im SELBEN Vorgang der
        /// Arbeitsstand des Zapfprofils (5.2). Der Katalog führt keinen Arbeitsstand: geschrieben
        /// wird sofort. Eine Ablehnung des Zapfprofils rollt alles zurück und nennt ihren Grund.
        /// </summary>
        private static void BrauchwasserSchreiben(bool ok, List<Z_ProjektBrauchwasserModel> liste,
                                                  ZapfprofilBehaelter zapfprofil)
        {
            if (!ok) return;

            int projektId = Dienste.Projekt.Id;
            string projektName = Dienste.Projekt.Name;

            ZapfprofilSpeicherergebnis e = ZapfprofilHuelle.BrauchwasserSchreiben(projektId, liste, zapfprofil);
            if (!e.Erfolg)
            {
                // Die Meldung laeuft HINTER dem Ereignis (Hausregel: kein synchrones
                // Plattformfenster aus einem Rueckruf der Oberflaeche).
                if (e.Meldung != null) _ = Dienste.Dialog.WarnungAsync(e.Meldung.Text, Titel());
                return;
            }

            var projctrl = new ProjektCtrl();
            projctrl.ReadSingle(projektName);
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();
        }

        /// <summary>Ein Katalogsatz nach Bezeichner — über den Kern-Controller, mit Parameter.</summary>
        internal static GebaeudeModel Laden(string bezeichner)
            => new GebaeudeStammCtrl().Lies(bezeichner);

        /// <summary>
        /// Der EINE Schreibweg des Editors samt ReadOnly-Sperre und Namensprobe. Angelegt
        /// wird nur unter einem freien Namen; überschrieben wird der URSPRUNGSNAME.
        /// </summary>
        internal static GebaeudeKatalogErgebnis Schreiben(
            GebaeudeKatalogDaten daten, bool istNeu, string bezeichner)
        {
            var ctrl = new GebaeudeStammCtrl();

            if (istNeu && ctrl.Lies(daten.Name) != null)
                return new GebaeudeKatalogErgebnis(false, Text_("GEBK_MSG_NAME_VERGEBEN",
                    "Ein Gebäude mit diesem Namen steht schon im Katalog."));

            if (!istNeu && ctrl.IsReadOnly(bezeichner))
                return new GebaeudeKatalogErgebnis(false, Text_("GEBK_MSG_READONLY",
                    "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht " +
                    "überschrieben werden."));

            GebaeudeModel vorher = istNeu ? new GebaeudeModel() : Laden(bezeichner) ?? new GebaeudeModel();
            GebaeudeModel modell = NachModell(daten, vorher);

            // Ueberschreiben trifft den URSPRUNGSNAMEN (WHERE Bezeichner = Gebaeudename).
            if (!istNeu) modell.Gebaeudename = bezeichner;

            bool ok = istNeu ? ctrl.Insert(modell) : ctrl.Overwrite(modell);
            return new GebaeudeKatalogErgebnis(ok,
                ok ? "" : Text_("GEBK_MSG_FEHLER", "Fehler beim Speichern!\nAlle Eingaben überprüfen!"));
        }

        /// <summary>Katalogsatz → Feldsatz.</summary>
        internal static GebaeudeKatalogDaten AusModell(GebaeudeModel m)
        {
            var d = new GebaeudeKatalogDaten
            {
                Name = m.Gebaeudename ?? "",
                Typ = m.Typ ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Gebaeudeart = m.Gebaeudeart ?? "",
                Verwendung = string.IsNullOrEmpty(m.Wohngebaeude_Nicht_Wohngebaeude)
                    ? VERWENDUNGSWERTE[0] : m.Wohngebaeude_Nicht_Wohngebaeude,
                Baualtersklasse = GebaeudeStammCtrl.KlassenIndex(m.Baualtersklasse),
                // W9-O-2: Die Bauart bleibt die ANZEIGE der gespeicherten Bauweise.
                Bauart = GebaeudeStammCtrl.BauartAusBauweise(m.Bauweise, m.Nutzflaeche),
                Bauweise = m.Bauweise,

                WohnflaecheGesamt = m.Wohnflaeche_gesamt,
                FlaecheNutzer = m.Flaeche_Nutzer,
                Waermegewinne = m.Interne_Waermegewinne,
                Fensterdurchlassgrad = m.Fensterdurchlassgrad,
                Raumhoehe = m.Raumhoehe,

                FensterflaecheNord = m.Fensterflaeche_Nord,
                FensterflaecheSued = m.Fensterflaeche_Sued,
                FensterflaecheOstWest = m.Fensterflaeche_OstWest,
                FlaecheAussenwand = m.Flaeche_Außenwand,
                Dachflaeche = m.Dachflaeche,
                Grundflaeche = m.Grundflaeche,
                SonstigeFlaechen = m.Sonstige_Flaechen,

                UWertAussenwand = m.k_Wert_Außenwand,
                UWertFenster = m.k_Wert_Fenster,
                UWertDachflaeche = m.k_Wert_Dachflaeche,
                UWertGrundflaeche = m.k_Wert_Grundflaeche,
                UWertSonstiges = m.k_Wert_Sonstiges,

                SollTag = m.Raumsolltemperatur_Tag,
                NachtAbsenkung = m.Raumsolltemperatur_Nachtabsenkung,
                MaxTemperatur = m.Maximaleraumtemperatur,
                WochenendAbsenkung = m.Raumsolltemperatur_Wochenende,
                SollFerien = m.Raumsolltemperatur_Ferien,

                WbvkFensterWand = m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                WbvkAussenwandKeller = m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke,
                WbvkWandDach = m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach,

                AnschlussFensterWand = m.Abmessung_Anschluß_Fenster_Wand,
                AnschlussWandDach = m.Abmessung_Anschluß_Wand_Dach,
                AnschlussAussenwandKeller = m.Abmessung_Anschluß_Außenwand_Kellerdecke,

                Luftwechselrate = m.Luftwechselrate,
                Wochenende = m.Wochenende,
                Ferien = m.Ferien,
                WwBedarf = m.WW_Bedarf,
                SpezWaermeverbrauch = m.spez_Waermeverbrauch,
                Waermebedarf = m.Waermebedarf,

                // Stufe G1: die zwoelf Felder der VDI-Struktur - NULL bleibt null.
                Modell = m.Gebaeude_Modell,
                GrundflaecheRandbedingung = m.Grundflaeche_Randbedingung,
                Kellertemperatur = m.Kellertemperatur,
                FensterflaecheOst = m.Fensterflaeche_Ost,
                FensterflaecheWest = m.Fensterflaeche_West,
                Rahmenanteil = m.Rahmenanteil,
                Verschattungsfaktor = m.Verschattungsfaktor,
                MasseanteilAussen = m.Masseanteil_Aussen,
                Innenflaechenfaktor = m.Innenflaechenfaktor,
                HeizungStrahlungsanteil = m.Heizung_Strahlungsanteil,
                HeizleistungMax = m.Heizleistung_Max,
                AussenbauteileStrahlung = m.Aussenbauteile_Strahlung,
                LuftwechselInfiltration = m.Luftwechsel_Infiltration,
                LuftwechselNutzer = m.Luftwechsel_Nutzer,
                Sommerlueftung = m.Sommerlueftung
            };

            d.Ferienbeginn = new[]
            {
                (int)m.Ferienbeginn_1, (int)m.Ferienbeginn_2,
                (int)m.Ferienbeginn_3, (int)m.Ferienbeginn_4
            };
            d.Ferienende = new[]
            {
                (int)m.Ferienende_1, (int)m.Ferienende_2,
                (int)m.Ferienende_3, (int)m.Ferienende_4
            };
            return d;
        }

        /// <summary>
        /// Feldsatz → Katalogsatz samt der Ableitungen des Vorläufers.
        /// <paramref name="vorher"/> ist der GELADENE Satz; alles, was keine Maske anfasst,
        /// bleibt daraus stehen.
        /// </summary>
        internal static GebaeudeModel NachModell(GebaeudeKatalogDaten d, GebaeudeModel vorher)
        {
            GebaeudeModel m = vorher ?? new GebaeudeModel();

            double wfl = d.WohnflaecheGesamt ?? 0;
            double nutzer = d.FlaecheNutzer ?? 0;

            m.Gebaeudename = d.Name ?? "";
            m.Typ = d.Typ ?? "";
            m.Beschreibung = d.Beschreibung ?? "";

            m.Wohnflaeche_gesamt = wfl;

            // "Flaeche_Nutzer == 0 -> 35" und die Bewohnerzahl daraus.
            m.Flaeche_Nutzer = nutzer;
            if (nutzer == 0) { m.Flaeche_Nutzer = 35; nutzer = 35; }
            m.Bewohner = wfl / nutzer;

            m.Interne_Waermegewinne = d.Waermegewinne ?? 0;

            // W9-O-2: Die Bauweise bildet der Dialog; hier wird sie nur uebernommen.
            m.Bauweise = d.Bauweise;

            m.Fensterflaeche_Sued = d.FensterflaecheSued ?? 0;
            m.Fensterflaeche_OstWest = d.FensterflaecheOstWest ?? 0;
            m.Fensterflaeche_Nord = d.FensterflaecheNord ?? 0;
            m.Fensterdurchlassgrad = d.Fensterdurchlassgrad ?? 0;

            m.k_Wert_Außenwand = d.UWertAussenwand ?? 0;
            m.k_Wert_Fenster = d.UWertFenster ?? 0;
            m.k_Wert_Dachflaeche = d.UWertDachflaeche ?? 0;
            m.k_Wert_Grundflaeche = d.UWertGrundflaeche ?? 0;
            m.k_Wert_Sonstiges = d.UWertSonstiges ?? 0;
            m.Flaeche_Außenwand = d.FlaecheAussenwand ?? 0;
            // Die gesamte Fensterflaeche ist GERECHNET: Sued + (Ost + West) + Nord
            // (Konzept 2.6) - dieselbe Summe, die der Dialog zeigt.
            m.gesamte_Fensterflaeche = m.Fensterflaeche_Sued + m.Fensterflaeche_OstWest +
                                       m.Fensterflaeche_Nord;
            m.Dachflaeche = d.Dachflaeche ?? 0;
            m.Grundflaeche = d.Grundflaeche ?? 0;
            m.Sonstige_Flaechen = d.SonstigeFlaechen ?? 0;
            m.Nutzflaeche = wfl;
            m.Raumhoehe = d.Raumhoehe ?? 0;

            m.Baualtersklasse = GebaeudeStammCtrl.KlassenBuchstabe(d.Baualtersklasse).ToString();
            m.Gebaeudeart = d.Gebaeudeart ?? "";
            m.Wohngebaeude_Nicht_Wohngebaeude = d.Verwendung ?? VERWENDUNGSWERTE[0];

            // Reiter 2 - die Ableitungen hat die Komponente im OK-Weg gemacht.
            m.Raumsolltemperatur_Tag = d.SollTag ?? 0;
            m.Raumsolltemperatur_Nachtabsenkung = d.NachtAbsenkung ?? 0;
            m.Maximaleraumtemperatur = d.MaxTemperatur ?? 0;
            m.Raumsolltemperatur_Wochenende = d.WochenendAbsenkung ?? 0;
            m.Raumsolltemperatur_Ferien = d.SollFerien ?? 0;
            m.Wochenende = d.Wochenende;
            m.Ferien = d.Ferien;

            m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = d.WbvkFensterWand ?? 0;
            m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke =
                d.WbvkAussenwandKeller ?? 0;
            m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = d.WbvkWandDach ?? 0;
            m.Abmessung_Anschluß_Fenster_Wand = d.AnschlussFensterWand ?? 0;
            m.Abmessung_Anschluß_Wand_Dach = d.AnschlussWandDach ?? 0;
            m.Abmessung_Anschluß_Außenwand_Kellerdecke = d.AnschlussAussenwandKeller ?? 0;

            m.Ferienbeginn_1 = d.Ferienbeginn[0];
            m.Ferienbeginn_2 = d.Ferienbeginn[1];
            m.Ferienbeginn_3 = d.Ferienbeginn[2];
            m.Ferienbeginn_4 = d.Ferienbeginn[3];
            m.Ferienende_1 = d.Ferienende[0];
            m.Ferienende_2 = d.Ferienende[1];
            m.Ferienende_3 = d.Ferienende[2];
            m.Ferienende_4 = d.Ferienende[3];

            m.Luftwechselrate = d.Luftwechselrate ?? 0;
            m.WW_Bedarf = d.WwBedarf;
            m.spez_Waermeverbrauch = d.SpezWaermeverbrauch;
            m.Waermebedarf = d.Waermebedarf;

            // Stufe G1: die zwoelf Felder der VDI-Struktur, NULL-erhaltend.
            m.Gebaeude_Modell = d.Modell;
            m.Grundflaeche_Randbedingung = d.GrundflaecheRandbedingung;
            m.Kellertemperatur = d.Kellertemperatur;
            m.Fensterflaeche_Ost = d.FensterflaecheOst;
            m.Fensterflaeche_West = d.FensterflaecheWest;
            m.Rahmenanteil = d.Rahmenanteil;
            m.Verschattungsfaktor = d.Verschattungsfaktor;
            m.Masseanteil_Aussen = d.MasseanteilAussen;
            m.Innenflaechenfaktor = d.Innenflaechenfaktor;
            m.Heizung_Strahlungsanteil = d.HeizungStrahlungsanteil;
            m.Heizleistung_Max = d.HeizleistungMax;
            m.Aussenbauteile_Strahlung = d.AussenbauteileStrahlung;
            m.Luftwechsel_Infiltration = d.LuftwechselInfiltration;
            m.Luftwechsel_Nutzer = d.LuftwechselNutzer;
            m.Sommerlueftung = d.Sommerlueftung;

            return m;
        }

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>
        /// Die beiden STEUERWERTE der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>. Sie
        /// werden NIE übersetzt (Befund W9‑B8).
        /// </summary>
        internal static readonly string[] VERWENDUNGSWERTE = { "Wohngebaeude", "Nicht Wohngebaeude" };

        private static string[] Verwendungen()
        {
            return new[]
            {
                Text_("GEBK_VERWENDUNG_WOHN", "Wohngebäude"),
                Text_("GEBK_VERWENDUNG_NICHTWOHN", "Nicht Wohngebäude")
            };
        }

        private static string[] Bauarten()
        {
            return new[]
            {
                Text_("GEBK_BAUART_LEICHT", "Leichte Bauart"),
                Text_("GEBK_BAUART_SCHWER", "Schwere Bauart"),
                Text_("GEBK_BAUART_SEHRSCHWER", "Sehr schwere Bauart")
            };
        }

        private static string[] Ferienzeitraeume()
        {
            return new[]
            {
                Text_("GEBK_FERIEN_WINTER", "Winter :"),
                Text_("GEBK_FERIEN_OSTERN", "Ostern :"),
                Text_("GEBK_FERIEN_SOMMER", "Sommer :"),
                Text_("GEBK_FERIEN_HERBST", "Herbst :")
            };
        }

        internal static string Titel() => Text_("GEBK_TITEL", "Gebäudedaten: Flächen, U-Werte");

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
