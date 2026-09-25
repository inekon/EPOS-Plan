using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        ///
        /// <para><b>Stufe G4, Welle 4:</b> <paramref name="vorbelegung"/> bringt den Editor mit
        /// vorbelegten Daten und einer Herleitungszeile hoch (der Gebäudeimport, Modus Neu);
        /// <c>null</c> = der Satz ist bitgleich der bisherige.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            string bezeichner, GebaeudeKatalogModus modus, GebaeudeVorbelegung vorbelegung = null)
        {
            IReadOnlyDictionary<string, object> gaben = Grundgaben(bezeichner, modus);
            if (vorbelegung?.Daten == null) return gaben;
            return new Dictionary<string, object>(gaben)
            {
                ["Daten"] = vorbelegung.Daten.Kopie(),
                ["Vorbelegung"] = vorbelegung.Herleitung ?? ""
            };
        }

        private static IReadOnlyDictionary<string, object> Grundgaben(
            string bezeichner, GebaeudeKatalogModus modus)
        {
            GebaeudeModel geladen = modus == GebaeudeKatalogModus.Neu
                ? new GebaeudeModel()
                : Laden(bezeichner) ?? new GebaeudeModel();
            return Grundgaben(geladen, modus);
        }

        // =================================================================================
        // Stufe G3, Welle D2: das Gebaeude IM PROJEKT - Projektkopie samt Zonen
        // =================================================================================

        /// <summary>Der Hilfeschlüssel der Betriebsart Projekt — Abschnitt „Hülle und Zonen" der Seite Gebäude.</summary>
        internal const string HILFE_PROJEKT = "GebaeudeProjekt.btn_Help";

        /// <summary>
        /// <b>Der Parametersatz des Editors in der Betriebsart Projekt</b> („Hülle und Zonen…" im
        /// Gebäudedialog): bearbeitet wird die PROJEKTKOPIE der Zuordnung <paramref name="idZ"/>
        /// (<c>Tab_Gebaeude</c>), nicht der Katalogsatz, samt ihrer Zonen (<see cref="Zonenweg"/>).
        /// <c>null</c>, wenn die Zuordnung keine Projektkopie hat (eine eben aufgenommene Zeile).
        ///
        /// <para><b>Der Schreibweg</b> „Speichern" trennt nach dem zweiten Parameter: <c>false</c> (OK)
        /// überschreibt die Projektkopie — unter ihrem Namen, die Zeile wird geändert, nie gelöscht
        /// (<see cref="GebaeudeStammCtrl.ProjektkopieUeberschreiben"/>) — und markiert das Projekt als
        /// geändert; <c>true</c> („Speichern unter") legt einen KATALOGSATZ an, wie im Modus Bearbeiten.
        /// Die Zonen schreibt der Zonenweg im dritten Schritt des OK-Wegs.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> ProjektGaben(int idProjekt, int idZ)
        {
            int idGebaeude = GebaeudeBedarfCtrl.TabGebaeudeId(idZ);
            GebaeudeModel kopie = GebaeudeStammCtrl.LiesProjektkopie(idGebaeude);
            if (idProjekt <= 0 || kopie == null) return null;

            var gaben = new Dictionary<string, object>(Grundgaben(kopie, GebaeudeKatalogModus.Projekt))
            {
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    (d, istNeu, bez) => istNeu ? Schreiben(d, true, bez) : ProjektSchreiben(idProjekt, idGebaeude, d)),
                ["Zonen"] = Zonenweg(idProjekt, idZ, idGebaeude),
                ["HilfeSchluessel"] = HILFE_PROJEKT
            };
            gaben.Remove("Lies");
            gaben.Remove("Katalognamen");
            return gaben;
        }

        /// <summary>
        /// OK in der Betriebsart Projekt, Schritt 1: die Gebäudedaten der Projektkopie — dieselben
        /// Ableitungen wie beim Katalog (<see cref="NachModell"/>), der Name bleibt der der Kopie.
        /// </summary>
        internal static GebaeudeKatalogErgebnis ProjektSchreiben(int idProjekt, int idGebaeude, GebaeudeKatalogDaten daten)
        {
            GebaeudeModel vorher = GebaeudeStammCtrl.LiesProjektkopie(idGebaeude);
            if (vorher == null || daten == null)
                return new GebaeudeKatalogErgebnis(false, MyResource.Resource.GEBZ_MSG_GEBAEUDE);
            string name = vorher.Gebaeudename;
            GebaeudeModel modell = NachModell(daten, vorher);
            modell.Gebaeudename = name;
            if (!GebaeudeStammCtrl.ProjektkopieUeberschreiben(idGebaeude, idProjekt, modell))
                return new GebaeudeKatalogErgebnis(false, MyResource.Resource.GEBZ_MSG_GEBAEUDE);
            MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);
            return new GebaeudeKatalogErgebnis(true, "");
        }

        /// <summary>
        /// <b>Der Zonenweg eines Projektgebäudes</b> (Softwarearchitektur 2.9, 3.3): die Zonen, wie
        /// <see cref="GebaeudeZonenCtrl.LesenJeGebaeude"/> sie liefert, die Aufbauten des Projekts und
        /// des Katalogs zur Wahl (U im Kern gerechnet), und die drei Wege des OK — Übernahme mit
        /// Hochrechnung (<see cref="GebaeudeZonenCtrl.Uebernahme"/>), Katalogaufbau in das Projekt
        /// (<see cref="BauteilaufbauCtrl.CopyFromStamm"/>) und das Aggregat der Zonen
        /// (<see cref="GebaeudeZonenCtrl.SpeichernJeGebaeude"/>).
        ///
        /// <para><b>Was die Oberfläche nicht bearbeitet, bleibt</b>: Die Spalten einer Zone, die G3
        /// nicht liest (Sollwerte, Lüftung, Kühl- und Übergabeeingaben, Herkunft), hält der Weg je Id
        /// fest und schreibt sie unverändert zurück; eine neue Zone ist beheizt und trägt die Herkunft
        /// ihres Vorschlags. Ein Duplikat (Stufe G6a, <see cref="ZoneDaten.VorlageId"/>) übernimmt diese
        /// Spalten von seiner Vorlage — ohne deren Herkunft, Quellkennung und Importpaarung.</para>
        /// </summary>
        internal static GebaeudeZonenweg Zonenweg(int idProjekt, int idZ, int idGebaeude)
        {
            var zonenCtrl = new GebaeudeZonenCtrl();
            var aufbauCtrl = new BauteilaufbauCtrl();
            var gelesen = new Dictionary<int, ZoneModel>();
            foreach (ZoneModel z in zonenCtrl.LesenJeGebaeude(idGebaeude)) gelesen[z.ID] = z;

            var projektaufbauten = aufbauCtrl.LesenJeProjekt(idProjekt).Where(a => a != null).ToDictionary(a => a.ID);
            List<AufbauWahl> projektwahl = projektaufbauten.Values.Select(a => Wahl(a, false)).ToList();
            List<AufbauWahl> katalogwahl = aufbauCtrl.LesenKatalog().Where(a => a != null).Select(a => Wahl(a, true)).ToList();

            ZoneDaten AlsDaten(ZoneModel z) => new ZoneDaten
            {
                Id = z.ID,
                Bezeichner = z.Bezeichner ?? "",
                Nutzflaeche = z.Nutzflaeche,
                Bauteile = (z.Bauteile ?? new List<BauteilModel>()).Where(b => b != null).Select(b =>
                {
                    AufbauWahl a = b.ID_Aufbau is int id ? projektwahl.FirstOrDefault(x => x.Id == id) : null;
                    return new BauteilDaten
                    {
                        Id = b.ID,
                        Bezeichner = b.Bezeichner ?? "",
                        Bauteilart = b.Bauteilart,
                        Flaeche = b.Flaeche,
                        UWert = b.U_Wert,
                        GWert = b.g_Wert,
                        Rahmenanteil = b.Rahmenanteil,
                        Verschattung = b.Verschattungsfaktor,
                        Azimut = b.Azimut,
                        Neigung = b.Neigung,
                        Randbedingung = b.Randbedingung,
                        PsiL = b.Psi_L,
                        IdAufbau = b.ID_Aufbau,
                        AufbauText = a?.Text ?? "",
                        UAufbau = a?.UWert,
                        Herkunft = b.Herkunft,
                        Quellkennung = b.Quellkennung
                    };
                }).ToList()
            };

            Func<GebaeudeKatalogDaten, Task<ZonenuebernahmeDaten>> uebernehmen = d =>
            {
                GebaeudeModel satz = NachModell(d, GebaeudeStammCtrl.LiesProjektkopie(idGebaeude) ?? new GebaeudeModel());
                GebaeudeZonenCtrl.Uebernahmevorschlag v = GebaeudeZonenCtrl.Uebernahme(idProjekt, idZ, satz);
                if (v.Ok && v.Zone != null) gelesen[v.Zone.ID] = v.Zone;
                return Task.FromResult(new ZonenuebernahmeDaten(v.Ok, v.Meldung ?? "", v.Faktor,
                    v.Zone == null ? null : AlsDaten(v.Zone), v.NutzflaecheGebaeude, v.Einheit ?? "", v.Angabe,
                    v.Verbrauchsangabe, v.HeizgrenzeKw, v.KuehlgrenzeKw));
            };

            Func<int, AufbauUebernahmeErgebnis> aufbauUebernehmen = stammId =>
            {
                int neu = aufbauCtrl.CopyFromStamm(stammId, idProjekt);
                BauteilaufbauModel kopie = neu > 0 ? aufbauCtrl.LesenProjektsatz(neu) : null;
                if (kopie == null)
                    return new AufbauUebernahmeErgebnis(false, MyResource.Resource.BTA_MSG_FEHLER, null);
                projektaufbauten[kopie.ID] = kopie;
                AufbauWahl w = Wahl(kopie, false);
                projektwahl.Add(w);
                return new AufbauUebernahmeErgebnis(true, "", w);
            };

            // Die Zeilen des Kerns aus dem Arbeitsstand (Stufe G6a): Was die Oberflaeche nicht fuehrt,
            // kommt aus der gelesenen Zeile gleicher Id; ein Duplikat nimmt es von seiner Vorlage
            // (VorlageId) - ohne Herkunft, Quellkennung und Importpaarung der Vorlage; eine neue
            // Zone ist beheizt und manuell.
            List<ZoneModel> Zeilen(IReadOnlyList<ZoneDaten> liste)
            {
                var zeilen = new List<ZoneModel>();
                foreach (ZoneDaten d in liste ?? Array.Empty<ZoneDaten>())
                {
                    ZoneModel z;
                    if (gelesen.TryGetValue(d.Id, out ZoneModel alt)) z = alt.Kopie();
                    else if (d.VorlageId is int vorlage && gelesen.TryGetValue(vorlage, out ZoneModel quelle))
                    {
                        z = quelle.Kopie();
                        z.ID = d.Id;
                        z.Herkunft = DbWerte.HERKUNFT_MANUELL;
                        z.Quellkennung = null;
                    }
                    else z = new ZoneModel { ID = d.Id, IstBeheizt = true, Herkunft = DbWerte.HERKUNFT_MANUELL };
                    z.Bezeichner = d.Bezeichner ?? "";
                    z.Nutzflaeche = d.Nutzflaeche;
                    z.Bauteile = d.Bauteile.Select(b => new BauteilModel
                    {
                        ID = b.Id,
                        Bezeichner = b.Bezeichner ?? "",
                        Bauteilart = b.Bauteilart,
                        ID_Aufbau = b.IdAufbau,
                        Flaeche = b.Flaeche ?? 0.0,
                        U_Wert = b.UWert,
                        g_Wert = b.GWert,
                        Rahmenanteil = b.Rahmenanteil,
                        Verschattungsfaktor = b.Verschattung,
                        Neigung = b.Neigung,
                        Azimut = b.Azimut,
                        Randbedingung = b.Randbedingung,
                        Psi_L = b.PsiL,
                        Herkunft = b.Herkunft,
                        Quellkennung = b.Quellkennung
                    }).ToList();
                    zeilen.Add(z);
                }
                return zeilen;
            }

            Func<IReadOnlyList<ZoneDaten>, string> speichern = liste =>
            {
                List<ZoneModel> zeilen = Zeilen(liste);
                GebaeudeZonenCtrl.Ergebnis e = zonenCtrl.SpeichernJeGebaeude(idGebaeude, zeilen);
                if (!e.Ok) return e.Meldung ?? "";
                gelesen.Clear();
                foreach (ZoneModel z in zeilen) gelesen[z.ID] = z;
                MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);
                return "";
            };

            // Die Pruefregeln des Kerns ueber die ganze Liste, ohne Datenbank (G6a).
            Func<IReadOnlyList<ZoneDaten>, string> pruefen = liste => GebaeudeZonenCtrl.Pruefen(Zeilen(liste)) ?? "";

            Func<AufbauWahl, AufbauAnsichtDaten> ansicht = w =>
            {
                if (w == null) return null;
                BauteilaufbauModel m = w.Katalog ? aufbauCtrl.LesenKatalogsatz(w.Id)
                    : projektaufbauten.TryGetValue(w.Id, out BauteilaufbauModel p) ? p : aufbauCtrl.LesenProjektsatz(w.Id);
                if (m == null) return null;
                BauteilaufbauDaten daten = BauteilaufbauHuelle.AlsDaten(m);
                return new AufbauAnsichtDaten(daten, BauteilaufbauHuelle.Kennwerte(daten),
                                              w.Katalog ? BauteilaufbauHuelle.Baustoffe() : Projektbaustoffe(idProjekt));
            };

            return new GebaeudeZonenweg
            {
                Zonen = gelesen.Values.OrderBy(z => z.Rang).Select(AlsDaten).ToList(),
                Uebernehmen = uebernehmen,
                AufbauUebernehmen = aufbauUebernehmen,
                Speichern = speichern,
                Pruefen = pruefen,
                Projektaufbauten = projektwahl,
                Katalogaufbauten = katalogwahl,
                Aufbau = ansicht
            };
        }

        /// <summary>Ein Aufbau zur Wahl: Name (samt Bauteilart) und der im Kern gerechnete U-Wert.</summary>
        private static AufbauWahl Wahl(BauteilaufbauModel a, bool katalog)
        {
            double? u = null;
            try { u = BauteilaufbauCtrl.Kennwerte(a, mitBezugsperiode: false).U_WM2K; }
            catch (Exception) { u = null; }
            string text = a.Bezeichner ?? "";
            if (!string.IsNullOrEmpty(a.Bauteilart)) text += " · " + BauteilaufbauCtrl.BauteilartText(a.Bauteilart);
            return new AufbauWahl(a.ID, katalog, text, a.Bauteilart ?? "", u);
        }

        /// <summary>Die Baustoffe des Projekts — die Schichten eines Projektaufbaus zeigen auf sie.</summary>
        private static IReadOnlyList<BaustoffWahl> Projektbaustoffe(int idProjekt)
            => new BaustoffCtrl().LesenProjekt(idProjekt)
                   .Select(b => new BaustoffWahl(b.ID, b.Bezeichner ?? "", b.Lambda, b.Rho, b.Cp)).ToList();

        private static IReadOnlyDictionary<string, object> Grundgaben(
            GebaeudeModel geladen, GebaeudeKatalogModus modus)
        {
            // Die Brauchwasser-Zuordnungen des laufenden Projekts. Sie werden erst beim
            // Oeffnen der Ueberlagerung gelesen; das OK der Profilliste schreibt sie zurueck -
            // zusammen mit dem Arbeitsstand des Zapfprofils (Behaelter je Oeffnen, 5.2).
            var brauchwasser = new List<Z_ProjektBrauchwasserModel>();
            GebaeudePrueftexte p = Prueftexte();

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
                    : new Func<IReadOnlyDictionary<string, object>>(() => BrauchwasserGaben(brauchwasser, modus)),
                // Kein "BrauchwasserFertig": Das OK der Profilliste schreibt Zuordnungen und
                // Zapfprofil selbst, bevor sie schliesst (ZapfprofilHuelle.Schreibweg), und markiert
                // das Projekt nur, wenn es tatsaechlich schreibt - ein OK ohne Aenderung laesst das
                // Aenderungsdatum stehen.

                // Stufe AK1 (Anlagenkopplung 8.4, 9.1, 9.2): die hergeleiteten Vorgaben der
                // Waermeuebergabe aus dem Kern (Klimareihe des laufenden Projekts, einmal je
                // Oeffnen gelesen) und das Vorschaubild des Sollwert-Zeitprogramms.
                ["UebergabeHerleitung"] = Herleitungsweg(Dienste.Projekt.Id),
                ["WochenVorschau"] = Wochenvorschau(),

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
                ["LabelBaualtersklasse"] = Text_("GEBK_LBL_BAUALTERSKLASSE", "Baualtersklasse :"),
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
                ["LabelNachtBeginn"] = Text_("GEBK_LBL_NACHT_BEGINN", "Nachtabsenkung von :"),
                ["LabelNachtEnde"] = Text_("GEBK_LBL_NACHT_ENDE", "Nachtabsenkung bis :"),
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

                // Feldnamen und Meldungen der Pruefung - aus EINER Zuordnung (Prueftexte), die
                // auch das Stammblatt der Gebaeudeverwaltung nimmt (#465).
                ["MeldungZahlFehlt"] = p.MeldungZahlFehlt,
                ["MeldungNameFehlt"] = p.MeldungNameFehlt,
                ["MeldungBaujahr"] = p.MeldungBaujahr,
                ["MeldungNachtzeitNurEine"] = p.MeldungNachtzeitNurEine,
                ["MeldungNachtzeitGleich"] = p.MeldungNachtzeitGleich,
                ["MeldungNachtzeitBereich"] = p.MeldungNachtzeitBereich,
                ["MeldungFerienWinter"] = p.MeldungFerienWinter,
                ["MeldungFerienOstern"] = p.MeldungFerienOstern,
                ["MeldungFerienSommer"] = p.MeldungFerienSommer,
                ["MeldungFerienHerbst"] = p.MeldungFerienHerbst,

                ["FeldWohnflaeche"] = p.FeldWohnflaeche,
                ["FeldFlaecheNutzer"] = p.FeldFlaecheNutzer,
                ["FeldWaermegewinne"] = p.FeldWaermegewinne,
                ["FeldFensterdurchlassgrad"] = p.FeldFensterdurchlassgrad,
                ["FeldRaumhoehe"] = p.FeldRaumhoehe,
                ["FeldFFSued"] = p.FeldFFSued,
                ["FeldFFNord"] = p.FeldFFNord,
                ["FeldFlaecheAussenwand"] = p.FeldFlaecheAussenwand,
                ["FeldDachflaeche"] = p.FeldDachflaeche,
                ["FeldGrundflaeche"] = p.FeldGrundflaeche,
                ["FeldSonstigeFlaechen"] = p.FeldSonstigeFlaechen,
                ["FeldUAussenwand"] = p.FeldUAussenwand,
                ["FeldUFenster"] = p.FeldUFenster,
                ["FeldUDachflaeche"] = p.FeldUDachflaeche,
                ["FeldUGrundflaeche"] = p.FeldUGrundflaeche,
                ["FeldUSonstiges"] = p.FeldUSonstiges,

                ["HilfeSchluessel"] = "Form_Gebaeude1.btn_Help"
            };
        }

        /// <summary>
        /// <b>Feldnamen und Meldungen der Prüfung</b> (<see cref="GebaeudeArbeitsstand.Pruefen"/>)
        /// — die EINE Zuordnung zu den Ressourcen, für den Katalogeditor (<see cref="Gaben"/>)
        /// und das Stammblatt der Gebäudeverwaltung (<c>GebaeudeAdminHuelle</c>, #465). Der
        /// Rückfall ist der Vorgabewert des Bündels.
        /// </summary>
        internal static GebaeudePrueftexte Prueftexte()
        {
            var p = new GebaeudePrueftexte();
            p.MeldungZahlFehlt = Text_("GEBK_MSG_ZAHL", p.MeldungZahlFehlt);
            p.MeldungNameFehlt = Text_("GEBK_MSG_NAME_LEER", p.MeldungNameFehlt);
            p.MeldungFerienWinter = Text_(Ferienzeit.MELDUNG_WINTER, p.MeldungFerienWinter);
            p.MeldungFerienOstern = Text_(Ferienzeit.MELDUNG_OSTERN, p.MeldungFerienOstern);
            p.MeldungFerienSommer = Text_(Ferienzeit.MELDUNG_SOMMER, p.MeldungFerienSommer);
            p.MeldungFerienHerbst = Text_(Ferienzeit.MELDUNG_HERBST, p.MeldungFerienHerbst);

            p.MeldungBaujahr = Text_("GEBK_MSG_BAUJAHR", p.MeldungBaujahr);
            p.FeldBaujahr = GebaeudeArbeitsstand.Feld(Text_("GEBK_LBL_BAUJAHR", p.FeldBaujahr));

            p.MeldungNachtzeitNurEine = Text_("GEBK_MSG_NACHTZEIT_NUR_EINE", p.MeldungNachtzeitNurEine);
            p.MeldungNachtzeitGleich = Text_("GEBK_MSG_NACHTZEIT_GLEICH", p.MeldungNachtzeitGleich);
            p.MeldungNachtzeitBereich = Text_("GEBK_MSG_NACHTZEIT_BEREICH", p.MeldungNachtzeitBereich);
            p.FeldNachtBeginn = GebaeudeArbeitsstand.Feld(Text_("GEBK_LBL_NACHT_BEGINN", p.FeldNachtBeginn));
            p.FeldNachtEnde = GebaeudeArbeitsstand.Feld(Text_("GEBK_LBL_NACHT_ENDE", p.FeldNachtEnde));

            p.FeldWohnflaeche = Text_("GEBK_FELD_WOHNFLAECHE", p.FeldWohnflaeche);
            p.FeldFlaecheNutzer = Text_("GEBK_FELD_FLAECHE_NUTZER", p.FeldFlaecheNutzer);
            p.FeldWaermegewinne = Text_("GEBK_FELD_WAERMEGEWINNE", p.FeldWaermegewinne);
            p.FeldFensterdurchlassgrad = Text_("GEBK_FELD_FENSTERDURCHLASS", p.FeldFensterdurchlassgrad);
            p.FeldRaumhoehe = Text_("GEBK_FELD_RAUMHOEHE", p.FeldRaumhoehe);
            p.FeldLuftwechsel = GebaeudeArbeitsstand.Feld(Text_("GEBK_LBL_LUFTWECHSEL", p.FeldLuftwechsel));
            p.FeldFFSued = Text_("GEBK_FELD_FF_SUED", p.FeldFFSued);
            p.FeldFFNord = Text_("GEBK_FELD_FF_NORD", p.FeldFFNord);
            p.FeldFlaecheAussenwand = Text_("GEBK_FELD_FL_AUSSENWAND", p.FeldFlaecheAussenwand);
            p.FeldDachflaeche = Text_("GEBK_FELD_DACHFLAECHE", p.FeldDachflaeche);
            p.FeldGrundflaeche = Text_("GEBK_FELD_GRUNDFLAECHE", p.FeldGrundflaeche);
            p.FeldSonstigeFlaechen = Text_("GEBK_FELD_SONST_FLAECHEN", p.FeldSonstigeFlaechen);
            p.FeldUAussenwand = Text_("GEBK_FELD_U_AUSSENWAND", p.FeldUAussenwand);
            p.FeldUFenster = Text_("GEBK_FELD_U_FENSTER", p.FeldUFenster);
            p.FeldUDachflaeche = Text_("GEBK_FELD_U_DACHFLAECHE", p.FeldUDachflaeche);
            p.FeldUGrundflaeche = Text_("GEBK_FELD_U_GRUNDFLAECHE", p.FeldUGrundflaeche);
            p.FeldUSonstiges = Text_("GEBK_FELD_U_SONSTIGES", p.FeldUSonstiges);
            return p;
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

            // Stufe KU1 (Kuehlkonzept 8.1): die Gruppe „Kuehlung".
            t.GruppeKuehlung = Text_("GEBK_GRP_KUEHLUNG", t.GruppeKuehlung);
            t.LabelKuehlungAktiv = Text_("GEBK_LBL_KUEHLUNG_AKTIV", t.LabelKuehlungAktiv);
            t.LabelKuehlSollwert = Text_("GEBK_LBL_KUEHL_SOLLWERT", t.LabelKuehlSollwert);
            t.LabelKuehlleistungMax = Text_("GEBK_LBL_KUEHLLEISTUNG_MAX", t.LabelKuehlleistungMax);
            t.VorgabeKuehlungAus = Text_("GEBK_VORGABE_KUEHLUNG_AUS", t.VorgabeKuehlungAus);
            t.ZeileKuehlungAn = Text_("GEBK_ZEILE_KUEHLUNG_AN", t.ZeileKuehlungAn);
            t.ZeileKuehlungAus = Text_("GEBK_ZEILE_KUEHLUNG_AUS", t.ZeileKuehlungAus);
            t.ZeileKuehlungProjekt = Text_("GEBK_ZEILE_KUEHLUNG_PROJEKT", t.ZeileKuehlungProjekt);
            t.ZeileKuehlungBestandsweg = Text_("GEBK_ZEILE_KUEHLUNG_BESTANDSWEG", t.ZeileKuehlungBestandsweg);
            t.MeldungKuehlsollwertBereich = Text_("GEBK_MSG_KUEHLSOLLWERT_BEREICH", t.MeldungKuehlsollwertBereich);
            t.MeldungKuehlsollwertHeizung = Text_("GEBK_MSG_KUEHLSOLLWERT_HEIZUNG", t.MeldungKuehlsollwertHeizung);
            t.MeldungKuehlleistung = Text_("GEBK_MSG_KUEHLLEISTUNG", t.MeldungKuehlleistung);

            t.HinweisSpeichernUnter = Text_("GEBK_HINWEIS_SPEICHERN_UNTER", t.HinweisSpeichernUnter);

            // Stufe AK1 (Anlagenkopplung 9.1, 9.2): die Gruppe „Waermeuebergabe" samt Wochenraster.
            t.Uebergabe = UebergabeTexte();
            // E37: der Unterabschnitt „Kuehluebergabe" der Gruppe „Kuehlung".
            t.Kuehluebergabe = KuehluebergabeTexte();

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

        /// <summary>Das Textbündel der Gruppe „Wärmeübergabe" und des Wochenrasters — Rückfall ist der Vorgabewert.</summary>
        internal static WaermeuebergabeTexte UebergabeTexte()
        {
            var u = new WaermeuebergabeTexte();
            u.Gruppe = Text_("GEBK_GRP_WAERMEUEBERGABE", u.Gruppe);
            u.LabelHeizkreisAktiv = Text_("GEBK_LBL_HEIZKREIS_AKTIV", u.LabelHeizkreisAktiv);
            u.LabelArt = Text_("GEBK_LBL_UEBERGABE_ART", u.LabelArt);
            u.ArtIdeal = Text_("GEBK_UEBERGABE_IDEAL", u.ArtIdeal);
            u.ArtRadiator = Text_("GEBK_UEBERGABE_RADIATOR", u.ArtRadiator);
            u.ArtFlaeche = Text_("GEBK_UEBERGABE_FLAECHE", u.ArtFlaeche);
            u.ArtKonvektor = Text_("GEBK_UEBERGABE_KONVEKTOR", u.ArtKonvektor);
            u.LabelExponent = Text_("GEBK_LBL_UEBERGABE_EXPONENT", u.LabelExponent);
            u.LabelAuslegungVorlauf = Text_("GEBK_LBL_AUSLEGUNG_VORLAUF", u.LabelAuslegungVorlauf);
            u.LabelAuslegungRuecklauf = Text_("GEBK_LBL_AUSLEGUNG_RUECKLAUF", u.LabelAuslegungRuecklauf);
            u.LabelAuslegungRaum = Text_("GEBK_LBL_AUSLEGUNG_RAUM", u.LabelAuslegungRaum);
            u.LabelAuslegungAussen = Text_("GEBK_LBL_AUSLEGUNG_AUSSEN", u.LabelAuslegungAussen);
            u.LabelNennleistung = Text_("GEBK_LBL_UEBERGABE_NENNLEISTUNG", u.LabelNennleistung);
            u.LabelHeizkurveAktiv = Text_("GEBK_LBL_HEIZKURVE_AKTIV", u.LabelHeizkurveAktiv);
            u.LabelHeizkurveNiveau = Text_("GEBK_LBL_HEIZKURVE_NIVEAU", u.LabelHeizkurveNiveau);
            u.LabelHeizkurveSteilheit = Text_("GEBK_LBL_HEIZKURVE_STEILHEIT", u.LabelHeizkurveSteilheit);
            u.LabelProportionalband = Text_("GEBK_LBL_PROPORTIONALBAND", u.LabelProportionalband);
            u.BandFrei = Text_("GEBK_BAND_FREI", u.BandFrei);
            u.LabelBandFrei = Text_("GEBK_LBL_BAND_FREI", u.LabelBandFrei);
            u.VorgabeHergeleitet = Text_("GEBK_VORGABE_HERGELEITET", u.VorgabeHergeleitet);
            u.LabelSollwertprofil = Text_("GEBK_LBL_SOLLWERTPROFIL", u.LabelSollwertprofil);
            u.ZeileAus = Text_("GEBK_ZEILE_UEBERGABE_AUS", u.ZeileAus);
            u.ZeileIdeal = Text_("GEBK_ZEILE_UEBERGABE_IDEAL", u.ZeileIdeal);
            u.ZeileArt = Text_("GEBK_ZEILE_UEBERGABE_ART", u.ZeileArt);
            u.ZeileRaum = Text_("GEBK_ZEILE_AUSLEGUNG_RAUM", u.ZeileRaum);
            u.ZeileAussen = Text_("GEBK_ZEILE_AUSLEGUNG_AUSSEN", u.ZeileAussen);
            u.ZeileAussenOhne = Text_("GEBK_ZEILE_AUSLEGUNG_AUSSEN_OHNE", u.ZeileAussenOhne);
            u.ZeileNennleistung = Text_("GEBK_ZEILE_NENNLEISTUNG", u.ZeileNennleistung);
            u.ZeileNennleistungOhne = Text_("GEBK_ZEILE_NENNLEISTUNG_OHNE", u.ZeileNennleistungOhne);
            u.ZeileHerleitungBefund = Text_("GEBK_ZEILE_HERLEITUNG_BEFUND", u.ZeileHerleitungBefund);
            u.ZeileHeizkurveAn = Text_("GEBK_ZEILE_HEIZKURVE_AN", u.ZeileHeizkurveAn);
            u.ZeileHeizkurveAus = Text_("GEBK_ZEILE_HEIZKURVE_AUS", u.ZeileHeizkurveAus);
            u.ZeileBand = Text_("GEBK_ZEILE_PROPORTIONALBAND", u.ZeileBand);
            u.ZeileProjekt = Text_("GEBK_ZEILE_UEBERGABE_PROJEKT", u.ZeileProjekt);
            u.ZeileBestandsweg = Text_("GEBK_ZEILE_UEBERGABE_BESTANDSWEG", u.ZeileBestandsweg);
            u.ZeileProfilOhne = Text_("GEBK_ZEILE_SOLLWERTPROFIL_OHNE", u.ZeileProfilOhne);
            u.ZeileProfilMit = Text_("GEBK_ZEILE_SOLLWERTPROFIL_MIT", u.ZeileProfilMit);
            u.MeldungBereich = Text_("GEBK_MSG_UEB_BEREICH", u.MeldungBereich);
            u.MeldungArtUnbekannt = Text_("GEBK_MSG_UEB_ART_UNBEKANNT", u.MeldungArtUnbekannt);
            u.MeldungNennleistung = Text_("GEBK_MSG_UEB_NENNLEISTUNG", u.MeldungNennleistung);
            u.MeldungVorlaufRaum = Text_("GEBK_MSG_UEB_VORLAUF_RAUM", u.MeldungVorlaufRaum);
            u.MeldungRuecklauf = Text_("GEBK_MSG_UEB_RUECKLAUF", u.MeldungRuecklauf);
            u.MeldungAussenRaum = Text_("GEBK_MSG_UEB_AUSSEN_RAUM", u.MeldungAussenRaum);
            u.MeldungProfilWertzahl = Text_("GEBK_MSG_UEB_PROFIL_WERTZAHL", u.MeldungProfilWertzahl);
            u.MeldungProfilKeineZahl = Text_("GEBK_MSG_UEB_PROFIL_KEINE_ZAHL", u.MeldungProfilKeineZahl);
            u.MeldungProfilWert = Text_("GEBK_MSG_UEB_PROFIL_WERT", u.MeldungProfilWert);

            EPOS.UI.Bausteine.WochenrasterTexte r = u.Raster;
            r.Wochentage = Text_("WRASTER_TAGE", r.Wochentage);
            r.KopfTag = Text_("WRASTER_KOPF_TAG", r.KopfTag);
            r.Zelle = Text_("WRASTER_ZELLE", r.Zelle);
            r.LabelZeile = Text_("WRASTER_LBL_ZEILE", r.LabelZeile);
            r.LabelZeilenwert = Text_("WRASTER_LBL_ZEILENWERT", r.LabelZeilenwert);
            r.KnopfZeileSetzen = Text_("WRASTER_BTN_ZEILE_SETZEN", r.KnopfZeileSetzen);
            r.KnopfWerktage = Text_("WRASTER_BTN_WERKTAGE", r.KnopfWerktage);
            r.KnopfWochenende = Text_("WRASTER_BTN_WOCHENENDE", r.KnopfWochenende);
            r.KnopfAlle = Text_("WRASTER_BTN_ALLE", r.KnopfAlle);
            r.KnopfAnlegen = Text_("WRASTER_BTN_ANLEGEN", r.KnopfAnlegen);
            r.KnopfVerwerfen = Text_("WRASTER_BTN_VERWERFEN", r.KnopfVerwerfen);
            r.ZeileVorgabe = Text_("WRASTER_ZEILE_VORGABE", r.ZeileVorgabe);
            r.BildTitel = Text_("WRASTER_BILD_TITEL", r.BildTitel);
            r.BildAchseX = Text_("WRASTER_BILD_X", r.BildAchseX);
            return u;
        }

        /// <summary>Das Textbündel des Unterabschnitts „Kühlübergabe" (E37) — Rückfall ist der Vorgabewert.</summary>
        internal static KuehluebergabeTexte KuehluebergabeTexte()
        {
            var k = new KuehluebergabeTexte();
            k.Unterabschnitt = Text_("GEBK_UABS_KUEHLUEBERGABE", k.Unterabschnitt);
            k.LabelAktiv = Text_("GEBK_LBL_KUEHLUEBERGABE_AKTIV", k.LabelAktiv);
            k.LabelArt = Text_("GEBK_LBL_KUEHL_UEBERGABE_ART", k.LabelArt);
            k.ArtIdeal = Text_("GEBK_KUEHLUEBERGABE_IDEAL", k.ArtIdeal);
            k.ArtKuehldecke = Text_("GEBK_KUEHLUEBERGABE_KUEHLDECKE", k.ArtKuehldecke);
            k.ArtFlaechenkuehlung = Text_("GEBK_KUEHLUEBERGABE_FLAECHENKUEHLUNG", k.ArtFlaechenkuehlung);
            k.ArtGeblaesekonvektor = Text_("GEBK_KUEHLUEBERGABE_GEBLAESEKONVEKTOR", k.ArtGeblaesekonvektor);
            k.LabelExponent = Text_("GEBK_LBL_KUEHL_UEBERGABE_EXPONENT", k.LabelExponent);
            k.LabelNennleistung = Text_("GEBK_LBL_KUEHL_UEBERGABE_NENNLEISTUNG", k.LabelNennleistung);
            k.LabelAuslegungVorlauf = Text_("GEBK_LBL_KUEHL_AUSLEGUNG_VORLAUF", k.LabelAuslegungVorlauf);
            k.LabelAuslegungRuecklauf = Text_("GEBK_LBL_KUEHL_AUSLEGUNG_RUECKLAUF", k.LabelAuslegungRuecklauf);
            k.LabelAuslegungRaum = Text_("GEBK_LBL_KUEHL_AUSLEGUNG_RAUM", k.LabelAuslegungRaum);
            k.LabelVorlaufgrenze = Text_("GEBK_LBL_KUEHL_VORLAUFGRENZE", k.LabelVorlaufgrenze);
            k.VorgabeHergeleitet = Text_("GEBK_VORGABE_HERGELEITET", k.VorgabeHergeleitet);
            k.VorgabeKeineGrenze = Text_("GEBK_VORGABE_KEINE_GRENZE", k.VorgabeKeineGrenze);
            k.ZeileAus = Text_("GEBK_ZEILE_KUEHLUEBERGABE_AUS", k.ZeileAus);
            k.ZeileIdeal = Text_("GEBK_ZEILE_KUEHLUEBERGABE_IDEAL", k.ZeileIdeal);
            k.ZeileArt = Text_("GEBK_ZEILE_KUEHLUEBERGABE_ART", k.ZeileArt);
            k.GrenzeKeine = Text_("GEBK_ZEILE_KUEHL_GRENZE_KEINE", k.GrenzeKeine);
            k.ZeileRaum = Text_("GEBK_ZEILE_KUEHL_AUSLEGUNG_RAUM", k.ZeileRaum);
            k.ZeileNennleistung = Text_("GEBK_ZEILE_KUEHL_NENNLEISTUNG", k.ZeileNennleistung);
            k.ZeileNennleistungOhne = Text_("GEBK_ZEILE_KUEHL_NENNLEISTUNG_OHNE", k.ZeileNennleistungOhne);
            k.ZeileHerleitungBefund = Text_("GEBK_ZEILE_HERLEITUNG_BEFUND", k.ZeileHerleitungBefund);
            k.ZeileVorlaufAnlage = Text_("GEBK_ZEILE_KUEHL_VORLAUF_ANLAGE", k.ZeileVorlaufAnlage);
            k.ZeileVorlaufGemischt = Text_("GEBK_ZEILE_KUEHL_VORLAUF_GEMISCHT", k.ZeileVorlaufGemischt);
            k.ZeileVorlaufAuslegung = Text_("GEBK_ZEILE_KUEHL_VORLAUF_AUSLEGUNG", k.ZeileVorlaufAuslegung);
            k.ZeileVorlaufOhne = Text_("GEBK_ZEILE_KUEHL_VORLAUF_OHNE", k.ZeileVorlaufOhne);
            k.ZeileGrenze = Text_("GEBK_ZEILE_KUEHL_GRENZE", k.ZeileGrenze);
            k.ZeileGrenzeUeberAuslegung = Text_("GEBK_ZEILE_KUEHL_GRENZE_UEBER_AUSLEGUNG", k.ZeileGrenzeUeberAuslegung);
            k.ZeileSensibel = Text_("GEBK_ZEILE_KUEHL_SENSIBEL", k.ZeileSensibel);
            k.ZeileWirksam = Text_("GEBK_ZEILE_KUEHL_WIRKSAM", k.ZeileWirksam);
            k.ZeileOhneSollwert = Text_("GEBK_ZEILE_KUEHL_OHNE_SOLLWERT", k.ZeileOhneSollwert);
            k.ZeileProjektOhneStufe = Text_("GEBK_ZEILE_KUEHL_PROJEKT_OHNE_STUFE", k.ZeileProjektOhneStufe);
            k.ZeileProjektOhneKaelte = Text_("GEBK_ZEILE_KUEHL_PROJEKT_OHNE_KAELTE", k.ZeileProjektOhneKaelte);
            k.ZeileProjekt = Text_("GEBK_ZEILE_KUEHL_PROJEKT", k.ZeileProjekt);
            k.MeldungBereich = Text_("GEBK_MSG_UEB_BEREICH", k.MeldungBereich);
            k.MeldungArtUnbekannt = Text_("GEBK_MSG_KUEHL_ART_UNBEKANNT", k.MeldungArtUnbekannt);
            k.MeldungNennleistung = Text_("GEBK_MSG_KUEHL_NENNLEISTUNG", k.MeldungNennleistung);
            k.MeldungReihenfolge = Text_("GEBK_MSG_KUEHL_REIHENFOLGE", k.MeldungReihenfolge);
            return k;
        }

        /// <summary>
        /// Der Weg der hergeleiteten Vorgaben (Anlagenkopplung 8.4, H10; E37 A2): eine Quelle je
        /// Öffnen — sie liest die Klimareihe des Projekts einmal — und je Aufruf ein Katalogsatz aus
        /// dem Probestand des Dialogs; er trägt Heiz- und Kälteseite. Ohne Projekt (≤ 0) oder ohne
        /// Klimaregion keine Zahl.
        /// </summary>
        internal static Func<GebaeudeKatalogDaten, UebergabeHerleitungDaten> Herleitungsweg(int idProjekt)
        {
            var quelle = new UebergabeHerleitungsquelle(idProjekt);
            return d =>
            {
                if (d == null) return null;
                GebaeudeModel satz = NachModell(d, new GebaeudeModel());
                UebergabeHerleitung h = quelle.Herleiten(satz);
                KuehluebergabeHerleitungDaten kuehl = Kuehlherleitung(quelle.HerleitenKuehlung(satz));
                if (h == null && kuehl == null) return null;
                return new UebergabeHerleitungDaten(h?.AuslegungAussenC, h?.AuslegungsheizlastKw, h?.Befund ?? "", kuehl);
            };
        }

        /// <summary>Die Herleitung der Kälteseite als DTO der Oberfläche; <c>null</c> bleibt <c>null</c>.</summary>
        internal static KuehluebergabeHerleitungDaten Kuehlherleitung(KuehluebergabeHerleitung k)
            => k == null
                ? null
                : new KuehluebergabeHerleitungDaten(k.AuslegungstagText, k.AuslegungstagMittelC, k.AuslegungskuehllastKw,
                                                    k.Vorlaufquelle == Vorlaufquelle.Anlage, k.VorlaufQuelleC, k.VorlaufC,
                                                    k.Gekappt, k.VorlaufgrenzeC, k.ProjektKoppelt, k.ProjektKuehlt,
                                                    k.Befund ?? "");

        /// <summary>Das Vorschaubild des Sollwert-Zeitprogramms — 168 Wochenstunden, gezeichnet im Kern.</summary>
        internal static Func<double[], WindowsFormsApplication1.Zeichnung.Zeichenmodell> Wochenvorschau()
        {
            WaermeuebergabeTexte u = UebergabeTexte();
            return werte => werte == null || werte.Length != AnlagenkopplungSchema.WOCHENWERTE
                ? null
                : ChartRenderer.StundenprofilModell(u.Raster.BildTitel, werte, 24, u.Raster.BildAchseX, "°C");
        }

        // =================================================================================
        // Datenseite
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Brauchwasser-Profilliste. Die Zuordnungen des laufenden
        /// Projekts werden hier frisch gelesen — der Vorläufer tat dasselbe beim Klick.
        /// </summary>
        private static IReadOnlyDictionary<string, object> BrauchwasserGaben(
            List<Z_ProjektBrauchwasserModel> ziel, GebaeudeKatalogModus modus)
        {
            int projektId = Dienste.Projekt.Id;

            // Aus der Verwaltung (Modus Admin) gehoert der Gebaeudekatalog keinem Projekt: Die
            // Huelle reicht keinen Zapfprofil-Behaelter, der Bedarfsprofil-Dialog zeigt dann weder
            // Knopf noch Optionsgruppe (Umsetzungskonzept Zapfprofilgenerator 5.2).
            ZapfprofilBehaelter zapfprofil = modus == GebaeudeKatalogModus.Admin
                ? null : new ZapfprofilBehaelter(projektId);

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

            return Gebaeudewege.BrauchwasserGaben?.Invoke(projektId, zeilen, geaendert, zapfprofil);
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
                // G4a: das Baujahr neben der Klasse - NULL bleibt null (unbekannt).
                Baujahr = m.Baujahr,
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
                // E43: die Nachtzeit NULL-erhaltend - leer heißt die Vorgabe 22 bis 6 Uhr.
                NachtBeginn = m.Nachtabsenkung_Beginn,
                NachtEnde = m.Nachtabsenkung_Ende,
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
                Sommerlueftung = m.Sommerlueftung,

                // Stufe KU1 (KU-S1, Kuehlkonzept 8.1): die vier Kuehleingaben - NULL bleibt null.
                KuehlungAktiv = m.Kuehlung_Aktiv,
                KuehlSollwert = m.Kuehl_Sollwert,
                KuehlleistungMax = m.Kuehlleistung_Max,
                KuehlSollwertNacht = m.Kuehl_Sollwert_Nacht,

                // Stufe AK1 (AK-S1, Anlagenkopplung 9.1): die dreizehn Felder der Waermeuebergabe -
                // NULL bleibt null.
                HeizkreisAktiv = m.Heizkreis_Aktiv,
                UebergabeArt = m.Uebergabe_Art,
                UebergabeExponent = m.Uebergabe_Exponent,
                UebergabeLeistungNennKw = m.Uebergabe_Leistung_Nenn,
                AuslegungVorlauf = m.Auslegung_Vorlauf,
                AuslegungRuecklauf = m.Auslegung_Ruecklauf,
                AuslegungRaumtemperatur = m.Auslegung_Raumtemperatur,
                AuslegungAussentemperatur = m.Auslegung_Aussentemperatur,
                HeizkurveAktiv = m.Heizkurve_Aktiv,
                HeizkurveNiveau = m.Heizkurve_Niveau,
                HeizkurveSteilheit = m.Heizkurve_Steilheit,
                ReglerProportionalband = m.Regler_Proportionalband,
                Sollwertprofil = m.Sollwertprofil,

                // E37 (KAK-S1, Anlagenkopplung 8.1): die acht Felder der Kuehluebergabe - NULL
                // bleibt null, der Schalter kennt kein NULL (A1).
                KuehluebergabeAktiv = m.Kuehluebergabe_Aktiv,
                KuehlUebergabeArt = m.Kuehl_Uebergabe_Art,
                KuehlUebergabeExponent = m.Kuehl_Uebergabe_Exponent,
                KuehlUebergabeLeistungNennKw = m.Kuehl_Uebergabe_Leistung_Nenn,
                KuehlAuslegungVorlauf = m.Kuehl_Auslegung_Vorlauf,
                KuehlAuslegungRuecklauf = m.Kuehl_Auslegung_Ruecklauf,
                KuehlAuslegungRaumtemperatur = m.Kuehl_Auslegung_Raumtemperatur,
                KuehlVorlaufgrenze = m.Kuehl_Vorlaufgrenze
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
            if (nutzer == 0) { m.Flaeche_Nutzer = GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE; nutzer = m.Flaeche_Nutzer; }
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
            // G4a: das Baujahr NULL-erhaltend - leer bleibt NULL ("unbekannt"), nie 0.
            m.Baujahr = d.Baujahr;
            m.Gebaeudeart = d.Gebaeudeart ?? "";
            m.Wohngebaeude_Nicht_Wohngebaeude = d.Verwendung ?? VERWENDUNGSWERTE[0];

            // Reiter 2 - die Ableitungen hat die Komponente im OK-Weg gemacht.
            m.Raumsolltemperatur_Tag = d.SollTag ?? 0;
            m.Raumsolltemperatur_Nachtabsenkung = d.NachtAbsenkung ?? 0;
            // E43: die Nachtzeit NULL-erhaltend - leer bleibt NULL (Vorgabe), nie 0.
            m.Nachtabsenkung_Beginn = d.NachtBeginn;
            m.Nachtabsenkung_Ende = d.NachtEnde;
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

            // Stufe KU1 (KU-S1): die vier Kuehleingaben, NULL-erhaltend - auch der Nachtwert,
            // den der Dialog nicht zeigt, reist mit (sonst verloere ihn „Speichern unter").
            m.Kuehlung_Aktiv = d.KuehlungAktiv;
            m.Kuehl_Sollwert = d.KuehlSollwert;
            m.Kuehlleistung_Max = d.KuehlleistungMax;
            m.Kuehl_Sollwert_Nacht = d.KuehlSollwertNacht;

            // Stufe AK1 (AK-S1): die dreizehn Felder der Waermeuebergabe, NULL-erhaltend. Sie
            // stehen HIER und nicht nur im geladenen Satz: "Speichern unter" legt einen NEUEN
            // Satz an (vorher = leeres Modell) - ohne diese Zeilen verloere er die Kopplung.
            m.Heizkreis_Aktiv = d.HeizkreisAktiv;
            m.Uebergabe_Art = d.UebergabeArt;
            m.Uebergabe_Exponent = d.UebergabeExponent;
            m.Uebergabe_Leistung_Nenn = d.UebergabeLeistungNennKw;
            m.Auslegung_Vorlauf = d.AuslegungVorlauf;
            m.Auslegung_Ruecklauf = d.AuslegungRuecklauf;
            m.Auslegung_Raumtemperatur = d.AuslegungRaumtemperatur;
            m.Auslegung_Aussentemperatur = d.AuslegungAussentemperatur;
            m.Heizkurve_Aktiv = d.HeizkurveAktiv;
            m.Heizkurve_Niveau = d.HeizkurveNiveau;
            m.Heizkurve_Steilheit = d.HeizkurveSteilheit;
            m.Regler_Proportionalband = d.ReglerProportionalband;
            m.Sollwertprofil = d.Sollwertprofil;

            // E37 (KAK-S1): die acht Felder der Kuehluebergabe, NULL-erhaltend und aus demselben
            // Grund hier: "Speichern unter" verloere sonst die Kaelteseite.
            m.Kuehluebergabe_Aktiv = d.KuehluebergabeAktiv;
            m.Kuehl_Uebergabe_Art = d.KuehlUebergabeArt;
            m.Kuehl_Uebergabe_Exponent = d.KuehlUebergabeExponent;
            m.Kuehl_Uebergabe_Leistung_Nenn = d.KuehlUebergabeLeistungNennKw;
            m.Kuehl_Auslegung_Vorlauf = d.KuehlAuslegungVorlauf;
            m.Kuehl_Auslegung_Ruecklauf = d.KuehlAuslegungRuecklauf;
            m.Kuehl_Auslegung_Raumtemperatur = d.KuehlAuslegungRaumtemperatur;
            m.Kuehl_Vorlaufgrenze = d.KuehlVorlaufgrenze;

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

        internal static string[] Verwendungen()
        {
            return new[]
            {
                Text_("GEBK_VERWENDUNG_WOHN", "Wohngebäude"),
                Text_("GEBK_VERWENDUNG_NICHTWOHN", "Nicht Wohngebäude")
            };
        }

        internal static string[] Bauarten()
        {
            return new[]
            {
                Text_("GEBK_BAUART_LEICHT", "Leichte Bauart"),
                Text_("GEBK_BAUART_SCHWER", "Schwere Bauart"),
                Text_("GEBK_BAUART_SEHRSCHWER", "Sehr schwere Bauart")
            };
        }

        internal static string[] Ferienzeitraeume()
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
