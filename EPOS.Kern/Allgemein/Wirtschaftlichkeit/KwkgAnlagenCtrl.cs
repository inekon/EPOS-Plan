using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die KWKG-Angaben <b>einer</b> BHKW-Anlage, wie der Dialog sie zeigt und speichert
    /// (Etappe E6). Jedes Feld ist NULL-fähig; <c>null</c> heißt durchgehend „kein
    /// eigener Wert — es gilt der Projektwert".
    /// </summary>
    public class KwkgAnlagenAngabe
    {
        /// <summary><c>Tab_Energieanlagen.ID</c> — die Zeile, in die gespeichert wird.</summary>
        public int IdAnlage;

        /// <summary>Projekt der Anlage (Stamm oder Variante der Vergleichsgruppe).</summary>
        public int IdProjekt;

        /// <summary>Anzeigename des Projekts (Stamm- bzw. Variantenname).</summary>
        public string Projektname = "";

        /// <summary>Bezeichner der Anlagenzeile — zugleich der Schlüssel, über den die
        /// Rechnung sie ihrer Ergebnis-Modulzeile zuordnet.</summary>
        public string Bezeichner = "";

        /// <summary>Elektrische Nennleistung [kW] aus der Gerätezeile <c>Tab_BHKW.Pel</c>.</summary>
        public double PelKW;

        /// <summary>Bestell-/Genehmigungsdatum dieser Anlage (§ 6 KWKG 2025).</summary>
        public DateTime? Stichtag;

        /// <summary>Inbetriebnahmedatum dieser Anlage.</summary>
        public DateTime? Inbetriebnahme;

        /// <summary>Anlagenart, Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c> (leer = nicht erfasst).</summary>
        public string Anlagenart = "";

        /// <summary>Tatbestand des § 6 Abs. 3, Steuerwert <c>DbWerte.KWKG_EIGENFALL_*</c>.</summary>
        public string Eigenfall = "";

        /// <summary>Überschreibwert des Einspeisesatzes [ct/kWh].</summary>
        public double? SatzEinspCt;

        /// <summary>Überschreibwert des Eigenstromsatzes [ct/kWh].</summary>
        public double? SatzEigenCt;

        /// <summary>Vbh-Kontingent dieser Anlage [h].</summary>
        public double? VbhKontingent;

        /// <summary>Jahresdeckel-Override dieser Anlage [h/a].</summary>
        public double? VbhDeckel;

        /// <summary>ETAPPE BK1 — Anteil an den Neuherstellungskosten dieser Anlage [%]
        /// (§ 8 Abs. 2/3, Spalte <c>KWKG_Kostenanteil</c>, Schemaschritt 89);
        /// <c>null</c> = nicht gepflegt.</summary>
        public double? Kostenanteil;

        // ------------- ETAPPE B5 — die drei B3a-Spalten (Schema-Schritt 61) -------------
        //
        // Sie sind seit B3a in der Datenbank und werden vom Rechenweg gelesen
        // (WirtschaftlichkeitCtrl.LiesAnlagen), hatten aber bis B5 KEINEN Schreibweg
        // (K7). Dieselbe Nullsemantik wie oben: leer/null = „kein eigener Wert".

        /// <summary>Entlastungsnorm DIESER Anlage, Steuerwert <c>DbWerte.ENERGIESTEUER_WAHL_*</c>
        /// (leer = Projektwert). Spalte <c>Energiesteuer_Wahl</c>, TEXT(20).</summary>
        public string EnergiesteuerWahl = "";

        /// <summary>Aufteilungsmethode DIESER Anlage, Steuerwert <c>DbWerte.AUFTEILUNG_*</c>
        /// (leer = Projektwert). Spalte <c>Aufteilung_Methode</c>, TEXT(30).</summary>
        public string AufteilungMethode = "";

        /// <summary>Hilfsenergieanteil dieser Anlage [% des Endenergiebedarfs];
        /// <c>null</c> bzw. 0 = keine Hilfsenergie. Spalte <c>Hilfsenergie_Anteil</c>.</summary>
        public double? HilfsenergieAnteil;

        // ------------- ETAPPE E7c — § 2 Nr. 16 KWKG, zweiter Fall (Schritt 105) -------------

        /// <summary>Kennzeichen „Vorrichtung zur Abwärmeabfuhr" (Spalte
        /// <c>KWKG_Abwaermeabfuhr</c>, 0/1): true = KWK-Strom nach Fall 2,
        /// min(Nettostromerzeugung, Nutzwärme × σ); false = Fall 1.</summary>
        public bool Abwaermeabfuhr;

        /// <summary>Stromkennzahl σ dieser Anlage (Spalte <c>KWKG_Stromkennzahl</c>);
        /// <c>null</c> = nicht gepflegt — dann gilt P_el ÷ P_th der Gerätezeile.</summary>
        public double? Stromkennzahl;

        /// <summary>Thermische Nennleistung [kW] aus der Gerätezeile <c>Tab_BHKW.Ptherm</c>
        /// — der Nenner des Vorschlags σ = P_el ÷ P_th. <b>Nur Anzeige.</b></summary>
        public double? PthKW;

        // ------------- ETAPPE B5 — reiner Ausweis, wird NIE geschrieben -------------

        /// <summary><c>Tab_Energieanlagen.ID_Carrier</c> (0 = keiner).</summary>
        public int IdCarrier;

        /// <summary>K4: Anzeigename des Energieträgers dieser Anlage
        /// (<c>energy_carrier.name</c>), ersatzweise der Katalogbrennstoff der
        /// Gerätezeile. Leer, wenn beides fehlt. <b>Nur Anzeige</b> — die Spalte
        /// „Brennstoff" der Anlagentabelle des Dialogs.</summary>
        public string Brennstoffname = "";

        /// <summary>true, wenn der Brennstoff dieser Anlage der Kategorie „Öl" angehört
        /// (<c>WirtschaftlichkeitCtrl.BRENNSTOFF_KATEGORIE_OEL</c>) — Grundlage der
        /// Warnzeile „Heizöl-Ausschluss ab Inbetriebnahme 2025". <b>Nur Anzeige.</b></summary>
        public bool Heizoel;
    }

    /// <summary>
    /// Lese- und Schreibweg der KWKG-Angaben <b>je BHKW-Anlage</b>
    /// (<c>Tab_Energieanlagen</c>, Migrationsschritt 22, Etappe E6).
    ///
    /// <para><b>Warum ein eigener Controller.</b> <c>WirtschaftlichkeitCtrl</c> liest
    /// dieselben acht Spalten für die RECHNUNG — dort zusammen mit der Brennstoff- und
    /// Trägerauflösung, gecacht je Berechne-Lauf und über eine private Klasse. Der Dialog
    /// braucht dagegen einen Schreibweg, die Projektzugehörigkeit und keine Caches. Beide
    /// Wege teilen sich die Spaltennamen aus <see cref="SchemaKatalog"/>; eine zweite
    /// Wahrheit über die Spalten gibt es damit nicht.</para>
    ///
    /// <para><b>Der Dialog schreibt nur diese acht Spalten.</b> Ein <c>UPDATE</c> je
    /// Anlagenzeile mit namentlich aufgezählten Feldern — nie <c>SELECT *</c> und
    /// Rückschreiben, weil <c>Tab_Energieanlagen</c> 65 Spalten des Rechenkerns führt, die
    /// dieser Dialog nicht kennt und nicht anfassen darf.</para>
    ///
    /// <para><b>ETAPPE B5 (K7): elf statt acht — aber nur auf Verlangen.</b> Die drei
    /// B3a-Spalten (<c>Energiesteuer_Wahl</c>, <c>Aufteilung_Methode</c>,
    /// <c>Hilfsenergie_Anteil</c>, Schema-Schritt 61) hatten seit B3a einen Leser, aber
    /// keinen Schreibweg. <see cref="Speichere(KwkgAnlagenAngabe, bool)"/> nimmt sie mit;
    /// die parameterlose Überladung <see cref="Speichere(KwkgAnlagenAngabe)"/> schreibt
    /// unverändert die acht E6-Spalten und lässt die drei neuen ANGETASTET stehen. Das
    /// ist kein Schönheitswert: die Modulmaske aus E6 kannte die drei Felder nicht
    /// und würde sie sonst bei jedem Speichern auf null zurücksetzen.</para>
    /// </summary>
    public class KwkgAnlagenCtrl
    {
        /// <summary>
        /// Alle BHKW-Anlagen der Vergleichsgruppe (Stammprojekt und seine Varianten) mit
        /// ihren KWKG-Angaben, in Projekt- und Lesereihenfolge.
        /// </summary>
        public List<KwkgAnlagenAngabe> LadeGruppe(int idStamm, string stammName)
        {
            var liste = new List<KwkgAnlagenAngabe>();
            if (idStamm <= 0) return liste;

            if (string.IsNullOrEmpty(stammName)) stammName = Projektname(idStamm);

            var varianten = new VariantenCtrl();
            varianten.StelleVariantentabelleSicher();
            foreach (VariantenCtrl.VarianteInfo vi in varianten.LadeGruppe(idStamm, stammName))
                Lade(vi.IdProjekt, string.IsNullOrEmpty(vi.Variantenname)
                                   ? vi.Projektname : vi.Variantenname, liste);
            return liste;
        }

        /// <summary>Die BHKW-Anlagen EINES Projekts anhängen.</summary>
        private void Lade(int idProjekt, string projektname, List<KwkgAnlagenAngabe> liste)
        {
            try
            {
                // ETAPPE B5: zuerst MIT den drei B3a-Spalten, sonst ohne. Dieselbe
                // Fähigkeitstreppe wie in WirtschaftlichkeitCtrl.LiesAnlagen und aus
                // demselben Grund: DataRepository liefert bei einem SQL-Fehler eine
                // LEERE Tabelle statt zu werfen — erkannt wird der Zustand deshalb an
                // der Spaltenliste, nicht an einer Ausnahme.
                // ETAPPE BK1: eine Stufe mehr — der Kostenanteil je Anlage entsteht
                // erst mit Migrationsschritt 89.
                // ETAPPE E7c (Befund K-1): eine Stufe mehr — Kennzeichen und
                // Stromkennzahl entstehen erst mit Migrationsschritt 105.
                DataTable dt = Anlagentabelle(idProjekt, true, true, true);
                bool mitK1 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR);
                if (!mitK1) dt = Anlagentabelle(idProjekt, true, true, false);
                bool mitBk1 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL);
                if (!mitBk1) dt = Anlagentabelle(idProjekt, true, false, false);
                bool mitB3a = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL);
                if (!mitB3a) dt = Anlagentabelle(idProjekt, false, false, false);
                if (dt == null || !dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG)) return;

                foreach (DataRow r in dt.Rows)
                {
                    var g = new KwkgAnlagenAngabe
                    {
                        IdAnlage = Ganzzahl(r, "ID"),
                        IdProjekt = idProjekt,
                        Projektname = projektname ?? "",
                        Bezeichner = Text(r, "Bezeichner"),
                        PelKW = D(r, "Pel") ?? 0,
                        Stichtag = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_STICHTAG),
                        Inbetriebnahme = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME),
                        Anlagenart = Text(r, SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART),
                        Eigenfall = Text(r, SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL),
                        SatzEinspCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP),
                        SatzEigenCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN),
                        VbhKontingent = D(r, SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT),
                        VbhDeckel = D(r, SchemaKatalog.SPALTE_EA_KWKG_DECKEL),
                        IdCarrier = Ganzzahl(r, "ID_Carrier")
                    };
                    if (mitBk1) g.Kostenanteil = D(r, SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL);
                    if (mitK1)
                    {
                        g.Abwaermeabfuhr = Ganzzahl(r, SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR) == 1;
                        g.Stromkennzahl = D(r, SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL);
                        g.PthKW = D(r, "Ptherm");
                    }
                    if (mitB3a)
                    {
                        g.EnergiesteuerWahl = Text(r, SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL);
                        g.AufteilungMethode = Text(r, SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE);
                        g.HilfsenergieAnteil = D(r, SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL);
                    }
                    // K4: Anzeigename des Brennstoffs — Träger vor Gerät, dieselbe
                    // Rangfolge wie WirtschaftlichkeitCtrl.BrennstoffId.
                    int idBrennstoff = BrennstoffId(g.IdCarrier, Ganzzahl(r, "Brennstoff"));
                    g.Brennstoffname = Brennstoffname(idBrennstoff);
                    g.Heizoel = Kategorie(idBrennstoff)
                                == WirtschaftlichkeitCtrl.BRENNSTOFF_KATEGORIE_OEL;
                    liste.Add(g);
                }
            }
            catch { }
        }

        /// <summary>Die Anlagenabfrage — mit oder ohne die drei B3a-Spalten.
        /// <c>null</c> = die Abfrage ist gescheitert (bei gesetztem Flag in aller Regel,
        /// weil die Spalten fehlen).</summary>
        private static DataTable Anlagentabelle(int idProjekt, bool mitB3a, bool mitBk1, bool mitK1)
        {
            string b3a = mitB3a
                ? ", a.[" + SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "]"
                : "";
            string bk1 = mitBk1 ? ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL + "]" : "";
            // ETAPPE E7c: die zwei Spalten aus Schritt 105 und P_th der Gerätezeile
            // (Nenner des Vorschlags σ = P_el ÷ P_th).
            string k1 = mitK1
                ? ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL + "]" +
                  ", b.Ptherm"
                : "";
            try
            {
                using (DataRepository.EngineModus())
                    return DataRepository.GetDataTable(
                        "SELECT a.ID, a.Bezeichner, a.ID_Carrier, b.Pel, b.Brennstoff" +
                        Spaltenliste("a") + b3a + bk1 + k1 + " " +
                        "FROM Tab_Energieanlagen AS a " +
                        "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                        // KEIN ORDER BY — wortgleiche Begründung wie in
                        // WirtschaftlichkeitCtrl.AnlagenTabelle: Die Zuordnung
                        // Anlage ↔ Ergebnismodul fällt notfalls auf die Lesereihenfolge
                        // zurück, und die entsteht dort ebenfalls ungeordnet.
                        "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.BHKW_TYP,
                        new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        /// <summary>
        /// Speichert die acht E6-Angaben einer Anlagenzeile — <b>Bestandsverhalten,
        /// unverändert</b>. Die drei B3a-Spalten bleiben unangetastet. Liefert false,
        /// wenn das UPDATE scheitert oder keine Zeile trifft.
        /// </summary>
        public bool Speichere(KwkgAnlagenAngabe g)
        {
            return Speichere(g, false);
        }

        /// <summary>
        /// ETAPPE B5 (K7) — speichert die Angaben einer Anlagenzeile, wahlweise
        /// einschließlich der drei B3a-Spalten <c>Energiesteuer_Wahl</c>,
        /// <c>Aufteilung_Methode</c> und <c>Hilfsenergie_Anteil</c> (Schema-Schritt 61).
        ///
        /// <para><b>Warum ein Schalter und kein zweiter Aufruf.</b> Die drei Spalten
        /// tragen dieselbe Nullsemantik wie die acht E6-Spalten: leer heißt
        /// „Projektwert". Ein Aufrufer, der sie nicht kennt (E6-Dialog
        /// der Modulmaske aus E6), hat sie auch nicht im Bildschirmzustand — er würde
        /// beim Speichern gepflegte Werte still auf NULL zurücksetzen. Der Schalter
        /// hält die beiden Fälle auseinander, ohne die Spaltennamen zu doppeln.</para>
        /// </summary>
        /// <param name="g">Die zu speichernde Anlagenzeile.</param>
        /// <param name="mitSteuerangaben">true = alle Spalten des BHKW-Dialogs (die acht
        /// E6-Spalten, Kostenanteil, die drei B3a-Spalten und — ETAPPE E7c — Kennzeichen
        /// „Vorrichtung zur Abwärmeabfuhr" und Stromkennzahl aus Schritt 105),
        /// false = die acht E6-Spalten wie im Bestand.</param>
        public bool Speichere(KwkgAnlagenAngabe g, bool mitSteuerangaben)
        {
            if (g == null || g.IdAnlage <= 0) return false;
            try
            {
                string satz =
                    "UPDATE Tab_Energieanlagen SET " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_STICHTAG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_DECKEL + "] = ?";
                var werte = new List<DbParam>
                {
                    Datumswert(g.Stichtag),
                    Datumswert(g.Inbetriebnahme),
                    Textwert(g.Anlagenart, 24),
                    Textwert(g.Eigenfall, 24),
                    Zahlwert(g.SatzEinspCt),
                    Zahlwert(g.SatzEigenCt),
                    Zahlwert(g.VbhKontingent),
                    Zahlwert(g.VbhDeckel)
                };

                if (mitSteuerangaben)
                {
                    // ETAPPE BK1: der Kostenanteil je Anlage geht denselben Weg wie die
                    // drei B3a-Spalten — nur der Dialog, der ihn kennt, schreibt ihn.
                    satz += ", [" + SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL + "] = ?" +
                            ", [" + SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL + "] = ?" +
                            ", [" + SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE + "] = ?" +
                            ", [" + SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] = ?";
                    // Breiten wie in SchemaKatalog.Schritt61_SteuerJeAnlage — ein zu
                    // langer Steuerwert ließe das UPDATE sonst STILL scheitern
                    // (Lehre aus Etappe E3, Probe C2).
                    werte.Add(Zahlwert(g.Kostenanteil));
                    werte.Add(Textwert(g.EnergiesteuerWahl, 20));
                    werte.Add(Textwert(g.AufteilungMethode, 30));
                    // 0 ist hier ein gültiger Wert („keine Hilfsenergie"), NULL heißt
                    // dasselbe — Zahlwert bildet beides ab, wie es hereinkommt.
                    werte.Add(Zahlwert(g.HilfsenergieAnteil));

                    // ETAPPE E7c (Befund K-1): Kennzeichen und Stromkennzahl gehen
                    // denselben Weg — nur der Dialog, der sie kennt (Überlagerung „Sätze
                    // und Herkunft"), schreibt sie. Ohne die Spalten (Datenbank vor
                    // Schritt 105, deren Vorsorge nicht lief) bleiben sie weg, statt das
                    // ganze UPDATE scheitern zu lassen. Eine Kennzahl ≤ 0 ist „kein
                    // eigener Wert" und wird NULL.
                    if (DataRepository.SpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN,
                                                       SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR))
                    {
                        satz += ", [" + SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR + "] = ?" +
                                ", [" + SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL + "] = ?";
                        werte.Add(new DbParam("@k", DbParamTyp.Integer) { Wert = g.Abwaermeabfuhr ? 1 : 0 });
                        werte.Add(Zahlwert(g.Stromkennzahl.HasValue && g.Stromkennzahl.Value > 0
                                           ? g.Stromkennzahl : null));
                    }
                }

                satz += " WHERE ID = ?";
                werte.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = g.IdAnlage });
                return DataRepository.ExecuteSQL(satz, werte.ToArray());
            }
            catch { return false; }
        }

        // ------------------------------------------------------------- Hilfsmittel

        /// <summary>Projektname eines Projekts; leer, wenn er sich nicht lesen lässt.</summary>
        private static string Projektname(int idProjekt)
        {
            try
            {
                using (DataRepository.EngineModus())
                {
                    object o = DataRepository.ExecuteScalar(
                        "SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                        new DbParam("@p", idProjekt));
                    if (o != null && o != DBNull.Value) return Convert.ToString(o).Trim();
                }
            }
            catch { }
            return "";
        }

        // ------------------------------------------------------ K4: Brennstoffname
        //
        // ETAPPE B5, Lücke K4: Die Anlagentabelle des Dialogs zeigt eine Spalte
        // „Brennstoff". Ein Leseweg dafür fehlte — das Anlagenmodell des Rechenwegs
        // führt nur die IDs (BhkwAnlage.IdCarrier/IdBrennstoff) und das Kennzeichen
        // Heizöl. Der Leser hier ist bewusst KLEIN: zwei Nachschlagelisten, einmal je
        // Dialogaufbau gefüllt, reine ANZEIGE. Er rechnet nichts und schreibt nichts.

        private Dictionary<int, string> _brennstoffName;
        private Dictionary<int, int> _brennstoffKategorie;
        private Dictionary<int, int> _carrierBrennstoff;

        /// <summary>
        /// Der maßgebliche <c>Tab_Brennstoff_Stamm.ID</c> einer Anlage (0 = nicht
        /// ermittelbar) — <b>Träger vor Gerät</b>, wortgleiche Rangfolge wie
        /// <c>WirtschaftlichkeitCtrl.BrennstoffId</c>: Der Energieträger hängt an der
        /// ANLAGE und ist seit dem Energieträger-Umbau die maßgebliche Zuordnung; die
        /// Gerätezeile <c>Tab_BHKW.Brennstoff</c> trägt den Brennstoff des KATALOGgeräts
        /// und greift nur, wenn die Anlage keinen Träger führt oder der Träger nicht bis
        /// zu einer bekannten Kategorie durchläuft.
        /// </summary>
        private int BrennstoffId(int idCarrier, int idBrennstoff)
        {
            Nachschlagelisten();
            int ausTraeger;
            if (idCarrier > 0 && _carrierBrennstoff.TryGetValue(idCarrier, out ausTraeger) &&
                ausTraeger > 0 && _brennstoffKategorie.ContainsKey(ausTraeger)) return ausTraeger;
            return idBrennstoff;
        }

        /// <summary>Anzeigename eines Brennstoffs; leer, wenn er nicht lesbar ist.</summary>
        private string Brennstoffname(int idBrennstoff)
        {
            Nachschlagelisten();
            string name;
            return idBrennstoff > 0 && _brennstoffName.TryGetValue(idBrennstoff, out name)
                 ? name : "";
        }

        /// <summary>Kategorie eines Brennstoffs (<c>Tab_BrennstoffKategorien.ID</c>);
        /// 0 = nicht ermittelbar.</summary>
        private int Kategorie(int idBrennstoff)
        {
            Nachschlagelisten();
            int k;
            return idBrennstoff > 0 && _brennstoffKategorie.TryGetValue(idBrennstoff, out k) ? k : 0;
        }

        /// <summary>Die drei Nachschlagelisten einmal je Prozesslauf. Scheitert eine
        /// Abfrage, bleibt die Liste leer — dann steht die Spalte leer, und sonst
        /// ändert sich nichts.</summary>
        private void Nachschlagelisten()
        {
            if (_brennstoffName != null) return;
            _brennstoffName = new Dictionary<int, string>();
            _brennstoffKategorie = new Dictionary<int, int>();
            _carrierBrennstoff = new Dictionary<int, int>();
            try
            {
                using (DataRepository.EngineModus())
                {
                    DataTable bs = DataRepository.GetDataTable(
                        "SELECT ID, Bezeichner, ID_Kategorie FROM Tab_Brennstoff_Stamm");
                    if (bs != null)
                        foreach (DataRow r in bs.Rows)
                        {
                            int id = Zahl(r, 0);
                            if (id <= 0) continue;
                            if (!_brennstoffName.ContainsKey(id))
                                _brennstoffName[id] = r[1] == DBNull.Value
                                                    ? "" : Convert.ToString(r[1]).Trim();
                            if (!_brennstoffKategorie.ContainsKey(id))
                                _brennstoffKategorie[id] = Zahl(r, 2);
                        }

                    DataTable ec = DataRepository.GetDataTable(
                        "SELECT id, ID_Brennstoff FROM energy_carrier");
                    if (ec != null)
                        foreach (DataRow r in ec.Rows)
                        {
                            int id = Zahl(r, 0);
                            if (id > 0 && !_carrierBrennstoff.ContainsKey(id))
                                _carrierBrennstoff[id] = Zahl(r, 1);
                        }
                }
            }
            catch { }
        }

        private static int Zahl(DataRow r, int spalte)
        {
            if (r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        /// <summary>Die acht E6-Spalten als SELECT-Anhang, mit Tabellenpräfix.</summary>
        private static string Spaltenliste(string praefix)
        {
            string s = "";
            foreach (SchemaSpalte sp in SchemaKatalog.Schritt22_KwkgJeAnlage)
                s += ", " + praefix + ".[" + sp.Name + "]";
            return s;
        }

        /// <summary>NULL statt 0 — „kein eigener Wert" ist etwas anderes als der Wert 0.</summary>
        private static DbParam Zahlwert(double? v)
        {
            return new DbParam("@d", DbParamTyp.Double)
            { Wert = v.HasValue ? (object)v.Value : DBNull.Value };
        }

        private static DbParam Datumswert(DateTime? v)
        {
            return new DbParam("@t", DbParamTyp.Date)
            { Wert = v.HasValue ? (object)v.Value.Date : DBNull.Value };
        }

        /// <summary>Steuerwert gekürzt auf die Spaltenbreite — ein zu langer Wert ließe das
        /// UPDATE STILL scheitern (die Lehre aus Etappe E3, Probe C2).</summary>
        private static DbParam Textwert(string s, int laenge)
        {
            object wert = DBNull.Value;
            if (!string.IsNullOrEmpty(s))
            {
                string t = s.Trim();
                if (t.Length > 0) wert = t.Length > laenge ? t.Substring(0, laenge) : t;
            }
            return new DbParam("@s", DbParamTyp.VarWChar, laenge) { Wert = wert };
        }

        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            try { return Convert.ToString(r[spalte]).Trim(); } catch { return ""; }
        }

        private static DateTime? Datum(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDateTime(r[spalte]); } catch { return null; }
        }
    }
}
