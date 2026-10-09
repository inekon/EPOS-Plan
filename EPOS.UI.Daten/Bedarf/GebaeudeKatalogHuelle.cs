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
            GebaeudeKatalogDaten daten = vorbelegung.Daten.Kopie();
            daten.Konditionierung ??= KonditionierungHuelle.Leer();
            return new Dictionary<string, object>(gaben)
            {
                ["Daten"] = daten,
                ["Vorbelegung"] = vorbelegung.Herleitung ?? ""
            };
        }

        private static IReadOnlyDictionary<string, object> Grundgaben(
            string bezeichner, GebaeudeKatalogModus modus)
        {
            GebaeudeModel geladen = modus == GebaeudeKatalogModus.Neu
                ? new GebaeudeModel()
                : Laden(bezeichner) ?? new GebaeudeModel();
            IReadOnlyDictionary<string, object> gaben = Grundgaben(geladen, modus);

            // Welle ZK-b: die Zonen des Katalogsatzes, bearbeitbar wie im Projekt. Der Zonenweg findet den Satz
            // über seinen Namen; der Schreibweg des Kopfs führt ihn nach (Modus Neu legt ihn erst an, „Speichern
            // unter" arbeitet am neuen Satz weiter).
            var ziel = new Katalogziel { Name = modus == GebaeudeKatalogModus.Neu ? "" : geladen.Gebaeudename ?? "" };
            gaben = new Dictionary<string, object>(gaben)
            {
                ["Zonen"] = KatalogZonenweg(ziel),
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>((d, istNeu, bez) =>
                {
                    GebaeudeKatalogErgebnis e = Schreiben(d, istNeu, bez);
                    if (e.Erfolg && istNeu) ziel.Name = d.Name ?? "";
                    return e;
                })
            };

            // DAS SCHLOSS ERREICHT DEN EDITOR (Stufe KP2, Befund B11): Ein ausgelieferter Satz
            // (ReadOnly) steht im Modus Bearbeiten gesperrt da - OK weich gesperrt mit Grund,
            // „Speichern unter" frei -, statt dass Schreiben ihn erst nach dem OK ablehnt. Die
            // Ablehnung in Schreiben bleibt als zweite Sicherung.
            if (modus != GebaeudeKatalogModus.Bearbeiten || string.IsNullOrEmpty(bezeichner)
                || !new GebaeudeStammCtrl().IsReadOnly(bezeichner))
                return gaben;
            return new Dictionary<string, object>(gaben)
            {
                ["Gesperrt"] = true,
                ["SperrGrund"] = Text_("KOND_TXT_HINWEIS_LESEMODUS",
                    "Dieser Katalogsatz gehört zur Auslieferung und ist nur lesbar. „Speichern unter“ " +
                    "legt eine bearbeitbare Kopie an.")
            };
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

            // Stufe KP2: die Konditionierung des PROJEKTGEBÄUDES im Feldsatz und der Weg im Projekt
            // (mit „aus dem Katalog erneut übernehmen").
            GebaeudeKatalogDaten daten = AusModell(kopie);
            daten.Konditionierung = KonditionierungHuelle.Lesen(KonditionierungCtrl.Eigner.Gebaeude(idGebaeude));
            // Stufe KP3, Welle O2 (E59): die manuelle Aufheizzeit der Projektkopie und ihre Vorschlaege.
            daten.AufheizzeitManuellH = AufheizauskunftCtrl.ManuellLesen(idGebaeude);
            var gaben = new Dictionary<string, object>(Grundgaben(kopie, GebaeudeKatalogModus.Projekt))
            {
                ["Daten"] = daten,
                ["Aufheizzeit"] = Aufheizvorschlaege(idProjekt, idGebaeude),
                // Der Bezug des Projekts (Kopplung, Referenzjahr, Kuehlbetrieb) wie im Lauf (KP2 U1, 4 (c)).
                ["Konditionierung"] = KonditionierungHuelle.Weg(Kalendereigentuemer.Gebaeude, idGebaeude, idProjekt),
                // „Speichern unter" im PROJEKTMODUS: Der neue Katalogbau bekommt die
                // Konditionierung des PROJEKTGEBÄUDES mit (Stufe KP1b, Konzept 5.5) — nur die
                // Gebäudeebene; die Zonenzeilen bleiben zurück und stehen im Befund der
                // Kernmethode für die Rückfrage, die mit KP2 kommt.
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    (d, istNeu, bez) => istNeu
                        ? Schreiben(d, true, bez, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude))
                        : ProjektSchreiben(idProjekt, idGebaeude, d)),
                ["Zonen"] = Zonenweg(idProjekt, idZ, idGebaeude),
                ["HilfeSchluessel"] = HILFE_PROJEKT
            };
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
            // Stufe KP2: Gebaeude samt Konditionierung in EINEM Vorgang (Schritt 1 des OK-Wegs).
            KonditionierungCtrl.Ergebnis e = GebaeudeStammCtrl.ProjektkopieSchreiben(
                idGebaeude, idProjekt, modell, KonditionierungHuelle.Schreibstand(daten, Kalendereigentuemer.Gebaeude));
            if (!e.Ok)
                return new GebaeudeKatalogErgebnis(false, string.IsNullOrEmpty(e.Meldung) ? MyResource.Resource.GEBZ_MSG_GEBAEUDE : e.Meldung);
            // Stufe KP3, Welle O2 (E59): die manuelle Aufheizzeit gehoert zu den Gebaeudedaten (Schritt 1) - nur
            // geschrieben, wenn sie sich geaendert hat; eine Datenbank ohne die Spalte lehnt einen Wert benannt ab.
            if (daten.AufheizzeitManuellH != AufheizauskunftCtrl.ManuellLesen(idGebaeude)
                && AufheizauskunftCtrl.ManuellSchreiben(idGebaeude, daten.AufheizzeitManuellH) is string grund)
                return new GebaeudeKatalogErgebnis(false, grund);
            MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);
            return new GebaeudeKatalogErgebnis(true, "");
        }

        /// <summary>
        /// Die Vorschläge des Feldes „Aufheizzeit manuell (h)" (Stufe KP3, Welle O2; Festlegung 40): die Auskunft der
        /// Bemessung der Projektkopie — auch bei ausgeschaltetem Schalter, damit der Vorschlag schon vor dem Einschalten
        /// steht — mit der bemessenen Zeit und der Spanne aus τ₂ (<see cref="AufheizauskunftCtrl.Vorschlag"/>).
        /// </summary>
        internal static AufheizzeitManuellDaten Aufheizvorschlaege(int idProjekt, int idGebaeude)
        {
            Aufheizauskunft a = AufheizauskunftCtrl.Projektgebaeude(idProjekt, idGebaeude, true);
            Aufheizvorschlag v = AufheizauskunftCtrl.Vorschlag(a);
            return new AufheizzeitManuellDaten
            {
                SchalterAn = AufheizauskunftCtrl.SchalterAn(idProjekt),
                BemessenH = v?.BemessenH,
                VonH = v?.VonH,
                BisH = v?.BisH,
                Tau2H = v == null ? null : a?.Tau2H,
                Unerreichbar = a?.Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR,
            };
        }

        /// <summary>
        /// <b>Der Zonenweg eines Projektgebäudes</b> (Softwarearchitektur 2.9, 3.3): die Zonen, wie
        /// <see cref="GebaeudeZonenCtrl.LesenJeGebaeude"/> sie liefert, die Aufbauten des Projekts und
        /// des Katalogs zur Wahl (U im Kern gerechnet), und die drei Wege des OK — Übernahme mit
        /// Hochrechnung (<see cref="GebaeudeZonenCtrl.Uebernahme"/>), Katalogaufbau in das Projekt
        /// (<see cref="BauteilaufbauCtrl.CopyFromStamm"/>) und das Aggregat der Zonen
        /// (<see cref="GebaeudeZonenCtrl.SpeichernJeGebaeude"/>).
        ///
        /// <para><b>Was die Oberfläche nicht bearbeitet, bleibt</b>: Die Spalten einer Zone, die der
        /// Zonendialog nicht führt (Kühleingaben, Herkunft; Stufe G6b, A4 (a)), hält der
        /// Weg je Id fest und schreibt sie unverändert zurück; eine neue Zone trägt die Herkunft
        /// ihres Vorschlags. Ein Duplikat (Stufe G6a, <see cref="ZoneDaten.VorlageId"/>) übernimmt diese
        /// Spalten von seiner Vorlage — ohne deren Herkunft, Quellkennung und Importpaarung.</para>
        /// </summary>
        internal static GebaeudeZonenweg Zonenweg(int idProjekt, int idZ, int idGebaeude)
            => Zonenweg(Zonenebene.Projekt, idProjekt, idZ, () => idGebaeude);

        /// <summary>
        /// <b>Der Zonenweg eines Katalogsatzes</b> (Welle ZK-b, Anwenderwunsch 08.10.2026): dieselben Wege wie im
        /// Projekt, auf den Katalogzwillingen (<see cref="Zonenebene.Katalog"/>). Der Satz wird über seinen Namen
        /// gefunden (<paramref name="ziel"/>) — im Modus Neu entsteht er erst im ersten Schritt des OK-Wegs, nach
        /// „Speichern unter" arbeitet der Editor am neuen Satz weiter. Die Bauteile wählen ihren Aufbau aus dem
        /// Aufbaukatalog (kein Übernahmeschritt); die Übernahme „Gebäude als eine Zone" rechnet mit dem Klima eines
        /// Projekts und fehlt hier. Ohne Schritt ZK steht der Weg benannt gesperrt da (<see cref="GebaeudeZonenweg.Sperre"/>).
        /// </summary>
        internal static GebaeudeZonenweg KatalogZonenweg(Katalogziel ziel)
        {
            if (!ZonenKatalogSchema.Lesbar())
                return new GebaeudeZonenweg
                {
                    Sperre = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                           MyResource.Resource.GEBZ_SPERRE_OHNE_KATALOGZONEN, ZonenKatalogSchema.SCHRITT)
                };
            return Zonenweg(Zonenebene.Katalog, 0, 0, () => ziel.Id());
        }

        /// <summary>
        /// Der Katalogsatz, an dem der Zonenweg im Katalog schreibt — über seinen Namen, den der Schreibweg des
        /// Kopfs nach einem gelungenen Anlegen nachführt.
        /// </summary>
        internal sealed class Katalogziel
        {
            /// <summary>Der Name des Satzes (<c>Tab_Gebaeude_STAMM.Bezeichner</c>); leer, solange er nicht angelegt ist.</summary>
            internal string Name { get; set; } = "";

            /// <summary>Die Id des Satzes; 0, solange es ihn nicht gibt.</summary>
            internal int Id() => string.IsNullOrEmpty(Name) ? 0 : new GebaeudeStammCtrl().Lies(Name)?.ID ?? 0;
        }

        private static GebaeudeZonenweg Zonenweg(Zonenebene ebene, int idProjekt, int idZ, Func<int> gebaeudeId)
        {
            bool katalog = ebene == Zonenebene.Katalog;
            int idGebaeude = gebaeudeId();
            var zonenCtrl = new GebaeudeZonenCtrl();
            var aufbauCtrl = new BauteilaufbauCtrl();
            var gelesen = new Dictionary<int, ZoneModel>();
            // Stufe KP2: die Ebene des Gebaeudes - die Zonen zeigen „vom Gebaeude", wo es einen Kalender angelegt hat.
            Konditionierungsstand gebaeudeebene = KonditionierungSchema.Lesbar() && idGebaeude > 0
                ? new KonditionierungCtrl().StandLesen(katalog
                    ? KonditionierungCtrl.Eigner.Katalogbau(idGebaeude)
                    : KonditionierungCtrl.Eigner.Gebaeude(idGebaeude), out _)
                : null;
            if (idGebaeude > 0)
                foreach (ZoneModel z in zonenCtrl.LesenJeGebaeude(idGebaeude, ebene)) gelesen[z.ID] = z;

            // Im Katalog zeigen die Bauteile in den Aufbaukatalog: Er ist die Liste „des Satzes", ein Übernahmeschritt
            // entfällt (ZK-b).
            var projektaufbauten = (katalog ? aufbauCtrl.LesenKatalog() : aufbauCtrl.LesenJeProjekt(idProjekt))
                .Where(a => a != null).ToDictionary(a => a.ID);
            List<AufbauWahl> projektwahl = projektaufbauten.Values.Select(a => Wahl(a, katalog)).ToList();
            List<AufbauWahl> katalogwahl = katalog
                ? new List<AufbauWahl>()
                : aufbauCtrl.LesenKatalog().Where(a => a != null).Select(a => Wahl(a, true)).ToList();

            // Die Konditionierung einer gelesenen Zone der Ebene.
            KonditionierungCtrl.Eigner Zoneneigner(int idZone)
                => katalog ? KonditionierungCtrl.Eigner.Katalogzone(gebaeudeId(), idZone)
                           : KonditionierungCtrl.Eigner.Zone(gebaeudeId(), idZone);

            ZoneDaten AlsDaten(ZoneModel z) => new ZoneDaten
            {
                Id = z.ID,
                Bezeichner = z.Bezeichner ?? "",
                Nutzflaeche = z.Nutzflaeche,
                Raumhoehe = z.Raumhoehe,
                Volumen = z.Volumen,
                IstBeheizt = z.IstBeheizt,
                SollTag = z.Raumsolltemperatur_Tag,
                SollNacht = z.Raumsolltemperatur_Nachtabsenkung,
                SollWochenende = z.Raumsolltemperatur_Wochenende,
                SollFerien = z.Raumsolltemperatur_Ferien,
                Maximaleraumtemperatur = z.Maximaleraumtemperatur,
                LuftwechselInfiltration = z.Luftwechsel_Infiltration,
                LuftwechselNutzer = z.Luftwechsel_Nutzer,
                InterneWaermegewinne = z.Interne_Waermegewinne,
                Bewohner = z.Bewohner,
                HeizungStrahlungsanteil = z.Heizung_Strahlungsanteil,
                HeizleistungMaxKw = z.Heizleistung_Max,
                // E63 (AK1z): die sieben Uebergabefelder der Zone; NULL = wie Gebaeude.
                UebergabeArt = z.Uebergabe_Art,
                UebergabeExponent = z.Uebergabe_Exponent,
                UebergabeLeistungNennKw = z.Uebergabe_Leistung_Nenn,
                AuslegungVorlauf = z.Auslegung_Vorlauf,
                AuslegungRuecklauf = z.Auslegung_Ruecklauf,
                AuslegungRaumtemperatur = z.Auslegung_Raumtemperatur,
                ReglerProportionalband = z.Regler_Proportionalband,
                // KU3-3 (E67/E68): die vier Kühlfelder der Zone; NULL = wie Gebaeude.
                KuehlungAktiv = z.Kuehlung_Aktiv,
                KuehlSollwert = z.Kuehl_Sollwert,
                KuehlSollwertNacht = z.Kuehl_Sollwert_Nacht,
                KuehlleistungMaxKw = z.Kuehlleistung_Max,
                // KK4 (Festlegung 15): die drei Kuehluebergabefelder der Zone (Schritt 137); NULL = wie Gebaeude.
                KuehlUebergabeArt = z.Kuehl_Uebergabe_Art,
                KuehlUebergabeExponent = z.Kuehl_Uebergabe_Exponent,
                KuehlUebergabeLeistungNennKw = z.Kuehl_Uebergabe_Leistung_Nenn,
                // NP3b (Q41, NP-F14): das zuletzt übernommene Nutzungsprofil; die Kalendernutzung nur zur Anzeige.
                Nutzungsprofil = z.Nutzungsprofil,
                // Die Kalendernutzung liest der Kern nur an Projektzonen; im Katalog bleibt sie leer.
                Kalendernutzung = !katalog && z.ID > 0 && RaumnutzungCtrl.Lesbar() ? RaumnutzungCtrl.Kalendernutzung(idGebaeude, z.ID) : null,
                Konditionierung = z.ID > 0
                    ? KonditionierungHuelle.Lesen(Zoneneigner(z.ID), gebaeudeebene)
                    : KonditionierungHuelle.Leer(),
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
                        Quellkennung = b.Quellkennung,
                        IdNachbarzone = b.ID_Nachbarzone,
                        TrennflaecheZuordnung = b.Trennflaeche_Zuordnung
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

            // Die Zeilen des Kerns aus dem Arbeitsstand (Stufe G6a): Was die Oberflaeche nicht fuehrt
            // (Kuehluebergabe der Zone), kommt aus der gelesenen Zeile gleicher Id; ein
            // Duplikat nimmt es von seiner Vorlage (VorlageId) - ohne Herkunft, Quellkennung und
            // Importpaarung der Vorlage; eine neue Zone ist manuell.
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
                    // Stufe G6b (W2): die Werte, die der Zonendialog fuehrt; leer = der Wert des Gebaeudes.
                    z.Raumhoehe = d.Raumhoehe;
                    z.Volumen = d.Volumen;
                    z.IstBeheizt = d.IstBeheizt;
                    z.Raumsolltemperatur_Tag = d.SollTag;
                    z.Raumsolltemperatur_Nachtabsenkung = d.SollNacht;
                    z.Raumsolltemperatur_Wochenende = d.SollWochenende;
                    z.Raumsolltemperatur_Ferien = d.SollFerien;
                    z.Maximaleraumtemperatur = d.Maximaleraumtemperatur;
                    z.Luftwechsel_Infiltration = d.LuftwechselInfiltration;
                    z.Luftwechsel_Nutzer = d.LuftwechselNutzer;
                    z.Interne_Waermegewinne = d.InterneWaermegewinne;
                    z.Bewohner = d.Bewohner;
                    z.Heizung_Strahlungsanteil = d.HeizungStrahlungsanteil;
                    z.Heizleistung_Max = d.HeizleistungMaxKw;
                    // E63 (AK1z): die Uebergabe je Zone fuehrt der Dialog - sie kommt aus dem Arbeitsstand,
                    // nicht aus der gelesenen Zeile; die Kuehlspalten bleiben, wie sie stehen.
                    z.Uebergabe_Art = d.UebergabeArt;
                    z.Uebergabe_Exponent = d.UebergabeExponent;
                    z.Uebergabe_Leistung_Nenn = d.UebergabeLeistungNennKw;
                    z.Auslegung_Vorlauf = d.AuslegungVorlauf;
                    z.Auslegung_Ruecklauf = d.AuslegungRuecklauf;
                    z.Auslegung_Raumtemperatur = d.AuslegungRaumtemperatur;
                    z.Regler_Proportionalband = d.ReglerProportionalband;
                    // KU3-3 (E67/E68): die Kuehlung je Zone fuehrt der Dialog - leer = wie Gebaeude.
                    z.Kuehlung_Aktiv = d.KuehlungAktiv;
                    z.Kuehl_Sollwert = d.KuehlSollwert;
                    z.Kuehl_Sollwert_Nacht = d.KuehlSollwertNacht;
                    z.Kuehlleistung_Max = d.KuehlleistungMaxKw;
                    // KK4 (Festlegung 15): die Kuehluebergabe je Zone fuehrt der Dialog - leer = wie Gebaeude.
                    z.Kuehl_Uebergabe_Art = d.KuehlUebergabeArt;
                    z.Kuehl_Uebergabe_Exponent = d.KuehlUebergabeExponent;
                    z.Kuehl_Uebergabe_Leistung_Nenn = d.KuehlUebergabeLeistungNennKw;
                    // NP3b: „Nutzungsprofil übernehmen…" setzt den Namen im Arbeitsstand, das OK schreibt ihn.
                    z.Nutzungsprofil = d.Nutzungsprofil;
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
                        Quellkennung = b.Quellkennung,
                        ID_Nachbarzone = b.IdNachbarzone,
                        Trennflaeche_Zuordnung = b.TrennflaecheZuordnung
                    }).ToList();
                    zeilen.Add(z);
                }
                return zeilen;
            }

            // Stufe G6b: die Luftstroeme des Arbeitsstands als Zeilen des Kerns; eine noch nicht
            // gewaehlte Zone traegt die Id 0 (keine Zone), ein fehlender Volumenstrom 0 - beides lehnt
            // die Regel des Kerns benannt ab.
            static List<ZonenluftstromModel> Luft(IReadOnlyList<ZonenluftstromDaten> liste)
                => liste?.Where(l => l != null).Select(l => new ZonenluftstromModel
                {
                    ID = l.Id, ID_ZoneA = l.IdZoneA ?? 0, ID_ZoneB = l.IdZoneB ?? 0, Volumenstrom = l.Volumenstrom ?? 0.0
                }).ToList();

            // OK-Weg, Schritt 3 (Stufe KP2, Befund B10): Zonen, Bauteile, Luftstroeme und die
            // Konditionierung der Zonen in EINEM Vorgang; zurueck kommt die Zuordnung der vorlaeufigen
            // Ids, die der Dialog in seinen Arbeitsstand uebernimmt.
            Func<ZonenstandDaten, ZonenSchreibergebnis> speichern = stand =>
            {
                int ziel = gebaeudeId();
                if (ziel <= 0) return ZonenSchreibergebnis.Fehler(MyResource.Resource.GEBZ_MSG_GEBAEUDE);
                List<ZoneModel> zeilen = Zeilen(stand?.Zonen);
                GebaeudeZonenCtrl.Schreibergebnis e = zonenCtrl.Schreiben(ziel, zeilen, Luft(stand?.Luftstroeme),
                                                                          Zonenkonditionierung(stand?.Zonen), ebene);
                if (!e.Ok)
                    return ZonenSchreibergebnis.Fehler(string.IsNullOrEmpty(e.Meldung) ? MyResource.Resource.GEBZ_MSG_GEBAEUDE : e.Meldung);
                gelesen.Clear();
                foreach (ZoneModel z in zeilen) gelesen[z.ID] = z;
                if (!katalog) MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);
                return new ZonenSchreibergebnis("", e.Zonen, e.Bauteile, e.Luftstroeme);
            };

            // Welle ZK-b: die Zonen des Satzes neu lesen - nach „Speichern unter" im Katalog am neuen Satz (der Kern
            // hat die gespeicherten Zonen dorthin kopiert); die gemerkten Zeilen folgen.
            List<ZonenluftstromDaten> LuftLesen(int id)
                => id <= 0
                    ? new List<ZonenluftstromDaten>()
                    : zonenCtrl.LuftstroemeJeGebaeude(id, ebene).Select(l => new ZonenluftstromDaten
                    {
                        Id = l.ID, IdZoneA = l.ID_ZoneA, IdZoneB = l.ID_ZoneB, Volumenstrom = l.Volumenstrom
                    }).ToList();
            Func<ZonenNeulesung> neulesen = () =>
            {
                int id = gebaeudeId();
                gelesen.Clear();
                if (id > 0)
                    foreach (ZoneModel z in zonenCtrl.LesenJeGebaeude(id, ebene)) gelesen[z.ID] = z;
                gebaeudeebene = KonditionierungSchema.Lesbar() && id > 0
                    ? new KonditionierungCtrl().StandLesen(katalog
                        ? KonditionierungCtrl.Eigner.Katalogbau(id)
                        : KonditionierungCtrl.Eigner.Gebaeude(id), out _)
                    : null;
                return new ZonenNeulesung(gelesen.Values.OrderBy(z => z.Rang).Select(AlsDaten).ToList(), LuftLesen(id));
            };

            // Die Konditionierung der Zonen fuer den Schreibweg (Stufe KP2): nur die mit geaenderter Fassung.
            static IReadOnlyDictionary<int, Konditionierungsstand> Zonenkonditionierung(IReadOnlyList<ZoneDaten> liste)
                => KonditionierungHuelle.Zonenstaende(liste);

            // Die Pruefregeln des Kerns ueber die ganze Liste samt Kopplung, ohne Datenbank (G6a/G6b).
            Func<ZonenstandDaten, string> pruefen = stand
                => GebaeudeZonenCtrl.Pruefen(Zeilen(stand?.Zonen), Luft(stand?.Luftstroeme), stand?.SollTagGebaeude) ?? "";

            // Die Hinweise der Kopplung (die Huelle jeder Zone ist geschlossen), ohne Datenbank (G6b).
            Func<IReadOnlyList<ZoneDaten>, IReadOnlyList<string>> hinweise = liste => Zonenkopplungsregeln.Hinweise(Zeilen(liste));

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
                Luftstroeme = LuftLesen(idGebaeude),
                // Die Hochrechnung braucht das Klima eines Projekts, der Katalogaufbau steht im Katalog schon zur Wahl.
                Uebernehmen = katalog ? null : uebernehmen,
                AufbauUebernehmen = katalog ? null : aufbauUebernehmen,
                Speichern = speichern,
                Neulesen = neulesen,
                Katalog = katalog,
                Pruefen = pruefen,
                Hinweise = hinweise,
                Projektaufbauten = projektwahl,
                Katalogaufbauten = katalogwahl,
                Aufbau = ansicht,
                // Stufe G6b: ohne Schemaschritt S-G (iOS migriert nicht nach) keine Trennflaeche und
                // kein Luftaustausch - benannt, der Schreibweg des Kerns lehnt sie ebenso ab.
                KopplungSperre = GebaeudeZonenanschluss.KopplungVorhanden() ? null
                    : string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.GEBZ_SPERRE_KOPPLUNG,
                                    ZonenkopplungSchema.SCHRITT),
                // E63 (AK1z): ohne den Schemaschritt der Uebergabe je Zone schreibt der Kern die vier
                // Auslegungs- und Reglerspalten nicht - benannt gesperrt statt still verloren.
                UebergabeSperre = GebaeudeZonenanschluss.UebergabespaltenVorhanden() ? null
                    : string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.ZONDLG_UEBERGABE_SPERRE,
                                    ZonenUebergabeSchema.SCHRITT),
                ProjektKoppelt = idProjekt > 0
                    ? Waermeuebergabe.StufeAn(KonfigurationCtrl.AnlagenkopplungLesen(idProjekt))
                    : null
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
            return new AufbauWahl(a.ID, katalog, text, a.Bauteilart ?? "", u, !string.IsNullOrEmpty(a.Typaufbau));
        }

        /// <summary>Die Baustoffe des Projekts — die Schichten eines Projektaufbaus zeigen auf sie.</summary>
        private static IReadOnlyList<BaustoffWahl> Projektbaustoffe(int idProjekt)
            => new BaustoffCtrl().LesenProjekt(idProjekt)
                   .Select(b => new BaustoffWahl(b.ID, b.Bezeichner ?? "", b.Lambda, b.Rho, b.Cp)).ToList();

        private static IReadOnlyDictionary<string, object> Grundgaben(
            GebaeudeModel geladen, GebaeudeKatalogModus modus)
        {
            GebaeudePrueftexte p = Prueftexte();

            // Stufe KP2: die Konditionierung des Katalogbaus im Feldsatz (ein neuer Satz beginnt leer) und
            // der Weg des Reiters; im Projekt ersetzt ProjektGaben beides.
            GebaeudeKatalogDaten daten = AusModell(geladen);
            daten.Konditionierung = geladen.ID > 0 && modus != GebaeudeKatalogModus.Neu
                ? KonditionierungHuelle.Lesen(KonditionierungCtrl.Eigner.Katalogbau(geladen.ID))
                : KonditionierungHuelle.Leer();

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = modus,
                ["Konditionierung"] = KonditionierungHuelle.Weg(Kalendereigentuemer.Katalogbau, 0),
                // Stufe KP2, Welle U1: die Texte des Reiters „Konditionierung" und seiner Rückfragen.
                ["KonditionierungTexte"] = KonditionierungTexteHuelle.Texte(),
                ["KonditionierungFragen"] = KonditionierungTexteHuelle.Fragen(),

                // Stufe NP3a: der Katalog der Nutzungsprofile als Blatt (Konzept Nutzungsprofile 6.1, NP-F22).
                ["Raumnutzung"] = RaumnutzungHuelle.Weg(),
                ["RaumnutzungTexte"] = RaumnutzungHuelle.Texte(),

                ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudetypen()),
                ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudearten(null)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    (d, istNeu, bez) => Schreiben(d, istNeu, bez)),

                // "Brauchwasser..." auf dem zweiten Reiter zeigt die Brauchwasser-Profilliste
                // des LAUFENDEN Projekts als Ueberlagerung - nur, wo die Schale den Weg
                // eingehaengt hat (Gebaeudewege). Mit Zapfprofil-Behaelter je Oeffnen (5.2);
                // die Verwaltung setzt ihren eigenen Weg ohne Behaelter (GebaeudeAdminHuelle).
                ["BrauchwasserGaben"] = Brauchwasserweg(mitZapfprofil: true),
                // Kein "BrauchwasserFertig": Das OK der Profilliste schreibt Zuordnungen und
                // Zapfprofil selbst, bevor sie schliesst (ZapfprofilHuelle.Schreibweg), und markiert
                // das Projekt nur, wenn es tatsaechlich schreibt - ein OK ohne Aenderung laesst das
                // Aenderungsdatum stehen.

                // Stufe AK1 (Anlagenkopplung 8.4, 9.1, 9.2): die hergeleiteten Vorgaben der
                // Waermeuebergabe aus dem Kern (Klimareihe des laufenden Projekts, einmal je
                // Oeffnen gelesen) und das Vorschaubild des Sollwert-Zeitprogramms.
                ["UebergabeHerleitung"] = Herleitungsweg(Dienste.Projekt.Id),
                // EV1 (E65): die Erdreichauskunft der Bodenplatte (B' und U_g aus dem Kern).
                ["ErdreichAuskunft"] = Erdreichweg(),
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
                ["LabelEnergiestandard"] = Text_("GEBK_LBL_ENERGIESTANDARD", "Energiestandard :"),
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
                ["LabelWEAbsenkung"] = Text_("GEBK_LBL_WE_ABSENKUNG", "Soll am Wochenende (ganztägig) :"),
                ["LabelSollFerien"] = Text_("GEBK_LBL_SOLL_FERIEN", "Soll in Ferien (ganztägig) :"),
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
                ["MeldungEnergiestandardWohnen"] = p.MeldungEnergiestandardWohnen,
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
            p.MeldungEnergiestandardWohnen = Text_("GEBK_MSG_ENERGIESTANDARD_WOHNEN", p.MeldungEnergiestandardWohnen);

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
            t.LabelErdreichUWirksam = Text_("GEBK_LBL_ERDREICH_U_WIRKSAM", t.LabelErdreichUWirksam);
            t.HinweisErdreichUWirksam = Text_("GEBK_HINWEIS_ERDREICH_U_WIRKSAM", t.HinweisErdreichUWirksam);
            t.SperreErdreichUWirksam = Text_("GEBK_SPERRE_ERDREICH_U_WIRKSAM", t.SperreErdreichUWirksam);
            t.ZeileErdreichRechnung = Text_("GEBK_ZEILE_ERDREICH_RECHNUNG", t.ZeileErdreichRechnung);
            t.ZeileErdreichVorgabe = Text_("GEBK_ZEILE_ERDREICH_VORGABE", t.ZeileErdreichVorgabe);
            t.MeldungErdreichU = Text_("GEBK_MSG_ERDREICH_U", t.MeldungErdreichU);
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
            t.PlatzhalterKeine = Text_("GEBK_PLATZHALTER_KEINE", t.PlatzhalterKeine);
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
            t.MeldungSpeichernUnterAngelegt = Text_("GEBK_MSG_SPEICHERN_UNTER_ANGELEGT", t.MeldungSpeichernUnterAngelegt);

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
            u.LabelHeizkurveRaumeinfluss = Text_("AK3_GEBK_LBL_RAUMEINFLUSS", u.LabelHeizkurveRaumeinfluss);
            u.ZeileRaumeinfluss = Text_("AK3_GEBK_HRL_RAUMEINFLUSS", u.ZeileRaumeinfluss);
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
            k.LabelKuehlkurve = Text_("GEBK_LBL_KUEHLKURVE_AKTIV", k.LabelKuehlkurve);
            k.LabelKuehlkurveFusspunkt = Text_("GEBK_LBL_KUEHLKURVE_FUSSPUNKT", k.LabelKuehlkurveFusspunkt);
            k.LabelKuehlkurveRaumeinfluss = Text_("GEBK_LBL_KUEHLKURVE_RAUMEINFLUSS", k.LabelKuehlkurveRaumeinfluss);
            k.LabelKuehlkurveWeg = Text_("GEBK_LBL_KUEHLKURVE_WEG", k.LabelKuehlkurveWeg);
            k.WegStunde = Text_("GEBK_KUEHLKURVE_WEG_STUNDE", k.WegStunde);
            k.WegTagesmittel = Text_("GEBK_KUEHLKURVE_WEG_TAGESMITTEL", k.WegTagesmittel);
            k.WegEingabe = Text_("GEBK_KUEHLKURVE_WEG_EINGABE", k.WegEingabe);
            k.LabelKuehlkurveAussen = Text_("GEBK_LBL_KUEHLKURVE_AUSSEN", k.LabelKuehlkurveAussen);
            k.ZeileKuehlkurve = Text_("GEBK_ZEILE_KUEHLKURVE", k.ZeileKuehlkurve);
            k.MeldungFusspunkt = Text_("GEBK_MSG_KUEHLKURVE_FUSSPUNKT", k.MeldungFusspunkt);
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

        /// <summary>
        /// <b>Die Erdreichauskunft der Bodenplatte</b> (EV1, E65): B′ und U_g, wie der Klassenweg sie für die
        /// Grundfläche rechnet (<see cref="Erdreichwiderstand.Bauteilsatz"/> mit Grundfläche, ihrem U-Wert, dem
        /// Umfangsfeld und der Vorgabe <c>Erdreich_U_Wirksam</c>). Ohne Datenbank. <c>null</c>, wenn keine
        /// Erdreichkorrektur rechnet: Randbedingung Keller oder Außenluft, keine Grundfläche oder kein U-Wert.
        /// </summary>
        internal static Func<GebaeudeKatalogDaten, ErdreichAuskunftDaten> Erdreichweg()
            => d =>
            {
                if (d == null) return null;
                string rand = d.GrundflaecheRandbedingung;
                if (!string.IsNullOrEmpty(rand) && !string.Equals(rand, DbWerte.GRUND_ERDREICH, StringComparison.Ordinal))
                    return null;
                double a = d.Grundflaeche ?? 0.0, u = d.UWertGrundflaeche ?? 0.0;
                if (!(a > 0.0) || !(u > 0.0)) return null;
                double vorgabe = d.ErdreichUWirksam is double ug && ug > 0.0 ? ug : double.NaN;
                Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (a, 180.0, u) }, a,
                                                                     d.AnschlussAussenwandKeller ?? 0.0, new double[1], vorgabe);
                if (k == null) return null;
                bool istVorgabe = k.Quelle == Erdreichumfangsquelle.Vorgabe;
                return new ErdreichAuskunftDaten(istVorgabe || double.IsNaN(k.B_M) ? null : k.B_M, k.Ug_WM2K, istVorgabe);
            };

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
        /// <b>Der Weg zur Brauchwasser-Profilliste des laufenden Projekts</b> — der Delegat
        /// <c>BrauchwasserGaben</c> des Editors; <c>null</c>, wo die Schale keinen Haken eingehängt
        /// hat (<see cref="Gebaeudewege"/>). Die Zuordnungen werden erst beim Öffnen der Überlagerung
        /// gelesen; das OK der Profilliste schreibt sie zurück, zusammen mit dem Arbeitsstand des
        /// Zapfprofils.
        /// </summary>
        /// <param name="mitZapfprofil">
        /// Reicht der Weg der Profilliste je Öffnen einen frischen Zapfprofil-Behälter
        /// (Umsetzungskonzept Zapfprofilgenerator 5.2)? <c>false</c> allein aus der Verwaltung
        /// (<see cref="GebaeudeAdminHuelle"/>).
        /// </param>
        internal static Func<IReadOnlyDictionary<string, object>> Brauchwasserweg(bool mitZapfprofil)
        {
            if (Gebaeudewege.BrauchwasserGaben == null) return null;
            var brauchwasser = new List<Z_ProjektBrauchwasserModel>();
            return () => BrauchwasserGaben(brauchwasser, mitZapfprofil);
        }

        /// <summary>
        /// Der Parametersatz der Brauchwasser-Profilliste. Die Zuordnungen des laufenden
        /// Projekts werden hier frisch gelesen — der Vorläufer tat dasselbe beim Klick.
        /// </summary>
        private static IReadOnlyDictionary<string, object> BrauchwasserGaben(
            List<Z_ProjektBrauchwasserModel> ziel, bool mitZapfprofil)
        {
            int projektId = Dienste.Projekt.Id;

            // Ohne Zapfprofil (die Verwaltung) reicht die Huelle keinen Behaelter; der
            // Bedarfsprofil-Dialog zeigt dann weder Knopf noch Optionsgruppe (Zapfprofil 5.2).
            ZapfprofilBehaelter zapfprofil = mitZapfprofil ? new ZapfprofilBehaelter(projektId) : null;

            ziel.Clear();
            ziel.AddRange(Z_ProjektBrauchwasserCtrl.LiesProjekt(projektId));

            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektBrauchwasserModel m in ziel)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser,
                    Name = m.szBezeichner ?? "", Summe = m.Summe,
                    KalenderId = m.ID_Betriebskalender
                });

            Action geaendert = () =>
            {
                ziel.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    ziel.Add(new Z_ProjektBrauchwasserModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Brauchwasser = z.IdStamm,
                        szBezeichner = z.Name, Summe = z.Summe,
                        ID_Betriebskalender = z.KalenderId
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
        ///
        /// <para><b>Im KATALOGMODUS</b> ist die Quelle der Konditionierung der Ursprungssatz —
        /// „Speichern unter" wirkt dort wie Duplizieren (Stufe KP1b, Festlegung 10); im Modus Neu
        /// gibt es keine, und der neue Satz beginnt ohne Matrix und Kalender. Der Dialog übergibt
        /// bei „Speichern unter" deshalb den URSPRUNGSNAMEN als <paramref name="bezeichner"/>; der
        /// neue Name steht im Satz selbst (Entwurf KP2, Befund B2).</para>
        /// </summary>
        internal static GebaeudeKatalogErgebnis Schreiben(
            GebaeudeKatalogDaten daten, bool istNeu, string bezeichner)
            => Schreiben(daten, istNeu, bezeichner, Katalogquelle(bezeichner));

        /// <summary>
        /// Der Ursprungs-Katalogbau als Eigentümer seiner Konditionierung; <c>null</c> ohne Namen
        /// (Modus Neu) oder wenn es den Satz nicht gibt.
        /// </summary>
        private static KonditionierungCtrl.Eigner Katalogquelle(string bezeichner)
        {
            GebaeudeModel m = string.IsNullOrEmpty(bezeichner) ? null : Laden(bezeichner);
            return m == null || m.ID <= 0 ? null : KonditionierungCtrl.Eigner.Katalogbau(m.ID);
        }

        /// <summary>
        /// Derselbe Schreibweg mit ausdrücklicher <paramref name="quelle"/> der Konditionierung —
        /// im Projektmodus das Projektgebäude, im Katalogmodus der Ursprungssatz.
        /// </summary>
        internal static GebaeudeKatalogErgebnis Schreiben(
            GebaeudeKatalogDaten daten, bool istNeu, string bezeichner,
            KonditionierungCtrl.Eigner quelle)
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

            // Stufe KP2: EINE Schreibstelle - neu, bearbeiten und „Speichern unter" schreiben Kopf und
            // Konditionierung in EINEM Vorgang samt Insert (GebaeudeStammCtrl.KatalogSchreiben).
            // Die Konditionierung kommt aus dem Arbeitsstand (Festlegung 2): bei einem neuen Satz immer
            // (so traegt „Speichern unter" sie mit), sonst nur bei geaenderter Fassung; ohne Tabellen
            // (keine Konditionierung im Feldsatz) die Kopie der Quelle wie bisher.
            Konditionierungsstand stand = KonditionierungHuelle.Schreibstand(daten, Kalendereigentuemer.Katalogbau, immer: istNeu);
            GebaeudeStammCtrl.Katalogschreibergebnis ergebnis =
                GebaeudeStammCtrl.KatalogSchreiben(modell, istNeu, bezeichner, stand, istNeu && stand == null ? quelle : null,
                                                   istNeu ? quelle : null);
            return new GebaeudeKatalogErgebnis(ergebnis.Ok,
                ergebnis.Ok
                    ? ""
                    : string.IsNullOrEmpty(ergebnis.Meldung)
                        ? Text_("GEBK_MSG_FEHLER", "Fehler beim Speichern!\nAlle Eingaben überprüfen!")
                        : ergebnis.Meldung);
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
                // G4a: das Baujahr neben der Klasse - NULL bleibt null (unbekannt); die gespeicherte
                // Klasse gilt, das Baujahr schlaegt beim Aendern nur vor.
                Baujahr = m.Baujahr,
                // E47: der Energiestandard als Code - NULL bleibt null (keiner).
                Energiestandard = string.IsNullOrEmpty(m.Energiestandard) ? null : m.Energiestandard,
                // E65: der wirksame U-Wert der Bodenplatte - NULL bleibt null (Rechnung nach DIN EN ISO 13370).
                ErdreichUWirksam = m.Erdreich_U_Wirksam,
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
                HeizkurveRaumeinfluss = m.Heizkurve_Raumeinfluss,
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
                KuehlVorlaufgrenze = m.Kuehl_Vorlaufgrenze,

                // KK (Schritt 202): die Kuehlkurve - NULL bleibt null, der Schalter NULL = aus.
                KuehlkurveAktiv = m.Kuehlkurve_Aktiv,
                KuehlkurveFusspunkt = m.Kuehlkurve_Fusspunkt,
                KuehlkurveRaumeinfluss = m.Kuehlkurve_Raumeinfluss,
                KuehlkurveAuslegungWeg = m.Kuehlkurve_Auslegung_Weg,
                KuehlkurveAuslegungAussen = m.Kuehlkurve_Auslegung_Aussen
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

            // Gespeichert wird die gewaehlte Klasse - das Baujahr schlaegt sie nur vor (Anwenderwunsch 08.10.2026).
            m.Baualtersklasse = GebaeudeStammCtrl.KlassenBuchstabe(d.Baualtersklasse).ToString();
            // G4a: das Baujahr NULL-erhaltend - leer bleibt NULL ("unbekannt"), nie 0.
            m.Baujahr = d.Baujahr;
            // E47: der Energiestandard als Code - leer bleibt NULL ("keiner").
            m.Energiestandard = string.IsNullOrEmpty(d.Energiestandard) ? null : d.Energiestandard;
            // E65: der wirksame U-Wert der Bodenplatte - leer bleibt NULL; nur ein Wert über null gilt.
            m.Erdreich_U_Wirksam = d.ErdreichUWirksam is double ug && ug > 0 ? ug : null;
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
            // AK3 (Festlegung 23): 0 heißt aus - gespeichert wie eingetragen, NULL bleibt null.
            m.Heizkurve_Raumeinfluss = d.HeizkurveRaumeinfluss;
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

            // KK (Schritt 202): die Kuehlkurve, NULL-erhaltend; ein leerer Weg wird NULL (= Tagesmittel).
            m.Kuehlkurve_Aktiv = d.KuehlkurveAktiv;
            m.Kuehlkurve_Fusspunkt = d.KuehlkurveFusspunkt;
            m.Kuehlkurve_Raumeinfluss = d.KuehlkurveRaumeinfluss;
            m.Kuehlkurve_Auslegung_Weg = string.IsNullOrEmpty(d.KuehlkurveAuslegungWeg) ? null : d.KuehlkurveAuslegungWeg;
            m.Kuehlkurve_Auslegung_Aussen = d.KuehlkurveAuslegungAussen;

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

        /// <summary>
        /// G5-N (N5/N6): der Abschnitt „Ausrichtung“ des Gebäudedialogs — gespeicherte Richtung der Planoberseite, Nordwinkel,
        /// Herkunft und Schnellwahl; ohne Importquelle nicht änderbar (<see cref="GebaeudeAusrichtungHuelle.Lesen"/>).
        /// </summary>
        internal static EPOS.UI.Dialoge.Import.GebaeudeAusrichtungDaten Ausrichtung(int idGebaeude) => GebaeudeAusrichtungHuelle.Lesen(idGebaeude);

        /// <summary>
        /// G5-N (N5): „Ausrichtung ändern“ — dreht alle Bauteile des Gebäudes in einem Vorgang und speichert den neuen
        /// Nordwinkel an der Quelle; liefert die Zahl der gedrehten Bauteile (<see cref="GebaeudeAusrichtungHuelle.Aendern"/>).
        /// </summary>
        internal static EPOS.UI.Dialoge.Import.GebaeudeAusrichtungErgebnis AusrichtungAendern(int idGebaeude, double planoberseiteGrad)
            => GebaeudeAusrichtungHuelle.Aendern(idGebaeude, planoberseiteGrad);

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
